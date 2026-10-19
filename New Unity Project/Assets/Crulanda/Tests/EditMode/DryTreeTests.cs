using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Play note 248 (Chris, 2026-10-19: "replace all dead trees with this pack in myassets - dry trees"): every dead tree is one of the
    /// seven Dry Trees. Data and files only: no zone is built. The new members are reached by reflection, so on the code before the
    /// change these fail on their asserts, not on the compile.
    /// </summary>
    public class DryTreeTests
    {
        static string Root { get { return Path.Combine(Application.dataPath, "Crulanda"); } }
        static string DryDir { get { return Path.Combine(Root, "Resources", "Props", "DryTrees"); } }
        /// <summary>The pack's own GUIDs (Dry Trees.unitypackage), kept so a later import of the pack lands on the same assets.</summary>
        static readonly Dictionary<string, string> PackGuids = new Dictionary<string, string> {
            { "Model/Dry3333.FBX", "f12b0b72f2d21d64db13e1bfa8f3ec4f" }, { "Model/Dry4195.FBX", "750f8f30b2708f64f86e0a9176d9d7dc" },
            { "Model/Dry4910.FBX", "392ccefdc43e68245b2b50fd1e5cd507" }, { "Model/Dry5818.FBX", "93f27f566ebdec1439fda34a79463ade" },
            { "Model/Dry6524.FBX", "5736b0f1a73af4a4581db8226d31045c" }, { "Model/Dry7509.FBX", "2a4339fd08e4a774f907817c85495b3b" },
            { "Model/Dry8872.FBX", "9d9a2868cd321ba4a90fb64be4b73abd" }, { "Materials/Bark.mat", "14f732f9f0a9e3d408764155dadf8518" },
            { "Materials/Bark.tif", "2669273f6d3e04e4bbb65d6af4e11282" }, { "Materials/Normal.tif", "12b143840cc45844e81342a74f9374d5" } };
        static string Guid(string metaPath) { var m = Regex.Match(File.ReadAllText(metaPath), @"^guid: ([0-9a-f]{32})", RegexOptions.Multiline); return m.Success ? m.Groups[1].Value : null; }
        static MethodInfo Method(string name) { return typeof(ZoneBuilder).GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); }
        static string[] Models()
        {
            var f = typeof(ZoneBuilder).GetField("DryTreeModels", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(f, "ZoneBuilder.DryTreeModels: the seven dry trees"); return (string[])f.GetValue(null);
        }

        [Test] public void No_old_dead_tree_model_or_builder_is_used()
        {
            var bad = new StringBuilder();
            foreach (var file in Directory.GetFiles(Path.Combine(Root, "Scripts"), "*.cs", SearchOption.AllDirectories))
                if (File.ReadAllText(file).Contains("Nature/DeadTree_")) bad.Append(Path.GetFileName(file) + " loads the nature kit's dead tree; ");
            Assert.AreEqual("", bad.ToString());
            // The one builder every dead tree goes through stands a dry tree, and builds no painted bole or limbs of its own.
            string src = File.ReadAllText(Path.Combine(Root, "Scripts", "World", "ZoneBuilder.cs"));
            int at = src.IndexOf("Transform DeadTree(", System.StringComparison.Ordinal); Assert.Greater(at, 0, "DeadTree is there");
            int end = src.IndexOf("\n        }", at, System.StringComparison.Ordinal); string body = src.Substring(at, end - at);
            StringAssert.Contains("DryTreeModel(", body, "a dead tree is a Dry Tree");
            foreach (var old in new[] { "Bole(", "Limb(", "KitTree(", "ModelProp(" }) StringAssert.DoesNotContain(old, body, "the old dead tree's " + old);
            // The fallen trunks are dry trees laid down; past the edge the near wood's and the first rows' dead trees are dry trees too.
            string woods = File.ReadAllText(Path.Combine(Root, "Scripts", "World", "ZoneBuilder.Woods.cs"));
            StringAssert.Contains("DryTreeModel(", woods, "the fallen trunk");
            Assert.AreEqual(3, Regex.Matches(src, @"PutDry\(").Count, "the backdrop's PutDry: its definition, the silhouettes and the near wood");
        }

        [Test] public void Every_pick_is_one_of_the_seven()
        {
            var models = Models();
            CollectionAssert.AreEqual(new[] { "Dry3333", "Dry4195", "Dry4910", "Dry5818", "Dry6524", "Dry7509", "Dry8872" }, models.Select(m => m.Substring(m.LastIndexOf('/') + 1)).ToArray());
            foreach (var m in models)
            {
                StringAssert.StartsWith("DryTrees/Model/", m);
                var file = Path.Combine(Path.Combine(Root, "Resources", "Props"), m.Replace('/', Path.DirectorySeparatorChar)) + ".FBX";
                Assert.IsTrue(File.Exists(file), m + ".FBX in the project"); Assert.IsTrue(File.Exists(file + ".meta"), m + ".FBX.meta");
            }
            var pick = Method("DryTreePick"); Assert.NotNull(pick, "ZoneBuilder.DryTreePick");
            var seen = new HashSet<int>();
            foreach (float u in Enumerable.Range(0, 1001).Select(i => i / 1000f).Concat(new[] { .99999f, 1f, -.1f, 1.5f }))
            {
                int k = (int)pick.Invoke(null, new object[] { u });
                Assert.That(k, Is.InRange(0, models.Length - 1), "pick of " + u); seen.Add(k);
            }
            Assert.AreEqual(7, seen.Count, "all seven are picked");
        }

        [Test] public void The_pack_is_in_with_its_own_GUIDs_and_Built_in_materials()
        {
            foreach (var kv in PackGuids)
            {
                var file = Path.Combine(DryDir, kv.Key.Replace('/', Path.DirectorySeparatorChar));
                Assert.IsTrue(File.Exists(file), kv.Key); Assert.IsTrue(File.Exists(file + ".meta"), kv.Key + ".meta");
                Assert.AreEqual(kv.Value, Guid(file + ".meta"), kv.Key + " keeps the pack's GUID");
                Assert.Greater(File.ReadAllLines(file + ".meta").Length, 4, kv.Key + ".meta is the pack's full meta, not a guid alone");
            }
            // Every material is Built-in Standard (fileID 46), not the pack's HDRP Lit; it shows the pack's bark and its normal map.
            var mats = Directory.GetFiles(DryDir, "*.mat", SearchOption.AllDirectories); Assert.IsNotEmpty(mats);
            foreach (var mat in mats)
            {
                string text = File.ReadAllText(mat);
                StringAssert.Contains("m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000, type: 0}", text, Path.GetFileName(mat) + " on Standard");
                StringAssert.DoesNotContain("6e4ae4064600d784cac1e41a9e6f2e59", text, "HDRP Lit");
                StringAssert.DoesNotContain("_BaseColorMap", text, "an HDRP property");
            }
            string bark = File.ReadAllText(Path.Combine(DryDir, "Materials", "Bark.mat"));
            StringAssert.Contains("- _MainTex:\n        m_Texture: {fileID: 2800000, guid: " + PackGuids["Materials/Bark.tif"], bark.Replace("\r\n", "\n"));
            StringAssert.Contains("- _BumpMap:\n        m_Texture: {fileID: 2800000, guid: " + PackGuids["Materials/Normal.tif"], bark.Replace("\r\n", "\n"));
            // Each tree's material slot is mapped to that bark; nothing of the HDRP demo comes along.
            foreach (var meta in Directory.GetFiles(Path.Combine(DryDir, "Model"), "*.FBX.meta"))
                StringAssert.Contains("second: {fileID: 2100000, guid: " + PackGuids["Materials/Bark.mat"], File.ReadAllText(meta), Path.GetFileName(meta));
            Assert.IsEmpty(Directory.GetFiles(DryDir, "*.unity", SearchOption.AllDirectories), "no demo scene");
            Assert.IsEmpty(Directory.GetFiles(DryDir, "*.asset", SearchOption.AllDirectories), "no HDRP volume profiles");
            Assert.IsEmpty(Directory.GetFiles(DryDir, "*.lighting", SearchOption.AllDirectories), "no demo lighting");
            // The textures are capped at 1024 on import.
            StringAssert.Contains("assetPath.Contains(\"/DryTrees/\")", File.ReadAllText(Path.Combine(Root, "Editor", "ThirdPartyImport.cs")), "ThirdPartyImport caps the pack's textures");
        }

        /// <summary>The preview-1019 frame (khaven-03): the Gallows Tree stood as a spindly dry tree with its beam in the air beside it. The
        /// landmark is the stoutest tree, tall and broadened, its crossbeam on a limb read from the mesh; the dead oaks are stout trees.</summary>
        [Test] public void The_Gallows_Tree_and_the_dead_oaks_are_stout()
        {
            Assert.AreEqual(7, ZoneBuilder.DryTrunk.Length);
            int stoutest = System.Array.IndexOf(ZoneBuilder.DryTrunk, ZoneBuilder.DryTrunk.Max());
            Assert.AreEqual(stoutest, ZoneBuilder.GallowsDryTree, "the gallows is the stoutest tree");
            Assert.AreEqual(3, ZoneBuilder.StoutDryTrees.Distinct().Count()); CollectionAssert.Contains(ZoneBuilder.StoutDryTrees, stoutest);
            foreach (int k in ZoneBuilder.StoutDryTrees) Assert.That(k, Is.InRange(0, 6));
            string src = File.ReadAllText(Path.Combine(Root, "Scripts", "World", "ZoneBuilder.cs"));
            int at = src.IndexOf("void Gallows(Transform t)", System.StringComparison.Ordinal); Assert.Greater(at, 0);
            string body = src.Substring(at, src.IndexOf("\n        }", at, System.StringComparison.Ordinal) - at);
            StringAssert.Contains("GallowsDryTree, 9, 1.35f", body, "the stoutest tree, 9 m, broadened");
            StringAssert.Contains("GallowsLimb(", body, "the crossbeam rests on a limb measured from the mesh");
            StringAssert.Contains("StoutDryTrees[", src, "the massive dead oaks pick from the stout trees");
            // (preview-1019 art2: the rope unseen, the platform off the beam's end.) The noose hangs from the beam's free end, its foot at
            // head height over the middle of the boards; the rope reads (3-4 cm); the boards stand clear of the trunk.
            StringAssert.Contains("GallowsPlan(limb)", body, "the gallows is laid out by GallowsPlan");
            Assert.That(ZoneBuilder.GallowsRope, Is.InRange(.03f, .045f), "a rope thick enough to see");
            foreach (var limb in new[] { new Vector3(2.6f, 4.1f, 0), new Vector3(1.6f, 3.4f, 0), new Vector3(3.6f, 5.5f, 0), new Vector3(1.2f, 2.8f, 0) })
            {
                var g = ZoneBuilder.GallowsPlan(limb); string at = "limb " + limb;
                float half = ZoneBuilder.GallowsBoards / 2;
                Assert.Less(Mathf.Abs(g.noose.x - g.platform.x), half * .5f, at + ": the noose over the middle of the boards (x)");
                Assert.Less(Mathf.Abs(g.noose.z - g.platform.z), half * .5f, at + ": the noose over the middle of the boards (z)");
                Assert.That(g.noose.y - ZoneBuilder.GallowsDeck, Is.InRange(1.5f, 2f), at + ": the noose's foot at head height over the deck");
                Assert.That(g.ropeTop.x, Is.InRange(g.beamFrom.x, g.beamTo.x), at + ": the rope hangs from the beam");
                Assert.Less(g.ropeTop.y, g.beamTo.y, at + ": under it"); Assert.Greater(g.ropeTop.y - g.knot.y, .3f, at + ": a length of rope shows");
                Assert.Less(g.beamTo.x - g.ropeTop.x, .25f, at + ": near the beam's free end");
                Assert.Greater(g.platform.x - half, .8f, at + ": the boards clear of the trunk");
                Assert.AreEqual(g.beamFrom.y, g.beamTo.y, 1e-4f, at + ": the beam level, at the limb");
            }
        }

        [Test] public void Heights_are_within_the_old_range()
        {
            var height = Method("DryTreeHeight"); Assert.NotNull(height, "ZoneBuilder.DryTreeHeight");
            float H(float s, bool massive, float u) { return (float)height.Invoke(null, new object[] { s, massive, u }); }
            // The old dead tree: 5.2 m a unit of scale x 1.35 (the nature kit's, stood to that), a dead oak 4.2 m x 2.1 (painted); each 0.9 to 1.1 of it.
            foreach (float s in new[] { .5f, .6f, .75f, .85f, 1f, 1.05f, 1.1f })
                foreach (float u in new[] { 0f, .25f, .5f, .75f, 1f })
                {
                    float common = H(s, false, u), oak = H(s, true, u);
                    Assert.That(common, Is.InRange(5.2f * 1.35f * s * .9f - 1e-4f, 5.2f * 1.35f * s * 1.1f + 1e-4f), "common, scale " + s + ", draw " + u);
                    Assert.That(oak, Is.InRange(4.2f * 2.1f * s * .9f - 1e-4f, 4.2f * 2.1f * s * 1.1f + 1e-4f), "dead oak, scale " + s + ", draw " + u);
                }
            Assert.AreEqual(5.2f * 1.35f * .9f, H(1, false, 0), 1e-4f, "the low end as the old draw made it");
            Assert.AreEqual(5.2f * 1.35f * 1.1f, H(1, false, 1), 1e-4f, "the high end");
        }
    }
}
