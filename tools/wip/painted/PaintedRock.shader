// Crulanda painted rock (the painted style pass, part 2): crags, cliffs, boulders and the backdrop's mountains. The paint is
// projected in world space on three axes (the rock meshes are blobs whose own UVs stretch), so strata run level across a whole
// cliff of separate lumps and hold their scale on any size of rock. Upward faces take a top light (sun-bleached, mossy or
// snow-dusted by zone: _Top) and undersides sink into shade, the way a painter blocks a rock in: light top, mid side, dark foot.
// Lit by the Standard model like everything else; works static-batched (world position only, no per-object data).
Shader "Crulanda/PaintedRock"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Painted rock (tiles in world metres)", 2D) = "white" {}
        _Scale ("Metres per tile", Float) = 5
        _Top ("Top light (multiplies upward faces)", Color) = (1.18, 1.14, 1.02, 1)
        _TopSharp ("Top sharpness", Range(1, 8)) = 2.5
        _Shade ("Underside shade", Range(0, 1)) = 0.4
        _Glossiness ("Smoothness", Range(0, 1)) = 0.04
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color, _Top;
        half _Scale, _TopSharp, _Shade, _Glossiness;
        struct Input { float3 worldPos; float3 wNormal; };
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.wNormal = UnityObjectToWorldNormal(v.normal);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = normalize(IN.wNormal);
            float3 w = pow(abs(n), 4); w /= (w.x + w.y + w.z);
            float3 p = IN.worldPos / _Scale;
            // Side projections keep world height as v, so the strata lie level; the top projection shows them as drifts.
            fixed3 paint = tex2D(_MainTex, p.zy).rgb * w.x + tex2D(_MainTex, p.xz).rgb * w.y + tex2D(_MainTex, p.xy).rgb * w.z;
            fixed3 c = paint * _Color.rgb;
            c = lerp(c, c * _Top.rgb, pow(saturate(n.y), _TopSharp));
            c *= 1 - _Shade * saturate(-n.y);
            o.Albedo = c; o.Metallic = 0; o.Smoothness = _Glossiness; o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
