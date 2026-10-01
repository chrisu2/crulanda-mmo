using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The trades' content (EncounterContent/Professions/*.json) read against the real items: it parses, it holds what the
    /// design says, and a bad file is caught with every problem named.</summary>
    public class ProfessionDataTests
    {
        static List<string> Texts(string folder)
        {
            var texts = new List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", folder), "*.json")) texts.Add(File.ReadAllText(f));
            return texts;
        }
        public static ItemDatabase Items() { return ItemDatabase.Parse(Texts("Items")); }
        public static ProfessionDatabase Db(ItemDatabase items) { return ProfessionDatabase.Parse(Texts("Professions"), items); }

        [Test] public void ProfessionsFile_Parses()
        {
            var items = Items(); var db = Db(items);
            CollectionAssert.AreEqual(new[] { "mining", "woodcutting", "herbalism", "cooking", "blacksmithing", "alchemy" }, db.Order.ConvertAll(d => d.id));
            Assert.AreEqual(2, db.CraftSlots);
            foreach (var d in db.Order) { Assert.IsFalse(string.IsNullOrEmpty(d.name), d.id); Assert.IsFalse(string.IsNullOrEmpty(d.description), d.id + " has a page."); Assert.IsFalse(string.IsNullOrEmpty(d.canonStatus), d.id + " is labelled."); }
            // Gathering is three skills; two need a tool, and each tool teaches its own skill.
            Assert.AreEqual("gather", db.Profession("mining").kind); Assert.AreEqual("gather", db.Profession("woodcutting").kind); Assert.AreEqual("gather", db.Profession("herbalism").kind);
            Assert.AreEqual("tool.pick", db.Profession("mining").tool); Assert.AreEqual("mining", items.Get("tool.pick").teaches); Assert.AreEqual("tool", items.Get("tool.pick").kind);
            Assert.AreEqual("tool.hatchet", db.Profession("woodcutting").tool); Assert.AreEqual("woodcutting", items.Get("tool.hatchet").teaches);
            Assert.IsFalse(string.IsNullOrEmpty(db.Profession("mining").taught)); Assert.IsFalse(string.IsNullOrEmpty(db.Profession("woodcutting").taught));
            Assert.IsTrue(string.IsNullOrEmpty(db.Profession("herbalism").tool), "Herbs are picked with bare hands.");
            // Cooking is everyone's; the two crafts each have a station and a trade that teaches them.
            Assert.AreEqual("free", db.Profession("cooking").kind); Assert.AreEqual("fire", db.Profession("cooking").station);
            Assert.AreEqual("craft", db.Profession("blacksmithing").kind); Assert.AreEqual("forge", db.Profession("blacksmithing").station); Assert.AreEqual("blacksmith", db.Profession("blacksmithing").trainerRole);
            Assert.AreEqual("craft", db.Profession("alchemy").kind); Assert.AreEqual("bench", db.Profession("alchemy").station); Assert.AreEqual("herbalist", db.Profession("alchemy").trainerRole);
            Assert.IsTrue(ProfessionDatabase.FromTheStart(db.Profession("herbalism"))); Assert.IsTrue(ProfessionDatabase.FromTheStart(db.Profession("cooking")));
            Assert.IsFalse(ProfessionDatabase.FromTheStart(db.Profession("mining"))); Assert.IsFalse(ProfessionDatabase.FromTheStart(db.Profession("alchemy")));
            // Node kinds: one tier per zone for each gathering skill (1, 20, 40, 60, 80), each giving a material that stacks to 20.
            Assert.AreEqual(17, db.Nodes.Count);
            foreach (var skill in new[] { "mining", "woodcutting", "herbalism" })
            {
                var tiers = new SortedSet<int>(); var gives = new HashSet<string>();
                foreach (var n in db.NodesFor(skill)) { tiers.Add(n.skill); gives.Add(n.item); }
                CollectionAssert.AreEqual(new[] { 1, 20, 40, 60, 80 }, tiers, skill);
                Assert.AreEqual(5, gives.Count, skill + ": five materials, one per tier.");
            }
            foreach (var n in db.Nodes)
            {
                var item = items.Get(n.item);
                Assert.AreEqual("material", item.kind, n.id); Assert.AreEqual(1, item.quality, n.id); Assert.AreEqual(20, item.stack, n.id);
                Assert.IsFalse(string.IsNullOrEmpty(n.name), n.id); Assert.IsFalse(string.IsNullOrEmpty(n.prompt), n.id + " has a prompt for E."); Assert.IsFalse(string.IsNullOrEmpty(n.look), n.id);
                Assert.AreSame(n, db.Node(n.id));
            }
            Assert.AreEqual(4, db.Node("node.copper_rich").max); Assert.AreEqual(2, db.Node("node.copper_rich").min);
            Assert.AreEqual(90, db.Node("node.yarrow").respawn); Assert.AreEqual(1.5f, db.Node("node.yarrow").seconds); Assert.AreEqual(180, db.Node("node.oak").respawn);
            Assert.IsNull(db.Profession("fletching")); Assert.IsNull(db.Node(null)); Assert.IsNull(db.Recipe("recipe.none"));
            Assert.IsEmpty(db.RecipesFor("fletching"));
        }

        /// <summary>The session reads the trades from the content asset, so the asset has to list the file. The validation scripts
        /// register it (Crulanda > World > Build Oakhaven) before the tests run.</summary>
        [Test] public void EncounterAsset_ListsTheProfessionsFile()
        {
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            Assert.NotNull(content, "Encounter.asset loads.");
            Assert.IsTrue(content.professionFiles != null && Array.Exists(content.professionFiles, f => f != null && f.name == "professions"),
                "Encounter.asset does not list EncounterContent/Professions/professions.json. Run Crulanda > World > Build Oakhaven to register it.");
        }

        [Test] public void A_bad_profession_file_is_refused_with_every_problem_named()
        {
            var items = Items();
            string bad = "{ \"professions\": [" +
                "{ \"id\": \"mining\", \"name\": \"Mining\", \"kind\": \"gather\", \"tool\": \"potion.minor\" }," +
                "{ \"id\": \"mining\", \"name\": \"Twice\", \"kind\": \"gather\" }," +
                "{ \"id\": \"whittling\", \"kind\": \"pastime\", \"tool\": \"no.such.tool\", \"station\": \"lathe\" }," +
                "{ \"name\": \"Nameless\", \"kind\": \"craft\" }," +
                "{ \"id\": \"cooking\", \"name\": \"Cooking\", \"kind\": \"free\", \"station\": \"fire\" } ]," +
                "\"nodes\": [" +
                "{ \"id\": \"node.a\", \"profession\": \"mining\", \"item\": \"no.such.item\", \"skill\": 0, \"min\": 3, \"max\": 2, \"respawn\": 0 }," +
                "{ \"id\": \"node.a\", \"profession\": \"mining\", \"item\": \"potion.minor\" }," +
                "{ \"id\": \"node.b\", \"profession\": \"cooking\", \"item\": \"potion.minor\", \"skill\": 101 }," +
                "{ \"id\": \"node.c\", \"profession\": \"sailing\", \"item\": \"potion.minor\" } ]," +
                "\"recipes\": [" +
                "{ \"id\": \"recipe.a\", \"profession\": \"cooking\", \"station\": \"fire|kiln\", \"output\": \"no.such.item\", \"skill\": 200, \"count\": 0, \"inputs\": [ { \"item\": \"no.such.input\" }, { \"item\": \"potion.minor\", \"count\": 0 } ] }," +
                "{ \"id\": \"recipe.a\", \"profession\": \"cooking\", \"station\": \"fire\", \"output\": \"potion.minor\", \"inputs\": [ { \"item\": \"potion.minor\" } ] }," +
                "{ \"id\": \"recipe.b\", \"profession\": \"sailing\", \"output\": \"potion.minor\" } ] }";
            var e = Assert.Throws<ArgumentException>(() => ProfessionDatabase.Parse(new[] { bad }, items));
            foreach (var expected in new[] { "its tool 'potion.minor' is not an item of kind tool", "Duplicate profession 'mining'", "Profession 'whittling': no name", "unknown kind 'pastime'",
                "unknown tool 'no.such.tool'", "unknown station 'lathe'", "A profession has no id",
                "Item 'tool.hatchet' teaches unknown profession 'woodcutting'",
                "Node 'node.a': unknown item 'no.such.item'", "Node 'node.a': skill 0 is outside 1-100", "Node 'node.a': yield 3-2 is not a range", "Node 'node.a': respawn and seconds must be above zero",
                "Duplicate node 'node.a'", "Node 'node.b': 'cooking' is not a gathering skill", "Node 'node.b': skill 101 is outside 1-100", "Node 'node.c': unknown profession 'sailing'",
                "Recipe 'recipe.a': unknown station 'kiln'", "Recipe 'recipe.a': unknown output 'no.such.item'", "Recipe 'recipe.a': skill 200 is outside 1-100", "Recipe 'recipe.a': makes 0",
                "Recipe 'recipe.a': unknown input 'no.such.input'", "Recipe 'recipe.a': takes 0 of 'potion.minor'", "Duplicate recipe 'recipe.a'",
                "Recipe 'recipe.b': unknown profession 'sailing'", "Recipe 'recipe.b': no station", "Recipe 'recipe.b': no inputs" })
                StringAssert.Contains(expected, e.Message);
            Assert.Throws<ArgumentNullException>(() => ProfessionDatabase.Parse(new[] { "{}" }, null), "The trades are read against the items.");
            // A sound file with no trades at all is an empty database, and a blank file is skipped.
            var empty = ProfessionDatabase.Parse(new[] { "", "{}" }, new ItemDatabase());
            Assert.IsEmpty(empty.Order); Assert.AreEqual(2, empty.CraftSlots);
        }

        [Test] public void Stations_split_on_the_bar()
        {
            CollectionAssert.AreEqual(new[] { "forge", "fire" }, ProfessionDatabase.Stations("forge|fire"));
            CollectionAssert.AreEqual(new[] { "bench" }, ProfessionDatabase.Stations("bench"));
            Assert.IsEmpty(ProfessionDatabase.Stations(null)); Assert.IsEmpty(ProfessionDatabase.Stations(""));
        }
    }
}
