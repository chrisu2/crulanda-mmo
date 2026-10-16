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
    /// Hallow's Creek's data holds together (EncounterContent/Zones/hallows.json, written by tools/wip/dungeon/hallows_layout.py; Docs/DUNGEON2_DESIGN.md;
    /// Chris, 2026-10-16: "make sure there is a zone for 15-20 and a dungeon for the same range"): a dungeon of levels 15-20, its wings stepped
    /// 15-17 and 17-19 and its last bosses 20; every name one part of the file gives another resolves (camps, wings, gates, anchors, events,
    /// waves and the bosses they wait on); its open-ground decks are walkable slopes and every camp up on one stands on it; its fifteen elites
    /// each have a move and a signature list; its quests name what is there; its door is Oakhaven's Road's End. The run a sim leads goes road,
    /// bridge, street, (the Granary and the Cellars when it takes them), the Archive, the Warden's walk, the tower. No machines, no Hollow
    /// children, Vala Solari named once. Pure data and files: no scene, no session, no save. Every offender is listed, not only the first.
    /// </summary>
    public class HallowsDataTests
    {
        static ZoneDefinition zone, oakhaven;
        static readonly Dictionary<string, Hollow> caves = new Dictionary<string, Hollow>();
        static string Content { get { return Path.Combine(Application.dataPath, "Crulanda/EncounterContent"); } }
        static ZoneDefinition Read(string file) { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Content, "Zones/" + file + ".json"))); }

        [OneTimeSetUp] public void Load()
        {
            zone = Read("hallows"); oakhaven = Read("oakhaven");
            caves.Clear();
            foreach (var p in zone.props)
            {
                if (p == null || p.kind != "cavern" || string.IsNullOrEmpty(p.name) || p.plan == null || p.plan.Length < 10 || caves.ContainsKey(p.name)) continue;
                var rows = new List<float[]>(); for (int i = 0; i + 4 < p.plan.Length; i += 5) rows.Add(new[] { p.plan[i], p.plan[i + 1], p.plan[i + 2], p.plan[i + 3], p.plan[i + 4] });
                caves[p.name] = new Hollow(p.name, p.at, p.rotation, rows, (x, z) => 0f);
            }
        }
        static void Clean(List<string> bad, string what) { Assert.IsEmpty(bad, bad.Count + " of Hallow's Creek's " + what + ":\n  " + string.Join("\n  ", bad)); }
        static HashSet<string> Elites { get { return new HashSet<string>(zone.camps.Where(c => c != null && c.elite).Select(c => c.mob)); } }

        [Test] public void It_is_a_dungeon_for_levels_15_to_20_with_its_wings_stepped()
        {
            Assert.AreEqual("zone.hallows", zone.id); Assert.IsTrue(zone.dungeon, "a dungeon"); Assert.IsFalse(zone.raid);
            Assert.AreEqual(15, zone.levelMin); Assert.AreEqual(20, zone.levelMax);
            Assert.LessOrEqual(zone.levelMax, EncounterProgress.LevelCap, "inside the cap");
            var bad = new List<string>();
            foreach (var c in zone.camps)
                if (c.levelMin < 15 || c.levelMax > 20 || c.levelMin > c.levelMax) bad.Add(c.name + " is " + c.levelMin + "-" + c.levelMax);
            Clean(bad, "camps outside 15-20");
            int Top(string wing) { return zone.camps.Where(c => c.wing == wing).Max(c => c.levelMax); }
            int Low(string wing) { return zone.camps.Where(c => c.wing == wing).Min(c => c.levelMin); }
            Assert.AreEqual(15, Low("road"), "the Ash Road starts at 15"); Assert.LessOrEqual(Top("road"), 17); Assert.LessOrEqual(Top("bridge"), 17);
            foreach (var w in new[] { "street", "granary", "cellars", "archive" }) { Assert.GreaterOrEqual(Low(w), 17, w); Assert.LessOrEqual(Top(w), 19, w); }
            Assert.AreEqual(20, zone.camps.Single(c => c.mob == EncounterSession.ExaminerName).levelMax, "the Examiner, the last boss, is 20");
            Assert.AreEqual(20, zone.camps.Single(c => c.mob == "Pact-Captain Rowena Kersey").levelMax, "the Pact-Captain is 20");
            Assert.IsTrue(zone.camps.All(c => c.respawn >= 3600), "no respawns while inside (decision 11)");
        }

        [Test] public void Every_camp_stands_in_a_wing_behind_a_gate_it_has_with_guards_it_has()
        {
            var bad = new List<string>(); var names = new HashSet<string>(); var wings = new HashSet<string>(zone.wings.Select(w => w.id));
            var gates = new HashSet<string>(zone.gates.Select(g => g.title)); var camps = new HashSet<string>(zone.camps.Select(c => c.name));
            foreach (var c in zone.camps)
            {
                if (!names.Add(c.name)) bad.Add(c.name + " is named twice");
                if (!wings.Contains(c.wing ?? "")) bad.Add(c.name + " is in no wing ('" + c.wing + "')");
                if (!string.IsNullOrEmpty(c.gate) && c.gate != "none" && !gates.Contains(c.gate)) bad.Add(c.name + " waits at '" + c.gate + "', no gate of the zone");
                if (!string.IsNullOrEmpty(c.cave) && !caves.ContainsKey(c.cave)) bad.Add(c.name + " is in no cave ('" + c.cave + "')");
                if (!string.IsNullOrEmpty(c.guards) && c.guards != "none")
                    foreach (var g in c.guards.Split(',')) if (!camps.Contains(g.Trim())) bad.Add(c.name + " names guard camp '" + g.Trim() + "'");
                if (c.name.IndexOf("child", StringComparison.OrdinalIgnoreCase) >= 0 || c.mob.IndexOf("child", StringComparison.OrdinalIgnoreCase) >= 0) bad.Add(c.name + ": no Hollow children (decision 12)");
            }
            // Rares come last, so a visit without them moves nobody else's mobs.
            int firstRare = Array.FindIndex(zone.camps, c => c.rare > 0);
            Assert.Greater(firstRare, 0); Assert.IsTrue(zone.camps.Skip(firstRare).All(c => c.rare > 0), "the rares come last");
            Assert.AreEqual(3, zone.camps.Count(c => c.rare > 0), "the Old Ram, the Grey Carter and the Lamplighter");
            Clean(bad, "camps that do not resolve");
        }

        [Test] public void Every_after_wave_and_unless_names_what_is_there()
        {
            var bad = new List<string>(); var mobs = new HashSet<string>(zone.camps.Select(c => c.mob)); var elites = Elites;
            var events = zone.events.ToDictionary(e => e.id);
            foreach (var c in zone.camps.Where(c => !string.IsNullOrEmpty(c.after)))
            {
                var a = c.after;
                if (a.StartsWith("kill:", StringComparison.Ordinal)) { if (!mobs.Contains(a.Substring(5))) bad.Add(c.name + ": " + a); }
                else if (a.StartsWith("loose:", StringComparison.Ordinal))
                {
                    var lord = a.Substring(6); if (!elites.Contains(lord)) bad.Add(c.name + ": " + a + " is no elite");
                    var move = EliteMoves.For(lord, false); if (move.phases == null || !move.phases.Any(p => p.kind == "loose" && p.camp == c.name)) bad.Add(c.name + ": " + lord + "'s move has no loose phase for it");
                }
                else if (a.StartsWith("event:", StringComparison.Ordinal))
                {
                    var m = Regex.Match(a.Substring(6), @"^([a-z]+)(\d+)$");
                    if (!m.Success || !events.TryGetValue(m.Groups[1].Value, out var ev)) { bad.Add(c.name + ": " + a + " is no event's wave"); continue; }
                    int n = int.Parse(m.Groups[2].Value); if (n < 1 || n > ev.waves.Length || ev.waves[n - 1].camp != c.name) bad.Add(c.name + ": " + a + " is not that wave's camp");
                }
                else bad.Add(c.name + ": after '" + a + "'");
                if (c.step > 0) bad.Add(c.name + " is released later and has a step");
            }
            foreach (var ev in zone.events)
            {
                if (!zone.anchors.Any(x => x.name == ev.anchor)) bad.Add("event " + ev.id + " centres on '" + ev.anchor + "', no anchor");
                if (!zone.wings.Any(w => w.id == ev.wing)) bad.Add("event " + ev.id + " is in no wing");
                foreach (var w in ev.waves) if (!string.IsNullOrEmpty(w.unless) && !elites.Contains(w.unless)) bad.Add("event " + ev.id + ": a wave unless '" + w.unless + "', no elite");
            }
            // The design's three events: the bakery's Glitch (optional, a hold), the Warden's walk (escort, each wave unless its boss is down),
            // the Waking Hour (the Examiner's phase at half).
            Assert.AreEqual(new[] { EncounterSession.GlitchEvent, EncounterSession.WalkEvent, EncounterSession.WakingEvent }, zone.events.Select(e => e.id).ToArray());
            var walk = events[EncounterSession.WalkEvent];
            CollectionAssert.AreEqual(new[] { "Picker-Master Fulke Yelland", "First-Cut" }, walk.waves.Select(w => w.unless).ToArray(), "the Pact's wave unless the Picker-Master is down, the Hollow's unless First-Cut is");
            var vowell = EliteMoves.For(EncounterSession.ExaminerName, false);
            Assert.IsTrue(vowell.boss, "the Examiner is the end boss"); Assert.IsTrue(vowell.phases.Any(p => p.kind == "event" && p.camp == EncounterSession.WakingEvent && Mathf.Approximately(p.at, .5f)), "at half he rings the bell");
            Clean(bad, "releases and waves");
        }

        [Test] public void Every_gate_and_anchor_stands_somewhere_and_has_what_its_kind_needs()
        {
            var bad = new List<string>(); var elites = Elites;
            foreach (var g in zone.gates)
            {
                bool open = string.IsNullOrEmpty(g.cave);
                if (open && g.at == Vector2.zero) bad.Add(g.title + " stands nowhere");
                if (!open && (!caves.TryGetValue(g.cave, out var h) || g.along < 0 || g.along > h.Length)) bad.Add(g.title + " is not in its cave");
                switch (string.IsNullOrEmpty(g.kind) ? "bossdoor" : g.kind)
                {
                    case "bossdoor": if (!elites.Contains(g.opensOn ?? "")) bad.Add(g.title + " opens on '" + g.opensOn + "', no elite"); break;
                    case "pen": case "bar": case "hiddendoor": if (string.IsNullOrEmpty(g.prompt)) bad.Add(g.title + " is worked by hand and has no prompt"); break;
                    case "ward": break;   // the Warden unbinds it (EncounterSession.Hallows)
                    default: bad.Add(g.title + " is of kind '" + g.kind + "'"); break;
                }
                if (!string.IsNullOrEmpty(g.opensOn) && !elites.Contains(g.opensOn)) bad.Add(g.title + " opens on '" + g.opensOn + "'");
            }
            Assert.IsTrue(zone.gates.Any(g => g.title == EncounterSession.WardGate && g.kind == "ward"), "the tower's ward-lock");
            Assert.IsTrue(zone.gates.Any(g => g.title == EncounterSession.WardenCell && g.kind == "pen" && g.opensOn == "Registrar Dunstan Pardew"), "the Warden's cell, opened by the Registrar's death");
            foreach (var a in zone.anchors)
            {
                if (!string.IsNullOrEmpty(a.cave)) { if (!caves.TryGetValue(a.cave, out var h) || a.along < 0 || a.along > h.Length) bad.Add("anchor " + a.name + " is not in its cave"); }
                else if (a.at == Vector2.zero) bad.Add("anchor " + a.name + " stands nowhere");
            }
            foreach (var w in zone.wings) if (!string.IsNullOrEmpty(w.home) && !zone.anchors.Any(a => a.name == w.home)) bad.Add("wing " + w.id + " regroups at '" + w.home + "', no anchor");
            Clean(bad, "gates and anchors");
        }

        /// <summary>The open-ground decks (N1, N3): no ramp steeper than 30 degrees, and every camp up (or down) on one stands over one at its height.</summary>
        [Test] public void The_decks_are_walkable_and_every_raised_camp_stands_on_one()
        {
            var bad = new List<string>(); var open = zone.decks.Where(ZoneBuilder.OpenDeck).ToList();
            Assert.Greater(open.Count, 40, "the slabs, the lower orbit, the gatehouse, the tower, the Archive and the Granary");
            foreach (var d in open)
            {
                float run = Vector2.Distance(d.at, d.at2), deg = run < .01f ? 0 : Mathf.Atan2(Mathf.Abs(d.rise2 - d.rise), run) * Mathf.Rad2Deg;
                if (deg > 30.5f) bad.Add(d.name + " is " + deg.ToString("0.0") + " degrees");
            }
            bool OnDeck(Vector2 p, float y)
            {
                foreach (var d in open)
                {
                    var ab = d.at2 - d.at; float t = ab.sqrMagnitude < 1e-4f ? 0 : Mathf.Clamp01(Vector2.Dot(p - d.at, ab) / ab.sqrMagnitude);
                    float side = Vector2.Distance(p, d.at + ab * t); if (side > d.width / 2 + .2f) continue;
                    if (Mathf.Abs(Mathf.Lerp(d.rise, d.rise2, t) - y) < .3f) return true;
                }
                return false;
            }
            foreach (var c in zone.camps.Where(c => string.IsNullOrEmpty(c.cave) && Mathf.Abs(c.rise) > .01f))
                if (!OnDeck(c.center, c.rise)) bad.Add(c.name + " stands at " + c.rise + " over the yard with no deck under it");
            // The chasm: the slabs cross it, the pit is deep, and its fog sends a fall back (N3).
            var pit = zone.pits.Single(); Assert.Greater(pit.depth, 30); Assert.Less(pit.fog, -15); Assert.Greater(pit.fog, -pit.depth);
            Assert.AreEqual(1, zone.drops.Count(d => string.IsNullOrEmpty(d.fromCave)), "one drop: off the third slab to the lower orbit");
            Clean(bad, "decks");
        }

        [Test] public void Every_elite_has_its_move_and_its_signature_list_and_holds_reach_past_the_tank()
        {
            var bad = new List<string>(); var plain = EliteMoves.For("nobody at all", false);
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items);
            foreach (var c in zone.camps.Where(c => c.elite))
            {
                var m = EliteMoves.For(c.mob, false);
                if (ReferenceEquals(m, plain) || m.mob != c.mob) { bad.Add(c.mob + " has no move of its own"); continue; }
                if (string.IsNullOrEmpty(m.cc)) bad.Add(c.mob + " holds nobody (every hold reaches past the tank)");
                var lists = loot.Matching(new LootContext { zone = "hallows", tag = LootContext.CampTag(c), mob = c.mob, level = c.levelMax, elite = true }).Where(d => d.groups.Any(g => g.signature)).ToList();
                if (lists.Count != 1) bad.Add(c.mob + " has " + lists.Count + " signature lists");
            }
            Assert.AreEqual(15, zone.camps.Count(c => c.elite), "fifteen elites (DUNGEON2_DESIGN section 2)");
            Assert.AreEqual(new[] { EncounterSession.ExaminerName }, zone.camps.Where(c => c.elite && EliteMoves.For(c.mob, false).boss).Select(c => c.mob).ToArray(), "one end boss");
            Clean(bad, "elites");
        }

        /// <summary>The planner over the zone's wings (DungeonPlan, as AditDataTests drives it): straight-line distances, everything standing.</summary>
        static List<RouteStep> Plan(DungeonPlan.Mode mode, List<string> order)
        {
            Vector3 CampAt(int c) { var cp = zone.camps[c]; return new Vector3(cp.center.x, cp.rise, cp.center.y); }
            Vector3? AnchorAt(string n) { var a = zone.anchors.FirstOrDefault(x => x.name == n); if (a == null) return null; return string.IsNullOrEmpty(a.cave) ? new Vector3(a.at.x, a.rise, a.at.y) : caves[a.cave].At(a.along, a.aside); }
            return DungeonPlan.Plan(zone, mode, new Vector3(zone.spawns.player.x, 0, zone.spawns.player.y), CampAt, AnchorAt, Vector3.Distance, c => true, c => false, order);
        }
        [Test] public void The_planner_plans_the_quick_standard_and_full_runs()
        {
            var quickOrder = new List<string>(); var quick = Plan(DungeonPlan.Mode.Quick, quickOrder);
            var standardOrder = new List<string>(); var standard = Plan(DungeonPlan.Mode.Standard, standardOrder);
            var fullOrder = new List<string>(); var full = Plan(DungeonPlan.Mode.Full, fullOrder);
            Debug.Log("HallowsDataTests: quick " + string.Join(", ", quickOrder) + " (" + quick.Count + "); standard " + standard.Count + "; full " + string.Join(", ", fullOrder) + " (" + full.Count + ")");
            Assert.AreEqual("The Ash Road", quickOrder.First()); Assert.AreEqual("The Bell Tower", quickOrder.Last()); Assert.AreEqual("The Bell Tower", fullOrder.Last());
            CollectionAssert.AreEqual(new[] { "The Ash Road", "The Bridge of Shattered Glass", "Main Street", "The Old Archive", "The Bell Tower" }, quickOrder, "the quick run: road, bridge, street, Archive, tower");
            int Fight(List<RouteStep> run, string mob) { return run.FindIndex(s => s.camps.Any(i => zone.camps[i].mob == mob)); }
            Assert.AreEqual(-1, Fight(quick, "Picker-Master Fulke Yelland"), "the quick run leaves the Granary"); Assert.AreEqual(-1, Fight(quick, "The Reader at the Desk"));
            Assert.GreaterOrEqual(Fight(standard, "Picker-Master Fulke Yelland"), 0, "the standard run takes the Granary's bosses"); Assert.GreaterOrEqual(Fight(standard, "The Reader at the Desk"), 0, "and the Reader");
            Assert.AreEqual(-1, Fight(standard, "First-Cut"), "the Cellars are the full run's");
            Assert.Less(Fight(full, "First-Cut"), Fight(full, EncounterSession.ExaminerName), "the full run takes the Cellars before the tower");
            Assert.Less(quick.Count, standard.Count, "the standard run takes the side bosses"); Assert.Less(standard.Count, full.Count, "the full run takes every side room");
            foreach (var c in zone.camps.Where(c => c.elite && string.IsNullOrEmpty(c.after)))
                Assert.IsTrue(full.Any(s => s.camps.Any(i => zone.camps[i] == c)), "the full run fights " + c.name);
            Assert.IsTrue(quick.Last(s => !s.regroup).camps.Any(i => zone.camps[i].mob == EncounterSession.ExaminerName), "the run ends with the Examiner");
        }

        [Test] public void The_door_is_oakhavens_roads_end_and_the_way_back_and_the_queue_find_it()
        {
            var into = oakhaven.exits.SingleOrDefault(e => e.to == "zone.hallows"); Assert.NotNull(into, "Oakhaven's way in");
            StringAssert.Contains("Hallow's Creek", into.name); StringAssert.Contains("dangerous, 15-20", into.name);
            var roadsEnd = oakhaven.secrets.Single(s => s.id == "secret.oakhaven.roads-end");
            Assert.Less(Vector2.Distance(into.at, roadsEnd.at), 15, "at the Road's End");
            Assert.Less(Vector2.Distance(into.arrive, zone.spawns.player), 6, "you arrive in the yard at the milestone");
            Assert.IsTrue(zone.exits.Any(e => e.to == "zone.oakhaven"), "the Ash Road back");
            Assert.IsTrue(EncounterSession.QueueFits(zone, 15) && EncounterSession.QueueFits(zone, 20) && !EncounterSession.QueueFits(zone, 12), "the queue offers it at 15-20");
            Assert.IsFalse(string.IsNullOrEmpty(zone.ride.name), "the wagon home (N7)");
            Assert.IsTrue(zone.props.Any(p => p.kind == "ride" && p.name == zone.ride.name), "the wagon stands in the Granary's yard");
            Assert.IsTrue(Elites.Contains(zone.ride.opensOn), "the wagon is the Picker-Master's until he is down");
        }

        [Test] public void Its_quests_name_what_is_there_and_the_preservationists_have_a_second_line()
        {
            var db = LootTestData.Quests(); var bad = new List<string>();
            var ids = db.Ordered.Where(q => q.id.StartsWith("side.hallows.", StringComparison.Ordinal)).Select(q => q.id).ToList();
            CollectionAssert.AreEquivalent(new[] { "side.hallows.fourpence", "side.hallows.bureau_cuts", "side.hallows.reach_stone", "side.hallows.archive_warden", "side.hallows.wardens_papers", "side.hallows.copying_cells" }, ids);
            var tags = new HashSet<string>(zone.camps.Select(c => LootContext.CampTag(c)));
            var interacts = new HashSet<string>(zone.props.Where(p => !string.IsNullOrEmpty(p.interact)).Select(p => p.name));
            foreach (var id in ids)
                foreach (var st in db.Quests[id].steps)
                    foreach (var o in st.objectives)
                    {
                        if (o.zone != "zone.hallows") continue;
                        if (o.type == "kill" && !tags.Contains(o.target.Split('.')[1])) bad.Add(id + " kills '" + o.target + "'");
                        if (o.type == "interact" && !interacts.Contains(o.target)) bad.Add(id + " uses '" + o.target + "'");
                        if (o.type == "collect" && !o.target.StartsWith("kill:", StringComparison.Ordinal) && !interacts.Contains(o.target)) bad.Add(id + " collects from '" + o.target + "'");
                        if (o.type == "flag" && o.target != "key:" + EncounterSession.WardKey) bad.Add(id + " waits on '" + o.target + "'");
                    }
            Assert.AreEqual(3, ids.Count(i => db.Quests[i].giver == "Warden Ivel"), "the Preservationists' second quest line (decision 13)");
            Clean(bad, "quest objectives");
        }

        [Test] public void No_machines_and_vala_solari_named_once()
        {
            var texts = new[] { "Zones/hallows.json", "Quests/hallows.json", "Lore/hallows.json", "Items/loot.hallows.json" }.Select(f => File.ReadAllText(Path.Combine(Content, f))).ToList();
            var all = string.Join("\n", texts).ToLowerInvariant();
            foreach (var word in new[] { "machine", "engine", "clockwork", "cogwheel", "gearwheel", "piston", "boiler" })
                Assert.IsFalse(all.Contains(word), "no machines (DUNGEON_DESIGN 0.05): '" + word + "'");
            Assert.AreEqual(1, Regex.Matches(string.Join("\n", texts), "Vala Solari").Count - Regex.Matches(texts[0], "Vala Solari").Count, "Vala Solari named once, on the Warden's papers (decision 6)");
            Assert.IsTrue(all.Contains("gable"), "Gable on the baker's sign (decision 8)");
        }
    }
}
