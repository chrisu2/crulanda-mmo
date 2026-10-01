using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Discoveries (hidden finds, DiscoveryLog): the "Discovered" toast, and the quest book's Discoveries tab, which counts every
    /// zone's secrets and names only the ones found. Nothing here draws on a map.
    /// </summary>
    public sealed partial class EncounterHud
    {
        GUIStyle toastKicker, toastName, qCount;
        string bookZone; Vector2 discoveryScroll;

        /// <summary>
        /// "DISCOVERED" over the find's name, centred high on the screen on a soft ink band ruled in gold. It fades in, holds and fades
        /// out over EncounterSession.ToastSeconds; finds made together follow one another. A cave you walk into names itself on the
        /// same band, its levels in place of "DISCOVERED".
        /// </summary>
        void DrawDiscoveryToast()
        {
            string name = session.ToastName; if (name == null) return;
            float age = session.ToastAge, alpha = Mathf.Clamp01(age / .3f) * Mathf.Clamp01((EncounterSession.ToastSeconds - age) / .8f);
            if (alpha <= .01f) return;
            if (toastName == null)
            {
                toastKicker = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                toastName = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
            }
            measureContent.text = name; float w = Mathf.Clamp(toastName.CalcSize(measureContent).x + 110, 360, 1200);
            var r = new Rect(720 - w / 2, 150, w, 88);
            Fill(r, new Color(ink.r, ink.g, ink.b, .55f * alpha));
            Fill(new Rect(r.x + 40, r.y, r.width - 80, 1.5f), new Color(gold.r, gold.g, gold.b, .75f * alpha));
            Fill(new Rect(r.x + 40, r.yMax - 1.5f, r.width - 80, 1.5f), new Color(gold.r, gold.g, gold.b, .75f * alpha));
            string kicker = session.ToastKicker ?? "DISCOVERED";
            if (kicker.Length > 0) Outlined(new Rect(r.x, r.y + 8, r.width, 24), kicker, toastKicker, new Color(gold.r, gold.g, gold.b, alpha));
            Outlined(new Rect(r.x, r.y + (kicker.Length > 0 ? 32 : 21), r.width, 46), name, toastName, new Color(1, .95f, .84f, alpha));
        }

        /// <summary>
        /// The quest book's Discoveries tab. Left: found / total for the whole world, then every zone (this one first) with its own
        /// count. Right, on parchment: the chosen zone's finds by name and text, then how many more lie hidden there, as a number only.
        /// </summary>
        void BookDiscoveries(Rect list, Rect page)
        {
            if (qCount == null) qCount = new GUIStyle(qHead) { alignment = TextAnchor.MiddleRight };
            var tallies = session.DiscoveryTallies();
            int found = 0, total = 0; foreach (var t in tallies) { found += t.found.Count; total += t.total; }
            float y = list.y + 8;
            Shadow(new Rect(list.x + 10, y, list.width - 20, 20), "DISCOVERIES  (" + found + " / " + total + ")", qHead, gold); y += 26;
            if (tallies.Count == 0)
            { Shadow(new Rect(page.x, page.y + 10, page.width, 60), "Nothing is hidden on this road.", text, new Color(.8f, .8f, .8f)); return; }
            if (bookZone == null || !tallies.Exists(t => t.zoneId == bookZone)) bookZone = tallies[0].zoneId;
            foreach (var t in tallies)
            {
                var r = new Rect(list.x + 8, y, list.width - 16, 34);
                GUI.enabled = bookZone != t.zoneId;
                if (GUI.Button(r, t.zoneName + (t.here ? "  (here)" : ""), qList)) { bookZone = t.zoneId; discoveryScroll = Vector2.zero; }
                GUI.enabled = true;
                bool all = t.total > 0 && t.Hidden == 0;
                Shadow(new Rect(r.xMax - 90, r.y + 7, 80, 20), t.found.Count + " / " + t.total, qCount, all ? new Color(.55f, 1, .55f) : gold);
                y += 38;
            }
            Shadow(new Rect(list.x + 10, list.yMax - 62, list.width - 20, 54), "No map shows these places: lookouts, caches and things left behind. Wander off the road and look.", tiny, new Color(.75f, .75f, .72f));

            var sel = tallies.Find(t => t.zoneId == bookZone);
            Fill(page, Parchment);
            float inner = page.width - 40, py = page.y + 16;
            Ink(new Rect(page.x + 20, py, inner, 32), sel.zoneName, qTitle, new Color(.42f, .22f, .05f)); py += 34;
            string sub = sel.total == 0 ? "Nothing is hidden here." : sel.found.Count + " of " + sel.total + " found" + (sel.here ? "  ·  you are here" : "");
            Ink(new Rect(page.x + 20, py, inner, 22), sub, qSmall, new Color(.4f, .32f, .22f)); py += 30;
            string rest = sel.Hidden == 1 ? "One more lies hidden in " + sel.zoneName + "." : sel.Hidden > 1 ? sel.Hidden + " more lie hidden in " + sel.zoneName + "." :
                sel.total > 0 ? "You have found everything hidden in " + sel.zoneName + "." : "";
            // Measure first, then draw in a scroll view (a zone can hold more finds than the page).
            var view = new Rect(page.x + 20, py, page.width - 30, page.yMax - 16 - py); float width = view.width - 24, height = 0;
            foreach (var d in sel.found) height += EntryHeight(d, width);
            measureContent.text = rest; float restH = rest.Length > 0 ? qSmall.CalcHeight(measureContent, width) : 0; height += restH + 8;
            discoveryScroll = GUI.BeginScrollView(view, discoveryScroll, new Rect(0, 0, width, height));
            float yy = 0;
            foreach (var d in sel.found)
            {
                Ink(new Rect(0, yy, width, 24), DiscoveryLog.Name(d), qHead, new Color(.42f, .22f, .05f)); yy += 24;
                if (!string.IsNullOrEmpty(d.text)) { measureContent.text = d.text; float th = qBody.CalcHeight(measureContent, width); Ink(new Rect(0, yy, width, th), d.text, qBody, InkBrown); yy += th; }
                if (!string.IsNullOrEmpty(d.canonStatus)) { Ink(new Rect(0, yy + 2, width, 20), "Lore status: " + d.canonStatus, qSmall, new Color(.45f, .38f, .28f)); yy += 22; }
                yy += 14;
            }
            if (restH > 0) Ink(new Rect(0, yy, width, restH), rest, qSmall, new Color(.35f, .27f, .18f));
            GUI.EndScrollView();
        }
        /// <summary>A found secret's height on the page: its name, its text, its lore status and the gap after it.</summary>
        float EntryHeight(Crulanda.World.ZoneSecret d, float width)
        {
            float h = 24 + 14;
            if (!string.IsNullOrEmpty(d.text)) { measureContent.text = d.text; h += qBody.CalcHeight(measureContent, width); }
            if (!string.IsNullOrEmpty(d.canonStatus)) h += 22;
            return h;
        }
    }
}
