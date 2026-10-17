using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The world map reads clearly (Chris, 2026-10-17: "map is not clear"): at the default size no two zone label plates overlap, no
    /// plate covers another zone's pin or leaves the map, the layout is the same each time, the roads are the real ones once each, and
    /// the pins sit on their places on the painted map (the Lowtowns at the foot of Argentis, not up in the floating city; the Adit
    /// in the Shattered Peaks; Hallow's Creek east of Oakhaven).
    /// </summary>
    public class WorldMapLabelTests
    {
        static string Zones { get { return Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones"); } }
        static List<ZoneDefinition> all;
        [OneTimeSetUp] public void Load()
        {
            all = new List<ZoneDefinition>();
            foreach (var f in Directory.GetFiles(Zones, "*.json")) all.Add(JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(f)));
        }
        static ZoneDefinition Z(string id) { return all.Find(z => z.id == id); }
        static Rect[] Layout(Rect map, out Vector2[] dots, out List<ZoneDefinition> pinned)
        {
            pinned = HudMaps.WorldMapZones(all); float scale = HudMaps.WorldMapScale(map);
            dots = new Vector2[pinned.Count]; var sizes = new Vector2[pinned.Count];
            for (int i = 0; i < pinned.Count; i++)
            {
                dots[i] = new Vector2(map.x + pinned[i].worldMapPosition.x * map.width, map.y + pinned[i].worldMapPosition.y * map.height);
                sizes[i] = HudMaps.WorldPlateSizeEstimate(pinned[i].displayName, HudMaps.Band(pinned[i]), scale);
            }
            return HudMaps.PlaceWorldLabels(map, dots, sizes, HudMaps.WorldMapTitles);
        }

        [Test] public void No_two_label_plates_overlap_at_the_default_size()
        {
            var map = HudMaps.DefaultWorldMapRect;
            var plates = Layout(map, out var dots, out var pinned);
            Assert.GreaterOrEqual(pinned.Count, 9, "Every zone has a pin.");
            for (int i = 0; i < plates.Length; i++)
            {
                Assert.IsTrue(map.Contains(plates[i].min) && map.Contains(plates[i].max), pinned[i].id + "'s label stays on the map.");
                for (int j = i + 1; j < plates.Length; j++)
                    Assert.AreEqual(0f, HudMaps.Overlap(plates[i], plates[j]), pinned[i].id + "'s label overlaps " + pinned[j].id + "'s.");
                for (int j = 0; j < dots.Length; j++)
                    Assert.IsFalse(plates[i].Contains(dots[j]), pinned[i].id + "'s label covers " + pinned[j].id + "'s pin.");
                Assert.Less(Vector2.Distance(dots[i], plates[i].center), 200f, pinned[i].id + "'s label stays near its pin.");
            }
        }

        [Test] public void Labels_stay_clear_at_a_bigger_map_and_the_layout_is_the_same_each_time()
        {
            var big = new Rect(100, 50, 900, 900);
            var a = Layout(big, out _, out _); var b = Layout(big, out _, out _);
            for (int i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i], b[i], "The same input gives the same layout.");
                for (int j = i + 1; j < a.Length; j++) Assert.AreEqual(0f, HudMaps.Overlap(a[i], a[j]), "Plates " + i + " and " + j + " overlap at 900 px.");
            }
            Assert.Greater(HudMaps.WorldNameFont(HudMaps.WorldMapScale(big)), HudMaps.WorldNameFont(1), "The text grows with the map.");
            Assert.Less(HudMaps.WorldBandFont(1), HudMaps.WorldNameFont(1), "The level band is smaller than the name.");
        }

        [Test] public void The_roads_are_the_real_links_once_each()
        {
            var pinned = HudMaps.WorldMapZones(all); var roads = HudMaps.WorldMapRoads(pinned);
            var seen = new HashSet<string>();
            foreach (var r in roads)
            {
                Assert.IsTrue(seen.Add(r.Key.id + "|" + r.Value.id), "Drawn once: " + r.Key.id + " to " + r.Value.id);
                bool linked = System.Array.Exists(r.Key.exits, e => e.to == r.Value.id) || System.Array.Exists(r.Value.exits, e => e.to == r.Key.id);
                Assert.IsTrue(linked, r.Key.id + " to " + r.Value.id + " is a road out of one of them.");
            }
            foreach (var a in pinned)
                foreach (var e in a.exits)
                    if (pinned.Exists(z => z.id == e.to))
                        Assert.IsTrue(roads.Exists(r => (r.Key.id == a.id && r.Value.id == e.to) || (r.Value.id == a.id && r.Key.id == e.to)), a.id + " to " + e.to + " is drawn.");
        }

        [Test] public void Pins_sit_on_their_places_on_the_painted_map()
        {
            Vector2 P(string id) { return Z(id).worldMapPosition; }
            // The floating city on the picture: roughly x .38-.60, y .28-.45 (its rock's tip at about .49, .48).
            Assert.That(P("zone.crown").x, Is.InRange(.40f, .58f)); Assert.That(P("zone.crown").y, Is.InRange(.30f, .42f), "The Crown on the floating city.");
            Assert.Greater(P("zone.lowtowns").y, .45f, "The Lowtowns at the foot of Argentis, below the floating city (ZONE_LOWTOWNS.md: at its base), not up in it.");
            Assert.That(P("zone.lowtowns").x, Is.InRange(.38f, .60f), "Under Argentis.");
            // Mount Umbra on the picture: roughly x .30-.62, y .50-.72.
            foreach (var id in new[] { "zone.peaks", "zone.adit" })
            {
                Assert.That(P(id).x, Is.InRange(.40f, .62f), id + " on Mount Umbra."); Assert.That(P(id).y, Is.InRange(.52f, .66f), id + " on Mount Umbra.");
            }
            Assert.Less(Vector2.Distance(P("zone.adit"), P("zone.peaks")), .12f, "The Adit within the Shattered Peaks.");
            Assert.Greater(P("zone.hallows").x, P("zone.oakhaven").x, "Hallow's Creek east of Oakhaven, toward the Corrupted Forests.");
            Assert.Greater(P("zone.ashrim").y, P("zone.oakhaven").y, "The Ashland Rim south of Oakhaven.");
            Assert.Less(P("zone.khaven").x, P("zone.oakhaven").x, "Khaven west of Oakhaven, at Mount Umbra's foot.");
            Assert.Less(P("zone.verdant").x, .2f, "The Verdant Shore on the west coast.");
        }
    }
}
