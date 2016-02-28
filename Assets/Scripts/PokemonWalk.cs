using UnityEngine;
using System.Collections;

public class PokemonWalk : MonoBehaviour
{
    private const float _range = 4f;
    private Vector3 _dest;
    private Transform _tr;
    private NavMeshAgent _nav;
    private Animator _anim;

    private float _waitTime = 0f;

	void Start()
    {
        _anim = GetComponent<Animator>();
        _nav = GetComponent<NavMeshAgent>();
        _tr = this.transform;
	}
	
	void Update()
    {
        _anim.SetFloat("Speed", _nav.velocity.magnitude);
        _waitTime -= Time.deltaTime;

        if (_nav.remainingDistance < 0.1f && _waitTime <= 0f)
        {
            if (Random.value < 0.2f)
            {
                _waitTime = Random.Range(2f, 4f);
            }
            else
            {
                _nav.SetDestination(new Vector3(Random.Range(-_range, _range), 0f, Random.Range(-_range, _range)));
            }
        }
	}
}
