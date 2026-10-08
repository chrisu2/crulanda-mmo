// Crulanda crop (playtest note 76, 2026-10-07): a field's crop as one combined mesh of the nature kit's tall grass, grown by the
// day (CropField sets _Grow, 0 bare to 1 ripe, and _Color, green to gold). Each plant's base height is in the second UV's x and
// its middle in the third UV (x, z), all in the mesh's own space: a plant shrinks toward its own base and middle, so plants on
// uneven ground grow from where they stand. The tips sway with the wind as the leaves do. Both faces drawn.
Shader "Crulanda/Crop"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}
        _Grow ("Growth 0..1", Range(0, 1)) = 1
        _Wind ("Wind strength", Float) = 0.08
        _Glossiness ("Smoothness", Range(0, 1)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        Cull Off

        CGINCLUDE
        #include "UnityCG.cginc"
        float _Grow, _Wind;
        float4 _WeatherWind;
        void Grow(inout appdata_full v)
        {
            float baseY = v.texcoord1.x; float2 mid = v.texcoord2.xy;
            float h = v.vertex.y - baseY;
            float g = max(_Grow, 0.001);
            v.vertex.y = baseY + h * g;
            v.vertex.xz = mid + (v.vertex.xz - mid) * lerp(0.45, 1, g);
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            float t = _Time.y * 1.3;
            float gust = sin(t + wp.x * 0.21 + wp.z * 0.17) * 0.6 + sin(t * 2.1 + wp.x * 0.8) * 0.4;
            float weight = saturate(h * g / 0.9);
            float2 lean = float2(gust, gust * 0.6) * _Wind * weight * (1 + _WeatherWind.z * 2) + _WeatherWind.xy * _Wind * weight * _WeatherWind.z * 2;
            v.vertex.xz += mul((float3x3)unity_WorldToObject, float3(lean.x, 0, lean.y)).xz;
        }
        ENDCG

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        sampler2D _MainTex; fixed4 _Color; half _Glossiness;
        struct Input { float2 uv_MainTex; };
        void vert(inout appdata_full v) { Grow(v); }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb; o.Smoothness = _Glossiness; o.Metallic = 0; o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
