using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A small animal's body and gait: primitives standing on legs that step in a gait, with small idle motions (pecking,
    /// grazing, hopping, flapping). A village critter (<see cref="Critter"/>) and a game animal (<see cref="GameAnimal"/>) both
    /// wear one; what moves it is theirs. The body has no colliders. Lifted out of Critter unchanged (BUILD_PLAN step 11).
    /// </summary>
    public sealed class CritterBody
    {
        public string Kind { get; private set; }
        /// <summary>The body's root ("Body") under the animal's own transform, <see cref="Lift"/> above it.</summary>
        public Transform Root { get; private set; }
        public Transform Head { get; private set; }
        /// <summary>Walking pace and bolting pace in m/s, and how near the player may come (m) before it bolts.</summary>
        public float Speed { get; private set; }
        public float FleeSpeed { get; private set; }
        public float FleeRadius { get; private set; }
        /// <summary>Whether a crow's wings are out (Flap) rather than folded.</summary>
        public bool WingsSpread { get; private set; }
        /// <summary>How far the body's root sits above (negative: below) the animal's transform: 0 for a critter, whose transform is
        /// at its feet; -1 for a game animal, whose transform rides 1 m up like every actor's (its agent's base offset).</summary>
        public float Lift { get; private set; }
        Transform wingL, wingR; float phase, seed;
        // Legs on hip pivots (see Leg): lag is the leg's place in the stride in radians, or -1 to swing with a rabbit's hop;
        // amp its swing in degrees (negative reaches forward). stepRate: phase per m/s that keeps a planted foot from sliding.
        readonly List<(Transform hip, float lag, float amp)> legs = new List<(Transform, float, float)>();
        float stepRate = 12, legLen, stride, tuck; bool atRest;
        // Torso height and half-thickness (m) for lying on its side when it dies (LieDown).
        float torso, flank;
        static readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
        static Material Mat(Color c)
        {
            if (!mats.TryGetValue(c, out var m) || m == null) { m = new Material(Shader.Find("Standard")) { color = c }; m.SetFloat("_Glossiness", .1f); mats[c] = m; }
            return m;
        }
        static Transform Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color c, Vector3? euler = null)
        {
            var o = GameObject.CreatePrimitive(type); Object.Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(parent, false); o.transform.localPosition = pos; o.transform.localScale = scale;
            if (euler.HasValue) o.transform.localEulerAngles = euler.Value;
            o.GetComponent<Renderer>().sharedMaterial = Mat(c); o.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return o.transform;
        }
        /// <summary>A leg on a hip pivot in body space, hip.y above the ground: a slim cylinder (thick &gt; 0) down to the
        /// ground, swung by <see cref="Legs"/>. lag: its place in the stride (0-1), or -1 to swing with the hop; amp: degrees.</summary>
        Transform Leg(Vector3 hip, float thick, Color c, float lag, float amp)
        {
            var t = new GameObject("Leg").transform; t.SetParent(Root, false); t.localPosition = hip;
            if (thick > 0) Part(PrimitiveType.Cylinder, t, new Vector3(0, -hip.y / 2, 0), new Vector3(thick, hip.y / 2, thick), c);
            legs.Add((t, lag < 0 ? -1 : lag * Mathf.PI * 2, amp)); legLen = hip.y;
            stepRate = Mathf.PI / (2 * hip.y * Mathf.Sin(Mathf.Abs(amp) * Mathf.Deg2Rad));   // a half-stride carries it 2 * len * sin(amp)
            return t;
        }
        /// <summary>A bird's foot at the bottom of a leg: one toe fore and aft (the hind toe behind) and two splayed forward.</summary>
        static void Toes(Transform leg, float toe, Color c)
        {
            var ankle = new Vector3(0, .006f - leg.localPosition.y, 0); var size = new Vector3(toe * .28f, .012f, toe);
            Part(PrimitiveType.Cube, leg, ankle + new Vector3(0, 0, toe * .3f), new Vector3(size.x, size.y, toe * 1.6f), c);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, leg, ankle + Quaternion.Euler(0, s * 38, 0) * new Vector3(0, 0, toe * .5f), size, c, new Vector3(0, s * 38, 0));
        }
        /// <summary>
        /// Builds a body of this kind (chicken, rabbit, crow, deer, sheep; anything else is a cat) under <paramref name="owner"/>.
        /// <paramref name="r"/> (0-1) picks its colouring; <paramref name="seed"/> its socks and the phase of its idle motions.
        /// </summary>
        public static CritterBody Build(Transform owner, string kind, float r, float seed, float lift = 0)
        {
            var b = new CritterBody { Kind = kind, seed = seed, Lift = lift };
            b.Root = new GameObject("Body").transform; b.Root.SetParent(owner, false); b.Root.localPosition = new Vector3(0, lift, 0);
            b.Make(kind, r);
            return b;
        }
        void Make(string kind, float r)
        {
            var body = Root;
            switch (kind)
            {
                case "chicken":
                    Speed = .7f; FleeSpeed = 2.8f; FleeRadius = 3; torso = .25f; flank = .12f;
                    var plumage = r < .5f ? new Color(.92f, .9f, .84f) : new Color(.55f, .36f, .2f);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .25f, 0), new Vector3(.26f, .24f, .34f), plumage);
                    Head = Part(PrimitiveType.Sphere, body, new Vector3(0, .43f, .14f), Vector3.one * .13f, plumage);
                    Part(PrimitiveType.Cube, Head, new Vector3(0, .6f, .1f), new Vector3(.25f, .5f, .4f), new Color(.8f, .15f, .12f));
                    Part(PrimitiveType.Cube, Head, new Vector3(0, -.05f, .6f), new Vector3(.25f, .2f, .5f), new Color(.9f, .7f, .2f));
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .33f, -.18f), new Vector3(.12f, .18f, .12f), plumage);
                    // Two thin yellow legs stepping in turn, each on three splayed toes.
                    var shank = new Color(.86f, .66f, .22f);
                    foreach (int s in new[] { -1, 1 }) Toes(Leg(new Vector3(s * .055f, .18f, .02f), .03f, shank, s < 0 ? 0 : .5f, 32), .05f, shank);
                    break;
                case "rabbit":
                    Speed = 1.1f; FleeSpeed = 6; FleeRadius = 7; torso = .18f; flank = .1f;
                    var fur = Color.Lerp(new Color(.52f, .44f, .34f), new Color(.62f, .6f, .56f), r);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .18f, 0), new Vector3(.2f, .2f, .32f), fur);
                    Head = Part(PrimitiveType.Sphere, body, new Vector3(0, .29f, .14f), Vector3.one * .15f, fur);
                    foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Capsule, Head, new Vector3(s * .25f, .9f, -.1f), new Vector3(.25f, .6f, .15f), fur, new Vector3(-15, 0, s * 10));
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .19f, -.17f), Vector3.one * .08f, new Color(.92f, .92f, .9f));
                    // Haunches: big bent back legs bulging from the flanks, each on a long hind foot flat on the ground; small
                    // forepaws under the chest. Mid-hop the haunches kick back and the forepaws reach forward (see Legs).
                    foreach (int s in new[] { -1, 1 })
                    {
                        var haunch = Leg(new Vector3(s * .07f, .1f, -.07f), 0, fur, -1, 45);
                        Part(PrimitiveType.Sphere, haunch, new Vector3(s * .005f, .005f, -.005f), new Vector3(.075f, .13f, .15f), fur);
                        Part(PrimitiveType.Sphere, haunch, new Vector3(0, -.085f, .03f), new Vector3(.045f, .03f, .15f), fur);
                        Part(PrimitiveType.Sphere, Leg(new Vector3(s * .045f, .13f, .1f), .03f, fur, -1, -35), new Vector3(0, -.116f, .012f), new Vector3(.036f, .028f, .05f), fur);
                    }
                    break;
                case "crow":
                    Speed = .5f; FleeSpeed = 7; FleeRadius = 7; torso = .18f; flank = .08f;
                    var black = new Color(.07f, .07f, .09f);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .18f, 0), new Vector3(.16f, .15f, .3f), black);
                    Head = Part(PrimitiveType.Sphere, body, new Vector3(0, .29f, .13f), Vector3.one * .11f, black);
                    Part(PrimitiveType.Cube, Head, new Vector3(0, -.1f, .7f), new Vector3(.2f, .2f, .7f), new Color(.15f, .15f, .15f));
                    // Wings on shoulder pivots: folded down over the flanks on the ground (FoldWings), spread wide and beating
                    // in flight (thin slivers flapping about the body's own axis made a flying crow look perched on thin air).
                    foreach (int s in new[] { -1, 1 })
                    {
                        var wing = new GameObject("Wing").transform; wing.SetParent(body, false); wing.localPosition = new Vector3(s * .07f, .23f, .03f);
                        Part(PrimitiveType.Cube, wing, new Vector3(s * .17f, 0, -.02f), new Vector3(.34f, .02f, .15f), black);
                        if (s < 0) wingL = wing; else wingR = wing;
                    }
                    FoldWings();
                    Part(PrimitiveType.Cube, body, new Vector3(0, .19f, -.2f), new Vector3(.1f, .02f, .16f), black);
                    // Two thin dark legs on splayed toes; tucked back under the tail in flight (see Critter.Fly).
                    var claw = new Color(.13f, .13f, .14f);
                    foreach (int s in new[] { -1, 1 }) Toes(Leg(new Vector3(s * .035f, .13f, .01f), .02f, claw, s < 0 ? 0 : .5f, 30), .036f, claw);
                    break;
                case "deer":
                    Speed = 1.2f; FleeSpeed = 7.5f; FleeRadius = 16; torso = 1; flank = .23f;
                    var hide = new Color(.5f, .34f, .2f);
                    Part(PrimitiveType.Capsule, body, new Vector3(0, 1f, 0), new Vector3(.45f, .6f, .45f), hide, new Vector3(90, 0, 0));
                    // Hips tucked up inside the body so the leg tops stay hidden as they swing; diagonal pairs step together.
                    foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Leg(new Vector3(sx * .15f, .92f, sz * .42f), .08f, hide * .8f, sx == sz ? 0 : .5f, 22);
                    Part(PrimitiveType.Capsule, body, new Vector3(0, 1.35f, .55f), new Vector3(.18f, .32f, .18f), hide, new Vector3(35, 0, 0));
                    Head = Part(PrimitiveType.Sphere, body, new Vector3(0, 1.62f, .78f), new Vector3(.2f, .2f, .32f), hide);
                    if (r < .5f) foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, Head, new Vector3(s * .5f, 1.4f, -.3f), new Vector3(.12f, .9f, .12f), new Color(.7f, .62f, .5f), new Vector3(-10, 0, s * 25));
                    Part(PrimitiveType.Sphere, body, new Vector3(0, 1.05f, -.62f), Vector3.one * .14f, new Color(.9f, .88f, .82f));
                    break;
                case "sheep":
                    Speed = .5f; FleeSpeed = 2.4f; FleeRadius = 3; torso = .62f; flank = .37f;
                    var wool = Color.Lerp(new Color(.9f, .88f, .82f), new Color(.8f, .77f, .7f), r);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .62f, 0), new Vector3(.75f, .6f, 1f), wool);
                    Head = Part(PrimitiveType.Sphere, body, new Vector3(0, .7f, .55f), new Vector3(.25f, .28f, .32f), new Color(.12f, .11f, .1f));
                    // Hips up inside the fleece (the old legs stopped just short of it at the corners).
                    foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Leg(new Vector3(sx * .2f, .47f, sz * .28f), .08f, new Color(.12f, .11f, .1f), sx == sz ? 0 : .5f, 20);
                    break;
                default: // cat
                    Speed = .9f; FleeSpeed = 4.5f; FleeRadius = 4; torso = .26f; flank = .09f;
                    var coat = r < .33f ? new Color(.15f, .14f, .13f) : r < .66f ? new Color(.75f, .45f, .2f) : new Color(.55f, .55f, .55f);
                    Part(PrimitiveType.Capsule, body, new Vector3(0, .26f, 0), new Vector3(.18f, .22f, .18f), coat, new Vector3(90, 0, 0));
                    Head = Part(PrimitiveType.Sphere, body, new Vector3(0, .38f, .22f), Vector3.one * .15f, coat);
                    foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, Head, new Vector3(s * .28f, .45f, 0), new Vector3(.2f, .3f, .1f), coat, new Vector3(0, 0, s * 15));
                    Part(PrimitiveType.Cylinder, body, new Vector3(0, .38f, -.28f), new Vector3(.04f, .2f, .04f), coat, new Vector3(-50, 0, 0));
                    // Four slim legs (diagonal pairs step together) on small paws; some cats wear white socks.
                    var paw = seed % 10 < 3 ? new Color(.9f, .88f, .82f) : Color.Lerp(coat, Color.white, .12f);
                    foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                        Part(PrimitiveType.Sphere, Leg(new Vector3(sx * .055f, .22f, sz * .14f), .05f, coat, sx == sz ? 0 : .5f, 28), new Vector3(0, -.202f, .015f), new Vector3(.065f, .036f, .085f), paw);
                    break;
            }
        }
        /// <summary>
        /// One frame of moving at <paramref name="v"/> m/s: the stride matched to the ground speed (capped, so a bolt is quick legs
        /// rather than a blur); a rabbit hops, one hop each half-turn, longer and higher as it speeds up.
        /// </summary>
        public void Stride(float v)
        {
            phase += Time.deltaTime * (Kind == "rabbit" ? Mathf.Min(v, 2.2f) * 5.5f : Mathf.Min(v * stepRate, 24));
            if (Kind == "rabbit") Root.localPosition = new Vector3(0, Lift + Mathf.Abs(Mathf.Sin(phase)) * Mathf.Lerp(.16f, .28f, (v - 1) / 5), 0);
            Legs(1);
            if (Head != null && Kind != "deer") Head.localEulerAngles = Vector3.zero;
        }
        /// <summary>
        /// Legs: swung in the gait while stepping (easing in and out), tucked back under the tail in flight. Walkers dip as
        /// their legs spread so the planted feet stay on the ground; a rabbit stretches out mid-hop and pitches nose up, then down.
        /// </summary>
        public void Legs(float moving, bool flying = false)
        {
            stride = Mathf.MoveTowards(stride, moving, Time.deltaTime * 5); tuck = Mathf.MoveTowards(tuck, flying ? 1 : 0, Time.deltaTime * 4);
            if (stride == 0 && tuck == 0) { if (atRest) return; atRest = true; } else atRest = false;
            foreach (var l in legs)
                l.hip.localEulerAngles = new Vector3((l.lag < 0 ? Mathf.Abs(Mathf.Sin(phase)) : Mathf.Sin(phase + l.lag)) * l.amp * stride + tuck * 75, 0, 0);
            if (Kind == "rabbit") Root.localEulerAngles = new Vector3(-Mathf.Sin(phase * 2) * 9 * stride, 0, 0);
            else if (legs.Count > 0) Root.localPosition = new Vector3(0, Lift - legLen * (1 - Mathf.Cos(Mathf.Sin(phase) * legs[0].amp * stride * Mathf.Deg2Rad)), 0);
        }
        /// <summary>Standing still: chickens and crows peck, sheep and deer graze (head down), rabbits and cats look around.</summary>
        public void Rest()
        {
            Root.localPosition = new Vector3(0, Lift, 0); Legs(0);
            if (Head == null) return;
            float t = Time.time + seed;
            if (Kind == "chicken" || Kind == "crow") Head.localEulerAngles = new Vector3(Mathf.Max(0, Mathf.Sin(t * 5)) * 55, 0, 0);
            else if (Kind == "sheep" || Kind == "deer") Head.localEulerAngles = new Vector3(Mathf.Sin(t * .4f) > 0 ? 40 : 0, 0, 0);
            else Head.localEulerAngles = new Vector3(0, Mathf.Sin(t * .8f) * 35, 0);
        }
        /// <summary>Head up and still: an animal that has noticed something and is watching it.</summary>
        public void Alert() { Root.localPosition = new Vector3(0, Lift, 0); Legs(0); if (Head != null) Head.localEulerAngles = Vector3.zero; }
        /// <summary>Wings out and beating at <paramref name="flap"/> degrees (a crow in flight; others have no wings).</summary>
        public void Flap(float flap)
        {
            if (wingL == null) return;
            WingsSpread = true; wingL.localEulerAngles = new Vector3(0, 0, flap); wingR.localEulerAngles = new Vector3(0, 0, -flap);
        }
        /// <summary>
        /// Wings folded (on the ground, from spawn and every landing): each swept back along the body and hung down over its
        /// flank, chord near vertical with the top edge tucked in, tip a touch up over the tail. A yaw alone left them lying
        /// flat, as wide as the chord: a square with a head. <see cref="Flap"/> spreads them.
        /// </summary>
        public void FoldWings()
        {
            if (wingL == null) return; WingsSpread = false;
            foreach (int s in new[] { -1, 1 })
                (s < 0 ? wingL : wingR).localRotation = Quaternion.AngleAxis(s * 80, Vector3.up) * Quaternion.AngleAxis(s * 8, Vector3.forward) * Quaternion.AngleAxis(-100, Vector3.right);
        }
        /// <summary>Dead: rolled onto its side where it fell, the torso resting on the ground and the legs out straight.</summary>
        public void LieDown()
        {
            stride = tuck = 0; atRest = false;
            foreach (var l in legs) l.hip.localEulerAngles = Vector3.zero;
            if (Head != null) Head.localEulerAngles = Vector3.zero;
            // Rolled 90 degrees about its length the torso swings from torso-height up to torso-height aside: shift it back over
            // the spot it stood on and up by the flank, so it lies on the ground rather than in it.
            Root.localRotation = Quaternion.Euler(0, 0, 90); Root.localPosition = new Vector3(torso, Lift + flank, 0);
        }
        /// <summary>On its feet again (a game animal back after its respawn).</summary>
        public void StandUp() { Root.localRotation = Quaternion.identity; Root.localPosition = new Vector3(0, Lift, 0); atRest = false; Legs(0); }
    }
}
