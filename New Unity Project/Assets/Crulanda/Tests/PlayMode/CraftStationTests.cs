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
    /// Making things at a station in Oakhaven, in the running game (BUILD_PLAN step 9): chop a windfall and burn the logs to
    /// charcoal at Vell's smithy at night with nobody there (E offers "Work at the forge" and opens the Trades window at Woodcutting's
    /// recipes; Make and Make all go on the work bar; the charcoal is in the bags and the save), and ten metres off it is refused. The
    /// Golden Cask's hearth and the Cask's kitchen are fires to make it at.
    /// </summary>
    public class CraftStationTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Open(float hour)
        {
            WorldClock.Hour = hour; EncounterSession.ForgetRestingNodes(); ZoneBuilder.RequestedZoneId = null;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-craft-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; WorldClock.Hour = 8.5f; EncounterSession.ForgetRestingNodes();
            EncounterHud.TradesPage = null; EncounterHud.TradesRecipes = false; EncounterHud.TradesRecipe = null; EncounterHud.TradesCanMakeOnly = false;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static float Flat(Vector3 a, Vector3 b) { return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)); }
        static void Hatchet(EncounterSession s)
        {
            Assert.AreEqual(0, Inventory.Add(s.Progress, s.Items, "tool.hatchet", 1));
            Assert.IsTrue(s.EquipFromBag(s.Progress.bag.FindIndex(x => x.item == "tool.hatchet")), "The hatchet hangs at the belt.");
        }
        /// <summary>Puts the player at a spot (ground, or the floor under it) with nobody near enough to talk to or to count as there.</summary>
        static IEnumerator StandAt(EncounterSession s, Vector3 ground)
        {
            s.Player.GetComponent<AdventurerMotor>().Teleport(ground + Vector3.up * 1.1f);
            if (VillageLife.Active != null) foreach (var v in VillageLife.Active.Villagers) if (Flat(v.transform.position, ground) < 10) v.Park();
            s.SelectFriendly(null, false);
            for (int i = 0; i < 3; i++) yield return null;
        }

        [UnityTest] public IEnumerator Charcoal_AtVellsSmithy_Works_TenMetresAway_Refused()
        {
            yield return Open(23.5f);
            var s = Session(); var p = s.Progress; var zone = s.Zone;
            Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            Assert.NotNull(s.Professions, "The trades' content loaded.");
            var oak = s.Professions.Db.Recipe("recipe.charcoal_oak"); Assert.NotNull(oak);
            // The smithy is a forge: one station, at its anvil.
            var forge = zone.Stations.Where(x => x.kind == "forge").ToList();
            Assert.AreEqual(1, forge.Count, "One forge in Oakhaven, however many places Vell works at it.");
            Assert.AreEqual("Vell's smithy", forge[0].name);
            var anvil = zone.Workplaces.First(w => w.kind == "forge" && w.name == "Vell's smithy");
            Assert.Less(Flat(forge[0].position, anvil.look), .01f, "The station is the anvil.");
            var smithy = forge[0].root;
            Assert.IsTrue(forge[0].Reaches(anvil.stand), "Worked from the front.");
            Assert.IsFalse(forge[0].Reaches(smithy.TransformPoint(new Vector3(-.6f, 0, 2.7f))), "Not from behind the back wall.");
            Assert.IsFalse(forge[0].Reaches(smithy.TransformPoint(new Vector3(3, 0, .5f))), "Not through the stone end wall.");
            // Chop a windfall.
            Hatchet(s);
            var windfall = zone.Interactables.Where(i => i.node == "node.oak").OrderBy(i => Flat(i.position, anvil.stand)).First();
            Assert.IsTrue(s.GatherNow(windfall), "Logs from the windfall.");
            int logs = Inventory.Count(p, "mat.oak_log"); Assert.That(logs, Is.InRange(1, 3));
            int wood = s.Professions.Skill("woodcutting"); Assert.AreEqual(2, wood);
            Inventory.Add(p, s.Items, "mat.oak_log", 3); logs += 3;
            // At the anvil at night, with nobody there.
            yield return StandAt(s, anvil.stand);
            Assert.Greater(WorldClock.Darkness, .5f, "Night.");
            Assert.IsFalse(VillageLife.Active.Villagers.Any(v => v.Visible && Flat(v.transform.position, anvil.stand) < 8), "Nobody at the smithy.");
            Assert.AreSame(forge[0], s.StationNear("forge")); Assert.AreSame(forge[0], s.StationNear(null)); Assert.IsNull(s.StationNear("bench"));
            Assert.AreEqual("Work at the forge", s.InteractPrompt);
            s.Interact();
            Assert.IsTrue(s.TradesOpen, "E at the forge opens the Trades window..."); Assert.IsTrue(s.InventoryOpen, "...beside the bags...");
            Assert.AreEqual("woodcutting", EncounterHud.TradesPage, "...on Woodcutting, whose charcoal is made at a forge..."); Assert.IsTrue(EncounterHud.TradesRecipes, "...at its recipes.");
            Assert.AreSame(forge[0], s.StationFor(oak));
            Assert.IsTrue(s.CanCraft(oak, out var why), why);
            // Make one: two seconds on the work bar.
            Assert.IsTrue(s.Make(oak, 1));
            Assert.IsTrue(s.Working); Assert.AreEqual(oak.name, s.PlayerCastName);
            Assert.AreEqual(0, Inventory.Count(p, "mat.charcoal"), "Nothing until the work is done.");
            yield return new WaitForSeconds(2.5f);
            Assert.IsFalse(s.Working);
            Assert.AreEqual(1, Inventory.Count(p, "mat.charcoal")); Assert.AreEqual(logs - 1, Inventory.Count(p, "mat.oak_log"));
            Assert.Contains("You make Charcoal.", s.Messages);
            Assert.AreEqual(wood + 1, s.Professions.Skill("woodcutting"), "Burning charcoal at Woodcutting 2 teaches every time.");
            // Make all: the rest of the logs, one after another.
            int rest = Inventory.Count(p, "mat.oak_log"); Assert.AreEqual(rest, s.Professions.CanMake(oak));
            Assert.IsTrue(s.Make(oak, rest));
            yield return new WaitForSeconds(rest * EncounterSession.CraftSeconds + 1);
            Assert.IsFalse(s.Working);
            Assert.AreEqual(0, Inventory.Count(p, "mat.oak_log"), "Every log burnt."); Assert.AreEqual(logs, Inventory.Count(p, "mat.charcoal"), "A charcoal for each.");
            // Saved.
            string text = File.ReadAllText(Path.Combine(root, EncounterSave.SlotFor(s.ClassDef.id) + ".save.json"));
            StringAssert.Contains("mat.charcoal", text);
            // With no logs left, Make is refused and says why.
            Assert.IsFalse(s.Make(oak, 1)); Assert.Contains("You need Harrow oak log.", s.Messages); Assert.IsFalse(s.Working);
            // Ten metres off: no forge, no fire, and nothing is made.
            Inventory.Add(p, s.Items, "mat.oak_log", 1);
            Vector3? away = null;
            for (int k = 0; k < 8 && away == null; k++)
            {
                var c = forge[0].position + Quaternion.Euler(0, k * 45, 0) * Vector3.forward * 10;
                if (!NavMesh.SamplePosition(c, out var hit, 1.5f, NavMesh.AllAreas) || Math.Abs(Flat(hit.position, forge[0].position) - 10) > 1.2f) continue;
                if (zone.Stations.Any(x => Flat(x.position, hit.position) < EncounterSession.StationRange + 1)) continue;
                away = hit.position;
            }
            Assert.IsTrue(away.HasValue, "Walkable ground ten metres from the smithy, out of reach of every station.");
            yield return StandAt(s, away.Value);
            Assert.IsNull(s.StationNear("forge")); Assert.IsNull(s.StationNear("fire")); Assert.IsNull(s.StationFor(oak));
            Assert.AreNotEqual("Work at the forge", s.InteractPrompt);
            int charcoal = Inventory.Count(p, "mat.charcoal");
            Assert.IsFalse(s.CanCraft(oak, out why)); Assert.AreEqual("You need a forge or a fire nearby.", why);
            Assert.IsFalse(s.Make(oak, 1)); Assert.IsFalse(s.Working); Assert.Contains("You need a forge or a fire nearby.", s.Messages);
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(charcoal, Inventory.Count(p, "mat.charcoal")); Assert.AreEqual(1, Inventory.Count(p, "mat.oak_log"));
            // Walking off mid-work stops it: back at the anvil, start one and step away.
            yield return StandAt(s, anvil.stand);
            Assert.IsTrue(s.Make(oak, 1)); Assert.IsTrue(s.Working);
            yield return new WaitForSeconds(.5f);
            yield return StandAt(s, anvil.stand + (forge[0].position - anvil.stand).normalized * -.9f);
            Assert.IsFalse(s.Working, "Moving stopped the work."); Assert.Contains(EncounterSession.WorkStoppedLine, s.Messages);
            yield return new WaitForSeconds(2);
            Assert.AreEqual(1, Inventory.Count(p, "mat.oak_log"), "Nothing was burnt."); Assert.AreEqual(charcoal, Inventory.Count(p, "mat.charcoal"));
        }

        [UnityTest] public IEnumerator GoldenCaskHearth_CountsAsFire()
        {
            yield return Open(14);
            var s = Session(); var p = s.Progress; var zone = s.Zone;
            var inn = zone.transform.Find("Zone props/The Golden Cask"); Assert.NotNull(inn, "The Golden Cask is built.");
            var hearth = zone.Stations.Find(x => x.kind == "fire" && x.name == "The Golden Cask"); Assert.NotNull(hearth, "The inn's hearth is a fire.");
            Assert.AreSame(inn, hearth.root);
            var light = inn.Find("Hearth fire"); Assert.NotNull(light);
            Assert.Less(Flat(hearth.position, light.position), .01f, "At the hearth's fire.");
            // In the taproom, a little way in front of the hearth.
            var spot = inn.TransformPoint(new Vector3(3.2f, 0, .8f)); spot.y = inn.position.y;
            yield return StandAt(s, spot);
            // Mira, not yet recruited, waits in the taproom beside this hearth (spawns.companion, about 1.8 m from the spot). E talks to
            // someone at hand before it offers a station, so while she is in reach she is asked first. Send her out to the green
            // (as Recover() does) so that E reaches the fire.
            if (s.CompanionInReach) Assert.AreEqual("Recruit Mira", s.InteractPrompt, "Talking to someone at hand comes before working at a station.");
            s.Companion.GetComponent<NavMeshAgent>().Warp(s.RecoveryPoint + Vector3.right * 2.5f - Vector3.up * .1f);
            yield return null;
            Assert.IsFalse(s.CompanionInReach, "Mira is out of talk range.");
            Assert.AreSame(hearth, s.StationNear("fire")); Assert.IsNull(s.StationNear("forge"));
            bool cooks = s.Professions.Db.RecipesFor("cooking").Count > 0;
            Assert.AreEqual(cooks ? "Cook at the fire" : "Work at the fire", s.InteractPrompt, "Cooking's prompt once it has recipes (step 10); charcoal's until then.");
            Assert.IsTrue(hearth.Reaches(s.Player.transform.position), "The taproom is the hearth's room.");
            Hatchet(s); Inventory.Add(p, s.Items, "mat.oak_log", 1);
            var oak = s.Professions.Db.Recipe("recipe.charcoal_oak");
            Assert.AreSame(hearth, s.StationFor(oak));
            Assert.IsTrue(s.Make(oak, 1)); yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(1, Inventory.Count(p, "mat.charcoal"), "Charcoal burnt at the inn's hearth."); Assert.AreEqual(0, Inventory.Count(p, "mat.oak_log"));
            // Make all stops, and says why, when the next has no room: the charcoal one short of a full stack and every other slot taken.
            Inventory.Add(p, s.Items, "mat.oak_log", 3); Assert.AreEqual(0, Inventory.Add(p, s.Items, "mat.charcoal", 18));
            for (int i = 0; i < Inventory.BagSize; i++) if (p.bag[i].Empty) p.bag[i] = new ItemStack { item = "tool.hatchet", count = 1 };   // tools stack to one
            Assert.AreEqual(1, Inventory.Room(p, s.Items, "mat.charcoal")); Assert.AreEqual(3, s.Professions.CanMake(oak), "Make all offers three.");
            s.Messages.Clear();
            Assert.IsTrue(s.Make(oak, 3)); yield return new WaitForSeconds(EncounterSession.CraftSeconds + .5f);
            Assert.IsFalse(s.Working, "The rest stopped.");
            Assert.AreEqual(20, Inventory.Count(p, "mat.charcoal")); Assert.AreEqual(2, Inventory.Count(p, "mat.oak_log"), "One burnt; two left with nowhere for their charcoal.");
            Assert.Contains(ProfessionLog.BagsFullLine, s.Messages, "Make all says why it stopped.");
            Assert.AreEqual(ProfessionLog.BagsFullLine, s.Messages[s.Messages.Count - 1]);
            // Outside the end wall, a stride from it and near enough to the hearth by distance alone: the wall is between, so no hearth.
            float half = light.localPosition.x + 1.6f; Vector3? outside = null;   // the light is 1.6 m in from the end wall
            foreach (float lz in new[] { .8f, 0, 1.6f, -.8f, 2.4f, -1.6f })
                foreach (float lx in new[] { half + 1, half + 1.4f, half + .7f })
                {
                    if (outside != null || !NavMesh.SamplePosition(inn.TransformPoint(new Vector3(lx, 0, lz)), out var hit, 1.5f, NavMesh.AllAreas)) continue;
                    if (inn.InverseTransformPoint(hit.position).x > half + .3f && Flat(hit.position, hearth.position) < EncounterSession.StationRange - .3f) outside = hit.position;
                }
            Assert.IsTrue(outside.HasValue, "Walkable ground outside the Golden Cask's end wall, within five metres of its hearth.");
            yield return StandAt(s, outside.Value);
            Assert.IsFalse(hearth.Reaches(s.Player.transform.position), "Outside the taproom.");
            Assert.AreNotSame(hearth, s.StationNear("fire"), "The hearth is not worked through the wall."); Assert.AreNotSame(hearth, s.StationNear(null));
            if (s.StationNear(null) == null) Assert.IsFalse(s.InteractPrompt != null && s.InteractPrompt.EndsWith("at the fire"), "No fire offered at a blank wall: " + s.InteractPrompt);
            // Khaven's Cracked Hearth is built by the same Inn(), so it is a fire too, walled in the same way (the stations' placement test checks it there).
        }

        [UnityTest] public IEnumerator CaskKitchen_CountsAsFire()
        {
            yield return Open(2);
            var s = Session(); var p = s.Progress; var zone = s.Zone;
            var kitchen = zone.Stations.Find(x => x.kind == "fire" && x.name == "The Cask's kitchen"); Assert.NotNull(kitchen, "The Cask's kitchen is a fire.");
            var range = zone.Workplaces.First(w => w.kind == "kitchen" && w.name == "The Cask's kitchen");
            Assert.Less(Flat(kitchen.position, range.look), .01f, "The station is the range.");
            Assert.AreEqual(1, zone.Stations.Count(x => x.name == "The Cask's kitchen"), "One station, though the cook has two places and three hand-over spots.");
            // The other fires and the bench: Thorne's oven and Lisbet's drying hut.
            Assert.NotNull(zone.Stations.Find(x => x.kind == "fire" && x.name == "Thorne's bakehouse"), "The bake oven is a fire.");
            Assert.NotNull(zone.Stations.Find(x => x.kind == "bench" && x.name == "Lisbet's drying hut"), "The drying hut is the herbalist's bench.");
            Assert.AreEqual(5, zone.Stations.Count, "Oakhaven: the smithy, the oven, the drying hut, the kitchen and the inn's hearth; none of its own.");
            Assert.IsNull(zone.transform.Find("Zone stations"), "Oakhaven builds no station props.");
            yield return StandAt(s, range.stand);
            Assert.AreSame(kitchen, s.StationNear("fire"), "At the range the kitchen is the nearer fire.");
            Hatchet(s); Inventory.Add(p, s.Items, "mat.oak_log", 2);
            var oak = s.Professions.Db.Recipe("recipe.charcoal_oak");
            s.Interact();
            Assert.IsTrue(s.TradesOpen); Assert.AreEqual(s.StationTrade("fire").id, EncounterHud.TradesPage, "Woodcutting until Cooking has recipes (step 10).");
            Assert.IsTrue(s.Make(oak, 2)); yield return new WaitForSeconds(2 * EncounterSession.CraftSeconds + 1);
            Assert.AreEqual(2, Inventory.Count(p, "mat.charcoal"), "Charcoal burnt at the kitchen range."); Assert.AreEqual(0, Inventory.Count(p, "mat.oak_log"));
            // Walls: the kitchen is open in front and at its post's end, but not worked from behind its stone end wall or from the
            // taproom; the drying hut's bench only from inside the hut; the oven, out in the open, from anywhere in reach.
            Assert.IsTrue(kitchen.Reaches(range.stand));
            var lean = kitchen.root; var size = ZoneBuilder.KitchenSize;
            Assert.IsFalse(kitchen.Reaches(lean.TransformPoint(new Vector3(-size.x / 2 - .6f, 0, -.2f))), "Not from behind the end wall.");
            Assert.IsFalse(kitchen.Reaches(lean.TransformPoint(new Vector3(-.2f, 0, size.y / 2 + .8f))), "Not from the taproom.");
            Assert.IsTrue(kitchen.Reaches(lean.TransformPoint(new Vector3(0, 0, -size.y / 2 - 1.5f))), "From the yard in front.");
            var bench = zone.Stations.Find(x => x.kind == "bench" && x.name == "Lisbet's drying hut"); var hut = bench.root; var hs = ZoneBuilder.DryingHutSize;
            Assert.IsTrue(bench.Reaches(zone.Workplaces.First(w => w.kind == "dryhut" && w.name == "Lisbet's drying hut").stand), "From where Lisbet stands at the bench.");
            Assert.IsFalse(bench.Reaches(hut.TransformPoint(new Vector3(0, 0, hs.y / 2 + .8f))), "Not from behind the hut.");
            Assert.IsFalse(bench.Reaches(hut.TransformPoint(new Vector3(hs.x / 2 + .8f, 0, .5f))), "Not through the wall beside the bench.");
            var oven = zone.Stations.Find(x => x.kind == "fire" && x.name == "Thorne's bakehouse");
            var mouth = zone.Workplaces.First(w => w.kind == "oven" && w.name == "Thorne's bakehouse");
            Assert.IsTrue(oven.Reaches(oven.position + Vector3.forward * 3) && oven.Reaches(oven.position + Vector3.back * 3), "The oven stands in the open.");
            yield return StandAt(s, mouth.stand);
            Assert.AreSame(oven, s.StationNear("fire"), "At the oven's mouth the oven is the fire.");
        }
    }
}
#endif
