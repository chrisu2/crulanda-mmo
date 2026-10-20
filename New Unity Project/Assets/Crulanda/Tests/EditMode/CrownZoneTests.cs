using System;
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
    /// The Crown of Argentis (zone.crown; Docs/CAPITAL_HUB.md, Phase 9 step 1) holds together as data: a safe city laid out after the city
    /// map, every building of the design and every person of the hooks (the halls, the bank and the Exchange key on these roles), every
    /// trade's trainer by its station, wardens in pairs, no camps, the road to Oakhaven both ways, a view for each place on the capture
    /// tour, and the lore labelled. The PlayMode side (built, walked into, reached) is CrownZonePlayTests.
    /// </summary>
    public class CrownZoneTests
    {
        static string Zones { get { return Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones"); } }
        static ZoneDefinition Zone(string file) { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Zones, file + ".json"))); }
        static ZoneDefinition crown, oakhaven;
        [OneTimeSetUp] public void Load() { crown = Zone("crown"); oakhaven = Zone("oakhaven"); }
        static ZoneResident Role(string role) { return crown.life.residents.FirstOrDefault(r => r != null && r.role == role); }
        static ZoneProp Prop(string name) { return crown.props.FirstOrDefault(p => p != null && p.name == name); }
        /// <summary>Whether a point lies within a prop's walls (its size round its middle, turned with it).</summary>
        static bool Inside(ZoneProp p, Vector2 at)
        {
            var size = ZoneBuilder.CitySize(p); if (size.x <= 0) return false;
            float r = p.rotation * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r); var d = at - p.at;
            return Mathf.Abs(d.x * c - d.y * s) < size.x / 2 - .3f && Mathf.Abs(d.x * s + d.y * c) < size.y / 2 - .3f;
        }
        static readonly string[] Labels = { "CANON", "CANON-EXPANDED", "GAME-ONLY", "PROVISIONAL" };
        static bool Labelled(string s) { return !string.IsNullOrEmpty(s) && Labels.Any(l => s.StartsWith(l, StringComparison.Ordinal)); }

        [Test] public void The_Crown_is_a_safe_city_on_the_world_map()
        {
            Assert.AreEqual("zone.crown", crown.id); Assert.AreEqual("The Crown of Argentis", crown.displayName);
            StringAssert.StartsWith("CANON", crown.canonStatus, "The place is CANON (world_bible.md, the city map).");
            Assert.IsTrue(crown.city, "A city."); Assert.AreEqual(1, crown.levelMin, "Everyone, from level 1 (Chris, 2026-10-15).");
            Assert.AreEqual("(all)", HudMaps.Band(crown), "The maps show its levels as all.");
            Assert.That(crown.size, Is.InRange(300f, 340f), "About 320 m.");
            Assert.IsEmpty(crown.camps, "No hostile camps."); Assert.IsEmpty(crown.spawns.enemies, "No enemies.");
            Assert.IsEmpty(crown.nodes, "No gathering nodes: the trainers teach; the wild zones give.");
            Assert.IsFalse(crown.dungeon || crown.groupZone);
            Assert.That(crown.worldMapPosition.x, Is.InRange(.4f, .6f)); Assert.That(crown.worldMapPosition.y, Is.InRange(.25f, .5f), "Argentis at the centre of the game map, north of Oakhaven.");
            Assert.Less(crown.worldMapPosition.x, oakhaven.worldMapPosition.x); Assert.Less(crown.worldMapPosition.y, oakhaven.worldMapPosition.y, "Up and to the west of Oakhaven: the road runs north.");
            Assert.GreaterOrEqual(crown.terraces.Length, 4, "The plateau, its ramp, the Crown terrace and the Grand Stair.");
            var high = crown.terraces.First(t => t.name == "The High City"); var upper = crown.terraces.First(t => t.name == "The Crown terrace");
            Assert.Greater(upper.height, high.height, "The Crown Square stands above the lower city.");
            Assert.IsTrue(crown.terraces.Where(t => t.ramp).All(t => Mathf.Abs(t.heightTo - t.height) / t.size.y < .35f), "Every ramp walkable (under about 19 degrees).");
        }

        [Test] public void Every_building_of_the_design_is_there_and_laid_out_after_the_map()
        {
            foreach (var (name, kind) in new[] { ("The Council Keep", "council_keep"), ("The Council Hall", "dome_hall"), ("The Archive", "archive"), ("The Exchange", "mansion"),
                ("The Treasury Vault", "mansion"), ("The Hall of Arms", "mansion"), ("The Armoury", "mansion"), ("The Gilded Lantern", "inn"), ("The Gilt Gate", "city_gate"),
                ("The Low Stair", "city_gate"), ("The Spire", "spire"), ("The Lowtowns", "lowtowns"), ("The Grand Stair", "stair") })
            {
                var p = Prop(name); Assert.IsNotNull(p, name + " is built."); Assert.AreEqual(kind, p.kind, name);
            }
            foreach (var g in SimGuilds.All)
                Assert.IsTrue(crown.props.Any(p => p != null && p.kind == "guild_hall" && p.name.EndsWith(g.name.Replace("The ", ""))), "A hall for " + g.name + " on Guild Row.");
            Assert.AreEqual(4, crown.props.Count(p => p != null && p.kind == "guild_hall"));
            Assert.AreEqual(1, crown.props.Count(p => p != null && p.kind == "city_gate" && p.variant == 1), "One gate shut: the Low Stair.");
            Assert.AreEqual(1, Prop("The Low Stair").variant, "The Low Stair is shut until the Lowtowns open.");
            Assert.IsTrue(crown.props.Any(p => p != null && p.kind == "signpost" && p.name.Contains("15-20")), "Its sign says what is below: dangerous, 15-20.");
            Assert.IsNotNull(Prop("The Gilt Gate")); Assert.AreEqual(0, Prop("The Gilt Gate").variant, "The Gilt Gate stands open.");
            Assert.GreaterOrEqual(crown.props.Count(p => p != null && p.kind == "city_wall" && p.variant == 0), 2, "The curtain wall.");
            // The map: the keep at the north-centre, the domed hall east of it, the many-spired Archive west of it, the Spire below the south wall's middle.
            Vector2 keep = Prop("The Council Keep").at, dome = Prop("The Council Hall").at, archive = Prop("The Archive").at, spire = Prop("The Spire").at;
            Assert.Less(Mathf.Abs(keep.x), 10); Assert.Greater(keep.y, crown.size * .25f, "The keep at the north-centre.");
            Assert.Greater(dome.x, keep.x + 20); Assert.Less(Mathf.Abs(dome.y - keep.y), 15, "The domed hall east of it.");
            Assert.Less(archive.x, keep.x - 20); Assert.Less(Mathf.Abs(archive.y - keep.y), 15, "The Archive west of it.");
            float south = crown.props.Where(p => p != null && p.kind == "city_wall" && p.variant == 0).SelectMany(p => p.points).Min(q => q.y);
            Assert.Less(spire.y, south - 20, "The Spire outside the south wall."); Assert.Less(Mathf.Abs(spire.x), 15, "Below its middle.");
            Assert.AreEqual(1, crown.props.Count(p => p != null && p.kind == "council_keep"), "One castle: the central point.");
            Assert.IsTrue(crown.props.Any(p => p != null && p.kind == "fountain") && crown.props.Any(p => p != null && p.kind == "colonnade") && crown.props.Any(p => p != null && p.kind == "parterre"), "Fountains, colonnades, gardens.");
        }

        [Test] public void Every_hooked_role_stands_in_its_place()
        {
            var aethelgard = Role("councilor"); Assert.IsNotNull(aethelgard); Assert.AreEqual("High Councilor Aethelgard", aethelgard.name);
            StringAssert.StartsWith("CANON", aethelgard.canonStatus, "Aethelgard is CANON (world_bible.md)."); Assert.IsFalse(aethelgard.works, "He keeps his post.");
            Assert.IsTrue(Inside(Prop("The Council Keep"), aethelgard.at), "In the keep's audience hall.");
            var clerk = Role("council-clerk"); Assert.IsNotNull(clerk); Assert.AreEqual("Clerk Odo Vantry", clerk.name);
            Assert.Less(Vector2.Distance(clerk.at, aethelgard.at), 4, "Beside him.");
            foreach (var (role, building) in new[] { ("archivist", "The Archive"), ("banker", "The Treasury Vault"), ("auctioneer", "The Exchange"), ("factor", "The Exchange"),
                ("dresser", "The Armoury"), ("herald", "The Council Hall") })
            {
                var r = Role(role); Assert.IsNotNull(r, "Someone is the " + role + ".");
                Assert.IsTrue(Inside(Prop(building), r.at), r.name + " (" + role + ") stands in " + building + ".");
            }
            StringAssert.StartsWith("CANON-EXPANDED", Role("factor").canonStatus, "Port Caelum's factor is CANON-EXPANDED.");
            foreach (var g in new[] { "lantern-watch", "oak-and-ember", "long-road", "scree-hounds" })
            {
                var m = Role("guildmaster:" + g); Assert.IsNotNull(m, "The master of " + g + ".");
                var spot = Prop("hall." + g); Assert.IsNotNull(spot, "The place in its hall where its members stand."); Assert.AreEqual("spot", spot.kind);
                var hall = crown.props.Where(p => p != null && p.kind == "guild_hall").OrderBy(p => Vector2.Distance(p.at, m.at)).First();
                Assert.IsTrue(Inside(hall, m.at) && Inside(hall, spot.at), g + "'s master and members in " + hall.name + ".");
            }
            foreach (var c in new[] { "warrior", "druid", "paladin", "ranger", "mage", "rogue", "archivist", "saltmender", "cleric", "scoundrel", "shaman", "pactblade" })
            {
                var t = Role("trainer:class." + c); Assert.IsNotNull(t, "A trainer for class." + c + ".");
                Assert.IsTrue(Inside(Prop("The Hall of Arms"), t.at), t.name + " in the Hall of Arms.");
            }
            var inn = Role("innkeeper"); Assert.IsNotNull(inn); Assert.IsTrue(Inside(Prop("The Gilded Lantern"), inn.at), "The innkeeper in the Gilded Lantern.");
            Assert.IsNotNull(Role("merchant"), "A vendor.");
            foreach (var r in crown.life.residents)
            {
                Assert.IsTrue(Labelled(r.canonStatus), r.name + " is labelled.");
                Assert.That(r.stories.Length, Is.InRange(2, 3), r.name + " has two or three stories.");
                Assert.IsFalse(r.works && r.at != Vector2.zero, r.name + ": a post or a working day, not both.");
            }
            Assert.AreEqual(crown.life.residents.Length, crown.life.residents.Select(r => r.name).Distinct().Count(), "Every name once.");
        }

        [Test] public void Every_trade_has_its_trainer_by_its_station_from_level_one()
        {
            var items = ProfessionDataTests.Items(); var db = ProfessionDataTests.Db(items);
            var own = crown.stations.Where(s => s != null).ToList();
            var kinds = new HashSet<string>(own.Select(s => s.kind)); if (crown.props.Any(p => p != null && p.kind == "inn")) kinds.Add("fire");   // an inn's hearth is a fire
            Assert.IsTrue(own.All(s => s.canonStatus != null && s.canonStatus.StartsWith("GAME-ONLY")), "The stations are GAME-ONLY.");
            foreach (var d in db.Order)
            {
                var t = Role("profession-trainer:" + d.id); Assert.IsNotNull(t, d.name + " has its trainer in the Crown.");
                var court = Prop("The Crafters' Court colonnade, east").at;
                Assert.Less(Vector2.Distance(t.at, court), 40, t.name + " stands in the Crafters' Court.");
                foreach (var need in ProfessionDatabase.Stations(d.station).Take(1))
                {
                    Assert.IsTrue(ProfessionDatabase.Stations(d.station).Any(kinds.Contains), d.name + ": a " + d.station + " stands in the Crown.");
                    var near = own.Where(s => ProfessionDatabase.Stations(d.station).Contains(s.kind)).OrderBy(s => Vector2.Distance(s.at, t.at)).FirstOrDefault();
                    Assert.IsNotNull(near, d.name + "'s station is the Court's own.");
                    Assert.Less(Vector2.Distance(near.at, t.at), EncounterSession.TrainerRange, t.name + " stands by " + near.name + ", near enough to start you off.");
                }
                if (!string.IsNullOrEmpty(d.tool))
                    CollectionAssert.Contains(items.StockFor(t.name, t.role, crown.levelMax), d.tool, t.name + " sells the " + d.tool + " that teaches " + d.name + ".");
            }
            foreach (var id in new[] { "leatherworking", "weaving" }) Assert.IsNotNull(Role("profession-trainer:" + id), "The " + id + " trainer stands ready for the trades another branch adds.");
            foreach (var r in db.Recipes.Where(r => r.skill <= 1)) Assert.IsTrue(ProfessionDatabase.Stations(r.station).Any(kinds.Contains), r.id + ": a first recipe can be made in the Crown.");
        }

        [Test] public void The_Council_wardens_stand_in_pairs()
        {
            var wardens = crown.life.residents.Where(r => r.role == "warden").ToList();
            Assert.GreaterOrEqual(wardens.Count, 8, "On the gates and on the streets.");
            foreach (var w in wardens)
            {
                Assert.AreEqual("concord", w.look, w.name + " wears the Concord's warden look.");
                Assert.IsTrue(wardens.Any(o => o != w && Vector2.Distance(o.at, w.at) < 10), w.name + " has a partner.");
            }
            foreach (var gate in new[] { "The Gilt Gate", "The Low Stair" })
                Assert.GreaterOrEqual(wardens.Count(w => Vector2.Distance(w.at, Prop(gate).at) < 14), 2, "Two wardens on " + gate + ".");
        }

        [Test] public void The_road_runs_both_ways_between_Oakhaven_and_the_Crown()
        {
            var down = crown.exits.Single(e => e.to == "zone.oakhaven"); var up = oakhaven.exits.Single(e => e.to == "zone.crown");
            Assert.AreEqual("North road to Argentis (the Crown)", up.name);
            Assert.Greater(up.at.y, oakhaven.size / 2 - 15, "On Oakhaven's north edge.");
            Assert.Less(Vector2.Distance(down.arrive, up.at), 15, "Back down you arrive by the North road's end."); Assert.Greater(Vector2.Distance(down.arrive, up.at), up.radius + 1, "and not inside it.");
            Assert.Less(Vector2.Distance(up.arrive, down.at), 20, "Up you arrive by the road down."); Assert.Greater(Vector2.Distance(up.arrive, down.at), down.radius + 1, "and not inside it.");
            Assert.Less(Vector2.Distance(up.arrive, crown.spawns.player), 3, "The Crown starts you at the Gilt Approach.");
            Assert.IsTrue(oakhaven.roads.Any(r => r.points.Length > 1 && Vector2.Distance(r.points[r.points.Length - 1], up.at) < 12), "Oakhaven's road is drawn to the exit.");
            Assert.IsTrue(crown.roads.Any(r => r.points.Any(p => Vector2.Distance(p, down.at) < 8)), "The Crown's road runs out to its exit.");
            foreach (var (p, z) in new[] { (down.at, crown), (up.arrive, crown), (up.at, oakhaven), (down.arrive, oakhaven) }) Assert.Less(Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)), z.size / 2, p + " inside " + z.displayName + ".");
        }

        [Test] public void Every_place_has_a_view_for_the_capture_tour_and_a_label()
        {
            foreach (var l in crown.landmarks)
            {
                Assert.AreNotEqual(Vector2.zero, l.view, l.name + " has an authored view.");
                Assert.IsTrue(Labelled(l.canonStatus), l.name + " is labelled.");
            }
            var whole = crown.landmarks.Single(l => l.name == "The Crown of Argentis");
            Assert.Greater(whole.view.x, whole.at.x + 60); Assert.Less(whole.view.y, whole.at.y - 40, "The whole city framed from the south-east, as the city map draws it.");
            Assert.IsTrue(crown.landmarks.Any(l => l.name == "The Audience Hall" && Inside(Prop("The Council Keep"), l.view)), "A frame inside the audience hall (Aethelgard).");
            foreach (var t in crown.terraces) Assert.IsTrue(Labelled(t.canonStatus), t.name + " is labelled.");
            foreach (var s in crown.secrets) Assert.IsTrue(Labelled(s.canonStatus), s.id + " is labelled.");
        }

        // ---------- the city inn and the city's people (p9-cityinn; Chris, 2026-10-16: "dont need the same exact inn in the castle. needs a
        // redesign", "will need lots more npcs in the castle with real jobs like in oakhaven") ----------
        /// <summary>A point of a prop's own frame (x across, z its front to back) in the world.</summary>
        static Vector2 World(ZoneProp p, Vector2 local)
        {
            float r = p.rotation * Mathf.Deg2Rad; return p.at + new Vector2(Mathf.Cos(r), -Mathf.Sin(r)) * local.x + new Vector2(Mathf.Sin(r), Mathf.Cos(r)) * local.y;
        }
        static float Outside(Vector2 p, Vector2 at, Vector2 half) { return new Vector2(Mathf.Max(0, Mathf.Abs(p.x - at.x) - half.x), Mathf.Max(0, Mathf.Abs(p.y - at.y) - half.y)).magnitude; }

        [Test] public void The_Gilded_Lantern_is_a_city_inn_laid_out_inside_its_walls()
        {
            var inn = Prop("The Gilded Lantern"); Assert.AreEqual("inn", inn.kind); Assert.AreEqual(3, inn.variant, "The city inn (variant 3), not the village's timber inn.");
            Assert.GreaterOrEqual(inn.size.x, 20); Assert.GreaterOrEqual(inn.size.y, 14, "A city inn: two storeys round a galleried taproom, 22 x 16.");
            var plan = ZoneBuilder.InnPlan.For(inn.size, inn.variant); var inner = plan.Inner; var door = plan.DoorZone; const float body = .3f;
            Assert.IsTrue(plan.city && !plan.waystation);
            Assert.AreEqual(ZoneBuilder.CityDoorW, plan.doorW, 1e-4f, "The grand double door."); Assert.Greater(plan.NavGap, 2, "Two pass through it with room to spare.");
            Assert.Greater(plan.galleryY, 3, "A gallery a storey up."); Assert.GreaterOrEqual(plan.columns.Count, 4, "On columns.");
            Assert.Greater(plan.stairSize.x, 4.5f, "A stair up to it"); Assert.Less(Mathf.Atan2(plan.galleryY, plan.stairSize.x) * Mathf.Rad2Deg, 40, "that can be walked (under 40 degrees).");
            Assert.GreaterOrEqual(plan.seats.Count, 28, "Booths and round tables."); Assert.AreEqual(12, plan.seats.Count(s => s.at.x < -inner.x + 2), "Three booths of four along the -x end.");
            Assert.GreaterOrEqual(plan.stools.Count, 6, "A long bar with its stools."); Assert.Greater(plan.barSize.x, 8, "The long polished bar.");
            Assert.AreEqual(2, plan.benchSeats.Count, "The bench before the hearth.");
            Assert.GreaterOrEqual(plan.serve.Count, 2, "Places for the servers to wait.");
            var problems = new List<string>(); var furniture = plan.Furniture();
            for (int i = 0; i < furniture.Count; i++)
            {
                var (what, at, half) = furniture[i];
                if (Mathf.Abs(at.x) + half.x > inner.x + .01f || Mathf.Abs(at.y) + half.y > inner.y + .01f) problems.Add(what + " goes through the walls");
                if (Rect.MinMaxRect(at.x - half.x, at.y - half.y, at.x + half.x, at.y + half.y).Overlaps(door)) problems.Add(what + " stands in the doorway");
                for (int j = i + 1; j < furniture.Count; j++) { var (o, oat, oh) = furniture[j]; if (Mathf.Abs(at.x - oat.x) < half.x + oh.x && Mathf.Abs(at.y - oat.y) < half.y + oh.y) problems.Add(what + " and " + o + " overlap"); }
            }
            var seats = plan.seats.Concat(plan.stools).Concat(plan.benchSeats).ToList();
            for (int i = 0; i < seats.Count; i++)
            {
                var (at, face) = seats[i]; string s = "the seat at " + at + " ";
                if (Mathf.Abs(at.x) > inner.x - body - .05f || Mathf.Abs(at.y) > inner.y - body - .05f) problems.Add(s + "is in the wall");
                if (door.Contains(at)) problems.Add(s + "is in the doorway");
                foreach (var (what, fat, half) in furniture)
                {
                    // A booth's seat faces across its own table, not at its middle.
                    bool own = Vector2.Distance(face, fat) < .05f || Outside(face, fat, half) <= 0 || what == "the bar" && Mathf.Abs(face.y - fat.y) < .05f; float clear = Outside(at, fat, half);
                    if (clear <= 0) problems.Add(s + "is in " + what); else if (!own && clear < body) problems.Add(s + "is " + clear.ToString("0.00") + " m from " + what);
                }
                for (int j = i + 1; j < seats.Count; j++) if (Vector2.Distance(at, seats[j].at) < 2 * body - .02f) problems.Add(s + "is in the lap of the seat at " + seats[j].at);
            }
            foreach (var (what, at) in new[] { ("the innkeeper's place", plan.stand), ("the cook's place", plan.cook), ("the hatch", plan.hatch) }.Concat(plan.serve.Select(s => ("a server's place", s))).Concat(plan.kitchen.Select(k => ("a kitchen place", k))))
            {
                if (Mathf.Abs(at.x) > inner.x - body || Mathf.Abs(at.y) > inner.y - body) problems.Add(what + " is in the wall");
                foreach (var (f, fat, half) in furniture) if (Outside(at, fat, half) < .5f) problems.Add(what + " is in " + f);
                if (door.Contains(at)) problems.Add(what + " is in the doorway");
            }
            Assert.Greater(plan.stand.x, plan.bar.x + plan.barSize.x / 2, "The innkeeper stands past the bar's end.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
            // The innkeeper keeps the bar where the plan has him; the inn clear of every building and road round it.
            Assert.Less(Vector2.Distance(Role("innkeeper").at, World(inn, plan.stand)), .3f, "Corin Bellweather at the bar's open end.");
            foreach (var o in crown.props.Where(o => o != null && o != inn && Vector2.Distance(o.at, inn.at) < 40))
            {
                var size = ZoneBuilder.CitySize(o); if (size.x <= 0) continue;
                Assert.Greater(Mathf.Max(Mathf.Abs(o.at.x - inn.at.x) - (size.x + inn.size.x) / 2, Mathf.Abs(o.at.y - inn.at.y) - (size.y + inn.size.y) / 2), 1, "The Lantern clear of " + o.name + ".");
            }
            foreach (var r in crown.roads) for (float u = -1; u <= 1.01f; u += .1f)
                foreach (var local in new[] { new Vector2(u * inn.size.x / 2, -inn.size.y / 2 - 2.6f), new Vector2(u * inn.size.x / 2, inn.size.y / 2) })
                {
                    var w = World(inn, local); float d = float.MaxValue;
                    for (int i = 0; i + 1 < r.points.Length; i++) { Vector2 a = r.points[i], ab = r.points[i + 1] - a; d = Mathf.Min(d, Vector2.Distance(w, a + ab * Mathf.Clamp01(Vector2.Dot(w - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude)))); }
                    Assert.Greater(d, r.width / 2, "The Lantern and its portico out of " + r.name + ".");
                }
        }

        /// <summary>
        /// No pocket in the Lantern (CrownLifeTests, 2026-10-16: a server "stood stuck at (40.17, 9.08, 36.72)", the cook's place, three
        /// times a day): the kitchen's range stood in the back corner 1.45 m from the gallery's corner column, so the navmesh between
        /// them (each solid padded by NavPad, the walkable mesh the agent's radius short of that) was a strip 0.25 m wide, and whoever was
        /// sent there could not get out; moved beside the hearth, its one place sat in the neck between the hearth, the range and the +x
        /// gallery's column, and drew the cook, the servers and the pot-boy round one point. The kitchen is now under the back gallery
        /// with three places a metre apart. Over the plan's solids (the walls, the bar, the hearth, the range, the columns, the stair and its
        /// rail: the tables, settles, stools and bench are walked between), a grid flood from inside the door through every point whose
        /// middle keeps NavPad + the agent's radius + half a way of 0.9 m from every solid (a way 0.9 m wide on the navmesh, 2.1 m of clear
        /// floor between solids: a walker and room to pass); every seat, stool, bench seat, server's place, the innkeeper's, the
        /// cook's and the hatch lie within a step of that way (a 1.2 m path to the door, the body and the way's half).
        /// </summary>
        [Test] public void Every_place_in_the_Gilded_Lantern_has_a_wide_way_to_the_door()
        {
            var inn = Prop("The Gilded Lantern"); var plan = ZoneBuilder.InnPlan.For(inn.size, inn.variant); var inner = plan.Inner;
            const float way = .9f, step = .05f; float keep = ZoneBuilder.NavPad + ZoneBuilder.NavAgentRadius + way / 2, reach = way / 2 + .15f;
            var solids = plan.Furniture().Where(f => !f.what.StartsWith("table")).ToList();
            int si = solids.FindIndex(f => f.what == "the stair"); Assert.GreaterOrEqual(si, 0, "The stair is solid.");
            solids[si] = ("the stair and its rail", solids[si].at + new Vector2(0, .04f), solids[si].half + new Vector2(0, .04f));   // the rail, .16 thick on its open side
            float Clear(Vector2 p)
            {
                float c = Mathf.Min(inner.x - Mathf.Abs(p.x), inner.y - Mathf.Abs(p.y));
                foreach (var (_, at, half) in solids) c = Mathf.Min(c, Outside(p, at, half));
                return c;
            }
            int nx = Mathf.CeilToInt(2 * inner.x / step), nz = Mathf.CeilToInt(2 * inner.y / step);
            Vector2 Cell(int i, int j) { return new Vector2(-inner.x + (i + .5f) * step, -inner.y + (j + .5f) * step); }
            var wide = new bool[nx, nz]; for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++) wide[i, j] = Clear(Cell(i, j)) >= keep;
            var reached = new bool[nx, nz]; var queue = new Queue<(int, int)>();
            var seed = (nx / 2, Mathf.FloorToInt((keep + .1f) / step)); Assert.IsTrue(wide[seed.Item1, seed.Item2], "The way starts inside the door.");
            reached[seed.Item1, seed.Item2] = true; queue.Enqueue(seed);
            while (queue.Count > 0)
            {
                var (i, j) = queue.Dequeue();
                foreach (var (a, b) in new[] { (i + 1, j), (i - 1, j), (i, j + 1), (i, j - 1) })
                    if (a >= 0 && b >= 0 && a < nx && b < nz && wide[a, b] && !reached[a, b]) { reached[a, b] = true; queue.Enqueue((a, b)); }
            }
            float ToWay(Vector2 p)
            {
                float best = float.MaxValue; int r = Mathf.CeilToInt(2 / step), ci = Mathf.FloorToInt((p.x + inner.x) / step), cj = Mathf.FloorToInt((p.y + inner.y) / step);
                for (int i = Mathf.Max(0, ci - r); i < Mathf.Min(nx, ci + r); i++) for (int j = Mathf.Max(0, cj - r); j < Mathf.Min(nz, cj + r); j++)
                    if (reached[i, j]) best = Mathf.Min(best, Vector2.Distance(p, Cell(i, j)));
                return best;
            }
            var places = plan.seats.Select(s => ("a seat", s.at)).Concat(plan.stools.Select(s => ("a stool", s.at))).Concat(plan.benchSeats.Select(s => ("a bench seat", s.at)))
                .Concat(plan.serve.Select(s => ("a server's place", s))).Concat(new[] { ("the innkeeper's place", plan.stand), ("the cook's place", plan.cook), ("the hatch", plan.hatch) }).Concat(plan.kitchen.Select(k => ("a kitchen place", k)));
            var problems = new List<string>();
            // More than one place in the kitchen, a metre apart: the cook, the servers and the pot-boy are all sent there.
            Assert.GreaterOrEqual(plan.kitchen.Count, 3, "Places in the kitchen for the cook, a server and the pot-boy."); Assert.AreEqual(plan.cook, plan.kitchen[0]);
            for (int i = 0; i < plan.kitchen.Count; i++) for (int j = i + 1; j < plan.kitchen.Count; j++)
                if (Vector2.Distance(plan.kitchen[i], plan.kitchen[j]) < 1) problems.Add("the kitchen's places at " + plan.kitchen[i] + " and " + plan.kitchen[j] + " are under a metre apart");
            foreach (var (what, at) in places) { float d = ToWay(at); if (d > reach) problems.Add(what + " at " + at + " is " + (d > 2 ? "over 2" : d.ToString("0.00")) + " m from a way " + way + " m wide to the door"); }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        static IEnumerable<ZoneResident> Workers { get { return crown.life.residents.Where(r => r != null && r.works && VillageWork.CityRoles.Contains(r.role)); } }

        [Test] public void At_least_thirty_of_the_Crowns_people_have_real_jobs_and_homes()
        {
            var workers = Workers.ToList();
            Assert.GreaterOrEqual(workers.Count, 30, "Lots more people with real jobs, as in Oakhaven.");
            foreach (var role in new[] { "clerk", "scribe", "page", "lamplighter", "gardener", "sweeper", "watercarrier", "cook", "server", "potboy", "porter", "vaultguard", "nightguard", "steward", "trader", "patron", "noble", "servant" })
                Assert.IsTrue(workers.Any(w => w.role == role), "Someone is a " + role + ".");
            var problems = new List<string>();
            foreach (var w in workers)
            {
                if (VillageWork.DayFor(w.role) == null) problems.Add(w.name + " (" + w.role + ") has no working day");
                if (w.at != Vector2.zero) problems.Add(w.name + " keeps a post, not a working day");
                if (string.IsNullOrEmpty(w.title)) problems.Add(w.name + " has no title");
                if (!Labelled(w.canonStatus)) problems.Add(w.name + " is not labelled");
                if (w.name == "Aethelgard" || w.name.Contains("Lysara") || w.name.Contains("Kaelen")) problems.Add(w.name + " is from the books");
                var home = crown.life.households.FirstOrDefault(h => h.members.Any(m => m.name == w.name));
                if (home == null) { problems.Add(w.name + " has no household"); continue; }
                var house = Prop(home.house);
                if (house == null || (house.kind != "gilt_house" && house.kind != "inn")) problems.Add(w.name + "'s home '" + home.house + "' is not a gilt house or the inn");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
            Assert.AreEqual(crown.life.households.Length, crown.life.households.Select(h => h.house).Distinct().Count(), "A household a house.");
            // The carriers carry visible loads, as Oakhaven's do: papers, crates, buckets, the besom, the lamplighter's pole.
            var loads = workers.Select(w => w.role).Distinct().SelectMany(r => VillageWork.DayFor(r).errands).Select(e => e.load).Distinct().ToList();
            foreach (var l in new[] { Crulanda.Encounter.Load.Papers, Crulanda.Encounter.Load.Goods, Crulanda.Encounter.Load.Bucket, Crulanda.Encounter.Load.Broom, Crulanda.Encounter.Load.Pole }) CollectionAssert.Contains(loads, l, "Someone carries " + l + ".");
            // Night: most go home (to the gilt houses) or to the inn; the night guard keeps the vault, the lamplighter lights the lamps at dusk.
            Assert.IsNotNull(VillageWork.ShiftFor("nightguard", 23)); CollectionAssert.Contains(VillageWork.ShiftFor("nightguard", 23).places, "treasury");
            Assert.IsNotNull(VillageWork.HoursOf("nightguard")); Assert.That(VillageWork.HoursOf("nightguard").Value.x, Is.InRange(6f, 12f), "The night guard sleeps by day.");
            CollectionAssert.Contains(VillageWork.ShiftFor("lamplighter", 19.5f).places, "lamp", "The lamplighter at the lamps at dusk.");
            foreach (var role in new[] { "clerk", "scribe", "gardener", "sweeper", "porter", "trader", "steward" })
                Assert.IsTrue(VillageWork.ShiftFor(role, 19).places.All(p => p == "inn" || p == "home" || p == "green" || p == "guildhall"), role + " is done with work in the evening.");
        }

        /// <summary>The prop kinds (and other parts of the zone's data) a city's place of each kind comes from (ZoneBuilder.Crown's
        /// Workplaces, VillageLife.FindPlaces).</summary>
        static bool Supplied(string place)
        {
            bool Has(Func<ZoneProp, bool> f) { return crown.props.Any(p => p != null && f(p)); }
            switch (place)
            {
                case "keep": return Has(p => p.kind == "council_keep");
                case "councilhall": return Has(p => p.kind == "dome_hall");
                case "archive": return Has(p => p.kind == "archive");
                case "exchange": return Has(p => p.kind == "mansion" && p.variant == 0);
                case "treasury": return Has(p => p.kind == "mansion" && p.variant == 1);
                case "guildhall": return Has(p => p.kind == "guild_hall");
                case "gate": return Has(p => p.kind == "city_gate" && p.variant == 0);
                case "lamp": return Has(p => p.kind == "city_lamp");
                case "garden": return Has(p => p.kind == "parterre");
                case "well": return Has(p => p.kind == "fountain");
                case "street": case "wander": return crown.roads.Any(r => r != null && r.points.Length > 1);
                case "green": return crown.clearings.Length > 0;
                case "inn": case "bar": case "kitchen": case "kitchendoor": case "serve": return Has(p => p.kind == "inn" && p.variant == 3);
                default: return false;
            }
        }

        [Test] public void Every_job_has_a_workplace_in_the_Crown()
        {
            var problems = new List<string>();
            foreach (var role in Workers.Select(w => w.role).Distinct())
                foreach (var place in VillageWork.PlacesOf(role))
                    if (!Supplied(place)) problems.Add(role + " is sent to '" + place + "', which the Crown has none of");
            foreach (var place in VillageLife.CityPlaces) if (!Supplied(place)) problems.Add("the city's place kind '" + place + "' has no source in the Crown");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>The ground's level at a point from the terraces (a later one over an earlier; their edges' blend left out).</summary>
        static float Level(Vector2 p)
        {
            float h = 0;
            foreach (var t in crown.terraces)
            {
                float r = t.rotation * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r), dx = p.x - t.center.x, dz = p.y - t.center.y, lx = dx * c - dz * s, lz = dx * s + dz * c;
                if (Mathf.Abs(lx) <= t.size.x / 2 && Mathf.Abs(lz) <= t.size.y / 2) h = ZoneBuilder.TerraceLevel(t, lz);
            }
            return h;
        }
        /// <summary>Whether a point is in a landmark's high frame: within the camera's half-angles (a 60 degree lens at 16:9) of its line of sight.</summary>
        static bool InFrame(ZoneLabel l, Vector3 p)
        {
            var cam = new Vector3(l.view.x, Level(l.view) + l.viewHeight, l.view.y); var aim = new Vector3(l.at.x, Level(l.at) + l.viewAim, l.at.y);
            var look = Quaternion.LookRotation(aim - cam); var local = Quaternion.Inverse(look) * (p - cam);
            if (local.z <= 0) return false;
            return Mathf.Abs(Mathf.Atan2(local.x, local.z)) * Mathf.Rad2Deg < 44 && Mathf.Abs(Mathf.Atan2(local.y, local.z)) * Mathf.Rad2Deg < 27;
        }

        [Test] public void The_map_view_frames_the_whole_High_City_and_the_dome_and_the_inn_their_own()
        {
            var whole = crown.landmarks.Single(l => l.name == "The Crown of Argentis");
            Assert.Greater(whole.viewHeight, 60, "From high over the south-east (crown-19 was a grass corner under the wall).");
            Vector2 keep = Prop("The Council Keep").at, dome = Prop("The Council Hall").at, archive = Prop("The Archive").at;
            foreach (var (what, p) in new[] { ("the keep's spire", new Vector3(keep.x, 14 + 73, keep.y + 3)), ("the keep's door", new Vector3(keep.x, 16, keep.y - 12)), ("the dome", new Vector3(dome.x, 14 + 25, dome.y)),
                ("the Archive's spires", new Vector3(archive.x, 14 + 40, archive.y - 7)), ("the Crown Square", new Vector3(0, 14, 80)), ("the Gilt Gate", new Vector3(110.5f, 12, 10)) })
                Assert.IsTrue(InFrame(whole, p), what + " is in the frame of " + whole.name + ".");
            var hall = crown.landmarks.Single(l => l.name == "The Council Hall");
            Assert.Greater(hall.viewHeight, 7, "Over the colonnade's roof, not under it (crown-08).");
            Assert.IsTrue(InFrame(hall, new Vector3(dome.x, 14 + 22, dome.y)), "The dome is in its frame.");
            var lantern = crown.landmarks.Single(l => l.name == "The Gilded Lantern"); var inn = Prop("The Gilded Lantern");
            Assert.Less(Vector2.Distance(lantern.at, inn.at), 3, "The landmark on the inn.");
            Assert.Less(lantern.view.y, inn.at.y - inn.size.y / 2 - 6, "Its frame from before its front.");
            foreach (var (what, local) in new[] { ("its door", new Vector3(0, 2, -inn.size.y / 2)), ("its left corner", new Vector3(-inn.size.x / 2, 6, -inn.size.y / 2)), ("its right corner", new Vector3(inn.size.x / 2, 6, -inn.size.y / 2)) })
            {
                var w = World(inn, new Vector2(local.x, local.z));
                Assert.IsTrue(InFrame(lantern, new Vector3(w.x, Level(inn.at) + local.y, w.y)), what + " is in the Lantern's frame.");
            }
        }

        [Test] public void No_machines_and_no_borrowed_names()
        {
            var text = File.ReadAllText(Path.Combine(Zones, "crown.json")).ToLowerInvariant();
            foreach (var word in new[] { "gondola", " lift", "elevator", "engine", "gear-", "winch", "pulley", "auction house", "stormwind", "ironforge", "orgrimmar" })
                Assert.IsFalse(text.Contains(word), "No '" + word.Trim() + "' in the Crown (no machines, Chris 2026-10-11; no World of Warcraft names).");
        }

        /// <summary>The gardens (ZoneBuilder.CrownDetail, Chris 2026-10-16: "we need a real garden"): a lawn in each quarter of a big
        /// enough one, where the zone's grass grows though the terrace is paved; never on the walks, the cross walks or the hedge.</summary>
        [Test] public void The_gardens_have_lawns_off_their_walks()
        {
            var gardens = crown.props.Where(p => p != null && p.kind == "parterre").ToList();
            Assert.GreaterOrEqual(gardens.Count, 4, "The Crown's gardens.");
            foreach (var g in gardens)
            {
                var size = g.size.x > 0 ? g.size : new Vector2(12, 8);
                foreach (var walk in new[] { Vector2.zero, new Vector2(0, size.y / 2 - .3f), new Vector2(size.x / 2 - .3f, 0), new Vector2(.5f, size.y / 4), new Vector2(size.x / 4, .5f), new Vector2(size.x / 2 - 1.1f, size.y / 4), new Vector2(size.x / 4, size.y / 2 - 1.1f) })
                    Assert.IsFalse(ZoneBuilder.InGardenLawn(size, walk), g.name + ": no lawn on its walks or its hedge, at " + walk + ".");
            }
            Assert.GreaterOrEqual(gardens.Count(g => ZoneBuilder.InGardenLawn(g.size, g.size / 4)), 3, "Lawns in the bigger gardens.");
        }
    }
}
