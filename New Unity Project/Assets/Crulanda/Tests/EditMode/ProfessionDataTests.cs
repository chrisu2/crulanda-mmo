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
        /// <summary>Every JSON file in an EncounterContent folder (Items, Professions), as text.</summary>
        public static List<string> Texts(string folder)
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
                { var z = JsonUtility.FromJson<Crulanda.World.ZoneDefinition>(File.ReadAllText(f)); if (z.dungeon) continue; zones[Path.GetFileNameWithoutExtension(f)] = z; }   // a dungeon (the Sealed Adit) has no trade nodes yet
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
            CollectionAssert.AreEqual(ProfessionDatabase.StationKinds, Crulanda.World.ZoneBuilder.StationKinds, "The zone builder and the trades know the same kinds of station.");
        }

        // ---------- recipes and stations (DESIGN 5, 6.1, 7; BUILD_PLAN step 9) ----------
        /// <summary>The five charcoal recipes (DESIGN 5.1): one log of each tier's wood makes 1 to 5 charcoal at a forge or a fire, at
        /// Woodcutting 1, 20, 40, 60 and 80.</summary>
        [Test] public void Charcoal_IsMadeFromEachTiersWood()
        {
            var items = Items(); var db = Db(items);
            var expect = new[] { ("recipe.charcoal_oak", "mat.oak_log", 1, 1), ("recipe.charcoal_blackpine", "mat.blackpine_log", 20, 2), ("recipe.charcoal_stonepine", "mat.stonepine_log", 40, 3),
                ("recipe.charcoal_snag", "mat.snag_wood", 60, 4), ("recipe.charcoal_ghostoak", "mat.ghostoak_log", 80, 5) };
            CollectionAssert.AreEqual(expect.Select(e => e.Item1), db.RecipesFor("woodcutting").Select(r => r.id), "Woodcutting's recipes, easiest first.");
            foreach (var (id, log, skill, count) in expect)
            {
                var r = db.Recipe(id); Assert.NotNull(r, id);
                Assert.AreEqual("woodcutting", r.profession, id); Assert.AreEqual(skill, r.skill, id); Assert.AreEqual("forge|fire", r.station, id);
                Assert.AreEqual(1, r.inputs.Length, id); Assert.AreEqual(log, r.inputs[0].item, id); Assert.AreEqual(1, r.inputs[0].count, id);
                Assert.AreEqual("mat.charcoal", r.output, id); Assert.AreEqual(count, r.count, id);
                Assert.IsTrue(r.name.StartsWith("Charcoal from "), id + ": " + r.name);
                Assert.IsTrue(db.NodesFor("woodcutting").Exists(n => n.item == log && n.skill == skill), id + ": its wood is the windfall of its tier.");
            }
            Assert.AreEqual(5, db.Recipes.Count(r => r.output == "mat.charcoal"), "Five charcoal recipes, one per tier (other trades' recipes may follow).");
        }

        /// <summary>
        /// Cooking's ten recipes (BUILD_PLAN step 10, DESIGN 5.4): made at a fire, at Cooking 1 to 90, each from the makings the design
        /// gives it. What each makes is a food (healing over 10 s, out of combat) with the design's heal, level and value, labelled, in
        /// no trade bag, and better than any food a vendor sells to a character of its level (the vendors' lists, heal 150 to 640 by tier,
        /// and the hen-wife's eggs at the merchant's, 180, which is why griddle bread heals 190 where the design gave it 160).
        /// The meats it is cooked from are materials no vendor sells.
        /// </summary>
        [Test] public void Cooking_HasTheTenRecipesOfTheDesign()
        {
            var items = Items(); var db = Db(items);
            // Easiest first, as RecipesFor gives them: (recipe, skill, makings, output, count, heal, level, value).
            var expect = new[] {
                ("recipe.boar_stew", 1, "junk.boar_meat 2", "food.boar_stew", 1, 220, 1, 3),
                ("recipe.griddle_bread", 1, "mat.flour 1", "food.griddle_bread", 1, 190, 1, 1),
                ("recipe.hearth_cake", 5, "mat.flour 1, food.fresh_eggs 1", "food.hearth_cake", 2, 200, 1, 2),
                ("recipe.wolf_skewer", 20, "mat.wolf_haunch 2, mat.mourners_cap 1", "food.wolf_skewer", 1, 340, 5, 5),
                ("recipe.harrow_pasty", 30, "mat.flour 1, food.harrow_cheese 1, junk.boar_meat 1", "food.harrow_pasty", 1, 380, 6, 5),
                ("recipe.smoked_loin", 40, "junk.boar_meat 2, mat.tarnwort 1, mat.salt 1", "food.smoked_loin", 1, 520, 9, 8),
                ("recipe.salt_flank", 60, "mat.hound_flank 2, mat.salt 1", "food.salt_flank", 1, 700, 12, 12),
                ("recipe.cinder_loaf", 70, "mat.flour 2, mat.cinder_thistle 1, mat.charcoal 1", "food.cinder_loaf", 1, 660, 12, 8),
                ("recipe.mossback_chop", 80, "mat.mossback_chop 2, mat.dewfern 1", "food.mossback_chop", 1, 820, 14, 18),
                ("recipe.venison_pie", 90, "mat.venison 2, mat.flour 1", "food.venison_pie", 1, 900, 14, 16) };
            CollectionAssert.AreEqual(expect.Select(e => e.Item1), db.RecipesFor("cooking").Select(r => r.id), "Cooking's recipes, easiest first.");
            var vendorFood = items.Vendors.SelectMany(v => v.items ?? new string[0]).Append(EncounterSession.FreshEggs).Distinct().Select(items.Get).Where(d => d != null && d.food).ToList();
            Assert.IsNotEmpty(vendorFood);
            foreach (var (id, skill, makings, output, count, heal, level, value) in expect)
            {
                var r = db.Recipe(id); Assert.NotNull(r, id);
                Assert.AreEqual("cooking", r.profession, id); Assert.AreEqual(skill, r.skill, id); Assert.AreEqual("fire", r.station, id + " is cooked at a fire.");
                Assert.AreEqual(makings, string.Join(", ", r.inputs.Select(i => i.item + " " + i.count)), id);
                Assert.AreEqual(output, r.output, id); Assert.AreEqual(count, r.count, id);
                var d = items.Get(output); Assert.NotNull(d, output);
                Assert.AreEqual("consumable", d.kind, output); Assert.IsTrue(d.food, output + " is eaten, not drunk.");
                Assert.AreEqual(heal, d.heal, output); Assert.AreEqual(level, d.level, output); Assert.AreEqual(value, d.value, output);
                Assert.AreEqual(1, d.quality, output); Assert.AreEqual(20, d.stack, output); Assert.AreEqual(r.name, d.name, id + " is named for what it makes.");
                Assert.IsTrue(string.IsNullOrEmpty(d.pouch), output + " goes in no trade bag.");
                Assert.IsFalse(string.IsNullOrEmpty(d.description), output); Assert.AreEqual("GAME-ONLY", d.canonStatus, output);
                int best = vendorFood.Where(f => f.level <= level).Select(f => f.heal).DefaultIfEmpty(0).Max();
                Assert.Greater(heal, best, output + " heals more than any food a vendor sells at level " + level + " (the merchant's eggs among them).");
            }
            foreach (var meat in new[] { "junk.boar_meat", "mat.wolf_haunch", "mat.hound_flank", "mat.mossback_chop", "mat.venison" })
            {
                Assert.AreEqual("material", items.Get(meat).kind, meat);
                Assert.IsFalse(items.Vendors.Any(v => (v.items ?? new string[0]).Contains(meat)), meat + " comes from the beasts only.");
            }
        }

        /// <summary>DESIGN 5: what a recipe makes is worth at most half again what goes into it (value x count).</summary>
        [Test] public void Recipes_NeverBeatTheirInputs()
        {
            var items = Items(); var db = Db(items); var problems = new List<string>();
            Assert.IsNotEmpty(db.Recipes);
            foreach (var r in db.Recipes)
            {
                int made = items.Get(r.output).value * Math.Max(1, r.count), cost = 0;
                foreach (var i in r.inputs) cost += items.Get(i.item).value * Math.Max(1, i.count);
                if (made > 1.5f * cost) problems.Add(r.id + ": makes " + made + " gold of " + r.output + " from " + cost + " gold of inputs (at most " + 1.5f * cost + ")");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>DESIGN 5, 7: no gold loop. A recipe whose every input a vendor sells (the hen-wife's eggs at the merchant's
        /// included) never makes more than those inputs cost to buy; and no vendor sells what is gathered (ore, logs, herbs).</summary>
        [Test] public void Recipes_FromVendorGoods_NeverProfit()
        {
            var items = Items(); var db = Db(items); var problems = new List<string>();
            var sold = new HashSet<string>(items.Vendors.SelectMany(v => v.items ?? new string[0])) { EncounterSession.FreshEggs };
            foreach (var r in db.Recipes)
            {
                if (!r.inputs.All(i => sold.Contains(i.item))) continue;
                int made = items.Get(r.output).value * Math.Max(1, r.count), price = 0;
                foreach (var i in r.inputs) price += Inventory.Price(items.Get(i.item)) * Math.Max(1, i.count);
                if (made > price) problems.Add(r.id + ": its inputs cost " + price + " gold at a vendor and what it makes sells for " + made);
            }
            foreach (var n in db.Nodes) if (sold.Contains(n.item)) problems.Add(n.item + " is gathered (" + n.id + ") and a vendor sells it too");
            Assert.IsTrue(sold.Contains("mat.charcoal"), "Smiths sell charcoal (4 gold) for those with no wood to burn.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>
        /// Every recipe can be made somewhere: a kind of station it names stands in some zone, from the zones' data: a zone's own
        /// stations (ZoneStation), and the props that are stations (a forge, a bake oven, the herbalist's drying hut, an inn's
        /// kitchen, an inn's hearth). Each zone has the stations DESIGN 6.1 gives it (Khaven no forge, the Peaks no bench), and a
        /// zone's own stations are of a known kind, named, labelled and named once.
        /// </summary>
        [Test] public void EveryRecipeStation_ExistsSomewhere()
        {
            var db = Db(Items()); var zones = Zones(); var problems = new List<string>(); var everywhere = new HashSet<string>();
            var want = new Dictionary<string, string[]> { { "oakhaven", new[] { "bench", "fire", "forge" } }, { "khaven", new[] { "bench", "fire" } }, { "peaks", new[] { "fire", "forge" } },
                { "ashrim", new[] { "bench", "fire", "forge" } }, { "verdant", new[] { "bench", "fire", "forge" } } };
            foreach (var kv in zones)
            {
                var z = kv.Value; var kinds = new SortedSet<string>(StringComparer.Ordinal); var names = new HashSet<string>();
                foreach (var p in z.props ?? new Crulanda.World.ZoneProp[0])
                {
                    if (p == null) continue;
                    var k = p.kind == "inn" ? "fire" : Crulanda.World.ZoneBuilder.StationKind(p.kind);
                    if (k != null) kinds.Add(k);
                }
                foreach (var s in z.stations ?? new Crulanda.World.ZoneStation[0])
                {
                    if (s == null) { problems.Add(kv.Key + ": a null station"); continue; }
                    if (Array.IndexOf(ProfessionDatabase.StationKinds, s.kind) < 0) problems.Add(kv.Key + ": the station '" + s.name + "' is a '" + s.kind + "'");
                    if (string.IsNullOrEmpty(s.name)) problems.Add(kv.Key + ": a station at " + s.at + " has no name");
                    else if (!names.Add(s.name)) problems.Add(kv.Key + ": two stations are called '" + s.name + "'");
                    if (string.IsNullOrEmpty(s.canonStatus) || !s.canonStatus.StartsWith("GAME-ONLY")) problems.Add(kv.Key + ": the station '" + s.name + "' is not labelled GAME-ONLY");
                    if (s.variant < 0 || s.variant > 2) problems.Add(kv.Key + ": the station '" + s.name + "' has variant " + s.variant);
                    kinds.Add(s.kind);
                }
                if (want.TryGetValue(kv.Key, out var w) && !kinds.SequenceEqual(w)) problems.Add(kv.Key + ": stations " + string.Join(", ", kinds) + ", where DESIGN 6.1 has " + string.Join(", ", w));
                everywhere.UnionWith(kinds);
            }
            CollectionAssert.AreEquivalent(ProfessionDatabase.StationKinds, everywhere, "A forge, a bench and a fire each stand somewhere.");
            foreach (var r in db.Recipes) if (!ProfessionDatabase.Stations(r.station).Any(everywhere.Contains)) problems.Add(r.id + ": no " + r.station + " stands in any zone");
            foreach (var d in db.Order) if (!string.IsNullOrEmpty(d.station) && !ProfessionDatabase.Stations(d.station).Any(everywhere.Contains)) problems.Add(d.id + ": its " + d.station + " stands in no zone");
            Assert.IsEmpty(problems, string.Join("\n", problems));
            Assert.AreEqual("forge", Crulanda.World.ZoneBuilder.StationKind("forge")); Assert.AreEqual("fire", Crulanda.World.ZoneBuilder.StationKind("oven"));
            Assert.AreEqual("fire", Crulanda.World.ZoneBuilder.StationKind("kitchen")); Assert.AreEqual("bench", Crulanda.World.ZoneBuilder.StationKind("dryhut"));
            Assert.IsNull(Crulanda.World.ZoneBuilder.StationKind("kitchendoor")); Assert.IsNull(Crulanda.World.ZoneBuilder.StationKind("stall")); Assert.IsNull(Crulanda.World.ZoneBuilder.StationKind("tannery"));
        }

        // ---------- the crafts (DESIGN 5.2, 5.3; BUILD_PLAN steps 12 and 13) ----------
        /// <summary>The Blacksmith's 21 pieces by the skill each is made at, with its makings (DESIGN 5.2).</summary>
        static readonly (int skill, string item, string makings)[] Pieces = {
            (5, "craft.copper_cudgel", "mat.copper_bar 2, mat.oak_log 1"), (8, "craft.copper_buckler", "mat.copper_bar 1, mat.oak_log 2"),
            (12, "craft.copper_gauntlets", "mat.copper_bar 2"), (16, "craft.copper_jerkin", "mat.copper_bar 3"),
            (24, "craft.bogiron_hatchet", "mat.bogiron_bar 3, mat.blackpine_log 1"), (28, "craft.bogiron_helm", "mat.bogiron_bar 2"),
            (32, "craft.bogiron_greaves", "mat.bogiron_bar 3"), (36, "craft.bogiron_hauberk", "mat.bogiron_bar 4"),
            (44, "craft.ridgesteel_blade", "mat.ridgesteel_bar 3, mat.stonepine_log 1"), (48, "craft.ridgesteel_shield", "mat.ridgesteel_bar 2, mat.stonepine_log 2"),
            (52, "craft.ridgesteel_pauldrons", "mat.ridgesteel_bar 3"), (56, "craft.ridgesteel_cuirass", "mat.ridgesteel_bar 4"),
            (64, "craft.ashsteel_cleaver", "mat.ashsteel_bar 3, mat.snag_wood 1"), (68, "craft.ashsteel_helm", "mat.ashsteel_bar 3"),
            (72, "craft.ashsteel_sabatons", "mat.ashsteel_bar 3"), (76, "craft.ashsteel_hauberk", "mat.ashsteel_bar 4"),
            (84, "craft.veridian_warblade", "mat.veridian_bar 4, mat.ghostoak_log 1"), (88, "craft.veridian_shield", "mat.veridian_bar 3, mat.ghostoak_log 2"),
            (92, "craft.veridian_legplates", "mat.veridian_bar 4"), (96, "craft.veridian_breastplate", "mat.veridian_bar 5"),
            (100, "craft.heartwood_greatblade", "mat.veridian_bar 8, mat.ghostoak_log 2, mat.charcoal 4") };

        /// <summary>
        /// Blacksmithing (DESIGN 5.2): five smelts at a forge, each two of its tier's ore and a charcoal for one bar, at skill 1, 20, 40,
        /// 60 and 80; the bars are materials the ore-poke holds and selling them feeds the smith's ore; then the 21 pieces at the skills
        /// and from the makings the design gives, each named for what it makes, made of its own tier's bars.
        /// </summary>
        [Test] public void Blacksmithing_HasTheFiveSmeltsAndTwentyOnePieces()
        {
            var items = Items(); var db = Db(items);
            var smelts = new[] { ("recipe.copper_bar", "mat.copper_ore", "mat.copper_bar", 1, 4), ("recipe.bogiron_bar", "mat.bogiron_ore", "mat.bogiron_bar", 20, 9),
                ("recipe.ridgesteel_bar", "mat.adit_ore", "mat.ridgesteel_bar", 40, 11), ("recipe.ashsteel_bar", "mat.cinder_ore", "mat.ashsteel_bar", 60, 13), ("recipe.veridian_bar", "mat.veridian_ore", "mat.veridian_bar", 80, 19) };
            var all = db.RecipesFor("blacksmithing");
            Assert.AreEqual(31, all.Count, "Five smelts, 21 pieces and the five resonance-tempered weapons (the notice boards' shard).");
            Assert.IsTrue(all.All(r => r.station == "forge"), "Everything a smith makes is made at a forge.");
            foreach (var (id, ore, bar, skill, value) in smelts)
            {
                var r = db.Recipe(id); Assert.NotNull(r, id);
                Assert.AreEqual("blacksmithing", r.profession, id); Assert.AreEqual(skill, r.skill, id);
                Assert.AreEqual(ore + " 2, mat.charcoal 1", string.Join(", ", r.inputs.Select(i => i.item + " " + i.count)), id);
                Assert.AreEqual(bar, r.output, id); Assert.AreEqual(1, r.count, id);
                Assert.IsTrue(db.NodesFor("mining").Exists(n => n.item == ore && n.skill == skill), id + ": its ore is mined at the same tier.");
                var d = items.Get(bar); Assert.NotNull(d, bar);
                Assert.AreEqual("material", d.kind, bar); Assert.AreEqual(1, d.quality, bar); Assert.AreEqual(20, d.stack, bar); Assert.AreEqual(value, d.value, bar);
                Assert.AreEqual("forge.ore", d.trade, bar + " sold in a village feeds the smith's ore."); Assert.AreEqual("ore", d.pouch, bar + " goes in the ore-poke.");
                Assert.AreEqual(r.name, d.name, id); Assert.AreEqual("GAME-ONLY", d.canonStatus, bar); Assert.IsFalse(string.IsNullOrEmpty(d.description), bar);
            }
            foreach (var (skill, item, makings) in Pieces)
            {
                var r = all.Find(x => x.output == item); Assert.NotNull(r, item + " has a recipe.");
                Assert.AreEqual("recipe." + item.Substring("craft.".Length), r.id); Assert.AreEqual(skill, r.skill, item); Assert.AreEqual(1, r.count, item);
                Assert.AreEqual(makings, string.Join(", ", r.inputs.Select(i => i.item + " " + i.count)), item);
                var d = items.Get(item); Assert.NotNull(d, item); Assert.AreEqual(r.name, d.name, item + ": the recipe is named for what it makes.");
                string tier = item.Substring(6, item.IndexOf('_') - 6); if (tier == "heartwood") tier = "veridian";
                Assert.IsTrue(r.inputs.Any(i => i.item == "mat." + tier + "_bar"), item + " is made of " + tier + " bars.");
            }
            var tempered = all.Where(r => r.id.StartsWith("recipe.tempered_")).Select(r => r.id).ToList();
            Assert.AreEqual(5, tempered.Count, "The five resonance-tempered weapons (the notice boards' Aether-Geode shard; 2026-10-03).");
            Assert.IsTrue(tempered.All(id => db.Recipe(id).inputs.Any(i => i.item == "mat.geode_shard")), "Each needs the shard.");
            CollectionAssert.AreEqual(smelts.Select(x => x.Item1).Concat(Pieces.Select(p => "recipe." + p.item.Substring(6))).Concat(tempered).OrderBy(x => db.Recipe(x).skill).ThenBy(x => x, StringComparer.Ordinal), all.Select(r => r.id), "Easiest first.");
        }

        /// <summary>
        /// The crafted pieces sit on the generated-gear curve (DESIGN 5.2): each is gear of its slot, uncommon, required at its zone's top
        /// level less one, with the damage or armour and the value generated uncommon gear has at that level, and one stat of about a
        /// quarter of the curve's power. The capstone is the one rare: required at 12, 36 damage (under the 38 of every level-13 rare
        /// weapon), +5 Strength and +5 Stamina. Every piece has its own look in looks.json and a line in the world's voice.
        /// </summary>
        [Test] public void CraftedGear_SitsOnTheGeneratedCurve()
        {
            var items = Items(); var looks = GearLooks.Parse(Resources.Load<TextAsset>("Gear/looks").text);
            var crafted = items.Items.Values.Where(d => d.id.StartsWith("craft.", StringComparison.Ordinal)).ToList();
            CollectionAssert.AreEquivalent(Pieces.Select(p => p.item), crafted.Select(d => d.id), "The 21 pieces, and nothing else, are craft.* items.");
            foreach (var d in crafted)
            {
                Assert.AreEqual("gear", d.kind, d.id); Assert.GreaterOrEqual(ItemDatabase.SlotIndex(d.slot), 0, d.id);
                Assert.IsTrue(new[] { 1, 4, 8, 11, 14 }.Contains(d.level), d.id + " is required at a zone's top level less one (" + d.level + "; round 29: Khaven 8, the Peaks 11, the Rim and the Shore 14).");
                var l = looks.Resolve(d); Assert.IsFalse(l.fallback, d.id + " has its own look."); Assert.AreEqual(d.slot, GearLooks.Family(l.family).slot, d.id);
                Assert.IsFalse(string.IsNullOrEmpty(d.description), d.id); Assert.IsTrue(d.canonStatus != null && d.canonStatus.StartsWith("GAME-ONLY"), d.id);
                int stats = d.stamina + d.strength + d.agility + d.intellect + d.spirit;
                if (d.id == "craft.heartwood_greatblade")
                {
                    Assert.AreEqual(3, d.quality, "The capstone is rare."); Assert.AreEqual(14, d.level); Assert.AreEqual("mainhand", d.slot);
                    Assert.LessOrEqual(d.weaponDamage, 42); Assert.Less(d.weaponDamage, items.Get(ItemDatabase.GearId("mainhand", 15, 3, 0)).weaponDamage, "Under a generated level-15 rare.");
                    Assert.AreEqual(5, d.strength); Assert.AreEqual(5, d.stamina); Assert.AreEqual(10, stats);
                    continue;
                }
                var gen = items.Get(ItemDatabase.GearId(d.slot, d.level + 1, 2, 0));
                Assert.AreEqual(2, d.quality, d.id + " is uncommon.");
                Assert.That(d.weaponDamage, Is.InRange(gen.weaponDamage - 1, gen.weaponDamage + 1), d.id + ": damage on the curve at level " + (d.level + 1));
                Assert.That(d.armor, Is.InRange(gen.armor - 1, gen.armor + 1), d.id + ": armour on the curve at level " + (d.level + 1));
                Assert.AreEqual(gen.value, d.value, d.id + ": value is generated gear's.");
                Assert.AreEqual(gen.level, d.level, d.id);
                Assert.AreEqual(Mathf.Max(1, Mathf.RoundToInt((d.level + 1) * 1.35f / 4)), stats, d.id + ": one stat of about a quarter of the curve's power.");
                Assert.AreEqual(1, new[] { d.stamina, d.strength, d.agility, d.intellect, d.spirit }.Count(x => x > 0), d.id + " has one stat.");
                // Above the vendor's common of its level, which has no stat to speak of.
                var common = items.Get(ItemDatabase.GearId(d.slot, d.level + 1, 1, 0));
                Assert.Greater(d.weaponDamage + d.armor, common.weaponDamage + common.armor, d.id + " beats the vendor's common.");
            }
        }

        /// <summary>
        /// Alchemy (DESIGN 5.3): five draughts at a herbalist's bench, at skill 1, 20, 40, 60 and 80: the four that were already in the
        /// game and the new Tarnwater draught, each from its tier's herbs and a vial, named for what it makes. The Tarnwater draught
        /// heals 280 at level 6, between the Healing draught and the Salt-cured tonic.
        /// </summary>
        [Test] public void Alchemy_HasTheFivePotionsOfTheDesign()
        {
            var items = Items(); var db = Db(items);
            var expect = new[] { ("recipe.potion_minor", 1, "mat.yarrow 2, mat.vial 1", "potion.minor", 90), ("recipe.potion_healing", 20, "mat.mourners_cap 2, mat.yarrow 1, mat.vial 1", "potion.healing", 200),
                ("recipe.potion_tarn", 40, "mat.tarnwort 2, mat.mourners_cap 1, mat.vial 1", "potion.tarn", 280), ("recipe.potion_salt", 60, "mat.cinder_thistle 2, mat.salt 1, mat.vial 1", "potion.salt", 360),
                ("recipe.potion_dewfern", 80, "mat.dewfern 2, mat.tarnwort 1, mat.vial 1", "potion.dewfern", 520) };
            CollectionAssert.AreEqual(expect.Select(e => e.Item1), db.RecipesFor("alchemy").Select(r => r.id), "Alchemy's recipes, easiest first.");
            foreach (var (id, skill, makings, output, heal) in expect)
            {
                var r = db.Recipe(id); Assert.NotNull(r, id);
                Assert.AreEqual("alchemy", r.profession, id); Assert.AreEqual(skill, r.skill, id); Assert.AreEqual("bench", r.station, id + " is made at a herbalist's bench.");
                Assert.AreEqual(makings, string.Join(", ", r.inputs.Select(i => i.item + " " + i.count)), id); Assert.AreEqual(output, r.output, id); Assert.AreEqual(1, r.count, id);
                var d = items.Get(output); Assert.NotNull(d, output);
                Assert.AreEqual("consumable", d.kind, output); Assert.IsFalse(d.food, output + " is drunk, and works in a fight."); Assert.AreEqual(heal, d.heal, output);
                Assert.AreEqual(r.name, d.name, id + " is named for what it makes.");
            }
            var tarn = items.Get("potion.tarn");
            Assert.AreEqual(9, tarn.level); Assert.AreEqual(11, tarn.value); Assert.AreEqual(10, tarn.stack); Assert.AreEqual("GAME-ONLY", tarn.canonStatus); Assert.IsFalse(string.IsNullOrEmpty(tarn.description));
            Assert.IsFalse(items.Vendors.Any(v => (v.items ?? new string[0]).Contains("potion.tarn")), "The Tarnwater draught is made, not sold.");
        }

        /// <summary>
        /// The two crafts can be taken up and are worked beside their trade (DESIGN 4, 6.2): each says where it is taken up, what is said
        /// when it is taken up alone, and what its trade's people say: Brannoc Vell and Lisbet Crane in their own words, and a line for
        /// any other blacksmith or herbalist. The trainer roles are trades the villages have.
        /// </summary>
        [Test] public void Crafts_say_where_they_are_taken_up_and_their_trainers_have_words()
        {
            var db = Db(Items());
            foreach (var (id, trainer, role) in new[] { ("blacksmithing", "Brannoc Vell", "blacksmith"), ("alchemy", "Lisbet Crane", "herbalist") })
            {
                var d = db.Profession(id);
                Assert.AreEqual(role, d.trainerRole, id);
                Assert.IsFalse(string.IsNullOrEmpty(d.learnAt), id + " says where it is taken up."); Assert.IsFalse(string.IsNullOrEmpty(d.takeUp), id + " has its words for taking it up alone.");
                Assert.IsTrue(d.helping != null && d.helping.Contains("{name}"), id + ": the chat line for a helper names them.");
                Assert.IsNotNull(ProfessionDatabase.LineFor(d.trainerLines, trainer), id + ": " + trainer + " has a line.");
                Assert.AreNotEqual(ProfessionDatabase.LineFor(d.trainerLines, "Nobody In Particular"), ProfessionDatabase.LineFor(d.trainerLines, trainer), id + ": " + trainer + "'s line is their own.");
                Assert.IsNotNull(ProfessionDatabase.LineFor(d.trainerLines, "Nobody In Particular"), id + ": any other trainer has a line.");
                Assert.IsNotNull(ProfessionDatabase.LineFor(d.helpLines, trainer), id + ": " + trainer + " has words while helping.");
                Assert.IsNotNull(ProfessionDatabase.LineFor(d.helpLines, "Nobody In Particular"), id + ": and anyone of the trade.");
            }
            Assert.AreEqual("Mind the scale. Copper first; it forgives you.", ProfessionDatabase.LineFor(db.Profession("blacksmithing").trainerLines, "Brannoc Vell"));
            Assert.IsNull(ProfessionDatabase.LineFor(null, "Brannoc Vell")); Assert.IsNull(ProfessionDatabase.LineFor(new[] { new TradeLine { npc = "Oska", text = "Only for Oska." } }, "Brannoc Vell"));
            // A craft line with no text is refused with the rest of a bad file.
            var e = Assert.Throws<ArgumentException>(() => ProfessionDatabase.Parse(new[] { "{ \"professions\": [ { \"id\": \"tanning\", \"name\": \"Tanning\", \"kind\": \"craft\", \"trainerLines\": [ { \"npc\": \"Maud Tanner\" } ] } ] }" }, Items()));
            StringAssert.Contains("Profession 'tanning': a trainer line with no text", e.Message); StringAssert.Contains("Profession 'tanning': a craft with no station", e.Message);
        }
    }
}
