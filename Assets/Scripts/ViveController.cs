using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ViveController : MonoBehaviour
{
    public Rigidbody _attach;
    public ViveController _otherHand;
    public Transform _attachPoint;

    private Transform _tr;
    private SteamVR_TrackedObject _trackedObj;
    private GrabbableObj _grabbedObj = null;
    private List<GrabbableObj> _touchedObj = new List<GrabbableObj>();
    private Vector3 _lastPos;
    
    void Start()
    {
        _tr = this.transform;
        _trackedObj = this.GetComponent<SteamVR_TrackedObject>();
    }

    void Update()
    {
        if (_trackedObj != null)
        {
            var device = SteamVR_Controller.Input((int)_trackedObj.index);

            if (device != null)
            {
                if (device.GetPress(SteamVR_Controller.ButtonMask.Trigger)
                    && device.GetPressDown(SteamVR_Controller.ButtonMask.ApplicationMenu)
                    && device.GetPress(SteamVR_Controller.ButtonMask.Touchpad)
                    && device.GetPress(SteamVR_Controller.ButtonMask.Grip))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }

                if (device.GetPressDown(SteamVR_Controller.ButtonMask.ApplicationMenu))
                {
                    if (Hub.pokeball == null)
                    {
                        Debug.LogWarning("No pokeball assigned in the hub.");
                    }
                    else
                    {
                        Hub.pokeball.StartBacking(_tr.position);
                    }
                }

                if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
                {
                    if (Bag.TakePokeball(_tr.position))
                    {
                        Debug.LogWarning("Grabbing a pokeball !");
                        // The player picks an empty Pokeball.
                        GameObject pokeball = (GameObject)GameObject.Instantiate(Resources.Load("PokeBall"), _attachPoint.position, _attachPoint.rotation);

                        if (pokeball == null)
                        {
                            Debug.LogError("Can't instantiate Pokeball");
                        }
                        else
                        {
                            GrabbableObj grabbableObj = pokeball.GetComponent<GrabbableObj>();

                            if (grabbableObj == null)
                            {
                                Debug.LogError("No GrabbableObj found in " + pokeball.name);
                            }
                            else
                            {
                                StartCoroutine(GrabObjNextFrameCoroutine(grabbableObj));
                            }
                        }
                    }
                    else
                    {
                        GrabbableObj closestObj = null;
                        float shortestDistance = float.MaxValue;
                        Vector3 myPos = this.transform.position;

                        foreach (GrabbableObj obj in _touchedObj)
                        {
                            float dist = (obj.transform.position - myPos).sqrMagnitude;

                            if (dist < shortestDistance)
                            {
                                shortestDistance = dist;
                                closestObj = obj;
                            }
                        }

                        if (closestObj != null)
                        {
                            GrabObj(closestObj);
                        }
                    }
                }

                if (device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
                {
                    DropObjBis();
                    //DropObj((this.transform.position - _lastPos) * 100f);
                }

                if (device.GetPress(SteamVR_Controller.ButtonMask.Grip) 
                    && device.GetPress(SteamVR_Controller.ButtonMask.ApplicationMenu))
                {
                    // Mécanique de déplacement.
                    Vector3 delta = _tr.position - _lastPos; // Here we get the vector that represents the move of the hand since it clicks.
                    delta.y = 0; // On empêche le joueur de "zoomer" en montant ou descendant la manette.
                    Hub.world.position += delta * 2f; // We add the offset of the move to the center of the map at the time the player clicked.
                }
            }
        }

        _lastPos = _tr.position;
    }

    public void OnTriggerEnter(Collider other)
    {
        GrabbableObj grabbableObj = other.gameObject.GetComponent<GrabbableObj>();

        if (grabbableObj != null && grabbableObj.IsGrabbable() && !_touchedObj.Contains(grabbableObj))
        {
            _touchedObj.Add(grabbableObj);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        GrabbableObj grabbableObj = other.gameObject.GetComponent<GrabbableObj>();
        if (grabbableObj != null && _touchedObj.Contains(grabbableObj))
        {
            _touchedObj.Remove(grabbableObj);
        }
    }

    IEnumerator GrabObjNextFrameCoroutine(GrabbableObj grabbableObj)
    {
        yield return new WaitForEndOfFrame();

        GrabObj(grabbableObj);
    }

    private void GrabObj(GrabbableObj grabbableObj)
    {
        if (grabbableObj.StartGrabbing(this.transform))
        {
            grabbableObj.SetAttachPoint(_attachPoint);
            //grabbableObj.Clip(_anchor, this.name);
            this.DropObj();
            _otherHand.DropObj(grabbableObj);
            _grabbedObj = grabbableObj;
        }
    }

    public void DropObj(GrabbableObj matchingObj = null)
    {
        if (_grabbedObj == null)
        {
            return;
        }
        else if (matchingObj != null && matchingObj != _grabbedObj)
        {
            return;
        }

        _grabbedObj.SetAttachPoint(null);
        _grabbedObj.StopGrabbing();
        _grabbedObj = null;
    }

    public void DropObj(Vector3 force)
    {
        if (_grabbedObj == null)
        {
            return;
        }

        _grabbedObj.SetAttachPoint(null);
        _grabbedObj.StopGrabbing(force);
        _grabbedObj = null;
    }

    public void DropObjBis()
    {
        if (_grabbedObj == null)
        {
            return;
        }

        SteamVR_Controller.Device device = SteamVR_Controller.Input((int)_trackedObj.index);

        if (device != null)
        {
            _grabbedObj.SetAttachPoint(null);
            _grabbedObj.StopGrabbingBis(device.velocity, device.angularVelocity);
            //_grabbedObj.Unclip(this.name);
            _grabbedObj = null;
        }
    }
}