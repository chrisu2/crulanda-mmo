using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Lowtowns' houses from the Medieval Village Pack Vol.1 (play note 246; ZoneBuilder.LowtownVillage; Docs/ZONE_LOWTOWNS.md "The
    /// houses"), read from the zone's data and the pack's files without building the zone:
    /// - every terrace plot gets the pack's houses (their prefabs there), doors to the street, scaled to a door the street's height, the
    ///   four-storey terraces on two storeys of brick; the smith's plot gets the Blacksmith;
    /// - the roof route's four terraces and the edge's blind backs keep their brick (the lead walks stand on their gutters) and the route
    ///   still joins up;
    /// - each house stands within its plot (the old terrace's box, so the navmesh is unchanged), the houses of a row side by side, none over
    ///   a road (but a lane that ends at that terrace), a stair or ramp, or a camp's centre;
    /// - the pack is used by the Lowtowns only; its windmill and water wheel are not in the project; its materials are Built-in shaders.
    /// </summary>
    public class LowtownsVillageTests
    {
        static ZoneDefinition z;
        static string Kit { get { return Path.Combine(Application.dataPath, "Crulanda", "Resources", "Props", "MedievalVillage1"); } }
        static float H(float x, float y) { return ZoneBuilder.TerraceGround(z.terraces, x, y, 0); }
        [OneTimeSetUp] public void Load() { z = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones", "lowtowns.json"))); }
        static List<ZoneBuilder.VillageRow> Rows() { return z.props.Where(ZoneBuilder.VillagePlot).Select(ZoneBuilder.PlanVillageRow).ToList(); }
        /// <summary>A point of a plot's own plan (x across, z from front -z to back) in the zone's plan.</summary>
        static Vector2 World(ZoneProp p, float x, float zz) { var o = Quaternion.Euler(0, p.rotation, 0) * new Vector3(x, 0, zz); return p.at + new Vector2(o.x, o.z); }
        static Vector2 Local(ZoneProp p, Vector2 q) { var l = Quaternion.Euler(0, -p.rotation, 0) * new Vector3(q.x - p.at.x, 0, q.y - p.at.y); return new Vector2(l.x, l.z); }
        static float SegDist(Vector2 q, Vector2 a, Vector2 b) { var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); return Vector2.Distance(q, a + ab * t); }

        [Test] public void Every_terrace_plot_gets_the_pack_houses()
        {
            var rows = Rows();
            Assert.Greater(rows.Count, 100, "The Lowtowns' terraces are the pack's now (" + rows.Count + ").");
            foreach (var row in rows)
            {
                Assert.IsNotNull(row); Assert.Greater(row.houses.Count, 0, "The terrace at " + row.prop.at + " has a house.");
                foreach (var h in row.houses)
                {
                    Assert.IsTrue(File.Exists(Path.Combine(Kit, "Prefabs", h.model.name + "_Standard.prefab")), h.model.name + "'s Built-in prefab is in the project.");
                    Assert.That(h.scale, Is.InRange(ZoneBuilder.VillageScaleMin, ZoneBuilder.VillageScaleMax), "A door the street's height (" + h.model.name + " at " + row.prop.at + ").");
                    Assert.That(Mathf.Abs(h.scaleX) / h.scale, Is.InRange(1f, 1.151f), "Widened at most 15 %.");
                    if (h.model.name == ZoneBuilder.VillageLow) Assert.AreEqual(2, row.under, "The low house only on two storeys of brick.");
                    if (row.under == 0) CollectionAssert.Contains(ZoneBuilder.VillageTall.Concat(new[] { "Blacksmith" }).ToList(), h.model.name, "On the setts, only a tall house.");
                }
                if (ZoneBuilder.TenementStoreys(row.prop.variant) == 4 && !row.smithy) Assert.AreEqual(2, row.under, "A four-storey terrace stays tall: two storeys of brick under its houses (" + row.prop.at + ").");
                if (!row.smithy) Assert.GreaterOrEqual(row.houses.Max(h => h.Top), 7.5f, "Vertical as the books have it: the row at " + row.prop.at + " stands over 7.5 m.");
            }
            Assert.AreEqual(1, rows.Count(r => r.smithy && r.houses.Single().model.name == "Blacksmith"), "The Rust Market smith's shop is the Blacksmith.");
            Assert.GreaterOrEqual(rows.SelectMany(r => r.houses).Select(h => h.model.name).Distinct().Count(), 9, "Varied: most of the pack's houses are used.");
            Assert.That(rows.SelectMany(r => r.houses).Count(h => h.scaleX < 0), Is.InRange(rows.Sum(r => r.houses.Count) / 4, rows.Sum(r => r.houses.Count) * 3 / 4), "Some mirrored, some not.");
            // The dressing's prefabs are there too.
            foreach (var n in new[] { "Pot_1", "Pot_2", "Wooden_Tub", "Wooden_Bucket", "Wooden_Barrel", "Wooden_Spinning_Wheel", "Stump_A", "Anvil", "Blacksmith_Table", "Hammer", "Sword", "Spear", "Shield", "Wood_Tyre", "Blacksmith_Logo" })
                Assert.IsTrue(File.Exists(Path.Combine(Kit, "Prefabs", n + "_Standard.prefab")), n);
        }

        [Test] public void The_roof_route_keeps_its_terraces_and_still_joins_up()
        {
            foreach (var at in ZoneBuilder.RoofRow)
            {
                var p = z.props.Single(q => q != null && q.kind == "tenement" && Vector2.Distance(q.at, at) < .6f);
                Assert.IsFalse(ZoneBuilder.VillagePlot(p), "The roof route's terrace at " + at + " keeps its brick: the lead walk stands on its gutter.");
                Assert.IsNull(ZoneBuilder.PlanVillageRow(p));
            }
            Assert.IsFalse(ZoneBuilder.VillagePlot(new ZoneProp { kind = "tenement", style = "back", at = new Vector2(0, 230), size = new Vector2(12, 8) }), "The edge's blind backs keep their brick.");
            var legs = ZoneBuilder.RoofRoute(z, H); Assert.IsNotNull(legs, "The route's terraces are all there.");
            var walks = legs.Where(l => l.kind == "deck" || l.kind == "bridge").ToList();
            var group = Enumerable.Range(0, walks.Count).ToArray();
            int Find(int i) { while (group[i] != i) i = group[i] = group[group[i]]; return i; }
            for (int i = 0; i < walks.Count; i++)
                for (int k = 0; k < walks.Count; k++)
                    if (i != k) foreach (var end in new[] { walks[i].a, walks[i].b }) if (walks[k].Off(end, out float dy) <= walks[k].width / 2 - .3f && Mathf.Abs(dy) < .1f) group[Find(i)] = Find(k);
            Assert.AreEqual(1, Enumerable.Range(0, walks.Count).Select(Find).Distinct().Count(), "The walks and rope bridges still make one route.");
        }

        [Test] public void Houses_stand_within_their_plots_doors_to_the_street()
        {
            foreach (var row in Rows())
            {
                float w = row.size.x, d = row.size.y;
                var plans = row.houses.Select(h => h.Plan(d)).ToList();
                foreach (var r in plans)
                {
                    Assert.GreaterOrEqual(r.xMin, -w / 2 - .01f, "Within its plot at " + row.prop.at); Assert.LessOrEqual(r.xMax, w / 2 + .01f, "Within its plot at " + row.prop.at);
                    Assert.AreEqual(-d / 2, r.yMin, .001f, "Its door wall on the street (the plot's front).");
                    Assert.LessOrEqual(r.yMax, d / 2 + .01f, "No deeper than the plot at " + row.prop.at);
                }
                for (int i = 1; i < plans.Count; i++) Assert.LessOrEqual(plans[i - 1].xMax, plans[i].xMin + .01f, "Side by side, not inside each other, at " + row.prop.at);
                foreach (var h in row.houses) Assert.AreEqual(row.underTop, h.baseY, .001f, "On the brick (or the plinth).");
            }
        }

        [Test] public void No_house_over_a_road_a_stair_or_a_camp()
        {
            var stairs = z.terraces.Where(t => t != null && (t.ramp || t.style == "stair" || t.style == "ramp")).ToList();
            // (data before the pack, 2026-10-19: the smugglers' pier camp's centre already stood inside the terrace at 88.74, 169.55)
            var already = new HashSet<string>(z.camps.Where(c => z.props.Any(p => p != null && p.kind == "tenement" && Mathf.Abs(Local(p, c.center).x) <= p.size.x / 2 && Mathf.Abs(Local(p, c.center).y) <= p.size.y / 2)).Select(c => c.name));
            foreach (var row in Rows())
            {
                var p = row.prop; float d = row.size.y;
                foreach (var h in row.houses)
                {
                    var r = h.Plan(d); var pts = new List<Vector2>();
                    for (int i = 0; i <= 8; i++) for (int k = 0; k <= 4; k++) pts.Add(World(p, Mathf.Lerp(r.xMin, r.xMax, i / 8f), Mathf.Lerp(r.yMin, r.yMax, k / 4f)));
                    foreach (var road in z.roads)
                    {
                        if (road == null || road.points == null || road.points.Length < 2) continue;
                        // A lane that ends at this terrace runs up to its wall (Gutter Alley, Weir Back, Weir Stair): not over it.
                        bool ends = new[] { road.points[0], road.points[road.points.Length - 1] }.Any(e => { var l = Local(p, e); return Mathf.Abs(l.x) <= row.size.x / 2 + road.width && Mathf.Abs(l.y) <= d / 2 + road.width; });
                        if (ends) continue;
                        foreach (var q in pts) for (int s = 0; s + 1 < road.points.Length; s++)
                                Assert.GreaterOrEqual(SegDist(q, road.points[s], road.points[s + 1]), road.width / 2 - .05f, h.model.name + " at " + p.at + " stands over " + road.name);
                    }
                    foreach (var st in stairs)
                        foreach (var q in pts)
                        {
                            var l = Quaternion.Euler(0, -st.rotation, 0) * new Vector3(q.x - st.center.x, 0, q.y - st.center.y);
                            Assert.IsFalse(Mathf.Abs(l.x) <= st.size.x / 2 && Mathf.Abs(l.z) <= st.size.y / 2, h.model.name + " at " + p.at + " stands on " + st.name);
                        }
                    foreach (var c in z.camps)
                    {
                        if (c == null || already.Contains(c.name)) continue;
                        var l = Local(p, c.center);
                        Assert.IsFalse(l.x >= r.xMin && l.x <= r.xMax && l.y >= r.yMin && l.y <= r.yMax, h.model.name + " at " + p.at + " stands on the camp " + c.name);
                    }
                }
            }
        }

        [Test] public void The_pack_is_the_Lowtowns_own()
        {
            // Only the Lowtowns' builder names the pack, and it builds nothing outside the Lowtowns.  (VillageKit.cs is the Quaternius village kit, another pack.)
            var scripts = Directory.GetFiles(Path.Combine(Application.dataPath, "Crulanda", "Scripts"), "*.cs", SearchOption.AllDirectories);
            var users = scripts.Where(f => { var s = File.ReadAllText(f); return s.Contains("MedievalVillage1") || s.Contains("MedievalKit"); }).Select(Path.GetFileName).ToList();
            CollectionAssert.AreEquivalent(new[] { "ZoneBuilder.LowtownVillage.cs" }, users, "Only the Lowtowns' houses use the pack.");
            StringAssert.Contains("if (Zone.biome != \"lowtowns\") return false;", File.ReadAllText(scripts.Single(f => Path.GetFileName(f) == "ZoneBuilder.LowtownVillage.cs")));
            // No scene, prefab or asset outside the pack's folder points at any of its files.
            var guids = new HashSet<string>(Directory.GetFiles(Kit, "*.meta", SearchOption.AllDirectories).Select(f => File.ReadAllLines(f).First(l => l.StartsWith("guid: ")).Substring(6).Trim()));
            Assert.Greater(guids.Count, 100);
            foreach (var ext in new[] { "*.unity", "*.prefab", "*.asset", "*.mat", "*.json" })
                foreach (var f in Directory.GetFiles(Application.dataPath, ext, SearchOption.AllDirectories))
                {
                    if (Path.GetFullPath(f).StartsWith(Path.GetFullPath(Kit))) continue;
                    var text = File.ReadAllText(f); if (!text.Contains("guid: ") && !text.Contains("MedievalVillage1")) continue;
                    Assert.IsFalse(text.Contains("MedievalVillage1"), f + " names the pack.");
                    foreach (var g in guids) if (text.Contains(g)) Assert.Fail(f + " uses the Lowtowns' pack (" + g + ").");
                }
        }

        [Test] public void No_windmill_or_water_wheel_in_the_project()
        {
            foreach (var f in Directory.GetFiles(Kit, "*", SearchOption.AllDirectories))
            {
                var n = Path.GetFileName(f);
                Assert.IsFalse(n.StartsWith("Water_Wheel") || n.StartsWith("WindMill") || n.StartsWith("Windmill"), "No machines (Chris, 2026-10-11): " + n);
            }
            // The pack's own windmill and water wheel, by their guids (the FBX, and the URP and Built-in prefabs), nowhere in the project.
            var banned = new[] { "d980eee059cd3a641b269cec8c0672df", "e3b0328d5a94c6745bc8d062ee897f82", "9b5dfb1a75f7bc248bdad826d7694adb", "9ac524a0b4f358b47811b1f189c51351", "2512dd8b485c6364a915bfa380500227", "59c2c4a87b6ee3d4891a425667f567d4" };
            foreach (var f in Directory.GetFiles(Application.dataPath, "*.meta", SearchOption.AllDirectories))
            {
                var g = File.ReadLines(f).Take(3).FirstOrDefault(l => l.StartsWith("guid: "));
                if (g != null) CollectionAssert.DoesNotContain(banned, g.Substring(6).Trim(), f);
            }
            // And none of them, nor the pack's tavern (the Rusty Spoon is the inn) or its demo scene, came in.
            foreach (var n in new[] { "Tavern", "Animal_Shed", "Demo" })
                Assert.IsEmpty(Directory.GetFiles(Kit, n + "*", SearchOption.AllDirectories), n);
        }

        [Test] public void The_pack_materials_are_Built_in_shaders()
        {
            var mats = Directory.GetFiles(Path.Combine(Kit, "Materials"), "*.mat");
            Assert.Greater(mats.Length, 50);
            var have = new HashSet<string>(Directory.GetFiles(Kit, "*.meta", SearchOption.AllDirectories).Select(f => File.ReadAllLines(f).First(l => l.StartsWith("guid: ")).Substring(6).Trim()));
            foreach (var f in mats)
            {
                var text = File.ReadAllText(f);
                StringAssert.Contains("m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000, type: 0}", text, Path.GetFileName(f) + " is on the Built-in Standard shader.");
                Assert.IsFalse(text.Contains("Universal") || text.Contains("933532a4fcc9baf4fa0491de14d08ed7"), Path.GetFileName(f) + " is no URP material (pink in this project).");
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, "guid: ([0-9a-f]{32})"))
                    if (m.Groups[1].Value != "0000000000000000f000000000000000") Assert.IsTrue(have.Contains(m.Groups[1].Value), Path.GetFileName(f) + " finds its texture " + m.Groups[1].Value);
            }
            foreach (var f in Directory.GetFiles(Path.Combine(Kit, "Prefabs"), "*.prefab"))
            {
                var text = File.ReadAllText(f);
                StringAssert.EndsWith("_Standard.prefab", f, "The pack's Built-in prefabs only.");
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, "guid: ([0-9a-f]{32})"))
                    Assert.IsTrue(have.Contains(m.Groups[1].Value), Path.GetFileName(f) + " finds its model or material " + m.Groups[1].Value);
            }
            // Each kept the pack's own .meta (its guid), never a guid-only one: a model's has its importer settings.
            foreach (var f in Directory.GetFiles(Path.Combine(Kit, "Meshes"), "*.fbx.meta")) StringAssert.Contains("ModelImporter:", File.ReadAllText(f), f);
            foreach (var f in Directory.GetFiles(Path.Combine(Kit, "Textures"), "*.png.meta")) StringAssert.Contains("TextureImporter:", File.ReadAllText(f), f);
        }
    }
}
