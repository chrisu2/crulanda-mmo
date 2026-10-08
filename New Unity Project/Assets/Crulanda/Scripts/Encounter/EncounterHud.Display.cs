using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The HUD on any screen, and the Display window (playtest note 94, 2026-10-08: "we need to be able to choose resolution, full
    /// screen, window mode. I'm using 3440x1440 but everything is too big and not scaled correctly"). The HUD is drawn on a 1440x900
    /// canvas; it was stretched to the screen's width and height apart, so on 21:9 it came out wide and big. Now it is scaled the same
    /// both ways (the smaller of the two fits) times the UI scale, and centred: on a wide screen the panels keep their shape in the
    /// middle, and names over heads still show out to the screen's edges. Esc > Display: resolution, fullscreen, borderless or
    /// windowed, and the UI scale (kept in PlayerPrefs, "ui.scale"; Unity keeps the resolution and mode itself).
    /// </summary>
    public sealed partial class EncounterHud
    {
        static float uiScale = -1;
        /// <summary>The player's UI scale (.6 to 1.4); at first a little smaller on a big screen (1440 lines and up).</summary>
        public static float UiScale
        {
            get { if (uiScale < 0) { float d = Screen.height >= 1400 ? .8f : 1; try { uiScale = PlayerPrefs.GetFloat("ui.scale", d); } catch { uiScale = d; } } return uiScale; }
            set { uiScale = Mathf.Clamp(value, .6f, 1.4f); try { PlayerPrefs.SetFloat("ui.scale", uiScale); } catch { } }
        }
        /// <summary>Canvas pixels to screen pixels, the same both ways.</summary>
        static float CanvasScale { get { return Mathf.Min(Screen.width / 1440f, Screen.height / 900f) * UiScale; } }
        /// <summary>Where the canvas's corner sits on the screen (it is centred).</summary>
        static Vector2 CanvasOffset { get { float s = CanvasScale; return new Vector2((Screen.width - 1440 * s) / 2, (Screen.height - 900 * s) / 2); } }
        static Matrix4x4 CanvasMatrix { get { var o = CanvasOffset; float s = CanvasScale; return Matrix4x4.TRS(new Vector3(o.x, o.y, 0), Quaternion.identity, new Vector3(s, s, 1)); } }
        /// <summary>A screen point (pixels, y up) on the canvas (y down).</summary>
        static Vector2 ScreenToCanvas(float x, float y) { var o = CanvasOffset; float s = CanvasScale; return new Vector2((x - o.x) / s, (Screen.height - y - o.y) / s); }
        /// <summary>Whether a canvas x is on the screen (past the canvas's own 0-1440 on a wide screen).</summary>
        static bool OnCanvasX(float x) { float m = CanvasOffset.x / CanvasScale; return x >= -m && x <= 1440 + m; }

        // ---------- movable panels (playtest note 97, 2026-10-08: "I can't move the UI, should be able to drag where I want") ----------
        static readonly (string id, string label, Rect rect, int anchor)[] Panels = {
            ("frames", "Your frames", new Rect(10, 10, 705, 240), -1), ("minimap", "Minimap", new Rect(1215, 0, 225, 240), 1),
            ("tracker", "Quest tracker", new Rect(1110, 236, 330, 200), 1), ("bar", "Action bar", new Rect(150, 772, 1140, 128), 0) };
        static readonly Dictionary<string, Vector2> panelOffsets = new Dictionary<string, Vector2>();
        static bool uiUnlocked; static string dragging; static Vector2 dragFrom;
        /// <summary>How far a panel is moved from its place on the 1440x900 canvas: where the player put it, else (on a wide screen) out to
        /// the screen's edge on its side.</summary>
        static Vector2 PanelOffset(string id)
        {
            if (panelOffsets.TryGetValue(id, out var o)) return o;
            float x = 0, y = 0; bool saved = false;
            try { saved = PlayerPrefs.HasKey("ui.pos." + id + ".x"); if (saved) { x = PlayerPrefs.GetFloat("ui.pos." + id + ".x"); y = PlayerPrefs.GetFloat("ui.pos." + id + ".y"); } } catch { }
            if (!saved) foreach (var pn in Panels) if (pn.id == id) x = pn.anchor * CanvasOffset.x / CanvasScale;
            return panelOffsets[id] = new Vector2(x, y);
        }
        static void SetPanelOffset(string id, Vector2 o) { panelOffsets[id] = o; try { PlayerPrefs.SetFloat("ui.pos." + id + ".x", o.x); PlayerPrefs.SetFloat("ui.pos." + id + ".y", o.y); } catch { } }
        /// <summary>Draw what follows where this panel was put (null: back to the canvas).</summary>
        static void At(string id) { GUI.matrix = id == null ? CanvasMatrix : CanvasMatrix * Matrix4x4.Translate(PanelOffset(id)); }
        /// <summary>Unlocked: a box over each panel to drag it, a button to lock them again and one to put them back.</summary>
        void DrawPanelHandles()
        {
            GUI.matrix = CanvasMatrix; var e = Event.current; var mouse = e.mousePosition;
            foreach (var pn in Panels)
            {
                var r = pn.rect; r.position += PanelOffset(pn.id);
                Fill(r, new Color(.95f, .78f, .35f, dragging == pn.id ? .35f : .18f)); Fill(new Rect(r.x, r.y, r.width, 2), new Color(.95f, .78f, .35f, .9f));
                Shadow(new Rect(r.x + 8, r.y + 6, 240, 22), pn.label + " (drag)", tiny, Color.white);
                if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(mouse)) { dragging = pn.id; dragFrom = mouse - PanelOffset(pn.id); e.Use(); }
            }
            if (e.type == EventType.MouseDrag && dragging != null) { SetPanelOffset(dragging, mouse - dragFrom); e.Use(); }
            if (e.type == EventType.MouseUp && dragging != null) { dragging = null; e.Use(); }
            if (GUI.Button(new Rect(620, 380, 200, 40), "Lock UI", button)) { uiUnlocked = false; dragging = null; }
            if (GUI.Button(new Rect(620, 426, 200, 32), "Put them back", slim)) foreach (var pn in Panels) { try { PlayerPrefs.DeleteKey("ui.pos." + pn.id + ".x"); PlayerPrefs.DeleteKey("ui.pos." + pn.id + ".y"); } catch { } panelOffsets.Remove(pn.id); }
        }
        static bool displayOpen; static int resPick = -1;
        static readonly string[] ModeNames = { "Fullscreen", "Borderless", "Windowed" };
        static readonly FullScreenMode[] Modes = { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
        static List<Resolution> resolutions;
        /// <summary>Everyone dressed again (the Show helms switch): you, your party and the sims about.</summary>
        void RedressAll()
        {
            GearBinder.Refresh();
            foreach (var c in session.PartySims) if (c != null) SimGear.Dress(c.GetComponent<ActorVisual>(), c.sim, session.Items);
            var pop = SimPopulation.Active; if (pop != null) foreach (var f in pop.Figures) if (f != null) SimGear.Dress(f.visual, f.sim, session.Items);
        }
        void DrawDisplay()
        {
            if (resolutions == null)
            {
                resolutions = new List<Resolution>();
                foreach (var r in Screen.resolutions) if (!resolutions.Exists(x => x.width == r.width && x.height == r.height)) resolutions.Add(r);
                if (resolutions.Count == 0) resolutions.Add(Screen.currentResolution);
            }
            if (resPick < 0) { resPick = resolutions.FindIndex(r => r.width == Screen.width && r.height == Screen.height); if (resPick < 0) resPick = resolutions.Count - 1; }
            int mode = System.Array.IndexOf(Modes, Screen.fullScreenMode); if (mode < 0) mode = 1;
            var box = new Rect(470, 200, 500, 360); Frame(box);
            GUI.Label(new Rect(box.x + 28, box.y + 16, 400, 34), "DISPLAY", heading);
            var res = resolutions[resPick];
            Shadow(new Rect(box.x + 28, box.y + 74, 140, 24), "Resolution", tiny, Color.white);
            if (GUI.Button(new Rect(box.x + 170, box.y + 70, 36, 30), "<", slim)) resPick = (resPick + resolutions.Count - 1) % resolutions.Count;
            Shadow(new Rect(box.x + 214, box.y + 74, 170, 24), res.width + " x " + res.height, tiny, new Color(1, .84f, .45f));
            if (GUI.Button(new Rect(box.x + 392, box.y + 70, 36, 30), ">", slim)) resPick = (resPick + 1) % resolutions.Count;
            Shadow(new Rect(box.x + 28, box.y + 124, 140, 24), "Mode", tiny, Color.white);
            for (int i = 0; i < 3; i++)
                if (GUI.Button(new Rect(box.x + 170 + i * 104, box.y + 120, 98, 30), ModeNames[i], slim)) { mode = i; Screen.fullScreenMode = Modes[i]; }
            Fill(new Rect(box.x + 170 + mode * 104, box.y + 151, 98, 3), new Color(1, .84f, .45f));
            Shadow(new Rect(box.x + 28, box.y + 174, 140, 24), "UI scale", tiny, Color.white);
            float v = GUI.HorizontalSlider(new Rect(box.x + 170, box.y + 180, 200, 20), UiScale, .6f, 1.4f);
            if (Mathf.Abs(v - UiScale) > .005f) UiScale = Mathf.Round(v * 20) / 20;
            Shadow(new Rect(box.x + 382, box.y + 174, 60, 24), Mathf.RoundToInt(UiScale * 100) + "%", tiny, new Color(1, .84f, .45f));
            bool helms = GUI.Toggle(new Rect(box.x + 260, box.y + 236, 220, 26), ActorVisual.ShowHelms, "  Show helms");
            if (helms != ActorVisual.ShowHelms) { ActorVisual.ShowHelms = helms; RedressAll(); }
            if (GUI.Button(new Rect(box.x + 260, box.y + 272, 200, 30), "Unlock UI (drag panels)", slim)) { uiUnlocked = true; displayOpen = false; session.Resume(); }
            if (GUI.Button(new Rect(box.x + 28, box.y + 230, 200, 36), "Apply resolution", button)) Screen.SetResolution(res.width, res.height, Modes[mode]);
            if (GUI.Button(new Rect(box.xMax - 128, box.y + 16, 100, 30), "Done", slim)) displayOpen = false;
            Shadow(new Rect(box.x + 28, box.y + 316, 450, 20), "The UI scale applies at once; the resolution when you apply it.", tiny, new Color(.85f, .85f, .8f));
        }
    }
}
