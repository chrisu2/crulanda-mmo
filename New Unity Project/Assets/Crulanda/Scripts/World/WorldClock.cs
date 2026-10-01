using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Day/night cycle. One game day = <see cref="RealMinutesPerDay"/> real minutes; the hour survives zone travel.
    /// Keyframes blend the zone's own daylight palette (zone JSON lighting) with dawn, dusk and a moonlit night:
    /// sun/moon direction and colour, ambient trilight, fog and sky exposure.
    /// </summary>
    public sealed class WorldClock : MonoBehaviour
    {
        public const float RealMinutesPerDay = 40;
        /// <summary>Current hour 0..24. Static so it carries across scene reloads (travel, character switch).</summary>
        public static float Hour = 8.5f;
        public static WorldClock Instance { get; private set; }
        public static bool IsNight { get { return Hour >= 21 || Hour < 6; } }
        public static bool Between(float from, float to) { return from <= to ? Hour >= from && Hour < to : Hour >= from || Hour < to; }
        public static string Text { get { int h = (int)Hour, m = (int)((Hour - h) * 60); return h.ToString("00") + ":" + m.ToString("00"); } }

        /// <summary>0 by day, 1 at full night; ramps through dusk (18.5-20.5) and dawn (5.5-7).</summary>
        public static float Darkness
        {
            get { float h = Hour; return h >= 12 ? Mathf.InverseLerp(18.5f, 20.5f, h) : 1 - Mathf.InverseLerp(5.5f, 7, h); }
        }
        /// <summary>Skip forward (development key, capture tours).</summary>
        public static void Advance(float hours) { Hour = Mathf.Repeat(Hour + hours, 24); if (Instance != null) Instance.Apply(); }

        Light sun; Material sky; ZoneLighting day; System.Collections.Generic.List<NightLight> lamps;
        struct Look { public Color sun, sky, equator, ground, fog; public float intensity, exposure; }
        Look dayLook, dawn, dusk, night;
        Color skyTint = Color.grey; float atmosphere = 1, sunSize = .04f;

        public void Init(Light sunLight, ZoneLighting lighting, Material skybox, System.Collections.Generic.List<NightLight> nightLights)
        {
            Instance = this; sun = sunLight; day = lighting; lamps = nightLights;
            if (skybox != null)
            {
                sky = new Material(skybox); RenderSettings.skybox = sky;
                if (sky.HasProperty("_SkyTint")) skyTint = sky.GetColor("_SkyTint");
                if (sky.HasProperty("_AtmosphereThickness")) atmosphere = sky.GetFloat("_AtmosphereThickness");
                if (sky.HasProperty("_SunSize")) sunSize = sky.GetFloat("_SunSize");
            }
            // A zone's own daytime sky (Khaven's dusk) replaces the shared one; night drains it the same way.
            if (!string.IsNullOrEmpty(day.skyTint)) skyTint = ZoneColors.Parse(day.skyTint, skyTint);
            if (day.skyHaze > 0) atmosphere = day.skyHaze;
            dayLook = new Look { sun = ZoneColors.Parse(day.sunColor, Color.white), intensity = day.sunIntensity, sky = ZoneColors.Parse(day.ambientSky, Color.grey),
                equator = ZoneColors.Parse(day.ambientEquator, Color.grey), ground = ZoneColors.Parse(day.ambientGround, Color.black), fog = ZoneColors.Parse(day.fogColor, Color.grey), exposure = day.skyExposure > 0 ? day.skyExposure : 1.05f };
            dawn = new Look { sun = new Color(1, .62f, .4f), intensity = .6f, sky = new Color(.5f, .45f, .55f), equator = new Color(.55f, .42f, .38f), ground = new Color(.22f, .18f, .16f), fog = new Color(.62f, .5f, .48f), exposure = .7f };
            dusk = new Look { sun = new Color(1, .5f, .28f), intensity = .55f, sky = new Color(.45f, .38f, .5f), equator = new Color(.5f, .34f, .3f), ground = new Color(.2f, .15f, .13f), fog = new Color(.55f, .4f, .38f), exposure = .6f };
            // Moonlit night: a blue ambient floor so faces turned from the moon stay readable; the sky stays dark (exposure .3).
            night = new Look { sun = new Color(.52f, .6f, .85f), intensity = .26f, sky = new Color(.2f, .24f, .36f), equator = new Color(.16f, .19f, .3f), ground = new Color(.08f, .09f, .13f), fog = new Color(.09f, .1f, .15f), exposure = .3f };
            Apply();
        }
        /// <summary>Sky-only reflection probe (water and glossy surfaces reflect the current sky, not a fixed default one).</summary>
        public ReflectionProbe Reflections;
        float nextProbe;
        void Update()
        {
            Hour = Mathf.Repeat(Hour + Time.deltaTime * 24f / (RealMinutesPerDay * 60f), 24);
            Apply();
            if (Reflections != null && Time.unscaledTime >= nextProbe && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            { nextProbe = Time.unscaledTime + 2; Reflections.RenderProbe(); }
        }
        static Look Blend(Look a, Look b, float t)
        {
            return new Look { sun = Color.Lerp(a.sun, b.sun, t), intensity = Mathf.Lerp(a.intensity, b.intensity, t), sky = Color.Lerp(a.sky, b.sky, t),
                equator = Color.Lerp(a.equator, b.equator, t), ground = Color.Lerp(a.ground, b.ground, t), fog = Color.Lerp(a.fog, b.fog, t), exposure = Mathf.Lerp(a.exposure, b.exposure, t) };
        }
        Look Current()
        {
            float h = Hour;
            if (h < 5) return night;
            if (h < 6.5f) return Blend(night, dawn, Mathf.InverseLerp(5, 6.5f, h));
            if (h < 9) return Blend(dawn, dayLook, Mathf.InverseLerp(6.5f, 9, h));
            if (h < 17.5f) return dayLook;
            if (h < 19.5f) return Blend(dayLook, dusk, Mathf.InverseLerp(17.5f, 19.5f, h));
            if (h < 21) return Blend(dusk, night, Mathf.InverseLerp(19.5f, 21, h));
            return night;
        }
        void Apply()
        {
            var look = Current();
            // Weather (WorldWeather): cloud takes the sun (less of the moon, so nights stay readable) and flattens the light toward
            // grey, with a little more skylight by day; the air greys, cools, pales or dulls with it and the fog closes in; a storm's
            // lightning flashes everything for a moment.
            var w = WorldWeather.Now; float dark = Darkness, lit = 1 - dark, flash = WorldWeather.Flash;
            Shader.SetGlobalFloat("_NightGlow", dark);   // glowing flowers and mushrooms brighten after dark (Crulanda/Grass)
            // In a cave (Hollow) the skylight and the air darken with depth; its torches and fires light it.
            var view = Camera.main; float under = view != null ? Hollow.DepthAt(view.transform.position) : 0; Hollow.CameraDepth = under;
            float loss = w.dim * Mathf.Lerp(.5f, 1, lit), lift = 1 + (.14f * w.dim - .2f * w.rain) * lit + flash * .7f;   // overcast skylight is bright; rain is gloomy
            // The same light plays sun by day and moon by night; it swings across the sky with the hour.
            bool moon = Hour >= 20.2f || Hour < 5.6f;
            // Noon height: the default arc, or the zone's own (sunHigh: Khaven's sun never climbs out of the evening).
            float high = day.sunHigh > 0 ? day.sunHigh : Mathf.Max(day.sunPitch * 1.6f, 45);
            float elevation = moon ? 38 : Mathf.Lerp(4, high, Mathf.Sin(Mathf.Clamp01((Hour - 5.6f) / 14.6f) * Mathf.PI));
            float yaw = day.sunYaw + (moon ? 180 : (Hour - 12) * 12);
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(elevation, yaw, 0); sun.color = look.sun;
                sun.intensity = look.intensity * (1 - .7f * loss) + flash * .45f; sun.shadowStrength = (moon ? .45f : .75f) * (1 - .7f * loss);
            }
            RenderSettings.ambientSkyColor = Flat(look.sky, .35f * loss) * lift; RenderSettings.ambientEquatorColor = Flat(look.equator, .35f * loss) * lift;
            RenderSettings.ambientGroundColor = Flat(look.ground, .3f * loss) * (1 + .5f * flash);
            float cave = Mathf.Lerp(1, .2f, under);
            RenderSettings.ambientSkyColor *= cave; RenderSettings.ambientEquatorColor *= cave; RenderSettings.ambientGroundColor *= cave;
            var fog = Color.Lerp(Weathered(look.fog, w, lit), new Color(.05f, .045f, .04f, look.fog.a), .9f * under);   // smoky cave air
            RenderSettings.fogColor = fog;
            // The fog closes in with the weather, never nearer than 55 m at its far end (enemies stay in sight).
            RenderSettings.fogStartDistance = Mathf.Lerp(day.fogStart * w.fog, 1.5f, under); RenderSettings.fogEndDistance = Mathf.Lerp(Mathf.Max(day.fogEnd * w.fog, Mathf.Min(day.fogEnd, 55)), 34, under);
            if (sky != null)
            {
                float exposure = look.exposure * (1 - .3f * w.dim) * (1 + flash);
                sky.SetFloat("_Exposure", exposure);
                // Night sky: deep blue, thin atmosphere, a small pale moon disc instead of the sun. Cloud greys the sky's tint, thins
                // its scattering (a thick atmosphere turns the horizon orange: a sunny glow under a rain sky) and hides the sun or moon.
                float d = dark;
                var tint = Color.Lerp(skyTint, Flat(skyTint, 1) * 1.03f, .85f * w.dim);
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", Color.Lerp(tint, new Color(.18f, .24f, .48f), d));
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(atmosphere * (1 - .35f * w.dim), .55f, d));
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", Mathf.Lerp(sunSize, .018f, d) * (1 - .9f * w.clouds));
                // Below the horizon the sky is the fog, so anything seen past the backdrop fades into fog like the land does, at
                // every hour, not into a brown plane. This gamma-space procedural sky draws its ground as colour * sqrt(exposure).
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", fog / Mathf.Sqrt(Mathf.Max(.09f, exposure)));
            }
            var cam = Camera.main; if (cam != null) cam.backgroundColor = fog;
            // The zone's realtime sky probe has its own intensity (RenderSettings.reflectionIntensity only scales the default
            // skybox reflection), so dim both: night water stays dark instead of glowing with the moonlit sky.
            RenderSettings.reflectionIntensity = Mathf.Lerp(1f, .55f, dark) * (1 - .35f * w.dim) * (1 - .85f * under); if (Reflections != null) Reflections.intensity = RenderSettings.reflectionIntensity;
            // Lamps are lit early under heavy cloud: a glow in the windows and on the lanterns through the rain.
            float lamp = Mathf.Max(dark, .5f * Mathf.InverseLerp(.5f, .9f, w.dim));
            if (lamps != null) foreach (var l in lamps)
                if (l.light != null) { l.light.intensity = Mathf.Lerp(l.dayIntensity, l.nightIntensity, lamp); l.light.enabled = l.light.intensity > .01f; }
        }
        /// <summary>A colour pulled toward its own grey by <paramref name="k"/> (0: as it is, 1: grey).</summary>
        static Color Flat(Color c, float k) { float g = c.grayscale; return Color.Lerp(c, new Color(g, g, g, c.a), k); }
        /// <summary>
        /// The fog (the air) under the weather: cloud greys and cools it; rain darkens it by day; mist pales it; an ash squall
        /// dulls it to dun; snow brightens it. Night keeps its own dark blue, only greyed.
        /// </summary>
        static Color Weathered(Color fog, WeatherLook w, float lit)
        {
            float g = fog.grayscale;
            var c = Color.Lerp(fog, new Color(g * .97f, g * .99f, g * 1.04f, fog.a), .6f * w.dim);
            c *= 1 - .14f * w.rain * lit;
            c = Color.Lerp(c, Color.Lerp(c, Color.white, .22f), .7f * w.mist * lit);
            c = Color.Lerp(c, new Color(g * 1.06f, g, g * .9f, fog.a), .45f * w.ash);
            c = Color.Lerp(c, Color.Lerp(c, new Color(.92f, .95f, 1f), .25f), .6f * w.snow * lit);
            c.a = fog.a; return c;
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }

    /// <summary>A light that changes with the hour (lamps are dark by day, lit at night).</summary>
    public sealed class NightLight { public Light light; public float dayIntensity, nightIntensity; }

    /// <summary>A chicken coop: hens roost inside at night. The hen-wife opens it in the morning and shuts it at dusk.</summary>
    public sealed class ZoneCoop
    {
        public string name;
        /// <summary>Foot of the ramp; yard in front; nest-box side; feed trough. World positions at ground level.</summary>
        public Vector3 door, yard, nest, trough;
        public Transform hinge;
        public bool Open { get; private set; }
        /// <summary>Eggs waiting in the nest boxes (hens lay through the day; the hen-wife collects).</summary>
        public int Eggs, LaidToday;
        /// <summary>Feed scattered on the ground this long ago, and where (hens come running).</summary>
        public float FedAt = -999; public Vector3 FeedSpot;
        public bool Feeding { get { return Time.time - FedAt < 28; } }
        /// <summary>The water pan by the ramp: where the hen-wife stands to fill it, its water (a disc that sinks as it dries), and how full it is.</summary>
        public Vector3 pan; public Transform water; public float Level { get; private set; }
        public float WateredAt = -999;
        /// <summary>Filled from the well: the hens come to drink the way they come to feed.</summary>
        public void Water() { WateredAt = FedAt = Time.time; FeedSpot = pan; Level = 1; Show(); }
        public void Dry(float amount) { if (Level <= 0) return; Level = Mathf.Max(0, Level - amount); Show(); }
        void Show()
        {
            if (water == null) return;
            water.gameObject.SetActive(Level > .02f);
            var p = water.localPosition; p.y = .05f + Level * .09f; water.localPosition = p;   // from the rim down into the pan
        }
        public void SetOpen(bool open) { Open = open; if (hinge != null) hinge.localRotation = Quaternion.Euler(open ? -100 : 0, 0, 0); }
    }
}
