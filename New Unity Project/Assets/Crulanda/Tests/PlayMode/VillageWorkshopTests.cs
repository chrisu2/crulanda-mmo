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
    /// The trades' workshops in Oakhaven (the leather shop, the drying hut, the Cask's kitchen and bar, the game rack): every place
    /// a villager stands to work there is on the navmesh where the builder put it and can be walked to, and the leatherworker
    /// keeps shop at her own counter, apart from the tannery yard.
    /// </summary>
    public class VillageWorkshopTests
    {
        static readonly string[] Kinds = { "leathershop", "dryhut", "kitchen", "kitchendoor", "bar", "lodge" };
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(float hour)
        {
            WorldClock.Hour = hour;
            if (root == null) { root = Path.Combine(Path.GetTempPath(), "Crulanda-workshop-" + Guid.NewGuid().ToString("N")); SceneManager.sceneLoaded += OnLoaded; }
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; WorldClock.Hour = 8.5f;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
            root = null;
        }
        static float Flat(Vector3 a, Vector3 b) { return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)); }
        static IEnumerator WaitUntil(Func<bool> done, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!done() && Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest] public IEnumerator Every_workshop_stand_is_reachable()
        {
            yield return Load(11);
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var zone = s.Zone; var life = VillageLife.Active;
            Assert.IsNotNull(life, "Oakhaven has village life.");
            Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(zone.Zone.spawns.player), out var start, 2.5f, NavMesh.AllAreas), "The player's start is walkable.");
            var path = new NavMeshPath(); var problems = new System.Collections.Generic.List<string>();
            foreach (var kind in Kinds)
            {
                var stands = zone.Workplaces.Where(w => w.kind == kind).ToList();
                if (stands.Count == 0) { problems.Add("no '" + kind + "' workplace is built"); continue; }
                foreach (var w in stands)
                {
                    string q = w.name + " (" + kind + ") at " + w.stand + ": ";
                    // As VillageLife.FindPlaces samples it; and found on this side of any wall, not snapped through it.
                    if (!NavMesh.SamplePosition(w.stand, out var hit, 1.5f, NavMesh.AllAreas)) { problems.Add(q + "no navmesh within 1.5 m"); continue; }
                    if (Flat(hit.position, w.stand) > .75f) problems.Add(q + "the nearest navmesh is " + Flat(hit.position, w.stand).ToString("0.00") + " m off");
                    if (!NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) problems.Add(q + "can't be walked to from the start");
                    if (!life.Places[kind].Any(p => (p - hit.position).sqrMagnitude < .01f)) problems.Add(q + "is not among the village's '" + kind + "' places");
                    else if (life.WorkplaceAt(hit.position) != w.name) problems.Add(q + "belongs to '" + life.WorkplaceAt(hit.position) + "'");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
            Assert.AreEqual(3, life.Places["kitchendoor"].Count, "Three hand-over spots at the kitchen (the hen-wives come together).");
            Assert.AreEqual(1, life.Places["bar"].Count, "One place behind the bar.");
            Assert.AreEqual(1, life.Places["lodge"].Count, "One place at the game rack.");
            Assert.IsTrue(zone.Doors.Any(d => d.kind == "rooms" && !d.openable && d.name == "The Golden Cask, upstairs"), "The inn has its rooms door.");
            Assert.IsFalse(life.Homes.Any(d => d.kind == "rooms"), "The rooms door is not a house.");
            Assert.IsFalse(life.Villagers.Any(v => v.Home != null && v.Home.kind == "rooms"), "Nobody in Oakhaven lodges behind the rooms door yet.");
            foreach (var name in new[] { "Carder farmhouse", "Crisp cottage" })
            {
                Assert.IsTrue(life.Homes.Any(d => d.name == name), name + " is a house.");
                var door = zone.Doors.Single(d => d.name == name);
                Assert.IsTrue(NavMesh.SamplePosition(door.position, out var dh, 2, NavMesh.AllAreas) && NavMesh.CalculatePath(start.position, dh.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, name + "'s door can be walked to.");
            }
            Assert.IsTrue(zone.Doors.Any(d => d.name == "Tanner house" && !d.openable), "The houses carry their names to their doors (Knock · Tanner house).");
            Assert.IsTrue(zone.Doors.Any(d => d.name == "Carder farmhouse") && zone.Doors.Any(d => d.name == "Crisp cottage"), "The two new houses have doors.");
        }

        [UnityTest] public IEnumerator The_leatherworker_keeps_shop_apart_from_the_tannery_yard()
        {
            // Her two blocks at the counter, by the clock.
            for (float h = 9; h < 18; h += .25f)
            {
                if (h >= 12 && h < 14) continue;
                var shift = VillageWork.ShiftFor("leatherworker", h);
                Assert.IsNotNull(shift, "Somewhere at " + h); CollectionAssert.AreEqual(new[] { "leathershop" }, shift.places, "At her shop at " + h);
            }
            // No errand of hers starts inside a counter block (an errand takes her off her shift).
            foreach (var e in VillageWork.DayFor("leatherworker").errands)
                Assert.IsTrue((e.until <= 9 || e.at >= 12) && (e.until <= 14 || e.at >= 18), "'" + e.id + "' (" + e.at + " to " + e.until + ") keeps clear of the counter's hours.");
            // Late morning too: the hour the old errand to the stall took her from the counter.
            foreach (float hour in new[] { 9.3f, 11.25f, 14.3f })
            {
                yield return Load(hour);
                var zone = ZoneBuilder.Active; var life = VillageLife.Active;
                var shop = zone.Zone.props.Single(p => p != null && p.kind == "leathershop"); var yard = zone.Zone.props.Single(p => p != null && p.kind == "tannery");
                Assert.Greater(Vector2.Distance(shop.at, yard.at), 30, "The shop stands well apart from the tannery yard.");
                var places = life.Places["leathershop"];
                Assert.AreNotSame(life.Places["tannery"], places, "Oakhaven's leatherworker has a shop of her own.");
                Assert.GreaterOrEqual(places.Count, 3);
                foreach (var p in places) { Assert.Less(Vector2.Distance(new Vector2(p.x, p.z), shop.at), 4.5f, "Every shop place is in the shop."); Assert.AreEqual(shop.name, life.WorkplaceAt(p)); }
                var maud = life.Villagers.Find(v => v.Role == "leatherworker");
                Assert.IsNotNull(maud, "Oakhaven has its leatherworker."); Assert.AreEqual("Maud Tanner", maud.Name);
                Time.timeScale = 3;
                Vector3? at = null;
                yield return WaitUntil(() => { at = maud.Visible && maud.Activity == "leathershop" ? places.Cast<Vector3?>().FirstOrDefault(p => Flat(p.Value, maud.transform.position) < 1) : null; return at.HasValue; }, 30);
                Time.timeScale = 1;
                Assert.IsTrue(at.HasValue, "At " + hour + " Maud goes to her shop and works there (now '" + maud.Activity + "', " + Vector2.Distance(new Vector2(maud.transform.position.x, maud.transform.position.z), shop.at).ToString("0") + " m from it).");
                Assert.AreEqual("Tanner's leather shop", life.WorkplaceAt(at.Value));
                Assert.Greater(Vector2.Distance(new Vector2(maud.transform.position.x, maud.transform.position.z), yard.at), 30, "Not in the tannery yard.");
            }
        }
    }
}
#endif
