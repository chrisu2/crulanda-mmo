using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Lowtowns' landmarks, turf and crossings (Lowtowns pick 12; Docs/ZONE_LOWTOWNS.md "Landmarks, turf and crossings"), read from the
    /// zone's data and ZoneBuilder.LowtownMarks' places without building the zone:
    /// - the Iron Teeth hold one of three corners a day, a new one each day in turn; every corner is well away from their warehouse
    ///   (neutral ground, book1 ch.7:21); each corner's two runners live in the zone, stand by its banner on level ground, cite ch.7:21, and
    ///   no Copper-Tithe is among them;
    /// - the punt's two landings and the weir's two ends are on dry, level ground on opposite banks of the river, clear of the barges and
    ///   the eels' shallows; the crossing takes a few seconds;
    /// - the Stack stands on the High Rows' level shelf, clear of every prop; the Rag Bridge is still there to build its houses on.
    /// </summary>
    public class LowtownsMarksTests
    {
        static ZoneDefinition z;
        [OneTimeSetUp] public void Load() { z = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones", "lowtowns.json"))); }
        static float H(Vector2 p) { return ZoneBuilder.TerraceGround(z.terraces, p.x, p.y, 0); }
        static float Range(Vector2 p, float r)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = -1; i <= 1; i++) for (int k = -1; k <= 1; k++) { float h = H(p + new Vector2(i * r, k * r)); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            return hi - lo;
        }
        static ZonePath River { get { return z.water.Single(w => w.name == "The Chemical River"); } }
        /// <summary>The river's middle at a z (it runs north to south).</summary>
        static float RiverX(float zz)
        {
            var p = River.points;
            for (int i = 0; i + 1 < p.Length; i++)
                if ((zz - p[i].y) * (zz - p[i + 1].y) <= 0) return Mathf.Lerp(p[i].x, p[i + 1].x, Mathf.InverseLerp(p[i].y, p[i + 1].y, zz));
            return p[0].x;
        }

        [Test] public void The_Iron_Teeth_hold_a_corner_a_day_never_the_warehouse()
        {
            Assert.AreEqual(3, ZoneBuilder.TurfCorners.Length, "Three corners (the plan's Sett Bridge foot, Teeth Lane, the weir).");
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, Enumerable.Range(10, 3).Select(ZoneBuilder.TurfCornerOn), "Each corner in turn over three days");
            for (int d = -5; d < 20; d++) Assert.AreNotEqual(ZoneBuilder.TurfCornerOn(d), ZoneBuilder.TurfCornerOn(d + 1), "and never the same two days running (day " + d + ").");
            var warehouse = z.landmarks.Single(l => l.name == "The Iron Teeth's Warehouse").at;
            var people = z.life.residents.ToDictionary(r => r.name);
            foreach (var c in ZoneBuilder.TurfCorners)
            {
                Assert.Greater(Vector2.Distance(c.banner, warehouse), 40, c.name + ": the warehouse is neutral ground, never turf (book1 ch.7:21).");
                Assert.LessOrEqual(Range(c.banner, .5f), .05f, c.name + "'s banner on level ground");
                Assert.AreEqual(2, c.folk.Length, c.name + ": a pair of runners");
                foreach (var n in c.folk)
                {
                    Assert.IsTrue(people.ContainsKey(n), n + " lives in the Lowtowns.");
                    var r = people[n];
                    Assert.AreEqual(c.name, ZoneBuilder.TurfCorners[ZoneBuilder.TurfCornerOf(n)].name);
                    Assert.LessOrEqual(Vector2.Distance(r.at, c.banner), 8, n + " stands at " + c.name);
                    Assert.Greater(Vector2.Distance(r.at, warehouse), 40, n + " is not at the warehouse.");
                    Assert.LessOrEqual(Range(r.at, 1), .3f, n + " on level ground");
                    StringAssert.Contains("ch.7:21", r.canonStatus, n + " cites the books.");
                    StringAssert.StartsWith("GAME-ONLY", r.canonStatus);
                    Assert.IsFalse(r.stories.Any(s => s.Contains("Copper-Tithe")) || r.title.Contains("Copper"), n + ": the Copper-Tithe stay out (conflict 2).");
                }
            }
            Assert.AreEqual(6, ZoneBuilder.TurfFolk.Distinct().Count(), "Six runners, two to a corner.");
            Assert.IsFalse(z.life.residents.Any(r => (r.title ?? "").Contains("Copper-Tithe")), "No Copper-Tithe in the Lowtowns.");
        }

        [Test] public void The_punt_and_the_weir_cross_the_river_bank_to_bank()
        {
            float half = River.width / 2;
            void Banks(Vector2 a, Vector2 b, string what)
            {
                float da = a.x - RiverX(a.y), db = b.x - RiverX(b.y);
                Assert.Less(da * db, 0, what + ": one end each side of the river");
                Assert.That(Mathf.Abs(da), Is.InRange(half + 1, half + 9), what + "'s west end on the bank");
                Assert.That(Mathf.Abs(db), Is.InRange(half + 1, half + 9), what + "'s east end on the bank");
            }
            Banks(ZoneBuilder.PuntWest, ZoneBuilder.PuntEast, "The punt");
            Banks(ZoneBuilder.WeirWest, ZoneBuilder.WeirEast, "The weir");
            foreach (var p in new[] { ZoneBuilder.PuntWest, ZoneBuilder.PuntEast }) Assert.LessOrEqual(Range(p, 1), .05f, "A landing on level ground at " + p);
            foreach (var b in z.props.Where(p => p.kind == "barge" || p.kind == "dock"))
            {
                float reach = Mathf.Max(b.size.x, b.size.y) / 2 + 2;
                Assert.Greater(Mathf.Abs(b.at.y - ZoneBuilder.WeirWest.y), ZoneBuilder.WeirCrest / 2 + (b.kind == "barge" ? 3.5f : 1), b.kind + " '" + b.name + "' clear of the weir");
                Assert.Greater(Mathf.Abs(b.at.y - ZoneBuilder.PuntWest.y), b.kind == "dock" ? reach - 2 : 4, b.kind + " '" + b.name + "' clear of the ferry");
            }
            foreach (var c in z.camps.Where(c => c.tag == "sludgeeel")) Assert.Greater(Mathf.Abs(c.center.y - ZoneBuilder.WeirWest.y), c.radius, c.name + " keep off the weir's crest");
            Assert.That(EncounterSession.PuntSeconds, Is.InRange(3f, 8f), "A short crossing");
        }

        [Test] public void The_Stack_stands_on_the_High_Rows_clear_of_everything()
        {
            var at = ZoneBuilder.StackAt;
            var rows = z.terraces.Single(t => t.name == "The High Rows");
            Assert.AreEqual(rows.height, H(at), .05f, "On the High Rows' shelf");
            Assert.LessOrEqual(Range(at, 6), .05f, "and level all under it.");
            foreach (var p in z.props) Assert.Greater(Vector2.Distance(p.at, at), 10, p.kind + " '" + p.name + "' clear of the Stack");
            foreach (var r in z.roads) foreach (var p in r.points) Assert.Greater(Vector2.Distance(p, at), 8, r.name);
            Assert.Greater(ZoneBuilder.StackStoreys * ZoneBuilder.StoreyHeight, 25, "Tall enough to steer by.");
            Assert.IsTrue(z.props.Any(p => p.kind == "brick_bridge" && p.name == "The Rag Bridge"), "The Rag Bridge to build its houses on.");
        }
    }
}
