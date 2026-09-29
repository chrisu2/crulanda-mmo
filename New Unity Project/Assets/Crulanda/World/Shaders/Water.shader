// Crulanda water (built-in pipeline surface shader, transparent, premultiplied alpha so reflections keep their strength).
// UVs are in metres: creeks run u = across, v = along the flow (uv2.x = 1 marks flowing water), lakes use planar
// local x/z (uv2.x = 0, almost still). Vertex alpha is the shallowness from the real depth under the vertex
// (1 at the waterline): it drives the shallow tint, foam and transparency. Queue Transparent-10 so splashes,
// leaves and the Wasting veil always draw over the water.
Shader "Crulanda/Water"
{
    Properties
    {
        _Color ("Deep water", Color) = (0.08, 0.17, 0.19, 0.9)
        _Shallow ("Shallows", Color) = (0.24, 0.33, 0.30, 0.45)
        _Foam ("Foam", Color) = (0.82, 0.86, 0.84, 1)
        _Normal ("Ripples (normal)", 2D) = "bump" {}
        _FlowSpeed ("Creek flow (m/s)", Float) = 0.35
        _Drift ("Lake drift", Vector) = (0.012, 0.007, -0.009, 0.011)
        _Wave ("Swell height", Float) = 0.025
        _Gloss ("Smoothness", Range(0, 1)) = 0.82
        _Bump ("Ripple strength", Range(0, 1)) = 0.3
        _Reflect ("Sky reflection face-on", Range(0, 1)) = 0.7
        _Glare ("Highlight cap (HDR)", Float) = 1.2
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 200
        Cull Back
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Water alpha:premul vertex:vert
        #pragma target 3.0
        // LightingStandard premultiplies the diffuse under this define (as it did with 'surf Standard').
        #ifndef _ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON 1
        #endif
        #include "UnityPBSLighting.cginc"
        sampler2D _Normal;
        fixed4 _Color, _Shallow, _Foam;
        float4 _Drift;
        float _FlowSpeed, _Wave, _Gloss, _Bump, _Reflect, _Glare;
        // Standard lighting with the sky reflection reined in. In this Gamma-space project Standard's dielectric F0 is 0.22
        // and its grazing term 1, so the water mirrored the sky-only HDR probe (sun disc, horizon haze, and the sky's ground
        // colour wherever a ripple tilts the reflection under the horizon) at every angle, and a zone tint, which only
        // reaches the premultiplied diffuse underneath, barely showed. Now the sky is _Reflect of that face-on, rising to
        // full at grazing; it fades where the reflection skims or dips under the horizon (banks and trees would be there);
        // and nothing, sun disc or glint, gets brighter than _Glare, so the water never out-shines the sky or blooms.
        half4 LightingWater(SurfaceOutputStandard s, half3 viewDir, UnityGI gi)
        {
            half3 r = reflect(-viewDir, s.Normal);
            half k = lerp(_Reflect, 1, Pow5(1 - saturate(dot(s.Normal, viewDir)))) * saturate(r.y * 4 + 0.4);
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
        struct Input { float2 uv_Normal; float2 flowFlag; float3 worldPos; float3 viewDir; float4 color : COLOR; };
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
            o.Normal = normalize(float3(slope, 1));
            float fres = pow(1 - saturate(dot(normalize(IN.viewDir), o.Normal)), 3);
            float shore = IN.color.a;
            fixed3 body = lerp(_Color.rgb, _Shallow.rgb, shore);
            // Foam: a broken, lapping line at the waterline, not a ring. The shallowness alone is exactly radial round a lake,
            // so slow noise sets how far out the foam reaches (none at all in places) and fine noise breaks it into clumps;
            // on creeks both ride the flow downstream.
            float2 fp = IN.uv_Normal - float2(0, _FlowSpeed * _Time.y) * flowing;
            float reach = saturate(WaterNoise(fp * 0.35 + 17.3) * 1.8 - 0.4);
            float band = saturate((shore - 1 + reach * 0.5 + 0.05 * sin(_Time.y * 1.3 + reach * 9)) * 4.5);
            float clumps = WaterNoise(fp * 1.8 + _Time.y * 0.06) * 0.65 + WaterNoise(fp * 4.3 - _Time.y * 0.04) * 0.35;
            float foam = smoothstep(clumps - 0.12, clumps + 0.12, band) * 0.9;
            o.Albedo = lerp(body, _Foam.rgb, foam);
            // Specular anti-aliasing: rougher where ripples are strong and far away (no flickering glints).
            float dist = distance(_WorldSpaceCameraPos, IN.worldPos);
            o.Smoothness = _Gloss * (1 - foam * 0.6) * lerp(1, 0.8, saturate(length(slope))) * lerp(1, 0.75, saturate(dist / 80));
            o.Metallic = 0;
            o.Alpha = saturate(lerp(_Color.a, _Shallow.a, shore) + fres * 0.2 + foam * 0.5);
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Diffuse"
}
