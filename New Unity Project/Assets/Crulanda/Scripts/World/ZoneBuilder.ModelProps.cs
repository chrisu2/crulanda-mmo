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
            { "Fantasy/Dummy", new Vector3(-90, 0, 0) }, { "Fantasy/Banner_1", new Vector3(-90, 0, 0) }, { "Fantasy/Banner_2", new Vector3(-90, 0, 0) }, { "Fantasy/WeaponStand", new Vector3(-90, 0, 0) },   // the weapon stand made Z up too: it lay tipped against the yard's fence (Chris, 15:20) and in Vell's smithy   // Banner_2 made Z up like Banner_1 (2.08 m along z): lying flat it came in some 15 times too big (the 14:12 yard frame) { "Fantasy/CandleStick", new Vector3(0, 0, -90) },
            { "Medieval/Prefabs/CandleV1", new Vector3(-90, 0, 0) },
            // Fantasy2/ (ithappy's Fantasy FREE, the market pieces, 2026-10-15) needs no entry: Blender's export with the turn baked in
            // (UpAxis Y, every node's rotation zero, metres), read from the files; MarketPropTests measures them standing.
            // The Small Props Pack's "Unity FBX" models stand on their heads: their vertices are the glTF copies' turned half round X (read
            // 2026-10-13 after Chris: "two archer targets upside down", "two dummies upside down"); only PracticeTarget_1_B is upright.
            { "SmallProps/Basin_A", new Vector3(180, 0, 0) },
            { "SmallProps/Bucket_1_A", new Vector3(180, 0, 0) },
            { "SmallProps/Hammer_1_A", new Vector3(180, 0, 0) },
            { "SmallProps/Manequin_1_A_001", new Vector3(180, 0, 0) },
            { "SmallProps/Manequin_1_A_002", new Vector3(180, 0, 0) },
            { "SmallProps/Manequin_2_A_001", new Vector3(180, 0, 0) },
            { "SmallProps/Manequin_2_A_002", new Vector3(180, 0, 0) },
            { "SmallProps/PracticeTarget_1_A", new Vector3(180, 0, 0) },
            { "SmallProps/PracticeTarget_1_C", new Vector3(180, 0, 0) },
            { "SmallProps/PracticeTarget_2_A", new Vector3(180, 0, 0) },
            { "SmallProps/PracticeTarget_2_B", new Vector3(180, 0, 0) },
            { "SmallProps/PracticeTarget_2_C", new Vector3(180, 0, 0) },
            { "SmallProps/Stool_A", new Vector3(180, 0, 0) },
        };

        /// <summary>
        /// A kit model placed under <paramref name="parent"/>: scaled so it stands <paramref name="height"/> tall (and no wider than
        /// <paramref name="width"/> when given), its bottom's middle at <paramref name="foot"/> (parent-local), turned
        /// <paramref name="yaw"/> about the vertical. Only its meshes come along (no kit scripts, colliders or lights); solid adds a box
        /// collider the navmesh keeps clear. Null when the model is missing, so the caller can draw its painted stand-in.
        /// </summary>
        static Material smallPropsAtlas;
        static Material SmallPropsAtlas { get { if (smallPropsAtlas == null) smallPropsAtlas = Resources.Load<Material>("Props/SmallProps/SmallPropsAtlas"); return smallPropsAtlas; } }
        /// <summary>
        /// The KayKit Dungeon Pack (Kay Lousberg, CC0; Resources/Props/KayKit, brought in 2026-10-15 after Chris, 01:12-01:16, of the chapel's
        /// end wall: "use some of the dungeon assedt not just plain walls and arch", "kaykit dungeon pack in downloads"): one palette atlas
        /// (dungeon_texture.png) for the whole pack, as the Small Props Pack has. Its "fbx(unity)" exports are Y up in metres with no node
        /// turn (read from the files: UnitScaleFactor 100, no rotation on the mesh node; the plain "fbx" set is the Blender kind, a -90
        /// turn and a scale of 100 on the node, like the Dungeon kit), so no PropTurns entry and no path rule: a wall stands 4 m by 4 m,
        /// 1 m thick, its foot at y 0, centred on x and z.
        /// </summary>
        static Material kayKitAtlas;
        static Material KayKitAtlas { get { if (kayKitAtlas == null) kayKitAtlas = Resources.Load<Material>("Props/KayKit/KayKitAtlas"); return kayKitAtlas; } }
        /// <summary>
        /// The Dungeon Modular Pack (logicalbeat, Asset Store EULA; Resources/Props/DungeonModular, 2026-10-15, Chris: "dungeon modular pack
        /// ... in myassets"): thirteen meshes on a 4 m grid (Wall_A/B 4 x 4 x 0.2, Wall_C 4 x 2, Pillar_A 0.82 x 4, Pillar_B 0.82 x 2,
        /// Arch_A 2.5 x 3.46 x 0.5, Step_A 4 x 2 x 4, Tile_A/B 4 x 4, Torch_A/B 0.7 tall, Handrail_A 2 x 1.2), their detail in the
        /// textures, not the meshes (a wall is eight vertices). Its own materials are a shader graph the project does not have, so the
        /// pieces take a Standard material a family each (M_Wall, M_Pillar, M_Arch, M_Step, M_Tile, M_Torch, M_Handrail: the colour and
        /// normal maps), by the family in the mesh's material name (Step_A carries M_Step_A and M_Wall_A), else by the file's name.
        /// </summary>
        static readonly Dictionary<string, Material> dungeonModularMats = new Dictionary<string, Material>();
        static Material DungeonModularMat(string family)
        {
            if (!dungeonModularMats.TryGetValue(family, out var m)) { m = Resources.Load<Material>("Props/DungeonModular/M_" + family); dungeonModularMats[family] = m; }
            return m;
        }
        static void DressDungeonModular(GameObject go, string file)
        {
            string own = file.Split('_')[0];   // Wall_A -> Wall
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string name = mats[i] != null ? mats[i].name : "", family = own;
                    if (name.StartsWith("M_")) { var parts = name.Split('_'); if (parts.Length > 1) family = parts[1]; }
                    var m = DungeonModularMat(family) ?? DungeonModularMat(own); if (m != null) mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
        }
        /// <summary>The height of a placed prop's top in <paramref name="parent"/>'s space (a table's top, to set its mugs on).</summary>
        public static float TopIn(Transform parent, GameObject prop)
        {
            var rs = prop.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return 0;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            return parent.InverseTransformPoint(new Vector3(b.center.x, b.max.y, b.center.z)).y;
        }
        public static GameObject ModelProp(Transform parent, string path, Vector3 foot, float yaw, float height, bool solid = false, float? width = null, Vector3? box = null)
        {
            var src = PropSource(path); if (src == null) return null;
            // A holder of our own round the kit's object, so a root mesh or a root scale in the kit never matters.
            var go = new GameObject("Model " + path.Substring(path.LastIndexOf('/') + 1)); var t = go.transform;
            t.SetParent(parent, false); t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity; t.localScale = Vector3.one;
            // The kit's own root rotation is kept: a one-mesh file's node (Blender's -90 about x, Z up) lands on the prefab root, and
            // zeroing it laid the Fantasy pack's tables and benches on their sides (Chris, 2026-10-14 19:35: "all the tables and
            // benches are turned on their sides"; the mugs set at table height then floated). KitPropTests.
            var inst = Instantiate(src, t, false); inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = src.transform.localRotation;
            foreach (var c in go.GetComponentsInChildren<Component>(true))
                if (!(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer) && !(c is SkinnedMeshRenderer)) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            if (PropTurns.TryGetValue(path, out var turn)) inst.transform.localRotation = Quaternion.Euler(turn);
            else if (path.StartsWith("Dungeon/")) inst.transform.localRotation = Quaternion.Euler(-90, 0, 0);   // the Adit's pieces (Blender, Z up), all of them
            var b = PropBounds(t); if (b.size.y < .001f) return go;
            float s = height / b.size.y;
            if (width.HasValue) s = Mathf.Min(s, width.Value / Mathf.Max(.001f, Mathf.Max(b.size.x, b.size.z)));
            // A box (a building fitted to its footprint, art round 5): each axis stretched to it, a little either way.
            var k = box.HasValue ? new Vector3(box.Value.x / Mathf.Max(.001f, b.size.x), box.Value.y / Mathf.Max(.001f, b.size.y), box.Value.z / Mathf.Max(.001f, b.size.z)) : Vector3.one * s;
            t.localScale = k; t.localRotation = Quaternion.Euler(0, yaw, 0);
            t.localPosition = foot + t.localRotation * new Vector3(-b.center.x * k.x, -b.min.y * k.y, -b.center.z * k.z);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
            if (path.StartsWith("SmallProps/") && SmallPropsAtlas != null)   // the Small Props Pack (RG Poly, CC0): one atlas for all its models
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = SmallPropsAtlas; r.sharedMaterials = mats; }
            if (path.StartsWith("KayKit/") && KayKitAtlas != null)   // the KayKit Dungeon Pack (CC0): one atlas for the whole pack
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = KayKitAtlas; r.sharedMaterials = mats; }
            if (path.StartsWith("DungeonModular/")) DressDungeonModular(go, path.Substring(path.LastIndexOf('/') + 1));
            if (path.StartsWith("Nature/")) DressNature(go);
            if (solid) { var col = go.AddComponent<BoxCollider>(); col.center = b.center; col.size = b.size; go.AddComponent<NavBlocker>(); }
            return go;
        }
        /// <summary>
        /// A building fitted by its walls (playtest note 80): the model turned <paramref name="yaw"/>, then stretched so the extent of
        /// its vertices in the lowest metre (the walls and posts where they meet the ground, not the roof's eaves) is
        /// <paramref name="w"/> x <paramref name="d"/>, and its whole height <paramref name="h"/>; its walls' middle on the root.
        /// </summary>
        public static GameObject WallProp(Transform parent, string path, float yaw, float w, float h, float d)
        {
            var src = PropSource(path); if (src == null) return null;
            var holder = new GameObject("Model " + path.Substring(path.LastIndexOf('/') + 1)).transform; holder.SetParent(parent, false);
            var turn = new GameObject("Turn").transform; turn.SetParent(holder, false); turn.localRotation = Quaternion.Euler(0, yaw, 0);
            var inst = Instantiate(src, turn, false); inst.transform.localPosition = Vector3.zero;
            foreach (var c in inst.GetComponentsInChildren<Component>(true))
                if (!(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer)) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            var all = PropBounds(holder); var low = new Bounds(); bool any = false; var toHolder = holder.worldToLocalMatrix;
            foreach (var mf in holder.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; if (mesh == null || !mesh.isReadable) continue; var m = toHolder * mf.transform.localToWorldMatrix;
                foreach (var v in mesh.vertices) { var p = m.MultiplyPoint3x4(v); if (p.y > all.min.y + 1) continue; if (!any) { low = new Bounds(p, Vector3.zero); any = true; } else low.Encapsulate(p); }
            }
            if (!any) low = all;
            var k = new Vector3(w / Mathf.Max(.01f, low.size.x), h / Mathf.Max(.01f, all.size.y), d / Mathf.Max(.01f, low.size.z));
            holder.localScale = k; holder.localPosition = new Vector3(-low.center.x * k.x, -all.min.y * k.y, -low.center.z * k.z);
            foreach (var r in holder.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
            return holder.gameObject;
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
        /// <summary>
        /// A post mill (art round 6, 2026-10-07): the Megapack's windmill on its stone podest, 13 m to the top sail, a little into the
        /// ground, a collider round the podest, and its sails turning slowly about their hub (the thinnest axis of the turbine's mesh,
        /// round the mesh's middle, so it turns true whatever the pivot).
        /// </summary>
        void Windmill(Transform t)
        {
            var go = GroundProp(t, "Megapack/Models/Buildings/Windmill", Vector3.down * .3f, 0, 13); if (go == null) return;
            var col = t.gameObject.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, 3.5f, 0); col.height = 7; col.radius = 2.4f; t.gameObject.AddComponent<NavBlocker>();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                if (mf.name.Contains("Turbine")) mf.gameObject.AddComponent<Sails>();
        }
        /// <summary>A windmill's sails turning about their hub, with the wind (2026-10-10, Chris: "windmill should be animated when wind
        /// blowing"): barely turning on a still day, about 20 degrees a second on a fair one, racing in a gale; easing up and down as the
        /// weather changes, with a slow gust. The mill now stands with the moving props, not the static scenery: static batching had
        /// frozen its sails in place.</summary>
        public sealed class Sails : MonoBehaviour
        {
            Vector3 axisLocal, hubLocal; public float degreesPerSecond = 22; float phase;
            /// <summary>Degrees a second for a wind of 0 to 1 (WorldWeather's).</summary>
            public static float SpeedFor(float wind) { return Mathf.Lerp(4, 70, Mathf.Clamp01(wind)); }
            void Start()
            {
                var m = GetComponent<MeshFilter>().sharedMesh; if (m == null) { enabled = false; return; }
                var e = m.bounds.extents; axisLocal = e.x <= e.y && e.x <= e.z ? Vector3.right : e.y <= e.z ? Vector3.up : Vector3.forward; hubLocal = m.bounds.center;
                phase = transform.position.x * .37f; if (WorldWeather.Active != null) degreesPerSecond = SpeedFor(WorldWeather.Now.wind);
            }
            void Update()
            {
                if (WorldWeather.Active != null) degreesPerSecond = Mathf.MoveTowards(degreesPerSecond, SpeedFor(WorldWeather.Now.wind) * (1 + .15f * Mathf.Sin(Time.time * .6f + phase)), 8 * Time.deltaTime);
                transform.RotateAround(transform.TransformPoint(hubLocal), transform.TransformDirection(axisLocal), degreesPerSecond * Time.deltaTime);
            }
        }
        /// <summary>The kit props the builders use, with the heights they stand at (PropCapture renders this row to look at).</summary>
        public static readonly (string path, float height)[] KitProps = {
            ("Fantasy/Barrel", .95f), ("Fantasy/Crate_Wooden", .82f), ("Fantasy/Table_Large", .8f), ("Fantasy/Stool", .45f), ("Fantasy/CandleStick", .22f), ("Fantasy/Mug", .12f),
            ("Fantasy/Chandelier", .8f), ("Fantasy/Bench", .5f), ("Fantasy/Workbench", .9f), ("Fantasy/WeaponStand", 1.6f), ("Fantasy/Whetstone", .6f), ("Fantasy/Bucket_Metal", .38f),
            ("Fantasy/FarmCrate_Apple", .34f), ("Fantasy/FarmCrate_Carrot", .34f), ("Fantasy/Dummy", 1.9f), ("Fantasy/Banner_1", 2.6f), ("Fantasy/Cauldron", .9f),
            ("Medieval/Prefabs/PileOfWoodV1", .5f), ("Medieval/Prefabs/CandleV1", .2f), ("Medieval/Prefabs/BottleV1", .28f), ("Medieval/Prefabs/MugV2", .13f), ("Medieval/Prefabs/PotV1", .3f),
            ("Medieval/Prefabs/BeerBarrelV1", .9f), ("Medieval/Prefabs/LanternV1", .4f),
            ("Nature/CommonTree_1", 7), ("Nature/CommonTree_3", 7), ("Nature/CommonTree_5", 7), ("Nature/Pine_1", 10), ("Nature/Pine_4", 10), ("Nature/Bush_Common", 1.3f), ("Nature/Bush_Common_Flowers", 1.3f),
            ("Nature/DeadTree_1", 6), ("Nature/Rock_Medium_1", 1.5f), ("Nature/Grass_Common_Tall", .5f), ("Nature/Flower_3_Group", .35f), ("Nature/TwistedTree_1", 8),
            ("Nature/Rock_Medium_2", 1.5f), ("Nature/Rock_Medium_3", 1.5f),
            ("Megapack/Models/Plants/Rock_1", 1.5f), ("Megapack/Models/Plants/Rock_2", 1.5f), ("Megapack/Models/Plants/Rock_3", 1.5f), ("Megapack/Models/Plants/Rock_4", 1.5f),
            ("Megapack/Models/Rock_Formation_1", 3), ("Megapack/Models/Plants/Rodck_Formation_2", 3), ("Megapack/Models/Plants/Rock_Formation_3", 3), ("Megapack/Models/Plants/Stone_1", .6f),
            ("Megapack/Models/Hay_1", 1.2f), ("Megapack/Models/Hay_2", 1.2f), ("Megapack/Models/Cart_1", 1.4f), ("Megapack/Models/Cart_2", 1.4f), ("Megapack/Models/Wheelbarrow", .8f),
            ("Megapack/Models/Trough_Hay", .7f), ("Megapack/Models/Market/Market_Table_1", 1), ("Megapack/Models/Stall_1", 2.6f), ("Megapack/Models/Market/Crate_1_Apples", .5f),
            ("Megapack/Models/Arrow Target", 1.5f), ("Megapack/Models/Plants/TreeTrunk_1", .8f), ("Megapack/Models/Tree_Broken_1", 2), ("Megapack/Models/WodenLog_Cuted", .6f),
            ("Megapack/Models/Fireplace", .6f), ("Megapack/Models/Barrel_1", .95f),
            ("Megapack/Models/Buildings/Buiilding_6_1", 7.6f), ("Megapack/Models/Buildings/Stable_1", 4.1f), ("Megapack/Models/Buildings/Tower_1", 9.5f), ("Megapack/Models/Buildings/Windmill", 13),
            ("Village/Wall_Plaster_Straight", 3), ("Village/Wall_Plaster_Door_Round", 3), ("Village/Wall_Plaster_Window_Wide_Round", 3), ("Village/Wall_UnevenBrick_Straight", 3),
            ("Village/Roof_RoundTiles_6x8", 3), ("Village/Floor_WoodDark", .3f), ("Village/Door_1_Round", 2.4f), ("Village/Prop_Chimney", 2.5f), ("Village/Corner_Exterior_Wood", 3), ("Village/Stairs_Exterior_Straight", 1.5f),
            // The KayKit Dungeon Pack at its own sizes (the chapter-house's rooms, 2026-10-15): walls 4 m, pillars 4 m, the stair 5.1 m to its rail.
            ("KayKit/wall", 4), ("KayKit/wall_doorway", 4), ("KayKit/wall_arched", 4), ("KayKit/wall_doorway_sides", 4), ("KayKit/wall_archedwindow_open", 4), ("KayKit/wall_window_open", 4),
            ("KayKit/wall_corner", 4), ("KayKit/wall_pillar", 4), ("KayKit/wall_broken", 4), ("KayKit/wall_cracked", 4), ("KayKit/wall_half", 4), ("KayKit/wall_endcap", 4),
            ("KayKit/pillar", 4), ("KayKit/pillar_decorated", 4), ("KayKit/column", 1.4f), ("KayKit/stairs", 5.1f), ("KayKit/stairs_wide", 5.1f), ("KayKit/stairs_narrow", 5.1f),
            ("KayKit/floor_tile_large", .15f), ("KayKit/floor_tile_small", .15f), ("KayKit/floor_tile_small_broken_A", .15f), ("KayKit/floor_tile_large_rocks", .64f), ("KayKit/ceiling_tile", .35f),
            ("KayKit/torch_mounted", 1.06f), ("KayKit/torch_lit", 1.13f), ("KayKit/banner_red", 3.2f), ("KayKit/banner_triple_red", 3.2f), ("KayKit/banner_shield_red", 3.2f),
            ("KayKit/table_long", 1), ("KayKit/table_medium", 1), ("KayKit/chair", 1.23f), ("KayKit/stool", .5f), ("KayKit/shelves", 1.95f), ("KayKit/chest", 1.3f), ("KayKit/candle_lit", 1.05f),
            ("KayKit/candle_triple", .87f), ("KayKit/barrel_large", 2), ("KayKit/box_large", 1.5f), ("KayKit/crates_stacked", 2.14f), ("KayKit/keg", 2.05f), ("KayKit/rubble_half", 3.5f),
            ("KayKit/sword_shield", 1.67f), ("KayKit/barrier", 1.1f), ("KayKit/bed_frame", 1.06f), ("KayKit/trunk_large_A", 1),
            // The Dungeon Modular Pack at its own sizes (metres from its centimetre files).
            ("DungeonModular/Wall_A", 4), ("DungeonModular/Wall_B", 4), ("DungeonModular/Wall_C", 2), ("DungeonModular/Pillar_A", 4), ("DungeonModular/Pillar_B", 2), ("DungeonModular/Arch_A", 3.46f),
            ("DungeonModular/Step_A", 2), ("DungeonModular/Tile_B", .2f), ("DungeonModular/Torch_A", .72f), ("DungeonModular/Torch_B", .71f), ("DungeonModular/Handrail_A", 1.19f), ("DungeonModular/Handrail_B", 1.25f),
            // The Fantasy FREE market pieces (2026-10-15; ZoneBuilder.Market has them by variant, at the pack's own metre heights).
            ("Fantasy2/stall_001", 2.57f), ("Fantasy2/stall_table_001", 1.08f), ("Fantasy2/table_001", .78f), ("Fantasy2/jug_001", .34f), ("Fantasy2/jug_002", .51f), ("Fantasy2/jug_003", .57f),
            ("Fantasy2/jug_004", .33f), ("Fantasy2/jug_005", .45f), ("Fantasy2/plate_001", .07f), ("Fantasy2/plate_002", .07f), ("Fantasy2/plate_003", .07f), ("Fantasy2/bag_001", .57f),
            ("Fantasy2/bag_002", .57f), ("Fantasy2/bag_003", .57f), ("Fantasy2/bag_004", .57f), ("Fantasy2/box_001", .54f), ("Fantasy2/box_001_001", .34f), ("Fantasy2/box_002", .17f),
            ("Fantasy2/box_003", .32f), ("Fantasy2/bucket_001", .48f), ("Fantasy2/barrel_001", .73f), ("Fantasy2/holder_001", 2.01f), ("Fantasy2/pointer_001", 2.17f), ("Fantasy2/log_001", 1.81f),
            ("Fantasy2/log_002", .74f), ("Fantasy2/log_003", .32f), ("Fantasy2/log_004", .54f), ("Fantasy2/cart_001", 1.16f),
            // The Fantasy kit's pieces the dungeons and rooms took up (playtest note 234, PropDressing) and the food models (note 235, FoodModels).
            ("Fantasy/SmallBottle", .12f), ("Fantasy/Book_7", .07f), ("Fantasy/Cabinet", 1f), ("Fantasy/Shelf_Simple", .3f),
            ("Fantasy/Bag", 0.55f), ("Fantasy/Rope_1", 0.08f), ("Fantasy/Rope_2", 0.06f), ("Fantasy/Rope_3", 0.1f), ("Fantasy/Bucket_Wooden_1", 0.3f), ("Fantasy/Shield_Wooden", 0.6f),
            ("Fantasy/Barrel_Apples", 0.9f), ("Fantasy/Barrel_Holder", 1.2f), ("Fantasy/Pickaxe_Bronze", 1f), ("Fantasy/Cage_Small", 0.85f), ("Fantasy/Chain_Coil", 0.08f), ("Fantasy/Vase_2", 0.5f),
            ("Fantasy/Vase_4", 0.45f), ("Fantasy/Vase_Rubble_Medium", 0.16f), ("Fantasy/Candle_1", 0.13f), ("Fantasy/Candle_2", 0.25f), ("Fantasy/CandleStick_Stand", 1.3f), ("Fantasy/CandleStick_Triple", 0.4f),
            ("Fantasy/Potion_1", 0.1f), ("Fantasy/Potion_2", 0.26f), ("Fantasy/Potion_4", 0.2f), ("Fantasy/Bottle_1", 0.24f), ("Fantasy/Pot_1", 0.2f), ("Fantasy/Pot_1_Lid", .07f),
            ("Fantasy/Book_Stack_1", 0.2f), ("Fantasy/Book_Stack_2", 0.2f), ("Fantasy/BookGroup_Small_1", 0.3f), ("Fantasy/BookGroup_Small_2", 0.28f), ("Fantasy/Bookcase_2", 2.4f), ("Fantasy/BookStand", 1.4f),
            ("Fantasy/Scroll_1", 0.05f), ("Fantasy/Scroll_2", 0.06f), ("Fantasy/Lantern_Wall", 0.55f), ("Fantasy/Peg_Rack", 0.35f), ("Fantasy/Crate_Metal", 0.8f), ("Fantasy/Shelf_Small_Bottles", 0.65f),
            ("Fantasy/Chalice", 0.2f), ("Fantasy/Coin_Pile", 0.07f), ("Fantasy/Coin_Pile_2", 0.06f), ("Fantasy/Key_Gold", 0.012f), ("Fantasy/Key_Metal", 0.012f), ("Fantasy/Pouch_Large", 0.14f),
            ("Food/Apple", .08f), ("Food/Apple_Green", .08f), ("Food/Orange", .075f), ("Food/Bread", .15f), ("Food/Bread_Slice", .1f), ("Food/Carrot", .2f),
            ("Food/Turnip", .17f), ("Food/Lettuce_Whole", .17f), ("Food/Mushroom", .06f), ("Food/Mushroom_Sliced", .025f), ("Food/Fish", .035f), ("Food/FishBone", .05f),
            ("Food/ChickenLeg", .066f), ("Food/Steak", .036f), ("Food/Sausage_Raw", .022f), ("Food/Sausage_Cooked", .022f), ("Food/Bacon_Uncooked", .006f), ("Food/Bacon_Cooked", .006f),
            ("Food/Egg_Whole", .055f), ("Food/Egg_Whole_White", .055f), ("Food/Egg_Fried", .012f), ("Food/Pancake", .027f), ("Food/Croissant", .06f), ("Food/CookingPot", .2f),
            ("Food/CookingPot_Soup", .2f), ("Food/CookingPot2", .11f), ("Food/CookingPot2_Soup", .11f), ("Food/FryingPan", .04f), ("Food/Plate", .035f), ("Food/Plate2", .033f),
            ("Food/Plate_Square", .021f), ("Food/Bottle1", .3f), ("Food/Bottle2", .3f), ("Food/Jar_Large", .25f), ("Food/Knife", .003f), ("Food/Fork", .005f),
            ("Food/Spoon", .008f),
        };
    }
}
