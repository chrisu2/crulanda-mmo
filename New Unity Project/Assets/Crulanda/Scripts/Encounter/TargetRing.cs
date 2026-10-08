using UnityEngine;
using UnityEngine.Rendering;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A ring on the ground under whoever you have selected: red for a living enemy, gold for a body to loot (grey once
    /// looted), green for a
    /// friend (a villager or Mira), with four arrowheads pointing in. It pops when the selection changes and breathes gently
    /// after, so there is never doubt about what Tab, a click, E or your abilities are aimed at.
    /// </summary>
    public sealed class TargetRing : MonoBehaviour
    {
        public EncounterSession session;
        public static readonly Color Hostile = new Color(1, .2f, .14f), Loot = new Color(1, .78f, .25f), Spent = new Color(.62f, .62f, .6f), Friend = new Color(.3f, 1, .42f);
        Transform ring; Material mat; object current; float since;
        /// <summary>What the ring is under right now (null when nothing is selected), and its colour. For tests and the HUD.</summary>
        public object Showing { get { return ring != null && ring.gameObject.activeSelf ? current : null; } }
        public Color Colour { get; private set; }

        void Start()
        {
            var go = new GameObject("Target ring"); ring = go.transform;
            go.AddComponent<MeshFilter>().sharedMesh = RingMesh();
            var r = go.AddComponent<MeshRenderer>(); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            // Sprites/Default (always in builds; vertex colour times _Color): particle shaders soft-fade near the ground.
            mat = new Material(Shader.Find("Sprites/Default")) { name = "Target ring", renderQueue = 3100 };
            r.sharedMaterial = mat; go.SetActive(false);
        }
        void OnDestroy() { if (ring != null) Destroy(ring.gameObject); if (mat != null) Destroy(mat); }

        void LateUpdate()
        {
            if (session == null || ring == null) return;
            // Who is selected: an enemy first (the session clears friends when you pick one), then Mira, then a villager.
            Vector3 feet = Vector3.zero; object who = null; Color c = Friend; float size = .7f;
            var t = session.Target;
            if (t != null && t.actor != null && !t.Hidden) { who = t; feet = t.transform.position - Vector3.up; c = t.actor.IsAlive ? Hostile : session.CanLoot(t) ? Loot : Spent; size = t.Elite ? 1.25f : .95f; }
            else if (session.FocusMira && session.Companion != null) { who = session.Companion; feet = session.Companion.transform.position - Vector3.up; }
            else if (session.FocusVillager != null && session.FocusVillager.Visible)
            {
                // Villagers ride 1 m above their feet (children .66 m), like WorldLife's agents.
                var v = session.FocusVillager; bool child = v.Role == "child"; who = v;
                feet = v.transform.position - Vector3.up * (child ? .66f : 1); if (child) size = .5f;
            }
            ring.gameObject.SetActive(who != null);
            if (who == null) { current = null; return; }
            if (!ReferenceEquals(who, current)) { current = who; since = 0; }
            since += Time.deltaTime; Colour = c;
            // Pop in from wide and bright, then breathe.
            float pop = 1 - Mathf.SmoothStep(0, 1, since / .28f);
            float scale = size * (1 + .7f * pop), alpha = Mathf.Lerp(.72f + .18f * Mathf.Sin(since * 4.5f), 1, pop);
            // Lie along the ground: on open terrain tilt with the slope (sampled a ring-width apart) and lift a little more
            // on steep ground, so no part sinks in; on floors and bridges stay flat.
            var up = Vector3.up; float lift = .07f; var zone = Crulanda.World.ZoneBuilder.Active;
            if (zone != null && Mathf.Abs(zone.HeightAt(feet.x, feet.z) - feet.y) < .35f)
            {
                float r = size, hx = zone.HeightAt(feet.x + r, feet.z) - zone.HeightAt(feet.x - r, feet.z), hz = zone.HeightAt(feet.x, feet.z + r) - zone.HeightAt(feet.x, feet.z - r);
                up = new Vector3(-hx, 2 * r, -hz).normalized; lift += (1 - up.y) * r * 1.5f; feet.y = Mathf.Max(feet.y, zone.HeightAt(feet.x, feet.z));
            }
            ring.position = feet + up * lift;
            ring.rotation = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(0, since * 25, 0);
            ring.localScale = new Vector3(scale, 1, scale);
            mat.color = new Color(c.r, c.g, c.b, alpha);
        }

        /// <summary>A soft ring band (transparent at both edges) plus four inward arrowheads, radius 1, lying flat.</summary>
        internal static Mesh RingMesh()
        {
            const int n = 64; float[] radii = { .76f, .88f, 1 }; float[] alphas = { 0, 1, .15f };
            var v = new System.Collections.Generic.List<Vector3>(); var col = new System.Collections.Generic.List<Color>(); var tri = new System.Collections.Generic.List<int>();
            for (int r = 0; r < 3; r++)
                for (int i = 0; i <= n; i++)
                {
                    float a = i * Mathf.PI * 2 / n; v.Add(new Vector3(Mathf.Cos(a) * radii[r], 0, Mathf.Sin(a) * radii[r])); col.Add(new Color(1, 1, 1, alphas[r]));
                }
            for (int r = 0; r < 2; r++)
                for (int i = 0; i < n; i++) { int a = r * (n + 1) + i, b = a + n + 1; tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI / 2; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var side = new Vector3(-dir.z, 0, dir.x);
                int s = v.Count; v.Add(dir * 1.2f + side * .17f); v.Add(dir * 1.2f - side * .17f); v.Add(dir * .95f);
                col.Add(Color.white); col.Add(Color.white); col.Add(Color.white); tri.AddRange(new[] { s, s + 2, s + 1 });
            }
            var m = new Mesh { name = "Target ring" }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tri, 0); m.RecalculateBounds();
            return m;
        }
    }
}
