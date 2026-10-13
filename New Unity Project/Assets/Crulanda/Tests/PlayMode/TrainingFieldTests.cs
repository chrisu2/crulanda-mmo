#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Combat;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Oakhaven's training field (Chris, 2026-10-13: "put a few test dummies in town ... one for dps and one for heals", "put out in a
    /// field near town", "these dummies should be targetable and hitable / healable of course", "just make the heal dummy party aligned").
    /// </summary>
    public class TrainingFieldTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-training-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.oakhaven";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch (IOException) { }
        }
        static EncounterSession Session() { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.NotNull(s, "a session"); return s; }
        static void Put(EncounterSession s, Vector3 at) { s.Player.GetComponent<AdventurerMotor>().Teleport(at + Vector3.up * .5f); }
        static IEnumerator Wait(float seconds) { for (float t = 0; t < seconds; t += Time.deltaTime) yield return null; }

        [UnityTest, Timeout(180000)] public IEnumerator The_training_dummy_is_struck_never_strikes_back_and_is_whole_again()
        {
            var s = Session(); var d = s.TrainingDummy;
            Assert.NotNull(d, "Oakhaven's field has a training dummy"); Assert.IsTrue(s.Enemies.Contains(d), "it is a foe you can target and tab to");
            Assert.AreEqual(s.Progress.Level, d.actor.Level, "at your level"); Assert.AreEqual(0, d.actor.Stats.Get(Crulanda.Core.StatType.Armor), 1e-3f, "no armour");
            Put(s, d.transform.position + d.transform.forward * 2.5f); yield return null;
            s.Select(d); Assert.AreSame(d, s.Target, "targetable");
            var at = d.transform.position; int mine = s.Player.Health.Pool.Current; int max = d.actor.Health.Pool.Max;
            d.Receive(500, s.Player); Assert.Less(d.actor.Health.Pool.Current, max, "hittable"); Assert.IsTrue(s.InCombat, "striking it is a fight, for the meter");
            yield return Wait(2.5f);
            Assert.Less(Vector3.Distance(at, d.transform.position), .1f, "it never moves");
            Assert.AreEqual(mine, s.Player.Health.Pool.Current, "it never strikes back");
            yield return Wait(EncounterSession.TrainingQuiet + 1.5f);
            Assert.AreEqual(max, d.actor.Health.Pool.Current, "whole again after a quiet spell");
            Assert.IsTrue(d.actor.IsAlive);
        }

        [UnityTest, Timeout(180000)] public IEnumerator The_mending_dummy_is_hurt_while_you_are_near_and_healed_as_one_of_your_party()
        {
            var s = Session(); var m = s.MendingDummy;
            Assert.NotNull(m, "Oakhaven's field has a mending dummy");
            Put(s, m.transform.position + m.transform.forward * 3); yield return null;
            Assert.IsTrue(s.Healables().Contains(m), "party aligned while you are near: every healer counts it");
            int max = m.Health.Pool.Max; yield return Wait(3.2f);
            Assert.Less(m.Health.Pool.Current, max, "it loses health while you are near");
            int before = m.Health.Pool.Current;
            int healed = m.GetComponent<Combatant>().Heal(100, s.Player);
            Assert.Greater(healed, 0); Assert.Greater(m.Health.Pool.Current, before, "healable");
            Assert.IsTrue(s.Meter.Rows.Any(r => r.healing > 0), "the meter counts the mend");
            yield return Wait(1.2f); Assert.IsTrue(s.InCombat, "mending it is a fight, for the meter");
            yield return Wait(30); Assert.GreaterOrEqual(m.Health.Pool.Ratio, EncounterSession.MendingFloor - .01f, "never below a fifth");
            Put(s, m.transform.position + Vector3.right * 60); yield return Wait(1.5f);
            Assert.IsFalse(s.Healables().Contains(m), "out of reach: not counted");
            Assert.AreEqual(max, m.Health.Pool.Current, "nobody practising: whole again");
        }

        [UnityTest] public IEnumerator Clicking_the_mending_dummy_selects_it_as_a_friend()
        {
            var s = Session(); Assert.NotNull(s.MendingDummy); yield return null;
            s.SelectMending();
            Assert.IsTrue(s.FocusMending && s.HasFriendlyFocus); Assert.AreEqual("Mending dummy", s.FocusName); Assert.IsNull(s.Target);
            s.Select(s.TrainingDummy); Assert.IsFalse(s.FocusMending, "selecting a foe drops the friend");
        }
    }
}
#endif
