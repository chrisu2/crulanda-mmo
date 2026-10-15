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
            ("tracker", "Quest tracker", new Rect(1110, 236, 330, 200), 1), ("bar", "Action bar", new Rect(150, 772, 1140, 128), 0),
            // Chris 2026-10-12 1:29, "meter should be movable in the ui. pet bar should be movable in the ui."
            ("meter", "Damage meter", new Rect(1100, 456, 340, 148), 1), ("pet", "Pet bar", new Rect(85, 800, 250, 76), 0),
            // Chris 2026-10-14 19:15, "i need the nerve now but probably need a seperate small ui element i can move around".
            ("nerve", "Nerve", new Rect(380, 96, 250, 44), -1) };
        /// <summary>A panel's rect on the canvas where it is drawn now (its place plus the offset the player gave it).</summary>
        public static Rect PanelRect(string id) { foreach (var pn in Panels) if (pn.id == id) { var r = pn.rect; r.position += PanelOffset(id); return r; } return Rect.zero; }
        /// <summary>Move a panel by an offset from its place (what a drag does), saved.</summary>
        public static void MovePanel(string id, Vector2 offset) { SetPanelOffset(id, offset); }
        /// <summary>Every panel back where the game puts it, the saved positions forgotten.</summary>
        public static void ResetPanels() { foreach (var pn in Panels) { try { PlayerPrefs.DeleteKey("ui.pos." + pn.id + ".x"); PlayerPrefs.DeleteKey("ui.pos." + pn.id + ".y"); } catch { } panelOffsets.Remove(pn.id); } }
        /// <summary>Forget the in-memory positions (as a fresh run would), so the next read comes from PlayerPrefs.</summary>
        public static void ForgetPanelCache() { panelOffsets.Clear(); }
        static readonly Dictionary<string, Vector2> shotOffsets = new Dictionary<string, Vector2>();
        /// <summary>For captures: a panel shown moved without touching the player's saved positions (null: back).</summary>
        public static void ShowPanelMoved(string id, Vector2? offset) { if (offset.HasValue) shotOffsets[id] = offset.Value; else shotOffsets.Remove(id); }
        static readonly Dictionary<string, Vector2> panelOffsets = new Dictionary<string, Vector2>();
        static bool uiUnlocked; static string dragging; static Vector2 dragFrom;
        /// <summary>How far a panel is moved from its place on the 1440x900 canvas: where the player put it, else (on a wide screen) out to
        /// the screen's edge on its side.</summary>
        /// <summary>The panels where the game puts them, the saved positions ignored (a capture at 1440x900 under Chris's 3440x1440 positions
        /// had the party frame off the screen, 2026-10-11).</summary>
        public static bool DefaultPanels;
        static Vector2 PanelOffset(string id)
        {
            if (shotOffsets.TryGetValue(id, out var so)) return so;
            if (DefaultPanels) { float dx = 0; foreach (var pn in Panels) if (pn.id == id) dx = pn.anchor * CanvasOffset.x / CanvasScale; return new Vector2(dx, 0); }
            if (panelOffsets.TryGetValue(id, out var o)) return OnScreen(id, o);
            float x = 0, y = 0; bool saved = false;
            try { saved = PlayerPrefs.HasKey("ui.pos." + id + ".x"); if (saved) { x = PlayerPrefs.GetFloat("ui.pos." + id + ".x"); y = PlayerPrefs.GetFloat("ui.pos." + id + ".y"); } } catch { }
            if (!saved) foreach (var pn in Panels) if (pn.id == id) x = pn.anchor * CanvasOffset.x / CanvasScale;
            return OnScreen(id, panelOffsets[id] = new Vector2(x, y));
        }
        /// <summary>A panel kept on the screen whatever its saved place (2026-10-13, Chris: "there is no target or self or party bars": the
        /// frames were saved off the edge of a smaller window, and not even their drag box showed). The saved place is kept, so on the big
        /// screen it comes back where it was put.</summary>
        public static Vector2 OnScreen(string id, Vector2 o)
        {
            foreach (var pn in Panels)
            {
                if (pn.id != id) continue;
                float mx = CanvasOffset.x / CanvasScale, my = CanvasOffset.y / CanvasScale, r = pn.rect.xMax, b = pn.rect.yMax;
                const float Keep = 40;   // at least this much of it on the screen, to see and drag back
                o.x = Mathf.Max(-mx - r + Keep, Mathf.Min(o.x, 1440 + mx - pn.rect.x - Keep)); o.y = Mathf.Max(-my - b + Keep, Mathf.Min(o.y, 900 + my - pn.rect.y - Keep));
            }
            return o;
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
            // The chat moves here too, whole (its own bar is mostly tabs; playtest note 100, "can't move chat panel").
            Fill(chatRect, new Color(.95f, .78f, .35f, dragging == "chat" ? .35f : .18f)); Shadow(new Rect(chatRect.x + 8, chatRect.y + 26, 240, 22), "Chat (drag)", tiny, Color.white);
            if (e.type == EventType.MouseDown && e.button == 0 && dragging == null && chatRect.Contains(mouse)) { dragging = "chat"; dragFrom = mouse - chatRect.position; e.Use(); }
            if (e.type == EventType.MouseDrag && dragging == "chat") { chatRect.position = mouse - dragFrom; try { PlayerPrefs.SetFloat("chat.x", chatRect.x); PlayerPrefs.SetFloat("chat.y", chatRect.y); } catch { } e.Use(); return; }
            if (e.type == EventType.MouseDrag && dragging != null) { SetPanelOffset(dragging, mouse - dragFrom); e.Use(); }
            if (e.type == EventType.MouseUp && dragging != null) { dragging = null; e.Use(); }
            if (GUI.Button(new Rect(620, 380, 200, 40), "Lock UI", button)) { uiUnlocked = false; dragging = null; }
            if (GUI.Button(new Rect(620, 426, 200, 32), "Put them back", slim)) ResetPanels();
        }
        static bool displayOpen; static int resPick = -1;
        // Full screen is always borderless at the monitor's own size (playtest note 121, Chris, 2026-10-11: "going full screen at
        // 3440x1440 everything is blurry and screw up my text on computer screen ... after i tab out"). "Fullscreen" had been the
        // exclusive mode at whatever size the window was (1920x1080 at first): the monitor itself switched to that size and stretched
        // it, and every alt-tab switched the monitor back and forth. The window size is for windowed play only.
        static readonly string[] ModeNames = { "Full screen", "Windowed" };
        public static readonly FullScreenMode[] DisplayModes = { FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
        /// <summary>The monitor's own size.</summary>
        static Vector2Int Native { get { var d = Display.main; return d != null && d.systemWidth > 0 ? new Vector2Int(d.systemWidth, d.systemHeight) : new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height); } }
        /// <summary>What to set at start-up, or null to leave it: anything full screen that is not borderless at the monitor's own size
        /// becomes that (an exclusive mode or a smaller size kept from before); a window is left as it is.</summary>
        public static (int width, int height, FullScreenMode mode)? StartupDisplay(FullScreenMode mode, int width, int height, int nativeWidth, int nativeHeight)
        {
            if (mode == FullScreenMode.Windowed || mode == FullScreenMode.MaximizedWindow) return null;
            if (mode == FullScreenMode.FullScreenWindow && width == nativeWidth && height == nativeHeight) return null;
            return (nativeWidth, nativeHeight, FullScreenMode.FullScreenWindow);
        }
        static bool displayChecked;
        /// <summary>Once a run: full screen put right (StartupDisplay).</summary>
        static void CheckDisplay()
        {
            if (displayChecked || Application.isEditor) return; displayChecked = true;
            var n = Native; var fix = StartupDisplay(Screen.fullScreenMode, Screen.width, Screen.height, n.x, n.y);
            if (fix.HasValue) Screen.SetResolution(fix.Value.width, fix.Value.height, fix.Value.mode);
        }
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
            int mode = Screen.fullScreenMode == FullScreenMode.Windowed || Screen.fullScreenMode == FullScreenMode.MaximizedWindow ? 1 : 0;
            var box = new Rect(470, 200, 500, 360); Frame(box);
            GUI.Label(new Rect(box.x + 28, box.y + 16, 400, 34), "DISPLAY", heading);
            var res = resolutions[resPick];
            Shadow(new Rect(box.x + 28, box.y + 74, 140, 24), "Window size", tiny, Color.white);
            if (GUI.Button(new Rect(box.x + 170, box.y + 70, 36, 30), "<", slim)) resPick = (resPick + resolutions.Count - 1) % resolutions.Count;
            Shadow(new Rect(box.x + 214, box.y + 74, 170, 24), res.width + " x " + res.height, tiny, new Color(1, .84f, .45f));
            if (GUI.Button(new Rect(box.x + 392, box.y + 70, 36, 30), ">", slim)) resPick = (resPick + 1) % resolutions.Count;
            Shadow(new Rect(box.x + 28, box.y + 124, 140, 24), "Mode", tiny, Color.white);
            for (int i = 0; i < DisplayModes.Length; i++)
                if (GUI.Button(new Rect(box.x + 170 + i * 104, box.y + 120, 98, 30), ModeNames[i], slim))
                {
                    mode = i; var n = Native;
                    if (DisplayModes[i] == FullScreenMode.Windowed) Screen.SetResolution(Mathf.Min(res.width, n.x), Mathf.Min(res.height, n.y), FullScreenMode.Windowed);
                    else Screen.SetResolution(n.x, n.y, FullScreenMode.FullScreenWindow);
                }
            Fill(new Rect(box.x + 170 + mode * 104, box.y + 151, 98, 3), new Color(1, .84f, .45f));
            Shadow(new Rect(box.x + 28, box.y + 174, 140, 24), "UI scale", tiny, Color.white);
            float v = GUI.HorizontalSlider(new Rect(box.x + 170, box.y + 180, 200, 20), UiScale, .6f, 1.4f);
            if (Mathf.Abs(v - UiScale) > .005f) UiScale = Mathf.Round(v * 20) / 20;
            Shadow(new Rect(box.x + 382, box.y + 174, 60, 24), Mathf.RoundToInt(UiScale * 100) + "%", tiny, new Color(1, .84f, .45f));
            bool helms = GUI.Toggle(new Rect(box.x + 260, box.y + 236, 220, 26), ActorVisual.ShowHelms, "  Show helms");
            if (helms != ActorVisual.ShowHelms) { ActorVisual.ShowHelms = helms; RedressAll(); }
            if (GUI.Button(new Rect(box.x + 260, box.y + 272, 200, 30), "Unlock UI (drag panels)", slim)) { uiUnlocked = true; displayOpen = false; session.Resume(); }
            if (GUI.Button(new Rect(box.x + 28, box.y + 230, 200, 36), "Window at this size", button)) { var n = Native; Screen.SetResolution(Mathf.Min(res.width, n.x), Mathf.Min(res.height, n.y), FullScreenMode.Windowed); }
            if (GUI.Button(new Rect(box.xMax - 128, box.y + 16, 100, 30), "Done", slim)) displayOpen = false;
            Shadow(new Rect(box.x + 28, box.y + 316, 450, 20), "Full screen is always your monitor's own size; the window size is for a window.", tiny, new Color(.85f, .85f, .8f));
        }
    }
}
