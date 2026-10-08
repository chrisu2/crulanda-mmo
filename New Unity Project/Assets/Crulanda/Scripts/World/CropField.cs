using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crulanda.World
{
    /// <summary>
    /// A field's crop, growing through the days (playtest note 76, 2026-10-07: "I see farmers in the field but they aren't actually
    /// doing anything or growing anything"). Every tilled ("soil") field gets plants on the ridges between its furrows (the furrows
    /// run along the field's local Z every 1.6 m, ZoneBuilder's ground paint), the nature kit's tall grass, combined into one mesh
    /// drawn with Crulanda/Crop. Each field runs a six-day round from its own day: bare, sown, sprouting, growing green, ripening gold,
    /// reaped; the fields are a day or two apart, so a walk round the farms shows every stage. <see cref="Day"/> is the game's day count
    /// (EncounterSession sets it); the hour moves it on smoothly.
    /// </summary>
    public sealed class CropField : MonoBehaviour
    {
        public static readonly List<CropField> All = new List<CropField>();
        /// <summary>The game's day count (EncounterProgress.days, set by the session each frame).</summary>
        public static int Day;
        public const float Round = 6;
        int offset; Material mat; Renderer rend; float nextCheck;
        public ZoneRect Field { get; private set; }
        /// <summary>0 bare, rising to 1 ripe; and whether it is reaped (hidden).</summary>
        public float Growth { get; private set; }
        public string Stage { get; private set; }

        public static CropField Build(ZoneBuilder zone, ZoneRect f, int index, Transform parent)
        {
            var src = ZoneBuilder.PropSource("Nature/Grass_Wispy_Tall"); var shader = Resources.Load<Shader>("Shaders/Crop");
            if (src == null || shader == null) return null;
            var mf = src.GetComponentInChildren<MeshFilter>(); var mr = src.GetComponentInChildren<MeshRenderer>(); if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) return null;
            var blade = mf.sharedMesh; var bladeToRoot = src.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix; var bb = blade.bounds;
            float bladeH = Mathf.Max(.01f, bb.size.y), want = .95f, s = want / bladeH;
            var go = new GameObject("Crop: " + (string.IsNullOrEmpty(f.name) ? "field " + index : f.name)); go.transform.SetParent(parent, false);
            var centre = zone.Ground(f.center); go.transform.position = centre; go.transform.rotation = Quaternion.Euler(0, -f.rotation, 0);
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>(); var uv1 = new List<Vector2>(); var uv2 = new List<Vector2>(); var tris = new List<int>();
            var bv = blade.vertices; var bn = blade.normals; var bu = blade.uv; var bt = blade.triangles;
            var rng = new System.Random(index * 7919 + 11);
            float hx = f.size.x / 2 - .6f, hz = f.size.y / 2 - .5f;
            // The ridges: the paint's furrows are where Repeat(local x, 1.6) < .55, so the ridge's middle is at 1.075 past each multiple of 1.6.
            for (float x = Mathf.Ceil((-hx - 1.075f) / 1.6f) * 1.6f + 1.075f; x <= hx; x += 1.6f)
                for (float z = -hz; z <= hz; z += .62f)
                {
                    float jx = x + ((float)rng.NextDouble() - .5f) * .2f, jz = z + ((float)rng.NextDouble() - .5f) * .25f;
                    if (Mathf.Abs(jx) > hx) continue;
                    var world = go.transform.TransformPoint(new Vector3(jx, 0, jz)); float ground = zone.HeightAt(world.x, world.z) - centre.y;
                    var local = new Vector3(jx, ground - .03f, jz);
                    var m = Matrix4x4.TRS(local, Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0), Vector3.one * s * (.85f + (float)rng.NextDouble() * .3f)) * bladeToRoot;
                    var m3 = m.inverse.transpose; int start = verts.Count;
                    for (int i = 0; i < bv.Length; i++)
                    {
                        verts.Add(m.MultiplyPoint3x4(bv[i] - new Vector3(0, bb.min.y, 0))); norms.Add(bn.Length > i ? m3.MultiplyVector(bn[i]).normalized : Vector3.up);
                        uvs.Add(bu.Length > i ? bu[i] : Vector2.zero); uv1.Add(new Vector2(local.y, 0)); uv2.Add(new Vector2(local.x, local.z));
                    }
                    for (int i = 0; i < bt.Length; i++) tris.Add(start + bt[i]);
                }
            if (verts.Count == 0) { Destroy(go); return null; }
            var mesh = new Mesh { name = "Crop", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, uvs); mesh.SetUVs(1, uv1); mesh.SetUVs(2, uv2); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
            mesh.bounds = new Bounds(mesh.bounds.center, mesh.bounds.size + Vector3.one * 2);   // room for the sway
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var c = go.AddComponent<CropField>(); c.Field = f; c.offset = index * 2 + index / 3;
            c.rend = go.AddComponent<MeshRenderer>(); c.rend.shadowCastingMode = ShadowCastingMode.On; c.rend.receiveShadows = true;
            c.mat = new Material(shader) { name = "Crop" }; if (mr != null && mr.sharedMaterial != null) c.mat.mainTexture = mr.sharedMaterial.mainTexture;
            c.rend.sharedMaterial = c.mat; c.Tick(); return c;
        }
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void Update() { if (Time.time >= nextCheck) { nextCheck = Time.time + 2; Tick(); } }
        /// <summary>Where the field is in its round now: bare (the first half day), sown and sprouting, growing green, ripening gold, then reaped (the last half day).</summary>
        public static (float grow, string stage) StageAt(float phase)
        {
            if (phase < .5f) return (0, "bare");
            if (phase < 1.2f) return (Mathf.Lerp(.06f, .2f, (phase - .5f) / .7f), "sown");
            if (phase < 4.4f) return (Mathf.Lerp(.2f, 1, (phase - 1.2f) / 3.2f), "growing");
            if (phase < 5.5f) return (1, "ripe");
            return (0, "reaped");
        }
        public void Tick()
        {
            float phase = Mathf.Repeat(Day + offset + WorldClock.Hour / 24f, Round);
            var (grow, stage) = StageAt(phase); Growth = grow; Stage = stage;
            if (rend != null) rend.enabled = grow > .01f;
            if (mat == null) return;
            mat.SetFloat("_Grow", grow);
            // Green as it grows, turning gold from three quarters grown; ripe barley a warm straw.
            var green = new Color(.42f, .62f, .3f); var gold = new Color(.86f, .66f, .34f);   // ripe: amber barley, not lemon (Chris: "gold is too light and too yellow")
            mat.color = stage == "ripe" ? gold : Color.Lerp(green, gold, Mathf.InverseLerp(.75f, 1, grow) * .6f);
        }
        /// <summary>The crop at a point, if it stands in a field (for the farmers' work: sow, hoe or reap).</summary>
        public static CropField At(Vector3 p)
        {
            foreach (var c in All)
            {
                if (c == null || c.Field == null) continue; var local = c.transform.InverseTransformPoint(p);
                if (Mathf.Abs(local.x) < c.Field.size.x / 2 + 2 && Mathf.Abs(local.z) < c.Field.size.y / 2 + 2) return c;
            }
            return null;
        }
    }
}
