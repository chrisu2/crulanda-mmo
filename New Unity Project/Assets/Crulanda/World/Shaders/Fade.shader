// Crulanda fade: what a tree turns into while it stands between the camera and the player (TreeFade). The same Standard
// lighting as the tree (so nothing jumps in brightness when it swaps), with a screen-door dither: only _Color.a of the
// pixels are drawn, in a fixed 4x4 pattern, so it stays in the opaque pass (depth, fog, received shadows) and you see
// through the gaps. Its ShadowCaster pass keeps the whole tree in the sun's shadow map, but only the drawn pixels in the
// camera's depth texture (see the pass below). Painted leaf cards (Crulanda/Leaf) keep their cutout, both faces, wind sway
// and painted shade here too (TreeFade.FadeOf copies those properties), so a faded crown keeps its ragged shape.
Shader "Crulanda/Fade"
{
    Properties
    {
        _Color ("Colour (alpha = how much of the tree shows)", Color) = (1, 1, 1, 0.25)
        _MainTex ("Albedo", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0, 1)) = 0.2
        // Leaf cards (Crulanda/Leaf) keep their cutout, both faces, wind and painted shade while faded: TreeFade copies these.
        _Cutoff ("Alpha cutoff (0: none)", Range(0, 1)) = 0
        _Wither ("Wither (grey the paint)", Range(0, 1)) = 0
        _VertexTint ("Vertex colour tints (1) or not (0)", Float) = 0
        _Wind ("Wind strength", Float) = 0
        _WindSpeed ("Wind speed", Float) = 1.4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
        LOD 200
        Cull [_Cull]

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        fixed4 _Color;
        half _Cutoff, _Wither, _VertexTint;
        float _Wind, _WindSpeed;
        // 4x4 ordered dither threshold for a screen position (as ComputeScreenPos gives it): only _Color.a of the pixels pass.
        float DitherThreshold(float4 screen)
        {
            float2 px = fmod(floor(screen.xy / max(screen.w, 1e-4) * _ScreenParams.xy), 4);
            float4 row = px.y < 1 ? float4(0, 8, 2, 10) : px.y < 2 ? float4(12, 4, 14, 6) : px.y < 3 ? float4(3, 11, 1, 9) : float4(15, 7, 13, 5);
            return (dot(row, float4(px.x < 1, px.x >= 1 && px.x < 2, px.x >= 2 && px.x < 3, px.x >= 3)) + 0.5) / 16;
        }
        // Leaf cards' wind, exactly as Crulanda/Leaf moves them: a slow gust and a quicker flutter by world position (the same
        // batched or not), by weight (the vertex colour's alpha: 0 at the bough, 1 at the leaf tips). _Wind is 0 for anything else.
        void LeafSway(inout float4 vertex, float weight)
        {
            float3 wp = mul(unity_ObjectToWorld, vertex).xyz;
            float t = _Time.y * _WindSpeed;
            float gust = sin(t + wp.x * 0.23 + wp.z * 0.19) * 0.6 + sin(t * 1.9 + wp.x * 0.9 - wp.y * 0.6) * 0.4;
            float lift = sin(t * 1.3 + wp.z * 0.7 + wp.x * 0.4) * 0.35;
            float3 sway = float3(gust, lift, gust * 0.55) * (_Wind * weight);
            vertex.xyz += mul((float3x3)unity_WorldToObject, sway);
        }
        ENDCG

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        half _Glossiness;
        struct Input { float2 uv_MainTex; float4 screenPos; float4 color : COLOR; };
        void vert(inout appdata_full v) { LeafSway(v.vertex, v.color.a); }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            clip(min(_Color.a - DitherThreshold(IN.screenPos), c.a - _Cutoff));   // the dither, and a leaf card's cutout
            fixed grey = dot(c.rgb, fixed3(0.3, 0.59, 0.11));
            o.Albedo = lerp(c.rgb, grey.xxx, _Wither) * _Color.rgb * lerp(fixed3(1, 1, 1), IN.color.rgb, _VertexTint);
            o.Smoothness = _Glossiness;
            o.Metallic = 0;
        }
        ENDCG

        // Shadow caster, which is also what the camera's depth texture is drawn with. The sun's shadow map (orthographic) and
        // lamps' cube maps keep the whole tree, so a faded tree still shades the ground. The camera's depth texture (perspective)
        // takes only the pixels the colour pass draws: with the whole tree in it, everything behind a faded or hidden tree that
        // reads that texture was cut out along its silhouette - the Wasting's haze and curtain (soft particles), leaving a hard
        // straight seam in the sky (oakhaven-18), and the water's depth - and the ground behind took the tree's own shadow.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_shadowcaster
            float4 _MainTex_ST;
            struct v2f { V2F_SHADOW_CASTER; float4 screen : TEXCOORD1; float2 uv : TEXCOORD2; };
            v2f vert(appdata_full v)
            {
                v2f o;
                LeafSway(v.vertex, v.color.a);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.screen = ComputeScreenPos(UnityObjectToClipPos(v.vertex));   // as the surface pass's screenPos, so the dither matches
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                clip(tex2D(_MainTex, i.uv).a - _Cutoff);   // a leaf card's cutout, in every shadow map and in the depth texture (_Cutoff 0: nothing cut)
                #if !defined(SHADOWS_CUBE)
                if (UNITY_MATRIX_P[3][3] < 0.5)   // perspective: the camera's depth texture, not the sun's (orthographic) shadow map
                    clip(_Color.a - DitherThreshold(i.screen));
                #endif
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Legacy Shaders/VertexLit"
}
