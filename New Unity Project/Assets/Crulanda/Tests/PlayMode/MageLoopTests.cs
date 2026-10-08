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
    /// <summary>The Mage in play (Phase 5.1c): its own character, Heat built by a cast and released by Flare, the Overload, the field, Smoulder, Bind and Unbind, the Ward.</summary>
    public class MageLoopTests
    {
        string root;
        EncounterSession session;
        MageKit Mage { get { return session.Mage; } }
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-mage-" + Guid.NewGuid().ToString("N"));
            EncounterSession.StartClassOverride = "class.mage";
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("PlayableEncounter", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(session.Player); Assert.NotNull(Mage);
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
        const int EmberBolt = 0, Scorch = 1, Flare = 2, CinderField = 3, Smoulder = 4, EmberWard = 5, Bind = 6, Unbind = 7, Quench = 8;

        [UnityTest] public IEnumerator Mage_is_a_separate_character_with_a_staff()
        {
            Assert.AreEqual("class.mage", session.ClassDef.id); Assert.AreEqual("class.mage", session.Progress.classId);
            Assert.AreEqual(Crulanda.Core.ResourceKind.Mana, session.ClassDef.resource);
            Assert.AreEqual(10, session.Kit.ActionCount); Assert.AreEqual("mage.quench", session.Kit.ActionAt(Quench).id);
            Assert.AreEqual("Talent", session.Kit.ActionLockLabel(Quench)); Assert.AreEqual("Level 2", session.Kit.ActionLockLabel(Flare));
            Level10();
            Assert.AreEqual("Heat 30", session.Kit.ActionLockLabel(Flare), "Flare waits on Heat."); Assert.AreEqual("No Charge", session.Kit.ActionLockLabel(Unbind));
            Assert.IsFalse(session.Kit.MeleeAutoAttacks); Assert.IsFalse(session.Kit.RangedAutoAttacks);
            Assert.AreEqual(ActorLook.Mage, EncounterSession.LookForClass("class.mage"));
            yield return null;
        }

        [UnityTest] public IEnumerator Ash_Hex_holds_one_at_a_time()
        {
            Level10(); var a = session.Enemies.Find(x => x != null && x.actor.IsAlive && !x.Elite && !x.Game); var b = session.Enemies.Find(x => x != null && x != a && x.actor.IsAlive && !x.Elite && !x.Game); Assert.NotNull(b);
            Near(a, 12); Assert.IsTrue(session.UseAbility(9), "Ash Hex"); yield return new WaitForSeconds(1.8f);
            Assert.IsTrue(a.Incapacitated, "hexed");
            Near(b, 12); Assert.IsTrue(session.UseAbility(9), "again"); yield return new WaitForSeconds(1.8f);
            Assert.IsTrue(b.Incapacitated); Assert.IsFalse(a.Incapacitated, "one at a time");
        }
        [UnityTest] public IEnumerator Ember_Bolt_is_cast_builds_Heat_and_Flare_releases_it()
        {
            Level10(); var e = Foe(); Near(e, 12);
            int before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(EmberBolt), "Ember Bolt begins its cast");
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(before, e.actor.Health.Pool.Current, "Nothing lands while it is cast."); Assert.AreEqual(0, Mage.Heat);
            bool ember = false;
            for (float w = 0; w < 1.6f; w += Time.deltaTime) { if (UnityEngine.Object.FindFirstObjectByType<Bolt>() != null) ember = true; yield return null; }
            Assert.Less(e.actor.Health.Pool.Current, before, "It lands."); Assert.AreEqual(MageKit.EmberHeat, Mage.Heat, "15 Heat built.");
            Assert.IsTrue(ember, "An ember is seen flying (Round 22).");
            Assert.IsFalse(session.Kit.Use(Flare), "Flare refuses under 30 Heat.");
            Mage.GainHeat(35); Assert.AreEqual(50, Mage.Heat);
            int mid = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(Flare), "Flare at 50 Heat");
            Assert.AreEqual(0, Mage.Heat, "Flare empties the Heat.");
            Assert.LessOrEqual(e.actor.Health.Pool.Current, mid - 40, "The burst carries the Heat: 10 + 10 + 30.");
        }

        [UnityTest] public IEnumerator Reaching_100_Heat_Overloads()
        {
            Level10(); yield return null;
            int hp = session.Player.Health.Pool.Current;
            Mage.GainHeat(60); Mage.GainHeat(60);
            Assert.AreEqual(0, Mage.Heat, "The gauge empties."); Assert.IsTrue(Mage.Overloaded);
            Assert.Less(session.Player.Health.Pool.Current, hp, "The Overload burns you.");
            Mage.GainHeat(20); Assert.AreEqual(0, Mage.Heat, "Nothing builds while Overloaded.");
            StringAssert.Contains("OVERLOADED", session.Kit.StatusLine);
            yield return new WaitForSeconds(MageKit.OverloadSeconds + .2f);
            Assert.IsFalse(Mage.Overloaded); Mage.GainHeat(20); Assert.AreEqual(20, Mage.Heat);
        }

        [UnityTest] public IEnumerator Cinder_Field_burns_the_ground_and_Smoulder_slows()
        {
            Level10(); var e = Foe(); Near(e, 10);
            int before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(CinderField)); Assert.IsTrue(Mage.FieldBurning);
            yield return null;   // the first tick comes with the kit's next Tick
            Assert.Less(e.actor.Health.Pool.Current, before, "The first tick is on the first frame.");
            StringAssert.Contains("In the cinders", session.Kit.TargetStatus(e));
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(Smoulder)); Assert.IsTrue(e.Slowed, "Smouldering.");
            StringAssert.Contains("SMOULDERING", session.Kit.TargetStatus(e));
        }

        [UnityTest] public IEnumerator Bind_banks_a_Charge_and_Unbind_raises_party_damage()
        {
            Level10(); var e = Foe(); Near(e, 10);
            Assert.IsTrue(session.UseAbility(Bind)); Assert.IsTrue(Mage.Binding);
            Assert.IsTrue(session.UseAbility(EmberBolt), "A bound Ember Bolt");
            yield return new WaitForSeconds(1.7f);
            Assert.AreEqual(1, Mage.Charges, "A Charge is banked."); Assert.IsFalse(Mage.Binding);
            Assert.AreEqual(1f, session.Kit.PartyDamageMultiplier(e), 1e-4f);
            Assert.IsTrue(session.UseAbility(Unbind)); Assert.AreEqual(0, Mage.Charges); Assert.IsTrue(Mage.Unbound);
            Assert.AreEqual(1 + MageKit.UnbindBonus, session.Kit.PartyDamageMultiplier(e), 1e-4f, "The party hits harder.");
            Assert.IsTrue(session.UseAbility(EmberWard) || true);   // on the global cooldown: tried again below
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(EmberWard), "Ember Ward");
            Assert.Greater(session.Player.GetComponent<Combatant>().Barrier, 0, "A barrier.");
        }
    }
}
#endif
