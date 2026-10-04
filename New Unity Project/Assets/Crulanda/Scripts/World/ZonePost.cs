using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Camera post-processing for generated zones (built-in pipeline, HDR camera):
    /// - bloom on lamps, windows, embers and the sun;
    /// - the sun itself (playtest note 9): a disc and a warm halo on the sky in HDR, cut by whatever stands in front, so the
    ///   bloom spreads it and the shafts streak it through trees and roofs; warmest and widest low in the sky; a veil of glare
    ///   when you look into it;
    /// - sun shafts when the sun is in view, strongest at dawn and dusk;
    /// - a filmic (ACES) tone map;
    /// - colour grading that follows the zone's biome, the hour and the weather (cooler at night, with firelight kept warm;
    ///   darker and duller under rain);
    /// - a soft vignette.
    /// Shader: Hidden/Crulanda/Post, kept in builds through ZoneArt.post.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ZonePost : MonoBehaviour
    {
        Material mat; ZoneDefinition zone; Camera cam; Light sun;
        const int Levels = 5;
        readonly RenderTexture[] down = new RenderTexture[Levels];
        public void Init(Material post, ZoneDefinition definition, Light sunLight)
        {
            mat = post != null ? new Material(post) : null; zone = definition; sun = sunLight;
            cam = GetComponent<Camera>(); cam.allowHDR = true;
        }
        struct Grade { public float saturation, contrast, exposure, vignette, bloom, threshold; public Color tint; }
        Grade ForBiome()
        {
            switch (zone != null ? zone.biome : "meadow")
            {
                // Thin cold air: crisp contrast, a touch more colour and less glow, so rock, pines and the far blue ridges separate.
                case "mountain": return new Grade { saturation = 1.2f, contrast = 1.2f, exposure = .95f, vignette = .5f, bloom = .42f, threshold = 1.1f, tint = new Color(.99f, 1, 1.02f) };   // the cool cast is in the skylight (peaks.json), so sunlit faces stay warm
                // Ash: drained grey with a faint bruised-violet cast (canon sky, book1 ch.20); a sepia tint turned the ash to desert tan.
                case "ash": return new Grade { saturation = .7f, contrast = 1.24f, exposure = .97f, vignette = .85f, bloom = .45f, threshold = 1.05f, tint = new Color(.99f, .985f, 1.02f) };   // less glow and more contrast, so the pale fogged ground is not lifted to a milky mid-grey
                case "gloom": return new Grade { saturation = .86f, contrast = 1.2f, exposure = 1.04f, vignette = .85f, bloom = .65f, threshold = .9f, tint = new Color(1.04f, .96f, 1.02f) };   // Khaven: drained, rose-violet dusk; deeper darks so the lit windows and the low sun carry it
                // The Verdant Shore: rich, saturated and a little soft, lifted toward emerald, with the glow of a humid forest.
                case "verdant": return new Grade { saturation = 1.3f, contrast = 1.1f, exposure = 1.04f, vignette = .55f, bloom = .7f, threshold = .9f, tint = new Color(.97f, 1.03f, .97f) };
                default: return new Grade { saturation = 1.28f, contrast = 1.12f, exposure = 1.03f, vignette = .55f, bloom = .62f, threshold = .95f, tint = new Color(1.02f, 1.01f, .98f) };   // Oakhaven: a clear pastoral day, warm light and cool distance
            }
        }
        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null) { Graphics.Blit(src, dst); return; }
            var g = ForBiome(); float dark = WorldClock.Darkness;
            // Weather (WorldWeather): cloud flattens the light (less colour and contrast, no glare); rain cools it; an ash squall drains it.
            var weather = WorldWeather.Now; float lit = 1 - dark;
            g.saturation *= 1 - .2f * weather.dim * lit - .12f * weather.rain * lit - .08f * weather.ash; g.contrast = Mathf.Lerp(g.contrast, 1.04f, .5f * weather.dim);
            g.threshold += .35f * weather.dim; g.bloom *= 1 - .25f * weather.dim;
            g.tint = Color.Lerp(Color.Lerp(g.tint, new Color(.98f, 1, 1.02f), .6f * weather.dim * lit), new Color(.95f, 1, 1.06f), .5f * weather.rain * lit);   // no warm sun under cloud
            g.exposure *= 1 - .12f * weather.rain * lit;   // rain is a dull day, a storm a dark one (the skylight falls with it: WorldClock)
            // In a cave: the eye adjusts a little and firelight blooms.
            float under = Hollow.CameraDepth;
            g.exposure *= 1 + .3f * under; g.threshold = Mathf.Lerp(g.threshold, .6f, under); g.bloom += .3f * under; g.vignette += .25f * under;
            // Night: only lamps, windows and fires bloom, and not so far that they clip to white; the moon and the blue skylight
            // carry the night's colour (WorldClock), so the grade drains little and cools a little, and the composite keeps
            // firelight warm (_Lift); dusk and dawn warm up a touch.
            float dusk = Mathf.Clamp01(1 - Mathf.Abs(dark - .5f) * 2);
            mat.SetFloat("_Threshold", Mathf.Lerp(g.threshold, .7f, dark)); mat.SetFloat("_Knee", .5f);
            mat.SetFloat("_BloomIntensity", Mathf.Lerp(g.bloom, .8f, dark));
            mat.SetFloat("_Exposure", Mathf.Lerp(g.exposure, 1.05f, dark) * (1 + .2f * WorldWeather.Flash));
            CloudShadows(weather, dark);
            mat.SetFloat("_Saturation", Mathf.Lerp(g.saturation, .9f, dark)); mat.SetFloat("_Contrast", g.contrast); mat.SetFloat("_Lift", dark);
            mat.SetFloat("_Vignette", g.vignette + dark * .15f);
            mat.SetColor("_Tint", Color.Lerp(Color.Lerp(g.tint, new Color(.9f, .95f, 1.1f), dark), new Color(1.08f, .96f, .88f), dusk * .6f));
            // The frame's corner rays (the sun pass and the cloud shadows find each pixel's direction from them).
            Rays();
            // The sun on the sky, in HDR, before anything takes the frame.
            RenderTexture sunlit = Sun(src, g, dark);
            if (sunlit != null) src = sunlit;
            // Bloom: prefilter at half size, blur down the chain, then back up additively.
            int w = src.width / 2, h = src.height / 2;
            var format = src.format;
            down[0] = RenderTexture.GetTemporary(w, h, 0, format);
            Graphics.Blit(src, down[0], mat, 0);
            int levels = 1;
            for (int i = 1; i < Levels; i++)
            {
                w = Mathf.Max(1, w / 2); h = Mathf.Max(1, h / 2); if (w < 4 || h < 4) break;
                down[i] = RenderTexture.GetTemporary(w, h, 0, format); Graphics.Blit(down[i - 1], down[i], mat, 1); levels++;
            }
            for (int i = levels - 1; i > 0; i--) Graphics.Blit(down[i], down[i - 1], mat, 2);
            // Sun shafts: radial blur of the bright pass toward the sun's screen position, when the sun is in front and up.
            RenderTexture shafts = null; float shaftStrength = 0;
            if (sun != null && dark < .6f)
            {
                var toSun = -sun.transform.forward; var vp = cam.WorldToViewportPoint(cam.transform.position + toSun * 1000);
                if (vp.z > 0 && toSun.y > .02f)
                {
                    float onScreen = Mathf.Clamp01(1.4f - Mathf.Max(Mathf.Abs(vp.x - .5f), Mathf.Abs(vp.y - .5f)) * 2);
                    // Strongest low in the sky (dawn and dusk, when it streams through the trees), a touch even at noon.
                    float low = 1 + 1.3f * (1 - Mathf.Clamp01(toSun.y / .6f));
                    shaftStrength = onScreen * (1 - dark) * .6f * low * Mathf.Clamp01(1 - WorldWeather.Now.clouds * 1.15f) * (1 - Hollow.CameraDepth);   // cloud cover hides the sun
                    mat.SetVector("_SunScreen", new Vector4(vp.x, vp.y, 1, 0));
                    if (shaftStrength > .01f) { shafts = RenderTexture.GetTemporary(src.width / 4, src.height / 4, 0, format); Graphics.Blit(down[0], shafts, mat, 3); }
                }
            }
            mat.SetTexture("_Bloom", down[0]);
            mat.SetTexture("_Shafts", shafts != null ? shafts : (Texture)Texture2D.blackTexture);
            mat.SetFloat("_ShaftIntensity", shaftStrength);
            Graphics.Blit(src, dst, mat, 4);
            for (int i = 0; i < levels; i++) { RenderTexture.ReleaseTemporary(down[i]); down[i] = null; }
            if (shafts != null) RenderTexture.ReleaseTemporary(shafts);
            if (sunlit != null) RenderTexture.ReleaseTemporary(sunlit);
        }
        /// <summary>
        /// The sun's disc, halo and glow drawn onto the sky (pass 5) into a temporary HDR copy of the frame, or null when there is
        /// no sun to see (night, below the horizon, a closed cloud lid, a cave). Low in the sky it is warmer, its halo wider and
        /// its glare stronger; under broken cloud it dims with the cover. The veil (glare over the whole frame) is set here too.
        /// </summary>
        RenderTexture Sun(RenderTexture src, Grade g, float dark)
        {
            mat.SetVector("_SunShape", Vector4.zero); mat.SetColor("_SunColor", Color.black);
            if (sun == null || dark > .6f) return null;
            var toSun = -sun.transform.forward; if (toSun.y < -.03f) return null;
            float clear = Mathf.Clamp01(1 - WorldWeather.Now.clouds * 1.2f) * (1 - Hollow.CameraDepth) * (1 - dark / .6f);
            if (clear < .01f) return null;
            float low = 1 - Mathf.Clamp01(toSun.y / .5f);   // 1 at the horizon, 0 from about 30 degrees up
            var colour = Color.Lerp(sun.color, new Color(1, .55f, .26f), .75f * low); colour = Color.Lerp(colour, new Color(1, .97f, .9f), .2f * (1 - low));   // gold to orange as it sinks
            mat.SetVector("_SunDir", toSun);
            mat.SetColor("_SunColor", colour * clear);
            // Disc, halo, wide glow; the veil grows as the sun nears the middle of the frame.
            var vp = cam.WorldToViewportPoint(cam.transform.position + toSun * 1000);
            float facing = vp.z > 0 ? Mathf.Clamp01(1 - new Vector2(vp.x - .5f, vp.y - .5f).magnitude * 1.6f) : 0;
            mat.SetVector("_SunShape", new Vector4(4f, .35f + .45f * low, .07f + .18f * low, .012f * facing * facing * (1 + low)));   // tuned on the 82-sun shots: no milky veil, no blown sky
            var lit = RenderTexture.GetTemporary(src.width, src.height, 0, src.format);
            Graphics.Blit(src, lit, mat, 5);
            return lit;
        }
        /// <summary>Rays through the frame's corners, scaled to one metre of eye depth: ground point = camera + ray * eye depth.</summary>
        void Rays()
        {
            var t = cam.transform; mat.SetVector("_CamPos", t.position);
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), 1, Camera.MonoOrStereoscopicEye.Mono, corners);   // bottom-left, top-left, top-right, bottom-right
            mat.SetVector("_RayBL", t.TransformVector(corners[0])); mat.SetVector("_RayTL", t.TransformVector(corners[1]));
            mat.SetVector("_RayTR", t.TransformVector(corners[2])); mat.SetVector("_RayBR", t.TransformVector(corners[3]));
        }
        readonly Vector3[] corners = new Vector3[4];
        /// <summary>
        /// Cloud shadows sweeping over the land: the composite finds each pixel's ground point from the depth texture and darkens
        /// it where the cloud layer (the same noise and drift as the sky's clouds) stands between it and the sun. Strongest under
        /// broken cloud; none under a clear sky, a full overcast lid (the light is already flat), at night or with the sun low.
        /// </summary>
        void CloudShadows(WeatherLook w, float dark)
        {
            float strength = 0;
            var toSun = sun != null ? -sun.transform.forward : Vector3.down;
            if (WorldWeather.CloudNoise != null && toSun.y > 0)
            {
                float c = w.clouds;
                strength = .36f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.22f, .5f, c)) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.74f, .98f, c)))
                    * (1 - .6f * w.dim) * (1 - dark) * Mathf.Clamp01(toSun.y * 3) * (1 - Hollow.CameraDepth);
            }
            mat.SetVector("_CloudShadow", new Vector4(strength, w.clouds, .2f, 0));
            if (strength <= .001f) return;
            mat.SetTexture("_CloudTex", WorldWeather.CloudNoise);
            mat.SetVector("_CloudDrift", WorldWeather.CloudDrift); mat.SetVector("_CloudSun", toSun);
            mat.SetFloat("_CloudHeight", WorldWeather.CloudHeight); mat.SetFloat("_CloudTile", WorldWeather.CloudTile);
            mat.SetVector("_CloudFog", new Vector4(RenderSettings.fogStartDistance, RenderSettings.fogEndDistance, 0, 0));
        }
    }
}
