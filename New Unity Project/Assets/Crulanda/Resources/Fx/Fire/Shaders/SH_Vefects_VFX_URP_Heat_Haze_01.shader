// Vefects' heat haze (made for URP, a screen-grab distortion): drawn as nothing in the built-in pipeline (2026-10-14, Crulanda), so the
// pack's fire prefabs that carry it keep their flames without a pink square. Same GUID as the vendor's.
Shader "Vefects/SH_Vefects_VFX_URP_Heat_Haze_01"
{
	Properties { _Texture("Texture", 2D) = "white" {} }
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		Pass { ColorMask 0 ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			float4 vert (float4 v : POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
			fixed4 frag () : SV_Target { return 0; }
			ENDCG
		}
	}
}
