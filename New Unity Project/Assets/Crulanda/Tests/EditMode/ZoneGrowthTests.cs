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
    /// Oakhaven grown to 560 m (playtest note 1: "the bandit camp is way too close to the village"), as its data stands:
    /// - the zone is 560 m, and what belongs to its edge stands at the edge: the exits on their roads, the arrivals from Khaven
    ///   and the Ashland Rim just inside them, the Wasting's curtain 26 m inside the east edge, Oak creek running off both sides;
    /// - nothing is placed past the edge or in the unmade;
    /// - no camp stands within 120 m of a house of the village (a house, the inn or the mill; the Concord collectors are
    ///   story enemies, not camps), and Crowsfoot Hollow's mouth and every one of its camps lie 250 to 300 m from the green;
    /// - in every zone, the camps within 120 m of a home (Moss's lodge and the other zones' homes included) are the ones
    ///   named in NearHomes, each with its reason, and no others: the rule is open outside Oakhaven's village (playtest note 1);
    /// - Oak creek runs through the village on the line it had before three points were put on its west end;
    /// - the fields the villagers work are the ones they always worked: the new fields lie past the village's reach;
    /// - the quests' points in Oakhaven are inside it, and the deer tracks still stop at the grey;
    /// - only Oakhaven thins its grass (ZoneDefinition.wildFrom), and only past the farms.
    /// The built zone is the PlayMode tests' (CaveTests, ZoneContentTests, SecretPlacementTests, NodePlacementTests).
    /// </summary>
    public class ZoneGrowthTests
    {
        static readonly string[] Files = { "oakhaven", "khaven", "peaks", "ashrim", "verdant" };
        static string Read(string folder, string file) { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/" + folder + "/" + file + ".json")); }
        static ZoneDefinition Zone(string file) { return JsonUtility.FromJson<ZoneDefinition>(Read("Zones", file)); }
        static float Cheb(Vector2 p) { return Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)); }
        static float ToPath(Vector2 p, Vector2[] pts)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector2 a = pts[i], ab = pts[i + 1] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }
        /// <summary>The village's houses: a house, the inn, the mill. Barns and ruins are not, and Moss's lodge (a barn out in the
        /// Mastwood) is a home but not of the village: Homes counts it.</summary>
        static List<ZoneProp> Houses(ZoneDefinition z) { return z.props.Where(p => p != null && (p.kind == "house" || p.kind == "inn" || p.kind == "mill")).ToList(); }
        /// <summary>Buildings of a home's kind that are the enemy's own: the Sandthrone's toll-house in the Peaks.</summary>
        static readonly string[] EnemyBuildings = { "Toll-house" };
        /// <summary>Where people live, in any zone: a house, an inn, a mill, a treehouse, a shelter, and whatever a household
        /// names as its house (Moss's lodge is a barn). As tools/wip/zonegrowth/rule120.py counts them, less the enemy's own.</summary>
        static List<ZoneProp> Homes(ZoneDefinition z)
        {
            var named = new HashSet<string>(z.life.households.Where(h => h != null && !string.IsNullOrEmpty(h.house)).Select(h => h.house));
            return z.props.Where(p => p != null && System.Array.IndexOf(EnemyBuildings, p.name) < 0
                && (p.kind == "house" || p.kind == "inn" || p.kind == "mill" || p.kind == "treehouse" || p.kind == "shelter" || (!string.IsNullOrEmpty(p.name) && named.Contains(p.name)))).ToList();
        }
        /// <summary>
        /// The camps that stand within 120 m of a home today, by zone file, each with why. Oakhaven's are all by Moss's lodge,
        /// the hunter's home out in the Mastwood, never by a house of the village. The other four zones were measured and not
        /// moved: whether to grow them or hold the rule for open villages only is Chris's choice (PLAYTEST_NOTES.md note 1).
        /// A camp that moves clear, or a new one that comes near, fails the test until this list says so.
        /// </summary>
        static readonly Dictionary<string, Dictionary<string, string>> NearHomes = new Dictionary<string, Dictionary<string, string>>
        {
            { "oakhaven", new Dictionary<string, string> {
                { "Mastwood boars", "the hunter lives among his game: the lodge stands in the boar wood by design (46 m)" },
                { "Hollow lookouts", "the lodge is 102 m from the mouth as the crow flies, the ridge's heel between (it was 92 m from the old mouth)" },
                { "Deserters' camp", "down the cave, under the ridge" }, { "Drop sentries", "down the cave, under the ridge" },
                { "Store Caves", "down the cave, under the ridge" }, { "The Quartermaster's desk", "down the cave, under the ridge" },
                { "Deep Stair watch", "down the cave, under the ridge" }, { "King's guard", "down the cave, under the ridge" },
                { "Caddock's hall", "down the cave, under the ridge" } } },
            { "khaven", new Dictionary<string, string>() },   // grown to 410 m: every camp 125 m and more from its houses (2026-10-02)
            { "peaks", new Dictionary<string, string> {
                { "Toll-gate guards", "the Sandthrone's toll-gate on the road, 70 m from Pilgrims' Rest: the guards stand at their gate" },
                { "Captain's eyrie", "the Sandthrone's own keep, 117 m from Pilgrims' Rest" } } },   // grown to 430 m; the beasts moved out (2026-10-02)
            { "ashrim", new Dictionary<string, string> {
                { "Cinderfold hollows", "91 m from the hunters' hide, where nobody lives (the Ash-Walkers are all at the enclave)" } } },   // grown to 430 m (2026-10-02)
            { "verdant", new Dictionary<string, string> {   // grown to 430 m (2026-10-02)
                { "The Briar Way", "guards the way to the Root-Mother's Deep, 95 m from the Guest-Tree: it stays at the dungeon's door" } } },
        };
        /// <summary>The cave's own camps: the deserters, their quartermaster and their king.</summary>
        static bool OfTheHollow(ZoneCamp c) { return c.tag == "deserter" || c.tag == "quartermaster" || c.tag == "banditking"; }

        [Test] public void Oakhaven_is_560_m_and_its_edge_stands_at_the_edge()
        {
            var z = Zone("oakhaven"); float half = z.size / 2;
            Assert.AreEqual(560, z.size, "Oakhaven is 560 m across.");
            Assert.AreEqual(2, z.exits.Length, "West to Khaven, south to the Ashland Rim.");
            foreach (var e in z.exits)
            {
                Assert.That(Cheb(e.at), Is.InRange(half - 12, half - 5), "The exit '" + e.name + "' stands at the edge.");
                Assert.IsTrue(z.roads.Any(r => ToPath(e.at, r.points) < r.width / 2), "The exit '" + e.name + "' stands on its road.");
                Assert.IsTrue(z.roads.Any(r => Cheb(r.points[r.points.Length - 1]) >= half - 3 && ToPath(e.at, r.points) < r.width / 2), "Its road runs off the edge past it.");
            }
            // Arriving from the neighbours: a few metres inside the exit that leads back, on the road.
            int arrivals = 0;
            foreach (var file in Files)
                foreach (var e in Zone(file).exits)
                {
                    if (e.to != z.id) continue;
                    arrivals++;
                    Assert.Less(Cheb(e.arrive), half - 6, "The arrival from " + file + " is inside the zone.");
                    Assert.IsTrue(z.exits.Any(back => Vector2.Distance(back.at, e.arrive) < 15 && Vector2.Distance(back.at, e.arrive) > back.radius + 1), "The arrival from " + file + " at " + e.arrive + " is just inside an exit, and clear of it.");
                    Assert.IsTrue(z.roads.Any(r => ToPath(e.arrive, r.points) < r.width / 2), "The arrival from " + file + " is on a road.");
                }
            Assert.AreEqual(2, arrivals, "Khaven and the Ashland Rim lead here.");
            Assert.AreEqual(half - 26, z.wasting.x, .01f, "The Wasting's curtain stands 26 m inside the east edge, as it did.");
            var creek = z.water.Single();
            Assert.AreEqual(-half, creek.points[0].x, .01f, "Oak creek comes in at the west edge"); Assert.AreEqual(half, creek.points[creek.points.Length - 1].x, .01f, "and runs out at the east.");
        }

        [Test] public void Nothing_in_Oakhaven_is_placed_past_the_edge_or_in_the_unmade()
        {
            var z = Zone("oakhaven"); float half = z.size / 2; var problems = new List<string>();
            void In(string what, Vector2 at, float reach, bool mayBeGrey = false)
            {
                if (Cheb(at) + reach > half - 4) problems.Add(what + " at " + at + " reaches the edge");
                if (!mayBeGrey && at.x + reach > z.wasting.x) problems.Add(what + " at " + at + " stands in the unmade");
            }
            foreach (var p in z.props) if (p != null && p.kind != "wall") In("prop " + p.kind + " '" + p.name + "'", p.at, 0);
            foreach (var c in z.camps) In("camp '" + c.name + "'", c.center, c.radius);
            foreach (var l in z.landmarks) In("landmark '" + l.name + "'", l.at, 0);
            foreach (var l in z.landmarks) if (l.view != Vector2.zero) In("the view of '" + l.name + "'", l.view, 8);
            foreach (var s in z.secrets) In("secret '" + s.id + "'", s.at, 0);
            foreach (var n in z.nodes) In("node " + n.node, n.at, 10);
            foreach (var g in z.groves) In("grove '" + g.name + "'", g.center, Mathf.Max(g.size.x, g.size.y) / 2, g.kind == "dead");   // the grey's own dead woods stand in its fringe
            foreach (var f in z.fields) In("field '" + f.name + "'", f.center, 0, true);   // the Hollins' east field went grey
            foreach (var c in z.clearings) In("clearing '" + c.name + "'", c.center, c.radius);
            foreach (var t in z.tallGrass) In("tall grass '" + t.name + "'", t.center, t.radius);
            foreach (var s in z.shapes) In("shape '" + s.name + "'", s.center, s.radius);
            foreach (var c in z.life.critters) In(c.kind + " group", c.center, c.radius);
            foreach (var e in z.spawns.enemies) In("story enemy '" + e.name + "'", e.at, 0);
            In("the player's start", z.spawns.player, 5); In("the recovery point", z.spawns.recovery, 5);
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test] public void The_camps_within_120_m_of_a_home_are_the_ones_named_in_every_zone()
        {
            var problems = new List<string>();
            foreach (var file in Files)
            {
                var z = Zone(file); var homes = Homes(z); var known = NearHomes[file]; var near = new HashSet<string>();
                Assert.IsNotEmpty(homes, file + " has homes to measure from.");
                if (file == "oakhaven") Assert.AreEqual(17, homes.Count, "The village's sixteen and Moss's lodge.");
                foreach (var c in z.camps)
                {
                    var h = homes.OrderBy(x => Vector2.Distance(c.center, x.at)).First(); float d = Vector2.Distance(c.center, h.at);
                    if (d >= 120) continue;
                    near.Add(c.name);
                    if (!known.ContainsKey(c.name)) problems.Add(file + ": '" + c.name + "' at " + c.center + " is " + d.ToString("0") + " m from " + h.name + " and is not named in NearHomes");
                    else if (file == "oakhaven") Assert.AreEqual("Moss's lodge", h.name, "'" + c.name + "' is named for the lodge alone.");
                }
                foreach (var name in known.Keys) if (!near.Contains(name)) problems.Add(file + ": '" + name + "' is named in NearHomes and is gone or no longer within 120 m of a home: take it off the list");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test] public void Oak_creek_runs_through_the_village_where_it_ran_before_it_was_lengthened()
        {
            // The swing is counted from the creek's old west end (ZonePath.swingFrom, swingAlong), so the three points put before
            // it leave the line through the village alone. Built as it stands, and as it was authored at 380 m: without those three
            // points or the ones past the old east end, counted from its first point. The land's height plays no part in the line.
            var grown = Zone("oakhaven"); var was = Zone("oakhaven"); var creek = was.water.Single();
            Assert.AreEqual(3, grown.water.Single().swingFrom, "Counted from the old first point,"); Assert.AreEqual(new Vector2(-190, -66), creek.points[3], "which is (-190, -66).");
            was.size = 380; creek.points = creek.points.Skip(3).Where(p => Mathf.Abs(p.x) <= 190).ToArray(); creek.swingFrom = 0; creek.swingAlong = 0;
            var now = new ZoneWater(); now.Prepare(grown, (x, y) => 0); var then = new ZoneWater(); then.Prepare(was, (x, y) => 0);
            Vector2[] a = now.Creeks.Single().pts, b = then.Creeks.Single().pts; int rows = 0; float worst = 0; Vector2 at = Vector2.zero;
            foreach (var p in a)
            {
                if (Mathf.Abs(p.x) >= 150) continue;
                rows++; float d = ToPath(p, b); if (d > worst) { worst = d; at = p; }
            }
            Assert.Greater(rows, 80, "The creek's rows through the village.");
            Assert.Less(worst, .05f, "Oak creek's line moved " + worst.ToString("0.00") + " m at " + at + ".");
            foreach (var file in Files) if (file != "oakhaven") foreach (var w in Zone(file).water) { Assert.AreEqual(0, w.swingFrom, file + ": " + w.name); Assert.AreEqual(0, w.swingAlong, file + ": " + w.name); }
        }

        [Test] public void No_camp_in_Oakhaven_stands_within_120_m_of_a_house()
        {
            // The village's own houses, with no exception. (Moss's lodge, out in the Mastwood, is the test above's.)
            var z = Zone("oakhaven"); var houses = Houses(z); var problems = new List<string>();
            Assert.AreEqual(16, houses.Count, "Fourteen houses, the Golden Cask and the mill.");
            foreach (var c in z.camps)
                foreach (var h in houses)
                    if (Vector2.Distance(c.center, h.at) < 120) problems.Add("'" + c.name + "' at " + c.center + " is " + Vector2.Distance(c.center, h.at).ToString("0") + " m from " + h.name);
            Assert.IsEmpty(problems, string.Join("\n", problems));
            // The beasts' country is a ring: every wild camp further out than the last house, none in the forest edge.
            foreach (var c in z.camps.Where(c => !OfTheHollow(c)))
                Assert.That(Cheb(c.center) + c.radius, Is.LessThan(z.size / 2 - 20), "'" + c.name + "' is clear of the forest edge.");
        }

        [Test] public void Crowsfoot_Hollow_lies_250_to_300_m_from_the_green()
        {
            var z = Zone("oakhaven"); var green = z.spawns.recovery;
            var cave = z.props.Single(p => p != null && p.kind == "cavern");
            Assert.AreEqual("Crowsfoot Hollow", cave.name);
            Assert.That(Vector2.Distance(cave.at, green), Is.InRange(250f, 300f), "The mouth, from the green.");
            var band = z.camps.Where(OfTheHollow).ToList();
            Assert.AreEqual(8, band.Count, "The lookouts, the camp, the Drop, the Store Caves and Hesk, the Deep Stair, the guard and Caddock.");
            foreach (var c in band)
            {
                Assert.Greater(Vector2.Distance(c.center, green), 225, "'" + c.name + "' is far from the green.");
                Assert.Less(Cheb(c.center) + c.radius, z.size / 2 - 12, "'" + c.name + "' is inside the zone's edge.");
                Assert.IsTrue(c.harder, "'" + c.name + "' is the dungeon's.");
            }
            // The first of them a traveller meets is at the mouth itself: nothing of theirs stands out in the fields.
            Assert.Less(Vector2.Distance(z.camps.Single(c => c.name == "Hollow lookouts").center, cave.at), 8, "The lookouts stand at the mouth.");
            // The way there: the North road runs on to the hills, and the Crowsfoot track from its end to the mouth.
            var north = z.roads.Single(r => r.name == "North road"); var track = z.roads.Single(r => r.name == "Crowsfoot track");
            Assert.Less(Vector2.Distance(north.points[north.points.Length - 1], track.points[0]), 1, "The track starts where the road ends.");
            Assert.Less(Vector2.Distance(track.points[track.points.Length - 1], cave.at), 3, "It ends at the mouth.");
            // The map's label and the tour's view moved with it.
            var label = z.landmarks.Single(l => l.name == "Crowsfoot Hollow");
            Assert.Less(Vector2.Distance(label.at, cave.at), 6); Assert.Less(Vector2.Distance(label.view, cave.at), 20);
            // Its key and its strongbox are down it, not left on the old hillside.
            foreach (var id in new[] { "secret.oakhaven.strongbox-key", "secret.oakhaven.quartermasters-strongbox" })
                Assert.Greater(Vector2.Distance(z.secrets.Single(s => s.id == id).at, green), 225, id);
        }

        [Test] public void The_villagers_fields_are_the_ones_they_always_worked()
        {
            // WorldLife.VillageReach: a field's middle within 125 m of the zone's is a place the farmers work. The growth's new
            // fields lie past it, so no villager's day changed for them.
            var z = Zone("oakhaven");
            var worked = z.fields.Where(f => f.center.magnitude <= 125).Select(f => f.name).OrderBy(n => n, System.StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(new[] { "Brook field", "Creek field", "Harrow stubble", "Last harvest", "North furrows", "Stubble field", "West furrows" }, worked);
        }

        [Test] public void Quest_points_in_Oakhaven_are_inside_it_and_the_deer_tracks_stop_at_the_grey()
        {
            var z = Zone("oakhaven"); float half = z.size / 2; int visits = 0; bool tracks = false;
            foreach (var file in new[] { "oakhaven", "khaven", "peaks", "ashrim", "verdant" })
            {
                var db = JsonUtility.FromJson<QuestFile>(Read("Quests", file));
                foreach (var q in db.quests)
                    foreach (var step in q.steps)
                        foreach (var o in step.objectives)
                        {
                            if (o.type != "visit" || (string.IsNullOrEmpty(o.zone) ? q.zone : o.zone) != z.id) continue;
                            visits++;
                            Assert.Less(Cheb(o.at), half - 10, q.id + ": the point " + o.at + " is inside the zone.");
                            Assert.Less(o.at.x, z.wasting.x - 4, q.id + ": the point " + o.at + " is short of the unmade.");
                            if (q.id == "npc.garet.tracks" && o.at.x > z.wasting.x - 40) tracks = true;
                        }
            }
            Assert.GreaterOrEqual(visits, 2, "The deer tracks and the Moonbell copse, at least.");
            Assert.IsTrue(tracks, "Garet's deer tracks are followed to within sight of the grey.");
        }

        [Test] public void Only_Oakhaven_thins_its_grass_and_only_past_the_farms()
        {
            foreach (var file in Files)
            {
                var z = Zone(file);
                if (file != "oakhaven") { Assert.AreEqual(0, z.wildFrom, file + " sows its grass as it always did."); continue; }
                Assert.Greater(z.wildFrom, 125, "Full meadow as far as the villagers go."); Assert.Less(z.wildFrom, z.size / 2);
                Assert.That(z.wildDensity, Is.InRange(.3f, .8f), "Thinner at the edge, not bare.");
            }
        }
    }
}
