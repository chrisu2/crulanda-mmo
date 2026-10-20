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
        const int Sinister = 0, Eviscerate = 1, Sap = 2, Gouge = 3, Kick = 4, Blind = 5, Vanish = 6, OpeningCut = 7, Flourish = 8, Tumble = 9, ThrowKnife = 10;
        /// <summary>The pull (Chris, 2026-10-11: "maybe a crossbow to pull but mainly melee"): a knife thrown from 15 m lands, starts the fight and gives a point.</summary>
        [UnityTest] public IEnumerator Throw_Knife_pulls_from_range()
        {
            Level10(); var e = Foe(); Near(e, 15); yield return null;
            int before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(ThrowKnife), "Throw Knife at 15 m");
            Assert.Less(e.actor.Health.Pool.Current, before, "the knife lands"); Assert.AreEqual(1, Rogue.Combo, "a point");
        }

        /// <summary>The Swashbuckler (TALENT_DEPTH.md, 2026-10-11): Opening Cut Exposes the target (the party's blows on it 8% harder, Wide
        /// Open more), the Flourish spends the combo on haste (your swings and Mira's casts; Encore mends the party on five), Tumble rolls
        /// you to its back and the next blow from behind lands harder.</summary>
        [UnityTest] public IEnumerator The_Swashbuckler_exposes_quickens_and_tumbles()
        {
            session.Progress.experience = EncounterProgress.XpForLevel(15); session.Player.SetLevel(15); session.Player.Resource.Pool.SetCurrent(100);
            Assert.AreEqual("Talent", session.Kit.ActionLockLabel(Tumble), "Tumble waits on its talent");
            foreach (var id in new[] { "bk-showmanship", "bk-showmanship", "bk-showmanship", "bk-light-blade", "bk-light-blade", "bk-light-blade", "bk-from-behind", "bk-from-behind", "bk-from-behind",
                "bk-long-flourish", "bk-long-flourish", "bk-tumble", "bk-wide-open", "bk-wide-open", "bk-wide-open", "bk-encore" })
            { Assert.IsTrue(session.Talents.Propose(session.Progress, id, 1, out var next, out var why), id + ": " + why); session.Progress.talents = next; }
            session.Kit.ApplyStats(); Assert.IsNull(session.Kit.ActionLockLabel(Tumble), "Tumble learnt");
            var e = Foe(); Near(e, 2); yield return null;
            Assert.AreEqual(1f, session.Kit.PartyDamageMultiplier(e), 1e-4f, "not yet open");
            Assert.AreEqual("No combo", session.Kit.ActionLockLabel(Flourish));
            Assert.IsTrue(session.UseAbility(OpeningCut), "Opening Cut"); yield return null;
            Assert.IsTrue(Rogue.Exposed(e)); Assert.AreEqual(1, Rogue.Combo);
            Assert.AreEqual(1 + RogueKit.ExposedBonus + 3 * RogueKit.WideOpenPerRank, session.Kit.PartyDamageMultiplier(e), 1e-4f, "Exposed: the party's blows 8% harder, Wide Open 6% more");
            StringAssert.Contains("EXPOSED", session.Kit.TargetStatus(e));
            if (!e.actor.IsAlive) yield break;
            float before = session.Kit.SwingInterval(2.6f);
            yield return new WaitForSeconds(1.1f);
            Assert.IsTrue(session.UseAbility(Flourish), "Rallying Flourish on one point");
            Assert.AreEqual(RogueKit.HastePerPoint, Rogue.Haste, 1e-4f, "4% for the one point"); Assert.AreEqual(RogueKit.HastePerPoint, session.Kit.CompanionHaste, 1e-4f, "Mira too");
            Assert.Less(session.Kit.SwingInterval(2.6f), before, "your swings quicker"); Assert.AreEqual(0, Rogue.Combo, "the combo spent");
            StringAssert.Contains("Flourish", session.Kit.StatusLine);
            // Tumble to its back; the next blow from behind lands harder.
            yield return new WaitForSeconds(1.1f);
            Assert.IsTrue(session.UseAbility(Tumble), "Tumble"); yield return null;
            var flat = e.transform.position - session.Player.transform.position; flat.y = 0;
            Assert.Less(flat.magnitude, 2.6f, "rolled to its side (" + flat.magnitude.ToString("0.0") + " m)"); Assert.IsTrue(Rogue.Tumbled, "the opening is yours");
            yield return new WaitForSeconds(1.1f);
            if (!e.actor.IsAlive) yield break;
            Assert.IsTrue(session.UseAbility(Sinister), "a strike from behind"); Assert.IsFalse(Rogue.Tumbled, "Tumble's blow spent");
        }

        [UnityTest] public IEnumerator The_Rogue_is_its_own_character()
        {
            Assert.AreEqual("class.rogue", session.ClassDef.id); Assert.AreEqual(Crulanda.Core.ResourceKind.Focus, session.ClassDef.resource);
            Assert.AreEqual(11, session.Kit.ActionCount, "seven, the Swashbuckler's three, the thrown knife"); Assert.AreEqual("Level 3", session.Kit.ActionLockLabel(Gouge));
            // A pair of dirks from the first (Chris, 2026-10-11: "rogue should have daggers"): one in each hand (a new character is armed in a
            // zone, ArmNewCharacter; this scene has none, so the sims' kit stands for it: RogueRulesTests).
            if (session.Zone != null)
            {
                Assert.AreEqual("item.rogues_dirk", session.Progress.equipment[(int)EquipSlot.MainHand].item, "a dirk in the right hand");
                Assert.AreEqual("item.rogues_second_dirk", session.Progress.equipment[(int)EquipSlot.OffHand].item, "and its pair in the left");
            }
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
