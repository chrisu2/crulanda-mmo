using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Crulanda.Data;
using Crulanda.Gameplay;

namespace Crulanda.EditorTools
{
    public static class Phase0Validation
    {
        // Run in a disposable validation project with -executeMethod.
        public static void ValidateGeneratedMap()
        {
            const string scene = "Assets/Crulanda/Scenes/Phase0_TestMap.unity";
            const string database = "Assets/Crulanda/ScriptableObjects/ContentDatabase.asset";
            Phase0TestMapBuilder.Build();
            if (!File.Exists(scene)) throw new Exception("Test map was not generated.");
            if (UnityEngine.Object.FindObjectsByType<Actor>(FindObjectsSortMode.None).Length != 4)
                throw new Exception("Expected four sample actors.");
            if (AssetDatabase.LoadAssetAtPath<ContentDatabase>(database) == null)
                throw new Exception("Content database missing.");
            var sentinel = new GameObject("Hand-authored preservation sentinel");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scene);
            string sceneBefore = File.ReadAllText(scene);
            string databaseBefore = File.ReadAllText(database);
            Phase0TestMapBuilder.Build();
            if (sceneBefore != File.ReadAllText(scene) || databaseBefore != File.ReadAllText(database))
                throw new Exception("Builder overwrote existing content.");
            UnityEngine.Object.DestroyImmediate(sentinel);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scene);
            ContentValidator.Validate();
            Debug.Log("PHASE0_MAP_VALIDATION_PASSED");
        }
    }
}
