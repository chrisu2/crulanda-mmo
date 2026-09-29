using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>Small procedural meshes the zone builder needs beyond Unity's primitives.</summary>
    public static class ZoneMeshes
    {
        /// <summary>Gable roof: ridge runs along X; width (X) x depth (Z) footprint, ridge at +height. Pivot at eave centre.</summary>
        public static Mesh GableRoof(float width, float depth, float height, float thickness = .25f)
        {
            float hw = width / 2, hd = depth / 2;
            var v = new List<Vector3>(); var t = new List<int>();
            // Two sloped planes (top faces) plus the slab underside and gable ends, kept simple and closed.
            Vector3 aL = new Vector3(-hw, 0, -hd), aR = new Vector3(hw, 0, -hd), rL = new Vector3(-hw, height, 0), rR = new Vector3(hw, height, 0);
            Vector3 bL = new Vector3(-hw, 0, hd), bR = new Vector3(hw, 0, hd);
            Quad(v, t, aL, rL, rR, aR);          // south slope
            Quad(v, t, bR, rR, rL, bL);          // north slope
            Vector3 d = Vector3.down * thickness;
            Quad(v, t, aR + d, bR + d, bL + d, aL + d); // underside
            Tri(v, t, aL, bL, rL); Tri(v, t, bR, aR, rR); // gable ends (closed triangles)
            Quad(v, t, aL + d, aL, aR, aR + d); Quad(v, t, bR + d, bR, bL, bL + d); // eave edges
            var m = Build("Gable roof", v, t);
            // Planar UVs in metres (x along the ridge, distance up the slope) so thatch/slate textures tile at a steady scale.
            var uv = new List<Vector2>();
            foreach (var p in v) uv.Add(new Vector2(p.x / 2.5f, (Mathf.Abs(p.z) + p.y) / 2.5f));
            m.SetUVs(0, uv); return m;
        }
        /// <summary>Upright cone with its base centred at the origin.</summary>
        public static Mesh Cone(float radius, float height, int sides = 10)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            var tip = Vector3.up * height;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                var p0 = new Vector3(Mathf.Cos(a0) * radius, 0, Mathf.Sin(a0) * radius);
                var p1 = new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);
                Tri(v, t, p1, p0, tip); Tri(v, t, p0, p1, Vector3.zero);
            }
            return Build("Cone", v, t);
        }
        /// <summary>Flat ribbon following a polyline at a fixed height, e.g. water surface.</summary>
        public static Mesh Ribbon(IList<Vector2> points, float width, Func<Vector2, float> height)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            float along = 0;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 dir = i < points.Count - 1 ? points[i + 1] - points[i] : points[i] - points[i - 1];
                if (i > 0) along += Vector2.Distance(points[i], points[i - 1]);
                var side = new Vector2(-dir.y, dir.x).normalized * width / 2;
                var l = points[i] + side; var r = points[i] - side;
                v.Add(new Vector3(l.x, height(l), l.y)); v.Add(new Vector3(r.x, height(r), r.y));
                uv.Add(new Vector2(0, along / width)); uv.Add(new Vector2(1, along / width));
                if (i > 0) { int b = v.Count - 4; t.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); }
            }
            var m = Build("Ribbon", v, t); m.SetUVs(0, uv); return m;
        }
        /// <summary>
        /// A lumpy unit sphere (radius about 0.5) displaced by layered noise: tree canopies and bushes (low roughness),
        /// rocks and boulders (high roughness, flattened underside). The seed picks the shape.
        /// </summary>
        public static Mesh Blob(int seed, float roughness, bool flatBottom, int rings = 9, int segments = 14)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            float ox = seed * 3.17f, oy = seed * 7.41f;
            for (int r = 0; r <= rings; r++)
            {
                float lat = Mathf.PI * r / rings - Mathf.PI / 2;
                for (int s = 0; s <= segments; s++)
                {
                    float lon = 2 * Mathf.PI * s / segments;
                    var dir = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
                    int ss = s % segments;   // wrap the seam so noise matches on both sides
                    var wrapped = new Vector3(Mathf.Cos(lat) * Mathf.Cos(2 * Mathf.PI * ss / segments), dir.y, Mathf.Cos(lat) * Mathf.Sin(2 * Mathf.PI * ss / segments));
                    float n = Mathf.PerlinNoise(wrapped.x * 1.7f + ox + wrapped.y, wrapped.z * 1.7f + oy - wrapped.y) * .7f + Mathf.PerlinNoise(wrapped.x * 4.1f + oy, wrapped.z * 4.1f + ox + wrapped.y * 2) * .3f;
                    float radius = .5f * (1 + (n - .5f) * roughness);
                    var p = dir * radius;
                    if (flatBottom && p.y < -.18f) p.y = -.18f + (p.y + .18f) * .15f;
                    v.Add(p); uv.Add(new Vector2((float)s / segments, (float)r / rings));
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * (segments + 1) + s, b = a + segments + 1;
                    t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            var m = Build("Blob", v, t); m.SetUVs(0, uv); return m;
        }
        /// <summary>Square ground grid of <paramref name="size"/> metres; UV (0..1) spans the whole zone for the painted texture.</summary>
        public static Mesh Ground(float size, int segments, Func<float, float, float> height)
        {
            var v = new Vector3[(segments + 1) * (segments + 1)]; var uv = new Vector2[v.Length]; var t = new int[segments * segments * 6];
            for (int z = 0, i = 0; z <= segments; z++)
                for (int x = 0; x <= segments; x++, i++)
                {
                    float px = -size / 2 + size * x / segments, pz = -size / 2 + size * z / segments;
                    v[i] = new Vector3(px, height(px, pz), pz); uv[i] = new Vector2((float)x / segments, (float)z / segments);
                }
            for (int z = 0, k = 0; z < segments; z++)
                for (int x = 0; x < segments; x++)
                {
                    int i = z * (segments + 1) + x;
                    t[k++] = i; t[k++] = i + segments + 1; t[k++] = i + 1;
                    t[k++] = i + 1; t[k++] = i + segments + 1; t[k++] = i + segments + 2;
                }
            // Normals from central differences on the height grid rather than from the triangles: symmetric in x and z, so a
            // curved slope or shore crossing the grid's fixed diagonals shades smoothly, not in light and dark triangles.
            var n = new Vector3[v.Length]; float step = size / segments; int row = segments + 1;
            for (int z = 0, i = 0; z <= segments; z++)
                for (int x = 0; x <= segments; x++, i++)
                {
                    int x0 = Mathf.Max(0, x - 1), x1 = Mathf.Min(segments, x + 1), z0 = Mathf.Max(0, z - 1), z1 = Mathf.Min(segments, z + 1);
                    float dx = (v[z * row + x1].y - v[z * row + x0].y) / ((x1 - x0) * step), dz = (v[z1 * row + x].y - v[z0 * row + x].y) / ((z1 - z0) * step);
                    n[i] = new Vector3(-dx, 1, -dz).normalized;
                }
            var m = new Mesh { name = "Zone ground", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.vertices = v; m.uv = uv; m.triangles = t; m.normals = n; m.RecalculateBounds();
            return m;
        }
        /// <summary>
        /// One side (0 south, 1 east, 2 north, 3 west) of the scenery skirt round the square ground: the ground's edge row
        /// scaled out from the centre to each offset past it. Row 0 is the ground's own edge (same vertices, so no crack).
        /// UVs mirror back across the edge into the zone's painted texture, so the paint (roads too) runs on over the seam.
        /// Normals come from the height function, so the four sides and the ground shade as one surface.
        /// </summary>
        public static Mesh Backdrop(float size, int segments, int side, float[] offsets, Func<float, float, float> height)
        {
            float half = size / 2; int cols = segments + 1;
            float C(int k) { return -size / 2 + size * k / segments; }   // exactly the ground grid's coordinates
            float Mirror(float a) { return a > half ? size - a : a < -half ? -size - a : a; }
            var v = new Vector3[cols * offsets.Length]; var n = new Vector3[v.Length]; var uv = new Vector2[v.Length]; var t = new int[(offsets.Length - 1) * segments * 6];
            for (int r = 0, i = 0; r < offsets.Length; r++)
                for (int k = 0; k < cols; k++, i++)
                {
                    // Anticlockwise seen from above: the south edge runs west to east, the east edge south to north, and so on.
                    var e = side == 0 ? new Vector2(C(k), C(0)) : side == 1 ? new Vector2(C(segments), C(k)) : side == 2 ? new Vector2(C(segments - k), C(segments)) : new Vector2(C(0), C(segments - k));
                    var p = e * ((half + offsets[r]) / half);
                    v[i] = new Vector3(p.x, height(p.x, p.y), p.y);
                    n[i] = new Vector3(height(p.x - .75f, p.y) - height(p.x + .75f, p.y), 1.5f, height(p.x, p.y - .75f) - height(p.x, p.y + .75f)).normalized;
                    uv[i] = new Vector2((Mirror(p.x) + half) / size, (Mirror(p.y) + half) / size);
                }
            for (int r = 0, j = 0; r + 1 < offsets.Length; r++)
                for (int k = 0; k < segments; k++)
                {
                    int a = r * cols + k, d = a + cols;   // clockwise from above (upward-facing)
                    t[j++] = a; t[j++] = a + 1; t[j++] = d + 1; t[j++] = a; t[j++] = d + 1; t[j++] = d;
                }
            var m = new Mesh { name = "Backdrop skirt" }; m.vertices = v; m.normals = n; m.uv = uv; m.triangles = t; m.RecalculateBounds();
            return m;
        }
        static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        { int s = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 }); }
        static void Tri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
        { int s = v.Count; v.Add(a); v.Add(b); v.Add(c); t.AddRange(new[] { s, s + 1, s + 2 }); }
        static Mesh Build(string name, List<Vector3> v, List<int> t)
        {
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
    }
}
