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
        /// <summary>
        /// What the hour and the weather leave of the zone's clear noon, as a tint for things drawn unlit (chimney smoke, falling
        /// water, foam, mist): white at noon, dimmer under rain, a dim blue at night. Set each frame; white with no clock.
        /// </summary>
        public static Color Unlit { get; private set; } = Color.white;
        /// <summary>Skip forward (development key, capture tours).</summary>
        public static void Advance(float hours) { Hour = Mathf.Repeat(Hour + hours, 24); if (Instance != null) Instance.Apply(); }

        Light sun; Material sky; ZoneLighting day; System.Collections.Generic.List<NightLight> lamps;
        struct Look { public Color sun, sky, equator, ground, fog; public float intensity, exposure; }
        Look dayLook, dawn, dusk, night;
        Color skyTint = Color.grey; float atmosphere = 1, sunSize = .04f;

        public void Init(Light sunLight, ZoneLighting lighting, Material skybox, System.Collections.Generic.List<NightLight> nightLights)
        {
            Instance = this; sun = sunLight; day = lighting; lamps = nightLights;
            TwinMoons.Ensure(transform);   // The Eye and The Tear (CANON)
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
            // Moonlit night: a clear blue moon and a blue ambient floor carry the night's colour (the grade no longer drains it), so
            // faces turned from the moon stay readable; the sky stays dark (exposure .3).
            night = new Look { sun = new Color(.45f, .6f, 1f), intensity = .4f, sky = new Color(.24f, .32f, .55f), equator = new Color(.18f, .24f, .42f), ground = new Color(.1f, .12f, .2f), fog = new Color(.09f, .1f, .15f), exposure = .3f };
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
            float loss = w.dim * Mathf.Lerp(.5f, 1, lit), lift = 1 + (.08f * w.dim - .45f * w.rain) * lit + flash * .7f;   // overcast skylight is a little bright; rain is gloomy, a storm dark
            // The same light plays sun by day and moon by night; it swings across the sky with the hour.
            bool moon = Hour >= 20.2f || Hour < 5.6f;
            // Noon height: the default arc, or the zone's own (sunHigh: Khaven's sun never climbs out of the evening).
            float high = day.sunHigh > 0 ? day.sunHigh : Mathf.Max(day.sunPitch * 1.6f, 45);
            float elevation = moon ? 38 : Mathf.Lerp(4, high, Mathf.Sin(Mathf.Clamp01((Hour - 5.6f) / 14.6f) * Mathf.PI));
            float yaw = day.sunYaw + (moon ? 180 : (Hour - 12) * 12);
            // By night the light is The Eye's (TwinMoons), shining from where it stands (never lower than 18 degrees, so the night
            // stays readable while it rises and sets).
            if (moon) { TwinMoons.Eye(Hour, out float e, out float b); elevation = Mathf.Max(18, e); yaw = b + 180; }
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(elevation, yaw, 0); sun.color = look.sun;
                sun.intensity = look.intensity * (1 - .7f * loss) + flash * .45f; sun.shadowStrength = (moon ? .45f : .75f) * (1 - .7f * loss);
            }
            RenderSettings.ambientSkyColor = Flat(look.sky, .35f * loss) * lift; RenderSettings.ambientEquatorColor = Flat(look.equator, .35f * loss) * lift;
            RenderSettings.ambientGroundColor = Flat(look.ground, .3f * loss) * (1 + .5f * flash);
            float cave = Mathf.Lerp(1, .2f, under);
            RenderSettings.ambientSkyColor *= cave; RenderSettings.ambientEquatorColor *= cave; RenderSettings.ambientGroundColor *= cave;
            // Unlit things take what is left of the light: the skylight and the sun or moon now against the zone's clear noon, a
            // step lower at night, in half of the skylight's change of hue.
            var amb = RenderSettings.ambientSkyColor; var noon = dayLook.sky; float ag = Mathf.Max(.01f, amb.grayscale), ng = Mathf.Max(.01f, noon.grayscale);
            float left = Mathf.Clamp((ag + .5f * (look.intensity * (1 - .7f * loss) + flash * .45f)) / (ng + .5f * dayLook.intensity), 0, 1.2f) * (1 - .2f * dark);
            var hue = new Color(Hue(amb.r / ag, noon.r / ng), Hue(amb.g / ag, noon.g / ng), Hue(amb.b / ag, noon.b / ng));
            Unlit = new Color(Mathf.Lerp(1, hue.r, .5f) * left, Mathf.Lerp(1, hue.g, .5f) * left, Mathf.Lerp(1, hue.b, .5f) * left, 1);
            var fog = Color.Lerp(Weathered(look.fog, w, lit), new Color(.05f, .045f, .04f, look.fog.a), .9f * under);   // smoky cave air
            RenderSettings.fogColor = fog;
            // The fog closes in with the weather, never nearer than 55 m at its far end (enemies stay in sight).
            RenderSettings.fogStartDistance = Mathf.Lerp(day.fogStart * w.fog, 1.5f, under); RenderSettings.fogEndDistance = Mathf.Lerp(Mathf.Max(day.fogEnd * w.fog, Mathf.Min(day.fogEnd, 55)), 34, under);
            if (sky != null)
            {
                float exposure = look.exposure * (1 - .3f * w.dim) * (1 - .25f * w.rain) * (1 + flash);
                sky.SetFloat("_Exposure", exposure);
                // Night sky: deep blue, thin atmosphere, a small pale moon disc instead of the sun. Cloud greys the sky's tint, thins
                // its scattering (a thick atmosphere turns the horizon orange: a sunny glow under a rain sky) and hides the sun or moon.
                float d = dark;
                var tint = Color.Lerp(skyTint, Flat(skyTint, 1) * 1.03f, .85f * w.dim);
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", Color.Lerp(tint, new Color(.18f, .24f, .48f), d));
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(atmosphere * (1 - .35f * w.dim), .55f, d));
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", Mathf.Lerp(sunSize, .0001f, d) * (1 - .9f * w.clouds));   // by night the moons are TwinMoons' own
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
        /// The fog (the air) under the weather: cloud greys and cools it; rain turns it to slate and darkens it by day (a storm by
        /// a third); mist pales it; an ash squall dulls it to dun; snow brightens it. Night keeps its own dark blue, only greyed.
        /// </summary>
        static Color Weathered(Color fog, WeatherLook w, float lit)
        {
            float g = fog.grayscale;
            var c = Color.Lerp(fog, new Color(g * .97f, g * .99f, g * 1.04f, fog.a), .6f * w.dim);
            c = Color.Lerp(c, new Color(g * .9f, g * .97f, g * 1.1f, fog.a), .5f * w.rain * lit) * (1 - .34f * w.rain * lit);
            c = Color.Lerp(c, Color.Lerp(c, Color.white, .22f), .7f * w.mist * lit);
            c = Color.Lerp(c, new Color(g * 1.06f, g, g * .9f, fog.a), .45f * w.ash);
            c = Color.Lerp(c, Color.Lerp(c, new Color(.92f, .95f, 1f), .25f), .6f * w.snow * lit);
            c.a = fog.a; return c;
        }
        /// <summary>One channel's change of hue, now against noon, kept within a sane band (a zone's noon skylight may lack a channel).</summary>
        static float Hue(float now, float noon) { return Mathf.Clamp(now / Mathf.Max(.05f, noon), .5f, 1.6f); }
        void OnDestroy() { if (Instance == this) { Instance = null; Unlit = Color.white; } }
    }

    /// <summary>A light that changes with the hour (lamps are dark by day, lit at night).</summary>
    public sealed class NightLight { public Light light; public float dayIntensity, nightIntensity; }

    /// <summary>
    /// Unlit effects in the hour's light: chimney smoke, a waterfall's foam and mist. Their shader takes no light, so at night they
    /// showed at their daytime brightness and bloomed. Each is tinted with the colour it was made with times
    /// <see cref="WorldClock.Unlit"/>; a particle system takes it on the particles it emits from then on.
    /// </summary>
    public sealed class HourTint : MonoBehaviour
    {
        readonly System.Collections.Generic.List<Material> mats = new System.Collections.Generic.List<Material>();
        readonly System.Collections.Generic.List<ParticleSystem> systems = new System.Collections.Generic.List<ParticleSystem>();
        readonly System.Collections.Generic.List<Color> matTints = new System.Collections.Generic.List<Color>(), systemTints = new System.Collections.Generic.List<Color>();
        Color shown = new Color(-1, -1, -1);
        /// <summary>A material of its own (not a shared asset) on the particle shader: its _TintColor follows the hour.</summary>
        public void Add(Material m) { if (m == null || !m.HasProperty("_TintColor")) return; mats.Add(m); matTints.Add(m.GetColor("_TintColor")); shown.r = -1; }
        /// <summary>A particle system whose start colour is already set: it follows the hour.</summary>
        public void Add(ParticleSystem ps) { if (ps == null) return; systems.Add(ps); systemTints.Add(ps.main.startColor.color); shown.r = -1; }
        /// <summary><paramref name="c"/> in the hour's light (its alpha kept).</summary>
        public static Color Of(Color c) { var u = WorldClock.Unlit; return new Color(c.r * u.r, c.g * u.g, c.b * u.b, c.a); }
        void LateUpdate()
        {
            var u = WorldClock.Unlit; if (Mathf.Abs(u.r - shown.r) + Mathf.Abs(u.g - shown.g) + Mathf.Abs(u.b - shown.b) < .004f) return;
            shown = u;
            for (int i = 0; i < mats.Count; i++) if (mats[i] != null) mats[i].SetColor("_TintColor", Of(matTints[i]));
            for (int i = 0; i < systems.Count; i++) if (systems[i] != null) { var main = systems[i].main; main.startColor = Of(systemTints[i]); }
        }
    }

    /// <summary>A chicken coop: hens roost inside at night. The hen-wife opens it in the morning and shuts it at dusk.</summary>
    public sealed class ZoneCoop
    {
        public string name;
        /// <summary>Foot of the ramp; yard in front; nest-box side; feed trough. World positions at ground level.</summary>
        public Vector3 door, yard, nest, trough;
        /// <summary>The ramp's foot on the ground and its top at the pop-hole: the hens walk up it to roost and down it in the morning (Chris, 2026-10-04).</summary>
        public Vector3 rampFoot, popHole;
        /// <summary>One hen on the ramp at a time (Chris: "they will have to wait in line so they are not running over each other"): the
        /// rest wait in a line behind its foot, in the order they came, and the next may set off when this time has come.</summary>
        public readonly System.Collections.Generic.List<MonoBehaviour> RampQueue = new System.Collections.Generic.List<MonoBehaviour>();
        public float RampNext;
        /// <summary>Where the hen <paramref name="place"/>th in the line waits: the ramp's foot, then back from it, away from the coop.</summary>
        public Vector3 QueueSpot(int place)
        {
            var away = rampFoot - popHole; away.y = 0; away = away.sqrMagnitude > .001f ? away.normalized : Vector3.back;
            return rampFoot + away * (place == 0 ? 0 : .35f + .42f * place);
        }
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
            var p = water.localPosition; p.y = .092f + Level * .02f; water.localPosition = p;   // lying in the pan's top (the pan is solid: 0 to .1), sinking as it dries
        }
        public void SetOpen(bool open) { Open = open; if (hinge != null) hinge.localRotation = Quaternion.Euler(open ? -100 : 0, 0, 0); }
    }
    /// <summary>
    /// A walk-in barn (2026-10-10, Chris: "barn door should be open enough to walk into, for farm animals to sleep"): the ground in front
    /// of its doorway, a point just inside it, and the stalls its farm beasts sleep in at night (one beast a stall). World positions.
    /// </summary>
    public sealed class ZoneBarn
    {
        public string name;
        /// <summary>The barn's right (along its front), for the two places in a stall that two sheep share.</summary>
        public Vector3 door, inside, right = Vector3.right; public Vector3[] stalls = new Vector3[0];
        readonly System.Collections.Generic.Dictionary<int, MonoBehaviour> taken = new System.Collections.Generic.Dictionary<int, MonoBehaviour>();
        readonly System.Collections.Generic.HashSet<MonoBehaviour> small = new System.Collections.Generic.HashSet<MonoBehaviour>();
        bool Free(int slot) { return !taken.TryGetValue(slot, out var o) || o == null; }
        /// <summary>
        /// The place <paramref name="who"/> has, or a new one; -1 when the barn is full. A place is a stall's half (slot = stall * 2 + 0 or 1):
        /// a big beast takes a whole stall, a small one (a sheep) shares a stall with another small one (2026-10-10: a flock and the cows
        /// would not fit one barn's stalls otherwise).
        /// </summary>
        public int Take(MonoBehaviour who, bool isSmall = false)
        {
            foreach (var kv in taken) if (kv.Value == who) return kv.Key;
            if (isSmall) for (int i = 0; i < stalls.Length; i++) if (!Free(i * 2) && small.Contains(taken[i * 2]) && Free(i * 2 + 1)) { taken[i * 2 + 1] = who; small.Add(who); return i * 2 + 1; }
            for (int i = 0; i < stalls.Length; i++) if (Free(i * 2) && Free(i * 2 + 1)) { taken[i * 2] = who; if (isSmall) small.Add(who); return i * 2; }
            return -1;
        }
        /// <summary>Where the beast in <paramref name="slot"/> lies: the stall's middle, or a side of it for a small one.</summary>
        public Vector3 SpotFor(int slot, bool isSmall) { var s = stalls[Mathf.Clamp(slot / 2, 0, stalls.Length - 1)]; return isSmall ? s + right * (slot % 2 == 0 ? -.55f : .55f) : s; }
        public void Free(MonoBehaviour who) { foreach (var k in new System.Collections.Generic.List<int>(taken.Keys)) if (taken[k] == who) taken.Remove(k); small.Remove(who); }
        public int Sleepers { get { int n = 0; foreach (var kv in taken) if (kv.Value != null) n++; return n; } }
        /// <summary>Whether <paramref name="who"/> has a stall here now.</summary>
        public bool Holds(MonoBehaviour who) { foreach (var kv in taken) if (kv.Value == who) return true; return false; }
    }
}
