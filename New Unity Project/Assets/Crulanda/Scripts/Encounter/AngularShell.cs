using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Faceted armour shells built round measured points (play note 239, 2026-10-18: "helms and gauntlet still tooo round"). A
    /// shell is a stack of rings; each ring is the polygon of a few flat facets (8 to 12 round a head, 6 to 8 round an arm) that
    /// stands a set gap outside every point of the body measured in its band (the points between the ring below and the ring
    /// above), so the plates between two rings stay outside the body all the way between them (a polygon's sides move linearly
    /// with its facets' distances, and both rings clear every point of that stretch). The top may close in a point, set just high
    /// enough that its sloping gores clear everything above the last ring. Each face has its own corners, so it shades flat and
    /// its edges read crisp; the wall has an inside and edges, so an opening (a barbute's T, a coif's face) shows a thickness.
    /// Pure geometry: no scene, testable on its own.
    /// </summary>
    public static class AngularShell
    {
        /// <summary>The normal of facet k of n in the ring's plane (x across, y forward): facet 0 faces +y (forward), then round
        /// toward +x.</summary>
        public static Vector2 Normal(int k, int n) { float a = 2 * Mathf.PI * k / n; return new Vector2(Mathf.Sin(a), Mathf.Cos(a)); }

        /// <summary>How far the points reach along each facet's normal, plus <paramref name="gap"/> (0 when there are none).</summary>
        public static float[] Support(IList<Vector2> pts, int n, float gap)
        {
            var h = new float[n];
            for (int k = 0; k < n; k++)
            {
                var d = Normal(k, n); float best = float.MinValue;
                for (int i = 0; i < pts.Count; i++) best = Mathf.Max(best, Vector2.Dot(pts[i], d));
                h[k] = (best == float.MinValue ? 0 : best) + gap;
            }
            return h;
        }

        /// <summary>The polygon whose facet k lies <paramref name="h"/>[k] out along facet k's normal: corner k is where facet k
        /// meets facet k + 1.</summary>
        public static Vector2[] Corners(float[] h)
        {
            int n = h.Length; var c = new Vector2[n];
            for (int k = 0; k < n; k++)
            {
                Vector2 a = Normal(k, n), b = Normal((k + 1) % n, n); float h0 = h[k], h1 = h[(k + 1) % n];
                float det = a.x * b.y - a.y * b.x;
                c[k] = new Vector2((h0 * b.y - a.y * h1) / det, (a.x * h1 - h0 * b.x) / det);
            }
            return c;
        }

        /// <summary>
        /// The facets' distances for rings at heights <paramref name="ys"/> (ascending) round points with heights, so the plates
        /// between every two rings stand at least the gap (<paramref name="gaps"/>, per ring, blended between) outside every point
        /// between them: each ring starts at the points right at its height, then each stretch is raised (both its rings) by as
        /// much as its worst point pokes through. With <paramref name="apex"/> the last ring also holds every point above it (the
        /// gores up to the point clear them: <see cref="ApexHeight"/>). <paramref name="given"/> rings start where given.
        /// </summary>
        public static float[][] Fit(float[] ys, IList<(Vector2 p, float y)> pts, int n, float[] gaps, bool apex, float[][] given = null)
        {
            int R = ys.Length; var h = new float[R][]; var have = new bool[R];
            for (int i = 0; i < R; i++)
            {
                if (given != null && given[i] != null) { h[i] = (float[])given[i].Clone(); have[i] = true; continue; }
                float near = float.MaxValue; if (i > 0) near = Mathf.Min(near, ys[i] - ys[i - 1]); if (i + 1 < R) near = Mathf.Min(near, ys[i + 1] - ys[i]);
                float e = near == float.MaxValue ? 1e-3f : near * .25f, lo = ys[i] - e, hi = apex && i == R - 1 ? float.MaxValue : ys[i] + e;
                var band = new List<Vector2>(); foreach (var (p, y) in pts) if (y >= lo && y <= hi) band.Add(p);
                have[i] = band.Count > 0; h[i] = Support(band, n, gaps[i]);
            }
            for (int i = 0; i < R; i++)
            {
                if (have[i]) continue; int best = -1;
                for (int j = 0; j < R; j++) if (have[j] && (best < 0 || Mathf.Abs(j - i) < Mathf.Abs(best - i))) best = j;
                if (best >= 0) for (int k = 0; k < n; k++) h[i][k] = h[best][k] + gaps[i] - gaps[best];
            }
            for (int s = 0; s + 1 < R; s++)
            {
                float y0 = ys[s], y1 = ys[s + 1], span = y1 - y0; if (span <= 1e-7f) continue;
                for (int k = 0; k < n; k++)
                {
                    var d = Normal(k, n); float worst = 0;
                    foreach (var (p, y) in pts)
                    {
                        if (y < y0 || y > y1) continue; float t = (y - y0) / span;
                        worst = Mathf.Max(worst, Vector2.Dot(p, d) + Mathf.Lerp(gaps[s], gaps[s + 1], t) - Mathf.Lerp(h[s][k], h[s + 1][k], t));
                    }
                    if (worst > 0) { h[s][k] += worst; h[s + 1][k] += worst; }
                }
            }
            return h;
        }

        /// <summary>
        /// How high (in the rings' up units) a point must stand over the axis for the gores from the last ring (supports
        /// <paramref name="hLast"/> at height <paramref name="yLast"/>) to clear every point above it: each point (its supports
        /// along the facets, with the gap, and its height) must lie inside the cone's slice at its height.
        /// </summary>
        public static float ApexHeight(float[] hLast, float yLast, IList<(Vector2 p, float y)> above, float gap)
        {
            int n = hLast.Length; float need = yLast;
            foreach (var (p, y) in above)
            {
                if (y <= yLast) continue;
                for (int k = 0; k < n; k++)
                {
                    float rho = (Vector2.Dot(p, Normal(k, n)) + gap) / Mathf.Max(1e-5f, hLast[k]);
                    if (rho <= 0) continue;
                    if (rho >= .995f) { need = Mathf.Max(need, y + (y - yLast) * 200); continue; }
                    need = Mathf.Max(need, (y - yLast * rho) / (1 - rho));
                }
            }
            return need;
        }

        /// <summary>
        /// The walled shell through <paramref name="rings"/> (outer corners, bottom to top, the same count each), its inside
        /// drawn in toward each ring's centre by <paramref name="thick"/>, closed at the top in <paramref name="apex"/> when given.
        /// <paramref name="open"/>(segment, facet) leaves a face out (segment i lies between ring i and i + 1; the last segment,
        /// with an apex, is the one up to it; facet k runs from corner k - 1 to corner k); every edge of a face whose neighbour is
        /// open or missing gets a wall, so rims and openings show a thickness. Every face has its own corners (flat shading).
        /// </summary>
        public static Mesh Build(IList<Vector3[]> rings, IList<Vector3> centres, float thick, System.Func<int, int, bool> open = null, Vector3? apex = null)
        {
            int R = rings.Count, n = rings[0].Length; int segs = R - 1 + (apex.HasValue ? 1 : 0);
            var inner = new List<Vector3[]>();
            for (int i = 0; i < R; i++)
            {
                var ring = new Vector3[n];
                for (int k = 0; k < n; k++) { var q = rings[i][k]; var d = centres[i] - q; ring[k] = q + (d.sqrMagnitude > 1e-12f ? d.normalized * Mathf.Min(thick, d.magnitude * .5f) : Vector3.zero); }
                inner.Add(ring);
            }
            Vector3 apexIn = Vector3.zero;
            if (apex.HasValue) { var d = centres[R - 1] - apex.Value; apexIn = apex.Value + (d.sqrMagnitude > 1e-12f ? d.normalized * Mathf.Min(thick, d.magnitude * .5f) : Vector3.zero); }
            bool Open(int s, int k) { if (s < 0 || s >= segs) return true; k = (k % n + n) % n; return open != null && open(s, k); }
            var v = new List<Vector3>(); var t = new List<int>();
            void Face(Vector3[] p, Vector3 outward)
            {
                int b = v.Count; v.AddRange(p);
                var nrm = Vector3.Cross(p[1] - p[0], p[2] - p[0]); bool flip = Vector3.Dot(nrm, outward) < 0;
                for (int i = 1; i + 1 < p.Length; i++) { if (flip) { t.Add(b); t.Add(b + i + 1); t.Add(b + i); } else { t.Add(b); t.Add(b + i); t.Add(b + i + 1); } }
            }
            void Wall(Vector3 a, Vector3 b, Vector3 ai, Vector3 bi, Vector3 from) { Face(new[] { a, b, bi, ai }, (a + b) / 2 - from); }
            for (int s = 0; s < segs; s++)
            {
                bool top = apex.HasValue && s == R - 1;
                for (int k = 0; k < n; k++)
                {
                    if (Open(s, k)) continue;
                    int k0 = (k - 1 + n) % n;
                    Vector3 a0 = rings[s][k0], a1 = rings[s][k], ai0 = inner[s][k0], ai1 = inner[s][k];
                    Vector3 mid = top ? (a0 + a1 + apex.Value) / 3 : (a0 + a1 + rings[s + 1][k0] + rings[s + 1][k]) / 4;
                    Vector3 c = Vector3.Lerp(centres[s], top ? centres[R - 1] : centres[s + 1], .5f);
                    if (top)
                    {
                        Face(new[] { a0, a1, apex.Value }, mid - c); Face(new[] { ai0, ai1, apexIn }, c - mid);
                        if (Open(s, k - 1)) Wall(a0, apex.Value, ai0, apexIn, mid);
                        if (Open(s, k + 1)) Wall(a1, apex.Value, ai1, apexIn, mid);
                        if (Open(s - 1, k)) Wall(a0, a1, ai0, ai1, mid);
                        continue;
                    }
                    Vector3 b0 = rings[s + 1][k0], b1 = rings[s + 1][k], bi0 = inner[s + 1][k0], bi1 = inner[s + 1][k];
                    Face(new[] { a0, a1, b1, b0 }, mid - c); Face(new[] { ai0, ai1, bi1, bi0 }, c - mid);
                    if (Open(s, k - 1)) Wall(a0, b0, ai0, bi0, mid);
                    if (Open(s, k + 1)) Wall(a1, b1, ai1, bi1, mid);
                    if (Open(s - 1, k)) Wall(a0, a1, ai0, ai1, mid);
                    if (Open(s + 1, k)) Wall(b0, b1, bi0, bi1, mid);
                }
            }
            var m = new Mesh { name = "Angular shell" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>A ring's corners (in its plane: x across, y forward) set in space: <paramref name="origin"/> plus x along
        /// <paramref name="across"/> and y along <paramref name="forward"/>.</summary>
        public static Vector3[] Place(Vector2[] corners, Vector3 origin, Vector3 across, Vector3 forward)
        {
            var r = new Vector3[corners.Length]; for (int k = 0; k < r.Length; k++) r[k] = origin + across * corners[k].x + forward * corners[k].y; return r;
        }

        /// <summary>
        /// How round a mesh is (the measure behind note 239): the share of its triangles shaded smooth, whose corners' normals
        /// turn more than <paramref name="degrees"/> from the face's own. A sphere, a lathed dome or a smoothed shell is near 1; a
        /// faceted shell of flat plates is 0.
        /// </summary>
        public static float Roundness(Mesh m, float degrees = 10)
        {
            if (m == null) return 0;
            var v = m.vertices; var n = m.normals; var t = m.triangles; if (t.Length == 0 || n.Length != v.Length) return 0;
            int smooth = 0, all = 0; float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                var f = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]); if (f.sqrMagnitude < 1e-16f) continue;
                f.Normalize(); all++;
                if (Vector3.Dot(n[t[i]], f) < cos || Vector3.Dot(n[t[i + 1]], f) < cos || Vector3.Dot(n[t[i + 2]], f) < cos) smooth++;
            }
            return all == 0 ? 0 : (float)smooth / all;
        }
    }
}
