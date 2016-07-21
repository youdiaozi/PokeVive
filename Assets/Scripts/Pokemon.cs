using UnityEngine;
using System.Collections;


/// List of required animations per Pokemon:
/// - [OPTIONAL BUT NICE] taunt (at the beginning of the battle for a wild pokemon, and when thrown out of the pokeball for a domesticated pokemon in good shape).
/// - idle loop when in good shape. [100-51%] HP. There will be no HP bar, so the idle animation is key to understanding the shape of your pokemon. There are 3 HP bar colors in Pokemon, so I suggest at least 3 animations. With 2 bars we can determine OHKO, 2HKO, 3H+KO.
/// - idle loop when in bad shape. [50%-20%] HP. Note: we can create this animation by blending "good shape" and "severely injured" within Unity.
/// - idle loop when severely injured. [19%-0%[. Note: at this point the Pokemon can't move to avoid attacks, but he can still move to attack.
/// - strafe/jump left to avoid incoming vertical attacks. Distance ~= 1.5m.
/// - strafe/jump right to avoid incoming vertical attacks. Distance ~= 1.5m.
/// - [OPTIONAL] jump up to avoid horizontal incoming attacks. Animation in 2 parts: jump up then fall down. Unused for birds and levitating pokemons.
/// - [OPTIONAL] block? may be used to tank an attack and take reduced damage, but with a faster counter-attack delay.
/// - run forward loop (used to go back to the battle spot after having been ejected, for wild pokemons to run away, to run to the opponent before attacking him, etc.)
/// - take damage (from idle position, so at least 1 anim, ideally 3 animations).
/// - [OPTIONAL] take damage and fall down from jumping up position.
/// - [OPTIONAL] take damage from blocking position.
/// - ko (from idle position, so at least 1 anim, ideally 3 animations).
/// 
/// Situational (up to 3 attacks per Pokémon):
/// - breath (fire/water/etc.) (from idle position)
/// - attack with pawn claws
/// - attack with feet
/// - tail hit
/// - high kick balayette rotatif
/// - etc.
/// 

public enum PokemonState
{
    ERROR,
    Idle,
    Roaming,
    Swallowed,
    TryingToEscapeBall,
    StoredInPokeball,
    BeingReleased
};
public class Pokemon : MonoBehaviour
{
    public string _name { get; private set; }
    public Deformation _deformation;
    public ParticleSystem _spawningParticle;
    public ParticleSystem _recallingParticle;
    public SkinnedMeshRenderer _skinnedMesh;

    private const float _range = 5f;
    private Vector3 _dest;
    private Transform _tr;
    private NavMeshAgent _nav;
    private Animator _anim;
    private Renderer _rend;
    private Vector3 _baseLocalScale;
    private Vector3 _baseBoundingBox;

    private float _waitTime = 0f;

    private PokemonState _state = PokemonState.Roaming;

    private bool _isBeingCaptured = false;
    private bool _isCaptured = false;
    private float _shrinkingRatio; // Shrinking ratio to fit inside a Pokeball.
    private Vector3 _pathToBall;
    private float _captureTime = -1f;

    private Color _emissionTargetColor = Color.black;
    private Color _emissionOriginalColor = Color.black;

    private Pokeball _homeBall = null;

    void Start()
    {
        _anim = GetComponent<Animator>();
        _nav = GetComponent<NavMeshAgent>();
        _tr = this.transform;
        _name = _tr.name.Replace("(Clone)", "");

        _baseLocalScale = _tr.localScale;
        _rend = this.GetComponentInChildren<Renderer>();

        foreach (Material mat in _rend.materials)
        {
            mat.EnableKeyword("_EMISSION");
        }

        _baseBoundingBox = _rend.bounds.size;
    }
	
	void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            GameObject go = new GameObject();
            go.name = "SkinnedMesh snapshot " + this.name;
            go.transform.position = Vector3.one * 10f;

            MeshFilter meshFilter = go.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshReductor>();

            _skinnedMesh.BakeMesh(meshFilter.mesh);
            meshRenderer.materials = _skinnedMesh.materials;

            Shader shader = Shader.Find("Transparent/Diffuse ZWrite");
            foreach (Material mat in meshRenderer.materials)
            {
                mat.shader = shader;
            }
        }

        if (_state == PokemonState.Swallowed)
        {
            float timeLeft = 1f - Mathf.Clamp01(Time.time - _captureTime); // Goes from 1 to 0.

            if (timeLeft <= 0f)
            {
                _state = PokemonState.TryingToEscapeBall; // This should actually start when the pokeball hits the ground.

                _homeBall.EndSwallow();
                this.gameObject.SetActive(false);
            }
            else
            {
                float scaleRatio = Mathf.Pow(timeLeft * (1 - _shrinkingRatio), 2f) + _shrinkingRatio;
                _tr.localScale = _baseLocalScale * scaleRatio;

                float threshold = 0.9f;
                if (timeLeft < threshold)
                {
                    _tr.position = _homeBall.transform.position + _pathToBall * timeLeft * (1f / threshold);
                }
            }

            return;
        }
        else if (_state == PokemonState.BeingReleased)
        {
            if (_tr.localScale != _baseLocalScale)
            {
                _tr.localScale += _baseLocalScale * 6f * Time.deltaTime;

                if (_tr.localScale.magnitude > _baseLocalScale.magnitude)
                {
                    _tr.localScale = _baseLocalScale;
                }
            }

            if (_deformation.DeformationHasEnded() && !_spawningParticle.isPlaying)
            {
                _emissionOriginalColor = Color.white;
                _emissionTargetColor = Color.black;
            }
        }
        else if (_state == PokemonState.Roaming)
        {
            _anim.SetFloat("Speed", _nav.velocity.magnitude);
            _waitTime -= Time.deltaTime;

            if (_nav.remainingDistance < 0.1f && _waitTime <= 0f)
            {
                if (Random.value < 0.4f)
                {
                    _waitTime = Random.Range(1f, 3f);
                }
                else
                {
                    _nav.SetDestination(new Vector3(Random.Range(-_range, _range), 0f, Random.Range(-_range, _range)));
                }
            }
        }

        Color _currentEmissionColor = _rend.material.GetColor("_EmissionColor");
        if (_emissionTargetColor != _currentEmissionColor) // We assume all the materials are at the same emission level and none of the materials use this property.
        {
            foreach (Material mat in _rend.materials)
            {
                Color newEmission = _currentEmissionColor + (_emissionTargetColor - _emissionOriginalColor) * 1.5f * Time.deltaTime;

                // If we went passed the target emission, we set it right.
                if (Mathf.Clamp01(newEmission.r) != newEmission.r)
                {
                    newEmission = _emissionTargetColor;
                }

                mat.SetColor("_EmissionColor", newEmission); // 0.5 second transition.
            }
        }
	}

    public Vector3 Touched(Pokeball ball)
    {
        // Returns the position the Pokeball should bounce back at.

        Debug.Log("Touched method called.");

        if (_homeBall != null)
        {
            // You can't capture someone else's Pokémon!
            return Vector3.zero;
        }

        _homeBall = ball;

        // Calculating the bounce destination. A ajuster.
        Vector3 destination = ball.transform.position - _tr.position;
        destination.y = 0;
        //destination.Normalize();

        Bounds bounds = _rend.bounds;
        destination.y = ball.transform.position.y + Mathf.Max(0.6f, bounds.extents.y); // Rajouter une telle hauteur va faire traverser les Pokémons volants au dessus du joueur à la Pokéball.

        _emissionOriginalColor = _rend.material.GetColor("_EmissionColor");
        _emissionTargetColor = Color.white;

        return destination;       
    }

    public void Swallow(Pokeball ball)
    {
        _state = PokemonState.Swallowed;

        _captureTime = Time.time;
        _anim.speed = 0f; // Freezes the Pokemon.
        _nav.enabled = false;

        // Calculating the shrinking ratio for the Pokémon to fit within the Pokéball.
        Bounds bounds = _rend.bounds;
        float diagonal = bounds.size.magnitude; // La magnitude de la taille de la bounding box représente la diagonale de cette boite et est un bon indicateur du volume du personne.
        _shrinkingRatio = Mathf.Sqrt(0.018252f) / diagonal; // Attention aux divisions par 0 ! Racine de 0.018252 est la diagonale de la bounding box de la pokeball.

        _pathToBall = _tr.position - ball.transform.position;
    }

    public void ReleaseFromPokeball()
    {
        _state = PokemonState.BeingReleased;

        _anim.speed = 1f;
        _nav.enabled = true;

        _rend.material.SetColor("_EmissionColor", Color.white);
        _emissionOriginalColor = Color.white;
        _emissionTargetColor = Color.white;

        if (_deformation != null)
        {
            if (_deformation._animator == null)
            {
                _deformation._animator = _anim;
            }

            _deformation.LaunchDeformation();
        }

        if (_spawningParticle != null)
        {
            if (!_spawningParticle.isStopped)
            {
                _spawningParticle.Stop();
            }

            //_spawningParticle.Play();
        }
    }

    public void StoreInPokeball()
    {
        _state = PokemonState.StoredInPokeball;
    }

    public float GetRealHeight()
    {
        return _baseBoundingBox.y;
    }
}
