// Vefects' Free Fire VFX smoke and embers (SH_Vefects_VFX_URP_Particles_Erosion_01, made for URP), written again for the built-in
// pipeline (2026-10-14, Crulanda). The same name, properties and GUID as the vendor's, so the pack's materials and prefabs draw
// unchanged: the mask, moved by the particle's custom data (TEXCOORD1.zw), times the scrolling noises, eroded by the particle's age
// (TEXCOORD1.x) and the dissolve; the edge fades where it meets the ground.
Shader "Vefects/SH_Vefects_VFX_URP_Particles_Erosion_01"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		_Noise_01_Texture("Noise_01_Texture", 2D) = "white" {}
		_Noise_02_Texture("Noise_02_Texture", 2D) = "white" {}
		_MaskTexture("Mask Texture", 2D) = "white" {}
		_MaskMoveTexture("Mask Move Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,0)
		_NoiseDistortionTexture("Noise Distortion Texture", 2D) = "white" {}
		_Noise01Scale("Noise 01 Scale", Vector) = (0.8,0.8,0,0)
		_Noise02Scale("Noise 02 Scale", Vector) = (1,1,0,0)
		_NoiseDistortionScale("Noise Distortion Scale", Vector) = (1,1,0,0)
		_Noise01Speed("Noise 01 Speed", Vector) = (0.5,0.5,0,0)
		_Noise02Speed("Noise 02 Speed", Vector) = (-0.2,0.4,0,0)
		_MaskMoveScale("Mask Move Scale", Vector) = (1,1,0,0)
		_MaskScale("Mask Scale", Vector) = (1,1,0,0)
		_MaskOffset("Mask Offset", Vector) = (0,0,0,0)
		_NoiseDistortionSpeed("Noise Distortion Speed", Vector) = (0.2,0.25,0,0)
		_MaskMultiply("Mask Multiply", Float) = 1
		_MaskMoveMultiply("Mask Move Multiply", Float) = 1
		_NoisesMultiply("Noises Multiply", Float) = 1
		_MaskPower("Mask Power", Float) = 1
		_NoisesPower("Noises Power", Float) = 1
		_MaskMovePower("Mask Move Power", Float) = 1
		_Distortion("Distortion", Float) = 1
		_DistortionIntensity("Distortion Intensity", Float) = 0
		_OpacityBoost("Opacity Boost", Float) = 5
		_DepthFade("Depth Fade", Float) = 1
		_EmissionIntensity("Emission Intensity", Float) = 1
		_WindSpeed("Wind Speed", Float) = 1
		_MaskSpeed("Mask Speed", Float) = 0
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
			sampler2D _Noise_01_Texture, _Noise_02_Texture, _MaskTexture, _MaskMoveTexture, _NoiseDistortionTexture;
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			float4 _Color, _Noise01Scale, _Noise02Scale, _NoiseDistortionScale, _Noise01Speed, _Noise02Speed, _MaskMoveScale, _MaskScale, _MaskOffset, _NoiseDistortionSpeed;
			float _MaskMultiply, _MaskMoveMultiply, _NoisesMultiply, _MaskPower, _NoisesPower, _MaskMovePower, _Distortion, _DistortionIntensity, _OpacityBoost, _DepthFade, _EmissionIntensity, _WindSpeed, _MaskSpeed, _Dissolve;
			struct appdata { float4 vertex : POSITION; float4 color : COLOR; float4 uv : TEXCOORD0; float4 custom : TEXCOORD1; };
			struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float4 custom : TEXCOORD1; float4 screen : TEXCOORD2; UNITY_FOG_COORDS(3) };
			v2f vert (appdata v)
			{
				v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv.xy; o.custom = v.custom;
				o.screen = ComputeScreenPos(o.pos); COMPUTE_EYEDEPTH(o.screen.z);
				UNITY_TRANSFER_FOG(o, o.pos); return o;
			}
			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv; float wind = _WindSpeed * _Time.y;
				float distortion = tex2D(_NoiseDistortionTexture, wind * _NoiseDistortionSpeed.xy + uv * _NoiseDistortionScale.xy).r * 0.1 * _Distortion;
				float noises = saturate(pow(tex2D(_Noise_01_Texture, distortion + wind * _Noise01Speed.xy + uv * _Noise01Scale.xy).r
					* tex2D(_Noise_02_Texture, distortion + wind * _Noise02Speed.xy + uv * _Noise02Scale.xy).r, _NoisesPower) * _NoisesMultiply);
				float mask = saturate(pow(tex2D(_MaskTexture, distortion * _DistortionIntensity + wind * float2(_MaskSpeed, 0) + uv * _MaskScale.xy + _MaskOffset.xy).r, _MaskPower) * _MaskMultiply);
				float moved = saturate(pow(tex2D(_MaskMoveTexture, uv * _MaskMoveScale.xy + i.custom.zw).r, _MaskMovePower) * _MaskMoveMultiply);
				float body = saturate(saturate(mask * moved) * noises);
				float scene = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screen)));
				float fade = saturate((scene - i.screen.z) / max(0.001, _DepthFade));
				fixed4 c;
				c.rgb = (_Color * i.color).rgb * body * _EmissionIntensity;
				c.a = saturate(i.color.a * saturate(saturate(saturate(body * _OpacityBoost) - (i.custom.x + _Dissolve)) * fade));
				UNITY_APPLY_FOG(i.fogCoord, c);
				return c;
			}
			ENDCG
		}
	}
}
