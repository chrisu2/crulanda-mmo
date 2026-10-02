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
    /// Every zone built with and without its nodes (the nodes array and the herb props' node fields): every tree trunk, every
    /// piece of scenery and every prop, every point of the creeks, every secret and every usable prop stands where it stood, so
    /// the seams, windfalls and herbs were added without moving the world (each zone's layout is one random stream).
    /// </summary>
    public class NodeStreamTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(string zoneId, bool withoutNodes)
        {
            WorldClock.Hour = 11;
            if (root == null) { root = Path.Combine(Path.GetTempPath(), "Crulanda-nodestream-" + Guid.NewGuid().ToString("N")); SceneManager.sceneLoaded += OnLoaded; }
            ZoneBuilder.DefinitionFilter = withoutNodes ? (Func<ZoneDefinition, ZoneDefinition>)(d => { d.nodes = new ZoneNode[0]; foreach (var p in d.props) if (p != null) p.node = null; return d; }) : null;
            ZoneBuilder.RequestedZoneId = zoneId;
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

        static readonly string[] ZoneIds = { "zone.oakhaven", "zone.khaven", "zone.peaks", "zone.ashrim", "zone.verdant" };

        [UnityTest, Timeout(900000)] public IEnumerator TreesAndScenery_Unmoved()
        {
            var problems = new List<string>();
            foreach (var id in ZoneIds)
            {
                yield return Load(id, true);
                var without = ZoneBuilder.Active; Assert.IsNotNull(without); string zp = id + ": ";
                if (without.Zone.id != id) { problems.Add(zp + "built " + without.Zone.id + " instead"); continue; }
                Assert.IsEmpty(without.Zone.nodes, zp + "built without the nodes."); Assert.IsNull(without.transform.Find("Zone nodes"), zp + "no nodes built");
                Assert.IsFalse(without.Interactables.Any(i => i.node != null), zp + "no herb prop is worked as a node");
                var before = Read(without);
                yield return Load(id, false);
                var with = ZoneBuilder.Active;
                int array = with.Zone.nodes.Length, props = with.Zone.props.Count(p => p != null && !string.IsNullOrEmpty(p.node));
                if (array == 0 || with.transform.Find("Zone nodes") == null) { problems.Add(zp + "has no nodes built"); continue; }
                if (with.transform.Find("Zone nodes").childCount != array) problems.Add(zp + with.transform.Find("Zone nodes").childCount + " of the " + array + " nodes in its array are built");
                if (with.Interactables.Count(i => i.node != null) != 28 || array + props != 28) problems.Add(zp + array + " nodes and " + props + " herb props worked as nodes, " + with.Interactables.Count(i => i.node != null) + " built: not twenty-eight");
                var after = Read(with);
                if (before.trunks.Count != after.trunks.Count) problems.Add(zp + "trees were dropped or added: " + before.trunks.Count + " then " + after.trunks.Count);
                else for (int i = 0; i < before.trunks.Count; i++) if (Vector2.Distance(before.trunks[i], after.trunks[i]) >= .001f) { problems.Add(zp + "trunk " + i + " moved from " + before.trunks[i] + " to " + after.trunks[i]); break; }
                var gone = before.scenery.Except(after.scenery).Take(10).ToList(); var come = after.scenery.Except(before.scenery).Take(10).ToList();
                if (gone.Count > 0) problems.Add(zp + "scenery moved or missing:\n" + string.Join("\n", gone) + "\nnow:\n" + string.Join("\n", come));
                if (before.scenery.Count != after.scenery.Count) problems.Add(zp + "scenery added:\n" + string.Join("\n", come));
                if (!before.secrets.SequenceEqual(after.secrets)) problems.Add(zp + "a secret moved");
                if (!before.usable.SequenceEqual(after.usable)) problems.Add(zp + "a usable prop (a herb worked as a node included) moved");
                if (before.creeks.Count != after.creeks.Count) problems.Add(zp + "creeks: " + before.creeks.Count + " then " + after.creeks.Count);
                else for (int c = 0; c < before.creeks.Count; c++)
                {
                    if (before.creeks[c].Length != after.creeks[c].Length) { problems.Add(zp + "creek " + c + " lost or gained points"); continue; }
                    for (int i = 0; i < before.creeks[c].Length; i++) if (Vector2.Distance(before.creeks[c][i], after.creeks[c][i]) >= .001f) { problems.Add(zp + "creek " + c + " point " + i + " moved from " + before.creeks[c][i]); break; }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
#endif
