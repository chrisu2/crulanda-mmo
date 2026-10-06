using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A spell or an arrow in flight (Round 22, 2026-10-05; Chris: "no animations on firebolt/scorch. shoot out fire?"): a small
    /// glowing ball with a trail and a point light (or a plain arrow shaft) that flies from the caster's hand to the target's chest
    /// in about a quarter second along a shallow arc, then bursts into a few embers. Cosmetic only: the blow itself lands when the
    /// ability resolves (EncounterSession.Bolt). Built from primitives and Sprites/Default like the loot beacons, so there is
    /// nothing to import.
    /// </summary>
    public sealed class Bolt : MonoBehaviour
    {
        Transform target; Vector3 from, lastTo; float t0, seconds, size; Color colour; bool arrow; Transform body; Light glow;
        static Material unlit;
        static Material Unlit { get { if (unlit == null) unlit = new Material(Shader.Find("Sprites/Default")) { name = "Bolt", renderQueue = 3150 }; return unlit; } }

        public static Bolt Fire(Vector3 from, Transform target, Color colour, float size = .18f, float seconds = .28f, bool arrow = false)
        {
            if (target == null) return null;
            var go = new GameObject(arrow ? "Arrow" : "Bolt");
            var b = go.AddComponent<Bolt>();
            b.target = target; b.from = from; b.lastTo = Aim(target); b.t0 = Time.time; b.seconds = Mathf.Max(.08f, seconds); b.size = size; b.colour = colour; b.arrow = arrow;
            go.transform.position = from;
            if (arrow)
            {
                var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder); shaft.name = "Shaft"; Object.Destroy(shaft.GetComponent<Collider>());
                shaft.transform.SetParent(go.transform, false); shaft.transform.localScale = new Vector3(.022f, .32f, .022f); shaft.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var r = shaft.GetComponent<MeshRenderer>(); r.sharedMaterial = Tinted(new Color(.42f, .3f, .18f)); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere); head.name = "Head"; Object.Destroy(head.GetComponent<Collider>());
                head.transform.SetParent(go.transform, false); head.transform.localPosition = new Vector3(0, 0, .32f); head.transform.localScale = Vector3.one * .035f;
                var hr = head.GetComponent<MeshRenderer>(); hr.sharedMaterial = Tinted(new Color(.75f, .78f, .82f)); hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                b.body = go.transform;
            }
            else
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere); ball.name = "Ball"; Object.Destroy(ball.GetComponent<Collider>());
                ball.transform.SetParent(go.transform, false); ball.transform.localScale = Vector3.one * size;
                var r = ball.GetComponent<MeshRenderer>(); r.sharedMaterial = Tinted(Color.Lerp(colour, Color.white, .35f)); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                var trail = go.AddComponent<TrailRenderer>(); trail.sharedMaterial = Tinted(colour); trail.time = .22f; trail.startWidth = size * 1.1f; trail.endWidth = 0; trail.minVertexDistance = .05f;
                trail.startColor = colour; trail.endColor = new Color(colour.r, colour.g, colour.b, 0); trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; trail.receiveShadows = false;
                b.glow = go.AddComponent<Light>(); b.glow.type = LightType.Point; b.glow.color = colour; b.glow.range = 2.5f + size * 6; b.glow.intensity = 1.6f; b.glow.shadows = LightShadows.None;
                b.body = ball.transform;
            }
            return b;
        }
        static Vector3 Aim(Transform t) { return t.position + Vector3.up * 1.1f; }
        static Material Tinted(Color c) { var m = new Material(Unlit) { color = c }; return m; }

        void Update()
        {
            if (target != null) lastTo = Aim(target);
            float u = Mathf.Clamp01((Time.time - t0) / seconds);
            var flat = Vector3.Lerp(from, lastTo, u);
            var pos = flat + Vector3.up * Mathf.Sin(u * Mathf.PI) * (arrow ? .35f : .5f);
            var dir = pos - transform.position;
            transform.position = pos;
            if (arrow && dir.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(dir);
            if (u >= 1) { if (!arrow) Burst(lastTo, colour, size); Destroy(gameObject); }
        }

        /// <summary>A few embers thrown out from the point of impact, fading over a third of a second.</summary>
        public static void Burst(Vector3 at, Color colour, float size)
        {
            var root = new GameObject("Burst"); root.transform.position = at;
            var burst = root.AddComponent<BoltBurst>(); burst.colour = colour;
            int n = Mathf.Clamp(Mathf.RoundToInt(size * 40), 5, 12);
            for (int i = 0; i < n; i++)
            {
                var e = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(e.GetComponent<Collider>());
                e.transform.SetParent(root.transform, false); e.transform.localScale = Vector3.one * size * .35f;
                var r = e.GetComponent<MeshRenderer>(); r.sharedMaterial = Tinted(Color.Lerp(colour, Color.white, .3f)); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                burst.embers.Add((e.transform, Random.onUnitSphere * (1.5f + size * 4) + Vector3.up));
            }
            var l = root.AddComponent<Light>(); l.type = LightType.Point; l.color = colour; l.range = 3 + size * 8; l.intensity = 2.5f; l.shadows = LightShadows.None;
            burst.light = l;
        }
    }

    /// <summary>The embers of a <see cref="Bolt"/>'s impact: flung out, slowed, shrinking away.</summary>
    public sealed class BoltBurst : MonoBehaviour
    {
        public readonly System.Collections.Generic.List<(Transform t, Vector3 v)> embers = new System.Collections.Generic.List<(Transform, Vector3)>();
        public Light light; public Color colour; float age;
        const float Life = .38f;
        void Update()
        {
            age += Time.deltaTime; float k = 1 - Mathf.Clamp01(age / Life);
            for (int i = 0; i < embers.Count; i++)
            {
                var (t, v) = embers[i]; if (t == null) continue;
                t.position += v * Time.deltaTime * k; t.localScale = Vector3.one * Mathf.Max(.001f, t.localScale.x * (1 - Time.deltaTime * 2.5f));
                embers[i] = (t, v + Vector3.down * 4 * Time.deltaTime);
            }
            if (light != null) light.intensity = 2.5f * k;
            if (age >= Life) Destroy(gameObject);
        }
    }
}
