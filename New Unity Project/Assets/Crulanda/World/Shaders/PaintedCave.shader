// Crulanda painted cave (the painted style pass): the inside of a walk-in cave (ZoneBuilder.CaveLining and the rock, earth
// and dripstone set against it). The paint is projected in world space on three axes, as on Crulanda/PaintedRock, so the beds
// run level round every bend of the passage and on from the wall onto a ledge or a fallen rock. Over it lies the shading the
// builder painted into the vertices (dark at the wall's foot and in the hollows, lighter on what stands proud, in warm and
// cool patches); a mesh with no vertex colours takes the paint as it is. Ledges take a small top light and the roof sinks a
// little: there is no sky down here, so neither is set per zone.
// Lit by the Standard model like everything else (torches, fires and sap-light do the rest). The shadow caster comes from the
// fallback.
Shader "Crulanda/PaintedCave"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Painted rock or earth (tiles in world metres)", 2D) = "white" {}
        _Scale ("Metres per tile", Float) = 3.6
        // A Vector, not a Color: no colour-space conversion and no clamp on values above 1.
        _Top ("Ledge light (multiplies upward faces)", Vector) = (1.14, 1.12, 1.06, 1)
        _TopSharp ("Ledge sharpness", Range(1, 8)) = 2
        _Shade ("Roof shade", Range(0, 1)) = 0.28
        _Glossiness ("Smoothness", Range(0, 1)) = 0.03
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
        struct Input { float3 worldPos; float3 worldNormal; fixed4 color : COLOR; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = normalize(IN.worldNormal);
            float3 w = abs(n); w *= w; w *= w; w /= max(w.x + w.y + w.z, 1e-4);
            float3 p = IN.worldPos / max(_Scale, 0.01);
            // Side projections keep world height as v, so the beds lie level. Ledges and the roof take two turned samples, so the
            // beds do not show as parallel stripes along world X.
            float2 t = float2(p.x * 0.8 + p.z * 0.6, p.z * 0.8 - p.x * 0.6) * 0.6;
            float2 t2 = float2(t.x * 0.34 - t.y * 0.94, t.x * 0.94 + t.y * 0.34) * 1.37 + 0.31;
            fixed3 top = (tex2D(_MainTex, t).rgb + tex2D(_MainTex, t2).rgb) * 0.5;
            fixed3 paint = tex2D(_MainTex, p.zy).rgb * w.x + top * w.y + tex2D(_MainTex, p.xy).rgb * w.z;
            half3 c = paint * _Color.rgb * IN.color.rgb;
            c = lerp(c, c * _Top.rgb, pow(saturate(n.y), _TopSharp));
            c *= 1 - _Shade * saturate(-n.y);
            o.Albedo = c; o.Metallic = 0; o.Smoothness = _Glossiness; o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
