using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Forge District (Docs/ZONE_FORGE_DISTRICT.md; mobs 19-22, the cap 20; no dungeon). The place is CANON (book1 ch.1:147, ch.3:7,
    /// ch.7:237-239, ch.9:5, ch.16:131; the author's map, maps/forge_district_map.png: the Spoke in the middle, the River Aureum and the
    /// Molten Vein, the Great Crucibles and the Fire Pits); every building's look and plan here is GAME-ONLY and the zone's own (no other
    /// zone's houses or inn kit). The palette: soot-black stone, iron roofs, ember orange and molten gold.
    /// - The kinds (<see cref="ForgeProp"/>): foundry halls (long soot-stone halls, their arches glowing), furnaces (bottle kilns of
    ///   brick), the Great Crucibles (a stone bowl on a stepped plinth, molten at the brim), fire pits, slag heaps, smelters' lodgings
    ///   (low stone rows under iron roofs), the district wall and its gate, molten channels behind stone kerbs and the stone bridges over
    ///   them, cinder lamps, ingot stacks, the Spoke (a rune-carved black monolith; never entered) and the Spoke's walled yard.
    /// - The ground (<see cref="ForgeGround"/>, <see cref="ForgeEdge"/>): soot and slag-black, cinders, no grass; the zone's edge is the
    ///   district wall, not woods; past it (<see cref="ForgeBackdrop"/>) chimney stacks in the smog and the Crown far off.
    /// - No machines anywhere (Chris, 2026-10-11; the map's cog-wheels, chains and hanging ladles are left out): the bellows are worked by
    ///   hand, the crucibles poured by ladle and hand, loads carried.
    /// Each kind draws only from a stream of its own keyed on where it stands, so nothing else in the zone moves for it.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The Forge District's prop kinds (built by <see cref="ForgeProp"/>).</summary>
        public static readonly HashSet<string> ForgeKinds = new HashSet<string> {
            "foundry_hall", "furnace", "crucible", "fire_pit", "slag_heap", "lodging_row", "forge_wall", "forge_gate", "molten_channel",
            "forge_bridge", "cinder_lamp", "ingot_stack", "the_spoke", "spoke_yard" };
        /// <summary>Whether this zone is the Forge District's ground (biome "forge").</summary>
        bool Forged { get { return Zone != null && Zone.biome == "forge"; } }

        // ---------- the palette ----------
        static readonly Color[] ForgeStones = { new Color(.2f, .185f, .18f), new Color(.24f, .21f, .19f), new Color(.17f, .16f, .16f), new Color(.27f, .23f, .2f) };
        Material ForgeStone(int variant) { return Dressed(ForgeStones[Mathf.Abs(variant) % ForgeStones.Length]); }
        Material KilnBrick { get { return Dressed(new Color(.42f, .24f, .16f)); } }
        Material IronRoof { get { return Tint(art.metal, new Color(.2f, .19f, .19f)); } }
        Material ForgeIron { get { return Tint(art.metal, new Color(.13f, .12f, .12f)); } }
        Material SlagBlack { get { return RockTint(new Color(.13f, .12f, .12f)); } }
        Material SpokeStone { get { return RockTint(new Color(.09f, .085f, .09f)); } }
        Material Ember(float strength = 1.6f) { return Glowing(new Color(1, .45f, .12f), strength); }
        Material Molten { get { return Glowing(new Color(1f, .62f, .16f), 2.2f); } }
        Material RuneGlow { get { return Glowing(new Color(1f, .38f, .1f), 2.4f); } }
        static readonly Color ForgeFire = new Color(1, .5f, .18f);
        System.Random ForgeRng(ZoneProp p) { return new System.Random(Zone.seed ^ (Mathf.RoundToInt(p.at.x * 8) * 73856093) ^ (Mathf.RoundToInt(p.at.y * 8) * 19349663) ^ 0x0F0F6E); }

        /// <summary>A Forge District building's size across its front (x) and front to back (y): the data's, else the kind's own.</summary>
        public static Vector2 ForgeSize(ZoneProp p)
        {
            if (p.size.x > 0 && p.size.y > 0) return p.size;
            switch (p.kind)
            {
                case "foundry_hall": return new Vector2(p.size.x > 0 ? p.size.x : 26, 14);
                case "lodging_row": return new Vector2(p.size.x > 0 ? p.size.x : 18, 7);
                case "furnace": return Vector2.one * (p.size.x > 0 ? p.size.x : 7);
                case "crucible": return Vector2.one * (p.size.x > 0 ? p.size.x : 16);
                case "the_spoke": return Vector2.one * (p.size.x > 0 ? p.size.x : 14);
                case "fire_pit": return Vector2.one * (p.size.x > 0 ? p.size.x : 8);
                case "slag_heap": return Vector2.one * (p.size.x > 0 ? p.size.x : 9);
                default: return p.size;
            }
        }
        /// <summary>Whether a kind is one of the Forge District's roofed or solid buildings (a footprint: no rain, no node, no station in it).</summary>
        public static bool ForgeBuilding(string kind) { return kind == "foundry_hall" || kind == "lodging_row" || kind == "furnace" || kind == "crucible" || kind == "the_spoke"; }
        /// <summary>Half-extents of a Forge District building's footprint, a little over its walls; zero for anything else.</summary>
        public static Vector2 ForgeFootprint(ZoneProp p) { return ForgeBuilding(p.kind) ? ForgeSize(p) / 2 + Vector2.one * .3f : Vector2.zero; }

        /// <summary>One prop of the Forge District's kinds on its root (Root: on the ground at the prop's place, turned, scaled). False for
        /// a kind that is none of them.</summary>
        bool ForgeProp(Transform t, ZoneProp p)
        {
            if (!ForgeKinds.Contains(p.kind)) return false;
            var r = ForgeRng(p);
            switch (p.kind)
            {
                case "foundry_hall": FoundryHall(t, ForgeSize(p), p.variant, r); break;
                case "furnace": Furnace(t, ForgeSize(p).x, p.variant, r); break;
                case "crucible": Crucible(t, ForgeSize(p).x); break;
                case "fire_pit": FirePit(t, ForgeSize(p).x, r); break;
                case "slag_heap": SlagHeap(t, ForgeSize(p).x, r); break;
                case "lodging_row": LodgingRow(t, ForgeSize(p), p.variant, r); break;
                case "forge_wall": ForgeWall(t, p.size.x > 0 ? p.size.x : 40, p.size.y > 0 ? p.size.y : 9, r); break;
                case "forge_gate": ForgeGate(t, p.size.x > 0 ? p.size.x : 8); break;
                case "molten_channel": MoltenChannel(t, p.points, p.size.x > 0 ? p.size.x : 8); break;
                case "forge_bridge": ForgeBridge(t, p.size.x > 0 ? p.size.x : 14, p.size.y > 0 ? p.size.y : 6); break;
                case "cinder_lamp": CinderLamp(t); break;
                case "ingot_stack": IngotStack(t, r); break;
                case "the_spoke": TheSpoke(t, ForgeSize(p).x, p.size.y > 0 ? p.size.y : 48, r); break;
                case "spoke_yard": SpokeYard(t, p.size.x > 0 ? p.size.x : 30); break;
            }
            return true;
        }

        /// <summary>A solid piece of its own (a kerb, a wall bay): a box with its own collider, kept off the navmesh.</summary>
        GameObject SolidPiece(Transform t, Vector3 at, Vector3 size, Material m, Quaternion? rot = null)
        {
            var o = BoxPart(t, at, size, m, rot); o.AddComponent<BoxCollider>(); o.AddComponent<NavBlocker>(); return o;
        }

        // ---------- the buildings ----------
        /// <summary>A foundry hall (front -z): a long soot-stone hall nine metres to the eaves, buttressed, its front a row of tall arches
        /// glowing with the furnaces inside (shut by iron grilles: not entered), an iron roof and two stacks smoking.</summary>
        void FoundryHall(Transform t, Vector2 size, int variant, System.Random r)
        {
            float w = size.x, d = size.y, H = 9; var stone = ForgeStone(variant); var parts = new Batch();
            float drop = FootDrop(t, w, d);
            BoxPart(t, new Vector3(0, (H - drop) / 2, 0), new Vector3(w, H + drop, d), stone, null, 2);
            Solid(t, new Vector3(0, H / 2, 0), new Vector3(w, H, d));
            MeshPart(ZoneMeshes.GableRoof(w + .8f, d + .8f, 3.2f, .3f, .3f), t, new Vector3(0, H, 0), IronRoof);
            int bays = Mathf.Max(2, Mathf.RoundToInt(w / 5)); float bay = w / bays; var glow = Ember(1.4f);
            for (int b = 0; b < bays; b++)
            {
                float x = -w / 2 + bay * (b + .5f);
                parts.Box(glow, new Vector3(x, 3.2f, -d / 2 - .03f), new Vector3(bay * .5f, 5, .06f));            // the arch's glow
                parts.Box(ForgeIron, new Vector3(x, 3.2f, -d / 2 - .1f), new Vector3(.12f, 5, .1f));              // its grille's bars
                parts.Box(ForgeIron, new Vector3(x, 1.8f, -d / 2 - .1f), new Vector3(bay * .5f, .1f, .1f));
                parts.Box(ForgeIron, new Vector3(x, 4.4f, -d / 2 - .1f), new Vector3(bay * .5f, .1f, .1f));
                parts.Box(stone, new Vector3(x, 6.1f, -d / 2 - .2f), new Vector3(bay * .62f, .7f, .4f));          // the arch's head
                if (b > 0) parts.Box(stone, new Vector3(-w / 2 + bay * b, (H - drop) / 2 - .5f, -d / 2 - .5f), new Vector3(.9f, H - 1 + drop, 1));   // a buttress
            }
            parts.Flush(this, t, "Foundry hall");
            foreach (float sx in new[] { -.3f, .3f })
            {
                float sh = 7 + (float)r.NextDouble() * 4;
                BoxPart(t, new Vector3(sx * w, H + 1 + sh / 2, d * .15f), new Vector3(1.8f, sh, 1.8f), KilnBrick, null, 2);
                Smoke(t, new Vector3(sx * w, H + 1.4f + sh, d * .15f));
            }
            Glow(t, new Vector3(0, 3, -d / 2 - 2.5f), 14, 1.4f, ForgeFire, 2.2f);
        }
        /// <summary>A furnace: a bottle kiln of brick on a stone ring, its mouth glowing at the foot (front -z), smoke from its neck.</summary>
        void Furnace(Transform t, float size, int variant, System.Random r)
        {
            float rad = size / 2, H = 10 + variant % 3 * 2;
            var profile = new[] { new Vector2(rad, -.6f), new Vector2(rad, 2.4f), new Vector2(rad * .96f, 4.2f), new Vector2(rad * .62f, H * .78f), new Vector2(rad * .34f, H), new Vector2(rad * .3f, H) };
            MeshPart(PropMesh("Kiln " + rad.ToString("F1") + " " + H, () => Turned(profile, 14, 2)), t, Vector3.zero, KilnBrick);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .25f, 0), new Vector3(size + .6f, .3f, size + .6f), ForgeStone(variant));
            BoxPart(t, new Vector3(0, 1, -rad + .05f), new Vector3(1.4f, 1.5f, .3f), Ember(2f));
            BoxPart(t, new Vector3(0, 1.95f, -rad - .05f), new Vector3(1.9f, .35f, .5f), ForgeStone(variant + 1));
            Solid(t, new Vector3(0, 3, 0), new Vector3(size * .9f, 6, size * .9f));
            Smoke(t, new Vector3(0, H + .4f, 0));
            Glow(t, new Vector3(0, 1.2f, -rad - 1.5f), 9, 1.2f, ForgeFire, 2);
        }
        /// <summary>A Great Crucible (the author's map): a great stone bowl on a three-step plinth, molten metal at its brim, a pouring lip
        /// on its -z side over a glowing spill-trough. Poured by ladle and hand; nothing turns it.</summary>
        void Crucible(Transform t, float size)
        {
            float rad = size / 2; var stone = ForgeStone(2);
            for (int s = 0; s < 3; s++) Part(PrimitiveType.Cylinder, t, new Vector3(0, .4f + s * .8f, 0), new Vector3(size + 3 - s * 1.4f, .4f, size + 3 - s * 1.4f), ForgeStone(s));
            var bowl = new[] { new Vector2(rad * .45f, 2.4f), new Vector2(rad * .8f, 3.6f), new Vector2(rad, 5.6f), new Vector2(rad, 6.2f), new Vector2(rad * .9f, 6.2f), new Vector2(rad * .88f, 5.9f) };
            MeshPart(PropMesh("Crucible bowl " + rad.ToString("F1"), () => Turned(bowl, 20, 2)), t, Vector3.zero, ForgeIron);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 5.8f, 0), new Vector3(rad * 1.78f, .05f, rad * 1.78f), Molten);
            BoxPart(t, new Vector3(0, 5.5f, -rad - .8f), new Vector3(1.6f, .4f, 2), ForgeIron, Quaternion.Euler(-20, 0, 0));
            BoxPart(t, new Vector3(0, 2.6f, -rad - 1.3f), new Vector3(.9f, 5.4f, .12f), Molten);   // the pour, a sheet of light
            BoxPart(t, new Vector3(0, .5f, -rad - 3.2f), new Vector3(3, 1, 3), stone);               // the trough's stone
            BoxPart(t, new Vector3(0, 1.02f, -rad - 3.2f), new Vector3(2.2f, .05f, 2.2f), Molten);
            Solid(t, new Vector3(0, 3, 0), new Vector3(size + 2, 6, size + 2));
            Glow(t, new Vector3(0, 7.5f, 0), 22, 1.8f, ForgeFire, 2.6f);
            Glow(t, new Vector3(0, 2, -rad - 3.5f), 10, 1.2f, ForgeFire, 2);
            Smoke(t, new Vector3(rad * .3f, 6.6f, 0)); Smoke(t, new Vector3(-rad * .3f, 6.6f, rad * .2f));
        }
        /// <summary>A fire pit: a round stone ring a metre high round a bed of glowing coals; nobody stands in it.</summary>
        void FirePit(Transform t, float size, System.Random r)
        {
            float rad = size / 2; var stone = ForgeStone(1); int n = Mathf.Max(10, Mathf.RoundToInt(rad * 4.5f));
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n; var at = new Vector3(Mathf.Cos(a) * rad, .5f, Mathf.Sin(a) * rad);
                SolidPiece(t, at, new Vector3(.9f, 1 + (float)r.NextDouble() * .3f, 2 * Mathf.PI * rad / n + .1f), stone, Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0));
            }
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .2f, 0), new Vector3(size - .8f, .05f, size - .8f), Ember(2.2f));
            for (int i = 0; i < 6; i++) Part(PrimitiveType.Sphere, t, new Vector3(((float)r.NextDouble() - .5f) * rad, .3f, ((float)r.NextDouble() - .5f) * rad), new Vector3(1.2f, .5f, 1f), SlagBlack);
            Glow(t, new Vector3(0, 1.4f, 0), 12, 1.6f, ForgeFire, 2.4f);
            Smoke(t, new Vector3(0, .8f, 0));
        }
        /// <summary>A slag heap: black lumps piled higher than a man, ember glowing in the cracks between them.</summary>
        void SlagHeap(Transform t, float size, System.Random r)
        {
            float rad = size / 2; var black = SlagBlack;
            Lump(BoulderAt(r.Next(6)), t, new Vector3(0, .9f, 0), new Vector3(size, 2.6f, size * .85f), black, (float)r.NextDouble() * 360);
            for (int i = 0; i < 5; i++)
            {
                float a = (float)r.NextDouble() * Mathf.PI * 2, d = rad * (.4f + (float)r.NextDouble() * .4f);
                Lump(BoulderAt(r.Next(6)), t, new Vector3(Mathf.Cos(a) * d, .5f, Mathf.Sin(a) * d), new Vector3(2.2f, 1.4f, 1.8f), black, (float)r.NextDouble() * 360);
            }
            for (int i = 0; i < 4; i++) Part(PrimitiveType.Cube, t, new Vector3(((float)r.NextDouble() - .5f) * rad, 1.2f + (float)r.NextDouble(), ((float)r.NextDouble() - .5f) * rad), new Vector3(.12f, .5f, 1.6f), Ember(1.8f), Quaternion.Euler(0, (float)r.NextDouble() * 180, 30));
            var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, 1, 0); cap.radius = rad * .7f; cap.height = 2.6f; cap.direction = 1;
            if (t.GetComponent<NavBlocker>() == null) t.gameObject.AddComponent<NavBlocker>();
        }
        /// <summary>Smelters' lodgings (front -z): a low row of stone cottages one storey and a loft high under one long iron roof, a door
        /// every four metres or so with a stone step, small shuttered windows, a forge-glow in one window in three. Never the Lowtowns'
        /// tenements. Style by variant (the stone).</summary>
        void LodgingRow(Transform t, Vector2 size, int variant, System.Random r)
        {
            float w = size.x, d = size.y, H = 4.6f; var stone = ForgeStone(variant + 1); var parts = new Batch();
            float drop = FootDrop(t, w, d);
            BoxPart(t, new Vector3(0, (H - drop) / 2, 0), new Vector3(w, H + drop, d), stone, null, 2);
            Solid(t, new Vector3(0, H / 2, 0), new Vector3(w, H, d));
            MeshPart(ZoneMeshes.GableRoof(w + .6f, d + 1, 2.6f, .2f, .25f), t, new Vector3(0, H, 0), IronRoof);
            int doors = Mathf.Max(2, Mathf.RoundToInt(w / 4.2f)); float bay = w / doors; var shutter = Tint(art.timber, new Color(.16f, .12f, .1f));
            for (int i = 0; i < doors; i++)
            {
                float x = -w / 2 + bay * (i + .5f);
                parts.Box(shutter, new Vector3(x - .6f, 1.05f, -d / 2 - .04f), new Vector3(1, 2.1f, .08f));
                parts.Box(ForgeStone(variant + 2), new Vector3(x - .6f, .1f, -d / 2 - .45f), new Vector3(1.4f, .2f, .8f));
                bool lit = r.NextDouble() < .34;
                parts.Box(lit ? Ember(1.1f) : DarkPane, new Vector3(x + .9f, 1.7f, -d / 2 - .03f), new Vector3(.8f, .7f, .05f));
                parts.Box(shutter, new Vector3(x + .9f, 1.7f, -d / 2 - .07f), new Vector3(.08f, .74f, .06f));
                parts.Box(stone, new Vector3(x, H + 1.3f, d * .1f), new Vector3(.7f, 1.6f, .7f));   // a stub chimney on the ridge
                if (r.NextDouble() < .3) Smoke(t, new Vector3(x, H + 2.3f, d * .1f));
            }
            parts.Flush(this, t, "Lodging row");
        }
        /// <summary>The district wall (along local x, its face -z): soot stone, buttressed every six metres, a walk with merlons on top.</summary>
        void ForgeWall(Transform t, float length, float high, System.Random r)
        {
            var stone = ForgeStone(2); var parts = new Batch();
            BoxPart(t, new Vector3(0, high / 2 - 1, 0), new Vector3(length, high + 2, 2.4f), stone, null, 2);
            Solid(t, new Vector3(0, high / 2, 0), new Vector3(length, high, 2.4f));
            for (float x = -length / 2 + 3; x < length / 2 - 1; x += 6)
            {
                parts.Box(stone, new Vector3(x, high / 2 - 1, -1.6f), new Vector3(1.2f, high + 2, 1));
                parts.Box(stone, new Vector3(x + 1.5f, high + .5f, -.9f), new Vector3(1.4f, 1, .6f));
                parts.Box(stone, new Vector3(x - 1.5f, high + .5f, -.9f), new Vector3(1.4f, 1, .6f));
            }
            parts.Flush(this, t, "Forge wall");
        }
        /// <summary>The Tithe Gate (its way along local z): two square towers of soot stone either side of an open arch of
        /// <paramref name="gap"/> metres, the Copper-Tithe's tally-board and two braziers. Open: the way through is walked.</summary>
        void ForgeGate(Transform t, float gap)
        {
            var stone = ForgeStone(3);
            foreach (int s in new[] { -1, 1 })
            {
                SolidPiece(t, new Vector3(s * (gap / 2 + 2.5f), 6, 0), new Vector3(5, 12, 5), stone);
                Brazier(t, new Vector3(s * (gap / 2 + .6f), 0, -3.2f), null);
            }
            BoxPart(t, new Vector3(0, 10.5f, 0), new Vector3(gap + .2f, 3, 4), stone);
            BoxPart(t, new Vector3(0, 7.6f, -2.05f), new Vector3(gap * .7f, 1.4f, .1f), Tint(art.metal, new Color(.6f, .38f, .2f)));   // the copper tally-board
            Glow(t, new Vector3(0, 4, -4), 12, 1, ForgeFire, 1.8f);
        }
        /// <summary>
        /// A molten channel (the River Aureum, the Molten Vein; the author's map): a ribbon of glowing metal along the prop's points (world
        /// metres), a black crust either side, and waist-high stone kerbs along both banks with their own colliders, open only where a
        /// forge_bridge stands across it. Nobody walks in it; the kerbs keep the navmesh off it.
        /// </summary>
        void MoltenChannel(Transform t, Vector2[] points, float width)
        {
            if (points == null || points.Length < 2) return;
            t.position = Vector3.zero; t.rotation = Quaternion.identity;
            var pts = Densify(points, 1.5f);
            MeshPart(ZoneMeshes.Ribbon(pts, width + 2.4f, q => HeightAt(q.x, q.y) + .03f), t, Vector3.zero, SlagBlack).name = "Channel crust";
            MeshPart(ZoneMeshes.Ribbon(pts, width, q => HeightAt(q.x, q.y) + .06f), t, Vector3.zero, Molten).name = "Molten metal";
            var bridges = new List<ZoneProp>(); foreach (var b in Zone.props) if (b != null && b.kind == "forge_bridge") bridges.Add(b);
            var stone = ForgeStone(1);
            for (int i = 0; i + 1 < pts.Count; i += 2)
            {
                var a = pts[i]; var b = pts[Mathf.Min(i + 2, pts.Count - 1)]; var dir = b - a; float len = dir.magnitude; if (len < .2f) continue;
                var side = new Vector2(-dir.y, dir.x) / len; var mid = (a + b) / 2; var yaw = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y));
                foreach (int s in new[] { -1, 1 })
                {
                    var k = mid + side * s * (width / 2 + 1.5f);
                    bool open = false;
                    foreach (var br in bridges) { float half = (br.size.y > 0 ? br.size.y : 6) / 2 + .2f; if (Vector2.Distance(k, br.at) < Mathf.Sqrt(half * half + (width / 2 + 1.5f) * (width / 2 + 1.5f)) + .3f) { open = true; break; } }
                    if (open) continue;
                    SolidPiece(t, new Vector3(k.x, HeightAt(k.x, k.y) + .55f, k.y), new Vector3(.7f, 1.3f, len + .15f), stone, yaw);
                }
                if (i % 12 == 0) { var g = pts[i]; Glow(t, new Vector3(g.x, HeightAt(g.x, g.y) + 1.5f, g.y), 12, 1.2f, ForgeFire, 2); }
            }
        }
        /// <summary>A stone bridge over a molten channel (its way along local z, <paramref name="length"/> long, <paramref name="width"/>
        /// across): a slab of black stone a hand over the ground and parapets either side with their own colliders.</summary>
        void ForgeBridge(Transform t, float length, float width)
        {
            var stone = ForgeStone(3);
            BoxPart(t, new Vector3(0, .1f, 0), new Vector3(width, .3f, length), stone);
            foreach (int s in new[] { -1, 1 }) SolidPiece(t, new Vector3(s * (width / 2 + .3f), .7f, 0), new Vector3(.6f, 1.4f, length), stone);
            foreach (int s in new[] { -1, 1 }) foreach (int e in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * (width / 2 + .3f), 1.6f, e * length / 2), new Vector3(.8f, .6f, .8f), ForgeStone(2));
        }
        /// <summary>A cinder lamp: an iron post with an iron fire-basket on top, embers in it day and night.</summary>
        void CinderLamp(Transform t)
        {
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 1.6f, 0), new Vector3(.16f, 1.6f, .16f), ForgeIron);
            MeshPart(PropMesh("Fire-basket", () => ZoneMeshes.Cone(.45f, .5f, 8)), t, new Vector3(0, 3.15f, 0), ForgeIron, Quaternion.Euler(180, 0, 0));
            Part(PrimitiveType.Sphere, t, new Vector3(0, 3.3f, 0), new Vector3(.6f, .3f, .6f), Ember(2.2f));
            Solid(t, new Vector3(0, 1.2f, 0), new Vector3(.4f, 2.4f, .4f));
            var l = Glow(t, new Vector3(0, 3.6f, 0), 9, .9f, ForgeFire, 1.8f); l.shadows = LightShadows.None;
        }
        /// <summary>Ingots stacked crosswise on a pallet of planks, waiting to be carried.</summary>
        void IngotStack(Transform t, System.Random r)
        {
            var bar = Tint(art.metal, new Color(.42f, .3f, .22f)); var parts = new Batch();
            parts.Box(Tint(art.timber, new Color(.22f, .16f, .12f)), new Vector3(0, .08f, 0), new Vector3(1.6f, .16f, 1.2f));
            for (int layer = 0; layer < 3 + r.Next(2); layer++)
                for (int i = 0; i < 4; i++)
                {
                    bool across = layer % 2 == 1; float o = (i - 1.5f) * .3f;
                    parts.Box(bar, new Vector3(across ? 0 : o, .26f + layer * .17f, across ? o : 0), across ? new Vector3(1.3f, .15f, .22f) : new Vector3(.22f, .15f, 1.1f));
                }
            parts.Flush(this, t, "Ingot stack");
            Solid(t, new Vector3(0, .5f, 0), new Vector3(1.6f, 1, 1.2f));
        }
        /// <summary>
        /// The Spoke (CANON: "a massive, rune-carved stone monolith", world_bible.md Magic; raised in the Forge District's Foundry, book1
        /// ch.7:237; black and jagged with glowing veins on the author's map): a tapering shaft of black stone <paramref name="height"/> high
        /// on a round three-step plinth, lesser shards leaning in round its foot, runes cut into its faces and glowing ember-orange. Solid;
        /// never entered or climbed (Docs/ZONE_FORGE_DISTRICT.md question 2).
        /// </summary>
        void TheSpoke(Transform t, float size, float height, System.Random r)
        {
            float rad = size / 2; var black = SpokeStone;
            for (int s = 0; s < 3; s++) Part(PrimitiveType.Cylinder, t, new Vector3(0, .35f + s * .7f, 0), new Vector3(size + 10 - s * 3, .35f, size + 10 - s * 3), ForgeStone(2 + s));
            MeshPart(PropMesh("The Spoke " + rad.ToString("F1") + " " + height, () => ZoneMeshes.Spire(rad, height, 7, 3)), t, new Vector3(0, 2.1f, 0), black);
            var glow = RuneGlow; var parts = new Batch();
            for (int f = 0; f < 7; f++)
            {
                float a = (f + .5f) * Mathf.PI * 2 / 7; var rot = Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90, 0);
                for (int k = 0; k < 9; k++)
                {
                    float y = 4 + k * height * .085f, inset = rad * (1 - (y - 2.1f) / height) * .9f;
                    if (inset < .3f) break;
                    parts.Box(glow, new Vector3(Mathf.Cos(a) * inset, y, Mathf.Sin(a) * inset), new Vector3(.14f, 1.2f + (float)r.NextDouble() * 1.6f, .45f), rot * Quaternion.Euler(0, 0, ((float)r.NextDouble() - .5f) * 40));
                }
            }
            parts.Flush(this, t, "The Spoke's runes");
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2 / 8 + (float)r.NextDouble() * .3f, h = height * (.18f + (float)r.NextDouble() * .16f);
                var at = new Vector3(Mathf.Cos(a) * (rad + 1.2f), 1.5f, Mathf.Sin(a) * (rad + 1.2f));
                MeshPart(PropMesh("Spoke shard " + i + " " + h.ToString("F0"), () => ZoneMeshes.Spire(1.2f, h, 5, 3)), t, at, black, Quaternion.Euler(Mathf.Sin(a) * 14, 0, -Mathf.Cos(a) * 14));
            }
            var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, height / 2, 0); cap.radius = rad + 2.2f; cap.height = height; cap.direction = 1;
            if (t.GetComponent<NavBlocker>() == null) t.gameObject.AddComponent<NavBlocker>();
            Glow(t, new Vector3(0, height * .35f, -rad - 3), 28, 1.4f, new Color(1, .42f, .14f), 2.4f);
        }
        /// <summary>The Spoke's yard: a ring wall of soot stone 3.2 m high and <paramref name="radius"/> round, open in a gate on its -z side
        /// (local), with cinder lamps either side of the gate.</summary>
        void SpokeYard(Transform t, float radius)
        {
            var stone = ForgeStone(0); int n = Mathf.Max(24, Mathf.RoundToInt(radius * 1.6f)); float gateHalf = 5;
            for (int i = 0; i < n; i++)
            {
                float a = (i + .5f) * Mathf.PI * 2 / n; var at = new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
                if (at.y < 0 && Mathf.Abs(at.x) < gateHalf) continue;   // the gate
                var world = t.TransformPoint(new Vector3(at.x, 0, at.y)); float y = HeightAt(world.x, world.z) - t.position.y;
                SolidPiece(t, new Vector3(at.x, y + 1.3f, at.y), new Vector3(.9f, 3.4f, 2 * Mathf.PI * radius / n + .15f), stone, Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0));
            }
            foreach (int s in new[] { -1, 1 })
            {
                var lamp = new GameObject("Yard lamp").transform; lamp.SetParent(t, false); lamp.localPosition = new Vector3(s * (gateHalf + 1.2f), 0, -radius - 1.2f);
                CinderLamp(lamp);
            }
        }

        // ---------- the ground and the edge ----------
        /// <summary>The Forge District's ground paint at a point (PaintGround's noises n1..n3): soot-black packed earth, cinder patches,
        /// a rust stain here and there.</summary>
        Color ForgeGround(float x, float z, float n1, float n2, float n3)
        {
            var c = Color.Lerp(new Color(.15f, .14f, .135f), new Color(.23f, .21f, .19f), n1) * (.9f + n2 * .14f + (n3 - .5f) * .12f);
            float cinder = Mathf.Clamp01((Mathf.PerlinNoise(x * .09f + 31, z * .09f + 17) - .55f) * 3);
            c = Color.Lerp(c, new Color(.09f, .085f, .085f), cinder * .75f);
            float rust = Mathf.Clamp01((Mathf.PerlinNoise(x * .05f + 7, z * .05f + 63) - .7f) * 4);
            return Color.Lerp(c, new Color(.3f, .17f, .1f), rust * .5f);
        }
        /// <summary>The Forge District's edge (in place of the woods' ring, BuildForestEdge): every road paved in dark setts, then the
        /// district wall round all four sides where nothing else stands, clear of the roads and the zone's own props.</summary>
        void ForgeEdge()
        {
            var paving = new GameObject("Paving").transform; paving.SetParent(statics, false);
            var setts = ForgeStone(3);
            foreach (var road in Zone.roads)
            {
                if (road == null || road.points == null || road.points.Length < 2) continue;
                var pts = Densify(road.points, 1.25f);
                MeshPart(ZoneMeshes.Ribbon(pts, road.width, q => HeightAt(q.x, q.y) + .05f), paving, Vector3.zero, setts).name = "Setts " + road.name;
            }
            var r = new System.Random(Zone.seed + 5353); int n = 0;
            for (int side = 0; side < 4; side++)
                for (float a = -Half + 4; a < Half - 4; a += 12)
                {
                    float inset = Half - 2.5f;
                    var at = side == 0 ? new Vector2(a + 6, -inset) : side == 1 ? new Vector2(inset, a + 6) : side == 2 ? new Vector2(a + 6, inset) : new Vector2(-inset, a + 6);
                    if (NearRoad(at, 7) || NearProp(at, 6) || InBuilding(at)) continue;
                    var p = new ZoneProp { kind = "forge_wall", at = at, rotation = side == 0 ? 0 : side == 1 ? 270 : side == 2 ? 180 : 90, size = new Vector2(12.2f, 9) };
                    var t = Root(p, statics); t.name = "District wall"; ForgeWall(t, 12.2f, 9 + (float)r.NextDouble() * 2, new System.Random(Zone.seed + 91 * ++n));
                }
        }
        /// <summary>Past the edge (after BuildBackdrop, which leaves the woods out here): dark blocks and tall stacks in rows, some smoking,
        /// and the Crown on its mountain in the north-east sky (CrownSkyline). Visual only: no colliders, one mesh per material.</summary>
        void ForgeBackdrop()
        {
            if (!Forged) return;
            var root = new GameObject("Forge District backdrop").transform; root.SetParent(transform, false);
            var r = new System.Random(Zone.seed + 6363); float R() { return (float)r.NextDouble(); }
            var walls = new List<CombineInstance>(); var stacks = new List<CombineInstance>(); var box = ZoneMeshes.Box(Vector3.one, 2);
            foreach (float row in new[] { 8f, 20, 34, 52 })
                for (int side = 0; side < 4; side++)
                {
                    float edge = Half + row;
                    for (float a = -edge; a < edge; a += 12 + R() * 10)
                    {
                        var q = side == 0 ? new Vector2(a, -edge) : side == 1 ? new Vector2(edge, a) : side == 2 ? new Vector2(a, edge) : new Vector2(-edge, a);
                        float w = 10 + R() * 12, d = 8 + R() * 6, h = 12 + R() * 10; var g = EdgeGround(q, 0);
                        var rot = Quaternion.Euler(0, (side == 0 || side == 2 ? 0 : 90) + (R() - .5f) * 6, 0);
                        walls.Add(new CombineInstance { mesh = box, transform = Matrix4x4.TRS(g + Vector3.up * (h / 2 - 1), rot, new Vector3(w, h + 2, d)) });
                        if (R() < .45f)
                        {
                            float sh = 8 + R() * 10;
                            stacks.Add(new CombineInstance { mesh = box, transform = Matrix4x4.TRS(g + new Vector3((R() - .5f) * w * .5f, h + sh / 2, 0), rot, new Vector3(2.4f, sh, 2.4f)) });
                            if (R() < .4f) Smoke(root, g + new Vector3(0, h + sh + .5f, 0) - root.position);
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
            Show(walls, ForgeStone(2), "Backdrop works"); Show(stacks, KilnBrick, "Backdrop stacks");
            CrownSkyline.Build(transform, Zone, Half);
        }
    }
}
