using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The books' look of a Weave-Eater, as a wrap for any body (Docs/WORLD_BOSS_1.md 1.2; Chris, play note 256, "Both": a creature body
    /// wrapped in the books' look). CANON, b1 ch.20:119: "towering silhouettes made of shimmering, corrupted aether and screaming void ... no
    /// faces, no discernible limbs, just jagged, shifting forms of absence. Where they stepped, the petrified ash dissolved into
    /// nothingness"; and ch.20:131, "a sudden, jerking displacement of space". GAME-ONLY in its making:
    /// - the void: a tower of jagged black spikes (thin four-sided shards, leaning out from a core) up to <see cref="Height"/> metres, each
    ///   shifting a little all the time (turn and stretch on slow noise), so the edge never settles;
    /// - the aether: the same shards a touch larger behind them in a shimmering violet, so every edge has a corrupted-light rim (readable
    ///   against the black river at night and the grey setts by day);
    /// - the ash: grey dust lifting and fading where it stands and more where it moves (a particle system on the ground, by distance);
    /// - the jerk: moved more than 2.5 m in a frame (a blink, a hunt's fall) it leaves a fading afterimage of its shards where it was and
    ///   lands squashed for a tenth of a second;
    /// - dead, it goes white crystal and still (salt's work, ch.20:205).
    /// Cheap to draw: two dozen shared four-triangle meshes on the sprite shader (always in builds), one small particle system. Put on any
    /// transform whose scale is the world's (UnravellerBody makes one under the Unraveller; a downloaded body is wrapped the same way).
    /// </summary>
    public sealed class VoidWrap : MonoBehaviour
    {
        public float Height = 7, Radius = 2.2f, Base = -1; public int Shards = 16;
        public static readonly Color Void = new Color(.02f, .0f, .045f, .93f), Aether = new Color(.62f, .32f, 1f, .5f), Crystal = new Color(.92f, .94f, .97f, .92f), Ash = new Color(.42f, .4f, .44f, .75f);
        /// <summary>A body's own actor: dead, the wrap goes to crystal.</summary>
        public Crulanda.Gameplay.Actor Owner;
        Transform shardRoot; readonly List<(Transform t, Quaternion rest, Vector3 scale, float seed, Renderer rim, Renderer core)> shards = new List<(Transform, Quaternion, Vector3, float, Renderer, Renderer)>();
        Material voidMat, rimMat; MaterialPropertyBlock block; ParticleSystem ash;
        Vector3 lastPos; float squashUntil; bool crystal; readonly List<(Transform t, float until)> ghosts = new List<(Transform, float)>();
        /// <summary>How many shards it is made of (the tests).</summary>
        public int ShardCount { get { return shards.Count; } }
        /// <summary>Afterimages left by jerks so far (the tests and the capture).</summary>
        public int Jerks { get; private set; }

        /// <summary>Wraps <paramref name="host"/>: a child "Void wrap" at its origin in world scale (its own scale undone), its feet at
        /// <paramref name="baseY"/> below the origin. Seeded by <paramref name="seed"/> so the same body frays the same way.</summary>
        public static VoidWrap Wrap(Transform host, float height, float radius, float baseY, int seed)
        {
            var go = new GameObject("Void wrap"); go.transform.SetParent(host, false);
            var s = host.lossyScale; go.transform.localScale = new Vector3(1 / Mathf.Max(.01f, s.x), 1 / Mathf.Max(.01f, s.y), 1 / Mathf.Max(.01f, s.z));
            var w = go.AddComponent<VoidWrap>(); w.Height = height; w.Radius = radius; w.Base = baseY;
            w.Owner = host.GetComponentInParent<Crulanda.Gameplay.Actor>();
            w.Build(seed); return w;
        }
        static Mesh shardMesh;
        /// <summary>A unit shard: a four-sided spike from a square base (half-width 1 at y 0) to a point (y 1), its faces flat.</summary>
        static Mesh ShardMesh()
        {
            if (shardMesh != null) return shardMesh;
            var b = new[] { new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(-1, 0, 0), new Vector3(0, 0, -1) }; var tip = new Vector3(.15f, 1, .1f);
            var v = new List<Vector3>(); var tri = new List<int>();
            for (int i = 0; i < 4; i++) { int k = v.Count; v.Add(b[i]); v.Add(b[(i + 1) % 4]); v.Add(tip); tri.AddRange(new[] { k, k + 2, k + 1 }); }
            shardMesh = new Mesh { name = "Void shard" }; shardMesh.SetVertices(v); shardMesh.SetTriangles(tri, 0); shardMesh.RecalculateNormals(); shardMesh.RecalculateBounds();
            return shardMesh;
        }
        void Build(int seed)
        {
            var rng = new System.Random(seed);
            float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
            var shader = Shader.Find("Sprites/Default");
            voidMat = new Material(shader) { name = "Void", renderQueue = 3001, color = Void };
            rimMat = new Material(shader) { name = "Aether rim", renderQueue = 3000, color = Aether };
            block = new MaterialPropertyBlock();
            shardRoot = new GameObject("Shards").transform; shardRoot.SetParent(transform, false); shardRoot.localPosition = new Vector3(0, Base, 0);
            for (int i = 0; i < Shards; i++)
            {
                // The tallest at the middle, shorter and leaning further out toward the edge: a towering, jagged form of absence.
                float out01 = i == 0 ? 0 : R(.15f, 1), h = Height * Mathf.Lerp(1, .35f, out01) * R(.85f, 1.05f), lean = Mathf.Lerp(4, 38, out01), yaw = R(0, 360);
                var t = new GameObject("Shard").transform; t.SetParent(shardRoot, false);
                t.localPosition = Quaternion.Euler(0, yaw, 0) * Vector3.forward * (Radius * .55f * out01);
                var rest = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(lean, R(0, 90), 0);
                var scale = new Vector3(Mathf.Lerp(.55f, .25f, out01) * R(.8f, 1.2f), h, Mathf.Lerp(.55f, .25f, out01) * R(.8f, 1.2f));
                t.localRotation = rest; t.localScale = scale;
                var rim = Part(t, "Aether", rimMat, 1.12f); var core = Part(t, "Void", voidMat, 1);
                shards.Add((t, rest, scale, R(0, 100), rim, core));
            }
            // The ash where it stands and moves: grey motes lifting and fading.
            var ashGo = new GameObject("Ash"); ashGo.transform.SetParent(transform, false); ashGo.transform.localPosition = new Vector3(0, Base + .05f, 0);
            ash = ashGo.AddComponent<ParticleSystem>(); ash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ash.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); main.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .6f);
            main.startSize = new ParticleSystem.MinMaxCurve(.08f, .26f); main.startColor = Ash; main.gravityModifier = -.05f; main.maxParticles = 160; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ash.emission; em.rateOverTime = 8; em.rateOverDistance = 5;
            var shape = ash.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = Radius * .9f; shape.rotation = new Vector3(-90, 0, 0);
            var col = ash.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Ash, 0), new GradientColorKey(new Color(.2f, .18f, .24f), 1) }, new[] { new GradientAlphaKey(.8f, 0), new GradientAlphaKey(0, 1) }); col.color = g;
            var pr = ashGo.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = new Material(shader) { name = "Ash", color = Color.white }; pr.shadowCastingMode = ShadowCastingMode.Off; pr.receiveShadows = false;
            ash.Play();
            lastPos = transform.position;
        }
        static Renderer Part(Transform parent, string name, Material m, float grow)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(grow, 1 + (grow - 1) * .5f, grow);
            go.GetComponent<MeshFilter>().sharedMesh = ShardMesh();
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off;
            return r;
        }

        void Update()
        {
            float t = Time.time;
            bool dead = Owner != null && !Owner.IsAlive;
            if (dead != crystal) SetCrystal(dead);
            // The jerk: a blink or a fall moved it more than 2.5 m since the last frame.
            var at = transform.position;
            if ((at - lastPos).sqrMagnitude > 2.5f * 2.5f && !crystal) Afterimage(lastPos);
            lastPos = at;
            for (int i = ghosts.Count - 1; i >= 0; i--)
            {
                var gh = ghosts[i]; if (gh.t == null) { ghosts.RemoveAt(i); continue; }
                float left = (gh.until - t) / .35f;
                if (left <= 0) { Destroy(gh.t.gameObject); ghosts.RemoveAt(i); continue; }
                gh.t.localScale = Vector3.one * (1 + (1 - left) * .25f);
                foreach (var r in gh.t.GetComponentsInChildren<Renderer>()) { block.SetColor("_Color", (r.sharedMaterial == rimMat ? Aether : Void) * new Color(1, 1, 1, left)); r.SetPropertyBlock(block); }
            }
            if (crystal) return;
            // The shifting edge: each shard turns and stretches on its own slow noise; the rim shimmers.
            float squash = t < squashUntil ? 1 : 0;
            foreach (var s in shards)
            {
                float n1 = Mathf.PerlinNoise(s.seed, t * .9f) - .5f, n2 = Mathf.PerlinNoise(s.seed + 7, t * 1.3f) - .5f, n3 = Mathf.PerlinNoise(s.seed + 13, t * 2.1f) - .5f;
                s.t.localRotation = s.rest * Quaternion.Euler(n1 * 14, n2 * 20, n3 * 14);
                s.t.localScale = new Vector3(s.scale.x * (1 + n2 * .35f + squash * .4f), s.scale.y * (1 + n1 * .18f - squash * .35f), s.scale.z * (1 + n3 * .35f + squash * .4f));
                float shimmer = .3f + .45f * Mathf.PerlinNoise(s.seed + 3, t * 3.5f);
                block.SetColor("_Color", new Color(Aether.r, Aether.g, Aether.b, shimmer)); s.rim.SetPropertyBlock(block);
            }
        }
        /// <summary>Where it was, its shards for a third of a second, fading: the jerk through space.</summary>
        void Afterimage(Vector3 from)
        {
            Jerks++; squashUntil = Time.time + .1f;
            var ghost = Instantiate(shardRoot.gameObject, shardRoot.position - transform.position + from, shardRoot.rotation).transform;
            ghost.name = "Void afterimage"; ghost.localScale = Vector3.one;
            ghosts.Add((ghost, Time.time + .35f));
        }
        void SetCrystal(bool on)
        {
            crystal = on;
            var c = on ? Crystal : Void;
            foreach (var s in shards) { block.SetColor("_Color", c); s.core.SetPropertyBlock(block); s.rim.enabled = !on; s.t.localRotation = s.rest; s.t.localScale = s.scale; }
            if (ash != null) { if (on) ash.Stop(true, ParticleSystemStopBehavior.StopEmitting); else ash.Play(); }
        }
        void OnDestroy()
        {
            foreach (var g in ghosts) if (g.t != null) Destroy(g.t.gameObject);
            if (voidMat != null) Destroy(voidMat); if (rimMat != null) Destroy(rimMat);
        }
    }
}
