using System;
using System.Collections.Generic;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Treasury Vault's contents (the bank; CAPITAL_HUB.md): its slots (the list index is the slot, an empty stack an empty slot)
    /// and how many rows of 12 have been bought past the first 48 slots. Kept in the profile save (CharacterProfile, format 2).
    /// </summary>
    [Serializable] public sealed class VaultData
    {
        public List<ItemStack> slots = new List<ItemStack>();
        public int rowsBought;
        /// <summary>The shared shelf (the profile's, every character's) rather than a character's own vault: its size and prices
        /// differ (Chris, 2026-10-16). Set when it is read; not saved.</summary>
        [NonSerialized] public bool shared;
    }

    /// <summary>
    /// The Treasury Vault (GAME-ONLY; Chris, 2026-10-15, the question prompt: "Shared by all (Recommended)"): one vault for every
    /// character, kept in the profile save beside the characters' saves, so what one character puts in another takes out. 48 slots
    /// to start; four more rows of 12 bought with coin, each dearer than the last. Putting a stack in tops up stacks of the same item
    /// first, then fills empty slots; quest items are refused. Pure logic on the bags and the vault: the session writes the profile
    /// and the character after each change (EncounterSession.Capital.cs).
    /// </summary>
    public static class TreasuryVault
    {
        public const int RowSize = 12, StartRows = 4, MaxBoughtRows = 4;
        public const int StartSlots = RowSize * StartRows;
        /// <summary>What each row past the first four costs, in order (the fifth row 100 crowns, the eighth 1,500).</summary>
        public static readonly int[] RowPrices = { 100, 250, 600, 1500 };
        public const string QuestLine = "The clerk won't take that: it belongs to a task, not a vault.";
        public const string FullLine = "The vault is full. Buy another row of slots from the clerk.";

        /// <summary>How many slots the vault has: 48 and 12 for each row bought.</summary>
        public static int Size(VaultData v)
        {
            int bought = Math.Max(0, Math.Min(MaxBoughtRows, v != null ? v.rowsBought : 0));
            return v != null && v.shared ? SharedStart + bought * SharedStep : (StartRows + bought) * RowSize;
        }
        /// <summary>The shared shelf (Chris, 2026-10-16: "need a limited shared space. maybe 3 slots and maybe they can buy more in the
        /// future. the remainder is character only"): 3 slots for all your characters, 3 more at a time, dear; each character's own
        /// vault is the 48 and its rows (EncounterProgress.vault).</summary>
        public const int SharedStart = 3, SharedStep = 3;
        public static readonly int[] SharedPrices = { 500, 1500, 4000, 10000 };
        /// <summary>Pads the vault to its size and clears empty stacks. Never shortened: slots past its size keep what they hold.</summary>
        public static VaultData Ensure(VaultData v)
        {
            if (v == null) v = new VaultData();
            if (v.slots == null) v.slots = new List<ItemStack>();
            v.rowsBought = Math.Max(0, Math.Min(MaxBoughtRows, v.rowsBought));
            for (int i = 0; i < v.slots.Count; i++) if (v.slots[i] == null) v.slots[i] = new ItemStack();
            while (v.slots.Count < Size(v)) v.slots.Add(new ItemStack());
            foreach (var s in v.slots) if (s.Empty) { s.item = ""; s.count = 0; }
            return v;
        }
        /// <summary>What the next row costs, or -1 when every row is bought.</summary>
        public static int NextRowPrice(VaultData v) { int n = v != null ? v.rowsBought : 0; return n >= MaxBoughtRows ? -1 : (v != null && v.shared ? SharedPrices : RowPrices)[Math.Max(0, n)]; }
        /// <summary>Buys the next row of 12 with the character's coin. Refused (why) when all are bought or the coin is short.</summary>
        public static bool BuyRow(VaultData v, EncounterProgress p, out string why)
        {
            why = null; Ensure(v);
            int price = NextRowPrice(v);
            if (price < 0) { why = "Every row of the vault is yours already."; return false; }
            if (p.gold < price) { why = "Another row costs " + Coin(price) + "."; return false; }
            p.gold -= price; v.rowsBought++; Ensure(v); return true;
        }
        public static int Used(VaultData v) { int n = 0; if (v?.slots != null) foreach (var s in v.slots) if (!s.Empty) n++; return n; }
        public static int Count(VaultData v, string item) { int n = 0; if (v?.slots != null) foreach (var s in v.slots) if (s.item == item) n += s.count; return n; }
        /// <summary>A quest's item: its kind is "quest" or its id is a quest's ("quest."). The quest bag's own items never reach the bags.</summary>
        public static bool IsQuestItem(ItemDef d) { return d != null && (d.kind == "quest" || (d.id != null && d.id.StartsWith("quest.", StringComparison.Ordinal))); }
        /// <summary>Whether the vault refuses this item: quest items, and an id this build does not know.</summary>
        public static bool Refuses(ItemDef d, out string why)
        {
            why = null;
            if (d == null) { why = "The clerk can't make out what that is."; return true; }
            if (IsQuestItem(d)) { why = QuestLine; return true; }
            return false;
        }

        /// <summary>
        /// Puts a bag slot's stack in the vault: onto the chosen vault slot when one is given (merging onto the same item, or swapping
        /// with what is there when the bag slot takes it back), else onto stacks of the same item, then into empty slots. What does
        /// not fit stays in the bags. Returns how many went in (0 and why when refused).
        /// </summary>
        public static int Deposit(VaultData v, EncounterProgress p, ItemDatabase db, int bagIndex, int toSlot, out string why)
        {
            why = null; Ensure(v); if (db == null || p?.bag == null) return 0;
            if (bagIndex < 0 || bagIndex >= p.bag.Count || p.bag[bagIndex].Empty) return 0;
            var from = p.bag[bagIndex]; var d = db.Get(from.item);
            if (Refuses(d, out why)) return 0;
            int moved = 0;
            if (toSlot >= 0 && toSlot < v.slots.Count)
            {
                var to = v.slots[toSlot];
                if (to.Empty) { v.slots[toSlot] = new ItemStack { item = from.item, count = from.count }; moved = from.count; from.count = 0; }
                else if (to.item == from.item) { int take = Math.Min(from.count, Math.Max(0, d.stack - to.count)); to.count += take; from.count -= take; moved = take; }
                else
                {
                    var back = db.Get(to.item);
                    if (back == null || !Inventory.Accepts(p, db, bagIndex, back)) { why = Inventory.RefusedLine(p, db, bagIndex); return 0; }
                    v.slots[toSlot] = new ItemStack { item = from.item, count = from.count }; moved = from.count;
                    p.bag[bagIndex] = new ItemStack { item = to.item, count = to.count };
                    return moved;
                }
            }
            else
            {
                for (int i = 0; i < v.slots.Count && from.count > 0; i++)
                {
                    var s = v.slots[i]; if (s.item != from.item || s.count >= d.stack) continue;
                    int take = Math.Min(from.count, d.stack - s.count); s.count += take; from.count -= take; moved += take;
                }
                for (int i = 0; i < v.slots.Count && from.count > 0; i++)
                {
                    if (!v.slots[i].Empty) continue;
                    int take = Math.Min(from.count, d.stack); v.slots[i] = new ItemStack { item = from.item, count = take }; from.count -= take; moved += take;
                }
            }
            if (from.count <= 0) p.bag[bagIndex] = new ItemStack();
            if (moved == 0) why = FullLine;
            return moved;
        }

        /// <summary>
        /// Takes a vault slot's stack into the bags: into the chosen bag slot when one is given and it takes the item (empty, or the
        /// same item with room), else onto stacks and into free slots as anything new comes into the bags (Inventory.Add). What does
        /// not fit stays in the vault. Returns how many came out (0 and why when the bags are full).
        /// </summary>
        public static int Withdraw(VaultData v, EncounterProgress p, ItemDatabase db, int slot, int toBag, out string why)
        {
            why = null; Ensure(v); if (db == null || p == null) return 0;
            if (slot < 0 || slot >= v.slots.Count || v.slots[slot].Empty) return 0;
            Inventory.Ensure(p);
            var from = v.slots[slot]; var d = db.Get(from.item); int before = from.count;
            if (d != null && toBag >= 0 && toBag < p.bag.Count && Inventory.Accepts(p, db, toBag, d))
            {
                var to = p.bag[toBag];
                if (to.Empty) { int take = Math.Min(from.count, d.stack); p.bag[toBag] = new ItemStack { item = from.item, count = take }; from.count -= take; }
                else if (to.item == from.item) { int take = Math.Min(from.count, Math.Max(0, d.stack - to.count)); to.count += take; from.count -= take; }
            }
            if (from.count > 0)
            {
                if (d != null) from.count = Inventory.Add(p, db, from.item, from.count);
                else
                {   // an id this build no longer knows: it comes out whole into a free ordinary slot, as it went in
                    int free = p.bag.FindIndex(0, Math.Min(Inventory.BagSize, p.bag.Count), s => s.Empty);
                    if (free >= 0) { p.bag[free] = new ItemStack { item = from.item, count = from.count }; from.count = 0; }
                }
            }
            int moved = before - from.count;
            if (from.count <= 0) v.slots[slot] = new ItemStack();
            if (moved == 0) why = "Your bags are full.";
            return moved;
        }
        /// <summary>Moves vault slot a onto b: merges the same item, otherwise swaps.</summary>
        public static void Move(VaultData v, ItemDatabase db, int a, int b)
        {
            Ensure(v);
            if (a == b || a < 0 || b < 0 || a >= v.slots.Count || b >= v.slots.Count || v.slots[a].Empty) return;
            var from = v.slots[a]; var to = v.slots[b]; var d = db?.Get(from.item);
            if (!to.Empty && to.item == from.item && d != null && d.stack > 1)
            {
                int take = Math.Min(from.count, d.stack - to.count); to.count += take; from.count -= take;
                if (from.count <= 0) v.slots[a] = new ItemStack();
                return;
            }
            v.slots[a] = to; v.slots[b] = from;
        }
        /// <summary>Coin as the game writes it everywhere (the bags, the merchants, the loot): "1500 crowns".</summary>
        public static string Coin(int n) { return n + " crowns"; }
    }
}
