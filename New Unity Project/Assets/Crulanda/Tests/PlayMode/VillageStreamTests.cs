#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Oakhaven built with and without the trades' new buildings (two houses and four workshops, appended to its props): every
    /// tree trunk, every piece of scenery and every point of the creek stands where it stood, so the buildings were added without
    /// moving the world (the zone's layout is one random stream). And the new floors are dry and bare.
    /// </summary>
    public class VillageStreamTests
    {
        /// <summary>The props the trades added to Oakhaven (as WorkshopDataTests lists them).</summary>
        public static readonly string[] NewProps = { "Carder farmhouse", "Crisp cottage", "Tanner's leather shop", "Lisbet's drying hut", "The Cask's kitchen", "Moss's game rack" };
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(bool withoutNewProps)
        {
            WorldClock.Hour = 11;
            if (root == null) { root = Path.Combine(Path.GetTempPath(), "Crulanda-stream-" + Guid.NewGuid().ToString("N")); SceneManager.sceneLoaded += OnLoaded; }
            ZoneBuilder.DefinitionFilter = withoutNewProps ? (Func<ZoneDefinition, ZoneDefinition>)(d => { d.props = d.props.Where(p => p == null || Array.IndexOf(NewProps, p.name) < 0).ToArray(); return d; }) : null;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
            ZoneBuilder.DefinitionFilter = null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            VillageEconomy.ResetAll();   // the purses are kept for the play session
            ZoneBuilder.DefinitionFilter = null; ZoneBuilder.RequestedZoneId = null;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
            root = null;
        }

        /// <summary>What a zone's random stream laid out: trunks, the scenery and props by name and place (the new buildings left
        /// out), and the creeks' points.</summary>
        sealed class Layout { public List<Vector2> trunks; public List<string> scenery; public List<Vector2[]> creeks; }
        static Layout Read(ZoneBuilder zone)
        {
            var scenery = new List<string>();
            foreach (var container in new[] { "Zone static scenery", "Zone props" })
            {
                var c = zone.transform.Find(container); Assert.IsNotNull(c, container);
                foreach (Transform t in c)
                    if (Array.IndexOf(NewProps, t.name) < 0)
                        scenery.Add(t.name + " " + t.position.x.ToString("0.000") + " " + t.position.y.ToString("0.000") + " " + t.position.z.ToString("0.000") + " " + t.eulerAngles.y.ToString("0.0"));
            }
            scenery.Sort(StringComparer.Ordinal);
            return new Layout { trunks = zone.Trunks.ToList(), scenery = scenery, creeks = zone.Water.Creeks.Select(c => (Vector2[])c.pts.Clone()).ToList() };
        }

        [UnityTest] public IEnumerator NewVillageProps_LeaveTreesSceneryAndCreekUnmoved()
        {
            yield return Load(true);
            var without = ZoneBuilder.Active; Assert.IsNotNull(without);
            Assert.IsFalse(without.Zone.props.Any(p => p != null && Array.IndexOf(NewProps, p.name) >= 0), "Built without the new buildings.");
            var before = Read(without);
            yield return Load(false);
            var with = ZoneBuilder.Active;
            foreach (var n in NewProps) Assert.IsNotNull(with.transform.Find("Zone static scenery/" + n) ?? with.transform.Find("Zone props/" + n), n + " is built.");
            var after = Read(with);
            Assert.AreEqual(before.trunks.Count, after.trunks.Count, "No tree was dropped or added.");
            for (int i = 0; i < before.trunks.Count; i++) Assert.Less(Vector2.Distance(before.trunks[i], after.trunks[i]), .001f, "Trunk " + i + " stays at " + before.trunks[i]);
            var gone = before.scenery.Except(after.scenery).Take(10).ToList(); var come = after.scenery.Except(before.scenery).Take(10).ToList();
            Assert.IsEmpty(gone, "Scenery moved or missing:\n" + string.Join("\n", gone) + "\nnow:\n" + string.Join("\n", come));
            Assert.AreEqual(before.scenery.Count, after.scenery.Count, "No scenery added:\n" + string.Join("\n", come));
            Assert.AreEqual(before.creeks.Count, after.creeks.Count);
            for (int c = 0; c < before.creeks.Count; c++)
            {
                Assert.AreEqual(before.creeks[c].Length, after.creeks[c].Length, "Creek " + c + " keeps its points.");
                for (int i = 0; i < before.creeks[c].Length; i++) Assert.Less(Vector2.Distance(before.creeks[c][i], after.creeks[c][i]), .001f, "Creek " + c + " point " + i + " stays at " + before.creeks[c][i]);
            }
        }

        [UnityTest] public IEnumerator New_floors_are_dry_and_bare()
        {
            yield return Load(false);
            var zone = ZoneBuilder.Active;
            // Openness is the grass's (0: no tuft grows); private, as the grass field is its only user.
            var openness = typeof(ZoneBuilder).GetMethod("Openness", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Vector2) }, null);
            Assert.IsNotNull(openness, "ZoneBuilder.Openness(Vector2)");
            foreach (var p in zone.Zone.props.Where(p => p != null && Array.IndexOf(NewProps, p.name) >= 0 && p.kind != "gamerack"))
            {
                Assert.IsTrue(zone.InBuilding(p.at), p.name + ": no rain falls on its floor.");
                Assert.AreEqual(0, (float)openness.Invoke(zone, new object[] { p.at }), 1e-4f, p.name + ": no grass grows through its floor.");
            }
        }
    }
}
#endif
