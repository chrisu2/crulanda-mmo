using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The quest book's Achievements tab (Achievements): points earned of the total and the title worn on the left, with every
    /// group (each zone, then Crulanda) and its count; on the right, on parchment, the chosen group's achievements, how far along
    /// each is, its points and its title, which can be worn from here.
    /// </summary>
    public sealed partial class EncounterHud
    {
        string featGroup; Vector2 featScroll;

        void BookAchievements(Rect list, Rect page)
        {
            var feats = session.Feats;
            if (feats == null || feats.All.Count == 0) { Shadow(new Rect(page.x, page.y + 10, page.width, 60), "No deeds to record yet.", text, new Color(.8f, .8f, .8f)); return; }
            if (qCount == null) qCount = new GUIStyle(qHead) { alignment = TextAnchor.MiddleRight };
            float y = list.y + 8;
            Shadow(new Rect(list.x + 10, y, list.width - 20, 20), "ACHIEVEMENTS  (" + feats.Points + " / " + feats.Total + " points)", qHead, gold); y += 26;
            string worn = session.Progress.title;
            Shadow(new Rect(list.x + 10, y, list.width - 120, 20), "Title: " + (string.IsNullOrEmpty(worn) ? "none" : worn), tiny, new Color(1, .92f, .75f));
            if (!string.IsNullOrEmpty(worn) && GUI.Button(new Rect(list.xMax - 104, y - 2, 94, 22), "Take it off", micro)) feats.Wear(null);
            y += 28;
            var groups = feats.Groups;
            if (featGroup == null || !groups.Contains(featGroup)) featGroup = session.Zone != null && groups.Contains(session.Zone.Zone.displayName) ? session.Zone.Zone.displayName : groups[0];
            foreach (var g in groups)
            {
                var r = new Rect(list.x + 8, y, list.width - 16, 32);
                GUI.enabled = featGroup != g;
                if (GUI.Button(r, g, qList)) { featGroup = g; featScroll = Vector2.zero; }
                GUI.enabled = true;
                int have = 0, all = 0; foreach (var a in feats.All) if (a.group == g) { all++; if (feats.Has(a.id)) have++; }
                Shadow(new Rect(r.xMax - 90, r.y + 6, 80, 20), have + " / " + all, qCount, have == all ? new Color(.55f, 1, .55f) : gold);
                y += 36;
            }

            Fill(page, Parchment);
            float inner = page.width - 40, py = page.y + 16;
            Ink(new Rect(page.x + 20, py, inner, 32), featGroup, qTitle, new Color(.42f, .22f, .05f)); py += 40;
            var view = new Rect(page.x + 20, py, page.width - 30, page.yMax - 16 - py); float width = view.width - 24, height = 0;
            foreach (var a in feats.All) if (a.group == featGroup) height += 82;
            featScroll = GUI.BeginScrollView(view, featScroll, new Rect(0, 0, width, height));
            float yy = 0;
            foreach (var a in feats.All)
            {
                if (a.group != featGroup) continue;
                bool got = feats.Has(a.id); var ink = got ? new Color(.42f, .22f, .05f) : new Color(.45f, .4f, .34f);
                Ink(new Rect(0, yy, width - 150, 24), a.name, qHead, ink);
                Ink(new Rect(width - 150, yy, 150, 24), a.points + " pts" + (got ? "  ·  earned" : ""), qSmall, ink);
                Ink(new Rect(0, yy + 24, width, 20), a.description, qSmall, InkBrown);
                int at = a.Progress; float bw = Mathf.Min(260, width - 120);
                Fill(new Rect(0, yy + 48, bw, 10), new Color(0, 0, 0, .15f));
                Fill(new Rect(0, yy + 48, bw * at / Mathf.Max(1, a.goal), 10), got ? new Color(.35f, .6f, .25f) : new Color(.75f, .55f, .2f));
                Ink(new Rect(bw + 8, yy + 43, 80, 20), at + " / " + a.goal, qSmall, ink);
                if (a.title != null)
                {
                    if (got && session.Progress.title != a.title) { if (GUI.Button(new Rect(width - 150, yy + 42, 150, 22), "Wear \"" + a.title + "\"", micro)) feats.Wear(a.title); }
                    else Ink(new Rect(width - 190, yy + 43, 190, 20), got ? "Worn: " + a.title : "Title: " + a.title, qSmall, ink);
                }
                yy += 82;
            }
            GUI.EndScrollView();
        }
    }
}
