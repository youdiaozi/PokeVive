using UnityEngine;
using System.Collections;

public class PokemonWalk : MonoBehaviour
{
    private const float _range = 5f;
    private Vector3 _dest;
    private Transform _tr;
    private NavMeshAgent _nav;
    private Animator _anim;
    private Renderer _rend;
    private Vector3 _baseLocalScale;

    private float _waitTime = 0f;

    private bool _isBeingCaptured = false;
    private bool _isCaptured = false;
    private float _shrinkingRatio; // Shrinking ratio to fit inside a Pokeball.
    private float _captureTime = -1f;

    private Transform _homeBall = null;

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
        if (_isCaptured)
        {
            return;
        }

        if (_isBeingCaptured)
        {
            float timeLeft = 1f - Mathf.Clamp01(Time.time - _captureTime); // Goes from 1 to 0.
            float scaleRatio = timeLeft * (1 - _shrinkingRatio) + _shrinkingRatio;
            _tr.localScale = _baseLocalScale * scaleRatio;

            //_tr.position.

            if (timeLeft <= 0f)
            {
                _isBeingCaptured = false;
                _isCaptured = true;
            }

            return;
        }

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

    private void Captured(CatchPokeball ball)
    {
        Debug.Log("Captured method called.");
        _isBeingCaptured = true;

        _homeBall = ball.transform;
        _captureTime = Time.time;

        _anim.speed = 0f;
        _nav.enabled = false;

        Bounds bounds = _rend.bounds;
        float diagonal = bounds.size.magnitude; // La magnitude de la taille de la bounding box représente la diagonale de cette boite et est un bon indicateur du volume du personne.
        _shrinkingRatio = Mathf.Sqrt(0.03f) / diagonal; // Attention aux divisions par 0 ! Racine de 300 est la diagonale de la bounding box de la pokeball.
    } 
}
