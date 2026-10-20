#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// The whole orcs move in play (2026-10-13, Chris playing the orc Shaman: "the only thing on orc that move is belt, staff and teeth",
    /// "orc has no walking animations"): an orc Shaman and an axe orc, built as the game builds them, walked across the ground for two
    /// seconds; their arms and legs swing (each hand and foot moves against the body), and their avatar is not a human one.
    /// And they hold what they carry (2026-10-14, Chris playing the orc Shaman: its arms hanging at its sides while its staff floated by the
    /// ground in front of it, or stuck out sideways): standing, walking, running and casting or striking, the Shaman's staff stays in its
    /// right hand and its eyes in its head, and the axe orc's axe in its right hand, as the packs' own poses hold them.
    /// </summary>
    public class OrcMotionTests
    {
        readonly List<GameObject> made = new List<GameObject>();
        [TearDown] public void Off() { foreach (var g in made) if (g != null) Object.Destroy(g); made.Clear(); }

        IEnumerator Walk(ActorLook look, string kind, bool orc = true, string name = null)
        {
            Assume.That(ModelBeast.Available(kind), kind + " is in the build");
            var go = new GameObject(name ?? "Orc motion " + kind); new GameObject("Body").transform.SetParent(go.transform, false); made.Add(go);
            var v = ActorVisual.Attach(go, look, 0, false, null, orc);
            Assert.NotNull(v.BeastModel, "a model"); Assert.AreEqual(kind, v.BeastModel.Kind);
            var anim = v.BeastModel.Animator; Assert.IsFalse(anim.avatar != null && anim.avatar.isHuman, kind + ": not a human avatar");
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // no camera in a test: animate unseen
            var all = v.BeastModel.Model.GetComponentsInChildren<Transform>(); var start = all.Select(t => (t.localPosition, t.localRotation)).ToArray();
            var limbs = v.BeastModel.Model.GetComponentsInChildren<Transform>().Where(t => { var n = t.name.ToLowerInvariant(); return (n.Contains("hand") || n.Contains("wrist") || n.Contains("foot") || n.Contains("ankle")) && !n.Contains("index") && !n.Contains("thumb") && !n.Contains("middle") && !n.Contains("ring") && !n.Contains("pinky") && !n.Contains("ik"); }).ToList();
            Assert.GreaterOrEqual(limbs.Count, 2, kind + ": hands and feet (" + string.Join(", ", v.BeastModel.Model.GetComponentsInChildren<Transform>().Select(t => t.name).Take(60)) + ")");
            var body = go.transform; var first = limbs.Select(t => body.InverseTransformPoint(t.position)).ToList(); float most = 0;
            for (float t = 0; t < 2; t += Time.deltaTime)
            {
                go.transform.position += Vector3.forward * 3 * Time.deltaTime;   // walking: the visual reads its pace from how it moves
                yield return null;
                for (int i = 0; i < limbs.Count; i++) most = Mathf.Max(most, Vector3.Distance(first[i], body.InverseTransformPoint(limbs[i].position)));
            }
            var moved = all.Where((t, i) => Vector3.Distance(t.localPosition, start[i].localPosition) > .001f || Quaternion.Angle(t.localRotation, start[i].localRotation) > 2).Select(t => t.name).ToList();
            Assert.Greater(most, .08f, kind + ": its hands and feet swing as it walks (" + most.ToString("0.000") + " m; " + moved.Count + " of " + all.Length + " bones moved: " + string.Join(", ", moved.Take(14)) + "; avatar " + (anim.avatar != null ? anim.avatar.name + " human " + anim.avatar.isHuman : "none") + ")");
        }
        [UnityTest] public IEnumerator An_orc_shaman_walks_on_its_own_legs() { yield return Walk(ActorLook.Druid, "OrcShaman"); }
        [UnityTest] public IEnumerator An_axe_orc_walks_on_its_own_legs() { yield return Walk(ActorLook.Warrior, "OrcBrute"); }
        [UnityTest] public IEnumerator The_bone_thrall_walks_on_its_own_legs() { yield return Walk(ActorLook.Skeleton, "UndeadUnit", false, "Bone thrall"); }

        // ------------------------------------------------------------------------------------------------- what it holds
        /// <summary>
        /// How far the staff may stray from its grip (metres, at the orc's size in the game). The Orc Shaman's staff is not skinned to its
        /// hand: a rigid mesh on a control of its own (locomotion/_Hip01/_Hand01), held in the hand only while the body and that control play
        /// the same frames. In the pack's own baked take (the model file's, all 651 frames) the staff stays within 0.074 m of its grip at
        /// frame 85 (mid-idle), the most in Hit, and within 0.022-0.030 m through idle, walk and run (orc-probe, 2026-10-14); the margin
        /// over that is for the cross-fades between clips. Taken as one 24 s master take per clip, it strayed 0.3-1.9 m.
        /// </summary>
        public const float StaffSlack = .1f;
        /// <summary>How far the Orc Shaman's eyes may stray from their place in its head: rigid too, on the _Head control; the pack's own
        /// take holds them within 0.045 m (Hit; 0.015-0.024 m in idle, walk and run). Taken whole, they strayed up to 2.4 m.</summary>
        public const float EyesSlack = .06f;

        /// <summary>A piece of the orc measured in the frame of a bone: its mesh's centre, a rigid mesh's by its own transform, a skinned one's
        /// through the bone that carries most of it (the axe orc's axe is skinned whole to Axe1_R).</summary>
        sealed class Piece
        {
            readonly Transform bone, carrier; readonly Renderer r; readonly Vector3 centre; readonly Matrix4x4 bind;
            public Piece(Transform root, string boneName, string rendererName)
            {
                bone = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == boneName);
                r = root.GetComponentsInChildren<Renderer>(true).FirstOrDefault(x => x.name == rendererName);
                Assert.NotNull(bone, root.name + ": its " + boneName); Assert.NotNull(r, root.name + ": its " + rendererName);
                var sk = r as SkinnedMeshRenderer; var mesh = sk != null ? sk.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                Assert.IsTrue(mesh != null && mesh.isReadable, rendererName + ": a readable mesh");
                var verts = mesh.vertices; Assert.Greater(verts.Length, 0, rendererName + ": vertices");
                foreach (var p in verts) centre += p; centre /= verts.Length;
                if (sk != null)
                {
                    var weight = new float[sk.bones.Length];
                    foreach (var w in mesh.boneWeights) { weight[w.boneIndex0] += w.weight0; weight[w.boneIndex1] += w.weight1; weight[w.boneIndex2] += w.weight2; weight[w.boneIndex3] += w.weight3; }
                    int most = 0; for (int i = 1; i < weight.Length; i++) if (weight[i] > weight[most]) most = i;
                    carrier = sk.bones[most]; bind = mesh.bindposes[most];
                }
            }
            Vector3 World { get { return carrier != null ? carrier.localToWorldMatrix.MultiplyPoint3x4(bind.MultiplyPoint3x4(centre)) : r.transform.localToWorldMatrix.MultiplyPoint3x4(centre); } }
            /// <summary>Its centre in the bone's frame (the same at any scale of the whole model).</summary>
            public Vector3 Local { get { return bone.InverseTransformPoint(World); } }
            /// <summary>How far it is from where <paramref name="grip"/> puts it, in metres at the model's size.</summary>
            public float Off(Vector3 grip) { return bone.TransformVector(Local - grip).magnitude; }
            public Vector3 Position { get { return World; } }
            public Transform Bone { get { return bone; } }
        }

        /// <summary>
        /// The orc built as the game builds it, its piece's grip taken from the pack's own pose (<paramref name="gripClip"/> at
        /// <paramref name="gripTime"/>, sampled on the file as it comes), then: standing, walking, running, and a cast (or a blow) played over its
        /// stand; every frame, how far the piece (and the eyes) strayed from that grip. The one-shot must be over within its own length.
        /// </summary>
        IEnumerator Holds(ActorLook look, string kind, string handName, string pieceName, string gripClip, float gripTime, string headName, string eyesName, bool cast, float shotWithin)
        {
            Assume.That(ModelBeast.Available(kind), kind + " is in the build");
            var reference = ModelBeast.Clip(kind, gripClip); Assert.NotNull(reference, kind + ": the pack's own pose (clip " + gripClip + ")");
            var raw = Object.Instantiate(Resources.Load<GameObject>("Creatures/" + kind)); raw.name = kind + " (as it comes)"; made.Add(raw);
            reference.SampleAnimation(raw, gripTime);
            var grip = new Piece(raw.transform, handName, pieceName).Local;
            var eyesGrip = eyesName != null ? new Piece(raw.transform, headName, eyesName).Local : Vector3.zero;
            raw.SetActive(false);

            var go = new GameObject("Orc holds " + kind); new GameObject("Body").transform.SetParent(go.transform, false); made.Add(go);
            var v = ActorVisual.Attach(go, look, 0, false, null, true);
            Assert.NotNull(v.BeastModel, "a model"); Assert.AreEqual(kind, v.BeastModel.Kind);
            v.BeastModel.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // no camera in a test: animate unseen
            var piece = new Piece(v.BeastModel.Model, handName, pieceName);
            var eyes = eyesName != null ? new Piece(v.BeastModel.Model, headName, eyesName) : null;
            var worst = new List<(string phase, float off, float eyes, float handY, float pieceY)>();
            float most = 0, eyesMost = 0, handY = 0, pieceY = 0;
            void Measure()
            {
                float ground = go.transform.position.y - 1;   // (ActorVisual stands a figure's feet a metre under its root)
                float o = piece.Off(grip); if (o > most) { most = o; handY = piece.Bone.position.y - ground; pieceY = piece.Position.y - ground; }
                if (eyes != null) eyesMost = Mathf.Max(eyesMost, eyes.Off(eyesGrip));
            }
            void Close(string phase) { worst.Add((phase, most, eyesMost, handY, pieceY)); most = 0; eyesMost = 0; }
            yield return null; yield return null;   // the graph has posed it

            foreach (var (phase, seconds, pace) in new[] { ("standing", 2.5f, 0f), ("walking", 2.5f, 1.4f), ("running", 2f, 5f) })
            {
                for (float t = 0; t < seconds; t += Time.deltaTime)
                {
                    go.transform.position += Vector3.forward * pace * Time.deltaTime;   // the visual reads its pace from how it moves
                    yield return null;
                    if (t > .3f || phase == "standing") Measure();   // (each phase but the first measured once the pace has eased in)
                }
                Close(phase);
            }
            // Standing again, a cast let fly (the Shaman's CastSpell) or a blow struck (the axe orc's swing).
            for (float t = 0; t < .6f; t += Time.deltaTime) yield return null;
            if (cast) v.CastRelease(); else v.Strike();
            Assert.IsTrue(v.BeastModel.Busy, kind + ": its " + (cast ? "cast" : "blow") + " plays");
            float over = -1;
            for (float t = 0; t < shotWithin + 1; t += Time.deltaTime)
            {
                yield return null; Measure();
                if (over < 0 && !v.BeastModel.Busy) over = t;
            }
            Close(cast ? "casting" : "striking");
            string report = kind + ": " + string.Join("; ", worst.Select(w => w.phase + " " + pieceName + " off its grip at most " + w.off.ToString("0.000") + " m (then the hand " + w.handY.ToString("0.00") + " m off the ground, its centre " + w.pieceY.ToString("0.00") + " m)"
                + (eyesName != null ? ", eyes off the head " + w.eyes.ToString("0.000") + " m" : ""))) + "; the " + (cast ? "cast" : "blow") + " over in " + (over < 0 ? "more than " + (shotWithin + 1).ToString("0.0") : over.ToString("0.00")) + " s";
            Debug.Log("ORC_HOLDS " + report);
            foreach (var w in worst)
            {
                Assert.Less(w.off, StaffSlack, kind + " " + w.phase + ": its " + pieceName + " stays in its hand (" + report + ")");
                if (eyesName != null) Assert.Less(w.eyes, EyesSlack, kind + " " + w.phase + ": its eyes stay in its head (" + report + ")");
            }
            Assert.IsTrue(over >= 0 && over <= shotWithin, kind + ": the " + (cast ? "cast" : "blow") + " is over within its clip's length, " + shotWithin.ToString("0.00") + " s (" + report + ")");
        }
        /// <summary>The Orc Shaman's staff, against its grip in the pack's own baked take at frame 85 (mid-idle); its cast is CastSpell, 64
        /// frames at 24 fps.</summary>
        [UnityTest] public IEnumerator An_orc_shamans_staff_stays_in_its_hand()
        { yield return Holds(ActorLook.Druid, "OrcShaman", "Character1_RightHand", "polySurface238", "Take 001", 85 / 24f, "Character1_Head", "_Eyes", true, 64 / 24f + .3f); }
        /// <summary>The axe orc's axe (its handle, skinned whole to Axe1_R under Hand_R), against its grip at the start of the pack's idle;
        /// its blows are 2.48 s.</summary>
        [UnityTest] public IEnumerator An_axe_orcs_axe_stays_in_its_hand()
        { yield return Holds(ActorLook.Warrior, "OrcBrute", "Hand_R", "Cylinder_low", "Idle", 0, null, null, false, 2.5f + .3f); }

        /// <summary>The Orc Shaman's clips are the pack's own (each file holds the pack's whole 27 s master take; the pack cuts its clip out
        /// by frames, at 24 fps): taken whole, every clip was 23.8 s, a cast held the orc for 24 s and a death took 23 s to fall.</summary>
        [Test] public void The_orc_shamans_clips_are_its_packs_own()
        {
            Assume.That(ModelBeast.Available("OrcShaman"), "the Orc Shaman is in the build");
            var frames = new Dictionary<string, int> { ["idle"] = 21, ["walk"] = 27, ["run"] = 18, ["attack"] = 21, ["attack2"] = 32, ["attack3"] = 16, ["cast"] = 64, ["spell"] = 34, ["hitL"] = 20, ["death"] = 22, ["roar"] = 80, ["block"] = 22 };
            foreach (var kv in frames)
            {
                var c = ModelBeast.SlotClip("OrcShaman", kv.Key); Assert.NotNull(c, "its " + kv.Key);
                Assert.AreEqual(kv.Value / 24f, c.length, .021f, "its " + kv.Key + " (" + c.name + ") is the pack's " + kv.Value + " frames");
            }
            foreach (var slot in new[] { "idle", "walk", "run" }) Assert.IsTrue(ModelBeast.SlotClip("OrcShaman", slot).isLooping, "its " + slot + " loops");
        }
    }
}
#endif
