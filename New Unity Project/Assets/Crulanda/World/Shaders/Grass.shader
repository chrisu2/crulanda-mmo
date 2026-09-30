// Crulanda grass: alpha-cut blades that sway in the wind (tips move, roots stay), lit like the ground beneath
// (upward normals from the mesh), GPU-instanced. Built-in pipeline surface shader.
Shader "Crulanda/Grass"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Blades", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.45
        _Wind ("Wind strength", Float) = 0.12
        _WindSpeed ("Wind speed", Float) = 1.6
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
        LOD 200
        Cull Off
        CGPROGRAM
        #pragma surface surf Lambert alphatest:_Cutoff vertex:vert addshadow
        #pragma multi_compile_instancing
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        float _Wind, _WindSpeed;
        // The weather's wind (WorldWeather, global): xy its direction on the ground (world x, z), z its strength 0..1. All zero
        // without weather: only the grass's own sway.
        float4 _WeatherWind;
        struct Input { float2 uv_MainTex; };
        void vert(inout appdata_full v)
        {
            UNITY_SETUP_INSTANCE_ID(v);
            float3 wp = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
            float tip = v.texcoord.y * v.texcoord.y;
            float gust = sin(_Time.y * _WindSpeed + wp.x * 0.35 + wp.z * 0.21) * 0.7 + sin(_Time.y * _WindSpeed * 2.3 + wp.x * 1.1) * 0.3;
            // Gusts roll across the field downwind (a travelling wave): they stir the sway and bow the blades downwind.
            float w = _WeatherWind.z;
            float wave = sin(dot(wp.xz, _WeatherWind.xy) * 0.09 - _Time.y * 1.8) * 0.5 + 0.5; wave *= wave;
            float stir = 1 + w * (0.6 + 1.4 * wave);
            v.vertex.x += gust * _Wind * stir * tip;
            v.vertex.z += gust * _Wind * 0.5 * stir * tip;
            float3 down = mul((float3x3)unity_WorldToObject, float3(_WeatherWind.x, 0, _WeatherWind.y));
            v.vertex.xyz += normalize(down + float3(1e-5, 0, 0)) * (_Wind * w * (0.9 + 2.1 * wave) * tip) + float3(0, -0.12, 0) * (w * wave * tip);
        }
        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/Diffuse"
}
