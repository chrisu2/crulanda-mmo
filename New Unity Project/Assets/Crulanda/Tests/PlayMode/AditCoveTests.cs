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
    /// The Deadmines end-piece (the Adit rework, stage S6, Docs/ADIT_REWORK.md section 20; Chris 2026-10-15, Q3 "Build the Cove"; GAME-ONLY;
    /// written 2026-10-15 outside Unity, not yet run): the Dead Line runs on from the Rail Hall into the Black Cove, whose quay, scaffold, gangway,
    /// barge and stern castle are walked on the navmesh while its black water is swum, not walked; the Haul (rope teams heaving the barge off the
    /// bar: no machine) runs while they are on the ropes and floats her, stops when they are killed or the warps are cut, and afloat sends her
    /// crew up out of the hold to Danner's calls; Danner fights on the stern castle and drops his named loot; the carriage in the Rail Hall rides
    /// you out once the lock is matched or Danner is dead.
    /// </summary>
    public class AditCoveTests
    {
        string root; EncounterSession session;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-aditcove-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.adit";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            AditHaul.HaulSeconds = 75; EncounterSession.RideSeconds = 5; EncounterSession.ReleaseDelay = 3; EncounterSession.Unseen = false;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        ZoneInteractable Thing(string kind) { var i = session.Zone.Interactables.Find(x => x.kind == kind); Assert.NotNull(i, "the Adit has its " + kind); return i; }
        int CampIndex(string name) { int c = Array.FindIndex(session.Zone.Zone.camps, x => x != null && x.name == name); Assert.GreaterOrEqual(c, 0, "the Adit has the camp " + name); return c; }
        ZoneCamp Camp(string name) { return session.Zone.Zone.camps[CampIndex(name)]; }
        List<EncounterEnemy> Mine(int camp) { return session.Enemies.Where(e => e != null && e.Camp && e.CampIndex == camp).ToList(); }
        Vector3 Anchor(string name) { Assert.IsTrue(session.Zone.Anchors.TryGetValue(name, out var at), "the zone has the anchor '" + name + "'"); return at; }
        EncounterEnemy Danner() { return session.Enemies.Find(e => e != null && e.Camp && e.Elite && e.MobName == EncounterSession.DannerName); }
        /// <summary>Where a camp stands: its centre on its floor or up on its deck.</summary>
        Vector3 CampSpot(string name) { var c = Camp(name); return session.Zone.StandAt(c.center, EncounterSession.DeckHeight(c, c.center), 0); }
        static Vector3 OnMesh(Vector3 p, string what)
        {
            Assert.IsTrue(NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas), what + " is on the navmesh (" + p + ")");
            Assert.Less(Mathf.Abs(hit.position.y - p.y), 1.2f, what + ": the navmesh at its height, not a floor above or below it (" + hit.position + " for " + p + ")");
            return hit.position;
        }

        [UnityTest] public IEnumerator The_Cove_is_walked_from_the_Rail_Hall_and_its_black_water_is_swum()
        {
            session.Zone.OpenGates(); yield return new WaitForSeconds(1);   // (a gate's carve is lifted by the navmesh's own update)
            Assert.IsTrue(session.CoveFinale, "Danner's camp stands in the Black Cove (the layout's COVE_FINALE)");
            Assert.IsNotNull(Hollow.All.Find(h => h.Name == "The Dead Line"), "the Dead Line is built");
            var cove = Hollow.All.Find(h => h.Name == "The Black Cove"); Assert.IsNotNull(cove, "the Black Cove is built");
            var hall = OnMesh(Anchor("hall"), "the Rail Hall");
            var places = new List<(string what, Vector3 at)>
            {
                ("the Dead Line's siding", CampSpot("Siding rearguard")),
                ("the quay's north end", Anchor("haul")),
                ("the rope team on the quay", CampSpot("Rope team (north)")),
                ("the scaffold's first tier", CampSpot("Harbour netters")),
                ("the rope team at the quay's south end", CampSpot("Rope team (south)")),
                ("the barge's deck", Anchor("barge bow")),
                ("the stern castle", Anchor("stern castle")),
            };
            var unreached = new List<string>();
            foreach (var (what, at) in places)
            {
                var to = OnMesh(at, what);
                if (!NavReach.Walkable(hall, to)) unreached.Add(what + " " + to);
            }
            Assert.IsEmpty(unreached, "walked from the Rail Hall: " + string.Join("; ", unreached));
            // The barge's deck and her stern castle stand up off the cove's floor, and the gangway is the way aboard.
            float level = session.Zone.CoveWaterLevel; Assert.IsFalse(float.IsNaN(level), "the Cove has its black water");
            Assert.That(Anchor("barge bow").y - level, Is.InRange(3.6f, 4.8f), "her deck about 4.2 m over the water");
            Assert.That(Anchor("stern castle").y - level, Is.InRange(6.9f, 8.1f), "her stern castle about 7.5 m over it");
            Assert.That(Anchor("haul").y - level, Is.InRange(.4f, 1.3f), "the quay about 0.8 m over it");
            // Out in the cove: swim-deep black water, which no agent walks.
            var open = cove.At(cove.Length * .62f, -22);
            Assert.IsTrue(session.Zone.WaterAtPoint(open + Vector3.up * .5f, out float surface, out float depth), "water out in the cove");
            Assert.That(depth, Is.InRange(2.2f, 3f), "2.6 m of it"); Assert.AreEqual(level, surface, .05f);
            Assert.IsFalse(NavMesh.SamplePosition(open, out _, 1.2f, NavMesh.AllAreas), "the cove's bed under the water is no place to walk");
        }

        [UnityTest] public IEnumerator The_haul_runs_while_the_rope_teams_are_on_the_ropes_and_floats_the_barge()
        {
            AditHaul.HaulSeconds = 3; EncounterSession.Unseen = true;   // (no mob notices you: the haulers stay on their ropes)
            var haul = session.Haul; Assert.IsNotNull(haul, "the Black Cove has its haul");
            Assert.AreEqual(AditHaul.Stage.Waiting, haul.Now, "the barge lies aground until you come");
            Assert.AreEqual(6, haul.Haulers.Count, "two rope teams of three");
            Assert.IsTrue(session.Zone.CoveWarps != null && session.Zone.CoveWarps.Length >= 4 && session.Zone.CoveWarps.All(w => w != null && w.activeSelf), "the warps are rove to the bollards");
            // You come onto the quay: the haul starts.
            session.Player.GetComponent<AdventurerMotor>().Teleport(OnMesh(Anchor("haul"), "the quay") + Vector3.up * 1.05f);
            float until = Time.time + 2; while (haul.Now == AditHaul.Stage.Waiting && Time.time < until) yield return null;
            Assert.AreEqual(AditHaul.Stage.Hauling, haul.Now, "coming onto the quay starts it");
            Assert.IsTrue(session.Messages.Any(m => m.Contains("Haul!")), "Danner calls it");
            yield return null; Assert.AreEqual(6, haul.Pulling, "every hauler on a rope");
            until = Time.time + 8; while (haul.Now == AditHaul.Stage.Hauling && Time.time < until) yield return null;
            Assert.AreEqual(AditHaul.Stage.Floated, haul.Now, "hauled off the bar (" + haul.Status + ")");
            Assert.AreEqual(1f, haul.Hauled, 1e-4f);
            // Afloat: each of Danner's calls brings two of her crew up out of the hold, twice.
            var danner = Danner(); Assert.IsNotNull(danner);
            int first = CampIndex("Up from the hold (first)"), second = CampIndex("Up from the hold (second)");
            Assert.IsEmpty(Mine(first), "nobody up from the hold before he calls");
            session.Rally(danner, session.Player); yield return Frames(2);
            Assert.AreEqual(2, Mine(first).Count(e => e.actor.IsAlive), "his first call: two up from the hold");
            Assert.IsEmpty(Mine(second));
            session.Rally(danner, session.Player); yield return Frames(2);
            Assert.AreEqual(2, Mine(second).Count(e => e.actor.IsAlive), "his second call: two more");
            int crew = session.Enemies.Count(e => e != null && e.Camp && (e.CampIndex == first || e.CampIndex == second));
            session.Rally(danner, session.Player); yield return Frames(2);
            Assert.AreEqual(crew, session.Enemies.Count(e => e != null && e.Camp && (e.CampIndex == first || e.CampIndex == second)), "and no more after that");
            foreach (var e in Mine(first).Concat(Mine(second)))
                Assert.IsTrue(NavMesh.SamplePosition(e.transform.position, out _, 1.5f, NavMesh.AllAreas), e.Name + " stands on the barge's deck on the navmesh at " + e.transform.position);
        }

        [UnityTest] public IEnumerator Killing_the_rope_teams_or_cutting_the_warps_leaves_her_fast()
        {
            AditHaul.HaulSeconds = 30; EncounterSession.Unseen = true;
            var haul = session.Haul; Assert.IsNotNull(haul);
            Assert.IsTrue(haul.Begin()); yield return new WaitForSeconds(.5f);
            Assert.Greater(haul.Hauled, 0f, "hauling");
            // A hauler struck leaves his rope: the haul goes as fast as the share still hauling.
            var one = haul.Haulers[0]; one.actor.Health.ApplyDamage(one.actor.Health.Pool.Max * 3); yield return Frames(2);
            Assert.Less(haul.Pulling, 6, "one fewer on the ropes (or fewer still, if his mates answered the blow)");
            foreach (var e in haul.Haulers) e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3);
            yield return Frames(2);
            Assert.AreEqual(AditHaul.Stage.Stopped, haul.Now, "the last of them down"); Assert.AreEqual("haulers", haul.StoppedBy);
            float hauled = haul.Hauled; yield return new WaitForSeconds(.5f);
            Assert.AreEqual(hauled, haul.Hauled, "she stays fast");
            var danner = Danner(); session.Rally(danner, session.Player); yield return Frames(2);
            Assert.IsEmpty(Mine(CampIndex("Up from the hold (first)")), "aground, nobody comes up out of the hold");

            // Another visit: the warps cut at the bollards stop it as well.
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single); yield return Frames(4);
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            for (float until = Time.time + 10; session.Haul == null && Time.time < until;) yield return null;   // (R129: null four frames in on the second load)
            haul = session.Haul;
            Assert.IsNotNull(haul, "the haul on a new visit"); Assert.AreEqual(AditHaul.Stage.Waiting, haul.Now, "aground again on a new visit");
            var warps = Thing("warps"); Assert.AreEqual("Cut the warps", session.InteractPromptFor(warps));
            Assert.IsTrue(haul.Begin());
            session.Player.GetComponent<AdventurerMotor>().Teleport(warps.position + Vector3.up * 1.05f); yield return Frames(2);
            session.UseInteractable(warps); yield return Frames(2);
            Assert.AreEqual(AditHaul.Stage.Stopped, haul.Now, "the warps cut"); Assert.AreEqual("cut", haul.StoppedBy);
            Assert.IsTrue(session.Zone.CoveWarps.All(w => w == null || !w.activeSelf), "gone into the water");
            Assert.IsNull(session.InteractPromptFor(warps), "nothing left to cut");
        }

        [UnityTest] public IEnumerator Danner_fights_on_his_stern_castle_and_drops_his_loot()
        {
            var danner = Danner(); Assert.IsNotNull(danner, "Danner is in the Adit");
            var camp = session.Zone.Zone.camps[danner.CampIndex];
            Assert.AreEqual(16, danner.CampIndex, "his pinned place (elitesSlain keeps)");
            Assert.AreEqual("The Black Cove", camp.cave); Assert.AreEqual("cove", camp.wing);
            Assert.IsFalse(danner.Hidden, "on his feet on the stern castle, not lying in the hall's dark");
            var castle = Anchor("stern castle");
            Assert.Less(Mathf.Abs(danner.transform.position.y - 1 - castle.y), 1.2f, "up on the stern castle (" + castle.y.ToString("0.0") + "), not the water under it: at " + danner.transform.position);
            Assert.Less(Vector2.Distance(new Vector2(danner.transform.position.x, danner.transform.position.z), new Vector2(castle.x, castle.z)), 7, "on the castle's planks");
            var guard = Mine(CampIndex("Rail-Captain's guard")); Assert.AreEqual(2, guard.Count, "his guard with him");
            Assert.IsTrue(guard.All(g => Mathf.Abs(g.transform.position.y - 1 - castle.y) < 1.2f), "on the castle too");
            // He is pulled: his guard answer, and he fights there.
            Assert.Greater(session.Rally(danner, session.Player), 0, "his call brings his guard");
            // Killed: his named loot on the body (the signature list is every time), and the letter's quest reads his id.
            var signature = session.Loot.Drops.Where(d => d.mob == EncounterSession.DannerName).SelectMany(d => d.groups.Where(g => g.signature)).SelectMany(g => g.pick).Select(p => p.item).ToList();
            Assert.IsNotEmpty(signature, "Danner has a signature list");
            danner.actor.Health.ApplyDamage(danner.actor.Health.Pool.Max * 3); yield return Frames(2);
            Assert.IsFalse(danner.actor.IsAlive); Assert.IsTrue(session.DannerDead);
            Assert.IsNotNull(danner.Drops, "his body is rolled as he dies");
            Assert.IsTrue(danner.Drops.Any(d => signature.Contains(d.item)), "one of his named pieces: " + string.Join(", ", danner.Drops.Select(d => d.item)));
            StringAssert.StartsWith("mob.railcaptain.adit.16.", danner.persistentId, "his id, as The Dead Line's kill reads it");
        }

        [UnityTest] public IEnumerator The_carriage_rides_out_once_the_lock_is_matched_or_Danner_is_dead()
        {
            EncounterSession.RideSeconds = .5f;
            var ride = Thing("carriage"); var yard = session.Zone.Ground(session.Zone.Zone.spawns.player, 1.1f);
            var motor = session.Player.GetComponent<AdventurerMotor>(); motor.Teleport(ride.position + Vector3.up);
            Assert.IsFalse(session.RideAllowed, "the lock unmatched and Danner standing");
            StringAssert.Contains("loading", session.InteractPromptFor(ride));
            session.UseInteractable(ride); yield return Frames(2);
            Assert.IsFalse(session.Working, "the Company's carriage takes nobody up");
            Assert.IsTrue(session.Messages.Any(m => m.Contains("Black Cove")), "it says where Danner is");
            // Danner dead on his barge: the goblins walk the carriage up the line (no lock needed).
            var danner = Danner(); danner.actor.Health.ApplyDamage(danner.actor.Health.Pool.Max * 3); yield return Frames(2);
            for (float until = Time.time + 20; session.InCombat && Time.time < until;) yield return null;
            Assert.IsFalse(session.Progress.keys.Contains(EncounterSession.WeaverLock));
            Assert.IsTrue(session.RideAllowed, "Danner dead");
            StringAssert.Contains("one ride out", session.InteractPromptFor(ride));
            // The spare wagon at the Dead Line's end is a way out too, so nobody walks back from the Cove (lesson L2).
            var spare = session.Zone.Interactables.Find(i => i.name == ZoneBuilder.AditSpareWagon); Assert.IsNotNull(spare, "the spare wagon at the line's end");
            Assert.AreEqual("carriage", spare.kind); StringAssert.Contains("one ride out", session.InteractPromptFor(spare));
            Assert.IsTrue(NavMesh.SamplePosition(spare.position, out _, 2.5f, NavMesh.AllAreas), "stood beside on the Dead Line's floor");
            motor.Teleport(ride.position + Vector3.up); yield return Frames(2);
            session.UseInteractable(ride); Assert.IsTrue(session.Working, "on the bar");
            yield return new WaitForSeconds(.8f);
            Assert.IsFalse(session.Working);
            Assert.Less(Vector3.Distance(session.Player.transform.position, yard), 4, "at the siding under the yard");
            // A new visit, Danner standing again: the lock alone frees it, as before S6.
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single); yield return Frames(4);
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.IsFalse(session.DannerDead, "every camp is back on a new visit");
            Assert.IsFalse(session.RideAllowed);
            session.Progress.keys.Add(EncounterSession.WeaverLock);
            Assert.IsTrue(session.RideAllowed, "the lock matched");
            StringAssert.Contains("one ride out", session.InteractPromptFor(Thing("carriage")));
        }
    }
}
#endif
