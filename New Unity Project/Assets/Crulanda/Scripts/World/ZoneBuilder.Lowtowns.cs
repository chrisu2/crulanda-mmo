using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Lowtowns and the Rust Market (Docs/ZONE_LOWTOWNS.md, Phase 9.1; levels 15-20). The place is CANON (world_bible.md Geography:
    /// the sprawling, shadowed districts at the Crown's foot; book1 ch.3: the Rust Market, Sector 4, under the Forge District's wall and
    /// its smog); every building's look and plan here is GAME-ONLY. The palette: soot brick, wet setts, tarred timber, orange lamps.
    /// - The kinds (<see cref="LowtownProp"/>): tenement terraces three and four storeys high with stoops and doors (a shopfront or a blind
    ///   back by style), the warehouse, lane walls with gates, brick bridges, drain mouths of red clay brick (the sewers' brick is CANON,
    ///   book1 ch.9) with the Outfall's grating, the dock with its moored barges (poled, never driven), the barge, the market hall (a long
    ///   covered stall run), the Shard-Burners' dead-house, the Holding House, the town walls and their gates (the Forge District's shut),
    ///   chimney stacks over the wall, lane lamps, cobbles, puddles, washing lines over the lanes, the soup-hall, the toll-house, the
    ///   dyer's vats and the burners' shard racks.
    /// - The ground (<see cref="LowtownGround"/>, <see cref="LowtownEdge"/>): soot-dark setts and black puddles in the paint, the ways paved
    ///   over in wet setts, no grass; the zone's edge is the blind backs of tenements, not woods.
    /// - The backdrop (<see cref="LowtownBackdrop"/>): roofs and stacks past the edge, the Forge District's chimneys to the east, and the
    ///   mountain with the Crown on it, white stone and gold above the smog, in the north-east sky: never reached (CrownSkyline). Past
    ///   them the city's own roofline runs round the horizon in the haze, rising toward the Crown (DistantRanges.CityRing), not a wood.
    /// - The streets' clutter (<see cref="LowtownClutter"/>): crates, barrels, sacks, rubbish, broken hand-carts and planks by the walls,
    ///   bills on the blind ends, puddles, washing lines across the narrow ways, more lamp posts, the market stalls' stock.
    /// - No machines anywhere (Chris, 2026-10-11): no pipes, cranes, lifts, gondolas or engines. The barges are poled, the bellows
    ///   hand-worked, loads carried.
    /// Each kind draws only from a stream of its own keyed on where it stands, so nothing else in the zone moves for it.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        // ---------- the palette ----------
        static readonly Color[] Bricks = { new Color(.36f, .22f, .17f), new Color(.31f, .21f, .18f), new Color(.4f, .26f, .2f), new Color(.27f, .23f, .21f), new Color(.34f, .25f, .22f), new Color(.29f, .19f, .15f) };
        /// <summary>Soot-darkened brick: the masonry in a brick's tint (one of six).</summary>
        Material SootBrick(int variant) { return Dressed(Bricks[Mathf.Abs(variant) % Bricks.Length]); }
        /// <summary>The sewers' red clay brick (CANON, book1 ch.9:47), a little cleaner where the water runs.</summary>
        Material ClayBrick { get { return Dressed(new Color(.5f, .25f, .17f)); } }
        Material WetSetts { get { return Dressed(new Color(.33f, .325f, .33f)); } }
        Material TarredTimber { get { return Tint(art.timber, new Color(.13f, .11f, .1f)); } }
        Material LowSlate { get { return Tint(art.slate, new Color(.3f, .3f, .33f)); } }
        Material SootIron { get { return Tint(art.metal, new Color(.16f, .15f, .15f)); } }
        Material BlackWater { get { return Tint(art.metal, new Color(.05f, .05f, .06f)); } }
        Material DarkPane { get { return Tint(art.timber, new Color(.06f, .06f, .07f)); } }
        static readonly Color LampOrange = new Color(1, .56f, .24f);
        /// <summary>The street lamps' light: "the sickly, yellow light of the street lamps" (CANON, book1 ch.3:45).</summary>
        static readonly Color LampSickly = new Color(.98f, .86f, .4f);
        /// <summary>Shard-light: violet, as the Council's geodes glow (CANON-EXPANDED, book1 ch.3:133: "a rhythmic, sickly, terrifying violet
        /// light"); the poor's spent shards burn the same colour, dimmer.</summary>
        static readonly Color ShardViolet = new Color(.72f, .42f, 1f);
        /// <summary>A lit window: most burn a fire (the orange fires of book1 ch.1:191), one in five a shard (violet).</summary>
        Material FireWindow { get { return Glowing(new Color(1, .58f, .26f), 1.25f); } }
        Material ShardWindow { get { return Glowing(ShardViolet, 1.1f); } }
        /// <summary>An inn's roof: thatch on the even variants and slate on the odd, as it was; slate always in the Lowtowns (the Rusty Spoon).</summary>
        Material InnRoof(int variant) { return Zone.biome == "lowtowns" ? LowSlate : variant % 2 == 0 ? art.thatch : art.slate; }
        /// <summary>A stream of its own for a prop, keyed on where it stands (the zone's stream is never touched).</summary>
        System.Random LowRng(ZoneProp p) { return new System.Random(Zone.seed ^ (Mathf.RoundToInt(p.at.x * 8) * 73856093) ^ (Mathf.RoundToInt(p.at.y * 8) * 19349663) ^ 0x10717); }

        /// <summary>A Lowtowns building's size across its front (x) and front to back (y): the data's, else the kind's own.</summary>
        public static Vector2 LowtownSize(ZoneProp p)
        {
            if (p.size.x > 0 && p.size.y > 0) return p.size;
            switch (p.kind)
            {
                case "tenement": return new Vector2(p.size.x > 0 ? p.size.x : 16, 8);
                case "warehouse": return new Vector2(24, 14);
                case "dead_house": return new Vector2(16, 11);
                case "holding_house": return new Vector2(14, 10);
                case "market_hall": return new Vector2(p.size.x > 0 ? p.size.x : 24, 6);
                case "soup_hall": return new Vector2(10, 16);
                case "toll_house": return new Vector2(5, 4);
                default: return p.size;
            }
        }
        /// <summary>Half-extents of a Lowtowns building's footprint, a little over its walls (the rain's roof and the camera's fade).</summary>
        Vector2 LowtownFootprint(ZoneProp p) { return LowtownSize(p) / 2 + Vector2.one * .3f; }

        /// <summary>One prop of the Lowtowns' kinds on its root (Root: on the ground at the prop's place, turned, scaled).</summary>
        void LowtownProp(Transform t, ZoneProp p)
        {
            var r = LowRng(p);
            switch (p.kind)
            {
                case "tenement": Tenement(t, LowtownSize(p), p.variant, p.style, r, !string.IsNullOrEmpty(p.name) && p.name != p.kind); break;
                case "warehouse": Warehouse(t, LowtownSize(p), p.variant); break;
                case "lane_wall": LaneWall(t, p.size.x > 0 ? p.size.x : 10, p.size.y > 0 ? p.size.y : 2.6f, p.variant, r); break;
                case "brick_bridge": BrickBridge(t, p.size.x > 0 ? p.size.x : 22, p.size.y > 0 ? p.size.y : 6); break;
                case "drain_mouth": DrainMouth(t, p.size.x > 0 ? p.size.x : 3, p.variant); break;
                case "dock": Dock(t, p.size.x > 0 ? p.size.x : 30, p.size.y > 0 ? p.size.y : 5, p.variant, r); break;
                case "barge": Barge(t, p.size.x > 0 ? p.size.x : 9, p.variant, r); break;
                case "market_hall": MarketHall(t, LowtownSize(p), p.variant, r); break;
                case "dead_house": DeadHouse(t, LowtownSize(p), r); break;
                case "holding_house": HoldingHouse(t, LowtownSize(p)); break;
                case "town_wall": TownWall(t, p.size.x > 0 ? p.size.x : 40, p.size.y > 0 ? p.size.y : 10, p.variant); break;
                case "chimney_stack": ChimneyStack(t, p.size.y > 0 ? p.size.y : 30, p.variant); break;
                case "lane_lamp": LaneLamp(t, p.variant); break;
                case "cobbles": Cobbles(t, p.size.x > 0 ? p.size : new Vector2(10, 10)); break;
                case "puddle": Puddle(t, p.size.x > 0 ? p.size.x : 1.2f, r); break;
                case "washing_line": WashingLine(t, p.size.x > 0 ? p.size.x : 8, p.size.y > 0 ? p.size.y : 6.2f, r); break;
                case "soup_hall": SoupHall(t, LowtownSize(p)); break;
                case "toll_house": TollHouse(t, LowtownSize(p)); break;
                case "dye_vat": DyeVats(t, r); break;
                case "shard_rack": ShardRack(t, p.size.x > 0 ? p.size.x : 3, r); break;
            }
        }

        // ---------- the buildings ----------
        /// <summary>
        /// A terrace of tenements (front -z): soot brick three or four storeys high (variant: odd four, even three; the brick by variant/2),
        /// a band at each floor, windows on every floor front and back (some lit, most dark: the poor burn shards a little at a time), a
        /// door every five metres or so on a stoop, a slate roof with chimney stacks along the ridge (one smoking in three). Style "shop":
        /// a shop window and a hanging board by the middle door; "back": a blind back with no doors (the zone's edge). A named terrace's
        /// middle door is a home for a household (ZoneDoor "house", as a cottage's).
        /// </summary>
        void Tenement(Transform t, Vector2 size, int variant, string style, System.Random r, bool named)
        {
            float w = size.x, d = size.y; int storeys = 3 + Mathf.Abs(variant) % 2;
            const float floor = 3, plinth = .5f; float H = plinth + storeys * floor;
            var brick = SootBrick(variant / 2 + (int)(r.NextDouble() * 2)); var band = SootBrick(3); var frame = TarredTimber;
            var parts = new Batch();   // the small parts merged, one mesh a material (a terrace is many: 150 boxes would be 150 objects)
            float drop = FootDrop(t, w, d);
            BoxPart(t, new Vector3(0, (H - drop) / 2, 0), new Vector3(w, H + drop, d), brick, null, 2);   // down to the lowest ground under it
            Solid(t, new Vector3(0, H / 2, 0), new Vector3(w, H, d));
            for (int s = 1; s <= storeys; s++)   // a string course at each floor and a cornice under the eaves
                foreach (int sz in new[] { -1, 1 }) parts.Box(band, new Vector3(0, plinth + s * floor - .1f, sz * (d / 2 + .06f)), new Vector3(w + .1f, s == storeys ? .32f : .16f, .14f));
            int bays = Mathf.Max(2, Mathf.RoundToInt(w / 2.6f)); float bay = w / bays;
            var lit = FireWindow; var dark = DarkPane;
            for (int s = 0; s < storeys; s++)
                for (int b = 0; b < bays; b++)
                {
                    float x = -w / 2 + bay * (b + .5f), y = plinth + s * floor + 1.55f;
                    foreach (int sz in new[] { -1, 1 })
                    {
                        if (style == "back" && sz < 0 && s == 0) continue;
                        bool door = sz < 0 && s == 0 && style != "back" && (b % 2 == 0 || bays <= 3);
                        if (door) continue;
                        bool glow = r.NextDouble() < (s == 0 ? .3f : .2f);   // a few lit: the poor burn their shards a little at a time
                        bool shard = glow && r.NextDouble() < .2f;   // a fire in most, a shard's violet in one in five (book1 ch.1:191; ch.3:133)
                        parts.Box(glow ? (shard ? ShardWindow : lit) : dark, new Vector3(x, y, sz * (d / 2 + .02f)), new Vector3(.8f, 1.2f, .06f));
                        parts.Box(band, new Vector3(x, y - .68f, sz * (d / 2 + .07f)), new Vector3(1.05f, .1f, .16f));   // the sill
                        parts.Box(frame, new Vector3(x, y + .66f, sz * (d / 2 + .05f)), new Vector3(1.0f, .14f, .12f));   // the lintel
                    }
                }
            // Doors on stoops of setts, every other bay along the front; the middle one a home.
            var doorWood = new[] { Tint(art.timber, new Color(.22f, .14f, .1f)), Tint(art.timber, new Color(.16f, .18f, .2f)), Tint(art.timber, new Color(.26f, .12f, .09f)) };
            if (style != "back")
            {
                int middle = bays / 2; if (middle % 2 != 0 && bays > 3) middle--;
                for (int b = 0; b < bays; b++)
                {
                    if (!(b % 2 == 0 || bays <= 3)) continue;
                    float x = -w / 2 + bay * (b + .5f); bool shop = style == "shop" && b == middle;
                    var wood = doorWood[(int)(r.NextDouble() * doorWood.Length)];
                    parts.Box(wood, new Vector3(x, plinth + 1.1f, -d / 2 - .04f), new Vector3(1.05f, 2.2f, .07f));
                    parts.Box(SootIron, new Vector3(x + .3f, plinth + 1.05f, -d / 2 - .09f), new Vector3(.1f, .1f, .04f));   // the latch
                    parts.Box(frame, new Vector3(x, plinth + 2.3f, -d / 2 - .06f), new Vector3(1.3f, .16f, .14f));
                    float g = Mathf.Min(LocalGround(t, x, -d / 2 - .6f), 0);
                    parts.Box(WetSetts, new Vector3(x, g + (plinth - g) / 2 - .02f, -d / 2 - .45f), new Vector3(1.6f, plinth - g, .9f));
                    parts.Box(WetSetts, new Vector3(x, (plinth * .5f + g) / 2, -d / 2 - .95f), new Vector3(1.6f, plinth * .5f - g + .02f, .2f));
                    if (shop)
                    {
                        // A shopfront: a wide window of small panes, its board hung out on an iron bracket.
                        foreach (int side in new[] { -1, 1 }) parts.Box(lit, new Vector3(x + side * 1.9f, plinth + 1.3f, -d / 2 - .05f), new Vector3(2, 1.5f, .08f));
                        Rod(t, new Vector3(x + .9f, plinth + 2.9f, -d / 2), new Vector3(x + .9f, plinth + 2.9f, -d / 2 - 1.1f), .05f, SootIron);
                        Part(PrimitiveType.Cube, t, new Vector3(x + .9f, plinth + 2.45f, -d / 2 - .8f), new Vector3(.06f, .7f, .9f), Tint(art.timber, new Color(.3f, .22f, .12f)));
                    }
                    if (named && b == middle) Doors.Add(new ZoneDoor { name = t.name, openable = false, position = t.TransformPoint(new Vector3(x, 1, -d / 2 - 1.35f)) });
                }
            }
            // The roof: slate, pitched along the terrace, with stacks along the ridge (one smoking in three).
            float roofH = Mathf.Max(2, d * .42f);
            MeshPart(ZoneMeshes.GableRoof(w + .4f, d + .8f, roofH, .22f, .2f), t, new Vector3(0, H, 0), LowSlate);
            RoofMoss(parts, w + .4f, d + .8f, roofH, H, r);
            foreach (int sx in new[] { -1, 1 }) MeshPart(ZoneMeshes.Gable(d, roofH, .3f, 2), t, new Vector3(sx * (w / 2 - .15f), H, 0), brick, Quaternion.Euler(0, 90, 0));
            for (float x = -w / 2 + 2.2f; x < w / 2 - 1; x += 5.5f)
            {
                parts.Box(brick, new Vector3(x, H + roofH * .7f, 0), new Vector3(1.1f, roofH * 1.4f + .8f, .9f));
                parts.Box(band, new Vector3(x, H + roofH * 1.4f + .45f, 0), new Vector3(1.25f, .12f, 1.05f));
                if (r.NextDouble() < .33f) Smoke(t, new Vector3(x, H + roofH * 1.4f + .9f, 0));
            }
            parts.Flush(this, t, "Terrace");
        }
        /// <summary>
        /// The neon moss on a slate roof (CANON, book1 ch.7:17: "a sprawling, chaotic sea of slate rooftops, vibrant, unnatural neon moss"):
        /// on most roofs one to three patches lying on the slope (a gable roof <paramref name="w"/> by <paramref name="d"/>, its ridge
        /// <paramref name="roofH"/> over the eaves at <paramref name="eaves"/>), sick bright green that glows a little after dark.
        /// </summary>
        void RoofMoss(Batch parts, float w, float d, float roofH, float eaves, System.Random r)
        {
            if (r.NextDouble() > .62f) return;
            float hd = d / 2, slope = Mathf.Atan2(roofH, hd) * Mathf.Rad2Deg; var moss = NeonMoss;
            for (int k = 0, n = 1 + (int)(r.NextDouble() * 3); k < n; k++)
            {
                int side = r.NextDouble() < .5f ? -1 : 1; float u = .2f + (float)r.NextDouble() * .6f, z = side * hd * u;
                float pw = 1.2f + (float)r.NextDouble() * 3.2f, pd = .8f + (float)r.NextDouble() * 1.6f, x = ((float)r.NextDouble() - .5f) * (w - pw - .6f);
                // Lying on its side of the roof, falling toward the eaves.
                var tilt = Quaternion.Euler(side * slope, 0, 0);
                parts.Box(moss, new Vector3(x, eaves + roofH * (1 - u) + .035f, z), new Vector3(pw, .04f, Mathf.Min(pd, hd * .7f)), tilt);
            }
        }
        /// <summary>Small boxes gathered by material and joined into one mesh each (a terrace's sills, lintels, panes and stoops).</summary>
        sealed class Batch
        {
            static Mesh unit;
            readonly Dictionary<Material, List<CombineInstance>> by = new Dictionary<Material, List<CombineInstance>>();
            public void Box(Material m, Vector3 at, Vector3 size, Quaternion? rot = null)
            {
                if (unit == null) unit = ZoneMeshes.Box(Vector3.one, 1);
                if (!by.TryGetValue(m, out var list)) by[m] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = unit, transform = Matrix4x4.TRS(at, rot ?? Quaternion.identity, size) });
            }
            /// <summary>Any mesh (a cylinder, a ball) placed by <paramref name="m"/> into the same joined meshes.</summary>
            public void AddMesh(Mesh mesh, Material mat, Matrix4x4 m)
            {
                if (!by.TryGetValue(mat, out var list)) by[mat] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = mesh, transform = m });
            }
            public void Flush(ZoneBuilder z, Transform t, string name)
            {
                foreach (var kv in by)
                {
                    var mesh = new Mesh { name = name + " parts", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.CombineMeshes(kv.Value.ToArray(), true, true); mesh.RecalculateBounds();
                    z.MeshPart(mesh, t, Vector3.zero, kv.Key);
                }
                by.Clear();
            }
        }
        /// <summary>The Iron Teeth's warehouse (CANON faction, book1 ch.7): a long brick block two tall storeys high with a loading door
        /// of planks, iron-banded and shut, small barred windows high up, a slate roof. Loads go in on backs and barrows.</summary>
        void Warehouse(Transform t, Vector2 size, int variant)
        {
            float w = size.x, d = size.y, H = 9; var brick = SootBrick(variant + 1);
            float drop = FootDrop(t, w, d);
            BoxPart(t, new Vector3(0, (H - drop) / 2, 0), new Vector3(w, H + drop, d), brick, null, 2); Solid(t, new Vector3(0, H / 2, 0), new Vector3(w, H, d));
            var plank = Tint(art.timber, new Color(.2f, .15f, .1f));
            foreach (float x in new[] { -w / 4, w / 4 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(x, 2, -d / 2 - .05f), new Vector3(3.4f, 4, .1f), plank);
                foreach (float y in new[] { 1, 3 }) Part(PrimitiveType.Cube, t, new Vector3(x, y, -d / 2 - .11f), new Vector3(3.5f, .14f, .04f), SootIron);
                Part(PrimitiveType.Cube, t, new Vector3(x, 4.15f, -d / 2 - .1f), new Vector3(4, .3f, .2f), TarredTimber);
            }
            for (float x = -w / 2 + 2; x < w / 2 - 1; x += 3)
                foreach (int sz in new[] { -1, 1 })
                {
                    Part(PrimitiveType.Cube, t, new Vector3(x, 7, sz * (d / 2 + .02f)), new Vector3(.9f, .7f, .06f), DarkPane);
                    for (int k = -1; k <= 1; k++) Part(PrimitiveType.Cube, t, new Vector3(x + k * .25f, 7, sz * (d / 2 + .06f)), new Vector3(.04f, .7f, .04f), SootIron);
                }
            MeshPart(ZoneMeshes.GableRoof(w + .5f, d + .8f, d * .3f, .22f, .2f), t, new Vector3(0, H, 0), LowSlate);
            foreach (int sx in new[] { -1, 1 }) MeshPart(ZoneMeshes.Gable(d, d * .3f, .3f, 2), t, new Vector3(sx * (w / 2 - .15f), H, 0), brick, Quaternion.Euler(0, 90, 0));
            IronTeethSign(t, w + .5f, d + .8f, d * .3f, H);
        }
        /// <summary>
        /// The Iron Teeth's mark painted broad on the warehouse's slate (CANON, book1 ch.7:21: "the jagged Iron Teeth symbol painted broadly on
        /// the slate roof in faded yellow ochre"), on both slopes so the High Rows and the market see it: a band with a row of jagged teeth
        /// standing up from it, flat on the slope. Paint only.
        /// </summary>
        void IronTeethSign(Transform t, float w, float d, float roofH, float eaves)
        {
            var ochre = Tint(art.cloth, new Color(.62f, .48f, .2f)); var parts = new Batch();
            float hd = d / 2, slope = Mathf.Atan2(roofH, hd) * Mathf.Rad2Deg, run = Mathf.Sqrt(hd * hd + roofH * roofH), span = w * .7f;
            foreach (int side in new[] { -1, 1 })
            {
                // Lying on this slope: x along the ridge, up the slope from the eaves.
                var tilt = Quaternion.Euler(side * slope, 0, 0);
                Vector3 On(float x, float up) { float u = up / run; return new Vector3(x, eaves + roofH * u + .03f, side * hd * (1 - u)); }
                parts.Box(ochre, On(0, run * .32f), new Vector3(span, .02f, run * .1f), tilt);
                int teeth = Mathf.Max(5, Mathf.RoundToInt(span / 1.6f));
                for (int k = 0; k < teeth; k++)
                {
                    float x = -span / 2 + span * (k + .5f) / teeth;
                    // Each tooth a diamond turned on the slope, its lower half under the band: a jagged point standing up the roof.
                    var turn = tilt * Quaternion.Euler(0, 45 + (k % 2) * 8, 0);
                    parts.Box(ochre, On(x, run * .4f), new Vector3(span / teeth * .62f, .02f, span / teeth * .62f), turn);
                }
            }
            parts.Flush(this, t, "Iron Teeth sign");
        }
        /// <summary>A yard wall of soot brick along local x (<paramref name="length"/> long) with a coping and piers every 3 m; variant 1 has a
        /// gate in the middle, its plank leaf standing half open; variant 2 a gate shut.</summary>
        void LaneWall(Transform t, float length, float high, int variant, System.Random r)
        {
            var brick = SootBrick(variant + 4); var cap = SootBrick(3); float gate = variant >= 1 ? 2.4f : 0;
            void Run(float a, float b)
            {
                if (b - a < .2f) return; float mid = (a + b) / 2, len = b - a;
                float low = Mathf.Min(LocalGround(t, a, 0), LocalGround(t, b, 0), LocalGround(t, mid, 0));
                var seg = BoxPart(t, new Vector3(mid, (high + low) / 2, 0), new Vector3(len, high - low, .45f), brick, null, 2);
                var box = seg.AddComponent<BoxCollider>(); box.center = Vector3.zero; box.size = new Vector3(len, high - low, .45f); seg.AddComponent<NavBlocker>();
                Part(PrimitiveType.Cube, t, new Vector3(mid, high + .06f, 0), new Vector3(len + .05f, .12f, .6f), cap);
            }
            if (gate > 0) { Run(-length / 2, -gate / 2); Run(gate / 2, length / 2); } else Run(-length / 2, length / 2);
            for (float x = -length / 2; x <= length / 2 + .01f; x += 3)
            {
                if (gate > 0 && Mathf.Abs(x) < gate / 2 + .1f) continue;
                BoxPart(t, new Vector3(x, high / 2 + .1f, 0), new Vector3(.62f, high + .2f, .62f), brick, null, 1.5f);
            }
            if (gate > 0)
            {
                foreach (int s in new[] { -1, 1 }) BoxPart(t, new Vector3(s * (gate / 2 + .3f), (high + .5f) / 2, 0), new Vector3(.6f, high + .5f, .62f), cap, null, 1.5f);
                var leaf = new GameObject("Gate leaf").transform; leaf.SetParent(t, false); leaf.localPosition = new Vector3(-gate / 2, 0, 0);
                leaf.localRotation = Quaternion.Euler(0, variant == 2 ? 0 : 70 + (float)r.NextDouble() * 30, 0);
                Part(PrimitiveType.Cube, leaf, new Vector3(gate / 2, 1, 0), new Vector3(gate - .05f, 1.8f, .08f), TarredTimber);
                foreach (float y in new[] { .5f, 1.5f }) Part(PrimitiveType.Cube, leaf, new Vector3(gate / 2, y, -.05f), new Vector3(gate - .1f, .1f, .03f), SootIron);
                if (variant == 2) { var shut = leaf.gameObject.AddComponent<BoxCollider>(); shut.center = new Vector3(gate / 2, 1, 0); shut.size = new Vector3(gate, 2, .2f); leaf.gameObject.AddComponent<NavBlocker>(); }
            }
        }
        /// <summary>
        /// A bridge of soot brick over the old river (front to back along local z, <paramref name="length"/> long and
        /// <paramref name="width"/> wide): a shallow arch of deck slabs (walkable), brick parapets with a stone coping, piers at the ends
        /// with a lamp on each. Seated on its banks like the village bridges (SeatBridge).
        /// </summary>
        void BrickBridge(Transform t, float length, float width)
        {
            SeatBridge(t, length);
            int segs = 10; float rise = 1.4f; var brick = SootBrick(0); var cap = Tint(Masonry, new Color(.5f, .48f, .45f));
            for (int i = 0; i < segs; i++)
            {
                float a = (i + .5f) / segs, z = -length / 2 + length * a;
                float y = .15f + Mathf.Sin(a * Mathf.PI) * rise, slope = Mathf.Cos(a * Mathf.PI) * rise * Mathf.PI / length;
                var tilt = Quaternion.Euler(-Mathf.Atan(slope) * Mathf.Rad2Deg, 0, 0); var slab = new Vector3(width, .5f, length / segs + .08f); var block = new Vector3(.45f, 1, length / segs + .08f);
                var deck = BoxPart(t, new Vector3(0, y, z), slab, WetSetts, tilt, 1.5f);
                var col = deck.AddComponent<BoxCollider>(); col.center = Vector3.zero; col.size = slab; deck.AddComponent<NavWalkable>();
                BoxPart(t, new Vector3(0, y - .55f, z), new Vector3(width - .2f, .6f, length / segs + .08f), brick, tilt, 2);   // the arch ring under the deck
                foreach (int s in new[] { -1, 1 })
                {
                    var at = new Vector3(s * (width / 2 + .22f), y + .65f, z);
                    var para = BoxPart(t, at, block, brick, tilt, 1.5f);
                    var wall = para.AddComponent<BoxCollider>(); wall.center = Vector3.zero; wall.size = block;
                    Part(PrimitiveType.Cube, t, at + tilt * new Vector3(0, .55f, 0), new Vector3(.6f, .12f, length / segs + .1f), cap, tilt);
                }
            }
            // Green mould on the arch ring's underside, where the river's damp never dries (book1 ch.6:129), and its glow on the black water.
            var under = new Batch(); var rm = new System.Random(Zone.seed ^ Mathf.RoundToInt(t.position.z * 11)); var mould = DrainMould;
            for (int k = 0; k < 14; k++)
            {
                float a = .15f + (float)rm.NextDouble() * .7f, z = -length / 2 + length * a, y = .15f + Mathf.Sin(a * Mathf.PI) * rise - .87f;
                under.Box(mould, new Vector3(((float)rm.NextDouble() - .5f) * (width - .6f), y, z), new Vector3(.4f + (float)rm.NextDouble() * .9f, .03f, .4f + (float)rm.NextDouble() * 1.2f));
            }
            under.Flush(this, t, "Mould under the arch");
            var glow = new GameObject("Mould light").AddComponent<Light>(); glow.transform.SetParent(t, false); glow.transform.localPosition = new Vector3(0, rise - 1.4f, 0);
            glow.type = LightType.Point; glow.range = 7; glow.intensity = .8f; glow.color = MouldGlow;
            NightLights.Add(new NightLight { light = glow, dayIntensity = 0, nightIntensity = 1 });
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
            {
                var at = new Vector3(sx * (width / 2 + .22f), .7f, sz * (length / 2 + .25f));
                BoxPart(t, at, new Vector3(.75f, 2.1f, .75f), brick, null, 1.5f);
                Part(PrimitiveType.Cube, t, at + new Vector3(0, 1.1f, 0), new Vector3(.9f, .14f, .9f), cap);
                if (sz > 0 == sx > 0) BracketLamp(t, at + new Vector3(0, 1.2f, 0), 0);
            }
        }
        /// <summary>
        /// A drain mouth (front -z): an arch of red clay brick (CANON, book1 ch.9:47) set in a brick face, the culvert black behind it, a
        /// skin of black water on the floor in front; solid across, nobody walks in. Variant 1, the Outfall: twice the span, its mouth
        /// barred with an iron grating, slime-green down the brick and the Forge District's run coming out under it.
        /// </summary>
        void DrainMouth(Transform t, float span, int variant)
        {
            bool outfall = variant == 1; if (outfall) span *= 2;
            float faceW = span + 3, faceH = span * .5f + 2.4f + (outfall ? 1.5f : 0), spring = 1.2f, radius = span / 2;
            var brick = SootBrick(5); var clay = ClayBrick;
            foreach (int s in new[] { -1, 1 }) BoxPart(t, new Vector3(s * (span / 2 + (faceW - span) / 4), faceH / 2, .4f), new Vector3((faceW - span) / 2, faceH, .8f), brick, null, 2);
            BoxPart(t, new Vector3(0, spring + radius + (faceH - spring - radius) / 2 + .2f, .4f), new Vector3(span, faceH - spring - radius, .8f), brick, null, 2);
            Part(PrimitiveType.Cube, t, new Vector3(0, spring / 2 + radius / 2, 1.2f), new Vector3(span, spring + radius, .2f), DarkPane);   // the black of the culvert
            for (int k = 0; k <= 10; k++)   // the arch's ring of clay brick voussoirs
            {
                float a = Mathf.PI * k / 10f; var at = new Vector3(Mathf.Cos(a) * (radius + .2f), spring + Mathf.Sin(a) * (radius + .2f), 0);
                Part(PrimitiveType.Cube, t, at, new Vector3(.4f, .3f, .9f), clay, Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90));
            }
            foreach (int s in new[] { -1, 1 }) BoxPart(t, new Vector3(s * (radius + .2f), spring / 2, 0), new Vector3(.4f, spring, .9f), clay, null, 1);
            Part(PrimitiveType.Cube, t, new Vector3(0, .03f, -1.4f), new Vector3(span + .5f, .03f, 3), BlackWater);
            Solid(t, new Vector3(0, faceH / 2, .4f), new Vector3(faceW, faceH, 1.6f));
            ArchMould(t, new Vector3(0, spring, 0), radius, .9f, new System.Random(Zone.seed ^ Mathf.RoundToInt(t.position.x * 13 + t.position.z * 7)));
            if (outfall)
            {
                for (float x = -radius + .25f; x < radius; x += .35f) Part(PrimitiveType.Cube, t, new Vector3(x, (spring + radius) / 2, -.35f), new Vector3(.07f, spring + radius, .07f), SootIron);
                for (float y = .4f; y < spring + radius; y += .7f) Part(PrimitiveType.Cube, t, new Vector3(0, y, -.4f), new Vector3(span, .06f, .06f), SootIron);
                var slime = Tint(art.stone, new Color(.18f, .24f, .12f));
                for (int s = -1; s <= 1; s += 2) Part(PrimitiveType.Cube, t, new Vector3(s * (radius + .9f), faceH * .35f, -.02f), new Vector3(.5f, faceH * .7f, .04f), slime);
            }
        }
        /// <summary>
        /// Green mould glowing in an arch (CANON, book1 ch.6:129: "the faint, greenish glow of bioluminescent mold clinging to the archways";
        /// ch.9:55): patches round the inside of an arch of <paramref name="radius"/> springing at <paramref name="spring"/> (local, front -z,
        /// <paramref name="deep"/> through), down its jambs and on the wet floor at its mouth, and a dim green light in it that shows after
        /// dark. The Drain Courts' mouths, the bridges' arches.
        /// </summary>
        void ArchMould(Transform t, Vector3 spring, float radius, float deep, System.Random r)
        {
            var parts = new Batch(); var mould = DrainMould;
            for (int k = 0, n = 6 + (int)(r.NextDouble() * 6); k < n; k++)
            {
                float a = Mathf.PI * (float)r.NextDouble(), z = ((float)r.NextDouble() - .5f) * deep, sz = .25f + (float)r.NextDouble() * .5f;
                var at = spring + new Vector3(Mathf.Cos(a) * (radius - .04f), Mathf.Sin(a) * (radius - .04f), z);
                parts.Box(mould, at, new Vector3(sz, .03f, sz * (.6f + (float)r.NextDouble() * .8f)), Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90));
            }
            foreach (int s in new[] { -1, 1 })
                for (int k = 0; k < 2; k++)
                {
                    float h = .3f + (float)r.NextDouble() * spring.y * .8f;
                    parts.Box(mould, spring + new Vector3(s * (radius - .03f), -spring.y + h / 2 + .05f, -deep / 2 + (float)r.NextDouble() * deep), new Vector3(.03f, h, .3f + (float)r.NextDouble() * .4f));
                }
            parts.Flush(this, t, "Mould");
            var l = new GameObject("Mould light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = spring + new Vector3(0, radius * .3f, -.6f);
            l.type = LightType.Point; l.range = 5; l.intensity = .9f; l.color = MouldGlow;
            NightLights.Add(new NightLight { light = l, dayIntensity = 0, nightIntensity = 1.1f });
        }
        /// <summary>
        /// A timber quay along the bank (front -z over the water, <paramref name="length"/> along local x, <paramref name="depth"/> out):
        /// a deck of tarred planks a hand above the bank on posts down into the river, mooring posts with rope, sacks and bales by hand
        /// (no crane: Chris, no machines), and <paramref name="barges"/> barges moored along it.
        /// </summary>
        void Dock(Transform t, float length, float depth, int barges, System.Random r)
        {
            float top = .25f; var plank = Tint(art.timber, new Color(.24f, .19f, .14f));
            var deck = BoxPart(t, new Vector3(0, top - .12f, -depth / 2 + .5f), new Vector3(length, .24f, depth + 1), plank, null, 1);
            var col = deck.AddComponent<BoxCollider>(); col.center = Vector3.zero; col.size = new Vector3(length, .24f, depth + 1); deck.AddComponent<NavWalkable>();
            for (float x = -length / 2 + .4f; x < length / 2; x += .9f) Part(PrimitiveType.Cube, t, new Vector3(x, top + .005f, -depth / 2 + .5f), new Vector3(.03f, .01f, depth + 1), TarredTimber);   // the plank seams
            for (float x = -length / 2 + .3f; x <= length / 2; x += 3)
                foreach (float z in new[] { -depth + .2f, -depth / 2 })
                    Part(PrimitiveType.Cylinder, t, new Vector3(x, top - 2.2f, z), new Vector3(.32f, 2.2f, .32f), TarredTimber);
            for (float x = -length / 2 + 1.5f; x < length / 2; x += 6)
            {
                Part(PrimitiveType.Cylinder, t, new Vector3(x, top + .4f, -depth + .35f), new Vector3(.3f, .45f, .3f), TarredTimber);
                Part(PrimitiveType.Cylinder, t, new Vector3(x, top + .55f, -depth + .35f), new Vector3(.36f, .06f, .36f), Tint(art.hay, new Color(.55f, .48f, .34f)));   // the rope's turns
            }
            var sack = Tint(art.cloth, new Color(.5f, .43f, .32f));
            for (int k = 0; k < Mathf.Max(2, (int)(length / 8)); k++)
            {
                float x = -length / 2 + 2 + (float)r.NextDouble() * (length - 4), z = -depth * (.25f + (float)r.NextDouble() * .3f);
                for (int i = 0; i < 3; i++) Part(PrimitiveType.Capsule, t, new Vector3(x + i * .55f - .55f, top + .25f, z + (i % 2) * .3f), new Vector3(.5f, .28f, .5f), sack, Quaternion.Euler(0, 0, 90));
                Crate(t, new Vector3(x + 1.4f, top + .41f, z - .4f), .82f, (float)r.NextDouble() * 30);
            }
            for (int b = 0; b < barges; b++)
            {
                var hull = new GameObject("Moored barge").transform; hull.SetParent(t, false);
                hull.localPosition = new Vector3(-length / 2 + length * (b + .5f) / Mathf.Max(1, barges), 0, -depth - 2.2f);
                Barge(hull, 9, b % 2, r);
            }
        }
        /// <summary>A poled barge on the water (length along local x): a flat hull of tarred planks, a low house at the stern, bales and
        /// sacks amidships, two long poles laid along it (no sail, no engine: it is poled). Variant 1 is laden higher. It sits at the river's
        /// surface where it is, else on the ground.</summary>
        void Barge(Transform t, float length, int variant, System.Random r)
        {
            var at = t.position; float y = at.y;
            if (WaterAt(new Vector2(at.x, at.z), out float surface, out _)) y = surface - .25f;
            t.position = new Vector3(at.x, y, at.z);
            float beam = 2.6f; var hull = Tint(art.timber, new Color(.16f, .13f, .1f));
            BoxPart(t, new Vector3(0, .25f, 0), new Vector3(length, .6f, beam), hull, null, 1);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * (length / 2 + .35f), .35f, 0), new Vector3(.8f, .45f, beam - .3f), hull, Quaternion.Euler(0, 0, s * 25));
            BoxPart(t, new Vector3(-length / 2 + 1.3f, 1.05f, 0), new Vector3(1.9f, 1.1f, beam - .6f), Tint(art.timber, new Color(.3f, .22f, .15f)), null, 1);
            var bale = Tint(art.hay, new Color(.5f, .45f, .3f)); int loads = variant == 1 ? 6 : 3;
            for (int k = 0; k < loads; k++) Part(PrimitiveType.Cube, t, new Vector3(-.5f + (k % 3) * 1.3f, .8f + (k / 3) * .6f, (k % 2) * .5f - .25f), new Vector3(1.1f, .55f, .8f), k % 2 == 0 ? bale : Tint(art.cloth, new Color(.45f, .38f, .28f)));
            foreach (int s in new[] { -1, 1 }) Rod(t, new Vector3(-length / 2 + .4f, .62f, s * (beam / 2 - .2f)), new Vector3(length / 2 + 1.5f, .9f, s * (beam / 2 - .3f)), .07f, Tint(art.timber, new Color(.45f, .36f, .25f)));
        }
        /// <summary>
        /// The Rust Market's long covered stall run (front -z; <paramref name="size"/>.x along it): tarred posts every three metres under a
        /// lean-to roof of slate, a brick back wall, a counter in each bay with its wares (crates, sacks, jars, rags of dyed cloth) and an
        /// awning of faded cloth over the front. Walked under, not into: only the counters and the back wall are solid.
        /// </summary>
        void MarketHall(Transform t, Vector2 size, int variant, System.Random r)
        {
            float w = size.x, d = size.y, front = 3.3f, back = 4.6f; var brick = SootBrick(variant + 2);
            var backWall = BoxPart(t, new Vector3(0, back / 2, d / 2 - .2f), new Vector3(w, back, .4f), brick, null, 2);
            backWall.AddComponent<BoxCollider>(); backWall.AddComponent<NavBlocker>();
            for (float x = -w / 2; x <= w / 2 + .01f; x += 3)
            {
                Part(PrimitiveType.Cube, t, new Vector3(x, front / 2, -d / 2 + .2f), new Vector3(.24f, front, .24f), TarredTimber);
                Part(PrimitiveType.Cube, t, new Vector3(x, (front + back) / 2 - .2f, 0), new Vector3(.18f, .18f, d), TarredTimber, Quaternion.Euler(Mathf.Atan2(back - front, d) * Mathf.Rad2Deg, 0, 0));
            }
            float slope = Mathf.Atan2(back - front, d) * Mathf.Rad2Deg, run = Mathf.Sqrt(d * d + (back - front) * (back - front));
            var amber = new GameObject("Stall light").AddComponent<Light>(); amber.transform.SetParent(t, false); amber.transform.localPosition = new Vector3(0, front - .9f, 0);
            amber.type = LightType.Point; amber.range = Mathf.Min(14, w * .6f); amber.intensity = 1.2f; amber.color = new Color(1, .62f, .26f);
            NightLights.Add(new NightLight { light = amber, dayIntensity = .25f, nightIntensity = 1.5f });
            Part(PrimitiveType.Cube, t, new Vector3(0, (front + back) / 2 + .05f, 0), new Vector3(w + .6f, .12f, run + .8f), LowSlate, Quaternion.Euler(-slope, 0, 0));
            Color[] dyes = { new Color(.55f, .2f, .14f), new Color(.24f, .3f, .4f), new Color(.5f, .42f, .2f), new Color(.3f, .38f, .24f), new Color(.42f, .26f, .38f) };
            int bays = Mathf.Max(1, Mathf.RoundToInt(w / 3));
            for (int b = 0; b < bays; b++)
            {
                float x = -w / 2 + 3 * (b + .5f);
                var counter = BoxPart(t, new Vector3(x, .5f, d / 2 - 1.6f), new Vector3(2.5f, 1, .9f), Tint(art.timber, new Color(.3f, .22f, .15f)), null, 1);
                counter.AddComponent<BoxCollider>(); counter.AddComponent<NavBlocker>();
                Part(PrimitiveType.Cube, t, new Vector3(x, front - .3f, -d / 2 + .05f), new Vector3(2.8f, .7f, .05f), Tint(art.cloth, dyes[(b + variant) % dyes.Length] * .85f), Quaternion.Euler(-20, 0, 0));
                // A stall-lantern of amber horn under every other bay's awning (GAME-ONLY: the market's own light, warmer than the lamps' sickly yellow).
                if (b % 2 == 0) Part(PrimitiveType.Cube, t, new Vector3(x + .9f, front - .75f, -d / 2 + .5f), new Vector3(.2f, .28f, .2f), Glowing(new Color(1, .64f, .24f), 1.4f));
                for (int k = 0; k < 3; k++)
                {
                    float wx = x - .8f + k * .8f, pick = (float)r.NextDouble();
                    if (pick < .35f) Crate(t, new Vector3(wx, 1.2f, d / 2 - 1.6f), .4f, pick * 90);
                    else if (pick < .65f) Part(PrimitiveType.Capsule, t, new Vector3(wx, 1.18f, d / 2 - 1.6f), new Vector3(.32f, .2f, .32f), Tint(art.cloth, new Color(.5f, .43f, .3f)));
                    else Part(PrimitiveType.Cylinder, t, new Vector3(wx, 1.15f, d / 2 - 1.6f), new Vector3(.22f, .15f, .22f), Tint(art.stone, dyes[(k + b) % dyes.Length]));
                }
            }
        }
        /// <summary>
        /// The Shard-Burners' dead-house (GAME-ONLY reading: the shards are memory sifted out of the foundry's slag, and the Lowtowns burn
        /// their own dead for light a shard at a time; DUNGEON_DESIGN.md): a squat hall of black brick with a tall square flue, its smoke
        /// pale, a wide arched door shut with iron bands, a lamp of pale shard-light over it. Solid.
        /// </summary>
        void DeadHouse(Transform t, Vector2 size, System.Random r)
        {
            float w = size.x, d = size.y, H = 6; var brick = SootBrick(3);
            float drop = FootDrop(t, w, d);
            BoxPart(t, new Vector3(0, (H - drop) / 2, 0), new Vector3(w, H + drop, d), brick, null, 2); Solid(t, new Vector3(0, H / 2, 0), new Vector3(w, H, d));
            MeshPart(ZoneMeshes.GableRoof(w + .6f, d + .8f, 2.6f, .25f, .2f), t, new Vector3(0, H, 0), LowSlate);
            foreach (int sx in new[] { -1, 1 }) MeshPart(ZoneMeshes.Gable(d, 2.6f, .3f, 2), t, new Vector3(sx * (w / 2 - .15f), H, 0), brick, Quaternion.Euler(0, 90, 0));
            BoxPart(t, new Vector3(w / 2 - 2, H + 5, d / 4), new Vector3(2, 12, 2), brick, null, 1.5f);
            var smoke = Smoke(t, new Vector3(w / 2 - 2, H + 11.4f, d / 4)); var main = smoke.main; main.startColor = new Color(.8f, .8f, .82f, .3f);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.6f, -d / 2 - .05f), new Vector3(3, 3.2f, .1f), TarredTimber);
            for (int k = 0; k <= 8; k++) { float a = Mathf.PI * k / 8f; Part(PrimitiveType.Cube, t, new Vector3(Mathf.Cos(a) * 1.75f, 3.2f + Mathf.Sin(a) * .9f, -d / 2 - .1f), new Vector3(.35f, .28f, .3f), SootBrick(2), Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90)); }
            foreach (float y in new[] { .7f, 1.7f, 2.7f }) Part(PrimitiveType.Cube, t, new Vector3(0, y, -d / 2 - .12f), new Vector3(3.05f, .12f, .03f), SootIron);
            ShardLamp(t, new Vector3(0, 4.6f, -d / 2 - .4f));
        }
        /// <summary>The Holding House (GAME-ONLY: where the warrant's children were kept a night): two storeys of grey dressed stone, its
        /// windows barred, a heavy door banded in iron, a lamp over it; the Council's hired warders keep it. Solid.</summary>
        void HoldingHouse(Transform t, Vector2 size)
        {
            float w = size.x, d = size.y, H = 7.2f; var stone = Dressed(new Color(.4f, .39f, .37f));
            float drop = FootDrop(t, w, d);
            BoxPart(t, new Vector3(0, (H - drop) / 2, 0), new Vector3(w, H + drop, d), stone, null, 1.75f); Solid(t, new Vector3(0, H / 2, 0), new Vector3(w, H, d));
            Part(PrimitiveType.Cube, t, new Vector3(0, 3.7f, -d / 2 - .05f), new Vector3(w + .1f, .2f, .14f), SootBrick(3));
            for (float x = -w / 2 + 1.6f; x < w / 2 - 1; x += 2.6f)
                foreach (float y in new[] { 1.9f, 5.4f })
                    foreach (int sz in new[] { -1, 1 })
                    {
                        if (sz < 0 && y < 3 && Mathf.Abs(x) < 1.4f) continue;
                        Part(PrimitiveType.Cube, t, new Vector3(x, y, sz * (d / 2 + .02f)), new Vector3(.7f, .9f, .06f), DarkPane);
                        for (int k = -1; k <= 1; k++) Part(PrimitiveType.Cube, t, new Vector3(x + k * .2f, y, sz * (d / 2 + .07f)), new Vector3(.05f, .95f, .05f), SootIron);
                    }
            PlankDoor(t, new Vector3(0, 1.2f, -d / 2 - .05f), 1.4f, 2.4f, TarredTimber, .1f);
            MeshPart(ZoneMeshes.GableRoof(w + .5f, d + .8f, 2.4f, .25f, .2f), t, new Vector3(0, H, 0), LowSlate);
            foreach (int sx in new[] { -1, 1 }) MeshPart(ZoneMeshes.Gable(d, 2.4f, .3f, 2), t, new Vector3(sx * (w / 2 - .15f), H, 0), stone, Quaternion.Euler(0, 90, 0));
            BracketLamp(t, new Vector3(1.3f, 2.9f, -d / 2 - .1f), 1);
        }
        /// <summary>
        /// A town wall along local x (<paramref name="length"/> long, <paramref name="high"/> tall, 3 m thick): soot brick on a dressed
        /// footing, buttresses every eight metres on its -z face, a walk's parapet on top. Variant 1: a gate in the middle, its two leaves
        /// open (Argentis's outer gate in the Gate Ward); variant 2: the gate shut and barred (the Forge District's, until that zone opens).
        /// Solid but for an open gate's way through.
        /// </summary>
        void TownWall(Transform t, float length, float high, int variant)
        {
            const float thick = 3; float gate = variant >= 1 ? 6 : 0, arch = 6.5f; var brick = SootBrick(variant == 2 ? 3 : 1); var foot = Dressed(new Color(.36f, .35f, .33f));
            void Run(float a, float b)
            {
                if (b - a < .2f) return; float mid = (a + b) / 2, len = b - a, low = Mathf.Min(LocalGround(t, a, 0), LocalGround(t, b, 0), LocalGround(t, mid, 0)) - .3f;
                var seg = BoxPart(t, new Vector3(mid, (high + low) / 2, 0), new Vector3(len, high - low, thick), brick, null, 2);
                var box = seg.AddComponent<BoxCollider>(); box.center = Vector3.zero; box.size = new Vector3(len, high - low, thick); seg.AddComponent<NavBlocker>();
                BoxPart(t, new Vector3(mid, (1.2f + low) / 2, 0), new Vector3(len + .02f, 1.2f - low, thick + .3f), foot, null, 1.75f);
                for (float x = a + .6f; x < b - .3f; x += 1.6f) Part(PrimitiveType.Cube, t, new Vector3(x, high + .45f, -thick / 2 + .3f), new Vector3(.8f, .9f, .6f), brick);   // the parapet's merlons
                for (float x = Mathf.Ceil(a / 8) * 8; x < b - 1; x += 8)
                    if (Mathf.Abs(x) > gate / 2 + 2) BoxPart(t, new Vector3(x, high * .4f, -thick / 2 - .6f), new Vector3(1.4f, high * .8f, 1.2f), brick, null, 2);
            }
            if (gate <= 0) { Run(-length / 2, length / 2); return; }
            Run(-length / 2, -gate / 2 - 2.5f); Run(gate / 2 + 2.5f, length / 2);
            foreach (int s in new[] { -1, 1 })   // the gate's two towers
            {
                var tw = BoxPart(t, new Vector3(s * (gate / 2 + 1.25f), (high + 3) / 2, 0), new Vector3(2.5f, high + 3, thick + 2), brick, null, 2);
                var tb = tw.AddComponent<BoxCollider>(); tb.center = Vector3.zero; tb.size = new Vector3(2.5f, high + 3, thick + 2); tw.AddComponent<NavBlocker>();
                for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, t, new Vector3(s * (gate / 2 + 1.25f) + (k - 1) * .8f, high + 3.4f, -thick / 2 - .6f), new Vector3(.6f, .8f, .6f), brick);
            }
            BoxPart(t, new Vector3(0, arch + (high - arch) / 2, 0), new Vector3(gate, high - arch, thick), brick, null, 2);
            for (int k = 0; k <= 10; k++) { float a = Mathf.PI * k / 10f; Part(PrimitiveType.Cube, t, new Vector3(Mathf.Cos(a) * (gate / 2), arch - gate / 2 * .35f + Mathf.Sin(a) * gate / 2 * .35f, -thick / 2 - .05f), new Vector3(.5f, .35f, .3f), foot, Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90)); }
            var oak = Tint(art.timber, new Color(.2f, .14f, .1f));
            foreach (int s in new[] { -1, 1 })
            {
                var leaf = new GameObject("Gate leaf").transform; leaf.SetParent(t, false); leaf.localPosition = new Vector3(s * gate / 2, 0, -thick / 2 + .1f);
                leaf.localRotation = Quaternion.Euler(0, variant == 2 ? 0 : s * -100, 0);
                Part(PrimitiveType.Cube, leaf, new Vector3(-s * gate / 4, arch / 2 - .3f, 0), new Vector3(gate / 2 - .05f, arch - .7f, .25f), oak);
                foreach (float y in new[] { 1.2f, 3, 4.8f }) Part(PrimitiveType.Cube, leaf, new Vector3(-s * gate / 4, y, -.14f), new Vector3(gate / 2 - .1f, .16f, .04f), SootIron);
            }
            if (variant == 2)
            {
                Part(PrimitiveType.Cube, t, new Vector3(0, 2.6f, -thick / 2 - .25f), new Vector3(gate + .8f, .35f, .3f), TarredTimber);   // the bar across, from the far side's brackets
                var shut = new GameObject("Shut gate").AddComponent<BoxCollider>(); shut.transform.SetParent(t, false); shut.center = new Vector3(0, arch / 2, 0); shut.size = new Vector3(gate, arch, thick); shut.gameObject.AddComponent<NavBlocker>();
            }
            foreach (int s in new[] { -1, 1 }) BracketLamp(t, new Vector3(s * (gate / 2 + 1.25f), 3.4f, -thick / 2 - 1.05f), 1);
        }
        /// <summary>A foundry's chimney stack over the Forge District's wall (a backdrop): square soot brick tapering, a band near the top,
        /// dark smoke rolling off it (Book 1's smog over the Rust Market; never "Blacksmoke Spire"). Not reached; no collider.</summary>
        void ChimneyStack(Transform t, float high, int variant)
        {
            var brick = SootBrick(variant + 3); float b0 = 3.2f, b1 = 2.1f; int steps = 4;
            for (int i = 0; i < steps; i++)
            {
                float a = i / (float)steps, wdt = Mathf.Lerp(b0, b1, a + .5f / steps), h = high / steps;
                BoxPart(t, new Vector3(0, h * (i + .5f), 0), new Vector3(wdt, h + .05f, wdt), brick, null, 2);
            }
            Part(PrimitiveType.Cube, t, new Vector3(0, high - 1.2f, 0), new Vector3(b1 + .5f, .5f, b1 + .5f), SootBrick(3));
            var s = Smoke(t, new Vector3(0, high + .4f, 0)); var main = s.main; main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 4.5f); main.startColor = new Color(.24f, .22f, .21f, .5f); main.startLifetime = 6;
            var em = s.emission; em.rateOverTime = 4; var shape = s.shape; shape.radius = .9f;
        }
        /// <summary>A lane lamp: an iron post with a lantern on a bracket, burning orange after dark (the Lowtowns' lamps by day are out:
        /// variant 1 burns by day too, the inn's and the gate's; variant 2 is snuffed, its glass frosted black inside: the Lantern-Eater's lane).</summary>
        void LaneLamp(Transform t, int variant)
        {
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 1.6f, 0), new Vector3(.14f, 1.6f, .14f), SootIron);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .15f, 0), new Vector3(.4f, .15f, .4f), WetSetts);
            BracketLamp(t, new Vector3(0, 3.1f, 0), variant);
            Solid(t, new Vector3(0, 1.2f, 0), new Vector3(.4f, 2.4f, .4f));
        }
        /// <summary>A lantern on an iron bracket at <paramref name="at"/> (its arm toward -z), with an orange point light the clock drives.</summary>
        void BracketLamp(Transform t, Vector3 at, int variant)
        {
            Rod(t, at, at + new Vector3(0, 0, -.65f), .05f, SootIron);
            var lantern = at + new Vector3(0, -.32f, -.6f);
            MeshPart(PropMesh("Lantern roof", () => ZoneMeshes.Cone(.27f, .2f, 4)), t, lantern + new Vector3(0, .18f, 0), SootIron, Quaternion.Euler(0, 45, 0));
            if (variant == 2) { Part(PrimitiveType.Cube, t, lantern, new Vector3(.26f, .34f, .26f), BlackWater); return; }   // snuffed: no light
            Part(PrimitiveType.Cube, t, lantern, new Vector3(.26f, .34f, .26f), Glowing(LampSickly, 1.5f));
            var l = new GameObject("Lamp light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = lantern + Vector3.down * .1f;
            // "the sickly, yellow light of the street lamps" (book1 ch.3:45)
            l.type = LightType.Point; l.range = 10; l.intensity = 1.3f; l.color = LampSickly;
            NightLights.Add(new NightLight { light = l, dayIntensity = variant > 0 ? .8f : 0, nightIntensity = 1.8f });
        }
        /// <summary>A lamp of shard-light (a burned memory's glow, GAME-ONLY): violet, as the geodes glow (CANON-EXPANDED, book1 ch.3:133), the
        /// Shard-Burners' Yard's own colour.</summary>
        void ShardLamp(Transform t, Vector3 at)
        {
            Rod(t, at + new Vector3(0, .35f, .4f), at + new Vector3(0, .35f, 0), .05f, SootIron);
            Part(PrimitiveType.Cube, t, at, new Vector3(.22f, .3f, .22f), Glowing(ShardViolet, 1.8f), Quaternion.Euler(0, 45, 0));
            var l = new GameObject("Shard light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = at;
            l.type = LightType.Point; l.range = 9; l.intensity = 1.1f; l.color = ShardViolet;
            NightLights.Add(new NightLight { light = l, dayIntensity = .5f, nightIntensity = 1.5f });
        }
        /// <summary>A square of wet setts (<paramref name="size"/>, x across, y along) laid over the ground: a court, a yard, a market's
        /// floor. No collider: the ground under it is walked.</summary>
        void Cobbles(Transform t, Vector2 size)
        {
            var a = t.TransformPoint(new Vector3(-size.x / 2, 0, 0)); var b = t.TransformPoint(new Vector3(size.x / 2, 0, 0));
            var pts = Densify(new[] { new Vector2(a.x, a.z), new Vector2(b.x, b.z) }, 1.25f);
            var go = MeshPart(ZoneMeshes.Ribbon(pts, size.y, q => HeightAt(q.x, q.y) + .05f), statics, Vector3.zero, WetSetts); go.name = "Setts " + t.name;
        }
        /// <summary>A puddle of black water (CANON: "freezing puddles of questionable origin", book1 ch.1:143), a hand over the setts.</summary>
        void Puddle(Transform t, float radius, System.Random r)
        {
            float g = LocalGround(t, 0, 0);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, g + .085f, 0), new Vector3(radius * 2, .004f, radius * (1.1f + (float)r.NextDouble() * .6f)), BlackWater, Quaternion.Euler(0, (float)r.NextDouble() * 180, 0));
        }
        /// <summary>A washing line across a lane (along local x, <paramref name="span"/> wide, <paramref name="high"/> up) from wall to wall:
        /// a sagging rope with shirts, sheets and rags pegged on it, greyed by the smog.</summary>
        void WashingLine(Transform t, float span, float high, System.Random r)
        {
            var rope = Tint(art.hay, new Color(.4f, .37f, .3f)); int n = 8; Vector3 Point(float u) { return new Vector3(-span / 2 + span * u, high - Mathf.Sin(u * Mathf.PI) * .55f, 0); }
            for (int i = 0; i < n; i++) Rod(t, Point(i / (float)n), Point((i + 1) / (float)n), .025f, rope);
            Color[] cloth = { new Color(.62f, .6f, .55f), new Color(.45f, .42f, .38f), new Color(.5f, .32f, .26f), new Color(.34f, .38f, .44f), new Color(.58f, .54f, .4f) };
            for (float u = .1f; u < .92f; u += .08f + (float)r.NextDouble() * .1f)
            {
                var at = Point(u); float wide = .4f + (float)r.NextDouble() * .6f, tall = .5f + (float)r.NextDouble() * .7f;
                Part(PrimitiveType.Cube, t, at + new Vector3(0, -tall / 2, 0), new Vector3(wide, tall, .02f), Tint(art.cloth, cloth[(int)(r.NextDouble() * cloth.Length)]), Quaternion.Euler(0, (float)r.NextDouble() * 20 - 10, 0));
            }
        }
        /// <summary>The soup-hall: a little chapel of no particular god (no device on it), grey stone with a bell-cote and no bell, turned to
        /// feed the Rows (front -z, the nave along z). Solid; Brother Aldo's kettle stands outside it (a cookfire station).</summary>
        void SoupHall(Transform t, Vector2 size)
        {
            float w = size.x, d = size.y, H = 5.5f; var stone = Dressed(new Color(.42f, .4f, .37f));
            float drop = FootDrop(t, w, d);
            BoxPart(t, new Vector3(0, (H - drop) / 2, 0), new Vector3(w, H + drop, d), stone, null, 1.75f); Solid(t, new Vector3(0, H / 2, 0), new Vector3(w, H, d));
            MeshPart(ZoneMeshes.GableRoof(d + .8f, w + .6f, w * .5f, .25f, .2f), t, new Vector3(0, H, 0), LowSlate, Quaternion.Euler(0, 90, 0));
            foreach (int sz in new[] { -1, 1 }) MeshPart(ZoneMeshes.Gable(w, w * .5f, .3f, 2), t, new Vector3(0, H, sz * (d / 2 - .15f)), stone);
            for (float z = -d / 2 + 2.5f; z < d / 2 - 1; z += 3)
                foreach (int sx in new[] { -1, 1 })
                {
                    Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .02f), 2.8f, z), new Vector3(.06f, 2, .7f), DarkPane);
                    Part(PrimitiveType.Cylinder, t, new Vector3(sx * (w / 2 + .02f), 3.8f, z), new Vector3(.7f, .03f, .7f), DarkPane, Quaternion.Euler(0, 0, 90));
                }
            PlankDoor(t, new Vector3(0, 1.3f, -d / 2 - .05f), 1.6f, 2.6f, Tint(art.timber, new Color(.3f, .2f, .13f)));
            BoxPart(t, new Vector3(0, H + w * .5f + 1, -d / 2 + .5f), new Vector3(1.4f, 2, 1), stone, null, 1.5f);   // the bell-cote, empty
            Part(PrimitiveType.Cube, t, new Vector3(0, H + w * .5f + 1.2f, -d / 2 + .5f), new Vector3(.8f, 1, 1.05f), DarkPane);
        }
        /// <summary>The toll-house at the gate: a stone booth with a shuttered hatch toward the road, the toll-book's desk under it, and a
        /// swing-pole on a post (lifted: no toll by warrant; lifted by hand, no machine).</summary>
        void TollHouse(Transform t, Vector2 size)
        {
            float w = size.x, d = size.y, H = 3.2f; var stone = Dressed(new Color(.39f, .37f, .35f));
            BoxPart(t, new Vector3(0, H / 2, 0), new Vector3(w, H, d), stone, null, 1.75f); Solid(t, new Vector3(0, H / 2, 0), new Vector3(w, H, d));
            MeshPart(ZoneMeshes.GableRoof(w + .5f, d + .6f, 1.4f, .2f, .2f), t, new Vector3(0, H, 0), LowSlate);
            foreach (int sx in new[] { -1, 1 }) MeshPart(ZoneMeshes.Gable(d, 1.4f, .3f, 2), t, new Vector3(sx * (w / 2 - .15f), H, 0), stone, Quaternion.Euler(0, 90, 0));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.6f, -d / 2 - .02f), new Vector3(1.6f, 1, .06f), art.glass);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.05f, -d / 2 - .3f), new Vector3(1.8f, .1f, .6f), Tint(art.timber, new Color(.3f, .22f, .15f)));   // the desk
            Part(PrimitiveType.Cube, t, new Vector3(-.3f, 1.13f, -d / 2 - .3f), new Vector3(.5f, .06f, .35f), Tint(art.cloth, new Color(.45f, .2f, .15f)));   // the toll-book
            Stake(t, new Vector3(w / 2 + 1.2f, 0, -d / 2 - 1), .26f, 1.2f, TarredTimber);
            var pole = new GameObject("Toll pole").transform; pole.SetParent(t, false); pole.localPosition = new Vector3(w / 2 + 1.2f, 1.15f, -d / 2 - 1); pole.localRotation = Quaternion.Euler(0, 0, 62);
            Part(PrimitiveType.Cube, pole, new Vector3(-3, 0, 0), new Vector3(6.4f, .14f, .14f), Tint(art.timber, new Color(.5f, .42f, .3f)));
            for (int k = 0; k < 4; k++) Part(PrimitiveType.Cube, pole, new Vector3(-1 - k * 1.4f, 0, 0), new Vector3(.6f, .15f, .15f), Tint(art.cloth, new Color(.55f, .18f, .12f)));
            BracketLamp(t, new Vector3(w / 2 - .4f, 2.6f, -d / 2), 1);
        }
        /// <summary>The dyer's vats in the Rows (GAME-ONLY; the Weavers' station beside them): three coopered vats of madder, woad and weld
        /// on a brick footing, a paddle across one, cloth wrung over a rail. Solid.</summary>
        void DyeVats(Transform t, System.Random r)
        {
            Color[] dyes = { new Color(.48f, .12f, .1f), new Color(.16f, .22f, .38f), new Color(.55f, .48f, .16f) };
            BoxPart(t, new Vector3(0, .15f, 0), new Vector3(4.4f, .3f, 1.6f), SootBrick(4), null, 1.5f);
            for (int k = 0; k < 3; k++)
            {
                var at = new Vector3(-1.4f + k * 1.4f, .3f, 0);
                Part(PrimitiveType.Cylinder, t, at + new Vector3(0, .45f, 0), new Vector3(1.15f, .45f, 1.15f), Tint(art.timber, new Color(.3f, .22f, .15f)));
                Part(PrimitiveType.Cylinder, t, at + new Vector3(0, .88f, 0), new Vector3(1.02f, .02f, 1.02f), Tint(art.stone, dyes[k]));
                foreach (float y in new[] { .25f, .7f }) Part(PrimitiveType.Cylinder, t, at + new Vector3(0, y, 0), new Vector3(1.18f, .03f, 1.18f), SootIron);
            }
            Rod(t, new Vector3(-.8f, 1.2f, -.2f), new Vector3(-.2f, 2.0f, .3f), .05f, Tint(art.timber, new Color(.45f, .36f, .25f)));
            Rod(t, new Vector3(-2.3f, 1.8f, .9f), new Vector3(2.3f, 1.8f, .9f), .06f, TarredTimber);
            foreach (int s in new[] { -1, 1 }) Rod(t, new Vector3(s * 2.3f, 0, .9f), new Vector3(s * 2.3f, 1.85f, .9f), .08f, TarredTimber);
            for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, t, new Vector3(-1.4f + k * 1.4f, 1.35f, .9f), new Vector3(.8f, .9f, .03f), Tint(art.cloth, dyes[(k + 1) % 3] * 1.1f));
            Solid(t, new Vector3(0, .6f, 0), new Vector3(4.4f, 1.2f, 1.6f));
        }
        /// <summary>The Shard-Burners' racks (GAME-ONLY): shelves of tarred timber along local x, rows of small shards in them glowing violet (the
        /// geodes' light, CANON-EXPANDED, book1 ch.3:133), each a burned memory bought back for light. Solid.</summary>
        void ShardRack(Transform t, float length, System.Random r)
        {
            foreach (float x in new[] { -length / 2, length / 2 }) Part(PrimitiveType.Cube, t, new Vector3(x, .9f, 0), new Vector3(.12f, 1.8f, .5f), TarredTimber);
            var glow = Glowing(ShardViolet, 1.3f);
            foreach (float y in new[] { .5f, 1.1f, 1.7f })
            {
                Part(PrimitiveType.Cube, t, new Vector3(0, y, 0), new Vector3(length, .05f, .48f), TarredTimber);
                for (float x = -length / 2 + .2f; x < length / 2 - .1f; x += .22f + (float)r.NextDouble() * .2f)
                    Part(PrimitiveType.Cube, t, new Vector3(x, y + .08f, (float)r.NextDouble() * .2f - .1f), new Vector3(.06f, .11f, .06f), glow, Quaternion.Euler(0, (float)r.NextDouble() * 90, 15));
            }
            Solid(t, new Vector3(0, .9f, 0), new Vector3(length, 1.8f, .5f));
        }

        // ---------- the ground and the edge ----------
        /// <summary>The Lowtowns' ground paint at a point (PaintGround's noises n1..n3): soot-dark setts, damp patches, black puddles.</summary>
        Color LowtownGround(float x, float z, float n1, float n2, float n3)
        {
            var c = Color.Lerp(new Color(.24f, .225f, .21f), new Color(.31f, .29f, .27f), n1) * (.9f + n2 * .14f + (n3 - .5f) * .1f);
            float damp = Mathf.Clamp01((Mathf.PerlinNoise(x * .07f + 13, z * .07f + 91) - .55f) * 3);
            c = Color.Lerp(c, new Color(.17f, .16f, .155f), damp * .7f);
            float pool = Mathf.Clamp01((Mathf.PerlinNoise(x * .21f + 57, z * .21f + 3) - .78f) * 8);
            return Color.Lerp(c, new Color(.08f, .08f, .09f), pool);
        }
        /// <summary>
        /// The Lowtowns' edge (in place of the woods' ring, BuildForestEdge): every road paved over in wet setts, then the blind backs of
        /// tenements along the north and south edges and the east and west where nothing else stands, clear of the roads and the water
        /// and the zone's own props. Each from a stream of its own; the zone's stream is never drawn.
        /// </summary>
        void LowtownEdge()
        {
            BuildLowtownLevels();   // the town built downhill: retaining walls and step-streets (ZoneBuilder.LowtownLevels)
            var paving = new GameObject("Paving").transform; paving.SetParent(statics, false);
            foreach (var road in Zone.roads)
            {
                if (road == null || road.points == null || road.points.Length < 2) continue;
                var pts = Densify(road.points, 1.25f);
                MeshPart(ZoneMeshes.Ribbon(pts, road.width, q => HeightAt(q.x, q.y) + .06f), paving, Vector3.zero, WetSetts).name = "Setts " + road.name;
            }
            var r = new System.Random(Zone.seed + 5151); float R() { return (float)r.NextDouble(); }
            int n = 0;
            for (int side = 0; side < 4; side++)
                for (float a = -Half + 6; a < Half - 6; a += 13 + R() * 4)
                {
                    float w = 11 + R() * 5, d = 8, inset = Half - 7; int v = (int)(R() * 8);
                    var at = side == 0 ? new Vector2(a, -inset) : side == 1 ? new Vector2(inset, a) : side == 2 ? new Vector2(a, inset) : new Vector2(-inset, a);
                    if (NearRoad(at, w / 2 + 3) || Water.NearWater(at, w / 2 + 2) || NearProp(at, w / 2 + 2) || InBuilding(at)) continue;
                    if (OnRampOrStair(at, w / 2 + 2)) continue;   // never across a stair or a ramp up to the edge
                    var p = new ZoneProp { kind = "tenement", at = at, rotation = side == 0 ? 0 : side == 1 ? 90 : side == 2 ? 180 : 270, size = new Vector2(w, d), variant = v, style = "back" };
                    var t = Root(p, statics); t.name = "Tenement backs"; Tenement(t, p.size, v, "back", new System.Random(Zone.seed + 77 * ++n), false);
                }
            LowtownClutter();
        }

        // ---------- the streets' clutter ----------
        /// <summary>Distance from a point to a segment (a point when its ends are one).</summary>
        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }
        /// <summary>How far round a small prop of the data the clutter keeps off (the buildings are InBuilding's); 0 for one it ignores.</summary>
        static float ClutterKeep(ZoneProp p)
        {
            switch (p.kind)
            {
                case "tenement": case "warehouse": case "dead_house": case "holding_house": case "soup_hall": case "toll_house": case "cobbles": case "puddle": case "washing_line": case "barge": case "dock": return 0;
                case "stall": return 2.2f;
                case "market": return p.variant == 0 ? 2 : p.variant == 27 ? 1.3f : 1;
                case "market_hall": return Mathf.Max(p.size.x, p.size.y) / 2 + 1;
                case "lane_lamp": case "signpost": case "brazier": return .7f;
                case "well": case "board": case "crates": case "barrels": case "hitch": case "oven": return 1.6f;
                case "cart": return 2.6f; case "wagon": return 3.2f; case "haystack": return 2.2f;
                case "dye_vat": return 2.6f; case "shard_rack": return (p.size.x > 0 ? p.size.x : 3) / 2 + .6f;
                case "drain_mouth": return (p.size.x > 0 ? p.size.x : 3) * (p.variant == 1 ? 2 : 1) / 2 + 2.5f;
                case "ruin": return Mathf.Max(p.size.x, p.size.y) / 2 + 1.5f;
                case "brick_bridge": return Mathf.Max(p.size.x, p.size.y) / 2 + 1;
                default: return Mathf.Max(3, Mathf.Max(p.size.x, p.size.y) / 2 + 1);
            }
        }
        /// <summary>
        /// The Lowtowns' streets lived in (2026-10-16, Chris: the Rust Market an empty plaza, the streets bare ground between the
        /// tenements): against the terraces' walls, between their doors and down their backs and ends, crates, barrels, sacks, heaps of
        /// rubbish, broken hand-carts and planks leant on the brick; bills pasted on the blind gable ends; puddles in the lanes; washing
        /// lines strung across the narrow ways from wall to wall; more lamp posts where a lane runs dark; and round the market's stalls
        /// their stock. GAME-ONLY dressing, no machines. Visual only (no colliders, nothing in the navmesh) but the lamp posts' thin iron
        /// (a lane lamp's own); nothing on a road, in a building, in the water or the river's bank, nor near a door, a node, a secret, a
        /// station, someone's post, a quest prop or an exit. The small parts are joined into one mesh a material. A stream of its own.
        /// </summary>
        void LowtownClutter()
        {
            var root = new GameObject("Street clutter").transform; root.SetParent(statics, false);
            var r = new System.Random(Zone.seed + 7171); float R() { return (float)r.NextDouble(); }
            var parts = new Batch(); float edge = Half - 12;
            var clear = new List<(Vector2 at, float r)>();
            foreach (var d in Doors) clear.Add((new Vector2(d.position.x, d.position.z), 2.4f));
            foreach (var n in Zone.nodes) if (n != null) clear.Add((n.at, 3));
            foreach (var s in Zone.secrets) if (s != null) clear.Add((s.at, s.radius + 2.5f));
            if (Zone.stations != null) foreach (var s in Zone.stations) if (s != null) clear.Add((s.at, 3));
            if (Zone.life != null && Zone.life.residents != null) foreach (var p in Zone.life.residents) if (p != null && p.at != Vector2.zero) clear.Add((p.at, 2));
            foreach (var e in Zone.exits) if (e != null) { clear.Add((e.at, 10)); clear.Add((e.arrive, 6)); }
            foreach (var i in Interactables) clear.Add((new Vector2(i.position.x, i.position.z), 2.2f));
            if (Zone.spawns != null) { clear.Add((Zone.spawns.player, 5)); clear.Add((Zone.spawns.companion, 3)); clear.Add((Zone.spawns.recovery, 3)); }
            var small = new List<(Vector2 a, Vector2 b, float r)>();
            foreach (var p in Zone.props)
            {
                if (p == null) continue;
                if (p.kind == "lane_wall" || p.kind == "town_wall")
                {
                    float half = (p.size.x > 0 ? p.size.x : p.kind == "town_wall" ? 40 : 10) / 2; var along = Quaternion.Euler(0, p.rotation, 0) * Vector3.right;
                    small.Add((p.at - new Vector2(along.x, along.z) * half, p.at + new Vector2(along.x, along.z) * half, p.kind == "town_wall" ? 4 : 1));
                    continue;
                }
                float k = ClutterKeep(p); if (k > 0) small.Add((p.at, p.at, k));
            }
            var placed = new List<(Vector2 at, float r)>();
            bool Free(Vector2 q, float rad, bool road = false)
            {
                if (Mathf.Abs(q.x) > edge || Mathf.Abs(q.y) > edge) return false;
                if (!road && NearRoad(q, rad + .3f)) return false;
                if (Water.NearWater(q, rad + 1.5f) || InBuilding(q)) return false;
                if (LowSteep(q, rad + .4f)) return false;   // not on a retaining wall's slope or a stair (ZoneBuilder.LowtownLevels)
                for (int k = 0; k < 4; k++) if (InBuilding(q + new Vector2(k < 2 ? (k * 2 - 1) * rad : 0, k >= 2 ? (k * 2 - 5) * rad : 0))) return false;
                foreach (var c in clear) if ((q - c.at).sqrMagnitude < (c.r + rad) * (c.r + rad)) return false;
                foreach (var s in small) if (SegDist(q, s.a, s.b) < s.r + rad) return false;
                foreach (var c in placed) if ((q - c.at).sqrMagnitude < (c.r + rad + .3f) * (c.r + rad + .3f)) return false;
                return true;
            }
            Vector3 G(Vector2 q, float lift = 0) { return root.InverseTransformPoint(new Vector3(q.x, HeightAt(q.x, q.y) + lift, q.y)); }
            var cyl = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx"); var ball = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            var crateWood = Tint(art.timber, new Color(.38f, .29f, .19f)); var batten = Tint(art.timber, new Color(.22f, .16f, .11f)); var stave = Tint(art.timber, new Color(.3f, .21f, .14f));
            var sacking = Tint(art.cloth, new Color(.48f, .41f, .3f)); var grime = Tint(art.stone, new Color(.17f, .16f, .15f)); var ash = Tint(art.stone, new Color(.27f, .25f, .23f));
            Material[] rags = { Tint(art.cloth, new Color(.4f, .22f, .17f)), Tint(art.cloth, new Color(.3f, .32f, .36f)), Tint(art.cloth, new Color(.45f, .4f, .3f)) };
            Material[] bills = { Tint(art.cloth, new Color(.72f, .67f, .55f)), Tint(art.cloth, new Color(.6f, .55f, .44f)), Tint(art.cloth, new Color(.66f, .5f, .4f)) };
            var ink = Tint(art.cloth, new Color(.12f, .11f, .11f));
            void Box(Material m, Vector3 at, Vector3 size, Quaternion rot) { parts.Box(m, at, size, rot); }
            void Shape(Mesh mesh, Material m, Vector3 at, Vector3 scale, Quaternion rot) { parts.AddMesh(mesh, m, Matrix4x4.TRS(at, rot, scale)); }
            // The groups, each round a point on the ground (yaw: its turn; out: away from the wall it stands by).
            void Crates(Vector3 at, Quaternion q)
            {
                int n = 1 + (int)(R() * 3);
                for (int i = 0; i < n; i++)
                {
                    float s = .6f + R() * .3f; bool top = i == 2; var c = at + q * new Vector3(top ? -.1f : (i - .5f) * .95f, top ? .78f + s / 2 : s / 2, (R() - .5f) * .2f);
                    var turn = q * Quaternion.Euler(0, (R() - .5f) * 24, 0);
                    Box(crateWood, c, Vector3.one * s, turn);
                    foreach (int sy in new[] { -1, 1 }) Box(batten, c + turn * new Vector3(0, sy * s * .42f, 0), new Vector3(s + .03f, s * .1f, s + .03f), turn);
                }
            }
            void Barrels(Vector3 at, Quaternion q)
            {
                int n = 1 + (int)(R() * 3);
                for (int i = 0; i < n; i++)
                {
                    var c = at + q * new Vector3((i - (n - 1) / 2f) * .72f, 0, (i % 2) * .3f - .15f);
                    Shape(cyl, stave, c + Vector3.up * .45f, new Vector3(.62f, .45f, .62f), Quaternion.identity);
                    foreach (float y in new[] { .16f, .74f }) Shape(cyl, SootIron, c + Vector3.up * y, new Vector3(.655f, .025f, .655f), Quaternion.identity);
                    Shape(cyl, batten, c + Vector3.up * .905f, new Vector3(.56f, .01f, .56f), Quaternion.identity);
                }
            }
            void Sacks(Vector3 at, Quaternion q)
            {
                int n = 2 + (int)(R() * 3);
                for (int i = 0; i < n; i++)
                {
                    bool up = R() < .4f; var c = at + q * new Vector3((R() - .5f) * 1.1f, up ? .32f : .2f, (R() - .5f) * .6f) + (i == n - 1 && n > 3 ? Vector3.up * .3f : Vector3.zero);
                    Shape(ball, sacking, c, up ? new Vector3(.45f, .64f, .42f) : new Vector3(.75f, .4f, .5f), q * Quaternion.Euler(0, R() * 360, 0));
                }
            }
            void Rubbish(Vector3 at, Quaternion q)
            {
                for (int i = 0, n = 3 + (int)(R() * 4); i < n; i++)
                {
                    var c = at + q * new Vector3((R() - .5f) * 1.4f, 0, (R() - .5f) * .9f); float s = .3f + R() * .45f;
                    Shape(ball, i % 3 == 0 ? ash : grime, c + Vector3.up * s * .2f, new Vector3(s * 1.3f, s * .55f, s), Quaternion.Euler(0, R() * 360, 0));
                }
                for (int i = 0, n = 2 + (int)(R() * 3); i < n; i++)
                {
                    var c = at + q * new Vector3((R() - .5f) * 1.3f, .08f + R() * .15f, (R() - .5f) * .8f);
                    if (i % 2 == 0) Box(batten, c, new Vector3(.9f + R() * .6f, .04f, .14f), Quaternion.Euler((R() - .5f) * 30, R() * 180, (R() - .5f) * 30));
                    else Box(rags[(int)(R() * rags.Length)], c, new Vector3(.45f, .03f, .35f), Quaternion.Euler((R() - .5f) * 40, R() * 180, (R() - .5f) * 40));
                }
            }
            void BrokenCart(Vector3 at, Quaternion q)
            {
                // A hand-cart with a wheel gone: its bed down on the axle's broken end, the shafts on the setts, the wheel left lying by it.
                var tilt = q * Quaternion.Euler(-9, 0, 14); var bed = at + q * new Vector3(0, .42f, 0);
                Box(crateWood, bed, new Vector3(1.2f, .08f, 1.9f), tilt);
                foreach (int s in new[] { -1, 1 }) Box(stave, bed + tilt * new Vector3(s * .6f, .17f, 0), new Vector3(.06f, .3f, 1.95f), tilt);
                Box(stave, bed + tilt * new Vector3(0, .17f, .96f), new Vector3(1.2f, .3f, .06f), tilt);
                foreach (int s in new[] { -1, 1 }) Box(batten, at + q * new Vector3(s * .42f, .22f, -1.55f), new Vector3(.07f, .07f, 1.6f), q * Quaternion.Euler(-11, 0, 0));
                Shape(cyl, batten, at + q * new Vector3(.68f, .5f, .1f), new Vector3(1, .045f, 1), q * Quaternion.Euler(0, 0, 90));
                Shape(cyl, batten, at + q * new Vector3(-1.25f, .05f, .55f), new Vector3(1, .045f, 1), q * Quaternion.Euler(0, R() * 90, 0));
            }
            void Planks(Vector3 wallFoot, Quaternion q)
            {
                // Planks and a door off its hinges leant on the brick (q: its z away from the wall).
                for (int i = 0, n = 2 + (int)(R() * 3); i < n; i++)
                {
                    float len = 2 + R() * 1.2f, lean = 12 + R() * 8;
                    var foot = wallFoot + q * new Vector3((i - n / 2f) * .32f + (R() - .5f) * .1f, 0, .1f + Mathf.Sin(lean * Mathf.Deg2Rad) * len);
                    var rot = q * Quaternion.Euler(-lean, (R() - .5f) * 8, 0);
                    Box(i % 3 == 2 ? batten : stave, foot + rot * new Vector3(0, len / 2, 0), new Vector3(.24f + R() * .1f, len, .04f), rot);
                }
            }
            void Bills(Vector3 onWall, Quaternion q)
            {
                // Bills pasted on a blind wall (q: its z out of the wall), torn and overlapping: a warrant's, a fight's, a lost child's.
                for (int i = 0, n = 2 + (int)(R() * 4); i < n; i++)
                {
                    var c = onWall + q * new Vector3((R() - .5f) * 2.4f, 1.3f + R() * 1.1f, .03f + i * .004f); var turn = q * Quaternion.Euler(0, 0, (R() - .5f) * 10);
                    var bill = bills[(int)(R() * bills.Length)]; float w = .45f + R() * .3f, h = .6f + R() * .35f;
                    Box(bill, c, new Vector3(w, h, .01f), turn);
                    Box(ink, c + turn * new Vector3(0, h * .3f, .006f), new Vector3(w * .7f, h * .12f, .004f), turn);
                    Box(ink, c + turn * new Vector3(0, -h * .05f, .006f), new Vector3(w * .55f, h * .05f, .004f), turn);
                }
            }
            void Puddle(Vector2 q, float rad)
            {
                Shape(cyl, BlackWater, G(q, .085f), new Vector3(rad * 2, .004f, rad * (1.1f + R() * .6f)), Quaternion.Euler(0, R() * 180, 0));
            }
            // Against the terraces: between the doors in front, down the backs, at the ends; bills on the blind gable ends.
            int groups = 0, billed = 0;
            foreach (var p in Zone.props)
            {
                if (p == null || p.kind != "tenement") continue;
                var size = LowtownSize(p); float w = size.x, d = size.y; var q = Quaternion.Euler(0, p.rotation, 0);
                Vector2 At(float x, float z) { var o = q * new Vector3(x, 0, z); return p.at + new Vector2(o.x, o.z); }
                int bays = Mathf.Max(2, Mathf.RoundToInt(w / 2.6f)); float bay = w / bays;
                var spots = new List<(Vector2 at, Quaternion face, float x)>();
                if (p.style != "back" && bays > 3) for (int b = 1; b < bays; b += 2) spots.Add((At(-w / 2 + bay * (b + .5f), -d / 2 - 1), q * Quaternion.Euler(0, 180, 0), 0));
                for (float x = -w / 2 + 2; x < w / 2 - 1; x += 3.5f + R() * 2.5f) spots.Add((At(x, d / 2 + 1), q, 0));
                foreach (int s in new[] { -1, 1 }) spots.Add((At(s * (w / 2 + 1), (R() - .5f) * d * .5f), q * Quaternion.Euler(0, s * 90, 0), s));
                foreach (var s in spots)
                {
                    if (R() > .62f) continue;
                    float pick = R(), rad = pick < .12f ? 1.6f : .9f;
                    var ground = s.at; if (!Free(ground, rad)) continue;
                    var at = G(ground); var face = s.face;
                    // Leant on the wall, or set against it: the wall is a metre behind each spot (its -z in face).
                    if (pick < .12f) BrokenCart(at + face * new Vector3(0, 0, .5f), face * Quaternion.Euler(0, 70 + R() * 40, 0));
                    else if (pick < .3f) Crates(at, face);
                    else if (pick < .47f) Barrels(at, face);
                    else if (pick < .62f) Sacks(at, face);
                    else if (pick < .82f) Rubbish(at, face);
                    else Planks(at + face * new Vector3(0, 0, -.65f), face);
                    placed.Add((ground, rad)); groups++;
                }
                // Bills on a blind end wall that stands free (no terrace against it).
                foreach (int s in new[] { -1, 1 })
                {
                    if (R() > .45f) continue;
                    var foot = At(s * (w / 2 + .6f), 0); if (InBuilding(foot)) continue;
                    var wall = At(s * w / 2, (R() - .5f) * d * .3f); Bills(G(wall), q * Quaternion.Euler(0, s * 90, 0)); billed++;
                }
            }
            // Round the market's stalls and the stall runs, their stock on the setts.
            foreach (var p in Zone.props)
            {
                if (p == null || (p.kind != "stall" && p.kind != "market_hall")) continue;
                var q = Quaternion.Euler(0, p.rotation, 0); float reach = p.kind == "stall" ? 2.9f : Mathf.Max(p.size.x, 24) / 2 + 1.5f;
                for (int k = 0; k < (p.kind == "stall" ? 3 : 5); k++)
                {
                    float x = (R() < .5f ? -1 : 1) * reach * (p.kind == "stall" ? 1 : .2f + R() * .8f), z = p.kind == "stall" ? (R() - .3f) * 1.4f : -(p.size.y > 0 ? p.size.y : 6) / 2 - 1.4f;
                    var o = q * new Vector3(x, 0, z); var ground = p.at + new Vector2(o.x, o.z);
                    // Free of everything but the stall it serves.
                    bool own = true; foreach (var s in small) if (s.a != p.at && SegDist(ground, s.a, s.b) < s.r + .8f) { own = false; break; }
                    if (!own || NearRoad(ground, 1.1f) || InBuilding(ground)) continue;
                    bool ok = true; foreach (var c in clear) if ((ground - c.at).sqrMagnitude < (c.r + .8f) * (c.r + .8f)) { ok = false; break; }
                    foreach (var c in placed) if ((ground - c.at).sqrMagnitude < (c.r + 1.1f) * (c.r + 1.1f)) { ok = false; break; }
                    if (!ok) continue;
                    var at = G(ground); var face = q * Quaternion.Euler(0, R() * 360, 0); float pick = R();
                    if (pick < .35f) Crates(at, face); else if (pick < .6f) Sacks(at, face); else if (pick < .85f) Barrels(at, face); else Rubbish(at, face);
                    placed.Add((ground, .8f)); groups++;
                }
            }
            // Puddles in the lanes and on the setts (on the roads too: a puddle is walked through).
            int puddles = 0;
            for (int tries = 0; tries < 900 && puddles < 70; tries++)
            {
                var q = new Vector2((R() * 2 - 1) * edge, (R() * 2 - 1) * edge); float rad = .7f + R() * 1.1f;
                if (InBuilding(q) || Water.NearWater(q, rad + 2) || LowSteep(q, rad + .3f)) continue;
                bool ok = true; foreach (var c in clear) if ((q - c.at).sqrMagnitude < c.r * c.r) { ok = false; break; }
                if (!ok) continue;
                Puddle(q, rad); puddles++;
            }
            parts.Flush(this, root, "Street clutter");
            // Washing lines across the narrow ways: from a terrace's front or back to the wall facing it, 4 to 9 m over.
            int lines = 0; var strung = new List<Vector2>();
            foreach (var p in Zone.props)
            {
                if (p == null || p.kind != "tenement" || lines >= 24) continue;
                var size = LowtownSize(p); var q = Quaternion.Euler(0, p.rotation, 0);
                foreach (int sz in new[] { -1, 1 })
                {
                    if (R() > .35f) continue;
                    var outV = q * new Vector3(0, 0, sz); var o2 = new Vector2(outV.x, outV.z); var c3 = q * new Vector3((R() - .5f) * size.x * .5f, 0, sz * size.y / 2); var from = p.at + new Vector2(c3.x, c3.z);
                    float span = 0; for (float s = 3.5f; s <= 9.5f; s += .5f) if (InBuilding(from + o2 * s)) { span = s; break; }
                    if (span <= 0) continue;
                    var mid = from + o2 * (span / 2); if (strung.Exists(m => (m - mid).sqrMagnitude < 14 * 14) || Water.NearWater(mid, 2) || LowSteep(mid, span / 2)) continue;
                    bool near = false; foreach (var c in clear) if ((mid - c.at).sqrMagnitude < c.r * c.r) { near = true; break; }
                    if (near) continue;
                    var t = new GameObject("Washing line").transform; t.SetParent(root, false);
                    t.position = new Vector3(mid.x, HeightAt(mid.x, mid.y), mid.y); t.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, outV));
                    WashingLine(t, span, 5.4f + R() * 2.2f, r); strung.Add(mid); lines++;
                }
            }
            // Lamp posts where a lane runs dark: on its verge, none within 22 m of another.
            var lamps = Zone.props.Where(p => p != null && p.kind == "lane_lamp").Select(p => p.at).ToList(); int posts = 0;
            foreach (var road in Zone.roads)
            {
                if (road == null || road.points == null || road.points.Length < 2) continue;
                var pts = Densify(road.points, 4f);
                for (int i = 2; i < pts.Count - 2 && posts < 20; i += 3)
                {
                    var dir = (pts[i + 1] - pts[i - 1]).normalized; var side = new Vector2(-dir.y, dir.x) * ((i / 3) % 2 == 0 ? 1 : -1);
                    var at = pts[i] + side * (road.width / 2 + .7f);
                    if (lamps.Exists(l => (l - at).sqrMagnitude < 22 * 22) || NearRoad(at, .3f) || !Free(at, .5f, true)) continue;
                    var t = new GameObject("Lane lamp").transform; t.SetParent(root, false); t.position = new Vector3(at.x, HeightAt(at.x, at.y), at.y); t.rotation = Quaternion.LookRotation(new Vector3(side.x, 0, side.y));
                    LaneLamp(t, 0); lamps.Add(at); placed.Add((at, .5f)); posts++;
                }
            }
            Debug.Log("Lowtowns clutter: " + groups + " groups, " + billed + " walls billed, " + puddles + " puddles, " + lines + " washing lines, " + posts + " lamp posts.");
        }
        /// <summary>
        /// Past the edge (after BuildBackdrop, which leaves the Lowtowns' woods out): rows of roofs and stacks on the backdrop's skirt, the
        /// Forge District's taller blocks and chimneys to the east, and the Crown on its mountain in the north-east sky (CrownSkyline).
        /// Visual only: no colliders, one mesh per material, a stream of its own.
        /// </summary>
        void LowtownBackdrop()
        {
            if (Zone.biome != "lowtowns") return;
            var root = new GameObject("Lowtowns backdrop").transform; root.SetParent(transform, false);
            var r = new System.Random(Zone.seed + 6161); float R() { return (float)r.NextDouble(); }
            var walls = new List<CombineInstance>(); var roofs = new List<CombineInstance>(); var box = ZoneMeshes.Box(Vector3.one, 2);
            var gable = ZoneMeshes.GableRoof(1, 1, 1, .02f, 0);
            foreach (float row in new[] { 6f, 16, 28, 42, 58 })
                for (int side = 0; side < 4; side++)
                {
                    float edge = Half + row;
                    for (float a = -edge; a < edge; a += 9 + R() * 8)
                    {
                        var q = side == 0 ? new Vector2(a, -edge) : side == 1 ? new Vector2(edge, a) : side == 2 ? new Vector2(a, edge) : new Vector2(-edge, a);
                        float w = 8 + R() * 8, d = 7 + R() * 4, h = (side == 1 ? 11 : 9) + R() * (side == 1 ? 6 : 7);
                        var g = EdgeGround(q, 0); float yaw = side == 0 || side == 2 ? 0 : 90;
                        var rot = Quaternion.Euler(0, yaw + (R() - .5f) * 6, 0);
                        walls.Add(new CombineInstance { mesh = box, transform = Matrix4x4.TRS(g + Vector3.up * (h / 2 - 1), rot, new Vector3(w, h + 2, d)) });
                        roofs.Add(new CombineInstance { mesh = gable, transform = Matrix4x4.TRS(g + Vector3.up * (h + 1), rot, new Vector3(w + .4f, d * .4f, d + .6f)) });
                        if (R() < (side == 1 ? .5f : .25f))
                        {
                            // The Forge District's stacks over the east wall: 6 to 12 m over the roofs (they were 20 to 45 m, some 70 m up: the smog
                            // swallowed their feet and their tops hung from the top of the view like roofs upside down, 2026-10-17).
                            float sh = side == 1 ? 6 + R() * 6 : 3 + R() * 3;
                            walls.Add(new CombineInstance { mesh = box, transform = Matrix4x4.TRS(g + new Vector3((R() - .5f) * w * .6f, h + sh / 2, 0), rot, new Vector3(side == 1 ? 2.6f : 1, sh, side == 1 ? 2.6f : 1)) });
                            if (side == 1 && R() < .5f) Smoke(root, g + new Vector3(0, h + sh + .5f, 0) - root.position);
                        }
                    }
                }
            void Show(List<CombineInstance> parts, Material m, string name)
            {
                if (parts.Count == 0) return;
                var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.CombineMeshes(parts.ToArray(), true, true);
                var go = new GameObject(name); go.transform.SetParent(root, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Show(walls, SootBrick(1), "Backdrop walls"); Show(roofs, LowSlate, "Backdrop roofs");
            CrownSkyline.Build(transform, Zone, Half);
        }
    }
}
