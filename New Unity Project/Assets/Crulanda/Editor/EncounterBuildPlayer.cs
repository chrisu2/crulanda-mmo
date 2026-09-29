using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Crulanda.EditorTools
{
    public static class EncounterBuildPlayer
    {
        const string ExePath = "Builds/Crulanda/Crulanda.exe";

        /// <summary>Batch-mode entry point (-executeMethod) and the core of the menu items.</summary>
        public static void Build()
        {
            Directory.CreateDirectory("Builds/Crulanda");
            PlayerSettings.companyName = "Crulanda";
            PlayerSettings.productName = "Crulanda - The Quiet Trail";   // also names the save folder; do not change casually
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = UnityEngine.FullScreenMode.Windowed;
            // Pick up any new zone or quest JSON (registers them on the scene and the encounter content; preserves the rest).
            if (File.Exists(ZoneSceneBuilder.OakhavenScene)) ZoneSceneBuilder.BuildOakhaven();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                // Oakhaven is the game's opening zone; the Quiet Trail stays in the build as the test map.
                scenes = File.Exists(ZoneSceneBuilder.OakhavenScene)
                    ? new[] { ZoneSceneBuilder.OakhavenScene, EncounterBuilder.ScenePath }
                    : new[] { EncounterBuilder.ScenePath },
                locationPathName = ExePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Player build failed: " + report.summary.result);
        }

        [MenuItem("Crulanda/Test Build/Build Test Player", priority = 1)]
        static void BuildMenu()
        {
            if (!EditorSceneManagerSaveGuard()) return;
            try { Build(); EditorUtility.RevealInFinder(ExePath); }
            catch (Exception e) { EditorUtility.DisplayDialog("Crulanda test build", e.Message, "OK"); }
        }

        [MenuItem("Crulanda/Test Build/Build and Run Test Player", priority = 2)]
        static void BuildAndRunMenu()
        {
            if (!EditorSceneManagerSaveGuard()) return;
            try { Build(); Launch(); }
            catch (Exception e) { EditorUtility.DisplayDialog("Crulanda test build", e.Message, "OK"); }
        }

        [MenuItem("Crulanda/Test Build/Run Last Test Player", priority = 3)]
        static void Launch()
        {
            if (!File.Exists(ExePath)) { EditorUtility.DisplayDialog("Crulanda test build", "No test build yet. Use Build Test Player first.", "OK"); return; }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.GetFullPath(ExePath)) { UseShellExecute = true });
        }

        static bool EditorSceneManagerSaveGuard()
        { return UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo(); }
    }
}
