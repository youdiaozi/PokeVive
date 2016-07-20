using UnityEngine;
using System.Collections;

public class Launcher : MonoBehaviour
{
    public float _force;
    public Transform _target;
    public GameObject _projectile;
	
	void Launch()
	{
        float speed = Ballistics.GetSpeedToReachDistanceWithGivenAngle((this.transform.position - _target.position).magnitude, -Physics.gravity.y, this.transform.eulerAngles.x, this.transform.position.y - _target.position.y);
        //speed *= 10f;
        Debug.Log("Required speed=" + speed);

        if (_force > 0f)
        {
            speed = _force;
        }

        GameObject go = (GameObject)GameObject.Instantiate(_projectile.gameObject, this.transform.position, Quaternion.identity);
        go.GetComponent<Rigidbody>().AddForce(this.transform.forward * speed, ForceMode.Impulse);
        StartCoroutine(KillProjectileLater(go));

        //Debug.Break();
	}
	
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.L))
        {
            Launch();
        }
	}

    IEnumerator KillProjectileLater(GameObject obj, float delay = 2f)
    {
        yield return new WaitForSeconds(delay);

        Destroy(obj);
    }
}