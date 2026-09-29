using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Crulanda.Core;
using Crulanda.Data;
using Crulanda.DevTools;
using Crulanda.Game;
using Crulanda.Gameplay;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// Generates the Phase 0 test map and its sample content from code, so the project state is
    /// reproducible and needs no manual scene wiring. Safe to re-run: existing assets are reused/updated
    /// and the scene is rebuilt. Menu: Crulanda > Phase 0 > Build Test Map.
    /// </summary>
    public static class Phase0TestMapBuilder
    {
        const string ActorsFolder = "Assets/Crulanda/ScriptableObjects/Actors";
        const string DatabasePath = "Assets/Crulanda/ScriptableObjects/ContentDatabase.asset";
        const string ScenePath = "Assets/Crulanda/Scenes/Phase0_TestMap.unity";

        [MenuItem("Crulanda/Phase 0/Build Test Map")]
        public static void Build()
        {
            // Never overwrite generated content: it may have been edited by hand.
            if (System.IO.File.Exists(ScenePath) || System.IO.File.Exists(DatabasePath) ||
                System.IO.Directory.Exists(ActorsFolder))
            {
                Debug.LogWarning("[Crulanda] Test content already exists. Build cancelled to preserve existing assets.");
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder("Assets/Crulanda/ScriptableObjects");
            EnsureFolder(ActorsFolder);
            EnsureFolder("Assets/Crulanda/Scenes");

            // Placeholder archetypes: neutral names on purpose. Real Crulanda content needs author-approved canon.
            var friendly = CreateArchetype("actor_test_friendly", "actor.test_friendly", "Test Friendly",
                1, Disposition.Friendly, ResourceKind.Mana,
                new[]
                {
                    new StatValue(StatType.Stamina, 10), new StatValue(StatType.Strength, 10),
                    new StatValue(StatType.MaxHealth, 120), new StatValue(StatType.MaxPower, 80)
                });

            var hostile = CreateArchetype("actor_test_hostile", "actor.test_hostile", "Test Hostile",
                1, Disposition.Hostile, ResourceKind.None,
                new[]
                {
                    new StatValue(StatType.Stamina, 12), new StatValue(StatType.Strength, 12),
                    new StatValue(StatType.MaxHealth, 200)
                });

            var database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ContentDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }
            database.EditorSetDefinitions(new DefinitionBase[] { friendly, hostile });
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrapGo = new GameObject("GameBootstrap");
            var bootstrap = bootstrapGo.AddComponent<GameBootstrap>();
            bootstrap.EditorSetContentDatabase(database);
            bootstrapGo.AddComponent<DevConsole>();

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(0f, 9f, -13f);
            cameraGo.transform.LookAt(Vector3.zero);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(3f, 1f, 3f);

            // Levels 1 / 5 / 12 give a spread to eyeball LevelCon against a level-5 viewer later.
            PlaceActor("Test Guard", friendly, 5, new Vector3(-4f, 1f, 0f));
            PlaceActor("Test Wolf A", hostile, 1, new Vector3(0f, 1f, 4f));
            PlaceActor("Test Wolf B", hostile, 5, new Vector3(4f, 1f, 4f));
            PlaceActor("Test Wolf C", hostile, 12, new Vector3(8f, 1f, 4f));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Crulanda] Phase 0 test map built: " + ScenePath +
                      ". Press Play, then ` (backquote) for the console and try: actor.list, actor.damage \"Test Wolf A\" 50");
        }

        static void PlaceActor(string displayName, ActorArchetypeDefinition archetype, int level, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = displayName;
            go.transform.position = position;
            var actor = go.AddComponent<Actor>(); // RequireComponent adds Health and ResourcePool
            actor.EditorSetup(archetype, displayName, level);
        }

        static ActorArchetypeDefinition CreateArchetype(string assetName, string id, string displayName, int level,
            Disposition disposition, ResourceKind resource, StatValue[] stats)
        {
            string path = ActorsFolder + "/" + assetName + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<ActorArchetypeDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ActorArchetypeDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorSetIdentity(new ContentId(id), displayName, CanonStatus.GameOnly);
            asset.EditorConfigure(level, disposition, ActorClassification.Normal, resource, stats);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            string leaf = path.Substring(path.LastIndexOf('/') + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
