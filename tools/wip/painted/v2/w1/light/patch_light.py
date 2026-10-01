# Painted pass, visual review items 1, 4 and 5 (light): Oakhaven's milky daylight, rain and storm that darken the day,
# and the night (warm windows and lamps, a readable moonlit base, less vignette, unlit smoke and falls in the hour's light).
# Usage: patch_light.py "<root>"   (default: D:\code\mmo\New Unity Project\Assets\Crulanda)
# Anchor based: every old block must occur exactly once. All checks run before any write; a second run fails without writing.
import json, os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'
CLOCK = r'Scripts\World\WorldClock.cs'
POST = r'Scripts\World\ZonePost.cs'
WEATHER = r'Scripts\World\WorldWeather.cs'
BUILDER = r'Scripts\World\ZoneBuilder.cs'
VERDANT = r'Scripts\World\ZoneBuilder.Verdant.cs'
SHADER = r'World\Shaders\Post.shader'
OAKHAVEN = r'EncounterContent\Zones\oakhaven.json'

# (file, old, new)
EDITS = [
    # ---- WorldClock: the moonlit night, rain's gloom, the storm's air and sky, and the light left for unlit things ----
    (CLOCK,
     '''        /// <summary>Skip forward (development key, capture tours).</summary>
''',
     '''        /// <summary>
        /// What the hour and the weather leave of the zone's clear noon, as a tint for things drawn unlit (chimney smoke, falling
        /// water, foam, mist): white at noon, dimmer under rain, a dim blue at night. Set each frame; white with no clock.
        /// </summary>
        public static Color Unlit { get; private set; } = Color.white;
        /// <summary>Skip forward (development key, capture tours).</summary>
'''),
    (CLOCK,
     '''            // Moonlit night: a blue ambient floor so faces turned from the moon stay readable; the sky stays dark (exposure .3).
            night = new Look { sun = new Color(.52f, .6f, .85f), intensity = .26f, sky = new Color(.2f, .24f, .36f), equator = new Color(.16f, .19f, .3f), ground = new Color(.08f, .09f, .13f), fog = new Color(.09f, .1f, .15f), exposure = .3f };
''',
     '''            // Moonlit night: a clear blue moon and a blue ambient floor carry the night's colour (the grade no longer drains it), so
            // faces turned from the moon stay readable; the sky stays dark (exposure .3).
            night = new Look { sun = new Color(.45f, .6f, 1f), intensity = .4f, sky = new Color(.24f, .32f, .55f), equator = new Color(.18f, .24f, .42f), ground = new Color(.1f, .12f, .2f), fog = new Color(.09f, .1f, .15f), exposure = .3f };
'''),
    (CLOCK,
     '''lift = 1 + (.14f * w.dim - .2f * w.rain) * lit + flash * .7f;   // overcast skylight is bright; rain is gloomy''',
     '''lift = 1 + (.08f * w.dim - .45f * w.rain) * lit + flash * .7f;   // overcast skylight is a little bright; rain is gloomy, a storm dark'''),
    (CLOCK,
     '''            RenderSettings.ambientSkyColor *= cave; RenderSettings.ambientEquatorColor *= cave; RenderSettings.ambientGroundColor *= cave;
''',
     '''            RenderSettings.ambientSkyColor *= cave; RenderSettings.ambientEquatorColor *= cave; RenderSettings.ambientGroundColor *= cave;
            // Unlit things take what is left of the light: the skylight and the sun or moon now against the zone's clear noon, a
            // step lower at night, in half of the skylight's change of hue.
            var amb = RenderSettings.ambientSkyColor; var noon = dayLook.sky; float ag = Mathf.Max(.01f, amb.grayscale), ng = Mathf.Max(.01f, noon.grayscale);
            float left = Mathf.Clamp((ag + .5f * (look.intensity * (1 - .7f * loss) + flash * .45f)) / (ng + .5f * dayLook.intensity), 0, 1.2f) * (1 - .2f * dark);
            var hue = new Color(Hue(amb.r / ag, noon.r / ng), Hue(amb.g / ag, noon.g / ng), Hue(amb.b / ag, noon.b / ng));
            Unlit = new Color(Mathf.Lerp(1, hue.r, .5f) * left, Mathf.Lerp(1, hue.g, .5f) * left, Mathf.Lerp(1, hue.b, .5f) * left, 1);
'''),
    (CLOCK,
     '''                float exposure = look.exposure * (1 - .3f * w.dim) * (1 + flash);
''',
     '''                float exposure = look.exposure * (1 - .3f * w.dim) * (1 - .25f * w.rain) * (1 + flash);
'''),
    (CLOCK,
     '''        /// The fog (the air) under the weather: cloud greys and cools it; rain darkens it by day; mist pales it; an ash squall
        /// dulls it to dun; snow brightens it. Night keeps its own dark blue, only greyed.
        /// </summary>
''',
     '''        /// The fog (the air) under the weather: cloud greys and cools it; rain turns it to slate and darkens it by day (a storm by
        /// a third); mist pales it; an ash squall dulls it to dun; snow brightens it. Night keeps its own dark blue, only greyed.
        /// </summary>
'''),
    (CLOCK,
     '''            c *= 1 - .14f * w.rain * lit;
''',
     '''            c = Color.Lerp(c, new Color(g * .9f, g * .97f, g * 1.1f, fog.a), .5f * w.rain * lit) * (1 - .34f * w.rain * lit);
'''),
    (CLOCK,
     '''        void OnDestroy() { if (Instance == this) Instance = null; }
    }

    /// <summary>A light that changes with the hour (lamps are dark by day, lit at night).</summary>
    public sealed class NightLight { public Light light; public float dayIntensity, nightIntensity; }
''',
     '''        /// <summary>One channel's change of hue, now against noon, kept within a sane band (a zone's noon skylight may lack a channel).</summary>
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
'''),

    # ---- ZonePost: rain and storm darken and drain the day; the night keeps its colour, glows less and has lighter corners ----
    (POST,
     '''    /// - colour grading that follows the zone's biome and the hour (cooler and less saturated at night);
''',
     '''    /// - colour grading that follows the zone's biome, the hour and the weather (cooler at night, with firelight kept warm;
    ///   darker and duller under rain);
'''),
    (POST,
     '''            g.saturation *= 1 - .2f * weather.dim * lit - .08f * weather.ash;''',
     '''            g.saturation *= 1 - .2f * weather.dim * lit - .12f * weather.rain * lit - .08f * weather.ash;'''),
    (POST,
     '''            g.exposure *= 1 - .06f * weather.rain * lit;
''',
     '''            g.exposure *= 1 - .12f * weather.rain * lit;   // rain is a dull day, a storm a dark one (the skylight falls with it: WorldClock)
'''),
    (POST,
     '''            // Night: lamps bloom more, colour drains and cools; dusk and dawn warm up a touch.
            float dusk = Mathf.Clamp01(1 - Mathf.Abs(dark - .5f) * 2);
            mat.SetFloat("_Threshold", Mathf.Lerp(g.threshold, .55f, dark)); mat.SetFloat("_Knee", .5f);
            mat.SetFloat("_BloomIntensity", Mathf.Lerp(g.bloom, 1.1f, dark));
            mat.SetFloat("_Exposure", Mathf.Lerp(g.exposure, 1.2f, dark) * (1 + .2f * WorldWeather.Flash));
''',
     '''            // Night: only lamps, windows and fires bloom, and not so far that they clip to white; the moon and the blue skylight
            // carry the night's colour (WorldClock), so the grade drains little and cools a little, and the composite keeps
            // firelight warm (_Lift); dusk and dawn warm up a touch.
            float dusk = Mathf.Clamp01(1 - Mathf.Abs(dark - .5f) * 2);
            mat.SetFloat("_Threshold", Mathf.Lerp(g.threshold, .7f, dark)); mat.SetFloat("_Knee", .5f);
            mat.SetFloat("_BloomIntensity", Mathf.Lerp(g.bloom, .8f, dark));
            mat.SetFloat("_Exposure", Mathf.Lerp(g.exposure, 1.05f, dark) * (1 + .2f * WorldWeather.Flash));
'''),
    (POST,
     '''            mat.SetFloat("_Saturation", Mathf.Lerp(g.saturation, .62f, dark)); mat.SetFloat("_Contrast", g.contrast); mat.SetFloat("_Lift", dark);
            mat.SetFloat("_Vignette", g.vignette + dark * .4f);
            mat.SetColor("_Tint", Color.Lerp(Color.Lerp(g.tint, new Color(.86f, .92f, 1.12f), dark), new Color(1.08f, .96f, .88f), dusk * .6f));
''',
     '''            mat.SetFloat("_Saturation", Mathf.Lerp(g.saturation, .9f, dark)); mat.SetFloat("_Contrast", g.contrast); mat.SetFloat("_Lift", dark);
            mat.SetFloat("_Vignette", g.vignette + dark * .15f);
            mat.SetColor("_Tint", Color.Lerp(Color.Lerp(g.tint, new Color(.9f, .95f, 1.1f), dark), new Color(1.08f, .96f, .88f), dusk * .6f));
'''),

    # ---- Post.shader: firelight keeps its colour after dark ----
    (SHADER,
     '''        half l = dot(c, half3(0.2126, 0.7152, 0.0722));
        c = lerp(l.xxx, c, _Saturation);
''',
     '''        half l = dot(c, half3(0.2126, 0.7152, 0.0722));
        // Firelight after dark (_Lift = darkness): bright warm pixels (lit windows, lamp glass, the ground under a lamp) keep
        // their colour and take a warm tint, not the night's cool one, so they read as fire and not as cold white. None by day.
        half fire = _Lift * smoothstep(0.3, 0.7, l) * saturate((c.r - c.b) * 4);
        c = lerp(l.xxx, c, lerp(_Saturation, max(_Saturation, 1.15), fire));
'''),
    (SHADER,
     '''        c = saturate(c) * _Tint.rgb;
''',
     '''        c = saturate(c) * lerp(_Tint.rgb, half3(1.04, 0.96, 0.84), fire);
'''),
    (SHADER,
     '''// 4 composite: cloud shadows, bloom + shafts, exposure, ACES filmic tone map, saturation/contrast/tint grade, vignette.
''',
     '''// 4 composite: cloud shadows, bloom + shafts, exposure, ACES filmic tone map, saturation/contrast/tint grade (firelight
//   kept warm at night), vignette.
'''),

    # ---- WorldWeather: a storm closes the fog further (the tests' floor is .5); rain darkens the cloud's underside ----
    (WEATHER,
     '''case WeatherKind.Storm: return new WeatherLook { clouds = 1, dim = .86f, rain = 1, wind = 1, fog = .56f };''',
     '''case WeatherKind.Storm: return new WeatherLook { clouds = 1, dim = .86f, rain = 1, wind = 1, fog = .5f };'''),
    (WEATHER,
     '''* (1 - .28f * shown.rain);           // darker with rain''',
     '''* (1 - .35f * shown.rain);           // darker with rain (the air it is cut from darkens too: WorldClock.Weathered)'''),

    # ---- ZoneBuilder: lamps reach further, the inn lantern is no daytime blob, smoke takes the hour's light ----
    (BUILDER,
     '''l.type = LightType.Point; l.range = variant > 0 ? 7 : 9; l.intensity = 1.2f; l.color = new Color(1, .7f, .38f);''',
     '''l.type = LightType.Point; l.range = variant > 0 ? 9 : 11; l.intensity = 1.2f; l.color = new Color(1, .7f, .38f);'''),
    (BUILDER,
     '''            NightLights.Add(new NightLight { light = glow, dayIntensity = 1.4f, nightIntensity = 2.2f });
''',
     '''            NightLights.Add(new NightLight { light = glow, dayIntensity = .6f, nightIntensity = 2.2f });   // faint by day: at 1.4 it bloomed to a pale blob on the plaster
'''),
    (BUILDER,
     '''        public readonly List<NightLight> NightLights = new List<NightLight>();
''',
     '''        public readonly List<NightLight> NightLights = new List<NightLight>();
        /// <summary>Unlit effects (chimney smoke, a fall's foam and mist) that take the hour's light. Made on first use.</summary>
        HourTint HourTinted { get { if (hourTint == null) hourTint = gameObject.AddComponent<HourTint>(); return hourTint; } }
        HourTint hourTint;
'''),
    (BUILDER,
     '''            vel.x = new ParticleSystem.MinMaxCurve(.25f, .5f); vel.y = new ParticleSystem.MinMaxCurve(0, 0); vel.z = new ParticleSystem.MinMaxCurve(.05f, .15f);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = art.particle;
''',
     '''            vel.x = new ParticleSystem.MinMaxCurve(.25f, .5f); vel.y = new ParticleSystem.MinMaxCurve(0, 0); vel.z = new ParticleSystem.MinMaxCurve(.05f, .15f);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = art.particle;
            HourTinted.Add(ps);   // grey by day, a dim wisp against the night sky
'''),

    # ---- ZoneBuilder.Verdant: the fall's sheets, foam and mist take the hour's light ----
    (VERDANT,
     '''            var foam = FallMaterial(.95f);
''',
     '''            var foam = FallMaterial(.95f); HourTinted.Add(foam);
'''),
    (VERDANT,
     '''new GradientAlphaKey(1, .25f), new GradientAlphaKey(0, 1) }); col.color = g;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = art.particle;
''',
     '''new GradientAlphaKey(1, .25f), new GradientAlphaKey(0, 1) }); col.color = g;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = art.particle;
            HourTinted.Add(ps);
'''),
    (VERDANT,
     '''        /// <summary>Streaked white-blue water (unlit, alpha-blended particles shader, both faces), its streaks tiling down the sheet.</summary>
''',
     '''        /// <summary>
        /// Streaked white-blue water (unlit, alpha-blended particles shader, both faces), its streaks tiling down the sheet. Unlit,
        /// so its tint follows the hour (<see cref="HourTint"/>; the sheets through <see cref="FallingWater"/>).
        /// </summary>
'''),
    (VERDANT,
     '''        public float speed = .6f; Material m;
        void Start() { var r = GetComponent<Renderer>(); if (r != null) m = r.material; }
        void Update() { if (m != null) m.mainTextureOffset = new Vector2(0, -Time.time * speed); }
''',
     '''        public float speed = .6f; Material m; Color tint;
        void Start() { var r = GetComponent<Renderer>(); if (r != null) { m = r.material; tint = m.GetColor("_TintColor"); } }
        // The sheet is unlit: its tint takes the hour's light, so the fall is moonlit at night and not the brightest thing in view.
        void Update() { if (m == null) return; m.mainTextureOffset = new Vector2(0, -Time.time * speed); m.SetColor("_TintColor", HourTint.Of(tint)); }
'''),
]

# Oakhaven's daylight: coloured air and a clear sky (lighting block only). (key, old, new)
LIGHTING = [
    ('fogColor', '#AFC0C6', '#96B8DA'),
    ('fogStart', 70, 105),
    ('fogEnd', 240, 330),
    ('skyTint', '#788FB8', '#6189C8'),
    ('skyExposure', 1.2, 1.05),
]


def fail(msg):
    print('FAILED: ' + msg + ' (nothing written)')
    sys.exit(1)


def dump(d):
    return (json.dumps(d, indent=2, ensure_ascii=False) + '\n').encode('utf-8')


def main():
    texts, out = {}, {}
    for rel in sorted(set(e[0] for e in EDITS)):
        path = os.path.join(ROOT, rel)
        if not os.path.isfile(path): fail('missing ' + path)
        raw = open(path, 'rb').read()
        if b'\r' in raw: fail(rel + ': CR line endings')
        texts[rel] = out[rel] = raw.decode('utf-8')
    for i, (rel, old, new) in enumerate(EDITS):
        if old == new: fail('edit %d is empty' % i)
        n = texts[rel].count(old)
        if n != 1: fail('%s: edit %d anchor occurs %d times: %r' % (rel, i, n, old[:90]))
        if out[rel].count(old) != 1: fail('%s: edit %d overlaps an earlier edit' % (rel, i))
        out[rel] = out[rel].replace(old, new)
    for rel in out:
        if 'HourTint' in texts[rel] or 'WorldClock.Unlit' in texts[rel]: fail(rel + ': already patched')
        if '\u2014' in out[rel]: fail(rel + ': em-dash')
    jpath = os.path.join(ROOT, OAKHAVEN)
    if not os.path.isfile(jpath): fail('missing ' + jpath)
    jraw = open(jpath, 'rb').read()
    zone = json.loads(jraw.decode('utf-8'))
    if dump(zone) != jraw: fail('oakhaven.json does not round-trip byte for byte')
    light = zone.get('lighting')
    if not isinstance(light, dict): fail('oakhaven.json: no lighting block')
    for key, old, new in LIGHTING:
        if key not in light or light[key] != old or type(light[key]) is not type(old): fail('oakhaven.json lighting.%s is %r, expected %r' % (key, light.get(key), old))
    for key, old, new in LIGHTING: light[key] = new
    jnew = dump(zone)
    # Only the five lines change, in place.
    a, b = jraw.split(b'\n'), jnew.split(b'\n')
    if len(a) != len(b) or sum(1 for x, y in zip(a, b) if x != y) != len(LIGHTING): fail('oakhaven.json: the edit touched more than the lighting lines')
    # All checks passed: write.
    for rel in sorted(out):
        with open(os.path.join(ROOT, rel), 'wb') as f: f.write(out[rel].encode('utf-8'))
        print('patched ' + rel)
    with open(jpath, 'wb') as f: f.write(jnew)
    print('patched ' + OAKHAVEN)
    print('OK: %d code edits, %d lighting fields' % (len(EDITS), len(LIGHTING)))


main()
