// Crulanda moons (TwinMoons): a moon's painted disc on a quad far out in the sky, faced to the camera. Unlit, no scene fog,
// blended over the sky by its own alpha times _Color.a (how far the night has come); drawn before the clouds (Transparent-100),
// so cloud passes over it. In Resources so a build always has it.
Shader "Crulanda/Moon"
{
    Properties
    {
        _MainTex ("Disc", 2D) = "white" {}
        _Color ("Tint (alpha: how much shows)", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-150" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Cull Off ZWrite Off ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _Color;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy; return o; }
            fixed4 frag(v2f i) : SV_Target { fixed4 c = tex2D(_MainTex, i.uv) * _Color; return c; }
            ENDCG
        }
    }
}
