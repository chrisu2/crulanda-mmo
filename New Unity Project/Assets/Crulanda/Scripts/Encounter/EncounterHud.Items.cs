using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Items UI:
    /// - Bags (I): a 24-slot grid, and under it one labelled row per trade bag worn ("Ore-poke 3/8"), its slots taking only what
    ///   that bag holds; with none worn, a line on where to get one.
    /// - Character sheet (C): nine equipment slots around your stats.
    /// - Merchant window: opens when you talk to a merchant.
    /// Drag and drop moves items: bag to bag (move/stack), bag to equipment slot (equip), equipment slot to bag (take off),
    /// bag to the merchant (sell), and anywhere outside the windows (destroy, after confirming).
    /// Right-click equips or uses an item, sells it while a merchant is open, or takes worn gear off.
    /// </summary>
    public sealed partial class EncounterHud
    {
        static bool bagsVisible, charVisible, vendorVisible;
        static readonly Rect CharRect = new Rect(190, 110, 470, 600), VendorRect = new Rect(670, 110, 370, 600);
        /// <summary>The bags window: 400 tall with the trade bags' part under the grid (bagsExtra, set as it is drawn), rising up the
        /// screen as that part grows so its foot stays above the action bar (with all four bags worn it rises over the minimap's foot).</summary>
        static Rect BagsRect { get { float h = 400 + bagsExtra; return new Rect(1054, Mathf.Min(290, 788 - h), 372, h); } }
        static float bagsExtra = TradeBagNoteH;
        /// <summary>True when the open bags window lies over the given screen rect (the minimap's buttons then stand down, since IMGUI
        /// gives a click to the control laid out first). bagsExtra holds the height on screen when the click arrives.</summary>
        public static bool BagsCover(Rect r) { return bagsVisible && BagsRect.Overlaps(r); }
        const float PouchSlot = 38, PouchGap = 4, PouchLabel = 22, TradeBagNoteH = 40;
        /// <summary>The bags window's line when no trade bag is worn (GAME-ONLY).</summary>
        public const string TradeBagNote = "Trade bags: Maud Tanner makes them, by the South road in Oakhaven.";
        static bool ItemUiBlocks(Vector2 p)
        { return (bagsVisible && BagsRect.Contains(p)) || (charVisible && CharRect.Contains(p)) || (vendorVisible && VendorRect.Contains(p)) || confirmDestroy >= 0; }
        const float Slot = 52, Gap = 6;
        // Drag state: from a bag slot (>= 0) or an equipment slot (dragEquip >= 0).
        int dragBag = -1, dragEquip = -1; static int confirmDestroy = -1;
        GUIStyle glyph, countStyle, tipTitle, tipBody, pouchNote;
        Vector2 vendorScroll;

        void ItemStyles()
        {
            if (glyph != null) return;
            glyph = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            countStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerRight };
            tipTitle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = true };
            tipBody = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            pouchNote = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleRight, wordWrap = false, clipping = TextClipping.Clip };
        }
        static string Glyph(ItemDef d)
        {
            if (d == null) return "?";
            if (d.kind == "junk") return "✦";
            if (d.kind == "consumable") return d.food ? "Fd" : "Hp";
            if (Inventory.IsHide(d)) return "Lt";
            if (d.kind == "material") return "Mt";
            if (d.kind == "tool") return "Tl";
            if (d.kind == "bag") return "Bg";
            switch (d.slot) { case "head": return "Hd"; case "neck": return "Nk"; case "shoulders": return "Sh"; case "chest": return "Ch"; case "hands": return "Gl"; case "legs": return "Lg"; case "feet": return "Ft"; case "mainhand": return "Wp"; case "offhand": return "Of"; }
            return "?";
        }
        /// <summary>Draws one item square: dark bed, quality border, glyph, stack count. Returns its rect.</summary>
        void ItemSquare(Rect r, ItemStack s, string emptyLabel = null)
        {
            var d = s == null || s.Empty ? null : session.Items?.Get(s.item);
            Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), d == null ? new Color(.25f, .22f, .18f) : ItemDatabase.QualityColors[Mathf.Clamp(d.quality, 0, 4)] * .9f);
            Fill(r, new Color(.09f, .1f, .11f)); Fill(new Rect(r.x, r.y, r.width, r.height / 2), new Color(1, 1, 1, .04f));
            if (d != null)
            {
                Shadow(r, Glyph(d), glyph, ItemDatabase.QualityColors[Mathf.Clamp(d.quality, 0, 4)]);
                SquareMarks(r, d);
                if (s.count > 1) Shadow(new Rect(r.x, r.y, r.width - 3, r.height - 1), s.count.ToString(), countStyle, Color.white);
            }
            else if (emptyLabel != null) Shadow(r, emptyLabel, new GUIStyle(countStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 10 }, new Color(.5f, .48f, .42f));
        }
        string tooltip; Vector2 tooltipAt;
        void ItemTooltip(ItemDef d, bool compare)
        {
            if (d == null) return;
            var lines = new List<string>();
            if (d.kind == "gear") lines.Add(ItemDatabase.SlotNames[ItemDatabase.SlotIndex(d.slot)] + "  ·  " + ItemDatabase.QualityNames[Mathf.Clamp(d.quality, 0, 4)]);
            else lines.Add(d.kind == "junk" ? "Junk" : Inventory.IsHide(d) ? "Hide" : d.kind == "material" ? "Crafting material" : d.kind == "tool" ? "Tool" : d.kind == "bag" ? "Trade bag" : d.food ? "Food" : "Potion");
            var stats = ItemDatabase.StatLines(d); if (stats.Length > 0) lines.Add(stats);
            if (d.kind == "bag")
                lines.Add(Inventory.Wears(session.Progress, d.id) ? "<color=#8f8>Worn: " + d.slots + " slots for " + Inventory.HoldsWords(d.holds) + ".</color>"
                    : "Trade bag: " + d.slots + " slots for " + Inventory.HoldsWords(d.holds) + ". Use it to wear it.");
            if (Inventory.IsHide(d)) lines.Add(EncounterSession.HideLine);
            var holder = string.IsNullOrEmpty(d.pouch) ? null : Inventory.BagFor(session.Items, d.pouch);
            if (holder != null) lines.Add("Goes in " + Article(holder.name) + " " + LowerFirst(holder.name) + ".");
            if (d.level > 1) lines.Add((d.level > session.Progress.Level ? "<color=#ff5544>" : "") + "Requires level " + d.level + (d.level > session.Progress.Level ? "</color>" : ""));
            if (!string.IsNullOrEmpty(d.description)) lines.Add("<i>" + d.description + "</i>");
            lines.Add("Sells for " + (d.value) + " gold");
            if (compare && d.kind == "gear")
            {
                var worn = session.Progress.equipment[ItemDatabase.SlotIndex(d.slot)];
                var w = worn.Empty ? null : session.Items.Get(worn.item);
                lines.Add(CompareLines(d, w));
            }
            tooltip = "<b><color=#" + ColorUtility.ToHtmlStringRGB(ItemDatabase.QualityColors[Mathf.Clamp(d.quality, 0, 4)]) + ">" + d.name + "</color></b>\n" + string.Join("\n", lines);
            tooltipAt = Event.current.mousePosition;
        }
        /// <summary>"a" or "an" before a word (by its first letter).</summary>
        static string Article(string word) { return !string.IsNullOrEmpty(word) && "aeiouAEIOU".IndexOf(word[0]) >= 0 ? "an" : "a"; }
        void DrawTooltip()
        {
            if (tooltip == null) return;
            var style = new GUIStyle(tipBody) { richText = true };
            float w = 290, h = style.CalcHeight(new GUIContent(tooltip), w - 20) + 16;
            var r = new Rect(Mathf.Min(tooltipAt.x + 18, 1440 - w - 6), Mathf.Clamp(tooltipAt.y - h - 8, 6, 900 - h - 6), w, h);
            Fill(r, new Color(.03f, .04f, .06f, .96f)); Fill(new Rect(r.x, r.y, r.width, 2), gold);
            GUI.Label(new Rect(r.x + 10, r.y + 8, w - 20, h), tooltip, style);
            tooltip = null;
        }

        // ---------- bags ----------
        /// <summary>The trade bags' rows under the 24 slots: each worn bag the content knows (its first slot and how many), then any
        /// slots past them that still hold something (a bag no longer made), so they can be emptied.</summary>
        List<(ItemDef bag, int start, int count)> PouchRows(EncounterProgress p)
        {
            var rows = new List<(ItemDef, int, int)>(); if (session.Items == null) return rows;
            int end = Inventory.BagSize;
            foreach (var r in Inventory.Pouches(p, session.Items)) { rows.Add((r.bag, r.start, r.count)); end = r.start + r.count; }
            for (int i = end; i < p.bag.Count; i++) if (!p.bag[i].Empty) { rows.Add((null, end, p.bag.Count - end)); break; }
            return rows;
        }
        /// <summary>A trade bag's row: its label, its slots eight to a line, and a gap.</summary>
        static float PouchRowH(int count) { return PouchLabel + Mathf.CeilToInt(count / 8f) * (PouchSlot + PouchGap) + 6; }
        void DrawBags()
        {
            ItemStyles(); var p = session.Progress;
            var rows = PouchRows(p); float extra = rows.Count == 0 ? TradeBagNoteH : 6; foreach (var row in rows) extra += PouchRowH(row.count);
            bagsExtra = extra; var w = BagsRect;
            Fill(new Rect(w.x - 3, w.y - 3, w.width + 6, w.height + 6), new Color(.3f, .22f, .12f, .98f)); Fill(w, new Color(.08f, .075f, .07f, .97f));
            Shadow(new Rect(w.x + 14, w.y + 8, 200, 28), "BAGS", heading, gold);
            Shadow(new Rect(w.x + 150, w.y + 12, 210, 24), p.gold + " gold  ·  " + Inventory.FreeSlots(p) + " free", text, new Color(1, .86f, .4f));
            var e = Event.current; var mouse = e.mousePosition;
            for (int i = 0; i < p.bag.Count && i < Inventory.BagSize; i++)
                BagSlot(new Rect(w.x + 14 + (i % 6) * (Slot + Gap), w.y + 48 + (i / 6) * (Slot + Gap), Slot, Slot), i, null, e, mouse);
            float y = w.y + 48 + 4 * (Slot + Gap) + 4;
            if (rows.Count == 0) Shadow(new Rect(w.x + 14, y, w.width - 28, TradeBagNoteH), TradeBagNote, tiny, new Color(.82f, .74f, .56f));
            else
            {
                float ry = y + 6;
                foreach (var (bag, start, count) in rows)
                {
                    int used = 0; for (int k = 0; k < count && start + k < p.bag.Count; k++) if (!p.bag[start + k].Empty) used++;
                    string label = (bag != null ? bag.name : "Loose slots") + "  " + used + "/" + count;
                    measureContent.text = label; float lw = frameName.CalcSize(measureContent).x;
                    Shadow(new Rect(w.x + 14, ry, w.width - 28, PouchLabel), label, frameName, bag != null ? gold : new Color(.7f, .66f, .6f));
                    string note = bag != null ? Inventory.HoldsWords(bag.holds) : "a bag no longer made: empty them out";
                    measureContent.text = note; var noteRect = new Rect(w.x + 14 + lw + 12, ry + 1, w.width - 28 - lw - 12, PouchLabel);
                    if (pouchNote.CalcSize(measureContent).x <= noteRect.width) Shadow(noteRect, note, pouchNote, new Color(.7f, .66f, .58f));
                    ry += PouchLabel;
                    for (int k = 0; k < count && start + k < p.bag.Count; k++)
                        BagSlot(new Rect(w.x + 14 + (k % 8) * (PouchSlot + PouchGap), ry + (k / 8) * (PouchSlot + PouchGap), PouchSlot, PouchSlot), start + k, bag, e, mouse);
                    ry += Mathf.CeilToInt(count / 8f) * (PouchSlot + PouchGap) + 6;
                }
            }
            y += extra;
            GUI.Label(new Rect(w.x + 14, y, w.width - 28, 40), "Drag to move · right-click to " + (session.VendorNpc != null ? "sell" : "equip / use") + " · drag out of the window to destroy", tiny);
            if (session.PotionCooldown > 0) Shadow(new Rect(w.x + 14, y + 36, 300, 20), "Potion ready in " + Mathf.CeilToInt(session.PotionCooldown) + "s", tiny, new Color(.9f, .7f, .5f));
            if (session.VendorNpc != null && GUI.Button(new Rect(w.x + 14, w.yMax - 44, 150, 32), "Sell junk", micro)) session.SellJunk();
            if (GUI.Button(new Rect(w.xMax - 124, w.yMax - 44, 110, 32), "Close [I]", micro)) { session.InventoryOpen = false; session.CloseVendor(); }
        }
        /// <summary>
        /// One bag slot: its square, its tooltip (an empty trade-bag slot says what it takes), and the mouse on it: drag from it, drop
        /// on it (a move, or worn gear taken off into it), right-click to sell it to an open merchant or else equip or use it. A trade
        /// bag is worn on right-click even at a merchant (one just bought is used at her counter); dragging it to her sells it.
        /// </summary>
        void BagSlot(Rect r, int i, ItemDef pouch, Event e, Vector2 mouse)
        {
            var p = session.Progress;
            ItemSquare(r, dragBag == i ? null : p.bag[i]);
            if (!r.Contains(mouse)) return;
            if (!p.bag[i].Empty && dragBag < 0 && dragEquip < 0) ItemTooltip(session.Items?.Get(p.bag[i].item), true);
            else if (p.bag[i].Empty && pouch != null && dragBag < 0 && dragEquip < 0) { tooltip = "<b>" + pouch.name + "</b>\nTakes " + Inventory.HoldsWords(pouch.holds) + "."; tooltipAt = mouse; }
            if (e.type == EventType.MouseDown && e.button == 0 && !p.bag[i].Empty) { dragBag = i; e.Use(); }
            else if (e.type == EventType.MouseDown && e.button == 1 && !p.bag[i].Empty)
            { var it = session.Items?.Get(p.bag[i].item); if (session.VendorNpc != null && it?.kind != "bag") session.SellBag(i); else session.EquipFromBag(i); e.Use(); }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (dragBag >= 0) session.MoveBag(dragBag, i);
                else if (dragEquip >= 0) session.UnequipSlot(dragEquip, i);
                dragBag = dragEquip = -1; e.Use();
            }
        }

        // ---------- character sheet ----------
        static readonly int[] LeftSlots = { 0, 1, 2, 3 }, RightSlots = { 4, 5, 6 }, BottomSlots = { 7, 8 };
        Rect EquipRect(int slot)
        {
            var w = CharRect;
            int li = System.Array.IndexOf(LeftSlots, slot), ri = System.Array.IndexOf(RightSlots, slot), bi = System.Array.IndexOf(BottomSlots, slot);
            if (li >= 0) return new Rect(w.x + 18, w.y + 70 + li * (Slot + 14), Slot, Slot);
            if (ri >= 0) return new Rect(w.xMax - 18 - Slot, w.y + 70 + ri * (Slot + 14), Slot, Slot);
            return new Rect(w.x + w.width / 2 - Slot - 8 + bi * (Slot + 16), w.y + 70 + 4 * (Slot + 14) - 10, Slot, Slot);
        }
        void DrawCharacter()
        {
            ItemStyles(); var p = session.Progress; var w = CharRect; var pl = session.Player; var cls = session.ClassDef;
            Fill(new Rect(w.x - 3, w.y - 3, w.width + 6, w.height + 6), new Color(.3f, .22f, .12f, .98f)); Fill(w, new Color(.08f, .075f, .07f, .97f));
            Shadow(new Rect(w.x + 16, w.y + 10, w.width - 32, 28), "You  ·  Level " + p.Level + " " + cls.displayName, heading, gold);
            // A standing figure in the middle, lightly tinted by what you wear.
            var mid = new Rect(w.x + 100, w.y + 64, w.width - 200, 250);
            Fill(mid, new Color(1, 1, 1, .03f));
            float cx = mid.center.x;
            Disc(new Rect(cx - 22, mid.y + 20, 44, 44), new Color(.76f, .6f, .48f));
            Fill(new Rect(cx - 34, mid.y + 68, 68, 86), p.equipment[3].Empty ? new Color(.35f, .3f, .25f) : ItemDatabase.QualityColors[Mathf.Clamp(session.Items?.Get(p.equipment[3].item)?.quality ?? 1, 0, 4)] * .55f);
            Fill(new Rect(cx - 54, mid.y + 70, 18, 78), new Color(.35f, .3f, .25f)); Fill(new Rect(cx + 36, mid.y + 70, 18, 78), new Color(.35f, .3f, .25f));
            Fill(new Rect(cx - 30, mid.y + 156, 26, 84), new Color(.28f, .24f, .2f)); Fill(new Rect(cx + 4, mid.y + 156, 26, 84), new Color(.28f, .24f, .2f));
            var e = Event.current; var mouse = e.mousePosition;
            for (int slot = 0; slot < ItemDatabase.SlotIds.Length; slot++)
            {
                var r = EquipRect(slot); var s = p.equipment[slot];
                ItemSquare(r, dragEquip == slot ? null : s, ItemDatabase.SlotNames[slot].Split(' ')[0]);
                if (!r.Contains(mouse)) continue;
                if (!s.Empty && dragBag < 0 && dragEquip < 0) ItemTooltip(session.Items?.Get(s.item), false);
                else if (s.Empty && dragBag < 0) { tooltip = ItemDatabase.SlotNames[slot] + " (empty)"; tooltipAt = mouse; }
                if (e.type == EventType.MouseDown && e.button == 0 && !s.Empty) { dragEquip = slot; e.Use(); }
                else if (e.type == EventType.MouseDown && e.button == 1 && !s.Empty) { session.UnequipSlot(slot); e.Use(); }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    if (dragBag >= 0)
                    {
                        var d = session.Items?.Get(p.bag[dragBag].item);
                        if (d != null && ItemDatabase.SlotIndex(d.slot) == slot) session.EquipFromBag(dragBag);
                        else session.Message("That doesn't go there.");
                    }
                    dragBag = dragEquip = -1; e.Use();
                }
            }
            // Stats.
            var st = pl.Stats; float y = w.y + 350;
            Shadow(new Rect(w.x + 20, y, 200, 22), "ATTRIBUTES", frameName, gold); Shadow(new Rect(w.x + 250, y, 200, 22), "COMBAT", frameName, gold); y += 26;
            string[] left = {
                "Stamina  " + st.GetRounded(Crulanda.Core.StatType.Stamina), "Strength  " + st.GetRounded(Crulanda.Core.StatType.Strength),
                "Agility  " + st.GetRounded(Crulanda.Core.StatType.Agility), "Intellect  " + st.GetRounded(Crulanda.Core.StatType.Intellect),
                "Spirit  " + st.GetRounded(Crulanda.Core.StatType.Spirit) };
            string[] right = {
                "Health  " + pl.Health.Pool.Max, EncounterHud.ResourceName(cls.resource) + "  " + pl.Resource.Pool.Max,
                "Attack power  " + st.GetRounded(Crulanda.Core.StatType.AttackPower), "Armor  " + st.GetRounded(Crulanda.Core.StatType.Armor),
                "Damage taken  " + Mathf.RoundToInt(100f * 100 / (100 + st.Get(Crulanda.Core.StatType.Armor))) + "%" };
            for (int i = 0; i < left.Length; i++) { Shadow(new Rect(w.x + 24, y + i * 24, 220, 22), left[i], text, Color.white); Shadow(new Rect(w.x + 254, y + i * 24, 220, 22), right[i], text, Color.white); }
            y += left.Length * 24 + 10;
            var t = session.Items != null ? Inventory.Totals(p, session.Items) : new ItemDef();
            GUI.Label(new Rect(w.x + 24, y, w.width - 48, 40), "From gear: +" + t.weaponDamage + " weapon damage, " + t.armor + " armor, +" + (t.stamina + t.strength + t.agility + t.intellect + t.spirit) + " attributes.", tiny);
            if (GUI.Button(new Rect(w.xMax - 124, w.yMax - 44, 110, 32), "Close [C]", micro)) session.CharacterOpen = false;
        }

        // ---------- merchant ----------
        void DrawVendor()
        {
            ItemStyles(); var w = VendorRect; var p = session.Progress;
            Fill(new Rect(w.x - 3, w.y - 3, w.width + 6, w.height + 6), new Color(.3f, .22f, .12f, .98f)); Fill(w, new Color(.9f, .84f, .7f, .98f));
            GUI.contentColor = new Color(.2f, .13f, .07f); GUI.Label(new Rect(w.x + 16, w.y + 10, w.width - 32, 30), session.VendorNpc + "'s wares", heading);
            GUI.Label(new Rect(w.x + 16, w.y + 42, w.width - 32, 22), "You have " + p.gold + " gold. Drag or right-click items in your bags to sell.", tiny); GUI.contentColor = Color.white;
            var view = new Rect(w.x + 12, w.y + 70, w.width - 24, w.height - 130);
            var stock = session.VendorStock; float rowH = Slot + 8;
            vendorScroll = GUI.BeginScrollView(view, vendorScroll, new Rect(0, 0, view.width - 20, stock.Count * rowH));
            var mouse = Event.current.mousePosition;
            for (int i = 0; i < stock.Count; i++)
            {
                var d = session.Items.Get(stock[i]); if (d == null) continue;
                var r = new Rect(4, i * rowH + 4, Slot, Slot);
                ItemSquare(r, new ItemStack { item = d.id, count = 1 });
                int price = Inventory.Price(d); bool afford = p.gold >= price;
                GUI.contentColor = ItemDatabase.QualityColors[Mathf.Clamp(d.quality, 0, 4)] * .6f; GUI.Label(new Rect(r.xMax + 10, r.y + 2, 200, 22), d.name, frameName);
                GUI.contentColor = afford ? new Color(.35f, .25f, .08f) : new Color(.7f, .15f, .1f); GUI.Label(new Rect(r.xMax + 10, r.y + 26, 160, 22), price + " gold" + (d.level > p.Level ? "  ·  level " + d.level : ""), tiny);
                GUI.contentColor = Color.white;
                GUI.enabled = afford;
                if (GUI.Button(new Rect(view.width - 90, r.y + 12, 64, 28), "Buy", micro)) session.Buy(d.id);
                GUI.enabled = true;
                if (new Rect(0, i * rowH, view.width, rowH).Contains(mouse)) { ItemTooltip(d, true); tooltipAt = GUIUtility.GUIToScreenPoint(mouse); tooltipAt = new Vector2(w.x + 12 + mouse.x - vendorScroll.x, w.y + 70 + mouse.y - vendorScroll.y); }
            }
            GUI.EndScrollView();
            // Dropping a dragged bag item on the merchant sells it.
            var e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && dragBag >= 0 && w.Contains(e.mousePosition)) { session.SellBag(dragBag); dragBag = -1; e.Use(); }
            if (GUI.Button(new Rect(w.xMax - 124, w.yMax - 48, 110, 34), "Goodbye", micro)) session.CloseVendor();
        }

        /// <summary>After all windows: the dragged item follows the pointer; letting go outside every window asks to destroy it.</summary>
        void DrawDragAndConfirm()
        {
            var e = Event.current; var p = session.Progress;
            if (dragBag >= 0 || dragEquip >= 0)
            {
                var s = dragBag >= 0 ? p.bag[dragBag] : p.equipment[dragEquip];
                ItemSquare(new Rect(e.mousePosition.x - Slot / 2, e.mousePosition.y - Slot / 2, Slot, Slot), s);
                if (e.type == EventType.MouseUp)
                {
                    bool overWindow = (bagsVisible && BagsRect.Contains(e.mousePosition)) || (charVisible && CharRect.Contains(e.mousePosition)) || (vendorVisible && VendorRect.Contains(e.mousePosition)) || TradesUiBlocks(e.mousePosition) || LootUiBlocks(e.mousePosition);
                    if (!overWindow && dragBag >= 0) confirmDestroy = dragBag;
                    dragBag = dragEquip = -1;
                }
            }
            if (confirmDestroy >= 0 && confirmDestroy < p.bag.Count && !p.bag[confirmDestroy].Empty)
            {
                var r = new Rect(560, 360, 320, 130);
                Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(.5f, .15f, .1f)); Fill(r, new Color(.08f, .07f, .07f, .98f));
                GUI.Label(new Rect(r.x + 16, r.y + 14, r.width - 32, 50), "Destroy " + session.ItemName(p.bag[confirmDestroy].item) + "?", text);
                if (GUI.Button(new Rect(r.x + 16, r.yMax - 50, 130, 36), "Destroy", button)) { session.DestroyBag(confirmDestroy); confirmDestroy = -1; }
                if (GUI.Button(new Rect(r.xMax - 146, r.yMax - 50, 130, 36), "Keep", button)) confirmDestroy = -1;
            }
            else confirmDestroy = -1;
            DrawTooltip();
        }
    }
}
