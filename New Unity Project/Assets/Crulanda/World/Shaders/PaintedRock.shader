// Crulanda painted rock (the painted style pass, part 2): crags, cliffs, boulders and the backdrop's mountains. The paint is
// projected in world space on three axes (the rock meshes are blobs whose own UVs stretch), so strata run level across a whole
// cliff of separate lumps and hold their scale on any size of rock. Upward faces take a top light (_Top: warm in the green
// zones, neutral in ash and gloom, a small lift in the mountains; ZoneBuilder.RockTint sets it per zone) and undersides sink
// into shade, the way a painter blocks a rock in: light top, mid side, dark foot.
// Lit by the Standard model like everything else; works static-batched (world position and world normal only, no per-object
// data). The shadow caster comes from the fallback.
Shader "Crulanda/PaintedRock"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Painted rock (tiles in world metres)", 2D) = "white" {}
        _Scale ("Metres per tile", Float) = 5
        // A Vector, not a Color: no colour-space conversion and no clamp on values above 1.
        _Top ("Top light (multiplies upward faces; set per zone by ZoneBuilder.RockTint)", Vector) = (1.18, 1.14, 1.02, 1)
        _TopSharp ("Top sharpness", Range(1, 8)) = 2.5
        _Shade ("Underside shade", Range(0, 1)) = 0.4
        _Glossiness ("Smoothness", Range(0, 1)) = 0.04
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half4 _Top;
        float _Scale;
        half _TopSharp, _Shade, _Glossiness;
        // surf never writes o.Normal, so the built-in worldNormal is the interpolated world normal as it stands.
        struct Input { float3 worldPos; float3 worldNormal; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = normalize(IN.worldNormal);
            float3 w = abs(n); w *= w; w *= w; w /= max(w.x + w.y + w.z, 1e-4);
            float3 p = IN.worldPos / max(_Scale, 0.01);
            // Side projections keep world height as v, so the strata lie level. The top takes two turned samples, so the beds do
            // not show as parallel stripes along world X on every flat rock in the zone.
            float2 t = float2(p.x * 0.8 + p.z * 0.6, p.z * 0.8 - p.x * 0.6) * 0.6;
            float2 t2 = float2(t.x * 0.34 - t.y * 0.94, t.x * 0.94 + t.y * 0.34) * 1.37 + 0.31;
            fixed3 top = (tex2D(_MainTex, t).rgb + tex2D(_MainTex, t2).rgb) * 0.5;
            fixed3 paint = tex2D(_MainTex, p.zy).rgb * w.x + top * w.y + tex2D(_MainTex, p.xy).rgb * w.z;
            half3 c = paint * _Color.rgb;
            c = lerp(c, c * _Top.rgb, pow(saturate(n.y), _TopSharp));
            c *= 1 - _Shade * saturate(-n.y);
            o.Albedo = c; o.Metallic = 0; o.Smoothness = _Glossiness; o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
