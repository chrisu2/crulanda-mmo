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
    /// The Crown's Crafters' Court as data (p9-court, 2026-10-16; Chris: "need more profession tools/small huts/vendored items", "make
    /// sure all professions are located in the castle"): a workshop for every trade (ZoneBuilder.CourtTrades), each with its station on
    /// its station spot, its trainer at the station and its stall-keeper behind the counter; the keepers' stock is real, priced and never
    /// what the world gives (ore, logs, herbs, meat, hides); nothing of the Court stands in its ways in, across it, or along its colonnades;
    /// the capture tour has the Court and two workshops close up. The built side (walked, reached, faced) is CrownCourtPlayTests.
    /// </summary>
    public class CrownCourtTests
    {
        static ZoneDefinition crown;
        [OneTimeSetUp] public void Load() { crown = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones", "crown.json"))); }
        static List<ZoneProp> Workshops { get { return crown.props.Where(p => ZoneBuilder.CourtTradeOf(p) != null).ToList(); } }
        static ZoneProp Prop(string name) { return crown.props.FirstOrDefault(p => p != null && p.name == name); }
        /// <summary>How far a point lies outside a prop's footprint (its size round its middle, turned with it); 0 inside.</summary>
        static float Outside(ZoneProp p, Vector2 size, Vector2 at)
        {
            float r = p.rotation * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r); var d = at - p.at;
            float lx = d.x * c - d.y * s, lz = d.x * s + d.y * c;
            return new Vector2(Mathf.Max(0, Mathf.Abs(lx) - size.x / 2), Mathf.Max(0, Mathf.Abs(lz) - size.y / 2)).magnitude;
        }
        /// <summary>The Court's solid things: its workshops, well, board and goods by their footprints, and its lamps and banner by a post's.</summary>
        static IEnumerable<(ZoneProp p, Vector2 size)> Solids
        {
            get
            {
                foreach (var p in crown.props.Where(p => p != null))
                {
                    var size = ZoneBuilder.CourtSize(p);
                    if (size.x > 0) yield return (p, size);
                    else if ((p.kind == "city_lamp" || p.kind == "council_banner") && p.name.Contains("Crafters' Court")) yield return (p, new Vector2(.6f, .6f));
                }
            }
        }
        [Test] public void Every_trade_has_a_workshop_with_its_station_its_trainer_and_its_stall()
        {
            var items = ProfessionDataTests.Items(); var db = ProfessionDataTests.Db(items); var problems = new List<string>();
            Assert.AreEqual(ZoneBuilder.CourtTrades.Length, Workshops.Count, "One workshop a trade.");
            Assert.AreEqual(ZoneBuilder.CourtTrades.Length, Workshops.Select(p => p.variant).Distinct().Count(), "No trade twice.");
            // Every trade the professions know, and the two of the craft branch (Leatherworking and Weaving: their trainers already stand ready).
            foreach (var id in db.Order.Select(d => d.id).Union(new[] { "leatherworking", "weaving" }))
            {
                var shops = Workshops.Where(p => ZoneBuilder.CourtTradeOf(p).profession == id).ToList();
                if (shops.Count != 1) { problems.Add(id + " has " + shops.Count + " workshops in the Court"); continue; }
                var p = shops[0]; var tr = ZoneBuilder.CourtTradeOf(p); string q = p.name + " (" + id + "): ";
                if (!p.name.StartsWith("The Court ")) problems.Add(q + "named for the Court, as its landmark and sign are");
                Vector2 Spot(Vector2 local) { return ZoneBuilder.CourtWorld(p, local); }
                // Its station on its spot, of the kind the trade's recipes are made at.
                var def = db.Profession(id);
                if (tr.station != null)
                {
                    var st = crown.stations.Where(s => s != null && ZoneBuilder.CourtWorkshopOf(crown, s) == p).ToList();
                    if (st.Count != 1) problems.Add(q + st.Count + " stations on its station spot");
                    else if (st[0].kind != tr.station) problems.Add(q + "its station is a " + st[0].kind + ", not a " + tr.station);
                    else if (Mathf.Abs(Mathf.DeltaAngle(st[0].rotation, p.rotation)) > 1) problems.Add(q + "its station is not turned with it");
                    if (def != null && !string.IsNullOrEmpty(def.station) && !ProfessionDatabase.Stations(def.station).Contains(tr.station)) problems.Add(q + "its recipes are not made at a " + tr.station);
                }
                else if (def != null && !string.IsNullOrEmpty(def.station)) problems.Add(q + "a craft with no station in its workshop");
                // Its trainer at the station, nearer the open front than it (so the city's rule turns them out to the court).
                var trainer = crown.life.residents.FirstOrDefault(r => r != null && r.role == "profession-trainer:" + id);
                if (trainer == null) problems.Add(q + "no trainer");
                else
                {
                    if (Vector2.Distance(trainer.at, Spot(tr.trainerAt)) > .05f) problems.Add(q + trainer.name + " stands at " + trainer.at + ", not at the trainer's post " + Spot(tr.trainerAt));
                    if (Vector2.Distance(trainer.at, Spot(tr.stationAt)) > EncounterSession.TrainerRange - 3) problems.Add(q + trainer.name + " is not by the station");
                    if (tr.trainerAt.y >= tr.stationAt.y) problems.Add(q + "the trainer stands behind the station");
                }
                // Its stall-keeper behind the counter, selling from the item data.
                var keeper = crown.life.residents.FirstOrDefault(r => r != null && r.role == "court-vendor:" + tr.id);
                if (keeper == null) { problems.Add(q + "no stall-keeper"); continue; }
                if (Vector2.Distance(keeper.at, Spot(tr.vendorAt)) > .05f) problems.Add(q + keeper.name + " stands at " + keeper.at + ", not behind the counter " + Spot(tr.vendorAt));
                if (tr.vendorAt.y <= tr.counterAt.y) problems.Add(q + "the keeper stands before the counter, not behind it");
                var stock = items.StockFor(keeper.name, keeper.role, crown.levelMax);
                if (stock.Count == 0) problems.Add(q + keeper.name + " sells nothing");
                if (def != null && !string.IsNullOrEmpty(def.tool) && !stock.Contains(def.tool)) problems.Add(q + keeper.name + " does not sell the " + def.tool + " that teaches " + def.name);
                if (string.IsNullOrEmpty(keeper.title) || !keeper.canonStatus.StartsWith("GAME-ONLY")) problems.Add(q + keeper.name + " has no title or is not labelled GAME-ONLY");
            }
            // The apprentices work in the workshops (their "tannery" benches), keeping no post.
            var apprentices = crown.life.residents.Where(r => r != null && r.role == "apprentice").ToList();
            Assert.GreaterOrEqual(apprentices.Count, 4, "Apprentices at work in the Court.");
            foreach (var a in apprentices) if (a.place != "tannery" || a.at != Vector2.zero || a.works) problems.Add(a.name + ": an apprentice works at the workshops' benches, keeping no post");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test] public void The_stall_keepers_sell_tools_and_makings_from_the_item_data_and_never_what_the_world_gives()
        {
            var items = ProfessionDataTests.Items(); var db = ProfessionDataTests.Db(items); var problems = new List<string>();
            var gathered = new HashSet<string>(db.Nodes.Select(n => n.item));
            var keepers = crown.life.residents.Where(r => r != null && r.role != null && r.role.StartsWith("court-vendor:")).ToList();
            Assert.AreEqual(ZoneBuilder.CourtTrades.Length, keepers.Count, "A keeper a workshop.");
            var court = items.Vendors.Where(v => v.npc != null && keepers.Any(k => k.name == v.npc)).ToList();
            Assert.AreEqual(keepers.Count, court.Count, "Each keeper's stock is listed under their name (Items/court.json).");
            foreach (var v in court)
            {
                if (!string.IsNullOrEmpty(v.role) || v.gearForZone) problems.Add(v.npc + ": a keeper's stock is theirs alone, not a whole trade's, and no gear");
                foreach (var id in v.items)
                {
                    var d = items.Get(id);
                    if (d == null) { problems.Add(v.npc + " sells '" + id + "', which is not in the item data"); continue; }
                    if (Inventory.Price(d) <= 0) problems.Add(v.npc + ": " + id + " has no price");
                    if (gathered.Contains(id)) problems.Add(v.npc + ": " + id + " is gathered in the world, never sold");
                    if (Inventory.IsHide(d) || d.trade == "inn.meat") problems.Add(v.npc + ": " + id + " is hunted, never sold");
                    if (d.kind == "bag") problems.Add(v.npc + ": " + id + ": the trade bags are the leatherworker's, earned by her quests");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test] public void Nothing_of_the_Court_stands_in_its_ways_or_in_another()
        {
            var problems = new List<string>(); const float half = 1.2f;   // a way's half width
            var solids = Solids.ToList();
            Assert.GreaterOrEqual(solids.Count, ZoneBuilder.CourtTrades.Length + 6, "The workshops, the well, the board and the goods.");
            foreach (var (name, at) in ZoneBuilder.CourtWayPoints())
                foreach (var (p, size) in solids)
                    if (Outside(p, size, at) < half) { problems.Add(p.name + " stands in " + name + " at " + at); break; }
            // Each workshop's open front: a strip 1.5 m deep before it, clear of everything else.
            foreach (var w in Workshops)
            {
                var tr = ZoneBuilder.CourtTradeOf(w);
                for (float x = -tr.w / 2 + .4f; x <= tr.w / 2 - .4f; x += .5f)
                    for (float z = .2f; z <= 1.5f; z += .4f)
                    {
                        var at = ZoneBuilder.CourtWorld(w, new Vector2(x, -tr.d / 2 - z));
                        foreach (var (p, size) in solids) if (p != w && Outside(p, size, at) < .1f) problems.Add(p.name + " stands before " + w.name + "'s open front at " + at);
                    }
            }
            // Clear of each other (a corner of each inside none of the rest) and of the Treasury and the colonnades; on the Crown terrace.
            var others = new[] { "The Treasury Vault", "The Crafters' Court colonnade, east", "The Crafters' Court colonnade, north" }.Select(Prop).ToList();
            var terrace = crown.terraces.First(t => t.name == "The Crown terrace");
            foreach (var (p, size) in solids)
            {
                var corners = new List<Vector2> { p.at };
                foreach (int a in new[] { -1, 1 }) foreach (int b in new[] { -1, 1 }) corners.Add(ZoneBuilder.CourtWorld(p, new Vector2(a * size.x / 2, b * size.y / 2)));
                foreach (var c in corners)
                {
                    foreach (var (o, osize) in solids) if (o != p && Outside(o, osize, c) < .3f) problems.Add(p.name + " runs into " + o.name + " at " + c);
                    foreach (var o in others)
                    {
                        var osize = o.kind == "colonnade" ? new Vector2(o.size.x + 1, o.size.y + 1) : ZoneBuilder.CitySize(o) + Vector2.one * 1.6f;
                        if (Outside(o, osize, c) < .5f) problems.Add(p.name + " runs into " + o.name + " at " + c);
                    }
                    if (Mathf.Abs(c.x - terrace.center.x) > terrace.size.x / 2 - 2 || Mathf.Abs(c.y - terrace.center.y) > terrace.size.y / 2 - 2) problems.Add(p.name + " stands off the Crown terrace at " + c);
                }
            }
            Assert.IsEmpty(problems.Distinct(), string.Join("\n", problems.Distinct()));
        }

        [Test] public void The_capture_tour_frames_the_Court_and_two_workshops_close_up()
        {
            var court = crown.landmarks.Single(l => l.name == "The Crafters' Court");
            Assert.Greater(court.viewPitch, 20, "The Court from above, its workshops round it.");
            foreach (var name in new[] { "The Court Smithy", "The Court Loom-house" })
            {
                var l = crown.landmarks.SingleOrDefault(x => x.name == name); Assert.IsNotNull(l, name + " has a close-up on the tour.");
                var w = Prop(name); Assert.IsNotNull(w, name + " is a workshop."); var tr = ZoneBuilder.CourtTradeOf(w); Assert.IsNotNull(tr);
                Assert.Less(Vector2.Distance(l.at, w.at), 1, name + ": the landmark on its workshop.");
                var front = ZoneBuilder.CourtWorld(w, new Vector2(0, -1)) - w.at; var look = l.view - w.at;
                Assert.Greater(Vector2.Dot(front.normalized, look.normalized), .8f, name + ": framed from before its open front.");
                Assert.That(l.viewZoom, Is.InRange(5f, 10f), name + ": close up.");
                StringAssert.StartsWith("GAME-ONLY", l.canonStatus);
                foreach (var (p, size) in Solids) Assert.Greater(Outside(p, size, l.view), .5f, name + "'s camera spot is clear of " + p.name);
            }
        }

        [Test] public void No_machines_and_no_borrowed_names_in_the_Court()
        {
            var words = ZoneBuilder.CourtTrades.SelectMany(t => new[] { t.name, t.sign }).Concat(crown.props.Where(p => p != null && p.kind != null && p.kind.StartsWith("court_")).Select(p => p.name))
                .Concat(crown.life.residents.Where(r => r != null && (r.role == "apprentice" || (r.role ?? "").StartsWith("court-vendor:"))).SelectMany(r => r.stories.Append(r.name).Append(r.title)));
            foreach (var s in words)
                foreach (var bad in new[] { "machine", "engine", "gear", "crank", "winch", "pulley", "wheel", "treadle", "stormwind", "ironforge", "orgrimmar" })
                    Assert.IsFalse(s.ToLowerInvariant().Contains(bad), "'" + s + "' says " + bad + " (no machines, Chris 2026-10-11; no World of Warcraft names).");
        }
    }
}
