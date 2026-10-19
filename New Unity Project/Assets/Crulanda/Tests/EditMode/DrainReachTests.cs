using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Drain Reach's ground (Docs/WORLD_BOSS_1.md 1.3, build step 3; lowtowns.json "shoals", ZoneWater, ZoneBuilder.DrainReach), read from
    /// the zone's data without building it: the Chemical River's water worked out from the terraces' ground as the builder does it, then carved.
    /// - the silt bar is about 30 m by 16 m in the river's north reach above the Rag Bridge, level, dry and standable, and the river runs round
    ///   it on both sides, chemical and wadeable (not swim-deep);
    /// - a neck of mud joins it to the west bank, dry all the way; the apron is level ground on the west bank, dry, about 9 m by 30 m;
    /// - nothing stands over the bar's walk (no camp, node, find, station, person, exit, road, prop or terrace on the bar, its neck or the
    ///   apron);
    /// - away from the reach the river is as it was.
    /// </summary>
    public class DrainReachTests
    {
        static ZoneDefinition z; static ZoneWater water;
        static ZoneDefinition Load() { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones", "lowtowns.json"))); }
        static ZoneWater Water(ZoneDefinition zone) { var w = new ZoneWater(); w.Prepare(zone, (x, y) => ZoneBuilder.TerraceGround(zone.terraces, x, y, 0)); return w; }
        [OneTimeSetUp] public void Build() { z = Load(); water = Water(z); }
        static float G(float x, float y) { return water.Carve(x, y, ZoneBuilder.TerraceGround(z.terraces, x, y, 0)); }
        static bool Wet(float x, float y, out float depth) { return water.At(new Vector2(x, y), G(x, y), out _, out depth); }
        static ZoneShoal Shoal(string name) { return z.shoals.Single(s => s.name == name); }
        /// <summary>A point in a shoal's frame (x across, z along) to the world.</summary>
        static Vector2 W(ZoneShoal s, float lx, float lz)
        {
            float r = s.rotation * Mathf.Deg2Rad, c = Mathf.Cos(r), sn = Mathf.Sin(r);
            return new Vector2(s.center.x + lx * c + lz * sn, s.center.y - lx * sn + lz * c);
        }

        [Test] public void The_silt_bar_stands_in_the_river_level_and_dry_with_the_water_round_it()
        {
            var bar = Shoal(ZoneBuilder.SiltBarName);
            Assert.AreEqual("silt", bar.kind); StringAssert.StartsWith("GAME-ONLY", bar.canonStatus, "labelled");
            Assert.That(bar.size.y, Is.InRange(26f, 34f), "about 30 m long"); Assert.That(bar.size.x, Is.InRange(14f, 18f), "and 16 m wide");
            Assert.Greater(bar.center.y, 96 + 30, "in the north reach, above the Rag Bridge (5.6, 96)");
            var w = water.Shoals.SingleOrDefault(s => s.def == bar); Assert.IsNotNull(w, "the bar lies in a river");
            Assert.AreEqual(ZoneBuilder.ChemicalKind, w.creek.def.kind, "the Chemical River");
            Assert.AreEqual("The Chemical River", w.creek.def.name);
            // Level and dry over its top (a little in from its edge, where it eases into the bed).
            float lo = float.MaxValue, hi = float.MinValue;
            for (float lx = -bar.size.x / 2 + .5f; lx <= bar.size.x / 2 - .5f; lx += 1)
                for (float lz = -bar.size.y / 2 + .5f; lz <= bar.size.y / 2 - .5f; lz += 1)
                {
                    var p = W(bar, lx, lz); float h = G(p.x, p.y);
                    lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                    Assert.IsFalse(Wet(p.x, p.y, out _), "dry on the bar at " + p);
                }
            Assert.LessOrEqual(hi - lo, .3f, "the bar is level");
            Assert.Greater(lo, w.top - bar.rise + .2f, "and stands over the water (its top " + bar.rise + " m over it)");
            // The river round it on both sides, along its length: wet, wadeable (never swim-deep), and the Chemical River's.
            foreach (int side in new[] { -1, 1 })
                for (float lz = -bar.size.y / 2 + 2; lz <= bar.size.y / 2 - 2; lz += 2)
                {
                    var p = W(bar, side * (bar.size.x / 2 + bar.blend + 2.5f), lz);
                    if (z.shoals.Any(s => s != bar && ZoneWater.ShoalOut(s, p.x, p.y) < s.blend + .5f)) continue;   // the neck crosses the west side
                    Assert.IsTrue(Wet(p.x, p.y, out float depth), "the river runs round the bar (" + (side < 0 ? "west" : "east") + ") at " + p);
                    Assert.Less(depth, ZoneWater.SwimDepth, "wadeable beside the bar at " + p);
                }
        }

        [Test] public void A_neck_of_mud_joins_the_bar_to_the_apron_on_the_west_bank()
        {
            var bar = Shoal(ZoneBuilder.SiltBarName); var neck = Shoal("The silt bar's neck"); var apron = Shoal(ZoneBuilder.ApronName);
            Assert.AreEqual("apron", apron.kind); StringAssert.StartsWith("GAME-ONLY", apron.canonStatus); StringAssert.StartsWith("GAME-ONLY", neck.canonStatus);
            Assert.Less(apron.center.x, bar.center.x, "the apron is on the west bank");
            Assert.That(apron.size.x, Is.InRange(8f, 12f), "about 10 m of setts"); Assert.GreaterOrEqual(apron.size.y, bar.size.y - 2, "along the bar's length");
            // The apron: level ground (the streets' level) and dry.
            float lo = float.MaxValue, hi = float.MinValue;
            for (float lx = -apron.size.x / 2 + .5f; lx <= apron.size.x / 2 - .5f; lx += 1)
                for (float lz = -apron.size.y / 2 + .5f; lz <= apron.size.y / 2 - .5f; lz += 1)
                {
                    var p = W(apron, lx, lz); float h = G(p.x, p.y); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                    Assert.IsFalse(Wet(p.x, p.y, out _), "the apron is dry at " + p);
                }
            Assert.LessOrEqual(hi - lo, .3f, "the apron is level"); Assert.AreEqual(0, (lo + hi) / 2, .3f, "at the bank's level");
            // From the bar's middle across the neck to the apron: dry the whole way, no step over a kerb's height.
            var from = W(bar, 0, neck.center.y - bar.center.y); var to = apron.center; to.y = from.y; float last = G(from.x, from.y);
            for (float t = 0; t <= 1; t += .02f)
            {
                var p = Vector2.Lerp(from, to, t); float h = G(p.x, p.y);
                Assert.IsFalse(Wet(p.x, p.y, out _), "dry across the neck at " + p);
                Assert.LessOrEqual(Mathf.Abs(h - last), .25f, "no step at " + p); last = h;
            }
        }

        [Test] public void Nothing_stands_over_the_bars_walk()
        {
            const float margin = 2;
            foreach (var s in z.shoals)
            {
                bool Over(Vector2 p, float pad = 0) { return ZoneWater.ShoalOut(s, p.x, p.y) < margin + pad; }
                foreach (var c in z.camps) Assert.IsFalse(Over(c.center, c.radius), "camp " + c.name + " on " + s.name);
                foreach (var n in z.nodes) Assert.IsFalse(Over(n.at), "node " + n.node + " at " + n.at + " on " + s.name);
                foreach (var f in z.secrets) Assert.IsFalse(Over(f.at), "find " + f.id + " on " + s.name);
                foreach (var st in z.stations) Assert.IsFalse(Over(st.at), "station " + st.name + " on " + s.name);
                foreach (var e in z.exits) Assert.IsFalse(Over(e.at, e.radius), "exit " + e.name + " on " + s.name);
                foreach (var p in z.life.residents.Where(p => p.at != Vector2.zero)) Assert.IsFalse(Over(p.at), "resident " + p.name + " on " + s.name);
                foreach (var p in z.props) Assert.IsFalse(Over(p.at, Mathf.Max(p.size.x, p.size.y) / 2), p.kind + " '" + p.name + "' on " + s.name);
                foreach (var p in z.props) if (p.points != null) foreach (var q in p.points) Assert.IsFalse(Over(q), p.kind + " '" + p.name + "' runs over " + s.name);
                // No terrace's rectangle reaches over it (a stair, a shelf's edge): its ground is the river's and the shoal's.
                for (float lx = -s.size.x / 2 - margin; lx <= s.size.x / 2 + margin; lx += 1)
                    for (float lz = -s.size.y / 2 - margin; lz <= s.size.y / 2 + margin; lz += 1)
                    {
                        var p = W(s, lx, lz);
                        foreach (var t in z.terraces) { ZoneBuilder.TerraceFrame(t, p.x, p.y, out float tx, out float tz); Assert.Greater(ZoneBuilder.TerraceOut(t, tx, tz), 0, "terrace " + t.name + " over " + s.name + " at " + p); }
                    }
                foreach (var r in z.roads)
                    for (int i = 0; i + 1 < r.points.Length; i++)
                        for (float k = 0; k <= 1; k += .05f) Assert.IsFalse(Over(Vector2.Lerp(r.points[i], r.points[i + 1], k), r.width / 2), "road " + r.name + " over " + s.name);
            }
        }

        [Test] public void Away_from_the_reach_the_river_is_as_it_was()
        {
            var plain = Load(); plain.shoals = new ZoneShoal[0]; var before = Water(plain);
            var bar = Shoal(ZoneBuilder.SiltBarName);
            Assert.IsTrue(Wet(bar.center.x, bar.center.y - 60, out float d1) && before.At(new Vector2(bar.center.x, bar.center.y - 60), before.Carve(bar.center.x, bar.center.y - 60, 0), out _, out _), "the river 60 m below the bar");
            foreach (var p in new[] { new Vector2(4, 120), new Vector2(8, 60), new Vector2(2, 0), new Vector2(2, -120) })
                Assert.AreEqual(before.Carve(p.x, p.y, ZoneBuilder.TerraceGround(plain.terraces, p.x, p.y, 0)), G(p.x, p.y), .01f, "the river's bed at " + p);
            // Where the bar is now, the river had run deep (swim-deep in its middle).
            Assert.IsTrue(before.At(bar.center, before.Carve(bar.center.x, bar.center.y, 0), out _, out float was), "the river ran where the bar is");
            Assert.Greater(was, ZoneWater.SwimDepth, "swim-deep there before");
        }
    }
}
