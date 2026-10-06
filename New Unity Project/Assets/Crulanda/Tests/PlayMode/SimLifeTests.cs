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
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>Sims living their day (Phase 5.3a): they hunt camp mobs that fight back, a sim's kill is its own, they work nodes, they go into the inn, and the bold choose to hunt.</summary>
    public class SimLifeTests
    {
        string root; EncounterSession session; float hourWas;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-life-" + Guid.NewGuid().ToString("N"));
            hourWas = WorldClock.Hour; SimPopulation.LifeOverride = true;
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(SimPopulation.Active); Assert.IsTrue(SimPopulation.Active.Lively);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            WorldClock.Hour = hourWas; SimPopulation.LifeOverride = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        /// <summary>One sim, online all day in Oakhaven, standing at a point.</summary>
        SimFigure SimAt(Vector3 at, int level, string classId = "class.warrior")
        {
            var pop = SimPopulation.Active; var s = pop.World.sims.First();
            s.zone = "zone.oakhaven"; s.onlineFrom = 0; s.onlineHours = 24; s.level = level; s.classId = classId; s.x = at.x; s.z = at.z;
            var old = pop.Find(s.id); if (old != null) { pop.Figures.Remove(old); UnityEngine.Object.Destroy(old.gameObject); }
            pop.Refresh(); var f = pop.Find(s.id); Assert.NotNull(f); return f;
        }

        [UnityTest] public IEnumerator A_sim_hunts_a_camp_mob_that_fights_back_and_the_kill_is_its_own()
        {
            var e = session.Enemies.Find(x => x != null && x.Camp && !x.Game && x.actor.IsAlive); Assert.NotNull(e, "a camp mob");
            yield return null;
            var f = SimAt(e.transform.position + Vector3.back * 5, e.actor.Level);
            yield return null;
            f.Start(SimFigure.Doing.Hunt, prey: e);
            int hp = e.actor.Health.Pool.Current; float t = 0;
            while (t < 15 && e.Victim != f.actor) { t += Time.deltaTime; yield return null; }
            Assert.AreSame(f.actor, e.Victim, "the mob fights the sim back");
            Assert.AreEqual(f.sim.id, e.TappedBy, "the sim's kill");
            Assert.IsFalse(session.InCombat, "a sim's fight is not yours");
            t = 0; while (t < 6 && e.actor.Health.Pool.Current >= hp) { t += Time.deltaTime; yield return null; }
            Assert.Less(e.actor.Health.Pool.Current, hp, "the sim hurts it");
            StringAssert.Contains("fighting", f.Doings);
            int xp = session.Progress.experience;
            e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 10); yield return null;
            Assert.IsFalse(e.actor.IsAlive);
            Assert.AreEqual(xp, session.Progress.experience, "you get nothing for a sim's kill");
        }

        [UnityTest] public IEnumerator A_sim_works_a_node_and_it_rests()
        {
            var node = session.Zone.Interactables.FirstOrDefault(i => i != null && i.node != null && Time.time >= i.hiddenUntil); Assert.NotNull(node, "a node");
            var f = SimAt(node.position + Vector3.right * 1.2f, 3); yield return null;
            f.Start(SimFigure.Doing.Gather, node: node);
            float t = 0; while (t < 20 && Time.time >= node.hiddenUntil) { t += Time.deltaTime; yield return null; }
            Assert.Greater(node.hiddenUntil, Time.time, "the node rests after the sim works it (" + f.Doings + ")");
        }

        [UnityTest] public IEnumerator A_sim_goes_into_the_inn_and_out_of_reach()
        {
            var inn = session.Zone.Doors.Find(d => d.kind == "inn"); Assert.NotNull(inn, "Oakhaven's inn");
            var f = SimAt(inn.position + Vector3.right * 2.5f, 4); yield return null;
            f.Start(SimFigure.Doing.Inn);
            float t = 0; while (t < 20 && !f.Hidden) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(f.Hidden, "inside");
            Assert.IsNull(SimPopulation.Active.FighterActor(f.sim.id), "no mob can fight one inside");
            StringAssert.Contains("inside the inn", session.InviteRefusal(f.sim));
        }

        [UnityTest] public IEnumerator The_bold_choose_to_hunt()
        {
            var e = session.Enemies.Find(x => x != null && x.Camp && !x.Game && x.actor.IsAlive); Assert.NotNull(e);
            var f = SimAt(e.transform.position + Vector3.back * 8, e.actor.Level); yield return null;
            f.sim.bold = 1; f.sim.chatty = 0;
            f.Choose();
            Assert.AreEqual(SimFigure.Doing.Hunt, f.Activity); Assert.NotNull(f.Quarry);
        }
    }
}
#endif
