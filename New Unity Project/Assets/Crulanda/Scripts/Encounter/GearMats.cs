using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Materials for worn gear, shared: one Standard material per colour, smoothness, metalness and glow (each rounded a little),
    /// so a hundred swaps make no new materials. Epic accents pulse through a property block on the renderer (GearGlow), never by
    /// editing the shared material.
    /// </summary>
    public static class GearMats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        /// <summary>How many shared gear materials exist (the cache test watches this stay flat over many swaps).</summary>
        public static int Count { get { return cache.Count; } }
        static int Q(float x) { return Mathf.RoundToInt(x * 255); }

        /// <summary>A solid material; an emission above black lights it (rare and epic accents, lantern cores). Emission may run well
        /// above 1 (an epic accent burns at five times its colour), so the bloom takes it.</summary>
        public static Material Get(Color c, float smooth = .15f, float metal = 0, Color emission = default)
        {
            string key = Q(c.r) + "," + Q(c.g) + "," + Q(c.b) + "|" + Mathf.RoundToInt(smooth * 100) + "|" + Mathf.RoundToInt(metal * 100) + "|" + Q(emission.r / 4) + "," + Q(emission.g / 4) + "," + Q(emission.b / 4);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Shader.Find("Standard")) { color = new Color(c.r, c.g, c.b, 1), name = "Gear " + key, hideFlags = HideFlags.DontUnloadUnusedAsset };
            m.SetFloat("_Glossiness", smooth); m.SetFloat("_Metallic", metal);
            if (emission.maxColorComponent > .001f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission); }
            cache[key] = m; return m;
        }
        /// <summary>See-through glowing glass (the Standard shader's Fade mode, as the Weave-Eaters' veils use): shards, panes, lantern glass.</summary>
        public static Material Glass(Color c, float alpha, Color emission)
        {
            string key = "glass:" + Q(c.r) + "," + Q(c.g) + "," + Q(c.b) + "," + Q(alpha) + "|" + Q(emission.r / 4) + "," + Q(emission.g / 4) + "," + Q(emission.b / 4);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Shader.Find("Standard")) { color = new Color(c.r, c.g, c.b, alpha), name = "Gear " + key, hideFlags = HideFlags.DontUnloadUnusedAsset };
            m.SetFloat("_Glossiness", .9f); m.SetFloat("_Metallic", .1f);
            m.SetFloat("_Mode", 2); m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission);   // always: the Fade-and-emission variant ships (the Weave-Eaters use it)
            cache[key] = m; return m;
        }
    }
}
