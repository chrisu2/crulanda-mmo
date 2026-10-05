// Crulanda distant ranges (DistantRanges): far hills and mountains past the zone's backdrop, drawn flat in the haze's own colours
// as aerial perspective would: the base of each range lost in the fog colour, the crest a step darker and bluer. No scene fog
// (they stand past the fog's end; the colour is the fog's already) and no lighting. In Resources so a build always has it.
Shader "Crulanda/Distant"
{
    Properties
    {
        _Color ("Crest colour", Color) = (0.4, 0.48, 0.58, 1)
        _Haze ("Haze colour (the fog)", Color) = (0.6, 0.72, 0.85, 1)
        _Base ("Height where the range is lost in haze (world y)", Float) = 0
        _Crest ("Height where it shows its own colour (world y)", Float) = 80
    }
    SubShader
    {
        Tags { "Queue" = "Geometry+10" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color, _Haze; float _Base, _Crest;
            struct v2f { float4 pos : SV_POSITION; float y : TEXCOORD0; };
            v2f vert(float4 v : POSITION) { v2f o; o.pos = UnityObjectToClipPos(v); o.y = mul(unity_ObjectToWorld, v).y; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float k = smoothstep(_Base, _Crest, i.y);
                return fixed4(lerp(_Haze.rgb, _Color.rgb, k), 1);
            }
            ENDCG
        }
    }
}
