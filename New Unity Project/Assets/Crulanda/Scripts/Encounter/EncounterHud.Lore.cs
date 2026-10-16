using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The quest book's Lore tab, the book of lore (Phase 8.6 step 7; Chris, 2026-10-14: "find recipes/ book of lore"; decision 8: a tab
    /// in the quest book): its chapters down the left (Places, People, Creatures, Herbs and Ores, Factions, Pages, Leaves); for a
    /// zone's chapter, the zones with how much of each is known, and on the parchment the chosen zone's lines: what doing told you,
    /// what the game knows of it (stories, haunts and drops, ground and makings), the better half once its page or find is found
    /// (until then, where it lies by zone only), and each line's lore status. Lines not yet known are a count, never a name.
    /// Pages are the book of 2026-10-15 (the lore pages, then the letters and ledgers); Leaves are the found recipes' (a hook).
    /// </summary>
    public sealed partial class EncounterHud
    {
        string loreChapter = LoreBook.Places, loreZone, loreReading; Vector2 loreScroll;
        /// <summary>The People chapter's pseudo-zone of the adventurers met (the sims), who belong to no zone.</summary>
        const string SimsZone = "sims";

        void BookLore(QuestLog log, Rect list, Rect page)
        {
            var book = session.Lore;
            // A page just revealed (or chosen) opens in the Pages chapter.
            if (!string.IsNullOrEmpty(session.ReadingDocument) && session.ReadingDocument != loreReading) loreChapter = LoreBook.Pages;
            loreReading = session.ReadingDocument;
            float y = list.y + 8;
            Shadow(new Rect(list.x + 10, y, list.width - 20, 20), "THE BOOK OF LORE", qHead, gold); y += 26;
            // The chapters, two to a row.
            float bw = (list.width - 24) / 2;
            for (int i = 0; i < LoreBook.Chapters.Length; i++)
            {
                string c = LoreBook.Chapters[i]; var r = new Rect(list.x + 8 + (i % 2) * (bw + 8), y + (i / 2) * 32, bw, 28);
                GUI.enabled = loreChapter != c;
                if (GUI.Button(r, LoreBook.ChapterName(c), qList)) { loreChapter = c; loreScroll = Vector2.zero; if (c != LoreBook.Pages) session.ReadingDocument = null; loreReading = session.ReadingDocument; }
                GUI.enabled = true;
            }
            y += ((LoreBook.Chapters.Length + 1) / 2) * 32 + 10;
            if (loreChapter == LoreBook.Pages) { BookLorePages(log, new Rect(list.x, y, list.width, list.yMax - y), page); return; }
            if (book == null) { Shadow(new Rect(page.x, page.y + 10, page.width, 60), "The book of lore is empty: this build has no lore files.", text, new Color(.8f, .8f, .8f)); return; }
            if (loreChapter == LoreBook.Factions) { LoreLines(page, "Factions", "The peoples and powers of Crulanda, as the Standing tab tells them.", null, book.FactionLines(), 0); return; }
            if (loreChapter == LoreBook.Leaves)
            {
                var leaves = session.LoreLeaves != null ? session.LoreLeaves() : book.LeafLines != null ? book.LeafLines() : null;
                LoreLines(page, "Leaves", "Recipes found in the world: a smith's leaf, a cook's leaf, a herbalist's leaf.", null, leaves ?? new List<LoreBook.Line>(), 0,
                    "No leaves yet. The recipes you find, bought, looted or turned up in hidden places, are kept here.");
                return;
            }
            // A zone's chapter: the zones, this one first, then by level; the People chapter adds the adventurers met.
            var zones = new List<Crulanda.World.ZoneDefinition>();
            if (session.Zone != null)
            {
                zones.Add(session.Zone.Zone);
                var others = session.Zone.AllZones().FindAll(z => z != null && z.id != session.Zone.Zone.id);
                others.Sort((a, b) => a.levelMin != b.levelMin ? a.levelMin.CompareTo(b.levelMin) : string.CompareOrdinal(a.id, b.id));
                zones.AddRange(others);
            }
            if (zones.Count == 0) return;
            if (loreZone == null || (loreZone != SimsZone && !zones.Exists(z => z.id == loreZone)) || (loreZone == SimsZone && loreChapter != LoreBook.People)) loreZone = zones[0].id;
            foreach (var z in zones)
            {
                if (y > list.yMax - 44) break;
                var lines = book.Lines(z, loreChapter, false); int shown = 0; foreach (var l in lines) if (l.Shown) shown++;
                var r = new Rect(list.x + 8, y, list.width - 16, 30);
                GUI.enabled = loreZone != z.id;
                if (GUI.Button(r, z.displayName + (z.id == session.Zone.Zone.id ? "  (here)" : ""), qList)) { loreZone = z.id; loreScroll = Vector2.zero; }
                GUI.enabled = true;
                Shadow(new Rect(r.xMax - 90, r.y + 5, 80, 20), shown + " / " + lines.Count, qCount ?? qHead, lines.Count > 0 && shown == lines.Count ? new Color(.55f, 1, .55f) : gold);
                y += 34;
            }
            if (loreChapter == LoreBook.People && y <= list.yMax - 44)
            {
                var met = SimsMet(book); var r = new Rect(list.x + 8, y, list.width - 16, 30);
                GUI.enabled = loreZone != SimsZone;
                if (GUI.Button(r, "Adventurers met", qList)) { loreZone = SimsZone; loreScroll = Vector2.zero; }
                GUI.enabled = true;
                Shadow(new Rect(r.xMax - 90, r.y + 5, 80, 20), met.Count.ToString(), qCount ?? qHead, gold);
            }
            if (loreZone == SimsZone) { LoreLines(page, "Adventurers Met", "Those you have hailed on the road. They go where they please.", null, SimsMet(book), 0, "Nobody yet. Hail an adventurer (H) and they are written here."); return; }
            var sel = zones.Find(z => z.id == loreZone) ?? zones[0];
            var all = book.Lines(sel, loreChapter); var known = all.FindAll(l => l.Shown);
            string intro = loreChapter == LoreBook.Places ? sel.canonNote : null;
            string empty = loreChapter == LoreBook.Places ? "Nowhere here is known to you yet. Walk the zone: every place you reach is written in." :
                loreChapter == LoreBook.People ? "Nobody here has spoken with you yet. Talk to people (E) and they are written in." :
                loreChapter == LoreBook.Creatures ? "Nothing here has fallen to you yet. Each kind you kill is written in." :
                "Nothing here has been worked yet. Each seam, windfall and herb you gather is written in.";
            LoreLines(page, LoreBook.ChapterName(loreChapter) + " of " + sel.displayName, null, intro, known, all.Count - known.Count, empty);
        }

        /// <summary>The adventurers you have hailed, from the world's roster.</summary>
        List<LoreBook.Line> SimsMet(LoreBook book)
        {
            var list = new List<LoreBook.Line>(); var world = SimPopulation.Active != null ? SimPopulation.Active.World : null;
            if (world == null || world.sims == null) return list;
            foreach (var s in world.sims)
            {
                if (s == null || !book.Knows(LoreBook.SimKey(s.id))) continue;
                var home = session.Zone != null ? session.Zone.FindZone(s.homeZone) : null;
                list.Add(new LoreBook.Line { chapter = LoreBook.People, subject = s.id, title = s.name, known = true, canonStatus = "GAME-ONLY: the world's adventurers",
                    plain = "A " + Races.Name(s.Race).ToLowerInvariant() + " " + SimRoster.ClassName(s.classId).ToLowerInvariant() + " of level " + s.level + (home != null ? ", from " + home.displayName : "") +
                        (string.IsNullOrEmpty(s.guild) ? "." : ", of the guild " + s.guild + ".") + (s.friend ? " A friend." : "") });
            }
            return list;
        }

        /// <summary>
        /// A chapter's page on parchment: its title, a line under it, a zone's canon note, then each known line (its title, what is known,
        /// its notes, its better half or where that lies, its lore status) in a scroll view, and how many are still to find.
        /// </summary>
        void LoreLines(Rect page, string title, string sub, string intro, List<LoreBook.Line> lines, int unknown, string empty = null)
        {
            Fill(page, Parchment);
            float inner = page.width - 40, py = page.y + 16;
            Ink(new Rect(page.x + 20, py, inner, 32), title, qTitle, new Color(.42f, .22f, .05f)); py += 34;
            if (!string.IsNullOrEmpty(sub)) { Ink(new Rect(page.x + 20, py, inner, 22), sub, qSmall, new Color(.4f, .32f, .22f)); py += 26; }
            else
            {
                int full = 0; foreach (var l in lines) if (l.Full) full++;
                Ink(new Rect(page.x + 20, py, inner, 22), lines.Count + " of " + (lines.Count + unknown) + " known" + (full < lines.Count ? "  ·  " + full + " told in full" : ""), qSmall, new Color(.4f, .32f, .22f)); py += 26;
            }
            var view = new Rect(page.x + 20, py, page.width - 30, page.yMax - 16 - py); float width = view.width - 24;
            float H(string s, GUIStyle st, float w) { if (string.IsNullOrEmpty(s)) return 0; measureContent.text = s; return st.CalcHeight(measureContent, w); }
            string rest = unknown == 1 ? "One more is still to find." : unknown > 1 ? unknown + " more are still to find." : "";
            // Measure first, then draw in the scroll view.
            float height = 0;
            if (!string.IsNullOrEmpty(intro)) height += H(intro, qSmall, width) + 14;
            if (lines.Count == 0 && !string.IsNullOrEmpty(empty)) height += H(empty, qBody, width) + 10;
            foreach (var l in lines) height += LineHeight(l, width, H);
            if (rest.Length > 0) height += H(rest, qSmall, width) + 8;
            loreScroll = GUI.BeginScrollView(view, loreScroll, new Rect(0, 0, width, height));
            float yy = 0;
            if (!string.IsNullOrEmpty(intro)) { float h = H(intro, qSmall, width); Ink(new Rect(0, yy, width, h), intro, qSmall, new Color(.36f, .3f, .22f)); yy += h + 14; }
            if (lines.Count == 0 && !string.IsNullOrEmpty(empty)) { float h = H(empty, qBody, width); Ink(new Rect(0, yy, width, h), empty, qBody, InkBrown); yy += h + 10; }
            foreach (var l in lines)
            {
                Ink(new Rect(0, yy, width, 24), l.title, qHead, new Color(.42f, .22f, .05f)); yy += 24;
                string plain = l.known ? l.plain : "(Not yet seen for yourself.)";
                float h = H(plain, qBody, width); if (h > 0) { Ink(new Rect(0, yy, width, h), plain, qBody, InkBrown); yy += h + 2; }
                foreach (var n in l.notes) { h = H(n, qSmall, width - 10); Ink(new Rect(10, yy, width - 10, h), n, qSmall, new Color(.3f, .24f, .16f)); yy += h + 2; }
                if (l.hasMore)
                {
                    string more = l.moreKnown ? l.more : l.moreHint;
                    h = H(more, qSmall, width); Ink(new Rect(0, yy + 2, width, h), more, qSmall, l.moreKnown ? new Color(.24f, .18f, .4f) : new Color(.5f, .44f, .36f)); yy += h + 4;
                }
                if (!string.IsNullOrEmpty(l.canonStatus)) { h = H("Lore status: " + l.canonStatus, qSmall, width); Ink(new Rect(0, yy, width, h), "Lore status: " + l.canonStatus, qSmall, new Color(.45f, .38f, .28f)); yy += h; }
                yy += 12;
            }
            if (rest.Length > 0) { float h = H(rest, qSmall, width); Ink(new Rect(0, yy, width, h), rest, qSmall, new Color(.35f, .27f, .18f)); }
            GUI.EndScrollView();
        }
        /// <summary>A line's height on the page, as LoreLines draws it.</summary>
        float LineHeight(LoreBook.Line l, float width, System.Func<string, GUIStyle, float, float> H)
        {
            float h = 24 + 12;
            float p = H(l.known ? l.plain : "(Not yet seen for yourself.)", qBody, width); if (p > 0) h += p + 2;
            foreach (var n in l.notes) h += H(n, qSmall, width - 10) + 2;
            if (l.hasMore) h += H(l.moreKnown ? l.more : l.moreHint, qSmall, width) + 4;
            if (!string.IsNullOrEmpty(l.canonStatus)) h += H("Lore status: " + l.canonStatus, qSmall, width);
            return h;
        }

        /// <summary>
        /// The Pages chapter (the book of 2026-10-15): the pages of kind "lore" found at the zones' hidden finds (EncounterSession.LorePages),
        /// the found ones to read and, zone by zone, how many more lie unread; then every other page found (the Chronicle's letters and
        /// ledgers), so one place holds all there is to read.
        /// </summary>
        void BookLorePages(QuestLog log, Rect list, Rect page)
        {
            var pages = session.LorePages(); int found = 0; foreach (var p in pages) if (p.found) found++;
            float y = list.y;
            Shadow(new Rect(list.x + 10, y, list.width, 20), "PAGES OF LORE  (" + found + " / " + pages.Count + ")", qHead, gold); y += 24;
            foreach (var p in pages)
            {
                if (!p.found || y > list.yMax - 40) continue;
                GUI.enabled = session.ReadingDocument != p.doc.id;
                if (GUI.Button(new Rect(list.x + 8, y, list.width - 16, 30), p.doc.title, qList)) { session.ReadingDocument = p.doc.id; loreReading = p.doc.id; textScroll = Vector2.zero; }
                GUI.enabled = true; y += 34;
            }
            if (found == 0) { Shadow(new Rect(list.x + 10, y, list.width - 20, 40), "Pages of the old lore lie at the zones' hidden places: shelves, cairns, the dead. Find them and they are kept here.", tiny, new Color(.8f, .8f, .8f)); y += 44; }
            // Unread pages, by zone, in the pages' order (this zone first, then by level).
            var zones = new List<string>(); var unread = new Dictionary<string, int>();
            foreach (var p in pages) { if (p.found) continue; string z = string.IsNullOrEmpty(p.zoneName) ? "elsewhere" : p.zoneName; if (!unread.ContainsKey(z)) { zones.Add(z); unread[z] = 0; } unread[z]++; }
            foreach (var z in zones) { if (y > list.yMax - 60) break; Shadow(new Rect(list.x + 16, y, list.width - 24, 20), unread[z] + (unread[z] == 1 ? " page lies unread " : " pages lie unread ") + (z == "elsewhere" ? "elsewhere" : "in " + z) + ".", tiny, new Color(.72f, .7f, .65f)); y += 20; }
            int others = 0; foreach (var id in log.Progress.documents) if (log.Db.Documents.TryGetValue(id, out var d) && !d.IsLore) others++;
            y += 8; if (y < list.yMax - 40) { Shadow(new Rect(list.x + 10, y, list.width, 20), "LETTERS AND LEDGERS  (" + others + ")", qHead, new Color(.7f, .7f, .7f)); y += 24; }
            foreach (var id in log.Progress.documents)
            {
                if (y > list.yMax - 34 || !log.Db.Documents.TryGetValue(id, out var doc) || doc.IsLore) continue;
                GUI.enabled = session.ReadingDocument != id;
                if (GUI.Button(new Rect(list.x + 8, y, list.width - 16, 28), doc.title, qList)) { session.ReadingDocument = id; loreReading = id; textScroll = Vector2.zero; }
                GUI.enabled = true; y += 32;
            }
            ReadPage(log, page, found + others == 0 ? "What the world remembers, as you find it written: the old lore at the hidden places, and the letters, ledgers and songs." : "Choose a page to read.");
        }
    }
}
