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
    /// Playtest note 15 ("a few trees are disjointed in the limb area"): in every zone, every limb of a standing tree grows out of
    /// its tree's wood. The point a limb starts from lies inside the trunk or another limb of the same tree (within its radius
    /// there, and 5 cm), as ZoneBuilder records them when RecordWood is on, in the tree's own space. The Verdant giants' branches
    /// stood up to a metre over their great limbs, and the dead trees' twigs over theirs, when they started on the straight line
    /// between a bowed limb's ends (ZoneBuilder.LimbAt is the bowed line). Wood that belongs to no standing tree (roots in a
    /// cave's walls, a fallen giant's, a windfall's brush) has no trunk recorded and is left out.
    /// </summary>
    public class TreeLimbTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            ZoneBuilder.RecordWood = true;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-limbs-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            ZoneBuilder.RecordWood = false; ZoneBuilder.RequestedZoneId = null;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        /// <summary>How far p lies outside a piece of wood: its distance from the axis less the radius there (negative inside).</summary>
        static float Outside(Vector3 p, ZoneBuilder.WoodAxis w)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < w.pts.Length; i++)
            {
                Vector3 a = w.pts[i], ab = w.pts[i + 1] - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector3.Distance(p, a + ab * t) - Mathf.Lerp(w.r[i], w.r[i + 1], t));
            }
            return best;
        }

        [UnityTest, Timeout(600000)] public IEnumerator Every_limb_grows_out_of_its_tree()
        {
            var first = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var zones = first.Zone.AllZones().Select(z => z.id).ToList();
            Assert.AreEqual(5, zones.Count, "Oakhaven, Khaven, the Peaks, the Ashland Rim and the Verdant Shore are registered.");
            var problems = new List<string>(); int trees = 0, limbs = 0, giants = 0;
            foreach (var id in zones)
            {
                ZoneBuilder.RequestedZoneId = id;
                yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
                for (int f = 0; f < 4; f++) yield return null;
                var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
                if (s.Zone.Zone.id != id) { problems.Add(id + ": built " + s.Zone.Zone.id + " instead"); continue; }
                int bad = 0;
                foreach (var kv in s.Zone.Wood)
                {
                    if (kv.Key == null || !kv.Value.Any(w => w.trunk)) continue;
                    trees++; if (kv.Value.Any(w => w.trunk && w.r.Max() > 1.5f)) giants++;
                    foreach (var limb in kv.Value.Where(w => w.from.HasValue))
                    {
                        limbs++; var from = limb.from.Value;
                        float outside = kv.Value.Where(w => w != limb).Select(w => Outside(from, w)).DefaultIfEmpty(float.MaxValue).Min();
                        if (outside <= .05f) continue;
                        if (++bad <= 12) problems.Add(id + ": the tree at " + kv.Key.position.ToString("0.0") + " has a limb that starts " + outside.ToString("0.00") + " m outside its wood (at " + from.ToString("0.00") + ", in the tree's own space)");
                    }
                }
                if (bad > 12) problems.Add(id + ": and " + (bad - 12) + " more");
            }
            // The kit's trees (art rounds 2 and 4, 2026-10-07) are models with no recorded wood: what is recorded now is the painted
            // trees left (the great oak, the giants, the massive dead oaks, the treehouse trees), and with the kit gone, all of them.
            Assert.Greater(trees, 10, "Standing painted trees recorded in the five zones.");
            Assert.Greater(limbs, 30, "Their limbs.");
            Assert.Greater(giants, 0, "The Verdant Shore's giants among them.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
#endif
