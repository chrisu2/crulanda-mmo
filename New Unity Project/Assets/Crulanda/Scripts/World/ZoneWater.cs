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
    /// - The path is densified to 3 m rows, its corners rounded, and it meanders gently with a width that breathes along
    ///   the flow (see Meander), except where bridges, the mill and roads beside it hold it to its drawn line.
    /// - Each row takes level = (lowest of the centre and both banks) - 0.42.
    /// - Levels only ever fall along the flow: the higher end is the source, so water never runs uphill.
    /// - Where the land rises the creek cuts a valley. Its banks terrace down to the water and blend back into the hills.
    /// - The channel below that is carved depth metres under the bank (the water is depth - 0.42 m deep in the middle).
    ///
    /// Lakes:
    /// - An irregular waterline: the radius swells and pulls in smoothly around the shore (RadiusAt), never a circle.
    /// - A flat level just under the lowest ground in a ring around the waterline.
    /// - A flat-bottomed bowl whose underwater bank stays under about 38 degrees, so you can always wade out.
    /// - A gentle shore rising from the waterline and easing into the land over a soft lip (no crease).
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
            /// <summary>Width factor per row: the creek widens and narrows along its flow. The half-widths above are for 1.</summary>
            public float[] wide;
            /// <summary>Drawn half-width where the width factor is <paramref name="w"/> (on row i: wide[i]).</summary>
            public float DrawHalf(float w) { return Mathf.Min(width * w, waterHalf * w + .35f); }
        }
        public sealed class Lake
        {
            public ZoneLake def; public float level, radius, swimInset, bottom, ramp; public Vector3 phase;
            /// <summary>
            /// Waterline radius toward <paramref name="dir"/> (from the centre): the mean radius swelling and pulling in smoothly
            /// by up to a fifth around the shore (three low harmonics, phased per lake), so no pond is a circle.
            /// </summary>
            public float RadiusAt(Vector2 dir)
            {
                float a = Mathf.Atan2(dir.y, dir.x);
                return radius * (1 + .09f * Mathf.Sin(2 * a + phase.x) + .07f * Mathf.Sin(3 * a + phase.y) + .04f * Mathf.Sin(5 * a + phase.z));
            }
            /// <summary>The drawn edge: a little past the waterline, tucked into the shore.</summary>
            public float DrawRadiusAt(Vector2 dir) { return RadiusAt(dir) + .3f; }
        }
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
                Meander(zone, c, pts, half);
                var lv = new float[pts.Count];
                for (int i = 0; i < pts.Count; i++)
                {
                    var dir = i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1];
                    var n = new Vector2(-dir.y, dir.x).normalized * c.width * c.wide[i];
                    lv[i] = Mathf.Min(uncarved(pts[i].x, pts[i].y), Mathf.Min(uncarved(pts[i].x + n.x, pts[i].y + n.y), uncarved(pts[i].x - n.x, pts[i].y - n.y))) - BankDrop;
                }
                // The higher end is the source; from there the level may only fall.
                if (lv[lv.Length - 1] > lv[0]) { pts.Reverse(); Array.Reverse(lv); Array.Reverse(c.wide); }
                for (int i = 1; i < lv.Length; i++) lv[i] = Mathf.Min(lv[i], lv[i - 1]);
                c.pts = pts.ToArray(); c.level = lv;
                float waterDepth = c.depth - BankDrop;
                c.waterHalf = waterDepth <= 0 ? 0 : c.width * (1 - InverseSmooth(BankDrop / c.depth));
                c.drawHalf = Mathf.Min(c.width, c.waterHalf + .35f);
                c.swimHalf = c.depth - BankDrop > SwimDepth ? c.width * (1 - InverseSmooth((BankDrop + SwimDepth) / c.depth)) : 0;
                float xmin = float.MaxValue, xmax = float.MinValue, ymin = float.MaxValue, ymax = float.MinValue;
                foreach (var p in c.pts) { xmin = Mathf.Min(xmin, p.x); xmax = Mathf.Max(xmax, p.x); ymin = Mathf.Min(ymin, p.y); ymax = Mathf.Max(ymax, p.y); }
                float m = c.width * 1.3f + 16;   // valley blending can reach a little past the channel (and its widest rows)
                c.box = Rect.MinMaxRect(xmin - m, ymin - m, xmax + m, ymax + m);
                Creeks.Add(c);
            }
            if (zone.lakes != null)
                foreach (var l in zone.lakes)
                {
                    if (l == null || l.radius < 2) continue;
                    var k = new Lake { def = l, radius = l.radius };
                    var rnd = new System.Random(zone.seed * 31 + Mathf.RoundToInt(l.center.x * 7 + l.center.y * 13));   // this pond's own shape
                    k.phase = new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble()) * Mathf.PI * 2;
                    // Level: just under the lowest natural ground in a ring from the (irregular) waterline out 3 m, including the
                    // drawn edge at the angles it is checked at, so the shore always rises from the water and never lets it spill.
                    float low = float.MaxValue;
                    foreach (float ring in new[] { 0f, .3f, 1.5f, 3f })
                        for (int a = 0; a < 48; a++)
                        {
                            float ang = a * Mathf.PI / 24; var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)); var p = l.center + dir * (k.RadiusAt(dir) + ring);
                            low = Mathf.Min(low, uncarved(p.x, p.y));
                        }
                    k.level = low - .05f;
                    float d = Mathf.Max(.8f, l.depth);
                    k.bottom = k.level - d;
                    k.ramp = Mathf.Min(.8f * l.radius, Mathf.Max(.55f * l.radius, 1.9f * d));   // underwater bank width (slope <= ~38 deg), inside the deepest bay
                    k.swimInset = d > SwimDepth ? k.ramp * InverseSmooth(SwimDepth / d) : 0;       // swim-deep this far in from the waterline
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
                Project(c, p, out float d, out float level, out float wide);
                float bank = level + BankDrop, hh = h, width = c.width * wide;
                if (h > bank)
                {
                    // Valley: terrace down to the bank beside the water, blending back into the hills over a width that
                    // grows with the cut (about 22 degrees at most).
                    float blend = Mathf.Max(2, 2.5f * (h - bank));
                    float wgt = Mathf.SmoothStep(0, 1, Mathf.Clamp01(1 - (d - width) / blend));
                    hh = Mathf.Lerp(h, bank, wgt);
                }
                if (d < width) hh = Mathf.Min(hh, bank - c.depth * Mathf.SmoothStep(0, 1, 1 - d / width));
                h = Mathf.Min(h, hh);
            }
            foreach (var k in Lakes)
            {
                var off = p - k.def.center; float d = off.magnitude;
                if (d > k.radius * 1.2f + 20) continue;
                float r = k.RadiusAt(off), s = d - r, target;
                if (s <= 0) target = Mathf.Lerp(k.bottom, k.level, Mathf.SmoothStep(0, 1, Mathf.Clamp01((d - (r - k.ramp)) / k.ramp)));
                else
                {
                    // A gentle shore (~21 degrees) easing into the land over a soft lip: a smooth minimum that widens away from
                    // the water, so there is no crease for the 1.25 m ground grid to saw into light and dark triangles.
                    target = k.level + s * .38f;
                    float soft = Mathf.Min(1.2f, s * .35f), g = Mathf.Max(0, soft - Mathf.Abs(h - target)) / soft;
                    target = Mathf.Min(h, target) - g * g * soft * .25f;
                }
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
            {
                var off = p - k.def.center;
                if (off.magnitude < k.DrawRadiusAt(off)) { surface = k.level; depth = surface - ground; if (depth > 0) return true; }
            }
            foreach (var c in Creeks)
            {
                if (c.drawHalf <= 0 || !c.box.Contains(p)) continue;
                Project(c, p, out float d, out float level, out float wide);
                if (d > c.DrawHalf(wide)) continue;
                surface = level; depth = level - ground;
                if (depth > 0) return true;
            }
            return false;
        }
        /// <summary>
        /// Creek current at a point: the downstream direction (creek points run source first) times a strength of 1 in
        /// mid-channel, falling to 0 at the drawn edge. Lakes are still.
        /// </summary>
        public Vector2 FlowAt(Vector2 p)
        {
            foreach (var c in Creeks)
            {
                if (c.drawHalf <= 0 || !c.box.Contains(p)) continue;
                float best = float.MaxValue, half = 1; Vector2 dir = Vector2.zero;
                for (int i = 0; i + 1 < c.pts.Length; i++)
                {
                    Vector2 a = c.pts[i], ab = c.pts[i + 1] - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude)), d = Vector2.Distance(p, a + ab * t);
                    if (d < best) { best = d; dir = ab.normalized; half = c.DrawHalf(Mathf.Lerp(c.wide[i], c.wide[i + 1], t)); }
                }
                if (best < half) { float x = best / half; return dir * (1 - x * x); }
            }
            return Vector2.zero;
        }
        public bool NearWater(Vector2 p, float margin)
        {
            foreach (var k in Lakes) { var off = p - k.def.center; if (off.magnitude < k.RadiusAt(off) + margin) return true; }
            foreach (var c in Creeks) { if (!c.box.Contains(p)) continue; Project(c, p, out float d, out _, out float wide); if (d < c.width * wide + margin) return true; }
            return false;
        }
        /// <summary>
        /// Metres out from the nearest waterline (negative in the water), or <paramref name="range"/> when further. Ground
        /// paint and grass use it, so the bank follows the real, wandering edge.
        /// </summary>
        public float Shore(Vector2 p, float range)
        {
            float best = range;
            foreach (var k in Lakes) { var off = p - k.def.center; float m = off.magnitude; if (m < k.radius * 1.25f + range) best = Mathf.Min(best, m - k.RadiusAt(off)); }
            foreach (var c in Creeks)
            {
                // The drawn legs first (a few segments): the wandering line never strays far from them.
                if (c.waterHalf <= 0 || !c.box.Contains(p) || Nearest(p, c.def.points, out _) > c.width * 1.3f + 3 + range) continue;
                Project(c, p, out float d, out _, out float wide);
                best = Mathf.Min(best, d - c.waterHalf * wide);
            }
            return best;
        }

        /// <summary>Distance to the creek's centre line, and the water level and width factor interpolated there.</summary>
        public static void Project(Creek c, Vector2 p, out float distance, out float level, out float wide)
        {
            distance = float.MaxValue; level = c.level[0]; wide = c.wide[0];
            for (int i = 0; i + 1 < c.pts.Length; i++)
            {
                Vector2 a = c.pts[i], b = c.pts[i + 1], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
                float d = Vector2.Distance(p, a + ab * t);
                if (d < distance) { distance = d; level = Mathf.Lerp(c.level[i], c.level[i + 1], t); wide = Mathf.Lerp(c.wide[i], c.wide[i + 1], t); }
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
        /// <summary>
        /// Makes a creek authored as a few straight legs look natural: the legs' corners are rounded, the line swings gently
        /// from side to side (long and short swings), and the width breathes along the flow (wide[], about +-20%). Bridges
        /// and the mill pin the line they were placed on; roads and props beside it only stop it swinging (or widening)
        /// toward them.
        /// </summary>
        static void Meander(ZoneDefinition zone, Creek c, List<Vector2> pts, float half)
        {
            int n = pts.Count; c.wide = new float[n]; var shift = new Vector2[n];
            for (int pass = 0; pass < 3; pass++) { var was = pts.ToArray(); for (int i = 1; i < n - 1; i++) pts[i] = (was[i - 1] + 2 * was[i] + was[i + 1]) / 4; }
            float along = 0, sway = Mathf.Min(1.6f, c.width * .4f), seed = zone.seed % 1000 * .731f + c.def.points[0].x * .173f;
            for (int i = 0; i < n; i++)
            {
                if (i > 0) along += Vector2.Distance(pts[i], pts[i - 1]);
                var dir = i < n - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1]; var side = new Vector2(-dir.y, dir.x).normalized;
                Room(zone, pts[i], side, c.width, out float left, out float right);
                float swing = sway * (Mathf.Sin(along * .137f + seed) + .35f * Mathf.Sin(along * .331f + seed * 2.3f));   // ~46 m and ~19 m swings
                float breathe = (Mathf.PerlinNoise(along * .04f + seed, 7.3f) - .5f) * .6f;
                shift[i] = side * swing * (swing > 0 ? left : right);
                c.wide[i] = Mathf.Clamp(1 + (breathe > 0 ? breathe * Mathf.Min(left, right) : breathe), .75f, 1.25f);
            }
            for (int i = 0; i < n; i++) pts[i] = new Vector2(Mathf.Clamp(pts[i].x + shift[i].x, -half, half), Mathf.Clamp(pts[i].y + shift[i].y, -half, half));
        }
        /// <summary>
        /// How much of its full swing a creek may take at p toward each side (left = +side). Bridges and the mill pin it; a
        /// road or prop beside it only stops it swinging toward that thing; a road crossing it stops both ways. The limits
        /// ease in and out (SmoothStep), so the line never kinks where it is let go.
        /// </summary>
        static void Room(ZoneDefinition zone, Vector2 p, Vector2 side, float width, out float left, out float right)
        {
            float l = 1, r = 1;
            void Beside(float d, Vector2 q)   // d: clearance from the creek's line to the thing's edge; q: where it is
            {
                float toward = Mathf.SmoothStep(0, 1, (d - width - 2) / 8), away = Mathf.SmoothStep(0, 1, (d - 1) / 4);
                if (Vector2.Dot(q - p, side) > 0) { l = Mathf.Min(l, toward); r = Mathf.Min(r, away); } else { r = Mathf.Min(r, toward); l = Mathf.Min(l, away); }
            }
            foreach (var road in zone.roads) if (road != null && road.points != null && road.points.Length > 1) Beside(Nearest(p, road.points, out var q) - road.width / 2, q);
            foreach (var pr in zone.props)
            {
                if (pr == null || pr.kind == "wall") continue;
                float d = Vector2.Distance(p, pr.at) - Mathf.Max(pr.size.x, pr.size.y, pr.kind == "bridge" ? 12 : 0) / 2;
                if (pr.kind == "bridge" || pr.kind == "mill") { float pin = Mathf.SmoothStep(0, 1, (d - width - 2) / 8); l = Mathf.Min(l, pin); r = Mathf.Min(r, pin); }
                else Beside(d, pr.at);
            }
            left = l; right = r;
        }
        /// <summary>Distance from p to a polyline, and the nearest point on it.</summary>
        static float Nearest(Vector2 p, Vector2[] pts, out Vector2 q)
        {
            float best = float.MaxValue; q = p;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector2 a = pts[i], ab = pts[i + 1] - a, at = a + ab * Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
                float d = Vector2.Distance(p, at); if (d < best) { best = d; q = at; }
            }
            return best;
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
                        float off = across[k] * c.DrawHalf(c.wide[i]); var p = c.pts[i] + side * off;
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
        /// <summary>A lake's level disc (rings out to just past its irregular waterline), with alpha from the real depth.</summary>
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
                for (int s = 0; s < Seg; s++) { float a = s * Mathf.PI * 2 / Seg; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); Add(dir * k.DrawRadiusAt(dir) * rings[r]); }
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
