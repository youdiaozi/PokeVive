using UnityEngine;
using System.Collections;
using NewtonVR;

public class CatchPokeball : MonoBehaviour
{
    private Rigidbody _rigid;

    void Start()
    {
        _rigid = GetComponent<Rigidbody>();

        if (_rigid == null)
        {
            Debug.LogError("No rigidbody attached to the pokeball");
            DestroyImmediate(this);
        }
    }

    void Update()
    {
        foreach (NVRHand hand in NVRPlayer.Instance.Hands)
        {
            if (hand.UseButtonPressed)
            {
                Recall(hand.transform.position);
            }
        }
    }

    private void Recall(Vector3 pos)
    {
        // Stopping the current movement.
        _rigid.velocity = Vector3.zero;
        _rigid.angularVelocity = Vector3.zero;

        // New movement.
        var vector = Vector3.Normalize(pos - this.transform.position);
        _rigid.AddForce(vector * 5, ForceMode.Impulse);
    }
}
