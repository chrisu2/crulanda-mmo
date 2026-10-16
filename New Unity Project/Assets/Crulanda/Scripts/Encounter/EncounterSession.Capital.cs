using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The capital's two counters (CAPITAL_HUB.md, Chris's answers of 2026-10-15): the Treasury Vault (a resident whose role is
    /// "banker", or /bank to try it anywhere) and the Exchange (a resident whose role is "auctioneer", or /exchange). Each opens its
    /// window with the bags beside it and shuts when you walk 6 m off, press Esc or loot. Nothing changes in a fight. The vault is
    /// written to the profile save before the character is saved (so a crash between leaves a thing in both, never in neither); the
    /// Exchange's lots live in the world save, written with the character (SimPopulation.Persist).
    /// </summary>
    public partial class EncounterSession
    {
        /// <summary>The resident roles that open the counters (WorldLife's residents; the castle's clerks are placed by the zone).</summary>
        public const string BankerRole = "banker", AuctioneerRole = "auctioneer";
        /// <summary>The Treasury Vault's window is open, with the vault read from the profile.</summary>
        public bool BankOpen { get; private set; }
        /// <summary>The Exchange's window is open.</summary>
        public bool ExchangeOpen { get; private set; }
        /// <summary>Who keeps the open counter (the clerk's name, or the counter's own when opened by a command).</summary>
        public string CapitalClerk { get; private set; }
        public VaultData Vault { get; private set; }
        /// <summary>The bag slot chosen to list on the Exchange (-1 none): right-click or drag it from the bags.</summary>
        public int ExchangeSell = -1;
        /// <summary>The world save the Exchange lives in (the sims'), or null with no zone loaded.</summary>
        public WorldSave ExchangeWorld { get { return SimPopulation.Active != null ? SimPopulation.Active.World : null; } }
        Vector3 capitalAt; bool bagsForCapital;

        /// <summary>Talking to a resident: a banker opens the vault, an auctioneer the Exchange. False for anyone else.</summary>
        bool CapitalTalk(Villager v)
        {
            if (v == null) return false;
            if (v.Role == BankerRole) { if (OpenBank(v.Name, v.transform.position)) { v.FacePlayer(); v.Hold(30); } return true; }
            if (v.Role == AuctioneerRole) { if (OpenExchange(v.Name, v.transform.position)) { v.FacePlayer(); v.Hold(30); } return true; }
            return false;
        }
        bool CapitalShut(string what)
        {
            if (Items == null) { Message(what + " is shut: no item content."); return true; }
            if (InCombat) { Message(FightingLine); return true; }
            if (saves == null || saveBlocked) { Message("Saving is off, so " + what.ToLowerInvariant() + " stays shut."); return true; }
            return false;
        }
        void OpenCounter(Vector3 at)
        {
            CloseCapital(); CloseVendor(); Conversation = null; TradesOpen = false; CharacterOpen = false; CloseLoot();
            capitalAt = at; bagsForCapital = !InventoryOpen; InventoryOpen = true;
        }
        /// <summary>Opens the Treasury Vault: refused in a fight, with saving off, or when the profile is there but cannot be read.</summary>
        public bool OpenBank(string clerk, Vector3 at)
        {
            if (CapitalShut("The vault")) return false;
            var v = CharacterProfile.LoadVault(SaveRoot, out var why);
            if (v == null) { Message("The vault can't be opened (" + why + "). Nothing in it is changed."); return false; }
            OpenCounter(at); Vault = v; BankOpen = true; CapitalClerk = clerk;
            return true;
        }
        /// <summary>Opens the Exchange: refused in a fight, with saving off, or with no world loaded.</summary>
        public bool OpenExchange(string clerk, Vector3 at)
        {
            if (CapitalShut("The Exchange")) return false;
            var w = ExchangeWorld; if (w == null) { Message("The Exchange is closed here."); return false; }
            OpenCounter(at); ExchangeOpen = true; CapitalClerk = clerk; ExchangeSell = -1;
            int waiting = Exchange.Waiting(Exchange.Ensure(w), Progress.classId).Count;
            if (waiting > 0) Message(clerk + ": \"Something waits for you here.\" (Collect)");
            return true;
        }
        /// <summary>Shuts the vault or the Exchange, and the bags with it if the counter opened them.</summary>
        public void CloseCapital()
        {
            if ((BankOpen || ExchangeOpen) && bagsForCapital) InventoryOpen = false;
            BankOpen = ExchangeOpen = false; bagsForCapital = false; Vault = null; ExchangeSell = -1; CapitalClerk = null;
        }
        /// <summary>Walked away from the counter: it shuts.</summary>
        void TickCapital()
        {
            if ((BankOpen || ExchangeOpen) && Player != null && Vector3.Distance(Player.transform.position, capitalAt) > 6) CloseCapital();
        }

        // ---------- the vault ----------
        List<ItemStack> CopyBag() { var c = new List<ItemStack>(); foreach (var s in Progress.bag) c.Add(new ItemStack { item = s.item, count = s.count }); return c; }
        /// <summary>Writes the vault; on failure the bags are put back as they were and the vault re-read.</summary>
        bool StoreVault(List<ItemStack> bagBefore, int goldBefore)
        {
            if (CharacterProfile.SaveVault(SaveRoot, Vault, out var why)) return true;
            Message("The vault could not be saved (" + why + "). Nothing moved.");
            Progress.bag = bagBefore; Progress.gold = goldBefore;
            Vault = CharacterProfile.LoadVault(SaveRoot, out _); if (Vault == null) CloseCapital();
            return false;
        }
        /// <summary>A bag stack into the vault (onto a chosen vault slot, or wherever it fits).</summary>
        public void DepositBag(int bagIndex, int toSlot = -1)
        {
            if (!BankOpen || Vault == null) return;
            if (InCombat) { Message(FightingLine); return; }
            var before = CopyBag(); int gold = Progress.gold;
            int n = TreasuryVault.Deposit(Vault, Progress, Items, bagIndex, toSlot, out var why);
            if (n <= 0) { if (why != null) Message(why); return; }
            if (StoreVault(before, gold)) Save(false);
        }
        /// <summary>A vault stack into the bags (into a chosen bag slot, or wherever it fits).</summary>
        public void WithdrawVault(int slot, int toBag = -1)
        {
            if (!BankOpen || Vault == null) return;
            if (InCombat) { Message(FightingLine); return; }
            var before = CopyBag(); int gold = Progress.gold;
            int n = TreasuryVault.Withdraw(Vault, Progress, Items, slot, toBag, out var why);
            if (n <= 0) { if (why != null) Message(why); return; }
            if (StoreVault(before, gold)) Save(false);
        }
        public void MoveVault(int a, int b)
        {
            if (!BankOpen || Vault == null) return;
            TreasuryVault.Move(Vault, Items, a, b); StoreVault(CopyBag(), Progress.gold);
        }
        public void BuyVaultRow()
        {
            if (!BankOpen || Vault == null) return;
            var before = CopyBag(); int gold = Progress.gold;
            if (!TreasuryVault.BuyRow(Vault, Progress, out var why)) { Message(why); return; }
            if (StoreVault(before, gold)) { Save(false); Message("Another row of 12: the vault holds " + TreasuryVault.Size(Vault) + "."); }
        }

        // ---------- the Exchange ----------
        /// <summary>Chooses a bag stack to list (the window's My lots tab shows it with its prices).</summary>
        public void ExchangeChoose(int bagIndex)
        {
            if (!ExchangeOpen || bagIndex < 0 || bagIndex >= Progress.bag.Count || Progress.bag[bagIndex].Empty) return;
            var d = Items?.Get(Progress.bag[bagIndex].item);
            if (TreasuryVault.IsQuestItem(d)) { Message("That belongs to a task. It can't be sold."); return; }
            ExchangeSell = bagIndex; EncounterHud.ExchangeTab = 1;
        }
        bool ExchangeReady() { if (!ExchangeOpen || ExchangeWorld == null) return false; if (InCombat) { Message(FightingLine); return false; } return true; }
        public bool ExchangeList(int buyout, int startBid, int hours)
        {
            if (!ExchangeReady()) return false;
            var l = Exchange.List(ExchangeWorld, Progress, Items, ExchangeSell, buyout, startBid, hours, out var why);
            if (l == null) { Message(why); return false; }
            Message("Listed " + Exchange.Lot(Items, l) + " for " + TreasuryVault.Coin(l.buyout) + (l.startBid > 0 ? " (bids from " + TreasuryVault.Coin(l.startBid) + ")" : "") + ", " + hours + " hours. Deposit " + TreasuryVault.Coin(l.deposit) + ".");
            ExchangeSell = -1; Save(false); return true;
        }
        public void ExchangeBuy(int id)
        {
            if (!ExchangeReady()) return;
            var l = Exchange.Ensure(ExchangeWorld).listings.Find(o => o.id == id); string lot = l != null ? Exchange.Lot(Items, l) : null; int price = l != null ? l.buyout : 0;
            if (!Exchange.BuyOut(ExchangeWorld, Progress, Items, id, out var why)) { Message(why); return; }
            Message("Bought " + lot + " for " + TreasuryVault.Coin(price) + "."); Save(false);
        }
        public void ExchangeBid(int id, int amount)
        {
            if (!ExchangeReady()) return;
            var l = Exchange.Ensure(ExchangeWorld).listings.Find(o => o.id == id);
            bool outright = l != null && amount >= l.buyout; string lot = l != null ? Exchange.Lot(Items, l) : null;
            if (!Exchange.Bid(ExchangeWorld, Progress, Items, id, amount, out var why)) { Message(why); return; }
            Message(outright ? "Bought " + lot + " for " + TreasuryVault.Coin(amount) + "." : "You bid " + TreasuryVault.Coin(amount) + " on " + lot + ". Outbid, the coin comes back at the clerk."); Save(false);
        }
        public void ExchangeCancel(int id)
        {
            if (!ExchangeReady()) return;
            if (!Exchange.Cancel(ExchangeWorld, Progress, Items, id, out var why)) { Message(why); return; }
            Message("Taken down. It waits at the clerk (Collect); the deposit is kept."); Save(false);
        }
        public void ExchangeCollect()
        {
            if (!ExchangeReady()) return;
            var line = Exchange.Collect(ExchangeWorld, Progress, Items);
            Message(line ?? "Nothing waits for you."); if (line != null) Save(false);
        }

        // ---------- tester's commands (ZoneChat) ----------
        /// <summary>/bank: the vault's window where you stand (to try it before the castle's clerk is placed).</summary>
        void TesterBank() { if (Player != null && OpenBank("The Treasury Vault", Player.transform.position)) Message("Testing: the Treasury Vault, shared by all your characters. Walk off or Esc to close."); }
        /// <summary>/exchange: the Exchange's window where you stand.</summary>
        void TesterExchange() { if (Player != null && OpenExchange("The Exchange", Player.transform.position)) Message("Testing: the Exchange. Right-click a bag item to list it; walk off or Esc to close."); }
    }
}
