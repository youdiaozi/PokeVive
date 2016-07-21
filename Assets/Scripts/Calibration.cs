using UnityEngine;
using System.Collections;

public class Calibration : MonoBehaviour
{
	
	
	void Start()
	{
        Material m = new Material(Shader.Find("Custom/StandardDepthBuffer"));
        m.SetFloat("_Mode", 2);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
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