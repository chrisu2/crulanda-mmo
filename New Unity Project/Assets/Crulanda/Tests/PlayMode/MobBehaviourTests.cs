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
    /// The Sealed Adit's mob behaviours (DUNGEON_DESIGN.md section 5; dungeon step D5): the Grey Breach's crawlers bolt at a quarter of
    /// their health and come back; the yard's pickets and the carriage gunners have shots to fire at range.
    /// </summary>
    public class MobBehaviourTests
    {
        string root; EncounterSession session;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-mobs-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.adit"; PlayerPrefs.SetInt("test.adit", 1);   // level 11 on arrival
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            EncounterEnemy.BoltSeconds = 6;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        static float Flat(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }

        [Test] public void The_pickets_and_the_gunners_shoot()
        {
            foreach (var mob in new[] { "Sandthrone picket", "Sandthrone carriage gunner" })
            {
                var c = MobCasts.For(mob); Assert.NotNull(c, mob + " has a shot"); Assert.AreEqual("bolt", c.kind, mob); Assert.GreaterOrEqual(c.reach, 20, mob + " fires at range");
            }
            var camps = session.Zone.Zone.camps;
            Assert.AreEqual(.25f, camps.First(c => c.mob == "Greyed crawler").flee, 1e-5f, "the crawlers bolt at a quarter");
            Assert.IsTrue(camps.Where(c => c.elite).All(c => c.flee == 0), "no elite bolts");
        }
        [UnityTest] public IEnumerator A_crawler_bolts_at_a_quarter_of_its_health_and_comes_back()
        {
            EncounterEnemy.BoltSeconds = 1.5f;
            var e = session.Enemies.Find(x => x != null && x.Camp && x.MobName == "Greyed crawler" && x.actor.IsAlive); Assert.NotNull(e, "a crawler");
            Assert.AreEqual(.25f, e.FleeAt, 1e-5f);
            var motor = session.Player.GetComponent<AdventurerMotor>();
            motor.Teleport(e.transform.position - e.transform.forward * 2.5f); session.Select(e);
            float t = 0; while (t < 3 && e.Victim != session.Player) { e.threat.Add(session.Player.EntityId.Value, 10); t += Time.deltaTime; yield return null; }
            Assert.AreEqual(session.Player, e.Victim, "it fights you");
            e.actor.Health.ApplyDamage(Mathf.CeilToInt(e.actor.Health.Pool.Max * .8f)); yield return null; yield return null;
            Assert.IsTrue(e.Bolting, "under a quarter: it breaks and runs"); Assert.IsFalse(e.Controlled, "not a hold"); Assert.IsFalse(e.CanAnswer, "and answers no call");
            Assert.That(e.ControlLabel, Does.StartWith("Fleeing"));
            var here = session.Player.transform.position; float near = Flat(e.transform.position, here);
            yield return new WaitForSeconds(1f);
            Assert.Greater(Flat(e.transform.position, here), near + 1.5f, "it has run from you");
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(e.Bolting, "the run is over");
            Assert.AreEqual(session.Player, e.Victim, "and it is back for you (its threat kept)");
            e.actor.Health.ApplyDamage(1); yield return null;
            Assert.IsFalse(e.Bolting, "once a fight: it fights on");
        }
    }
}
#endif
