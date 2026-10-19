using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Lowtowns' houses from the Medieval Village Pack Vol.1 (MyDreamGameStudio, Asset Store EULA; Resources/Props/MedievalVillage1,
    /// 2026-10-19, play note 246, Chris: "i like this better for lowtowns look and feel"). Docs/ZONE_LOWTOWNS.md "The houses".
    /// The place is CANON (world_bible.md: the sprawling, shadowed districts at the Crown's foot; book1 ch.3, the Rust Market; ch.7:17,
    /// "a sprawling, chaotic sea of slate rooftops, vibrant, unnatural neon moss"); how the houses stand is GAME-ONLY.
    /// - Every tenement plot of the data (a terrace: its frontage <c>size.x</c>, its depth <c>size.y</c>, its front -z on the street) is
    ///   filled side by side with the pack's houses (<see cref="PlanVillageRow"/>), their doors on the street, each scaled to the plot's depth
    ///   (a door 1.8 to 2.6 m tall: the street's storey), mirrored or not, which house drawn from a stream of the plot's own.
    /// - The town stays vertical, as the books have it (tenements three and four storeys high): a four-storey terrace becomes a house on two
    ///   storeys of soot brick (an undercroft with its own doors on the street, a timber gallery along its top for the house's door); a
    ///   three-storey one becomes either a house on one brick storey or one of the pack's tall houses (House_5, House_6, House_10) standing
    ///   on the setts.
    /// - Kept as they were (procedural soot brick): the four terraces of the roof route over Pell Row (<see cref="RoofRow"/>: the lead
    ///   walks stand on their gutters, ZoneBuilder.Roofs), the blind backs at the zone's edge, and every building that is not a tenement
    ///   (the Rusty Spoon, the warehouse, the soup-hall, the Holding House, the dead-house, the market halls, the toll-house).
    /// - The Rust Market smith's shop is the pack's Blacksmith, on the terrace plot nearest the market hearth (<see cref="SmithyPlot"/>),
    ///   its anvil, table and weapon stand under its front awning.
    /// - The Lowtowns' colour on them: every material soot-darkened (two shades), their glass a fire's orange or a shard's violet in a
    ///   few windows and dark in the rest, thin moss on the roofs that follows the tiles (play note 257), soot smoke from one chimney in three, lanterns (no light of their
    ///   own) on some fronts; the yellow haze and the lane lamps are the zone's own, unchanged.
    /// - Colliders and the navmesh are the old ones: one box over the whole plot (Solid), as the tenement had, so nothing moves where
    ///   people walk. The houses only render.
    /// - Not used: the pack's Water_Wheel and WindMill (no machines, Chris 2026-10-11), its Tavern (the Rusty Spoon stays the zone's one
    ///   inn) and its Animal_Shed; neither these nor any of its models are used outside the Lowtowns (every zone its own buildings).
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        public const string MedievalKit = "MedievalVillage1/Prefabs/";
        /// <summary>Where the Rust Market's smith keeps shop: the terrace plot nearest the market hearth (The Rust Market hearth, 72, 24).</summary>
        public static readonly Vector2 SmithyPlot = new Vector2(50.88f, 33.53f);
        public const float VillagePlinth = .3f, VillageStorey = 3f, VillageGallery = .9f;
        /// <summary>How far a house may be shrunk or grown to fit its plot's depth (its door 1.8 to 2.6 m tall).</summary>
        public const float VillageScaleMin = .85f, VillageScaleMax = 1.15f;

        /// <summary>
        /// One of the pack's buildings, measured from its FBX (centimetres, Y up, the front door on +z; tools: the FBX's vertices by
        /// material): its extent across (x) and front to back (z, <see cref="front"/> the face of its door wall), its top, its eaves, a
        /// chimney's top (none when y is 0).
        /// </summary>
        public sealed class VillageModel
        {
            public string name; public float xMin, xMax, yMin, zMin, front, top, eaves; public bool glass; public Vector3 smoke;
            public string Prefab { get { return MedievalKit + name + "_Standard"; } }
            public float Width { get { return (xMax - xMin) / 100; } }
            public float Depth { get { return (front - zMin) / 100; } }
            public float MidX { get { return (xMax + xMin) / 200; } }
        }
        static VillageModel VM(string n, float x0, float x1, float y0, float z0, float front, float top, float eaves, bool glass, Vector3 smoke) { return new VillageModel { name = n, xMin = x0, xMax = x1, yMin = y0, zMin = z0, front = front, top = top, eaves = eaves, glass = glass, smoke = smoke }; }
        public static readonly VillageModel[] VillageModels =
        {
            VM("House_1", -216, 213, -4, -241, 214, 479, 224, false, new Vector3(-1, 479, -116)),
            VM("House_2", -341, 348, -8, -324, 291, 1056, 445, false, new Vector3(2, 1056, -114)),
            VM("House_3", -563, 576, -1, -382, 325, 871, 298, false, new Vector3(-428, 871, -25)),
            VM("House_4", -561, 579, -1, -396, 313, 912, 296, false, new Vector3(357, 912, -31)),
            VM("House_5", -506, 409, -6, -457, 399, 1573, 1073, false, new Vector3(10, 1573, -315)),
            VM("House_6", -428, 433, -4, -533, 363, 1126, 650, true, new Vector3(-12, 1126, -345)),
            VM("House_9", -616, 610, -7, -471, 378, 888, 294, true, new Vector3(-414, 888, -157)),
            VM("House_10", -289, 312, -6, -486, 377, 1189, 778, true, new Vector3(12, 1189, -348)),
            VM("Blacksmith", -709, 443, 3, -526, 494, 733, 246, false, Vector3.zero),
        };
        public static VillageModel VillageModelNamed(string name) { foreach (var m in VillageModels) if (m.name == name) return m; return null; }
        /// <summary>The houses that stand on the setts alone (tall enough for a three-storey terrace), and those that stand on brick.</summary>
        public static readonly string[] VillageTall = { "House_5", "House_6", "House_9", "House_10" };
        public static readonly string[] VillageOnBrick = { "House_2", "House_3", "House_4", "House_6", "House_10" };
        // (2026-10-19, the lowtowns-art2 frames: House_9's deep dark-timbered porch front read as a black slab raised on the brick; it stands
        // on the setts only, where it is a porch. House_7 and House_8, the same porch-fronted design and low, were taken out of the project.)
        /// <summary>The small one: House_1 only to fill the end of a row on brick.</summary>
        public const string VillageFiller = "House_1";

        /// <summary>One house of a row: its middle across the plot (x), its scale (front to back and up), its scale across (signed: a
        /// mirrored house is negative), and the height it stands on.</summary>
        public sealed class VillagePlace
        {
            public VillageModel model; public float x, scale, scaleX, baseY;
            public float Width { get { return model.Width * Mathf.Abs(scaleX); } }
            /// <summary>Its walls in the plot's own plan (x across, z from the front): left, right, front, back.</summary>
            public Rect Plan(float d) { return Rect.MinMaxRect(x - Width / 2, -d / 2, x + Width / 2, -d / 2 + model.Depth * scale); }
            public float Eaves { get { return baseY + model.eaves / 100 * scale; } }
            public float Top { get { return baseY + model.top / 100 * scale; } }
            /// <summary>Where its model's origin stands in the plot (turned half round, so its door on +z faces the plot's front, -z).</summary>
            public Vector3 Origin(float d) { return new Vector3(x + scaleX * model.MidX, baseY, scale * model.front / 100 - d / 2); }
        }
        /// <summary>A terrace plot's plan: its storeys of brick under the houses (0, 1 or 2), their top, and the houses left to right.</summary>
        public sealed class VillageRow
        {
            public ZoneProp prop; public Vector2 size; public int under; public float underTop; public bool smithy;
            public readonly List<VillagePlace> houses = new List<VillagePlace>();
            /// <summary>How high a washing line may hang at a point of the plot's front or back (x across): under the eaves there.</summary>
            public float EavesAt(float x)
            {
                foreach (var h in houses) if (Mathf.Abs(x - h.x) <= h.Width / 2) return h.Eaves;
                return under > 0 ? underTop : VillagePlinth + 3.2f;   // the brick, or the yard wall
            }
        }
        /// <summary>Whether a prop is a terrace the pack's houses fill: a tenement, not a blind back at the edge, not on the roof route.</summary>
        public static bool VillagePlot(ZoneProp p)
        {
            if (p == null || p.kind != "tenement" || p.style == "back") return false;
            foreach (var at in RoofRow) if (Vector2.Distance(p.at, at) < .6f) return false;
            return true;
        }
        /// <summary>A house's scale for a plot <paramref name="d"/> deep (its walls front to back at most d - .1); 0 when it cannot fit.</summary>
        public static float VillageScale(VillageModel m, float d)
        {
            float s = Mathf.Min(VillageScaleMax, (d - .1f) / m.Depth);
            return s < VillageScaleMin ? 0 : s;
        }
        /// <summary>
        /// The plan of a terrace plot (null if it is not one, <see cref="VillagePlot"/>): from a stream of the plot's own (its place and
        /// variant), so nothing else in the zone moves for it. Pure data: the tests read it without building the zone.
        /// </summary>
        public static VillageRow PlanVillageRow(ZoneProp p)
        {
            if (!VillagePlot(p)) return null;
            var size = LowtownSize(p); float w = size.x, d = size.y;
            var r = new System.Random((Mathf.RoundToInt(p.at.x * 8) * 73856093) ^ (Mathf.RoundToInt(p.at.y * 8) * 19349663) ^ (p.variant * 83492791) ^ 0x51A7E);
            float R() { return (float)r.NextDouble(); }
            var row = new VillageRow { prop = p, size = size };
            if (Vector2.Distance(p.at, SmithyPlot) < .6f)
            {
                var smith = VillageModelNamed("Blacksmith"); float s = VillageScale(smith, d);
                if (s > 0)
                {
                    row.smithy = true; row.under = 0; row.underTop = VillagePlinth;
                    row.houses.Add(new VillagePlace { model = smith, x = 0, scale = s, scaleX = s, baseY = VillagePlinth });
                    return row;
                }
            }
            int storeys = TenementStoreys(p.variant);
            row.under = storeys >= 4 ? 2 : R() < .5f ? 1 : 0;
            row.underTop = VillagePlinth + row.under * VillageStorey;
            var pool = new List<string>(row.under == 0 ? VillageTall : VillageOnBrick);
            float usable = w - .4f, used = 0; string prev = null;
            var picks = new List<(VillageModel m, float s)>();
            for (int guard = 0; guard < 12; guard++)
            {
                float rem = usable - used;
                var fits = new List<VillageModel>();
                foreach (var n in pool) { var m = VillageModelNamed(n); float s = VillageScale(m, d); if (s > 0 && m.Width * s <= rem + .01f) fits.Add(m); }
                if (fits.Count == 0 && row.under > 0) { var f = VillageModelNamed(VillageFiller); float s = VillageScale(f, d); if (s > 0 && f.Width * s <= rem + .01f) fits.Add(f); }
                if (fits.Count == 0) break;
                // Drawn by width (a wide house as likely per metre of street as a narrow one), never the same house twice side by side.
                float sum = 0; foreach (var m in fits) sum += m.Width; float u = R() * sum; int k = fits.Count - 1;
                for (int i = 0; i < fits.Count; i++) { u -= fits[i].Width; if (u < 0) { k = i; break; } }
                if (fits[k].name == prev && fits.Count > 1) k = (k + 1) % fits.Count;
                var pick = fits[k]; float ps = VillageScale(pick, d);
                picks.Add((pick, ps)); used += pick.Width * ps; prev = pick.name;
            }
            if (picks.Count == 0) return row;   // (no house fits: the plot keeps its brick)
            // Close the row up: each widened a little (at most 15 %) toward the plot's width, the rest left at its ends as yard walls.
            float stretch = Mathf.Clamp(usable / Mathf.Max(.01f, used), 1, 1.15f), total = used * stretch, x = -total / 2;
            foreach (var (m, s) in picks)
            {
                float wide = m.Width * s * stretch; bool mirror = R() < .5f;
                row.houses.Add(new VillagePlace { model = m, x = x + wide / 2, scale = s, scaleX = (mirror ? -1 : 1) * s * stretch, baseY = row.underTop });
                x += wide;
            }
            return row;
        }

        // ---------- building them ----------
        /// <summary>The rows built, by their plots (for the washing lines' heights and the clutter's door clearances).</summary>
        readonly List<VillageRow> villageRows = new List<VillageRow>();
        /// <summary>Points in front of the pack houses' doors the street clutter keeps clear of (LowtownClutter).</summary>
        readonly List<Vector2> villageDoorClear = new List<Vector2>();
        /// <summary>The pack's houses placed (the PlayMode tests count and measure them).</summary>
        public readonly List<(ZoneProp plot, VillagePlace place, Transform model)> VillageHouses = new List<(ZoneProp, VillagePlace, Transform)>();

        /// <summary>Soot over the pack's paint: two shades, a little warm (the smog's yellow).</summary>
        static readonly Color[] VillageSoot = { new Color(.84f, .8f, .74f), new Color(.74f, .7f, .64f) };   // (lighter after the art2 frames: the pack's timber is dark already)

        /// <summary>
        /// A terrace plot filled with the pack's houses (front -z): its brick under them, the houses, their gallery, its yard walls, the
        /// dressing. False when the plot is not one or the pack is not in the project (the tests' bare scenes): the caller builds the old
        /// tenement.
        /// </summary>
        bool VillageTerrace(Transform t, ZoneProp p, System.Random r)
        {
            if (Zone.biome != "lowtowns") return false;
            var row = PlanVillageRow(p); if (row == null || row.houses.Count == 0) return false;
            foreach (var h in row.houses) if (PropSource(h.model.Prefab) == null) return false;
            var g = new System.Random(r.Next()); float R() { return (float)g.NextDouble(); }
            float w = row.size.x, d = row.size.y; int storeys = TenementStoreys(p.variant);
            var brick = SootBrick(p.variant / 2 + (int)(R() * 2)); var band = SootBrick(3);
            var parts = new Batch();
            float drop = FootDrop(t, w, d);
            bool named = !string.IsNullOrEmpty(p.name) && p.name != p.kind;
            if (row.under > 0) Undercroft(t, parts, row, brick, band, drop, named, g);
            else
            {
                // A plinth of brick down to the lowest ground under the plot, a step of setts along the front, and yard walls behind the
                // houses and at the row's ends (the plot's width the old terrace had; the yards are behind them).
                BoxPart(t, new Vector3(0, (VillagePlinth - drop) / 2, 0), new Vector3(w, VillagePlinth + drop, d), brick, null, 2);
                float gr = Mathf.Min(LocalGround(t, 0, -d / 2 - .6f), 0);
                parts.Box(WetSetts, new Vector3(0, gr + (VillagePlinth - gr) / 2 - .02f, -d / 2 - .45f), new Vector3(w - .2f, VillagePlinth - gr, .9f));
                YardWalls(t, parts, row, brick, band, row.houses[0].x - row.houses[0].Width / 2, row.houses[row.houses.Count - 1].x + row.houses[row.houses.Count - 1].Width / 2, g);
            }
            // The houses.
            int middle = row.houses.Count / 2;
            for (int i = 0; i < row.houses.Count; i++)
            {
                var h = row.houses[i];
                var model = PlaceVillageModel(t, h, d, R() < .5f ? 0 : 1, g);
                if (model == null) continue;
                VillageHouses.Add((p, h, model));
                // Keep the street clutter off its front.
                for (float x = h.x - h.Width / 2 + .4f; x <= h.x + h.Width / 2 - .3f; x += 1.5f) { var o = t.TransformPoint(new Vector3(x, 0, -d / 2 - 1)); villageDoorClear.Add(new Vector2(o.x, o.z)); }
                if (row.under == 0 && named && i == middle) Doors.Add(new ZoneDoor { name = t.name, openable = false, position = t.TransformPoint(new Vector3(h.x, 1, -d / 2 - 1.35f)) });
                // A pot, a tub, a barrel or a spinning wheel on the gallery beside its door.
                if (row.under > 0 && R() < .55f)
                {
                    string[] gear = { "Pot_1", "Pot_2", "Wooden_Tub", "Wooden_Bucket", "Wooden_Barrel", "Wooden_Spinning_Wheel" };
                    float[] tall = { .55f, .5f, .45f, .4f, .9f, .95f };
                    int k = (int)(R() * gear.Length) % gear.Length; float side = R() < .5f ? -1 : 1;
                    float gx = h.x + side * Mathf.Max(.6f, h.Width / 2 - .7f);
                    if (Mathf.Abs(gx) < w / 2 - .4f) ModelProp(t, MedievalKit + gear[k] + "_Standard", new Vector3(gx, row.underTop, -d / 2 - VillageGallery / 2), R() * 360, tall[k], false, VillageGallery - .1f);
                }
            }
            if (row.smithy) SmithyDressing(t, row.houses[0], d);
            // A lantern on the front (glowing glass, no light of its own: the lane lamps light the street), on one plot in three.
            if (R() < .34f)
            {
                float lx = row.houses.Count > 1 ? row.houses[0].x + row.houses[0].Width / 2 : row.houses[0].x - row.houses[0].Width / 2 + .5f;
                var at = new Vector3(lx, row.under > 0 ? 2.9f : 3.1f, -d / 2);
                Rod(t, at, at + new Vector3(0, 0, -.55f), .045f, SootIron);
                parts.Box(Glowing(LampSickly, 1.4f), at + new Vector3(0, -.3f, -.5f), new Vector3(.24f, .3f, .24f));
                parts.Box(SootIron, at + new Vector3(0, -.12f, -.5f), new Vector3(.3f, .05f, .3f));
            }
            if (p.style == "shop")
            {
                // The shop's board out on an iron bracket by the middle house.
                float sx = row.houses[middle].x + .9f;
                Rod(t, new Vector3(sx, 2.9f, -d / 2), new Vector3(sx, 2.9f, -d / 2 - 1.1f), .05f, SootIron);
                Part(PrimitiveType.Cube, t, new Vector3(sx, 2.45f, -d / 2 - .8f), new Vector3(.06f, .7f, .9f), Tint(art.timber, new Color(.3f, .22f, .12f)));
            }
            // As the tenement had: one box over the whole plot (its height the old eaves), so the navmesh is the old one.
            float H = .5f + storeys * VillageStorey;
            Solid(t, new Vector3(0, H / 2, 0), new Vector3(w, H, d));
            parts.Flush(this, t, "Village");
            villageRows.Add(row);
            return true;
        }

        /// <summary>
        /// The brick under a row (one or two storeys of soot brick, the old terrace's): a block under each house, its own footprint and a
        /// hand more (the 2026-10-19 frames: one box for the whole plot read as a blank crate with a house overhanging it), doors on
        /// stoops on the street, windows with sills, lintels and jambs on every face that shows (a few lit, most dark), a string course at
        /// each floor, a leaded top; a timber gallery along the row's front at the top on iron brackets, a rail on its edge, for the houses'
        /// doors. Where the plot runs on past the blocks (its ends, behind a shallow house) a brick yard wall (YardWalls).
        /// </summary>
        void Undercroft(Transform t, Batch parts, VillageRow row, Material brick, Material band, float drop, bool named, System.Random g)
        {
            float R() { return (float)g.NextDouble(); }
            float w = row.size.x, d = row.size.y, U = row.underTop;
            var lit = FireWindow; var dark = DarkPane; var frame = TarredTimber;
            var doorWood = new[] { Tint(art.timber, new Color(.22f, .14f, .1f)), Tint(art.timber, new Color(.16f, .18f, .2f)), Tint(art.timber, new Color(.26f, .12f, .09f)) };
            int middleHouse = row.houses.Count / 2; float rowLeft = w / 2, rowRight = -w / 2;
            for (int hi = 0; hi < row.houses.Count; hi++)
            {
                var h = row.houses[hi];
                float x0 = Mathf.Max(-w / 2, h.x - h.Width / 2 - .15f), x1 = Mathf.Min(w / 2, h.x + h.Width / 2 + .15f);
                if (hi > 0) x0 = Mathf.Max(x0, row.houses[hi - 1].x + row.houses[hi - 1].Width / 2 + .15f);   // abutting its neighbour's block, not inside it
                float back = Mathf.Min(d / 2, -d / 2 + h.model.Depth * h.scale + .2f), bw = x1 - x0, bd = back + d / 2, cx = (x0 + x1) / 2, cz = (back - d / 2) / 2;
                rowLeft = Mathf.Min(rowLeft, x0); rowRight = Mathf.Max(rowRight, x1);
                float dropHere = FootDrop(t, bw, bd, cz);
                BoxPart(t, new Vector3(cx, (U - dropHere) / 2, cz), new Vector3(bw, U + dropHere, bd), brick, null, 2);
                parts.Box(TarredTimber, new Vector3(cx, U + .03f, cz), new Vector3(bw + .04f, .06f, bd + .04f));   // the leads
                for (int s = 1; s <= row.under; s++)
                {
                    float y = VillagePlinth + s * VillageStorey - .1f, th = s == row.under ? .26f : .16f;
                    foreach (int sz in new[] { -1, 1 }) parts.Box(band, new Vector3(cx, y, sz < 0 ? -d / 2 - .06f : back + .06f), new Vector3(bw + .1f, th, .14f));
                    foreach (int sx in new[] { -1, 1 }) parts.Box(band, new Vector3(sx < 0 ? x0 - .06f : x1 + .06f, y, cz), new Vector3(.14f, th, bd + .1f));
                }
                // The faces that show: the front (doors on the ground floor), the back, and an end that stands free.
                var faces = new List<(Vector3 origin, Vector3 along, float len, bool front)> { (new Vector3(x0, 0, -d / 2), Vector3.right, bw, true), (new Vector3(x1, 0, back), Vector3.left, bw, false) };
                if (hi == 0) faces.Add((new Vector3(x0, 0, back), Vector3.back, bd, false));
                if (hi == row.houses.Count - 1) faces.Add((new Vector3(x1, 0, -d / 2), Vector3.forward, bd, false));
                foreach (var f in faces)
                {
                    var outward = -Vector3.Cross(Vector3.up, f.along).normalized;   // out of the wall (along +x on the front: -z)
                    var turn = Quaternion.LookRotation(outward);
                    int bays = Mathf.Max(1, Mathf.RoundToInt(f.len / 2.6f)); float bay = f.len / bays; int middle = bays / 2;
                    for (int s = 0; s < row.under; s++)
                        for (int b = 0; b < bays; b++)
                        {
                            var at = f.origin + f.along * (bay * (b + .5f)); float y = VillagePlinth + s * VillageStorey;
                            if (f.front && s == 0 && (b % 2 == 0 || bays <= 2))
                            {
                                parts.Box(doorWood[(int)(R() * doorWood.Length) % doorWood.Length], at + new Vector3(0, y + 1.1f, -.04f), new Vector3(1.05f, 2.2f, .07f));
                                parts.Box(SootIron, at + new Vector3(.3f, y + 1.05f, -.09f), new Vector3(.1f, .1f, .04f));
                                parts.Box(frame, at + new Vector3(0, y + 2.3f, -.06f), new Vector3(1.3f, .16f, .14f));
                                float gr = Mathf.Min(LocalGround(t, at.x, -d / 2 - .6f), 0);
                                parts.Box(WetSetts, new Vector3(at.x, gr + (VillagePlinth - gr) / 2 - .02f, -d / 2 - .45f), new Vector3(1.6f, VillagePlinth - gr, .9f));
                                if (named && hi == middleHouse && b == 0) Doors.Add(new ZoneDoor { name = t.name, openable = false, position = t.TransformPoint(new Vector3(at.x, 1, -d / 2 - 1.35f)) });
                                continue;
                            }
                            bool glow = R() < (s == 0 ? .3f : .2f), shard = glow && R() < .2f;   // a fire in most, a shard's violet in one in five (book1 ch.1:191; ch.3:133)
                            var c = at + new Vector3(0, y + 1.55f, 0);
                            parts.Box(glow ? (shard ? ShardWindow : lit) : dark, c + outward * .02f, new Vector3(.8f, 1.2f, .06f), turn);
                            parts.Box(band, c + outward * .07f + Vector3.down * .68f, new Vector3(1.05f, .1f, .16f), turn);   // the sill
                            parts.Box(frame, c + outward * .05f + Vector3.up * .66f, new Vector3(1.0f, .14f, .12f), turn);   // the lintel
                            foreach (int k in new[] { -1, 1 }) parts.Box(frame, c + outward * .05f + turn * new Vector3(k * .44f, 0, 0), new Vector3(.08f, 1.25f, .08f), turn);   // the jambs
                        }
                }
            }
            YardWalls(t, parts, row, brick, band, rowLeft, rowRight, g);
            // The gallery: planks over the street above head height, a rail at its edge, iron brackets under it (nothing stands in the street).
            float gl = rowLeft + .05f, gw = rowRight - rowLeft - .1f, gc = (rowLeft + rowRight) / 2, gz = -d / 2 - VillageGallery / 2;
            parts.Box(TarredTimber, new Vector3(gc, U - .06f, gz), new Vector3(gw, .12f, VillageGallery));
            parts.Box(TarredTimber, new Vector3(gc, U + .95f, -d / 2 - VillageGallery + .04f), new Vector3(gw, .07f, .07f));
            for (float x = gl; x <= gl + gw + .01f; x += 1.2f)
                parts.Box(TarredTimber, new Vector3(x, U + .47f, -d / 2 - VillageGallery + .04f), new Vector3(.06f, .95f, .06f));
            for (float x = gl + .5f; x < gl + gw - .3f; x += 2.4f)
                Rod(t, new Vector3(x, U - .9f, -d / 2), new Vector3(x, U - .14f, -d / 2 - VillageGallery + .1f), .035f, SootIron);
        }

        /// <summary>
        /// Brick yard walls where a row leaves its plot open (the plot's one collider box stands there): its ends past the row
        /// (<paramref name="left"/>, <paramref name="right"/>) full depth, and behind each house shallower than the plot; a coping on top
        /// and a plank gate with iron straps in the ends and in half the back walls, so none reads as a blank box.
        /// </summary>
        void YardWalls(Transform t, Batch parts, VillageRow row, Material brick, Material band, float left, float right, System.Random g)
        {
            float w = row.size.x, d = row.size.y; const float yard = 3.2f;
            var gate = Tint(art.timber, new Color(.24f, .17f, .11f));
            void Wall(Vector3 c, Vector3 size, Vector3 gateFace)
            {
                parts.Box(brick, c, size);
                parts.Box(band, c + Vector3.up * (size.y / 2 + .06f), new Vector3(size.x + .05f, .12f, size.z + .05f));
                if (gateFace == Vector3.zero) return;
                float along = Mathf.Abs(gateFace.z) > .5f ? size.x : size.z; if (along < 1.6f) return;
                var face = c + Vector3.Scale(gateFace, size / 2) + gateFace * .03f; face.y = VillagePlinth + 1.05f;
                var q = Quaternion.LookRotation(gateFace);
                parts.Box(gate, face, new Vector3(1.2f, 2.1f, .07f), q);
                foreach (float k in new[] { -.55f, .55f }) parts.Box(SootIron, face + gateFace * .03f + Vector3.up * k, new Vector3(1.2f, .08f, .03f), q);
                parts.Box(band, face + Vector3.up * 1.15f, new Vector3(1.45f, .14f, .16f), q);   // the lintel over it
            }
            if (left + w / 2 > .15f) Wall(new Vector3((-w / 2 + left) / 2, VillagePlinth + yard / 2, 0), new Vector3(left + w / 2, yard, d), Vector3.back);
            if (w / 2 - right > .15f) Wall(new Vector3((right + w / 2) / 2, VillagePlinth + yard / 2, 0), new Vector3(w / 2 - right, yard, d), Vector3.back);
            foreach (var h in row.houses)
                if (-d / 2 + h.model.Depth * h.scale < d / 2 - .6f)
                    Wall(new Vector3(h.x, VillagePlinth + yard / 2, d / 2 - .15f), new Vector3(h.Width, yard, .3f), g.NextDouble() < .5 ? Vector3.forward : Vector3.zero);
        }

        /// <summary>
        /// One of the pack's houses on its place in the plot: its prefab (the pack's own Built-in materials) under a holder turned half round
        /// (its door to the street), scaled, mirrored when its scale across is negative; measured on placing so it lands where its table says
        /// whatever the FBX's units. Soot over its paint (<paramref name="soot"/>), its glass lit or dark, moss on a plain roof, smoke from
        /// one chimney in three. No colliders (the plot's box is the wall).
        /// </summary>
        Transform PlaceVillageModel(Transform t, VillagePlace h, float d, int soot, System.Random g)
        {
            float R() { return (float)g.NextDouble(); }
            var m = h.model; var src = PropSource(m.Prefab); if (src == null) return null;
            var holder = new GameObject("Village " + m.name).transform; holder.SetParent(t, false);
            holder.localPosition = h.Origin(d); holder.localRotation = Quaternion.Euler(0, 180, 0); holder.localScale = new Vector3(h.scaleX, h.scale, h.scale);
            var kit = new GameObject("Kit").transform; kit.SetParent(holder, false);
            var inst = Instantiate(src, kit, false); inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = src.transform.localRotation; inst.transform.localScale = src.transform.localScale;
            foreach (var c in inst.GetComponentsInChildren<Component>(true))
                if (!(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer)) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            // The kit in the table's metres: scaled by its measured width, its corner on the table's.
            var b = PropBounds(kit); float f = m.Width / Mathf.Max(.001f, b.size.x);
            kit.localScale = Vector3.one * f;
            kit.localPosition = new Vector3(m.xMin / 100 - f * b.min.x, m.yMin / 100 - f * b.min.y, m.zMin / 100 - f * b.min.z);
            // The FBX importer turns the file's right hand into Unity's left by negating x: if the model came in mirrored against the table
            // (its measured left edge is the table's right), it is turned back, so its door and chimney stand where the table says.
            if (Mathf.Abs(f * b.min.x - m.xMin / 100) > Mathf.Abs(f * b.min.x + m.xMax / 100) + .05f)
            {
                kit.localScale = new Vector3(-f, f, f);
                kit.localPosition = new Vector3(m.xMin / 100 + f * b.max.x, kit.localPosition.y, kit.localPosition.z);
            }
            // The Lowtowns' colour: soot on every material; the glass lit (a fire, or a shard's violet) in three houses in ten, dark in the rest.
            var tint = VillageSoot[Mathf.Clamp(soot, 0, VillageSoot.Length - 1)];
            bool glow = R() < .3f, shard = glow && R() < .2f; var pane = glow ? (shard ? ShardWindow : FireWindow) : DarkPane;
            foreach (var rend in holder.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = rend.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    mats[i] = mats[i].name.Contains("_Glass") ? pane : Tint(mats[i], tint);
                }
                rend.sharedMaterials = mats; rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; rend.receiveShadows = true;
            }
            // Neon moss on a plain roof (CANON, book1 ch.7:17), lying on its slopes either side of the ridge.
            // Moss on the roof as moss grows (play note 257, Chris: "Make it look like moss"): thin, uneven pads lying on the roof's own
            // tiles, found on the model's roof triangles, gathered along the ridge, the eaves and round the chimney; dull by day.
            if (R() < .62f) PackRoofMoss(holder, m, g);
            // Soot smoke from one chimney in three (on the plot, not under the house's scale).
            if (m.smoke.y > 0 && R() < .34f) Smoke(t, t.InverseTransformPoint(holder.TransformPoint(new Vector3(m.smoke.x / 100, m.smoke.y / 100 + .25f, m.smoke.z / 100))));
            return holder;
        }

        /// <summary>
        /// Neon moss on one of the pack's roofs (CANON, book1 ch.7:17; play note 257, Chris, 2026-10-19: "Make it look like moss"): the roof's
        /// own triangles (the submeshes whose material is a roof's) are read in the house's space and every pad is laid on the tile under it,
        /// along its slope, a hand's breadth thick at most and lifted a few millimetres, so nothing floats and nothing is a flat sticker.
        /// Where moss grows: strips along the ridge and the eaves (the roof's top and bottom tenth), clumps round the chimney's foot, a rare
        /// cushion on the open slope; each a scatter of small pads of their own size, turn and green, so the edge is broken.
        /// </summary>
        void PackRoofMoss(Transform holder, VillageModel m, System.Random g)
        {
            float R() { return (float)g.NextDouble(); }
            var tris = new List<Vector3>(); var toHolder = holder.worldToLocalMatrix;
            foreach (var mf in holder.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; var rend = mf.GetComponent<MeshRenderer>(); if (mesh == null || rend == null || !mesh.isReadable) continue;
                var mats = rend.sharedMaterials; var verts = mesh.vertices; var mtx = toHolder * mf.transform.localToWorldMatrix;
                for (int sm = 0; sm < mesh.subMeshCount && sm < mats.Length; sm++)
                {
                    if (mats[sm] == null || !mats[sm].name.Contains("_Roof") || mats[sm].name.Contains("Window")) continue;
                    var idx = mesh.GetTriangles(sm);
                    for (int i = 0; i + 2 < idx.Length; i += 3)
                    {
                        Vector3 a = mtx.MultiplyPoint3x4(verts[idx[i]]), b = mtx.MultiplyPoint3x4(verts[idx[i + 1]]), c = mtx.MultiplyPoint3x4(verts[idx[i + 2]]);
                        var n = Vector3.Cross(b - a, c - a); if (n.sqrMagnitude < 1e-8f) continue;
                        if (Mathf.Abs(n.normalized.y) < .25f) continue;   // a gable's face or a fascia: no moss on the upright
                        tris.Add(a); tris.Add(b); tris.Add(c);
                    }
                }
            }
            if (tris.Count == 0) return;
            const float cell = .5f; var grid = new Dictionary<(int, int), List<int>>();
            float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            for (int t = 0; t < tris.Count; t += 3)
            {
                float ax = Mathf.Min(tris[t].x, Mathf.Min(tris[t + 1].x, tris[t + 2].x)), bx = Mathf.Max(tris[t].x, Mathf.Max(tris[t + 1].x, tris[t + 2].x));
                float az = Mathf.Min(tris[t].z, Mathf.Min(tris[t + 1].z, tris[t + 2].z)), bz = Mathf.Max(tris[t].z, Mathf.Max(tris[t + 1].z, tris[t + 2].z));
                x0 = Mathf.Min(x0, ax); x1 = Mathf.Max(x1, bx); z0 = Mathf.Min(z0, az); z1 = Mathf.Max(z1, bz);
                for (int k = 0; k < 3; k++) { y0 = Mathf.Min(y0, tris[t + k].y); y1 = Mathf.Max(y1, tris[t + k].y); }
                for (int gx = Mathf.FloorToInt(ax / cell); gx <= Mathf.FloorToInt(bx / cell); gx++)
                    for (int gz = Mathf.FloorToInt(az / cell); gz <= Mathf.FloorToInt(bz / cell); gz++)
                    { if (!grid.TryGetValue((gx, gz), out var l)) grid[(gx, gz)] = l = new List<int>(); l.Add(t); }
            }
            // The top of the roof over (x, z), and its slope there.
            bool Top(float x, float z, out Vector3 hit, out Vector3 normal)
            {
                hit = default; normal = Vector3.up; bool any = false;
                if (!grid.TryGetValue((Mathf.FloorToInt(x / cell), Mathf.FloorToInt(z / cell)), out var l)) return false;
                foreach (int t in l)
                {
                    Vector3 a = tris[t], b = tris[t + 1], c = tris[t + 2];
                    float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z); if (Mathf.Abs(d) < 1e-7f) continue;
                    float u = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / d, v = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / d, w = 1 - u - v;
                    if (u < -.001f || v < -.001f || w < -.001f) continue;
                    float y = u * a.y + v * b.y + w * c.y;
                    if (any && y <= hit.y) continue;
                    hit = new Vector3(x, y, z); var n = Vector3.Cross(b - a, c - a).normalized; normal = n.y < 0 ? -n : n; any = true;
                }
                return any;
            }
            var batch = new Batch(); var greens = new[] { NeonMoss, NeonMossPale }; int pads = 0;
            void Pad(float x, float z, float size, float stretch)
            {
                if (!Top(x, z, out var hit, out var n)) return;
                var along = Vector3.Cross(Vector3.up, n); if (along.sqrMagnitude < 1e-4f) along = Vector3.right; along.Normalize();   // along the tiles' courses
                var rot = Quaternion.AngleAxis((R() - .5f) * 40, n) * Quaternion.LookRotation(Vector3.Cross(along, n), n);
                float thick = .01f + R() * .01f;
                var dims = new Vector3(size * (.6f + R() * .7f) * stretch, thick, size * (.45f + R() * .5f));
                batch.Box(greens[R() < .7f ? 0 : 1], hit + n * (.004f + thick / 2), dims, rot); pads++;
            }
            void Clump(float x, float z, float spread, int n, float stretch)
            {
                for (int i = 0; i < n; i++) { float f = i == 0 ? 0 : spread * (.3f + R() * .7f), a = R() * Mathf.PI * 2; Pad(x + Mathf.Cos(a) * f, z + Mathf.Sin(a) * f, (i == 0 ? .34f : .16f) + R() * .22f, stretch); }
            }
            // Walk the roof on a coarse grid; moss where it gathers (ridge, eaves, the chimney's foot), now and then on the open slope.
            float rise = Mathf.Max(.1f, y1 - y0);
            var chimney = m.smoke.y > 0 ? new Vector2(m.smoke.x / 100, m.smoke.z / 100) : new Vector2(1e6f, 1e6f);
            for (float x = x0 + .2f; x < x1 - .2f; x += .55f)
                for (float z = z0 + .2f; z < z1 - .2f; z += .55f)
                {
                    if (!Top(x, z, out var hit, out _)) continue;
                    float rel = (hit.y - y0) / rise, chance = rel > .88f ? .22f : rel < .14f ? .2f : .015f;
                    if ((new Vector2(x, z) - chimney).magnitude < 1.3f) chance = .45f;
                    if (R() > chance) continue;
                    Clump(x + (R() - .5f) * .3f, z + (R() - .5f) * .3f, .35f + R() * .35f, 2 + (int)(R() * 4), rel > .88f || rel < .14f ? 1.4f : 1);
                    if (pads > 220) break;
                }
            if (pads == 0) return;
            var root = new GameObject("Roof moss").transform; root.SetParent(holder, false);
            batch.Flush(this, root, "Moss");
        }

        /// <summary>The smith's shop's gear under its front awning, beside the door (the pack's anvil on a stump, its table with a hammer,
        /// a weapon stand (the Fantasy kit's, as the forges have) with the pack's sword, spear and shield, a cart wheel for mending) and its sign over the awning.</summary>
        void SmithyDressing(Transform t, VillagePlace h, float d)
        {
            Vector3 At(float mx, float mz) { return new Vector3(h.x + h.scaleX * (h.model.MidX - mx / 100), h.baseY, h.scale * (h.model.front - mz) / 100 - d / 2); }
            // (model x: the door is on the left, -533..-224, so the gear stands on the right; model z: under the awning, 380..490)
            ModelProp(t, MedievalKit + "Stump_A_Standard", At(150, 440), 0, .55f);
            var anvil = ModelProp(t, MedievalKit + "Anvil_Standard", At(150, 440) + Vector3.up * .55f, 90, .34f);
            ModelProp(t, MedievalKit + "Blacksmith_Table_Standard", At(300, 445), 180, .86f);
            ModelProp(t, MedievalKit + "Hammer_Standard", At(300, 445) + Vector3.up * .86f, 30, .12f);
            ModelProp(t, "Fantasy/WeaponStand", At(20, 470), 180, 1.5f, false, 1.4f);   // (the pack's Built-in set has no weapon stand)
            ModelProp(t, MedievalKit + "Sword_Standard", At(-30, 470) + Vector3.up * .1f, 180, 1.06f);
            ModelProp(t, MedievalKit + "Spear_Standard", At(60, 470) + Vector3.up * .05f, 180, 1.8f);
            ModelProp(t, MedievalKit + "Shield_Standard", At(390, 470), 200, .63f);
            ModelProp(t, MedievalKit + "Wood_Tyre_Standard", At(-150, 480), 170, 1.0f);
            ModelProp(t, MedievalKit + "Blacksmith_Logo_Standard", At(-600, 400) + Vector3.up * 1.05f, 180, .8f);   // on the wall left of the door
            if (anvil == null) Debug.LogWarning("LOWTOWNS_VILLAGE the smithy's anvil is missing from the pack.");
        }

        /// <summary>How high a washing line may hang where it meets a terrace at <paramref name="at"/> (just under the eaves of the house
        /// there); no limit off the pack's rows.</summary>
        float VillageLineCap(Vector2 at)
        {
            foreach (var row in villageRows)
            {
                var l = Quaternion.Euler(0, -row.prop.rotation, 0) * new Vector3(at.x - row.prop.at.x, 0, at.y - row.prop.at.y);
                if (Mathf.Abs(l.x) <= row.size.x / 2 + .3f && Mathf.Abs(l.z) <= row.size.y / 2 + .3f) return row.EavesAt(l.x) - .3f;
            }
            return float.MaxValue;
        }
    }
}
