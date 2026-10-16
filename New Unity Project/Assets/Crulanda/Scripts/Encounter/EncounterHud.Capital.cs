using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The capital's two windows (EncounterSession.Capital.cs), each beside the bags:
    /// - The Treasury Vault: each character's own slots twelve to a row, and a shared shelf of 3 for all your characters. Right-click or drag a bag item onto it to store
    ///   it (onto a slot to put it there), right-click or drag a vault item onto the bags to take it out, drag within the vault to
    ///   move or stack; "Buy a row" adds 12 slots for the price shown.
    /// - The Exchange: three tabs. Browse (the sims' lots, searched by name, slot, level range and quality; Buy outright or Bid the
    ///   least next bid), My lots (the item chosen from the bags, by right-click or drag, with its buyout, starting bid and 12/24/48
    ///   hours and the deposit; your lots up, which can be taken down until someone bids; what happened lately) and Collect (coin
    ///   and items waiting at the clerk).
    /// </summary>
    public sealed partial class EncounterHud
    {
        static bool bankVisible, exchangeVisible;
        const float VaultSlot = 40, VaultGap = 5;
        static readonly Rect BankRect = new Rect(190, 110, 48 + TreasuryVault.RowSize * (VaultSlot + VaultGap), 560), ExchangeRect = new Rect(190, 110, 840, 600);
        static bool CapitalUiBlocks(Vector2 p) { return (bankVisible && BankRect.Contains(p)) || (exchangeVisible && ExchangeRect.Contains(p)); }
        /// <summary>The Exchange's tab: 0 Browse, 1 My lots, 2 Collect (choosing an item to sell turns to My lots).</summary>
        public static int ExchangeTab;
        int dragVault = -1;
        Vector2 vaultScroll, browseScroll, mineScroll, collectScroll;
        static string browseText = "", browseMin = "", browseMax = "", sellBuyout = "", sellStart = "";
        static int browseSlot = -1, browseQuality = -1, sellHours = 24, sellFor = -1; static string sellItem;

        /// <summary>A text box in a capital window: while it has the keys the character does not move (EncounterInput.Typing), and
        /// Return or Esc lets go of it.</summary>
        string CapitalField(string name, Rect r, string value, int max)
        {
            GUI.SetNextControlName(name);
            var s = GUI.TextField(r, value ?? "", max);
            if (GUI.GetNameOfFocusedControl() == name)
            {
                EncounterInput.Typing = true;
                var e = Event.current;
                if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)) { GUI.FocusControl(null); e.Use(); }
            }
            return s;
        }
        static int Number(string s, int fallback) { return int.TryParse((s ?? "").Trim(), out int n) ? n : fallback; }
        void CapitalFrame(Rect w, string title)
        {
            Fill(new Rect(w.x - 3, w.y - 3, w.width + 6, w.height + 6), new Color(.3f, .22f, .12f, .98f)); Fill(w, new Color(.08f, .075f, .07f, .97f));
            Shadow(new Rect(w.x + 14, w.y + 8, w.width - 28, 30), title, heading, gold);
        }
        Color QualityInk(ItemDef d) { return d == null ? Color.white : ItemDatabase.QualityColors[Mathf.Clamp(d.quality, 0, ItemDatabase.MaxQuality)]; }

        // ---------- the Treasury Vault ----------
        void DrawBank()
        {
            ItemStyles(); var v = session.Vault; if (v == null) return;
            var w = BankRect; var p = session.Progress; var e = Event.current; var mouse = e.mousePosition;
            CapitalFrame(w, "THE TREASURY VAULT");
            // Two tabs: this character's own vault, and the shelf every character shares (Chris, 2026-10-16: "need a limited shared space.
            // maybe 3 slots and maybe they can buy more in the future. the remainder is character only").
            if (GUI.Toggle(new Rect(w.x + 14, w.y + 38, 150, 24), !session.BankShared, "Your vault", micro) && session.BankShared) session.ShowSharedVault(false);
            if (GUI.Toggle(new Rect(w.x + 170, w.y + 38, 170, 24), session.BankShared, "Shared shelf", micro) && !session.BankShared) session.ShowSharedVault(true);
            v = session.Vault; if (v == null) return;
            Shadow(new Rect(w.x + 350, w.y + 40, w.width - 364, 22), (session.BankShared ? "every character's" : "this character's") + "  ·  " + TreasuryVault.Used(v) + "/" + TreasuryVault.Size(v) + " slots used", tiny, new Color(.82f, .74f, .56f));
            var view = new Rect(w.x + 10, w.y + 68, w.width - 20, w.height - 170);
            int rows = Mathf.CeilToInt(v.slots.Count / (float)TreasuryVault.RowSize);
            vaultScroll = GUI.BeginScrollView(view, vaultScroll, new Rect(0, 0, view.width - 20, rows * (VaultSlot + VaultGap) + 4));
            var local = Event.current.mousePosition;
            for (int i = 0; i < v.slots.Count; i++)
            {
                var r = new Rect(4 + (i % TreasuryVault.RowSize) * (VaultSlot + VaultGap), 4 + (i / TreasuryVault.RowSize) * (VaultSlot + VaultGap), VaultSlot, VaultSlot);
                ItemSquare(r, dragVault == i ? null : v.slots[i]);
                if (!r.Contains(local)) continue;
                if (!v.slots[i].Empty && dragBag < 0 && dragVault < 0) { ItemTooltip(session.Items?.Get(v.slots[i].item), true); tooltipAt = new Vector2(view.x + local.x - vaultScroll.x, view.y + local.y - vaultScroll.y); }
                if (e.type == EventType.MouseDown && e.button == 0 && !v.slots[i].Empty) { dragVault = i; e.Use(); }
                else if (e.type == EventType.MouseDown && e.button == 1 && !v.slots[i].Empty) { session.WithdrawVault(i); e.Use(); }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    if (dragBag >= 0) session.DepositBag(dragBag, i);
                    else if (dragVault >= 0) session.MoveVault(dragVault, i);
                    dragBag = dragEquip = dragVault = -1; e.Use();
                }
            }
            GUI.EndScrollView();
            // A bag item dropped anywhere else on the window goes in wherever it fits.
            if (e.type == EventType.MouseUp && e.button == 0 && dragBag >= 0 && w.Contains(e.mousePosition)) { session.DepositBag(dragBag); dragBag = -1; e.Use(); }
            float y = w.yMax - 96;
            GUI.Label(new Rect(w.x + 14, y, w.width - 28, 40), "Right-click or drag from your bags to store · right-click or drag out to take back · stacks merge · quest items stay with you", tiny);
            int price = TreasuryVault.NextRowPrice(v);
            if (price < 0) Shadow(new Rect(w.x + 14, w.yMax - 44, 300, 30), session.BankShared ? "The shelf is as big as it grows." : "Every row of the vault is yours.", text, new Color(.7f, .66f, .58f));
            else
            {
                GUI.enabled = p.gold >= price;
                if (GUI.Button(new Rect(w.x + 14, w.yMax - 44, 270, 32), (session.BankShared ? "Buy 3 more shared slots (" : "Buy a row of 12 (") + TreasuryVault.Coin(price) + ")", micro)) session.BuyVaultRow();
                GUI.enabled = true;
            }
            Shadow(new Rect(w.x + 300, w.yMax - 40, 150, 24), p.gold + " crowns", text, new Color(1, .86f, .4f));
            if (GUI.Button(new Rect(w.xMax - 124, w.yMax - 44, 110, 32), "Goodbye", micro)) session.CloseCapital();
        }

        // ---------- the Exchange ----------
        static readonly string[] ExchangeTabs = { "Browse", "My lots", "Collect" };
        void DrawExchange()
        {
            ItemStyles(); var world = session.ExchangeWorld; if (world == null) return;
            var x = Exchange.Ensure(world); var w = ExchangeRect; var p = session.Progress; var e = Event.current;
            CapitalFrame(w, "THE EXCHANGE");
            Shadow(new Rect(w.x + 260, w.y + 14, 360, 24), session.CapitalClerk ?? "", tiny, new Color(.82f, .74f, .56f));
            Shadow(new Rect(w.xMax - 220, w.y + 12, 206, 24), p.gold + " crowns", text, new Color(1, .86f, .4f));
            int waiting = Exchange.Waiting(x, p.classId).Count;
            for (int t = 0; t < ExchangeTabs.Length; t++)
            {
                string label = ExchangeTabs[t] + (t == 1 ? " (" + Exchange.Mine(x, p.classId).Count + ")" : t == 2 && waiting > 0 ? " (" + waiting + ")" : "");
                GUI.color = ExchangeTab == t ? gold : Color.white;
                if (GUI.Button(new Rect(w.x + 14 + t * 130, w.y + 44, 124, 28), label, micro)) { ExchangeTab = t; GUI.FocusControl(null); }
                GUI.color = Color.white;
            }
            var body = new Rect(w.x + 14, w.y + 82, w.width - 28, w.height - 136);
            if (ExchangeTab == 0) DrawBrowse(x, body);
            else if (ExchangeTab == 1) DrawMine(x, body);
            else DrawCollect(x, body);
            // A bag item dropped on the window is the one to sell.
            if (e.type == EventType.MouseUp && e.button == 0 && dragBag >= 0 && w.Contains(e.mousePosition)) { session.ExchangeChoose(dragBag); dragBag = -1; e.Use(); }
            GUI.Label(new Rect(w.x + 14, w.yMax - 42, w.width - 160, 36), "Right-click or drag an item from your bags to sell it · the sims buy what is fairly priced, more often the more of them are online", tiny);
            if (GUI.Button(new Rect(w.xMax - 124, w.yMax - 44, 110, 32), "Goodbye", micro)) session.CloseCapital();
        }
        void DrawBrowse(ExchangeState x, Rect r)
        {
            var p = session.Progress; var db = session.Items;
            GUI.Label(new Rect(r.x, r.y + 2, 50, 24), "Name", tiny);
            browseText = CapitalField("cap.name", new Rect(r.x + 46, r.y, 170, 24), browseText, 40);
            GUI.Label(new Rect(r.x + 228, r.y + 2, 40, 24), "Slot", tiny);
            if (GUI.Button(new Rect(r.x + 266, r.y, 96, 24), browseSlot < 0 ? "Any" : ItemDatabase.SlotNames[browseSlot], micro)) browseSlot = browseSlot + 1 >= ItemDatabase.SlotIds.Length ? -1 : browseSlot + 1;
            GUI.Label(new Rect(r.x + 374, r.y + 2, 50, 24), "Level", tiny);
            browseMin = CapitalField("cap.min", new Rect(r.x + 420, r.y, 36, 24), browseMin, 2);
            GUI.Label(new Rect(r.x + 460, r.y + 2, 12, 24), "-", tiny);
            browseMax = CapitalField("cap.max", new Rect(r.x + 472, r.y, 36, 24), browseMax, 2);
            GUI.Label(new Rect(r.x + 520, r.y + 2, 60, 24), "Quality", tiny);
            if (GUI.Button(new Rect(r.x + 576, r.y, 96, 24), browseQuality < 0 ? "Any" : ItemDatabase.QualityNames[browseQuality], micro)) browseQuality = browseQuality + 1 > ItemDatabase.MaxQuality ? -1 : browseQuality + 1;
            if (GUI.Button(new Rect(r.x + 684, r.y, 100, 24), "Clear", micro)) { browseText = browseMin = browseMax = ""; browseSlot = browseQuality = -1; GUI.FocusControl(null); }
            var lots = Exchange.Browse(x, db, browseText, browseSlot, Number(browseMin, 0), Number(browseMax, 0), browseQuality);
            var view = new Rect(r.x, r.y + 34, r.width, r.height - 34); float rowH = 50;
            if (lots.Count == 0) { Shadow(new Rect(view.x, view.y + 10, view.width, 24), x.listings.Exists(l => !Exchange.IsYours(l)) ? "Nothing matches." : "Nothing for sale just now. The sims list what they gather and brew as they play.", text, new Color(.7f, .66f, .58f)); return; }
            browseScroll = GUI.BeginScrollView(view, browseScroll, new Rect(0, 0, view.width - 20, lots.Count * rowH));
            var mouse = Event.current.mousePosition;
            for (int i = 0; i < lots.Count; i++)
            {
                var l = lots[i]; var d = db.Get(l.item); float y = i * rowH;
                var sq = new Rect(2, y + 4, 42, 42);
                ItemSquare(sq, new ItemStack { item = l.item, count = l.count });
                Shadow(new Rect(54, y + 2, 300, 22), d.name, frameName, QualityInk(d));
                GUI.Label(new Rect(54, y + 24, 320, 22), l.sellerName + "  ·  " + Exchange.TimeLeft(x, l) + (d.level > 1 ? "  ·  level " + d.level : ""), tiny);
                string bid = l.startBid <= 0 ? "buyout only" : l.bid > 0 ? "bid " + TreasuryVault.Coin(l.bid) + (Exchange.YouBid(l) && l.bidder == Exchange.YouId(p.classId) ? " (yours)" : " (" + l.bidderName + ")") : "bids from " + TreasuryVault.Coin(l.startBid);
                GUI.Label(new Rect(380, y + 4, 200, 22), TreasuryVault.Coin(l.buyout), text);
                GUI.Label(new Rect(380, y + 26, 220, 22), bid, tiny);
                GUI.enabled = p.gold >= l.buyout;
                if (GUI.Button(new Rect(view.width - 200, y + 10, 84, 30), "Buy", micro)) session.ExchangeBuy(l.id);
                int min = Exchange.MinBid(l);
                GUI.enabled = l.startBid > 0 && min < l.buyout && p.gold >= min && l.bidder != Exchange.YouId(p.classId);
                if (GUI.Button(new Rect(view.width - 110, y + 10, 84, 30), l.startBid > 0 ? "Bid " + min : "Bid", micro)) session.ExchangeBid(l.id, min);
                GUI.enabled = true;
                if (new Rect(0, y, 360, rowH).Contains(mouse)) { ItemTooltip(d, true); tooltipAt = new Vector2(view.x + mouse.x - browseScroll.x, view.y + mouse.y - browseScroll.y); }
            }
            GUI.EndScrollView();
        }
        void DrawMine(ExchangeState x, Rect r)
        {
            var p = session.Progress; var db = session.Items; float y = r.y;
            // What is to be sold: the item chosen from the bags, its prices and time, the deposit.
            int at = session.ExchangeSell; var stack = at >= 0 && at < p.bag.Count ? p.bag[at] : null; var d = stack != null && !stack.Empty ? db.Get(stack.item) : null;
            Fill(new Rect(r.x, y, r.width, 104), new Color(1, 1, 1, .04f));
            if (d == null) { session.ExchangeSell = -1; sellFor = -1; Shadow(new Rect(r.x + 12, y + 36, r.width - 24, 30), "Right-click or drag an item from your bags to sell it here.", text, new Color(.7f, .66f, .58f)); }
            else
            {
                if (sellFor != at || sellItem != stack.item) { sellFor = at; sellItem = stack.item; sellBuyout = Exchange.Fair(d, stack.count).ToString(); sellStart = ""; }
                ItemSquare(new Rect(r.x + 10, y + 10, Slot, Slot), stack);
                Shadow(new Rect(r.x + 72, y + 8, 320, 24), d.name + (stack.count > 1 ? " x" + stack.count : ""), frameName, QualityInk(d));
                GUI.Label(new Rect(r.x + 72, y + 32, 320, 22), "A fair price: about " + TreasuryVault.Coin(Exchange.Fair(d, stack.count)), tiny);
                GUI.Label(new Rect(r.x + 400, y + 10, 70, 24), "Buyout", tiny);
                sellBuyout = CapitalField("cap.buyout", new Rect(r.x + 470, y + 8, 90, 24), sellBuyout, 7);
                GUI.Label(new Rect(r.x + 400, y + 40, 70, 24), "Start bid", tiny);
                sellStart = CapitalField("cap.start", new Rect(r.x + 470, y + 38, 90, 24), sellStart, 7);
                GUI.Label(new Rect(r.x + 566, y + 40, 120, 24), "(blank: buyout only)", tiny);
                for (int k = 0; k < Exchange.Durations.Length; k++)
                {
                    int h = Exchange.Durations[k]; GUI.color = sellHours == h ? gold : Color.white;
                    if (GUI.Button(new Rect(r.x + 72 + k * 64, y + 62, 58, 26), h + " h", micro)) sellHours = h;
                    GUI.color = Color.white;
                }
                int deposit = Exchange.Deposit(d, stack.count, sellHours);
                GUI.Label(new Rect(r.x + 270, y + 66, 200, 22), "Deposit " + TreasuryVault.Coin(deposit) + " (back when it sells)", tiny);
                GUI.enabled = p.gold >= deposit;
                if (GUI.Button(new Rect(r.xMax - 130, y + 60, 120, 32), "List it", micro)) { if (session.ExchangeList(Number(sellBuyout, 0), Number(sellStart, 0), sellHours)) sellFor = -1; GUI.FocusControl(null); }
                GUI.enabled = true;
            }
            y += 114;
            var mine = Exchange.Mine(x, p.classId); var history = Exchange.History(x, p.classId);
            Shadow(new Rect(r.x, y, 300, 22), "YOUR LOTS  " + mine.Count + "/" + Exchange.MaxYourListings, frameName, gold); y += 26;
            var view = new Rect(r.x, y, r.width, r.yMax - y - 86); float rowH = 40;
            mineScroll = GUI.BeginScrollView(view, mineScroll, new Rect(0, 0, view.width - 20, Mathf.Max(1, mine.Count) * rowH));
            if (mine.Count == 0) GUI.Label(new Rect(4, 6, view.width - 30, 24), "Nothing up for sale.", tiny);
            var mouse = Event.current.mousePosition;
            for (int i = 0; i < mine.Count; i++)
            {
                var l = mine[i]; var ld = db.Get(l.item); float ry = i * rowH;
                ItemSquare(new Rect(2, ry + 3, 34, 34), new ItemStack { item = l.item, count = l.count });
                Shadow(new Rect(46, ry + 2, 280, 22), ld != null ? ld.name : l.item, frameName, QualityInk(ld));
                GUI.Label(new Rect(46, ry + 20, 300, 20), Exchange.TimeLeft(x, l) + " left  ·  deposit " + TreasuryVault.Coin(l.deposit), tiny);
                GUI.Label(new Rect(340, ry + 2, 220, 22), "Buyout " + TreasuryVault.Coin(l.buyout), tiny);
                GUI.Label(new Rect(340, ry + 20, 260, 20), l.startBid <= 0 ? "buyout only" : l.bid > 0 ? "bid " + TreasuryVault.Coin(l.bid) + " (" + l.bidderName + ")" : "no bids yet (from " + TreasuryVault.Coin(l.startBid) + ")", tiny);
                GUI.enabled = l.bid <= 0;
                if (GUI.Button(new Rect(view.width - 130, ry + 6, 100, 28), "Take down", micro)) session.ExchangeCancel(l.id);
                GUI.enabled = true;
                if (ld != null && new Rect(0, ry, 330, rowH).Contains(mouse)) { ItemTooltip(ld, false); tooltipAt = new Vector2(view.x + mouse.x - mineScroll.x, view.y + mouse.y - mineScroll.y); }
            }
            GUI.EndScrollView();
            y = r.yMax - 80;
            Shadow(new Rect(r.x, y, 300, 22), "LATELY", frameName, gold); y += 22;
            for (int i = 0; i < 3; i++)
            {
                int k = history.Count - 1 - i; if (k < 0) break;
                GUI.Label(new Rect(r.x, y + i * 18, r.width, 20), history[k].line, tiny);
            }
            if (history.Count == 0) GUI.Label(new Rect(r.x, y, r.width, 20), "Nothing yet.", tiny);
        }
        void DrawCollect(ExchangeState x, Rect r)
        {
            var p = session.Progress; var db = session.Items; var waiting = Exchange.Waiting(x, p.classId);
            if (waiting.Count == 0) { Shadow(new Rect(r.x, r.y + 10, r.width, 24), "Nothing waits for you here. Coin from a sale, bids given back, lots won and lots that did not sell come here.", text, new Color(.7f, .66f, .58f)); return; }
            int coin = 0; foreach (var c in waiting) coin += c.coin;
            Shadow(new Rect(r.x, r.y, 400, 24), TreasuryVault.Coin(coin) + " and " + waiting.FindAll(c => c.count > 0).Count + " lots wait for you", text, new Color(1, .86f, .4f));
            if (GUI.Button(new Rect(r.xMax - 150, r.y - 2, 150, 30), "Collect all", micro)) session.ExchangeCollect();
            var view = new Rect(r.x, r.y + 36, r.width, r.height - 36); float rowH = 40;
            collectScroll = GUI.BeginScrollView(view, collectScroll, new Rect(0, 0, view.width - 20, waiting.Count * rowH));
            for (int i = 0; i < waiting.Count; i++)
            {
                var c = waiting[i]; float y = i * rowH;
                if (c.count > 0 && !string.IsNullOrEmpty(c.item)) ItemSquare(new Rect(2, y + 3, 34, 34), new ItemStack { item = c.item, count = c.count });
                else Shadow(new Rect(2, y + 3, 34, 34), "¤", glyph, new Color(1, .86f, .4f));
                GUI.Label(new Rect(46, y + 2, view.width - 200, 36), c.note, tiny);
                if (c.coin > 0) Shadow(new Rect(view.width - 150, y + 8, 130, 24), TreasuryVault.Coin(c.coin), text, new Color(1, .86f, .4f));
            }
            GUI.EndScrollView();
        }
    }
}
