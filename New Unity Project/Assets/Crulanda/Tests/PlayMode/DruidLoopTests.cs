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
    public class DruidLoopTests
    {
        string root;
        EncounterSession session;
        DruidKit Druid { get { return session.Druid; } }
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-druid-" + Guid.NewGuid().ToString("N"));
            EncounterSession.StartClassOverride = "class.druid";
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("PlayableEncounter", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(session.Player); Assert.NotNull(Druid);
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
        void Buy(params string[] ids) { Level10(); foreach (var id in ids) Assert.IsTrue(session.ChangeTalent(id, 1), id); }
        void Near(EncounterEnemy e, float distance) { session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * distance); session.Select(e); }
        IEnumerator Shift(DruidForm form) { Assert.IsTrue(session.UseAbility((int)form), "shift " + form); Assert.AreEqual(form, Druid.Form); yield return new WaitForSeconds(1.6f); }
        Combatant Combat(Crulanda.Gameplay.Actor a) { return a.GetComponent<Combatant>(); }

        [UnityTest] public IEnumerator Druid_is_a_separate_character_with_its_own_save()
        {
            Assert.AreEqual("class.druid", session.ClassDef.id); Assert.AreEqual("class.druid", session.Progress.classId);
            Assert.AreEqual(DruidForm.Thornsong, Druid.Form); Assert.AreEqual(10, session.ActionCount);
            Assert.AreEqual("Active", session.ActionLockLabel(3)); Assert.IsNull(session.ActionLockLabel(0));
            Assert.AreEqual("Level 6", session.ActionLockLabel(9), "Stillroot unlocks at level 6.");
            session.Save();
            Assert.Greater(Directory.GetFiles(root, "encounter-druid*").Length, 0);
            Assert.AreEqual(0, Directory.GetFiles(root, "encounter.*").Length, "The Warrior slot is untouched.");
            yield return null;
        }
        [UnityTest] public IEnumerator Shifting_costs_breath_and_never_refills_pools()
        {
            int breath = session.Player.Resource.Pool.Current;
            Assert.IsTrue(session.UseAbility((int)DruidForm.Barkhide));
            Assert.AreEqual(breath - DruidKit.ShiftCost, session.Player.Resource.Pool.Current);
            Assert.IsTrue(Druid.MeleeAutoAttacks); Assert.Greater(session.Player.Stats.Get(Crulanda.Core.StatType.Armor), 29);
            yield return new WaitForSeconds(1.6f);
            Druid.GainBark(50);
            yield return Shift(DruidForm.Thornclaw);
            Assert.GreaterOrEqual(Druid.Bark, 35, "Bark is kept (only slow out-of-combat decay), never refilled.");
            Assert.AreEqual(0, Druid.Fang);
            Assert.Less(session.Player.Stats.Get(Crulanda.Core.StatType.Armor), 29, "Barkhide armor leaves with the form.");
        }
        [UnityTest] public IEnumerator Barkhide_builds_bark_from_strikes_and_blows_and_barkmend_spends_it()
        {
            Level10(); var enemy = session.Enemies[0];
            yield return Shift(DruidForm.Barkhide); Near(enemy, 2.5f);
            Assert.IsTrue(session.UseAbility(4)); Assert.AreEqual(12, Druid.Bark);
            Druid.ResolveEnemyHit(enemy, session.Player, 13); Assert.AreEqual(17, Druid.Bark);
            Druid.GainBark(20); session.Player.Health.ApplyDamage(80); int hurt = session.Player.Health.Pool.Current;
            yield return new WaitForSeconds(1.6f);
            int bark = Druid.Bark; hurt = session.Player.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(6)); Assert.AreEqual(bark - 30, Druid.Bark); Assert.Greater(session.Player.Health.Pool.Current, hurt);
        }
        [UnityTest] public IEnumerator Thornclaw_rake_bleeds_and_tear_spends_fang()
        {
            Level10(); var enemy = session.Enemies[0];
            yield return Shift(DruidForm.Thornclaw); Near(enemy, 2.5f);
            Assert.IsTrue(session.UseAbility(4)); Assert.AreEqual(1, Druid.Fang);
            Assert.IsTrue(Druid.Periodic.Has("bleed", enemy)); int afterRake = enemy.actor.Health.Pool.Current;
            yield return new WaitForSeconds(2.2f);
            Assert.Less(enemy.actor.Health.Pool.Current, afterRake, "Bleed ticked.");
            Assert.IsTrue(session.UseAbility(6)); Assert.AreEqual(0, Druid.Fang);
        }
        [UnityTest] public IEnumerator Rootmend_seedling_heals_over_time_and_builds_sap()
        {
            yield return Shift(DruidForm.Rootmend);
            session.Player.Health.ApplyDamage(90); int hurt = session.Player.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(4)); Assert.IsTrue(Druid.Periodic.Has("seedling", session.Player));
            yield return new WaitForSeconds(2.2f);
            Assert.Greater(session.Player.Health.Pool.Current, hurt); Assert.GreaterOrEqual(Druid.Sap, 4);
        }
        [UnityTest] public IEnumerator Thornsong_alternation_builds_glimmer_and_stillroot_roots()
        {
            Level10(); var enemy = session.Enemies[0]; Near(enemy, 9);
            Assert.IsTrue(session.UseAbility(4)); Assert.AreEqual(0, Druid.Glimmer, "First cast has nothing to alternate from.");
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(5)); Assert.IsTrue(session.PlayerCasting);
            yield return new WaitForSeconds(2.0f);
            Assert.AreEqual(10, Druid.Glimmer);
            Assert.IsTrue(session.UseAbility(9)); Assert.IsTrue(enemy.Rooted);
            yield return null; yield return null;
            Assert.AreEqual(0, enemy.GetComponent<UnityEngine.AI.NavMeshAgent>().speed);
        }
        [UnityTest] public IEnumerator Heartwood_brace_is_talent_gated_and_reduces_damage()
        {
            yield return Shift(DruidForm.Barkhide);
            Assert.AreEqual("Talent", session.ActionLockLabel(7));
            Buy("bh-ringed-hide", "bh-ringed-hide", "bh-ringed-hide", "bh-ringed-hide", "bh-ringed-hide", "bh-heartwood-brace");
            Druid.GainBark(40);
            Assert.IsTrue(session.UseAbility(7)); Assert.AreEqual(0, Druid.Bark);
            Assert.AreEqual(.65f, Combat(session.Player).Statuses.IncomingDamageMultiplier, .001f);
        }
        [UnityTest] public IEnumerator Rending_flurry_spends_fang_over_two_seconds()
        {
            Buy("tc-keen-claws", "tc-keen-claws", "tc-keen-claws", "tc-keen-claws", "tc-keen-claws", "tc-rending-flurry");
            var enemy = session.Enemies[0];
            yield return Shift(DruidForm.Thornclaw); Near(enemy, 2.5f);
            Druid.GainFang(5); int before = enemy.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(7)); Assert.AreEqual(0, Druid.Fang);
            yield return new WaitForSeconds(2.3f);
            Assert.IsFalse(Druid.Periodic.HasKey("flurry"));
            Assert.Less(enemy.actor.Health.Pool.Current, before - 40, "8 strikes landed.");
        }
        [UnityTest] public IEnumerator Burst_bloom_consumes_seedling_for_a_big_heal()
        {
            Buy("rm-slow-sap", "rm-slow-sap", "rm-slow-sap", "rm-slow-sap", "rm-slow-sap", "rm-burst-bloom");
            yield return Shift(DruidForm.Rootmend);
            session.Player.Health.ApplyDamage(150); Assert.IsTrue(session.UseAbility(4));
            Druid.GainSap(30); yield return new WaitForSeconds(1.6f);
            int hurt = session.Player.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(7));
            Assert.IsFalse(Druid.Periodic.Has("seedling", session.Player));
            Assert.Greater(session.Player.Health.Pool.Current - hurt, 40);
        }
        [UnityTest] public IEnumerator Bramblestorm_spends_glimmer_and_damages_the_area()
        {
            Buy("ts-green-voice", "ts-green-voice", "ts-green-voice", "ts-green-voice", "ts-green-voice", "ts-bramblestorm");
            var enemy = session.Enemies[0]; Near(enemy, 9);
            Druid.GainGlimmer(60); int before = enemy.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(7)); Assert.AreEqual(0, Druid.Glimmer);
            yield return new WaitForSeconds(3.3f);
            Assert.Less(enemy.actor.Health.Pool.Current, before - 30);
        }
        [UnityTest] public IEnumerator Druid_save_round_trips_talents_and_respec_keeps_form()
        {
            Buy("ts-green-voice", "ts-green-voice");
            session.Save(); session.Load(); yield return null;
            Assert.AreEqual(2, session.TalentRank("ts-green-voice")); Assert.AreEqual("class.druid", session.Progress.classId);
            Assert.IsTrue(session.ResetTalents()); Assert.AreEqual(0, session.TalentRank("ts-green-voice"));
        }
    }
}
#endif
