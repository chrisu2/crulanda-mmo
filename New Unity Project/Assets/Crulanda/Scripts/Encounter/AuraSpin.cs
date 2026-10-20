using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>An aura's ring (ArchivistKit.Aura): kept round its actor's visible body in world space and turned slowly. Anchored to the
    /// body's meshes, not the actor's root: in the build the root stood metres from the drawn figure (step-4 frames 09, 11-13).</summary>
    public sealed class AuraSpin : MonoBehaviour
    {
        public static readonly List<AuraSpin> All = new List<AuraSpin>();
        public Transform Follow; public float Height = 1;
        float turn; Renderer[] body;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        /// <summary>The figure's drawn meshes (skinned first; else any mesh), without labels, rings or effects.</summary>
        public static Renderer[] BodyOf(Transform who)
        {
            if (who == null) return new Renderer[0];
            var skinned = who.GetComponentsInChildren<SkinnedMeshRenderer>(true).Cast<Renderer>().ToArray();
            if (skinned.Length > 0) return skinned;
            return who.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.GetComponent<TextMesh>() == null && !r.name.Contains("ring") && !r.name.Contains("Ring")).Cast<Renderer>().ToArray();
        }
        /// <summary>Where the ring centres: the body's bounds centre on the ground plane, <paramref name="height"/> above the feet.</summary>
        public static Vector3 Anchor(Transform who, Renderer[] body, float height)
        {
            if (who == null) return Vector3.zero;
            bool any = false; Bounds b = default;
            foreach (var r in body) if (r != null && r.enabled && r.gameObject.activeInHierarchy) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (!any) return who.position + Vector3.up * height;
            return new Vector3(b.center.x, b.min.y + height, b.center.z);
        }
        public Vector3 BodyPoint { get { return Anchor(Follow, body ?? (body = BodyOf(Follow)), 0); } }
        public void Place() { if (Follow == null) return; if (body == null) body = BodyOf(Follow); transform.position = Anchor(Follow, body, Height); transform.rotation = Quaternion.Euler(0, turn, 0); }
        void LateUpdate() { if (Follow == null) { Destroy(gameObject); return; } turn += 60 * Time.deltaTime; Place(); }
    }
}