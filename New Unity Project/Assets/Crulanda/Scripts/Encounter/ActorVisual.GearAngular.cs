using System.Collections.Generic;
using UnityEngine;
using M = Crulanda.Encounter.GearMeshes;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Helms, gloves, gauntlets, wraps, pauldrons and spaulders built on the model figure itself (play note 239, 2026-10-18,
    /// Chris: "helms and gauntlet still tooo round" / "in the 04-sets-front"; 2026-10-10: "we really need a complete overhaul on
    /// armor/weapons/helms, all seem to not fit properly"). On the old figure these were lathed domes and balls drawn round its
    /// ball head and mitten hands; on a model they were scaled over the head (SeatHead, HatFit) or warped onto the hand's box
    /// (ModelArmour), so a helm was a round dome and a glove a ball bigger than the fist.
    ///
    /// Here each is measured from the model: a helm is a stack of faceted rings round the head's own outline (the bind pose's
    /// head, or head and hair, per height: HeadOutline), each a few millimetres outside every point of the head between its
    /// neighbours, closed in a point just high enough that its gores clear the skull (AngularShell). It comes down over the skull
    /// to the brow, the ears or the jaw by the family, never perched; plates are flat, with ridges, a nasal bar, cheek guards, a
    /// brim of sloping plates; a mail coif opens round the face and drapes to the shoulders. A glove is the hand's own skin stood
    /// off it (ModelArmour.HandShell), a cuff, a wrap's band or a lame a ring of plates round the forearm or upper arm, a pauldron
    /// a faceted cap over the shoulder. Every family keeps its look's materials (the quality ladder, the zones' palettes) and
    /// its tier's extras; only the shapes and the fit are new. The smooth figure (models off) keeps the old drawings.
    /// GAME-ONLY designs.
    /// </summary>
    public sealed partial class ActorVisual
    {
        bool AngularHead { get { return model != null && headOutline != null && headFrame != null; } }
        bool AngularArms { get { return model != null && model.Cloud != null && model.Arms is SkinnedMeshRenderer; } }
        static readonly bool[] Sides = { false, true };
        static readonly Matrix4x4 I = Matrix4x4.identity;

        // ------------------------------------------------------------------------------------------------ the head
        /// <summary>A head measured for a helm: the outline its walls clear (the head, or head and hair), metres per head-frame
        /// unit along x, y and z, the eye line, the skull's height over the eyes, the outline's top and axis.</summary>
        sealed class Helm
        {
            public Vector3 s; public float eyes, H, top, z0; public string key;
            /// <summary>The points the walls clear: metres about the axis, with their frame heights.</summary>
            public List<(Vector2 q, float y)> pts;
            /// <summary>Metres as head-frame height.</summary>
            public float Y(float metres) { return metres / s.y; }
            /// <summary>A point of a ring's plane (metres about the axis) at frame height y, in the head frame.</summary>
            public Vector3 At(Vector2 q, float y) { return new Vector3(q.x / s.x, y, z0 + q.y / s.z); }
        }
        /// <summary>
        /// A head measured for a helm, from its own vertices in the bind pose (2026-10-18, second run: the outline's rows, 8 mm apart
        /// and each the hull of three, stood the crown out 3-4 cm). <paramref name="hairBelow"/>: the hair's points under that
        /// frame height count too (a cap or kettle hat: the hair above it is cut away, <see cref="CutHair"/>, and what hangs under the
        /// rim stays); NaN for none (a barbute or a coif hides the hair).
        /// </summary>
        Helm HelmOn(float hairBelow)
        {
            var sc = headFrame.lossyScale; bool hair = !float.IsNaN(hairBelow);
            var h = new Helm { s = new Vector3(Mathf.Abs(sc.x), Mathf.Abs(sc.y), Mathf.Abs(sc.z)), eyes = headOutline.eyes, z0 = headOutline.z0, pts = new List<(Vector2, float)>() };
            h.H = HeadH; float top = float.MinValue;
            void Take(Vector3 p) { h.pts.Add((new Vector2(p.x * h.s.x, (p.z - h.z0) * h.s.z), p.y)); top = Mathf.Max(top, p.y); }
            if (helmHead != null && helmHead.Count > 0) foreach (var p in helmHead) Take(p);
            else { h.pts.AddRange(OutlinePoints(headOutline, h)); top = headOutline.top; }
            if (hair && helmHair != null) foreach (var (_, at) in helmHair) foreach (var p in at) if (p.y <= hairBelow) Take(p);
            h.top = top == float.MinValue ? headOutline.top : top;
            h.key = OutlineKey(headOutline) + "." + (hair ? K(hairBelow) + "." + helmHairKey : "bare") + "." + K(h.s.x, h.s.y, h.s.z);
            return h;
        }
        /// <summary>The skull's height over the eyes (head-frame units).</summary>
        float HeadH { get { return Mathf.Max(.05f, headOutline.top - headOutline.eyes); } }
        /// <summary>The head's and the hair's vertices in the head frame, taken with the outlines in the bind pose (ActorVisual.Model,
        /// Frames); the hair per renderer, every vertex, for cutting it under a cap.</summary>
        List<Vector3> helmHead; List<(SkinnedMeshRenderer smr, Vector3[] at)> helmHair; string helmHairKey = "";
        readonly Dictionary<SkinnedMeshRenderer, Mesh> wholeHair = new Dictionary<SkinnedMeshRenderer, Mesh>();
        static readonly Dictionary<(Mesh, int), Mesh> cutHairs = new Dictionary<(Mesh, int), Mesh>();
        void MeasureHelm()
        {
            if (model == null || headFrame == null) return;
            var toFrame = headFrame.worldToLocalMatrix; helmHead = null; helmHair = new List<(SkinnedMeshRenderer, Vector3[])>(); var key = new System.Text.StringBuilder();
            if (model.Head is SkinnedMeshRenderer hs) helmHead = BindPoints(hs, toFrame);
            foreach (var r in model.Hair)
            {
                if (!(r is SkinnedMeshRenderer smr) || !r.name.Contains("Hair_") || r.name.Contains("Beard")) continue;
                var mesh = smr.sharedMesh; if (mesh == null || !mesh.isReadable) continue;
                var v = mesh.vertices; var w = mesh.boneWeights; var bp = mesh.bindposes; var bones = smr.bones; if (w.Length != v.Length || bp.Length != bones.Length) continue;
                var skin = new Matrix4x4[bones.Length]; for (int i = 0; i < bones.Length; i++) skin[i] = toFrame * (bones[i] != null ? bones[i].localToWorldMatrix : Matrix4x4.identity) * bp[i];
                var at = new Vector3[v.Length];
                for (int i = 0; i < v.Length; i++) { var b = w[i]; at[i] = skin[b.boneIndex0].MultiplyPoint3x4(v[i]) * b.weight0 + skin[b.boneIndex1].MultiplyPoint3x4(v[i]) * b.weight1 + skin[b.boneIndex2].MultiplyPoint3x4(v[i]) * b.weight2 + skin[b.boneIndex3].MultiplyPoint3x4(v[i]) * b.weight3; }
                helmHair.Add((smr, at)); key.Append(mesh.name).Append(';');
            }
            helmHairKey = key.ToString();
        }
        /// <summary>The hair cut at a frame height under a cap or kettle hat: only its triangles wholly under it are drawn (the rest
        /// would stand through the crown). Given back whole when the head piece comes off (<see cref="WholeHair"/>).</summary>
        void CutHair(float below)
        {
            if (helmHair == null) return;
            foreach (var (smr, at) in helmHair)
            {
                if (smr == null) continue;
                if (!wholeHair.TryGetValue(smr, out var whole)) wholeHair[smr] = whole = smr.sharedMesh;
                var ck = (whole, Mathf.RoundToInt(below * 2000));
                if (!cutHairs.TryGetValue(ck, out var cut) || cut == null)
                {
                    cut = Instantiate(whole); cut.name = whole.name + " (under a helm)";
                    for (int s = 0; s < whole.subMeshCount; s++)
                    {
                        var t = whole.GetTriangles(s); var keep = new List<int>();
                        for (int i = 0; i + 2 < t.Length; i += 3) if (at[t[i]].y <= below && at[t[i + 1]].y <= below && at[t[i + 2]].y <= below) { keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]); }
                        cut.SetTriangles(keep, s);
                    }
                    cutHairs[ck] = cut;
                }
                smr.sharedMesh = cut;
            }
        }
        void WholeHair() { foreach (var kv in wholeHair) if (kv.Key != null) kv.Key.sharedMesh = kv.Value; wholeHair.Clear(); }
        /// <summary>The outline's sampled points (a figure whose head mesh could not be read).</summary>
        static List<(Vector2, float)> OutlinePoints(HeadOutline o, Helm h)
        {
            var list = new List<(Vector2, float)>();
            for (int iy = 0; iy < o.ny; iy++) for (int ia = 0; ia < HeadAngles; ia++)
            {
                float r = o.r[iy, ia]; if (r <= 0) continue; float a = 2 * Mathf.PI * ia / HeadAngles;
                list.Add((new Vector2(Mathf.Sin(a) * r * h.s.x, Mathf.Cos(a) * r * h.s.z), o.y0 + iy * HeadStep));
            }
            return list;
        }
        /// <summary>A short hash of an outline (built meshes are shared by heads of one shape).</summary>
        static string OutlineKey(HeadOutline o)
        {
            if (o.key != null) return o.key;
            ulong hash = 14695981039346656037UL;
            void Mix(float x) { hash ^= (uint)Mathf.RoundToInt(x * 2000); hash *= 1099511628211UL; }
            Mix(o.y0); Mix(o.z0); Mix(o.eyes); Mix(o.top); foreach (var r in o.r) Mix(r);
            return o.key = hash.ToString("x16");
        }
        /// <summary>The head's points between two heights (metres about the axis, with their frame heights).</summary>
        static List<(Vector2 q, float y)> HelmPoints(Helm h, float y0, float y1)
        {
            var list = new List<(Vector2, float)>(); foreach (var p in h.pts) if (p.y >= y0 && p.y <= y1) list.Add(p);
            return list;
        }
        static float[] HelmSupport(Helm h, float y0, float y1, int n, float gap) { return AngularShell.Support(HelmPoints(h, y0, y1).ConvertAll(p => p.q), n, gap); }
        static float[] Plus(float[] a, float by) { var r = new float[a.Length]; for (int k = 0; k < a.Length; k++) r[k] = a[k] + by; return r; }
        static float[] Wider(float[] a, float[] b, float by) { var r = new float[a.Length]; for (int k = 0; k < a.Length; k++) r[k] = Mathf.Max(a[k], b[k]) + by; return r; }
        /// <summary>
        /// A helm's shell: rings at frame heights <paramref name="ys"/> (ascending), each <paramref name="n"/> flat facets
        /// (facet 0 to the front) <paramref name="gap"/> plus <paramref name="extra"/>[i] metres outside the head between its
        /// neighbours' heights, or as <paramref name="given"/>; closed with <paramref name="apex"/> in a point
        /// <paramref name="lift"/> metres over what it must clear (and no lower than <paramref name="minApex"/>). Cached per head shape.
        /// </summary>
        Mesh HelmShell(Helm h, string key, float[] ys, float[] extra, int n, float gap, float thick, System.Func<int, int, bool> open, bool apex, float lift,
            out float[][] sup, out Vector3[][] rings, out Vector3 apexAt, float[][] given = null, float minApex = float.MinValue)
        {
            int R0 = ys.Length; var gaps = new float[R0]; for (int i = 0; i < R0; i++) gaps[i] = gap + (extra != null ? extra[i] : 0);
            var ysA = ys; var givenA = given;
            if (apex)
            {
                // The crown (2026-10-18, first run: a cone's point over the last ring stood half a metre up on a coif): one more ring
                // the gap over the top of the head (or hair), starting round the top's own points, then a low point just over it.
                float yTop = Mathf.Max(h.top + h.Y(gap), minApex - h.Y(lift));
                ysA = new float[R0 + 1]; System.Array.Copy(ys, ysA, R0); ysA[R0] = yTop;
                var g2 = new float[R0 + 1]; System.Array.Copy(gaps, g2, R0); g2[R0] = gaps[R0 - 1]; gaps = g2;
                givenA = new float[R0 + 1][]; if (given != null) System.Array.Copy(given, givenA, R0);
                givenA[R0] = HelmSupport(h, Mathf.Lerp(ys[R0 - 1], h.top, .5f), h.top, n, gaps[R0]);
            }
            int R = ysA.Length; rings = new Vector3[R][]; var centres = new Vector3[R];
            sup = AngularShell.Fit(ysA, HelmPoints(h, ysA[0], ysA[R - 1]), n, gaps, false, givenA);
            for (int i = 0; i < R; i++) { centres[i] = new Vector3(0, ysA[i], h.z0); var c = AngularShell.Corners(sup[i]); rings[i] = new Vector3[n]; for (int k = 0; k < n; k++) rings[i][k] = h.At(c[k], ysA[i]); }
            apexAt = Vector3.zero; Vector3? top = null;
            if (apex) { apexAt = new Vector3(0, ysA[R - 1] + h.Y(lift), h.z0); top = apexAt; }
            var rr = rings; var cc = centres; var tp = top; float th = thick / h.s.x;
            return M.Cached("h." + key + "." + h.key + "." + K(ys) + "|" + (minApex > float.MinValue ? K(minApex) : ""), () => AngularShell.Build(rr, cc, th, open, tp));
        }
        /// <summary>The middle of facet k of a ring (supports <paramref name="sup"/>) at frame height y, <paramref name="outBy"/> metres outside it.</summary>
        static Vector3 OnFacet(Helm h, float[] sup, int k, float y, float outBy) { var d = AngularShell.Normal(k, sup.Length); return h.At(d * (sup[k] + outBy), y); }
        static float FacetYaw(int k, int n) { return 360f * k / n; }
        /// <summary>A box at a point of the head frame, turned to face yaw, its size in metres.</summary>
        static Matrix4x4 HelmBox(Helm h, Vector3 at, float yaw, Vector3 metres, float roll = 0)
        {
            return Matrix4x4.TRS(at, Quaternion.Euler(0, yaw, 0), new Vector3(metres.x / h.s.x, metres.y / h.s.y, metres.z / h.s.z)) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, roll));
        }
        /// <summary>A flat bar from a to b (head frame) standing <paramref name="tall"/> metres out along <paramref name="up"/>: a comb or a ridge.</summary>
        static Matrix4x4 HelmBar(Helm h, Vector3 a, Vector3 b, Vector3 up, float wide, float tall)
        {
            var d = b - a; var rot = Quaternion.LookRotation(d, up);
            return Matrix4x4.TRS((a + b) / 2 + up.normalized * h.Y(tall) * .5f, rot, new Vector3(wide / h.s.x, h.Y(tall), d.magnitude));
        }
        /// <summary>
        /// A comb or a seam up facet k's middle, from ring <paramref name="from"/> over every ring above to the point: bar by bar on
        /// the plates (2026-10-18, third run: one straight bar from the lower ring to the point cut inside the crown, so a kettle's
        /// comb stood 3 cm into the head).
        /// </summary>
        static void Ridge(Fit f, string role, Helm h, float[][] sup, Vector3[][] rings, Vector3 apex, int k, int from, float wide, float tall)
        {
            int n = sup[0].Length; var pts = new List<Vector3>();
            for (int i = from; i < sup.Length; i++) pts.Add(OnFacet(h, sup[i], k, rings[i][0].y, .001f));
            pts.Add(apex);
            for (int i = 0; i + 1 < pts.Count; i++) f.Add(OnBody, role, Cube, HelmBar(h, pts[i], pts[i + 1], FacetUp(k, n), wide, tall));
        }
        static Vector3 FacetUp(int k, int n, float lean = .6f) { var d = AngularShell.Normal(k, n); return new Vector3(d.x, lean, d.y); }
        /// <summary>A small diamond (a gem, a stud) on a helm.</summary>
        static Matrix4x4 Gem(Helm h, Vector3 at, float yaw, float metres) { return HelmBox(h, at, yaw, new Vector3(metres, metres, metres * .45f), 45); }

        /// <summary>
        /// A leather (or cloth) skull-cap: eight gores over the head and hair from just above the brows to a low point, a band round
        /// the brim. Flaps: plates hanging over the ears. Leaf: leaves stitched round the band and a sprig on top.
        /// </summary>
        void AngularCap(Fit f)
        {
            f.onHead = true; const int n = 8;
            float rim = headOutline.eyes + .22f * HeadH;   // just over the brows (HatAboveEyes left it 6.5 cm under the top: perched)
            float cut = rim + .008f / Mathf.Abs(headFrame.lossyScale.y); CutHair(cut); var h = HelmOn(cut); float top = h.top;
            string main = f.v == "leaf" || f.tier == 0 ? "cloth" : "leather", band = f.tier >= 1 ? "trim" : "cloth2";
            var ys = new[] { rim, Mathf.Lerp(rim, top, .4f), Mathf.Lerp(rim, top, .7f), Mathf.Lerp(rim, top, .88f) };
            f.Add(OnBody, main, HelmShell(h, "cap", ys, null, n, .004f, .003f, null, true, .002f, out var sup, out var rings, out var apex), I);
            var bs = Wider(sup[0], sup[1], .0045f);
            f.Add(OnBody, band, HelmShell(h, "cap.band", new[] { rim - h.Y(.002f), rim + h.Y(.012f) }, null, n, 0, .003f, null, false, 0, out _, out _, out _, new[] { bs, bs }), I);
            if (f.v == "flaps") f.Add(OnBody, main, HelmShell(h, "cap.flaps", new[] { h.eyes - .45f * h.H, rim + h.Y(.01f) }, null, n, .006f, .004f, (s, k) => k != 2 && k != 6, false, 0, out _, out _, out _), I);
            if (f.v == "leaf")
            {
                var leaves = new List<Matrix4x4>();
                for (int k = 0; k < n; k++) if (k != n / 2) leaves.Add(Fit.At(OnFacet(h, bs, k, rim + h.Y(.004f), .002f), V(-25, FacetYaw(k, n), 0), One));
                f.Add(OnBody, "green", M.Many("a.cap.leaves." + h.key, Leaf, leaves.ToArray()), I);
                f.Add(OnBody, "green", M.Many("a.cap.sprig", Leaf, M.At(V0, V(0, 0, 0), One), M.At(V0, V(0, 0, 35), V(.8f, .8f, .8f)), M.At(V0, V(0, 0, -35), V(.8f, .8f, .8f))), Fit.At(apex, V(-15, 0, 0), One));
            }
            if (f.tier >= 1 || f.l.extra) f.Add(OnBody, band, Cube, HelmBox(h, apex, 45, new Vector3(.012f, .007f, .012f)));   // the button on top
            if (f.l.extra) Ridge(f, "trim", h, sup, rings, apex, 0, 0, .005f, .003f);   // a raised seam up the front gore
            f.Glow(1, OnBody, Cube, Gem(h, OnFacet(h, bs, 0, rim + h.Y(.005f), .002f), 0, .012f));
            f.Glow(2, OnBody, Cube, Gem(h, apex + V(0, h.Y(.004f), 0), 0, .012f));
            f.Rust(OnBody, OnFacet(h, sup[1], 1, ys[1], .001f), new Vector3(.014f / h.s.x, .01f / h.s.y, .003f / h.s.z), V(0, FacetYaw(1, n), 0));
            Mark(f, OnBody, OnFacet(h, bs, 1, rim + h.Y(.006f), .002f), V(0, FacetYaw(1, n), 0));
        }

        /// <summary>
        /// A kettle hat: eight steel (or boiled leather) gores rising to a point over the hair, a rim band, and a brim of sloping
        /// plates. Half: no brim, a lower rim and a nasal bar down to the nose. Plate cheek guards from the third band, rivets from
        /// the third, a comb from the fourth (taller at the fifth).
        /// </summary>
        void AngularKettle(Fit f)
        {
            f.onHead = true; const int n = 8; bool half = f.v == "half";
            string main = f.tier >= 1 ? "plate" : "leather2", hk = half ? "kettle.half" : "kettle";
            float rim = headOutline.eyes + (half ? .17f : .22f) * HeadH;
            float cut = rim + .008f / Mathf.Abs(headFrame.lossyScale.y); CutHair(cut); var h = HelmOn(cut); float top = h.top;
            var ys = new[] { rim, Mathf.Lerp(rim, top, .4f), Mathf.Lerp(rim, top, .7f), Mathf.Lerp(rim, top, .88f) };
            f.Add(OnBody, main, HelmShell(h, hk, ys, null, n, .006f, .004f, null, true, half ? .005f : .01f, out var sup, out var rings, out var apex), I);
            var rs = Wider(sup[0], sup[1], .005f);
            f.Add(OnBody, "trim", HelmShell(h, hk + ".rim", new[] { rim - h.Y(.002f), rim + h.Y(.011f) }, null, n, 0, .003f, null, false, 0, out _, out _, out _, new[] { rs, rs }), I);
            if (!half)
            {
                // The brim: plates sloping out and down from the rim, its edge rolled at the better bands.
                var outer = Plus(rs, .04f);
                f.Add(OnBody, main, HelmShell(h, "kettle.brim", new[] { rim - h.Y(.017f), rim + h.Y(.001f) }, null, n, 0, .004f, null, false, 0, out _, out _, out _, new[] { outer, rs }), I);
                var edge = Plus(outer, .002f);
                f.Fancy(OnBody, HelmShell(h, "kettle.brim.edge", new[] { rim - h.Y(.021f), rim - h.Y(.014f) }, null, n, 0, .003f, null, false, 0, out _, out _, out _, new[] { edge, edge }), I);
                if (f.tier >= 2 && main == "plate")   // cheek guards under the brim
                    f.Add(OnBody, main, HelmShell(h, "kettle.cheeks", new[] { h.eyes - .5f * h.H, rim + h.Y(.004f) }, null, n, .008f, .004f, (s, k) => k != 2 && k != 6, false, 0, out _, out _, out _), I);
            }
            else
            {
                float nose = h.eyes - .35f * h.H; float front = HelmSupport(h, nose, rim, n, .006f)[0];
                f.Add(OnBody, main, Cube, HelmBox(h, h.At(new Vector2(0, front + .003f), (rim + nose) / 2), 0, new Vector3(.016f, (rim - nose) * h.s.y, .006f)));   // the nasal
                var band2 = Wider(sup[0], sup[1], .002f); float y2 = Mathf.Lerp(rim, top, .3f);
                f.Fancy(OnBody, HelmShell(h, "kettle.half.band", new[] { y2, y2 + h.Y(.007f) }, null, n, 0, .002f, null, false, 0, out _, out _, out _, new[] { band2, band2 }), I);
            }
            if (f.tier >= 2)
            {
                var rivets = new Matrix4x4[n]; for (int k = 0; k < n; k++) rivets[k] = HelmBox(h, OnFacet(h, rs, k, rim + h.Y(.0045f), .0015f), FacetYaw(k, n), new Vector3(.006f, .006f, .003f));
                f.Add(OnBody, "dark", M.Many("a.kettle.rivets." + hk + "." + h.key, Cube, rivets), I);
            }
            if (f.tier >= 3)
            {
                float tall = f.tier >= 4 ? .024f : .014f;
                Ridge(f, "trim", h, sup, rings, apex, 0, 1, .005f, tall); Ridge(f, "trim", h, sup, rings, apex, n / 2, 1, .005f, tall);
            }
            f.Glow(1, OnBody, Cube, Gem(h, OnFacet(h, rs, 0, rim + h.Y(.0045f), .002f), 0, .012f));
            f.Glow(2, OnBody, Cube, Gem(h, apex + V(0, h.Y(.005f), 0), 0, .013f));
            f.Rust(OnBody, OnFacet(h, sup[1], 1, ys[1], .001f), new Vector3(.014f / h.s.x, .012f / h.s.y, .003f / h.s.z), V(0, FacetYaw(1, n), 0));
            Mark(f, OnBody, OnFacet(h, rs, 1, rim + h.Y(.005f), .002f), V(0, FacetYaw(1, n), 0));
        }

        /// <summary>
        /// A barbute: twelve steel plates closed round the head from the jaw to a low point, a T opening (an eye slit and a slot to
        /// the chin), a collar of plates (mail below the fifth band). Epic: the slit's edges glow. Fifth band (or extra): a brow
        /// band; fifth: a comb and cheek rivets. Rimed: frost on the rims, icicles at the jaw.
        /// </summary>
        void AngularBarbute(Fit f)
        {
            f.onHead = true; var h = HelmOn(float.NaN); const int n = 12; float e = h.eyes, H = h.H;
            var ys = new[] { e - .85f * H, e - .5f * H, e - .15f * H, e + .15f * H, e + .45f * H, e + .75f * H, e + .9f * H };
            bool Open(int s, int k) { return s <= 1 && k == 0 || s == 2 && (k == 11 || k == 0 || k == 1); }
            f.Add(OnBody, "plate", HelmShell(h, "barbute", ys, null, n, .007f, .004f, Open, true, .004f, out var sup, out var rings, out var apex), I);
            var g0 = sup[0];
            f.Add(OnBody, f.tier >= 4 ? "plate" : "mail", HelmShell(h, "barbute.gorget", new[] { e - 1.35f * H, e - 1.1f * H, ys[0] + h.Y(.004f) }, null, n, 0, .004f, null, false, 0, out _, out _, out _,
                new[] { Plus(g0, .03f), Plus(g0, .012f), Plus(g0, .003f) }), I);
            if (f.l.accents >= 2)
                foreach (int k in new[] { 11, 0, 1 })
                {
                    float w = 2 * sup[3][k] * Mathf.Tan(Mathf.PI / n);
                    f.Add(OnBody, "glow", Cube, HelmBox(h, OnFacet(h, sup[3], k, ys[3] + h.Y(.002f), .0005f), FacetYaw(k, n), new Vector3(w, .004f, .004f)));
                    f.Add(OnBody, "glow", Cube, HelmBox(h, OnFacet(h, sup[2], k, ys[2] - h.Y(.002f), .0005f), FacetYaw(k, n), new Vector3(w, .004f, .004f)));
                }
            if (f.tier >= 4 || f.l.extra)
            {
                var bb = Wider(sup[3], sup[4], .004f);
                f.Add(OnBody, "trim", HelmShell(h, "barbute.brow", new[] { ys[3] + h.Y(.004f), ys[3] + h.Y(.016f) }, null, n, 0, .003f, null, false, 0, out _, out _, out _, new[] { bb, bb }), I);
            }
            if (f.tier >= 4)
            {
                Ridge(f, "trim", h, sup, rings, apex, 0, 4, .005f, .02f); Ridge(f, "trim", h, sup, rings, apex, n / 2, 2, .005f, .02f);
                var rivets = new List<Matrix4x4>(); foreach (int k in new[] { 2, 3, 9, 10 }) rivets.Add(HelmBox(h, OnFacet(h, sup[1], k, ys[1], .0015f), FacetYaw(k, n), new Vector3(.006f, .006f, .003f)));
                f.Add(OnBody, "dark", M.Many("a.barbute.rivets." + h.key, Cube, rivets.ToArray()), I);
            }
            if (f.v == "rimed")
            {
                var frost = new List<Matrix4x4>(); for (int k = 0; k < n; k += 2) { frost.Add(HelmBox(h, OnFacet(h, g0, k, ys[0] + h.Y(.006f), .002f), FacetYaw(k, n), new Vector3(.02f, .007f, .006f))); frost.Add(HelmBox(h, OnFacet(h, sup[4], k + 1, ys[4], .002f), FacetYaw(k + 1, n), new Vector3(.02f, .006f, .006f))); }
                f.Add(OnBody, "rime", M.Many("a.barbute.rime." + h.key, Cube, frost.ToArray()), I);
                var icicles = new List<Matrix4x4>(); for (int k = 4; k <= 8; k++) icicles.Add(Matrix4x4.TRS(OnFacet(h, g0, k, ys[0] - h.Y(.002f), .0f), Quaternion.Euler(180, 0, 0), new Vector3(.007f / h.s.x, h.Y(.012f + (k % 3) * .005f), .007f / h.s.z)));
                f.Add(OnBody, "rime", M.Many("a.barbute.icicles." + h.key, M.Cone(6), icicles.ToArray()), I);
            }
            f.Glow(1, OnBody, Cube, Gem(h, OnFacet(h, sup[4], 0, ys[4], .002f), 0, .013f));
            f.Rust(OnBody, OnFacet(h, sup[4], 10, ys[4], .001f), new Vector3(.014f / h.s.x, .012f / h.s.y, .003f / h.s.z), V(0, FacetYaw(10, n), 0));
            Mark(f, OnBody, OnFacet(h, sup[1], 3, ys[1], .002f), V(0, FacetYaw(3, n), 0));
        }

        /// <summary>
        /// A mail coif: twelve panels of mail in rows (every other ring stands a little proud) round the head, open round the face
        /// from the chin to the brow, draping to the shoulders, a collar at the hem. Third band: a brow band; fourth: a steel
        /// skull-plate; fifth: a nasal. Extra: studs round the collar.
        /// </summary>
        void AngularCoif(Fit f)
        {
            f.onHead = true; var h = HelmOn(float.NaN); const int n = 12; float e = h.eyes, H = h.H, chin = e - .9f * H;
            var ys = new[] { chin - .62f * H, chin - .32f * H, chin, e - .55f * H, e - .2f * H, e + .2f * H, e + .5f * H, e + .75f * H, e + .9f * H };
            var chinSup = HelmSupport(h, chin, e - .55f * H, n, .005f);
            var given = new float[ys.Length][]; given[0] = Plus(chinSup, .05f); given[1] = Plus(chinSup, .02f);
            var extra = new float[ys.Length]; for (int i = 2; i < ys.Length; i++) extra[i] = i % 2 == 0 ? .0015f : 0;
            bool Open(int s, int k) { return s >= 2 && s <= 4 && (k == 11 || k == 0 || k == 1); }
            f.Add(OnBody, "mail", HelmShell(h, "coif", ys, extra, n, .005f, .004f, Open, true, .002f, out var sup, out _, out var apex, given), I);
            var collar = Plus(given[0], .004f);
            f.Add(OnBody, f.tier >= 1 ? "leather" : "cloth2", HelmShell(h, "coif.collar", new[] { ys[0] - h.Y(.003f), ys[0] + h.Y(.012f) }, null, n, 0, .004f, null, false, 0, out _, out _, out _, new[] { collar, collar }), I);
            if (f.tier >= 2)
            {
                var bb = Wider(sup[5], sup[6], .004f);
                f.Add(OnBody, "trim", HelmShell(h, "coif.brow", new[] { ys[5], ys[5] + h.Y(.012f) }, null, n, 0, .003f, null, false, 0, out _, out _, out _, new[] { bb, bb }), I);
            }
            if (f.tier >= 3)
            {
                float y0 = e + .45f * H, y1 = e + .62f * H, y2 = e + .82f * H;
                f.Add(OnBody, "plate", HelmShell(h, "coif.plate", new[] { y0, y1, y2 }, null, n, .0135f, .004f, null, true, .004f, out _, out _, out _,
                    new[] { Wider(sup[5], sup[6], .006f), Wider(sup[6], sup[7], .006f), Wider(sup[7], sup[8], .006f) }, apex.y + h.Y(.006f)), I);
            }
            if (f.tier >= 4)
            {
                float nose = e - .35f * H, brow = e + .25f * H, front = HelmSupport(h, nose, brow, n, .009f)[0];
                f.Add(OnBody, "plate", Cube, HelmBox(h, h.At(new Vector2(0, front + .003f), (brow + nose) / 2), 0, new Vector3(.016f, (brow - nose) * h.s.y, .006f)));
            }
            if (f.l.extra)
            {
                var studs = new List<Matrix4x4>(); for (int k = 1; k < n; k += 2) studs.Add(HelmBox(h, OnFacet(h, collar, k, ys[0] + h.Y(.0045f), .002f), FacetYaw(k, n), new Vector3(.006f, .006f, .003f), 45));
                f.Add(OnBody, "trim", M.Many("a.coif.studs." + h.key, Cube, studs.ToArray()), I);
            }
            f.Glow(1, OnBody, Cube, Gem(h, OnFacet(h, collar, 0, ys[0] + h.Y(.0045f), .002f), 0, .013f));
            f.Glow(2, OnBody, Cube, Gem(h, f.tier >= 2 ? OnFacet(h, Wider(sup[5], sup[6], .004f), 0, ys[5] + h.Y(.006f), .002f) : apex + V(0, h.Y(.004f), 0), 0, .013f));
            f.Rust(OnBody, OnFacet(h, sup[4], 10, ys[4], .001f), new Vector3(.014f / h.s.x, .014f / h.s.y, .003f / h.s.z), V(0, FacetYaw(10, n), 0));
            Mark(f, OnBody, OnFacet(h, collar, 2, ys[0] + h.Y(.005f), .002f), V(0, FacetYaw(2, n), 0));
        }

        // ------------------------------------------------------------------------------------------------ the arms
        float PerMetre { get { return 1f / Mathf.Max(1e-5f, Mathf.Abs(model.Model.lossyScale.y)); } }
        string ArmKey { get { var smr = model.Arms as SkinnedMeshRenderer; return (smr != null && smr.sharedMesh != null ? smr.sharedMesh.GetHashCode().ToString() : "x") + "." + K(PerMetre); } }
        /// <summary>A ring of plates round the forearm (0 the elbow, 1 the wrist; past 1 round the hand, then held by the hand).</summary>
        Mesh ForeRing(ModelArmour.ArmMeasure a, bool left, float t0, float t1, int rings, int n, float gap, float flare0, float flare1, float thick, float tLo, float tHi, bool whole = false)
        {
            float pm = PerMetre; var c = model.Cloud; bool palm = t0 >= 1;   // round the palm: held by the hand, the thumb left out (it stands off in the bind pose)
            var weigh = palm ? ModelArmour.On(a.handBone) : ModelArmour.On(a.foreBone);
            return ModelArmour.ArmRing(c, ArmKey + ".fore", left, a.elbow, a.wrist, t0, t1, rings, n, gap * pm, flare0 * pm, flare1 * pm, thick * pm, tLo, tHi, a.back, weigh, null, whole, palm);
        }
        /// <summary>A small part held by one bone: a box (or <paramref name="mesh"/>) at a root-space point, its z out along
        /// <paramref name="outward"/> and its y along <paramref name="along"/>, its size in metres.</summary>
        void Stud(Fit f, int at, string role, Mesh mesh, Vector3 pos, Vector3 outward, Vector3 along, Vector3 metres, int bone, float roll = 0)
        {
            float pm = PerMetre; var rot = Quaternion.LookRotation(outward, along) * Quaternion.Euler(0, 0, roll);
            f.Built(at, role, ModelArmour.Rigid(model.Cloud, mesh, Matrix4x4.TRS(pos, rot, metres * pm), bone));
        }
        /// <summary>The look's small mark (Mark) on a built piece: at a root-space point facing <paramref name="outward"/>, held by a bone.</summary>
        void MarkOn(Fit f, int at, Vector3 pos, Vector3 outward, Vector3 along, int bone, float size, bool join)
        {
            if (join && !f.parts.Exists(p => p.at == at && p.role == MarkRole(f.l.detail))) return;
            var tmp = new Fit(f.k); Mark(tmp, OnBody, V0, V0, 1, false);
            var place = Matrix4x4.TRS(pos, Quaternion.LookRotation(outward, along), Vector3.one * size * PerMetre * .25f);
            foreach (var p in tmp.parts) f.Built(at, p.role, ModelArmour.Rigid(model.Cloud, p.mesh, place * p.m, bone));
        }
        /// <summary>A point on the forearm's surface at t, along <paramref name="dir"/>, <paramref name="outBy"/> metres outside the
        /// arm's own points (sleeve and skin).</summary>
        Vector3 ForeSurface(ModelArmour.ArmMeasure a, bool left, float t, Vector3 dir, float outBy)
        {
            dir = Vector3.ProjectOnPlane(dir, a.axis).normalized;
            float r = ModelArmour.Reach(model.Cloud, left, a.elbow, a.wrist, t - .06f, Mathf.Min(1, t + .06f), dir);
            return a.Along(t) + dir * (r + outBy * PerMetre);
        }
        /// <summary>A point on the back of the hand, a share of the way from the wrist to the knuckles, outBy metres off the skin.</summary>
        Vector3 HandBack(ModelArmour.ArmMeasure a, float share, float outBy)
        {
            var p = a.wrist + a.axis * a.hand * share; return p + a.back * (a.backReach - Vector3.Dot(p - a.wrist, a.back) + outBy * PerMetre);
        }

        /// <summary>
        /// Gloves: the hand's own skin in leather, a cuff of six plates flaring up the forearm. Fingerless: the palm and the thumb's
        /// root, the fingers bare. Mitts: thick (5 mm) over the whole hand, a fur cuff. A trim ring at the cuff's top from the
        /// second band, a stitched panel on the back of the hand, a wrist strap with a buckle from the third, knuckle studs from the fourth.
        /// </summary>
        void AngularGloves(Fit f)
        {
            var c = model.Cloud; var arms = (SkinnedMeshRenderer)model.Arms; float pm = PerMetre; string v = f.v, main = "leather"; bool mitts = v == "mitts";
            model.ShowExtras("Bracer", false);   // the outfit's bracers go under the cuff (given back with the hands: Bare)
            foreach (bool left in Sides)
            {
                var a = ModelArmour.Arm(c, left); if (!a.ok) continue; int at = left ? OnArmL : OnArmR; var side = Vector3.Cross(a.axis, a.back);
                f.Built(at, main, ModelArmour.HandShell(c, arms, left, (mitts ? .005f : .0018f) * pm, v == "fingerless" ? 1 : 0, a.back));
                float t0 = mitts ? .7f : .64f, gap = mitts ? .008f : .004f, flare = mitts ? .012f : .011f, thick = mitts ? .006f : .003f;
                f.Built(at, mitts ? "fur" : main, ForeRing(a, left, t0, 1.05f, 3, mitts ? 8 : 6, gap, flare, .002f, thick, t0 - .05f, 1));
                float over = gap + flare + thick + .0015f;
                if (f.tier >= 1 && !mitts) f.Built(at, "trim", ForeRing(a, left, t0 - .025f, t0 + .025f, 2, 6, over, 0, 0, .002f, t0 - .05f, 1, true));
                if (f.tier >= 1 && v == "plain") Stud(f, at, "trim", Cube, HandBack(a, .45f, .002f), a.back, a.axis, new Vector3(.014f, .04f, .003f), a.handBone);
                if (f.tier >= 2)
                {
                    f.Built(at, main, ForeRing(a, left, .9f, .95f, 2, 6, over, 0, 0, .002f, t0 - .05f, 1, true));
                    Stud(f, at, "trim", Cube, ForeSurface(a, left, .925f, a.back, over + .002f), a.back, a.axis, new Vector3(.012f, .01f, .003f), a.foreBone);
                }
                if (f.tier >= 3 && !mitts)
                {
                    string s = left ? "_l" : "_r";
                    foreach (var fn in new[] { "index_01", "middle_01", "ring_01" })
                    {
                        int b = ModelArmour.BoneIndex(c, fn + s); if (b < 0) continue; var kn = c.bind[b].GetColumn(3);
                        Stud(f, at, "trim", Cube, (Vector3)kn + a.back * (a.backReach - Vector3.Dot((Vector3)kn - a.wrist, a.back) + .003f * pm), a.back, a.axis, new Vector3(.006f, .006f, .004f), a.handBone, 45);
                    }
                }
                if (f.l.extra) Stud(f, at, "trim", Cube, ForeSurface(a, left, .78f, a.back, over), a.back, a.axis, new Vector3(.012f, .012f, .003f), a.foreBone, 45);
                if (f.l.accents >= 1) Stud(f, at, "glow", Cube, ForeSurface(a, left, .85f, side, over), side, a.axis, new Vector3(.01f, .01f, .004f), a.foreBone, 45);
                if (f.l.accents >= 2) Stud(f, at, "glow", Cube, HandBack(a, .5f, .003f), a.back, a.axis, new Vector3(.009f, .009f, .004f), a.handBone, 45);
                if (f.l.worn) Stud(f, at, "rust", Cube, HandBack(a, .3f, .0025f), a.back, a.axis, new Vector3(.014f, .012f, .002f), a.handBone);
                MarkOn(f, at, ForeSurface(a, left, .74f, -side, over), -side, a.axis, a.foreBone, .6f, true);
            }
        }

        /// <summary>Wraps: three bands of six plates wound up the forearm and one round the palm, the fingers bare. Fur: thick bands.
        /// Silk: shining bands, a trailing end. Third band: a band round the knuckles; fourth: studs; fifth: a trim ring.</summary>
        void AngularWraps(Fit f)
        {
            var c = model.Cloud; string v = f.v, main = v == "fur" ? "fur" : v == "silk" ? "silk" : "cloth";
            model.ShowExtras("Bracer", false);
            float gap = v == "fur" ? .007f : .003f, thick = v == "fur" ? .006f : .0025f, over = gap + thick + .0015f;
            foreach (bool left in Sides)
            {
                var a = ModelArmour.Arm(c, left); if (!a.ok) continue; int at = left ? OnArmL : OnArmR; var side = Vector3.Cross(a.axis, a.back);
                float hand = a.hand / Mathf.Max(1e-5f, a.fore);
                foreach (float t in new[] { .52f, .68f, .84f }) f.Built(at, main, ForeRing(a, left, t - .045f, t + .045f, 2, 6, gap, 0, 0, thick, .3f, 1));
                f.Built(at, main, ForeRing(a, left, 1 + hand * .28f, 1 + hand * .46f, 2, 6, gap, 0, 0, thick, 1 + hand * .15f, 1 + hand * .6f));   // round the palm
                if (f.tier >= 2) f.Built(at, main, ForeRing(a, left, 1 + hand * .62f, 1 + hand * .74f, 2, 6, gap, 0, 0, thick, 1 + hand * .55f, 1 + hand * .8f));
                if (v == "silk") Stud(f, at, main, Cube, ForeSurface(a, left, .9f, side, over) - a.axis * .02f * PerMetre, side, a.axis, new Vector3(.025f, .1f, .003f), a.foreBone);
                if (f.tier >= 3) { Stud(f, at, "trim", Cube, ForeSurface(a, left, .66f, side, over), side, a.axis, new Vector3(.007f, .007f, .004f), a.foreBone, 45); Stud(f, at, "trim", Cube, ForeSurface(a, left, .7f, side, over), side, a.axis, new Vector3(.007f, .007f, .004f), a.foreBone, 45); }
                if (f.tier >= 4) f.Built(at, "trim", ForeRing(a, left, .835f, .845f, 2, 6, over, 0, 0, .002f, .795f, .885f, true));
                if (f.l.extra) f.Built(at, "trim", ForeRing(a, left, .515f, .525f, 2, 6, over, 0, 0, .002f, .475f, .565f, true));
                if (f.l.accents >= 1) Stud(f, at, "glow", Cube, ForeSurface(a, left, .68f, a.back, over), a.back, a.axis, new Vector3(.009f, .009f, .004f), a.foreBone, 45);
                if (f.l.accents >= 2) Stud(f, at, "glow", Cube, HandBack(a, .37f, .004f), a.back, a.axis, new Vector3(.008f, .008f, .004f), a.handBone, 45);
                if (f.l.worn) Stud(f, at, "rust", Cube, ForeSurface(a, left, .84f, -a.back, over), -a.back, a.axis, new Vector3(.012f, .01f, .002f), a.foreBone);
                MarkOn(f, at, ForeSurface(a, left, .6f, side, over), side, a.axis, a.foreBone, .6f, true);
            }
        }

        /// <summary>
        /// Gauntlets: a leather glove of the hand's own skin, plates over the backs of the fingers (lames) and over the back of the
        /// hand, and a cuff of eight plates flaring toward the elbow (wider each band). Boiled leather in the first two bands, plate
        /// after; a trim ring at the cuff's top from the fourth band; knuckle spikes on plate in the last. Bone: claws at the
        /// fingertips. Glass: glass plates. Root: roots wound round the forearm, buds lit along them, over a plain glove.
        /// </summary>
        void AngularGauntlets(Fit f)
        {
            var c = model.Cloud; var arms = (SkinnedMeshRenderer)model.Arms; float pm = PerMetre; string v = f.v;
            string main = v == "bone" ? "bone" : v == "glass" ? "glass" : v == "root" ? "wood" : f.tier >= 2 ? "plate" : "leather2";
            model.ShowExtras("Bracer", false);
            foreach (bool left in Sides)
            {
                var a = ModelArmour.Arm(c, left); if (!a.ok) continue; int at = left ? OnArmL : OnArmR; var side = Vector3.Cross(a.axis, a.back); string s = left ? "_l" : "_r";
                f.Built(at, "leather", ModelArmour.HandShell(c, arms, left, .0015f * pm, 0, a.back));
                float flare = .012f + .004f * f.tier, over = .004f + flare + .0035f + .0015f;
                if (v != "root")
                {
                    f.Built(at, main, ModelArmour.HandShell(c, arms, left, .0032f * pm, 2, a.back, .2f));    // lames over the fingers
                    f.Built(at, main, ModelArmour.HandShell(c, arms, left, .0065f * pm, 1, a.back, .35f));   // the plate over the back of the hand
                    f.Built(at, main, ForeRing(a, left, .45f, 1.05f, 3, 8, .004f, flare, .002f, .0035f, .4f, 1));
                    if (f.tier >= 3) f.Built(at, "trim", ForeRing(a, left, .43f, .48f, 2, 8, over, 0, 0, .002f, .4f, 1, true));
                    if (f.tier >= 4 && v == "plate")
                        foreach (var fn in new[] { "index_01", "ring_01" })
                        {
                            int b = ModelArmour.BoneIndex(c, fn + s); if (b < 0) continue; Vector3 kn = c.bind[b].GetColumn(3);
                            var at3 = kn + a.back * (a.backReach - Vector3.Dot(kn - a.wrist, a.back) + .006f * pm);
                            f.Built(at, "edge", ModelArmour.Rigid(c, M.Cone(6), Matrix4x4.TRS(at3, Quaternion.FromToRotation(Vector3.up, a.back), new Vector3(.009f, .016f, .009f) * pm), a.handBone));   // whetted steel
                        }
                    if (f.l.extra) f.Built(at, "trim", ForeRing(a, left, .955f, .985f, 2, 8, over, 0, 0, .002f, .4f, 1, true));
                }
                if (v == "bone")
                    foreach (var fn in new[] { "index", "middle", "ring" })
                    {
                        int b3 = ModelArmour.BoneIndex(c, fn + "_03" + s), b2 = ModelArmour.BoneIndex(c, fn + "_02" + s); if (b3 < 0 || b2 < 0) continue;
                        Vector3 p3 = c.bind[b3].GetColumn(3), p2 = c.bind[b2].GetColumn(3); var dir = (p3 - p2).normalized;
                        f.Built(at, "bone", ModelArmour.Rigid(c, M.Cone(6), Matrix4x4.TRS(p3 + dir * .012f * pm, Quaternion.FromToRotation(Vector3.up, dir), new Vector3(.007f, .02f, .007f) * pm), b3));
                    }
                if (f.l.accents >= 1) Stud(f, at, "glow", Cube, ForeSurface(a, left, .7f, side, over), side, a.axis, new Vector3(.011f, .011f, .004f), a.foreBone, 45);
                if (f.l.accents >= 2) Stud(f, at, "glow", Cube, HandBack(a, .45f, .009f), a.back, a.axis, new Vector3(.01f, .01f, .004f), a.handBone, 45);
                if (f.l.worn) Stud(f, at, "rust", Cube, ForeSurface(a, left, .62f, a.back, over), a.back, a.axis, new Vector3(.014f, .02f, .002f), a.foreBone);
                MarkOn(f, at, ForeSurface(a, left, .66f, -side, over), -side, a.axis, a.foreBone, .6f, true);
            }
            if (v == "root")   // roots wound round the forearm, buds lit along them (the old figure's drawing, fitted to the arm)
            {
                var helix = new Vector3[6]; for (int i = 0; i < 6; i++) { float r = i * 75 * Mathf.Deg2Rad; helix[i] = V(Mathf.Cos(r) * .1f, -.4f - i * .035f, Mathf.Sin(r) * .1f); }
                var mirrored = System.Array.ConvertAll(helix, p => V(-p.x, p.y, p.z));
                f.Add(OnArmR, "wood", M.Rod("a.gauntlet.root.r", helix, .013f, .009f, 6, 16), Matrix4x4.identity);
                f.Add(OnArmL, "wood", M.Rod("a.gauntlet.root.l", mirrored, .013f, .009f, 6, 16), Matrix4x4.identity);
                f.Pair(OnArmR, "lit", Cube, V(.084f, -.44f, .03f), S(.02f), V(0, 0, 45)); f.Pair(OnArmR, "lit", Cube, V(-.03f, -.5f, .08f), S(.02f), V(0, 0, 45));
            }
        }

        // ------------------------------------------------------------------------------------------------ the shoulders
        /// <summary>
        /// Pauldrons (<paramref name="big"/>) and spaulders on a model: a cap of eight plates over each shoulder, open on the neck's
        /// side, its rim a band of its own. Pauldrons: leather on a strap in the first bands, plate from the third, rivets from the
        /// second, a lame under the cap and a ridge from the fourth, a spike in the last, a little bigger each band; bone: bone
        /// plates with horns. Spaulders: a smaller cap and two or three lames stepping down the upper arm, open on the inside;
        /// glass panes, bark plates with moss.
        /// </summary>
        void AngularShoulders(Fit f, bool big)
        {
            var c = model.Cloud; float pm = PerMetre; string v = f.v; bool bone = big && v == "bone";
            string main = big ? (bone ? "bone" : f.tier >= 2 ? "plate" : "leather2") : v == "glass" ? "glass" : v == "bark" ? "bark" : f.tier >= 1 ? "plate" : "leather2";
            string rimRole = f.tier >= 2 ? "trim" : "dark";
            float grow = big ? 1 + .04f * f.tier : .82f + .03f * f.tier;
            foreach (bool left in Sides)
            {
                var a = ModelArmour.Arm(c, left); if (!a.ok) continue;
                var cap = ModelArmour.ShoulderCap(c, ArmKey + (big ? ".pauldron" : ".spaulder"), left, .12f * grow * pm, (big ? .055f : .04f) * grow * pm, .007f * pm, .005f * pm, (big ? .008f : .004f) * pm, 8,
                    out var rings, out var apex, out var u, out var fwd, out var rim, .004f * pm, .012f * pm);
                if (cap == null) continue;
                var weigh = ModelArmour.Between(a.clavicle, .45f, a.upperBone); string wk = "c" + a.clavicle + "u" + a.upperBone;
                f.Built(OnBody, main, cap); f.Built(OnBody, rimRole, rim);
                // Lames down the upper arm, open on the side toward the body.
                int lames = big ? (f.tier >= 3 && !bone ? 1 : 0) : (f.tier >= 2 ? 3 : 2);
                var inward = -a.outward; var armAxis = (a.elbow - a.shoulder).normalized;
                for (int i = 0; i < lames; i++)
                {
                    float t0 = (big ? .34f : .26f) + i * .13f;
                    bool Inside(int s, int k) { return Vector3.Dot(ModelArmour.FacetOut(k, 8, armAxis, Vector3.forward), inward) > .45f; }
                    f.Built(OnBody, main, ModelArmour.ArmRing(c, ArmKey + ".lame", left, a.shoulder, a.elbow, t0, t0 + .12f, 2, 8, (.006f + .0025f * i) * pm, 0, .004f * pm, .004f * pm, .1f, .95f, Vector3.forward, ModelArmour.On(a.upperBone), Inside));
                }
                if (big && f.tier <= 1 && !bone)   // the strap round the arm
                    f.Built(OnBody, "dark", ModelArmour.ArmRing(c, ArmKey + ".strap", left, a.shoulder, a.elbow, .3f, .36f, 2, 8, .003f * pm, 0, 0, .003f * pm, .2f, .5f, Vector3.forward, ModelArmour.On(a.upperBone)));
                // What sits on the cap: on its outer plates (ring 1's corners), its front, its top.
                var ring = rings[1]; Vector3 centre = Vector3.zero; foreach (var p in ring) centre += p; centre /= ring.Length;
                Vector3 Corner(Vector3 dir) { int best = 0; for (int k = 1; k < ring.Length; k++) if (Vector3.Dot(ring[k] - centre, dir) > Vector3.Dot(ring[best] - centre, dir)) best = k; return ring[best]; }
                Vector3 Out(Vector3 p) { return (p - centre).normalized; }
                var front = Corner(fwd); var outer = Corner(a.outward); var back = Corner(-fwd);
                void Part(string role, Mesh mesh, Vector3 pos, Vector3 outward, Vector3 metres, float roll = 0)
                {
                    f.Built(OnBody, role, ModelArmour.Rigid(c, mesh, Matrix4x4.TRS(pos, Quaternion.LookRotation(outward, u) * Quaternion.Euler(0, 0, roll), metres * pm), weigh, wk));
                }
                if ((big && f.tier >= 1) || (!big && f.l.extra))
                    for (int k = 0; k < ring.Length; k++)
                    {
                        if (Vector3.Dot(Out(ring[k]), a.outward) < -.3f) continue;   // not on the open, neck side
                        Part("dark", Cube, Vector3.Lerp(ring[k], rings[0][k], .5f) + Out(ring[k]) * .002f * pm, Out(ring[k]), new Vector3(.006f, .006f, .003f), 45);
                    }
                if (f.tier >= 3 && !bone && v != "bark")
                {
                    // A ridge from the front plate over the top to the back.
                    void Bar(Vector3 p, Vector3 q) { var d = q - p; f.Built(OnBody, "trim", ModelArmour.Rigid(c, Cube, Matrix4x4.TRS((p + q) / 2 + u * .004f * pm, Quaternion.LookRotation(d, u), new Vector3(.005f * pm, .008f * pm, d.magnitude)), weigh, wk)); }
                    Bar(front, apex); Bar(apex, back);
                }
                if (f.tier >= 4 && !bone && v != "bark") f.Built(OnBody, main, ModelArmour.Rigid(c, M.Cone(6), Matrix4x4.TRS(apex, Quaternion.FromToRotation(Vector3.up, u + a.outward * .3f), new Vector3(.012f, .03f, .012f) * pm), weigh, wk));
                if (bone)
                {
                    var o = a.outward; var hp = new[] { apex, apex + (u * .035f + o * .015f) * pm, apex + (u * .06f + o * .05f - fwd * .015f) * pm };
                    f.Built(OnBody, "bone", ModelArmour.Rigid(c, M.Rod("a.pauldron.horn." + ArmKey + left, hp, .011f * pm, .002f * pm), I, weigh, wk));
                }
                if (v == "bark") f.Built(OnBody, "green", ModelArmour.Rigid(c, M.Lump("a.spaulder.moss", 31, .5f), Matrix4x4.TRS(apex - u * .004f * pm, Quaternion.LookRotation(fwd, u), new Vector3(.05f, .015f, .04f) * pm), weigh, wk));
                if (f.l.accents >= 1) Part("glow", Cube, front + Out(front) * .003f * pm, Out(front), new Vector3(.012f, .012f, .004f), 45);
                if (f.l.accents >= 2) Part("glow", Cube, outer + Out(outer) * .003f * pm, Out(outer), new Vector3(.011f, .011f, .004f), 45);
                if (f.l.worn) Part("rust", Cube, Vector3.Lerp(outer, apex, .4f) + Out(outer) * .002f * pm, Out(outer), new Vector3(.016f, .012f, .002f));
                if (left) MarkOnShoulder(f, outer + Out(outer) * .002f * pm, Out(outer), u, weigh, wk);
            }
        }
        void MarkOnShoulder(Fit f, Vector3 pos, Vector3 outward, Vector3 up, System.Func<Vector3, BoneWeight> weigh, string wk)
        {
            if (!f.parts.Exists(p => p.at == OnBody && p.role == MarkRole(f.l.detail))) return;
            var tmp = new Fit(f.k); Mark(tmp, OnBody, V0, V0, 1, false);
            var place = Matrix4x4.TRS(pos, Quaternion.LookRotation(outward, up), Vector3.one * PerMetre * .2f);
            foreach (var p in tmp.parts) f.Built(OnBody, p.role, ModelArmour.Rigid(model.Cloud, p.mesh, place * p.m, weigh, wk));
        }

        /// <summary>A part built on the model (ModelArmour.Built) as a skinned part on the model's own bones.</summary>
        Transform BuiltPart(Transform root, Mesh mesh, Material m)
        {
            var go = new GameObject("Gear part"); go.transform.SetParent(root, false);
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh; smr.bones = model.BonesFor(model.Cloud.bones); smr.rootBone = model.Hips; smr.sharedMaterial = m;
            smr.quality = SkinQuality.Bone4; smr.updateWhenOffscreen = false; smr.localBounds = new Bounds(Vector3.zero, Vector3.one * 2.6f);
            return go.transform;
        }
    }
}
