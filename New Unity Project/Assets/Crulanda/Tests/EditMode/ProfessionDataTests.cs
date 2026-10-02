using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>Every zone's data, by file name.</summary>
        static Dictionary<string, Crulanda.World.ZoneDefinition> Zones()
        {
            var zones = new Dictionary<string, Crulanda.World.ZoneDefinition>();
            foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones"), "*.json"))
                zones[Path.GetFileNameWithoutExtension(f)] = JsonUtility.FromJson<Crulanda.World.ZoneDefinition>(File.ReadAllText(f));
            return zones;
        }
        /// <summary>A zone's nodes, from its nodes array and its props: the node id, where it is put, and whether it is a herb prop.</summary>
        static List<(string node, Vector2 at, string where)> NodesOf(Crulanda.World.ZoneDefinition z)
        {
            var list = new List<(string, Vector2, string)>();
            foreach (var n in z.nodes ?? new Crulanda.World.ZoneNode[0]) if (n != null) list.Add((n.node, n.at, "node at " + n.at));
            foreach (var p in z.props ?? new Crulanda.World.ZoneProp[0]) if (p != null && !string.IsNullOrEmpty(p.node)) list.Add((p.node, p.at, p.kind + " prop '" + p.name + "' at " + p.at));
            return list;
        }

        [Test] public void EveryNodeAndRecipe_UsesKnownItems()
        {
            var items = Items(); var db = Db(items); var problems = new List<string>();
            foreach (var n in db.Nodes)
            {
                if (items.Get(n.item) == null) problems.Add(n.id + ": unknown item " + n.item);
                else if (items.Get(n.item).kind != "material") problems.Add(n.id + ": gives " + n.item + ", which is not a material");
                if (n.skill < 1 || n.skill > ProfessionDatabase.MaxSkill) problems.Add(n.id + ": skill " + n.skill);
                if (Array.IndexOf(new[] { "ore", "ore_rich", "windfall", "herb" }, n.look) < 0) problems.Add(n.id + ": no look the zone builder knows ('" + n.look + "')");
                if (n.look == "herb" && (n.variant < 0 || n.variant > 5)) problems.Add(n.id + ": herb variant " + n.variant);
                if ((n.look == "ore" || n.look == "ore_rich" || n.look == "windfall") && (n.variant < 0 || n.variant > 4)) problems.Add(n.id + ": variant " + n.variant + " (tiers 0-4)");
                if (db.Profession(n.profession)?.kind != "gather") problems.Add(n.id + ": its trade is not a gathering skill");
            }
            foreach (var r in db.Recipes)
            {
                if (items.Get(r.output) == null) problems.Add(r.id + ": unknown output " + r.output);
                foreach (var i in r.inputs) if (items.Get(i.item) == null) problems.Add(r.id + ": unknown input " + i.item);
                if (r.skill < 1 || r.skill > ProfessionDatabase.MaxSkill) problems.Add(r.id + ": skill " + r.skill);
                foreach (var s in ProfessionDatabase.Stations(r.station)) if (Array.IndexOf(ProfessionDatabase.StationKinds, s) < 0) problems.Add(r.id + ": station " + s);
            }
            // The zones' nodes name real kinds of node; a herb prop worked as a node is a herb; no two share a respawn key (name and metre).
            foreach (var kv in Zones())
            {
                var keys = new HashSet<string>();
                foreach (var (node, at, where) in NodesOf(kv.Value))
                {
                    var def = db.Node(node);
                    if (def == null) { problems.Add(kv.Key + ": " + where + " names the unknown node '" + node + "'"); continue; }
                    if (where.Contains(" prop ") && !(where.StartsWith("herb prop") && def.look == "herb")) problems.Add(kv.Key + ": " + where + " is worked as a " + def.look + " node; only herb props are, as herb nodes");
                    if (!keys.Add(def.name + "|" + Mathf.RoundToInt(at.x) + "|" + Mathf.RoundToInt(at.y))) problems.Add(kv.Key + ": two '" + def.name + "' within a metre at " + at + " would share a respawn");
                }
                foreach (var p in kv.Value.props ?? new Crulanda.World.ZoneProp[0])
                    if (p != null && !string.IsNullOrEmpty(p.node) && string.IsNullOrEmpty(p.interact)) problems.Add(kv.Key + ": prop '" + p.name + "' is a node with no E prompt (interact)");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>Oakhaven, tier 1 (DESIGN 2.4): ten ore (six seams outside and four rich ones on Crowsfoot Hollow's floor), eight
        /// windfalls and ten herbs (the eight Yarrow props and two more), every one at Oakhaven's skill, so a new character can start
        /// at once. The Yarrow props still give the quest's yarrow.</summary>
        [Test] public void Oakhaven_HasTenOreEightTimberTenHerbNodes()
        {
            var db = Db(Items()); var z = Zones()["oakhaven"]; var all = NodesOf(z);
            int Count(string look) { return all.Count(x => db.Node(x.node)?.look == look); }
            Assert.AreEqual(6, Count("ore"), "Six seams outside."); Assert.AreEqual(4, Count("ore_rich"), "Four rich seams in the Hollow.");
            Assert.AreEqual(4, z.nodes.Count(n => n != null && n.under), "The rich seams are on the cave's floor.");
            Assert.IsTrue(z.nodes.Where(n => n != null && n.under).All(n => db.Node(n.node).look == "ore_rich"));
            Assert.AreEqual(8, Count("windfall")); Assert.AreEqual(10, Count("herb"));
            foreach (var x in all) Assert.AreEqual(1, db.Node(x.node).skill, x.where + ": Oakhaven's nodes are tier 1.");
            var yarrows = z.props.Where(p => p != null && p.kind == "herb" && p.name == "Yarrow").ToList();
            Assert.AreEqual(8, yarrows.Count);
            foreach (var p in yarrows) { Assert.AreEqual("node.yarrow", p.node); Assert.AreEqual("item.yarrow", p.item, "The quest's yarrow is still given."); }
            Assert.IsTrue(z.nodes.Where(n => n != null && n.node == "node.yarrow").All(n => n.item == "item.yarrow"), "The two new yarrows count for the quest too.");
        }

        /// <summary>Each zone's tier: the skill every one of its nodes asks (DESIGN 2.4).</summary>
        static readonly Dictionary<string, int> Tiers = new Dictionary<string, int> { { "oakhaven", 1 }, { "khaven", 20 }, { "peaks", 40 }, { "ashrim", 60 }, { "verdant", 80 } };

        /// <summary>Every zone (DESIGN 2.4, BUILD_PLAN step 8): ten ore, eight windfalls and ten herbs, counting the herb props worked
        /// as nodes, every one a kind the trades know at the zone's tier, no two sharing a respawn (name and metre). Rich seams are
        /// the ones under (on a cave's floor): Crowsfoot Hollow's four and the Root-Mother's Deep's four; no other zone has one. A
        /// herb prop worked as a node still gives its quest's herb (the Yarrows, Khaven's seven Mourner's caps), and so do the new
        /// nodes of that herb.</summary>
        [Test] public void EveryZone_HasTenOreEightTimberTenHerbNodes()
        {
            var db = Db(Items()); var zones = Zones(); var problems = new List<string>();
            CollectionAssert.AreEquivalent(Tiers.Keys, zones.Keys, "The five zones, each with a tier.");
            foreach (var kv in zones)
            {
                var z = kv.Value; var all = NodesOf(z); string p = kv.Key + ": ";
                int Count(string look) { return all.Count(x => db.Node(x.node)?.look == look); }
                if (Count("ore") + Count("ore_rich") != 10) problems.Add(p + (Count("ore") + Count("ore_rich")) + " ore, not ten");
                if (Count("windfall") != 8) problems.Add(p + Count("windfall") + " windfalls, not eight");
                if (Count("herb") != 10) problems.Add(p + Count("herb") + " herbs, not ten");
                int rich = kv.Key == "oakhaven" || kv.Key == "verdant" ? 4 : 0;
                if (Count("ore_rich") != rich) problems.Add(p + Count("ore_rich") + " rich seams, not " + rich);
                var under = (z.nodes ?? new Crulanda.World.ZoneNode[0]).Where(n => n != null && n.under).ToList();
                if (under.Count != rich || under.Any(n => db.Node(n.node)?.look != "ore_rich")) problems.Add(p + "the nodes under are not its " + rich + " rich seams");
                var keys = new HashSet<string>();
                foreach (var (node, at, where) in all)
                {
                    var def = db.Node(node);
                    if (def == null) { problems.Add(p + where + " names the unknown node '" + node + "'"); continue; }
                    if (def.skill != Tiers[kv.Key]) problems.Add(p + where + " is a " + def.id + " (skill " + def.skill + "); the zone's tier is " + Tiers[kv.Key]);
                    if (!keys.Add(def.name + "|" + Mathf.RoundToInt(at.x) + "|" + Mathf.RoundToInt(at.y))) problems.Add(p + "two '" + def.name + "' within a metre at " + at);
                }
                foreach (var h in (z.props ?? new Crulanda.World.ZoneProp[0]).Where(h => h != null && h.kind == "herb" && !string.IsNullOrEmpty(h.item)))
                    if (string.IsNullOrEmpty(h.node)) problems.Add(p + "the herb prop '" + h.name + "' at " + h.at + " is not worked as a node");
                    else if (db.Node(h.node)?.name != h.name) problems.Add(p + "the herb prop '" + h.name + "' is worked as " + h.node + ", another herb");
                foreach (var n in (z.nodes ?? new Crulanda.World.ZoneNode[0]).Where(n => n != null && db.Node(n.node)?.look == "herb"))
                {
                    var quest = (z.props ?? new Crulanda.World.ZoneProp[0]).FirstOrDefault(h => h != null && h.kind == "herb" && h.node == n.node && !string.IsNullOrEmpty(h.item));
                    if (quest != null && n.item != quest.item) problems.Add(p + "the " + n.node + " at " + n.at + " does not give the quest's " + quest.item + " as the props do");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
            var khaven = zones["khaven"].props.Where(h => h != null && h.kind == "herb" && h.name == "Mourner's cap").ToList();
            Assert.AreEqual(7, khaven.Count, "Khaven's seven Mourner's caps.");
            foreach (var h in khaven) { Assert.AreEqual("node.mourners_cap", h.node); Assert.AreEqual("item.mourners_cap", h.item, "Wenna's quest still gets its caps."); }
        }

        [Test] public void Stations_split_on_the_bar()
        {
            CollectionAssert.AreEqual(new[] { "forge", "fire" }, ProfessionDatabase.Stations("forge|fire"));
            CollectionAssert.AreEqual(new[] { "bench" }, ProfessionDatabase.Stations("bench"));
            Assert.IsEmpty(ProfessionDatabase.Stations(null)); Assert.IsEmpty(ProfessionDatabase.Stations(""));
        }
    }
}
