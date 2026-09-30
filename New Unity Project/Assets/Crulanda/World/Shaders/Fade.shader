// Crulanda fade: what a tree turns into while it stands between the camera and the player (TreeFade). The same Standard
// lighting as the tree (so nothing jumps in brightness when it swaps), with a screen-door dither: only _Color.a of the
// pixels are drawn, in a fixed 4x4 pattern, so it stays in the opaque pass (depth, fog, received shadows) and you see
// through the gaps. Its ShadowCaster pass keeps the whole tree in the sun's shadow map, but only the drawn pixels in the
// camera's depth texture (see the pass below).
Shader "Crulanda/Fade"
{
    Properties
    {
        _Color ("Colour (alpha = how much of the tree shows)", Color) = (1, 1, 1, 0.25)
        _MainTex ("Albedo", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        struct Input { float2 uv_MainTex; float4 screenPos; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // 4x4 ordered dither threshold for this screen pixel.
            float2 px = fmod(floor(IN.screenPos.xy / max(IN.screenPos.w, 1e-4) * _ScreenParams.xy), 4);
            float4 row = px.y < 1 ? float4(0, 8, 2, 10) : px.y < 2 ? float4(12, 4, 14, 6) : px.y < 3 ? float4(3, 11, 1, 9) : float4(15, 7, 13, 5);
            float threshold = (dot(row, float4(px.x < 1, px.x >= 1 && px.x < 2, px.x >= 2 && px.x < 3, px.x >= 3)) + 0.5) / 16;
            clip(_Color.a - threshold);
            o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
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
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct v2f { V2F_SHADOW_CASTER; float4 screen : TEXCOORD1; };
            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.screen = ComputeScreenPos(UnityObjectToClipPos(v.vertex));   // as the surface pass's screenPos, so the dither matches
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                #if !defined(SHADOWS_CUBE)
                if (UNITY_MATRIX_P[3][3] < 0.5)   // perspective: the camera's depth texture, not the sun's (orthographic) shadow map
                {
                    float2 px = fmod(floor(i.screen.xy / max(i.screen.w, 1e-4) * _ScreenParams.xy), 4);
                    float4 row = px.y < 1 ? float4(0, 8, 2, 10) : px.y < 2 ? float4(12, 4, 14, 6) : px.y < 3 ? float4(3, 11, 1, 9) : float4(15, 7, 13, 5);
                    float threshold = (dot(row, float4(px.x < 1, px.x >= 1 && px.x < 2, px.x >= 2 && px.x < 3, px.x >= 3)) + 0.5) / 16;
                    clip(_Color.a - threshold);
                }
                #endif
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Legacy Shaders/VertexLit"
}
