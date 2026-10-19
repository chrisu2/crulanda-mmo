using System.Collections.Generic;
using Crulanda.Gameplay;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The harpoons and the salt seen (play note 290, Chris: "i dont see the salt or harpoons is that a visible item?"):
    /// - a bone harpoon (pale: "petrified leviathan bone", CANON, book1 ch.20) leaves the thrower's hand in an arc and sticks in her body; a
    ///   glowing amber rope (the book's "trailing glowing ropes of amber light", CANON) runs from it back to the thrower for as long as its pin
    ///   lasts: several pins, several ropes; taut and brighter while she is hauled; it fades when the pin ends;
    /// - salt: a white handful thrown from the hand, a puff of white where it lands (the brood it holds turn white crystal as before).
    /// Nothing permanent: each goes when it is done, and <see cref="ClearAll"/> at the fight's end.
    /// </summary>
    public sealed class RaidThrows : MonoBehaviour
    {
        public static readonly HashSet<RaidThrows> Live = new HashSet<RaidThrows>();
        public static readonly Color Bone = new Color(.86f, .82f, .7f), Amber = new Color(1f, .62f, .18f, .9f), Salt = new Color(.96f, .96f, .93f, .9f);
        const float HarpoonFlight = .45f, SaltFlight = .35f, Fade = .35f;

        bool harpoon; Actor by; EncounterEnemy her; Transform target; Vector3 from, offset; float born, pinUntil, endAt = -1;
        Transform shaft; LineRenderer rope; Material ropeMat, mat; readonly List<(Transform t, Vector3 spread)> grains = new List<(Transform, Vector3)>();
        /// <summary>Ropes from harpoons stuck in her now (tests, the log).</summary>
        public static int RopesNow { get { int n = 0; foreach (var r in Live) if (r != null && r.harpoon && r.rope != null && r.rope.enabled) n++; return n; } }

        /// <summary>A bone harpoon from <paramref name="hand"/> into her, its rope held <paramref name="pinSeconds"/>.</summary>
        public static RaidThrows Harpoon(Vector3 hand, Actor by, EncounterEnemy her, float pinSeconds)
        {
            if (her == null) return null;
            var go = new GameObject("A bone harpoon"); var r = go.AddComponent<RaidThrows>();
            r.harpoon = true; r.by = by; r.her = her; r.target = her.transform; r.from = hand; r.born = Time.time; r.pinUntil = Time.time + Mathf.Max(.5f, pinSeconds);
            var side = hand - her.transform.position; side.y = 0; side = side.sqrMagnitude > .01f ? side.normalized : Vector3.forward;
            side = Quaternion.Euler(0, Random.Range(-35f, 35f), 0) * side;
            r.offset = side * her.BodyRadius * .75f + Vector3.up * Random.Range(.7f, 1.4f);
            var shader = Shader.Find("Sprites/Default");
            r.mat = new Material(shader) { name = "Bone", color = Bone };
            r.shaft = Prim(PrimitiveType.Cylinder, go.transform, new Vector3(.06f, .7f, .06f), r.mat);
            var tip = Prim(PrimitiveType.Sphere, r.shaft, new Vector3(1.8f, .12f, 1.8f), r.mat); tip.localPosition = new Vector3(0, 1, 0);
            r.ropeMat = new Material(shader) { name = "Amber rope", color = Amber };
            r.rope = go.AddComponent<LineRenderer>(); r.rope.sharedMaterial = r.ropeMat; r.rope.positionCount = 2; r.rope.widthMultiplier = .05f;
            r.rope.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.rope.receiveShadows = false; r.rope.numCapVertices = 2; r.rope.enabled = false;
            Live.Add(r); r.Place(0);
            return r;
        }
        /// <summary>A white handful of salt from <paramref name="hand"/> onto <paramref name="at"/>.</summary>
        public static RaidThrows SaltAt(Vector3 hand, Transform at)
        {
            if (at == null) return null;
            var go = new GameObject("A handful of salt"); var r = go.AddComponent<RaidThrows>();
            r.target = at; r.from = hand; r.born = Time.time;
            r.mat = new Material(Shader.Find("Sprites/Default")) { name = "Salt", color = Salt };
            for (int i = 0; i < 12; i++)
            {
                var g = Prim(PrimitiveType.Sphere, go.transform, Vector3.one * Random.Range(.06f, .11f), r.mat);
                r.grains.Add((g, Random.insideUnitSphere * .45f));
            }
            Live.Add(r); r.Update();
            return r;
        }
        /// <summary>Every harpoon, rope and handful gone (the fight over).</summary>
        public static void ClearAll() { foreach (var r in new List<RaidThrows>(Live)) if (r != null) Destroy(r.gameObject); Live.Clear(); }

        static Transform Prim(PrimitiveType kind, Transform parent, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(kind); var c = g.GetComponent<Collider>(); if (c != null) Destroy(c);
            g.transform.SetParent(parent, false); g.transform.localScale = scale;
            var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g.transform;
        }
        Vector3 HandNow { get { return by != null ? by.transform.position + Vector3.up * .4f : from; } }
        void Place(float k)
        {
            var stuck = target.position + offset;
            if (k < 1)
            {
                // In the air: an arc from the hand to her side, point first.
                var p = Vector3.Lerp(from, stuck, k) + Vector3.up * (1.6f * 4 * k * (1 - k));
                var ahead = Vector3.Lerp(from, stuck, Mathf.Min(1, k + .05f)) + Vector3.up * (1.6f * 4 * Mathf.Min(1, k + .05f) * (1 - Mathf.Min(1, k + .05f)));
                shaft.position = p; var dir = ahead - p; if (dir.sqrMagnitude > 1e-5f) shaft.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
                return;
            }
            // Stuck in her, its butt toward the thrower; the rope from it to their hand.
            var back = HandNow - stuck; shaft.rotation = Quaternion.FromToRotation(Vector3.up, back.sqrMagnitude > 1e-4f ? -back.normalized : Vector3.down);
            shaft.position = stuck + (back.sqrMagnitude > 1e-4f ? back.normalized * .35f : Vector3.zero);
            rope.enabled = true; rope.SetPosition(0, shaft.position + (back.sqrMagnitude > 1e-4f ? back.normalized * .35f : Vector3.zero)); rope.SetPosition(1, HandNow);
            bool taut = her != null && her.Hauled;
            rope.widthMultiplier = taut ? .08f : .05f;
            float glow = taut ? 1 : .8f + .2f * Mathf.Sin(Time.time * 4 + born);
            ropeMat.color = new Color(Amber.r, Amber.g * (taut ? 1.1f : 1), Amber.b, Amber.a * glow * FadeLeft());
            mat.color = new Color(Bone.r, Bone.g, Bone.b, FadeLeft());
        }
        float FadeLeft() { return endAt < 0 ? 1 : Mathf.Clamp01(1 - (Time.time - endAt) / Fade); }
        void Update()
        {
            if (target == null) { Destroy(gameObject); return; }
            float now = Time.time;
            if (harpoon)
            {
                float k = (now - born) / HarpoonFlight;
                // The pin over, her torn free (her leap), her fight over or the thrower fallen: the rope fades.
                bool over = now >= pinUntil || her == null || !her.actor.IsAlive || her.PullAt < 0 || her.Pins.In(now) == 0 && k >= 1 || by != null && !by.IsAlive;
                if (over && endAt < 0) endAt = now;
                if (endAt >= 0 && now - endAt >= Fade) { Destroy(gameObject); return; }
                Place(Mathf.Min(1, k));
                return;
            }
            float s = (now - born) / SaltFlight;
            if (s >= 1 + Fade / SaltFlight * 1.5f) { Destroy(gameObject); return; }
            var to = target.position + Vector3.up * 1.1f;
            foreach (var g in grains)
            {
                if (g.t == null) continue;
                if (s < 1) g.t.position = Vector3.Lerp(from, to, s) + g.spread * s + Vector3.up * (.8f * 4 * s * (1 - s));
                else g.t.position = to + g.spread * (1 + (s - 1) * 3);   // the puff where it lands
            }
            if (mat != null) mat.color = new Color(Salt.r, Salt.g, Salt.b, Salt.a * (s < 1 ? 1 : Mathf.Clamp01(1 - (s - 1) * SaltFlight / (Fade * 1.5f))));
        }
        void OnDestroy() { Live.Remove(this); if (mat != null) Destroy(mat); if (ropeMat != null) Destroy(ropeMat); }
    }
}
