using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The quest book's Armoury tab (loot DESIGN.md 4, row 8; step L3; ArmouryLog), modelled on the Discoveries tab. Left: found /
    /// total over the land, the looks seen, then every zone (this one first) and the pieces that drop anywhere, each with its own
    /// count. Right, on parchment: the chosen zone's named pieces by kind of source (bosses, camps, quests, merchants, hidden finds,
    /// rare finds). A piece not yet known is a silhouette with its slot and the kind of source; once its source is met its name and
    /// source show in grey; once held it shows in its quality's colour, with its tooltip on hover. Opening the tab clears the count
    /// on its button.
    /// </summary>
    public sealed partial class EncounterHud
    {
        string armouryZone; Vector2 armouryScroll; GUIStyle armName, armSub;
        const float ArmouryRowH = 52;

        void BookArmoury(Rect list, Rect page)
        {
            ItemStyles(); session.SeenArmoury();
            if (qCount == null) qCount = new GUIStyle(qHead) { alignment = TextAnchor.MiddleRight };
            if (armName == null) { armName = new GUIStyle(qHead) { fontSize = 14, wordWrap = false, clipping = TextClipping.Clip }; armSub = new GUIStyle(qSmall) { fontSize = 12, wordWrap = false, clipping = TextClipping.Clip }; }
            var tallies = session.ArmouryTallies();
            int found = 0, total = 0; foreach (var t in tallies) { found += t.found; total += t.Total; }
            float y = list.y + 8;
            Shadow(new Rect(list.x + 10, y, list.width - 20, 20), "ARMOURY  (" + found + " / " + total + ")", qHead, gold); y += 22;
            Shadow(new Rect(list.x + 10, y, list.width - 20, 18), "Looks seen: " + (session.Armoury != null ? session.Armoury.LooksSeen : 0), tiny, new Color(.82f, .78f, .7f)); y += 26;
            if (tallies.Count == 0)
            { Shadow(new Rect(page.x, page.y + 10, page.width, 60), "No named gear is known on this road.", text, new Color(.8f, .8f, .8f)); return; }
            if (armouryZone == null || !tallies.Exists(t => t.zone == armouryZone)) armouryZone = tallies[0].zone;
            foreach (var t in tallies)
            {
                var r = new Rect(list.x + 8, y, list.width - 16, 34);
                GUI.enabled = armouryZone != t.zone;
                if (GUI.Button(r, t.zoneName + (t.here ? "  (here)" : ""), qList)) { armouryZone = t.zone; armouryScroll = Vector2.zero; }
                GUI.enabled = true;
                Shadow(new Rect(r.xMax - 90, r.y + 7, 80, 20), t.found + " / " + t.Total, qCount, t.Total > 0 && t.found == t.Total ? new Color(.55f, 1, .55f) : gold);
                y += 38;
            }
            Shadow(new Rect(list.x + 10, list.yMax - 62, list.width - 20, 54), "Every named piece in the land. Kill where one comes from and its name is written here; hold it once and it is yours to look at.", tiny, new Color(.75f, .75f, .72f));

            var sel = tallies.Find(t => t.zone == armouryZone);
            Fill(page, Parchment);
            float inner = page.width - 40, py = page.y + 16;
            Ink(new Rect(page.x + 20, py, inner, 32), sel.zoneName, qTitle, new Color(.42f, .22f, .05f)); py += 34;
            Ink(new Rect(page.x + 20, py, inner, 22), sel.found + " of " + sel.Total + " found" + (sel.here ? "  ·  you are here" : ""), qSmall, new Color(.4f, .32f, .22f)); py += 30;
            // Measure first, then draw in a scroll view: two pieces to a line under each kind of source.
            var view = new Rect(page.x + 20, py, page.width - 30, page.yMax - 16 - py); float width = view.width - 24, colW = (width - 12) / 2, height = 0;
            var groups = new List<(string heading, List<ArmouryLog.Entry> entries)>();
            foreach (var (kind, heading, _) in ArmouryLog.Kinds)
            {
                var g = sel.entries.FindAll(e => e.sourceKind == kind); if (g.Count == 0) continue;
                groups.Add((heading, g)); height += 28 + Mathf.CeilToInt(g.Count / 2f) * ArmouryRowH + 8;
            }
            var mouse = Event.current.mousePosition; bool over = view.Contains(mouse);
            armouryScroll = GUI.BeginScrollView(view, armouryScroll, new Rect(0, 0, width, height));
            var local = Event.current.mousePosition; float yy = 0;
            foreach (var (heading, entries) in groups)
            {
                int got = entries.FindAll(e => e.state == ArmouryLog.State.Found).Count;
                Ink(new Rect(0, yy, width, 24), heading + "  (" + got + " / " + entries.Count + ")", qHead, new Color(.42f, .22f, .05f)); yy += 28;
                for (int i = 0; i < entries.Count; i++)
                {
                    var r = new Rect((i % 2) * (colW + 12), yy + (i / 2) * ArmouryRowH, colW, ArmouryRowH - 6);
                    bool hover = over && r.Contains(local);
                    ArmouryRow(r, entries[i], hover);
                    if (hover && entries[i].state == ArmouryLog.State.Found && session.Items != null)
                    { ItemTooltip(session.Items.Get(entries[i].id), true); tooltipAt = new Vector2(view.x + local.x - armouryScroll.x, view.y + local.y - armouryScroll.y); }
                }
                yy += Mathf.CeilToInt(entries.Count / 2f) * ArmouryRowH + 8;
            }
            GUI.EndScrollView();
        }
        /// <summary>
        /// One piece: found, its square and its name in its quality's colour over where it comes from; known, a grey square with its
        /// slot and its name in grey; unknown, a dark silhouette with only its slot and the kind of source.
        /// </summary>
        void ArmouryRow(Rect r, ArmouryLog.Entry e, bool hover)
        {
            if (hover) Fill(r, new Color(.42f, .22f, .05f, .08f));
            var sq = new Rect(r.x + 2, r.y + 2, 40, 40); string name, line; Color ink;
            string slot = ItemDatabase.SlotIndex(e.slot) >= 0 ? ItemDatabase.SlotNames[ItemDatabase.SlotIndex(e.slot)] : "Gear";
            if (e.state == ArmouryLog.State.Found)
            {
                ItemSquare(sq, new ItemStack { item = e.id, count = 1 });
                name = e.name; line = slot + (e.source != null ? "  ·  " + e.source : ""); ink = ParchmentQuality(e.quality);
            }
            else
            {
                bool known = e.state == ArmouryLog.State.Known;
                Fill(new Rect(sq.x - 2, sq.y - 2, sq.width + 4, sq.height + 4), known ? new Color(.47f, .44f, .4f) : new Color(.36f, .31f, .25f));
                Fill(sq, known ? new Color(.2f, .19f, .18f) : new Color(.12f, .11f, .1f));
                var silhouette = IconDb.Slot(e.slot);   // the slot's grey silhouette, fainter for one not yet seen
                if (silhouette != null) { GUI.color = new Color(1, 1, 1, known ? .75f : .4f); GUI.DrawTexture(new Rect(sq.x + 1, sq.y + 1, sq.width - 2, sq.height - 2), silhouette, ScaleMode.ScaleToFit); GUI.color = Color.white; }
                else Shadow(sq, Glyph(new ItemDef { kind = "gear", slot = e.slot }), glyph, known ? new Color(.62f, .6f, .56f) : new Color(.36f, .34f, .31f));
                name = known ? e.name : slot + "  ·  unknown"; line = known ? slot + (e.source != null ? "  ·  " + e.source : "") : ArmouryLog.UnknownLine(e.sourceKind);
                ink = known ? new Color(.45f, .42f, .38f) : new Color(.5f, .44f, .36f);
            }
            Ink(new Rect(sq.xMax + 10, r.y + 3, r.width - 56, 22), name, armName, ink);
            Ink(new Rect(sq.xMax + 10, r.y + 24, r.width - 56, 20), line, armSub, new Color(.4f, .32f, .22f));
        }
        /// <summary>A quality's colour dark enough to read on parchment (poor and common in ink).</summary>
        static Color ParchmentQuality(int q) { return q <= 1 ? InkBrown : Color.Lerp(ItemDatabase.QualityColors[Mathf.Clamp(q, 0, 4)], Color.black, .3f); }
    }
}
