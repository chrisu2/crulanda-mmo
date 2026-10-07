using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Kit models as props (2026-10-07, the art rounds from Chris's assets): Quaternius's Fantasy Props MegaKit (CC0) and Lukas
    /// Bobor's Medieval props (Asset Store EULA), under Resources/Props (Fantasy/&lt;Name&gt;, Medieval/Prefabs/&lt;Name&gt;; the
    /// importer settings are ThirdPartyImport). <see cref="ModelProp"/> stands one where a builder asks, scaled to a height, its
    /// bottom on the spot, turned; a builder keeps its painted fallback for when the model is missing (the tests' bare scenes).
    /// Barrels and crates everywhere, the inn's tables, stools, candles, mugs and chandelier, the smithy's bench, whetstone and
    /// weapon stand, the market's crates of apples and carrots are the first to use them.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        static readonly Dictionary<string, GameObject> propSources = new Dictionary<string, GameObject>();
        /// <summary>The kit prefab (or model) under Resources/Props, cached; null when the kit is not there.</summary>
        public static GameObject PropSource(string path)
        {
            if (!propSources.TryGetValue(path, out var go)) { go = Resources.Load<GameObject>("Props/" + path); propSources[path] = go; }
            return go;
        }
        public static bool HasProp(string path) { return PropSource(path) != null; }
        /// <summary>Models that come in lying down (a Z-up export: their height runs along z, or along x), set upright first (PropCapture
        /// showed which, 2026-10-07): the turn is put on the model's own parts before it is measured.</summary>
        static readonly Dictionary<string, Vector3> PropTurns = new Dictionary<string, Vector3> {
            { "Fantasy/Barrel", new Vector3(-90, 0, 0) }, { "Fantasy/Stool", new Vector3(-90, 0, 0) }, { "Fantasy/Chandelier", new Vector3(-90, 0, 0) },
            { "Fantasy/Dummy", new Vector3(-90, 0, 0) }, { "Fantasy/Banner_1", new Vector3(-90, 0, 0) }, { "Fantasy/CandleStick", new Vector3(0, 0, -90) },
            { "Medieval/Prefabs/CandleV1", new Vector3(-90, 0, 0) },
        };

        /// <summary>
        /// A kit model placed under <paramref name="parent"/>: scaled so it stands <paramref name="height"/> tall (and no wider than
        /// <paramref name="width"/> when given), its bottom's middle at <paramref name="foot"/> (parent-local), turned
        /// <paramref name="yaw"/> about the vertical. Only its meshes come along (no kit scripts, colliders or lights); solid adds a box
        /// collider the navmesh keeps clear. Null when the model is missing, so the caller can draw its painted stand-in.
        /// </summary>
        public static GameObject ModelProp(Transform parent, string path, Vector3 foot, float yaw, float height, bool solid = false, float? width = null)
        {
            var src = PropSource(path); if (src == null) return null;
            // A holder of our own round the kit's object, so a root mesh or a root scale in the kit never matters.
            var go = new GameObject("Model " + path.Substring(path.LastIndexOf('/') + 1)); var t = go.transform;
            t.SetParent(parent, false); t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity; t.localScale = Vector3.one;
            var inst = Instantiate(src, t, false); inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity;
            foreach (var c in go.GetComponentsInChildren<Component>(true))
                if (!(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer) && !(c is SkinnedMeshRenderer)) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            if (PropTurns.TryGetValue(path, out var turn)) inst.transform.localRotation = Quaternion.Euler(turn);
            var b = PropBounds(t); if (b.size.y < .001f) return go;
            float s = height / b.size.y;
            if (width.HasValue) s = Mathf.Min(s, width.Value / Mathf.Max(.001f, Mathf.Max(b.size.x, b.size.z)));
            t.localScale = Vector3.one * s; t.localRotation = Quaternion.Euler(0, yaw, 0);
            t.localPosition = foot + t.localRotation * new Vector3(-b.center.x * s, -b.min.y * s, -b.center.z * s);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
            if (solid) { var box = go.AddComponent<BoxCollider>(); box.center = b.center; box.size = b.size; go.AddComponent<NavBlocker>(); }
            return go;
        }
        /// <summary>The model's meshes' bounds in its root's own space (before any scale), from each mesh's corners.</summary>
        public static Bounds PropBounds(Transform root)
        {
            var b = new Bounds(); bool any = false; var toRoot = root.worldToLocalMatrix;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue; var mb = mf.sharedMesh.bounds; var m = toRoot * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var p = m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            foreach (var sk in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (sk.sharedMesh == null) continue; var mb = sk.sharedMesh.bounds; var m = toRoot * sk.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var p = m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            return b;
        }
        /// <summary>A kit model out of doors: placed, then its foot set on the land (a slope under a stall's crates).</summary>
        GameObject GroundProp(Transform parent, string path, Vector3 foot, float yaw, float height, bool solid = false)
        {
            var go = ModelProp(parent, path, foot, yaw, height, solid); if (go == null) return null;
            var p = go.transform.position; float ground = HeightAt(p.x, p.z); var dy = ground - parent.TransformPoint(foot).y;
            go.transform.position = p + Vector3.up * dy; return go;
        }
        /// <summary>The kit props the builders use, with the heights they stand at (PropCapture renders this row to look at).</summary>
        public static readonly (string path, float height)[] KitProps = {
            ("Fantasy/Barrel", .95f), ("Fantasy/Crate_Wooden", .82f), ("Fantasy/Table_Large", .8f), ("Fantasy/Stool", .45f), ("Fantasy/CandleStick", .22f), ("Fantasy/Mug", .12f),
            ("Fantasy/Chandelier", .8f), ("Fantasy/Bench", .5f), ("Fantasy/Workbench", .9f), ("Fantasy/WeaponStand", 1.6f), ("Fantasy/Whetstone", .6f), ("Fantasy/Bucket_Metal", .38f),
            ("Fantasy/FarmCrate_Apple", .34f), ("Fantasy/FarmCrate_Carrot", .34f), ("Fantasy/Dummy", 1.9f), ("Fantasy/Banner_1", 2.6f), ("Fantasy/Cauldron", .9f),
            ("Medieval/Prefabs/PileOfWoodV1", .5f), ("Medieval/Prefabs/CandleV1", .2f), ("Medieval/Prefabs/BottleV1", .28f), ("Medieval/Prefabs/MugV2", .13f), ("Medieval/Prefabs/PotV1", .3f),
            ("Medieval/Prefabs/BeerBarrelV1", .9f), ("Medieval/Prefabs/LanternV1", .4f),
        };
    }
}
