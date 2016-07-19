using UnityEngine;
using System.Collections;

public class LookAt : MonoBehaviour
{
	public Transform _target;

    private Transform _tr;

    void Start()
    {
        _tr = this.transform;
        _target = Camera.main.transform;
    }

	void Update()
	{
        if (_target == null)
        {
            _target = Camera.main.transform;
        }
        else
        {
            _tr.LookAt(_target.position);
        }
	}
}
