using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A building that comes between the camera and the player turns see-through, as a tree does (TreeFade, whose fade materials
    /// it shares): any part of it (a roof, a pent roof, a wall, a chimney) that the line from the camera to the player's head or
    /// body passes through fades, and the whole building with it, so the player under the smithy's eaves or inside the Golden
    /// Cask is never lost behind a roof (playtest note 26). Only buildings carry it (ZoneBuilder.BuildProps: anything with a
    /// footprint); trees keep TreeFade, and nothing that treats a TreeFade as a tree sees these.
    /// </summary>
    public sealed class RoofFade : MonoBehaviour
    {
        public static readonly List<RoofFade> All = new List<RoofFade>();
        /// <summary>How much of a faded building still shows.</summary>
        public const float FadedAlpha = .18f;
        static MaterialPropertyBlock block;

        Renderer[] parts; Material[][] solid; Bounds bounds; Bounds[] partBounds; bool ready;
        float alpha = 1, target = 1;
        public bool Faded { get { return alpha < .999f; } }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); Restore(); }

        void Prepare()
        {
            ready = true; parts = GetComponentsInChildren<Renderer>();
            solid = new Material[parts.Length][]; partBounds = new Bounds[parts.Length];
            bounds = new Bounds(transform.position, Vector3.one);
            // Only the big parts (roofs, walls, chimneys) count as blocking; a table or a lamp inside the inn between the camera and
            // the player does not fade the building (its box stays empty: it never blocks).
            for (int i = 0; i < parts.Length; i++)
            {
                solid[i] = parts[i].sharedMaterials; var pb = parts[i].bounds; bounds.Encapsulate(pb);
                partBounds[i] = Mathf.Max(pb.size.x, Mathf.Max(pb.size.y, pb.size.z)) >= 1.8f ? pb : new Bounds(new Vector3(0, -9999, 0), Vector3.zero);
            }
        }

        /// <summary>Fades every building that blocks the camera's view of the player and brings back the rest. Once a frame,
        /// after the camera has moved (AdventurerMotor, with TreeFade.UpdateAll).</summary>
        public static void UpdateAll(Vector3 camera, Vector3 head, Vector3 body)
        {
            if (TreeFade.FadeTemplate == null) return;
            float dt = Mathf.Min(Time.deltaTime, .1f);
            foreach (var r in All)
            {
                if (!r.ready) r.Prepare();
                r.target = r.Hides(camera, head, body) ? FadedAlpha : 1;
                if (Mathf.Approximately(r.alpha, r.target)) continue;
                r.alpha = Mathf.MoveTowards(r.alpha, r.target, dt * 5);
                r.Apply();
            }
        }
        bool Hides(Vector3 camera, Vector3 head, Vector3 body)
        {
            var whole = bounds; whole.Expand(.3f);
            if (!whole.Contains(camera) && Entry(whole, camera, head) < 0 && Entry(whole, camera, body) < 0) return false;
            foreach (var b in partBounds)
            {
                if (b.Contains(camera)) return true;
                if ((!b.Contains(head) && Entry(b, camera, head) >= 0) || (!b.Contains(body) && Entry(b, camera, body) >= 0)) return true;
            }
            return false;
        }
        static float Entry(Bounds b, Vector3 from, Vector3 to)
        {
            var d = to - from; float length = d.magnitude; if (length < .01f) return -1;
            return b.IntersectRay(new Ray(from, d / length), out float hit) && hit < length - .4f ? hit : -1;
        }
        void Apply()
        {
            if (alpha >= .999f) { Restore(); return; }
            if (block == null) block = new MaterialPropertyBlock();
            for (int i = 0; i < parts.Length; i++)
            {
                var r = parts[i]; if (r == null) continue;
                var mats = solid[i]; var faded = new Material[mats.Length];
                for (int m = 0; m < mats.Length; m++) faded[m] = TreeFade.FadeOf(mats[m]);
                r.sharedMaterials = faded;
                var tint = mats.Length > 0 && mats[0] != null && mats[0].HasProperty("_Color") ? mats[0].color : Color.white;
                r.GetPropertyBlock(block); tint.a = alpha; block.SetColor("_Color", tint); r.SetPropertyBlock(block);
            }
        }
        void Restore()
        {
            if (parts == null) return;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null) continue;
                parts[i].sharedMaterials = solid[i]; parts[i].SetPropertyBlock(null);
            }
            alpha = target = 1;
        }
    }
}
