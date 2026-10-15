// Crulanda candle flame (built-in pipeline, one plain vertex/fragment pass): the flame on a candle, a torch or a fire-bowl,
// glowing and wavering. Stands in for the Shader Graph "Candle Shader" Chris downloaded (2026-10-15 01:20: "i have downloaded
// some shaders: stylized water, interactive grass, and candle flicker. use these where appropriate"; the project is on the
// built-in pipeline, no Shader Graph). That graph summed three sines of different speeds (6, 28 and 40 a second), seeded per
// object, into an emission that never fell under a floor; its script spun the flame and breathed its height. Here:
// - The glow is _EmissionColor (the Glowing material's: it carries over when CandleFlame swaps this shader in) times _Flicker,
//   which CandleFlame sets each frame from the lamp light's Flicker, so the flame and the pool of light it throws rise and fall
//   as one; a flame with no light wavers on the same curve by itself.
// - The quick jitter a candle has on top of that waver is the pack's three sines, seeded from where the flame stands, kept small
//   (_Jitter) and never under a floor.
// - The flame breathes: its height sways by _Wobble (the pack's script scaled it 1 to 1.2) and its tip leans a little, from the
//   base up (the cone stands on its origin, the sphere round it: nothing below the base moves).
// - Unlit: the tongue's own colour gets only the sky's ambient, so by day it reads as a pale tongue, not a lit lump. No shadow
//   pass: a flame throws no shadow. Opaque, in the geometry queue, like the Standard material it replaces.
// Compiled only in Unity: CrulandaShaders uses it only when it is supported, so the old look stays if it ever fails.
Shader "Crulanda/CandleFlame"
{
    Properties
    {
        _Color ("Tongue", Color) = (1, 0.74, 0.32, 1)
        _EmissionColor ("Glow (HDR)", Color) = (2.6, 1.9, 0.8, 1)
        _Flicker ("Flicker now (CandleFlame sets it)", Float) = 1
        _Jitter ("Quick jitter", Range(0, 1)) = 0.18
        _Wobble ("Breathing height", Range(0, 0.5)) = 0.14
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            fixed4 _Color;
            float4 _EmissionColor;   // HDR: the Glowing material's glow runs past 1
            float _Flicker, _Jitter, _Wobble;
            struct v2f { float4 pos : SV_POSITION; float3 normal : TEXCOORD0; float3 view : TEXCOORD1; float seed : TEXCOORD2; };
            v2f vert(appdata_base v)
            {
                v2f o;
                // This flame's own time, seeded from where it stands: a row of candles never breathes in step.
                float3 at = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                o.seed = frac(dot(at, float3(12.9898, 78.233, 37.719))) * 6.2832;
                float t = _Time.y + o.seed;
                // Breathing: the height sways, and the tip leans, more the higher up; nothing below the base moves.
                float h = max(v.vertex.y, 0);
                v.vertex.y *= 1 + _Wobble * (0.6 * sin(t * 4.5) + 0.4 * sin(t * 7.3 + 1.7));
                v.vertex.x += 0.12 * h * sin(t * 2.7);
                v.vertex.z += 0.08 * h * sin(t * 3.4 + 0.9);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = _WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y + i.seed;
                // The pack's three sines (6.09, 28.2 and 40 a second, amplitudes 6.78, 5.35 and 2.36) brought to 0..1, then kept
                // small: the candle's quick jitter over the slow waver the light shares.
                float j = (sin(t * 6.09) * 6.78 + sin(t * 28.2) * 5.35 + sin(t * 40.0) * 2.36) / 14.49 * 0.5 + 0.5;
                float glow = max(_Flicker, 0.3) * lerp(1, 0.75 + 0.5 * j, _Jitter);
                float3 N = normalize(i.normal), V = normalize(i.view);
                // Brightest where the tongue faces us, a touch darker at its edges, so it reads as a shape and not a flat cut-out.
                float face = 0.7 + 0.3 * saturate(dot(N, V));
                float3 col = _Color.rgb * ShadeSH9(float4(N, 1)) * 0.35 + _EmissionColor.rgb * glow * face;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Color"
}
