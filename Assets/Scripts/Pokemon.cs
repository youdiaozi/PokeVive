using UnityEngine;
using System.Collections;


/// List of required animations per Pokemon (minimum 5):
/// - [OPTIONAL BUT NICE] taunt (at the beginning of the battle for a wild pokemon, and when thrown out of the pokeball for a domesticated pokemon in good shape).
/// - [MANDATORY] idle loop when in good shape. [100-51%] HP. There will be no HP bar, so the idle animation is key to understanding the shape of your pokemon. There are 3 HP bar colors in Pokemon, so I suggest at least 3 animations. With 2 bars we can determine OHKO, 2HKO, 3H+KO.
/// - idle loop when in bad shape. [50%-20%] HP. Note: we can create this animation by blending "good shape" and "severely injured" within Unity.
/// - [MANDATORY]idle loop when severely injured. [19%-0%[. Note: at this point the Pokemon can't move to avoid attacks, but he can still move to attack.
/// - [MANDATORY]strafe/jump left to avoid incoming vertical attacks. Distance ~= 1.5m.
/// - strafe/jump right to avoid incoming vertical attacks. Distance ~= 1.5m.
/// - [OPTIONAL] jump up to avoid horizontal incoming attacks. Animation in 2 parts: jump up then fall down. Unused for birds and levitating pokemons.
/// - [OPTIONAL] block? may be used to tank an attack and take reduced damage, but with a faster counter-attack delay.
/// - [MANDATORY]run forward loop (used to go back to the battle spot after having been ejected, for wild pokemons to run away, to run to the opponent before attacking him, etc.)
/// - take damage (from idle position, so at least 1 anim, ideally 3 animations).
/// - [OPTIONAL] take damage and fall down from jumping up position.
/// - [OPTIONAL] take damage from blocking position.
/// - [MANDATORY]ko (from idle position, so at least 1 anim, ideally 3 animations).
/// 
/// Situational (from 2 to 3 attacks per Pokémon):
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
    BeingReleased,
    TurnsIntoRedEnergy, // Period between the moment the Pokémon is touched by the laser of its Pokeball and the moment it starts being swallowed under red energy form.
    LaserSwallowed, // When the Pokemon has already been captured and is getting recalled by its pokeball.
    Taunting
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
    private MeshRenderer _hologram = null;
    private MeshFilter _hologramMeshFilter = null;

    private float _redEnergyConversionTime = 0f;
    private float _redEnergyConversionTotalTime = 1f;
    private MeshCollider _meshCollider;

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

        GameObject go = new GameObject();
        go.name = "SkinnedMeshCollider";
        go.transform.SetParent(_tr);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = new Vector3(1f / _tr.localScale.x, 1f / _tr.localScale.y, 1f / _tr.localScale.z);

        _meshCollider = go.AddComponent<MeshCollider>();
        _meshCollider.sharedMesh = new Mesh();
    }
	
	void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            CreateHologram();
        }

        if (_state == PokemonState.TurnsIntoRedEnergy)
        {
            _redEnergyConversionTime += Time.deltaTime;

            float percentage = _redEnergyConversionTime / _redEnergyConversionTotalTime;

            if (percentage < 1f)
            {
                foreach (Material mat in _hologram.materials)
                {
                    float inversedPercentage = 1f - percentage;
                    mat.color = new Color(1f, inversedPercentage, inversedPercentage, 1f);
                }
            }
            else
            {
                _state = PokemonState.LaserSwallowed;
                _homeBall.Recall();
                //_hologram.gameObject.BroadcastMessage("StartShrinking", _tr);
                _hologram.GetComponent<MeshReductor>().StartShrinking(_homeBall.transform);
            }
        }
        else if (_state == PokemonState.Swallowed)
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
                _state = PokemonState.Idle;
                _anim.SetTrigger("TauntTrigger");
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

        _skinnedMesh.enabled = true;
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

    public void TurnIntoRedEnergy(bool status_)
    {
        _redEnergyConversionTime = 0f;

        if (status_ == true)
        {
            _state = PokemonState.TurnsIntoRedEnergy;

            _anim.speed = 0f;
            CreateHologram(); // Also enables the MeshRenderer that is _hologram.

            if (_skinnedMesh != null)
            {
                _skinnedMesh.enabled = false;
            }

            _recallingParticle.Play();
        }
        else
        {
            _state = PokemonState.Idle;

            _anim.speed = 1f;
            if (_hologram != null)
            {
                _hologram.enabled = false;
            }

            if (_skinnedMesh != null)
            {
                _skinnedMesh.enabled = true;
            }

            _recallingParticle.Stop();
        }
    }

    void CreateHologram()
    {
        if (_hologram == null)
        {
            GameObject go = new GameObject();
            go.name = "Hologram " + this.name;
            go.transform.SetParent(Hub.hologramsContainer);

            _hologramMeshFilter = go.AddComponent<MeshFilter>();
            _hologram = go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshReductor>();

            _hologram.materials = (Material[])_skinnedMesh.materials.Clone();

            Shader shader = Shader.Find("Transparent/Diffuse ZWrite");
            foreach (Material mat in _hologram.materials)
            {
                mat.shader = shader;
            }
        }

        if (_hologram != null)
        {
            _hologram.enabled = true;

            if (_hologramMeshFilter != null)
            {
                _skinnedMesh.BakeMesh(_hologramMeshFilter.mesh);
            }
            else
            {
                Debug.LogError("Hologram mesh filter not found for " + this.name);
            }

            _hologram.transform.position = _tr.position;
            _hologram.transform.rotation = _tr.rotation;

            foreach (Material mat in _hologram.materials)
            {
                mat.color = Color.white;
            }
        }
        else
        {
            Debug.LogError("Failed to create hologram for " + this.name);
        }
    }

    public void StoreInPokeball()
    {
        _state = PokemonState.StoredInPokeball;
        _recallingParticle.Stop();
    }

    public float GetRealHeight()
    {
        return _baseBoundingBox.y;
    }

    public void UpdateMeshCollider()
    {
        _skinnedMesh.BakeMesh(_meshCollider.sharedMesh);
    }
}
