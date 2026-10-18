using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Lowtowns' rework, pick 3, the roofs (Docs/REWORK_LOWTOWNS_HALLOWS.md; Docs/ZONE_LOWTOWNS.md "The roofs"), read from the zone's
    /// data without building it (the ground is the terraces, ZoneBuilder.TerraceGround):
    /// - the route's terraces are there, all with their gutters level;
    /// - the walks and the bridges join into one piece; each climb starts on the lane, clear of every building, and ends on a walk; each
    ///   drop leaves a walk and lands on the lane through washing; the tower stands on a walk;
    /// - each bridge crosses open air between two terraces and is walkable (level, wide enough for an agent);
    /// - the walks stand out from the walls (outside the camera's fade box: RoofFade.Inside stays false on them);
    /// - the route is a second way along Pell Row, labelled with the books' lines.
    /// </summary>
    public class LowtownsRoofsTests
    {
        static ZoneDefinition z; static List<ZoneBuilder.RoofLeg> legs; static List<ZoneBuilder.RoofTop> tops;
        static float H(float x, float y) { return ZoneBuilder.TerraceGround(z.terraces, x, y, 0); }
        [OneTimeSetUp] public void Load()
        {
            z = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones", "lowtowns.json")));
            tops = ZoneBuilder.RoofTops(z, H); legs = ZoneBuilder.RoofRoute(z, H);
        }
        static IEnumerable<ZoneBuilder.RoofLeg> Of(string kind) { return legs.Where(l => l.kind == kind); }
        static bool OnWalk(Vector3 p, out ZoneBuilder.RoofLeg on)
        {
            foreach (var w in legs.Where(l => l.kind == "deck" || l.kind == "bridge"))
                if (w.Off(p, out float dy) <= w.width / 2 - .3f && Mathf.Abs(dy) < .1f) { on = w; return true; }
            on = null; return false;
        }
        static IEnumerable<(Vector2 at, Vector2 half, float rot)> Buildings()
        {
            foreach (var p in z.props) if (p != null && (p.kind == "tenement" || p.kind == "warehouse" || p.kind == "soup_hall" || p.kind == "dead_house" || p.kind == "holding_house" || p.kind == "market_hall" || p.kind == "toll_house"))
                    yield return (p.at, ZoneBuilder.LowtownSize(p) / 2, p.rotation);
        }
        static bool InBuilding(Vector3 q, float pad)
        {
            foreach (var (at, half, rot) in Buildings())
            {
                var l = Quaternion.Euler(0, -rot, 0) * new Vector3(q.x - at.x, 0, q.z - at.y);
                if (Mathf.Abs(l.x) <= half.x + pad && Mathf.Abs(l.z) <= half.y + pad) return true;
            }
            return false;
        }

        [Test] public void The_terraces_are_there_with_their_gutters_level()
        {
            Assert.NotNull(tops, "Every terrace of ZoneBuilder.RoofRow is a tenement in the data."); Assert.NotNull(legs);
            Assert.AreEqual(ZoneBuilder.RoofRow.Length, tops.Count);
            foreach (var t in tops) Assert.AreEqual(tops[0].root.y + tops[0].DeckTop, t.root.y + t.DeckTop, .05f, t.prop.name + " has its walk at the others' height.");
            Assert.AreEqual(4, Of("deck").Count(), "a walk on each terrace");
            Assert.AreEqual(3, Of("bridge").Count(), "two alleys and Pell Row crossed by rope bridges");
            Assert.AreEqual(2, Of("climb").Count(), "a rain-spout and a fire escape");
            Assert.AreEqual(2, Of("drop").Count(), "two drops through the washing");
            Assert.AreEqual(1, Of("tower").Count(), "a water tower");
            Assert.IsTrue(Of("climb").Any(l => l.name == ZoneBuilder.SpoutName) && Of("climb").Any(l => l.name == ZoneBuilder.EscapeName));
        }

        [Test] public void The_walks_and_bridges_join_into_one_route()
        {
            var walks = legs.Where(l => l.kind == "deck" || l.kind == "bridge").ToList();
            var group = Enumerable.Range(0, walks.Count).ToArray();
            int Find(int i) { while (group[i] != i) i = group[i] = group[group[i]]; return i; }
            for (int i = 0; i < walks.Count; i++)
                for (int k = 0; k < walks.Count; k++)
                {
                    if (i == k) continue;
                    foreach (var end in new[] { walks[i].a, walks[i].b })
                        if (walks[k].Off(end, out float dy) <= walks[k].width / 2 - .3f && Mathf.Abs(dy) < .1f) group[Find(i)] = Find(k);
                }
            Assert.AreEqual(1, Enumerable.Range(0, walks.Count).Select(Find).Distinct().Count(), "Every walk and bridge joins the rest: one route.");
            foreach (var b in Of("bridge")) Assert.IsTrue(Of("deck").Count(d => d.Off(b.a, out float y1) < .3f && Mathf.Abs(y1) < .1f) >= 1 && Of("deck").Count(d => d.Off(b.b, out float y2) < .3f && Mathf.Abs(y2) < .1f) >= 1, b.name + " lands on a walk at each end.");
        }

        [Test] public void Climbs_start_on_the_lane_and_end_on_a_walk()
        {
            foreach (var c in Of("climb"))
            {
                Assert.AreEqual(H(c.a.x, c.a.z), c.a.y, .05f, c.name + " starts on the ground.");
                Assert.IsFalse(InBuilding(c.a, .4f), c.name + " starts clear of every building.");
                Assert.IsTrue(OnWalk(c.b, out _), c.name + " ends on a walk.");
                Assert.Greater(c.b.y - c.a.y, 8, c.name + " climbs three storeys.");
                Assert.Less(Vector2.Distance(new Vector2(c.a.x, c.a.z), new Vector2(c.b.x, c.b.z)), ZoneBuilder.RoofDeckWide + 1.5f, c.name + " goes straight up the wall.");
            }
        }

        [Test] public void Drops_go_through_washing_to_the_lane_and_do_no_harm()
        {
            foreach (var d in Of("drop"))
            {
                Assert.IsTrue(OnWalk(d.a, out _), "A drop leaves from a walk at " + d.a);
                Assert.AreEqual(H(d.b.x, d.b.z), d.b.y, .05f, "and lands on the ground");
                Assert.IsFalse(InBuilding(d.b, .4f), "clear of the buildings");
                Assert.Greater(d.a.y - d.b.y, 8);
                var flat = new Vector2(d.b.x, d.b.z);
                Assert.IsTrue(d.ownLine || z.props.Any(p => p != null && p.kind == "washing_line" && Vector2.Distance(p.at, flat) < 4), "Washing hangs where it lands (book1 ch.1:225).");
                Assert.IsTrue(z.roads.Any(r => r.name == "Pell Row" && r.points.Zip(r.points.Skip(1), (p, q) => Seg(flat, p, q)).Min() < r.width / 2 + 2.5f), "It lands in Pell Row.");
            }
            Assert.AreEqual(0, EncounterSession.LaundryDropDamage, "The laundry breaks the fall.");
        }
        static float Seg(Vector2 q, Vector2 a, Vector2 b) { var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude)); return Vector2.Distance(q, a + ab * t); }

        [Test] public void Bridges_cross_open_air_and_are_walkable()
        {
            foreach (var b in Of("bridge"))
            {
                Assert.That(b.Length, Is.InRange(2.5f, 12f), b.name + " spans an alley or a lane.");
                Assert.AreEqual(b.a.y, b.b.y, .1f, b.name + " is level.");
                Assert.GreaterOrEqual(b.width, 2 * ZoneBuilder.NavAgentRadius + .7f, "wide enough for the party to walk it");
                Assert.IsFalse(InBuilding((b.a + b.b) / 2, -.2f), b.name + "'s middle is over open air.");
            }
            foreach (var d in Of("deck")) Assert.GreaterOrEqual(d.width, 2 * ZoneBuilder.NavAgentRadius + .7f);
        }

        [Test] public void The_walks_stand_outside_the_walls_and_clear_of_the_fade()
        {
            foreach (var d in Of("deck"))
            {
                var t = tops[d.roof];
                // The player's middle keeps the agent's radius off the slope's wall at the walk's inner edge; the fade's box is the walls
                // less .8 (ZoneBuilder.BuildProps): standing on the walk is never inside the building.
                float inner = Mathf.Abs(t.DeckZ) - ZoneBuilder.RoofDeckWide / 2 + ZoneBuilder.NavAgentRadius;
                Assert.Greater(inner, t.size.y / 2 + .3f - .8f, t.prop.name + ": the walk is outside the camera's fade box.");
                Assert.Greater(Mathf.Abs(t.DeckZ), t.size.y / 2, "out over the gutter");
                Assert.AreEqual(t.root.y + t.eaves + ZoneBuilder.RoofDeckLift, d.a.y, .05f, "just over the eaves");
            }
            var tower = Of("tower").Single();
            Assert.IsTrue(OnWalk(tower.a, out var w) && w.kind == "deck", "The water tower's ladder comes down to a walk.");
            Assert.Greater(tower.b.y, tower.a.y + 4, "and it stands high over the ridge (book1 ch.7:41).");
        }

        [Test] public void A_second_way_along_Pell_Row_labelled_from_the_books()
        {
            var spout = Of("climb").Single(c => c.name == ZoneBuilder.SpoutName);
            Assert.IsTrue(Of("drop").All(d => Vector2.Distance(new Vector2(d.b.x, d.b.z), new Vector2(spout.a.x, spout.a.z)) > 15), "Up at one end, down well along the lane.");
            Assert.Greater(Of("deck").Sum(d => d.Length) + Of("bridge").Sum(b => b.Length), 60, "a real run of roofs");
            StringAssert.StartsWith("CANON-EXPANDED", ZoneBuilder.RoofsCanon);
            foreach (var cite in new[] { "ch.1:225", "ch.1:261", "ch.1:297", "ch.7:35-41", "GAME-ONLY" }) StringAssert.Contains(cite, ZoneBuilder.RoofsCanon);
        }
    }
}
