using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Crulanda.World
{
    /// <summary>
    /// Feet in the grass: tells Grass.shader where up to eight walkers stand (_GrassBenders, a global array), so the tufts part
    /// round them. From the "Interactive Grass" Shader Graph Chris downloaded (2026-10-15 01:20: "... interactive grass ... use
    /// these where appropriate"): that pushed a tuft away from one character's position fed by a script; this feeds the same
    /// push for whoever is near. GrassField adds one of these. Who bends the grass: the player (the one CharacterController),
    /// then the nearest walkers on the navmesh (Mira, villagers, animals, mobs), found again every half second so a crowd never
    /// costs a search a frame; anything else registers itself (<see cref="Register"/>). The reach is a stride: a tuft within a
    /// third of it lies flat, one at the reach is untouched (Grass.shader).
    /// </summary>
    public sealed class GrassBend : MonoBehaviour
    {
        public const int Slots = 8;
        public const float Reach = .9f, Rescan = .5f;
        static readonly List<Transform> registered = new List<Transform>();
        static readonly Vector4[] slots = new Vector4[Slots];
        static readonly int BendersId = Shader.PropertyToID("_GrassBenders"), CountId = Shader.PropertyToID("_GrassBenderCount");
        readonly List<Transform> near = new List<Transform>(), found = new List<Transform>(); readonly List<Vector3> at = new List<Vector3>();
        readonly int[] chosen = new int[Slots];
        float nextScan;

        /// <summary>Something that should part the grass and is neither a CharacterController nor a NavMeshAgent (a cart, a pet).</summary>
        public static void Register(Transform t) { if (t != null && !registered.Contains(t)) registered.Add(t); }
        public static void Unregister(Transform t) { registered.Remove(t); }

        void LateUpdate()
        {
            if (Time.time >= nextScan) { nextScan = Time.time + Rescan; Scan(); }
            int n = 0;
            for (int i = 0; i < near.Count && n < Slots; i++)
            {
                var t = near[i]; if (t == null || !t.gameObject.activeInHierarchy) continue;
                var p = t.position; slots[n++] = new Vector4(p.x, p.y, p.z, Reach);
            }
            for (int i = n; i < Slots; i++) slots[i] = Vector4.zero;
            Shader.SetGlobalVectorArray(BendersId, slots); Shader.SetGlobalFloat(CountId, n);
        }
        void OnDisable() { Shader.SetGlobalFloat(CountId, 0); }

        /// <summary>The walkers to feed: the player first, whatever the distance, then the nearest of the rest to the camera.</summary>
        void Scan()
        {
            found.Clear(); at.Clear();
            foreach (var c in FindObjectsByType<CharacterController>(FindObjectsSortMode.None)) found.Add(c.transform);
            int keep = found.Count;
            foreach (var t in registered) if (t != null) found.Add(t);
            foreach (var a in FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None)) found.Add(a.transform);
            var cam = Camera.main; var from = cam != null ? cam.transform.position : found.Count > 0 ? found[0].position : Vector3.zero;
            foreach (var t in found) at.Add(t.position);
            int n = Choose(at, from, keep, chosen);
            near.Clear(); for (int i = 0; i < n; i++) near.Add(found[chosen[i]]);
        }
        /// <summary>
        /// Which of <paramref name="at"/> get a slot (their indices into <paramref name="chosen"/>, whose length is the most): the first
        /// <paramref name="keep"/> whatever their distance from <paramref name="from"/>, then the nearest of the rest. Returns how many.
        /// </summary>
        public static int Choose(IReadOnlyList<Vector3> at, Vector3 from, int keep, int[] chosen)
        {
            int n = 0, max = chosen.Length;
            var taken = new bool[at.Count];
            for (int i = 0; i < at.Count && i < keep && n < max; i++) { chosen[n++] = i; taken[i] = true; }
            while (n < max)   // the nearest of the rest, one at a time: a handful of slots, no sort needed
            {
                int best = -1; float bestD = float.MaxValue;
                for (int i = 0; i < at.Count; i++) { if (taken[i]) continue; float d = (at[i] - from).sqrMagnitude; if (d < bestD) { bestD = d; best = i; } }
                if (best < 0) break;
                taken[best] = true; chosen[n++] = best;
            }
            return n;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { registered.Clear(); }
    }
}
