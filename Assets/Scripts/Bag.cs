using UnityEngine;
using System.Collections;

public class Bag : MonoBehaviour
{
    private static Transform _head;
    private readonly static float _tolerance = 0.45f;
    private static int _emptyPokeballsLeft = 100;
	
	void Start()
	{
        _head = Camera.main.transform;
	}
	
	void Update()
	{
        
	}

    public static bool TakePokeball(Vector3 pos_)
    {
        if (_emptyPokeballsLeft > 0)
        {
            if (IsHandInBag(pos_))
            {
                _emptyPokeballsLeft--;
                return true;
            }
        }

        return false;
    }

    public static bool IsHandInBag(Vector3 pos_)
    {
        // The player is supposed to stand up while playing.

        Vector3 forward = Vector3.zero;

        if (Mathf.Abs(Vector3.Dot(Vector3.up, _head.forward)) < _tolerance)
        {
            Debug.LogWarning("Chosen forward = cam.forward");
            forward = _head.forward;
        }
        else if (Mathf.Abs(Vector3.Dot(Vector3.up, _head.up)) < _tolerance)
        {
            Debug.LogWarning("Chosen forward = cam.up");
            forward = _head.up;
        }
        else
        {
            Debug.LogWarning("Chosen forward = mix");

            // Is that ok... ?
            forward = (_head.forward + _head.up).normalized;
        }

        Plane pl = new Plane(forward, _head.position - forward * 0.15f);
        if (!pl.GetSide(pos_))
        {
            Debug.LogWarning("Point on negative side");

            if (pos_.y < _head.position.y)
            {
                Debug.LogWarning("Point lower than head");

                if (_head.position.y - pos_.y < Hub.playerHeight * 0.45f)
                {
                    Debug.LogWarning("Point higher than waist");

                    Vector3 headPosFromTop = _head.position;
                    headPosFromTop.y = 0f;

                    Vector3 handPosFromTop = pos_;
                    handPosFromTop.y = 0f;

                    if ((headPosFromTop - handPosFromTop).magnitude < 0.55f)
                    {
                        Debug.LogWarning("Point within tube");

                        return true;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Point on positive side");
        }

        return false;
    }
}