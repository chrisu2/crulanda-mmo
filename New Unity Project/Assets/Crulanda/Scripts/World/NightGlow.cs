using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Materials that glow only after dark (the Lowtowns' neon moss, play note 257, Chris 2026-10-19: "Make it look like moss"): each keeps
    /// its night glow and is set to a faint trace of it by day, rising through dusk with <see cref="WorldClock.Darkness"/> to the full glow at
    /// night. One on the zone's builder; the materials are shared, so a thousand patches cost one update.
    /// </summary>
    public sealed class NightGlow : MonoBehaviour
    {
        /// <summary>How much of the night glow is left by day (a trace: the moss reads dull green in daylight).</summary>
        public const float DayShare = .04f;
        readonly List<(Material mat, Color glow)> mats = new List<(Material, Color)>();
        float last = -1;
        public void Add(Material m, Color glow) { mats.Add((m, glow)); Set(m, glow, Level); }
        public static float Level { get { return Mathf.Lerp(DayShare, 1, WorldClock.Darkness); } }
        static void Set(Material m, Color glow, float k) { if (m != null && m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", glow * k); }
        void Update()
        {
            float k = Level; if (Mathf.Abs(k - last) < .01f) return;
            last = k; foreach (var (m, g) in mats) Set(m, g, k);
        }
    }
}
