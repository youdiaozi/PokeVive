// https://docs.unity3d.com/Manual/SL-CullAndDepth.html

Shader "Transparent/Diffuse ZWrite 1" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
	}
		SubShader{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 200

		// extra pass that renders to depth buffer only
		Pass{
		ZWrite On
		ColorMask 0
	}

		// paste in forward rendering passes from Transparent/Diffuse
		UsePass "Unlit/Alpha"

	}
}