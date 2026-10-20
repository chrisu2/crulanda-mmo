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
    /// <summary>The Pact-Blade in play (Docs/CLASSES_9_2.md section 4, 2026-10-19): its bar locks each path's abilities behind that path's
    /// talents; Measured Cut builds Vital-Aether and Aether Edge spends it all; the White Whirl cuts every foe near; Sentinel's Stop holds a
    /// foe where it stands; two swords in hand; every ability on the bar casts at 20 with its talent.</summary>
    public class PactBladeLoopTests
    {
        string root; EncounterSession session;
        PactBladeKit Kit { get { return session.Kit as PactBladeKit; } }
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-pactblade-" + Guid.NewGuid().ToString("N"));
            EncounterSession.StartClassOverride = "class.pactblade";
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
        const int Measured = 0, Second = 1, Movement = 2, Hilt = 3, Vow = 4, Stop = 5, Edge = 6, Afterimage = 7, Blood = 8, Whirl = 9, Glow = 10, Turning = 11, Answer = 12, Disarm = 13, Ring = 14;
        void Level(int level) { session.Progress.experience = EncounterProgress.XpForLevel(level); session.Player.SetLevel(level); }
        void BuyPath(string branchId)
        {
            Level(20); session.Progress.talents = new List<TalentRank>();
            var ids = session.Talents.Branches.First(b => b.id == branchId).nodes.OrderBy(n => n.tier).Select(n => n.id).ToList();
            for (int pass = 0; pass < 40 && session.Talents.Available(session.Progress) > 0; pass++)
                foreach (var id in ids) if (session.Talents.Propose(session.Progress, id, 1, out var next, out _)) session.Progress.talents = next;
            session.Kit.ApplyStats();
        }
        List<EncounterEnemy> Foes() { return session.Enemies.Where(x => x != null && x.actor.IsAlive && !x.Elite && !x.Game && !x.Engaged).ToList(); }
        EncounterEnemy Foe() { var e = Foes().FirstOrDefault(); Assert.NotNull(e, "A living mob."); return e; }
        void Near(EncounterEnemy e, float d) { session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * d); session.Select(e); }
        static void Put(EncounterEnemy e, Vector3 at) { var ag = e.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (ag != null && ag.isOnNavMesh) ag.Warp(at); else e.transform.position = at; }
        IEnumerator Ready() { for (float t = 0; t < 1.6f; t += Time.deltaTime) yield return null; }

        [UnityTest] public IEnumerator The_Pact_Blade_is_its_own_character_and_each_path_waits_on_its_talents()
        {
            Assert.AreEqual("class.pactblade", session.ClassDef.id); Assert.AreEqual("Vital-Aether", session.ClassDef.resourceName);
            Assert.AreEqual(15, session.Kit.ActionCount);
            session.Progress.talents = new List<TalentRank>(); session.Progress.experience = 0; session.Player.SetLevel(1);
            Assert.IsNull(session.Kit.ActionLockLabel(Measured)); Assert.AreEqual("Level 2", session.Kit.ActionLockLabel(Second)); Assert.AreEqual("Level 10", session.Kit.ActionLockLabel(Stop));
            for (int slot = Edge; slot <= Ring; slot++) Assert.AreEqual("Talent", session.Kit.ActionLockLabel(slot), session.Kit.ActionAt(slot).id);
            BuyPath("white-whirl");
            for (int slot = Edge; slot <= Ring; slot++) Assert.AreEqual(slot >= Whirl && slot <= Turning ? null : "Talent", session.Kit.ActionLockLabel(slot), "a White Whirl: " + session.Kit.ActionAt(slot).id);
            Assert.IsTrue(session.Kit.MeleeAutoAttacks);
            // Two swords from the first: a new character is armed in a zone (ArmNewCharacter); this scene has none, so the sims' kit
            // stands for it there (PactBladeRulesTests.Two_swords_one_for_each_hand), as the Rogue's test does.
            if (session.Zone != null)
            {
                Assert.AreEqual("item.pact_sword", session.Progress.equipment[(int)EquipSlot.MainHand].item, "a sword in the right hand");
                Assert.AreEqual("item.pact_second_sword", session.Progress.equipment[(int)EquipSlot.OffHand].item, "and one in the left");
            }
            yield return null;
        }
        [UnityTest] public IEnumerator Measured_Cut_builds_Vital_Aether_and_Aether_Edge_spends_it_all()
        {
            BuyPath("twin-blade"); var e = Foe(); Near(e, 2); yield return null;
            var pool = session.Player.Resource.Pool; pool.SetCurrent(0);
            Assert.IsTrue(session.UseAbility(Measured), "Measured Cut"); Assert.GreaterOrEqual(pool.Current, PactBladeKit.MeasuredAether, "Vital-Aether built");
            yield return Ready();
            if (!e.actor.IsAlive) yield break;
            pool.SetCurrent(90); int before = e.actor.Health.Pool.Current;
            Assert.IsTrue(session.UseAbility(Edge), "Aether Edge");
            Assert.AreEqual(PactBladeKit.MeasuredEndBack, pool.Current, "all spent, and Measured to the End gives 20 back from 90");
            Assert.Less(e.actor.Health.Pool.Current, before, "the cut lands");
        }
        [UnityTest] public IEnumerator The_White_Whirl_cuts_every_foe_near()
        {
            BuyPath("white-whirl");
            var foes = Foes().Take(3).ToList(); if (foes.Count < 2) Assert.Inconclusive("the test scene has too few mobs");
            var at = foes[0].transform.position;
            for (int i = 1; i < foes.Count; i++) Put(foes[i], at + Quaternion.Euler(0, i * 120, 0) * Vector3.forward * 1.5f);
            Near(foes[0], 1.5f); yield return null;
            var before = foes.Select(f => f.actor.Health.Pool.Current).ToList();
            session.Player.Resource.Pool.SetCurrent(60);
            Assert.IsTrue(session.UseAbility(Whirl), "White Whirl"); yield return null;
            for (int i = 0; i < foes.Count; i++) Assert.Less(foes[i].actor.Health.Pool.Current, before[i], "foe " + i + " cut");
        }
        [UnityTest] public IEnumerator Sentinels_Stop_holds_a_foe_where_it_stands()
        {
            Level(10); session.Progress.talents = new List<TalentRank>(); var e = Foe(); Near(e, 2); yield return null;
            session.Player.Resource.Pool.SetCurrent(50);
            Assert.IsTrue(session.UseAbility(Stop), "Sentinel's Stop"); yield return null;
            Assert.IsTrue(e.Rooted || !e.actor.IsAlive, "held where it stands"); Assert.IsTrue(e.Slowed || !e.actor.IsAlive, "and slowed");
        }
        [UnityTest] public IEnumerator Every_ability_casts_at_twenty_with_its_path()
        {
            foreach (var path in new[] { "twin-blade", "white-whirl", "sentinel" })
            {
                BuyPath(path); var e = Foe(); Near(e, 2); yield return null;
                for (int slot = 0; slot < session.Kit.ActionCount; slot++)
                {
                    if (session.Kit.ActionLockLabel(slot) != null) continue;
                    var id = session.Kit.ActionAt(slot).id;
                    yield return Ready();
                    // Each cast set up on its own, after the wait (the foe walks, the pool drains out of a fight): a living foe selected
                    // and in reach (8 m off for the stride, which wants ground to close), a full pool and full health.
                    if (!e.actor.IsAlive) e = Foe();
                    Near(e, id == "pactblade.minimum_movement" ? 8 : 1.8f); yield return null;
                    session.Player.Resource.Pool.SetCurrent(100); session.Player.Health.Pool.Fill();
                    bool used = session.UseAbility(slot);
                    string why = " (cooldown " + session.CooldownRemaining(slot).ToString("0.00") + " s, " + session.Distance(e).ToString("0.0") + " m off, Aether " + Kit.Aether
                        + ", foe alive " + e.actor.IsAlive + ", last line: " + (session.Messages.Count > 0 ? session.Messages[session.Messages.Count - 1] : "none") + ")";
                    if (id == "pactblade.answering_cut") Assert.AreEqual(Kit.CanAnswer, used, "the Answering Cut only after a blow on you" + why);
                    else Assert.IsTrue(used, path + ": " + id + " casts" + why);
                    if (id == "pactblade.minimum_movement") Assert.Less(Vector3.Distance(session.Player.transform.position, e.transform.position), 4, "the stride closes the ground");
                }
            }
        }
    }
}
#endif
