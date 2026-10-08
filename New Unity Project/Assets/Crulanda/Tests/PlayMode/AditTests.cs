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
    /// The Sealed Adit, the dungeon under the Shattered Peaks (Docs/DUNGEON_DESIGN.md, step D1): its own zone, a network of caves (the
    /// main way down to the Rail Hall and three branches off the Singing Gallery, each opening through the Gallery's wall), every
    /// camp on its own cave's floor and reachable on foot from the yard, the Peaks' way in and the cut back up, and a sim's run
    /// through all of it in order.
    /// </summary>
    public class AditTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-adit-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.adit";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        const string Main = "The Sealed Adit";
        static readonly string[] Branches = { "The Geode Floor", "The Ember Vent", "The Grey Breach" };
        static Hollow Cave(string name) { return Hollow.All.Find(h => h.Name == name); }

        [UnityTest] public IEnumerator The_network_is_built_and_every_camp_is_walkable_from_the_yard()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.adit", zone.Zone.id); Assert.IsTrue(zone.Zone.dungeon, "a dungeon");
            var main = Cave(Main); Assert.NotNull(main, "the main way"); Assert.IsNull(main.Parent);
            foreach (var b in Branches) { var h = Cave(b); Assert.NotNull(h, b); Assert.AreSame(main, h.Parent, b + " opens out of the Gallery"); }
            Assert.Less(main.Centre[main.Centre.Count - 1].y, main.Centre[0].y - 35, "the Rail Hall lies deep under the Peaks");
            Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(zone.Zone.spawns.player, .2f), out var start, 2.5f, NavMesh.AllAreas), "the yard is on the navmesh");
            var path = new NavMeshPath();
            foreach (var cp in zone.Zone.camps)
            {
                var h = Cave(cp.cave); Assert.NotNull(h, cp.name + " names its cave");
                Assert.IsTrue(h.FloorAt(cp.center, out float y), cp.name + " stands on " + cp.cave + "'s floor");
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(cp.center.x, y + .2f, cp.center.y), out var at, 2, NavMesh.AllAreas), cp.name + " is on the navmesh");
                Assert.IsTrue(NavMesh.CalculatePath(start.position, at.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, cp.name + " can be walked to from the yard");
            }
            yield return null;
        }
        [UnityTest] public IEnumerator Each_branch_opens_through_the_gallery_wall()
        {
            yield return null;
            foreach (var b in Branches)
            {
                var h = Cave(b);
                // From in the Gallery, just inside the branch's mouth, straight down the branch: nothing of the Gallery's wall in the way.
                var from = h.At(1) + Vector3.up * 1.4f; var to = h.At(10) + Vector3.up * 1.4f;
                Assert.IsFalse(Physics.Linecast(from, to, out var hit, ~0, QueryTriggerInteraction.Ignore), b + ": the way in is open (hit " + (hit.collider != null ? hit.collider.name : "") + " at " + hit.point + ")");
                Assert.IsTrue(Cave(Main).Open(h.At(1) + Vector3.up * 1.4f), b + "'s mouth is in the Gallery's air");
            }
        }
        [UnityTest] public IEnumerator Every_camp_is_spawned_where_it_stands()
        {
            yield return null; var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            foreach (var cp in ZoneBuilder.Active.Zone.camps)
                Assert.IsTrue(session.Enemies.Exists(e => e != null && e.Camp && Vector2.Distance(new Vector2(e.transform.position.x, e.transform.position.z), cp.center) < cp.radius + 2.5f), cp.name + " has its mobs");
            Assert.IsTrue(session.Enemies.Exists(e => e != null && e.Elite && e.actor.DisplayName.Contains("Orsk Danner")), "the Rail-Captain waits at the end");
        }
        [UnityTest] public IEnumerator A_sim_leads_the_whole_adit_in_order()
        {
            yield return null; var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var pop = SimPopulation.Active; var s = pop.World.sims.First();
            s.zone = "zone.adit"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = 9; session.Progress.experience = EncounterProgress.XpForLevel(8);
            var p = session.Player.transform.position; s.x = p.x + 2; s.z = p.z; pop.Refresh(); yield return null;
            Assert.IsTrue(session.Invite(s.id, true)); var c = session.PartySim(s.id);
            Assert.IsNotNull(c.LeadDungeon(out var why), why); Assert.AreEqual("The Sealed Adit", c.Dungeon);
            Assert.AreEqual(ZoneBuilder.Active.Zone.camps.Length, c.CampsLeft, "every camp on the way");
            Assert.AreEqual("Adit pickets", c.LeadingName, "the first in from the mouth");
        }
        [UnityTest] public IEnumerator The_peaks_lead_in_and_the_cut_leads_back()
        {
            yield return null; var zone = ZoneBuilder.Active;
            var peaks = zone.FindZone("zone.peaks"); Assert.NotNull(peaks);
            var into = peaks.exits.FirstOrDefault(e => e.to == "zone.adit"); Assert.NotNull(into, "the Peaks' way in at the Sealed Adit");
            Assert.IsTrue(zone.Zone.exits.Any(e => e.to == "zone.peaks"), "the cut back up");
            Assert.IsTrue(SimPopulation.Dungeon(zone, "zone.adit"), "the sims keep out of it on their own");
            Assert.Less(Vector2.Distance(into.arrive, zone.Zone.spawns.player), 6, "you arrive in the yard");
        }
    }
}
#endif
