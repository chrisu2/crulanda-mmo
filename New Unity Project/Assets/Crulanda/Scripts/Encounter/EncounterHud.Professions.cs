using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Trades window (K): on the left one row per trade with its skill as a bar (gathering skills, then what everyone has,
    /// then the crafts and how many of them are taken up); on the right, on parchment, the chosen trade's page: what it is, how
    /// far the skill has come, and whether its tool hangs at the belt. It stands where the character sheet does, with the bags
    /// open beside it.
    /// </summary>
    public sealed partial class EncounterHud
    {
        static bool tradesVisible;
        static readonly Rect TradesRect = new Rect(190, 110, 660, 600);
        static bool TradesUiBlocks(Vector2 p) { return tradesVisible && TradesRect.Contains(p); }
        /// <summary>The trade whose page is open, by id (capture tools set it; null = the first).</summary>
        public static string TradesPage;
        static readonly Color SkillBar = new Color(.78f, .58f, .22f), Dim = new Color(.68f, .66f, .6f);
        GUIStyle tradeNote;

        void DrawTrades()
        {
            QuestStyles(); var log = session.Professions; if (log == null) return;
            if (tradeNote == null) tradeNote = new GUIStyle(tiny) { wordWrap = false, clipping = TextClipping.Clip };
            var w = TradesRect;
            Fill(new Rect(w.x - 3, w.y - 3, w.width + 6, w.height + 6), new Color(.3f, .22f, .12f, .98f)); Fill(w, new Color(.08f, .075f, .07f, .97f));
            Shadow(new Rect(w.x + 16, w.y + 10, 300, 32), "TRADES", heading, gold);
            if (GUI.Button(new Rect(w.xMax - 124, w.y + 12, 110, 30), "Close [K]", micro)) session.ShowTrades(false);
            var list = new Rect(w.x + 14, w.y + 54, 212, w.height - 68); var page = new Rect(list.xMax + 14, list.y, w.xMax - list.xMax - 28, list.height);
            Fill(list, new Color(1, 1, 1, .04f));
            if (log.Db.Order.Count == 0) { Shadow(new Rect(page.x, page.y + 10, page.width, 30), "No trades are known here.", text, new Color(.8f, .8f, .8f)); return; }
            if (TradesPage == null || log.Db.Profession(TradesPage) == null) TradesPage = log.Db.Order[0].id;
            float y = list.y + 8;
            y = TradeGroup(log, list, y, "GATHERING", "gather");
            y = TradeGroup(log, list, y, "FOR EVERYONE", "free");
            TradeGroup(log, list, y, "CRAFTS  " + log.CraftSlotsUsed + " of " + log.Db.CraftSlots, "craft");
            TradePage(log, log.Db.Profession(TradesPage), page);
        }
        /// <summary>A heading and the rows of one kind of trade. Returns the y under them (unchanged when there is none of that kind).</summary>
        float TradeGroup(ProfessionLog log, Rect list, float y, string title, string kind)
        {
            if (!log.Db.Order.Exists(d => d.kind == kind)) return y;
            Shadow(new Rect(list.x + 10, y, list.width - 20, 22), title, qHead, gold); y += 26;
            foreach (var d in log.Db.Order)
            {
                if (d.kind != kind) continue;
                var r = new Rect(list.x + 6, y, list.width - 12, 50); bool has = log.Has(d.id);
                GUI.enabled = TradesPage != d.id;
                if (GUI.Button(r, GUIContent.none, qList)) TradesPage = d.id;
                GUI.enabled = true;
                Shadow(new Rect(r.x + 10, r.y + 3, r.width - 20, 22), d.name, qHead, has ? Color.white : Dim);
                if (has) UnitBar(new Rect(r.x + 10, r.y + 30, r.width - 20, 12), log.Skill(d.id) / (float)ProfessionDatabase.MaxSkill, SkillBar, log.Skill(d.id) + " / " + ProfessionDatabase.MaxSkill);
                else Shadow(new Rect(r.x + 10, r.y + 25, r.width - 14, 22), ShortNeed(d), tradeNote, Dim);
                y += 54;
            }
            return y + 8;
        }
        /// <summary>Why a trade's row has no bar, in a few words: "Needs a pick (merchants)", or "Not taken up" for a craft.</summary>
        string ShortNeed(ProfessionDef d)
        {
            var tool = string.IsNullOrEmpty(d.tool) ? null : session.Items?.Get(d.tool);
            if (tool == null) return "Not taken up";
            string word = tool.name.Substring(tool.name.LastIndexOf(' ') + 1);
            return "Needs a " + word + " (merchants)";
        }
        static string LowerFirst(string s) { return string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1); }
        void TradePage(ProfessionLog log, ProfessionDef d, Rect page)
        {
            Fill(page, Parchment);
            float inner = page.width - 40, x = page.x + 20, py = page.y + 16; bool has = log.Has(d.id);
            var brown = new Color(.42f, .22f, .05f); var soft = new Color(.4f, .32f, .22f);
            Ink(new Rect(x, py, inner, 32), d.name, qTitle, brown); py += 34;
            string kind = d.kind == "gather" ? "Gathering  ·  free to everyone" : d.kind == "free" ? "Free to everyone" :
                "Craft  ·  " + log.CraftSlotsUsed + " of " + log.Db.CraftSlots + " taken up";
            Ink(new Rect(x, py, inner, 22), kind, qSmall, soft); py += 30;
            if (has) { int skill = log.Skill(d.id); UnitBar(new Rect(x, py + 2, inner, 18), skill / (float)ProfessionDatabase.MaxSkill, SkillBar, "Skill " + skill + " of " + ProfessionDatabase.MaxSkill); py += 36; }
            if (!string.IsNullOrEmpty(d.description))
            { measureContent.text = d.description; float h = qBody.CalcHeight(measureContent, inner); Ink(new Rect(x, py, inner, h), d.description, qBody, InkBrown); py += h + 14; }
            // Where the character stands with it: the tool at the belt, or what is still wanting.
            var tool = string.IsNullOrEmpty(d.tool) ? null : session.Items?.Get(d.tool);
            string state = tool != null
                ? (has ? tool.name + ": at your belt. It takes no room in your bags." : "You need a " + LowerFirst(tool.name) + ". Merchants sell them. Use it from your bags [I] to hang it at your belt.")
                : has ? null : "Not taken up.";
            if (state != null)
            { measureContent.text = state; float h = qSmall.CalcHeight(measureContent, inner); Ink(new Rect(x, py, inner, h), state, qSmall, has ? new Color(.2f, .36f, .14f) : new Color(.55f, .2f, .1f)); py += h + 10; }
            if (!string.IsNullOrEmpty(d.canonStatus)) Ink(new Rect(x, page.yMax - 34, inner, 22), "Lore status: " + d.canonStatus, qSmall, new Color(.45f, .38f, .28f));
        }
    }
}
