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
    /// The Sealed Adit, the dungeon under the Shattered Peaks (Docs/DUNGEON_DESIGN.md, step D1): its own zone, a network of caves (the
    /// main way down to the Rail Hall and three branches off the Singing Gallery, each opening through the Gallery's wall), every
    /// camp on its own cave's floor and reachable on foot from the yard, the Peaks' way in and the cut back up, and a sim's run
    /// through all of it in order.
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
        const string Main = "The Sealed Adit";
        static readonly string[] Branches = { "The Geode Floor", "The Ember Vent", "The Grey Breach" };
        static Hollow Cave(string name) { return Hollow.All.Find(h => h.Name == name); }

        [UnityTest] public IEnumerator The_network_is_built_and_every_camp_is_walkable_from_the_yard()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.adit", zone.Zone.id); Assert.IsTrue(zone.Zone.dungeon, "a dungeon");
            zone.OpenGates(); for (int i = 0; i < 3; i++) yield return null;   // the way through, with its gates open (D3; AditGateTests has them shut)
            var main = Cave(Main); Assert.NotNull(main, "the main way"); Assert.IsNull(main.Parent);
            foreach (var b in Branches) { var h = Cave(b); Assert.NotNull(h, b); Assert.AreSame(main, h.Parent, b + " opens out of the Gallery"); }
            Assert.Less(main.Centre[main.Centre.Count - 1].y, main.Centre[0].y - 35, "the Rail Hall lies deep under the Peaks");
            Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(zone.Zone.spawns.player, .2f), out var start, 2.5f, NavMesh.AllAreas), "the yard is on the navmesh");
            var path = new NavMeshPath();
            var walked = new System.Collections.Generic.List<(Hollow cave, float s, Vector3 at)>();   // each step's floor point, for the camps below
            // Down each cave in six-metre steps first, so a break in the floor names its stretch.
            foreach (var h in Hollow.All)
            {
                var from = h.Parent == null ? start.position : (NavMesh.SamplePosition(h.At(1) + Vector3.up * .2f, out var m, 2, NavMesh.AllAreas) ? m.position : start.position);
                for (float s = 3; s < h.Length - 4; s += 6)
                {
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
            Assert.GreaterOrEqual(mobs.Count(e => e.Patrol != null), 16, "eight patrols of two walk the drifts");
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
            var still = mobs.Where(e => e != null && e.actor.IsAlive && !e.Hidden && e.Inside == null && !moved.Contains(e)).ToList();
            Assert.Less(still.Count, mobs.Count * .25f, still.Count + " of " + mobs.Count + " stood still: " + string.Join(", ", still.Take(12).Select(e => e.actor.DisplayName)));
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
                // From in the Gallery, just inside the branch's mouth, straight down the branch: nothing of the Gallery's wall in the way.
                var from = h.At(1) + Vector3.up * 1.4f; var to = h.At(24) + Vector3.up * 1.4f;   // (10 m in the first Adit; its mouths drawn out three times)
                // (The rock, not who stands in it: a mob strolling in the mouth is no wall.)
                var rock = Physics.RaycastAll(from, (to - from).normalized, Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore).Where(x => x.collider.GetComponentInParent<Crulanda.Gameplay.Actor>() == null).ToList();
                Assert.IsEmpty(rock.Select(x => x.collider.name + " at " + x.point).ToList(), b + ": the way in is open");
                Assert.IsTrue(Cave(Main).Open(h.At(1) + Vector3.up * 1.4f), b + "'s mouth is in the Gallery's air");
            }
        }
        [UnityTest] public IEnumerator Every_camp_is_spawned_where_it_stands()
        {
            yield return null; var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            foreach (var cp in ZoneBuilder.Active.Zone.camps)
                if (cp.rare <= 0)   // a rare camp is there one visit in a few (AditGateTests)
                    Assert.IsTrue(session.Enemies.Exists(e => e != null && e.Camp && Vector2.Distance(new Vector2(e.transform.position.x, e.transform.position.z), cp.center) < cp.radius + 2.5f), cp.name + " has its mobs");
            Assert.IsTrue(session.Enemies.Exists(e => e != null && e.Elite && e.actor.DisplayName.Contains("Orsk Danner")), "the Rail-Captain waits at the end");
        }
        [UnityTest] public IEnumerator A_sim_leads_the_whole_adit_in_order()
        {
            yield return null; var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            ZoneBuilder.Active.OpenGates();   // the whole of it: the gates open (with them shut the run stops at the lift: AditGateTests)
            var pop = SimPopulation.Active; var s = pop.World.sims.First();
            s.zone = "zone.adit"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = 11; session.Progress.experience = EncounterProgress.XpForLevel(9);   // the Adit is 10-12 (round 29)
            var p = session.Player.transform.position; s.x = p.x + 2; s.z = p.z; pop.Refresh(); yield return null;
            Assert.IsTrue(session.Invite(s.id, true)); var c = session.PartySim(s.id);
            Assert.IsNotNull(c.LeadDungeon(out var why), why); Assert.AreEqual("The Sealed Adit", c.Dungeon);
            Assert.AreEqual(ZoneBuilder.Active.Zone.camps.Length, c.CampsLeft, "every camp on the way");
            Assert.AreEqual("Adit pickets", c.LeadingName, "the first in from the mouth");
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
