using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The sky from Staggart Creations' Stylized Skyboxes (Asset Store EULA; Resources/Sky/Stylized; play note 254, Chris 2026-10-19:
    /// "some better sky boxes in my assets Stylized Skyboxes | FREE"): nine painted HDR skies, three of them mixed at a time on the
    /// Crulanda/StylizedSky shader by the hour and the weather. Night, then sunrise, the day sky (clear noon, cloudy or overcast by the
    /// zone's weather), sunset, dusk and night again; a clear night shows the galaxy, and on a rare clear night where the zone allows it
    /// (ZoneLighting.aurora: the Peaks, the Ash Rim) the aurora. A zone keeps its own air (WorldClock): its sky tint, its haze (the
    /// Lowtowns' yellow smog), its fog below the horizon, and may hold its own day sky (ZoneLighting.skyDay: Hallow's Creek's grey,
    /// still sky) or keep the old procedural sky (skyStyle "procedural"). The painted sun is turned to stand where
    /// the sun light comes from; the night sky's painted moon is painted over (the moons are TwinMoons'). The skylight (ambient) leans a
    /// third of the way to the sky's own colours, its brightness the zone's own, so the light matches the sky without a GI update.
    /// GAME-ONLY dressing.
    /// </summary>
    public static class StylizedSky
    {
        public const string Folder = "Sky/Stylized/";
        /// <summary>The skies, as named in the pack (Resources/Sky/Stylized/HDRI/&lt;name&gt;.exr).</summary>
        public static readonly string[] Names = { "Noon", "Noon (Cloudy)", "Noon (Overcast)", "Sunrise", "Sunset", "Dusk", "Night", "NightGalaxy", "Aurora" };
        public const int Noon = 0, Cloudy = 1, Overcast = 2, Sunrise = 3, Sunset = 4, Dusk = 5, Night = 6, Galaxy = 7, Aurora = 8;
        /// <summary>Where each picture's sun is across it (u; read from the files: the brightest pixel), 0 where there is none.</summary>
        public static readonly float[] SunU = { 0, 0, 0, .999f, .006f, 0, 0, 0, 0 };
        /// <summary>A painted moon to paint over (u, v in the picture, radius in degrees with its halo): Night's, at 56 degrees up.</summary>
        public static Vector4 Hide(int sky) { return sky == Night ? new Vector4(.002f, .8128f, 3, 0) : Vector4.zero; }
        /// <summary>Each sky's own light, linear, read from the files: the sky above 20 degrees, and the band at the horizon.</summary>
        static readonly Color[] SkyLight = { new Color(.091f, .128f, .234f), new Color(.249f, .257f, .327f), new Color(.262f, .244f, .25f), new Color(.174f, .095f, .075f),
            new Color(.249f, .195f, .17f), new Color(.035f, .074f, .142f), new Color(.002f, .008f, .027f), new Color(.167f, .12f, .153f), new Color(.017f, .114f, .021f) };
        static readonly Color[] HorizonLight = { new Color(.447f, .433f, .469f), new Color(.491f, .457f, .454f), new Color(.264f, .249f, .263f), new Color(.683f, .239f, .119f),
            new Color(.6f, .31f, .144f), new Color(.232f, .153f, .123f), new Color(.013f, .019f, .039f), new Color(.03f, .02f, .025f), new Color(.003f, .021f, .004f) };
        public static int IndexOf(string name) { return System.Array.IndexOf(Names, name); }

        /// <summary>The phases of the day by the hour, as (phase, weight): night, sunrise, day, sunset, dusk. Each hour is in one phase or
        /// between two neighbours; dawn and dusk follow WorldClock's light (dawn's look at 6.5, dusk's at 19.5, night from 21).</summary>
        public enum Phase { Night, Sunrise, Day, Sunset, Dusk }
        public static List<(Phase phase, float weight)> Phases(float hour)
        {
            float h = Mathf.Repeat(hour, 24); var list = new List<(Phase, float)>();
            void Two(Phase a, Phase b, float t) { t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t)); if (t < 1) list.Add((a, 1 - t)); if (t > 0) list.Add((b, t)); }
            if (h < 5) list.Add((Phase.Night, 1));
            else if (h < 6.5f) Two(Phase.Night, Phase.Sunrise, (h - 5) / 1.5f);
            else if (h < 7) list.Add((Phase.Sunrise, 1));
            else if (h < 9) Two(Phase.Sunrise, Phase.Day, (h - 7) / 2);
            else if (h < 17) list.Add((Phase.Day, 1));
            else if (h < 18.75f) Two(Phase.Day, Phase.Sunset, (h - 17) / 1.75f);
            else if (h < 19.75f) Two(Phase.Sunset, Phase.Dusk, h - 18.75f);
            else if (h < 21) Two(Phase.Dusk, Phase.Night, (h - 19.75f) / 1.25f);
            else list.Add((Phase.Night, 1));
            return list;
        }
        /// <summary>How cloudy (fair, windy: broken cloud) and how overcast (a closed grey lid) the weather reads, each 0..1.</summary>
        public static void Cloud(WeatherLook w, out float cloudy, out float overcast)
        {
            cloudy = Mathf.Clamp01((w.clouds - .3f) / .3f); overcast = Mathf.Clamp01((w.dim - .3f) / .3f);
        }
        /// <summary>
        /// The skies to show and their weights (summing to 1, at most three: the shader's), for the hour, the weather (cloudy, overcast
        /// 0..1), whether tonight is an aurora night here, and the zone's own day sky (-1: the weather's). Under an overcast the sunrise
        /// and sunset are greyed toward the overcast sky; a clear night is the galaxy (or the aurora), a clouded one the plain night.
        /// </summary>
        public static List<(int sky, float weight)> Weights(float hour, float cloudy, float overcast, bool auroraNight, int daySky = -1)
        {
            var w = new float[Names.Length];
            float clear = 1 - Mathf.Clamp01(Mathf.Max(cloudy * 1.2f, overcast));
            foreach (var (phase, k) in Phases(hour))
                switch (phase)
                {
                    case Phase.Day:
                        if (daySky >= 0 && daySky != Overcast) { w[daySky] += k * (1 - overcast); w[Overcast] += k * overcast; }
                        else if (daySky == Overcast) w[Overcast] += k;
                        else { w[Overcast] += k * overcast; w[Cloudy] += k * (1 - overcast) * cloudy; w[Noon] += k * (1 - overcast) * (1 - cloudy); }
                        break;
                    case Phase.Sunrise: case Phase.Sunset:
                    {
                        int own = phase == Phase.Sunrise ? Sunrise : Sunset; float grey = daySky == Overcast ? .8f : .7f * overcast;
                        w[own] += k * (1 - grey); w[Overcast] += k * grey; break;
                    }
                    case Phase.Dusk: w[Dusk] += k; break;
                    case Phase.Night: w[auroraNight ? Aurora : Galaxy] += k * clear; w[Night] += k * (1 - clear); break;
                }
            // The three heaviest, renormalised.
            var all = new List<(int, float)>(); for (int i = 0; i < w.Length; i++) if (w[i] > .0001f) all.Add((i, w[i]));
            all.Sort((a, b) => b.Item2.CompareTo(a.Item2)); if (all.Count > 3) all.RemoveRange(3, all.Count - 3);
            float sum = 0; foreach (var (_, x) in all) sum += x;
            var list = new List<(int, float)>(); foreach (var (i, x) in all) list.Add((i, x / Mathf.Max(.0001f, sum)));
            return list;
        }
        /// <summary>Whether night number <paramref name="night"/> is an aurora night (one in six, by a hash of the night).</summary>
        public static bool AuroraNight(int night) { uint x = (uint)night * 2654435761u; x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; return x % 6 == 0; }

        static readonly Texture2D[] textures = new Texture2D[9];
        public static Texture2D Texture(int sky) { if (textures[sky] == null) textures[sky] = Resources.Load<Texture2D>(Folder + "HDRI/" + Names[sky]); return textures[sky]; }
        /// <summary>A fresh sky material of the zone (a copy of the pack's Sky_Noon on Crulanda/StylizedSky), or null when the pack is missing.</summary>
        public static Material NewMaterial()
        {
            var src = Resources.Load<Material>(Folder + "Sky_Noon"); if (src == null || src.shader == null || !src.shader.isSupported) return null;
            return new Material(src) { name = "Stylized sky" };
        }
        /// <summary>The zone's tint for the sky: a third of the way from white to its own sky tint's hue (none given: white).</summary>
        public static Color ZoneTint(string skyTint)
        {
            if (string.IsNullOrEmpty(skyTint)) return Color.white;
            var c = ZoneColors.Parse(skyTint, Color.grey); float g = Mathf.Max(.05f, c.grayscale);
            var hue = new Color(Mathf.Clamp(c.r / g, .5f, 1.6f), Mathf.Clamp(c.g / g, .5f, 1.6f), Mathf.Clamp(c.b / g, .5f, 1.6f), 1);
            return Color.Lerp(Color.white, hue, .35f);
        }
        /// <summary>The zone's haze up from the horizon, 0..1, from its skyHaze (the procedural sky's atmosphere: 1 a clear air, the
        /// Lowtowns' 2.2 a thick smog).</summary>
        public static float ZoneHaze(float skyHaze) { return Mathf.Clamp01(((skyHaze > 0 ? skyHaze : 1) - .8f) / 1.6f); }

        /// <summary>Sets the material for these skies and weights; the sun's bearing (Unity yaw of the direction to it).</summary>
        public static void Set(Material m, List<(int sky, float weight)> mix, float sunBearing)
        {
            string[] tex = { "_TexA", "_TexB", "_TexC" }, hide = { "_HideA", "_HideB", "_HideC" };
            var weights = Vector4.zero; var sunU = Vector4.zero;
            for (int i = 0; i < 3; i++)
            {
                if (i < mix.Count) { var t = Texture(mix[i].sky); m.SetTexture(tex[i], t); weights[i] = t != null ? mix[i].weight : 0; sunU[i] = SunU[mix[i].sky]; m.SetVector(hide[i], Hide(mix[i].sky)); }
                else { weights[i] = 0; m.SetVector(hide[i], Vector4.zero); }
            }
            m.SetVector("_Weights", weights); m.SetVector("_SunU", sunU); m.SetFloat("_SunAzimuth", sunBearing);
        }
        /// <summary>The sky's own light for the mix, in gamma (the project's space): above, and at the horizon.</summary>
        public static void Light(List<(int sky, float weight)> mix, out Color above, out Color horizon)
        {
            above = Color.black; horizon = Color.black;
            foreach (var (s, k) in mix) { above += SkyLight[s] * k; horizon += HorizonLight[s] * k; }
            above = above.gamma; horizon = horizon.gamma;
        }
        /// <summary><paramref name="c"/> with its hue turned <paramref name="k"/> of the way to <paramref name="toward"/>'s, its brightness kept.</summary>
        public static Color HueToward(Color c, Color toward, float k)
        {
            float g = c.grayscale, t = Mathf.Max(.01f, toward.grayscale); if (g <= 0) return c;
            var hue = new Color(Mathf.Clamp(toward.r / t, .4f, 2), Mathf.Clamp(toward.g / t, .4f, 2), Mathf.Clamp(toward.b / t, .4f, 2));
            var turned = new Color(g * hue.r, g * hue.g, g * hue.b, c.a); return Color.Lerp(c, turned, k);
        }
    }
}
