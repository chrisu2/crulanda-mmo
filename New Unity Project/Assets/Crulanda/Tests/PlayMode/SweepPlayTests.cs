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
    /// The Sweep in play (Lowtowns pick 9; EncounterSession.Sweep): coming to Pell Row's dead end sounds the horns, puts the warders out and the
    /// child in her hiding place, and a Seeker on a far roof that is only a figure; E takes her hand; she climbs the rain-spout with you,
    /// follows you along the walks and over the rope bridges (never down in the lane), comes down the fire escape with you and is taken in at
    /// Brother Aldo's soup-hall (the key kept, the day's pay). And a warder near her in the open lane fails the attempt, which resets a while
    /// later.
    /// </summary>
    public class SweepPlayTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load()
        {
            WorldClock.Hour = 11; ZoneBuilder.RequestedZoneId = "zone.lowtowns";
            root = Path.Combine(Path.GetTempPath(), "Crulanda-sweep-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 30; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            VillageEconomy.ResetAll();
            Time.timeScale = 1; WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            EncounterSession.SweepResetSeconds = 40; EncounterSession.SweepAgainSeconds = 120;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static void Put(EncounterSession s, Vector3 at) { s.Player.GetComponent<AdventurerMotor>().Teleport(at + Vector3.up * .1f); }
        static float Flat(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        /// <summary>Waits (game time, up to <paramref name="seconds"/>) until <paramref name="done"/>.</summary>
        static IEnumerator Until(Func<bool> done, float seconds)
        {
            for (float t = 0; t < seconds && !done(); t += Time.deltaTime) yield return null;
        }
        /// <summary>The horns sounded: you at the child's hiding place, the sweep begun.</summary>
        IEnumerator Horns(EncounterSession s)
        {
            Assert.AreEqual(EncounterSession.SweepStage.Quiet, s.SweepNow, "the lane is quiet on arrival");
            Assert.IsTrue(NavMesh.SamplePosition(s.SweepHide + Vector3.right * 2, out var near, 3, NavMesh.AllAreas), "ground by her hiding place");
            Put(s, near.position + Vector3.up);
            yield return Until(() => s.SweepNow == EncounterSession.SweepStage.Hiding, 3);
            Assert.AreEqual(EncounterSession.SweepStage.Hiding, s.SweepNow, "the horns sound at Pell Row's dead end");
            Assert.IsTrue(s.Messages.Any(m => m.Contains("two-tone wail")), "the horns' wail");
            Assert.GreaterOrEqual(s.SweepWarders.Count, 2, "the warders are out on the sweep");
            Assert.IsTrue(s.SweepWarders.All(e => e.Patrol != null && e.Patrol.Length >= 2), "they walk the lane");
            Assert.IsNotNull(s.SweepChild, "the child in her hiding place"); Assert.IsNotNull(s.SweepChildThing);
            // The Seeker: glimpsed far off, up on a roof, and nothing to fight.
            var seeker = s.SeekerGlimpse; Assert.IsNotNull(seeker, "a Seeker on a far roof");
            Assert.Greater(Flat(seeker.position, s.SweepHide), 44, "far off");
            Assert.Greater(seeker.position.y - s.SweepHide.y, 8, "up on a roof");
            Assert.IsNull(seeker.GetComponentInChildren<Collider>(), "nothing to strike"); Assert.IsNull(seeker.GetComponent<Crulanda.Gameplay.Actor>(), "never fought");
            Assert.IsFalse(s.Enemies.Any(e => e != null && e.transform == seeker));
        }

        [UnityTest, Timeout(300000)] public IEnumerator The_child_follows_you_over_the_roofs_and_is_taken_in_at_the_soup_hall()
        {
            yield return Load();
            var s = Session(); Assert.IsNotNull(s); var zone = ZoneBuilder.Active; Assert.AreEqual("zone.lowtowns", zone.Zone.id);
            var roofs = zone.LowtownRoofs; Assert.IsNotNull(roofs, "the roofs over Pell Row");
            yield return Horns(s);
            // Fight them, the other way past (the test's: they are put down so nobody sees her while the way over the roofs is walked).
            foreach (var e in s.SweepWarders) e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 10);
            yield return null;
            int gold = s.Progress.gold;
            s.UseInteractable(s.SweepChildThing);
            Assert.AreEqual(EncounterSession.SweepStage.Led, s.SweepNow, "E takes her hand");
            var child = s.SweepChild;

            // Up the rain-spout, together.
            var spout = roofs.things.First(kv => kv.Value.kind == "climb" && kv.Value.name == ZoneBuilder.SpoutName);
            Put(s, spout.Value.a + Vector3.up);
            Time.timeScale = 3;
            yield return Until(() => Flat(child.position, spout.Value.a) < 4, 6);
            s.UseInteractable(spout.Key);
            yield return Until(() => s.Player.transform.position.y > spout.Value.b.y - 1, 8);
            Assert.Greater(s.Player.transform.position.y, spout.Value.b.y - 1, "you are up on the walk");
            Assert.IsFalse(s.SweepChildInLane(), "she climbed with you");

            // Along the walks and over the rope bridges, she at your heels: the row's three gutters, then over Pell Row to the far terrace.
            var decks = roofs.legs.Where(l => l.kind == "deck").ToList(); var bridges = roofs.legs.Where(l => l.kind == "bridge").ToList();
            var lane = bridges.Single(b => b.name == "The rope bridge over Pell Row");
            var way = new List<Vector3> { decks[0].a, decks[0].b, decks[1].a, decks[1].b, decks[2].a, lane.a, lane.b, decks[3].b };
            var steps = new List<Vector3>();
            for (int k = 0; k + 1 < way.Count; k++)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(way[k], way[k + 1]) / 5));
                for (int m = 1; m <= n; m++) steps.Add(Vector3.Lerp(way[k], way[k + 1], m / (float)n));
            }
            foreach (var at in steps)
            {
                Put(s, at + Vector3.up);
                yield return Until(() => Flat(child.position, s.Player.transform.position) < 3.5f && Mathf.Abs(child.position.y - s.Player.transform.position.y) < 2, 10);
                Assert.Less(Flat(child.position, s.Player.transform.position), 3.5f, "she follows you over the roofs (at " + at + ")");
                Assert.IsFalse(s.SweepChildInLane(), "she keeps to the roofs (at " + at + ")");
            }

            // Down the fire escape, together.
            var down = roofs.things.First(kv => kv.Value.kind == "down");
            s.UseInteractable(down.Key);
            yield return Until(() => s.Player.transform.position.y < down.Value.b.y + 2.5f, 8);
            Assert.Less(s.Player.transform.position.y, down.Value.b.y + 2.5f, "you are down in the lane");
            Assert.IsTrue(s.SweepChildInLane(), "she came down with you");
            Assert.AreEqual(EncounterSession.SweepStage.Led, s.SweepNow);

            // Along the lane to Brother Aldo: she follows, and he takes her in.
            var aldo = s.SweepKeeperAt();
            var from = s.Player.transform.position; int legsTo = Mathf.Max(1, Mathf.CeilToInt(Flat(from, aldo) / 8));
            for (int m = 1; m <= legsTo && !s.SweepDelivered; m++)
            {
                var at = Vector3.Lerp(from, aldo, m / (float)legsTo);
                if (!NavMesh.SamplePosition(at, out var hit, 4, NavMesh.AllAreas)) continue;
                Put(s, hit.position + Vector3.up);
                yield return Until(() => s.SweepDelivered || Flat(child.position, s.Player.transform.position) < 3.5f, 12);
            }
            yield return Until(() => s.SweepDelivered, 15);
            Time.timeScale = 1;
            Assert.IsTrue(s.SweepDelivered, "Brother Aldo takes her in");
            Assert.AreEqual(EncounterSession.SweepStage.Ended, s.SweepNow);
            Assert.IsTrue(s.Progress.keys.Contains(EncounterSession.SweepKey), "kept for good (the quest's flag)");
            Assert.AreEqual(gold + EncounterSession.SweepGold, s.Progress.gold, "the day's pay");
        }

        [UnityTest, Timeout(300000)] public IEnumerator A_warder_near_her_in_the_lane_fails_the_attempt_and_it_resets()
        {
            yield return Load();
            var s = Session(); Assert.IsNotNull(s);
            EncounterSession.SweepResetSeconds = 1; EncounterSession.SweepAgainSeconds = 1;
            yield return Horns(s);
            s.UseInteractable(s.SweepChildThing);
            Assert.AreEqual(EncounterSession.SweepStage.Led, s.SweepNow);
            Assert.IsTrue(s.SweepChildInLane());
            var w = s.SweepWarders[0]; var agent = w.GetComponent<NavMeshAgent>();
            Assert.IsTrue(NavMesh.SamplePosition(s.SweepChild.position + Vector3.right * 3, out var hit, 3, NavMesh.AllAreas));
            agent.Warp(hit.position);
            yield return Until(() => s.SweepNow != EncounterSession.SweepStage.Led, 2);
            Assert.AreEqual(EncounterSession.SweepStage.Ended, s.SweepNow, "a warder saw her in the lane");
            Assert.IsFalse(s.SweepDelivered); Assert.IsFalse(s.Progress.keys.Contains(EncounterSession.SweepKey));
            Assert.IsTrue(s.SweepChild == null, "she slipped away");
            yield return Until(() => s.SweepNow == EncounterSession.SweepStage.Quiet, 4);
            Assert.AreEqual(EncounterSession.SweepStage.Quiet, s.SweepNow, "the lane is quiet again a while later: the sweep can come again");
        }
    }
}
#endif
