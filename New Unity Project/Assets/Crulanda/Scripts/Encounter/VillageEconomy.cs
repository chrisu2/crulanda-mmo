using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A household's purse (GAME-ONLY; tools/wip/professions/ADDENDUM.md C): its coin, the stipend that comes in each morning from
    /// outside the village, what it buys in order (<see cref="needs"/>), and how today stands with each need: claimed (the coin set
    /// aside while somebody fetches it), met (bought, or nothing to buy), said (the short-of-coin line has been spoken). cold: the
    /// house went without firewood, and its chimney stays out until the next.
    /// </summary>
    public sealed class Purse
    {
        public string household; public int coin, stipend, bonus; public string[] needs = VillageEconomy.Goods;
        /// <summary>False for a household that does not shop (the inn's: its kitchen feeds it).</summary>
        public bool shops = true;
        /// <summary>The trades of its members, and the goods those trades make (never bought).</summary>
        public readonly HashSet<string> roles = new HashSet<string>(), makes = new HashSet<string>();
        public readonly HashSet<string> met = new HashSet<string>(), claimed = new HashSet<string>(), metYesterday = new HashSet<string>(), said = new HashSet<string>();
        public bool cold;
        /// <summary>The day's stipend with the bonus of the bags the player wears (for the leatherworker's household).</summary>
        public int Stipend { get { return stipend + bonus; } }
        /// <summary>Coin set aside for claimed needs.</summary>
        public int Reserved { get { int n = 0; foreach (var c in claimed) n += VillageEconomy.Price(c); return n; } }
        /// <summary>Coin not yet set aside.</summary>
        public int Free { get { return coin - Reserved; } }
    }

    /// <summary>
    /// The village's purses (GAME-ONLY; ADDENDUM C), one per household, with no scene behind them. Money comes in by the daily
    /// stipend and by the player; it moves between households only when one buys bread, firewood or eggs from the trade that
    /// sells them (deliveries between the trades carry no price). Each morning a purse takes its stipend (up to <see cref="Cap"/>)
    /// and plans: it walks its needs in order and claims each one it can afford, stopping at the first it cannot. A claim is paid,
    /// and the need met, when the goods are picked up (<see cref="Settle"/>); a claim let go before then is released.
    /// Not saved: the table lives for the play session, keyed by the character and the zone, and is cleared when another
    /// character plays, at the start of play and by <see cref="ResetAll"/>. What lasts is carried by the saved bags: each trade
    /// bag the character wears adds <see cref="BagStipend"/> a day to the leatherworker's household and <see cref="BagStart"/> to
    /// its first purse of a session.
    /// </summary>
    public sealed class VillageEconomy
    {
        /// <summary>No purse holds more; what is paid past it goes to the tithe-man.</summary>
        public const int Cap = 40;
        public const int BagStipend = 2, BagStart = 6;
        /// <summary>What a household can buy, in the default order.</summary>
        public static readonly string[] Goods = { "bread", "firewood", "eggs" };
        public static int Price(string need)
        {
            switch (need) { case "bread": return 2; case "firewood": return 3; case "eggs": return 1; default: return 0; }
        }
        /// <summary>What a trade makes for its own household, so never buys: the baker bread, the woodcutter and the hunter firewood,
        /// the hen-wife and the merchant eggs. Null for the rest.</summary>
        public static string Makes(string role)
        {
            switch (role) { case "baker": return "bread"; case "lumberjack": case "hunter": return "firewood"; case "henwife": case "merchant": return "eggs"; default: return null; }
        }
        /// <summary>The trade whose household is paid for a need: bread the baker's, firewood the woodcutter's, eggs a merchant's.</summary>
        public static string SoldBy(string need)
        {
            switch (need) { case "bread": return "baker"; case "firewood": return "lumberjack"; case "eggs": return "merchant"; default: return null; }
        }

        // ---------- the table: one economy per character and zone, for the play session ----------
        static readonly Dictionary<string, VillageEconomy> table = new Dictionary<string, VillageEconomy>();
        static string character;
        /// <summary>Test hook: when set, the coin every purse starts with on first use (instead of its stipend). Cleared by <see cref="ResetAll"/>.</summary>
        public static int? StartingCoin;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetOnPlay() { ResetAll(); }
        /// <summary>Forget every purse (village tests call it in teardown).</summary>
        public static void ResetAll() { table.Clear(); character = null; StartingCoin = null; }
        /// <summary>The economy of a zone for a character (its save folder and slot); another character clears the table first.</summary>
        public static VillageEconomy For(string characterKey, string zone)
        {
            if (characterKey != character) { table.Clear(); character = characterKey; }
            if (!table.TryGetValue(zone ?? "", out var e)) table[zone ?? ""] = e = new VillageEconomy();
            return e;
        }

        readonly List<Purse> purses = new List<Purse>();
        public IReadOnlyList<Purse> Purses { get { return purses; } }
        public Purse PurseOf(string household) { return purses.Find(p => p.household == household); }
        /// <summary>The hour at the last <see cref="Turned"/> (below 0 before the first).</summary>
        public float LastHour = -1;
        /// <summary>True when the hour has dropped by more than six since the last call: midnight has passed (or the clock was set
        /// back to the morning), as <c>Villager.StartErrand</c> tells a new day.</summary>
        public bool Turned(float hour) { bool turned = LastHour >= 0 && hour < LastHour - 6; LastHour = hour; return turned; }

        /// <summary>
        /// A household's purse, made on first use with its stipend (plus <see cref="BagStart"/> a worn bag) as coin, or the one it
        /// already has (its coin and today's state kept, its stipend, needs and trades brought up to date). roles are its members'
        /// trades; bags the trade bags the player wears, for the leatherworker's household (0 for every other).
        /// </summary>
        public Purse Open(string household, int stipend, string[] needs, IEnumerable<string> roles, int bags = 0)
        {
            var p = PurseOf(household); bool fresh = p == null;
            if (fresh) { p = new Purse { household = household }; purses.Add(p); }
            p.stipend = Mathf.Max(0, stipend); p.needs = needs != null && needs.Length > 0 ? needs : Goods; p.bonus = BagStipend * Mathf.Max(0, bags);
            p.roles.Clear(); p.makes.Clear();
            if (roles != null) foreach (var r in roles) { if (r == null) continue; p.roles.Add(r); var m = Makes(r); if (m != null) p.makes.Add(m); }
            p.shops = !p.roles.Contains("innkeeper");
            if (fresh) p.coin = Mathf.Clamp(StartingCoin ?? p.Stipend + BagStart * Mathf.Max(0, bags), 0, Cap);
            return p;
        }
        /// <summary>The bags the player wears, for the leatherworker's household's stipend from the next morning.</summary>
        public void SetBags(string household, int bags) { var p = PurseOf(household); if (p != null) p.bonus = BagStipend * Mathf.Max(0, bags); }

        /// <summary>The household paid for a need: the first with a member of the selling trade. Eggs need hens as well (a merchant
        /// sells the hen-wife's eggs). Null when the zone has none, and the need counts as met.</summary>
        public Purse SellerOf(string need)
        {
            if (need == "eggs" && !purses.Exists(x => x.roles.Contains("henwife"))) return null;
            string role = SoldBy(need); return role == null ? null : purses.Find(x => x.roles.Contains(role));
        }
        /// <summary>Whether a household buys this need at all: it is one of its needs, it shops, its own trade does not make it,
        /// and someone in the zone sells it.</summary>
        public bool Live(Purse p, string need)
        {
            if (p == null || !p.shops || System.Array.IndexOf(p.needs, need) < 0 || p.makes.Contains(need)) return false;
            var seller = SellerOf(need); return seller != null && seller != p;
        }
        /// <summary>
        /// Walk the needs in order: one met or claimed already is passed over, one the household does not buy counts as met, and
        /// each other is claimed while the free coin covers it; the first it cannot cover stops the walk. Returns what was claimed now.
        /// </summary>
        public List<string> Plan(Purse p)
        {
            var added = new List<string>(); if (p == null) return added;
            foreach (var need in p.needs)
            {
                if (p.met.Contains(need) || p.claimed.Contains(need)) continue;
                if (!Live(p, need)) { p.met.Add(need); continue; }
                if (p.Free < Price(need)) break;
                p.claimed.Add(need); added.Add(need);
            }
            return added;
        }
        public void PlanAll() { foreach (var p in purses) Plan(p); }
        /// <summary>The need a household is short of: the first in order neither met nor claimed, when it buys it and cannot cover
        /// it. Null when nothing is wanting.</summary>
        public string ShortOf(Purse p)
        {
            if (p == null) return null;
            foreach (var need in p.needs)
            {
                if (p.met.Contains(need) || p.claimed.Contains(need)) continue;
                return Live(p, need) && p.Free < Price(need) ? need : null;
            }
            return null;
        }
        /// <summary>Pay a claim at pick-up: the coin goes to the seller's household (which plans again), and the need is met. False
        /// (nothing changes) when the need was not claimed; a purse never goes below nothing.</summary>
        public bool Settle(Purse p, string need)
        {
            if (p == null || !p.claimed.Contains(need)) return false;
            int price = Price(need); if (p.coin < price) { p.claimed.Remove(need); return false; }
            p.coin -= price; p.claimed.Remove(need); p.met.Add(need);
            var seller = SellerOf(need); if (seller != null && seller != p) Earn(seller, price, true, out _);
            return true;
        }
        /// <summary>Let a claim go (the errand was given up before pick-up): its coin is free again.</summary>
        public void Release(Purse p, string need) { if (p != null) p.claimed.Remove(need); }
        /// <summary>Coin into a purse, up to the cap; with <paramref name="plan"/> the household plans again (claimed: what it set out
        /// for). Returns what went past the cap.</summary>
        public int Earn(Purse p, int coin, bool plan, out List<string> claimed)
        {
            claimed = null; if (p == null || coin <= 0) return 0;
            int over = Mathf.Max(0, p.coin + coin - Cap); p.coin = Mathf.Min(p.coin + coin, Cap);
            if (plan) claimed = Plan(p);
            return over;
        }
        /// <summary>A new day: yesterday's met kept for what people say, today's cleared with any claim not picked up, the stipend
        /// in (up to the cap), and every household plans.</summary>
        public void NewDay()
        {
            foreach (var p in purses)
            {
                p.metYesterday.Clear(); p.metYesterday.UnionWith(p.met); p.met.Clear(); p.claimed.Clear(); p.said.Clear();
                p.coin = Mathf.Min(p.coin + p.Stipend, Cap);
            }
            PlanAll();
        }
    }
}
