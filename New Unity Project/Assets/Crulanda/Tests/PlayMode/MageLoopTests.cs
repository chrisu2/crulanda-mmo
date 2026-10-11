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
        const int EmberBolt = 0, Scorch = 1, Flare = 2, CinderField = 3, Smoulder = 4, EmberWard = 5, Bind = 6, Unbind = 7, Quench = 8, KindleHound = 10, Stoke = 11, CinderBurst = 12;

        /// <summary>The Cinderwright (TALENT_DEPTH.md, 2026-10-11): Kindle Hound calls an ember hound (the wolf's body dressed in embers, a
        /// party member like the Ranger's wolf) that joins the Mage's fight; Stoke pours the Heat into its bites (refused under 20 Heat);
        /// Cinder Burst flares it, burning every foe close by and costing the hound 15% of its health; Kindle again sends it back.</summary>
        [UnityTest] public IEnumerator The_ember_hound_is_kindled_stoked_and_bursts()
        {
            session.Progress.experience = EncounterProgress.XpForLevel(15); session.Player.SetLevel(15); session.Player.Resource.Pool.SetCurrent(session.Player.Resource.Pool.Max);
            Assert.AreEqual("mage.kindle_hound", session.Kit.ActionAt(KindleHound).id); Assert.AreEqual("Talent", session.Kit.ActionLockLabel(CinderBurst));
            foreach (var id in new[] { "cw-banked-coals", "cw-banked-coals", "cw-banked-coals", "cw-hot-jaws", "cw-hot-jaws", "cw-hot-jaws", "cw-kinship", "cw-kinship", "cw-kinship",
                "cw-long-stoke", "cw-long-stoke", "cw-cinder-burst", "cw-shared-heat", "cw-shared-heat", "cw-shared-heat", "cw-white-coals" })
            { Assert.IsTrue(session.Talents.Propose(session.Progress, id, 1, out var next, out var why), id + ": " + why); session.Progress.talents = next; }
            session.Kit.ApplyStats(); Assert.IsNull(session.Kit.ActionLockLabel(CinderBurst), "Cinder Burst learnt");
            Assert.IsNull(session.Pet);
            Assert.IsTrue(session.UseAbility(KindleHound), "Kindle Hound"); var hound = session.Pet;
            Assert.NotNull(hound, "an ember hound"); Assert.AreEqual("hound", hound.Kind); Assert.AreEqual("Ember hound", hound.actor.DisplayName);
            Assert.AreSame(hound.actor, session.PartyActor(hound.actor.EntityId.Value), "a party member the enemies can turn on");
            Assert.AreEqual(1.24f, Mage.PetHealthScale, 1e-3f, "Banked Coals: 8% more health a rank");
            StringAssert.Contains("Hound", session.Kit.StatusLine);
            // Stoke: refused without Heat; with 50 it is spent on the hound's bites.
            yield return new WaitForSeconds(1.6f);
            Assert.IsFalse(session.UseAbility(Stoke), "no Heat, no Stoke");
            Mage.GainHeat(50); Assert.AreEqual(50, Mage.Heat);
            Assert.IsTrue(session.UseAbility(Stoke), "Stoke"); Assert.AreEqual(0, Mage.Heat, "all the Heat spent"); Assert.IsTrue(Mage.Stoked);
            Assert.AreEqual(1.3f * 1.5f, Mage.PetBiteScale, 1e-3f, "Hot Jaws 30%, and 50 Heat stoked: half again");
            // It joins the fight; Cinder Burst burns what is near it.
            var e = Foe(); Near(e, 10);
            hound.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(e.transform.position + Vector3.right * 1.5f); yield return null;
            int before = e.actor.Health.Pool.Current, houndBefore = hound.actor.Health.Pool.Current;
            yield return new WaitForSeconds(1.6f);
            before = e.actor.Health.Pool.Current; houndBefore = hound.actor.Health.Pool.Current;
            hound.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(e.transform.position + Vector3.right * 1.5f);
            Assert.IsTrue(session.UseAbility(CinderBurst), "Cinder Burst");
            Assert.Less(e.actor.Health.Pool.Current, before, "the foe beside the hound burns");
            Assert.Less(hound.actor.Health.Pool.Current, houndBefore, "and the burst costs the hound");
            // Kindle again: it sinks back into the embers.
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(KindleHound), "Kindle again"); Assert.IsNull(session.Pet, "gone back into the embers");
        }

        [UnityTest] public IEnumerator Mage_is_a_separate_character_with_a_staff()
        {
            Assert.AreEqual("class.mage", session.ClassDef.id); Assert.AreEqual("class.mage", session.Progress.classId);
            Assert.AreEqual(Crulanda.Core.ResourceKind.Mana, session.ClassDef.resource);
            Assert.AreEqual(13, session.Kit.ActionCount, "ten, and the Cinderwright's three"); Assert.AreEqual("mage.quench", session.Kit.ActionAt(Quench).id);
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
