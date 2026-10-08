using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Controls window (playtest note 75, 2026-10-07), from the pause menu: every game key with its key; click one, then press
    /// the new key (Esc cancels); WASD and ESDF presets. Saved at once (KeyBindings).
    /// </summary>
    public sealed partial class EncounterHud
    {
        static bool controlsOpen; static int rebinding = -1;
        void DrawControls()
        {
            var rows = KeyBindings.All; float rowH = 30;
            var r = new Rect(430, 120, 580, 150 + rowH * rows.Length);
            Frame(r);
            GUI.Label(new Rect(r.x + 28, r.y + 16, 400, 34), "CONTROLS", heading);
            if (GUI.Button(new Rect(r.x + 28, r.y + 58, 160, 30), "WASD (E interacts)", slim)) { KeyBindings.Preset(false); rebinding = -1; }
            if (GUI.Button(new Rect(r.x + 196, r.y + 58, 180, 30), "ESDF (G interacts)", slim)) { KeyBindings.Preset(true); rebinding = -1; }
            float y = r.y + 100;
            for (int i = 0; i < rows.Length; i++)
            {
                Shadow(new Rect(r.x + 28, y + 4, 300, 22), rows[i].label, tiny, Color.white);
                string shown = rebinding == i ? "press a key..." : KeyBindings.Label(rows[i].key);
                if (GUI.Button(new Rect(r.x + 360, y, 190, 26), shown, slim)) rebinding = rebinding == i ? -1 : i;
                y += rowH;
            }
            Shadow(new Rect(r.x + 28, y + 6, 520, 20), rebinding >= 0 ? "Press the new key (Esc cancels). A key already in use swaps over." : "Click a key to change it. Saved at once.", tiny, new Color(.85f, .85f, .8f));
            if (GUI.Button(new Rect(r.xMax - 128, r.y + 16, 100, 30), "Done", slim)) { controlsOpen = false; rebinding = -1; }
#if ENABLE_INPUT_SYSTEM
            if (rebinding >= 0 && Event.current.type == EventType.Repaint)
            {
                var k = KeyBindings.PressedNow();
                if (k == UnityEngine.InputSystem.Key.Escape) rebinding = -1;
                else if (k != UnityEngine.InputSystem.Key.None) { KeyBindings.Bind(rows[rebinding].key, k); rebinding = -1; }
            }
#endif
        }
    }
}
