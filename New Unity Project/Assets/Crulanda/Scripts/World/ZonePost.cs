using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Camera post-processing for generated zones (built-in pipeline, HDR camera):
    /// - bloom on lamps, windows, embers and the sun;
    /// - sun shafts when the sun is in view;
    /// - a filmic (ACES) tone map;
    /// - colour grading that follows the zone's biome and the hour (cooler and less saturated at night);
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
                case "mountain": return new Grade { saturation = 1.02f, contrast = 1.14f, exposure = .98f, vignette = .55f, bloom = .5f, threshold = 1.05f, tint = new Color(.97f, 1, 1.04f) };
                case "ash": return new Grade { saturation = .78f, contrast = 1.16f, exposure = .95f, vignette = .95f, bloom = .6f, threshold = .95f, tint = new Color(1.05f, .97f, .9f) };
                default: return new Grade { saturation = 1.18f, contrast = 1.14f, exposure = 1f, vignette = .6f, bloom = .55f, threshold = 1f, tint = new Color(1.03f, 1, .95f) };
            }
        }
        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null) { Graphics.Blit(src, dst); return; }
            var g = ForBiome(); float dark = WorldClock.Darkness;
            // Night: lamps bloom more, colour drains and cools; dusk and dawn warm up a touch.
            float dusk = Mathf.Clamp01(1 - Mathf.Abs(dark - .5f) * 2);
            mat.SetFloat("_Threshold", Mathf.Lerp(g.threshold, .55f, dark)); mat.SetFloat("_Knee", .5f);
            mat.SetFloat("_BloomIntensity", Mathf.Lerp(g.bloom, 1.1f, dark));
            mat.SetFloat("_Exposure", Mathf.Lerp(g.exposure, 1.2f, dark));
            mat.SetFloat("_Saturation", Mathf.Lerp(g.saturation, .62f, dark)); mat.SetFloat("_Contrast", g.contrast);
            mat.SetFloat("_Vignette", g.vignette + dark * .4f);
            mat.SetColor("_Tint", Color.Lerp(Color.Lerp(g.tint, new Color(.86f, .92f, 1.12f), dark), new Color(1.08f, .96f, .88f), dusk * .6f));
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
                    shaftStrength = onScreen * (1 - dark) * .45f;
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
        }
    }
}
