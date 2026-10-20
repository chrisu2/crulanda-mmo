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
            Assert.AreEqual(13, session.Kit.ActionCount, "ten, and the Skirmisher's three"); Assert.AreEqual("ranger.pin", session.Kit.ActionAt(Pin).id);
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

        /// <summary>The Snare Trap lies on the ground at your feet (playtest note 123, Chris, 2026-10-11: "trap set up in the air"; it
        /// hung a metre up, at the middle of the player's capsule).</summary>
        [UnityTest] public IEnumerator Snare_Trap_lies_on_the_ground_at_your_feet()
        {
            Level10(); yield return new WaitForSeconds(.3f);
            int slot = -1; for (int i = 0; i < session.Kit.ActionCount; i++) if (session.Kit.ActionAt(i).id == "ranger.snare_trap") slot = i;
            Assert.GreaterOrEqual(slot, 0, "the Snare Trap is on the bar");
            Assert.IsTrue(session.UseAbility(slot), "Snare Trap set");
            yield return null;
            var trap = GameObject.Find("Snare trap"); Assert.NotNull(trap, "the trap is there");
            var at = trap.transform.position;
            float ground = float.NaN;
            foreach (var hit in Physics.RaycastAll(at + Vector3.up * 5, Vector3.down, 12, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                if (hit.collider.GetComponentInParent<Crulanda.Gameplay.Actor>() == null && hit.collider.gameObject != trap) { ground = hit.point.y; break; }
            Assert.IsFalse(float.IsNaN(ground), "ground under the trap");
            Assert.AreEqual(ground, at.y, .08f, "the trap lies on the ground (it was " + (at.y - ground).ToString("0.00") + " m up)");
            var feet = session.Player.GetComponent<CharacterController>().bounds.min.y;
            Assert.Less(Mathf.Abs(feet - at.y), .25f, "at the player's feet");
            var flat = at - session.Player.transform.position; flat.y = 0; Assert.Less(flat.magnitude, .3f, "where the player stands");
        }

        /// <summary>Esc stops the auto shot, then lets go of the target, then pauses (playtest note 124, Chris, 2026-10-11: "no way to
        /// turn off auto shot").</summary>
        [UnityTest] public IEnumerator Esc_stops_the_auto_shot_then_lets_go_of_the_target()
        {
            Level10(); var e = Foe(); Near(e, 15);
            Assert.IsTrue(session.UseAbility(QuickShot)); Assert.IsTrue(session.AutoAttack, "the auto shot begins");
            StringAssert.Contains("Esc stops", session.AutoAttackStatus, "the bar says how to stop it");
            session.Escape(); Assert.IsFalse(session.AutoAttack, "Esc stops the auto shot"); Assert.AreSame(e, session.Target, "the target is kept"); Assert.IsFalse(session.Paused);
            session.Escape(); Assert.IsNull(session.Target, "Esc again lets go of the target"); Assert.IsFalse(session.Paused);
            session.Escape(); Assert.IsTrue(session.Paused, "then the menu"); session.Resume();
            yield return null;
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

        /// <summary>Playtest note 90: called, the wolf joins the Ranger's fight unasked (no Sic, no Pack Sense).</summary>
        [UnityTest] public IEnumerator The_wolf_joins_the_fight_unasked()
        {
            Level10(); var e = Foe(); Near(e, 8);
            Assert.IsTrue(session.UseAbility(CallCompanion), "Call Companion"); yield return new WaitForSeconds(1.6f);
            Assert.IsNull(session.Pet.Quarry, "at heel before the fight");
            int before = e.actor.Health.Pool.Current;
            session.Select(e); session.BeginAutoAttack(); yield return null; yield return null;
            Assert.AreSame(e, session.Pet.Quarry, "goes for what the Ranger fights");
            yield return new WaitForSeconds(4f);
            Assert.Less(e.actor.Health.Pool.Current, before, "and bites it");
        }
        /// <summary>Playtest note 93: the companion bar's orders. Stay holds it out of your fight; Attack sends it; Assist brings it back to heel.</summary>
        [UnityTest] public IEnumerator The_wolf_stays_attacks_and_assists_on_order()
        {
            Level10(); var e = Foe(); Near(e, 8);
            Assert.IsTrue(session.UseAbility(CallCompanion)); yield return new WaitForSeconds(1.6f);
            session.PetOrder(RangerPet.Order.Stay); Assert.AreEqual(RangerPet.Order.Stay, session.Pet.Mode);
            session.Select(e); session.BeginAutoAttack(); yield return null; yield return null;
            Assert.IsNull(session.Pet.Quarry, "staying, it keeps out of it");
            Assert.IsTrue(session.PetAttack(), "Attack"); Assert.AreSame(e, session.Pet.Quarry);
            session.PetOrder(RangerPet.Order.Assist); Assert.AreEqual(RangerPet.Order.Assist, session.Pet.Mode);
            session.Select(null); Assert.IsFalse(session.PetAttack(), "no target, no attack");
        }
        int Slot(string id) { for (int i = 0; i < session.Kit.ActionCount; i++) if (session.Kit.ActionAt(i).id == id) return i; return -1; }
        void Level15() { session.Progress.experience = EncounterProgress.XpForLevel(15); session.Player.SetLevel(15); session.Player.Resource.Pool.SetCurrent(100); }
        void Learn(params string[] ids)
        {
            foreach (var id in ids) { Assert.IsTrue(session.Talents.Propose(session.Progress, id, 1, out var next, out var why), id + ": " + why); session.Progress.talents = next; }
            session.Kit.ApplyStats();
        }
        /// <summary>The Skirmisher (TALENT_DEPTH.md, 2026-10-11): Spear Lunge bounds in from 7 m to a stride from the target and drives the
        /// spear for 1.6 times the weapon's hit, on Focus; past its 8 m it refuses.</summary>
        [UnityTest] public IEnumerator Spear_Lunge_bounds_in_and_drives_the_spear()
        {
            Level10(); session.Player.Resource.Pool.SetCurrent(100); var e = Foe(); int lunge = Slot("ranger.spear_lunge");
            Assert.AreEqual(10, lunge, "on the - key"); Assert.IsNull(session.Kit.ActionLockLabel(lunge), "level 4");
            Near(e, 12); Assert.IsFalse(session.UseAbility(lunge), "out of its 8 m");
            Near(e, 7); yield return null;
            int before = e.actor.Health.Pool.Current, focus = session.Player.Resource.Pool.Current;
            Assert.IsTrue(session.UseAbility(lunge), "Spear Lunge from 7 m"); yield return null;
            var flat = e.transform.position - session.Player.transform.position; flat.y = 0;
            Assert.Less(flat.magnitude, 2.4f, "bounded in to a stride from it (" + flat.magnitude.ToString("0.0") + " m)");
            float hit = 6 + session.WeaponDamage * RangerKit.LungeScale;
            Assert.GreaterOrEqual(before - e.actor.Health.Pool.Current, Mathf.Min(before, Mathf.FloorToInt(hit * .75f)), "the spear's hit, half again a Quick Shot's and more");
            Assert.Less(session.Player.Resource.Pool.Current, focus, "on Focus");
        }
        /// <summary>Whirling Spear: every foe within 3.5 m takes the haft, the one in front and the one beside it.</summary>
        [UnityTest] public IEnumerator Whirling_Spear_strikes_every_foe_around_you()
        {
            Level10(); session.Player.Resource.Pool.SetCurrent(100);
            var foes = session.Enemies.Where(x => x != null && x.actor.IsAlive && !x.Game && !x.Hidden).Take(2).ToList(); Assert.AreEqual(2, foes.Count, "two foes");
            var a = foes[0]; var b = foes[1]; var spot = a.transform.position + Vector3.right * 1.6f;
            var agent = b.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (agent != null && agent.isOnNavMesh) agent.Warp(spot); else b.transform.position = spot;
            Near(a, 1.5f); yield return null;
            int ha = a.actor.Health.Pool.Current, hb = b.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(Slot("ranger.whirling_spear")), "Whirling Spear");
            Assert.Less(a.actor.Health.Pool.Current, ha, "the one in front"); Assert.Less(b.actor.Health.Pool.Current, hb, "and the one beside it");
        }
        /// <summary>The Skirmisher's talents at work: Feint turns the next blow aside (one blow only), Light Step and Pack Tactics close to a
        /// foe the wolf is on, Hit and Run's free lunge after Disengage, Long Haft's longer reach and Bleeding Point's wound.</summary>
        [UnityTest] public IEnumerator Feint_Hit_and_Run_and_the_Skirmishers_talents()
        {
            Level15(); int feint = Slot("ranger.feint"), lunge = Slot("ranger.spear_lunge");
            Assert.AreEqual("Talent", session.Kit.ActionLockLabel(feint), "Feint waits on its talent");
            Learn("sk-long-haft", "sk-long-haft", "sk-long-haft", "sk-light-step", "sk-light-step", "sk-light-step", "sk-bleeding-point", "sk-bleeding-point", "sk-bleeding-point",
                "sk-quick-recovery", "sk-quick-recovery", "sk-feint", "sk-pack-tactics", "sk-pack-tactics", "sk-pack-tactics", "sk-hit-and-run");
            Assert.IsNull(session.Kit.ActionLockLabel(feint), "Feint learnt");
            var e = Foe(); Near(e, 2); yield return null;
            Assert.AreEqual(1.03f, session.Kit.MoveSpeedMultiplier, 1e-3f, "Light Step: 3% quicker with a foe within 5 m");
            Assert.IsTrue(session.UseAbility(feint), "Feint"); Assert.IsTrue(Ranger.Feinted(e)); StringAssert.Contains("FEINTED", session.Kit.TargetStatus(e));
            int hp = session.Player.Health.Pool.Current;
            Assert.AreEqual(0, session.Kit.ResolveEnemyHit(e, session.Player, 40), "its next blow misses"); Assert.AreEqual(hp, session.Player.Health.Pool.Current);
            Assert.IsFalse(Ranger.Feinted(e), "one blow only"); Assert.Greater(session.Kit.ResolveEnemyHit(e, session.Player, 40), 0, "the one after lands");
            // The wolf on it, then Disengage (Hit and Run) and a free lunge back in from 10 m (Long Haft: 11 m).
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(CallCompanion)); yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(Sic)); Assert.AreSame(e, session.Pet.Quarry);
            Assert.IsTrue(session.UseAbility(Disengage), "Disengage"); Assert.IsTrue(Ranger.FreeLunge, "Hit and Run: the next lunge is free");
            StringAssert.Contains("free Spear Lunge", session.Kit.StatusLine);
            yield return new WaitForSeconds(1.6f);
            Near(e, 10); session.Player.Resource.Pool.SetCurrent(50);
            Assert.IsTrue(session.UseAbility(lunge), "Spear Lunge from 10 m (Long Haft)");
            Assert.AreEqual(50, session.Player.Resource.Pool.Current, "free"); Assert.IsFalse(Ranger.FreeLunge, "and spent");
            if (e.actor.IsAlive)
            {
                StringAssert.Contains("Wounded", session.Kit.TargetStatus(e), "Bleeding Point");
                Assert.AreEqual(1.09f, session.Kit.PartyDamageMultiplier(e), 1e-3f, "Pack Tactics: struck by you and the wolf on it, 9% more");
            }
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
