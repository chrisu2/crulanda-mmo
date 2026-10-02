using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Trades window (K): on the left one row per trade with its skill as a bar (gathering skills, then what everyone has,
    /// then the crafts and how many of them are taken up); on the right, on parchment, the chosen trade's page: what it is, how
    /// far the skill has come, whether its tool hangs at the belt, and for a gathering skill its trade bag (worn, or who makes it) and the guide to its seams, windfalls or
    /// herbs (NodeGuide). A trade with recipes has a second tab, its recipes (RecipePage): the list coloured by what each still
    /// teaches, the chosen one's makings, the station in reach, and Make and Make all. It stands where the character sheet does,
    /// with the bags open beside it; E at a station opens it on the station's trade, at its recipes.
    /// </summary>
    public sealed partial class EncounterHud
    {
        static bool tradesVisible;
        static readonly Rect TradesRect = new Rect(190, 110, 660, 600);
        static bool TradesUiBlocks(Vector2 p) { return tradesVisible && TradesRect.Contains(p); }
        /// <summary>The trade whose page is open, by id (capture tools set it; null = the first).</summary>
        public static string TradesPage;
        /// <summary>Whether a trade with recipes shows them (the Recipes tab) rather than its page (About). E at a station sets it.</summary>
        public static bool TradesRecipes;
        /// <summary>The recipe chosen in the Recipes tab, by id (null: the first that can be made now, else the first).</summary>
        public static string TradesRecipe;
        /// <summary>The Recipes tab lists only what can be made from the bags now.</summary>
        public static bool TradesCanMakeOnly;
        static readonly Color SkillBar = new Color(.78f, .58f, .22f), Dim = new Color(.68f, .66f, .6f);
        GUIStyle tradeNote;

        void DrawTrades()
        {
            QuestStyles(); ItemStyles(); var log = session.Professions; if (log == null) return;
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
        /// <summary>Who makes the trade bags, and where (GAME-ONLY).</summary>
        const string BagMaker = "Maud Tanner, Oakhaven";
        /// <summary>The trade bag that holds what a gathering trade's nodes give (the ore-poke for Mining), or null.</summary>
        ItemDef BagForTrade(ProfessionLog log, ProfessionDef d)
        {
            foreach (var n in log.Db.NodesFor(d.id)) { var item = session.Items?.Get(n.item); var bag = item == null ? null : Inventory.BagFor(session.Items, item.pouch); if (bag != null) return bag; }
            return null;
        }
        void TradePage(ProfessionLog log, ProfessionDef d, Rect page)
        {
            Fill(page, Parchment);
            float inner = page.width - 40, x = page.x + 20, py = page.y + 16; bool has = log.Has(d.id);
            var brown = new Color(.42f, .22f, .05f); var soft = new Color(.4f, .32f, .22f);
            // A trade with recipes has two tabs: its page and its recipes.
            bool recipes = log.Db.RecipesFor(d.id).Count > 0;
            if (recipes)
            {
                GUI.enabled = TradesRecipes; if (GUI.Button(new Rect(page.xMax - 176, page.y + 14, 76, 26), "About", micro)) TradesRecipes = false;
                GUI.enabled = !TradesRecipes; if (GUI.Button(new Rect(page.xMax - 96, page.y + 14, 76, 26), "Recipes", micro)) TradesRecipes = true;
                GUI.enabled = true;
                if (TradesRecipes) { RecipePage(log, d, page); return; }
            }
            Ink(new Rect(x, py, recipes ? inner - 170 : inner, 32), d.name, qTitle, brown); py += 34;
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
            // The trade bag for what this skill gathers: worn, carried, or who makes it.
            var bag = d.kind == "gather" ? BagForTrade(log, d) : null;
            if (bag != null)
            {
                bool worn = Inventory.Wears(session.Progress, bag.id), carried = !worn && Inventory.Count(session.Progress, bag.id) > 0;
                string line = worn ? bag.name + ": worn. " + bag.slots + " slots for " + Inventory.HoldsWords(bag.holds) + "."
                    : carried ? bag.name + ": in your bags. Use it to wear it." : "No " + LowerFirst(bag.name) + " (" + BagMaker + "). It keeps " + Inventory.HoldsWords(bag.holds) + " out of your bags.";
                measureContent.text = line; float h = qSmall.CalcHeight(measureContent, inner);
                Ink(new Rect(x, py, inner, h), line, qSmall, worn ? new Color(.2f, .36f, .14f) : carried ? new Color(.42f, .22f, .05f) : InkBrown); py += h + 10;
            }
            if (d.kind == "gather") NodeGuide(log, d, x, py, inner, page.yMax - 40);
            if (!string.IsNullOrEmpty(d.canonStatus)) Ink(new Rect(x, page.yMax - 34, inner, 22), "Lore status: " + d.canonStatus, qSmall, new Color(.45f, .38f, .28f));
        }
        static readonly Color HardInk = new Color(.62f, .16f, .1f), TeachesInk = new Color(.66f, .34f, .04f), SometimesInk = new Color(.5f, .45f, .06f), SpentInk = new Color(.45f, .42f, .38f);
        GUIStyle nodeInk;
        /// <summary>
        /// A gathering trade's guide: its kinds of node, easiest first, each with the skill it comes easily at and the zones it is
        /// found in, coloured by the character's skill: red where it is hard going (slow, and one at a time), orange while every one
        /// worked teaches, yellow while some do, grey once it has nothing left to teach. Rows that would run past bottom are left off.
        /// </summary>
        void NodeGuide(ProfessionLog log, ProfessionDef d, float x, float y, float width, float bottom)
        {
            var nodes = log.Db.NodesFor(d.id); if (nodes.Count == 0 || y + 46 > bottom) return;
            if (nodeInk == null) nodeInk = new GUIStyle(qSmall) { wordWrap = false, clipping = TextClipping.Clip };
            var brown = new Color(.42f, .22f, .05f); bool has = log.Has(d.id); int skill = log.Skill(d.id);
            Ink(new Rect(x, y, width, 22), d.id == "mining" ? "Seams" : d.id == "woodcutting" ? "Windfalls" : "Where it grows", qHead, brown); y += 26;
            foreach (var n in nodes)
            {
                if (y + 20 > bottom - 22) break;
                var where = session.ZonesWithNode(n.id);
                float up = ProfessionLog.GatherUpChance(skill, n.skill);
                var c = !has ? InkBrown : skill < n.skill ? HardInk : up >= 1 ? TeachesInk : up > 0 ? SometimesInk : SpentInk;
                Ink(new Rect(x, y, width * .46f, 20), n.name, nodeInk, c);
                Ink(new Rect(x + width * .46f, y, width * .12f, 20), n.skill.ToString(), nodeInk, c);
                Ink(new Rect(x + width * .58f, y, width * .42f, 20), where.Count > 0 ? string.Join(", ", where) : "not found yet", nodeInk, c);
                y += 21;
            }
            if (has && y + 20 <= bottom) Ink(new Rect(x, y + 2, width, 20), "Red: hard going, slow and one at a time.", nodeInk, HardInk);
        }

        // ---------- the Recipes tab ----------
        static readonly Color GreenInk = new Color(.2f, .42f, .12f), GoodInk = new Color(.2f, .36f, .14f);
        GUIStyle recipeToggle, nodeInkRight;
        Vector2 recipeScroll;
        /// <summary>A recipe's colour in the list: red while locked, then orange, yellow, green and grey as it teaches less.</summary>
        static Color RecipeInk(ProfessionLog.Difficulty d)
        {
            switch (d) { case ProfessionLog.Difficulty.Orange: return TeachesInk; case ProfessionLog.Difficulty.Yellow: return SometimesInk; case ProfessionLog.Difficulty.Green: return GreenInk; case ProfessionLog.Difficulty.Grey: return SpentInk; default: return HardInk; }
        }
        static string RecipeTeaches(ProfessionLog.Difficulty d)
        {
            switch (d) { case ProfessionLog.Difficulty.Orange: return "Teaches every time"; case ProfessionLog.Difficulty.Yellow: return "Teaches now and then"; case ProfessionLog.Difficulty.Green: return "Seldom teaches now"; case ProfessionLog.Difficulty.Grey: return "Teaches nothing now"; default: return null; }
        }
        /// <summary>"Forge", "Bench", "Fire": a station kind as the station line names it.</summary>
        static string StationTitle(string kind) { return string.IsNullOrEmpty(kind) ? "" : char.ToUpperInvariant(kind[0]) + kind.Substring(1); }
        /// <summary>
        /// A trade's recipes (DESIGN 11): the skill bar, the list easiest first, each coloured by what it still teaches (red while
        /// locked, with the skill it wants) and with how many the bags make ("x3"), a "Can make" filter; under it the chosen recipe:
        /// what it makes (the item's own tooltip), its makings as have / need (red when short), the station in reach ("Forge: Vell's
        /// smithy") or the one wanted (red), Make and Make all, and why it can't be made when it can't.
        /// </summary>
        void RecipePage(ProfessionLog log, ProfessionDef d, Rect page)
        {
            if (recipeToggle == null)
            {
                recipeToggle = new GUIStyle(GUI.skin.toggle) { fontSize = 14 };
                foreach (var s in new[] { recipeToggle.normal, recipeToggle.onNormal, recipeToggle.hover, recipeToggle.onHover, recipeToggle.active, recipeToggle.onActive }) s.textColor = InkBrown;
            }
            if (nodeInk == null) nodeInk = new GUIStyle(qSmall) { wordWrap = false, clipping = TextClipping.Clip };
            if (nodeInkRight == null) nodeInkRight = new GUIStyle(nodeInk) { alignment = TextAnchor.UpperRight };
            float inner = page.width - 40, x = page.x + 20, py = page.y + 16; bool has = log.Has(d.id); int skill = log.Skill(d.id);
            var brown = new Color(.42f, .22f, .05f); var items = session.Items;
            Ink(new Rect(x, py, inner - 170, 32), d.name, qTitle, brown); py += 38;
            if (has) UnitBar(new Rect(x, py + 2, inner, 18), skill / (float)ProfessionDatabase.MaxSkill, SkillBar, "Skill " + skill + " of " + ProfessionDatabase.MaxSkill);
            else Ink(new Rect(x, py, inner, 22), ShortNeed(d), qSmall, HardInk);
            py += 30;
            // The list.
            Ink(new Rect(x, py, inner * .5f, 22), "Recipes", qHead, brown);
            TradesCanMakeOnly = GUI.Toggle(new Rect(x + inner - 104, py, 104, 22), TradesCanMakeOnly, " Can make", recipeToggle);
            py += 24;
            var all = log.Db.RecipesFor(d.id);
            var list = TradesCanMakeOnly ? all.FindAll(r => log.DifficultyOf(r) != ProfessionLog.Difficulty.Locked && log.CanMake(r) > 0) : all;
            if (TradesRecipe == null || !list.Exists(r => r.id == TradesRecipe))
                TradesRecipe = (list.Find(r => log.DifficultyOf(r) != ProfessionLog.Difficulty.Locked && log.CanMake(r) > 0) ?? (list.Count > 0 ? list[0] : null))?.id;
            const float rowH = 26, listH = 140;
            var box = new Rect(x, py, inner, listH); Fill(box, new Color(.35f, .22f, .08f, .08f));
            if (list.Count == 0) Ink(new Rect(x + 8, py + 6, inner - 16, 22), "Nothing you can make from your bags.", qSmall, InkBrown);
            bool scroll = list.Count * rowH > listH;
            var view = new Rect(0, 0, inner - (scroll ? 18 : 0), list.Count * rowH);
            recipeScroll = GUI.BeginScrollView(box, recipeScroll, view);
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i]; var row = new Rect(0, i * rowH, view.width, rowH); var diff = log.DifficultyOf(r);
                if (r.id == TradesRecipe) Fill(row, new Color(.55f, .36f, .1f, .22f));
                Ink(new Rect(row.x + 8, row.y + 3, row.width * .66f, 20), r.name, nodeInk, RecipeInk(diff));
                int n = log.CanMake(r);
                string right = diff == ProfessionLog.Difficulty.Locked ? d.name + " " + r.skill : n > 0 ? "x" + n : "";
                Ink(new Rect(row.x + row.width * .66f, row.y + 3, row.width * .34f - 8, 20), right, nodeInkRight, diff == ProfessionLog.Difficulty.Locked ? HardInk : brown);
                if (GUI.Button(row, GUIContent.none, GUIStyle.none)) TradesRecipe = r.id;
            }
            GUI.EndScrollView();
            py += listH + 10;
            var rec = log.Db.Recipe(TradesRecipe); if (rec == null) return;
            // What it makes.
            var outDef = items?.Get(rec.output); int count = System.Math.Max(1, rec.count);
            var square = new Rect(x, py, Slot, Slot);
            ItemSquare(square, new ItemStack { item = rec.output, count = count });
            if (square.Contains(Event.current.mousePosition) && outDef != null) ItemTooltip(outDef, true);
            var made = log.DifficultyOf(rec);
            float tx = square.xMax + 10, tw = inner - Slot - 10; string teaches = RecipeTeaches(made);
            Ink(new Rect(tx, py - 2, tw, 22), rec.name, qHead, RecipeInk(made));
            Ink(new Rect(tx, py + 17, tw, 20), "Makes " + (outDef != null ? outDef.name : rec.output) + (count > 1 ? " x" + count : ""), nodeInk, InkBrown);
            Ink(new Rect(tx, py + 35, tw, 20), teaches ?? "Wants " + d.name + " " + rec.skill, nodeInk, teaches != null ? RecipeInk(made) : HardInk);
            py += Slot + 10;
            // Its makings: have / need.
            foreach (var kv in ProfessionLog.Needs(rec))
            {
                var inDef = items?.Get(kv.Key); int haveN = Inventory.Count(session.Progress, kv.Key);
                var sq = new Rect(x, py, 34, 34);
                ItemSquare(sq, new ItemStack { item = kv.Key, count = 1 });
                if (sq.Contains(Event.current.mousePosition) && inDef != null) ItemTooltip(inDef, false);
                Ink(new Rect(sq.xMax + 10, py + 7, inner * .62f, 22), inDef != null ? inDef.name : kv.Key, nodeInk, InkBrown);
                Ink(new Rect(x + inner * .7f, py + 7, inner * .3f, 22), haveN + " / " + kv.Value, nodeInkRight, haveN >= kv.Value ? GoodInk : HardInk);
                py += 38;
            }
            // The station.
            var station = session.StationFor(rec);
            Ink(new Rect(x, py + 2, inner, 22), station != null ? StationTitle(station.kind) + ": " + station.name : "Needs " + ProfessionLog.StationWords(rec.station) + " nearby", nodeInk, station != null ? GoodInk : HardInk);
            py += 28;
            // Make, Make all, and why not.
            bool can = session.CanCraft(rec, out string why); int all2 = can ? log.CanMake(rec) : 0;
            GUI.enabled = can && !session.Working;
            if (GUI.Button(new Rect(x, py, 110, 32), "Make", micro)) session.Make(rec, 1);
            if (GUI.Button(new Rect(x + 120, py, 150, 32), "Make all (" + all2 + ")", micro)) session.Make(rec, all2);
            GUI.enabled = true;
            py += 38;
            if (!can && why != null && why != ProfessionLog.StationWanted(rec.station) && py + 20 <= page.yMax - 6)
            { measureContent.text = why; float h = Mathf.Min(qSmall.CalcHeight(measureContent, inner), page.yMax - 6 - py); Ink(new Rect(x, py, inner, h), why, qSmall, HardInk); }
            else if (session.Working && py + 20 <= page.yMax - 6) Ink(new Rect(x, py, inner, 22), "At work: moving stops it.", qSmall, InkBrown);
        }
    }
}
