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
    /// Oakhaven built with and without its nodes (the nodes array and the herb props' node fields): every tree trunk, every piece
    /// of scenery and every prop, every point of the creek, every secret and every usable prop stands where it stood, so the
    /// seams, windfalls and herbs were added without moving the world (the zone's layout is one random stream).
    /// </summary>
    public class NodeStreamTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(bool withoutNodes)
        {
            WorldClock.Hour = 11;
            if (root == null) { root = Path.Combine(Path.GetTempPath(), "Crulanda-nodestream-" + Guid.NewGuid().ToString("N")); SceneManager.sceneLoaded += OnLoaded; }
            ZoneBuilder.DefinitionFilter = withoutNodes ? (Func<ZoneDefinition, ZoneDefinition>)(d => { d.nodes = new ZoneNode[0]; foreach (var p in d.props) if (p != null) p.node = null; return d; }) : null;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
            ZoneBuilder.DefinitionFilter = null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            ZoneBuilder.DefinitionFilter = null; ZoneBuilder.RequestedZoneId = null;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
            root = null;
        }
        static string Where(Vector3 v) { return v.x.ToString("0.000") + " " + v.y.ToString("0.000") + " " + v.z.ToString("0.000"); }
        sealed class Layout { public List<Vector2> trunks; public List<string> scenery, secrets, usable; public List<Vector2[]> creeks; }
        static Layout Read(ZoneBuilder zone)
        {
            var scenery = new List<string>();
            foreach (var container in new[] { "Zone static scenery", "Zone props" })
            {
                var c = zone.transform.Find(container); Assert.IsNotNull(c, container);
                foreach (Transform t in c) scenery.Add(t.name + " " + Where(t.position) + " " + t.eulerAngles.y.ToString("0.0") + " " + t.GetComponentsInChildren<Renderer>(true).Length);
            }
            scenery.Sort(StringComparer.Ordinal);
            var secrets = zone.Secrets.Select(s => s.def.id + " " + Where(s.position)).OrderBy(x => x, StringComparer.Ordinal).ToList();
            var usable = zone.Interactables.Where(i => i.kind != "node").Select(i => i.name + " " + Where(i.position)).OrderBy(x => x, StringComparer.Ordinal).ToList();
            return new Layout { trunks = zone.Trunks.ToList(), scenery = scenery, secrets = secrets, usable = usable, creeks = zone.Water.Creeks.Select(c => (Vector2[])c.pts.Clone()).ToList() };
        }

        [UnityTest] public IEnumerator TreesAndScenery_Unmoved()
        {
            yield return Load(true);
            var without = ZoneBuilder.Active; Assert.IsNotNull(without);
            Assert.IsEmpty(without.Zone.nodes, "Built without the nodes."); Assert.IsNull(without.transform.Find("Zone nodes"));
            Assert.IsFalse(without.Interactables.Any(i => i.node != null));
            var before = Read(without);
            yield return Load(false);
            var with = ZoneBuilder.Active;
            Assert.AreEqual(20, with.Zone.nodes.Length, "Oakhaven's nodes array."); Assert.IsNotNull(with.transform.Find("Zone nodes"));
            Assert.AreEqual(20, with.transform.Find("Zone nodes").childCount, "Every node in the array is built.");
            Assert.AreEqual(28, with.Interactables.Count(i => i.node != null), "Twenty nodes and the eight Yarrow props.");
            var after = Read(with);
            Assert.AreEqual(before.trunks.Count, after.trunks.Count, "No tree was dropped or added.");
            for (int i = 0; i < before.trunks.Count; i++) Assert.Less(Vector2.Distance(before.trunks[i], after.trunks[i]), .001f, "Trunk " + i + " stays at " + before.trunks[i]);
            var gone = before.scenery.Except(after.scenery).Take(10).ToList(); var come = after.scenery.Except(before.scenery).Take(10).ToList();
            Assert.IsEmpty(gone, "Scenery moved or missing:\n" + string.Join("\n", gone) + "\nnow:\n" + string.Join("\n", come));
            Assert.AreEqual(before.scenery.Count, after.scenery.Count, "No scenery added:\n" + string.Join("\n", come));
            CollectionAssert.AreEqual(before.secrets, after.secrets, "Every secret stands where it stood.");
            CollectionAssert.AreEqual(before.usable, after.usable, "Every usable prop (the Yarrows included) stands where it stood.");
            Assert.AreEqual(before.creeks.Count, after.creeks.Count);
            for (int c = 0; c < before.creeks.Count; c++)
            {
                Assert.AreEqual(before.creeks[c].Length, after.creeks[c].Length, "Creek " + c + " keeps its points.");
                for (int i = 0; i < before.creeks[c].Length; i++) Assert.Less(Vector2.Distance(before.creeks[c][i], after.creeks[c][i]), .001f, "Creek " + c + " point " + i + " stays at " + before.creeks[c][i]);
            }
        }
    }
}
#endif
