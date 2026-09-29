// Crulanda post-processing for the built-in pipeline (camera OnRenderImage):
// 0 bright-pass prefilter, 1 blur down, 2 blur up (additive), 3 sun shafts (radial blur toward the sun),
// 4 composite: bloom + shafts, exposure, ACES filmic tone map, saturation/contrast/tint grade, vignette.
Shader "Hidden/Crulanda/Post"
{
    Properties { _MainTex ("", 2D) = "white" {} }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex; float4 _MainTex_TexelSize;
    sampler2D _Bloom; sampler2D _Shafts;
    float _Threshold, _Knee, _BloomIntensity, _ShaftIntensity, _Exposure, _Contrast, _Saturation, _Vignette;
    float4 _Tint, _SunScreen;
    struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
    v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }
    half3 Box(float2 uv, float d)
    {
        float4 o = _MainTex_TexelSize.xyxy * float4(-d, -d, d, d);
        return (tex2D(_MainTex, uv + o.xy).rgb + tex2D(_MainTex, uv + o.zy).rgb + tex2D(_MainTex, uv + o.xw).rgb + tex2D(_MainTex, uv + o.zw).rgb) * 0.25;
    }
    // Karis average: weight each tap by 1/(1+brightness) so a single hot pixel (a water glint) cannot flare into a
    // blinking bloom blob; clamp the result as well.
    half3 Karis(float2 uv)
    {
        float4 o = _MainTex_TexelSize.xyxy * float4(-1, -1, 1, 1);
        half3 a = tex2D(_MainTex, uv + o.xy).rgb, b = tex2D(_MainTex, uv + o.zy).rgb, c = tex2D(_MainTex, uv + o.xw).rgb, d = tex2D(_MainTex, uv + o.zw).rgb;
        half wa = 1 / (1 + max(a.r, max(a.g, a.b))), wb = 1 / (1 + max(b.r, max(b.g, b.b))), wc = 1 / (1 + max(c.r, max(c.g, c.b))), wd = 1 / (1 + max(d.r, max(d.g, d.b)));
        return min((a * wa + b * wb + c * wc + d * wd) / (wa + wb + wc + wd), 6);
    }
    half4 fragPrefilter(v2f i) : SV_Target
    {
        half3 c = Karis(i.uv);
        half br = max(c.r, max(c.g, c.b));
        half soft = clamp(br - _Threshold + _Knee, 0, 2 * _Knee); soft = soft * soft / (4 * _Knee + 1e-4);
        half contrib = max(soft, br - _Threshold) / max(br, 1e-4);
        return half4(c * contrib, 1);
    }
    half4 fragDown(v2f i) : SV_Target { return half4(Box(i.uv, 1), 1); }
    half4 fragUp(v2f i) : SV_Target { return half4(Box(i.uv, 0.5), 1); }
    half4 fragShafts(v2f i) : SV_Target
    {
        float2 step = (_SunScreen.xy - i.uv) / 28; float2 uv = i.uv; half3 acc = 0; half w = 1;
        for (int k = 0; k < 28; k++) { acc += tex2D(_MainTex, uv).rgb * w; w *= 0.93; uv += step; }
        return half4(acc * (_SunScreen.z / 11), 1);
    }
    half3 ACES(half3 x) { return saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14)); }
    half4 fragComposite(v2f i) : SV_Target
    {
        half3 c = tex2D(_MainTex, i.uv).rgb;
        c += tex2D(_Bloom, i.uv).rgb * _BloomIntensity + tex2D(_Shafts, i.uv).rgb * _ShaftIntensity;
        c = ACES(c * _Exposure);
        half l = dot(c, half3(0.2126, 0.7152, 0.0722));
        c = lerp(l.xxx, c, _Saturation);
        c = saturate((c - 0.5) * _Contrast + 0.5) * _Tint.rgb;
        float2 v = i.uv - 0.5; c *= saturate(1 - dot(v, v) * _Vignette);
        return half4(c, 1);
    }
    ENDCG
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragPrefilter
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDown
            ENDCG }
        Pass { Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragUp
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragShafts
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragComposite
            ENDCG }
    }
}
