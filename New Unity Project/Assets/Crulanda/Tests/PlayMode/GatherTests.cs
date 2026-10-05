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
    /// Gathering in Oakhaven, in the running game: a copper seam worked with a pick fills the bags, raises Mining, rests (its ore
    /// gone, its rock still there) and comes back; a rest outlives a zone reload; a Yarrow gives the herb always and the quest's
    /// yarrow while the quest wants it (full bags then refuse only once it is no longer wanted); moving or being hit stops the work,
    /// an ability the kit refuses does not; bags that filled while the work went on leave the node as it was. In the Verdant Shore
    /// a new miner is refused a Veridian seam (a node above the skill refuses; Chris, 2026-10-05).
    /// </summary>
    public class GatherTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Open(float hour = 10, string zone = null)
        {
            WorldClock.Hour = hour; EncounterSession.ForgetRestingNodes();
            root = Path.Combine(Path.GetTempPath(), "Crulanda-gather-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = zone;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; WorldClock.Hour = 8.5f; EncounterSession.ForgetRestingNodes(); ZoneBuilder.RequestedZoneId = null;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static float Flat(Vector3 a, Vector3 b) { return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)); }
        static bool Shown(Transform t) { return t.GetComponentsInChildren<Renderer>(true).All(r => r.enabled); }
        static bool Gone(Transform t) { return t.GetComponentsInChildren<Renderer>(true).All(r => !r.enabled); }
        /// <summary>The outside copper seam nearest the player.</summary>
        static ZoneInteractable Seam(EncounterSession s)
        {
            var p = s.Player.transform.position;
            return s.Zone.Interactables.Where(i => i.node == "node.copper").OrderBy(i => Flat(i.position, p)).First();
        }
        static void Pick(EncounterSession s)
        {
            Assert.AreEqual(0, Inventory.Add(s.Progress, s.Items, "tool.pick", 1));
            Assert.IsTrue(s.EquipFromBag(s.Progress.bag.FindIndex(x => x.item == "tool.pick")), "The pick hangs at the belt.");
        }
        /// <summary>Puts the player on walkable ground within reach of a node (its face first), with nobody near enough to talk to.</summary>
        static IEnumerator StandBy(EncounterSession s, ZoneInteractable node)
        {
            var zone = s.Zone; var at = new Vector2(node.position.x, node.position.z); Vector3? spot = null;
            foreach (float r in new[] { 1.6f, 2.1f, 1.1f })
                for (int k = 0; k < 8 && spot == null; k++)
                {
                    var dir = node.root.rotation * Quaternion.Euler(0, k * 45, 0) * Vector3.back;
                    var c = zone.StandAt(at + new Vector2(dir.x, dir.z) * r, node.position.y);
                    if (NavMesh.SamplePosition(c, out var hit, .8f, NavMesh.AllAreas) && Flat(hit.position, node.position) < EncounterSession.UseRange - .3f && Mathf.Abs(hit.position.y - node.position.y) < 1.5f) spot = hit.position;
                }
            Assert.IsTrue(spot.HasValue, "Walkable ground within reach of " + node.name + " at " + node.position);
            s.Player.GetComponent<AdventurerMotor>().Teleport(spot.Value + Vector3.up * 1.1f);
            if (VillageLife.Active != null)
                foreach (var v in VillageLife.Active.Villagers)
                    if (Vector3.Distance(v.transform.position, spot.Value) < 8) v.StandAt(spot.Value + Vector3.back * 30 + Vector3.right * 2 * VillageLife.Active.Villagers.IndexOf(v), 0);
            s.SelectFriendly(null, false);
            for (int i = 0; i < 3; i++) yield return null;
        }

        [UnityTest] public IEnumerator CopperSeam_WithPick_FillsBag_HidesAndReturns()
        {
            yield return Open();
            var s = Session(); var p = s.Progress; var trades = s.Professions;
            Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            Assert.NotNull(trades, "The trades' content loaded.");
            var seam = Seam(s);
            Assert.AreEqual("Copper seam", seam.name); Assert.AreEqual("Mine the copper seam", seam.prompt); Assert.AreEqual("node", seam.kind);
            Assert.NotNull(seam.part, "A seam's ore is what vanishes."); Assert.AreEqual("full", seam.part.name);
            Assert.IsTrue(Shown(seam.root));
            yield return StandBy(s, seam);
            Assert.AreSame(seam, s.NearbyInteractable); Assert.AreEqual("Mine the copper seam", s.InteractPrompt);
            // No pick: refused, and no work starts.
            s.Interact();
            Assert.IsFalse(s.Working); Assert.Contains("You need a miner's pick. Merchants sell them.", s.Messages);
            // With the pick at the belt: two seconds on the work bar.
            Pick(s);
            s.Interact();
            Assert.IsTrue(s.Working); Assert.IsTrue(s.PlayerCasting); Assert.AreEqual("Mining", s.PlayerCastName);
            Assert.AreEqual(0, Inventory.Count(p, "mat.copper_ore"), "Nothing until the work is done.");
            yield return new WaitForSeconds(2.5f);
            Assert.IsFalse(s.Working, "Done in two seconds.");
            Assert.That(Inventory.Count(p, "mat.copper_ore"), Is.InRange(1, 3), "A copper seam gives one to three.");
            Assert.AreEqual(2, trades.Skill("mining")); Assert.Contains("Mining 2.", s.Messages);
            // It rests: the ore is gone, the rock stays, and E has nothing here.
            Assert.Greater(seam.hiddenUntil, Time.time + 170, "Three minutes' rest.");
            Assert.IsTrue(Gone(seam.part), "Its ore is hidden.");
            Assert.IsTrue(seam.root.GetComponentsInChildren<Renderer>().Any(r => r.enabled && !r.transform.IsChildOf(seam.part)), "Its rock stays.");
            Assert.AreNotSame(seam, s.NearbyInteractable);
            // Saved: the skill and the ore.
            string text = File.ReadAllText(Path.Combine(root, EncounterSave.SlotFor(s.ClassDef.id) + ".save.json"));
            StringAssert.Contains("{\\\"id\\\":\\\"mining\\\",\\\"skill\\\":2}", text, "Mining 2 is saved."); StringAssert.Contains("mat.copper_ore", text);
            // Three minutes on, it is back.
            Time.timeScale = 60; yield return new WaitForSeconds(182); Time.timeScale = 1;
            Assert.LessOrEqual(seam.hiddenUntil, Time.time);
            Assert.IsTrue(Shown(seam.root), "Its ore is back.");
            Assert.AreSame(seam, s.NearbyInteractable, "E offers it again.");
        }

        [UnityTest] public IEnumerator VeridianSeam_NewMiner_IsRefused()
        {
            yield return Open(10, "zone.verdant");
            var s = Session(); var p = s.Progress; var trades = s.Professions;
            Assert.AreEqual("zone.verdant", s.Zone.Zone.id); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            var pos = s.Player.transform.position;
            var seam = s.Zone.Interactables.Where(i => i.node == "node.veridian").OrderBy(i => Flat(i.position, pos)).First();
            Assert.AreEqual("Veridian seam", seam.name); Assert.AreEqual("Mine the Veridian seam", seam.prompt);
            yield return StandBy(s, seam);
            Pick(s);
            Assert.AreEqual(1, trades.Skill("mining"), "A new miner.");
            Assert.AreSame(seam, s.NearbyInteractable);
            s.Interact();
            // A node above the skill refuses (Chris, 2026-10-05): nothing starts, nothing is gathered or learned.
            Assert.IsFalse(s.Working, "Mining 1 can't work a seam of 80."); Assert.Contains("Requires Mining 80.", s.Messages);
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(0, Inventory.Count(p, "mat.veridian_ore")); Assert.AreEqual(1, trades.Skill("mining"));
        }

        [UnityTest] public IEnumerator Respawn_SurvivesZoneReload()
        {
            yield return Open();
            var s = Session(); Pick(s);
            var seam = Seam(s); var other = s.Zone.Interactables.First(i => i.node == "node.copper" && i != seam);
            yield return StandBy(s, seam);
            Assert.IsTrue(s.GatherNow(seam), "Worked: ore in the bags.");
            string key = seam.Key(s.ZoneId); float until = seam.hiddenUntil; string otherKey = other.Key(s.ZoneId);
            Assert.Greater(until, Time.time); Assert.IsTrue(Gone(seam.part));
            // Travel away and back (the scene is built again from scratch): the seam is still resting.
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
            var s2 = Session(); Assert.AreNotSame(s, s2); Assert.AreEqual(root, s2.SaveDirectoryOverride);
            var again = s2.Zone.Interactables.Find(i => i.node != null && i.Key(s2.ZoneId) == key);
            Assert.NotNull(again, "The seam is built where it was.");
            Assert.AreEqual(until, again.hiddenUntil, .05f, "Its rest carries over.");
            Assert.IsTrue(Gone(again.part), "Its ore is still gone.");
            Assert.AreNotSame(again, s2.NearbyInteractable, "E does not offer it.");
            var fresh = s2.Zone.Interactables.Find(i => i.node != null && i.Key(s2.ZoneId) == otherKey);
            Assert.LessOrEqual(fresh.hiddenUntil, Time.time); Assert.IsTrue(Shown(fresh.root), "The other seams were not worked.");
            Assert.AreEqual(2, s2.Professions.Skill("mining"), "The skill was saved.");
        }

        [UnityTest] public IEnumerator Yarrow_GivesBagHerbAlways_AndQuestItemWhenWanted()
        {
            yield return Open();
            var s = Session(); var p = s.Progress; var log = s.Quests; Assert.NotNull(log);
            var yarrows = s.Zone.Interactables.Where(i => i.name == "Yarrow" && i.node == "node.yarrow").ToList();
            Assert.AreEqual(10, yarrows.Count, "The eight Yarrow props and the two new patches.");
            Assert.AreEqual(8, yarrows.Count(y => y.kind == "herb")); Assert.IsTrue(yarrows.All(y => y.item == "item.yarrow"));
            Assert.IsTrue(yarrows.All(y => y.part == null), "A picked herb vanishes whole.");
            // No quest: the herb goes into the bags, and no quest yarrow.
            Assert.IsTrue(s.GatherNow(yarrows[0]));
            int herbs = Inventory.Count(p, "mat.yarrow"); Assert.That(herbs, Is.InRange(1, 2));
            Assert.AreEqual(0, log.ItemCount("item.yarrow")); Assert.AreEqual(2, s.Professions.Skill("herbalism"), "Herbalism 2.");
            Assert.IsTrue(Gone(yarrows[0].root), "Picked, the patch is gone until it grows back.");
            Assert.AreEqual(90, yarrows[0].hiddenUntil - Time.time, 1, "Herbs grow back in a minute and a half.");
            // Lisbet's quest: a Yarrow worked through E and the work bar gives the quest's yarrow as well.
            Assert.IsTrue(log.Accept(log.Def("npc.lisbet.yarrow"), s.ZoneId));
            var next = yarrows[1]; yield return StandBy(s, next);
            Assert.AreSame(next, s.NearbyInteractable); Assert.AreEqual("Gather yarrow", s.InteractPrompt);
            s.Interact();
            Assert.IsTrue(s.Working); Assert.AreEqual("Gathering", s.PlayerCastName);
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(s.Working, "A second and a half.");
            Assert.Greater(Inventory.Count(p, "mat.yarrow"), herbs); Assert.AreEqual(1, log.ItemCount("item.yarrow"), "The quest's yarrow too.");
            // Full bags: while the quest still wants yarrow it is picked all the same.
            for (int i = 0; i < p.bag.Count; i++) { if (p.bag[i].item == "mat.yarrow") p.bag[i].count = 20; else if (p.bag[i].Empty) p.bag[i] = new ItemStack { item = "tool.hatchet", count = 1 }; }
            Assert.AreEqual(0, Inventory.Room(p, s.Items, "mat.yarrow"));
            var third = yarrows[2]; yield return StandBy(s, third);
            s.Interact(); Assert.IsTrue(s.Working, "Wanted by a quest, so full bags don't refuse it.");
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(2, log.ItemCount("item.yarrow"));
            // Once the quest has its five, full bags refuse.
            while (log.ItemCount("item.yarrow") < 5) log.GiveItem("item.yarrow");
            Assert.IsFalse(log.Wants("item.yarrow", "Yarrow"));
            var fourth = yarrows[3]; yield return StandBy(s, fourth);
            s.Interact(); Assert.IsFalse(s.Working); Assert.Contains(ProfessionLog.BagsFullLine, s.Messages);
        }

        [UnityTest] public IEnumerator Moving_CancelsWork()
        {
            yield return Open();
            var s = Session(); var p = s.Progress; Pick(s); var seam = Seam(s); var motor = s.Player.GetComponent<AdventurerMotor>();
            yield return StandBy(s, seam);
            s.Interact(); Assert.IsTrue(s.Working);
            yield return new WaitForSeconds(.6f);
            // A step away stops it: nothing is gathered and the seam does not rest.
            var here = s.Player.transform.position; var away = s.Zone.StandAt(new Vector2(here.x + .8f, here.z + .6f), here.y - 1.1f, 1.1f);
            motor.Teleport(away); yield return null; yield return null;
            Assert.IsFalse(s.Working, "Moving stopped the work."); Assert.Contains(EncounterSession.WorkStoppedLine, s.Messages);
            yield return new WaitForSeconds(2);
            Assert.AreEqual(0, Inventory.Count(p, "mat.copper_ore")); Assert.AreEqual(1, s.Professions.Skill("mining"));
            Assert.LessOrEqual(seam.hiddenUntil, Time.time); Assert.IsTrue(Shown(seam.root), "The seam was not worked.");
            // So does a blow.
            yield return StandBy(s, seam);
            s.Interact(); Assert.IsTrue(s.Working);
            yield return new WaitForSeconds(.5f);
            s.Player.Health.ApplyDamage(5); yield return null; yield return null;
            Assert.IsFalse(s.Working, "Being hit stopped the work.");
            yield return new WaitForSeconds(2);
            Assert.AreEqual(0, Inventory.Count(p, "mat.copper_ore"));
            // Standing still, it finishes.
            s.Interact(); Assert.IsTrue(s.Working);
            yield return new WaitForSeconds(2.5f);
            Assert.IsFalse(s.Working); Assert.Greater(Inventory.Count(p, "mat.copper_ore"), 0);
        }

        [UnityTest] public IEnumerator BagsFilledDuringWork_NodeStays_AndARefusedAbilityLeavesTheWork()
        {
            yield return Open();
            var s = Session(); var p = s.Progress; Pick(s); var seam = Seam(s);
            yield return StandBy(s, seam);
            // An ability the kit refuses (or one that goes off) decides whether the work goes on: only one that is used stops it.
            s.Interact(); Assert.IsTrue(s.Working);
            s.Select(null);
            bool used = s.UseAbility(0);
            if (used) Assert.IsFalse(s.Working, "An ability that goes off stops the work.");
            else { Assert.IsTrue(s.Working, "A refused ability leaves the work alone."); Assert.IsFalse(s.Messages.Contains(EncounterSession.WorkStoppedLine)); }
            s.CancelWork();
            // The bags fill while the work goes on (a corpse looted, something bought): nothing goes in, the seam does not rest, and
            // the game does not save over it.
            for (int i = 0; i < p.bag.Count; i++) if (p.bag[i].Empty) p.bag[i] = new ItemStack { item = "tool.hatchet", count = 1 };
            Assert.AreEqual(0, Inventory.Room(p, s.Items, "mat.copper_ore"), "The bags are full.");
            Assert.IsFalse(s.GatherNow(seam), "Nothing fits.");
            Assert.Contains(ProfessionLog.BagsFullLine, s.Messages);
            Assert.AreEqual(0, Inventory.Count(p, "mat.copper_ore")); Assert.AreEqual(1, s.Professions.Skill("mining"), "No skill for nothing gathered.");
            Assert.LessOrEqual(seam.hiddenUntil, Time.time, "The seam does not rest."); Assert.IsTrue(Shown(seam.root), "Its ore is still there.");
            Assert.AreSame(seam, s.NearbyInteractable, "E still offers it.");
        }
    }
}
#endif
