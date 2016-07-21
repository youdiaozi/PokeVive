using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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
    Opening, // The Pokeball is locked into the air and opens which releases its capture ray.
    Swallowing, // The Pokemon is being dragged into the pokeball.
    Closing, // The pokeball closes before falling down.
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
    public GrabbableObj _grabbableObj;
    public TextMeshPro _pkmnName;
    public LaserEmitter _laser;

    private const float _pokeballCatchRadius = 0.1f; // The real pokeball radius is 0.05.

    private Transform _tr;
    private Rigidbody _rigid;
    private Animator _animator;
    private List<Vector3> _verticesToDraw = new List<Vector3>(); // A virer.

    private bool _shrinked = false;
    private bool _isRecalling = false; // True when the Pokéball is trying to call back its Pokémon using its red laser.
    private bool _isPokemonInside = false; // False when the Pokemon contains a Pokemon but the pkmn is out of the pokeball, for a battle for instance.
    private Pokemon _content = null; // The Pokémon the Pokéball contains.
    private Pokemon _temporaryContent = null; // The Pokémon the Pokéball contains.
    private PokeballState _state = PokeballState.Lying;

    private Vector3 _destination;
    private Pokemon _target = null; // Used for auto-guidance of empty Pokéball thrown at wild Pokémons.
    private bool _frozen = false;
    private int _shakeLeft = -1;
    private int _missedShots = 0;

    private Vector3 _backingOrigin;
    private Vector3 _backingDestination;
    private float _backingDuration;
    private float _backingTotalDuration = 1.5f;
    private bool _isBacking = false;
    private bool _hasInvokedStopBacking = false;
    private bool _isLaserActive = false;

    void Start()
    {
        _tr = this.transform;
        _rigid = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();

        if (_editorTest)
        {
            _state = PokeballState.Thrown;
        }

        if (_rigid == null)
        {
            Debug.LogError("No rigidbody attached to the pokeball");
            DestroyImmediate(this);
        }

        if (_animator == null)
        {
            Debug.LogError("No animator attached to the pokeball");
            DestroyImmediate(this);
        }

        if (_content == null)
        {
            _pkmnName.text = "";
        }

        DisactivateRecallingLaser();

        _grabbableObj.Grabbed += OnGrabbed;

        _tr.position = Camera.main.transform.position + Camera.main.transform.forward * 0.4f + Vector3.up * 0.4f;
    }

    void Update()
    {
        _verticesToDraw.Clear(); // A virer.

        //if (_state == PokeballState.Lying && Hub.rightHand.index != SteamVR_TrackedObject.EIndex.None)
        //{
        //    SteamVR_Controller.Device device = SteamVR_Controller.Input((int)Hub.rightHand.index);
        //    if (device.GetPress(SteamVR_Controller.ButtonMask.ApplicationMenu))
        //    {
        //        BackToHand(Hub.rightHand.transform.position);
        //    }
        //}

        // --- Any kind of Pokeball. ---

        if (_state == PokeballState.Lying
            || _state == PokeballState.Held
            || _state == PokeballState.Thrown
            || _state == PokeballState.Backing)
        {
            // Note: as this is not set exactly when the state changes, there might be a 1 frame shifting that could be exploited by players.
            _grabbableObj.SetGrabbable(true);
        }
        else
        {
            _grabbableObj.SetGrabbable(false);
        }

        if (_state == PokeballState.Held && !_isLaserActive && _content != null && !_isPokemonInside && Hub.playerHeight > 0f)
        {
            // When the Pokeball is held, it has to face the right direction so the player can see the button.

            if (ShouldLaserTrigger())
            {
                // If the player extends his arm towards its battling Pokemon, while at pointing the front of the pokeball at it, and while looking at it, it triggers the recalling laser of the Pokeball.

                _isLaserActive = true;
                ActivateRecallingLaser(); 
            }
        }

        if (_isLaserActive)
        {
            if (!ShouldLaserTrigger())
            {
                _isLaserActive = false;
                DisactivateRecallingLaser(); 
            }
        }

        if (_state == PokeballState.Held && !_grabbableObj.IsGrabbed())
        {
            /// Here we have to determine a target in order to make the assisted guidance work.
            /// I say we do the following: we know the angle, and the force of the ball. Therefore, we can predict where it's going to fall.
            /// If the pokeball will fall within a reasonable range of a wild pokemon, then we set it as the target.
            /// Otherwise, there will be no auto guidance.
            /// This is made so to catch a pokemon, you'll still have to aim well, but not perfectly. 

            if (Spawner.WildPokemons.Count == 1)
            {
                _target = Spawner.WildPokemons[0].GetComponent<Pokemon>();
            }

            _state = PokeballState.Thrown;
        }

        if (false) // Button pushed.
        {
            Shrink();
        }

        if (_content == null && _state == PokeballState.Thrown)
        {
            if (_target != null)
            {
                // Assisted guidance of the pokeball so it's easier to aim for one.

                Vector3 vel = _rigid.velocity;
                Vector3 distanceToTarget = (_target.transform.position + Vector3.up * _target.GetRealHeight()) - _tr.position;
                Vector3 towardsTarget = distanceToTarget.normalized * vel.magnitude;

                Vector3 adjustedVelocity = Vector3.Lerp(vel, towardsTarget, Mathf.Min(0.03f, 1f / (20f * Mathf.Pow(distanceToTarget.magnitude, 3f))));
                _rigid.velocity = adjustedVelocity;
            }
        }
        else if (_state == PokeballState.BouncingBack)
        {
            Vector3 distToDest = _destination - _tr.position;

            if (distToDest.magnitude > 0.01f)
            {
                float speed = 4f;

                _tr.position += distToDest * speed * Time.deltaTime;

                Pokemon targetPkmn = _content;
                if (targetPkmn == null)
                {
                    targetPkmn = _temporaryContent;
                }

                Vector3 destinationAngle = targetPkmn.transform.position - _tr.position;
                destinationAngle.Normalize();
                _tr.forward = Vector3.Slerp(_tr.forward, destinationAngle, speed * 2f * Time.deltaTime);
            }
            else
            {
                if (_content == null)
                {
                    _state = PokeballState.Opening;
                    Freeze(true);

                    _animator.SetBool("Open", true);
                }
                else if (_isPokemonInside) // Double.
                {
                    _state = PokeballState.Opening;
                    Freeze(true);

                    _animator.SetBool("Open", true);
                }
            }
        }

        // --- Empty Pokéball. ---
        if (_content == null)
        {
            if (_state == PokeballState.Thrown && !_shrinked)
            {
                CheckCapture();
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

        if (_isBacking)
        {
            BackToHand();
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (_state == PokeballState.Thrown)
        {
            if (!_editorTest)
            {
                if (_content != null && _isPokemonInside && collision.contacts[0].point.y < 0.05f)
                {
                    _content.gameObject.SetActive(true);

                    _destination = _tr.position + Camera.main.transform.right + Vector3.up * Mathf.Max(1f, _content.GetRealHeight() * 1.1f);
                    _content.transform.position = _tr.position;
                    _content.transform.localScale = Vector3.zero;

                    Vector3 forward = _tr.position - Camera.main.transform.position;
                    forward.y = 0f;
                    forward.Normalize();

                    _content.transform.LookAt(_tr.position + forward, Vector3.up);

                    Freeze(true);
                    _state = PokeballState.BouncingBack;
                }
                /*else if (_content == null) // A virer ? 
                {
                    /// Thoughts:
                    /// Making an empty pokeball go back to the trainer after a missed throw allows the player to use it several times until it breaks. 
                    /// This behaviour doesn't exist neither in the anime nor in the video games.
                    /// It the pokeball doesn't come back, the player can't use it again during this battle as the pokeball will be close to the wild pokemon.
                    /// He could pick it up after the battle to refill its inventory, which is great!
                    /// Throwing the pokeball back at him might be over powered and makes no sense. So actually, we'll get rid of it after the test phase :)

                    _missedShots++;

                    if (_missedShots > 2)
                    {
                        _state = PokeballState.Breaking;
                    }
                    else
                    {
                        StartBacking();
                    }
                }*/
                else
                {
                    _state = PokeballState.Lying;
                }
            }
        }
        else if (_state == PokeballState.Falling)
        {
            _state = PokeballState.Shaking;
        }
    }

    private void BackToHand()
    {
        Vector3 dest = _backingDestination;

        // Stopping the current movement.
        _rigid.velocity = Vector3.zero;
        _rigid.angularVelocity = Vector3.zero;

        // New movement.
        Vector3 vectorDiff = dest - _tr.position;

        _backingDuration += Time.deltaTime;
        float travelledPercentage = _backingDuration / _backingTotalDuration;

        _tr.position = Vector3.Slerp(_backingOrigin, dest, Mathf.Pow(travelledPercentage, 0.75f));
        //_tr.position += vectorDiff.normalized * Time.deltaTime * Mathf.Clamp(vectorDiff.magnitude * vectorDiff.magnitude, 5f, 20f);

        // We curve the travel of the ball.
        //Vector3 totalDistance = (dest - _backingOrigin);
        //Vector2 totalDistanceFromTop = new Vector2(totalDistance.x, totalDistance.z);

        //Vector3 travelledDistance = vectorDiff;
        //Vector2 travelledDistanceFromTop = new Vector2(travelledDistance.x, travelledDistance.z);

        //travelledPercentage = travelledDistanceFromTop.magnitude / totalDistanceFromTop.magnitude;
        //float distanceToHalfWay = Mathf.Abs(travelledPercentage - 0.5f);
        //float ratio = distanceToHalfWay * 2f;

        //_tr.position += Vector3.up * ratio;

        // If we went past the destination point, that means the back travelling has ended.
        Vector3 newVectorDiff = dest - _tr.position;
        if (Vector3.Dot((_backingDestination - _backingOrigin), newVectorDiff) < 0f
            || vectorDiff.magnitude < 0.01f
            || _backingDuration > _backingTotalDuration)
        {
            if (!_hasInvokedStopBacking)
            {
                _tr.position = dest;
                _hasInvokedStopBacking = true;
                Invoke("StopBacking", 0.5f);
            }
        }

        //_rigid.velocity = Vector3.Normalize(vectorDiff) * 5.0f;
    }

    void StopBacking()
    {
        _hasInvokedStopBacking = false;

        if (_isBacking)
        {
            Freeze(false);
            _isBacking = false;
            _state = PokeballState.Lying;
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
                            _target = null;

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
        _state = PokeballState.Closing;
        _animator.SetBool("Open", false);
    }

    private void ShakeOnce()
    {
        if (_state != PokeballState.Shaking)
        {
            return;
        }

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
            float delay = Random.Range(1.3f, 2f) + (_shakeLeft == 1 ? 1.2f : 0f);

            //if (_editorTest)
            {
                delay /= 5f;
            }

            Invoke("ShakeOnce", delay);
        }
        else
        {
            // The capture is a success!

            _state = PokeballState.Lying;
            _isPokemonInside = true;

            _shakeLeft = -1;

            _content = _temporaryContent;
            _content.StoreInPokeball();
            _pkmnName.text = _content._name;

            _temporaryContent = null;
            StopMove();

            Instantiate(Resources.Load("Particles/CaptureParticle"), _tr.position, Quaternion.identity);

            //StartBacking();
            StartCoroutine(StartBackingWithDelayCoroutine(2.25f));
        }
    }

    IEnumerator BalanceShakeForce(Vector3 force_)
    {
        yield return new WaitForSeconds(0.3f);

        if (_state != PokeballState.Shaking)
        {
            yield break;
        }

        StopMove();
        _rigid.AddForce(force_, ForceMode.VelocityChange);

        yield return new WaitForSeconds(0.3f);
        //StopMove();
    }

    private void Shrink()
    {
        _shrinked = !_shrinked;
    }

    private void OpeningDone()
    {
        if (_state == PokeballState.Opening)
        {
            if (_content == null)
            {
                // Is called by the animation when the pokeball is fully opened.
                _state = PokeballState.Swallowing;

                // Do calls on the Pokemon.
                _temporaryContent.Swallow(this);
            }
            else if (_isPokemonInside)
            {
                _state = PokeballState.Releasing; // This phase is actually usuless, it's just a delay so that the pokeball stays mid air for a short time. It's not synchronized with the pokemon appearance animation.

                _isPokemonInside = false;

                // Do calls on the Pokemon.
                _content.ReleaseFromPokeball();

                Invoke("Close", 0.3f);                
            }
        }
    }

    private void Close()
    {
        _state = PokeballState.Closing;
        _animator.SetBool("Open", false);
    }

    private void ClosingDone()
    {
        // Is called by the animation when the pokeball is fully opened.

        if (_state == PokeballState.Closing)
        {
            // After swallowing a Pokemon, the Pokeball falls down.
            if (_content == null)
            {
                _state = PokeballState.Falling;

                Freeze(false);
            }
            else
            {
                // The ball goes back to the player's hand after releasing the Pokemon it contains.
                StartCoroutine(StartBackingWithDelayCoroutine(0.5f));

                //Freeze(false);
            }
        }
    }

    IEnumerator StartBackingWithDelayCoroutine(float delay_, Vector3? dest_ = null)
    {
        yield return new WaitForSeconds(delay_);

        if (_state == PokeballState.Lying || _state == PokeballState.Closing)
        {
            StartBacking(dest_);
        }
    }

    public void StartBacking(Vector3? dest_ = null)
    {
        if (_state == PokeballState.Lying
            || (_content == null && _state == PokeballState.Thrown)
            || (_content != null && _state == PokeballState.Closing))
        {
            //if ((_tr.position - Camera.main.transform.position).magnitude > 1f) // We allow the pokeball to back only if it's far from the player. Otherwise it would look bad. Anyway if the pokeball is close by the player, he better go pick it up by himself!
            {
                _state = PokeballState.Backing;
                _backingOrigin = _tr.position;

                if (dest_ != null)
                {
                    _backingDestination = (Vector3)dest_;
                }
                else
                {
                    if (false && (Hub.rightHand != null || Hub.leftHand != null))
                    {
                        Transform hand = (Hub.rightHand != null ? Hub.rightHand.transform : Hub.leftHand.transform);
                        _backingDestination = hand.position;
                        _backingDestination += (_backingOrigin - hand.position).normalized * 0.3f;
                        _backingDestination.y = Camera.main.transform.position.y * 0.8f;
                    }
                    else // If no hand is detected, we'll use the head as a target instead. Actually this formula may be better than the previous one!
                    {
                        Transform hand = Camera.main.transform;
                        _backingDestination = hand.position;
                        _backingDestination += (_backingOrigin - hand.position).normalized * 0.3f;
                        _backingDestination += hand.right * 0.3f;
                        _backingDestination.y = Camera.main.transform.position.y * 0.8f;
                    }
                }


                // We want the speed of the pokeball to go slower when it's closer and we'd like to keep an average back time close to 1 second when the pokeball is far away. However, to look realistic, we set a max speed to the pokeball.
                Vector3 travel = _tr.position - _backingDestination;
                float pokeballMaxSpeed = 20f;
                float backingSpeed = 3f;
                float backingDesiredDuration = 1f;
                _backingTotalDuration = travel.magnitude / backingSpeed;

                if (_backingTotalDuration > backingDesiredDuration)
                {
                    _backingTotalDuration = Mathf.Lerp(_backingTotalDuration, backingDesiredDuration, 0.85f);
                }

                if (travel.magnitude / _backingTotalDuration > pokeballMaxSpeed) // If the pokeball speed is higher than its max speed, we cap it to its max speed, so the travel of the pokeball looks credible.
                {
                    _backingTotalDuration = travel.magnitude / pokeballMaxSpeed;
                }

                _backingDuration = 0f;
                _isBacking = true;
            }
        }
    }

    private void OnGrabbed()
    {
        _state = PokeballState.Held;
        _isBacking = false;
    }

    private bool ShouldLaserTrigger()
    {
        if (_state != PokeballState.Held)
        {
            return false;
        }

        Vector3 headToBall = _tr.position - Camera.main.transform.position;
        Vector3 headToPokemon = _content.transform.position + Vector3.up * _content.GetRealHeight() / 2f - Camera.main.transform.position;
        Vector3 ballToPokemon = _content.transform.position + Vector3.up * _content.GetRealHeight() / 2f - _tr.position;
        Vector3 ballForward = _tr.forward;

        headToBall.Normalize();
        headToPokemon.Normalize();
        ballToPokemon.Normalize();
        ballForward.Normalize();

        if (_isLaserActive)
        {
            if (headToBall.magnitude > 0.34f * Hub.playerHeight // This forces the player stretch his arm.
                    &&  Vector3.Dot(headToBall, headToPokemon) > 0.1f // This force the player to put his arm between his head at the target so he can't put the laser in his own face to see the glitches.
                    && Vector3.Dot(ballForward, ballToPokemon) > 0.95f) // This force the player to make the Pokéball face the target.
            {
                return true;
            }
        }
        else
        {
            if (headToBall.magnitude > 0.38f * Hub.playerHeight
                    &&  Vector3.Dot(headToBall, headToPokemon) > 0.2f
                    && Vector3.Dot(ballForward, ballToPokemon) > 0.975f)
            {
                return true;
            }
        }

        return false;
    }

    private void ActivateRecallingLaser()
    {
        _laser.Play(this);
    }

    private void DisactivateRecallingLaser()
    {
        _laser.Stop();
    }

    public bool IsMyPokemon(Transform tr)
    {
        if (_content == null)
        {
            return false;
        }

        return (_content.transform.GetInstanceID() == tr.GetInstanceID());
    }

    public void Recall()
    {
        // This is a temporary version, à virer. A améliorer pour être précis.

        _content.gameObject.SetActive(false);
        _isPokemonInside = true;
        _content.StoreInPokeball();
    }
}
