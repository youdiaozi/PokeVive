using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GrabbableObj : MonoBehaviour
{
    public string _identity;
    public bool _scaleChangeOnClip;
    public PhotoSnap _snapAtStart;

    private Transform _tr;
    private Vector3 _defaultScale;

    private Rigidbody _rigid;
    private bool _useGravity;
    private bool _isKinematic;

    private List<PhotoSnap> _hoveredAnchors = new List<PhotoSnap>();
    private PhotoSnap _anchor = null;
    private float _transitionTime = 1f;
    private float _transitionDuration = 0.5f;

    private bool _isBeingGrabbed = false;
    private bool _isClipped = false;

    private Transform _defaultParent;

    void Start()
    {
        _tr = this.transform;
        _defaultScale = new Vector3(_tr.lossyScale.x, _tr.lossyScale.y, _tr.lossyScale.z);
        _rigid = this.GetComponent<Rigidbody>();

        _defaultParent = _tr.parent;

        if (_rigid == null)
        {
            Debug.LogError("Missing Rigidobdy on " + this.name);
            Destroy(this.gameObject);
            return;
        }
        else
        {
            _useGravity = _rigid.useGravity;
            _isKinematic = _rigid.isKinematic;
        }

        Collider[] colliders = this.GetComponents<Collider>();

        bool triggerFound = false;
        foreach (Collider coll in colliders)
        {
            if (coll.isTrigger)
            {
                triggerFound = true;
            }
        }

        if (!triggerFound)
        {
            Debug.LogError("Missing Trigger on " + this.name);
            Destroy(this.gameObject);
            return;
        }

        if (_snapAtStart != null)
        {
            Clip(_snapAtStart, "grab obj start", true);
        }
    }

    void Update()
    {
        if (_transitionTime < _transitionDuration)
        {
            _transitionTime += Time.deltaTime;
            float ratio = Mathf.Clamp01(_transitionTime / _transitionDuration);

            _tr.localPosition = Vector3.Lerp(_tr.localPosition, _anchor._snappingPoint.localPosition, ratio);
            _tr.localRotation = Quaternion.Slerp(_tr.localRotation, _anchor._snappingPoint.localRotation, ratio);

            if (_scaleChangeOnClip)
            {
                _tr.localScale = Vector3.Slerp(_tr.localScale, _anchor._snappingPoint.localScale, ratio);
            }
        }

        if (_scaleChangeOnClip && !_isClipped && _tr.lossyScale != _defaultScale)
        {
            Debug.Log("adapting scale back to default");
            float speed = 5f * Time.deltaTime;

            // Cette fonction est censée faire un changement de scale animé sur plusieurs frames, mais visiblement l'algo est faux.
            float xRatio = ((((_defaultScale.x / _tr.lossyScale.x) - 1f) * speed) + 1f) * _tr.localScale.x;
            float yRatio = ((((_defaultScale.y / _tr.lossyScale.y) - 1f) * speed) + 1f) * _tr.localScale.y;
            float zRatio = ((((_defaultScale.z / _tr.lossyScale.z) - 1f) * speed) + 1f) * _tr.localScale.z;

            _tr.localScale = new Vector3(xRatio, yRatio, zRatio);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        PhotoSnap snap = other.gameObject.GetComponent<PhotoSnap>();

        if (snap != null && !_hoveredAnchors.Contains(snap))
        {
            if (other.transform.IsChildOf(_tr))
            {
                Debug.LogError("anchor is child");
                return;
            }

            // Here we lit the anchor so we know we can drop the picture and it will clip.
            snap.Lit(true);
            _hoveredAnchors.Add(snap);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        PhotoSnap snap = other.gameObject.GetComponent<PhotoSnap>();
        if (snap != null && _hoveredAnchors.Contains(snap))
        {
            snap.Lit(false);
            _hoveredAnchors.Remove(snap);
        }
    }

    public void StartGrabbing(Transform parent, bool adaptScale = false)
    {
        Vector3 scaleBefore = _tr.lossyScale;
        this.transform.SetParent(parent, true);
        Vector3 scaleAfter = _tr.lossyScale;

        if (_scaleChangeOnClip && adaptScale)
        {
            _tr.localScale = new Vector3(_tr.localScale.x / (scaleAfter.x / scaleBefore.x), _tr.localScale.y / (scaleAfter.y / scaleBefore.y), _tr.localScale.z / (scaleAfter.z / scaleBefore.z));
        }

        _rigid.useGravity = true;
        _rigid.isKinematic = true;

        _isBeingGrabbed = true;

        if (_isClipped && _anchor != null)
        {
            ClipEventManager.singleton.UnclipEvent(_identity, _anchor._name);
        }

        if (_isClipped && !adaptScale)
        {
            _anchor.UnregisterMe(this);
            Debug.Log("GrabbableObject.StartGrabbing");
            _isClipped = false;
            _anchor = null;
        }
    }

    public void StopGrabbing(Vector3? force = null)
    {
        if (_identity == "selfie stick")
        {
            this.transform.SetParent(null, true);
        }
        else
        {
            this.transform.SetParent(Hub.world, true);
        }

        _rigid.useGravity = _useGravity;
        _rigid.isKinematic = _isKinematic;

        if (force != null)
        {
            if (_hoveredAnchors.Count > 0)
            {
                Clip(_hoveredAnchors[0], "stop grabbing");
            }
            else
            {
                _rigid.AddForce((Vector3)force * _rigid.mass, ForceMode.Impulse);
            }
        }

        _isBeingGrabbed = false;
    }

    public void StopGrabbingBis(Vector3 velocity, Vector3 angularVelocity)
    {
        if (_identity == "selfie stick")
        {
            this.transform.SetParent(null, true);
        }
        else
        {
            this.transform.SetParent(Hub.world, true);
        }

        _rigid.useGravity = _useGravity;
        _rigid.isKinematic = _isKinematic;

        if (velocity != null)
        {
            if (_hoveredAnchors.Count > 0)
            {
                Clip(_hoveredAnchors[0], "stop grabbing");
            }
            else
            {
                _rigid.velocity = velocity;
                _rigid.angularVelocity = angularVelocity;
            }
        }

        _isBeingGrabbed = false;
    }

    public void Clip(PhotoSnap anchor, string caller, bool instantTransition = false)
    {
        Debug.Log("isClipped = true " + caller);
        _isClipped = true;
        _anchor = anchor;
        anchor.EjectOtherAndRegisterMe(this);

        if (instantTransition)
        {
            _transitionTime = _transitionDuration * 0.99f;
        }
        else
        {
            _transitionTime = 0f;
        }
        
        StartGrabbing(anchor._snappingPoint, true);

        ClipEventManager.singleton.ClipEvent(_identity, _anchor._name);
    }

    public void Unclip(string caller)
    {
        ClipEventManager.singleton.UnclipEvent(_identity, _anchor._name);
        Debug.Log("isClipped = false " + caller);
        _isClipped = false;
        _anchor = null;
        StopGrabbing();
    }

    public bool IsGrabbed()
    {
        return _isBeingGrabbed;
    }
}