using System;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Scoundrel's charm outside a fight (Docs/CLASS_SCOUNDREL.md; Chris 2026-10-13: "charisma for songs/merchants/wheeling and
    /// dealing"; CANON-EXPANDED: Valen was once "a man with a cart and a dream" (book 2 ch. 6) and the Glimmer is the sound of trade
    /// (book 3 ch. 11); the numbers GAME-ONLY): a village merchant asks a tenth less and pays a tenth more, and an innkeeper lets slip a
    /// rumour (VillageLife.Rumour), and haggles once a day per merchant for a further twentieth off that day (Haggle: kept for the session, not in the
    /// save, 2026-10-14). Sims trade as they always did.
    /// </summary>
    public static class Charm
    {
        public const string ClassId = "class.scoundrel";
        public const double BuyShare = .9, SellShare = 1.1;
        /// <summary>Whether this character has the charm (a Scoundrel).</summary>
        public static bool Has(EncounterProgress p) { return p != null && p.classId == ClassId; }
        /// <summary>What a merchant asks of you for <paramref name="price"/>: a tenth off for a Scoundrel, rounded, never under 1.</summary>
        public static int Ask(EncounterProgress p, int price) { return Has(p) && price > 0 ? Math.Max(1, (int)Math.Round(price * (BuyShare - (HaggledToday(Current) ? HaggleShare : 0)), MidpointRounding.AwayFromZero)) : price; }
        /// <summary>A haggle's further share off, with the merchant haggled with today.</summary>
        public const double HaggleShare = .05;
        static readonly System.Collections.Generic.Dictionary<string, DateTime> haggled = new System.Collections.Generic.Dictionary<string, DateTime>();
        /// <summary>The merchant whose window is open (null: none).</summary>
        public static string Current;
        public static bool HaggledToday(string merchant) { return merchant != null && haggled.TryGetValue(merchant, out var d) && d == DateTime.Today; }
        /// <summary>A Scoundrel opening a merchant's window: the first time today with this one, a haggle (true: say so).</summary>
        public static bool Haggle(EncounterProgress p, string merchant)
        {
            Current = merchant;
            if (!Has(p) || merchant == null || HaggledToday(merchant)) return false;
            haggled[merchant] = DateTime.Today; return true;
        }
        /// <summary>Tests: forget today's haggles.</summary>
        public static void ForgetHaggles() { haggled.Clear(); Current = null; }
        /// <summary>What a merchant pays you for goods worth <paramref name="gold"/>: a tenth more for a Scoundrel, rounded.</summary>
        public static int Pay(EncounterProgress p, int gold) { return Has(p) && gold > 0 ? (int)Math.Round(gold * SellShare, MidpointRounding.AwayFromZero) : gold; }
    }
}
