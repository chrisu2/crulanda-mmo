#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Sealed Adit, the dungeon under the Shattered Peaks (Docs/DUNGEON_DESIGN.md, step D1; reworked, Docs/ADIT_REWORK.md): its own
    /// zone, a web of caves round the Singing Gallery (the Old Workings down to a lip high in its wall, the wings off it at three heights,
    /// the Deep Way on to the Rail Hall, each opening through its parent's wall), every camp on its own cave's floor or its deck and
    /// reachable on foot from the yard, the Peaks' way in and the cut back up, and a sim's run through all of it.
    /// </summary>
    public class AditTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-adit-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.adit";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        const string Main = "The Sealed Adit", Gallery = "The Singing Gallery", Deep = "The Deep Way";
        /// <summary>The ways on out of the Gallery: the Company door 12 m up its east wall, the chute lip 6 m up it, the ash steps, the river
        /// and the Deep Way at its north neck (the Adit rework).</summary>
        static readonly string[] Branches = { "The Cutting Drift", "The Geode Foundry", "The Ash Garden", "The Sunless River", Deep };
        static Hollow Cave(string name) { return Hollow.All.Find(h => h.Name == name); }

        [UnityTest] public IEnumerator The_network_is_built_and_every_camp_is_walkable_from_the_yard()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.adit", zone.Zone.id); Assert.IsTrue(zone.Zone.dungeon, "a dungeon");
            zone.OpenGates(); for (int i = 0; i < 3; i++) yield return null;   // the way through, with its gates open (D3; AditGateTests has them shut)
            var main = Cave(Main); Assert.NotNull(main, "the main way"); Assert.IsNull(main.Parent);
            var gallery = Cave(Gallery); Assert.NotNull(gallery, "the hub"); Assert.AreSame(main, gallery.Parent, "the Gallery's sump drift opens off the dark drift");
            Assert.AreSame(gallery, main.Join, "the Workings end in the Gallery"); Assert.AreEqual(14, main.JoinRise, .3f, "the overlook: a lip 14 m up its wall");
            foreach (var b in Branches) { var h = Cave(b); Assert.NotNull(h, b); Assert.AreSame(gallery, h.Parent, b + " opens out of the Gallery"); }
            foreach (var h in Hollow.All) if (h != main) Assert.NotNull(h.Parent, h.Name + " opens out of another cave");
            var deep = Cave(Deep); Assert.Less(deep.Centre[deep.Centre.Count - 1].y, main.Centre[0].y - 80, "the Rail Hall lies deep under the Peaks");
            Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(zone.Zone.spawns.player, .2f), out var start, 2.5f, NavMesh.AllAreas), "the yard is on the navmesh");
            var path = new NavMeshPath();
            var walked = new System.Collections.Generic.List<(Hollow cave, float s, Vector3 at)>();   // each step's floor point, for the camps below
            // Down each cave in six-metre steps first, so a break in the floor names its stretch (where it has a floor of its own: not its
            // mouth out in its parent's air, nor its end out over the cave it joins).
            foreach (var h in Hollow.All)
            {
                var from = h.Parent == null ? start.position : (NavMesh.SamplePosition(h.At(1) + Vector3.up * .2f, out var m, 2, NavMesh.AllAreas) ? m.position : start.position);
                for (float s = 3; s < h.Length - 4; s += 6)
                {
                    if (!h.Floored(h.Nearest(new Vector2(h.At(s).x, h.At(s).z), out _))) continue;
                    // The middle of the way, or beside it where the middle is not floor (the Ember Vent's lake of molten rock).
                    float aside = h.Half[h.Nearest(new Vector2(h.At(s).x, h.At(s).z), out _)] * .62f; NavMeshHit next = default; bool on = false;
                    foreach (var off in new[] { 0f, aside, -aside }) if (NavMesh.SamplePosition(h.At(s, off) + Vector3.up * .2f, out next, 2, NavMesh.AllAreas)) { on = true; break; }
                    Assert.IsTrue(on, h.Name + ": the floor " + s + " m in is on the navmesh (" + h.At(s) + ")");
                    Assert.IsTrue(NavMesh.CalculatePath(from, next.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, h.Name + ": walkable from " + from + " to " + next.position + " (" + s + " m in)");
                    from = next.position; walked.Add((h, s, next.position));
                }
            }
            // Every floor is a place a save may hold (a save 80 m down came back a new character, 2026-10-11).
            foreach (var w in walked) Assert.IsTrue(EncounterSave.Placeable(w.at.x, w.at.y, w.at.z), w.cave.Name + ": a save " + w.s + " m in (" + w.at + ") loads again");
            foreach (var cp in zone.Zone.camps)
            {
                var h = Cave(cp.cave); Assert.NotNull(h, cp.name + " names its cave");
                Assert.IsTrue(h.FloorAt(cp.center, out float y), cp.name + " stands on " + cp.cave + "'s floor");
                if (cp.rise > 0) { y = zone.StandAt(cp.center, y + cp.rise).y; Assert.AreEqual(h.At(cp.along, cp.aside).y + cp.rise, y, .6f, cp.name + " stands on its deck, " + cp.rise + " m up"); }
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(cp.center.x, y + .2f, cp.center.y), out var at, 2, NavMesh.AllAreas), cp.name + " is on the navmesh");
                // From the nearest step of its cave's walk (each step was walked from the one before, back to the yard): a search the length
                // of the whole drawn-out dungeon gives up part way (2026-10-10).
                var near = walked.Where(w => w.cave == h).OrderBy(w => Mathf.Abs(w.s - cp.along)).First();
                Assert.IsTrue(NavMesh.CalculatePath(near.at, at.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, cp.name + " can be walked to from the yard (from " + near.s + " m in, " + near.at + ", to " + at.position + ")");
            }
            yield return null;
        }
        /// <summary>The drawn-out Adit is filled (note 132, Chris, 2026-10-11: "all you did in the dungeon was make it bigger which in turn has
        /// less mobs"), its mobs stand a body's width apart, stroll about their camps without leaving them, and the patrols walk their roads
        /// (note 131: "shouldnt spawn almost on top of each other. should all patrol. all mobs just stand in one place").</summary>
        [UnityTest, Timeout(300000)] public IEnumerator Mobs_stand_apart_stroll_their_camps_and_patrols_walk_their_roads()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            float Across(Vector3 v) { v.y = 0; return v.magnitude; }
            var mobs = s.Enemies.Where(e => e != null && e.actor.IsAlive && e.Camp).ToList();
            Assert.GreaterOrEqual(mobs.Count, 140, "the Adit holds a 60-75 minute run's mobs (70 before the fill)");
            Assert.GreaterOrEqual(mobs.Count(e => e.Patrol != null), 22, "eleven patrols walk the drifts and the Gallery (and the Old Burrower its pool)");
            // Apart from the first: no two of a camp closer than their width.
            var close = new System.Collections.Generic.List<string>();
            for (int x = 0; x < mobs.Count; x++)
                for (int y = x + 1; y < mobs.Count; y++)
                {
                    var a = mobs[x]; var b = mobs[y];
                    if (a.CampCenter == b.CampCenter && !a.Hidden && !b.Hidden && Across(a.transform.position - b.transform.position) < Mathf.Min(a.Gap, b.Gap) * .9f)
                        close.Add(a.actor.DisplayName + " and " + b.actor.DisplayName + " " + Across(a.transform.position - b.transform.position).ToString("0.0") + " m apart");
                }
            Assert.IsEmpty(close, "mobs spawned on top of each other: " + string.Join("; ", close.Take(8)));
            // Ninety seconds of their time: who has moved, who has left its camp, how far each patrol has walked.
            var start = mobs.ToDictionary(e => e, e => e.transform.position);
            var moved = new System.Collections.Generic.HashSet<EncounterEnemy>(); var strayed = new System.Collections.Generic.List<string>();
            var walked = mobs.Where(e => e.Patrol != null).ToDictionary(e => e, e => 0f);
            Time.timeScale = 3;
            try
            {
                for (float t = 0; t < 30; t += Time.unscaledDeltaTime)
                {
                    yield return null;
                    foreach (var e in mobs)
                    {
                        if (e == null || !e.actor.IsAlive || e.Engaged) continue;
                        if (Across(e.transform.position - start[e]) > 1.5f) moved.Add(e);
                        if (e.Patrol != null) walked[e] = Mathf.Max(walked[e], Across(e.transform.position - start[e]));
                        else if (!e.Evading && Across(e.transform.position - new Vector3(e.CampCenter.x, 0, e.CampCenter.y)) > e.CampRadius + e.Gap + 1.5f)
                            strayed.Add(e.actor.DisplayName + " " + Across(e.transform.position - new Vector3(e.CampCenter.x, 0, e.CampCenter.y)).ToString("0.0") + " m from its camp (radius " + e.CampRadius + ")");
                    }
                }
            }
            finally { Time.timeScale = 1; }
            // A camp's mob more than EncounterEnemy.StrollRange from the player stands on purpose (the CPU, Chris 2026-10-11), so the strolling is
            // measured on the patrols and the camps in the player's reach; the far ones are counted only for the message.
            var player = s.Player.transform.position;
            var inReach = mobs.Where(e => e != null && e.actor.IsAlive && !e.Hidden && e.Inside == null
                && (e.Patrol != null || Across(start[e] - player) <= EncounterEnemy.StrollRange - 5)).ToList();
            Assert.GreaterOrEqual(inReach.Count, 16, "the patrols and the near camps to watch");
            var still = inReach.Where(e => !moved.Contains(e)).ToList();
            Assert.Less(still.Count, inReach.Count * .25f, still.Count + " of the " + inReach.Count + " in reach stood still (" + (mobs.Count - inReach.Count) + " farther than "
                + EncounterEnemy.StrollRange + " m stand by design): " + string.Join(", ", still.Take(12).Select(e => e.actor.DisplayName)));
            Assert.IsEmpty(strayed.Distinct().Take(8).ToList(), "a mob left its camp: " + string.Join("; ", strayed.Distinct().Take(8)));
            var lazy = walked.Where(w => w.Value < 20).Select(w => w.Key.actor.DisplayName + " " + w.Value.ToString("0") + " m (its road " + w.Key.Patrol.Length + " points, "
                + Across(w.Key.Patrol[w.Key.Patrol.Length - 1] - w.Key.Patrol[0]).ToString("0") + " m end to end; now " + Across(w.Key.transform.position - start[w.Key]).ToString("0") + " m out)").ToList();
            Assert.IsEmpty(lazy, "every patrol walks its road (20 m and more in ninety seconds): " + string.Join(", ", lazy));
        }
        /// <summary>No machines in the Adit (Chris, 2026-10-11: "more machines!!!!!  i told you NO Machines!"): the Rock-Eater is a beast on
        /// the bear's body with Nix in its saddle, two mules stand in the carriage's traces, and nothing of the old engine is built.</summary>
        [UnityTest] public IEnumerator No_machines_the_Rock_Eater_is_a_beast_and_mules_haul_the_carriage()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var rock = s.Enemies.Find(e => e != null && e.MobName == EncounterSession.RockEaterName); Assert.NotNull(rock, "the Rock-Eater");
            var look = rock.GetComponent<ActorVisual>();
            Assert.NotNull(look.BeastModel, "a beast's body (the bear's), not a hull and a drill"); Assert.NotNull(look.RiderSeat, "a saddle on it");
            Assert.IsNull(GameObject.Find("Drill"), "no drill"); Assert.IsNull(GameObject.Find("Bit"), "no drill bit");
            var mules = UnityEngine.Object.FindObjectsByType<PitMule>(FindObjectsSortMode.None);
            Assert.AreEqual(2, mules.Length, "two mules in the carriage's traces");
            foreach (var m in mules) Assert.Less(s.Zone.RailTeam.Min(p => Vector3.Distance(p, m.transform.position)), .5f, "standing in the traces");
            Assert.AreEqual("The ore carriage", ZoneBuilder.AditCarriage);
            yield return null;
        }
        [UnityTest] public IEnumerator Each_branch_opens_through_the_gallery_wall()
        {
            yield return null;
            foreach (var b in Branches)
            {
                var h = Cave(b);
                // From in the Gallery, just inside the branch's mouth (high in its wall for the Company door and the chute), straight down the
                // branch: nothing of the Gallery's rock in the way (the caves' shells and knolls; what stands on the floors is not the wall).
                var from = h.At(1) + Vector3.up * 1.4f; var to = h.At(24) + Vector3.up * 1.4f;
                var rock = Physics.RaycastAll(from, (to - from).normalized, Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore)
                    .Where(x => x.collider.name.EndsWith(" shell") || x.collider.name.EndsWith(" knoll")).ToList();
                Assert.IsEmpty(rock.Select(x => x.collider.name + " at " + x.point).ToList(), b + ": the way in is open");
                Assert.IsTrue(Cave(Gallery).Open(h.At(1) + Vector3.up * 1.4f), b + "'s mouth is in the Gallery's air");
            }
        }
        [UnityTest] public IEnumerator Every_camp_is_spawned_where_it_stands()
        {
            yield return null; var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var camps = ZoneBuilder.Active.Zone.camps;
            for (int c = 0; c < camps.Length; c++)
            {
                var cp = camps[c]; int index = c;
                if (!string.IsNullOrEmpty(cp.after)) { Assert.IsFalse(session.Enemies.Exists(e => e != null && e.Camp && e.CampIndex == index), cp.name + " is released later (" + cp.after + "): not there at the start"); continue; }
                if (cp.rare > 0) continue;   // a rare camp is there one visit in a few (AditGateTests)
                var mine = session.Enemies.Where(e => e != null && e.Camp && e.CampIndex == index && e.Inside == null).ToList();   // (not Nix in the Rock-Eater's saddle)
                Assert.IsTrue(mine.Exists(e => Vector2.Distance(new Vector2(e.transform.position.x, e.transform.position.z), cp.center) < cp.radius + 2.5f), cp.name + " has its mobs");
                if (cp.rise <= 0 || cp.route != null && cp.route.Length > 1) continue;
                // Up on its deck (the Adit rework: a trestle's landing, the bridge's tower, a catwalk, the tally stage), not on the floor under it.
                float deck = EncounterSession.DeckHeight(cp, cp.center);
                Assert.IsTrue(mine.Exists(e => Mathf.Abs(e.transform.position.y - 1 - deck) < 1.2f), cp.name + " stands on its deck " + cp.rise + " m up (" + deck.ToString("0.0") + "): its mobs at " + string.Join(", ", mine.Select(e => (e.transform.position.y - 1).ToString("0.0"))));
            }
            Assert.IsTrue(session.Enemies.Exists(e => e != null && e.Elite && e.actor.DisplayName.Contains("Orsk Danner")), "the Rail-Captain waits at the end");
        }
        /// <summary>A sim's run through the web (the Adit rework, DungeonPlan; ADIT_REWORK.md section 9): wing by wing, the nearest open one next,
        /// each wing's fights together and in its own order (no wing walked twice), a regroup where a wing's way home comes down; the bosses' doors
        /// open as the run goes, so it waits only at the singing door. The quick run takes what must be done, the standard run the side bosses in
        /// the wings it goes through too, the full run every side room and both sides of the fork.</summary>
        [UnityTest] public IEnumerator A_sim_leads_the_whole_adit_wing_by_wing()
        {
            yield return null; var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var zone = ZoneBuilder.Active.Zone; var camps = zone.camps;
            var pop = SimPopulation.Active; var s = pop.World.sims.First();
            s.zone = "zone.adit"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = 11; session.Progress.experience = EncounterProgress.XpForLevel(9);   // the Adit is 10-12 (round 29)
            var p = session.Player.transform.position; s.x = p.x + 2; s.z = p.z; pop.Refresh(); yield return null;
            Assert.IsTrue(session.Invite(s.id, true)); var c = session.PartySim(s.id);

            // The quick run: what must be done.
            Assert.IsNotNull(c.LeadDungeon("quick", out var why), why); Assert.AreEqual("The Sealed Adit", c.Dungeon); Assert.AreEqual(DungeonPlan.Mode.Quick, c.RunMode);
            var wings = c.WingOrder.ToList(); var steps = c.Route.ToList();
            Debug.Log("AditTests: the quick run's wings: " + string.Join(", ", wings) + "; " + c.CampsLeft + " camps");
            CollectionAssert.AreEqual(new[] { "The Old Workings", "The Miners' Trestle", "The Geode Foundry" }, wings.Take(3).ToArray(), "down the Workings, onto the trestle, over the bridge into the Foundry (enter high, work down)");
            Assert.AreEqual("The Deep Way", wings.Last(), "the Deep Way last, through the singing door");
            Assert.AreEqual(1, wings.Count(w => w == "The Scriptorium" || w == "The Muster Yard"), "one side of the fork: " + string.Join(", ", wings));
            foreach (var (before, after) in new[] { ("The Ash Garden", "The Chapel of Embers"), ("The Sunless River", "The Grey Breach"), ("The Geode Foundry", "The Deep Way"), ("The Grey Breach", "The Deep Way"), ("The Chapel of Embers", "The Deep Way") })
                Assert.Less(wings.IndexOf(before), wings.IndexOf(after), before + " before " + after);
            Assert.AreEqual("Adit pickets", c.LeadingName, "the first in from the mouth");
            Assert.IsFalse(steps.Exists(x => x.optional), "nothing optional in the quick run");
            // Every fight of every wing it takes, one fight to each wing's step (a boss and his guards together), and nothing else.
            var taken = zone.wings.Where(w => wings.Contains(w.name)).Select(w => w.id).ToList();
            int fights = camps.Where(cp => DungeonPlan.RequiredCamp(cp) && taken.Contains(cp.wing)).Select(cp => cp.wing + "/" + cp.step).Distinct().Count();
            Assert.AreEqual(fights, c.CampsLeft, "each wing's fights");
            foreach (var w in zone.wings.Where(wg => !wg.optional && string.IsNullOrEmpty(wg.group) && camps.Any(cp => cp.wing == wg.id && DungeonPlan.RequiredCamp(cp))))
                Assert.Contains(w.name, wings, w.name + " is on every run");
            // No wing walked twice: each wing's steps come together, in its own order.
            var walked = new System.Collections.Generic.List<string>();
            foreach (var x in steps) { if (walked.Count > 0 && walked[walked.Count - 1] == x.wing) continue; Assert.IsFalse(walked.Contains(x.wing), x.wing + " is gone back to"); walked.Add(x.wing); }
            foreach (var id in taken)
            {
                var order = steps.Where(x => x.wing == id && !x.regroup).Select(x => camps[x.Camp].step).ToList();
                CollectionAssert.IsOrdered(order, id + "'s fights in its own order");
            }
            // The bosses with the sigils are on the way; a regroup where each way home comes down (the chute, the cloister stair, the falls), the
            // boss's own door on the way to it.
            foreach (var keyed in camps.Where(cp => !string.IsNullOrEmpty(cp.key))) Assert.IsTrue(steps.Exists(x => x.camps.Any(i => camps[i] == keyed)), keyed.name + " carries " + keyed.key);
            foreach (var (home, door) in new[] { ("chute pool", "The chute door"), ("cloister foot", "The cloister door"), ("plunge pool", "The sluice-boards") })
            {
                int at = steps.FindIndex(x => x.regroup && x.name == home); Assert.GreaterOrEqual(at, 1, "a regroup at the " + home);
                Assert.AreEqual(steps[at - 1].wing, steps[at].wing, "straight after its wing's last fight");
                Assert.IsNotNull(steps[at].gate, "the way to the " + home); Assert.AreEqual(door, steps[at].gate.Title);
            }
            Assert.IsFalse(steps.Exists(x => x.regroup && x.name == "trestle landing"), "no walking back to the landing from the bridge's tower");
            // The doors: the bosses open theirs on the way (Lusk's before the dark drift, never the chute door for the Foundry's hall, reached
            // from its far end), so the run waits only at the singing door.
            Assert.AreEqual("Lusk's door", steps.First(x => x.name == "Timber crew").gate.Title);
            foreach (var hall in new[] { "Catwalk sorters", "Overseer Grael Vosk", "Nix and the Rock-Eater", "Smelters at the furnace" })
                Assert.AreEqual("Lusk's door", steps.First(x => x.camps.Any(i => camps[i].name == hall)).gate.Title, hall + " is reached from the Foundry's far end");
            Assert.AreEqual("The singing door", steps.First(x => x.name == "Stair guard").gate.Title);
            Assert.AreEqual(ZoneBuilder.AditLiftGate, c.StoppedBy, "the run will wait at the singing door, and only there before the platform gate");
            int quick = c.CampsLeft;

            // The standard run: the side bosses inside the wings it goes through too.
            c.StopLeading(); Assert.IsNotNull(c.LeadDungeon(null, out why), why); Assert.AreEqual(DungeonPlan.Mode.Standard, c.RunMode);
            steps = c.Route.ToList();
            Assert.IsTrue(steps.Exists(x => x.name == "The Old Burrower" && x.follow && x.optional), "the Old Burrower round his pool, followed");
            Assert.AreEqual(c.WingOrder.Contains("The Muster Yard"), steps.Exists(x => x.name == "The Vent-Hound"), "the Vent-Hound when the run goes through the Muster Yard");
            Assert.IsFalse(steps.Exists(x => x.name == "Brack the Bailer" || x.name == "Old Bristle"), "the side rooms are left");
            Assert.Greater(c.CampsLeft, quick);

            // The full run: every side room and side boss, both sides of the fork; a side room as the run passes its mouth.
            c.StopLeading(); Assert.IsNotNull(c.LeadDungeon("full", out why), why); Assert.AreEqual(DungeonPlan.Mode.Full, c.RunMode);
            steps = c.Route.ToList(); Debug.Log("AditTests: the full run's wings: " + string.Join(", ", c.WingOrder) + "; " + c.CampsLeft + " camps");
            int Fight(string camp) { return steps.FindIndex(x => x.camps.Any(i => camps[i].name == camp)); }
            foreach (var side in new[] { "Brack the Bailer", "Old Bristle", "The Vent-Hound", "The Old Burrower", "Old Kettle, the pitch-boiler", "Under the water", "Powder store" })
                Assert.GreaterOrEqual(Fight(side), 0, side + " in the full run");
            Assert.IsTrue(c.WingOrder.Contains("The Scriptorium") && c.WingOrder.Contains("The Muster Yard"), "both sides of the fork");
            Assert.Less(Fight("Brack the Bailer"), Fight("Overlook watch"), "the Flooded Working before the Workings are left");
            Assert.Less(Fight("Old Bristle"), Fight("Overlook watch"), "the Pen before the Workings are left");
            Assert.Greater(Fight("Old Kettle, the pitch-boiler"), Fight("Stair guard"), "the Pitch Pit off the Rail Hall");
            Assert.Greater(c.CampsLeft, quick);
        }
        [UnityTest] public IEnumerator The_peaks_lead_in_and_the_cut_leads_back()
        {
            yield return null; var zone = ZoneBuilder.Active;
            var peaks = zone.FindZone("zone.peaks"); Assert.NotNull(peaks);
            var into = peaks.exits.FirstOrDefault(e => e.to == "zone.adit"); Assert.NotNull(into, "the Peaks' way in at the Sealed Adit");
            Assert.IsTrue(zone.Zone.exits.Any(e => e.to == "zone.peaks"), "the cut back up");
            Assert.IsTrue(SimPopulation.Dungeon(zone, "zone.adit"), "the sims keep out of it on their own");
            Assert.Less(Vector2.Distance(into.arrive, zone.Zone.spawns.player), 6, "you arrive in the yard");
        }
    }
}
#endif
