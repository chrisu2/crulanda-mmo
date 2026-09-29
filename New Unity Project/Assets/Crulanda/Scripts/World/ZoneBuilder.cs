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
            PrepareRelief(); Water = new ZoneWater(); Water.Prepare(Zone, (x, z) => HeightAt(x, z, false));
            BuildLighting(); BuildGround(); BuildWater(); BuildWasting(); BuildProps(); BuildGroves(); BuildExits(); BuildForestEdge(); BuildBoundaries(); BuildBackdrop();
            if (art.grass != null && art.grass.Length > 0)
            {
                // Gloom: the tufts are dry, greyed straw and no wildflowers bloom.
                var grass = Gloom ? art.grass.Select(m => new Material(m) { name = m.name + " (dry)", color = Color.Lerp(m.color, new Color(.5f, .46f, .37f), .75f), enableInstancing = true }).ToArray() : art.grass;
                gameObject.AddComponent<GrassField>().Build(this, grass, Gloom ? null : art.flowers, Openness, Zone.seed + 99, Zone.biome == "meadow" ? 2.3f : 1.6f, TallGrassPatches());
            }
            gameObject.AddComponent<FallingLeaves>().Init(this);
            Splashes.Ensure(this); TreeFade.Begin(art.fade);
            var view = Camera.main;
            if (view != null && art.post != null && view.GetComponent<ZonePost>() == null) view.gameObject.AddComponent<ZonePost>().Init(art.post, Zone, sunLight);
            try { StaticBatchingUtility.Combine(statics.gameObject); } catch (Exception e) { Debug.LogWarning("Static batching skipped: " + e.Message); }
            MapTexture = RenderMap(1024);   // in daylight, before the clock sets the hour
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
            if (carveWater && Water != null) h = Water.Carve(x, z, h);   // creeks and lakes (see ZoneWater)
            return h;
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
            GroundMesh = go.AddComponent<MeshFilter>(); GroundMesh.sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); var m = new Material(art.ground) { name = "Painted ground" };
            m.SetTextureScale("_DetailAlbedoMap", Vector2.one * Zone.size / 1.8f);   // ~1.8 m grain repeat, whatever the zone size
            if (Zone.biome == "ash") { m.SetTexture("_DetailAlbedoMap", AshDetail(Zone.seed)); m.SetTextureScale("_DetailAlbedoMap", Vector2.one * Zone.size / 5); }   // fine crazing, sharp up close
            m.mainTexture = PaintGround(Mathf.Clamp(Mathf.RoundToInt(Zone.size * 8 / 256) * 256, 1024, 2048)); r.sharedMaterial = m;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
        Texture2D PaintGround(int res)
        {
            var tex = new Texture2D(res, res, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4, name = "Ground paint" };
            var px = new Color32[res * res];
            Color grassA = new Color(.30f, .38f, .20f), grassB = new Color(.42f, .43f, .22f), grassC = new Color(.47f, .38f, .20f);
            Color dirt = new Color(.46f, .38f, .27f), rut = new Color(.34f, .28f, .20f), mud = new Color(.28f, .25f, .19f);
            Color soil = new Color(.30f, .23f, .16f), furrow = new Color(.20f, .15f, .11f), stubble = new Color(.62f, .53f, .30f);
            Color unmade = new Color(.47f, .47f, .49f);
            bool gloom = Gloom;
            if (gloom) { dirt = new Color(.42f, .38f, .34f); rut = new Color(.31f, .28f, .26f); }   // grey-brown village dirt and roads
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
                        float steep = Mathf.Clamp01((1 - UpAt(x, z) - .06f) * 6);
                        float rock = Mathf.Clamp01(MountainRock(x, z) + steep + (n3 - .5f) * .5f);
                        Color rockC = Color.Lerp(new Color(.27f, .27f, .28f), new Color(.39f, .38f, .36f), n2);
                        Color scree = Color.Lerp(new Color(.38f, .35f, .30f), new Color(.46f, .42f, .36f), n3);
                        Color alp = Color.Lerp(new Color(.27f, .31f, .18f), new Color(.38f, .36f, .22f), n1);
                        c = Color.Lerp(alp, Color.Lerp(scree, rockC, Mathf.Clamp01(steep * 1.4f + (n1 - .5f) * .8f)), rock) * (.88f + n3 * .18f);
                    }
                    else if (ashen)
                    {
                        // Ashland: pale grey petrified ash in plates a shade apart, split by angular cracks (Voronoi cell edges, some
                        // left faint so the network looks broken, not paved), and the odd copper-rust stain (canon: the dust tastes of
                        // copper). The edges bend a little; lines are about a texel wide, so up close they stay thin seams, not bands.
                        float u = (x + half) / plate + 3 + (n2 - .5f) * .1f + (n3 - .5f) * .03f;
                        float v = (z + half) / plate + 3 + (Mathf.PerlinNoise(x * .45f + 31, z * .45f + 17) - .5f) * .1f + (n3 - .5f) * .03f;
                        int cx = (int)u, cy = (int)v, near = 0, other = 0; float best = 99, edge = 99;
                        for (int b = cy - 1; b <= cy + 1; b++) for (int a = cx - 1; a <= cx + 1; a++)
                        { int k = b * cells + a; float dx = sx[k] - u, dz = sz[k] - v; if (dx * dx + dz * dz < best) { best = dx * dx + dz * dz; near = k; } }
                        for (int m = near * 8; m < near * 8 + 8; m++) { float e = bo[m] - u * bx[m] - v * bz[m]; if (e < edge) { edge = e; other = bn[m]; } }
                        uint pair = (uint)(Mathf.Min(near, other) * 7919 + Mathf.Max(near, other)) * 2654435761u;
                        float line = Mathf.Clamp01((.04f + .06f * n1 - edge * plate) / texel + .5f) * (pair >> 24 < 64 ? .35f : 1);
                        Color ashC = Color.Lerp(new Color(.46f, .46f, .47f), new Color(.60f, .59f, .59f), n1) * (.97f + .06f * Mathf.Repeat(sx[near] * 7.31f + sz[near] * 3.17f, 1));
                        ashC = Color.Lerp(ashC, new Color(.52f, .44f, .40f), Mathf.Clamp01((Mathf.PerlinNoise(x * .02f + 60, z * .02f) - .66f) * 2.2f));
                        c = Color.Lerp(ashC, new Color(.17f, .165f, .165f), line * .6f) * (.93f + n3 * .1f);
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
                    // The water's edge, creeks and lakes alike, along the real (wandering) waterline: a dark silty bed, wet mud
                    // and pale pebbles at the water, a narrow earthen bank, then grass. Noise breaks up the bank's outer edge;
                    // the grass stops at the same line (Openness).
                    float shore = Water.Shore(p, 3) + (n2 - .5f) * 1.1f;
                    if (shore < 2.2f)
                    {
                        c = Color.Lerp(c, Color.Lerp(mud, dirt, n1 * .4f), Mathf.Clamp01((2.2f - shore) / 1.5f));
                        if (shore < .6f) c = Color.Lerp(c, new Color(.22f, .2f, .15f), Mathf.Clamp01((.6f - shore) / .6f) * .6f);
                        if (Mathf.Abs(shore) < .5f && n3 > .5f) c = Color.Lerp(c, new Color(.55f, .52f, .46f), .45f);
                        if (shore < -.5f) c = Color.Lerp(c, new Color(.16f, .15f, .12f), Mathf.Clamp01((-.5f - shore) / 3));
                    }
                    // Ash: roads, yards and grove litter are trodden ash too, keeping only a trace of their earth colour.
                    if (ashen) { float l = c.grayscale; c = Color.Lerp(new Color(l, l, l), c, .4f); }
                    float gone = Unmade(x, z);
                    if (gone > 0)
                    {
                        // The colour drains first (grey grass, grey dirt), then the land's own features go too: roads, cracks and
                        // shading are erased into flat grey with a fine fizz of static. Not burnt, not rotted: unmade.
                        uint hsh = (uint)(i * 73856093) ^ (uint)(j * 19349663); hsh = (hsh ^ (hsh >> 13)) * 0x5bd1e995u;
                        float grey = c.grayscale, fizz = ((hsh ^ (hsh >> 15)) & 1023) / 1023f;
                        var drained = Color.Lerp(c, new Color(grey, grey, grey * 1.02f), Mathf.Clamp01(gone * 1.8f));
                        var stat = unmade * (.9f + (fizz - .5f) * .22f + (n1 - .5f) * .12f);
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
                var tint = ZoneColors.Parse(Zone.waterTint, baseMat.color);
                if (m.HasProperty("_Shallow")) { tint.a = .86f; m.SetColor("_Color", tint); m.SetColor("_Shallow", Color.Lerp(tint, Color.grey, .3f) * new Color(1, 1, 1, .6f)); }
                else { tint.a = baseMat.color.a; m.color = tint; }
            }
            // The tint only colours the (premultiplied) body under the sky reflection: murky water also reflects less sky
            // face-on and glints less, or the untinted sky is all you see (Gloom Creek read as chrome).
            if (Zone.waterReflect > 0 && m.HasProperty("_Reflect"))
            {
                // Murky water is also harder to see into and calmer-looking (big ripples on dark water read as tar).
                float r = Mathf.Clamp01(Zone.waterReflect); m.SetFloat("_Reflect", r); m.SetFloat("_Glare", m.GetFloat("_Glare") * (.6f + .4f * r));
                if (m.HasProperty("_Murk")) { m.SetFloat("_Murk", Mathf.Lerp(3.2f, 1.2f, r)); m.SetFloat("_Bump", m.GetFloat("_Bump") * .55f); }
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
                float edge = Half + BackdropWidth, x0 = w.x + 2, x1 = edge - 12, span = Zone.size * 1.1f;
                // The static curtain: crawling grey static, thin at the ground and thickest a few metres up, so the land behind it
                // greys with distance instead of behind a ruled bottom edge. It runs past the view; its ends fade out.
                Haze(HazeMesh("Static curtain", new[] { HazeRow(24, s => new Vector3(w.x, -1, (s * 2 - 1) * span)), HazeRow(24, s => new Vector3(w.x, w.curtainHeight - 1, (s * 2 - 1) * span)) },
                    p => (span - Mathf.Abs(p.z)) / 30), 3000, true, .35f, 1.2f);
                // The unmade fades into haze: a flat sheet over it, clear at the curtain and opaque before the backdrop's flat skirt
                // ends (and toward its sides). Linear fog alone leaves that edge half-fogged at the sides of the view (Oakhaven, Peaks).
                var floor = new Vector3[8][];
                for (int r = 0; r < 8; r++) { float x = r < 7 ? Mathf.Lerp(x0, x1, r / 6f) : edge + 40; floor[r] = HazeRow(40, s => new Vector3(x, .3f, (s * 2 - 1) * (edge + 40))); }
                Haze(HazeMesh("Unmade haze", floor, p => Mathf.Clamp01((edge + 40 - Mathf.Abs(p.z)) / 40) * Mathf.SmoothStep(0, 1, Mathf.Max(Mathf.InverseLerp(x0, x1, p.x),
                    Mathf.InverseLerp(Half + 20, x1, Mathf.Abs(p.z)) * Mathf.InverseLerp(x0, x0 + 30, p.x)))), 2995, false, 0, 1);
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
                    case "crypt": Crypt(t); break;
                    case "cliff": Cliff(t, p.size.x > 0 ? p.size.x : 20, Mathf.Abs(p.lift)); break;
                    case "dead_oak": DeadTree(p.at, 1, Tint(art.bark, new Color(.4f, .36f, .32f)), 5, statics, t); break;   // weathered, paler bark (plain bark reads black in shade); size comes from the root's scale
                    case "tree": Broadleaf(t, p.variant); break;
                    case "pine": Pine(t); break;
                    case "fence": Fence(t, p.size.x > 0 ? p.size.x : 8); break;
                    case "hedge": Part(PrimitiveType.Cube, t, new Vector3(0, .7f, 0), new Vector3(p.size.x > 0 ? p.size.x : 6, 1.4f, 1.2f), Tint(art.foliage, new Color(.22f, .30f, .16f))); Solid(t, new Vector3(0, .7f, 0), new Vector3(p.size.x > 0 ? p.size.x : 6, 1.4f, 1.2f)); break;
                    case "haystack": Part(PrimitiveType.Sphere, t, new Vector3(0, .9f, 0), new Vector3(2.6f, 2.2f, 2.6f), art.hay); Solid(t, new Vector3(0, 1, 0), new Vector3(2.4f, 2, 2.4f)); break;
                    case "cart": Cart(t); break;
                    case "barrels": for (int i = 0; i <= p.variant % 3; i++) Part(PrimitiveType.Cylinder, t, new Vector3(i * .75f - .4f, .5f, (i % 2) * .6f), new Vector3(.7f, .5f, .7f), art.timber); Solid(t, new Vector3(0, .5f, .3f), new Vector3(2.2f, 1, 1.4f)); break;
                    case "crates": for (int i = 0; i <= p.variant % 3; i++) Part(PrimitiveType.Cube, t, new Vector3(i * .9f - .5f, .45f + (i == 2 ? .9f : 0), 0), Vector3.one * .9f, Tint(art.timber, new Color(.45f, .33f, .21f))); Solid(t, new Vector3(0, .5f, 0), new Vector3(2.2f, 1, 1)); break;
                    case "lamp": Lamp(t, p.variant); break;
                    case "grave": Part(PrimitiveType.Cube, t, new Vector3(0, .5f, 0), new Vector3(.6f, 1, .15f), art.stone, Quaternion.Euler(R01 * 8 - 4, 0, R01 * 8 - 4)); break;
                    case "rock": { float s = 1 + p.variant * .6f; Lump(Boulder(), t, new Vector3(0, .3f * s, 0), new Vector3(2f * s, 1.3f * s, 1.7f * s), art.stone, R01 * 360); if (s > 1.3f) Lump(Boulder(), t, new Vector3(.7f * s, .15f * s, .5f * s), new Vector3(.9f * s, .6f * s, .8f * s), art.stone, R01 * 360); Solid(t, new Vector3(0, .5f * s, 0), new Vector3(1.6f * s, 1f * s, 1.4f * s)); break; }
                    case "bridge": SeatBridge(t, p.size.x > 0 ? p.size.x : 12); Bridge(t, p.size.x > 0 ? p.size.x : 12); break;
                    case "signpost": Part(PrimitiveType.Cube, t, new Vector3(0, 1.1f, 0), new Vector3(.15f, 2.2f, .15f), art.timber); Part(PrimitiveType.Cube, t, new Vector3(.45f, 1.8f, 0), new Vector3(.9f, .28f, .06f), art.timber); break;
                    case "ruin": Ruin(t, p.size.x > 0 ? p.size.x : 6); break;
                    default: Debug.LogWarning("Unknown zone prop kind '" + p.kind + "'."); break;
                }
                if (!string.IsNullOrEmpty(p.interact) && t != null)
                    Interactables.Add(new ZoneInteractable { name = string.IsNullOrEmpty(p.name) ? p.kind : p.name, prompt = p.interact, item = p.item, kind = p.kind, once = p.once, position = t.position, root = t });
            }
        }
        static readonly Color[] Plaster = { new Color(.78f, .72f, .60f), new Color(.70f, .66f, .58f), new Color(.74f, .64f, .52f) };
        void House(Transform t, Vector2 size, float wallHeight, int variant, bool inn)
        {
            float w = size.x, d = size.y;
            var plaster = Tint(art.plaster, Plaster[Mathf.Abs(variant) % Plaster.Length]);
            var roofMat = variant % 2 == 0 ? art.thatch : art.slate;
            Part(PrimitiveType.Cube, t, new Vector3(0, .3f, 0), new Vector3(w + .3f, .6f, d + .3f), art.stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, .6f + wallHeight / 2, 0), new Vector3(w, wallHeight, d), plaster);
            // Timber framing: corner posts, a mid rail per storey and cross braces on the long walls.
            float top = .6f + wallHeight;
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
                Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2), .6f + wallHeight / 2, sz * (d / 2)), new Vector3(.3f, wallHeight, .3f), art.timber);
            int storeys = inn ? 2 : 1;
            for (int s = 1; s <= storeys; s++)
            {
                float y = .6f + wallHeight * s / (storeys + (inn ? 0 : 1)) ;
                foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(0, y, sz * (d / 2 + .02f)), new Vector3(w, .22f, .12f), art.timber);
                foreach (int sx in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .02f), y, 0), new Vector3(.12f, .22f, d), art.timber);
            }
            foreach (int sz in new[] { -1, 1 })
                for (float x = -w / 2 + 1.8f; x < w / 2 - 1; x += 2.6f)
                    Part(PrimitiveType.Cube, t, new Vector3(x, .6f + wallHeight * .3f, sz * (d / 2 + .03f)), new Vector3(.14f, wallHeight * .62f, .1f), art.timber, Quaternion.Euler(0, 0, 32));
            // Door faces south (-Z); warm windows either side and on the back.
            Part(PrimitiveType.Cube, t, new Vector3(0, .6f + 1.05f, -d / 2 - .05f), new Vector3(1.2f, 2.1f, .12f), Tint(art.timber, new Color(.22f, .14f, .09f)));
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
        /// Each wall piece is its own collider/nav blocker so the interior stays open.
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
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 - 1.1f, H + roofH * .75f, d * .15f), new Vector3(1, roofH * 1.1f, 1), art.stone);
            // The door: a plank door on a hinge at the left jamb, plus a stone step.
            var hinge = new GameObject("Door hinge").transform; hinge.SetParent(t, false); hinge.localPosition = new Vector3(-doorW / 2, 0, -d / 2 - .02f);
            var door = Part(PrimitiveType.Cube, hinge, new Vector3(doorW / 2, doorH / 2, 0), new Vector3(doorW, doorH, .1f), dark);
            var doorCollider = door.AddComponent<BoxCollider>();
            Part(PrimitiveType.Cube, t, new Vector3(0, .06f, -d / 2 - .55f), new Vector3(2, .12f, .8f), art.stone);
            var innDoor = new ZoneDoor { name = t.name, openable = true, hinge = hinge, blocker = doorCollider, position = t.TransformPoint(new Vector3(0, 1, -d / 2)) };
            Doors.Add(innDoor); innDoor.SetOpen(true);   // the inn keeps its door open; villagers come and go
            if (art.particle != null) Smoke(t, new Vector3(w / 2 - 1.1f, H + roofH * 1.4f, d * .15f));
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
            Part(PrimitiveType.Cylinder, t, new Vector3(-2, 2.8f, -d / 2 - 1.46f), new Vector3(.45f, .06f, .45f), art.metal, Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Cylinder, t, new Vector3(w / 2 - 1.2f, .5f, -d / 2 - 1), new Vector3(.8f, .5f, .8f), art.timber);
            var glow = new GameObject("Inn lantern").AddComponent<Light>(); glow.transform.SetParent(t, false);
            glow.transform.localPosition = new Vector3(0, 2.6f, -d / 2 - 1.2f); glow.type = LightType.Point; glow.range = 8; glow.intensity = 1.4f; glow.color = new Color(1, .72f, .4f);
            NightLights.Add(new NightLight { light = glow, dayIntensity = 1.4f, nightIntensity = 2.2f });
        }
        void Barn(Transform t, Vector2 size)
        {
            float w = size.x, d = size.y, h = 4.2f;
            var boards = Tint(art.timber, new Color(.36f, .22f, .15f));
            Part(PrimitiveType.Cube, t, new Vector3(0, h / 2, 0), new Vector3(w, h, d), boards);
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
        /// <summary>Recursive branching dead tree (the Oakhaven oak, Wasting husks). Trunk gets a collider when solid.</summary>
        void DeadTree(Vector2 at, float scale, Material mat, int depth, Transform parent, Transform existing = null, float girth = -1)
        {
            var root = existing != null ? existing : new GameObject("Dead tree").transform;
            if (existing == null) { root.SetParent(parent, false); root.position = Ground(at); root.rotation = Quaternion.Euler(0, R01 * 360, 0); }
            // The Great Oak keeps a massive trunk; ordinary dead trees are slimmer with longer, reaching limbs.
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
        Mesh[] canopies, boulders;
        Mesh Canopy() { if (canopies == null) { canopies = new Mesh[6]; for (int i = 0; i < 6; i++) canopies[i] = ZoneMeshes.Blob(Zone.seed + i * 17, .55f, false); } return canopies[(int)(R01 * 6) % 6]; }
        Mesh Boulder() { if (boulders == null) { boulders = new Mesh[6]; for (int i = 0; i < 6; i++) boulders[i] = ZoneMeshes.Blob(Zone.seed + 400 + i * 23, 1.1f, true, 8, 12); } return boulders[(int)(R01 * 6) % 6]; }
        GameObject Lump(Mesh mesh, Transform parent, Vector3 at, Vector3 scale, Material m, float yaw)
        {
            var o = MeshPart(mesh, parent, at, m, Quaternion.Euler(0, yaw, 0)); o.transform.localScale = scale; return o;
        }
        void Broadleaf(Transform t, int variant)
        {
            LeafTrees.Add(t.position);
            float h = 3.5f + R01 * 1.5f;
            // A tapering trunk with two limbs into the crown, and a crown of lumpy clumps in two shades.
            Part(PrimitiveType.Cylinder, t, new Vector3(0, h / 2, 0), new Vector3(.5f, h / 2, .5f), art.bark);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, h * .08f, 0), new Vector3(.72f, h * .08f, .72f), art.bark);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, t, new Vector3(s * .45f, h * .92f, 0), new Vector3(.22f, .9f, .22f), art.bark, Quaternion.Euler(0, R01 * 360, s * 35));
            var baseLeaf = Leaf[(Mathf.Abs(variant) + (int)(R01 * 2)) % Leaf.Length];
            var leaf = Tint(art.foliage, Wither(baseLeaf)); var leafLight = Tint(art.foliage, Wither(Color.Lerp(baseLeaf, new Color(.75f, .7f, .35f), .22f)));
            int clumps = 5 + (int)(R01 * 3);
            for (int i = 0; i < clumps; i++)
            {
                float a = i * 2.4f + R01, r = i == 0 ? 0 : 1 + R01 * .9f, s = (i == 0 ? 3.4f : 2.2f + R01 * 1.1f);
                Lump(Canopy(), t, new Vector3(Mathf.Cos(a) * r, h + .9f + (i == 0 ? .8f : R01 * 1.2f), Mathf.Sin(a) * r), new Vector3(s, s * .85f, s), i % 3 == 1 ? leafLight : leaf, R01 * 360);
            }
            var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, h / 2, 0); cap.height = h; cap.radius = .35f;
            t.gameObject.AddComponent<NavBlocker>(); t.gameObject.AddComponent<TreeFade>();
        }
        Mesh cone;
        void Pine(Transform t)
        {
            if (cone == null) cone = ZoneMeshes.Cone(1, 1);
            float h = 6 + R01 * 4;
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 1, 0), new Vector3(.45f, 1, .45f), art.bark);
            // Five tiers, each slightly tilted and turned, darker at the base where the light doesn't reach.
            var top = Wither(Color.Lerp(new Color(.18f, .27f, .18f), new Color(.22f, .3f, .19f), R01));
            for (int i = 0; i < 5; i++)
            {
                var mat = Tint(art.pine, Color.Lerp(top * .72f, top, i / 4f));
                var part = MeshPart(cone, t, new Vector3(R01 * .2f - .1f, 1.1f + i * h * .15f, R01 * .2f - .1f), mat, Quaternion.Euler(R01 * 6 - 3, R01 * 360, R01 * 6 - 3));
                float rad = (2.6f - i * .45f) * (h / 8); part.transform.localScale = new Vector3(rad, h * .34f, rad);
            }
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
                float h = .6f + R01 * 2.4f;
                Part(PrimitiveType.Cube, t, new Vector3(x, h / 2, 0), new Vector3(1.05f, h, .8f), Tint(art.stone, new Color(.42f, .41f, .38f)), Quaternion.Euler(0, R01 * 6 - 3, R01 * 6 - 3));
            }
            Solid(t, new Vector3(0, 1.2f, 0), new Vector3(length, 2.4f, 1));
        }

        void RuinedHouse(Transform t, Vector2 size)
        {
            // A fallen building: broken wall stubs of uneven height, a charred remnant of roof slumped inside, fallen beams.
            float w = size.x, d = size.y; var plaster = Tint(art.plaster, Zone.biome == "ash" ? new Color(.46f, .455f, .45f) : new Color(.52f, .48f, .42f)); var charred = Tint(art.timber, new Color(.13f, .11f, .1f));
            Part(PrimitiveType.Cube, t, new Vector3(0, .3f, 0), new Vector3(w + .3f, .6f, d + .3f), art.stone);
            foreach (int sz in new[] { -1, 1 })
                for (float x = -w / 2 + .6f; x < w / 2; x += 1.2f)
                {
                    float h = sz < 0 && Mathf.Abs(x) < 1 ? .2f : .8f + R01 * 2.4f;
                    Part(PrimitiveType.Cube, t, new Vector3(x, .6f + h / 2, sz * d / 2), new Vector3(1.2f, h, .35f), plaster);
                }
            foreach (int sx in new[] { -1, 1 })
                for (float z = -d / 2 + .6f; z < d / 2; z += 1.2f) { float h = .8f + R01 * 2.8f; Part(PrimitiveType.Cube, t, new Vector3(sx * w / 2, .6f + h / 2, z), new Vector3(.35f, h, 1.2f), plaster); }
            MeshPart(ZoneMeshes.GableRoof(w * .7f, d * .8f, 1.6f), t, new Vector3(.8f, .9f, .4f), Tint(art.thatch, new Color(.24f, .2f, .15f)), Quaternion.Euler(14, 8, -22));
            for (int i = 0; i < 4; i++) Part(PrimitiveType.Cube, t, new Vector3(R01 * w - w / 2, .75f, R01 * d - d / 2), new Vector3(.25f, .25f, 3 + R01 * 2), charred, Quaternion.Euler(R01 * 20, R01 * 180, R01 * 30));
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
                Part(PrimitiveType.Cube, seg, new Vector3(0, 1.8f, 0), new Vector3(1.2f, 4.2f, len + .6f), stone);
                for (float z = -len / 2 + .5f; z < len / 2; z += 1.4f)
                    foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, seg, new Vector3(s * .45f, 4.2f, z), new Vector3(.35f, .6f, .7f), stone);
                Solid(seg, new Vector3(0, 2, 0), new Vector3(1.4f, 5, len + .4f));
            }
        }
        void Tower(Transform t, float diameter)
        {
            float h = 7.5f; var stone = Tint(art.stone, new Color(.44f, .43f, .41f));
            Part(PrimitiveType.Cylinder, t, new Vector3(0, h / 2, 0), new Vector3(diameter, h / 2, diameter), stone);
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
        void Crypt(Transform t)
        {
            var stone = Tint(art.stone, new Color(.36f, .36f, .37f));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.2f, 1.5f), new Vector3(6, 2.4f, 6), stone);
            MeshPart(ZoneMeshes.GableRoof(6.6f, 6.6f, 1.6f), t, new Vector3(0, 2.4f, 1.5f), Tint(art.slate, new Color(.27f, .28f, .3f)));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1, -1.52f), new Vector3(1.8f, 2, .1f), Tint(art.metal, new Color(.14f, .14f, .16f)));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * 1.4f, 1.3f, -1.7f), new Vector3(.6f, 2.6f, .6f), stone);
            for (int i = 0; i < 3; i++) Part(PrimitiveType.Cube, t, new Vector3(0, .1f + i * .12f, -2.6f + i * .35f), new Vector3(2.6f, .2f, .4f), stone);
            for (int i = 0; i < 6; i++) Part(PrimitiveType.Cube, t, new Vector3(R01 * 10 - 5, .5f, -4 - R01 * 4), new Vector3(.55f, 1, .15f), stone, Quaternion.Euler(R01 * 10 - 5, R01 * 30 - 15, R01 * 10 - 5));
            Solid(t, new Vector3(0, 1.8f, 1.2f), new Vector3(6.2f, 3.6f, 6.6f));
        }
        void Cliff(Transform t, float length, float lift = 0)
        {
            // A broken escarpment of tall leaning stone blocks, taller where the ground is raised behind it (CliffLift). On
            // mountains each block stands on the lower of the ground at its front and back, so none hangs off a slope.
            var stone = Tint(art.stone, Zone.biome == "ash" ? new Color(.37f, .365f, .37f) : new Color(.4f, .38f, .35f));   // ash: grey, not sandstone
            bool seat = Zone.biome == "mountain";
            for (float x = -length / 2; x < length / 2; x += 2.2f)
            {
                float h = 4 + R01 * 4 + lift * .5f, z = R01 * 1.5f, y = 0; var size = new Vector3(2.6f + R01, h, 3 + R01 * 2);
                if (seat)
                {
                    Vector3 front = t.TransformPoint(new Vector3(x, 0, z - size.z / 2)), back = t.TransformPoint(new Vector3(x, 0, z + size.z / 2));
                    y = Mathf.Min(HeightAt(front.x, front.z), HeightAt(back.x, back.z)) - t.position.y;
                }
                Part(PrimitiveType.Cube, t, new Vector3(x, y + h / 2 - .5f, z), size, stone, Quaternion.Euler(R01 * 10 - 5, R01 * 30 - 15, R01 * 8 - 4));
            }
            Solid(t, new Vector3(0, (6 + lift) / 2, .8f), new Vector3(length + 2, 6 + lift, 4.5f));
        }
        void BuildGroves()
        {
            foreach (var g in Zone.groves.Where(g => g.kind == "orchard"))
                for (float x = g.center.x - g.size.x / 2 + 3; x < g.center.x + g.size.x / 2; x += 6.5f)
                    for (float z = g.center.y - g.size.y / 2 + 3; z < g.center.y + g.size.y / 2; z += 6.5f)
                    {
                        var at = new Vector2(x + (R01 - .5f), z + (R01 - .5f));
                        if (NearRoad(at, 2) || Water.NearWater(at, 1.5f)) continue;
                        var t = Root(new ZoneProp { kind = "tree", at = at, scale = .75f + R01 * .15f }, statics);
                        float h = 2.4f + R01 * .6f;
                        Part(PrimitiveType.Cylinder, t, new Vector3(0, h / 2, 0), new Vector3(.35f, h / 2, .35f), art.bark);
                        var leaf = Tint(art.foliage, Color.Lerp(new Color(.3f, .42f, .18f), new Color(.38f, .44f, .2f), R01));
                        for (int k = 0; k < 3; k++) { float s = 2 + R01 * .8f; Lump(Canopy(), t, new Vector3(R01 * 1.2f - .6f, h + .5f + R01 * .7f, R01 * 1.2f - .6f), new Vector3(s, s * .85f, s), leaf, R01 * 360); }
                        var fruit = Tint(art.hay, new Color(.72f, .18f, .12f));
                        for (int k = 0; k < 6; k++) Part(PrimitiveType.Sphere, t, new Vector3(R01 * 2.4f - 1.2f, h + .2f + R01 * 1.4f, R01 * 2.4f - 1.2f), Vector3.one * .22f, fruit);
                        var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, h / 2, 0); cap.height = h; cap.radius = .3f;
                        t.gameObject.AddComponent<NavBlocker>(); t.gameObject.AddComponent<TreeFade>();
                    }
            foreach (var g in Zone.groves.Where(g => g.kind != "orchard"))
                for (int i = 0; i < g.count; i++)
                {
                    var at = new Vector2(g.center.x + (R01 - .5f) * g.size.x, g.center.y + (R01 - .5f) * g.size.y);
                    if (NearRoad(at, 2.5f) || NearProp(at, 5) || Water.NearWater(at, 1.5f)) continue;
                    if (g.kind == "dead") { DeadTree(at, .5f + R01 * .55f, DeadBark(), 3 + (R01 > .6f ? 1 : 0), statics); continue; }
                    var p = new ZoneProp { kind = g.kind == "pine" ? "pine" : "tree", at = at, rotation = R01 * 360, scale = .8f + R01 * .5f, variant = (int)(R01 * 4) };
                    var t = Root(p, statics);
                    if (p.kind == "pine") Pine(t); else Broadleaf(t, p.variant);
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
        /// <summary>A low leafy bush of two or three clumps; no collider (you push through it).</summary>
        void Bush(Vector2 at, bool conifer)
        {
            var t = new GameObject("Bush").transform; t.SetParent(statics, false); t.position = Ground(at, -.1f); t.rotation = Quaternion.Euler(0, R01 * 360, 0);
            var c = conifer ? Color.Lerp(new Color(.17f, .25f, .15f), new Color(.24f, .3f, .17f), R01) : Leaf[(int)(R01 * Leaf.Length) % Leaf.Length] * (.85f + R01 * .2f);
            var mat = Tint(art.foliage, Wither(c));
            int n = 2 + (int)(R01 * 2);
            for (int i = 0; i < n; i++) { float s = .9f + R01 * .8f; Lump(Canopy(), t, new Vector3(R01 * 1.2f - .6f, s * .3f, R01 * 1.2f - .6f), new Vector3(s * 1.3f, s * .8f, s * 1.3f), mat, R01 * 360); }
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
            foreach (var r in Zone.roads) if (DistanceToPath(p, r.points) < r.width / 2 + .8f) return 0;
            // Grass grows down to the bank's noisy edge (the same line PaintGround draws there), never in the water.
            if (Water.Shore(p, 3) + (Mathf.PerlinNoise(p.x * .45f, p.y * .45f) - .5f) * 1.1f < 1.3f) return 0;
            foreach (var c in Zone.clearings) if (Vector2.Distance(p, c.center) < c.radius + 1) return 0;
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
            if (Gloom) open *= .25f + .6f * Mathf.PerlinNoise(p.x * .07f + 13, p.y * .07f + 29);   // dry, patchy grass on hard ground
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
        /// <summary>Market stall: striped awning on four posts, a counter of goods (variant: 0 produce, 1 cloth and pots, 2 bread).</summary>
        void Stall(Transform t, int variant)
        {
            var colors = Awnings[Mathf.Abs(variant) % Awnings.Length];
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, new Vector3(sx * 1.4f, sz > 0 ? 1.25f : 1.1f, sz * .9f), new Vector3(.1f, sz > 0 ? 2.5f : 2.2f, .1f), art.timber);
            for (int i = 0; i < 6; i++)
                Part(PrimitiveType.Cube, t, new Vector3(-1.25f + i * .5f, 2.35f, 0), new Vector3(.5f, .05f, 2.3f), Tint(art.cloth, colors[i % 2]), Quaternion.Euler(-9, 0, 0));
            for (int i = 0; i < 6; i++)
                Part(PrimitiveType.Cube, t, new Vector3(-1.25f + i * .5f, 2.05f, -1.16f), new Vector3(.5f, .3f, .03f), Tint(art.cloth, colors[i % 2])); // valance
            Part(PrimitiveType.Cube, t, new Vector3(0, .5f, -.45f), new Vector3(2.8f, 1, .7f), Tint(art.timber, new Color(.45f, .32f, .2f)));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.02f, -.45f), new Vector3(2.9f, .05f, .8f), Tint(art.timber, new Color(.35f, .25f, .16f)));
            var r = new System.Random(Zone.seed + (int)(t.position.x * 13 + t.position.z * 7));
            float R() { return (float)r.NextDouble(); }
            for (int i = 0; i < 7; i++)
            {
                var at = new Vector3(-1.2f + i * .4f, 1.1f, -.45f + (R() - .5f) * .3f);
                switch (Mathf.Abs(variant) % 3)
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
            Part(PrimitiveType.Sphere, t, new Vector3(-1.5f, .38f, -.6f), new Vector3(.55f, .7f, .5f), Tint(art.cloth, new Color(.9f, .88f, .82f)));
            Part(PrimitiveType.Sphere, t, new Vector3(-1.6f, .32f, .1f), new Vector3(.5f, .6f, .45f), Tint(art.cloth, new Color(.9f, .88f, .82f)));
            Part(PrimitiveType.Cube, t, new Vector3(-1.2f, 1.1f, .9f), new Vector3(.05f, 2.2f, .05f), art.timber, Quaternion.Euler(-12, 0, 20));
            Part(PrimitiveType.Cube, t, new Vector3(-1.6f, 2.1f, 1.1f), new Vector3(.35f, .45f, .03f), art.timber, Quaternion.Euler(-12, 0, 20));
            Part(PrimitiveType.Cube, t, new Vector3(-1.5f, .9f, 1.3f), new Vector3(1, 1.8f, .6f), art.stone);    // woodstack for the fire
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
            for (int i = 0; i < 4; i++) Part(PrimitiveType.Cube, t, new Vector3(-2.4f + (i % 2) * .15f, .1f + i * .09f, -.6f), new Vector3(1, .08f, .7f), Tint(art.cloth, hides[i % 3]), Quaternion.Euler(0, i * 17, 0)); // pelts
            Part(PrimitiveType.Cube, t, new Vector3(-.4f, .55f, -.9f), new Vector3(.25f, .25f, 1.6f), dark, Quaternion.Euler(22, 0, 0));      // scraping beam
            Part(PrimitiveType.Cube, t, new Vector3(-.4f, .62f, -.9f), new Vector3(.3f, .04f, 1.2f), Tint(art.cloth, hides[2]), Quaternion.Euler(22, 0, 0));
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
                    if (Gloom)
                    {
                        float pick = R01;
                        if (pick < .4f) DeadTree(at, .6f + R01 * .5f, DeadBark(), 3, statics);
                        else if (pick < .82f) Pine(Root(new ZoneProp { kind = "pine", at = at, rotation = R01 * 360, scale = .85f + R01 * .5f }, statics));
                        else EdgeRock(at);
                        continue;
                    }
                    if (Zone.biome == "ash") { if (R01 < .55f) DeadTree(at, .6f + R01 * .5f, Tint(art.bark, new Color(.2f, .19f, .18f)), 3, statics); else EdgeRock(at); continue; }
                    var p = new ZoneProp { kind = Zone.biome == "mountain" ? (R01 < .7f ? "pine" : "rock") : R01 < .6f ? "pine" : "tree", at = at, rotation = R01 * 360, scale = .85f + R01 * .5f, variant = (int)(R01 * 4) };
                    if (p.kind == "rock") { EdgeRock(at); continue; }
                    var t = Root(p, statics);
                    if (p.kind == "pine") Pine(t); else Broadleaf(t, p.variant);
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
        /// <summary>A big weathered boulder for rocky edges (mountain and ash zones).</summary>
        void EdgeRock(Vector2 at)
        {
            var t = Root(new ZoneProp { kind = "rock", at = at, rotation = R01 * 360 }, statics);
            float s = 1.6f + R01 * 1.8f;
            var mat = Tint(art.stone, Zone.biome == "ash" ? new Color(.33f, .32f, .31f) : new Color(.42f, .41f, .39f));
            if (Zone.biome == "mountain")
            {
                // Sunk by the fall of the slope across it, so its downhill side never hangs in the air.
                float low = Mathf.Min(Mathf.Min(HeightAt(at.x + s, at.y), HeightAt(at.x - s, at.y)), Mathf.Min(HeightAt(at.x, at.y + s), HeightAt(at.x, at.y - s)));
                t.position += Vector3.down * (Mathf.Clamp(t.position.y - low, 0, .6f * s) + .15f * s);
            }
            Lump(Boulder(), t, new Vector3(0, .45f * s, 0), new Vector3(2.1f * s, 1.5f * s, 1.8f * s), mat, R01 * 360);
            Lump(Boulder(), t, new Vector3(.8f * s, .25f * s, .45f * s), new Vector3(1.2f * s, .9f * s, 1.1f * s), mat, R01 * 360);
            Solid(t, new Vector3(0, .6f * s, 0), new Vector3(1.8f * s, 1.2f * s, 1.5f * s));
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
            var paint = GroundMesh.GetComponent<MeshRenderer>().sharedMaterial;
            for (int side = 0; side < 4; side++)
            {
                var skirt = ZoneMeshes.Backdrop(Zone.size, GroundSegments, side, rings, H);
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
            var rockMat = Tint(art.stone, ash ? new Color(.36f, .355f, .36f) : new Color(.40f, .39f, .38f));
            var parts = new Dictionary<(Material, int), List<CombineInstance>>();
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
                        if (rock)
                        {
                            // A tor: tall and narrow, sunk deep enough that its downhill side never hangs over the slope.
                            float s = mountain ? 12 + R() * 14 : 5 + R() * 6;   // ash: low broken tors on the swells
                            Put(side, crag, rockMat, at + Vector3.down * s * .8f, Quaternion.Euler(0, yaw, 0), new Vector3(1.2f * s, 2.8f * s, 1.1f * s));
                        }
                        else if (pick < dead)
                        {
                            Put(side, spire, deadMat, at, Quaternion.identity, new Vector3(.45f, th, .45f));
                            for (int j = 0; j < 3; j++) Put(side, spire, deadMat, at + Vector3.up * th * (.45f + j * .14f), Quaternion.Euler(0, yaw + j * 120, 35 + R() * 20), new Vector3(.18f, th * .4f, .18f));
                        }
                        else if (pick < dead + pines)
                        {
                            // A bark trunk under lifted boughs, so a pine on a slope stands on it rather than hovering on a flat cone base.
                            Put(side, spire, art.bark, at, Quaternion.identity, new Vector3(.4f, th * .5f, .4f));
                            Put(side, spire, pineMat, at + Vector3.up * (1 + th * .16f), Quaternion.Euler(0, yaw, 0), new Vector3(th * .26f, th * .6f, th * .26f));
                            Put(side, spire, pineMat, at + Vector3.up * (1 + th * .52f), Quaternion.Euler(0, yaw + 30, 0), new Vector3(th * .18f, th * .56f, th * .18f));
                        }
                        else
                        {
                            float c = th * .45f; var leaf = Tint(art.foliage, Wither(Leaf[(int)(R() * 3)]));
                            Put(side, spire, art.bark, at, Quaternion.identity, new Vector3(.35f, th * .62f, .35f));
                            Put(side, crown, leaf, at + Vector3.up * th * .62f, Quaternion.Euler(0, yaw, 0), new Vector3(c, c * .85f, c));
                            Put(side, crown, leaf, at + new Vector3(c * .3f, th * .5f, c * .2f), Quaternion.Euler(0, yaw + 90, 0), new Vector3(c * .7f, c * .6f, c * .7f));
                        }
                    }
            }
            foreach (var kv in parts)
            {
                var mesh = new Mesh { name = "Backdrop " + kv.Key.Item1.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true); Show(mesh, kv.Key.Item1);
            }
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
                px = new Color32[128 * 64]; m.mainTexture = tex; m.mainTextureScale = new Vector2(14, 1);
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
            // The fog's colour (particle shaders double their tint), the curtain's pulled toward grey and a touch brighter.
            var f = RenderSettings.fogColor; float l = f.grayscale;
            var c = Color.Lerp(f, new Color(l, l, l * 1.03f), grey) * (.5f * gain); c.a = .5f; m.SetColor("_TintColor", c);
        }
        void Fill()
        {
            // Crawling grey static: thin at the ground (the land behind fades in with distance, no ruled bottom edge), thickest a
            // few metres up, dissolving upward into haze. Soft slanted bands crawl sideways through it, not rain-like streaks.
            float t = Time.time;
            for (int y = 0; y < 64; y++)
            {
                float h = y / 63f, body = Mathf.SmoothStep(0, 1, h / .3f) * (1 - Mathf.SmoothStep(0, 1, (h - .3f) / .7f));
                for (int x = 0; x < 128; x++)
                {
                    int v = rng.Next(170, 250);
                    float u = x * Mathf.PI / 64, band = .5f + .3f * Mathf.Sin(u * 3 + t * .25f + y * .19f) + .2f * Mathf.Sin(u * 5 - t * .45f + y * .31f + 1.7f);
                    byte a = (byte)Mathf.Clamp(rng.Next(40, 140) * body * (.4f + band * .9f), 0, 255);
                    px[y * 128 + x] = new Color32((byte)v, (byte)v, (byte)Mathf.Min(255, v + 4), a);
                }
            }
            tex.SetPixels32(px); tex.Apply(false);
        }
    }
}
