using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The bags' Legendary tab (WORLD_BOSS_1.md 5.1): the component bag (no slots: the pieces in a grid, each with its count and tooltip),
    /// the bad-luck counters, and your class's weapon and its stage. Nothing here can be dragged, sold or destroyed: the pieces are bound.
    /// </summary>
    public sealed partial class EncounterHud
    {
        bool legendTab;
        void DrawLegendTab(Rect w)
        {
            var p = session.Progress; var e = Event.current; var mouse = e.mousePosition;
            var bag = ComponentBag.Of(p);
            Shadow(new Rect(w.x + 160, w.y + 12, 200, 24), "Legendary pieces", text, new Color(1, .6f, .2f));
            float y = w.y + 48;
            if (bag.Count == 0) Shadow(new Rect(w.x + 14, y, w.width - 28, 40), "None yet. The Unraveller, risen in the Drain Reach, drops them: a Brood-Crystal every kill, your blank and her Heart in time.", tiny, new Color(.82f, .74f, .56f));
            for (int i = 0; i < bag.Count; i++)
            {
                var r = new Rect(w.x + 14 + (i % 6) * (Slot + Gap), y + (i / 6) * (Slot + Gap), Slot, Slot);
                ItemSquare(r, bag[i]);
                if (r.Contains(mouse)) ItemTooltip(session.Items?.Get(bag[i].item), false);
            }
            y += Mathf.Max(1, Mathf.CeilToInt(bag.Count / 6f)) * (Slot + Gap) + 8;
            GUI.Label(new Rect(w.x + 14, y, w.width - 28, 40), session.LegendCounters, tiny); y += 42;
            var weapon = LegendaryWeapons.For(p.classId);
            if (weapon != null)
            {
                string stage = LegendaryWeapons.IsAwake(p, weapon.id) ? "awake (legendary)" : LegendaryWeapons.Forged(p, weapon.id) ? "forged, asleep (epic): a note of the Original Song wakes it" : "not yet forged";
                Shadow(new Rect(w.x + 14, y, w.width - 28, 22), weapon.name + ": " + stage, frameName, LegendaryWeapons.IsAwake(p, weapon.id) ? new Color(1, .6f, .2f) : gold); y += 26;
                var missing = LegendaryWeapons.Missing(p, session.Items);
                if (missing != null) GUI.Label(new Rect(w.x + 14, y, w.width - 28, 60), missing.Count == 0 ? "Everything is here: take it to " + LegendaryWeapons.Smith + "'s night forge by the Forge District Gate (21:00-06:00)." : "Still wanting: " + string.Join(", ", missing) + ".", tiny);
            }
            if (GUI.Button(new Rect(w.xMax - 124, w.yMax - 44, 110, 32), "Close [I]", micro)) { session.InventoryOpen = false; session.CloseVendor(); session.CloseCapital(); }
        }
    }
}
