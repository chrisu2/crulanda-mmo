// Crulanda leaf: painted leaf-cluster and pine-bough cards (ZoneBuilder.LeafCrown, Pine, the Great Oak's fringe). Alpha-cut,
// lit by the Standard model like the rest of the tree, drawn on both faces. Two-sided lighting comes from the mesh, not from
// flipping: ZoneMeshes.Cards gives every card the normal of the round crown's surface at that spot (away from the crown's heart,
// turned a little up), so a card's back shades like its front and a card facing away from the sun is not black; flipping on the
// back face would break that. The far edge of a card sways a little in the wind (vertex colour alpha = how much; the phase is
// the world position, so it works static-batched and needs no per-instance data). Albedo = the paint x _Color x the vertex
// colour (a lighter or darker cluster, a darker pine tier: one material per leaf family), greyed by _Wither in gloom zones.
// Its ShadowCaster pass keeps the cutout and the sway, so shadows and the camera's depth texture hold the leaves' shape.
// TreeFade swaps these cards onto Crulanda/Fade, which reads the same properties, so a faded crown keeps its shape and sway.
Shader "Crulanda/Leaf"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Leaf cluster (alpha = leaf)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.45
        _Glossiness ("Smoothness", Range(0, 1)) = 0.05
        _Wither ("Wither (grey the paint)", Range(0, 1)) = 0
        _VertexTint ("Vertex colour tints (1) or not (0)", Float) = 1
        _Wind ("Wind strength", Float) = 0.07
        _WindSpeed ("Wind speed", Float) = 1.4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
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
        // The wind: a slow gust and a quicker flutter by world position (the same for a batched or an unbatched tree), moving the
        // vertex by weight (the vertex colour's alpha: 0 at the bough, 1 at the leaf tips) in world metres.
        void LeafSway(inout float4 vertex, float weight)
        {
            float3 wp = mul(unity_ObjectToWorld, vertex).xyz;
            float t = _Time.y * _WindSpeed;
            float gust = sin(t + wp.x * 0.23 + wp.z * 0.19) * 0.6 + sin(t * 1.9 + wp.x * 0.9 - wp.y * 0.6) * 0.4;
            float lift = sin(t * 1.3 + wp.z * 0.7 + wp.x * 0.4) * 0.35;
            float3 sway = float3(gust, lift, gust * 0.55) * (_Wind * weight);
            vertex.xyz += mul((float3x3)unity_WorldToObject, sway);
        }
        // The painted colour of a card pixel: the texture, greyed by _Wither, in the material's and the vertex's tint.
        fixed3 LeafPaint(fixed3 tex, fixed3 vertexTint)
        {
            fixed grey = dot(tex, fixed3(0.3, 0.59, 0.11));
            return lerp(tex, grey.xxx, _Wither) * _Color.rgb * lerp(fixed3(1, 1, 1), vertexTint, _VertexTint);
        }
        ENDCG

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        half _Glossiness;
        struct Input { float2 uv_MainTex; float4 color : COLOR; };
        void vert(inout appdata_full v) { LeafSway(v.vertex, v.color.a); }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            clip(c.a - _Cutoff);
            o.Albedo = LeafPaint(c.rgb, IN.color.rgb);
            o.Smoothness = _Glossiness;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG

        // Shadow caster (the sun's shadow map, lamps' cube maps and the camera's depth texture): the same sway and cutout.
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
            struct v2f { V2F_SHADOW_CASTER; float2 uv : TEXCOORD1; };
            v2f vert(appdata_full v)
            {
                v2f o;
                LeafSway(v.vertex, v.color.a);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                clip(tex2D(_MainTex, i.uv).a - _Cutoff);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Legacy Shaders/Transparent/Cutout/Diffuse"
}
