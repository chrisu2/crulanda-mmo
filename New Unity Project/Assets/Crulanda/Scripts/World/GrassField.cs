using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Instanced grass tufts and wildflowers on open ground (not roads, fields, clearings, water, groves or the unmade
    /// east). Tufts are baked into 24 m cells; only cells near the camera are drawn, via Graphics.DrawMeshInstanced.
    /// No shadows, no colliders: pure dressing. Materials come from ZoneArt (instancing-enabled cutout assets).
    /// </summary>
    public sealed class GrassField : MonoBehaviour
    {
        const float Cell = 24, DrawDistance = 70;
        sealed class Batch { public Material material; public List<Matrix4x4[]> chunks = new List<Matrix4x4[]>(); }
        sealed class CellData { public Vector3 center; public readonly Dictionary<Material, List<Matrix4x4>> byMaterial = new Dictionary<Material, List<Matrix4x4>>(); public List<(Material, Matrix4x4[])> baked; }
        readonly List<CellData> cells = new List<CellData>();
        Mesh tuft;

        public void Build(ZoneBuilder zone, Material[] grass, Material[] flowers, System.Func<Vector2, float> openness, int seed, float density, List<(Vector2 center, float radius)> tall = null)
        {
            if (grass == null || grass.Length == 0) return;
            tuft = TuftMesh();
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
                var mat = flower ? flowers[rng.Next(flowers.Length)] : grass[rng.Next(grass.Length)];
                float s = flower ? .45f + (float)rng.NextDouble() * .25f : .55f + (float)rng.NextDouble() * .6f;
                var m = Matrix4x4.TRS(zone.Ground(p, -.02f), Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0), new Vector3(s, s * (.8f + (float)rng.NextDouble() * .5f), s));
                if (!cell.byMaterial.TryGetValue(mat, out var list)) cell.byMaterial[mat] = list = new List<Matrix4x4>();
                list.Add(m);
            }
            // Tall grass patches: dense, knee-to-waist high, enough to hide a crouching wolf.
            if (tall != null)
                foreach (var (center, radius) in tall)
                {
                    int n = Mathf.RoundToInt(Mathf.PI * radius * radius * 5);
                    for (int i = 0; i < n; i++)
                    {
                        float a = (float)rng.NextDouble() * Mathf.PI * 2, r = Mathf.Sqrt((float)rng.NextDouble()) * radius;
                        var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        if (openness(p) <= 0 && Vector2.Distance(p, center) > radius * .3f) continue;
                        int cx = Mathf.Clamp((int)((p.x + half) / Cell), 0, perAxis - 1), cz = Mathf.Clamp((int)((p.y + half) / Cell), 0, perAxis - 1);
                        var cell = grid[cx, cz];
                        if (cell == null) { cell = grid[cx, cz] = new CellData { center = new Vector3(-half + (cx + .5f) * Cell, 0, -half + (cz + .5f) * Cell) }; cells.Add(cell); }
                        var mat = grass[rng.Next(grass.Length)];
                        float s = 1.25f + (float)rng.NextDouble() * .7f;
                        var m = Matrix4x4.TRS(zone.Ground(p, -.02f), Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0), new Vector3(s, s * (1.3f + (float)rng.NextDouble() * .5f), s));
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
