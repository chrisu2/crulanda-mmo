#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Sims at the training yard (Chris, 2026-10-13: "also have sims wonder into the fighting arena (dummies) to practice and improve their
    /// dps"): the yard's four dummies and four butts are marks a sim can stand at, a blade's sim takes a dummy and a bow's a butt, two sims
    /// never share one, and a sim that practises there is better for it (a little more damage, up to a tenth).
    /// </summary>
    public class SimPracticeTests
    {
        string root; EncounterSession session; float hourWas;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-practice-" + Guid.NewGuid().ToString("N"));
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
            WorldClock.Hour = hourWas; SimPopulation.LifeOverride = null; SimFigure.PracticeSeconds = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        /// <summary>A sim (roster position <paramref name="n"/>), online all day in Oakhaven, standing at a point.</summary>
        SimFigure SimAt(int n, Vector3 at, int level, string classId)
        {
            var pop = SimPopulation.Active; var s = pop.World.sims[n];
            s.zone = "zone.oakhaven"; s.onlineFrom = 0; s.onlineHours = 24; s.level = level; s.classId = classId; s.x = at.x; s.z = at.z; s.practice = 0;
            var old = pop.Find(s.id); if (old != null) { pop.Figures.Remove(old); UnityEngine.Object.Destroy(old.gameObject); }
            pop.Refresh(); var f = pop.Find(s.id); Assert.NotNull(f); return f;
        }

        [Test] public void Practice_adds_up_to_a_tenth_to_a_sims_damage()
        {
            var s = new SimAdventurer();
            Assert.AreEqual(100, s.Practised(100), "unpractised: as it was");
            s.practice = .5f; Assert.AreEqual(105, s.Practised(100));
            s.practice = 1; Assert.AreEqual(110, s.Practised(100)); s.practice = 3; Assert.AreEqual(110, s.Practised(100), "never more than a tenth");
        }

        [UnityTest] public IEnumerator The_yard_has_four_dummies_and_four_butts_to_practise_at_inside_its_fence()
        {
            yield return null;
            var marks = session.Zone.PracticeMarks; Assert.AreEqual(8, marks.Count);
            Assert.AreEqual(4, marks.Count(m => m.ranged), "four butts"); Assert.AreEqual(4, marks.Count(m => !m.ranged), "four dummies");
            var yard = session.Zone.Zone.props.First(p => p.kind == "training_yard");
            var centre = new Vector3(yard.at.x, 0, yard.at.y);
            foreach (var m in marks)
            {
                Assert.IsTrue(NavMesh.SamplePosition(m.stand, out var hit, 1.2f, NavMesh.AllAreas), "a sim can stand at " + m.aim.name + " (" + m.stand + ")");
                var flat = new Vector3(m.stand.x, 0, m.stand.z);
                Assert.Less(Vector3.Distance(flat, centre), 11.5f, "inside the yard");
                Assert.Greater(Vector3.Distance(m.stand, m.aim.position), m.ranged ? 6 : .8f, m.ranged ? "across the yard from its butt" : "a step in front of its dummy");
            }
        }

        [UnityTest] public IEnumerator A_blade_takes_a_dummy_a_bow_takes_a_butt_and_no_two_share_one()
        {
            yield return null;
            var near = session.Zone.PracticeMarks[0].stand + Vector3.right * 2;
            var warrior = SimAt(0, near, 5, "class.warrior"); var ranger = SimAt(1, near, 5, "class.ranger"); yield return null;
            var m1 = warrior.FindMark(); var m2 = ranger.FindMark(); Assert.NotNull(m1); Assert.NotNull(m2);
            Assert.IsFalse(m1.ranged, "a warrior strikes a dummy"); Assert.IsTrue(m2.ranged, "a ranger shoots at a butt");
            warrior.Begin(SimFigure.Doing.Practice, mark: m1); yield return null;
            var other = SimAt(2, near, 5, "class.paladin"); yield return null;
            Assert.AreNotSame(m1, other.FindMark(), "the warrior's dummy is taken");
        }

        [UnityTest] public IEnumerator A_sim_practises_at_the_yard_and_is_better_for_it()
        {
            SimFigure.PracticeSeconds = 16;
            var mark = session.Zone.PracticeMarks.First(m => !m.ranged);
            var f = SimAt(0, mark.stand + (mark.stand - mark.aim.position).normalized * 3, 6, "class.warrior"); yield return null;
            float before = f.sim.practice; Assert.AreEqual(0, before);
            f.Begin(SimFigure.Doing.Practice, mark: mark);
            StringAssert.Contains("practising", f.Doings);
            float t = 0; bool stood = false;
            while (t < 40 && f.Activity == SimFigure.Doing.Practice) { t += Time.deltaTime; var at = f.transform.position; if (new Vector2(at.x - mark.stand.x, at.z - mark.stand.z).magnitude < 1.5f) stood = true; yield return null; }
            Assert.IsTrue(stood, "it went to its dummy");
            Assert.AreNotEqual(SimFigure.Doing.Practice, f.Activity, "it finished and chose something else");
            Assert.Greater(f.sim.practice, before, "a little better for the session");
            Assert.Greater(f.sim.Practised(100), 100);
        }
    }
}
#endif
