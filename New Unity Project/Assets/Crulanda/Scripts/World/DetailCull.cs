using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Small detail not drawn far off (2026-10-17, Chris at /crown: "still laggy"; the Crown at 64 fps at noon on the capture rig, 24.7 M
    /// triangles a frame, 13.6 M before the ivy was made sheets). The Crown's ivy, leaves, blossoms, ironwork and weathering
    /// (ZoneBuilder.CrownDetail, cut into cells of <see cref="ZoneBuilder.DetailCell"/> metres) and the gardens' plants each have a
    /// sphere and a band: past <see cref="Bands"/>[band] metres from the camera (to the sphere's edge) the renderer is set
    /// forceRenderingOff, apart from the game's own hiding. Distances in metres, not screen size, so the quality level's LOD bias (0.3
    /// to 2) does not move them. A share of the cells is measured each frame from the main camera (all of them in a quarter second);
    /// 3 m of give either way so a cell does not flicker on its line. (A CullingGroup did this first and hid nothing in the tests:
    /// its bands are only worked out when its camera renders.) GAME-ONLY.
    /// </summary>
    public sealed class DetailCull : MonoBehaviour
    {
        /// <summary>The bands: 0 the small things (ivy, leaves, flowers, iron, plants), 1 the weathering laid over the walls.</summary>
        public static readonly float[] Bands = { 70, 130 };
        const float Give = 3;
        Renderer[] renderers; int[] bands; Vector3[] centres; float[] radii; int next;
        public int Count { get { return renderers != null ? renderers.Length : 0; } }
        public int Off { get; private set; }
        public void Init(List<(Renderer r, int band)> list)
        {
            var keep = list.FindAll(x => x.r != null);
            renderers = new Renderer[keep.Count]; bands = new int[keep.Count]; centres = new Vector3[keep.Count]; radii = new float[keep.Count];
            for (int i = 0; i < keep.Count; i++)
            {
                renderers[i] = keep[i].r; bands[i] = Mathf.Clamp(keep[i].band, 0, Bands.Length - 1);
                var b = keep[i].r.bounds; centres[i] = b.center; radii[i] = b.extents.magnitude;
            }
            Off = 0;
            var cam = Camera.main; if (cam != null) Refresh(cam.transform.position);
        }
        /// <summary>Every cell measured now from <paramref name="eye"/> (the tests; Init).</summary>
        public void Refresh(Vector3 eye) { for (int i = 0; i < Count; i++) Apply(i, eye); }
        void Apply(int i, Vector3 eye)
        {
            var r = renderers[i]; if (r == null) return;
            float d = Vector3.Distance(eye, centres[i]) - radii[i], reach = Bands[bands[i]];
            bool far = r.forceRenderingOff ? d > reach - Give : d > reach + Give;
            if (r.forceRenderingOff != far) { r.forceRenderingOff = far; Off += far ? 1 : -1; }
        }
        void Update()
        {
            var cam = Camera.main; if (cam == null || Count == 0) return;
            var eye = cam.transform.position;
            int n = Mathf.Max(64, Mathf.CeilToInt(Count / 15f));
            for (int k = 0; k < n && k < Count; k++) { next = (next + 1) % Count; Apply(next, eye); }
        }
    }
}
