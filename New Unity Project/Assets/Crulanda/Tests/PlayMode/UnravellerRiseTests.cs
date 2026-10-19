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
using Crulanda.Core;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Unraveller rising at the Drain Reach's silt bar (Docs/WORLD_BOSS_1.md 1.3-1.4, build step 3; EncounterSession.Unraveller), in the
    /// Lowtowns, the world's clock brought to its moments (the test's own save folder; never Chris's):
    /// - the silt bar is built and walkable from the apron across its neck;
    /// - five minutes before: the System line, the reach's water stilled and grey, the Drain Courts' lamps out, Tibb Marrow and Ketta Rune
    ///   calling in Zone chat and standing by their salt barrel and harpoon rack on the apron;
    /// - she rises out of the river at the bar (the line in the chat), level 30, a raid boss for ten ("Raid (10)", the Boss rank), not scaled
    ///   to a group, three times a Weave-Eater's size, her pool 112,000;
    /// - after 45 minutes she sinks and is gone, and the reach is as it was.
    /// </summary>
    public class UnravellerRiseTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(float hour)
        {
            WorldClock.Hour = hour; ZoneBuilder.RequestedZoneId = "zone.lowtowns";
            root = Path.Combine(Path.GetTempPath(), "Crulanda-unraveller-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 30; i++) yield return null;
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
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static float Flat(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        static IEnumerator Until(Func<bool> done, float seconds) { for (float t = 0; t < seconds && !done(); t += Time.deltaTime) yield return null; }

        [UnityTest, Timeout(300000)] public IEnumerator She_rises_at_the_silt_bar_and_sinks_after_forty_five_minutes()
        {
            yield return Load(21);
            var s = Session(); Assert.IsNotNull(s); var zone = ZoneBuilder.Active; Assert.AreEqual("zone.lowtowns", zone.Zone.id);
            var w = s.WorldBossNow; Assert.IsNotNull(w, "the world's clock"); Assert.AreEqual(WorldBossState.Waiting, w.state, "a new world: waiting");
            Assert.IsNull(s.UnravellerFoe, "she is not here"); Assert.IsFalse(zone.ReachStill); Assert.IsFalse(zone.ReachStockOut);
            var bar = zone.SiltBar; Assert.IsNotNull(bar, "the silt bar");
            var barTop = zone.Ground(bar.center);
            Assert.IsFalse(zone.WaterAt(bar.center, out _, out _), "the bar stands out of the water");
            Assert.IsTrue(NavMesh.SamplePosition(barTop, out var onBar, 1.5f, NavMesh.AllAreas), "the bar is walkable");
            Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(zone.ReachApron.center), out var onApron, 1.5f, NavMesh.AllAreas), "and the apron");
            var path = new NavMeshPath();
            Assert.IsTrue(NavMesh.CalculatePath(onApron.position, onBar.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "from the apron onto the bar");
            Assert.IsTrue(zone.WaterAt(bar.center + new Vector2(bar.size.x / 2 + 4, 0), out _, out _), "the river beside it (east)");

            // Five minutes before.
            int lit = zone.NightLights.Count(l => l != null && l.light != null && l.light.enabled);
            yield return null; Assert.Greater(w.riseAt, WorldBossClock.Cycle - 1, "the clock ticks in play: its moment rolled");
            w.played = w.riseAt - WorldBossClock.Warning - .05f;
            yield return Until(() => w.state == WorldBossState.Warned, 3);
            Assert.AreEqual(WorldBossState.Warned, w.state);
            yield return null; yield return null;
            Assert.IsTrue(s.Chat.Any(c => c.text == Unraveller.WarningSystem), "the System line in the Lowtowns");
            Assert.IsTrue(s.Chat.Any(c => c.channel == ChatChannel.Zone && c.speaker == Unraveller.Tibb && c.text == Unraveller.TibbCall), "Tibb Marrow calls");
            Assert.IsTrue(s.Chat.Any(c => c.channel == ChatChannel.Zone && c.speaker == Unraveller.Ketta && c.text == Unraveller.KettaCall), "Ketta Rune calls");
            Assert.IsTrue(zone.ReachStill, "the reach's water stilled"); Assert.IsTrue(zone.DrainLampsOut, "the lamps out");
            Assert.Greater(zone.DrainLampsGuttered, 0, "lamps of the Drain Courts and the reach guttered");
            Assert.Less(zone.NightLights.Count(l => l != null && l.light != null && l.light.enabled), Mathf.Max(1, lit), "fewer lamps lit at night");
            Assert.IsTrue(zone.ReachStockOut, "the salt barrel and the harpoon rack on the apron");
            var tibb = VillageLife.Active.Find(Unraveller.Tibb); var ketta = VillageLife.Active.Find(Unraveller.Ketta);
            Assert.IsNotNull(tibb); Assert.IsNotNull(ketta);
            Assert.Less(Flat(tibb.transform.position, zone.ReachBarrelAt), 3.5f, "Tibb by her barrel");
            Assert.Less(Flat(ketta.transform.position, zone.ReachRackAt), 3.5f, "Ketta by her rack");
            Assert.IsNull(s.UnravellerFoe, "not risen yet");

            // She rises.
            w.played = w.riseAt - .05f;
            yield return Until(() => s.UnravellerFoe != null, 3);
            var e = s.UnravellerFoe; Assert.IsNotNull(e, "she rises"); Assert.AreEqual(WorldBossState.Up, w.state);
            Assert.IsTrue(s.UnravellerRising, "out of the river");
            Assert.IsTrue(s.Chat.Any(c => c.text == Unraveller.RisenSystem), "the System line");
            Assert.IsTrue(s.Chat.Any(c => c.speaker == Unraveller.Name && c.text == Unraveller.RisingLine), "her line");
            Assert.Less(e.transform.Find("Body").localPosition.y, -.5f, "under the water as she starts");
            yield return Until(() => !s.UnravellerRising, Unraveller.RiseSeconds + 3);
            Assert.IsFalse(s.UnravellerRising); Assert.AreEqual(0, e.transform.Find("Body").localPosition.y, .01f, "risen");
            Assert.IsTrue(e.enabled && e.actor.IsAlive);
            Assert.Less(Flat(e.transform.position, barTop), 4, "at the silt bar");
            Assert.IsFalse(zone.WaterAt(new Vector2(e.transform.position.x, e.transform.position.z), out _, out _), "on the bar, not in the river");
            Assert.AreEqual(Unraveller.Level, e.actor.Level, "level 30");
            Assert.IsTrue(e.RaidBoss); Assert.AreEqual(ActorClassification.Boss, e.actor.Classification, "a raid boss's rank");
            Assert.AreEqual("Raid (10)", e.RankNote, "\"Raid (10)\" on her frame");
            Assert.IsFalse(e.ScalesWithGroup, "no group scaling"); Assert.IsNull(e.GroupNote);
            Assert.AreEqual(Unraveller.HealthPool, e.actor.Health.Pool.Max, "her pool");
            Assert.AreEqual(Unraveller.Scale, e.transform.localScale.y, .01f, "three times a man's height");
            Assert.IsTrue(e.BookWeaveEater, "a Weave-Eater: the book's rules hold (salt, harpoons, steel goes black)");
            Assert.IsTrue(s.Enemies.Contains(e));

            // 45 minutes up: she sinks, and the reach is as it was.
            w.upFor = WorldBossClock.Stay - .05f;
            yield return Until(() => s.UnravellerSinking || s.UnravellerFoe == null, 3);
            Assert.AreEqual(WorldBossState.Waiting, w.state, "the next 3 hours begun"); Assert.AreEqual(1, w.cycles);
            Assert.IsTrue(s.Chat.Any(c => c.text == Unraveller.SunkSystem), "she sinks back into the river, fed");
            yield return Until(() => s.UnravellerFoe == null, Unraveller.SinkSeconds + 3);
            Assert.IsNull(s.UnravellerFoe, "gone"); Assert.IsFalse(s.Enemies.Any(x => x != null && x.persistentId == Unraveller.Id));
            Assert.IsFalse(zone.ReachStill); Assert.IsFalse(zone.DrainLampsOut); Assert.IsFalse(zone.ReachStockOut);
            Assert.Greater(Flat(tibb.transform.position, zone.ReachBarrelAt), 10, "Tibb back at her stall");
        }
    }
}
#endif
