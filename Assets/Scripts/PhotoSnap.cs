using UnityEngine;
using System.Collections;

public class PhotoSnap : MonoBehaviour
{
    public string _name;
    public Transform _snappingPoint;
    public Renderer _indicator;

    private GrabbableObj _snappedObj = null;

    void Start()
    {
        Lit(false);
    }

    public void EjectOtherAndRegisterMe(GrabbableObj obj)
    {
        if (_snappedObj != null)
        {
            _snappedObj.Unclip("PhotoSnap.Clip");
        }

        _snappedObj = obj;

        Lit(false);
    }

    public void UnregisterMe(GrabbableObj obj)
    {
        if (_snappedObj == obj)
        {
            _snappedObj = null;
            Lit(true);
        }
    }

    public void Lit(bool on = true)
    {
        if (_indicator == null)
        {
            return;
        }

        if (on)
        {
            _indicator.material.color = new Color(1f, 1f, 0f, 0.5f);
        }
        else
        {
            _indicator.material.color = Color.clear;
        }
    }
}