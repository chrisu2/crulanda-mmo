using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Crowd control you can see (Docs/CC_DESIGN.md section 4, step C4, 2026-10-08): a ring on the ground under every mob that is held
    /// (pale blue), stunned (gold), fleeing (violet) or silenced (white), turning slowly and fading in its last three seconds, so a pull
    /// shows at a glance which mobs are out of the fight. The nameplate says the state and its seconds (EncounterHud.DrawPlate).
    /// </summary>
    public sealed class ControlRings : MonoBehaviour
    {
        public EncounterSession session;
        public static readonly Color Held = new Color(.55f, .78f, 1), Stun = new Color(1, .85f, .3f), Fear = new Color(.78f, .5f, 1), Hush = new Color(.92f, .92f, .95f);
        readonly List<(Transform t, Material m)> pool = new List<(Transform, Material)>();
        Mesh mesh;
        /// <summary>How many rings are on the ground now (for tests).</summary>
        public int Showing { get; private set; }

        public static Color ColourFor(EncounterEnemy e) { return e.Incapacitated ? Held : e.Stunned ? Stun : e.Feared ? Fear : Hush; }

        void LateUpdate()
        {
            if (session == null) return;
            int n = 0; var zone = Crulanda.World.ZoneBuilder.Active;
            foreach (var e in session.Enemies)
            {
                if (e == null || e.actor == null || !e.actor.IsAlive || e.Hidden || e.ControlLabel == null) continue;
                var (t, m) = Ring(n++);
                var feet = e.transform.position - Vector3.up;
                if (zone != null && Mathf.Abs(zone.HeightAt(feet.x, feet.z) - feet.y) < .35f) feet.y = Mathf.Max(feet.y, zone.HeightAt(feet.x, feet.z));
                float size = e.Elite ? 1.1f : .75f, left = e.ControlSecondsLeft;
                t.position = feet + Vector3.up * .06f; t.rotation = Quaternion.Euler(0, Time.time * 40, 0); t.localScale = new Vector3(size, 1, size);
                var c = ColourFor(e); m.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(left / 3f) * (.6f + .2f * Mathf.Sin(Time.time * 3)));
                t.gameObject.SetActive(true);
            }
            for (int i = n; i < pool.Count; i++) pool[i].t.gameObject.SetActive(false);
            Showing = n;
        }
        (Transform, Material) Ring(int i)
        {
            while (pool.Count <= i)
            {
                if (mesh == null) mesh = TargetRing.RingMesh();
                var go = new GameObject("Control ring"); go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                var mat = new Material(Shader.Find("Sprites/Default")) { name = "Control ring", renderQueue = 3099 };
                r.sharedMaterial = mat; go.SetActive(false); pool.Add((go.transform, mat));
            }
            return pool[i];
        }
        void OnDestroy() { foreach (var p in pool) { if (p.t != null) Destroy(p.t.gameObject); if (p.m != null) Destroy(p.m); } }
    }
}
