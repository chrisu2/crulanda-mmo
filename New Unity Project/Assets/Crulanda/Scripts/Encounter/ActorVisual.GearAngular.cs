using System.Collections.Generic;
using UnityEngine;
using M = Crulanda.Encounter.GearMeshes;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Gloves, gauntlets, wraps, pauldrons, spaulders and hoods built on the model figure itself (play note 239, 2026-10-18,
    /// Chris: "helms and gauntlet still tooo round" / "in the 04-sets-front"; 2026-10-10: "we really need a complete overhaul on
    /// armor/weapons/helms, all seem to not fit properly"). On the old figure these were lathed domes and balls drawn round its
    /// ball head and mitten hands; on a model they were scaled over the head (SeatHead, HatFit) or warped onto the hand's box
    /// (ModelArmour), so a glove was a ball bigger than the fist and a hood a bell round the head.
    ///
    /// Here each is measured from the model: a glove is the hand's own skin stood off it (ModelArmour.HandShell), a cuff, a
    /// wrap's band or a lame a ring of plates round the forearm or upper arm, a pauldron a faceted cap over the shoulder, a hood
    /// cloth laid round the skull and down onto the shoulders (play note 242). Every family keeps its look's materials (the
    /// quality ladder, the zones' palettes) and its tier's extras; only the shapes and the fit are new. The smooth figure
    /// (models off) keeps the old drawings.
    ///
    /// The helms built round the head here (2026-10-18) were taken out again on 2026-10-19 (play note 242, Chris: "i think we
    /// have reverted too much bring back what we had before. helm dont fit. most sit around the head a too large for the head");
    /// caps, kettle hats, barbutes and coifs are drawn and seated as before (ActorVisual.GearArmor, SeatHead and HatFit).
    /// GAME-ONLY designs.
    /// </summary>
    public sealed partial class ActorVisual
    {
        bool AngularArms { get { return model != null && model.Cloud != null && model.Arms is SkinnedMeshRenderer; } }
        static readonly bool[] Sides = { false, true };
        static readonly Matrix4x4 I = Matrix4x4.identity;

        // ------------------------------------------------------------------------------------------------ the hood
        /// <summary>
        /// A head measured for a hood, in the model root's space (the cloud's bind pose; the figure faces -z there): the skull's
        /// axis, its top, eye line, brow and jaw, the neck's base, the shoulders' half-width (joint to joint), the bones that
        /// carry the cloth, and the points it lies over (the head; the neck, the shoulders and the upper chest and back, never
        /// further out than the shoulder joints).
        /// </summary>
        sealed class HoodFit
        {
            public Vector3 axis; public float top, eyes, brow, jaw, neck, half, pm;
            public int head, neckBone, chest, clavL, clavR;
            public readonly List<Vector3> headPts = new List<Vector3>(), bodyPts = new List<Vector3>();
            public Vector3 At(float y) { return new Vector3(axis.x, y, axis.z); }
            /// <summary>Metres as root units.</summary>
            public float M(float metres) { return metres * pm; }
        }
        /// <summary>The cloth laid round the head and down onto the shoulders: rows of points from the crown down (24 round each,
        /// point 0 to the front), the crown's point, which rows the face opening spans, and the meshes.</summary>
        sealed class HoodGrid
        {
            public readonly List<Vector3[]> rows = new List<Vector3[]>(); public Vector3 apex; public int brow, jaw, neck; public Mesh shell, hem;
        }
        const int HoodN = 24;
        static readonly Dictionary<string, HoodFit> hoodFits = new Dictionary<string, HoodFit>();
        static readonly Dictionary<string, HoodGrid> hoodGrids = new Dictionary<string, HoodGrid>();
        /// <summary>Column k's direction round the head (k = 0 straight ahead: -z in the root).</summary>
        static Vector3 HoodDir(int k) { float a = 2 * Mathf.PI * k / HoodN; return new Vector3(Mathf.Sin(a), 0, -Mathf.Cos(a)); }
        /// <summary>How far ahead column k faces (1 straight ahead, -1 straight back).</summary>
        static float HoodAhead(float k) { return Mathf.Cos(2 * Mathf.PI * k / HoodN); }
        static int BoneOf(ModelArmour.Cloud c, Transform t)
        {
            if (t == null) return -1;
            for (int i = 0; i < c.bones.Length; i++) if (string.Equals(c.bones[i], t.name, System.StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
        /// <summary>How far the points within <paramref name="band"/> of height y reach from the axis along d (their support), or
        /// <paramref name="none"/>.</summary>
        static float HoodReach(List<Vector3> pts, Vector3 axis, float y, float band, Vector3 d, float none)
        {
            float best = float.MinValue;
            foreach (var p in pts) { if (Mathf.Abs(p.y - y) > band) continue; float r = (p.x - axis.x) * d.x + (p.z - axis.z) * d.z; if (r > best) best = r; }
            return best == float.MinValue ? none : best;
        }

        HoodFit MeasureHood()
        {
            var c = model.Cloud; if (c == null || c.p == null || c.part == null) return null;
            float pm = PerMetre; string key = c.GetHashCode() + "." + K(pm);
            if (hoodFits.TryGetValue(key, out var done)) return done;
            var h = new HoodFit { pm = pm }; hoodFits[key] = null;
            h.head = BoneOf(c, model.HeadBone); if (h.head < 0) return null;
            h.neckBone = BoneOf(c, model.Neck); if (h.neckBone < 0) h.neckBone = h.head;
            h.chest = BoneOf(c, model.Chest); if (h.chest < 0) h.chest = h.neckBone;
            var aL = ModelArmour.Arm(c, true); var aR = ModelArmour.Arm(c, false); if (!aL.ok || !aR.ok) return null;
            h.clavL = aL.clavicle >= 0 ? aL.clavicle : h.chest; h.clavR = aR.clavicle >= 0 ? aR.clavicle : h.chest;
            Vector3 hb = c.bind[h.head].GetColumn(3); h.top = float.MinValue;
            for (int i = 0; i < c.p.Length; i++) if (c.part[i] == ModelArmour.Part.Head) { h.headPts.Add(c.p[i]); h.top = Mathf.Max(h.top, c.p[i].y); }
            if (h.headPts.Count < 50) return null;
            float H = h.top - hb.y; if (H < h.M(.05f)) return null;
            // The eyes about 10 cm over the Head bone (ActorVisual.Model, Frames), the brow a little over them, the jaw at the bone.
            h.eyes = hb.y + Mathf.Min(h.M(.1f), .5f * H); h.brow = h.eyes + h.M(.035f); h.jaw = hb.y - h.M(.01f);
            float x = 0, zLo = float.MaxValue, zHi = float.MinValue; int nx = 0;
            foreach (var p in h.headPts) if (p.y >= h.eyes) { x += p.x; nx++; zLo = Mathf.Min(zLo, p.z); zHi = Mathf.Max(zHi, p.z); }
            if (nx == 0) return null;
            h.axis = new Vector3(x / nx, 0, (zLo + zHi) / 2);
            h.half = Mathf.Abs(aL.shoulder.x - aR.shoulder.x) / 2; float shoulderY = (aL.shoulder.y + aR.shoulder.y) / 2;
            float neckY = h.neckBone != h.head ? c.bind[h.neckBone].m13 : Mathf.Lerp(shoulderY, hb.y, .4f);
            h.neck = Mathf.Max(shoulderY + h.M(.02f), Mathf.Min(neckY, h.jaw - h.M(.03f)));
            // What the cowl lies on: the neck, the shoulders and the upper chest and back; an arm only within the shoulder joint
            // (in a T pose the arms stand out level with the shoulders: they are no part of what the cloth falls on).
            float reach = h.half + h.M(.015f), low = h.neck - h.M(.25f);
            for (int i = 0; i < c.p.Length; i++)
            {
                var p = c.p[i]; var part = c.part[i];
                if (part == ModelArmour.Part.LegL || part == ModelArmour.Part.LegR || p.y < low || p.y > h.jaw) continue;
                float dx = p.x - h.axis.x, dz = p.z - h.axis.z; if (dx * dx + dz * dz > reach * reach) continue;
                h.bodyPts.Add(p);
            }
            hoodFits[key] = h; return h;
        }

        /// <summary>
        /// The hood's cloth (play note 242, 2026-10-19, Chris: "hood is awful"; the old hood, a lathed bell drawn round the old ball
        /// head and scaled over the model's, stood out round the head with a wide flat collar disc). Laid on the model's own
        /// points: round the skull about 1 cm off (a little more at the back, where cloth gathers), a face opening from the brow
        /// to the chin, falling from the ears and the back of the head to the jaw (not clinging in under them), sagging in onto
        /// the neck, and on over the shoulders and the upper chest and back, a few centimetres longer at the back, short over
        /// the shoulder points; soft folds in the cowl, no rims. <paramref name="shortHem"/>: tucked short under shoulder armour.
        /// <paramref name="peak"/>: the oilskin's cloth stands a little forward over the brow.
        /// </summary>
        HoodGrid HoodOn(HoodFit h, bool shortHem, bool peak)
        {
            string key = h.GetHashCode() + "." + shortHem + "." + peak;
            if (hoodGrids.TryGetValue(key, out var g)) return g;
            g = new HoodGrid(); float gap = h.M(.011f), pull = h.M(.005f);
            var ys = new List<float>();
            float y0 = h.top - h.M(.006f);
            for (int i = 0; i <= 6; i++) ys.Add(Mathf.Lerp(y0, h.brow, i / 6f)); g.brow = ys.Count - 1;
            for (int i = 1; i <= 4; i++) ys.Add(Mathf.Lerp(h.brow, h.jaw, i / 4f)); g.jaw = ys.Count - 1;
            for (int i = 1; i <= 3; i++) ys.Add(Mathf.Lerp(h.jaw, h.neck, i / 3f)); g.neck = ys.Count - 1;
            var prev = new float[HoodN];
            // Round the skull: each row the head's reach there (half the rows' spacing up and down) and the gap; from the eyes to the
            // jaw the cloth falls from what is above it (it hangs, it does not cling under the skull, the ears or the chin); under the
            // jaw it sags in toward the neck and the body, half the way each row (cloth lies on what is under it).
            for (int r = 0; r < ys.Count; r++)
            {
                float y = ys[r], band = r == 0 ? h.M(.006f) : Mathf.Abs(ys[r] - ys[r - 1]) * .5f + h.M(.002f);
                bool onBody = r > g.jaw; var row = new Vector3[HoodN];
                for (int k = 0; k < HoodN; k++)
                {
                    var d = HoodDir(k); float ahead = HoodAhead(k);
                    float rr = HoodReach(onBody ? h.bodyPts : h.headPts, h.axis, y, band, d, 0);
                    if (onBody) rr = Mathf.Max(rr, HoodReach(h.headPts, h.axis, y, band, d, 0));
                    rr += gap + pull * Mathf.Max(0, -ahead);
                    if (peak && r == g.brow) rr += h.M(.012f) * Mathf.Max(0, ahead) * Mathf.Max(0, ahead);
                    if (peak && r == g.brow - 1) rr += h.M(.006f) * Mathf.Max(0, ahead) * Mathf.Max(0, ahead);
                    if (onBody) rr = Mathf.Max(rr, (rr + prev[k]) / 2);   // under the jaw it sags in toward the neck, half the way a row
                    else if (y < h.eyes && r > 0) rr = Mathf.Max(rr, prev[k]);
                    rr = Mathf.Max(rr, h.M(.012f)); prev[k] = rr;
                    row[k] = h.At(y) + d * rr;
                }
                g.rows.Add(row);
            }
            // The cowl: from the neck's base down onto the shoulders, the chest and the back. Each column its own length: longest at
            // the back, short over the shoulder points; it never stands wider than the shoulders.
            const int cowlRows = 6; float limit = h.half + h.M(.02f);
            for (int i = 1; i <= cowlRows; i++)
            {
                float t = i / (float)cowlRows; var row = new Vector3[HoodN];
                for (int k = 0; k < HoodN; k++)
                {
                    var d = HoodDir(k); float ahead = HoodAhead(k), a = Mathf.Abs(ahead);
                    float drop = Mathf.Lerp(.045f, ahead >= 0 ? .12f : .14f, Mathf.Pow(a, 1.5f)) * (shortHem ? .5f : 1);
                    float y = Mathf.Lerp(h.neck, h.neck - h.M(drop), t), band = h.M(drop) / cowlRows * .5f + h.M(.004f);
                    float own = HoodReach(h.bodyPts, h.axis, y, band, d, 0) + h.M(.01f), rr = Mathf.Max(own, (own + prev[k]) / 2);
                    prev[k] = rr;
                    rr += h.M(.007f) * t * a * (.5f + .5f * Mathf.Sin(2 * Mathf.PI * k * 5f / HoodN));   // soft folds, front and back
                    rr = Mathf.Min(rr, limit / Mathf.Max(.2f, Mathf.Abs(d.x)));   // no wider than the shoulders
                    row[k] = h.At(y) + d * rr;
                }
                g.rows.Add(row);
            }
            g.apex = h.At(h.top + gap * .8f) + Vector3.forward * h.M(.008f);   // the crown's point a little back (+z)
            int brow = g.brow, jaw = g.jaw;
            bool Open(int s, int k) { return s >= brow && s < jaw && Mathf.Abs(Mathf.DeltaAngle(0, 360f * (k + .5f) / HoodN)) < 55; }
            float thick = h.M(.004f);
            g.shell = ModelArmour.Skin(model.Cloud, HoodLoft(g.rows, g.apex, h.axis, thick, Open), p => HoodWeight(h, p));
            // The hem's binding: the cowl's last fifth, a hair proud of it.
            var hemRows = new List<Vector3[]>(); int last = g.rows.Count - 1;
            foreach (float u in new[] { .55f, 1f })
            {
                var row = new Vector3[HoodN];
                for (int k = 0; k < HoodN; k++) { var p = Vector3.Lerp(g.rows[last - 1][k], g.rows[last][k], u); var o = p - h.At(p.y); row[k] = p + o.normalized * h.M(.0015f); }
                hemRows.Add(row);
            }
            g.hem = ModelArmour.Skin(model.Cloud, HoodLoft(hemRows, null, h.axis, thick, null), p => HoodWeight(h, p));
            hoodGrids[key] = g; return g;
        }

        /// <summary>
        /// Cloth through rows of points (top down, the same count round each), closed over the top in <paramref name="apex"/> when
        /// given, <paramref name="thick"/> deep (drawn in toward the axis), shaded smooth; <paramref name="open"/>(segment, k)
        /// leaves out the face between rows s and s + 1 and columns k and k + 1; every edge left bare gets a thin wall.
        /// </summary>
        static Mesh HoodLoft(List<Vector3[]> rows, Vector3? apex, Vector3 axis, float thick, System.Func<int, int, bool> open)
        {
            int R = rows.Count, n = rows[0].Length, segs = R - 1;
            var v = new List<Vector3>(); var t = new List<int>();
            Vector3 In(Vector3 p) { var o = p - new Vector3(axis.x, p.y, axis.z); return o.sqrMagnitude > 1e-12f ? p - o.normalized * Mathf.Min(thick, o.magnitude * .5f) : p - Vector3.up * thick; }
            Vector3 Out(Vector3 p) { return p - new Vector3(axis.x, p.y, axis.z); }
            for (int r = 0; r < R; r++) foreach (var p in rows[r]) v.Add(p);
            int inner = v.Count; for (int r = 0; r < R; r++) foreach (var p in rows[r]) v.Add(In(p));
            int ap = -1, api = -1; if (apex.HasValue) { ap = v.Count; v.Add(apex.Value); api = v.Count; v.Add(apex.Value - Vector3.up * thick); }
            int O(int r, int k) { return r * n + (k % n + n) % n; }
            void Tri(int a, int b, int c, Vector3 outward)
            {
                var nrm = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
                if (Vector3.Dot(nrm, outward) >= 0) { t.Add(a); t.Add(b); t.Add(c); } else { t.Add(a); t.Add(c); t.Add(b); }
            }
            void Quad(int a, int b, int c, int d, Vector3 outward) { Tri(a, b, c, outward); Tri(a, c, d, outward); }
            void Wall(Vector3 a, Vector3 b, Vector3 from)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(In(b)); v.Add(In(a)); var mid = (a + b) / 2;
                Quad(i, i + 1, i + 2, i + 3, mid - from);
            }
            bool Shut(int s, int k) { return s < 0 || s >= segs || (open != null && open(s, (k % n + n) % n)); }
            if (apex.HasValue)
                for (int k = 0; k < n; k++)
                {
                    Tri(ap, O(0, k), O(0, k + 1), Vector3.up); Tri(api, inner + O(0, k), inner + O(0, k + 1), Vector3.down);
                }
            for (int s = 0; s < segs; s++)
                for (int k = 0; k < n; k++)
                {
                    if (Shut(s, k)) continue;
                    Vector3 a = rows[s][k], b = rows[s][(k + 1) % n], c = rows[s + 1][(k + 1) % n], d = rows[s + 1][k];
                    var mid = (a + b + c + d) / 4; var outward = Out(mid);
                    Quad(O(s, k), O(s, k + 1), O(s + 1, k + 1), O(s + 1, k), outward);
                    Quad(inner + O(s, k), inner + O(s, k + 1), inner + O(s + 1, k + 1), inner + O(s + 1, k), -outward);
                    if (Shut(s, k - 1)) Wall(a, d, mid);
                    if (Shut(s, k + 1)) Wall(b, c, mid);
                    if (Shut(s - 1, k) && !(s == 0 && apex.HasValue)) Wall(a, b, mid);
                    if (Shut(s + 1, k)) Wall(d, c, mid);
                }
            var m = new Mesh { name = "Hood cloth" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>The cloth's bones: the head over the jaw; the head giving way to the neck down to its base; under it the neck,
        /// the chest and, out toward each shoulder, that collarbone.</summary>
        static BoneWeight HoodWeight(HoodFit h, Vector3 p)
        {
            var w = new Dictionary<int, float>();
            void Add(int b, float x) { if (b < 0 || x <= 0) return; w.TryGetValue(b, out float o); w[b] = o + x; }
            if (p.y >= h.jaw) Add(h.head, 1);
            else if (p.y >= h.neck) { float u = (p.y - h.neck) / Mathf.Max(1e-5f, h.jaw - h.neck); Add(h.head, u); Add(h.neckBone, 1 - u); }
            else
            {
                float s = Mathf.Clamp01((h.neck - p.y) / h.M(.06f)), side = Mathf.Clamp01(Mathf.Abs(p.x - h.axis.x) / Mathf.Max(1e-5f, h.half)) * .7f;
                Add(h.neckBone, 1 - s); Add(h.chest, s * (1 - side)); Add(p.x > h.axis.x ? h.clavL : h.clavR, s * side);   // the model's left is +x
            }
            var list = new List<KeyValuePair<int, float>>(w); list.Sort((a, b) => b.Value.CompareTo(a.Value));
            float sum = 0; for (int i = 0; i < list.Count && i < 4; i++) sum += list[i].Value; if (sum <= 0) return new BoneWeight { boneIndex0 = Mathf.Max(0, h.head), weight0 = 1 };
            var bw = new BoneWeight();
            if (list.Count > 0) { bw.boneIndex0 = list[0].Key; bw.weight0 = list[0].Value / sum; }
            if (list.Count > 1) { bw.boneIndex1 = list[1].Key; bw.weight1 = list[1].Value / sum; }
            if (list.Count > 2) { bw.boneIndex2 = list[2].Key; bw.weight2 = list[2].Value / sum; }
            if (list.Count > 3) { bw.boneIndex3 = list[3].Key; bw.weight3 = list[3].Value / sum; }
            return bw;
        }

        /// <summary>
        /// A hood (or the Cult's bone mask in a hood) on a model, laid on its own head and shoulders (<see cref="HoodOn"/>). Its
        /// materials and its tier's extras as before: the hem's binding (trim from the second band), a clasp at the throat from the
        /// third or with the extra, glows, wear, the mark. Mask: a bone face in the opening with two eye holes and a brow ridge;
        /// tear: its weeping violet light. False when the head could not be measured (the old drawing is used).
        /// </summary>
        bool AngularHood(Fit f, bool mask)
        {
            if (model == null || model.Cloud == null) return false;
            var h = MeasureHood(); if (h == null) return false;
            var g = HoodOn(h, f.shoulders, f.v == "oilskin");
            f.onHead = true;
            string main = f.v == "oilskin" ? "oil" : "cloth", hemRole = f.tier >= 1 ? "trim" : "cloth2";
            f.Built(OnBody, main, g.shell);
            f.Built(OnBody, hemRole, g.hem);
            var fwd = HoodDir(0); var up = Vector3.up;
            // The throat: the front column halfway down the neck.
            int throatRow = (g.jaw + g.neck + 1) / 2; var throat = g.rows[throatRow][0] + fwd * h.M(.004f);
            if (f.tier >= 2 || f.l.extra) Stud(f, OnBody, "trim", Sphere, throat, fwd, up, new Vector3(.02f, .02f, .012f), h.neckBone);
            if (mask)
            {
                float front = HoodReach(h.headPts, h.axis, h.eyes, h.M(.02f), fwd, h.M(.09f)), wide = HoodReach(h.headPts, h.axis, h.eyes, h.M(.02f), Vector3.right, h.M(.07f));
                var face = h.At(h.eyes - h.M(.02f)) + fwd * (front - h.M(.025f));
                Stud(f, OnBody, "bone", Sphere, face, fwd, up, new Vector3(2 * wide * .8f / h.pm, .14f, .07f), h.head);
                foreach (float sx in new[] { -1f, 1f })
                    Stud(f, OnBody, "slot", Sphere, h.At(h.eyes) + fwd * (front + h.M(.006f)) + Vector3.right * h.M(.026f) * sx, fwd, up, new Vector3(.028f, .017f, .01f), h.head);
                if (f.v == "tear") Stud(f, OnBody, "lit", Cube, h.At(h.eyes - h.M(.035f)) + fwd * (front + h.M(.004f)) + Vector3.right * h.M(.034f), fwd, up, new Vector3(.01f, .04f, .004f), h.head, 5);
                else Stud(f, OnBody, "bone", Cube, h.At(h.eyes + h.M(.024f)) + fwd * (front + h.M(.002f)), fwd, up, new Vector3(.09f, .012f, .01f), h.head);
                if (f.l.accents >= 1) Stud(f, OnBody, "glow", Cube, h.At(h.eyes + h.M(.04f)) + fwd * (front + h.M(.002f)), fwd, up, new Vector3(.012f, .012f, .005f), h.head, 45);
            }
            else if (f.l.accents >= 1) Stud(f, OnBody, "glow", Cube, throat + fwd * h.M(.004f) - up * h.M(.02f), fwd, up, new Vector3(.012f, .012f, .005f), h.neckBone, 45);
            if (f.l.accents >= 2)
            {
                if (mask) Stud(f, OnBody, "glow", Cube, throat + fwd * h.M(.004f), fwd, up, new Vector3(.011f, .011f, .005f), h.neckBone, 45);
                else Stud(f, OnBody, "glow", Cube, g.rows[g.brow - 1][0] + fwd * h.M(.002f), fwd, up, new Vector3(.011f, .011f, .005f), h.head, 45);
            }
            if (f.l.worn)
            {
                int k = HoodN * 3 / 8; var d = HoodDir(k);
                Stud(f, OnBody, "rust", Cube, g.rows[g.brow - 2][k] + d * h.M(.002f), d, up, new Vector3(.03f, .025f, .002f), h.head);
            }
            { int k = HoodN - 3, r = g.rows.Count - 2; var d = HoodDir(k); MarkOn(f, OnBody, g.rows[r][k] + d * h.M(.003f), d, up, h.chest, .6f, false); }
            return true;
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
