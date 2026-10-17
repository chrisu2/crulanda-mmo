using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Small detail not drawn far off (2026-10-17, Chris at /crown: "still laggy"; the Crown at 64 fps at noon on the capture rig, 24.7 M
    /// triangles a frame, 13.6 M before the ivy was made sheets). The Crown's ivy, leaves, blossoms, ironwork and weathering
    /// (ZoneBuilder.CrownDetail, cut into cells of <see cref="ZoneBuilder.DetailCell"/> metres) and the gardens' plants each have a
    /// sphere in a CullingGroup and a band: past <see cref="Bands"/>[band] metres from the camera (to the sphere's edge) the renderer
    /// is set forceRenderingOff, the flag PropCull uses, apart from the game's own hiding. Distances in metres, not screen size, so the
    /// quality level's LOD bias (0.3 to 2) does not move them. GAME-ONLY.
    /// </summary>
    public sealed class DetailCull : MonoBehaviour
    {
        /// <summary>The bands: 0 the small things (ivy, leaves, flowers, iron, plants), 1 the weathering laid over the walls.</summary>
        public static readonly float[] Bands = { 70, 130 };
        CullingGroup group; Renderer[] renderers; int[] bands; BoundingSphere[] spheres;
        public int Count { get { return renderers != null ? renderers.Length : 0; } }
        public int Off { get; private set; }
        public void Init(List<(Renderer r, int band)> list)
        {
            var keep = list.FindAll(x => x.r != null);
            renderers = new Renderer[keep.Count]; bands = new int[keep.Count]; spheres = new BoundingSphere[keep.Count];
            for (int i = 0; i < keep.Count; i++)
            {
                renderers[i] = keep[i].r; bands[i] = Mathf.Clamp(keep[i].band, 0, Bands.Length - 1);
                var b = keep[i].r.bounds; spheres[i] = new BoundingSphere(b.center, b.extents.magnitude);
            }
            group = new CullingGroup(); group.SetBoundingSpheres(spheres); group.SetBoundingSphereCount(spheres.Length);
            group.SetBoundingDistances(Bands);
            group.onStateChanged = Changed;
            Off = 0; Follow();
        }
        void Follow()
        {
            var cam = Camera.main; if (cam == null || group == null) return;
            group.targetCamera = cam; group.SetDistanceReferencePoint(cam.transform);
        }
        void Changed(CullingGroupEvent e)
        {
            var r = renderers[e.index]; if (r == null) return;
            // The band index CullingGroup reports is the first distance the sphere is within (Bands.Length: past them all).
            bool far = e.currentDistance > bands[e.index];
            if (r.forceRenderingOff != far) { r.forceRenderingOff = far; Off += far ? 1 : -1; }
        }
        void LateUpdate() { if (group != null && group.targetCamera != Camera.main) Follow(); }
        void OnDestroy() { if (group != null) { group.Dispose(); group = null; } }
    }
}
