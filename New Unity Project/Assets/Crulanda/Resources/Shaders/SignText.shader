// Letters burnt into a sign board (Round 22-23, 2026-10-06): the font's alpha in the text's colour, hidden behind the board like
// anything solid. The built-in font material draws over everything, so a board's back face's mirrored words showed through its
// front ("text on signs not correct").
Shader "Crulanda/SignText"
{
    Properties { _MainTex ("Font", 2D) = "white" {} _Color ("Colour", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Lighting Off Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color * _Color; o.uv = TRANSFORM_TEX(v.uv, _MainTex); UNITY_TRANSFER_FOG(o, o.pos); return o; }
            fixed4 frag(v2f i) : SV_Target { fixed4 c = i.color; c.a *= tex2D(_MainTex, i.uv).a; UNITY_APPLY_FOG(i.fogCoord, c); return c; }
            ENDCG
        }
    }
}
