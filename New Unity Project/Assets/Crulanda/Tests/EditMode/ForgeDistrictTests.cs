using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Forge District (zone.forge; Docs/ZONE_FORGE_DISTRICT.md; mobs 19-22, the cap 20, no dungeon), from its data (EncounterContent/Zones,
    /// Quests and Lore/forge.json, made by tools/wip/forge/make_forge.py) and the rules it brings:
    /// - the zone reads, is 360 m, levels 19-22, biome "forge", not a dungeon, every prop a kind of its own (ZoneBuilder.Forge) or one of the
    ///   few shared small kinds (the inn's floor plan, a ruin); none of the Lowtowns' tenements or MedievalVillage1 kinds;
    /// - the camps stand at 19-22, beasts at 20 at most (the hide tables), no elite yet, the rares last;
    /// - the road runs both ways: the Lowtowns' Forge District Gate arrives by the Tithe Gate and the other way about;
    /// - the breadcrumb follows the Lowtowns' last main quest and turns in here; the chain's givers are the zone's people;
    /// - no machine words anywhere in its files; its own music set, played by no other zone.
    /// </summary>
    public class ForgeDistrictTests
    {
        static string Content(string sub) { return Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", sub); }
        static ZoneDefinition Zone(string file) { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Content("Zones/" + file + ".json"))); }
        [System.Serializable] class QFile { public QDef[] quests = new QDef[0]; }
        [System.Serializable] class QDef { public string id, kind, giver, turnIn, zone; public int level; public string[] requires = new string[0]; public string canonStatus; }
        static ZoneDefinition z;
        [OneTimeSetUp] public void Load() { z = Zone("forge"); }

        [Test] public void The_zone_reads_as_the_Forge_District()
        {
            Assert.AreEqual("zone.forge", z.id); Assert.AreEqual(360, z.size); Assert.AreEqual("forge", z.biome);
            Assert.AreEqual(19, z.levelMin); Assert.AreEqual(22, z.levelMax, "Mobs to 22 (Chris, 2026-10-19: \"new zone still up to lvl 22\").");
            Assert.IsFalse(z.dungeon, "No dungeon for now (Chris, 2026-10-19)."); Assert.IsFalse(z.city); Assert.IsFalse(z.raid);
            StringAssert.StartsWith("CANON", z.canonStatus);
            foreach (var n in new[] { "The Tithe Gate", "The Rust & Ruin", "The Forge Warrens", "The Foundry", "The Spoke", "The River Aureum", "The Molten Vein", "The Great Crucible", "The Slag Fields" })
                Assert.IsTrue(z.landmarks.Any(l => l.name == n), n + " is a landmark.");
            foreach (var l in z.landmarks) Assert.IsFalse(string.IsNullOrEmpty(l.canonStatus), l.name + " is labelled.");
        }

        [Test] public void Its_buildings_are_its_own()
        {
            var shared = new HashSet<string> { "inn", "ruin" };   // the inn's floor plan (in soot stone and iron, ZONE_FORGE_DISTRICT.md question 3) and fallen rubble
            foreach (var p in z.props)
                Assert.IsTrue(ZoneBuilder.ForgeKinds.Contains(p.kind) || shared.Contains(p.kind), p.name + ": '" + p.kind + "' is none of the Forge District's kinds.");
            foreach (var k in new[] { "the_spoke", "spoke_yard", "crucible", "foundry_hall", "furnace", "fire_pit", "slag_heap", "lodging_row", "molten_channel", "forge_bridge", "forge_gate" })
                Assert.IsTrue(z.props.Any(p => p.kind == k), "A " + k + " stands in the zone.");
            Assert.AreEqual(2, z.props.Count(p => p.kind == "molten_channel"), "The River Aureum and the Molten Vein.");
            Assert.AreEqual(1, z.props.Count(p => p.kind == "inn"), "One inn, the Rust & Ruin.");
            foreach (var p in z.props.Where(p => ZoneBuilder.ForgeBuilding(p.kind)))
                Assert.Greater(ZoneBuilder.ForgeFootprint(p).x, 0, p.name + " has a footprint (no rain, no node in it).");
        }

        [Test] public void Every_molten_channel_is_crossed_by_its_bridges_on_the_roads()
        {
            var bridges = z.props.Where(p => p.kind == "forge_bridge").ToList();
            Assert.GreaterOrEqual(bridges.Count, 4);
            foreach (var b in bridges)
            {
                Assert.IsTrue(z.roads.Any(r => Dist(b.at, r.points) < r.width / 2), b.name + " carries a road.");
                Assert.IsTrue(z.props.Where(p => p.kind == "molten_channel").Any(c => Dist(b.at, c.points) < 1.5f), b.name + " stands over a channel.");
            }
        }

        [Test] public void The_camps_stand_at_19_to_22_with_beasts_at_20_and_no_elite_yet()
        {
            Assert.GreaterOrEqual(z.camps.Length, 12);
            bool rareSeen = false;
            foreach (var c in z.camps)
            {
                Assert.That(c.levelMin, Is.InRange(19, 22), c.name); Assert.That(c.levelMax, Is.InRange(c.levelMin, 22), c.name);
                if (c.look == "wolf" || c.look == "boar" || c.look == "bear" || c.look == "stag") Assert.LessOrEqual(c.levelMax, 20, c.name + ": the hide tables stop at 20.");
                Assert.IsFalse(c.elite, c.name + ": no elite until its moves and a loot band past 20 are made (ZONE_FORGE_DISTRICT.md question 8).");
                if (c.rare > 0) rareSeen = true; else Assert.IsFalse(rareSeen, c.name + " comes after a rare: the rares come last.");
                Assert.IsFalse(string.IsNullOrEmpty(c.canonStatus), c.name + " is labelled.");
            }
            Assert.IsTrue(z.camps.Any(c => c.levelMax == 22), "Some ground at 22: harder for geared level-20s.");
            Assert.IsFalse(z.camps.Any(c => c.tag == "seeker" || (c.mob ?? "").Contains("Seeker") || (c.mob ?? "").Contains("Copper-Tithe")), "Seekers are the Transit Hub's; the Copper-Tithe are never enemies (DUNGEON_DESIGN.md).");
        }

        [Test] public void The_road_runs_both_ways_through_the_Forge_District_Gate()
        {
            var low = Zone("lowtowns");
            var there = low.exits.Single(e => e.to == "zone.forge"); var back = z.exits.Single(e => e.to == "zone.lowtowns");
            Assert.Less(Vector2.Distance(there.arrive, back.at), 15); Assert.Greater(Vector2.Distance(there.arrive, back.at), back.radius + 1);
            Assert.Less(Vector2.Distance(back.arrive, there.at), 15); Assert.Greater(Vector2.Distance(back.arrive, there.at), there.radius + 1);
            Assert.Less(Vector2.Distance(there.at, low.landmarks.Single(l => l.name == "The Forge District Gate").at), 8, "The Lowtowns' way out is at their Forge District Gate.");
            Assert.AreEqual(1, low.props.Single(p => p.name == "The Forge District's wall").variant, "The gate stands open.");
        }

        [Test] public void The_breadcrumb_follows_the_Lowtowns_and_the_chain_is_the_zones_people()
        {
            var quests = JsonUtility.FromJson<QFile>(File.ReadAllText(Content("Quests/forge.json"))).quests;
            var crumb = quests.Single(q => q.id == "main.forge.0");
            CollectionAssert.Contains(crumb.requires, "main.lowtowns.5", "After the Lowtowns' last main quest.");
            Assert.AreEqual("zone.lowtowns", crumb.zone); Assert.AreEqual("Edda Holt", crumb.giver); Assert.AreEqual("Hesta Wode", crumb.turnIn);
            var people = new HashSet<string>(z.life.residents.Select(r => r.name));
            for (int i = 1; i <= 4; i++)
            {
                var q = quests.Single(x => x.id == "main.forge." + i);
                CollectionAssert.Contains(q.requires, "main.forge." + (i - 1)); Assert.AreEqual("zone.forge", q.zone);
                Assert.IsTrue(people.Contains(q.giver) && people.Contains(q.turnIn), q.id + ": its people live here.");
            }
            foreach (var q in quests) { Assert.IsFalse(string.IsNullOrEmpty(q.canonStatus), q.id); Assert.LessOrEqual(q.level, 22, q.id); }
        }

        [Test] public void No_machine_words_and_its_own_music()
        {
            foreach (var f in new[] { "Zones/forge.json", "Quests/forge.json", "Lore/forge.json" })
            {
                var text = File.ReadAllText(Content(f)).ToLowerInvariant();
                foreach (var w in new[] { "machine", "engine", "clockwork", "gear", "piston", "pump", "valve", "conveyor", "crane", " lift", "golem", "drill", "cog", "boiler", "contraption" })
                    Assert.IsFalse(text.Contains(w), f + " says '" + w + "' (no machines, Chris 2026-10-11).");
            }
            Assert.IsTrue(ZoneMusic.Sets.ContainsKey("zone.forge"));
            var mine = ZoneMusic.Sets["zone.forge"];
            string Key(string[] a) { return string.Join("|", (a ?? new string[0]).OrderBy(s => s)); }
            foreach (var kv in ZoneMusic.Sets.Where(kv => kv.Key != "zone.forge"))
                Assert.AreNotEqual(Key(mine.day), Key(kv.Value.day), "No other zone plays the Forge District's day set (" + kv.Key + ").");
        }

        static float Dist(Vector2 p, Vector2[] pts)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector2 a = pts[i], ab = pts[i + 1] - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }
    }
}
