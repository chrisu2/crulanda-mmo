using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Armour built on the model itself instead of warped from the old figure's drawing (play note 239, 2026-10-18: "helms and
    /// gauntlet still tooo round"; 2026-10-10: "all seem to not fit properly"). The old gloves were balls drawn round the old
    /// mitten hand and stretched over the model's hand box, so a glove was a ball bigger than the fist; the old pauldrons were
    /// domes. Here the pieces come from the model's own points in its bind pose (the cloud): a glove is the hand's own skin
    /// stood off it by a few millimetres and skinned to the same finger bones, so it closes round a grip; a cuff, a band or a
    /// lame is a ring of flat plates round the forearm or upper arm's points (AngularShell); a pauldron is a faceted cap over the
    /// shoulder's points, open on the neck side. Every piece is in the cloud's form (root space at bind, the cloud's bones,
    /// <see cref="Cloud.bindInverse"/>), ready for a SkinnedMeshRenderer on the model's bones.
    /// </summary>
    public static partial class ModelArmour
    {
        static readonly Dictionary<string, Mesh> built = new Dictionary<string, Mesh>();

        /// <summary>The cloud's index of a bone by name, or -1.</summary>
        public static int BoneIndex(Cloud c, string name) { return System.Array.IndexOf(c.bones, name); }
        static Vector3 BindAt(Cloud c, int i) { return c.bind[i].GetColumn(3); }
        static int Dominant(BoneWeight b)
        {
            return b.weight0 >= b.weight1 && b.weight0 >= b.weight2 && b.weight0 >= b.weight3 ? b.boneIndex0 : b.weight1 >= b.weight2 && b.weight1 >= b.weight3 ? b.boneIndex1 : b.weight2 >= b.weight3 ? b.boneIndex2 : b.boneIndex3;
        }

        /// <summary>An arm measured in the bind pose (root space): its joints, the forearm's axis (elbow to wrist), the back of the
        /// hand (away from the palm), outward from the body, the lengths, and how far the hand's back and the forearm stand from
        /// their axis.</summary>
        public struct ArmMeasure
        {
            public bool ok; public Vector3 shoulder, elbow, wrist, knuckles, axis, back, outward; public float fore, upper, hand, backReach;
            public int upperBone, foreBone, handBone, clavicle;
            /// <summary>A point on the forearm's line at t (0 the elbow, 1 the wrist; past 1 into the hand).</summary>
            public Vector3 Along(float t) { return elbow + axis * fore * t; }
        }
        public static ArmMeasure Arm(Cloud c, bool left)
        {
            var a = new ArmMeasure(); string s = left ? "_l" : "_r";
            a.upperBone = BoneIndex(c, "upperarm" + s); a.foreBone = BoneIndex(c, "lowerarm" + s); a.handBone = BoneIndex(c, "hand" + s);
            a.clavicle = BoneIndex(c, "clavicle" + s); if (a.clavicle < 0) a.clavicle = BoneIndex(c, "spine_03");
            if (a.upperBone < 0 || a.foreBone < 0 || a.handBone < 0) return a;
            a.shoulder = BindAt(c, a.upperBone); a.elbow = BindAt(c, a.foreBone); a.wrist = BindAt(c, a.handBone);
            a.fore = (a.wrist - a.elbow).magnitude; a.upper = (a.elbow - a.shoulder).magnitude; if (a.fore < 1e-5f) return a;
            a.axis = (a.wrist - a.elbow) / a.fore;
            int mid = BoneIndex(c, "middle_01" + s), index = BoneIndex(c, "index_01" + s), pinky = BoneIndex(c, "pinky_01" + s), thumb = BoneIndex(c, "thumb_01" + s);
            a.knuckles = mid >= 0 ? BindAt(c, mid) : a.Along(1.35f);
            a.hand = Mathf.Max(Vector3.Dot(a.knuckles - a.wrist, a.axis), a.fore * .2f);
            // The back of the hand: the thumb's root sits on the palm's side of the plane through the wrist and the index and little
            // knuckles (no handedness assumed).
            var back = Vector3.up;
            if (index >= 0 && pinky >= 0 && thumb >= 0)
            {
                var n = Vector3.Cross(BindAt(c, index) - a.wrist, BindAt(c, pinky) - a.wrist);
                if (n.sqrMagnitude > 1e-12f) { n.Normalize(); back = Vector3.Dot(BindAt(c, thumb) - a.wrist, n) > 0 ? -n : n; }
            }
            back = Vector3.ProjectOnPlane(back, a.axis); a.back = back.sqrMagnitude > 1e-10f ? back.normalized : Vector3.ProjectOnPlane(Vector3.up, a.axis).normalized;
            // Outward: from the other shoulder across to this one, level (no side assumed).
            int other = BoneIndex(c, "upperarm" + (left ? "_r" : "_l"));
            var across = other >= 0 ? Vector3.ProjectOnPlane(a.shoulder - BindAt(c, other), Vector3.up) : Vector3.zero;
            a.outward = across.sqrMagnitude > 1e-10f ? across.normalized : new Vector3(left ? 1 : -1, 0, 0);
            float reach = 0;
            for (int i = 0; i < c.p.Length; i++)
            {
                if (Dominant(c.w[i]) != a.handBone) continue;
                reach = Mathf.Max(reach, Vector3.Dot(c.p[i] - a.wrist, a.back));
            }
            a.backReach = reach > 0 ? reach : a.fore * .1f;
            a.ok = true; return a;
        }

        /// <summary>The cloud's points of one arm (sleeve, skin, hand).</summary>
        public static List<Vector3> ArmPoints(Cloud c, bool left, bool noThumb = false)
        {
            var want = left ? Part.ArmL : Part.ArmR; var list = new List<Vector3>();
            for (int i = 0; i < c.p.Length; i++)
                if (c.part[i] == want && !(noThumb && c.bones[Dominant(c.w[i])].StartsWith("thumb", System.StringComparison.Ordinal))) list.Add(c.p[i]);
            return list;
        }

        /// <summary>
        /// A faceted shell round <paramref name="pts"/> along an axis: rings at heights <paramref name="hs"/> (distances along
        /// <paramref name="axis"/> from <paramref name="origin"/>, ascending), each the polygon of <paramref name="n"/> flat facets
        /// (facet 0 facing <paramref name="fwd"/>) standing <paramref name="gap"/> plus <paramref name="extra"/>[i] outside the points
        /// between its neighbours' heights (held inside <paramref name="bandLo"/>..<paramref name="bandHi"/>); a ring with no points
        /// there takes the nearest ring's. With <paramref name="apex"/> it closes in a point <paramref name="lift"/> over what the
        /// gores must clear. The rings' supports (along each facet) and corners come back for placing what goes on it.
        /// </summary>
        public static Mesh AxisShell(IList<Vector3> pts, Vector3 origin, Vector3 axis, Vector3 fwd, float[] hs, float[] extra, int n, float gap, float thick,
            System.Func<int, int, bool> open, bool apex, float lift, float bandLo, float bandHi, out float[][] sup, out Vector3[][] rings, out Vector3 apexAt,
            string cacheKey = null, bool whole = false)
        {
            axis.Normalize(); fwd = Vector3.ProjectOnPlane(fwd, axis).normalized; var across = Vector3.Cross(axis, fwd).normalized;
            int R = hs.Length; rings = new Vector3[R][]; var centres = new Vector3[R];
            var flat = new List<(Vector2 q, float h)>(pts.Count);
            foreach (var p in pts)
            {
                var d = p - origin; float h = Vector3.Dot(d, axis); if (h < bandLo || h > bandHi) continue;
                var q = d - axis * h; flat.Add((new Vector2(Vector3.Dot(q, across), Vector3.Dot(q, fwd)), h));
            }
            var gaps = new float[R]; for (int i = 0; i < R; i++) gaps[i] = gap + (extra != null ? extra[i] : 0);
            if (whole)
            {
                // Every ring round the whole stretch (a trim ring over a cuff stands clear of all of it).
                var all = flat.ConvertAll(f => f.q); sup = new float[R][]; for (int i = 0; i < R; i++) sup[i] = AngularShell.Support(all, n, gaps[i]);
            }
            else if (!apex) sup = AngularShell.Fit(hs, flat, n, gaps, false);
            else
            {
                // The crown (2026-10-18, first run: a cone's point over the last ring shot a metre out of each shoulder): one more ring
                // the gap over the highest point, starting round the top's own points, then a low point just over it.
                float hTop = hs[R - 1]; foreach (var f in flat) hTop = Mathf.Max(hTop, f.h);
                var hs2 = new float[R + 1]; System.Array.Copy(hs, hs2, R); hs2[R] = hTop + gap;
                var g2 = new float[R + 1]; System.Array.Copy(gaps, g2, R); g2[R] = gaps[R - 1];
                var given = new float[R + 1][]; var crownPts = new List<Vector2>(); float from = Mathf.Lerp(hs[R - 1], hTop, .5f);
                foreach (var f in flat) if (f.h >= from) crownPts.Add(f.q);
                given[R] = AngularShell.Support(crownPts, n, g2[R]);
                hs = hs2; gaps = g2; R++; rings = new Vector3[R][]; centres = new Vector3[R];
                sup = AngularShell.Fit(hs, flat, n, gaps, false, given);
            }
            if (flat.Count == 0) for (int i = 0; i < R; i++) for (int k = 0; k < n; k++) sup[i][k] = gaps[i] + .01f;
            for (int i = 0; i < R; i++) { centres[i] = origin + axis * hs[i]; rings[i] = AngularShell.Place(AngularShell.Corners(sup[i]), centres[i], across, fwd); }
            apexAt = Vector3.zero; Vector3? top = null;
            if (apex) { apexAt = origin + axis * (hs[R - 1] + lift); top = apexAt; }
            if (cacheKey != null && built.TryGetValue(cacheKey, out var done) && done != null) return done;
            var m = AngularShell.Build(rings, centres, thick, open, top);
            if (cacheKey != null) { m.name = "Gear " + cacheKey; built[cacheKey] = m; }
            return m;
        }
        /// <summary>True when a mesh under this key was built already (it then carries its weights).</summary>
        static bool Made(string key) { return built.TryGetValue(key, out var m) && m != null; }

        /// <summary>A facet's outward direction in 3D for a shell made by <see cref="AxisShell"/> with this axis and forward.</summary>
        public static Vector3 FacetOut(int k, int n, Vector3 axis, Vector3 fwd)
        {
            axis.Normalize(); fwd = Vector3.ProjectOnPlane(fwd, axis).normalized; var across = Vector3.Cross(axis, fwd).normalized; var d = AngularShell.Normal(k, n);
            return across * d.x + fwd * d.y;
        }

        /// <summary>Gives a built mesh its weights (from <paramref name="weigh"/>, by vertex) and the cloud's bind poses.</summary>
        public static Mesh Skin(Cloud c, Mesh m, System.Func<Vector3, BoneWeight> weigh)
        {
            var v = m.vertices; var w = new BoneWeight[v.Length]; for (int i = 0; i < v.Length; i++) w[i] = weigh(v[i]);
            m.boneWeights = w; m.bindposes = c.bindInverse; return m;
        }
        public static System.Func<Vector3, BoneWeight> On(int bone) { return _ => new BoneWeight { boneIndex0 = Mathf.Max(0, bone), weight0 = 1 }; }
        public static System.Func<Vector3, BoneWeight> Between(int a, float wa, int b) { return _ => new BoneWeight { boneIndex0 = Mathf.Max(0, a), weight0 = wa, boneIndex1 = Mathf.Max(0, b), weight1 = 1 - wa }; }

        /// <summary>
        /// A ring of plates round an arm, from t0 to t1 along the line <paramref name="from"/> to <paramref name="to"/> (fractions;
        /// a cuff runs up the forearm from the wrist, a band round the palm runs past it), in <paramref name="count"/> rings of
        /// <paramref name="n"/> facets (facet 0 on the back of the hand), flaring by <paramref name="flare0"/> at t0 and
        /// <paramref name="flare1"/> at t1; only the arm's points between <paramref name="tLo"/> and <paramref name="tHi"/> are
        /// measured (a cuff does not take the hand's). Weighted by <paramref name="weigh"/>. Cached by its key.
        /// </summary>
        public static Mesh ArmRing(Cloud c, string key, bool left, Vector3 from, Vector3 to, float t0, float t1, int count, int n, float gap, float flare0, float flare1, float thick,
            float tLo, float tHi, Vector3 fwd, System.Func<Vector3, BoneWeight> weigh, System.Func<int, int, bool> open = null, bool whole = false, bool noThumb = false)
        {
            key = "arm." + key + "." + left + "." + Fl(t0, t1, count, n, gap, flare0, flare1, thick, tLo, tHi, whole ? 1 : 0, noThumb ? 1 : 0) + "." + c.GetHashCode();
            if (Made(key)) return built[key];
            float len = (to - from).magnitude; var axis = (to - from) / Mathf.Max(1e-6f, len);
            var hs = new float[count]; var extra = new float[count];
            for (int i = 0; i < count; i++) { float u = count == 1 ? 0 : (float)i / (count - 1); hs[i] = Mathf.Lerp(t0, t1, u) * len; extra[i] = Mathf.Lerp(flare0, flare1, u); }
            var m = AxisShell(ArmPoints(c, left, noThumb), from, axis, fwd, hs, extra, n, gap, thick, open, false, 0, tLo * len, tHi * len, out _, out _, out _, key, whole);
            return Skin(c, m, weigh);
        }
        static string Fl(params float[] x) { var sb = new System.Text.StringBuilder(); foreach (var f in x) sb.Append(f.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture)).Append(','); return sb.ToString(); }

        /// <summary>How far an arm's points reach along <paramref name="dir"/> from the line from..to, between t0 and t1 (a cuff's or
        /// a band's face, for what is set on it).</summary>
        public static float Reach(Cloud c, bool left, Vector3 from, Vector3 to, float t0, float t1, Vector3 dir)
        {
            float len = (to - from).magnitude; var axis = (to - from) / Mathf.Max(1e-6f, len); dir = Vector3.ProjectOnPlane(dir, axis).normalized; float best = 0;
            foreach (var p in ArmPoints(c, left))
            {
                float t = Vector3.Dot(p - from, axis) / Mathf.Max(1e-6f, len); if (t < t0 || t > t1) continue;
                best = Mathf.Max(best, Vector3.Dot(p - from, dir));
            }
            return best;
        }

        /// <summary>
        /// A cap of plates over a shoulder: the points of the torso and that arm within <paramref name="reach"/> of the shoulder
        /// joint and above <paramref name="drop"/> under it, ringed along an axis tipped out over the arm, closed in a low point,
        /// open on the neck's side; weighted part to the collarbone, part to the upper arm, so it rides the shoulder and swings a
        /// little with the arm. Gives the rings (for rims, ridges and accents) and the cap's outward direction.
        /// </summary>
        public static Mesh ShoulderCap(Cloud c, string key, bool left, float reach, float drop, float gap, float thick, float lift, int n,
            out Vector3[][] rings, out Vector3 apex, out Vector3 axisOut, out Vector3 fwdOut, out Mesh rim, float rimOut = 0, float rimTall = 0)
        {
            rim = null;
            var a = Arm(c, left); rings = null; apex = Vector3.zero; axisOut = Vector3.up; fwdOut = Vector3.forward;
            if (!a.ok) return null;
            var J = a.shoulder; var outward = a.outward;
            var u = (Vector3.up + outward * .5f).normalized; var fwd = Vector3.ProjectOnPlane(Vector3.back, u).normalized;   // the figure faces -z in the model's root (turned 180)
            axisOut = u; fwdOut = fwd;
            var pts = new List<Vector3>(); var side = left ? Part.ArmL : Part.ArmR;
            for (int i = 0; i < c.p.Length; i++)
            {
                if (c.part[i] != Part.Torso && c.part[i] != side) continue;
                var d = c.p[i] - J; if (d.magnitude > reach || Vector3.Dot(d, u) < -drop) continue;
                if (Vector3.Dot(d, outward) < -reach * .35f) continue;   // the neck and the other side are not this shoulder
                pts.Add(c.p[i]);
            }
            float top = -drop; foreach (var p in pts) top = Mathf.Max(top, Vector3.Dot(p - J, u));
            var hs = new[] { -drop, Mathf.Lerp(-drop, top, .4f), Mathf.Lerp(-drop, top, .72f) };
            var openSet = new bool[n]; for (int k = 0; k < n; k++) openSet[k] = Vector3.Dot(FacetOut(k, n, u, fwd), outward) < -.45f;   // the neck's side
            key = "shoulder." + key + "." + left + "." + Fl(reach, drop, gap, thick, lift, n, rimOut, rimTall) + "." + c.GetHashCode();
            bool made = Made(key);
            var m = AxisShell(pts, J, u, fwd, hs, null, n, gap, thick, (s, k) => openSet[k], true, lift, float.MinValue, float.MaxValue, out _, out rings, out apex, key);
            var weigh = Between(a.clavicle, .45f, a.upperBone);
            if (!made) Skin(c, m, weigh);
            if (rimTall > 0)
            {
                // The rim: a band round the cap's lower edge, on the cap's own slope and standing rimOut off it.
                string rk = key + ".rim";
                if (Made(rk)) rim = built[rk];
                else
                {
                    float f = Mathf.Clamp01(rimTall / Mathf.Max(1e-5f, hs[1] - hs[0]));
                    var r0 = new Vector3[n]; var r1 = new Vector3[n]; var c0 = J + u * hs[0]; var c1 = J + u * (hs[0] + rimTall);
                    for (int k = 0; k < n; k++)
                    {
                        var a0 = rings[0][k]; var a1 = Vector3.Lerp(rings[0][k], rings[1][k], f);
                        r0[k] = a0 + (a0 - c0).normalized * rimOut - u * rimOut * .5f; r1[k] = a1 + (a1 - c1).normalized * rimOut;
                    }
                    rim = AngularShell.Build(new[] { r0, r1 }, new[] { c0, c1 }, thick * .8f, (s, k) => openSet[k]); rim.name = "Gear " + rk;
                    Skin(c, rim, weigh); built[rk] = rim;
                }
            }
            return m;
        }

        /// <summary>
        /// A glove: the hand's own skin (the arms piece's triangles on the hand's bones: <paramref name="which"/> 0 the whole hand,
        /// 1 the palm and the thumb's root, 2 the fingers), each vertex stood <paramref name="off"/> out along its normal and kept
        /// on its own bones, every triangle its own corners (flat plates). With <paramref name="backOnly"/> above -1 only the
        /// triangles facing the back of the hand that much (a knuckle plate). Null when the arms piece cannot be read.
        /// </summary>
        public static Mesh HandShell(Cloud c, SkinnedMeshRenderer arms, bool left, float off, int which, Vector3 back, float backOnly = -1)
        {
            var mesh = arms != null ? arms.sharedMesh : null; if (mesh == null || !mesh.isReadable) return null;
            string key = "hand." + mesh.GetHashCode() + "." + left + "." + which + "." + off.ToString("0.######") + "." + backOnly.ToString("0.##");
            if (built.TryGetValue(key, out var done) && done != null) return done;
            var v = mesh.vertices; var nr = mesh.normals; var bw = mesh.boneWeights; var bp = mesh.bindposes; var bones = arms.bones;
            if (bw.Length != v.Length || nr.Length != v.Length || bp.Length != bones.Length) return null;
            string s = left ? "_l" : "_r"; int nb = bones.Length; var map = new int[nb]; var skin = new Matrix4x4[nb]; var kind = new int[nb];
            for (int i = 0; i < nb; i++)
            {
                string name = bones[i] != null ? bones[i].name : ""; int k = BoneIndex(c, name); map[i] = Mathf.Max(0, k);
                skin[i] = k >= 0 ? c.bind[k] * bp[i] : Matrix4x4.identity;
                bool finger = name.EndsWith(s) && (name.StartsWith("thumb") || name.StartsWith("index") || name.StartsWith("middle") || name.StartsWith("ring") || name.StartsWith("pinky"));
                kind[i] = name == "hand" + s ? 1 : name == "thumb_01" + s ? 3 : finger ? 2 : 0;
            }
            var P = new Vector3[v.Length]; var N = new Vector3[v.Length]; var K = new int[v.Length];
            var sum = new Dictionary<Vector3Int, Vector3>();
            for (int i = 0; i < v.Length; i++)
            {
                var b = bw[i];
                P[i] = skin[b.boneIndex0].MultiplyPoint3x4(v[i]) * b.weight0 + skin[b.boneIndex1].MultiplyPoint3x4(v[i]) * b.weight1 + skin[b.boneIndex2].MultiplyPoint3x4(v[i]) * b.weight2 + skin[b.boneIndex3].MultiplyPoint3x4(v[i]) * b.weight3;
                N[i] = (skin[b.boneIndex0].MultiplyVector(nr[i]) * b.weight0 + skin[b.boneIndex1].MultiplyVector(nr[i]) * b.weight1 + skin[b.boneIndex2].MultiplyVector(nr[i]) * b.weight2 + skin[b.boneIndex3].MultiplyVector(nr[i]) * b.weight3).normalized;
                K[i] = kind[Dominant(b)];
                var key3 = Snap(P[i], off); sum.TryGetValue(key3, out var acc); sum[key3] = acc + N[i];   // one normal where seams split a corner
            }
            bool Takes(int kd) { return which == 0 ? kd > 0 : which == 1 ? kd == 1 || kd == 3 : kd == 2 || kd == 3; }
            var outV = new List<Vector3>(); var outW = new List<BoneWeight>(); var outT = new List<int>(); var t = mesh.triangles;
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                int a0 = t[i], a1 = t[i + 1], a2 = t[i + 2];
                if (!Takes(K[a0]) || !Takes(K[a1]) || !Takes(K[a2])) continue;
                if (backOnly > -1)
                {
                    var fn = Vector3.Cross(P[a1] - P[a0], P[a2] - P[a0]); if (fn.sqrMagnitude < 1e-16f || Vector3.Dot(fn.normalized, back) < backOnly) continue;
                }
                foreach (int j in new[] { a0, a1, a2 })
                {
                    var n = sum[Snap(P[j], off)].normalized; outT.Add(outV.Count); outV.Add(P[j] + n * off);
                    var b = bw[j]; outW.Add(new BoneWeight { boneIndex0 = map[b.boneIndex0], weight0 = b.weight0, boneIndex1 = map[b.boneIndex1], weight1 = b.weight1, boneIndex2 = map[b.boneIndex2], weight2 = b.weight2, boneIndex3 = map[b.boneIndex3], weight3 = b.weight3 });
                }
            }
            if (outT.Count == 0) return null;
            var m = new Mesh { name = "Gear " + key }; m.SetVertices(outV); m.SetTriangles(outT, 0); m.RecalculateNormals(); m.RecalculateBounds();
            m.boneWeights = outW.ToArray(); m.bindposes = c.bindInverse;
            built[key] = m; return m;
        }
        static Vector3Int Snap(Vector3 p, float scale) { float q = Mathf.Max(1e-6f, Mathf.Abs(scale) * .02f); return new Vector3Int(Mathf.RoundToInt(p.x / q), Mathf.RoundToInt(p.y / q), Mathf.RoundToInt(p.z / q)); }

        /// <summary>A small rigid part (a stud, a spike, a gem) placed in root space and held by one bone.</summary>
        public static Mesh Rigid(Cloud c, Mesh src, Matrix4x4 place, int bone) { return Rigid(c, src, place, On(bone), "b" + bone); }
        public static Mesh Rigid(Cloud c, Mesh src, Matrix4x4 place, System.Func<Vector3, BoneWeight> weigh, string weighKey)
        {
            var sb = new System.Text.StringBuilder("rigid.").Append(src.GetHashCode()).Append('.').Append(weighKey).Append('.').Append(c.GetHashCode()).Append('.');
            for (int i = 0; i < 12; i++) sb.Append(place[i % 3, i / 3].ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            string key = sb.ToString(); if (Made(key)) return built[key];
            // Every triangle its own corners: a horn or a spike shades in flat facets like the plates (2026-10-18: a smooth horn
            // made the bone pauldron measure round).
            var m = new Mesh { name = "Gear " + key }; var v = src.vertices; var t = src.triangles; var o = new Vector3[t.Length]; var tt = new int[t.Length];
            for (int i = 0; i < t.Length; i++) { o[i] = place.MultiplyPoint3x4(v[t[i]]); tt[i] = i; }
            m.vertices = o; m.triangles = tt; m.RecalculateNormals(); m.RecalculateBounds();
            built[key] = m; return Skin(c, m, weigh);
        }

        /// <summary>Built pieces of one material on one limb, as one mesh (they share the cloud's bones and bind poses).</summary>
        public static Mesh Concat(string key, IList<Mesh> parts)
        {
            if (built.TryGetValue(key, out var done) && done != null) return done;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var w = new List<BoneWeight>(); var t = new List<int>(); Matrix4x4[] bind = null;
            foreach (var p in parts)
            {
                if (p == null) continue; int b = v.Count; v.AddRange(p.vertices); n.AddRange(p.normals); w.AddRange(p.boneWeights); bind = bind ?? p.bindposes;
                foreach (int i in p.triangles) t.Add(b + i);
            }
            var m = new Mesh { name = "Gear " + key }; if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.boneWeights = w.ToArray(); m.bindposes = bind ?? new Matrix4x4[0]; m.RecalculateBounds();
            built[key] = m; return m;
        }
    }
}
