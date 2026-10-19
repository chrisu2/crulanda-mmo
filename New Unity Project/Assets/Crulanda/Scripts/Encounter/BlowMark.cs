using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crulanda.Encounter
{
    /// <summary>What a ground effect is to the party (Chris, play note 275: "how can you tell which is good and which are bad. no way to know
    /// to get out or stand in it"): every mark the game draws on the ground is one or the other, and looks it.</summary>
    public enum GroundKind { Harmful, Helpful }

    /// <summary>
    /// A mark on the ground. One rule for every ground effect in the game (play note 275):
    /// - HARMFUL (a heavy blow's reach, a warned hold, a trick's ring, a Hollow's dust, the loop's snap, the Unraveller's unmade ground, her
    ///   hunt, her spikes): red-violet, a jagged cracked edge, pulsing, a disc filling as the warning runs out (full = it bites); the sims step
    ///   out of these (GroundMarks) and the HUD names the one you stand in, in red ("Unmade ground: move!");
    /// - HELPFUL (a Paladin's Consecrate and Colours, a Cleric's Sanctuary, a Salt-Mender's circle, a Shaman's rain, a Mage's burning field:
    ///   GroundEffects): soft gold or green, a smooth edge, low and translucent, a slow shimmer; an ally's drawn fainter and a touch smaller
    ///   than yours so they never fill the screen; the HUD notes the one you stand in, in green ("In Consecrated ground").
    /// It lies along the ground as the target ring does (tilted with open terrain, flat on floors), at world scale.
    /// </summary>
    public sealed class BlowMark : MonoBehaviour
    {
        /// <summary>The harmful look: a red-violet edge and body.</summary>
        public static readonly Color Edge = new Color(.43f, .04f, .06f, .97f), Body = new Color(.69f, .08f, .12f, .5f);   // deep red (#6E0A10 edge, #B0141E body; Chris, tune-5: "it really looks pink so make it a deeper red")
        /// <summary>The helpful looks: soft gold (holy, a standard, fire on the foe) and green (rain, salt, growth); low and translucent.</summary>
        public static readonly Color HelpEdgeGold = new Color(1, .86f, .45f, .5f), HelpBodyGold = new Color(1, .85f, .45f, .12f), HelpEdgeGreen = new Color(.5f, .95f, .6f, .5f), HelpBodyGreen = new Color(.45f, .9f, .55f, .12f);
        Transform ring, fill; Material ringMat, fillMat;
        /// <summary>Harmful or helpful (its look, and whether the sims step out of it).</summary>
        public GroundKind Kind { get; private set; }
        /// <summary>What the HUD calls it when you stand in it ("Unmade ground", "Consecrated ground"); null: "Danger" or "Blessed ground".</summary>
        public string Label;
        /// <summary>A body colour of its own (her unmade ground keeps its grey under the red-violet edge); null for its kind's.</summary>
        public Color? BodyColour;
        /// <summary>An ally's (not yours): drawn fainter and a touch smaller.</summary>
        public bool Ally;
        Color edgeColour, bodyColour;
        /// <summary>The reach it shows (metres) and how full it is (0 to 1); 0 reach when hidden. For tests and the HUD.</summary>
        public float Reach { get; private set; }
        public float Filled { get; private set; }
        public bool Showing { get { return gameObject.activeSelf; } }
        /// <summary>Where it lies (the feet it was shown at), on the flat.</summary>
        public Vector3 Feet { get; private set; }
        /// <summary>Every mark showing now (GroundMarks: the sims step out of marked ground, any mark the game draws; WORLD_BOSS_1.md 3.5).
        /// Every mark the game lays is ground to be out of (a heavy blow, a warned hold, a trick's ring, a Hollow's dust, a snapping loop, the
        /// Unraveller's unmade ground and her hunt).</summary>
        public static readonly HashSet<BlowMark> Live = new HashSet<BlowMark>();
        void OnEnable() { Live.Add(this); }
        void OnDisable() { Live.Remove(this); }

        /// <summary>A mark of <paramref name="kind"/> (every mark is one or the other: there is no untagged one), named <paramref name="label"/>
        /// for the HUD; <paramref name="green"/>: a helpful one's green look (rain, salt, growth) in place of its gold.</summary>
        public static BlowMark Create(GroundKind kind, float lifetime, string label = null, bool green = false)
        {
            var go = new GameObject(kind == GroundKind.Harmful ? "Blow mark" : "Helpful ground"); var m = go.AddComponent<BlowMark>();
            m.Kind = kind; m.Label = label; m.Lifetime = Mathf.Max(.5f, lifetime);
            m.edgeColour = kind == GroundKind.Harmful ? Edge : green ? HelpEdgeGreen : HelpEdgeGold;
            m.bodyColour = kind == GroundKind.Harmful ? Body : green ? HelpBodyGreen : HelpBodyGold;
            m.ring = Part(go.transform, "Reach", kind == GroundKind.Harmful ? JaggedRingMesh() : RingMesh(), m.edgeColour, out m.ringMat);
            m.fill = Part(go.transform, "Gathering", DiscMesh(), m.bodyColour, out m.fillMat);
            if (kind == GroundKind.Helpful) { m.ringMat.renderQueue = 3080; m.fillMat.renderQueue = 3080; }   // under the harmful marks
            go.SetActive(false);
            return m;
        }
        /// <summary>
        /// How long one showing may last at most (seconds; Chris: "nothing should be permanent onscreen"): a mark hides itself when it has
        /// shown this long since it was put out, or when nothing has shown it for <see cref="Stale"/> seconds (its owner gone). Required of
        /// every mark (Create).
        /// </summary>
        public float Lifetime { get; private set; }
        public const float Stale = 1.5f;
        float shownFrom = -1, shownAt;
        /// <summary>Whether a showing begun at <paramref name="from"/> and last renewed at <paramref name="last"/> is over at <paramref name="now"/>.</summary>
        public static bool Expired(float from, float last, float now, float lifetime) { return from >= 0 && (now - from > lifetime || now - last > Stale); }
        void Update() { if (Expired(shownFrom, shownAt, Time.time, Lifetime)) Hide(); }
        /// <summary>Its look's two colours now (the tests: the rule kept).</summary>
        public Color EdgeColourNow { get { return ringMat != null ? ringMat.color : edgeColour; } }
        public Color BodyColourNow { get { return fillMat != null ? fillMat.color : bodyColour; } }
        /// <summary>Whether its edge is the jagged one (harmful) or the smooth one (helpful).</summary>
        public bool Jagged { get { return ring != null && ring.GetComponent<MeshFilter>().sharedMesh == jaggedMesh; } }
        static Transform Part(Transform parent, string name, Mesh mesh, Color colour, out Material mat)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            // Sprites/Default, as the target ring: always in builds, vertex colour times _Color, no soft fade into the ground.
            mat = new Material(Shader.Find("Sprites/Default")) { name = "Blow mark", renderQueue = 3090, color = colour };
            r.sharedMaterial = mat;
            return go.transform;
        }
        /// <summary>Lays the mark at the elite's feet: <paramref name="reach"/> metres wide each way, <paramref name="progress"/> full.</summary>
        public void Show(Vector3 feet, float reach, float progress)
        {
            Reach = reach; Filled = Mathf.Clamp01(progress); Feet = feet;
            if (!gameObject.activeSelf || shownFrom < 0) shownFrom = Time.time;
            if (Expired(shownFrom, shownAt, Time.time, Lifetime) && gameObject.activeSelf) return;   // its time is up: not shown again until hidden and put out afresh
            shownAt = Time.time;
            var up = Vector3.up; float lift = .09f; var zone = Crulanda.World.ZoneBuilder.Active;
            if (zone != null && Mathf.Abs(zone.HeightAt(feet.x, feet.z) - feet.y) < .35f)
            {
                float hx = zone.HeightAt(feet.x + reach, feet.z) - zone.HeightAt(feet.x - reach, feet.z), hz = zone.HeightAt(feet.x, feet.z + reach) - zone.HeightAt(feet.x, feet.z - reach);
                up = new Vector3(-hx, 2 * reach, -hz).normalized; lift += (1 - up.y) * reach * 1.5f; feet.y = Mathf.Max(feet.y, zone.HeightAt(feet.x, feet.z));
            }
            transform.position = feet + up * lift;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
            float size = Kind == GroundKind.Helpful && Ally ? .92f : 1;
            ring.localScale = new Vector3(reach * size, 1, reach * size);
            if (Kind == GroundKind.Harmful)
            {
                float f = Mathf.Max(.02f, Filled) * reach; fill.localScale = new Vector3(f, 1, f);
                // Pulsing all the while, and fast in the last third of the warning: it is about to bite.
                float pulse = Filled > .66f ? .7f + .3f * Mathf.Sin(Time.time * 26) : .8f + .2f * Mathf.Sin(Time.time * 6);
                ringMat.color = new Color(edgeColour.r, edgeColour.g, edgeColour.b, edgeColour.a * pulse);
                var body = BodyColour ?? bodyColour; fillMat.color = body;
            }
            else
            {
                // Calm: whole, a slow shimmer; an ally's fainter.
                fill.localScale = new Vector3(reach * size, 1, reach * size);
                float shimmer = .85f + .15f * Mathf.Sin(Time.time * 1.3f), faint = Ally ? .55f : 1;
                ringMat.color = new Color(edgeColour.r, edgeColour.g, edgeColour.b, edgeColour.a * shimmer * faint);
                fillMat.color = new Color(bodyColour.r, bodyColour.g, bodyColour.b, bodyColour.a * faint);
            }
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }
        public void Hide() { Reach = 0; Filled = 0; shownFrom = -1; if (gameObject.activeSelf) gameObject.SetActive(false); }
        void OnDestroy() { Live.Remove(this); if (ringMat != null) Destroy(ringMat); if (fillMat != null) Destroy(fillMat); }

        static Mesh ringMesh, discMesh, jaggedMesh;
        /// <summary>A cracked edge: the ring's band with its outer line torn in and out (the harmful look).</summary>
        static Mesh JaggedRingMesh()
        {
            if (jaggedMesh != null) return jaggedMesh;
            const int n = 72; var rng = new System.Random(23);
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
            float[] radii = { 0, .86f, .95f, 1 }; float[] alphas = { .1f, .2f, 1, 0 };
            var tear = new float[n + 1]; for (int i = 0; i <= n; i++) tear[i] = i == n ? tear[0] : (i % 3 == 0 ? .1f : 0) * (float)rng.NextDouble() - .04f * (float)rng.NextDouble();
            for (int r = 0; r < radii.Length; r++)
                for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2 / n, rr = r >= 2 ? radii[r] + tear[i] : radii[r]; v.Add(new Vector3(Mathf.Cos(a) * rr, 0, Mathf.Sin(a) * rr)); col.Add(new Color(1, 1, 1, alphas[r])); }
            for (int r = 0; r < radii.Length - 1; r++)
                for (int i = 0; i < n; i++) { int a = r * (n + 1) + i, b = a + n + 1; tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            jaggedMesh = new Mesh { name = "Harmful mark ring" }; jaggedMesh.SetVertices(v); jaggedMesh.SetColors(col); jaggedMesh.SetTriangles(tri, 0); jaggedMesh.RecalculateBounds();
            return jaggedMesh;
        }
        /// <summary>A band at the edge of a unit circle (bright, soft on both sides) over a faint wash of the whole circle.</summary>
        static Mesh RingMesh()
        {
            if (ringMesh != null) return ringMesh;
            const int n = 72; float[] radii = { 0, .9f, .96f, 1 }; float[] alphas = { .1f, .16f, 1, 0 };
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
            for (int r = 0; r < radii.Length; r++)
                for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2 / n; v.Add(new Vector3(Mathf.Cos(a) * radii[r], 0, Mathf.Sin(a) * radii[r])); col.Add(new Color(1, 1, 1, alphas[r])); }
            for (int r = 0; r < radii.Length - 1; r++)
                for (int i = 0; i < n; i++) { int a = r * (n + 1) + i, b = a + n + 1; tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            ringMesh = new Mesh { name = "Blow mark ring" }; ringMesh.SetVertices(v); ringMesh.SetColors(col); ringMesh.SetTriangles(tri, 0); ringMesh.RecalculateBounds();
            return ringMesh;
        }
        /// <summary>A unit disc, a little stronger at its rim: the part that grows.</summary>
        static Mesh DiscMesh()
        {
            if (discMesh != null) return discMesh;
            const int n = 72;
            var v = new List<Vector3> { Vector3.zero }; var col = new List<Color> { new Color(1, 1, 1, .55f) }; var tri = new List<int>();
            for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2 / n; v.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))); col.Add(Color.white); }
            for (int i = 1; i <= n; i++) tri.AddRange(new[] { 0, i + 1, i });
            discMesh = new Mesh { name = "Blow mark disc" }; discMesh.SetVertices(v); discMesh.SetColors(col); discMesh.SetTriangles(tri, 0); discMesh.RecalculateBounds();
            return discMesh;
        }
    }
}
