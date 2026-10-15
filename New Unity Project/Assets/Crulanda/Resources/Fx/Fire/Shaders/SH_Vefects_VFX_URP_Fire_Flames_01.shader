// Vefects' Free Fire VFX flames (SH_Vefects_VFX_URP_Fire_Flames_01, made for URP), written again for the built-in pipeline (2026-10-14,
// Crulanda: Chris, "i added some spell effects, aoe effects, melee effects, fire to myassets"). The same name, properties and asset
// GUID as the vendor's, so the pack's own materials and fire prefabs draw unchanged: two scrolling noises, distorted, eat into the
// flame mask; the colour runs from the bottom colour to the top by the gradient mask; the edge fades where it meets the ground (the
// camera's depth texture).
Shader "Vefects/SH_Vefects_VFX_URP_Fire_Flames_01"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		_EmissiveIntensity("Emissive Intensity", Float) = 1
		_Noise_01_Texture("Noise_01_Texture", 2D) = "white" {}
		_Noise_02_Texture("Noise_02_Texture", 2D) = "white" {}
		_ColorGradientMask("Color Gradient Mask", 2D) = "white" {}
		_DistortionMaskTexture("Distortion Mask Texture", 2D) = "white" {}
		_Mask_Texture("Mask_Texture", 2D) = "white" {}
		_TextureSample2("Texture Sample 2", 2D) = "white" {}
		_ColorBottom("Color Bottom", Color) = (1,0.3666667,0,0)
		_ColorTop("Color Top", Color) = (1,0.6666667,0,0)
		_NoiseDistortion_Texture("NoiseDistortion_Texture", 2D) = "white" {}
		_Noise_01_Scale("Noise_01_Scale", Vector) = (0.8,0.8,0,0)
		_Noise_02_Scale("Noise_02_Scale", Vector) = (1,1,0,0)
		_NoiseDistortion_Scale("NoiseDistortion_Scale", Vector) = (1,1,0,0)
		_Noise_01_Speed("Noise_01_Speed", Vector) = (0.5,0.5,0,0)
		_Noise_02_Speed("Noise_02_Speed", Vector) = (-0.2,0.4,0,0)
		_Mask_Scale("Mask_Scale", Vector) = (1,1,0,0)
		_Mask_Offset("Mask_Offset", Vector) = (0,0,0,0)
		_NoiseDistortion_Speed("NoiseDistortion_Speed", Vector) = (-0.3,-0.3,0,0)
		_Mask_Multiply("Mask_Multiply", Float) = 1
		_Noises_Multiply("Noises_Multiply", Float) = 5
		_Mask_Power("Mask_Power", Float) = 1
		_Noises_Power("Noises_Power", Float) = 1
		_DistortionAmount("Distortion Amount", Float) = 1
		_DistortionMaskIntensity("Distortion Mask Intensity", Float) = 1
		_NoisesOpacityBoost("Noises Opacity Boost", Float) = 1
		_DepthFade("Depth Fade", Float) = 1
		_WindSpeed("Wind Speed", Float) = 1
		_Dissolve("Dissolve", Float) = 0
		_Cull1("Cull", Float) = 2
		_Src1("Src", Float) = 5
		_Dst1("Dst", Float) = 10
		_ZWrite1("ZWrite", Float) = 0
		_ZTest1("ZTest", Float) = 2
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
		Blend [_Src1] [_Dst1], One OneMinusSrcAlpha
		ZWrite [_ZWrite1]
		ZTest [_ZTest1]
		Cull [_Cull1]
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			sampler2D _Noise_01_Texture, _Noise_02_Texture, _ColorGradientMask, _DistortionMaskTexture, _Mask_Texture, _TextureSample2, _NoiseDistortion_Texture;
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			float4 _ColorGradientMask_ST, _ColorBottom, _ColorTop, _Noise_01_Scale, _Noise_02_Scale, _NoiseDistortion_Scale, _Noise_01_Speed, _Noise_02_Speed, _Mask_Scale, _Mask_Offset, _NoiseDistortion_Speed;
			float _EmissiveIntensity, _Mask_Multiply, _Noises_Multiply, _Mask_Power, _Noises_Power, _DistortionAmount, _DistortionMaskIntensity, _NoisesOpacityBoost, _DepthFade, _WindSpeed, _Dissolve;
			struct appdata { float4 vertex : POSITION; float4 color : COLOR; float4 uv : TEXCOORD0; };
			struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float4 screen : TEXCOORD1; UNITY_FOG_COORDS(2) };
			v2f vert (appdata v)
			{
				v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv.xy;
				o.screen = ComputeScreenPos(o.pos); COMPUTE_EYEDEPTH(o.screen.z);
				UNITY_TRANSFER_FOG(o, o.pos); return o;
			}
			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv; float wind = _WindSpeed * _Time.y;
				float distortion = tex2D(_NoiseDistortion_Texture, wind * _NoiseDistortion_Speed.xy + uv * _NoiseDistortion_Scale.xy).r * 0.1 * _DistortionAmount;
				float noises = saturate(pow(tex2D(_Noise_01_Texture, wind * _Noise_01_Speed.xy + uv * _Noise_01_Scale.xy + distortion).r
					* tex2D(_Noise_02_Texture, wind * _Noise_02_Speed.xy + uv * _Noise_02_Scale.xy + distortion).r, _Noises_Power) * _Noises_Multiply);
				float mask = saturate(pow(tex2D(_Mask_Texture, tex2D(_DistortionMaskTexture, uv + float2(0, 0.4)).r * distortion * _DistortionMaskIntensity + uv * _Mask_Scale.xy + _Mask_Offset.xy).r, _Mask_Power) * _Mask_Multiply);
				float body = mask - noises * _NoisesOpacityBoost * tex2D(_TextureSample2, uv + float2(0, 0.4)).r;
				float4 tint = lerp(_ColorBottom, _ColorTop, tex2D(_ColorGradientMask, uv * _ColorGradientMask_ST.xy + _ColorGradientMask_ST.zw).r);
				float scene = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screen)));
				float fade = saturate((scene - i.screen.z) / max(0.001, _DepthFade));
				fixed4 c;
				c.rgb = (i.color * tint).rgb * body * _EmissiveIntensity;
				c.a = saturate(saturate((body - _Dissolve) * fade) * i.color.a);
				UNITY_APPLY_FOG(i.fogCoord, c);
				return c;
			}
			ENDCG
		}
	}
}
