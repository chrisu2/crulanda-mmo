using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Quest interface: tracker (under the minimap), ! and ? over quest givers, the parchment conversation window, and the
    /// quest book (L) with Quests, Chronicle, Standing, Discoveries and Armoury tabs (the last two in EncounterHud.Discoveries.cs and
    /// EncounterHud.Armoury.cs).
    /// </summary>
    public sealed partial class EncounterHud
    {
        static bool bookVisible, talkVisible;
        static readonly Rect TalkRect = new Rect(470, 120, 500, 600), BookRect = new Rect(200, 90, 1040, 680);
        static bool QuestUiBlocks(Vector2 p) { return (talkVisible && TalkRect.Contains(p)) || (bookVisible && BookRect.Contains(p)); }
        static readonly Color Parchment = new Color(.9f, .84f, .7f, .98f), InkBrown = new Color(.2f, .13f, .07f), QuestGold = new Color(1, .82f, .15f);
        GUIStyle qTitle, qBody, qSmall, qHead, qMark, qList;
        /// <summary>Open quest book tab: quests, chronicle, standing, discoveries or armoury (capture tools set it).</summary>
        public static string BookTab = "quests";
        string bookQuest, lastReading;
        Vector2 bookScroll, textScroll;

        void QuestStyles()
        {
            if (qTitle != null) return;
            qTitle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
            qBody = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, richText = true };
            qSmall = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, richText = true };
            qHead = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            qMark = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            qList = new GUIStyle(GUI.skin.button) { fontSize = 14, alignment = TextAnchor.MiddleLeft, wordWrap = true, padding = new RectOffset(10, 6, 4, 4) };
        }
        void Ink(Rect r, string s, GUIStyle style, Color c) { GUI.contentColor = c; GUI.Label(r, s, style); GUI.contentColor = Color.white; }
        static string KindLabel(QuestDef q) { return q.kind == "main" ? "Chronicle" : q.kind == "npc" ? "Village work" : q.kind == "faction" ? "Faction" : q.kind == "bounty" ? (q.rare ? "Rare posting" : q.poster == "sandthrone" ? "Sandthrone contract" : "Bounty") : "Side quest"; }

        /// <summary>The ! or ? over someone's head, or ' ' (no quests here, or too far away).</summary>
        // Each person's marker is worked out again every 0.4 s, not every frame (playtest note 23: the quest log was asked for every
        // villager in sight, every frame).
        readonly System.Collections.Generic.Dictionary<string, (float until, char m, bool grey)> markers = new System.Collections.Generic.Dictionary<string, (float, char, bool)>();
        char HeadMarker(string npc, float distance, out bool grey)
        {
            grey = false;
            if (session.Quests == null || session.Zone == null || distance > 45 || npc == null) return ' ';
            if (markers.TryGetValue(npc, out var k) && Time.unscaledTime < k.until) { grey = k.grey; return k.m; }
            char m = session.Quests.Marker(npc, session.ZoneId, session.Progress.Level, out grey);
            if (markers.Count > 500) markers.Clear();
            markers[npc] = (Time.unscaledTime + .4f, m, grey); return m;
        }
        /// <summary>Draws a head marker centred on <paramref name="at"/> (the nameplate pass places it above the name).</summary>
        void DrawHeadMarker(char m, bool grey, Vector2 at)
        {
            QuestStyles(); string s = m == '!' ? "!" : "?"; var r = new Rect(at.x - 30, at.y - 24, 60, 48);
            GUI.contentColor = new Color(0, 0, 0, .8f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, qMark);
            GUI.contentColor = grey ? new Color(.72f, .72f, .72f) : QuestGold; GUI.Label(r, s, qMark); GUI.contentColor = Color.white;
        }

        /// <summary>Target frame for a clicked villager or Mira: green ring, name, trade, and what E will do.</summary>
        void DrawFriendFrame()
        {
            var sim = session.FocusSim;
            if (sim != null && session.FocusVillager == null && !session.FocusMira) { DrawSimFrame(sim); return; }
            var v = session.FocusVillager; string name = session.FocusName; if (name == null) return;
            string title = session.FocusMira ? "Healer" : v.Title;
            float dist = Vector3.Distance(session.Player.transform.position, session.FocusPosition);
            Fill(new Rect(372, 22, 290, 64), new Color(0, 0, 0, .55f));
            Portrait(new Vector2(668, 54), 74, new Color(.35f, .8f, .4f), name.Substring(0, 1), null);
            Shadow(new Rect(380, 24, 240, 20), name, frameName, new Color(.55f, 1, .55f));
            Shadow(new Rect(380, 46, 260, 20), title != null ? "<" + title + ">" : session.ZoneTitle + " villager", tiny, new Color(1, .84f, .45f));
            char m = session.Quests != null && session.Zone != null ? session.Quests.Marker(name, session.ZoneId, session.Progress.Level, out _) : ' ';
            string hint = !session.FocusInTalkReach ? "Friendly  ·  " + (dist >= EncounterSession.TalkRange ? Mathf.RoundToInt(dist) + " m away" : "out of earshot") :
                "Friendly  ·  [" + KeyBindings.InteractLabel + "] " + (m == '!' ? "has work for you" : m == '?' ? "wants to hear from you" : "talk");
            Shadow(new Rect(380, 66, 280, 20), hint, tiny, Color.white);
            if (m != ' ') { QuestStyles(); Ink(new Rect(700, 18, 30, 44), m.ToString(), qMark, QuestGold); }
        }

        /// <summary>A sim you clicked (Phase 5.2b): name in the class's colour, class, level and folk, and Invite (or Leave party).</summary>
        void DrawSimFrame(SimAdventurer s)
        {
            var col = ClassColour(s.classId); bool member = session.InParty(s.id);
            Fill(new Rect(372, 22, 290, 70), new Color(0, 0, 0, .55f));
            Portrait(new Vector2(668, 54), 74, col, s.name.Substring(0, 1), s.level.ToString());
            Shadow(new Rect(380, 24, 240, 20), s.name, frameName, col);
            Shadow(new Rect(380, 46, 260, 20), "<" + SimRoster.ClassName(s.classId) + " " + s.level + " · " + s.folk + ">", tiny, new Color(1, .84f, .45f));
            if (member) { if (GUI.Button(new Rect(380, 63, 120, 26), "Leave party", slim)) session.LeaveParty(s.id); }
            else
            {
                if (GUI.Button(new Rect(380, 63, 90, 26), "Invite", slim)) session.Invite(s.id);
                if (GUI.Button(new Rect(476, 63, 80, 26), "Whisper", slim)) BeginWhisper(s.name);   // 5.5
                var st = SimMemory.Of(s);
                Shadow(new Rect(562, 66, 100, 20), s.friend ? "Friend" : st == SimMemory.Standing.Stranger ? "Adventurer" : char.ToUpper(SimMemory.Label(st)[0]) + SimMemory.Label(st).Substring(1), tiny, st == SimMemory.Standing.Rival ? new Color(1, .5f, .45f) : s.friend || st == SimMemory.Standing.Friend ? new Color(.55f, 1, .7f) : Color.white);
            }
        }

        // ---------- tracker ----------
        float trackerX, trackerW, trackerH;
        /// <summary>With nothing tracked, the zone name and hint show this long after arriving (or after the last tracked quest), then fade over 3 s.</summary>
        const float HintSeconds = 14; float hintSince; string hintZone; bool hintIdle;
        /// <summary>Paints the tracker's ink plate from the previous pass's text extent (IMGUI paints in call order, so the plate goes
        /// first), then starts measuring this pass's lines.</summary>
        void TrackerBegin(Rect r, float alpha)
        {
            if (trackerW > 0 && trackerH > 0) HudBacking(new Rect(r.x - 8, r.y - 4, trackerW + 16, trackerH + 8), alpha);
            trackerX = r.x; trackerW = 0;
        }
        /// <summary>One outlined tracker line; widens the plate to fit it (a wrapped line takes the full width).</summary>
        void TrackLine(Rect r, string s, GUIStyle style, Color c)
        {
            Outlined(r, s, style, c);
            measureContent.text = s; trackerW = Mathf.Max(trackerW, Mathf.Min(r.xMax, r.x + style.CalcSize(measureContent).x) - trackerX);
        }
        void DrawQuests()
        {
            QuestStyles();
            var r = new Rect(1120, 240, 310, 260); var log = session.Quests;
            // Nothing tracked: the zone name and the hint show for a while after arriving (or once the last tracked quest is
            // done), then fade over 3 s, so they don't sit over the scene for good. The minimap names the zone anyway.
            if (hintZone != session.ZoneId) { hintZone = session.ZoneId; hintSince = Time.time; }
            float fade = hintIdle ? Mathf.Clamp01(1 - (Time.time - hintSince - HintSeconds) / 3) : 1;
            if (fade <= 0) { trackerArea = default; trackerW = 0; return; }
            TrackerBegin(r, fade);
            TrackLine(new Rect(r.x, r.y, r.width, 20), session.ZoneTitle, frameName, new Color(gold.r, gold.g, gold.b, fade));
            float y = r.y + 24; int shown = 0;
            // Chronicle first, then the rest in the order taken.
            var active = new List<(QuestDef quest, QuestState state)>(log.Active());
            active.Sort((a, b) => (a.quest.IsMain ? 0 : 1).CompareTo(b.quest.IsMain ? 0 : 1));
            foreach (var (q, s) in active)
            {
                if (!s.tracked || shown >= 5 || y > 470) continue;
                shown++;
                TrackLine(new Rect(r.x, y, r.width, 20), (q.IsMain ? "◆ " : "") + q.title, tiny, q.IsMain ? new Color(1, .86f, .5f) : new Color(1, .82f, .2f)); y += 19;
                var step = log.CurrentStep(s);
                // Breadcrumb: the next step waits in another zone; say which road leads there (it is ringed on the maps).
                var elsewhere = session.QuestZoneElsewhere(q, s);
                if (elsewhere != null)
                {
                    var exit = session.ExitToward(elsewhere);
                    string road = "→ Travel to " + session.ZoneName(elsewhere) + (exit != null && exit.to != elsewhere ? " (via " + session.ZoneName(exit.to) + ")" : "");
                    TrackLine(new Rect(r.x + 12, y, r.width - 12, 20), road, tiny, new Color(1, .85f, .4f)); y += 20;
                }
                if (step == null) { TrackLine(new Rect(r.x + 12, y, r.width - 12, 20), "Return to " + q.turnIn, tiny, new Color(.6f, 1, .6f)); y += 20; continue; }
                for (int i = 0; i < step.objectives.Length; i++)
                {
                    bool done = log.ObjectiveDone(s, i);
                    var line = (done ? "✓ " : "– ") + log.ObjectiveLine(s, i);
                    measureContent.text = line; float h = tiny.CalcHeight(measureContent, r.width - 12);
                    TrackLine(new Rect(r.x + 12, y, r.width - 12, h), line, tiny, done ? new Color(.6f, .6f, .6f) : new Color(1, .95f, .85f)); y += h + 1;
                }
                y += 4;
            }
            if (shown == 0)
            {
                if (!hintIdle) { hintIdle = true; hintSince = Time.time; }
                const string hint = "Look for gold ! over people's heads, or open your quests [L].";
                measureContent.text = hint; float hh = tiny.CalcHeight(measureContent, r.width);
                TrackLine(new Rect(r.x, y, r.width, hh), hint, tiny, new Color(.85f, .85f, .8f, fade)); y += hh + 2;
            }
            else hintIdle = false;
            trackerH = y - r.y;
            trackerArea = new Rect(r.x - 8, r.y - 4, Mathf.Max(trackerW, 120) + 16, y - r.y + 8);   // world labels keep out of it
            var story = session.StoryEnemies; int total = story.Count, dead = story.FindAll(e => !e.actor.IsAlive).Count;
            if (total > 0 && dead == total && GUI.Button(new Rect(r.x, y + 6, 250, 26), "Patrol returns · keep gear and XP", micro)) session.RepeatTrail();
        }

        // ---------- conversation ----------
        void DrawConversation()
        {
            QuestStyles();
            var c = session.Conversation; var log = session.Quests; if (c == null || log == null) return;
            var w = TalkRect;
            Fill(new Rect(w.x - 4, w.y - 4, w.width + 8, w.height + 8), new Color(.25f, .17f, .08f, .98f));
            Fill(w, Parchment);
            Ink(new Rect(w.x + 24, w.y + 16, w.width - 48, 30), c.npc, qTitle, InkBrown);
            float y = w.y + 56;
            if (c.selected == null)
            {
                bool board = c.npc == Bounties.BoardName;
                Ink(new Rect(w.x + 24, y, w.width - 48, 40), board ? (c.entries.Count == 0 ? "Rain-marks and old nails. Nothing to read today." : "Pinned to the board:") : "\"What can I do for you?\"", qBody, InkBrown); y += 44;
                foreach (var (q, st) in c.entries)
                {
                    string mark = st == QuestStatus.ReadyToTurnIn ? "?  " : "!  ";
                    if (GUI.Button(new Rect(w.x + 24, y, w.width - 48, 36), mark + q.title + "   (" + KindLabel(q) + ")", qList)) { c.selected = q; textScroll = Vector2.zero; }
                    y += 42;
                }
                var seller = VillageLife.Active?.Find(c.npc);
                if (session.IsVendor(seller) && GUI.Button(new Rect(w.x + 24, w.yMax - 52, 170, 36), "Browse wares", button)) session.OpenVendor(seller);
                if (GUI.Button(new Rect(w.xMax - 154, w.yMax - 52, 130, 36), board ? "Step away" : "Goodbye", button)) session.Conversation = null;
                return;
            }
            var quest = c.selected; var status = log.Status(quest, session.ZoneId);
            Ink(new Rect(w.x + 24, y, w.width - 48, 30), quest.title, qTitle, new Color(.42f, .22f, .05f)); y += 30;
            Ink(new Rect(w.x + 24, y, w.width - 48, 20), KindLabel(quest) + (quest.canonStatus != null ? "  ·  " + quest.canonStatus : ""), qSmall, new Color(.4f, .32f, .22f)); y += 26;
            string body = status == QuestStatus.ReadyToTurnIn ? quest.complete : quest.offer;
            var view = new Rect(w.x + 24, y, w.width - 48, w.yMax - 70 - y);
            float bodyH = qBody.CalcHeight(new GUIContent(body), view.width - 20);
            float extraH = 220;
            textScroll = GUI.BeginScrollView(view, textScroll, new Rect(0, 0, view.width - 20, bodyH + extraH));
            Ink(new Rect(0, 0, view.width - 20, bodyH), body, qBody, InkBrown);
            float yy = bodyH + 14;
            if (status == QuestStatus.Available)
            {
                Ink(new Rect(0, yy, 300, 22), "OBJECTIVES", qHead, new Color(.42f, .22f, .05f)); yy += 24;
                string summary = !string.IsNullOrEmpty(quest.summary) ? quest.summary : quest.steps[0].text;
                float sh = qSmall.CalcHeight(new GUIContent(summary), view.width - 20);
                Ink(new Rect(0, yy, view.width - 20, sh), summary, qSmall, InkBrown); yy += sh + 12;
            }
            Ink(new Rect(0, yy, 300, 22), "REWARDS", qHead, new Color(.42f, .22f, .05f)); yy += 24;
            Ink(new Rect(0, yy, view.width - 20, 80), RewardText(quest), qSmall, InkBrown);
            GUI.EndScrollView();
            if (status == QuestStatus.Available)
            {
                if (GUI.Button(new Rect(w.x + 24, w.yMax - 52, 150, 36), "Accept", button)) session.AcceptQuest(quest);
                if (GUI.Button(new Rect(w.xMax - 174, w.yMax - 52, 150, 36), "Decline", button)) { if (c.entries.Count > 1) c.selected = null; else session.Conversation = null; }
            }
            else if (status == QuestStatus.ReadyToTurnIn)
            {
                if (GUI.Button(new Rect(w.x + 24, w.yMax - 52, 190, 36), "Complete quest", button)) session.CompleteQuest(quest);
                if (GUI.Button(new Rect(w.xMax - 174, w.yMax - 52, 150, 36), c.entries.Count > 1 ? "Back" : "Later", button)) { if (c.entries.Count > 1) c.selected = null; else session.Conversation = null; }
            }
            else if (GUI.Button(new Rect(w.xMax - 174, w.yMax - 52, 150, 36), "Close", button)) session.Conversation = null;
        }
        string RewardText(QuestDef q)
        {
            var r = q.rewards ?? new QuestRewardDef(); var parts = new List<string>();
            if (r.xp > 0) parts.Add(r.xp + " experience");
            if (r.gold > 0) parts.Add(r.gold + " silver crowns");
            foreach (var i in r.items) parts.Add(session.Quests.Db.ItemName(i));
            foreach (var i in r.bagItems ?? new string[0]) parts.Add(session.ItemName(i) + (Inventory.Owns(session.Progress, i) ? " (you have one: its worth in crowns instead)" : ""));
            foreach (var d in r.documents) parts.Add("Chronicle page: " + (session.Quests.Db.Documents.TryGetValue(d, out var doc) ? doc.title : d));
            foreach (var s in r.reputation) parts.Add((s.amount > 0 ? "+" : "") + s.amount + " standing with " + (session.Quests.Db.Factions.TryGetValue(s.faction, out var f) ? f.name : s.faction));
            return parts.Count == 0 ? "None but thanks." : string.Join("\n", parts);
        }

        // ---------- quest book ----------
        void DrawQuestBook()
        {
            QuestStyles();
            var log = session.Quests; if (log == null) return;
            var w = BookRect;
            Fill(new Rect(w.x - 4, w.y - 4, w.width + 8, w.height + 8), new Color(.25f, .17f, .08f, .98f));
            Fill(w, new Color(.1f, .09f, .08f, .97f));
            Shadow(new Rect(w.x + 24, w.y + 14, 400, 32), "QUEST BOOK", heading, gold);
            // A newly revealed page jumps to the Chronicle once; after that the reader can switch tabs freely.
            if (!string.IsNullOrEmpty(session.ReadingDocument) && session.ReadingDocument != lastReading) BookTab = "chronicle";
            lastReading = session.ReadingDocument;
            string[] tabs = { "quests", "chronicle", "standing", "discoveries", "achievements", "armoury" };
            string[] names = { "Quests", "Chronicle", "Standing", "Discoveries", "Achievements", "Armoury" + (session.ArmouryUnseen > 0 ? "  +" + session.ArmouryUnseen : "") };
            for (int i = 0; i < tabs.Length; i++)
            {
                GUI.enabled = BookTab != tabs[i];
                if (GUI.Button(new Rect(w.x + 236 + i * 111, w.y + 16, 106, 30), names[i], micro)) { BookTab = tabs[i]; if (tabs[i] != "chronicle") session.ReadingDocument = null; }
                GUI.enabled = true;
            }
            if (GUI.Button(new Rect(w.xMax - 124, w.y + 16, 100, 30), "Close [L]", micro)) { session.QuestBookOpen = false; session.ReadingDocument = null; }
            var list = new Rect(w.x + 20, w.y + 64, 330, w.height - 84); var page = new Rect(list.xMax + 20, list.y, w.xMax - list.xMax - 40, list.height);
            Fill(list, new Color(1, 1, 1, .04f));
            if (BookTab == "quests") BookQuests(log, list, page);
            else if (BookTab == "chronicle") BookChronicle(log, list, page);
            else if (BookTab == "discoveries") BookDiscoveries(list, page);
            else if (BookTab == "achievements") BookAchievements(list, page);
            else if (BookTab == "armoury") BookArmoury(list, page);
            else BookStanding(log, new Rect(list.x, list.y, w.width - 40, list.height));
        }
        void BookQuests(QuestLog log, Rect list, Rect page)
        {
            var active = new List<(QuestDef quest, QuestState state)>(log.Active());
            active.Sort((a, b) => (a.quest.IsMain ? 0 : 1).CompareTo(b.quest.IsMain ? 0 : 1));
            float y = list.y + 8;
            Shadow(new Rect(list.x + 10, y, list.width, 20), "ACTIVE  (" + active.Count + ")", qHead, gold); y += 26;
            foreach (var (q, s) in active)
            {
                bool ready = s.step >= q.steps.Length;
                GUI.enabled = bookQuest != q.id;
                if (GUI.Button(new Rect(list.x + 8, y, list.width - 16, 34), (q.IsMain ? "◆ " : "") + q.title + (ready ? "  (complete)" : ""), qList)) bookQuest = q.id;
                GUI.enabled = true; y += 38;
            }
            if (active.Count == 0) { Shadow(new Rect(list.x + 10, y, list.width - 20, 40), "No quests. Look for ! over people's heads.", tiny, new Color(.8f, .8f, .8f)); y += 44; }
            y += 10; Shadow(new Rect(list.x + 10, y, list.width, 20), "COMPLETED  (" + log.Progress.questsDone.Count + ")", qHead, new Color(.7f, .7f, .7f)); y += 24;
            foreach (var id in log.Progress.questsDone)
            {
                if (y > list.yMax - 24) break;
                var q = log.Def(id); Shadow(new Rect(list.x + 16, y, list.width - 24, 20), "✓ " + (q != null ? q.title : id), tiny, new Color(.65f, .65f, .62f)); y += 20;
            }
            var sel = log.Def(bookQuest); var state = sel != null ? log.State(sel.id) : null;
            if (sel == null || state == null) { if (active.Count > 0) bookQuest = active[0].quest.id; Shadow(new Rect(page.x, page.y + 10, page.width, 30), "Select a quest.", text, new Color(.8f, .8f, .8f)); return; }
            Fill(page, Parchment);
            float py = page.y + 16; var inner = page.width - 40;
            Ink(new Rect(page.x + 20, py, inner, 32), sel.title, qTitle, new Color(.42f, .22f, .05f)); py += 34;
            Ink(new Rect(page.x + 20, py, inner, 22), KindLabel(sel) + (sel.giver == "auto" ? "" : "  ·  from " + sel.giver) + (sel.turnIn != sel.giver ? "  ·  return to " + sel.turnIn : "") +
                (sel.canonStatus != null ? "  ·  " + sel.canonStatus : ""), qSmall, new Color(.4f, .32f, .22f)); py += 30;
            var step = log.CurrentStep(state);
            string now = step == null ? "Return to " + sel.turnIn + "." : step.text;
            float h = qBody.CalcHeight(new GUIContent(now), inner); Ink(new Rect(page.x + 20, py, inner, h), now, qBody, InkBrown); py += h + 12;
            if (step != null)
                for (int i = 0; i < step.objectives.Length; i++)
                { bool done = log.ObjectiveDone(state, i); Ink(new Rect(page.x + 30, py, inner - 10, 22), (done ? "✓ " : "• ") + log.ObjectiveLine(state, i), qSmall, done ? new Color(.35f, .45f, .3f) : InkBrown); py += 22; }
            py += 10;
            if (!string.IsNullOrEmpty(sel.summary)) { float sh = qSmall.CalcHeight(new GUIContent(sel.summary), inner); Ink(new Rect(page.x + 20, py, inner, sh), sel.summary, qSmall, new Color(.35f, .27f, .18f)); py += sh + 12; }
            Ink(new Rect(page.x + 20, py, 300, 22), "REWARDS", qHead, new Color(.42f, .22f, .05f)); py += 24;
            Ink(new Rect(page.x + 20, py, inner, 90), RewardText(sel), qSmall, InkBrown);
            state.tracked = GUI.Toggle(new Rect(page.x + 20, page.yMax - 44, 24, 30), state.tracked, "");
            Ink(new Rect(page.x + 46, page.yMax - 46, 200, 26), "Show in tracker", qBody, InkBrown);
            if (!sel.IsMain && GUI.Button(new Rect(page.xMax - 170, page.yMax - 48, 150, 34), "Abandon", micro)) { log.Abandon(sel.id); bookQuest = null; session.Save(false); }
        }
        void BookChronicle(QuestLog log, Rect list, Rect page)
        {
            float y = list.y + 8;
            Shadow(new Rect(list.x + 10, y, list.width, 20), "PAGES FOUND  (" + log.Progress.documents.Count + ")", qHead, gold); y += 26;
            foreach (var id in log.Progress.documents)
            {
                if (!log.Db.Documents.TryGetValue(id, out var doc)) continue;
                GUI.enabled = session.ReadingDocument != id;
                if (GUI.Button(new Rect(list.x + 8, y, list.width - 16, 34), doc.title, qList)) { session.ReadingDocument = id; textScroll = Vector2.zero; }
                GUI.enabled = true; y += 38;
            }
            y += 10; Shadow(new Rect(list.x + 10, y, list.width, 20), "THE STORY SO FAR", qHead, gold); y += 24;
            foreach (var id in log.Progress.questsDone)
            { var q = log.Def(id); if (q == null || !q.IsMain) continue; Shadow(new Rect(list.x + 16, y, list.width - 24, 20), "◆ " + q.title, tiny, new Color(1, .86f, .5f)); y += 20; }
            foreach (var (q, s) in log.Active()) if (q.IsMain) { Shadow(new Rect(list.x + 16, y, list.width - 24, 20), "◇ " + q.title + " (now)", tiny, new Color(.85f, .8f, .7f)); y += 20; }
            if (session.ReadingDocument == null || !log.Db.Documents.TryGetValue(session.ReadingDocument, out var reading))
            { Shadow(new Rect(page.x, page.y + 10, page.width, 60), log.Progress.documents.Count == 0 ? "Letters, ledgers and songs you find are kept here." : "Choose a page to read.", text, new Color(.8f, .8f, .8f)); return; }
            Fill(page, new Color(.93f, .88f, .76f, 1));
            Ink(new Rect(page.x + 30, page.y + 20, page.width - 60, 34), reading.title, qTitle, new Color(.3f, .16f, .05f));
            var view = new Rect(page.x + 30, page.y + 64, page.width - 50, page.height - 128);
            float th = qBody.CalcHeight(new GUIContent(reading.text), view.width - 24);
            textScroll = GUI.BeginScrollView(view, textScroll, new Rect(0, 0, view.width - 24, th + 10));
            Ink(new Rect(0, 0, view.width - 24, th), reading.text, qBody, InkBrown);
            GUI.EndScrollView();
            if (!string.IsNullOrEmpty(reading.canonStatus)) Ink(new Rect(page.x + 30, page.yMax - 56, page.width - 60, 48), "Lore status: " + reading.canonStatus, qSmall, new Color(.45f, .38f, .28f));
        }
        void BookStanding(QuestLog log, Rect area)
        {
            float y = area.y + 10;
            Shadow(new Rect(area.x + 10, y, area.width, 24), "How the peoples and powers of Crulanda regard you.", text, new Color(.85f, .85f, .8f)); y += 40;
            foreach (var f in log.Db.FactionOrder)
            {
                int v = log.Standing(f.id), tier = QuestLog.Tier(v);
                Fill(new Rect(area.x, y, area.width, 86), new Color(1, 1, 1, .04f));
                Shadow(new Rect(area.x + 14, y + 8, 360, 24), f.name, qHead, gold);
                Color tc = tier <= 0 ? new Color(.9f, .3f, .25f) : tier == 1 ? new Color(.9f, .6f, .3f) : tier == 2 ? new Color(.85f, .85f, .7f) : new Color(.45f, .85f, .45f);
                UnitBar(new Rect(area.x + 380, y + 12, 320, 16), QuestLog.TierProgress(v), tc, QuestLog.TierNames[tier] + (f.fixedStanding ? "  (fixed)" : ""));
                Shadow(new Rect(area.x + 14, y + 36, area.width - 28, 48), f.description + (f.canonStatus != null ? "   [" + f.canonStatus + "]" : ""), tiny, new Color(.82f, .8f, .74f));
                y += 94;
            }
        }
    }
}
