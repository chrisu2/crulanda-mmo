using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Hallow's Creek's lived-in detail (2026-10-16, Chris on the first frames: a half-done look is not accepted), GAME-ONLY dressing on
    /// CANON places (Docs/DUNGEON2_DESIGN.md):
    /// - "townlife" props: variant 1 the Square's market round the crater's rim (stalls, a draw-well, dead lamps, benches, a notice board,
    ///   and the crowd caught looking at the blast: more paused figures for the session to stand there), variant 2 Main Street's yards
    ///   (barrels, crates, woodpiles, washing lines strung between the houses, handcarts, benches, hanging shop signs, dead lamps for the
    ///   Lamplighter), variant 3 the broken bridge's glass (shards along the slabs' edges, glass skins on their sides, cracks across their
    ///   tops, glass hanging under them and chips stopped in the air). A "townlife" prop of style "stone" is turned to stone with the rest.
    /// - the Old Archive's roof (broken where the blast went through: the rafters bare over the reading hall, slates stopped falling), its
    ///   gables, windows, portal and blown-in doors, the shelf maze as real shelves of ledgers, cases along the walls, clerks' desks, papers
    ///   stopped in the air and hanging lamps (ZoneBuilder.Hallows calls these);
    /// - the Bell Tower's base course, buttresses, quoins, string course, window slits, the belfry's arched openings and its pyramid roof.
    /// No machines (DUNGEON_DESIGN 0.05): carts are hand-drawn, the lamps are dead lanterns on posts, nothing turns. Nothing here stands on a
    /// way, a camp or an anchor: the colliders are kept to the backs of things and out of the walked lines.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>A child of <paramref name="parent"/> standing on the ground at a world point, turned to <paramref name="yaw"/>.</summary>
        Transform TownSpot(Transform parent, string name, Vector2 at, float yaw)
        {
            var s = new GameObject(name).transform; s.SetParent(parent, false);
            s.SetPositionAndRotation(Ground(at), Quaternion.Euler(0, yaw, 0)); return s;
        }
        /// <summary>A point <paramref name="r"/> metres from <paramref name="c"/> on a bearing (degrees, 0 north, 90 east).</summary>
        static Vector2 Bearing(Vector2 c, float bearing, float r) { float a = bearing * Mathf.Deg2Rad; return c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r; }
        /// <summary>A "townlife" prop: 1 the Square, 2 Main Street, 3 the bridge's glass.</summary>
        void TownLife(Transform t, int variant)
        {
            if (variant == 1) SquareLife(t);
            else if (variant == 3) BridgeGlass(t);
            else StreetLife(t);
        }

        // ---------- the Square ----------
        /// <summary>
        /// The Square round the crater (its centre the prop's place): the bowl's wall of bollards stands at 25 m, the ring walk is 25 to 40.
        /// The ways across it are kept clear: Main Street comes in from the south, the tower door is north, the Archive's steps east and the
        /// Warden's walk from them to the tower crosses the north-east; the well-house is on the west rim, the willow and its spirit stone
        /// on the east. The market stands on the west and south-east of the ring, its stalls facing the crater.
        /// </summary>
        void SquareLife(Transform t)
        {
            var c = new Vector2(t.position.x, t.position.z);
            float[] stalls = { 268, 284, 300, 316, 332, 140, 155 };
            for (int i = 0; i < stalls.Length; i++)
            {
                // A stall's front (its -z) faces the crater: turned to its own bearing.
                var s = TownSpot(t, "A market stall", Bearing(c, stalls[i], 35), stalls[i]);
                Stall(s, i % 3);
                if (i % 2 == 0) Crate(s, new Vector3(-2.15f, .41f, .2f), .8f, i * 17);
                else { Crate(s, new Vector3(-2.1f, .41f, .1f), .82f, 6); Crate(s, new Vector3(-2.15f, 1.2f, .1f), .66f, 31); }
            }
            // The dead lamps round the rim (the Lamplighter's; never lit while the town sleeps), their arms toward the crater.
            foreach (float b in new float[] { 22, 68, 112, 158, 202, 248, 292, 338 }) Lamp(TownSpot(t, "A dead lamp", Bearing(c, b, 40.5f), b + 90), 0);
            // The draw-well on the north-west of the ring (the well-house over the cellar stair is its neighbour on the west rim).
            Well(TownSpot(t, "The Square's well", Bearing(c, 345, 33), 345), 0);
            foreach (float b in new float[] { 276, 308 }) CityBench(TownSpot(t, "A bench", Bearing(c, b, 38.5f), b), Vector3.zero, 0, 1.6f);
            NoticeBoard(TownSpot(t, "The Square's notice board", Bearing(c, 196, 39.5f), 196));
            // The crowd: caught where they stood when the working broke, most turned to the blast. Pose 0 caught walking, 2 mid-word,
            // 3 thrown down with their arms up (the session stands a figure at each: EncounterSession.Hallows).
            var crowd = new (float bearing, float r, int pose, float turn)[] {
                (262, 29.5f, 2, 20), (276, 30.5f, 0, -10), (292, 29, 3, 0), (306, 31, 2, -25), (324, 29.5f, 0, 15),
                (150, 30, 0, -15), (163, 29, 3, 10), (30, 29.5f, 2, -20), (50, 30.5f, 0, 25) };
            foreach (var (b, r, pose, turn) in crowd) PausedSpots.Add((Ground(Bearing(c, b, r)), Quaternion.Euler(0, b + 180 + turn, 0), pose, null));
            // Two at the stalls, haggling, and a stall-keeper behind her counter.
            foreach (float b in new float[] { 268, 140 }) PausedSpots.Add((Ground(Bearing(c, b, 32.4f)), Quaternion.Euler(0, b, 0), 2, null));
            PausedSpots.Add((Ground(Bearing(c, 300, 35.9f)), Quaternion.Euler(0, 300 + 180, 0), 2, null));
        }

        // ---------- Main Street ----------
        /// <summary>
        /// Main Street's yards (its houses stand at x = +-14 every 14 m, fronts at +-10, garden fences at +-8.6; the road is 12 wide): in the
        /// alleys between the houses the yards' barrels, crates and woodpiles, washing strung across, a handcart; dead lamps where the alleys
        /// meet the fence line; hanging shop signs; benches in front gardens. Clear of the cutters' doorsteps, the trapdoor yard behind the
        /// bakery, the laundry yard and Upcott's men.
        /// </summary>
        void StreetLife(Transform t)
        {
            var rr = new System.Random(5309);
            float R() { return (float)rr.NextDouble(); }
            // The alleys: (side, the alley's middle along the street, what is in it).
            var alleys = new (int side, float y, int kind)[] {
                (-1, -83, 0), (-1, -55, 1), (-1, -41, 2), (-1, -27, 0), (-1, -13, 1), (-1, 1, 2),
                (1, -111, 1), (1, -97, 0), (1, -83, 2), (1, -69, 1), (1, -27, 2), (1, -13, 0), (1, 1, 1) };
            foreach (var (side, y, kind) in alleys)
            {
                // Each yard's own frame: its x across the alley (along the street), its z away from the street.
                var yard = TownSpot(t, "A yard", new Vector2(side * 14.5f, y), side < 0 ? 270 : 90);
                switch (kind)
                {
                    case 0:
                        // Barrels against a house's side and the washing across the alley.
                        for (int k = 0; k < 3; k++) Barrel(yard, new Vector3(-1.7f + (k % 2) * .25f, 0, -1.2f + k * .78f), k == 1 ? .9f : 1, k * 50);
                        Solid(yard, new Vector3(-1.75f, .5f, -.4f), new Vector3(.9f, 1, 2.4f));
                        foreach (int s in new[] { -1, 1 }) Rod(yard, new Vector3(s * 2.3f, 2.55f, .6f), new Vector3(s * 2.48f, 2.55f, .6f), .05f, art.timber);
                        WashingLine(PlaceUnder(yard, new Vector3(0, 0, .6f)), 4.6f, 2.6f, rr);
                        break;
                    case 1:
                        // A woodpile against one house, a stack of crates against the other.
                        Woodpile(PlaceUnder(yard, new Vector3(.6f, 0, 1.4f), 90));
                        Crate(yard, new Vector3(-1.8f, .41f, -.6f), .82f, 8); Crate(yard, new Vector3(-1.75f, .41f, .35f), .82f, -5); Crate(yard, new Vector3(-1.78f, 1.2f, -.1f), .7f, 22);
                        Solid(yard, new Vector3(-1.8f, .6f, -.1f), new Vector3(1, 1.2f, 2));
                        break;
                    default:
                        // A handcart left in the alley, its shafts toward the street (pulled by hand: no beast, no works).
                        Cart(PlaceUnder(yard, new Vector3(.2f, 0, .9f), (R() - .5f) * 16));
                        Barrel(yard, new Vector3(-1.9f, 0, 2.2f), .9f, 20);
                        break;
                }
            }
            // The dead lamps on the fence line where alleys open (the Lamplighter's round), the arm over the road.
            foreach (var (x, y) in new (float, float)[] { (-8.6f, -97), (-8.6f, -55), (-8.6f, -27), (-8.6f, 1), (8.6f, -111), (8.6f, -83), (8.6f, -55), (8.6f, -13) })
                Lamp(TownSpot(t, "A dead lamp", new Vector2(x, y), x < 0 ? 0 : 180), 0);
            // Shop signs hung from brackets beside the doors (the boards' paint long gone to stone: no words left on them).
            foreach (var (x, y) in new (float, float)[] { (-10, -45.4f), (10, -59.4f), (-10, -17.4f), (10, 10.6f), (10, -101.4f) }) HangingSign(TownSpot(t, "A shop sign", new Vector2(x, y), x < 0 ? 90 : 270));
            // Benches in front gardens, against the house fronts, facing the street.
            foreach (var (x, y) in new (float, float)[] { (-9.45f, -37.2f), (9.45f, -23.2f), (-9.45f, 4.8f) }) CityBench(TownSpot(t, "A bench", new Vector2(x, y), x < 0 ? 270 : 90), Vector3.zero, 0, 1.4f);
        }
        /// <summary>A child of <paramref name="parent"/> at a local point, turned (for builders that work on a transform of their own).</summary>
        static Transform PlaceUnder(Transform parent, Vector3 local, float yaw = 0)
        {
            var s = new GameObject("Part").transform; s.SetParent(parent, false); s.localPosition = local; s.localRotation = Quaternion.Euler(0, yaw, 0); return s;
        }
        /// <summary>A shop sign on an iron bracket out from a house front (the spot is on the front, its +z out over the garden).</summary>
        void HangingSign(Transform t)
        {
            var iron = Tint(art.metal, new Color(.2f, .2f, .22f)); var board = Tint(art.timber, new Color(.4f, .3f, .2f));
            Part(PrimitiveType.Cube, t, new Vector3(0, 3, .55f), new Vector3(.05f, .05f, 1.1f), iron);
            Rod(t, new Vector3(0, 2.55f, .02f), new Vector3(0, 2.98f, .9f), .03f, iron);
            foreach (float z in new[] { .55f, .95f }) Rod(t, new Vector3(0, 2.98f, z), new Vector3(0, 2.78f, z), .015f, iron);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.52f, .75f), new Vector3(.05f, .52f, .62f), board, Quaternion.Euler(0, 0, 0));
        }

        // ---------- the bridge's glass ----------
        static Material shardGlass;
        /// <summary>The bridge's glass: pale, cold, see-through and glossy.</summary>
        Material ShardGlass
        {
            get
            {
                if (shardGlass != null) return shardGlass;
                var m = new Material(Shader.Find("Standard")) { name = "Shattered glass", color = new Color(.74f, .84f, .92f, .5f) };
                m.SetFloat("_Glossiness", .96f); m.SetFloat("_Metallic", .15f);
                m.SetFloat("_Mode", 2); m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_ALPHABLEND_ON"); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.06f, .08f, .11f));
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                return shardGlass = m;
            }
        }
        /// <summary>
        /// The Bridge of Shattered Glass (CANON, book1 ch.17): every open-ground stone deck over the chasm's fog (the slabs and the ramps
        /// between them, and the lower orbit's) given its glass: a skin of glass down each long side, shards standing up and out along both
        /// edges (leaning away from the walk), cracks across the top, long splinters of glass hanging under it and chips stopped in the air
        /// round it. Looks only: no collider, nothing on the walk.
        /// </summary>
        void BridgeGlass(Transform t)
        {
            var glass = ShardGlass; var crack = Tint(art.stone, new Color(.1f, .11f, .14f)); var rr = new System.Random(8117);
            float R() { return (float)rr.NextDouble(); }
            // Where a ramp or a stair meets a deck no shard stands: the walk goes on across that edge.
            bool AtJoin(ZoneDeckSpot self, Vector3 p)
            {
                foreach (var o in DeckSpots)
                {
                    if (o == self || o == null || o.cave != null || o.mid == null || o.mid.Length < 2) continue;
                    Vector3 oa = o.mid[0], ob = o.mid[o.mid.Length - 1]; var ab = new Vector2(ob.x - oa.x, ob.z - oa.z);
                    float u = Mathf.Clamp01(Vector2.Dot(new Vector2(p.x - oa.x, p.z - oa.z), ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                    var near = Vector3.Lerp(oa, ob, u);
                    if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(near.x, near.z)) < o.half + .7f && Mathf.Abs(p.y - near.y) < 2.5f) return true;
                }
                return false;
            }
            foreach (var spot in DeckSpots)
            {
                if (spot == null || spot.cave != null || spot.round || spot.def == null || spot.def.style != "stone" || spot.mid == null || spot.mid.Length < 2) continue;
                var a = spot.mid[0]; var b = spot.mid[spot.mid.Length - 1];
                if (!FogFloor(new Vector2((a.x + b.x) / 2, (a.z + b.z) / 2), out _)) continue;
                var along = b - a; float len = along.magnitude; if (len < .5f) continue;
                var rot = Quaternion.LookRotation(along / len, Vector3.up); Vector3 up = rot * Vector3.up, right = rot * Vector3.right, fwd = along / len;
                float half = spot.half, thick = 1.1f;
                var g = new GameObject("Glass: " + spot.def.name).transform; g.SetParent(t, false); g.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                // The skins down the long sides.
                foreach (int side in new[] { -1, 1 })
                    Part(PrimitiveType.Cube, g, (a + b) / 2 - up * thick / 2 + right * side * (half + .03f), new Vector3(.05f, thick + .08f, len), glass, rot);
                // Shards along both edges, leaning out.
                foreach (int side in new[] { -1, 1 })
                    for (float u = .5f; u < len - .3f; u += .8f + R() * .6f)
                    {
                        float tall = .3f + R() * .8f, lean = 15 + R() * 25, wide = .14f + R() * .22f;
                        var q = rot * Quaternion.Euler(0, (R() - .5f) * 40, -side * lean);
                        var foot = a + fwd * u + right * side * (half - .06f) - up * .12f;
                        if (AtJoin(spot, foot)) continue;
                        Part(PrimitiveType.Cube, g, foot + q * Vector3.up * (tall / 2), new Vector3(.04f, tall, wide), glass, q);
                    }
                // Cracks across the top: a few zigzags of dark hairline.
                int lines = Mathf.Clamp(Mathf.RoundToInt(len / 3), 1, 4);
                for (int l = 0; l < lines; l++)
                {
                    var p = a + fwd * (len * (l + .5f) / lines) + right * ((R() - .5f) * half) + up * .006f;
                    float heading = R() * 360;
                    for (int k = 0; k < 4; k++)
                    {
                        float seg = .5f + R() * .7f; heading += (R() - .5f) * 80;
                        var dir = rot * Quaternion.Euler(0, heading, 0) * Vector3.forward; var q = Vector3.Project(p - a, right).magnitude > half - .3f ? -dir : dir;
                        var next = p + q * seg;
                        if (Mathf.Abs(Vector3.Dot(next - (a + b) / 2, right)) > half - .1f) break;
                        Part(PrimitiveType.Cube, g, (p + next) / 2, new Vector3(.035f, .01f, seg), crack, Quaternion.LookRotation(q, up));
                        p = next;
                    }
                }
                // Glass hanging under it, and chips stopped in the air round it.
                int hang = Mathf.Clamp(Mathf.RoundToInt(len / 1.6f), 2, 6);
                for (int k = 0; k < hang; k++)
                {
                    float drop = 1 + R() * 2.6f, rad = .18f + R() * .25f;
                    var at = a + fwd * (len * (k + .3f + R() * .4f) / hang) + right * ((R() - .5f) * half * 1.6f) - up * (thick + .02f);
                    MeshPart(PropMesh("Glass splinter", () => ZoneMeshes.Cone(1, 1, 5)), g, at, glass, rot * Quaternion.Euler(180 + (R() - .5f) * 14, R() * 360, 0)).transform.localScale = new Vector3(rad, drop, rad);
                }
                for (int k = 0; k < 4; k++)
                {
                    var at = a + fwd * (R() * len) + right * ((R() < .5f ? -1 : 1) * (half + .8f + R() * 2.4f)) + up * (R() * 3 - 1.5f);
                    Part(PrimitiveType.Cube, g, at, new Vector3(.03f, .2f + R() * .4f, .15f + R() * .3f), glass, Quaternion.Euler(R() * 360, R() * 360, R() * 360));
                }
            }
        }

        // ---------- the Old Archive ----------
        /// <summary>
        /// One run of the shelf maze (the Archive's inner walls, 3 m high on the floor at <paramref name="f"/>): a timber frame (a back
        /// panel, end posts, five boards, a cornice) and ledgers on both faces in runs of uneven height with a gap here and there. Joined
        /// into a mesh a material (the frame and three tones of book), so a run is four objects however many books are on it.
        /// </summary>
        void ArchiveStack(Transform t, float x, float z0, float z1, float f, System.Random rr)
        {
            float R() { return (float)rr.NextDouble(); }
            float len = z1 - z0, zc = (z0 + z1) / 2;
            var frame = new List<CombineInstance>();
            frame.Add(Ashlar(new Vector3(x, f + 1.5f, zc), new Vector3(.1f, 3, len)));
            foreach (float z in new[] { z0 + .05f, z1 - .05f }) frame.Add(Ashlar(new Vector3(x, f + 1.5f, z), new Vector3(.76f, 3, .1f)));
            for (int k = 0; k < 5; k++) frame.Add(Ashlar(new Vector3(x, f + .04f + k * .72f, zc), new Vector3(.74f, .05f, len - .1f)));
            frame.Add(Ashlar(new Vector3(x, f + 3.05f, zc), new Vector3(.86f, .12f, len + .1f)));
            var books = new[] { new List<CombineInstance>(), new List<CombineInstance>(), new List<CombineInstance>() };
            for (int row = 0; row < 4; row++)
                foreach (int side in new[] { -1, 1 })
                {
                    float z = z0 + .14f;
                    while (z < z1 - .3f)
                    {
                        float run = Mathf.Min(.35f + R() * .8f, z1 - .14f - z), high = .42f + R() * .2f;
                        if (R() < .1f) { z += .25f; continue; }
                        books[rr.Next(3)].Add(Ashlar(new Vector3(x + side * .2f, f + .065f + row * .72f + high / 2, z + run / 2), new Vector3(.3f, high, run - .02f), .5f));
                        z += run + .02f;
                    }
                }
            Stonework("Shelf frame", t, Tint(art.timber, new Color(.4f, .38f, .36f)), frame.ToArray());
            var tones = new[] { new Color(.46f, .42f, .38f), new Color(.38f, .36f, .35f), new Color(.52f, .5f, .46f) };
            for (int i = 0; i < 3; i++) if (books[i].Count > 0) Stonework("Ledgers", t, Tint(art.stone, tones[i]), books[i].ToArray());
        }
        /// <summary>A case of ledgers against a wall (its open front along yaw's -z), solid: the frame, five boards, books in runs.</summary>
        void LedgerCase(Transform t, Vector3 foot, float yaw, System.Random rr)
        {
            float R() { return (float)rr.NextDouble(); }
            var q = Quaternion.Euler(0, yaw, 0); const float W = 2.2f, H = 2.6f, D = .5f;
            var wood = Tint(art.timber, new Color(.4f, .37f, .34f));
            Solidify(BoxPart(t, foot + q * new Vector3(0, H / 2, .2f), new Vector3(W, H, .08f), wood, q));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, foot + q * new Vector3(s * (W / 2 - .04f), H / 2, 0), new Vector3(.08f, H, D), wood, q);
            for (int row = 0; row < 5; row++) Part(PrimitiveType.Cube, t, foot + q * new Vector3(0, .06f + row * .6f, 0), new Vector3(W - .1f, .05f, D), wood, q);
            Part(PrimitiveType.Cube, t, foot + q * new Vector3(0, H + .06f, -.03f), new Vector3(W + .14f, .12f, D + .1f), wood, q);
            var tones = new[] { Tint(art.stone, new Color(.46f, .42f, .38f)), Tint(art.stone, new Color(.38f, .36f, .35f)), Tint(art.stone, new Color(.52f, .5f, .46f)) };
            for (int row = 0; row < 4; row++)
            {
                float x = -W / 2 + .12f;
                while (x < W / 2 - .25f)
                {
                    float run = Mathf.Min(.3f + R() * .5f, W / 2 - .12f - x), high = .38f + R() * .16f;
                    if (R() < .12f) { x += .2f; continue; }
                    Part(PrimitiveType.Cube, t, foot + q * new Vector3(x + run / 2, .085f + row * .6f + high / 2, -.02f), new Vector3(run - .02f, high, .32f), tones[rr.Next(3)], q);
                    x += run + .02f;
                }
            }
        }
        /// <summary>
        /// The Old Archive dressed (its frame: x along the hall's 50 m, z across its 24, the floor at <paramref name="f"/>, the walls
        /// <paramref name="h"/> high, a metre thick on the lines x = +-w and z = +-d; the doorless front on the west):
        /// - the roof: slate on rafters, gabled east and west; over the reading hall the blast went through it, so the rafters stand bare
        ///   there and slates hang where they were falling;
        /// - tall round-headed windows down both long walls over the galleries, and two in the apse;
        /// - the portal round the west doorway (pilasters, an entablature, a pediment with the open book and the eye) and its doors blown in:
        ///   one leaf thrown against the south wall, the other tumbling over the aisle, stopped;
        /// - cases of ledgers against the walls under the galleries, clerks' desks in the bays, papers stopped in the air, lamps hung from
        ///   the ridge.
        /// </summary>
        void ArchiveDressing(Transform t, float w, float d, float h, float f)
        {
            var rr = new System.Random(6151);
            float R() { return (float)rr.NextDouble(); }
            var slate = Tint(art.slate != null ? art.slate : art.stone, new Color(.36f, .36f, .4f)); var beam = Tint(art.timber, new Color(.3f, .27f, .24f));
            var wall = Dressed(new Color(.6f, .58f, .55f)); var trim = Dressed(new Color(.7f, .68f, .64f)); var pane = Tint(art.stone, new Color(.07f, .08f, .1f));
            // The roof: the eaves a little over the walls, the ridge along x.
            float D = d + .5f + .6f, rise = 6, eave = h - .3f, L = Mathf.Sqrt(D * D + rise * rise), ang = Mathf.Atan2(rise, D) * Mathf.Rad2Deg;
            float x0 = -w - .6f, span = (2 * w + 1.2f) / 10;
            // Which tenths of each slope still carry slate (from, to as fractions of the tenth); the rest is open to the sky.
            (float from, float to)[] Kept(int s, int i)
            {
                if (s < 0) return i == 4 ? new[] { (0f, .62f) } : i == 5 || i == 6 ? new (float, float)[0] : i == 7 ? new[] { (.32f, 1f) } : new[] { (0f, 1f) };
                return i == 5 ? new[] { (0f, .7f) } : i == 6 ? new (float, float)[0] : i == 7 ? new[] { (.24f, 1f) } : new[] { (0f, 1f) };
            }
            var ridge = new Vector3(0, eave + rise, 0);
            foreach (int s in new[] { -1, 1 })
            {
                var q = Quaternion.Euler(s * ang, 0, 0); var down = new Vector3(0, -rise, s * D) / L;
                for (int i = 0; i < 10; i++)
                {
                    float a0 = x0 + i * span;
                    foreach (var (from, to) in Kept(s, i))
                    {
                        float xa = a0 + from * span, xb = a0 + to * span;
                        BoxPart(t, new Vector3((xa + xb) / 2, 0, 0) + ridge + down * (L / 2) + Vector3.down * .15f, new Vector3(xb - xa, .3f, L), slate, q);
                    }
                }
                // The bare rafters over the hole (every metre, some broken off short), and two purlins across them.
                float g0 = s < 0 ? x0 + 4.62f * span : x0 + 5.7f * span, g1 = s < 0 ? x0 + 7.32f * span : x0 + 7.24f * span;
                for (float x = g0 + .4f; x < g1; x += 1)
                {
                    float l = rr.Next(3) == 0 ? L * (.35f + R() * .3f) : L;
                    Part(PrimitiveType.Cube, t, new Vector3(x, 0, 0) + ridge + down * (l / 2) + Vector3.down * .42f, new Vector3(.16f, .24f, l), beam, q);
                }
                foreach (float k in new[] { .33f, .66f }) Part(PrimitiveType.Cube, t, new Vector3((g0 + g1) / 2, 0, 0) + ridge + down * (L * k) + Vector3.down * .62f, new Vector3(g1 - g0, .2f, .2f), beam, q);
            }
            Part(PrimitiveType.Cube, t, ridge + Vector3.down * .3f, new Vector3(2 * w + 1.2f, .32f, .32f), beam);
            // Slates stopped falling through the hole, some of them down to the galleries.
            for (int k = 0; k < 14; k++)
                Part(PrimitiveType.Cube, t, new Vector3(x0 + 4.8f * span + R() * 2.3f * span, f + 3.5f + R() * (eave - f - 3), (R() - .5f) * 2 * (d - 3)), new Vector3(.5f, .04f, .34f), slate, Quaternion.Euler(R() * 360, R() * 360, R() * 360));
            // The gables, west and east, from the wall top to the ridge.
            foreach (int s in new[] { -1, 1 }) MeshPart(ZoneMeshes.Gable(2 * d + 1, rise - .3f, 1, 2, 0, h), t, new Vector3(s * w, h, 0), wall, Quaternion.Euler(0, 90, 0));
            // Windows: tall, round-headed, dark glass in a stone sill and keystone, through the long walls over the galleries.
            void Window(Vector3 at, bool acrossX)
            {
                var q = acrossX ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
                Part(PrimitiveType.Cube, t, at, new Vector3(1.3f, 3, 1.12f), pane, q);
                Part(PrimitiveType.Cylinder, t, at + Vector3.up * 1.5f, new Vector3(1.3f, .56f, 1.3f), pane, q * Quaternion.Euler(90, 0, 0));
                Part(PrimitiveType.Cube, t, at + Vector3.down * 1.58f, new Vector3(1.7f, .18f, 1.4f), trim, q);
                Part(PrimitiveType.Cube, t, at + Vector3.up * 2.32f, new Vector3(.36f, .46f, 1.3f), trim, q);
            }
            for (float x = -w + 7; x < w - 2; x += 6) foreach (int s in new[] { -1, 1 }) Window(new Vector3(x, 8.6f, s * d), false);
            foreach (int s in new[] { -1, 1 }) Window(new Vector3(w, 8.6f, s * 5), true);
            // The portal: pilasters either side of the west doorway, an entablature and a pediment, the crest on it.
            foreach (int s in new[] { -1, 1 }) BoxPart(t, new Vector3(-w - .8f, 4.25f, s * 4.4f), new Vector3(.6f, 8.5f, 1.1f), trim, null, 1.75f);
            BoxPart(t, new Vector3(-w - .85f, 8.95f, 0), new Vector3(.7f, .9f, 10.6f), trim, null, 1.75f);
            MeshPart(ZoneMeshes.Gable(10.6f, 2.2f, .7f, 2, 0, 9.4f), t, new Vector3(-w - .85f, 9.4f, 0), trim, Quaternion.Euler(0, 90, 0));
            var crest = Tint(art.stone, new Color(.78f, .74f, .62f));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(-w - 1.24f, 10.15f, s * .45f), new Vector3(.06f, .7f, .8f), crest, Quaternion.Euler(0, s * 18, 0));
            Part(PrimitiveType.Sphere, t, new Vector3(-w - 1.28f, 10.7f, 0), new Vector3(.08f, .22f, .36f), crest);
            // The doors, blown in: one leaf thrown against the west wall north of the doorway (its foot on the floor, its top on the wall),
            // the other tumbling over the aisle where the blast left it, high enough to walk under.
            var leaf = Tint(art.timber, new Color(.34f, .3f, .27f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            var thrown = new GameObject("A door, thrown").transform; thrown.SetParent(t, false); thrown.localPosition = new Vector3(-w + 1.5f, f, 6.2f); thrown.localRotation = Quaternion.Euler(0, 90, 0) * Quaternion.Euler(-13, 0, 0);
            var hung = new GameObject("A door, in the air").transform; hung.SetParent(t, false); hung.localPosition = new Vector3(-w + 9, f + 6, 1.2f); hung.localRotation = Quaternion.Euler(38, 24, 64);
            foreach (var door in new[] { thrown, hung })
            {
                float foot = door == thrown ? 3.1f : 0;
                Part(PrimitiveType.Cube, door, new Vector3(0, foot, 0), new Vector3(3.4f, 6.2f, .22f), leaf);
                foreach (float y in new[] { -2.1f, 0, 2.1f }) Part(PrimitiveType.Cube, door, new Vector3(0, foot + y, -.13f), new Vector3(3.3f, .14f, .05f), iron);
            }
            // Cases of ledgers along the walls, under the galleries, in the gaps between the twisted columns and the galleries' posts
            // (the north wall's cell doors kept clear, and the north-west bay where the High Archivist's page lies).
            foreach (float x in new[] { -19.05f, -11.3f, -6.4f, 6.6f, 11.5f }) LedgerCase(t, new Vector3(x, f, -d + .72f), 180, rr);
            foreach (float x in new[] { -11.3f, 11.5f, 16.75f }) LedgerCase(t, new Vector3(x, f, d - .72f), 0, rr);
            // Clerks' desks in the bays, each with its stool, an open ledger, loose papers and an ink-pot (no colliders: the bays are walked).
            var desk = Tint(art.timber, new Color(.34f, .3f, .27f)); var paper = Tint(art.plaster, new Color(.82f, .8f, .74f)); var ink = Tint(art.stone, new Color(.06f, .06f, .08f));
            foreach (var (x, z, yaw) in new (float, float, float)[] { (-16, -8.4f, 0), (3.5f, -8.4f, 0), (14.5f, -8.2f, 0), (-16, 8.6f, 180), (3.5f, 8.4f, 180), (14.5f, 8.2f, 180) })
            {
                var o = new GameObject("A clerk's desk").transform; o.SetParent(t, false); o.localPosition = new Vector3(x, f, z); o.localRotation = Quaternion.Euler(0, yaw + (R() - .5f) * 12, 0);
                Part(PrimitiveType.Cube, o, new Vector3(0, .8f, 0), new Vector3(1.4f, .06f, .7f), desk, Quaternion.Euler(-9, 0, 0));
                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, o, new Vector3(s * .62f, .39f, 0), new Vector3(.07f, .78f, .6f), desk);
                Part(PrimitiveType.Cylinder, o, new Vector3(0, .46f, .7f), new Vector3(.4f, .03f, .4f), desk);
                Part(PrimitiveType.Cube, o, new Vector3(0, .23f, .7f), new Vector3(.08f, .46f, .08f), desk);
                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, o, new Vector3(s * .17f, .86f, -.02f), new Vector3(.32f, .03f, .42f), paper, Quaternion.Euler(-9, 0, s * -5));
                Part(PrimitiveType.Cube, o, new Vector3(.5f, .85f, .1f), new Vector3(.22f, .01f, .3f), paper, Quaternion.Euler(-9, 30, 0));
                Part(PrimitiveType.Cylinder, o, new Vector3(-.55f, .87f, .2f), new Vector3(.07f, .04f, .07f), ink);
            }
            // Papers stopped in the air: thickest just inside the blown door, thinning down the hall.
            for (int k = 0; k < 40; k++)
            {
                float u = R(); u *= u;
                Part(PrimitiveType.Cube, t, new Vector3(-w + 2 + u * (2 * w - 6), f + 1.2f + R() * 6, (R() - .5f) * 2 * (d - 2.5f)), new Vector3(.21f, .004f, .3f), paper, Quaternion.Euler(R() * 360, R() * 360, R() * 360));
            }
            // Lamps hung from the ridge on chains, down the aisle (dead: the hall's light is its lamp rings and the hole in the roof).
            var lantern = Glowing(new Color(.9f, .82f, .6f), .5f);
            foreach (float x in new[] { -w + 9, 0f, w - 9 })
            {
                Rod(t, new Vector3(x, eave + rise - .45f, 0), new Vector3(x, f + 6.4f, 0), .04f, iron);
                Part(PrimitiveType.Cylinder, t, new Vector3(x, f + 6.2f, 0), new Vector3(1.6f, .05f, 1.6f), iron);
                foreach (int s in new[] { -1, 1 }) foreach (int r in new[] { 0, 1 })
                    Part(PrimitiveType.Cube, t, new Vector3(x + (r == 0 ? s * .72f : 0), f + 6.35f, r == 1 ? s * .72f : 0), new Vector3(.12f, .2f, .12f), lantern);
            }
        }

        // ---------- the Bell Tower ----------
        /// <summary>
        /// The Bell Tower dressed (its frame: the walls a metre thick on x, z = +-w, their outer faces at +-(w + .5); the belfry floor at
        /// <paramref name="belfry"/>, the lintels' top at <paramref name="h"/>): a base course round the foot (the door left clear),
        /// stepped buttresses clasping the corners to 9 m and quoins above them, a string course under the belfry, window slits lighting
        /// the stair, each belfry opening split by a pier into two round-headed arches, and a slate pyramid roof on a cornice with a finial.
        /// The light stone is joined into one mesh.
        /// </summary>
        void BelfryDressing(Transform t, float w, float belfry, float h)
        {
            float o = w + .5f;
            var dressed = new List<CombineInstance>();
            void Block(Vector3 at, Vector3 size) { dressed.Add(Ashlar(at, size)); }
            // The base course: a metre high and a quarter proud, a chamfer course on it; the south face parted for the door.
            foreach (var (y, high, proud) in new[] { (.5f, 1f, .25f), (1.07f, .14f, .12f) })
            {
                float face = o + proud / 2 - .1f, depth = proud + .2f, run = 2 * (o + proud);
                Block(new Vector3(0, y, face), new Vector3(run, high, depth));
                Block(new Vector3(face, y, 0), new Vector3(depth, high, run));
                Block(new Vector3(-face, y, 0), new Vector3(depth, high, run));
                foreach (int s in new[] { -1, 1 }) Block(new Vector3(s * (1.7f + (o + proud - 1.7f) / 2), y, -face), new Vector3(o + proud - 1.7f, high, depth));
            }
            // The buttresses: clasping each corner, .7 proud to 6 m, .4 to 9 m.
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                foreach (var (y0, y1, out1) in new[] { (0f, 6f, .7f), (6f, 9f, .4f) })
                {
                    float y = (y0 + y1) / 2, high = y1 - y0;
                    Block(new Vector3(sx * (o - .7f), y, sz * (o + out1 / 2 - .05f)), new Vector3(1.4f, high, out1 + .1f));
                    Block(new Vector3(sx * (o + out1 / 2 - .05f), y, sz * (o - .7f)), new Vector3(out1 + .1f, high, 1.4f));
                    Block(new Vector3(sx * (o + out1 / 2 - .05f), y, sz * (o + out1 / 2 - .05f)), new Vector3(out1 + .1f, high, out1 + .1f));
                    Solid(t, new Vector3(sx * (o + (out1 - 1.4f) / 2), y, sz * (o + (out1 - 1.4f) / 2)), new Vector3(1.4f + out1, high, 1.4f + out1));
                }
            // Quoins up the corners above the buttresses, long and short by turns.
            int course = 0;
            for (float y = 9.3f; y < belfry - .3f; y += .55f, course++)
                foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                    Block(course % 2 == 0 ? new Vector3(sx * (o - .5f), y, sz * (o - .22f)) : new Vector3(sx * (o - .22f), y, sz * (o - .5f)),
                        course % 2 == 0 ? new Vector3(1.1f, .5f, .56f) : new Vector3(.56f, .5f, 1.1f));
            // A string course under the belfry (the dial's face, under it on the south, kept clear).
            foreach (int s in new[] { -1, 1 })
            {
                Block(new Vector3(0, belfry - .15f, s * (o + .02f)), new Vector3(2 * o + .3f, .26f, .3f));
                Block(new Vector3(s * (o + .02f), belfry - .15f, 0), new Vector3(.3f, .26f, 2 * o + .3f));
            }
            // The belfry's openings: a pier up the middle of each face, and over each half a round head (the wall filled in course by course
            // above its curve). The parapet stands at belfry + 1.1; the lintels from h - 1.2.
            float top = h - 1.2f, halfW = (w - 1 - .45f) / 2, spring = top - halfW;
            foreach (int f in new[] { 0, 1, 2, 3 })
            {
                var q = Quaternion.Euler(0, f * 90, 0);
                Block(q * new Vector3(0, (belfry + 1.1f + top) / 2, -w), f % 2 == 0 ? new Vector3(.9f, top - belfry - 1.1f, 1) : new Vector3(1, top - belfry - 1.1f, .9f));
                foreach (int side in new[] { -1, 1 })
                {
                    float cx = side * (.45f + halfW);
                    for (float y = spring; y < top - .01f; y += .2f)
                    {
                        float yy = Mathf.Min(y + .2f, top), mid = (y + yy) / 2, open = Mathf.Sqrt(Mathf.Max(0, halfW * halfW - (mid - spring) * (mid - spring)));
                        float fill = halfW - open; if (fill < .02f) continue;
                        foreach (int edge in new[] { -1, 1 })
                        {
                            var local = new Vector3(cx + edge * (halfW - fill / 2), mid, -w);
                            var size = new Vector3(fill, yy - y, 1);
                            Block(q * local, f % 2 == 0 ? size : new Vector3(size.z, size.y, size.x));
                        }
                    }
                }
            }
            // Window slits lighting the stair: tall, dark, a stone surround; on the south only one, clear of the door and the dial.
            var pane = Tint(art.stone, new Color(.06f, .06f, .08f));
            foreach (int f in new[] { 1, 2, 3 })
                foreach (float y in new[] { 5.5f, 10.2f, 14.6f })
                {
                    var q = Quaternion.Euler(0, f * 90, 0);
                    Part(PrimitiveType.Cube, t, q * new Vector3(0, y, -o - .01f), new Vector3(.32f, 1.7f, .06f), pane, q);
                    Block(q * new Vector3(0, y - .95f, -o - .04f), f % 2 == 0 ? new Vector3(.8f, .16f, .2f) : new Vector3(.2f, .16f, .8f));
                    Block(q * new Vector3(0, y + .95f, -o - .04f), f % 2 == 0 ? new Vector3(.7f, .2f, .2f) : new Vector3(.2f, .2f, .7f));
                }
            Part(PrimitiveType.Cube, t, new Vector3(3.2f, 8.4f, -o - .01f), new Vector3(.32f, 1.7f, .06f), pane);
            Block(new Vector3(3.2f, 7.45f, -o - .04f), new Vector3(.8f, .16f, .2f)); Block(new Vector3(3.2f, 9.35f, -o - .04f), new Vector3(.7f, .2f, .2f));
            Stonework("Dressed stone", t, Dressed(new Color(.74f, .72f, .68f)), dressed.ToArray());
            // The roof: a cornice on the lintels, a slate pyramid over it, a finial at its point (a plain iron spike: nothing on it turns).
            var slate = Tint(art.slate != null ? art.slate : art.stone, new Color(.34f, .34f, .38f));
            BoxPart(t, new Vector3(0, h + .2f, 0), new Vector3(2 * o + 1, .4f, 2 * o + 1), Dressed(new Color(.7f, .68f, .64f)), null, 1.75f);
            float eave = o + .8f, spireH = 9;
            MeshPart(ZoneMeshes.Spire(eave * Mathf.Sqrt(2), spireH, 4, 2.5f), t, new Vector3(0, h + .4f, 0), slate, Quaternion.Euler(0, 45, 0));
            var iron = Tint(art.metal, new Color(.18f, .18f, .2f));
            Rod(t, new Vector3(0, h + .4f + spireH - .3f, 0), new Vector3(0, h + .4f + spireH + 1.6f, 0), .07f, iron);
            Part(PrimitiveType.Sphere, t, new Vector3(0, h + .4f + spireH + .5f, 0), Vector3.one * .3f, iron);
        }
    }
}
