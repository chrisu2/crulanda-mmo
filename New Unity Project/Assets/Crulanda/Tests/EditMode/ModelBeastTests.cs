using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// The animals as models (2026-10-03; ModelBeast, ActorVisual.Beasts.cs, CritterBody): the pack's wolf, stag and deer are in the
    /// build with the clips the game plays; a wolf, an ash hound, a stag and a doe are built as models standing on the ground at their
    /// heights, facing forward; an ash hound's eyes burn; the stag's antlers stand above its head; the dead lie on the ground; game
    /// deer wear the stag or the deer; with models off, the old bodies are built. Built in edit mode (no scene, no save).
    /// </summary>
    public class ModelBeastTests
    {
        readonly List<GameObject> made = new List<GameObject>();
        bool models, beasts;
        [SetUp] public void On() { models = ActorVisual.Models; beasts = ModelBeast.Enabled; ActorVisual.Models = true; ModelBeast.Enabled = true; }
        [TearDown] public void TearDown() { foreach (var g in made) if (g != null) Object.DestroyImmediate(g); made.Clear(); ActorVisual.Models = models; ModelBeast.Enabled = beasts; }

        ActorVisual Beast(ActorLook look, int variant = 0, string name = "Beast test")
        {
            var go = new GameObject(name); new GameObject("Body").transform.SetParent(go.transform, false); go.transform.position = new Vector3(made.Count * 4, 1, 0); made.Add(go);
            return ActorVisual.Attach(go, look, variant);
        }
        /// <summary>The model's skin as it is posed now, in world space, vertex by vertex (a renderer's bounds carry a margin);
        /// <paramref name="skinnedOnly"/> leaves the antlers out.</summary>
        static Bounds Extent(ModelBeast m, bool skinnedOnly)
        {
            Bounds b = default; bool any = false; var mesh = new Mesh();
            foreach (var r in m.Renderers)
            {
                if (skinnedOnly && ModelBeast.Antlers(r)) continue;
                Vector3[] verts;
                if (r is SkinnedMeshRenderer sk) { sk.BakeMesh(mesh, true); verts = mesh.vertices; }
                else verts = r.GetComponent<MeshFilter>().sharedMesh.vertices;
                var w = r.transform.localToWorldMatrix;
                foreach (var v in verts) { var p = w.MultiplyPoint3x4(v); if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p); }
            }
            Object.DestroyImmediate(mesh); return b;
        }

        [Test] public void The_animals_are_in_with_their_clips()
        {
            foreach (var kind in new[] { "Wolf", "Stag", "Deer" })
            {
                Assert.IsTrue(ModelBeast.Available(kind), kind + " is in the build (Resources/Creatures).");
                foreach (var slot in new[] { "idle", "walk", "run", "attack", "hitL", "death", "eat" })
                    Assert.NotNull(ModelBeast.SlotClip(kind, slot), kind + ": a clip for " + slot + ".");
                Assert.IsTrue(ModelBeast.SlotClip(kind, "walk").isLooping, kind + ": the walk loops.");
                Assert.IsFalse(ModelBeast.SlotClip(kind, "death").isLooping, kind + ": the death is played once.");
            }
        }

        [Test] public void Wolves_hounds_stags_and_does_stand_on_the_ground_facing_forward()
        {
            Assume.That(ModelBeast.Available("Wolf") && ModelBeast.Available("Stag") && ModelBeast.Available("Deer") && ModelBeast.Available("Boar"));
            foreach (var (look, variant, kind, height) in new[] { (ActorLook.Wolf, 0, "Wolf", 1f), (ActorLook.Wolf, 1, "Wolf", 1f), (ActorLook.Stag, 0, "Stag", 1.9f), (ActorLook.Stag, 1, "Deer", 1.6f), (ActorLook.Boar, 0, "Boar", .95f) })
            {
                var v = Beast(look, variant); var m = v.BeastModel; string what = look + " " + variant;
                Assert.NotNull(m, what + " is a model."); Assert.AreEqual(kind, m.Kind, what + " wears the " + kind + ".");
                Assert.IsNull(v.transform.Find("Body/Leg FL"), what + ": no legs of its old body.");
                var b = Extent(m, true); float ground = v.transform.position.y - 1;
                Assert.AreEqual(ground, b.min.y, .06f, what + ": its feet on the ground.");
                Assert.AreEqual(height, b.max.y - ground, .06f, what + ": its head " + height + " m up.");
                Assert.Greater(v.transform.InverseTransformPoint(m.Head.position).z, v.transform.InverseTransformPoint(m.Hips.position).z, what + ": it faces forward (+Z), head before hips.");
                Assert.Greater(m.Length, height * .6f, what + ": longer than a sliver.");
                Assert.IsTrue(m.Renderers.TrueForAll(r => System.Array.TrueForAll(r.sharedMaterials, x => x != null && x.shader.name == "Standard")), what + ": every material the game's own.");
            }
        }

        [Test] public void An_ash_hound_has_burning_eyes_and_a_stag_its_antlers()
        {
            Assume.That(ModelBeast.Available("Wolf") && ModelBeast.Available("Stag"));
            bool Glows(ActorVisual v) { return v.BeastModel.Renderers.Exists(r => System.Array.Exists(r.sharedMaterials, x => x.IsKeywordEnabled("_EMISSION"))); }
            Assert.IsTrue(Glows(Beast(ActorLook.Wolf, 1, "Ash hound")), "An ash hound's eyes burn.");
            Assert.IsFalse(Glows(Beast(ActorLook.Wolf, 0, "Grey wolf")), "A grey wolf's do not.");
            var stag = Beast(ActorLook.Stag, 0, "Forest stag").BeastModel;
            Assert.Greater(Extent(stag, false).max.y - Extent(stag, true).max.y, .3f, "The stag's antlers stand well above its head.");
        }

        [Test] public void The_dead_lie_down_and_the_walk_moves_the_legs()
        {
            Assume.That(ModelBeast.Available("Wolf"));
            var v = Beast(ActorLook.Wolf); var m = v.BeastModel; float ground = v.transform.position.y - 1;
            float standing = m.Hips.position.y - ground;
            var foot = m.Bone("FrontLowerLeg.L"); Assert.NotNull(foot, "The wolf has its front leg.");
            v.BeastPreview("walk", 0); var a = foot.position; v.BeastPreview("walk", m.ClipLength("walk") / 2); var b = foot.position;
            Assert.Greater(Vector3.Distance(a, b), .05f, "Half a stride on, the foreleg has moved.");
            v.BeastPreview("death", 10);
            Assert.Less(m.Hips.position.y - ground, standing * .6f, "Dead, its hips are down near the ground.");
            Assert.Greater(Lowest(m), ground - .15f, "And not sunk into it.");
        }
        /// <summary>The lowest point of the model's skin as it is posed now (BakeMesh: the renderers' bounds keep the rest pose's).</summary>
        static float Lowest(ModelBeast m)
        {
            float low = float.MaxValue; var mesh = new Mesh();
            foreach (var r in m.Renderers)
            {
                if (!(r is SkinnedMeshRenderer sk)) continue;
                sk.BakeMesh(mesh, true); var w = sk.transform.localToWorldMatrix;   // baked in its own (scaled) space
                foreach (var p in mesh.vertices) low = Mathf.Min(low, w.MultiplyPoint3x4(p).y);
            }
            Object.DestroyImmediate(mesh); return low;
        }

        [Test] public void The_boar_is_moved_by_the_code()
        {
            Assume.That(ModelBeast.Available("Boar"));
            var v = Beast(ActorLook.Boar, 0, "Wild boar"); var m = v.BeastModel; float ground = v.transform.position.y - 1;
            Assert.IsTrue(m.Procedural, "CraftPix's boar has no clips: the code moves it.");
            Assert.IsTrue(m.Renderers.TrueForAll(r => System.Array.TrueForAll(r.sharedMaterials, x => x.mainTexture != null)), "It keeps its palette texture.");
            float standing = m.Hips.position.y - ground;
            var legs = new List<Transform>(); foreach (var t in m.Model.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("Front_Knee_L") || t.name.StartsWith("Hind_Knee_L")) legs.Add(t);
            Assert.AreEqual(2, legs.Count, "It has its front and hind knees.");
            v.BeastPreview("walk", 0); var a = legs[0].position; var b0 = legs[1].position;
            v.BeastPreview("walk", m.ClipLength("walk") * .25f);
            Assert.Greater(Vector3.Distance(a, legs[0].position) + Vector3.Distance(b0, legs[1].position), .04f, "A quarter of a stride on, its legs have moved.");
            v.BeastPreview("attack", .26f); Assert.Greater(v.transform.InverseTransformPoint(m.Model.position).z, .05f, "Its charge lunges forward.");
            v.BeastPreview("death", 2);
            Assert.Less(m.Hips.position.y - ground, standing * .7f, "Dead, it lies on its side.");
            Assert.Greater(Lowest(m), ground - .12f, "On the ground, not in it.");
            v.BeastPreview("idle", 0); Assert.AreEqual(standing, m.Hips.position.y - ground, .02f, "And up again as it was.");
        }

        [Test] public void Game_deer_wear_the_stag_or_the_deer()
        {
            Assume.That(ModelBeast.Available("Stag") && ModelBeast.Available("Deer"));
            var go = new GameObject("Hill deer"); made.Add(go);
            var antlered = CritterBody.Build(go.transform, "deer", .2f, 3, -1);
            Assert.NotNull(antlered.Model); Assert.AreEqual("Stag", antlered.Model.Kind, "An antlered hill deer is the stag.");
            var go2 = new GameObject("Hill deer 2"); made.Add(go2);
            var doe = CritterBody.Build(go2.transform, "deer", .8f, 5, -1);
            Assert.AreEqual("Deer", doe.Model.Kind, "The rest are the deer.");
            Assert.AreEqual(1.55f, doe.Model.Height, .001f);
            doe.LieDown(); Assert.IsTrue(doe.Model.Dead, "Fallen, it plays its death."); Assert.Less(Quaternion.Angle(doe.Root.localRotation, Quaternion.identity), 1, "Not rolled over as well.");
            doe.StandUp(); Assert.IsFalse(doe.Model.Dead, "Back on its feet.");
            var go3 = new GameObject("Hen"); made.Add(go3);
            Assert.IsNull(CritterBody.Build(go3.transform, "chicken", .3f, 1).Model, "Hens keep their own bodies (the pack has none).");
        }

        [Test] public void Farm_animals_are_horses_donkeys_and_cows_never_hunted()
        {
            Assume.That(ModelBeast.Available("Horse") && ModelBeast.Available("Donkey") && ModelBeast.Available("Cow"));
            var colours = new HashSet<string>();
            foreach (var (kind, animal, height) in new[] { ("horse", "Horse", 2.15f), ("donkey", "Donkey", 1.6f), ("cow", "Cow", 1.6f) })
            {
                Assert.Contains(kind, GameAnimals.NeverHunted, "A " + kind + " is never hunted."); Assert.IsFalse(GameAnimals.IsGame(kind));
                foreach (var r in new[] { .1f, .3f, .6f, .9f })
                {
                    var go = new GameObject("Farm " + kind + " " + r); made.Add(go);
                    var c = CritterBody.Build(go.transform, kind, r, 7);
                    Assert.NotNull(c.Model, kind + " is a model."); Assert.AreEqual(animal, c.Model.Kind);
                    Assert.AreEqual(height, c.Model.Height, .001f, kind + "'s height.");
                    var b = Extent(c.Model, true); Assert.AreEqual(go.transform.position.y, b.min.y, .06f, kind + ": its feet on the ground.");
                    Assert.Less(c.Speed, 1.5f, kind + " walks."); Assert.Less(c.FleeSpeed, 2.5f, kind + " only ambles off."); Assert.Less(c.FleeRadius, 2.5f, kind + " lets you come close.");
                    colours.Add(kind + ":" + string.Join(",", System.Array.ConvertAll(c.Model.Renderers[0].sharedMaterials, m => ColorUtility.ToHtmlStringRGB(m.color))));
                }
            }
            Assert.GreaterOrEqual(colours.Count, 8, "Horses, cows and donkeys come in more than one coat.");
            ModelBeast.Enabled = false;
            var plain = new GameObject("Plain cow"); made.Add(plain);
            var old = CritterBody.Build(plain.transform, "cow", .5f, 3);
            Assert.IsNull(old.Model, "Animal models off: a plain beast."); Assert.Greater(plain.GetComponentsInChildren<Renderer>().Length, 5, "Built of parts.");
        }

        [Test] public void Models_off_builds_the_old_bodies()
        {
            ActorVisual.Models = false;
            var v = Beast(ActorLook.Wolf);
            Assert.IsNull(v.BeastModel, "Models off: the old wolf.");
            Assert.NotNull(v.transform.Find("Body/Leg FL"), "With its legs.");
            ActorVisual.Models = true; ModelBeast.Enabled = false;
            var go = new GameObject("Hill deer"); made.Add(go);
            Assert.IsNull(CritterBody.Build(go.transform, "deer", .2f, 3, -1).Model, "Animal models off: the old deer.");
            Assert.IsNull(Beast(ActorLook.Stag).BeastModel, "And the old stag.");
        }
    }
}
