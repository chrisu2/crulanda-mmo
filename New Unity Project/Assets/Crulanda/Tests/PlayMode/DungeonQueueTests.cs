#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The dungeon queue, "Gather a Party" (Chris, 2026-10-15: "the dungeons should include an instance que that can be done every 2 days for
    /// loot"; EncounterSession.Queue), and the lockouts in play ("bosses/elites will not respawn"; EncounterSession.Lockout). From the
    /// Shattered Peaks at level 11: queued for the Sealed Adit as damage, a party of four sims forms by role (a tank, a healer, two damage) and
    /// you all arrive at the Adit's door, the dungeon fresh for the run and the tank leading; leaving the party sends the queued sims away. And
    /// an elite your party kills out in the Peaks is locked for you, and after the zone loads again it is not there: dead, its body gone.
    /// </summary>
    public class DungeonQueueTests
    {
        string root; float queueSeconds, leadAfter;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11; EncounterSession.ForgetQueue();
            queueSeconds = EncounterSession.QueueSeconds; leadAfter = EncounterSession.QueueLeadAfter;
            EncounterSession.QueueSeconds = .5f; EncounterSession.QueueLeadAfter = 1;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-queue-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.peaks";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            EncounterSession.ForgetQueue(); EncounterSession.QueueSeconds = queueSeconds; EncounterSession.QueueLeadAfter = leadAfter;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        /// <summary>Waits (up to <paramref name="seconds"/>) for the scene to load again: a new session, standing in <paramref name="zone"/>.</summary>
        static IEnumerator Reloaded(EncounterSession old, string zone, float seconds, Action<EncounterSession> found)
        {
            for (float w = 0; w < seconds; w += Time.unscaledDeltaTime)
            {
                var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
                if (s != null && s != old && s.Player != null && s.ZoneId == zone && SimPopulation.Active != null) { for (int i = 0; i < 5; i++) yield return null; found(s); yield break; }
                yield return null;
            }
            found(null);
        }

        [UnityTest] public IEnumerator Queued_at_level_11_a_party_of_four_forms_by_role_and_arrives_at_the_adits_door()
        {
            var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.NotNull(session); Assert.AreEqual("zone.peaks", session.ZoneId);
            for (float w = 0; w < 6 && SimPopulation.Active == null; w += Time.deltaTime) yield return null;
            Assert.NotNull(SimPopulation.Active, "the sims are about");
            session.Progress.experience = EncounterProgress.XpForLevel(11); Assert.AreEqual(11, session.Progress.Level);
            session.Progress.recruited = false; Assert.AreEqual(0, session.PartySims.Count);
            session.QueueRole = SimCompanion.PartyRole.Damage;

            // The window's list: the Adit, 10-12, fits level 11, no lockouts yet; "any" is the Adit too.
            var adit = session.QueueDungeons().Find(d => d.id == "zone.adit"); Assert.NotNull(adit, "the Sealed Adit is offered");
            Assert.IsTrue(EncounterSession.QueueFits(adit, 11)); Assert.IsFalse(EncounterSession.QueueFits(adit, 5), "not at level 5");
            Assert.AreSame(adit, session.QueueDungeonFor(11));
            var door = session.QueueDoor(adit); Assert.AreEqual(new Vector2(0, -382), door, "the door: where the Peaks' road into the Adit arrives");
            Assert.AreEqual(0, session.QueueLockState(adit).locked, "nothing locked yet");
            Assert.Greater(session.QueueLockState(adit).bosses, 10, "its bosses and elites counted");

            Assert.IsTrue(session.JoinQueue("The Sealed Adit"), "queued");
            Assert.AreEqual("zone.adit", session.QueuedFor, "finding a group");
            Assert.IsFalse(session.JoinQueue("any"), "queued already");

            EncounterSession arrived = null;
            yield return Reloaded(session, "zone.adit", 90, s => arrived = s);
            Assert.NotNull(arrived, "the party is taken to the Sealed Adit");
            session = arrived;
            Assert.AreEqual("zone.adit", session.QueueRunZone); Assert.IsTrue(session.QueueFresh, "the dungeon is fresh for the run");
            var p = session.Player.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(p.x, p.z), door), 3, "you arrive at the door");

            // Four sims, by role round you (damage): a tank, a healer, two damage; all of them the queue's, all near your level, all with you.
            Assert.AreEqual(4, session.PartySims.Count, "a party of four sims: " + string.Join(", ", session.PartySims.Select(c => c.sim.name + " " + c.sim.classId)));
            Assert.AreEqual(1, session.PartySims.Count(c => c.Role == SimCompanion.PartyRole.Tank), "one tank");
            Assert.AreEqual(1, session.PartySims.Count(c => c.Role == SimCompanion.PartyRole.Healer), "one healer");
            Assert.AreEqual(2, session.PartySims.Count(c => c.Role == SimCompanion.PartyRole.Damage), "two damage, and you");
            foreach (var c in session.PartySims)
            {
                Assert.IsTrue(session.QueueSim(c.sim.id), c.sim.name + " came by the queue");
                Assert.LessOrEqual(Mathf.Abs(c.sim.level - 11), EncounterSession.QueueLevelGap, c.sim.name + " is near your level");
                Assert.Less(Vector3.Distance(c.transform.position, session.Player.transform.position), 15, c.sim.name + " stands by you at the door");
            }
            var leader = session.PartySim(session.QueueLeader); Assert.NotNull(leader, "a leader"); Assert.AreEqual(SimCompanion.PartyRole.Tank, leader.Role, "the tank leads");
            for (float w = 0; w < EncounterSession.QueueLeadAfter + 3 && leader.Dungeon == null; w += Time.deltaTime) yield return null;
            Assert.AreEqual("The Sealed Adit", leader.Dungeon, "the leader sets out through the Adit (LeadDungeon)");

            // Leaving: the queued sims go, back where they came from; the run is over.
            var ids = session.PartySims.Select(c => c.sim.id).ToList();
            session.DisbandQueue("test: leave");
            Assert.AreEqual(0, session.PartySims.Count, "the queued sims leave the party");
            Assert.IsNull(session.QueueRunZone); Assert.IsFalse(session.QueueFresh);
            foreach (var id in ids) Assert.AreNotEqual("zone.adit", SimPopulation.Active.World.sims.Find(x => x.id == id).zone, "sent home, not left in the dungeon");
        }

        [UnityTest] public IEnumerator A_killed_elite_is_locked_and_not_there_after_the_zone_loads_again()
        {
            var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.NotNull(session); Assert.AreEqual("zone.peaks", session.ZoneId);
            for (float w = 0; w < 6 && SimPopulation.Active == null; w += Time.deltaTime) yield return null;
            var elite = session.Enemies.Find(e => e != null && EncounterSession.Lockable(e) && e.actor.IsAlive);
            Assert.NotNull(elite, "the Peaks have an elite"); string id = elite.persistentId, key = Lockouts.KeyOf(id);
            Assert.IsFalse(session.LockedFor(elite)); Assert.AreSame(Lockouts.WorldElite, session.LockKindOf(elite), "an open-world elite: a day");
            var pack = session.Enemies.FindAll(e => e != null && e != elite && e.Camp && e.CampIndex == elite.CampIndex && !e.Elite);

            elite.Receive(1000000, session.Player);
            Assert.IsFalse(elite.actor.IsAlive, "killed by you");
            Assert.IsTrue(session.LockedFor(elite), "locked for you");
            Assert.IsTrue(Lockouts.Locked(session.Progress, key)); Assert.AreEqual(1, session.Progress.lockouts.Count);
            Assert.IsFalse(elite.NoLoot, "the first kill: its loot"); Assert.IsTrue(elite.Drops != null && (elite.Drops.Count > 0 || elite.Coins > 0), "something on the body");
            Assert.IsTrue(session.LockoutRows().Exists(r => r.name == elite.MobName && r.where == "The Shattered Peaks" && r.left > TimeSpan.Zero && r.left <= TimeSpan.FromDays(1)), "in the Lockouts list, a day at most");
            Assert.IsTrue(session.KeptAway(elite), "kept away: where it would respawn, it goes");
            foreach (var e in session.Enemies) if (e != null) e.ResetFight();
            yield return null;

            // The zone loads again (the road back into it): the lockout is in the save, and the elite is not there.
            var at = session.Player.transform.position;
            Assert.IsTrue(session.TravelTo(new ZoneExit { to = "zone.peaks", name = "test", arrive = new Vector2(at.x, at.z) }), "the zone loads again");
            EncounterSession again = null;
            yield return Reloaded(session, "zone.peaks", 60, s => again = s);
            Assert.NotNull(again, "back in the Peaks");
            Assert.IsTrue(Lockouts.Locked(again.Progress, key), "the lockout came through the save");
            var back = again.Enemies.Find(e => e != null && e.persistentId == id); Assert.NotNull(back, "the elite's body is accounted for");
            Assert.IsTrue(back.Away, "put away by its lockout"); Assert.IsFalse(back.actor.IsAlive, "dead"); Assert.IsFalse(back.gameObject.activeSelf, "its body gone");
            Assert.IsFalse(again.CanLoot(back), "nothing to loot");
            if (pack.Count > 0) Assert.IsTrue(again.Enemies.Exists(e => e != null && e.Camp && e.CampIndex == back.CampIndex && !e.Elite && e.actor.IsAlive), "its camp's ordinary mobs are there");
            // The lockout over (the clock past its reset): it comes back with its camp.
            var was = Lockouts.UtcNow;
            try
            {
                Lockouts.UtcNow = () => DateTime.UtcNow.AddDays(2);
                for (float w = 0; w < 8 && back.Away; w += Time.deltaTime) yield return null;
                Assert.IsFalse(back.Away, "its lockout over, it is back"); Assert.IsTrue(back.actor.IsAlive); Assert.IsTrue(back.gameObject.activeSelf);
            }
            finally { Lockouts.UtcNow = was; }
        }
    }
}
#endif
