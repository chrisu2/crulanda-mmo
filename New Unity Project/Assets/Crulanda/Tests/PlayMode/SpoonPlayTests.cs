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
    /// The Rusty Spoon in play (the Lowtowns' rework, pick 5; SpoonRules, EncounterSession.Spoon; b1 ch.1): Snivels Gear-Grinder stands at the
    /// bar (ch.1:105); the Triad table takes a stake and pays by the book's rule (ch.1:45); a brawl among the drinkers (ch.1:107) that you wade
    /// into hurts and never kills, and ends with one of them out through the front window into the lane, the window boarded after (ch.1:121).
    /// </summary>
    public class SpoonPlayTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(string zone)
        {
            WorldClock.Hour = 20.5f; ZoneBuilder.RequestedZoneId = zone;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-spoon-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            VillageEconomy.ResetAll();
            Time.timeScale = 1; WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }

        [UnityTest] public IEnumerator Snivels_pours_Triad_pays_and_the_brawl_ends_through_the_window()
        {
            yield return Load("zone.lowtowns");
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.lowtowns", zone.Zone.id);
            var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.IsNotNull(session);
            var life = VillageLife.Active; Assert.IsNotNull(life);
            var spoon = zone.Inns.FirstOrDefault(i => i != null && i.frame != null && i.frame.Find(ZoneBuilder.SpoonWindowName) != null);
            Assert.IsNotNull(spoon, "The Spoon has its front window.");
            Assert.IsFalse(spoon.frame.Find(ZoneBuilder.SpoonBoardsName).gameObject.activeSelf, "Glazed at the start of a visit.");
            for (int i = 0; i < 30; i++) yield return null;

            // Snivels at the bar.
            var snivels = life.Find(SpoonRules.Bartender); Assert.IsNotNull(snivels, "Snivels is spawned.");
            Assert.IsTrue(spoon.Contains(snivels.transform.position), "Snivels stands in the Spoon (at " + snivels.transform.position + ").");

            // Triad: a hand at the card table.
            var table = zone.Interactables.FirstOrDefault(i => i.kind == "triad"); Assert.IsNotNull(table, "The Triad table is in reach.");
            StringAssert.StartsWith("Sit in at Triad", session.InteractPromptFor(table));
            session.Progress.gold = 500; int stake = SpoonRules.Stake(session.Player.Level);
            session.UseInteractable(table);
            var allowed = new[] { 500 - stake, 500, 500 + stake, 500 + 3 * stake };
            CollectionAssert.Contains(allowed, session.Progress.gold, "Lost the stake, a push, a win or the Crown.");
            if (session.SpoonBrawling) { session.SpoonBrawlIn = 9999; Time.timeScale = 4; for (float t = 0; t < 8; t += Time.unscaledDeltaTime) yield return null; Time.timeScale = 1; }

            // Three drinkers set down in the room, and the brawl.
            var seats = spoon.seats.Take(4).ToList();
            var folk = life.Villagers.Where(v => v != null && v.Visible && v.Post == null && !v.Brawling).Take(3).ToList();
            Assert.GreaterOrEqual(folk.Count, 3, "Folk to drink.");
            for (int i = 0; i < folk.Count; i++)
            {
                var agent = folk[i].GetComponent<NavMeshAgent>();
                Assert.IsTrue(NavMesh.SamplePosition(seats[i].at, out var hit, 2, NavMesh.AllAreas), "A seat on the floor.");
                agent.Warp(hit.position);
            }
            Assert.IsTrue(session.StartSpoonBrawl(null), "The brawl takes the room.");
            Assert.GreaterOrEqual(session.SpoonBrawlers.Count, 3);
            CollectionAssert.DoesNotContain(session.SpoonBrawlers.Select(v => v.Name).ToList(), SpoonRules.Bartender, "Snivels keeps behind his bar.");

            // Wade in.
            var motor = session.Player.GetComponent<AdventurerMotor>();
            Assert.IsTrue(NavMesh.SamplePosition(spoon.frame.TransformPoint(new Vector3(.2f, 0, -1.8f)), out var floor, 2, NavMesh.AllAreas));
            motor.Teleport(floor.position + Vector3.up);
            session.Player.Health.ApplyHealing(session.Player.Health.Pool.Max);
            session.JoinSpoonBrawl();
            int max = session.Player.Health.Pool.Max, least = max;
            Time.timeScale = 4;
            for (float t = 0; t < 9 && session.SpoonBrawling; t += Time.unscaledDeltaTime) { least = Mathf.Min(least, session.Player.Health.Pool.Current); yield return null; }
            Time.timeScale = 1;
            Assert.IsFalse(session.SpoonBrawling, "The brawl is over.");
            Assert.IsTrue(session.Player.IsAlive, "A brawl never kills.");
            Assert.GreaterOrEqual(least, Mathf.FloorToInt(max * SpoonRules.JoinFloor) - 1, "Never under a third and a bit of your health.");
            Assert.IsTrue(session.SpoonWindowBroken, "One went through the front window: it is boarded.");
            var lane = spoon.frame.Find(ZoneBuilder.SpoonLaneName).position;
            Assert.IsTrue(life.Villagers.Any(v => v != null && v.KnockedDown && !spoon.Contains(v.transform.position) && Vector3.Distance(v.transform.position, lane) < 3.5f), "Someone lies in the lane's mud outside the window.");
        }
    }
}
#endif
