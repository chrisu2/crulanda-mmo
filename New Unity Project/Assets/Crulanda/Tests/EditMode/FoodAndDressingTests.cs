using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Playtest notes 234 and 235 (2026-10-18): the food models (FoodModels) and the Fantasy kit's pieces laid out (PropDressing). Every food
    /// and buff item has a look made of models that are in the project; the foods left out are not in it; the food models stand up; the
    /// zones' fare and the caves' sets are each their own; the floor pieces keep off the way through.
    /// </summary>
    public class FoodAndDressingTests
    {
        static string Props { get { return Path.Combine(Application.dataPath, "Crulanda", "Resources", "Props"); } }
        static bool ModelThere(string path) { var f = Path.Combine(Props, path.Replace('/', Path.DirectorySeparatorChar)); return File.Exists(f + ".fbx") || File.Exists(f + ".prefab") || File.Exists(f + ".FBX"); }
        static ItemDatabase Items()
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Items");
            var texts = new List<string>(); foreach (var f in Directory.GetFiles(dir, "*.json")) texts.Add(File.ReadAllText(f));
            return ItemDatabase.Parse(texts);
        }
        static double Overlap(IEnumerable<string> a, IEnumerable<string> b)
        {
            var x = new HashSet<string>(a); var y = new HashSet<string>(b);
            int both = x.Count(y.Contains); int either = x.Union(y).Count(); return either == 0 ? 0 : both / (double)either;
        }

        [Test] public void Every_food_and_buff_item_has_a_look()
        {
            var missing = new StringBuilder(); int n = 0;
            foreach (var d in Items().Items.Values)
            {
                if (d.kind != "consumable") continue;
                bool wanted = d.food || Consumables.HasBuff(d) || (d.restore > 0 && !string.IsNullOrEmpty(d.pool));
                if (!wanted) continue;
                n++;
                if (!FoodModels.Dishes.ContainsKey(d.id)) missing.Append(d.id + "; ");
            }
            Assert.Greater(n, 20, "the food and buff items were read");
            Assert.AreEqual("", missing.ToString(), "food or buff items with no look in FoodModels.Dishes: ");
        }

        [Test] public void Every_look_and_piece_names_a_model_in_the_project()
        {
            var missing = new StringBuilder();
            foreach (var p in FoodModels.AllPaths().Concat(PropDressing.AllPaths()).Distinct()) if (!ModelThere(p)) missing.Append(p + "; ");
            foreach (var (path, _) in ZoneBuilder.KitProps) if ((path.StartsWith("Food/") || path.StartsWith("Fantasy/")) && !ModelThere(path)) missing.Append(path + "; ");
            Assert.AreEqual("", missing.ToString(), "named but not under Resources/Props: ");
            foreach (var d in FoodModels.Dishes) foreach (var b in d.Value) Assert.Greater(b.height, 0, d.Key + ": " + b.path + " has no height");
        }

        [Test] public void Only_the_kept_foods_are_in_the_project()
        {
            Assert.AreEqual(103, FoodModels.Kept.Length + FoodModels.LeftOut.Length, "the pack's 103 models, each kept or left out");
            Assert.IsEmpty(FoodModels.Kept.Intersect(FoodModels.LeftOut).ToArray(), "kept and left out at once");
            var dir = Path.Combine(Props, "Food");
            var there = Directory.GetFiles(dir, "*.fbx").Select(Path.GetFileNameWithoutExtension).ToArray();
            CollectionAssert.AreEquivalent(FoodModels.Kept, there, "Resources/Props/Food holds the kept models, no more and no fewer");
            // Nor anywhere else in the project under the pack's names (a burger brought in by another way).
            var stray = Directory.GetFiles(Path.Combine(Application.dataPath, "Crulanda"), "*.fbx", SearchOption.AllDirectories)
                .Where(f => FoodModels.LeftOut.Contains(Path.GetFileNameWithoutExtension(f))).ToArray();
            Assert.IsEmpty(stray, "left-out foods in the project: " + string.Join(", ", stray));
            foreach (var k in FoodModels.Kept) Assert.Greater(FoodModels.LifeHeight(k), 0, k + " has no life-size height");
        }

        /// <summary>The food pack is Blender's like the Fantasy kit (one node, turned -90 about x): placed, each mesh's z points at the sky.</summary>
        [Test] public void Every_food_model_stands_up()
        {
            var root = new GameObject("food").transform; var wrong = new StringBuilder(); int meshes = 0;
            foreach (var k in FoodModels.Kept)
            {
                var go = ZoneBuilder.ModelProp(root, "Food/" + k, Vector3.zero, 0, FoodModels.LifeHeight(k)); Assert.NotNull(go, k);
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) { meshes++; if (Vector3.Dot(mf.transform.forward, Vector3.up) < .99f) wrong.Append(k + "; "); }
            }
            Object.DestroyImmediate(root.gameObject);
            Assert.GreaterOrEqual(meshes, FoodModels.Kept.Length);
            Assert.AreEqual("", wrong.ToString(), "on their sides: ");
        }

        [Test] public void Each_zones_fare_is_its_own()
        {
            var zones = FoodModels.Fare.Keys.ToArray(); var same = new StringBuilder();
            for (int i = 0; i < zones.Length; i++)
                for (int j = i + 1; j < zones.Length; j++)
                {
                    double o = Overlap(FoodModels.Fare[zones[i]].Select(b => b.path), FoodModels.Fare[zones[j]].Select(b => b.path));
                    if (o > .5) same.Append(zones[i] + " and " + zones[j] + " share " + o.ToString("0.00") + "; ");
                }
            Assert.AreEqual("", same.ToString(), "zones laid with much the same fare: ");
            foreach (var z in new[] { "zone.oakhaven", "zone.khaven", "zone.ashrim", "zone.peaks", "zone.crown", "zone.lowtowns", "zone.verdant" })
                Assert.IsTrue(FoodModels.Fare.ContainsKey(z), z + " has no fare of its own");
        }

        [Test] public void Each_caves_set_is_its_own()
        {
            var keys = PropDressing.Caves.Keys.ToArray(); var same = new StringBuilder();
            for (int i = 0; i < keys.Length; i++)
                for (int j = i + 1; j < keys.Length; j++)
                {
                    double o = Overlap(PropDressing.Caves[keys[i]].Select(p => p.path), PropDressing.Caves[keys[j]].Select(p => p.path));
                    if (o > .6) same.Append(keys[i] + " and " + keys[j] + " share " + o.ToString("0.00") + "; ");
                }
            Assert.AreEqual("", same.ToString(), "caves dressed with much the same set: ");
        }

        /// <summary>The way through stays clear: a cave's floor pieces stand at the walls (|x| at least PropDressing.CaveWay of the
        /// half-width), its wall pieces on the wall; a room's floor pieces out of its ways, with their reach (half a metre) round them.</summary>
        [Test] public void Floor_pieces_keep_off_the_way_through()
        {
            var bad = new StringBuilder();
            foreach (var kv in PropDressing.Caves)
                foreach (var p in kv.Value)
                {
                    if (p.place == PropDressing.Place.Floor && Mathf.Abs(p.x) < PropDressing.CaveWay) bad.Append(kv.Key + ": " + p.path + " at " + p.x + "; ");
                    if (p.place == PropDressing.Place.Wall && Mathf.Abs(p.x) < .85f) bad.Append(kv.Key + ": wall piece " + p.path + " off the wall; ");
                    if (Mathf.Abs(p.x) > 1) bad.Append(kv.Key + ": " + p.path + " in the rock; ");
                    if (p.place == PropDressing.Place.Floor && p.height > 1.6f && !p.solid) bad.Append(kv.Key + ": " + p.path + " is big and not solid; ");
                }
            foreach (var kv in PropDressing.Rooms)
                foreach (var p in kv.Value.pieces)
                {
                    if (p.place != PropDressing.Place.Floor) continue;
                    foreach (var way in kv.Value.ways)
                    {
                        var grown = new Rect(way.xMin - .5f, way.yMin - .5f, way.width + 1, way.height + 1);
                        if (grown.Contains(new Vector2(p.x, p.z))) bad.Append(kv.Key + ": " + p.path + " in a way at " + p.x + ", " + p.z + "; ");
                    }
                }
            Assert.AreEqual("", bad.ToString(), "in the way: ");
        }
    }
}
