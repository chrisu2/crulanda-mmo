using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Instanced grass tufts and wildflowers on open ground (not roads, fields, clearings, water, groves or the unmade
    /// east). Tufts are baked into 24 m cells; only cells near the camera are drawn, via Graphics.DrawMeshInstanced.
    /// No shadows, no colliders: pure dressing. Materials come from ZoneArt (instancing-enabled cutout assets); the field
    /// draws its own copies of them, which shrink the tufts into the ground over the 20 m before the nearest cell that may
    /// be off (Grass.shader's _FadeFar), so the grass thins out into the turf's paint and no cell is seen switching.
    /// </summary>
    public sealed class GrassField : MonoBehaviour
    {
        const float Cell = 24, DrawDistance = 70;
        sealed class Batch { public Material material; public List<Matrix4x4[]> chunks = new List<Matrix4x4[]>(); }
        sealed class CellData { public Vector3 center; public readonly Dictionary<Material, List<Matrix4x4>> byMaterial = new Dictionary<Material, List<Matrix4x4>>(); public List<(Material, Matrix4x4[])> baked; }
        readonly List<CellData> cells = new List<CellData>();
        Mesh tuft;

        /// <summary>Bakes the tufts. Tall patches may use their own materials (<paramref name="tallGrass"/>), height and density scales.</summary>
        public void Build(ZoneBuilder zone, Material[] grass, Material[] flowers, System.Func<Vector2, float> openness, int seed, float density, List<(Vector2 center, float radius)> tall = null,
            Material[] tallGrass = null, float tallHeight = 1, float tallDensity = 1)
        {
            if (tallGrass == null || tallGrass.Length == 0) tallGrass = grass;
            if (grass == null || grass.Length == 0) return;
            tuft = TuftMesh();
            // Its own copies: fading by distance (a cell is drawn while its centre is within DrawDistance + Cell on both axes, so
            // every tuft within DrawDistance + Cell / 2 of the camera is drawn: the fade ends there); a bleached one for tall drifts.
            var own = new Dictionary<Material, Material>(); var bleached = new Dictionary<Material, Material>();
            Material Own(Material m)
            {
                if (!own.TryGetValue(m, out var c)) { own[m] = c = new Material(m) { name = m.name + " (field)", enableInstancing = true }; c.SetFloat("_FadeFar", DrawDistance + Cell / 2); }
                return c;
            }
            Material Bleached(Material m)
            {
                if (!bleached.TryGetValue(m, out var c)) { var k = m.color; float g = k.grayscale; bleached[m] = c = new Material(Own(m)) { name = m.name + " (bleached)", color = Color.Lerp(k, new Color(g * 1.25f, g * 1.2f, g * .75f, k.a), .35f), enableInstancing = true }; }
                return c;
            }
            var rng = new System.Random(seed);
            float half = zone.Half, size = zone.Zone.size;
            int perAxis = Mathf.CeilToInt(size / Cell);
            var grid = new CellData[perAxis, perAxis];
            int count = Mathf.RoundToInt(size * size * density);
            for (int i = 0; i < count; i++)
            {
                var p = new Vector2((float)rng.NextDouble() * size - half, (float)rng.NextDouble() * size - half);
                float open = openness(p);
                if (open <= 0 || rng.NextDouble() > open) continue;
                int cx = Mathf.Clamp((int)((p.x + half) / Cell), 0, perAxis - 1), cz = Mathf.Clamp((int)((p.y + half) / Cell), 0, perAxis - 1);
                var cell = grid[cx, cz];
                if (cell == null) { cell = grid[cx, cz] = new CellData { center = new Vector3(-half + (cx + .5f) * Cell, 0, -half + (cz + .5f) * Cell) }; cells.Add(cell); }
                bool flower = flowers != null && flowers.Length > 0 && rng.NextDouble() < .07;
                var mat = Own(flower ? flowers[rng.Next(flowers.Length)] : grass[rng.Next(grass.Length)]);
                float s = flower ? .45f + (float)rng.NextDouble() * .25f : .55f + (float)rng.NextDouble() * .6f;
                var m = Matrix4x4.TRS(zone.Ground(p, -.02f), Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0), new Vector3(s, s * (.8f + (float)rng.NextDouble() * .5f), s));
                if (!cell.byMaterial.TryGetValue(mat, out var list)) cell.byMaterial[mat] = list = new List<Matrix4x4>();
                list.Add(m);
            }
            // Tall grass patches: dense, knee-to-waist high, enough to hide a crouching wolf. A patch is no disc: its rim wobbles in
            // and out by bearing (.78 to 1.15 of the radius), the grass thins and shortens over the outer 40% (to half height at the
            // rim), every sixteenth tuft stands out past the rim, the height rolls a little across the patch and drifts of
            // bleached tufts run through it. The inner 60% is as dense and tall as it was (an ambusher lies within 30%).
            if (tall != null)
                foreach (var (center, radius) in tall)
                {
                    int n = Mathf.RoundToInt(Mathf.PI * radius * radius * 5 * tallDensity);
                    for (int i = 0; i < n; i++)
                    {
                        float a = (float)rng.NextDouble() * Mathf.PI * 2, u = Mathf.Sqrt((float)rng.NextDouble());
                        float rim = radius * (.78f + .37f * Mathf.Clamp01(Mathf.PerlinNoise(center.x * .31f + Mathf.Cos(a) * 1.3f + 40, center.y * .31f + Mathf.Sin(a) * 1.3f + 40)));
                        bool outlier = i % 16 == 7; float r = outlier ? rim * (1.02f + .28f * u) : u * rim;
                        var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        if (openness(p) <= 0 && Vector2.Distance(p, center) > radius * .3f) continue;
                        float edge = Mathf.Clamp01((r / rim - .6f) / .4f);   // 0 over the inner 60%, 1 at the rim and past it
                        if (!outlier && Hash(p) < edge * .7f) continue;       // thinner toward the rim
                        int cx = Mathf.Clamp((int)((p.x + half) / Cell), 0, perAxis - 1), cz = Mathf.Clamp((int)((p.y + half) / Cell), 0, perAxis - 1);
                        var cell = grid[cx, cz];
                        if (cell == null) { cell = grid[cx, cz] = new CellData { center = new Vector3(-half + (cx + .5f) * Cell, 0, -half + (cz + .5f) * Cell) }; cells.Add(cell); }
                        var pick = tallGrass[rng.Next(tallGrass.Length)];
                        var mat = Mathf.PerlinNoise(p.x * .3f + 17, p.y * .3f + 3) > .62f ? Bleached(pick) : Own(pick);
                        float s = (1.25f + (float)rng.NextDouble() * .7f) * Mathf.Lerp(1, .8f, edge);
                        float roll = (.9f + .3f * Mathf.Clamp01(Mathf.PerlinNoise(p.x * .22f + 9, p.y * .22f + 31))) * Mathf.Lerp(1, .5f, edge);
                        var m = Matrix4x4.TRS(zone.Ground(p, -.02f), Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0), new Vector3(s, s * (1.3f + (float)rng.NextDouble() * .5f) * tallHeight * roll, s));
                        if (!cell.byMaterial.TryGetValue(mat, out var list)) cell.byMaterial[mat] = list = new List<Matrix4x4>();
                        list.Add(m);
                    }
                }
            foreach (var cell in cells)
            {
                cell.baked = new List<(Material, Matrix4x4[])>();
                foreach (var kv in cell.byMaterial)
                    for (int start = 0; start < kv.Value.Count; start += 1023)
                        cell.baked.Add((kv.Key, kv.Value.GetRange(start, Mathf.Min(1023, kv.Value.Count - start)).ToArray()));
                cell.byMaterial.Clear();
            }
        }
        void Update()
        {
            var cam = Camera.main; if (cam == null || tuft == null) return;
            var at = cam.transform.position; float reach = DrawDistance + Cell;
            foreach (var cell in cells)
            {
                if (Mathf.Abs(cell.center.x - at.x) > reach || Mathf.Abs(cell.center.z - at.z) > reach) continue;
                foreach (var (mat, matrices) in cell.baked)
                    Graphics.DrawMeshInstanced(tuft, 0, mat, matrices, matrices.Length, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
            }
        }
        /// <summary>A steady 0..1 from a spot on the ground (to 6 cm), for a choice that must not draw from a stream.</summary>
        static float Hash(Vector2 p)
        {
            uint h = (uint)(Mathf.RoundToInt(p.x * 16) * 73856093) ^ (uint)(Mathf.RoundToInt(p.y * 16) * 19349663);
            h ^= h >> 15; h *= 2654435761u; h ^= h >> 13; return (h & 0xffffff) / 16777216f;
        }
        /// <summary>Three crossed quads, ~0.6 m tall, with UVs spanning the blade texture.</summary>
        static Mesh TuftMesh()
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>(); var n = new List<Vector3>();
            for (int i = 0; i < 3; i++)
            {
                var dir = Quaternion.Euler(0, i * 60, 0) * Vector3.right * .45f; int b = v.Count;
                v.Add(-dir); v.Add(dir); v.Add(dir + Vector3.up * .6f); v.Add(-dir + Vector3.up * .6f);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                for (int k = 0; k < 4; k++) n.Add(Vector3.up);   // upward normals: tufts light like the ground beneath them
                t.AddRange(new[] { b, b + 3, b + 2, b, b + 2, b + 1, b, b + 1, b + 2, b, b + 2, b + 3 });   // double-sided
            }
            var m = new Mesh { name = "Grass tuft" }; m.SetVertices(v); m.SetUVs(0, uv); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>Turns a mill's water wheel.</summary>
    public sealed class Spinner : MonoBehaviour
    {
        public Vector3 axis = Vector3.right; public float degreesPerSecond = 25;
        void Update() { transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self); }
    }
}
