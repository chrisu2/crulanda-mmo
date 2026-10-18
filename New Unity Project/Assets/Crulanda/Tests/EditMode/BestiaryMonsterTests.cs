using System.Collections.Generic;
using System.IO;
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
    /// mode (no scene, no save). What they are in the world is Chris's pick (Docs/BESTIARY_MONSTERS.md): no camp wears them yet.
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
