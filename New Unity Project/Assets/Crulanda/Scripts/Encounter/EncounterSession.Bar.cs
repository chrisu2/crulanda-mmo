using System.Collections.Generic;
using System.Linq;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The chosen bar on the session (ActionBarRules; Docs/ACTION_BAR.md): ten slots, keys 1-0, and up to four more bought with alternate
    /// advancement points at the cap (-, =, [, ]). The kit keeps its whole list of abilities, the spellbook; the bar maps its slots onto it,
    /// so the kits, their tests, the captures and the balance harness still use the kit's own slots. The Druid's forms sit on a bar of their
    /// own (Shift+1-4). The spellbook (P) is where the player chooses.
    /// </summary>
    public partial class EncounterSession
    {
        public bool SpellbookOpen;
        /// <summary>The book key picked in the spellbook, waiting for a bar slot to be clicked (null: none).</summary>
        public string BookPick;
        int[] barMap = new int[0]; string barFor; int barLevel = -1, barRanks = -1, barExtra = -1; List<TalentRank> barList;
        public int BarSlots { get { return ActionBarRules.Slots(Progress != null ? Progress.aaSlots : 0); } }
        /// <summary>Every slot of the kit that the chosen bar may hold (all but the stances), with its key and whether it is yours yet.</summary>
        public List<(int slot, string key, bool yours)> Book()
        {
            var list = new List<(int, string, bool)>(); if (Kit == null) return list;
            var stance = new HashSet<int>(Kit.StanceSlots);
            for (int i = 0; i < Kit.ActionCount; i++)
            {
                if (stance.Contains(i) || Kit.ActionAt(i) == null) continue;
                var key = Kit.BookKey(i); if (string.IsNullOrEmpty(key)) continue;
                list.Add((i, key, !ActionBarRules.NotYetYours(Kit.ActionLockLabel(i))));
            }
            return list;
        }
        /// <summary>The kit's slot behind bar slot <paramref name="b"/> (-1: empty).</summary>
        public int BarKitSlot(int b) { EnsureBar(); return b >= 0 && b < barMap.Length ? barMap[b] : -1; }
        /// <summary>Re-reads the bar when the class, level, ranks or bought slots change, or when the player placed something.</summary>
        void EnsureBar()
        {
            if (Kit == null || Progress == null) { barMap = new int[0]; return; }
            var ranks = Progress.talents; int count = 0; if (ranks != null) foreach (var t in ranks) if (t != null) count += t.rank;
            string cls = ClassDef != null ? ClassDef.id : "";
            if (barFor == cls && barLevel == Progress.Level && barRanks == count && ReferenceEquals(barList, ranks) && barExtra == Progress.aaSlots && barMap.Length == BarSlots) return;
            barFor = cls; barLevel = Progress.Level; barRanks = count; barList = ranks; barExtra = Progress.aaSlots;
            var book = Book();
            Progress.bar = ActionBarRules.Reconcile(Progress.bar, book.Select(e => (e.key, e.yours)).ToList(), BarSlots);
            barMap = Progress.bar.Select(k => { foreach (var e in book) if (e.key == k) return e.slot; return -1; }).ToArray();
        }
        /// <summary>Puts a spellbook entry on bar slot <paramref name="b"/> (swapping if it was already on the bar).</summary>
        public void PlaceOnBar(int b, string key)
        {
            EnsureBar(); if (b < 0 || b >= Progress.bar.Count || string.IsNullOrEmpty(key)) return;
            ActionBarRules.Place(Progress.bar, b, key); barFor = null; EnsureBar();
            var a = BarKitSlot(b) >= 0 ? Kit.ActionAt(BarKitSlot(b)) : null;
            Message((a != null ? a.name : key) + " is on slot " + EncounterInput.SlotLabel(b) + ".");
        }
        public int AaAvailable { get { return Progress == null ? 0 : ActionBarRules.AaAvailable(Progress.experience, Progress.aaSlots); } }
        public int NextSlotCost { get { return ActionBarRules.SlotCost((Progress != null ? Progress.aaSlots : 0) + 1); } }
        /// <summary>Buys the next bar slot with alternate advancement points (at the level cap, up to four).</summary>
        public bool BuyBarSlot()
        {
            if (Progress == null) return false;
            if (Progress.Level < EncounterProgress.LevelCap) { Message("Alternate advancement comes at level " + EncounterProgress.LevelCap + "."); return false; }
            if (Progress.aaSlots >= ActionBarRules.MaxExtraSlots) { Message("Your bar has all " + ActionBarRules.Slots(ActionBarRules.MaxExtraSlots) + " slots."); return false; }
            if (AaAvailable < NextSlotCost) { Message("The next slot costs " + NextSlotCost + " AA point" + (NextSlotCost == 1 ? "" : "s") + "; you have " + AaAvailable + "."); return false; }
            Progress.aaSlots++; barFor = null; EnsureBar();
            Message("A new bar slot: " + EncounterInput.SlotLabel(BarSlots - 1) + ". Choose what goes on it in the spellbook (P).");
            return true;
        }
    }
}
