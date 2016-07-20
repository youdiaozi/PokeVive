using UnityEngine;
using System.Collections;

public class ProjectileLife : MonoBehaviour
{
    private Rigidbody _rigid;
	
	void Start()
	{
        _rigid = this.GetComponent<Rigidbody>();
	}
	
	void Update()
	{
		if (_rigid.velocity.y < 0f && this.transform.position.y <= 0f)
        {
            //Debug.Break();
        }
	}
}