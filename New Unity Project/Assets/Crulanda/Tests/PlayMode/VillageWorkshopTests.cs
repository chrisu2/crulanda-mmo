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
    /// keeps shop at her own counter, apart from the tannery yard. Each trade works its own workshop (life.workshops: the three
    /// stall-keepers their own stalls, the herbalist's herbs to her hut); eggs, the first loaves and the hares go round to the
    /// kitchen's back door, where the innkeeper, Hob Linden, answers (and with him away, nobody); and he goes up to bed by 23:30 and
    /// is in the kitchen at first light.
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
            CollectionAssert.AreEqual(new[] { "Hob Linden" }, life.Villagers.Where(v => v.Home != null && v.Home.kind == "rooms").Select(v => v.Name).ToArray(), "Only the innkeeper sleeps behind the rooms door.");
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
                Time.timeScale = 4;   // she may start on the green, 40 m off; a game hour is 25 s at this speed
                Vector3? at = null;
                yield return WaitUntil(() => { at = maud.Visible && maud.Activity == "leathershop" ? places.Cast<Vector3?>().FirstOrDefault(p => Flat(p.Value, maud.transform.position) < 1) : null; return at.HasValue; }, 75);
                Time.timeScale = 1;
                Assert.IsTrue(at.HasValue, "At " + hour + " Maud goes to her shop and works there (now '" + maud.Activity + "', " + Vector2.Distance(new Vector2(maud.transform.position.x, maud.transform.position.z), shop.at).ToString("0") + " m from it).");
                Assert.AreEqual("Tanner's leather shop", life.WorkplaceAt(at.Value));
                Assert.Greater(Vector2.Distance(new Vector2(maud.transform.position.x, maud.transform.position.z), yard.at), 30, "Not in the tannery yard.");
            }
        }

        /// <summary>In bed at home: indoors, and gone in at their own door (not hidden wherever they stood).</summary>
        static bool InBed(Villager v) { return v.Home != null && v.Indoors && Flat(v.transform.position, v.Home.position) < 3.5f; }
        static bool Standing(Villager v) { var a = v.GetComponent<NavMeshAgent>(); return a != null && a.velocity.sqrMagnitude < .05f; }
        /// <summary>The stand point of this kind within a metre of where someone stands, if any.</summary>
        static Vector3? At(VillageLife life, string kind, Vector3 where) { return life.Places[kind].Cast<Vector3?>().FirstOrDefault(p => Flat(p.Value, where) < 1); }

        [UnityTest] public IEnumerator Each_trade_stands_at_its_own_workshop()
        {
            yield return Load(9.6f);
            var zone = ZoneBuilder.Active; var life = VillageLife.Active; var problems = new System.Collections.Generic.List<string>();
            // Every workshop in the zone's data: for each kind of place its prop has, the owner's places are its stand points, all of them theirs.
            Assert.Greater(zone.Zone.life.workshops.Length, 0, "Oakhaven names its workshops.");
            foreach (var w in zone.Zone.life.workshops)
            {
                var v = life.Find(w.who); Assert.IsNotNull(v, w.who + " is in the village.");
                CollectionAssert.Contains(life.WorkshopsOf(w.who), w.prop);
                foreach (var kind in zone.Workplaces.Where(p => p.name == w.prop).Select(p => p.kind).Distinct())
                {
                    var mine = life.PlaceFor(v, kind);
                    if (mine.Count == 0) { problems.Add(w.who + ": no '" + kind + "' place at " + w.prop); continue; }
                    foreach (var p in mine) if (life.WorkplaceAt(p) != w.prop) problems.Add(w.who + "'s '" + kind + "' place at " + p + " belongs to '" + life.WorkplaceAt(p) + "', not " + w.prop);
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
            var stalls = new[] { ("Ama Rusk", "Produce stall"), ("Tamsin Reed", "Cloth and pots"), ("Hedda Thorne", "Bread stall") };
            foreach (var (who, stall) in stalls) { var mine = life.PlaceFor(life.Find(who), "stall"); Assert.AreEqual(1, mine.Count, who + " has one stall."); Assert.AreEqual(stall, life.WorkplaceAt(mine[0])); }
            Assert.AreEqual(life.Places["stall"].Count, life.PlaceFor(life.Find("Lisbet Crane"), "stall").Count, "Someone with no stall of her own may stand at any.");
            // A delivery for the merchants goes to a merchant's stall, never the baker's.
            for (int i = 0; i < 12; i++) { var p = life.WorkedPlace("stall", null, "merchant"); Assert.IsTrue(p.HasValue, "A merchant's stall."); Assert.AreNotEqual("Bread stall", life.WorkplaceAt(p.Value)); }

            // By day Ama, Tamsin and Hedda each come to their own stall, and none of them stands at another's.
            Time.timeScale = 4;
            var seen = new System.Collections.Generic.HashSet<string>();
            yield return WaitUntil(() =>
            {
                foreach (var (who, stall) in stalls)
                {
                    var v = life.Find(who); if (!v.Visible || v.Activity != "stall" || !Standing(v)) continue;
                    var at = At(life, "stall", v.transform.position); if (!at.HasValue) continue;
                    if (life.WorkplaceAt(at.Value) == stall) seen.Add(who); else problems.Add(who + " at " + life.WorkplaceAt(at.Value) + " at " + WorldClock.Hour.ToString("0.00"));
                }
                return seen.Count == stalls.Length;
            }, 90);
            Time.timeScale = 1;
            Assert.IsEmpty(problems, "Nobody keeps another's stall:\n" + string.Join("\n", problems));
            Assert.AreEqual(stalls.Length, seen.Count, "Each keeps her own stall (seen: " + string.Join(", ", seen) + "; " + string.Join(", ", stalls.Select(s => s.Item1 + " '" + life.Find(s.Item1).Activity + "'")) + ").");

            // Evening: Lisbet hangs the day's herbs at her drying hut.
            var lisbet = life.Find("Lisbet Crane"); Vector3? hung = null;
            life.HandedOver += (v, e, taker) => { if (v == lisbet && e.id == "herbs to dry") hung = v.transform.position; };
            // She chooses at once (from where she stands), so the errand's window is not lost to a long walk or a dwell begun
            // before the clock moved (it failed that way once in a full run, never alone).
            WorldClock.Hour = 16.35f; lisbet.Release(lisbet.transform.position); Time.timeScale = 4;
            var trace = new System.Text.StringBuilder(); float nextT = 0;
            yield return WaitUntil(() => { if (Time.time > nextT) { nextT = Time.time + 3; trace.Append(" | " + WorldClock.Hour.ToString("0.00") + " " + lisbet.Activity + " vis=" + lisbet.Visible + " at=" + lisbet.transform.position.ToString("0") + " danger=" + life.Danger(lisbet.transform.position)); } return hung.HasValue; }, 120);
            Time.timeScale = 1;
            Assert.IsTrue(hung.HasValue, "Lisbet brings her herbs in to dry (now '" + lisbet.Activity + "')." + trace);
            var hut = At(life, "dryhut", hung.Value);
            Assert.IsTrue(hut.HasValue, "at a stand point of the drying hut (" + Flat(hung.Value, life.Places["dryhut"][0]).ToString("0.0") + " m from it)");
            Assert.AreEqual("Lisbet's drying hut", life.WorkplaceAt(hut.Value));
            Assert.Greater(life.Count("dryhut.herbs"), 0);
        }

        [UnityTest] public IEnumerator Eggs_loaves_and_hares_are_handed_over_at_the_kitchen()
        {
            yield return Load(8.45f);   // the hen-wives' morning feed is about done and the baker's first loaves are due
            var life = VillageLife.Active; var hob = life.Find("Hob Linden");
            Assert.IsNotNull(hob, "The Golden Cask has its innkeeper."); Assert.AreEqual("innkeeper", hob.Role);
            var doors = life.Places["kitchendoor"]; Assert.AreEqual(3, doors.Count, "Three hand-over spots at the kitchen.");
            var log = new System.Collections.Generic.List<(Villager who, Errand e, Villager taker, Vector3 at, string reply)>();
            life.HandedOver += (v, e, taker) => log.Add((v, e, taker, v.transform.position, taker != null ? taker.Bubble : null));
            foreach (var c in life.Zone.Coops) { c.Eggs = 4; c.FedAt = Time.time; }
            // Hob at the range the whole while, so it is he who answers.
            float rewarp = 0;
            bool Keep() { if (hob.Activity != "kitchen" || Time.realtimeSinceStartup > rewarp) { hob.WorkAt("kitchen"); rewarp = Time.realtimeSinceStartup + 4; } return true; }
            Time.timeScale = 4;
            yield return WaitUntil(() => Keep() && log.Any(x => x.e.id == "eggs to the inn") && log.Any(x => x.e.id == "first loaves to the inn"), 150);
            Time.timeScale = 1; WorldClock.Hour = Mathf.Max(WorldClock.Hour, 12.6f);   // past the hide's window: the hares are next
            var garet = life.Villagers.First(v => v.Role == "hunter"); garet.Release(life.Places["tannery"][0]);   // he chooses now, at the tannery, not whenever his walk ends
            Time.timeScale = 4;
            yield return WaitUntil(() => Keep() && log.Any(x => x.e.id == "hares to the inn"), 150);
            Time.timeScale = 1;
            foreach (var (id, good, carrier) in new[] { ("eggs to the inn", "eggs", "henwife"), ("first loaves to the inn", "bread", "baker"), ("hares to the inn", "meat", "hunter") })
            {
                var handed = log.Where(x => x.e.id == id).ToList();
                Assert.Greater(handed.Count, 0, id + " is handed over (" + string.Join(", ", life.Villagers.Where(v => v.Role == carrier).Select(v => v.Name + ": " + v.Activity)) + ").");
                foreach (var x in handed)
                {
                    Assert.AreEqual(carrier, x.who.Role);
                    var at = At(life, "kitchendoor", x.at);
                    Assert.IsTrue(at.HasValue, x.who.Name + " hands " + good + " over at the kitchen's door (" + Flat(x.at, doors[0]).ToString("0.0") + " m from it).");
                    Assert.AreEqual("The Cask's kitchen", life.WorkplaceAt(at.Value));
                    Assert.AreSame(hob, x.taker, "Hob answers " + x.who.Name + (x.taker != null ? " (not " + x.taker.Name + ")" : " (nobody did)") + ".");
                    CollectionAssert.Contains(new[] { VillageWork.Reply(good, .2f), VillageWork.Reply(good, .8f) }, x.reply, "with a word about the " + good);
                }
                Assert.Greater(life.Count("inn." + good), 0, "The inn's stock of " + good + " is kept (inn." + good + ").");
            }
        }

        [UnityTest] public IEnumerator Nobody_answers_at_the_kitchen_door_while_the_innkeeper_is_away()
        {
            yield return Load(8.45f);
            var life = VillageLife.Active; var hob = life.Find("Hob Linden"); Assert.IsNotNull(hob);
            hob.Park();   // away from the kitchen: the hen-wives stand at the next spots, but none of them answers for him
            var log = new System.Collections.Generic.List<(Villager who, Errand e, Villager taker, Vector3 at)>();
            life.HandedOver += (v, e, taker) => log.Add((v, e, taker, v.transform.position));
            foreach (var c in life.Zone.Coops) { c.Eggs = 4; c.FedAt = Time.time; }
            Time.timeScale = 4;
            yield return WaitUntil(() => log.Any(x => x.e.door == VillageWork.KitchenDoor), 150);
            Time.timeScale = 1;
            var handed = log.Where(x => x.e.door == VillageWork.KitchenDoor).ToList();
            Assert.Greater(handed.Count, 0, "Eggs or loaves come to the kitchen's door (" + string.Join(", ", life.Villagers.Where(v => v.Role == "henwife" || v.Role == "baker").Select(v => v.Name + ": " + v.Activity)) + ").");
            foreach (var x in handed)
            {
                Assert.IsTrue(At(life, "kitchendoor", x.at).HasValue, x.who.Name + " hands " + x.e.good + " over at the kitchen's door.");
                Assert.IsNull(x.taker, "With Hob away nobody answers " + x.who.Name + " at the kitchen's door (not " + x.taker?.Name + ").");
            }
            hob.Release(life.Places["kitchen"][0]);
        }

        [UnityTest] public IEnumerator The_innkeeper_is_abed_by_2330()
        {
            yield return Load(22.4f);
            var life = VillageLife.Active; var hob = life.Find("Hob Linden");
            Assert.IsNotNull(hob); Assert.AreEqual("Innkeeper", hob.Title); Assert.IsFalse(hob.Resident, "He works the inn's day; he keeps no post.");
            var cask = life.HouseholdOf("Hob Linden");
            Assert.IsNotNull(cask); Assert.AreEqual("The Golden Cask", cask.name); Assert.AreSame(hob, cask.Head, "Hob is the head of the house.");
            Assert.AreSame(cask.house, hob.Home); Assert.AreEqual("rooms", hob.Home.kind); Assert.AreEqual("The Golden Cask, upstairs", hob.Home.name);
            CollectionAssert.Contains(cask.members.Select(v => v.Name).ToArray(), "Quill", "Quill lodges at the Cask (and keeps the post in the taproom).");
            Assert.IsTrue(hob.Visible, "Up at 22:24 (now '" + hob.Activity + "').");
            CollectionAssert.Contains(new[] { "bar", "kitchen" }, hob.Activity, "At the bar or in the kitchen in the evening.");
            Time.timeScale = 4;
            yield return WaitUntil(() => InBed(hob) || WorldClock.Hour >= 23.5f || WorldClock.Hour < 22, 60);
            float hour = WorldClock.Hour; Time.timeScale = 1;
            Assert.IsTrue(InBed(hob), "Hob goes up to bed by 23:30 (now " + hour.ToString("0.00") + ", '" + hob.Activity + "', " + Flat(hob.transform.position, hob.Home.position).ToString("0.0") + " m from the rooms door).");
            Assert.Less(hour, 23.5f);
            CollectionAssert.Contains(life.AtHome(hob.Home), hob);
            // And down again at first light, to the kitchen.
            WorldClock.Hour = VillageWork.InnkeeperUp + .2f; Time.timeScale = 4;
            yield return WaitUntil(() => hob.Visible && hob.Activity == "kitchen", 40);
            Time.timeScale = 1;
            Assert.IsTrue(hob.Visible, "Up at first light."); Assert.AreEqual("kitchen", hob.Activity, "The kitchen first.");
        }
    }
}
#endif
