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
            Quad(v, t, aR + d, aR, bR, bR + d); Quad(v, t, bL + d, bL, aL, aL + d); // slab ends under the gables (left open, the eave slab read as a loose board)
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
        /// <summary>
        /// A ring segment: a block on a circle round the origin with radial (mitred) ends, from angle a0 to a1 (degrees,
        /// anticlockwise from +x seen from above) and radius r0 to r1, height h up from y = 0. Flat-shaded, with metre UVs so
        /// stone tiles across it (kerbs, bench rings).
        /// </summary>
        public static Mesh Arc(float r0, float r1, float a0, float a1, float h)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            Vector3 P(float r, float a, float y) { return new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * r, y, Mathf.Sin(a * Mathf.Deg2Rad) * r); }
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud) { Quad(v, t, a, b, c, d); uv.Add(ua / 1.2f); uv.Add(ub / 1.2f); uv.Add(uc / 1.2f); uv.Add(ud / 1.2f); }
            Vector2 Flat(Vector3 p) { return new Vector2(p.x, p.z); }
            float l0 = a0 * Mathf.Deg2Rad, l1 = a1 * Mathf.Deg2Rad;   // arc lengths per unit radius, for the curved faces
            Vector3 i00 = P(r0, a0, 0), i01 = P(r0, a1, 0), o00 = P(r1, a0, 0), o01 = P(r1, a1, 0), i10 = P(r0, a0, h), i11 = P(r0, a1, h), o10 = P(r1, a0, h), o11 = P(r1, a1, h);
            Face(i10, i11, o11, o10, Flat(i10), Flat(i11), Flat(o11), Flat(o10));                                   // top
            Face(o00, o01, i01, i00, Flat(o00), Flat(o01), Flat(i01), Flat(i00));                                   // underside
            Face(o00, o10, o11, o01, new Vector2(l0 * r1, 0), new Vector2(l0 * r1, h), new Vector2(l1 * r1, h), new Vector2(l1 * r1, 0));   // outer face
            Face(i01, i11, i10, i00, new Vector2(l1 * r0, 0), new Vector2(l1 * r0, h), new Vector2(l0 * r0, h), new Vector2(l0 * r0, 0));   // inner face
            Face(i00, i10, o10, o00, new Vector2(r0, 0), new Vector2(r0, h), new Vector2(r1, h), new Vector2(r1, 0));   // the end at a0
            Face(o01, o11, i11, i01, new Vector2(r1, 0), new Vector2(r1, h), new Vector2(r0, h), new Vector2(r0, 0));   // the end at a1
            var m = Build("Arc", v, t); m.SetUVs(0, uv); return m;
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
        /// rocks and boulders (high roughness, flattened underside). The seed picks the shape. UVs wrap once round and once up
        /// unless <paramref name="uvAround"/> and <paramref name="uvUp"/> repeat them (a big rock lump keeps its stone texture at
        /// a steady size that way). <paramref name="faceted"/> gives every triangle its own vertices, so it shades flat: broken
        /// rock rather than clay.
        /// </summary>
        public static Mesh Blob(int seed, float roughness, bool flatBottom, int rings = 9, int segments = 14, float uvAround = 1, float uvUp = 1, bool faceted = false)
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
                    v.Add(p); uv.Add(new Vector2((float)s / segments * uvAround, (float)r / rings * uvUp));
                }
            }
            // The bottom and top rings are each one point, so the triangles that have two corners on them are dropped.
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * (segments + 1) + s, b = a + segments + 1;
                    if (r > 0) t.AddRange(new[] { a, b, a + 1 });
                    if (r < rings - 1) t.AddRange(new[] { a + 1, b, b + 1 });
                }
            if (faceted)
            {
                var fv = new List<Vector3>(t.Count); var fuv = new List<Vector2>(t.Count); var ft = new List<int>(t.Count);
                for (int i = 0; i < t.Count; i++) { fv.Add(v[t[i]]); fuv.Add(uv[t[i]]); ft.Add(i); }
                v = fv; uv = fuv; t = ft;
            }
            var m = Build("Blob", v, t); m.SetUVs(0, uv); return m;
        }
        /// <summary>
        /// A tube lofted along a curve (trunks, limbs, roots): ring k sits at centre(rings[k]), square to the curve, with its
        /// radius at each angle from radius(t, angle) (taper, gnarl, buttress ridges). Angle 0 points along <paramref name="reference"/>
        /// (a direction the curve never runs along) squared to the curve, so the rings never twist. Normals come from the surface
        /// function itself, so two tubes cut from one function at the same t meet without a shading seam. u wraps round
        /// <paramref name="wraps"/> times; v is t * <paramref name="vPerT"/>, or half the length along when that is 0. No end caps:
        /// an end sits inside another part or underground, or the radius closes it to a point.
        /// </summary>
        public static Mesh Tube(Func<float, Vector3> centre, Func<float, float, float> radius, IList<float> rings, int sides, Vector3 reference, int wraps = 1, float vPerT = 0)
        {
            Vector3 At(float t, float a)
            {
                var tan = (centre(t + .002f) - centre(t - .002f)).normalized;
                var n = (reference - tan * Vector3.Dot(reference, tan)).normalized;
                return centre(t) + (n * Mathf.Cos(a) + Vector3.Cross(tan, n) * Mathf.Sin(a)) * radius(t, a);
            }
            int cols = sides + 1; var v = new Vector3[rings.Count * cols]; var nrm = new Vector3[v.Length]; var uv = new Vector2[v.Length];
            float along = 0; var last = centre(rings[0]);
            for (int k = 0; k < rings.Count; k++)
            {
                float t = rings[k]; var c = centre(t); along += Vector3.Distance(c, last); last = c;
                for (int s = 0; s < cols; s++)
                {
                    float a = 2 * Mathf.PI * (s % sides) / sides; int i = k * cols + s;   // the seam column repeats column 0 exactly
                    v[i] = At(t, a);
                    var n = Vector3.Cross((At(t + .01f, a) - At(t - .01f, a)).normalized, (At(t, a + .02f) - At(t, a - .02f)).normalized);
                    if (n.sqrMagnitude < 1e-6f) n = (centre(t + .01f) - centre(t - .01f)) * (k == 0 ? -1 : 1);   // a closed tip: along the curve, outward
                    n.Normalize(); nrm[i] = Vector3.Dot(n, v[i] - c) < 0 ? -n : n;   // away from the axis
                    uv[i] = new Vector2((float)s / sides * wraps, vPerT > 0 ? t * vPerT : along / 2);
                }
            }
            // Wound to face outward (a front face's Cross(p1 - p0, p2 - p0) points at the viewer): test one quad, wind them all alike.
            int q = (rings.Count - 2) / 2 * cols; bool fwd = Vector3.Dot(Vector3.Cross(v[q + cols] - v[q], v[q + 1] - v[q]), nrm[q]) > 0;
            var tri = new int[(rings.Count - 1) * sides * 6];
            for (int k = 0, j = 0; k + 1 < rings.Count; k++)
                for (int s = 0; s < sides; s++)
                {
                    int a = k * cols + s, b = a + 1, c = a + cols, d = c + 1;
                    if (fwd) { tri[j++] = a; tri[j++] = c; tri[j++] = b; tri[j++] = b; tri[j++] = c; tri[j++] = d; }
                    else { tri[j++] = a; tri[j++] = b; tri[j++] = c; tri[j++] = b; tri[j++] = d; tri[j++] = c; }
                }
            var m = new Mesh { name = "Tube" }; m.vertices = v; m.normals = nrm; m.uv = uv; m.triangles = tri; m.RecalculateBounds();
            return m;
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
        /// <summary>
        /// Painted leaf cards collected into one mesh (a crown's clusters, a pine's boughs; Crulanda/Leaf). Each card is a quad
        /// standing on its near edge at <c>at</c>, reaching <c>length</c> along <c>along</c> and <c>width</c> across <c>across</c>;
        /// its UV v runs from 0 at that edge (where the leaf texture's twig sits, on the bough) to 1 at the far edge. Its normals
        /// are not the card's face but the crown's: away from <c>heart</c> (a point inside the crown), turned up by <c>lift</c>
        /// (0..1), so every card shades like the outside of one round crown whichever way it faces, and its back face shades like
        /// its front (the leaf shader draws both faces and culls nothing, so the winding doesn't matter). The vertex colour tints
        /// the card (rgb: a lighter or darker cluster; <c>footTint</c>, when given, is the near edge's, so a bough can darken in at
        /// the trunk) and carries how much the wind moves it (alpha: 0 at the near edge, 1 at the far).
        /// </summary>
        public sealed class Cards
        {
            readonly List<Vector3> v = new List<Vector3>(); readonly List<Vector3> n = new List<Vector3>(); readonly List<Vector2> uv = new List<Vector2>();
            readonly List<Color> c = new List<Color>(); readonly List<int> t = new List<int>();
            public int Count { get { return v.Count / 4; } }
            public void Add(Vector3 at, Vector3 along, Vector3 across, float length, float width, Vector3 heart, float lift, Color tint, float swayNear = 0, float swayFar = 1, Color? footTint = null)
            {
                var w = across.normalized * (width / 2); var l = along.normalized * length; int s = v.Count;
                foreach (var p in new[] { at - w, at + w, at + w + l, at - w + l })
                {
                    var away = p - heart; var normal = away.sqrMagnitude < 1e-4f ? Vector3.up : Vector3.Slerp(away.normalized, Vector3.up, lift);
                    v.Add(p); n.Add(normal.normalized);
                }
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                var near = footTint ?? tint; near.a = swayNear; var far = tint; far.a = swayFar;
                c.Add(near); c.Add(near); c.Add(far); c.Add(far);
                t.AddRange(new[] { s, s + 2, s + 1, s, s + 3, s + 2 });
            }
            /// <summary>
            /// A leaf cluster: two cards crossed on one axis, the second turned a right angle about <c>along</c>, a little smaller and a
            /// shade deeper; with <paramref name="cards"/> 3, a third between them in roll and tilted 24 degrees off the shared axis, so
            /// no view (down the axis, or edge-on to the pair) meets only lines, and the cluster has a body from below as well.
            /// </summary>
            public void AddCross(Vector3 at, Vector3 along, Vector3 across, float length, float width, Vector3 heart, float lift, Color tint, int cards = 2)
            {
                Add(at, along, across, length, width, heart, lift, tint);
                Add(at, along, Vector3.Cross(along, across), length * .92f, width * .85f, heart, lift, tint * new Color(.93f, .93f, .93f, 1));
                if (cards < 3) return;
                var tilted = Quaternion.AngleAxis(24, across.normalized) * along.normalized;
                Add(at, tilted, Quaternion.AngleAxis(45, tilted) * across, length * .88f, width * .9f, heart, lift, tint * new Color(.9f, .92f, .88f, 1));
            }
            public Mesh Build(string name)
            {
                var m = new Mesh { name = name };
                if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;   // a backdrop side's every card in one mesh
                m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateBounds();
                return m;
            }
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
