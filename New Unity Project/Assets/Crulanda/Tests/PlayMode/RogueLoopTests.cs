#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The Rogue in play (Docs/CC_DESIGN.md section 0): Sap before the pull, broken by the strike; combo points built and spent;
    /// Kick silences, Blind sends it off; Vanish drops its interest in you.</summary>
    public class RogueLoopTests
    {
        string root; EncounterSession session;
        RogueKit Rogue { get { return session.Kit as RogueKit; } }
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-rogue-" + Guid.NewGuid().ToString("N"));
            EncounterSession.StartClassOverride = "class.rogue";
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("PlayableEncounter", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(Rogue);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            EncounterSession.StartClassOverride = null; SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("PlayableEncounter");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        void Level10() { session.Progress.experience = EncounterProgress.XpForLevel(10); session.Player.SetLevel(10); }
        EncounterEnemy Foe() { var e = session.Enemies.Find(x => x != null && x.actor.IsAlive && !x.Elite && !x.Game && !x.Engaged); Assert.NotNull(e, "A living mob."); return e; }
        void Near(EncounterEnemy e, float d) { session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * d); session.Select(e); }
        const int Sinister = 0, Eviscerate = 1, Sap = 2, Gouge = 3, Kick = 4, Blind = 5, Vanish = 6;

        [UnityTest] public IEnumerator The_Rogue_is_its_own_character()
        {
            Assert.AreEqual("class.rogue", session.ClassDef.id); Assert.AreEqual(Crulanda.Core.ResourceKind.Focus, session.ClassDef.resource);
            Assert.AreEqual(7, session.Kit.ActionCount); Assert.AreEqual("Level 3", session.Kit.ActionLockLabel(Gouge));
            Assert.IsTrue(session.Kit.MeleeAutoAttacks); Assert.AreEqual(ActorLook.Ranger, EncounterSession.LookForClass("class.rogue"));
            yield return null;
        }
        [UnityTest] public IEnumerator Sap_holds_before_the_pull_and_the_strike_breaks_it_for_a_combo()
        {
            Level10(); var e = Foe(); Near(e, 2.5f);
            Assert.IsTrue(session.UseAbility(Sap), "Sap"); yield return null;
            Assert.IsTrue(e.Incapacitated, "held"); StringAssert.StartsWith("Held", e.ControlLabel);
            yield return new WaitForSeconds(1.1f);
            Assert.IsTrue(e.Incapacitated, "still held: nothing struck it");
            Assert.IsTrue(session.UseAbility(Sinister), "Sinister Strike"); yield return null;
            Assert.IsFalse(e.Incapacitated, "the strike breaks it"); Assert.AreEqual(1, Rogue.Combo);
            yield return new WaitForSeconds(1.1f);
            if (!e.actor.IsAlive) yield break;
            int before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(Eviscerate), "Eviscerate"); yield return null;
            Assert.AreEqual(0, Rogue.Combo, "spent"); Assert.Less(e.actor.Health.Pool.Current, before);
        }
        [UnityTest] public IEnumerator Kick_silences_and_Blind_sends_it_off()
        {
            Level10(); var e = Foe(); Near(e, 2.5f);
            Assert.IsTrue(session.UseAbility(Kick), "Kick"); yield return null;
            Assert.IsTrue(e.Silenced);
            yield return new WaitForSeconds(1.1f);
            Assert.IsTrue(session.UseAbility(Blind), "Blind"); yield return null;
            Assert.IsTrue(e.Feared); StringAssert.StartsWith("Fleeing", e.ControlLabel);
        }
        [UnityTest] public IEnumerator Vanish_drops_its_interest_in_you()
        {
            Level10(); var e = Foe(); Near(e, 2.5f);
            Assert.IsTrue(session.UseAbility(Sinister)); yield return null; yield return null;
            Assert.AreEqual(session.Player, e.Victim, "it comes for you");
            yield return new WaitForSeconds(1.1f);
            Assert.IsTrue(session.UseAbility(Vanish), "Vanish"); yield return null; yield return null;
            Assert.AreNotEqual(session.Player, e.Victim, "it has lost you");
        }
    }
}
#endif
