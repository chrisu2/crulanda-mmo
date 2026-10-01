using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Meshes for worn gear, each made once and kept by name (a family's shapes do not depend on palette or quality, so swapping
    /// gear makes no new meshes). Every face is wound to face out of the shape, whatever order the caller gives its points in.
    /// Unity's built-in primitives are fetched once, so gear parts carry no colliders and build the same in edit mode tests.
    /// </summary>
    public static class GearMeshes
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>(StringComparer.Ordinal);
        static readonly Mesh[] prims = new Mesh[6];
        /// <summary>How many generated meshes are kept (the cache test watches this stay flat over many swaps).</summary>
        public static int Count { get { return cache.Count; } }

        /// <summary>The mesh of a built-in primitive (cube 1 m, sphere 1 m across, cylinder and capsule 2 m tall and 1 m across).</summary>
        public static Mesh Prim(PrimitiveType type)
        {
            int i = (int)type; if (prims[i] != null) return prims[i];
            var o = GameObject.CreatePrimitive(type); prims[i] = o.GetComponent<MeshFilter>().sharedMesh; UnityEngine.Object.DestroyImmediate(o);
            return prims[i];
        }
        /// <summary>The mesh kept under this key, made by <paramref name="make"/> the first time (and again if Unity has unloaded it).</summary>
        public static Mesh Cached(string key, Func<Mesh> make)
        {
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = make(); m.name = "Gear " + key; m.hideFlags = HideFlags.DontUnloadUnusedAsset; cache[key] = m; return m;
        }

        /// <summary>Collects triangles, turning each one to face along a hint (the outward direction at that face).</summary>
        sealed class Builder
        {
            public readonly List<Vector3> v = new List<Vector3>(); readonly List<int> t = new List<int>();
            public int Add(Vector3 p) { v.Add(p); return v.Count - 1; }
            public void Tri(int a, int b, int c, Vector3 outward)
            {
                var n = Vector3.Cross(v[b] - v[a], v[c] - v[a]); if (n.sqrMagnitude < 1e-14f) return;
                if (Vector3.Dot(n, outward) < 0) { t.Add(a); t.Add(c); t.Add(b); } else { t.Add(a); t.Add(b); t.Add(c); }
            }
            public void Quad(int a, int b, int c, int d, Vector3 outward) { Tri(a, b, c, outward); Tri(a, c, d, outward); }
            public Mesh Build(Quaternion? turn = null)
            {
                var m = new Mesh();
                if (turn.HasValue) for (int i = 0; i < v.Count; i++) v[i] = turn.Value * v[i];
                m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
            }
        }

        /// <summary>
        /// A solid of revolution about Y. The profile is (radius, height) points, bottom to top, going out from the axis, up
        /// the outside and back in (a closed ring traces round its section the same way). Smooth round the axis; with
        /// <paramref name="smooth"/> also smooth down the profile, otherwise each profile segment is its own band (crisp rims).
        /// <paramref name="arc"/> under 360 makes an open slice starting at +X.
        /// </summary>
        public static Mesh Lathe(string key, Vector2[] profile, int sides = 14, bool smooth = false, float arc = 360)
        {
            return Cached(key, () =>
            {
                var b = new Builder(); bool full = arc >= 360; int cols = full ? sides : sides + 1;
                float A(int j) { return arc * Mathf.Deg2Rad * j / sides; }
                int[] Ring(Vector2 p) { var r = new int[cols]; for (int j = 0; j < cols; j++) r[j] = b.Add(new Vector3(Mathf.Cos(A(j)) * p.x, p.y, Mathf.Sin(A(j)) * p.x)); return r; }
                int[] lower = Ring(profile[0]);
                for (int i = 0; i + 1 < profile.Length; i++)
                {
                    var p0 = profile[i]; var p1 = profile[i + 1];
                    if (!smooth && i > 0) lower = Ring(p0);
                    var upper = Ring(p1);
                    float dr = p1.x - p0.x, dy = p1.y - p0.y;
                    for (int j = 0; j < sides; j++)
                    {
                        int k = full ? (j + 1) % sides : j + 1; float a = (A(j) + A(j + 1)) / 2;
                        var outward = new Vector3(dy * Mathf.Cos(a), -dr, dy * Mathf.Sin(a));
                        b.Tri(lower[j], upper[j], lower[k], outward); b.Tri(upper[j], upper[k], lower[k], outward);
                    }
                    lower = upper;
                }
                return b.Build();
            });
        }

        /// <summary>
        /// A blade along +Y from the grip end (y 0) to <paramref name="length"/>, flat faces to the sides (X), the edge toward +Z.
        /// At each fraction t of its length, <paramref name="edge"/> is how far the edge stands out toward +Z and
        /// <paramref name="back"/> how far the other edge (or the spine) stands out toward -Z; <paramref name="bend"/> moves the
        /// whole section along Z (a curved sabre, a hooked knife). A double-edged blade has a diamond section with a ridge down the
        /// middle; a single-edged one a flat spine that bevels to the edge. Thickness thins a little toward the tip. Faces are flat
        /// across and smooth along, so the ridge and bevels catch the light.
        /// </summary>
        public static Mesh Blade(string key, float length, Func<float, float> edge, Func<float, float> back, float thick, Func<float, float> bend = null, bool single = false, int stations = 12)
        {
            return Cached(key, () =>
            {
                var b = new Builder(); int n = single ? 5 : 4;
                var sec = new Vector3[stations + 1][]; var mid = new Vector3[stations + 1];
                for (int k = 0; k <= stations; k++)
                {
                    float t = (float)k / stations, y = t * length, z0 = bend != null ? bend(t) : 0, e = edge(t), s = back(t), th = thick / 2 * (1 - .45f * t * t);
                    float zc = z0 + (e - s) / 2; mid[k] = new Vector3(0, y, zc);
                    sec[k] = single
                        ? new[] { new Vector3(0, y, z0 + e), new Vector3(th * .85f, y, z0 + Mathf.Lerp(-s, e, .4f)), new Vector3(th, y, z0 - s), new Vector3(-th, y, z0 - s), new Vector3(-th * .85f, y, z0 + Mathf.Lerp(-s, e, .4f)) }
                        : new[] { new Vector3(0, y, z0 + e), new Vector3(th, y, zc), new Vector3(0, y, z0 - s), new Vector3(-th, y, zc) };
                }
                for (int f = 0; f < n; f++)
                {
                    int g = (f + 1) % n; var colA = new int[stations + 1]; var colB = new int[stations + 1];
                    for (int k = 0; k <= stations; k++) { colA[k] = b.Add(sec[k][f]); colB[k] = b.Add(sec[k][g]); }
                    for (int k = 0; k < stations; k++)
                    {
                        var outward = (sec[k][f] + sec[k][g] + sec[k + 1][f] + sec[k + 1][g]) / 4 - (mid[k] + mid[k + 1]) / 2;
                        b.Quad(colA[k], colA[k + 1], colB[k + 1], colB[k], outward);
                    }
                }
                foreach (int k in new[] { 0, stations })
                {
                    var outward = k == 0 ? Vector3.down : Vector3.up; int c = b.Add(mid[k]); var ring = new int[n];
                    for (int f = 0; f < n; f++) ring[f] = b.Add(sec[k][f]);
                    for (int f = 0; f < n; f++) b.Tri(c, ring[f], ring[(f + 1) % n], outward);
                }
                return b.Build();
            });
        }
        /// <summary>
        /// A straight blade narrowing a little along its length and coming to a point over its last <paramref name="tip"/> fraction:
        /// centred when double-edged, on the spine line when single-edged (the edge sweeps up to meet it).
        /// </summary>
        public static Mesh Straight(string key, float length, float width, float thick, float tip = .2f, bool single = false)
        {
            float h = width / 2;
            float Taper(float t) { return 1 - .12f * t; }
            float U(float t) { return Mathf.Clamp01((t - (1 - tip)) / tip); }
            if (single) return Blade(key, length, t => h * Taper(t) * (1 - 2 * U(t) * U(t)), t => h * Taper(t), thick, null, true);
            return Blade(key, length, t => h * Taper(t) * (1 - U(t)), t => h * Taper(t) * (1 - U(t)), thick);
        }

        /// <summary>
        /// A flat plate cut to an outline of (x, z) points, <paramref name="thick"/> through along Y, its +Y face domed up by
        /// <paramref name="bulge"/> at <paramref name="centre"/> and falling to nothing at <paramref name="radius"/> (x, z) from it,
        /// so plates cut from one dome (a shield's field and its painted chief) meet seamlessly. <paramref name="lift"/> raises the
        /// whole plate (a painted field just proud of the face). <paramref name="thinTo"/> under 1 thins it toward +Z (a wedge for
        /// an axe head). The outline must be star-shaped round <paramref name="fan"/> (by default its centroid; the dome centre
        /// defaults to that too). <paramref name="turn"/> turns the result.
        /// </summary>
        public static Mesh Plate(string key, Vector2[] outline, float thick, float bulge = 0, Vector2? centre = null, Vector2? radius = null, float lift = 0, float thinTo = 1, Quaternion? turn = null, Vector2? fan = null)
        {
            return Cached(key, () =>
            {
                var b = new Builder(); int n = outline.Length;
                var f = fan ?? Centroid(outline); var c = centre ?? f;
                float zMin = float.MaxValue, zMax = float.MinValue; foreach (var p in outline) { zMin = Mathf.Min(zMin, p.y); zMax = Mathf.Max(zMax, p.y); }
                var rad = radius ?? Reach(outline, c);
                float Half(Vector2 p) { return thick / 2 * Mathf.Lerp(1, thinTo, zMax > zMin ? (p.y - zMin) / (zMax - zMin) : 0); }
                float Dome(Vector2 p) { var d = new Vector2((p.x - c.x) / Mathf.Max(.001f, rad.x), (p.y - c.y) / Mathf.Max(.001f, rad.y)); return bulge * Mathf.Max(0, 1 - d.sqrMagnitude); }
                Vector3 Top(Vector2 p) { return new Vector3(p.x, lift + Half(p) + Dome(p), p.y); }
                Vector3 Bottom(Vector2 p) { return new Vector3(p.x, lift - Half(p), p.y); }
                // The domed face: centre, two inner rings and the rim, shared so it shades smooth.
                var rings = new[] { .34f, .68f, 1f }; var idx = new int[rings.Length][]; int ci = b.Add(Top(f));
                for (int r = 0; r < rings.Length; r++) { idx[r] = new int[n]; for (int i = 0; i < n; i++) idx[r][i] = b.Add(Top(Vector2.Lerp(f, outline[i], rings[r]))); }
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n; b.Tri(ci, idx[0][i], idx[0][j], Vector3.up);
                    for (int r = 0; r + 1 < rings.Length; r++) b.Quad(idx[r][i], idx[r + 1][i], idx[r + 1][j], idx[r][j], Vector3.up);
                }
                int cb = b.Add(Bottom(f)); var under = new int[n]; for (int i = 0; i < n; i++) under[i] = b.Add(Bottom(outline[i]));
                for (int i = 0; i < n; i++) b.Tri(cb, under[i], under[(i + 1) % n], Vector3.down);
                // The edge all round, one flat band per side.
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n; var p = outline[i]; var q = outline[j]; var m = (p + q) / 2 - f; var e = q - p;
                    var normal = new Vector3(e.y, 0, -e.x); if (normal.x * m.x + normal.z * m.y < 0) normal = -normal;
                    b.Quad(b.Add(Bottom(p)), b.Add(Top(p)), b.Add(Top(q)), b.Add(Bottom(q)), normal);
                }
                return b.Build(turn);
            });
        }
        /// <summary>
        /// A raised band round an outline (a shield's rim): <paramref name="width"/> in from the edge, standing <paramref name="height"/>
        /// above <paramref name="baseY"/>, with the plate's dome under it, so it sits on the face.
        /// </summary>
        public static Mesh Rim(string key, Vector2[] outline, float width, float height, float baseY, float bulge = 0, Vector2? centre = null, Vector2? radius = null, float overhang = .006f)
        {
            return Cached(key, () =>
            {
                var b = new Builder(); int n = outline.Length; var c = centre ?? Centroid(outline); var rad = radius ?? Reach(outline, c);
                float Dome(Vector2 p) { var d = new Vector2((p.x - c.x) / Mathf.Max(.001f, rad.x), (p.y - c.y) / Mathf.Max(.001f, rad.y)); return bulge * Mathf.Max(0, 1 - d.sqrMagnitude); }
                var outer = new Vector2[n]; var inner = new Vector2[n];
                for (int i = 0; i < n; i++) { var d = outline[i] - c; float l = d.magnitude; outer[i] = c + d * ((l + overhang) / Mathf.Max(l, .001f)); inner[i] = c + d * (Mathf.Max(0, l - width) / Mathf.Max(l, .001f)); }
                Vector3 P(Vector2 p, float up) { return new Vector3(p.x, baseY + Dome(p) + up, p.y); }
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n; var mid = (outline[i] + outline[j]) / 2 - c; var outward = new Vector3(mid.x, 0, mid.y);
                    b.Quad(b.Add(P(outer[i], height)), b.Add(P(outer[j], height)), b.Add(P(inner[j], height)), b.Add(P(inner[i], height)), Vector3.up);
                    b.Quad(b.Add(P(outer[i], -height * .6f)), b.Add(P(outer[j], -height * .6f)), b.Add(P(outer[j], height)), b.Add(P(outer[i], height)), outward);
                    b.Quad(b.Add(P(inner[i], 0)), b.Add(P(inner[i], height)), b.Add(P(inner[j], height)), b.Add(P(inner[j], 0)), -outward);
                    b.Quad(b.Add(P(outer[i], -height * .6f)), b.Add(P(inner[i], -height * .6f)), b.Add(P(inner[j], -height * .6f)), b.Add(P(outer[j], -height * .6f)), Vector3.down);
                }
                return b.Build();
            });
        }

        /// <summary>
        /// A bent rod through the points of a Bezier curve (a knuckle bow, a root curl, a tine), tapering from r0 to r1, both ends
        /// capped flat.
        /// </summary>
        public static Mesh Rod(string key, Vector3[] control, float r0, float r1, int sides = 6, int rings = 9)
        {
            return Cached(key, () =>
            {
                var rs = new float[rings]; for (int i = 0; i < rings; i++) rs[i] = (float)i / (rings - 1);
                // Square to the rod's general run: the axis it runs along least, so the rings never twist.
                var dir = (control[control.Length - 1] - control[0]).normalized; var reference = Vector3.right;
                foreach (var axis in new[] { Vector3.up, Vector3.forward }) if (Mathf.Abs(Vector3.Dot(dir, axis)) < Mathf.Abs(Vector3.Dot(dir, reference))) reference = axis;
                var m = Crulanda.World.ZoneMeshes.Tube(t => Bezier(control, t), (t, a) => Mathf.Lerp(r0, r1, t), rs, sides, reference);
                // Close both ends with a flat fan (Tube leaves them open, and an open end shows through where nothing covers it: a
                // crook's tip, a tusk's butt). Ring k is vertices k * (sides + 1) on; each cap has its own vertices so it shades flat.
                var v = new List<Vector3>(m.vertices); var n = new List<Vector3>(m.normals); var uv = new List<Vector2>(m.uv); var tri = new List<int>(m.triangles); int cols = sides + 1;
                foreach (int k in new[] { 0, rings - 1 })
                {
                    var c = Bezier(control, rs[k]); var outward = (Bezier(control, Mathf.Min(1, rs[k] + .01f)) - Bezier(control, Mathf.Max(0, rs[k] - .01f))).normalized * (k == 0 ? -1 : 1);
                    int ci = v.Count; v.Add(c); n.Add(outward); uv.Add(uv[k * cols]);
                    for (int s = 0; s < sides; s++) { v.Add(v[k * cols + s]); n.Add(outward); uv.Add(uv[k * cols + s]); }
                    for (int s = 0; s < sides; s++)
                    {
                        int a = ci + 1 + s, b = ci + 1 + (s + 1) % sides;
                        if (Vector3.Dot(Vector3.Cross(v[a] - c, v[b] - c), outward) > 0) { tri.Add(ci); tri.Add(a); tri.Add(b); } else { tri.Add(ci); tri.Add(b); tri.Add(a); }
                    }
                }
                m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateBounds(); return m;
            });
        }
        static Vector3 Bezier(Vector3[] p, float t)
        {
            var q = (Vector3[])p.Clone();
            for (int n = q.Length - 1; n > 0; n--) for (int i = 0; i < n; i++) q[i] = Vector3.Lerp(q[i], q[i + 1], t);
            return q[0];
        }
        /// <summary>A lumpy ball (a burl, a stone fist, a slate slab) from the zone builder's Blob.</summary>
        public static Mesh Lump(string key, int seed, float roughness, bool faceted = false)
        {
            return Cached(key, () => Crulanda.World.ZoneMeshes.Blob(seed, roughness, false, 8, 12, 1, 1, faceted));
        }
        /// <summary>An upright cone, base at the origin (a spike, a lantern cap, a thorn).</summary>
        public static Mesh Cone(int sides = 8) { return Cached("cone" + sides, () => Crulanda.World.ZoneMeshes.Cone(.5f, 1, sides)); }
        /// <summary>
        /// A ring segment in the XZ plane, centred on y 0, from angle a0 to a1 (degrees, anticlockwise from +X seen from above) and
        /// radius r0 to r1, curved in steps of 10 degrees or less: a crescent axe head, a sickle, a handle.
        /// </summary>
        public static Mesh Arc(string key, float r0, float r1, float a0, float a1, float h)
        {
            return Cached(key, () =>
            {
                var b = new Builder(); int n = Mathf.Max(6, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 10)); float y0 = -h / 2, y1 = h / 2, s = Mathf.Sign(a1 - a0);
                Vector3 D(float a) { return new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad)); }
                Vector3 P(float r, float a, float y) { return D(a) * r + Vector3.up * y; }
                for (int i = 0; i < n; i++)
                {
                    float p = Mathf.Lerp(a0, a1, (float)i / n), q = Mathf.Lerp(a0, a1, (float)(i + 1) / n); var radial = D((p + q) / 2);
                    b.Quad(b.Add(P(r0, p, y1)), b.Add(P(r1, p, y1)), b.Add(P(r1, q, y1)), b.Add(P(r0, q, y1)), Vector3.up);
                    b.Quad(b.Add(P(r0, p, y0)), b.Add(P(r1, p, y0)), b.Add(P(r1, q, y0)), b.Add(P(r0, q, y0)), Vector3.down);
                    b.Quad(b.Add(P(r1, p, y0)), b.Add(P(r1, p, y1)), b.Add(P(r1, q, y1)), b.Add(P(r1, q, y0)), radial);
                    b.Quad(b.Add(P(r0, p, y0)), b.Add(P(r0, p, y1)), b.Add(P(r0, q, y1)), b.Add(P(r0, q, y0)), -radial);
                }
                Vector3 T(float a) { return Vector3.Cross(D(a), Vector3.up) * s; }   // along the arc toward a1
                b.Quad(b.Add(P(r0, a0, y0)), b.Add(P(r0, a0, y1)), b.Add(P(r1, a0, y1)), b.Add(P(r1, a0, y0)), -T(a0));
                b.Quad(b.Add(P(r0, a1, y0)), b.Add(P(r0, a1, y1)), b.Add(P(r1, a1, y1)), b.Add(P(r1, a1, y0)), T(a1));
                return b.Build();
            });
        }
        /// <summary>
        /// One mesh made of several placed copies of another (six flanges, a ring of studs, a cage's bars): one part, not many.
        /// A built-in primitive is swapped for a generated one of the same size, since only readable meshes can be combined in a
        /// player build.
        /// </summary>
        public static Mesh Many(string key, Mesh part, params Matrix4x4[] places)
        {
            return Cached(key, () =>
            {
                part = Combinable(part);
                var c = new CombineInstance[places.Length]; for (int i = 0; i < places.Length; i++) c[i] = new CombineInstance { mesh = part, transform = places[i] };
                var m = new Mesh(); m.CombineMeshes(c, true, true); m.RecalculateBounds(); return m;
            });
        }
        /// <summary>The generated stand-in for a built-in primitive (same size: cube 1, sphere 1 across, cylinder 2 tall and 1 across).</summary>
        static Mesh Combinable(Mesh part)
        {
            if (part == prims[(int)PrimitiveType.Cube]) return Plate("unit.cube", new[] { new Vector2(-.5f, -.5f), new Vector2(.5f, -.5f), new Vector2(.5f, .5f), new Vector2(-.5f, .5f) }, 1);
            if (part == prims[(int)PrimitiveType.Cylinder]) return Lathe("unit.cylinder", new[] { new Vector2(0, -1), new Vector2(.5f, -1), new Vector2(.5f, 1), new Vector2(0, 1) }, 14);
            if (part == prims[(int)PrimitiveType.Sphere])
            {
                var arc = new Vector2[9]; for (int i = 0; i <= 8; i++) { float a = Mathf.PI * i / 8; arc[i] = new Vector2(Mathf.Sin(a) * .5f, -Mathf.Cos(a) * .5f); }
                return Lathe("unit.sphere", arc, 14, true);
            }
            return part;
        }
        /// <summary>A placement for <see cref="Many"/>.</summary>
        public static Matrix4x4 At(Vector3 pos, Vector3 euler, Vector3 scale) { return Matrix4x4.TRS(pos, Quaternion.Euler(euler), scale); }
        /// <summary>
        /// One mesh made of different placed meshes (a pauldron's dome, its lame and its strap): one part, not three. Built-in
        /// primitives are swapped for generated ones of the same size, as in <see cref="Many"/>.
        /// </summary>
        public static Mesh Join(string key, IList<(Mesh mesh, Matrix4x4 at)> parts)
        {
            return Cached(key, () =>
            {
                var c = new CombineInstance[parts.Count]; long verts = 0;
                for (int i = 0; i < parts.Count; i++) { var part = Combinable(parts[i].mesh); c[i] = new CombineInstance { mesh = part, transform = parts[i].at }; verts += part.vertexCount; }
                var m = new Mesh(); if (verts > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.CombineMeshes(c, true, true); m.RecalculateBounds(); return m;
            });
        }

        /// <summary>
        /// A walled shell round the body or a limb (a breastplate, a hauberk, a hood, a greave). Rings run bottom to top, each
        /// (half width, height, half depth, centre z) and each a rounded rectangle: <paramref name="square"/> 2 is an ellipse, higher
        /// is boxier, so it can hug the square torso without bulging. The wall is <paramref name="thick"/> deep (the inside is the
        /// rings drawn in by that much) and closed at the hem and at the top. <paramref name="open"/> leaves a gap in each ring of that
        /// many degrees (one value per ring, the last repeated), centred on the direction <paramref name="gapAt"/> (degrees from +X
        /// toward +Z: 90 is the front, 270 the back), with the cut edges walled too: a hood's face, a greave's open back. Without
        /// gaps the seam is at <paramref name="gapAt"/> and shades smooth across.
        /// </summary>
        public static Mesh Shell(string key, Vector4[] rings, float thick, float square = 6, float[] open = null, float gapAt = 270, int sides = 24, bool smooth = true)
        {
            return Cached(key, () =>
            {
                var b = new Builder(); int n = rings.Length; float e = 2 / square;
                var seams = new List<(int a, int b)>();
                float Pow(float c) { return Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), e); }
                float Gap(int i) { return open == null || open.Length == 0 ? 0 : open[Mathf.Min(i, open.Length - 1)]; }
                float Angle(int i, int j) { float o = Gap(i); return (gapAt + o / 2 + (360 - o) * j / sides) * Mathf.Deg2Rad; }
                Vector3 Point(int i, float a, float shrink)
                {
                    var r = rings[i]; float w = Mathf.Max(0, r.x - shrink), d = Mathf.Max(0, r.z - shrink);
                    return new Vector3(w * Pow(Mathf.Cos(a)), r.y, r.w + d * Pow(Mathf.Sin(a)));
                }
                int[] Ring(int i, float shrink)
                {
                    var idx = new int[sides + 1]; for (int j = 0; j <= sides; j++) idx[j] = b.Add(Point(i, Angle(i, j), shrink));
                    seams.Add((idx[0], idx[sides])); return idx;
                }
                var outer = new int[n][]; var inner = new int[n][];
                foreach (var (shrink, sign, keep) in new[] { (0f, 1f, outer), (thick, -1f, inner) })
                {
                    int[] lower = null;
                    for (int i = 0; i + 1 < n; i++)
                    {
                        if (lower == null || !smooth) lower = Ring(i, shrink);
                        var upper = Ring(i + 1, shrink); keep[i] = keep[i] ?? lower; keep[i + 1] = upper;
                        var r0 = rings[i]; var r1 = rings[i + 1]; float dy = r1.y - r0.y, ds = (r1.x + r1.z - r0.x - r0.z) / 2, zc = (r0.w + r1.w) / 2;
                        for (int j = 0; j < sides; j++)
                        {
                            var c = (b.v[lower[j]] + b.v[lower[j + 1]] + b.v[upper[j]] + b.v[upper[j + 1]]) / 4;
                            var radial = new Vector3(c.x, 0, c.z - zc); if (radial.sqrMagnitude > 1e-12f) radial.Normalize();
                            b.Quad(lower[j], upper[j], upper[j + 1], lower[j + 1], (radial * dy - Vector3.up * ds) * sign);
                        }
                        lower = upper;
                    }
                }
                // The hem and the top: a band from the outside in, flat shaded.
                foreach (var (i, outward) in new[] { (0, Vector3.down), (n - 1, Vector3.up) })
                {
                    var o = new int[sides + 1]; var u = new int[sides + 1];
                    for (int j = 0; j <= sides; j++) { o[j] = b.Add(b.v[outer[i][j]]); u[j] = b.Add(b.v[inner[i][j]]); }
                    for (int j = 0; j < sides; j++) b.Quad(o[j], o[j + 1], u[j + 1], u[j], outward);
                }
                // The cut edges of a gap, walled across.
                if (open != null)
                    for (int i = 0; i + 1 < n; i++)
                        foreach (int j in new[] { 0, sides })
                        {
                            float a = Angle(i, j), step = j == 0 ? -.05f : .05f; var outward = Point(i, a + step, 0) - Point(i, a, 0);
                            b.Quad(b.Add(b.v[outer[i][j]]), b.Add(b.v[outer[i + 1][j]]), b.Add(b.v[inner[i + 1][j]]), b.Add(b.v[inner[i][j]]), outward);
                        }
                var m = b.Build();
                // A closed ring's first and last columns are the same points: share their normals so the seam does not show.
                var normals = m.normals;
                foreach (var (p, q) in seams)
                    if ((b.v[p] - b.v[q]).sqrMagnitude < 1e-10f) { var avg = (normals[p] + normals[q]).normalized; normals[p] = normals[q] = avg; }
                m.normals = normals; return m;
            });
        }
        /// <summary>
        /// Shell rings resampled into rows every <paramref name="step"/> up, for mail and scale. Mail: each row swells by
        /// <paramref name="depth"/> halfway up, a soft ripple that catches the light in bands. Scales: each row's lower edge stands
        /// proud and tucks in under the next, crisp rows of overlapping plates. Gaps (<paramref name="open"/>) are carried along.
        /// </summary>
        public static Vector4[] Rows(Vector4[] stations, float step, float depth, bool scales, float[] open, out float[] rowOpen)
        {
            var rows = new List<Vector4>(); var gaps = new List<float>();
            float y0 = stations[0].y, y1 = stations[stations.Length - 1].y;
            (Vector4 r, float o) Sample(float y)
            {
                for (int i = 0; i + 1 < stations.Length; i++)
                    if (y <= stations[i + 1].y || i + 2 == stations.Length)
                    {
                        float t = Mathf.Clamp01((y - stations[i].y) / Mathf.Max(1e-5f, stations[i + 1].y - stations[i].y));
                        float g0 = open == null ? 0 : open[Mathf.Min(i, open.Length - 1)], g1 = open == null ? 0 : open[Mathf.Min(i + 1, open.Length - 1)];
                        var r = Vector4.Lerp(stations[i], stations[i + 1], t); r.y = y; return (r, Mathf.Lerp(g0, g1, t));
                    }
                return (stations[0], 0);
            }
            void Put(float y, float swell) { var (r, o) = Sample(y); rows.Add(new Vector4(r.x + swell, r.y, r.z + swell, r.w)); gaps.Add(o); }
            for (float y = y0; y < y1 - step * .3f; y += step)
            {
                if (scales) { Put(y, depth); Put(Mathf.Min(y + step * .88f, y1), 0); }
                else { Put(y, 0); Put(Mathf.Min(y + step * .5f, y1), depth); }
            }
            Put(y1, 0);
            rowOpen = gaps.ToArray(); return rows.ToArray();
        }
        /// <summary>
        /// A flat panel cut to an outline drawn in (x, y), standing up, <paramref name="thick"/> through along Z with its face domed
        /// by <paramref name="bulge"/> toward +Z (<paramref name="front"/>) or -Z: a tabard, a back drape, a coat skirt. The outline
        /// must be star-shaped round <paramref name="fan"/> (by default its centroid; a ragged hem needs a point nearer the top).
        /// The dome stays centred on the centroid either way.
        /// </summary>
        public static Mesh Panel(string key, Vector2[] outline, float thick, float bulge = 0, bool front = true, Vector2? fan = null)
        {
            var o = new Vector2[outline.Length]; for (int i = 0; i < o.Length; i++) o[i] = front ? new Vector2(outline[i].x, -outline[i].y) : outline[i];
            Vector2? fn = fan.HasValue ? (front ? new Vector2(fan.Value.x, -fan.Value.y) : fan.Value) : (Vector2?)null;
            return Plate(key, o, thick, bulge, fn.HasValue ? Centroid(o) : (Vector2?)null, null, 0, 1, Quaternion.Euler(front ? 90 : -90, 0, 0), fn);
        }

        static Vector2 Centroid(Vector2[] o) { var c = Vector2.zero; foreach (var p in o) c += p; return c / o.Length; }
        static Vector2 Reach(Vector2[] o, Vector2 c) { var r = Vector2.zero; foreach (var p in o) { r.x = Mathf.Max(r.x, Mathf.Abs(p.x - c.x)); r.y = Mathf.Max(r.y, Mathf.Abs(p.y - c.y)); } return r * 1.05f; }
    }
}
