// Crulanda water (built-in pipeline surface shader in the transparent queue): stylized, hand-painted, clear water.
// Art direction (Chris, 2026-09-29): saturated painted water. Pale teal shallows with the bed showing through, deepening
// smoothly to turquoise and then deep teal-blue; soft, slightly wobbly white foam hugging banks, rocks, posts and legs;
// soft painted ripple highlights (not photographic glints); only a gentle sky tint at a slant (no mirror, no chrome, no murk).
// How it works:
// - A named GrabPass (_WaterBackground) holds the scene behind the water. The camera depth texture gives the metres of water
//   the view ray crosses to reach it (thick) and, from that, how deep the water is there straight down (d).
// - Colour by depth: _ShallowColor -> _MidColor (by _MidDepth) -> _DeepColor (by _DeepDepth), lit like the scene (ambient
//   from above plus the sun or moon) and dimmed a little faster than it, so dusk and night water stay dark.
// - The bed shows through (a little refracted, _Refract), lifted by light scattered in the shallows (a screen blend, _Glow),
//   and fades into the depth colour at _Murk per metre (sooner at a slant).
// - Painted highlights (_Sparkle): two soft noise layers sliding past each other; faint and sparse away from the sun, crowded
//   and bright along the sun's (or moon's) reflection.
// - Foam: a soft band wherever the water touches something (from the depth texture), its width wobbling slowly, plus on still
//   water a thinner line washing in toward the shore (_FoamWaves). Only the foam is lit (LightingWater); the rest is emission.
// - The composite is written opaquely (finalcolor sets alpha 1 over premultiplied blending).
// UVs are in metres: creeks run u = across, v = along the flow (uv2.x = 1 marks flowing water), lakes use planar local x/z
// (uv2.x = 0, almost still). Vertex alpha is the shallowness under the vertex (1 at the waterline): it calms the swell near the
// shore and gives the zone map (orthographic camera, no depth texture) its depth colours. Queue Transparent-10 so splashes,
// leaves and the Wasting veil draw over the water. Needs the main camera's depth texture (ZoneBuilder turns it on).
// Zones override the palette in ZoneBuilder.WaterMaterial (waterTint, waterReflect: Khaven's dark, murky Gloom Creek).
// The project is in Gamma colour space: these colours are what the post stack (ACES, per-biome grade) starts from.
Shader "Crulanda/Water"
{
    Properties
    {
        [MainColor] _DeepColor ("Deep water (pond middles)", Color) = (0.03, 0.28, 0.44, 1)
        _MidColor ("Turquoise", Color) = (0.12, 0.62, 0.66, 1)
        _ShallowColor ("Pale shallows", Color) = (0.38, 0.92, 0.84, 1)
        _MidDepth ("Turquoise by (m deep)", Float) = 0.55
        _DeepDepth ("Deep by (m deep)", Float) = 2.2
        _Murk ("Murk (bed fades, per metre)", Float) = 1.1
        _Glow ("Light in the shallows", Range(0, 1)) = 0.75
        _BedTint ("Bed seen through the water", Color) = (0.82, 1, 1, 1)
        _FoamColor ("Foam (alpha = opacity)", Color) = (0.84, 0.92, 0.91, 0.85)
        _FoamWidth ("Foam band (m)", Float) = 0.14
        _FoamWaves ("Foam washing in", Range(0, 1)) = 0.7
        _Sparkle ("Painted highlights", Range(0, 2)) = 1
        _SkyTint ("Sky tint at a slant", Range(0, 1)) = 0.35
        _Normal ("Ripples (normal)", 2D) = "bump" {}
        _Bump ("Ripple strength", Range(0, 1)) = 0.22
        _FlowSpeed ("Creek flow (m/s)", Float) = 0.35
        _Drift ("Lake drift", Vector) = (0.012, 0.007, -0.009, 0.011)
        _Wave ("Swell height", Float) = 0.025
        _Refract ("Refraction", Range(0, 0.1)) = 0.015
        _Glare ("Brightness cap (HDR)", Float) = 1.2
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 200
        GrabPass { "_WaterBackground" }
        Cull Back
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Water alpha:premul vertex:vert finalcolor:Opaque nolightmap nometa
        #pragma target 3.0
        #include "UnityPBSLighting.cginc"
        sampler2D _Normal, _WaterBackground, _CameraDepthTexture;
        float4 _CameraDepthTexture_TexelSize;
        fixed4 _DeepColor, _MidColor, _ShallowColor, _BedTint, _FoamColor;
        float4 _Drift;
        float _MidDepth, _DeepDepth, _Murk, _Glow, _FoamWidth, _FoamWaves, _Sparkle, _SkyTint, _Bump, _FlowSpeed, _Wave, _Refract, _Glare;
        // The weather (WorldWeather, global): x cloud dimness by day, y rain. Zero without weather.
        float4 _WeatherTone;
        // Only the foam is lit here (it is the one thing on the water with a body of its own), softly so it never goes flat on
        // the shaded side; lanterns (the forward-add passes) light it too. Premultiplied by its alpha. Everything else the water
        // shows (bed, depth colour, sky tint, highlights) is emission from surf.
        half4 LightingWater(SurfaceOutputStandard s, half3 viewDir, UnityGI gi)
        {
            half soft = saturate(dot(s.Normal, gi.light.dir) * 0.7 + 0.3);
            half4 c;
            c.rgb = min(s.Albedo * (gi.light.color * soft + gi.indirect.diffuse), _Glare) * s.Alpha;
            c.a = s.Alpha;
            return c;
        }
        void LightingWater_GI(SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi) { gi = UnityGlobalIllumination(data, 1.0, s.Normal); }
        // Value noise 0..1, cells in metres (sin-free hash, stable far from the origin).
        float WaterHash(float2 p) { float3 q = frac(p.xyx * 0.1031); q += dot(q, q.yzx + 33.33); return frac((q.x + q.y) * q.z); }
        float WaterNoise(float2 p)
        {
            float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
            return lerp(lerp(WaterHash(i), WaterHash(i + float2(1, 0)), f.x), lerp(WaterHash(i + float2(0, 1)), WaterHash(i + 1), f.x), f.y);
        }
        struct Input { float2 uv_Normal; float2 flowFlag; float3 worldPos; float4 screenPos; float4 color : COLOR; float3 worldNormal; INTERNAL_DATA };
        // The surface composites itself over the grabbed scene, so it replaces what is behind it.
        void Opaque(Input IN, SurfaceOutputStandard o, inout fixed4 color) { color.a = 1; }
        // The scene behind the water at this pixel, bent by `bend` (screen units) where there is water to bend through, and
        // the metres of water the view ray crosses to reach it. Things in front of the surface are never bent.
        float3 Behind(float4 sp, float2 bend, out float thick)
        {
            float2 uv = sp.xy / sp.w;
            #if UNITY_UV_STARTS_AT_TOP
            if (_CameraDepthTexture_TexelSize.y < 0) uv.y = 1 - uv.y;
            #endif
            float t0 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv)) - sp.w;
            float2 bent = uv + bend * saturate(t0);
            thick = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, bent)) - sp.w;
            if (thick < 0) { bent = uv; thick = t0; }
            thick = max(thick, 0);
            return tex2D(_WaterBackground, bent).rgb;
        }
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.flowFlag = v.texcoord1.xy;
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            // Gentle swell, fading out toward the shore so the waterline stays put.
            v.vertex.y += (sin(wp.x * 0.8 + _Time.y * 1.3) + sin(wp.z * 0.63 + _Time.y * 1.07)) * _Wave * (1 - v.color.a);
        }
        // The water's own colour at a depth (metres): pale teal shallows, turquoise, then deep teal-blue, blending smoothly.
        float3 Ramp(float d)
        {
            float3 c = lerp(_ShallowColor.rgb, _MidColor.rgb, smoothstep(0, _MidDepth, d));
            return lerp(c, _DeepColor.rgb, smoothstep(_MidDepth * 0.5, _DeepDepth, d));
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float t = _Time.y, flowing = IN.flowFlag.x;
            float3 toCam = _WorldSpaceCameraPos - IN.worldPos;
            float dist = length(toCam);
            float3 V = toCam / max(dist, 0.001);
            // Ripples: two normal layers (tiles of about 9 m). Creeks scroll both downstream (+v) at slightly different speeds;
            // lakes drift. Calmer at a slant (steep ripples there read as blotches) and far off (there they only flicker).
            float2 uv = IN.uv_Normal * 0.11;
            float2 d1 = lerp(_Drift.xy, float2(0.02, _FlowSpeed * 0.11), flowing) * t;
            float2 d2 = lerp(_Drift.zw, float2(-0.015, _FlowSpeed * 0.07), flowing) * t;
            float2 slope = (UnpackNormal(tex2D(_Normal, uv - d1)).xy + UnpackNormal(tex2D(_Normal, uv * 1.9 - d2)).xy) * _Bump;
            slope *= lerp(0.3, 1, saturate(V.y * 2.5)) * lerp(1, 0.5, saturate(dist / 60)) * (1 + 0.6 * _WeatherTone.y);   // rain roughens it
            o.Normal = normalize(float3(slope, 1));
            float3 N = normalize(WorldNormalVector(IN, o.Normal));

            // The scene's light now (ambient from above plus the sun or moon from above). The water's own colours follow it and
            // dim a little faster than it, so dusk and night water sit darker than the land around them instead of glowing.
            float3 lightNow = ShadeSH9(float4(0, 1, 0, 1)) + _LightColor0.rgb * saturate(_WorldSpaceLightPos0.y) * 0.6;
            float dim = saturate(dot(lightNow, float3(0.333, 0.333, 0.333)) * 1.15);

            // What is behind the water, the metres of water the view ray crosses to reach it (thick), and how deep the water is
            // there straight down (d: thick scaled by the camera's height over the surface's eye depth; same ray, similar
            // triangles). The zone map (orthographic) has no depth texture: it takes depth from the vertex shallowness and shows
            // colour only (no bed, foam or highlights).
            float thick = 3, d = (1 - IN.color.a) * 1.2, show = 0;
            float3 bed = 0;
            if (unity_OrthoParams.w < 0.5)
            {
                bed = Behind(IN.screenPos, slope * _Refract, thick);
                d = thick * max(toCam.y, 0) / max(IN.screenPos.w, 0.01);
                show = 1;
            }
            float wet = saturate(thick / 0.05);   // 0 right at the waterline: the water fades in there, with no seam

            // Colour by depth (a little deeper looking at a slant).
            float3 body = Ramp(d + thick * 0.06) * lightNow * dim;
            // Clear water: the bed shows through, lifted by light scattered in the shallows (a screen blend, so even a dark silty
            // bed reads pale and bright), and fades into the depth colour at _Murk per metre (sooner at a slant).
            float3 bedSeen = saturate(bed * lerp(1, _BedTint.rgb, wet));
            float3 lifted = 1 - (1 - bedSeen) * (1 - saturate(body * _Glow * wet));
            float clear = exp2(-_Murk * lerp(d, thick, 0.3)) * show;
            float3 water = lerp(body, lifted, clear);

            // A gentle sky tint where the water is seen at a slant, from the blurred sky probe (dimmed at night by WorldClock);
            // none face-on: no mirror, no chrome.
            float3 R = reflect(-V, N);
            float3 sky = DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, float3(R.x, max(R.y, 0.05), R.z), 4), unity_SpecCube0_HDR);
            water = lerp(water, min(sky, 1), _SkyTint * pow(1 - saturate(dot(N, V)), 4) * wet);
            // Under cloud the turquoise dulls toward a cool grey-green and darkens a little: no sunny water under a rain sky.
            water = lerp(water, dot(water, float3(0.3, 0.59, 0.11)) * float3(0.9, 0.97, 1.0), _WeatherTone.x * 0.45) * (1 - 0.2 * _WeatherTone.x);

            // Painted highlights: two soft noise layers in metres sliding past each other (streaked along a creek's flow) make
            // blobs that swell and fade. Away from the sun only the brightest few show, faintly; along the sun's (or moon's)
            // reflection they crowd together and brighten. Soft-edged, anti-aliased (fade out where they would be sub-pixel)
            // and capped with the rest by _Glare: paint, not glints.
            float stretch = lerp(1, 0.45, flowing);
            float2 hp = IN.uv_Normal - lerp(float2(0.11, 0.06), float2(0.03, _FlowSpeed), flowing) * t;
            float2 hq = IN.uv_Normal - lerp(float2(-0.08, 0.1), float2(-0.02, _FlowSpeed * 0.8), flowing) * t;
            hp.y *= stretch; hq.y *= stretch;
            float h = WaterNoise(hp * 1.35) * 0.55 + WaterNoise(float2(hq.x * 0.77 - hq.y * 0.64, hq.x * 0.64 + hq.y * 0.77) * 2.1 + 3.7) * 0.45;
            float lobe = pow(saturate(dot(R, _WorldSpaceLightPos0.xyz)), 10);
            float cut = lerp(0.7, 0.56, lobe), aa = fwidth(h);
            float spark = smoothstep(cut - aa, cut + 0.06 + aa, h) * saturate(1 - aa * 4) * show * wet;
            float3 glint = _LightColor0.rgb * spark * lerp(0.14, 0.75, lobe) * _Sparkle;
            // The sun's path itself (playtest note 9: glints on water): along the narrow reflection the brightest blobs go past
            // the paint's cap, so the bloom catches them as glints, a few and soft (the bloom's prefilter keeps one pixel from flaring).
            float path = pow(saturate(dot(R, _WorldSpaceLightPos0.xyz)), 120) * spark * _Sparkle;
            float3 sunGlint = _LightColor0.rgb * path * 2.2;

            // Foam: a soft band hugging whatever the water touches (banks, rocks, posts, legs; from the depth texture), its width
            // wobbling slowly, and on still water a thinner line washing in toward the shore every few seconds (half as much on
            // creeks). It rides the flow on creeks and widens a little with distance, so it still reads far off.
            float fd = min(thick, d * 2.5);   // about how far (m) from the edge the water touches
            float2 fp = IN.uv_Normal - float2(0, _FlowSpeed * t) * flowing;
            float wob = WaterNoise(fp * 1.6 + t * 0.12) * 0.65 + WaterNoise(fp * 4.1 - t * 0.17) * 0.35;
            float band = max(_FoamWidth * (0.55 + 0.9 * wob) * (1 + dist * 0.02), 0.001);
            float foam = 1 - smoothstep(band * 0.55, band, fd);
            float phase = frac(t * 0.16 + WaterNoise(fp * 0.21 + 11.3) * 1.7);   // each stretch of shore washes in its own time
            float wash = 1 - smoothstep(band * 0.15, band * 0.4, abs(fd - band * (0.9 + (1 - phase) * 2.6)));
            wash *= smoothstep(0, 0.3, phase) * (1 - smoothstep(0.75, 1, phase)) * smoothstep(0.3, 0.55, WaterNoise(fp * 1.9 + 5.1));
            foam = max(foam, wash * 0.8 * _FoamWaves * lerp(1, 0.5, flowing));
            foam *= _FoamColor.a * lerp(0.55, 1, dim) * show;

            // Foam is white paint only by day: at night the moonlit ambient would light it brighter than the grass around the
            // pond (a glowing rim), so it greys down with the light (dim is about 1 by day, 0.4 on a moonlit night).
            o.Albedo = _FoamColor.rgb * lerp(0.35, 1, dim);
            o.Alpha = foam;                                                  // only the foam has a body; the rest is emission
            o.Emission = (min(water + glint, _Glare) + sunGlint) * (1 - foam);
            o.Smoothness = 0;
            o.Metallic = 0;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Diffuse"
}
