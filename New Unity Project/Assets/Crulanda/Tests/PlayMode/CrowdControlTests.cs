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
    /// <summary>Crowd control, the engine (Docs/CC_DESIGN.md step C1): a hold stops a mob and breaks on damage; a stun does not break;
    /// the same hold again within 18 s is half as long and a third time does not take; a boss takes no hold; a held mob answers no call.</summary>
    public class CrowdControlTests
    {
        string root; EncounterSession session;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-cc-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        EncounterEnemy Mob() { return session.Enemies.First(e => e != null && e.actor.IsAlive && e.Camp && !e.Elite && !e.Game && !e.Hidden); }

        [UnityTest] public IEnumerator A_hold_stops_a_mob_and_breaks_on_damage()
        {
            var e = Mob(); yield return null;
            Assert.IsNull(e.Apply("incap", session.Player, 20), "it takes");
            Assert.IsTrue(e.Incapacitated); Assert.IsTrue(e.Controlled); Assert.IsFalse(e.CanAnswer, "a held mob answers no call");
            StringAssert.StartsWith("Held", e.ControlLabel);
            yield return null; e.Receive(1, session.Player);
            Assert.IsFalse(e.Incapacitated, "damage breaks it");
            Assert.IsNull(e.Apply("stun", session.Player, 3)); e.Receive(1, session.Player);
            Assert.IsTrue(e.Stunned, "a stun does not break");
        }
        [UnityTest] public IEnumerator The_same_hold_again_is_shorter_then_does_not_take()
        {
            var e = Mob(); yield return null;
            Assert.IsNull(e.Apply("incap", session.Player, 20)); e.Receive(1, session.Player);
            Assert.IsNull(e.Apply("incap", session.Player, 20), "again: it takes");
            StringAssert.Contains("10 s", e.ControlLabel, "half as long");
            e.Receive(1, session.Player);
            Assert.AreEqual("Immune (too soon)", e.Apply("incap", session.Player, 20), "a third time: no");
        }
        [UnityTest] public IEnumerator A_boss_takes_no_hold()
        {
            yield return null;
            var king = session.Enemies.FirstOrDefault(x => x != null && x.Elite && x.actor.DisplayName.Contains("Caddock"));
            Assert.NotNull(king, "Caddock is in Crowsfoot Hollow");
            Assert.AreEqual("Immune", king.Apply("incap", session.Player, 20)); Assert.AreEqual("Immune", king.Apply("fear", session.Player, 8));
            Assert.IsNull(king.Apply("stun", session.Player, 3), "a stun works on a boss");
        }
    }
}
#endif
