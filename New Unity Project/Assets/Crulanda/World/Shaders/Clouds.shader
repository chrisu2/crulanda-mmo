// Crulanda clouds: the painted cloud layer over the sky (WorldWeather). Drawn on a dome that rides with the camera, after the
// skybox and before anything transparent, behind all land (depth tested, no depth written, no fog). Each pixel's view ray meets a
// flat sheet of cloud _Height metres up, so clouds hold still in the world, drift with the wind and crowd toward the horizon.
// The density comes from a tiling noise (two scales) cut at the weather's cover: soft-edged puffs when fair, a closed grey lid
// when overcast. Shading: tops in _TopColor, thick parts and the side away from the sun in _UnderColor (a second sample toward
// the sun gives the cloud's lit and shaded sides), a silver rim on thin cloud round the sun, thinning out at the horizon.
// ZonePost casts the matching shadows on the land from the same noise, cover and drift.
Shader "Crulanda/Clouds"
{
    Properties
    {
        _MainTex ("Cloud noise (red = density; tiles)", 2D) = "gray" {}
        _Coverage ("Cover", Range(0, 1)) = 0.45
        _Softness ("Edge softness", Range(0.02, 0.5)) = 0.14
        _TopColor ("Sunlit tops", Color) = (1, 0.985, 0.96, 1)
        _UnderColor ("Shaded undersides", Color) = (0.62, 0.67, 0.77, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.96
        _Drift ("Drift (xy, metres)", Vector) = (0, 0, 0, 0)
        _SunDir ("Direction to the sun (world)", Vector) = (0, 1, 0, 0)
        _Height ("Cloud layer height (m)", Float) = 900
        _Tile ("Metres per noise repeat", Float) = 2600
        _HorizonColor ("The air at the horizon (the fog)", Color) = (0.62, 0.62, 0.6, 1)
        _Overcast ("Overcast (0: broken cloud, 1: a closed lid)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-100" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Sphere" }
        Cull Off ZWrite Off ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Coverage, _Softness, _Opacity, _Height, _Tile, _Overcast;
            fixed4 _TopColor, _UnderColor, _HorizonColor;
            float4 _Drift, _SunDir;
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.dir = v.vertex.xyz;   // the dome is a unit dome about the camera
                return o;
            }
            float Density(float2 world)
            {
                float2 uv = (world - _Drift.xy) / _Tile;
                return tex2D(_MainTex, uv).r * 0.65 + tex2D(_MainTex, uv * 2.7 + 0.37).r * 0.35;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                if (d.y < -0.02) return fixed4(0, 0, 0, 0);
                float2 world = _WorldSpaceCameraPos.xz + d.xz * ((_Height - _WorldSpaceCameraPos.y) / max(d.y, 0.012));   // the sheet stands _Height up in the world, as the shadows take it
                float n = Density(world), edge = 1 - _Coverage;
                float a = smoothstep(edge - _Softness, edge + _Softness, n);
                float3 toSun = normalize(_SunDir.xyz + float3(0, 1e-4, 0));
                float2 sunward = normalize(toSun.xz + float2(1e-4, 0));
                float lee = Density(world + sunward * 160) - n;                  // thicker toward the sun: this side is in its shade
                float thick = saturate((n - edge) / max(_Softness * 2.5, 0.05));
                float shade = saturate(0.28 + lee * 3.2 * (1 - 0.7 * _Overcast) + thick * 0.55);
                shade = lerp(shade, 0.7 + 0.3 * shade, _Overcast);               // a lid: mostly the underside colour, gently mottled
                fixed3 col = lerp(_TopColor.rgb, _UnderColor.rgb, shade);
                col += _TopColor.rgb * pow(saturate(dot(d, toSun)), 6) * (1 - thick) * 0.55 * (1 - _Overcast);   // silver lining round the sun
                // Toward the horizon the cloud melts into the air (the fog's colour, like the land's far edge). Broken cloud thins
                // out above it; a closed lid runs right down to it, so no sunny horizon shows under an overcast sky.
                col = lerp(col, _HorizonColor.rgb, (1 - smoothstep(0, 0.3, d.y)) * 0.85);
                a *= lerp(smoothstep(0.03, 0.26, d.y), smoothstep(-0.015, 0.05, d.y), _Overcast) * _Opacity;
                return fixed4(col, a);
            }
            ENDCG
        }
    }
    FallBack Off
}
