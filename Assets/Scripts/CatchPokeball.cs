using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SteamVR_TrackedObject))]
public class CatchPokeball : MonoBehaviour
{
    public GameObject pokeball;
    public Rigidbody attachPoint;

    private SteamVR_Controller.Device _device;

    SteamVR_TrackedObject trackedObj;
    FixedJoint joint;

    void Awake()
    {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
        _device = SteamVR_Controller.Input((int)trackedObj.index);
    }

    void Update()
    {
        if (_device != null) return;
    }
}
