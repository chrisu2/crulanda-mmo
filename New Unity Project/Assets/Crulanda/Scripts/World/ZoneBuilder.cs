using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Generates a zone from its JSON definition at load: sculpted ground with a painted texture (grass, roads,
    /// fields, creek banks, unmade grey), water, buildings and props from primitives/procedural meshes, forest
    /// edges, the Wasting curtain, lighting and boundaries. Deterministic for a given zone seed.
    /// Runs before EncounterNavigation so the navigation mesh can be built from what it creates.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed partial class ZoneBuilder : MonoBehaviour
    {
        public TextAsset zoneJson;
        [Tooltip("Every zone this scene can build; travel picks one by id. zoneJson is the default/opening zone.")]
        public TextAsset[] zones = new TextAsset[0];
        public ZoneArt art;
        /// <summary>Set before reloading the scene to build a different zone (zone travel).</summary>
        public static string RequestedZoneId;
        public static ZoneBuilder Active { get; private set; }
        /// <summary>Tests only: changes the zone's definition after it is chosen and before anything is built (to build a zone
        /// without some of its props and compare the two). Null in play; a test that sets it clears it in its teardown.</summary>
        public static Func<ZoneDefinition, ZoneDefinition> DefinitionFilter;
        public bool HasZone(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (var z in zones) if (z != null && JsonUtility.FromJson<ZoneDefinition>(z.text)?.id == id) return true;
            return false;
        }
        /// <summary>A zone by id, from the zones parsed once (<see cref="AllZones"/>). It parsed every zone's JSON at each call, and the HUD
        /// asked for each road out every frame: 2 ms and most of 770 KB of garbage a frame (playtest note 23).</summary>
        /// <summary>The zone to build, parsed fresh from its JSON (this zone's own copy, which the build may adjust).</summary>
        ZoneDefinition ParsedZone(string id)
        {
            foreach (var z in zones) { var d = z == null ? null : ParseZone(z.text); if (d != null && d.id == id) return d; }
            return null;
        }
        public ZoneDefinition FindZone(string id)
        {
            foreach (var d in AllZones()) if (d != null && d.id == id) return d;
            return null;
        }
        /// <summary>
        /// A zone from its JSON. JsonUtility never leaves a nested class null, so a zone without a "wasting" block got a default
        /// Wasting at x = 58 (grey unmade ground, a static curtain and a wall across Khaven's Carrion Cliffs and the Peaks' toll
        /// road east). Only zones that describe their Wasting have one.
        /// </summary>
        static ZoneDefinition ParseZone(string json)
        {
            var d = JsonUtility.FromJson<ZoneDefinition>(json);
            if (d != null && !json.Contains("\"wasting\"")) d.wasting = null;
            return d;
        }
        public ZoneDefinition Zone { get; private set; }
        public MeshFilter GroundMesh { get; private set; }
        /// <summary>The zone's own ground material (WorldWeather darkens and glosses it when the ground is wet).</summary>
        public Material GroundMaterial { get; private set; }
        public readonly List<ZoneDoor> Doors = new List<ZoneDoor>();
        public float Half { get { return Zone.size / 2; } }
        Transform props, statics;
        System.Random rng;
        readonly Dictionary<string, Material> tints = new Dictionary<string, Material>();

        void Awake()
        {
            Zone = (RequestedZoneId != null ? ParsedZone(RequestedZoneId) : null) ?? ParseZone(zoneJson != null ? zoneJson.text : "{}");
            RequestedZoneId = null;
            if (Zone != null && DefinitionFilter != null) Zone = DefinitionFilter(Zone);
            if (Zone == null || string.IsNullOrEmpty(Zone.id) || art == null) { Debug.LogError("Zone definition or art missing."); enabled = false; return; }
            Active = this;
            var built = System.Diagnostics.Stopwatch.StartNew(); var phases = new System.Text.StringBuilder(); long mark = 0;
            void Lap(string what) { long now = built.ElapsedMilliseconds; phases.Append(what).Append(' ').Append(now - mark).Append(", "); mark = now; }   // where a zone's load time goes
            rng = new System.Random(Zone.seed);
            props = new GameObject("Zone props").transform; props.SetParent(transform, false);
            statics = new GameObject("Zone static scenery").transform; statics.SetParent(transform, false);
            PrepareRelief(); PrepareShapes(); Water = new ZoneWater(); Water.Prepare(Zone, (x, z) => HeightAt(x, z, false)); PrepareHollows(); Lap("prepare");
            BuildLighting(); BuildGround(); Lap("ground"); BuildWater(); BuildWasting(); Lap("water"); BuildProps(); BuildHomeDoors(); Lap("props"); BuildGroves(); BuildExits(); Lap("groves");
            BuildForestEdge(); BuildBoundaries(); Lap("edge"); BuildBackdrop(); DistantRanges.Build(transform, Zone, Half + BackdropWidth); Lap("backdrop");
            if (art.grass != null && art.grass.Length > 0)
            {
                // Gloom: the tufts are dry, greyed straw and no wildflowers bloom.
                var grass = Gloom ? art.grass.Select(m => new Material(m) { name = m.name + " (dry)", color = Color.Lerp(m.color, new Color(.5f, .46f, .37f), .75f), enableInstancing = true }).ToArray() : art.grass;
                // Gloom: tall grass is dark grey dead stalks, a little shorter and thinner, so a boar or wolf breaking out of it shows.
                var tall = Gloom ? art.grass.Select(m => new Material(m) { name = m.name + " (dead, tall)", color = Color.Lerp(m.color, new Color(.36f, .35f, .34f), .9f), enableInstancing = true }).ToArray() : null;
                // Meadow and mountain: the shared tufts are yellow-olive, so they are pulled toward each zone's turf green.
                var lush = Zone.biome == "verdant" ? art.grass.Select(m => new Material(m) { name = m.name + " (lush)", color = Color.Lerp(m.color, new Color(.3f, .62f, .22f), .55f), enableInstancing = true }).ToArray()
                    : Zone.biome == "meadow" ? art.grass.Select(m => new Material(m) { name = m.name + " (meadow)", color = Color.Lerp(m.color, new Color(.34f, .6f, .2f), .4f), enableInstancing = true }).ToArray()
                    : Zone.biome == "mountain" ? art.grass.Select(m => new Material(m) { name = m.name + " (alpine)", color = Color.Lerp(m.color, new Color(.3f, .5f, .25f), .35f), enableInstancing = true }).ToArray()
                    : grass;
                gameObject.AddComponent<GrassField>().Build(this, lush, Gloom ? null : art.flowers, Openness, Zone.seed + 99, Zone.biome == "meadow" ? 3f : Zone.biome == "verdant" ? 2.8f : 1.6f, TallGrassPatches(), tall, Gloom ? .8f : Zone.biome == "verdant" ? 1.25f : 1, Gloom ? .7f : 1,
                    Zone.wildFrom > 0 ? Wild : (Func<Vector2, float>)null);
                // The grass runs on past the edge over the backdrop's near slope, thinning to nothing (the same tufts, at the field's density at the line).
                GetComponent<GrassField>().BuildEdge(Half, lush, Gloom ? null : art.flowers, EdgeOpenness, EdgeGround, Zone.seed + 101, (Zone.biome == "meadow" ? 3f : Zone.biome == "verdant" ? 2.8f : 1.6f) * Wild(new Vector2(Half, 0)), EdgeDressing);
            }
            Lap("grass");
            if (art.fern != null || art.broadLeaf != null || art.reeds != null) gameObject.AddComponent<PlantField>().Build(this, art, Gloom ? null : art.flowers, Openness, Zone.seed + 177, EdgeOpenness, EdgeGround, EdgeDressing);
            Lap("plants");
            gameObject.AddComponent<FallingLeaves>().Init(this);
            Splashes.Ensure(this); TreeFade.Begin(art.fade);
            var view = Camera.main;
            if (view != null && art.post != null && view.GetComponent<ZonePost>() == null) view.gameObject.AddComponent<ZonePost>().Init(art.post, Zone, sunLight);
            int before = MergeStatics();
            try { StaticBatchingUtility.Combine(statics.gameObject); } catch (Exception e) { Debug.LogWarning("Static batching skipped: " + e.Message); }
            Debug.Log("Static scenery merged: " + before + " renderers into " + statics.GetComponentsInChildren<MeshRenderer>().Length + ".");
            Lap("batching");
            // In daylight, before the clock sets the hour; about 4 px a metre (1024 for the old 260 m zones).
            MapTexture = RenderMap(Mathf.Clamp(Mathf.RoundToInt(Zone.size * 4 / 256f) * 256, 1024, 2048));
            Lap("map");
            BuildSecrets();   // after the map: a hidden find must never show on the minimap or the zone map
            BuildNodes();     // after the secrets (they keep clear of them), each from a stream of its own: nothing else moves
            BuildStations();  // the trades' stations of their own (field anvils, benches, cookfires): streams of their own, no colliders
            gameObject.AddComponent<WorldWeather>().Init(this, sunLight);   // before the clock: its first light already has the weather in it
            var clock = gameObject.AddComponent<WorldClock>(); clock.Init(sunLight, Zone.lighting, art.skybox, NightLights);
            // Reflections follow the real sky: a sky-only realtime probe covering the zone, refreshed by the clock.
            var probe = new GameObject("Sky reflection").AddComponent<ReflectionProbe>(); probe.transform.SetParent(transform, false);
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime; probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing; probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
            probe.cullingMask = 0; probe.resolution = 64; probe.hdr = true; probe.size = new Vector3(Zone.size + 40, 400, Zone.size + 40); probe.importance = 0;
            clock.Reflections = probe;
            Lap("rest");
            Debug.Log("Zone " + Zone.id + " (" + Zone.size + " m) built in " + built.ElapsedMilliseconds + " ms (" + phases.ToString().TrimEnd(',', ' ') + ").");
        }
        Light sunLight;
        /// <summary>North-up top-down picture of the zone (before characters spawn), used by the minimap and zone map.</summary>
        public Texture2D MapTexture { get; private set; }
        Texture2D RenderMap(int res)
        {
            var map = new Texture2D(res, res, TextureFormat.RGB24, false) { name = "Zone map", wrapMode = TextureWrapMode.Clamp };
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return map;   // batch tests: no renderer
            var rt = RenderTexture.GetTemporary(res, res, 24);
            var go = new GameObject("Map camera"); var cam = go.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = Zone.size / 2; cam.transform.position = new Vector3(0, 150, 0);
            cam.transform.rotation = Quaternion.Euler(90, 0, 0); cam.nearClipPlane = 1; cam.farClipPlane = 300;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.12f, .12f, .1f); cam.targetTexture = rt; cam.enabled = false;
            bool fog = RenderSettings.fog; RenderSettings.fog = false;
            var previous = RenderTexture.active;
            try { cam.Render(); RenderTexture.active = rt; map.ReadPixels(new Rect(0, 0, res, res), 0, 0); map.Apply(false); }
            finally { RenderTexture.active = previous; RenderSettings.fog = fog; cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt); Destroy(go); }
            return map;
        }
        List<ZoneDefinition> all;
        /// <summary>Every registered zone (for the world map).</summary>
        public List<ZoneDefinition> AllZones()
        {
            if (all != null) return all;
            all = new List<ZoneDefinition>();
            foreach (var z in zones) { var d = z == null ? null : ParseZone(z.text); if (d != null && !string.IsNullOrEmpty(d.id)) all.Add(d); }
            if (!all.Exists(d => d.id == Zone.id)) all.Add(Zone);
            return all;
        }
        /// <summary>World position to 0..1 map coordinates (x east, y north).</summary>
        public Vector2 MapUV(Vector3 world) { return new Vector2((world.x + Half) / Zone.size, (world.z + Half) / Zone.size); }
        void OnDestroy() { if (Active == this) Active = null; }

        // ---------- ground height ----------
        public float HeightAt(float x, float z) { return HeightAt(x, z, true); }
        /// <summary>Ground height; carveWater=false gives the bank level a creek surface should sit under.</summary>
        public float HeightAt(float x, float z, bool carveWater)
        {
            float r = new Vector2(x, z).magnitude;
            float hills = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(Zone.flatRadius, Zone.flatRadius + 22, r));
            float n = Mathf.PerlinNoise(x * .035f + Zone.seed, z * .035f + Zone.seed * 2) * .75f + Mathf.PerlinNoise(x * .11f, z * .11f) * .25f;
            float h = hills * Zone.hillHeight * n + (hills > 0 ? hills * Crag(x, z) : 0) + CliffLift(x, z);   // mountain crags off the kept ways; shelves behind cliffs
            if (Zone.wasting != null && x > Zone.wasting.x - 4) h *= Mathf.Clamp01((Zone.wasting.x + 2 - x) / 6);  // unmade ground is dead flat
            if (shapes != null) h = Shape(x, z, h);   // landmark ground: level pads, perches and basins (ZoneShape)
            if (carveWater && Water != null) h = Water.Carve(x, z, h);   // creeks and lakes (see ZoneWater)
            return h;
        }
        ZoneShape[] shapes;   // the zone's landmark ground, live once PrepareShapes has levelled its pads
        /// <summary>
        /// Levels each pad (after PrepareRelief, so crags and cliff lifts count): a pad or basin at the mean ground round its edge
        /// plus its height; a perch its height above the lowest ground round its foot (radius + blend), so no way up it climbs
        /// more than that, however high the crags beside it.
        /// </summary>
        void PrepareShapes()
        {
            if (Zone.shapes == null || Zone.shapes.Length == 0) return;
            foreach (var s in Zone.shapes)
            {
                if (s == null) continue;
                bool perch = s.height > 0; float r = s.radius + (perch ? Mathf.Max(.5f, s.blend) : 0), sum = 0, low = float.MaxValue;
                for (int i = 0; i < 32; i++) { float a = i * Mathf.PI / 16, y = HeightAt(s.center.x + Mathf.Cos(a) * r, s.center.y + Mathf.Sin(a) * r, false); sum += y; low = Mathf.Min(low, y); }
                s.level = (perch ? low : sum / 32) + s.height;
            }
            shapes = Zone.shapes;
        }
        /// <summary>Landmark ground over the land: each pad levels it (a perch raised, a basin sunk), easing back over its blend.</summary>
        float Shape(float x, float z, float h)
        {
            var p = new Vector2(x, z);
            foreach (var s in shapes)
            {
                if (s == null) continue;
                float blend = Mathf.Max(.5f, s.blend), d = Vector2.Distance(p, s.center) - s.radius;
                if (d < blend) h = Mathf.Lerp(h, s.level, 1 - Mathf.SmoothStep(0, 1, d / blend));
            }
            return h;
        }
        /// <summary>How much of a pad's ground paint covers p (0..1): the pad right across, fading out a little past its rim.</summary>
        /// <summary>Whether a point lies on a shape painted <paramref name="paint"/> (its blend included).</summary>
        bool OnPaint(Vector2 p, string paint)
        {
            if (Zone.shapes == null) return false;
            foreach (var s in Zone.shapes) if (s != null && s.paint == paint && ShapeCover(s, p) > .2f) return true;
            return false;
        }
        static float ShapeCover(ZoneShape s, Vector2 p)
        {
            float blend = Mathf.Max(.5f, s.blend), fade = blend * .6f + 1, reach = s.radius + fade;
            if ((p - s.center).sqrMagnitude >= reach * reach) return 0;   // most of the zone: a cheap reject (PaintGround asks per pixel)
            return 1 - Mathf.SmoothStep(0, 1, (Vector2.Distance(p, s.center) - s.radius) / fade);
        }
        public Vector3 Ground(Vector2 p, float lift = 0) { return new Vector3(p.x, HeightAt(p.x, p.y) + lift, p.y); }
        /// <summary>
        /// <see cref="Ground"/> for points that never move (a landmark, a camp, an exit, a quest place), worked out once: the HUD asked
        /// for every landmark's height every frame for its place names and the minimap, and the terrain's height is costly (playtest
        /// note 23: 2.1 ms a frame).
        /// </summary>
        public Vector3 GroundFixed(Vector2 p, float lift = 0)
        {
            if (!fixedGround.TryGetValue(p, out float h)) { if (fixedGround.Count > 4000) fixedGround.Clear(); fixedGround[p] = h = HeightAt(p.x, p.y); }
            return new Vector3(p.x, h + lift, p.y);
        }
        readonly Dictionary<Vector2, float> fixedGround = new Dictionary<Vector2, float>();
        /// <summary>
        /// Where to stand at a point: on the land, or on a cave passage's floor under it (Hollow), whichever is nearer the height
        /// <paramref name="near"/> (where the thing was, or float.NegativeInfinity to prefer any passage floor): camps and their
        /// respawns inside a cave, a save made down in one, and secrets tucked into one.
        /// </summary>
        public Vector3 StandAt(Vector2 p, float near, float lift = 0)
        {
            float land = HeightAt(p.x, p.y), floor = land;
            if (Hollow.FloorUnder(p, ref floor) && (float.IsNegativeInfinity(near) || Mathf.Abs(near - floor) < Mathf.Abs(near - land))) return new Vector3(p.x, floor + lift, p.y);
            return new Vector3(p.x, land + lift, p.y);
        }
        static List<Vector2> Densify(Vector2[] pts, float step)
        {
            var list = new List<Vector2>();
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(pts[i], pts[i + 1]) / step));
                for (int k = 0; k < n; k++) list.Add(Vector2.Lerp(pts[i], pts[i + 1], (float)k / n));
            }
            if (pts.Length > 0) list.Add(pts[pts.Length - 1]);
            return list;
        }
        /// <summary>Distance to a polyline; <paramref name="interior"/> is false when the nearest point is an open end cap.</summary>
        static float DistanceToPath(Vector2 p, Vector2[] pts, out bool interior)
        {
            float best = float.MaxValue; interior = true;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1], ab = b - a;
                float raw = Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude), t = Mathf.Clamp01(raw);
                float d = Vector2.Distance(p, a + ab * t);
                if (d < best) { best = d; interior = !((i == 0 && raw < 0) || (i == pts.Length - 2 && raw > 1)); }
            }
            return best;
        }
        static float DistanceToPath(Vector2 p, Vector2[] pts)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        // ---------- relief ----------
        // Mountain zones get crags on top of the rolling hills: steep knolls, narrow rock ribs and walls that climb to the edge
        // and on under the backdrop. They rise only where the keep grid is 0: never on a road, clearing, camp, prop, person,
        // landmark, exit or arrival, nor on a 6 m way from each of those to its nearest road, so all of it stays reachable.
        // In any zone, a cliff prop with a lift raises the ground behind it into a shelf that high, so the crag reads as the
        // face of a scarp or ledge instead of blocks standing on a lawn.
        const float KeepStep = 2;
        float[] keep; int keepN;
        float[] groundUp;   // the ground grid's normal.y (1 on the flat), for slope-aware paint and grass
        float[] groundY; Vector3[] groundN;   // the ground grid's heights and normals (the drawn surface; sun-aware mountain paint)
        readonly List<(Vector2 at, float cos, float sin, float half, float lift)> lifts = new List<(Vector2, float, float, float, float)>();
        void PrepareRelief()
        {
            foreach (var p in Zone.props)
                if (p != null && p.kind == "cliff" && p.lift != 0)
                    lifts.Add((p.at, Mathf.Cos(p.rotation * Mathf.Deg2Rad), Mathf.Sin(p.rotation * Mathf.Deg2Rad), (p.size.x > 0 ? p.size.x : 20) / 2, p.lift));
            if (Zone.biome != "mountain") return;
            var circles = new List<(Vector2 c, float r)>(); var lines = new List<(Vector2 a, Vector2 b, float w)>();
            foreach (var road in Zone.roads) for (int i = 0; i + 1 < road.points.Length; i++) lines.Add((road.points[i], road.points[i + 1], road.width / 2 + 3));
            foreach (var c in Zone.clearings) circles.Add((c.center, c.radius + 3));
            foreach (var c in Zone.camps) circles.Add((c.center, c.radius + 4));
            foreach (var e in Zone.exits) circles.Add((e.at, e.radius + 4));
            foreach (var l in Zone.landmarks) circles.Add((l.at, Mathf.Min(l.radius * .5f, 6)));
            foreach (var z in AllZones()) foreach (var e in z.exits) if (e.to == Zone.id) circles.Add((e.arrive, 5));
            circles.Add((Zone.spawns.player, 5)); circles.Add((Zone.spawns.companion, 4)); circles.Add((Zone.spawns.recovery, 4));
            // The low side of each raised cliff stays level for a few metres, so its face stands clear above the ground there.
            foreach (var (at, cos, sin, half, lift) in lifts)
            {
                Vector2 along = new Vector2(cos, -sin) * half, front = new Vector2(sin, cos) * (-Mathf.Sign(lift) * 7);
                lines.Add((at + along + front, at - along + front, 4));
            }
            foreach (var p in Zone.props)
            {
                if (p == null || p.kind == "cliff" || (p.kind == "rock" && string.IsNullOrEmpty(p.interact))) continue;   // crags and loose rocks may sink into it
                if (p.kind == "wall") { for (int i = 0; i + 1 < p.points.Length; i++) lines.Add((p.points[i], p.points[i + 1], 3)); continue; }
                circles.Add((p.at, Mathf.Max(p.size.x, p.size.y) * .75f + 3));
            }
            if (Zone.life != null)
            {
                foreach (var r in Zone.life.residents) if (r != null) circles.Add((r.at, 4));
                foreach (var c in Zone.life.critters) if (c != null) circles.Add((c.center, Mathf.Min(c.radius * .7f, 8)));
            }
            var roadPts = new List<Vector2>(); foreach (var road in Zone.roads) roadPts.AddRange(Densify(road.points, 2));
            if (roadPts.Count > 0)
                foreach (var (c, _) in circles)
                {
                    var near = roadPts[0]; foreach (var q in roadPts) if ((q - c).sqrMagnitude < (near - c).sqrMagnitude) near = q;
                    lines.Add((c, near, 3));
                }
            keepN = Mathf.CeilToInt(Zone.size / KeepStep) + 1; keep = new float[keepN * keepN];
            for (int j = 0; j < keepN; j++)
                for (int i = 0; i < keepN; i++)
                {
                    var p = new Vector2(-Half + i * KeepStep, -Half + j * KeepStep); float d = float.MaxValue;
                    foreach (var (c, r) in circles) d = Mathf.Min(d, Vector2.Distance(p, c) - r);
                    foreach (var (a, b, w) in lines) d = Mathf.Min(d, Segment(p, a, b) - w);
                    keep[j * keepN + i] = 1 - Mathf.SmoothStep(0, 1, d / 10);   // held out to d = 0, free 10 m further on
                }
        }
        static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }
        /// <summary>Keep grid at a point (1 held, 0 free). Past the edge it mirrors back like the backdrop's paint, so a road's
        /// notch runs on out with its mirrored paint.</summary>
        float Keep(float x, float z)
        {
            if (keep == null) return 1;
            float s = Zone.size, h = Half;
            if (x > h) x = s - x; else if (x < -h) x = -s - x;
            if (z > h) z = s - z; else if (z < -h) z = -s - z;
            float fx = Mathf.Clamp((x + h) / KeepStep, 0, keepN - 1.001f), fz = Mathf.Clamp((z + h) / KeepStep, 0, keepN - 1.001f);
            int ix = (int)fx, iz = (int)fz; fx -= ix; fz -= iz; int i = iz * keepN + ix;
            return Mathf.Lerp(Mathf.Lerp(keep[i], keep[i + 1], fx), Mathf.Lerp(keep[i + keepN], keep[i + keepN + 1], fx), fz);
        }
        /// <summary>Mountain relief added to the rolling hills at a point: 0 off mountains and wherever the keep grid holds.</summary>
        float Crag(float x, float z)
        {
            if (keep == null) return 0;
            float free = 1 - Keep(x, z); if (free <= 0) return 0;
            float cheb = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            float rib = 1 - Mathf.Abs(Mathf.PerlinNoise(x * .019f + 170, z * .019f + 60) * 2 - 1);    // ridged: 1 on a crest line
            float peak = 1 - Mathf.Abs(Mathf.PerlinNoise(x * .031f + 12, z * .031f + 140) * 2 - 1);
            float knoll = Mathf.Clamp01((Mathf.PerlinNoise(x * .043f + 33, z * .043f + 81) - .52f) * 4.5f);
            float edge = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(Half - 40, Half - 6, cheb));
            rib *= rib; rib *= rib;
            // Narrow rock ribs, steep knolls, and walls with jagged crests toward the edge; past it the backdrop's ridges take over.
            float h = rib * 5 + knoll * knoll * 8 + edge * (6 + peak * peak * 14);
            return h * free * (1 - .6f * Mathf.SmoothStep(0, 1, (cheb - Half) / 60));
        }
        /// <summary>Ground raised behind cliff props (lift metres on the prop's +z side, or its -z side when negative): a 3 m face
        /// at the crag, a shelf 16 m deep easing out over 12 m, and a 14 m ramp past each end.</summary>
        float CliffLift(float x, float z)
        {
            float best = 0;
            foreach (var (at, cos, sin, half, lift) in lifts)
            {
                float wx = x - at.x, wz = z - at.y, lx = wx * cos - wz * sin, lz = (wx * sin + wz * cos) * Mathf.Sign(lift);   // into the prop's frame, raised side +
                if (lz < -1 || lz > 28 || Mathf.Abs(lx) > half + 14) continue;
                float along = 1 - Mathf.SmoothStep(0, 1, (Mathf.Abs(lx) - half) / 14);
                float behind = Mathf.SmoothStep(0, 1, (lz + 1) / 3) * (1 - Mathf.SmoothStep(0, 1, (lz - 16) / 12));
                best = Mathf.Max(best, Mathf.Abs(lift) * along * behind);
            }
            return best;
        }
        /// <summary>How level the ground is at a point (the ground grid's normal.y, 1 on the flat).</summary>
        float UpAt(float x, float z)
        {
            if (groundUp == null) return 1;
            int seg = GroundSegments, row = seg + 1;
            float fx = Mathf.Clamp((x + Half) / Zone.size * seg, 0, seg - .001f), fz = Mathf.Clamp((z + Half) / Zone.size * seg, 0, seg - .001f);
            int ix = (int)fx, iz = (int)fz; fx -= ix; fz -= iz; int i = iz * row + ix;
            return Mathf.Lerp(Mathf.Lerp(groundUp[i], groundUp[i + 1], fx), Mathf.Lerp(groundUp[i + row], groundUp[i + row + 1], fx), fz);
        }
        /// <summary>The ground grid's normal at a point (bilinear).</summary>
        Vector3 NormalAt(float x, float z)
        {
            if (groundN == null) return Vector3.up;
            int seg = GroundSegments, row = seg + 1;
            float fx = Mathf.Clamp((x + Half) / Zone.size * seg, 0, seg - .001f), fz = Mathf.Clamp((z + Half) / Zone.size * seg, 0, seg - .001f);
            int ix = (int)fx, iz = (int)fz; fx -= ix; fz -= iz; int i = iz * row + ix;
            return Vector3.Lerp(Vector3.Lerp(groundN[i], groundN[i + 1], fx), Vector3.Lerp(groundN[i + row], groundN[i + row + 1], fx), fz).normalized;
        }
        /// <summary>The drawn ground's height at a point (its triangles). HeightAt is exact only at the grid's vertices: on a sharp
        /// crest the drawn surface sits well below it.</summary>
        float MeshY(float x, float z)
        {
            if (groundY == null) return HeightAt(x, z);
            int seg = GroundSegments, row = seg + 1;
            float fx = Mathf.Clamp((x + Half) / Zone.size * seg, 0, seg - .001f), fz = Mathf.Clamp((z + Half) / Zone.size * seg, 0, seg - .001f);
            int ix = (int)fx, iz = (int)fz; fx -= ix; fz -= iz; int i = iz * row + ix;
            float h00 = groundY[i], h10 = groundY[i + 1], h01 = groundY[i + row], h11 = groundY[i + row + 1];   // split along (x+1,z)-(x,z+1)
            return fx + fz <= 1 ? h00 + (h10 - h00) * fx + (h01 - h00) * fz : h11 + (h01 - h11) * (1 - fx) + (h10 - h11) * (1 - fz);
        }
        /// <summary>Mountain ground: how rocky a point is before slope (noise patches, more toward the high edges); 0 = turf.</summary>
        float MountainRock(float x, float z)
        {
            float edge = Mathf.InverseLerp(Half - 40, Half - 6, Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)));
            return (Mathf.PerlinNoise(x * .045f + 90, z * .045f + 17) - .45f) * 2.4f + edge * .6f;
        }
        /// <summary>Mountain ground: how bare a point is before the paint's ragged edge (under .5 turf, over it scree and rock):
        /// the rocky patches (MountainRock), steep ground (half way at 27 degrees), the scree run-out under a steep face
        /// (<paramref name="fan"/>: 1 where the ground 3 m uphill, or less so 7 m uphill, is a face) and the apron round a crag.
        /// The paint and the grass both read it. Draws nothing random.</summary>
        float MountainBare(float x, float z, out float fan)
        {
            float steep = Mathf.Clamp01((1 - UpAt(x, z) - .035f) * 7); var n = NormalAt(x, z); float len = Mathf.Sqrt(n.x * n.x + n.z * n.z); fan = 0;
            if (len > .03f)
            {
                float ux = -n.x / len, uz = -n.z / len;   // uphill
                fan = Mathf.Max(Mathf.Clamp01((1 - UpAt(x + ux * 3, z + uz * 3) - .13f) * 8), Mathf.Clamp01((1 - UpAt(x + ux * 7, z + uz * 7) - .13f) * 8) * .8f);
            }
            return MountainRock(x, z) + Mathf.Max(steep, fan * .62f) + CragApron(x, z) * 1.2f;
        }
        (Vector2 a, Vector2 b)[] cragFeet;   // each mountain cliff prop's line through the middle of its lumps
        /// <summary>Mountains: how close a point is to a crag (a cliff prop): 1 within 3.5 m of the line through its lumps, 0 from
        /// 8 m and in every other biome. The ground there is painted bare, as scree in the rock's grey, and grows no grass, so a
        /// crag stands in its own fall of rock. BuildGround's detail mask asks first, on the main thread, so the lines are built
        /// before the paint's rows ask in parallel.</summary>
        float CragApron(float x, float z)
        {
            if (cragFeet == null)
            {
                var feet = new List<(Vector2, Vector2)>();
                if (Zone.biome == "mountain")
                    foreach (var p in Zone.props)
                    {
                        if (p == null || p.kind != "cliff") continue;
                        float r = p.rotation * Mathf.Deg2Rad, s = p.scale <= 0 ? 1 : p.scale;
                        Vector2 along = new Vector2(Mathf.Cos(r), -Mathf.Sin(r)) * ((p.size.x > 0 ? p.size.x : 20) / 2 * s), mid = p.at + new Vector2(Mathf.Sin(r), Mathf.Cos(r)) * ((p.lift != 0 ? Mathf.Sign(p.lift) * .8f : 1.6f) * s);
                        feet.Add((mid - along, mid + along));
                    }
                cragFeet = feet.ToArray();
            }
            float best = 0; var q = new Vector2(x, z);
            foreach (var (a, b) in cragFeet)
            {
                if (Mathf.Abs(x - (a.x + b.x) / 2) > Mathf.Abs(a.x - b.x) / 2 + 8 || Mathf.Abs(z - (a.y + b.y) / 2) > Mathf.Abs(a.y - b.y) / 2 + 8) continue;
                best = Mathf.Max(best, 1 - Mathf.SmoothStep(0, 1, (Segment(q, a, b) - 3.5f) / 4.5f));
            }
            return best;
        }
        static readonly float[] RockBedTops = { 1f, 2.6f, 3.4f, 5.2f, 6.3f, 8f };   // the tops of the six beds in an 8 m run of strata (.8 to 1.8 m thick)
        /// <summary>
        /// Mountain ground paint under the turf at a point (texel i, j; n1, n2, n3 are the paint's three noises): scree, or bedded
        /// rock where the ground is a face (half way at 36 degrees, the edge broken by noise). <paramref name="rock"/> is how much
        /// of it shows through the turf: 0 or 1 but for a narrow ragged edge about MountainBare = .5, broken at 2 m, half a metre
        /// and 25 cm. <paramref name="hollow"/> is the turf's shade (darker in a gully, a touch lighter on a crest).
        /// Scree: the warm gravel, with pale stones, dark gaps and 25 cm grit; darker and in the rock's own grey round a crag's
        /// foot. Rock: strata keyed on the drawn ground's height, so on the top-down paint they run level round every hump and
        /// keep their thickness on any slope: beds .8 to 1.8 m thick that dip a little and wander, each with its own tone, a
        /// shadow line at its foot (faint along some stretches), a worn lit lip and a dark joint every few metres. Gullies (the ground lower than its surroundings 3 m out) are
        /// darker, crests lighter. Reads only the ground grid and the zone's data, so the paint's rows may ask in parallel.
        /// </summary>
        Color MountainGround(float x, float z, int i, int j, float n1, float n2, float n3, out float rock, out float hollow)
        {
            float y = MeshY(x, z), slope = 1 - UpAt(x, z), apron = CragApron(x, z), bare = MountainBare(x, z, out float fan);
            uint blk = (uint)((i >> 1) * 83492791) ^ (uint)((j >> 1) * 29765729); blk = (blk ^ (blk >> 13)) * 0x5bd1e995u; float speck = ((blk ^ (blk >> 15)) & 1023) / 1023f;
            rock = Mathf.Clamp01((bare + (n2 - .5f) * .5f + (n3 - .5f) * .4f + (speck - .5f) * .2f - .5f) * 4 + .5f);
            float lap = (MeshY(x + 3, z) + MeshY(x - 3, z) + MeshY(x, z + 3) + MeshY(x, z - 3)) / 4 - y;
            float gully = Mathf.Clamp01(lap * 1.5f - .08f + (n2 - .5f) * .2f), crest = Mathf.Clamp01(-lap * 1.5f - .1f);
            hollow = 1 - .16f * gully + .05f * crest;
            var grey = new Color(.25f, .238f, .222f);   // the crags' tint (Cliff) times their painted stone's mean, a little under
            float stone = Mathf.Clamp01((n3 - .6f) * 7), gap = Mathf.Clamp01((.36f - n3) * 7);
            Color scree = Color.Lerp(new Color(.288f, .262f, .22f), new Color(.346f, .312f, .262f), n2) * (1 + .16f * stone - .2f * gap + (speck - .5f) * .16f + fan * .06f);
            scree = Color.Lerp(scree, grey * (.85f + speck * .2f + stone * .12f), apron * .55f);
            float bedY = y + x * .045f - z * .03f + (Mathf.PerlinNoise(x * .09f + 51, z * .09f + 23) - .5f) * 1.8f + (n2 - .5f) * .3f;
            float run = Mathf.Floor(bedY / 8), by = bedY - run * 8; int bed = 0; while (bed < 5 && by >= RockBedTops[bed]) bed++;
            float foot = bed == 0 ? 0 : RockBedTops[bed - 1], thick = RockBedTops[bed] - foot, up = by - foot;   // up: metres above the bed's foot
            uint id = (uint)((int)run * 6 + bed + 4096) * 2654435761u; id ^= id >> 15; float tone = (id & 1023) / 1023f;
            float joint = Mathf.Abs(Mathf.PerlinNoise(x * .21f + tone * 37, z * .21f + bed * 7.3f) * 2 - 1), strong = .35f + .65f * Mathf.Clamp01(Mathf.PerlinNoise(x * .13f + tone * 91, z * .13f + bed * 3.1f) * 2.2f - .35f);
            float v = (.8f + tone * .4f) * (.93f + n1 * .14f) * (.95f + speck * .1f);
            v *= 1 - .42f * strong * (1 - Mathf.SmoothStep(0, 1, up / .32f));       // the shadow line at the bed's foot, faint along some stretches
            v *= 1 + .16f * Mathf.SmoothStep(0, 1, (up - thick + .28f) / .28f);     // its worn, lit lip
            v *= 1 - .26f * Mathf.Clamp01((.06f - joint) / .03f);                   // a joint, every few metres
            float warm = (Mathf.Repeat(tone * 3, 1) - .5f) * .08f;
            var bedded = new Color(grey.r * v * (1 + warm), grey.g * v, grey.b * v * (1 - warm * 1.2f));
            float face = Mathf.Clamp01(((slope - .1275f) * 8 + (n1 - .5f) * .5f + (n3 - .5f) * .5f - .5f) * 3 + .5f);
            return Color.Lerp(scree, bedded, face) * (1 - .26f * gully + .07f * crest) * (1 - .14f * apron);
        }

        // ---------- lighting ----------
        void BuildLighting()
        {
            var l = Zone.lighting;
            var sun = FindFirstObjectByType<Light>();
            if (sun == null || sun.type != LightType.Directional) { sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; }
            sunLight = sun;
            sun.color = ZoneColors.Parse(l.sunColor, Color.white); sun.intensity = l.sunIntensity; sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .75f; sun.transform.rotation = Quaternion.Euler(l.sunPitch, l.sunYaw, 0);
            // Render quality for the open-world look: longer, sharper shadows, MSAA, anisotropic ground, more lit lamps.
            QualitySettings.shadowDistance = 110; QualitySettings.shadowCascades = 4; QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadows = ShadowQuality.All; QualitySettings.antiAliasing = 4; QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.pixelLightCount = 8; sun.shadowBias = .04f; sun.shadowNormalBias = .3f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ZoneColors.Parse(l.ambientSky, Color.grey);
            RenderSettings.ambientEquatorColor = ZoneColors.Parse(l.ambientEquator, Color.grey);
            RenderSettings.ambientGroundColor = ZoneColors.Parse(l.ambientGround, Color.black);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = ZoneColors.Parse(l.fogColor, Color.grey); RenderSettings.fogStartDistance = l.fogStart; RenderSettings.fogEndDistance = l.fogEnd;
            if (art.skybox != null) { RenderSettings.skybox = art.skybox; RenderSettings.sun = sun; }
            var cam = Camera.main;
            if (cam != null) { cam.clearFlags = art.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor; cam.depthTextureMode |= DepthTextureMode.Depth; cam.farClipPlane = Mathf.Max(cam.farClipPlane, (Half + BackdropWidth) * 2.75f + Half * 1.45f + 40); }   // far enough for the distant ranges (DistantRanges) from a corner
        }

        // ---------- ground ----------
        void BuildGround()
        {
            var go = new GameObject("Ground"); go.transform.SetParent(transform, false); go.layer = 0;
            var mesh = ZoneMeshes.Ground(Zone.size, GroundSegments, HeightAt, p => Hollow.InsideAny(p, .3f));   // no land inside a cave's passage
            var normals = mesh.normals; groundUp = new float[normals.Length]; for (int i = 0; i < normals.Length; i++) groundUp[i] = normals[i].y;
            var verts = mesh.vertices; groundN = normals; groundY = new float[verts.Length]; for (int i = 0; i < verts.Length; i++) groundY[i] = verts[i].y;
            GroundMesh = go.AddComponent<MeshFilter>(); GroundMesh.sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); var m = new Material(art.ground) { name = "Painted ground" };
            m.SetTextureScale("_DetailAlbedoMap", Vector2.one * Zone.size / 1.8f);   // ~1.8 m grain repeat, whatever the zone size
            if (Zone.biome == "ash")
            {
                m.SetTexture("_DetailAlbedoMap", AshDetail(Zone.seed)); m.SetTextureScale("_DetailAlbedoMap", Vector2.one * Zone.size / 5);   // powdery grain and a few hairlines, sharp up close
                m.SetTexture("_DetailMask", DetailMask(256, (x, z) => (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, .95f, Unmade(x, z)))) * (1 - .75f * Licked(x, z))));   // none on the unmade, little on licked ground
            }
            if (Zone.biome == "mountain") m.SetTexture("_DetailMask", DetailMask(256, (x, z) => Mathf.Lerp(.5f, .9f, Mathf.Clamp01(MountainBare(x, z, out _) * 2 - .5f))));   // half grain on the turf (in hard alpine light it was a harsh speckle), nearly full on rock and scree
            m.mainTexture = PaintGround(Mathf.Clamp(Mathf.RoundToInt(Zone.size * 8 / 256) * 256, 1024, 4096)); r.sharedMaterial = m; GroundMaterial = m;   // paint: about 8 px a metre (a 560 m zone gets 7.3 at the 4096 cap)
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
        Texture2D PaintGround(int res)
        {
            var tex = new Texture2D(res, res, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4, name = "Ground paint" };
            var px = new Color32[res * res];
            Color grassA = new Color(.30f, .38f, .20f), grassB = new Color(.42f, .43f, .22f), grassC = new Color(.47f, .38f, .20f);
            Color dirt = new Color(.46f, .38f, .27f), rut = new Color(.34f, .28f, .20f), mud = new Color(.28f, .25f, .19f);
            Color soil = new Color(.30f, .23f, .16f), furrow = new Color(.20f, .15f, .11f), stubble = new Color(.62f, .53f, .30f);
            // Unmade grey, cooled against the sun's tint (half of it), so a warm sun lights it grey, not beige sand.
            var sunC = ZoneColors.Parse(Zone.lighting.sunColor, Color.white); sunC /= Mathf.Max(.05f, sunC.grayscale);
            Color unmade = new Color(.47f / Mathf.Lerp(1, sunC.r, .3f), .47f / Mathf.Lerp(1, sunC.g, .3f), .49f / Mathf.Lerp(1, sunC.b, .3f));
            bool gloom = Gloom;
            // Gloom: trodden roads and yards are dark, near-grey brown, about half the earth's value and less saturated, so they
            // read as ways against the rose ground under Khaven's orange sun and rose grade (a hue nudge alone left them lost in it).
            // Green is held level with red so the rose light doesn't turn them mauve.
            if (gloom) { dirt = new Color(.235f, .23f, .205f); rut = new Color(.175f, .172f, .155f); }
            if (Zone.biome == "mountain") { dirt = new Color(.4f, .35f, .28f); rut = new Color(.31f, .27f, .21f); }   // yards and roads a shade darker in the hard light
            if (Zone.biome == "meadow") { grassA = new Color(.25f, .48f, .17f); grassB = new Color(.37f, .58f, .21f); grassC = new Color(.50f, .44f, .20f); dirt = new Color(.49f, .39f, .25f); rut = new Color(.36f, .28f, .19f); }   // Oakhaven: fresh pasture green with late-summer gold patches; warm trodden earth
            bool verdant = Zone.biome == "verdant";
            if (verdant)
            {
                // The Verdant Shore: deep saturated greens, from obsidian moss to bright emerald turf (canon: "a thousand shades" of green),
                // over dark loam; the paths are damp brown earth.
                grassA = new Color(.13f, .33f, .14f); grassB = new Color(.24f, .46f, .17f); grassC = new Color(.36f, .55f, .2f);
                dirt = new Color(.33f, .25f, .16f); rut = new Color(.24f, .18f, .12f); mud = new Color(.2f, .16f, .11f);
            }
            var toSun = Quaternion.Euler(55, Zone.lighting.sunYaw, 0) * Vector3.back;   // the day sun, roughly (mountain counter-shading)
            float size = Zone.size, half = size / 2, texel = size / res;
            // Ash crust: petrified-ash plates, a Voronoi network with one jittered site per 5 m cell (sx/sz in cell units, three
            // cells of margin), plus each site's 8 bisectors with its neighbours (unit normal bx/bz, offset bo, neighbour bn), so
            // a pixel's distance to its nearest plate edge is 8 dot products. The ash branch below decides which edges show.
            bool ashen = Zone.biome == "ash"; const float plate = 5; int cells = Mathf.CeilToInt(size / plate) + 7;
            var sx = new float[ashen ? cells * cells : 0]; var sz = new float[sx.Length]; var crust = new System.Random(Zone.seed + 77);
            for (int k = 0; k < sx.Length; k++) { sx[k] = k % cells + .15f + .7f * (float)crust.NextDouble(); sz[k] = k / cells + .15f + .7f * (float)crust.NextDouble(); }
            var bx = new float[sx.Length * 8]; var bz = new float[bx.Length]; var bo = new float[bx.Length]; var bn = new int[bx.Length];
            for (int k = 0, m = 0; k < sx.Length; k++)
                for (int e = 0; e < 9; e++)
                {
                    if (e == 4) continue; int a = k % cells + e % 3 - 1, b = k / cells + e / 3 - 1, o = b * cells + a;
                    if (a < 0 || b < 0 || a >= cells || b >= cells) { bo[m++] = 99; continue; }   // off the grid: never the nearest
                    float ax = sx[o] - sx[k], az = sz[o] - sz[k], len = Mathf.Sqrt(ax * ax + az * az);
                    bx[m] = ax / len; bz[m] = az / len; bo[m] = ((sx[o] + sx[k]) * ax + (sz[o] + sz[k]) * az) * .5f / len; bn[m++] = o;
                }
            // Bounding boxes let most pixels skip the per-segment distance tests (the expensive part at 2048x2048).
            var roadBox = Zone.roads.Select(r => Box(r.points, r.width / 2 + 1.3f)).ToArray();
            // Rows in parallel: every pixel reads only the zone's data and the ground grid (no random stream, no Unity objects), and
            // writes only its own texel. A 380 m zone's 3072 px paint took 20 s on one core.
            System.Threading.Tasks.Parallel.For(0, res, j => {
                for (int i = 0; i < res; i++)
                {
                    float x = -half + size * (i + .5f) / res, z = -half + size * (j + .5f) / res; var p = new Vector2(x, z);
                    float n1 = Mathf.PerlinNoise(x * .08f + 11, z * .08f + 7), n2 = Mathf.PerlinNoise(x * .45f, z * .45f), n3 = Mathf.PerlinNoise(x * 1.9f + 3, z * 1.9f + 5);
                    Color c = Color.Lerp(Color.Lerp(grassA, grassB, n1), grassC, Mathf.Clamp01((Mathf.PerlinNoise(x * .03f + 40, z * .03f) - .55f) * 3));
                    c *= .88f + n2 * .18f + (n3 - .5f) * .08f;
                    if (verdant)
                    {
                        // Moss in the hollows and shade (a darker, bluer green, in patches), and the brightest emerald where the light falls.
                        float mossy = Mathf.Clamp01((Mathf.PerlinNoise(x * .06f + 21, z * .06f + 9) - .5f) * 3), sunlit = Mathf.Clamp01((Mathf.PerlinNoise(x * .02f + 77, z * .02f + 5) - .6f) * 4);
                        c = Color.Lerp(c, new Color(.09f, .24f, .13f) * (.9f + n3 * .2f), mossy * .7f);
                        c = Color.Lerp(c, new Color(.4f, .62f, .2f), sunlit * .5f);
                    }
                    if (Zone.biome == "mountain")
                    {
                        // Alpine: thin dry turf on the gentler ground only, ending on a ragged edge; warm scree on the rocky flats,
                        // under the faces and round the crags' feet; bedded grey rock on the steep ground (crags, scarps, gorge
                        // walls) and the high edges (MountainGround). Mid-dark values, so it never reads as snow. Rock and scree are
                        // counter-shaded toward the day sun (faces turned to it a little darker, faces turned away lifted), so a
                        // sunlit crag stays well under the fog's value and the relief reads as a gradient, not near-white against
                        // near-black.
                        var nrm = NormalAt(x, z); float turn = nrm.x * toSun.x + nrm.z * toSun.z, shade = 1 - Mathf.Clamp(turn, -.6f, .6f) * (turn < 0 ? .5f : .22f);   // horizontal turn toward the midday sun (55 deg, an average)
                        Color rockC = MountainGround(x, z, i, j, n1, n2, n3, out float rock, out float hollow) * shade;
                        Color alp = Color.Lerp(new Color(.26f, .41f, .18f), new Color(.38f, .46f, .21f), n1);   // a step toward the alpine tufts' green, so they sit in turf
                        c = Color.Lerp(alp * hollow, rockC, rock) * (.91f + n3 * .12f);
                    }
                    else if (ashen)
                    {
                        // Ashland: a crust of petrified ash in plates of uneven size, soft pale ash drifted over it in long tongues
                        // and lying in its joints, scorched ground showing dark between, and the odd copper-rust stain (canon: the
                        // dust tastes of copper). The plates are Voronoi cells in a warped field (so their sizes differ); six edges in ten are left
                        // out, so cells join into larger plates whose tones run into each other. The rest are joints of pale ash, a
                        // few with a dark crack along them, and both fade in and out along their run: broken runs, never a net.
                        // Slopes shed the drift. Licked ground (the Unwoven Flats) has neither plate, joint nor crack. Values: drifts
                        // pale, plates mid, scorched ground and cracks dark, and the whole a shade lighter or darker across 80 m.
                        float u = (x + half) / plate + 3 + (Mathf.PerlinNoise(x * .05f + 91, z * .05f + 13) - .5f) * .7f + (n2 - .5f) * .1f + (n3 - .5f) * .03f;
                        float v = (z + half) / plate + 3 + (Mathf.PerlinNoise(x * .05f + 37, z * .05f + 59) - .5f) * .7f + (Mathf.PerlinNoise(x * .45f + 31, z * .45f + 17) - .5f) * .1f + (n3 - .5f) * .03f;
                        int cx = (int)u, cy = (int)v, near = 0, other = 0; float best = 99, edge = 99;
                        for (int b = cy - 1; b <= cy + 1; b++) for (int a = cx - 1; a <= cx + 1; a++)
                        { int k = b * cells + a; float dx = sx[k] - u, dz = sz[k] - v; if (dx * dx + dz * dz < best) { best = dx * dx + dz * dz; near = k; } }
                        for (int m = near * 8; m < near * 8 + 8; m++) { float e = bo[m] - u * bx[m] - v * bz[m]; if (e < edge) { edge = e; other = bn[m]; } }
                        uint pair = (uint)(Mathf.Min(near, other) * 7919 + Mathf.Max(near, other)) * 2654435761u;
                        int joint = (int)(pair >> 24);   // under 150: no joint (the two cells are one plate); under 195: a joint of drifted ash; above: a crack as well
                        float d = edge * plate, lick = Licked(x, z), steep = Mathf.Clamp01((1 - UpAt(x, z) - .02f) * 9);
                        // A plate's tone runs into its neighbour's across the edge, so joined cells shade into each other softly.
                        float tone = Mathf.Lerp(Mathf.Repeat(sx[near] * 7.31f + sz[near] * 3.17f, 1), Mathf.Repeat(sx[other] * 7.31f + sz[other] * 3.17f, 1), .5f * (1 - Mathf.SmoothStep(0, 1, d / .6f)));
                        // Drifts lie in long soft tongues, all drawn out the same way (la along them, lb across), feathered at the rim.
                        float la = x * .8f + z * .6f, lb = z * .8f - x * .6f;
                        float drift = Mathf.SmoothStep(0, 1, (Mathf.PerlinNoise(la * .03f + 140, lb * .085f + 71) * .7f + Mathf.PerlinNoise(la * .09f + 19, lb * .22f + 47) * .22f + n2 * .08f - .47f) * 3.4f) * (1 - steep);
                        float scorch = Mathf.SmoothStep(0, 1, (Mathf.PerlinNoise(x * .028f + 210, z * .028f + 33) + (n2 - .5f) * .1f - .56f) * 3.2f);
                        float seam = joint < 150 ? 0 : (1 - Mathf.SmoothStep(0, 1, d / (.55f + .4f * n1))) * Mathf.Clamp01((Mathf.PerlinNoise(x * .19f + 9, z * .19f + 77) - .3f) * 2.4f);
                        float runs = Mathf.Clamp01((Mathf.PerlinNoise(x * .035f + 7, z * .035f + 83) - .4f) * 4) * Mathf.Clamp01((Mathf.PerlinNoise(x * .23f + 51, z * .23f + 5) - .32f) * 2.6f);
                        float line = joint < 195 ? 0 : Mathf.Clamp01((.05f + .05f * n1 - d) / texel + .5f) * runs * (1 - drift);
                        Color crustC = Color.Lerp(new Color(.36f, .36f, .375f), new Color(.5f, .495f, .5f), Mathf.Clamp01(n1 * .45f + tone * .55f));
                        crustC = Color.Lerp(crustC, new Color(.52f, .44f, .40f), Mathf.Clamp01((Mathf.PerlinNoise(x * .02f + 60, z * .02f) - .66f) * 2.2f));
                        crustC = Color.Lerp(crustC, Color.Lerp(new Color(.25f, .245f, .245f), new Color(.31f, .3f, .295f), n2), scorch * .75f);
                        crustC = Color.Lerp(crustC, new Color(.15f, .145f, .145f), line * .34f);
                        Color driftC = Color.Lerp(new Color(.57f, .565f, .57f), new Color(.66f, .655f, .645f), n2);
                        c = Color.Lerp(crustC, driftC, Mathf.Max(drift, seam * .36f) * (1 - .5f * scorch));
                        // Licked: one smooth cold grey with faint long streaks, a darker lip where the crust breaks off around it.
                        if (lick > 0) c = Color.Lerp(c, Color.Lerp(new Color(.52f, .52f, .535f), new Color(.565f, .565f, .58f), Mathf.PerlinNoise(x * .05f + 41, z * .5f + 3)), lick) * (1 - .1f * Mathf.Clamp01(1 - Mathf.Abs(lick - .5f) * 2.5f));
                        c *= (.9f + .2f * Mathf.PerlinNoise(x * .012f + 300, z * .012f + 17)) * (.95f + n3 * .08f);
                    }
                    if (gloom)
                    {
                        // Gloom (Khaven): hard grey-brown earth under thin dead grass, darker in the damp hollows. Nothing green.
                        Color earth = Color.Lerp(new Color(.37f, .34f, .31f), new Color(.46f, .43f, .38f), n1), sere = Color.Lerp(new Color(.44f, .41f, .32f), new Color(.5f, .46f, .35f), n3);
                        c = Color.Lerp(earth, sere, Mathf.Clamp01((Mathf.PerlinNoise(x * .05f + 70, z * .05f + 33) - .35f) * 2.2f));
                        c = Color.Lerp(c, new Color(.27f, .25f, .24f), Mathf.Clamp01((Mathf.PerlinNoise(x * .025f + 5, z * .025f + 81) - .64f) * 3) * .7f) * (.9f + n2 * .15f);
                    }
                    // Fields: tilled furrows or golden stubble, following each field's rotation.
                    foreach (var f in Zone.fields)
                    {
                        var local = Rotate(p - f.center, -f.rotation);
                        float ex = f.size.x / 2 - Mathf.Abs(local.x), ez = f.size.y / 2 - Mathf.Abs(local.y);
                        if (ex <= -1 || ez <= -1) continue;
                        float edge = Mathf.Clamp01(Mathf.Min(ex, ez) + 1) / 2;
                        bool rows = Mathf.Repeat(local.x, 1.6f) < .55f;
                        Color fc = f.crop == "stubble" ? Color.Lerp(stubble, soil, rows ? .35f : 0) : rows ? furrow : soil;
                        c = Color.Lerp(c, fc * (.9f + n2 * .2f), edge);
                    }
                    foreach (var cl in Zone.clearings)
                    {
                        float d = Vector2.Distance(p, cl.center);
                        if (d < cl.radius + 2) c = Color.Lerp(c, Color.Lerp(dirt, rut, n3 * .6f), Mathf.Clamp01((cl.radius + 2 - d) / 3) * (.75f + n2 * .25f));
                    }
                    foreach (var g in Zone.groves)
                    {
                        float ex = g.size.x / 2 - Mathf.Abs(x - g.center.x), ez = g.size.y / 2 - Mathf.Abs(z - g.center.y);
                        if (ex <= -4 || ez <= -4) continue;
                        float edge = Mathf.Clamp01((Mathf.Min(ex, ez) + 4) / 8) * (.7f + n2 * .3f);
                        Color litter = g.kind == "dead" ? (gloom ? Color.Lerp(new Color(.33f, .32f, .31f), new Color(.4f, .38f, .36f), n3) : Color.Lerp(new Color(.29f, .26f, .21f), new Color(.36f, .31f, .23f), n3))
                            : g.kind == "pine" ? Color.Lerp(new Color(.20f, .24f, .15f), new Color(.30f, .25f, .17f), n3)
                            : Color.Lerp(new Color(.38f, .32f, .17f), new Color(.46f, .30f, .15f), n3);
                        c = Color.Lerp(c, gloom && g.kind != "dead" ? Wither(litter) : litter, edge);   // gloom: needles and leaves withered too
                    }
                    // Landmark ground (ZoneShape paint): a wallow's dark wet mud, a brood's grey unmade floor veined with pale thread.
                    if (Zone.shapes != null)
                        foreach (var s in Zone.shapes)
                        {
                            if (s == null || string.IsNullOrEmpty(s.paint)) continue;
                            float cover = ShapeCover(s, p); if (cover <= 0) continue;
                            Color sc;
                            if (s.paint == "unmade") { float vein = Mathf.Abs(Mathf.PerlinNoise(x * .7f + 13, z * .7f + 29) * 2 - 1); sc = Color.Lerp(unmade * (.8f + n3 * .35f), new Color(.76f, .76f, .8f), Mathf.Clamp01((.07f - vein) * 14)); }
                            else if (s.paint == "salt")
                            {
                                // Salt-flats: a grey-white crust cracked into plates, darker wet slush in the low spots.
                                float wet = Mathf.Clamp01((Mathf.PerlinNoise(x * .04f + 17, z * .04f + 52) - .52f) * 4), plateLine = Mathf.Abs(Mathf.PerlinNoise(x * .32f + 3, z * .32f + 9) * 2 - 1);
                                sc = Color.Lerp(new Color(.8f, .8f, .78f), new Color(.86f, .87f, .85f), n2) * (.95f + n3 * .1f);
                                sc = Color.Lerp(sc, new Color(.55f, .57f, .58f), wet * .8f);
                                if (plateLine < .06f) sc *= .85f;
                            }
                            else sc = Color.Lerp(new Color(.25f, .2f, .15f), new Color(.12f, .095f, .075f), Mathf.Clamp01((n1 - .4f) * 2.5f + (n3 - .5f) * .4f));   // mud
                            c = Color.Lerp(c, sc, Mathf.Clamp01(cover * (.85f + n2 * .3f)));
                        }
                    // A cave's floor: bare trodden earth and grit, a little lighter where it's worn, under its walls.
                    foreach (var hol in Hollow.All)
                    {
                        float cover = hol.Cover(p, 1.4f); if (cover <= 0) continue;
                        var earth = Color.Lerp(new Color(.31f, .27f, .22f), new Color(.19f, .165f, .14f), Mathf.Clamp01((n1 - .35f) * 2 + (n3 - .5f) * .5f));
                        c = Color.Lerp(c, earth, cover);
                    }
                    for (int k = 0; k < Zone.roads.Length; k++)
                    {
                        var road = Zone.roads[k]; if (!roadBox[k].Contains(p)) continue;
                        float d = DistanceToPath(p, road.points, out bool interior), hw = road.width / 2;
                        if (d > hw + 1.2f) continue;
                        // Wheel ruts run along the road but never loop around its open ends.
                        float wheel = interior && Mathf.Abs(d - hw * .45f) < .35f ? .55f : 0;
                        Color rc = Color.Lerp(Color.Lerp(dirt, rut, wheel + n3 * .3f), mud, n2 * .2f);
                        c = Color.Lerp(c, rc, Mathf.Clamp01((hw + 1.2f - d) / 1.6f + (n2 - .5f) * .6f));
                    }
                    // The water's edge, creeks and lakes alike, along the real (wandering) waterline: a pale sandy bed that shows
                    // through the clear turquoise shallows, wet sand and pebbles at the water, a narrow earthen bank, then grass.
                    // Khaven's murky creek keeps its dark silt. Noise breaks up the bank's outer edge; the grass stops at the
                    // same line (Openness).
                    float shore = Water.Shore(p, 3) + (n2 - .5f) * 1.1f;
                    if (shore < 2.2f)
                    {
                        Color wet = gloom ? new Color(.22f, .2f, .15f) : new Color(.6f, .54f, .4f), bed = gloom ? new Color(.16f, .15f, .12f) : new Color(.54f, .49f, .36f);
                        c = Color.Lerp(c, Color.Lerp(mud, dirt, n1 * .4f), Mathf.Clamp01((2.2f - shore) / 1.5f));
                        if (shore < .6f) c = Color.Lerp(c, wet, Mathf.Clamp01((.6f - shore) / .6f) * .6f);
                        if (Mathf.Abs(shore) < .5f && n3 > .5f) c = Color.Lerp(c, new Color(.66f, .62f, .54f), .45f);
                        if (shore < -.5f) c = Color.Lerp(c, Color.Lerp(bed, bed * .8f, n1 * .5f), Mathf.Clamp01((-.5f - shore) / 3));
                    }
                    // Ash: roads, yards and grove litter are trodden ash too, keeping only a trace of their earth colour.
                    if (ashen) { float l = c.grayscale; c = Color.Lerp(new Color(l, l, l), c, .4f); }
                    float gone = Unmade(x, z);
                    if (gone > 0)
                    {
                        // The colour drains first (grey grass, grey dirt), then the land's own features go too: roads, cracks and
                        // shading are erased into flat grey with a fine fizz of static. Not burnt, not rotted: unmade.
                        // Static grain: per texel plus 2x2-texel (25 cm) specks that still show a few metres off; hardly any
                        // broad shading, which read as soft sand dunes.
                        uint hsh = (uint)(i * 73856093) ^ (uint)(j * 19349663); hsh = (hsh ^ (hsh >> 13)) * 0x5bd1e995u;
                        uint blk = (uint)((i >> 1) * 83492791) ^ (uint)((j >> 1) * 29765729); blk = (blk ^ (blk >> 13)) * 0x5bd1e995u;
                        float grey = c.grayscale, fizz = ((hsh ^ (hsh >> 15)) & 1023) / 1023f, speck = ((blk ^ (blk >> 15)) & 1023) / 1023f;
                        var drained = Color.Lerp(c, new Color(grey, grey, grey * 1.02f), Mathf.Clamp01(gone * 1.8f));
                        var stat = unmade * (.9f + (fizz - .5f) * .2f + (speck - .5f) * .26f + (n1 - .5f) * .04f);
                        c = Color.Lerp(drained, stat, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, .95f, gone)));
                    }
                    px[j * res + i] = c;
                }
            });
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }
        /// <summary>
        /// Ash ground detail (x2 over the paint, 5 m repeat): powdery grain in four sizes (3 cm to 1 m, tiling noise) and a few
        /// hairline crazes, short runs along the edges of a tileable Voronoi of 8x8 cells that fade out along their length.
        /// No closed plates: at this size they read as paving. Averages mid-grey. Sharp up close, where the paint blurs.
        /// </summary>
        static Texture2D AshDetail(int seed)
        {
            const int n = 512, cells = 8; float per = (float)n / cells; var rnd = new System.Random(seed + 31);
            var sx = new float[cells * cells]; var sz = new float[sx.Length];
            for (int k = 0; k < sx.Length; k++) { sx[k] = .15f + .7f * (float)rnd.NextDouble(); sz[k] = .15f + .7f * (float)rnd.NextDouble(); }
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // Neighbour sites wrap around the tile (so it repeats seamlessly) but sit at their true, unwrapped offset.
                    float u = (x + .5f) / per, v = (y + .5f) / per, nx = 0, nz = 0, best = 99, edge = 99; int cx = (int)u, cy = (int)v, near = 0, other = 0;
                    for (int b = -1; b <= 1; b++) for (int a = -1; a <= 1; a++)
                    {
                        int k = (cy + b + cells) % cells * cells + (cx + a + cells) % cells; float ux = cx + a + sx[k], uz = cy + b + sz[k];
                        float d = (ux - u) * (ux - u) + (uz - v) * (uz - v); if (d < best) { best = d; nx = ux; nz = uz; near = k; }
                    }
                    for (int b = -2; b <= 2; b++) for (int a = -2; a <= 2; a++)
                    {
                        int k = (cy + b + cells) % cells * cells + (cx + a + cells) % cells; float ux = cx + a + sx[k] - nx, uz = cy + b + sz[k] - nz, len = Mathf.Sqrt(ux * ux + uz * uz);
                        float e = len > .001f ? ((nx + ux * .5f - u) * ux + (nz + uz * .5f - v) * uz) / len : 99; if (e < edge) { edge = e; other = k; }
                    }
                    uint h = (uint)x * 374761393u + (uint)y * 668265263u, pair = (uint)(Mathf.Min(near, other) * 97 + Mathf.Max(near, other)) * 2654435761u;
                    h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
                    float s = (x + .5f) / n, t = (y + .5f) / n; int craze = (int)(pair >> 24);
                    float g = .51f + ((h & 1023) / 1023f - .5f) * .05f + (TileNoise(s * 160, t * 160, 160, 1) - .5f) * .09f + (TileNoise(s * 48, t * 48, 48, 2) - .5f) * .1f
                        + (TileNoise(s * 14, t * 14, 14, 3) - .5f) * .1f + (TileNoise(s * 5, t * 5, 5, 4) - .5f) * .06f;
                    // One edge in four is a hairline (~1.5 cm), and only where the run noise lets it show: it fades out along its length.
                    g = Mathf.Lerp(g, .36f, Mathf.Clamp01(1.3f - edge * per) * (craze < 190 ? 0 : craze < 232 ? .25f : .5f) * Mathf.Clamp01((TileNoise(s * 6, t * 6, 6, 5) - .45f) * 4));
                    byte c = (byte)(Mathf.Clamp01(g) * 255); px[y * n + x] = new Color32(c, c, c, 255);
                }
            var tex = new Texture2D(n, n, TextureFormat.RGB24, true) { name = "Ash grain", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            tex.SetPixels32(px); tex.Apply(true, true); return tex;
        }
        /// <summary>Smooth value noise, 0..1, that repeats every <paramref name="period"/> lattice cells (for textures that tile).</summary>
        static float TileNoise(float u, float v, int period, uint salt)
        {
            int x0 = Mathf.FloorToInt(u), y0 = Mathf.FloorToInt(v); float tx = u - x0, ty = v - y0; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            float At(int a, int b)
            {
                uint h = (uint)((a % period + period) % period) * 374761393u + (uint)((b % period + period) % period) * 668265263u + salt * 2246822519u;
                h = (h ^ (h >> 13)) * 1274126177u; return ((h ^ (h >> 16)) & 1023) / 1023f;
            }
            return Mathf.Lerp(Mathf.Lerp(At(x0, y0), At(x0 + 1, y0), tx), Mathf.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), tx), ty);
        }
        /// <summary>
        /// How far the ground at a point is licked smooth (0..1): inside a landmark whose ground is "licked" (the Unwoven Flats),
        /// right across its inner two thirds and gone by its radius, with a wandering rim. Paint only: the land's shape, the
        /// colliders and the navmesh do not ask.
        /// </summary>
        float Licked(float x, float z)
        {
            var places = lickedPlaces ?? (lickedPlaces = Zone.landmarks.Where(l => l != null && l.ground == "licked").ToArray());   // PaintGround's rows ask in parallel: built once, read only
            float best = 0;
            foreach (var l in places)
            {
                float reach = Mathf.Max(1, l.radius), dx = x - l.at.x, dz = z - l.at.y; if (dx * dx + dz * dz > reach * reach * 1.7f) continue;
                float d = Mathf.Sqrt(dx * dx + dz * dz) + (Mathf.PerlinNoise(x * .09f + 5, z * .09f + 61) - .5f) * reach * .3f;
                best = Mathf.Max(best, 1 - Mathf.SmoothStep(0, 1, (d - reach * .68f) / (reach * .3f)));
            }
            return best;
        }
        ZoneLabel[] lickedPlaces;
        /// <summary>Detail strength over the zone for the Standard shader's _DetailMask (alpha, sampled with the paint's UVs).</summary>
        Texture2D DetailMask(int res, Func<float, float, float> strength)
        {
            var px = new Color32[res * res];
            for (int j = 0; j < res; j++)
                for (int i = 0; i < res; i++)
                    px[j * res + i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(strength(-Half + Zone.size * (i + .5f) / res, -Half + Zone.size * (j + .5f) / res)) * 255));
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { name = "Detail mask", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px); tex.Apply(false, true); return tex;
        }
        static Rect Box(Vector2[] pts, float pad)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var p in pts) { x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y); }
            return Rect.MinMaxRect(x0 - pad, y0 - pad, x1 + pad, y1 + pad);
        }
        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        // ---------- water ----------
        /// <summary>The zone's water: levels, carving, surfaces and queries (one source of truth).</summary>
        public ZoneWater Water { get; private set; }
        Material WaterMaterial()
        {
            var baseMat = art.waterSurface != null ? art.waterSurface : art.water;
            if (string.IsNullOrEmpty(Zone.waterTint) && Zone.waterReflect <= 0) return baseMat;
            var m = new Material(baseMat);
            if (!string.IsNullOrEmpty(Zone.waterTint))
            {
                if (m.HasProperty("_DeepColor"))
                {
                    // Murky water (Gloom Creek): the whole depth ramp becomes the zone's tint, only a little lighter toward the
                    // banks, and the foam a faint, dull scum line instead of clean white.
                    var tint = ZoneColors.Parse(Zone.waterTint, m.GetColor("_DeepColor")); tint.a = 1;
                    m.SetColor("_DeepColor", tint); m.SetColor("_MidColor", Color.Lerp(tint, Color.grey, .12f)); m.SetColor("_ShallowColor", Color.Lerp(tint, Color.grey, .3f));
                    var scum = Color.Lerp(tint, new Color(.62f, .62f, .58f), .65f); scum.a = .4f; m.SetColor("_FoamColor", scum);
                }
                else { var tint = ZoneColors.Parse(Zone.waterTint, baseMat.color); tint.a = baseMat.color.a; m.color = tint; }
            }
            // waterReflect (0..1): how much sky murky water shows at a slant (_SkyTint). Murky water also hides its bed within a
            // metre or so, glints and ripples less (big ripples on dark water read as tar) and washes up less foam.
            if (Zone.waterReflect > 0 && m.HasProperty("_SkyTint"))
            {
                float r = Mathf.Clamp01(Zone.waterReflect); m.SetFloat("_SkyTint", r); m.SetFloat("_Glare", m.GetFloat("_Glare") * (.6f + .4f * r));
                m.SetFloat("_Sparkle", m.GetFloat("_Sparkle") * r); m.SetFloat("_FoamWaves", m.GetFloat("_FoamWaves") * .4f);
                m.SetFloat("_Murk", Mathf.Lerp(3.2f, 1.2f, r)); m.SetFloat("_Bump", m.GetFloat("_Bump") * .55f);
            }
            return m;
        }
        /// <summary>Water at a point: its surface height and how deep it is over the ground there. False on dry land.</summary>
        public bool WaterAt(Vector2 p, out float surface, out float depth) { surface = depth = 0; return Water != null && Water.At(p, HeightAt(p.x, p.y), out surface, out depth); }
        /// <summary>Creek current at a point (downstream, 1 mid-channel fading to 0 at the banks); zero on lakes and land.</summary>
        public Vector2 FlowAt(Vector2 p) { return Water != null ? Water.FlowAt(p) : Vector2.zero; }
        void BuildWater()
        {
            var waterMat = WaterMaterial();
            Func<float, float, float> ground = (x, z) => HeightAt(x, z);
            foreach (var c in Water.Creeks)
                foreach (var mesh in Water.CreekMeshes(c, ground))
                {
                    var go = new GameObject(c.def.name ?? "Creek"); go.transform.SetParent(transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = waterMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                    if (art.waterSurface == null) go.AddComponent<WaterDrift>();
                }
            foreach (var k in Water.Lakes)
            {
                var go = new GameObject(k.def.name ?? "Lake"); go.transform.SetParent(transform, false); go.transform.position = new Vector3(k.def.center.x, 0, k.def.center.y);
                go.AddComponent<MeshFilter>().sharedMesh = Water.LakeMesh(k, ground);
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = waterMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            }
        }

        // ---------- the Wasting ----------
        /// <summary>
        /// How far the Wasting has eaten the ground at a point: 0 living, 1 unmade (from its line east). The front wanders and
        /// frays across the fade band (grey tongues reaching in, living spurs holding out) but is pinned to the band's ends.
        /// </summary>
        float Unmade(float x, float z)
        {
            var w = Zone.wasting; if (w == null) return 0;
            float t = (x - (w.x - w.fade)) / Mathf.Max(1, w.fade); if (t <= 0) return 0; if (t >= 1) return 1;
            float fray = (Mathf.PerlinNoise(z * .045f + 3.1f, 7.7f) - .5f) * 1.1f + (Mathf.PerlinNoise(x * .21f + 40, z * .21f) - .5f) * .5f;
            return Mathf.Clamp01(t + fray * 4 * t * (1 - t));
        }
        /// <summary>A sheet of haze through rows of points (each row the same length): vertex alpha from <paramref name="alpha"/>,
        /// u 0..1 along the rows, v 0..1 across them. Drawn with a particle shader (Cull Off), so both faces show.</summary>
        static Mesh HazeMesh(string name, Vector3[][] rows, Func<Vector3, float> alpha)
        {
            int n = rows[0].Length; var v = new List<Vector3>(); var col = new List<Color>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int r = 0; r < rows.Length; r++)
                for (int k = 0; k < n; k++) { v.Add(rows[r][k]); col.Add(new Color(1, 1, 1, Mathf.Clamp01(alpha(rows[r][k])))); uv.Add(new Vector2(k / (n - 1f), r / (rows.Length - 1f))); }
            for (int r = 0; r + 1 < rows.Length; r++)
                for (int k = 0; k + 1 < n; k++) { int a = r * n + k, b = a + n; t.AddRange(new[] { a, b, b + 1, a, b + 1, a + 1 }); }
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetColors(col); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        }
        /// <summary>n + 1 evenly spaced points from at(0) to at(1).</summary>
        static Vector3[] HazeRow(int n, Func<float, Vector3> at) { var row = new Vector3[n + 1]; for (int k = 0; k <= n; k++) row[k] = at(k / (float)n); return row; }
        void BuildWasting()
        {
            var w = Zone.wasting; if (w == null) return;
            var root = new GameObject("The Wasting").transform; root.SetParent(transform, false);
            // Haze over the unmade: unlit sheets in the fog's own colour hour by hour (WastingStatic), so the unmade greys into haze
            // rather than sitting behind a lit, glassy pane, and never ends in a ruled line against a brighter sky. Queues: the far
            // bank, then the water (Transparent-10), then the haze over the unmade, then the curtain.
            void Haze(Mesh mesh, int queue, bool fizz, float grey, float gain)
            {
                var go = new GameObject(mesh.name); go.transform.SetParent(root, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                r.sharedMaterial = new Material(art.particle) { name = mesh.name, renderQueue = queue };
                go.AddComponent<WastingStatic>().Init(fizz, grey, gain);
            }
            if (art.particle != null)
            {
                float edge = Half + BackdropWidth, x0 = w.x + 2, x1 = edge - 12, span = Zone.size * 1.3f;
                // The static curtain: crawling grey static, thin at the ground and thickest a few metres up, so the land behind it
                // greys with distance instead of behind a ruled bottom edge. It runs far past the zone's north and south edges and
                // fades out over all of that run: a short fade at its ends showed as a hard seam in the sky, seen end-on from afar.
                Haze(HazeMesh("Static curtain", new[] { HazeRow(48, s => new Vector3(w.x, -1, (s * 2 - 1) * span)), HazeRow(48, s => new Vector3(w.x, w.curtainHeight - 1, (s * 2 - 1) * span)) },
                    p => 1 - Mathf.SmoothStep(0, 1, (Mathf.Abs(p.z) - Half) / (span - Half))), 3000, true, .35f, 1.2f);
                // The unmade fades into haze: a flat sheet over it, clear at the curtain and opaque before the backdrop's flat skirt
                // ends (and toward its sides). Linear fog alone leaves that edge half-fogged at the sides of the view (Oakhaven, Peaks).
                var floor = new Vector3[8][];
                for (int r = 0; r < 8; r++) { float x = r < 7 ? Mathf.Lerp(x0, x1, r / 6f) : edge + 40; floor[r] = HazeRow(40, s => new Vector3(x, .3f, (s * 2 - 1) * (edge + 40))); }
                Haze(HazeMesh("Unmade haze", floor, p => Mathf.Clamp01((edge + 40 - Mathf.Abs(p.z)) / 40) * Mathf.SmoothStep(0, 1, Mathf.Max(Mathf.InverseLerp(x0, x1, p.x),
                    Mathf.InverseLerp(Half + 20, x1, Mathf.Abs(p.z)) * Mathf.InverseLerp(x0, x0 + 30, p.x)))), 2995, false, .6f, 1);   // greyed: a warm fog made it beige
                // The far bank on the horizon beyond it, on an arc covering every way the flat unmade lies from the zone: opaque below
                // the eye line and thinning upward into the sky, so the haze meets the sky in a gradient, not a line.
                float reach = edge * 1.5f, arc = Mathf.Atan2(edge, x0) + .35f;
                var bank = new[] { -70f, 0, 10, 26, 55 }.Select(y => HazeRow(48, s => new Vector3(Mathf.Cos((s * 2 - 1) * arc) * reach, y, Mathf.Sin((s * 2 - 1) * arc) * reach))).ToArray();
                Haze(HazeMesh("Unmade bank", bank, p => Mathf.Clamp01((arc - Mathf.Abs(Mathf.Atan2(p.z, p.x))) / .3f) * Mathf.Pow(Mathf.Clamp01(1 - p.y / 55), 2)), 2990, false, 0, 1);
            }
            // Grey, leafless husks of what stood beyond: the land isn't burnt, it is unmade.
            for (int i = 0; i < 14; i++)
            {
                var p = new Vector2(w.x + 3 + (float)rng.NextDouble() * (Half - w.x - 4), -Half + 6 + (float)rng.NextDouble() * (Zone.size - 12));
                DeadTree(p, .6f + (float)rng.NextDouble() * .5f, art.ash, 3, statics);
            }
            var motes = new GameObject("Unmaking motes").AddComponent<ParticleSystem>(); motes.transform.SetParent(root, false);
            motes.transform.position = new Vector3(w.x - 2, 4, 0); motes.transform.rotation = Quaternion.Euler(0, -90, 0);
            var main = motes.main; main.startLifetime = 9; main.startSpeed = .6f; main.startSize = .18f; main.maxParticles = 700;
            main.startColor = new Color(.85f, .85f, .9f, .55f); main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = motes.emission; em.rateOverTime = 60;
            var sh = motes.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(Zone.size, 8, 2);
            var vel = motes.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-1.1f, -.3f); vel.y = new ParticleSystem.MinMaxCurve(-.1f, .25f); vel.z = new ParticleSystem.MinMaxCurve(-.2f, .2f);
            var noise = motes.noise; noise.enabled = true; noise.strength = .4f; noise.frequency = .3f;
            motes.GetComponent<ParticleSystemRenderer>().sharedMaterial = art.particle;
            // The curtain is impassable: nothing walks into the unmade.
            var wall = new GameObject("Wasting boundary").AddComponent<BoxCollider>(); wall.transform.SetParent(root, false);
            wall.center = new Vector3(w.x + 1.5f, 5, 0); wall.size = new Vector3(3, 12, Zone.size);
            wall.gameObject.AddComponent<NavBlocker>();
        }

        // ---------- props ----------
        /// <summary>Dressed stone; falls back to natural stone until the art asset has been regenerated.</summary>
        Material Masonry { get { return art.masonry != null ? art.masonry : art.stone; } }
        /// <summary>
        /// Playtest note 23 (fps): the static scenery was some 18,800 renderers in Oakhaven (every tree, house, fence and rock a heap of
        /// primitives), so culling, shadows and draw calls cost the CPU most of a frame. Each thing in it (each child of the static
        /// scenery: a tree, a house, a cairn) has its plain parts merged into one mesh per material and shadow mode, in its own space,
        /// so it still fades as a whole (TreeFade, RoofFade) and nothing else moves. Left as they are: parts with a script on them (a
        /// flicker, a pulse), parts with more than one material, hidden ones, and parts something finds by name (a building's
        /// "Footing" and "Door steps"). Colliders and the parts' transforms stay; only their mesh renderers go. Returns how many
        /// renderers there were.
        /// </summary>
        int MergeStatics()
        {
            int total = 0;
            var groups = new Dictionary<(Material, UnityEngine.Rendering.ShadowCastingMode, bool), List<CombineInstance>>();
            var merged = new List<MeshRenderer>();
            foreach (Transform root in statics)
            {
                groups.Clear(); merged.Clear();
                var w2l = root.worldToLocalMatrix;
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
                {
                    total++;
                    if (!mr.enabled || mr.sharedMaterials.Length != 1 || mr.sharedMaterial == null || mr.name == "Footing" || mr.name == "Door steps") continue;
                    var mf = mr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null || mf.sharedMesh.subMeshCount != 1 || !mf.sharedMesh.isReadable) continue;
                    if (mr.GetComponent<MonoBehaviour>() != null) continue;
                    var key = (mr.sharedMaterial, mr.shadowCastingMode, mr.receiveShadows);
                    if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<CombineInstance>();
                    list.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = 0, transform = w2l * mr.localToWorldMatrix }); merged.Add(mr);
                }
                if (merged.Count < 2) continue;
                foreach (var kv in groups)
                {
                    var mesh = new Mesh { name = root.name + " (merged)", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                    mesh.CombineMeshes(kv.Value.ToArray(), true, true); mesh.RecalculateBounds();
                    var go = new GameObject("Merged " + kv.Key.Item1.name); go.transform.SetParent(root, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = kv.Key.Item1; r.shadowCastingMode = kv.Key.Item2; r.receiveShadows = kv.Key.Item3;
                }
                foreach (var mr in merged) { var mf = mr.GetComponent<MeshFilter>(); DestroyImmediate(mr); if (mf != null) DestroyImmediate(mf); }
            }
            return total;
        }
        Material Tint(Material baseMat, Color color)
        {
            // Rounded to 1/48 a channel (playtest note 23, fps): randomised tints (each dead tree's bark, each stone) made thousands of
            // near-identical materials, 3,500 material switches a frame; a step this small is not seen.
            color = new Color(Mathf.Round(color.r * 48) / 48, Mathf.Round(color.g * 48) / 48, Mathf.Round(color.b * 48) / 48, color.a);
            string key = baseMat.name + ColorUtility.ToHtmlStringRGB(color);
            if (!tints.TryGetValue(key, out var m)) { m = new Material(baseMat) { color = color, name = key }; tints[key] = m; }
            return m;
        }
        Material rockBase;
        /// <summary>Natural rock in a zone's tint: the painted rock (world-projected strata, tops lit by biome), or plain stone if the art has none.</summary>
        Material RockTint(Color color)
        {
            if (art.rock == null) return Tint(art.stone, color);
            if (rockBase == null)
            {
                rockBase = new Material(art.rock) { name = "Painted rock " + Zone.biome };   // the biome in the name keeps Tint's key apart per zone
                rockBase.SetVector("_Top", Zone.biome == "ash" || Gloom ? new Vector4(1.08f, 1.08f, 1.09f, 1) : Zone.biome == "mountain" ? new Vector4(1.08f, 1.06f, 1f, 1) : new Vector4(1.18f, 1.14f, 1.02f, 1));   // ash and gloom: neutral, grey stays grey; mountain: a small lift, the crag stays under the fog
            }
            return Tint(rockBase, color);
        }
        float R01 { get { return (float)rng.NextDouble(); } }
        Transform Root(ZoneProp p, Transform parent)
        {
            var t = new GameObject(string.IsNullOrEmpty(p.name) ? p.kind : p.name).transform; t.SetParent(parent, false);
            t.position = Ground(p.at); t.rotation = Quaternion.Euler(0, p.rotation, 0); t.localScale = Vector3.one * (p.scale <= 0 ? 1 : p.scale);
            return t;
        }
        /// <summary>Ground height under a point of a prop (local x, z), relative to its root: props sit at their centre's height, so on a slope the corners differ.</summary>
        float LocalGround(Transform t, float x, float z)
        {
            var p = t.TransformPoint(new Vector3(x, 0, z));
            return (HeightAt(p.x, p.z) - t.position.y) / Mathf.Max(.01f, t.lossyScale.y);
        }
        /// <summary>How far a plinth or footing must reach below the root so no part of a w x d footprint (centred at local z = cz) hangs over a slope; 0 on the flat.</summary>
        float FootDrop(Transform t, float w, float d, float cz = 0)
        {
            float low = 0;
            for (int sx = -1; sx <= 1; sx++) for (int sz = -1; sz <= 1; sz++) low = Mathf.Min(low, LocalGround(t, sx * w / 2, cz + sz * d / 2));
            return low < -.02f ? .12f - low : 0;
        }
        GameObject Part(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material m, Quaternion? rot = null)
        {
            var o = GameObject.CreatePrimitive(type); DestroyImmediate(o.GetComponent<Collider>());
            o.transform.SetParent(parent, false); o.transform.localPosition = localPos; o.transform.localScale = scale;
            if (rot.HasValue) o.transform.localRotation = rot.Value;
            o.GetComponent<Renderer>().sharedMaterial = m; return o;
        }
        GameObject MeshPart(Mesh mesh, Transform parent, Vector3 localPos, Material m, Quaternion? rot = null)
        {
            var o = new GameObject(mesh.name); o.transform.SetParent(parent, false); o.transform.localPosition = localPos;
            if (rot.HasValue) o.transform.localRotation = rot.Value;
            o.AddComponent<MeshFilter>().sharedMesh = mesh; o.AddComponent<MeshRenderer>().sharedMaterial = m; return o;
        }
        /// <summary>A box whose painted texture tiles per metre (walls, plinths, chimneys) and runs on unbroken from one box of a
        /// building to the next; see <see cref="ZoneMeshes.Box"/>.</summary>
        GameObject BoxPart(Transform parent, Vector3 localPos, Vector3 size, Material m, Quaternion? rot = null, float tile = 2)
        {
            return MeshPart(ZoneMeshes.Box(size, tile, localPos), parent, localPos, m, rot);
        }
        /// <summary>
        /// Dressed stone in a zone's own tint: the painted masonry. Its texture is darker than the plain stone that built stone
        /// used to wear (a mean of about .63 against .83), so the tint is lifted by a little less than that and a wall keeps
        /// the colour it had, never brighter. Plain stone in the tint as given until the art has a masonry.
        /// </summary>
        Material Dressed(Color c) { return art.masonry != null ? Tint(art.masonry, new Color(c.r * 1.25f, c.g * 1.25f, c.b * 1.25f)) : Tint(art.stone, c); }
        /// <summary>One block of a piece of stonework (see Stonework): a box at <paramref name="at"/> in its prop, its texture
        /// measured in metres from the prop's own origin, so the courses run on from block to block.</summary>
        static CombineInstance Ashlar(Vector3 at, Vector3 size, float tile = 1.75f, Quaternion? rot = null) { return Piece(ZoneMeshes.Box(size, tile, at), at, rot); }
        /// <summary>Blocks joined into one mesh of one material (a wall with its merlons, a gate's span and corbels): one object
        /// for the lot. The blocks' own meshes are used up.</summary>
        GameObject Stonework(string name, Transform parent, Material m, params CombineInstance[] blocks)
        {
            var mesh = Joined(blocks); mesh.name = name;
            foreach (var b in blocks) DestroyImmediate(b.mesh);
            return MeshPart(mesh, parent, Vector3.zero, m);
        }
        /// <summary>A headstone: one slab with a rounded head, .6 wide, a metre tall and .15 thick, centred like a cube.</summary>
        Mesh Headstone { get { return PropMesh("Headstone", () => Cutout(new[] { new Vector2(-.3f, -.5f), new Vector2(-.3f, .26f), new Vector2(-.22f, .41f), new Vector2(-.09f, .5f), new Vector2(.09f, .5f), new Vector2(.22f, .41f), new Vector2(.3f, .26f), new Vector2(.3f, -.5f) }, .15f)); } }
        void Solid(Transform root, Vector3 center, Vector3 size)
        {
            var box = root.gameObject.AddComponent<BoxCollider>(); box.center = center; box.size = size;
            if (root.GetComponent<NavBlocker>() == null) root.gameObject.AddComponent<NavBlocker>();
        }
        void BuildProps()
        {
            foreach (var p in Zone.props)
            {
                if (p == null || string.IsNullOrEmpty(p.kind)) continue;
                // Moving parts (doors, wheels) and things that can be picked or emptied can't be static-batched.
                var parent = p.kind == "bridge" || p.kind == "cavern" || p.kind == "inn" || p.kind == "mill" || p.kind == "coop" || p.kind == "herb" || !string.IsNullOrEmpty(p.interact) ? props : statics;
                var t = Root(p, parent);
                // A ruin never stands in a building: moved clear here, or (a plain one) built for its draws and then left out.
                bool drop = (p.kind == "ruin" || p.kind == "ruined_house") && !ClearOfBuildings(p, t);
                if (p.kind == "wall") WarnWallInBuilding(p);
                // New landmark kinds draw from their own stream, after taking the draws the kind that stood here took, so
                // every later prop, tree and rock keeps the layout it had.
                var zoneRng = rng; int legacy = p.kind == "cave" ? 30 : p.kind == "rib" ? 6 : p.kind == "perch" || p.kind == "wallow" || p.kind == "brood" || p.kind == "cavern" || p.kind == "monolith" || p.kind == "giant_tree" || p.kind == "treehouse" || p.kind == "waterfall" || p.kind == "mushrooms" || p.kind == "fallen_giant" || p.kind == "leathershop" || p.kind == "dryhut" || p.kind == "kitchen" || p.kind == "gamerack" ? 0 : -1;
                if (legacy >= 0) { for (int k = 0; k < legacy; k++) _ = R01; rng = new System.Random(Zone.seed ^ (Mathf.RoundToInt(p.at.x * 8) * 73856093) ^ (Mathf.RoundToInt(p.at.y * 8) * 19349663)); }
                switch (p.kind)
                {
                    case "house": House(t, p.size.x > 0 ? p.size : new Vector2(7, 5), 3f, p.variant, false); break;
                    case "inn": Inn(t, p.size.x > 0 ? p.size : new Vector2(12, 8), p.variant); break;
                    case "barn": Barn(t, p.size.x > 0 ? p.size : new Vector2(12, 8)); break;
                    case "mill": Mill(t, p.size.x > 0 ? p.size : new Vector2(7, 6)); break;
                    case "well": Well(t, p.variant); break;
                    case "coop": Coop(t, string.IsNullOrEmpty(p.name) ? "Hen coop" : p.name); break;
                    case "forge": Forge(t); break;
                    case "stall": Stall(t, p.variant); break;
                    case "oven": Oven(t); break;
                    case "tannery": Tannery(t); break;
                    case "woodpile": Woodpile(t); break;
                    case "leathershop": LeatherShop(t); break;
                    case "dryhut": DryingHut(t); break;
                    case "kitchen": Kitchen(t); break;
                    case "gamerack": GameRack(t); break;
                    case "wagon": Wagon(t); break;
                    case "herb": Herb(t, p.variant); break;
                    case "ruined_house": RuinedHouse(t, p.size.x > 0 ? p.size : new Vector2(8, 6)); break;
                    case "wall": Wall(p.points, statics); DestroyImmediate(t.gameObject); break;
                    case "tower": Tower(t, p.size.x > 0 ? p.size.x : 4.4f); break;
                    case "giant_tree": GiantTree(t, p.variant); trunks.Add(p.at); break;
                    case "fallen_giant": FallenGiant(t, p.size.x > 0 ? p.size.x : 36); break;
                    case "treehouse": Treehouse(t, p.variant); trunks.Add(p.at); break;
                    case "waterfall": Waterfall(t, p.size.x > 0 ? p.size.x : 6, p.lift > 0 ? p.lift : 10); break;
                    case "mushrooms": Mushrooms(t, p.size.x > 0 ? p.size.x : 2, p.variant); break;
                    case "monolith": Monolith(t); break;
                    case "gallows": Gallows(t); break;
                    case "crypt": Crypt(t, p.variant); break;
                    case "cliff": Cliff(t, p.size.x > 0 ? p.size.x : 20, p.lift); break;
                    case "dead_oak": DeadTree(p.at, 1, Tint(art.bark, new Color(.4f, .36f, .32f)), 5, statics, t); break;   // weathered, paler bark (plain bark reads black in shade); size comes from the root's scale
                    case "great_oak": GreatOak(t); break;
                    case "tree": Broadleaf(t, p.variant); break;
                    case "pine": Pine(t); break;
                    case "fence": Fence(t, p.size.x > 0 ? p.size.x : 8); break;
                    case "hedge": Hedge(t, p.size.x > 0 ? p.size.x : 6); break;
                    case "haystack": Haystack(t); break;
                    case "cart": Cart(t); break;
                    case "barrels": for (int i = 0; i <= p.variant % 3; i++) Barrel(t, new Vector3(i * .75f - .4f, 0, (i % 2) * .6f), i == 1 ? .9f : 1, i * 70); Solid(t, new Vector3(0, .5f, .3f), new Vector3(2.2f, 1, 1.4f)); break;
                    case "crates": for (int i = 0; i <= p.variant % 3; i++) Crate(t, new Vector3(i == 2 ? -.05f : i * .9f - .5f, i == 2 ? 1.19f : .41f, 0), i == 2 ? .7f : .82f, i == 2 ? 24 : i * 9 - 4); Solid(t, new Vector3(0, .5f, 0), new Vector3(2.2f, 1, 1)); break;   // the third crate rests across the two below, not on air
                    case "lamp": Lamp(t, p.variant); break;
                    case "grave": { float a = R01 * 8 - 4, b = R01 * 8 - 4; if (p.variant == 1) { a = Mathf.Sign(a) * (14 + Mathf.Abs(a) * 2); b *= 2; } MeshPart(Headstone, t, new Vector3(0, p.variant == 1 ? .36f : .46f, 0), RockTint(new Color(.52f, .51f, .48f)), Quaternion.Euler(a, 0, b)); break; }   // variant 1: heaved over and half sunk (the drowned graves)
                    case "rock":
                    {
                        float s = 1 + p.variant * .6f; var stone = RockTint(new Color(.52f, .51f, .48f));
                        // Mountains: a loose rock left on a face the crags raised rolls to flatter ground near by and sinks by the slope.
                        if (Zone.biome == "mountain") { stone = RockTint(MountainStone); if (string.IsNullOrEmpty(p.interact)) { var spot = FlatterSpot(p.at, 1.2f * s); t.position = Ground(spot); SinkBySlope(t, spot, s); } }
                        else
                        {
                            // On a slope the ground falls away under one side: seated on the lowest ground under it (playtest note 21).
                            float low = t.position.y;
                            foreach (float k in new[] { -.9f, 0, .9f }) foreach (float j in new[] { -.8f, 0, .8f }) low = Mathf.Min(low, HeightAt(t.position.x + k * s, t.position.z + j * s));
                            t.position += Vector3.up * (low - t.position.y) * .85f;
                        }
                        Lump(Boulder(), t, new Vector3(0, .3f * s, 0), new Vector3(2f * s, 1.3f * s, 1.7f * s), stone, R01 * 360); if (s > 1.3f) Lump(Boulder(), t, new Vector3(.7f * s, .15f * s, .5f * s), new Vector3(.9f * s, .6f * s, .8f * s), stone, R01 * 360); Solid(t, new Vector3(0, .5f * s, 0), new Vector3(1.6f * s, 1f * s, 1.4f * s)); if (Zone.biome == "mountain" && string.IsNullOrEmpty(p.interact)) RockSkirt(t, s * .8f, stone); break;
                    }
                    case "bridge": SeatBridge(t, p.size.x > 0 ? p.size.x : 12); if (p.variant == 1) RopeBridge(t, p.size.x > 0 ? p.size.x : 12); else Bridge(t, p.size.x > 0 ? p.size.x : 12); break;
                    case "signpost": Signpost(t, p.name); break;
                    case "board": NoticeBoard(t); break;
                    case "ruin": Ruin(t, p.size.x > 0 ? p.size.x : 6); break;
                    case "gate": Gate(t, p.size.x > 0 ? p.size.x : 6.4f); break;
                    case "keep": Stronghold(t, p.size.x > 0 ? p.size : new Vector2(7, 6)); break;
                    case "perch": Perch(t, p.size.x > 0 ? p.size.x : 8); break;
                    case "wallow": Wallow(t, p.size.x > 0 ? p.size.x : 6); break;
                    case "cave": Cave(t, p.size.x > 0 ? p.size.x : 20); break;
                    case "shelter": Shelter(t); break;
                    case "brazier": Brazier(t, Vector3.zero, p.variant == 1 ? Bone : null); Solid(t, new Vector3(0, .6f, 0), new Vector3(.8f, 1.2f, .8f)); break;
                    case "brood": Brood(t, p.size.x > 0 ? p.size.x : 5); break;
                    case "rib": Rib(t, p.size.x > 0 ? p.size.x : 6, p.size.y > 0 ? p.size.y : 7); break;
                    case "spine": Spine(t, p.size.x > 0 ? p.size.x : 24, p.size.y > 0 ? p.size.y : 7); break;
                    case "shrine": Shrine(t); break;
                    case "idol": Idol(t); break;
                    case "wayshrine": Wayshrine(t); break;
                    case "cavern": Cavern(t, Hollow.All.Find(h => h.Name == t.name), p.variant); break;
                    case "chest": TreasureChest(t); break;
                    default: Debug.LogWarning("Unknown zone prop kind '" + p.kind + "'."); break;
                }
                rng = zoneRng;
                if (drop) { DestroyImmediate(t.gameObject); continue; }
                if (t != null && Footprint(p).x > 0) t.gameObject.AddComponent<RoofFade>();   // see-through when it hides the player (playtest note 26)
                if (!string.IsNullOrEmpty(p.interact) && t != null)
                    Interactables.Add(new ZoneInteractable { name = string.IsNullOrEmpty(p.name) ? p.kind : p.name, prompt = p.interact, item = p.item, kind = p.kind, once = p.once, node = string.IsNullOrEmpty(p.node) ? null : p.node, position = t.position, root = t });
            }
        }
        /// <summary>
        /// A treasure chest (2026-10-05, Chris's animated chest: "use in the loot table"): quiArt's wooden chest, a little under a
        /// metre wide, its lid driven by ChestLid; solid. The session fills it and opens it (EncounterSession.OpenChest).
        /// </summary>
        void TreasureChest(Transform t)
        {
            var src = Resources.Load<GameObject>("Props/Chest_Wood"); if (src == null) { Crate(t, new Vector3(0, .41f, 0), .82f, 0); return; }
            var go = Instantiate(src, t, false); go.name = "Chest";
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            var b = new Bounds(); bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (any) { float s = .95f / Mathf.Max(.01f, Mathf.Max(b.size.x, b.size.z)); go.transform.localScale = Vector3.one * s; go.transform.localPosition = new Vector3(0, (t.position.y - b.min.y) * s, 0); }
            go.AddComponent<ChestLid>().Init(go);
            Solid(t, new Vector3(0, .35f, 0), new Vector3(1, .7f, .7f));
        }

        /// <summary>
        /// A barn a household lives in (Moss's lodge) has no door of its own to knock at: it gets a barred one ("home") a little
        /// out from the middle of its big doors on the -Z side, so villagers have somewhere to go in. Draws nothing random.
        /// </summary>
        void BuildHomeDoors()
        {
            if (Zone.life == null || Zone.life.households == null) return;
            foreach (var h in Zone.life.households)
            {
                if (h == null || string.IsNullOrEmpty(h.house) || Doors.Exists(d => d.name == h.house)) continue;
                var p = Array.Find(Zone.props, q => q != null && q.kind == "barn" && q.name == h.house); if (p == null) continue;
                float d = (p.size.y > 0 ? p.size.y : 8) * (p.scale <= 0 ? 1 : p.scale);
                var at = Ground(p.at) + Quaternion.Euler(0, p.rotation, 0) * new Vector3(0, 0, -d / 2 - .6f);
                Doors.Add(new ZoneDoor { name = h.house, kind = "home", openable = false, position = Ground(new Vector2(at.x, at.z), 1) });
            }
        }
        /// <summary>Half-extents of a building's footprint (a little over its walls, for roof and plinth); zero for anything else.</summary>
        static Vector2 Footprint(ZoneProp p)
        {
            switch (p.kind)
            {
                case "house": return (p.size.x > 0 ? p.size : new Vector2(7, 5)) / 2 + Vector2.one * .8f;
                case "inn": case "barn": return (p.size.x > 0 ? p.size : new Vector2(12, 8)) / 2 + Vector2.one * .8f;
                case "mill": case "keep": return (p.size.x > 0 ? p.size : new Vector2(7, 6)) / 2 + Vector2.one * .8f;
                case "tower": return Vector2.one * ((p.size.x > 0 ? p.size.x : 4.4f) / 2 + .3f);
                case "crypt": return new Vector2(3.6f, 4.6f);
                case "forge": return new Vector2(2.9f, 2.4f);
                case "tannery": case "shelter": return new Vector2(2.2f, 2.1f);
                case "leathershop": return LeatherShopSize / 2 + Vector2.one * .8f;
                case "dryhut": return DryingHutSize / 2 + Vector2.one * .8f;
                case "kitchen": return KitchenSize / 2 + new Vector2(.3f, .4f);
                default: return Vector2.zero;
            }
        }
        /// <summary>Whether a point stands under a building's roof (its footprint, eaves included), under an inn's porch or in a cave: no rain or snow falls there.</summary>
        public bool UnderRoof(Vector2 p) { return Hollow.CoverAt(p, .3f) > 0 || InBuilding(p) || UnderPorch(p); }   // a cave's passage, a building, or an inn's porch
        /// <summary>Whether a point stands inside a building's footprint, eaves included (house, inn, barn, mill, keep, tower, crypt,
        /// forge, tannery, shelter, leather shop, drying hut, kitchen).</summary>
        public bool InBuilding(Vector2 p)
        {
            foreach (var b in Zone.props)
            {
                if (b == null) continue;
                var h = Footprint(b); if (h.x <= 0) continue;
                float r = b.rotation * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r); var d = p - b.at;
                if (Mathf.Abs(d.x * c - d.y * s) <= h.x && Mathf.Abs(d.x * s + d.y * c) <= h.y) return true;   // along its local x and z (as Overlap)
            }
            return false;
        }
        /// <summary>Whether two footprints (centre, half-size, yaw) overlap; push is the shortest move of the first that parts them.</summary>
        static bool Overlap(Vector2 ca, Vector2 ha, float ya, Vector2 cb, Vector2 hb, float yb, out Vector2 push)
        {
            Vector2 X(float yaw) { float r = yaw * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(r), -Mathf.Sin(r)); }   // a prop's local x and z in the world (Unity's yaw)
            Vector2 Z(float yaw) { float r = yaw * Mathf.Deg2Rad; return new Vector2(Mathf.Sin(r), Mathf.Cos(r)); }
            Vector2 xa = X(ya), za = Z(ya), xb = X(yb), zb = Z(yb), d = cb - ca; push = Vector2.zero; float least = float.MaxValue;
            foreach (var n in new[] { xa, za, xb, zb })
            {
                float reach = Mathf.Abs(Vector2.Dot(xa, n)) * ha.x + Mathf.Abs(Vector2.Dot(za, n)) * ha.y + Mathf.Abs(Vector2.Dot(xb, n)) * hb.x + Mathf.Abs(Vector2.Dot(zb, n)) * hb.y;
                float along = Vector2.Dot(d, n), gap = reach - Mathf.Abs(along);
                if (gap <= 0) return false;   // a separating axis
                if (gap < least) { least = gap; push = n * (along > 0 ? -gap : gap); }
            }
            return true;
        }
        /// <summary>
        /// A ruin or fallen house never stands in a building: one over a building's footprint is pushed straight out of it (up
        /// to 5 m, never onto a road or into water), with a warning. If that fails a usable one (a quest search) stays put and a
        /// plain one is left out. Draws nothing random, and the prop is built either way, so the zone's layout is unchanged.
        /// </summary>
        bool ClearOfBuildings(ZoneProp p, Transform t)
        {
            var half = p.kind == "ruin" ? new Vector2((p.size.x > 0 ? p.size.x : 6) / 2 + .5f, .6f) : (p.size.x > 0 ? p.size : new Vector2(8, 6)) / 2 + Vector2.one * .3f;
            var at = p.at; string name = Zone.id + ": '" + (string.IsNullOrEmpty(p.name) ? p.kind : p.name) + "'";
            for (int pass = 0; pass < 4; pass++)
            {
                ZoneProp hit = null; Vector2 push = Vector2.zero;
                foreach (var b in Zone.props)
                {
                    if (b == null || b == p) continue; var bh = Footprint(b); if (bh.x <= 0) continue;
                    if (Overlap(at, half, p.rotation, b.at, bh, b.rotation, out var v) && v.sqrMagnitude > push.sqrMagnitude) { hit = b; push = v; }
                }
                if (hit == null) { if (pass > 0) { t.position = Ground(at); Debug.LogWarning(name + " stood in a building; moved from " + p.at + " to " + at + "."); } return true; }
                var next = at + push + push.normalized * .4f;
                if (Vector2.Distance(next, p.at) > 5 || NearRoad(next, half.x) || Water.NearWater(next, half.x)) break;
                at = next;
            }
            if (!string.IsNullOrEmpty(p.interact)) { Debug.LogWarning(name + " stands in a building and could not be moved clear; kept, it is used."); return true; }
            Debug.LogWarning(name + " stands in a building and could not be moved clear; left out."); return false;
        }
        /// <summary>A curtain wall through a house, inn, barn, mill or keep is reported (it is built as authored: it joins towers and gates).</summary>
        void WarnWallInBuilding(ZoneProp p)
        {
            for (int i = 0; i + 1 < p.points.Length; i++)
            {
                Vector2 a = p.points[i], b = p.points[i + 1], dir = b - a; float len = dir.magnitude; if (len < .1f) continue;
                float yaw = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;   // the segment along its local x
                foreach (var h in Zone.props)
                    if (h != null && (h.kind == "house" || h.kind == "inn" || h.kind == "barn" || h.kind == "mill" || h.kind == "keep") && Overlap((a + b) / 2, new Vector2(len / 2, .6f), yaw, h.at, Footprint(h), h.rotation, out _))
                        Debug.LogWarning(Zone.id + ": wall '" + p.name + "' runs through '" + (string.IsNullOrEmpty(h.name) ? h.kind : h.name) + "' at " + h.at + ".");
            }
        }
        static readonly Color[] Plaster = { new Color(.88f, .82f, .68f), new Color(.8f, .76f, .66f), new Color(.86f, .72f, .54f) };
        // Painted shutters and doors (playtest note 13, "need high fantasy not pale"), times the timber grain: green, madder red,
        // woad blue, ochre; doors oiled oak, oxblood, blue, bottle green and ochre.
        static readonly Color[] Shutter = { new Color(.12f, .5f, .22f), new Color(.8f, .15f, .1f), new Color(.16f, .34f, .86f), new Color(.95f, .62f, .14f) };
        static readonly Color[] Door = { new Color(.5f, .3f, .15f), new Color(.7f, .12f, .08f), new Color(.14f, .3f, .74f), new Color(.12f, .46f, .22f), new Color(.9f, .6f, .14f) };
        /// <summary>House paint in this zone: as given in a living village, withered in a gloom, half-drained under the ash.</summary>
        Color Paint(Color c) { if (Zone.biome == "ash") { float g = c.grayscale; return Color.Lerp(c, new Color(g, g, g, c.a), .5f); } return Wither(c); }
        /// <summary>
        /// The eaves of a gable roof (the painted style pass, 2026-10-01): a fascia board along each eave, rafter ends hung under
        /// the soffit from the wall out to the fascia, and a ridge cap seated on the ridge. <paramref name="w"/> and
        /// <paramref name="d"/> are the walls' footprint, <paramref name="top"/> the wall top (the roof's eave line); the roof
        /// reaches .7 m past the walls front and back and its slab is .25 m thick.
        /// </summary>
        void Eaves(Transform t, float w, float d, float top, float roofH, Material roofMat)
        {
            var dark = Tint(art.timber, new Color(.26f, .17f, .11f));
            int n = Mathf.Max(1, Mathf.FloorToInt((w - .5f) / .9f));
            foreach (int sz in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(0, top - .2f, sz * (d / 2 + .72f)), new Vector3(w + 1.3f, .38f, .1f), dark);   // fascia: top-.39 .. top-.01, over the eave edge and the rafter ends
                for (int i = 0; i <= n; i++) Part(PrimitiveType.Cube, t, new Vector3((i - n / 2f) * .9f, top - .31f, sz * (d / 2 + .33f)), new Vector3(.12f, .14f, .7f), dark);   // rafter ends: top-.38 .. top-.24, 1 cm into the soffit (top-.25), from 2 cm inside the wall face to 1 cm into the fascia
            }
            float sag = roofH * .17f / (d / 2 + .7f);   // how far the slope has dropped at the cap's edge
            Part(PrimitiveType.Cube, t, new Vector3(0, top + roofH + .05f - (sag + .07f) / 2, 0), new Vector3(w + 1.3f, sag + .07f, .34f), roofMat == art.slate ? dark : Tint(art.thatch, new Color(.6f, .48f, .26f)));   // ridge cap: its bottom edges sink 2 cm into the slopes
        }
        /// <summary>
        /// The two gable ends of a walled building under an open gable roof (ZoneMeshes.GableRoof with a verge): the wall carried
        /// up to the roof in <paramref name="wall"/>, its texture running on from the wall below (<paramref name="tile"/> metres a
        /// tile); a dark cheek that closes the corner under each eave, beside the wall; a barge board down each verge, so the
        /// roof's edge reads thick; and, with <paramref name="beam"/> above 0 (the timbers' depth), a tie beam on the wall top
        /// with a king post and a collar over it. <paramref name="face"/> is the gable walls' outer face (x) and
        /// <paramref name="d"/> their width, <paramref name="top"/> the wall top, <paramref name="run"/> half the roof's depth,
        /// <paramref name="end"/> half its length and <paramref name="slab"/> its thickness. For the eye only: no colliders.
        /// </summary>
        void Gables(Transform t, float face, float d, float top, float roofH, float run, float end, Material wall, float tile, float beam, float slab = .25f)
        {
            var dark = Tint(art.timber, new Color(.26f, .17f, .11f));
            var infill = ZoneMeshes.Gable(d, roofH, .3f, tile, run - d / 2, top);
            var cheeks = ZoneMeshes.Gable(run * 2, roofH + slab, .06f, 1, run * slab / roofH); cheeks.name = "Eave cheeks";   // from the soffit up: the same slope, upright for the slab's depth at each eave
            float slope = Mathf.Sqrt(run * run + roofH * roofH), pitch = Mathf.Atan2(roofH, run) * Mathf.Rad2Deg, board = Mathf.Min(.3f, roofH * .2f);
            foreach (int sx in new[] { -1, 1 })
            {
                var turn = Quaternion.Euler(0, sx * 90, 0);   // the meshes' +z face looks out
                MeshPart(infill, t, new Vector3(sx * (face - .15f), top, 0), wall, turn);       // face-.3 .. face
                MeshPart(cheeks, t, new Vector3(sx * (face - .05f), top - slab, 0), dark, turn);   // face-.08 .. face-.02: inside the wall, seen only past its corners
                if (beam > 0)
                {
                    float post = roofH * (1 - beam * .45f / run) - beam / 2, half = run * (.5f - beam * .4f / roofH);   // the post's head and the collar's ends stop on the slope
                    Part(PrimitiveType.Cube, t, new Vector3(sx * (face + .02f), top, 0), new Vector3(.12f, beam, d), art.timber);                                   // tie beam: face-.04 .. face+.08
                    Part(PrimitiveType.Cube, t, new Vector3(sx * (face + .02f), top + beam / 2 + post / 2, 0), new Vector3(.12f, post, beam * .9f), art.timber);   // king post, standing on it
                    Part(PrimitiveType.Cube, t, new Vector3(sx * (face + .02f), top + roofH * .5f, 0), new Vector3(.1f, beam * .8f, half * 2), art.timber);       // collar: 1 cm behind the post's face
                }
                // Barge boards: 3 cm above the slope and hanging down from it, from 3 cm past the ridge (the two cross there) to 12 cm past the eave; end-.02 .. end+.06, 1 cm proud of the fascia's end.
                foreach (int sz in new[] { -1, 1 })
                {
                    var down = new Vector3(0, -roofH, sz * run) / slope; var up = new Vector3(0, run, sz * roofH) / slope;
                    Part(PrimitiveType.Cube, t, new Vector3(sx * (end + .02f), top + roofH / 2, sz * run / 2) + down * .045f - up * (board / 2 - .03f), new Vector3(.08f, board, slope + .15f), dark, Quaternion.Euler(sz * pitch, 0, 0));
                }
            }
        }
        /// <summary>
        /// A framed window on a wall: <paramref name="face"/> is the point on the OUTER WALL FACE at the window's centre and
        /// <paramref name="sz"/> the way that face looks (+1 or -1 along z). Frame, sill and shutters are all set into the wall
        /// by 1 to 2 cm. <paramref name="glass"/> is false where the wall already has its pane (the inn's passes through the wall).
        /// </summary>
        void Window(Transform t, Vector3 face, float wide, float high, int sz, int variant, bool shutters, bool glass = true)
        {
            var frame = Tint(art.timber, new Color(.24f, .16f, .1f));
            var at = face + new Vector3(0, 0, sz * .05f);
            if (glass) Part(PrimitiveType.Cube, t, at, new Vector3(wide, high, .08f), art.glass);   // face+.01 .. face+.09
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, at + new Vector3(0, s * (high / 2 + .04f), 0), new Vector3(wide + .16f, .08f, .14f), frame);   // face-.02 .. face+.12
                Part(PrimitiveType.Cube, t, at + new Vector3(s * (wide / 2 + .04f), 0, 0), new Vector3(.08f, high, .14f), frame);
            }
            Part(PrimitiveType.Cube, t, face + new Vector3(0, -high / 2 - .1f, sz * .11f), new Vector3(wide + .3f, .08f, .26f), frame);   // the sill: face-.02 .. face+.24
            if (shutters) foreach (int s in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, face + new Vector3(s * (wide / 2 + .24f), 0, sz * .02f), new Vector3(.3f, high + .1f, .06f), Tint(art.timber, Paint(Shutter[Mathf.Abs(variant) % Shutter.Length])));   // on the wall: face-.01 .. face+.05, 1 cm clear of the frame
        }
        /// <summary>A plank door facing -z: the slab (returned), three plank seams, two iron bands and a ring, the iron set 5 mm into the planks.</summary>
        GameObject PlankDoor(Transform t, Vector3 at, float wide, float high, Material planks, float thick = .06f)
        {
            var slab = Part(PrimitiveType.Cube, t, at, new Vector3(wide, high, thick), planks);
            for (int i = -1; i <= 1; i++) Part(PrimitiveType.Cube, t, at + new Vector3(i * wide / 4, 0, -thick / 2 - .005f), new Vector3(.025f, high, .01f), Tint(art.timber, new Color(.1f, .07f, .05f)));
            foreach (float y in new[] { -high * .28f, high * .25f }) Part(PrimitiveType.Cube, t, at + new Vector3(0, y, -thick / 2 - .01f), new Vector3(wide * .92f, .09f, .03f), art.metal);
            Part(PrimitiveType.Cylinder, t, at + new Vector3(wide * .3f, -high * .05f, -thick / 2 - .01f), new Vector3(.14f, .015f, .14f), art.metal, Quaternion.Euler(90, 0, 0));   // the ring, between the bands
            return slab;
        }
        void House(Transform t, Vector2 size, float wallHeight, int variant, bool inn)
        {
            float w = size.x, d = size.y;
            var plaster = Tint(art.plaster, Plaster[Mathf.Abs(variant) % Plaster.Length]);
            var roofMat = variant % 2 == 0 ? art.thatch : art.slate;
            Footing(t, w + .34f, d + .34f, .6f, .52f, .9f, .62f);   // the plinth: stepped stone down to the ground and up the slope (2 cm proud of the corner posts it climbs over), open behind the door
            BoxPart(t, new Vector3(0, .6f + wallHeight / 2, 0), new Vector3(w, wallHeight, d), plaster);
            // Timber framing: corner posts, a sill rail under the windows (a rail per storey on an inn) and a knee brace from each
            // corner post up to the first rail, so the door and windows sit in clear panels with nothing crossing them.
            float top = .6f + wallHeight;
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
                Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2), .6f + wallHeight / 2, sz * (d / 2)), new Vector3(.3f, wallHeight, .3f), art.timber);
            int storeys = inn ? 2 : 1; float firstRail = .6f + wallHeight * (inn ? .5f : .3f);
            for (int s = 1; s <= storeys; s++)
            {
                float y = inn ? .6f + wallHeight * s / storeys : firstRail;
                foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(0, y, sz * (d / 2 + .02f)), new Vector3(w, .22f, .12f), art.timber);
                foreach (int sx in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .02f), y, 0), new Vector3(.12f, .22f, d), art.timber);
            }
            float rise = firstRail - .6f, run = rise * .625f;   // 32 degree braces: foot against the post, head under the rail
            foreach (int sz in new[] { -1, 1 }) foreach (int sx in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 - .15f - run / 2), .6f + rise / 2, sz * (d / 2 + .03f)), new Vector3(.14f, rise / .848f, .1f), art.timber, Quaternion.Euler(0, 0, sx * 32));
            // Door faces south (-Z), in a timber frame in front of the plinth. It stands on a stone threshold at the highest
            // ground across the doorway, so the grass never cuts it, with steps up to it where the ground falls away in front
            // (DoorSteps). Warm windows either side and on the back.
            float foot = Mathf.Clamp(DoorGround(t, 0, -d / 2 - .3f, 1.2f) + .03f, -.25f, .65f);
            PlankDoor(t, new Vector3(0, (foot + 2.7f) / 2, -d / 2 - .19f), 1.2f, 2.7f - foot, Tint(art.timber, Paint(Door[Mathf.Abs(variant * 2 + 1) % Door.Length])));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * .68f, (foot + 2.86f) / 2, -d / 2 - .12f), new Vector3(.16f, 2.86f - foot, .24f), art.timber);
            DoorSteps(t, 0, -d / 2 - .15f, foot, 1.5f);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.78f, -d / 2 - .12f), new Vector3(1.52f, .16f, .24f), art.timber);
            ZoneDoor front = null;
            // The door's point stands on the step in front of it: behind the slab, between door and wall, the navmesh can leave a
            // pocket of its own that a villager going home cannot path to (the Crisp cottage, 2026-10-01).
            if (!inn) { front = new ZoneDoor { name = t.name, openable = false, position = t.TransformPoint(new Vector3(0, 1, -d / 2 - .75f)) }; Doors.Add(front); }
            for (float x = -w / 2 + 1.4f; x < w / 2 - .8f; x += 2.6f)
            {
                if (Mathf.Abs(x) < 1.2f) continue;
                // Shutters only where they clear the corner post (inner face at w/2 - .15) and, on the door's wall, the door jamb (outer edge at .76).
                foreach (int sz in new[] { -1, 1 })
                    Window(t, new Vector3(x, .6f + wallHeight * (inn ? .3f : .55f), sz * d / 2), .8f, .7f, sz, variant, Mathf.Abs(x) + .79f <= w / 2 - .15f && (sz > 0 || Mathf.Abs(x) - .79f >= .78f));
                if (inn) foreach (int sz in new[] { -1, 1 })
                    Window(t, new Vector3(x, .6f + wallHeight * .78f, sz * d / 2), .8f, .7f, sz, variant, false);
            }
            float roofH = Mathf.Max(2.2f, d * .55f);
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH, .25f, .6f), t, new Vector3(0, top, 0), roofMat);
            Eaves(t, w, d, top, roofH, roofMat);
            Gables(t, w / 2, d, top, roofH, d / 2 + .7f, w / 2 + .6f, plaster, 2, .22f);   // plaster to the ridge, framed like the walls
            BoxPart(t, new Vector3(w / 2 - 1.1f, top + roofH * .75f, d * .15f), new Vector3(.9f, roofH * 1.1f, .9f), Masonry, null, 1);
            BoxPart(t, new Vector3(w / 2 - 1.1f, top + roofH * 1.3f + .04f, d * .15f), new Vector3(1.1f, .12f, 1.1f), Tint(Masonry, new Color(.45f, .44f, .4f)), null, 1);   // the chimney's cap, 2 cm down over the stack
            if (art.particle != null) { var smoke = Smoke(t, new Vector3(w / 2 - 1.1f, top + roofH * 1.35f, d * .15f)); if (front != null) front.smoke = smoke; }
            if (inn)
            {
                // Hanging sign on a bracket by the door: the Golden Cask.
                Part(PrimitiveType.Cube, t, new Vector3(-1.6f, 3.2f, -d / 2 - .8f), new Vector3(.12f, .12f, 1.6f), art.timber);
                Part(PrimitiveType.Cube, t, new Vector3(-1.6f, 2.6f, -d / 2 - 1.4f), new Vector3(.9f, .7f, .08f), Tint(art.timber, new Color(.5f, .36f, .2f)));
                Part(PrimitiveType.Cylinder, t, new Vector3(-1.6f, 2.6f, -d / 2 - 1.46f), new Vector3(.45f, .06f, .45f), art.metal, Quaternion.Euler(90, 0, 0));
                Part(PrimitiveType.Cylinder, t, new Vector3(w / 2 - 1.2f, .5f, -d / 2 - 1), new Vector3(.8f, .5f, .8f), art.timber);
                Part(PrimitiveType.Cylinder, t, new Vector3(w / 2 - 2.1f, .5f, -d / 2 - 1.1f), new Vector3(.8f, .5f, .8f), art.timber);
                var glow = new GameObject("Inn light").AddComponent<Light>(); glow.transform.SetParent(t, false);
                glow.transform.localPosition = new Vector3(0, 2.2f, -d / 2 - 1.5f); glow.type = LightType.Point; glow.range = 9; glow.intensity = 1.6f; glow.color = new Color(1, .72f, .4f);
            }
            Solid(t, new Vector3(0, (top + roofH) / 2, 0), new Vector3(w + .4f, top + roofH, d + .4f));
        }
        /// <summary>
        /// A walk-in inn: hollow ground floor behind a working door, with a bar, tables, stools and a lit hearth.
        /// Built at ground level (no plinth under the floor) so characters and navigation walk straight in.
        /// Each wall piece is its own collider/nav blocker so the interior stays open. variant 1 is the Cracked Hearth (CrackedHearth).
        /// </summary>
        void Inn(Transform t, Vector2 size, int variant)
        {
            float w = size.x, d = size.y, H = 6.4f, storey = 3.7f, wall = .3f, doorW = 1.6f, doorH = 2.4f;
            var plaster = Tint(art.plaster, Plaster[Mathf.Abs(variant) % Plaster.Length]);
            var boards = Tint(art.timber, new Color(.38f, .27f, .18f));
            var dark = Tint(art.timber, new Color(.2f, .13f, .08f));
            GameObject WallPiece(Vector3 pos, Vector3 scale)
            {
                var o = BoxPart(t, pos, scale, plaster);
                o.AddComponent<BoxCollider>(); o.AddComponent<NavBlocker>(); return o;
            }
            // Floor, skirting and walls.
            Part(PrimitiveType.Cube, t, new Vector3(0, .02f, 0), new Vector3(w - wall, .05f, d - wall), boards);
            WallPiece(new Vector3(0, H / 2, d / 2), new Vector3(w, H, wall));
            foreach (int s in new[] { -1, 1 }) WallPiece(new Vector3(s * w / 2, H / 2, 0), new Vector3(wall, H, d));
            float side = (w - doorW) / 2;
            foreach (int s in new[] { -1, 1 })
            {
                // The jambs' colliders stop .1 m short of the opening (the plaster doesn't): EncounterNavigation pads every blocker
                // by .1 m and the navmesh erodes by the agent's .5 m, which left the 1.6 m door a one-voxel diagonal thread that broke
                // at some angles to the grid (the Cracked Hearth's taproom was cut off from Khaven). The nav gap is now 1.6 m.
                var jamb = WallPiece(new Vector3(s * (doorW / 2 + side / 2), H / 2, -d / 2), new Vector3(side, H, wall)).GetComponent<BoxCollider>();
                jamb.size -= new Vector3(.1f, 0, 0); jamb.center += new Vector3(s * .05f, 0, 0);
            }
            WallPiece(new Vector3(0, doorH + (H - doorH) / 2, -d / 2), new Vector3(doorW, H - doorH, wall));
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, new Vector3(sx * w / 2, H / 2, sz * d / 2), new Vector3(.34f, H, .34f), art.timber);
            // A stone footing hugs the outside of the walls only (the floor inside stays level with the ground), stepping down to
            // the ground on a slope (Footing), open at the door and where a kitchen lean-to stands on the back wall.
            float? kitchen = KitchenBehind(t, d / 2 + wall / 2);
            var footing = Tint(Masonry, new Color(.5f, .48f, .44f));
            Footing(t, w + .5f, d + .5f, .3f, .35f, .5f, doorW / 2 + .05f, kitchen.HasValue ? new Vector2(kitchen.Value - KitchenSize.x / 2 - .1f, kitchen.Value + KitchenSize.x / 2 + .1f) : (Vector2?)null, footing);
            // Storey line and braces outside; the floor above is a closed ceiling inside.
            foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(0, storey, sz * (d / 2 + .02f)), new Vector3(w, .22f, wall + .1f), art.timber);
            foreach (int sx in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .02f), storey, 0), new Vector3(wall + .1f, .22f, d), art.timber);
            // Ceiling: solid so the third-person camera pulls in under it instead of poking through.
            Part(PrimitiveType.Cube, t, new Vector3(0, storey - .1f, 0), new Vector3(w - wall, .15f, d - wall), Tint(art.timber, new Color(.3f, .22f, .15f))).AddComponent<BoxCollider>();
            for (float x = -w / 2 + 1.2f; x < w / 2; x += 1.5f) Part(PrimitiveType.Cube, t, new Vector3(x, storey - .25f, 0), new Vector3(.18f, .2f, d - wall), dark); // ceiling beams
            // Windows glow on both faces (the glass passes through the wall). A kitchen lean-to on the back wall takes the place
            // of the ground-floor windows it would cover; the upstairs ones look out over its roof. The upper front is the
            // jetty's, with windows of its own (InnFront).
            foreach (int sz in new[] { -1, 1 })
                for (float x = -w / 2 + 1.5f; x < w / 2 - .8f; x += 2.6f)
                {
                    if (sz < 0 && Mathf.Abs(x) < doorW) continue;
                    bool covered = sz > 0 && kitchen.HasValue && Mathf.Abs(x - kitchen.Value) < KitchenSize.x / 2 + .85f;   // its glass, frame or shutters would meet the lean-to
                    if (!covered) Part(PrimitiveType.Cube, t, new Vector3(x, 1.5f, sz * d / 2), new Vector3(.9f, .8f, wall + .1f), art.glass);
                    if (sz > 0) Part(PrimitiveType.Cube, t, new Vector3(x, storey + 1.3f, sz * d / 2), new Vector3(.8f, .7f, wall + .1f), art.glass);
                    // Frames and sills round that glass on the outer wall face; shutters downstairs where they clear the corner post (inner face at w/2 - .17) and the door opening.
                    if (!covered) Window(t, new Vector3(x, 1.5f, sz * (d / 2 + wall / 2)), .9f, .8f, sz, variant, Mathf.Abs(x) + .84f <= w / 2 - .17f && (sz > 0 || Mathf.Abs(x) - .84f >= doorW / 2 + .05f), false);
                    if (sz > 0) Window(t, new Vector3(x, storey + 1.3f, sz * (d / 2 + wall / 2)), .8f, .7f, sz, variant, false, false);
                }
            float roofH = Mathf.Max(2.4f, d * .5f);
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH, .25f, .6f - wall / 2), t, new Vector3(0, H, 0), variant % 2 == 0 ? art.thatch : art.slate);
            Eaves(t, w, d, H, roofH, variant % 2 == 0 ? art.thatch : art.slate);
            Gables(t, w / 2 + wall / 2, d + wall, H, roofH, d / 2 + .7f, w / 2 + .6f, plaster, 2, .22f);   // the end walls' outer faces, out to the front and back walls' faces
            if (variant == 1) CrackedHearth(t, w, H, roofH);   // the Cracked Hearth: its chimney breast split, the fire showing through
            else
            {
                BoxPart(t, new Vector3(w / 2 - 1.1f, H + roofH * .75f, d * .15f), new Vector3(1, roofH * 1.1f, 1), Masonry, null, 1);
                BoxPart(t, new Vector3(w / 2 - 1.1f, H + roofH * 1.3f + .04f, d * .15f), new Vector3(1.2f, .12f, 1.2f), Tint(Masonry, new Color(.45f, .44f, .4f)), null, 1);   // its cap
            }
            // The door: a plank door on a hinge at the left jamb, on a stone threshold (with steps down if the ground falls away).
            var hinge = new GameObject("Door hinge").transform; hinge.SetParent(t, false); hinge.localPosition = new Vector3(-doorW / 2, 0, -d / 2 - .02f);
            var door = PlankDoor(hinge, new Vector3(doorW / 2, doorH / 2, 0), doorW, doorH, dark, .1f);
            var doorCollider = door.AddComponent<BoxCollider>();
            DoorSteps(t, 0, -d / 2 - wall / 2, .05f, 2, wall, footing);
            var innDoor = new ZoneDoor { name = t.name, kind = "inn", openable = true, hinge = hinge, blocker = doorCollider, position = t.TransformPoint(new Vector3(0, 1, -d / 2)) };
            Doors.Add(innDoor); innDoor.SetOpen(true);   // the inn keeps its door open; villagers come and go
            if (art.particle != null && variant != 1) Smoke(t, new Vector3(w / 2 - 1.1f, H + roofH * 1.4f, d * .15f));
            // Inside: bar counter with barrels behind it, three tables with stools, and the hearth.
            var bar = Part(PrimitiveType.Cube, t, new Vector3(-w / 2 + 2.6f, .55f, d / 2 - 1.5f), new Vector3(4, 1.1f, .7f), boards);
            bar.AddComponent<BoxCollider>(); bar.AddComponent<NavBlocker>();
            Part(PrimitiveType.Cube, t, new Vector3(-w / 2 + 2.6f, 1.12f, d / 2 - 1.5f), new Vector3(4.2f, .08f, .9f), dark);
            for (int i = 0; i < 3; i++) Part(PrimitiveType.Cylinder, t, new Vector3(-w / 2 + 1.2f + i * 1.1f, .5f, d / 2 - .55f), new Vector3(.8f, .5f, .8f), art.timber);
            // The innkeeper's place at the open end of the bar, and behind it the door to the rooms upstairs (barred to the player:
            // whoever lodges at the inn goes to bed through it), set between two of the back wall's windows. With a kitchen on the
            // back wall, the kitchen's own door shows in the taproom too.
            Workplace(t, "bar", new Vector3(-w / 2 + 5.3f, 0, d / 2 - 1.4f), new Vector3(0, 1, -1));
            float roomsX = -w / 2 + 5.4f;
            InnerDoor(t, roomsX, d / 2 - wall / 2, dark);
            Doors.Add(new ZoneDoor { name = t.name + ", upstairs", kind = "rooms", openable = false, position = t.TransformPoint(new Vector3(roomsX, 1, d / 2 - .7f)) });
            if (kitchen.HasValue) InnerDoor(t, kitchen.Value - KitchenDoorX, d / 2 - wall / 2, dark);
            foreach (var at in new[] { new Vector2(2.2f, -1.6f), new Vector2(2.8f, 1.3f), new Vector2(-2.8f, -1.9f) })
            {
                Part(PrimitiveType.Cylinder, t, new Vector3(at.x, .78f, at.y), new Vector3(1.2f, .04f, 1.2f), boards);
                Part(PrimitiveType.Cylinder, t, new Vector3(at.x, .4f, at.y), new Vector3(.18f, .38f, .18f), dark);
                for (int k = 0; k < 3; k++)
                {
                    float a = k * 120 * Mathf.Deg2Rad;
                    Part(PrimitiveType.Cylinder, t, new Vector3(at.x + Mathf.Cos(a) * .95f, .25f, at.y + Mathf.Sin(a) * .95f), new Vector3(.4f, .25f, .4f), dark);
                }
                Part(PrimitiveType.Cylinder, t, new Vector3(at.x, .88f, at.y), new Vector3(.12f, .08f, .12f), art.glass);   // candle
            }
            var hearth = BoxPart(t, new Vector3(w / 2 - .6f, 1, .8f), new Vector3(.9f, 2, 2.4f), Masonry, null, 1);
            hearth.AddComponent<BoxCollider>(); hearth.AddComponent<NavBlocker>();
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 - 1.02f, .55f, .8f), new Vector3(.1f, .8f, 1.4f), art.glass);   // embers
            var fire = new GameObject("Hearth fire").AddComponent<Light>(); fire.transform.SetParent(t, false);
            fire.transform.localPosition = new Vector3(w / 2 - 1.6f, 1, .8f); fire.type = LightType.Point; fire.range = 9; fire.intensity = 1.8f; fire.color = new Color(1, .55f, .25f);
            AddStation("fire", t.name, Ground(new Vector2(fire.transform.position.x, fire.transform.position.z)), t);   // the hearth is a fire to cook at, named after the inn
            StationRoom(t, new Rect(-w / 2, -d / 2, w, d));   // worked from the taproom, not from the street or the kitchen through a wall
            var room = new GameObject("Taproom light").AddComponent<Light>(); room.transform.SetParent(t, false);
            room.transform.localPosition = new Vector3(-1, storey - .6f, 0); room.type = LightType.Point; room.range = 8; room.intensity = 1.1f; room.color = new Color(1, .78f, .5f);
            // Outside: the framing, the jetty, the porch with its lantern, the sign, window boxes, a bench and barrels (InnFront).
            InnFront(t, w, d, H, storey, doorW, doorH, roofH, variant, kitchen, plaster, variant % 2 == 0 ? art.thatch : art.slate);
            var glow = new GameObject("Inn lantern").AddComponent<Light>(); glow.transform.SetParent(t, false);
            glow.transform.localPosition = new Vector3(0, 2.2f, -d / 2 - 1.2f); glow.type = LightType.Point; glow.range = 8; glow.intensity = 1.4f; glow.color = new Color(1, .72f, .4f);
            NightLights.Add(new NightLight { light = glow, dayIntensity = .6f, nightIntensity = 2.2f });   // faint by day: at 1.4 it bloomed to a pale blob on the plaster
        }
        /// <summary>
        /// The Cracked Hearth (inn variant 1): a stone chimney breast on the hearth gable (+X), split from its glowing ash-pit
        /// door up to the eaves with the fire showing through, and the stack above knocked askew on a glowing seam.
        /// </summary>
        void CrackedHearth(Transform t, float w, float H, float roofH)
        {
            var stone = Dressed(new Color(.42f, .4f, .37f)); float face = w / 2 + 1.05f, z = .8f, low = H * .62f;
            var breast = BoxPart(t, new Vector3(w / 2 + .6f, low / 2, z), new Vector3(.9f, low, 2.2f), stone, null, 1);
            var solid = breast.AddComponent<BoxCollider>(); solid.center = Vector3.zero; solid.size = new Vector3(.9f, low, 2.2f); breast.AddComponent<NavBlocker>();
            BoxPart(t, new Vector3(w / 2 + .65f, (low + H + .4f) / 2, z), new Vector3(.8f, H + .4f - low, 1.2f), stone, null, 1);
            Part(PrimitiveType.Cube, t, new Vector3(face, .42f, z), new Vector3(.04f, .45f, .55f), art.glass);   // the ash-pit door, glowing
            // The crack zig-zags up the outer face from the ash-pit to the seam (p: height, depth).
            var p = new Vector2(.5f, z - .05f);
            for (int i = 0; i < 6; i++)
            {
                float a = i % 2 == 0 ? 20 : -20; var step = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * 1.1f;
                Part(PrimitiveType.Cube, t, new Vector3(face + .005f, p.x + step.x / 2, p.y + step.y / 2), new Vector3(.05f, 1.16f, .2f), art.glass, Quaternion.Euler(a, 0, 0));
                p += step;
            }
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 + .65f, H + .4f, z), new Vector3(.84f, .08f, 1.24f), art.glass);
            var stack = new GameObject("Chimney stack").transform; stack.SetParent(t, false);
            stack.localPosition = new Vector3(w / 2 + .72f, H + .44f, z); stack.localRotation = Quaternion.Euler(0, 0, -5);   // leaning away from the house
            float tall = roofH + 1.2f;
            BoxPart(stack, new Vector3(0, tall / 2, 0), new Vector3(.8f, tall, 1.2f), stone, null, 1);
            BoxPart(stack, new Vector3(0, tall + .1f, 0), new Vector3(1, .2f, 1.4f), stone, null, 1);
            if (art.particle != null) Smoke(stack, new Vector3(0, tall + .4f, 0));
            // Stones fallen from the crack.
            BoxPart(t, new Vector3(face + .5f, .1f, z + .9f), new Vector3(.4f, .25f, .3f), stone, Quaternion.Euler(0, 30, 0));
            BoxPart(t, new Vector3(face + .35f, .08f, z - .7f), new Vector3(.3f, .2f, .35f), stone, Quaternion.Euler(0, -20, 0));
        }
        void Barn(Transform t, Vector2 size)
        {
            float w = size.x, d = size.y, h = 4.2f;
            var boards = Tint(art.timber, new Color(.4f, .25f, .16f));
            BoxPart(t, new Vector3(0, h / 2, 0), new Vector3(w, h, d), boards);
            Footing(t, w + .3f, d + .3f, .45f, .4f, 1, 1.75f);   // a stone sill round the boards' foot, stepping up and down the slope, open at the doors
            for (float x = -w / 2; x <= w / 2 + .01f; x += 1.2f)
                foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(x, h / 2, sz * (d / 2 + .04f)), new Vector3(.12f, h, .08f), art.timber);
            // The doors in a frame of two posts and a lintel, braced corner to corner, down to the highest ground across the
            // doorway (the grass never cuts them), on a stone threshold down to the lowest, with steps if the ground falls away.
            float sill = Mathf.Clamp(DoorGround(t, 0, -d / 2 - .3f, 3.2f) + .03f, -1, .6f), tall = 3.2f - sill, mid = (3.2f + sill) / 2;
            float brace = Mathf.Atan2(tall, 3.2f) * Mathf.Rad2Deg, across = Mathf.Sqrt(3.2f * 3.2f + tall * tall) - .3f;
            Part(PrimitiveType.Cube, t, new Vector3(0, mid, -d / 2 - .07f), new Vector3(3.2f, tall, .1f), Tint(art.timber, new Color(.18f, .12f, .09f)));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(0, mid, -d / 2 - .12f), new Vector3(across, .2f, .06f), art.timber, Quaternion.Euler(0, 0, s * brace));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * 1.72f, (sill + 3.4f) / 2, -d / 2 - .1f), new Vector3(.22f, 3.4f - sill, .2f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(0, 3.32f, -d / 2 - .1f), new Vector3(3.66f, .2f, .2f), art.timber);
            DoorSteps(t, 0, -d / 2 - .15f, sill, 3.44f);
            float roofH = d * .5f;
            MeshPart(ZoneMeshes.GableRoof(w + 1, d + 1.4f, roofH, .25f, .5f), t, new Vector3(0, h, 0), art.thatch);
            Eaves(t, w - .2f, d, h, roofH, art.thatch);   // the barn's roof is 1 m wider than its walls, not 1.2
            Gables(t, w / 2, d, h, roofH, d / 2 + .7f, w / 2 + .5f, boards, d, .2f);   // boards to the ridge: the wall below is a cube, one tile across its width
            Solid(t, new Vector3(0, (h + roofH) / 2, 0), new Vector3(w + .3f, h + roofH, d + .3f));
        }
        /// <summary>
        /// A village well: a round ring of dressed stone on a plinth course under a coping, the water down in the shaft, and a
        /// roofed windlass on two posts with its rope, bucket and crank. variant 1: a blood-stone well (Khaven), reddened stone
        /// and dark water.
        /// </summary>
        void Well(Transform t, int variant = 0)
        {
            var dark = Tint(art.timber, new Color(.26f, .18f, .12f)); var rope = Tint(art.hay, new Color(.58f, .5f, .34f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            var ring = PropMesh("Well ring", () => Turned(new[] { new Vector2(1.2f, -.3f), new Vector2(1.2f, .16f), new Vector2(1.1f, .22f), new Vector2(1.08f, .72f), new Vector2(1.2f, .76f), new Vector2(1.2f, .9f), new Vector2(1.15f, .94f), new Vector2(.84f, .94f), new Vector2(.8f, .9f), new Vector2(.8f, .2f) }, 20, 1.5f));
            MeshPart(ring, t, Vector3.zero, variant == 1 ? Tint(Masonry, new Color(.5f, .32f, .3f)) : Masonry);
            var water = art.water; if (variant == 1) { water = new Material(art.water); water.color = new Color(.28f, .06f, .05f, .9f); }
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .6f, 0), new Vector3(1.62f, .02f, 1.62f), water);   // down in the shaft
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * 1, 1.55f, 0), new Vector3(.22f, 2.4f, .22f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.68f, 0), new Vector3(2.5f, .14f, .2f), dark);         // tie beam, its ends past the posts
            MeshPart(ZoneMeshes.GableRoof(2.8f, 2.2f, 1f, .1f), t, new Vector3(0, 2.75f, 0), art.slate, Quaternion.Euler(0, 90, 0));   // on posts, no wall under it: closed ends
            foreach (int s in new[] { -1, 1 }) MeshPart(PropMesh("Well gable", () => ZoneMeshes.Gable(2.2f, 1, .04f, 1)), t, new Vector3(0, 2.75f, s * 1.41f), dark);   // boarded over: 3 cm proud of each end
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 1.9f, 0), new Vector3(.24f, .89f, .24f), art.timber, Quaternion.Euler(0, 0, 90));   // windlass drum
            Part(PrimitiveType.Cylinder, t, new Vector3(.2f, 1.9f, 0), new Vector3(.33f, .15f, .33f), rope, Quaternion.Euler(0, 0, 90));       // the rope wound on it
            Rod(t, new Vector3(.2f, 1.78f, 0), new Vector3(.2f, 1.36f, 0), .04f, rope);
            MeshPart(PropMesh("Bucket", () => Turned(new[] { new Vector2(0, 0), new Vector2(.15f, 0), new Vector2(.2f, .36f), new Vector2(.175f, .36f), new Vector2(.13f, .04f), new Vector2(0, .04f) }, 10)), t, new Vector3(.2f, 1, 0), Tint(art.timber, new Color(.42f, .3f, .19f)));
            Rod(t, new Vector3(.01f, 1.36f, 0), new Vector3(.39f, 1.36f, 0), .03f, iron);                      // the bucket's bail
            Part(PrimitiveType.Cube, t, new Vector3(1.14f, 1.75f, 0), new Vector3(.05f, .38f, .06f), iron);    // crank
            Rod(t, new Vector3(1.14f, 1.6f, 0), new Vector3(1.36f, 1.6f, 0), .045f, dark);
            Solid(t, new Vector3(0, 1.2f, 0), new Vector3(2.4f, 2.4f, 2.4f));
        }
        /// <summary>
        /// A dead tree (Khaven's Whispering Wood, the Rim's husks, the unmade): a tapered, slightly bent bole on a root flare of
        /// buttress ridges (Bole), ending in a broken leader; three to five limbs leaving it at different heights, each crooked (a
        /// kink part way) and tapering to a point, with branches off its length, not its tip, and twigs off those. Grey weathered
        /// wood, no bark grain in the shade. Its look is its own (TreeRandom); from the zone's stream it takes exactly the draws the
        /// old tree took, so nothing else moves. <paramref name="girth"/> &gt; .6 (a dead_oak prop) keeps a massive trunk.
        /// </summary>
        Transform DeadTree(Vector2 at, float scale, Material mat, int depth, Transform parent, Transform existing = null, float girth = -1)
        {
            var root = existing != null ? existing : new GameObject("Dead tree").transform;
            if (existing == null) { root.SetParent(parent, false); root.position = Ground(at); root.rotation = Quaternion.Euler(0, R01 * 360, 0); }
            if (girth < 0) girth = existing != null && depth >= 5 ? .9f : .38f;
            float trunkH = (girth > .6f ? 4.2f : 5.2f) * scale, trunkR = girth * scale;
            for (int i = 0; i < 5; i++) _ = R01;                                           // the old root flare's draws
            SkipBranchDraws(depth, trunkR * (girth > .6f ? .75f : .7f));                     // and the old limbs'
            var tr = TreeRandom(root.position); float T() { return (float)tr.NextDouble(); }
            bool massive = girth > .6f;
            // A massive one (a dead_oak prop) is a great old tree: tall, a broad but not squat bole, long heavy limbs.
            float top = (massive ? trunkH * 2.1f : trunkH * 1.35f) * (.9f + T() * .2f), r0 = massive ? trunkR * .66f : Mathf.Max(.14f, trunkR * .8f);
            var axis = Bole(root, tr, top, r0, mat, .45f);   // a snapped-off top
            // Playtest 2026-10-03 ("this tree is still broke"): the limbs were long thin spears, all climbing, so the tree read as an
            // antler or a broom. A dead oak's limbs are short for its height, heavy where they leave the bole, crooked in three
            // lengths that each turn a little, and end in snapped stubs; forks off every length, some drooping, each forked again.
            int n = (massive ? 5 : 4) + (int)(T() * 2); float turn = T() * 360;
            for (int i = 0; i < n; i++)
            {
                // Up the trunk from about a third to four fifths, lowest first: the low limbs heavy and near level, the upper ones shorter and climbing.
                float u = .32f + .48f * (i + T() * .6f) / n, yaw = turn + i * 360f / n + (T() - .5f) * 70;
                var outward = Quaternion.Euler(0, yaw, 0) * Vector3.right;
                float reach = top * (massive ? .32f + T() * .18f : .32f + T() * .2f) * (1.3f - u * .6f);
                float r = r0 * (massive ? .5f + T() * .14f : .5f + T() * .16f) * (1.2f - u * .45f);
                var from = axis(top * u); var dir = (outward + Vector3.up * (.05f + u * .4f + T() * .3f)).normalized;
                float segLen = reach * (.45f + T() * .1f);
                for (int seg = 0; seg < 3; seg++)
                {
                    var to = from + dir * segLen; float bow = .06f + T() * .06f, rEnd = r * (seg == 2 ? .35f : .6f);
                    Limb(root, from, to, r, rEnd, mat, bow, 8, seg == 0, seg == 2);
                    // Forks off this length (on its bowed line), turning their own way, some drooping; most fork once more.
                    int subs = seg == 2 ? 1 : 1 + (int)(T() * 2);
                    for (int k = 0; k < subs; k++)
                    {
                        float s2 = .35f + T() * .5f; var at2 = LimbAt(from, to, bow, s2); float rAt = Mathf.Lerp(r, rEnd, s2) * .6f;
                        var d2 = (Quaternion.AngleAxis((T() - .5f) * 110, Vector3.up) * dir + Vector3.up * (T() * .9f - .25f)).normalized;
                        float len = segLen * (.5f + T() * .6f); var tip = at2 + d2 * len; float bb = .08f + T() * .08f;
                        Limb(root, at2, tip, rAt, rAt * .3f, mat, bb, 6);
                        if (T() < .7f)
                        {
                            var at3 = LimbAt(at2, tip, bb, .5f + T() * .3f); float rs = rAt * .45f;
                            var d3 = (Quaternion.AngleAxis((T() - .5f) * 120, Vector3.up) * d2 + Vector3.up * (T() * .8f - .2f)).normalized;
                            Limb(root, at3, at3 + d3 * len * (.4f + T() * .3f), rs, rs * .3f, mat, .1f, 5);
                        }
                    }
                    // The next length: a kink in plan and in pitch, thinner and shorter.
                    from = to; r = rEnd; segLen *= .75f;
                    dir = (Quaternion.AngleAxis((T() - .5f) * 60, Vector3.up) * dir + Vector3.up * ((T() - .5f) * .5f + .15f)).normalized;
                }
            }
            // The leader's broken top: a last short twig or two just under the snag.
            for (int k = 0, m = 1 + (int)(T() * 2); k < m; k++)
            {
                var from = axis(top * (.86f + T() * .06f)); var dir = (Quaternion.Euler(0, T() * 360, 0) * Vector3.right + Vector3.up * (.6f + T() * .8f)).normalized;
                Limb(root, from, from + dir * top * (.08f + T() * .06f), r0 * .26f, r0 * .07f, mat, .08f, 5);
            }
            var cap = root.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, top / 2, 0); cap.height = top; cap.radius = r0;
            root.gameObject.AddComponent<NavBlocker>(); root.gameObject.AddComponent<TreeFade>();
            return root;
        }
        /// <summary>Bark for dead woods: charred dark in the ash, weathered grey elsewhere (Khaven's Whispering Wood). One draw.</summary>
        Material DeadBark() { float t = R01; return Tint(art.bark, Zone.biome == "ash" ? Color.Lerp(new Color(.2f, .18f, .16f), new Color(.3f, .27f, .24f), t) : Color.Lerp(new Color(.58f, .57f, .56f), new Color(.72f, .71f, .69f), t)); }
        static readonly Color[] Leaf = { new Color(.33f, .38f, .17f), new Color(.55f, .42f, .16f), new Color(.52f, .27f, .13f), new Color(.40f, .36f, .15f) };
        bool Gloom { get { return Zone.biome == "gloom"; } }
        /// <summary>Gloom (Khaven): living colour drained toward a dull grey, so woods and brush read withered rather than autumnal.</summary>
        Color Wither(Color c) { if (!Gloom) return c; float g = c.grayscale * .8f; return Color.Lerp(c, new Color(g, g, g, c.a), .6f); }
        /// <summary>Broadleaf trees (their positions drive where autumn leaves fall).</summary>
        public readonly List<Vector3> LeafTrees = new List<Vector3>();
        /// <summary>Tall dense grass: the zone's own patches plus a patch around every ambush camp for its hunters to hide in.</summary>
        List<(Vector2 center, float radius)> TallGrassPatches()
        {
            var list = new List<(Vector2, float)>();
            if (Zone.tallGrass != null) foreach (var c in Zone.tallGrass) if (c != null) list.Add((c.center, c.radius));
            if (Zone.camps != null) foreach (var c in Zone.camps) if (c != null && c.ambush) list.Add((c.center, c.radius + 4));
            return list;
        }
        Mesh[] canopies, boulders, crags;
        Mesh Canopy() { if (canopies == null) { canopies = new Mesh[6]; for (int i = 0; i < 6; i++) canopies[i] = ZoneMeshes.Blob(Zone.seed + i * 17, .55f, false); } return canopies[(int)(R01 * 6) % 6]; }
        /// <summary>A boulder blob picked with one zone draw (the draw is taken before the cache fills, as it always was).</summary>
        Mesh Boulder() { return BoulderAt((int)(R01 * 6)); }
        /// <summary>A boulder blob by index, no zone draw: for a prop that picks with its own stream.</summary>
        Mesh BoulderAt(int i) { if (boulders == null) { boulders = new Mesh[6]; for (int k = 0; k < 6; k++) boulders[k] = ZoneMeshes.Blob(Zone.seed + 400 + k * 23, 1.1f, true, 8, 12); } return boulders[((i % 6) + 6) % 6]; }
        /// <summary>A crag's rock lump by index, no zone draw: a faceted blob, lumpier than a boulder, its stone tiled about 2 m on a 5 x 9 m lump.</summary>
        Mesh CragRock(int i) { if (crags == null) { crags = new Mesh[6]; for (int k = 0; k < 6; k++) crags[k] = ZoneMeshes.Blob(Zone.seed + 700 + k * 31, 1.25f, true, 9, 14, 6, 4, true); } return crags[((i % 6) + 6) % 6]; }
        GameObject Lump(Mesh mesh, Transform parent, Vector3 at, Vector3 scale, Material m, float yaw)
        {
            var o = MeshPart(mesh, parent, at, m, Quaternion.Euler(0, yaw, 0)); o.transform.localScale = scale; return o;
        }
        void Broadleaf(Transform t, int variant)
        {
            LeafTrees.Add(t.position);
            float h = 3.5f + R01 * 1.5f;
            float yawA = R01 * 120, yawB = R01 * 120 + 180;   // the two main limbs' bearings, on roughly opposite sides (the zone's draws, as before)
            int family = (Mathf.Abs(variant) + (int)(R01 * 2)) % Leaf.Length;   // its leaf: fresh green, yellow-green, autumn or dull gold (the same draw as before)
            // A tapered, slightly bent and leaning trunk on a root flare, in its own shade of bark, running up into the crown's
            // heart; limbs grow out of it toward the crown's side clusters. Its look comes from its own stream (TreeRandom).
            var tr = TreeRandom(t.position); float T() { return (float)tr.NextDouble(); }
            var bark = BarkShade(T()); float r0 = .24f + T() * .07f;
            var axis = Bole(t, tr, h + .7f, r0, bark); var lean = axis(h) - new Vector3(0, h, 0);   // the crown sits over the leaning top
            // Where the crown's clumps stood (the zone's draws, exactly as before; no blob is built now): each becomes a cluster of
            // painted leaf cards fanning out from that spot (LeafCrown). The first is the crown's top, the rest its sides.
            int clumps = 5 + (int)(R01 * 3); var sides = new List<Vector3>(); var spots = new List<(Vector3 at, float size, bool light)>();
            for (int i = 0; i < clumps; i++)
            {
                float a = i * 2.4f + R01, r = i == 0 ? 0 : 1 + R01 * .9f, s = (i == 0 ? 3.4f : 2.2f + R01 * 1.1f);
                _ = Canopy(); var at = new Vector3(Mathf.Cos(a) * r, h + .9f + (i == 0 ? .8f : R01 * 1.2f), Mathf.Sin(a) * r) + lean;
                _ = R01;   // the blob's yaw
                spots.Add((at, s, i % 3 == 1));
                if (i > 0) sides.Add(at);
            }
            // Two limbs (a third on some trees) from inside the trunk, bowing up and out to the side cluster nearest their bearing.
            var bearings = T() < .55f ? new[] { yawA, yawB, yawA + 70 + T() * 40 } : new[] { yawA, yawB };
            var ends = new List<Vector3> { axis(h + .4f) };   // where the crown's boughs grow from: the trunk's top and each limb's end
            foreach (float yaw in bearings)
            {
                var want = Quaternion.Euler(0, yaw, 0) * Vector3.right; var to = sides[0];
                foreach (var c in sides) if (Vector3.Dot(new Vector3(c.x, 0, c.z).normalized, want) > Vector3.Dot(new Vector3(to.x, 0, to.z).normalized, want)) to = c;
                var from = axis(h * (.56f + T() * .12f)); var end = to - new Vector3(0, .3f, 0) + want * .15f;
                Limb(t, from, end, r0 * (.44f + T() * .1f), .06f, bark);
                ends.Add(end);
            }
            // A full crown: a leaf-coloured lump at every clump spot, in the family's own deep, mid and light (the Great Oak's way:
            // a billowed mass, never a brown ball), three cards a cluster as its ragged fringe, ten to fourteen clusters, three or
            // four lying under it, and a small core in the family's shade at the heart, so from below it is leaf mass, not sky.
            var core = Tint(art.foliage, Wither(Color.Lerp(LeafMass[family][0], new Color(.14f, .22f, .1f), .5f)));
            LeafCrown(t, tr, lean + new Vector3(0, h + 1.5f, 0), spots, ends, LeafMaterial(family), bark, core, 1.5f, new Vector3(2.6f, 1.5f, 2.6f), 3, 10, 14, 3, 4, LeafMassOf(family), .95f);
            var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, h / 2, 0); cap.height = h; cap.radius = .35f;
            t.gameObject.AddComponent<NavBlocker>(); t.gameObject.AddComponent<TreeFade>();
        }
        /// <summary>
        /// A crown of painted leaf cards (Crulanda/Leaf) over a trunk. At every clump spot a cluster of crossed cards (two, or three
        /// with <paramref name="cardsPer"/> 3) fans out from a short bough that grows in from the nearest limb end or the trunk's top;
        /// a few small clusters hang low round the rim between them, so the underside is ragged leaf, not a shelf; and a dark squashed
        /// core sits at the heart, so the crown is never hollow from below. A big crown asks for more: clusters spread round and
        /// through it until it has <paramref name="crownMin"/> to <paramref name="crownMax"/>, and <paramref name="underMin"/> to
        /// <paramref name="underMax"/> low clusters lying inward along its underside, so looking up meets leaf, not sky between
        /// cut-outs (all off by default: the orchard's trees keep their counts). The cards' union covers about the volume the clumps
        /// had (a fan is centred a little past its spot, reaching where the clump's rim was), so a tree's bounds, fade and collider
        /// are as before. One mesh for the cards and one for the boughs. Draws only from <paramref name="tr"/>, the tree's own
        /// stream, after its trunk and limbs, the extras' draws after the ones a plain crown takes: the zone's is untouched.
        /// With <paramref name="mass"/> (deep, mid and light leaf, LeafMassOf) the crown has a body: a rounded lump at every clump
        /// spot, <paramref name="massSize"/> of the clump across and drawn a fifth of the way in to the heart, so they run together
        /// into one billowed mass in the leaf's own colour and the cards (<paramref name="cardSize"/> of the clump) are its
        /// ragged edge. The top and the light spots take the light leaf, the low ones the deep. Drawn last of all from the stream.
        /// </summary>
        void LeafCrown(Transform t, System.Random tr, Vector3 heart, List<(Vector3 at, float size, bool light)> spots, List<Vector3> ends, Material leaves, Material bark, Material core, float rim, Vector3 coreSize,
            int cardsPer = 2, int crownMin = 0, int crownMax = 0, int underMin = 0, int underMax = 0, Material[] mass = null, float cardSize = .8f, float massSize = .68f)
        {
            float T() { return (float)tr.NextDouble(); }
            var cards = new ZoneMeshes.Cards(); var boughs = new List<CombineInstance>();
            Color Shade(int level, float u)   // a deep (cool), mid or light (warm) cluster, with a little variation: the vertex tint
            {
                var c = level == 0 ? new Color(.68f, .74f, .66f) : level == 1 ? new Color(.86f, .9f, .84f) : new Color(1, 1, .95f);
                return c * (.94f + u * .12f);
            }
            void Cluster(Vector3 at, float size, Color shade, bool bough, bool under = false)
            {
                if (under)
                {
                    // Under the crown: the fan lies along the underside from out at the rim in toward the heart, near flat, so its
                    // first card faces down, its second hangs and its third is tilted: looking up meets leaf.
                    var inward = heart - at; inward.y = 0;
                    var axis = (inward.normalized + Vector3.up * ((T() - .5f) * .3f)).normalized;
                    var flat = Quaternion.AngleAxis((T() - .5f) * 40, axis) * Vector3.Cross(axis, Vector3.up).normalized;
                    cards.AddCross(at, axis, flat, size * (.95f + T() * .2f), size * (.85f + T() * .25f), heart, .25f, shade, cardsPer);
                    return;
                }
                // The fan's axis: out from the heart and lifted; one over the heart leans over instead of standing (a vertical cross
                // is a line from above). Rolled at random about that axis, then its crossed cards.
                var outward = at - heart; outward.y = 0;
                var along = (outward.sqrMagnitude < .04f ? Quaternion.Euler(0, T() * 360, 0) * Vector3.right * .9f : outward.normalized * .8f) + Vector3.up * (.4f + T() * .4f);
                along = (Quaternion.Euler((T() - .5f) * 30, 0, (T() - .5f) * 30) * along).normalized;
                var across = Vector3.Cross(along, Vector3.up); if (across.sqrMagnitude < .01f) across = Vector3.right;
                across = Quaternion.AngleAxis(T() * 180, along) * across.normalized;
                float len = size * (.95f + T() * .2f), wide = size * (.85f + T() * .25f);
                var foot = at - along * len * .42f;
                cards.AddCross(foot, along, across, len, wide, heart, .25f, shade, cardsPer);
                if (!bough) return;
                // Its bough: from the nearest limb end or the trunk's top in to the foot of the fan; thin, bowed a little, closed at the tip.
                var from = ends[0]; foreach (var e in ends) if ((e - foot).sqrMagnitude < (from - foot).sqrMagnitude) from = e;
                var d = foot - from; float l = d.magnitude; if (l < .2f) return;
                var side = Vector3.Cross(d / l, Vector3.up); if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
                Vector3 C(float s) { return from + d * s + Vector3.up * (Mathf.Sin(s * Mathf.PI) * l * .05f); }
                float R(float s, float a) { return Mathf.Lerp(.055f, .022f, s) * Mathf.Clamp01((1.05f - s) / .1f); }
                boughs.Add(new CombineInstance { mesh = ZoneMeshes.Tube(C, R, new[] { 0, .3f, .6f, .85f, 1.05f }, 5, side.normalized, 1, l * .5f), transform = Matrix4x4.identity });
            }
            foreach (var spot in spots) Cluster(spot.at, spot.size * cardSize, Shade(spot.light ? 2 : 1, T()), true);
            // Low fillers round the rim between the side clusters: small, deep in shade, on no bough (they hang among the others).
            int fill = 2 + (int)(T() * 2);
            for (int k = 0; k < fill; k++) { float a = T() * Mathf.PI * 2; Cluster(heart + new Vector3(Mathf.Cos(a) * rim, -.9f + T() * .5f, Mathf.Sin(a) * rim), rim * 1.2f + T() * .4f, Shade(0, T()), false); }
            // A big crown's extra clusters: spread round and through it, on boughs of their own, until it has its count.
            if (crownMax > 0)
            {
                int more = crownMin + (int)(T() * (crownMax - crownMin + 1)) - spots.Count;
                for (int k = 0; k < more; k++)
                {
                    float a = T() * Mathf.PI * 2, r = rim * (.4f + T());
                    Cluster(heart + new Vector3(Mathf.Cos(a) * r, -.5f + T() * 1.5f, Mathf.Sin(a) * r), rim * 1.2f + T() * .8f, Shade(k % 3 == 1 ? 2 : 1, T()), true);
                }
            }
            // Under it: low clusters spaced round the underside, lying inward, deep in shade, on no bough.
            if (underMax > 0)
            {
                int under = underMin + (int)(T() * (underMax - underMin + 1));
                for (int k = 0; k < under; k++)
                {
                    float a = (k + T() * .7f) * Mathf.PI * 2 / under, r = rim * (.7f + T() * .6f);
                    Cluster(heart + new Vector3(Mathf.Cos(a) * r, -1.2f + T() * .5f, Mathf.Sin(a) * r), rim * 1.1f + T() * .5f, Shade(0, T()), false, true);
                }
            }
            MeshPart(cards.Build("Leaves"), t, Vector3.zero, leaves);
            if (boughs.Count > 0)
            {
                var mesh = new Mesh { name = "Boughs" }; mesh.CombineMeshes(boughs.ToArray(), true, false); mesh.RecalculateBounds();
                foreach (var piece in boughs) DestroyImmediate(piece.mesh);
                MeshPart(mesh, t, Vector3.zero, bark);
            }
            // The core: a dark squashed clump at the heart, seen only through the gaps and from below.
            var coreMesh = canopies != null ? canopies[(int)(T() * 6) % 6] : null;
            if (coreMesh != null) Lump(coreMesh, t, heart - new Vector3(0, .3f, 0), coreSize, core, T() * 360);
            // The leaf mass: a lump at every clump spot (the first is the crown's top).
            if (mass == null || mass.Length < 3) return;
            for (int i = 0; i < spots.Count; i++)
            {
                var spot = spots[i]; float wide = spot.size * (massSize + T() * .1f);
                var size = new Vector3(wide, spot.size * (massSize * .65f + T() * .08f), wide * (.92f + T() * .16f));
                Lump(LeafLumpAt((int)(T() * 6)), t, Vector3.Lerp(spot.at, heart, .2f), size, mass[i == 0 || spot.light ? 2 : spot.at.y < heart.y ? 0 : 1], T() * 360).name = "Leaf mass";
            }
        }
        /// <summary>A crown's leaf mass by leaf family (Leaf[]): deep, mid and light, taken from the family's painted card (fresh green,
        /// yellow-green, autumn orange, dull gold), so a crown's body is the colour of its leaves.</summary>
        static readonly Color[][] LeafMass = {
            new[] { new Color(.20f, .33f, .12f), new Color(.27f, .42f, .15f), new Color(.34f, .50f, .18f) },
            new[] { new Color(.33f, .38f, .11f), new Color(.46f, .48f, .13f), new Color(.56f, .56f, .16f) },
            new[] { new Color(.50f, .22f, .07f), new Color(.64f, .30f, .08f), new Color(.76f, .42f, .11f) },
            new[] { new Color(.34f, .34f, .11f), new Color(.45f, .44f, .13f), new Color(.54f, .52f, .16f) } };
        /// <summary>A leaf family's mass materials (deep, mid, light), greyed in gloom.</summary>
        Material[] LeafMassOf(int family) { var c = LeafMass[Mathf.Abs(family) % LeafMass.Length]; return new[] { Tint(art.foliage, Wither(c[0])), Tint(art.foliage, Wither(c[1])), Tint(art.foliage, Wither(c[2])) }; }
        Mesh[] leafLumps;
        /// <summary>A rounded leafy lump by index, no zone draw (crowns' masses, hedges, loose hay): a blob a little lumpier and lighter
        /// (8 x 12) than a canopy, since a crown takes five to seven.</summary>
        Mesh LeafLumpAt(int i) { if (leafLumps == null) { leafLumps = new Mesh[6]; for (int k = 0; k < 6; k++) leafLumps[k] = ZoneMeshes.Blob(Zone.seed + 950 + k * 19, .6f, false, 8, 12); } return leafLumps[((i % 6) + 6) % 6]; }
        /// <summary>
        /// A hedge along x: a row of overlapping rounded leafy lumps in two greens, smaller and lighter ones along its top and a few
        /// sprays of painted leaf standing out of it, inside the box it always was (<paramref name="length"/> x 1.4 x 1.2 m: its
        /// collider, unchanged). Its shape comes from a stream keyed to where it stands: the zone's draws are untouched.
        /// </summary>
        void Hedge(Transform t, float length)
        {
            var hr = TreeRandom(t.position); float H() { return (float)hr.NextDouble(); }
            var greens = new[] { Tint(art.foliage, Wither(new Color(.19f, .32f, .12f))), Tint(art.foliage, Wither(new Color(.25f, .39f, .14f))), Tint(art.foliage, Wither(new Color(.31f, .45f, .17f))) };
            bool cardArt = art.leafCards != null && art.leafCards.Length > 0; var cards = new ZoneMeshes.Cards();
            int n = Mathf.Max(2, Mathf.RoundToInt(length / .8f)); float step = Mathf.Max(0, length - 1.6f) / (n - 1);
            for (int i = 0; i < n; i++)
            {
                float x = -length / 2 + .8f + i * step, wide = 1.45f + H() * .3f, high = 1.3f + H() * .3f;
                if (length < 1.6f) x = 0;
                Lump(LeafLumpAt((int)(H() * 6)), t, new Vector3(x, high * .46f, (H() - .5f) * .14f), new Vector3(wide, high, 1.1f + H() * .2f), greens[i % 2], H() * 360).name = "Hedge";
                if (i % 2 == 0) continue;
                // Every other lump: a small light one on top, and a spray of leaf leaning out of it.
                Lump(LeafLumpAt((int)(H() * 6)), t, new Vector3(x + (H() - .5f) * .3f, 1.1f + H() * .12f, (H() - .5f) * .3f), new Vector3(.85f + H() * .25f, .6f, .8f + H() * .2f), greens[2], H() * 360).name = "Hedge";
                var along = new Vector3((H() - .5f) * .8f, 1, (H() - .5f) * 1.2f).normalized; var across = Quaternion.AngleAxis(H() * 180, along) * Vector3.Cross(along, Vector3.forward).normalized;
                float size = .6f + H() * .25f; var foot = new Vector3(x + (H() - .5f) * .4f, 1.02f, (H() - .5f) * .5f);
                if (cardArt) cards.AddCross(foot, along, across, size, size * .95f, new Vector3(x, .5f, 0), .3f, new Color(.8f, .86f, .78f) * (.9f + H() * .15f));
            }
            if (cards.Count > 0) MeshPart(cards.Build("Hedge leaves"), t, Vector3.zero, LeafMaterial(art.leafCards[0], new Color(.72f, .8f, .64f)));
            Solid(t, new Vector3(0, .7f, 0), new Vector3(length, 1.4f, 1.2f));
        }
        /// <summary>
        /// A hayrick: a round stack built up in four tiers, each standing a little proud of the one below (a flared skirt, two
        /// shoulders, a cap), in pale thatch whose painted layers run round it, with the stack pole out of its top and loose hay
        /// at its foot. About the size of the old round one (2.6 m across, a little over 2 m high); its collider is unchanged.
        /// Height, girth, turn and the pole's lean come from a stream keyed to where it stands: no zone draw.
        /// </summary>
        void Haystack(Transform t)
        {
            var hr = TreeRandom(t.position); float H() { return (float)hr.NextDouble(); }
            var rick = PropMesh("Hayrick", () => Turned(new[] { new Vector2(1.2f, -.35f), new Vector2(1.27f, .12f), new Vector2(1.16f, .62f), new Vector2(1.24f, .58f), new Vector2(1.04f, 1.12f), new Vector2(1.11f, 1.08f), new Vector2(.74f, 1.6f), new Vector2(.8f, 1.56f), new Vector2(.36f, 1.98f), new Vector2(0, 2.2f) }, 14, 2.5f));
            float girth = .95f + H() * .07f, high = .92f + H() * .16f;
            MeshPart(rick, t, Vector3.zero, Tint(art.thatch, new Color(.84f, .72f, .42f)), Quaternion.Euler(0, H() * 360, 0)).transform.localScale = new Vector3(girth, high, girth);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 2.2f * high + .12f, 0), new Vector3(.07f, .4f, .07f), Tint(art.timber, new Color(.3f, .21f, .13f)), Quaternion.Euler((H() - .5f) * 10, 0, (H() - .5f) * 10));   // the stack pole
            for (int k = 0; k < 3; k++)
            {
                float a = (k + H() * .7f) * 2.1f, x = Mathf.Cos(a) * 1.25f, z = Mathf.Sin(a) * 1.25f;
                Lump(LeafLumpAt(k), t, new Vector3(x, LocalGround(t, x, z) + .08f, z), new Vector3(.8f + H() * .3f, .34f, .6f + H() * .25f), art.hay, H() * 360).name = "Loose hay";
            }
            Solid(t, new Vector3(0, 1, 0), new Vector3(2.4f, 2, 2.4f));
        }
        /// <summary>The tint of each leaf family's painted cards (near white: the paint carries the colour), and which card each family takes
        /// (fresh green, yellow-green, autumn; dull gold is the yellow card in a duller tint).</summary>
        static readonly Color[] LeafTints = { new Color(1, 1, .94f), new Color(1, .97f, .84f), new Color(1, .92f, .86f), new Color(.9f, .86f, .7f) };
        static readonly int[] LeafCardOf = { 0, 1, 2, 1 };
        /// <summary>The painted leaf-card material for a leaf family (Leaf[]), greyed in gloom; the old flat foliage when the palette has no card art.</summary>
        Material LeafMaterial(int family)
        {
            var cards = art.leafCards; family = Mathf.Abs(family) % Leaf.Length;
            if (cards == null || cards.Length == 0) return Tint(art.foliage, Wither(Leaf[family]));
            return LeafMaterial(cards[LeafCardOf[family] % cards.Length], LeafTints[family]);
        }
        /// <summary>A painted leaf-card material in a tint; in gloom the paint itself is greyed too (a tint alone can't drain a painted green).</summary>
        Material LeafMaterial(Material card, Color tint)
        {
            var m = Tint(card, Wither(tint));
            if (Gloom && m.HasProperty("_Wither")) m.SetFloat("_Wither", .7f);
            return m;
        }
        /// <summary>A pine's painted bough material in one of four shades of the zone's draw (few materials, so pines batch); the old flat pine when the palette has no card art.</summary>
        Material PineMaterial(float pick)
        {
            pick = Mathf.Round(Mathf.Clamp01(pick) * 3) / 3;
            if (art.pineBough == null) return Tint(art.pine, Wither(Color.Lerp(new Color(.18f, .27f, .18f), new Color(.22f, .3f, .19f), pick)));
            return LeafMaterial(art.pineBough, Color.Lerp(new Color(.86f, .94f, .86f), new Color(1, 1, .9f), pick));
        }
        /// <summary>A tree's own draws (bark shade, lean, flare, bends), seeded by where it stands, so the zone's stream and every
        /// layout drawn from it stay exactly as they were.</summary>
        System.Random TreeRandom(Vector3 at) { return new System.Random((Zone.seed * 7919) ^ (Mathf.RoundToInt(at.x * 8) * 73856093) ^ (Mathf.RoundToInt(at.z * 8) * 19349663)); }
        static readonly Color[] Barks = { new Color(.30f, .26f, .22f), new Color(.36f, .31f, .26f), new Color(.27f, .25f, .23f), new Color(.40f, .36f, .31f), new Color(.33f, .31f, .27f), new Color(.25f, .22f, .19f) };
        /// <summary>One of a few bark shades (warm brown, grey-brown, weathered pale), so neighbouring trees differ without a material each.</summary>
        Material BarkShade(float u) { return Tint(art.bark, Wither(Barks[(int)(u * Barks.Length) % Barks.Length])); }
        /// <summary>
        /// A living tree's trunk, from under the ground to <paramref name="top"/> (local metres): tapering from <paramref name="r0"/>,
        /// gently bent, a few degrees off plumb and a little gnarled, on a root flare (a swell and four or five buttress ridges that
        /// sink into the soil, where a pipe had a collar). Closed at the top. Returns its axis, where a limb from a height starts.
        /// </summary>
        Func<float, Vector3> Bole(Transform t, System.Random tr, float top, float r0, Material bark, float close = 1.6f)
        {
            float T() { return (float)tr.NextDouble(); }
            float lx = (T() - .5f) * .1f, lz = (T() - .5f) * .1f, bend = (T() - .5f) * r0 * .9f, ph = T() * 6.3f, seed = T() * 60;
            int n = 4 + (int)(T() * 2); var at = new float[n]; var w = new float[n];
            for (int i = 0; i < n; i++) { at[i] = (i + (T() - .5f) * .6f) * 2 * Mathf.PI / n; w[i] = .7f + T() * .5f; }
            Vector3 Axis(float y) { float s = Mathf.Sin(y / top * 3.1f + ph) * bend; return new Vector3(lx * y + s, y, lz * y + s * .6f); }
            float Radius(float y, float a)
            {
                float r = r0 * (1 - .42f * Mathf.Clamp01(y / top)) * (1 + .16f * (Mathf.PerlinNoise(Mathf.Cos(a) + seed, Mathf.Sin(a) + y * 1.4f) - .5f));
                float flare = Mathf.Exp(-Mathf.Max(0, y + .1f) / (r0 * 2.1f)), ridge = 0;   // a long, concave swell, not a boot
                for (int i = 0; i < n; i++) ridge += w[i] * Mathf.Pow(Mathf.Max(0, Mathf.Cos(a - at[i])), 5);
                return (r + r0 * flare * (.22f + .9f * ridge)) * Mathf.Clamp01((top - y) / (r0 * close));   // close: a dead snag ends blunt, not in a spear
            }
            var rings = new[] { -.45f, -.2f, -.05f, .08f, .2f, .36f, .58f, top * .3f, top * .48f, top * .66f, top * .82f, top * .93f, top };
            MeshPart(ZoneMeshes.Tube(Axis, Radius, rings, 14, Vector3.right, 2, .5f), t, Vector3.zero, bark).name = "Trunk";
            Record(t, Axis, Radius, rings, true);
            return Axis;
        }
        /// <summary>
        /// A limb from inside a trunk or limb (<paramref name="from"/>, on its axis) out to <paramref name="to"/>: tapered, swelling
        /// where it leaves the wood it grows from, and bowed a little below the straight line so it sets out flatter and turns
        /// upward. Closed at the tip. Local metres, like the parts round it.
        /// </summary>
        void Limb(Transform t, Vector3 from, Vector3 to, float r0, float tip, Material bark, float bow = .12f, int sides = 8, bool collar = true, bool closed = true)
        {
            var d = to - from; float len = d.magnitude; if (len < .05f) return; var dir = d / len;
            var side = Vector3.Cross(dir, Vector3.up); if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
            var sag = Vector3.Cross(side.normalized, dir) * len * bow;   // perpendicular to the limb, on its underside
            if (sag.y > 0) sag = -sag;
            Vector3 C(float s) { return from + d * s + sag * (4 * s * (1 - s)); }
            // collar: swelling where it leaves the wood it grows from; closed: drawn in to its tip. A length that carries on from
            // another has neither, and runs on past its end into the next, so a crooked limb shows no knot or waist at its joints
            // (playtest note 24).
            float R(float s, float a) { return Mathf.Lerp(r0, tip, s) * (collar ? 1 + .35f * Mathf.Exp(-s * 9) : 1) * (closed ? Mathf.Clamp01((1.05f - s) / .1f) : 1); }
            var rings = new[] { 0, .12f, .26f, .42f, .58f, .74f, .88f, .97f, 1.05f };
            MeshPart(ZoneMeshes.Tube(C, R, rings, sides, side.normalized, 1, len * .5f), t, Vector3.zero, bark).name = "Limb";
            Record(t, C, R, rings, false);
        }
        /// <summary>
        /// The point <paramref name="s"/> (0 to 1) of the way along a <see cref="Limb"/> from <paramref name="from"/> to
        /// <paramref name="to"/> bowed by <paramref name="bow"/>: on its axis, where a branch off it starts. (A point on the
        /// straight line between its ends floats above it by up to bow times its length: playtest note 15, "a few trees are
        /// disjointed in the limb area": the Verdant giants' branches a metre over their limbs, the dead trees' twigs.)
        /// </summary>
        static Vector3 LimbAt(Vector3 from, Vector3 to, float bow, float s)
        {
            var d = to - from; float len = d.magnitude; if (len < .05f) return Vector3.Lerp(from, to, s); var dir = d / len;
            var side = Vector3.Cross(dir, Vector3.up); if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
            var sag = Vector3.Cross(side.normalized, dir) * len * bow;   // as Limb bows it
            if (sag.y > 0) sag = -sag;
            return from + d * s + sag * (4 * s * (1 - s));
        }
        /// <summary>
        /// For the tests (playtest note 15; TreeLimbTests): when on as a zone builds, every trunk's and limb's axis as built, by its
        /// tree's root (in the tree's own space): points along it with its mean radius at each, and where a limb starts. Off in play,
        /// and nothing is kept.
        /// </summary>
        public static bool RecordWood;
        public sealed class WoodAxis { public Vector3[] pts; public float[] r; public bool trunk; public Vector3? from; }
        public readonly Dictionary<Transform, List<WoodAxis>> Wood = new Dictionary<Transform, List<WoodAxis>>();
        void Record(Transform tree, Func<float, Vector3> centre, Func<float, float, float> radius, IList<float> rings, bool trunk)
        {
            if (!RecordWood) return;
            if (!Wood.TryGetValue(tree, out var list)) Wood[tree] = list = new List<WoodAxis>();
            var w = new WoodAxis { pts = new Vector3[rings.Count], r = new float[rings.Count], trunk = trunk, from = trunk ? (Vector3?)null : centre(rings[0]) };
            for (int i = 0; i < rings.Count; i++)
            {
                w.pts[i] = centre(rings[i]); float sum = 0;
                for (int k = 0; k < 8; k++) sum += radius(rings[i], k * Mathf.PI / 4);
                w.r[i] = sum / 8;
            }
            list.Add(w);
        }
        Mesh[] oakLeaves;
        /// <summary>
        /// The Great Oak on Oakhaven's green. Canon (book1 ch.4): a massive, ancient oak dominating the square, the settlement's
        /// historical heart; after the erasure it is found petrified mid-bloom in spring leaf, so now, before it, it is alive and in
        /// full leaf. A gnarled lofted bole on a flare of six buttresses, surface roots running out into the turf, five great limbs
        /// growing out of the bole where it swells and bending up into a layered dome of leaf, and a low stone bench ring round it.
        /// Local metres: the prop's scale multiplies it. Its own stream draws it (the zone's moves on as for the old dead oak).
        /// </summary>
        void GreatOak(Transform t)
        {
            SkipDeadOakDraws();
            LeafTrees.Add(t.position);
            var tr = new System.Random(Zone.seed + 4127); float T() { return (float)tr.NextDouble(); }
            var bark = Tint(art.bark, new Color(.41f, .36f, .31f)); var rootBark = Tint(art.bark, new Color(.35f, .33f, .27f));
            // The bole: a slight lean and wander, a metre in radius above the flare, swelling where the limbs part, then a tapering
            // leader up into the crown. Gnarl, deep fluting and a few burls; six buttresses, twisting a little as they climb.
            float lx = (T() - .5f) * .05f, lz = (T() - .5f) * .05f, ph = T() * 6.3f, seed = T() * 40;
            Vector3 Axis(float y) { float g = Mathf.Max(0, y) / 4; return new Vector3(lx * y + .12f * Mathf.Sin(y * .8f + ph) * g, y, lz * y + .1f * Mathf.Sin(y * 1.05f + ph + 2) * g); }
            const int Buttresses = 6; var bAt = new float[Buttresses]; var bW = new float[Buttresses];
            for (int i = 0; i < Buttresses; i++) { bAt[i] = (i + (T() - .5f) * .5f) * 2 * Mathf.PI / Buttresses; bW[i] = .75f + T() * .4f; }
            var burls = new Vector3[4]; for (int i = 0; i < burls.Length; i++) burls[i] = new Vector3(T() * 360, .9f + T() * 2.6f, .1f + T() * .12f);   // bearing (degrees), height, size
            float Girth(float y, float a)
            {
                float r = Mathf.Lerp(1.02f, .78f, Mathf.SmoothStep(0, 1, y / 3.4f)) + .18f * Mathf.Exp(-(y - 3.9f) * (y - 3.9f) / .36f) - .5f * Mathf.SmoothStep(0, 1, (y - 4.2f) / 2.2f);
                r *= 1 + .14f * (Mathf.PerlinNoise(Mathf.Cos(a) * 1.4f + seed, Mathf.Sin(a) * 1.4f + y * .7f) - .5f) + .025f * Mathf.Sin(a * 9 + y * 1.3f);
                foreach (var burl in burls) { float da = Mathf.DeltaAngle(a * Mathf.Rad2Deg, burl.x) / 28, dy = (y - burl.y) / .35f; r += burl.z * Mathf.Exp(-da * da - dy * dy); }
                float flare = Mathf.Exp(-Mathf.Max(0, y + .25f) / .5f), ridge = 0;
                for (int i = 0; i < Buttresses; i++) ridge += bW[i] * Mathf.Pow(Mathf.Max(0, Mathf.Cos(a - bAt[i] - y * .12f)), 8);
                return (r + flare * (.28f + 1.05f * ridge)) * Mathf.Clamp01((6.5f - y) / .5f);
            }
            // Two parts cut from the one surface (they meet without a seam): the flare, low and wide, and the bole, tall and narrow,
            // so the camera only counts as inside the tree (TreeFade) when it really is.
            MeshPart(ZoneMeshes.Tube(Axis, Girth, new[] { -.6f, -.42f, -.26f, -.13f, 0, .12f, .26f, .42f, .6f, .82f, 1.05f, 1.3f }, 32, Vector3.right, 3, .5f), t, Vector3.zero, bark).name = "Oak root flare";
            var boleRings = new[] { 1.3f, 1.65f, 2f, 2.4f, 2.8f, 3.2f, 3.55f, 3.85f, 4.15f, 4.5f, 4.9f, 5.35f, 5.8f, 6.2f, 6.5f };
            MeshPart(ZoneMeshes.Tube(Axis, Girth, boleRings, 32, Vector3.right, 3, .5f), t, Vector3.zero, bark).name = "Oak bole";
            Record(t, Axis, Girth, boleRings, true);
            // Surface roots: one out of each buttress and two between, flattened, sinking into the turf as they thin. One mesh.
            var roots = new List<CombineInstance>();
            for (int i = 0; i < Buttresses + 2; i++)
            {
                bool main = i < Buttresses; float a = main ? bAt[i] + .1f : bAt[i * 3 % Buttresses] + Mathf.PI / Buttresses, len = (main ? 1.7f : 1.1f) + T() * .6f, rr = main ? .42f * bW[i] : .26f, wig = (T() - .5f) * .9f, ph2 = T() * 6;
                var dir = new Vector3(Mathf.Cos(a), 0, -Mathf.Sin(a)); var across = new Vector3(-dir.z, 0, dir.x);   // the bole's angle a points this way
                Vector3 C(float s) { var p = dir * (1.05f + len * s) + across * (wig * Mathf.Sin(s * 3 + ph2) * s); return new Vector3(p.x, LocalGround(t, p.x, p.z) + .3f - .62f * s, p.z); }
                float R(float s, float ang) { return Mathf.Lerp(rr, .08f, s) * (1 - .38f * Mathf.Abs(Mathf.Cos(ang))); }   // angle 0 is up: wider than tall
                roots.Add(new CombineInstance { mesh = ZoneMeshes.Tube(C, R, new[] { 0, .12f, .25f, .4f, .55f, .7f, .85f, 1 }, 10, Vector3.up, 1, len * .5f), transform = Matrix4x4.identity });
            }
            var rootMesh = new Mesh { name = "Oak roots" }; rootMesh.CombineMeshes(roots.ToArray(), true, false); rootMesh.RecalculateBounds();
            foreach (var piece in roots) DestroyImmediate(piece.mesh);
            MeshPart(rootMesh, t, Vector3.zero, rootBark);
            // Five great limbs out of the bole where it swells: the low ones sprawl wide and nearly level, the high ones climb. Each
            // leaves the bole flat and bends upward (a quadratic curve), swells where it joins, and carries three lesser branches.
            var tips = new List<(Vector3 at, float size)>(); float turn = T() * 360;
            for (int i = 0; i < 5; i++)
            {
                float yaw = turn + i * 72 + (T() - .5f) * 26, y0 = 3.3f + i * 2 % 5 * .25f + T() * .1f, low = Mathf.Clamp01(1 - (y0 - 3.3f));
                var outward = Quaternion.Euler(0, yaw, 0) * Vector3.right;
                float reach = Mathf.Lerp(3.2f, 4.6f, low) + T() * .5f, rise = Mathf.Lerp(3.3f, 1.8f, low) + T() * .5f, rb = Mathf.Lerp(.44f, .54f, low);
                Vector3 p0 = Axis(y0), p2 = p0 + outward * reach + Vector3.up * rise, p1 = p0 + outward * reach * .55f + Vector3.up * rise * .12f;
                Vector3 C(float s) { return (1 - s) * (1 - s) * p0 + 2 * s * (1 - s) * p1 + s * s * p2; }
                float R(float s, float a) { return Mathf.Lerp(rb, .12f, s) * (1 + .3f * Mathf.Exp(-s * 7)) * (1 + .05f * Mathf.Sin(a * 3 + s * 9)) * Mathf.Clamp01((1.06f - s) / .1f); }
                var limbRings = new[] { 0, .08f, .17f, .27f, .38f, .5f, .62f, .74f, .86f, .96f, 1.06f };
                MeshPart(ZoneMeshes.Tube(C, R, limbRings, 14, Vector3.Cross(Vector3.up, outward), 2, 2.5f), t, Vector3.zero, bark).name = "Oak limb";
                Record(t, C, R, limbRings, false);
                tips.Add((p2 + Vector3.up * .25f, 3.1f + T() * .5f));
                for (int k = 0; k < 3; k++)
                {
                    float s = .4f + k * .2f + T() * .06f, turnBy = (k % 2 == 0 ? 1 : -1) * (26 + T() * 20), len = (2.6f - k * .35f) * (.85f + T() * .3f);
                    var along = C(s + .01f) - C(s - .01f); var flat = new Vector3(along.x, 0, along.z).normalized;
                    var dir = (Quaternion.AngleAxis(turnBy, Vector3.up) * flat + Vector3.up * (.18f + T() * .3f)).normalized;   // out to the side, not straight up like a peg
                    var from = C(s); var to = from + dir * len + Vector3.up * len * .25f;
                    Limb(t, from, to, R(s, 0) * .68f, .06f, bark, .16f, 9);   // bowed: it sets out level and turns up into the leaf
                    tips.Add((to + Vector3.up * .2f, 2.6f + T() * .6f));
                }
            }
            // The crown: leaf on every limb and branch end (small: they are its edges), then the bulk as five big squashed masses
            // over the limbs and one over the leader, a ring of middle clumps under their rims, a low wide skirt of small clumps
            // and small clumps on top, so from above it reads as a few broad billows, not a heap of equal balls; big clumps
            // inside too, so no sky shows through its heart. Fresh green in a few shades, a touch of autumn gold on the rim.
            // All from the oak's own stream, so nothing else in the zone moves.
            if (oakLeaves == null) { oakLeaves = new Mesh[5]; for (int i = 0; i < 5; i++) oakLeaves[i] = ZoneMeshes.Blob(Zone.seed + 830 + i * 29, .6f, false, 10, 16); }
            Material deep = Tint(art.foliage, new Color(.19f, .30f, .12f)), mid = Tint(art.foliage, new Color(.25f, .37f, .14f)), light = Tint(art.foliage, new Color(.32f, .44f, .17f)),
                sunlit = Tint(art.foliage, new Color(.40f, .47f, .19f)), gold = Tint(art.foliage, new Color(.42f, .43f, .16f));   // gold: the first turn of autumn, on two low clumps
            void Clump(Vector3 at, float size, Material m, float flat = .8f)
            {
                var o = MeshPart(oakLeaves[(int)(T() * 5) % 5], t, at, m, Quaternion.Euler((T() - .5f) * 20, T() * 360, (T() - .5f) * 20));
                o.transform.localScale = new Vector3(size, size * flat, size); o.name = "Oak leaves";
            }
            foreach (var tip in tips) Clump(tip.at, tip.size * .7f, T() < .5f ? mid : light);
            for (int i = 0; i < 5; i++) { float a = (i + (T() - .5f) * .3f) * Mathf.PI * 2 / 5 + .4f, r = 1.9f + T() * .9f; Clump(new Vector3(Mathf.Cos(a) * r, 7.1f + T() * .9f, Mathf.Sin(a) * r), 5.6f + T() * 1.6f, i % 2 == 0 ? mid : light, .5f + T() * .1f); }
            Clump(new Vector3(.2f, 8.7f, -.2f), 6.4f, sunlit, .55f);
            for (int i = 0; i < 12; i++) { float a = (i + T() * .5f) * Mathf.PI / 6, r = 4.7f + T(); Clump(new Vector3(Mathf.Cos(a) * r, 4.8f + T() * .6f, Mathf.Sin(a) * r), 2 + T() * .8f, i % 6 == 1 ? gold : i % 2 == 0 ? deep : mid, .62f); }
            for (int i = 0; i < 8; i++) { float a = (i + T() * .6f) * Mathf.PI / 4 + .3f, r = 3.6f + T() * .9f; Clump(new Vector3(Mathf.Cos(a) * r, 6.2f + T() * .7f, Mathf.Sin(a) * r), 3 + T() * .8f, i % 3 == 0 ? light : mid, .65f); }
            for (int i = 0; i < 5; i++) { float a = (i + T() * .6f) * Mathf.PI * 2 / 5 + .9f, r = .7f + T() * 1.6f; Clump(new Vector3(Mathf.Cos(a) * r, 9.9f + T() * .8f, Mathf.Sin(a) * r), 1.7f + T() * .8f, i % 2 == 0 ? sunlit : light); }
            for (int i = 0; i < 3; i++) { float a = i * 2.1f + T(); Clump(new Vector3(Mathf.Cos(a) * 1.5f, 6.3f + T() * .6f, Mathf.Sin(a) * 1.5f), 4 + T() * .4f, deep); }
            // A fringe of painted leaf cards (Crulanda/Leaf) round the skirt, the middle ring and the top, fanning out past the clumps,
            // so the crown's edge against the sky and the ground is ragged leaf, not the clumps' smooth curves. Three meshes (skirt,
            // rim, top), so the fade still tells the crown's levels apart. From the oak's stream, after every clump: nothing else moves.
            if (art.leafCards != null && art.leafCards.Length > 0)
            {
                var crownHeart = new Vector3(0, 7.2f, 0); var fringeLeaf = LeafMaterial(0);
                void Fringe(string name, int count, float step, float phase, float r0, float r1, float y0, float y1, float s0, float s1, float lift0, float lift1)
                {
                    var fringe = new ZoneMeshes.Cards();
                    for (int i = 0; i < count; i++)
                    {
                        float a = (i + T() * .6f) * step + phase, r = Mathf.Lerp(r0, r1, T()), y = Mathf.Lerp(y0, y1, T()), size = Mathf.Lerp(s0, s1, T());
                        var outward = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var along = (outward + Vector3.up * Mathf.Lerp(lift0, lift1, T())).normalized;
                        var across = Quaternion.AngleAxis(T() * 180, along) * Vector3.Cross(along, Vector3.up).normalized;
                        var shade = i % 7 == 1 ? new Color(1, .95f, .72f) : i % 2 == 0 ? new Color(.8f, .84f, .74f) : new Color(.92f, .95f, .86f);   // a touch of gold, as on the clumps
                        fringe.AddCross(outward * r + Vector3.up * y - along * size * .3f, along, across, size, size * .9f, crownHeart, .25f, shade);
                    }
                    MeshPart(fringe.Build(name), t, Vector3.zero, fringeLeaf);
                }
                Fringe("Oak leaf skirt", 14, Mathf.PI / 7, 0, 4.4f, 5.2f, 4.5f, 5.3f, 2.6f, 3.2f, -.2f, .2f);
                Fringe("Oak leaf rim", 10, Mathf.PI / 5, .3f, 3.5f, 4.3f, 6.5f, 7.3f, 2.8f, 3.4f, .3f, .7f);
                Fringe("Oak leaf top", 5, Mathf.PI * 2 / 5, .9f, 1, 2.4f, 9.4f, 10.2f, 2.3f, 2.8f, 1, 1.8f);
            }
            var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, 3.2f, 0); cap.height = 6.4f; cap.radius = 1.1f;
            t.gameObject.AddComponent<NavBlocker>(); t.gameObject.AddComponent<TreeFade>();
            OakRing(t, 3.05f * t.localScale.x);
        }
        /// <summary>A low dry-stone bench ring round the Great Oak's roots, where the village sits in its shade (GAME-ONLY). Its own
        /// root beside the tree, so the stone never fades with it; solid all round, so people walk round it.</summary>
        void OakRing(Transform oak, float radius)
        {
            var ring = new GameObject("Oak bench ring").transform; ring.SetParent(oak.parent, false); ring.position = oak.position;
            var tr = new System.Random(Zone.seed + 4133); float T() { return (float)tr.NextDouble(); }
            var stones = new[] { Tint(art.stone, new Color(.52f, .5f, .46f)), Tint(art.stone, new Color(.47f, .45f, .41f)), Tint(art.stone, new Color(.56f, .53f, .48f)) };
            var seat = Tint(art.stone, new Color(.6f, .58f, .53f));
            // Each block and its cap stone are ring segments with radial (mitred) ends, so the joints neither gap on the outside
            // nor overlap within; the courses differ a little in height and shade, as laid stone does.
            const int Blocks = 22; float step = 360f / Blocks;
            for (int i = 0; i < Blocks; i++)
            {
                float a0 = i * step, a = a0 + step / 2, high = .36f + T() * .06f, y = LocalGround(ring, Mathf.Cos(a * Mathf.Deg2Rad) * radius, Mathf.Sin(a * Mathf.Deg2Rad) * radius);
                MeshPart(ZoneMeshes.Arc(radius - .25f, radius + .25f, a0 - .15f, a0 + step + .15f, high), ring, Vector3.up * (y - .08f), stones[(int)(T() * 3) % 3]).name = "Bench stone";
                MeshPart(ZoneMeshes.Arc(radius - .32f, radius + .32f, a0 - .25f, a0 + step + .25f, .1f), ring, Vector3.up * (y + high - .1f), seat).name = "Bench seat";   // the flat cap stone
            }
            for (int i = 0; i < 10; i++)
            {
                var seg = new GameObject("Bench").transform; seg.SetParent(ring, false); float a = i * 36;
                seg.localPosition = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * radius, 0, Mathf.Sin(a * Mathf.Deg2Rad) * radius); seg.localRotation = Quaternion.Euler(0, -(a + 90), 0);
                Solid(seg, new Vector3(0, .3f, 0), new Vector3(2 * radius * Mathf.Sin(18 * Mathf.Deg2Rad) + .25f, .8f, .7f));
            }
        }
        /// <summary>The zone draws DeadTree took for the Great Oak when it was a dead oak (its flare, then a five-deep limb tree),
        /// taken again without building anything, so every prop, grove and tree drawn after it keeps its old place.</summary>
        void SkipDeadOakDraws() { for (int i = 0; i < 5; i++) _ = R01; SkipBranchDraws(5, .675f); }
        void SkipBranchDraws(int depth, float radius)
        {
            if (depth <= 0 || radius < .02f) return;
            int kids = depth > 3 ? 3 : 2 + (R01 > .5f ? 1 : 0);
            for (int i = 0; i < kids; i++) { _ = R01 + R01 + R01; SkipBranchDraws(depth - 1, radius * .58f); }
        }
        Mesh cone;
        void Pine(Transform t)
        {
            float h = 6 + R01 * 4;
            var tr = TreeRandom(t.position); float T() { return (float)tr.NextDouble(); }
            // The trunk runs the whole height now, a pole tapering to the leader's tip on a small root swell (the boughs no longer
            // hide a stub). A tube, so the bark grain runs up it: the shared cone mesh has no UVs and would read as one flat texel.
            float top = 1.1f + h * .94f;
            Vector3 Axis(float s) { return new Vector3(0, s, 0); }
            float Radius(float s, float a) { return Mathf.Lerp(.24f, .04f, Mathf.Clamp01(s / top)) * (1 + .35f * Mathf.Exp(-Mathf.Max(0, s + .3f) / .45f)) * Mathf.Clamp01((top - s) / .3f); }
            var rings = new[] { -.3f, -.1f, .1f, .4f, top * .2f, top * .4f, top * .6f, top * .8f, top * .92f, top };
            MeshPart(ZoneMeshes.Tube(Axis, Radius, rings, 8, Vector3.right, 2, .5f), t, Vector3.zero, art.bark).name = "Trunk";
            Record(t, Axis, Radius, rings, true);
            // Five or six tiers of painted bough cards (no solid cones), six to eight boughs each fanning out from the trunk and
            // drooping 20-35 degrees, every card 1.6 tier radii across, so the boughs overlap round a tier and the tiers overlap
            // down the tree: no trunk shows between them, and the silhouette is a full triangle, widest at the foot. Each bough
            // is crossed by a narrower hanging fin, so a tier has depth from the side. Darker in at the trunk on every bough and
            // toward the tree's foot, in the shade the zone drew for this tree. The zone's draws are the ones the cones took
            // (the shade, then five tiers' small offset, tilt and turn); a sixth tier, and every bough's own shape, come from the
            // tree's own stream.
            float pick = R01;
            var cards = new ZoneMeshes.Cards();
            int tiers = 5 + (T() < .5f ? 1 : 0);
            for (int i = 0; i < tiers; i++)
            {
                float ox, oz, tiltX, turn, tiltZ;
                if (i < 5) { ox = R01 * .2f - .1f; oz = R01 * .2f - .1f; tiltX = R01 * 6 - 3; turn = R01 * 360; tiltZ = R01 * 6 - 3; }
                else { ox = T() * .2f - .1f; oz = T() * .2f - .1f; tiltX = T() * 6 - 3; turn = T() * 360; tiltZ = T() * 6 - 3; }
                float u = (float)i / (tiers - 1), y = 1.1f + u * h * .6f, rad = Mathf.Lerp(2.6f, .8f, u) * (h / 8);
                var tilt = Quaternion.Euler(tiltX, 0, tiltZ); var tier = Color.Lerp(new Color(.58f, .66f, .58f), Color.white, u);
                var heart = new Vector3(ox, y - rad * .5f, oz);   // the cards' normals fan out and up from here, like a cone's surface
                int boughs = 6 + (int)(T() * 3);
                for (int k = 0; k < boughs; k++)
                {
                    float droop = (20 + T() * 15) * Mathf.Deg2Rad; var outward = Quaternion.Euler(0, turn + k * 360f / boughs + (T() - .5f) * 24, 0) * Vector3.right;
                    var along = tilt * (outward * Mathf.Cos(droop) - Vector3.up * Mathf.Sin(droop));
                    var across = Quaternion.AngleAxis((T() - .5f) * 30, along) * Vector3.Cross(along, Vector3.up).normalized;
                    float len = rad * (.95f + T() * .2f), wide = rad * 1.6f * (.9f + T() * .2f);
                    var foot = new Vector3(ox, y + h * .06f, oz) + outward * .05f; var shade = tier * (.92f + T() * .1f); var footShade = shade * .6f;
                    cards.Add(foot, along, across, len, wide, heart, .35f, shade, 0, 1, footShade);
                    cards.Add(foot, along, Vector3.Cross(along, across), len * .85f, wide * .4f, heart, .35f, shade * .85f, 0, 1, footShade * .85f);
                }
            }
            // The leader: a small tier of five short boughs over the top tier, then three upright fronds crossed at sixty degrees
            // running to the tip, so the top is a dense spike, not a tuft.
            {
                float y = 1.1f + h * .7f, rad = .5f * (h / 8), turn = T() * 360; var heart = new Vector3(0, y - rad * .5f, 0);
                for (int k = 0; k < 5; k++)
                {
                    float droop = (25 + T() * 15) * Mathf.Deg2Rad; var outward = Quaternion.Euler(0, turn + k * 72 + (T() - .5f) * 20, 0) * Vector3.right;
                    var along = outward * Mathf.Cos(droop) - Vector3.up * Mathf.Sin(droop);
                    cards.Add(new Vector3(0, y + h * .03f, 0), along, Vector3.Cross(along, Vector3.up).normalized, rad * 1.1f, rad * 1.6f, heart, .35f, Color.white, 0, 1, new Color(.6f, .6f, .6f));
                }
                float ty = 1.1f + h * .66f, tl = h * .3f, lead = T() * 180; var tip = new Vector3(0, ty - tl * .5f, 0);
                for (int k = 0; k < 3; k++)
                    cards.Add(new Vector3(0, ty, 0), Vector3.up, Quaternion.Euler(0, lead + k * 60, 0) * Vector3.right, tl * (k == 0 ? 1 : .95f), tl * .36f, tip, .5f, k == 0 ? Color.white : new Color(.92f, .92f, .9f), 0, 1, new Color(.68f, .7f, .68f));
            }
            MeshPart(cards.Build("Boughs"), t, Vector3.zero, PineMaterial(pick));
            var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, 2, 0); cap.height = 4; cap.radius = .4f;
            t.gameObject.AddComponent<NavBlocker>(); t.gameObject.AddComponent<TreeFade>();
        }
        // ---------- chunky props (the painted style pass, part 4): shared shapes ----------
        readonly Dictionary<string, Mesh> propMeshes = new Dictionary<string, Mesh>();
        /// <summary>A prop mesh built once and shared by every prop that uses it (posts, barrels, crates, wheels).</summary>
        Mesh PropMesh(string key, Func<Mesh> make)
        {
            if (!propMeshes.TryGetValue(key, out var m)) { m = make(); m.name = key; propMeshes[key] = m; }
            return m;
        }
        /// <summary>
        /// A turned shape (well ring, barrel, bucket, pot, wheel rim): the profile (x radius, y height) swept round the y axis.
        /// The profile runs up the outside, in over the top and down the inside; each stretch of it is flat-shaded against the
        /// next, so hoops and rims keep a crisp edge. UVs tile every <paramref name="tile"/> metres.
        /// </summary>
        static Mesh Turned(Vector2[] profile, int sides, float tile = 1)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            float along = 0, widest = 0; foreach (var p in profile) widest = Mathf.Max(widest, p.x);
            int wraps = Mathf.Max(1, Mathf.RoundToInt(2 * Mathf.PI * widest / tile));
            for (int k = 0; k + 1 < profile.Length; k++)
            {
                Vector2 a = profile[k], b = profile[k + 1], d = b - a; float len = d.magnitude; if (len < 1e-4f) continue;
                int at = v.Count;
                for (int s = 0; s <= sides; s++)
                {
                    float ang = 2 * Mathf.PI * (s % sides) / sides, c = Mathf.Cos(ang), sn = Mathf.Sin(ang), u = (float)s / sides * wraps;
                    var normal = new Vector3(c * d.y, -d.x, sn * d.y) / len;   // square to the stretch, away from the solid
                    v.Add(new Vector3(c * a.x, a.y, sn * a.x)); n.Add(normal); uv.Add(new Vector2(u, along / tile));
                    v.Add(new Vector3(c * b.x, b.y, sn * b.x)); n.Add(normal); uv.Add(new Vector2(u, (along + len) / tile));
                }
                for (int s = 0; s < sides; s++) { int i = at + s * 2; tri.AddRange(new[] { i, i + 1, i + 3, i, i + 3, i + 2 }); }
                along += len;
            }
            var m = new Mesh { name = "Turned" }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateBounds();
            return m;
        }
        /// <summary>A board cut to a convex outline (x, y), <paramref name="thick"/> deep along z: sign boards, an awning's scallops.</summary>
        static Mesh Cutout(Vector2[] outline, float thick)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            var o = (Vector2[])outline.Clone(); float area = 0, h = thick / 2;
            for (int i = 0; i < o.Length; i++) { Vector2 a = o[i], b = o[(i + 1) % o.Length]; area += a.x * b.y - b.x * a.y; }
            if (area > 0) Array.Reverse(o);   // clockwise seen from -z
            foreach (int face in new[] { -1, 1 })
            {
                int at = v.Count;
                foreach (var p in o) { v.Add(new Vector3(p.x, p.y, face * h)); n.Add(new Vector3(0, 0, face)); uv.Add(p); }
                for (int i = 1; i + 1 < o.Length; i++) tri.AddRange(face < 0 ? new[] { at, at + i, at + i + 1 } : new[] { at, at + i + 1, at + i });
            }
            for (int i = 0; i < o.Length; i++)
            {
                Vector2 a = o[i], b = o[(i + 1) % o.Length], e = b - a; var side = new Vector3(-e.y, e.x, 0).normalized; int at = v.Count;
                v.Add(new Vector3(a.x, a.y, -h)); v.Add(new Vector3(a.x, a.y, h)); v.Add(new Vector3(b.x, b.y, h)); v.Add(new Vector3(b.x, b.y, -h));
                for (int k = 0; k < 4; k++) n.Add(side);
                uv.Add(Vector2.zero); uv.Add(new Vector2(0, thick)); uv.Add(new Vector2(e.magnitude, thick)); uv.Add(new Vector2(e.magnitude, 0));
                tri.AddRange(new[] { at, at + 1, at + 2, at, at + 2, at + 3 });
            }
            var m = new Mesh { name = "Cutout" }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateBounds();
            return m;
        }
        static CombineInstance Piece(Mesh mesh, Vector3 at = default(Vector3), Quaternion? rot = null)
        {
            return new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(at, rot ?? Quaternion.identity, Vector3.one) };
        }
        /// <summary>Several pieces as one mesh of one material.</summary>
        static Mesh Joined(params CombineInstance[] pieces)
        {
            var m = new Mesh(); m.CombineMeshes(pieces, true); m.RecalculateBounds(); return m;
        }
        /// <summary>One mesh of two materials: the first takes <paramref name="a"/>, the second <paramref name="b"/> (wood and its iron, a log and its cut ends).</summary>
        static Mesh TwoTone(Mesh a, Mesh b)
        {
            var m = new Mesh(); m.CombineMeshes(new[] { Piece(a), Piece(b) }, false); m.RecalculateBounds(); return m;
        }
        GameObject MeshPart2(Mesh mesh, Transform parent, Vector3 localPos, Material first, Material second, Quaternion? rot = null)
        {
            var o = MeshPart(mesh, parent, localPos, first, rot); o.GetComponent<MeshRenderer>().sharedMaterials = new[] { first, second }; return o;
        }
        /// <summary>A round bar between two points (rope, iron braces, axles, handles).</summary>
        GameObject Rod(Transform t, Vector3 a, Vector3 b, float thick, Material m)
        {
            var d = b - a; return Part(PrimitiveType.Cylinder, t, (a + b) / 2, new Vector3(thick, d.magnitude / 2, thick), m, Quaternion.FromToRotation(Vector3.up, d));
        }
        /// <summary>A squared bar between two points that are not one above the other, its faces kept upright (rails, shafts, trestle legs).</summary>
        GameObject Bar(Transform t, Vector3 a, Vector3 b, float high, float thick, Material m)
        {
            var d = b - a; return Part(PrimitiveType.Cube, t, (a + b) / 2, new Vector3(thick, high, d.magnitude), m, Quaternion.LookRotation(d));
        }
        /// <summary>
        /// A hewn post standing on <paramref name="foot"/>: six-sided, thicker at the foot, with a chamfered cap proud of the
        /// shaft. Every post of one thickness and height shares a mesh (heights go in steps of 4 cm).
        /// </summary>
        GameObject Stake(Transform t, Vector3 foot, float thick, float tall, Material m, Quaternion? rot = null)
        {
            int w = Mathf.RoundToInt(thick * 100), h = Mathf.RoundToInt(tall * 25); float r = w / 200f, top = h / 25f;
            var mesh = PropMesh("Post " + w + "x" + h, () => Turned(new[] { new Vector2(r * 1.16f, 0), new Vector2(r, .22f), new Vector2(r * .94f, top - r * 1.5f), new Vector2(r * 1.3f, top - r * 1.3f), new Vector2(r * 1.3f, top - r * .45f), new Vector2(r * .85f, top - r * .1f), new Vector2(0, top) }, 6));
            return MeshPart(mesh, t, foot, m, rot);
        }
        /// <summary>A coopered barrel standing on <paramref name="foot"/>, a metre tall and .72 across the bilge at size 1: bulged staves, a recessed head and three iron hoops.</summary>
        GameObject Barrel(Transform t, Vector3 foot, float size = 1, float yaw = 0)
        {
            var mesh = PropMesh("Barrel", () => TwoTone(
                Turned(new[] { new Vector2(0, 0), new Vector2(.3f, 0), new Vector2(.345f, .28f), new Vector2(.36f, .5f), new Vector2(.345f, .72f), new Vector2(.3f, 1), new Vector2(.27f, 1), new Vector2(.27f, .95f), new Vector2(0, .95f) }, 12),
                Joined(Piece(Turned(new[] { new Vector2(.315f, .12f), new Vector2(.335f, .12f), new Vector2(.348f, .2f), new Vector2(.328f, .2f) }, 12)),
                    Piece(Turned(new[] { new Vector2(.355f, .46f), new Vector2(.376f, .46f), new Vector2(.376f, .54f), new Vector2(.355f, .54f) }, 12)),
                    Piece(Turned(new[] { new Vector2(.328f, .8f), new Vector2(.348f, .8f), new Vector2(.335f, .88f), new Vector2(.315f, .88f) }, 12)))));
            var o = MeshPart2(mesh, t, foot, art.timber, Tint(art.metal, new Color(.2f, .2f, .22f)), Quaternion.Euler(0, yaw, 0)); o.transform.localScale = Vector3.one * size;
            return o;
        }
        /// <summary>A packing crate <paramref name="size"/> a side, centred on <paramref name="at"/>: plank panels and a lid set in a frame of darker battens, a brace across each side.</summary>
        GameObject Crate(Transform t, Vector3 at, float size, float yaw = 0)
        {
            var mesh = PropMesh("Crate", () =>
            {
                var frame = new List<CombineInstance>();
                foreach (int a in new[] { -1, 1 }) foreach (int b in new[] { -1, 1 })
                {
                    frame.Add(Piece(ZoneMeshes.Box(new Vector3(.12f, 1, .12f), 1), new Vector3(a * .44f, 0, b * .44f)));        // corner posts
                    frame.Add(Piece(ZoneMeshes.Box(new Vector3(.76f, .12f, .12f), 1), new Vector3(0, a * .44f, b * .44f)));     // rails along x
                    frame.Add(Piece(ZoneMeshes.Box(new Vector3(.12f, .12f, .76f), 1), new Vector3(b * .44f, a * .44f, 0)));     // rails along z
                }
                foreach (int s in new[] { -1, 1 })
                {
                    frame.Add(Piece(ZoneMeshes.Box(new Vector3(.1f, 1.05f, .04f), 1), new Vector3(0, 0, s * .47f), Quaternion.Euler(0, 0, s * 45)));
                    frame.Add(Piece(ZoneMeshes.Box(new Vector3(.04f, 1.05f, .1f), 1), new Vector3(s * .47f, 0, 0), Quaternion.Euler(s * 45, 0, 0)));
                }
                return TwoTone(Joined(Piece(ZoneMeshes.Box(new Vector3(.92f, .96f, .92f), .5f)), Piece(ZoneMeshes.Box(new Vector3(.8f, .05f, .8f), .5f), new Vector3(0, .5f, 0))), Joined(frame.ToArray()));
            });
            var o = MeshPart2(mesh, t, at, Tint(art.timber, new Color(.45f, .33f, .21f)), Tint(art.timber, new Color(.27f, .19f, .12f)), Quaternion.Euler(0, yaw, 0)); o.transform.localScale = Vector3.one * size;
            return o;
        }
        /// <summary>A spoked cart wheel, 1.23 m across, its axle along the part's y: wooden felloes, eight spokes and a hub in the first material, an iron tyre in the second.</summary>
        Mesh Wheel
        {
            get
            {
                return PropMesh("Cart wheel", () =>
                {
                    var wood = new List<CombineInstance> {
                        Piece(Turned(new[] { new Vector2(.48f, -.05f), new Vector2(.585f, -.05f), new Vector2(.585f, .05f), new Vector2(.48f, .05f), new Vector2(.48f, -.05f) }, 16)),
                        Piece(Turned(new[] { new Vector2(0, -.13f), new Vector2(.1f, -.13f), new Vector2(.13f, -.04f), new Vector2(.13f, .04f), new Vector2(.1f, .13f), new Vector2(0, .13f) }, 10)) };
                    for (int k = 0; k < 4; k++) wood.Add(Piece(ZoneMeshes.Box(new Vector3(1, .06f, .08f), 1), Vector3.zero, Quaternion.Euler(0, k * 45, 0)));
                    return TwoTone(Joined(wood.ToArray()), Turned(new[] { new Vector2(.585f, -.06f), new Vector2(.615f, -.06f), new Vector2(.615f, .06f), new Vector2(.585f, .06f) }, 16));
                });
            }
        }
        /// <summary>A fingerpost: a hewn post leaning a little, its board cut to a point and hung a touch crooked on two pegs.</summary>
        /// <summary>A notice board (2026-10-03, the bounty boards): two posts, a wide board under a little pent roof, and a few
        /// pale notices pinned to it at odd angles. Its prop carries interact "Read the notices"; the session opens the board.</summary>
        void NoticeBoard(Transform t)
        {
            foreach (float x in new[] { -.7f, .7f }) Stake(t, new Vector3(x, -.1f, 0), .14f, 2.1f, art.timber, Quaternion.identity);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.35f, 0), new Vector3(1.7f, .9f, .06f), Tint(art.timber, new Color(.42f, .31f, .19f)));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.86f, -.06f), new Vector3(1.9f, .05f, .3f), Tint(art.timber, new Color(.3f, .22f, .13f)), Quaternion.Euler(-12, 0, 0));   // the pent roof
            var paper = Tint(art.timber, new Color(.86f, .8f, .66f));
            foreach (var (x, y, tilt, w, h) in new[] { (-.5f, 1.4f, 4f, .34f, .42f), (-.05f, 1.32f, -6f, .3f, .36f), (.45f, 1.45f, 3f, .36f, .3f), (.3f, 1.05f, -3f, .28f, .22f) })
            {
                Part(PrimitiveType.Cube, t, new Vector3(x, y, -.04f), new Vector3(w, h, .01f), paper, Quaternion.Euler(0, 0, tilt));
                Part(PrimitiveType.Sphere, t, new Vector3(x, y + h / 2 - .03f, -.05f), Vector3.one * .03f, Tint(art.timber, new Color(.2f, .14f, .09f)));   // the nail
            }
        }
        static Material signText;
        /// <summary>The font in the sign-text shader (Resources/Shaders/SignText): hidden behind the board like anything solid.</summary>
        static Material SignTextMaterial(Font font)
        {
            if (signText != null) return signText;
            var sh = Resources.Load<Shader>("Shaders/SignText");
            signText = sh != null ? new Material(sh) { name = "Sign text", mainTexture = font.material.mainTexture } : font.material;
            return signText;
        }
        void Signpost(Transform t, string text = null)
        {
            Stake(t, new Vector3(0, -.1f, 0), .2f, 2.44f, art.timber, Quaternion.Euler(0, 0, 2));
            var board = PropMesh("Sign board", () => Cutout(new[] { new Vector2(-.24f, -.17f), new Vector2(-.24f, .17f), new Vector2(.86f, .17f), new Vector2(1.08f, 0), new Vector2(.86f, -.17f) }, .07f));
            MeshPart(board, t, new Vector3(0, 1.82f, -.12f), Tint(art.timber, new Color(.5f, .37f, .22f)), Quaternion.Euler(0, 0, -5));
            foreach (float x in new[] { -.13f, .02f }) Part(PrimitiveType.Cube, t, new Vector3(x, 1.82f - x * .087f, -.16f), new Vector3(.05f, .05f, .06f), Tint(art.timber, new Color(.2f, .14f, .09f)));   // the pegs, on the board's own slant
            // The board's writing (Round 22, playtest note 49: "should say something on it"): the prop's name, burnt into both faces.
            if (string.IsNullOrEmpty(text)) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (float side in new[] { -1f, 1f })
            {
                var tm = new GameObject("Sign text").AddComponent<TextMesh>(); tm.transform.SetParent(t, false);
                tm.transform.localPosition = new Vector3(.4f, 1.82f - .4f * .087f, -.12f + side * .045f); tm.transform.localRotation = Quaternion.Euler(0, side < 0 ? 0 : 180, side < 0 ? -5 : 5);
                tm.text = text; tm.font = font; tm.fontSize = 64; tm.characterSize = Mathf.Min(.03f, 1.1f / Mathf.Max(6, text.Length) * .26f); tm.fontStyle = FontStyle.Bold;
                tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = new Color(.17f, .11f, .05f);
                var r = tm.GetComponent<MeshRenderer>(); r.sharedMaterial = SignTextMaterial(font); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            }
        }
        /// <summary>
        /// A post-and-rail fence along x: hewn posts with chamfered caps, each leaning and standing a little taller or shorter
        /// by its own two draws (the same two a post always took, in the same order), and two rails a span, lapped past the
        /// posts on alternate faces and following the posts' uneven heights.
        /// </summary>
        void Fence(Transform t, float length)
        {
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 2) + 1); float step = length / (posts - 1), low = 0, high = 0, lowZ = 0, highZ = 0;
            var rail = Tint(art.timber, new Color(.4f, .29f, .19f));
            for (int i = 0; i < posts; i++)
            {
                float ax = R01 * 6 - 3, az = R01 * 6 - 3, x = -length / 2 + step * i;   // two draws a post: its lean, and from the same two its height and where the rails meet it
                Stake(t, new Vector3(x, -.12f, 0), i % 2 == 0 ? .2f : .18f, 1.26f + ax * .04f, art.timber, Quaternion.Euler(ax, 0, az));
                float l = .42f + ax * .012f, h = .86f + az * .015f, face = i % 2 == 0 ? .1f : -.1f, lean = Mathf.Sin(ax * Mathf.Deg2Rad), lz = lean * (l + .12f), hz = lean * (h + .12f);   // how far the leaning post stands off the line at each rail
                if (i > 0)
                {
                    Bar(t, new Vector3(x - step - .18f, low, face + lowZ), new Vector3(x + .18f, l, face + lz), .1f, .05f, rail);
                    Bar(t, new Vector3(x - step - .18f, high, face + highZ), new Vector3(x + .18f, h, face + hz), .1f, .05f, rail);
                }
                low = l; high = h; lowZ = lz; highZ = hz;
            }
            Solid(t, new Vector3(0, .6f, 0), new Vector3(length, 1.2f, .3f));
        }
        /// <summary>A farm cart, shafts to -z: a plank bed between flared side boards, two spoked wheels with iron tyres on an axle, and a heaped load of hay.</summary>
        void Cart(Transform t)
        {
            var board = Tint(art.timber, new Color(.42f, .3f, .19f)); var dark = Tint(art.timber, new Color(.25f, .17f, .11f));
            BoxPart(t, new Vector3(0, .8f, 0), new Vector3(1.5f, .12f, 2.6f), art.timber, null, 1);   // the bed
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(s * .8f, 1.08f, 0), new Vector3(.07f, .46f, 2.74f), board, Quaternion.Euler(0, 0, s * -9));   // side boards, flared out over the wheels
                MeshPart2(Wheel, t, new Vector3(s * .95f, .615f, -.2f), dark, Tint(art.metal, new Color(.2f, .2f, .22f)), Quaternion.Euler(0, 0, 90));
                Bar(t, new Vector3(s * .56f, .8f, -1.2f), new Vector3(s * .46f, .52f, -3.15f), .1f, .09f, art.timber);   // shafts
            }
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.05f, -1.3f), new Vector3(1.56f, .4f, .07f), board);   // head board
            Part(PrimitiveType.Cube, t, new Vector3(0, 1, 1.3f), new Vector3(1.56f, .3f, .07f), board);        // tail board, lower
            Rod(t, new Vector3(-1.06f, .615f, -.2f), new Vector3(1.06f, .615f, -.2f), .11f, dark);            // axle
            Part(PrimitiveType.Cube, t, new Vector3(0, .7f, -.2f), new Vector3(1.3f, .14f, .16f), dark);       // bolster: the bed rests on the axle
            // The load: a heaped mound of hay in three tiers, the rick's thatch, as long and wide and high as the old round one.
            MeshPart(PropMesh("Hay load", () => Turned(new[] { new Vector2(.47f, -.1f), new Vector2(.5f, .1f), new Vector2(.43f, .3f), new Vector2(.47f, .28f), new Vector2(.27f, .52f), new Vector2(0, .62f) }, 12, 1.8f)), t, new Vector3(0, .98f, .05f), Tint(art.thatch, new Color(.84f, .72f, .42f)))
                .transform.localScale = new Vector3(1.42f, 1.15f, 2.4f);
            Solid(t, new Vector3(0, .8f, -.4f), new Vector3(2.1f, 1.6f, 3.8f));
        }
        /// <summary>A street lamp: a hewn post on a stone footing, an arm through it on an iron brace, and a roofed lantern hung round the light.</summary>
        void Lamp(Transform t, int variant)
        {
            var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            Stake(t, new Vector3(0, -.1f, 0), .2f, 3.16f, art.timber);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .1f, 0), new Vector3(.5f, .2f, .5f), Masonry);              // footing
            Part(PrimitiveType.Cube, t, new Vector3(.3f, 2.8f, 0), new Vector3(.86f, .11f, .11f), art.timber);          // the arm, its butt through the post
            Rod(t, new Vector3(.06f, 2.36f, 0), new Vector3(.5f, 2.76f, 0), .045f, iron);                              // brace
            MeshPart(PropMesh("Lantern roof", () => ZoneMeshes.Cone(.27f, .2f, 4)), t, new Vector3(.6f, 2.56f, 0), iron, Quaternion.Euler(0, 45, 0));   // its peak meets the arm
            Part(PrimitiveType.Cube, t, new Vector3(.6f, 2.4f, 0), new Vector3(.28f, .36f, .28f), art.glass);
            // Every lamp is lit after dark; variant > 0 lamps also burn by day (inn yard, mill).
            var l = new GameObject("Lamp light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(.6f, 2.3f, 0);
            l.type = LightType.Point; l.range = variant > 0 ? 9 : 11; l.intensity = 1.2f; l.color = new Color(1, .7f, .38f);
            NightLights.Add(new NightLight { light = l, dayIntensity = variant > 0 ? 1.2f : 0, nightIntensity = 1.7f });
        }
        /// <summary>Lights that brighten after dark (lamps, inn lanterns). Driven by <see cref="WorldClock"/>.</summary>
        public readonly List<NightLight> NightLights = new List<NightLight>();
        /// <summary>Unlit effects (chimney smoke, a fall's foam and mist) that take the hour's light. Made on first use.</summary>
        HourTint HourTinted { get { if (hourTint == null) hourTint = gameObject.AddComponent<HourTint>(); return hourTint; } }
        HourTint hourTint;
        /// <summary>Chicken coops in this zone; the hen-wife works them and hens roost in them.</summary>
        public readonly List<ZoneCoop> Coops = new List<ZoneCoop>();
        /// <summary>
        /// A hen coop: a small raised hut on legs with a ramp down to a pop-hole door on a hinge (front faces -Z),
        /// nest boxes on the side and a feed trough in the yard. Registered in <see cref="Coops"/>.
        /// </summary>
        void Coop(Transform t, string name)
        {
            float w = 2.6f, d = 2f, floor = .7f, h = 1.5f;
            var boards = Tint(art.timber, new Color(.44f, .31f, .2f));
            var dark = Tint(art.timber, new Color(.22f, .15f, .1f));
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 - .12f), floor / 2, sz * (d / 2 - .12f)), new Vector3(.14f, floor, .14f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(0, floor + h / 2, 0), new Vector3(w, h, d), boards);
            for (float x = -w / 2 + .3f; x < w / 2; x += .45f)
                foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(x, floor + h / 2, sz * (d / 2 + .02f)), new Vector3(.06f, h, .04f), dark);
            MeshPart(ZoneMeshes.GableRoof(w + .5f, d + .6f, .9f, .25f, .25f), t, new Vector3(0, floor + h, 0), art.thatch);
            Gables(t, w / 2, d, floor + h, .9f, d / 2 + .3f, w / 2 + .25f, boards, d, 0);   // boards to the ridge and barge boards; too small for a truss
            // Nest boxes bolted to the east side, with a lid the hen-wife lifts to collect eggs.
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 + .3f, floor + .45f, 0), new Vector3(.6f, .6f, d * .8f), boards);
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 + .32f, floor + .8f, 0), new Vector3(.7f, .08f, d * .85f), art.slate, Quaternion.Euler(0, 0, -18));
            // Pop-hole and its door on a bottom hinge; the ramp runs down to the yard.
            Part(PrimitiveType.Cube, t, new Vector3(0, floor + .35f, -d / 2 - .03f), new Vector3(.5f, .6f, .04f), Tint(art.timber, new Color(.08f, .06f, .05f)));
            var hinge = new GameObject("Coop door hinge").transform; hinge.SetParent(t, false); hinge.localPosition = new Vector3(0, floor + .05f, -d / 2 - .07f);
            Part(PrimitiveType.Cube, hinge, new Vector3(0, .3f, 0), new Vector3(.56f, .62f, .05f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(0, floor / 2, -d / 2 - .75f), new Vector3(.5f, .05f, 1.6f), boards, Quaternion.Euler(-26, 0, 0));
            // Feed trough and a grain sack out front, and a water pan the other side of the ramp (the hen-wife fills it from the well;
            // the water sinks as the day dries it).
            Part(PrimitiveType.Cube, t, new Vector3(-1.4f, .18f, -d / 2 - 1.6f), new Vector3(1.2f, .25f, .35f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(-1.4f, .3f, -d / 2 - 1.6f), new Vector3(1.1f, .04f, .25f), art.hay);
            // The grain sack (playtest note 21: one tall sphere read as a giant egg): a slumped burlap body wider than it is tall,
            // a gathered neck and its cord, leaning back against the coop.
            var burlap = Tint(art.cloth, new Color(.62f, .52f, .36f)); var sack = new GameObject("Grain sack").transform;
            sack.SetParent(t, false); sack.localPosition = new Vector3(1.5f, 0, -d / 2 - .45f); sack.localRotation = Quaternion.Euler(-8, 15, 0);
            Part(PrimitiveType.Sphere, sack, new Vector3(0, .2f, 0), new Vector3(.52f, .42f, .44f), burlap);
            Part(PrimitiveType.Sphere, sack, new Vector3(0, .38f, 0), new Vector3(.4f, .3f, .36f), burlap);
            Part(PrimitiveType.Cylinder, sack, new Vector3(0, .56f, 0), new Vector3(.13f, .07f, .13f), burlap);
            Part(PrimitiveType.Cylinder, sack, new Vector3(0, .54f, 0), new Vector3(.15f, .015f, .15f), Tint(art.timber, new Color(.35f, .27f, .17f)));   // the cord
            Part(PrimitiveType.Sphere, sack, new Vector3(0, .65f, 0), new Vector3(.17f, .1f, .17f), burlap);   // the gathered top
            var pan = new GameObject("Water pan").transform; pan.SetParent(t, false); pan.localPosition = new Vector3(1.3f, 0, -d / 2 - 1.7f);
            Part(PrimitiveType.Cylinder, pan, new Vector3(0, .05f, 0), new Vector3(.8f, .05f, .8f), Tint(art.metal, new Color(.3f, .3f, .32f)));
            var water = Part(PrimitiveType.Cylinder, pan, new Vector3(0, .06f, 0), new Vector3(.68f, .012f, .68f), Tint(art.stone, new Color(.3f, .42f, .5f)));
            water.SetActive(false);
            Solid(t, new Vector3(.2f, (floor + h + .9f) / 2, 0), new Vector3(w + .9f, floor + h + .9f, d + .2f));
            var coop = new ZoneCoop { name = name, hinge = hinge, door = t.TransformPoint(new Vector3(0, 0, -d / 2 - 1.7f)), yard = t.TransformPoint(new Vector3(0, 0, -d / 2 - 4.5f)),
                nest = t.TransformPoint(new Vector3(w / 2 + 1.1f, 0, 0)), trough = t.TransformPoint(new Vector3(-1.4f, 0, -d / 2 - 2.3f)), pan = t.TransformPoint(new Vector3(1.3f, 0, -d / 2 - 2.4f)), water = water.transform };
            coop.rampFoot = Ground(new Vector2(t.TransformPoint(new Vector3(0, 0, -d / 2 - 1.45f)).x, t.TransformPoint(new Vector3(0, 0, -d / 2 - 1.45f)).z), .03f);
            coop.popHole = t.TransformPoint(new Vector3(0, floor + .04f, -d / 2 - .05f));
            coop.SetOpen(false); Coops.Add(coop);
        }
        /// <summary>
        /// A bridge rests on its banks, not on the creek bed: the deck ends meet the higher abutment and the arch always
        /// clears the water (crest underside at least 0.6 m above the surface), whatever the creek's depth.
        /// </summary>
        void SeatBridge(Transform t, float length)
        {
            var a = t.position + t.forward * length / 2; var b = t.position - t.forward * length / 2;
            float y = Mathf.Max(HeightAt(a.x, a.z), HeightAt(b.x, b.z)) - .6f;
            if (WaterAt(new Vector2(t.position.x, t.position.z), out float surface, out _)) y = Mathf.Max(y, surface + .6f - 1.13f);
            t.position = new Vector3(t.position.x, y, t.position.z);
        }
        void Bridge(Transform t, float length)
        {
            // Stone arch: deck segments follow a gentle arc; the deck is walkable, the parapets are not. Dressed stone with a
            // coping course on the parapets and a capped pier at each corner; the coping and the piers are for the eye only.
            int segs = 8; float rise = 1.2f, width = 4; var coping = Tint(Masonry, new Color(.62f, .6f, .55f));
            for (int i = 0; i < segs; i++)
            {
                float a = (i + .5f) / segs, z = -length / 2 + length * a;
                float y = .15f + Mathf.Sin(a * Mathf.PI) * rise, slope = Mathf.Cos(a * Mathf.PI) * rise * Mathf.PI / length;
                var tilt = Quaternion.Euler(-Mathf.Atan(slope) * Mathf.Rad2Deg, 0, 0); var slab = new Vector3(width, .45f, length / segs + .08f); var block = new Vector3(.4f, .8f, length / segs + .08f);
                var deck = BoxPart(t, new Vector3(0, y, z), slab, Masonry, tilt, 1.5f);
                var col = deck.AddComponent<BoxCollider>(); col.center = Vector3.zero; col.size = slab; deck.AddComponent<NavWalkable>();   // the box the old cube had
                foreach (int s in new[] { -1, 1 })
                {
                    var at = new Vector3(s * (width / 2 + .2f), y + .55f, z);
                    var para = BoxPart(t, at, block, Masonry, tilt, 1.5f);
                    var wall = para.AddComponent<BoxCollider>(); wall.center = Vector3.zero; wall.size = block;
                    Part(PrimitiveType.Cube, t, at + tilt * new Vector3(0, .44f, 0), new Vector3(.54f, .12f, length / segs + .1f), coping, tilt);
                }
            }
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
            {
                var at = new Vector3(sx * (width / 2 + .2f), .6f, sz * (length / 2 + .2f));
                BoxPart(t, at, new Vector3(.64f, 1.9f, .64f), Masonry, null, 1.5f);
                Part(PrimitiveType.Cube, t, at + new Vector3(0, 1.01f, 0), new Vector3(.8f, .14f, .8f), coping);
            }
        }
        /// <summary>A fraction (0 to 1) made from a value a prop has already drawn and a slot number: a ruin's extra shapes are laid out by these, so nothing more is drawn from the zone's stream.</summary>
        static float RuinHash(float drawn, int slot) { return Mathf.Repeat(drawn * 91.7f + slot * .618f, 1); }
        /// <summary>Whether green still grows on old stone here: not on the ash and not in the gloom, and only with leaf-card art.</summary>
        bool Overgrown { get { return Zone.biome != "ash" && !Gloom && art.leafCards != null && art.leafCards.Length > 0; } }
        /// <summary>
        /// Ivy up a wall face: leaf cards lying a finger off the wall from <paramref name="foot"/>, their tips no higher than
        /// <paramref name="tall"/> above it, wide at the root and thinning as it climbs. <paramref name="outward"/> is the way the face looks and
        /// <paramref name="along"/> runs along the wall. Laid out by <paramref name="seed"/>, a value the wall has drawn.
        /// </summary>
        void Ivy(ZoneMeshes.Cards cards, Vector3 foot, Vector3 outward, Vector3 along, float tall, float seed)
        {
            int n = 3 + (int)(tall * 2.5f);
            for (int k = 0; k < n; k++)
            {
                float a = RuinHash(seed, 40 + k), b = RuinHash(seed * 1.7f, 60 + k), leaf = .55f + b * .35f, up = Mathf.Max(0, tall - leaf) * k / n;   // the tips stop at tall
                var at = foot + along * ((a - .5f) * (1 - .6f * k / n)) + Vector3.up * up + outward * (.03f + .02f * (k % 3));
                var climb = (Vector3.up + along * ((b - .5f) * 1.4f)).normalized;
                cards.Add(at, climb, Vector3.Cross(climb, outward), leaf, leaf, at - outward, .15f, new Color(.62f, .74f, .56f) * (.85f + a * .25f), 0, .1f);
            }
        }
        /// <summary>
        /// A length of fallen wall (it runs along x): a stub every 1.1 m, each a body of masonry under courses that step down
        /// toward its lower neighbour, the lot one mesh. Where most has come down a toppled block and a heap of broken rock lie
        /// at the foot, and ivy climbs some stubs where the biome allows. A ruin of one stub is a single leaning stone of the
        /// zone's rock. Three draws a stub, as the row of blocks took.
        /// </summary>
        void Ruin(Transform t, float length)
        {
            var stone = Dressed(new Color(.42f, .41f, .38f));
            var xs = new List<float>(); var hs = new List<float>(); var yaws = new List<float>(); var rolls = new List<float>();
            for (float x = -length / 2; x < length / 2; x += 1.1f) { xs.Add(x); hs.Add(.6f + R01 * 2.4f); yaws.Add(R01 * 6 - 3); rolls.Add(R01 * 6 - 3); }
            int n = xs.Count;
            if (n == 1)
            {
                // One stone standing alone: a crag lump drawn up tall, sunk a little and leaning the way the block was turned.
                float tall = hs[0] + .55f, sy = tall / .68f, wide = .8f + RuinHash(hs[0], 1) * .3f;
                MeshPart(CragRock((int)(RuinHash(hs[0], 2) * 6)), t, new Vector3(0, LocalGround(t, 0, 0) - .35f + .2f * sy, 0), SecretStone(1.05f), Quaternion.Euler(yaws[0] * 2.5f, RuinHash(hs[0], 3) * 360, rolls[0] * 2.5f)).transform.localScale = new Vector3(wide, sy, wide * .62f);
            }
            else
            {
                var blocks = new List<CombineInstance>(); var rubble = SecretStone(.92f); var ivy = new ZoneMeshes.Cards(); float green = !Overgrown ? 0 : Zone.biome == "mountain" ? .2f : .4f;
                for (int i = 0; i < n; i++)
                {
                    float x = xs[i], h = hs[i], g = LocalGround(t, x, 0) - .15f;   // each stub stands on (and a little into) the ground under it
                    float before = i > 0 ? hs[i - 1] : h * .5f, after = i + 1 < n ? hs[i + 1] : h * .5f, top = h * .5f + (before + after) * .25f + .15f;
                    int courses = Mathf.Clamp(Mathf.RoundToInt(top * 1.5f), 1, 4), toward = after > before ? 1 : -1; float body = top - courses * .3f;
                    var turn = Quaternion.Euler(0, yaws[i], rolls[i]);
                    blocks.Add(Ashlar(new Vector3(x, g + body / 2, 0), new Vector3(1.12f, body, .8f), 1.5f, turn));
                    for (int j = 0; j < courses; j++)
                    {
                        float len = 1.12f * (1 - (j + .5f + RuinHash(h, j) * .5f) / (courses + .6f));
                        blocks.Add(Ashlar(new Vector3(x + toward * (1.12f - len) / 2, g + body + j * .3f + .13f, (RuinHash(h, j + 5) - .5f) * .08f), new Vector3(len, .34f, .78f - j * .04f), 1.5f, turn));
                    }
                    int side = RuinHash(h, 9) < .5f ? -1 : 1;
                    if (h < 1.9f)
                    {
                        float bx = x + (RuinHash(h, 10) - .5f) * .6f, bz = side * (.62f + RuinHash(h, 11) * .16f);
                        blocks.Add(Ashlar(new Vector3(bx, LocalGround(t, bx, bz) + .1f, bz), new Vector3(.55f, .3f, .38f), 1.5f, Quaternion.Euler(RuinHash(h, 12) * 24 - 12, RuinHash(h, 13) * 360, RuinHash(h, 14) * 30 - 15)));
                        for (int k = 0; k < 2; k++)
                        {
                            float s = .32f + RuinHash(h, 15 + k) * .26f, rx = x + (RuinHash(h, 17 + k) - .5f) * 1f, rz = (k == 0 ? -side : side) * (.5f + RuinHash(h, 19 + k) * .2f);
                            Lump(BoulderAt((int)(RuinHash(h, 21 + k) * 6)), t, new Vector3(rx, LocalGround(t, rx, rz) + .18f * s * .7f - .05f, rz), new Vector3(s * 1.25f, s * .7f, s), rubble, RuinHash(h, 23 + k) * 360);
                        }
                    }
                    else if (RuinHash(h, 8) < green) Ivy(ivy, new Vector3(x, g + .1f, side * .43f), new Vector3(0, 0, side), Vector3.right, body + .25f, h);
                }
                Stonework("Ruin stone", t, stone, blocks.ToArray());
                if (ivy.Count > 0) MeshPart(ivy.Build("Ivy"), t, Vector3.zero, LeafMaterial(art.leafCards[0], new Color(.5f, .62f, .44f)));
            }
            Solid(t, new Vector3(0, 1.2f, 0), new Vector3(length, 2.4f, 1));
        }
        /// <summary>
        /// A wayside shrine (front faces -Z): two stone steps and a squared pillar carrying a little gabled niche with a pale
        /// figure in it and a sun-disc on the gable; a bowl, two candle stubs and offerings gone grey at its foot.
        /// </summary>
        void Wayshrine(Transform t)
        {
            var stone = Tint(art.stone, new Color(.6f, .57f, .5f)); var dark = Tint(art.stone, new Color(.12f, .11f, .1f));
            // Two steps (the lower deep enough to sit on a slope), the pillar, a cap course on it and the niche house: small dressed stone, one mesh.
            Stonework("Wayshrine stone", t, Dressed(new Color(.6f, .57f, .5f)), Ashlar(new Vector3(0, .02f, 0), new Vector3(2.2f, .5f, 2.2f), 1), Ashlar(new Vector3(0, .35f, .15f), new Vector3(1.5f, .22f, 1.5f), 1),
                Ashlar(new Vector3(0, 1.23f, .25f), new Vector3(.5f, 1.6f, .5f), 1), Ashlar(new Vector3(0, 1.95f, .25f), new Vector3(.66f, .12f, .66f), 1), Ashlar(new Vector3(0, 2.4f, .25f), new Vector3(.9f, .8f, .75f), 1));
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.38f, -.13f), new Vector3(.6f, .56f, .04f), dark);
            Part(PrimitiveType.Capsule, t, new Vector3(0, 2.36f, -.17f), new Vector3(.18f, .2f, .1f), Tint(art.stone, new Color(.86f, .84f, .78f)));
            MeshPart(ZoneMeshes.GableRoof(1.25f, 1.15f, .45f, .12f), t, new Vector3(0, 2.8f, .25f), art.slate, Quaternion.Euler(0, 90, 0));   // a cap on the niche: closed ends
            foreach (int s in new[] { -1, 1 }) MeshPart(PropMesh("Wayshrine gable", () => ZoneMeshes.Gable(1.15f, .45f, .04f, 1)), t, new Vector3(0, 2.8f, .25f + s * .635f), Dressed(new Color(.6f, .57f, .5f)));   // faced in the shrine's stone, 3 cm proud
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 3.42f, -.28f), new Vector3(.34f, .03f, .34f), stone, Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .5f, -.35f), new Vector3(.46f, .05f, .46f), dark);
            Part(PrimitiveType.Sphere, t, new Vector3(0, .55f, -.35f), new Vector3(.34f, .12f, .34f), art.ash);
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cylinder, t, new Vector3(s * .5f, .56f, -.38f), new Vector3(.08f, .1f, .08f), Tint(art.stone, new Color(.82f, .8f, .72f)));
                Part(PrimitiveType.Sphere, t, new Vector3(s * .5f, .7f, -.38f), new Vector3(.05f, .08f, .05f), art.glass);
            }
            // Offerings on the lower step. Fifteen draws, as the old ruin wall here took, so the rest of the zone keeps its layout.
            for (int i = 0; i < 5; i++) { float s = .12f + R01 * .1f; Part(PrimitiveType.Sphere, t, new Vector3(i * .4f - .8f + R01 * .16f, .27f + s * .3f, -.85f + R01 * .2f), new Vector3(s, s * .7f, s), art.ash); }
            Solid(t, new Vector3(0, 1.4f, .1f), new Vector3(1.5f, 2.8f, 1.5f));
        }

        void RuinedHouse(Transform t, Vector2 size)
        {
            // A fallen building: broken walls with a ragged, scorched skyline on a stone footing, charred corner posts, the door
            // frame still standing, one end left as a broken gable, rubble at the foot; a charred remnant of roof slumped
            // inside and fallen beams.
            float w = size.x, d = size.y; var plaster = Tint(art.plaster, Zone.biome == "ash" ? new Color(.56f, .555f, .55f) : new Color(.52f, .48f, .42f)); var charred = Tint(art.timber, new Color(.13f, .11f, .1f));
            // The floor slab inside, and round it a stepped stone footing down to the ground on a slope (Footing): no slab
            // overhangs a hillside.
            var flags = Dressed(new Color(.52f, .51f, .48f));
            BoxPart(t, new Vector3(0, .45f, 0), new Vector3(w - .5f, .3f, d - .5f), flags, null, 1.5f);
            Footing(t, w + .3f, d + .3f, .6f, .45f, 0, 0, null, flags);
            // The heights the wall stubs drew, run by run (front, back, the -x end, the +x end): the same draws in the same
            // order as the stubs took, and none for the doorway's (-1 marks them).
            var runs = new[] { new List<float>(), new List<float>(), new List<float>(), new List<float>() };
            foreach (int sz in new[] { -1, 1 })
                for (float x = -w / 2 + .6f; x < w / 2; x += 1.2f) runs[sz < 0 ? 0 : 1].Add(sz < 0 && Mathf.Abs(x) < 1 ? -1 : .8f + R01 * 2.4f);
            foreach (int sx in new[] { -1, 1 })
                for (float z = -d / 2 + .6f; z < d / 2; z += 1.2f) runs[sx < 0 ? 2 : 3].Add(.8f + R01 * 2.8f);
            var walls = new List<CombineInstance>(); var scorch = new List<CombineInstance>(); var footing = new List<CombineInstance>(); var ends = new float[4, 2];
            var rubble = SecretStone(.95f); var ivy = new ZoneMeshes.Cards(); float green = !Overgrown ? 0 : Zone.biome == "mountain" ? .2f : .45f;
            Mesh Slab(Vector2[] outline) { var m = Cutout(outline, .35f); var uv = m.uv; for (int k = 0; k < uv.Length; k++) uv[k] *= .5f; m.uv = uv; return m; }   // the plaster at the houses' scale (2 m a tile)
            // One wall: it runs along its own x from at (the middle of its foot, on the floor slab), turned about y; outward is
            // the side of its own z that looks out of the house. Each 1.2 m stub gives the skyline three points (its two
            // ends and a peak or dip between), so the top slopes and steps raggedly instead of standing in square teeth.
            void Run(int run, Vector3 at, float turn, int outward, float span, bool gable)
            {
                var hs = runs[run]; int n = hs.Count, keep = RuinHash(hs[0], 6) < .5f ? -1 : 1; float reach = span / 2, win = -99, leanL = 0, leanR = 0, lean = 0;
                var face = Quaternion.Euler(0, turn, 0); var top = new List<Vector2>();
                Vector3 P(float x, float y, float z = 0) { return at + face * new Vector3(x, y, z); }
                float E(int i) { return i >= n ? reach : Mathf.Min(-reach + i * 1.2f, reach); }                    // where stub i starts; the last one ends at the corner
                float G(float x) { return 2.3f + (1 - Mathf.Abs(x) / reach) * span * .4f; }                         // the gable's line
                bool Split(float a, float b) { return RuinHash(a + b, 4) < .4f; }                                   // a break between two stubs: each ends at its own height
                // The back wall keeps a window if a stub away from the corners stands high enough to hold its head.
                if (run == 1) { int best = -1; for (int i = 1; i + 1 < n; i++) if (hs[i] >= 2.6f && (best < 0 || hs[i] > hs[best])) best = i; if (best >= 0) win = (E(best) + E(best + 1)) / 2; }
                void Quad(float xa, float xb, float y0, float ya, float yb, Quaternion rot, bool burnt)
                {
                    walls.Add(Piece(Slab(new[] { new Vector2(xa, y0), new Vector2(xa, ya), new Vector2(xb, yb), new Vector2(xb, y0) }), at, rot));
                    if (burnt) scorch.Add(Piece(Cutout(new[] { new Vector2(xa, Mathf.Max(y0, ya - .26f)), new Vector2(xa, ya + .012f), new Vector2(xb, yb + .012f), new Vector2(xb, Mathf.Max(y0, yb - .26f)) }, .39f), at, rot));
                }
                // A stretch of wall between corners and the doorway: its footing course, then the plaster under each span of the skyline.
                void Flush(float to)
                {
                    if (top.Count == 0) return;
                    float from = top[0].x, a = from + (from < -reach + .01f ? (run < 2 ? -.235f : .235f) : 0), b = to + (to > reach - .01f ? (run < 2 ? .235f : -.235f) : 0);   // front and back take the corners
                    footing.Add(Ashlar(P((a + b) / 2, .2f), new Vector3(b - a, .4f, .47f), 1.5f, face));
                    if (win > -90)
                        foreach (float x in new[] { win - .38f, win + .38f })
                            for (int k = 0; k + 1 < top.Count; k++)
                                if (top[k].x < x - .02f && top[k + 1].x > x + .02f) { top.Insert(k + 1, new Vector2(x, Mathf.Lerp(top[k].y, top[k + 1].y, Mathf.InverseLerp(top[k].x, top[k + 1].x, x)))); break; }
                    for (int k = 0; k + 1 < top.Count; k++)
                    {
                        Vector2 p = top[k], q2 = top[k + 1]; if (q2.x - p.x < .02f) continue;
                        float m = (p.x + q2.x) / 2; var rot = m > leanL && m < leanR ? face * Quaternion.Euler(lean, 0, 0) : face;
                        if (win > -90 && Mathf.Abs(m - win) < .38f) { Quad(p.x, q2.x, 0, .9f, .9f, rot, false); if (p.y > 2.1f && q2.y > 2.1f) Quad(p.x, q2.x, 1.9f, p.y, q2.y, rot, true); }   // under the sill, over the head
                        else Quad(p.x, q2.x, 0, p.y, q2.y, rot, true);
                    }
                    top.Clear();
                }
                for (int i = 0; i < n; i++)
                {
                    float h = hs[i], l = E(i), r = E(i + 1);
                    if (h < 0) { Flush(l); continue; }   // the doorway
                    bool joinL = i > 0 && hs[i - 1] >= 0, joinR = i + 1 < n && hs[i + 1] >= 0, cutL = !joinL || Split(hs[i - 1], h), cutR = !joinR || Split(h, hs[i + 1]);
                    float mid = (l + r) / 2 + (RuinHash(h, 1) - .5f) * (r - l) * .5f, hm = h;
                    float hl = cutL ? h * (.6f + RuinHash(h, 3) * .3f) : Mathf.Lerp(Mathf.Min(hs[i - 1], h), Mathf.Max(hs[i - 1], h), .35f);
                    float hr = cutR ? h * (.62f + RuinHash(h, 5) * .3f) : Mathf.Lerp(Mathf.Min(h, hs[i + 1]), Mathf.Max(h, hs[i + 1]), .35f);
                    if (win >= l && win <= r) { hl = Mathf.Max(hl, 2.2f); hr = Mathf.Max(hr, 2.2f); mid = (l + r) / 2; }   // the window's stub keeps its head
                    if (gable)
                    {
                        // The gable stands whole on one side of the ridge and a little past it; the rest is down to what the stubs drew.
                        if (l < 0 && r > 0) mid = 0;
                        hl = l * keep <= .9f ? G(l) : Mathf.Min(hl, G(l)); hm = mid * keep <= .9f ? G(mid) : Mathf.Min(hm, G(mid)); hr = r * keep <= .9f ? G(r) : Mathf.Min(hr, G(r));
                    }
                    else if (lean == 0 && cutL && cutR && i > 0 && i < n - 1 && h > 1.4f && (win < l || win > r)) { leanL = l; leanR = r; lean = outward * (4 + RuinHash(h, 7) * 6); }   // broken free at both ends: it leans out
                    if (i == 0) ends[run, 0] = hl;
                    if (i == n - 1) ends[run, 1] = hr;
                    top.Add(new Vector2(l, hl)); top.Add(new Vector2(mid, hm)); top.Add(new Vector2(r, hr));
                    float xc = (l + r) / 2;
                    if (!gable && h < 1.7f)
                        for (int k = 0; k < 2; k++)
                        {
                            // Where the wall is low, what fell lies in a heap against its foot outside.
                            float s = .34f + RuinHash(h, 10 + k) * .2f; var p = P(xc + (RuinHash(h, 12 + k) - .5f) * .9f, 0, outward * (.4f + k * .08f));
                            Lump(BoulderAt((int)(RuinHash(h, 14 + k) * 6)), t, new Vector3(p.x, LocalGround(t, p.x, p.z) + .18f * s * .7f - .05f, p.z), new Vector3(s * 1.25f, s * .7f, s), rubble, RuinHash(h, 16 + k) * 360);
                        }
                    else if (RuinHash(h, 8) < green && (win < l || win > r)) Ivy(ivy, P(xc, -.35f, outward * .26f), face * new Vector3(0, 0, outward), face * Vector3.right, (gable ? 2.3f : h) * .85f + .35f, h);
                }
                Flush(reach);
                if (win > -90)
                {
                    Part(PrimitiveType.Cube, t, P(win, 1.97f), new Vector3(1, .14f, .43f), charred, face);
                    Part(PrimitiveType.Cube, t, P(win, .93f), new Vector3(.9f, .06f, .45f), charred, face);
                }
                if (gable)
                {
                    // The rafters of the gable's standing side, charred, on its inner face; a stub of the other past the ridge.
                    var ridge = P(0, G(0) - .12f, -outward * .26f);
                    Bar(t, P(-keep * (reach - .1f), 2.2f, -outward * .26f), ridge, .18f, .14f, charred);
                    Bar(t, ridge, P(keep * 1.1f, G(1.1f) - .12f, -outward * .26f), .18f, .14f, charred);
                }
            }
            Run(0, new Vector3(0, .6f, -d / 2), 0, -1, w, false); Run(1, new Vector3(0, .6f, d / 2), 0, 1, w, false);
            Run(2, new Vector3(-w / 2, .6f, 0), -90, 1, d, false); Run(3, new Vector3(w / 2, .6f, 0), -90, -1, d, true);
            Stonework("Ruined walls", t, plaster, walls.ToArray());
            Stonework("Scorched wall tops", t, Tint(art.plaster, Zone.biome == "ash" ? new Color(.2f, .2f, .2f) : new Color(.19f, .16f, .13f)), scorch.ToArray());
            Stonework("Ruined footing", t, Dressed(new Color(.5f, .49f, .46f)), footing.ToArray());
            if (ivy.Count > 0) MeshPart(ivy.Build("Ivy"), t, Vector3.zero, LeafMaterial(art.leafCards[0], new Color(.5f, .62f, .44f)));
            // A charred post at each corner, a little above the taller of the two walls that meet there.
            for (int k = 0; k < 4; k++)
            {
                int cx = k < 2 ? 0 : 1, cz = k % 2; float tall = Mathf.Max(ends[cz, cx], ends[2 + cx, cz]) + .22f;
                Part(PrimitiveType.Cube, t, new Vector3((cx * 2 - 1) * w / 2, .6f + tall / 2, (cz * 2 - 1) * d / 2), new Vector3(.44f, tall, .44f), charred);
            }
            // The door frame still stands where the doorway was: two charred posts and the lintel, a little askew.
            int doorFrom = runs[0].IndexOf(-1), doorTo = runs[0].LastIndexOf(-1);
            if (doorFrom >= 0)
            {
                float gl = -w / 2 + doorFrom * 1.2f, gr = Mathf.Min(-w / 2 + (doorTo + 1) * 1.2f, w / 2);
                foreach (float px in new[] { gl + .11f, gr - .11f }) Part(PrimitiveType.Cube, t, new Vector3(px, 1.65f, -d / 2), new Vector3(.22f, 2.1f, .42f), charred);
                Part(PrimitiveType.Cube, t, new Vector3((gl + gr) / 2, 2.79f, -d / 2), new Vector3(gr - gl + .36f, .22f, .46f), charred, Quaternion.Euler(0, 0, 2));
            }
            // The roof fell in: a section lies in one end of the shell, its low eave on the floor and its high side on a charred
            // post, clear of the walls and never through the floor slab (a tilted slab used to push out under the base).
            var fallen = Quaternion.Euler(0, 6, -15); float rw = w * .55f, rd = d * .6f, rh = 1.2f; Vector3 lo = Vector3.one * 99, hi = -lo;
            foreach (int sx in new[] { -1, 1 }) foreach (var q in new[] { new Vector3(0, -.25f, -1), new Vector3(0, -.25f, 1), new Vector3(0, rh, 0) })
            { var c = fallen * new Vector3(sx * rw / 2, q.y, q.z * rd / 2); lo = Vector3.Min(lo, c); hi = Vector3.Max(hi, c); }
            float hx = w / 2 - .45f, hz = d / 2 - .45f;   // inside the wall stubs, with a margin
            var roofAt = new Vector3(-hx - lo.x, .6f - lo.y - .05f, -(lo.z + hi.z) / 2);
            MeshPart(ZoneMeshes.GableRoof(rw, rd, rh), t, roofAt, Tint(art.thatch, new Color(.24f, .2f, .15f)), fallen);
            var post = roofAt + fallen * new Vector3(-rw / 2 + .35f, -.25f, 0);
            if (post.y > .7f) Part(PrimitiveType.Cube, t, new Vector3(post.x, (.6f + post.y) / 2, post.z), new Vector3(.2f, post.y - .6f, .2f), charred);
            // Fallen joists lie side by side on the floor in the open end, with one fallen across them: inside the walls and
            // resting on something (random beams used to run through the walls and hang out over the grass).
            float x0 = roofAt.x + hi.x + .15f, lane = (hx - x0) / 3;
            for (int i = 0; i < 4; i++)
            {
                float fx = R01, fz = R01, len = 3 + R01 * 2; _ = R01; float yaw = R01; _ = R01;   // the same six draws as before, so everything built after stays put
                if (lane < .38f) continue;
                if (i < 3) { float half = Mathf.Min(len / 2, hz - .1f); Part(PrimitiveType.Cube, t, new Vector3(x0 + (i + .5f + (fx - .5f) * .2f) * lane, .72f, (fz * 2 - 1) * (hz - half)), new Vector3(.25f, .25f, half * 2), charred, Quaternion.Euler(0, (yaw - .5f) * 3, 0)); }
                else { float half = Mathf.Min(len / 2, (hx - x0) / 2 + .05f); Part(PrimitiveType.Cube, t, new Vector3((x0 + hx) / 2, .97f, (fz * 2 - 1) * .35f), new Vector3(.25f, .25f, half * 2), charred, Quaternion.Euler(0, 90 + (yaw - .5f) * 16, 0)); }
            }
            Solid(t, new Vector3(0, 1.5f, 0), new Vector3(w + .4f, 3, d + .4f));
        }
        /// <summary>Stone curtain wall along a polyline: a plinth course at its foot, a coping under the battlements and
        /// crenellations, each run one mesh of dressed stone.</summary>
        void Wall(Vector2[] pts, Transform parent)
        {
            if (pts == null || pts.Length < 2) return;
            var stone = Dressed(new Color(.46f, .45f, .42f));
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1]; float len = Vector2.Distance(a, b); if (len < .1f) continue;
                var seg = new GameObject("Wall").transform; seg.SetParent(parent, false);
                var mid = (a + b) / 2; seg.position = Ground(mid); seg.rotation = Quaternion.LookRotation(new Vector3(b.x - a.x, 0, b.y - a.y));
                // Down to the lowest ground along the run, so a wall across a slope (or off a levelled gate pad) never shows daylight under it.
                float low = seg.position.y; for (int k = 0; k <= 8; k++) { var q = Vector2.Lerp(a, b, k / 8f); low = Mathf.Min(low, HeightAt(q.x, q.y)); }
                float sink = seg.position.y - low + .4f;
                var blocks = new List<CombineInstance> { Ashlar(new Vector3(0, (3.9f - sink) / 2, 0), new Vector3(1.2f, 3.9f + sink, len + .6f)),
                    Ashlar(new Vector3(0, (.55f - sink) / 2, 0), new Vector3(1.4f, .55f + sink, len + .7f)),   // the plinth course, as wide as the collider
                    Ashlar(new Vector3(0, 3.84f, 0), new Vector3(1.4f, .2f, len + .7f)) };                       // the coping the merlons stand on
                for (float z = -len / 2 + .5f; z < len / 2; z += 1.4f)
                    foreach (int s in new[] { -1, 1 }) blocks.Add(Ashlar(new Vector3(s * .45f, 4.2f, z), new Vector3(.35f, .6f, .7f)));
                Stonework("Wall stone", seg, stone, blocks.ToArray());
                Solid(seg, new Vector3(0, 2, 0), new Vector3(1.4f, 5, len + .4f));
            }
        }
        /// <summary>
        /// A Silent Statue (CANON, world_bible.md: giant faceless monoliths standing in the Wasting): a robed, hooded figure of pale
        /// weathered stone, about 10 m tall at scale 1, all one body from the hem of its robe to the crown of its hood, and nothing
        /// in the hood but shadow. It bows a little, and stands to the knees in the rubble of the ground it stood in. Draws from
        /// its own stream (BuildProps).
        /// </summary>
        void Monolith(Transform t)
        {
            var stone = Tint(art.stone, new Color(.56f, .56f, .58f)); var rubble = Tint(art.stone, new Color(.42f, .42f, .44f));
            var hollow = Tint(art.stone, new Color(.06f, .06f, .07f));
            if (statue == null)
                // The outline, hem to crown (height, radius): the robe falls wide, narrows to the waist, swells over the shoulders,
                // pinches at the neck under the hood, and the hood rounds off, a little peaked.
                statue = Lathe("Silent Statue", new[] { new Vector2(0, 1.95f), new Vector2(.6f, 1.85f), new Vector2(2, 1.6f), new Vector2(3.6f, 1.36f),
                    new Vector2(5.2f, 1.2f), new Vector2(6.3f, 1.22f), new Vector2(6.9f, 1.42f), new Vector2(7.35f, 1.3f), new Vector2(7.75f, .86f),
                    new Vector2(8.05f, .84f), new Vector2(8.6f, .92f), new Vector2(9.15f, .86f), new Vector2(9.6f, .66f), new Vector2(9.95f, .36f),
                    new Vector2(10.15f, .08f) }, 18, .74f);
            var bow = Quaternion.Euler(4, 0, -1.5f);
            MeshPart(statue, t, Vector3.zero, stone, bow);
            Part(PrimitiveType.Sphere, t, bow * new Vector3(0, 8.55f, .5f), new Vector3(.95f, 1.25f, .5f), hollow, bow);   // the hood's empty front
            for (int k = 0; k < 7; k++)
            {
                float a = (k * 51 + R01 * 25) * Mathf.Deg2Rad, r = 1.8f + R01 * .6f, size = .8f + R01 * .7f;
                Lump(BoulderAt(k), t, new Vector3(Mathf.Cos(a) * r, .2f + R01 * .2f, Mathf.Sin(a) * r * .85f), Vector3.one * size, rubble, R01 * 360);
            }
            Solid(t, new Vector3(0, 4, 0), new Vector3(2.8f, 8, 2.2f));
        }
        Mesh statue;
        /// <summary>A smooth surface of revolution round +y through <paramref name="profile"/> (height, radius) points, squashed
        /// front to back (z) by <paramref name="depth"/>, closed at the bottom.</summary>
        static Mesh Lathe(string name, Vector2[] profile, int sides, float depth)
        {
            var v = new List<Vector3>(); var t = new List<int>(); int row = sides + 1;
            foreach (var pt in profile)
                for (int k = 0; k <= sides; k++) { float a = k * Mathf.PI * 2 / sides; v.Add(new Vector3(Mathf.Cos(a) * pt.y, pt.x, Mathf.Sin(a) * pt.y * depth)); }
            for (int r = 0; r + 1 < profile.Length; r++)
                for (int k = 0; k < sides; k++) { int a = r * row + k, b = a + row; t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            int c = v.Count; v.Add(Vector3.zero); for (int k = 0; k < sides; k++) t.AddRange(new[] { c, k, k + 1 });   // the base
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
        void Tower(Transform t, float diameter)
        {
            float h = 7.5f, r = diameter / 2; var stone = Dressed(new Color(.44f, .43f, .41f)); var slit = Tint(art.timber, new Color(.05f, .04f, .03f));
            float drop = FootDrop(t, diameter * .8f, diameter * .8f);   // on a slope the tower's foot reaches the lowest ground at its base
            // The drum, turned in one piece: a plinth course at the foot, the shaft (the old cylinder's girth), and a corbelled
            // ring under the battlements. The stone tiles in metres round it and up it.
            var drum = Turned(new[] { new Vector2(r + .14f, -drop), new Vector2(r + .14f, .5f), new Vector2(r, .68f), new Vector2(r, h - 1), new Vector2(r + .16f, h - .8f), new Vector2(r + .16f, h), new Vector2(0, h) }, 20, 1.75f);
            drum.name = "Tower drum"; MeshPart(drum, t, Vector3.zero, stone);
            var merlons = new CombineInstance[10];
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36 * Mathf.Deg2Rad;
                merlons[i] = Piece(ZoneMeshes.Box(new Vector3(.6f, .7f, .6f), 1.75f), new Vector3(Mathf.Cos(a) * diameter * .46f, h + .35f, Mathf.Sin(a) * diameter * .46f), Quaternion.Euler(0, -i * 36, 0));
            }
            Stonework("Tower battlements", t, stone, merlons);
            MeshPart(PropMesh("Tower roof " + Mathf.RoundToInt(diameter * 100), () => ZoneMeshes.Spire(diameter * .55f, 3.2f)), t, new Vector3(0, h + .2f, 0), art.slate);
            Part(PrimitiveType.Cube, t, new Vector3(0, 4.5f, -diameter / 2 - .02f), new Vector3(.4f, .9f, .1f), art.glass);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * r, 3.3f, 0), new Vector3(.1f, 1, .16f), slit);   // arrow slits to the sides and the back
            Part(PrimitiveType.Cube, t, new Vector3(0, 3.3f, r), new Vector3(.16f, 1, .1f), slit);
            var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, h / 2, 0); cap.height = h + 3; cap.radius = diameter / 2;
            t.gameObject.AddComponent<NavBlocker>();
        }
        void Gallows(Transform t)
        {
            // The Gallows Tree: a leafless tree with a heavy limb, a rope and a wooden platform beneath.
            // The tree on its own child, so only the tree fades for the camera, not the platform and rope.
            var tree = new GameObject("Gallows tree").transform; tree.SetParent(t, false);
            DeadTree(Vector2.zero, .75f, Tint(art.bark, new Color(.2f, .17f, .15f)), 3, statics, tree);
            Part(PrimitiveType.Cylinder, t, new Vector3(1.6f, 3.6f, 0), new Vector3(.28f, 1.7f, .28f), art.bark, Quaternion.Euler(0, 0, 80));
            Part(PrimitiveType.Cylinder, t, new Vector3(2.8f, 2.7f, 0), new Vector3(.05f, .75f, .05f), Tint(art.cloth, new Color(.45f, .38f, .28f)));
            Part(PrimitiveType.Cube, t, new Vector3(2.8f, .5f, 0), new Vector3(2.4f, .15f, 2.4f), art.timber);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(2.8f + sx, .25f, sz), new Vector3(.18f, .5f, .18f), art.timber);
        }
        /// <summary>
        /// A stone crypt (door faces -Z) with grave markers before it. variant 1: a barrow, an earthen mound ringed with kerb
        /// stones, the slab that sealed its black doorway thrown down in front.
        /// </summary>
        void Crypt(Transform t, int variant = 0)
        {
            var stone = Dressed(new Color(.36f, .36f, .37f));
            if (variant == 1)
            {
                // On a slope the mound reaches down past the lowest ground round its foot (its top drops only half as far).
                float e = LocalGround(t, 0, -2.6f), sink = Mathf.Min(FootDrop(t, 5.2f, 6, 2.2f), 1.2f);
                Part(PrimitiveType.Sphere, t, new Vector3(0, -.2f - sink, 2.2f), new Vector3(7.4f, 5.8f + sink, 8.6f), Tint(art.foliage, new Color(.3f, .31f, .19f)));
                Part(PrimitiveType.Cube, t, new Vector3(0, e + 1.05f, -2.15f), new Vector3(1.6f, 2.2f, .1f), Tint(art.metal, new Color(.04f, .04f, .05f)));
                Part(PrimitiveType.Cube, t, new Vector3(0, e + 1.05f, -1.2f), new Vector3(1.6f, 2.2f, 1.9f), Tint(art.metal, new Color(.04f, .04f, .05f)));   // the dark passage behind the door
                stone = RockTint(new Color(.36f, .36f, .37f));   // a barrow is single great stones, not coursed work
                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * 1.05f, e + 1.1f, -1.2f), new Vector3(.5f, 2.3f, 2.6f), stone);   // jambs run back into the mound
                Part(PrimitiveType.Cube, t, new Vector3(0, e + 2.4f, -.9f), new Vector3(2.9f, .45f, 3.2f), stone);
                Part(PrimitiveType.Cube, t, new Vector3(0, e - .1f, -2.9f), new Vector3(2.6f, .4f, 1.2f), stone);
                Part(PrimitiveType.Cube, t, new Vector3(1.9f, e - .02f, -3.3f), new Vector3(1.6f, .36f, 2.1f), stone, Quaternion.Euler(0, 20, 0));   // the sealing slab, thrown down
                for (int i = 0; i < 9; i++)   // kerb stones round the mound, open at the door
                {
                    float a = 40 + i * 35, kx = Mathf.Sin(a * Mathf.Deg2Rad) * 3.9f, kz = 2.2f - Mathf.Cos(a * Mathf.Deg2Rad) * 4.5f;
                    Part(PrimitiveType.Cube, t, new Vector3(kx, LocalGround(t, kx, kz) + .2f, kz), new Vector3(.7f, .55f, .45f), stone, Quaternion.Euler(0, -a, (i % 3 - 1) * 5));
                }
            }
            else
            {
                float drop = FootDrop(t, 6, 6, 1.5f);   // on a slope the vault reaches down to the lowest ground under it
                // The vault, a cornice under the eaves, the door's pillars and three steps: one mesh of dressed stone.
                var blocks = new List<CombineInstance> { Ashlar(new Vector3(0, 1.2f - drop / 2, 1.5f), new Vector3(6, 2.4f + drop, 6)), Ashlar(new Vector3(0, 2.09f, 1.5f), new Vector3(6.3f, .22f, 6.3f)) };
                foreach (int s in new[] { -1, 1 }) blocks.Add(Ashlar(new Vector3(s * 1.4f, 1.3f, -1.7f), new Vector3(.6f, 2.6f, .6f)));
                for (int i = 0; i < 3; i++) blocks.Add(Ashlar(new Vector3(0, .1f + i * .12f, -2.6f + i * .35f), new Vector3(2.6f, .2f, .4f)));
                Stonework("Crypt stone", t, stone, blocks.ToArray());
                MeshPart(ZoneMeshes.GableRoof(6.6f, 6.6f, 1.6f), t, new Vector3(0, 2.4f, 1.5f), Tint(art.slate, new Color(.27f, .28f, .3f)));   // the vault fills it: closed ends
                foreach (int s in new[] { -1, 1 }) MeshPart(PropMesh("Crypt pediment", () => ZoneMeshes.Gable(6.6f, 1.6f, .06f, 1.75f, 0, 2.4f)), t, new Vector3(s * 3.31f, 2.4f, 1.5f), stone, Quaternion.Euler(0, s * 90, 0));   // a pediment of the vault's stone, 4 cm proud of each end
                Part(PrimitiveType.Cube, t, new Vector3(0, 1, -1.52f), new Vector3(1.8f, 2, .1f), Tint(art.metal, new Color(.14f, .14f, .16f)));
            }
            // Grave markers before it (the same five draws each, in the same order), set on the ground where they stand: headstones, each one slab.
            var slab = RockTint(new Color(.36f, .36f, .37f));
            for (int i = 0; i < 6; i++) { float gx = R01 * 10 - 5, gz = -4 - R01 * 4; MeshPart(Headstone, t, new Vector3(gx, LocalGround(t, gx, gz) + .46f, gz), slab, Quaternion.Euler(R01 * 10 - 5, R01 * 30 - 15, R01 * 10 - 5)); }
            if (variant == 1) Solid(t, new Vector3(0, 1.2f, 1.85f), new Vector3(6.8f, 2.4f, 8.7f)); else Solid(t, new Vector3(0, 1.8f, 1.2f), new Vector3(6.2f, 3.6f, 6.6f));
        }
        /// <summary>
        /// A crag: a broken escarpment of lumpy rock. Each 2.2 m step of it is a stack of overlapping faceted lumps (CragRock, the
        /// stone tiled on them at about 2 m): a wide base bulging a little forward, an upper mass set back and leaning, and on
        /// two steps in three a crest knob, some standing well over the crest; so the face is stepped and knobbly and its crest
        /// ragged, with no flat top anywhere. On mountains each step stands on the lower of the ground at its front and back, so
        /// none hangs off a slope. With a lift (CliffLift raises a shelf behind it, on local +z, or -z when the lift is negative)
        /// it is a scarp instead: a stepped rock face that is the slope, each step sunk .6 m under the lowest ground along its
        /// foot and rising to the shelf's ground a metre behind it and a little over, its lumps leaning back into the shelf; a
        /// second, lower course of lumps sits sunk into the shelf's lip behind, so from below the face is rock from foot to crest
        /// and no smooth ground shows between or above the lumps. Fallen blocks and scree lie along the foot (Scree). The zone's
        /// seven draws per step are taken as before and set the step's envelope, so its lowest foot and the colliders are exactly
        /// what they were; every other choice comes from the prop's own stream, keyed on its position.
        /// </summary>
        void Cliff(Transform t, float length, float lift = 0)
        {
            var stone = RockTint(Zone.biome == "ash" ? new Color(.37f, .365f, .37f) : Zone.biome == "mountain" ? new Color(.35f, .335f, .31f) : new Color(.4f, .38f, .35f));   // ash: grey, not sandstone; mountain: darker, so a sunlit crag stays under the fog
            bool seat = Zone.biome == "mountain"; float side = Mathf.Sign(lift), rise = Mathf.Abs(lift), low = 0;
            float Y(float x, float z) { var w = t.TransformPoint(new Vector3(x, 0, z)); return HeightAt(w.x, w.z) - t.position.y; }
            var own = new System.Random(Zone.seed ^ (Mathf.RoundToInt(t.position.x * 8) * 73856093) ^ (Mathf.RoundToInt(t.position.z * 8) * 19349663)); float O() { return (float)own.NextDouble(); }
            // A lump: a crag blob (the prop's stream picks which) with its flat base at bottom, scaled (sx, sy, sz), tipped lean
            // degrees about x (positive tips its top to +z), rolled about z, turned yaw.
            void Rock(float x, float bottom, float z, float sx, float sy, float sz, float lean, float roll, float yaw)
            {
                MeshPart(CragRock((int)(O() * 6)), t, new Vector3(x, bottom + .2f * sy, z), stone, Quaternion.Euler(lean, yaw, roll)).transform.localScale = new Vector3(sx, sy, sz);
            }
            var fronts = new List<Vector2>(); var backs = new List<Vector2>();   // each step's (x, front plane z), and a free crag's (x, back plane z), for the scree
            // Mountains: how far a base lump must sink (at most 3 m) so its flat base lies .4 m under the lowest ground beneath
            // its w x d footprint (the height function and the drawn ground both), not only under its foot line.
            float Sink(float x, float bottom, float z, float w, float d)
            {
                float lowest = float.MaxValue;
                for (int k = 0; k < 5; k++)
                {
                    var q = t.TransformPoint(new Vector3(x + (k == 4 ? 0 : (k % 2 * 2 - 1) * w * .35f), 0, z + (k == 4 ? 0 : (k / 2 * 2 - 1) * d * .35f)));
                    lowest = Mathf.Min(lowest, Mathf.Min(HeightAt(q.x, q.z), MeshY(q.x, q.z)) - t.position.y);
                }
                return Mathf.Clamp(bottom - (lowest - .4f), 0, 3);
            }
            // One step of the face: the lumps that fill an envelope w wide, d deep and h tall from its front plane zf and its foot.
            // f is which way is back (into a scarp's shelf, +z on a free crag); a scarp's lumps lean that way, a free crag's
            // either way. The zone's lean draws (ra, rb, rc) tip the upper mass, as they tipped the old slab.
            void Step(float x, float zf, float foot, float h, float w, float d, float f, bool scarp, float ra, float rb, float rc)
            {
                fronts.Add(new Vector2(x, zf)); if (!scarp) backs.Add(new Vector2(x, zf + f * d));
                // The base: wide and low (half the height), bulging .3 m in front of the step's front plane, sunk .3 m under the foot;
                // in the mountains it grows down further wherever the ground falls away under its footprint (Sink), its top where it was.
                float hA = Mathf.Max(2.5f, h * (.5f + O() * .15f)), szA = d * .85f + O() * .5f;
                float xA = x + (O() - .5f) * .8f, zA = zf + f * (szA / 2 - .3f), wA = w + 1.8f + O() * .8f, sink = seat ? Sink(xA, foot - .3f, zA, wA, szA) : 0;
                Rock(xA, foot - .3f - sink, zA, wA, hA + sink * 1.4f, szA, scarp ? f * (1 + O() * 3) : (O() - .5f) * 5, (O() - .5f) * 3, (O() - .5f) * 18);
                // The upper mass: set back (.8-1.6 m on a scarp, .3-.8 on a free crag), its top .95-1.15 of the way from the foot to
                // the crest, so the crest is ragged; tall enough (.65-.85 of the height) that its base sits well down in the base lump.
                // (2026-10-05, Chris: "boulders still unnatural looking": the leaning upper masses and the crest knobs read as boulders
                // piled up and balanced on one another.) Now the upper mass stands nearly upright, deep in the base (its base at a
                // third of the height), a little wider so the steps run together into one face, its crest varying by under a metre;
                // no knobs.
                float hB = Mathf.Max(2.5f, h * (.72f + O() * .12f)), szB = d * .8f + O() * .3f, topB = foot + h * (.96f + O() * .1f), zB = zf + f * ((scarp ? .8f + O() * .6f : .25f + O() * .3f) + szB / 2);
                Rock(x + (O() - .5f) * .8f, topB - hB, zB, w + 1.8f + O() * .6f, hB, szB, scarp ? f * (2 + ra * 4) : ra * 5 - 2.5f, rc * 4 - 2, scarp ? rb * 12 - 6 : rb * 16 - 8);
            }
            for (float x = -length / 2; x < length / 2; x += 2.2f)
            {
                float r0 = R01, r1 = R01, r2 = R01, r3 = R01, ra = R01, rb = R01, rc = R01;   // the seven draws every step always took
                if (rise > 0)
                {
                    // Scarp step: 3.2-3.8 m wide on the 2.2 m step (always overlapping), 3.5-5 m deep across the ground's 3 m rise,
                    // from .6 m under the lowest ground along its foot up to the shelf a metre behind it plus .3-1.1 m of parapet.
                    float w = 3.2f + r2 * .6f, d = 3.5f + r3 * 1.5f, z = side * (.4f + r1 * .8f), zf = z - side * d / 2;
                    float foot = Mathf.Min(Y(x, zf), Mathf.Min(Y(x - w / 2, zf), Y(x + w / 2, zf))) - .6f, top = Y(x, z + side * (d / 2 + 1)) + .3f + r0 * .8f, h = Mathf.Max(3, top - foot);
                    Step(x, zf, foot, h, w, d, side, true, ra, rb, rc);
                    low = Mathf.Min(low, foot); continue;
                }
                // A free crag's step: 2.6-3.6 m wide, 4-8 m tall, 3-5 m deep, its foot half a metre under the ground it stands on.
                float hh = 4 + r0 * 4, zz = r1 * 1.5f, y = 0; var size = new Vector3(2.6f + r2, hh, 3 + r3 * 2);
                if (seat) y = Mathf.Min(Y(x, zz - size.z / 2), Y(x, zz + size.z / 2));
                Step(x, zz - size.z / 2, y - .5f, hh, size.x, size.z, 1, false, ra, rb, rc);
                low = Mathf.Min(low, y - .5f);
            }
            if (rise > 0)
            {
                // The lip course: lower lumps a step behind and half a step along, sunk a metre into the shelf's edge and standing
                // 1.6-3 m proud of it, overlapping the front course's backs, half of them with a small knob. Solid too, so nothing
                // on the shelf walks into it.
                float zb = side * 4.2f, shelf = Y(0, zb);
                for (float x = -length / 2 + 1.1f; x < length / 2; x += 2.4f)
                {
                    float w = 3.4f + O() * 1.2f, d = 2.8f + O(), z = zb + side * (O() - .5f) * .6f, h = 2.6f + O() * 1.4f, g = Mathf.Min(Y(x, z), Y(x, z - side * d / 2));
                    Rock(x, g - 1, z, w, h, d, side * O() * 4, (O() - .5f) * 6, (O() - .5f) * 50);
                    if (O() < .5f) { float k = 1.2f + O() * 1.2f; Rock(x + (O() - .5f) * 1.6f, g - 1 + h * .55f, z + side * (O() - .5f), 1.6f + O(), k, 1.4f + O() * .8f, (O() - .5f) * 14, (O() - .5f) * 14, O() * 360); }
                }
                Solid(t, new Vector3(0, shelf + 1.5f, zb), new Vector3(length + 1, 6, 3.2f));
            }
            // The face's collider: over the lumps' full depth (a scarp's course sits on the shelf's side), and down to the lowest
            // seated step, so nothing walks into a lump's foot below the root.
            Solid(t, new Vector3(0, (low + 6 + rise) / 2, rise > 0 ? side * .8f : .8f), new Vector3(length + 2, 6 + rise - low, rise > 0 ? 5.8f : 4.5f));
            Scree(t, fronts, rise > 0 ? side : 1, rise > 0 ? 1 : seat ? .8f : .5f, stone, O);
            if (seat && rise <= 0) Scree(t, backs, -1, .5f, stone, O);   // mountains: a free crag stands in its fall on both sides
        }
        /// <summary>
        /// Fallen rock along a crag's foot, in the crag root's frame: the steps' front planes are <paramref name="fronts"/> (x, z)
        /// and the low side is -<paramref name="f"/>. A block broken off the face every 6-12 m (1.2-2 m, faceted like the face,
        /// its back in the face's foot) and scree out to 4 m (.35-1.3 m boulder lumps, most within a metre of the foot and the far
        /// ones small), every lump sunk a quarter to a half into the lower of the height function and the drawn ground, and
        /// none where loose rock may not lie (RockMayLie). No colliders, so the navmesh is as it was. Draws only from the crag's
        /// own stream <paramref name="O"/>; <paramref name="density"/> thins a free crag's fall to half a scarp's.
        /// </summary>
        void Scree(Transform t, List<Vector2> fronts, float f, float density, Material stone, Func<float> O)
        {
            if (fronts.Count == 0) return;
            float Front(float x) { var best = fronts[0]; foreach (var q in fronts) if (Mathf.Abs(q.x - x) < Mathf.Abs(best.x - x)) best = q; return best.y; }
            float G(float x, float z) { var w = t.TransformPoint(new Vector3(x, 0, z)); return Mathf.Min(HeightAt(w.x, w.z), MeshY(w.x, w.z)) - t.position.y; }
            bool May(float x, float z) { var w = t.TransformPoint(new Vector3(x, 0, z)); return RockMayLie(new Vector2(w.x, w.z)); }
            float x0 = fronts[0].x - 1, x1 = fronts[fronts.Count - 1].x + 1;
            for (float x = x0 + O() * 4; x < x1; x += 6 + O() * 6)
            {
                if (O() > density) continue;
                float s = 1.2f + O() * .8f, z = Front(x) - f * (.4f + s * .25f), sy = s * .8f; if (!May(x, z)) continue;
                MeshPart(CragRock((int)(O() * 6)), t, new Vector3(x, G(x, z) - s * .4f + .2f * sy, z), stone, Quaternion.Euler((O() - .5f) * 30, O() * 360, (O() - .5f) * 30)).transform.localScale = new Vector3(s * (1 + O() * .4f), sy, s * (.9f + O() * .4f));
            }
            for (float x = x0; x < x1; x += 1.1f)
            {
                if (O() > density * .75f) continue;
                float r = O(), o = .3f + r * r * 4.2f, s = .35f + O() * (o > 2 ? .5f : .95f), xx = x + (O() - .5f), z = Front(xx) - f * o, sy = s * .9f; if (!May(xx, z)) continue;
                Lump(BoulderAt((int)(O() * 6)), t, new Vector3(xx, G(xx, z) - s * (.25f + O() * .3f) + .18f * sy, z), new Vector3(s * (1.1f + O() * .6f), sy, s * (1 + O() * .5f)), stone, O() * 360);
            }
        }
        /// <summary>Whether loose rock may lie at a zone point: off the roads and out of the water, and clear of camps, clearings,
        /// exits, arrivals from other zones, the spawns and the other props (not crags, loose rocks, trees, graves or walls).
        /// Draws nothing random.</summary>
        bool RockMayLie(Vector2 p)
        {
            if (NearRoad(p, 2) || Water.NearWater(p, 1.5f)) return false;
            if (Zone.camps != null) foreach (var c in Zone.camps) if (c != null && Vector2.Distance(p, c.center) < c.radius + 1) return false;
            foreach (var c in Zone.clearings) if (c != null && Vector2.Distance(p, c.center) < c.radius + 1) return false;
            foreach (var e in Zone.exits) if (e != null && Vector2.Distance(p, e.at) < e.radius + 2) return false;
            foreach (var z in AllZones()) foreach (var e in z.exits) if (e != null && e.to == Zone.id && Vector2.Distance(p, e.arrive) < 5) return false;
            if (Vector2.Distance(p, Zone.spawns.player) < 4 || Vector2.Distance(p, Zone.spawns.companion) < 3 || Vector2.Distance(p, Zone.spawns.recovery) < 3) return false;
            foreach (var q in Zone.props)
            {
                if (q == null || q.kind == "cliff" || q.kind == "rock" || q.kind == "tree" || q.kind == "pine" || q.kind == "grave" || q.kind == "wall") continue;
                if (Vector2.Distance(p, q.at) < Mathf.Max(q.size.x, q.size.y) / 2 + 1.5f) return false;
            }
            return true;
        }
        /// <summary>Trunks already standing (hand-placed trees, orchards, groves, the forest edge): scattered trees keep clear of them.</summary>
        readonly List<Vector2> trunks = new List<Vector2>();
        /// <summary>The trunks standing in the zone (PlantField grows ferns in their shade).</summary>
        public IReadOnlyList<Vector2> Trunks { get { return trunks; } }
        /// <summary>Whether a point is at least <paramref name="r"/> from every standing trunk.</summary>
        public bool TrunkClear(Vector2 p, float r) { float r2 = r * r; foreach (var t in trunks) if ((t - p).sqrMagnitude < r2) return false; return true; }
        /// <summary>
        /// Where a scattered tree may grow clear of the trunks already standing: here, or pushed out to <paramref name="gap"/> from the
        /// one it grew into; null if that lands on a road, in water, by a building or still against a trunk. Draws nothing random.
        /// </summary>
        Vector2? Spot(Vector2 at, float gap)
        {
            for (int pass = 0; ; pass++)
            {
                int near = trunks.FindIndex(q => (q - at).sqrMagnitude < gap * gap);
                if (near < 0) return at;
                if (pass == 3) return null;
                var away = at - trunks[near]; at = trunks[near] + (away.sqrMagnitude > 1e-4f ? away.normalized : Vector2.right) * (gap + .05f);
                if (NearRoad(at, 2.5f) || NearProp(at, 5) || Water.NearWater(at, 1.5f)) return null;
            }
        }
        /// <summary>
        /// Records a scattered tree's trunk, or drops the tree when it found no clear spot. It is built either way, so the zone's
        /// random stream (every later tree, bush and rock) stays exactly where it was.
        /// </summary>
        void Keep(Transform tree, Vector2 at, bool clear)
        {
            clear &= Hollow.CoverAt(at, 3) <= 0;   // never growing in a cave
            if (clear) { trunks.Add(at); return; }
            LeafTrees.Remove(tree.position); DestroyImmediate(tree.gameObject);
        }
        void BuildGroves()
        {
            foreach (var prop in Zone.props) if (prop != null && (prop.kind == "tree" || prop.kind == "pine" || prop.kind == "dead_oak" || prop.kind == "great_oak" || prop.kind == "giant_tree" || prop.kind == "treehouse")) trunks.Add(prop.at);
            foreach (var g in Zone.groves.Where(g => g.kind == "orchard"))
                for (float x = g.center.x - g.size.x / 2 + 3; x < g.center.x + g.size.x / 2; x += 6.5f)
                    for (float z = g.center.y - g.size.y / 2 + 3; z < g.center.y + g.size.y / 2; z += 6.5f)
                    {
                        var at = new Vector2(x + (R01 - .5f), z + (R01 - .5f));
                        if (NearRoad(at, 2) || Water.NearWater(at, 1.5f)) continue;
                        var t = Root(new ZoneProp { kind = "tree", at = at, scale = .75f + R01 * .15f }, statics);
                        float h = 2.4f + R01 * .6f;
                        // An old fruit tree: a short leaning trunk on a root flare, forking low into three limbs, one to each leaf
                        // cluster (LeafCrown: painted cards, no blobs). Its shape and bark come from its own stream; the zone's draws
                        // below are the ones it always took.
                        var tr = TreeRandom(t.position); var bark = BarkShade((float)tr.NextDouble());
                        var axis = Bole(t, tr, h + .3f, .14f + (float)tr.NextDouble() * .04f, bark); var lean = axis(h) - new Vector3(0, h, 0);
                        float pick = R01;   // which fresh green (was the flat leaf's shade)
                        var spots = new List<(Vector3 at, float size, bool light)>(); var ends = new List<Vector3> { axis(h + .15f) };
                        for (int k = 0; k < 3; k++)
                        {
                            float s = 2 + R01 * .8f; _ = Canopy(); var c = new Vector3(R01 * 1.2f - .6f, h + .5f + R01 * .7f, R01 * 1.2f - .6f) + lean;
                            _ = R01;   // the clump's yaw
                            spots.Add((c, s, k == 1));
                            var from = axis(h * (.42f + (float)tr.NextDouble() * .12f)); var reach = new Vector3(c.x - from.x, 0, c.z - from.z);
                            if (reach.sqrMagnitude < .25f) reach = Quaternion.Euler(0, k * 120 + (float)tr.NextDouble() * 40, 0) * Vector3.right * .6f;   // a cluster right overhead: spread the fork
                            var end = new Vector3(from.x + reach.x, c.y - .35f, from.z + reach.z);
                            Limb(t, from, end, .085f, .04f, bark, .14f, 7); ends.Add(end);
                        }
                        var orchardLeaf = art.leafCards != null && art.leafCards.Length > 0 ? LeafMaterial(art.leafCards[0], Color.Lerp(new Color(.94f, 1, .9f), new Color(1, 1, .8f), Mathf.Round(pick * 2) / 2))
                            : Tint(art.foliage, Color.Lerp(new Color(.3f, .42f, .18f), new Color(.38f, .44f, .2f), pick));
                        // Its leaf mass: small fresh-green lumps (the fruit still shows round them) under the cards.
                        LeafCrown(t, tr, lean + new Vector3(0, h + .85f, 0), spots, ends, orchardLeaf, bark, Tint(art.foliage, new Color(.19f, .29f, .12f)), 1, new Vector3(1.5f, .9f, 1.5f), mass: LeafMassOf(0), cardSize: .9f, massSize: .54f);
                        var fruit = Tint(art.hay, new Color(.72f, .18f, .12f));
                        for (int k = 0; k < 6; k++) Part(PrimitiveType.Sphere, t, new Vector3(R01 * 2.4f - 1.2f, h + .2f + R01 * 1.4f, R01 * 2.4f - 1.2f) + lean, Vector3.one * .22f, fruit);
                        var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, h / 2, 0); cap.height = h; cap.radius = .3f; trunks.Add(at);
                        t.gameObject.AddComponent<NavBlocker>(); t.gameObject.AddComponent<TreeFade>();
                    }
            foreach (var g in Zone.groves.Where(g => g.kind != "orchard"))
                for (int i = 0; i < g.count; i++)
                {
                    var at = new Vector2(g.center.x + (R01 - .5f) * g.size.x, g.center.y + (R01 - .5f) * g.size.y);
                    if (NearRoad(at, 2.5f) || NearProp(at, 5) || Water.NearWater(at, 1.5f)) continue;
                    var spot = Spot(at, 1.8f); if (spot.HasValue) at = spot.Value;   // never growing through a trunk already standing
                    if (g.kind == "dead") { Keep(DeadTree(at, .5f + R01 * .55f, DeadBark(), 3 + (R01 > .6f ? 1 : 0), statics), at, spot.HasValue); continue; }
                    if (g.kind == "giant" && i % 3 == 0 && !NearProp(at, 9) && TrunkClear(at, 9.5f) && !NearRoad(at, 7))
                    {
                        // Every third tree a giant, if the ground round it is free: its look is its own (TreeRandom); the draws a broadleaf
                        // would have taken are taken all the same, so the grove's other trees stand where they would have.
                        var gp = new ZoneProp { kind = "giant_tree", at = at, rotation = R01 * 360, scale = .85f + R01 * .3f, variant = (int)(R01 * 3) };   // the broadleaf's three draws
                        var gt = Root(gp, statics); GiantTree(gt, gp.variant); trunks.Add(at); continue;
                    }
                    var p = new ZoneProp { kind = g.kind == "pine" ? "pine" : "tree", at = at, rotation = R01 * 360, scale = .8f + R01 * .5f, variant = (int)(R01 * 4) };
                    var t = Root(p, statics);
                    if (p.kind == "pine") Pine(t); else Broadleaf(t, p.variant);
                    if (p.kind == "pine" && !SeatPine(t, at)) DestroyImmediate(t.gameObject);   // built first: the zone's draws stay the same
                    else Keep(t, at, spot.HasValue);
                }
            // Undergrowth: low bushes in living woods (none in dead groves), so a wood reads as a wood at eye level.
            foreach (var g in Zone.groves.Where(g => g.kind == "broadleaf" || g.kind == "pine"))
                for (int i = 0; i < g.count; i++)
                {
                    var at = new Vector2(g.center.x + (R01 - .5f) * g.size.x, g.center.y + (R01 - .5f) * g.size.y);
                    if (NearRoad(at, 2) || NearProp(at, 3) || Water.NearWater(at, 1)) continue;
                    Bush(at, g.kind == "pine");
                }
        }
        /// <summary>
        /// A low leafy bush: two or three crossed pairs of painted leaf cards (four to six cards) fanning up and out round a small dark
        /// core, on the green leaf card a shade darker than a tree's (a pine grove's a little cooler), about the spread the old two or
        /// three blobs had; those blobs when the palette has no card art. No collider (you push through it). The zone's draws are the
        /// ones the blobs took, in their order; the cards' own shape comes from a stream keyed to where it stands.
        /// </summary>
        void Bush(Vector2 at, bool conifer)
        {
            var t = new GameObject("Bush").transform; t.SetParent(statics, false); t.position = Ground(at, -.1f); t.rotation = Quaternion.Euler(0, R01 * 360, 0);
            var c = conifer ? Color.Lerp(new Color(.17f, .25f, .15f), new Color(.24f, .3f, .17f), R01) : Leaf[(int)(R01 * Leaf.Length) % Leaf.Length] * (.85f + R01 * .2f);
            bool cardArt = art.leafCards != null && art.leafCards.Length > 0;
            var mat = cardArt ? LeafMaterial(art.leafCards[0], conifer ? new Color(.62f, .74f, .66f) : new Color(.72f, .8f, .64f)) : Tint(art.foliage, Wither(c));
            int n = 2 + (int)(R01 * 2);
            var cards = new ZoneMeshes.Cards(); var br = TreeRandom(t.position); float B() { return (float)br.NextDouble(); }
            var heart = new Vector3(0, .3f, 0); float size = 0;
            for (int i = 0; i < n; i++)
            {
                float s = .9f + R01 * .8f; var blob = Canopy(); var spot = new Vector3(R01 * 1.2f - .6f, s * .3f, R01 * 1.2f - .6f); float yaw = R01 * 360;
                if (!cardArt) { Lump(blob, t, spot, new Vector3(s * 1.3f, s * .8f, s * 1.3f), mat, yaw); continue; }
                size += s / n;
                // A crossed pair fanning up and out from the core toward where the blob sat, rolled at random about that line.
                var outward = new Vector3(spot.x, 0, spot.z); if (outward.sqrMagnitude < .04f) outward = Quaternion.Euler(0, B() * 360, 0) * Vector3.right * .3f;
                var along = (outward.normalized * (.55f + B() * .3f) + Vector3.up * (.45f + B() * .35f)).normalized;
                var across = Quaternion.AngleAxis(B() * 180, along) * Vector3.Cross(along, Vector3.up).normalized;
                float len = s * (1.05f + B() * .25f);
                cards.AddCross(heart + outward * .25f - along * len * .15f, along, across, len, s * (.95f + B() * .25f), heart, .3f, new Color(.8f, .86f, .78f) * (.9f + B() * .15f));
            }
            if (Hollow.CoverAt(at, 2) > 0) { DestroyImmediate(t.gameObject); return; }   // its draws are taken; none grows in a cave
            if (!cardArt) return;
            MeshPart(cards.Build("Bush leaves"), t, Vector3.zero, mat);
            // The core: a small dark clump at the heart, seen through the cards' gaps (canopies is filled: the loop drew from it).
            var coreMat = Tint(art.foliage, Wither(conifer ? new Color(.1f, .17f, .11f) : new Color(.12f, .2f, .09f)));
            Lump(canopies[(int)(B() * 6) % 6], t, heart, new Vector3(1.1f, .7f, 1.1f) * size, coreMat, B() * 360);
        }
        bool NearProp(Vector2 p, float margin)
        {
            foreach (var prop in Zone.props)
            {
                if (prop == null || prop.kind == "tree" || prop.kind == "pine" || prop.kind == "grave" || prop.kind == "wall") continue;
                float r = Mathf.Max(prop.size.x, prop.size.y) / 2 + margin;
                if (Vector2.Distance(p, prop.at) < Mathf.Max(r, margin)) return true;
            }
            return false;
        }
        /// <summary>Exit markers: a waystone with a lantern in its head where a road leaves the zone. Dressing only: no collider.</summary>
        void BuildExits()
        {
            foreach (var e in Zone.exits)
            {
                var t = new GameObject("Exit: " + e.name).transform; t.SetParent(statics, false); t.position = Ground(e.at);
                // An eight-sided standing stone, tapered, leaning a little with the years (its lean and turn from where it stands: no
                // zone draw), set deep enough for a slope; a carved band round its waist; a lantern house cut through its head (four
                // posts under a pointed cap) with the light set back inside it; two loose stones at its foot. One mesh of painted rock.
                var wr = TreeRandom(t.position); float W() { return (float)wr.NextDouble(); }
                var rock = RockTint(new Color(.5f, .48f, .44f));
                var stone = new GameObject("Waystone").transform; stone.SetParent(t, false); stone.localRotation = Quaternion.Euler(2 + W() * 3, 15 + (W() - .5f) * 30, (W() - .5f) * 5);
                MeshPart(PropMesh("Waystone stone", () => Joined(
                    Piece(Turned(new[] { new Vector2(.41f, -.5f), new Vector2(.38f, .25f), new Vector2(.31f, .95f), new Vector2(.275f, .98f), new Vector2(.27f, 1.08f), new Vector2(.3f, 1.11f), new Vector2(.27f, 1.56f), new Vector2(.34f, 1.6f), new Vector2(.34f, 1.68f), new Vector2(0, 1.68f) }, 8, 1.2f)),
                    Piece(Turned(new[] { new Vector2(0, 2.02f), new Vector2(.37f, 2.02f), new Vector2(.37f, 2.1f), new Vector2(.12f, 2.38f), new Vector2(0, 2.42f) }, 8, 1.2f)),
                    Piece(ZoneMeshes.Box(new Vector3(.1f, .36f, .1f)), new Vector3(.2f, 1.85f, .2f)), Piece(ZoneMeshes.Box(new Vector3(.1f, .36f, .1f)), new Vector3(-.2f, 1.85f, .2f)),
                    Piece(ZoneMeshes.Box(new Vector3(.1f, .36f, .1f)), new Vector3(.2f, 1.85f, -.2f)), Piece(ZoneMeshes.Box(new Vector3(.1f, .36f, .1f)), new Vector3(-.2f, 1.85f, -.2f)))), stone, Vector3.zero, rock);
                Part(PrimitiveType.Cube, stone, new Vector3(0, 1.85f, 0), new Vector3(.27f, .32f, .27f), art.glass);
                Lump(BoulderAt(1), t, new Vector3(.46f, LocalGround(t, .46f, .14f) + .07f, .14f), new Vector3(.56f, .36f, .46f), rock, W() * 360);
                Lump(BoulderAt(4), t, new Vector3(-.32f, LocalGround(t, -.32f, -.36f) + .05f, -.36f), new Vector3(.38f, .25f, .32f), rock, W() * 360);
                var l = new GameObject("Waystone light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(0, 1.9f, 0);
                l.type = LightType.Point; l.range = 6; l.intensity = 1.1f; l.color = new Color(1, .7f, .4f);
            }
        }

        /// <summary>The share of the open ground's grass kept at a point of a big zone's outer country (ZoneDefinition.wildFrom,
        /// wildDensity): all of it out to wildFrom from the middle (the larger of |x| and |z|), falling evenly to wildDensity at
        /// the edge and staying there past it. 1 everywhere in a zone that names no wildFrom. Draws nothing.</summary>
        float Wild(Vector2 p)
        {
            if (Zone.wildFrom <= 0 || Zone.wildFrom >= Half) return 1;
            float far = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y));
            return Mathf.Lerp(1, Mathf.Clamp01(Zone.wildDensity), Mathf.InverseLerp(Zone.wildFrom, Half, far));
        }
        Rect[] grassRoadBox; (Vector2 at, float r)[] grassBuildings;   // Openness's caches: it is asked ~330k times on a 380 m zone
        /// <summary>How much grass should grow at a point (0 = none): open meadow is full, the village core is trampled.</summary>
        float Openness(Vector2 p)
        {
            if (grassRoadBox == null)
            {
                // A road counts only within 3.3 m of its edge (the verge); a building's footprint and 1.5 m round it is bare.
                grassRoadBox = Zone.roads.Select(r => Box(r.points, r.width / 2 + 3.5f)).ToArray();
                grassBuildings = Zone.props.Where(o => o != null && (o.kind == "house" || o.kind == "inn" || o.kind == "barn" || o.kind == "mill" || o.kind == "ruined_house"))
                    .Select(o => (o.at, Mathf.Max(2, Mathf.Max(o.size.x, o.size.y) * .75f) + 1.5f))
                    .Concat(Zone.props.Where(o => o != null && (o.kind == "leathershop" || o.kind == "dryhut" || o.kind == "kitchen" || o.kind == "forge")).Select(o => (o.at, 4.5f))).ToArray();   // a workshop's floor (these props carry no size)
            }
            float living = 1 - Unmade(p.x, p.y) / .55f; if (living <= 0) return 0;   // grass thins out along the grey's ragged front
            if (Hollow.CoverAt(p, 1.2f) > 0) return 0;   // a cave's bare floor
            float verge = 0;   // 1 at a road's edge, 0 from 2.5 m out
            for (int k = 0; k < Zone.roads.Length; k++)
            {
                if (!grassRoadBox[k].Contains(p)) continue;
                var r = Zone.roads[k]; float d = DistanceToPath(p, r.points) - r.width / 2; if (d < .8f) return 0; verge = Mathf.Max(verge, 1 - Mathf.Clamp01((d - .8f) / 2.5f));
            }
            // Grass grows down to the bank's noisy edge (the same line PaintGround draws there), never in the water.
            if (Water.Shore(p, 3) + (Mathf.PerlinNoise(p.x * .45f, p.y * .45f) - .5f) * 1.1f < 1.3f) return 0;
            foreach (var c in Zone.clearings) if (Vector2.Distance(p, c.center) < c.radius + 1) return 0;
            if (Zone.shapes != null) foreach (var s in Zone.shapes) if (s != null && !string.IsNullOrEmpty(s.paint) && ShapeCover(s, p) > .35f) return 0;   // wallow mud, unmade brood ground
            foreach (var f in Zone.fields)
            {
                var local = Rotate(p - f.center, -f.rotation);
                if (Mathf.Abs(local.x) < f.size.x / 2 + .5f && Mathf.Abs(local.y) < f.size.y / 2 + .5f) return 0;
            }
            float open = 1;
            foreach (var g in Zone.groves)
                if (Mathf.Abs(p.x - g.center.x) < g.size.x / 2 && Mathf.Abs(p.y - g.center.y) < g.size.y / 2) open = g.kind == "dead" ? .15f : .45f;
            foreach (var (at, r) in grassBuildings) if ((p - at).sqrMagnitude < r * r) return 0;
            if (p.magnitude < Zone.flatRadius * .45f) open *= .35f;
            if (Zone.biome == "ash") return 0;                       // nothing grows in the ash
            if (Zone.biome == "mountain")
            {
                // Thin alpine turf in tufts: none on rock, scree or steep ground, under a face or round a crag's foot (the
                // paint's own measure, MountainBare; its turf ends about .5), and clumped where it does grow.
                float turf = 1 - Mathf.Clamp01(MountainBare(p.x, p.y, out _) * 1.5f);
                open *= turf * turf * Mathf.Clamp01((Mathf.PerlinNoise(p.x * .23f + 7, p.y * .23f + 3) - .38f) * 2.4f) * .8f;
            }
            // Gloom: dry, patchy grass on hard ground, but a dense dry verge along the roads so they read at a glance.
            if (Gloom) open *= Mathf.Lerp(.25f + .6f * Mathf.PerlinNoise(p.x * .07f + 13, p.y * .07f + 29), 1, verge * .85f);
            return open * living;
        }
        /// <summary>Soft grey smoke from a chimney (or any stack). Returns its particle system (a house keeps it on its door).</summary>
        ParticleSystem Smoke(Transform parent, Vector3 localPos)
        {
            var ps = new GameObject("Chimney smoke").AddComponent<ParticleSystem>(); ps.transform.SetParent(parent, false);
            ps.transform.localPosition = localPos; ps.transform.rotation = Quaternion.Euler(-90, 0, 0);
            var main = ps.main; main.startLifetime = 6; main.startSpeed = .7f; main.startSize = new ParticleSystem.MinMaxCurve(.6f, 1.2f);
            main.startColor = new Color(.62f, .6f, .58f, .35f); main.maxParticles = 60; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 5;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 8; shape.radius = .15f;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .5f, 1, 2.6f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient(); grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .15f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(.25f, .5f); vel.y = new ParticleSystem.MinMaxCurve(0, 0); vel.z = new ParticleSystem.MinMaxCurve(.05f, .15f);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = art.particle;
            HourTinted.Add(ps);   // grey by day, a dim wisp against the night sky
            return ps;
        }
        /// <summary>A watermill: a house on the bank with a turning wheel in the creek on its west side.</summary>
        void Mill(Transform t, Vector2 size)
        {
            House(t, size, 3.4f, 1, false);
            var wheel = new GameObject("Water wheel").transform; wheel.SetParent(t, false); wheel.localPosition = new Vector3(-size.x / 2 - 1.1f, 1.3f, 0);
            // Hang it in the creek, whatever ground the house stands on: its inner face where the water is 0.4 m deep out from
            // the west wall, the hub set from the surface so the lower paddles dip 0.35 m in, and a shaft back to the wall.
            float wall = size.x / 2;
            for (float x = wall + .6f; x < wall + 9; x += .25f)
            {
                var at = t.TransformPoint(new Vector3(-x, 0, 0));
                if (!WaterAt(new Vector2(at.x, at.z), out float surface, out float depth) || depth < .4f) continue;
                float hub = x + .5f; at = t.TransformPoint(new Vector3(-hub, 0, 0));
                wheel.position = new Vector3(at.x, surface + 1.85f, at.z);
                if (hub - wall > 1) Part(PrimitiveType.Cylinder, t, new Vector3(-(wall + hub) / 2, wheel.localPosition.y, 0), new Vector3(.3f, (hub - wall) / 2, .3f), art.timber, Quaternion.Euler(0, 0, 90));
                break;
            }
            var spokes = art.timber;
            Part(PrimitiveType.Cylinder, wheel, Vector3.zero, new Vector3(4, .5f, 4), Tint(art.timber, new Color(.3f, .22f, .15f)), Quaternion.Euler(0, 0, 90));
            for (int i = 0; i < 8; i++) Part(PrimitiveType.Cube, wheel, Vector3.zero, new Vector3(.9f, 4.4f, .3f), spokes, Quaternion.Euler(i * 22.5f, 0, 0));
            Part(PrimitiveType.Cylinder, wheel, Vector3.zero, new Vector3(.4f, 1, .4f), art.metal, Quaternion.Euler(0, 0, 90));
            wheel.gameObject.AddComponent<Spinner>().axis = Vector3.right;
        }

        // ---------- trades: workplaces for villagers (front faces -Z) ----------
        /// <summary>Workplaces by trade (forge, stall, oven, tannery, woodpile); villagers of that trade work there. A smithy, a bake
        /// oven, the herbalist's drying hut and an inn's kitchen are the player's stations too (StationKind): one each, at the thing
        /// its first workplace looks at (the anvil, the oven's mouth, the bench, the range).</summary>
        public readonly List<ZoneWorkplace> Workplaces = new List<ZoneWorkplace>();
        void Workplace(Transform t, string kind, Vector3 stand, Vector3 look)
        {
            var s = t.TransformPoint(stand); var l = t.TransformPoint(look);
            Workplaces.Add(new ZoneWorkplace { kind = kind, name = t.name, stand = Ground(new Vector2(s.x, s.z)), look = l });
            AddStation(StationKind(kind), t.name, Ground(new Vector2(l.x, l.z)), t);
        }
        Light Glow(Transform t, Vector3 at, float range, float intensity, Color color, float night)
        {
            var l = new GameObject("Glow").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = at;
            l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = color;
            NightLights.Add(new NightLight { light = l, dayIntensity = intensity, nightIntensity = night });
            return l;
        }
        static readonly Color[][] Awnings = { new[] { new Color(.66f, .08f, .07f), new Color(.96f, .9f, .74f) }, new[] { new Color(.07f, .2f, .6f), new Color(.96f, .9f, .74f) }, new[] { new Color(.08f, .36f, .14f), new Color(.9f, .6f, .12f) } };
        static readonly Color[] Pennants = { new Color(.66f, .08f, .07f), new Color(.9f, .6f, .12f), new Color(.07f, .2f, .6f), new Color(.08f, .36f, .14f) };
        /// <summary>
        /// Market stall: an awning on four stout posts and two rails, a scalloped valance, a plank counter of goods (variant:
        /// 0 baskets of produce, 1 bolts of cloth and pots, 2 bread, all under dyed stripes; 3 Ash-Walker, GAME-ONLY look: two
        /// stitched hides on bone poles, a ragged hide valance, salt and bone), a crate and a tied sack beside it.
        /// </summary>
        void Stall(Transform t, int variant)
        {
            bool hide = variant == 3;
            var colors = hide ? new[] { new Color(.56f, .43f, .29f), new Color(.64f, .52f, .36f) } : Awnings[Mathf.Abs(variant) % Awnings.Length];
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                if (hide) Part(PrimitiveType.Cylinder, t, new Vector3(sx * 1.4f, sz > 0 ? 1.25f : 1.1f, sz * .9f), new Vector3(.13f, sz > 0 ? 1.25f : 1.1f, .13f), Bone);
                else Part(PrimitiveType.Cube, t, new Vector3(sx * 1.4f, sz > 0 ? 1.25f : 1.1f, sz * .9f), new Vector3(.16f, sz > 0 ? 2.5f : 2.2f, .16f), art.timber);
            if (!hide) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(0, sz > 0 ? 2.42f : 2.12f, sz * .9f), new Vector3(3.1f, .1f, .1f), art.timber);   // rails under the awning, their ends past the posts
            if (!hide && !Gloom) foreach (int sx in new[] { -1, 1 })   // a pennant on a pole at each back post (playtest note 13)
            {
                Part(PrimitiveType.Cylinder, t, new Vector3(sx * 1.4f, 2.72f, .9f), new Vector3(.03f, .22f, .03f), art.timber);
                Part(PrimitiveType.Cube, t, new Vector3(sx * 1.4f + .17f, 2.84f, .9f), new Vector3(.32f, .16f, .02f), Tint(art.cloth, Pennants[(Mathf.Abs(variant) + (sx > 0 ? 1 : 0)) % Pennants.Length]));
            }
            for (int i = 0; i < 6; i++)   // stripes, or one hide per half
                Part(PrimitiveType.Cube, t, new Vector3(-1.25f + i * .5f, 2.35f, 0), new Vector3(.5f, .05f, 2.3f), Tint(art.cloth, colors[hide ? i / 3 : i % 2]), Quaternion.Euler(-9, 0, hide ? (i / 3) * 3 - 1.5f : 0));
            var scallop = PropMesh("Awning scallop", () =>
            {
                var o = new List<Vector2> { new Vector2(-.25f, 0), new Vector2(.25f, 0) };
                for (int k = 0; k <= 6; k++) o.Add(new Vector2(Mathf.Cos(k * 30 * Mathf.Deg2Rad) * .25f, -.1f - Mathf.Sin(k * 30 * Mathf.Deg2Rad) * .25f));
                return Cutout(o.ToArray(), .03f);
            });
            for (int i = 0; i < 6; i++)   // valance: a scallop a stripe, or ragged hide strips of uneven length
                if (hide) Part(PrimitiveType.Cube, t, new Vector3(-1.25f + i * .5f, 2.14f - (i * 7 % 4) * .04f, -1.16f), new Vector3(.5f, .12f + (i * 7 % 4) * .08f, .03f), Tint(art.cloth, colors[0]));
                else MeshPart(scallop, t, new Vector3(-1.25f + i * .5f, 2.2f, -1.16f), Tint(art.cloth, colors[i % 2]));
            BoxPart(t, new Vector3(0, .5f, -.45f), new Vector3(2.8f, 1, .7f), Tint(art.timber, new Color(.45f, .32f, .2f)), null, 1);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.03f, -.45f), new Vector3(3, .08f, .86f), Tint(art.timber, new Color(.35f, .25f, .16f)));
            var r = new System.Random(Zone.seed + (int)(t.position.x * 13 + t.position.z * 7));
            float R() { return (float)r.NextDouble(); }
            for (int i = 0; i < 7; i++)
            {
                var at = new Vector3(-1.2f + i * .4f, 1.1f, -.45f + (R() - .5f) * .3f);
                switch (hide ? 3 : Mathf.Abs(variant) % 3)
                {
                    case 0:   // baskets of apples, turnips and cabbages, heaped over the rim
                        MeshPart(PropMesh("Basket", () => Turned(new[] { new Vector2(0, 0), new Vector2(.13f, 0), new Vector2(.19f, .14f), new Vector2(.165f, .14f), new Vector2(.12f, .03f), new Vector2(0, .03f) }, 10, .25f)), t, at + new Vector3(0, -.03f, 0), art.hay);
                        var produce = new[] { new Color(.86f, .1f, .08f), new Color(.92f, .8f, .56f), new Color(.3f, .68f, .18f), new Color(.96f, .52f, .08f) }[i % 4];
                        for (int k = 0; k < 3; k++) Part(PrimitiveType.Sphere, t, at + new Vector3((k - 1) * .09f, .12f + (k % 2) * .03f, (k % 2) * .08f - .04f), Vector3.one * .15f, Tint(art.foliage, produce));
                        break;
                    case 1:   // bolts of cloth laid across the counter, and clay pots
                        if (i % 2 == 0) Part(PrimitiveType.Cylinder, t, at + new Vector3(0, .07f, 0), new Vector3(.2f, .26f, .2f), Tint(art.cloth, Color.HSVToRGB(R(), .8f, .6f)), Quaternion.Euler(90, 0, 0));
                        else MeshPart(PropMesh("Pot", () => Turned(new[] { new Vector2(0, 0), new Vector2(.07f, 0), new Vector2(.13f, .1f), new Vector2(.13f, .16f), new Vector2(.07f, .24f), new Vector2(.09f, .28f), new Vector2(.06f, .28f), new Vector2(.06f, .25f), new Vector2(0, .25f) }, 10)), t, at + new Vector3(0, -.03f, 0), Tint(art.stone, new Color(.82f, .38f, .18f)));
                        break;
                    case 3:   // salt blocks, bone harpoon heads, strips of dried meat
                        if (i % 3 == 0) Part(PrimitiveType.Cube, t, at + new Vector3(0, .1f, 0), new Vector3(.24f, .2f, .2f), Tint(art.stone, new Color(.94f, .94f, .91f)), Quaternion.Euler(0, R() * 40 - 20, 0));
                        else if (i % 3 == 1) Part(PrimitiveType.Cube, t, at + new Vector3(0, .03f, 0), new Vector3(.07f, .05f, .34f), Bone, Quaternion.Euler(0, R() * 50 - 25, 0));
                        else Part(PrimitiveType.Capsule, t, at + new Vector3(0, .05f, 0), new Vector3(.08f, .14f, .08f), Tint(art.cloth, new Color(.36f, .16f, .12f)), Quaternion.Euler(90, R() * 60, 0));
                        break;
                    default:  // loaves and rolls
                        Part(PrimitiveType.Capsule, t, at + new Vector3(0, .07f, 0), new Vector3(.2f, .17f, .2f), Tint(art.hay, new Color(.72f, .48f, .22f)), Quaternion.Euler(90, R() * 60, 0));
                        break;
                }
            }
            Crate(t, new Vector3(1.9f, .3f, .2f), .6f, 12);
            Part(PrimitiveType.Sphere, t, new Vector3(-1.9f, .33f, .3f), new Vector3(.55f, .66f, .5f), art.cloth);                                // sack
            Part(PrimitiveType.Cylinder, t, new Vector3(-1.9f, .68f, .3f), new Vector3(.16f, .06f, .16f), art.cloth);                             // its tied neck
            Solid(t, new Vector3(0, .5f, -.45f), new Vector3(2.9f, 1, .8f));
            Workplace(t, "stall", new Vector3(0, 0, .4f), new Vector3(0, 1.1f, -1.5f));
        }
        /// <summary>Bakehouse oven: a clay dome on a stone plinth with a glowing mouth, a bread table, flour sacks and a peel.</summary>
        void Oven(Transform t)
        {
            var clay = Tint(art.stone, new Color(.66f, .42f, .3f));
            BoxPart(t, new Vector3(0, .45f, .3f), new Vector3(2.2f, .9f, 2.2f), Masonry, null, 1);
            Part(PrimitiveType.Sphere, t, new Vector3(0, 1.1f, .3f), new Vector3(2f, 1.5f, 2f), clay);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.15f, -.72f), new Vector3(.7f, .55f, .1f), Tint(art.timber, new Color(.08f, .05f, .04f)));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.0f, -.69f), new Vector3(.5f, .18f, .08f), art.glass);   // embers in the mouth
            Part(PrimitiveType.Cylinder, t, new Vector3(.4f, 2.0f, .6f), new Vector3(.3f, .35f, .3f), clay);
            if (art.particle != null) Smoke(t, new Vector3(.4f, 2.45f, .6f));
            Glow(t, new Vector3(0, 1.2f, -1.1f), 5, 1.1f, new Color(1, .55f, .25f), 1.8f);
            // Bread table, a sack of flour and the long peel.
            Part(PrimitiveType.Cube, t, new Vector3(2.1f, .75f, -.3f), new Vector3(1.4f, .08f, .8f), art.timber);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(2.1f + sx * .6f, .37f, -.3f + sz * .32f), new Vector3(.07f, .74f, .07f), art.timber);
            for (int i = 0; i < 5; i++) Part(PrimitiveType.Capsule, t, new Vector3(1.6f + i * .25f, .86f, -.3f + (i % 2) * .15f), new Vector3(.13f, .15f, .13f), Tint(art.hay, new Color(.72f, .48f, .22f)), Quaternion.Euler(90, i * 10, 0));
            // Flour sacks of sacking tied at the neck, standing on the ground clear of the plinth (white balls read as eggs sunk in the stone).
            var sacking = Tint(art.cloth, new Color(.84f, .78f, .64f));
            foreach (var sack in new[] { new Vector3(-1.55f, .55f, -.55f), new Vector3(-1.62f, .5f, .1f) })
            {
                Part(PrimitiveType.Sphere, t, new Vector3(sack.x, sack.y * .45f, sack.z), new Vector3(.52f, sack.y, .46f), sacking);
                Part(PrimitiveType.Cylinder, t, new Vector3(sack.x, sack.y * .95f + .03f, sack.z), new Vector3(.15f, .07f, .15f), sacking);
            }
            // The peel leans on the back edge of the plinth with its blade up clear of the dome (it used to stick into the stone).
            Part(PrimitiveType.Cube, t, new Vector3(.5f, 1, 1.35f), new Vector3(.05f, 2.2f, .05f), art.timber, Quaternion.Euler(-24, 0, 0));
            Part(PrimitiveType.Cube, t, new Vector3(.5f, 1.87f, .97f), new Vector3(.35f, .45f, .03f), art.timber, Quaternion.Euler(-24, 0, 0));
            // The fire's woodstack: split logs piled beside the plinth (a tall stone block here read as a chimney cutting the dome).
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 3 - row; i++)
                    Part(PrimitiveType.Cylinder, t, new Vector3(-2.05f + row * .15f + i * .3f, .15f + row * .26f, 1.2f), new Vector3(.3f, .35f, .3f), art.bark, Quaternion.Euler(90, 0, 0));
            Solid(t, new Vector3(0, .9f, .3f), new Vector3(2.3f, 1.8f, 2.3f));
            Solid(t, new Vector3(2.1f, .4f, -.3f), new Vector3(1.4f, .8f, .8f));
            Workplace(t, "oven", new Vector3(0, 0, -1.5f), new Vector3(0, 1.1f, -.7f));
            Workplace(t, "oven", new Vector3(2.1f, 0, -1.15f), new Vector3(2.1f, .8f, -.3f));
        }
        /// <summary>Tannery yard: two frames with stretched hides, a soaking vat, a pile of pelts and a scraping beam.</summary>
        void Tannery(Transform t)
        {
            var dark = Tint(art.timber, new Color(.3f, .21f, .13f));
            var hides = new[] { new Color(.62f, .48f, .32f), new Color(.5f, .36f, .22f), new Color(.7f, .6f, .46f) };
            for (int f = 0; f < 2; f++)
            {
                float x = -1.6f + f * 2.4f;
                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(x + s * .8f, 1, .8f), new Vector3(.1f, 2, .1f), dark);
                foreach (float y in new[] { .35f, 1.85f }) Part(PrimitiveType.Cube, t, new Vector3(x, y, .8f), new Vector3(1.7f, .08f, .08f), dark);
                Part(PrimitiveType.Cube, t, new Vector3(x, 1.1f, .8f), new Vector3(1.3f, 1.3f, .03f), Tint(art.cloth, hides[f % 3]), Quaternion.Euler(0, 0, f * 6 - 3));
                for (int k = 0; k < 4; k++) Part(PrimitiveType.Cube, t, new Vector3(x - .6f + k * .4f, 1.78f, .8f), new Vector3(.02f, .12f, .02f), art.cloth);  // lacing
            }
            Part(PrimitiveType.Cylinder, t, new Vector3(2.4f, .45f, -.2f), new Vector3(1.3f, .45f, 1.3f), dark);                                 // vat
            Part(PrimitiveType.Cylinder, t, new Vector3(2.4f, .91f, -.2f), new Vector3(1.15f, .01f, 1.15f), Tint(art.water, new Color(.35f, .24f, .12f)));
            for (int i = 0; i < 4; i++) Part(PrimitiveType.Cube, t, new Vector3(-2.4f + (i % 2) * .15f, .04f + i * .08f, -.6f), new Vector3(1, .08f, .7f), Tint(art.cloth, hides[i % 3]), Quaternion.Euler(0, i * 17, 0)); // pelts, each on the one below
            // Scraping beam: the far end rests on the ground and the tanner's end on a pair of splayed legs (not one end in the air).
            Part(PrimitiveType.Cube, t, new Vector3(-.4f, .42f, -.9f), new Vector3(.25f, .25f, 1.6f), dark, Quaternion.Euler(22, 0, 0));
            Part(PrimitiveType.Cube, t, new Vector3(-.4f, .56f, -.85f), new Vector3(.3f, .04f, 1.2f), Tint(art.cloth, hides[2]), Quaternion.Euler(22, 0, 0));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(-.4f + s * .12f, .3f, -1.5f), new Vector3(.07f, .62f, .07f), dark, Quaternion.Euler(0, 0, s * 12));
            Solid(t, new Vector3(-.4f, 1, .8f), new Vector3(4.2f, 2, .4f));
            Solid(t, new Vector3(2.4f, .45f, -.2f), new Vector3(1.4f, .9f, 1.4f));
            Workplace(t, "tannery", new Vector3(-1.6f, 0, 0), new Vector3(-1.6f, 1.1f, .8f));
            Workplace(t, "tannery", new Vector3(-.4f, 0, -1.9f), new Vector3(-.4f, .6f, -.9f));
            Workplace(t, "tannery", new Vector3(2.4f, 0, -1.2f), new Vector3(2.4f, .9f, -.2f));
        }
        /// <summary>Props usable with E (quest wagons, crates, herb patches). Registered from ZoneProp.interact.</summary>
        public readonly List<ZoneInteractable> Interactables = new List<ZoneInteractable>();
        /// <summary>The zone's secrets as built (on no map; the encounter's discovery system finds and rewards them).</summary>
        public readonly List<ZoneSecretSpot> Secrets = new List<ZoneSecretSpot>();
        /// <summary>
        /// The Investigation Bureau's black-iron wagon (CANON, book1 ch.4): a barred cage on a heavy bed, its door hanging
        /// open, child-sized manacles on a chain inside. Front faces -Z.
        /// </summary>
        void Wagon(Transform t)
        {
            var iron = Tint(art.metal, new Color(.16f, .16f, .18f)); var dark = Tint(art.timber, new Color(.16f, .12f, .09f));
            Part(PrimitiveType.Cube, t, new Vector3(0, .9f, 0), new Vector3(2.2f, .2f, 3.6f), dark);                                  // bed
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                Part(PrimitiveType.Cylinder, t, new Vector3(sx * 1.2f, .6f, sz * 1.2f), new Vector3(1.15f, .07f, 1.15f), iron, Quaternion.Euler(0, 0, 90));
            // Cage: corner posts, roof, and bars on three sides; the back door hangs open.
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, new Vector3(sx * 1.02f, 1.95f, sz * 1.72f), new Vector3(.12f, 1.9f, .12f), iron);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.95f, 0), new Vector3(2.3f, .12f, 3.7f), iron);
            foreach (int sx in new[] { -1, 1 }) for (float z = -1.4f; z <= 1.45f; z += .35f) Part(PrimitiveType.Cube, t, new Vector3(sx * 1.02f, 1.95f, z), new Vector3(.05f, 1.9f, .05f), iron);
            for (float x = -.8f; x <= .85f; x += .35f) Part(PrimitiveType.Cube, t, new Vector3(x, 1.95f, 1.72f), new Vector3(.05f, 1.9f, .05f), iron);
            var door = new GameObject("Cage door").transform; door.SetParent(t, false); door.localPosition = new Vector3(-1.02f, 1.95f, -1.72f); door.localRotation = Quaternion.Euler(0, 118, 0);
            for (float x = .15f; x <= 1.95f; x += .35f) Part(PrimitiveType.Cube, door, new Vector3(x, 0, 0), new Vector3(.05f, 1.8f, .05f), iron);
            foreach (float y in new[] { -.85f, .85f }) Part(PrimitiveType.Cube, door, new Vector3(1, y, 0), new Vector3(2, .07f, .07f), iron);
            // Inside: a chain along the floor with small manacles, and a strongbox where the ledger was kept.
            for (int i = 0; i < 6; i++) Part(PrimitiveType.Cylinder, t, new Vector3(-.4f + (i % 2) * .1f, 1.02f, -1.2f + i * .45f), new Vector3(.2f, .015f, .2f), Tint(art.metal, new Color(.4f, .4f, .42f)), Quaternion.Euler(90, i * 30, 0));
            Part(PrimitiveType.Cube, t, new Vector3(.5f, 1.2f, 1.2f), new Vector3(.6f, .4f, .45f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(.5f, 1.42f, 1.2f), new Vector3(.64f, .05f, .5f), iron);
            // Shafts, and the Concord's seal on the side.
            foreach (int sx in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * .5f, .75f, 2.9f), new Vector3(.1f, .1f, 2.2f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(1.09f, 2.2f, 0), new Vector3(.02f, .5f, .5f), Tint(art.metal, new Color(.75f, .62f, .25f)));
            Solid(t, new Vector3(0, 1.5f, 0), new Vector3(2.4f, 3, 3.8f));
        }
        /// <summary>
        /// A herb patch, knee-high so it reads from the path above the meadow grass and its wildflowers: 0 yarrow (feathery leaves
        /// under flat white heads of tiny florets), 1 mourner's cap (a cluster of grey, black-gilled caps on a dark damp patch, as
        /// Wenna tells it), 2 comfrey (broad leaves under nodding violet bells); 3 tarnwort, 4 cinder-thistle and 5 dewfern are the
        /// trades' herbs, see HerbOfTheTrades. The clump (child "Clump") is then laid on the ground it grows from (LayOnGround), so on
        /// a slope nothing of it hangs over the downhill side; <paramref name="groundY"/> is that ground's height at a world point
        /// (x, z), the land's when not given (a node in a cave gives its floor's). Fixed tables: draws nothing random.
        /// </summary>
        void Herb(Transform t, int variant, Func<float, float, float> groundY = null)
        {
            int v = variant >= 3 && variant <= 5 ? variant : Mathf.Abs(variant) % 3;
            // Drawn a third larger than life, as the held weapons are: from the path a life-size clump is lost among the wildflowers.
            var clump = new GameObject("Clump").transform; clump.SetParent(t, false); clump.localScale = Vector3.one * (v == 1 ? 1.15f : 1.3f);
            if (v >= 3) HerbOfTheTrades(clump, v); else if (v == 1) MournersCap(clump); else MeadowHerb(clump, v);
            LayOnGround(clump, groundY ?? LandY);
        }
        /// <summary>The most a herb's clump leans with the slope it grows on, in degrees: past the steepest ground a herb grows on (a
        /// Peaks tarnwort's 0.64 slope is about 33 degrees, a creek bank less), so its leaves lie on the slope and are not buried by the
        /// sink. On steeper ground the rest is taken up by sinking it.</summary>
        const float HerbTilt = 40;
        /// <summary>The least a mourner's cap's gills stand over the ground once its cluster is laid on a slope.</summary>
        const float GillClear = .05f;
        /// <summary>The drawn land's height at a world point: the lower of the land and its mesh (as under a node).</summary>
        float LandY(float x, float z) { return Mathf.Min(HeightAt(x, z), MeshY(x, z)); }
        /// <summary>
        /// Lays a herb's clump on the ground it grows from (a bank, a hillside). It leans with the slope under its reach (a plane
        /// fitted to the ground under the middle of every part and on a grid of a fifth of its reach out to its farthest part), by at
        /// most <see cref="HerbTilt"/> degrees, and is then sunk until the plane it stands on is nowhere above the ground at those
        /// points (and 2 cm more), so no leaf or frond hangs over the downhill side. Leaning that far, the sink on any slope a herb grows
        /// on is a few centimetres, so the leaves round its foot lie on the ground and are not buried. The clump turns and sinks whole
        /// (its heads, bells and fronds are separate parts and would come apart). On level ground it stays as built (a lean under a
        /// degree is none, and so is a sink under 5 mm).
        /// <paramref name="groundY"/>: the ground's height at a world point (x, z). Draws nothing random.
        /// </summary>
        void LayOnGround(Transform clump, Func<float, float, float> groundY)
        {
            var o = clump.position;
            List<Vector2> Under()
            {
                var pts = new List<Vector2>(); float reach = 0;
                foreach (var r in clump.GetComponentsInChildren<Renderer>())
                {
                    var b = r.bounds; pts.Add(new Vector2(b.center.x, b.center.z));
                    foreach (float x in new[] { b.min.x, b.max.x }) foreach (float z in new[] { b.min.z, b.max.z }) reach = Mathf.Max(reach, Vector2.Distance(new Vector2(x, z), new Vector2(o.x, o.z)));
                }
                for (int i = -5; i <= 5; i++) for (int k = -5; k <= 5; k++) if (i * i + k * k <= 25) pts.Add(new Vector2(o.x + i * reach / 5, o.z + k * reach / 5));
                return pts;
            }
            // The slope: the plane best fitting the ground under it (least squares).
            var under = Under(); int n = under.Count; var ys = new float[n]; float mx = 0, mz = 0, my = 0;
            for (int i = 0; i < n; i++) { ys[i] = groundY(under[i].x, under[i].y); mx += under[i].x; mz += under[i].y; my += ys[i]; }
            mx /= n; mz /= n; my /= n;
            float sxx = 0, sxz = 0, szz = 0, sxy = 0, szy = 0;
            for (int i = 0; i < n; i++) { float dx = under[i].x - mx, dz = under[i].y - mz, dy = ys[i] - my; sxx += dx * dx; sxz += dx * dz; szz += dz * dz; sxy += dx * dy; szy += dz * dy; }
            float det = sxx * szz - sxz * sxz, ax = 0, az = 0;   // the ground's rise along x and along z
            if (Mathf.Abs(det) > 1e-6f) { ax = (sxy * szz - szy * sxz) / det; az = (szy * sxx - sxy * sxz) / det; }
            var normal = new Vector3(-ax, 1, -az).normalized;
            if (Vector3.Angle(Vector3.up, normal) >= 1) clump.rotation = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, normal), HerbTilt) * clump.parent.rotation;
            // A cluster on stalks (mourner's cap: its parts named "Stalk" and "Gills") stands on its stalks' feet, built under its
            // plane. It is sunk only until no foot is clear of the ground (and 2 cm more), and never so far that a cap's gills come
            // within GillClear of it (raised, where the ground rises round it); a stalk whose foot is then still clear is lengthened
            // down its own line into the ground. Sinking its plane under a bank's crest buried the small caps.
            float RimOver(Transform cyl, int at)   // a cylinder's lower rim (0-7) and its middle (8), over the ground
            {
                var w = cyl.TransformPoint(at == 8 ? new Vector3(0, -1, 0) : new Vector3(.5f * Mathf.Cos(at * Mathf.PI / 4), -1, .5f * Mathf.Sin(at * Mathf.PI / 4)));
                return w.y - groundY(w.x, w.z);
            }
            float need = float.MinValue, room = float.MaxValue; var stalks = new List<Transform>(); bool stalked = false;
            foreach (var part in clump.GetComponentsInChildren<Transform>())
            {
                bool foot = part.name == "Stalk"; if (!foot && part.name != "Gills") continue;
                stalked = true; if (foot) stalks.Add(part);
                for (int q = 0; q <= 8; q++) { float over = RimOver(part, q); if (foot) need = Mathf.Max(need, over); else room = Mathf.Min(room, over); }
            }
            if (stalked)
            {
                float drop = Mathf.Min(Mathf.Max(0, need + .02f), room - GillClear);
                if (Mathf.Abs(drop) > .005f) clump.position = o - Vector3.up * drop;
                foreach (var stalk in stalks)
                    for (int pass = 0; pass < 4; pass++)
                    {
                        float high = float.MinValue; for (int q = 0; q <= 8; q++) high = Mathf.Max(high, RimOver(stalk, q));
                        if (high <= -.01f) break;
                        float more = (high + .03f) / Mathf.Max(.4f, stalk.up.y);   // longer by this, at its foot (its top, cap and gills stay)
                        stalk.position -= stalk.up * (more / 2);
                        var size = stalk.localScale; size.y += more / 2 / clump.lossyScale.y; stalk.localScale = size;
                    }
                return;
            }
            // Sunk until the plane it stands on is under the ground beneath every part (measured as leaned).
            var up = clump.up; float sink = float.MinValue;
            foreach (var c in Under()) sink = Mathf.Max(sink, o.y - (up.x * (c.x - o.x) + up.z * (c.y - o.z)) / up.y - groundY(c.x, c.y));
            if (sink > .005f) clump.position = o - Vector3.up * (sink + .02f);
        }
        /// <summary>Yarrow (0: feathery leaves under flat white heads of tiny florets) and comfrey (2: broad leaves under nodding
        /// violet bells), as Herb variants. Fixed tables: draws nothing random.</summary>
        void MeadowHerb(Transform t, int v)
        {
            var leaf = Tint(art.foliage, Wither(v == 0 ? new Color(.27f, .48f, .2f) : new Color(.25f, .44f, .19f)));
            var shade = Tint(art.foliage, Wither(v == 0 ? new Color(.18f, .35f, .15f) : new Color(.17f, .31f, .14f)));
            var stem = Tint(art.foliage, Wither(new Color(.32f, .5f, .23f)));
            var bloom = Tint(art.foliage, v == 0 ? new Color(.97f, .95f, .87f) : new Color(.6f, .42f, .82f));
            var heart = Tint(art.foliage, v == 0 ? new Color(.97f, .95f, .87f) : new Color(.44f, .28f, .64f));   // yarrow's head one white (a beige disc made it a mushroom cap)
            // The leaves round the foot: long and feathery for yarrow (two narrow blades a leaf), broad and rough for comfrey.
            for (int i = 0; i < 9; i++)
            {
                var q = Quaternion.Euler(0, i * 40 + 7, 0); float l = .34f + (i % 3) * .07f;
                if (v == 0)
                    for (int k = -1; k <= 1; k += 2)
                        Part(PrimitiveType.Cube, t, q * new Vector3(k * .02f, .08f, l * .45f), new Vector3(.05f, .012f, l), (i + k) % 2 == 0 ? leaf : shade, q * Quaternion.Euler(-26, k * 9, 0));
                else Part(PrimitiveType.Sphere, t, q * new Vector3(0, .07f, l * .45f), new Vector3(.2f, .04f, l), i % 2 == 0 ? leaf : shade, q * Quaternion.Euler(-16, 0, 0));
            }
            // Stems leaning out from the crown, each with its head: yarrow's flat crown of florets, comfrey's hanging bells.
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40 + 20, r = .04f + (i % 3) * .08f, h = .52f + (i % 4) * .08f;
                var at = Quaternion.Euler(0, a, 0) * new Vector3(r, 0, 0); var lean = Quaternion.Euler(5 + (i % 3) * 5, a, 0);
                Part(PrimitiveType.Cube, t, at + lean * new Vector3(0, h / 2, 0), new Vector3(.03f, h, .03f), stem, lean);
                var top = at + lean * new Vector3(0, h, 0);
                if (v == 0)
                {
                    Part(PrimitiveType.Cylinder, t, top, new Vector3(.17f, .018f, .17f), heart);
                    for (int k = 0; k < 5; k++) Part(PrimitiveType.Sphere, t, top + Quaternion.Euler(0, k * 72 + i * 23, 0) * new Vector3(.065f, .022f, 0), new Vector3(.09f, .05f, .09f), bloom);
                }
                else
                {
                    // Comfrey: the stem curls over at the top and four bells hang from it, the lowest open-mouthed.
                    var curl = top + lean * new Vector3(0, .02f, .06f);
                    Part(PrimitiveType.Cube, t, (top + curl) / 2, new Vector3(.025f, .025f, .09f), stem, lean * Quaternion.Euler(-30, 0, 0));
                    for (int k = 0; k < 4; k++)
                        Part(PrimitiveType.Sphere, t, curl + lean * new Vector3((k - 1.5f) * .035f, -.05f - (k % 2) * .03f, .02f), new Vector3(.055f, .085f, .055f), k == 3 ? heart : bloom, lean * Quaternion.Euler(0, 0, (k - 1.5f) * 14));
                }
            }
        }
        /// <summary>Mourner's cap: grey caps with black gills in a tight cluster, the tallest in the middle, their stalks sunk a little
        /// so none stands clear of a bank's slope. Fixed tables: draws nothing random.</summary>
        void MournersCap(Transform t)
        {
            var stalk = Tint(art.plaster, Wither(new Color(.74f, .71f, .64f)));
            var cap = Tint(art.plaster, Wither(new Color(.5f, .49f, .47f)));
            var crown = Tint(art.plaster, Wither(new Color(.38f, .37f, .36f)));
            var gill = Tint(art.plaster, new Color(.08f, .07f, .07f));
            // Each cap: where it stands, how tall, how wide, and how far it leans out from the cluster.
            var caps = new[] { (new Vector2(0, .1f), .36f, .3f, 6f), (new Vector2(-.15f, -.02f), .28f, .25f, 14f), (new Vector2(.16f, 0), .3f, .27f, 12f),
                (new Vector2(-.05f, -.16f), .2f, .2f, 18f), (new Vector2(.1f, -.17f), .16f, .17f, 22f), (new Vector2(-.26f, .12f), .18f, .18f, 20f), (new Vector2(.27f, .14f), .14f, .15f, 24f) };
            foreach (var (at, h, w, tilt) in caps)
            {
                float yaw = Mathf.Atan2(at.x, at.y) * Mathf.Rad2Deg; var lean = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(tilt, 0, 0);
                var foot = new Vector3(at.x, -.08f, at.y); var top = foot + lean * new Vector3(0, h + .08f, 0);
                Part(PrimitiveType.Cylinder, t, (foot + top) / 2, new Vector3(w * .24f, (h + .08f) / 2, w * .24f), stalk, lean).name = "Stalk";
                Part(PrimitiveType.Cylinder, t, top - lean * new Vector3(0, .012f, 0), new Vector3(w * .92f, .01f, w * .92f), gill, lean).name = "Gills";   // the black gills under the rim
                Part(PrimitiveType.Sphere, t, top + lean * new Vector3(0, .02f, 0), new Vector3(w, w * .42f, w), cap, lean);
                Part(PrimitiveType.Sphere, t, top + lean * new Vector3(0, .055f, 0), new Vector3(w * .45f, w * .22f, w * .45f), crown, lean);
            }
        }
        /// <summary>
        /// Woodpile: logs with pale cut ends stacked between stakes, a chopping block with an axe in it, split wood scattered
        /// about, and a sawhorse with a log on it in front of the stack (for the eye only: it has no collider).
        /// </summary>
        void Woodpile(Transform t)
        {
            var bark = art.bark; var split = Tint(art.timber, new Color(.62f, .5f, .34f)); var dark = Tint(art.timber, new Color(.3f, .21f, .13f));
            var log = PropMesh("Log", () => TwoTone(Turned(new[] { new Vector2(.225f, -1.1f), new Vector2(.225f, 1.1f) }, 9),
                Joined(Piece(Turned(new[] { new Vector2(0, -1.1f), new Vector2(.225f, -1.1f) }, 9)), Piece(Turned(new[] { new Vector2(.225f, 1.1f), new Vector2(0, 1.1f) }, 9)))));
            var longs = new[] { 1, .94f, 1.03f, .9f, .97f }; var skews = new[] { -2, 1.5f, 0, 2.5f, -1 };   // fixed tables: no two neighbours alike
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 5 - row; i++)
                    MeshPart2(log, t, new Vector3(-1.4f + row * .25f + i * .5f, .25f + row * .42f, .6f), bark, split, Quaternion.Euler(0, skews[(row * 2 + i) % 5], 0) * Quaternion.Euler(90, 0, 0)).transform.localScale = new Vector3(1, longs[(row * 3 + i) % 5], 1);
            foreach (float x in new[] { -1.71f, .91f }) foreach (float z in new[] { .1f, 1.1f }) Stake(t, new Vector3(x, -.1f, z), .14f, 1.24f, dark);   // the stakes that hold the stack
            MeshPart2(PropMesh("Chopping block", () => TwoTone(Turned(new[] { new Vector2(.39f, 0), new Vector2(.35f, .2f), new Vector2(.34f, .6f) }, 12), Turned(new[] { new Vector2(.34f, .6f), new Vector2(0, .6f) }, 12))), t, new Vector3(1.4f, 0, -.6f), bark, split);
            Part(PrimitiveType.Cylinder, t, new Vector3(1.3f, .95f, -.6f), new Vector3(.05f, .35f, .05f), art.timber, Quaternion.Euler(0, 0, 25));  // axe handle
            Part(PrimitiveType.Cube, t, new Vector3(1.43f, .66f, -.6f), new Vector3(.26f, .08f, .17f), art.metal, Quaternion.Euler(0, 0, 25));
            for (int i = 0; i < 6; i++) Part(PrimitiveType.Cube, t, new Vector3(.6f + (i % 3) * .5f, .08f, -1.3f + (i / 3) * .4f), new Vector3(.16f, .15f, .5f), split, Quaternion.Euler(0, i * 37, 90));
            // The sawhorse: two crossed trestles on a rail, a log lying in their forks. It stands clear of the stack's log ends (they reach z -.53).
            foreach (float x in new[] { -1.05f, .05f }) foreach (int s in new[] { -1, 1 })
                Bar(t, new Vector3(x, 0, -1 - s * .26f), new Vector3(x, .78f, -1 + s * .26f), .08f, .07f, dark);
            Part(PrimitiveType.Cube, t, new Vector3(-.5f, .34f, -1), new Vector3(1.3f, .08f, .08f), dark);
            MeshPart2(log, t, new Vector3(-.5f, .68f, -1), bark, split, Quaternion.Euler(0, 0, 90)).transform.localScale = new Vector3(.71f, .8f, .71f);
            Solid(t, new Vector3(-.4f, .6f, .6f), new Vector3(3, 1.3f, 1.2f));
            Workplace(t, "woodpile", new Vector3(1.4f, 0, -1.4f), new Vector3(1.4f, .6f, -.6f));
        }

        // ---------- landmarks (the Peaks, the Ashland Rim) ----------
        /// <summary>Bleached bone: masks, poles, tusks, stall goods.</summary>
        Material Bone { get { return Tint(art.plaster, new Color(.84f, .8f, .7f)); } }
        /// <summary>An emissive colour (flames, the purple tear): a tinted copy of the window glow, glowing in that colour.</summary>
        Material Glowing(Color c, float strength)
        {
            var m = Tint(art.glass, c);
            if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * strength); }
            return m;
        }
        /// <summary>
        /// A curved limb of capsules (ribs, tusks, threads): from a base it rises, bows out by bulge, then curves in to end
        /// reach metres ahead (the yaw's +Z) at height, tapering from thick to thin. Returns the tip.
        /// </summary>
        Vector3 Arc(Transform t, Vector3 at, float yaw, float reach, float height, float bulge, float thick, float thin, Material m, int segs = 8)
        {
            var rot = Quaternion.Euler(0, yaw, 0); var prev = at;
            for (int i = 1; i <= segs; i++)
            {
                float u = i / (float)segs, a = u * Mathf.PI / 2;
                var next = at + rot * new Vector3(0, height * Mathf.Sin(a), reach * (1 - Mathf.Cos(a)) - bulge * Mathf.Sin(u * Mathf.PI));
                var seg = next - prev; float d = Mathf.Lerp(thick, thin, u - .5f / segs);
                Part(PrimitiveType.Capsule, t, (prev + next) / 2, new Vector3(d, (seg.magnitude + d) / 2, d), m, Quaternion.FromToRotation(Vector3.up, seg));
                prev = next;
            }
            return prev;
        }
        /// <summary>
        /// A gateway between two towers (the Sandthrone toll gate, GAME-ONLY): a crenellated span over the gap with an iron
        /// portcullis raised into it, the timber leaves swung open inward (+Z), Sandthrone banners (sand with a rust-red
        /// stripe, GAME-ONLY colours) over the arch, and a barrier boom raised on its post outside (-Z). width: the gap.
        /// </summary>
        void Gate(Transform t, float width)
        {
            float half = width / 2, top = 4.4f, step = width / 6;
            var stone = Dressed(new Color(.44f, .43f, .41f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f)); var planks = Tint(art.timber, new Color(.3f, .21f, .14f));
            var sand = Tint(art.cloth, new Color(.9f, .66f, .24f)); var rust = Tint(art.cloth, new Color(.72f, .1f, .06f));
            // One mesh of dressed stone: the span, reaching into both towers, a string course under its battlements, two stepped
            // corbels a side round the arch (level courses, where two cubes stood on edge), and the merlons.
            var blocks = new List<CombineInstance> { Ashlar(new Vector3(0, top + .7f, 0), new Vector3(width + 3, 1.4f, 2.6f)), Ashlar(new Vector3(0, top + 1.32f, 0), new Vector3(width + 3, .2f, 2.84f)) };
            foreach (int s in new[] { -1, 1 }) { blocks.Add(Ashlar(new Vector3(s * (half - .05f), top - .16f, 0), new Vector3(1.3f, .36f, 2.5f))); blocks.Add(Ashlar(new Vector3(s * (half + .2f), top - .5f, 0), new Vector3(.8f, .36f, 2.5f))); }   // their outer ends run into the towers
            for (int i = 0; i <= 6; i++) foreach (int sz in new[] { -1, 1 }) blocks.Add(Ashlar(new Vector3(-half + i * step, top + 1.75f, sz * 1.05f), new Vector3(.6f, .7f, .45f)));
            Stonework("Gate stone", t, stone, blocks.ToArray());
            // The portcullis, raised: its grid and spiked foot hang below the arch.
            for (float x = -half + .3f; x < half - .2f; x += .45f)
            {
                Part(PrimitiveType.Cube, t, new Vector3(x, top - .2f, -.8f), new Vector3(.08f, 2.4f, .08f), iron);
                Part(PrimitiveType.Cube, t, new Vector3(x, top - 1.45f, -.8f), new Vector3(.12f, .12f, .08f), iron, Quaternion.Euler(0, 0, 45));
            }
            foreach (float y in new[] { top - 1.1f, top - .45f }) Part(PrimitiveType.Cube, t, new Vector3(0, y, -.8f), new Vector3(width - .4f, .09f, .09f), iron);
            // The leaves, open against the passage sides, with iron straps; banners hang over the arch.
            float leaf = half - .15f;
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(s * (half - .25f), 1.8f, 1.1f + leaf / 2), new Vector3(.18f, 3.6f, leaf), planks);
                foreach (float y in new[] { .6f, 1.8f, 3 }) Part(PrimitiveType.Cube, t, new Vector3(s * (half - .36f), y, 1.1f + leaf / 2), new Vector3(.04f, .14f, leaf), iron);
                Solid(t, new Vector3(s * (half - .25f), 1.8f, 1.1f + leaf / 2), new Vector3(.3f, 3.6f, leaf));
                Part(PrimitiveType.Cube, t, new Vector3(s * (half - .8f), top + .25f, -1.36f), new Vector3(1, 2.3f, .04f), sand);
                Part(PrimitiveType.Cube, t, new Vector3(s * (half - .8f), top + .25f, -1.385f), new Vector3(.3f, 2.3f, .02f), rust);
            }
            // Barrier boom outside, raised on its post over a stone counterweight; the rest post opposite takes it when lowered.
            var pivot = new Vector3(-half + .2f, 1.25f, -3.4f); var boom = Quaternion.Euler(0, 0, 72);
            Part(PrimitiveType.Cube, t, new Vector3(pivot.x, .6f, pivot.z), new Vector3(.35f, 1.3f, .35f), planks);
            Part(PrimitiveType.Cube, t, pivot + boom * new Vector3(2.6f, 0, 0), new Vector3(6.4f, .16f, .16f), planks, boom);
            Part(PrimitiveType.Cube, t, pivot + boom * new Vector3(-.75f, 0, 0), new Vector3(.5f, .45f, .4f), RockTint(new Color(.44f, .43f, .41f)), boom);   // one rough stone
            Part(PrimitiveType.Cube, t, pivot + boom * new Vector3(5.5f, 0, 0) + new Vector3(0, -.3f, 0), new Vector3(.04f, .55f, .3f), rust);   // pennant
            Part(PrimitiveType.Cube, t, new Vector3(half - .2f, .55f, pivot.z), new Vector3(.25f, 1.1f, .25f), planks);
            Part(PrimitiveType.Cube, t, new Vector3(half - .2f, 1.12f, pivot.z), new Vector3(.45f, .1f, .25f), iron);
            Solid(t, new Vector3(pivot.x, .6f, pivot.z), new Vector3(.5f, 1.2f, .5f));
            Solid(t, new Vector3(half - .2f, .55f, pivot.z), new Vector3(.4f, 1.1f, .4f));
        }
        /// <summary>
        /// A small fortified hall on a deep footing (the Sandthrone captain's eyrie, GAME-ONLY): a crenellated parapet, an
        /// iron-strapped door in a stone surround with a step, arrow slits, a lit loophole, and Sandthrone banners (sand with
        /// a rust-red stripe, GAME-ONLY colours) either side of the door and flying from a pole on the roof. Front faces -Z.
        /// </summary>
        void Stronghold(Transform t, Vector2 size)
        {
            float w = size.x, d = size.y, h = 5;
            var stone = Dressed(new Color(.47f, .45f, .42f)); var dark = Tint(art.timber, new Color(.2f, .14f, .1f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            var sand = Tint(art.cloth, new Color(.9f, .66f, .24f)); var rust = Tint(art.cloth, new Color(.72f, .1f, .06f)); var slit = Tint(art.timber, new Color(.05f, .04f, .03f));
            BoxPart(t, new Vector3(0, -.75f, 0), new Vector3(w + .7f, 1.7f, d + .7f), Dressed(new Color(.4f, .39f, .37f)), null, 1.75f);   // footing: a low plinth, deep where the perch falls away
            // The hall's stone is one mesh: the body, the string course, the merlons, and the door's surround and step below.
            var blocks = new List<CombineInstance> { Ashlar(new Vector3(0, h / 2, 0), new Vector3(w, h, d)), Ashlar(new Vector3(0, h + .1f, 0), new Vector3(w + .3f, .2f, d + .3f)) };
            int nx = Mathf.Max(2, Mathf.RoundToInt(w / 1.15f)), nz = Mathf.Max(2, Mathf.RoundToInt(d / 1.15f));
            for (int i = 0; i < nx; i++) foreach (int sz in new[] { -1, 1 }) blocks.Add(Ashlar(new Vector3(Mathf.Lerp(-w / 2 + .3f, w / 2 - .3f, i / (nx - 1f)), h + .6f, sz * (d / 2 - .1f)), new Vector3(.6f, .8f, .45f)));
            for (int i = 1; i < nz - 1; i++) foreach (int sx in new[] { -1, 1 }) blocks.Add(Ashlar(new Vector3(sx * (w / 2 - .1f), h + .6f, Mathf.Lerp(-d / 2 + .3f, d / 2 - .3f, i / (nz - 1f))), new Vector3(.45f, .8f, .6f)));
            // The door: planks and straps in a stone surround, a step before it, a lit loophole above.
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.2f, -d / 2 - .06f), new Vector3(1.5f, 2.4f, .12f), dark);
            foreach (float y in new[] { .5f, 1.2f, 1.9f }) Part(PrimitiveType.Cube, t, new Vector3(0, y, -d / 2 - .13f), new Vector3(1.5f, .1f, .04f), iron);
            foreach (int s in new[] { -1, 1 }) blocks.Add(Ashlar(new Vector3(s * .95f, 1.3f, -d / 2 - .1f), new Vector3(.4f, 2.6f, .2f), 1));
            blocks.Add(Ashlar(new Vector3(0, 2.7f, -d / 2 - .1f), new Vector3(2.3f, .4f, .2f), 1));
            blocks.Add(Ashlar(new Vector3(0, .1f, -d / 2 - .45f), new Vector3(2.2f, .2f, .7f), 1));
            Stonework("Keep stone", t, stone, blocks.ToArray());
            Part(PrimitiveType.Cube, t, new Vector3(0, 3.6f, -d / 2 - .02f), new Vector3(.2f, .7f, .06f), art.glass);
            foreach (int sx in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(sx * w * .25f, 3, d / 2 + .02f), new Vector3(.16f, .9f, .06f), slit);
                Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .02f), 3, 0), new Vector3(.06f, .9f, .16f), slit);
                Part(PrimitiveType.Cube, t, new Vector3(sx * 2.05f, h - .15f, -d / 2 - .2f), new Vector3(1.2f, .1f, .1f), dark);   // banner pole
                Part(PrimitiveType.Cube, t, new Vector3(sx * 2.05f, h - 1.55f, -d / 2 - .1f), new Vector3(1, 2.7f, .04f), sand);
                Part(PrimitiveType.Cube, t, new Vector3(sx * 2.05f, h - 1.55f, -d / 2 - .125f), new Vector3(.28f, 2.7f, .02f), rust);
            }
            Part(PrimitiveType.Cylinder, t, new Vector3(w / 2 - .9f, h + 2.4f, d / 2 - .9f), new Vector3(.12f, 2.4f, .12f), dark);   // flagpole
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 - 1.68f, h + 4.1f, d / 2 - .9f), new Vector3(1.5f, .95f, .04f), sand, Quaternion.Euler(0, 0, 3));
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 - 1.68f, h + 4.1f, d / 2 - .925f), new Vector3(1.52f, .26f, .02f), rust, Quaternion.Euler(0, 0, 3));
            Solid(t, new Vector3(0, h / 2, 0), new Vector3(w + .5f, h + 1.2f, d + .5f));
        }
        /// <summary>
        /// Rocks round a raised perch (a ZoneShape with a height): boulders sunk into its slopes all the way round but
        /// for the way up, which faces the prop's -Z. radius: the perch's edge.
        /// </summary>
        void Perch(Transform t, float radius)
        {
            var mat = RockTint(Zone.biome == "mountain" ? MountainStone : new Color(.42f, .41f, .39f));   // the boulders' stone (EdgeRock)
            for (float a = R01 * 10; a < 360; a += 18 + R01 * 10)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(a, 180)) < 38) continue;   // the way up
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward; float r = radius + .6f + R01 * 2.2f, s = 1.8f + R01 * 1.8f, lo = float.MaxValue;
                // Sit on the lowest ground anywhere under it, along the line out and across it (playtest note 21: on a knob the
                // ground fell away beside a boulder seated only along the line, and its far side hung in the air).
                var across = Quaternion.Euler(0, a + 90, 0) * Vector3.forward;
                foreach (float k in new[] { -.7f, 0, .7f }) foreach (float j in new[] { -.7f, 0, .7f }) { var w = t.TransformPoint(dir * (r + k * s) + across * (j * s)); lo = Mathf.Min(lo, HeightAt(w.x, w.z)); }
                var at = dir * r + Vector3.up * (lo - t.position.y);
                Lump(Boulder(), t, at + new Vector3(0, .1f * s, 0), new Vector3(1.5f * s, 1.3f * s, 1.3f * s), mat, R01 * 360);
                Solid(t, at + new Vector3(0, .5f * s, 0), new Vector3(1.1f * s, s, 1.1f * s));
            }
        }
        /// <summary>
        /// A mud wallow (on a sunk ZoneShape painted mud): pools of brown water, churned banks round the rim, and two
        /// rubbing stones worn smooth. Walkable; only the stones are solid.
        /// </summary>
        void Wallow(Transform t, float radius)
        {
            // The pools are wet mud: dark, smooth and a little glossy (a plain Standard material, no grain), so they catch the
            // sky as a sheen instead of reading as a flat grey shape.
            var pool = new Material(art.metal) { name = "Wallow pool", color = new Color(.15f, .115f, .08f) }; pool.SetFloat("_Metallic", 0); pool.SetFloat("_Glossiness", .8f);
            var mud = Tint(art.soil, new Color(.22f, .17f, .12f)); var wet = Tint(art.soil, new Color(.16f, .12f, .085f)); var stone = RockTint(new Color(.42f, .4f, .37f));
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60 + R01 * 40, r = i == 0 ? R01 : radius * (.3f + R01 * .35f), s = i == 0 ? radius * .95f : radius * (.3f + R01 * .25f);
                Part(PrimitiveType.Cylinder, t, Quaternion.Euler(0, a, 0) * new Vector3(0, .03f + i * .004f, r), new Vector3(s, .03f, s * (.55f + R01 * .35f)), pool, Quaternion.Euler(0, R01 * 180, 0));
            }
            // A raised rim of churned mud round the pools: sixteen flattened lumps, each longer than its step so they run
            // together, sunk a little and wetter on some; then the old lower banks pushed out beyond the rim.
            for (int i = 0; i < 16; i++)
            {
                float a = i * 22.5f + (R01 - .5f) * 8, r = radius * (.84f + R01 * .1f), len = radius * (.5f + R01 * .2f);
                Part(PrimitiveType.Sphere, t, Quaternion.Euler(0, a, 0) * new Vector3(0, .08f + R01 * .1f, r), new Vector3(1.6f + R01 * .8f, .6f + R01 * .35f, len), i % 3 == 0 ? wet : mud, Quaternion.Euler(0, a + 90 + (R01 - .5f) * 12, 0));
            }
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36 + R01 * 20;
                Part(PrimitiveType.Sphere, t, Quaternion.Euler(0, a, 0) * new Vector3(0, -.05f, radius * (1.02f + R01 * .25f)), new Vector3(1.4f + R01, .4f + R01 * .2f, 2.2f + R01 * 1.5f), mud, Quaternion.Euler(0, a + 90, 0));
            }
            foreach (var at in new[] { new Vector3(radius * .55f, 0, radius * .35f), new Vector3(-radius * .45f, 0, -radius * .5f) })
            {
                Lump(Boulder(), t, at + new Vector3(0, .35f, 0), new Vector3(2.2f, 1.6f, 1.8f), stone, R01 * 360);
                Solid(t, at + new Vector3(0, .6f, 0), new Vector3(1.7f, 1.2f, 1.5f));
            }
        }
        /// <summary>
        /// The Ash-Walker enclave's cave mouths (CANON, book1 ch.20: the Ash-Walkers live in the deep caves of the Eastern
        /// Ridge; this look is GAME-ONLY): a craggy rock face length wide with three mouths, the great one framed by bone
        /// tusks and lit by a fire within, a hide awning on bone poles over one, a hide curtain drawn half across another,
        /// and salt wherever the Eaters might come in: crusted lintels, a salt line over every threshold, stacked blocks.
        /// Front faces -Z; the rock is solid (the caves are not entered).
        /// </summary>
        void Cave(Transform t, float length)
        {
            float H = 10, D = 7;
            var rock = RockTint(new Color(.37f, .365f, .37f)); var dark = Tint(art.stone, new Color(.035f, .03f, .03f)); var salt = Tint(art.stone, new Color(.94f, .94f, .91f));   // rock: the ash cliffs' grey, so the face runs on into the Ridge
            var hide = Tint(art.cloth, new Color(.56f, .43f, .29f)); var hide2 = Tint(art.cloth, new Color(.67f, .55f, .39f));
            var mouths = new[] { new Vector3(0, 4.2f, 4.6f), new Vector3(-length * .32f, 2.4f, 3), new Vector3(length * .3f, 2.4f, 2.8f) };   // (x, width, height)
            float Floor(float at) { var w = t.TransformPoint(new Vector3(at, 0, 0)); return HeightAt(w.x, w.z) - t.position.y; }
            for (float x = -length / 2; x <= length / 2 + .01f; x += 1.8f)
            {
                float y = Floor(x), bottom = y - 1, top = y + H - 1.5f + R01 * 3;
                foreach (var m in mouths) if (Mathf.Abs(x - m.x) < m.y / 2 + .6f) bottom = Floor(m.x) + m.z;   // a lintel over a mouth
                Part(PrimitiveType.Cube, t, new Vector3(x, (bottom + top) / 2, D / 2 + R01 * .6f), new Vector3(2.2f + R01 * .6f, top - bottom, D), rock, Quaternion.Euler(R01 * 5 - 3, R01 * 10 - 5, R01 * 4 - 2));
                if (R01 < .5f) Lump(Boulder(), t, new Vector3(x, top - .3f, D * .6f), new Vector3(2.6f, 1.8f, 3), rock, R01 * 360);   // a rounded crest
            }
            foreach (var m in mouths)
            {
                float fy = Floor(m.x);
                Part(PrimitiveType.Cube, t, new Vector3(m.x, fy + m.z / 2, 2.7f), new Vector3(m.y + 1.6f, m.z + .2f, 3.6f), dark);   // the dark within
                foreach (int s in new[] { -1, 1 }) Lump(Boulder(), t, new Vector3(m.x + s * m.y * .45f, fy + m.z - .3f, -.05f), new Vector3(1.5f, 1.4f, 1.2f), rock, R01 * 360);   // round the arch
                Part(PrimitiveType.Cube, t, new Vector3(m.x, fy + .03f, -.4f), new Vector3(m.y + .4f, .06f, .3f), salt);   // salt line across the threshold
                Part(PrimitiveType.Cube, t, new Vector3(m.x, fy + m.z + .08f, -.3f), new Vector3(m.y + 1, .16f, .6f), salt, Quaternion.Euler(0, 0, R01 * 4 - 2));   // salt-crusted lintel
            }
            // The great mouth: bone tusks arching over it, a fire inside, salt blocks stacked beside it.
            var g = mouths[0]; float gy = Floor(g.x);
            foreach (int s in new[] { -1, 1 })
            {
                Arc(t, new Vector3(s * (g.y / 2 + .9f), gy - .3f, -1), s > 0 ? -90 : 90, g.y / 2 + .7f, g.z + 1.4f, .5f, .45f, .2f, Bone, 7);
                Solid(t, new Vector3(s * (g.y / 2 + .9f), gy + 1, -1.1f), new Vector3(.7f, 2, .7f));
            }
            if (cone == null) cone = ZoneMeshes.Cone(1, 1);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, gy + .06f, .5f), new Vector3(1.2f, .05f, .7f), art.glass);   // embers on the cave floor
            MeshPart(cone, t, new Vector3(0, gy + .08f, .55f), Glowing(new Color(1, .55f, .2f), 2.2f)).transform.localScale = new Vector3(.35f, 1, .3f);
            Glow(t, new Vector3(0, gy + 1.5f, -.2f), 10, 1.5f, new Color(1, .55f, .25f), 2.4f);
            float bx = g.y / 2 + 2;
            for (int i = 0; i < 3; i++) Part(PrimitiveType.Cube, t, new Vector3(bx + (i == 2 ? .35f : i * .75f), Floor(bx) + (i == 2 ? .75f : .25f), -1.5f), new Vector3(.7f, .5f, .5f), salt, Quaternion.Euler(0, R01 * 20 - 10, 0));
            Solid(t, new Vector3(bx + .4f, Floor(bx) + .5f, -1.5f), new Vector3(1.8f, 1, .8f));
            // A hide awning on bone poles over the second mouth.
            var n = mouths[1]; float ny = Floor(n.x);
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cylinder, t, new Vector3(n.x + s * 1.6f, ny + 1.2f, -2.6f), new Vector3(.12f, 1.2f, .12f), Bone);
                Solid(t, new Vector3(n.x + s * 1.6f, ny + 1.2f, -2.6f), new Vector3(.3f, 2.4f, .3f));
            }
            Part(PrimitiveType.Cylinder, t, new Vector3(n.x, ny + 2.4f, -2.6f), new Vector3(.1f, 1.75f, .1f), Bone, Quaternion.Euler(0, 0, 90));
            Part(PrimitiveType.Cube, t, new Vector3(n.x - .75f, ny + 2.72f, -1.35f), new Vector3(1.9f, .05f, 2.75f), hide, Quaternion.Euler(-13, 0, 2));
            Part(PrimitiveType.Cube, t, new Vector3(n.x + .8f, ny + 2.7f, -1.35f), new Vector3(1.8f, .05f, 2.7f), hide2, Quaternion.Euler(-13, 0, -2));
            // A hide curtain drawn half across the third, on a bone rod.
            var c = mouths[2]; float cy = Floor(c.x);
            Part(PrimitiveType.Cylinder, t, new Vector3(c.x, cy + c.z - .15f, -.25f), new Vector3(.09f, c.y / 2 + .3f, .09f), Bone, Quaternion.Euler(0, 0, 90));
            Part(PrimitiveType.Cube, t, new Vector3(c.x - c.y * .2f, cy + (c.z - .2f) / 2, -.2f), new Vector3(c.y * .62f, c.z - .25f, .05f), hide, Quaternion.Euler(0, 0, 1.5f));
            Solid(t, new Vector3(0, gy + H / 2 - 1, D / 2 + .3f), new Vector3(length + 1, H + 2, D));
        }
        // ---------- walk-in caves ----------
        /// <summary>Walk-in caves ("cavern" props): their passages, known before the ground is painted, the grass sown or a tree grown.</summary>
        /// <summary>
        /// Variant 1, the Root-Mother's Deep under the Veridian Temple (the Verdant Shore's dungeon): the root-stair down from the
        /// Temple's sunk hollow; the Root Gallery, a hall of root columns; a root-choked passage east; the Sap Well, a chamber round a
        /// pool of glowing sap; the Cold Stair down west; and the Root-Mother's Heart, seventeen metres down, where the cold has
        /// got into the root. No stretch steeper than about 30 degrees.
        /// </summary>
        static float[][] RootDeepPlan()
        {
            return new[] {
                new[] { 0f, -1.2f, 2.2f, 3.2f, 0 }, new[] { 0f, 2, 2.3f, 3.2f, -.6f }, new[] { .4f, 6, 2.5f, 3.4f, -2.6f }, new[] { .8f, 10, 2.7f, 3.6f, -4.6f },
                new[] { 1.2f, 14, 3.2f, 4, -6.6f }, new[] { 1f, 18, 5, 5, -8 }, new[] { .5f, 23, 6.5f, 6, -8.6f }, new[] { 0f, 29, 6.2f, 5.8f, -8.9f },
                new[] { 1f, 34, 4, 4.4f, -9.2f }, new[] { 4f, 38, 2.6f, 3.2f, -9.8f }, new[] { 8f, 41.5f, 2.4f, 3, -10.6f }, new[] { 12f, 45, 2.6f, 3.2f, -11.4f },
                new[] { 15f, 50, 5, 4.8f, -12 }, new[] { 16f, 56, 6, 5.2f, -12.2f }, new[] { 14.5f, 62, 4.5f, 4.4f, -12.4f },
                new[] { 11f, 66, 2.6f, 3.2f, -13.2f }, new[] { 6.5f, 69.5f, 2.5f, 3.1f, -14.6f }, new[] { 2f, 73, 2.8f, 3.4f, -16 },
                new[] { 0f, 78, 7, 7, -16.5f }, new[] { -1f, 84, 9, 8.5f, -16.8f }, new[] { -1.5f, 90, 8.5f, 8, -16.8f }, new[] { -2f, 95, 7, 8.5f, -16.8f },
                // The Heart stays tall to its back wall (the Root-Mother stands seven metres high against it), then closes at once.
                new[] { -2f, 98, 5, 8, -16.8f }, new[] { -2f, 100.5f, .3f, .4f, -16.8f } };
        }
        void PrepareHollows()
        {
            Hollow.All.Clear();
            foreach (var p in Zone.props)
                if (p != null && p.kind == "cavern")
                    Hollow.All.Add(new Hollow(string.IsNullOrEmpty(p.name) ? "cavern" : p.name, p.at, p.rotation, CavernPlan(p.variant), (x, z) => HeightAt(x, z)));
        }
        /// <summary>
        /// A cavern's passage in its frame (mouth at the origin, facing -z): rows (x, z, half-width, height, floor drop below the
        /// mouth's ground). Variant 0, Crowsfoot Hollow, the first dungeon: the entrance passage north into the deserters' camp; a
        /// low passage west; the Drop, a steep stepped descent north that takes the passage under the zone's edge and into the
        /// hills (its roof well under the edge's boundary); the Store Caves; a winding passage east down the Deep Stair; and the
        /// Echoing Hall, sixteen metres down under the northern hills, where Caddock holds court. No stretch of floor is steeper
        /// than about 34 degrees (feet and agents climb to 45).
        /// </summary>
        static float[][] CavernPlan(int variant)
        {
            if (variant == 1) return RootDeepPlan();
            return new[] {
                new[] { 0f, -1.2f, 1.9f, 2.7f, 0 }, new[] { 0f, 1.5f, 2f, 2.9f, 0 }, new[] { .3f, 5, 2.4f, 3.3f, 0 }, new[] { .6f, 8.5f, 4.4f, 4.4f, 0 },   // a low, narrow mouth: a hole in the hill, not a gate
                new[] { 1f, 12, 6.4f, 5.6f, 0 }, new[] { .5f, 15.5f, 6.1f, 5.4f, 0 }, new[] { -1.5f, 18.5f, 4, 4.3f, 0 },
                new[] { -4.5f, 20.6f, 2.6f, 3.4f, 0 }, new[] { -8f, 21.6f, 2.5f, 3.3f, -.2f },
                new[] { -11.2f, 23.8f, 2.8f, 3.5f, -.8f }, new[] { -12.8f, 27.4f, 2.6f, 3.5f, -2.2f }, new[] { -12.6f, 31.2f, 2.5f, 3.6f, -4.4f },
                new[] { -11.6f, 35, 2.6f, 3.7f, -7 }, new[] { -10.4f, 38.8f, 2.7f, 3.8f, -9.6f }, new[] { -9.2f, 42.4f, 3, 4, -11.4f },
                new[] { -6.8f, 46.2f, 4.8f, 4.8f, -12.2f }, new[] { -4.4f, 50, 6, 5.4f, -12.5f }, new[] { -1.6f, 53.8f, 5.6f, 5.2f, -12.6f }, new[] { .8f, 57, 3.6f, 4.2f, -12.8f },
                new[] { 3.6f, 59.6f, 2.5f, 3.3f, -13.1f }, new[] { 7.6f, 61.2f, 2.5f, 3.3f, -13.6f }, new[] { 11.8f, 62.6f, 2.6f, 3.4f, -14.3f }, new[] { 15.4f, 64.8f, 2.8f, 3.6f, -15.1f },
                new[] { 17.6f, 68.6f, 3.8f, 4.4f, -15.7f },
                new[] { 18.8f, 73.4f, 7.8f, 7.6f, -16 }, new[] { 18.6f, 78.6f, 9.2f, 8.8f, -16 }, new[] { 16.8f, 83.6f, 8.4f, 8.2f, -16 }, new[] { 14f, 87.4f, 4.6f, 5, -16 },
                new[] { 12f, 89.6f, 1.8f, 2.2f, -16 }, new[] { 11.2f, 90.4f, .3f, .4f, -16 } };
        }
        /// <summary>
        /// A walk-in cave in a rocky knoll (GAME-ONLY: Crowsfoot Hollow, the Sandthrone deserters' hideout). You walk in off the road;
        /// there is no loading. Its passage (<see cref="Hollow"/>) is a lofted rock shell: faceted, drawn from inside and out (so it
        /// casts shadow either way and no sun reaches the floor), one mesh collider, and in the navmesh as an obstacle, over a floor
        /// that is the levelled ground (a ZoneShape pad in the zone data, painted bare earth). Faceted crag lumps heap over and round
        /// it into a knoll, each kept clear of the passage. Torches down the walls; the deserters' camp round a fire in the first
        /// chamber; in the deep hall the Bandit King's throne on a stone dais between braziers, his banner, and the plunder (a usable
        /// pile, "The deserters' plunder"). The light and the air darken as you go in (Hollow.CameraDepth). Draws only from its own
        /// stream (BuildProps gives new landmark kinds one).
        /// </summary>
        void Cavern(Transform t, Hollow h, int variant = 0)
        {
            if (h == null || h.Centre.Count < 3) return;
            bool roots = variant == 1;   // the Root-Mother's Deep: earth and root, not rock; sap-light, not torches
            // The shell's outside is the hillside's rock: dark and mossed in a meadow (it is dug into a brow), the zone's stone elsewhere.
            var rock = roots ? Tint(art.bark, new Color(.3f, .24f, .16f)) : Tint(art.stone, new Color(.42f, .39f, .35f));
            var knoll = roots ? Tint(art.soil, new Color(.24f, .2f, .13f)) : RockTint(Zone.biome == "meadow" ? new Color(.33f, .34f, .29f) : new Color(.46f, .44f, .41f));
            int n = h.Centre.Count; const int K = 14; int P = K + 3;
            var c = new Vector3[n]; var right = new Vector3[n];
            for (int i = 0; i < n; i++) c[i] = t.InverseTransformPoint(h.Centre[i]);
            for (int i = 0; i < n; i++) { var f = c[Mathf.Min(n - 1, i + 1)] - c[Mathf.Max(0, i - 1)]; f.y = 0; f.Normalize(); right[i] = new Vector3(f.z, 0, -f.x); }
            float seed = (Zone.seed % 997) * .173f;
            // A ring: a foot under the floor at the right wall, the arch over from the right wall to the left (walls near upright,
            // the roof rounded, both broken by two scales of noise), a foot under the floor at the left wall.
            var ring = new Vector3[n, P];
            for (int i = 0; i < n; i++)
                for (int k = 0; k < P; k++)
                {
                    float th = Mathf.Clamp(k - 1, 0, K) * Mathf.PI / K, s = h.Along[i];
                    float bump = (Mathf.PerlinNoise(s * .28f + seed, th * 1.6f + 3.1f) - .5f) * .26f + (Mathf.PerlinNoise(s * .9f + 11, th * 4.2f + seed) - .5f) * .12f;
                    float cs = Mathf.Cos(th), sn = Mathf.Sin(th);
                    float side = h.Half[i] * Mathf.Sign(cs) * Mathf.Pow(Mathf.Abs(cs), .75f) * (1 + bump), up = h.Height[i] * Mathf.Pow(Mathf.Max(0, sn), .85f) * (1 + bump * .8f);   // sin(pi) is a hair under 0 in floats: no NaN
                    if (k == 0 || k == P - 1) { side = h.Half[i] * (k == 0 ? 1 : -1) * (1 + bump); up = -1.2f; }
                    ring[i, k] = c[i] + right[i] * side + Vector3.up * up;
                }
            // The shell: faceted (every triangle its own corners, so it shades as broken rock), the stone tiling about every 2.4 m round
            // the arch and along; the inside faces, then the same faces turned out.
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>(); var arc = new float[n, P];
            for (int i = 0; i < n; i++) for (int k = 1; k < P; k++) arc[i, k] = arc[i, k - 1] + Vector3.Distance(ring[i, k], ring[i, k - 1]);
            void Face(int i0, int k0, int i1, int k1, int i2, int k2)
            {
                int b = v.Count; v.Add(ring[i0, k0]); v.Add(ring[i1, k1]); v.Add(ring[i2, k2]);
                uv.Add(new Vector2(arc[i0, k0] / 2.4f, h.Along[i0] / 2.4f)); uv.Add(new Vector2(arc[i1, k1] / 2.4f, h.Along[i1] / 2.4f)); uv.Add(new Vector2(arc[i2, k2] / 2.4f, h.Along[i2] / 2.4f));
                tri.Add(b); tri.Add(b + 1); tri.Add(b + 2);
            }
            for (int i = 0; i + 1 < n; i++)
                for (int k = 0; k + 1 < P; k++) { Face(i, k, i + 1, k, i, k + 1); Face(i, k + 1, i + 1, k, i + 1, k + 1); }
            {   // inward, whichever way that winding came out: a triangle at the crown of a middle ring must face down
                int mid = ((n / 2) * (P - 1) + P / 2) * 6;
                var a = v[tri[mid]]; var b2 = v[tri[mid + 1]]; var d = v[tri[mid + 2]];
                if (Vector3.Cross(b2 - a, d - a).y > 0) for (int q = 0; q < tri.Count; q += 3) { int sw = tri[q + 1]; tri[q + 1] = tri[q + 2]; tri[q + 2] = sw; }
            }
            var shell = new Mesh { name = h.Name + " shell" };
            if (v.Count > 65000) shell.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            shell.SetVertices(v); shell.SetUVs(0, uv); shell.SetTriangles(tri, 0); shell.RecalculateNormals(); shell.RecalculateBounds();
            var body = MeshPart(shell, t, Vector3.zero, rock);
            body.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;   // no sun reaches the floor
            body.AddComponent<MeshCollider>().sharedMesh = shell; body.AddComponent<NavWalkable>();   // walls and roof: in the navmesh as obstacles
            // What shows inside is the painted lining over that shell (the shell stays the collider and the navmesh's obstacle, not
            // drawn), and against it the rock, dripstone, root and earth that break the walls up. With no cave art the shell shows.
            var paint = roots ? art.caveEarth : art.cave;
            var lining = paint != null ? Tint(paint, roots ? new Color(.5f, .4f, .27f) : new Color(.56f, .51f, .45f)) : null;
            if (lining != null) { body.GetComponent<MeshRenderer>().enabled = false; CaveLining(t, h, c, ring, n, P, seed, lining); }
            CaveDressing(t, h, c, right, ring, n, P, roots, lining ?? (roots ? knoll : rock));

            // The knoll: an outer shell round the passage, 2-4 m of rock thick and thickest over the top (so it heaps into a mound),
            // broken by big slow swells; closed past the passage's end; at the mouth, a cut face of rock joining it to the passage.
            float Thick(int i, int k)
            {
                float th = Mathf.Clamp(k - 1, 0, K) * Mathf.PI / K;
                return 2f + 2.4f * Mathf.PerlinNoise(h.Along[i] * .16f + seed + 40, th * 1.3f + 5) + .9f * Mathf.PerlinNoise(h.Along[i] * .55f + seed, th * 3.4f + 9) + Mathf.Sin(th) * h.Height[i] * .35f;   // big swells and smaller knuckles
            }
            int m = n + 3; var outer = new Vector3[m, P]; var outAlong = new float[m];
            for (int i = 0; i < n; i++)
            {
                outAlong[i] = h.Along[i];
                for (int k = 0; k < P; k++)
                {
                    bool footK = k == 0 || k == P - 1;
                    var dir = footK ? right[i] * (k == 0 ? 1 : -1) : ring[i, k] - (c[i] + Vector3.up * h.Height[i] * .35f);
                    outer[i, k] = ring[i, k] + dir.normalized * Thick(i, k);
                    if (footK) outer[i, k].y = c[i].y - 1.2f;   // the foot stays under the floor
                }
            }
            var fwdEnd = c[n - 1] - c[n - 2]; fwdEnd.y = 0; fwdEnd.Normalize();
            var tip = c[n - 1] + fwdEnd * 3.5f + Vector3.up * 1.4f;
            for (int j = 1; j <= 3; j++)
            {
                outAlong[n - 1 + j] = h.Along[n - 1] + j * 1.2f;
                for (int k = 0; k < P; k++)
                {
                    var q = Vector3.Lerp(outer[n - 1, k], tip, j / 3f);
                    if (k == 0 || k == P - 1) q.y = c[n - 1].y - 1.2f;
                    outer[n - 1 + j, k] = q;
                }
            }
            var ov = new List<Vector3>(); var ouv = new List<Vector2>(); var ot = new List<int>(); var oarc = new float[m, P];
            for (int i = 0; i < m; i++) for (int k = 1; k < P; k++) oarc[i, k] = oarc[i, k - 1] + Vector3.Distance(outer[i, k], outer[i, k - 1]);
            void OFace(Vector3 a, Vector3 b, Vector3 d, Vector2 ua, Vector2 ub, Vector2 ud)
            {
                int o = ov.Count; ov.Add(a); ov.Add(b); ov.Add(d); ouv.Add(ua); ouv.Add(ub); ouv.Add(ud); ot.Add(o); ot.Add(o + 1); ot.Add(o + 2);
            }
            Vector2 OU(int i, int k) { return new Vector2(oarc[i, k] / 2.6f, outAlong[i] / 2.6f); }
            for (int i = 0; i + 1 < m; i++)
                for (int k = 0; k + 1 < P; k++)
                {
                    OFace(outer[i, k], outer[i + 1, k], outer[i, k + 1], OU(i, k), OU(i + 1, k), OU(i, k + 1));
                    OFace(outer[i, k + 1], outer[i + 1, k], outer[i + 1, k + 1], OU(i, k + 1), OU(i + 1, k), OU(i + 1, k + 1));
                }
            {   // outward, whichever way that winding came out: a triangle at the crown of a middle ring must face up
                int mid = ((n / 2) * (P - 1) + P / 2) * 6;
                if (Vector3.Cross(ov[ot[mid + 1]] - ov[ot[mid]], ov[ot[mid + 2]] - ov[ot[mid]]).y < 0) for (int q = 0; q < ot.Count; q += 3) { int sw = ot[q + 1]; ot[q + 1] = ot[q + 2]; ot[q + 2] = sw; }
            }
            int lipStart = ot.Count; var mouthOut = c[0] - c[1]; mouthOut.y = 0; mouthOut.Normalize();
            for (int k = 0; k + 1 < P; k++)   // the mouth's cut face, from the passage's edge out to the knoll's
            {
                OFace(ring[0, k], ring[0, k + 1], outer[0, k], new Vector2(0, arc[0, k] / 2.6f), new Vector2(0, arc[0, k + 1] / 2.6f), new Vector2(Thick(0, k) / 2.6f, arc[0, k] / 2.6f));
                OFace(ring[0, k + 1], outer[0, k + 1], outer[0, k], new Vector2(0, arc[0, k + 1] / 2.6f), new Vector2(Thick(0, k + 1) / 2.6f, arc[0, k + 1] / 2.6f), new Vector2(Thick(0, k) / 2.6f, arc[0, k] / 2.6f));
            }
            {   // facing out of the mouth
                int q0 = lipStart + (P / 2) * 6;
                if (Vector3.Dot(Vector3.Cross(ov[ot[q0 + 1]] - ov[ot[q0]], ov[ot[q0 + 2]] - ov[ot[q0]]), mouthOut) < 0)
                    for (int q = lipStart; q < ot.Count; q += 3) { int sw = ot[q + 1]; ot[q + 1] = ot[q + 2]; ot[q + 2] = sw; }
            }
            var mound = new Mesh { name = h.Name + " knoll" };
            if (ov.Count > 65000) mound.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mound.SetVertices(ov); mound.SetUVs(0, ouv); mound.SetTriangles(ot, 0); mound.RecalculateNormals(); mound.RecalculateBounds();
            var hill = MeshPart(mound, t, Vector3.zero, knoll);
            hill.AddComponent<MeshCollider>().sharedMesh = mound; hill.AddComponent<NavWalkable>();

            // Crests sunk into the mound's top and buttresses into its flanks (kept clear of the passage's walls); loose rock round the foot.
            bool Clear(Vector3 at, float radius)
            {
                for (int j = 0; j < n; j++) if (new Vector2(at.x - c[j].x, at.z - c[j].z).magnitude < h.Half[j] * 1.2f + .3f + radius) return false;
                return true;
            }
            void Heap(Vector3 bottom, Vector3 scale, float yaw, bool solid)
            {
                var lump = MeshPart(CragRock((int)(R01 * 6)), t, bottom + Vector3.up * .2f * scale.y, knoll, Quaternion.Euler((R01 - .5f) * 8, yaw, (R01 - .5f) * 8));
                lump.transform.localScale = scale;
                if (solid) { lump.AddComponent<MeshCollider>().sharedMesh = lump.GetComponent<MeshFilter>().sharedMesh; lump.AddComponent<NavWalkable>(); }
            }
            var foot = new List<(Vector3 at, Vector3 out1)>();
            for (float s = 1.5f; s < h.Length; s += 2.3f)
            {
                int i = 0; while (i + 1 < n && h.Along[i + 1] <= s) i++;
                if (!h.NearSurface(i) || roots) continue;   // deep under the land: nothing of the knoll shows (a root deep shows nothing anyway)
                var fwd = new Vector3(-right[i].z, 0, right[i].x); float yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
                // Crests: one or two rocks broken up out of the top, the bigger over the chambers.
                for (int r = 0; r < 2; r++)
                    if (R01 < (r == 0 ? .85f : .4f))
                    {
                        int k2 = P / 2 + (int)((R01 - .5f) * 6); var crest = outer[i, k2] + Vector3.down * 1.1f;
                        float roof = float.MinValue;   // the highest roof within its reach along the passage (a chamber's rises past a tunnel's)
                        for (int j = 0; j < n; j++) if (Mathf.Abs(h.Along[j] - h.Along[i]) < 4.5f) roof = Mathf.Max(roof, c[j].y + h.Height[j] * 1.25f + .5f);
                        crest.y = Mathf.Max(crest.y, roof);   // its flat underside never reaches down through the roof below
                        Heap(crest, new Vector3(2.8f + R01 * 2.4f + h.Half[i] * .35f, 2 + R01 * 2.6f + h.Height[i] * .2f, 2.8f + R01 * 2.8f), R01 * 360, false);
                    }
                foreach (int side in new[] { -1, 1 })
                {
                    float fx = 2.4f + R01 * 1.4f, fz = 3 + R01 * 1.6f, fy = h.Height[i] * (.6f + R01 * .4f) + 1, keep = R01;
                    var at = outer[i, side > 0 ? 2 : P - 3] + right[i] * side * fx * .15f; at.y = c[i].y - .8f;
                    if (keep < .75f && Clear(at, fx * .36f)) { Heap(at, new Vector3(fx, fy, fz), yaw + (R01 - .5f) * 14, true); foot.Add((at, right[i] * side * fx * .55f)); }
                }
            }
            var loose = RockTint(new Color(.4f, .38f, .35f));
            foreach (var (at, out1) in foot)   // loose rock round the knoll's foot, sunk (no colliders)
            {
                var p = at + out1 * (1.2f + R01 * .8f); float size = .45f + R01 * .7f;
                var wp = t.TransformPoint(p); if (!RockMayLie(new Vector2(wp.x, wp.z)) || !Clear(p, size + 2)) continue;
                p.y = LocalGround(t, p.x, p.z) - size * .3f;
                Lump(BoulderAt((int)(R01 * 6)), t, p, new Vector3(size * 1.3f, size, size * 1.1f), loose, R01 * 360);
            }

            // The floor: the passage's own, from wall foot to wall foot (the land is cut away inside, see ZoneMeshes.Ground), and a lip
            // of it out of the mouth onto the ground outside, so the way in has no seam. Earth over rock; walkable (navmesh), solid.
            {
                var fv = new List<Vector3>(); var fuv = new List<Vector2>(); var ft = new List<int>();
                var outward = c[0] - c[1]; outward.y = 0; outward.Normalize();
                for (int i = -1; i < n; i++)
                {
                    int r = Mathf.Max(0, i); Vector3 a = ring[r, 1], b = ring[r, P - 2];
                    if (i < 0) { a += outward * 2.4f; b += outward * 2.4f; a.y = LocalGround(t, a.x, a.z) + .02f; b.y = LocalGround(t, b.x, b.z) + .02f; }   // over the land's cut edge
                    else a.y = b.y = c[r].y;
                    float fvv = (i < 0 ? -2.4f : h.Along[r]) / 2.4f;
                    fv.Add(a); fv.Add(b); fuv.Add(new Vector2(0, fvv)); fuv.Add(new Vector2(Vector3.Distance(a, b) / 2.4f, fvv));
                }
                for (int i = 0; i < n; i++) { int o = i * 2; ft.AddRange(new[] { o, o + 2, o + 1, o + 1, o + 2, o + 3 }); }
                if (Vector3.Cross(fv[ft[1]] - fv[ft[0]], fv[ft[2]] - fv[ft[0]]).y < 0) for (int q = 0; q < ft.Count; q += 3) { int sw = ft[q + 1]; ft[q + 1] = ft[q + 2]; ft[q + 2] = sw; }   // facing up
                var floorMesh = new Mesh { name = h.Name + " floor" }; floorMesh.SetVertices(fv); floorMesh.SetUVs(0, fuv); floorMesh.SetTriangles(ft, 0); floorMesh.RecalculateNormals(); floorMesh.RecalculateBounds();
                var ground = MeshPart(floorMesh, t, Vector3.zero, Tint(art.soil, roots ? new Color(.2f, .16f, .1f) : new Color(.34f, .3f, .25f)));
                ground.AddComponent<MeshCollider>().sharedMesh = floorMesh; ground.AddComponent<NavWalkable>();
            }
            // Where things stand inside: the passage floor under a point (in the root's frame), not the land far above it.
            float FloorY(float x, float z)
            {
                var w = t.TransformPoint(new Vector3(x, 0, z)); int i = h.Nearest(new Vector2(w.x, w.z), out _);
                return c[i].y;
            }
            int RingAt(float s) { int i = 0; while (i + 1 < n && h.Along[i + 1] <= s) i++; return i; }
            Vector3 On(float s, float aside) { var w = h.At(s, aside); var l = t.InverseTransformPoint(w); l.y = c[RingAt(s)].y; return l; }
            Quaternion Along(float s) { int i = RingAt(s); var f = new Vector3(-right[i].z, 0, right[i].x); return Quaternion.LookRotation(f); }
            float Drop(int i) { return c[i].y - c[0].y; }
            // The chambers, found from the passage itself: the widest ring down in the store caves' stretch, and the hall at the far end.
            int widest(float from, float to) { int best = -1; for (int i = 0; i < n; i++) if (h.Along[i] >= from && h.Along[i] <= to && (best < 0 || h.Half[i] > h.Half[best])) best = i; return best; }
            int store = widest(h.Length * .45f, h.Length * .7f), hall = widest(h.Length - 28, h.Length - 4);
            // Solid where it stands: you walk round a brazier, the desk, a stack of crates or the plunder, and so do the deserters.
            void Block(Vector3 at, Vector3 size, Quaternion rot)
            {
                var o = new GameObject("Solid"); o.transform.SetParent(t, false); o.transform.localPosition = at + rot * new Vector3(0, size.y / 2, 0); o.transform.localRotation = rot;
                o.AddComponent<BoxCollider>().size = size; o.AddComponent<NavBlocker>();
            }


            if (roots)
            {
                RootDeepInterior(t, h, c, right, ring, n, P, FloorY, RingAt, On, Along, Block);
                return;
            }
            // Inside: torches down the walls, alternating sides, set on the rock at about head height; every twelfth one in the deep
            // left dark (the deserters don't waste pitch on every stretch).
            int torchNo = 0;
            for (float s = 3.5f; s < h.Length - 2; s += 6.6f, torchNo++)
            {
                int i = RingAt(s), side = torchNo % 2 == 0 ? 1 : -1;
                if (Drop(i) < -10 && torchNo % 12 == 11) continue;
                int best = side > 0 ? 1 : P - 2;
                for (int k = 1; k < P - 1; k++)
                    if ((side > 0 ? k <= P / 2 : k >= P / 2) && Mathf.Abs(ring[i, k].y - c[i].y - 1.9f) < Mathf.Abs(ring[i, best].y - c[i].y - 1.9f)) best = k;
                var wallAt = ring[i, best]; var inward = c[i] + Vector3.up * 1.9f - wallAt; inward.y = 0; inward.Normalize();
                Torch(t, wallAt - inward * .05f, inward);
            }
            var hide = Tint(art.cloth, new Color(.5f, .4f, .29f)); var hide2 = Tint(art.cloth, new Color(.38f, .31f, .24f));
            var sand = Tint(art.cloth, new Color(.72f, .6f, .37f)); var wood = Tint(art.timber, new Color(.3f, .21f, .13f)); var sack = Tint(art.hay, new Color(.62f, .54f, .37f));
            var crate = Tint(art.timber, new Color(.44f, .32f, .2f)); var iron = Tint(art.metal, new Color(.2f, .19f, .19f));
            // The camp chamber: a fire with a pot on a tripod, bedrolls round it, stolen grain, crates and barrels, a torn banner.
            var fire = new Vector3(1.2f, FloorY(1.2f, 12.6f), 12.6f);
            Campfire(t, fire);
            for (int k = 0; k < 3; k++) { var q = Quaternion.Euler(0, k * 120, 0); Part(PrimitiveType.Cylinder, t, fire + q * new Vector3(0, .62f, .38f), new Vector3(.05f, .66f, .05f), wood, q * Quaternion.Euler(-26, 0, 0)); }
            Part(PrimitiveType.Cylinder, t, fire + new Vector3(0, .62f, 0), new Vector3(.42f, .2f, .42f), iron);   // the pot
            foreach (var deg in new[] { 55f, 115f, 235f, 300f })
            {
                var q = Quaternion.Euler(0, deg, 0); var at = fire + q * new Vector3(0, 0, 2.9f); at.y = FloorY(at.x, at.z);
                Part(PrimitiveType.Cube, t, at + new Vector3(0, .07f, 0), new Vector3(.85f, .12f, 1.9f), deg < 180 ? hide : hide2, q);
                Part(PrimitiveType.Cube, t, at + q * new Vector3(0, .17f, .75f), new Vector3(.55f, .14f, .3f), sand, q);   // a rolled cloak for a pillow
            }
            for (int k = 0; k < 4; k++) Part(PrimitiveType.Sphere, t, new Vector3(4.4f + (k % 2) * .7f, FloorY(4.6f, 9.8f) + .35f, 9.5f + k * .45f), new Vector3(.8f, .7f, .7f), sack, Quaternion.Euler(0, k * 40, k * 8));
            Part(PrimitiveType.Sphere, t, new Vector3(3.6f, FloorY(3.6f, 9.4f) + .12f, 9.4f), new Vector3(1.1f, .22f, .8f), Tint(art.hay, new Color(.8f, .72f, .5f)));   // spilt grain
            for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, t, new Vector3(-3.9f + (k == 2 ? .45f : k * .9f), FloorY(-3.5f, 15.6f) + (k == 2 ? 1.35f : .45f), 15.6f), Vector3.one * .9f, crate, Quaternion.Euler(0, 12, 0));
            for (int k = 0; k < 2; k++) Part(PrimitiveType.Cylinder, t, new Vector3(5.1f, FloorY(5.1f, 13.6f) + .5f, 13.2f + k * .8f), new Vector3(.7f, .5f, .7f), wood);
            Banner(t, new Vector3(-4.6f, FloorY(-4.6f, 9.8f), 9.8f), 20, sand, wood, 2.9f);
            // The low passage: a dropped sack, a spent torch.
            Part(PrimitiveType.Sphere, t, new Vector3(-7.4f, FloorY(-7.4f, 20.6f) + .3f, 20.6f), new Vector3(.7f, .55f, .6f), sack, Quaternion.Euler(0, 30, 20));
            Part(PrimitiveType.Cylinder, t, new Vector3(-9.8f, FloorY(-9.8f, 22.9f) + .05f, 22.9f), new Vector3(.06f, .35f, .06f), wood, Quaternion.Euler(0, 40, 88));
            // None of the camp has a collider: the nodes keep clear of it by these (ZoneBuilder.Nodes, keepClear).
            KeepClear(t, fire, 1.2f);
            foreach (var deg in new[] { 55f, 115f, 235f, 300f }) KeepClear(t, fire + Quaternion.Euler(0, deg, 0) * new Vector3(0, 0, 2.9f), 1.1f);
            KeepClear(t, new Vector3(4.6f, 0, 10.1f), 1.2f); KeepClear(t, new Vector3(-3.45f, 0, 15.6f), 1); KeepClear(t, new Vector3(5.1f, 0, 13.6f), .6f);
            KeepClear(t, new Vector3(-4.6f, 0, 9.8f), .5f); KeepClear(t, new Vector3(-7.4f, 0, 20.6f), .5f); KeepClear(t, new Vector3(-9.8f, 0, 22.9f), .4f);

            // The Drop, and wherever the floor runs steep: plank treads pegged across it and a rope rail on posts down one wall.
            float lastPost = -9;
            for (float s = 0; s < h.Length - 1; s += .55f)
            {
                int i = RingAt(s), j = Mathf.Min(n - 1, i + 2); float run = h.Along[j] - h.Along[i];
                if (run <= 0 || (c[i].y - c[j].y) / run < .28f) continue;
                var q = Along(s); float w = h.Half[i] * .62f;
                Part(PrimitiveType.Cube, t, On(s, 0) + Vector3.up * .04f, new Vector3(w * 2, .07f, .3f), wood, q); KeepClear(t, On(s, 0), w);
                if (s - lastPost < 2.4f) continue;
                var post = On(s, h.Half[i] * .72f);
                Part(PrimitiveType.Cylinder, t, post + Vector3.up * .55f, new Vector3(.07f, .55f, .07f), wood); KeepClear(t, post, .4f);
                if (lastPost > 0) { var prev = On(lastPost, h.Half[RingAt(lastPost)] * .72f) + Vector3.up * 1.05f; var top = post + Vector3.up * 1.05f; var d = top - prev;
                    Part(PrimitiveType.Cylinder, t, (prev + top) / 2, new Vector3(.035f, d.magnitude / 2, .035f), Tint(art.hay, new Color(.55f, .47f, .32f)), Quaternion.FromToRotation(Vector3.up, d)); }
                lastPost = s;
            }
            // The deep: a faint blue-green glow of fungus on the walls, little lights of it every so often; puddles where water seeps.
            var fungus = Glowing(new Color(.3f, .95f, .8f), .9f); var puddle = Tint(art.stone, new Color(.2f, .18f, .15f));   // wet rock, a shade under the floor (metal went black in the dark: no reflections in a cave)
            for (float s = 0; s < h.Length - 2; s += 2.8f)
            {
                int i = RingAt(s); if (Drop(i) > -8) continue;
                foreach (int side in new[] { -1, 1 })
                {
                    if (R01 < .45f) continue;
                    int k = side > 0 ? 2 + (int)(R01 * 3) : P - 3 - (int)(R01 * 3);
                    var at = ring[i, k]; var inward = c[i] + Vector3.up * (at.y - c[i].y) - at; inward.y = 0; inward.Normalize();
                    for (int f = 0; f < 3 + (int)(R01 * 3); f++)
                        Part(PrimitiveType.Sphere, t, at + inward * .06f + new Vector3((R01 - .5f) * .6f, (R01 - .5f) * .5f, (R01 - .5f) * .6f), new Vector3(.16f + R01 * .1f, .05f, .12f + R01 * .08f), fungus, Quaternion.LookRotation(inward) * Quaternion.Euler(90, 0, 0));
                }
                if ((int)(s / 2.8f) % 5 == 0) Glow(t, c[i] + Vector3.up * 1.2f, 6, .45f, new Color(.35f, .95f, .82f), .45f);
                if (R01 < .3f) Part(PrimitiveType.Cylinder, t, On(s, (R01 - .5f) * h.Half[i]) + Vector3.up * .015f, new Vector3(.9f + R01 * .8f, .01f, .6f + R01 * .6f), puddle, Along(s) * Quaternion.Euler(0, R01 * 90, 0));
            }
            // The Store Caves: the stolen stores stacked along the walls, the Quartermaster's desk (a plank on two barrels, his
            // ledger and a candle), and a side way choked with fallen rock.
            if (store >= 0)
            {
                float s0 = h.Along[store]; var q0 = Along(s0);
                for (int k = 0; k < 5; k++)
                {
                    var at = On(s0 - 3 + k * 1.4f, (k % 2 == 0 ? 1 : -1) * h.Half[RingAt(s0 - 3 + k * 1.4f)] * .7f);
                    for (int stack = 0; stack < 1 + k % 3; stack++) Part(PrimitiveType.Cube, t, at + Vector3.up * (.45f + stack * .9f), Vector3.one * .9f, crate, q0 * Quaternion.Euler(0, k * 17 + stack * 9, 0));
                    Block(at, new Vector3(1.05f, .9f * (1 + k % 3), 1.05f), q0 * Quaternion.Euler(0, k * 17 + 9, 0));
                    Part(PrimitiveType.Sphere, t, at + q0 * new Vector3(0, .35f, 1.1f), new Vector3(.8f, .7f, .7f), sack, Quaternion.Euler(0, k * 40, 0));
                    KeepClear(t, at + q0 * new Vector3(0, 0, .5f), 1.2f);   // the stack and its sack
                }
                var desk = On(s0 + 1.5f, -h.Half[store] * .45f); KeepClear(t, desk, 1.3f);
                foreach (int b in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, t, desk + q0 * new Vector3(b * .7f, .45f, 0), new Vector3(.55f, .45f, .55f), wood);
                Part(PrimitiveType.Cube, t, desk + Vector3.up * .95f, new Vector3(2, .08f, .8f), wood, q0);
                Part(PrimitiveType.Cube, t, desk + q0 * new Vector3(-.3f, 1.03f, 0), new Vector3(.4f, .06f, .3f), Tint(art.cloth, new Color(.4f, .22f, .14f)), q0 * Quaternion.Euler(0, 8, 0));   // the ledger
                Part(PrimitiveType.Cylinder, t, desk + q0 * new Vector3(.5f, 1.1f, .1f), new Vector3(.06f, .08f, .06f), Tint(art.plaster, new Color(.9f, .86f, .75f)));   // a candle
                Glow(t, desk + q0 * new Vector3(.5f, 1.4f, .1f), 5, .9f, new Color(1, .7f, .4f), .9f).gameObject.AddComponent<Flicker>();
                Block(desk, new Vector3(2.1f, 1, .9f), q0);
                var warm = On(s0 - 1.5f, h.Half[store] * .3f); Brazier(t, warm, null); Block(warm, new Vector3(.8f, 1.2f, .8f), Quaternion.identity); KeepClear(t, warm, .6f);   // off the way through
                // The choked side way: a dark mouth in the left wall and a heap of fallen rock across it.
                int sw2 = Mathf.Min(n - 1, store + 3); var wall = ring[sw2, P - 5]; var inw = c[sw2] + Vector3.up * (wall.y - c[sw2].y) - wall; inw.y = 0; inw.Normalize();
                Part(PrimitiveType.Sphere, t, wall - inw * .1f + Vector3.up * .3f, new Vector3(2.2f, 2.6f, .6f), Tint(art.stone, new Color(.03f, .03f, .03f)), Quaternion.LookRotation(inw));
                for (int k = 0; k < 7; k++) Lump(BoulderAt(k), t, wall + inw * (.3f + (k % 3) * .35f) + new Vector3((R01 - .5f) * 1.6f, .2f + (k / 3) * .35f, (R01 - .5f) * 1.6f), Vector3.one * (.55f + R01 * .5f), Tint(art.stone, new Color(.38f, .36f, .33f)), R01 * 360);
                KeepClear(t, wall + inw * .6f, 1.6f);   // the fallen rock
            }
            // The Echoing Hall: stalagmites round its edges, stalactites overhead, and at the far end the throne on its dais facing the
            // way in, braziers either side, the banner behind, the plunder.
            if (hall >= 0)
            {
                var stone = lining ?? Tint(art.stone, new Color(.4f, .37f, .34f));   // dripstone in the wall's own paint
                for (int i = 0; i < n; i++)
                {
                    if (h.Half[i] < 6 || h.Along[i] < h.Length - 30 || i % 2 == 1) continue;
                    foreach (int side in new[] { -1, 1 })
                    {
                        if (R01 < .35f) continue;
                        float off = h.Half[i] * (.72f + R01 * .18f) * side, tall = 1 + R01 * 1.8f;
                        var baseAt = c[i] + right[i] * off; baseAt.y = c[i].y - .1f;
                        MeshPart(Drip(i), t, baseAt, stone).transform.localScale = new Vector3(.35f + R01 * .35f, tall, .35f + R01 * .35f);
                        var hang = c[i] + right[i] * (off * .75f) + Vector3.up * h.Height[i] * .92f;
                        MeshPart(Drip(i + 1), t, hang, stone, Quaternion.Euler(180, R01 * 360, 0)).transform.localScale = new Vector3(.25f + R01 * .25f, .8f + R01 * 1.4f, .25f + R01 * .25f);
                    }
                }
                float sEnd = h.Length - 4.2f; var seat = On(sEnd, 0); var toDoor = On(h.Length - 20, 0) - seat; toDoor.y = 0;
                var face = Quaternion.LookRotation(toDoor.normalized);
                Throne(t, seat, face, sand, wood);
                foreach (int k in new[] { -1, 1 }) { var at = seat + face * new Vector3(k * 2.4f, 0, .6f); at.y = FloorY(at.x, at.z); Brazier(t, at, null); Block(at, new Vector3(.8f, 1.2f, .8f), Quaternion.identity); }
                foreach (int k in new[] { -1, 1 }) { var at = On(h.Along[hall], k * h.Half[hall] * .6f); Brazier(t, at, null); Block(at, new Vector3(.8f, 1.2f, .8f), Quaternion.identity); }
                Banner(t, seat + face * new Vector3(0, 0, -1.6f), face.eulerAngles.y, sand, wood, 3.6f);
                KeepClear(t, seat, 1.2f); KeepClear(t, seat + face * new Vector3(0, 0, -1.6f), .6f);
                foreach (int k in new[] { -1, 1 }) { KeepClear(t, seat + face * new Vector3(k * 2.4f, 0, .6f), .6f); KeepClear(t, On(h.Along[hall], k * h.Half[hall] * .6f), .6f); }
                var loot = new GameObject("The deserters' plunder").transform; loot.SetParent(t, false);
                var lootAt = seat + face * new Vector3(-3.4f, 0, .9f); lootAt.y = FloorY(lootAt.x, lootAt.z); loot.localPosition = lootAt; loot.localRotation = face;
                Block(lootAt, new Vector3(1.9f, 1.8f, 1), face);   // the crates; the sacks and the open chest in front stay underfoot
                KeepClear(t, lootAt + face * new Vector3(0, 0, .5f), 1.8f);
                for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, loot, new Vector3(k == 2 ? .1f : k * .9f - .45f, k == 2 ? 1.35f : .45f, 0), Vector3.one * .9f, Tint(art.timber, new Color(.42f, .31f, .19f)), Quaternion.Euler(0, k * 9, 0));
                for (int k = 0; k < 4; k++) Part(PrimitiveType.Sphere, loot, new Vector3(-1.3f + k * .35f, .3f, .9f + (k % 2) * .3f), new Vector3(.75f, .6f, .65f), sack, Quaternion.Euler(0, k * 50, 0));
                Part(PrimitiveType.Cube, loot, new Vector3(1.4f, .3f, .8f), new Vector3(.9f, .6f, .6f), Tint(art.timber, new Color(.3f, .2f, .12f)));   // a chest
                Part(PrimitiveType.Cube, loot, new Vector3(1.4f, .68f, .56f), new Vector3(.92f, .08f, .62f), Tint(art.timber, new Color(.3f, .2f, .12f)), Quaternion.Euler(-38, 0, 0));   // its lid, thrown back
                var gold = Glowing(new Color(1, .78f, .3f), .5f);
                for (int k = 0; k < 7; k++) Part(PrimitiveType.Sphere, loot, new Vector3(1.2f + (k % 3) * .17f, .62f + (k / 3) * .04f, .75f + (k % 2) * .14f), new Vector3(.14f, .05f, .14f), gold);
                Interactables.Add(new ZoneInteractable { name = loot.name, prompt = "Search the deserters' plunder", kind = "crates", position = loot.position, root = loot });
                for (int k = 0; k < 4; k++)
                {
                    var at = On(h.Along[hall] + (R01 - .5f) * 8, (R01 - .5f) * h.Half[hall]);
                    for (int b = 0; b < 2; b++) Part(PrimitiveType.Cylinder, t, at + new Vector3(b * .2f, .05f, 0), new Vector3(.07f, .3f, .07f), Bone, Quaternion.Euler(0, b * 70 + k * 40, 90));
                }
            }
        }
        /// <summary>
        /// The Root-Mother's Deep (CANON-EXPANDED: the Veridian Temple and the Root-Mother are Book 3's; the deep is GAME-ONLY): a
        /// passage of earth and root under the Temple, lit by veins of glowing sap in the walls and teal fungus, not torches. Root
        /// columns in the Gallery; a pool of emerald sap in the Sap Well with a Keeper's votive stones round it; and in the Heart, the
        /// Root-Mother herself, a vast knot of root in the back wall with a hollow where a face would be, the cold got into her: a black
        /// many-faced rod lodged in the root with hoarfrost spreading from it, which the quest has you salt ("The cold in the root").
        /// Its own stream (TreeRandom at the mouth).
        /// </summary>
        void RootDeepInterior(Transform t, Hollow h, Vector3[] c, Vector3[] right, Vector3[,] ring, int n, int P, Func<float, float, float> FloorY, Func<float, int> RingAt, Func<float, float, Vector3> On, Func<float, Quaternion> Along, Action<Vector3, Vector3, Quaternion> Block)
        {
            var dr = TreeRandom(t.position); float D() { return (float)dr.NextDouble(); }
            var bark = Tint(art.bark, new Color(.34f, .27f, .18f)); var pale = Tint(art.bark, new Color(.6f, .56f, .48f));
            var sap = Glowing(new Color(.3f, 1, .5f), 1.6f); var fungus = Glowing(new Color(.3f, .95f, .8f), .9f); var frost = Glowing(new Color(.85f, .9f, 1), .35f);
            var loam = Tint(art.soil, new Color(.17f, .13f, .09f));
            // Sap veins: a few short glowing segments on the walls every six metres or so, and a dim green light at every third.
            for (float s0 = 2.5f; s0 < h.Length - 2; s0 += 5.5f + D() * 2)
            {
                int i = RingAt(s0); int side = D() < .5f ? 1 : -1; int k = side > 0 ? 2 + (int)(D() * 4) : P - 3 - (int)(D() * 4);
                var at = ring[i, k]; var inward = c[i] + Vector3.up * (at.y - c[i].y) - at; inward.y = 0; inward.Normalize();
                for (int v = 0; v < 3 + (int)(D() * 3); v++)
                {
                    var from = at + inward * .08f + new Vector3((D() - .5f) * .8f, (D() - .5f) * 1.2f, (D() - .5f) * .8f);
                    var dir = (new Vector3(D() - .5f, .6f + D() * .6f, D() - .5f)).normalized * (.5f + D() * .7f);
                    Part(PrimitiveType.Capsule, t, from + dir / 2, new Vector3(.05f, dir.magnitude / 2, .05f), sap, Quaternion.FromToRotation(Vector3.up, dir));
                }
                if ((int)(s0 / 5.5f) % 3 == 0) Glow(t, c[i] + Vector3.up * 1.6f + inward * -.5f, 7, .55f, new Color(.4f, 1, .55f), .55f);
                // Fungus near the vein, and a drip of sap on the floor under it.
                for (int f = 0; f < 4; f++) Part(PrimitiveType.Sphere, t, at + inward * .06f + new Vector3((D() - .5f) * .7f, (D() - .5f) * .5f - .6f, (D() - .5f) * .7f), new Vector3(.14f + D() * .1f, .05f, .11f + D() * .08f), fungus, Quaternion.LookRotation(inward) * Quaternion.Euler(90, 0, 0));
                Part(PrimitiveType.Cylinder, t, On(s0, side * h.Half[i] * .6f) + Vector3.up * .012f, new Vector3(.6f + D() * .5f, .01f, .5f + D() * .4f), sap, Along(s0) * Quaternion.Euler(0, D() * 90, 0));
            }
            // Roots through the walls and roof: knotted root-ends poking in everywhere, thicker in the Gallery.
            for (float s0 = 1; s0 < h.Length - 2; s0 += 1.6f)
            {
                int i = RingAt(s0); if (D() < .35f) continue;
                int k = 1 + (int)(D() * (P - 2)); var at = ring[i, k]; var inward = c[i] + Vector3.up * (at.y - c[i].y) - at; inward.y = 0; inward.Normalize();
                var dir = (inward * .6f + new Vector3((D() - .5f) * .6f, -.3f - D() * .6f, (D() - .5f) * .6f)).normalized; float len = .6f + D() * 1.2f;
                Limb(t, at - inward * .2f, at + dir * len, .07f + D() * .08f, .02f, bark, .15f, 6);
            }
            // The chambers: the Gallery (the widest ring in the first third), the Sap Well (the middle third) and the Heart (the last).
            int widest(float from, float to) { int best = -1; for (int i = 0; i < n; i++) if (h.Along[i] >= from && h.Along[i] <= to && (best < 0 || h.Half[i] > h.Half[best])) best = i; return best; }
            int gallery = widest(h.Length * .15f, h.Length * .4f), well = widest(h.Length * .45f, h.Length * .7f), heart = widest(h.Length - 26, h.Length - 4);
            if (gallery >= 0)
            {
                // Root columns from floor to roof, a little off plumb, with knobs, two rows down the hall; solid.
                float sg = h.Along[gallery];
                for (int k = 0; k < 6; k++)
                {
                    float s1 = sg - 6 + k * 2.4f + (D() - .5f); int i = RingAt(s1); int side = k % 2 == 0 ? 1 : -1;
                    var foot = On(s1, side * h.Half[i] * .45f); float tall = h.Height[i] * 1.15f;
                    var lean = new Vector3((D() - .5f) * .3f, 1, (D() - .5f) * .3f).normalized;
                    var col = Part(PrimitiveType.Cylinder, t, foot + lean * tall / 2, new Vector3(.5f + D() * .3f, tall / 2, .5f + D() * .3f), bark, Quaternion.FromToRotation(Vector3.up, lean));
                    for (int b = 0; b < 3; b++) Part(PrimitiveType.Sphere, t, foot + lean * (tall * (.2f + D() * .6f)) + new Vector3((D() - .5f) * .5f, 0, (D() - .5f) * .5f), Vector3.one * (.5f + D() * .4f), bark);
                    Block(foot, new Vector3(1.1f, tall, 1.1f), Quaternion.identity);
                }
            }
            if (well >= 0)
            {
                // The Sap Well: a pool of glowing sap in a rim of pale root against the east wall (the way through stays open along the
                // west), drips from the roof, a Keeper's votive stones round it.
                float sw = h.Along[well]; float pr = Mathf.Min(2.4f, h.Half[well] * .36f); var pool = On(sw, h.Half[well] - pr - .7f);
                Part(PrimitiveType.Cylinder, t, pool + Vector3.up * .06f, new Vector3(pr * 2 + .8f, .12f, pr * 2 + .8f), pale);
                Part(PrimitiveType.Cylinder, t, pool + Vector3.up * .13f, new Vector3(pr * 2, .03f, pr * 2), sap);
                Glow(t, pool + Vector3.up * 1.4f, 11, 1.1f, new Color(.4f, 1, .55f), 1.2f);
                Block(pool, new Vector3(pr * 2 + .6f, .3f, pr * 2 + .6f), Quaternion.identity);
                for (int k = 0; k < 7; k++) { float a = k * 51 * Mathf.Deg2Rad + D(); var st = pool + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (pr + .8f + D() * .4f); Part(PrimitiveType.Cube, t, st + Vector3.up * .25f, new Vector3(.35f, .5f + D() * .4f, .3f), pale, Quaternion.Euler(0, D() * 360, 0)); }
                for (int k = 0; k < 5; k++)
                {
                    var top = pool + new Vector3((D() - .5f) * pr * 1.6f, h.Height[well] * .92f, (D() - .5f) * pr * 1.6f);
                    Part(PrimitiveType.Capsule, t, top - Vector3.up * .5f, new Vector3(.06f, .5f, .06f), sap);
                }
            }
            if (heart >= 0)
            {
                // The Root-Mother: a vast knot of root grown out of the back wall, a hollow where a face would be; roots off it into the
                // floor and roof. Before her, lodged in the root, the cold: a black many-faced rod and hoarfrost spreading from it.
                float sEnd = h.Length - 3.5f; var seat = On(sEnd, 0); var toDoor = On(h.Length - 20, 0) - seat; toDoor.y = 0; var face = Quaternion.LookRotation(toDoor.normalized);
                var knot = seat + face * new Vector3(0, 0, -1.5f);
                Part(PrimitiveType.Sphere, t, knot + Vector3.up * 2.6f, new Vector3(7, 5.2f, 4), bark, face);
                Part(PrimitiveType.Sphere, t, knot + Vector3.up * 5.4f, new Vector3(4.2f, 3.4f, 3), bark, face);
                Part(PrimitiveType.Sphere, t, knot + face * new Vector3(0, 5.3f, 1.4f), new Vector3(1.6f, 2.2f, 1), Tint(art.soil, new Color(.05f, .04f, .03f)), face);   // the hollow of the face
                foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Sphere, t, knot + face * new Vector3(side * .55f, 5.6f, 1.95f), new Vector3(.22f, .14f, .1f), sap, face);   // and the sap-light in it
                Glow(t, knot + face * new Vector3(0, 5.2f, 2.6f), 13, 1.3f, new Color(.4f, 1, .55f), 1.4f);   // her face lit from its own hollow
                Glow(t, On(h.Length - 11, 0) + Vector3.up * 2.4f, 15, .5f, new Color(.35f, .9f, .5f), .6f);   // and a little of it reaching across the Heart
                for (int k = 0; k < 14; k++)
                {
                    float a = (k * 25.7f + D() * 20) * Mathf.Deg2Rad, r = 2.6f + D() * 2.4f; var from = knot + Vector3.up * (1 + D() * 4) + face * new Vector3(Mathf.Sin(a) * 3, 0, Mathf.Cos(a) * 1.5f);
                    var to = from + face * new Vector3(Mathf.Sin(a) * r, k % 3 == 0 ? 3 + D() * 3 : seat.y + .1f - from.y, Mathf.Cos(a) * r * .8f + 2);
                    float thick = .3f + D() * .25f;
                    if (Mathf.Abs(Mathf.Sin(a)) < .45f && Mathf.Cos(a) > 0) continue;   // none down the front of her face (the draws are taken all the same)
                    Limb(t, from, to, thick, .05f, bark, .12f, 7);
                }
                Block(knot, new Vector3(6, 7, 3.5f), face);
                var cold = seat + face * new Vector3(1.6f, 0, 2.4f); cold.y = FloorY(cold.x, cold.z);
                var husk = new GameObject("The cold in the root").transform; husk.SetParent(t, false); husk.localPosition = cold; husk.localRotation = face;
                // The cold itself (rod, light, hoarfrost) is the usable part: salted once, it is gone for good; the pale root stays.
                var rod = new GameObject("The cold").transform; rod.SetParent(husk, false);
                var black = Tint(art.metal, new Color(.04f, .04f, .06f));
                Part(PrimitiveType.Cylinder, rod, new Vector3(0, .9f, 0), new Vector3(.22f, .8f, .22f), black, Quaternion.Euler(12, 30, 8));
                Part(PrimitiveType.Cylinder, rod, new Vector3(.02f, .9f, .02f), new Vector3(.2f, .7f, .2f), black, Quaternion.Euler(-10, 75, -6));
                Part(PrimitiveType.Sphere, rod, new Vector3(0, 1.75f, 0), new Vector3(.3f, .3f, .3f), Glowing(new Color(.45f, .2f, .7f), 1.2f));
                for (int k = 0; k < 9; k++) Part(PrimitiveType.Sphere, rod, new Vector3((D() - .5f) * 3.2f, .03f, (D() - .5f) * 3.2f), new Vector3(.6f + D() * .9f, .05f, .5f + D() * .8f), frost);   // the hoarfrost
                Part(PrimitiveType.Sphere, husk, new Vector3(0, .25f, 0), new Vector3(1.6f, .5f, 1.6f), pale);   // the root it is lodged in, gone pale
                Glow(rod, new Vector3(0, 1.8f, 0), 6, .5f, new Color(.6f, .35f, .9f), .9f);
                Interactables.Add(new ZoneInteractable { name = husk.name, prompt = "Salt the cold root", kind = "crates", once = true, position = husk.position, root = rod });
                // Bones of what came down before, and the Pilgrim's abandoned pack by the way in.
                for (int k = 0; k < 3; k++)
                {
                    var at = On(h.Along[heart] + (D() - .5f) * 8, (D() - .5f) * h.Half[heart]);
                    for (int b = 0; b < 2; b++) Part(PrimitiveType.Cylinder, t, at + new Vector3(b * .2f, .05f, 0), new Vector3(.07f, .3f, .07f), Bone, Quaternion.Euler(0, b * 70 + k * 40, 90));
                }
            }
            // Puddles of seep-water, dark, here and there (stone, not metal: nothing to reflect down here).
            var puddle = Tint(art.stone, new Color(.12f, .11f, .1f));
            for (float s0 = 4; s0 < h.Length - 3; s0 += 4.5f) if (D() < .4f) Part(PrimitiveType.Cylinder, t, On(s0, (D() - .5f) * h.Half[RingAt(s0)]) + Vector3.up * .012f, new Vector3(.9f + D() * .8f, .01f, .6f + D() * .6f), puddle, Along(s0) * Quaternion.Euler(0, D() * 90, 0));
        }
        /// <summary>
        /// A cave's painted lining: what shows from inside, laid over the faceted shell (which stays the collider). The shell's own
        /// rings, with one more vertex between each two round the arch (curved through its neighbours and sunk a little into the
        /// rock, so the wall is scalloped where the shell is flat), smooth normals, and its shading painted into the vertices: dark
        /// at the wall's foot and in the hollows, lighter on what stands proud, in warm and cool patches. Straight at the mouth,
        /// where it meets the knoll's cut face. In stretches of ten rings, so each takes the lights near it; it casts shadow both
        /// ways, as the shell did (no sun reaches the floor). No colliders; draws from no stream.
        /// </summary>
        void CaveLining(Transform t, Hollow h, Vector3[] c, Vector3[,] ring, int n, int P, float seed, Material m)
        {
            int W = 2 * P - 1; var g = new Vector3[n, W]; var nrm = new Vector3[n, W]; var col = new Color[n, W];
            for (int i = 0; i < n; i++)
            {
                float s = h.Along[i], inside = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, 2.6f, s));
                for (int k = 0; k < W; k++)
                {
                    int a = k / 2; if (k % 2 == 0) { g[i, k] = ring[i, a]; continue; }
                    var p = (ring[i, a] + ring[i, a + 1]) / 2;
                    if (a >= 1 && a + 1 <= P - 2)   // not the two feet under the floor
                    {
                        var curved = (9 * (ring[i, a] + ring[i, a + 1]) - ring[i, Mathf.Max(1, a - 1)] - ring[i, Mathf.Min(P - 2, a + 2)]) / 16;
                        var into = (p - (c[i] + Vector3.up * h.Height[i] * .35f)).normalized;
                        p = Vector3.Lerp(p, curved, inside) + into * (.03f + .2f * Mathf.PerlinNoise(s * .5f + seed + 7, p.y * 1.3f + k * .37f)) * inside;
                    }
                    g[i, k] = p;
                }
            }
            for (int i = 0; i < n; i++)
                for (int k = 0; k < W; k++)
                {
                    int i0 = Mathf.Max(0, i - 1), i1 = Mathf.Min(n - 1, i + 1), k0 = Mathf.Max(0, k - 1), k1 = Mathf.Min(W - 1, k + 1);
                    var p = g[i, k]; var face = Vector3.Cross(g[i1, k] - g[i0, k], g[i, k1] - g[i, k0]).normalized;
                    if (Vector3.Dot(face, c[i] + Vector3.up * h.Height[i] * .35f - p) < 0) face = -face;   // toward the air
                    nrm[i, k] = face;
                    float hollow = Vector3.Dot((g[i0, k] + g[i1, k] + g[i, k0] + g[i, k1]) / 4 - p, face);   // how far its neighbours stand proud of it
                    float shade = Mathf.Lerp(.56f, 1, Mathf.SmoothStep(0, 1, (p.y - c[i].y) / Mathf.Max(.5f, h.Height[i]) / .4f)) * Mathf.Clamp(.86f - hollow * 2.4f, .55f, 1);
                    float patch = Mathf.PerlinNoise(p.x * .19f + seed, p.z * .19f + p.y * .31f) - .5f;
                    col[i, k] = new Color(Mathf.Min(1, shade * (1 + patch * .14f)), Mathf.Min(1, shade * (1 + patch * .03f)), Mathf.Min(1, shade * (1 - patch * .13f)), 1);
                }
            bool turned = Vector3.Dot(Vector3.Cross(g[n / 2 + 1, W / 2] - g[n / 2, W / 2], g[n / 2, W / 2 + 1] - g[n / 2, W / 2]), nrm[n / 2, W / 2]) < 0;   // wound to face the air, whichever way the rings run
            for (int from = 0; from + 1 < n; from += 10)
            {
                int rows = Mathf.Min(n - 1, from + 10) - from + 1;
                var v = new List<Vector3>(rows * W); var vn = new List<Vector3>(rows * W); var vc = new List<Color>(rows * W); var tri = new List<int>((rows - 1) * (W - 1) * 6);
                for (int i = from; i < from + rows; i++) for (int k = 0; k < W; k++) { v.Add(g[i, k]); vn.Add(nrm[i, k]); vc.Add(col[i, k]); }
                for (int i = 0; i + 1 < rows; i++)
                    for (int k = 0; k + 1 < W; k++)
                    {
                        int a = i * W + k, b = a + W, d = a + 1, e = b + 1;
                        tri.AddRange(turned ? new[] { a, d, b, d, e, b } : new[] { a, b, d, d, b, e });
                    }
                var mesh = new Mesh { name = h.Name + " lining" }; mesh.SetVertices(v); mesh.SetNormals(vn); mesh.SetColors(vc); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
                MeshPart(mesh, t, Vector3.zero, m).GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            }
        }
        Mesh[] drips;
        /// <summary>Dripstone, its foot at the origin, a unit tall and about a unit in radius at the foot (a stalagmite as it
        /// stands, a stalactite turned over): a lumpy taper to a point, a little off plumb, one of three. Open at the foot, which
        /// sits in the rock.</summary>
        Mesh Drip(int i)
        {
            if (drips == null)
            {
                drips = new Mesh[3];
                for (int k = 0; k < 3; k++)
                {
                    float o = k * 1.7f;
                    drips[k] = ZoneMeshes.Tube(y => new Vector3(Mathf.Sin(y * 2.2f + o) * .07f * y, y, Mathf.Cos(y * 1.7f + o * 2) * .07f * y),
                        (y, a) => (Mathf.Pow(Mathf.Max(0, 1 - y), 1.25f) * (.8f + .2f * Mathf.Sin(a * 2 + o + y * 3)) + .35f * Mathf.Exp(-Mathf.Max(0, y) * 9)) * (1 + .1f * Mathf.Sin(y * 17 + o)) * Mathf.Clamp01((1 - y) / .04f),
                        new[] { 0, .05f, .14f, .28f, .44f, .6f, .76f, .9f, 1 }, 8, Vector3.right, 1, 1);
                    drips[k].name = "Dripstone";
                }
            }
            return drips[((i % 3) + 3) % 3];
        }
        /// <summary>
        /// What breaks a cave's walls up, for the eye only (no colliders, nothing in the navmesh, kept to the walls and the roof).
        /// In rock: fallen stone heaped at the wall's foot, ledges standing proud between knee and shoulder, buttresses up the
        /// wall (none where a torch stands), and dripstone hanging in clusters from the higher roofs, clear of a head. In the
        /// Root-Mother's Deep: roots as thick as an arm arched over the passage from floor to floor, runners along the walls and
        /// rootlets hanging from the roof (joined into one mesh a stretch), earth slumped at the wall's foot, and a few beads of
        /// amber sap, each with a small warm light of its own against the green. Its own stream (TreeRandom a little off the
        /// mouth): the zone's, the cave's and the deep's are not touched.
        /// </summary>
        void CaveDressing(Transform t, Hollow h, Vector3[] c, Vector3[] right, Vector3[,] ring, int n, int P, bool roots, Material wall)
        {
            var cr = TreeRandom(t.position + new Vector3(7.5f, 0, 3.25f)); float C() { return (float)cr.NextDouble(); }
            int RingAt(float s) { int i = 0; while (i + 1 < n && h.Along[i + 1] <= s) i++; return i; }
            Vector3 Inward(int i, int k) { var d = c[i] - ring[i, k]; d.y = 0; return d.sqrMagnitude > .01f ? d.normalized : Vector3.zero; }
            Vector3 ToAir(int i, int k) { return (c[i] + Vector3.up * h.Height[i] * .4f - ring[i, k]).normalized; }
            float Yaw(int i) { return Mathf.Atan2(-right[i].z, right[i].x) * Mathf.Rad2Deg; }   // the way the passage runs
            if (!roots)
            {
                for (float s = 2.4f; s < h.Length - 3; s += 1.7f)
                {
                    int i = RingAt(s), side = C() < .5f ? 1 : -1, foot = side > 0 ? 1 : P - 2; float pick = C(), size = C(), turn = C(), lift = C();
                    bool torch = Mathf.Abs(Mathf.Repeat(s - 3.5f + 3.3f, 6.6f) - 3.3f) < 1.3f;   // a torch stands in the wall about here (Cavern sets one every 6.6 m)
                    if (pick < .4f)
                    {   // fallen stone at the wall's foot, a big one and a small one beside it
                        var at = ring[i, foot] - Inward(i, foot) * .1f; at.y = c[i].y + .1f;
                        Lump(BoulderAt((int)(turn * 6)), t, at, new Vector3(1 + size * .9f, .6f + size * .7f, .9f + lift * .7f), wall, turn * 360);
                        Lump(BoulderAt((int)(lift * 6)), t, at + Inward(i, foot) * .3f + new Vector3(-right[i].z, 0, right[i].x) * (.75f + size * .35f), Vector3.one * (.35f + lift * .3f), wall, lift * 360);
                    }
                    else if (pick < .76f && !torch)
                    {   // a ledge: a bed of the rock standing proud of the wall, long the way the passage runs
                        int k = side > 0 ? 2 + (int)(lift * 2) : P - 3 - (int)(lift * 2);
                        Lump(CragRock((int)(turn * 6)), t, ring[i, k] - Inward(i, k) * .3f, new Vector3(1.3f + size * .4f, .5f + lift * .4f, 1.8f + size * 1.6f), wall, Yaw(i) + (turn - .5f) * 16);
                    }
                    else if (!torch && h.Height[i] > 3)
                    {   // a buttress up the wall, lost in it higher up where the wall leans in
                        var at = ring[i, foot] - Inward(i, foot) * .4f; at.y = c[i].y;
                        Lump(CragRock((int)(turn * 6)), t, at, new Vector3(1.1f + size * .6f, h.Height[i] * (1 + lift * .5f), 1.2f + size * .8f), wall, Yaw(i) + (turn - .5f) * 30);
                    }
                }
                for (float s = 5; s < h.Length - 3; s += 2.3f)
                {
                    int i = RingAt(s), k = P / 2 + (int)((C() - .5f) * 5), count = 2 + (int)(C() * 3); float go = C();
                    if (h.Height[i] < 3.7f || go < .4f) continue;   // a low roof stays bare
                    for (int d = 0; d < count; d++)
                    {
                        float len = .45f + C() * 1.2f, r = .12f + C() * .12f, yaw = C() * 360; var at = ring[i, k] + new Vector3((C() - .5f) * .8f, .25f, (C() - .5f) * .8f);
                        len = Mathf.Min(len, at.y - c[i].y - 2.5f); if (len < .35f) continue;
                        MeshPart(Drip(i + d), t, at, wall, Quaternion.Euler(180, yaw, 0)).transform.localScale = new Vector3(r + len * .08f, len, r + len * .08f);
                    }
                }
                return;
            }
            // The deep. Roots are lofted as a limb is (ZoneBuilder.Limb) and joined, one mesh of thick root and one of rootlets to
            // every nine metres of passage.
            int stretches = (int)(h.Length / 9) + 1; var thick = new List<CombineInstance>[stretches]; var thin = new List<CombineInstance>[stretches];
            for (int b = 0; b < stretches; b++) { thick[b] = new List<CombineInstance>(); thin[b] = new List<CombineInstance>(); }
            void Strand(List<CombineInstance> into, Vector3 from, Vector3 to, float r0, float tip, float bow, int sides)
            {
                var d = to - from; float len = d.magnitude; if (len < .05f) return; var dir = d / len;
                var side = Vector3.Cross(dir, Vector3.up); if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
                var sag = Vector3.Cross(side.normalized, dir) * len * bow; if (sag.y > 0) sag = -sag;   // bowed down and out from the wall
                Vector3 Mid(float u) { return from + d * u + sag * (4 * u * (1 - u)); }
                float R(float u, float a) { return Mathf.Lerp(r0, tip, u) * (1 + .35f * Mathf.Exp(-u * 9)) * Mathf.Clamp01((1.05f - u) / .1f); }
                into.Add(Piece(ZoneMeshes.Tube(Mid, R, new[] { 0, .12f, .26f, .42f, .58f, .74f, .88f, .97f, 1.05f }, sides, side.normalized, 1, len * .5f)));
            }
            Vector3 OnWall(int i, int k, float r) { return ring[i, k] + ToAir(i, k) * (.04f + r * .5f); }   // half sunk in the earth
            // Arched over the passage from the foot of one wall to the foot of the other, wandering a ring or two as they climb.
            for (float s = 3; s < h.Length - 6; s += 5 + C() * 3.5f)
            {
                int i = RingAt(s), k = 1; float r = .13f + C() * .12f; var from = OnWall(i, 1, r) + Vector3.down * .3f;
                while (k < P - 2)
                {
                    int k2 = Mathf.Min(P - 2, k + 2), j = Mathf.Clamp(i + (int)((C() - .5f) * 4), 1, n - 2);
                    var to = OnWall(j, k2, r); if (k2 == P - 2) to += Vector3.down * .3f;
                    Strand(thick[(int)(s / 9)], from, to, r * (.85f + C() * .3f), r * .8f, .07f, 7);
                    from = to; k = k2;
                }
            }
            // Runners along the walls, two to four lengths each, thinning as they go.
            for (float s = 1.5f; s < h.Length - 5; s += 1.6f + C() * 1.6f)
            {
                int i = RingAt(s), side = C() < .5f ? 1 : -1, k = side > 0 ? 2 + (int)(C() * 5) : P - 3 - (int)(C() * 5), runs = 2 + (int)(C() * 3); float r = .06f + C() * .1f;
                var from = OnWall(i, k, r) - ToAir(i, k) * .15f;   // out of the earth
                for (int q = 0; q < runs; q++)
                {
                    i += 2 + (int)(C() * 2); k = Mathf.Clamp(k + (int)(C() * 3) - 1, 2, P - 3); if (i > n - 3) break;
                    var to = OnWall(i, k, r); Strand(thick[(int)(s / 9)], from, to, r, r * .75f, .05f, 6);
                    from = to; r *= .8f;
                }
            }
            // Rootlets hanging from the roof in bunches, none lower than a head.
            for (float s = 2; s < h.Length - 4; s += 1.3f + C() * 1.5f)
            {
                int i = RingAt(s), k = P / 2 + (int)((C() - .5f) * 5), count = 3 + (int)(C() * 4);
                for (int q = 0; q < count; q++)
                {
                    var top = ring[i, k] + new Vector3((C() - .5f) * .6f, .2f, (C() - .5f) * .6f); var sway = new Vector3((C() - .5f) * .3f, 0, (C() - .5f) * .3f);
                    float len = Mathf.Min(.35f + C() * 1.3f, top.y - c[i].y - 2.2f), r = .035f + C() * .03f;
                    if (len >= .3f) Strand(thin[(int)(s / 9)], top, top + Vector3.down * len + sway, r, .012f, .06f, 5);
                }
            }
            var bark = Tint(art.bark, new Color(.36f, .29f, .2f)); var pale = Tint(art.bark, new Color(.5f, .43f, .32f));
            for (int b = 0; b < stretches; b++)
            {
                if (thick[b].Count > 0) Stonework("Roots", t, bark, thick[b].ToArray());
                if (thin[b].Count > 0) Stonework("Rootlets", t, pale, thin[b].ToArray());
            }
            // Earth slumped at the wall's foot (not into the Sap Well's pool, which lies against the right wall).
            int well = -1; for (int i = 0; i < n; i++) if (h.Along[i] >= h.Length * .45f && h.Along[i] <= h.Length * .7f && (well < 0 || h.Half[i] > h.Half[well])) well = i;
            for (float s = 2.5f; s < h.Length - 4; s += 2.2f + C() * 2)
            {
                int i = RingAt(s), side = C() < .5f ? 1 : -1, foot = side > 0 ? 1 : P - 2; float size = C(), turn = C();
                if (well >= 0 && side > 0 && Mathf.Abs(s - h.Along[well]) < 3.4f) continue;
                var at = ring[i, foot] - Inward(i, foot) * .15f; at.y = c[i].y + .08f;
                Lump(BoulderAt((int)(turn * 6)), t, at, new Vector3(1 + size * .9f, .6f + size * .7f, 1.1f + turn * 1.1f), wall, turn * 360);
            }
            // Amber sap welling from the wall every fifteen metres, on alternate sides: a few beads and a small warm light.
            var amber = Glowing(new Color(1, .62f, .22f), 1.5f);
            for (float s = 9; s < h.Length - 12; s += 15)
            {
                int i = RingAt(s), k = (int)(s / 15) % 2 == 0 ? P - 4 : 3; var at = ring[i, k] + ToAir(i, k) * .05f;
                for (int q = 0; q < 4; q++) Part(PrimitiveType.Sphere, t, at + new Vector3((C() - .5f) * .5f, (C() - .5f) * .7f, (C() - .5f) * .5f), Vector3.one * (.07f + C() * .09f), amber);
                Glow(t, at + ToAir(i, k) * .6f, 6.5f, .55f, new Color(1, .66f, .3f), .55f);
            }
        }
        /// <summary>A pitch torch in a wall: a short stick leaning out of the rock, its glowing head and flame, a flickering light.</summary>
        void Torch(Transform t, Vector3 wallAt, Vector3 inward)
        {
            var wood = Tint(art.timber, new Color(.26f, .18f, .1f));
            var lean = (Vector3.up + inward * .7f).normalized;
            Part(PrimitiveType.Cylinder, t, wallAt + lean * .28f, new Vector3(.07f, .3f, .07f), wood, Quaternion.FromToRotation(Vector3.up, lean));
            var tip = wallAt + lean * .6f;
            Part(PrimitiveType.Sphere, t, tip, Vector3.one * .14f, art.glass);
            if (cone == null) cone = ZoneMeshes.Cone(1, 1);
            MeshPart(cone, t, tip + Vector3.up * .04f, Glowing(new Color(1, .55f, .2f), 2.2f)).transform.localScale = new Vector3(.13f, .34f, .13f);
            Glow(t, tip + inward * .3f + Vector3.up * .2f, 7.5f, 1.35f, new Color(1, .6f, .3f), 1.35f).gameObject.AddComponent<Flicker>();
        }
        /// <summary>A campfire: a ring of stones, crossed logs on a bed of embers, two flames, a flickering light.</summary>
        void Campfire(Transform t, Vector3 at)
        {
            var stone = Tint(art.stone, new Color(.34f, .32f, .3f)); var wood = Tint(art.timber, new Color(.22f, .15f, .09f));
            for (int i = 0; i < 9; i++) { var q = Quaternion.Euler(0, i * 40, 0); Lump(BoulderAt(i), t, at + q * new Vector3(0, .08f, .72f), new Vector3(.34f, .24f, .3f), stone, i * 47); }
            for (int i = 0; i < 3; i++) Part(PrimitiveType.Cylinder, t, at + new Vector3(0, .14f, 0), new Vector3(.1f, .5f, .1f), wood, Quaternion.Euler(0, i * 60, 70));
            Part(PrimitiveType.Cylinder, t, at + new Vector3(0, .05f, 0), new Vector3(.9f, .03f, .9f), art.glass);
            if (cone == null) cone = ZoneMeshes.Cone(1, 1);
            MeshPart(cone, t, at + new Vector3(0, .1f, 0), Glowing(new Color(1, .5f, .15f), 2.6f)).transform.localScale = new Vector3(.4f, .9f, .4f);
            MeshPart(cone, t, at + new Vector3(.12f, .1f, -.08f), Glowing(new Color(1, .75f, .3f), 2.2f)).transform.localScale = new Vector3(.22f, .6f, .22f);
            Glow(t, at + new Vector3(0, 1.1f, 0), 11, 1.9f, new Color(1, .55f, .25f), 1.9f).gameObject.AddComponent<Flicker>();
        }
        /// <summary>A torn company banner on a pole: two strips of sand-coloured cloth, one hanging shorter where it ripped, a faded red stripe.</summary>
        void Banner(Transform t, Vector3 at, float yaw, Material cloth, Material wood, float height)
        {
            var q = Quaternion.Euler(0, yaw, 0);
            Part(PrimitiveType.Cylinder, t, at + new Vector3(0, height / 2, 0), new Vector3(.08f, height / 2, .08f), wood);
            Part(PrimitiveType.Cylinder, t, at + q * new Vector3(.5f, height - .1f, 0), new Vector3(.05f, .55f, .05f), wood, q * Quaternion.Euler(0, 0, 90));
            Part(PrimitiveType.Cube, t, at + q * new Vector3(.28f, height - .75f, 0), new Vector3(.5f, 1.25f, .03f), cloth, q);
            Part(PrimitiveType.Cube, t, at + q * new Vector3(.78f, height - .55f, 0), new Vector3(.46f, .85f, .03f), cloth, q * Quaternion.Euler(0, 0, 3));
            Part(PrimitiveType.Cube, t, at + q * new Vector3(.5f, height - .42f, -.01f), new Vector3(.9f, .08f, .03f), Tint(art.cloth, new Color(.72f, .1f, .06f)), q);
        }
        /// <summary>The Bandit King's throne: a stone dais, a plank seat with a high back and arms, a cloak thrown over it, crossed bones over the top.</summary>
        void Throne(Transform t, Vector3 at, Quaternion face, Material cloth, Material wood)
        {
            var stone = Tint(art.stone, new Color(.36f, .34f, .31f));
            for (int k = 0; k < 3; k++) Lump(BoulderAt(k + 2), t, at + face * new Vector3((k - 1) * 1.1f, -.05f, -.3f + (k % 2) * .5f), new Vector3(1.8f, .45f, 1.6f), stone, k * 60 + face.eulerAngles.y);
            var seat = at + Vector3.up * .3f;
            Part(PrimitiveType.Cube, t, seat + face * new Vector3(0, .45f, 0), new Vector3(1, .12f, .85f), wood, face);
            Part(PrimitiveType.Cube, t, seat + face * new Vector3(0, 1.25f, -.42f), new Vector3(1.05f, 1.7f, .12f), wood, face);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, seat + face * new Vector3(s * .5f, .72f, 0), new Vector3(.1f, .5f, .8f), wood, face);
            Part(PrimitiveType.Cube, t, seat + face * new Vector3(0, 1.4f, -.34f), new Vector3(.95f, 1.2f, .04f), cloth, face * Quaternion.Euler(4, 0, 0));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, t, seat + face * new Vector3(0, 2.2f, -.44f), new Vector3(.07f, .5f, .07f), Bone, face * Quaternion.Euler(0, 0, s * 38));
        }
        /// <summary>An Ash-Walker lean-to (GAME-ONLY look): two hides stretched from a bone ridge pole down to the ground behind, a bedroll and a basket of salt inside. Open front faces -Z.</summary>
        void Shelter(Transform t)
        {
            var hide = Tint(art.cloth, new Color(.56f, .43f, .29f)); var hide2 = Tint(art.cloth, new Color(.67f, .55f, .39f));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, t, new Vector3(s * 1.8f, 1.1f, -1.4f), new Vector3(.13f, 1.1f, .13f), Bone, Quaternion.Euler(0, 0, s * -4));
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 2.2f, -1.4f), new Vector3(.11f, 2.05f, .11f), Bone, Quaternion.Euler(0, 0, 90));
            Part(PrimitiveType.Cube, t, new Vector3(-.95f, 1.04f, .35f), new Vector3(2, .05f, 4.2f), hide, Quaternion.Euler(33, 0, 0));   // pegged into the ground behind
            Part(PrimitiveType.Cube, t, new Vector3(.95f, 1.02f, .37f), new Vector3(2, .05f, 4.2f), hide2, Quaternion.Euler(33, 0, 0));
            Part(PrimitiveType.Cube, t, new Vector3(-.6f, .08f, .3f), new Vector3(.9f, .14f, 1.8f), hide2);   // bedroll
            Part(PrimitiveType.Cylinder, t, new Vector3(1.1f, .22f, .5f), new Vector3(.6f, .22f, .6f), Tint(art.hay, new Color(.45f, .38f, .26f)));   // basket
            Part(PrimitiveType.Cube, t, new Vector3(1.1f, .5f, .5f), new Vector3(.35f, .2f, .3f), Tint(art.stone, new Color(.94f, .94f, .91f)));   // salt
            Solid(t, new Vector3(0, 1.1f, .2f), new Vector3(4, 2.2f, 3.8f));
        }
        /// <summary>A fire-bowl on three legs (iron, or bone) with glowing coals and a flame; burns day and night.</summary>
        void Brazier(Transform t, Vector3 at, Material legs)
        {
            var iron = Tint(art.metal, new Color(.18f, .17f, .17f));
            for (int i = 0; i < 3; i++) { var q = Quaternion.Euler(0, i * 120, 0); Part(PrimitiveType.Cube, t, at + q * new Vector3(0, .5f, .24f), new Vector3(.07f, 1.05f, .07f), legs ?? iron, q * Quaternion.Euler(-14, 0, 0)); }
            Part(PrimitiveType.Sphere, t, at + new Vector3(0, 1, 0), new Vector3(.8f, .3f, .8f), iron);
            Part(PrimitiveType.Sphere, t, at + new Vector3(0, 1.07f, 0), new Vector3(.66f, .18f, .66f), art.glass);   // coals heaped in the bowl, showing over its rim
            if (cone == null) cone = ZoneMeshes.Cone(1, 1);
            MeshPart(cone, t, at + new Vector3(0, 1.12f, 0), Glowing(new Color(1, .55f, .2f), 2.2f)).transform.localScale = new Vector3(.2f, .55f, .2f);
            Glow(t, at + new Vector3(0, 1.7f, 0), 7, 1.2f, new Color(1, .58f, .28f), 1.9f);
        }
        /// <summary>
        /// The Weave-Eater brood (GAME-ONLY; Weave-Eaters born of the frayed threads of the broken Weave are CANON, book1
        /// ch.20): a hollow of unmade ground (a sunk ZoneShape painted unmade) ringed with torn crust, with burrows,
        /// thread-wrapped sacs and frayed grey threads arching out of the ash or strung across it. Walkable, no colliders.
        /// </summary>
        void Brood(Transform t, float radius)
        {
            var thread = Tint(art.ash, new Color(.82f, .82f, .86f)); var crust = Tint(art.ash, new Color(.46f, .46f, .49f));
            var sac = Tint(art.ash, new Color(.66f, .64f, .7f)); var hole = Tint(art.stone, new Color(.03f, .03f, .035f));
            for (int k = 0; k < 16; k++) { var q = Quaternion.Euler(0, k * 22.5f + R01 * 10, 0); Part(PrimitiveType.Cube, t, q * new Vector3(0, .15f, radius * 1.05f), new Vector3(1.8f, .12f, 1.3f), crust, q * Quaternion.Euler(-30 - R01 * 25, 0, R01 * 16 - 8)); }   // the nest's torn rim
            for (int i = 0; i < 3; i++)   // burrows: dark mouths in the floor, lipped with torn crust
            {
                var at = Quaternion.Euler(0, i * 120 + R01 * 40, 0) * new Vector3(0, 0, radius * (.25f + R01 * .3f));
                Part(PrimitiveType.Sphere, t, at + new Vector3(0, -.1f, 0), new Vector3(1.7f + R01 * .6f, .26f, 1.3f + R01 * .5f), hole);
                for (int k = 0; k < 6; k++) { var q = Quaternion.Euler(0, k * 60 + R01 * 25, 0); Part(PrimitiveType.Cube, t, at + q * new Vector3(0, .06f, 1.05f), new Vector3(.8f, .08f, .55f), crust, q * Quaternion.Euler(-20 - R01 * 25, 0, 0)); }
            }
            for (int i = 0; i < 8; i++)   // sacs, half sunk, wrapped in thread
            {
                var at = Quaternion.Euler(0, R01 * 360, 0) * new Vector3(0, 0, radius * (.3f + R01 * .55f)); float s = .45f + R01 * .5f;
                Part(PrimitiveType.Sphere, t, at + new Vector3(0, s * .3f, 0), new Vector3(s, s * 1.35f, s), sac, Quaternion.Euler(R01 * 30 - 15, 0, R01 * 30 - 15));
                for (int k = 0; k < 2; k++) Part(PrimitiveType.Cylinder, t, at + new Vector3(0, s * (.2f + k * .3f), 0), new Vector3(s * 1.04f, .015f, s * 1.04f), thread, Quaternion.Euler(R01 * 40 - 20, 0, R01 * 40 - 20));
            }
            for (int i = 0; i < 11; i++)   // frayed threads arching out of the ash; their tips unravel
            {
                var at = Quaternion.Euler(0, R01 * 360, 0) * new Vector3(0, 0, radius * R01 * 1.1f);
                var tip = Arc(t, at + Vector3.down * .2f, R01 * 360, .8f + R01 * 2.4f, 1.2f + R01 * 2.4f, R01 * .8f, .14f, .05f, thread, 6);
                for (int k = 0; k < 3; k++) { var dir = Quaternion.Euler(R01 * 360, R01 * 360, 0) * Vector3.up; Part(PrimitiveType.Cube, t, tip + dir * .2f, new Vector3(.025f, .4f, .025f), thread, Quaternion.FromToRotation(Vector3.up, dir)); }
            }
            for (int i = 0; i < 5; i++)   // loose threads strung across the hollow
            {
                Vector3 a = Quaternion.Euler(0, R01 * 360, 0) * new Vector3(0, .3f + R01 * .9f, radius * (.6f + R01 * .5f)), b = Quaternion.Euler(0, R01 * 360, 0) * new Vector3(0, .2f + R01 * .7f, radius * (.6f + R01 * .5f));
                Part(PrimitiveType.Cylinder, t, (a + b) / 2, new Vector3(.04f, Vector3.Distance(a, b) / 2, .04f), thread, Quaternion.FromToRotation(Vector3.up, b - a));
            }
        }
        /// <summary>
        /// A rib of the old ribcage (a colossal petrified ribcage on the ash plain is a CANON image, book1 ch.20; this one is
        /// GAME-ONLY): bone turned to pale grey stone, rising out of the ash, bowing out, then curving in over reach metres
        /// (+Z) to meet the spine at height (see Spine).
        /// </summary>
        void Rib(Transform t, float reach, float height)
        {
            var bone = Tint(art.stone, new Color(.76f, .73f, .65f));
            Arc(t, new Vector3(0, -.4f, 0), 0, reach, height + .4f, reach * .28f, .8f, .42f, bone);
            Part(PrimitiveType.Sphere, t, new Vector3(0, .02f, -.25f), new Vector3(1.3f, .45f, 1.2f), Tint(art.ash, new Color(.4f, .39f, .38f)));   // ash heaped at its foot
            Solid(t, new Vector3(0, 1.3f, -.3f), new Vector3(1.1f, 2.6f, 1.4f));
        }
        /// <summary>The old ribcage's spine (see Rib): vertebrae along local X at the ribs' height, plunging into the ash at both ends.</summary>
        void Spine(Transform t, float length, float height)
        {
            var bone = Tint(art.stone, new Color(.76f, .73f, .65f));
            float half = length / 2, level = half - 5;   // level over the ribs, then five metres down into the ground (neck and tail)
            float Y(float u) { float e = Mathf.Clamp01((Mathf.Abs(u) - level) / 5); return height * (1 - e * e) - .8f * e; }
            Vector3? last = null; float run = 99;
            for (float x = -half; x <= half; x += .05f)
            {
                run += new Vector2(.05f, Y(x + .05f) - Y(x)).magnitude; if (run < 1.2f) continue; run = 0;   // a vertebra every 1.2 m along the curve
                float s = 1 - Mathf.Clamp01((Mathf.Abs(x) - level) / 5) * .35f;
                var r = Quaternion.Euler(0, 0, Mathf.Atan((Y(x + .05f) - Y(x - .05f)) / .1f) * Mathf.Rad2Deg); var at = new Vector3(x, Y(x) + .15f, 0);
                Part(PrimitiveType.Cylinder, t, at, new Vector3(1.1f * s, .28f, 1.1f * s), bone, r * Quaternion.Euler(0, 0, 90));
                Part(PrimitiveType.Cube, t, at + r * new Vector3(-.1f, .65f * s, 0), new Vector3(.22f, .9f * s, .45f), bone, r * Quaternion.Euler(0, 0, 16));   // dorsal spine
                Part(PrimitiveType.Cube, t, at, new Vector3(.2f, .2f, 1.9f * s), bone, r);   // side processes
                if (last.HasValue) { var seg = at - last.Value; Part(PrimitiveType.Capsule, t, (at + last.Value) / 2, new Vector3(.55f * s, (seg.magnitude + .55f) / 2, .55f * s), bone, Quaternion.FromToRotation(Vector3.up, seg)); }
                last = at;
            }
            foreach (int sx in new[] { -1, 1 }) Solid(t, new Vector3(sx * (half - .5f), .8f, 0), new Vector3(1.6f, 1.8f, 1.8f));   // where it goes into the ground
        }
        /// <summary>
        /// The Cult of Ash's shrine (bone masks wired with iron: CANON, book1 ch.10; the purple tear is the later House of
        /// Ash's symbol, used early: GAME-ONLY): a scorched dais, an altar-stone with candles and bones, a stele daubed with
        /// the purple tear on both faces and hung with wired bone masks, a brazier either side. Front faces -Z.
        /// </summary>
        void Shrine(Transform t)
        {
            var dark = Tint(art.stone, new Color(.25f, .24f, .24f)); var soot = Tint(art.stone, new Color(.15f, .14f, .14f)); var tear = Glowing(new Color(.5f, .16f, .62f), 1.4f);
            Part(PrimitiveType.Cube, t, new Vector3(0, .02f, 0), new Vector3(4.6f, .14f, 4.6f), soot, Quaternion.Euler(0, 45, 0));   // scorched dais
            BoxPart(t, new Vector3(0, .1f, .35f), new Vector3(3.2f, .16f, 2.6f), Dressed(new Color(.25f, .24f, .24f)), null, 1.5f);   // laid stone
            dark = RockTint(new Color(.25f, .24f, .24f));   // the altar and the stele are each one stone
            Part(PrimitiveType.Cube, t, new Vector3(0, .6f, 0), new Vector3(2, .9f, 1.1f), dark);   // the altar-stone
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.1f, 0), new Vector3(2.2f, .14f, 1.3f), soot);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.75f, 1.05f), new Vector3(1.3f, 3.3f, .45f), dark);   // the stele
            Part(PrimitiveType.Cube, t, new Vector3(0, 3.45f, 1.05f), new Vector3(1.5f, .2f, .6f), soot);
            if (cone == null) cone = ZoneMeshes.Cone(1, 1);
            foreach (int s in new[] { -1, 1 })
            {
                float z = 1.05f + s * .23f;   // the tear on both faces
                Part(PrimitiveType.Sphere, t, new Vector3(0, 2.05f, z), new Vector3(.62f, .62f, .06f), tear);
                MeshPart(cone, t, new Vector3(0, 2.05f, z), tear).transform.localScale = new Vector3(.3f, .8f, .03f);
                Mask(t, new Vector3(s * .62f, 2.75f, .78f), 0, .85f);
                Brazier(t, new Vector3(s * 2.1f, 0, -.4f), null);
                Solid(t, new Vector3(s * 2.1f, .6f, -.4f), new Vector3(.8f, 1.2f, .8f));
            }
            for (int i = 0; i < 3; i++) Part(PrimitiveType.Cylinder, t, new Vector3(-.7f + i * .7f, 1.3f, -.3f + (i % 2) * .2f), new Vector3(.08f, .12f, .08f), art.glass);   // candles
            for (int i = 0; i < 4; i++) Part(PrimitiveType.Cube, t, new Vector3(-.3f + R01 * .6f, 1.21f, .1f + R01 * .3f), new Vector3(.07f, .06f, .45f), Bone, Quaternion.Euler(0, R01 * 180, 0));   // bones
            Glow(t, new Vector3(0, 2.3f, .3f), 5, .8f, new Color(.62f, .3f, .85f), 1.4f);
            Solid(t, new Vector3(0, 1.7f, .5f), new Vector3(2.4f, 3.4f, 2.1f));
        }
        /// <summary>A Cult of Ash bone mask (CANON, book1 ch.10): jagged bone plates wired together with rusting iron wire, dark eye holes. Faces -Z.</summary>
        void Mask(Transform t, Vector3 at, float yaw, float s)
        {
            var hole = Tint(art.stone, new Color(.05f, .05f, .05f)); var wire = Tint(art.metal, new Color(.36f, .21f, .12f)); var r = Quaternion.Euler(0, yaw, 0);
            Part(PrimitiveType.Sphere, t, at, new Vector3(.5f, .66f, .22f) * s, Bone, r);
            for (int i = 0; i < 4; i++) Part(PrimitiveType.Cube, t, at + r * new Vector3(i % 2 == 0 ? -.2f : .2f, i < 2 ? .24f : -.22f, -.01f) * s, new Vector3(.2f, .22f, .06f) * s, Bone, r * Quaternion.Euler(0, 0, 25 + i * 40));   // jagged plates
            foreach (int sx in new[] { -1, 1 }) Part(PrimitiveType.Sphere, t, at + r * new Vector3(sx * .11f, .07f, -.1f) * s, new Vector3(.13f, .08f, .05f) * s, hole, r);
            foreach (float y in new[] { .17f, -.13f }) Part(PrimitiveType.Cube, t, at + r * new Vector3(0, y, -.1f) * s, new Vector3(.54f, .025f, .03f) * s, wire, r * Quaternion.Euler(0, 0, y * 25));
        }
        /// <summary>A cult idol: a stake hung with grey rags on a cross-arm and crowned with a wired bone mask (see Mask). Faces -Z.</summary>
        void Idol(Transform t)
        {
            var stake = Tint(art.timber, new Color(.2f, .16f, .13f)); var rag = Tint(art.cloth, new Color(.36f, .35f, .34f));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.2f, 0), new Vector3(.14f, 2.4f, .14f), stake);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.8f, 0), new Vector3(1.3f, .1f, .1f), stake, Quaternion.Euler(0, 0, R01 * 14 - 7));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.35f, -.02f), new Vector3(1.1f, .85f, .03f), rag, Quaternion.Euler(3, 0, R01 * 8 - 4));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * .5f, 1.2f, -.02f), new Vector3(.18f, .6f, .03f), rag, Quaternion.Euler(0, 0, s * 8));   // tatters
            Mask(t, new Vector3(0, 2.3f, -.1f), 0, 1.3f);
            Solid(t, new Vector3(0, 1, 0), new Vector3(.5f, 2, .5f));
        }

        // ---------- secrets: hidden finds on no map ----------
        /// <summary>
        /// The zone's hidden finds (<see cref="ZoneSecret"/>), each built as a small, subtle prop and registered in
        /// <see cref="Secrets"/> for the encounter's discovery system. Built after the map is rendered, so none shows on the minimap
        /// or the zone map; no colliders, so the navmesh is as it was; drawn from a stream of its own keyed on where it stands, so
        /// the zone's stream (every tree, bush and rock) is untouched. Its frame faces -Z the way you come to it (its rotation) with
        /// its hiding place behind (+Z):
        /// - cache: a dented tin, or an oilcloth bundle when its prompt or name says bundle, oilcloth, sack or pack (crusted with
        ///   salt when it says salt);
        /// - note: a folded page standing out of a crack, or lying under a stone;
        /// - herb: a small patch of pale, faintly glowing bells;
        /// - chest: an iron-banded chest with a padlock; key: a small brass key on a thong from a nail;
        /// - vista: nothing, a small cairn ("stones", "cairn"), a ring of fire-cracked beacon stones ("beacon") or a line of salt
        ///   across the way, its rotation turning it ("salt").
        /// A word in a searchable's prompt or name sets a scene round it: "camp" (a cold fire ring and a rolled bedroll in front),
        /// "log" or "trunk" (a fallen trunk behind it), "stump" (a hollow stump it is pushed into), "rocks" or "cleft" (two stones
        /// either side), "stone" (a flat stone half over it), "scrape" (a shallow scrape it lies half buried in).
        /// Things you take (a note, a key, a herb) are kept apart from their scene: <see cref="ZoneSecretSpot.root"/> is the thing
        /// alone, so only it vanishes when found (the nail, the crack and the stump stay). Metal catches the light in a small glint,
        /// pages are pale and the herbs glow faintly: enough to catch a curious eye, never a quest marker's glow.
        /// A herb, or a find in a scene of its own, that stands in a trunk or a rock is moved clear (up to 2.5 m; never onto a
        /// road, into water or a building); one tucked against a prop (a key on a gallows, a page in a wall) stays exactly put.
        /// </summary>
        void BuildSecrets()
        {
            if (Zone.secrets == null || Zone.secrets.Length == 0) return;
            var all = new GameObject("Zone secrets").transform; all.SetParent(transform, false);
            Physics.SyncTransforms();   // this frame's colliders, for moving a find out of a trunk or a rock
            foreach (var s in Zone.secrets)
            {
                if (s == null || string.IsNullOrEmpty(s.id)) continue;
                string words = ((s.prompt ?? "") + " " + (s.name ?? "") + (s.kind == "vista" ? " " + (s.text ?? "") : "")).ToLowerInvariant();
                bool Says(params string[] any) { foreach (var w in any) if (words.Contains(w)) return true; return false; }
                string scene = s.kind == "vista" ? (Says("beacon") ? "beacon" : Says("salt") ? "salt" : Says("stones", "cairn") ? "cairn" : null)
                    : Says("camp") ? "camp" : Says("log", "trunk") ? "log" : Says("stump") ? "stump" : Says("rocks", "cleft") ? "rocks" : Says("stone") ? "stone" : Says("scrape") ? "scrape" : null;
                var sr = new System.Random(Zone.seed ^ (Mathf.RoundToInt(s.at.x * 8) * 73856093) ^ (Mathf.RoundToInt(s.at.y * 8) * 19349663) ^ 0x51c2e7);
                float R() { return (float)sr.NextDouble(); }
                var at = s.at;
                if (s.kind == "herb" || (s.kind != "vista" && scene != null)) at = SecretClear(at, scene == "camp" ? 1.9f : scene == "log" ? 1.5f : scene == "rocks" ? .9f : .5f);
                var t = new GameObject("Secret: " + (string.IsNullOrEmpty(s.name) ? s.id : s.name)).transform; t.SetParent(all, false);
                t.position = StandAt(at, float.NegativeInfinity); t.rotation = Quaternion.Euler(0, s.rotation, 0);   // in a cave: on its floor
                // In a cave, a key on its nail or a page in a crack hangs on the rock itself: the nearest wall at its height, facing out.
                if ((s.kind == "key" || s.kind == "note") && CaveWall(t.position + Vector3.up * s.height, out var wallAt, out var outOf))
                { t.position = new Vector3(wallAt.x, t.position.y, wallAt.z) + outOf * .03f; t.rotation = Quaternion.LookRotation(-outOf); }
                var item = new GameObject(s.kind).transform; item.SetParent(t, false); item.localPosition = Vector3.up * s.height;
                switch (s.kind)
                {
                    case "vista":
                        if (scene == "beacon") SecretBeacon(t, R);
                        else if (scene == "salt") SecretSaltLine(t, R);
                        else if (scene == "cairn") SecretCairn(t, R, Zone.wasting != null && Unmade(at.x, at.y) > .2f ? RockTint(new Color(.6f, .6f, .62f)) : null);   // in the grey, its top stone greyed too
                        break;
                    case "cache":
                        if (Says("bundle", "oilcloth", "sack", "pack")) SecretBundle(item, R, Says("salt"));
                        else SecretTin(item, R, Says("salt"), scene == "scrape" ? .07f : .035f);
                        break;
                    case "note": SecretPage(item, R, scene == "stone"); if (scene == null) SecretCrack(t, s.height); break;
                    case "herb": SecretHerb(item, R); break;
                    case "chest": SecretChest(item, t, R); break;
                    case "key": SecretKey(item, t, s.height, R); break;
                    default: Debug.LogWarning(Zone.id + ": secret '" + s.id + "' has an unknown kind '" + s.kind + "'."); break;
                }
                if (s.kind != "vista")
                    switch (scene)
                    {
                        case "camp": SecretCamp(t, R); break;
                        case "log": SecretLog(t, R); break;
                        case "stump": SecretStump(t, R, s.height); break;
                        case "rocks": SecretRocks(t, R); break;
                        case "stone": SecretFlatStone(t, R); break;
                        case "scrape": SecretScrape(t, R); break;
                    }
                bool pocketed = s.kind == "note" || s.kind == "key" || s.kind == "herb";   // what you take vanishes once found; its scene stays
                Secrets.Add(new ZoneSecretSpot { def = s, position = s.kind == "vista" ? t.position : item.position, root = pocketed ? item : t });
            }
        }
        /// <summary>
        /// Where a find, and the scene round it (radius r), may stand at or near at: clear of every collider but the ground's (a trunk,
        /// a rock, a wall), off the roads and out of the water and buildings, up to 2.5 m away; at itself when nothing near is clear.
        /// Draws nothing random.
        /// </summary>
        Vector2 SecretClear(Vector2 at, float r)
        {
            var ground = GroundMesh != null ? GroundMesh.GetComponent<Collider>() : null;
            bool Clear(Vector2 q)
            {
                if (NearRoad(q, 1) || Water.NearWater(q, .5f) || InBuilding(q)) return false;
                foreach (var c in Physics.OverlapSphere(Ground(q, .6f), r, ~0, QueryTriggerInteraction.Ignore)) if (c != ground) return false;
                return true;
            }
            if (Clear(at)) return at;
            for (float d = .5f; d <= 2.51f; d += .5f)
                for (int k = 0; k < 8; k++) { var q = at + new Vector2(Mathf.Cos(k * Mathf.PI / 4), Mathf.Sin(k * Mathf.PI / 4)) * d; if (Clear(q)) return q; }
            return at;
        }
        /// <summary>
        /// The rock of a cave passage (Hollow) nearest a point in it, at the point's height: where a level ray from the passage's
        /// middle out through the point meets the shell, and the level direction out of the rock there. False outside a passage.
        /// </summary>
        bool CaveWall(Vector3 p, out Vector3 wall, out Vector3 outOf)
        {
            wall = outOf = Vector3.zero; var q = new Vector2(p.x, p.z); var ground = GroundMesh != null ? GroundMesh.GetComponent<Collider>() : null;
            foreach (var h in Hollow.All)
            {
                if (!h.FloorAt(q, out _)) continue;
                int i = h.Nearest(q, out _); var from = new Vector3(h.Centre[i].x, p.y, h.Centre[i].z); var dir = p - from; dir.y = 0;
                if (dir.sqrMagnitude < .01f) return false;
                float best = float.MaxValue;
                foreach (var hit in Physics.RaycastAll(from, dir.normalized, h.Half[i] * 1.6f + 1, ~0, QueryTriggerInteraction.Ignore))
                    if (hit.collider is MeshCollider && hit.collider != ground && hit.distance < best) { best = hit.distance; wall = hit.point; outOf = -dir.normalized; }
                return best < float.MaxValue;
            }
            return false;
        }
        /// <summary>Stone for a find's scene (cairns, stones), in the zone's own rock.</summary>
        Material SecretStone(float shade = 1)
        {
            var c = Zone.biome == "ash" ? new Color(.35f, .34f, .33f) : Zone.biome == "mountain" ? MountainStone : Gloom ? new Color(.39f, .38f, .37f) : new Color(.46f, .45f, .42f);
            return RockTint(c * shade);
        }
        /// <summary>A small bright fleck where metal catches the light: enough to catch a curious eye, nowhere near a marker's glow.</summary>
        void Glint(Transform t, Vector3 at, float size = .02f) { Part(PrimitiveType.Sphere, t, at, Vector3.one * size, Glowing(new Color(1, .94f, .8f), .55f)).name = "Glint"; }
        /// <summary>A dented tin, its lid not quite square, a spot of rust and a brass clasp catching the light; sunk a little (half
        /// buried in a scrape); a salt-stiff cord round it for a Salt-Mender's.</summary>
        void SecretTin(Transform item, Func<float> R, bool cord, float sink)
        {
            var box = new GameObject("Tin").transform; box.SetParent(item, false);
            box.localPosition = Vector3.down * sink; box.localRotation = Quaternion.Euler((R() - .5f) * 10, (R() - .5f) * 40, (R() - .5f) * 10);
            Part(PrimitiveType.Cube, box, new Vector3(0, .065f, 0), new Vector3(.3f, .13f, .2f), Tint(art.metal, new Color(.36f, .35f, .32f)));
            Part(PrimitiveType.Cube, box, new Vector3(.006f, .138f, .004f), new Vector3(.314f, .028f, .214f), Tint(art.metal, new Color(.42f, .41f, .38f)), Quaternion.Euler(0, 2, 1.5f));
            Part(PrimitiveType.Cube, box, new Vector3(-.075f, .06f, -.1015f), new Vector3(.11f, .07f, .004f), Tint(art.metal, new Color(.36f, .2f, .12f)));   // rust
            Part(PrimitiveType.Cube, box, new Vector3(0, .12f, -.107f), new Vector3(.03f, .036f, .012f), Tint(art.metal, new Color(.62f, .48f, .22f)));   // the clasp
            Glint(box, new Vector3(.004f, .126f, -.114f));
            if (cord) Part(PrimitiveType.Cube, box, new Vector3(.07f, .075f, 0), new Vector3(.014f, .162f, .218f), Tint(art.cloth, new Color(.8f, .78f, .72f)));
        }
        /// <summary>An oilcloth bundle, tied twice round with cord and knotted, a flap folded over; crusted white where salt was packed in it,
        /// and tied with red cord (an Ash-Walker's sign: take what you need).</summary>
        void SecretBundle(Transform item, Func<float> R, bool salt)
        {
            var b = new GameObject("Bundle").transform; b.SetParent(item, false); b.localRotation = Quaternion.Euler(0, (R() - .5f) * 50, (R() - .5f) * 8);
            var cloth = Tint(art.cloth, Wither(new Color(.25f, .23f, .17f))); var cord = Tint(art.cloth, salt ? new Color(.55f, .16f, .1f) : new Color(.55f, .46f, .3f));
            Part(PrimitiveType.Sphere, b, new Vector3(0, .1f, 0), new Vector3(.48f, .26f, .34f), cloth);
            Part(PrimitiveType.Cube, b, new Vector3(.1f, .19f, -.05f), new Vector3(.24f, .02f, .2f), Tint(art.cloth, Wither(new Color(.2f, .19f, .14f))), Quaternion.Euler(-14, 24, 9));   // the flap
            // Two turns of cord: flat rings standing across it, their rims just proud of the cloth; the knot on top.
            foreach (int k in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, b, new Vector3(k * .09f, .1f, 0), new Vector3(.26f, .008f, .33f), cord, Quaternion.Euler(0, 0, 90));
            Part(PrimitiveType.Sphere, b, new Vector3(.09f, .23f, -.03f), new Vector3(.05f, .04f, .05f), cord);
            if (salt) for (int k = 0; k < 6; k++) Part(PrimitiveType.Cube, b, new Vector3((R() - .5f) * .3f, .2f + R() * .03f, (R() - .5f) * .2f), Vector3.one * (.025f + R() * .03f), Tint(art.stone, new Color(.93f, .93f, .9f)), Quaternion.Euler(R() * 90, R() * 90, 0));
        }
        /// <summary>A page folded small: standing out of a crack (or a hollow), its corner bent over and a spot of wax on it; or, flat,
        /// lying under a stone with its end showing.</summary>
        void SecretPage(Transform item, Func<float> R, bool flat)
        {
            var p = new GameObject("Page").transform; p.SetParent(item, false);
            var paper = Tint(art.plaster, new Color(.9f, .85f, .7f));
            if (flat)
            {
                // Face up, its far end (+Z) under the stone behind it.
                p.localPosition = Vector3.up * .006f; p.localRotation = Quaternion.Euler(0, (R() - .5f) * 30, 0);
                Part(PrimitiveType.Cube, p, new Vector3(0, 0, .03f), new Vector3(.12f, .004f, .16f), paper);
                Part(PrimitiveType.Cube, p, new Vector3(.04f, .012f, -.045f), new Vector3(.05f, .003f, .04f), paper, Quaternion.Euler(-24, 0, 0));   // a corner lifting
                Part(PrimitiveType.Sphere, p, new Vector3(-.02f, .004f, -.02f), new Vector3(.024f, .007f, .024f), Tint(art.cloth, new Color(.52f, .12f, .09f)));
                return;
            }
            // Pushed into the face (the frame's z = 0 plane), turned a little toward you: most of it in the crack, a hand's width out.
            p.localRotation = Quaternion.Euler(0, 40 + (R() - .5f) * 14, (R() - .5f) * 16);
            Part(PrimitiveType.Cube, p, new Vector3(0, 0, -.035f), new Vector3(.004f, .15f, .12f), paper);
            Part(PrimitiveType.Cube, p, new Vector3(.013f, -.045f, -.085f), new Vector3(.004f, .05f, .045f), paper, Quaternion.Euler(0, 35, 0));   // the corner bent over
            Part(PrimitiveType.Sphere, p, new Vector3(.003f, .02f, -.06f), new Vector3(.008f, .022f, .022f), Tint(art.cloth, new Color(.52f, .12f, .09f)));
        }
        /// <summary>The crack a page is pushed into: a dark seam on the face behind it (it stays when the page is taken).</summary>
        void SecretCrack(Transform t, float height)
        {
            var dark = Tint(art.stone, new Color(.06f, .055f, .05f));
            Part(PrimitiveType.Cube, t, new Vector3(0, height, -.004f), new Vector3(.03f, .3f, .012f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(.03f, height - .17f, -.004f), new Vector3(.02f, .1f, .012f), dark, Quaternion.Euler(0, 0, 28));
        }
        /// <summary>A small patch of pale bells nodding on arching stems over rosettes of leaves, glowing faintly: moonbell pale blue on
        /// the meadow, widow's-lamp green-white in the gloom, frostbell ice-blue in the mountains, last-light pale lilac in the ash.</summary>
        void SecretHerb(Transform item, Func<float> R)
        {
            var pale = Zone.biome == "ash" ? new Color(.94f, .86f, 1) : Zone.biome == "mountain" ? new Color(.72f, .88f, 1) : Gloom ? new Color(.84f, 1, .84f) : new Color(.82f, .9f, 1);
            var leafC = Zone.biome == "ash" ? new Color(.38f, .42f, .36f) : Zone.biome == "mountain" ? new Color(.2f, .3f, .24f) : Wither(new Color(.2f, .32f, .15f));
            var bell = Glowing(pale, .4f); var leaf = Tint(art.foliage, leafC); var stem = Tint(art.foliage, leafC * 1.25f);
            for (int c = 0; c < 3; c++)
            {
                var at = c == 0 ? Vector3.zero : Quaternion.Euler(0, c * 150 + R() * 60, 0) * new Vector3(0, 0, .17f + R() * .1f);
                for (int k = 0; k < 5; k++) { var q = Quaternion.Euler(0, k * 72 + R() * 30, 0); Part(PrimitiveType.Sphere, item, at + q * new Vector3(0, .015f, .075f), new Vector3(.055f, .014f, .15f), leaf, q * Quaternion.Euler(-10, 0, 0)); }
                int stems = c == 0 ? 4 : 2 + (int)(R() * 2);
                for (int k = 0; k < stems; k++)
                {
                    // A stem leaning out, then a short piece bending over, the bell hanging from its tip under a little green cap.
                    float h = (c == 0 ? .32f : .24f) + R() * .14f; var q = Quaternion.Euler(0, R() * 360, 0) * Quaternion.Euler(8 + R() * 14, 0, 0);
                    var top = at + q * Vector3.up * h; var over = q * Quaternion.Euler(70, 0, 0); var tip = top + over * Vector3.up * .045f;
                    Part(PrimitiveType.Cylinder, item, at + q * Vector3.up * (h / 2), new Vector3(.011f, h / 2, .011f), stem, q);
                    Part(PrimitiveType.Cylinder, item, top + over * Vector3.up * .022f, new Vector3(.009f, .024f, .009f), stem, over);
                    Part(PrimitiveType.Sphere, item, tip + Vector3.down * .038f, new Vector3(.058f, .07f, .058f), bell);
                    Part(PrimitiveType.Sphere, item, tip + Vector3.down * .006f, new Vector3(.032f, .02f, .032f), stem);
                }
            }
        }
        /// <summary>An iron-banded chest with a domed lid, ring handles and a padlock on its hasp (the lock catches the light), sunk a
        /// little and askew, reaching down to the lowest ground under it.</summary>
        void SecretChest(Transform item, Transform t, Func<float> R)
        {
            float low = 0; foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) low = Mathf.Min(low, LocalGround(t, sx * .36f, sz * .22f));
            var c = new GameObject("Chest").transform; c.SetParent(item, false);
            c.localPosition = Vector3.up * (low - .04f); c.localRotation = Quaternion.Euler((R() - .5f) * 5, (R() - .5f) * 12, (R() - .5f) * 5);
            var wood = Tint(art.timber, new Color(.3f, .2f, .12f)); var lidWood = Tint(art.timber, new Color(.34f, .23f, .14f)); var iron = Tint(art.metal, new Color(.2f, .2f, .21f));
            Part(PrimitiveType.Cube, c, new Vector3(0, .18f, 0), new Vector3(.72f, .36f, .44f), wood);
            Part(PrimitiveType.Cylinder, c, new Vector3(0, .36f, 0), new Vector3(.44f, .36f, .44f), lidWood, Quaternion.Euler(0, 0, 90));   // the domed lid (its lower half inside)
            foreach (int sx in new[] { -1, 1 })
            {
                // An iron band over the lid and down the front and back, a corner bracket at each front corner, a ring handle at the end.
                Part(PrimitiveType.Cylinder, c, new Vector3(sx * .22f, .36f, 0), new Vector3(.46f, .025f, .46f), iron, Quaternion.Euler(0, 0, 90));
                foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, c, new Vector3(sx * .22f, .18f, sz * .225f), new Vector3(.05f, .36f, .012f), iron);
                Part(PrimitiveType.Cube, c, new Vector3(sx * .34f, .05f, -.225f), new Vector3(.06f, .08f, .012f), iron);
                Part(PrimitiveType.Cylinder, c, new Vector3(sx * .366f, .22f, 0), new Vector3(.08f, .006f, .08f), iron, Quaternion.Euler(0, 0, 90));
            }
            // The hasp, and the padlock hanging on it: body, shackle, a brass keyhole plate catching the light.
            Part(PrimitiveType.Cube, c, new Vector3(0, .34f, -.228f), new Vector3(.05f, .1f, .014f), iron);
            Part(PrimitiveType.Cube, c, new Vector3(0, .27f, -.247f), new Vector3(.085f, .075f, .035f), Tint(art.metal, new Color(.26f, .25f, .24f)));
            Part(PrimitiveType.Cylinder, c, new Vector3(0, .325f, -.247f), new Vector3(.055f, .006f, .055f), iron, Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Cube, c, new Vector3(0, .265f, -.266f), new Vector3(.018f, .026f, .004f), Tint(art.metal, new Color(.62f, .48f, .22f)));
            Glint(c, new Vector3(.006f, .272f, -.27f));
        }
        /// <summary>A small brass key on a greasy thong, hung from a nail driven into what is behind it: the key is the find (it swings a
        /// little), the nail stays.</summary>
        void SecretKey(Transform item, Transform t, float height, Func<float> R)
        {
            var iron = Tint(art.metal, new Color(.18f, .18f, .19f));
            Part(PrimitiveType.Cylinder, t, new Vector3(0, height, .005f), new Vector3(.014f, .025f, .014f), iron, Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Cylinder, t, new Vector3(0, height, -.022f), new Vector3(.03f, .004f, .03f), iron, Quaternion.Euler(90, 0, 0));
            var k = new GameObject("Key").transform; k.SetParent(item, false); k.localPosition = new Vector3(0, 0, -.028f); k.localRotation = Quaternion.Euler(0, 0, (R() - .5f) * 16);
            var brass = Tint(art.metal, new Color(.66f, .5f, .24f));
            Part(PrimitiveType.Cylinder, k, new Vector3(0, -.045f, 0), new Vector3(.007f, .045f, .007f), Tint(art.cloth, new Color(.18f, .13f, .09f)));   // the thong
            Part(PrimitiveType.Cylinder, k, new Vector3(0, -.11f, 0), new Vector3(.042f, .005f, .042f), brass, Quaternion.Euler(90, 0, 0));   // the bow
            Part(PrimitiveType.Cylinder, k, new Vector3(0, -.11f, -.0055f), new Vector3(.017f, .002f, .017f), Tint(art.metal, new Color(.12f, .1f, .08f)), Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Cube, k, new Vector3(0, -.168f, 0), new Vector3(.009f, .075f, .009f), brass);   // the shank
            Part(PrimitiveType.Cube, k, new Vector3(.013f, -.197f, 0), new Vector3(.022f, .012f, .006f), brass);   // the bit
            Part(PrimitiveType.Cube, k, new Vector3(.011f, -.181f, 0), new Vector3(.016f, .01f, .006f), brass);
            Glint(k, new Vector3(-.012f, -.098f, -.006f), .014f);
        }
        /// <summary>A little cairn: three stones stacked, each on the one below; its top stone of <paramref name="top"/> when given.</summary>
        void SecretCairn(Transform t, Func<float> R, Material top)
        {
            var stone = SecretStone(); float at = LocalGround(t, 0, 0) - .03f;
            float[] w = { .56f, .42f, .28f }, h = { .26f, .22f, .18f };
            for (int k = 0; k < 3; k++)
            {
                // A boulder lump's flat foot lies .18 of its height under its middle; the next stone sits a little down into this one.
                Lump(BoulderAt((int)(R() * 6)), t, new Vector3((R() - .5f) * .06f, at + .18f * h[k], (R() - .5f) * .06f), new Vector3(w[k], h[k], w[k] * .9f), k == 2 && top != null ? top : stone, R() * 360);
                at += .58f * h[k];
            }
        }
        /// <summary>An old beacon: a ring of fire-cracked stones round a charred patch with burnt-out stubs, a standing stone behind it.</summary>
        void SecretBeacon(Transform t, Func<float> R)
        {
            var soot = Tint(art.stone, new Color(.16f, .15f, .14f)); var burnt = RockTint(new Color(.28f, .26f, .24f)); var coal = Tint(art.timber, new Color(.07f, .06f, .05f));
            float g = LocalGround(t, 0, 0);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, g + .006f, 0), new Vector3(1.5f, .01f, 1.5f), soot);
            for (int k = 0; k < 7; k++)
            {
                float a = k * Mathf.PI * 2 / 7 + R() * .3f, x = Mathf.Cos(a) * 1.05f, z = Mathf.Sin(a) * 1.05f, s = .38f + R() * .22f;
                Lump(BoulderAt((int)(R() * 6)), t, new Vector3(x, LocalGround(t, x, z) + .18f * s * .8f - .05f, z), new Vector3(s, s * .8f, s * .9f), k % 3 == 0 ? soot : burnt, R() * 360);
            }
            for (int k = 0; k < 3; k++) Part(PrimitiveType.Cylinder, t, new Vector3((R() - .5f) * .5f, g + .05f, (R() - .5f) * .5f), new Vector3(.09f, .26f + R() * .1f, .09f), coal, Quaternion.Euler(90, R() * 180, 0));
            Lump(BoulderAt((int)(R() * 6)), t, new Vector3(.2f, LocalGround(t, .2f, 1.6f) + .1f, 1.6f), new Vector3(.45f, 1.05f, .38f), burnt, R() * 360);
        }
        /// <summary>A line of salt laid across the way (along the frame's x), broken here and there, with a little cairn beside it whose
        /// top stone is crusted white.</summary>
        void SecretSaltLine(Transform t, Func<float> R)
        {
            var salt = Tint(art.stone, new Color(.92f, .92f, .89f));
            for (float x = -3.7f; x < 3.7f; x += .55f)
            {
                if (R() < .12f) continue;
                float len = .45f + R() * .2f, z = (R() - .5f) * .06f, mid = x + len / 2;
                Part(PrimitiveType.Cube, t, new Vector3(mid, LocalGround(t, mid, z) + .008f, z), new Vector3(len, .02f, .1f + R() * .05f), salt, Quaternion.Euler(0, (R() - .5f) * 8, 0));
            }
            // In the middle of the way, on the near side (-Z) of the line: the ends run in under rock the colliders don't cover.
            var c = new GameObject("Salt cairn").transform; c.SetParent(t, false); c.localPosition = new Vector3(.3f, 0, -1.3f);
            SecretCairn(c, R, salt);
        }
        /// <summary>A cold camp in front of a find: a ring of stones round old ash and two charred sticks, a rolled bedroll tied twice.</summary>
        void SecretCamp(Transform t, Func<float> R)
        {
            var stone = SecretStone(.85f); var ash = Tint(art.stone, new Color(.21f, .2f, .19f)); var coal = Tint(art.timber, new Color(.07f, .06f, .05f));
            var fire = new Vector3(.3f, 0, -1.35f); fire.y = LocalGround(t, fire.x, fire.z);
            Part(PrimitiveType.Cylinder, t, fire + Vector3.up * .006f, new Vector3(.8f, .01f, .8f), ash);
            for (int k = 0; k < 8; k++) { var q = Quaternion.Euler(0, k * 45 + R() * 12, 0); var p = fire + q * new Vector3(0, 0, .48f); p.y = LocalGround(t, p.x, p.z) + .02f; Lump(BoulderAt(k), t, p, new Vector3(.24f, .16f, .2f), stone, R() * 360); }
            for (int k = 0; k < 2; k++) Part(PrimitiveType.Cylinder, t, fire + new Vector3(0, .04f, 0), new Vector3(.05f, .24f, .05f), coal, Quaternion.Euler(90, k * 70 + R() * 30, 0));
            var roll = new Vector3(-1.05f, 0, -.35f); roll.y = LocalGround(t, roll.x, roll.z) + .12f;
            var lie = Quaternion.Euler(0, 15 + R() * 30, 0) * Quaternion.Euler(0, 0, 90); var along = lie * Vector3.up;
            Part(PrimitiveType.Cylinder, t, roll, new Vector3(.26f, .38f, .26f), Tint(art.cloth, Wither(new Color(.4f, .33f, .24f))), lie);
            foreach (int k in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, t, roll + along * (k * .2f), new Vector3(.272f, .012f, .272f), Tint(art.cloth, new Color(.3f, .24f, .16f)), lie);
        }
        /// <summary>A fallen trunk lying behind a find along the ground, half sunk, its broken end splintered and a snapped limb standing up.</summary>
        void SecretLog(Transform t, Func<float> R)
        {
            var bark = Tint(art.bark, Gloom || Zone.biome == "ash" ? new Color(.52f, .51f, .49f) : new Color(.34f, .3f, .26f)); var heart = Tint(art.timber, Wither(new Color(.5f, .42f, .31f)));
            float len = 2.4f + R() * .5f, rad = .23f; var dir = Quaternion.Euler(0, (R() - .5f) * 16, 0) * Vector3.right; var mid = new Vector3(.15f, 0, .6f);
            Vector3 End(float k) { var p = mid + dir * (k * len / 2); p.y = LocalGround(t, p.x, p.z) + rad * .55f; return p; }
            Vector3 a = End(-1), b = End(1), d = b - a;
            Part(PrimitiveType.Cylinder, t, (a + b) / 2, new Vector3(rad * 2, d.magnitude / 2, rad * 2), bark, Quaternion.FromToRotation(Vector3.up, d.normalized));
            for (int k = 0; k < 3; k++) { var q = Quaternion.FromToRotation(Vector3.up, d.normalized) * Quaternion.Euler((R() - .5f) * 50, k * 120, (R() - .5f) * 50); Part(PrimitiveType.Cube, t, b + d.normalized * .08f + q * new Vector3(0, .1f, .09f), new Vector3(.07f, .26f, .05f), k == 1 ? heart : bark, q); }
            var stub = a + d * .35f + Vector3.up * rad * .8f; var up = (Vector3.up + dir * .4f).normalized;
            Part(PrimitiveType.Cylinder, t, stub + up * .2f, new Vector3(.08f, .22f, .08f), bark, Quaternion.FromToRotation(Vector3.up, up));
        }
        /// <summary>A hollow stump (a lightning-split yew or oak gone to punk) behind a find, the dark hollow in its front at the find's
        /// height; heartwood shards stand up round its broken top.</summary>
        void SecretStump(Transform t, Func<float> R, float hollowAt)
        {
            var bark = Tint(art.bark, Wither(new Color(.35f, .3f, .25f))); var wood = Tint(art.timber, Wither(new Color(.46f, .38f, .28f)));
            var c = new Vector3(0, 0, .32f); float g = LocalGround(t, c.x, c.z), top = Mathf.Max(.85f, hollowAt + .3f);
            Part(PrimitiveType.Cylinder, t, new Vector3(c.x, g + top / 2 - .06f, c.z), new Vector3(.6f, top / 2 + .06f, .6f), bark);
            for (int k = 0; k < 5; k++)
            {
                var q = Quaternion.Euler(0, k * 72 + R() * 30, 0); var p = c + q * new Vector3(0, 0, .22f);
                Part(PrimitiveType.Cube, t, new Vector3(p.x, g + top + .04f + R() * .08f, p.z), new Vector3(.06f + R() * .05f, .16f + R() * .18f, .05f), k % 2 == 0 ? bark : wood, q * Quaternion.Euler(-12 - R() * 16, 0, (R() - .5f) * 24));
            }
            for (int k = 0; k < 4; k++) { var q = Quaternion.Euler(0, k * 90 + 45 + R() * 20, 0); var p = c + q * new Vector3(0, 0, .3f); Part(PrimitiveType.Sphere, t, new Vector3(p.x, LocalGround(t, p.x, p.z) + .03f, p.z), new Vector3(.2f, .16f, .42f), bark, q * Quaternion.Euler(12, 0, 0)); }
            Part(PrimitiveType.Sphere, t, new Vector3(0, hollowAt, .005f), new Vector3(.24f, .32f, .05f), Tint(art.stone, new Color(.045f, .04f, .035f)));   // the hollow
        }
        /// <summary>Two stones either side of a find, a little behind it, so it sits in the cleft between them.</summary>
        void SecretRocks(Transform t, Func<float> R)
        {
            foreach (int k in new[] { -1, 1 })
            {
                float s = .5f + R() * .25f; var p = new Vector3(k * (.26f + s * .45f), 0, .1f + R() * .1f); p.y = LocalGround(t, p.x, p.z) + .18f * s * .75f - .06f;
                Lump(BoulderAt((int)(R() * 6)), t, p, new Vector3(s, s * .75f, s * .9f), SecretStone(), R() * 360);
            }
        }
        /// <summary>A flat stone lying half over a find (a page slid under it, its near end showing).</summary>
        void SecretFlatStone(Transform t, Func<float> R)
        {
            var p = new Vector3(.02f, 0, .25f); p.y = LocalGround(t, p.x, p.z) + .02f;
            Lump(BoulderAt((int)(R() * 6)), t, p, new Vector3(.46f, .13f, .36f), SecretStone(), R() * 360);
        }
        /// <summary>A shallow scrape (some animal dug here) round a half-buried find, the spoil heaped beside it.</summary>
        void SecretScrape(Transform t, Func<float> R)
        {
            bool ashen = Zone.biome == "ash";
            Part(PrimitiveType.Cylinder, t, new Vector3(0, LocalGround(t, 0, 0) + .004f, 0), new Vector3(.62f, .006f, .46f), Tint(art.soil, ashen ? new Color(.25f, .24f, .24f) : new Color(.2f, .16f, .12f)), Quaternion.Euler(0, R() * 30, 0));
            Part(PrimitiveType.Sphere, t, new Vector3(.42f, LocalGround(t, .42f, .1f) + .015f, .1f), new Vector3(.34f, .1f, .24f), ashen ? Tint(art.ash, new Color(.5f, .5f, .5f)) : Tint(art.soil, new Color(.3f, .24f, .17f)));
        }

        // ---------- edges ----------
        void BuildForestEdge()
        {
            // A ragged ring of woods: pine and autumn broadleaf; grey dead wood, dark pine and boulders in gloom. A Wasting holds the east side.
            float edge = Half - 5; var sides = Zone.wasting != null ? new[] { 0, 1, 2 } : new[] { 0, 1, 2, 3 };
            for (float s = -Half + 3; s < Half - 3; s += 3.2f)
                foreach (var side in sides)
                {
                    var at = side == 0 ? new Vector2(-edge - R01 * 3, s) : side == 1 ? new Vector2(s, edge + R01 * 3) : side == 2 ? new Vector2(s, -edge - R01 * 3) : new Vector2(edge + R01 * 3, s);
                    if (Zone.wasting != null && at.x > Zone.wasting.x - 6) continue;
                    if (NearRoad(at, 5) || Water.NearWater(at, 1.5f) || OnPaint(at, "salt")) continue;   // (the coast: the flats run to the sea)
                    var spot = Spot(at, 1.8f); if (spot.HasValue) at = spot.Value;   // never growing through a grove tree
                    if (Gloom)
                    {
                        float pick = R01;
                        if (pick < .4f) Keep(DeadTree(at, .6f + R01 * .5f, DeadBark(), 3, statics), at, spot.HasValue);
                        else if (pick < .82f) { var pt = Root(new ZoneProp { kind = "pine", at = at, rotation = R01 * 360, scale = .85f + R01 * .5f }, statics); Pine(pt); Keep(pt, at, spot.HasValue); }
                        else EdgeRock(at);
                        continue;
                    }
                    if (Zone.biome == "ash") { if (R01 < .55f) Keep(DeadTree(at, .6f + R01 * .5f, Tint(art.bark, new Color(.2f, .19f, .18f)), 3, statics), at, spot.HasValue); else EdgeRock(at); continue; }
                    var p = new ZoneProp { kind = Zone.biome == "mountain" ? (R01 < .7f ? "pine" : "rock") : R01 < .6f ? "pine" : "tree", at = at, rotation = R01 * 360, scale = .85f + R01 * .5f, variant = (int)(R01 * 4) };
                    if (p.kind == "rock") { EdgeRock(at); continue; }
                    if (Zone.biome == "verdant")
                    {
                        // The forest that doesn't know when to stop: the edge is giant trees (every fourth place, when the ground is free)
                        // with broadleaf between, never pines. The draws are the ones a pine or tree took.
                        if (p.kind == "pine" && TrunkClear(at, 9) && !NearProp(at, 9))
                        {
                            var gt = Root(new ZoneProp { kind = "giant_tree", at = at, rotation = p.rotation, scale = .8f + (p.scale - .85f) * .5f, variant = p.variant % 3 }, statics);
                            GiantTree(gt, p.variant % 3); trunks.Add(at); continue;
                        }
                        p.kind = "tree";
                    }
                    var t = Root(p, statics);
                    if (p.kind == "pine") Pine(t); else Broadleaf(t, p.variant);
                    if (p.kind == "pine" && !SeatPine(t, at))
                    {
                        // Too steep for a pine: every other one becomes a boulder, drawn from its own stream (the zone's is untouched).
                        DestroyImmediate(t.gameObject);
                        uint hash = (uint)(Mathf.RoundToInt(at.x * 10) * 73856093) ^ (uint)(Mathf.RoundToInt(at.y * 10) * 19349663) ^ (uint)Zone.seed;
                        if ((hash >> 7 & 1) == 0) { var zoneRng = rng; rng = new System.Random((int)(hash & 0x7fffffff)); EdgeRock(at); rng = zoneRng; }
                    }
                    else Keep(t, at, spot.HasValue);   // recorded, or dropped when crowded (the draws are unchanged)
                    var bushAt = at + new Vector2(R01 * 4 - 2, R01 * 4 - 2);
                    if (Zone.biome == "meadow" && R01 < .5f && !Water.NearWater(bushAt, 1)) Bush(bushAt, p.kind == "pine");
                }
            // Mountains: boulders strewn over the rough ground between the kept ways (never on a road, camp or path).
            if (Zone.biome == "mountain")
                for (int i = 0, placed = 0; i < 600 && placed < 70; i++)
                {
                    var at = new Vector2((R01 - .5f) * (Zone.size - 16), (R01 - .5f) * (Zone.size - 16));
                    if (Keep(at.x, at.y) > .1f || UpAt(at.x, at.y) < .85f || NearProp(at, 4)) continue;
                    EdgeRock(at); placed++;
                }
        }
        /// <summary>The mountains' rock (boulders, crags, tors): a shade darker than plain stone, so sunlit rock stays under the fog.</summary>
        Color MountainStone { get { return new Color(.36f, .35f, .335f); } }
        /// <summary>A big weathered boulder for rocky edges (mountain and ash zones). None on near-vertical ground (it read as glued
        /// to the face); its draws are taken all the same, so the zone's stream is unchanged.</summary>
        void EdgeRock(Vector2 at)
        {
            float yaw = R01 * 360, s = 1.6f + R01 * 1.8f; Mesh b0 = Boulder(); float y0 = R01 * 360; Mesh b1 = Boulder(); float y1 = R01 * 360;
            if (Zone.biome == "mountain" && Steep(at, s) > 1) return;   // steeper than 45 degrees across it
            if (Hollow.CoverAt(at, 2) > 0) return;   // or in a cave
            var t = Root(new ZoneProp { kind = "rock", at = at, rotation = yaw }, statics);
            var mat = RockTint(Zone.biome == "ash" ? new Color(.33f, .32f, .31f) : Zone.biome == "mountain" ? MountainStone : new Color(.42f, .41f, .39f));
            if (Zone.biome == "mountain") SinkBySlope(t, at, s);
            float deep = Zone.biome == "mountain" ? .14f * s : 0;   // mountains: the lumps a seventh deeper in the ground (the collider is where it was)
            Lump(b0, t, new Vector3(0, .45f * s - deep, 0), new Vector3(2.1f * s, 1.5f * s, 1.8f * s), mat, y0);
            Lump(b1, t, new Vector3(.8f * s, .25f * s - deep, .45f * s), new Vector3(1.2f * s, .9f * s, 1.1f * s), mat, y1);
            Solid(t, new Vector3(0, .6f * s, 0), new Vector3(1.8f * s, 1.2f * s, 1.5f * s));
            if (Zone.biome == "mountain") RockSkirt(t, s, mat);
        }
        /// <summary>The steepest rise or fall per metre from a point out to <paramref name="reach"/> (8 ways, drawn ground and height
        /// function both, so a sharp crest or a ledge edge counts): about 1 at 45 degrees.</summary>
        float Steep(Vector2 at, float reach)
        {
            float hc = Mathf.Min(HeightAt(at.x, at.y), MeshY(at.x, at.y)), worst = 0;
            for (int k = 0; k < 8; k++)
            {
                var q = at + new Vector2(Mathf.Cos(k * Mathf.PI / 4), Mathf.Sin(k * Mathf.PI / 4)) * reach;
                worst = Mathf.Max(worst, Mathf.Max(Mathf.Abs(HeightAt(q.x, q.y) - hc), Mathf.Abs(MeshY(q.x, q.y) - hc)) / reach);
            }
            return worst;
        }
        /// <summary>Sinks a boulder of size s by the fall of the slope across it (to the lowest drawn ground under it, at most .6 s),
        /// so its downhill side never hangs in the air.</summary>
        void SinkBySlope(Transform t, Vector2 at, float s)
        {
            float low = t.position.y;
            for (int k = 0; k < 8; k++) { var q = at + new Vector2(Mathf.Cos(k * Mathf.PI / 4), Mathf.Sin(k * Mathf.PI / 4)) * s; low = Mathf.Min(low, Mathf.Min(HeightAt(q.x, q.y), MeshY(q.x, q.y))); }
            t.position += Vector3.down * (Mathf.Clamp(t.position.y - low, 0, .6f * s) + .15f * s);
        }
        /// <summary>Mountains: 4-7 small stones round the foot of a boulder of size s (.22-.52 of it, .95-1.65 of it out, each sunk a
        /// fifth to two fifths of its height into the lower of the height function and the drawn ground), so the boulder lies in
        /// its own fall of rock, not alone on the turf. No colliders, so the navmesh is as it was; none where loose rock may not
        /// lie (RockMayLie) or in a cave. Draws only from its own stream, keyed on where the boulder stands.</summary>
        void RockSkirt(Transform t, float s, Material mat)
        {
            var own = TreeRandom(t.position); float O() { return (float)own.NextDouble(); }
            int n = 4 + (int)(O() * 4); float up = Mathf.Max(.01f, t.lossyScale.y);
            for (int k = 0; k < n; k++)
            {
                float a = (k + O()) / n * Mathf.PI * 2, r = s * (.95f + O() * .7f), size = s * (.22f + O() * .3f), h = size * .8f, sunk = .2f + O() * .2f, yaw = O() * 360;
                var at = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r * .9f); var w = t.TransformPoint(at); var q = new Vector2(w.x, w.z);
                if (!RockMayLie(q) || Hollow.CoverAt(q, 1) > 0) continue;
                at.y = (Mathf.Min(HeightAt(w.x, w.z), MeshY(w.x, w.z)) - t.position.y) / up + .18f * h - sunk * h;
                Lump(BoulderAt(k + (int)yaw), t, at, new Vector3(size * 1.25f, h, size * 1.1f), mat, yaw);
            }
        }
        /// <summary>
        /// Mountains: whether a generated pine may stand here, and if so sinks it to the lowest drawn ground under it (its trunk and
        /// a little beyond). Refused on steep ground (a rise or fall over .7 m per metre within 1.5 m, .75 within 3 m, or a grid
        /// normal under .82), where its trunk ended in mid-air in front of the slope; the ring tests also catch sharp crests and
        /// ledge edges, which central differences call level.
        /// </summary>
        bool SeatPine(Transform t, Vector2 at)
        {
            if (Zone.biome != "mountain") return true;
            if (Steep(at, 1.5f) > .7f || Steep(at, 3) > .75f || UpAt(at.x, at.y) < .82f) return false;
            float low = Mathf.Min(HeightAt(at.x, at.y), MeshY(at.x, at.y)), foot = .4f * t.localScale.x;
            for (int k = 0; k < 12; k++)
                foreach (float f in new[] { foot, foot * 2 })
                {
                    var q = at + new Vector2(Mathf.Cos(k * Mathf.PI / 6), Mathf.Sin(k * Mathf.PI / 6)) * f;
                    low = Mathf.Min(low, Mathf.Min(HeightAt(q.x, q.y), MeshY(q.x, q.y)));
                }
            t.position = new Vector3(t.position.x, low - .12f, t.position.z); return true;
        }
        /// <summary>
        /// Mountains: a loose rock (no quest use) where crags rose round it (PrepareRelief keeps no ground for loose rocks) rolls to
        /// the flattest ground within 6 m that is off the roads, out of the water and no more a kept way than where it lay; stays put
        /// when nothing near is flatter. Draws nothing random.
        /// </summary>
        Vector2 FlatterSpot(Vector2 at, float size)
        {
            float best = Steep(at, size), keepHere = Keep(at.x, at.y) + .05f; var spot = at;
            if (best <= .8f) return at;
            for (float r = 2; r <= 6; r += 2)
                for (int k = 0; k < 12; k++)
                {
                    var q = at + new Vector2(Mathf.Cos(k * Mathf.PI / 6), Mathf.Sin(k * Mathf.PI / 6)) * r;
                    if (Mathf.Max(Mathf.Abs(q.x), Mathf.Abs(q.y)) > Half - 4 || NearRoad(q, 1.5f + size) || Water.NearWater(q, 1.5f) || Keep(q.x, q.y) > keepHere) continue;
                    float s = Steep(q, size); if (s < best - .1f) { best = s; spot = q; }
                }
            return spot;
        }
        bool NearRoad(Vector2 p, float margin)
        {
            foreach (var r in Zone.roads) if (DistanceToPath(p, r.points) < r.width / 2 + margin) return true;
            return false;
        }
        void BuildBoundaries()
        {
            var root = new GameObject("Zone boundary").transform; root.SetParent(transform, false);
            for (int side = 0; side < 4; side++)
            {
                var b = new GameObject("Boundary").AddComponent<BoxCollider>(); b.transform.SetParent(root, false);
                bool ns = side < 2; float sign = side % 2 == 0 ? 1 : -1;
                // Mountain edges climb far past 12 m: stand tall enough that no walkable slope steps over it onto the backdrop.
                float h = Zone.biome == "mountain" ? 90 : 12;
                b.center = ns ? new Vector3(0, h / 2 - 1, sign * (Half - 1)) : new Vector3(sign * (Half - 1), h / 2 - 1, 0);
                b.size = ns ? new Vector3(Zone.size, h, 2) : new Vector3(2, h, Zone.size);
            }
        }

        // ---------- backdrop ----------
        /// <summary>How far the scenery runs on past the playable edge, in metres (the camera's far plane reaches past it).</summary>
        const float BackdropWidth = 90;
        /// <summary>How far past the edge the ground's dressing (grass, ferns, flowers) runs on, thinning out, in metres.</summary>
        const float EdgeDressing = 26;
        /// <summary>The backdrop skirt's drawn height at a point (BuildBackdrop sets it).</summary>
        Func<float, float, float> skirtY;
        /// <summary>The ground under a point for dressing: the zone's inside the edge, the backdrop's skirt as drawn past it.</summary>
        Vector3 EdgeGround(Vector2 p, float lift)
        {
            if (skirtY == null || Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) <= Half) return Ground(p, lift);
            return new Vector3(p.x, skirtY(p.x, p.y) + lift, p.y);
        }
        /// <summary>
        /// How much grass should grow at a point past the edge (Openness inside it). The skirt wears the ground's paint mirrored
        /// across the edge, so the grass follows the mirror image: none on the road's paint running on, the mirrored yards and
        /// fields. None past EdgeDressing, toward the Wasting, or where the skirt climbs steeper than 40 degrees. Draws nothing.
        /// </summary>
        float EdgeOpenness(Vector2 p)
        {
            float past = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Half; if (past <= 0) return Openness(p);
            if (past >= EdgeDressing || (Zone.wasting != null && p.x > Zone.wasting.x - 6)) return 0;
            if (skirtY != null && Mathf.Max(Mathf.Abs(skirtY(p.x + 1, p.y) - skirtY(p.x - 1, p.y)), Mathf.Abs(skirtY(p.x, p.y + 1) - skirtY(p.x, p.y - 1))) > 1.7f) return 0;
            return Openness(new Vector2(p.x > Half ? Zone.size - p.x : p.x < -Half ? -Zone.size - p.x : p.x, p.y > Half ? Zone.size - p.y : p.y < -Half ? -Zone.size - p.y : p.y));
        }
        /// <summary>The near wood's leaf mass by leaf family (0 to 2): deep, mid and light, the colours of the family's painted card.</summary>
        static readonly Color[] NearLeaf = {
            new Color(.20f, .33f, .12f), new Color(.27f, .42f, .15f), new Color(.34f, .50f, .18f),
            new Color(.33f, .38f, .11f), new Color(.46f, .48f, .13f), new Color(.56f, .56f, .16f),
            new Color(.50f, .22f, .07f), new Color(.64f, .30f, .08f), new Color(.76f, .42f, .11f) };
        /// <summary>Ground grid resolution (the backdrop's inner row shares the ground's edge vertices exactly).</summary>
        int GroundSegments { get { return Mathf.RoundToInt(Zone.size / 1.25f); } }
        /// <summary>
        /// Scenery past the playable edge, so the world never visibly ends: a skirt of ground carrying on from the edge (same
        /// heights at the seam, the painted ground mirrored across it) that rises into the biome's hills or mountains, crags
        /// above the treeline on mountain and ash ridges, and silhouettes in the zone's own mix of woods. A road that runs off
        /// the edge carries on (its mirrored paint) up a valley that closes into trees or a col. By the edge a near wood
        /// (full crowns, pines, dead wood and boulders from 3 to 21 m out, in the same mix) stands between the silhouettes'
        /// first rows, so the land carries on at the line; the grass and plants run on over the same slope (EdgeOpenness).
        /// Visual only: no colliders, no nav sources, outside the navmesh bounds and the map. Its own random streams leave
        /// the zone's layout unchanged.
        /// </summary>
        void BuildBackdrop()
        {
            var root = new GameObject("Backdrop").transform; root.SetParent(transform, false);
            bool mountain = Zone.biome == "mountain", ash = Zone.biome == "ash";
            float rise = mountain ? 58 : ash ? 20 : 32;   // the Ashland Rim is a plain: low swells, not a wall (fog hides the far edge)
            // Valleys: the mirror image of the last 45 m of each road that leaves the zone (where its mirrored paint runs).
            var valleys = new List<Vector2[]>();
            foreach (var road in Zone.roads)
                for (int end = 0; end < 2 && road.points.Length > 1; end++)
                {
                    var pts = end == 0 ? road.points : Enumerable.Reverse(road.points).ToArray(); var e = pts[0];
                    bool ew = Mathf.Abs(e.x) >= Half - 3;
                    if ((!ew && Mathf.Abs(e.y) < Half - 3) || (Zone.wasting != null && e.x > Zone.wasting.x)) continue;
                    var list = new List<Vector2>(); float left = 45;
                    for (int i = 0; i < pts.Length && left > 0; i++)
                    {
                        var p = pts[i];
                        if (i > 0) { float seg = Vector2.Distance(pts[i - 1], p); if (seg > left) p = Vector2.Lerp(pts[i - 1], p, left / seg); left -= seg; }
                        list.Add(ew ? new Vector2(Mathf.Sign(e.x) * Zone.size - p.x, p.y) : new Vector2(p.x, Mathf.Sign(e.y) * Zone.size - p.y));
                    }
                    valleys.Add(list.ToArray());
                }
            // Height past the edge: the zone's own ground carried on, plus a rise that is nothing on the seam and levels off
            // as a ridge by 80% of the width (above the camera, so nothing below the horizon shows through).
            float Rise(float x, float z)
            {
                float d = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) - Half; if (d <= 0) return 0;
                float n = Mathf.PerlinNoise(x * .011f + 300, z * .011f + Zone.seed % 500), m = Mathf.PerlinNoise(x * .045f + 80, z * .045f + 30);
                if (mountain) { n = 1 - Mathf.Abs(n * 2 - 1); m = 1 - Mathf.Abs(m * 2 - 1); }   // ridged: peaks and cols with jagged crests, not rolling hills
                // Mountains start their climb more gently, so an exit near the edge doesn't face a fog-grey wall.
                float ramp = Mathf.SmoothStep(0, 1, d / (BackdropWidth * .8f)); if (mountain) ramp *= ramp;
                float h = rise * (.5f + .5f * n + (m - .5f) * (mountain ? .4f : .15f)) * ramp;
                var at = new Vector2(x, z);
                foreach (var v in valleys) h *= Mathf.Lerp(.5f, 1, Mathf.Clamp01((DistanceToPath(at, v) - 6) / 20));
                if (Zone.wasting != null) h *= Mathf.Clamp01((Zone.wasting.x + 2 - x) / 40);   // hills fall away to the unmade, which stays flat
                return h;
            }
            float H(float x, float z) { return HeightAt(x, z) + Rise(x, z); }
            void Show(Mesh mesh, Material mat)
            {
                var go = new GameObject(mesh.name); go.transform.SetParent(root, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // The skirt: four sides (each culled alone and clear of the village's point lights), in the ground's own paint.
            // Rings close together by the edge (the slope the exits look up, and the dressing stands on it), wider further out.
            float[] rings = { 0, 1.5f, 2.5f, 4, 6, 8, 10.5f, 13, 16, 19, 22.5f, 26, 30, 34, 43, 53, 64, 76, BackdropWidth };
            var paint = GroundMesh.GetComponent<MeshRenderer>().sharedMaterial; var skirtV = new Vector3[4][];
            for (int side = 0; side < 4; side++)
            {
                var skirt = ZoneMeshes.Backdrop(Zone.size, GroundSegments, side, rings, H); skirtV[side] = skirt.vertices;
                // Past the Wasting the mirrored paint would bring meadow, roads and cracks back out of the grey: past the edge, spread
                // the unmade strip's own paint (Wasting line to edge) across the skirt instead. Linear, so it interpolates exactly.
                float start = Zone.wasting != null ? Zone.wasting.x + 2 : Half;
                if (start < Half - 1)
                {
                    var sv = skirt.vertices; var suv = skirt.uv;
                    for (int i = 0; i < sv.Length; i++) if (sv[i].x > Half) suv[i].x = (Half - (sv[i].x - Half) * (Half - start) / BackdropWidth + Half) / Zone.size;
                    skirt.uv = suv;
                }
                Show(skirt, paint);
            }
            // Silhouettes: the zone's own mix of woods (weighted by its groves) on the lower slopes, tors above the treeline on
            // mountain and ash ridges. One merged mesh per material and side, no colliders, no shadows.
            var rnd = new System.Random(Zone.seed + 4242); float R() { return (float)rnd.NextDouble(); }
            float dead = 0, pines = 0, leafy = 0;
            foreach (var g in Zone.groves) { if (g.kind == "dead") dead += g.count; else if (g.kind == "pine") pines += g.count; else if (g.kind == "broadleaf") leafy += g.count; }
            if (dead + pines + leafy <= 0) pines = 1;
            var spire = ZoneMeshes.Cone(1, 1, 6); var crown = ZoneMeshes.Blob(Zone.seed + 910, .55f, false, 5, 7); var crag = ZoneMeshes.Blob(Zone.seed + 920, 1.1f, true, 8, 12);
            var pineMat = Tint(art.pine, Wither(new Color(.16f, .23f, .16f))); var deadMat = Tint(art.bark, Gloom ? new Color(.55f, .54f, .53f) : new Color(.26f, .24f, .22f));   // gloom: the grey wood carries on
            var rockMat = RockTint(ash ? new Color(.36f, .355f, .36f) : mountain ? MountainStone : new Color(.40f, .39f, .38f));
            if (rockMat.HasProperty("_Scale")) { rockMat = new Material(rockMat) { name = "Backdrop rock" }; rockMat.SetFloat("_Scale", 22); }   // painted rock: 22 m a tile (beds of 2.6 to 5.7 m), so the strata still read from the zone; its own clone, the near boulders keep 5 m
            var parts = new Dictionary<(Material, int), List<CombineInstance>>();
            // The wood silhouettes' painted leaf cards (the zone's own Crulanda/Leaf materials), one card mesh per material and side,
            // so a far ridge is ragged leaf and needle like the near trees, not blobs and cones beside them; the blobs and cones when
            // the palette has no card art. Their shapes draw from a stream of their own: the placing draws (rnd) are as they were,
            // so every silhouette stands and sits where it did.
            bool cardArt = art.leafCards != null && art.leafCards.Length > 0 && art.pineBough != null;
            var sil = new System.Random(Zone.seed + 4343); float S() { return (float)sil.NextDouble(); }
            var cardSets = new Dictionary<(Material, int), ZoneMeshes.Cards>();
            ZoneMeshes.Cards CardsFor(Material mat, int side) { if (!cardSets.TryGetValue((mat, side), out var set)) cardSets[(mat, side)] = set = new ZoneMeshes.Cards(); return set; }
            var boughMat = cardArt ? LeafMaterial(art.pineBough, new Color(.8f, .88f, .8f)) : null;
            // The skirt as drawn: the height of its own triangles under a point. Its rings are the edge row scaled out from the
            // centre, so a point's column is where its ray from the centre crosses the edge row, and between two rings (5-14 m
            // apart out here) the surface runs straight from one ring's vertices to the next. On a ridged crest that chord lies
            // metres under the height function, and a silhouette set on the function stood on air (the pines above the Peaks'
            // exit); a crest between two columns sits under it too, so the vertices are read, not the function.
            float Drawn(float x, float z)
            {
                float ax = Mathf.Abs(x), az = Mathf.Abs(z), cheb = Mathf.Max(ax, az), o = cheb - Half; if (o <= 0) return MeshY(x, z);
                int side = az >= ax ? (z < 0 ? 0 : 2) : (x > 0 ? 1 : 3), seg = GroundSegments, cols = seg + 1;
                float kf = ((side == 0 || side == 2 ? x : z) * Half / cheb + Half) / Zone.size * seg; if (side >= 2) kf = seg - kf;   // the north and west rows run the other way
                int r = 0; while (r + 2 < rings.Length && rings[r + 1] < o) r++;
                int k = Mathf.Clamp((int)kf, 0, seg - 1); float u = Mathf.Clamp01(kf - k), s = Mathf.Clamp01((o - rings[r]) / (rings[r + 1] - rings[r]));
                var v = skirtV[side]; int a = r * cols + k, d = a + cols; float ha = v[a].y, hb = v[a + 1].y, hc = v[d + 1].y, hd = v[d].y;
                return u >= s ? ha + (hb - ha) * u + (hc - hb) * s : ha + (hd - ha) * s + (hc - hd) * u;   // the quad's two triangles, split from a to d+1
            }
            skirtY = Drawn;   // the grass and plants past the edge stand on it (EdgeGround)
            void Put(int side, Mesh mesh, Material mat, Vector3 at, Quaternion rot, Vector3 scale)
            {
                if (!parts.TryGetValue((mat, side), out var list)) parts[(mat, side)] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(at, rot, scale) });
            }
            float treeline = mountain ? 32 : ash ? 22 : 43;
            foreach (float row in new[] { 8f, 14, 22, 32, 43, 53, 64 })
            {
                bool rock = row > treeline; if (rock && !mountain && !ash) break;
                // keep: near rows leave the roads' way out open; further out the woods close across it (the road goes into cover).
                float edge = Half + row, step = rock ? 20 + row * .2f : (6 + row * .3f) * (ash ? 1.8f : 1), keep = rock ? 14 : row < 30 ? 9 : 0;
                for (int side = 0; side < 4; side++)
                    for (float a = -edge + R() * step; a < edge; a += step * (.7f + R() * .6f))
                    {
                        float o = edge + (R() - .5f) * 3;
                        var q = side == 0 ? new Vector2(a, -o) : side == 1 ? new Vector2(o, a) : side == 2 ? new Vector2(-a, o) : new Vector2(-o, -a);
                        if ((Zone.wasting != null && q.x > Zone.wasting.x - 6) || (keep > 0 && valleys.Exists(v => DistanceToPath(q, v) < keep))) continue;
                        var at = new Vector3(q.x, H(q.x, q.y) - 1, q.y); float yaw = R() * 360, th = 7 + R() * 5, pick = R() * (dead + pines + leafy);
                        // Set on the lowest drawn ground round it (1.2 m out, and at the trunk's own radius); no tree where that
                        // ground is near-vertical (a rise of over 2.2 m across it, or a 2 m step within 2.5 m: a crest or ledge). The
                        // trunk sinks half a metre, and further the steeper the ground, so the cone's base never shows.
                        // The draws are taken either way, so the rest stand where they did.
                        float lo = Drawn(q.x, q.y), hi = lo, far = 0;
                        for (int k = 0; k < 12; k++)
                        {
                            float ca = Mathf.Cos(k * .5236f), sa = Mathf.Sin(k * .5236f), y = Drawn(q.x + ca * (k % 2 == 0 ? 1.2f : .5f), q.y + sa * (k % 2 == 0 ? 1.2f : .5f));
                            lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y); if (k % 2 == 0) far = Mathf.Max(far, Mathf.Abs(Drawn(q.x + ca * 2.5f, q.y + sa * 2.5f) - y));
                        }
                        bool bare = !rock && (hi - lo > 2.2f || far > 2); at.y = lo - (rock ? 0 : .5f + (hi - lo) * .3f);
                        if (rock)
                        {
                            // A tor: tall and narrow, sunk deep enough that its downhill side never hangs over the slope.
                            float s = mountain ? 12 + R() * 14 : 5 + R() * 6;   // ash: low broken tors on the swells
                            Put(side, crag, rockMat, at + Vector3.down * s * .8f, Quaternion.Euler(0, yaw, 0), new Vector3(1.2f * s, 2.8f * s, 1.1f * s));
                        }
                        else if (pick < dead)
                        {
                            if (!bare) Put(side, spire, deadMat, at, Quaternion.identity, new Vector3(.45f, th, .45f));
                            for (int j = 0; j < 3; j++) { float tilt = 35 + R() * 20; if (!bare) Put(side, spire, deadMat, at + Vector3.up * th * (.45f + j * .14f), Quaternion.Euler(0, yaw + j * 120, tilt), new Vector3(.18f, th * .4f, .18f)); }
                        }
                        else if (bare) { if (pick >= dead + pines) R(); }   // (a broadleaf's leaf shade draw)
                        else if (pick < dead + pines)
                        {
                            // A bark trunk under lifted boughs, so a pine on a slope stands on it rather than hovering on a flat cone base.
                            Put(side, spire, art.bark, at, Quaternion.identity, new Vector3(.4f, th * .5f, .4f));
                            if (cardArt)
                            {
                                // Three tiers of four drooping bough cards, widest at the foot, over the height the cones had, and two
                                // crossed upright fronds to the tip. The rows by the edge are seen from a few metres: five tiers of five
                                // there, from lower on the trunk, so they stand as pines beside the zone's own and not as three ruffs on a pole.
                                var set = CardsFor(boughMat, side); float turn = S() * 360; int tiers = row < 20 ? 5 : 3, boughs = row < 20 ? 5 : 4;
                                for (int i = 0; i < tiers; i++)
                                {
                                    float u = i / (tiers - 1f), y = 1 + th * Mathf.Lerp(row < 20 ? .08f : .2f, .62f, u), rad = th * Mathf.Lerp(.3f, .14f, u); var heart = at + Vector3.up * (y - rad * .5f);
                                    var tier = Color.Lerp(new Color(.6f, .68f, .6f), Color.white, u);
                                    for (int k = 0; k < boughs; k++)
                                    {
                                        float droop = (22 + S() * 12) * Mathf.Deg2Rad; var outward = Quaternion.Euler(0, turn + i * 45 + k * (360f / boughs) + (S() - .5f) * 20, 0) * Vector3.right;
                                        var along = outward * Mathf.Cos(droop) - Vector3.up * Mathf.Sin(droop);
                                        set.Add(at + Vector3.up * (y + th * .05f), along, Vector3.Cross(along, Vector3.up).normalized, rad * 1.1f, rad * 1.6f, heart, .35f, tier, 0, 1, tier * .6f);
                                    }
                                }
                                float ty = 1 + th * .68f, tl = th * .4f, lead = S() * 180; var tip = at + Vector3.up * (ty - tl * .5f);
                                set.Add(at + Vector3.up * ty, Vector3.up, Quaternion.Euler(0, lead, 0) * Vector3.right, tl, tl * .36f, tip, .5f, Color.white, 0, 1, new Color(.68f, .7f, .68f));
                                set.Add(at + Vector3.up * ty, Vector3.up, Quaternion.Euler(0, lead + 90, 0) * Vector3.right, tl * .95f, tl * .34f, tip, .5f, new Color(.92f, .92f, .9f), 0, 1, new Color(.68f, .7f, .68f));
                            }
                            else
                            {
                                Put(side, spire, pineMat, at + Vector3.up * (1 + th * .16f), Quaternion.Euler(0, yaw, 0), new Vector3(th * .26f, th * .6f, th * .26f));
                                Put(side, spire, pineMat, at + Vector3.up * (1 + th * .52f), Quaternion.Euler(0, yaw + 30, 0), new Vector3(th * .18f, th * .56f, th * .18f));
                            }
                        }
                        else
                        {
                            float c = th * .45f; int fam = (int)(R() * 3);   // its leaf shade: the same draw as before
                            Put(side, spire, art.bark, at, Quaternion.identity, new Vector3(.35f, th * .62f, .35f));
                            if (cardArt)
                            {
                                // Three leaf-cluster cards crossed at sixty degrees about a tilted axis through the crown's middle, the
                                // size the blobs were, so from any side one card faces the eye and the crown's edge is ragged leaf.
                                var set = CardsFor(LeafMaterial(fam), side); var centre = at + Vector3.up * th * .62f;
                                var axis = Quaternion.Euler(0, S() * 360, 0) * (Quaternion.Euler(15 + S() * 15, 0, 0) * Vector3.up);
                                var across = Quaternion.AngleAxis(S() * 60, axis) * Vector3.Cross(axis, Vector3.up).normalized;
                                float len = c * 1.2f, wide = c * 1.1f; var foot = centre - axis * len * .5f;
                                // Inside them a leaf-coloured mass, two lumps in the family's mid and deep: a crown with a ragged edge, not cards on a stick.
                                var mass = LeafMassOf(fam);
                                Put(side, crown, mass[1], centre, Quaternion.Euler(0, yaw, 0), new Vector3(c * .8f, c * .6f, c * .8f));
                                Put(side, crown, mass[0], centre + new Vector3(c * .22f, -c * .2f, c * .15f), Quaternion.Euler(0, yaw + 90, 0), new Vector3(c * .62f, c * .46f, c * .62f));
                                for (int k = 0; k < 3; k++)
                                    set.Add(foot, axis, Quaternion.AngleAxis(k * 60, axis) * across, len * (k == 0 ? 1 : .95f), wide * (k == 0 ? 1 : .92f), centre, .3f, k == 0 ? new Color(1, 1, .95f) : new Color(.88f, .9f, .85f));
                            }
                            else
                            {
                                var leaf = Tint(art.foliage, Wither(Leaf[fam]));
                                Put(side, crown, leaf, at + Vector3.up * th * .62f, Quaternion.Euler(0, yaw, 0), new Vector3(c, c * .85f, c));
                                Put(side, crown, leaf, at + new Vector3(c * .3f, th * .5f, c * .2f), Quaternion.Euler(0, yaw + 90, 0), new Vector3(c * .7f, c * .6f, c * .7f));
                            }
                        }
                    }
            }
            // The near wood: trees and boulders scattered from 3 to 21 m past the line, most of them close to it, in the zone's
            // own mix, so at an exit the wood carries on between the silhouettes' first rows and thins out up the slope. A
            // broadleaf has a body (three lumps in its leaf's deep, mid and light) under eight leaf clusters, five round its
            // waist and three over the top; a pine five tiers of five boughs; dead wood five limbs. Each stands on the lowest
            // drawn ground round it, none on a face, on a road's way out or toward the Wasting. A stream of its own: the
            // silhouettes above stand where they did.
            var near = new System.Random(Zone.seed + 4545); float N() { return (float)near.NextDouble(); }
            var nearRock = RockTint(ash ? new Color(.33f, .32f, .31f) : mountain ? MountainStone : new Color(.42f, .41f, .39f));   // the zone's own boulders' stone
            float rocky = mountain ? .35f : ash ? .5f : .08f;
            for (int i = 0, n = Mathf.RoundToInt(4 * (Zone.size + 24) / (ash ? 9 : 4.2f)); i < n; i++)
            {
                int side = Mathf.Min(3, (int)(N() * 4)); float a = (N() * 2 - 1) * (Half + 12), o = Half + 3 + 18 * Mathf.Pow(N(), 1.5f), yaw = N() * 360, th = 6.5f + N() * 5, kind = N(), pick = N() * (dead + pines + leafy);
                var q = side == 0 ? new Vector2(a, -o) : side == 1 ? new Vector2(o, a) : side == 2 ? new Vector2(-a, o) : new Vector2(-o, -a);
                if ((Zone.wasting != null && q.x > Zone.wasting.x - 6) || valleys.Exists(v => DistanceToPath(q, v) < 8)) continue;
                float lo = Drawn(q.x, q.y), hi = lo;
                for (int k = 0; k < 6; k++) { float y = Drawn(q.x + Mathf.Cos(k * 1.0472f) * 1.1f, q.y + Mathf.Sin(k * 1.0472f) * 1.1f); lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y); }
                if (hi - lo > 2) continue;   // a face: nothing stands on it
                var at = new Vector3(q.x, lo - .3f - (hi - lo) * .3f, q.y);
                if (kind < rocky)
                {
                    // A boulder, half sunk, and a smaller one against it.
                    float s = .6f + N() * 1.2f;
                    Put(side, crag, nearRock, at + Vector3.up * .3f * s, Quaternion.Euler(0, yaw, 0), new Vector3(2.1f * s, 1.5f * s, 1.8f * s));
                    Put(side, crag, nearRock, at + Quaternion.Euler(0, yaw, 0) * new Vector3(.8f * s, .15f * s, .45f * s), Quaternion.Euler(0, yaw + 140, 0), new Vector3(1.2f * s, .9f * s, 1.1f * s));
                }
                else if (pick < dead)
                {
                    Put(side, spire, deadMat, at, Quaternion.identity, new Vector3(.5f, th, .5f));
                    for (int j = 0; j < 5; j++) Put(side, spire, deadMat, at + Vector3.up * th * (.36f + j * .1f), Quaternion.Euler(0, yaw + j * 137, 32 + N() * 24), new Vector3(.19f, th * (.42f - j * .04f), .19f));
                }
                else if (pick < dead + pines)
                {
                    Put(side, spire, art.bark, at, Quaternion.identity, new Vector3(.45f, th * .55f, .45f));
                    if (cardArt)
                    {
                        var set = CardsFor(boughMat, side); float turn = N() * 360;
                        for (int j = 0; j < 5; j++)
                        {
                            float u = j / 4f, y = 1 + th * Mathf.Lerp(.08f, .62f, u), rad = th * Mathf.Lerp(.3f, .14f, u); var heart = at + Vector3.up * (y - rad * .5f);
                            var tier = Color.Lerp(new Color(.6f, .68f, .6f), Color.white, u);
                            for (int k = 0; k < 5; k++)
                            {
                                float droop = (22 + N() * 12) * Mathf.Deg2Rad; var outward = Quaternion.Euler(0, turn + j * 36 + k * 72 + (N() - .5f) * 20, 0) * Vector3.right;
                                var along = outward * Mathf.Cos(droop) - Vector3.up * Mathf.Sin(droop);
                                set.Add(at + Vector3.up * (y + th * .05f), along, Vector3.Cross(along, Vector3.up).normalized, rad * 1.1f, rad * 1.6f, heart, .35f, tier, 0, 1, tier * .6f);
                            }
                        }
                        float ty = 1 + th * .68f, tl = th * .4f, lead = N() * 180; var tip = at + Vector3.up * (ty - tl * .5f);
                        set.Add(at + Vector3.up * ty, Vector3.up, Quaternion.Euler(0, lead, 0) * Vector3.right, tl, tl * .36f, tip, .5f, Color.white, 0, 1, new Color(.68f, .7f, .68f));
                        set.Add(at + Vector3.up * ty, Vector3.up, Quaternion.Euler(0, lead + 90, 0) * Vector3.right, tl * .95f, tl * .34f, tip, .5f, new Color(.92f, .92f, .9f), 0, 1, new Color(.68f, .7f, .68f));
                    }
                    else
                    {
                        Put(side, spire, pineMat, at + Vector3.up * (1 + th * .16f), Quaternion.Euler(0, yaw, 0), new Vector3(th * .26f, th * .6f, th * .26f));
                        Put(side, spire, pineMat, at + Vector3.up * (1 + th * .52f), Quaternion.Euler(0, yaw + 30, 0), new Vector3(th * .18f, th * .56f, th * .18f));
                    }
                }
                else
                {
                    int fam = Mathf.Min(2, (int)(N() * 3)); float c = th * .46f; var centre = at + Vector3.up * th * .66f; var turned = Quaternion.Euler(0, yaw, 0);
                    Put(side, spire, art.bark, at, Quaternion.identity, new Vector3(.5f, th * .7f, .5f));
                    Put(side, crown, Tint(art.foliage, Wither(NearLeaf[fam * 3 + 1])), centre, turned, new Vector3(c, c * .72f, c));
                    Put(side, crown, Tint(art.foliage, Wither(NearLeaf[fam * 3])), centre + turned * new Vector3(c * .3f, -c * .22f, c * .1f), Quaternion.Euler(0, yaw + 90, 0), new Vector3(c * .78f, c * .52f, c * .78f));
                    Put(side, crown, Tint(art.foliage, Wither(NearLeaf[fam * 3 + 2])), centre + turned * new Vector3(-c * .2f, c * .26f, -c * .12f), Quaternion.Euler(0, yaw + 200, 0), new Vector3(c * .62f, c * .46f, c * .62f));
                    if (!cardArt) continue;
                    var set = CardsFor(LeafMaterial(fam), side);
                    for (int k = 0; k < 8; k++)
                    {
                        bool top = k >= 5; float round = yaw + (top ? (k - 5) * 120 + 40 : k * 72) + (N() - .5f) * 30, up = top ? 52 + N() * 22 : -6 + N() * 30, size = c * (.54f + N() * .14f);
                        var dir = Quaternion.Euler(0, round, 0) * (Quaternion.Euler(0, 0, up) * Vector3.right);
                        set.AddCross(centre + Vector3.Scale(dir, new Vector3(c * .3f, c * .2f, c * .3f)), dir, Vector3.Cross(dir, Vector3.up).normalized, size, size * .95f, centre, .3f, top ? new Color(1, 1, .95f) : new Color(.86f, .9f, .84f), 3);
                    }
                }
            }
            foreach (var kv in parts)
            {
                var mesh = new Mesh { name = "Backdrop " + kv.Key.Item1.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true); Show(mesh, kv.Key.Item1);
            }
            foreach (var kv in cardSets) Show(kv.Value.Build("Backdrop " + kv.Key.Item1.name + " cards"), kv.Key.Item1);
        }
    }

    /// <summary>Slowly scrolls water texture coordinates.</summary>
    public sealed class WaterDrift : MonoBehaviour
    {
        Material m; void Start() { m = GetComponent<Renderer>().material; }
        void Update() { if (m != null) m.mainTextureOffset = new Vector2(0, Time.time * .04f); }
    }

    /// <summary>
    /// Haze over the unmade: unlit, in the fog's own colour hour by hour, so what lies behind it greys into haze instead of
    /// sitting behind a lit pane (and at night it is dark haze, never a glowing wall). The curtain is pulled toward neutral grey
    /// and regenerates a small noise texture so it reads as crawling grey static; the haze over the unmade and the bank are plain.
    /// </summary>
    public sealed class WastingStatic : MonoBehaviour
    {
        Material m; Texture2D tex; Color32[] px; float next, grey, gain; bool fizz; System.Random rng = new System.Random(7);
        /// <summary>Set up at once (the zone map is rendered before Start). The renderer's material must be its own.</summary>
        public void Init(bool noise, float toGrey, float brightness)
        {
            fizz = noise; grey = toGrey; gain = brightness; m = GetComponent<Renderer>().sharedMaterial;
            if (fizz)
            {
                tex = new Texture2D(128, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat, name = "Wasting static" };
                tex.wrapModeV = TextureWrapMode.Clamp;
                px = new Color32[128 * 64]; m.mainTexture = tex; m.mainTextureScale = new Vector2(16.5f, 1);   // 14 repeats over the old 1.1x span, kept per metre on the 1.3x one
                Fill();
            }
            else m.mainTexture = Texture2D.whiteTexture;
            Tint();
        }
        void LateUpdate()
        {
            if (m == null) return;
            Tint();   // after the clock has set this frame's fog
            if (fizz && Time.time >= next) { next = Time.time + .11f; Fill(); }
        }
        void Tint()
        {
            // The fog's colour (particle shaders double their tint), pulled toward grey and a touch brighter. The curtain never goes
            // below the fog in any channel (its static only lightens), and thins at night, when the sky above the horizon is
            // brighter than the fog and any darker streak smeared across it like a squall.
            var f = RenderSettings.fogColor; float l = f.grayscale;
            var c = Color.Lerp(f, new Color(l, l, l * 1.03f), grey);
            if (fizz) c = new Color(Mathf.Max(c.r, f.r), Mathf.Max(c.g, f.g), Mathf.Max(c.b, f.b));
            c *= .5f * gain; c.a = fizz ? .5f * (1 - .5f * WorldClock.Darkness) : .5f; m.SetColor("_TintColor", c);
        }
        void Fill()
        {
            // Crawling grey static: thin at the ground (the land behind fades in with distance, no ruled bottom edge), thickest a
            // few metres up, dissolving upward into haze. Soft slanted bands crawl sideways through it, not rain-like streaks.
            // Texels are 214-255: with the tint's gain (1.2) none is darker than the fog. By night the bands and the static's
            // contrast flatten into an even veil.
            float t = Time.time, calm = WorldClock.Darkness;
            for (int y = 0; y < 64; y++)
            {
                float h = y / 63f, body = Mathf.SmoothStep(0, 1, h / .3f) * (1 - Mathf.SmoothStep(0, 1, (h - .3f) / .7f));
                for (int x = 0; x < 128; x++)
                {
                    int v = rng.Next(214, 256);
                    float u = x * Mathf.PI / 64, band = .5f + .3f * Mathf.Sin(u * 3 + t * .25f + y * .19f) + .2f * Mathf.Sin(u * 5 - t * .45f + y * .31f + 1.7f);
                    band = Mathf.Lerp(band, .5f, calm * .9f);
                    byte a = (byte)Mathf.Clamp(Mathf.Lerp(rng.Next(40, 140), 90, calm * .7f) * body * (.4f + band * .9f), 0, 255);
                    px[y * 128 + x] = new Color32((byte)v, (byte)v, (byte)Mathf.Min(255, v + 4), a);
                }
            }
            tex.SetPixels32(px); tex.Apply(false);
        }
    }
}
