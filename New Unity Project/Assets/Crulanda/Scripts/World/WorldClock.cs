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
            dayLook = new Look { sun = ZoneColors.Parse(day.sunColor, Color.white), intensity = day.sunIntensity, sky = ZoneColors.Parse(day.ambientSky, Color.grey),
                equator = ZoneColors.Parse(day.ambientEquator, Color.grey), ground = ZoneColors.Parse(day.ambientGround, Color.black), fog = ZoneColors.Parse(day.fogColor, Color.grey), exposure = 1.05f };
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
            // The same light plays sun by day and moon by night; it swings across the sky with the hour.
            bool moon = Hour >= 20.2f || Hour < 5.6f;
            float elevation = moon ? 38 : Mathf.Lerp(4, Mathf.Max(day.sunPitch * 1.6f, 45), Mathf.Sin(Mathf.Clamp01((Hour - 5.6f) / 14.6f) * Mathf.PI));
            float yaw = day.sunYaw + (moon ? 180 : (Hour - 12) * 12);
            if (sun != null) { sun.transform.rotation = Quaternion.Euler(elevation, yaw, 0); sun.color = look.sun; sun.intensity = look.intensity; sun.shadowStrength = moon ? .45f : .75f; }
            RenderSettings.ambientSkyColor = look.sky; RenderSettings.ambientEquatorColor = look.equator; RenderSettings.ambientGroundColor = look.ground;
            RenderSettings.fogColor = look.fog;
            if (sky != null)
            {
                sky.SetFloat("_Exposure", look.exposure);
                // Night sky: deep blue, thin atmosphere, a small pale moon disc instead of the sun.
                float d = Darkness;
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", Color.Lerp(skyTint, new Color(.18f, .24f, .48f), d));
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(atmosphere, .55f, d));
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", Mathf.Lerp(sunSize, .018f, d));
                // Below the horizon the sky is the fog, so anything seen past the backdrop fades into fog like the land does, at
                // every hour, not into a brown plane. This gamma-space procedural sky draws its ground as colour * sqrt(exposure).
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", look.fog / Mathf.Sqrt(Mathf.Max(.09f, look.exposure)));
            }
            var cam = Camera.main; if (cam != null) cam.backgroundColor = look.fog;
            float dark = Darkness;
            // The zone's realtime sky probe has its own intensity (RenderSettings.reflectionIntensity only scales the default
            // skybox reflection), so dim both: night water stays dark instead of glowing with the moonlit sky.
            RenderSettings.reflectionIntensity = Mathf.Lerp(1f, .55f, dark); if (Reflections != null) Reflections.intensity = RenderSettings.reflectionIntensity;
            if (lamps != null) foreach (var l in lamps)
                if (l.light != null) { l.light.intensity = Mathf.Lerp(l.dayIntensity, l.nightIntensity, dark); l.light.enabled = l.light.intensity > .01f; }
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
        public void SetOpen(bool open) { Open = open; if (hinge != null) hinge.localRotation = Quaternion.Euler(open ? -100 : 0, 0, 0); }
    }
}
