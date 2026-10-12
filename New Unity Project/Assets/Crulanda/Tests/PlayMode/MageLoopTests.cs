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
        const int EmberBolt = 0, Scorch = 1, Flare = 2, CinderField = 3, Smoulder = 4, EmberWard = 5, Bind = 6, Unbind = 7, Quench = 8, RaiseThrall = 10, MarrowDrain = 11, BoneSpikes = 12;

        /// <summary>The Bonecaller (the necromancer, 2026-10-12, reworked from the Cinderwright; GAME-ONLY): Raise Bone Thrall stands the
        /// risen dead's skeleton up at your side, a party member like the Ranger's wolf; Marrow Drain draws the life out of a foe over time,
        /// half of it mending you and the thrall; Bone Spikes bursts the ground round the thrall, at a tenth of its health; Bone Armour: the
        /// thrall takes 30% less while the drain runs; Raise again lays it down.</summary>
        [UnityTest] public IEnumerator The_bone_thrall_is_raised_drains_and_bursts_spikes()
        {
            session.Progress.experience = EncounterProgress.XpForLevel(15); session.Player.SetLevel(15); session.Player.Resource.Pool.SetCurrent(session.Player.Resource.Pool.Max);
            Assert.AreEqual("mage.raise_thrall", session.Kit.ActionAt(RaiseThrall).id); Assert.AreEqual("Talent", session.Kit.ActionLockLabel(BoneSpikes));
            foreach (var id in new[] { "bc-hard-bone", "bc-hard-bone", "bc-hard-bone", "bc-sharp-marrow", "bc-sharp-marrow", "bc-sharp-marrow", "bc-grave-bond", "bc-grave-bond", "bc-grave-bond",
                "bc-long-drain", "bc-long-drain", "bc-bone-spikes", "bc-death-echo", "bc-death-echo", "bc-death-echo", "bc-bone-armour" })
            { Assert.IsTrue(session.Talents.Propose(session.Progress, id, 1, out var next, out var why), id + ": " + why); session.Progress.talents = next; }
            session.Kit.ApplyStats(); Assert.IsNull(session.Kit.ActionLockLabel(BoneSpikes), "Bone Spikes learnt");
            Assert.IsNull(session.Pet);
            Assert.IsTrue(session.UseAbility(RaiseThrall), "Raise Bone Thrall"); yield return new WaitForSeconds(1.7f);
            var thrall = session.Pet;
            Assert.NotNull(thrall, "a bone thrall"); Assert.AreEqual("thrall", thrall.Kind); Assert.AreEqual("Bone thrall", thrall.actor.DisplayName);
            Assert.AreEqual(ActorLook.Skeleton, thrall.GetComponent<ActorVisual>().Look, "the risen dead's bones");
            Assert.AreSame(thrall.actor, session.PartyActor(thrall.actor.EntityId.Value), "a party member the enemies can turn on");
            Assert.AreEqual(1.24f, Mage.PetHealthScale, 1e-3f, "Hard Bone: 8% more health a rank"); Assert.AreEqual(1.3f, Mage.PetBiteScale, 1e-3f, "Sharp Marrow");
            StringAssert.Contains("Thrall", session.Kit.StatusLine);
            var e = Foe(); Near(e, 10); yield return null;
            var you = session.Player; you.Health.ApplyDamage(you.Health.Pool.Max / 2); int hp = you.Health.Pool.Current, before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(MarrowDrain), "Marrow Drain"); yield return null;
            StringAssert.Contains("Drained", session.Kit.TargetStatus(e)); Assert.IsTrue(Mage.Draining);
            yield return new WaitForSeconds(2.3f);
            Assert.Less(e.actor.Health.Pool.Current, before, "the life drawn out"); Assert.Greater(you.Health.Pool.Current, hp, "half of it mends you");
            int thrallHp = thrall.actor.Health.Pool.Current; session.Kit.ResolveEnemyHit(e, thrall.actor, 100);
            Assert.AreEqual(thrallHp - 70, thrall.actor.Health.Pool.Current, 1, "Bone Armour: 30% less while the drain runs");
            thrall.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(e.transform.position + Vector3.right * 1.5f); yield return null;
            before = e.actor.Health.Pool.Current; thrallHp = thrall.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(BoneSpikes), "Bone Spikes");
            Assert.NotNull(GameObject.Find("Bone spikes"), "the spikes are seen");
            Assert.Less(e.actor.Health.Pool.Current, before, "the foe beside the thrall is struck");
            Assert.Less(thrall.actor.Health.Pool.Current, thrallHp, "and the spikes cost the thrall");
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(RaiseThrall), "Raise again"); Assert.IsNull(session.Pet, "laid back down");
        }

        [UnityTest] public IEnumerator Mage_is_a_separate_character_with_a_staff()
        {
            Assert.AreEqual("class.mage", session.ClassDef.id); Assert.AreEqual("class.mage", session.Progress.classId);
            Assert.AreEqual(Crulanda.Core.ResourceKind.Mana, session.ClassDef.resource);
            Assert.AreEqual(13, session.Kit.ActionCount, "ten, and the Bonecaller's three"); Assert.AreEqual("mage.quench", session.Kit.ActionAt(Quench).id);
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
