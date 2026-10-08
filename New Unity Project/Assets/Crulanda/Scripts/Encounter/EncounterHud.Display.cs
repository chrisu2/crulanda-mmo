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
            var box = new Rect(470, 200, 500, 330); Frame(box);
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
            if (GUI.Button(new Rect(box.x + 28, box.y + 230, 200, 36), "Apply resolution", button)) Screen.SetResolution(res.width, res.height, Modes[mode]);
            if (GUI.Button(new Rect(box.xMax - 128, box.y + 16, 100, 30), "Done", slim)) displayOpen = false;
            Shadow(new Rect(box.x + 28, box.y + 280, 450, 20), "The UI scale applies at once; the resolution when you apply it.", tiny, new Color(.85f, .85f, .8f));
        }
    }
}
