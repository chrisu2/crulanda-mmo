using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The training field's two dummies (GAME-ONLY; Chris, 2026-10-13: "put a few test dummies in town ... one for dps and one for heals",
    /// "put out in a field near town"). The look is built onto the session's own foe and friend (EncounterSession.Training), so the model,
    /// its nameplate and its click box are always in one place (the 13:27 frames: the foe's agent had snapped it metres from a look built
    /// here at the prop). training_dummy: the asset pack's straw dummy on its post. mending_dummy: the same, bound in pale bandages with a
    /// green sash, so the two read apart from across the field.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>A place in the training yard a sim may practise at (Chris, 2026-10-13: "also have sims wonder into the fighting arena
        /// (dummies) to practice and improve their dps"): what it strikes or shoots at (an aim point about chest high), where it stands to
        /// do it, and whether that is a butt for the bow and the bolt (across the yard) or a dummy for the blade (a step in front).</summary>
        public sealed class PracticeMark { public Transform aim; public Vector3 stand; public bool ranged; }
        /// <summary>The yard's dummies and butts (SimFigure's practice).</summary>
        public readonly List<PracticeMark> PracticeMarks = new List<PracticeMark>();
        void Mark(Transform t, Vector3 local, Vector3 standLocal, bool ranged)
        {
            var aim = new GameObject(ranged ? "Practice butt" : "Practice dummy").transform; aim.SetParent(t, false);
            var at = t.TransformPoint(local); at.y = HeightAt(at.x, at.z) + 1.25f; aim.position = at;
            var stand = t.TransformPoint(standLocal); stand.y = HeightAt(stand.x, stand.z);
            PracticeMarks.Add(new PracticeMark { aim = aim, stand = stand, ranged = ranged });
        }
        /// <summary>
        /// The sparring yard (Chris, 2026-10-13 ~13:40: "sparring area needs to resemble something like this", with pictures of a fenced
        /// yard of tents, banners, dummies, weapon racks and targets; "try the small props pack in downloads"). GAME-ONLY. A fenced ground
        /// <paramref name="size"/> across (local x) and deep (local z), its gate in the front fence (+z, toward the town): two tents at the
        /// back, banners at the gate and the corners, dummies and mannequins down the left, archery butts down the right, weapon stands and a
        /// peg rack, crates and a barrel by the right tent, and a healer's corner (a stool, a bucket, a basin) in front of it. The session's
        /// own training and mending dummies stand inside it (their own zone props). The Small Props Pack (RG Poly, CC0) gives the butts,
        /// the mannequins and the healer's things.
        /// </summary>
        void TrainingYard(Transform t, Vector2 size)
        {
            float hx = Mathf.Max(8, size.x) / 2, hz = Mathf.Max(6, size.y) / 2, gate = 2.6f;
            float G(float x, float z) { return LocalGround(t, x, z); }
            // A closed ring of plank panels (Chris, 14:20: "area is not closed in like the pictures i sent you"; the rail fence read as open):
            // the Megapack's fence panels (3.56 m long, 1.56 m high as made) fitted end to end, about 3 m apart, 1.3 m high, solid; the only
            // gap is the gate.
            int panel = 0;
            void Panels(float x0, float z0, float x1, float z1)
            {
                float dx = x1 - x0, dz = z1 - z0, len = Mathf.Sqrt(dx * dx + dz * dz); if (len < .5f) return;
                int n = Mathf.Max(1, Mathf.RoundToInt(len / 3f)); float seg = len / n, yaw = Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg;
                for (int i = 0; i < n; i++)
                {
                    float f = (i + .5f) / n, x = x0 + dx * f, z = z0 + dz * f;
                    ModelProp(t, panel++ % 2 == 0 ? "Megapack/Models/Fence_1_1" : "Megapack/Models/Fence_1_2", new Vector3(x, G(x, z), z), yaw, 1.3f, true, null, new Vector3(seg, 1.3f, .12f));
                }
            }
            Panels(-hx, -hz, hx, -hz);                       // back
            Panels(-hx, -hz, -hx, hz); Panels(hx, -hz, hx, hz);   // sides
            Panels(-hx, hz, -gate, hz); Panels(gate, hz, hx, hz); // front, either side of the gate
            // Two tents at the back, their open sides to the yard.
            GroundProp(t, "Megapack/Models/Tent_1", new Vector3(-hx + 3.4f, 0, -hz + 2.8f), 180, 3.0f, true);
            GroundProp(t, "Megapack/Models/Tent_1", new Vector3(hx - 3.4f, 0, -hz + 2.8f), 180, 3.0f, true);
            // Banners: either side of the gate and at the four corners.
            foreach (float x in new[] { -gate - .4f, gate + .4f }) GroundProp(t, "Fantasy/Banner_1", new Vector3(x, 0, hz + .3f), 0, 2.8f);
            foreach (var (x, z) in new[] { (-hx + .5f, -hz + .5f), (hx - .5f, -hz + .5f), (-hx + .5f, hz - .5f), (hx - .5f, hz - .5f) }) GroundProp(t, "Fantasy/Banner_2", new Vector3(x, 0, z), 0, 3.2f);
            // Pennants along the side fences, as in Chris's picture of a sparring yard (13:40).
            foreach (float side in new[] { -1f, 1f }) foreach (float z in new[] { -hz / 3, hz / 3 }) GroundProp(t, "Fantasy/Banner_2", new Vector3(side * (hx + .35f), 0, z), side < 0 ? 90 : -90, 3.0f);
            // Dummies and mannequins down the left, facing in.
            string[] dummies = { "Megapack/Models/Outpost/Dummy", "SmallProps/Manequin_1_A_001", "Fantasy/Dummy", "SmallProps/Manequin_2_A_002" };
            for (int i = 0; i < dummies.Length; i++)
            {
                GroundProp(t, dummies[i], new Vector3(-hx + 1.6f, 0, -1.6f + i * 2.2f), 90, dummies[i].StartsWith("SmallProps") ? 1.8f : 1.9f, true);
                Mark(t, new Vector3(-hx + 1.6f, 0, -1.6f + i * 2.2f), new Vector3(-hx + 3.0f, 0, -1.6f + i * 2.2f), false);   // a sim with a blade a step in front
            }
            // Archery butts down the right, facing in.
            string[] butts = { "SmallProps/PracticeTarget_1_A", "SmallProps/PracticeTarget_2_A", "Megapack/Models/Arrow Target", "SmallProps/PracticeTarget_1_B" };
            // Each faces into the yard, its back to the fence (Chris, 14:25). Settled from the frames and Chris's three reports
            // (2026-10-14, "still a couple are backwards", "now three are backward"; the butts' turns logged by FaceLane): the Small
            // Props targets face the lane at 180, the second kind too (its paint is on the far side from its legs), the Megapack's at 90.
            for (int i = 0; i < butts.Length; i++)
            {
                GroundProp(t, butts[i], new Vector3(hx - 1.4f, 0, 1.0f + i * 1.9f), butts[i].StartsWith("SmallProps") ? 180 : 90, 1.7f, true);   // their lanes clear of the healer's corner
                Mark(t, new Vector3(hx - 1.4f, 0, 1.0f + i * 1.9f), new Vector3(hx - 9.4f, 0, 1.0f + i * 1.9f), true);   // a sim with a bow or a spell across the yard
            }
            // Weapon stands by the left tent and against the back fence (the Fantasy peg rack is a wall board 35 cm high: it came in lying flat
            // and seventeen times too big, the 14:12 frame's "giant beams").
            // Standing on the ground a step in from the fence (Chris, 15:20: "weapon wracks sit on the ground. not hanging on fence").
            GroundProp(t, "Fantasy/WeaponStand", new Vector3(-hx + 2.2f, 0, -hz + 5.6f), 90, 1.6f, true);
            GroundProp(t, "Fantasy/WeaponStand", new Vector3(-hx + 6.4f, 0, -hz + 1.9f), 0, 1.6f, true);
            GroundProp(t, "Fantasy/WeaponStand", new Vector3(0, 0, -hz + 1.9f), 0, 1.6f, true);
            // Stores by the right tent.
            Crate(t, new Vector3(hx - 1.1f, G(hx - 1.1f, -hz + 1.1f) + .41f, -hz + 1.1f), .82f, 20);
            Crate(t, new Vector3(hx - 2.0f, G(hx - 2.0f, -hz + 1.0f) + .41f, -hz + 1.0f), .82f, -12);
            Barrel(t, new Vector3(hx - 1.0f, G(hx - 1.0f, -hz + 2.2f), -hz + 2.2f), 1, 30);
            Solid(t, new Vector3(hx - 1.5f, .6f, -hz + 1.5f), new Vector3(2.2f, 1.2f, 2.2f));
            // The healer's corner in front of the right tent.
            GroundProp(t, "SmallProps/Stool_A", new Vector3(hx - 5.4f, 0, -hz + 5.6f), 20, .45f);
            GroundProp(t, "SmallProps/Bucket_1_A", new Vector3(hx - 4.6f, 0, -hz + 5.2f), 0, .35f);
            GroundProp(t, "SmallProps/Basin_A", new Vector3(hx - 6.4f, 0, -hz + 5.8f), 0, .3f);
        }
        /// <summary>
        /// Turns a standing target (an archery butt) about the vertical so its face looks at <paramref name="toward"/>, from its own
        /// shape: the face is the upper part's thinnest horizontal spread; a tripod butt stands on two legs splayed under its face
        /// with a short prop behind, so the lower part's middle lies in front of the disc (the Small Props targets, read from their
        /// meshes 2026-10-14: the legs' middle 3-12 cm out on the painted side; turned the other way they all faced the fence, Chris,
        /// "now three are backward"). Left as it is when its meshes cannot be read or the legs stand under the disc (the Megapack's).
        /// </summary>
        public static bool FaceLane(GameObject go, Vector3 toward)
        {
            var pts = new List<Vector3>();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                var m = mf.sharedMesh; if (m == null || !m.isReadable) continue;
                var vs = m.vertices; var to = mf.transform.localToWorldMatrix; int step = Mathf.Max(1, vs.Length / 600);
                for (int i = 0; i < vs.Length; i += step) pts.Add(to.MultiplyPoint3x4(vs[i]));
            }
            if (pts.Count < 12) return false;
            float lo = float.MaxValue, hi = float.MinValue; foreach (var p in pts) { lo = Mathf.Min(lo, p.y); hi = Mathf.Max(hi, p.y); }
            var up = pts.FindAll(p => p.y > lo + (hi - lo) * .55f); var low = pts.FindAll(p => p.y < lo + (hi - lo) * .3f);
            if (up.Count < 4 || low.Count < 4) return false;
            Vector3 Mid(List<Vector3> l) { var c = Vector3.zero; foreach (var p in l) c += p; return c / l.Count; }
            var cu = Mid(up); var cl = Mid(low);
            // The face's normal: the horizontal direction the upper part spreads least along (a 2 x 2 covariance's smaller axis).
            float xx = 0, zz = 0, xz = 0; foreach (var p in up) { float dx = p.x - cu.x, dz = p.z - cu.z; xx += dx * dx; zz += dz * dz; xz += dx * dz; }
            float angle = .5f * Mathf.Atan2(2 * xz, xx - zz);   // the major axis
            var n = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle));   // at right angles to it
            float behind = Vector3.Dot(cl - cu, n);
            if (Mathf.Abs(behind) < .02f) return false;
            var front = behind > 0 ? n : -n;   // the legs' side
            var want = toward - cu; want.y = 0; if (want.sqrMagnitude < .01f) return false;
            float turn = Vector3.SignedAngle(front, want.normalized, Vector3.up);
            go.transform.RotateAround(new Vector3(cu.x, go.transform.position.y, cu.z), Vector3.up, turn);
            Debug.Log("Butt " + go.name + ": legs " + (behind * 100).ToString("0") + " cm off the disc, turned " + turn.ToString("0") + " degrees to its lane.");
            return true;
        }
        /// <summary>The dummy's look under <paramref name="t"/>, its feet at <paramref name="foot"/> (local).</summary>
        public void TrainingDummyLook(Transform t, Vector3 foot, bool mending)
        {
            var straw = Tint(art.hay, new Color(.78f, .66f, .4f)); var wood = Tint(art.timber, new Color(.38f, .27f, .17f));
            if (ModelProp(t, "Megapack/Models/Outpost/Dummy", foot, 0, 1.9f) == null)
            {
                // Without the pack: a post, a straw body and a cross-arm.
                Part(PrimitiveType.Cylinder, t, foot + new Vector3(0, .95f, 0), new Vector3(.12f, .95f, .12f), wood);
                Part(PrimitiveType.Capsule, t, foot + new Vector3(0, 1.25f, 0), new Vector3(.5f, .45f, .4f), straw);
                Part(PrimitiveType.Sphere, t, foot + new Vector3(0, 1.78f, 0), new Vector3(.32f, .32f, .3f), straw);
                Part(PrimitiveType.Cube, t, foot + new Vector3(0, 1.45f, 0), new Vector3(1.2f, .09f, .09f), wood);
            }
            if (!mending) return;
            var bandage = Tint(art.cloth, new Color(.9f, .88f, .8f)); var sash = Tint(art.cloth, new Color(.25f, .55f, .3f));
            foreach (float y in new[] { 1.05f, 1.3f, 1.55f }) Part(PrimitiveType.Cylinder, t, foot + new Vector3(0, y, 0), new Vector3(.56f, .04f, .48f), bandage);
            Part(PrimitiveType.Cube, t, foot + new Vector3(0, 1.3f, 0), new Vector3(.6f, .7f, .06f), sash, Quaternion.Euler(0, 0, 38));
        }
    }
}
