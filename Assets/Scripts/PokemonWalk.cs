using UnityEngine;
using System.Collections;

public class PokemonWalk : MonoBehaviour
{
    private const float _range = 4f;
    private Vector3 _dest;
    private Transform _tr;
    private NavMeshAgent _nav;

	void Start()
    {
        _nav = GetComponent<NavMeshAgent>();
        _tr = this.transform;
	}
	
	void Update()
    {
	    if (_nav.remainingDistance < 0.1f)
        {
            _nav.SetDestination(new Vector3(Random.Range(-_range, _range), 0f, Random.Range(-_range, _range)));
        }
	}
}
