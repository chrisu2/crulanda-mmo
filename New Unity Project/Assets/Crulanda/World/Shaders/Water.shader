// Crulanda water (built-in pipeline surface shader in the transparent queue).
// It is see-through: a named GrabPass holds the scene behind the water and the camera depth texture gives how much water
// the view ray crosses to reach it (metres). The bed shows through shallow water, bent a little by the ripples, and
// fades into the water's own colour with depth (Beer-Lambert). The surface adds fresnel-weighted sky reflection and sun
// glints (LightingWater) and thin, broken foam only where the water actually touches something (banks, legs, posts).
// The composite is written opaquely (finalcolor sets alpha 1 over premultiplied blending).
// UVs are in metres: creeks run u = across, v = along the flow (uv2.x = 1 marks flowing water), lakes use planar
// local x/z (uv2.x = 0, almost still). Vertex alpha is the shallowness under the vertex (1 at the waterline); it only
// calms the swell near the shore now. Queue Transparent-10 so splashes, leaves and the Wasting veil draw over the water.
// Needs the main camera's depth texture (ZoneBuilder turns it on); an orthographic camera (the zone map) sees plain
// water colour instead.
Shader "Crulanda/Water"
{
    Properties
    {
        _Color ("Deep water", Color) = (0.08, 0.17, 0.19, 0.9)
        _Shallow ("Shallows tint", Color) = (0.24, 0.33, 0.30, 0.45)
        _Foam ("Foam", Color) = (0.82, 0.86, 0.84, 1)
        _Normal ("Ripples (normal)", 2D) = "bump" {}
        _FlowSpeed ("Creek flow (m/s)", Float) = 0.35
        _Drift ("Lake drift", Vector) = (0.012, 0.007, -0.009, 0.011)
        _Wave ("Swell height", Float) = 0.025
        _Gloss ("Smoothness", Range(0, 1)) = 0.82
        _Bump ("Ripple strength", Range(0, 1)) = 0.3
        _Reflect ("Sky reflection face-on", Range(0, 1)) = 0.7
        _Glare ("Highlight cap (HDR)", Float) = 1.2
        _Murk ("Murk (per metre of water)", Float) = 0.8
        _Refract ("Refraction", Range(0, 0.1)) = 0.03
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 200
        GrabPass { "_WaterBackground" }
        Cull Back
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Water alpha:premul vertex:vert finalcolor:Opaque
        #pragma target 3.0
        // LightingStandard premultiplies the diffuse under this define (as it did with 'surf Standard').
        #ifndef _ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON 1
        #endif
        #include "UnityPBSLighting.cginc"
        sampler2D _Normal, _WaterBackground, _CameraDepthTexture;
        float4 _CameraDepthTexture_TexelSize;
        fixed4 _Color, _Shallow, _Foam;
        float4 _Drift;
        float _FlowSpeed, _Wave, _Gloss, _Bump, _Reflect, _Glare, _Murk, _Refract;
        // Standard lighting with the sky reflection reined in. In this Gamma-space project Standard's dielectric F0 is 0.22
        // and its grazing term 1, so plain Standard mirrors the sky-only HDR probe at every angle. Here the sky is _Reflect
        // of that face-on, rising to full at grazing; it fades where the reflection skims or dips under the horizon (banks
        // and trees would be there); and nothing, sun disc or glint, gets brighter than _Glare.
        half4 LightingWater(SurfaceOutputStandard s, half3 viewDir, UnityGI gi)
        {
            half3 r = reflect(-viewDir, s.Normal);
            // Below the horizon the reflection would show banks and trees: keep some of it (the sky's lower half matches
            // the fog), never black.
            half k = lerp(_Reflect, 1, Pow5(1 - saturate(dot(s.Normal, viewDir)))) * lerp(0.45, 1, saturate(r.y * 4 + 0.4));
            gi.indirect.specular = min(gi.indirect.specular, _Glare) * k;
            half4 c = LightingStandard(s, viewDir, gi);
            c.rgb = min(c.rgb, _Glare);
            return c;
        }
        void LightingWater_GI(SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi) { LightingStandard_GI(s, data, gi); }
        // Value noise 0..1, cells in metres (sin-free hash, stable far from the origin).
        float WaterHash(float2 p) { float3 q = frac(p.xyx * 0.1031); q += dot(q, q.yzx + 33.33); return frac((q.x + q.y) * q.z); }
        float WaterNoise(float2 p)
        {
            float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
            return lerp(lerp(WaterHash(i), WaterHash(i + float2(1, 0)), f.x), lerp(WaterHash(i + float2(0, 1)), WaterHash(i + 1), f.x), f.y);
        }
        struct Input { float2 uv_Normal; float2 flowFlag; float3 worldPos; float3 viewDir; float4 screenPos; float4 color : COLOR; };
        // The surface composites itself over the grabbed scene, so it replaces what is behind it.
        void Opaque(Input IN, SurfaceOutputStandard o, inout fixed4 color) { color.a = 1; }
        // The scene behind the water at this pixel, bent by `bend` (screen units) where there is water to bend through, and
        // the metres of water the view ray crosses to reach it. Things in front of the surface are never bent.
        float3 Behind(float4 sp, float2 bend, out float thick)
        {
            float2 uv = sp.xy / sp.w;
            #if UNITY_UV_STARTS_AT_TOP
            if (_CameraDepthTexture_TexelSize.y < 0) uv.y = 1 - uv.y;
            #endif
            float t0 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv)) - sp.w;
            float2 bent = uv + bend * saturate(t0);
            thick = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, bent)) - sp.w;
            if (thick < 0) { bent = uv; thick = t0; }
            thick = max(thick, 0);
            return tex2D(_WaterBackground, bent).rgb;
        }
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.flowFlag = v.texcoord1.xy;
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            // Gentle swell, fading out toward the shore so the waterline stays put.
            v.vertex.y += (sin(wp.x * 0.8 + _Time.y * 1.3) + sin(wp.z * 0.63 + _Time.y * 1.07)) * _Wave * (1 - v.color.a);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Metres to ripple tiles. Creeks scroll both layers downstream (+v) at slightly different speeds; lakes drift.
            float2 uv = IN.uv_Normal * 0.11;
            float flowing = IN.flowFlag.x;
            float2 d1 = lerp(_Drift.xy, float2(0.02, _FlowSpeed * 0.11), flowing) * _Time.y;
            float2 d2 = lerp(_Drift.zw, float2(-0.015, _FlowSpeed * 0.07), flowing) * _Time.y;
            float3 n1 = UnpackNormal(tex2D(_Normal, uv - d1));
            float3 n2 = UnpackNormal(tex2D(_Normal, uv * 1.9 - d2));
            float2 slope = (n1.xy + n2.xy) * _Bump;
            // Calm the ripples where the view skims the water and far away: steep ripples at grazing angles bounce the view
            // under the horizon and read as black blotches, and far off they only flicker.
            float dist = distance(_WorldSpaceCameraPos, IN.worldPos);
            slope *= lerp(0.3, 1, saturate(normalize(IN.viewDir).z * 2.5)) * lerp(1, 0.5, saturate(dist / 60));
            o.Normal = normalize(float3(slope, 1));
            // The water's own colour, lit roughly like the scene (ambient plus sun or moon from above), so it darkens at night.
            float3 lightNow = ShadeSH9(float4(0, 1, 0, 1)) + _LightColor0.rgb * saturate(_WorldSpaceLightPos0.y) * 0.6;
            float3 deep = _Color.rgb * lightNow;
            // Look through: the bed and anything under the surface show in shallow water, tinted a little, and fade into the
            // water colour with the depth the ray crosses. The zone map (orthographic) has no depth: plain water colour.
            float thick; float3 below;
            if (unity_OrthoParams.w > 0.5) { thick = 3; below = deep; }
            else below = Behind(IN.screenPos, slope * _Refract, thick);
            float see = exp2(-_Murk * thick);
            float3 tint = lerp(1, saturate(_Shallow.rgb * 2.2), 0.4);
            float3 through = lerp(deep, below * tint, see);
            // Foam: thin and broken, only where the water meets something (the waterline, legs, posts, rocks), riding the
            // flow on creeks. Slow noise decides where there is any at all; fine noise breaks it into clumps.
            float2 fp = IN.uv_Normal - float2(0, _FlowSpeed * _Time.y) * flowing;
            float touch = 1 - saturate(thick / 0.14);
            float patchy = saturate(WaterNoise(fp * 0.3 + 17.3) * 2.2 - 0.9);
            float clumps = WaterNoise(fp * 2.1 + _Time.y * 0.08) * 0.6 + WaterNoise(fp * 5.3 - _Time.y * 0.05) * 0.4;
            float foam = smoothstep(clumps - 0.1, clumps + 0.1, touch * patchy) * 0.55 * (unity_OrthoParams.w > 0.5 ? 0 : 1);
            o.Albedo = lerp(deep, _Foam.rgb, foam);
            o.Alpha = foam;                                   // the surface itself is clear; only foam has body
            // Light seen through the surface loses what the surface reflects (mostly at grazing angles).
            float fres = Pow5(1 - saturate(dot(normalize(IN.viewDir), o.Normal)));
            o.Emission = through * (1 - foam) * (1 - fres * 0.6);
            // Specular anti-aliasing: rougher where ripples are strong and far away (no flickering glints).
            o.Smoothness = _Gloss * (1 - foam * 0.6) * lerp(1, 0.8, saturate(length(slope))) * lerp(1, 0.75, saturate(dist / 80));
            o.Metallic = 0;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Diffuse"
}
