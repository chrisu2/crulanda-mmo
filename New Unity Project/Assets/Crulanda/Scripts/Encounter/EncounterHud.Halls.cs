using System.Collections.Generic;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The capital's hall windows (CAPITAL_HUB.md step 4; EncounterSession.Halls), on parchment like a conversation: Aethelgard's audience
    /// (his line, its lore status, the topics to put to him, your standing with the Council), the Archive's reading room (a shelf a zone:
    /// the pages found there to read, how many are still lost, a clue, and the copying trade), a guild's hall (motto, roster, notice
    /// board, Join), a class trainer (the paid reset, the abilities by level) and a trade's trainer (take up, put aside, buy the tool).
    /// </summary>
    public sealed partial class EncounterHud
    {
        static readonly Rect HallRect = new Rect(330, 100, 780, 640);
        static bool hallVisible;
        static bool HallUiBlocks(Vector2 p) { return hallVisible && HallRect.Contains(p); }
        Vector2 hallScroll, hallListScroll;
        const string AethelgardLook = "Ancient, older than he looks: pale skin pulled tight against his skull, robes of white silk worked with patterns that hurt the eye. His voice is dry, like dead leaves on cold stone.";
        const string AethelgardLookLabel = "CANON (book1 ch.12:365-393)";

        void DrawHall()
        {
            session.TendHall();
            var h = session.Hall; if (h == null) return;
            QuestStyles();
            var w = HallRect;
            Fill(new Rect(w.x - 4, w.y - 4, w.width + 8, w.height + 8), new Color(.25f, .17f, .08f, .98f));
            Fill(w, Parchment);
            string title = h.npc;
            string sub = h.anywhere ? null : HallRoles.Title(h.kind + (string.IsNullOrEmpty(h.arg) ? "" : ":" + h.arg));
            var v = h.anywhere ? null : VillageLife.Active?.Find(h.npc); if (v != null && !string.IsNullOrEmpty(v.Title)) sub = v.Title;
            Ink(new Rect(w.x + 24, w.y + 14, w.width - 48, 30), title, qTitle, InkBrown);
            if (!string.IsNullOrEmpty(sub)) Ink(new Rect(w.x + 24, w.y + 42, w.width - 48, 20), sub, qSmall, new Color(.4f, .32f, .22f));
            var body = new Rect(w.x + 24, w.y + 68, w.width - 48, w.height - 68 - 60);
            switch (h.kind)
            {
                case HallRoles.Councilor: HallAudience(h, body); break;
                case HallRoles.Archivist: HallArchive(h, body); break;
                case HallRoles.Guildmaster: HallGuild(h, body); break;
                case HallRoles.Trainer: HallTrainer(h, body); break;
                case HallRoles.ProfessionTrainer: HallTrade(h, body); break;
            }
            if (session.Hall != null && GUI.Button(new Rect(w.xMax - 154, w.yMax - 52, 130, 36), h.kind == HallRoles.Councilor ? "Step back" : "Goodbye", button)) session.Hall = null;
        }
        float InkWrapped(Rect r, string s, GUIStyle style, Color c)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            measureContent.text = s; float hh = style.CalcHeight(measureContent, r.width);
            Ink(new Rect(r.x, r.y, r.width, hh), s, style, c); return hh;
        }

        // ---------- Aethelgard ----------
        void HallAudience(EncounterSession.HallWindow h, Rect r)
        {
            float y = r.y;
            y += InkWrapped(new Rect(r.x, y, r.width, 0), AethelgardLook, qSmall, new Color(.36f, .3f, .22f)) + 2;
            y += InkWrapped(new Rect(r.x, y, r.width, 0), "Lore status: " + AethelgardLookLabel, qSmall, new Color(.45f, .38f, .28f)) + 14;
            y += InkWrapped(new Rect(r.x, y, r.width, 0), "\"" + h.line.text + "\"", qBody, InkBrown) + 4;
            y += InkWrapped(new Rect(r.x, y, r.width, 0), "Lore status: " + h.line.label, qSmall, new Color(.45f, .38f, .28f)) + 18;
            var topics = CouncilAudience.Topics; float bw = (r.width - 12) / 2;
            for (int i = 0; i < topics.Length; i++)
            {
                var b = new Rect(r.x + (i % 2) * (bw + 12), y + (i / 2) * 42, bw, 36);
                GUI.enabled = h.topic != topics[i].topic || topics[i].topic == CouncilAudience.Errands;
                if (GUI.Button(b, topics[i].ask, qList)) { session.AudienceAsk(topics[i].topic); GUI.enabled = true; return; }
                GUI.enabled = true;
            }
            y += ((topics.Length + 1) / 2) * 42 + 10;
            var log = session.Quests;
            if (log != null && log.Db.Factions.TryGetValue(CouncilAudience.Faction, out var f))
            {
                int st = log.Standing(CouncilAudience.Faction);
                Ink(new Rect(r.x, y, r.width, 22), "Your standing with " + f.name + ": " + QuestLog.TierNames[QuestLog.Tier(st)] + " (" + st + ")", qSmall, new Color(.3f, .24f, .16f));
            }
        }

        // ---------- the Archive ----------
        void HallArchive(EncounterSession.HallWindow h, Rect r)
        {
            var shelves = session.ArchiveShelves();
            if (shelves.Count == 0) { Ink(new Rect(r.x, r.y, r.width, 40), "The shelves are empty: this build has no pages.", qBody, InkBrown); return; }
            if (h.shelf == null || !shelves.Exists(s => s.zoneId == h.shelf)) h.shelf = shelves[0].zoneId;
            // Left: the shelves, a zone each, with found / all.
            var list = new Rect(r.x, r.y, 230, r.height);
            float lh = shelves.Count * 40;
            hallListScroll = GUI.BeginScrollView(list, hallListScroll, new Rect(0, 0, list.width - 20, lh));
            for (int i = 0; i < shelves.Count; i++)
            {
                var s = shelves[i]; int all = s.pages.Count + s.pagesLost;
                GUI.enabled = h.shelf != s.zoneId;
                if (GUI.Button(new Rect(0, i * 40, list.width - 22, 36), s.zoneName + "   " + s.pages.Count + "/" + all, qList)) { h.shelf = s.zoneId; h.reading = null; hallScroll = Vector2.zero; }
                GUI.enabled = true;
            }
            GUI.EndScrollView();
            // Right: the shelf, or the page being read.
            var shelf = shelves.Find(s => s.zoneId == h.shelf);
            var page = new Rect(r.x + 244, r.y, r.width - 244, r.height - 70);
            var view = new Rect(page.x, page.y, page.width, page.height); float width = view.width - 22;
            DocumentDef reading = null; if (h.reading != null && session.Quests != null) session.Quests.Db.Documents.TryGetValue(h.reading, out reading);
            var lines = new List<(string text, GUIStyle style, Color colour, string doc)>();
            if (reading != null)
            {
                lines.Add((reading.title, qHead, new Color(.42f, .22f, .05f), null));
                lines.Add((reading.text, qBody, InkBrown, null));
                if (!string.IsNullOrEmpty(reading.canonStatus)) lines.Add(("Lore status: " + reading.canonStatus, qSmall, new Color(.45f, .38f, .28f), null));
            }
            else
            {
                lines.Add((shelf.zoneName, qHead, new Color(.42f, .22f, .05f), null));
                lines.Add((shelf.pages.Count == 0 ? "No page of this shelf is yours yet." : "Pages found here (choose one to read):", qSmall, new Color(.4f, .32f, .22f), null));
                foreach (var d in shelf.pages) lines.Add((d.title + (d.IsLore ? "" : "  (Chronicle)"), qList, InkBrown, d.id));
                lines.Add(((shelf.pagesLost == 1 ? "One page is" : shelf.pagesLost + " pages are") + " still lost" + (shelf.zoneId == ArchiveRoom.Elsewhere ? "" : " here") +
                    (shelf.zoneId == ArchiveRoom.Elsewhere ? "." : ", and " + (shelf.findsHidden == 1 ? "one hidden find" : shelf.findsHidden + " hidden finds") + " not yet found."), qSmall, new Color(.35f, .27f, .18f), null));
                if (!string.IsNullOrEmpty(shelf.clue)) lines.Add(("A clue: " + shelf.clue, qSmall, new Color(.24f, .18f, .4f), null));
                foreach (var t in shelf.told) lines.Add(("The archivist's word: " + t, qSmall, new Color(.24f, .18f, .4f), null));
            }
            float H(string s, GUIStyle st) { if (string.IsNullOrEmpty(s)) return 0; if (st == qList) return 32; measureContent.text = s; return st.CalcHeight(measureContent, width); }
            float total = 0; foreach (var l in lines) total += H(l.text, l.style) + 8;
            hallScroll = GUI.BeginScrollView(view, hallScroll, new Rect(0, 0, width, total));
            float yy = 0;
            foreach (var l in lines)
            {
                float hh = H(l.text, l.style);
                if (l.doc != null) { if (GUI.Button(new Rect(0, yy, width, 28), l.text, qList)) { h.reading = l.doc; hallScroll = Vector2.zero; } }
                else Ink(new Rect(0, yy, width, hh), l.text, l.style, l.colour);
                yy += hh + 8;
            }
            GUI.EndScrollView();
            // Under the page: back to the shelf, the copying trade, and the archivist's last word.
            float by = page.yMax + 6;
            if (reading != null)
            {
                if (GUI.Button(new Rect(page.x, by, 120, 30), "Back", micro)) { h.reading = null; hallScroll = Vector2.zero; }
                GUI.enabled = ArchiveRoom.CanCopy(session.Progress, reading.id);
                if (GUI.Button(new Rect(page.x + 130, by, 300, 30), GUI.enabled ? "Lend this page to be copied" : "Copied already", micro)) session.ArchiveCopy(reading.id);
                GUI.enabled = true;
            }
            if (!string.IsNullOrEmpty(h.said)) Ink(new Rect(page.x, by + 34, page.width, 40), h.said, qSmall, new Color(.24f, .18f, .4f));
        }

        // ---------- a guild's hall ----------
        void HallGuild(EncounterSession.HallWindow h, Rect r)
        {
            var guild = GuildHalls.FromSlug(h.arg); if (guild == null) return;
            var world = SimPopulation.Active != null ? SimPopulation.Active.World : null;
            var roster = GuildHalls.Roster(world, guild, WorldClock.Hour, session.InParty);
            bool member = session.Guild == guild; int online = roster.FindAll(m => m.online).Count;
            float y = r.y;
            Ink(new Rect(r.x, y, r.width, 26), "<" + guild + ">", qHead, new Color(.42f, .22f, .05f)); y += 24;
            y += InkWrapped(new Rect(r.x, y, r.width, 0), "\"" + char.ToUpperInvariant(SimGuilds.Motto(guild)[0]) + SimGuilds.Motto(guild).Substring(1) + ".\"  " + (roster.Count + (member ? 1 : 0)) + " members, " + online + " about now.", qSmall, new Color(.36f, .3f, .22f)) + 10;
            // The roster.
            var view = new Rect(r.x, y, r.width, r.yMax - y - 150); float width = view.width - 22;
            hallScroll = GUI.BeginScrollView(view, hallScroll, new Rect(0, 0, width, Mathf.Max(1, roster.Count) * 24));
            float yy = 0;
            foreach (var m in roster)
            {
                var s = m.sim; var c = m.online ? InkBrown : new Color(.5f, .44f, .36f);
                Ink(new Rect(0, yy, 230, 22), s.name + (m.master ? "  (guild master)" : ""), qSmall, c);
                Ink(new Rect(240, yy, 120, 22), SimRoster.ClassName(s.classId), qSmall, c);
                Ink(new Rect(370, yy, 60, 22), s.level.ToString(), qSmall, c);
                Ink(new Rect(430, yy, width - 430, 22), m.online ? session.ZoneName(s.zone) : "offline", qSmall, c);
                yy += 24;
            }
            if (roster.Count == 0) Ink(new Rect(0, 0, width, 22), "No adventurer of the world is of this guild yet.", qSmall, InkBrown);
            GUI.EndScrollView();
            y = view.yMax + 8;
            // The notice board's asks, and what you can do.
            var asks = GuildHalls.Asks(session.Quests != null ? session.Quests.Db : null, guild);
            string board = asks.Count == 0 ? "The notice board is bare." : "On the notice board: " + string.Join("; ", asks.ConvertAll(q => q.title + " (level " + q.level + ")")) + ".";
            y += InkWrapped(new Rect(r.x, y, r.width, 0), board, qSmall, new Color(.3f, .24f, .16f)) + 8;
            if (member) { if (GUI.Button(new Rect(r.x, y, 260, 34), "Read the notice board", button)) session.OpenGuildBoard(guild); }
            else if (!session.InGuild) { if (GUI.Button(new Rect(r.x, y, 260, 34), "Join " + guild, button)) session.JoinHallGuild(guild); }
            else Ink(new Rect(r.x, y, r.width, 40), "You are of <" + session.Guild + ">. This hall's board is for its own.", qSmall, InkBrown);
        }

        // ---------- a class trainer ----------
        void HallTrainer(EncounterSession.HallWindow h, Rect r)
        {
            string cls = session.TrainerClass; bool yours = session.ClassDef != null && cls == session.ClassDef.id;
            float y = r.y;
            Ink(new Rect(r.x, y, r.width, 26), SimRoster.ClassName(cls) + " training", qHead, new Color(.42f, .22f, .05f)); y += 28;
            if (yours)
            {
                int spent = TalentTree.Spent(session.Progress), price = TrainerRules.ResetPrice(session.Progress);
                y += InkWrapped(new Rect(r.x, y, r.width, 0), "\"Unlearn it all and start again? Every point back. It costs more each time: the body remembers, and it has to be made to forget.\"  You have " + spent + " talent points spent and " + session.Progress.gold + " crowns.", qBody, InkBrown) + 8;
                GUI.enabled = spent > 0 && session.Progress.gold >= price && session.CanEditBuild;
                if (GUI.Button(new Rect(r.x, y, 320, 34), "Reset talents (" + price + " crowns)", button)) session.PaidTalentReset();
                GUI.enabled = true; y += 44;
            }
            else { y += InkWrapped(new Rect(r.x, y, r.width, 0), "\"I train " + SimRoster.ClassName(cls) + "s. Your own kind's trainer will see to you.\"", qBody, InkBrown) + 10; }
            Ink(new Rect(r.x, y, r.width, 22), "ABILITIES BY LEVEL", qHead, new Color(.42f, .22f, .05f)); y += 24;
            var list = session.TrainerAbilities(cls);
            var view = new Rect(r.x, y, r.width, r.yMax - y); float width = view.width - 22;
            hallScroll = GUI.BeginScrollView(view, hallScroll, new Rect(0, 0, width, Mathf.Max(1, list.Count) * 24));
            float yy = 0; int lvl = session.Progress.Level;
            foreach (var (level, name, talent) in list)
            {
                var c = !yours || level <= lvl ? InkBrown : new Color(.5f, .44f, .36f);
                Ink(new Rect(0, yy, 80, 22), "Level " + level, qSmall, c);
                Ink(new Rect(90, yy, width - 90, 22), name + (talent != null ? "  (a talent's: " + (session.Talents?.Find(talent)?.name ?? talent) + ")" : "") + (yours && level > lvl ? "  (to come)" : ""), qSmall, c);
                yy += 24;
            }
            GUI.EndScrollView();
        }

        // ---------- a trade's trainer ----------
        void HallTrade(EncounterSession.HallWindow h, Rect r)
        {
            var log = session.Professions; var d = log != null ? log.Db.Profession(h.arg) : null; if (d == null) return;
            float y = r.y;
            Ink(new Rect(r.x, y, r.width, 26), d.name, qHead, new Color(.42f, .22f, .05f)); y += 28;
            y += InkWrapped(new Rect(r.x, y, r.width, 0), d.description, qBody, InkBrown) + 8;
            if (!string.IsNullOrEmpty(d.canonStatus)) y += InkWrapped(new Rect(r.x, y, r.width, 0), "Lore status: " + d.canonStatus, qSmall, new Color(.45f, .38f, .28f)) + 10;
            bool has = log.Has(d.id);
            Ink(new Rect(r.x, y, r.width, 22), has ? "Your skill: " + log.Skill(d.id) + " / " + ProfessionDatabase.MaxSkill : "Not yours yet.", qHead, InkBrown); y += 32;
            if (d.kind == "craft")
            {
                if (!has)
                {
                    bool can = log.CanLearn(d.id, out var why);
                    GUI.enabled = can; if (GUI.Button(new Rect(r.x, y, 260, 34), "Take up " + d.name, button)) session.LearnCraft(d.id); GUI.enabled = true;
                    if (!can && why != null) Ink(new Rect(r.x + 270, y + 6, r.width - 270, 40), why, qSmall, InkBrown);
                }
                else if (!h.confirm) { if (GUI.Button(new Rect(r.x, y, 260, 34), "Put " + d.name + " aside...", button)) h.confirm = true; }
                else
                {
                    Ink(new Rect(r.x, y - 2, r.width, 22), "Forget " + d.name + "? Skill " + log.Skill(d.id) + " is lost.", qSmall, InkBrown);
                    if (GUI.Button(new Rect(r.x, y + 22, 200, 32), "Yes, forget it", button)) { session.ForgetCraft(d.id); h.confirm = false; }
                    if (GUI.Button(new Rect(r.x + 210, y + 22, 120, 32), "No", button)) h.confirm = false;
                }
            }
            else if (!string.IsNullOrEmpty(d.tool) && !Inventory.Owns(session.Progress, d.tool) && !Inventory.Has(session.Progress, d.tool))
            {
                var tool = session.Items != null ? session.Items.Get(d.tool) : null;
                string label = "Buy a " + session.ItemName(d.tool) + (tool != null ? " (" + Charm.Ask(session.Progress, Inventory.Price(tool)) + " crowns)" : "");
                if (GUI.Button(new Rect(r.x, y, 360, 34), label, button)) session.BuyTradeTool(d.id);
            }
            else Ink(new Rect(r.x, y, r.width, 40), has ? "\"You have it already. Practice is all that is left to teach.\"" : "\"Everyone has a little of " + d.name + ". Work at it.\"", qSmall, InkBrown);
            if (GUI.Button(new Rect(r.x, r.yMax - 40, 240, 34), "Open the Trades book (K)", micro)) session.OpenTradePage(d.id);
        }
    }
}
