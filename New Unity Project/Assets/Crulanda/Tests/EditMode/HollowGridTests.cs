using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The caves' engine for the Adit rework (Docs/ADIT_REWORK.md, stage S1): the rings filed by cells give the ring the plain scan gives;
    /// a point's place in a passage turns back into the point; a branch opening high in a hall's wall has no floor of its own while it is
    /// in the hall's air, and the hall keeps its floor; a cave joining a hall level at its middle leaves the hall its floor there; and the
    /// zone data's new fields read as written. Hollows built here, not a zone: Hollow.All is put back as it was.
    /// </summary>
    public class HollowGridTests
    {
        static Hollow Make(string name, Vector2 at, float yaw, float[][] rows, float? mouthY = null) { return new Hollow(name, at, yaw, rows, (x, z) => 0, mouthY); }
        List<Hollow> saved;
        [SetUp] public void Keep() { saved = new List<Hollow>(Hollow.All); Hollow.All.Clear(); }
        [TearDown] public void PutBack() { Hollow.All.Clear(); Hollow.All.AddRange(saved); }

        [Test] public void Nearest_by_cells_gives_the_ring_the_plain_scan_gives()
        {
            var rows = new List<float[]>();
            for (int k = 0; k < 40; k++) rows.Add(new[] { Mathf.Sin(k * .4f) * 30, k * 12f, 3 + (k % 5) * 4, 4 + (k % 3) * 3, -k * .5f });
            var h = Make("A winding cave", new Vector2(10, -200), 23, rows.ToArray());
            Assert.Greater(h.Centre.Count, 200, "long enough to be filed by cells");
            var rand = new System.Random(7);
            var points = new List<Vector2>();
            for (int n = 0; n < 2000; n++) points.Add(new Vector2(-180 + (float)rand.NextDouble() * 460, -280 + (float)rand.NextDouble() * 640));
            for (int i = 0; i < h.Centre.Count; i += 17) points.Add(new Vector2(h.Centre[i].x, h.Centre[i].z));   // on a ring's centre: a tie with nothing
            foreach (var p in points)
            {
                int brute = 0; float bestD = float.MaxValue;
                for (int i = 0; i < h.Centre.Count; i++) { float dx = p.x - h.Centre[i].x, dz = p.y - h.Centre[i].z, d = dx * dx + dz * dz; if (d < bestD) { bestD = d; brute = i; } }
                int got = h.Nearest(p, out float flat);
                Assert.AreEqual(brute, got, "the nearest ring to " + p);
                Assert.AreEqual(Mathf.Sqrt(bestD), flat, 1e-4f, "how far " + p + " is from it");
            }
        }
        [Test] public void A_point_in_a_passage_turns_back_into_its_along_and_aside()
        {
            var h = Make("A bending drift", new Vector2(0, 0), 10, new[] { new[] { 0f, -1, 3, 4, 0 }, new[] { 0f, 20, 3, 4, -2 }, new[] { 6f, 40, 3, 4, -4 }, new[] { 18f, 60, 3, 4, -6 }, new[] { 30f, 80, 3, 4, -8 }, new[] { 34f, 100, .3f, .4f, -8 } });
            for (float s = 5; s < h.Length - 5; s += 7)
                foreach (float aside in new[] { -2f, 0, 2 })
                {
                    var w = h.At(s, aside); h.Local(new Vector2(w.x, w.z), out float along, out float a);
                    Assert.AreEqual(s, along, .75f, "along, " + s + " m in");
                    Assert.AreEqual(aside, a, .4f, "aside, " + s + " m in");
                }
        }
        static float[][] Hall()
        {
            return new[] { new[] { 0f, -1, 3, 4, 0 }, new[] { 0f, 8, 3, 4, 0 }, new[] { 0f, 16, 14, 12, 0 }, new[] { 0f, 40, 14, 12, 0 }, new[] { 0f, 52, 3, 4, 0 }, new[] { 0f, 56, .3f, .4f, 0 } };
        }
        [Test] public void A_branch_high_in_a_halls_wall_has_no_floor_in_the_halls_air()
        {
            var hall = Make("Hall", Vector2.zero, 0, Hall());
            var mouth = hall.At(28, 8);   // on the hall's floor, 8 m to the right of its middle
            var branch = Make("Branch", new Vector2(mouth.x, mouth.z), 90, new[] { new[] { 0f, -1, 3, 4, 0 }, new[] { 0f, 10, 3, 4, 0 }, new[] { 0f, 25, 3, 4, 0 }, new[] { 0f, 30, .3f, .4f, 0 } }, mouth.y + 6);
            branch.Parent = hall; branch.MouthRise = 6;
            Hollow.All.Add(hall); Hollow.All.Add(branch);
            foreach (var h in Hollow.All) h.ComputeFloored();
            Assert.IsTrue(branch.MouthRaised, "its mouth is 6 m up the wall");
            Assert.IsFalse(branch.Floored(0), "its first ring is out in the hall's air");
            Assert.IsTrue(branch.Floored(branch.Centre.Count - 1), "its far end has a floor");
            var m = new Vector2(mouth.x + 1, mouth.z);
            Assert.IsFalse(branch.FloorAt(m, out _), "no branch floor out over the hall");
            Assert.IsTrue(hall.FloorAt(m, out float y), "the hall's floor under the mouth"); Assert.AreEqual(0, y, .01f);
            var deep = branch.At(20); Assert.IsTrue(branch.FloorAt(new Vector2(deep.x, deep.z), out float by), "the branch's own floor"); Assert.AreEqual(6, by, .05f);
            var floors = new List<float>(); Hollow.FloorsUnder(new Vector2(deep.x, deep.z), floors); Assert.AreEqual(1, floors.Count, "one floor there");
            for (int i = 0; i < hall.Centre.Count; i++) Assert.IsTrue(hall.Floored(i), "the hall keeps its floor (ring " + i + ")");
        }
        [Test] public void A_cave_joining_a_hall_at_its_middle_leaves_the_hall_its_floor()
        {
            var hall = Make("Hall", Vector2.zero, 0, Hall());
            // A way in from the land 40 m east that runs west and ends, level, at the hall's middle (its last rows full width, open).
            var loop = Make("Loop", new Vector2(40, 28), -90, new[] { new[] { 0f, -1, 3, 4, 0 }, new[] { 0f, 10, 3, 4, 0 }, new[] { 0f, 30, 3, 4, 0 }, new[] { 0f, 40, 3, 4, 0 } });
            loop.Join = hall; loop.JoinRise = 0;
            Hollow.All.Add(hall); Hollow.All.Add(loop);
            foreach (var h in Hollow.All) h.ComputeFloored();
            Assert.IsFalse(loop.JoinRaised, "a level join");
            for (int i = 0; i < hall.Centre.Count; i++) Assert.IsTrue(hall.Floored(i), "the hall keeps its floor (ring " + i + ")");
            Assert.IsFalse(loop.Floored(loop.Centre.Count - 1), "the loop's end is the hall's air");
            Assert.IsTrue(loop.Floored(0), "the loop's way in has its floor");
            Assert.IsTrue(Hollow.FloorGivesWay(loop, loop.Centre[loop.Centre.Count - 1] + Vector3.up * .3f), "the loop's floor gives way to the hall's");
            Assert.IsFalse(Hollow.FloorGivesWay(hall, new Vector3(0, .3f, 28)), "the hall's does not give way to the loop's");
        }
        [Test] public void The_zone_data_reads_the_rework_fields()
        {
            const string json = "{\"id\":\"zone.test\"," +
                "\"props\":[{\"kind\":\"cavern\",\"name\":\"C\",\"rise\":12,\"joins\":\"H\",\"style\":\"geode\"}]," +
                "\"camps\":[{\"name\":\"K\",\"wing\":\"w\",\"step\":2,\"after\":\"kill:Boss\",\"gate\":\"none\",\"optional\":true,\"passive\":true,\"rise\":6}]," +
                "\"secrets\":[{\"id\":\"secret.test.s\",\"cave\":\"C\",\"along\":3,\"aside\":1,\"rise\":1.4}]," +
                "\"decks\":[{\"name\":\"D\",\"cave\":\"C\",\"style\":\"rope\",\"rails\":\"both\",\"wall\":1,\"from\":1,\"to\":9,\"aside\":2,\"aside2\":-2,\"rise\":2,\"rise2\":4,\"width\":2.6,\"radius\":0,\"solid\":true}]," +
                "\"drops\":[{\"name\":\"X\",\"fromCave\":\"C\",\"toCave\":\"H\",\"from\":3,\"fromAside\":1,\"fromRise\":6,\"to\":5,\"toAside\":-1,\"toRise\":0,\"width\":2,\"falls\":true,\"scenery\":false}]," +
                "\"caveWater\":[{\"name\":\"P\",\"cave\":\"H\",\"from\":4,\"to\":4,\"aside\":10,\"half\":2,\"radius\":3,\"depth\":1.2,\"flood\":false}]," +
                "\"gates\":[{\"title\":\"G\",\"cave\":\"C\",\"along\":7,\"kind\":\"bossdoor\",\"prompt\":\"\",\"lord\":\"L\",\"opensOn\":\"Boss\",\"look\":\"boards\",\"side\":\"far\"}]," +
                "\"anchors\":[{\"name\":\"a\",\"cave\":\"C\",\"along\":2,\"aside\":1,\"rise\":12}]," +
                "\"wings\":[{\"id\":\"w\",\"name\":\"W\",\"after\":\"x,y\",\"group\":\"g\",\"exit\":\"drop\",\"home\":\"a\",\"optional\":true}]," +
                "\"events\":[{\"id\":\"wake\",\"kind\":\"hold\",\"wing\":\"wake\",\"anchor\":\"a\",\"step\":1,\"waves\":[{\"at\":15,\"camp\":\"Wake porters\"}]}]," +
                "\"crumbs\":[{\"cave\":\"C\",\"from\":1,\"to\":2,\"aside\":-1}]}";
            var z = JsonUtility.FromJson<ZoneDefinition>(json);
            var p = z.props[0]; Assert.AreEqual(12, p.rise); Assert.AreEqual("H", p.joins); Assert.AreEqual("geode", p.style);
            var c = z.camps[0]; Assert.AreEqual("w", c.wing); Assert.AreEqual(2, c.step); Assert.AreEqual("kill:Boss", c.after); Assert.AreEqual("none", c.gate); Assert.IsTrue(c.optional); Assert.IsTrue(c.passive); Assert.AreEqual(6, c.rise);
            var s = z.secrets[0]; Assert.AreEqual("C", s.cave); Assert.AreEqual(3, s.along); Assert.AreEqual(1, s.aside); Assert.AreEqual(1.4f, s.rise, 1e-5f);
            var d = z.decks[0]; Assert.AreEqual("rope", d.style); Assert.AreEqual("both", d.rails); Assert.AreEqual(1, d.wall); Assert.AreEqual(9, d.to); Assert.AreEqual(-2, d.aside2); Assert.AreEqual(4, d.rise2); Assert.AreEqual(2.6f, d.width, 1e-5f); Assert.IsTrue(d.solid);
            var x = z.drops[0]; Assert.AreEqual("H", x.toCave); Assert.AreEqual(6, x.fromRise); Assert.AreEqual(-1, x.toAside); Assert.IsTrue(x.falls); Assert.IsFalse(x.scenery);
            var w = z.caveWater[0]; Assert.AreEqual(3, w.radius); Assert.AreEqual(1.2f, w.depth, 1e-5f); Assert.AreEqual(10, w.aside); Assert.IsFalse(w.flood);
            var g = z.gates[0]; Assert.AreEqual("bossdoor", g.kind); Assert.AreEqual("Boss", g.opensOn); Assert.AreEqual("far", g.side); Assert.AreEqual("boards", g.look); Assert.AreEqual(7, g.along);
            Assert.AreEqual(12, z.anchors[0].rise);
            Assert.AreEqual("x,y", z.wings[0].after); Assert.AreEqual("drop", z.wings[0].exit); Assert.IsTrue(z.wings[0].optional);
            Assert.AreEqual(15, z.events[0].waves[0].at); Assert.AreEqual("Wake porters", z.events[0].waves[0].camp);
            Assert.AreEqual(-1, z.crumbs[0].aside);
        }
    }
}
