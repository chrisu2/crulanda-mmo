// Crulanda post-processing for the built-in pipeline (camera OnRenderImage):
// 0 bright-pass prefilter, 1 blur down, 2 blur up (additive), 3 sun shafts (radial blur toward the sun),
// 4 composite: cloud shadows, bloom + shafts, exposure, ACES filmic tone map, saturation/contrast/tint grade (firelight
//   kept warm at night), vignette; 5 sun: the sun's disc and warm halo added to the sky in HDR before the bloom and the
//   shafts take the frame (playtest note 9), so trees and roofs in front of it cut it and its rays.
Shader "Hidden/Crulanda/Post"
{
    Properties { _MainTex ("", 2D) = "white" {} }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex; float4 _MainTex_TexelSize;
    sampler2D _Bloom; sampler2D _Shafts;
    float _Threshold, _Knee, _BloomIntensity, _ShaftIntensity, _Exposure, _Contrast, _Saturation, _Vignette, _Lift;
    float4 _Tint, _SunScreen;
    // The sun pass (ZonePost.Sun): direction to the sun (world), its colour * strength, x disc, y halo, z wide glow, w veil.
    float4 _SunDir, _SunColor, _SunShape;
    // Cloud shadows (ZonePost.CloudShadows): x strength (0: off), y cloud cover, z edge softness.
    UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
    sampler2D _CloudTex; float4 _CloudShadow, _CloudDrift, _CloudSun, _CamPos, _RayBL, _RayBR, _RayTL, _RayTR, _CloudFog; float _CloudHeight, _CloudTile;
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
    // The view ray through this pixel (world, unnormalised) from the frame's corner rays.
    float3 ViewRay(float2 suv) { return lerp(lerp(_RayBL.xyz, _RayBR.xyz, suv.x), lerp(_RayTL.xyz, _RayTR.xyz, suv.x), suv.y); }
    float2 ScreenUV(float2 uv)
    {
        float2 suv = uv;
        #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0) suv.y = 1 - suv.y;
        #endif
        return suv;
    }
    // The sun on the sky: a hot disc about two degrees across with a soft rim (painted, not a pin-prick), a warm halo and a
    // wide glow, only where the sky shows (the depth texture's far plane), so whatever stands in front cuts it; the bloom
    // spreads it back over their edges and the shafts streak it through the gaps.
    half4 fragSun(v2f i) : SV_Target
    {
        half3 c = tex2D(_MainTex, i.uv).rgb;
        float2 suv = ScreenUV(i.uv);
        float raw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, suv);
        float sky = smoothstep(0.96, 0.995, Linear01Depth(raw));
        float cosA = dot(normalize(ViewRay(suv)), _SunDir.xyz), ang = acos(clamp(cosA, -1, 1));
        float disc = 1 - smoothstep(0.014, 0.019, ang);
        float glow = _SunShape.x * disc + _SunShape.y * exp(-ang / 0.06) + _SunShape.z * exp(-ang / 0.3);
        return half4(c + _SunColor.rgb * glow * sky, 1);
    }
    half3 ACES(half3 x) { return saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14)); }
    // How much sunlight reaches this pixel's ground point past the cloud layer (1 = all). The point comes from the depth
    // texture and the frame's corner rays; the cloud is the one toward the sun from it, cut from the sky's own noise, cover
    // and drift (Crulanda/Clouds), so the shadows move with the clouds overhead. The sky, and far land in the haze, keep theirs.
    half CloudLight(float2 uv)
    {
        float2 suv = ScreenUV(uv);
        float raw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, suv);
        if (Linear01Depth(raw) > 0.98) return 1;
        float eye = LinearEyeDepth(raw);
        float3 ray = ViewRay(suv);
        float3 p = _CamPos.xyz + ray * eye;
        float2 at = (p.xz + _CloudSun.xz / max(_CloudSun.y, 0.25) * (_CloudHeight - p.y) - _CloudDrift.xy) / _CloudTile;
        float n = tex2D(_CloudTex, at).r * 0.65 + tex2D(_CloudTex, at * 2.7 + 0.37).r * 0.35;
        float edge = 1 - _CloudShadow.y;
        float cover = smoothstep(edge - _CloudShadow.z, edge + _CloudShadow.z, n);
        float land = saturate((_CloudFog.y - eye) / max(_CloudFog.y - _CloudFog.x, 1));   // the shade is on the land, not on the fog in front of it
        return 1 - _CloudShadow.x * cover * saturate(1.3 - eye / 260) * land;
    }
    half4 fragComposite(v2f i) : SV_Target
    {
        half3 c = tex2D(_MainTex, i.uv).rgb;
        if (_CloudShadow.x > 0.001) c *= CloudLight(i.uv);
        c += tex2D(_Bloom, i.uv).rgb * _BloomIntensity + tex2D(_Shafts, i.uv).rgb * _ShaftIntensity;
        c += _SunColor.rgb * _SunShape.w;   // veiling glare: looking into the sun washes a little light over the whole frame
        c = ACES(c * _Exposure);
        half l = dot(c, half3(0.2126, 0.7152, 0.0722));
        // Firelight after dark (_Lift = darkness): bright warm pixels (lit windows, lamp glass, the ground under a lamp) keep
        // their colour and take a warm tint, not the night's cool one, so they read as fire and not as cold white. None by day.
        half fire = _Lift * smoothstep(0.3, 0.7, l) * saturate((c.r - c.b) * 4);
        c = lerp(l.xxx, c, lerp(_Saturation, max(_Saturation, 1.15), fire));
        // Contrast about mid-grey. By day: the straight line, with a small quadratic toe only where it would clip to black
        // (shadows stay crisp and saturated). Toward night (_Lift = darkness): a power toe that lifts the darks, so faces
        // turned from the moon stay readable. Both match the line above mid-grey.
        float c0 = 0.5 - 0.5 / max(_Contrast, 1.0001);
        half3 lin = (c - 0.5) * _Contrast + 0.5;
        half3 dayC = c < 2 * c0 ? _Contrast * c * c / (4 * c0) : lin;
        half3 nightC = c > 0.5 ? lin : 0.5 * pow(max(c * 2, 1e-4), _Contrast);
        c = lerp(dayC, nightC, _Lift);
        c = saturate(c) * lerp(_Tint.rgb, half3(1.04, 0.96, 0.84), fire);
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
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragSun
            ENDCG }
    }
}
