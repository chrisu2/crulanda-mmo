#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    public class EncounterLoopTests
    {
        string root;
        EncounterSession session;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-loop-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("PlayableEncounter", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(session.Player);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("PlayableEncounter");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        [UnityTest]
        public IEnumerator Talent_stats_persist_and_respec_without_stacking()
        {
            int initial=session.WeaponDamage;
            Assert.IsTrue(session.ChangeTalent("dp-weapon-pressure",1)); Assert.AreEqual(initial+2,session.WeaponDamage);
            Assert.IsFalse(session.ChangeTalent("tk-intercept",1), "Second row needs 5 points in Tank.");
            session.Save(); session.Load(); yield return null;
            Assert.AreEqual(initial+2,session.WeaponDamage); Assert.AreEqual(1,session.TalentRank("dp-weapon-pressure"));
            Assert.IsTrue(session.ResetTalents()); Assert.AreEqual(initial,session.WeaponDamage);
            Assert.IsTrue(session.ChangeTalent("dp-weapon-pressure",1)); Assert.AreEqual(initial+2,session.WeaponDamage);
        }
        void Buy(params string[] ids)
        {
            session.Progress.experience = EncounterProgress.XpForLevel(10); session.Player.SetLevel(10);
            foreach(var id in ids) Assert.IsTrue(session.ChangeTalent(id,1), id);
        }
        static readonly string[] TankPath = { "tk-hardened-grip","tk-hardened-grip","tk-hardened-grip","tk-tempered-armor","tk-tempered-armor",
            "tk-timed-guard","tk-timed-guard","tk-timed-guard","tk-steady-challenge","tk-intercept","tk-bulwark" };
        static readonly string[] DpsPath = { "dp-weapon-pressure","dp-weapon-pressure","dp-read-the-opening","dp-read-the-opening","dp-read-the-opening",
            "dp-edge-discipline","dp-edge-discipline","dp-edge-discipline","dp-finishing-strike","dp-breaching-blow","dp-sweeping-strike" };
        static readonly string[] SupportPath = { "sp-rally-reserve","sp-rally-reserve","sp-called-cadence","sp-called-cadence","sp-called-cadence",
            "sp-mark-the-gap","sp-mark-the-gap","sp-mark-the-gap","sp-rallying-challenge","sp-muster","sp-shared-shelter" };
        void NearEnemy(EncounterEnemy enemy) { session.Player.GetComponent<AdventurerMotor>().Teleport(enemy.transform.position+Vector3.back*2.5f); session.Select(enemy); }
        [UnityTest] public IEnumerator Tank_signature_modifies_guard_and_respec_clears_it()
        {
            Buy(TankPath);
            Assert.IsTrue(session.UseAbility(2));
            var combat=session.Player.GetComponent<Crulanda.Combat.Combatant>();
            Assert.AreEqual(.25f,combat.Statuses.IncomingDamageMultiplier);
            Assert.Greater(combat.Statuses.Remaining("status.guard",Time.time),6.9f);
            Assert.AreEqual(22,combat.Damage(100));
            Assert.IsTrue(session.ResetTalents()); Assert.AreEqual(1,combat.Statuses.IncomingDamageMultiplier);
            Assert.AreEqual(0,session.Player.Stats.Get(Crulanda.Core.StatType.Armor));
            yield return null;
        }
        [UnityTest] public IEnumerator Dps_signature_cleaves_and_combat_blocks_respec()
        {
            Buy(DpsPath);
            var enemy=session.Enemies[0]; var other=session.Enemies[1];
            session.Player.GetComponent<AdventurerMotor>().Teleport(enemy.transform.position+Vector3.back*2.5f);
            other.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(enemy.transform.position+Vector3.right*2);
            session.Select(enemy); int before=other.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(0)); Assert.Less(other.actor.Health.Pool.Current,before);
            Assert.IsFalse(session.ResetTalents()); Assert.IsFalse(session.ChangeTalent("dp-sweeping-strike",-1));
            yield return null;
        }
        [UnityTest] public IEnumerator Breaching_blow_is_talent_gated_spends_pressure_and_exposes()
        {
            Assert.IsFalse(session.ActionUnlocked(4)); Assert.AreEqual("Talent", session.ActionLockLabel(4));
            Buy(DpsPath);
            Assert.IsTrue(session.ActionUnlocked(4)); Assert.AreEqual("Breaching Blow", session.ActionAt(4).name);
            var enemy=session.Enemies[0]; NearEnemy(enemy);
            Assert.IsFalse(session.UseAbility(4), "No pressure yet.");
            Assert.IsTrue(session.UseAbility(0)); Assert.AreEqual(1, session.Warrior.Pressure);
            session.Warrior.GainPressure(10); Assert.AreEqual(WarriorKit.MaxPressure, session.Warrior.Pressure);
            yield return new WaitForSeconds(1.6f);
            int before=enemy.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(4));
            Assert.AreEqual(0, session.Warrior.Pressure);
            Assert.GreaterOrEqual(before-enemy.actor.Health.Pool.Current, 50);
            Assert.IsTrue(session.Warrior.IsExposed(enemy));
            Assert.Greater(session.Warrior.ExposedRemaining(enemy), 5.4f, "4s + 0.5s per Read the Opening rank.");
            Assert.AreEqual(1.05f, session.Warrior.PartyDamageMultiplier(enemy), .0001f);
        }
        [UnityTest] public IEnumerator Guard_exposes_attacker_and_timed_guard_refunds_vigor()
        {
            Buy(TankPath);
            var enemy=session.Enemies[0]; NearEnemy(enemy);
            Assert.IsTrue(session.UseAbility(2));
            session.Player.Resource.Pool.SetCurrent(50);
            session.Warrior.ResolveEnemyHit(enemy, session.Player, 13);
            Assert.AreEqual(50+3+9, session.Player.Resource.Pool.Current, "Hardened Grip 3 + Timed Guard 3x3.");
            Assert.IsTrue(session.Warrior.IsExposed(enemy));
            session.Warrior.ResolveEnemyHit(enemy, session.Player, 13);
            Assert.AreEqual(62+3, session.Player.Resource.Pool.Current, "Timed Guard pays once per Guard.");
            yield return null;
        }
        [UnityTest] public IEnumerator Intercept_moves_to_mira_and_takes_her_next_blow()
        {
            Buy(TankPath); session.Progress.recruited=true;
            var enemy=session.Enemies[0];
            session.Companion.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(session.Player.transform.position+Vector3.right*6);
            yield return null;
            Assert.IsTrue(session.UseAbility(3));
            Assert.Less(Vector3.Distance(session.Player.transform.position, session.Companion.transform.position), 2.5f);
            int mira=session.Companion.actor.Health.Pool.Current, you=session.Player.Health.Pool.Current;
            session.Warrior.ResolveEnemyHit(enemy, session.Companion.actor, 13);
            Assert.AreEqual(mira, session.Companion.actor.Health.Pool.Current);
            Assert.Less(session.Player.Health.Pool.Current, you);
            session.Warrior.ResolveEnemyHit(enemy, session.Companion.actor, 13);
            Assert.Less(session.Companion.actor.Health.Pool.Current, mira, "Only the next blow is intercepted.");
        }
        [UnityTest] public IEnumerator Muster_barrier_absorbs_and_plants_standard()
        {
            Buy("sp-rally-reserve","sp-rally-reserve","sp-called-cadence","sp-called-cadence","sp-called-cadence",
                "sp-mark-the-gap","sp-mark-the-gap","sp-mark-the-gap","sp-rallying-challenge","sp-muster","sp-planted-standard");
            session.Progress.recruited=true;
            session.Companion.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(session.Player.transform.position+Vector3.right*3);
            yield return null;
            Assert.IsTrue(session.UseAbility(5));
            var you=session.Player.GetComponent<Crulanda.Combat.Combatant>();
            Assert.AreEqual(20, you.Barrier); Assert.AreEqual(20, session.Companion.actor.GetComponent<Crulanda.Combat.Combatant>().Barrier);
            int health=session.Player.Health.Pool.Current;
            Assert.AreEqual(0, you.Damage(12)); Assert.AreEqual(health, session.Player.Health.Pool.Current);
            Assert.Greater(session.Warrior.StandardUntil, Time.time + 3.5f);
            Assert.IsTrue(session.ResetTalents()); Assert.AreEqual(0, you.Barrier, "Respec clears temporary talent benefits.");
        }
        [UnityTest] public IEnumerator Support_challenge_and_guard_help_nearby_companion()
        {
            Buy(SupportPath); session.Progress.recruited=true;
            var enemy=session.Enemies[0];
            session.Player.GetComponent<AdventurerMotor>().Teleport(enemy.transform.position+Vector3.back*2.5f);
            session.Companion.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(session.Player.transform.position+Vector3.right*2);
            session.Companion.actor.Resource.Pool.SetCurrent(10); session.Companion.actor.Health.Pool.SetCurrent(50);
            session.Select(enemy); Assert.IsTrue(session.UseAbility(1)); Assert.AreEqual(22,session.Companion.actor.Resource.Pool.Current);
            Assert.IsTrue(session.UseAbility(2)); Assert.AreEqual(70,session.Companion.actor.Health.Pool.Current);
            Assert.AreEqual(.7f,session.Companion.actor.GetComponent<Crulanda.Combat.Combatant>().Statuses.IncomingDamageMultiplier);
            yield return null;
        }
        [UnityTest]
        public IEnumerator Class_unlocks_block_costs_and_effects_until_required_level()
        {
            var unlock = session.content.playerClass.unlocks[2];
            int previous = unlock.level;
            try
            {
                unlock.level = 4;
                int before = session.Player.Resource.Pool.Current;
                Assert.IsFalse(session.UseAbility(2));
                Assert.AreEqual(before, session.Player.Resource.Pool.Current);
                Assert.AreEqual(0, session.Player.GetComponent<Crulanda.Combat.Combatant>().Statuses.Count);
                Assert.IsFalse(session.UseAbility(-1)); Assert.IsFalse(session.UseAbility(999));
                session.Player.SetLevel(4);
                Assert.IsTrue(session.UseAbility(2));
                Assert.AreEqual(before - session.ActionAt(2).cost, session.Player.Resource.Pool.Current);
                Assert.AreEqual(1, session.Player.GetComponent<Crulanda.Combat.Combatant>().Statuses.Count);
            }
            finally { unlock.level = previous; }
            yield return null;
        }
        [UnityTest]
        public IEnumerator Auto_attacks_continue_while_strike_is_on_cooldown()
        {
            var enemy = session.Enemies[0];
            session.Player.GetComponent<AdventurerMotor>().Teleport(enemy.transform.position + Vector3.back * 2.5f);
            session.Select(enemy);
            Assert.IsTrue(session.UseAbility(0));
            int afterStrike = enemy.actor.Health.Pool.Current;
            yield return new WaitForSeconds(session.content.playerSwingInterval + .2f);
            Assert.Greater(session.CooldownRemaining(0), 0);
            Assert.Less(enemy.actor.Health.Pool.Current, afterStrike);
            Assert.IsTrue(session.AutoAttack);
            Assert.Greater(session.SwingRemaining, 0);
        }
        [UnityTest]
        public IEnumerator Solid_scenery_blocks_player_and_navigation()
        {
            var tent = GameObject.Find("Supply tent");
            Assert.NotNull(tent.GetComponent<Collider>());
            session.Player.GetComponent<AdventurerMotor>().Teleport(new Vector3(-8, 1.05f, -17));
            var controller = session.Player.GetComponent<CharacterController>();
            for (int i = 0; i < 60; i++) controller.Move(Vector3.forward * .1f);
            Assert.Less(session.Player.transform.position.z, -14.7f, "Tent must stop the player.");
            UnityEngine.AI.NavMeshHit hit;
            Assert.IsFalse(UnityEngine.AI.NavMesh.SamplePosition(new Vector3(-8, 0, -13), out hit, .2f, UnityEngine.AI.NavMesh.AllAreas));
            var path = new UnityEngine.AI.NavMeshPath();
            Assert.IsTrue(UnityEngine.AI.NavMesh.CalculatePath(new Vector3(-8, 0, -17), new Vector3(-8, 0, -9), UnityEngine.AI.NavMesh.AllAreas, path));
            Assert.AreEqual(UnityEngine.AI.NavMeshPathStatus.PathComplete, path.status);
            Assert.Greater(path.corners.Length, 2, "Agents must route around the tent.");
            yield return null;
        }
        [UnityTest]
        public IEnumerator Recruit_fight_heal_loot_equip_save_and_reload()
        {
            session.Interact(); Assert.IsTrue(session.Progress.recruited);
            string playerId = session.Player.EntityId.Value;
            session.Player.Health.ApplyDamage(70);
            int hurt = session.Player.Health.Pool.Current;
            yield return new WaitForSeconds(2);
            Assert.Greater(session.Player.Health.Pool.Current, hurt);
            Assert.Less(session.Companion.actor.Resource.Pool.Current, 120);
            var motor = session.Player.GetComponent<AdventurerMotor>();
            foreach (var enemy in session.Enemies)
            {
                motor.Teleport(enemy.transform.position + Vector3.back * 2.5f);
                session.Select(enemy);
                float fightStarted = Time.time;
                float deadline = Time.time + 45;
                while (enemy.actor.IsAlive && session.Player.IsAlive && Time.time < deadline)
                {
                    // Tank like a player would: without Challenge, Mira's healing threat can legitimately pull the
                    // sentry out of melee reach (seen when fight timings shift), which is aggro behavior, not a bug.
                    session.UseAbility(0); session.UseAbility(1); session.UseAbility(2);
                    yield return new WaitForSeconds(.2f);
                }
                Assert.IsTrue(session.Player.IsAlive, "Player died before completing the encounter.");
                Assert.IsFalse(enemy.actor.IsAlive, "Enemy did not die within timeout.");
                float duration = Time.time - fightStarted;
                Debug.Log("PACING_MEASUREMENT " + enemy.persistentId + " " + duration.ToString("F1") + " seconds");
                Assert.GreaterOrEqual(duration, 15f, "Fresh-character fights should leave time for tactical decisions.");
                motor.Teleport(enemy.transform.position + Vector3.back * 2);
                session.Interact();
            }
            session.Equip(); Assert.IsTrue(Inventory.IsEquipped(session.Progress, session.content.itemId));
            int earned = 2 * EncounterProgress.KillXp(1, 1, false) + EncounterProgress.KillXp(1, 1, true);   // two level-1 sentries and the veteran (counts as elite)
            Assert.AreEqual(earned, session.Progress.experience); Assert.AreEqual(24, session.Progress.gold);
            Assert.AreEqual(session.Progress.Level, session.Player.Level);
            session.Save(); session.Load(); yield return null;
            Assert.AreEqual(playerId, session.Player.EntityId.Value);
            Assert.AreEqual(24, session.Progress.gold); Assert.AreEqual(earned, session.Progress.experience);
            Assert.IsTrue(Inventory.IsEquipped(session.Progress, session.content.itemId));
            Assert.IsTrue(session.Enemies.TrueForAll(e => !e.actor.IsAlive));
        }
        [UnityTest]
        public IEnumerator Keyboard_movement_and_camera_follow_work()
        {
            var previousBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            var previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var otherKeyboards = new System.Collections.Generic.List<Keyboard>();
            foreach (var device in InputSystem.devices)
                if (device is Keyboard existing && existing.enabled) { otherKeyboards.Add(existing); InputSystem.DisableDevice(existing); }
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                var start = session.Player.transform.position;
                var cameraStart = session.View.transform.position;
                float finish = Time.time + .7f;
                while (Time.time < finish)
                {
                    keyboard.MakeCurrent();
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                    yield return null;
                }
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.Greater(session.Player.transform.position.z - start.z, 2f);
                Assert.Greater(session.View.transform.position.z - cameraStart.z, 2f);
            }
            finally {
                InputSystem.RemoveDevice(keyboard);
                foreach (var existing in otherKeyboards) InputSystem.EnableDevice(existing);
                InputSystem.settings.editorInputBehaviorInPlayMode = previousBehavior;
                InputSystem.settings.backgroundBehavior = previousBackground;
            }
        }
        [UnityTest]
        public IEnumerator Death_recovery_keeps_equipment_and_repositions_party()
        {
            session.Player.Health.ApplyDamage(10000);
            Assert.IsFalse(session.Player.IsAlive);
            session.Recover(); yield return null;
            Assert.IsTrue(session.Player.IsAlive);
            Assert.Less(Vector3.Distance(session.Player.transform.position, new Vector3(0,1.1f,-13)), .5f);
            Assert.IsTrue(session.Companion.actor.IsAlive);
        }
    }
}
#endif





