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
    /// <summary>
    /// The leatherworker's trade bags in the running game (Oakhaven; tools/wip/professions/ADDENDUM.md D): Maud Tanner sells the
    /// four bags, and one bought, carried or worn leaves her list; a worn bag's slots take its own class and refuse the rest; "Sell
    /// junk" keeps pelts; a knock at the Tanner house at night opens her quest talk or her wares; the wallet earned with three wolf
    /// pelts fills with yarrow, and the rows are there after a save and a load.
    /// </summary>
    public class TradeBagTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Open(float hour)
        {
            WorldClock.Hour = hour; EncounterSession.ForgetRestingNodes();
            root = Path.Combine(Path.GetTempPath(), "Crulanda-bags-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; WorldClock.Hour = 8.5f; EncounterSession.ForgetRestingNodes();
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static readonly string[] Bags = { "bag.simples_wallet", "bag.log_sling", "bag.larder_scrip", "bag.ore_poke" };
        const string Maud = "Maud Tanner", WalletQuest = "npc.leatherworker.wallet";

        [UnityTest] public IEnumerator The_leatherworker_sells_the_four_bags_and_a_worn_one_leaves_her_list()
        {
            yield return Open(10.5f);
            var s = Session(); var p = s.Progress; var life = VillageLife.Active;
            Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            var maud = life.Find(Maud); Assert.NotNull(maud, "Maud Tanner keeps the leather shop.");
            Assert.AreEqual("leatherworker", maud.Role); Assert.IsTrue(s.IsVendor(maud), "She has wares now.");
            // Her wares: the four bags, 12 to 24 gold. (All in one frame: a merchant's window shuts when you are not beside it.)
            p.gold = 100;
            s.OpenVendor(maud);
            Assert.AreEqual(Maud, s.VendorNpc); CollectionAssert.AreEqual(Bags, s.VendorStock);
            CollectionAssert.AreEqual(new[] { 12, 16, 20, 24 }, Bags.Select(b => Inventory.Price(s.Items.Get(b))).ToArray());
            s.Buy("bag.ore_poke");
            Assert.AreEqual(76, p.gold, "The ore-poke costs 24 gold."); Assert.AreEqual(1, Inventory.Count(p, "bag.ore_poke"));
            Assert.IsFalse(s.VendorStock.Contains("bag.ore_poke"), "Bought, it leaves her list.");
            s.Buy("bag.ore_poke"); Assert.AreEqual(76, p.gold, "A second is not sold."); Assert.AreEqual(1, Inventory.Count(p, "bag.ore_poke"));
            s.CloseVendor(); s.OpenVendor(maud);
            CollectionAssert.AreEqual(new[] { "bag.simples_wallet", "bag.log_sling", "bag.larder_scrip" }, s.VendorStock, "Carried, it stays off her list.");
            s.CloseVendor();

            // Worn: its eight slots follow the 24.
            Assert.IsTrue(s.EquipFromBag(p.bag.FindIndex(x => x.item == "bag.ore_poke")));
            CollectionAssert.AreEqual(new[] { "bag.ore_poke" }, p.pouches); Assert.AreEqual(Inventory.BagSize + 8, p.bag.Count); Assert.AreEqual(0, Inventory.Count(p, "bag.ore_poke"));
            Assert.Contains("You hang the ore-poke at your hip: 8 slots for ore, bars and charcoal.", s.Messages);
            s.OpenVendor(maud); Assert.IsFalse(s.VendorStock.Contains("bag.ore_poke"), "Worn, it stays off her list."); s.CloseVendor();
            // Ore goes in the poke; a sword is refused by a poke slot.
            Assert.AreEqual(0, Inventory.Add(p, s.Items, "mat.copper_ore", 3)); Assert.AreEqual("mat.copper_ore", p.bag[Inventory.BagSize].item);
            string sword = ItemDatabase.GearId("mainhand", 2, 1, 7); p.bag[0] = new ItemStack { item = sword, count = 1 };
            s.MoveBag(0, Inventory.BagSize + 1);
            Assert.AreEqual(sword, p.bag[0].item); Assert.IsTrue(p.bag[Inventory.BagSize + 1].Empty); Assert.AreEqual("Only ore, bars and charcoal go in the ore-poke.", s.Messages.Last());

            // Pelts: "Sell junk" keeps them, a right-click names who works them, and sold in the village they go to the tannery.
            Assert.AreEqual(0, Inventory.Add(p, s.Items, "junk.wolf_pelt", 3)); Assert.AreEqual(0, Inventory.Add(p, s.Items, "junk.wolf_fang", 2));
            Assert.IsFalse(s.EquipFromBag(p.bag.FindIndex(x => x.item == "junk.wolf_pelt"))); Assert.AreEqual(EncounterSession.HideLine, s.Messages.Last());
            s.OpenVendor(maud); s.SellJunk();
            Assert.AreEqual(3, Inventory.Count(p, "junk.wolf_pelt"), "Sell junk keeps pelts."); Assert.AreEqual(0, Inventory.Count(p, "junk.wolf_fang"));
            int hides = life.Count("tannery.hides");
            s.SellBag(p.bag.FindIndex(x => x.item == "junk.wolf_pelt"));
            Assert.AreEqual(0, Inventory.Count(p, "junk.wolf_pelt")); Assert.AreEqual(hides + 3, life.Count("tannery.hides")); Assert.AreEqual(3, life.Count("sold.tannery.hides"));
            s.CloseVendor();

            // All four bought and worn: she has nothing left to sell you, and says so.
            p.gold = 100;
            s.OpenVendor(maud); foreach (var b in new[] { "bag.simples_wallet", "bag.log_sling", "bag.larder_scrip" }) s.Buy(b); s.CloseVendor();
            Assert.AreEqual(100 - 12 - 16 - 20, p.gold);
            foreach (var b in new[] { "bag.simples_wallet", "bag.log_sling", "bag.larder_scrip" }) Assert.IsTrue(s.EquipFromBag(p.bag.FindIndex(x => x.item == b)), b);
            CollectionAssert.AreEqual(new[] { "bag.ore_poke", "bag.simples_wallet", "bag.log_sling", "bag.larder_scrip" }, p.pouches);
            Assert.AreEqual(Inventory.BagSize + 8 + 6 + 6 + 8, p.bag.Count);
            s.OpenVendor(maud);
            Assert.IsNull(s.VendorNpc, "No wares to show."); Assert.AreEqual(Maud + ": " + EncounterSession.AllBagsLine, s.Messages.Last());
            Assert.AreEqual(QuestStatus.Unavailable, s.Quests.Status(s.Quests.Def(WalletQuest), s.ZoneId), "With a wallet worn, her wallet quest is not offered.");

            // Save, load: the bags are worn and their rows hold what they held.
            Assert.IsFalse(s.InCombat, "Nothing is fighting at the village gate, so the save goes through.");
            s.Save(false); s.Load();
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreNotSame(p, s.Progress, "The load read the save.");
            CollectionAssert.AreEqual(new[] { "bag.ore_poke", "bag.simples_wallet", "bag.log_sling", "bag.larder_scrip" }, s.Progress.pouches);
            Assert.AreEqual(Inventory.BagSize + 28, s.Progress.bag.Count); Assert.AreEqual("mat.copper_ore", s.Progress.bag[Inventory.BagSize].item);
        }

        /// <summary>The player a step out from a house's door, so it is what E knocks at.</summary>
        static IEnumerator StandAtDoor(EncounterSession s, ZoneDoor door)
        {
            var house = s.Zone.Zone.props.First(x => x != null && x.name == door.name);
            var out2 = new Vector2(door.position.x - house.at.x, door.position.z - house.at.y).normalized;
            s.Player.GetComponent<AdventurerMotor>().Teleport(s.Zone.Ground(new Vector2(door.position.x, door.position.z) + out2, 1.1f));
            yield return null;
        }

        [UnityTest] public IEnumerator Knocking_at_night_opens_her_wares()
        {
            yield return Open(23.5f);
            var s = Session(); var life = VillageLife.Active;
            var door = s.Zone.Doors.Single(d => d.name == "Tanner house");
            Assert.IsTrue(life.AtHome(door).Any(v => v.Name == Maud), "Maud is abed at home.");
            // With her wallet quest still to offer, the knock opens her talk: the quest, and "Browse wares" one click away.
            yield return StandAtDoor(s, door);
            Assert.AreSame(door, s.NearbyDoor);
            s.Interact();
            Assert.Contains("Tanner house: " + EncounterSession.ShutterLine, s.Messages);
            Assert.NotNull(s.Conversation, "The knock opens her talk."); Assert.AreEqual(Maud, s.Conversation.npc);
            Assert.IsTrue(s.Conversation.entries.Any(e => e.quest.id == WalletQuest)); Assert.IsNull(s.Conversation.selected, "She sells too, so it opens on the list.");
            s.Conversation = null;
            // With nothing to offer, the knock opens her wares at the door.
            s.Progress.questsDone.Add(WalletQuest); s.Progress.gold = 30;
            s.Interact();
            Assert.AreEqual(Maud, s.VendorNpc, "The knock opens Maud's wares."); CollectionAssert.AreEqual(Bags, s.VendorStock);
            s.Buy("bag.simples_wallet");
            Assert.AreEqual(18, s.Progress.gold); Assert.AreEqual(1, Inventory.Count(s.Progress, "bag.simples_wallet"), "Bought through the shutter.");
            yield return null;
            Assert.AreEqual(Maud, s.VendorNpc, "The window stays open while you stand at her door.");
            s.CloseVendor();
        }

        [UnityTest] public IEnumerator A_wallet_earned_with_three_pelts_fills_with_yarrow_and_survives_a_reload()
        {
            yield return Open(10.5f);
            var s = Session(); var p = s.Progress; var log = s.Quests; var maud = VillageLife.Active.Find(Maud);
            var q = log.Def(WalletQuest); Assert.NotNull(q, "The wallet quest is in the content.");
            // Offered with her wares, then taken on.
            Assert.IsTrue(s.QuestTalk(Maud, maud.transform.position)); Assert.IsTrue(s.Conversation.entries.Any(e => e.quest == q));
            s.AcceptQuest(q); Assert.AreEqual(QuestStatus.Active, log.Status(q, s.ZoneId));
            // Three pelts in the bags; talking to her hands them over; the wallet is the reward.
            Assert.AreEqual(0, Inventory.Add(p, s.Items, "junk.wolf_pelt", 3));
            Assert.AreEqual('?', log.Marker(Maud, s.ZoneId, p.Level, out bool grey)); Assert.IsFalse(grey);
            Assert.IsTrue(s.QuestTalk(Maud, maud.transform.position));
            Assert.AreEqual(0, Inventory.Count(p, "junk.wolf_pelt")); Assert.AreEqual(QuestStatus.ReadyToTurnIn, log.Status(q, s.ZoneId));
            s.CompleteQuest(q);
            Assert.IsTrue(log.IsDone(WalletQuest)); Assert.AreEqual(1, Inventory.Count(p, "bag.simples_wallet"));
            // Worn, the wallet takes the yarrow picked from a meadow patch.
            Assert.IsTrue(s.EquipFromBag(p.bag.FindIndex(x => x.item == "bag.simples_wallet")));
            var yarrow = s.Zone.Interactables.First(i => i.node == "node.yarrow");
            Assert.IsTrue(s.GatherNow(yarrow));
            Assert.AreEqual("mat.yarrow", p.bag[Inventory.BagSize].item, "The yarrow lands in the wallet's row.");
            for (int i = 0; i < Inventory.BagSize; i++) Assert.AreNotEqual("mat.yarrow", p.bag[i].item);
            int picked = p.bag[Inventory.BagSize].count;
            // Saved and loaded, the row is there with the yarrow in it.
            s.Save(false); s.Load();
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreNotSame(p, s.Progress);
            CollectionAssert.AreEqual(new[] { "bag.simples_wallet" }, s.Progress.pouches);
            Assert.AreEqual(Inventory.BagSize + 6, s.Progress.bag.Count);
            Assert.AreEqual("mat.yarrow", s.Progress.bag[Inventory.BagSize].item); Assert.AreEqual(picked, s.Progress.bag[Inventory.BagSize].count);
            Assert.IsTrue(s.Quests.IsDone(WalletQuest));
        }
    }
}
#endif
