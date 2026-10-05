using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The light over a camp body that still holds something (loot DESIGN.md 4, feature 2), so what lies on it reads from across
    /// the camp by colour and height alone: a white twinkle for coins, junk and common things; a green glint and a 1.2 m column for
    /// uncommon; a blue beam 4 m high for rare; a purple beam 7 m high with a turning ring on the ground for epic, pulsing slowly.
    /// It stands on the enemy's root (the Body tips over as it dies), at world scale whatever the elite's size. Drawn with
    /// Sprites/Default (unlit, so it reads at night as well as at noon, and always in builds), from five shared materials whose
    /// colours run above 1 for rare and epic so the camera's bloom takes them; no light and no collider (clicks and sight lines go
    /// through it). It goes out when the body is emptied or the mob respawns.
    /// </summary>
    public sealed class LootBeacon : MonoBehaviour
    {
        public const string Name = "Loot beacon";
        /// <summary>The quality this beacon shows: 0 poor to 4 epic.</summary>
        public int Quality { get; private set; }
        static Material[] mats; static Mesh beamMesh, starMesh, ringMesh; static int pulsedFrame = -1;
        Transform glint, ring; float phase, glintSize;

        /// <summary>Lights (or relights) the beacon over a body for the best quality on it. A negative quality puts it out.</summary>
        public static void Show(EncounterEnemy e, int quality)
        {
            if (e == null) return;
            if (quality < 0) { Clear(e); return; }
            quality = Mathf.Clamp(quality, 0, ItemDatabase.MaxQuality);
            var old = Of(e); if (old != null && old.Quality == quality) return;
            Clear(e);
            var go = new GameObject(Name); go.transform.SetParent(e.transform, false);
            // On the ground under the root (camp roots ride a metre above their feet, times their scale), at world scale.
            float s = Mathf.Max(.01f, e.transform.lossyScale.y);
            go.transform.position = e.transform.position - Vector3.up * s; go.transform.localScale = Vector3.one / s;
            go.AddComponent<LootBeacon>().Build(quality, e.persistentId);
        }
        /// <summary>Puts out the beacon over a body, if it has one.</summary>
        public static void Clear(EncounterEnemy e)
        {
            var b = Of(e); if (b == null) return;
            b.transform.SetParent(null, false); b.gameObject.SetActive(false); Destroy(b.gameObject);
        }
        /// <summary>The beacon over a body, or null.</summary>
        public static LootBeacon Of(EncounterEnemy e) { return e == null ? null : e.GetComponentInChildren<LootBeacon>(); }
        /// <summary>The quality a body's beacon shows, or -1 when it has none.</summary>
        public static int Showing(EncounterEnemy e) { var b = Of(e); return b != null ? b.Quality : -1; }
        /// <summary>How high a quality's beam stands: none for poor and common, 1.2 m uncommon, 4 m rare, 7 m epic.</summary>
        public static float BeamHeight(int quality) { return quality >= 5 ? 10 : quality >= 4 ? 7 : quality == 3 ? 4 : quality == 2 ? 1.2f : 0; }
        /// <summary>A quality's colour as the beacons, the loot window and the call-outs show it: the item colours, with blue and purple lifted so they read on dark ground and in the chat.</summary>
        public static Color Colour(int quality)
        {
            switch (Mathf.Clamp(quality, 0, ItemDatabase.MaxQuality))
            {
                case 2: return new Color(.25f, 1, .2f);
                case 3: return new Color(.22f, .56f, 1);
                case 4: return new Color(.76f, .36f, 1);
                case 5: return new Color(1, .56f, .1f);   // legendary: orange
                default: return Color.white;
            }
        }
        /// <summary>How much brighter than its colour a quality's beacon burns (above 1 the bloom takes it).</summary>
        static float Intensity(int quality) { return quality >= 5 ? 3.4f : quality >= 4 ? 3 : quality == 3 ? 2.4f : quality == 2 ? 1.6f : 1.25f; }

        void Build(int quality, string id)
        {
            Quality = quality; Shared();
            phase = (id != null ? Mathf.Abs(id.GetHashCode() % 1000) : 0) * .0063f;
            float h = BeamHeight(quality);
            // The glint: a four-pointed star over the body, turned to the camera every frame.
            glintSize = quality >= 3 ? .62f : quality == 2 ? .5f : .38f;
            glint = Part("Glint", starMesh, quality, new Vector3(0, .95f, 0), Vector3.one * glintSize);
            if (h > 0)
            {
                float r = quality >= 4 ? .24f : quality == 3 ? .17f : .09f;
                Part("Beam", beamMesh, quality, Vector3.zero, new Vector3(r, h, r));
                Part("Beam core", beamMesh, quality, Vector3.zero, new Vector3(r * .38f, h * .92f, r * .38f));
            }
            if (quality >= 4) ring = Part("Ring", ringMesh, quality, Vector3.up * .06f, new Vector3(1.05f, 1, 1.05f));
        }
        Transform Part(string name, Mesh mesh, int quality, Vector3 at, Vector3 scale)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.localPosition = at; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mats[quality];
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go.transform;
        }
        void LateUpdate()
        {
            float t = Time.time + phase;
            var cam = Camera.main;
            if (glint != null)
            {
                // Twinkle: it swells, shrinks and turns, faster for the better things.
                float speed = Quality >= 3 ? 3.4f : 4.6f, size = glintSize * (.72f + .34f * Mathf.Abs(Mathf.Sin(t * speed)));
                glint.localScale = Vector3.one * size;
                if (cam != null) glint.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, t * 40);
            }
            if (ring != null) ring.localRotation = Quaternion.Euler(0, t * 30, 0);
            // The epic material breathes (shared: every epic beacon pulses together), once a frame.
            if (Quality >= 4 && pulsedFrame != Time.frameCount && mats != null)
            {
                pulsedFrame = Time.frameCount; float k = Intensity(4) * (.8f + .2f * Mathf.Sin(Time.time * 2 * Mathf.PI * .6f));
                var c = Colour(4); mats[4].color = new Color(c.r * k, c.g * k, c.b * k, 1);
            }
        }

        static void Shared()
        {
            if (mats != null && mats[0] != null && beamMesh != null) return;
            mats = new Material[ItemDatabase.MaxQuality + 1];
            var shader = Shader.Find("Sprites/Default");
            for (int q = 0; q <= ItemDatabase.MaxQuality; q++)
            {
                var c = Colour(q); float k = Intensity(q);
                // Sprites/Default: vertex colour times _Color, blended, unlit and two-sided; drawn after the world's glass and water.
                mats[q] = new Material(shader) { name = "Loot beacon " + ItemDatabase.QualityNames[q], renderQueue = 3150, color = new Color(c.r * k, c.g * k, c.b * k, 1) };
            }
            beamMesh = BeamMesh(); starMesh = StarMesh(); ringMesh = RingMesh();
        }
        /// <summary>An open cylinder of radius 1 from the ground to height 1, bright at its foot and fading to nothing at its top.</summary>
        static Mesh BeamMesh()
        {
            const int n = 18; float[] heights = { 0, .12f, .5f, 1 }; float[] alphas = { .85f, .7f, .32f, 0 };
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
            for (int k = 0; k < heights.Length; k++)
                for (int i = 0; i <= n; i++)
                {
                    float a = i * Mathf.PI * 2 / n; v.Add(new Vector3(Mathf.Cos(a), heights[k], Mathf.Sin(a))); col.Add(new Color(1, 1, 1, alphas[k]));
                }
            for (int k = 0; k + 1 < heights.Length; k++)
                for (int i = 0; i < n; i++) { int a = k * (n + 1) + i, b = a + n + 1; tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            var m = new Mesh { name = "Loot beam" }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tri, 0); m.RecalculateBounds();
            return m;
        }
        /// <summary>A flat four-pointed star of radius 1 in the XY plane: solid at the heart, clear at the tips, with a soft round glow behind.</summary>
        static Mesh StarMesh()
        {
            var v = new List<Vector3> { Vector3.zero }; var col = new List<Color> { Color.white }; var tri = new List<int>();
            const int points = 8;
            for (int i = 0; i < points; i++)
            {
                float a = i * Mathf.PI * 2 / points, r = i % 2 == 0 ? 1 : .2f;
                v.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0)); col.Add(new Color(1, 1, 1, i % 2 == 0 ? 0 : .55f));
            }
            for (int i = 0; i < points; i++) tri.AddRange(new[] { 0, 1 + i, 1 + (i + 1) % points });
            // The glow: a disc of radius .42, half bright at the heart.
            int c = v.Count; v.Add(new Vector3(0, 0, .001f)); col.Add(new Color(1, 1, 1, .5f));
            const int n = 16;
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; v.Add(new Vector3(Mathf.Cos(a) * .42f, Mathf.Sin(a) * .42f, .001f)); col.Add(new Color(1, 1, 1, 0)); }
            for (int i = 0; i < n; i++) tri.AddRange(new[] { c, c + 1 + i, c + 1 + (i + 1) % n });
            var m = new Mesh { name = "Loot glint" }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tri, 0); m.RecalculateBounds();
            return m;
        }
        /// <summary>A soft ring band of radius 1 lying flat, clear at both edges.</summary>
        static Mesh RingMesh()
        {
            const int n = 48; float[] radii = { .72f, .86f, 1 }; float[] alphas = { 0, .9f, 0 };
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
            for (int r = 0; r < radii.Length; r++)
                for (int i = 0; i <= n; i++)
                {
                    float a = i * Mathf.PI * 2 / n; v.Add(new Vector3(Mathf.Cos(a) * radii[r], 0, Mathf.Sin(a) * radii[r])); col.Add(new Color(1, 1, 1, alphas[r]));
                }
            for (int r = 0; r + 1 < radii.Length; r++)
                for (int i = 0; i < n; i++) { int a = r * (n + 1) + i, b = a + n + 1; tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            var m = new Mesh { name = "Loot ring" }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tri, 0); m.RecalculateBounds();
            return m;
        }
    }
}
