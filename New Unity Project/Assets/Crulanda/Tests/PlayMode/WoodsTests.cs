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
    /// <summary>The wild woods (ZoneBuilder.Woods.cs; Chris, 2026-10-14: "Filling the zones in the Elwynn style", "id like to keep some of
    /// the trees we have now. only the big tree in some areas maybe making it a dense forest area"): Oakhaven's wild country is wooded,
    /// its great oaks stand in a few patches only, and no tree stands on a road, in a camp, on a gathering node or at a secret.</summary>
    public class WoodsTests
    {
        string root;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-woods-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        static float ToPath(Vector2 p, Vector2[] pts)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                var a = pts[i]; var b = pts[i + 1]; var ab = b - a; float t = ab.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0;
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        [UnityTest] public IEnumerator Oakhaven_is_wooded_and_its_ways_stay_open()
        {
            var zone = ZoneBuilder.Active; Assert.NotNull(zone); Assert.AreEqual("zone.oakhaven", zone.Zone.id);
            var trees = zone.WoodsTrees; var z = zone.Zone;
            Debug.Log("Woods test: " + trees.Count + " trees");
            Assert.GreaterOrEqual(trees.Count, 400, "the wild is wooded");
            var problems = new List<string>();
            foreach (var t in trees)
            {
                foreach (var r in z.roads) if (r != null && r.points.Length > 1 && ToPath(t, r.points) < r.width / 2 + 3.4f) problems.Add("a tree on the road '" + r.name + "' at " + t);
                foreach (var c in z.camps) if (c != null && Vector2.Distance(t, c.center) < c.radius * 1.42f + 1.5f) problems.Add("a tree in the camp '" + c.name + "' at " + t);
                foreach (var n in z.nodes) if (n != null && Vector2.Distance(t, n.at) < 4.5f) problems.Add("a tree on the node " + n.node + " at " + n.at);
                foreach (var s in z.secrets) if (s != null && Vector2.Distance(t, s.at) < s.radius + 3.5f) problems.Add("a tree on the secret " + s.id);
                if (t.magnitude < z.woods.from - .1f) problems.Add("a tree inside the village's reach at " + t);
            }
            Assert.IsEmpty(problems.Take(20).ToList(), problems.Count + " problems");
            // The great oaks: some, and only in the old forest's few patches (not one in every corner).
            var oaks = zone.GetComponentsInChildren<Transform>(true).Where(x => x.name == "Old oak" || x.name == "giant_tree").Select(x => new Vector2(x.position.x, x.position.z)).ToList();
            Debug.Log("Woods test: " + oaks.Count + " great oaks");
            Assert.Greater(oaks.Count, 8, "great oaks in the old forest");
            Assert.Less(oaks.Count, trees.Count / 3, "the great oaks only in some areas");
            yield return null;
        }
    }
}
#endif
