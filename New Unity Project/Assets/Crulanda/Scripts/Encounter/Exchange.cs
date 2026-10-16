using System;
using System.Collections.Generic;

namespace Crulanda.Encounter
{
    /// <summary>
    /// One lot on the Exchange: what (item and count), whose (a sim's id and name, or "" for yours, with the character that listed
    /// it in <see cref="owner"/>), its buyout for the whole lot, its starting bid (0: buyout only), the highest bid and who made it
    /// (a sim's id, or "you:" and your character's class id), your deposit, and the world-hours it went up and runs out
    /// (ExchangeState.hours).
    /// </summary>
    [Serializable] public sealed class ExchangeListing
    {
        public int id;
        public string item = ""; public int count = 1;
        public string seller = "", sellerName = "", owner = "";
        public int buyout, startBid, bid, deposit;
        public string bidder = "", bidderName = "";
        public float listedAt, endsAt;
    }
    /// <summary>What waits at the clerk for one of your characters (by class id): coin from a sale or a bid given back, or an item
    /// (one that did not sell, one taken back, one won), with a line saying why.</summary>
    [Serializable] public sealed class ExchangeParcel { public string owner = "", item = "", note = ""; public int coin, count; }
    /// <summary>A line of the Exchange's history for one of your characters (sold, bought, won, outbid, expired).</summary>
    [Serializable] public sealed class ExchangeRecord { public string owner = "", line = ""; public float at; }
    /// <summary>
    /// The Exchange in the world save (WorldSave.exchange, world format 2): the lots, what waits at the clerk, the history, and its
    /// own clock: <see cref="hours"/> world-hours since it opened (it only goes up), <see cref="tickedTo"/> the last whole hour run,
    /// <see cref="clockAt"/> the world-clock hour last seen and <see cref="realUtc"/> the real time last saved (the hours you were
    /// away are run on the next start, up to two world days).
    /// </summary>
    [Serializable] public sealed class ExchangeState
    {
        public int nextId = 1;
        public float hours, clockAt = -1;
        public int tickedTo;
        public string realUtc = "";
        public List<ExchangeListing> listings = new List<ExchangeListing>();
        public List<ExchangeParcel> parcels = new List<ExchangeParcel>();
        public List<ExchangeRecord> history = new List<ExchangeRecord>();
    }

    /// <summary>
    /// The Exchange (GAME-ONLY; CAPITAL_HUB.md, Chris 2026-10-15, the question prompt: "Sims list and buy (Recommended)"): the
    /// capital's market, single player, no real money. You list a stack from your bags for 12, 24 or 48 world-hours with a buyout and
    /// an optional starting bid, paying a small deposit that comes back when it sells; the sims list their goods (what they gather and
    /// brew, from SimEconomy) at its value times a markup by quality; each world-hour sims online buy your lots that are fairly
    /// priced (more often the more of them are online and the fairer the price; never at three times fair), bid on those with a
    /// starting bid, and now and then outbid you on theirs. Coin from a sale, bids given back, lots won and lots that did not sell
    /// all wait at the clerk (Collect). It runs by the world clock wherever you are (SimPopulation.Refresh) and for the hours you
    /// were away (SimPopulation.Init). Pure logic on the world save and the character; the session saves both.
    /// </summary>
    public static class Exchange
    {
        public static readonly int[] Durations = { 12, 24, 48 };
        /// <summary>Lots one character may have up at once, and the most the sims keep up between them.</summary>
        public const int MaxYourListings = 20, MaxSimListings = 60, HistoryKept = 60;
        /// <summary>The most world-hours run for time away from the game (two world days).</summary>
        public const float AwayHoursCap = 48;
        /// <summary>World-hours in a real hour: a world day is 40 real minutes (WorldClock.RealMinutesPerDay), so 36.</summary>
        public static float WorldHoursPerRealHour { get { return 24f * 60f / Crulanda.World.WorldClock.RealMinutesPerDay; } }
        /// <summary>A bid or a buyer that is you: "you:" and the character's class id.</summary>
        public const string You = "you:";
        public static string YouId(string classId) { return You + (classId ?? ""); }
        public static bool IsYours(ExchangeListing l) { return l != null && string.IsNullOrEmpty(l.seller); }
        public static bool YouBid(ExchangeListing l) { return l != null && l.bidder != null && l.bidder.StartsWith(You, StringComparison.Ordinal); }

        public static ExchangeState Ensure(WorldSave w)
        {
            if (w.exchange == null) w.exchange = new ExchangeState();
            var x = w.exchange;
            if (x.listings == null) x.listings = new List<ExchangeListing>();
            if (x.parcels == null) x.parcels = new List<ExchangeParcel>();
            if (x.history == null) x.history = new List<ExchangeRecord>();
            if (x.nextId < 1) x.nextId = 1;
            x.listings.RemoveAll(l => l == null || string.IsNullOrEmpty(l.item) || l.count <= 0);
            x.parcels.RemoveAll(c => c == null || (c.coin <= 0 && (string.IsNullOrEmpty(c.item) || c.count <= 0)));
            foreach (var l in x.listings) { if (l.seller == null) l.seller = ""; if (l.bidder == null) l.bidder = ""; if (l.owner == null) l.owner = ""; if (l.id >= x.nextId) x.nextId = l.id + 1; }
            return x;
        }

        // ---------- prices ----------
        static readonly float[] Markups = { 1.5f, 2f, 2.5f, 3f, 4f, 5f };
        /// <summary>A sim's markup on an item's value, by quality: poor 1.5x, common 2x, uncommon 2.5x, rare 3x, epic 4x, legendary 5x.</summary>
        public static float Markup(int quality) { return Markups[Math.Max(0, Math.Min(Markups.Length - 1, quality))]; }
        /// <summary>A fair price for a lot: what a sim asks (its value times the markup), for the whole count.</summary>
        public static int Fair(ItemDef d, int count) { return d == null ? 0 : Math.Max(1, (int)Math.Round(Math.Max(1, d.value) * Markup(d.quality) * Math.Max(1, count))); }
        /// <summary>The deposit to list a lot: a tenth of its value for each 12 hours (at least 1 crown), given back when it sells.</summary>
        public static int Deposit(ItemDef d, int count, int hours) { return d == null ? 0 : Math.Max(1, (int)Math.Round(Math.Max(1, d.value) * Math.Max(1, count) * .1f * Math.Max(12, hours) / 12f)); }
        /// <summary>The least the next bid may be: the starting bid, or a twentieth over the highest (at least 1 crown more).</summary>
        public static int MinBid(ExchangeListing l) { return l.bid > 0 ? l.bid + Math.Max(1, l.bid / 20) : Math.Max(1, l.startBid); }
        /// <summary>How fair a price is, 0-1: 1 at fair or under, falling to 0 at two and a half times fair.</summary>
        public static float Fairness(float ratio) { return ratio <= 1 ? 1 : ratio >= 2.5f ? 0 : (2.5f - ratio) / 1.5f; }
        /// <summary>The chance in a world-hour that a sim buys a lot at this price (ratio to fair), with this share of the sims online:
        /// .6 at fair with everyone on, half as much again under three-quarters of fair, nothing at two and a half times.</summary>
        public static float BuyChance(float ratio, float online) { return Math.Min(.9f, .6f * Math.Max(0, Math.Min(1, online)) * Fairness(ratio) * (ratio <= .75f ? 1.5f : 1)); }
        /// <summary>The chance in a world-hour that a sim outbids you, with everyone on and a fair next bid.</summary>
        public const float OutbidChance = .15f;

        // ---------- you ----------
        /// <summary>
        /// Lists a bag slot's whole stack: buyout at least 1 crown, a starting bid under the buyout (0 for buyout only), 12, 24 or 48
        /// hours, the deposit paid now. Refused (null and why) for quest items, an unknown item, too many lots up, or too little coin.
        /// </summary>
        public static ExchangeListing List(WorldSave w, EncounterProgress p, ItemDatabase db, int bagIndex, int buyout, int startBid, int hours, out string why)
        {
            why = null; var x = Ensure(w);
            if (db == null || p?.bag == null || bagIndex < 0 || bagIndex >= p.bag.Count || p.bag[bagIndex].Empty) { why = "Choose something from your bags."; return null; }
            var s = p.bag[bagIndex]; var d = db.Get(s.item);
            if (d == null) { why = "The clerk can't make out what that is."; return null; }
            if (TreasuryVault.IsQuestItem(d)) { why = "That belongs to a task. It can't be sold."; return null; }
            if (Array.IndexOf(Durations, hours) < 0) { why = "12, 24 or 48 hours."; return null; }
            if (buyout < 1) { why = "Set a buyout price."; return null; }
            if (startBid < 0 || startBid >= buyout) { why = "The starting bid must be under the buyout."; return null; }
            if (Mine(x, p.classId).Count >= MaxYourListings) { why = "You have " + MaxYourListings + " lots up already."; return null; }
            int deposit = Deposit(d, s.count, hours);
            if (p.gold < deposit) { why = "The deposit is " + TreasuryVault.Coin(deposit) + "."; return null; }
            p.gold -= deposit;
            var l = new ExchangeListing { id = x.nextId++, item = s.item, count = s.count, owner = p.classId ?? "", buyout = buyout, startBid = startBid,
                deposit = deposit, listedAt = x.hours, endsAt = x.hours + hours };
            p.bag[bagIndex] = new ItemStack();
            x.listings.Add(l);
            return l;
        }
        /// <summary>Takes a lot of yours down: the item waits at the clerk and the deposit is kept. Refused once someone has bid.</summary>
        public static bool Cancel(WorldSave w, EncounterProgress p, ItemDatabase db, int id, out string why)
        {
            why = null; var x = Ensure(w); var l = x.listings.Find(o => o.id == id);
            if (l == null || !IsYours(l) || l.owner != (p.classId ?? "")) { why = "That lot isn't yours to take down."; return false; }
            if (l.bid > 0) { why = "Someone has bid on it: it runs its time."; return false; }
            x.listings.Remove(l);
            Parcel(x, l.owner, 0, l.item, l.count, "Taken down: " + Lot(db, l) + " (the deposit is kept).");
            return true;
        }
        /// <summary>Buys a sim's lot outright: coin out, the lot into your bags (refused when they lack room), a bid of yours on it
        /// given back, the coin to the sim.</summary>
        public static bool BuyOut(WorldSave w, EncounterProgress p, ItemDatabase db, int id, out string why)
        {
            why = null; var x = Ensure(w); var l = x.listings.Find(o => o.id == id);
            if (l == null) { why = "That lot has gone."; return false; }
            if (IsYours(l)) { why = "That is one of your own lots."; return false; }
            int held = l.bidder == YouId(p.classId) ? l.bid : 0;   // your own bid on it counts towards the price
            if (p.gold + held < l.buyout) { why = "You need " + TreasuryVault.Coin(l.buyout) + "."; return false; }
            if (db == null || db.Get(l.item) == null || Inventory.Room(p, db, l.item) < l.count) { why = "Your bags are full."; return false; }
            if (YouBid(l) && held == 0) Parcel(x, l.bidder.Substring(You.Length), l.bid, "", 0, "Outbid on " + Lot(db, l) + ": your " + TreasuryVault.Coin(l.bid) + " came back.");
            p.gold += held; p.gold -= l.buyout;
            Inventory.Add(p, db, l.item, l.count);
            var seller = Sim(w, l.seller); if (seller != null) seller.coin += l.buyout;
            x.listings.Remove(l);
            Record(x, p.classId, "Bought " + Lot(db, l) + " from " + l.sellerName + " for " + TreasuryVault.Coin(l.buyout) + ".");
            return true;
        }
        /// <summary>Bids on a sim's lot: at least <see cref="MinBid"/>, the coin held until you are outbid (it comes back at the clerk)
        /// or win (the lot waits at the clerk). A bid at or over the buyout buys it outright.</summary>
        public static bool Bid(WorldSave w, EncounterProgress p, ItemDatabase db, int id, int amount, out string why)
        {
            why = null; var x = Ensure(w); var l = x.listings.Find(o => o.id == id);
            if (l == null) { why = "That lot has gone."; return false; }
            if (IsYours(l)) { why = "That is one of your own lots."; return false; }
            if (l.startBid <= 0) { why = "Buyout only: no bids on this one."; return false; }
            if (l.bidder == YouId(p.classId)) { why = "Yours is the highest bid already."; return false; }
            if (amount >= l.buyout) return BuyOut(w, p, db, id, out why);
            int min = MinBid(l);
            if (amount < min) { why = "The least bid now is " + TreasuryVault.Coin(min) + "."; return false; }
            if (p.gold < amount) { why = "You need " + TreasuryVault.Coin(amount) + "."; return false; }
            if (YouBid(l)) Parcel(x, l.bidder.Substring(You.Length), l.bid, "", 0, "Outbid on " + Lot(db, l) + ": your " + TreasuryVault.Coin(l.bid) + " came back.");
            p.gold -= amount; l.bid = amount; l.bidder = YouId(p.classId); l.bidderName = "you";
            return true;
        }
        /// <summary>Takes what waits at the clerk for this character: coin into the purse, items into the bags (what does not fit
        /// stays waiting). Returns a line saying what was taken, or null when nothing waits.</summary>
        public static string Collect(WorldSave w, EncounterProgress p, ItemDatabase db)
        {
            var x = Ensure(w); string owner = p.classId ?? ""; int coin = 0, items = 0; bool full = false;
            foreach (var c in x.parcels.ToArray())
            {
                if (c.owner != owner) continue;
                coin += c.coin; p.gold += c.coin; c.coin = 0;
                if (!string.IsNullOrEmpty(c.item) && c.count > 0)
                {
                    int left = db != null && db.Get(c.item) != null ? Inventory.Add(p, db, c.item, c.count) : c.count;
                    items += c.count - left; c.count = left; if (left > 0) full = true;
                }
                if (c.coin <= 0 && (string.IsNullOrEmpty(c.item) || c.count <= 0)) x.parcels.Remove(c);
            }
            if (coin == 0 && items == 0) return full ? "Your bags are full." : null;
            return "Collected" + (coin > 0 ? " " + TreasuryVault.Coin(coin) : "") + (coin > 0 && items > 0 ? " and" : "") + (items > 0 ? " " + items + (items == 1 ? " item" : " items") : "") + "." + (full ? " Your bags are full: the rest waits." : "");
        }
        public static List<ExchangeListing> Mine(ExchangeState x, string classId) { return x.listings.FindAll(l => IsYours(l) && l.owner == (classId ?? "")); }
        public static List<ExchangeParcel> Waiting(ExchangeState x, string classId) { return x.parcels.FindAll(c => c.owner == (classId ?? "")); }
        public static List<ExchangeRecord> History(ExchangeState x, string classId) { return x.history.FindAll(r => r.owner == (classId ?? "")); }
        /// <summary>
        /// The lots for sale to you (the sims'), filtered: a name containing the text (any case), a slot (-1 any), a level range (gear
        /// and anything with a level), a quality (-1 any). Cheapest for each first, by name.
        /// </summary>
        public static List<ExchangeListing> Browse(ExchangeState x, ItemDatabase db, string text, int slot, int minLevel, int maxLevel, int quality)
        {
            var list = new List<ExchangeListing>(); if (db == null) return list;
            foreach (var l in x.listings)
            {
                if (IsYours(l)) continue;
                var d = db.Get(l.item); if (d == null) continue;
                if (!string.IsNullOrEmpty(text) && (d.name ?? "").IndexOf(text.Trim(), StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (slot >= 0 && ItemDatabase.SlotIndex(d.slot) != slot) continue;
                if (d.level < minLevel || (maxLevel > 0 && d.level > maxLevel)) continue;
                if (quality >= 0 && d.quality != quality) continue;
                list.Add(l);
            }
            list.Sort((a, b) =>
            {
                int n = string.CompareOrdinal(db.Get(a.item).name, db.Get(b.item).name); if (n != 0) return n;
                return (a.buyout / (float)Math.Max(1, a.count)).CompareTo(b.buyout / (float)Math.Max(1, b.count));
            });
            return list;
        }

        // ---------- the clock ----------
        /// <summary>The world clock moved on (SimPopulation.Refresh, each second): the hours since it was last seen are run.</summary>
        public static int Follow(WorldSave w, float clockHour, ItemDatabase db)
        {
            var x = Ensure(w);
            if (x.clockAt < 0) { x.clockAt = clockHour; return 0; }
            float d = Repeat(clockHour - x.clockAt, 24); x.clockAt = clockHour;
            return Run(w, d, clockHour, db);
        }
        /// <summary>A new session (SimPopulation.Init): the real time since the world was last saved runs as world-hours (36 a real
        /// hour, at most <see cref="AwayHoursCap"/>), and the clock is taken from here.</summary>
        public static int CatchUp(WorldSave w, DateTime nowUtc, float clockHour, ItemDatabase db)
        {
            var x = Ensure(w); int ran = 0;
            if (!string.IsNullOrEmpty(x.realUtc) && DateTime.TryParse(x.realUtc, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var then))
            {
                double away = (nowUtc - then.ToUniversalTime()).TotalHours * WorldHoursPerRealHour;
                if (away > 0) ran = Run(w, (float)Math.Min(AwayHoursCap, away), clockHour, db);
            }
            x.clockAt = clockHour; x.realUtc = nowUtc.ToString("O");
            return ran;
        }
        /// <summary>Notes the real time as the world is saved (SimPopulation.Persist).</summary>
        public static void Stamp(WorldSave w, DateTime nowUtc) { Ensure(w).realUtc = nowUtc.ToString("O"); }
        /// <summary>Runs this many world-hours ending at this world-clock hour: each whole hour passed is one market hour. Returns how many.</summary>
        public static int Run(WorldSave w, float hours, float clockHour, ItemDatabase db)
        {
            var x = Ensure(w); if (hours <= 0) return 0;
            x.hours += hours; int end = (int)Math.Floor(x.hours);
            if (x.tickedTo > end) x.tickedTo = end;
            if (end - x.tickedTo > 400) x.tickedTo = end - 400;   // never more than a long absence's worth at once
            int ran = 0;
            for (int k = x.tickedTo + 1; k <= end; k++) { Hour(w, k, Repeat(clockHour - (x.hours - k), 24), db); ran++; }
            x.tickedTo = end;
            return ran;
        }
        /// <summary>One market hour, at world-hour <paramref name="now"/> and this hour of the day: lots run out; the sims online buy
        /// and bid on yours, outbid you on theirs and list their goods.</summary>
        static void Hour(WorldSave w, int now, float hourOfDay, ItemDatabase db)
        {
            var x = w.exchange; var rng = new Random(unchecked(w.seed * 486187739 + now * 7919 + 17));
            for (int i = x.listings.Count - 1; i >= 0; i--) if (x.listings[i].endsAt <= now) { var l = x.listings[i]; x.listings.RemoveAt(i); Settle(w, l, db); }
            var online = new List<SimAdventurer>(); foreach (var s in w.sims) if (s != null && s.IsOnlineAt(hourOfDay)) online.Add(s);
            if (online.Count == 0) return;
            float share = online.Count / (float)w.sims.Count;
            foreach (var l in x.listings.ToArray())
            {
                if (!IsYours(l)) continue;
                int fair = Fair(db?.Get(l.item), l.count); if (fair <= 0) continue;
                var buyer = online[rng.Next(online.Count)];
                if (rng.NextDouble() < BuyChance(l.buyout / (float)fair, share)) { x.listings.Remove(l); SoldToSim(w, l, buyer, l.buyout, db); continue; }
                if (l.startBid > 0 && l.bidder != buyer.id)
                {
                    int b = MinBid(l);
                    if (b < l.buyout && rng.NextDouble() < .5f * BuyChance(b / (float)fair, share)) { l.bid = b; l.bidder = buyer.id; l.bidderName = buyer.name; }
                }
            }
            foreach (var l in x.listings.ToArray())
            {
                if (IsYours(l) || !YouBid(l)) continue;
                int fair = Fair(db?.Get(l.item), l.count); if (fair <= 0) continue;
                var rival = online[rng.Next(online.Count)]; if (rival.id == l.seller) continue;
                int b = MinBid(l);
                if (rng.NextDouble() >= OutbidChance * share * Fairness(b / (float)fair)) continue;
                Parcel(x, l.bidder.Substring(You.Length), l.bid, "", 0, "Outbid on " + Lot(db, l) + " by " + rival.name + ": your " + TreasuryVault.Coin(l.bid) + " came back.");
                Record(x, l.bidder.Substring(You.Length), "Outbid on " + Lot(db, l) + " by " + rival.name + ".", now);
                if (b >= l.buyout) { x.listings.Remove(l); SimTakes(w, l, rival, l.buyout, db); }
                else { l.bid = b; l.bidder = rival.id; l.bidderName = rival.name; }
            }
            int simLots = 0; foreach (var l in x.listings) if (!IsYours(l)) simLots++;
            foreach (var s in online)
            {
                if (simLots >= MaxSimListings) break;
                if (s.goodIds == null || s.goodIds.Count == 0 || rng.NextDouble() >= .1) continue;
                int j = rng.Next(s.goodIds.Count); string id = s.goodIds[j]; var d = db?.Get(id);
                if (d == null || d.kind == "gear" || TreasuryVault.IsQuestItem(d) || s.goodCounts[j] <= 0) continue;
                int n = Math.Min(s.goodCounts[j], Math.Max(1, d.stack)), price = Math.Max(1, (int)Math.Round(Fair(d, n) * (.9 + rng.NextDouble() * .25)));
                int start = rng.NextDouble() < .4 ? Math.Max(1, (int)Math.Round(price * .6)) : 0; if (start >= price) start = 0;
                SimEconomy.Add(s, id, -n);
                x.listings.Add(new ExchangeListing { id = x.nextId++, item = id, count = n, seller = s.id, sellerName = s.name, buyout = price, startBid = start,
                    listedAt = now, endsAt = now + (rng.Next(2) == 0 ? 24 : 48) });
                simLots++;
            }
        }
        /// <summary>A lot runs out: sold to the highest bid if it has one, else back to its seller (yours wait at the clerk; the
        /// deposit is kept).</summary>
        static void Settle(WorldSave w, ExchangeListing l, ItemDatabase db)
        {
            var x = w.exchange;
            if (IsYours(l))
            {
                if (l.bid > 0 && !string.IsNullOrEmpty(l.bidder)) { SoldToSim(w, l, Sim(w, l.bidder), l.bid, db, l.bidderName); return; }
                Parcel(x, l.owner, 0, l.item, l.count, Lot(db, l) + " did not sell; it waits here (the deposit is kept).");
                Record(x, l.owner, Lot(db, l) + " did not sell.", l.endsAt);
                return;
            }
            var seller = Sim(w, l.seller);
            if (YouBid(l))
            {
                string owner = l.bidder.Substring(You.Length);
                Parcel(x, owner, 0, l.item, l.count, "You won " + Lot(db, l) + " for " + TreasuryVault.Coin(l.bid) + ".");
                Record(x, owner, "Won " + Lot(db, l) + " from " + l.sellerName + " for " + TreasuryVault.Coin(l.bid) + ".", l.endsAt);
                if (seller != null) seller.coin += l.bid;
            }
            else if (l.bid > 0 && !string.IsNullOrEmpty(l.bidder)) SimTakes(w, l, Sim(w, l.bidder), l.bid, db);
            else if (seller != null) SimEconomy.Add(seller, l.item, l.count);
        }
        /// <summary>A lot of yours sold to a sim: its price and your deposit wait at the clerk; the sim pays (what it has) and keeps it.</summary>
        static void SoldToSim(WorldSave w, ExchangeListing l, SimAdventurer buyer, int price, ItemDatabase db, string buyerName = null)
        {
            string who = buyer != null ? buyer.name : buyerName ?? "a buyer";
            Parcel(w.exchange, l.owner, price + l.deposit, "", 0, "Sold " + Lot(db, l) + " to " + who + " for " + TreasuryVault.Coin(price) + " (and your " + TreasuryVault.Coin(l.deposit) + " deposit back).");
            Record(w.exchange, l.owner, "Sold " + Lot(db, l) + " to " + who + " for " + TreasuryVault.Coin(price) + ".", w.exchange.hours);
            if (buyer != null) { buyer.coin = Math.Max(0, buyer.coin - price); var d = db?.Get(l.item); if (d != null && d.kind != "gear") SimEconomy.Add(buyer, l.item, l.count); }
        }
        /// <summary>A sim's lot goes to another sim: the seller is paid, the buyer pays (what it has) and keeps it.</summary>
        static void SimTakes(WorldSave w, ExchangeListing l, SimAdventurer buyer, int price, ItemDatabase db)
        {
            var seller = Sim(w, l.seller); if (seller != null) seller.coin += price;
            if (buyer != null) { buyer.coin = Math.Max(0, buyer.coin - price); var d = db?.Get(l.item); if (d != null && d.kind != "gear") SimEconomy.Add(buyer, l.item, l.count); }
        }
        static void Parcel(ExchangeState x, string owner, int coin, string item, int count, string note)
        {
            x.parcels.Add(new ExchangeParcel { owner = owner ?? "", coin = Math.Max(0, coin), item = item ?? "", count = Math.Max(0, count), note = note ?? "" });
        }
        static void Record(ExchangeState x, string owner, string line, float at = -1)
        {
            x.history.Add(new ExchangeRecord { owner = owner ?? "", line = line, at = at < 0 ? x.hours : at });
            while (x.history.Count > HistoryKept) x.history.RemoveAt(0);
        }
        static SimAdventurer Sim(WorldSave w, string id) { return string.IsNullOrEmpty(id) ? null : w.sims.Find(s => s != null && s.id == id); }
        /// <summary>A lot in words: "Copper Ore x20", "Toll-road Blade".</summary>
        public static string Lot(ItemDatabase db, ExchangeListing l) { string n = db?.Get(l.item)?.name ?? l.item; return l.count > 1 ? n + " x" + l.count : n; }
        /// <summary>How long a lot has left, in world-hours, as the window says it: "31 h", "under 1 h".</summary>
        public static string TimeLeft(ExchangeState x, ExchangeListing l) { float h = l.endsAt - x.hours; return h < 1 ? "under 1 h" : (int)h + " h"; }
        static float Repeat(float t, float length) { return t - (float)Math.Floor(t / length) * length; }
    }
}
