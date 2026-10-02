using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Loot on the HUD (loot DESIGN.md 4, step L1):
    /// - the loot window (E on a body that holds gear): framed in the colour of the best thing on the body, a coins row, one row
    ///   per thing with its quality border, coloured name and upgrade arrow; a click takes one, "Take all [E]" takes everything
    ///   that fits, and the rest stays on the body;
    /// - the green upgrade arrow on item squares (bags, merchants, the loot window) for gear you can wear that beats what you wear;
    /// - the better-or-worse lines on a gear tooltip ("+3 weapon damage" in green, "-2 Stamina" in red);
    /// - the colours of the rarity call-outs: loot chat lines and the "RARE" and "EPIC" toasts;
    /// - a named piece's tooltip lines (step L2): unique, effects, its set and bonuses, and its source.
    /// </summary>
    public sealed partial class EncounterHud
    {
        static bool lootVisible; static Rect lootRect;
        const float LootRowH = Slot + 8, LootTop = 58, LootFoot = 56;
        static bool LootUiBlocks(Vector2 p) { return lootVisible && lootRect.Contains(p); }
        /// <summary>Capture tools: the tooltip of this item, shown as if hovered at <see cref="PinnedTooltipAt"/> (null = none).</summary>
        public static string PinnedTooltip; public static Vector2 PinnedTooltipAt;
        static readonly Color Gain = new Color(.3f, 1, .3f);
        const string GainHex = "#4dff4d", LossHex = "#ff5547";
        GUIStyle lootSub, lootName, lootTitle;

        /// <summary>The window for this many rows (coins and things), beside the character sheet and clear of the bags.</summary>
        static Rect LootRectFor(int rows) { return new Rect(680, 170, 366, LootTop + Mathf.Max(1, rows) * LootRowH + LootFoot); }

        /// <summary>The loot window when it is open, and a pinned tooltip (captures). The tooltip is drawn here unless an item window will draw it later.</summary>
        void DrawLoot()
        {
            lootVisible = session.LootOpen;
            if (lootVisible) LootWindow();
            if (PinnedTooltip != null && session.Items != null) { ItemTooltip(session.Items.Get(PinnedTooltip), true); tooltipAt = PinnedTooltipAt; }
            if (tooltip != null && !(session.InventoryOpen || session.CharacterOpen || session.VendorNpc != null)) DrawTooltip();
        }
        void LootWindow()
        {
            ItemStyles(); if (lootSub == null) lootSub = new GUIStyle(tiny) { wordWrap = false, clipping = TextClipping.Clip };
            if (lootName == null) lootName = new GUIStyle(frameName) { fontSize = 13, wordWrap = false, clipping = TextClipping.Clip };
            if (lootTitle == null) lootTitle = new GUIStyle(frameName) { wordWrap = false };   // long names clip on one line, never wrap out of their row
            var body = session.LootBody; var items = session.LootItems; int coins = session.LootCoins;
            var w = lootRect = LootRectFor(items.Count + (coins > 0 ? 1 : 0));
            int best = session.BestQuality(items); var edge = best >= 2 ? LootBeacon.Colour(best) * .8f : new Color(.3f, .22f, .12f);
            Fill(new Rect(w.x - 3, w.y - 3, w.width + 6, w.height + 6), new Color(edge.r, edge.g, edge.b, .98f)); Fill(w, new Color(.08f, .075f, .07f, .97f));
            Shadow(new Rect(w.x + 14, w.y + 8, w.width - 28, 24), body != null ? body.actor.DisplayName : "Loot", lootTitle, gold);
            Shadow(new Rect(w.x + 14, w.y + 30, w.width - 28, 20), "Click a row to take it", lootSub, new Color(.7f, .66f, .58f));
            var e = Event.current; var mouse = e.mousePosition; float y = w.y + LootTop;
            if (coins > 0)
            {
                var row = new Rect(w.x + 8, y - 2, w.width - 16, LootRowH - 4);
                if (row.Contains(mouse)) Fill(row, new Color(1, 1, 1, .05f));
                var r = new Rect(w.x + 14, y, Slot, Slot);
                Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), new Color(.62f, .5f, .2f)); Fill(r, new Color(.09f, .1f, .11f));
                Disc(new Rect(r.x + 14, r.y + 14, 24, 24), new Color(1, .82f, .3f)); Disc(new Rect(r.x + 19, r.y + 19, 14, 14), new Color(.85f, .62f, .15f));
                Shadow(new Rect(r.xMax + 12, r.y + 14, 220, 24), coins + " gold", frameName, new Color(1, .86f, .4f));
                if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 1) && row.Contains(mouse)) { session.TakeLootCoins(); e.Use(); return; }
                y += LootRowH;
            }
            for (int i = 0; i < items.Count; i++)
            {
                var drop = items[i]; var d = session.Items?.Get(drop.item);
                var row = new Rect(w.x + 8, y - 2, w.width - 16, LootRowH - 4);
                if (row.Contains(mouse)) Fill(row, new Color(1, 1, 1, .05f));
                var r = new Rect(w.x + 14, y, Slot, Slot);
                ItemSquare(r, new ItemStack { item = drop.item, count = drop.count });
                if (d != null)
                {
                    int q = Mathf.Clamp(d.quality, 0, 4);
                    Shadow(new Rect(r.xMax + 12, r.y + 4, w.width - Slot - 40, 22), d.name, lootName, q == 0 ? ItemDatabase.QualityColors[0] : LootBeacon.Colour(q));
                    Shadow(new Rect(r.xMax + 12, r.y + 27, w.width - Slot - 40, 20), LootKind(d, drop.count), lootSub, new Color(.75f, .72f, .66f));
                    if (row.Contains(mouse)) ItemTooltip(d, true);
                }
                if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 1) && row.Contains(mouse)) { session.TakeLoot(i); e.Use(); return; }
                y += LootRowH;
            }
            if (GUI.Button(new Rect(w.x + 14, w.yMax - 44, 150, 32), "Take all [E]", micro)) { session.TakeAllLoot(); return; }
            if (GUI.Button(new Rect(w.xMax - 124, w.yMax - 44, 110, 32), "Close [Esc]", micro)) session.CloseLoot();
        }
        /// <summary>A row's second line: "Rare  ·  Main hand", or what kind of thing it is, with the count when there are several.</summary>
        static string LootKind(ItemDef d, int count)
        {
            string kind = d.kind == "gear" ? ItemDatabase.QualityNames[Mathf.Clamp(d.quality, 0, 4)] + "  ·  " + ItemDatabase.SlotNames[Mathf.Max(0, ItemDatabase.SlotIndex(d.slot))]
                : d.kind == "junk" ? "Junk" : Inventory.IsHide(d) ? "Hide" : d.kind == "material" ? "Crafting material" : d.kind == "consumable" ? (d.food ? "Food" : "Potion") : ItemDatabase.QualityNames[Mathf.Clamp(d.quality, 0, 4)];
            return count > 1 ? kind + "  ·  " + count : kind;
        }

        /// <summary>Marks on an item square: a green arrow in its corner for gear you can wear now that beats what you wear in its slot (LootJudge).</summary>
        void SquareMarks(Rect r, ItemDef d)
        {
            if (d == null || d.kind != "gear" || session.Items == null || !LootJudge.IsUpgrade(d, Worn(d), session.Progress.Level)) return;
            UpArrow(r.x + 4, r.y + 4, new Color(0, 0, 0, .85f), 1); UpArrow(r.x + 4, r.y + 4, Gain, 0);
        }
        /// <summary>What you wear in a piece's slot, or null when the slot is empty.</summary>
        ItemDef Worn(ItemDef d)
        {
            int slot = ItemDatabase.SlotIndex(d.slot); var eq = session.Progress.equipment;
            return slot < 0 || slot >= eq.Count || eq[slot].Empty ? null : session.Items.Get(eq[slot].item);
        }
        /// <summary>A small arrow pointing up, 12 wide and 14 tall, from rects (offset by <paramref name="shift"/> for its shadow).</summary>
        void UpArrow(float x, float y, Color c, float shift)
        {
            x += shift; y += shift;
            for (int k = 0; k < 6; k++) Fill(new Rect(x + 5 - k, y + k * 1.2f, 2 + 2 * k, 1.4f), c);
            Fill(new Rect(x + 3.5f, y + 7, 5, 7), c);
        }
        /// <summary>
        /// A gear tooltip's comparison with what you wear in its slot: the difference stat by stat, gains in green and losses in red
        /// (LootJudge.DeltaLines), and "An upgrade" when it scores higher and you can wear it.
        /// </summary>
        string CompareLines(ItemDef d, ItemDef worn)
        {
            var l = new List<string>();
            l.Add(worn == null ? "<color=" + GainHex + ">Nothing worn in this slot</color>" : "Compared with " + worn.name + ":");
            var deltas = LootJudge.DeltaLines(d, worn);
            foreach (var (line, gain) in deltas) l.Add("<color=" + (gain ? GainHex : LossHex) + ">" + line + "</color>");
            if (worn != null && deltas.Count == 0) l.Add("The same as what you wear.");
            if (LootJudge.IsUpgrade(d, worn, session.Progress.Level)) l.Add("<color=" + GainHex + "><b>An upgrade</b></color>");
            return string.Join("\n", l);
        }
        /// <summary>
        /// A named piece's tooltip lines (step L2; LootDatabase.TooltipParts): "Unique", its effects in green, its set in gold with
        /// the pieces worn bright and the rest grey, each bonus green when it is on and grey when not, and where it comes from.
        /// Null for anything without a gear entry, or when the named loot is not loaded.
        /// </summary>
        string LootLines(ItemDef d)
        {
            if (d == null || session.Loot == null) return null;
            var parts = session.Loot.TooltipParts(d.id, session.Progress); if (parts.Count == 0) return null;
            var l = new List<string>();
            foreach (var (line, kind) in parts)
                switch (kind)
                {
                    case LootDatabase.TipLine.Effect: case LootDatabase.TipLine.BonusOn: l.Add("<color=" + GainHex + ">" + (kind == LootDatabase.TipLine.BonusOn ? "  " : "") + line + "</color>"); break;
                    case LootDatabase.TipLine.Set: l.Add("<color=#ffd24d>" + line + "</color>"); break;
                    case LootDatabase.TipLine.PieceWorn: l.Add("<color=#f2e6c4>  " + line + "</color>"); break;
                    case LootDatabase.TipLine.PieceMissing: case LootDatabase.TipLine.BonusOff: l.Add("<color=#8c867a>  " + line + "</color>"); break;
                    case LootDatabase.TipLine.Source: l.Add("<color=#c8bfa8>" + line + "</color>"); break;
                    default: l.Add(line); break;
                }
            return string.Join("\n", l);
        }
        /// <summary>The rarity call-out toast's colours: a "RARE" or "EPIC" kicker in its quality's colour over the name in a lighter tint. False for any other toast.</summary>
        static bool LootToastColours(string kicker, out Color kick, out Color name)
        {
            int q = kicker == "EPIC" ? 4 : kicker == "RARE" ? 3 : -1;
            kick = q > 0 ? LootBeacon.Colour(q) : Color.white; name = Color.Lerp(kick, Color.white, .45f);
            return q > 0;
        }
    }
}
