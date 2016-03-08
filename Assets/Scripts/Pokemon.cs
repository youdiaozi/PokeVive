using UnityEngine;
using System.Collections;

public enum PokemonState
{
    ERROR,
    Idle,
    Roaming,
    Swallowed,
    TryingToEscapeBall
};
public class Pokemon : MonoBehaviour
{
    private const float _range = 5f;
    private Vector3 _dest;
    private Transform _tr;
    private NavMeshAgent _nav;
    private Animator _anim;
    private Renderer _rend;
    private Vector3 _baseLocalScale;

    private float _waitTime = 0f;

    private PokemonState _state = PokemonState.Roaming;

    private bool _isBeingCaptured = false;
    private bool _isCaptured = false;
    private float _shrinkingRatio; // Shrinking ratio to fit inside a Pokeball.
    private Vector3 _pathToBall;
    private float _captureTime = -1f;

    private Pokeball _homeBall = null;

    void Start()
    {
        _anim = GetComponent<Animator>();
        _nav = GetComponent<NavMeshAgent>();
        _tr = this.transform;
        _baseLocalScale = _tr.localScale;
        _rend = this.GetComponentInChildren<Renderer>();
    }
	
	void Update()
    {
        if (_state == PokemonState.Swallowed)
        {
            float timeLeft = 1f - Mathf.Clamp01(Time.time - _captureTime); // Goes from 1 to 0.

            if (timeLeft <= 0f)
            {
                _state = PokemonState.TryingToEscapeBall;

                _homeBall.EndSwallow();
                this.gameObject.SetActive(false);
            }
            else
            {
                float scaleRatio = Mathf.Pow(timeLeft * (1 - _shrinkingRatio), 3f) + _shrinkingRatio;
                _tr.localScale = _baseLocalScale * scaleRatio;

                float threshold = 0.9f;
                if (timeLeft < threshold)
                {
                    _tr.position = _homeBall.transform.position + _pathToBall * timeLeft * (1f / threshold);
                }
            }

            return;
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
	}

    public Vector3 Touched(Pokeball ball)
    {
        // Returns the position the Pokeball bounce back at.

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
        destination.y = ball.transform.position.y + Mathf.Max(1f, bounds.extents.y); // Rajouter une telle hauteur va faire traverser les Pokémons volants au dessus du joueur à la Pokéball.

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
        _shrinkingRatio = Mathf.Sqrt(0.03f) / diagonal; // Attention aux divisions par 0 ! Racine de 300 est la diagonale de la bounding box de la pokeball.

        _pathToBall = _tr.position - ball.transform.position;
    }
}
