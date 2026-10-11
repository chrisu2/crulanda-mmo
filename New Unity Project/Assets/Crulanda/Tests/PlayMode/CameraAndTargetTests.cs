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

        /// <summary>Inside the Golden Cask (set at an angle) the room stays a room: with the camera inside behind you no wall fades; with
        /// the camera outside behind the back wall that wall fades and the rest stays; and the camera itself, turned all the way round,
        /// stays within the walls (playtest note 120, Chris, 2026-10-11: "GOING IN A BUILDING MAKES IT DISAPPEAR"; the tester's note 117,
        /// "The camera goes inside walls when you walk into a building"). Before, every part of the inn faded at once.</summary>
        [UnityTest] public IEnumerator Inside_the_inn_only_what_blocks_the_view_fades_and_the_camera_stays_in()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var motor = s.Player.GetComponent<AdventurerMotor>();
            var innProp = ZoneBuilder.Active.Zone.props.First(p => p != null && p.kind == "inn");
            var inn = RoofFade.All.OrderBy(r => Vector2.Distance(new Vector2(r.transform.position.x, r.transform.position.z), innProp.at)).First();
            Assert.Greater(inn.BigParts, 4, "the inn's walls and roof are its big parts");
            Assert.Greater(inn.Half.x, 1, "the inn knows its walls");
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(inn.transform.position + Vector3.up, out var floor, 4, UnityEngine.AI.NavMesh.AllAreas), "the taproom's floor");
            motor.enabled = false; motor.Teleport(floor.position + Vector3.up);   // the capsule's middle, a metre over the floor
            yield return null;
            Vector3 pos = s.Player.transform.position, head = pos + Vector3.up * 1.5f, body = pos + Vector3.up * .7f;
            var local = inn.transform.InverseTransformPoint(pos);
            IEnumerator Settle(Vector3 camera) { for (float t = 0; t < .6f; t += Time.deltaTime) { RoofFade.UpdateAll(camera, head, body); yield return null; } }
            // The camera inside, two and a half metres behind along the room's length.
            yield return Settle(inn.transform.TransformPoint(local + new Vector3(Mathf.Min(2.5f, inn.Half.x - .6f) * -Mathf.Sign(local.x == 0 ? 1 : local.x), .6f, 0)));
            Assert.LessOrEqual(inn.FadedBigParts, 1, "with the camera inside the room, the walls stay (" + inn.FadedBigParts + " of " + inn.BigParts + " big parts faded)");
            // The camera outside, three metres beyond a wall, all the way round: whichever wall of the inn the line of sight hits fades
            // (measured by a ray, not by the fade's own boxes), and the far walls stay.
            int walls = 0; string seen = "";
            for (int k = 0; k < 8; k++)
            {
                var dir = Quaternion.Euler(0, k * 45, 0) * Vector3.forward; float reach = Mathf.Abs(dir.x) > .01f && Mathf.Abs(dir.z) > .01f ? Mathf.Min(inn.Half.x / Mathf.Abs(dir.x), inn.Half.y / Mathf.Abs(dir.z)) : Mathf.Abs(dir.x) > .01f ? inn.Half.x : inn.Half.y;
                var camera = inn.transform.TransformPoint(new Vector3(local.x, local.y + 1.2f, local.z) + dir * (reach + 3));
                if (!Physics.Raycast(camera, (head - camera).normalized, out var hit, Vector3.Distance(camera, head), ~0, QueryTriggerInteraction.Ignore) || hit.collider.GetComponentInParent<RoofFade>() != inn) { seen += k * 45 + ": clear; "; continue; }
                var wall = hit.collider.GetComponentInParent<Renderer>() ?? hit.collider.GetComponentInChildren<Renderer>();
                yield return Settle(camera); walls++;
                seen += k * 45 + ": " + hit.collider.name + " faded " + inn.FadedBigParts + "; ";
                Assert.GreaterOrEqual(inn.FadedBigParts, 1, "the wall the view hits fades (" + k * 45 + " degrees, " + hit.collider.name + "): " + seen);
                if (wall != null && wall.GetComponentInParent<RoofFade>() == inn) Assert.AreEqual("Crulanda/Fade", wall.sharedMaterial.shader.name, "the very wall the view hits fades (" + k * 45 + " degrees, " + hit.collider.name + "): " + seen);
                Assert.Less(inn.FadedBigParts, inn.BigParts / 2f, "the far walls stay (" + inn.FadedBigParts + " of " + inn.BigParts + " faded): " + seen);
            }
            Assert.Greater(walls, 0, "some view from outside is blocked by a wall of the inn: " + seen);
            // The camera itself, turned all the way round: within the walls every time, and the room never gone.
            motor.enabled = true;
            foreach (float yaw in new[] { 0f, 90, 180, 270, 45, 225 })
            {
                motor.SetView(yaw + inn.transform.eulerAngles.y, 25, 8);
                for (int f = 0; f < 30; f++) yield return null;
                var cam = inn.transform.InverseTransformPoint(Camera.main.transform.position);
                Assert.LessOrEqual(Mathf.Abs(cam.x), inn.Half.x, "the camera stays within the walls (yaw " + yaw + ", " + cam + ")");
                Assert.LessOrEqual(Mathf.Abs(cam.z), inn.Half.y, "the camera stays within the walls (yaw " + yaw + ", " + cam + ")");
                Assert.Less(inn.FadedBigParts, inn.BigParts / 2f, "the room stays a room (yaw " + yaw + ": " + inn.FadedBigParts + " of " + inn.BigParts + " faded)");
            }
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
