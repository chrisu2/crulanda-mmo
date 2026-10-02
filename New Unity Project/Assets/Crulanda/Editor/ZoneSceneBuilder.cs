using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// Creates the shared zone art palette (materials + small generated surface textures) and the Oakhaven scene.
    /// Existing materials and the scene are preserved; rerunning only fills in anything missing and fixes build settings.
    /// </summary>
    public static partial class ZoneSceneBuilder
    {
        const string ArtRoot = "Assets/Crulanda/World/Art";
        public const string OakhavenScene = "Assets/Crulanda/Scenes/Oakhaven.unity";
        const string OakhavenJson = "Assets/Crulanda/EncounterContent/Zones/oakhaven.json";
        public const string ProfessionsFolder = "Assets/Crulanda/EncounterContent/Professions";

        [MenuItem("Crulanda/World/Build Oakhaven")]
        public static void BuildOakhaven()
        {
            var art = EnsureArt();
            if (!File.Exists(OakhavenScene)) CreateScene(art);
            else Debug.Log("Oakhaven scene already exists; preserving it (only the zone registry is refreshed).");
            RegisterZones();
            RegisterQuests();
            RegisterLootDraft();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(OakhavenScene, true), new EditorBuildSettingsScene(EncounterBuilder.ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("OAKHAVEN_BUILT");
        }
        /// <summary>Points the encounter content at every quest, item and profession JSON (EncounterContent/Quests, Items, Professions), so new files are never left out.</summary>
        static void RegisterQuests()
        {
            var content = AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            if (content == null) throw new Exception("Encounter content missing.");
            var files = new System.Collections.Generic.List<TextAsset>();
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/Crulanda/EncounterContent/Quests" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".json")) files.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
            }
            files.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            content.questFiles = files.ToArray();
            var items = new System.Collections.Generic.List<TextAsset>();
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/Crulanda/EncounterContent/Items" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".json")) items.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
            }
            content.itemFiles = items.ToArray();
            // Trades (EncounterContent/Professions): the folder may be missing in an older checkout, and FindAssets warns about that.
            var trades = new System.Collections.Generic.List<TextAsset>();
            if (AssetDatabase.IsValidFolder(ProfessionsFolder))
                foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { ProfessionsFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".json")) trades.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
                }
            trades.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            content.professionFiles = trades.ToArray();
            EditorUtility.SetDirty(content);
            Debug.Log("Registered " + files.Count + " quest files and " + items.Count + " item files.");
            Debug.Log("Registered " + trades.Count + " profession files.");
        }
        /// <summary>
        /// Points Resources/Gear/LootDraft.asset at the drafted loot files (EncounterContent/Loot) so the wardrobe capture can read
        /// them in a player. The game itself does not load them until step L2 moves them into EncounterContent/Items.
        /// </summary>
        static void RegisterLootDraft()
        {
            var files = new System.Collections.Generic.List<TextAsset>();
            if (AssetDatabase.IsValidFolder(LootDraft.Folder))
                foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { LootDraft.Folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".json")) files.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
                }
            files.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            const string asset = "Assets/Crulanda/Resources/" + LootDraft.ResourcePath + ".asset";
            var draft = AssetDatabase.LoadAssetAtPath<LootDraft>(asset);
            if (draft == null)
            {
                if (files.Count == 0) return;
                draft = ScriptableObject.CreateInstance<LootDraft>(); AssetDatabase.CreateAsset(draft, asset);
            }
            draft.files = files.ToArray();
            EditorUtility.SetDirty(draft);
            Debug.Log("Registered " + files.Count + " drafted loot files for the wardrobe.");
        }

        static void CreateScene(ZoneArt art)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera"); camera.tag = "MainCamera";
            var view = camera.AddComponent<Camera>(); view.fieldOfView = 55; view.nearClipPlane = .2f; view.farClipPlane = 260;
            camera.AddComponent<AudioListener>();
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.shadows = LightShadows.Soft;
            var world = new GameObject("Oakhaven");
            var zone = world.AddComponent<ZoneBuilder>(); zone.art = art; zone.zoneJson = AssetDatabase.LoadAssetAtPath<TextAsset>(OakhavenJson);
            world.AddComponent<EncounterNavigation>();
            var session = world.AddComponent<EncounterSession>(); session.content = AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            world.AddComponent<EncounterHud>().session = session;
            if (zone.zoneJson == null || session.content == null) throw new Exception("Oakhaven zone JSON or encounter content missing.");
            EditorSceneManager.SaveScene(scene, OakhavenScene);
        }

        /// <summary>Points the scene's ZoneBuilder at every zone JSON so travel can build any of them. Touches nothing else.</summary>
        static void RegisterZones()
        {
            var zones = new System.Collections.Generic.List<TextAsset>();
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/Crulanda/EncounterContent/Zones" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && AssetDatabase.GUIDToAssetPath(guid).EndsWith(".json")) zones.Add(asset);
            }
            var scene = EditorSceneManager.OpenScene(OakhavenScene, OpenSceneMode.Single);
            var builder = UnityEngine.Object.FindFirstObjectByType<ZoneBuilder>();
            if (builder == null) throw new Exception("Oakhaven scene has no ZoneBuilder.");
            builder.zones = zones.ToArray(); EditorUtility.SetDirty(builder);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("Registered " + zones.Count + " zones.");
        }

        static ZoneArt EnsureArt()
        {
            Directory.CreateDirectory(ArtRoot);
            string path = ArtRoot + "/ZoneArt.asset";
            var art = AssetDatabase.LoadAssetAtPath<ZoneArt>(path);
            if (art == null) { art = ScriptableObject.CreateInstance<ZoneArt>(); AssetDatabase.CreateAsset(art, path); }
            var noise = Tex("noise_fine", 256, (x, y) => Grey(.82f + Perlin(x, y, .09f) * .18f + Perlin(x, y, .4f) * .06f));
            var wood = Tex("wood_grain", 256, (x, y) => Grey(.7f + Mathf.Sin((x * .05f + Perlin(x, y, .02f) * 6)) * .12f + Perlin(x, y * .08f, .5f) * .15f));
            var stone = Tex("stone_blotch", 256, (x, y) => Grey(.68f + Perlin(x, y, .05f) * .25f + Perlin(x, y, .3f) * .1f - (Perlin(x, y, .12f) > .72f ? .15f : 0)));
            var straw = Tex("thatch_straw", 256, (x, y) => Grey(.66f + Perlin(x * .15f, y, .9f) * .3f + Perlin(x, y, .07f) * .1f));
            var slate = Tex("slate_rows", 256, (x, y) => Grey(.72f + ((y % 32) < 3 ? -.3f : 0) + (((x + (y / 32 % 2) * 21) % 42) < 2 ? -.2f : 0) + Perlin(x, y, .2f) * .12f));
            var ripple = Tex("water_ripple", 256, (x, y) => Grey(.75f + Perlin(x, y * 3, .06f) * .25f));
            var mote = Tex("mote", 64, (x, y) => { float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 32; float a = Mathf.Clamp01(1 - d); return new Color(1, 1, 1, a * a); }, true);

            art.ground = art.ground ?? Standard("Ground", Color.white, null, .05f);
            art.plaster = art.plaster ?? Standard("Plaster", new Color(.78f, .72f, .6f), noise, .08f);
            art.timber = art.timber ?? Standard("Timber", new Color(.32f, .22f, .15f), wood, .1f);
            art.thatch = art.thatch ?? Standard("Thatch", new Color(.58f, .47f, .27f), straw, .03f);
            art.slate = art.slate ?? Standard("Slate", new Color(.33f, .35f, .38f), slate, .2f);
            art.stone = art.stone ?? Standard("Stone", new Color(.52f, .51f, .48f), stone, .1f);
            art.bark = art.bark ?? Standard("Bark", new Color(.28f, .24f, .21f), wood, .05f);
            art.foliage = art.foliage ?? Standard("Foliage", new Color(.35f, .38f, .18f), noise, .05f);
            art.pine = art.pine ?? Standard("Pine", new Color(.18f, .26f, .18f), noise, .05f);
            art.soil = art.soil ?? Standard("Soil", new Color(.3f, .23f, .16f), noise, .02f);
            art.hay = art.hay ?? Standard("Hay", new Color(.74f, .62f, .34f), straw, .02f);
            art.cloth = art.cloth ?? Standard("Cloth", new Color(.55f, .45f, .35f), noise, .05f);
            art.metal = art.metal ?? Standard("Metal", new Color(.42f, .42f, .45f), null, .6f, .6f);
            art.ash = art.ash ?? Standard("Unmade", new Color(.62f, .62f, .65f), noise, .02f);
            if (art.glass == null)
            {
                art.glass = Standard("Window glow", new Color(.95f, .72f, .38f), null, .5f);
                art.glass.EnableKeyword("_EMISSION"); art.glass.SetColor("_EmissionColor", new Color(1f, .62f, .25f) * 1.3f);
                art.glass.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; EditorUtility.SetDirty(art.glass);
            }
            if (art.water == null)
            {
                art.water = Standard("Creek water", new Color(.2f, .3f, .31f, .78f), ripple, .88f);
                Fade(art.water);
            }
            if (art.veil == null)
            {
                art.veil = Standard("Wasting veil", new Color(.86f, .86f, .9f, .75f), null, 0);
                Fade(art.veil); art.veil.EnableKeyword("_EMISSION"); art.veil.SetColor("_EmissionColor", new Color(.42f, .42f, .46f));
                art.veil.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; EditorUtility.SetDirty(art.veil);
            }
            if (art.particle == null)
            {
                art.particle = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = mote };
                AssetDatabase.CreateAsset(art.particle, ArtRoot + "/Wasting motes.mat");
            }
            if (art.skybox == null)
            {
                art.skybox = new Material(Shader.Find("Skybox/Procedural"));
                art.skybox.SetFloat("_SunSize", .035f); art.skybox.SetFloat("_AtmosphereThickness", 1.15f);
                art.skybox.SetColor("_SkyTint", new Color(.56f, .58f, .62f)); art.skybox.SetColor("_GroundColor", new Color(.37f, .35f, .31f));
                art.skybox.SetFloat("_Exposure", 1.05f);
                AssetDatabase.CreateAsset(art.skybox, ArtRoot + "/Overcast sky.mat");
            }
            // Grass and wildflower tufts: instanced, alpha-cutout (so the variants are kept in builds).
            var blades = Tex("grass_blades", 128, (x, y) => {
                float a = 0;
                for (int b = 0; b < 9; b++)
                {
                    float cx = 8 + b * 14 + Mathf.Sin(b * 3.1f) * 5, h = 70 + (b * 37 % 50), lean = (b % 3 - 1) * .18f;
                    float bx = cx + lean * y, width = 5.5f * (1 - y / h);
                    if (y < h && Mathf.Abs(x - bx) < width) a = 1;
                }
                float shade = .55f + .45f * y / 127f;
                return new Color(shade, shade, shade, a);
            }, true);
            var bloom = Tex("flower_tuft", 128, (x, y) => {
                float a = 0; Color c = new Color(.5f, .62f, .38f);
                for (int b = 0; b < 5; b++)
                {
                    float cx = 14 + b * 25, h = 60 + (b * 29 % 40);
                    if (y < h && Mathf.Abs(x - cx) < 2.5f) a = 1;
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(cx, h + 6)) < 9) { a = 1; c = Color.white; }
                }
                return new Color(c.r, c.g, c.b, a);
            }, true);
            if (art.grass == null || art.grass.Length == 0)
                art.grass = new[] { Cutout("Grass A", new Color(.42f, .52f, .26f), blades), Cutout("Grass B", new Color(.52f, .54f, .28f), blades), Cutout("Grass C", new Color(.36f, .45f, .24f), blades) };
            if (art.flowers == null || art.flowers.Length == 0)
                art.flowers = new[] { Cutout("Flowers yellow", new Color(.95f, .85f, .35f), bloom), Cutout("Flowers violet", new Color(.72f, .6f, .9f), bloom), Cutout("Flowers white", new Color(.95f, .95f, .92f), bloom) };
            // Fine grain on the painted ground so it holds up close (detail x2 needs a texture averaging mid-grey).
            var grain = Tex("detail_grain", 256, (x, y) => Grey(.5f + (Perlin(x, y, .19f) - .5f) * .35f + (Perlin(x, y, .7f) - .5f) * .2f));
            if (art.ground.GetTexture("_DetailAlbedoMap") == null)
            {
                art.ground.SetTexture("_DetailAlbedoMap", grain); art.ground.EnableKeyword("_DETAIL_MULX2"); EditorUtility.SetDirty(art.ground);
            }
            if (art.worldMap == null)
            {
                // The author's overworld map from the lore folder becomes the in-game world map.
                string target = ArtRoot + "/crulanda_overworld_map.png";
                foreach (var source in new[] { @"D:\code\crulanda\maps\crulanda_overworld_map.png", @"D:\code\crulanda\crulanda_overworld_map.png" })
                    if (!File.Exists(target) && File.Exists(source)) File.Copy(source, target);
                if (File.Exists(target))
                {
                    AssetDatabase.ImportAsset(target);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(target);
                    importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = 2048; importer.mipmapEnabled = false; importer.SaveAndReimport();
                    art.worldMap = AssetDatabase.LoadAssetAtPath<Texture2D>(target);
                }
                else Debug.LogWarning("Overworld map image not found; world map will be blank.");
            }
            // ---------- nature pass: water surface, falling leaves and ash, splashes, wind in the grass ----------
            var ripples = NormalTex("water_normal", 256);
            if (art.waterSurface == null)
            {
                art.waterSurface = new Material(Shader.Find("Crulanda/Water")) { name = "Water surface" };
                art.waterSurface.SetTexture("_Normal", ripples);
                AssetDatabase.CreateAsset(art.waterSurface, ArtRoot + "/Water surface.mat");
            }
            var leafTex = Tex("leaf", 64, (x, y) => {
                // A pointed oval leaf with a darker midrib, tip up.
                float u = (x - 31.5f) / 20f, v = (y - 34f) / 28f; float edge = 1 - v * v;
                bool inside = v > -1 && v < 1 && Mathf.Abs(u) < Mathf.Max(0, edge) * (1 - Mathf.Max(0, v) * .5f);
                bool rib = Mathf.Abs(x - 31.5f) < 1.2f && y > 8 && y < 60; bool stem = Mathf.Abs(x - 31.5f) < 1.2f && y <= 8 && y > 2;
                float shade = rib ? .65f : .85f + Perlin(x, y, .3f) * .15f;
                return new Color(shade, shade, shade, inside || stem ? 1 : 0);
            }, true);
            var flakeTex = Tex("ash_flake", 64, (x, y) => {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 22f; float ragged = Perlin(x, y, .25f) * .6f;
                float a = Mathf.Clamp01((1 - d - ragged * .4f) * 3); float g = .8f + Perlin(x, y, .5f) * .2f; return new Color(g, g, g, a);
            }, true);
            if (art.leaf == null) { art.leaf = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = leafTex, name = "Falling leaf" }; AssetDatabase.CreateAsset(art.leaf, ArtRoot + "/Falling leaf.mat"); }
            if (art.ashFlake == null) { art.ashFlake = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = flakeTex, name = "Ash flake" }; AssetDatabase.CreateAsset(art.ashFlake, ArtRoot + "/Ash flake.mat"); }
            if (art.splash == null) { art.splash = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = mote, name = "Splash" }; AssetDatabase.CreateAsset(art.splash, ArtRoot + "/Splash.mat"); }
            if (art.post == null) { art.post = new Material(Shader.Find("Hidden/Crulanda/Post")) { name = "Post" }; AssetDatabase.CreateAsset(art.post, ArtRoot + "/Post.mat"); }
            if (art.fade == null) { art.fade = new Material(Shader.Find("Crulanda/Fade")) { name = "Tree fade" }; AssetDatabase.CreateAsset(art.fade, ArtRoot + "/Tree fade.mat"); }
            if (art.clouds == null) { art.clouds = new Material(Shader.Find("Crulanda/Clouds")) { name = "Clouds" }; AssetDatabase.CreateAsset(art.clouds, ArtRoot + "/Clouds.mat"); }
            // ---------- painted leaf cards: the crowns of broadleaf, orchard and pine trees (ZoneBuilder.LeafCrown, Pine) ----------
            // Saturated painted colour lives in the textures (dark, mid, light of each leaf); ZoneBuilder's tints stay near white.
            var leafGreen = LeafClusterTex("leaf_cluster_green", 11, new Color(.14f, .29f, .13f), new Color(.36f, .57f, .20f), new Color(.72f, .82f, .32f));
            var leafYellow = LeafClusterTex("leaf_cluster_yellow", 23, new Color(.26f, .33f, .10f), new Color(.60f, .62f, .16f), new Color(.92f, .84f, .34f));
            var leafAutumn = LeafClusterTex("leaf_cluster_autumn", 37, new Color(.42f, .14f, .06f), new Color(.82f, .38f, .10f), new Color(.98f, .72f, .24f));
            // The bough is pine_bough_full, a fuller feather than the first pine_bough (Tex writes a PNG once, so a repaint takes a new
            // name); a Pine bough material made earlier is pointed at it.
            var pineBough = PineBoughTex("pine_bough_full", new Color(.09f, .19f, .13f), new Color(.24f, .42f, .22f), new Color(.52f, .62f, .28f));
            if (art.leafCards == null || art.leafCards.Length == 0)
                art.leafCards = new[] { LeafCard("Leaf green", leafGreen, .07f), LeafCard("Leaf yellow", leafYellow, .07f), LeafCard("Leaf autumn", leafAutumn, .08f) };
            if (art.pineBough == null) art.pineBough = LeafCard("Pine bough", pineBough, .04f);
            else if (art.pineBough.mainTexture != pineBough) { art.pineBough.mainTexture = pineBough; EditorUtility.SetDirty(art.pineBough); }
            // Grass and flowers sway in the wind (same textures and tints; the wind shader keeps instancing).
            var grassShader = Shader.Find("Crulanda/Grass");
            if (grassShader != null)
                foreach (var m in System.Linq.Enumerable.Concat(art.grass ?? new Material[0], art.flowers ?? new Material[0]))
                {
                    if (m == null || m.shader == grassShader) continue;
                    var tex = m.mainTexture; var col = m.color;
                    m.shader = grassShader; m.mainTexture = tex; m.color = col; m.SetFloat("_Cutoff", .45f);
                    m.SetFloat("_Wind", m.name.StartsWith("Flowers") ? .08f : .13f); m.enableInstancing = true; EditorUtility.SetDirty(m);
                }
            // ---------- painted plants (PlantField): ferns in the woods' shade, broad leaves by the water, reeds at the waterline ----------
            var fernTex = FernFrondTex("fern_frond", new Color(.08f, .2f, .09f), new Color(.24f, .45f, .17f), new Color(.58f, .76f, .3f));
            var broadTex = BroadLeafTex("broad_leaf", new Color(.09f, .21f, .1f), new Color(.23f, .43f, .17f), new Color(.5f, .68f, .27f));
            var reedTex = ReedTex("reed_clump", new Color(.19f, .29f, .13f), new Color(.37f, .49f, .2f), new Color(.68f, .7f, .38f), new Color(.31f, .19f, .1f));
            if (art.fern == null) art.fern = PlantCard("Fern", fernTex, .07f);
            if (art.broadLeaf == null) art.broadLeaf = PlantCard("Broad leaf", broadTex, .05f);
            if (art.reeds == null) art.reeds = PlantCard("Reeds", reedTex, .12f);
            PaintedTextures(art);   // the painted style pass: plaster, thatch, slate and timber repainted, and the masonry material (ZoneSceneBuilder.Painted.cs)
            EnsurePaintedRock(art);   // and the painted natural rock (ZoneSceneBuilder.PaintedRock.cs)
            EnsurePaintedCave(art);   // and the cave walls, rock and earth (ZoneSceneBuilder.PaintedCave.cs)
            EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
            return art;
        }
        /// <summary>A tileable ripple normal map (sums of whole-period sine waves), imported as a normal map.</summary>
        static Texture2D NormalTex(string name, int size)
        {
            string path = ArtRoot + "/" + name + ".png";
            if (!File.Exists(path))
            {
                var waves = new (int kx, int ky, float amp, float phase)[] { (3, 1, 1, 0), (-2, 4, .8f, 1.3f), (5, -3, .5f, 2.1f), (1, 7, .35f, .7f), (-7, 2, .3f, 2.9f), (9, 5, .18f, 1.9f), (-4, -9, .15f, .4f) };
                var t = new Texture2D(size, size, TextureFormat.RGB24, false);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = 0, dy = 0;
                        foreach (var w in waves)
                        {
                            float arg = 2 * Mathf.PI * (w.kx * x + w.ky * y) / size + w.phase, c = Mathf.Cos(arg) * w.amp * 2 * Mathf.PI / size;
                            dx += c * w.kx; dy += c * w.ky;
                        }
                        var n = new Vector3(-dx * 18, -dy * 18, 1).normalized;
                        t.SetPixel(x, y, new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f));
                    }
                File.WriteAllBytes(path, t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.NormalMap; importer.wrapMode = TextureWrapMode.Repeat; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        /// <summary>A painted leaf-card material: Crulanda/Leaf (alpha cut, both faces, wind sway keyed to world position), or a one-sided
        /// Standard cutout when that shader is missing. White tint: the paint carries the colour; ZoneBuilder tints per tree.</summary>
        static Material LeafCard(string name, Texture2D tex, float wind)
        {
            var shader = Shader.Find("Crulanda/Leaf");
            if (shader == null) { Debug.LogWarning("Crulanda/Leaf shader missing: " + name + " is a one-sided Standard cutout."); return Cutout(name, Color.white, tex); }
            var m = new Material(shader) { name = name, color = Color.white, mainTexture = tex };
            m.SetFloat("_Cutoff", .45f); m.SetFloat("_Wind", wind); m.SetFloat("_Glossiness", .05f);
            AssetDatabase.CreateAsset(m, ArtRoot + "/" + name + ".mat"); return m;
        }
        /// <summary>
        /// A painted cluster of lobed leaves on a twig (alpha cut, tip up): the twig runs up from the bottom edge (v = 0, where the
        /// card meets its bough) and ten to twelve leaves fan up and out from it, overlapping, the low ones reaching sideways and the
        /// top ones up. The outer and upper leaves are lighter, the inner lower ones darker; each has a lighter half, a darker base, a
        /// dark painted rim and a midrib; the gaps between them are clear, so sky shows through a crown and its edge is ragged leaf.
        /// Colours run dark (shade) through mid to light (sunlit): the paint carries the hue shift, the tint doesn't have to.
        /// </summary>
        static Texture2D LeafClusterTex(string name, int seed, Color dark, Color mid, Color light)
        {
            var r = new System.Random(seed); float R() { return (float)r.NextDouble(); }
            var leaves = new System.Collections.Generic.List<(Vector2 at, float angle, float len, float wide, int lobes, float shade, float side)>();
            int n = 10 + (int)(R() * 3);
            for (int k = 0; k < n; k++)
            {
                float u = (float)k / (n - 1), side = k % 2 == 0 ? -1 : 1;   // up the twig; which side the leaf leaves it on
                float angle = u > .82f ? (R() - .5f) * 30 : side * (28 + R() * 42) * (1 - u * .35f);   // degrees from straight up
                leaves.Add((new Vector2(64 + (R() - .5f) * 8 + side * u * 4, 10 + u * 44), angle, 34 + R() * 16 - u * 4, .46f + R() * .14f, R() < .5f ? 3 : 4, .3f + u * .45f + R() * .25f, R() < .5f ? -1 : 1));
            }
            leaves.Sort((a, b) => a.shade.CompareTo(b.shade));   // painter's order: the deeper (darker) leaves first, the lighter ones over them
            Color Ramp(float s) { s = Mathf.Clamp01(s); return s < .5f ? Color.Lerp(dark, mid, s * 2) : Color.Lerp(mid, light, (s - .5f) * 2); }
            return Tex(name, 128, (x, y) => {
                var p = new Vector2(x + .5f, y + .5f); Color c = mid; float a = 0;   // clear pixels keep the mid colour: no dark halo once mipped
                if (y < 60 && Mathf.Abs(x - 63.5f) < 1.6f - y * .012f) { c = new Color(.24f, .17f, .10f); a = 1; }   // the twig, under everything
                foreach (var leaf in leaves)
                {
                    float rad = leaf.angle * Mathf.Deg2Rad; var dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)); var across = new Vector2(dir.y, -dir.x);
                    var d = p - leaf.at; float u = Vector2.Dot(d, dir) / leaf.len, v = Vector2.Dot(d, across) / leaf.len;
                    if (u <= 0 || u >= 1) continue;
                    // A lobed leaf: widest below its middle, pointed at both ends, scalloped along its edge.
                    float half = leaf.wide * .5f * Mathf.Pow(Mathf.Sin(u * Mathf.PI), .55f) * (1 - u * .25f) * (.76f + .24f * Mathf.Abs(Mathf.Cos(u * Mathf.PI * leaf.lobes)));
                    float edge = half - Mathf.Abs(v); if (edge <= 0) continue;
                    float shade = leaf.shade + (v * leaf.side > 0 ? .1f : -.04f) - (1 - u) * .12f + (Perlin(x, y, .3f) - .5f) * .14f;
                    if (edge < 1.4f / leaf.len) shade -= .28f;                                    // the painted rim
                    if (Mathf.Abs(v) < .9f / leaf.len && u > .08f && u < .92f) shade -= .12f;    // the midrib
                    c = Ramp(shade); a = 1;
                }
                return new Color(c.r, c.g, c.b, a);
            }, true);
        }
        /// <summary>
        /// A painted pine bough (alpha cut): a twig up the middle from the trunk end (v = 0) to the tip, with a solid body of needles
        /// either side, painted as needles lying out and forward at a slant, darker in at the twig and lighter out at the edge, and a
        /// comb of needle tips standing past the body, so the edge is ragged. A full feather: the body is opaque and takes most of
        /// the card's width from close to the trunk end (two thirds of it a third of the way out), narrowing only toward the tip, so
        /// the boughs carry a tier's mass and overlap; thin lines would mip away at a distance and leave a pine sparse.
        /// </summary>
        static Texture2D PineBoughTex(string name, Color dark, Color mid, Color light)
        {
            Color Ramp(float s) { s = Mathf.Clamp01(s); return s < .5f ? Color.Lerp(dark, mid, s * 2) : Color.Lerp(mid, light, (s - .5f) * 2); }
            return Tex(name, 128, (x, y) => {
                float u = (y - 4) / 118f, v = (x - 63.5f) / 128f;   // along the twig (0 at the trunk end, 1 at the tip); across, in texture widths
                if (u <= 0 || u >= 1) return new Color(mid.r, mid.g, mid.b, 0);
                float body = .40f * Mathf.Pow(Mathf.Sin(u * Mathf.PI), .35f) * (1 - u * .4f);            // near full width from close to the trunk end, pointed only at the tip
                float slant = Mathf.Abs(v) * 128 * .9f - u * 118 * .55f;                                 // constant along a needle: they lie out and forward
                float comb = .06f * (1 - u * .5f) * Mathf.Pow(Mathf.Abs(Mathf.Sin(slant * .55f)), 3);    // needle tips standing past the body
                float edge = body + comb - Mathf.Abs(v);
                if (edge <= 0) return new Color(mid.r, mid.g, mid.b, 0);
                float across = Mathf.Abs(v) / Mathf.Max(.02f, body + comb), stripe = Mathf.Sin(slant * .55f) * .5f + .5f;
                float shade = .2f + across * .5f + stripe * .22f + (Perlin(x, y, .2f) - .5f) * .12f;
                if (edge < .012f) shade += .12f;                              // lit needle tips
                if (Mathf.Abs(v) < .012f * (1 - u * .6f)) shade -= .2f;       // the twig's shadow line
                var c = Ramp(shade); return new Color(c.r, c.g, c.b, 1);
            }, true);
        }
        /// <summary>A painted plant card for PlantField: Crulanda/Grass (alpha cut, both faces, sways from the root, instanced). White
        /// tint: the paint carries the colour; PlantField tints per zone (a withered Khaven fern).</summary>
        static Material PlantCard(string name, Texture2D tex, float wind)
        {
            var shader = Shader.Find("Crulanda/Grass");
            if (shader == null) { Debug.LogWarning("Crulanda/Grass shader missing: " + name + " is a Standard cutout."); return Cutout(name, Color.white, tex); }
            var m = new Material(shader) { name = name, color = Color.white, mainTexture = tex, enableInstancing = true };
            m.SetFloat("_Cutoff", .45f); m.SetFloat("_Wind", wind);
            AssetDatabase.CreateAsset(m, ArtRoot + "/" + name + ".mat"); return m;
        }
        static Color Ramp3(Color dark, Color mid, Color light, float s) { s = Mathf.Clamp01(s); return s < .5f ? Color.Lerp(dark, mid, s * 2) : Color.Lerp(mid, light, (s - .5f) * 2); }
        /// <summary>
        /// A painted fern frond (alpha cut, tip up; v = 0 at the root): a rachis curving a little to one side, and pinnae in pairs
        /// either side of it, longest a third of the way up and tapering to the tip, each a narrow pointed leaflet reaching up and
        /// out. Darker toward the root and the rachis, lighter at the leaflets' ends and the frond's tip; a painted rim and midrib.
        /// </summary>
        static Texture2D FernFrondTex(string name, Color dark, Color mid, Color light)
        {
            float Rachis(float y) { float t = y / 124f; return 64 + 5 * t * t; }
            return Tex(name, 128, (x, y) => {
                var p = new Vector2(x + .5f, y + .5f); Color c = mid; float a = 0;
                if (y > 2 && y < 124 && Mathf.Abs(p.x - Rachis(p.y)) < 1.5f - y * .008f) { c = Ramp3(dark, mid, light, .18f + y / 400f); a = 1; }
                for (int k = 0; k < 17; k++)
                {
                    float y0 = 9 + k * 6.6f; if (y0 > 118) break;
                    float t = y0 / 124f, len = 50 * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01((y0 - 3) / 128f)), .7f) * (1 - t * .55f) + 4;
                    foreach (int side in new[] { -1, 1 })
                    {
                        float ang = (58 - t * 18) * Mathf.Deg2Rad; var dir = new Vector2(side * Mathf.Sin(ang), Mathf.Cos(ang)); var across = new Vector2(dir.y, -dir.x);
                        var d = p - new Vector2(Rachis(y0), y0 + side * 1.6f); float u = Vector2.Dot(d, dir) / len, v = Vector2.Dot(d, across) / len;
                        if (u <= 0 || u >= 1) continue;
                        float half = .13f * Mathf.Pow(Mathf.Sin(u * Mathf.PI), .55f) * (1 - u * .3f) * (.85f + .15f * Mathf.Abs(Mathf.Cos(u * Mathf.PI * 4)));
                        float edge = half - Mathf.Abs(v); if (edge <= 0) continue;
                        float shade = .28f + u * .38f + t * .3f + (v * side > 0 ? .06f : -.03f) + (Perlin(x, y, .35f) - .5f) * .12f;
                        if (edge < 1.2f / len) shade -= .25f;                                     // the painted rim
                        if (Mathf.Abs(v) < .7f / len && u > .06f && u < .9f) shade -= .1f;     // the leaflet's midrib
                        c = Ramp3(dark, mid, light, shade); a = 1;
                    }
                }
                return new Color(c.r, c.g, c.b, a);
            }, true);
        }
        /// <summary>
        /// A painted broad leaf on its stalk (alpha cut, tip up): a stalk up from the root, and a big oval blade, a little heart-shaped
        /// at the base and pointed at the tip, with a lighter midrib and paired side veins curving to the edge, a dark painted rim,
        /// darker at the base and toward the shaded side, lighter toward the tip.
        /// </summary>
        static Texture2D BroadLeafTex(string name, Color dark, Color mid, Color light)
        {
            return Tex(name, 128, (x, y) => {
                float px = x + .5f - 64, py = y + .5f; Color c = mid; float a = 0;
                if (py < 34 && Mathf.Abs(px) < 1.8f) { c = Ramp3(dark, mid, light, .3f); a = 1; }   // the stalk
                float v = (py - 26) / 99f;
                if (v > 0 && v < 1)
                {
                    float half = 54 * Mathf.Pow(Mathf.Sin(Mathf.PI * v), .62f) * (1 - v * .3f) * (v < .12f ? .7f + v * 2.5f : 1);
                    float edge = half - Mathf.Abs(px);
                    if (edge > 0)
                    {
                        float across = Mathf.Abs(px) / Mathf.Max(1, half);
                        float shade = .3f + v * .35f + across * .18f + (px > 0 ? .08f : -.04f) + (Perlin(x, y, .18f) - .5f) * .16f;
                        float vein = Mathf.Abs(Mathf.Repeat(v * 9 - across * 1.6f, 1) - .5f);                         // side veins sweeping out and up
                        if (vein < .045f && across > .08f && across < .9f) shade += .1f;
                        if (Mathf.Abs(px) < 1.3f + (1 - v) * 1.2f) shade += .12f;                                    // the midrib, paler
                        if (edge < 2.2f) shade -= .26f;                                                                // the painted rim
                        c = Ramp3(dark, mid, light, shade); a = 1;
                    }
                }
                return new Color(c.r, c.g, c.b, a);
            }, true);
        }
        /// <summary>
        /// A painted clump of reeds (alpha cut, tip up): long narrow blades from the root leaning a little either way, darker at the
        /// root, lighter toward the tips, and three stems carrying brown seed heads, one bent over.
        /// </summary>
        static Texture2D ReedTex(string name, Color dark, Color mid, Color light, Color head)
        {
            return Tex(name, 128, (x, y) => {
                float px = x + .5f, py = y + .5f; Color c = mid; float a = 0;
                for (int b = 0; b < 9; b++)
                {
                    float root = 36 + b * 7f + Mathf.Sin(b * 2.3f) * 3, h = 96 + (b * 41 % 30), lean = (b % 3 - 1) * .13f + Mathf.Sin(b * 1.7f) * .05f;   // inside the card
                    if (py > h) continue;
                    float t = py / h, bx = root + lean * py + lean * py * t * .6f, width = 3.2f * (1 - t) + .5f;
                    if (Mathf.Abs(px - bx) < width) { c = Ramp3(dark, mid, light, .2f + t * .7f + (px - bx) / width * .08f); a = 1; }
                }
                foreach (var (sx, top, bend) in new[] { (47f, 121f, 0f), (71f, 112f, .12f), (86f, 104f, -.06f) })
                {
                    float bx = sx + bend * py * py / 120f;
                    if (py < top && Mathf.Abs(px - bx) < 1.1f) { c = Ramp3(dark, mid, light, .45f + py / 300f); a = 1; }
                    float hx = sx + bend * (top - 12) * (top - 12) / 120f;
                    if (py > top - 24 && py < top - 4 && Mathf.Abs(px - hx) < 3.4f - Mathf.Abs(py - (top - 14)) * .06f)
                    { float s = .7f + (px - hx) * .06f + (Perlin(x, y, .5f) - .5f) * .2f; c = head * s; a = 1; }   // the seed head
                }
                return new Color(c.r, c.g, c.b, a);
            }, true);
        }
        static Material Standard(string name, Color color, Texture2D tex, float smoothness, float metallic = 0)
        {
            var m = new Material(Shader.Find("Standard")) { color = color, name = name };
            if (tex != null) m.mainTexture = tex;
            m.SetFloat("_Glossiness", smoothness); m.SetFloat("_Metallic", metallic);
            AssetDatabase.CreateAsset(m, ArtRoot + "/" + name + ".mat"); return m;
        }
        static Material Cutout(string name, Color color, Texture2D tex)
        {
            var m = Standard(name, color, tex, .05f);
            m.SetFloat("_Mode", 1); m.SetOverrideTag("RenderType", "TransparentCutout"); m.SetFloat("_Cutoff", .45f);
            m.SetInt("_SrcBlend", (int)BlendMode.One); m.SetInt("_DstBlend", (int)BlendMode.Zero); m.SetInt("_ZWrite", 1);
            m.EnableKeyword("_ALPHATEST_ON"); m.DisableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.AlphaTest; m.enableInstancing = true; EditorUtility.SetDirty(m);
            return m;
        }
        static void Fade(Material m)
        {
            m.SetFloat("_Mode", 2); m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON"); m.EnableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent; EditorUtility.SetDirty(m);
        }
        static float Perlin(float x, float y, float f) { return Mathf.PerlinNoise(x * f + 13.7f, y * f + 71.3f); }
        static Color Grey(float v) { v = Mathf.Clamp01(v); return new Color(v, v, v, 1); }
        /// <summary>Writes a generated texture as a PNG asset once, then reuses it.</summary>
        static Texture2D Tex(string name, int size, Func<int, int, Color> pixel, bool alpha = false)
        {
            string path = ArtRoot + "/" + name + ".png";
            if (!File.Exists(path))
            {
                var t = new Texture2D(size, size, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) t.SetPixel(x, y, pixel(x, y));
                File.WriteAllBytes(path, t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.wrapMode = alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat; importer.alphaIsTransparency = alpha;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
