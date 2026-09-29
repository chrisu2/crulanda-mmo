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
    public static class ZoneSceneBuilder
    {
        const string ArtRoot = "Assets/Crulanda/World/Art";
        public const string OakhavenScene = "Assets/Crulanda/Scenes/Oakhaven.unity";
        const string OakhavenJson = "Assets/Crulanda/EncounterContent/Zones/oakhaven.json";

        [MenuItem("Crulanda/World/Build Oakhaven")]
        public static void BuildOakhaven()
        {
            var art = EnsureArt();
            if (!File.Exists(OakhavenScene)) CreateScene(art);
            else Debug.Log("Oakhaven scene already exists; preserving it (only the zone registry is refreshed).");
            RegisterZones();
            RegisterQuests();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(OakhavenScene, true), new EditorBuildSettingsScene(EncounterBuilder.ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("OAKHAVEN_BUILT");
        }
        /// <summary>Points the encounter content at every quest JSON (EncounterContent/Quests), so new quest files are never left out.</summary>
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
            EditorUtility.SetDirty(content);
            Debug.Log("Registered " + files.Count + " quest files and " + items.Count + " item files.");
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
