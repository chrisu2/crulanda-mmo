#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Hallow's Creek built and walked (Docs/DUNGEON2_DESIGN.md; the Adit's tests' patterns): the zone builds on open ground with its decks,
    /// gates and anchors (N1, N2), the slabs carry the navmesh across the chasm and nothing reaches its floor (N3), every camp's mobs can be
    /// walked to from the yard with the gates open, a fall into the fog comes back (N3), a hungry shadow slows (N4), the Warden's cell opens
    /// on the Registrar's death and she unbinds the ward-lock at the tower, the wagon takes you home once the Picker-Master is down (N7), and a
    /// sim leads the run: road, bridge, street, Archive, tower, waiting only at the ward-lock.
    /// </summary>
    public class HallowsNavTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-hallows-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.hallows";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null; EncounterSession.ReleaseDelay = 3; EncounterSession.RideSeconds = 5;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static Vector3 On(Vector3 p, string what, float reach = 3) { Assert.IsTrue(NavMesh.SamplePosition(p, out var hit, reach, NavMesh.AllAreas), what + " is on the navmesh (" + p + ")"); return hit.position; }

        [UnityTest, Timeout(300000)] public IEnumerator It_builds_on_open_ground_and_the_slabs_carry_the_way_over_the_chasm()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.hallows", zone.Zone.id); Assert.IsTrue(zone.Zone.dungeon);
            yield return Frames(10);
            Assert.Greater(zone.DeckSpots.Count(d => d.cave == null), 40, "the open-ground decks");
            foreach (var a in new[] { "the gatehouse", "the Warden's cell", "the tower door", "the belfry stair-head", "the wagon", "the Cut-Store" }) Assert.IsTrue(zone.Anchors.ContainsKey(a), "the anchor " + a);
            Assert.AreEqual(18, zone.Anchors["the belfry stair-head"].y - zone.Datum, .5f, "the belfry stair-head is 18 m up");
            Assert.AreEqual(2, zone.Anchors["the Warden's cell"].y - zone.Datum, .4f, "the Warden's cell is on the Archive's floor");
            foreach (var t in new[] { "Tredway's gate", EncounterSession.WardenCell, EncounterSession.WardGate, "The yard bar", "The dark cellar door" }) Assert.IsTrue(zone.Gates.Exists(g => g.Title == t), "the gate " + t);
            Assert.Greater(zone.PausedSpots.Count, 15, "the paused townsfolk");
            // The chasm: its floor is forty metres down and out of the navmesh; the slabs carry the way from the lip to the gatehouse.
            var pit = zone.Zone.pits[0]; Assert.Less(zone.HeightAt(pit.center.x + 40, pit.center.y), zone.Datum - 30, "the chasm is deep");
            Assert.IsFalse(NavMesh.SamplePosition(new Vector3(pit.center.x + 40, zone.Datum - pit.depth, pit.center.y), out _, 3, NavMesh.AllAreas), "no navmesh at the chasm's floor");
            zone.OpenGates(); yield return Frames(60);
            var yard = On(zone.Ground(zone.Zone.spawns.player, .2f), "the yard");
            var gatehouse = On(zone.Anchors["the gatehouse"] + Vector3.up * .2f, "the gatehouse");
            float over = NavReach.Length(yard, gatehouse); Assert.Less(over, 400, "over the slabs from the yard to the gatehouse (" + over.ToString("0") + " m)");
            // The lower orbit: down by the drop, back up the stair to the fifth slab; a camp's mob never takes the drop.
            var third = zone.DeckSpots.Find(d => d.def != null && d.def.name == "The third slab"); var mason = zone.DeckSpots.Find(d => d.def != null && d.def.name == "The Mason's slab");
            var top = On(third.mid[third.mid.Length / 2] + Vector3.up * .2f, "the third slab"); var low = On(mason.mid[mason.mid.Length / 2] + Vector3.up * .2f, "the Mason's slab");
            Assert.Less(NavReach.Length(top, low), 80, "down to the Mason by the drop"); Assert.Less(NavReach.Length(low, top), 120, "and back up the stair of small slabs");
            // The belfry by the tower's flights, and the Archive's floor by its steps.
            Assert.Less(NavReach.Length(On(zone.Anchors["the tower door"] + Vector3.up * .2f, "the tower door"), On(zone.Anchors["the belfry stair-head"] + Vector3.up * .2f, "the belfry stair-head")), 120, "up the tower to the belfry");
            Assert.Less(NavReach.Length(On(zone.Anchors["the Archive's steps"] + Vector3.up * .2f, "the Archive's steps"), On(zone.Anchors["the Warden's cell"] + Vector3.up * .2f, "the Warden's cell")), 120, "into the Archive and to the Warden's cell");
            Assert.Less(NavReach.Length(yard, On(zone.Anchors["the Cut-Store"] + Vector3.up * .2f, "the Cut-Store", 5)), 700, "down the Cellars to the Cut-Store");
        }

        [UnityTest, Timeout(300000)] public IEnumerator Every_camp_can_be_walked_to_from_the_yard()
        {
            var s = Session(); Assert.NotNull(s); Assert.AreEqual("zone.hallows", s.ZoneId);
            s.Zone.OpenGates(); yield return Frames(60);
            var start = On(s.Player.transform.position, "the yard", 4);
            var bad = new List<string>(); int n = 0;
            foreach (var e in s.Enemies)
            {
                if (e == null || !e.Camp || e.Game) continue; n++;
                if (!NavMesh.SamplePosition(e.transform.position, out var at, 4f, NavMesh.AllAreas)) { bad.Add(e.Name + " at " + e.transform.position.ToString("0.0") + ": off the mesh"); continue; }
                if (float.IsPositiveInfinity(NavReach.Length(start, at.position)) && float.IsPositiveInfinity(NavReach.Length(at.position, start))) bad.Add(e.Name + " at " + at.position.ToString("0.0") + ": no way there");
                if (n % 10 == 0) yield return null;
            }
            Assert.Greater(n, 80, "Hallow's Creek's camp mobs");
            Assert.IsEmpty(bad, "camp mobs no one can walk to:\n" + string.Join("\n", bad));
            // Every raised camp stands up on its deck (a slab, a loft, the belfry), not on the land under it.
            foreach (var cp in s.Zone.Zone.camps.Where(c => string.IsNullOrEmpty(c.cave) && Mathf.Abs(c.rise) > .01f && string.IsNullOrEmpty(c.after)))
            {
                int c = Array.IndexOf(s.Zone.Zone.camps, cp); var mine = s.Enemies.FindAll(e => e != null && e.CampIndex == c);
                if (mine.Count == 0) continue;   // (a rare not here this visit)
                float want = s.Zone.Datum + cp.rise;
                Assert.IsTrue(mine.Exists(e => Mathf.Abs(e.transform.position.y - 1 - want) < 1.2f), cp.name + " stands on its deck at " + want.ToString("0.0") + ": " + string.Join(", ", mine.Select(e => (e.transform.position.y - 1).ToString("0.0"))));
            }
        }

        [UnityTest] public IEnumerator A_fall_into_the_fog_comes_back_and_a_shadow_slows()
        {
            var s = Session(); var zone = s.Zone; yield return Frames(5);
            var motor = s.Player.GetComponent<AdventurerMotor>();
            var first = zone.DeckSpots.Find(d => d.def != null && d.def.name == "The near Table");
            motor.Teleport(first.mid[0] + Vector3.up * .3f); yield return Frames(20);
            var stood = s.Player.transform.position;
            var pit = zone.Zone.pits[0];
            motor.Teleport(new Vector3(pit.center.x + 30, zone.Datum + pit.fog - 3, pit.center.y)); yield return Frames(4);
            Assert.Greater(s.Player.transform.position.y, zone.Datum + pit.fog + 1, "the fog put you back");
            Assert.Less(Vector3.Distance(s.Player.transform.position, stood), 6, "where you last stood");
            var shadow = zone.Hazards[0]; var mid = (shadow.a + shadow.b) / 2;
            motor.Teleport(new Vector3(mid.x, shadow.floor + .3f, mid.y)); yield return Frames(10);
            Assert.Less(s.GroundSlow, 1, "the shadow slows you");
            motor.Teleport(zone.Ground(zone.Zone.spawns.player, .3f)); yield return Frames(10);
            Assert.AreEqual(1, s.GroundSlow, 1e-4f, "and out of it you are your own pace again");
        }

        [UnityTest, Timeout(300000)] public IEnumerator The_warden_walks_to_the_tower_and_unbinds_the_ward_lock()
        {
            var s = Session(); var zone = s.Zone; yield return Frames(5);
            var ward = zone.Gates.Find(g => g.Title == EncounterSession.WardGate); var cell = zone.Gates.Find(g => g.Title == EncounterSession.WardenCell);
            Assert.IsFalse(ward.Open, "the ward-lock shut"); Assert.IsFalse(cell.Open, "the Warden's cell shut");
            // The Registrar down: his death opens her cell, and she sets off.
            var pardew = s.Enemies.Find(e => e != null && e.MobName == "Registrar Dunstan Pardew"); Assert.NotNull(pardew);
            foreach (var e in s.Enemies.ToArray()) if (e != null && e.Camp && e.actor.IsAlive && e.MobName != "Registrar Dunstan Pardew") e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 10);   // nobody to fight on her way
            yield return Frames(5);
            pardew.Receive(1000000, s.Player); yield return Frames(30);
            Assert.IsTrue(cell.Open, "the cell opens as the Registrar falls");
            Assert.NotNull(s.Warden, "the Warden walks"); Assert.AreEqual(EncounterSession.WalkEvent, s.EventRunning, "the walk's waves are coming");
            for (float w = 0; w < 150 && !s.Warden.Arrived; w += Time.deltaTime) { Time.timeScale = 4; yield return null; }
            Time.timeScale = 1;
            Assert.IsTrue(s.Warden.Arrived, "she gets to the tower's door");
            Assert.IsTrue(ward.Open, "and the ward-lock is unbound"); Assert.IsTrue(s.Progress.keys.Contains(EncounterSession.WardKey), "for good");
        }

        [UnityTest] public IEnumerator The_wagon_takes_you_home_once_the_picker_master_is_down()
        {
            var s = Session(); var zone = s.Zone; yield return Frames(5); EncounterSession.RideSeconds = .2f;
            var wagon = zone.Interactables.Find(i => i.kind == "ride"); Assert.NotNull(wagon, "the black-iron wagon");
            StringAssert.Contains("Picker-Master", s.InteractPromptFor(wagon), "his, while he stands");
            var yelland = s.Enemies.Find(e => e != null && e.MobName == "Picker-Master Fulke Yelland"); Assert.NotNull(yelland);
            yelland.Receive(1000000, s.Player); yield return Frames(10);
            foreach (var e in s.Enemies.ToArray()) if (e != null && e.actor.IsAlive) e.ResetFight();
            yield return Frames(10);
            s.Player.GetComponent<AdventurerMotor>().Teleport(wagon.position + Vector3.up * .3f + Vector3.right * 1.5f); yield return Frames(5);
            s.UseInteractable(wagon);
            for (float w = 0; w < 10 && Vector2.Distance(new Vector2(s.Player.transform.position.x, s.Player.transform.position.z), zone.Zone.ride.arrive) > 5; w += Time.deltaTime) yield return null;
            Assert.Less(Vector2.Distance(new Vector2(s.Player.transform.position.x, s.Player.transform.position.z), zone.Zone.ride.arrive), 5, "at the milestone");
        }

        /// <summary>A sim leads the run (DungeonPlan on open ground): road, bridge, street, the Archive, the tower; Tredway's gate opens on the way,
        /// so the run waits only at the ward-lock; the standard run takes the Granary's bosses and the Reader, the full run the Cellars too.</summary>
        [UnityTest] public IEnumerator A_sim_leads_the_run_wing_by_wing()
        {
            yield return null; var session = Session(); var zone = ZoneBuilder.Active.Zone; var camps = zone.camps;
            var pop = SimPopulation.Active; var s = pop.World.sims.First();
            s.zone = "zone.hallows"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = 17; session.Progress.experience = EncounterProgress.XpForLevel(16);
            var p = session.Player.transform.position; s.x = p.x + 2; s.z = p.z; pop.Refresh(); yield return null;
            Assert.IsTrue(session.Invite(s.id, true)); var c = session.PartySim(s.id);
            Assert.IsNotNull(c.LeadDungeon("quick", out var why), why); Assert.AreEqual("Hallow's Creek", c.Dungeon);
            var wings = c.WingOrder.ToList(); Debug.Log("HallowsNavTests: the quick run's wings: " + string.Join(", ", wings) + "; " + c.CampsLeft + " camps");
            CollectionAssert.AreEqual(new[] { "The Ash Road", "The Bridge of Shattered Glass", "Main Street", "The Old Archive", "The Bell Tower" }, wings);
            Assert.AreEqual(EncounterSession.WardGate, c.StoppedBy, "the run waits at the ward-lock, and only there");
            var steps = c.Route.ToList();
            Assert.IsTrue(steps.Last(x => !x.regroup).camps.Any(i => camps[i].mob == EncounterSession.ExaminerName), "the Examiner last");
            Assert.AreEqual("Tredway's gate", steps.First(x => x.wing == "street").gate.Title, "Main Street behind Tredway's gate");
            int quick = c.CampsLeft;
            c.StopLeading(); Assert.IsNotNull(c.LeadDungeon(null, out why), why); Assert.Greater(c.CampsLeft, quick, "the standard run takes more");
            Assert.IsTrue(c.Route.Any(x => x.camps.Any(i => camps[i].mob == "Picker-Master Fulke Yelland")), "the Granary's Picker-Master");
            c.StopLeading(); Assert.IsNotNull(c.LeadDungeon("full", out why), why);
            Assert.IsTrue(c.Route.Any(x => x.camps.Any(i => camps[i].mob == "First-Cut")), "the Cellars in the full run");
            // The queue offers it at 15-20, and its door is where Oakhaven's road into it arrives.
            var d = session.QueueDungeons().Find(z => z.id == "zone.hallows"); Assert.NotNull(d, "the queue lists Hallow's Creek");
            Assert.IsTrue(EncounterSession.QueueFits(d, 17)); Assert.Less(Vector2.Distance(session.QueueDoor(d), zone.spawns.player), 6, "the door: the yard at the milestone");
        }
    }
}
#endif
