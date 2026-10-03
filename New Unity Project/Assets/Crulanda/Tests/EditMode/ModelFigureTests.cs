using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// The modelled people (playtest note 12; ModelFigure, ActorVisual.Model.cs): every person's look builds a model with its
    /// frames; the frames stand on the model's joints in any pose; held gear is in the hand and worn armour on the head, chest
    /// and limbs' frames; chest armour dyes the shirt and gives it back; a barbute hides the hair; women are built as women; a
    /// swimmer's face rides over the water and the dead lie on the ground; with models off, the smooth figure is built.
    /// Built in edit mode and posed with ActorVisual.Preview (no scene, no save).
    /// </summary>
    public class ModelFigureTests
    {
        readonly List<GameObject> made = new List<GameObject>();
        bool models;
        [SetUp] public void On() { models = ActorVisual.Models; ActorVisual.Models = true; }
        [TearDown] public void TearDown() { foreach (var g in made) if (g != null) Object.DestroyImmediate(g); made.Clear(); ActorVisual.Models = models; }

        ActorVisual Figure(ActorLook look, int variant = 0, string role = null, string name = "Model test figure")
        {
            var go = new GameObject(name); new GameObject("Body").transform.SetParent(go.transform, false); go.transform.position = new Vector3(made.Count * 3, 1, 0); made.Add(go);
            return ActorVisual.Attach(go, look, variant, false, role);
        }
        static GearLooks Looks() { return GearLooks.Parse(Resources.Load<TextAsset>("Gear/looks").text); }
        static string Piece(ItemDatabase db, string slot, int level, int quality, string piece)
        {
            for (int seed = 0; seed < 5000; seed++)
            {
                string id = ItemDatabase.GearId(slot, level, quality, seed);
                if (GearLooks.TrySplitGenerated(db.Get(id), out _, out var p, out _, out _) && p == piece) return id;
            }
            Assert.Fail("No generated " + piece + "."); return null;
        }
        static readonly string[] Frames = { "Arm L", "Arm L/Forearm L", "Arm L/Forearm L/Hand L", "Arm R", "Arm R/Forearm R", "Arm R/Forearm R/Hand R",
            "Leg L", "Leg L/Shin L", "Leg L/Shin L/Foot L", "Leg R", "Leg R/Shin R", "Leg R/Shin R/Foot R", "Head frame", "Chest frame", "Hips frame" };

        [Test] public void Every_person_is_a_model_with_its_frames()
        {
            Assume.That(ModelFigure.Available, "The character kits are in the project (Resources/Characters).");
            var people = new (ActorLook, string)[] { (ActorLook.Warrior, null), (ActorLook.Druid, null), (ActorLook.Healer, null), (ActorLook.Collector, null), (ActorLook.Warden, null),
                (ActorLook.Sentry, null), (ActorLook.Outrider, null), (ActorLook.Hollow, null), (ActorLook.Cultist, null), (ActorLook.Deserter, null), (ActorLook.BanditKing, null),
                (ActorLook.Villager, "blacksmith"), (ActorLook.Villager, "henwife"), (ActorLook.Villager, "merchant"), (ActorLook.Villager, "elder"), (ActorLook.Villager, "child"), (ActorLook.Villager, null) };
            foreach (var (look, role) in people)
            {
                var v = Figure(look, 3, role); string what = look + (role != null ? " (" + role + ")" : "");
                Assert.NotNull(v.Model, what + " is a model.");
                foreach (var f in Frames) Assert.NotNull(v.transform.Find("Body/" + f), what + ": the frame " + f + ".");
                var m = v.Model;
                foreach (var r in new[] { m.Head, m.Torso, m.Arms, m.Legs, m.Feet }) { Assert.NotNull(r, what + ": every piece."); Assert.IsTrue(r.sharedMaterials.Length > 0 && System.Array.TrueForAll(r.sharedMaterials, x => x != null), what + ": " + r.name + " has its materials."); }
                Assert.IsNull(v.transform.Find("Body/Smooth chest"), what + ": no smooth body under it.");
                Assert.IsTrue(m.Animator.avatar != null && m.Animator.avatar.isHuman, what + ": a humanoid avatar for the clips.");
            }
            ActorVisual.Models = false;
            var smooth = Figure(ActorLook.Villager, 3, "farmer");
            Assert.IsNull(smooth.Model, "Models off: the smooth figure.");
            Assert.NotNull(smooth.transform.Find("Body/Smooth chest"));
        }

        [Test] public void Frames_stand_on_the_joints_in_every_pose()
        {
            Assume.That(ModelFigure.Available);
            var v = Figure(ActorLook.Villager, 23, "farmer", "Garet Moss"); var m = v.Model; var body = v.transform.Find("Body");
            foreach (var (pose, walk, t) in new[] { (ActorPose.None, 0f, .4f), (ActorPose.None, 1.5f, .3f), (ActorPose.Sit, 0f, .5f), (ActorPose.Gather, 0f, 2.5f), (ActorPose.Hammer, 0f, .45f), (ActorPose.Sneak, 1.2f, .5f) })
            {
                v.Preview(pose, walk, t); string what = pose + (walk > 0 ? " moving" : "");
                foreach (var (frame, bone, tol) in new[] { ("Arm R", m.UpperArmR, .002f), ("Arm R/Forearm R", m.LowerArmR, .03f), ("Arm R/Forearm R/Hand R", m.HandR, .04f), ("Arm L/Forearm L/Hand L", m.HandL, .04f),
                    ("Leg L", m.UpperLegL, .002f), ("Leg L/Shin L", m.LowerLegL, .04f), ("Leg L/Shin L/Foot L", m.FootL, .05f), ("Leg R/Shin R/Foot R", m.FootR, .05f) })
                    Assert.Less(Vector3.Distance(body.Find(frame).position, bone.position), tol, what + ": " + frame + " on its joint.");
                // The frames point down their bones: the forearm's -Y runs from the elbow to the wrist.
                var fore = body.Find("Arm R/Forearm R"); var along = (m.HandR.position - m.LowerArmR.position).normalized;
                Assert.Greater(Vector3.Dot(-fore.up, along), .98f, what + ": the forearm frame runs down the forearm.");
            }
            // The head and chest frames ride their bones.
            v.Preview(ActorPose.None, 0, .4f); var head = body.Find("Head frame"); var h0 = head.position - m.HeadBone.position;
            v.Preview(ActorPose.Sit, 0, .5f); Assert.Less(Vector3.Distance(head.position - m.HeadBone.position, h0), .1f, "The head frame rides the head (sitting it goes down with it).");
            Assert.Less(m.HeadBone.position.y, v.transform.position.y + .3f, "Sitting, the head is low (standing it is .6 higher).");
        }

        [Test] public void Gear_sits_in_the_hand_and_on_the_frames()
        {
            Assume.That(ModelFigure.Available);
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure(ActorLook.Warrior); var m = v.Model;
            string blade = Piece(db, "mainhand", 5, 2, "Blade"), shield = Piece(db, "offhand", 5, 2, "Shield");
            v.ApplyGearIds(new[] { blade, shield, Piece(db, "head", 7, 3, "Cap"), Piece(db, "chest", 7, 3, "Hauberk"), Piece(db, "hands", 7, 3, "Gauntlets"), Piece(db, "legs", 7, 3, "Greaves"), Piece(db, "feet", 7, 3, "Sabatons") }, db, looks);
            v.Preview(ActorPose.None, 0, .4f);
            Assert.AreSame(v.RightArm.Find("Forearm R"), v.GearRoot(EquipSlot.MainHand).parent, "The blade follows the right forearm.");
            Assert.Less(Vector3.Distance(v.GearRoot(EquipSlot.MainHand).position, m.HandR.position), .14f, "The grip is in the right hand.");
            Assert.AreEqual("Head frame", v.GearRoot(EquipSlot.Head).parent.name); Assert.AreEqual("Chest frame", v.GearRoot(EquipSlot.Chest).parent.name);
            Assert.Less(Vector3.Distance(v.GearRoot(EquipSlot.Head).TransformPoint(new Vector3(0, .8f, 0)), m.HeadBone.position + Vector3.up * .12f), .1f, "The cap is on the model's head.");
            var hands = v.GearLimbRoots(EquipSlot.Hands); Assert.AreEqual(2, hands.Count);
            CollectionAssert.AreEquivalent(new[] { "Arm L", "Arm R" }, new[] { hands[0].parent.name, hands[1].parent.name });
            foreach (var r in v.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (r.name == "Gear part")
                {
                    Assert.AreEqual(m.Cloud.bones.Length, r.bones.Length, "Worn armour is skinned to the model's own bones.");
                    Assert.IsTrue(System.Array.TrueForAll(r.bones, b => b != null && b.IsChildOf(m.Model)), "Every one of them the model's.");
                }
            Assert.AreEqual(0, v.ClassKitParts, "Gear drives it: the class kit (the outfit's pauldron, the sword and shield) is gone.");
        }

        [Test] public void Armour_dyes_the_clothes_and_gives_them_back()
        {
            Assume.That(ModelFigure.Available);
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure(ActorLook.Villager, 23, "farmer", "Garet Moss"); var m = v.Model;
            var torso = m.Torso.sharedMaterials; var arms = m.Arms.sharedMaterials;
            v.ApplyGearIds(new[] { Piece(db, "chest", 5, 2, "Tunic"), Piece(db, "hands", 5, 2, "Gloves") }, db, looks);
            Assert.AreNotEqual(torso[0], m.Torso.sharedMaterials[0], "The tunic dyes the shirt.");
            Assert.AreNotEqual(arms, m.Arms.sharedMaterials, "The sleeves and the hands take the gear's colours.");
            v.ApplyGearIds(new string[0], db, looks);
            CollectionAssert.AreEqual(torso, m.Torso.sharedMaterials, "Off: the shirt is its own again.");
            CollectionAssert.AreEqual(arms, m.Arms.sharedMaterials, "And the sleeves and hands.");
            var hood = Piece(db, "head", 5, 2, "Hood");
            Assert.IsTrue(v.HairShowing);
            v.ApplyGearIds(new[] { hood }, db, looks); Assert.IsFalse(v.HairShowing, "A hood hides the hair.");
            v.ApplyGearIds(new string[0], db, looks); Assert.IsTrue(v.HairShowing, "Bare-headed again.");
        }

        [Test] public void Armour_follows_the_form_and_hats_sit_on_the_head()
        {
            Assume.That(ModelFigure.Available);
            var db = new ItemDatabase(); var looks = Looks(); var v = Figure(ActorLook.Warrior); var m = v.Model;
            v.ApplyGearIds(new[] { Piece(db, "chest", 7, 3, "Hauberk"), Piece(db, "hands", 7, 3, "Gauntlets"), Piece(db, "legs", 7, 3, "Greaves") }, db, looks);
            v.Preview(ActorPose.None, 0, .4f);
            // The hauberk hugs the torso: its shell (the chest slot's skinned parts) stands within a hand of the model's chest.
            foreach (var r in v.GearRoot(EquipSlot.Chest).GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var baked = new Mesh(); r.BakeMesh(baked, true); var w = r.transform.localToWorldMatrix; float far = 0;
                foreach (var p in baked.vertices) { var q = w.MultiplyPoint3x4(p); if (q.y > m.Hips.position.y && q.y < m.Neck.position.y) far = Mathf.Max(far, new Vector2(q.x - m.Chest.position.x, q.z - m.Chest.position.z).magnitude); }
                Assert.Less(far, .32f, r.sharedMesh.name + ": the chest armour stands within 32 cm of the spine (the old chunky shell stood off it).");
            }
            // A glove rides the hand: skinned to the hand's bones, it goes where the hand goes.
            var hands = v.GearLimbRoots(EquipSlot.Hands); Assert.AreEqual(2, hands.Count);
            bool onHand = false;
            foreach (var r in hands[0].GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach (var bw in r.sharedMesh.boneWeights) { var b = r.bones[bw.boneIndex0]; if (b == m.HandL || b == m.HandR || b.IsChildOf(m.HandL) || b.IsChildOf(m.HandR)) { onHand = true; break; } }
            Assert.IsTrue(onHand, "A gauntlet's weights are on the hand's bones.");
            // Hats: the farmer's brim sits between the brows and the crown of the head, not perched on top.
            var farmer = Figure(ActorLook.Villager, 23, "farmer", "Garet Moss"); var fm = farmer.Model;
            var hat = farmer.transform.Find("Body/Head frame/Hat"); Assert.NotNull(hat, "The farmer's hat is fitted (Body/Head frame/Hat).");
            float brim = float.MaxValue; foreach (var r in hat.GetComponentsInChildren<Renderer>()) brim = Mathf.Min(brim, r.bounds.min.y);
            float eyes = fm.HeadBone.position.y + .1f;
            Assert.That(brim - eyes, Is.InRange(.0f, .1f), "The brim sits just above the brows.");
            // A cap hugs the head: the smith's skullcap is a shell of his head, no wider than a head and a half.
            var smith = Figure(ActorLook.Villager, 3, "blacksmith", "Brannoc Vell");
            Renderer cap = null; foreach (var r in smith.transform.Find("Body/Head frame").GetComponentsInChildren<MeshRenderer>()) if (r.GetComponent<MeshFilter>().sharedMesh.name == "Cap (fitted)") cap = r;
            Assert.NotNull(cap, "The skullcap is made over as a fitted cap.");
            Assert.Less(cap.bounds.size.x, .26f, "Hugging the head (a head is about 16 cm across).");
        }

        [Test] public void Women_swimmers_and_the_dead()
        {
            Assume.That(ModelFigure.Available);
            var hen = Figure(ActorLook.Villager, 40, "henwife", "Hedda Thorne"); StringAssert.Contains("Female", hen.Model.Torso.name, "A hen-wife is a woman.");
            var smith = Figure(ActorLook.Villager, 3, "blacksmith", "Brannoc Vell"); StringAssert.Contains("Male", smith.Model.Torso.name);
            var druid = Figure(ActorLook.Druid); StringAssert.Contains("Female", druid.Model.Torso.name, "The Druid is a woman (her hood, her staff).");
            Assert.NotNull(druid.Model.Hood, "Her hood.");
            var swimmer = Figure(ActorLook.Warrior); var body = swimmer.transform.Find("Body");
            swimmer.Preview(ActorPose.Swim, 1.6f, .4f);
            Assert.That(body.InverseTransformPoint(swimmer.Model.HeadBone.position).y + body.localPosition.y, Is.InRange(.25f, .5f), "Swimming, the face rides just over the water (.3 over the actor's middle).");
            var dead = Figure(ActorLook.Deserter); dead.PreviewDead();
            Assert.Less(dead.Model.HeadBone.position.y, dead.transform.position.y - .55f, "Dead, the head is on the ground.");
        }
    }
}
