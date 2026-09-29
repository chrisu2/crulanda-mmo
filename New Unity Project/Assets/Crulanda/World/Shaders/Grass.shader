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
        struct Input { float2 uv_MainTex; };
        void vert(inout appdata_full v)
        {
            UNITY_SETUP_INSTANCE_ID(v);
            float3 wp = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
            float tip = v.texcoord.y * v.texcoord.y;
            float gust = sin(_Time.y * _WindSpeed + wp.x * 0.35 + wp.z * 0.21) * 0.7 + sin(_Time.y * _WindSpeed * 2.3 + wp.x * 1.1) * 0.3;
            v.vertex.x += gust * _Wind * tip;
            v.vertex.z += gust * _Wind * 0.5 * tip;
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
