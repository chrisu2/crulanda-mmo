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

namespace Crulanda.Tests
{
    /// <summary>The Shaman swings its staff (2026-10-17, Chris: "shaman still had no auto attack"): the interact key with a foe targeted
    /// starts the swing, and a swing lands at the Shaman's own swing scale, not its build's spell scale (ShamanKit.SwingDamageScale).</summary>
    public class ShamanSwingTests
    {
        string root; EncounterSession s;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-shamanswing-" + Guid.NewGuid().ToString("N"));
            EncounterSession.StartClassOverride = "class.shaman";
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.NotNull(s);
            if (s.Companion != null) s.Companion.gameObject.SetActive(false);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            EncounterSession.StartClassOverride = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        [UnityTest] public IEnumerator The_shaman_swings_its_staff_at_its_own_scale()
        {
            Assert.AreEqual("class.shaman", s.ClassDef.id); Assert.IsTrue(s.Kit.MeleeAutoAttacks);
            var e = s.Enemies.Where(x => x != null && x.actor.IsAlive && !x.Game && !x.Hidden && x.Inside == null).OrderBy(x => x.transform.position.sqrMagnitude).First();
            // A dummy: it stands, never strikes, has no armour (a share of every blow would go) and health enough.
            e.enabled = false; var ag = e.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (ag != null && ag.isOnNavMesh) ag.isStopped = true;
            e.actor.Stats.SetBase(Crulanda.Core.StatType.Armor, 0); e.actor.Health.Pool.SetMax(1000000, false); e.actor.Health.Pool.SetCurrent(1000000);
            s.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * 1.8f + Vector3.up * .5f); yield return null;
            s.Select(e); Assert.IsFalse(s.AutoAttack);
            StringAssert.Contains(KeyBindings.InteractLabel + " begins it", s.AutoAttackStatus, "the bar says how to begin it");
            s.Interact();
            Assert.IsTrue(s.AutoAttack, "the interact key with a foe targeted starts the swing (" + s.Messages.LastOrDefault() + ")");
            int before = e.actor.Health.Pool.Current; float t = 0;
            while (t < 4 && e.actor.Health.Pool.Current == before) { t += Time.deltaTime; yield return null; }
            int dealt = before - e.actor.Health.Pool.Current;
            Assert.Greater(dealt, 0, "a swing landed");
            float own = s.Kit.SwingDamageScale(s.PlayerDamageScale);
            Assert.AreEqual(s.LeadBuild == null ? ShamanKit.StoneSwing : ShamanKit.CasterSwing, own, 1e-4f, "the Shaman's own swing scale");
            Assert.AreEqual(Mathf.Max(1, Mathf.RoundToInt(s.WeaponDamage * own)), dealt, 1, "a swing is its weapon at its own scale");
        }
    }
}
#endif
