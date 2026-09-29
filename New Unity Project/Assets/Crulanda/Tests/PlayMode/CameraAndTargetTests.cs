#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The camera looks through trees (they fade while they block the view of the player, and come back after), and the
    /// target ring always marks what is selected: red for an enemy, green for a friend, nothing when nothing is.
    /// </summary>
    public class CameraAndTargetTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-camera-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [UnityTest] public IEnumerator A_tree_between_the_camera_and_the_player_fades_and_comes_back()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            s.Player.GetComponent<AdventurerMotor>().enabled = false;   // only this test drives the fade
            Assert.IsNotEmpty(TreeFade.All, "Every tree registers to fade.");
            Assert.IsNotNull(TreeFade.FadeTemplate, "The zone hands the fade material over.");
            var tree = TreeFade.All[0]; var foot = tree.transform.position;
            Vector3 camera = foot + new Vector3(-7, 2, 0), body = foot + new Vector3(7, 1, 0);
            for (float t = 0; t < .6f; t += Time.deltaTime) { TreeFade.UpdateAll(camera, body + Vector3.up * .7f, body); yield return null; }   // a fade takes about 0.25 s
            Assert.IsTrue(tree.Faded, "A tree in the line of sight turns see-through.");
            Assert.AreEqual("Crulanda/Fade", tree.GetComponentInChildren<Renderer>().sharedMaterial.shader.name);
            // Swing the camera round to the player's side: the view is clear, and the tree is solid again.
            camera = body + new Vector3(4, 3, 0);
            for (float t = 0; t < .6f; t += Time.deltaTime) { TreeFade.UpdateAll(camera, body + Vector3.up * .7f, body); yield return null; }   // a fade takes about 0.25 s
            Assert.IsFalse(tree.Faded, "Once the view is clear the tree comes back.");
            Assert.AreNotEqual("Crulanda/Fade", tree.GetComponentInChildren<Renderer>().sharedMaterial.shader.name);
        }

        [UnityTest] public IEnumerator The_target_ring_marks_what_is_selected()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var ring = s.GetComponent<TargetRing>(); Assert.IsNotNull(ring, "The session puts the ring out.");
            var enemy = s.Enemies.First(e => e != null && e.actor.IsAlive && !e.Hidden);
            s.Select(enemy); yield return null; yield return null;
            Assert.AreSame(enemy, ring.Showing); Assert.AreEqual(TargetRing.Hostile, ring.Colour);
            s.SelectFriendly(null, true); yield return null;
            Assert.AreSame(s.Companion, ring.Showing, "Selecting Mira moves the ring to her."); Assert.AreEqual(TargetRing.Friend, ring.Colour);
            s.SelectFriendly(null, false); s.Select(null); yield return null;
            Assert.IsNull(ring.Showing, "Nothing selected: no ring.");
        }
    }
}
#endif
