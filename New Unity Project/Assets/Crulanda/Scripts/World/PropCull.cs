using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Far props drawn by none (Chris, 2026-10-15: "whole zone needs optimized laggy"; the Adit's probe: its 15,857 prop renderers, 56
    /// million triangles and 82,000 draw passes at the yard cost some 38 ms of a 79 ms frame, the whole dungeon drawn behind the hill).
    /// In a zone with caves every renderer under the zone's props has a sphere in a CullingGroup; beyond <see cref="Reach"/> metres of the
    /// camera (to the sphere's edge) it is set forceRenderingOff, a flag apart from the game's own hiding (a picked crate stays hidden). In
    /// a cave nothing is seen that far; from the yard the caves' insides are behind rock.
    /// </summary>
    public sealed class PropCull : MonoBehaviour
    {
        public const float Reach = 70;
        CullingGroup group; Renderer[] renderers; BoundingSphere[] spheres;
        public int Count { get { return renderers != null ? renderers.Length : 0; } }
        public int Off { get; private set; }
        public void Init(Transform props)
        {
            var list = new List<Renderer>(props.GetComponentsInChildren<Renderer>(true));
            list.RemoveAll(r => r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer);
            // Never the caves themselves (a shell, knoll, floor or lining, one mesh a cave) nor anything big: a far cave's shell not drawn
            // left its neighbour's mouth open on the sky (the 14:36 tour: white panes at the Black Cove and the Foundry's far end).
            list.RemoveAll(r => { var n = r.gameObject.name; return n.EndsWith(" shell") || n.EndsWith(" knoll") || n.EndsWith(" floor") || n.EndsWith(" lining") || r.bounds.extents.magnitude > 20; });
            renderers = list.ToArray(); spheres = new BoundingSphere[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) { var b = renderers[i].bounds; spheres[i] = new BoundingSphere(b.center, b.extents.magnitude); }
            group = new CullingGroup(); group.SetBoundingSpheres(spheres); group.SetBoundingSphereCount(spheres.Length);
            group.SetBoundingDistances(new[] { Reach });
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
            bool far = e.currentDistance >= 1;
            if (r.forceRenderingOff != far) { r.forceRenderingOff = far; Off += far ? 1 : -1; }
        }
        void LateUpdate() { if (group != null && group.targetCamera != Camera.main) Follow(); }
        void OnDestroy() { if (group != null) { group.Dispose(); group = null; } }
    }
}
