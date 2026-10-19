using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Play note 253 (2026-10-19, Chris: "assets for alchemy - myassets Free Alchemy and Magic Pack"): the pack's mortar, flask and crystal
    /// flask load and stand at life size on their foot; every material they wear is a Built-in shader and their maps are capped at 1024;
    /// every potion item has a look whose models are in the project, the cordials and draughts the pack's flasks; each zone's herbalist's
    /// bench has its own set, on the bench's top.
    /// </summary>
    public class AlchemyPackTests
    {
        static readonly string[] BuiltIn = { "Standard", "Standard (Specular setup)" };
        static string Props { get { return Path.Combine(Application.dataPath, "Crulanda", "Resources", "Props"); } }
        static bool ModelThere(string path)
        {
            var f = Path.Combine(Props, path.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(f + ".fbx") || File.Exists(f + ".FBX") || File.Exists(f + ".prefab");
        }
        static readonly (string path, float height)[] Pack = {
            (ZoneBuilder.AlchemyMortar, ZoneBuilder.MortarHeight), (ZoneBuilder.AlchemyFlask, ZoneBuilder.FlaskHeight), (ZoneBuilder.AlchemyCrystal, ZoneBuilder.CrystalHeight) };

        [Test] public void The_packs_models_load_and_stand_at_life_size()
        {
            var root = new GameObject("alchemy").transform; var wrong = new StringBuilder();
            try
            {
                foreach (var (path, height) in Pack)
                {
                    Assert.IsTrue(ModelThere(path), path + " is in the project");
                    var holder = new GameObject(path).transform; holder.SetParent(root, false);
                    var go = ZoneBuilder.ModelProp(holder, path, Vector3.zero, 0, height);
                    if (go == null) { wrong.Append(path + " does not load; "); continue; }
                    var b = ZoneBuilder.PropBounds(holder);
                    if (Mathf.Abs(b.size.y - height) > .01f) wrong.Append(path + " stands " + b.size.y.ToString("0.000") + ", not " + height + "; ");
                    if (Mathf.Abs(b.min.y) > .01f) wrong.Append(path + " foot at " + b.min.y.ToString("0.000") + "; ");
                    if (Mathf.Max(b.size.x, b.size.z) > height * 1.6f) wrong.Append(path + " is " + Mathf.Max(b.size.x, b.size.z).ToString("0.00") + " across: on its side?; ");
                    if (go.GetComponentsInChildren<MeshRenderer>().Length == 0) wrong.Append(path + " has no mesh; ");
                }
                Assert.IsTrue(Pack.All(p => ZoneBuilder.KitProps.Any(k => k.path == p.path)), "in the kit row (PropCapture)");
            }
            finally { Object.DestroyImmediate(root.gameObject); }
            Assert.AreEqual("", wrong.ToString(), "wrong: ");
        }

        [Test] public void Every_material_is_a_Built_in_shader_and_the_maps_are_capped()
        {
            var mats = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Crulanda/Resources/Props/Alchemy" }).Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Assert.GreaterOrEqual(mats.Length, 5, "the pack's four materials and PotionGlass");
            foreach (var m in mats)
            {
                Assert.Contains(m.shader.name, BuiltIn, m.name + "'s shader");
                Assert.IsTrue(m.shader.isSupported, m.name + "'s shader runs here");
            }
            Assert.NotNull(ZoneBuilder.PotionGlass, "Resources/Props/Alchemy/PotionGlass.mat");
            Assert.Greater(ZoneBuilder.PotionGlass.color.a, .2f, "the potion glass is tinted enough to see");
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Crulanda/Resources/Props/Alchemy" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g); var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.NotNull(imp, path); Assert.LessOrEqual(imp.maxTextureSize, 1024, path + " capped at 1024 (ThirdPartyImport)");
            }
            // Placed, each model wears only Built-in materials, none missing, and its glass is the potion glass.
            var root = new GameObject("alchemy").transform;
            try
            {
                foreach (var (path, height) in Pack)
                {
                    var go = ZoneBuilder.ModelProp(root, path, Vector3.zero, 0, height);
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                        foreach (var m in r.sharedMaterials)
                        {
                            Assert.NotNull(m, path + " (" + r.name + ") has a missing material");
                            Assert.Contains(m.shader.name, BuiltIn, path + " (" + r.name + ")");
                            Assert.IsFalse(m.name.Contains("Glass") && m != ZoneBuilder.PotionGlass, path + " (" + r.name + ") still wears the pack's clear glass");
                        }
                }
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        static ItemDatabase Items()
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Items");
            var texts = new List<string>(); foreach (var f in Directory.GetFiles(dir, "*.json")) texts.Add(File.ReadAllText(f));
            return ItemDatabase.Parse(texts);
        }

        [Test] public void Every_potion_items_look_names_models_in_the_project()
        {
            var potions = Items().Items.Keys.Where(id => id.StartsWith("potion.")).ToArray();
            Assert.Greater(potions.Length, 15, "the potion items were read");
            var wrong = new StringBuilder();
            foreach (var id in potions)
            {
                if (!FoodModels.Dishes.TryGetValue(id, out var bits) || bits.Length == 0) { wrong.Append(id + " has no look; "); continue; }
                foreach (var b in bits) if (!ModelThere(b.path) || !ZoneBuilder.HasProp(b.path)) wrong.Append(id + ": " + b.path + " not in the project; ");
            }
            Assert.AreEqual("", wrong.ToString(), "wrong: ");
            // The pack's flasks are the cordials' and the draughts' look (the tinctures keep the kit's small vials).
            foreach (var id in potions.Where(i => i.StartsWith("potion.cordial_")))
                Assert.AreEqual(ZoneBuilder.AlchemyCrystal, FoodModels.Dishes[id][0].path, id);
            foreach (var id in new[] { "potion.minor", "potion.healing", "potion.draught_mana", "potion.draught_vigor", "potion.draught_focus", "potion.draught_nerve", "potion.draught_breath" })
                Assert.AreEqual(ZoneBuilder.AlchemyFlask, FoodModels.Dishes[id][0].path, id);
        }

        [Test] public void Each_zones_herbalists_bench_has_its_own_set_on_the_bench()
        {
            var zones = new HashSet<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones"), "*.json"))
            {
                var z = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(f));
                if (z.stations != null && z.stations.Any(s => s != null && s.kind == "bench" && !s.name.Contains("Court"))) zones.Add(z.id);
            }
            Assert.GreaterOrEqual(zones.Count, 4, "zones with a herbalist's bench");
            var seen = new List<string>();
            foreach (var zone in zones)
            {
                Assert.IsTrue(ZoneBuilder.BenchSets.ContainsKey(zone), zone + " has its own bench set");
                var set = ZoneBuilder.BenchSets[zone];
                Assert.IsTrue(set.Any(p => p.path == ZoneBuilder.AlchemyMortar), zone + ": a mortar");
                foreach (var p in set)
                {
                    Assert.LessOrEqual(Mathf.Abs(p.x) + .1f, .8f, zone + ": " + p.path + " on the bench's length");
                    Assert.LessOrEqual(Mathf.Abs(p.z), .2f, zone + ": " + p.path + " on the bench's depth");
                    Assert.IsTrue(ModelThere(p.path), p.path);
                }
                var key = string.Join("|", set.Select(p => p.path + "@" + p.x.ToString("0.00") + "," + p.z.ToString("0.00")));
                Assert.IsFalse(seen.Contains(key), zone + "'s bench is laid out like another zone's");
                seen.Add(key);
            }
        }
    }
}
