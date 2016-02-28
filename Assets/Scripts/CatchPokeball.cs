using UnityEngine;
using System.Collections;
using NewtonVR;

public class CatchPokeball : NVRInteractableItem
{
    public NVRHand _rightHand;

    public override void UseButtonUp()
    {
        Debug.Log("Button Up - Right Hand position = " + _rightHand.transform.position);

        base.UseButtonUp();

        this.GetComponent<Rigidbody>().velocity = Vector3.zero;
        this.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        this.GetComponent<Rigidbody>().AddForce(_rightHand.transform.position - this.transform.position, ForceMode.Impulse);
    }
}
