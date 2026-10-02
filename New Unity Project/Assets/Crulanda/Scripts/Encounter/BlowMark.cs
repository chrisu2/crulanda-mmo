using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The mark an elite's heavy blow lays on the ground while it draws back: an orange ring out to the blow's reach, and a disc
    /// that fills it as the blow gathers (full = it falls). Standing outside the ring when it fills avoids the blow. It lies along
    /// the ground as the target ring does (tilted with open terrain, flat on floors), at world scale whatever the elite's size.
    /// Each elite owns one and shows it only while it winds up.
    /// </summary>
    public sealed class BlowMark : MonoBehaviour
    {
        public static readonly Color Edge = new Color(1, .55f, .12f, .95f), Body = new Color(1, .3f, .1f, .42f);
        Transform ring, fill; Material ringMat, fillMat;
        /// <summary>The reach it shows (metres) and how full it is (0 to 1); 0 reach when hidden. For tests and the HUD.</summary>
        public float Reach { get; private set; }
        public float Filled { get; private set; }
        public bool Showing { get { return gameObject.activeSelf; } }

        public static BlowMark Create()
        {
            var go = new GameObject("Blow mark"); var m = go.AddComponent<BlowMark>();
            m.ring = Part(go.transform, "Reach", RingMesh(), Edge, out m.ringMat);
            m.fill = Part(go.transform, "Gathering", DiscMesh(), Body, out m.fillMat);
            go.SetActive(false);
            return m;
        }
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
            Reach = reach; Filled = Mathf.Clamp01(progress);
            var up = Vector3.up; float lift = .09f; var zone = Crulanda.World.ZoneBuilder.Active;
            if (zone != null && Mathf.Abs(zone.HeightAt(feet.x, feet.z) - feet.y) < .35f)
            {
                float hx = zone.HeightAt(feet.x + reach, feet.z) - zone.HeightAt(feet.x - reach, feet.z), hz = zone.HeightAt(feet.x, feet.z + reach) - zone.HeightAt(feet.x, feet.z - reach);
                up = new Vector3(-hx, 2 * reach, -hz).normalized; lift += (1 - up.y) * reach * 1.5f; feet.y = Mathf.Max(feet.y, zone.HeightAt(feet.x, feet.z));
            }
            transform.position = feet + up * lift;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
            ring.localScale = new Vector3(reach, 1, reach);
            float f = Mathf.Max(.02f, Filled) * reach; fill.localScale = new Vector3(f, 1, f);
            // The last third of the wind-up flashes: the blow is about to fall.
            float flash = Filled > .66f ? .75f + .25f * Mathf.Sin(Time.time * 26) : 1;
            ringMat.color = new Color(Edge.r, Edge.g, Edge.b, Edge.a * flash);
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }
        public void Hide() { Reach = 0; Filled = 0; if (gameObject.activeSelf) gameObject.SetActive(false); }
        void OnDestroy() { if (ringMat != null) Destroy(ringMat); if (fillMat != null) Destroy(fillMat); }

        static Mesh ringMesh, discMesh;
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
