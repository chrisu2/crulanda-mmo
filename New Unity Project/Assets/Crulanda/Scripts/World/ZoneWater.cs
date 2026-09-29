using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The single source of truth for a zone's water. Creeks and lakes each get a water LEVEL computed once from the
    /// uncarved terrain. The terrain is carved relative to that level, the surface meshes are built at it (reaching
    /// exactly to the real waterline), and WaterAt() answers gameplay questions (wading, swimming, splashes) from the
    /// same numbers, so what you see and what you touch always agree.
    ///
    /// Creeks:
    /// - The path is densified to 3 m rows and each row takes level = (lowest of the centre and both banks) - 0.42.
    /// - Levels only ever fall along the flow: the higher end is the source, so water never runs uphill.
    /// - Where the land rises the creek cuts a valley. Its banks terrace down to the water and blend back into the hills.
    /// - The channel below that is carved depth metres under the bank (the water is depth - 0.42 m deep in the middle).
    ///
    /// Lakes:
    /// - A flat level just under the lowest ground in a ring around the waterline.
    /// - A flat-bottomed bowl whose underwater bank stays under about 38 degrees, so you can always wade out.
    /// - A gentle shore rising from the waterline.
    /// </summary>
    public sealed class ZoneWater
    {
        public const float BankDrop = .42f;      // bank to water surface
        public const float SwimDepth = 1.35f;    // deeper than this, you swim
        public sealed class Creek
        {
            public ZonePath def; public Vector2[] pts; public float[] level; public float width, depth;
            /// <summary>Waterline half-width on level banks, and the drawn half-width (a little past it, tucked into the bank).</summary>
            public float waterHalf, drawHalf, swimHalf; public Rect box;
        }
        public sealed class Lake { public ZoneLake def; public float level, radius, drawRadius, swimRadius, bottom, ramp; }
        public readonly List<Creek> Creeks = new List<Creek>();
        public readonly List<Lake> Lakes = new List<Lake>();

        /// <summary>Prepares levels from the uncarved height function. Call before any carved height is asked for.</summary>
        public void Prepare(ZoneDefinition zone, Func<float, float, float> uncarved)
        {
            float half = zone.size / 2 - 1;
            foreach (var w in zone.water)
            {
                if (w == null || w.points == null || w.points.Length < 2) continue;
                var c = new Creek { def = w, width = Mathf.Max(1, w.width), depth = Mathf.Max(.5f, w.depth > 0 ? w.depth : 1.1f) };
                var raw = new List<Vector2>();
                foreach (var p in w.points) raw.Add(new Vector2(Mathf.Clamp(p.x, -half, half), Mathf.Clamp(p.y, -half, half)));
                var pts = Densify(raw, 3);
                var lv = new float[pts.Count];
                for (int i = 0; i < pts.Count; i++)
                {
                    var dir = i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1];
                    var n = new Vector2(-dir.y, dir.x).normalized * c.width;
                    lv[i] = Mathf.Min(uncarved(pts[i].x, pts[i].y), Mathf.Min(uncarved(pts[i].x + n.x, pts[i].y + n.y), uncarved(pts[i].x - n.x, pts[i].y - n.y))) - BankDrop;
                }
                // The higher end is the source; from there the level may only fall.
                if (lv[lv.Length - 1] > lv[0]) { pts.Reverse(); Array.Reverse(lv); }
                for (int i = 1; i < lv.Length; i++) lv[i] = Mathf.Min(lv[i], lv[i - 1]);
                c.pts = pts.ToArray(); c.level = lv;
                float waterDepth = c.depth - BankDrop;
                c.waterHalf = waterDepth <= 0 ? 0 : c.width * (1 - InverseSmooth(BankDrop / c.depth));
                c.drawHalf = Mathf.Min(c.width, c.waterHalf + .35f);
                c.swimHalf = c.depth - BankDrop > SwimDepth ? c.width * (1 - InverseSmooth((BankDrop + SwimDepth) / c.depth)) : 0;
                float xmin = float.MaxValue, xmax = float.MinValue, ymin = float.MaxValue, ymax = float.MinValue;
                foreach (var p in c.pts) { xmin = Mathf.Min(xmin, p.x); xmax = Mathf.Max(xmax, p.x); ymin = Mathf.Min(ymin, p.y); ymax = Mathf.Max(ymax, p.y); }
                float m = c.width + 16;   // valley blending can reach a little past the channel
                c.box = Rect.MinMaxRect(xmin - m, ymin - m, xmax + m, ymax + m);
                Creeks.Add(c);
            }
            if (zone.lakes != null)
                foreach (var l in zone.lakes)
                {
                    if (l == null || l.radius < 2) continue;
                    var k = new Lake { def = l, radius = l.radius };
                    // Level: just under the lowest natural ground in a ring from the waterline out 3 m, so the shore
                    // always rises from the water and never lets it spill.
                    float low = float.MaxValue;
                    foreach (float ring in new[] { 0f, 1.5f, 3f })
                        for (int a = 0; a < 24; a++)
                        {
                            float ang = a * Mathf.PI / 12; var p = l.center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (l.radius + ring);
                            low = Mathf.Min(low, uncarved(p.x, p.y));
                        }
                    k.level = low - .05f;
                    float d = Mathf.Max(.8f, l.depth);
                    k.bottom = k.level - d;
                    k.ramp = Mathf.Min(l.radius, Mathf.Max(.55f * l.radius, 1.9f * d));   // underwater bank width (slope <= ~38 deg)
                    k.drawRadius = l.radius + .3f;
                    k.swimRadius = d > SwimDepth ? l.radius - k.ramp * InverseSmooth(SwimDepth / d) : 0;
                    Lakes.Add(k);
                }
        }

        /// <summary>Carved height at a point, from its natural height h.</summary>
        public float Carve(float x, float z, float h)
        {
            var p = new Vector2(x, z);
            foreach (var c in Creeks)
            {
                if (!c.box.Contains(p)) continue;
                Project(c, p, out float d, out float level);
                float bank = level + BankDrop, hh = h;
                if (h > bank)
                {
                    // Valley: terrace down to the bank beside the water, blending back into the hills over a width that
                    // grows with the cut (about 22 degrees at most).
                    float blend = Mathf.Max(2, 2.5f * (h - bank));
                    float wgt = Mathf.SmoothStep(0, 1, Mathf.Clamp01(1 - (d - c.width) / blend));
                    hh = Mathf.Lerp(h, bank, wgt);
                }
                if (d < c.width) hh = Mathf.Min(hh, bank - c.depth * Mathf.SmoothStep(0, 1, 1 - d / c.width));
                h = Mathf.Min(h, hh);
            }
            foreach (var k in Lakes)
            {
                float d = Vector2.Distance(p, k.def.center);
                if (d > k.radius + 20) continue;
                float target = d <= k.radius
                    ? Mathf.Lerp(k.bottom, k.level, Mathf.SmoothStep(0, 1, Mathf.Clamp01((d - (k.radius - k.ramp)) / k.ramp)))
                    : k.level + (d - k.radius) * .38f;   // gentle shore (~21 degrees) until it meets the land
                h = Mathf.Min(h, target);
            }
            return h;
        }

        /// <summary>
        /// Water at a point: the surface height and how deep it is over the given (carved) ground. False on dry land.
        /// Uses the same levels and extents the surface meshes were built from.
        /// </summary>
        public bool At(Vector2 p, float ground, out float surface, out float depth)
        {
            surface = depth = 0;
            foreach (var k in Lakes)
                if (Vector2.Distance(p, k.def.center) < k.drawRadius) { surface = k.level; depth = surface - ground; if (depth > 0) return true; }
            foreach (var c in Creeks)
            {
                if (c.drawHalf <= 0 || !c.box.Contains(p)) continue;
                Project(c, p, out float d, out float level);
                if (d > c.drawHalf) continue;
                surface = level; depth = level - ground;
                if (depth > 0) return true;
            }
            return false;
        }
        public bool NearWater(Vector2 p, float margin)
        {
            foreach (var k in Lakes) if (Vector2.Distance(p, k.def.center) < k.radius + margin) return true;
            foreach (var c in Creeks) { if (!c.box.Contains(p)) continue; Project(c, p, out float d, out _); if (d < c.width + margin) return true; }
            return false;
        }

        /// <summary>Distance to the creek's centre line and the interpolated water level there.</summary>
        public static void Project(Creek c, Vector2 p, out float distance, out float level)
        {
            distance = float.MaxValue; level = c.level[0];
            for (int i = 0; i + 1 < c.pts.Length; i++)
            {
                Vector2 a = c.pts[i], b = c.pts[i + 1], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
                float d = Vector2.Distance(p, a + ab * t);
                if (d < distance) { distance = d; level = Mathf.Lerp(c.level[i], c.level[i + 1], t); }
            }
        }
        /// <summary>Inverse of SmoothStep(0,1,x) on [0,1] (bisection).</summary>
        public static float InverseSmooth(float y)
        {
            y = Mathf.Clamp01(y); float lo = 0, hi = 1;
            for (int i = 0; i < 30; i++) { float m = (lo + hi) / 2; if (m * m * (3 - 2 * m) < y) lo = m; else hi = m; }
            return (lo + hi) / 2;
        }
        static List<Vector2> Densify(List<Vector2> pts, float step)
        {
            var list = new List<Vector2>();
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(pts[i], pts[i + 1]) / step));
                for (int k = 0; k < n; k++) list.Add(Vector2.Lerp(pts[i], pts[i + 1], (float)k / n));
            }
            list.Add(pts[pts.Count - 1]);
            return list;
        }

        // ---------- surface meshes ----------
        /// <summary>
        /// A creek's surface as chunks of about 24 m (for sorting, culling and light selection). There are nine vertices
        /// across, all at the row's level. UV runs across and along in metres, and uv2.x = 1 marks flowing water.
        /// Vertex alpha is the shallowness (1 at the waterline), from the real depth under each vertex.
        /// </summary>
        public List<Mesh> CreekMeshes(Creek c, Func<float, float, float> ground)
        {
            var meshes = new List<Mesh>();
            if (c.drawHalf <= 0) return meshes;
            float[] across = { -1, -.8f, -.6f, -.3f, 0, .3f, .6f, .8f, 1 };
            const int Rows = 8;
            float along = 0;
            var alongAt = new float[c.pts.Length];
            for (int i = 1; i < c.pts.Length; i++) { along += Vector2.Distance(c.pts[i], c.pts[i - 1]); alongAt[i] = along; }
            for (int start = 0; start < c.pts.Length - 1; start += Rows)
            {
                int end = Mathf.Min(c.pts.Length - 1, start + Rows);
                var v = new List<Vector3>(); var col = new List<Color>(); var uv = new List<Vector2>(); var uv2 = new List<Vector2>(); var t = new List<int>();
                for (int i = start; i <= end; i++)
                {
                    var dir = i < c.pts.Length - 1 ? c.pts[i + 1] - c.pts[i] : c.pts[i] - c.pts[i - 1];
                    var side = new Vector2(-dir.y, dir.x).normalized;
                    for (int k = 0; k < across.Length; k++)
                    {
                        float off = across[k] * c.drawHalf; var p = c.pts[i] + side * off;
                        v.Add(new Vector3(p.x, c.level[i], p.y));
                        float depth = c.level[i] - ground(p.x, p.y);
                        col.Add(new Color(1, 1, 1, 1 - Mathf.Clamp01(depth / .8f)));
                        uv.Add(new Vector2(off, alongAt[i])); uv2.Add(new Vector2(1, 0));
                    }
                    if (i > start)
                    {
                        int b = (i - start - 1) * across.Length, n = across.Length;
                        for (int k = 0; k < n - 1; k++) t.AddRange(new[] { b + k, b + k + 1, b + n + k, b + k + 1, b + n + k + 1, b + n + k });
                    }
                }
                meshes.Add(Finish("Creek surface", v, t, col, uv, uv2));
            }
            return meshes;
        }
        /// <summary>A lake's level disc (rings out to just past the waterline), with alpha from the real depth.</summary>
        public Mesh LakeMesh(Lake k, Func<float, float, float> ground)
        {
            float[] rings = { 0, .25f, .5f, .7f, .84f, .93f, .98f, 1 };
            const int Seg = 48; var c = k.def.center;
            var v = new List<Vector3>(); var col = new List<Color>(); var uv = new List<Vector2>(); var uv2 = new List<Vector2>(); var t = new List<int>();
            void Add(Vector2 local)
            {
                v.Add(new Vector3(local.x, k.level, local.y));
                float depth = k.level - ground(c.x + local.x, c.y + local.y);
                col.Add(new Color(1, 1, 1, 1 - Mathf.Clamp01(depth / .8f)));
                uv.Add(local); uv2.Add(Vector2.zero);
            }
            Add(Vector2.zero);
            for (int r = 1; r < rings.Length; r++)
                for (int s = 0; s < Seg; s++) { float a = s * Mathf.PI * 2 / Seg; Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * k.drawRadius * rings[r]); }
            // Upward-facing winding (clockwise seen from above in Unity's left-handed frame).
            for (int s = 0; s < Seg; s++) t.AddRange(new[] { 0, 1 + (s + 1) % Seg, 1 + s });
            for (int r = 1; r < rings.Length - 1; r++)
                for (int s = 0; s < Seg; s++)
                {
                    int a0 = 1 + (r - 1) * Seg + s, a1 = 1 + (r - 1) * Seg + (s + 1) % Seg, b0 = a0 + Seg, b1 = a1 + Seg;
                    t.AddRange(new[] { a0, a1, b0, a1, b1, b0 });
                }
            return Finish("Lake surface", v, t, col, uv, uv2);
        }
        static Mesh Finish(string name, List<Vector3> v, List<int> t, List<Color> col, List<Vector2> uv, List<Vector2> uv2)
        {
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetTriangles(t, 0); m.SetColors(col); m.SetUVs(0, uv); m.SetUVs(1, uv2);
            var n = new Vector3[v.Count]; for (int i = 0; i < n.Length; i++) n[i] = Vector3.up;
            m.normals = n; m.RecalculateTangents(); m.RecalculateBounds();
            return m;
        }
    }
}
