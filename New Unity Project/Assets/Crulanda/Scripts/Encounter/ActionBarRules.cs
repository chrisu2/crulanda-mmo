using System;
using System.Collections.Generic;
using System.Linq;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The chosen action bar (Chris, 2026-10-13: "we should limit to 10 bar slots and they have to choose what spells/abilities at max level
    /// they can earn alternate advancement point to expand the slots"; Docs/ACTION_BAR.md). Ten slots, keys 1-0; at the level cap, experience
    /// past it earns alternate advancement (AA) points, one for each level's worth, which buy up to four more slots at 1, 2, 3 and 4 points
    /// (keys -, =, [ and ]). The bar holds book keys (an ability's id, or a kit's own key for a slot whose ability changes, such as the
    /// Druid's form abilities); "" is an empty slot. Pure rules, so EditMode can test them.
    /// </summary>
    public static class ActionBarRules
    {
        public const int BaseSlots = 10, MaxExtraSlots = 4;
        /// <summary>What the <paramref name="n"/>th extra slot costs (1-based): 1, 2, 3, 4.</summary>
        public static int SlotCost(int n) { return Math.Max(1, n); }
        /// <summary>The points spent on <paramref name="extra"/> extra slots.</summary>
        public static int SpentOn(int extra) { extra = Math.Max(0, Math.Min(MaxExtraSlots, extra)); return extra * (extra + 1) / 2; }
        /// <summary>Experience one AA point takes: a level's worth at the cap.</summary>
        public static int AaPointXp { get { return EncounterProgress.XpToNext(EncounterProgress.LevelCap - 1); } }
        /// <summary>AA points earned by the experience past the cap.</summary>
        public static int AaEarned(int experience)
        {
            int past = experience - EncounterProgress.XpForLevel(EncounterProgress.LevelCap);
            return past <= 0 ? 0 : past / AaPointXp;
        }
        /// <summary>Experience toward the next AA point (0 before the cap).</summary>
        public static int AaProgress(int experience)
        {
            int past = experience - EncounterProgress.XpForLevel(EncounterProgress.LevelCap);
            return past <= 0 ? 0 : past % AaPointXp;
        }
        public static int AaAvailable(int experience, int extra) { return Math.Max(0, AaEarned(experience) - SpentOn(extra)); }
        public static int Slots(int extra) { return BaseSlots + Math.Max(0, Math.Min(MaxExtraSlots, extra)); }
        /// <summary>A lock that the bar treats as "not yet yours" (a level or a talent still to come), as against a moment's state
        /// (a form, a charge, a pet away) which the kit reports through the same label.</summary>
        public static bool NotYetYours(string lockLabel) { return lockLabel != null && (lockLabel.StartsWith("Level ", StringComparison.Ordinal) || lockLabel == "Talent"); }

        /// <summary>
        /// The bar, kept in step with the book: <paramref name="book"/> is every ability the kit has (in its own order), each with whether
        /// it is yours yet. A slot whose key the book no longer has is emptied; empty slots are filled with abilities that are yours and not
        /// on the bar (in book order); a slot holding something not yet yours gives way to one that is. A new bar (none saved) is the book's
        /// first abilities that are yours, then those to come, so it shows what is coming. The bar is trimmed or padded to
        /// <paramref name="slots"/>.
        /// </summary>
        public static List<string> Reconcile(IList<string> bar, IList<(string key, bool yours)> book, int slots)
        {
            var known = new HashSet<string>(book.Select(b => b.key));
            var yours = new HashSet<string>(book.Where(b => b.yours).Select(b => b.key));
            bool fresh = bar == null || bar.Count == 0 || bar.All(string.IsNullOrEmpty);
            var result = new List<string>();
            if (!fresh) foreach (var k in bar) result.Add(!string.IsNullOrEmpty(k) && known.Contains(k) && !result.Contains(k) ? k : "");
            while (result.Count < slots) result.Add("");
            if (result.Count > slots) result.RemoveRange(slots, result.Count - slots);
            var waiting = new Queue<string>(book.Where(b => b.yours && !result.Contains(b.key)).Select(b => b.key));
            for (int i = 0; i < result.Count && waiting.Count > 0; i++) if (result[i] == "") result[i] = waiting.Dequeue();
            for (int i = 0; i < result.Count && waiting.Count > 0; i++) if (!yours.Contains(result[i])) result[i] = waiting.Dequeue();
            if (fresh)
            {
                var toCome = new Queue<string>(book.Where(b => !b.yours && !result.Contains(b.key)).Select(b => b.key));
                for (int i = 0; i < result.Count && toCome.Count > 0; i++) if (result[i] == "") result[i] = toCome.Dequeue();
            }
            return result;
        }
        /// <summary>Puts <paramref name="key"/> in slot <paramref name="slot"/>; if it was already on the bar, the two slots swap.</summary>
        public static void Place(IList<string> bar, int slot, string key)
        {
            if (bar == null || slot < 0 || slot >= bar.Count) return;
            int was = bar.IndexOf(key);
            if (was >= 0 && was != slot) bar[was] = bar[slot];
            bar[slot] = key ?? "";
        }
    }
}
