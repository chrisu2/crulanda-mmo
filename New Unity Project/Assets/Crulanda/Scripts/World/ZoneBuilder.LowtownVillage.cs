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
    ///   few windows and dark in the rest, neon moss on the plain roofs, soot smoke from one chimney in three, lanterns (no light of their
    ///   own) on some fronts; the yellow haze and the lane lamps are the zone's own, unchanged.
    /// - Colliders and the navmesh are the old ones: one box over the whole plot (Solid), as the tenement had, so nothing moves where
    ///   people walk. The houses only render.
    /// - Not used: the pack's Water_Wheel and WindMill (no machines, Chris 2026-10-11), its Tavern (the Rusty Spoon stays the zone's one
    ///   inn) and its Animal_Shed; neither these nor any of its models are used outside the Lowtowns (every zone its own buildings).
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        public const string VillageKit = "MedievalVillage1/Prefabs/";
        /// <summary>Where the Rust Market's smith keeps shop: the terrace plot nearest the market hearth (The Rust Market hearth, 72, 24).</summary>
        public static readonly Vector2 SmithyPlot = new Vector2(50.88f, 33.53f);
        public const float VillagePlinth = .3f, VillageStorey = 3f, VillageGallery = .9f;
        /// <summary>How far a house may be shrunk or grown to fit its plot's depth (its door 1.8 to 2.6 m tall).</summary>
        public const float VillageScaleMin = .85f, VillageScaleMax = 1.15f;

        /// <summary>
        /// One of the pack's buildings, measured from its FBX (centimetres, Y up, the front door on +z; tools: the FBX's vertices by
        /// material): its extent across (x) and front to back (z, <see cref="front"/> the face of its door wall), its top, its eaves, a
        /// chimney's top (none when y is 0), and the ridge of a plain roof for the moss (ridge x or z, along x or z, its span and half
        /// width, eaves and ridge heights; none when <see cref="mossSpan"/> is 0).
        /// </summary>
        public sealed class VillageModel
        {
            public string name; public float xMin, xMax, yMin, zMin, front, top, eaves; public bool glass; public Vector3 smoke;
            public bool ridgeAlongX; public float ridgeAt, mossMid, mossSpan, mossHalf, mossEaves, mossRidge;
            public string Prefab { get { return VillageKit + name + "_Standard"; } }
            public float Width { get { return (xMax - xMin) / 100; } }
            public float Depth { get { return (front - zMin) / 100; } }
            public float MidX { get { return (xMax + xMin) / 200; } }
            public bool Moss { get { return mossSpan > 0; } }
        }
        static VillageModel VM(string n, float x0, float x1, float y0, float z0, float front, float top, float eaves, bool glass, Vector3 smoke) { return new VillageModel { name = n, xMin = x0, xMax = x1, yMin = y0, zMin = z0, front = front, top = top, eaves = eaves, glass = glass, smoke = smoke }; }
        static VillageModel Moss(VillageModel m, bool alongX, float at, float mid, float span, float half, float e, float ridge) { m.ridgeAlongX = alongX; m.ridgeAt = at; m.mossMid = mid; m.mossSpan = span; m.mossHalf = half; m.mossEaves = e; m.mossRidge = ridge; return m; }
        public static readonly VillageModel[] VillageModels =
        {
            Moss(VM("House_1", -216, 213, -4, -241, 214, 479, 224, false, new Vector3(-1, 479, -116)), false, 1, 0, 483, 211, 224, 417),
            Moss(VM("House_2", -341, 348, -8, -324, 291, 1056, 445, false, new Vector3(2, 1056, -114)), false, 4, 8, 613, 344, 445, 877),
            Moss(VM("House_3", -563, 576, -1, -382, 325, 871, 298, false, new Vector3(-428, 871, -25)), true, -15, 6, 1139, 366, 298, 749),
            Moss(VM("House_4", -561, 579, -1, -396, 313, 912, 296, false, new Vector3(357, 912, -31)), true, -25, 9, 1140, 371, 296, 747),
            Moss(VM("House_5", -506, 409, -6, -457, 399, 1573, 1073, false, new Vector3(10, 1573, -315)), false, 17, -6, 829, 391, 1073, 1380),
            Moss(VM("House_6", -428, 433, -4, -533, 363, 1126, 650, true, new Vector3(-12, 1126, -345)), false, 2, -45, 926, 430, 650, 987),
            VM("House_7", -617, 609, -5, -462, 380, 575, 309, false, Vector3.zero),
            VM("House_8", -633, 593, -1, -454, 393, 772, 301, true, Vector3.zero),
            VM("House_9", -616, 610, -7, -471, 378, 888, 294, true, new Vector3(-414, 888, -157)),
            Moss(VM("House_10", -289, 312, -6, -486, 377, 1189, 778, true, new Vector3(12, 1189, -348)), false, 12, -43, 874, 300, 778, 1099),
            VM("Blacksmith", -709, 443, 3, -526, 494, 733, 246, false, Vector3.zero),
        };
        public static VillageModel VillageModelNamed(string name) { foreach (var m in VillageModels) if (m.name == name) return m; return null; }
        /// <summary>The houses that stand on the setts alone (tall enough for a three-storey terrace), and those that stand on brick.</summary>
        public static readonly string[] VillageTall = { "House_5", "House_6", "House_10" };
        public static readonly string[] VillageOnBrick = { "House_2", "House_3", "House_4", "House_6", "House_8", "House_9", "House_10" };
        /// <summary>The low ones: House_7 only on two storeys of brick; House_1 only to fill the end of a row on brick.</summary>
        public const string VillageLow = "House_7", VillageFiller = "House_1";

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
            if (row.under == 2) pool.Add(VillageLow);
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
        static readonly Color[] VillageSoot = { new Color(.7f, .66f, .6f), new Color(.58f, .55f, .5f) };

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
                const float yard = 3.2f;
                foreach (var h in row.houses)   // behind a house shallower than the plot (not through a deep one's back eaves)
                    if (-d / 2 + h.model.Depth * h.scale < d / 2 - .6f)
                    {
                        parts.Box(brick, new Vector3(h.x, VillagePlinth + yard / 2, d / 2 - .15f), new Vector3(h.Width, yard, .3f));
                        parts.Box(band, new Vector3(h.x, VillagePlinth + yard + .06f, d / 2 - .15f), new Vector3(h.Width + .05f, .12f, .4f));
                    }
                float left = row.houses[0].x - row.houses[0].Width / 2, right = row.houses[row.houses.Count - 1].x + row.houses[row.houses.Count - 1].Width / 2;
                foreach (var (a, b) in new[] { (-w / 2, left), (right, w / 2) })
                    if (b - a > .15f)
                    {
                        parts.Box(brick, new Vector3((a + b) / 2, VillagePlinth + yard / 2, 0), new Vector3(b - a, yard, d));
                        parts.Box(band, new Vector3((a + b) / 2, VillagePlinth + yard + .06f, 0), new Vector3(b - a + .05f, .12f, d + .05f));
                    }
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
                    if (Mathf.Abs(gx) < w / 2 - .4f) ModelProp(t, VillageKit + gear[k] + "_Standard", new Vector3(gx, row.underTop, -d / 2 - VillageGallery / 2), R() * 360, tall[k], false, VillageGallery - .1f);
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
        /// The brick under a row (one or two storeys of soot brick, the old terrace's): doors on stoops every other bay on the street (the
        /// middle one a household's when the terrace is named), windows (a few lit, most dark), a string course at each floor, a flat leaded
        /// top under the houses, and a timber gallery along its front at the top on iron brackets, a rail on its edge, for the houses' doors.
        /// </summary>
        void Undercroft(Transform t, Batch parts, VillageRow row, Material brick, Material band, float drop, bool named, System.Random g)
        {
            float R() { return (float)g.NextDouble(); }
            float w = row.size.x, d = row.size.y, U = row.underTop;
            BoxPart(t, new Vector3(0, (U - drop) / 2, 0), new Vector3(w, U + drop, d), brick, null, 2);
            parts.Box(TarredTimber, new Vector3(0, U + .03f, 0), new Vector3(w + .04f, .06f, d + .04f));   // the leads
            for (int s = 1; s <= row.under; s++)
                foreach (int sz in new[] { -1, 1 }) parts.Box(band, new Vector3(0, VillagePlinth + s * VillageStorey - .1f, sz * (d / 2 + .06f)), new Vector3(w + .1f, .16f, .14f));
            int bays = Mathf.Max(2, Mathf.RoundToInt(w / 2.6f)); float bay = w / bays;
            var lit = FireWindow; var dark = DarkPane; var frame = TarredTimber;
            var doorWood = new[] { Tint(art.timber, new Color(.22f, .14f, .1f)), Tint(art.timber, new Color(.16f, .18f, .2f)), Tint(art.timber, new Color(.26f, .12f, .09f)) };
            int middle = bays / 2; if (middle % 2 != 0 && bays > 3) middle--;
            for (int s = 0; s < row.under; s++)
                for (int b = 0; b < bays; b++)
                {
                    float x = -w / 2 + bay * (b + .5f), y = VillagePlinth + s * VillageStorey + 1.55f;
                    foreach (int sz in new[] { -1, 1 })
                    {
                        bool door = sz < 0 && s == 0 && (b % 2 == 0 || bays <= 3);
                        if (door)
                        {
                            parts.Box(doorWood[(int)(R() * doorWood.Length) % doorWood.Length], new Vector3(x, VillagePlinth + 1.1f, -d / 2 - .04f), new Vector3(1.05f, 2.2f, .07f));
                            parts.Box(SootIron, new Vector3(x + .3f, VillagePlinth + 1.05f, -d / 2 - .09f), new Vector3(.1f, .1f, .04f));
                            parts.Box(frame, new Vector3(x, VillagePlinth + 2.3f, -d / 2 - .06f), new Vector3(1.3f, .16f, .14f));
                            float gr = Mathf.Min(LocalGround(t, x, -d / 2 - .6f), 0);
                            parts.Box(WetSetts, new Vector3(x, gr + (VillagePlinth - gr) / 2 - .02f, -d / 2 - .45f), new Vector3(1.6f, VillagePlinth - gr, .9f));
                            if (named && b == middle) Doors.Add(new ZoneDoor { name = t.name, openable = false, position = t.TransformPoint(new Vector3(x, 1, -d / 2 - 1.35f)) });
                            continue;
                        }
                        bool glow = R() < (s == 0 ? .3f : .2f), shard = glow && R() < .2f;   // a fire in most, a shard's violet in one in five (book1 ch.1:191; ch.3:133)
                        parts.Box(glow ? (shard ? ShardWindow : lit) : dark, new Vector3(x, y, sz * (d / 2 + .02f)), new Vector3(.8f, 1.2f, .06f));
                        parts.Box(band, new Vector3(x, y - .68f, sz * (d / 2 + .07f)), new Vector3(1.05f, .1f, .16f));
                        parts.Box(frame, new Vector3(x, y + .66f, sz * (d / 2 + .05f)), new Vector3(1.0f, .14f, .12f));
                    }
                }
            // The gallery: planks over the street above head height, a rail at its edge, iron brackets under it (nothing stands in the street).
            float gz = -d / 2 - VillageGallery / 2;
            parts.Box(TarredTimber, new Vector3(0, U - .06f, gz), new Vector3(w - .1f, .12f, VillageGallery));
            parts.Box(TarredTimber, new Vector3(0, U + .95f, -d / 2 - VillageGallery + .04f), new Vector3(w - .1f, .07f, .07f));
            for (float x = -w / 2 + .1f; x <= w / 2 - .05f; x += 1.2f)
                parts.Box(TarredTimber, new Vector3(x, U + .47f, -d / 2 - VillageGallery + .04f), new Vector3(.06f, .95f, .06f));
            for (float x = -w / 2 + .6f; x < w / 2 - .3f; x += 2.4f)
                Rod(t, new Vector3(x, U - .9f, -d / 2), new Vector3(x, U - .14f, -d / 2 - VillageGallery + .1f), .035f, SootIron);
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
            if (m.Moss)
            {
                var moss = new GameObject("Roof moss").transform; moss.SetParent(holder, false);
                moss.localPosition = m.ridgeAlongX ? new Vector3(m.mossMid / 100, 0, m.ridgeAt / 100) : new Vector3(m.ridgeAt / 100, 0, m.mossMid / 100);
                moss.localRotation = m.ridgeAlongX ? Quaternion.identity : Quaternion.Euler(0, 90, 0);
                var mp = new Batch();
                RoofMoss(mp, m.mossSpan / 100, m.mossHalf * 2 / 100, (m.mossRidge - m.mossEaves) / 100, m.mossEaves / 100, g);
                mp.Flush(this, moss, "Moss");
            }
            // Soot smoke from one chimney in three (on the plot, not under the house's scale).
            if (m.smoke.y > 0 && R() < .34f) Smoke(t, t.InverseTransformPoint(holder.TransformPoint(new Vector3(m.smoke.x / 100, m.smoke.y / 100 + .25f, m.smoke.z / 100))));
            return holder;
        }

        /// <summary>The smith's shop's gear under its front awning, beside the door (the pack's anvil on a stump, its table with a hammer,
        /// its weapon stand with a sword, a spear and a shield, a cart wheel for mending) and its sign over the awning.</summary>
        void SmithyDressing(Transform t, VillagePlace h, float d)
        {
            Vector3 At(float mx, float mz) { return new Vector3(h.x + h.scaleX * (h.model.MidX - mx / 100), h.baseY, h.scale * (h.model.front - mz) / 100 - d / 2); }
            // (model x: the door is on the left, -533..-224, so the gear stands on the right; model z: under the awning, 380..490)
            ModelProp(t, VillageKit + "Stump_A_Standard", At(150, 440), 0, .55f);
            var anvil = ModelProp(t, VillageKit + "Anvil_Standard", At(150, 440) + Vector3.up * .55f, 90, .34f);
            ModelProp(t, VillageKit + "Blacksmith_Table_Standard", At(300, 445), 180, .86f);
            ModelProp(t, VillageKit + "Hammer_Standard", At(300, 445) + Vector3.up * .86f, 30, .12f);
            ModelProp(t, VillageKit + "Weapon_Stand_Standard", At(20, 470), 180, 1.28f);
            ModelProp(t, VillageKit + "Sword_Standard", At(-30, 470) + Vector3.up * .1f, 180, 1.06f);
            ModelProp(t, VillageKit + "Spear_Standard", At(60, 470) + Vector3.up * .05f, 180, 1.8f);
            ModelProp(t, VillageKit + "Shield_Standard", At(390, 470), 200, .63f);
            ModelProp(t, VillageKit + "Wood_Tyre_Standard", At(-150, 480), 170, 1.0f);
            ModelProp(t, VillageKit + "Blacksmith_Logo_Standard", At(-600, 400) + Vector3.up * 1.05f, 180, .8f);   // on the wall left of the door
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
