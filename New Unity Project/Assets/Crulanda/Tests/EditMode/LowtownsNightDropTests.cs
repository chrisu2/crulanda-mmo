using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Lowtowns' rework, picks 10 and 8 (Docs/REWORK_LOWTOWNS_HALLOWS.md; Docs/ZONE_LOWTOWNS.md "Dead Man's Drop and the Night Market"),
    /// read from the zone's data without building it (the ground is the terraces, ZoneBuilder.TerraceGround):
    /// - Dead Man's Drop is a deep pit in the Rust Market's shelf (book1 ch.1:313), a lane running to its lip, a place with the books' lines
    ///   and a view; nothing else stands in it or at its lip; the net hangs three stories down (ch.1:347) and the rungs let you out lower;
    ///   a fall takes a little and never the last of your health;
    /// - the Night Market's three keepers live in the market, labelled (the tales cited: lore_stories.md #6 and #9), with their stories, on
    ///   level ground; the chalk ring stands clear of everything.
    /// </summary>
    public class LowtownsNightDropTests
    {
        static ZoneDefinition z;
        [OneTimeSetUp] public void Load() { z = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones", "lowtowns.json"))); }
        static float H(float x, float y) { return ZoneBuilder.TerraceGround(z.terraces, x, y, 0); }
        static float H(Vector2 p) { return H(p.x, p.y); }
        static float Range(Vector2 p, float r)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = -1; i <= 1; i++) for (int k = -1; k <= 1; k++) { float h = H(p.x + i * r, p.y + k * r); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            return hi - lo;
        }
        static bool Near(ZonePit pit, Vector2 p, float pad)
        {
            return Mathf.Abs(p.x - pit.center.x) <= pit.size.x / 2 + pad && Mathf.Abs(p.y - pit.center.y) <= pit.size.y / 2 + pad;
        }

        [Test] public void Dead_Mans_Drop_is_a_deep_pit_in_the_Rust_Market()
        {
            var pit = ZoneBuilder.DropPit(z); Assert.NotNull(pit, "The Drop is a pit in the zone's data.");
            Assert.AreEqual(0, pit.rotation, "Square to the lanes (the gap is on its west side).");
            Assert.GreaterOrEqual(pit.depth, 25, "A deep dark drop (book1 ch.1:313: 'bottomless').");
            Assert.AreEqual(0, pit.fog, 1e-4f, "No Hallow's Creek fog floor: the net catches you.");
            Assert.That(pit.size.x, Is.InRange(8f, 16f)); Assert.That(pit.size.y, Is.InRange(8f, 16f));
            float lip = H(pit.center);
            Assert.AreEqual(8, lip, .05f, "In the Rust Market's lanes (book1 ch.1:313), on its shelf.");
            for (int i = -1; i <= 1; i++) for (int k = -1; k <= 1; k++)
                Assert.AreEqual(lip, H(pit.center.x + i * (pit.size.x / 2 + 3), pit.center.y + k * (pit.size.y / 2 + 3)), .05f, "level all round the lip");
            Assert.That(ZoneBuilder.NetBelow, Is.InRange(8.5f, 11f), "The net three stories down (book1 ch.1:347).");
            Assert.Less(ZoneBuilder.NetBelow, pit.depth - 10, "and the dark goes on far under it.");
            Assert.Less(ZoneBuilder.DropSide, Mathf.Min(pit.size.x, pit.size.y) / 2 - 3, "The net is wide enough to land on.");
            Assert.Less(H(ZoneBuilder.DropOut), lip - 3, "The rungs let you out on a lower lane.");
            Assert.LessOrEqual(Range(ZoneBuilder.DropOut, 1.5f), .3f, "on level ground");
            Assert.IsTrue(z.roads.Any(r => r.name == "Cooper's Row" && r.points.Any(p => Vector2.Distance(p, ZoneBuilder.DropOut) < 20)), "on Cooper's Row");
        }

        [Test] public void A_lane_ends_at_the_lip_and_the_place_cites_the_book()
        {
            var pit = ZoneBuilder.DropPit(z);
            var lane = z.roads.Single(r => r.name == "Drop Lane");
            var end = lane.points.Last(); var lipMid = new Vector2(pit.center.x - pit.size.x / 2, pit.center.y);
            Assert.Less(Vector2.Distance(end, lipMid), 2.5f, "The lane runs to the gap in the railing (the west side's middle).");
            Assert.Less(Mathf.Abs(end.y - pit.center.y), ZoneBuilder.DropGap / 2, "straight at the gap");
            Assert.IsTrue(z.roads.Any(r => r != lane && r.points.Any(p => Vector2.Distance(p, lane.points[0]) < 1) || r != lane && Close(r, lane.points[0])), "It leaves another street.");
            foreach (var p in lane.points) Assert.AreEqual(8, H(p), .05f, "The lane is on the market's level at " + p);
            var place = z.landmarks.Single(l => l.name == ZoneBuilder.DropName);
            StringAssert.Contains("ch.1:313", place.canonStatus); StringAssert.Contains("ch.1:347", place.canonStatus);
            StringAssert.StartsWith("CANON", place.canonStatus);
            StringAssert.Contains("vanishes", place.text, "The book's street that simply vanished.");
            Assert.AreNotEqual(Vector2.zero, place.view, "A view for the capture tour.");
            Assert.LessOrEqual(Vector2.Distance(place.at, lipMid), place.radius, "Found at the lip.");
        }
        static bool Close(ZonePath r, Vector2 q)
        {
            for (int i = 0; i + 1 < r.points.Length; i++)
            {
                var a = r.points[i]; var b = r.points[i + 1]; var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                if (Vector2.Distance(q, a + ab * t) < 1.5f) return true;
            }
            return false;
        }

        [Test] public void Nothing_stands_in_the_Drop_or_on_its_lip()
        {
            var pit = ZoneBuilder.DropPit(z);
            foreach (var p in z.props) Assert.IsFalse(Near(pit, p.at, p.kind == "tenement" ? 1 : 2.5f), p.kind + " '" + p.name + "' at " + p.at);
            foreach (var c in z.camps) Assert.IsFalse(Near(pit, c.center, c.radius + 4), "camp " + c.name);
            foreach (var n in z.nodes) Assert.IsFalse(Near(pit, n.at, 4), "node " + n.node);
            foreach (var s in z.secrets) Assert.IsFalse(Near(pit, s.at, 4), "secret " + s.id);
            foreach (var r in z.life.residents.Where(r => r.at != Vector2.zero)) Assert.IsFalse(Near(pit, r.at, 4), "resident " + r.name);
            foreach (var s in z.stations) Assert.IsFalse(Near(pit, s.at, 4), "station " + s.name);
            foreach (var r in z.roads.Where(r => r.name != "Drop Lane"))
                foreach (var p in r.points) Assert.IsFalse(Near(pit, p, 3), "road " + r.name);
        }

        [Test] public void A_fall_onto_the_net_hurts_a_little_and_never_kills()
        {
            Assert.AreEqual(80, EncounterSession.DropBite(1000, 1000), "8% of your health");
            Assert.AreEqual(1, EncounterSession.DropBite(10, 10), "at least one");
            Assert.AreEqual(0, EncounterSession.DropBite(1, 1000), "never the last of it");
            Assert.AreEqual(4, EncounterSession.DropBite(5, 1000));
            StringAssert.Contains("three stories down", EncounterSession.DropLandLine, "The book's net (book1 ch.1:347).");
        }

        [Test] public void The_Night_Market_keepers_live_in_the_market()
        {
            var folk = z.life.residents.Where(r => ZoneBuilder.NightFolk.Contains(r.name)).ToList();
            Assert.AreEqual(ZoneBuilder.NightFolk.Length, folk.Count, "All three keepers live here.");
            foreach (var r in folk)
            {
                Assert.IsFalse(string.IsNullOrEmpty(r.canonStatus), r.name + " is labelled.");
                Assert.GreaterOrEqual(r.stories.Length, 3, r.name + " has three stories.");
                Assert.AreEqual(8, H(r.at), .05f, r.name + " is in the Rust Market (book1 ch.3:5).");
                Assert.LessOrEqual(Range(r.at, 2), .3f, r.name + " stands on level ground with room for a stall.");
                Assert.IsFalse(r.stories.Any(s => s.Contains("Valen")), "The books' boy is not named.");
                foreach (var p in z.props.Where(p => p.kind == "stall" || p.kind == "market_hall" || p.kind == "tenement"))
                    Assert.Greater(Vector2.Distance(p.at, r.at), 3.5f, r.name + "'s night stall is clear of " + p.kind + " '" + p.name + "'");
            }
            StringAssert.Contains("lore_stories.md #9", folk.Single(r => r.name == ZoneBuilder.MerchantOfNothing).canonStatus);
            StringAssert.Contains("lore_stories.md #6", folk.Single(r => r.name == ZoneBuilder.Candlemaker).canonStatus);
            StringAssert.StartsWith("GAME-ONLY", folk.Single(r => r.name == ZoneBuilder.CardReader).canonStatus, "The card-reader and the chalk ring are the game's.");
            Assert.IsTrue(folk.Single(r => r.name == ZoneBuilder.MerchantOfNothing).stories.Any(s => s.Contains("premium nothingness")), "His own words (lore #9).");
            Assert.IsTrue(folk.Single(r => r.name == ZoneBuilder.Candlemaker).stories.Any(s => s.Contains("butterflies")), "The light-butterflies (lore #6).");
            Assert.LessOrEqual(Range(ZoneBuilder.ChalkRing, ZoneBuilder.ChalkRingRadius), .3f, "The chalk ring lies on level ground.");
            foreach (var p in z.props) Assert.Greater(Vector2.Distance(p.at, ZoneBuilder.ChalkRing), ZoneBuilder.ChalkRingRadius + 1.5f, "The ring is clear of " + p.kind + " '" + p.name + "'");
            foreach (var r in z.roads) Assert.IsFalse(Close(r, ZoneBuilder.ChalkRing), "The ring is off the road " + r.name);
        }
    }
}
