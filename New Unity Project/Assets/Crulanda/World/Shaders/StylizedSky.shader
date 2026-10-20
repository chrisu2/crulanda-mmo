// Crulanda stylized sky (play note 254: Staggart Creations' Stylized Skyboxes, Resources/Sky/Stylized): up to three of the pack's
// lat-long HDR skies blended by weight (WorldClock and StylizedSky pick them by the hour and the weather), Built-in pipeline, gamma
// space. Each sky is turned so its painted sun stands where the game's sun light comes from (_SunAzimuth; _SunU*: where the sun is
// in each picture); a painted moon a sky may carry is painted over with the sky beside it (_Hide*), since the moons are TwinMoons'
// (The Eye and The Tear, CANON). The zone keeps its own air: a tint (_Tint), a haze of the fog colour from the horizon up (_Haze:
// the Lowtowns' yellow smog), and below the horizon the fog itself (_Ground), so land seen past the backdrop fades into fog.
// The pictures are linear HDR; the project is in gamma space, so the mix is taken to gamma at the end.
Shader "Crulanda/StylizedSky"
{
    Properties
    {
        _TexA ("Sky A (lat-long HDR)", 2D) = "black" {}
        _TexB ("Sky B", 2D) = "black" {}
        _TexC ("Sky C", 2D) = "black" {}
        _Weights ("Weights of A, B, C", Vector) = (1, 0, 0, 0)
        _SunU ("Where each sky's sun is across the picture (u of A, B, C)", Vector) = (0, 0, 0, 0)
        _SunAzimuth ("The game's sun bearing (degrees, Unity yaw)", Float) = 0
        _HideA ("A's painted moon to hide (u, v in the picture; radius in degrees; 0: none)", Vector) = (0, 0, 0, 0)
        _HideB ("B's painted moon to hide", Vector) = (0, 0, 0, 0)
        _HideC ("C's painted moon to hide", Vector) = (0, 0, 0, 0)
        _Exposure ("Exposure", Range(0, 4)) = 1
        _Tint ("Zone tint (multiplied)", Color) = (1, 1, 1, 1)
        _Ground ("The fog: below the horizon, and the haze's colour", Color) = (0.6, 0.6, 0.6, 1)
        _Haze ("Haze from the horizon up (0: none, 1: thick smog)", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _TexA, _TexB, _TexC;
            float4 _Weights, _SunU, _HideA, _HideB, _HideC;
            float _SunAzimuth, _Exposure, _Haze;
            fixed4 _Tint, _Ground;
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.dir = v.vertex.xyz; return o; }
            // Unity's panoramic layout (as Skybox/Panoramic reads it): u from the bearing, v from the height; turned so the picture's
            // sun (sunU) lies on the game sun's bearing. Within a hidden moon's disc the picture is read from beside it.
            float3 Sample(sampler2D tex, float3 d, float sunU, float4 hide)
            {
                float yaw = degrees(atan2(d.x, d.z));                       // Unity yaw: 0 along +z, 90 along +x
                float u = frac((yaw - _SunAzimuth) / 360.0 + sunU);
                float v = 1.0 - acos(clamp(d.y, -1, 1)) / UNITY_PI;
                if (hide.z > 0)
                {
                    float du = abs(u - hide.x); du = min(du, 1 - du);
                    float dist = length(float2(du * 360 * cos((v - 0.5) * UNITY_PI), (v - hide.y) * 180));   // degrees on the sky
                    u = frac(u + (1 - smoothstep(hide.z, hide.z + 1.5, dist)) * 0.04);
                }
                return tex2Dlod(tex, float4(u, v, 0, 0)).rgb;            // one level (no mips): no seam where u wraps
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 c = 0;
                if (_Weights.x > 0.001) c += _Weights.x * Sample(_TexA, d, _SunU.x, _HideA);
                if (_Weights.y > 0.001) c += _Weights.y * Sample(_TexB, d, _SunU.y, _HideB);
                if (_Weights.z > 0.001) c += _Weights.z * Sample(_TexC, d, _SunU.z, _HideC);
                c *= _Exposure;
                #ifdef UNITY_COLORSPACE_GAMMA
                c = LinearToGammaSpace(max(c, 0));
                #endif
                c *= _Tint.rgb;
                // The zone's air: haze of the fog colour thickest at the horizon, reaching higher the thicker it is; the fog below.
                float up = d.y;
                float haze = _Haze * (1 - smoothstep(0, 0.12 + 0.6 * _Haze, up));
                c = lerp(c, _Ground.rgb, saturate(haze));
                c = lerp(c, _Ground.rgb, smoothstep(0.0, -0.06, up));
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
