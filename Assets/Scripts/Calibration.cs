using UnityEngine;
using System.Collections;

public class Calibration : MonoBehaviour
{
	
	
	void Start()
	{
		
	}
	
	void Update()
	{
		if (Hub.playerHeight < 0)
        {
            if (Camera.main.transform.position.y > 1f)
            {
                CalibratePlayerHeight();
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            CalibratePlayerHeight();
        }
	}

    public void CalibratePlayerHeight()
    {
        Hub.playerHeight = Camera.main.transform.position.y;
    }
}