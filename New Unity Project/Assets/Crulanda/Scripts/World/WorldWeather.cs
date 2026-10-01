using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The weather a zone can have, calmest first. Its severity (<see cref="WeatherSchedule.Severity"/>) keeps changes gradual:
    /// clear, then fair or windy, then overcast or mist, then rain, flurries (Snow) or an ash squall, then a storm.
    /// </summary>
    public enum WeatherKind { Clear, Fair, Windy, Overcast, Mist, Rain, Snow, AshSquall, Storm }

    /// <summary>
    /// What weather does to the world, as amounts the world reads (blended while the weather turns).
    /// - clouds: sky cover (the cloud layer and its shadows on the land);
    /// - dim: sunlight lost to cloud (flatter, greyer light, less glare);
    /// - rain, snow, ash, mist: falling or drifting particles (ash: a squall's dust off the Wasting);
    /// - wind: gusts through grass, leaves and anything falling;
    /// - fog: the zone's own fog distances times this (1 = as the zone sets them; lower closes the fog in).
    /// </summary>
    [Serializable] public struct WeatherLook
    {
        public float clouds, dim, rain, snow, ash, mist, wind, fog;
        /// <summary>No weather at all: what a scene without a zone's weather reads.</summary>
        public static readonly WeatherLook None = new WeatherLook { fog = 1 };
        public static WeatherLook Of(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Clear: return new WeatherLook { clouds = .18f, wind = .12f, fog = 1 };
                case WeatherKind.Fair: return new WeatherLook { clouds = .46f, dim = .06f, wind = .25f, fog = 1 };
                case WeatherKind.Windy: return new WeatherLook { clouds = .52f, dim = .1f, wind = .85f, fog = .95f };
                case WeatherKind.Overcast: return new WeatherLook { clouds = .96f, dim = .62f, wind = .35f, fog = .82f };
                case WeatherKind.Mist: return new WeatherLook { clouds = .8f, dim = .42f, mist = 1, wind = .06f, fog = .62f };
                case WeatherKind.Rain: return new WeatherLook { clouds = 1, dim = .74f, rain = .72f, wind = .45f, fog = .66f };
                case WeatherKind.Snow: return new WeatherLook { clouds = .9f, dim = .5f, snow = 1, wind = .5f, fog = .7f };
                case WeatherKind.AshSquall: return new WeatherLook { clouds = .88f, dim = .52f, ash = 1, wind = .92f, fog = .72f };
                case WeatherKind.Storm: return new WeatherLook { clouds = 1, dim = .86f, rain = 1, wind = 1, fog = .56f };
                default: return None;
            }
        }
        public static WeatherLook Blend(WeatherLook a, WeatherLook b, float t)
        {
            return new WeatherLook { clouds = Mathf.Lerp(a.clouds, b.clouds, t), dim = Mathf.Lerp(a.dim, b.dim, t), rain = Mathf.Lerp(a.rain, b.rain, t),
                snow = Mathf.Lerp(a.snow, b.snow, t), ash = Mathf.Lerp(a.ash, b.ash, t), mist = Mathf.Lerp(a.mist, b.mist, t),
                wind = Mathf.Lerp(a.wind, b.wind, t), fog = Mathf.Lerp(a.fog, b.fog, t) };
        }
        /// <summary>Every amount moved toward <paramref name="b"/>'s by at most <paramref name="step"/>.</summary>
        public static WeatherLook Toward(WeatherLook a, WeatherLook b, float step)
        {
            return new WeatherLook { clouds = Mathf.MoveTowards(a.clouds, b.clouds, step), dim = Mathf.MoveTowards(a.dim, b.dim, step),
                rain = Mathf.MoveTowards(a.rain, b.rain, step), snow = Mathf.MoveTowards(a.snow, b.snow, step), ash = Mathf.MoveTowards(a.ash, b.ash, step),
                mist = Mathf.MoveTowards(a.mist, b.mist, step), wind = Mathf.MoveTowards(a.wind, b.wind, step), fog = Mathf.MoveTowards(a.fog, b.fog, step) };
        }
    }

    /// <summary>
    /// A zone's weather as a sequence of spells, each <see cref="SpellSeconds"/> long, picked from the zone's table (kinds with
    /// weights) by a seeded random: the same seed and table always give the same sequence, whatever order it is read in. A spell
    /// moves at most one step of severity from the last (clear to fair to overcast to rain, never clear straight into a storm).
    /// Each spell turns in from the last one's look over its first <see cref="TurnSeconds"/>.
    /// </summary>
    public sealed class WeatherSchedule
    {
        /// <summary>A spell of weather lasts this long in real seconds (a game day is 40 minutes: five or six spells a day).</summary>
        public const float SpellSeconds = 420;
        /// <summary>The first this-many seconds of a spell blend from the last spell's weather into its own.</summary>
        public const float TurnSeconds = 50;
        readonly List<(WeatherKind kind, float weight)> table = new List<(WeatherKind kind, float weight)>();
        readonly List<WeatherKind> spells = new List<WeatherKind>();
        readonly int seed;

        public WeatherSchedule(int seed, IEnumerable<(WeatherKind kind, float weight)> kinds)
        {
            this.seed = seed;
            if (kinds != null) foreach (var k in kinds) if (k.weight > 0 && !table.Exists(t => t.kind == k.kind)) table.Add(k);
            if (table.Count == 0) table.Add((WeatherKind.Clear, 1));
        }
        /// <summary>The zone's kinds and weights, in the zone's order (calmest first by convention).</summary>
        public IReadOnlyList<(WeatherKind kind, float weight)> Table { get { return table; } }
        public bool Has(WeatherKind kind) { return table.Exists(t => t.kind == kind); }

        public static int Severity(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Clear: return 0;
                case WeatherKind.Fair: case WeatherKind.Windy: return 1;
                case WeatherKind.Overcast: case WeatherKind.Mist: return 2;
                case WeatherKind.Rain: case WeatherKind.Snow: case WeatherKind.AshSquall: return 3;
                default: return 4;
            }
        }
        /// <summary>The kind of the spell with this index (spell i covers weather time i * SpellSeconds onward).</summary>
        public WeatherKind Spell(int index)
        {
            if (index < 0) index = 0;
            while (spells.Count <= index) spells.Add(Pick(spells.Count));
            return spells[index];
        }
        WeatherKind Pick(int i)
        {
            int last = i > 0 ? Severity(spells[i - 1]) : -1;
            float total = 0;
            foreach (var t in table) if (Allowed(t.kind, last)) total += t.weight;
            bool gentle = total > 0;
            if (!gentle) foreach (var t in table) total += t.weight;
            double roll = Roll(seed, i) * total; WeatherKind pick = table[0].kind;
            foreach (var t in table)
            {
                if (gentle && !Allowed(t.kind, last)) continue;
                pick = t.kind; roll -= t.weight; if (roll < 0) break;
            }
            return pick;
        }
        static bool Allowed(WeatherKind kind, int last) { return last < 0 || Mathf.Abs(Severity(kind) - last) <= 1; }
        /// <summary>
        /// Spell i's roll, 0..1: the seed and index mixed by SplitMix64's finaliser. (A System.Random seeded per spell gave
        /// correlated first draws for neighbouring spells: one zone went 600 spells without a storm.)
        /// </summary>
        static double Roll(int seed, int i)
        {
            unchecked
            {
                ulong z = (ulong)(uint)seed * 0x9E3779B97F4A7C15UL + (ulong)(uint)i * 0xBF58476D1CE4E5B9UL + 0x94D049BB133111EBUL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL; z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL; z ^= z >> 31;
                return (z >> 11) * (1.0 / 9007199254740992.0);
            }
        }

        /// <summary>The look at a weather time (seconds): the spell's own, blending in from the last spell over its first TurnSeconds.</summary>
        public WeatherLook LookAt(double time, out WeatherKind kind)
        {
            if (time < 0) time = 0;
            int i = (int)Math.Floor(time / SpellSeconds); float into = (float)(time - i * (double)SpellSeconds);
            kind = Spell(i);
            var look = WeatherLook.Of(kind);
            if (i > 0 && into < TurnSeconds) look = WeatherLook.Blend(WeatherLook.Of(Spell(i - 1)), look, Mathf.SmoothStep(0, 1, into / TurnSeconds));
            return look;
        }
        /// <summary>A weather time at or after <paramref name="from"/> inside a calm spell (clear or fair, or the table's calmest), its turn done.</summary>
        public double CalmFrom(double from)
        {
            int calmest = int.MaxValue; foreach (var t in table) calmest = Mathf.Min(calmest, Severity(t.kind));
            int i = (int)Math.Floor(Math.Max(0, from) / SpellSeconds);
            for (int k = 0; k < 80; k++, i++) if (Severity(Spell(i)) <= Mathf.Max(1, calmest)) return i * (double)SpellSeconds + TurnSeconds + 1;
            return Math.Max(0, from);
        }

        /// <summary>The weather a biome has when its zone names none.</summary>
        public static (WeatherKind kind, float weight)[] Defaults(string biome)
        {
            switch (biome)
            {
                case "gloom": return new[] { (WeatherKind.Fair, 1f), (WeatherKind.Windy, 1f), (WeatherKind.Overcast, 3f), (WeatherKind.Mist, 4f), (WeatherKind.Rain, 2f) };
                case "mountain": return new[] { (WeatherKind.Clear, 2f), (WeatherKind.Fair, 3f), (WeatherKind.Windy, 2.5f), (WeatherKind.Overcast, 2f), (WeatherKind.Snow, 3f) };
                case "ash": return new[] { (WeatherKind.Fair, 1f), (WeatherKind.Windy, 2f), (WeatherKind.Overcast, 3f), (WeatherKind.AshSquall, 3f) };
                default: return new[] { (WeatherKind.Clear, 2f), (WeatherKind.Fair, 3f), (WeatherKind.Windy, 1.5f), (WeatherKind.Overcast, 2.5f), (WeatherKind.Rain, 3f), (WeatherKind.Storm, 1.2f) };
            }
        }
        /// <summary>A zone's schedule: its JSON "weather" table (unknown kinds warn and are skipped), else its biome's default.</summary>
        public static WeatherSchedule For(ZoneDefinition zone)
        {
            var kinds = new List<(WeatherKind kind, float weight)>();
            if (zone.weather != null)
                foreach (var w in zone.weather)
                {
                    if (w == null) continue;
                    if (TryParse(w.kind, out var k)) kinds.Add((k, w.weight));
                    else Debug.LogWarning(zone.id + ": unknown weather kind '" + w.kind + "'.");
                }
            if (kinds.Count == 0) kinds.AddRange(Defaults(zone.biome));
            return new WeatherSchedule(zone.seed, kinds);
        }
        /// <summary>A kind by name, any case, spaces or underscores ignored; "flurries" is Snow, "squall" is AshSquall.</summary>
        public static bool TryParse(string name, out WeatherKind kind)
        {
            kind = WeatherKind.Clear;
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
            if (n == "flurries") { kind = WeatherKind.Snow; return true; }
            if (n == "squall") { kind = WeatherKind.AshSquall; return true; }
            foreach (WeatherKind k in Enum.GetValues(typeof(WeatherKind))) if (k.ToString().ToLowerInvariant() == n) { kind = k; return true; }
            return false;
        }
        /// <summary>How the weather is named to the player (the minimap, the dev key).</summary>
        public static string Name(WeatherKind kind)
        {
            switch (kind) { case WeatherKind.Snow: return "Flurries"; case WeatherKind.AshSquall: return "Ash squall"; default: return kind.ToString(); }
        }
        /// <summary>The chat line when the zone's weather turns (null: a change too small to mention, such as clear to fair).</summary>
        public static string Herald(WeatherKind from, WeatherKind to)
        {
            bool wasRain = from == WeatherKind.Rain || from == WeatherKind.Storm;
            switch (to)
            {
                case WeatherKind.Storm: return "A storm breaks overhead.";
                case WeatherKind.Rain: return from == WeatherKind.Storm ? "The storm eases to a steady rain." : "It starts to rain.";
                case WeatherKind.Snow: return "Snow starts to fall.";
                case WeatherKind.AshSquall: return "An ash squall blows in off the Wasting.";
                case WeatherKind.Mist: return "Mist rises from the ground.";
            }
            if (wasRain) return "The rain stops.";
            if (from == WeatherKind.Snow) return "The snow stops.";
            if (from == WeatherKind.AshSquall) return "The squall blows itself out.";
            if (from == WeatherKind.Mist) return "The mist lifts.";
            if (to == WeatherKind.Overcast) return "Clouds close over the sky.";
            if (to == WeatherKind.Windy) return "The wind picks up.";
            if (from == WeatherKind.Overcast) return "The clouds break up.";
            if (from == WeatherKind.Windy) return "The wind drops.";
            return null;
        }
    }

    /// <summary>
    /// The zone's weather in play. It follows the zone's <see cref="WeatherSchedule"/> on a weather clock that, like the hour,
    /// carries across zone travel (a new session starts inside a calm spell); the dev key (F8), the capture tours, tests and
    /// --crulanda-weather &lt;kind&gt; can hold a kind instead. Each frame it moves toward the weather's look and drives:
    /// - light, sky and fog (read by <see cref="WorldClock"/>), and the grade and cloud shadows (read by <see cref="ZonePost"/>);
    /// - a painted cloud layer over the sky (Crulanda/Clouds on a dome round the camera; the same noise casts the cloud shadows);
    /// - wind: the global _WeatherWind the grass, leaf and fade shaders gust by, and the drift of leaves, ash, rain and snow;
    /// - rain (streaks, splashes on the ground, rings on the water, a darker, glossier wet ground), flurries, drifting mist or an ash
    ///   squall's dust, and a storm's lightning. Nothing falls under a roof, and the camera indoors sees none.
    /// Weather is not saved: it is a function of the weather clock and the zone.
    /// </summary>
    [DefaultExecutionOrder(100)]   // after the camera has moved: the dome and the emitters follow this frame's view
    public sealed class WorldWeather : MonoBehaviour
    {
        /// <summary>Weather time in seconds; negative until the first zone starts it. Static so it carries across zone travel.</summary>
        public static double Clock = -1;
        public static WorldWeather Active { get; private set; }
        /// <summary>This frame's weather (<see cref="WeatherLook.None"/> where no zone has weather).</summary>
        public static WeatherLook Now { get { return Active != null ? Active.shown : WeatherLook.None; } }
        /// <summary>A lightning flash, 0..~1.2 for a moment in a storm: the clock, the post and the clouds brighten by it.</summary>
        public static float Flash { get; private set; }
        /// <summary>Wind direction on the ground plane (x east, y north); it veers slowly with the weather clock.</summary>
        public static Vector2 WindDirection { get; private set; } = new Vector2(.8f, .6f);
        /// <summary>How far the cloud layer has drifted, in metres (kept within one tile of the noise).</summary>
        public static Vector2 CloudDrift { get; private set; }
        /// <summary>The noise the clouds and their shadows are cut from (grey, tiling; made once per session).</summary>
        public static Texture2D CloudNoise { get; private set; }
        /// <summary>The cloud layer's height, and the metres one repeat of <see cref="CloudNoise"/> covers.</summary>
        public const float CloudHeight = 900, CloudTile = 2600;
        /// <summary>Raised when the zone's own weather turns to a new kind (from, to); not when a kind is forced.</summary>
        public static event Action<WeatherKind, WeatherKind> Turned;

        public WeatherSchedule Schedule { get; private set; }
        /// <summary>The weather now: the forced kind, else the schedule's current spell.</summary>
        public WeatherKind Kind { get; private set; }
        public bool Forced { get { return forced.HasValue; } }
        /// <summary>How wet the ground is, 0..1: it soaks through rain and dries after.</summary>
        public float Wet { get { return wet; } }

        ZoneBuilder zone; Light sun; WeatherLook shown; float wet; WeatherKind? forced; WeatherKind scheduled;
        Material clouds, haze, droplets; Mesh domeMesh; Transform dome; ParticleSystem rain, splash, snow, drift;
        Material ground; Color groundColor; float groundGloss = -1;
        float splashDue, hazeDue, nextBolt, boltAt = -99; bool sheltered; Vector3 lastFocus; System.Random fx;

        public void Init(ZoneBuilder builder, Light sunLight)
        {
            Active = this; zone = builder; sun = sunLight; fx = new System.Random(builder.Zone.seed + 4099);
            Schedule = WeatherSchedule.For(builder.Zone);
            if (Clock < 0) Clock = Schedule.CalmFrom(UnityEngine.Random.Range(0, 48) * (double)WeatherSchedule.SpellSeconds);
            shown = Schedule.LookAt(Clock, out scheduled); Kind = scheduled; wet = Soaked(shown) ? 1 : 0;
            if (CloudNoise == null) CloudNoise = MakeCloudNoise(256, 7);
            ground = builder.GroundMaterial;
            if (ground != null) { groundColor = ground.color; if (ground.HasProperty("_Glossiness")) groundGloss = ground.GetFloat("_Glossiness"); }
            BuildSky(); BuildParticles();
            nextBolt = UnityEngine.Time.time + 6;
            // Asked for on the command line (--crulanda-weather rain); else the capture tours hold one calm look, so shots compare.
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "--crulanda-weather");
            if (at >= 0 && at + 1 < args.Length && WeatherSchedule.TryParse(args[at + 1], out var asked)) Force(asked, true);
            else if (Array.IndexOf(args, "--crulanda-capture") >= 0 || Array.IndexOf(args, "--crulanda-ui-capture") >= 0 || Array.IndexOf(args, "--crulanda-world-capture") >= 0) Force(TourKind(), true);
            else Step(0);
        }
        static bool Soaked(WeatherLook w) { return w.rain > .15f; }

        // ---------- forcing ----------
        /// <summary>Holds a kind (dev key, capture tours, tests) until <see cref="Release"/>. Instant: at once, particles and all; else it turns in.</summary>
        public void Force(WeatherKind kind, bool instant)
        {
            forced = kind; Kind = kind;
            if (!instant) return;
            shown = WeatherLook.Of(kind); wet = Soaked(shown) ? 1 : 0; Fill(); Step(0);
        }
        /// <summary>Back to the zone's own weather: at once, or turning over a spell's turn.</summary>
        public void Release(bool instant)
        {
            forced = null; Kind = scheduled;
            if (!instant) return;
            shown = Schedule.LookAt(Clock, out _); wet = Soaked(shown) ? 1 : 0; Fill(); Step(0);
        }
        /// <summary>Dev key: the zone's next kind at once, and after the last, the zone's own weather again. Returns what it is now.</summary>
        public string CycleForced()
        {
            var table = Schedule.Table; int i = -1;
            if (forced.HasValue) for (int k = 0; k < table.Count; k++) if (table[k].kind == forced.Value) i = k;
            if (forced.HasValue && i + 1 >= table.Count) { Release(true); return WeatherSchedule.Name(Kind) + " (the zone's own weather again)"; }
            Force(table[i + 1].kind, true);
            return WeatherSchedule.Name(Kind);
        }
        /// <summary>What the capture tours hold for their shots: fair (a few clouds) if the zone has it, else its calmest kind.</summary>
        public WeatherKind TourKind()
        {
            if (Schedule.Has(WeatherKind.Fair)) return WeatherKind.Fair;
            var best = Schedule.Table[0].kind;
            foreach (var t in Schedule.Table) if (WeatherSchedule.Severity(t.kind) < WeatherSchedule.Severity(best)) best = t.kind;
            return best;
        }
        /// <summary>The zone's unsettled weather (overcast and worse), calmest first: what the capture tour shows off.</summary>
        public List<WeatherKind> Showcase()
        {
            var list = new List<WeatherKind>();
            foreach (var t in Schedule.Table) if (WeatherSchedule.Severity(t.kind) >= 2) list.Add(t.kind);
            list.Sort((a, b) => WeatherSchedule.Severity(a).CompareTo(WeatherSchedule.Severity(b)));
            return list;
        }

        // ---------- each frame ----------
        void Update()
        {
            float dt = UnityEngine.Time.deltaTime;
            Clock += dt;
            var target = Schedule.LookAt(Clock, out var now);
            if (now != scheduled) { var was = scheduled; scheduled = now; if (!forced.HasValue) { Kind = now; Turned?.Invoke(was, now); } }
            if (forced.HasValue) target = WeatherLook.Of(forced.Value);
            shown = WeatherLook.Toward(shown, target, dt * 1.5f / WeatherSchedule.TurnSeconds);
            bool soaking = Soaked(shown);
            wet = Mathf.MoveTowards(wet, soaking ? 1 : 0, dt / (soaking ? 45f : 160f));
            Step(dt);
        }
        void Step(float dt)
        {
            float dark = WorldClock.Darkness;
            float veer = .64f + (float)(Math.Sin(Clock / 530.0) * .7 + Math.Sin(Clock / 173.0) * .2);
            var dir = new Vector2(Mathf.Cos(veer), Mathf.Sin(veer)); WindDirection = dir;
            Shader.SetGlobalVector("_WeatherWind", new Vector4(dir.x, dir.y, shown.wind, 0));
            Shader.SetGlobalVector("_WeatherTone", new Vector4(shown.dim * (1 - .5f * dark), shown.rain, 0, 0));   // the water dulls and roughens
            var drifted = CloudDrift + dir * (2.5f + 10 * shown.wind) * dt;
            CloudDrift = new Vector2(Mathf.Repeat(drifted.x, CloudTile * 10), Mathf.Repeat(drifted.y, CloudTile * 10));   // whole tiles of both noise scales (1 and 2.7): no jump
            Lightning();
            if (ground != null)
            {
                // Wet ground: darker, with a little sheen (much more and the sky's reflection paled it instead).
                var c = groundColor * (1 - .26f * wet); c.a = groundColor.a; ground.color = c;
                if (groundGloss >= 0) ground.SetFloat("_Glossiness", Mathf.Lerp(groundGloss, .32f, wet));
            }
            if (!Focus(out var cam, out var focus, out var ahead)) return;
            bool under = Hollow.DepthAt(cam) > .02f || zone.UnderRoof(new Vector2(cam.x, cam.z)) && Physics.Raycast(cam, Vector3.up, 20, ~0, QueryTriggerInteraction.Ignore);   // in a cave, or under a roof   // a ceiling overhead, not just beside a wall or above the roofs
            if (under && !sheltered) { if (rain != null) rain.Clear(); if (snow != null) snow.Clear(); }
            sheltered = under;
            bool jumped = (focus - lastFocus).sqrMagnitude > 30 * 30; lastFocus = focus;
            Rain(dt, cam, focus, ahead, dir, dark);
            Snow(focus, dir, dark, jumped);
            Haze(dt, focus, dir, dark, jumped);
        }
        /// <summary>The camera, a point on the ground plane 9 m ahead of it, and its flat heading.</summary>
        static bool Focus(out Vector3 cam, out Vector3 focus, out Vector2 ahead)
        {
            var c = Camera.main; cam = focus = Vector3.zero; ahead = Vector2.up;
            if (c == null) return false;
            cam = c.transform.position; var f = Vector3.ProjectOnPlane(c.transform.forward, Vector3.up);
            if (f.sqrMagnitude > 1e-4f) ahead = new Vector2(f.x, f.z).normalized;
            focus = cam + new Vector3(ahead.x, 0, ahead.y) * 9;
            return true;
        }
        void Lightning()
        {
            // A storm flashes every 8-26 s, the first 7-13 s after it breaks: a bright stroke and a second, weaker one a moment after.
            float t = UnityEngine.Time.time;
            bool storm = shown.rain > .85f && shown.wind > .8f;
            if (!storm) nextBolt = t + 7 + (float)fx.NextDouble() * 6;
            else if (t >= nextBolt) { boltAt = t; nextBolt = t + 8 + (float)fx.NextDouble() * 18; }
            float x = t - boltAt;
            Flash = x < 0 || x > 1.2f ? 0 : Mathf.Exp(-x * 16) + (x > .16f ? .75f * Mathf.Exp(-(x - .16f) * 11) : 0);
        }
        void Rain(float dt, Vector3 cam, Vector3 focus, Vector2 ahead, Vector2 dir, float dark)
        {
            if (rain == null) return;
            float amount = sheltered ? 0 : shown.rain, slant = 1.5f + 6 * shown.wind;
            var em = rain.emission; em.rateOverTime = amount * 4200;
            float groundY = zone.HeightAt(focus.x, focus.z), top = Mathf.Max(cam.y, groundY) + 13;
            // Born upwind of the view, so the drops blow into it as they fall; they live until they reach the ground.
            rain.transform.position = new Vector3(focus.x - dir.x * slant * .8f, top, focus.z - dir.y * slant * .8f);
            var main = rain.main; main.startLifetime = (top - groundY) / 15.5f + .2f;
            main.startColor = Color.Lerp(new Color(.78f, .82f, .9f, .42f), new Color(.32f, .36f, .44f, .3f), dark);
            var v = rain.velocityOverLifetime; v.x = dir.x * slant; v.y = -15.5f; v.z = dir.y * slant;
            if (amount <= .02f || splash == null) return;
            // Splashes where the drops land, mostly in front of the camera: droplets off the ground, rings on the water.
            splashDue += amount * 340 * dt;
            var tint = Color.Lerp(new Color(.82f, .86f, .92f, .6f), new Color(.34f, .38f, .46f, .45f), dark);
            float yaw = Mathf.Atan2(ahead.x, ahead.y);
            for (; splashDue >= 1; splashDue -= 1)
            {
                float a = yaw + ((float)fx.NextDouble() * 2 - 1) * 1.3f, d = Mathf.Lerp(1.2f, 17, Mathf.Sqrt((float)fx.NextDouble()));
                var p = new Vector2(cam.x + Mathf.Sin(a) * d, cam.z + Mathf.Cos(a) * d);
                if (zone.UnderRoof(p)) continue;
                float h = zone.HeightAt(p.x, p.y);
                if (zone.Water.At(p, h, out float surface, out _))
                {
                    if (Splashes.Active != null) Splashes.Active.Drop(new Vector3(p.x, surface, p.y), .16f + (float)fx.NextDouble() * .1f, .85f);
                    continue;
                }
                splash.Emit(new ParticleSystem.EmitParams { position = new Vector3(p.x, h + .03f, p.y), applyShapeToPosition = true, startColor = tint }, 2);
            }
        }
        void Snow(Vector3 focus, Vector2 dir, float dark, bool jumped)
        {
            if (snow == null) return;
            float amount = sheltered ? 0 : shown.snow;
            var em = snow.emission; em.rateOverTime = amount * 850;
            snow.transform.position = new Vector3(focus.x, zone.HeightAt(focus.x, focus.z) + 7, focus.z);
            if (jumped && amount > .1f) { snow.Clear(); snow.Emit(Mathf.RoundToInt(amount * 3600)); }   // a teleport: fill the air at once
            float push = .6f + 2.6f * shown.wind;
            var v = snow.velocityOverLifetime; v.x = dir.x * push; v.y = -.85f; v.z = dir.y * push;
            var main = snow.main; main.startColor = Color.Lerp(new Color(.96f, .97f, 1f, .92f), new Color(.46f, .5f, .6f, .7f), dark);
        }
        void Haze(float dt, Vector3 focus, Vector2 dir, float dark, bool jumped)
        {
            if (drift == null) return;
            float amount = HazeLook(dir, dark, out var tint, out var wind, out bool dust);
            if (amount <= .02f) return;
            if (jumped) { drift.Clear(); for (int i = 0; i < amount * 420; i++) EmitHaze(focus, tint, wind, dust, 3 + (float)fx.NextDouble() * 22); }   // arrived: the air is already thick
            hazeDue += amount * 22 * dt;
            for (; hazeDue >= 1; hazeDue -= 1) EmitHaze(focus, tint, wind, dust, 22 + (float)fx.NextDouble() * 8);
        }
        /// <summary>
        /// Drifting haze: mist (pale, low, slow) or an ash squall's dust (dun, higher, fast), whichever is thicker. Its colour
        /// comes from the fog at this hour, so it belongs to the zone's air by day and by night.
        /// </summary>
        float HazeLook(Vector2 dir, float dark, out Color tint, out Vector3 wind, out bool dust)
        {
            float mist = shown.mist, ash = shown.ash * .85f; dust = ash > mist;
            var fog = RenderSettings.fogColor; float g = fog.grayscale;
            tint = dust ? Color.Lerp(fog, new Color(g * 1.08f, g, g * .9f), .6f) : Color.Lerp(fog, Color.white, Mathf.Lerp(.3f, .1f, dark));
            float amount = Mathf.Max(mist, ash); tint.a = (dust ? .22f : .34f) * amount;
            wind = new Vector3(dir.x, 0, dir.y) * (dust ? 1.5f + 4 * shown.wind : .25f + .6f * shown.wind) + Vector3.up * .03f;
            return amount;
        }
        void EmitHaze(Vector3 focus, Color tint, Vector3 wind, bool dust, float life)
        {
            float a = (float)fx.NextDouble() * Mathf.PI * 2, d = Mathf.Lerp(9, 70, Mathf.Sqrt((float)fx.NextDouble()));
            var p = new Vector2(focus.x + Mathf.Cos(a) * d, focus.z + Mathf.Sin(a) * d);
            if (Mathf.Abs(p.x) > zone.Half + 30 || Mathf.Abs(p.y) > zone.Half + 30) return;
            float y = zone.HeightAt(p.x, p.y) + (dust ? 1 + (float)fx.NextDouble() * 5 : .3f + (float)fx.NextDouble() * 1.6f);   // mist hugs the ground
            drift.Emit(new ParticleSystem.EmitParams { position = new Vector3(p.x, y, p.y), velocity = wind, startColor = tint,
                startSize = (dust ? 5 : 7) + (float)fx.NextDouble() * 6, startLifetime = life, rotation = (float)fx.NextDouble() * 360 }, 1);
        }
        /// <summary>A forced change lands at once: the air is filled with its snow or haze (rain refills itself within a second).</summary>
        void Fill()
        {
            if (rain != null) rain.Clear();
            if (!Focus(out _, out var focus, out _)) return;
            lastFocus = focus;
            if (snow != null)
            {
                snow.Clear();
                if (shown.snow > .1f) { snow.transform.position = new Vector3(focus.x, zone.HeightAt(focus.x, focus.z) + 7, focus.z); snow.Emit(Mathf.RoundToInt(shown.snow * 3600)); }
            }
            if (drift != null)
            {
                drift.Clear();
                float amount = HazeLook(WindDirection, WorldClock.Darkness, out var tint, out var wind, out bool dust);
                for (int i = 0; i < amount * 420; i++) EmitHaze(focus, tint, wind, dust, 3 + (float)fx.NextDouble() * 22);
            }
        }
        void LateUpdate()
        {
            // The cloud layer rides with the camera (it is a sky: never reached), its colours from the hour, the sun and the air.
            var cam = Camera.main; if (dome == null || cam == null) return;
            dome.position = cam.transform.position; dome.localScale = Vector3.one * cam.farClipPlane * .9f;
            float dark = WorldClock.Darkness, overcast = Mathf.Clamp01((shown.dim - .25f) / .45f); var fog = RenderSettings.fogColor;
            float g = fog.grayscale; var grey = new Color(g * .98f, g, g * 1.03f);                          // the air's own grey, a touch cool
            var top = Color.Lerp(new Color(1, .985f, .96f), sun != null ? sun.color : Color.white, .35f);   // sunlit tops, warmed at dawn and dusk
            var under = new Color(.62f, .67f, .77f);                                                         // blue-grey undersides
            top = Color.Lerp(top, grey * 1.18f, .8f * overcast);                                               // under cover: a grey lid,
            under = Color.Lerp(under, grey * .86f, .3f + .6f * overcast) * (1 - .28f * shown.rain);           // darker with rain
            top = Color.Lerp(top, fog * 1.7f + new Color(.02f, .03f, .06f), dark); under = Color.Lerp(under, fog * 1.05f, dark);   // night: dim slate
            top *= 1 + Flash * 1.2f; under *= 1 + Flash; top.a = under.a = 1; fog.a = 1;
            clouds.SetColor("_TopColor", top); clouds.SetColor("_UnderColor", under); clouds.SetColor("_HorizonColor", fog);
            clouds.SetFloat("_Coverage", shown.clouds); clouds.SetFloat("_Overcast", overcast); clouds.SetFloat("_Softness", Mathf.Lerp(.11f, .22f, overcast));
            clouds.SetFloat("_Opacity", Mathf.Lerp(.96f, .85f, dark));
            clouds.SetVector("_Drift", CloudDrift); clouds.SetFloat("_Height", CloudHeight); clouds.SetFloat("_Tile", CloudTile);
            clouds.SetVector("_SunDir", sun != null ? -sun.transform.forward : Vector3.up);
        }

        // ---------- building ----------
        void BuildSky()
        {
            if (zone.art.clouds == null) return;
            clouds = new Material(zone.art.clouds) { name = "Cloud layer", mainTexture = CloudNoise };
            var go = new GameObject("Cloud layer"); go.transform.SetParent(transform, false);
            domeMesh = Dome(32, 12); go.AddComponent<MeshFilter>().sharedMesh = domeMesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = clouds;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            dome = go.transform;
        }
        /// <summary>A unit dome from just under the horizon to the zenith (the shader only needs each vertex's direction).</summary>
        static Mesh Dome(int around, int up)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int j = 0; j <= up; j++)
            {
                float el = Mathf.Lerp(-6, 90, j / (float)up) * Mathf.Deg2Rad;
                for (int i = 0; i <= around; i++) { float az = i * Mathf.PI * 2 / around; v.Add(new Vector3(Mathf.Cos(el) * Mathf.Cos(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Sin(az))); }
            }
            for (int j = 0; j < up; j++)
                for (int i = 0; i < around; i++) { int a = j * (around + 1) + i, b = a + around + 1; t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            var m = new Mesh { name = "Cloud dome" }; m.SetVertices(v); m.SetTriangles(t, 0); m.bounds = new Bounds(Vector3.zero, Vector3.one * 2);
            return m;
        }
        void BuildParticles()
        {
            var mat = zone.art.splash; if (mat == null) return;   // the soft dot: streaks when stretched, droplets, flakes and puffs
            // Rain: streaks stretched along their fall, from a box above and ahead of the camera.
            rain = Particles("Rain", mat, 7000, true);
            var main = rain.main; main.startLifetime = 1.3f; main.startSize = new ParticleSystem.MinMaxCurve(.034f, .052f);
            var shape = rain.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(34, .1f, 34);
            var v = rain.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = 0; v.y = -15.5f; v.z = 0;
            var r = rain.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = .045f; r.lengthScale = 1.2f; r.cameraVelocityScale = 0; r.maxParticleSize = .01f;   // a drop past the lens stays a streak, not a white bar
            rain.Play();
            // Splash droplets where drops hit the ground (emitted by hand).
            droplets = new Material(mat) { name = "Rain splash" }; if (droplets.HasProperty("_InvFade")) droplets.SetFloat("_InvFade", 30);   // they live a hand's breadth off the ground: no soft fade
            splash = Particles("Rain splashes", droplets, 1500, false);
            main = splash.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.18f, .3f); main.startSpeed = new ParticleSystem.MinMaxCurve(.8f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(.04f, .08f); main.gravityModifier = 1.3f;
            shape = splash.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 32; shape.radius = .03f; shape.rotation = new Vector3(-90, 0, 0);
            FadeOut(splash, 0, 1);
            // Flurries: flakes through the whole air column, tumbling on the wind.
            snow = Particles("Flurries", mat, 6000, true);
            main = snow.main; main.startLifetime = new ParticleSystem.MinMaxCurve(9, 15); main.startSize = new ParticleSystem.MinMaxCurve(.055f, .13f);
            shape = snow.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(50, 16, 50);
            var noise = snow.noise; noise.enabled = true; noise.strength = .6f; noise.frequency = .35f; noise.scrollSpeed = .25f;
            v = snow.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = 0; v.y = -.85f; v.z = 0;
            r = snow.GetComponent<ParticleSystemRenderer>(); r.maxParticleSize = .012f;   // a flake passing the lens stays a flake
            FadeOut(snow, .06f, .85f);
            snow.Play();
            // Haze: big soft puffs of mist or dust, faded where they meet the ground (soft particles).
            haze = new Material(mat) { name = "Weather haze" }; if (haze.HasProperty("_InvFade")) haze.SetFloat("_InvFade", .3f);
            drift = Particles("Drifting haze", haze, 1200, false);
            main = drift.main; main.startLifetime = 24; main.startSize = 9;
            var spin = drift.rotationOverLifetime; spin.enabled = true; spin.z = new ParticleSystem.MinMaxCurve(-.06f, .06f);
            r = drift.GetComponent<ParticleSystemRenderer>(); r.sortMode = ParticleSystemSortMode.Distance; r.maxParticleSize = .45f;
            FadeOut(drift, .25f, .7f);
        }
        ParticleSystem Particles(string name, Material mat, int max, bool loop)
        {
            var ps = new GameObject(name).AddComponent<ParticleSystem>(); ps.transform.SetParent(transform, false);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = loop; main.duration = 5; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max; main.startSpeed = 0; main.gravityModifier = 0;
            var em = ps.emission; em.rateOverTime = 0;
            var shape = ps.shape; shape.enabled = false;
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return ps;
        }
        /// <summary>Alpha over a particle's life: in by <paramref name="shown"/>, full until <paramref name="gone"/>, then out.</summary>
        static void FadeOut(ParticleSystem ps, float shown, float gone)
        {
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            var alpha = shown > 0 ? new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, shown), new GradientAlphaKey(1, gone), new GradientAlphaKey(0, 1) }
                                  : new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, gone) };
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, alpha); col.color = g;
        }
        /// <summary>
        /// Tiling cloud noise: five octaves of smooth value noise wrapped on the tile, then evened out by rank, so a coverage of c
        /// cuts about c of the texels. Grey, mipmapped (toward the horizon the layer is seen from very far off).
        /// </summary>
        static Texture2D MakeCloudNoise(int n, int seed)
        {
            const int Octaves = 5, BaseCells = 4;
            var rnd = new System.Random(seed); var lattice = new float[Octaves][];
            for (int o = 0; o < Octaves; o++) { int cells = BaseCells << o; lattice[o] = new float[cells * cells]; for (int i = 0; i < lattice[o].Length; i++) lattice[o][i] = (float)rnd.NextDouble(); }
            var values = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float sum = 0, amp = 1, norm = 0;
                    for (int o = 0; o < Octaves; o++)
                    {
                        int cells = BaseCells << o; float gx = x * cells / (float)n, gy = y * cells / (float)n;
                        int x0 = (int)gx, y0 = (int)gy; float tx = gx - x0, ty = gy - y0;
                        tx = tx * tx * tx * (tx * (tx * 6 - 15) + 10); ty = ty * ty * ty * (ty * (ty * 6 - 15) + 10);
                        int x1 = (x0 + 1) % cells, y1 = (y0 + 1) % cells; var l = lattice[o];
                        float a = l[y0 * cells + x0], b = l[y0 * cells + x1], c = l[y1 * cells + x0], d = l[y1 * cells + x1];
                        sum += (a + (b - a) * tx + (c - a) * ty + (a - b - c + d) * tx * ty) * amp; norm += amp; amp *= .5f;
                    }
                    values[y * n + x] = sum / norm;
                }
            var order = new int[values.Length]; for (int i = 0; i < order.Length; i++) order[i] = i;
            Array.Sort((float[])values.Clone(), order);
            var px = new Color32[values.Length];
            for (int rank = 0; rank < order.Length; rank++) { byte g = (byte)(rank * 255 / (order.Length - 1)); px[order[rank]] = new Color32(g, g, g, 255); }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "Cloud noise", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }
        void OnDestroy()
        {
            if (Active == this) { Active = null; Flash = 0; Shader.SetGlobalVector("_WeatherWind", Vector4.zero); Shader.SetGlobalVector("_WeatherTone", Vector4.zero); }
            if (ground != null) { ground.color = groundColor; if (groundGloss >= 0) ground.SetFloat("_Glossiness", groundGloss); }
            if (clouds != null) Destroy(clouds);
            if (haze != null) Destroy(haze);
            if (droplets != null) Destroy(droplets);
            if (domeMesh != null) Destroy(domeMesh);
        }
    }
}
