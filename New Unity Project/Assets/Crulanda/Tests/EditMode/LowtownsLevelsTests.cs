using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Lowtowns' rework, picks 1, 2 and 6 (Docs/REWORK_LOWTOWNS_HALLOWS.md; Docs/ZONE_LOWTOWNS.md "Built downhill", "The books' colours",
    /// "The Chemical River"), read from the zone's data (EncounterContent/Zones/lowtowns.json) without building it: the ground is the terraces
    /// alone (the zone is flat under them, hillHeight 0), worked out by ZoneBuilder.TerraceGround as the builder does.
    /// - the town falls from the Rust Market (the top, against the Forge District's wall) to the river, the Gate Ward and the docks; the Drain
    ///   Courts are sunk under the High Rows; every terrace is labelled, and those the books seed cite them;
    /// - every road climbs between the levels by a stair or a ramp (never steeper than 31 degrees along it or across it), and every ramp is
    ///   one of those two;
    /// - every camp, node, secret, station, resident, exit and the start stand on level ground, and every building on ground that falls
    ///   less than its footing reaches;
    /// - the Chemical River is chemical (black, it poisons, it hides you), with its sludge-eels in its shallows and the rare Old Croc, and
    ///   the rules do what ChemicalRiver says;
    /// - the air is the books' colours: a dirty yellow haze by day, grey smog by night.
    /// </summary>
    public class LowtownsLevelsTests
    {
        static ZoneDefinition z;
        [OneTimeSetUp] public void Load() { z = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones", "lowtowns.json"))); }
        static float H(float x, float y) { return ZoneBuilder.TerraceGround(z.terraces, x, y, 0); }
        static float H(Vector2 p) { return H(p.x, p.y); }
        static Vector2 Place(string name) { return z.landmarks.Single(l => l.name == name).at; }
        /// <summary>How much the ground climbs or falls within <paramref name="r"/> of a point.</summary>
        static float Range(Vector2 p, float r)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = -1; i <= 1; i++) for (int k = -1; k <= 1; k++) { float h = H(p.x + i * r, p.y + k * r); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            return hi - lo;
        }

        [Test] public void The_town_falls_from_the_market_to_the_river()
        {
            Assert.AreEqual(0, z.hillHeight, "Under the terraces the zone is flat.");
            Assert.Greater(z.terraces.Length, 10, "Shelves and the stairs and ramps between them.");
            foreach (var t in z.terraces)
            {
                Assert.IsFalse(string.IsNullOrEmpty(t.canonStatus), t.name + " is labelled.");
                Assert.IsTrue(t.style == "shelf" || t.style == "stair" || t.style == "ramp", t.name + ": a shelf, a stair or a ramp.");
                if (t.canonStatus.StartsWith("CANON")) StringAssert.IsMatch("book1 ch\\.|world_bible", t.canonStatus, t.name + " cites the books (GAME_BRIEF.md: design docs reference the books).");
            }
            float market = H(Place("The Rust Market")), gate = H(Place("The Gate Ward")), docks = H(Place("The Docks")), courts = H(Place("The Drain Courts"));
            Assert.GreaterOrEqual(market, 7, "The Rust Market is the top of the town (book1 ch.3:7).");
            Assert.AreEqual(market, H(Place("The Forge District Gate")), .05f, "against the Forge District's wall, on the market's level");
            Assert.Greater(market, H(60, 50), "The Lower Market a shelf under it");
            Assert.Greater(H(60, 50), H(Place("The Chemical River")), "and the river under that.");
            Assert.AreEqual(0, gate, .05f, "The Gate Ward is low."); Assert.AreEqual(0, docks, .05f, "The docks are low.");
            Assert.AreEqual(0, H(z.spawns.player), .05f, "The road from Oakhaven comes in at the bottom.");
            Assert.Less(courts, -2, "The Drain Courts are sunk under the streets");
            Assert.Greater(H(-75, 180) - courts, 6, "and the High Rows stand well over them.");
            Assert.Greater(H(Place("The Shard-Burners' Yard")), 2, "The Shard-Burners' Yard has a shelf of its own");
        }

        [Test] public void Every_road_climbs_by_a_stair_or_a_ramp_never_a_cliff()
        {
            float steep = Mathf.Tan(31 * Mathf.Deg2Rad);
            foreach (var r in z.roads)
            {
                for (int i = 0; i + 1 < r.points.Length; i++)
                {
                    Vector2 a = r.points[i], b = r.points[i + 1]; float len = Vector2.Distance(a, b); if (len < .01f) continue;
                    var dir = (b - a) / len; var side = new Vector2(-dir.y, dir.x) * (r.width / 2);
                    for (float s = 0; s + .5f <= len; s += .5f)
                    {
                        var p = a + dir * s; var q = p + dir * .5f;
                        Assert.LessOrEqual(Mathf.Abs(H(q) - H(p)) / .5f, steep, r.name + " climbs too steeply at " + p);
                        Assert.LessOrEqual(Mathf.Abs(H(p + side) - H(p - side)) / r.width, steep, r.name + " leans too steeply across at " + p);
                    }
                }
            }
            int stairs = 0;
            foreach (var t in z.terraces.Where(t => t.ramp))
            {
                Assert.LessOrEqual(Mathf.Abs(t.heightTo - t.height) / t.size.y, steep, t.name + " is no steeper than 31 degrees.");
                Assert.IsTrue(t.style == "stair" || t.style == "ramp", t.name);
                if (t.style == "stair") stairs++;
            }
            Assert.GreaterOrEqual(stairs, 6, "Step-streets between the levels");
            Assert.IsTrue(z.terraces.Any(t => t.ramp && t.name == "The twisting causeway" && t.canonStatus.Contains("ch.3:257")), "and the books' twisting causeway down to the slums.");
        }

        [Test] public void Ramps_and_stairs_have_a_rail_over_every_drop_beside_them()
        {
            // (2026-10-18, Chris: "falling off ramps and bad pathing") Along each ramp's and stair's sides, wherever the street beside it is
            // more than a metre down, the retaining wall there stands a parapet a rail's height over the walk.
            Assert.LessOrEqual(ZoneBuilder.LowParapetDrop, 1f, "A drop of more than a metre gets a rail, as Hallow's Creek's decks.");
            int railed = 0;
            foreach (var t in z.terraces.Where(t => t.ramp))
            {
                float rr = t.rotation * Mathf.Deg2Rad, c = Mathf.Cos(rr), sn = Mathf.Sin(rr);
                Vector2 W(float lx, float lz) { return new Vector2(t.center.x + lx * c + lz * sn, t.center.y - lx * sn + lz * c); }
                foreach (int side in new[] { -1, 1 })
                    for (float lz = -t.size.y / 2 + .5f; lz < t.size.y / 2 - .5f; lz += 1)
                    {
                        float walk = H(W(side * (t.size.x / 2 - .4f), lz)), beside = H(W(side * (t.size.x / 2 + Mathf.Max(.25f, t.blend) + .4f), lz));
                        if (walk - beside <= ZoneBuilder.LowParapetDrop) continue;
                        railed++;
                        Assert.GreaterOrEqual(ZoneBuilder.LowWallTop(walk, beside) - walk, .95f, t.name + "'s side has a rail at " + W(side * t.size.x / 2, lz));
                    }
            }
            Assert.Greater(railed, 0, "Some ramp or stair stands over the street beside it.");
            Assert.Less(ZoneBuilder.LowWallTop(2, 1.5f), 2.1f, "A small step stays a kerb.");
        }

        [Test] public void Everything_stands_on_level_ground()
        {
            foreach (var c in z.camps) Assert.LessOrEqual(Range(c.center, 1.5f), .3f, "camp " + c.name);
            foreach (var n in z.nodes) Assert.LessOrEqual(Range(n.at, 1.5f), .3f, "node " + n.node + " at " + n.at);
            foreach (var s in z.secrets) Assert.LessOrEqual(Range(s.at, 1.2f), .3f, "secret " + s.id);
            foreach (var s in z.stations) Assert.LessOrEqual(Range(s.at, 1.5f), .3f, "station " + s.name);
            foreach (var p in z.life.residents.Where(p => p.at != Vector2.zero)) Assert.LessOrEqual(Range(p.at, 1), .3f, "resident " + p.name);
            foreach (var e in z.exits) Assert.LessOrEqual(Range(e.at, 2), .3f, "exit " + e.name);
            Assert.LessOrEqual(Range(z.spawns.player, 2), .3f, "the start");
            var buildings = new[] { "tenement", "warehouse", "dead_house", "holding_house", "soup_hall", "toll_house", "inn", "market_hall" };
            foreach (var p in z.props.Where(p => buildings.Contains(p.kind)))
            {
                var size = ZoneBuilder.LowtownSize(p); if (p.kind == "inn") size = p.size;
                var q = Quaternion.Euler(0, p.rotation, 0); float lo = float.MaxValue, hi = float.MinValue;
                for (int i = -1; i <= 1; i++) for (int k = -1; k <= 1; k++)
                {
                    var o = q * new Vector3(i * size.x / 2, 0, k * size.y / 2); float h = H(p.at.x + o.x, p.at.y + o.z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                }
                Assert.LessOrEqual(hi - lo, p.kind == "market_hall" || p.kind == "inn" ? .25f : 1, p.kind + " '" + p.name + "' at " + p.at + " stands over a level's edge");
            }
            Assert.Greater(z.props.Count(p => p.kind == "tenement"), 120, "Still a dense town.");
        }

        [Test] public void The_Chemical_River_is_black_with_its_eels_and_its_croc()
        {
            var river = z.water.Single(w => w.name == "The Chemical River");
            Assert.AreEqual(ZoneBuilder.ChemicalKind, river.kind, "The river is the books' Chemical River (book1 ch.15:335; ch.16:5).");
            Assert.AreEqual(ZoneBuilder.ChemicalKind, z.water.Single(w => w.name == "The Outfall run").kind, "and so is the Forge District's run into it.");
            float ToRiver(Vector2 p) { float best = float.MaxValue; for (int i = 0; i + 1 < river.points.Length; i++) { var a = river.points[i]; var ab = river.points[i + 1] - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); best = Mathf.Min(best, Vector2.Distance(p, a + ab * t)); } return best; }
            var eels = z.camps.Where(c => c.tag == "sludgeeel").ToList();
            Assert.GreaterOrEqual(eels.Count, 2, "Sludge-eels (book1 ch.15:391)");
            foreach (var c in eels)
            {
                Assert.AreEqual("eel", c.look); Assert.AreEqual("Sludge-eel", c.mob); StringAssert.Contains("ch.15:391", c.canonStatus);
                Assert.That(ToRiver(c.center), Is.InRange(5f, 10f), c.name + " lies in the river's shallows (wading, not swimming: the navmesh walks it).");
            }
            var croc = z.camps.Single(c => c.tag == "rivercroc");
            Assert.AreEqual("croc", croc.look); Assert.Greater(croc.rare, 0, "The Old Croc is a rare"); Assert.IsFalse(croc.elite);
            Assert.That(ToRiver(croc.center), Is.InRange(5f, 10f), "in the river's shallows");
            StringAssert.Contains("ch.15:391", croc.canonStatus);
            Assert.Less(System.Array.IndexOf(z.camps, croc), z.camps.Length - 1, "before the Lantern-Eater, which stays last");
            Assert.NotNull(MobCasts.For("Sludge-eel")); Assert.NotNull(MobCasts.For("The Old Croc"));
        }

        [Test] public void The_river_poisons_but_never_kills_and_hides_you_from_magic()
        {
            Assert.AreEqual(6, ChemicalRiver.Bite(1000), "1.2% of your health a second, a bite each half second");
            Assert.AreEqual(1, ChemicalRiver.Bite(10), "at least one");
            Assert.AreEqual(0, ChemicalRiver.BiteFrom(1, 1000), "It never takes the last of it");
            Assert.AreEqual(3, ChemicalRiver.BiteFrom(4, 1000));
            Assert.IsTrue(ChemicalRiver.LosesTrack(true, 0, 5), "A Weave-Eater loses you unless it can touch you");
            Assert.IsFalse(ChemicalRiver.LosesTrack(true, 0, 2));
            Assert.IsTrue(ChemicalRiver.LosesTrack(false, 12, 10), "a far-reaching caster loses you from afar");
            Assert.IsFalse(ChemicalRiver.LosesTrack(false, 12, 4), "not when you are within a few strides");
            Assert.IsFalse(ChemicalRiver.LosesTrack(false, 0, 20), "and what only sees you keeps after you");
            Assert.IsFalse(ChemicalRiver.LosesTrack(false, 3.5f, 20), "a short-reach move is no tracking spell");
        }

        [Test] public void The_air_is_the_books_colours()
        {
            Assert.IsTrue(ColorUtility.TryParseHtmlString(z.lighting.fogColor, out var fog));
            Assert.Greater(fog.r, fog.b + .1f, "A dirty yellow haze by day (book1 ch.1:145)");
            Assert.Greater(fog.g, fog.b + .05f);
            Assert.IsTrue(ColorUtility.TryParseHtmlString(z.lighting.nightFog, out var night), "The night has its own smog");
            Assert.Less(night.grayscale, .3f); Assert.GreaterOrEqual(night.r, night.b, "grey smog warmed by the fires under it, not moonlit blue (book1 ch.1:191)");
        }
    }
}
