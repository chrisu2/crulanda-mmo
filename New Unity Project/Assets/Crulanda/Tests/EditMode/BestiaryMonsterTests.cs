using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// Quaternius's Bestiary - Dungeon Monsters Kit (play note 233, Chris: "use this for some different monsters"): the Imp and the Puglin
    /// are in Resources/Characters/Bestiary with their three skins, normal and emissive maps; they come in Humanoid with their own avatars
    /// (Editor/CharacterImport.cs); the camp data's "imp" and "puglin" make their looks; each is built on its human avatar, moved by the
    /// people's clips, at its height (the Imp near a man's, the Puglin small) and in the skin its variant picks, its eyes lit. Built in edit
    /// mode (no scene, no save). Chris picked what they are on 2026-10-18 (Docs/BESTIARY_MONSTERS.md): the Adit's Ember-bound and the Lowtowns' Bank-snouts.
    /// </summary>
    public class BestiaryMonsterTests
    {
        static readonly string[] Kinds = { "Imp", "Puglin" };
        const string Folder = "Assets/Crulanda/Resources/Characters/Bestiary/";
        readonly List<GameObject> made = new List<GameObject>();
        bool models, beasts;
        [SetUp] public void On() { models = ActorVisual.Models; beasts = ModelBeast.Enabled; ActorVisual.Models = true; ModelBeast.Enabled = true; }
        [TearDown] public void TearDown() { foreach (var g in made) if (g != null) Object.DestroyImmediate(g); made.Clear(); ActorVisual.Models = models; ModelBeast.Enabled = beasts; }

        ActorVisual Monster(ActorLook look, int variant, string name = "Bestiary test")
        {
            var go = new GameObject(name); new GameObject("Body").transform.SetParent(go.transform, false); go.transform.position = new Vector3(made.Count * 4, 1, 0); made.Add(go);
            return ActorVisual.Attach(go, look, variant);
        }
        static string OnDisk(string assetPath) { return Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath); }

        [Test] public void The_models_and_their_textures_are_in()
        {
            Assert.IsTrue(File.Exists(OnDisk(Folder + "License.txt")), "the kit's licence (QAL) beside the models");
            foreach (var kind in Kinds)
            {
                Assert.IsTrue(File.Exists(OnDisk(Folder + kind + ".fbx")), kind + ".fbx");
                Assert.IsTrue(File.Exists(OnDisk(Folder + kind + ".fbx.meta")), kind + ".fbx.meta");
                foreach (var map in new[] { "BaseColor_1", "BaseColor_2", "BaseColor_3", "Normal", "Emissive" })
                {
                    Assert.IsTrue(File.Exists(OnDisk(Folder + "Textures/T_" + kind + "_" + map + ".png")), kind + " " + map);
                    Assert.NotNull(Resources.Load<Texture2D>(ModelBeast.BestiaryPath + "Textures/T_" + kind + "_" + map), kind + " " + map + " loads from Resources");
                }
                Assert.IsTrue(ModelBeast.Available(kind), kind + " is in the build");
            }
        }

        [Test] public void They_come_in_humanoid_with_their_own_avatars()
        {
            var rule = File.ReadAllText(OnDisk("Assets/Crulanda/Editor/CharacterImport.cs"));
            StringAssert.Contains("Root + \"Bestiary/\"", rule, "the import rule names the folder");
            StringAssert.Contains("ModelImporterAnimationType.Human", rule, "and makes its models Humanoid");
            foreach (var kind in Kinds)
            {
                var m = AssetImporter.GetAtPath(Folder + kind + ".fbx") as ModelImporter;
                Assert.NotNull(m, kind + " is imported as a model");
                Assert.AreEqual(ModelImporterAnimationType.Human, m.animationType, kind + " is Humanoid");
                Assert.AreEqual(ModelImporterAvatarSetup.CreateFromThisModel, m.avatarSetup, kind + " has its own avatar");
                Assert.IsFalse(m.importAnimation, kind + " brings no clips of its own");
                Assert.IsTrue(m.isReadable, kind + " is readable (its mesh gets tangents for the normal map)");
                Avatar avatar = null; foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Folder + kind + ".fbx")) if (o is Avatar a) avatar = a;
                Assert.NotNull(avatar, kind + "'s avatar"); Assert.IsTrue(avatar.isHuman && avatar.isValid, kind + "'s avatar is a valid human one");
                var normal = AssetImporter.GetAtPath(Folder + "Textures/T_" + kind + "_Normal.png") as TextureImporter;
                Assert.NotNull(normal); Assert.AreEqual(TextureImporterType.NormalMap, normal.textureType, kind + "'s normal map");
            }
        }

        [Test] public void The_camp_data_names_make_their_looks_and_the_looks_build_the_models()
        {
            var lookFor = typeof(EncounterSession).GetMethod("LookFor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(lookFor, "EncounterSession.LookFor (a camp's look from the zone data)");
            Assert.AreEqual(ActorLook.Imp, lookFor.Invoke(null, new object[] { "imp", false }));
            Assert.AreEqual(ActorLook.Puglin, lookFor.Invoke(null, new object[] { "Puglin", false }));
            Assert.IsFalse(ActorVisual.IsBeast(ActorLook.Imp) || ActorVisual.IsBeast(ActorLook.Puglin), "they are two-legged fighters with weapons, not beasts");
            foreach (var (look, kind, height) in new[] { (ActorLook.Imp, "Imp", 1.7f), (ActorLook.Puglin, "Puglin", 1f) })
            {
                foreach (var slot in new[] { "idle", "walk", "run", "attack", "attack2", "attack3", "hitL", "hitR", "death" })
                {
                    var c = ModelBeast.SlotClip(kind, slot); Assert.NotNull(c, kind + ": " + slot);
                    Assert.IsTrue(c.isHumanMotion, kind + "'s " + slot + " is a humanoid clip (" + c.name + ")");
                }
                var v = Monster(look, 0, kind + " test"); var m = v.BeastModel;
                Assert.NotNull(m, kind + " is built as a model"); Assert.AreEqual(kind, m.Kind);
                Assert.IsTrue(m.OnHumanAvatar, kind + " stands on its human avatar"); Assert.IsTrue(m.Animator.avatar.isHuman);
                Assert.AreEqual(height, m.Height, .1f, kind + "'s height");
                Assert.GreaterOrEqual(m.Renderers.Count, kind == "Imp" ? 5 : 3, kind + ": body and gear (the Imp's mace, chains, collar and shorts; the Puglin's stick and tusks)");
                bool weapon = false; foreach (var r in m.Renderers) if (r.name.Contains(kind == "Imp" ? "Mace" : "Stick")) weapon = true;
                Assert.IsTrue(weapon, kind + " holds its weapon");
                m.Sample("walk", .3f); m.Sample("death", 5);   // the people's clips pose it without complaint
            }
            var elite = Monster(ActorLook.Imp, 0, "Imp (elite)"); Assert.Greater(elite.BeastModel.Height, 1.8f, "an elite a size bigger");
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b) { var d = b - a; float t = d.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, d) / d.sqrMagnitude) : 0; return Vector2.Distance(p, a + d * t); }
        static float PathDist(Vector2 p, Vector2[] pts) { float m = float.MaxValue; for (int i = 0; i + 1 < pts.Length; i++) m = Mathf.Min(m, SegDist(p, pts[i], pts[i + 1])); return m; }

        /// <summary>Chris, 2026-10-18: "Ember-bound (Recommended)". The Ember Vent (the Ash Garden and the Chapel of Embers) has its chained
        /// Ember-bound in pairs, on the Imp, at the Vent's level, solitary, each with its Chain-Lash; the plan's stand-in "ember elementals" are
        /// in no camp; the rare stays last.</summary>
        [Test] public void The_ember_vent_has_its_ember_bound_on_the_imp()
        {
            var adit = LootTestData.Zones().Single(z => z.id == "zone.adit");
            var ember = adit.camps.Where(c => c != null && c.mob == "Ember-bound").ToList();
            Assert.AreEqual(2, ember.Count, "two chained pairs");
            CollectionAssert.AreEquivalent(new[] { "vent", "chapel" }, ember.Select(c => c.wing).ToArray(), "one in the Ash Garden, one at the lake of fire");
            foreach (var c in ember)
            {
                Assert.AreEqual("imp", c.look, c.name); Assert.AreEqual("emberbound", c.tag, c.name); Assert.IsFalse(c.elite, c.name);
                Assert.That(c.count, Is.InRange(1, 2), c.name + ": pulled one or two at a time (DUNGEON_DESIGN.md)");
                Assert.AreEqual(11, c.levelMin, c.name); Assert.AreEqual(11, c.levelMax, c.name);
                Assert.AreEqual("solitary", c.social, c.name + ": chained apart");
                Assert.Greater(c.step, 0, c.name + " is a fight of the run"); StringAssert.StartsWith("GAME-ONLY", c.canonStatus);
            }
            Assert.IsFalse(adit.camps.Any(c => c != null && (c.mob ?? "").IndexOf("elemental", System.StringComparison.OrdinalIgnoreCase) >= 0), "no stand-in elementals");
            Assert.NotNull(MobCasts.For("Ember-bound"), "its Chain-Lash"); Assert.AreEqual("root", MobCasts.For("Ember-bound").cc);
            Assert.AreEqual("The Quiet Miner", adit.camps[adit.camps.Length - 1].mob, "the rare still last");
            Assert.AreEqual("T_Imp_BaseColor_1", ActorVisual.BestiaryCoat("Imp", 0).Albedo.name, "a camp mob's variant (0) is the Imp's red skin");
        }

        /// <summary>Chris, 2026-10-18: "Bank-snouts (Recommended)". The Lowtowns have small packs of Bank-snouts on the Chemical River's banks: on the
        /// Puglin, in the zone's band, packing, off the water, the walkways and the quays, before the rare Lantern-Eater.</summary>
        [Test] public void The_lowtowns_have_bank_snout_packs_on_the_river_banks()
        {
            var z = LootTestData.Zones().Single(x => x.id == "zone.lowtowns");
            var river = z.water.Single(w => w.name == "The Chemical River");
            var snouts = z.camps.Where(c => c != null && c.mob == "Bank-snout").ToList();
            Assert.That(snouts.Count, Is.InRange(2, 4), "two to four packs");
            foreach (var c in snouts)
            {
                Assert.AreEqual("puglin", c.look, c.name); Assert.AreEqual("banksnout", c.tag, c.name); Assert.IsFalse(c.elite, c.name);
                Assert.That(c.count, Is.InRange(3, 4), c.name + ": a small pack"); Assert.AreEqual("pack", c.social, c.name);
                Assert.GreaterOrEqual(c.levelMin, 15, c.name); Assert.LessOrEqual(c.levelMax, 20, c.name);
                float off = PathDist(c.center, river.points) - river.width / 2;
                Assert.LessOrEqual(off, 14f, c.name + ": on the bank band (" + off.ToString("0.0") + " m from the waterline)");
                // Every member dry (the second tour, 2026-10-18: a blue snout stood in the river): its spawn square's corners (radius times
                // root two), its stroll circle (the radius, 2.5 m at the least) and the code's own guard (the camp's dry flag).
                Assert.IsTrue(c.dry, c.name + " keeps to dry ground (spawn, stroll, roam and respawn: EncounterSession.Dry)");
                float reach = Mathf.Max(c.radius * 1.415f, Mathf.Max(2.5f, c.radius));
                foreach (var w in z.water.Where(w => w != null && w.points != null && w.points.Length > 1))
                    Assert.GreaterOrEqual(PathDist(c.center, w.points) - w.width / 2 - reach, EncounterSession.DryMargin, c.name + ": every spot it may be stood on or stroll to is " + EncounterSession.DryMargin + " m or more from " + w.name);
                foreach (var road in z.roads) Assert.Greater(PathDist(c.center, road.points) - road.width / 2, c.radius + 2, c.name + ": off " + road.name);
                foreach (var p in z.props.Where(p => p.kind == "dock" || p.kind == "brick_bridge"))
                    Assert.Greater(Vector2.Distance(c.center, p.at), 18, c.name + ": clear of " + p.name);
                StringAssert.Contains("book1 ch.15:391", c.canonStatus, c.name + " cites the river's 'and worse'");
            }
            Assert.AreEqual("The Lantern-Eater", z.camps.Last().mob, "the rare still last");
            Assert.NotNull(MobCasts.For("Bank-snout"), "its Tusk Butt");
        }

        [Test] public void The_variant_picks_the_skin_and_the_eyes_glow()
        {
            foreach (var kind in Kinds)
            {
                var look = kind == "Imp" ? ActorLook.Imp : ActorLook.Puglin; var skins = new HashSet<string>();
                for (int variant = 0; variant < 3; variant++)
                {
                    var m = Monster(look, variant).BeastModel; Assert.NotNull(m);
                    var mat = m.Renderers[0].sharedMaterial; Assert.NotNull(mat.mainTexture, kind + " is painted");
                    StringAssert.Contains("T_" + kind + "_BaseColor_" + (variant + 1), mat.mainTexture.name, kind + " variant " + variant);
                    Assert.IsTrue(mat.IsKeywordEnabled("_EMISSION") && mat.GetTexture("_EmissionMap") != null, kind + "'s eyes glow (the emissive mask)");
                    Assert.IsTrue(mat.IsKeywordEnabled("_NORMALMAP"), kind + " has its normal map");
                    skins.Add(mat.mainTexture.name);
                }
                Assert.AreEqual(3, skins.Count, kind + ": three colourings");
            }
        }
    }
}
