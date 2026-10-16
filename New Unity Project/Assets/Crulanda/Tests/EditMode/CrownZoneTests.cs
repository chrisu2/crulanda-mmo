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
            Assert.IsTrue(crown.props.Any(p => p != null && p.kind == "signpost" && p.name.Contains("16-19")), "Its sign says what is below: dangerous, 16-19.");
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
            foreach (var c in new[] { "warrior", "druid", "paladin", "ranger", "mage", "rogue", "archivist", "saltmender", "cleric", "scoundrel", "shaman" })
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
