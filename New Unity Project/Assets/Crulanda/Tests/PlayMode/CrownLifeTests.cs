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
    /// The Crown's working life, built and run (p9-cityinn; Chris, 2026-10-16: "dont need the same exact inn in the castle. needs a
    /// redesign", "will need lots more npcs in the castle with real jobs like in oakhaven"):
    /// - the Gilded Lantern is built as the city inn and registered (ZoneInn "city", staffed): its double door, its seats, stools and
    ///   bench inside its walls and on the navmesh, reached from the Gilt Approach; the gallery walked up to by its stair; the bar, the
    ///   kitchen, the hatch and the servers' places among the city's places;
    /// - every place the city's trades are sent to has spots, and the people go about a simulated day (dawn to after dusk, the clock
    ///   stepped through it) without standing stuck: nobody on the way somewhere for a whole stretch without getting anywhere, most
    ///   reach their own trade's places, the carriers carry, and at night most are home or at the inn.
    /// The data side (the plan, the households, the workplaces, the frames) is CrownZoneTests.
    /// </summary>
    public class CrownLifeTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(float hour)
        {
            WorldClock.Hour = hour; ZoneBuilder.RequestedZoneId = "zone.crown";
            root = Path.Combine(Path.GetTempPath(), "Crulanda-crownlife-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            VillageEconomy.ResetAll();
            Time.timeScale = 1; WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static Vector3 Start(ZoneBuilder zone) { Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(zone.Zone.spawns.player), out var hit, 2.5f, NavMesh.AllAreas)); return hit.position; }

        [UnityTest] public IEnumerator The_Gilded_Lantern_is_built_as_the_city_inn_and_its_seats_are_reached()
        {
            yield return Load(11);
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.crown", zone.Zone.id); var start = Start(zone);
            var inn = zone.Inns.SingleOrDefault(i => i.name == "The Gilded Lantern"); Assert.IsNotNull(inn, "The Lantern is registered for the city's life and the sims.");
            Assert.AreEqual("city", inn.style); Assert.IsTrue(inn.Staffed, "A bar, a cask and an innkeeper.");
            Assert.IsNotNull(inn.door.hinge2, "A double door."); Assert.Greater(inn.door.Way.walkable, 2, "The grand door: two pass with room to spare.");
            var problems = new List<string>();
            foreach (var (what, list) in new[] { ("seat", inn.seats), ("stool", inn.bar), ("bench seat", inn.bench) })
                foreach (var s in list)
                {
                    if (!inn.Contains(s.at, .2f)) problems.Add("a " + what + " at " + s.at + " is outside the walls");
                    if (!NavMesh.SamplePosition(s.at, out var hit, 1.2f, NavMesh.AllAreas)) { problems.Add("a " + what + " at " + s.at + " is off the navmesh"); continue; }
                    if (!NavReach.Walkable(start, hit.position)) problems.Add("a " + what + " at " + s.at + " can't be walked to from the Gilt Approach");
                }
            Assert.GreaterOrEqual(inn.seats.Count, 28); Assert.GreaterOrEqual(inn.bar.Count, 6); Assert.AreEqual(2, inn.bench.Count);
            // Step-free through the door: the floor just outside and just inside are one walk.
            var way = inn.door.Way;
            foreach (var (what, at) in new[] { ("outside the door", way.middle - way.inward * 1.5f), ("inside the door", way.middle + way.inward * 1.5f) })
                if (!NavMesh.SamplePosition(at, out var hit, .8f, NavMesh.AllAreas) || !NavReach.Walkable(start, hit.position)) problems.Add("no way " + what);
            // The gallery, up the stair: its floor over the +x end is walked to.
            var plan = ZoneBuilder.InnPlan.For(new Vector2(22, 16), 3);
            var gallery = inn.frame.TransformPoint(new Vector3(plan.Inner.x - plan.galleryDepth / 2, plan.galleryY, 0));
            if (!NavMesh.SamplePosition(gallery, out var up, 1, NavMesh.AllAreas) || up.position.y < gallery.y - .5f) problems.Add("the gallery has no floor in the navmesh");
            else if (!NavReach.Walkable(start, up.position)) problems.Add("the gallery can't be walked to up the stair");
            var life = VillageLife.Active; Assert.IsNotNull(life);
            foreach (var key in new[] { "bar", "kitchen", "kitchendoor", "serve" })
                if (!life.Places.TryGetValue(key, out var spots) || spots.Count == 0) problems.Add("the city has no '" + key + "' place (the Lantern's)");
                else if (!spots.Any(p => inn.Contains(p))) problems.Add("the '" + key + "' place is not in the Lantern");
            Assert.GreaterOrEqual(life.Places["inn"].Count, 28, "Its seats are the city's 'inn' places.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [UnityTest] public IEnumerator The_city_folk_go_about_their_day_without_standing_stuck()
        {
            yield return Load(6.4f);
            var life = VillageLife.Active; Assert.IsNotNull(life); var zone = ZoneBuilder.Active;
            var folk = life.Villagers.Where(v => VillageWork.CityRoles.Contains(v.Role) && !v.Resident).ToList();
            Assert.GreaterOrEqual(folk.Count, 30, "Thirty or more of the Crown's people at work.");
            var problems = new List<string>();
            // Every place their days send them has spots in the built city.
            foreach (var role in folk.Select(v => v.Role).Distinct())
                foreach (var place in VillageWork.PlacesOf(role))
                    if (!life.Places.TryGetValue(place, out var spots) || spots.Count == 0) problems.Add(role + " is sent to '" + place + "', which has no spot");
            Assert.IsEmpty(problems, string.Join("\n", problems));
            // The day: the clock stepped through it, each stretch run at four times speed; each person watched four times a stretch.
            Time.timeScale = 4;
            var own = folk.ToDictionary(v => v, v => new HashSet<string>(VillageWork.PlacesOf(v.Role)));
            var atWork = new HashSet<Villager>(); var loads = new HashSet<Load>(); var stuck = new List<string>();
            foreach (float hour in new[] { 7f, 8.5f, 10f, 11.5f, 13f, 14.5f, 16f, 17.5f, 19f, 20.2f, 21.5f })
            {
                WorldClock.Hour = hour;
                var from = folk.ToDictionary(v => v, v => v.transform.position); var travelling = folk.ToDictionary(v => v, v => 0);
                for (int look = 0; look < 4; look++)
                {
                    float end = Time.realtimeSinceStartup + 2.5f;
                    while (Time.realtimeSinceStartup < end) yield return null;
                    foreach (var v in folk)
                    {
                        if (v.Visible && v.Travelling) travelling[v]++;
                        if (v.Carrying) loads.Add(v.Carried);
                        if (v.Visible && !v.Travelling && (own[v].Contains(v.Activity) || v.Errand != null)) atWork.Add(v);
                    }
                }
                foreach (var v in folk)
                    if (travelling[v] == 4 && Vector3.Distance(from[v], v.transform.position) < .5f)
                        stuck.Add(v.Name + " (" + v.Role + ") stood stuck at " + v.transform.position + " on the way to '" + v.Activity + "' at " + hour.ToString("0.0"));
            }
            Assert.IsEmpty(stuck, stuck.Count + " stuck:\n" + string.Join("\n", stuck.Take(15)));
            Assert.GreaterOrEqual(atWork.Count, folk.Count * 8 / 10, "Most reach their own trade's places in the day (" + atWork.Count + " of " + folk.Count + "); never: " + string.Join(", ", folk.Where(v => !atWork.Contains(v)).Select(v => v.Name + " (" + v.Role + ")")));
            Assert.GreaterOrEqual(loads.Count, 3, "The carriers carry their loads about the city (seen: " + string.Join(", ", loads) + ").");
            // Night: most are home (gone in at a gilt house's door) or at the inn; the night guard keeps the vault.
            WorldClock.Hour = 23.2f;
            float until = Time.realtimeSinceStartup + 25;
            while (Time.realtimeSinceStartup < until && folk.Count(v => v.Indoors || zone.Inns.Any(i => i.Contains(v.transform.position))) < folk.Count * 7 / 10) yield return null;
            int settled = folk.Count(v => v.Indoors || zone.Inns.Any(i => i.Contains(v.transform.position)));
            Assert.GreaterOrEqual(settled, folk.Count * 7 / 10, "By night most are home or at the Lantern (" + settled + " of " + folk.Count + ").");
            var guard = folk.FirstOrDefault(v => v.Role == "nightguard");
            if (guard != null) Assert.IsTrue(guard.Visible && !guard.Indoors, "The night guard is out at the vault, not abed.");
        }
    }
}
#endif
