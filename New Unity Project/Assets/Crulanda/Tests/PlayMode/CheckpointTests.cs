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
    /// The dungeons' checkpoints built and used (Chris, 2026-10-16: "have checkpoints in the dungeon so they don't have to run all the back
    /// from the original spawn area. maybe 3 checkpoints in dungeons"; ZoneBuilder.Checkpoints, EncounterSession.Checkpoints): the Sealed
    /// Adit's three cairns and Hallow's Creek's three still lamps stand on the floor, each with its marker, where you wake on the navmesh in
    /// front of it, and each can be walked to from the yard with the gates open; walking up to one reaches it (a line in the chat); the
    /// furthest along the run reached is where you wake when you fall, not the last touched, and not the yard.
    /// </summary>
    public class CheckpointTests
    {
        string root; EncounterSession session;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-checkpoints-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return null;
        }
        IEnumerator Load(string zone)
        {
            ZoneBuilder.RequestedZoneId = zone;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session, "the session in " + zone);
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
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static float Across(Vector3 a, Vector3 b) { return new Vector2(a.x - b.x, a.z - b.z).magnitude; }

        IEnumerator Placed(string zoneId)
        {
            yield return Load(zoneId);
            var z = session.Zone; Assert.AreEqual(zoneId, z.Zone.id);
            Assert.AreEqual(3, z.Checkpoints.Count, zoneId + " has three checkpoints");
            z.OpenGates(); yield return Frames(60);
            Assert.IsTrue(NavMesh.SamplePosition(z.Ground(z.Zone.spawns.player, .2f), out var yard, 3, NavMesh.AllAreas), "the yard is on the navmesh");
            foreach (var c in z.Checkpoints)
            {
                string who = zoneId + ": " + c.def.name;
                Assert.NotNull(c.root, who + " has its marker"); Assert.Greater(c.root.GetComponentsInChildren<Renderer>().Length, 3, who + " is seen");
                Assert.IsTrue(NavMesh.SamplePosition(c.position + Vector3.up * .3f, out var foot, 1.5f, NavMesh.AllAreas), who + " stands on the floor (" + c.position + ")");
                Assert.Less(Mathf.Abs(foot.position.y - c.position.y), 1, who + ": its foot is on the floor, not over or under it");
                var wake = session.CheckpointWake(c);
                Assert.IsTrue(NavMesh.SamplePosition(wake, out var w, 1.6f, NavMesh.AllAreas), who + ": where you wake is on the navmesh (" + wake + ")");
                Assert.Less(Across(w.position, c.position), 3.5f, who + ": you wake beside it");
                Assert.IsTrue(NavReach.Walkable(yard.position, w.position), who + " can be walked to from the yard");
            }
        }
        [UnityTest, Timeout(600000)] public IEnumerator The_Adit_cairns_stand_on_the_floor_and_can_be_walked_to() { yield return Placed("zone.adit"); }
        [UnityTest, Timeout(600000)] public IEnumerator Hallows_still_lamps_stand_on_the_floor_and_can_be_walked_to() { yield return Placed("zone.hallows"); }

        [UnityTest, Timeout(300000)] public IEnumerator You_wake_at_the_furthest_checkpoint_reached()
        {
            yield return Load("zone.hallows");
            var cps = session.Zone.Checkpoints; Assert.AreEqual(3, cps.Count);
            Assert.IsNull(session.LatestCheckpoint, "none reached on arrival");
            Assert.AreEqual(session.RecoveryPoint, session.RecoverAt, "with none reached you wake where you always did");
            // Walking up to one reaches it.
            session.Player.GetComponent<AdventurerMotor>().Teleport(session.CheckpointWake(cps[0]));
            yield return new WaitForSeconds(.8f);
            Assert.AreSame(cps[0], session.LatestCheckpoint, "reached by coming near it");
            Assert.IsTrue(session.Messages.Any(m => m.StartsWith("Checkpoint reached: " + cps[0].def.name)), "a line in the chat");
            Assert.AreEqual(cps[0].def.name, session.CheckpointsReached[0]);
            // The furthest along the run reached is the one you wake at, whatever the order they were reached in.
            session.ReachCheckpoint(cps[2]); session.ReachCheckpoint(cps[1]);
            Assert.AreSame(cps[2], session.LatestCheckpoint, "the furthest reached, not the last touched");
            Assert.AreEqual(3, session.CheckpointsReached.Count);
            // Dead back in the yard: you wake at the last lamp, on its floor, and Mira with you.
            session.Player.GetComponent<AdventurerMotor>().Teleport(session.RecoveryPoint); yield return Frames(2);
            session.Player.Health.ApplyDamage(session.Player.Health.Pool.Max * 3); yield return null;
            Assert.IsFalse(session.Player.IsAlive);
            session.Recover(); yield return Frames(3);
            Assert.IsTrue(session.Player.IsAlive);
            Assert.Less(Across(session.Player.transform.position, cps[2].position), 4, "woken at " + cps[2].def.name + ", not in the yard");
            Assert.IsTrue(NavMesh.SamplePosition(session.Player.transform.position, out _, 2, NavMesh.AllAreas), "on its floor");
            Assert.AreEqual(cps[2].def.name, session.WokeAt);
            Assert.IsTrue(session.Messages.Any(m => m.StartsWith("You wake at the checkpoint: " + cps[2].def.name)), "the chat says where");
            Assert.Less(Across(session.Companion.transform.position, cps[2].position), 8, "Mira wakes with you");
        }
    }
}
#endif
