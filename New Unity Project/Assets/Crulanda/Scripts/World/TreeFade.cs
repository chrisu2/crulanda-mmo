using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A tree that fades to see-through while it stands between the camera and the player, and comes back once the view is
    /// clear. ZoneBuilder puts one on every tree root; the player's camera calls <see cref="UpdateAll"/> each frame. Trees
    /// never pull the camera in (the camera skips colliders under a TreeFade): it looks through them instead.
    /// Fading swaps the tree's renderers onto shared Crulanda/Fade copies of their materials (one per original material,
    /// same Standard lighting, a screen-door dither for the see-through part) and sets the alpha per renderer with a
    /// property block, so static batching and other trees are untouched and there is no jump in brightness.
    /// </summary>
    public sealed class TreeFade : MonoBehaviour
    {
        public static readonly List<TreeFade> All = new List<TreeFade>();
        /// <summary>The Crulanda/Fade material (ZoneArt.fade). Without it trees stay solid.</summary>
        public static Material FadeTemplate { get; private set; }
        /// <summary>A zone is being built: use its fade material and forget the last zone's faded copies.</summary>
        public static void Begin(Material template) { FadeTemplate = template; fadeCopies.Clear(); }
        /// <summary>How much of a faded tree still shows.</summary>
        public const float FadedAlpha = .22f;
        /// <summary>A blocking part this close to the camera (a trunk right in front of the lens) is hidden outright, not dithered.</summary>
        public const float NearHide = 3.5f;
        static readonly Dictionary<Material, Material> fadeCopies = new Dictionary<Material, Material>();
        static MaterialPropertyBlock block;

        Renderer[] parts; Material[][] solid; Bounds bounds; Bounds[] partBounds; bool ready;
        float alpha = 1, target = 1;
        /// <summary>True while the tree is (or is turning) see-through.</summary>
        public bool Faded { get { return alpha < .999f; } }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); Restore(); }

        /// <summary>Renderers and their bounds, read once the zone is built (after static batching).</summary>
        void Prepare()
        {
            ready = true; parts = GetComponentsInChildren<Renderer>();
            solid = new Material[parts.Length][]; partBounds = new Bounds[parts.Length];
            bounds = new Bounds(transform.position + Vector3.up * 3, Vector3.one);
            for (int i = 0; i < parts.Length; i++) { solid[i] = parts[i].sharedMaterials; partBounds[i] = parts[i].bounds; partBounds[i].Expand(.2f); bounds.Encapsulate(parts[i].bounds); }
        }

        /// <summary>
        /// Fades every tree that blocks the line from the camera to the player's head or body, or that the camera sits
        /// inside, and brings back the rest. Call once per frame after the camera has moved.
        /// </summary>
        public static void UpdateAll(Vector3 camera, Vector3 head, Vector3 body)
        {
            if (FadeTemplate == null) return;
            float dt = Mathf.Min(Time.deltaTime, .1f);
            foreach (var t in All)
            {
                if (!t.ready) t.Prepare();
                // In front of you: see-through. Around or right in front of the camera (inside a crown, a trunk at the lens):
                // gone, shadow kept.
                int hides = t.Hides(camera, head, body); t.target = hides == 2 ? 0 : hides == 1 ? FadedAlpha : 1;
                if (Mathf.Approximately(t.alpha, t.target)) continue;
                t.alpha = Mathf.MoveTowards(t.alpha, t.target, dt * 4);   // about a quarter of a second either way
                t.Apply();
            }
        }
        /// <summary>
        /// Whether this tree really hides the player: 0 no, 1 a part (the trunk, a leaf clump or bough tier, a limb) cuts
        /// the line from the camera to the head or body before the player, 2 the camera is inside a part. The whole-tree box
        /// is only a quick first check; standing under a crown, with the camera clear of it, doesn't fade the tree.
        /// </summary>
        int Hides(Vector3 camera, Vector3 head, Vector3 body)
        {
            var whole = bounds; whole.Expand(.3f);
            if (!whole.Contains(camera) && !Blocks(whole, camera, head) && !Blocks(whole, camera, body)) return 0;
            int result = 0;
            foreach (var b in partBounds)
            {
                if (b.Contains(camera)) return 2;
                // Where the line to the head or body first enters this part (a part the player stands in doesn't count).
                float eh = b.Contains(head) ? -1 : Entry(b, camera, head), eb = b.Contains(body) ? -1 : Entry(b, camera, body);
                float at = eh < 0 ? eb : eb < 0 ? eh : Mathf.Min(eh, eb);
                if (at >= 0) { if (at < NearHide) return 2; result = 1; }
            }
            return result;
        }
        static bool Blocks(Bounds b, Vector3 from, Vector3 to) { return Entry(b, from, to) >= 0; }
        /// <summary>How far from the camera the line to the player enters this box (-1 if it doesn't, or only at the player).</summary>
        static float Entry(Bounds b, Vector3 from, Vector3 to)
        {
            var d = to - from; float length = d.magnitude; if (length < .01f) return -1;
            return b.IntersectRay(new Ray(from, d / length), out float hit) && hit < length - .5f ? hit : -1;
        }

        void Apply()
        {
            if (alpha >= .999f) { Restore(); return; }
            if (block == null) block = new MaterialPropertyBlock();
            for (int i = 0; i < parts.Length; i++)
            {
                var r = parts[i]; if (r == null) continue;
                var mats = solid[i]; var faded = new Material[mats.Length];
                for (int m = 0; m < mats.Length; m++) faded[m] = FadeOf(mats[m]);
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
                parts[i].sharedMaterials = solid[i];
                parts[i].SetPropertyBlock(null);
            }
            alpha = target = 1;
        }
        static Material FadeOf(Material solidMat)
        {
            if (solidMat == null) return null;
            if (!fadeCopies.TryGetValue(solidMat, out var f) || f == null)
            {
                f = new Material(FadeTemplate) { name = solidMat.name + " (fade)" };
                if (solidMat.HasProperty("_MainTex")) f.mainTexture = solidMat.mainTexture;
                if (solidMat.HasProperty("_Color")) f.color = solidMat.color;
                if (solidMat.HasProperty("_Glossiness")) f.SetFloat("_Glossiness", solidMat.GetFloat("_Glossiness"));
                // Painted leaf cards (Crulanda/Leaf): their cutout, both faces, wind and painted shade carry over (the fade shader
                // reads the same properties), so a faded crown keeps its ragged shape and keeps swaying. Solid parts stay as they were.
                if (solidMat.shader != null && solidMat.shader.name == "Crulanda/Leaf")
                    foreach (var p in new[] { "_Cutoff", "_Cull", "_Wither", "_VertexTint", "_Wind", "_WindSpeed" })
                        if (solidMat.HasProperty(p) && f.HasProperty(p)) f.SetFloat(p, solidMat.GetFloat(p));
                fadeCopies[solidMat] = f;
            }
            return f;
        }
    }
}
