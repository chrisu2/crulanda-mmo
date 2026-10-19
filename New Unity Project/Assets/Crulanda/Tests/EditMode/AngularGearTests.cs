using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// Helms, hand pieces and shoulders that fit (play note 239, 2026-10-18, Chris: "helms and gauntlet still tooo round" / "in
    /// the 04-sets-front"; 2026-10-10: "all seem to not fit properly"). Pure measuring, no looking: a faceted shell built round
    /// points stands outside every one of them and close to them; on a model figure every helm comes down over the skull and
    /// stands within 2 cm of the head (or hair) all round its top, none perched; a glove or gauntlet is no bigger than a hand
    /// and a quarter; a pauldron sits on the shoulder; and none of their main parts is round (a sphere, a lathed dome: the share
    /// of smooth-shaded triangles, AngularShell.Roundness). Today's pieces fail these by their logic: the domes and balls are
    /// smooth-shaded, the mitten ball is half again a hand deep, and the pack's helms stood above the skull.
    /// </summary>
    public class AngularGearTests
    {
        readonly List<GameObject> made = new List<GameObject>();
        bool models;
        [SetUp] public void On() { models = ActorVisual.Models; ActorVisual.Models = true; }
        [TearDown] public void TearDown() { foreach (var g in made) if (g != null) Object.DestroyImmediate(g); made.Clear(); ActorVisual.Models = models; }

        ActorVisual Figure(ActorLook look)
        {
            var go = new GameObject("Angular gear figure"); new GameObject("Body").transform.SetParent(go.transform, false); go.transform.position = new Vector3(made.Count * 3, 1, 0); made.Add(go);
            return ActorVisual.Attach(go, look, 0, false, null);
        }
        static GearLooks Looks() { return GearLooks.Parse(Resources.Load<TextAsset>("Gear/looks").text); }
        /// <summary>A named test item wearing a look (as the wardrobe capture makes them).</summary>
        static string Item(ItemDatabase db, GearLooks looks, string look, string slot, int level)
        {
            string id = "angular." + db.Items.Count; looks.Register(id, look);
            db.Items[id] = new ItemDef { id = id, name = look, kind = "gear", slot = slot, quality = 3, level = level, canonStatus = "GAME-ONLY" };
            return id;
        }
        static readonly string[] HelmLooks = { "head.cap:plain/oakhaven", "head.cap:flaps/khaven", "head.cap:leaf/veridian", "head.kettle:plain/tollroad", "head.kettle:half/sandthrone", "head.barbute:plain/tollroad", "head.barbute:rimed/pale+glow", "head.coif:mail/tollroad" };
        static readonly string[] HandLooks = { "hands.gloves:plain/oakhaven", "hands.gloves:fingerless/sandthrone", "hands.gloves:mitts/pilgrim", "hands.gauntlets:plate/tollroad", "hands.gauntlets:bone/ashwalker", "hands.gauntlets:glass/pale+glow" };
        static readonly string[] ShoulderLooks = { "shoulder.pauldron:dome/tollroad", "shoulder.pauldron:bone/ashwalker", "shoulder.spaulder:lames/tollroad", "shoulder.spaulder:bark/veridian" };

        // ------------------------------------------------------------------------------------------------ measuring
        /// <summary>Every triangle a renderer draws, in world space (a skinned one as it is posed).</summary>
        static void Triangles(Renderer r, List<Vector3> into)
        {
            Mesh mesh; Matrix4x4 w = r.transform.localToWorldMatrix;
            if (r is SkinnedMeshRenderer s) { mesh = new Mesh(); s.BakeMesh(mesh, true); }
            else { var mf = r.GetComponent<MeshFilter>(); mesh = mf != null ? mf.sharedMesh : null; }
            if (mesh == null) return;
            var v = mesh.vertices; foreach (int i in mesh.triangles) into.Add(w.MultiplyPoint3x4(v[i]));
        }
        static List<Vector3> Points(Renderer r)
        {
            var list = new List<Vector3>(); Mesh mesh; Matrix4x4 w = r.transform.localToWorldMatrix;
            if (r is SkinnedMeshRenderer s) { mesh = new Mesh(); s.BakeMesh(mesh, true); } else { var mf = r.GetComponent<MeshFilter>(); mesh = mf != null ? mf.sharedMesh : null; }
            if (mesh == null) return list;
            var used = new bool[mesh.vertexCount]; foreach (int i in mesh.triangles) used[i] = true; var v = mesh.vertices;
            for (int i = 0; i < v.Length; i++) if (used[i]) list.Add(w.MultiplyPoint3x4(v[i]));
            return list;
        }
        /// <summary>The nearest hit of a ray on a triangle soup (Moller-Trumbore, both faces), or +infinity.</summary>
        static float Hit(List<Vector3> tris, Vector3 o, Vector3 d)
        {
            float best = float.PositiveInfinity;
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                Vector3 a = tris[i], e1 = tris[i + 1] - a, e2 = tris[i + 2] - a, p = Vector3.Cross(d, e2); float det = Vector3.Dot(e1, p);
                if (Mathf.Abs(det) < 1e-12f) continue; float inv = 1 / det; var s = o - a; float u = Vector3.Dot(s, p) * inv; if (u < 0 || u > 1) continue;
                var q = Vector3.Cross(s, e1); float vv = Vector3.Dot(d, q) * inv; if (vv < 0 || u + vv > 1) continue;
                float t = Vector3.Dot(e2, q) * inv; if (t > 1e-6f && t < best) best = t;
            }
            return best;
        }
        /// <summary>The largest renderer under a root (by bounds volume): a piece's main part.</summary>
        static Renderer Main(IEnumerable<Transform> roots)
        {
            Renderer best = null; float vol = -1;
            foreach (var root in roots) if (root != null) foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var b = r is SkinnedMeshRenderer s && s.sharedMesh != null ? s.sharedMesh.bounds : r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null ? r.GetComponent<MeshFilter>().sharedMesh.bounds : new Bounds();
                float v = b.size.x * b.size.y * b.size.z; if (v > vol) { vol = v; best = r; }
            }
            return best;
        }
        static Mesh MeshOf(Renderer r) { return r is SkinnedMeshRenderer s ? s.sharedMesh : r.GetComponent<MeshFilter>().sharedMesh; }

        // ------------------------------------------------------------------------------------------------ the shell itself
        /// <summary>A shell measured round an egg of points (a head's size) stands outside every point, within 2 cm of the egg
        /// all round and over its top, and is flat-faced; a sphere and a lathed dome measure round.</summary>
        [Test] public void A_faceted_shell_holds_close_round_what_it_was_measured_on_and_is_not_round()
        {
            var pts = new List<Vector3>();
            for (int iy = 0; iy <= 40; iy++) for (int ia = 0; ia < 48; ia++)
            {
                float u = iy / 40f * Mathf.PI * .5f, a = ia / 48f * 2 * Mathf.PI;   // the upper half of an egg 15 cm across, 20 deep, 11 high
                pts.Add(new Vector3(Mathf.Sin(a) * .075f * Mathf.Cos(u), .11f * Mathf.Sin(u), Mathf.Cos(a) * .1f * Mathf.Cos(u)));
            }
            float[] hs = { 0, .044f, .077f, .097f }; const float gap = .005f;
            var mesh = ModelArmour.AxisShell(pts, Vector3.zero, Vector3.up, Vector3.forward, hs, null, 8, gap, .003f, null, true, .002f, float.MinValue, float.MaxValue, out _, out _, out var apex);
            var tris = new List<Vector3>(); var v = mesh.vertices; foreach (int i in mesh.triangles) tris.Add(v[i]);
            float worstIn = 0, worstGap = 0;
            foreach (var p in pts)
            {
                if (p.y < .002f) continue;
                var axis = new Vector3(0, p.y, 0); var d = p - axis; float r = d.magnitude;
                if (p.y > .1f || r < .005f) { float t = Hit(tris, p, Vector3.up); worstGap = Mathf.Max(worstGap, t); continue; }   // the crown: straight up
                float hit = Hit(tris, axis, d / r); worstIn = Mathf.Max(worstIn, r - hit); worstGap = Mathf.Max(worstGap, hit - r);
            }
            Assert.LessOrEqual(worstIn, .0005f, "No point of the egg pokes through the shell's inside.");
            Assert.Less(worstGap, .02f, "The shell's inside stands within 2 cm of the egg all round and over the top.");
            Assert.Less(apex.y, .11f + .03f, "Its point is low over the top (no tall dome).");
            Assert.AreEqual(0f, AngularShell.Roundness(mesh), 1e-6f, "Flat plates: every triangle shades flat.");
            Assert.Greater(AngularShell.Roundness(GearMeshes.Prim(PrimitiveType.Sphere)), .8f, "A sphere measures round.");
            var dome = GearMeshes.Lathe("test.angular.dome", new[] { new Vector2(.19f, .885f), new Vector2(.1785f, .936f), new Vector2(.1455f, .981f), new Vector2(.095f, 1.015f), new Vector2(.049f, 1.03f), new Vector2(0, 1.035f) }, 16, true);
            Assert.Greater(AngularShell.Roundness(dome), .5f, "Today's cap dome (a.cap.dome, lathed smooth) measures round: the check fails it.");
        }

        // ------------------------------------------------------------------------------------------------ on a model
        /// <summary>
        /// Every helm on a model: it comes down at least 7 cm under the top of the skull (not perched), and round the top 5 cm of the
        /// skull every ray out from the head's axis meets its inside no nearer than the head and hair and no more than 2 cm beyond
        /// them; straight up it closes within 4.5 cm (a kettle's point included). Its main part is not round.
        /// </summary>
        [Test] public void Helms_come_down_over_the_skull_and_stand_close_all_round()
        {
            Assume.That(ModelFigure.Available, "The character kits are in the project (Resources/Characters).");
            var db = new ItemDatabase(); var looks = Looks(); var fails = new List<string>();
            foreach (var look in HelmLooks)
            {
                var v = Figure(ActorLook.Warrior); var m = v.Model;
                v.ApplyGearIds(new[] { Item(db, looks, look, "head", 13) }, db, looks);
                v.Preview(ActorPose.None, 0, 0);
                var root = v.GearRoot(EquipSlot.Head); var up = root.parent.up;
                var tris = new List<Vector3>(); foreach (var r in root.GetComponentsInChildren<Renderer>(true)) Triangles(r, tris);
                var head = Points(m.Head); var body = new List<Vector3>(head);
                foreach (var r in m.Hair) if (r != null && r.gameObject.activeInHierarchy && r.enabled && r.name.Contains("Hair_") && !r.name.Contains("Beard")) body.AddRange(Points(r));
                var centre = Vector3.zero; foreach (var p in head) centre += p; centre /= head.Count;
                float H(Vector3 p) { return Vector3.Dot(p - centre, up); }
                float skullTop = float.MinValue; foreach (var p in head) skullTop = Mathf.Max(skullTop, H(p));
                float bodyTop = float.MinValue; foreach (var p in body) bodyTop = Mathf.Max(bodyTop, H(p));
                float low = float.MaxValue; for (int i = 0; i < tris.Count; i++) low = Mathf.Min(low, H(tris[i]));
                if (low > skullTop - .07f) { fails.Add(look + ": perched, its lowest edge only " + (skullTop - low).ToString("0.000") + " m under the skull's top"); continue; }
                var side = Vector3.Cross(up, Vector3.forward).sqrMagnitude > .1f ? Vector3.Cross(up, Vector3.forward).normalized : Vector3.right; var fwd = Vector3.Cross(side, up);
                float worstIn = 0, worstGap = 0; int missed = 0;
                for (float dh = .01f; dh <= .05f; dh += .01f)
                    for (int ia = 0; ia < 24; ia++)
                    {
                        float a = ia * 15 * Mathf.Deg2Rad, h = skullTop - dh; var dir = side * Mathf.Sin(a) + fwd * Mathf.Cos(a); var o = centre + up * h;
                        float surf = 0; bool any = false;
                        foreach (var p in body)
                        {
                            if (Mathf.Abs(H(p) - h) > .006f) continue; var q = Vector3.ProjectOnPlane(p - o, up); float r = q.magnitude; if (r < 1e-4f) continue;
                            if (Vector3.Dot(q / r, dir) < Mathf.Cos(8 * Mathf.Deg2Rad)) continue; surf = Mathf.Max(surf, r); any = true;
                        }
                        if (!any) continue;
                        float hit = Hit(tris, o, dir); if (float.IsInfinity(hit)) { missed++; continue; }
                        worstIn = Mathf.Max(worstIn, surf - hit); worstGap = Mathf.Max(worstGap, hit - surf);
                    }
                float upHit = Hit(tris, centre + up * (bodyTop - .01f), up);
                if (missed > 0) fails.Add(look + ": " + missed + " rays round the skull's top meet no helm");
                if (worstIn > .002f) fails.Add(look + ": the head pokes " + worstIn.ToString("0.000") + " m through");
                if (worstGap > .02f) fails.Add(look + ": stands " + worstGap.ToString("0.000") + " m off the head (over 2 cm)");
                if (float.IsInfinity(upHit) || upHit - .01f > .045f) fails.Add(look + ": the top is open or stands " + (upHit - .01f).ToString("0.000") + " m over the head");
                var main = Main(new[] { root }); float round = main != null ? AngularShell.Roundness(MeshOf(main)) : 1;
                if (round > .15f) fails.Add(look + ": its main part is round (" + round.ToString("0.00") + " of it smooth-shaded)");
            }
            Assert.IsEmpty(fails, string.Join("\n", fails));
        }

        /// <summary>
        /// Every glove and gauntlet on a model: past the wrist its bounds (along the hand, across it and through it) are no more
        /// than a quarter over the bare hand's (through it, a quarter and 8 mm: a mitten's lining), and its main part is not
        /// round. The hand's bones carry it (it closes round a grip with the fingers).
        /// </summary>
        [Test] public void Gloves_and_gauntlets_are_hand_sized_and_follow_the_fingers()
        {
            Assume.That(ModelFigure.Available, "The character kits are in the project (Resources/Characters).");
            var db = new ItemDatabase(); var looks = Looks(); var fails = new List<string>();
            foreach (var look in HandLooks)
            {
                var v = Figure(ActorLook.Warrior); var m = v.Model;
                v.Preview(ActorPose.None, 0, 0);
                var arms = (SkinnedMeshRenderer)m.Arms; var w = arms.sharedMesh.boneWeights; var bones = arms.bones;
                var hand = m.HandR; var along = (m.MiddleR.position - hand.position).normalized;
                var baked = new Mesh(); arms.BakeMesh(baked, true); var bv = baked.vertices; var l2w = arms.transform.localToWorldMatrix;
                var bare = new List<Vector3>();
                for (int i = 0; i < bv.Length; i++)
                {
                    var b = w[i]; int dom = b.weight0 >= b.weight1 && b.weight0 >= b.weight2 && b.weight0 >= b.weight3 ? b.boneIndex0 : b.weight1 >= b.weight2 && b.weight1 >= b.weight3 ? b.boneIndex1 : b.weight2 >= b.weight3 ? b.boneIndex2 : b.boneIndex3;
                    var t = bones[dom]; if (t != null && (t == hand || t.IsChildOf(hand))) bare.Add(l2w.MultiplyPoint3x4(bv[i]));
                }
                v.ApplyGearIds(new[] { Item(db, looks, look, "hands", 13) }, db, looks);
                v.Preview(ActorPose.None, 0, 0);
                Transform rightRoot = null; foreach (var r in v.GearLimbRoots(EquipSlot.Hands)) if (r.parent == v.RightArm) rightRoot = r;
                Assert.NotNull(rightRoot, look + ": a right hand's root.");
                // The glove's own parts: not its small accents (studs, gems, knuckle spikes: under a sixth of the biggest part's box).
                var glove = new List<Vector3>(); bool onHand = false; var parts = new List<(List<Vector3> pts, float vol)>(); float most = 0;
                foreach (var r in rightRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var pts = Points(r); if (pts.Count == 0) continue; var bb0 = new Bounds(pts[0], Vector3.zero); foreach (var p in pts) bb0.Encapsulate(p);
                    float vol = bb0.size.x * bb0.size.y * bb0.size.z; parts.Add((pts, vol)); most = Mathf.Max(most, vol);
                    foreach (var bw in r.sharedMesh.boneWeights) { var bb = r.bones[bw.boneIndex0]; if (bb == hand || bb.IsChildOf(hand)) { onHand = true; break; } }
                }
                foreach (var (pts, vol) in parts) if (vol >= most / 6) glove.AddRange(pts);
                if (!onHand) fails.Add(look + ": no part rides the hand's bones");
                var back = Vector3.ProjectOnPlane(hand.up, along).normalized; if (back.sqrMagnitude < .5f) back = Vector3.ProjectOnPlane(hand.forward, along).normalized;
                var across = Vector3.Cross(along, back);
                Vector3 Size(List<Vector3> pts)
                {
                    Vector3 lo = Vector3.one * float.MaxValue, hi = -lo; int n = 0;
                    foreach (var p in pts)
                    {
                        var d = p - hand.position; if (Vector3.Dot(d, along) < .02f) continue;   // past the wrist (and the cuff's lip)
                        var q = new Vector3(Vector3.Dot(d, along), Vector3.Dot(d, across), Vector3.Dot(d, back)); lo = Vector3.Min(lo, q); hi = Vector3.Max(hi, q); n++;
                    }
                    return n == 0 ? Vector3.zero : hi - lo;
                }
                var sb = Size(bare); var sg = Size(glove);
                if (sb.x <= 0) { fails.Add(look + ": the bare hand was not found"); continue; }
                if (sg.x > sb.x * 1.25f || sg.y > sb.y * 1.25f || sg.z > sb.z * 1.25f + .008f)
                    fails.Add(look + ": " + sg.ToString("F3") + " m over a bare hand of " + sb.ToString("F3") + " (along, across, through)");
                var main = Main(new[] { rightRoot }); float round = main != null ? AngularShell.Roundness(MeshOf(main)) : 1;
                if (round > .15f) fails.Add(look + ": its main part is round (" + round.ToString("0.00") + ")");
            }
            Assert.IsEmpty(fails, string.Join("\n", fails));
        }

        /// <summary>Pauldrons and spaulders on a model: within 3 cm of the shoulder's own surface (resting on it, not floating),
        /// over the shoulder joint, and not round.</summary>
        [Test] public void Shoulder_plates_rest_on_the_shoulder_and_are_not_round()
        {
            Assume.That(ModelFigure.Available, "The character kits are in the project (Resources/Characters).");
            var db = new ItemDatabase(); var looks = Looks(); var fails = new List<string>();
            foreach (var look in ShoulderLooks)
            {
                var v = Figure(ActorLook.Warrior); var m = v.Model;
                v.ApplyGearIds(new[] { Item(db, looks, look, "shoulders", 13) }, db, looks);
                v.Preview(ActorPose.None, 0, 0);
                var body = new List<Vector3>(); body.AddRange(Points(m.Torso)); body.AddRange(Points(m.Arms));
                var main = Main(new[] { v.GearRoot(EquipSlot.Shoulders) });
                if (main == null) { fails.Add(look + ": nothing built"); continue; }
                var pts = Points(main); float nearest = float.MaxValue; Vector3 mid = Vector3.zero; int right = 0;
                foreach (var p in pts)
                {
                    float best = float.MaxValue; foreach (var q in body) best = Mathf.Min(best, (p - q).sqrMagnitude);
                    nearest = Mathf.Min(nearest, Mathf.Sqrt(best));
                    if ((p - m.UpperArmR.position).sqrMagnitude < (p - m.UpperArmL.position).sqrMagnitude) { mid += p; right++; }
                }
                if (nearest > .03f) fails.Add(look + ": floats " + nearest.ToString("0.000") + " m off the body");
                if (right == 0 || Vector3.Distance(mid / right, m.UpperArmR.position) > .12f) fails.Add(look + ": not over the right shoulder");
                float round = AngularShell.Roundness(MeshOf(main)); if (round > .15f) fails.Add(look + ": round (" + round.ToString("0.00") + ")");
            }
            Assert.IsEmpty(fails, string.Join("\n", fails));
        }

        /// <summary>The Modular Hero helmets (pinned out of another rig, perched and tilted) are out of the helm pool; the sentinel
        /// helm, which an Ash Rim drop wears, stays.</summary>
        [Test] public void The_pack_helms_that_perched_are_retired()
        {
            CollectionAssert.AreEqual(new[] { "Ashen_gth_equi_sentinel_helm" }, GearLooks.ModelHelms);
            foreach (var look in WardrobeCapture.HeadLooks) StringAssert.DoesNotContain("Headgear.", look);
        }
    }
}
