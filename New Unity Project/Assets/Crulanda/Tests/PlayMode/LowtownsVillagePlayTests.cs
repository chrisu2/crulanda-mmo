#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Lowtowns' houses from the Medieval Village Pack Vol.1 in the built zone (play note 246; ZoneBuilder.LowtownVillage): every planned
    /// house is placed, on the Built-in Standard shader (none pink), soot-tinted; each stands on its plot (its meshes within the old terrace's
    /// box, but for its eaves and porch over the street), its base on the brick or the plinth; the roof route's walks over Pell Row still
    /// join on the navmesh; and no other zone uses the pack (Oakhaven built after it has none).
    /// </summary>
    public class LowtownsVillagePlayTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(string zone)
        {
            WorldClock.Hour = 12; ZoneBuilder.RequestedZoneId = zone; ZoneBuilder.NoStaticBatching = true;   // the check reads the houses' own meshes
            root = Path.Combine(Path.GetTempPath(), "Crulanda-village-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 6; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            VillageEconomy.ResetAll();
            Time.timeScale = 1; WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null; ZoneBuilder.NoStaticBatching = false;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }

        [UnityTest] public IEnumerator The_pack_houses_stand_on_their_plots_and_the_roofs_still_join()
        {
            yield return Load("zone.lowtowns");
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.lowtowns", zone.Zone.id);
            int planned = zone.Zone.props.Where(ZoneBuilder.VillagePlot).Select(ZoneBuilder.PlanVillageRow).Sum(r => r.houses.Count);
            Assert.AreEqual(planned, zone.VillageHouses.Count, "Every planned house is placed.");
            int pink = 0, plain = 0;
            foreach (var (plot, place, model) in zone.VillageHouses)
            {
                Assert.IsNotNull(model, "Placed: " + place.model.name + " at " + plot.at);
                var t = model.parent; float w = plot.size.x, d = plot.size.y;
                var lo = Vector3.one * 1e9f; var hi = -lo;
                foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null || mf.name.Contains("Moss") || mf.transform.parent.name == "Roof moss") continue;
                    var mb = mf.sharedMesh.bounds; var m = t.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                    for (int i = 0; i < 8; i++) { var p = m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))); lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
                }
                string at = place.model.name + " at " + plot.at;
                Assert.GreaterOrEqual(lo.x, -w / 2 - .9f, at + ": within its plot across (eaves allowed)"); Assert.LessOrEqual(hi.x, w / 2 + .9f, at);
                Assert.GreaterOrEqual(lo.z, -d / 2 - 2.1f, at + ": its front on the street (eaves and porch over it)"); Assert.LessOrEqual(hi.z, d / 2 + .9f, at + ": no deeper than its plot");
                Assert.AreEqual(place.baseY, lo.y, .35f, at + ": on its brick or plinth");
                Assert.AreEqual(place.Top, hi.y, .6f, at + ": its height as planned (the model's measure is right)");
                foreach (var r in model.GetComponentsInChildren<MeshRenderer>(true))
                    foreach (var mat in r.sharedMaterials)
                    {
                        if (mat == null || mat.shader == null || mat.shader.name.Contains("Error")) { pink++; continue; }
                        if (r.transform.parent != null && r.transform.parent.name == "Roof moss") continue;
                        if (mat.shader.name == "Standard" && mat.color.r > .95f && mat.color.g > .95f) plain++;
                    }
            }
            Assert.AreEqual(0, pink, "No pink material (URP in the Built-in pipeline).");
            Assert.AreEqual(0, plain, "Every house is soot-tinted (the Lowtowns' colour).");
            Assert.IsTrue(zone.VillageHouses.Any(h => h.place.model.name == "Blacksmith"), "The smith's shop.");

            // The roof route over Pell Row: its terraces kept their brick (no pack house on them) and its walks still join on the navmesh.
            var roofs = zone.LowtownRoofs; Assert.IsNotNull(roofs, "The roofs are built.");
            foreach (var top in roofs.roofs) Assert.IsFalse(zone.VillageHouses.Any(h => h.plot == top.prop), "The route's terrace at " + top.prop.at + " keeps its brick.");
            var decks = roofs.legs.Where(l => l.kind == "deck").ToList();
            Assert.IsTrue(NavMesh.SamplePosition(decks[0].a, out var from, 1, NavMesh.AllAreas), "On the first walk.");
            Assert.IsTrue(NavMesh.SamplePosition(decks[2].b, out var to, 1, NavMesh.AllAreas), "On the third walk.");
            var path = new NavMeshPath(); NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path);
            Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, "Along the walks and over the alleys' rope bridges.");
            Assert.IsTrue(path.corners.All(c => c.y > from.position.y - 1), "The way stays up on the roofs (never down in the lane).");
        }

        [UnityTest] public IEnumerator No_other_zone_uses_the_pack()
        {
            yield return Load("zone.oakhaven");
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.oakhaven", zone.Zone.id);
            Assert.AreEqual(0, zone.VillageHouses.Count);
            Assert.IsFalse(zone.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Village House_") || t.name == "Village Blacksmith"), "No pack house outside the Lowtowns.");
        }
    }
}
#endif
