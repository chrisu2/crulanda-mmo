using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Crulanda.World
{
    /// <summary>
    /// The inns' second pass (Phase 8.6 plan 3a, steps 1 and 3; Chris, 2026-10-14 note 205: "running into each other and sometimes get
    /// stuck or delayed at door of inn..prob need double doors and inn a bit bigger so for people", and on 2026-10-15 "Yes, your
    /// defaults"): every inn's door is a double door of two leaves on two hinges, 2.6 m jamb to jamb, so the navmesh's way through is
    /// about 1.6 m and two can pass (it was 0.6: one at a time, nobody past anybody); the taverns are bigger (the Golden Cask 14 x 10,
    /// the Cracked Hearth 13 x 9) with five tables of three, a longer bar with two stools and the bench by the hearth; and the zones
    /// that had nowhere to rest have a walk-in one: the Peaks' Pilgrims' Rest and the Rim's Wain's Rest as waystations (one storey, a
    /// long table, bunks), the Shore's Hearth-Tree with a door and seats (ZoneBuilder.Verdant). The room's layout is one plan
    /// (<see cref="InnPlan"/>) that the builder lays out and the tests measure, and each inn is registered (<see cref="Inns"/>,
    /// ZoneInn) for the village's life to read. Nothing here draws from the zone's stream. GAME-ONLY.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The inns' door, jamb to jamb (two leaves of 1.3 m), and its height in a tavern.</summary>
        public const float InnDoorW = 2.6f, InnDoorH = 2.4f;
        /// <summary>The city inn's grand double door (variant 3, the Gilded Lantern): two leaves of 1.6 m, 3.4 m high.</summary>
        public const float CityDoorW = 3.2f, CityDoorH = 3.4f;
        /// <summary>
        /// How the navmesh is cut round a door (EncounterNavigation): every blocker's box is padded by <see cref="NavPad"/> a side, and the
        /// walkable mesh stops the bake's agent radius (<see cref="NavAgentRadius"/>, NavMesh settings 0) short of that; the jambs'
        /// colliders stop <see cref="JambShort"/> short of the opening (the Cracked Hearth's taproom was cut off from Khaven when they
        /// did not).
        /// </summary>
        public const float NavPad = .1f, NavAgentRadius = .5f, JambShort = .1f;
        /// <summary>The walkable width through a doorway of this opening (jamb to jamb) as the navmesh is baked: 2.6 m gives 1.6.</summary>
        public static float NavGap(float opening) { return opening + 2 * JambShort - 2 * NavPad - 2 * NavAgentRadius; }
        /// <summary>The walkable width through a fortified gate of this width (ZoneBuilder.Gate): its open leaves stand .25 in from each
        /// side and .3 thick, and the barrier boom's posts stand further in still (.45 and .4 from the sides), each a solid.</summary>
        public static float GateOpening(float width)
        {
            float half = width / 2, leaves = half - .25f - .15f, boom = half - .45f, rest = half - .4f;
            return Mathf.Min(2 * leaves, boom + rest) - 2 * NavPad - 2 * NavAgentRadius;
        }
        /// <summary>The walk-in inns of this zone, as built (the taverns, the waystations and the hearth-house).</summary>
        public readonly List<ZoneInn> Inns = new List<ZoneInn>();

        /// <summary>
        /// The draws a fallen house of this size takes from the zone's stream (ZoneBuilder.RuinedHouse: a wall stub every 1.2 m round it
        /// but the doorway's, and six for each of four fallen joists), so a building raised where one stood (Wain's Rest, ZoneProp.was)
        /// takes them too and every tree, rock and prop after it keeps its place.
        /// </summary>
        public static int RuinDraws(Vector2 size)
        {
            int n = 0; float w = size.x, d = size.y;
            foreach (int sz in new[] { -1, 1 }) for (float x = -w / 2 + .6f; x < w / 2; x += 1.2f) if (!(sz < 0 && Mathf.Abs(x) < 1)) n++;
            foreach (int sx in new[] { -1, 1 }) for (float z = -d / 2 + .6f; z < d / 2; z += 1.2f) n++;
            return n + 4 * 6;
        }

        /// <summary>
        /// One inn's room as laid out, in its own frame (front -z, the door in the middle of it; x along the front), for the builder and
        /// the tests alike. A tavern (variants 0 and 1): the bar along the back wall from the -x end with two stools before it and the
        /// innkeeper's place at its open end, the rooms door past it, the hearth on the +x end with its bench and woodpile, five
        /// tables of three seats kept off the doorway (its leaves swing in 1.3 m and the lane's line waits inside it). A waystation
        /// (variant 2): one long table down the room with a bench either side, bunks along the back wall, the hearth on the +x end.
        /// </summary>
        public sealed class InnPlan
        {
            public float w, d, doorW, doorH, wall = .3f; public bool waystation, city;
            /// <summary>Tavern: the bar's middle and size (x along it, y its depth), the innkeeper's place, the rooms door's x.</summary>
            public Vector2 bar, barSize, stand; public float roomsX;
            /// <summary>The hearth's footprint (middle, size), its fire (the light, and a station), and the bench before it.</summary>
            public Vector2 hearth, hearthSize, fire, bench, woodpile;
            /// <summary>The tables' middles and turns, and the radius their seats stand at.</summary>
            public readonly List<Vector2> tables = new List<Vector2>(); public readonly List<float> turns = new List<float>();
            public const float SeatRadius = 1, TableRadius = .75f;
            /// <summary>Where each sitter stands and the point they face.</summary>
            public readonly List<(Vector2 at, Vector2 face)> seats = new List<(Vector2, Vector2)>(), stools = new List<(Vector2, Vector2)>(), benchSeats = new List<(Vector2, Vector2)>();
            /// <summary>Waystation: the long table (middle, size), the bunks (middles; each <see cref="bunkSize"/>), and where a lodger goes to bed.</summary>
            public Vector2 longTable, longTableSize, bunkSize = new Vector2(2, .8f), sleep; public readonly List<Vector2> bunks = new List<Vector2>();
            /// <summary>A city inn (variant 3, the Crown's Gilded Lantern): the gallery's floor height and depth round three walls, the
            /// columns under its corners and ends, the stair up to it (middle, size; it rises toward +x along the front wall), the
            /// kitchen's range against the back wall (middle, size) with the cook's place and the hatch where goods are handed in, and the
            /// servers' places on the floor.</summary>
            public float galleryY, galleryDepth; public readonly List<Vector2> columns = new List<Vector2>(), serve = new List<Vector2>();
            public Vector2 stair, stairSize, range, rangeSize, cook, hatch;
            /// <summary>The kitchen's places before the range (the cook's first), each a metre or more from the next.</summary>
            public readonly List<Vector2> kitchen = new List<Vector2>();
            public const float ColumnRadius = .35f;
            /// <summary>Half the room inside the walls.</summary>
            public Vector2 Inner { get { return new Vector2(w / 2 - wall / 2, d / 2 - wall / 2); } }
            /// <summary>The gap between the jambs' colliders, and the navmesh's walkable width through it.</summary>
            public float ColliderGap { get { return doorW + 2 * JambShort; } }
            public float NavGap { get { return ZoneBuilder.NavGap(doorW); } }
            /// <summary>Where the door's leaves lie open (each 1.3 m back along the inside of its jamb), and the lane's waiting room inside
            /// (two in line): nothing stands there.</summary>
            public Rect DoorZone { get { return Rect.MinMaxRect(-doorW / 2 - .6f, -d / 2, doorW / 2 + .6f, -d / 2 + ZoneDoorway.Reach + ZoneDoorway.WaitOff + ZoneDoorway.WaitStep + .3f); } }

            public static InnPlan For(Vector2 size, int variant, float doorW = InnDoorW)
            {
                if (variant == 3) return City(size);
                var p = new InnPlan { w = size.x, d = size.y, doorW = doorW, waystation = variant == 2 };
                float w = p.w, d = p.d;
                if (p.waystation)
                {
                    p.doorH = 2.3f;
                    // The hearth on the +x end, the long table down the middle away from it, between the doorway and the bunks.
                    p.hearth = new Vector2(w / 2 - .55f, .2f); p.hearthSize = new Vector2(.8f, 1.6f); p.fire = new Vector2(w / 2 - 1.15f, .2f);
                    float length = w - 4.4f; p.longTable = new Vector2(-.6f, .6f); p.longTableSize = new Vector2(length, .8f);
                    int n = Mathf.Max(2, Mathf.FloorToInt(length / .9f + .01f));   // a seat every .9 m or a little more
                    foreach (int sz in new[] { -1, 1 })
                        for (int i = 0; i < n; i++)
                        {
                            float x = p.longTable.x - length / 2 + (i + .5f) * length / n;
                            p.seats.Add((new Vector2(x, p.longTable.y + sz * .65f), new Vector2(x, p.longTable.y)));
                        }
                    for (int i = 0, bn = Mathf.FloorToInt((w - .8f) / 2.2f); i < bn; i++) p.bunks.Add(new Vector2(-w / 2 + 1.4f + 2.2f * i, d / 2 - .55f));
                    p.sleep = new Vector2(p.bunks.Count > 0 ? p.bunks[0].x : 0, d / 2 - 1.7f);
                    return p;
                }
                p.doorH = InnDoorH;
                // The bar from the -x end along the back wall (4.4 m: it was 4), its top a little longer; the innkeeper at its open end.
                p.bar = new Vector2(-w / 2 + 2.45f, d / 2 - 1.5f); p.barSize = new Vector2(4.4f, .7f);
                p.stand = new Vector2(-w / 2 + 5.3f, d / 2 - 1.4f); p.roomsX = -w / 2 + 5.4f;
                foreach (float x in new[] { -w / 2 + 1.8f, -w / 2 + 3.4f }) p.stools.Add((new Vector2(x, d / 2 - 2.4f), new Vector2(x, p.bar.y)));
                p.hearth = new Vector2(w / 2 - .6f, .8f); p.hearthSize = new Vector2(.9f, 2.4f); p.fire = new Vector2(w / 2 - 1.6f, .8f);
                p.bench = new Vector2(w / 2 - 2.3f, .8f); p.woodpile = new Vector2(w / 2 - 1.2f, 2.7f);
                foreach (float z in new[] { .35f, 1.25f }) p.benchSeats.Add((new Vector2(p.bench.x, z), new Vector2(p.hearth.x, z)));
                // Five tables: two by the front corners, two by the -x end, one by the hearth's side of the room; their seats at 30, 150
                // and 270 degrees (or 90, 210, 330) so none sits in another table's lap or in the doorway.
                var at = new[] { new Vector2(-w / 2 + 2, -d / 2 + 1.9f), new Vector2(w / 2 - 2.2f, -d / 2 + 1.9f), new Vector2(-1.3f, -d / 2 + 4.2f), new Vector2(1.9f, -d / 2 + 5), new Vector2(-w / 2 + 2, -d / 2 + 4.8f) };
                var from = new[] { 30f, 30, 90, 90, 30 };
                for (int i = 0; i < at.Length; i++)
                {
                    p.tables.Add(at[i]); p.turns.Add(from[i]);
                    for (int k = 0; k < 3; k++)
                    {
                        float a = (from[i] + k * 120) * Mathf.Deg2Rad;
                        p.seats.Add((at[i] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * SeatRadius, at[i]));
                    }
                }
                return p;
            }
            /// <summary>
            /// The city inn (variant 3; GAME-ONLY, p9-cityinn; Chris, 2026-10-16: "dont need the same exact inn in the castle. needs a
            /// redesign"): white stone walls half a metre thick round a taproom two storeys high, a gallery 2.6 m deep at 3.5 m round the
            /// back and both ends on columns, a grand double door of <see cref="CityDoorW"/>. Under the back gallery the long bar from the
            /// -x end with six stools before it and the innkeeper at its open end, the rooms door past him; the hearth with its carved
            /// mantel on the +x end with the bench before it, the kitchen's range against the back wall past the rooms door; three private booths along the
            /// -x end (a table, a settle either side, two at each); five round tables of four on the open floor, none in the doorway; the
            /// stair up to the +x gallery along the front wall. Laid out for 22 x 16.
            /// </summary>
            static InnPlan City(Vector2 size)
            {
                var p = new InnPlan { w = size.x, d = size.y, doorW = CityDoorW, doorH = CityDoorH, wall = .5f, city = true, galleryY = 3.5f, galleryDepth = 2.6f };
                float hx = p.w / 2 - p.wall / 2, hz = p.d / 2 - p.wall / 2, g = p.galleryDepth;
                // The bar under the back gallery, from 3.75 m in from the -x wall, twelve metres short of the room's width.
                float len = Mathf.Max(6, p.w - 12), left = -hx + 3.75f;
                p.bar = new Vector2(left + len / 2, hz - 1.45f); p.barSize = new Vector2(len, .8f);
                p.stand = new Vector2(left + len + .7f, p.bar.y); p.roomsX = left + len + 2;
                for (float x = left + .6f; x <= left + len - 1.4f + .01f; x += 1.4f) p.stools.Add((new Vector2(x, p.bar.y - .4f - 1), new Vector2(x, p.bar.y)));
                // The hearth on the +x end, the bench before it, the woodpile in the back corner. The kitchen under the back gallery past
                // the rooms door: the range against the back wall (its front -z), the cook's place before it and two more by it for
                // whoever else is sent to the kitchen, a metre apart; the hatch out on the open floor before it. (The range stood in the
                // +x back corner 1.45 m from the gallery's corner column, a strip of navmesh 0.25 m wide with the cook's place in it, and
                // then by the hearth, where the one kitchen place drew the cook, the servers and the pot-boy into a corner of the range,
                // the hearth and the column round one point: the servers sent there stood stuck, CrownLifeTests, 2026-10-16.)
                p.hearth = new Vector2(hx - .45f, -1); p.hearthSize = new Vector2(.9f, 2.8f); p.fire = new Vector2(hx - 1.3f, -1);
                p.bench = new Vector2(hx - 2, -1); p.woodpile = new Vector2(hx - .6f, hz - .7f);
                foreach (float z in new[] { -1.6f, -.4f }) p.benchSeats.Add((new Vector2(hx - 2.15f, z), new Vector2(p.hearth.x, z)));
                p.range = new Vector2(left + len + 3.8f, hz - .4f); p.rangeSize = new Vector2(1.6f, .8f);
                p.cook = new Vector2(p.range.x, hz - 1.55f);
                p.kitchen.Add(p.cook); p.kitchen.Add(new Vector2(p.range.x - 1.1f, hz - 1.75f)); p.kitchen.Add(new Vector2(p.range.x, hz - 2.75f));
                p.hatch = new Vector2(p.range.x - 1.4f, hz - 3.85f);
                // The gallery's columns: at its inner corners and two down each end.
                foreach (int s in new[] { -1, 1 }) foreach (float z in new[] { -4.2f, 2.4f, hz - g }) p.columns.Add(new Vector2(s * (hx - g), z));
                // The stair up the front wall to the +x gallery, from 2.8 m right of the middle.
                p.stair = new Vector2((2.8f + hx - g) / 2, -hz + p.wall / 2 + 1f); p.stairSize = new Vector2(hx - g - 2.8f, 2f);   // 2 m wide from the wall's inner face (1.4 m centred on the wall's middle left 0.15 m to walk past the rail: CrownLifeTests)
                // The booths along the -x end: a table against the wall, a settle either side, two at each.
                foreach (float z in new[] { -4.6f, -1.6f, 1.4f })
                {
                    var at = new Vector2(-hx + .95f, z); p.tables.Add(at); p.turns.Add(90);
                    foreach (int sz in new[] { -1, 1 }) foreach (float dx in new[] { -.4f, .4f }) p.seats.Add((new Vector2(at.x + dx, z + sz), new Vector2(at.x + dx, z)));
                }
                // Five round tables of four on the open floor, seats at 45, 135, 225 and 315 degrees.
                foreach (var at in new[] { new Vector2(-5, -5.2f), new Vector2(5.2f, -3.8f), new Vector2(-4.2f, .8f), new Vector2(3.6f, .6f), new Vector2(-.4f, -2.4f) })
                {
                    p.tables.Add(at); p.turns.Add(45);
                    for (int k = 0; k < 4; k++) { float a = (45 + k * 90) * Mathf.Deg2Rad; p.seats.Add((at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * SeatRadius * 1.15f, at)); }   // on the diagonals: out past the table's corners (CrownZoneTests)
                }
                // Where the servers and the pot-boy wait between trays, in the aisles.
                p.serve.Add(new Vector2(-1.6f, 3.4f)); p.serve.Add(new Vector2(1.4f, -.6f)); p.serve.Add(new Vector2(-2.6f, -4.4f));
                return p;
            }
            /// <summary>What stands on the floor, each as a box (middle, half size) in the room's frame: the bar, the hearth, the tables,
            /// the long table and the bunks, and in a city inn its columns, its stair and its range (the tests keep the seats and each
            /// other clear of them).</summary>
            public List<(string what, Vector2 at, Vector2 half)> Furniture()
            {
                var list = new List<(string, Vector2, Vector2)>();
                if (barSize.x > 0) list.Add(("the bar", bar, barSize / 2));
                if (hearthSize.x > 0) list.Add(("the hearth", hearth, hearthSize / 2));
                for (int i = 0; i < tables.Count; i++) list.Add(("table " + (i + 1), tables[i], Vector2.one * TableRadius));
                if (longTableSize.x > 0) list.Add(("the long table", longTable, longTableSize / 2));
                for (int i = 0; i < bunks.Count; i++) list.Add(("bunk " + (i + 1), bunks[i], bunkSize / 2));
                for (int i = 0; i < columns.Count; i++) list.Add(("column " + (i + 1), columns[i], Vector2.one * ColumnRadius));
                if (stairSize.x > 0) list.Add(("the stair", stair, stairSize / 2));
                if (rangeSize.x > 0) list.Add(("the range", range, rangeSize / 2));
                return list;
            }
        }

        /// <summary>
        /// A double door on the face at local z = <paramref name="face"/> of <paramref name="t"/> (looking along -z): two plank leaves,
        /// each half the opening, hung on hinges at the two jambs and swinging in to lie against them (ZoneDoor.SetOpen), each with a
        /// collider for when it is shut and an obstacle for the walkers' avoidance; and the doorway's lanes (ZoneDoorway) through it.
        /// </summary>
        ZoneDoor DoubleDoor(Transform t, float face, float doorW, float doorH, Material planks, string name)
        {
            var door = new ZoneDoor { name = name, kind = "inn", openable = true, position = t.TransformPoint(new Vector3(0, 1, face)) };
            foreach (int s in new[] { -1, 1 })
            {
                var hinge = new GameObject(s < 0 ? "Door hinge" : "Door hinge (right)").transform; hinge.SetParent(t, false); hinge.localPosition = new Vector3(s * doorW / 2, 0, face - .02f);
                var leaf = PlankDoor(hinge, new Vector3(-s * doorW / 4, doorH / 2, 0), doorW / 2, doorH, planks, .1f);
                var shut = leaf.AddComponent<BoxCollider>();
                var stop = leaf.AddComponent<NavMeshObstacle>(); stop.shape = NavMeshObstacleShape.Box; stop.center = Vector3.zero; stop.size = Vector3.one; stop.carving = false; stop.enabled = false;
                if (s < 0) { door.hinge = hinge; door.blocker = shut; door.stop = stop; } else { door.hinge2 = hinge; door.blocker2 = shut; door.stop2 = stop; }
            }
            var mid = t.TransformPoint(new Vector3(0, 0, face));
            door.Way = new ZoneDoorway { name = name, middle = mid, inward = FlatOf(t.forward), across = FlatOf(t.right), width = doorW, walkable = NavGap(doorW) };
            return door;
        }
        static Vector3 FlatOf(Vector3 v) { v.y = 0; return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward; }
        /// <summary>Registers a walk-in inn: its seats, stools and bench (from the plan, in the world), its door and its room.</summary>
        ZoneInn RegisterInn(Transform frame, string style, ZoneDoor door, InnPlan plan, Vector2 half, bool round = false)
        {
            var inn = new ZoneInn { name = door.name, style = style, door = door, frame = frame, half = half, round = round };
            InnSeat Seat((Vector2 at, Vector2 face) s) { return new InnSeat { at = frame.TransformPoint(new Vector3(s.at.x, 0, s.at.y)), face = frame.TransformPoint(new Vector3(s.face.x, 0, s.face.y)) }; }
            if (plan != null)
            {
                foreach (var s in plan.seats) inn.seats.Add(Seat(s));
                foreach (var s in plan.stools) inn.bar.Add(Seat(s));
                foreach (var s in plan.benchSeats) inn.bench.Add(Seat(s));
            }
            Inns.Add(inn);
            return inn;
        }

        /// <summary>
        /// A waystation (inn variant 2; Phase 8.6 decision 12, 2026-10-15): one storey of plastered walls on a stone footing under a gable
        /// roof, a double door in the middle of its front with a lantern by it, windows either side and in the -x end, and inside a
        /// long table with a bench down either side, bunks along the back wall and a stone hearth on the +x end, lit. Walk-in like the
        /// inns (each wall piece its own collider and navigation blocker; the floor is the ground's), with a closed ceiling so the
        /// camera pulls in under it. Where it was raised over a fallen house (<see cref="ZoneProp.was"/>: Wain's Rest, whose last
        /// rafters the Ash-Walkers' deacon burnt, the Deacon's Tally) the roof is salt-cart canvas over a ridge pole and the wall tops
        /// are scorched: the walls stood, the carters roofed it again (GAME-ONLY, PROVISIONAL until Chris reads it).
        /// </summary>
        void Waystation(Transform t, Vector2 size, ZoneProp p)
        {
            var plan = InnPlan.For(size, 2);
            float w = size.x, d = size.y, H = 3f, wall = plan.wall, doorW = plan.doorW, doorH = plan.doorH;
            bool reroofed = p != null && p.was == "ruined_house";
            var plaster = Tint(art.plaster, Zone.biome == "ash" ? new Color(.66f, .64f, .61f) : Plaster[1]);
            var boards = Tint(art.timber, new Color(.38f, .27f, .18f)); var dark = Tint(art.timber, new Color(.2f, .13f, .08f));
            var roofMat = reroofed ? Tint(art.cloth, new Color(.74f, .7f, .6f)) : Zone.biome == "mountain" ? art.slate : art.thatch;
            GameObject WallPiece(Vector3 pos, Vector3 scale) { var o = BoxPart(t, pos, scale, plaster); o.AddComponent<BoxCollider>(); o.AddComponent<NavBlocker>(); return o; }
            // Floor, walls (the jambs' colliders stop short of the opening, as the inn's), corner posts and a rail at sill height.
            Part(PrimitiveType.Cube, t, new Vector3(0, .02f, 0), new Vector3(w - wall, .05f, d - wall), boards);
            WallPiece(new Vector3(0, H / 2, d / 2), new Vector3(w, H, wall));
            foreach (int s in new[] { -1, 1 }) WallPiece(new Vector3(s * w / 2, H / 2, 0), new Vector3(wall, H, d));
            float side = (w - doorW) / 2;
            foreach (int s in new[] { -1, 1 })
            {
                var jamb = WallPiece(new Vector3(s * (doorW / 2 + side / 2), H / 2, -d / 2), new Vector3(side, H, wall)).GetComponent<BoxCollider>();
                jamb.size -= new Vector3(JambShort, 0, 0); jamb.center += new Vector3(s * JambShort / 2, 0, 0);
            }
            WallPiece(new Vector3(0, doorH + (H - doorH) / 2, -d / 2), new Vector3(doorW, H - doorH, wall));
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * w / 2, H / 2, sz * d / 2), new Vector3(.34f, H, .34f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(0, .95f, d / 2 + .02f), new Vector3(w, .16f, wall + .08f), art.timber);
            float run = side - .14f;   // the front's rail stops at the door's frame
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * (w / 2 - run / 2), .95f, -d / 2 - .02f), new Vector3(run, .16f, wall + .08f), art.timber);
            foreach (int sx in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .02f), .95f, 0), new Vector3(wall + .08f, .16f, d), art.timber);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * (doorW / 2 + .07f), (doorH + .1f) / 2, -d / 2 - .03f), new Vector3(.14f, doorH + .1f, wall + .1f), art.timber);   // the door's posts
            Part(PrimitiveType.Cube, t, new Vector3(0, doorH + .08f, -d / 2 - .03f), new Vector3(doorW + .28f, .16f, wall + .1f), art.timber);   // and its lintel
            if (reroofed) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(0, H - .08f, sz * (d / 2 + .01f)), new Vector3(w + .02f, .18f, wall + .04f), Tint(art.plaster, new Color(.19f, .17f, .15f)));   // the fire's scorch along the wall tops
            var footing = Tint(Masonry, new Color(.5f, .48f, .44f));
            Footing(t, w + .5f, d + .5f, .3f, .35f, .5f, doorW / 2 + .05f, null, footing);
            Part(PrimitiveType.Cube, t, new Vector3(0, H - .1f, 0), new Vector3(w - wall, .12f, d - wall), Tint(art.timber, new Color(.3f, .22f, .15f))).AddComponent<BoxCollider>();   // the ceiling: the camera pulls in under it
            for (float x = -w / 2 + 1; x < w / 2 - .5f; x += 1.6f) Part(PrimitiveType.Cube, t, new Vector3(x, H - .25f, 0), new Vector3(.16f, .18f, d - wall), dark);
            // Windows: one either side of the door on the front, one in the -x end (the bunks have the back wall).
            foreach (int s in new[] { -1, 1 })
            {
                float x = s * (doorW / 2 + 1.4f); if (Mathf.Abs(x) + .5f > w / 2 - .2f) continue;
                Part(PrimitiveType.Cube, t, new Vector3(x, 1.55f, -d / 2), new Vector3(.8f, .7f, wall + .1f), art.glass);
                Window(t, new Vector3(x, 1.55f, -(d / 2 + wall / 2)), .8f, .7f, -1, 2, Mathf.Abs(x) + .74f <= w / 2 - .17f && Mathf.Abs(x) - .74f >= doorW / 2 + .05f, false);
            }
            Part(PrimitiveType.Cube, t, new Vector3(-w / 2, 1.55f, -.3f), new Vector3(wall + .1f, .7f, .8f), art.glass);
            // The roof: thatch (slate in the mountains); over the fallen house's shell, canvas thrown over a ridge pole.
            float roofH = Mathf.Max(1.8f, d * .4f);
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH, reroofed ? .06f : .25f, .6f - wall / 2), t, new Vector3(0, H, 0), roofMat);
            if (!reroofed) Eaves(t, w, d, H, roofH, roofMat);
            else Part(PrimitiveType.Cube, t, new Vector3(0, H + roofH - .02f, 0), new Vector3(w + 1.4f, .16f, .16f), dark);   // the ridge pole the canvas is thrown over
            Gables(t, w / 2 + wall / 2, d + wall, H, roofH, d / 2 + .7f, w / 2 + .6f, plaster, 2, .2f, reroofed ? .06f : .25f);
            BoxPart(t, new Vector3(w / 2 - .55f, H + roofH * .6f, plan.hearth.y), new Vector3(.8f, roofH * 1.3f, .8f), Masonry, null, 1);   // the chimney stack over the hearth
            if (art.particle != null) Smoke(t, new Vector3(w / 2 - .55f, H + roofH * 1.3f, plan.hearth.y));
            // The door under the roof's eaves (a storey of 3 m leaves no room for a hood of its own), on its stone threshold.
            var door = DoubleDoor(t, -d / 2, doorW, doorH, dark, t.name);
            DoorSteps(t, 0, -d / 2 - wall / 2, .05f, doorW + .4f, wall, footing);
            Doors.Add(door); door.SetOpen(true);
            // Inside: the long table on two trestles, a bench either side, the bunks (a frame of two berths, a straw tick on each), the hearth.
            var lt = plan.longTable; var ls = plan.longTableSize;
            Part(PrimitiveType.Cube, t, new Vector3(lt.x, .76f, lt.y), new Vector3(ls.x, .06f, ls.y), boards);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(lt.x + s * (ls.x / 2 - .35f), .38f, lt.y), new Vector3(.1f, .72f, ls.y - .1f), dark);
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(lt.x, .44f, lt.y + s * .65f), new Vector3(ls.x - .2f, .06f, .32f), boards);
                foreach (int e in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(lt.x + e * (ls.x / 2 - .5f), .21f, lt.y + s * .65f), new Vector3(.08f, .42f, .28f), dark);
            }
            for (float x = lt.x - ls.x / 2 + .5f; x < lt.x + ls.x / 2 - .3f; x += 1.3f) ModelProp(t, "Fantasy/Mug", new Vector3(x, .79f, lt.y + ((int)(x * 10) % 2 == 0 ? .2f : -.2f)), x * 40, .12f);
            var tick = Tint(art.cloth, new Color(.62f, .55f, .4f)); var bs = plan.bunkSize;
            foreach (var b in plan.bunks)
            {
                var berths = new GameObject("Bunk").transform; berths.SetParent(t, false); berths.localPosition = new Vector3(b.x, 0, b.y);
                foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, berths, new Vector3(sx * (bs.x / 2 - .05f), .85f, sz * (bs.y / 2 - .05f)), new Vector3(.1f, 1.7f, .1f), dark);
                foreach (float y in new[] { .35f, 1.25f })
                {
                    Part(PrimitiveType.Cube, berths, new Vector3(0, y, 0), new Vector3(bs.x, .08f, bs.y), boards);
                    Part(PrimitiveType.Cube, berths, new Vector3(0, y + .1f, 0), new Vector3(bs.x - .2f, .12f, bs.y - .12f), tick);
                }
                var solid = berths.gameObject.AddComponent<BoxCollider>(); solid.center = new Vector3(0, .85f, 0); solid.size = new Vector3(bs.x, 1.7f, bs.y); berths.gameObject.AddComponent<NavBlocker>();
            }
            var hearth = BoxPart(t, new Vector3(plan.hearth.x, .9f, plan.hearth.y), new Vector3(plan.hearthSize.x, 1.8f, plan.hearthSize.y), Masonry, null, 1);
            hearth.AddComponent<BoxCollider>(); hearth.AddComponent<NavBlocker>();
            Part(PrimitiveType.Cube, t, new Vector3(plan.hearth.x - plan.hearthSize.x / 2 - .02f, .5f, plan.hearth.y), new Vector3(.08f, .6f, 1), art.glass);   // embers
            var fire = new GameObject("Hearth fire").AddComponent<Light>(); fire.transform.SetParent(t, false);
            fire.transform.localPosition = new Vector3(plan.fire.x, 1, plan.fire.y); fire.type = LightType.Point; fire.range = 7; fire.intensity = 1.6f; fire.color = new Color(1, .55f, .25f);
            var lamp = new Vector3(doorW / 2 + .32f, 2.05f, -d / 2 - .3f);   // a lantern on the wall by the door
            var lantern = Glow(t, lamp, 7, .5f, new Color(1, .72f, .4f), 1.8f); lantern.name = "Waystation lantern";
            Part(PrimitiveType.Cube, t, lamp, new Vector3(.2f, .26f, .2f), art.glass);
            Part(PrimitiveType.Cube, t, lamp + new Vector3(0, .2f, .08f), new Vector3(.06f, .06f, .3f), dark);   // its bracket
            // Registered: its seats down both benches, its lodgers' bunks (a bed, not a door to knock at).
            var inn = RegisterInn(t, "waystation", door, plan, plan.Inner);
            var sleep = t.TransformPoint(new Vector3(plan.sleep.x, 0, plan.sleep.y));
            inn.bunks = new ZoneDoor { name = t.name, kind = "bunk", openable = false, position = sleep + Vector3.up };
            Doors.Add(inn.bunks);
        }
    }
}
