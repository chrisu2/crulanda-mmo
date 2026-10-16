using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A far place on a zone's skyline (Chris, 2026-10-16: "maybe see a castle off in the distance when looking north"): from
    /// Oakhaven, up its North road, the Crown of Argentis on its mountain (CANON: Argentis, the Sky Bastion, above Mount Umbra at the
    /// centre of the land, the overworld map; the High City's crown of white-and-gold towers, the domed hall and the spire, the city
    /// map). A zone names it in <see cref="ZoneDefinition.skyline"/> (kind, bearing, how far out in reaches, scale), so any zone can show
    /// one later. A silhouette of a handful of low-poly shapes past the world's edge, beyond the forest ring the distant ranges dip
    /// for it (DistantRanges): the mountain, the walled plateau and its towers, the keep and its tall spire, the dome, the Archive's
    /// spires. Drawn flat in the haze's colours as the ranges are (Resources/Shaders/Distant): every frame the white stone is a pale
    /// mix of the fog and its own colour, the gilt a warmer one, the mountain's rock a darker; it fades in thick weather and darkens at
    /// dusk. Nothing solid, nothing in the navmesh, no shadows: it is never reached. Draws nothing random.
    /// </summary>
    public sealed class DistantSkyline : MonoBehaviour
    {
        Material rock, stone, gilt;

        public static void Build(Transform parent, ZoneDefinition zone, float reach)
        {
            if (zone == null || zone.skyline == null || zone.skyline.Length == 0) return;
            var shader = Resources.Load<Shader>("Shaders/Distant"); if (shader == null) return;
            foreach (var s in zone.skyline)
            {
                if (s == null || s.kind != "crown") continue;   // the one kind so far
                var go = new GameObject("Distant skyline: " + (string.IsNullOrEmpty(s.name) ? s.kind : s.name)); go.transform.SetParent(parent, false);
                float r = s.bearing * Mathf.Deg2Rad, far = reach * (s.distance > 0 ? s.distance : 1.75f), k = s.scale > 0 ? s.scale : 1;
                // Out along its bearing (0 north, 90 east), its front turned to face the zone.
                go.transform.localPosition = new Vector3(Mathf.Sin(r), 0, Mathf.Cos(r)) * far; go.transform.localRotation = Quaternion.Euler(0, s.bearing + 180, 0);
                go.transform.localScale = Vector3.one * k;
                var d = go.AddComponent<DistantSkyline>();
                d.rock = Mat(shader, -40, 60); d.stone = Mat(shader, 20, 150); d.gilt = Mat(shader, 60, 200);
                d.Crown(go.transform);
                d.LateUpdate();
            }
        }
        static Material Mat(Shader shader, float lost, float crest) { var m = new Material(shader) { name = "Distant skyline" }; m.SetFloat("_Base", lost); m.SetFloat("_Crest", crest); return m; }

        void Shape(Transform t, Mesh mesh, Vector3 at, Vector3 scale, Material m, float yaw = 0)
        {
            var o = new GameObject(mesh.name); o.transform.SetParent(t, false); o.transform.localPosition = at; o.transform.localScale = scale; o.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            o.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = o.AddComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        }
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static Mesh Prim(PrimitiveType type)
        {
            string key = type.ToString();
            if (!meshes.TryGetValue(key, out var m) || m == null) { var o = GameObject.CreatePrimitive(type); m = o.GetComponent<MeshFilter>().sharedMesh; if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); meshes[key] = m; }
            return m;
        }
        static Mesh Cone(int sides) { string key = "cone" + sides; if (!meshes.TryGetValue(key, out var m) || m == null) meshes[key] = m = ZoneMeshes.Cone(.5f, 1, sides); return m; }

        /// <summary>
        /// The Crown of Argentis as the city map draws it, seen from the south (local -z faces the zone), in metres at scale 1: Mount
        /// Umbra's shoulders, the plateau on its cliff, the curtain wall's square towers along its front, the keep at the middle with its
        /// great tower and gilt spire (some 230 m over the zone's ground), the dome to the east, the Archive's twin spires to the west,
        /// gilt roofs between. The Spire below the south wall and the Lowtowns are left out: too small to read at this distance.
        /// </summary>
        void Crown(Transform t)
        {
            Mesh cube = Prim(PrimitiveType.Cube), cyl = Prim(PrimitiveType.Cylinder), ball = Prim(PrimitiveType.Sphere);
            // The mountain: a broad cone and two shoulders, sunk so only the upper slopes stand over the hills.
            Shape(t, Cone(9), new Vector3(0, -80, 0), new Vector3(420, 190, 300), rock);   // its peak (110) under the plateau's top
            Shape(t, Cone(7), new Vector3(-150, -60, 30), new Vector3(260, 140, 200), rock, 20);
            Shape(t, Cone(7), new Vector3(160, -60, 40), new Vector3(240, 125, 200), rock, -15);
            // The plateau on its cliff, and the curtain wall round its top.
            Shape(t, cyl, new Vector3(0, 100, 10), new Vector3(170, 18, 120), rock);
            Shape(t, cube, new Vector3(0, 122, -48), new Vector3(160, 10, 4), stone);   // the south wall, toward the zone
            foreach (int s in new[] { -1, 1 }) Shape(t, cube, new Vector3(s * 80, 122, 10), new Vector3(4, 10, 110), stone);
            for (int i = 0; i < 7; i++) { float x = -75 + i * 25; Shape(t, cube, new Vector3(x, 126, -48), new Vector3(7, 18, 7), stone); Shape(t, Cone(4), new Vector3(x, 135, -48), new Vector3(9, 7, 9), gilt, 45); }
            // Gilt-roofed houses packed inside the walls (a few low blocks) and the Grand Stair's terrace.
            for (int i = 0; i < 9; i++) { float x = -64 + i * 16; Shape(t, cube, new Vector3(x, 121 + (i % 3) * 2, -30 + (i % 2) * 8), new Vector3(12, 10 + (i % 3) * 3, 10), stone); Shape(t, Cone(4), new Vector3(x, 127 + (i % 3) * 3.5f, -30 + (i % 2) * 8), new Vector3(14, 5, 12), gilt, 45); }
            // The Council Keep: the block, its corner towers, the great tower and its tall gilt spire.
            Shape(t, cube, new Vector3(0, 132, 30), new Vector3(36, 30, 28), stone);
            foreach (int sx in new[] { -1, 1 }) { Shape(t, cube, new Vector3(sx * 18, 140, 18), new Vector3(8, 46, 8), stone); Shape(t, Cone(4), new Vector3(sx * 18, 163, 18), new Vector3(11, 10, 11), gilt, 45); }
            Shape(t, cube, new Vector3(0, 165, 33), new Vector3(13, 66, 13), stone);
            Shape(t, Cone(8), new Vector3(0, 198, 33), new Vector3(14, 36, 14), gilt);
            // The Council Hall's dome to the east, on its drum.
            Shape(t, cyl, new Vector3(52, 128, 28), new Vector3(30, 9, 30), stone);
            Shape(t, ball, new Vector3(52, 138, 28), new Vector3(26, 22, 26), gilt);
            // The Archive to the west: a nave and two tall gilt spires on their towers.
            Shape(t, cube, new Vector3(-52, 130, 28), new Vector3(30, 22, 24), stone);
            foreach (int s in new[] { -1, 1 }) { Shape(t, cube, new Vector3(-52 + s * 9, 140, 16), new Vector3(6, 40, 6), stone); Shape(t, Cone(8), new Vector3(-52 + s * 9, 160, 16), new Vector3(7, 20, 7), gilt); }
        }

        void LateUpdate()
        {
            var fog = RenderSettings.fogColor;
            float strength = Mathf.Clamp01((RenderSettings.fogEndDistance - 60) / 220);   // thick weather swallows it, as it does the ranges
            float light = Mathf.Clamp(RenderSettings.ambientSkyColor.grayscale / .55f, .15f, 1.1f);
            Color Lit(Color c) { return new Color(c.r * light, c.g * light, c.b * light); }
            // Faint: white stone and gilt a little stronger than the far range so the castle reads against it, the rock as the near range.
            Set(rock, fog, Color.Lerp(fog, Lit(new Color(.36f, .42f, .5f)), .34f * strength));
            Set(stone, fog, Color.Lerp(fog, Lit(new Color(.97f, .95f, .9f)), .42f * strength));
            Set(gilt, fog, Color.Lerp(fog, Lit(new Color(1f, .8f, .36f)), .45f * strength));
        }
        static void Set(Material m, Color haze, Color crest) { if (m == null) return; m.SetColor("_Haze", haze); m.SetColor("_Color", crest); }
    }
}
