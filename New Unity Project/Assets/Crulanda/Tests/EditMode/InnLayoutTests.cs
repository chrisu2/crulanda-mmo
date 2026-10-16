using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The inns' second pass, from the plan the builder lays out (ZoneBuilder.InnPlan) and the zones' data (Phase 8.6 step 1, 2026-10-15;
    /// Chris, note 205: "prob need double doors and inn a bit bigger so for people"):
    /// - every inn's door is a double door 2.6 m jamb to jamb, and the navmesh's way through it, worked out from the jambs' colliders,
    ///   the blockers' padding and the bake's agent radius, is over 1.4 m (two abreast; the old 1.6 m door left 0.6, one at a time);
    /// - the Golden Cask is 14 x 10 and the Cracked Hearth 13 x 9, with five tables of three, two stools at the bar and the bench by
    ///   the hearth; every zone with folk has a walk-in inn (the Peaks' and the Rim's waystations, the Shore's hearth-house);
    /// - every seat stands inside the walls, clear of the furniture, of the other seats and of the doorway's leaves and its line;
    /// - no inn meets a neighbour (a house, a stall, the hitching rail, the yard's table...) or stands in a road.
    /// </summary>
    public class InnLayoutTests
    {
        static readonly string[] Files = { "oakhaven", "khaven", "peaks", "ashrim", "verdant", "lowtowns" };   // the Lowtowns' Rusty Spoon (Phase 9.1)
        static ZoneDefinition Zone(string file) { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Zones/" + file + ".json"))); }
        static IEnumerable<(string file, ZoneProp p)> Inns()
        {
            foreach (var f in Files) foreach (var p in Zone(f).props) if (p != null && p.kind == "inn") yield return (f, p);
        }
        static ZoneBuilder.InnPlan Plan(ZoneProp p) { return ZoneBuilder.InnPlan.For(p.size.x > 0 ? p.size : p.variant == 2 ? new Vector2(8, 6) : new Vector2(12, 8), p.variant); }
        /// <summary>How far a point lies outside a box (middle, half size) in the same frame; 0 inside it.</summary>
        static float Outside(Vector2 p, Vector2 at, Vector2 half) { return new Vector2(Mathf.Max(0, Mathf.Abs(p.x - at.x) - half.x), Mathf.Max(0, Mathf.Abs(p.y - at.y) - half.y)).magnitude; }

        [Test] public void Every_inn_has_a_double_door_two_can_pass_through()
        {
            Assert.AreEqual(2.6f, ZoneBuilder.InnDoorW, 1e-4f, "The door is 2.6 m jamb to jamb: two leaves of 1.3 m.");
            // The navmesh's way through, from the geometry: the jambs' colliders stop JambShort short of the opening, every blocker is
            // padded by NavPad a side (EncounterNavigation), and the walkable mesh stops the agent's radius short of that.
            float gap = ZoneBuilder.InnDoorW + 2 * ZoneBuilder.JambShort - 2 * ZoneBuilder.NavPad - 2 * ZoneBuilder.NavAgentRadius;
            Assert.AreEqual(gap, ZoneBuilder.NavGap(ZoneBuilder.InnDoorW), 1e-4f);
            Assert.Greater(gap, 1.4f, "Two abreast through the double door (two bodies of .6 m with room between).");
            Assert.Less(ZoneBuilder.NavGap(1.6f), 1.2f, "The old door of one 1.6 m leaf left a strip one body wide: why they jammed.");
            int n = 0;
            foreach (var (file, p) in Inns())
            {
                var plan = Plan(p); n++;
                Assert.AreEqual(2.6f, plan.doorW, 1e-4f, file + ": " + p.name + "'s door");
                Assert.AreEqual(2.8f, plan.ColliderGap, 1e-4f, file + ": " + p.name + "'s jambs' colliders stand 2.8 m apart");
                Assert.Greater(plan.NavGap, 1.4f, file + ": " + p.name + "'s way through");
                Assert.Less(plan.doorW, plan.w - 2, file + ": " + p.name + "'s door leaves wall either side");
            }
            Assert.AreEqual(4, n, "The Golden Cask, the Cracked Hearth, Pilgrims' Rest and Wain's Rest.");
            Assert.Greater(ZoneBuilder.NavGap(ZoneBuilder.InnDoorW), 1.4f, "The Hearth-Tree's hearth-house has the same door.");
        }

        [Test] public void The_taverns_are_bigger_and_every_zone_has_somewhere_to_rest()
        {
            var cask = Zone("oakhaven").props.Single(p => p != null && p.name == "The Golden Cask");
            var hearth = Zone("khaven").props.Single(p => p != null && p.name == "The Cracked Hearth");
            Assert.AreEqual(new Vector2(14, 10), cask.size, "The Golden Cask is 14 x 10 (it was 12 x 8).");
            Assert.AreEqual(new Vector2(13, 9), hearth.size, "The Cracked Hearth is 13 x 9 (it was 11 x 7).");
            foreach (var p in new[] { cask, hearth })
            {
                var plan = Plan(p);
                Assert.AreEqual(5, plan.tables.Count, p.name + ": five tables");
                Assert.AreEqual(15, plan.seats.Count, p.name + ": fifteen seats at them");
                Assert.AreEqual(2, plan.stools.Count, p.name + ": two stools at the bar");
                Assert.AreEqual(2, plan.benchSeats.Count, p.name + ": the bench by the hearth");
                Assert.Greater(plan.barSize.x, 4, p.name + ": a longer bar than the old 4 m");
            }
            // Somewhere to rest indoors in every zone with folk (SimFigure.Inn finds one): the waystations and the Hearth-Tree.
            foreach (var (file, name) in new[] { ("peaks", "Pilgrims' Rest"), ("ashrim", "Wain's Rest") })
            {
                var p = Zone(file).props.Single(q => q != null && q.name == name && q.kind != "signpost");
                Assert.AreEqual("inn", p.kind, name + " is walked into"); Assert.AreEqual(2, p.variant, name + " is a waystation");
                var plan = Plan(p);
                Assert.GreaterOrEqual(plan.seats.Count, 8, name + ": seats down the long table"); Assert.GreaterOrEqual(plan.bunks.Count, 2, name + ": bunks");
            }
            Assert.AreEqual("ruined_house", Zone("ashrim").props.Single(q => q != null && q.name == "Wain's Rest" && q.kind == "inn").was, "Wain's Rest is raised over its ruin: it takes the ruin's draws.");
            Assert.AreEqual(48, ZoneBuilder.RuinDraws(new Vector2(10, 6)), "A fallen house of 10 x 6 took 48 of the zone's draws (six front stubs, eight back, ten ends, 24 for the joists).");
            var tree = Zone("verdant").props.Single(q => q != null && q.name == "The Hearth-Tree");
            Assert.AreEqual("treehouse", tree.kind); Assert.AreEqual("inn", tree.style, "The Hearth-Tree's house is walked into.");
        }

        [Test] public void Every_seat_stands_inside_the_walls_clear_of_the_furniture_the_others_and_the_doorway()
        {
            var problems = new List<string>(); const float body = .3f;
            foreach (var (file, p) in Inns())
            {
                var plan = Plan(p); var inner = plan.Inner; string q = file + ": " + p.name + ": "; var door = plan.DoorZone;
                var furniture = plan.Furniture();
                // The furniture inside the walls, and none in another's place or in the doorway.
                for (int i = 0; i < furniture.Count; i++)
                {
                    var (what, at, half) = furniture[i];
                    if (Mathf.Abs(at.x) + half.x > inner.x + .01f || Mathf.Abs(at.y) + half.y > inner.y + .01f) problems.Add(q + what + " goes through the walls");
                    if (Rect.MinMaxRect(at.x - half.x, at.y - half.y, at.x + half.x, at.y + half.y).Overlaps(door)) problems.Add(q + what + " stands in the doorway");
                    for (int j = i + 1; j < furniture.Count; j++)
                    {
                        var (other, oat, ohalf) = furniture[j];
                        if (Mathf.Abs(at.x - oat.x) < half.x + ohalf.x && Mathf.Abs(at.y - oat.y) < half.y + ohalf.y) problems.Add(q + what + " and " + other + " overlap");
                    }
                }
                var seats = plan.seats.Concat(plan.stools).Concat(plan.benchSeats).ToList();
                for (int i = 0; i < seats.Count; i++)
                {
                    var (at, face) = seats[i]; string s = q + "the seat at " + at + " ";
                    if (Mathf.Abs(at.x) > inner.x - body - .05f || Mathf.Abs(at.y) > inner.y - body - .05f) problems.Add(s + "is in the wall");
                    if (door.Contains(at)) problems.Add(s + "is in the doorway (the leaves and the line waiting to go out)");
                    // Clear of every piece of furniture but what the sitter sits at (a seat's own table, the long table, the bar).
                    foreach (var (what, fat, half) in furniture)
                    {
                        bool own = Vector2.Distance(face, fat) < .05f || what == "the long table" && Mathf.Abs(face.y - fat.y) < .05f;
                        float clear = Outside(at, fat, half);
                        if (clear <= 0) problems.Add(s + "is in " + what);
                        else if (!own && clear < body) problems.Add(s + "is " + clear.ToString("0.00") + " m from " + what);
                    }
                    for (int j = i + 1; j < seats.Count; j++) if (Vector2.Distance(at, seats[j].at) < 2 * body - .02f) problems.Add(s + "is in the lap of the seat at " + seats[j].at);
                    if ((face - at).magnitude < .2f) problems.Add(s + "faces nothing");
                }
                if (!plan.waystation)
                {
                    // The tavern's own places: the innkeeper behind the open end of the bar, the rooms door past it, the stools before it.
                    Assert.Greater(plan.stand.x, plan.bar.x + plan.barSize.x / 2, q + "the innkeeper stands past the bar's end");
                    Assert.Greater(plan.roomsX - .52f, plan.bar.x + plan.barSize.x / 2 + .2f, q + "the rooms door clears the bar's top");
                    foreach (var (at, _) in plan.stools) Assert.Less(at.y, plan.bar.y - plan.barSize.y / 2, q + "a stool stands before the bar");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        // ---------- the neighbours ----------
        /// <summary>A prop's footprint near the ground, as a box in its own frame (middle offset, half size): a building to its footing, the
        /// small things to what of them stands in the way. Null for what has no footprint (a road's verge, a secret, a light).</summary>
        static (Vector2 off, Vector2 half)? Footprint(ZoneProp p)
        {
            Vector2 size = p.size;
            switch (p.kind)
            {
                case "house": case "inn": case "barn": case "mill": case "keep": return (Vector2.zero, (size.x > 0 ? size : new Vector2(7, 5)) / 2 + Vector2.one * .31f);
                case "ruined_house": return (Vector2.zero, (size.x > 0 ? size : new Vector2(8, 6)) / 2 + Vector2.one * .3f);
                case "kitchen": return (Vector2.zero, ZoneBuilder.KitchenSize / 2 + new Vector2(.3f, .1f));
                case "leathershop": return (Vector2.zero, ZoneBuilder.LeatherShopSize / 2 + Vector2.one * .2f);
                case "dryhut": return (Vector2.zero, ZoneBuilder.DryingHutSize / 2 + Vector2.one * .2f);
                case "hitch": return (new Vector2(0, .3f), new Vector2(2, 1.8f));   // the open stable over the rail
                case "barrels": return (new Vector2(0, .3f), new Vector2(1.1f, .7f));
                case "crates": return (Vector2.zero, new Vector2(1.1f, .5f));
                case "stall": return (Vector2.zero, new Vector2(1.6f, 1.2f));
                case "well": return (Vector2.zero, Vector2.one * 1.6f);
                case "hedge": return (Vector2.zero, new Vector2((size.x > 0 ? size.x : 6) / 2, .7f));
                case "board": return (Vector2.zero, new Vector2(.8f, .3f));
                case "lamp": case "signpost": return (Vector2.zero, Vector2.one * .3f);
                case "cart": case "wagon": return (Vector2.zero, new Vector2(1.2f, .8f));
                case "market": { var piece = ZoneBuilder.MarketPieces[Mathf.Abs(p.variant) % ZoneBuilder.MarketPieces.Length]; return piece.solid ? (Vector2.zero, Vector2.one * (piece.name == "stall" ? 1.4f : piece.name == "hand cart" || piece.name == "log store" ? .9f : .6f)) : ((Vector2, Vector2)?)null; }
                case "tree": case "pine": case "dead_oak": case "rock": return (Vector2.zero, Vector2.one * .6f);
                default: return null;
            }
        }
        static Vector2 Axis(float yaw, bool z) { float r = yaw * Mathf.Deg2Rad; return z ? new Vector2(Mathf.Sin(r), Mathf.Cos(r)) : new Vector2(Mathf.Cos(r), -Mathf.Sin(r)); }   // a prop's local x or z in the world (Unity's yaw)
        static Vector2 At(ZoneProp p, Vector2 local) { return p.at + Axis(p.rotation, false) * local.x + Axis(p.rotation, true) * local.y; }
        /// <summary>The gap between two turned boxes (centre, half size, yaw) along the axis that parts them most; negative: they overlap.</summary>
        static float Gap(Vector2 ca, Vector2 ha, float ya, Vector2 cb, Vector2 hb, float yb)
        {
            Vector2 xa = Axis(ya, false), za = Axis(ya, true), xb = Axis(yb, false), zb = Axis(yb, true), d = cb - ca; float best = float.MinValue;
            foreach (var n in new[] { xa, za, xb, zb })
            {
                float reach = Mathf.Abs(Vector2.Dot(xa, n)) * ha.x + Mathf.Abs(Vector2.Dot(za, n)) * ha.y + Mathf.Abs(Vector2.Dot(xb, n)) * hb.x + Mathf.Abs(Vector2.Dot(zb, n)) * hb.y;
                best = Mathf.Max(best, Mathf.Abs(Vector2.Dot(d, n)) - reach);
            }
            return best;
        }
        static float ToPath(Vector2 p, Vector2[] pts)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < pts.Length; i++) { Vector2 a = pts[i], ab = pts[i + 1] - a; best = Mathf.Min(best, Vector2.Distance(p, a + ab * Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude)))); }
            return best;
        }

        [Test] public void No_inn_meets_a_neighbour_or_stands_in_a_road()
        {
            var problems = new List<string>(); int seen = 0;
            foreach (var file in Files)
            {
                var z = Zone(file);
                foreach (var inn in z.props.Where(p => p != null && p.kind == "inn"))
                {
                    var plan = Plan(inn); string q = file + ": " + inn.name + " ";
                    // Its walls and footing; in front, the tavern's porch (posts half a metre past the jambs, 2.3 m out) or the waystation's hood.
                    var parts = new List<(string what, Vector2 off, Vector2 half)> { ("walls", Vector2.zero, new Vector2(plan.w / 2 + .31f, plan.d / 2 + .31f)) };
                    if (plan.waystation) parts.Add(("door hood", new Vector2(0, -plan.d / 2 - .7f), new Vector2(plan.doorW / 2 + .45f, .55f)));
                    else parts.Add(("porch", new Vector2(0, -plan.d / 2 - .15f - 1.2f), new Vector2(plan.doorW / 2 + .5f + .45f + .05f, 1.25f)));
                    foreach (var (what, off, half) in parts)
                    {
                        var c = At(inn, off);
                        foreach (var o in z.props)
                        {
                            if (o == null || o == inn || o.kind == "wall" || Vector2.Distance(o.at, inn.at) > 25) continue;
                            if (o.kind == "kitchen" && what == "walls") continue;   // the Cask's kitchen abuts its back wall (WorkshopDataTests)
                            var f = Footprint(o); if (f == null) continue;
                            float gap = Gap(c, half, inn.rotation, At(o, f.Value.off), f.Value.half, o.rotation); seen++;
                            if (gap < 0) problems.Add(q + "(its " + what + ") meets " + o.kind + " '" + o.name + "' at " + o.at + " (" + (-gap).ToString("0.00") + " m into it)");
                        }
                    }
                    // In no road: its walls' corners and edges clear of every road's half width.
                    for (int k = 0; k <= 40; k++)
                    {
                        float u = -1 + k / 20f; var pw = new Vector2(plan.w / 2 + .31f, plan.d / 2 + .31f);
                        foreach (var local in new[] { new Vector2(u * pw.x, -pw.y), new Vector2(u * pw.x, pw.y), new Vector2(-pw.x, u * pw.y), new Vector2(pw.x, u * pw.y) })
                        {
                            var w = At(inn, local);
                            foreach (var r in z.roads) if (r != null && r.points.Length > 1 && ToPath(w, r.points) < r.width / 2) { problems.Add(q + "stands in " + r.name + " at " + w); goto nextInn; }
                        }
                    }
                    nextInn:;
                }
            }
            Assert.Greater(seen, 20, "The inns' neighbours were measured.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
