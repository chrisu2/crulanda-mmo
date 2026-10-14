#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The Scoundrel in play (Docs/CLASS_SCOUNDREL.md, 2026-10-14): its bar locks each path's abilities behind that path's talents;
    /// Quick Cut builds Nerve and Settle the Score spends it all; Glimmer dazes four and a blow wakes only the one it lands on; Stay holds
    /// through blows; the debt comes due; the War-Verse lifts the party's blows and stops on a second use; a Bribe costs crowns.</summary>
    public class ScoundrelLoopTests
    {
        string root; EncounterSession session;
        ScoundrelKit Kit { get { return session.Kit as ScoundrelKit; } }
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-scoundrel-" + Guid.NewGuid().ToString("N"));
            EncounterSession.StartClassOverride = "class.scoundrel";
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("PlayableEncounter", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(Kit);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            EncounterSession.StartClassOverride = null; SceneManager.sceneLoaded -= OnLoaded; var was = SceneManager.GetActiveScene().name;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(was);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        const int QuickCut = 0, Jeer = 1, Settle = 2, Wine = 3, MarkDebt = 4, Glimmer = 5, Jagged = 6, Stay = 7, WarVerse = 8, Rally = 9, Bribe = 10, Favour = 11;
        void Level15() { session.Progress.experience = EncounterProgress.XpForLevel(15); session.Player.SetLevel(15); session.Player.Resource.Pool.Fill(); }
        /// <summary>Talents as the balance harness buys them: every point in one path.</summary>
        void BuyPath(string branchId)
        {
            Level15(); session.Progress.talents = new List<TalentRank>();
            var ids = session.Talents.Branches.First(b => b.id == branchId).nodes.Select(n => n.id).ToList();
            for (int pass = 0; pass < 40 && session.Talents.Available(session.Progress) > 0; pass++)
                foreach (var id in ids) if (session.Talents.Propose(session.Progress, id, 1, out var next, out _)) session.Progress.talents = next;
            session.Kit.ApplyStats();
        }
        void Learn(params string[] ids)
        {
            foreach (var id in ids) { Assert.IsTrue(session.Talents.Propose(session.Progress, id, 1, out var next, out var why), id + ": " + why); session.Progress.talents = next; }
            session.Kit.ApplyStats();
        }
        List<EncounterEnemy> Foes() { return session.Enemies.Where(x => x != null && x.actor.IsAlive && !x.Elite && !x.Game && !x.Engaged).ToList(); }
        EncounterEnemy Foe() { var e = Foes().FirstOrDefault(); Assert.NotNull(e, "A living mob."); return e; }
        void Near(EncounterEnemy e, float d) { session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * d); session.Select(e); }
        static void Put(EncounterEnemy e, Vector3 at) { var ag = e.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (ag != null && ag.isOnNavMesh) ag.Warp(at); else e.transform.position = at; }

        [UnityTest] public IEnumerator The_Scoundrel_is_its_own_character_and_each_path_waits_on_its_talents()
        {
            Assert.AreEqual("class.scoundrel", session.ClassDef.id); Assert.AreEqual(Crulanda.Core.ResourceKind.Focus, session.ClassDef.resource);
            Assert.AreEqual("Nerve", session.ClassDef.resourceName); Assert.AreEqual(12, session.Kit.ActionCount);
            session.Progress.talents = new List<TalentRank>(); session.Progress.experience = 0; session.Player.SetLevel(1);
            Assert.IsNull(session.Kit.ActionLockLabel(QuickCut), "Quick Cut from the first"); Assert.AreEqual("Level 2", session.Kit.ActionLockLabel(Jeer));
            for (int slot = Settle; slot <= Favour; slot++) Assert.AreEqual("Talent", session.Kit.ActionLockLabel(slot), session.Kit.ActionAt(slot).id);
            BuyPath("silver-tongue");
            for (int slot = Settle; slot <= Favour; slot++)
                Assert.AreEqual(slot >= Glimmer && slot <= Stay ? null : "Talent", session.Kit.ActionLockLabel(slot), "a Silver Tongue: " + session.Kit.ActionAt(slot).id);
            Assert.IsTrue(session.Kit.MeleeAutoAttacks); Assert.AreEqual(ActorLook.Ranger, EncounterSession.LookForClass("class.scoundrel"));
            yield return null;
        }
        [UnityTest] public IEnumerator Quick_Cut_builds_Nerve_and_Settle_the_Score_spends_it_all()
        {
            Level15(); Learn("ct-settle"); var e = Foe(); Near(e, 2); yield return null;
            var pool = session.Player.Resource.Pool; pool.SetCurrent(50);
            Assert.IsTrue(session.UseAbility(QuickCut), "Quick Cut"); Assert.GreaterOrEqual(pool.Current, 50 + ScoundrelKit.QuickCutNerve, "Nerve built");
            yield return new WaitForSeconds(1.1f);
            if (!e.actor.IsAlive) yield break;
            pool.SetCurrent(80); int before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(Settle), "Settle the Score"); Assert.AreEqual(0, pool.Current, "all the Nerve spent");
            Assert.Less(e.actor.Health.Pool.Current, before, "the cut lands");
        }
        [UnityTest] public IEnumerator Glimmer_dazes_four_and_a_blow_wakes_only_the_one_it_lands_on()
        {
            // The test scene has too few ordinary mobs for a verse of four: this one plays in Oakhaven, among its camps.
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.NotNull(Kit, "a Scoundrel in Oakhaven");
            Level15(); Learn("st-glimmer");
            var foes = session.Enemies.Where(x => x != null && x.actor.IsAlive && x.Camp && !x.Elite && !x.Game && !x.Hidden).Take(5).ToList();
            Assert.GreaterOrEqual(foes.Count, 4, "Oakhaven's camps");
            var at = foes[0].transform.position;
            for (int i = 1; i < foes.Count; i++) Put(foes[i], at + Quaternion.Euler(0, i * 72, 0) * Vector3.forward * (1.2f + .4f * i));
            Near(foes[0], 2.5f); yield return null;
            Assert.IsTrue(session.UseAbility(Glimmer), "Glimmer"); yield return new WaitForSeconds(1.8f);   // a sung verse: 1.5 s
            int held = foes.Count(f => f.Incapacitated);
            Assert.AreEqual(Mathf.Min(4, foes.Count), held, "four dazed at most");
            Assert.IsTrue(foes[0].Incapacitated, "the target among them"); StringAssert.StartsWith("Held", foes[0].ControlLabel);
            var others = foes.Skip(1).Where(f => f.Incapacitated).ToList();
            Assert.IsTrue(session.UseAbility(QuickCut), "a cut on the target"); yield return null;
            Assert.IsFalse(foes[0].Incapacitated, "the blow wakes it");
            Assert.IsTrue(others.All(f => f.Incapacitated), "the rest stand dazed");
        }
        [UnityTest] public IEnumerator Stay_holds_through_blows()
        {
            BuyPath("silver-tongue"); var e = Foe(); Near(e, 2.5f); yield return null;
            Assert.IsTrue(session.UseAbility(Stay), "Stay"); yield return null;
            Assert.IsTrue(e.Stunned, "held by a word");
            yield return new WaitForSeconds(1.1f);
            if (!e.actor.IsAlive) yield break;
            Assert.IsTrue(session.UseAbility(QuickCut)); yield return null;
            Assert.IsTrue(e.Stunned, "a blow does not break it");
        }
        [UnityTest] public IEnumerator The_debt_comes_due()
        {
            BuyPath("cutthroat"); var e = Foe(); Near(e, 2); yield return null;
            Assert.IsTrue(session.UseAbility(MarkDebt), "Mark the Debt"); Assert.IsTrue(Kit.Indebted(e));
            StringAssert.Contains("Debt due", session.Kit.TargetStatus(e));
            for (int k = 0; k < 3 && e.actor.IsAlive; k++) { yield return new WaitForSeconds(1.1f); session.UseAbility(QuickCut); }
            if (!e.actor.IsAlive) yield break;
            Assert.Greater(Kit.Owed(e), 0, "the cuts are run up");
            yield return new WaitForSeconds(6f);
            Assert.IsFalse(Kit.Indebted(e), "the debt came due and is settled");
        }
        [UnityTest] public IEnumerator The_War_Verse_lifts_the_party_and_stops_on_a_second_use()
        {
            BuyPath("fixer"); var e = Foe(); yield return null;
            Assert.AreEqual(1f, session.Kit.PartyDamageMultiplier(e), 1e-4f, "not yet sung");
            Assert.IsTrue(session.UseAbility(WarVerse), "War-Verse"); Assert.AreEqual(ScoundrelKit.Verses.War, Kit.Verse);
            Assert.AreEqual(1 + ScoundrelKit.WarLift + ScoundrelKit.RousingPerRank * session.TalentRank("fx-rousing"), session.Kit.PartyDamageMultiplier(e), 1e-4f);
            StringAssert.Contains("War-Verse", session.Kit.StatusLine);
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(session.UseAbility(WarVerse), "again, to stop"); Assert.AreEqual(ScoundrelKit.Verses.None, Kit.Verse);
            Assert.AreEqual(1f, session.Kit.PartyDamageMultiplier(e), 1e-4f);
        }
        [UnityTest] public IEnumerator A_Bribe_costs_crowns_and_holds_a_foe_that_can_be_bought()
        {
            BuyPath("fixer"); var e = Foes().FirstOrDefault(ScoundrelKit.Bribable); if (e == null) Assert.Ignore("no foe here that can be bought");
            session.Progress.gold = 100; Near(e, 3); yield return null;
            Assert.IsTrue(session.UseAbility(Bribe), "Bribe"); yield return null;
            Assert.IsTrue(e.Incapacitated, "it stops fighting"); Assert.AreEqual(100 - Mathf.Max(1, e.actor.Level), session.Progress.gold, "its level in crowns");
        }
        [UnityTest] public IEnumerator Call_in_a_Favour_needs_someone_waiting()
        {
            BuyPath("fixer"); session.Progress.recruited = false; if (session.Companion != null) session.Companion.gameObject.SetActive(false);
            yield return null; int nerve = session.Player.Resource.Pool.Current;
            Assert.IsFalse(session.UseAbility(Favour), "no ally near: no favour"); Assert.AreEqual(nerve, session.Player.Resource.Pool.Current, "nothing spent");
        }
    }
}
#endif
