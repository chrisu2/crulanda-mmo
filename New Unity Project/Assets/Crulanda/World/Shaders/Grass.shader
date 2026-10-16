// Crulanda grass: alpha-cut blades that sway in the wind (tips move, roots stay), lit like the ground beneath
// (upward normals from the mesh), GPU-instanced. Built-in pipeline surface shader. The blades also part round feet
// (_GrassBenders, fed by GrassBend): the push from the "Interactive Grass" Shader Graph Chris downloaded (2026-10-15 01:20,
// "use these where appropriate"), rewritten here since the project has no Shader Graph; the wind was already ours.
Shader "Crulanda/Grass"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Blades", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.45
        _Wind ("Wind strength", Float) = 0.12
        _WindSpeed ("Wind speed", Float) = 1.6
        _Glow ("Night glow", Float) = 0
        _Wither ("Wither (drains the paint's green)", Range(0, 1)) = 0
        _FadeFar ("Fade end in metres from the camera (0: no fade)", Float) = 0
        _Bend ("Parting round feet (the tip's lean)", Float) = 0.3
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
        float _Wind, _WindSpeed, _Glow, _Wither;
        // GrassField's tufts (its own material copies set this): a tuft shrinks into the ground over the 20 m before _FadeFar, so
        // the field thins out into the turf's paint instead of whole cells switching. 0 (plants, the assets): no fade.
        float _FadeFar;
        // How dark the night is (WorldClock, global): a glowing flower (_Glow) is faint by day and lights up after dark.
        float _NightGlow;
        // The weather's wind (WorldWeather, global): xy its direction on the ground (world x, z), z its strength 0..1. All zero
        // without weather: only the grass's own sway.
        float4 _WeatherWind;
        // Who is walking through the grass (GrassBend, global, up to 8): xz on the ground, w the reach in metres. Count 0: no one.
        float4 _GrassBenders[8];
        float _GrassBenderCount, _Bend;
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
            // Blades part round feet (the interactive grass pack's push, 2026-10-15): the tuft leans away from whoever stands within
            // reach, flat near the foot and easing off by the reach, tips most and roots not at all, and crouches as it leans.
            float2 away = 0;
            for (int i = 0; i < 8; i++)
            {
                float4 b = _GrassBenders[i]; float r = max(b.w, 0.01);
                float2 d = wp.xz - b.xz; float dist = length(d);
                away += d / max(dist, 0.05) * (1 - smoothstep(r * 0.3, r, dist)) * step(i + 0.5, _GrassBenderCount);
            }
            float part = min(length(away), 1);
            float3 lean = normalize(mul((float3x3)unity_WorldToObject, float3(away.x, 0, away.y)) + float3(0, 0, 1e-5));
            v.vertex.xyz += lean * (_Bend * part * tip);
            v.vertex.y *= 1 - 0.35 * part * tip;
            // The distance fade: the tuft's root is the object's origin, so scaling the vertex draws it down into its root.
            float fade = _FadeFar > 0 ? saturate((_FadeFar - distance(wp.xz, _WorldSpaceCameraPos.xz)) * 0.05) : 1;
            v.vertex.xyz *= fade;
        }
        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            // Withered (Khaven's dead wood): the paint's green drains to dry brown-grey, keeping the painted light and dark.
            float grey = dot(c.rgb, float3(0.3, 0.59, 0.11));
            c.rgb = lerp(c.rgb, float3(grey * 1.15, grey * 0.98, grey * 0.78), _Wither);
            o.Albedo = c.rgb;
            o.Emission = c.rgb * _Glow * (0.15 + 0.85 * _NightGlow);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/Diffuse"
}
