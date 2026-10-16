using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Combat;
using Crulanda.Core;
using Crulanda.Encounter;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Lowtowns and the Rust Market (zone.lowtowns; Docs/ZONE_LOWTOWNS.md, Phase 9.1; levels 15-20), from its data
    /// (EncounterContent/Zones/lowtowns.json, made by tools/wip/lowtowns/make_lowtowns.py) and the rules it brings:
    /// - the zone reads, is 480 m, levels 15-20, biome "lowtowns", every prop a kind the builder knows, every place of section 2 named;
    /// - the people of section 3 are there (Gorm Rock-Wall an orc at the Spoon), every villager housed once in a named terrace;
    /// - the camps of section 4 stand at 15-20, the two elites at 20 and the rare, the Drain Courts' Weave-Eaters by night only;
    /// - the quests chain from Corwin's breadcrumb (after the Shore's last) through the five, every giver someone of the zone;
    /// - the road runs both ways: Oakhaven's south-east exit arrives by the Lowtowns' west exit and the other way about;
    /// - the kept-out list (section 9): none of Book 1's people, no machine among the props;
    /// - salt, the bone harpoon and blackened steel (WeaveEaterRules) do what section 4 says.
    /// </summary>
    public class LowtownsZoneTests
    {
        static string Content(string sub) { return Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", sub); }
        static ZoneDefinition Zone(string file) { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Content("Zones/" + file + ".json"))); }
        static List<string> Texts(string folder) { return Directory.GetFiles(Content(folder), "*.json").OrderBy(f => f, System.StringComparer.Ordinal).Select(File.ReadAllText).ToList(); }
        static ZoneDefinition z;
        [OneTimeSetUp] public void Load() { z = Zone("lowtowns"); }

        /// <summary>The kinds the builder knows (ZoneBuilder.BuildProps' switch, the Lowtowns' among them).</summary>
        static readonly string[] Kinds = { "tenement", "warehouse", "lane_wall", "brick_bridge", "drain_mouth", "dock", "barge", "market_hall", "dead_house", "holding_house",
            "town_wall", "chimney_stack", "lane_lamp", "cobbles", "puddle", "washing_line", "soup_hall", "toll_house", "dye_vat", "shard_rack",
            "inn", "kitchen", "forge", "oven", "tannery", "leathershop", "stall", "market", "well", "board", "cart", "wagon", "crates", "barrels", "hitch", "haystack", "brazier", "signpost", "ruin" };

        [Test] public void The_zone_reads_and_is_the_Lowtowns()
        {
            Assert.AreEqual("zone.lowtowns", z.id); Assert.AreEqual("The Lowtowns", z.displayName);
            Assert.AreEqual(480, z.size); Assert.AreEqual(15, z.levelMin); Assert.AreEqual(20, z.levelMax);
            Assert.AreEqual("lowtowns", z.biome); Assert.AreEqual("CANON-EXPANDED", z.canonStatus); Assert.IsFalse(z.dungeon);
            Assert.Greater(z.lighting.nightClear, 0, "The smog thins at night.");
            Assert.Less(z.lighting.fogEnd, 200, "Smog by day: the fog closes in.");
            float rain = z.weather.Where(w => w.kind == "rain" || w.kind == "storm").Sum(w => w.weight), all = z.weather.Sum(w => w.weight);
            Assert.Greater(rain / all, .35f, "Rain often.");
            foreach (var p in z.props) CollectionAssert.Contains(Kinds, p.kind, "A prop of kind " + p.kind + " at " + p.at);
            Assert.Greater(z.props.Count(p => p.kind == "tenement"), 120, "A dense town: the terraces.");
            foreach (var m in new[] { "crane", "engine", "pipe", "lift", "gondola" })
                Assert.IsFalse(z.props.Any(p => (p.kind + " " + p.name).ToLowerInvariant().Contains(m)), "No machines (Chris, 2026-10-11): no " + m + ".");
        }

        [Test] public void Every_place_of_the_design_is_there_with_a_view_for_the_tour()
        {
            var places = new[] { "The Gate Ward", "The Holding House", "The Rusty Spoon", "The Tenement Rows", "The Soup-Hall", "The Rust Market", "The Blind Seer's Antiquities",
                "The Iron Teeth's Warehouse", "The Shard-Burners' Yard", "The Docks", "The Drain Courts", "The Outfall", "The Forge District Gate", "The Chemical River" };
            foreach (var n in places)
            {
                var l = z.landmarks.SingleOrDefault(x => x.name == n); Assert.NotNull(l, n + " is a place.");
                Assert.IsFalse(string.IsNullOrEmpty(l.canonStatus), n + " is labelled."); Assert.AreNotEqual(Vector2.zero, l.view, n + " has a view for the capture tour.");
            }
            Assert.AreEqual("inn", z.props.Single(p => p.name == "The Rusty Spoon").kind, "The Spoon is walked into.");
            Assert.AreEqual(1, z.props.Single(p => p.name == "The Outfall").variant, "The Outfall is grated (the Sludge-Tunnels are not built).");
            Assert.IsTrue(z.props.Any(p => p.kind == "signpost" && p.name.Contains("dangerous, 18-20")), "A sign at the Outfall says dangerous, 18-20.");
            Assert.AreEqual(2, z.props.Single(p => p.name == "The Forge District's wall").variant, "The Forge District's gate is shut.");
            Assert.AreEqual(1, z.props.Single(p => p.name == "Argentis's outer wall").variant, "The outer gate stands open.");
            Assert.GreaterOrEqual(z.props.Count(p => p.kind == "drain_mouth"), 6, "The Drain Courts' mouths.");
            Assert.GreaterOrEqual(z.props.Count(p => p.kind == "dock"), 3); Assert.GreaterOrEqual(z.props.Count(p => p.kind == "brick_bridge"), 2, "Two brick bridges over the river.");
            Assert.GreaterOrEqual(z.props.Count(p => p.kind == "chimney_stack"), 5, "Stacks over the Forge District's wall.");
            Assert.IsTrue(z.water.Any(w => w.name == "The Chemical River" && w.depth > 1.9f), "The black river, deep enough to swim.");
        }

        [Test] public void The_people_of_the_design_are_there_and_every_villager_is_housed_once()
        {
            var residents = z.life.residents.ToDictionary(r => r.name);
            foreach (var n in new[] { "Edda Holt", "Ludo Brand", "Mother Sall", "Gorm Rock-Wall", "Gytha Pell", "Brother Aldo", "Old Wulf", "Tibb Marrow", "Ketta Rune", "Grimbold", "Osric Fenn" })
                Assert.IsTrue(residents.ContainsKey(n), n + " lives here.");
            Assert.AreEqual("orc", residents["Gorm Rock-Wall"].look, "Gorm is an orc (CANON, book1 ch.1:21).");
            Assert.AreEqual("inn", residents["Gorm Rock-Wall"].place, "Gorm drinks at the Spoon.");
            Assert.AreEqual("innkeeper", residents["Mother Sall"].role); Assert.IsTrue(residents["Mother Sall"].works);
            var housed = new Dictionary<string, int>();
            foreach (var h in z.life.households)
            {
                var house = z.props.Where(p => p.name == h.house).ToList();
                Assert.AreEqual(1, house.Count, h.house + " is one prop."); Assert.AreEqual("tenement", house[0].kind, h.house + " is a terrace (its middle door a home).");
                foreach (var m in h.members) housed[m.name] = (housed.TryGetValue(m.name, out var n) ? n : 0) + 1;
            }
            foreach (var n in VillageLife.VillagerNames(z.life).Take(z.life.villagers)) Assert.AreEqual(1, housed.TryGetValue(n, out var k) ? k : 0, n + " lives in one terrace.");
            foreach (var kept in new[] { "Valen", "Rill", "Silas Vane", "Veyra", "Kaelen", "Aethelgard", "Architect", "Horgus" })
            {
                Assert.IsFalse(z.life.residents.Any(r => r.name.Contains(kept)) || z.life.names.Any(n => n.Contains(kept)), kept + " is kept out (section 9).");
                foreach (var file in new[] { "Zones/lowtowns.json", "Quests/lowtowns.json", "Lore/lowtowns.json" })
                    StringAssert.DoesNotContain(kept, File.ReadAllText(Content(file)), kept + " is named nowhere in " + file + ".");
            }
            foreach (var file in new[] { "Zones/lowtowns.json", "Quests/lowtowns.json", "Lore/lowtowns.json" }) StringAssert.DoesNotContain("Anduin", File.ReadAllText(Content(file)), "No World of Warcraft names.");
        }

        [Test] public void The_camps_stand_at_15_to_20_with_two_elites_at_20_and_the_rare()
        {
            foreach (var c in z.camps) { Assert.That(c.levelMin, Is.InRange(15, 20), c.name); Assert.That(c.levelMax, Is.InRange(c.levelMin, 20), c.name); Assert.IsFalse(string.IsNullOrEmpty(c.canonStatus), c.name); }
            Assert.AreEqual(15, z.camps.Min(c => c.levelMin), "The zone starts at 15."); Assert.AreEqual(20, z.camps.Max(c => c.levelMax));
            var elites = z.camps.Where(c => c.elite && c.rare <= 0).ToList();
            CollectionAssert.AreEquivalent(new[] { "Mother of Drains", "Knuckle Brannoc" }, elites.Select(c => c.mob));
            Assert.IsTrue(elites.All(c => c.levelMin == 20), "The two elites at 20.");
            Assert.AreEqual("drainmother", elites.Single(c => c.mob == "Mother of Drains").tag); Assert.AreEqual("brannoc", elites.Single(c => c.mob == "Knuckle Brannoc").tag);
            var rare = z.camps.Last(); Assert.AreEqual("The Lantern-Eater", rare.mob); Assert.Greater(rare.rare, 0, "The rare comes last, on a chance.");
            foreach (var tag in new[] { "gutterhound", "dockrat", "weaveeater", "ironteeth", "sifter", "warder", "greytouched" }) Assert.IsTrue(z.camps.Any(c => c.tag == tag), "A camp of " + tag + ".");
            // The Drain Courts' Weave-Eaters come up at night; a few keep the deepest court by day; the Mother only after dark.
            Assert.GreaterOrEqual(z.camps.Count(c => c.look == "weaveeater" && c.night && !c.elite), 2);
            Assert.IsTrue(z.camps.Any(c => c.look == "weaveeater" && !c.night && !c.elite), "A few by day in the deepest court.");
            Assert.IsTrue(elites.Single(c => c.mob == "Mother of Drains").night);
            Assert.IsFalse(z.camps.Any(c => c.night && c.look != "weaveeater"), "Only the Eaters keep the night.");
            foreach (var e in z.camps.Where(c => c.elite)) Assert.AreNotSame(EliteMoves.For("nobody at all", false), EliteMoves.For(e.mob, false), e.mob + " has a move of its own.");
        }

        [Test] public void The_quests_chain_from_Corwin_through_the_five()
        {
            var db = QuestDatabase.Parse(Texts("Quests"));
            var crumb = db.Quests["main.lowtowns.0"];
            Assert.AreEqual("Corwin Ashby", crumb.giver); Assert.AreEqual("zone.oakhaven", crumb.zone); CollectionAssert.Contains(crumb.requires, "main.verdant.5", "After the Shore's last.");
            Assert.AreEqual(15, crumb.level);
            for (int i = 1; i <= 5; i++)
            {
                var q = db.Quests["main.lowtowns." + i]; Assert.AreEqual("main", q.kind); Assert.AreEqual("zone.lowtowns", q.zone);
                CollectionAssert.Contains(q.requires, "main.lowtowns." + (i - 1), q.id + " follows the one before.");
                Assert.That(q.level, Is.InRange(15, 20), q.id);
            }
            var people = new HashSet<string>(VillageLife.VillagerNames(z.life).Take(z.life.villagers).Concat(z.life.residents.Select(r => r.name)));
            var mine = db.Ordered.Where(q => q.zone == "zone.lowtowns").ToList();
            Assert.GreaterOrEqual(mine.Count(q => q.kind == "side" || q.kind == "npc"), 8, "About eight side and NPC quests.");
            foreach (var q in mine.Where(q => q.kind != "bounty")) { Assert.IsTrue(people.Contains(q.giver), q.id + ": " + q.giver + " lives here."); Assert.IsTrue(people.Contains(q.turnIn), q.id + ": " + q.turnIn); }
            Assert.IsTrue(db.Factions.ContainsKey("lowtowners"), "The Lowtowners' standing.");
            Assert.GreaterOrEqual(db.Documents.Keys.Count(k => k.StartsWith("doc.lowtowns.") && !db.Documents[k].IsLore), 5, "Five Chronicle pages.");
            Assert.AreEqual(10, db.Documents.Keys.Count(k => k.StartsWith("doc.lowtowns.") && db.Documents[k].IsLore), "Ten pages of the book of lore.");
            var log = new QuestLog(db, EncounterSession.FreshProgress()) { Say = (t, s) => { } };
            new Bounties(log).Pool("zone.lowtowns", out var common, out var rarePost);
            Assert.GreaterOrEqual(common.Count, 3, "The board's bounties."); Assert.NotNull(rarePost, "and its rare posting.");
        }

        [Test] public void The_road_runs_both_ways()
        {
            var oak = Zone("oakhaven");
            var there = oak.exits.Single(e => e.to == "zone.lowtowns"); var back = z.exits.Single(e => e.to == "zone.oakhaven");
            StringAssert.Contains("15-20", there.name, "Oakhaven's exit says the band.");
            Assert.That(Vector2.Distance(there.arrive, back.at), Is.InRange(back.radius + 1, 15), "Arriving from Oakhaven, just inside the way back.");
            Assert.That(Vector2.Distance(back.arrive, there.at), Is.InRange(there.radius + 1, 15), "Arriving in Oakhaven, just inside the way here.");
            Assert.AreEqual(z.spawns.player, there.arrive, "The player starts the zone where Oakhaven's road brings them.");
        }

        // ---------- the rules (WeaveEaterRules) ----------
        [Test] public void Salt_and_the_harpoon_hold_a_Weave_Eater_and_only_sting_anything_else()
        {
            Assert.IsTrue(WeaveEaterRules.IsWeaveEater("Weave-Eater", false)); Assert.IsTrue(WeaveEaterRules.IsWeaveEater("Mother of Drains", false));
            Assert.IsTrue(WeaveEaterRules.IsWeaveEater("The Lantern-Eater", false)); Assert.IsTrue(WeaveEaterRules.IsWeaveEater("anything", true));
            Assert.IsFalse(WeaveEaterRules.IsWeaveEater("Gutter-hound", false));
            var salt = WeaveEaterRules.Throw(WeaveEaterRules.Salt, 18, true); Assert.AreEqual("stun", salt.hold); Assert.AreEqual(4, salt.seconds, 1e-4f, "Calcified 4 s (section 4).");
            var harpoon = WeaveEaterRules.Throw(WeaveEaterRules.Harpoon, 18, true); Assert.AreEqual("root", harpoon.hold); Assert.Greater(harpoon.seconds, 0);
            Assert.Greater(harpoon.sting, salt.sting, "The harpoon bites harder.");
            var miss = WeaveEaterRules.Throw(WeaveEaterRules.Salt, 18, false); Assert.IsNull(miss.hold); Assert.Greater(miss.sting, 0, "On anything else, only the sting.");
            var items = ItemDatabase.Parse(Texts("Items"));
            Assert.AreEqual(WeaveEaterRules.Salt, WeaveEaterRules.ThrownKind(items.Get("throw.salt_twist")));
            Assert.AreEqual(WeaveEaterRules.Harpoon, WeaveEaterRules.ThrownKind(items.Get("throw.bone_harpoon")));
            Assert.IsNull(WeaveEaterRules.ThrownKind(items.Get("potion.salt")), "A tonic is drunk, not thrown.");
            CollectionAssert.Contains(items.StockFor("Tibb Marrow", "stranger", 18), "throw.salt_twist", "Tibb sells salt.");
            CollectionAssert.Contains(items.StockFor("Ketta Rune", "stranger", 18), "throw.bone_harpoon", "Ketta sells harpoons.");
        }

        [Test] public void Steel_through_a_Weave_Eater_comes_out_blackened_ten_percent_for_twenty_seconds()
        {
            Assert.IsTrue(WeaveEaterRules.Blackens(true, 3)); Assert.IsFalse(WeaveEaterRules.Blackens(true, 12), "A bolt from afar is no steel through it.");
            Assert.IsFalse(WeaveEaterRules.Blackens(false, 2), "Only a Weave-Eater blackens steel.");
            var stats = new StatBlock(); stats.SetBase(StatType.AttackPower, 100);
            var on = new StatusEffectRuntime(stats);
            Assert.IsTrue(on.Apply(WeaveEaterRules.Blackened(), 0));
            Assert.AreEqual(90, stats.Get(StatType.AttackPower), .01f, "-10% attack power.");
            Assert.IsTrue(on.Apply(WeaveEaterRules.Blackened(), 5), "A second blow refreshes it");
            Assert.AreEqual(90, stats.Get(StatType.AttackPower), .01f, "and does not stack.");
            Assert.AreEqual(20, on.Remaining(WeaveEaterRules.BlackenedId, 5), .01f, "Twenty seconds from the last blow.");
            Assert.AreEqual(WeaveEaterRules.BlackenedName, WeaveEaterRules.StatusName(WeaveEaterRules.BlackenedId));
        }
    }
}
