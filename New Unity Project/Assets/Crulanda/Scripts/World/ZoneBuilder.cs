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
    public sealed class ZoneBuilder : MonoBehaviour
    {
        public TextAsset zoneJson;
        [Tooltip("Every zone this scene can build; travel picks one by id. zoneJson is the default/opening zone.")]
        public TextAsset[] zones = new TextAsset[0];
        public ZoneArt art;
        /// <summary>Set before reloading the scene to build a different zone (zone travel).</summary>
        public static string RequestedZoneId;
        public static ZoneBuilder Active { get; private set; }
        public bool HasZone(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (var z in zones) if (z != null && JsonUtility.FromJson<ZoneDefinition>(z.text)?.id == id) return true;
            return false;
        }
        public ZoneDefinition FindZone(string id)
        {
            foreach (var z in zones) { var d = z == null ? null : ParseZone(z.text); if (d != null && d.id == id) return d; }
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
            Zone = (RequestedZoneId != null ? FindZone(RequestedZoneId) : null) ?? ParseZone(zoneJson != null ? zoneJson.text : "{}");
            RequestedZoneId = null;
            if (Zone == null || string.IsNullOrEmpty(Zone.id) || art == null) { Debug.LogError("Zone definition or art missing."); enabled = false; return; }
            Active = this;
            rng = new System.Random(Zone.seed);
            props = new GameObject("Zone props").transform; props.SetParent(transform, false);
            statics = new GameObject("Zone static scenery").transform; statics.SetParent(transform, false);
            PrepareRelief(); PrepareShapes(); Water = new ZoneWater(); Water.Prepare(Zone, (x, z) => HeightAt(x, z, false));
            BuildLighting(); BuildGround(); BuildWater(); BuildWasting(); BuildProps(); BuildGroves(); BuildExits(); BuildForestEdge(); BuildBoundaries(); BuildBackdrop();
            if (art.grass != null && art.grass.Length > 0)
            {
                // Gloom: the tufts are dry, greyed straw and no wildflowers bloom.
                var grass = Gloom ? art.grass.Select(m => new Material(m) { name = m.name + " (dry)", color = Color.Lerp(m.color, new Color(.5f, .46f, .37f), .75f), enableInstancing = true }).ToArray() : art.grass;
                // Gloom: tall grass is dark grey dead stalks, a little shorter and thinner, so a boar or wolf breaking out of it shows.
                var tall = Gloom ? art.grass.Select(m => new Material(m) { name = m.name + " (dead, tall)", color = Color.Lerp(m.color, new Color(.36f, .35f, .34f), .9f), enableInstancing = true }).ToArray() : null;
                gameObject.AddComponent<GrassField>().Build(this, grass, Gloom ? null : art.flowers, Openness, Zone.seed + 99, Zone.biome == "meadow" ? 2.3f : 1.6f, TallGrassPatches(), tall, Gloom ? .8f : 1, Gloom ? .7f : 1);
            }
            gameObject.AddComponent<FallingLeaves>().Init(this);
            Splashes.Ensure(this); TreeFade.Begin(art.fade);
            var view = Camera.main;
            if (view != null && art.post != null && view.GetComponent<ZonePost>() == null) view.gameObject.AddComponent<ZonePost>().Init(art.post, Zone, sunLight);
            try { StaticBatchingUtility.Combine(statics.gameObject); } catch (Exception e) { Debug.LogWarning("Static batching skipped: " + e.Message); }
            MapTexture = RenderMap(1024);   // in daylight, before the clock sets the hour
            gameObject.AddComponent<WorldWeather>().Init(this, sunLight);   // before the clock: its first light already has the weather in it
            var clock = gameObject.AddComponent<WorldClock>(); clock.Init(sunLight, Zone.lighting, art.skybox, NightLights);
            // Reflections follow the real sky: a sky-only realtime probe covering the zone, refreshed by the clock.
            var probe = new GameObject("Sky reflection").AddComponent<ReflectionProbe>(); probe.transform.SetParent(transform, false);
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime; probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing; probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
            probe.cullingMask = 0; probe.resolution = 64; probe.hdr = true; probe.size = new Vector3(Zone.size + 40, 400, Zone.size + 40); probe.importance = 0;
            clock.Reflections = probe;
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
        static float ShapeCover(ZoneShape s, Vector2 p)
        {
            float blend = Mathf.Max(.5f, s.blend), fade = blend * .6f + 1, reach = s.radius + fade;
            if ((p - s.center).sqrMagnitude >= reach * reach) return 0;   // most of the zone: a cheap reject (PaintGround asks per pixel)
            return 1 - Mathf.SmoothStep(0, 1, (Vector2.Distance(p, s.center) - s.radius) / fade);
        }
        public Vector3 Ground(Vector2 p, float lift = 0) { return new Vector3(p.x, HeightAt(p.x, p.y) + lift, p.y); }
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
            if (cam != null) { cam.clearFlags = art.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor; cam.depthTextureMode |= DepthTextureMode.Depth; cam.farClipPlane = Mathf.Max(cam.farClipPlane, (Half + BackdropWidth) * 2.9f); }
        }

        // ---------- ground ----------
        void BuildGround()
        {
            var go = new GameObject("Ground"); go.transform.SetParent(transform, false); go.layer = 0;
            var mesh = ZoneMeshes.Ground(Zone.size, GroundSegments, HeightAt);
            var normals = mesh.normals; groundUp = new float[normals.Length]; for (int i = 0; i < normals.Length; i++) groundUp[i] = normals[i].y;
            var verts = mesh.vertices; groundN = normals; groundY = new float[verts.Length]; for (int i = 0; i < verts.Length; i++) groundY[i] = verts[i].y;
            GroundMesh = go.AddComponent<MeshFilter>(); GroundMesh.sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); var m = new Material(art.ground) { name = "Painted ground" };
            m.SetTextureScale("_DetailAlbedoMap", Vector2.one * Zone.size / 1.8f);   // ~1.8 m grain repeat, whatever the zone size
            if (Zone.biome == "ash")
            {
                m.SetTexture("_DetailAlbedoMap", AshDetail(Zone.seed)); m.SetTextureScale("_DetailAlbedoMap", Vector2.one * Zone.size / 5);   // fine crazing, sharp up close
                m.SetTexture("_DetailMask", DetailMask(256, (x, z) => 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, .95f, Unmade(x, z)))));   // none on the unmade
            }
            if (Zone.biome == "mountain") m.SetTexture("_DetailMask", DetailMask(4, (x, z) => .5f));   // half grain: in hard alpine light it was a harsh speckle
            m.mainTexture = PaintGround(Mathf.Clamp(Mathf.RoundToInt(Zone.size * 8 / 256) * 256, 1024, 2048)); r.sharedMaterial = m; GroundMaterial = m;
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
            var toSun = Quaternion.Euler(55, Zone.lighting.sunYaw, 0) * Vector3.back;   // the day sun, roughly (mountain counter-shading)
            float size = Zone.size, half = size / 2, texel = size / res;
            // Ash crust: petrified-ash plates split by angular cracks, a Voronoi network with one jittered site per 5 m cell
            // (sx/sz in cell units, three cells of margin), plus each site's 8 bisectors with its neighbours (unit normal bx/bz,
            // offset bo, neighbour bn), so a pixel's distance to its nearest crack is 8 dot products.
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
            for (int j = 0; j < res; j++)
                for (int i = 0; i < res; i++)
                {
                    float x = -half + size * (i + .5f) / res, z = -half + size * (j + .5f) / res; var p = new Vector2(x, z);
                    float n1 = Mathf.PerlinNoise(x * .08f + 11, z * .08f + 7), n2 = Mathf.PerlinNoise(x * .45f, z * .45f), n3 = Mathf.PerlinNoise(x * 1.9f + 3, z * 1.9f + 5);
                    Color c = Color.Lerp(Color.Lerp(grassA, grassB, n1), grassC, Mathf.Clamp01((Mathf.PerlinNoise(x * .03f + 40, z * .03f) - .55f) * 3));
                    c *= .88f + n2 * .18f + (n3 - .5f) * .08f;
                    if (Zone.biome == "mountain")
                    {
                        // Alpine: patches of thin dry turf between warm scree and grey rock. Rock takes the steep ground (crags,
                        // scarps, gorge walls) and the high edges, scree the rocky flats. Mid-dark values, so it never reads as snow.
                        // Rock and scree are warm and a shade darker, and counter-shaded toward the day sun (faces turned to it a
                        // little darker, faces turned away lifted), so a sunlit crag stays well under the fog's value and the relief
                        // reads as a gradient, not near-white against near-black. Close-up grain (n3) is kept soft.
                        float steep = Mathf.Clamp01((1 - UpAt(x, z) - .06f) * 6);
                        float rock = Mathf.Clamp01(MountainRock(x, z) + steep + (n3 - .5f) * .3f);
                        var nrm = NormalAt(x, z); float turn = nrm.x * toSun.x + nrm.z * toSun.z, shade = 1 - Mathf.Clamp(turn, -.6f, .6f) * (turn < 0 ? .5f : .22f);   // horizontal turn toward the midday sun (55 deg, an average)
                        // A sixth darker again than step 2's (sunlit crags still read ~140 of 255 in the tour; the aim is 110-130).
                        Color rockC = Color.Lerp(new Color(.195f, .186f, .178f), new Color(.278f, .262f, .245f), n2) * shade;
                        Color scree = Color.Lerp(new Color(.288f, .262f, .22f), new Color(.346f, .312f, .262f), (n2 + n3) * .5f) * shade;
                        Color alp = Color.Lerp(new Color(.27f, .31f, .18f), new Color(.38f, .36f, .22f), n1);
                        c = Color.Lerp(alp, Color.Lerp(scree, rockC, Mathf.Clamp01(steep * 1.4f + (n1 - .5f) * .8f)), rock) * (.91f + n3 * .12f);
                    }
                    else if (ashen)
                    {
                        // Ashland: pale grey petrified ash in plates a shade apart, split by angular cracks (Voronoi cell edges, some
                        // left faint so the network looks broken, not paved), and the odd copper-rust stain (canon: the dust tastes of
                        // copper). The edges bend a little. The paint is magnified up close, where a texel-wide line blurs into a soft
                        // dark band: so these lines are thin and faint, and the crisp crazing detail carries the cracks near the camera.
                        float u = (x + half) / plate + 3 + (n2 - .5f) * .1f + (n3 - .5f) * .03f;
                        float v = (z + half) / plate + 3 + (Mathf.PerlinNoise(x * .45f + 31, z * .45f + 17) - .5f) * .1f + (n3 - .5f) * .03f;
                        int cx = (int)u, cy = (int)v, near = 0, other = 0; float best = 99, edge = 99;
                        for (int b = cy - 1; b <= cy + 1; b++) for (int a = cx - 1; a <= cx + 1; a++)
                        { int k = b * cells + a; float dx = sx[k] - u, dz = sz[k] - v; if (dx * dx + dz * dz < best) { best = dx * dx + dz * dz; near = k; } }
                        for (int m = near * 8; m < near * 8 + 8; m++) { float e = bo[m] - u * bx[m] - v * bz[m]; if (e < edge) { edge = e; other = bn[m]; } }
                        uint pair = (uint)(Mathf.Min(near, other) * 7919 + Mathf.Max(near, other)) * 2654435761u;
                        float line = Mathf.Clamp01((.03f + .04f * n1 - edge * plate) / texel + .5f) * (pair >> 24 < 64 ? .35f : 1);
                        Color ashC = Color.Lerp(new Color(.46f, .46f, .47f), new Color(.60f, .59f, .59f), n1) * (.97f + .06f * Mathf.Repeat(sx[near] * 7.31f + sz[near] * 3.17f, 1));
                        ashC = Color.Lerp(ashC, new Color(.52f, .44f, .40f), Mathf.Clamp01((Mathf.PerlinNoise(x * .02f + 60, z * .02f) - .66f) * 2.2f));
                        c = Color.Lerp(ashC, new Color(.17f, .165f, .165f), line * .24f) * (.93f + n3 * .1f);
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
                            else sc = Color.Lerp(new Color(.25f, .2f, .15f), new Color(.12f, .095f, .075f), Mathf.Clamp01((n1 - .4f) * 2.5f + (n3 - .5f) * .4f));   // mud
                            c = Color.Lerp(c, sc, Mathf.Clamp01(cover * (.85f + n2 * .3f)));
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
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }
        /// <summary>
        /// Ash ground detail (x2 over the paint, 5 m repeat): fine angular crazing, the edges of a tileable Voronoi of 8x8
        /// plates (~.6 m), some left faint, over powdery grain; averages mid-grey. Sharp up close, where the paint blurs.
        /// </summary>
        static Texture2D AshDetail(int seed)
        {
            const int n = 512, cells = 8; float per = (float)n / cells; var rnd = new System.Random(seed + 31);
            var sx = new float[cells * cells]; var sz = new float[sx.Length]; var tone = new float[sx.Length];
            for (int k = 0; k < sx.Length; k++) { sx[k] = .15f + .7f * (float)rnd.NextDouble(); sz[k] = .15f + .7f * (float)rnd.NextDouble(); tone[k] = (float)rnd.NextDouble() - .5f; }
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
                    float g = .52f + tone[near] * .04f + ((h & 1023) / 1023f - .5f) * .07f;
                    g = Mathf.Lerp(g, .34f, Mathf.Clamp01(1.7f - edge * per) * (pair >> 24 < 80 ? .35f : 1));   // ~2.5 px (2.5 cm) line
                    byte c = (byte)(Mathf.Clamp01(g) * 255); px[y * n + x] = new Color32(c, c, c, 255);
                }
            var tex = new Texture2D(n, n, TextureFormat.RGB24, true) { name = "Ash crazing", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            tex.SetPixels32(px); tex.Apply(true, true); return tex;
        }
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
        Material Tint(Material baseMat, Color color)
        {
            string key = baseMat.name + ColorUtility.ToHtmlStringRGB(color);
            if (!tints.TryGetValue(key, out var m)) { m = new Material(baseMat) { color = color, name = key }; tints[key] = m; }
            return m;
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
                var parent = p.kind == "bridge" || p.kind == "inn" || p.kind == "mill" || p.kind == "coop" || p.kind == "herb" || !string.IsNullOrEmpty(p.interact) ? props : statics;
                var t = Root(p, parent);
                // A ruin never stands in a building: moved clear here, or (a plain one) built for its draws and then left out.
                bool drop = (p.kind == "ruin" || p.kind == "ruined_house") && !ClearOfBuildings(p, t);
                if (p.kind == "wall") WarnWallInBuilding(p);
                // New landmark kinds draw from their own stream, after taking the draws the kind that stood here took, so
                // every later prop, tree and rock keeps the layout it had.
                var zoneRng = rng; int legacy = p.kind == "cave" ? 30 : p.kind == "rib" ? 6 : p.kind == "perch" || p.kind == "wallow" || p.kind == "brood" ? 0 : -1;
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
                    case "wagon": Wagon(t); break;
                    case "herb": Herb(t, p.variant); break;
                    case "ruined_house": RuinedHouse(t, p.size.x > 0 ? p.size : new Vector2(8, 6)); break;
                    case "wall": Wall(p.points, statics); DestroyImmediate(t.gameObject); break;
                    case "tower": Tower(t, p.size.x > 0 ? p.size.x : 4.4f); break;
                    case "gallows": Gallows(t); break;
                    case "crypt": Crypt(t, p.variant); break;
                    case "cliff": Cliff(t, p.size.x > 0 ? p.size.x : 20, p.lift); break;
                    case "dead_oak": DeadTree(p.at, 1, Tint(art.bark, new Color(.4f, .36f, .32f)), 5, statics, t); break;   // weathered, paler bark (plain bark reads black in shade); size comes from the root's scale
                    case "great_oak": GreatOak(t); break;
                    case "tree": Broadleaf(t, p.variant); break;
                    case "pine": Pine(t); break;
                    case "fence": Fence(t, p.size.x > 0 ? p.size.x : 8); break;
                    case "hedge": Part(PrimitiveType.Cube, t, new Vector3(0, .7f, 0), new Vector3(p.size.x > 0 ? p.size.x : 6, 1.4f, 1.2f), Tint(art.foliage, new Color(.22f, .30f, .16f))); Solid(t, new Vector3(0, .7f, 0), new Vector3(p.size.x > 0 ? p.size.x : 6, 1.4f, 1.2f)); break;
                    case "haystack": Part(PrimitiveType.Sphere, t, new Vector3(0, .9f, 0), new Vector3(2.6f, 2.2f, 2.6f), art.hay); Solid(t, new Vector3(0, 1, 0), new Vector3(2.4f, 2, 2.4f)); break;
                    case "cart": Cart(t); break;
                    case "barrels": for (int i = 0; i <= p.variant % 3; i++) Part(PrimitiveType.Cylinder, t, new Vector3(i * .75f - .4f, .5f, (i % 2) * .6f), new Vector3(.7f, .5f, .7f), art.timber); Solid(t, new Vector3(0, .5f, .3f), new Vector3(2.2f, 1, 1.4f)); break;
                    case "crates": for (int i = 0; i <= p.variant % 3; i++) Part(PrimitiveType.Cube, t, new Vector3(i == 2 ? -.05f : i * .9f - .5f, i == 2 ? 1.35f : .45f, 0), Vector3.one * .9f, Tint(art.timber, new Color(.45f, .33f, .21f))); Solid(t, new Vector3(0, .5f, 0), new Vector3(2.2f, 1, 1)); break;   // the third crate rests across the two below, not on air
                    case "lamp": Lamp(t, p.variant); break;
                    case "grave": { float a = R01 * 8 - 4, b = R01 * 8 - 4; if (p.variant == 1) { a = Mathf.Sign(a) * (14 + Mathf.Abs(a) * 2); b *= 2; } Part(PrimitiveType.Cube, t, new Vector3(0, p.variant == 1 ? .36f : .46f, 0), new Vector3(.6f, 1, .15f), art.stone, Quaternion.Euler(a, 0, b)); break; }   // variant 1: heaved over and half sunk (the drowned graves)
                    case "rock":
                    {
                        float s = 1 + p.variant * .6f; var stone = art.stone;
                        // Mountains: a loose rock left on a face the crags raised rolls to flatter ground near by and sinks by the slope.
                        if (Zone.biome == "mountain") { stone = Tint(art.stone, MountainStone); if (string.IsNullOrEmpty(p.interact)) { var spot = FlatterSpot(p.at, 1.2f * s); t.position = Ground(spot); SinkBySlope(t, spot, s); } }
                        Lump(Boulder(), t, new Vector3(0, .3f * s, 0), new Vector3(2f * s, 1.3f * s, 1.7f * s), stone, R01 * 360); if (s > 1.3f) Lump(Boulder(), t, new Vector3(.7f * s, .15f * s, .5f * s), new Vector3(.9f * s, .6f * s, .8f * s), stone, R01 * 360); Solid(t, new Vector3(0, .5f * s, 0), new Vector3(1.6f * s, 1f * s, 1.4f * s)); break;
                    }
                    case "bridge": SeatBridge(t, p.size.x > 0 ? p.size.x : 12); Bridge(t, p.size.x > 0 ? p.size.x : 12); break;
                    case "signpost": Part(PrimitiveType.Cube, t, new Vector3(0, 1.1f, 0), new Vector3(.15f, 2.2f, .15f), art.timber); Part(PrimitiveType.Cube, t, new Vector3(.45f, 1.8f, 0), new Vector3(.9f, .28f, .06f), art.timber); break;
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
                    default: Debug.LogWarning("Unknown zone prop kind '" + p.kind + "'."); break;
                }
                rng = zoneRng;
                if (drop) { DestroyImmediate(t.gameObject); continue; }
                if (!string.IsNullOrEmpty(p.interact) && t != null)
                    Interactables.Add(new ZoneInteractable { name = string.IsNullOrEmpty(p.name) ? p.kind : p.name, prompt = p.interact, item = p.item, kind = p.kind, once = p.once, position = t.position, root = t });
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
                default: return Vector2.zero;
            }
        }
        /// <summary>Whether a point stands under a building's roof (its footprint, eaves included): no rain or snow falls there.</summary>
        public bool UnderRoof(Vector2 p)
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
        static readonly Color[] Plaster = { new Color(.78f, .72f, .60f), new Color(.70f, .66f, .58f), new Color(.74f, .64f, .52f) };
        void House(Transform t, Vector2 size, float wallHeight, int variant, bool inn)
        {
            float w = size.x, d = size.y;
            var plaster = Tint(art.plaster, Plaster[Mathf.Abs(variant) % Plaster.Length]);
            var roofMat = variant % 2 == 0 ? art.thatch : art.slate;
            float drop = FootDrop(t, w + .3f, d + .3f);   // on a slope the plinth reaches down to the lowest ground under it
            Part(PrimitiveType.Cube, t, new Vector3(0, .3f - drop / 2, 0), new Vector3(w + .3f, .6f + drop, d + .3f), art.stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, .6f + wallHeight / 2, 0), new Vector3(w, wallHeight, d), plaster);
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
            // Door faces south (-Z). It stands on the ground in a timber frame in front of the plinth (which stops at the doorway)
            // instead of hanging on the wall above it. Warm windows either side and on the back.
            float foot = Mathf.Min(0, LocalGround(t, 0, -d / 2 - .19f)) - .05f;
            Part(PrimitiveType.Cube, t, new Vector3(0, (foot + 2.7f) / 2, -d / 2 - .19f), new Vector3(1.2f, 2.7f - foot, .06f), Tint(art.timber, new Color(.22f, .14f, .09f)));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * .68f, (foot + 2.86f) / 2, -d / 2 - .12f), new Vector3(.16f, 2.86f - foot, .24f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.78f, -d / 2 - .12f), new Vector3(1.52f, .16f, .24f), art.timber);
            if (!inn) Doors.Add(new ZoneDoor { name = t.name, openable = false, position = t.TransformPoint(new Vector3(0, 1, -d / 2 - .1f)) });
            for (float x = -w / 2 + 1.4f; x < w / 2 - .8f; x += 2.6f)
            {
                if (Mathf.Abs(x) < 1.2f) continue;
                foreach (int sz in new[] { -1, 1 })
                    Part(PrimitiveType.Cube, t, new Vector3(x, .6f + wallHeight * (inn ? .3f : .55f), sz * (d / 2 + .05f)), new Vector3(.8f, .7f, .08f), art.glass);
                if (inn) foreach (int sz in new[] { -1, 1 })
                    Part(PrimitiveType.Cube, t, new Vector3(x, .6f + wallHeight * .78f, sz * (d / 2 + .05f)), new Vector3(.8f, .7f, .08f), art.glass);
            }
            float roofH = Mathf.Max(2.2f, d * .55f);
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH), t, new Vector3(0, top, 0), roofMat);
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 - 1.1f, top + roofH * .75f, d * .15f), new Vector3(.9f, roofH * 1.1f, .9f), art.stone);
            if (art.particle != null) Smoke(t, new Vector3(w / 2 - 1.1f, top + roofH * 1.35f, d * .15f));
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
                var o = Part(PrimitiveType.Cube, t, pos, scale, plaster);
                o.AddComponent<BoxCollider>(); o.AddComponent<NavBlocker>(); return o;
            }
            // Floor, skirting and walls.
            Part(PrimitiveType.Cube, t, new Vector3(0, .02f, 0), new Vector3(w - wall, .05f, d - wall), boards);
            WallPiece(new Vector3(0, H / 2, d / 2), new Vector3(w, H, wall));
            foreach (int s in new[] { -1, 1 }) WallPiece(new Vector3(s * w / 2, H / 2, 0), new Vector3(wall, H, d));
            float side = (w - doorW) / 2;
            foreach (int s in new[] { -1, 1 }) WallPiece(new Vector3(s * (doorW / 2 + side / 2), H / 2, -d / 2), new Vector3(side, H, wall));
            WallPiece(new Vector3(0, doorH + (H - doorH) / 2, -d / 2), new Vector3(doorW, H - doorH, wall));
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, new Vector3(sx * w / 2, H / 2, sz * d / 2), new Vector3(.34f, H, .34f), art.timber);
            // Stone footing strips hug the outside of the walls only (the floor inside stays level with the ground).
            var footing = Tint(art.stone, new Color(.45f, .44f, .41f));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * (w / 2 + .12f), .15f, 0), new Vector3(.25f, .3f, d + .5f), footing);
            Part(PrimitiveType.Cube, t, new Vector3(0, .15f, d / 2 + .12f), new Vector3(w + .5f, .3f, .25f), footing);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * (doorW / 2 + side / 2 + .1f), .15f, -d / 2 - .12f), new Vector3(side + .3f, .3f, .25f), footing);
            // Storey line and braces outside; the floor above is a closed ceiling inside.
            foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(0, storey, sz * (d / 2 + .02f)), new Vector3(w, .22f, wall + .1f), art.timber);
            foreach (int sx in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .02f), storey, 0), new Vector3(wall + .1f, .22f, d), art.timber);
            // Ceiling: solid so the third-person camera pulls in under it instead of poking through.
            Part(PrimitiveType.Cube, t, new Vector3(0, storey - .1f, 0), new Vector3(w - wall, .15f, d - wall), Tint(art.timber, new Color(.3f, .22f, .15f))).AddComponent<BoxCollider>();
            for (float x = -w / 2 + 1.2f; x < w / 2; x += 1.5f) Part(PrimitiveType.Cube, t, new Vector3(x, storey - .25f, 0), new Vector3(.18f, .2f, d - wall), dark); // ceiling beams
            foreach (int sz in new[] { -1, 1 })
                for (float x = -w / 2 + 1.6f; x < w / 2 - 1; x += 2.8f)
                {
                    if (sz < 0 && Mathf.Abs(x) < doorW) continue;
                    Part(PrimitiveType.Cube, t, new Vector3(x, storey + (H - storey) * .5f, sz * (d / 2 + .03f)), new Vector3(.14f, (H - storey) * .8f, .1f), art.timber, Quaternion.Euler(0, 0, 32));
                }
            // Windows glow on both faces (the glass passes through the wall).
            foreach (int sz in new[] { -1, 1 })
                for (float x = -w / 2 + 1.5f; x < w / 2 - .8f; x += 2.6f)
                {
                    if (sz < 0 && Mathf.Abs(x) < doorW) continue;
                    Part(PrimitiveType.Cube, t, new Vector3(x, 1.5f, sz * d / 2), new Vector3(.9f, .8f, wall + .1f), art.glass);
                    Part(PrimitiveType.Cube, t, new Vector3(x, storey + 1.3f, sz * d / 2), new Vector3(.8f, .7f, wall + .1f), art.glass);
                }
            float roofH = Mathf.Max(2.4f, d * .5f);
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH), t, new Vector3(0, H, 0), variant % 2 == 0 ? art.thatch : art.slate);
            if (variant == 1) CrackedHearth(t, w, H, roofH);   // the Cracked Hearth: its chimney breast split, the fire showing through
            else Part(PrimitiveType.Cube, t, new Vector3(w / 2 - 1.1f, H + roofH * .75f, d * .15f), new Vector3(1, roofH * 1.1f, 1), art.stone);
            // The door: a plank door on a hinge at the left jamb, plus a stone step.
            var hinge = new GameObject("Door hinge").transform; hinge.SetParent(t, false); hinge.localPosition = new Vector3(-doorW / 2, 0, -d / 2 - .02f);
            var door = Part(PrimitiveType.Cube, hinge, new Vector3(doorW / 2, doorH / 2, 0), new Vector3(doorW, doorH, .1f), dark);
            var doorCollider = door.AddComponent<BoxCollider>();
            Part(PrimitiveType.Cube, t, new Vector3(0, .06f, -d / 2 - .55f), new Vector3(2, .12f, .8f), art.stone);
            var innDoor = new ZoneDoor { name = t.name, openable = true, hinge = hinge, blocker = doorCollider, position = t.TransformPoint(new Vector3(0, 1, -d / 2)) };
            Doors.Add(innDoor); innDoor.SetOpen(true);   // the inn keeps its door open; villagers come and go
            if (art.particle != null && variant != 1) Smoke(t, new Vector3(w / 2 - 1.1f, H + roofH * 1.4f, d * .15f));
            // Inside: bar counter with barrels behind it, three tables with stools, and the hearth.
            var bar = Part(PrimitiveType.Cube, t, new Vector3(-w / 2 + 2.6f, .55f, d / 2 - 1.5f), new Vector3(4, 1.1f, .7f), boards);
            bar.AddComponent<BoxCollider>(); bar.AddComponent<NavBlocker>();
            Part(PrimitiveType.Cube, t, new Vector3(-w / 2 + 2.6f, 1.12f, d / 2 - 1.5f), new Vector3(4.2f, .08f, .9f), dark);
            for (int i = 0; i < 3; i++) Part(PrimitiveType.Cylinder, t, new Vector3(-w / 2 + 1.2f + i * 1.1f, .5f, d / 2 - .55f), new Vector3(.8f, .5f, .8f), art.timber);
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
            var hearth = Part(PrimitiveType.Cube, t, new Vector3(w / 2 - .6f, 1, .8f), new Vector3(.9f, 2, 2.4f), art.stone);
            hearth.AddComponent<BoxCollider>(); hearth.AddComponent<NavBlocker>();
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 - 1.02f, .55f, .8f), new Vector3(.1f, .8f, 1.4f), art.glass);   // embers
            var fire = new GameObject("Hearth fire").AddComponent<Light>(); fire.transform.SetParent(t, false);
            fire.transform.localPosition = new Vector3(w / 2 - 1.6f, 1, .8f); fire.type = LightType.Point; fire.range = 9; fire.intensity = 1.8f; fire.color = new Color(1, .55f, .25f);
            var room = new GameObject("Taproom light").AddComponent<Light>(); room.transform.SetParent(t, false);
            room.transform.localPosition = new Vector3(-1, storey - .6f, 0); room.type = LightType.Point; room.range = 8; room.intensity = 1.1f; room.color = new Color(1, .78f, .5f);
            // Outside: the hanging sign and barrels by the door.
            Part(PrimitiveType.Cube, t, new Vector3(-2, 3.4f, -d / 2 - .8f), new Vector3(.12f, .12f, 1.6f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(-2, 2.8f, -d / 2 - 1.4f), new Vector3(1, .75f, .08f), Tint(art.timber, new Color(.5f, .36f, .2f)));
            // The board hangs on two chains from the bracket, and a strut braces the bracket to the wall (seen close up, a board
            // with nothing above it in frame read as a slab floating in mid-air).
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(-2 + s * .38f, 3.26f, -d / 2 - 1.4f), new Vector3(.03f, .2f, .03f), art.metal);
            Part(PrimitiveType.Cube, t, new Vector3(-2, 2.98f, -d / 2 - .38f), new Vector3(.1f, .1f, 1.06f), art.timber, Quaternion.Euler(45, 0, 0));
            Part(PrimitiveType.Cylinder, t, new Vector3(-2, 2.8f, -d / 2 - 1.46f), new Vector3(.45f, .06f, .45f), art.metal, Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Cylinder, t, new Vector3(w / 2 - 1.2f, .5f, -d / 2 - 1), new Vector3(.8f, .5f, .8f), art.timber);
            var glow = new GameObject("Inn lantern").AddComponent<Light>(); glow.transform.SetParent(t, false);
            glow.transform.localPosition = new Vector3(0, 2.6f, -d / 2 - 1.2f); glow.type = LightType.Point; glow.range = 8; glow.intensity = 1.4f; glow.color = new Color(1, .72f, .4f);
            NightLights.Add(new NightLight { light = glow, dayIntensity = 1.4f, nightIntensity = 2.2f });
        }
        /// <summary>
        /// The Cracked Hearth (inn variant 1): a stone chimney breast on the hearth gable (+X), split from its glowing ash-pit
        /// door up to the eaves with the fire showing through, and the stack above knocked askew on a glowing seam.
        /// </summary>
        void CrackedHearth(Transform t, float w, float H, float roofH)
        {
            var stone = Tint(art.stone, new Color(.42f, .4f, .37f)); float face = w / 2 + 1.05f, z = .8f, low = H * .62f;
            var breast = Part(PrimitiveType.Cube, t, new Vector3(w / 2 + .6f, low / 2, z), new Vector3(.9f, low, 2.2f), stone);
            breast.AddComponent<BoxCollider>(); breast.AddComponent<NavBlocker>();
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 + .65f, (low + H + .4f) / 2, z), new Vector3(.8f, H + .4f - low, 1.2f), stone);
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
            Part(PrimitiveType.Cube, stack, new Vector3(0, tall / 2, 0), new Vector3(.8f, tall, 1.2f), stone);
            Part(PrimitiveType.Cube, stack, new Vector3(0, tall + .1f, 0), new Vector3(1, .2f, 1.4f), stone);
            if (art.particle != null) Smoke(stack, new Vector3(0, tall + .4f, 0));
            // Stones fallen from the crack.
            Part(PrimitiveType.Cube, t, new Vector3(face + .5f, .1f, z + .9f), new Vector3(.4f, .25f, .3f), stone, Quaternion.Euler(0, 30, 0));
            Part(PrimitiveType.Cube, t, new Vector3(face + .35f, .08f, z - .7f), new Vector3(.3f, .2f, .35f), stone, Quaternion.Euler(0, -20, 0));
        }
        void Barn(Transform t, Vector2 size)
        {
            float w = size.x, d = size.y, h = 4.2f;
            var boards = Tint(art.timber, new Color(.36f, .22f, .15f));
            Part(PrimitiveType.Cube, t, new Vector3(0, h / 2, 0), new Vector3(w, h, d), boards);
            float drop = FootDrop(t, w, d); if (drop > 0) Part(PrimitiveType.Cube, t, new Vector3(0, .1f - drop / 2, 0), new Vector3(w + .2f, drop + .2f, d + .2f), art.stone);   // stone footing on a slope
            for (float x = -w / 2; x <= w / 2 + .01f; x += 1.2f)
                foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(x, h / 2, sz * (d / 2 + .04f)), new Vector3(.12f, h, .08f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.6f, -d / 2 - .07f), new Vector3(3.2f, 3.2f, .1f), Tint(art.timber, new Color(.18f, .12f, .09f)));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.6f, -d / 2 - .12f), new Vector3(3.3f, .2f, .06f), art.timber, Quaternion.Euler(0, 0, 44));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.6f, -d / 2 - .12f), new Vector3(3.3f, .2f, .06f), art.timber, Quaternion.Euler(0, 0, -44));
            float roofH = d * .5f;
            MeshPart(ZoneMeshes.GableRoof(w + 1, d + 1.4f, roofH), t, new Vector3(0, h, 0), art.thatch);
            Solid(t, new Vector3(0, (h + roofH) / 2, 0), new Vector3(w + .3f, h + roofH, d + .3f));
        }
        void Well(Transform t, int variant = 0)
        {
            // variant 1: a blood-stone well (Khaven) - reddened stone and dark water.
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .45f, 0), new Vector3(2.2f, .45f, 2.2f), variant == 1 ? Tint(art.stone, new Color(.45f, .3f, .28f)) : art.stone);
            var water = art.water; if (variant == 1) { water = new Material(art.water); water.color = new Color(.28f, .06f, .05f, .9f); }
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .88f, 0), new Vector3(1.7f, .03f, 1.7f), water);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * 1, 1.6f, 0), new Vector3(.18f, 2.3f, .18f), art.timber);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 2.3f, 0), new Vector3(.15f, 1.05f, .15f), art.timber, Quaternion.Euler(0, 0, 90));
            MeshPart(ZoneMeshes.GableRoof(2.8f, 2.2f, 1f, .1f), t, new Vector3(0, 2.75f, 0), art.slate, Quaternion.Euler(0, 90, 0));
            Part(PrimitiveType.Cylinder, t, new Vector3(.2f, 1.5f, 0), new Vector3(.35f, .22f, .35f), art.timber);
            Solid(t, new Vector3(0, 1.2f, 0), new Vector3(2.4f, 2.4f, 2.4f));
        }
        /// <summary>Recursive branching dead tree (dead_oak props, grey woods, Wasting husks). Trunk gets a collider when solid. Returns its root.</summary>
        Transform DeadTree(Vector2 at, float scale, Material mat, int depth, Transform parent, Transform existing = null, float girth = -1)
        {
            var root = existing != null ? existing : new GameObject("Dead tree").transform;
            if (existing == null) { root.SetParent(parent, false); root.position = Ground(at); root.rotation = Quaternion.Euler(0, R01 * 360, 0); }
            // A dead_oak prop (Oakhaven's lone dead tree) keeps a massive trunk; ordinary dead trees are slimmer with longer, reaching limbs.
            if (girth < 0) girth = existing != null && depth >= 5 ? .9f : .38f;
            float trunkH = (girth > .6f ? 4.2f : 5.2f) * scale, trunkR = girth * scale;
            Part(PrimitiveType.Cylinder, root, new Vector3(0, trunkH / 2, 0), new Vector3(trunkR * 2, trunkH / 2, trunkR * 2), mat);
            // Root flare: a broad swell at the foot of the trunk and low buttress ridges running out and down into the soil
            // (long, half-sunk spheres). Thin tilted cylinders here read as sticks laid round the tree.
            Part(PrimitiveType.Sphere, root, new Vector3(0, .1f * scale, 0), new Vector3(trunkR * 2.9f, .9f * scale, trunkR * 2.9f), mat);
            for (int i = 0; i < 5; i++)
            {
                var outward = Quaternion.Euler(0, i * 72 + R01 * 20, 0);
                Part(PrimitiveType.Sphere, root, outward * new Vector3(0, .05f * scale, trunkR * 1.3f), new Vector3(trunkR * .9f, .55f * scale, trunkR * 2.4f), mat, outward * Quaternion.Euler(12, 0, 0));
            }
            Branch(root, new Vector3(0, trunkH * .92f, 0), Vector3.up, trunkH * (girth > .6f ? .55f : .5f), trunkR * (girth > .6f ? .75f : .7f), depth, mat);
            var cap = root.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, trunkH / 2, 0); cap.height = trunkH; cap.radius = trunkR;
            root.gameObject.AddComponent<NavBlocker>(); root.gameObject.AddComponent<TreeFade>();
            return root;
        }
        /// <summary>Bark for dead woods: charred dark in the ash, weathered grey elsewhere (Khaven's Whispering Wood). One draw.</summary>
        Material DeadBark() { float t = R01; return Tint(art.bark, Zone.biome == "ash" ? Color.Lerp(new Color(.2f, .18f, .16f), new Color(.3f, .27f, .24f), t) : Color.Lerp(new Color(.58f, .57f, .56f), new Color(.72f, .71f, .69f), t)); }
        void Branch(Transform root, Vector3 start, Vector3 dir, float length, float radius, int depth, Material mat)
        {
            if (depth <= 0 || radius < .02f) return;
            var end = start + dir.normalized * length;
            // Tapered limb: a cylinder plus a small knuckle at the fork so joints don't look sawn off.
            Part(PrimitiveType.Cylinder, root, (start + end) / 2, new Vector3(radius * 2, length / 2, radius * 2), mat, Quaternion.FromToRotation(Vector3.up, dir));
            Part(PrimitiveType.Sphere, root, end, Vector3.one * radius * 1.9f, mat);
            int kids = depth > 3 ? 3 : 2 + (R01 > .5f ? 1 : 0);
            for (int i = 0; i < kids; i++)
            {
                var axis = Quaternion.AngleAxis(i * 360f / kids + R01 * 50, Vector3.up) * Vector3.right;
                var nd = Quaternion.AngleAxis(30 + R01 * 32, axis) * dir;
                nd = Vector3.Lerp(nd, Vector3.up, .1f);
                Branch(root, end, nd, length * (.7f + R01 * .15f), radius * .58f, depth - 1, mat);
            }
        }
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
            // A full crown: three cards a cluster, ten to fourteen clusters, three or four lying under it, and a dark core about
            // 60% of the crown's radius (the crown reaches about 3.4 m from its heart), so from below it is leaf mass, not sky.
            LeafCrown(t, tr, lean + new Vector3(0, h + 1.5f, 0), spots, ends, LeafMaterial(family), bark, Tint(art.foliage, Wither(Color.Lerp(Leaf[family], Color.black, .45f))), 1.5f, new Vector3(4, 2.3f, 4), 3, 10, 14, 3, 4);
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
        /// </summary>
        void LeafCrown(Transform t, System.Random tr, Vector3 heart, List<(Vector3 at, float size, bool light)> spots, List<Vector3> ends, Material leaves, Material bark, Material core, float rim, Vector3 coreSize,
            int cardsPer = 2, int crownMin = 0, int crownMax = 0, int underMin = 0, int underMax = 0)
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
            foreach (var spot in spots) Cluster(spot.at, spot.size * .8f, Shade(spot.light ? 2 : 1, T()), true);
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
        Func<float, Vector3> Bole(Transform t, System.Random tr, float top, float r0, Material bark)
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
                return (r + r0 * flare * (.22f + .9f * ridge)) * Mathf.Clamp01((top - y) / (r0 * 1.6f));
            }
            MeshPart(ZoneMeshes.Tube(Axis, Radius, new[] { -.45f, -.2f, -.05f, .08f, .2f, .36f, .58f, top * .3f, top * .48f, top * .66f, top * .82f, top * .93f, top }, 14, Vector3.right, 2, .5f), t, Vector3.zero, bark).name = "Trunk";
            return Axis;
        }
        /// <summary>
        /// A limb from inside a trunk or limb (<paramref name="from"/>, on its axis) out to <paramref name="to"/>: tapered, swelling
        /// where it leaves the wood it grows from, and bowed a little below the straight line so it sets out flatter and turns
        /// upward. Closed at the tip. Local metres, like the parts round it.
        /// </summary>
        void Limb(Transform t, Vector3 from, Vector3 to, float r0, float tip, Material bark, float bow = .12f, int sides = 8)
        {
            var d = to - from; float len = d.magnitude; if (len < .05f) return; var dir = d / len;
            var side = Vector3.Cross(dir, Vector3.up); if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
            var sag = Vector3.Cross(side.normalized, dir) * len * bow;   // perpendicular to the limb, on its underside
            if (sag.y > 0) sag = -sag;
            Vector3 C(float s) { return from + d * s + sag * (4 * s * (1 - s)); }
            float R(float s, float a) { return Mathf.Lerp(r0, tip, s) * (1 + .35f * Mathf.Exp(-s * 9)) * Mathf.Clamp01((1.05f - s) / .1f); }
            MeshPart(ZoneMeshes.Tube(C, R, new[] { 0, .12f, .26f, .42f, .58f, .74f, .88f, .97f, 1.05f }, sides, side.normalized, 1, len * .5f), t, Vector3.zero, bark).name = "Limb";
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
            MeshPart(ZoneMeshes.Tube(Axis, Girth, new[] { 1.3f, 1.65f, 2f, 2.4f, 2.8f, 3.2f, 3.55f, 3.85f, 4.15f, 4.5f, 4.9f, 5.35f, 5.8f, 6.2f, 6.5f }, 32, Vector3.right, 3, .5f), t, Vector3.zero, bark).name = "Oak bole";
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
                MeshPart(ZoneMeshes.Tube(C, R, new[] { 0, .08f, .17f, .27f, .38f, .5f, .62f, .74f, .86f, .96f, 1.06f }, 14, Vector3.Cross(Vector3.up, outward), 2, 2.5f), t, Vector3.zero, bark).name = "Oak limb";
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
            MeshPart(ZoneMeshes.Tube(Axis, Radius, new[] { -.3f, -.1f, .1f, .4f, top * .2f, top * .4f, top * .6f, top * .8f, top * .92f, top }, 8, Vector3.right, 2, .5f), t, Vector3.zero, art.bark).name = "Trunk";
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
        void Fence(Transform t, float length)
        {
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 2) + 1);
            for (int i = 0; i < posts; i++)
                Part(PrimitiveType.Cube, t, new Vector3(-length / 2 + length * i / (posts - 1), .55f, 0), new Vector3(.14f, 1.1f, .14f), art.timber, Quaternion.Euler(R01 * 6 - 3, 0, R01 * 6 - 3));
            foreach (float y in new[] { .45f, .85f }) Part(PrimitiveType.Cube, t, new Vector3(0, y, 0), new Vector3(length, .08f, .06f), art.timber);
            Solid(t, new Vector3(0, .6f, 0), new Vector3(length, 1.2f, .3f));
        }
        void Cart(Transform t)
        {
            Part(PrimitiveType.Cube, t, new Vector3(0, .9f, 0), new Vector3(1.6f, .5f, 2.6f), art.timber);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, t, new Vector3(s * .95f, .6f, -.2f), new Vector3(1.2f, .06f, 1.2f), Tint(art.timber, new Color(.25f, .17f, .11f)), Quaternion.Euler(0, 0, 90));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * .5f, .75f, -2.2f), new Vector3(.1f, .1f, 2), art.timber, Quaternion.Euler(-12, 0, 0));
            Part(PrimitiveType.Sphere, t, new Vector3(0, 1.3f, .3f), new Vector3(1.3f, .6f, 1.7f), art.hay);
            Solid(t, new Vector3(0, .8f, -.4f), new Vector3(2.1f, 1.6f, 3.8f));
        }
        void Lamp(Transform t, int variant)
        {
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.4f, 0), new Vector3(.14f, 2.8f, .14f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(.35f, 2.7f, 0), new Vector3(.7f, .1f, .1f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(.6f, 2.4f, 0), new Vector3(.3f, .38f, .3f), art.glass);
            // Every lamp is lit after dark; variant > 0 lamps also burn by day (inn yard, mill).
            var l = new GameObject("Lamp light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(.6f, 2.3f, 0);
            l.type = LightType.Point; l.range = variant > 0 ? 7 : 9; l.intensity = 1.2f; l.color = new Color(1, .7f, .38f);
            NightLights.Add(new NightLight { light = l, dayIntensity = variant > 0 ? 1.2f : 0, nightIntensity = 1.7f });
        }
        /// <summary>Lights that brighten after dark (lamps, inn lanterns). Driven by <see cref="WorldClock"/>.</summary>
        public readonly List<NightLight> NightLights = new List<NightLight>();
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
            MeshPart(ZoneMeshes.GableRoof(w + .5f, d + .6f, .9f), t, new Vector3(0, floor + h, 0), art.thatch);
            // Nest boxes bolted to the east side, with a lid the hen-wife lifts to collect eggs.
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 + .3f, floor + .45f, 0), new Vector3(.6f, .6f, d * .8f), boards);
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 + .32f, floor + .8f, 0), new Vector3(.7f, .08f, d * .85f), art.slate, Quaternion.Euler(0, 0, -18));
            // Pop-hole and its door on a bottom hinge; the ramp runs down to the yard.
            Part(PrimitiveType.Cube, t, new Vector3(0, floor + .35f, -d / 2 - .03f), new Vector3(.5f, .6f, .04f), Tint(art.timber, new Color(.08f, .06f, .05f)));
            var hinge = new GameObject("Coop door hinge").transform; hinge.SetParent(t, false); hinge.localPosition = new Vector3(0, floor + .05f, -d / 2 - .07f);
            Part(PrimitiveType.Cube, hinge, new Vector3(0, .3f, 0), new Vector3(.56f, .62f, .05f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(0, floor / 2, -d / 2 - .75f), new Vector3(.5f, .05f, 1.6f), boards, Quaternion.Euler(-26, 0, 0));
            // Feed trough and a grain sack out front.
            Part(PrimitiveType.Cube, t, new Vector3(-1.4f, .18f, -d / 2 - 1.6f), new Vector3(1.2f, .25f, .35f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(-1.4f, .3f, -d / 2 - 1.6f), new Vector3(1.1f, .04f, .25f), art.hay);
            Part(PrimitiveType.Sphere, t, new Vector3(1.5f, .35f, -d / 2 - .5f), new Vector3(.55f, .7f, .5f), art.cloth);
            Solid(t, new Vector3(.2f, (floor + h + .9f) / 2, 0), new Vector3(w + .9f, floor + h + .9f, d + .2f));
            var coop = new ZoneCoop { name = name, hinge = hinge, door = t.TransformPoint(new Vector3(0, 0, -d / 2 - 1.7f)), yard = t.TransformPoint(new Vector3(0, 0, -d / 2 - 4.5f)),
                nest = t.TransformPoint(new Vector3(w / 2 + 1.1f, 0, 0)), trough = t.TransformPoint(new Vector3(-1.4f, 0, -d / 2 - 2.3f)) };
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
            // Stone arch: deck segments follow a gentle arc; the deck is walkable, the parapets are not.
            int segs = 8; float rise = 1.2f, width = 4;
            for (int i = 0; i < segs; i++)
            {
                float a = (i + .5f) / segs, z = -length / 2 + length * a;
                float y = .15f + Mathf.Sin(a * Mathf.PI) * rise, slope = Mathf.Cos(a * Mathf.PI) * rise * Mathf.PI / length;
                var deck = Part(PrimitiveType.Cube, t, new Vector3(0, y, z), new Vector3(width, .45f, length / segs + .08f), art.stone, Quaternion.Euler(-Mathf.Atan(slope) * Mathf.Rad2Deg, 0, 0));
                var col = deck.AddComponent<BoxCollider>(); deck.AddComponent<NavWalkable>();
                foreach (int s in new[] { -1, 1 })
                {
                    var para = Part(PrimitiveType.Cube, t, new Vector3(s * (width / 2 + .2f), y + .55f, z), new Vector3(.4f, .8f, length / segs + .08f), art.stone, Quaternion.Euler(-Mathf.Atan(slope) * Mathf.Rad2Deg, 0, 0));
                    para.AddComponent<BoxCollider>();
                }
            }
        }
        void Ruin(Transform t, float length)
        {
            for (float x = -length / 2; x < length / 2; x += 1.1f)
            {
                float h = .6f + R01 * 2.4f, g = LocalGround(t, x, 0) - .15f;   // each block stands on (and a little into) the ground under it
                Part(PrimitiveType.Cube, t, new Vector3(x, g + (h + .15f) / 2, 0), new Vector3(1.05f, h + .15f, .8f), Tint(art.stone, new Color(.42f, .41f, .38f)), Quaternion.Euler(0, R01 * 6 - 3, R01 * 6 - 3));
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
            Part(PrimitiveType.Cube, t, new Vector3(0, .02f, 0), new Vector3(2.2f, .5f, 2.2f), stone);   // deep enough to sit on a slope
            Part(PrimitiveType.Cube, t, new Vector3(0, .35f, .15f), new Vector3(1.5f, .22f, 1.5f), stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.23f, .25f), new Vector3(.5f, 1.6f, .5f), stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.4f, .25f), new Vector3(.9f, .8f, .75f), stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.38f, -.13f), new Vector3(.6f, .56f, .04f), dark);
            Part(PrimitiveType.Capsule, t, new Vector3(0, 2.36f, -.17f), new Vector3(.18f, .2f, .1f), Tint(art.stone, new Color(.86f, .84f, .78f)));
            MeshPart(ZoneMeshes.GableRoof(1.25f, 1.15f, .45f, .12f), t, new Vector3(0, 2.8f, .25f), art.slate, Quaternion.Euler(0, 90, 0));
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
            // A fallen building: broken wall stubs of uneven height, a charred remnant of roof slumped inside, fallen beams.
            float w = size.x, d = size.y; var plaster = Tint(art.plaster, Zone.biome == "ash" ? new Color(.46f, .455f, .45f) : new Color(.52f, .48f, .42f)); var charred = Tint(art.timber, new Color(.13f, .11f, .1f));
            float drop = FootDrop(t, w + .3f, d + .3f);   // on a slope the floor slab reaches down to the lowest ground under it
            Part(PrimitiveType.Cube, t, new Vector3(0, .3f - drop / 2, 0), new Vector3(w + .3f, .6f + drop, d + .3f), art.stone);
            foreach (int sz in new[] { -1, 1 })
                for (float x = -w / 2 + .6f; x < w / 2; x += 1.2f)
                {
                    float h = sz < 0 && Mathf.Abs(x) < 1 ? .2f : .8f + R01 * 2.4f;
                    Part(PrimitiveType.Cube, t, new Vector3(x, .6f + h / 2, sz * d / 2), new Vector3(1.2f, h, .35f), plaster);
                }
            foreach (int sx in new[] { -1, 1 })
                for (float z = -d / 2 + .6f; z < d / 2; z += 1.2f) { float h = .8f + R01 * 2.8f; Part(PrimitiveType.Cube, t, new Vector3(sx * w / 2, .6f + h / 2, z), new Vector3(.35f, h, 1.2f), plaster); }
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
        /// <summary>Stone curtain wall along a polyline, with a walkway lip and crenellations.</summary>
        void Wall(Vector2[] pts, Transform parent)
        {
            if (pts == null || pts.Length < 2) return;
            var stone = Tint(art.stone, new Color(.46f, .45f, .42f));
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1]; float len = Vector2.Distance(a, b); if (len < .1f) continue;
                var seg = new GameObject("Wall").transform; seg.SetParent(parent, false);
                var mid = (a + b) / 2; seg.position = Ground(mid); seg.rotation = Quaternion.LookRotation(new Vector3(b.x - a.x, 0, b.y - a.y));
                // Down to the lowest ground along the run, so a wall across a slope (or off a levelled gate pad) never shows daylight under it.
                float low = seg.position.y; for (int k = 0; k <= 8; k++) { var q = Vector2.Lerp(a, b, k / 8f); low = Mathf.Min(low, HeightAt(q.x, q.y)); }
                float sink = seg.position.y - low + .4f;
                Part(PrimitiveType.Cube, seg, new Vector3(0, (3.9f - sink) / 2, 0), new Vector3(1.2f, 3.9f + sink, len + .6f), stone);
                for (float z = -len / 2 + .5f; z < len / 2; z += 1.4f)
                    foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, seg, new Vector3(s * .45f, 4.2f, z), new Vector3(.35f, .6f, .7f), stone);
                Solid(seg, new Vector3(0, 2, 0), new Vector3(1.4f, 5, len + .4f));
            }
        }
        void Tower(Transform t, float diameter)
        {
            float h = 7.5f; var stone = Tint(art.stone, new Color(.44f, .43f, .41f));
            float drop = FootDrop(t, diameter * .8f, diameter * .8f);   // on a slope the tower's foot reaches the lowest ground at its base
            Part(PrimitiveType.Cylinder, t, new Vector3(0, (h - drop) / 2, 0), new Vector3(diameter, (h + drop) / 2, diameter), stone);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36 * Mathf.Deg2Rad;
                Part(PrimitiveType.Cube, t, new Vector3(Mathf.Cos(a) * diameter * .46f, h + .35f, Mathf.Sin(a) * diameter * .46f), new Vector3(.6f, .7f, .6f), stone, Quaternion.Euler(0, -i * 36, 0));
            }
            if (cone == null) cone = ZoneMeshes.Cone(1, 1);
            var roof = MeshPart(cone, t, new Vector3(0, h + .2f, 0), art.slate); roof.transform.localScale = new Vector3(diameter * .55f, 3.2f, diameter * .55f);
            Part(PrimitiveType.Cube, t, new Vector3(0, 4.5f, -diameter / 2 - .02f), new Vector3(.4f, .9f, .1f), art.glass);
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
            var stone = Tint(art.stone, new Color(.36f, .36f, .37f));
            if (variant == 1)
            {
                // On a slope the mound reaches down past the lowest ground round its foot (its top drops only half as far).
                float e = LocalGround(t, 0, -2.6f), sink = Mathf.Min(FootDrop(t, 5.2f, 6, 2.2f), 1.2f);
                Part(PrimitiveType.Sphere, t, new Vector3(0, -.2f - sink, 2.2f), new Vector3(7.4f, 5.8f + sink, 8.6f), Tint(art.foliage, new Color(.3f, .31f, .19f)));
                Part(PrimitiveType.Cube, t, new Vector3(0, e + 1.05f, -2.15f), new Vector3(1.6f, 2.2f, .1f), Tint(art.metal, new Color(.04f, .04f, .05f)));
                Part(PrimitiveType.Cube, t, new Vector3(0, e + 1.05f, -1.2f), new Vector3(1.6f, 2.2f, 1.9f), Tint(art.metal, new Color(.04f, .04f, .05f)));   // the dark passage behind the door
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
                Part(PrimitiveType.Cube, t, new Vector3(0, 1.2f - drop / 2, 1.5f), new Vector3(6, 2.4f + drop, 6), stone);
                MeshPart(ZoneMeshes.GableRoof(6.6f, 6.6f, 1.6f), t, new Vector3(0, 2.4f, 1.5f), Tint(art.slate, new Color(.27f, .28f, .3f)));
                Part(PrimitiveType.Cube, t, new Vector3(0, 1, -1.52f), new Vector3(1.8f, 2, .1f), Tint(art.metal, new Color(.14f, .14f, .16f)));
                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * 1.4f, 1.3f, -1.7f), new Vector3(.6f, 2.6f, .6f), stone);
                for (int i = 0; i < 3; i++) Part(PrimitiveType.Cube, t, new Vector3(0, .1f + i * .12f, -2.6f + i * .35f), new Vector3(2.6f, .2f, .4f), stone);
            }
            // Grave markers before it (the same five draws each, in the same order), set on the ground where they stand.
            for (int i = 0; i < 6; i++) { float gx = R01 * 10 - 5, gz = -4 - R01 * 4; Part(PrimitiveType.Cube, t, new Vector3(gx, LocalGround(t, gx, gz) + .46f, gz), new Vector3(.55f, 1, .15f), stone, Quaternion.Euler(R01 * 10 - 5, R01 * 30 - 15, R01 * 10 - 5)); }
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
            var stone = Tint(art.stone, Zone.biome == "ash" ? new Color(.37f, .365f, .37f) : Zone.biome == "mountain" ? new Color(.35f, .335f, .31f) : new Color(.4f, .38f, .35f));   // ash: grey, not sandstone; mountain: darker, so a sunlit crag stays under the fog
            bool seat = Zone.biome == "mountain"; float side = Mathf.Sign(lift), rise = Mathf.Abs(lift), low = 0;
            float Y(float x, float z) { var w = t.TransformPoint(new Vector3(x, 0, z)); return HeightAt(w.x, w.z) - t.position.y; }
            var own = new System.Random(Zone.seed ^ (Mathf.RoundToInt(t.position.x * 8) * 73856093) ^ (Mathf.RoundToInt(t.position.z * 8) * 19349663)); float O() { return (float)own.NextDouble(); }
            // A lump: a crag blob (the prop's stream picks which) with its flat base at bottom, scaled (sx, sy, sz), tipped lean
            // degrees about x (positive tips its top to +z), rolled about z, turned yaw.
            void Rock(float x, float bottom, float z, float sx, float sy, float sz, float lean, float roll, float yaw)
            {
                MeshPart(CragRock((int)(O() * 6)), t, new Vector3(x, bottom + .2f * sy, z), stone, Quaternion.Euler(lean, yaw, roll)).transform.localScale = new Vector3(sx, sy, sz);
            }
            var fronts = new List<Vector2>();   // each step's (x, front plane z), for the scree
            // One step of the face: the lumps that fill an envelope w wide, d deep and h tall from its front plane zf and its foot.
            // f is which way is back (into a scarp's shelf, +z on a free crag); a scarp's lumps lean that way, a free crag's
            // either way. The zone's lean draws (ra, rb, rc) tip the upper mass, as they tipped the old slab.
            void Step(float x, float zf, float foot, float h, float w, float d, float f, bool scarp, float ra, float rb, float rc)
            {
                fronts.Add(new Vector2(x, zf));
                // The base: wide and low (half the height), bulging .3 m in front of the step's front plane, sunk .3 m under the foot.
                float hA = Mathf.Max(2.5f, h * (.5f + O() * .15f)), szA = d * .85f + O() * .5f;
                Rock(x + (O() - .5f) * .8f, foot - .3f, zf + f * (szA / 2 - .3f), w + 1.8f + O() * .8f, hA, szA, scarp ? f * (1 + O() * 3) : (O() - .5f) * 8, (O() - .5f) * 4, (O() - .5f) * 30);
                // The upper mass: set back (.8-1.6 m on a scarp, .3-.8 on a free crag), its top .95-1.15 of the way from the foot to
                // the crest, so the crest is ragged; tall enough (.65-.85 of the height) that its base sits well down in the base lump.
                float hB = Mathf.Max(2.5f, h * (.65f + O() * .2f)), szB = d * .75f + O() * .4f, topB = foot + h * (.95f + O() * .2f), zB = zf + f * ((scarp ? .8f + O() * .8f : .3f + O() * .5f) + szB / 2);
                Rock(x + (O() - .5f) * 1.4f, topB - hB, zB, w + 1.2f + O() * .8f, hB, szB, scarp ? f * (2 + ra * 5) : ra * 14 - 7, rc * 6 - 3, scarp ? rb * 16 - 8 : rb * 30 - 15);
                // A crest knob on two steps in three, part sunk into the upper mass's top, some standing a couple of metres over it.
                if (O() < .65f) { float hC = 1.6f + O() * 2.2f; Rock(x + (O() - .5f) * 2.2f, topB - hC * (.35f + O() * .4f), zB + f * (O() - .5f) * 1.2f, 1.8f + O() * 1.6f, hC, 1.6f + O() * 1.2f, (O() - .5f) * 16, (O() - .5f) * 16, O() * 360); }
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
            Scree(t, fronts, rise > 0 ? side : 1, rise > 0 ? 1 : .5f, stone, O);
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
            if (clear) { trunks.Add(at); return; }
            LeafTrees.Remove(tree.position); DestroyImmediate(tree.gameObject);
        }
        void BuildGroves()
        {
            foreach (var prop in Zone.props) if (prop != null && (prop.kind == "tree" || prop.kind == "pine" || prop.kind == "dead_oak" || prop.kind == "great_oak")) trunks.Add(prop.at);
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
                        LeafCrown(t, tr, lean + new Vector3(0, h + .85f, 0), spots, ends, orchardLeaf, bark, Tint(art.foliage, new Color(.19f, .29f, .12f)), 1, new Vector3(1.8f, 1.1f, 1.8f));
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
        /// <summary>Exit markers: a waystone with a lantern where a road leaves the zone.</summary>
        void BuildExits()
        {
            foreach (var e in Zone.exits)
            {
                var t = new GameObject("Exit: " + e.name).transform; t.SetParent(statics, false); t.position = Ground(e.at);
                Part(PrimitiveType.Cube, t, new Vector3(0, 1.1f, 0), new Vector3(.8f, 2.2f, .5f), Tint(art.stone, new Color(.5f, .48f, .44f)), Quaternion.Euler(0, 15, 0));
                Part(PrimitiveType.Cube, t, new Vector3(0, 2.45f, 0), new Vector3(.35f, .45f, .35f), art.glass);
                var l = new GameObject("Waystone light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(0, 2.5f, 0);
                l.type = LightType.Point; l.range = 6; l.intensity = 1.1f; l.color = new Color(1, .7f, .4f);
            }
        }

        /// <summary>How much grass should grow at a point (0 = none): open meadow is full, the village core is trampled.</summary>
        float Openness(Vector2 p)
        {
            float living = 1 - Unmade(p.x, p.y) / .55f; if (living <= 0) return 0;   // grass thins out along the grey's ragged front
            float verge = 0;   // 1 at a road's edge, 0 from 2.5 m out
            foreach (var r in Zone.roads) { float d = DistanceToPath(p, r.points) - r.width / 2; if (d < .8f) return 0; verge = Mathf.Max(verge, 1 - Mathf.Clamp01((d - .8f) / 2.5f)); }
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
            foreach (var prop in Zone.props)
            {
                if (prop == null || prop.kind == "wall") continue;
                float r = Mathf.Max(2, Mathf.Max(prop.size.x, prop.size.y) * .75f);
                if ((prop.kind == "house" || prop.kind == "inn" || prop.kind == "barn" || prop.kind == "mill" || prop.kind == "ruined_house") && Vector2.Distance(p, prop.at) < r + 1.5f) return 0;
            }
            if (p.magnitude < Zone.flatRadius * .45f) open *= .35f;
            if (Zone.biome == "ash") return 0;                       // nothing grows in the ash
            if (Zone.biome == "mountain")
            {
                // Thin alpine turf in tufts: none on rock, scree or steep ground, and clumped where it does grow.
                float turf = 1 - Mathf.Clamp01(MountainRock(p.x, p.y) + Mathf.Clamp01((1 - UpAt(p.x, p.y) - .06f) * 6));
                open *= turf * turf * Mathf.Clamp01((Mathf.PerlinNoise(p.x * .23f + 7, p.y * .23f + 3) - .38f) * 2.4f) * .8f;
            }
            // Gloom: dry, patchy grass on hard ground, but a dense dry verge along the roads so they read at a glance.
            if (Gloom) open *= Mathf.Lerp(.25f + .6f * Mathf.PerlinNoise(p.x * .07f + 13, p.y * .07f + 29), 1, verge * .85f);
            return open * living;
        }
        /// <summary>Soft grey smoke from a chimney (or any stack).</summary>
        void Smoke(Transform parent, Vector3 localPos)
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
        /// <summary>Workplaces by trade (forge, stall, oven, tannery, woodpile); villagers of that trade work there.</summary>
        public readonly List<ZoneWorkplace> Workplaces = new List<ZoneWorkplace>();
        void Workplace(Transform t, string kind, Vector3 stand, Vector3 look)
        {
            var s = t.TransformPoint(stand); var l = t.TransformPoint(look);
            Workplaces.Add(new ZoneWorkplace { kind = kind, name = t.name, stand = Ground(new Vector2(s.x, s.z)), look = l });
        }
        Light Glow(Transform t, Vector3 at, float range, float intensity, Color color, float night)
        {
            var l = new GameObject("Glow").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = at;
            l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = color;
            NightLights.Add(new NightLight { light = l, dayIntensity = intensity, nightIntensity = night });
            return l;
        }
        /// <summary>Open-fronted smithy: lean-to roof on posts, stone hearth with glowing coals and a hood, anvil, quench barrel, tool rack.</summary>
        void Forge(Transform t)
        {
            var dark = Tint(art.timber, new Color(.25f, .17f, .11f));
            var iron = Tint(art.metal, new Color(.22f, .22f, .24f));
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, new Vector3(sx * 2.4f, sz > 0 ? 1.6f : 1.35f, sz * 1.9f), new Vector3(.22f, sz > 0 ? 3.2f : 2.7f, .22f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.95f, 0), new Vector3(5.6f, .14f, 4.6f), art.slate, Quaternion.Euler(-8, 0, 0));
            Part(PrimitiveType.Cube, t, new Vector3(0, .7f, 1.95f), new Vector3(5, 1.4f, .3f), art.stone);                        // back wall
            // Hearth, coals, hood and chimney.
            Part(PrimitiveType.Cube, t, new Vector3(1.2f, .5f, 1.1f), new Vector3(1.8f, 1, 1.3f), art.stone);
            Part(PrimitiveType.Cube, t, new Vector3(1.2f, 1.02f, 1.05f), new Vector3(1.3f, .08f, .9f), art.glass);
            Part(PrimitiveType.Cube, t, new Vector3(1.2f, 2.1f, 1.35f), new Vector3(1.3f, .9f, .9f), art.stone);
            Part(PrimitiveType.Cube, t, new Vector3(1.2f, 3.4f, 1.5f), new Vector3(.55f, 1.8f, .55f), art.stone);
            if (art.particle != null) Smoke(t, new Vector3(1.2f, 4.4f, 1.5f));
            Glow(t, new Vector3(1.2f, 1.5f, .6f), 7, 1.4f, new Color(1, .5f, .2f), 2.2f);
            Part(PrimitiveType.Cube, t, new Vector3(2.15f, .9f, 1.1f), new Vector3(.25f, .5f, .7f), Tint(art.timber, new Color(.4f, .28f, .18f))); // bellows
            // Anvil on a stump.
            Part(PrimitiveType.Cylinder, t, new Vector3(-.6f, .3f, -.2f), new Vector3(.6f, .3f, .6f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(-.6f, .7f, -.2f), new Vector3(.26f, .2f, .55f), iron);
            Part(PrimitiveType.Cube, t, new Vector3(-.6f, .84f, -.2f), new Vector3(.36f, .1f, .72f), iron);
            Part(PrimitiveType.Sphere, t, new Vector3(-.6f, .84f, .22f), new Vector3(.2f, .1f, .28f), iron);
            // Quench barrel, a rack of tools, and finished work leaning on the wall.
            Part(PrimitiveType.Cylinder, t, new Vector3(-1.9f, .45f, .9f), new Vector3(.8f, .45f, .8f), art.timber);
            Part(PrimitiveType.Cylinder, t, new Vector3(-1.9f, .91f, .9f), new Vector3(.66f, .01f, .66f), art.water);
            Part(PrimitiveType.Cube, t, new Vector3(-.9f, 1.8f, 1.78f), new Vector3(1.8f, .08f, .08f), dark);
            for (int i = 0; i < 5; i++)
            {
                Part(PrimitiveType.Cube, t, new Vector3(-1.6f + i * .35f, 1.55f, 1.76f), new Vector3(.04f, .45f, .04f), dark);
                Part(PrimitiveType.Cube, t, new Vector3(-1.6f + i * .35f, 1.32f, 1.74f), new Vector3(i % 2 == 0 ? .14f : .06f, .08f, .06f), iron);
            }
            for (int i = 0; i < 3; i++) Part(PrimitiveType.Cube, t, new Vector3(-2.1f + i * .2f, .7f, 1.65f), new Vector3(.05f, 1.2f, .05f), art.metal, Quaternion.Euler(-10, 0, 0)); // blades and bars
            Solid(t, new Vector3(1.2f, 1, 1.2f), new Vector3(1.9f, 2, 1.5f));
            Solid(t, new Vector3(0, .7f, 1.95f), new Vector3(5, 1.4f, .3f));
            Solid(t, new Vector3(-.6f, .45f, -.2f), new Vector3(.6f, .9f, .75f));
            Workplace(t, "forge", new Vector3(-.6f, 0, -1.15f), new Vector3(-.6f, .9f, -.2f));
            Workplace(t, "forge", new Vector3(1.2f, 0, -.3f), new Vector3(1.2f, 1, 1.1f));
        }
        static readonly Color[][] Awnings = { new[] { new Color(.62f, .2f, .16f), new Color(.86f, .8f, .66f) }, new[] { new Color(.2f, .34f, .52f), new Color(.86f, .8f, .66f) }, new[] { new Color(.32f, .45f, .22f), new Color(.8f, .66f, .3f) } };
        /// <summary>
        /// Market stall: an awning on four posts, a counter of goods (variant: 0 produce, 1 cloth and pots, 2 bread, all under
        /// dyed stripes; 3 Ash-Walker, GAME-ONLY look: two stitched hides on bone poles, a ragged hide valance, salt and bone).
        /// </summary>
        void Stall(Transform t, int variant)
        {
            bool hide = variant == 3;
            var colors = hide ? new[] { new Color(.56f, .43f, .29f), new Color(.64f, .52f, .36f) } : Awnings[Mathf.Abs(variant) % Awnings.Length];
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                if (hide) Part(PrimitiveType.Cylinder, t, new Vector3(sx * 1.4f, sz > 0 ? 1.25f : 1.1f, sz * .9f), new Vector3(.13f, sz > 0 ? 1.25f : 1.1f, .13f), Bone);
                else Part(PrimitiveType.Cube, t, new Vector3(sx * 1.4f, sz > 0 ? 1.25f : 1.1f, sz * .9f), new Vector3(.1f, sz > 0 ? 2.5f : 2.2f, .1f), art.timber);
            for (int i = 0; i < 6; i++)   // stripes, or one hide per half
                Part(PrimitiveType.Cube, t, new Vector3(-1.25f + i * .5f, 2.35f, 0), new Vector3(.5f, .05f, 2.3f), Tint(art.cloth, colors[hide ? i / 3 : i % 2]), Quaternion.Euler(-9, 0, hide ? (i / 3) * 3 - 1.5f : 0));
            for (int i = 0; i < 6; i++)   // valance: striped, or ragged hide strips of uneven length
                Part(PrimitiveType.Cube, t, new Vector3(-1.25f + i * .5f, hide ? 2.14f - (i * 7 % 4) * .04f : 2.05f, -1.16f), new Vector3(.5f, hide ? .12f + (i * 7 % 4) * .08f : .3f, .03f), Tint(art.cloth, colors[hide ? 0 : i % 2]));
            Part(PrimitiveType.Cube, t, new Vector3(0, .5f, -.45f), new Vector3(2.8f, 1, .7f), Tint(art.timber, new Color(.45f, .32f, .2f)));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.02f, -.45f), new Vector3(2.9f, .05f, .8f), Tint(art.timber, new Color(.35f, .25f, .16f)));
            var r = new System.Random(Zone.seed + (int)(t.position.x * 13 + t.position.z * 7));
            float R() { return (float)r.NextDouble(); }
            for (int i = 0; i < 7; i++)
            {
                var at = new Vector3(-1.2f + i * .4f, 1.1f, -.45f + (R() - .5f) * .3f);
                switch (hide ? 3 : Mathf.Abs(variant) % 3)
                {
                    case 0:   // baskets of apples, turnips and cabbages
                        Part(PrimitiveType.Cylinder, t, at, new Vector3(.32f, .08f, .32f), art.hay);
                        var produce = new[] { new Color(.7f, .15f, .12f), new Color(.85f, .75f, .6f), new Color(.4f, .6f, .25f), new Color(.8f, .55f, .15f) }[i % 4];
                        for (int k = 0; k < 3; k++) Part(PrimitiveType.Sphere, t, at + new Vector3((k - 1) * .08f, .1f, (k % 2) * .06f), Vector3.one * .11f, Tint(art.foliage, produce));
                        break;
                    case 1:   // bolts of cloth and clay pots
                        if (i % 2 == 0) Part(PrimitiveType.Cylinder, t, at + new Vector3(0, .08f, 0), new Vector3(.16f, .18f, .16f), Tint(art.cloth, Color.HSVToRGB(R(), .5f, .6f)), Quaternion.Euler(0, 0, 90));
                        else Part(PrimitiveType.Sphere, t, at + new Vector3(0, .12f, 0), new Vector3(.22f, .26f, .22f), Tint(art.stone, new Color(.62f, .4f, .28f)));
                        break;
                    case 3:   // salt blocks, bone harpoon heads, strips of dried meat
                        if (i % 3 == 0) Part(PrimitiveType.Cube, t, at + new Vector3(0, .1f, 0), new Vector3(.24f, .2f, .2f), Tint(art.stone, new Color(.94f, .94f, .91f)), Quaternion.Euler(0, R() * 40 - 20, 0));
                        else if (i % 3 == 1) Part(PrimitiveType.Cube, t, at + new Vector3(0, .03f, 0), new Vector3(.07f, .05f, .34f), Bone, Quaternion.Euler(0, R() * 50 - 25, 0));
                        else Part(PrimitiveType.Capsule, t, at + new Vector3(0, .05f, 0), new Vector3(.08f, .14f, .08f), Tint(art.cloth, new Color(.36f, .16f, .12f)), Quaternion.Euler(90, R() * 60, 0));
                        break;
                    default:  // loaves and rolls
                        Part(PrimitiveType.Capsule, t, at + new Vector3(0, .06f, 0), new Vector3(.14f, .14f, .14f), Tint(art.hay, new Color(.72f, .48f, .22f)), Quaternion.Euler(90, R() * 60, 0));
                        break;
                }
            }
            Part(PrimitiveType.Cube, t, new Vector3(1.9f, .3f, .2f), new Vector3(.6f, .6f, .6f), Tint(art.timber, new Color(.45f, .33f, .21f)));   // crate
            Part(PrimitiveType.Sphere, t, new Vector3(-1.9f, .35f, .3f), new Vector3(.5f, .6f, .45f), art.cloth);                                // sack
            Solid(t, new Vector3(0, .5f, -.45f), new Vector3(2.9f, 1, .8f));
            Workplace(t, "stall", new Vector3(0, 0, .4f), new Vector3(0, 1.1f, -1.5f));
        }
        /// <summary>Bakehouse oven: a clay dome on a stone plinth with a glowing mouth, a bread table, flour sacks and a peel.</summary>
        void Oven(Transform t)
        {
            var clay = Tint(art.stone, new Color(.66f, .42f, .3f));
            Part(PrimitiveType.Cube, t, new Vector3(0, .45f, .3f), new Vector3(2.2f, .9f, 2.2f), art.stone);
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
        /// <summary>A herb patch: a clump of stems with flower heads (variant 0 yarrow white, 1 feverfew yellow, 2 comfrey violet).</summary>
        void Herb(Transform t, int variant)
        {
            var stem = Tint(art.foliage, new Color(.3f, .5f, .22f));
            var flower = Tint(art.foliage, new[] { new Color(.95f, .94f, .88f), new Color(.95f, .82f, .25f), new Color(.6f, .4f, .75f) }[Mathf.Abs(variant) % 3]);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 51, r = .12f + (i % 3) * .12f, h = .35f + (i % 4) * .08f;
                var at = Quaternion.Euler(0, a, 0) * new Vector3(r, 0, 0);
                Part(PrimitiveType.Cube, t, at + new Vector3(0, h / 2, 0), new Vector3(.03f, h, .03f), stem, Quaternion.Euler(8, a, 6));
                Part(PrimitiveType.Sphere, t, at + new Vector3(0, h + .02f, 0), new Vector3(.12f, .06f, .12f), flower);
            }
            for (int i = 0; i < 4; i++) Part(PrimitiveType.Cube, t, new Vector3(0, .06f, 0), new Vector3(.34f, .02f, .09f), stem, Quaternion.Euler(0, i * 45, 0));
        }
        /// <summary>Woodpile: stacked logs, a chopping block with an axe in it, split wood scattered about.</summary>
        void Woodpile(Transform t)
        {
            var bark = art.bark; var split = Tint(art.timber, new Color(.62f, .5f, .34f));
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 5 - row; i++)
                    Part(PrimitiveType.Cylinder, t, new Vector3(-1.4f + row * .25f + i * .5f, .25f + row * .42f, .6f), new Vector3(.45f, 1.1f, .45f), bark, Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Cylinder, t, new Vector3(1.4f, .3f, -.6f), new Vector3(.7f, .3f, .7f), bark);                                   // chopping block
            Part(PrimitiveType.Cylinder, t, new Vector3(1.4f, .61f, -.6f), new Vector3(.66f, .01f, .66f), split);
            Part(PrimitiveType.Cylinder, t, new Vector3(1.3f, .95f, -.6f), new Vector3(.04f, .35f, .04f), art.timber, Quaternion.Euler(0, 0, 25));  // axe handle
            Part(PrimitiveType.Cube, t, new Vector3(1.43f, .66f, -.6f), new Vector3(.2f, .06f, .14f), art.metal, Quaternion.Euler(0, 0, 25));
            for (int i = 0; i < 6; i++) Part(PrimitiveType.Cube, t, new Vector3(.6f + (i % 3) * .5f, .08f, -1.3f + (i / 3) * .4f), new Vector3(.14f, .14f, .5f), split, Quaternion.Euler(0, i * 37, 90));
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
            var stone = Tint(art.stone, new Color(.44f, .43f, .41f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f)); var planks = Tint(art.timber, new Color(.3f, .21f, .14f));
            var sand = Tint(art.cloth, new Color(.78f, .63f, .38f)); var rust = Tint(art.cloth, new Color(.5f, .15f, .1f));
            Part(PrimitiveType.Cube, t, new Vector3(0, top + .7f, 0), new Vector3(width + 3, 1.4f, 2.6f), stone);   // the span, reaching into both towers
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * half, top, 0), new Vector3(1.2f, 1.2f, 2.5f), stone, Quaternion.Euler(0, 0, 45));   // corbels round the arch
            for (int i = 0; i <= 6; i++) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(-half + i * step, top + 1.75f, sz * 1.05f), new Vector3(.6f, .7f, .45f), stone);
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
            Part(PrimitiveType.Cube, t, pivot + boom * new Vector3(-.75f, 0, 0), new Vector3(.5f, .45f, .4f), stone, boom);
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
            var stone = Tint(art.stone, new Color(.47f, .45f, .42f)); var dark = Tint(art.timber, new Color(.2f, .14f, .1f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            var sand = Tint(art.cloth, new Color(.78f, .63f, .38f)); var rust = Tint(art.cloth, new Color(.5f, .15f, .1f)); var slit = Tint(art.timber, new Color(.05f, .04f, .03f));
            Part(PrimitiveType.Cube, t, new Vector3(0, -.75f, 0), new Vector3(w + .7f, 1.7f, d + .7f), Tint(art.stone, new Color(.4f, .39f, .37f)));   // footing: a low plinth, deep where the perch falls away
            Part(PrimitiveType.Cube, t, new Vector3(0, h / 2, 0), new Vector3(w, h, d), stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, h + .1f, 0), new Vector3(w + .3f, .2f, d + .3f), stone);   // string course
            int nx = Mathf.Max(2, Mathf.RoundToInt(w / 1.15f)), nz = Mathf.Max(2, Mathf.RoundToInt(d / 1.15f));
            for (int i = 0; i < nx; i++) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(Mathf.Lerp(-w / 2 + .3f, w / 2 - .3f, i / (nx - 1f)), h + .6f, sz * (d / 2 - .1f)), new Vector3(.6f, .8f, .45f), stone);
            for (int i = 1; i < nz - 1; i++) foreach (int sx in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 - .1f), h + .6f, Mathf.Lerp(-d / 2 + .3f, d / 2 - .3f, i / (nz - 1f))), new Vector3(.45f, .8f, .6f), stone);
            // The door: planks and straps in a stone surround, a step before it, a lit loophole above.
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.2f, -d / 2 - .06f), new Vector3(1.5f, 2.4f, .12f), dark);
            foreach (float y in new[] { .5f, 1.2f, 1.9f }) Part(PrimitiveType.Cube, t, new Vector3(0, y, -d / 2 - .13f), new Vector3(1.5f, .1f, .04f), iron);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * .95f, 1.3f, -d / 2 - .1f), new Vector3(.4f, 2.6f, .2f), stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.7f, -d / 2 - .1f), new Vector3(2.3f, .4f, .2f), stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, .1f, -d / 2 - .45f), new Vector3(2.2f, .2f, .7f), stone);
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
            var mat = Tint(art.stone, Zone.biome == "mountain" ? MountainStone : new Color(.42f, .41f, .39f));   // the boulders' stone (EdgeRock)
            for (float a = R01 * 10; a < 360; a += 18 + R01 * 10)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(a, 180)) < 38) continue;   // the way up
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward; float r = radius + .6f + R01 * 2.2f, s = 1.8f + R01 * 1.8f, lo = float.MaxValue;
                foreach (float k in new[] { -.7f, 0, .7f }) { var w = t.TransformPoint(dir * (r + k * s)); lo = Mathf.Min(lo, HeightAt(w.x, w.z)); }   // sit on the downhill side
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
            var mud = Tint(art.soil, new Color(.22f, .17f, .12f)); var wet = Tint(art.soil, new Color(.16f, .12f, .085f)); var stone = Tint(art.stone, new Color(.42f, .4f, .37f));
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
            var rock = Tint(art.stone, new Color(.37f, .365f, .37f)); var dark = Tint(art.stone, new Color(.035f, .03f, .03f)); var salt = Tint(art.stone, new Color(.94f, .94f, .91f));   // rock: the ash cliffs' grey, so the face runs on into the Ridge
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
            Part(PrimitiveType.Cube, t, new Vector3(0, .1f, .35f), new Vector3(3.2f, .16f, 2.6f), dark);
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
                    if (NearRoad(at, 5) || Water.NearWater(at, 1.5f)) continue;
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
            var t = Root(new ZoneProp { kind = "rock", at = at, rotation = yaw }, statics);
            var mat = Tint(art.stone, Zone.biome == "ash" ? new Color(.33f, .32f, .31f) : Zone.biome == "mountain" ? MountainStone : new Color(.42f, .41f, .39f));
            if (Zone.biome == "mountain") SinkBySlope(t, at, s);
            Lump(b0, t, new Vector3(0, .45f * s, 0), new Vector3(2.1f * s, 1.5f * s, 1.8f * s), mat, y0);
            Lump(b1, t, new Vector3(.8f * s, .25f * s, .45f * s), new Vector3(1.2f * s, .9f * s, 1.1f * s), mat, y1);
            Solid(t, new Vector3(0, .6f * s, 0), new Vector3(1.8f * s, 1.2f * s, 1.5f * s));
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
        /// <summary>Ground grid resolution (the backdrop's inner row shares the ground's edge vertices exactly).</summary>
        int GroundSegments { get { return Mathf.RoundToInt(Zone.size / 1.25f); } }
        /// <summary>
        /// Scenery past the playable edge, so the world never visibly ends: a skirt of ground carrying on from the edge (same
        /// heights at the seam, the painted ground mirrored across it) that rises into the biome's hills or mountains, crags
        /// above the treeline on mountain and ash ridges, and silhouettes in the zone's own mix of woods. A road that runs off
        /// the edge carries on (its mirrored paint) up a valley that closes into trees or a col. Visual only: no colliders,
        /// no nav sources, outside the navmesh bounds and the map. Its own random stream leaves the zone's layout unchanged.
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
            float[] rings = { 0, 1.5f, 4, 8, 13, 19, 26, 34, 43, 53, 64, 76, BackdropWidth };
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
            var rockMat = Tint(art.stone, ash ? new Color(.36f, .355f, .36f) : mountain ? MountainStone : new Color(.40f, .39f, .38f));
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
                                // crossed upright fronds to the tip.
                                var set = CardsFor(boughMat, side); float turn = S() * 360;
                                for (int i = 0; i < 3; i++)
                                {
                                    float u = i / 2f, y = 1 + th * Mathf.Lerp(.2f, .62f, u), rad = th * Mathf.Lerp(.3f, .14f, u); var heart = at + Vector3.up * (y - rad * .5f);
                                    var tier = Color.Lerp(new Color(.6f, .68f, .6f), Color.white, u);
                                    for (int k = 0; k < 4; k++)
                                    {
                                        float droop = (22 + S() * 12) * Mathf.Deg2Rad; var outward = Quaternion.Euler(0, turn + i * 45 + k * 90 + (S() - .5f) * 20, 0) * Vector3.right;
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
