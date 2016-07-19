using UnityEngine;
using System.Collections;

public class Hub : MonoBehaviour
{
    public static Transform world { get; private set; }
    public Transform _world;

    public static GameObject currentDrawing { get; set; }

    public static GameObject defaultTrail { get; private set; }
    public GameObject _defaultTrail;

    public static Rigidbody globeRigidbody { get; private set; }
    public Rigidbody _globeRigidbody;

    public static SteamVR_TrackedObject leftHand { get; private set; }
    public SteamVR_TrackedObject _leftHand;

    public static SteamVR_TrackedObject rightHand { get; private set; }
    public SteamVR_TrackedObject _rightHand;


    void Awake()
    {
        world = _world;
        defaultTrail = _defaultTrail;
        globeRigidbody = _globeRigidbody;
        leftHand = _leftHand;
        rightHand = _rightHand;
    }
}
