using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Painted undergrowth (ZoneArt.fern, broadLeaf, reeds and the flowers): ferns in the shade round trunks, in the groves and along
    /// the forest edge; broad-leaved plants along the water; reeds at the waterline; flowers in drifts of one colour across open
    /// ground. Instanced cards baked into 24 m cells and drawn near the camera (as GrassField), wherever grass may grow (Openness:
    /// never on a road, a yard, a field, in a building, a cave or the unmade). Each biome has its own mix: the meadow's, the
    /// mountain's few ferns and alpine flowers, Khaven's withered ferns and dark reeds, the verdant shore's riot (ferns everywhere,
    /// glowing flowers at night); nothing grows on the ash. The edge ferns and the flower drifts run on past the zone's edge
    /// over the backdrop's near slope and thin out to nothing there. Its own random stream: the zone's layout is untouched.
    /// </summary>
    public sealed class PlantField : MonoBehaviour
    {
        const float Cell = 24, DrawDistance = 90;
        sealed class CellData { public Vector3 center; public readonly Dictionary<(Mesh, Material), List<Matrix4x4>> lists = new Dictionary<(Mesh, Material), List<Matrix4x4>>(); public List<(Mesh, Material, Matrix4x4[])> baked; }
        readonly List<CellData> cells = new List<CellData>();
        CellData[,] grid; float half; int perAxis;
        Mesh fernMesh, leafMesh, reedMesh, tuftMesh;
        /// <summary>How many plants of each kind were planted (tests and the build log).</summary>
        public int Ferns { get; private set; }
        public int BroadLeaves { get; private set; }
        public int Reeds { get; private set; }
        public int Flowers { get; private set; }

        /// <summary>The biome's mix: plants a square metre in each kind of place (0 = none).</summary>
        struct Mix { public float fernShade, fernWood, fernOpen, fernTrunk, leafWater, reedLine, flowerDrift, glow, wither; public Color fernTint, leafTint, reedTint; public bool flowers; }
        static Mix MixFor(string biome)
        {
            switch (biome)
            {
                case "verdant": return new Mix { fernTrunk = 10, fernShade = .5f, fernWood = .32f, fernOpen = .05f, leafWater = .32f, reedLine = .55f, flowerDrift = .9f, glow = 1, flowers = true,
                    fernTint = new Color(1, 1.05f, 1), leafTint = Color.white, reedTint = Color.white };
                case "mountain": return new Mix { fernTrunk = 1.5f, fernShade = .08f, fernWood = .05f, leafWater = .04f, reedLine = .35f, flowerDrift = .25f, flowers = true,
                    fernTint = new Color(.82f, .9f, .82f), leafTint = new Color(.85f, .92f, .85f), reedTint = new Color(.85f, .88f, .8f) };
                case "gloom": return new Mix { fernTrunk = 1.2f, fernShade = .08f, fernWood = .05f, reedLine = .3f, wither = .85f,
                    fernTint = new Color(.7f, .62f, .5f), reedTint = new Color(.55f, .5f, .45f) };   // withered: the paint's green drained; no flowers
                case "ash": return new Mix();
                default: return new Mix { fernTrunk = 4, fernShade = .22f, fernWood = .14f, leafWater = .18f, reedLine = .45f, flowerDrift = .45f, flowers = true,
                    fernTint = Color.white, leafTint = Color.white, reedTint = Color.white };
            }
        }

        /// <summary>Bakes the plants. With <paramref name="edgeOpenness"/> and <paramref name="edgeGround"/> (what may grow at a point
        /// past the edge, and the ground there) the edge's plants run on <paramref name="edgeReach"/> metres past it.</summary>
        public void Build(ZoneBuilder zone, ZoneArt art, Material[] flowers, System.Func<Vector2, float> openness, int seed,
            System.Func<Vector2, float> edgeOpenness = null, System.Func<Vector2, float, Vector3> edgeGround = null, float edgeReach = 0)
        {
            Material[] glowing = null;
            var z = zone.Zone; var mix = MixFor(z.biome);
            if (art == null || (art.fern == null && art.broadLeaf == null && art.reeds == null)) return;
            var rng = new System.Random(seed); float R() { return (float)rng.NextDouble(); }
            half = zone.Half; perAxis = Mathf.CeilToInt(z.size / Cell); grid = new CellData[perAxis, perAxis];
            fernMesh = Rosette("Fern", 7, 1, .46f, 62, .28f); leafMesh = Rosette("Broad leaves", 5, .78f, .62f, 48, .2f); reedMesh = Clump("Reeds", 1.7f, .95f); tuftMesh = Clump("Flower clump", .62f, .9f);
            var fern = Tinted(art.fern, mix.fernTint, mix.wither); var leaf = Tinted(art.broadLeaf, mix.leafTint, mix.wither); var reed = Tinted(art.reeds, mix.reedTint, mix.wither);
            // Where a plant may stand: open to growth, not in the water (reeds excepted), and not on a trunk.
            bool Ground(Vector2 p) { return openness(p) > 0; }
            void Plant(Mesh mesh, Material mat, Vector2 p, float size, float lift = -.03f, bool past = false)
            {
                if (mesh == null || mat == null) return;
                int cx = Mathf.Clamp((int)((p.x + half) / Cell), 0, perAxis - 1), cz = Mathf.Clamp((int)((p.y + half) / Cell), 0, perAxis - 1);
                var cell = grid[cx, cz];
                if (cell == null) { cell = grid[cx, cz] = new CellData { center = new Vector3(-half + (cx + .5f) * Cell, 0, -half + (cz + .5f) * Cell) }; cells.Add(cell); }
                var key = (mesh, mat); if (!cell.lists.TryGetValue(key, out var list)) cell.lists[key] = list = new List<Matrix4x4>();
                list.Add(Matrix4x4.TRS(past ? edgeGround(p, lift) : zone.Ground(p, lift), Quaternion.Euler(0, R() * 360, 0), new Vector3(size, size * (.85f + R() * .3f), size)));
            }
            // Ferns round the trunks (in their shade), in the woods, along the forest edge, and (on the verdant shore) in the open too.
            if (fern != null)
            {
                foreach (var t in zone.Trunks)
                {
                    int n = Mathf.RoundToInt(mix.fernTrunk * (.6f + R() * .8f));
                    for (int k = 0; k < n; k++)
                    {
                        float a = R() * Mathf.PI * 2, r = 1.3f + R() * (z.biome == "verdant" ? 8 : 5);
                        var p = t + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        if (Ground(p) && NotOnTrunk(zone, p)) { Plant(fernMesh, fern, p, .7f + R() * .7f); Ferns++; }
                    }
                }
                foreach (var g in z.groves)
                {
                    if (g == null || g.kind == "orchard") continue;
                    float density = g.kind == "dead" ? mix.fernWood * .4f : mix.fernWood;
                    int n = Mathf.RoundToInt(g.size.x * g.size.y * density);
                    for (int k = 0; k < n; k++)
                    {
                        var p = g.center + new Vector2((R() - .5f) * g.size.x, (R() - .5f) * g.size.y);
                        if (Ground(p) && NotOnTrunk(zone, p)) { Plant(fernMesh, fern, p, .6f + R() * .8f); Ferns++; }
                    }
                }
                float band = 14, edgeArea = 4 * z.size * band;
                for (int k = 0, n = Mathf.RoundToInt(edgeArea * mix.fernShade); k < n; k++)
                {
                    float along = (R() - .5f) * z.size, inward = half - 3 - R() * band; int side = (int)(R() * 4);
                    var p = side == 0 ? new Vector2(-inward, along) : side == 1 ? new Vector2(inward, along) : side == 2 ? new Vector2(along, -inward) : new Vector2(along, inward);
                    if (Ground(p) && NotOnTrunk(zone, p)) { Plant(fernMesh, fern, p, .7f + R() * .8f); Ferns++; }
                }
                for (int k = 0, n = Mathf.RoundToInt(z.size * z.size * mix.fernOpen); k < n; k++)
                {
                    var p = new Vector2((R() - .5f) * z.size, (R() - .5f) * z.size);
                    if (Ground(p) && openness(p) > .3f && Mathf.PerlinNoise(p.x * .05f + 9, p.y * .05f + 4) > .45f && NotOnTrunk(zone, p)) { Plant(fernMesh, fern, p, .5f + R() * .6f); Ferns++; }
                }
            }
            // Broad leaves along the banks, reeds at the waterline: candidates over the zone, kept where the shore is right.
            if (zone.Water != null && (leaf != null || reed != null) && (mix.leafWater > 0 || mix.reedLine > 0))
            {
                // Reeds stand in clumps here and there along the water, not in a line down every bank (Chris, 2026-10-05: "way too
                // many. should be clumped together in random spots"): only where a slow noise is high, a sixth or so of the shore.
                float rx = R() * 500, ry = R() * 500;
                for (int k = 0, n = Mathf.RoundToInt(z.size * z.size * .35f); k < n; k++)
                {
                    var p = new Vector2((R() - .5f) * z.size, (R() - .5f) * z.size); float pick = R(), size = R();
                    float shore = zone.Water.Shore(p, 5); if (shore >= 5) continue;
                    if (shore > -.7f && shore < .45f && reed != null && pick < mix.reedLine * 1.4f && Mathf.PerlinNoise(rx + p.x * .045f, ry + p.y * .045f) > .66f) { Plant(reedMesh, reed, p, .75f + size * .5f, -.1f); Reeds++; }
                    else if (shore > 1 && shore < 4.5f && leaf != null && pick < mix.leafWater && Ground(p) && NotOnTrunk(zone, p)) { Plant(leafMesh, leaf, p, .7f + size * .6f); BroadLeaves++; }
                }
            }
            // Flowers in drifts: a patch of one colour where the drift noise is high, the colour from a second, slower noise.
            if (mix.flowers && flowers != null && flowers.Length > 0 && mix.flowerDrift > 0)
            {
                var glow = glowing = mix.glow > 0 ? Glowing(flowers) : null;
                for (int k = 0, n = Mathf.RoundToInt(z.size * z.size * .12f); k < n; k++)
                {
                    var p = new Vector2((R() - .5f) * z.size, (R() - .5f) * z.size); float keep = R(), size = R();
                    float drift = Mathf.PerlinNoise(p.x * .045f + 31, p.y * .045f + 17);
                    if (drift < .58f || keep > mix.flowerDrift * (drift - .58f) * 4 || !Ground(p) || openness(p) < .35f) continue;
                    int hue = Mathf.Min(flowers.Length - 1, (int)(Mathf.PerlinNoise(p.x * .012f + 7, p.y * .012f + 3) * flowers.Length * 1.2f));
                    bool lit = glow != null && Mathf.PerlinNoise(p.x * .02f + 50, p.y * .02f + 60) > .62f;
                    Plant(tuftMesh, lit ? glow[hue] : flowers[hue], p, .8f + size * .5f); Flowers++;
                }
            }
            // Past the edge: the forest edge's ferns (which stop 3 m short of the line inside) and the flower drifts run on over the
            // backdrop's near slope, thinning to nothing by edgeReach, so the dressing never stops on a line (the grass does the
            // same: GrassField.BuildEdge). They go in the edge cells. Drawn after everything else: every plant inside the zone
            // stands where it did.
            if (edgeOpenness != null && edgeGround != null && edgeReach > 0)
            {
                float span = z.size + edgeReach;   // each side's strip takes one corner
                Vector2 Spot(float from, out float thin)
                {
                    int side = (int)(R() * 4); float a = R() * span, o = from + R() * (edgeReach - from);
                    var p = side == 0 ? new Vector2(half - a, -half - o) : side == 1 ? new Vector2(half + o, half - a) : side == 2 ? new Vector2(-half + a, half + o) : new Vector2(-half - o, -half + a);
                    thin = Mathf.SmoothStep(1, 0, (Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half) / edgeReach); return p;
                }
                if (fern != null)
                    for (int k = 0, n = Mathf.RoundToInt(4 * span * (edgeReach + 3) * mix.fernShade); k < n; k++)
                    {
                        var p = Spot(-3, out float thin); float keep = R(), size = R();
                        if (keep < thin && edgeOpenness(p) > 0 && NotOnTrunk(zone, p)) { Plant(fernMesh, fern, p, .7f + size * .8f, -.03f, true); Ferns++; }
                    }
                if (mix.flowers && flowers != null && flowers.Length > 0 && mix.flowerDrift > 0)
                    for (int k = 0, n = Mathf.RoundToInt(4 * span * edgeReach * .12f); k < n; k++)
                    {
                        var p = Spot(0, out float thin); float keep = R(), size = R();
                        float drift = Mathf.PerlinNoise(p.x * .045f + 31, p.y * .045f + 17);
                        if (drift < .58f || keep > mix.flowerDrift * (drift - .58f) * 4 * thin || edgeOpenness(p) < .35f) continue;
                        int hue = Mathf.Min(flowers.Length - 1, (int)(Mathf.PerlinNoise(p.x * .012f + 7, p.y * .012f + 3) * flowers.Length * 1.2f));
                        bool lit = glowing != null && Mathf.PerlinNoise(p.x * .02f + 50, p.y * .02f + 60) > .62f;
                        Plant(tuftMesh, lit ? glowing[hue] : flowers[hue], p, .8f + size * .5f, -.03f, true); Flowers++;
                    }
            }
            foreach (var cell in cells)
            {
                cell.baked = new List<(Mesh, Material, Matrix4x4[])>();
                foreach (var kv in cell.lists)
                    for (int start = 0; start < kv.Value.Count; start += 1023)
                        cell.baked.Add((kv.Key.Item1, kv.Key.Item2, kv.Value.GetRange(start, Mathf.Min(1023, kv.Value.Count - start)).ToArray()));
                cell.lists.Clear();
            }
        }
        /// <summary>Not inside a trunk: a metre and more from every standing trunk (a giant tree's buttresses reach further).</summary>
        static bool NotOnTrunk(ZoneBuilder zone, Vector2 p) { return zone.TrunkClear(p, 1.1f); }
        static Material Tinted(Material m, Color tint, float wither = 0)
        {
            if (m == null) return null; if (tint == Color.white && wither <= 0) return m;
            var t = new Material(m) { name = m.name + " (tinted)", color = m.color * tint, enableInstancing = true };
            if (wither > 0 && t.HasProperty("_Wither")) t.SetFloat("_Wither", wither);
            return t;
        }
        /// <summary>Night-glowing copies of the flowers (the verdant shore's): the grass shader's glow, strongest after dark.</summary>
        static Material[] Glowing(Material[] flowers)
        {
            var lit = new Material[flowers.Length];
            for (int i = 0; i < flowers.Length; i++) { lit[i] = new Material(flowers[i]) { name = flowers[i].name + " (glowing)", enableInstancing = true }; lit[i].SetFloat("_Glow", 1.4f); }
            return lit;
        }
        /// <summary>
        /// A rosette of <paramref name="count"/> cards from the root: each rises at <paramref name="lean"/> degrees from upright and
        /// arches over (a second, flatter segment), <paramref name="length"/> long and <paramref name="width"/> wide, uv v 0 at the root
        /// and 1 at the tip (the sway and the paint follow it). Normals point up: lit like the ground under them.
        /// </summary>
        static Mesh Rosette(string name, int count, float length, float width, float lean, float droop)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var n = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < count; i++)
            {
                float yaw = i * 360f / count + (i % 2) * 17; var q = Quaternion.Euler(0, yaw, 0);
                var up1 = Quaternion.Euler(lean, 0, 0) * Vector3.up; var up2 = Quaternion.Euler(lean + 30 + droop * 60, 0, 0) * Vector3.up;   // rising, then arching over
                var a = Vector3.zero; var mid = a + up1 * length * .55f; var tip = mid + up2 * length * .45f;
                var side = Vector3.right * width * .5f; int b = v.Count;
                foreach (var (p, vv, w) in new[] { (a, 0f, .35f), (mid, .55f, 1f), (tip, 1f, .7f) })
                {
                    v.Add(q * (p - side * w)); v.Add(q * (p + side * w)); uv.Add(new Vector2(.5f - w * .5f, vv)); uv.Add(new Vector2(.5f + w * .5f, vv));
                    n.Add(Vector3.up); n.Add(Vector3.up);
                }
                t.AddRange(new[] { b, b + 2, b + 3, b, b + 3, b + 1, b + 2, b + 4, b + 5, b + 2, b + 5, b + 3 });
            }
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetUVs(0, uv); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
        }
        /// <summary>Three crossed upright cards, <paramref name="height"/> tall and <paramref name="width"/> wide (reeds, flower clumps).</summary>
        static Mesh Clump(string name, float height, float width)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var n = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                var dir = Quaternion.Euler(0, i * 60, 0) * Vector3.right * width * .5f; int b = v.Count;
                v.Add(-dir); v.Add(dir); v.Add(dir + Vector3.up * height); v.Add(-dir + Vector3.up * height);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                for (int k = 0; k < 4; k++) n.Add(Vector3.up);
                t.AddRange(new[] { b, b + 3, b + 2, b, b + 2, b + 1 });
            }
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetUVs(0, uv); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
        }
        void Update()
        {
            var cam = Camera.main; if (cam == null) return;
            var at = cam.transform.position; float reach = DrawDistance + Cell;
            foreach (var cell in cells)
            {
                if (Mathf.Abs(cell.center.x - at.x) > reach || Mathf.Abs(cell.center.z - at.z) > reach) continue;
                foreach (var (mesh, mat, matrices) in cell.baked)
                    Graphics.DrawMeshInstanced(mesh, 0, mat, matrices, matrices.Length, null, UnityEngine.Rendering.ShadowCastingMode.Off, true);
            }
        }
    }
}
