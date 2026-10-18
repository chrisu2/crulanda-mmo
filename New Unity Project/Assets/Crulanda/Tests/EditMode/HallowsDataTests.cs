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
            // A camp released later ("after": the cellar trapdoors' "pull:") may follow the rares: it draws nothing from the camps' dice
            // (SpawnCamps skips it), and putting it before them would shift the rares' indices, which mob ids and lockouts key on.
            Assert.Greater(firstRare, 0); Assert.IsTrue(zone.camps.Skip(firstRare).All(c => c.rare > 0 || !string.IsNullOrEmpty(c.after)), "the rares come last (only released-later camps after them)");
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
                // (pick 6) a cellar trapdoor's Hollow, up when a camp there from the start is pulled (HollowRules.PullKey; b1 ch.4:161)
                else if (a.StartsWith(HollowRules.PullKey, StringComparison.Ordinal)) { var on = zone.camps.FirstOrDefault(x => x.name == a.Substring(HollowRules.PullKey.Length)); if (on == null || !string.IsNullOrEmpty(on.after)) bad.Add(c.name + ": " + a + " is no camp there from the start"); }
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
            // The jump is a player's, onto the low slab under the third slab's lip; the paths go down the stair of small slabs (hallows-1, 2026-10-16:
            // as a navmesh link over a six-metre gap the run bot walked off after the leader into the fog for four minutes).
            var jump = zone.drops.Single(d => string.IsNullOrEmpty(d.fromCave)); Assert.IsTrue(jump.scenery, "the jump is no navmesh link");
            var low = zone.decks.Single(d => d.name == "The low slab"); var third = zone.decks.Single(d => d.name == "The third slab");
            Assert.Greater(low.at.x + low.width / 2, third.at.x - third.width / 2 + .4f, "the low slab runs under the third slab's lip");
            Assert.Less(Mathf.Abs(jump.toAt.x - low.at.x), low.width / 2 - 1, "the jump lands on the low slab");
            // Broad enough for a party between the parapets (hallows-3, 2026-10-16): the ramps and the stair of small slabs 3.5 m and more, the
            // step-stones 5 m.
            foreach (var d in open.Where(d => d.style == "stone" && (d.name.StartsWith("Ramp to") || d.name == "The low ramp" || d.name.EndsWith(" stair") && !d.name.Contains("wall-walk"))))
                if (d.width < 3.5f) bad.Add(d.name + " is " + d.width + " m wide");
            foreach (var d in open.Where(d => d.name.EndsWith("step-stone"))) if (d.width < 5) bad.Add(d.name + " is " + d.width + " m wide");
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

        /// <summary>The second pass (2026-10-16): the design's departures closed in the data.</summary>
        [Test] public void The_pact_captain_holds_the_second_landing_on_a_floor()
        {
            var kersey = zone.camps.Single(c => c.mob == "Pact-Captain Rowena Kersey"); var line = zone.camps.Single(c => c.name == "Kersey's line");
            Assert.AreEqual(9, kersey.rise, .01f, "the second landing (+9), not the ground floor (DUNGEON2_DESIGN 2.7)"); Assert.AreEqual(9, line.rise, .01f, "her line with her");
            var landing = zone.decks.Single(d => d.name == "The second landing");
            Assert.AreEqual("floor", landing.style); Assert.AreEqual(9, landing.rise, .01f); Assert.GreaterOrEqual(landing.width, 7.5f); Assert.GreaterOrEqual(Vector2.Distance(landing.at, landing.at2), 5.5f, "a floor to fight on");
            foreach (var n in new[] { "The first landing", "The third landing", "The belfry floor" }) Assert.IsTrue(zone.decks.Any(d => d.name == n), n);
            Assert.AreEqual(4.5f, zone.decks.Single(d => d.name == "The first landing").rise, .01f); Assert.AreEqual(13.5f, zone.decks.Single(d => d.name == "The third landing").rise, .01f);
            Assert.IsTrue(zone.anchors.Any(a => a.name == "the second landing" && Mathf.Approximately(a.rise, 9)), "the anchor the nav test walks to");
            // No flight passes under a floor with less than 2.2 m over it: every pair of tower decks that overlap in plan are 2.2 m apart or more.
            var tower = zone.decks.Where(d => string.IsNullOrEmpty(d.cave) && Vector2.Distance((d.at + d.at2) / 2, new Vector2(0, 118)) < 9).ToList();
            var bad = new List<string>();
            Rect Plan(ZoneDeck d) { var lo = Vector2.Min(d.at, d.at2); var hi = Vector2.Max(d.at, d.at2); bool alongX = Mathf.Abs(d.at2.x - d.at.x) > Mathf.Abs(d.at2.y - d.at.y); var half = alongX ? new Vector2(0, d.width / 2) : new Vector2(d.width / 2, 0); return Rect.MinMaxRect(lo.x - half.x + .05f, lo.y - half.y + .05f, hi.x + half.x - .05f, hi.y + half.y - .05f); }
            foreach (var a in tower) foreach (var b in tower)
            {
                if (a == b || !Plan(a).Overlaps(Plan(b))) continue;
                float gap = Mathf.Min(a.rise, a.rise2) - Mathf.Max(b.rise, b.rise2);
                if (gap > 0 && gap < 2.2f + (a.style == "stone" ? 1.1f : a.style == "floor" ? .4f : .3f)) bad.Add(a.name + " is " + gap.ToString("0.00") + " m over " + b.name);
            }
            Clean(bad, "tower decks with too little head-room");
        }

        [Test] public void The_bars_are_as_designed_and_the_run_lifts_the_trapdoor()
        {
            var sally = zone.gates.Single(g => g.title == "The sally-port bar");
            Assert.AreEqual("bar", sally.kind); Assert.AreEqual("far", sally.side, "the gatehouse's town-side bar: lifted from the town side"); Assert.IsFalse(string.IsNullOrEmpty(sally.prompt));
            var gate = zone.gates.Single(g => g.title == "Tredway's gate"); Assert.AreEqual(gate.at.y, sally.at.y, .01f, "in the gatehouse's north wall with Tredway's gate");
            var trap = zone.gates.Single(g => g.title == "The well-house trapdoor");
            Assert.AreEqual("bar", trap.kind); Assert.AreEqual("far", trap.side, "lifted from below only"); Assert.IsTrue(trap.behind, "it bars again behind the party");
            Assert.AreEqual("The well-house stair", trap.cave); Assert.IsTrue(caves.ContainsKey(trap.cave));
            Assert.AreEqual(trap.title, zone.wings.Single(w => w.id == "cellars").gate, "the Cellars' way out is through it");
            Assert.IsTrue(zone.gates.Where(g => g.behind).All(g => g.kind == "bar" && g.side == "far"));
            var order = new List<string>(); var full = Plan(DungeonPlan.Mode.Full, order);
            int lastCellar = full.FindLastIndex(s => !s.regroup && s.wing == "cellars"); Assert.GreaterOrEqual(lastCellar, 0, "the full run takes the Cellars");
            var after = full[lastCellar + 1]; Assert.IsTrue(after.regroup && after.name == "the well-house" && after.wing == "cellars", "after the Cellars' last fight the run regroups at the well-house (through the trapdoor), not back the way it came: " + after.name);
            Assert.IsFalse(Plan(DungeonPlan.Mode.Quick, new List<string>()).Any(s => s.wing == "cellars"), "the quick run leaves the Cellars");
        }

        [Test] public void The_secrets_give_what_the_design_says()
        {
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items);
            var want = new Dictionary<string, string> { { "secret.hallows.bets-cart", "loot.hal.the_hollins_ring" }, { "secret.hallows.masons-cache", "loot.hal.the_mortar_ring" }, { "secret.hallows.reed-chest", "loot.hal.the_kingfisher_ring" } };
            foreach (var kv in want)
            {
                var s = zone.secrets.Single(x => x.id == kv.Key);
                Assert.AreEqual(kv.Value, s.item, kv.Key + " holds its ring"); Assert.AreEqual(0, s.gold, kv.Key + ": the ring, not coin");
                var d = items.Get(kv.Value); Assert.NotNull(d, kv.Value); Assert.AreEqual("gear", d.kind); StringAssert.Contains("Ring", d.name); Assert.AreEqual("GAME-ONLY", d.canonStatus);
                var g = loot.Meta(kv.Value); Assert.NotNull(g); Assert.AreEqual("secret:" + kv.Key, g.source, kv.Value + " is the secret's");
                Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "Crulanda/Resources/Icons/item/" + kv.Value.Replace('.', '-') + ".png")), kv.Value + " has its icon (tools/wip/dungeon/hallows_icons.py)");
            }
            var mortar = loot.Meta("loot.hal.the_mortar_ring"); Assert.IsTrue(mortar.effects != null && mortar.effects.Any(e => e.kind == "procRoot" && e.ability == LootDatabase.AutoTrigger), "the Mortar ring roots (section 2.2)");
            Assert.AreEqual(0, zone.secrets.Single(x => x.id == "secret.hallows.satchel").gold, "the satchel holds the soldier's letter");
            Assert.IsFalse(string.IsNullOrEmpty(zone.secrets.Single(x => x.id == "secret.hallows.satchel").document));
            Assert.AreEqual("Grey Quartz Ring", items.Get("loot.hal.grey_quartz_pendant").name, "First-Cut's Grey Quartz Ring (section 2.5)");
        }

        [Test] public void The_town_wakes_for_its_hold_and_the_squalls_are_silver()
        {
            var glitch = zone.events.Single(e => e.id == EncounterSession.GlitchEvent); var waking = zone.events.Single(e => e.id == EncounterSession.WakingEvent);
            Assert.That(glitch.hold, Is.InRange(60, 90), "a Glitch wakes them for 60-90 s (section 4.2)"); Assert.AreEqual(90, waking.hold, .01f, "the Waking Hour, 90 s (E3)");
            Assert.AreEqual(4, zone.camps.Where(c => c.after != null && c.after.StartsWith("event:waking")).Sum(c => c.count), "four paused ringers");
            Assert.IsTrue(waking.waves.All(w => w.at < waking.hold - EncounterSession.SnapWarnSeconds), "every ringer climbs before the snap is warned");
            var walk = zone.events.Single(e => e.id == EncounterSession.WalkEvent);
            CollectionAssert.AreEqual(new[] { 33f, 67f }, walk.waves.Select(w => w.at).ToArray(), "the walk's waves at a third and two thirds of the way (E2)");
            Assert.IsTrue(ColorUtility.TryParseHtmlString(zone.squallTint, out var silver), "the time-squalls' tint"); Assert.Less(Mathf.Abs(silver.r - silver.b), .15f, "silver, not dun");
            StringAssert.Contains("silver", zone.squallText);
            var vowell = EliteMoves.For(EncounterSession.ExaminerName, false); var upcott = EliteMoves.For("Assessor Piers Upcott", false);
            Assert.AreEqual("stone", vowell.ccLook, "the Examiner's hour is the pause"); Assert.AreEqual("stone", upcott.ccLook, "and the Assessor's mark");
            Assert.Greater(vowell.ccWarn, vowell.ccSeconds, "warned by the bell's hum, longer than the pause lasts (section 4.3)");
            // The hour can be got clear of on the belfry floor (hallows-6, 2026-10-17: at 8 m no part of the 7.8 m floor was clear and it held
            // the whole party every time): a corner of the floor lies a metre and more past the ring round where he stands.
            var floor = zone.decks.Single(d => d.name == "The belfry floor"); var home = zone.camps.Single(c => c.mob == EncounterSession.ExaminerName).center;
            float far = 0;
            foreach (var x in new[] { floor.at.x - floor.width / 2, floor.at.x + floor.width / 2 })
                foreach (var y in new[] { floor.at.y, floor.at2.y }) far = Mathf.Max(far, Vector2.Distance(home, new Vector2(x, y)));
            Assert.Greater(far, vowell.ccReach + 1, "a healer or an archer can step clear of the hour on the belfry floor");
            Assert.Less(vowell.ccReach, floor.width, "and the hour still reaches past the melee");
            Assert.Greater(EliteMoves.For("The Long Shadow", false).ccWarn, 0, "the Long Shadow's minute is telegraphed");
        }

        /// <summary>
        /// The town as the books write it (Docs/REWORK_LOWTOWNS_HALLOWS.md picks 1, 2, 3 and 12; Chris, 2026-10-18: "keep all", "stone girl is
        /// talla"): white and perfect, not grey (b1 ch.17:269; ch.21:11), only the farm outside it stone (ch.17:45); the bubble of still air
        /// over the town and not the bridge (ch.21:19); Main Street's pristine gravel; nothing blown by a wind; the Square's crater, chalk,
        /// well, willow and chapel; the named people (Mr Gable, Mrs Miller, Talla) and the children with their ball, each looked at with E
        /// and labelled with its chapter.
        /// </summary>
        [Test] public void The_town_is_white_and_still_and_its_people_are_the_books()
        {
            var bad = new List<string>();
            foreach (var p in zone.props.Where(p => p.style == "stone"))
                if (p.at.y > -200) bad.Add(p.kind + " '" + p.name + "' at " + p.at + " is stone in the town (the books make it white)");
            Assert.Greater(zone.props.Count(p => p.style == "white" && p.kind == "house"), 15, "the town's houses white");
            Assert.Greater(zone.props.Count(p => p.style == "white" && p.kind == "fence"), 20, "its fences white pickets");
            Assert.AreEqual(4, zone.props.Count(p => p.style == "stone" && p.at.y < -250), "the fossilised farm stays stone (b1 ch.17:45)");
            foreach (var p in zone.props) if (!string.IsNullOrEmpty(p.name) && Regex.IsMatch(p.name, @"\bwind\b", RegexOptions.IgnoreCase)) bad.Add("'" + p.name + "': there is no wind (b1 ch.21:19)");
            // The bubble: over the gatehouse, the street, the Square, the tower, the Archive and the Granary; not over the bridge or the road.
            var b = zone.bubble; Assert.NotNull(b); Assert.Greater(b.radius, 0, "the bubble of static reality");
            StringAssert.Contains("ch.21:19", b.canonStatus);
            foreach (var at in new[] { new Vector2(0, -128), new Vector2(0, -60), new Vector2(0, 70), new Vector2(0, 118), new Vector2(97, 70), new Vector2(115, -70) })
                Assert.AreEqual(1, b.StillAt(at.x, at.y), 1e-3f, "still at " + at);
            foreach (var at in new[] { new Vector2(0, -180), new Vector2(0, -300) }) Assert.AreEqual(0, b.StillAt(at.x, at.y), 1e-3f, "the wind and the fog outside it at " + at);
            Assert.AreEqual("gravel", zone.roads.Single(r => r.name == "Main Street").surface, "the pristine gravel (b1 ch.21:19)");
            // The Square and the people: looked at with E, each with its words and its label citing the book.
            var looked = new Dictionary<string, string> {
                { EncounterSession.GableName, "ch.21" }, { "Mrs Miller", "ch.21" }, { "Talla", "ch.19" }, { "A child chasing the ball", "ch.21" },
                { "The crater's rim", "ch.21" }, { "The chalked equations", "ch.19" }, { "The ancient town well", "ch.19" }, { "The chapel's glass", "ch.19" } };
            foreach (var kv in looked)
            {
                var p = zone.props.SingleOrDefault(x => x.name == kv.Key);
                if (p == null) { bad.Add(kv.Key + " is not in the town"); continue; }
                if (string.IsNullOrEmpty(p.interact) || string.IsNullOrEmpty(p.text)) bad.Add(kv.Key + " is not looked at with E");
                if (string.IsNullOrEmpty(p.canonStatus) || !p.canonStatus.StartsWith("CANON") || !p.canonStatus.Contains("b1 " + kv.Value)) bad.Add(kv.Key + " is not labelled with its chapter (" + p.canonStatus + ")");
            }
            StringAssert.Contains("TALLA", zone.props.Single(p => p.name == "Talla").text, "the stone girl is Talla (Chris, 2026-10-18)");
            StringAssert.Contains("Chris, 2026-10-18", zone.props.Single(p => p.name == "Talla").canonStatus);
            Assert.AreEqual(7, zone.props.Single(p => p.name == "Talla").variant, "bent over the hollow");
            Assert.IsTrue(zone.props.Any(p => p.kind == "hollow"), "the dug-out hollow by the willow");
            var willow = zone.props.Single(p => p.name == "The weeping willow"); Assert.AreEqual("willow", willow.style); Assert.AreEqual("dead_oak", willow.kind, "the dead oak's draws kept");
            Assert.Less(Vector2.Distance(willow.at, zone.props.Single(p => p.name == "Talla").at), 9, "Talla by the willow");
            Assert.IsTrue(zone.props.Any(p => p.kind == "chapel"), "the chapel (b1 ch.19:53)"); Assert.IsTrue(zone.props.Any(p => p.kind == "handcart"), "the bicycle as a handcart (no machines)");
            Assert.GreaterOrEqual(zone.props.Count(p => p.kind == "paused" && p.variant == 5), 2, "the children"); Assert.IsTrue(zone.props.Any(p => p.kind == "paused" && p.variant == 6), "a dog in mid-leap");
            // Every prop given words or a label says where it comes from.
            foreach (var p in zone.props.Where(p => !string.IsNullOrEmpty(p.canonStatus)))
                if (!Regex.IsMatch(p.canonStatus, @"^(CANON|CANON-EXPANDED|GAME-ONLY)")) bad.Add(p.name + ": '" + p.canonStatus + "'");
            Clean(bad, "the town as the books write it");
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

        /// <summary>
        /// The Ash Road's dangers and the bridge as the book left it (Docs/REWORK_LOWTOWNS_HALLOWS.md picks 7 and 10; b1 ch.17): green
        /// fissures along the road toward the chasm (ch.17:185), shadows pointing at the sun (ch.17:11; lore #5), thickets of stone leaves
        /// off the road (ch.17:23), the shelter from the time-squall a cave beside the road (ch.17:93), slabs hanging and turning beside the
        /// walk over the chasm and clear of every deck (ch.17:193-201), and one of the walk's own slabs that tips (ch.17:243). Every one
        /// labelled with its chapter.
        /// </summary>
        [Test] public void The_ash_road_has_its_dangers_and_the_bridge_its_drifting_slabs()
        {
            var bad = new List<string>();
            var road = zone.roads.Single(r => r.name == EncounterSession.AshRoadName);
            float FromRoad(Vector2 p)
            {
                float best = float.MaxValue;
                for (int i = 0; i + 1 < road.points.Length; i++)
                {
                    var a = road.points[i]; var ab = road.points[i + 1] - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
                }
                return best;
            }
            var kinds = new Dictionary<string, string> { { "fissure", "ch.17:185" }, { "wrongshadow", "ch.17:11" }, { "stoneleaves", "ch.17:23" }, { "driftslab", "ch.17:201" }, { "tipslab", "ch.17:243" } };
            foreach (var kv in kinds)
            {
                var ps = zone.props.Where(p => p.kind == kv.Key).ToList();
                if (ps.Count == 0) bad.Add("no " + kv.Key);
                foreach (var p in ps)
                {
                    if (!ZoneBuilder.HallowsKinds.Contains(p.kind)) bad.Add(p.kind + " is not a Hallow's Creek kind");
                    if (string.IsNullOrEmpty(p.canonStatus) || !p.canonStatus.StartsWith("CANON") || !p.canonStatus.Contains(kv.Value)) bad.Add(p.name + " is not labelled with " + kv.Value);
                    if (p.style == "stone") bad.Add(p.name + " would count as the farm's stone");
                }
            }
            Assert.GreaterOrEqual(zone.props.Count(p => p.kind == "fissure"), 4, "a few fissures");
            foreach (var p in zone.props.Where(p => p.kind == "fissure"))
                if (FromRoad(p.at) > 12 || p.at.y < -260 || p.at.y > -210) bad.Add(p.name + " is not along the road's last stretch to the chasm");
            foreach (var p in zone.props.Where(p => p.kind == "stoneleaves"))
                if (FromRoad(p.at) - Mathf.Max(p.size.x, p.size.y) / 2 < road.width / 2) bad.Add(p.name + " reaches onto the road (they cut only off it)");
            foreach (var p in zone.props.Where(p => p.kind == "wrongshadow"))
                if (FromRoad(p.at) < road.width / 2 + 1) bad.Add(p.name + "'s stump stands on the road");
            // The squall's shelter: the overhang, a cave a few metres off the road.
            Assert.IsTrue(caves.ContainsKey(EncounterSession.SquallShelter), "the overhang");
            Assert.Less(FromRoad(zone.props.Single(p => p.kind == "cavern" && p.name == EncounterSession.SquallShelter).at), EncounterSession.SquallReach, "the overhang is within the squall's reach of the road: a run to it");
            // The drifting slabs: over the chasm, clear of every deck's walk (nobody could stand on one, and none hangs in the way).
            var pit = zone.pits.Single(); var spin = 0;
            foreach (var p in zone.props.Where(p => p.kind == "driftslab"))
            {
                if (Mathf.Abs(p.at.y - pit.center.y) > pit.size.y / 2 - 3) bad.Add(p.name + " is not over the chasm");
                if (p.variant == 1) spin++;
                foreach (var d in zone.decks)
                {
                    var ab = d.at2 - d.at; float t = ab.sqrMagnitude < 1e-4f ? 0 : Mathf.Clamp01(Vector2.Dot(p.at - d.at, ab) / ab.sqrMagnitude);
                    float gap = Vector2.Distance(p.at, d.at + ab * t) - d.width / 2 - Mathf.Max(p.size.x, p.size.y) * .75f;
                    if (gap < 1.5f) bad.Add(p.name + " hangs within " + gap.ToString("0.0") + " m of " + d.name);
                }
            }
            Assert.GreaterOrEqual(spin, 2, "some spinning slowly"); Assert.GreaterOrEqual(zone.props.Count(p => p.kind == "driftslab" && p.variant == 0), 2, "some tilted sideways");
            // The slab that tips is one of the walk's own decks over the chasm.
            foreach (var p in zone.props.Where(p => p.kind == "tipslab"))
            {
                var d = zone.decks.SingleOrDefault(x => x.name == p.name);
                if (d == null) bad.Add("the tipping slab '" + p.name + "' is no deck");
                else if (Mathf.Abs(d.at.y - pit.center.y) > pit.size.y / 2) bad.Add(p.name + " is not over the chasm");
            }
            Clean(bad, "Ash Road and bridge dressing");
        }
    }
}
