#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The sky ghost over the Rust Market (the Lowtowns' world capture, lowtowns-12-the-rust-market.png, 2026-10-17): pale upside-down
    /// roofs, chimneys and downward spikes in the top 15% of the frame. Builds zone.lowtowns as the world capture does, frames the
    /// landmark as CaptureWorldTour does (its authored view, pitch and zoom), renders the main camera into a 1600x900 RenderTexture,
    /// and measures the top band: the share of its pixels whose luminance stands more than 12/255 off the band's median (the
    /// main figure) and its edge count. Then renders again with each suspect off in turn (every top-level child of the zone root,
    /// the children of the two that clear most, each other enabled camera, the water, the reflection probes, the cloud dome, the
    /// post-processing, RoofFade, particles, the skybox, fog, the distant-shader rings, anything upside down or mirrored, and
    /// everything outside the zone) and logs "SKYGHOST name metric ...". The screen (the Game view, as ScreenCapture sees it) is
    /// measured too, for the cameras that draw over the main one. Ends with "SKYGHOST RESULT": the toggle that drops the figure most.
    /// PNGs of the baseline, the screen and the three best toggles go to D:/crulanda-work/world-captures/skyghost/.
    /// Explicit (never in the normal suites); needs a graphics device (not -nographics). Never fails on what it finds.
    /// </summary>
    public class SkyGhostDiagnostics
    {
        const string Landmark = "The Rust Market", OutDir = "D:/crulanda-work/world-captures/skyghost";
        const int W = 1600, H = 900;
        const float Band = .15f;
        string root; float timeScaleWas = 1;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-skyghost-" + Guid.NewGuid().ToString("N"));
            ZoneBuilder.RequestedZoneId = "zone.lowtowns";
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = timeScaleWas; EncounterHud.Hidden = false;
            ZoneBuilder.RequestedZoneId = null;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        /// <summary>One suspect: how to turn it off and back on.</summary>
        sealed class Suspect { public string name; public Action off, on; public float rt = -1, screen = -1; public int edges; public Color32[] pixels; }

        /// <summary>The band's figure: the share of pixels off the median by more than 12/255, and the count of edges (a step over 8/255
        /// to the right or upward neighbour). Rows from the top of the frame (ReadPixels' row 0 is the bottom).</summary>
        static float Measure(Color32[] px, int w, int h, out int edges)
        {
            int y0 = Mathf.FloorToInt(h * (1 - Band)), n = (h - y0) * w; var lum = new byte[n]; var hist = new int[256];
            for (int y = y0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = px[y * w + x]; byte l = (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * .2126f + c.g * .7152f + c.b * .0722f), 0, 255);
                    lum[(y - y0) * w + x] = l; hist[l]++;
                }
            int median = 0; for (int acc = 0; median < 256; median++) { acc += hist[median]; if (acc * 2 >= n) break; }
            int off = 0; edges = 0; int rows = h - y0;
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < w; x++)
                {
                    int l = lum[y * w + x]; if (Mathf.Abs(l - median) > 12) off++;
                    if (x + 1 < w && Mathf.Abs(l - lum[y * w + x + 1]) > 8) edges++;
                    if (y + 1 < rows && Mathf.Abs(l - lum[(y + 1) * w + x]) > 8) edges++;
                }
            return off / (float)n;
        }

        static Color32[] RenderMain(Camera cam, RenderTexture rt, Texture2D read)
        {
            var previous = RenderTexture.active; var oldTarget = cam.targetTexture;
            try
            {
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                read.ReadPixels(new Rect(0, 0, W, H), 0, 0); read.Apply(false);
                return read.GetPixels32();
            }
            finally { cam.targetTexture = oldTarget; RenderTexture.active = previous; }
        }

        static void Save(string file, Color32[] px, int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGB24, false); t.SetPixels32(px); t.Apply(false);
            File.WriteAllBytes(Path.Combine(OutDir, file), t.EncodeToPNG()); UnityEngine.Object.Destroy(t);
        }

        static string Slug(string s) { var b = new StringBuilder(); foreach (char c in s.ToLowerInvariant()) b.Append(char.IsLetterOrDigit(c) ? c : '-'); return b.ToString().Trim('-'); }

        /// <summary>Renderers off and on (only those enabled now).</summary>
        static Suspect Renderers(string name, IEnumerable<Renderer> list)
        {
            var on = list.Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy).ToList();
            return new Suspect { name = name + " (" + on.Count + " renderers)", off = () => { foreach (var r in on) if (r != null) r.enabled = false; }, on = () => { foreach (var r in on) if (r != null) r.enabled = true; } };
        }

        static bool UsesShader(Renderer r, string part)
        {
            foreach (var m in r.sharedMaterials) if (m != null && m.shader != null && m.shader.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        [UnityTest, Explicit("Diagnostic: renders the Rust Market view with each suspect off in turn"), Category("Diagnostics"), Timeout(300000)]
        public IEnumerator Find_the_sky_ghost()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Inconclusive("SKYGHOST needs a graphics device: run without -nographics.");
            var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var zone = ZoneBuilder.Active;
            Assert.NotNull(session); Assert.NotNull(zone); Assert.AreEqual("zone.lowtowns", zone.Zone.id);
            Directory.CreateDirectory(OutDir);
            // As CaptureWorldTour: two seconds in, the HUD hidden.
            yield return new WaitForSeconds(2);
            EncounterHud.Hidden = true;
            var l = Array.Find(zone.Zone.landmarks, x => x != null && x.name == Landmark); Assert.NotNull(l, Landmark + " is not a landmark of the Lowtowns.");
            // The landmark's shot as the tour frames it: its authored view (else from its south-west), facing it, its own pitch and zoom.
            var motor = session.Player.GetComponent<AdventurerMotor>(); var cam = session.View;
            var z = zone.Zone; var from = l.at + new Vector2(-.6f, -.8f) * Mathf.Min(l.radius + 4, 14);
            float lim = z.size / 2 - 10; from = new Vector2(Mathf.Clamp(from.x, -lim, lim), Mathf.Clamp(from.y, -lim, lim));
            bool authored = l.view != Vector2.zero; if (authored) from = l.view;
            var dir = l.at - from; float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            float pitch = l.viewPitch > 0 ? l.viewPitch : 17, zoom = l.viewZoom > 0 ? l.viewZoom : 11;
            if (authored && l.viewHeight > 0)
            {
                // The free camera high over the view (the tour's viewHeight branch), the player hidden below it.
                var high = zone.Ground(from) + Vector3.up * l.viewHeight; var target = zone.Ground(l.at) + Vector3.up * l.viewAim;
                motor.Teleport(zone.Ground(from, 1.1f)); foreach (var r in session.Player.GetComponentsInChildren<Renderer>()) r.enabled = false;
                motor.enabled = false; var look = Quaternion.LookRotation(target - high);
                for (float w = 0; w < .8f; w += Time.deltaTime) { cam.transform.SetPositionAndRotation(high, look); yield return null; }
                cam.transform.SetPositionAndRotation(high, look);
            }
            else
            {
                if (!authored) Debug.Log("SKYGHOST note: " + Landmark + " has no authored view; the tour would search one (LandmarkView), the orbit framing stands in here.");
                motor.Teleport(zone.Ground(from, 1.1f)); motor.SetView(yaw, pitch, zoom);
                yield return new WaitForSeconds(.6f);
            }
            Debug.Log("SKYGHOST camera at " + cam.transform.position.ToString("F1") + " euler " + cam.transform.eulerAngles.ToString("F1") + " fov " + cam.fieldOfView.ToString("F1")
                + " far " + cam.farClipPlane.ToString("F0") + " (view " + from + ", yaw " + yaw.ToString("F0") + ", pitch " + pitch + ", zoom " + zoom + ")");
            // Held still from here: the weather, the particles and the fades stop moving between the renders.
            timeScaleWas = Time.timeScale; Time.timeScale = 0;
            yield return null;

            var rt = new RenderTexture(W, H, 24); var read = new Texture2D(W, H, TextureFormat.RGB24, false);
            float ScreenFigure(out Color32[] px, out int pw, out int ph)
            {
                px = null; pw = ph = 0;
                Texture2D shot = null;
                try { shot = ScreenCapture.CaptureScreenshotAsTexture(); } catch (Exception e) { Debug.Log("SKYGHOST note: no screen capture (" + e.Message + ")"); }
                if (shot == null || shot.width < 64 || shot.height < 64) { if (shot != null) UnityEngine.Object.Destroy(shot); return -1; }
                px = shot.GetPixels32(); pw = shot.width; ph = shot.height; UnityEngine.Object.Destroy(shot);
                return Measure(px, pw, ph, out _);
            }
            try
            {
                // The baseline: the main camera into the RenderTexture, and the screen as ScreenCapture takes it.
                var basePx = RenderMain(cam, rt, read); float baseline = Measure(basePx, W, H, out int baseEdges);
                Save("00-baseline.png", basePx, W, H);
                yield return new WaitForEndOfFrame();
                float baseScreen = ScreenFigure(out var screenPx, out int sw, out int sh);
                if (screenPx != null) Save("00-screen.png", screenPx, sw, sh);
                Debug.Log("SKYGHOST baseline " + baseline.ToString("F4") + " edges=" + baseEdges + " screen=" + baseScreen.ToString("F4") + (screenPx != null ? " (" + sw + "x" + sh + ")" : ""));

                // What is in the scene: each other enabled camera, anything mirrored or upside down (logged by name).
                var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c != cam && c.enabled && c.gameObject.activeInHierarchy).ToList();
                foreach (var c in cameras) Debug.Log("SKYGHOST note: camera '" + c.name + "' depth " + c.depth + " target " + (c.targetTexture != null ? c.targetTexture.name + " " + c.targetTexture.width + "x" + c.targetTexture.height : "screen") + " clear " + c.clearFlags + " mask " + c.cullingMask);
                var all = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToList();
                var mirrored = all.Where(r => r.transform.localToWorldMatrix.determinant < 0).ToList();
                var upside = all.Where(r => r.transform.up.y < -.5f && !(r is ParticleSystemRenderer)).ToList();
                foreach (var r in mirrored.Take(12)) Debug.Log("SKYGHOST note: mirrored renderer " + NameOf(r.transform) + " at " + r.bounds.center.ToString("F0") + " size " + r.bounds.size.ToString("F0"));
                foreach (var r in upside.Take(12)) Debug.Log("SKYGHOST note: upside-down renderer " + NameOf(r.transform) + " at " + r.bounds.center.ToString("F0") + " size " + r.bounds.size.ToString("F0"));

                var suspects = new List<Suspect>();
                var children = new List<Transform>(); foreach (Transform t in zone.transform) if (t.gameObject.activeSelf) children.Add(t);
                foreach (var t in children) suspects.Add(Renderers("zone/" + t.name, t.GetComponentsInChildren<Renderer>()));
                foreach (var c in cameras) { var cc = c; suspects.Add(new Suspect { name = "camera " + c.name, off = () => cc.enabled = false, on = () => cc.enabled = true }); }
                suspects.Add(Renderers("water", all.Where(r => UsesShader(r, "Water"))));
                var probes = UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Where(p => p.enabled).ToList();
                suspects.Add(new Suspect { name = "reflection probes (" + probes.Count + ")", off = () => { foreach (var p in probes) p.enabled = false; }, on = () => { foreach (var p in probes) p.enabled = true; } });
                var dome = zone.transform.Find("Cloud layer");
                suspects.Add(Renderers("cloud dome", dome != null ? dome.GetComponentsInChildren<Renderer>() : new Renderer[0]));
                var post = cam.GetComponent<ZonePost>();
                if (post != null && post.enabled) suspects.Add(new Suspect { name = "post (ZonePost)", off = () => post.enabled = false, on = () => post.enabled = true });
                var fades = RoofFade.All.Where(f => f != null && f.enabled).ToList();
                suspects.Add(new Suspect { name = "RoofFade (" + fades.Count + ")", off = () => { foreach (var f in fades) if (f != null) f.enabled = false; }, on = () => { foreach (var f in fades) if (f != null) f.enabled = true; } });
                suspects.Add(Renderers("particles", all.Where(r => r is ParticleSystemRenderer)));
                var clearWas = cam.clearFlags; var backWas = cam.backgroundColor;
                suspects.Add(new Suspect { name = "skybox (solid fog colour)", off = () => { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor; }, on = () => { cam.clearFlags = clearWas; cam.backgroundColor = backWas; } });
                bool fogWas = RenderSettings.fog;
                suspects.Add(new Suspect { name = "fog", off = () => RenderSettings.fog = false, on = () => RenderSettings.fog = fogWas });
                suspects.Add(Renderers("distant shader", all.Where(r => UsesShader(r, "Crulanda/Distant"))));
                suspects.Add(Renderers("mirrored (negative scale)", mirrored));
                suspects.Add(Renderers("upside down", upside));
                suspects.Add(Renderers("outside the zone root", all.Where(r => !r.transform.IsChildOf(zone.transform))));

                IEnumerator Run(Suspect s)
                {
                    s.off();
                    s.pixels = RenderMain(cam, rt, read); s.rt = Measure(s.pixels, W, H, out s.edges);
                    yield return new WaitForEndOfFrame();
                    s.screen = ScreenFigure(out _, out _, out _);
                    s.on();
                    Debug.Log("SKYGHOST " + s.name + " " + s.rt.ToString("F4") + " edges=" + s.edges + " screen=" + s.screen.ToString("F4") + " drop=" + (baseline - s.rt).ToString("F4"));
                }
                foreach (var s in suspects) yield return Run(s);
                // A finer pass: each child of the two top-level children that clear the band most.
                var finer = new List<Suspect>();
                foreach (var t in children.Select(ch => (ch, s: suspects.First(x => x.name.StartsWith("zone/" + ch.name + " (")))).OrderBy(p => p.s.rt).Take(2).Select(p => p.ch))
                    foreach (Transform sub in t) if (sub.gameObject.activeInHierarchy) finer.Add(Renderers("zone/" + t.name + "/" + sub.name, sub.GetComponentsInChildren<Renderer>()));
                foreach (var s in finer) yield return Run(s);
                suspects.AddRange(finer);

                // The ranking by the RenderTexture figure (and the screen's, for the cameras), the best three saved.
                var ranked = suspects.Where(s => s.rt >= 0).OrderBy(s => s.rt).ToList();
                for (int i = 0; i < Mathf.Min(3, ranked.Count); i++) Save((i + 1).ToString("00") + "-" + Slug(ranked[i].name) + ".png", ranked[i].pixels, W, H);
                var best = ranked.FirstOrDefault();
                var bestScreen = baseScreen >= 0 ? suspects.Where(s => s.screen >= 0).OrderBy(s => s.screen).FirstOrDefault() : null;
                Debug.Log("SKYGHOST RESULT best=" + (best != null ? best.name : "none") + " baseline=" + baseline.ToString("F4") + " after=" + (best != null ? best.rt.ToString("F4") : "-")
                    + " drop=" + (best != null ? (baseline - best.rt).ToString("F4") : "-")
                    + (bestScreen != null ? " | screen best=" + bestScreen.name + " baseline=" + baseScreen.ToString("F4") + " after=" + bestScreen.screen.ToString("F4") : "")
                    + " | top: " + string.Join(", ", ranked.Take(6).Select(s => s.name + " " + s.rt.ToString("F4"))) + " | PNGs in " + OutDir);
            }
            finally
            {
                Time.timeScale = timeScaleWas;
                rt.Release(); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(read);
            }
        }

        static string NameOf(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
    }
}
#endif
