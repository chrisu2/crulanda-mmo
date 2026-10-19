using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.Persistence;

namespace Crulanda.Tests
{
    /// <summary>The Exchange (CAPITAL_HUB.md; Exchange.cs, world format 2): listing and its deposit, the sims buying fair lots and
    /// leaving silly ones, expiry, bids and being outbid, collecting at the clerk, the hours run while away, the sims' own lots, the
    /// world save's round trip and its migration from format 1; and (p9-exchange2) typed bids, gear bought by a sim it fits and
    /// worn, the sims trading among themselves, their lots bounded and put up again cheaper, their crafted goods and leaves listed,
    /// the notices, and the last-sold prices.</summary>
    public class ExchangeTests
    {
        const string QuestItemFile = "{\"items\":[{\"id\":\"quest.sealed_letter\",\"name\":\"Sealed Letter\",\"kind\":\"quest\",\"quality\":1}]}";
        static ItemDatabase db;
        static ItemDatabase Db()
        {
            if (db != null) return db;
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Items");
            var texts = new List<string>(); foreach (var f in Directory.GetFiles(dir, "*.json")) texts.Add(File.ReadAllText(f));
            texts.Add(QuestItemFile);
            return db = ItemDatabase.Parse(texts);
        }
        /// <summary>A small world: six sims online all day with coin, nothing listed.</summary>
        static WorldSave World(int seed = 7, int online = 24)
        {
            var w = new WorldSave { seed = seed };
            for (int i = 0; i < 6; i++) w.sims.Add(new SimAdventurer { id = "sim." + i, name = "Sim " + i, classId = "class.warrior", onlineFrom = 0, onlineHours = online, coin = 500 });
            Exchange.Ensure(w); return w;
        }
        static EncounterProgress Character(string classId, int gold) { var p = EncounterSession.FreshProgress(classId); p.gold = gold; return p; }
        static ExchangeListing SimLot(WorldSave w, string item, int count, int buyout, int startBid, int hours = 24)
        {
            var x = Exchange.Ensure(w); var s = w.sims[0];
            var l = new ExchangeListing { id = x.nextId++, item = item, count = count, seller = s.id, sellerName = s.name, buyout = buyout, startBid = startBid, listedAt = x.hours, endsAt = x.hours + hours };
            x.listings.Add(l); return l;
        }

        [Test] public void Listing_takes_the_stack_and_a_small_deposit()
        {
            var w = World(); var p = Character("class.warrior", 100);
            Inventory.Add(p, Db(), "junk.wolf_pelt", 10);
            int fair = Exchange.Fair(Db().Get("junk.wolf_pelt"), 10); Assert.AreEqual(40, fair, "value 2 x common's 2 x 10");
            Assert.IsNull(Exchange.List(w, p, Db(), 0, 40, 0, 13, out var why), "12, 24 or 48 hours only"); Assert.IsNotNull(why);
            Assert.IsNull(Exchange.List(w, p, Db(), 0, 40, 40, 24, out why), "a starting bid under the buyout");
            Assert.IsNull(Exchange.List(w, p, Db(), 0, 0, 0, 24, out why), "a buyout is needed");
            var l = Exchange.List(w, p, Db(), 0, 40, 0, 24, out why); Assert.NotNull(l, why);
            Assert.AreEqual(4, l.deposit, "a tenth of its value for each 12 hours"); Assert.AreEqual(96, p.gold);
            Assert.IsTrue(p.bag[0].Empty, "the stack left the bags"); Assert.AreEqual(10, l.count); Assert.AreEqual("class.warrior", l.owner); Assert.IsTrue(Exchange.IsYours(l));
            Assert.AreEqual(24, l.endsAt - l.listedAt, 1e-4f); Assert.AreEqual(1, Exchange.Mine(w.exchange, "class.warrior").Count); Assert.AreEqual(0, Exchange.Mine(w.exchange, "class.mage").Count);
            // Refused: a quest item, and a deposit you cannot pay.
            p.bag[1] = new ItemStack { item = "quest.sealed_letter", count = 1 };
            Assert.IsNull(Exchange.List(w, p, Db(), 1, 10, 0, 12, out why)); Assert.AreEqual("quest.sealed_letter", p.bag[1].item);
            var poor = Character("class.mage", 0); Inventory.Add(poor, Db(), "junk.wolf_pelt", 10);
            Assert.IsNull(Exchange.List(w, poor, Db(), 0, 40, 0, 48, out why)); Assert.AreEqual(10, poor.bag[0].count, "kept");
        }

        [Test] public void Buy_chance_follows_fairness_and_how_many_are_online()
        {
            Assert.AreEqual(0, Exchange.BuyChance(3, 1), 1e-6f, "three times fair never sells");
            Assert.AreEqual(0, Exchange.BuyChance(2.5f, 1), 1e-6f);
            Assert.Greater(Exchange.BuyChance(1, 1), Exchange.BuyChance(2, 1));
            Assert.Greater(Exchange.BuyChance(.5f, 1), Exchange.BuyChance(1, 1), "a bargain goes faster");
            Assert.Greater(Exchange.BuyChance(1, 1), Exchange.BuyChance(1, .2f), "more sims online, more buyers");
            Assert.AreEqual(0, Exchange.BuyChance(1, 0), 1e-6f, "nobody online, nobody buys");
        }

        [Test] public void Sims_buy_a_fair_lot_and_the_coin_and_deposit_wait_at_the_clerk()
        {
            var w = World(); var p = Character("class.warrior", 100); Inventory.Add(p, Db(), "junk.wolf_pelt", 10);
            var l = Exchange.List(w, p, Db(), 0, 40, 0, 48, out var why); Assert.NotNull(l, why);
            Exchange.Run(w, 47, 12, Db());
            Assert.IsFalse(w.exchange.listings.Contains(l), "a fair lot sells within two days with everyone online");
            var waiting = Exchange.Waiting(w.exchange, "class.warrior");
            Assert.AreEqual(1, waiting.Count); Assert.AreEqual(40 + l.deposit, waiting[0].coin, "the price and the deposit back"); StringAssert.StartsWith("Sold", waiting[0].note);
            Assert.AreEqual(500 * 6 - 40, w.sims.Sum(s => s.coin), "a sim paid for it");
            int listed = w.exchange.listings.Where(o => !Exchange.IsYours(o) && o.item == "junk.wolf_pelt").Sum(o => o.count);
            Assert.AreEqual(10, w.sims.Sum(s => SimEconomy.Count(s, "junk.wolf_pelt")) + listed, "and has the pelts (or has put them up again)");
            Assert.IsTrue(Exchange.History(w.exchange, "class.warrior").Exists(r => r.line.StartsWith("Sold")));
        }

        [Test] public void A_silly_price_does_not_sell_and_comes_back_when_it_runs_out()
        {
            var w = World(); var p = Character("class.warrior", 100); Inventory.Add(p, Db(), "junk.wolf_pelt", 10);
            var l = Exchange.List(w, p, Db(), 0, 400, 0, 12, out var why); Assert.NotNull(l, why);   // ten times fair
            Exchange.Run(w, 11.5f, 12, Db());
            Assert.IsTrue(w.exchange.listings.Contains(l), "still up after 11 hours: nobody pays ten times fair");
            Exchange.Run(w, 1, 12, Db());
            Assert.IsFalse(w.exchange.listings.Contains(l), "12 hours: it ran out");
            var waiting = Exchange.Waiting(w.exchange, "class.warrior");
            Assert.AreEqual(1, waiting.Count); Assert.AreEqual("junk.wolf_pelt", waiting[0].item); Assert.AreEqual(10, waiting[0].count); Assert.AreEqual(0, waiting[0].coin, "the deposit is kept");
            Assert.AreEqual(3000, w.sims.Sum(s => s.coin));
        }

        [Test] public void Nobody_online_nobody_buys()
        {
            var w = World(online: 0); var p = Character("class.warrior", 100); Inventory.Add(p, Db(), "junk.wolf_pelt", 10);
            var l = Exchange.List(w, p, Db(), 0, 20, 0, 24, out var why); Assert.NotNull(l, why);   // half of fair
            Exchange.Run(w, 23, 12, Db());
            Assert.IsTrue(w.exchange.listings.Contains(l));
        }

        [Test] public void Taking_a_lot_down_returns_the_item_but_not_the_deposit()
        {
            var w = World(online: 0); var p = Character("class.warrior", 100); Inventory.Add(p, Db(), "potion.minor", 5);
            var l = Exchange.List(w, p, Db(), 0, 30, 10, 24, out var why); Assert.NotNull(l, why);
            Assert.IsFalse(Exchange.Cancel(w, Character("class.mage", 0), Db(), l.id, out why), "another character's lot");
            Assert.IsTrue(Exchange.Cancel(w, p, Db(), l.id, out why), why);
            var c = Exchange.Waiting(w.exchange, "class.warrior").Single(); Assert.AreEqual("potion.minor", c.item); Assert.AreEqual(5, c.count); Assert.AreEqual(0, c.coin);
            l = Exchange.List(w, p, Db(), p.bag.FindIndex(s => s.Empty), 30, 10, 24, out why); Assert.IsNull(l, "nothing chosen");
        }

        [Test] public void You_buy_a_sims_lot_outright()
        {
            var w = World(); var p = Character("class.warrior", 50);
            var l = SimLot(w, "potion.minor", 5, 40, 0);
            Assert.IsFalse(Exchange.Bid(w, p, Db(), l.id, 10, out var why), "buyout only");
            Assert.IsTrue(Exchange.BuyOut(w, p, Db(), l.id, out why), why);
            Assert.AreEqual(10, p.gold); Assert.AreEqual(5, Inventory.Count(p, "potion.minor")); Assert.AreEqual(540, w.sims[0].coin, "the seller is paid");
            Assert.IsFalse(w.exchange.listings.Contains(l));
            var dear = SimLot(w, "potion.minor", 5, 400, 0);
            Assert.IsFalse(Exchange.BuyOut(w, p, Db(), dear.id, out why), "too dear"); Assert.AreEqual(10, p.gold);
            var full = Character("class.mage", 1000); for (int i = 0; i < Inventory.BagSize; i++) full.bag[i] = new ItemStack { item = ItemDatabase.GearId("feet", 2, 1, i), count = 1 };
            Assert.IsFalse(Exchange.BuyOut(w, full, Db(), dear.id, out why)); Assert.AreEqual("Your bags are full.", why); Assert.AreEqual(1000, full.gold);
        }

        [Test] public void Bids_hold_the_coin_and_an_outbid_bid_comes_back()
        {
            var w = World(online: 0); var warrior = Character("class.warrior", 200); var mage = Character("class.mage", 200);
            var l = SimLot(w, "potion.minor", 5, 100, 20);
            Assert.IsFalse(Exchange.Bid(w, warrior, Db(), l.id, 10, out var why), "under the starting bid");
            Assert.IsTrue(Exchange.Bid(w, warrior, Db(), l.id, 20, out why), why); Assert.AreEqual(180, warrior.gold); Assert.AreEqual(Exchange.YouId("class.warrior"), l.bidder);
            Assert.AreEqual(21, Exchange.MinBid(l));
            Assert.IsFalse(Exchange.Bid(w, warrior, Db(), l.id, 40, out why), "already the highest");
            Assert.IsTrue(Exchange.Bid(w, mage, Db(), l.id, 30, out why), why); Assert.AreEqual(170, mage.gold);
            var back = Exchange.Waiting(w.exchange, "class.warrior").Single(); Assert.AreEqual(20, back.coin); StringAssert.StartsWith("Outbid", back.note);
            // A bid at the buyout buys it; the mage's held 30 counts towards it.
            Assert.IsTrue(Exchange.Bid(w, mage, Db(), l.id, 0, out why) == false);
            Assert.IsTrue(Exchange.BuyOut(w, mage, Db(), l.id, out why), why); Assert.AreEqual(100, mage.gold); Assert.AreEqual(5, Inventory.Count(mage, "potion.minor"));
            Assert.AreEqual("Collected 20 crowns.", Exchange.Collect(w, warrior, Db())); Assert.AreEqual(200, warrior.gold);
        }

        [Test] public void Sims_sometimes_outbid_you_and_sometimes_you_win()
        {
            int won = 0, outbid = 0;
            for (int seed = 1; seed <= 30; seed++)
            {
                var w = World(seed); var p = Character("class.warrior", 100);
                var l = SimLot(w, "potion.minor", 5, 100, 20, 24);   // fair is 30: a bid of 60 is outbid only now and then
                Assert.IsTrue(Exchange.Bid(w, p, Db(), l.id, 60, out var why), why);
                Exchange.Run(w, 24, 12, Db());
                Assert.IsFalse(w.exchange.listings.Contains(l), "it ran its time");
                var waiting = Exchange.Waiting(w.exchange, "class.warrior");
                bool item = waiting.Exists(c => c.item == "potion.minor" && c.count == 5), coin = waiting.Exists(c => c.coin == 60);
                Assert.IsTrue(item ^ coin, "seed " + seed + ": either the lot is yours or your coin came back, never both or neither");
                if (item) won++; else outbid++;
            }
            Assert.Greater(won, 0, "you win some"); Assert.Greater(outbid, 0, "the sims outbid you sometimes");
        }

        [Test] public void Sims_bid_on_your_lot_and_the_highest_bid_takes_it_at_the_end()
        {
            int sold = 0;
            for (int seed = 1; seed <= 20; seed++)
            {
                var w = World(seed); var p = Character("class.warrior", 100); Inventory.Add(p, Db(), "junk.wolf_pelt", 10);
                var l = Exchange.List(w, p, Db(), 0, 200, 30, 12, out var why); Assert.NotNull(l, why);   // a silly buyout, a fair starting bid
                Exchange.Run(w, 12, 12, Db());
                var c = Exchange.Waiting(w.exchange, "class.warrior").Single();
                if (c.coin > 0) { sold++; Assert.GreaterOrEqual(c.coin, 30 + l.deposit); Assert.Less(c.coin, 200 + l.deposit, "sold to a bid, not the buyout"); }
                else Assert.AreEqual(10, c.count, "unsold, it came back");
            }
            Assert.Greater(sold, 0, "a fair starting bid draws bids");
        }

        [Test] public void Collect_pays_this_character_only_and_full_bags_keep_the_rest_waiting()
        {
            var w = World(); var x = w.exchange;
            x.parcels.Add(new ExchangeParcel { owner = "class.warrior", coin = 75, note = "Sold" });
            x.parcels.Add(new ExchangeParcel { owner = "class.warrior", item = "potion.minor", count = 3, note = "Won" });
            x.parcels.Add(new ExchangeParcel { owner = "class.mage", coin = 9, note = "Sold" });
            var p = Character("class.warrior", 0);
            Assert.AreEqual("Collected 75 crowns and 3 items.", Exchange.Collect(w, p, Db()));
            Assert.AreEqual(75, p.gold); Assert.AreEqual(3, Inventory.Count(p, "potion.minor"));
            Assert.AreEqual(1, x.parcels.Count); Assert.AreEqual("class.mage", x.parcels[0].owner);
            Assert.IsNull(Exchange.Collect(w, p, Db()), "nothing more");
            x.parcels.Add(new ExchangeParcel { owner = "class.warrior", item = "junk.wolf_pelt", count = 5, note = "Back" });
            for (int i = 0; i < Inventory.BagSize; i++) p.bag[i] = new ItemStack { item = ItemDatabase.GearId("feet", 2, 1, i), count = 1 };
            Assert.AreEqual("Your bags are full.", Exchange.Collect(w, p, Db())); Assert.AreEqual(5, Exchange.Waiting(x, "class.warrior").Single().count);
        }

        [Test] public void The_clock_runs_the_market_and_the_hours_away_run_on_the_next_start()
        {
            var w = World(); var x = w.exchange;
            Assert.AreEqual(0, Exchange.Follow(w, 23, Db()), "the first look only sets the clock");
            Assert.AreEqual(2, Exchange.Follow(w, 1, Db()), "23:00 to 01:00 is two hours, past midnight");
            Assert.AreEqual(0, Exchange.Follow(w, 1.5f, Db()), "half an hour is not a market hour yet");
            Assert.AreEqual(1, Exchange.Follow(w, 2.1f, Db()), "the half hours add up");
            // Away from the game: two real hours are 72 world-hours, run as two days at most.
            var p = Character("class.warrior", 100); Inventory.Add(p, Db(), "junk.wolf_pelt", 10);
            var l = Exchange.List(w, p, Db(), 0, 400, 0, 12, out var why); Assert.NotNull(l, why);
            var now = new DateTime(2026, 10, 15, 20, 0, 0, DateTimeKind.Utc);
            x.realUtc = now.AddHours(-2).ToString("O"); float before = x.hours;
            Assert.AreEqual(48, Exchange.CatchUp(w, now, 9, Db()), "48 world-hours at most");
            Assert.AreEqual(before + 48, x.hours, 1e-3f); Assert.IsFalse(x.listings.Contains(l), "the 12-hour lot ran out while you were away");
            Assert.AreEqual(now.ToString("O"), x.realUtc); Assert.AreEqual(9, x.clockAt, 1e-4f);
            x.realUtc = now.AddMinutes(-10).ToString("O");
            Assert.AreEqual(6, Exchange.CatchUp(w, now, 9, Db()), "ten real minutes are six world-hours");
            x.realUtc = ""; Assert.AreEqual(0, Exchange.CatchUp(w, now, 9, Db()), "a world never saved by this build runs nothing");
        }

        [Test] public void Sims_list_their_goods_at_a_markup_and_take_back_what_does_not_sell()
        {
            var w = World(); foreach (var s in w.sims) SimEconomy.Add(s, "mat.copper_ore", 40);
            Exchange.Run(w, 30, 12, Db());
            var lots = w.exchange.listings.Where(l => !Exchange.IsYours(l)).ToList();
            Assert.Greater(lots.Count, 0, "the sims put up their ore");
            int fair20 = Exchange.Fair(Db().Get("mat.copper_ore"), 20); Assert.AreEqual(40, fair20);
            foreach (var l in lots)
            {
                Assert.AreEqual("mat.copper_ore", l.item); Assert.LessOrEqual(l.count, 20, "a stack at most");
                float perOne = l.buyout / (float)l.count; Assert.GreaterOrEqual(perOne, 2 * .85f * Math.Pow(Exchange.RelistCut, l.relisted) - .05f, "less each time it went up again"); Assert.LessOrEqual(perOne, 2 * 1.2f, "value 1 x common's markup of 2, give or take");
                Assert.Less(l.startBid, l.buyout);
            }
            int listed = lots.Sum(l => l.count), held = w.sims.Sum(s => SimEconomy.Count(s, "mat.copper_ore"));
            Assert.AreEqual(6 * 40, listed + held, "nothing made or lost: what ran out came back to its sim");
            Assert.AreEqual(1, Exchange.Browse(w.exchange, Db(), "copper", -1, 0, 0, -1).Select(l => l.item).Distinct().Count());
            Assert.AreEqual(0, Exchange.Browse(w.exchange, Db(), "copper", 0, 0, 0, -1).Count, "not a head piece");
            Assert.AreEqual(0, Exchange.Browse(w.exchange, Db(), "", -1, 0, 0, 3).Count, "not rare");
            Assert.AreEqual(lots.Count, Exchange.Browse(w.exchange, Db(), "COPPER ORE", -1, 1, 5, 1).Count, "any case, its level and quality");
        }

        [Test] public void The_world_save_round_trips_the_exchange()
        {
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-exchange-" + Guid.NewGuid().ToString("N"));
            try
            {
                var w = SimRoster.LoadOrCreate(root); var p = Character("class.warrior", 100); Inventory.Add(p, Db(), "potion.minor", 4);
                var l = Exchange.List(w, p, Db(), 0, 50, 12, 48, out var why); Assert.NotNull(l, why);
                w.exchange.parcels.Add(new ExchangeParcel { owner = "class.mage", coin = 7, note = "Sold" });
                l.relisted = 2; w.exchange.history.Add(new ExchangeRecord { owner = "class.warrior", line = "Sold.", fresh = true }); w.exchange.sales.Add(new ExchangeSale { item = "potion.minor", each = 6, at = 1 });
                w.exchange.hours = 3.5f; w.exchange.tickedTo = 3; Exchange.Stamp(w, new DateTime(2026, 10, 15, 21, 0, 0, DateTimeKind.Utc));
                SimRoster.Save(root, w);
                Assert.IsTrue(new SaveFileStore(root).TryRead("world", out var env, out _)); Assert.AreEqual(WorldSave.FormatVersion, env.formatVersion);   // 3 since the world boss (2026-10-19)
                var back = SimRoster.Load(root, out var msg); Assert.NotNull(back, msg);
                var bl = back.exchange.listings.Single(o => o.id == l.id);
                Assert.AreEqual("potion.minor", bl.item); Assert.AreEqual(4, bl.count); Assert.AreEqual(50, bl.buyout); Assert.AreEqual(12, bl.startBid); Assert.AreEqual(l.deposit, bl.deposit);
                Assert.AreEqual("class.warrior", bl.owner); Assert.AreEqual(l.endsAt, bl.endsAt, 1e-4f);
                Assert.AreEqual(w.exchange.hours, back.exchange.hours, 1e-4f); Assert.AreEqual(w.exchange.nextId, back.exchange.nextId); Assert.AreEqual(w.exchange.realUtc, back.exchange.realUtc);
                Assert.AreEqual(7, Exchange.Waiting(back.exchange, "class.mage").Single().coin);
                Assert.AreEqual(1, back.exchange.history.Count(r => r.fresh), "a notice not yet told stays news"); Assert.AreEqual(2, bl.relisted);
                Assert.AreEqual(6, Exchange.LastSold(back.exchange, "potion.minor"), "the last-sold prices are kept");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test] public void A_format_1_world_migrates_with_an_empty_exchange()
        {
            Assert.AreEqual("{\"seed\":5,\"exchange\":{}}", new AddExchangeMigration().Migrate("{\"seed\":5}"), "only the field added");
            Assert.AreEqual("{\"exchange\":{}}", new AddExchangeMigration().Migrate("{}"));
            Assert.AreEqual("{\"seed\":5,\"exchange\":{}}", new AddExchangeMigration().Migrate("{\"seed\":5,\"exchange\":{}}"), "left alone when there");
            Assert.Throws<SaveMigrationException>(() => new AddExchangeMigration().Migrate("[]"));
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-exchange-" + Guid.NewGuid().ToString("N"));
            try
            {
                var w = SimRoster.LoadOrCreate(root); w.sims[2].level = 7; w.party.Add(w.sims[1].id);
                // The world as a format-1 build wrote it: everything but the Exchange.
                string json = JsonUtility.ToJson(w); int cut = json.IndexOf(",\"exchange\":", StringComparison.Ordinal); Assert.Greater(cut, 0);
                string v1 = json.Substring(0, cut) + "}";
                var store = new SaveFileStore(root);
                store.Write("world", new SaveEnvelope { formatVersion = 1, payloadType = WorldSave.PayloadType, gameVersion = "0.3.0", savedAtUtc = DateTime.UtcNow.ToString("O"), payloadJson = v1 });
                var back = SimRoster.Load(root, out var msg); Assert.NotNull(back, msg);
                Assert.AreEqual(w.sims.Count, back.sims.Count); Assert.AreEqual(7, back.sims[2].level); Assert.AreEqual(w.sims[1].id, back.party.Single()); Assert.AreEqual(w.seed, back.seed);
                Assert.NotNull(back.exchange); Assert.AreEqual(0, back.exchange.listings.Count); Assert.AreEqual(0, back.exchange.parcels.Count); Assert.AreEqual(1, back.exchange.nextId);
                // A newer world than this build knows is refused, not read.
                store.Write("world", new SaveEnvelope { formatVersion = WorldSave.FormatVersion + 1, payloadType = WorldSave.PayloadType, gameVersion = "9", savedAtUtc = DateTime.UtcNow.ToString("O"), payloadJson = json });
                Assert.IsNull(SimRoster.Load(root, out msg));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    

        // ---------- p9-exchange2: finished (Chris, 2026-10-16: "id rather it be finished before testing") ----------
        static ProfessionDatabase trades;
        static ProfessionDatabase Trades() { return trades ?? (trades = ProfessionDatabase.Parse(new[] { File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Professions", "professions.json")) }, Db())); }
        static SimAdventurer Sim(WorldSave w, int i, string classId, int level, int coin = 500)
        {
            var s = w.sims[i]; s.classId = classId; s.level = level; s.coin = coin; return s;
        }

        [Test] public void A_typed_bid_is_refused_under_the_least_bid_or_over_your_coin_and_buys_at_the_buyout()
        {
            var w = World(online: 0); var p = Character("class.warrior", 120);
            var l = SimLot(w, "potion.minor", 5, 200, 20);
            Assert.IsFalse(Exchange.Bid(w, p, Db(), l.id, 19, out var why), "under the starting bid"); StringAssert.Contains("20 crowns", why);
            Assert.IsFalse(Exchange.Bid(w, p, Db(), l.id, 150, out why), "more than your coin"); StringAssert.StartsWith("You need", why); Assert.AreEqual(120, p.gold);
            Assert.IsTrue(Exchange.Bid(w, p, Db(), l.id, 57, out why), why); Assert.AreEqual(57, l.bid, "the amount typed, not the least"); Assert.AreEqual(63, p.gold);
            Assert.AreEqual(59, Exchange.MinBid(l), "a twentieth over 57");
            var mage = Character("class.mage", 300);
            Assert.IsFalse(Exchange.Bid(w, mage, Db(), l.id, 58, out why), "under the least next bid");
            Assert.IsTrue(Exchange.Bid(w, mage, Db(), l.id, 250, out why), "at or over the buyout it buys outright: " + why);
            Assert.AreEqual(100, mage.gold, "for the buyout, not the 250 typed"); Assert.AreEqual(5, Inventory.Count(mage, "potion.minor")); Assert.IsFalse(w.exchange.listings.Contains(l));
            Assert.AreEqual(57, Exchange.Waiting(w.exchange, "class.warrior").Single().coin, "the warrior's bid came back");
            // A buyout-only lot takes no bid under the buyout, and a typed buyout buys it.
            var only = SimLot(w, "potion.minor", 2, 30, 0); var rich = Character("class.cleric", 100);
            Assert.IsFalse(Exchange.Bid(w, rich, Db(), only.id, 20, out why)); Assert.IsTrue(Exchange.Bid(w, rich, Db(), only.id, 30, out why), why); Assert.AreEqual(70, rich.gold);
        }

        [Test] public void Gear_fits_a_sim_by_class_weight_level_and_slot()
        {
            var w = World(); var warrior = Sim(w, 0, "class.warrior", 10); var mage = Sim(w, 1, "class.mage", 10); var ranger = Sim(w, 2, "class.ranger", 10); var young = Sim(w, 3, "class.warrior", 2);
            var hauberk = Db().Get("craft.bogiron_hauberk"); var robe = Db().Get("craft.bogiron_wool_robe"); var jerkin = Db().Get("craft.bogiron_hide_jerkin");
            Assert.IsTrue(SimGear.Fits(warrior, hauberk, Trades()), "mail for the mail classes"); Assert.IsFalse(SimGear.Fits(mage, hauberk, Trades())); Assert.IsFalse(SimGear.Fits(young, hauberk, Trades()), "level 8: not at 2");
            Assert.IsTrue(SimGear.Fits(mage, robe, Trades()), "cloth for the cloth classes"); Assert.IsFalse(SimGear.Fits(warrior, robe, Trades()));
            Assert.IsTrue(SimGear.Fits(ranger, jerkin, Trades()), "leather for the leather classes"); Assert.IsFalse(SimGear.Fits(mage, jerkin, Trades()));
            Assert.IsTrue(SimGear.Fits(mage, Db().Get("craft.bogiron_torc"), Trades()), "a neck piece is anyone's");
            Assert.IsTrue(SimGear.Fits(warrior, Db().Get("craft.bogiron_hatchet"), Trades()), "a hatchet for the Warrior");
            Assert.IsFalse(SimGear.Fits(warrior, Db().Get("craft.bogiron_bow"), Trades()), "no bow: the Warrior's is a blade");
            Assert.IsFalse(SimGear.Fits(ranger, Db().Get("craft.bogiron_hatchet"), Trades()), "the others keep their own weapons");
            // An upgrade beats what it wears in the slot.
            SimEconomy.Wear(warrior, "chest", "craft.copper_jerkin");
            Assert.AreEqual("craft.copper_jerkin", SimGear.InSlot(warrior, "chest", Db()));
            Assert.IsTrue(SimGear.IsUpgrade(warrior, hauberk, Db(), Trades())); Assert.IsFalse(SimGear.IsUpgrade(warrior, Db().Get("craft.copper_jerkin"), Db(), Trades()), "not what it wears");
            SimEconomy.Wear(warrior, "chest", "craft.bogiron_hauberk"); Assert.IsFalse(SimGear.IsUpgrade(warrior, Db().Get("craft.copper_jerkin"), Db(), Trades()), "not a step down");
        }

        [Test] public void Your_gear_is_bought_by_a_sim_it_fits_and_betters_who_then_wears_it()
        {
            var w = World(); var warrior = Sim(w, 0, "class.warrior", 10); for (int i = 1; i < 6; i++) Sim(w, i, "class.mage", 10);
            SimEconomy.Wear(warrior, "chest", "craft.copper_jerkin");
            var p = Character("class.paladin", 100); Inventory.Add(p, Db(), "craft.bogiron_hauberk", 1);
            int fair = Exchange.Fair(Db().Get("craft.bogiron_hauberk"), 1);
            var l = Exchange.List(w, p, Db(), p.bag.FindIndex(s => s.item == "craft.bogiron_hauberk"), fair, 0, 48, out var why); Assert.NotNull(l, why);
            var dressed = new List<string>(); Exchange.Wore = s => dressed.Add(s.id);
            try { Exchange.Run(w, 47, 12, Db(), Trades()); } finally { Exchange.Wore = null; }
            Assert.IsFalse(w.exchange.listings.Contains(l), "a fair piece that betters a warrior sells");
            Assert.AreEqual("craft.bogiron_hauberk", SimEconomy.Worn(warrior, "chest"), "the warrior wears it"); CollectionAssert.Contains(SimGear.For(warrior, Db()), "craft.bogiron_hauberk", "and is seen in it");
            CollectionAssert.Contains(dressed, warrior.id, "its figure is dressed again");
            Assert.AreEqual(500 - fair, warrior.coin); Assert.AreEqual(fair + l.deposit, Exchange.Waiting(w.exchange, "class.paladin").Single().coin);
            bool spare = SimEconomy.Count(warrior, "craft.copper_jerkin") == 1 || w.exchange.listings.Exists(o => o.seller == warrior.id && o.item == "craft.copper_jerkin");
            Assert.IsTrue(spare, "the jerkin it took off is a spare to sell (or up on the Exchange)");
            foreach (var m in w.sims.Skip(1)) Assert.IsNull(SimEconomy.Worn(m, "chest"), "no mage put on mail");
            // A cloth robe finds no buyer among warriors.
            var w2 = World(); var p2 = Character("class.mage", 100); Inventory.Add(p2, Db(), "craft.bogiron_wool_robe", 1);
            foreach (var s in w2.sims) { s.level = 10; }
            var robe = Exchange.List(w2, p2, Db(), 0, Exchange.Fair(Db().Get("craft.bogiron_wool_robe"), 1), 0, 48, out why); Assert.NotNull(robe, why);
            Exchange.Run(w2, 47, 12, Db(), Trades());
            Assert.IsTrue(w2.exchange.listings.Contains(robe), "no warrior buys a robe");
        }

        [Test] public void Sims_buy_from_each_other_what_they_want_and_the_market_moves()
        {
            // Gear: a warrior buys another sim's hauberk and wears it.
            var w = World(); foreach (var s in w.sims) Sim(w, w.sims.IndexOf(s), "class.mage", 1);
            var warrior = Sim(w, 1, "class.warrior", 10); SimEconomy.Wear(warrior, "chest", "craft.copper_jerkin");
            int fair = Exchange.Fair(Db().Get("craft.bogiron_hauberk"), 1);
            var hauberk = SimLot(w, "craft.bogiron_hauberk", 1, fair, 0, 48);
            int before = w.sims.Sum(s => s.coin);
            Exchange.Run(w, 47, 12, Db(), Trades());
            Assert.IsFalse(w.exchange.listings.Contains(hauberk), "the warrior bought it");
            Assert.AreEqual("craft.bogiron_hauberk", SimEconomy.Worn(warrior, "chest"));
            Assert.AreEqual(500 + fair, w.sims[0].coin, "the seller was paid"); Assert.AreEqual(before, w.sims.Sum(s => s.coin), "coin moved, none made");
            Assert.AreEqual(fair, Exchange.LastSold(w.exchange, "craft.bogiron_hauberk"), "the sale is noted");
            // Makings: a smith buys ore for its bars; a mage (a weaver) does not.
            var w2 = World(); for (int i = 0; i < 6; i++) Sim(w2, i, i == 2 ? "class.warrior" : "class.mage", i == 2 ? 3 : 1);   // a level-1 weaver's cloth takes no ore
            var ore = SimLot(w2, "mat.copper_ore", 10, Exchange.Fair(Db().Get("mat.copper_ore"), 10), 0, 48);
            Exchange.Run(w2, 47, 12, Db(), Trades());
            Assert.IsFalse(w2.exchange.listings.Contains(ore), "the smith wanted the ore");
            int smithHas = SimEconomy.Count(w2.sims[2], "mat.copper_ore") + w2.exchange.listings.Where(o => o.seller == w2.sims[2].id && o.item == "mat.copper_ore").Sum(o => o.count);
            Assert.AreEqual(10, smithHas, "the smith has it (or has put spare ore up again)");
            // The pack for the bosses: a sim lacking its dish buys one off the Exchange.
            var w3 = World(); foreach (var s in w3.sims) s.level = 5;
            var dish = BossBuffs.Pack("class.warrior", 5, Db()).Find(e => BossBuffs.IsDish(Db().Get(e.id))).id; Assert.NotNull(dish, "a level-5 dish");
            var dishLot = SimLot(w3, dish, 1, Exchange.Fair(Db().Get(dish), 1), 0, 48);
            Exchange.Run(w3, 47, 12, Db(), Trades());
            Assert.IsFalse(w3.exchange.listings.Contains(dishLot), "bought for a pack");
            Assert.AreEqual(1, w3.sims.Skip(1).Sum(s => SimEconomy.Count(s, dish)), "and kept in the pack, not put up again");
            // Wanted by nobody: a leaf is never bought by a sim, and goes up again cheaper.
            var w4 = World(); var leaf = SimLot(w4, "leaf.cordial_ox", 1, Exchange.Fair(Db().Get("leaf.cordial_ox"), 1), 0, 24); int first = leaf.buyout;
            Exchange.Run(w4, 25, 12, Db(), Trades());
            Assert.IsTrue(w4.exchange.listings.Contains(leaf), "unsold, it went up again"); Assert.AreEqual(1, leaf.relisted); Assert.Less(leaf.buyout, first, "cheaper");
        }

        [Test] public void An_unsold_sim_lot_goes_up_again_cheaper_down_to_the_stalls_price_then_home()
        {
            var w = World(online: 0); var l = SimLot(w, "mat.copper_ore", 20, 40, 20, 24);
            Exchange.Run(w, 24, 12, Db());
            Assert.IsTrue(w.exchange.listings.Contains(l)); Assert.AreEqual(1, l.relisted); Assert.AreEqual(34, l.buyout, "15% off"); Assert.AreEqual(17, l.startBid, "its starting bid too");
            Assert.AreEqual(48, l.endsAt, 1e-4f, "for another day");
            Exchange.Run(w, 24, 12, Db()); Assert.AreEqual(29, l.buyout);
            Exchange.Run(w, 24, 12, Db()); Assert.AreEqual(25, l.buyout); Assert.AreEqual(3, l.relisted);
            Exchange.Run(w, 24, 12, Db());
            Assert.IsFalse(w.exchange.listings.Contains(l), "three times at most"); Assert.AreEqual(20, SimEconomy.Count(w.sims[0], "mat.copper_ore"), "back to its seller");
            // At the stall's price already: home at once.
            var cheap = SimLot(w, "mat.copper_ore", 20, 20, 0, 24);
            Exchange.Run(w, 24, 12, Db()); Assert.IsFalse(w.exchange.listings.Contains(cheap)); Assert.AreEqual(40, SimEconomy.Count(w.sims[0], "mat.copper_ore"));
        }

        [Test] public void The_sims_lots_stay_bounded_and_a_crowded_old_market_is_trimmed()
        {
            var w = new WorldSave { seed = 3 };
            string[] goods = { "mat.copper_ore", "mat.oak_log", "mat.yarrow", "junk.wolf_pelt", "hide.boar", "mat.mourners_cap", "mat.salt", "mat.flour" };
            for (int i = 0; i < 40; i++)
            {
                var s = new SimAdventurer { id = "sim." + i, name = "Sim " + i, classId = "class.mage", level = 1, onlineFrom = 0, onlineHours = 24, coin = 0 };
                foreach (var g in goods) SimEconomy.Add(s, g, 30);
                w.sims.Add(s);
            }
            Exchange.Ensure(w); int most = 0;
            for (int h = 0; h < 72; h++)
            {
                Exchange.Run(w, 1, h % 24, Db(), Trades());
                int n = w.exchange.listings.Count(l => !Exchange.IsYours(l)); most = Math.Max(most, n);
                Assert.LessOrEqual(n, Exchange.MaxSimListings, "hour " + h);
            }
            Assert.GreaterOrEqual(most, Exchange.MinSimListings, "a full market with goods to sell");
            // An older world with more lots than the bound: the oldest go home.
            var x = w.exchange;
            while (x.listings.Count(l => !Exchange.IsYours(l)) < Exchange.MaxSimListings + 30) x.listings.Add(new ExchangeListing { id = x.nextId++, item = "mat.salt", count = 1, seller = "sim.0", sellerName = "Sim 0", buyout = 9, listedAt = x.hours, endsAt = x.hours + 48 });
            int Total() { return w.sims.Sum(s => SimEconomy.GoodsCount(s)) + x.listings.Sum(l => l.count); }
            int before = Total();
            Exchange.Run(w, 1, 1, Db(), Trades());
            Assert.LessOrEqual(x.listings.Count(l => !Exchange.IsYours(l)), Exchange.MaxSimListings);
            Assert.AreEqual(before, Total(), "trimmed lots went back to their sellers: nothing lost");
        }

        [Test] public void Sims_list_their_crafted_goods_and_leaves_but_keep_their_pack()
        {
            var w = World(); var s = Sim(w, 0, "class.warrior", 5); for (int i = 1; i < 6; i++) w.sims[i].onlineHours = 0;   // only the lister online
            var dish = BossBuffs.Pack("class.warrior", 5, Db()).Find(e => BossBuffs.IsDish(Db().Get(e.id))).id; Assert.NotNull(dish);
            SimEconomy.Add(s, dish, 2); SimEconomy.Add(s, "leaf.cordial_ox", 1); SimEconomy.Add(s, "craft.copper_hide_jerkin", 1); SimEconomy.Add(s, "food.boar_stew", 6);
            var seen = new HashSet<string>();
            for (int h = 0; h < 300; h++) { Exchange.Run(w, 1, 12, Db(), Trades()); foreach (var l in w.exchange.listings) if (l.seller == s.id) seen.Add(l.item); }
            CollectionAssert.Contains(seen, "leaf.cordial_ox", "a found leaf"); CollectionAssert.Contains(seen, "craft.copper_hide_jerkin", "a spare piece"); CollectionAssert.Contains(seen, "food.boar_stew", "a cooked dish");
            Assert.AreEqual(2, SimEconomy.Count(s, dish), "its pack's two dishes are never listed"); CollectionAssert.DoesNotContain(seen, dish);
            // The goods they come by: a leaf turned up on the hunt, meat cooked at the hearth.
            Assert.NotNull(SimEconomy.LeafFor(5, Trades(), Db(), 7)); Assert.IsTrue(SimEconomy.IsLeaf(Db().Get(SimEconomy.LeafFor(5, Trades(), Db(), 7))));
            var cook = new SimAdventurer { id = "sim.cook", name = "Cook", classId = "class.mage", level = 1 }; SimEconomy.Add(cook, "junk.boar_meat", 2);
            Assert.AreEqual("food.boar_stew", SimEconomy.Cook(cook, Trades(), Db())); Assert.AreEqual(1, SimEconomy.Count(cook, "food.boar_stew")); Assert.AreEqual(0, SimEconomy.Count(cook, "junk.boar_meat"));
            Assert.IsNull(SimEconomy.Cook(cook, Trades(), Db()), "no meat, no dish");
            SimEconomy.Add(w.sims[1], "leaf.cordial_ox", 1); SimEconomy.SellAll(w.sims[1], Db());
            Assert.AreEqual(1, SimEconomy.Count(w.sims[1], "leaf.cordial_ox"), "a leaf is not sold at the stall: it is for the Exchange");
        }

        [Test] public void Notices_tell_you_once_of_a_sale_an_expiry_and_being_outbid()
        {
            var w = World(); var p = Character("class.warrior", 200); Inventory.Add(p, Db(), "junk.wolf_pelt", 10);
            var l = Exchange.List(w, p, Db(), 0, 40, 0, 48, out var why); Assert.NotNull(l, why);
            Exchange.Run(w, 47, 12, Db());
            var news = Exchange.TakeNotices(w.exchange, "class.warrior");
            Assert.AreEqual(1, news.Count); StringAssert.StartsWith("Sold Grey wolf pelt x10", news[0]); StringAssert.Contains("waits at the clerk", news[0]);
            Assert.AreEqual(0, Exchange.TakeNotices(w.exchange, "class.warrior").Count, "told once");
            // Ran out unsold, and outbid: both told; another character's news waits for them.
            var idle = World(online: 0); Inventory.Add(p, Db(), "junk.wolf_pelt", 5);
            var dear = Exchange.List(idle, p, Db(), p.bag.FindIndex(s => s.item == "junk.wolf_pelt"), 400, 0, 12, out why); Assert.NotNull(dear, why);
            var mageLot = SimLot(idle, "potion.minor", 5, 100, 20, 48); var mage = Character("class.mage", 100);
            Assert.IsTrue(Exchange.Bid(idle, mage, Db(), mageLot.id, 20, out why), why);
            Exchange.Run(idle, 12, 12, Db());
            var warriorNews = Exchange.TakeNotices(idle.exchange, "class.warrior"); Assert.AreEqual(1, warriorNews.Count); StringAssert.Contains("did not sell", warriorNews[0]);
            Assert.AreEqual(0, Exchange.TakeNotices(idle.exchange, "class.mage").Count, "nobody online: not outbid yet");
            foreach (var s in idle.sims) s.onlineHours = 24;
            Exchange.Run(idle, 40, 12, Db());   // the lot runs out at 48: outbid by then (most likely) or won
            var mageNews = Exchange.TakeNotices(idle.exchange, "class.mage");
            Assert.IsTrue(mageNews.Exists(n => n.StartsWith("Outbid") || n.StartsWith("Won")), "the mage hears it was outbid, or that it won: " + string.Join(" | ", mageNews));
            // Your own buying is no news; an older history line is never told.
            var buy = SimLot(w, "potion.minor", 1, 5, 0); Assert.IsTrue(Exchange.BuyOut(w, p, Db(), buy.id, out why), why);
            w.exchange.history.Add(new ExchangeRecord { owner = "class.warrior", line = "Sold something long ago." });
            Assert.AreEqual(0, Exchange.TakeNotices(w.exchange, "class.warrior").Count);
        }

        [Test] public void Last_sold_prices_are_kept_a_few_an_item_and_bounded_in_all()
        {
            var w = World(online: 0); var p = Character("class.warrior", 1000);
            Assert.AreEqual(-1, Exchange.LastSold(w.exchange, "potion.minor"), "never sold");
            var a = SimLot(w, "potion.minor", 5, 40, 0); Assert.IsTrue(Exchange.BuyOut(w, p, Db(), a.id, out var why), why);
            Assert.AreEqual(8, Exchange.LastSold(w.exchange, "potion.minor"), "the price for one");
            for (int i = 0; i < 6; i++) { var b = SimLot(w, "potion.minor", 1, 10 + i, 0); Assert.IsTrue(Exchange.BuyOut(w, p, Db(), b.id, out why), why); }
            Assert.AreEqual(15, Exchange.LastSold(w.exchange, "potion.minor"), "the latest");
            Assert.AreEqual(Exchange.SalesPerItem, w.exchange.sales.Count(o => o.item == "potion.minor"), "a few an item");
            for (int i = 0; i < Exchange.SalesKept + 20; i++) w.exchange.sales.Add(new ExchangeSale { item = "gen.x." + i, each = 1 });
            var c = SimLot(w, "junk.wolf_pelt", 2, 6, 0); Assert.IsTrue(Exchange.BuyOut(w, p, Db(), c.id, out why), why);
            Assert.AreEqual(Exchange.SalesKept, w.exchange.sales.Count, "bounded in all"); Assert.AreEqual(3, Exchange.LastSold(w.exchange, "junk.wolf_pelt"));
        }
    }
}
