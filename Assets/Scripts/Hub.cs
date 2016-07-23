using UnityEngine;
using System.Collections;

public class Hub : MonoBehaviour
{
    public static Transform world { get; private set; }
    public Transform _world;

    public static SteamVR_TrackedObject leftHand { get; private set; }
    public SteamVR_TrackedObject _leftHand;

    public static SteamVR_TrackedObject rightHand { get; private set; }
    public SteamVR_TrackedObject _rightHand;

    public static Pokeball pokeball { get; private set; }
    public Pokeball _pokeball;

    public static Transform hologramsContainer { get; private set; }
    public Transform _hologramsContainer;

    public static float playerHeight = -1f;

    void Awake()
    {
        world = _world;
        leftHand = _leftHand;
        rightHand = _rightHand;
        pokeball = _pokeball;
        hologramsContainer = _hologramsContainer;
    }
}
