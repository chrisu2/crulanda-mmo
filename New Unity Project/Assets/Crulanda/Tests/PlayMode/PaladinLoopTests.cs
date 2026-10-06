#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.Combat;

namespace Crulanda.Tests
{
    /// <summary>The Paladin in play (Phase 5.1): its own character, Conviction built and spent, its heals, its Ward and its consecrated ground.</summary>
    public class PaladinLoopTests
    {
        string root;
        EncounterSession session;
        PaladinKit Paladin { get { return session.Paladin; } }
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-paladin-" + Guid.NewGuid().ToString("N"));
            EncounterSession.StartClassOverride = "class.paladin";
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("PlayableEncounter", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(session.Player); Assert.NotNull(Paladin);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            EncounterSession.StartClassOverride = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("PlayableEncounter");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        void Level10() { session.Progress.experience = EncounterProgress.XpForLevel(10); session.Player.SetLevel(10); }
        EncounterEnemy Foe() { var e = session.Enemies.Find(x => x != null && x.actor.IsAlive); Assert.NotNull(e, "A living enemy."); return e; }
        void Near(EncounterEnemy e, float distance) { session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * distance); session.Select(e); }
        static Combatant Combat(Crulanda.Gameplay.Actor a) { return a.GetComponent<Combatant>(); }
        const int Smite = 0, Oath = 1, Mend = 2, Ward = 3, Judgement = 4, Consecrate = 5, LayOn = 6, Aegis = 7, Censure = 8;

        [UnityTest] public IEnumerator Paladin_is_a_separate_character_with_its_own_save()
        {
            Assert.AreEqual("class.paladin", session.ClassDef.id); Assert.AreEqual("class.paladin", session.Progress.classId);
            Assert.AreEqual(Crulanda.Core.ResourceKind.Mana, session.ClassDef.resource);
            Assert.AreEqual(9, session.Kit.ActionCount);
            Assert.AreEqual("paladin.smite", session.Kit.ActionAt(Smite).id); Assert.AreEqual("paladin.censure", session.Kit.ActionAt(Censure).id);
            Assert.AreEqual("Talent", session.Kit.ActionLockLabel(Censure), "Censure waits on its talent.");
            Assert.AreEqual("Level 5", session.Kit.ActionLockLabel(LayOn), "Lay On comes at level 5.");
            Assert.AreEqual(ActorLook.Paladin, EncounterSession.LookForClass("class.paladin"));
            Assert.AreEqual(3, session.Talents.Branches.Count);
            yield return null;
        }

        [UnityTest] public IEnumerator Smite_builds_Conviction_and_Judgement_throws_it()
        {
            Level10(); var e = Foe(); Near(e, 2);
            int before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(Smite), "Smite"); Assert.AreEqual(1, Paladin.Conviction);
            Assert.Less(e.actor.Health.Pool.Current, before, "Smite lands.");
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(Smite)); Assert.AreEqual(2, Paladin.Conviction);
            yield return new WaitForSeconds(1.6f);
            int mid = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(Judgement), "Judgement at two pips");
            Assert.AreEqual(0, Paladin.Conviction, "Judgement spends every pip."); Assert.Less(e.actor.Health.Pool.Current, mid, "Judgement lands.");
            // (The kit's own refusal: the session would refuse first on Judgement's 6 s cooldown, without a word.)
            Assert.IsFalse(session.Kit.Use(Judgement), "No Conviction, no Judgement."); StringAssert.Contains("Judgement needs Conviction", session.Messages[session.Messages.Count - 1]);
        }

        [UnityTest] public IEnumerator Mend_heals_and_Lay_On_needs_three_pips()
        {
            Level10(); var player = session.Player; var combat = Combat(player);
            combat.Damage(150); int hurt = player.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(Mend), "Mend begins its cast");
            yield return new WaitForSeconds(1.9f);
            Assert.Greater(player.Health.Pool.Current, hurt, "Mend healed you.");
            Assert.IsFalse(session.UseAbility(LayOn), "Lay On without Conviction refuses."); StringAssert.Contains("Lay On needs 3 Conviction", session.Messages[session.Messages.Count - 1]);
            combat.Damage(200); hurt = player.Health.Pool.Current;
            Paladin.GainConviction(3);
            Assert.IsTrue(session.UseAbility(LayOn), "Lay On at three pips");
            Assert.AreEqual(0, Paladin.Conviction);
            Assert.GreaterOrEqual(player.Health.Pool.Current - hurt, Mathf.RoundToInt(player.Health.Pool.Max * .25f) - 1, "A quarter of your health, at once.");
            yield return null;
        }

        [UnityTest] public IEnumerator Ward_softens_blows_and_they_build_Conviction()
        {
            Level10(); var e = Foe(); var player = session.Player;
            Assert.IsTrue(session.UseAbility(Ward), "Ward");
            Assert.Greater(Paladin.WardRemaining, 4f);
            int dealt = session.Kit.ResolveEnemyHit(e, player, 100);
            Assert.LessOrEqual(dealt, 61, "Six tenths of the blow lands under the Ward."); Assert.Greater(dealt, 0);
            Assert.AreEqual(1, Paladin.Conviction, "A blow taken under the Ward is a pip.");
            session.Kit.ResolveEnemyHit(e, player, 100);
            Assert.AreEqual(1, Paladin.Conviction, "At most a pip every 1.5 s.");
            yield return null;
        }

        [UnityTest] public IEnumerator Consecrate_burns_the_ground_you_stand_on()
        {
            Level10(); var e = Foe(); Near(e, 2);
            int before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(Consecrate), "Consecrate"); Assert.IsTrue(Paladin.Consecrating);
            yield return new WaitForSeconds(2.5f);
            Assert.Less(e.actor.Health.Pool.Current, before, "The enemy on the ground burns.");
            StringAssert.Contains("consecrated ground", session.Kit.TargetStatus(e));
        }
    }
}
#endif
