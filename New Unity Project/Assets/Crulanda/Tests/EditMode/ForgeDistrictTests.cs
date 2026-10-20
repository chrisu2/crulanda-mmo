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
            var low = Zone("lowtowns").worldMapPosition; var crown = Zone("crown").worldMapPosition;
            Assert.Greater(z.worldMapPosition.x, low.x, "Pinned east of the Lowtowns (Chris, 2026-10-19)."); Assert.Greater(z.worldMapPosition.y, crown.y, "Below the floating city.");
        }

        [Test] public void Its_buildings_are_its_own()
        {
            var shared = new HashSet<string> { "inn", "ruin" };   // the inn (its own variant 5, ZoneBuilder.RustRuin) and fallen rubble
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

        /// <summary>
        /// The Rust &amp; Ruin is its own inn (Chris, 2026-10-19: "Fully its own now"): variant 5, its own plan (the bar down the +x end, the fire
        /// pit out in the floor, the crews' long table, the booths), and no pocket: over the plan's solids (the walls, the bar, the fire pit)
        /// a grid flood from inside the door through every point keeping NavPad + the agent's radius + half a way of 0.9 m; every seat,
        /// stool, bench seat, the keeper's place and the rooms door lie within a step of that way (as the Rusty Spoon's, InnLayoutTests).
        /// </summary>
        [Test] public void The_Rust_and_Ruin_is_its_own_inn_with_a_wide_way_to_the_door()
        {
            var inn = z.props.Single(p => p.kind == "inn" && p.name == "The Rust & Ruin");
            Assert.AreEqual(ZoneBuilder.InnPlan.RuinVariant, inn.variant, "Inn variant 5, its own house (ZoneBuilder.RustRuinInn).");
            var plan = ZoneBuilder.InnPlan.For(inn.size, inn.variant); var inner = plan.Inner;
            Assert.IsTrue(plan.ruin && !plan.spoon && !plan.city && !plan.waystation, "Its own plan.");
            Assert.AreEqual(2.6f, plan.doorW, 1e-4f, "The double door every inn has.");
            Assert.Greater(plan.bar.x, inner.x - 2, "The bar stands down the +x end (the Spoon's is down the -x end, the taverns' along the back wall).");
            Assert.Greater(plan.barSize.y, plan.barSize.x, "It runs along z.");
            Assert.Less(Mathf.Abs(plan.hearth.x), inner.x - 3, "The fire pit stands out in the floor, not against a wall.");
            Assert.Less(Mathf.Abs(plan.hearth.y), inner.y - 2);
            Assert.Greater(plan.longTableSize.x, 4, "The crews' long table."); Assert.AreEqual(10, plan.seats.Count(s => Mathf.Abs(s.face.y - plan.longTable.y) < .01f && Mathf.Abs(s.face.x - plan.longTable.x) < plan.longTableSize.x / 2));
            Assert.AreEqual(4, plan.stools.Count); Assert.AreEqual(2, plan.benchSeats.Count); Assert.GreaterOrEqual(plan.seats.Count, 24);
            foreach (var (at, _) in plan.stools) Assert.Less(at.x, plan.bar.x - plan.barSize.x / 2, "A stool stands before the bar, on the room's side.");
            Assert.Greater(plan.stand.y, plan.bar.y + plan.barSize.y / 2 + .5f, "The keeper keeps the bar's back end.");
            Assert.AreEqual("bar", z.life.residents.Single(r => r.name == "Berrow Orr").place, "Berrow Orr works the bar.");
            const float way = .9f, step = .05f; float keep = ZoneBuilder.NavPad + ZoneBuilder.NavAgentRadius + way / 2, reach = way / 2 + .15f;
            var solids = plan.Furniture().Where(f => f.what == "the bar" || f.what == "the hearth").ToList();
            Assert.AreEqual(2, solids.Count, "The bar and the fire pit (the tables, benches and stools are walked between).");
            float Outside(Vector2 p, Vector2 at, Vector2 half) { return new Vector2(Mathf.Max(0, Mathf.Abs(p.x - at.x) - half.x), Mathf.Max(0, Mathf.Abs(p.y - at.y) - half.y)).magnitude; }
            float Clear(Vector2 p) { float c = Mathf.Min(inner.x - Mathf.Abs(p.x), inner.y - Mathf.Abs(p.y)); foreach (var (_, at, half) in solids) c = Mathf.Min(c, Outside(p, at, half)); return c; }
            int nx = Mathf.CeilToInt(2 * inner.x / step), nz = Mathf.CeilToInt(2 * inner.y / step);
            Vector2 Cell(int i, int j) { return new Vector2(-inner.x + (i + .5f) * step, -inner.y + (j + .5f) * step); }
            var wide = new bool[nx, nz]; for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++) wide[i, j] = Clear(Cell(i, j)) >= keep;
            var reached = new bool[nx, nz]; var queue = new Queue<(int, int)>(); var seed = (nx / 2, Mathf.FloorToInt((keep + .1f) / step));
            Assert.IsTrue(wide[seed.Item1, seed.Item2], "The way starts inside the door."); reached[seed.Item1, seed.Item2] = true; queue.Enqueue(seed);
            while (queue.Count > 0)
            {
                var (i, j) = queue.Dequeue();
                foreach (var (a, b) in new[] { (i + 1, j), (i - 1, j), (i, j + 1), (i, j - 1) })
                    if (a >= 0 && b >= 0 && a < nx && b < nz && wide[a, b] && !reached[a, b]) { reached[a, b] = true; queue.Enqueue((a, b)); }
            }
            float ToWay(Vector2 p)
            {
                float best = float.MaxValue; int r = Mathf.CeilToInt(2 / step), ci = Mathf.FloorToInt((p.x + inner.x) / step), cj = Mathf.FloorToInt((p.y + inner.y) / step);
                for (int i = Mathf.Max(0, ci - r); i < Mathf.Min(nx, ci + r); i++) for (int j = Mathf.Max(0, cj - r); j < Mathf.Min(nz, cj + r); j++) if (reached[i, j]) best = Mathf.Min(best, Vector2.Distance(p, Cell(i, j)));
                return best;
            }
            var places = plan.seats.Select(s => ("a seat", s.at)).Concat(plan.stools.Select(s => ("a stool", s.at))).Concat(plan.benchSeats.Select(s => ("a bench seat", s.at)))
                .Concat(new[] { ("the keeper's place", plan.stand), ("the rooms door", new Vector2(plan.roomsX, plan.d / 2 - .7f)) });
            var problems = new List<string>();
            foreach (var (what, at) in places)
            {
                float dd = ToWay(at); if (dd > reach) problems.Add(what + " at " + at + " is " + (dd > 2 ? "over 2" : dd.ToString("0.00")) + " m from a way " + way + " m wide to the door");
                foreach (var (f, fat, half) in solids) if (Outside(at, fat, half) < (what.StartsWith("a ") ? .3f : .5f)) problems.Add(what + " at " + at + " is in " + f);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
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
