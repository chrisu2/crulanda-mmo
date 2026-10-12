using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Framing for the capture tools (2026-10-11, Chris on the step-2 frames: "can't see the farmers", "a tree blocks the view", "my
    /// character isn't visible"): a camera of the capture's own that no game script moves, a clear line to the subject (a spot on
    /// the circle round it with nothing between, else the things in the way hidden for the shot only), and the hidden things shown
    /// again after.
    /// </summary>
    public static class CaptureFraming
    {
        static Camera own;
        /// <summary>The capture's own camera, drawn over the game's (depth 50), with the game camera's look (sky, fog, clip planes).</summary>
        public static Camera Own()
        {
            if (own != null) { own.enabled = true; return own; }
            var main = Camera.main;
            var go = new GameObject("Capture camera"); own = go.AddComponent<Camera>();
            if (main != null) { own.CopyFrom(main); }
            own.depth = 50; own.fieldOfView = 50; own.nearClipPlane = .1f; own.enabled = true;
            return own;
        }
        /// <summary>The capture camera off again (the game's camera and the HUD show as before).</summary>
        public static void Release() { if (own != null) own.enabled = false; }

        /// <summary>Whether anything solid or drawn (not <paramref name="subject"/>, not the ground) lies between the two points.</summary>
        public static List<Renderer> InTheWay(Vector3 from, Vector3 to, Transform subject, Transform alsoClear = null)
        {
            var list = new List<Renderer>(); var dir = to - from; float len = dir.magnitude; if (len < .01f) return list; dir /= len;
            var ray = new Ray(from, dir);
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (subject != null && r.transform.IsChildOf(subject)) continue;
                if (alsoClear != null && r.transform.IsChildOf(alsoClear)) continue;
                if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                var b = r.bounds; if (b.size.x > 40 || b.size.z > 40) continue;   // the ground, water, sky: never in the way
                b.Expand(.1f);
                if (b.Contains(from)) { list.Add(r); continue; }
                if (b.IntersectRay(ray, out float at) && at < len - .5f) list.Add(r);
            }
            return list;
        }
        /// <summary>A spot <paramref name="distance"/> from the subject at <paramref name="height"/>, starting from the bearing
        /// <paramref name="yaw"/> and turning up to <paramref name="search"/> degrees either way until the line to the subject is clear.
        /// Returns the spot; <paramref name="blocked"/> lists what is still in the way there (hide it for the shot).</summary>
        public static Vector3 ClearSpot(Vector3 subjectAt, float yaw, float distance, float height, float search, Transform subject, out List<Renderer> blocked, Transform alsoClear = null)
        {
            Vector3 best = default; blocked = null; var target = subjectAt + Vector3.up * 1.1f;
            for (int k = 0; k <= 12; k++)
            {
                float turn = k == 0 ? 0 : ((k + 1) / 2) * (search / 6) * (k % 2 == 0 ? -1 : 1);
                var dir = Quaternion.Euler(0, yaw + turn, 0) * Vector3.forward;
                var at = subjectAt + dir * distance + Vector3.up * height;
                var zone = Crulanda.World.ZoneBuilder.Active; if (zone != null) at.y = Mathf.Max(at.y, zone.HeightAt(at.x, at.z) + .6f);
                var inWay = InTheWay(at, target, subject, alsoClear);
                if (blocked == null || inWay.Count < blocked.Count) { best = at; blocked = inWay; }
                if (inWay.Count == 0) break;
            }
            return best;
        }
        /// <summary>Hides the renderers for a shot; Show puts them back.</summary>
        public static void Hide(List<Renderer> list) { if (list != null) foreach (var r in list) if (r != null) r.enabled = false; }
        public static void Show(List<Renderer> list) { if (list != null) foreach (var r in list) if (r != null) r.enabled = true; }
    }
}