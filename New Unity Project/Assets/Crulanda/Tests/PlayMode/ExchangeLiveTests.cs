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
    /// The Exchange in a live session in the Crown of Argentis (CAPITAL_HUB.md; Exchange.cs, EncounterSession.Capital.cs; p9-exchange2,
    /// Chris 2026-10-16: "id rather it be finished before testing"): E on the auctioneer opens it; a stack from the bags is listed for a
    /// fair price, a sim buys it as the world-hours run, you are told, and the coin is collected at the clerk; a sim's lot is bought
    /// outright, and a bid of your own typing is placed (refused under the least bid, a buy at the buyout). The market's hours are run
    /// by hand (the sims' life is off in a test run, so the world clock does not run it).
    /// </summary>
    public class ExchangeLiveTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11; ZoneBuilder.RequestedZoneId = "zone.crown";
            root = Path.Combine(Path.GetTempPath(), "Crulanda-exchange-live-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.NotNull(s); Assert.NotNull(s.Player);
            Assert.AreEqual("zone.crown", ZoneBuilder.Active.Zone.id, "In the Crown of Argentis.");
            Assert.NotNull(s.ExchangeWorld, "The world (and its Exchange) is loaded.");
            return s;
        }
        /// <summary>Every sim online all day, with coin: buyers at every hour.</summary>
        static void Busy(WorldSave w) { foreach (var sim in w.sims) { sim.onlineFrom = 0; sim.onlineHours = 24; sim.coin = Math.Max(sim.coin, 400); } }

        [UnityTest] public IEnumerator Talk_to_the_auctioneer_list_a_lot_a_sim_buys_it_and_collect_the_coin()
        {
            var s = Session();
            var clerk = VillageLife.Active?.Villagers.FirstOrDefault(v => v != null && v.Role == EncounterSession.AuctioneerRole);
            Assert.NotNull(clerk, "The Exchange's clerk (role auctioneer) is at the counter in the Crown.");
            s.Player.GetComponent<AdventurerMotor>().Teleport(clerk.transform.position + clerk.transform.forward * 1.4f);
            for (int i = 0; i < 3; i++) yield return null;
            s.SelectFriendly(clerk, false); s.Interact();
            Assert.IsTrue(s.ExchangeOpen, "E on the auctioneer opens the Exchange.");
            Assert.AreEqual(clerk.Name, s.CapitalClerk);

            Inventory.Add(s.Progress, s.Items, "junk.wolf_pelt", 10);
            int at = s.Progress.bag.FindIndex(b => b.item == "junk.wolf_pelt"); Assert.GreaterOrEqual(at, 0);
            s.Progress.gold = Math.Max(s.Progress.gold, 50);
            s.ExchangeChoose(at); Assert.AreEqual(at, s.ExchangeSell, "Chosen to sell.");
            int fair = Exchange.Fair(s.Items.Get("junk.wolf_pelt"), 10);
            Assert.IsTrue(s.ExchangeList(fair, 0, 12), "Listed at a fair price.");
            var w = s.ExchangeWorld; var x = Exchange.Ensure(w);
            var lot = Exchange.Mine(x, s.Progress.classId).Single(); Assert.AreEqual(10, lot.count);
            Assert.AreEqual(0, Inventory.Count(s.Progress, "junk.wolf_pelt"), "The stack left the bags.");

            Busy(w);
            for (int h = 0; h < 11 && x.listings.Contains(lot); h++) Exchange.Run(w, 1, WorldClock.Hour, s.Items, s.Professions?.Db);
            Assert.IsFalse(x.listings.Contains(lot), "A sim bought the fair lot within the half day.");
            s.ExchangeNotices(false);
            Assert.IsTrue(s.Messages.Any(m => m.StartsWith("The Exchange: Sold")), "You are told it sold: " + string.Join(" | ", s.Messages));
            int gold = s.Progress.gold;
            s.ExchangeCollect();
            Assert.AreEqual(gold + fair + lot.deposit, s.Progress.gold, "The price and the deposit, collected at the clerk.");
            Assert.AreEqual(0, Exchange.Waiting(x, s.Progress.classId).Count, "Nothing more waits.");
            yield return null;
        }

        [UnityTest] public IEnumerator Buy_a_sims_lot_and_place_a_bid_of_your_own_typing()
        {
            var s = Session();
            s.PlayerChat("/exchange");
            Assert.IsTrue(s.ExchangeOpen, "/exchange opens it where you stand.");
            var w = s.ExchangeWorld; var x = Exchange.Ensure(w); var seller = w.sims[0];
            ExchangeListing Put(int count, int buyout, int start)
            {
                var l = new ExchangeListing { id = x.nextId++, item = "potion.minor", count = count, seller = seller.id, sellerName = seller.name, buyout = buyout, startBid = start, listedAt = x.hours, endsAt = x.hours + 24 };
                x.listings.Add(l); return l;
            }
            s.Progress.gold = 300; int sellerCoin = seller.coin, had = Inventory.Count(s.Progress, "potion.minor");
            var outright = Put(3, 40, 0);
            Assert.IsTrue(Exchange.Browse(x, s.Items, "", -1, 0, 0, -1).Contains(outright), "The sim's lot is in Browse.");
            s.ExchangeBuy(outright.id);
            Assert.AreEqual(260, s.Progress.gold); Assert.AreEqual(had + 3, Inventory.Count(s.Progress, "potion.minor")); Assert.AreEqual(sellerCoin + 40, seller.coin, "The sim is paid.");
            Assert.AreEqual(13, Exchange.LastSold(x, "potion.minor"), "Browse shows what one last sold for.");

            var auction = Put(5, 120, 20);
            s.ExchangeBid(auction.id, 15);
            Assert.AreEqual(0, auction.bid, "Under the least bid: refused."); Assert.AreEqual(260, s.Progress.gold);
            s.ExchangeBid(auction.id, 400);
            Assert.AreEqual(0, auction.bid, "Over your coin: refused, though it is over the buyout too."); Assert.AreEqual(260, s.Progress.gold); Assert.IsTrue(x.listings.Contains(auction));
            s.ExchangeBid(auction.id, 35);
            Assert.AreEqual(35, auction.bid, "The amount typed."); Assert.AreEqual(Exchange.YouId(s.Progress.classId), auction.bidder); Assert.AreEqual(225, s.Progress.gold);
            var dear = Put(2, 90, 10);
            s.ExchangeBid(dear.id, 90);
            Assert.IsFalse(x.listings.Contains(dear), "A typed bid at the buyout buys it outright."); Assert.AreEqual(135, s.Progress.gold);
            s.Escape(); Assert.IsFalse(s.ExchangeOpen, "Esc shuts the Exchange.");
            yield return null;
        }
    }
}
#endif
