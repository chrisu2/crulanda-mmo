using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Crown on the Lowtowns' skyline (Docs/ZONE_LOWTOWNS.md decision 5: seen, never reached): the mountain Argentis is built on, far
    /// off in the north-east, and on its summit the High City, white stone and gold (CANON, world_bible.md Geography: "The Crown: the
    /// highest tier, built of white stone and gold, housing the Council and the wealthy"): a curtain wall, towers, a dome and the Council's
    /// tall gilt spire. The shapes are GAME-ONLY. Drawn flat in the haze's colours like the distant ranges (Resources/Shaders/Distant, no
    /// scene fog and no lighting): every frame the foot takes the fog's colour and the tops a mix of it and their own (dark rock, pale
    /// stone, gold), so the smog veils it by day, it thins at night, and it darkens with the evening. No colliders; outside the map.
    /// </summary>
    public sealed class CrownSkyline : MonoBehaviour
    {
        Material rock, stone, gold; float strength = 1;
        static readonly Color Slate = new Color(.3f, .32f, .36f), White = new Color(.93f, .91f, .86f), Gilt = new Color(.95f, .76f, .36f);
        /// <summary>How far past the zone's edge the mountain's foot stands, and how high its summit rises (metres).</summary>
        public const float Beyond = 380, Summit = 210;
        /// <summary>Its bearing from the zone's centre, degrees clockwise from north (the north-east sky).</summary>
        public const float Bearing = 38;

        public static CrownSkyline Build(Transform parent, ZoneDefinition zone, float half)
        {
            var shader = Resources.Load<Shader>("Shaders/Distant"); if (shader == null) return null;
            var go = new GameObject("The Crown on the skyline"); go.transform.SetParent(parent, false);
            var c = go.AddComponent<CrownSkyline>();
            var dir = Quaternion.Euler(0, Bearing, 0) * Vector3.forward; var foot = dir * (half + Beyond);
            go.transform.localPosition = foot;
            c.rock = Mat(shader, -30, Summit * .8f); c.stone = Mat(shader, Summit - 10, Summit + 50); c.gold = Mat(shader, Summit + 20, Summit + 110);
            Show(go.transform, Mountain(zone != null ? zone.seed : 1), c.rock, "The mountain");
            // The High City on the summit: a ring of wall with square towers, the halls inside, a dome and the gilt spire at the middle.
            var box = ZoneMeshes.Box(Vector3.one, 2); var cone = ZoneMeshes.Cone(1, 1, 8); var walls = new List<CombineInstance>(); var gilt = new List<CombineInstance>();
            void Box(Vector3 at, Vector3 size) { walls.Add(new CombineInstance { mesh = box, transform = Matrix4x4.TRS(at, Quaternion.identity, size) }); }
            void Spire(Vector3 at, float r, float h, List<CombineInstance> into) { into.Add(new CombineInstance { mesh = cone, transform = Matrix4x4.TRS(at, Quaternion.identity, new Vector3(r, h, r)) }); }
            float top = Summit - 6;
            for (int k = 0; k < 10; k++)
            {
                float a = k * 36 * Mathf.Deg2Rad, a2 = (k + 1) * 36 * Mathf.Deg2Rad; var p = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 62; var q = new Vector3(Mathf.Sin(a2), 0, Mathf.Cos(a2)) * 62;
                var mid = (p + q) / 2; walls.Add(new CombineInstance { mesh = box, transform = Matrix4x4.TRS(mid + Vector3.up * (top + 9), Quaternion.LookRotation(q - p), new Vector3(4, 18, (q - p).magnitude)) });
                Box(p + Vector3.up * (top + 13), new Vector3(9, 26, 9)); Spire(p + Vector3.up * (top + 26), 6, 9, walls);
            }
            Box(new Vector3(-18, top + 20, 6), new Vector3(30, 40, 22)); Box(new Vector3(20, top + 16, -10), new Vector3(26, 32, 20)); Box(new Vector3(4, top + 30, 26), new Vector3(18, 60, 18));
            Box(new Vector3(0, top + 28, -4), new Vector3(16, 56, 16));
            Spire(new Vector3(0, top + 56, -4), 8, 70, gilt);                          // the Council Keep's tall gilt spire
            Spire(new Vector3(4, top + 60, 26), 7, 24, gilt); Spire(new Vector3(-18, top + 40, 6), 9, 14, walls);
            gilt.Add(new CombineInstance { mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx"), transform = Matrix4x4.TRS(new Vector3(20, top + 32, -10), Quaternion.identity, new Vector3(22, 20, 22)) });   // the domed Council Hall, gilt
            Show(go.transform, Joined(walls, "The High City"), c.stone, "The High City");
            Show(go.transform, Joined(gilt, "The gilt"), c.gold, "The gilt");
            c.LateUpdate();
            return c;
        }
        static Material Mat(Shader s, float baseY, float crestY)
        {
            var m = new Material(s) { name = "Crown skyline" }; m.SetFloat("_Base", baseY); m.SetFloat("_Crest", crestY); return m;
        }
        static Mesh Joined(List<CombineInstance> parts, string name)
        {
            var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.CombineMeshes(parts.ToArray(), true, true); m.RecalculateBounds(); return m;
        }
        static void Show(Transform parent, Mesh mesh, Material mat, string name)
        {
            if (mesh == null) return;
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }
        /// <summary>The mountain: three rings of a ragged cone (foot, shoulder, crown) closed over a flat summit the city stands on.</summary>
        static Mesh Mountain(int seed)
        {
            const int N = 48; float[] radius = { 330, 170, 78 }, height = { -40, Summit * .62f, Summit - 6 };
            var v = new List<Vector3>(); var t = new List<int>();
            for (int ring = 0; ring < 3; ring++)
                for (int i = 0; i < N; i++)
                {
                    float a = i * Mathf.PI * 2 / N, n = Mathf.PerlinNoise(seed % 97 + Mathf.Cos(a) * 2 + ring, Mathf.Sin(a) * 2 + ring * 3);
                    float r = radius[ring] * (ring == 2 ? 1 : .8f + .4f * n), h = height[ring] + (ring == 1 ? (n - .5f) * 40 : 0);
                    v.Add(new Vector3(Mathf.Sin(a) * r, h, Mathf.Cos(a) * r));
                }
            v.Add(new Vector3(0, Summit - 6, 0));
            for (int ring = 0; ring < 2; ring++)
                for (int i = 0; i < N; i++)
                {
                    int a = ring * N + i, b = ring * N + (i + 1) % N, c = a + N, d = b + N;
                    t.AddRange(new[] { a, c, b, b, c, d });
                }
            for (int i = 0; i < N; i++) t.AddRange(new[] { 2 * N + i, 3 * N, 2 * N + (i + 1) % N });
            var m = new Mesh { name = "Crown mountain" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        void LateUpdate()
        {
            var fog = RenderSettings.fogColor;
            strength = Mathf.Clamp01((RenderSettings.fogEndDistance - 30) / 200);   // the smog veils it; a clear night shows it
            float light = Mathf.Clamp(RenderSettings.ambientSkyColor.grayscale / .55f, .15f, 1.1f);
            Set(rock, fog, Color.Lerp(fog, Slate * light, .2f + .35f * strength));
            Set(stone, fog, Color.Lerp(fog, White * light, .3f + .45f * strength));
            Set(gold, fog, Color.Lerp(fog, Gilt * light, .35f + .45f * strength));
        }
        static void Set(Material m, Color haze, Color crest) { if (m == null) return; m.SetColor("_Haze", haze); m.SetColor("_Color", crest); }
    }
}
