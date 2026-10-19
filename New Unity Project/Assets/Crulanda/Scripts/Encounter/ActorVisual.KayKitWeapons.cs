using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The drawn weapons and shields as KayKit models (play note 245, 2026-10-19; the map is KayKitWeapons). The pack builds every
    /// piece the same way (measured from its glTF and its Unity FBX): one mesh, its pivot at the middle of the grip (a shield's at
    /// its handle, behind the face), a weapon standing up +Y from the hand with its head or edge across X and its flats facing Z, a
    /// shield facing +Z with its top +Y, a bow lying along X with its tips and string toward -Z. So the grip goes on the frame's
    /// origin, where the fist closes, and the model is turned by its own measured bounds: a weapon's length up the frame's +Y (the
    /// forearm's pointing line, as the drawings were), its head or edge (the side it reaches further to) toward +Z, scaled so its
    /// length is its family's drawn length; a shield's face toward +Y (outward from the forearm), its top toward +X (up the
    /// forearm), scaled to its family's height; a bow's limbs up and down +Y, its string toward +X (in toward the archer's body,
    /// as ActorVisual.ModelFit laid the pack bow). The put-away copy is built the same way in the slung frame, so it sits on the
    /// back or at the hip where the drawing did. The colours: the atlas's steel and wood swatches pulled toward the item's palette
    /// metal and wood (KayKitWeapons.Tint), one texture per palette; rare and epic pieces keep their glowing accents on the
    /// weapon root (a stone at the guard, one at the pommel; a boss on a shield), epic ones pulsing (Finish).
    /// </summary>
    public sealed partial class ActorVisual
    {
        static readonly Dictionary<string, Bounds> kayKitBounds = new Dictionary<string, Bounds>();
        static readonly Dictionary<string, Material> kayKitMats = new Dictionary<string, Material>();
        static Material kayKitAtlas;

        /// <summary>A KayKit model's bounds in its own space (the pivot, its grip, at the origin): from its vertices when readable.</summary>
        public static bool KayKitBounds(GameObject src, out Bounds b)
        {
            b = default; if (src == null) return false;
            if (kayKitBounds.TryGetValue(src.name, out b)) return true;
            bool any = false; var inv = src.transform.worldToLocalMatrix;
            foreach (var mf in src.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; if (mesh == null) continue; var m = inv * mf.transform.localToWorldMatrix;
                if (mesh.isReadable)
                    foreach (var v in mesh.vertices) { var p = m.MultiplyPoint3x4(v); if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p); }
                else
                    for (int i = 0; i < 8; i++)
                    {
                        var p = m.MultiplyPoint3x4(mesh.bounds.center + Vector3.Scale(mesh.bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                        if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                    }
            }
            if (any) kayKitBounds[src.name] = b;
            return any;
        }

        static Vector3 Axis(int i, float sign = 1) { var v = Vector3.zero; v[i] = sign; return v; }
        /// <summary>The side of the origin an axis reaches further to: +1 or -1.</summary>
        static float Further(Bounds b, int i) { return Mathf.Abs(b.max[i]) >= Mathf.Abs(b.min[i]) ? 1 : -1; }

        /// <summary>
        /// How to lay a KayKit model in a weapon, shield or bow frame: the rotation, the offset and the scale that put its grip (its
        /// pivot) at the frame's origin and its length at <paramref name="length"/> (see the class summary for the axes).
        /// </summary>
        public static (Quaternion rot, Vector3 offset, float scale) KayKitFit(Bounds b, KayKitWeapons.Kind kind, float length)
        {
            var size = b.size; var order = new List<int> { 0, 1, 2 }; order.Sort((x, y) => size[y].CompareTo(size[x]));
            int lng = order[0], mid = order[1], thin = order[2];
            if (kind == KayKitWeapons.Kind.Shield)
            {
                // Face +Z of the model (the pack's shields face forward; the handle is behind, toward -Z); its top toward the shorter
                // reach of its height (a heater's point is at the bottom, further from the handle).
                var face = Axis(thin); var top = Axis(lng, Mathf.Abs(b.max[lng]) <= Mathf.Abs(b.min[lng]) ? 1 : -1);
                var rotS = Quaternion.LookRotation(Vector3.up, Vector3.right) * Quaternion.Inverse(Quaternion.LookRotation(face, top));   // face to +Y, top to +X
                float sc = length / Mathf.Max(.01f, size[lng]);
                return (rotS, Vector3.down * .02f, sc);   // the handle a little in toward the forearm
            }
            var up = Axis(lng, Further(b, lng)); var edge = Axis(mid, Further(b, mid));
            var rot = Quaternion.Inverse(Quaternion.LookRotation(edge, up));   // edge to +Z, length to +Y
            if (kind == KayKitWeapons.Kind.Bow) rot = Quaternion.Euler(0, 90, 0) * rot;   // the bow's depth (toward its tips and string) to +X
            return (rot, Vector3.zero, length / Mathf.Max(.01f, size[lng]));
        }

        /// <summary>The model under <paramref name="frame"/>, fitted, in the item's colours (null palette: the pack's own). Null when the model is not in the build.</summary>
        public static Transform PlaceKayKit(Transform frame, string model, KayKitWeapons.Kind kind, float length, Color? metal = null, Color? wood = null)
        {
            var src = Resources.Load<GameObject>(KayKitWeapons.Folder + model);
            if (src == null || !KayKitBounds(src, out var b)) return null;
            var go = Instantiate(src, frame, false); go.name = "KayKit " + model;
            foreach (var c in go.GetComponentsInChildren<Component>(true))
                if (!(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer)) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            var mat = KayKitMaterial(metal, wood);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true;
                if (mat != null) { var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = mat; r.sharedMaterials = mats; }
            }
            var fit = KayKitFit(b, kind, length);
            go.transform.localRotation = fit.rot; go.transform.localScale = Vector3.one * fit.scale; go.transform.localPosition = fit.offset;
            return go.transform;
        }

        /// <summary>A drawn family's piece as its KayKit model, with its quality's accents; false (nothing built) when it has none or it is missing.</summary>
        bool KayKitGear(GearKit k, Transform root)
        {
            string model = KayKitWeapons.ModelFor(k.l.family, k.l.variant); if (model == null) return false;
            var kind = KayKitWeapons.KindOf(k.l.family); float length = KayKitWeapons.LengthOf(k.l.family);
            var t = PlaceKayKit(root, model, kind, length, k.l.metal, k.l.wood); if (t == null) return false;
            if (kind == KayKitWeapons.Kind.Shield)
            {
                Accent(k, 1, root, Sphere, V(0, .07f, 0), V(.07f, .04f, .07f));                 // a stone on the boss
                Accent(k, 2, root, Sphere, V(length * .32f, .05f, 0), V(.045f, .03f, .045f));   // and one at the top
                return true;
            }
            KayKitBounds(Resources.Load<GameObject>(KayKitWeapons.Folder + model), out var b);
            int lng = b.size.x >= b.size.y && b.size.x >= b.size.z ? 0 : b.size.y >= b.size.z ? 1 : 2;
            float butt = -length * Mathf.Abs(Further(b, lng) > 0 ? b.min[lng] : b.max[lng]) / Mathf.Max(.01f, b.size[lng]);   // where the pommel or butt ends, below the fist
            Accent(k, 1, root, Sphere, V(0, .1f, 0), V(.05f, .04f, .05f));                      // a stone just above the fist, at the guard
            Accent(k, 2, root, Sphere, V(0, butt, 0), S(.045f));                                // and one at the pommel
            return true;
        }

        /// <summary>The Ranger's bow (or another class piece) as a KayKit model under a holder at pos/euler; null when it is not in the build.</summary>
        Transform KayKitHeld(string model, KayKitWeapons.Kind kind, float length, Transform parent, Vector3 pos, Vector3 euler)
        {
            var holder = new GameObject("Class " + model).transform; holder.SetParent(parent, false);
            holder.localPosition = pos; holder.localRotation = Quaternion.Euler(euler);
            if (PlaceKayKit(holder, model, kind, length) != null) return holder;
            if (Application.isPlaying) Destroy(holder.gameObject); else DestroyImmediate(holder.gameObject);
            return null;
        }

        /// <summary>The atlas material, or a copy whose steel and wood swatches lean to this metal and wood (one per pair, kept).</summary>
        static Material KayKitMaterial(Color? metal, Color? wood)
        {
            if (kayKitAtlas == null) kayKitAtlas = Resources.Load<Material>(KayKitWeapons.Atlas);
            if (kayKitAtlas == null || !KayKitWeapons.Tint || metal == null || wood == null) return kayKitAtlas;
            string key = ColorUtility.ToHtmlStringRGB(metal.Value) + ColorUtility.ToHtmlStringRGB(wood.Value);
            if (kayKitMats.TryGetValue(key, out var m) && m != null) return m;
            var tex = TintedAtlas(kayKitAtlas.mainTexture as Texture2D, metal.Value, wood.Value);
            if (tex == null) return kayKitAtlas;
            m = new Material(kayKitAtlas) { name = "KayKitWeapons " + key, mainTexture = tex };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            kayKitMats[key] = m; return m;
        }
        /// <summary>The atlas at 256 px with its steel and wood swatches recoloured: each pixel keeps most of its own brightness and takes
        /// the palette colour's hue; null when the atlas cannot be read.</summary>
        public static Texture2D TintedAtlas(Texture2D src, Color metal, Color wood)
        {
            if (src == null || !src.isReadable) return null;
            const int N = 256; var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var c = src.GetPixelBilinear((x + .5f) / N, (y + .5f) / N);
                    int col = x * 8 / N, row = 3 - y * 4 / N;   // the image's swatches from the top-left; a texture's rows start at the bottom
                    if (KayKitWeapons.MetalCell(row, col)) c = Lean(c, metal, KayKitWeapons.MetalTint);
                    else if (KayKitWeapons.WoodCell(row, col)) c = Lean(c, wood, KayKitWeapons.WoodTint);
                    px[y * N + x] = c;
                }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "KayKitWeapons tinted", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }
        static Color Lean(Color c, Color to, float k)
        {
            float lum = c.grayscale, tl = Mathf.Max(.12f, to.grayscale);
            var t = to * Mathf.Lerp(lum / tl, 1, .3f); t.a = c.a;
            var o = Color.Lerp(c, t, k); o.r = Mathf.Clamp01(o.r); o.g = Mathf.Clamp01(o.g); o.b = Mathf.Clamp01(o.b); return o;
        }
    }
}
