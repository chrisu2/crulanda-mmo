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
    /// The smugglers' drowned vaults (Lowtowns pick 11, conflict 4; Docs/ZONE_LOWTOWNS.md "The drowned vaults"), read from the zone's data
    /// without building it: the three caves made as the builder makes them (Hollow, over the terraces' ground, ZoneBuilder.TerraceGround):
    /// - three ways in from three levels: the market's cellar stair (+8), the Drain Courts (-3), the river's east bank (0);
    /// - the culvert opens into the crawl and the crawl into the vaults, floor to floor, so all three are one place; the crawl runs under
    ///   the river, its rock under the river's bed, so the vaults come out on both banks (a loop);
    /// - no stretch steeper than feet climb; past each mouth's stair every passage lies under the town with its rock under the ground;
    /// - nothing of the streets stands over a passage (a camp, a secret, a node, a station, someone's post, an exit, the start would be
    ///   set down on the cave's floor), so the market and the Night Market above are as they were; what is the vaults' is on their floor;
    /// - the black canal is the Chemical River's water, the fence keeps a pier on the causeway clear of the guards, and sells; the Iron
    ///   Teeth guard it at the zone's levels; the secret is in the crawl; everything cites the books.
    /// </summary>
    public class LowtownsVaultsTests
    {
        static ZoneDefinition z;
        static Dictionary<string, Hollow> caves;
        static string Content(string file) { return Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", file); }
        [OneTimeSetUp] public void Load()
        {
            z = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Content("Zones/lowtowns.json")));
            caves = new Dictionary<string, Hollow>();
            foreach (var p in z.props.Where(p => p.kind == "cavern" && p.style == ZoneBuilder.VaultStyle))
            {
                var rows = new List<float[]>();
                for (int i = 0; i + 4 < p.plan.Length; i += 5) rows.Add(new[] { p.plan[i], p.plan[i + 1], p.plan[i + 2], p.plan[i + 3], p.plan[i + 4] });
                caves[p.name] = new Hollow(p.name, p.at, p.rotation, rows, H);
            }
        }
        static float H(float x, float y) { return ZoneBuilder.TerraceGround(z.terraces, x, y, 0); }
        static float H(Vector2 p) { return H(p.x, p.y); }
        static float Range(Vector2 p, float r)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = -1; i <= 1; i++) for (int k = -1; k <= 1; k++) { float h = H(p.x + i * r, p.y + k * r); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            return hi - lo;
        }
        static float ToPath(Vector2 q, Vector2[] pts)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < pts.Length; i++) { var a = pts[i]; var ab = pts[i + 1] - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude)); best = Mathf.Min(best, Vector2.Distance(q, a + ab * t)); }
            return best;
        }
        static ZonePath River { get { return z.water.Single(w => w.name == "The Chemical River"); } }
        static ZoneProp Prop(string name) { return z.props.Single(p => p.kind == "cavern" && p.name == name); }
        static Vector2 Flat(Vector3 v) { return new Vector2(v.x, v.z); }
        /// <summary>The highest the cave's rock (its knoll's shell) can stand over its floor at ring i: the roof's bumps and 2-5.3 m of rock
        /// over it (ZoneBuilder.Cavern's Thick), with a little to spare.</summary>
        static float RockTop(Hollow h, int i) { return h.Centre[i].y + 1.5f * h.Height[i] + 5.4f; }

        [Test] public void Three_ways_in_from_three_levels()
        {
            Assert.AreEqual(3, caves.Count, "The vaults, the crawl and the culvert.");
            var mouths = new (string cave, float level, string where)[] { (ZoneBuilder.VaultsCave, 8, "the Rust Market"), (ZoneBuilder.CrawlCave, -3, "the Drain Courts"), (ZoneBuilder.CulvertCave, 0, "the river bank") };
            foreach (var m in mouths)
            {
                var p = Prop(m.cave);
                Assert.AreEqual(2, p.variant, m.cave + ": the caves the Adit and the cellars are made of.");
                Assert.AreEqual(m.level, H(p.at), .05f, m.cave + "'s mouth is on " + m.where);
                Assert.LessOrEqual(Range(p.at, 3), .3f, m.cave + "'s mouth on level ground");
                foreach (var r in z.roads) Assert.Greater(ToPath(p.at, r.points), r.width / 2 + 3, m.cave + "'s mouth is off " + r.name);
                foreach (var o in z.props.Where(o => o.kind != "cavern" && o.kind != "town_wall" && o.kind != "cobbles"))
                    Assert.Greater(Vector2.Distance(o.at, p.at), Mathf.Max(o.size.x, o.size.y) / 2 + 3, m.cave + "'s mouth is clear of " + o.kind + " '" + o.name + "'");
                StringAssert.StartsWith(m.cave == ZoneBuilder.CulvertCave ? "GAME-ONLY" : "CANON-EXPANDED", p.canonStatus, m.cave + " is labelled");
            }
            Assert.IsTrue(z.terraces.Any(t => t.name.StartsWith("The Rust Market") && Mathf.Abs(t.center.x - Prop(ZoneBuilder.VaultsCave).at.x) < t.size.x / 2 && Mathf.Abs(t.center.y - Prop(ZoneBuilder.VaultsCave).at.y) < t.size.y / 2 + 20), "The cellar stair goes down from the market's own shelf.");
            var bank = Prop(ZoneBuilder.CulvertCave).at;
            Assert.That(ToPath(bank, River.points), Is.InRange(River.width / 2 + 2, River.width / 2 + 20f), "The culvert opens on the river's bank.");
        }

        [Test] public void The_ways_join_into_one_place_and_loop_under_the_river()
        {
            void Joins(string from, string to)
            {
                Assert.AreEqual(to, Prop(from).joins, from + " opens into " + to);
                var h = caves[from]; var o = caves[to]; var end = h.Centre[h.Centre.Count - 1];
                int j = o.Nearest(Flat(end), out float flat);
                Assert.Less(flat, o.Half[j] * .8f, from + "'s far end is inside " + to + "'s passage");
                Assert.IsTrue(o.FloorSmooth(Flat(end), out float floor));
                Assert.AreEqual(floor, end.y, .4f, from + "'s floor meets " + to + "'s (walked, not climbed)");
            }
            Joins(ZoneBuilder.CulvertCave, ZoneBuilder.CrawlCave);
            Joins(ZoneBuilder.CrawlCave, ZoneBuilder.VaultsCave);
            Assert.IsTrue(string.IsNullOrEmpty(Prop(ZoneBuilder.VaultsCave).joins), "The vaults close at their west end: the crawl comes in there.");
            // The crawl crosses under the river: rings under its water with the rock's top under its bed; its mouth on the west bank, the
            // vaults' and the culvert's on the east.
            var crawl = caves[ZoneBuilder.CrawlCave]; var river = River; int under = 0;
            for (int i = 0; i < crawl.Centre.Count; i++)
                if (ToPath(Flat(crawl.Centre[i]), river.points) < river.width / 2 + 2) { under++; Assert.Less(RockTop(crawl, i), -river.depth - .3f, "The crawl's rock stays under the river's bed at " + crawl.Centre[i]); }
            Assert.Greater(under, 10, "The crawl runs under the river.");
            float Side(Vector2 p) { int best = 0; float d = float.MaxValue; for (int i = 0; i < river.points.Length; i++) { float e = Mathf.Abs(river.points[i].y - p.y); if (e < d) { d = e; best = i; } } return p.x - river.points[best].x; }
            Assert.Less(Side(Prop(ZoneBuilder.CrawlCave).at), 0, "The crawl comes up west of the river (the Drain Courts).");
            Assert.Greater(Side(Prop(ZoneBuilder.VaultsCave).at), 0, "The stair is east of it (the market).");
            Assert.Greater(Side(Prop(ZoneBuilder.CulvertCave).at), 0, "and so is the culvert (the east bank).");
        }

        [Test] public void No_stretch_too_steep_and_all_of_it_under_the_town()
        {
            foreach (var kv in caves)
            {
                var h = kv.Value;
                for (int i = 1; i < h.Centre.Count; i++)
                {
                    float run = Vector2.Distance(Flat(h.Centre[i]), Flat(h.Centre[i - 1])); if (run < .05f) continue;
                    Assert.LessOrEqual(Mathf.Atan2(Mathf.Abs(h.Centre[i].y - h.Centre[i - 1].y), run) * Mathf.Rad2Deg, 30, kv.Key + " at " + h.Centre[i] + ": no steeper than feet and agents climb");
                }
                for (int i = 0; i < h.Centre.Count; i++)
                {
                    if (h.Along[i] < 24) continue;
                    var at = Flat(h.Centre[i]);
                    // Under the river the bed is the ground (the crawl's test above); everywhere else the terraces'.
                    if (ToPath(at, River.points) < River.width / 2 + 4) continue;
                    Assert.LessOrEqual(RockTop(h, i), H(at), kv.Key + ": past its stair the rock lies under the street at " + at + " (along " + h.Along[i] + ")");
                }
            }
        }

        [Test] public void Nothing_of_the_streets_stands_over_the_vaults_and_theirs_is_on_their_floor()
        {
            bool Over(Vector2 p, out string which) { foreach (var kv in caves) if (kv.Value.FloorAt(p, out _)) { which = kv.Key; return true; } which = null; return false; }
            foreach (var c in z.camps)
            {
                bool over = Over(c.center, out string cave);
                if (string.IsNullOrEmpty(c.cave)) Assert.IsFalse(over, "camp " + c.name + " is over " + cave);
                else Assert.AreEqual(c.cave, cave, "camp " + c.name + " stands on its cave's floor");
            }
            foreach (var s in z.secrets)
            {
                bool over = Over(s.at, out string cave);
                if (string.IsNullOrEmpty(s.cave)) Assert.IsFalse(over, "secret " + s.id + " is over " + cave);
                else Assert.AreEqual(s.cave, cave, "secret " + s.id + " is on its cave's floor");
            }
            foreach (var r in z.life.residents.Where(r => r.at != Vector2.zero))
            {
                bool over = Over(r.at, out string cave);
                if (r.under) Assert.AreEqual(ZoneBuilder.VaultsCave, cave, r.name + " keeps a post down in the vaults");
                else Assert.IsFalse(over, r.name + "'s post is over " + cave);
            }
            foreach (var n in z.nodes) Assert.IsFalse(Over(n.at, out string c1), "node " + n.node + " at " + n.at + " is over " + c1);
            foreach (var s in z.stations) Assert.IsFalse(Over(s.at, out string c2), "station " + s.name + " is over " + c2);
            foreach (var e in z.exits) Assert.IsFalse(Over(e.at, out string c3) || Over(e.arrive, out c3), "exit " + e.name + " is over " + c3);
            Assert.IsFalse(Over(z.spawns.player, out string c4), "the start is over " + c4);
            // The quests' places (a visit is counted on the floor under it when there is one).
            var quests = File.ReadAllText(Content("Quests/lowtowns.json"));
            foreach (var m in System.Text.RegularExpressions.Regex.Matches(quests, "\"at\":\\s*\\{\\s*\"x\":\\s*(-?[0-9.]+),\\s*\"y\":\\s*(-?[0-9.]+)").Cast<System.Text.RegularExpressions.Match>())
            {
                var at = new Vector2(float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
                Assert.IsFalse(Over(at, out string c5), "a quest's place at " + at + " is over " + c5);
            }
            // The market above as it was: the Night Market's row, the stalls and Dead Man's Drop are nowhere over the vaults.
            foreach (var p in z.props.Where(p => p.kind == "stall" || p.kind == "market" || p.kind == "market_hall")) Assert.IsFalse(Over(p.at, out string c6), p.kind + " '" + p.name + "' is over " + c6);
            var pit = ZoneBuilder.DropPit(z);
            foreach (var kv in caves) foreach (var c in kv.Value.Centre)
                    Assert.IsFalse(Mathf.Abs(c.x - pit.center.x) < pit.size.x / 2 + 10 && Mathf.Abs(c.z - pit.center.y) < pit.size.y / 2 + 10, kv.Key + " runs by Dead Man's Drop's shaft");
            for (float x = 96; x <= 125; x += 1) for (float y = 48; y <= 54; y += 1) Assert.IsFalse(Over(new Vector2(x, y), out _), "The Night Market's row is over the vaults");
        }

        [Test] public void The_black_canal_the_fence_and_the_guards()
        {
            var canal = z.caveWater.Single(w => w.name == ZoneBuilder.CanalName);
            Assert.AreEqual(ZoneBuilder.VaultsCave, canal.cave);
            Assert.AreEqual(ZoneBuilder.ChemicalKind, canal.kind, "The canal is the river's black water (book1 ch.6:187; ch.16:5).");
            Assert.That(canal.depth, Is.InRange(.6f, 1.44f), "Knee to chest deep: waded, not swum (the navmesh walks it).");
            Assert.IsFalse(canal.flood, "a canal down one side, the causeway along it");
            var vaults = caves[ZoneBuilder.VaultsCave];
            for (float s = canal.from; s <= canal.to; s += 2)
            {
                int i = vaults.Nearest(Flat(vaults.At(s)), out _);
                Assert.LessOrEqual(canal.aside + canal.half, vaults.Half[i] + .2f, "The canal stays inside the vault at " + s);
                Assert.GreaterOrEqual(vaults.Half[i] + canal.aside - canal.half, 2.5f, "and leaves a causeway beside it at " + s);
            }
            var fence = z.life.residents.Single(r => r.name == ZoneBuilder.FenceName);
            Assert.IsTrue(fence.under, "The fence keeps a post down in the vaults.");
            Assert.AreEqual("Fence", fence.title);
            StringAssert.StartsWith("GAME-ONLY", fence.canonStatus); StringAssert.Contains("ch.6:185", fence.canonStatus); StringAssert.Contains("ch.6:203", fence.canonStatus);
            vaults.Local(fence.at, out float along, out float aside);
            Assert.Greater(Mathf.Abs(aside - canal.aside), canal.half + .5f, "The fence stands on the causeway, not in the canal.");
            Assert.That(along, Is.InRange(canal.from + 3, canal.to - 3), "by the canal (the skiff is moored beside her)");
            Assert.GreaterOrEqual(fence.stories.Length, 3);
            var items = ItemDatabase.Parse(new[] { File.ReadAllText(Content("Items/items.json")), File.ReadAllText(Content("Items/lowtowns.json")) });
            var sells = items.Vendors.SingleOrDefault(v => v.npc == ZoneBuilder.FenceName);
            Assert.NotNull(sells, "The fence sells."); Assert.IsTrue(sells.gearForZone, "black-market gear for the zone's levels");
            foreach (var id in sells.items) Assert.NotNull(items.Get(id), "The fence sells " + id);
            var guards = z.camps.Where(c => c.cave == ZoneBuilder.VaultsCave).ToList();
            Assert.That(guards.Count, Is.InRange(1, 2), "a camp or two");
            foreach (var c in guards)
            {
                Assert.AreEqual("deserter", c.look); Assert.That(c.tag, Is.EqualTo("ironteeth").Or.EqualTo("sifter"), "The Iron Teeth, dealers of black-market aether.");
                Assert.That(c.levelMin, Is.InRange(16, 19)); Assert.That(c.levelMax, Is.InRange(c.levelMin, 19)); Assert.IsFalse(c.elite);
                StringAssert.Contains("ch.7:21", c.canonStatus);
                Assert.Greater(Vector2.Distance(c.center, fence.at), c.radius + 9, c.name + " stands off the fence's pier (nobody works with an enemy at their elbow).");
            }
            Assert.Less(System.Array.IndexOf(z.camps, guards.Last()), z.camps.Length - 1, "before the Lantern-Eater, which stays last");
            var secret = z.secrets.Single(s => s.id == "secret.lowtowns.moss-carvings");
            Assert.AreEqual(ZoneBuilder.CrawlCave, secret.cave); StringAssert.Contains("ch.9:55", secret.canonStatus);
            var place = z.landmarks.Single(l => l.name == ZoneBuilder.VaultsCave);
            StringAssert.StartsWith("CANON-EXPANDED", place.canonStatus);
            foreach (var cite in new[] { "ch.6:41", "ch.6:129", "ch.6:185" }) StringAssert.Contains(cite, place.canonStatus);
            Assert.IsTrue(vaults.FloorAt(place.at, out _), "The place is in the vaults.");
            foreach (var p in z.props.Where(p => p.style == ZoneBuilder.VaultStyle)) StringAssert.DoesNotContain("brick", (p.canonStatus ?? "").ToLowerInvariant(), "Rubble stone, never the Sludge-Tunnels' red brick (conflict 4).");
        }
    }
}
