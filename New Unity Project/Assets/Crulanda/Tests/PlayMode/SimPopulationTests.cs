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
    /// <summary>The sims in the zone (Phase 5.2 round 1): those of Oakhaven who are online stand there as figures, go when they log off, and their places are saved with the character.</summary>
    public class SimPopulationTests
    {
        string root; EncounterSession session; float hourWas;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-sims-" + Guid.NewGuid().ToString("N"));
            hourWas = WorldClock.Hour;
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);   // the zone scene (PlayableEncounter is the old test map, no zone, no sims)
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(SimPopulation.Active, "The sims are in the world.");
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            WorldClock.Hour = hourWas;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [UnityTest] public IEnumerator Oakhavens_online_sims_stand_in_the_village_and_go_when_they_log_off()
        {
            var pop = SimPopulation.Active; var world = pop.World;
            Assert.AreEqual(SimRoster.Count, world.sims.Count);
            Assert.IsTrue(File.Exists(Path.Combine(root, "world.save.json")), "the world slot is written beside the character's");
            var local = world.sims.Where(s => s.zone == "zone.oakhaven").ToList(); Assert.GreaterOrEqual(local.Count, 6);
            // An hour when one of them is on and another off.
            var on = local[0]; WorldClock.Hour = Mathf.Repeat(on.onlineFrom + 1, 24); pop.Refresh(); yield return null;
            var f = pop.Find(on.id); Assert.NotNull(f, on.name + " is here at " + WorldClock.Hour);
            Assert.NotNull(f.visual); Assert.AreEqual(EncounterSession.LookForClass(on.classId), f.visual.Look);
            Assert.NotNull(f.GetComponent<UnityEngine.AI.NavMeshAgent>()); Assert.IsTrue(f.GetComponent<UnityEngine.AI.NavMeshAgent>().isOnNavMesh, "on the NavMesh");
            foreach (var s in local) if (s.IsOnlineAt(WorldClock.Hour)) Assert.NotNull(pop.Find(s.id), s.name + " online but not here"); else Assert.IsNull(pop.Find(s.id), s.name + " offline but here");
            foreach (var s in world.sims.Where(s => s.zone != "zone.oakhaven")) Assert.IsNull(pop.Find(s.id), s.name + " is in another zone");
            // Logging off: the hour past their window.
            if (on.onlineHours < 24)
            {
                WorldClock.Hour = Mathf.Repeat(on.onlineFrom + on.onlineHours + .5f, 24); pop.Refresh(); yield return null;
                Assert.IsNull(pop.Find(on.id), on.name + " has logged off");
            }
        }

        [UnityTest] public IEnumerator Their_places_are_saved_with_the_character()
        {
            var pop = SimPopulation.Active; var local = pop.World.sims.First(s => s.zone == "zone.oakhaven");
            WorldClock.Hour = Mathf.Repeat(local.onlineFrom + 1, 24); pop.Refresh(); yield return null;
            var f = pop.Find(local.id); Assert.NotNull(f);
            f.transform.position = new Vector3(5, f.transform.position.y, -7);
            session.Save(false);
            var back = SimRoster.Load(root, out var msg); Assert.NotNull(back, msg);
            var saved = back.sims.First(s => s.id == local.id);
            Assert.AreEqual(5, saved.x, .01f); Assert.AreEqual(-7, saved.z, .01f);
        }
    }
}
#endif
