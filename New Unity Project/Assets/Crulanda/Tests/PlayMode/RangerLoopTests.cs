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
    /// <summary>The Ranger in play (Phase 5.1b): its own character, the bow's auto-shot at range, the drawn shot, the mark, the snare, the wolf, the leap.</summary>
    public class RangerLoopTests
    {
        string root;
        EncounterSession session;
        RangerKit Ranger { get { return session.Ranger; } }
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-ranger-" + Guid.NewGuid().ToString("N"));
            EncounterSession.StartClassOverride = "class.ranger";
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("PlayableEncounter", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(session.Player); Assert.NotNull(Ranger);
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
        const int QuickShot = 0, AimedShot = 1, BarbedArrow = 2, HuntersMark = 3, Snare = 4, CallCompanion = 5, Sic = 6, Disengage = 7, Pin = 8;

        [UnityTest] public IEnumerator Ranger_is_a_separate_character_with_a_bow()
        {
            Assert.AreEqual("class.ranger", session.ClassDef.id); Assert.AreEqual("class.ranger", session.Progress.classId);
            Assert.AreEqual(Crulanda.Core.ResourceKind.Focus, session.ClassDef.resource);
            Assert.AreEqual(9, session.Kit.ActionCount); Assert.AreEqual("ranger.pin", session.Kit.ActionAt(Pin).id);
            Assert.AreEqual("Level 2", session.Kit.ActionLockLabel(Sic)); Level10();
            Assert.AreEqual("Talent", session.Kit.ActionLockLabel(Pin)); Assert.AreEqual("No wolf", session.Kit.ActionLockLabel(Sic), "Sic waits on the wolf.");
            Assert.IsFalse(session.Kit.MeleeAutoAttacks); Assert.IsTrue(session.Kit.RangedAutoAttacks); Assert.AreEqual(RangerKit.BowRange, session.Kit.AutoAttackRange);
            Assert.AreEqual(ActorLook.Ranger, EncounterSession.LookForClass("class.ranger"));
            yield return null;
        }

        [UnityTest] public IEnumerator Quick_Shot_hits_at_range_and_the_bow_keeps_shooting()
        {
            Level10(); var e = Foe(); Near(e, 15);
            int before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(QuickShot), "Quick Shot at 15 m");
            Assert.Less(e.actor.Health.Pool.Current, before, "The arrow lands."); Assert.IsTrue(session.AutoAttack, "The bow's auto-shot begins.");
            int after = e.actor.Health.Pool.Current;
            yield return new WaitForSeconds(3.2f);
            Assert.Less(e.actor.Health.Pool.Current, after, "A second arrow, from the auto-shot, without moving.");
        }

        [UnityTest] public IEnumerator Aimed_Shot_is_drawn_and_lands_hard()
        {
            Level10(); var e = Foe(); Near(e, 12);
            int before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(AimedShot), "Aimed Shot begins its draw");
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(before, e.actor.Health.Pool.Current, "Nothing lands while it is drawn.");
            yield return new WaitForSeconds(1.4f);
            Assert.LessOrEqual(e.actor.Health.Pool.Current, before - 22, "It lands for its power and more.");
        }

        [UnityTest] public IEnumerator Mark_raises_party_damage_and_Snare_slows()
        {
            Level10(); var e = Foe(); Near(e, 10);
            Assert.AreEqual(1f, session.Kit.PartyDamageMultiplier(e), 1e-4f);
            Assert.IsTrue(session.UseAbility(HuntersMark)); Assert.IsTrue(Ranger.Marked(e));
            Assert.AreEqual(1 + RangerKit.MarkBonus, session.Kit.PartyDamageMultiplier(e), 1e-4f, "A marked target takes a tenth more.");
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(Snare)); Assert.IsTrue(e.Slowed, "Snared.");
            StringAssert.Contains("MARKED", session.Kit.TargetStatus(e)); StringAssert.Contains("SNARED", session.Kit.TargetStatus(e));
        }

        [UnityTest] public IEnumerator The_wolf_comes_to_heel_and_hunts_what_it_is_sent_at()
        {
            Level10(); var e = Foe(); Near(e, 8);
            Assert.IsNull(session.Pet);
            Assert.IsTrue(session.UseAbility(CallCompanion), "Call Companion");
            Assert.NotNull(session.Pet, "A wolf."); Assert.IsTrue(session.Pet.actor.IsAlive);
            Assert.AreSame(session.Pet.actor, session.PartyActor(session.Pet.actor.EntityId.Value), "The wolf is a party member the enemies can turn on.");
            Assert.IsNull(session.Kit.ActionLockLabel(Sic));
            yield return new WaitForSeconds(1.6f);
            int before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(Sic), "Sic"); Assert.AreSame(e, session.Pet.Quarry);
            yield return new WaitForSeconds(4f);
            Assert.Less(e.actor.Health.Pool.Current, before, "The wolf bit it.");
            StringAssert.Contains("Wolf", session.Kit.StatusLine);
        }

        [UnityTest] public IEnumerator Disengage_leaps_away_from_the_target()
        {
            Level10(); var e = Foe(); Near(e, 3);
            float before = session.Distance(e);
            Assert.IsTrue(session.UseAbility(Disengage), "Disengage");
            Assert.Greater(session.Distance(e), before + 3.5f, "Six metres back, more or less.");
            yield return null;
        }
    }
}
#endif
