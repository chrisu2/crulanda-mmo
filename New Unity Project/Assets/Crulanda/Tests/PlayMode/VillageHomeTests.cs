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
    /// Households in Oakhaven (tools/wip/professions/ADDENDUM.md A): every villager and hen-wife lives behind their own household's
    /// named door and walks there at bedtime, families share one house, the hunter makes it out to his lodge in time, every home
    /// door can be walked to, and a knock is answered by the household behind the door.
    /// </summary>
    public class VillageHomeTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(float hour)
        {
            WorldClock.Hour = hour;
            if (root == null) { root = Path.Combine(Path.GetTempPath(), "Crulanda-home-" + Guid.NewGuid().ToString("N")); SceneManager.sceneLoaded += OnLoaded; }
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
        /// <summary>In bed at home: indoors, and gone in at their own door (not hidden wherever they stood).</summary>
        static bool InBed(Villager v) { return v.Home != null && v.Indoors && Flat(v.transform.position, v.Home.position) < 3.5f; }
        /// <summary>The player a step out from a door, so it is what E knocks at.</summary>
        static IEnumerator StandAtDoor(EncounterSession s, ZoneDoor door)
        {
            var house = s.Zone.Zone.props.First(p => p != null && p.name == door.name);
            var out2 = new Vector2(door.position.x - house.at.x, door.position.z - house.at.y).normalized;
            s.Player.GetComponent<AdventurerMotor>().Teleport(s.Zone.Ground(new Vector2(door.position.x, door.position.z) + out2, 1.1f));
            yield return null;
        }

        [UnityTest] public IEnumerator Every_villager_sleeps_behind_their_own_named_door()
        {
            yield return Load(19);
            var life = VillageLife.Active; Assert.IsNotNull(life, "Oakhaven has village life.");
            var folk = life.Villagers.Where(v => !v.Resident).ToList();
            Assert.AreEqual(23, folk.Count, "Twenty villagers and three hen-wives.");
            foreach (var v in folk)
            {
                Assert.IsNotNull(v.Household, v.Name + " has a household."); Assert.IsNotNull(v.Home, v.Name + " has a home.");
                Assert.AreSame(v.Household.house, v.Home, v.Name + " lives in the household's house.");
                Assert.AreEqual(v.Household.def.house, v.Home.name, v.Name + "'s door is named for the house.");
                CollectionAssert.Contains(v.Household.members, v);
            }
            Assert.AreEqual(15, life.Households.Count);
            Assert.AreEqual(life.Households.Count, life.Households.Select(h => h.house).Distinct().Count(), "No two households share a door.");
            Assert.IsTrue(life.Households.All(h => h.house != null), "Every house is built and has a door.");
            // Past everyone's bedtime but the drinkers': they walk home from wherever they are and go in at their own door.
            WorldClock.Hour = 21.3f; Time.timeScale = 4;
            var sleepers = folk.Where(v => v.Role != "drinker").ToList();
            yield return WaitUntil(() => sleepers.All(InBed) || WorldClock.Hour >= 23.5f || WorldClock.Hour < 21, 150);
            float hour = WorldClock.Hour; Time.timeScale = 1;
            var missing = sleepers.Where(v => !InBed(v)).Select(v => v.Name + " (" + v.Activity + (v.Visible ? ", up" : ", hidden") + ", " + Flat(v.transform.position, v.Home.position).ToString("0") + " m from " + v.Home.name + ")").ToList();
            Assert.IsEmpty(missing, "By 23:30 (now " + hour.ToString("0.00") + ") everyone is behind their own door:\n" + string.Join("\n", missing));
            foreach (var name in new[] { "Old Tobin", "Jory" }) Assert.AreEqual(name == "Jory" ? "Jory's house" : "Pell house", life.Find(name).Home.name, "The drinkers have homes to go to as well.");
        }

        [UnityTest] public IEnumerator The_Tanners_share_one_house()
        {
            yield return Load(23.5f);
            var life = VillageLife.Active;
            var tanners = life.HouseholdOf("Maud Tanner"); Assert.IsNotNull(tanners); Assert.AreEqual("Tanner", tanners.name);
            CollectionAssert.AreEquivalent(new[] { "Maud Tanner", "Fen Walker", "Nettie" }, tanners.members.Select(v => v.Name).ToArray(), "Maud, Fen and Nettie.");
            Assert.AreEqual("Tanner house", tanners.house.name);
            Assert.AreSame(tanners.members[0], tanners.Head, "Maud is the head of the house.");
            Assert.AreEqual("leatherworker", life.Find("Maud Tanner").Role); Assert.AreEqual("skinner", life.Find("Fen Walker").Role); Assert.AreEqual("child", life.Find("Nettie").Role);
            foreach (var v in tanners.members) { Assert.AreSame(tanners.house, v.Home, v.Name + " lives at the Tanner house."); Assert.AreSame(tanners, v.Household); }
            CollectionAssert.AreEquivalent(tanners.members, life.AtHome(tanners.house), "At night all three are in.");
            Assert.IsFalse(life.Villagers.Any(v => v.Home == tanners.house && !tanners.members.Contains(v)), "Nobody else lives there.");
            // The other families that share.
            CollectionAssert.AreEquivalent(new[] { "Osk Farrow", "Pim" }, life.HouseholdOf("Pim").members.Select(v => v.Name).ToArray());
            CollectionAssert.AreEquivalent(new[] { "Old Tobin", "Edda Pell" }, life.HouseholdOf("Edda Pell").members.Select(v => v.Name).ToArray());
        }

        [UnityTest] public IEnumerator Farmers_live_with_the_hen_wife_at_their_farm()
        {
            yield return Load(11);
            var life = VillageLife.Active;
            foreach (var (house, names) in new[] { ("Carder farmhouse", new[] { "Wil Carder", "Hettie Brook" }), ("Harrow farmhouse", new[] { "Sel Harrow", "Ilse Brandt", "Goody Marl" }), ("Brook farmhouse", new[] { "Grete Lowe", "Nan Pennock" }) })
            {
                var h = life.HouseholdOf(names[0]); Assert.IsNotNull(h, names[0]);
                Assert.AreEqual(house, h.house.name);
                CollectionAssert.AreEquivalent(names, h.members.Select(v => v.Name).ToArray(), house + "'s household.");
                var keeper = h.members.Single(v => v.Role == "henwife");
                Assert.IsNotNull(keeper.Coop);
                Assert.Less(Flat(keeper.Coop.door, h.house.position), 30, keeper.Name + " keeps the hens by her own farmhouse.");
            }
            Assert.IsTrue(life.Villagers.Where(v => v.Role == "farmer").All(v => v.Home.name.EndsWith("farmhouse")), "Every farmer lives at a farm.");
            Assert.AreEqual("Crisp cottage", life.Find("Aldo Crisp").Home.name, "The miller lives in his cottage, not at the mill.");
            Assert.IsFalse(life.Villagers.Any(v => v.Home != null && v.Home.name == "Oak creek mill"), "Nobody sleeps at the mill.");
        }

        [UnityTest] public IEnumerator The_hunter_is_hidden_by_2330_starting_from_the_inn()
        {
            yield return Load(19.5f);
            var life = VillageLife.Active; var garet = life.Find("Garet Moss");
            Assert.IsNotNull(garet); Assert.AreEqual("hunter", garet.Role);
            Assert.AreEqual("Moss's lodge", garet.Home.name); Assert.AreEqual("home", garet.Home.kind, "The lodge is a barn: it has a home door of its own.");
            // At the inn at his bedtime: the furthest walk home he has.
            var seat = life.Places["inn"][0];
            WorldClock.Hour = Villager.HunterBed + .05f;
            garet.StandAt(seat, 0); garet.Release(seat);
            Assert.Less(Flat(garet.transform.position, seat), 2, "He sets off from the inn.");
            Assert.Greater(Flat(seat, garet.Home.position), 150, "A long way out.");
            Time.timeScale = 4;
            yield return WaitUntil(() => InBed(garet) || WorldClock.Hour >= 23.5f || WorldClock.Hour < 19, 120);
            float hour = WorldClock.Hour; Time.timeScale = 1;
            Assert.IsTrue(InBed(garet), "Garet is in at his lodge by 23:30 (now " + hour.ToString("0.00") + ", '" + garet.Activity + "', " + Flat(garet.transform.position, garet.Home.position).ToString("0") + " m from the door).");
            Assert.Less(hour, 23.5f);
        }

        [UnityTest] public IEnumerator Home_doors_are_on_the_navmesh()
        {
            yield return Load(11);
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var zone = s.Zone; var life = VillageLife.Active;
            Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(zone.Zone.spawns.player), out var start, 2.5f, NavMesh.AllAreas), "The player's start is walkable.");
            var path = new NavMeshPath(); var problems = new System.Collections.Generic.List<string>();
            foreach (var h in life.Households)
            {
                if (h.house == null) { problems.Add(h.name + ": no door"); continue; }
                string q = h.name + " (" + h.house.name + ") at " + h.house.position + ": ";
                if (!NavMesh.SamplePosition(h.house.position, out var hit, 2, NavMesh.AllAreas)) { problems.Add(q + "no navmesh within 2 m"); continue; }
                if (Flat(hit.position, h.house.position) > 1.5f) problems.Add(q + "the nearest navmesh is " + Flat(hit.position, h.house.position).ToString("0.00") + " m off");
                if (!NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) problems.Add(q + "can't be walked to from the start");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
            var homeDoors = zone.Doors.Where(d => d.kind == "home").ToList();
            Assert.AreEqual(1, homeDoors.Count, "One barn is lived in."); Assert.AreEqual("Moss's lodge", homeDoors[0].name);
            Assert.IsFalse(homeDoors[0].openable);
        }

        [UnityTest] public IEnumerator Knocking_names_the_household()
        {
            yield return Load(23.5f);
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var life = VillageLife.Active;
            var door = s.Zone.Doors.Single(d => d.name == "Tanner house");
            Assert.AreEqual("Tanner", life.HouseholdAt(door).name);
            // Night: all three abed.
            yield return StandAtDoor(s, door);
            Assert.AreSame(door, s.NearbyDoor); Assert.AreEqual("Knock · Tanner house", s.InteractPrompt);
            s.Interact();
            Assert.AreEqual("Tanner house: The Tanners are abed. A child coughs, and somebody hushes her.", s.Messages.Last());
            // Someone at home with wares for you opens up at the door: Ama Rusk sells from her stall by day.
            var rusk = s.Zone.Doors.Single(d => d.name == "Rusk house");
            Assert.IsTrue(life.AtHome(rusk).Any(v => v.Name == "Ama Rusk"), "Ama is home at night.");
            yield return StandAtDoor(s, rusk);
            s.Interact();
            Assert.Contains("Rusk house: " + EncounterSession.ShutterLine, s.Messages);
            Assert.IsTrue(s.VendorNpc == "Ama Rusk" || (s.Conversation != null && s.Conversation.npc == "Ama Rusk"), "The knock opens Ama's wares (or her quest talk).");
            s.CloseVendor(); s.Conversation = null;
            // A door nobody lives behind keeps the old lines.
            Assert.IsNull(life.KnockLine(s.Zone.Doors.Single(d => d.name == "Oak creek mill")), "The mill has no household.");

            // By day: the Tanners out at work, Nettie in.
            WorldClock.Hour = 10.5f;
            for (int i = 0; i < 3; i++) yield return null;   // up and out of bed
            var maud = life.Find("Maud Tanner"); var fen = life.Find("Fen Walker"); var nettie = life.Find("Nettie");
            Assert.IsTrue(maud.WorkAt("leathershop")); Assert.IsTrue(fen.WorkAt("tannery")); Assert.IsTrue(nettie.GoIndoors());
            Assert.AreEqual("A voice through the planks: \"Maud's at the shop by the South road. Try there.\"", life.KnockLine(door));
            nettie.Release(nettie.transform.position);
            Assert.IsFalse(life.AtHome(door).Any(), "Nobody in.");
            Assert.AreEqual("No answer. The Tanner house is empty till supper.", life.KnockLine(door));
            Assert.IsTrue(maud.GoIndoors());
            Assert.AreEqual("Maud's voice, through the planks: \"Not now. Since the collectors came, this door stays barred.\"", life.KnockLine(door));
            maud.Release(maud.transform.position);
        }
    }
}
#endif
