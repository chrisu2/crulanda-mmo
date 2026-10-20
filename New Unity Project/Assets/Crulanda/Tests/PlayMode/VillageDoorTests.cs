#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// The Golden Cask's door on an evening (Phase 8.6 step 2, 2026-10-15; Chris, note 205: "running into each other and sometimes get
    /// stuck or delayed at door of inn..prob need double doors"): twelve villagers sent in to the tables one after another, four sims
    /// in to rest, and the twelve out again to the green, with the rest of the village about its own evening; every one of them
    /// goes through the double door by its lanes, and nobody on the way anywhere stands still for over 4 s within 3 m of it. After
    /// the innkeeper's bedtime one leaf is shut and the lane on the open one serves both ways.
    /// </summary>
    public class VillageDoorTests
    {
        string root; float hourWas;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Open(float hour)
        {
            hourWas = WorldClock.Hour; WorldClock.Hour = hour; SimPopulation.LifeOverride = true;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-door-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            VillageEconomy.ResetAll();
            Time.timeScale = 1; WorldClock.Hour = hourWas; SimPopulation.LifeOverride = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }
        /// <summary>A spot on the navmesh out in the inn's yard: <paramref name="out"/> metres in front of the door, <paramref name="aside"/> along it,
        /// or the nearest in toward the door from which the doorway can be walked to. (2026-10-15: 12 m out the yard reaches into the Great
        /// Oak's bench ring, solid all round, and two of the twelve were stood on the ground inside it, which nobody walks out of: Garet
        /// Moss and Osk Farrow, never in.)</summary>
        static Vector3 Yard(ZoneDoorway way, float @out, float aside)
        {
            var path = new NavMeshPath(); var door = way.Entry(true);
            if (NavMesh.SamplePosition(door, out var dh, 1.5f, NavMesh.AllAreas)) door = dh.position;
            for (float o = @out; o >= 2; o -= .5f)
            {
                var q = way.middle - way.inward * o + way.across * aside;
                if (NavMesh.SamplePosition(q, out var h, 3, NavMesh.AllAreas) && NavMesh.CalculatePath(h.position, door, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete) return h.position;
            }
            var p = way.middle - way.inward * @out + way.across * aside;
            return NavMesh.SamplePosition(p, out var hit, 3, NavMesh.AllAreas) ? hit.position : p;
        }

        [UnityTest, Timeout(400000)] public IEnumerator Twelve_villagers_and_four_sims_pass_the_Casks_door_and_nobody_stands_in_it()
        {
            yield return Open(18.3f);
            var zone = ZoneBuilder.Active; var life = VillageLife.Active; var pop = SimPopulation.Active; var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(life); Assert.NotNull(pop); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            var inn = zone.Inns.FirstOrDefault(i => i.name == "The Golden Cask"); Assert.NotNull(inn, "The Golden Cask is registered.");
            var door = inn.door; var way = door.Way;
            Assert.NotNull(door.hinge2, "A double door: two leaves."); Assert.AreEqual(ZoneBuilder.InnDoorW, way.width, 1e-4f); Assert.IsTrue(door.Open && !door.HalfShut, "Both leaves open in the evening.");
            Assert.GreaterOrEqual(life.Places["inn"].Count, 15, "Its fifteen seats (and stools and bench) are the village's.");
            // Twelve of the village (not the keepers of a post, a coop or a fold, nor the innkeeper), stood out in the yard to begin.
            var folk = life.Villagers.Where(v => !v.Resident && v.Coop == null && v.Fold == null && v.Role != "innkeeper" && !v.Keeper).Take(12).ToList();
            Assert.AreEqual(12, folk.Count, "Twelve villagers.");
            for (int i = 0; i < folk.Count; i++) folk[i].StandAt(Yard(way, 7 + (i % 3) * 2.5f, -6 + (i / 3) * 4), 0);
            // Four sims out in the yard too, online all evening.
            var sims = pop.World.sims.Take(4).ToList(); Assert.AreEqual(4, sims.Count);
            for (int i = 0; i < sims.Count; i++)
            {
                var at = Yard(way, 9, 6 + i * 1.5f); var sd = sims[i];
                sd.zone = zone.Zone.id; sd.onlineFrom = 0; sd.onlineHours = 24; sd.level = 4; sd.x = at.x; sd.z = at.z;
                var old = pop.Find(sd.id); if (old != null) { pop.Figures.Remove(old); UnityEngine.Object.Destroy(old.gameObject); }
            }
            yield return null; pop.Refresh(); yield return null;
            var figures = sims.Select(x => pop.Find(x.id)).ToList(); Assert.IsTrue(figures.All(f => f != null), "The four sims stand in the yard.");

            // Watch the door: who crosses its line each way, and how long anyone on the way somewhere stands still within 3 m of it.
            var side = new Dictionary<object, float>(); var inward = new HashSet<object>(); var outward = new HashSet<object>();
            var still = new Dictionary<object, float>(); var last = new Dictionary<object, Vector3>(); float worst = 0; string worstWho = "";
            void Watch(object who, string name, Vector3 at, bool going)
            {
                float d = Vector3.Dot(at - way.middle, way.inward);
                if (side.TryGetValue(who, out var was)) { if (was < 0 && d >= 0) inward.Add(who); if (was > 0 && d <= 0) outward.Add(who); }
                if (Mathf.Abs(d) > .05f) side[who] = d;
                bool near = Flat(at, way.middle) < 3;
                if (near && going && last.TryGetValue(who, out var prev) && Flat(prev, at) < .1f * Mathf.Max(Time.deltaTime, 1e-3f))
                {
                    still[who] = (still.TryGetValue(who, out var t) ? t : 0) + Time.deltaTime;
                    if (still[who] > worst) { worst = still[who]; worstWho = name + " at " + at.ToString("0.0") + " (" + Flat(at, way.middle).ToString("0.0") + " m from the door)"; }
                }
                else still[who] = 0;
                last[who] = at;
            }
            void WatchAll()
            {
                foreach (var v in life.Villagers) if (v != null && v.Visible) Watch(v, v.Name + " (" + v.Trace + ")", v.transform.position, v.Travelling);
                foreach (var f in pop.Figures) if (f != null && !f.Hidden) Watch(f, f.sim.name + " (" + f.Doings + ")", f.transform.position, f.Walking || f.Route != null);
            }
            // The evening: a villager sent in every second and a half, a sim every three; once all are in, the twelve sent out again.
            float t0 = Time.time, sentIn = 0, sentSims = 0, sentOut = -1;
            while (Time.time - t0 < 200)
            {
                float t = Time.time - t0;
                while (sentIn < folk.Count && t >= sentIn * 1.5f) { Assert.IsTrue(folk[(int)sentIn].SendTo("inn"), folk[(int)sentIn].Name + " goes in to a seat."); sentIn++; }
                while (sentSims < figures.Count && t >= 1 + sentSims * 3) { figures[(int)sentSims].Begin(SimFigure.Doing.Inn); sentSims++; }
                if (sentOut < 0 && folk.All(v => inward.Contains(v)) && t > 25) sentOut = t;
                if (sentOut >= 0) { int n = Mathf.Min(folk.Count, 1 + (int)((t - sentOut) / 1.5f)); for (int i = 0; i < n; i++) if (folk[i].Activity == "inn" && !outward.Contains(folk[i]) && inward.Contains(folk[i])) folk[i].SendTo("green"); }
                WatchAll();
                if (sentOut >= 0 && folk.All(v => outward.Contains(v)) && figures.All(f => f.Hidden)) break;
                yield return null;
            }
            string Tell(IEnumerable<Villager> vs) { return string.Join("; ", vs.Select(v => v.Name + ": " + v.Trace)); }
            Assert.IsTrue(folk.All(v => inward.Contains(v)), "Every one of the twelve went in through the door: missing " + Tell(folk.Where(v => !inward.Contains(v))));
            Assert.IsTrue(folk.All(v => outward.Contains(v)), "and came out through it again: missing " + Tell(folk.Where(v => !outward.Contains(v))));
            Assert.IsTrue(figures.All(f => f.Hidden), "The four sims are in, resting: " + string.Join("; ", figures.Select(f => f.sim.name + " " + f.Doings + " at " + f.transform.position.ToString("0.0"))));
            Assert.IsTrue(figures.All(f => inward.Contains(f) || inn.Contains(f.transform.position) || Flat(f.transform.position, way.middle) < 5), "They went in by the door.");
            Assert.Less(worst, 4, "Nobody on the way anywhere stood still for over 4 s within 3 m of the door; the longest was " + worst.ToString("0.0") + " s: " + worstWho);
        }

        [UnityTest] public IEnumerator After_the_innkeepers_bedtime_one_leaf_is_shut_and_one_lane_serves()
        {
            yield return Open(VillageWork.InnkeeperBed + .1f);
            var inn = ZoneBuilder.Active.Inns.First(i => i.name == "The Golden Cask"); var door = inn.door;
            yield return null;
            Assert.IsTrue(door.Open, "The door stands open,"); Assert.IsTrue(door.HalfShut, "one leaf shut after the innkeeper's bedtime.");
            Assert.IsTrue(door.Way.Single, "One lane, both ways, through the open leaf.");
            Assert.IsTrue(door.blocker.enabled && !door.blocker2.enabled, "The first leaf is shut in its frame (its collider on), the second open.");
            Assert.Greater(Vector3.Dot(door.Way.Entry(true) - door.Way.middle, door.Way.across), 0, "The lane runs through the open (right-hand) leaf.");
            WorldClock.Hour = VillageWork.InnkeeperUp + .1f; yield return null;
            Assert.IsFalse(door.HalfShut, "Both leaves open again once he is up."); Assert.IsFalse(door.Way.Single);
            door.SetOpen(false); Assert.IsFalse(door.Open); Assert.IsTrue(door.blocker.enabled && door.blocker2.enabled, "Shut: both leaves.");
            door.SetOpen(true); Assert.IsTrue(door.Open && !door.HalfShut);
        }
    }
}
#endif
