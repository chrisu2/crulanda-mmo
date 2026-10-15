using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Sealed Adit's data holds together (EncounterContent/Zones/adit.json; the Adit rework, Docs/ADIT_REWORK.md; GAME-ONLY): every
    /// name one part of the file gives another part resolves. The file is written in pieces by tools/wip/dungeon/adit_layout.py and read
    /// by hand in places, and a name that resolves nowhere fails quietly in the game: a deck in no cave is a warning in the log, a camp
    /// released after a mob nobody is never comes, a gate that opens on a mob nobody is stays shut, the run waits at a gate that is not
    /// there (2026-10-15, written while the rework's releases and events were still being built, so the forms of "after" are the ones
    /// Docs/ADIT_REWORK.md E2 and section 4 give: "kill:&lt;mob&gt;" a camp's mob, "loose:&lt;mob&gt;" an elite's, "event:&lt;id&gt;&lt;wave&gt;" a wave
    /// of a ZoneEvent, or an event the code raises, "event:powder" from the keg).
    /// Pure data and files: no scene, no session, no save. The caves are made as the builder makes them (Hollow, from each cavern's plan)
    /// so that "along" can be held against each cave's real length. Every offender is listed, not only the first.
    /// The elites' moves and their guards are SocialRulesTests' and EliteBalanceTests' business (every elite has a move of its own, the
    /// counts, the guards that stand beside their lord): this asks only the other way round, that every move names an elite who is there.
    /// </summary>
    public class AditDataTests
    {
        static ZoneDefinition zone;
        /// <summary>The zone's caverns by name, in the file's order, each made from its plan over level ground (x and z are the caves' own;
        /// the floors' heights are relative to each mouth, which is enough to measure along them).</summary>
        static readonly Dictionary<string, Hollow> caves = new Dictionary<string, Hollow>();
        static Dictionary<string, ZoneCamp> camps; static Dictionary<string, ZoneWing> wings; static Dictionary<string, ZoneGateDef> gates;
        static Dictionary<string, ZoneAnchor> anchors; static Dictionary<string, ZoneEvent> events;
        /// <summary>Every camp's mob, and the elites' mobs (what "kill:", "loose:", opensOn and lord may name).</summary>
        static HashSet<string> mobs, elites;
        /// <summary>Event keys the code raises without a ZoneEvent (Docs/ADIT_REWORK.md E4: taking the keg releases Hobb and his sappers).</summary>
        static readonly string[] CodeEvents = { "powder", "greyline1", "greyline2", "greyline3", "haul1", "haul2" };   // (S7) the Grey Spreads' lines (EncounterSession.GreySpread.cs); (S6) the barge's crew up out of the hold to Danner's calls once the Haul has her afloat (EncounterSession.Haul)

        [OneTimeSetUp] public void Load()
        {
            zone = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Zones/adit.json")));
            Assert.IsTrue(zone.dungeon, "adit.json is the dungeon.");
            caves.Clear();
            foreach (var p in zone.props)
            {
                if (p == null || p.kind != "cavern" || string.IsNullOrEmpty(p.name) || p.plan == null || p.plan.Length < 10) continue;
                if (caves.ContainsKey(p.name)) continue;   // (named twice: Every_cavern says so)
                var rows = new List<float[]>(); for (int i = 0; i + 4 < p.plan.Length; i += 5) rows.Add(new[] { p.plan[i], p.plan[i + 1], p.plan[i + 2], p.plan[i + 3], p.plan[i + 4] });
                caves[p.name] = new Hollow(p.name, p.at, p.rotation, rows, (x, z) => 0f);
            }
            camps = First(zone.camps, c => c.name); wings = First(zone.wings, w => w.id); gates = First(zone.gates, g => g.title);
            anchors = First(zone.anchors, a => a.name); events = First(zone.events, e => e.id);
            mobs = new HashSet<string>(zone.camps.Where(c => c != null && !string.IsNullOrEmpty(c.mob)).Select(c => c.mob));
            elites = new HashSet<string>(zone.camps.Where(c => c != null && c.elite && !string.IsNullOrEmpty(c.mob)).Select(c => c.mob));
        }
        /// <summary>The first of each name (a second of the same name is an offender of its own test).</summary>
        static Dictionary<string, T> First<T>(T[] list, Func<T, string> key)
        {
            var d = new Dictionary<string, T>();
            foreach (var x in list ?? new T[0]) { if (x == null) continue; var k = key(x); if (!string.IsNullOrEmpty(k) && !d.ContainsKey(k)) d[k] = x; }
            return d;
        }
        /// <summary>Whether <paramref name="along"/> metres lies in the cave named, into <paramref name="bad"/> when it does not.</summary>
        static void InCave(List<string> bad, string what, string name, string cave, float along)
        {
            if (string.IsNullOrEmpty(cave) || !caves.TryGetValue(cave, out var h)) { bad.Add(what + " '" + name + "' names a cave the zone has not got: '" + cave + "'"); return; }
            if (along < 0 || along > h.Length + .01f) bad.Add(what + " '" + name + "' is " + along.ToString("0.00") + " m along '" + cave + "', which is " + h.Length.ToString("0.0") + " m long");
        }
        static void Clean(List<string> bad, string what) { Assert.IsEmpty(bad, bad.Count + " of the Adit's " + what + " do not resolve:\n  " + string.Join("\n  ", bad)); }

        [Test] public void Every_cavern_is_named_once_and_opens_out_of_a_cavern_listed_before_it()
        {
            var bad = new List<string>(); var seen = new HashSet<string>();
            foreach (var p in zone.props)
            {
                if (p == null || p.kind != "cavern") continue;
                if (string.IsNullOrEmpty(p.name)) { bad.Add("a cavern at " + p.at + " has no name"); continue; }
                if (!seen.Add(p.name)) bad.Add("cavern '" + p.name + "' is listed twice");
                // within: the parent must be made first (ZoneBuilder.PrepareHollows finds it among the caves made so far); joins is resolved after all are made.
                if (!string.IsNullOrEmpty(p.within) && (!seen.Contains(p.within) || p.within == p.name)) bad.Add("cavern '" + p.name + "' is within '" + p.within + "', which is not a cavern listed before it");
                if (!string.IsNullOrEmpty(p.joins) && (!caves.ContainsKey(p.joins) || p.joins == p.name)) bad.Add("cavern '" + p.name + "' joins '" + p.joins + "', which is not another cavern of the zone");
            }
            Assert.GreaterOrEqual(caves.Count, 10, "The reworked Adit's caves were all read (the Workings, the Gallery, its branches and the Deep Way).");
            Clean(bad, "caverns");
        }

        [Test] public void Every_camp_stands_in_a_cave_in_a_wing_behind_a_gate_the_zone_has_with_guards_it_has()
        {
            var bad = new List<string>(); var seen = new HashSet<string>();
            foreach (var c in zone.camps)
            {
                if (c == null) continue;
                if (!seen.Add(c.name)) bad.Add("camp '" + c.name + "' is named twice");
                InCave(bad, "camp", c.name, c.cave, c.along);
                if (string.IsNullOrEmpty(c.wing) || !wings.ContainsKey(c.wing)) bad.Add("camp '" + c.name + "' is in wing '" + c.wing + "', which the zone has not got");
                // The gate the run waits at before it: a title, "none", or empty for the last gate before it in its cave (EncounterSession.GateBefore).
                if (!string.IsNullOrEmpty(c.gate) && c.gate != "none" && !gates.ContainsKey(c.gate)) bad.Add("camp '" + c.name + "' waits at gate '" + c.gate + "', which the zone has not got");
                if (!string.IsNullOrEmpty(c.guards))
                {
                    if (!c.elite) bad.Add("camp '" + c.name + "' names guards but is no elite");
                    if (c.guards != "none")
                        foreach (var raw in c.guards.Split(',')) { var g = raw.Trim(); if (g.Length > 0 && !camps.ContainsKey(g)) bad.Add("camp '" + c.name + "' names guard camp '" + g + "', which the zone has not got"); }
                }
            }
            Clean(bad, "camps");
        }

        [Test] public void Every_after_names_a_mob_a_bosss_phase_or_a_wave_of_an_event()
        {
            var bad = new List<string>();
            foreach (var c in zone.camps)
            {
                if (c == null || string.IsNullOrEmpty(c.after)) continue;
                string a = c.after;
                if (a.StartsWith("kill:", StringComparison.Ordinal)) { if (!mobs.Contains(a.Substring(5))) bad.Add("camp '" + c.name + "' comes after '" + a + "': no camp's mob is '" + a.Substring(5) + "'"); }
                else if (a.StartsWith("loose:", StringComparison.Ordinal)) { if (!elites.Contains(a.Substring(6))) bad.Add("camp '" + c.name + "' comes after '" + a + "': no elite's mob is '" + a.Substring(6) + "' (a boss's phase)"); }
                else if (a.StartsWith("event:", StringComparison.Ordinal))
                {
                    // "event:<id><n>": wave n of the event, which must bring this camp back; else an event the code raises.
                    string key = a.Substring(6); int digits = 0; while (digits < key.Length && char.IsDigit(key[key.Length - 1 - digits])) digits++;
                    ZoneEvent ev = digits > 0 && digits < key.Length ? (events.TryGetValue(key.Substring(0, key.Length - digits), out var found) ? found : null) : null;
                    if (ev != null)
                    {
                        int n = int.Parse(key.Substring(key.Length - digits)); var waves = ev.waves ?? new ZoneWave[0];
                        if (n < 1 || n > waves.Length) bad.Add("camp '" + c.name + "' comes after '" + a + "': event '" + ev.id + "' has " + waves.Length + " waves");
                        else if (waves[n - 1] == null || waves[n - 1].camp != c.name) bad.Add("camp '" + c.name + "' comes after '" + a + "', but that wave brings '" + waves[n - 1]?.camp + "'");
                    }
                    else if (Array.IndexOf(CodeEvents, key) < 0) bad.Add("camp '" + c.name + "' comes after '" + a + "': no event of the zone and none the code raises");
                }
                else bad.Add("camp '" + c.name + "' has after '" + a + "', which is no kill:, loose: or event:");
                if (c.step > 0) bad.Add("camp '" + c.name + "' is released later (after '" + a + "') but has step " + c.step + " (a released camp is no step of the run)");
            }
            // The other way round: every wave of every event brings a camp that waits for that wave.
            foreach (var ev in events.Values)
            {
                var waves = ev.waves ?? new ZoneWave[0];
                for (int k = 0; k < waves.Length; k++)
                {
                    string want = "event:" + ev.id + (k + 1);
                    if (waves[k] == null || !camps.TryGetValue(waves[k].camp ?? "", out var c)) bad.Add("event '" + ev.id + "' wave " + (k + 1) + " brings camp '" + waves[k]?.camp + "', which the zone has not got");
                    else if (c.after != want) bad.Add("event '" + ev.id + "' wave " + (k + 1) + " brings '" + c.name + "', whose after is '" + c.after + "', not '" + want + "'");
                }
            }
            Clean(bad, "releases");
        }

        [Test] public void Every_gate_stands_in_its_cave_and_has_what_its_kind_needs()
        {
            var bad = new List<string>(); var seen = new HashSet<string>();
            foreach (var g in zone.gates)
            {
                if (g == null) continue;
                if (!seen.Add(g.title)) bad.Add("gate '" + g.title + "' is titled twice");
                InCave(bad, "gate", g.title, g.cave, g.along);
                string kind = string.IsNullOrEmpty(g.kind) ? "bossdoor" : g.kind;   // (ZoneBuilder.Adit DataGates: no kind is a boss's door)
                switch (kind)
                {
                    case "bossdoor":   // opens of itself when its mob dies (EncounterSession opens the gates whose OpensOn is the dead mob's name)
                        if (string.IsNullOrEmpty(g.opensOn)) bad.Add("bossdoor '" + g.title + "' has no opensOn");
                        else if (!elites.Contains(g.opensOn)) bad.Add("bossdoor '" + g.title + "' opens on '" + g.opensOn + "', which is no elite camp's mob");
                        break;
                    case "liftgate":   // the code builds the singing door under its own title, at the data's place (ZoneBuilder.Adit MainAdit)
                        if (g.title != ZoneBuilder.AditLiftGate) bad.Add("liftgate '" + g.title + "' is not titled '" + ZoneBuilder.AditLiftGate + "', the one the code builds");
                        if (string.IsNullOrEmpty(g.prompt)) bad.Add("liftgate '" + g.title + "' has no prompt");
                        break;
                    case "platformgate":   // likewise; its lord is the elite whose people answer the loud way in
                        if (g.title != ZoneBuilder.AditPlatformGate) bad.Add("platformgate '" + g.title + "' is not titled '" + ZoneBuilder.AditPlatformGate + "', the one the code builds");
                        if (string.IsNullOrEmpty(g.prompt)) bad.Add("platformgate '" + g.title + "' has no prompt");
                        if (string.IsNullOrEmpty(g.lord)) bad.Add("platformgate '" + g.title + "' has no lord");
                        break;
                    case "pen": case "bar": case "hiddendoor":   // worked by hand: E at the thing itself, so a prompt
                        if (string.IsNullOrEmpty(g.prompt)) bad.Add(kind + " '" + g.title + "' has no prompt (it is worked by hand)");
                        break;
                    default: bad.Add("gate '" + g.title + "' is of kind '" + kind + "', which ZoneGate does not know"); break;
                }
                if (!string.IsNullOrEmpty(g.lord) && !elites.Contains(g.lord)) bad.Add("gate '" + g.title + "' has lord '" + g.lord + "', which is no elite camp's mob");
                if (!string.IsNullOrEmpty(g.side) && g.side != "far") bad.Add("gate '" + g.title + "' has side '" + g.side + "' (only \"far\" means anything)");
            }
            Clean(bad, "gates");
        }

        [Test] public void Every_anchor_deck_drop_water_secret_and_crumb_lies_within_its_cave()
        {
            var bad = new List<string>(); var seen = new HashSet<string>();
            foreach (var a in zone.anchors)
            {
                if (a == null) continue;
                if (!seen.Add(a.name)) bad.Add("anchor '" + a.name + "' is named twice (ZoneBuilder.Anchors is keyed by name alone)");
                InCave(bad, "anchor", a.name, a.cave, a.along);
            }
            foreach (var d in zone.decks)
            {
                if (d == null) continue;
                InCave(bad, "deck", d.name, d.cave, d.from);
                if (d.radius <= 0) InCave(bad, "deck (its end)", d.name, d.cave, d.to);   // a round deck stands at from alone
            }
            foreach (var d in zone.drops)
            {
                if (d == null) continue;
                InCave(bad, "drop", d.name, d.fromCave, d.from);
                InCave(bad, "drop (its foot)", d.name, string.IsNullOrEmpty(d.toCave) ? d.fromCave : d.toCave, d.to);
            }
            foreach (var w in zone.caveWater)
            {
                if (w == null) continue;
                InCave(bad, "cave water", w.name, w.cave, w.from);
                if (w.radius <= 0) InCave(bad, "cave water (its end)", w.name, w.cave, w.to);
            }
            var secretIds = new HashSet<string>(zone.secrets.Where(s => s != null).Select(s => s.id));
            foreach (var s in zone.secrets)
            {
                if (s == null) continue;
                if (!string.IsNullOrEmpty(s.cave)) InCave(bad, "secret", s.id, s.cave, s.along);
                if (!string.IsNullOrEmpty(s.needs) && !secretIds.Contains(s.needs)) bad.Add("secret '" + s.id + "' needs '" + s.needs + "', which the zone has not got");
            }
            for (int i = 0; i < zone.crumbs.Length; i++)
            {
                var c = zone.crumbs[i]; if (c == null) continue;
                InCave(bad, "crumb trail", i.ToString(), c.cave, c.from); InCave(bad, "crumb trail (its end)", i.ToString(), c.cave, c.to);
            }
            Clean(bad, "places in the caves");
        }

        [Test] public void Every_wing_opens_after_wings_or_groups_and_regroups_at_an_anchor()
        {
            var bad = new List<string>(); var seen = new HashSet<string>();
            var groups = new HashSet<string>(zone.wings.Where(w => w != null && !string.IsNullOrEmpty(w.group)).Select(w => w.group));
            foreach (var w in zone.wings)
            {
                if (w == null) continue;
                if (!seen.Add(w.id)) bad.Add("wing '" + w.id + "' has its id twice");
                if (!string.IsNullOrEmpty(w.after))
                    foreach (var raw in w.after.Split(',')) { var x = raw.Trim(); if (x.Length > 0 && !wings.ContainsKey(x) && !groups.Contains(x)) bad.Add("wing '" + w.id + "' comes after '" + x + "', which is no wing or group"); }
                if (!string.IsNullOrEmpty(w.home) && !anchors.ContainsKey(w.home)) bad.Add("wing '" + w.id + "' regroups at '" + w.home + "', which is no anchor");
                if (!string.IsNullOrEmpty(w.exit) && w.exit != "drop" && w.exit != "walk") bad.Add("wing '" + w.id + "' has exit '" + w.exit + "' (drop or walk)");
                if (!zone.camps.Any(c => c != null && c.wing == w.id)) bad.Add("wing '" + w.id + "' has no camps");
            }
            foreach (var e in zone.events)
            {
                if (e == null) continue;
                if (string.IsNullOrEmpty(e.wing) || !wings.ContainsKey(e.wing)) bad.Add("event '" + e.id + "' is in wing '" + e.wing + "', which the zone has not got");
                if (string.IsNullOrEmpty(e.anchor) || !anchors.ContainsKey(e.anchor)) bad.Add("event '" + e.id + "' centres on '" + e.anchor + "', which is no anchor");
            }
            Clean(bad, "wings and events");
        }

        /// <summary>The planner over the zone's web of wings, as DungeonPlanTests drives it: straight-line distances, every camp standing, no
        /// quest wanting anyone; where each camp stands is its written centre and its rise, an anchor its cave's point at its rise.</summary>
        static List<RouteStep> Plan(DungeonPlan.Mode mode, List<string> order)
        {
            Vector3 CampAt(int c) { var cp = zone.camps[c]; return new Vector3(cp.center.x, cp.rise, cp.center.y); }
            Vector3? AnchorAt(string n)
            {
                if (!anchors.TryGetValue(n, out var a) || !caves.TryGetValue(a.cave, out var h)) return null;
                var p = h.At(a.along, a.aside); return new Vector3(p.x, a.rise, p.z);
            }
            var start = new Vector3(zone.spawns.player.x, 0, zone.spawns.player.y);
            return DungeonPlan.Plan(zone, mode, start, CampAt, AnchorAt, Vector3.Distance, c => true, c => false, order);
        }
        [Test] public void The_planner_plans_the_quick_standard_and_full_runs_of_the_zone()
        {
            var quickOrder = new List<string>(); var quick = Plan(DungeonPlan.Mode.Quick, quickOrder);
            var standardOrder = new List<string>(); var standard = Plan(DungeonPlan.Mode.Standard, standardOrder);
            var fullOrder = new List<string>(); var full = Plan(DungeonPlan.Mode.Full, fullOrder);
            // Every fight of every wing the run goes through is in it (a wing left out is the fork's other side, and only in the quick and
            // standard runs); the standard run takes more, the full run more again.
            foreach (var (run, order, name) in new[] { (quick, quickOrder, "quick"), (standard, standardOrder, "standard"), (full, fullOrder, "full") })
                for (int c = 0; c < zone.camps.Length; c++)
                {
                    var cp = zone.camps[c]; if (!DungeonPlan.RequiredCamp(cp)) continue;
                    string fight = "'" + cp.name + "' (wing " + cp.wing + ", step " + cp.step + ")";
                    if (!order.Contains(wings[cp.wing].name)) { Assert.IsTrue(name != "full" && !string.IsNullOrEmpty(wings[cp.wing].group), "the " + name + " run leaves out " + fight + ", which is no side of a fork"); continue; }
                    Assert.IsTrue(run.Any(s => s.camps.Contains(c)), "the " + name + " run fights " + fight);
                }
            Assert.Less(quick.Count, standard.Count, "the standard run takes the side bosses too"); Assert.Less(standard.Count, full.Count, "the full run takes every side room");
            // The full run fights every elite who is there from the start (the released ones, Hobb and Hollowsong, come when they are brought).
            foreach (var c in zone.camps.Where(c => c != null && c.elite && string.IsNullOrEmpty(c.after)))
                Assert.IsTrue(full.Any(s => s.camps.Any(i => zone.camps[i] == c)), "the full run fights " + c.name);
            // The web: in at the Workings, the only wing after nothing; out by the Deep Way, after the Wake, which waits for the four wings
            // before it; one side of the vent fork (the Scriptorium or the Muster Yard) in the quick run, both in the full.
            // (S6) The run ends in the Black Cove, after the Deep Way (Danner on his barge); the full run may take the Pitch Pit off the Rail Hall last.
            Assert.AreEqual("The Old Workings", quickOrder.First()); Assert.AreEqual("The Black Cove", quickOrder.Last());
            Assert.AreEqual("The Old Workings", fullOrder.First()); Assert.Contains(fullOrder.Last(), new[] { "The Black Cove", "The Pitch Pit" }, string.Join(", ", fullOrder));
            Assert.Less(quickOrder.IndexOf("The Deep Way"), quickOrder.IndexOf("The Black Cove")); Assert.Less(fullOrder.IndexOf("The Deep Way"), fullOrder.IndexOf("The Black Cove"));
            Assert.AreEqual(1, quickOrder.Count(w => w == "The Scriptorium" || w == "The Muster Yard"), "one side of the fork: " + string.Join(", ", quickOrder));
            Assert.AreEqual(2, fullOrder.Count(w => w == "The Scriptorium" || w == "The Muster Yard"), "both sides of the fork: " + string.Join(", ", fullOrder));
            Assert.Less(fullOrder.IndexOf("The Geode Foundry"), fullOrder.IndexOf("The Deep Way")); Assert.Less(fullOrder.IndexOf("The Grey Breach"), fullOrder.IndexOf("The Deep Way"));
            Assert.Less(fullOrder.IndexOf("The Chapel of Embers"), fullOrder.IndexOf("The Deep Way")); Assert.Less(fullOrder.IndexOf("The Gallery's floor"), fullOrder.IndexOf("The Deep Way"));
        }

        [Test] public void Every_move_in_the_table_names_an_elite_who_is_there()
        {
            // SocialRulesTests says every elite camp has its own move; this is the other way: a move for a mob of no camp is a mob renamed
            // in the data and not in EliteMoves (or the other way round). The Rock-Eater alone is no camp: he rides beside Nix in his.
            var all = new HashSet<string>(LootTestData.Zones().SelectMany(z => z.camps ?? new ZoneCamp[0]).Where(c => c != null && c.elite).Select(c => c.mob));
            var strays = EliteMoves.All.Select(m => m.mob).Where(m => !all.Contains(m)).ToList();
            CollectionAssert.AreEquivalent(new[] { EncounterSession.RockEaterName }, strays, "moves for mobs no elite camp has: " + string.Join(", ", strays));
            Assert.IsTrue(elites.All(e => EliteMoves.All.Any(m => m.mob == e)), "every Adit elite has a move (SocialRulesTests says more about it)");
        }

        /// <summary>(S7) The Grey Spreads in the data: three lines across the Grey Breach in order, each with its seep of crawlers 25 m behind it,
        /// released by the line's key (EncounterSession.GreyLineKey) and by nothing else, in the Breach's wing, no step of the run; and the lines
        /// lie in the quiet between the Breach's fights (no step's camp within 12 m of a line along the cave).</summary>
        [Test] public void The_grey_lines_cross_the_breach_in_order_each_with_its_seep_behind_it()
        {
            var lines = zone.anchors.Where(a => a != null && a.name != null && a.name.StartsWith(ZoneBuilder.GreyLineAnchor)).OrderBy(a => a.name).ToList();
            Assert.AreEqual(3, lines.Count, "three grey lines");
            for (int n = 1; n <= 3; n++)
            {
                var a = lines[n - 1]; Assert.AreEqual(ZoneBuilder.GreyLineAnchor + n, a.name); Assert.AreEqual(EncounterSession.GreyCave, a.cave, a.name + " is in the Breach");
                if (n > 1) Assert.Greater(a.along, lines[n - 2].along, a.name + " lies past the one before");
                var seeps = zone.camps.Where(c => c != null && c.after == EncounterSession.GreyLineKey(n)).ToList();
                Assert.AreEqual(1, seeps.Count, "one seep for " + a.name);
                var seep = seeps[0];
                Assert.AreEqual(EncounterSession.GreyCave, seep.cave); Assert.AreEqual(a.along - 25, seep.along, .5f, seep.name + " waits 25 m behind its line");
                Assert.AreEqual("breach", seep.wing); Assert.AreEqual(0, seep.step); Assert.IsFalse(seep.elite);
                foreach (var c in zone.camps.Where(c => c != null && c.cave == EncounterSession.GreyCave && c.step > 0))
                    Assert.Greater(Mathf.Abs(c.along - a.along), 12, a.name + " is not in the middle of " + c.name + "'s fight");
            }
            Assert.IsTrue(elites.Contains(EncounterSession.GreyForeman), "the Foreman who stops it is there");
        }
        /// <summary>(S7) Every Adit secret pays something worth the finding: experience, and coin, a piece, a page, or the key to another.</summary>
        [Test] public void Every_adit_secret_pays_something_worth_finding()
        {
            var bad = new List<string>();
            foreach (var s in zone.secrets)
            {
                if (s == null) continue;
                if (s.xp <= 0) bad.Add(s.id + " gives no experience");
                if (s.gold <= 0 && string.IsNullOrEmpty(s.item) && string.IsNullOrEmpty(s.document) && s.kind != "key") bad.Add(s.id + " pays no coin, piece, page or key");
            }
            Assert.IsEmpty(bad, string.Join("; ", bad));
            Assert.GreaterOrEqual(zone.secrets.Count(s => s != null && !string.IsNullOrEmpty(s.item)), 3, "three of them a piece or a herb to carry out");
        }
        /// <summary>(S7) The Foundry's swing-loads: three, on its high walks (12 m up: the catwalk and the cross-walk).</summary>
        [Test] public void The_swing_loads_hang_over_the_foundrys_high_walks()
        {
            var loads = zone.anchors.Where(a => a != null && a.name != null && a.name.StartsWith(ZoneBuilder.SwingLoadAnchor)).ToList();
            Assert.AreEqual(3, loads.Count);
            foreach (var a in loads) { Assert.AreEqual("The Geode Foundry", a.cave, a.name); Assert.AreEqual(12, a.rise, .01f, a.name + " is on a high walk"); }
        }
    }
}
