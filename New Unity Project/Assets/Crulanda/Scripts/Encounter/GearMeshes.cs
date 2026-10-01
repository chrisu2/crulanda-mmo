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

        /// <summary>A bent rod through the points of a Bezier curve (a knuckle bow, a root curl, a tine), tapering from r0 to r1.</summary>
        public static Mesh Rod(string key, Vector3[] control, float r0, float r1, int sides = 6, int rings = 9)
        {
            return Cached(key, () =>
            {
                var rs = new float[rings]; for (int i = 0; i < rings; i++) rs[i] = (float)i / (rings - 1);
                // Square to the rod's general run: the axis it runs along least, so the rings never twist.
                var dir = (control[control.Length - 1] - control[0]).normalized; var reference = Vector3.right;
                foreach (var axis in new[] { Vector3.up, Vector3.forward }) if (Mathf.Abs(Vector3.Dot(dir, axis)) < Mathf.Abs(Vector3.Dot(dir, reference))) reference = axis;
                return Crulanda.World.ZoneMeshes.Tube(t => Bezier(control, t), (t, a) => Mathf.Lerp(r0, r1, t), rs, sides, reference);
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
        /// <summary>A ring segment in the XZ plane (Crulanda.World.ZoneMeshes.Arc): a crescent axe head, a sickle, a handle.</summary>
        public static Mesh Arc(string key, float r0, float r1, float a0, float a1, float h)
        {
            return Cached(key, () => { var m = Crulanda.World.ZoneMeshes.Arc(r0, r1, a0, a1, h); var v = m.vertices; for (int i = 0; i < v.Length; i++) v[i].y -= h / 2; m.vertices = v; m.RecalculateBounds(); return m; });
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

        static Vector2 Centroid(Vector2[] o) { var c = Vector2.zero; foreach (var p in o) c += p; return c / o.Length; }
        static Vector2 Reach(Vector2[] o, Vector2 c) { var r = Vector2.zero; foreach (var p in o) { r.x = Mathf.Max(r.x, Mathf.Abs(p.x - c.x)); r.y = Mathf.Max(r.y, Mathf.Abs(p.y - c.y)); } return r * 1.05f; }
    }
}
