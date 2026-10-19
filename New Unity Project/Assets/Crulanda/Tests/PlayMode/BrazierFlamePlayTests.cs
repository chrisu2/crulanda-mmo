#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Play note 255 (2026-10-19, Chris: "fire goes around the bazier", "it was dancing around the brazier"): the flame shader breathes the
    /// flame from its mesh's own vertex positions, and static batching had moved the flames' vertices into the zone's space, so a
    /// brazier's flame was drawn beside its bowl, lower, swinging round it. Flames are kept out of static batching (ZoneBuilder.Batchable),
    /// and the shader holds its breathing to the mesh's unit height. In the Ash Rim (its braziers), the Adit's camps and the Crown:
    /// no flame is part of a static batch or wears a combined mesh; each brazier's flame stands over its bowl, and its drawn base
    /// (CandleFlame.Sway, the shader's breathing on the CPU) stays within 3 cm of the bowl's middle across half a minute.
    /// </summary>
    public class BrazierFlamePlayTests
    {
        string root; bool loaded;   // the plain [Test] loads no zone: the teardown drops only what Load loaded
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(string zone)
        {
            WorldClock.Hour = 21; ZoneBuilder.RequestedZoneId = zone;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-flames-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded; loaded = true;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            VillageEconomy.ResetAll();
            Time.timeScale = 1; WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            if (!loaded) yield break;
            loaded = false;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }

        [Test] public void A_flame_is_never_batched_and_its_base_never_moves()
        {
            var plain = new GameObject("plain"); var flame = new GameObject("flame"); flame.AddComponent<CandleFlame>();
            var deck = new GameObject("deck"); deck.AddComponent<NavWalkable>();
            try
            {
                Assert.IsTrue(ZoneBuilder.Batchable(plain), "scenery is batched");
                Assert.IsFalse(ZoneBuilder.Batchable(flame), "a flame is not");
                Assert.IsFalse(ZoneBuilder.Batchable(deck), "nor a walked deck");
            }
            finally { UnityEngine.Object.DestroyImmediate(plain); UnityEngine.Object.DestroyImmediate(flame); UnityEngine.Object.DestroyImmediate(deck); }
            for (float t = 0; t < 60; t += .05f)
            {
                foreach (var b in new[] { Vector3.zero, new Vector3(1, 0, 0), new Vector3(0, 0, -1), new Vector3(.7f, 0, .7f) })
                    Assert.Less((CandleFlame.Sway(b, t) - b).magnitude, 1e-5f, "the base never moves (t " + t + ")");
                var tip = CandleFlame.Sway(Vector3.up, t);
                Assert.LessOrEqual(Mathf.Abs(tip.x), .1201f); Assert.LessOrEqual(Mathf.Abs(tip.z), .0801f);
                // Drawn in metres far off its origin (as a batched mesh was), the breathing is held to the unit height.
                var far = new Vector3(3, 12, -4); Assert.LessOrEqual((CandleFlame.Sway(far, t) - far).magnitude, .2f, "held to a unit's height");
            }
        }

        [UnityTest] public IEnumerator Every_brazier_flame_stands_in_its_bowl([Values("zone.ashrim", "zone.adit", "zone.crown")] string zoneId)
        {
            yield return Load(zoneId);
            Assert.AreEqual(zoneId, ZoneBuilder.Active.Zone.id);
            var flames = UnityEngine.Object.FindObjectsByType<CandleFlame>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(flames.Length, 0, "flames in " + zoneId);
            var wrong = new List<string>(); int braziers = 0;
            foreach (var f in flames)
            {
                var r = f.GetComponent<MeshRenderer>(); var mf = f.GetComponent<MeshFilter>();
                if (r == null || mf == null) continue;
                if (r.isPartOfStaticBatch) wrong.Add(f.name + " at " + f.transform.position + " is in a static batch");
                if (mf.sharedMesh == null || mf.sharedMesh.name.StartsWith("Combined Mesh")) wrong.Add(f.name + " at " + f.transform.position + " wears a combined mesh");
                // A brazier's flame (ZoneBuilder.Brazier): the cone scaled .2 x .55 over a bowl .8 x .3, .12 below it.
                if ((f.transform.localScale - new Vector3(.2f, .55f, .2f)).sqrMagnitude > 1e-4f || f.transform.parent == null) continue;
                Transform bowl = null; float best = float.MaxValue;
                foreach (Transform c in f.transform.parent)
                {
                    if ((c.localScale - new Vector3(.8f, .3f, .8f)).sqrMagnitude > 1e-4f) continue;
                    float d = (c.position - f.transform.position).sqrMagnitude; if (d < best) { best = d; bowl = c; }
                }
                if (bowl == null) continue;
                braziers++;
                var centre = bowl.position; var m = f.transform.localToWorldMatrix;
                for (float t = 0; t < 30; t += .1f)
                {
                    var b = m.MultiplyPoint3x4(CandleFlame.Sway(Vector3.zero, t)); var flat = new Vector2(b.x - centre.x, b.z - centre.z);
                    if (flat.magnitude > .03f) { wrong.Add("brazier at " + centre + ": the flame's base " + flat.magnitude.ToString("0.000") + " m off the bowl's middle at t " + t.ToString("0.0")); break; }
                    if (b.y < centre.y - .05f || b.y > centre.y + .3f) { wrong.Add("brazier at " + centre + ": the flame's base " + (b.y - centre.y).ToString("0.00") + " m over the bowl"); break; }
                }
                var bounds = r.bounds; var off = new Vector2(bounds.center.x - centre.x, bounds.center.z - centre.z);
                if (off.magnitude > .03f) wrong.Add("brazier at " + centre + ": the flame's bounds " + off.magnitude.ToString("0.000") + " m off the bowl's middle");
            }
            if (zoneId != "zone.crown") Assert.Greater(braziers, 0, "braziers found in " + zoneId);
            Assert.IsEmpty(wrong, string.Join("\n", wrong));
        }
    }
}
#endif
