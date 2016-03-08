using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NewtonVR;

public enum PokeballState // Only one of those states can be true at a time.
{
    // 2 additionnal status aren't states so they are not stored in here.
    // Shrinked.
    // Content.

    // Common.
    ERROR,
    Lying,
    Held,
    Thrown, // An empty Pokeball can only catch/call a pokemon if it has been thrown by the player.

    // Relative to an empty Pokeball.
    BouncingBack, // After an empty Pokeball touches a wild Pokemon, the first thing it does it going backward.
    Swallowing, 
    Falling, // Before shaking.
    Shaking, // Last step before validating a capture.
    Breaking, // When the capture failed. This is actually not a state, but only an event.
    Confirming, // When the capture succeeds. This is actually not a state, but only an event.

    // Relative to a loaded Pokeball.
    Releasing,
    Backing // Going back to the hand.
};

public class Pokeball : MonoBehaviour
{
    public bool _editorTest = false;

    private const float _pokeballCatchRadius = 0.1f; // The real pokeball radius is 0.05.

    private Transform _tr;
    private Rigidbody _rigid;
    private NVRInteractableItem _interactableItem;
    private List<Vector3> _verticesToDraw = new List<Vector3>(); // A virer.

    private bool _shrinked = false;
    private bool _isRecalling = false; // True when the Pokéball is trying to call back its Pokémon using its red laser.
    private Pokemon _content = null; // The Pokémon the Pokéball contains.
    private Pokemon _temporaryContent = null; // The Pokémon the Pokéball contains.
    private PokeballState _state = PokeballState.Lying;

    private Vector3 _destination;
    private Pokemon _target = null; // Used for auto-guidance of empty Pokéball thrown at wild Pokémons.
    private bool _frozen = false;
    private int _shakeLeft = -1;

    void Start()
    {
        _tr = this.transform;
        _rigid = GetComponent<Rigidbody>();
        _interactableItem = GetComponent<NVRInteractableItem>();

        if (_editorTest)
        {
            _state = PokeballState.Thrown;
        }

        if (_rigid == null)
        {
            Debug.LogError("No rigidbody attached to the pokeball");
            DestroyImmediate(this);
        }
    }

    void Update()
    {
        foreach (NVRHand hand in NVRPlayer.Instance.Hands)
        {
            if (hand.UseButtonPressed)
            {
                Recall(hand.transform.position);
            }
        }

        _verticesToDraw.Clear(); // A virer.



        // --- Any kind of Pokeball. ---
        if (_state != PokeballState.Held && _interactableItem.IsAttached)
        {
            _state = PokeballState.Held;
        }

        if (_state == PokeballState.Held && !_interactableItem.IsAttached)
        {
            _state = PokeballState.Thrown;
        }

        if (false) // Appuies bouton
        {
            Shrink();
        }

        // --- Empty Pokéball. ---
        if (_content == null)
        {
            if (_state == PokeballState.Thrown && !_shrinked)
            {
                CheckCapture();
            }

            if (_state == PokeballState.BouncingBack)
            {
                Vector3 distToDest = _destination - _tr.position;

                if (distToDest.magnitude > 0.05f)
                {
                    float speed = 4f;

                    _tr.position += distToDest * speed * Time.deltaTime;

                    Vector3 destinationAngle = _temporaryContent.transform.position - _tr.position;
                    destinationAngle.Normalize();
                    _tr.forward = Vector3.Slerp(_tr.forward, destinationAngle, speed * 2f * Time.deltaTime);
                }
                else
                {
                    _state = PokeballState.Swallowing;
                    Freeze(true);

                    // Do calls on the Pokemon.
                    _temporaryContent.Swallow(this);
                }
            }

            if (_state == PokeballState.Shaking)
            {
                if (_shakeLeft == -1 && _rigid.velocity.magnitude < 0.02f)
                {
                    _shakeLeft = Random.Range(4, 6);
                    //StopMove();
                    Invoke("ShakeOnce", 1f);
                }
            }
        }
        // --- Loaded Pokéball. ---
        else
        {

        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (_state == PokeballState.Thrown)
        {
            if (!_editorTest)
            {
                _state = PokeballState.Lying;
            }
        }
        else if (_state == PokeballState.Falling)
        {
            _state = PokeballState.Shaking;
        }
    }

    private void Recall(Vector3 pos)
    {
        // Stopping the current movement.
        _rigid.velocity = Vector3.zero;
        _rigid.angularVelocity = Vector3.zero;

        // New movement.
        var vectorDiff = pos - _tr.position;
        if (vectorDiff.magnitude > 0.1f)
        {
            _rigid.velocity = Vector3.Normalize(vectorDiff) * 5.0f;
        }
    }

    private bool CheckCapture()
    {
        // We check if the pokeball hit a pokemon.
        Vector3 pos = _tr.position;
        foreach (SphereCollider pk in Spawner.WildPokemons)
        {
            // To reduce the amount of calculation, we first check if the pokeball is close enough to the pokemon.
            Transform pkTr = pk.transform;
            float maxScale = Mathf.Max(pk.transform.lossyScale.x, pk.transform.lossyScale.y, pk.transform.lossyScale.z);
            if ((pos - (pkTr.TransformPoint(pk.center))).magnitude < pk.radius * maxScale)
            {
                //Debug.Break();
                SkinnedMeshRenderer[] renderers = pk.GetComponentsInChildren<SkinnedMeshRenderer>();

                // A pokemon is supposed to have one mesh. This script has been built on that hypothesis, so we better check.
                if (renderers.Length > 1f)
                {
                    Debug.LogError("More than one skin renderers found on " + pk.name + ":");

                    foreach (Renderer rend in renderers)
                    {
                        Debug.LogError("- " + rend.name);
                    }

                    Debug.Break();
                    DestroyImmediate(this);
                    return false;
                }
                else if (renderers.Length <= 0)
                {
                    Debug.LogError("No skin renderer found for " + pk.name + ":");

                    Debug.Break();
                    DestroyImmediate(this);
                    return false;
                }
                else if (renderers.Length == 1)
                {
                    Mesh mesh = new Mesh();
                    renderers[0].BakeMesh(mesh); // This may take a lot of time and shouldn't be called every frame! Especially at 90 FPS!

                    //Vector3 posBis = pos - pkTr.position; // This represents the position of the pokeball relative to the pokemon space (so we don't have to convert every vertex of the pokemon to world space).
                    foreach (Vector3 vertex in mesh.vertices)
                    {
                        // If a vertex of a pokemon in within the ball radius, then it is captured! Watch out though: a pokemon with wide space between its vertices (like big wings) may be missed by the pokeball.
                        Vector3 vertexWorldPos = pkTr.position + pkTr.rotation * vertex; // pkTr.TransformPoint(vertex); // 
                        _verticesToDraw.Add(vertexWorldPos); // A virer.
                        Vector3 distance = (pos - vertexWorldPos);
                        if (distance.magnitude < _pokeballCatchRadius)
                        {
                            // Pokémon touched! 
                            
                            Debug.Log(pk.name + " as been touched!");

                            Pokemon pkmn = pkTr.GetComponent<Pokemon>();
                            _destination = pkmn.Touched(this);

                            if (_destination == Vector3.zero)
                            {
                                Debug.Log("... but " + pk.name + " has already been captured.");
                                _state = PokeballState.Breaking;
                                return false;
                            }

                            _state = PokeballState.BouncingBack;
                            _destination += _tr.position;
                            Freeze(true);

                            _temporaryContent = pkmn;
                            
                            Spawner.WildPokemons.Remove(pk);

                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public void OnDrawGizmos()
    {
        /*if (_verticesToDraw == null)
            return;*/

        Gizmos.color = Color.yellow;
        foreach (Vector3 vert in _verticesToDraw)
        {
            Gizmos.DrawSphere(vert, 0.02f);
        }
    }

    public void Freeze(bool state_)
    {
        _frozen = state_;
        _rigid.isKinematic = _frozen;

        if (_frozen)
        {
            StopMove();
        }
    }

    public void StopMove()
    {
        _rigid.velocity = Vector3.zero;
        _rigid.angularVelocity = Vector3.zero;
    }

    public void EndSwallow()
    {
        _state = PokeballState.Falling;

        Freeze(false);
    }

    private void ShakeOnce()
    {
        _shakeLeft--;

        if (_shakeLeft > 0)
        {
            // For now Pokéballs are Masterballs.

            Vector3 force = Random.onUnitSphere * 0.3f;
            force.y = 0;

            Freeze(false);
            StopMove();
            _rigid.AddForce(force, ForceMode.VelocityChange);

            StartCoroutine("BalanceShakeForce", -force);
            Invoke("ShakeOnce", Random.Range(1.3f, 2f) + (_shakeLeft == 1 ? 1.2f : 0f));
        }
        else
        {
            // The capture is a success!

            _state = PokeballState.Lying;

            _shakeLeft = -1;

            _content = _temporaryContent;
            _temporaryContent = null;
            StopMove();

            Instantiate(Resources.Load("Particles/CaptureParticle"), _tr.position, Quaternion.identity);
        }
    }

    IEnumerator BalanceShakeForce(Vector3 force_)
    {
        yield return new WaitForSeconds(0.3f);

        StopMove();
        _rigid.AddForce(force_, ForceMode.VelocityChange);

        yield return new WaitForSeconds(0.3f);
        //StopMove();
    }

    private void Shrink()
    {
        _shrinked = !_shrinked;
    }
}
