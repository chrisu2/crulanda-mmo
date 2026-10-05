using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The loot files read from disk (EncounterContent/Items since step L2, beside the core item file), with the core items, the
    /// zones and the quests: shared by the loot data and roll tests.
    /// </summary>
    public static class LootTestData
    {
        public static string Content { get { return Path.Combine(Application.dataPath, "Crulanda", "EncounterContent"); } }
        /// <summary>Every loot.*.json in the Items folder, by file name (the order the scene builder registers them in).</summary>
        public static List<string> LootPaths()
        {
            var paths = new List<string>(Directory.GetFiles(Path.Combine(Content, "Items"), "loot.*.json"));
            paths.Sort((a, b) => string.CompareOrdinal(Path.GetFileName(a), Path.GetFileName(b)));
            return paths;
        }
        /// <summary>The item files that are not loot files (items.json and any the professions work adds).</summary>
        public static List<string> CorePaths()
        {
            var paths = Directory.GetFiles(Path.Combine(Content, "Items"), "*.json").Where(p => !Path.GetFileName(p).StartsWith("loot.", StringComparison.Ordinal)).ToList();
            paths.Sort(StringComparer.Ordinal); return paths;
        }
        public static List<string> Texts(IEnumerable<string> paths) { return paths.Select(File.ReadAllText).ToList(); }
        public static ItemDatabase Items() { return ItemDatabase.Parse(Texts(CorePaths().Concat(LootPaths()))); }
        public static LootDatabase Loot(ItemDatabase items, GearLooks looks = null) { return LootDatabase.Parse(Texts(LootPaths()), items, looks); }
        public static GearLooks Looks()
        {
            var text = Resources.Load<TextAsset>("Gear/looks"); Assert.NotNull(text, "Resources/Gear/looks.json is there.");
            return GearLooks.Parse(text.text);
        }
        public static List<ZoneDefinition> Zones()
        {
            return Directory.GetFiles(Path.Combine(Content, "Zones"), "*.json").OrderBy(p => p, StringComparer.Ordinal).Select(p => JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(p))).ToList();
        }
        public static QuestDatabase Quests() { return QuestDatabase.Parse(Texts(Directory.GetFiles(Path.Combine(Content, "Quests"), "*.json"))); }
        /// <summary>The five zones' short names in loot ids, and their zone ids without "zone.".</summary>
        public static readonly (string shortId, string zone)[] ZoneShorts = { ("oak", "oakhaven"), ("kha", "khaven"), ("pea", "peaks"), ("ash", "ashrim"), ("ver", "verdant") };
        /// <summary>Items on any signature list.</summary>
        public static HashSet<string> Signature(LootDatabase loot)
        {
            var s = new HashSet<string>();
            foreach (var d in loot.Drops) foreach (var g in d.groups) if (g.signature) foreach (var k in g.pick) s.Add(k.item);
            return s;
        }
    }

    /// <summary>
    /// The loot database's data (loot DESIGN.md 3 and 7, steps A3 and L2): the six files are item files, registered with the rest
    /// (the old drafts folder and its stand-in are gone); they parse with the core items, alone and together;
    /// ids follow the scheme and never go away; named gear has generated gear's armour, damage and value and its stat budget;
    /// every named item has a look, a source that exists, flavour and a canon label; every elite has a signature list and every
    /// zone named gear in every slot; drop groups, sets and effects stay inside the rules.
    /// </summary>
    public class LootDataTests
    {
        static readonly Regex IdScheme = new Regex(@"^loot\.(oak|kha|pea|ash|ver|world)\.[a-z0-9_]+$");
        static readonly Dictionary<string, string> FileShort = new Dictionary<string, string> { { "oakhaven", "oak" }, { "khaven", "kha" }, { "peaks", "pea" }, { "ashrim", "ash" }, { "verdant", "ver" }, { "world", "world" } };

        [Test] public void Every_item_file_parses_together()
        {
            var paths = LootTestData.LootPaths();
            Assert.AreEqual(6, paths.Count, "Six loot files: " + string.Join(", ", paths.Select(Path.GetFileName)));
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items, LootTestData.Looks());
            var named = items.Items.Keys.Where(k => k.StartsWith("loot.", StringComparison.Ordinal)).ToList();
            Assert.AreEqual(109, named.Count, "104 named items (ITEMS_V1.md) and the five legendaries (2026-10-05).");
            Assert.AreEqual(109 + 12, loot.Gear.Count, "A gear entry for each, and for the twelve named items already in the game.");
            Assert.AreEqual(12, loot.GearOrder.Count(g => g.legacy));
            Assert.AreEqual(2, loot.Sets.Count, "Two sets.");
            Assert.AreEqual(49, named.Count(id => items.Get(id).quality == 2), "49 uncommon.");
            Assert.AreEqual(48, named.Count(id => items.Get(id).quality == 3), "48 rare.");
            Assert.AreEqual(7, named.Count(id => items.Get(id).quality == 4), "7 epic.");
        }

        /// <summary>
        /// Step L2: the loot files sit with the item files (their .meta files moved with them, GUIDs kept), the drafts folder is
        /// gone, and Encounter.asset lists all seven item files. The validation scripts register them (Crulanda > World > Build
        /// Oakhaven) before the tests run.
        /// </summary>
        [Test] public void The_loot_files_are_registered_item_files()
        {
            Assert.IsFalse(Directory.Exists(Path.Combine(LootTestData.Content, "Loot")), "EncounterContent/Loot is gone: the loot files are item files.");
            var guids = new Dictionary<string, string> {
                { "loot.ashrim", "9f701a55d07c4f4299fbb7c48497aeb4" }, { "loot.khaven", "103ac81766944eeb9928acfa11d938c2" }, { "loot.oakhaven", "256143be1814496f9a1bddd5c4fc0913" },
                { "loot.peaks", "af637de598024e20b5544b2751570efa" }, { "loot.verdant", "8551b0a84e6e40eabece9e15ae762d17" }, { "loot.world", "09624b8f2d254830aac358cd2c9554c3" } };
            foreach (var kv in guids)
            {
                string path = "Assets/Crulanda/EncounterContent/Items/" + kv.Key + ".json";
                Assert.AreEqual(kv.Value, UnityEditor.AssetDatabase.AssetPathToGUID(path), path + " kept its GUID.");
            }
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            Assert.NotNull(content, "Encounter.asset loads.");
            var names = (content.itemFiles ?? new TextAsset[0]).Where(f => f != null).Select(f => f.name).ToList();
            foreach (var name in new[] { "items" }.Concat(guids.Keys))
                CollectionAssert.Contains(names, name, "Encounter.asset does not list " + name + ".json. Run Crulanda > World > Build Oakhaven to register it.");
            CollectionAssert.AreEqual(names.OrderBy(n => n, StringComparer.Ordinal).ToList(), names, "The item files are registered by name, so the drop lists roll in the tests' order.");
            Assert.IsNull(Resources.Load("Gear/LootDraft"), "The A3 stand-in (Resources/Gear/LootDraft.asset) is gone.");
        }

        [Test] public void Each_loot_file_parses_alone_beside_the_core_file()
        {
            var core = LootTestData.Texts(LootTestData.CorePaths());
            foreach (var path in LootTestData.LootPaths())
            {
                string text = File.ReadAllText(path), name = Path.GetFileName(path);
                try
                {
                    var items = ItemDatabase.Parse(core.Concat(new[] { text }));
                    var loot = LootDatabase.Parse(new[] { text }, items);
                    Assert.Greater(loot.Gear.Count, 0, name + " has gear entries.");
                }
                catch (ArgumentException e) { Assert.Fail(name + " does not parse beside the core items:\n" + e.Message); }
            }
        }

        [Test] public void Loot_ids_follow_the_scheme_and_match_their_file()
        {
            foreach (var path in LootTestData.LootPaths())
            {
                string name = Path.GetFileName(path), zone = name.Substring(5, name.Length - 10);
                Assert.IsTrue(FileShort.TryGetValue(zone, out var shortId), name + " is one of the six loot files.");
                var text = File.ReadAllText(path); var file = JsonUtility.FromJson<ItemFile>(text); var lootFile = JsonUtility.FromJson<LootFile>(text);
                foreach (var i in file.items)
                {
                    StringAssert.IsMatch(IdScheme.ToString(), i.id, name + ": " + i.id + " follows loot.<zone>.<snake_name>.");
                    Assert.AreEqual("loot." + shortId + ".", i.id.Substring(0, shortId.Length + 6), i.id + " is in its zone's file.");
                }
                Assert.AreEqual(0, (file.loot ?? new LootTableDef[0]).Length, name + " has no old-style loot tables (drops go in drops).");
                foreach (var g in lootFile.gear)
                    if (g.legacy) { Assert.AreEqual("world", zone, g.id + ": the twelve older named items are listed in loot.world.json."); StringAssert.StartsWith("item.", g.id); }
                    else Assert.IsTrue(file.items.Any(i => i.id == g.id), name + ": the gear entry " + g.id + " is for an item of this file.");
            }
            foreach (var path in LootTestData.CorePaths())
                Assert.IsFalse(JsonUtility.FromJson<ItemFile>(File.ReadAllText(path)).items.Any(i => i.id.StartsWith("loot.", StringComparison.Ordinal)), Path.GetFileName(path) + " holds no loot ids.");
        }

        [Test] public void Loot_id_baseline_still_resolves()
        {
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items);
            var baseline = File.ReadAllLines(Path.Combine(Application.dataPath, "Crulanda", "Tests", "EditMode", "LootIdBaseline.txt")).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")).ToList();
            Assert.GreaterOrEqual(baseline.Count, 104);
            foreach (var id in baseline)
            {
                Assert.NotNull(items.Get(id), id + " is in the baseline: saves may hold it, so it can never be renamed or removed.");
                Assert.NotNull(loot.Meta(id), id + " keeps its gear entry.");
            }
            foreach (var id in items.Items.Keys.Where(k => k.StartsWith("loot.", StringComparison.Ordinal))) CollectionAssert.Contains(baseline, id, id + " is new: add it to LootIdBaseline.txt.");
        }

        [Test] public void Named_gear_sits_on_the_generated_curve()
        {
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items); int n = 0;
            foreach (var g in loot.GearOrder.Where(g => !g.legacy))
            {
                var d = items.Get(g.id); var gen = items.Get(ItemDatabase.GearId(d.slot, d.level + 1, d.quality, 0)); n++;
                Assert.AreEqual(gen.weaponDamage, d.weaponDamage, d.id + ": weapon damage is generated gear's at level " + (d.level + 1) + " and its quality.");
                Assert.AreEqual(gen.armor, d.armor, d.id + ": armour is generated gear's.");
                Assert.AreEqual(gen.value, d.value, d.id + ": value is generated gear's.");
                Assert.AreEqual(gen.level, d.level, d.id + ": required level is the curve level less one.");
            }
            Assert.AreEqual(109, n);
        }

        [Test] public void Named_gear_stat_budgets_hold()
        {
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items); var signature = LootTestData.Signature(loot);
            foreach (var g in loot.GearOrder.Where(g => !g.legacy))
            {
                var d = items.Get(g.id); int curve = d.level + 1;
                float power = curve * (d.quality == 2 ? 1.35f : d.quality == 3 ? 1.7f : d.quality == 4 ? 2.1f : 2.5f), k = Mathf.Max(1, power / 4);
                float per = d.quality == 5 ? 7 : d.quality == 2 ? 4 : d.quality == 4 || signature.Contains(g.id) ? 6 : 5;
                int budget = Mathf.RoundToInt(per * k), total = d.stamina + d.strength + d.agility + d.intellect + d.spirit;
                bool charm = (g.effects ?? new GearEffect[0]).Any(e => e.kind == "luck");
                Assert.LessOrEqual(total, budget, d.id + ": " + total + " stat points against a budget of " + budget + " (" + per + "k, k = " + k + ").");
                if (!charm) Assert.GreaterOrEqual(total, budget - 1, d.id + " spends its budget (" + total + " of " + budget + ").");
            }
        }

        [Test] public void Every_named_item_has_meta_look_text_and_canon_label()
        {
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items);
            foreach (var d in items.Items.Values.Where(i => i.id.StartsWith("loot.", StringComparison.Ordinal)))
            {
                var g = loot.Meta(d.id); Assert.NotNull(g, d.id + " has a gear entry.");
                Assert.IsFalse(string.IsNullOrEmpty(g.look), d.id + " has a look.");
                Assert.AreEqual("gear", d.kind, d.id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(d.name), d.id + " has a name.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(d.description), d.id + " has its line of flavour.");
                Assert.AreEqual("GAME-ONLY", d.canonStatus, d.id + " is labelled GAME-ONLY.");
                Assert.That(d.quality, Is.InRange(2, 5), d.id + " is uncommon, rare, epic or legendary.");
                Assert.IsFalse((d.name + d.description).Contains("—"), d.id + ": no em-dashes.");
            }
            foreach (var g in loot.GearOrder.Where(g => g.legacy)) Assert.IsFalse(string.IsNullOrEmpty(items.Get(g.id).canonStatus), g.id + " keeps its canon label.");
        }

        [Test] public void Every_look_string_names_a_real_family_variant_and_palette()
        {
            var looks = LootTestData.Looks(); var items = LootTestData.Items();
            var before = new Dictionary<string, string>(); foreach (var kv in looks.Explicit) before[kv.Key] = kv.Value;
            var loot = LootTestData.Loot(items, looks);
            foreach (var g in loot.GearOrder)
            {
                var d = items.Get(g.id);
                Assert.IsTrue(looks.IsValid(g.look, out var why), g.id + ": " + why);
                Assert.IsTrue(GearLooks.TryParseLook(g.look, out var family, out var variant, out var palette, out bool glow), g.id);
                Assert.AreEqual(d.slot, GearLooks.Family(family).slot, g.id + " wears a " + d.slot + " family.");
                var l = looks.Resolve(d);
                Assert.IsFalse(l.fallback, g.id + " resolves to its own look once registered.");
                Assert.AreEqual(family + ":" + variant + "/" + palette, l.family + ":" + l.variant + "/" + l.palette, g.id);
                Assert.AreEqual(glow, l.forceGlow, g.id + " glows if its look says so.");
                if (before.TryGetValue(g.id, out var old)) Assert.AreEqual(old, g.look, g.id + ": the loot file and looks.json agree.");
            }
        }

        [Test] public void Every_named_item_has_a_source_and_every_source_exists()
        {
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items);
            foreach (var g in loot.GearOrder) Assert.NotNull(LootDatabase.SourceKind(g.source), g.id + " has a source (" + g.source + ").");
            var problems = LootDatabase.Validate(loot, LootTestData.Zones(), LootTestData.Quests());
            Assert.IsEmpty(problems, string.Join("\n", problems));
            var counts = loot.GearOrder.Where(g => !g.legacy).GroupBy(g => LootDatabase.SourceKind(g.source)).ToDictionary(x => x.Key, x => x.Count());
            Assert.AreEqual(15, counts["quest"], "Fifteen quest rewards.");
            Assert.AreEqual(5, counts["vendor"], "Five vendor pieces.");
            Assert.AreEqual(6, counts["world"], "Six world drops.");
            Assert.AreEqual(40, counts["boss"], "24 signature pieces, 5 from rare tables, 6 epics, 5 legendaries.");
            Assert.AreEqual(43, counts["mob"], "43 from ordinary mobs.");
        }

        [Test] public void A_mob_source_names_its_zone_by_its_display_name()
        {
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items); var zones = LootTestData.Zones();
            Func<string, string> name = s => { var z = zones.Find(x => x.id == "zone." + s); return z != null ? z.displayName : null; };
            foreach (var g in loot.GearOrder.Where(g => LootDatabase.SourceKind(g.source) == "mob"))
            {
                var zone = zones.Find(x => x.id == "zone." + g.source.Substring(g.source.IndexOf('@') + 1));
                Assert.AreEqual("Dropped in " + zone.displayName, LootDatabase.SourceText(g.source, name), g.id);
            }
            Assert.AreEqual("Dropped in The Ashland Rim", LootDatabase.SourceText("mob:cultist@ashrim", name));
            Assert.AreEqual("Dropped in Oakhaven", LootDatabase.SourceText("mob:wolf@oakhaven"), "Without names the short id is capitalised.");
            loot.ZoneName = name;
            var harrow = loot.GearOrder.First(g => g.source == "mob:any@oakhaven");
            StringAssert.EndsWith("Dropped in Oakhaven", loot.TooltipLines(harrow.id, null), "The tooltip uses the loader's names.");
        }

        [Test] public void Every_elite_camp_has_a_signature_list()
        {
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items); int elites = 0;
            foreach (var z in LootTestData.Zones())
                foreach (var c in z.camps.Where(c => c != null && c.elite))
                {
                    elites++;
                    var ctx = new LootContext { zone = z.id.Replace("zone.", ""), tag = LootContext.CampTag(c), mob = c.mob, level = c.levelMax, elite = true };
                    var lists = loot.Matching(ctx).Where(d => d.groups.Any(g => g.signature)).ToList();
                    Assert.AreEqual(1, lists.Count, c.mob + " (" + z.id + ") has one signature list.");
                    var pieces = lists[0].groups.Where(g => g.signature).SelectMany(g => g.pick).Select(k => k.item).ToList();
                    Assert.That(pieces.Count, Is.InRange(2, 3), c.mob + " has two or three signature pieces.");
                    Assert.IsTrue(pieces.All(p => loot.Meta(p).boss && loot.Meta(p).unique), c.mob + "'s pieces are boss pieces and unique.");
                    var normal = ctx; normal.elite = false;
                    Assert.IsFalse(loot.Matching(normal).Any(d => d.groups.Any(g => g.signature)), c.mob + "'s pack mates do not roll the list.");
                }
            Assert.AreEqual(12, elites, "Twelve elites.");
        }

        [Test] public void Every_zone_has_named_gear_for_every_slot()
        {
            var items = LootTestData.Items();
            foreach (var (shortId, zone) in LootTestData.ZoneShorts)
            {
                var slots = new HashSet<string>(items.Items.Values.Where(d => d.id.StartsWith("loot." + shortId + ".", StringComparison.Ordinal)).Select(d => d.slot));
                foreach (var slot in ItemDatabase.SlotIds) Assert.IsTrue(slots.Contains(slot), zone + " has named gear for the " + slot + " slot.");
            }
        }

        [Test] public void Drop_groups_are_sane()
        {
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items);
            Assert.Greater(loot.Drops.Count, 40);
            foreach (var d in loot.Drops)
                foreach (var g in d.groups)
                {
                    Assert.That(g.chance, Is.GreaterThan(0).And.LessThanOrEqualTo(1), d.id);
                    Assert.Greater(g.pick.Length, 0, d.id);
                    foreach (var k in g.pick)
                    {
                        Assert.Greater(k.weight, 0, d.id + ": " + k.item + " has a weight above 0.");
                        Assert.NotNull(items.Get(k.item), d.id + ": " + k.item + " is an item.");
                        Assert.NotNull(loot.Meta(k.item), d.id + ": " + k.item + " is named gear.");
                    }
                    if (g.signature) { Assert.AreEqual("elite", d.rank, d.id + ": signature lists are an elite's."); Assert.IsFalse(string.IsNullOrEmpty(d.mob), d.id + " names its elite."); Assert.IsFalse(g.lucky, d.id + ": luck does not touch a signature list."); }
                    bool epic = g.pick.All(k => items.Get(k.item).quality == 4);
                    if (g.pity > 0) Assert.IsTrue(epic, d.id + ": only epics have a pity count.");
                    if (epic && !string.IsNullOrEmpty(d.mob)) { Assert.IsTrue(g.lucky, d.id); Assert.That(g.pity, Is.EqualTo(10).Or.EqualTo(25), d.id + ": certain by the 10th (dungeon) or 25th (outdoor) dry kill."); Assert.That(g.chance, Is.EqualTo(.1f).Or.EqualTo(.04f), d.id); }
                    if (!g.signature && !epic) Assert.IsTrue(g.lucky, d.id + ": every other group is lucky.");
                    if (g.pick.All(k => items.Get(k.item).quality == 5)) Assert.That(g.chance, Is.LessThanOrEqualTo(.005f), d.id + ": a legendary is one in two hundred by day at most (Chris, 2026-10-05; Loot.LegendaryNight by night).");
                    if (string.IsNullOrEmpty(d.zone) && string.IsNullOrEmpty(d.mob)) Assert.That(g.chance, Is.LessThanOrEqualTo(.02f), d.id + ": world drops are rare.");
                }
        }

        [Test] public void Sets_have_real_pieces_in_distinct_slots_and_rising_bonuses()
        {
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items);
            CollectionAssert.AreEquivalent(new[] { "set.crowsfoot", "set.vigil" }, loot.Sets.Keys);
            foreach (var s in loot.SetOrder)
            {
                Assert.AreEqual(4, s.pieces.Length, s.id + " has four pieces.");
                var slots = s.pieces.Select(p => items.Get(p).slot).ToList();
                Assert.AreEqual(slots.Count, slots.Distinct().Count(), s.id + ": one piece per slot.");
                Assert.IsTrue(s.pieces.Any(p => p.StartsWith("item.", StringComparison.Ordinal)), s.id + " includes its boss's existing crown.");
                int last = 1;
                foreach (var b in s.bonuses) { Assert.Greater(b.count, last, s.id + ": bonuses rise."); Assert.LessOrEqual(b.count, s.pieces.Length); Assert.Greater(b.effects.Length, 0); last = b.count; }
                Assert.AreEqual(2, s.bonuses[0].count, s.id + " starts at two pieces.");
                foreach (var p in s.pieces) { Assert.AreEqual(s.id, loot.Meta(p).set, p + " names its set."); Assert.AreSame(s, loot.SetOf(p)); Assert.IsTrue(loot.Meta(p).unique, p + " is unique."); }
            }
        }

        [Test] public void Effects_use_only_kinds_the_game_carries_out_and_stay_inside_their_caps()
        {
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items);
            var effects = loot.GearOrder.SelectMany(g => g.effects ?? new GearEffect[0]).Concat(loot.SetOrder.SelectMany(s => s.bonuses).SelectMany(b => b.effects)).ToList();
            foreach (var e in effects)
            {
                CollectionAssert.Contains(LootDatabase.EffectKinds, e.kind);
                Assert.Greater(e.amount, 0, e.text); Assert.LessOrEqual(e.amount, GearEffects.Cap(e.kind), e.text);
                Assert.IsFalse(string.IsNullOrEmpty(e.text));
                if (e.kind == "stat") { Assert.IsTrue(Enum.TryParse(e.stat, out Crulanda.Core.StatType s), e.stat); CollectionAssert.Contains(LootDatabase.EffectStats, s, e.text); }
                else Assert.IsFalse(e.percent, e.text + ": only stats take percentages.");
            }
            var carriers = loot.GearOrder.Where(g => g.effects != null && g.effects.Length > 0).ToList();
            Assert.AreEqual(13, carriers.Count, "Thirteen items carry an effect: the seven epics and six rares.");
            Assert.IsTrue(items.Items.Values.Where(d => d.quality == 4 && d.id.StartsWith("loot.", StringComparison.Ordinal)).All(d => carriers.Any(c => c.id == d.id)), "Every epic has an effect.");
            Assert.IsTrue(carriers.All(c => items.Get(c.id).quality >= 3), "Only rares and epics carry effects.");
        }

        [Test] public void Crafted_gear_stays_under_named_rares_of_its_band()
        {
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items);
            var crafted = items.Items.Values.Where(d => d.id.StartsWith("craft.", StringComparison.Ordinal) && d.kind == "gear").ToList();
            if (crafted.Count == 0) Assert.Ignore("No crafted gear yet (the professions work adds it).");
            var rares = loot.GearOrder.Where(g => !g.legacy).Select(g => items.Get(g.id)).Where(d => d.quality == 3).ToList();
            foreach (var c in crafted)
                foreach (var r in rares.Where(r => r.slot == c.slot && GearLooks.BandOf(r.level + 1) == GearLooks.BandOf(c.level + 1) && r.level >= c.level))
                {
                    Assert.LessOrEqual(c.weaponDamage, r.weaponDamage, c.id + " stays under " + r.id + " in damage.");
                    Assert.LessOrEqual(c.armor, r.armor, c.id + " stays under " + r.id + " in armour.");
                    Assert.Less(c.stamina + c.strength + c.agility + c.intellect + c.spirit, r.stamina + r.strength + r.agility + r.intellect + r.spirit, c.id + " has fewer stat points than " + r.id + ".");
                }
        }
    }
}
