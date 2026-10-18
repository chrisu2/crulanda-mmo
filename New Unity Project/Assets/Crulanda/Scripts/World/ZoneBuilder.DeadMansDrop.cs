using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Dead Man's Drop (Docs/REWORK_LOWTOWNS_HALLOWS.md, Lowtowns pick 10 and conflict 5; Docs/ZONE_LOWTOWNS.md "Dead Man's Drop and the Night
    /// Market"). CANON: in the Rust Market's lanes a blind corner ends "right at the lip of Dead Man's Drop, where the street simply vanished
    /// into the yawning, bottomless abyss of the Under-City" (book1 ch.1:313), loose stones clicking away into it; the walls of the drop are
    /// rough tower masonry, and three stories down hangs "a thick, rotting fungal-net" (ch.1:347).
    /// The game: the pit named <see cref="DropName"/> in the zone's data (a ZonePit, sunk after the terraces into the market's shelf), a
    /// lane running to its west lip, and here:
    /// - soot brick lining the four sides from the dark up to the lip (the towers' masonry; visual only, the ground's own slope is solid);
    /// - a broken railing round the lip, solid and cut out of the navmesh, with one gap where the lane ends: nobody walks off it by
    ///   accident, and no townsfolk ever do, but you may step off;
    /// - the net <see cref="NetBelow"/> metres under the lip (three stories), filling the shaft wall to wall: a collider you land on, grey
    ///   rotten cords and a few clumps of the green glowing mould (CANON, book1 ch.6:129);
    /// - under it the dark: fog sheets one under another and a black floor far down (the Under-City is not this zone's: GAME-ONLY);
    /// - the iron rungs (GAME-ONLY) up the north wall from the net to a drain-cut: E on the net climbs them and lets you out on Cooper's Row
    ///   under the market (<see cref="DropOut"/>).
    /// Falling in costs a little health and never kills (EncounterSession.NightMarket). No machines: brick, rope, rusted iron.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The pit's name in the zone's data and the place's name.</summary>
        public const string DropName = "Dead Man's Drop";
        /// <summary>The rungs' name (their ZoneInteractable).</summary>
        public const string RungsName = "The iron rungs";
        /// <summary>How far under the lip the fungal net hangs: three stories (book1 ch.1:347), three metres and a bit each.</summary>
        public const float NetBelow = 9.5f;
        /// <summary>How far in from the pit's edge its sheer side reaches (the brick lining's inner face): the net fills the rest.</summary>
        public const float DropSide = 1.3f;
        /// <summary>Where the rungs let you out: Cooper's Row, a lane under the market on the Shard-Burners' Yard's shelf (GAME-ONLY).</summary>
        public static readonly Vector2 DropOut = new Vector2(165, -48.8f);
        /// <summary>The width of the gap in the railing where the lane ends.</summary>
        public const float DropGap = 2.4f;

        /// <summary>The Drop as built: the pit, its lip and net heights, its centre, the net's half size, the rungs, and the gap's middle.</summary>
        public sealed class DropSpot
        {
            public ZonePit pit; public Vector3 centre; public float lip, net; public Vector2 half; public ZoneInteractable rungs; public Vector3 gap;
            /// <summary>Whether a world point is over the shaft (within the pit's edges).</summary>
            public bool Over(Vector3 p)
            {
                var d = Quaternion.Euler(0, -pit.rotation, 0) * (p - centre);
                return Mathf.Abs(d.x) <= pit.size.x / 2 && Mathf.Abs(d.z) <= pit.size.y / 2;
            }
        }
        /// <summary>The Lowtowns' Drop, or null in any other zone.</summary>
        public DropSpot LowtownDrop { get; private set; }

        /// <summary>The pit named <see cref="DropName"/> in a zone's data, or null.</summary>
        public static ZonePit DropPit(ZoneDefinition z)
        {
            if (z == null || z.pits == null) return null;
            foreach (var p in z.pits) if (p != null && p.name == DropName) return p;
            return null;
        }

        void BuildDeadMansDrop()
        {
            LowtownDrop = null;
            var pit = DropPit(Zone); if (pit == null) return;
            var rot = Quaternion.Euler(0, pit.rotation, 0);
            // The lip: the market's own height just outside the pit (the terraces', the pit not yet in it).
            float lip = TerraceGround(Zone.terraces, pit.center.x, pit.center.y, 0);
            float net = lip - NetBelow, floor = lip - pit.depth;
            var centre = new Vector3(pit.center.x, lip, pit.center.y);
            var half = new Vector2(pit.size.x / 2 - DropSide, pit.size.y / 2 - DropSide);
            var root = new GameObject("Dead Man's Drop").transform; root.SetParent(statics, false); root.position = centre; root.rotation = rot;
            var spot = new DropSpot { pit = pit, centre = centre, lip = lip, net = net, half = half };
            var r = new System.Random(Zone.seed + 3131); float R() { return (float)r.NextDouble(); }
            var b = new Batch(); var brick = SootBrick(3); var brick2 = SootBrick(5); var iron = SootIron; var cope = WallCoping;
            float hx = pit.size.x / 2, hz = pit.size.y / 2;

            // The towers' masonry: four walls of soot brick from the floor of the dark to the lip, their inner faces at DropSide in.
            float tall = pit.depth + .1f, thick = DropSide - .25f;
            b.Box(brick, new Vector3(0, -tall / 2, hz - DropSide + thick / 2), new Vector3(pit.size.x, tall, thick));
            b.Box(brick, new Vector3(0, -tall / 2, -hz + DropSide - thick / 2), new Vector3(pit.size.x, tall, thick));
            b.Box(brick2, new Vector3(hx - DropSide + thick / 2, -tall / 2, 0), new Vector3(thick, tall, pit.size.y));
            b.Box(brick2, new Vector3(-hx + DropSide - thick / 2, -tall / 2, 0), new Vector3(thick, tall, pit.size.y));
            // The faces broken up: string courses of darker brick, a few blocks proud of the face, streaks of damp down from the lip.
            var wet = Tint(art.stone, new Color(.11f, .1f, .1f));
            for (float y = -2.5f; y > -pit.depth + 2; y -= 3.2f + R() * 1.5f)
                foreach (int s in new[] { -1, 1 })
                {
                    b.Box(cope, new Vector3(0, y, s * (hz - DropSide - .04f)), new Vector3(pit.size.x - 2 * DropSide, .18f, .12f));
                    b.Box(cope, new Vector3(s * (hx - DropSide - .04f), y, 0), new Vector3(.12f, .18f, pit.size.y - 2 * DropSide));
                }
            for (int k = 0; k < 14; k++)
            {
                float along = (R() - .5f) * (pit.size.x - 2 * DropSide - .6f), len = 2 + R() * 9; int side = k % 4;
                var at = side == 0 ? new Vector3(along, -len / 2, hz - DropSide - .03f) : side == 1 ? new Vector3(along, -len / 2, -hz + DropSide + .03f)
                    : side == 2 ? new Vector3(hx - DropSide - .03f, -len / 2, along) : new Vector3(-hx + DropSide + .03f, -len / 2, along);
                b.Box(wet, at, side < 2 ? new Vector3(.25f + R() * .5f, len, .04f) : new Vector3(.04f, len, .25f + R() * .5f));
            }

            // The lip: a coping of dark stone round it, and loose stones on the setts by the gap (ch.1:313: "loose stones clicking away").
            b.Box(cope, new Vector3(0, -.05f, hz - .3f), new Vector3(pit.size.x, .2f, .7f));
            b.Box(cope, new Vector3(0, -.05f, -hz + .3f), new Vector3(pit.size.x, .2f, .7f));
            b.Box(cope, new Vector3(hx - .3f, -.05f, 0), new Vector3(.7f, .2f, pit.size.y));
            b.Box(cope, new Vector3(-hx + .3f, -.05f, 0), new Vector3(.7f, .2f, pit.size.y));
            for (int k = 0; k < 9; k++) b.Box(WetSetts, new Vector3(-hx - .4f - R() * 1.6f, .05f, (R() - .5f) * 3), Vector3.one * (.1f + R() * .14f), Quaternion.Euler(R() * 40, R() * 90, 0));

            // The railing: iron posts and two rails along every side but the gap where the lane ends (the west side's middle). Rusted
            // and bent; by the gap the rails hang snapped. Each side solid (Blocker: a collider cut out of the navmesh).
            void Rail(Vector3 a, Vector3 c, bool snappedEnd)
            {
                var run = c - a; float len = run.magnitude; if (len < .2f) return;
                var q = Quaternion.LookRotation(run / len); var mid = (a + c) / 2;
                for (float s = 0; s <= len + .01f; s += 1.5f) b.Box(iron, a + run / len * s + Vector3.up * .55f, new Vector3(.09f, 1.1f, .09f));
                b.Box(iron, mid + Vector3.up * 1.05f, new Vector3(.06f, .06f, len), q);
                b.Box(iron, mid + Vector3.up * .55f, new Vector3(.05f, .05f, len), q);
                if (snappedEnd) b.Box(iron, c + Vector3.up * .6f - run / len * .3f + Vector3.down * .3f, new Vector3(.05f, .05f, .9f), q * Quaternion.Euler(50, 0, 0));
                Blocker(root, mid + Vector3.up * .7f, new Vector3(.3f, 1.4f, len), q);
            }
            float rx = hx + .15f, rz = hz + .15f, g = DropGap / 2;
            Rail(new Vector3(-rx, 0, rz), new Vector3(rx, 0, rz), false);
            Rail(new Vector3(-rx, 0, -rz), new Vector3(rx, 0, -rz), false);
            Rail(new Vector3(rx, 0, -rz), new Vector3(rx, 0, rz), false);
            Rail(new Vector3(-rx, 0, rz), new Vector3(-rx, 0, g), true);
            Rail(new Vector3(-rx, 0, -rz), new Vector3(-rx, 0, -g), true);
            spot.gap = root.TransformPoint(new Vector3(-rx, 0, 0));

            // The net, three stories down: a rotten weave of grey-green cords sagging between the walls, rags and fungus caught in it.
            var cord = Tint(art.cloth, new Color(.3f, .32f, .26f)); var rot2 = Tint(art.cloth, new Color(.22f, .2f, .16f));
            float ny = net - lip;
            float Sag(float x, float z) { return -.35f * (1 - Mathf.Clamp01((x * x) / (half.x * half.x))) * (1 - Mathf.Clamp01((z * z) / (half.y * half.y))); }
            for (float x = -half.x; x <= half.x + .01f; x += .55f)
                for (float z = -half.y; z < half.y - .01f; z += .5f)
                {
                    float y0 = Sag(x, z), y1 = Sag(x, z + .5f);
                    // Rotted through here and there.
                    if (R() < .05f) continue;
                    b.Box(R() < .3f ? rot2 : cord, new Vector3(x, ny + (y0 + y1) / 2, z + .25f), new Vector3(.05f, .05f, .52f), Quaternion.Euler(Mathf.Atan2(y0 - y1, .5f) * Mathf.Rad2Deg, 0, 0));
                }
            for (float z = -half.y; z <= half.y + .01f; z += .55f)
                for (float x = -half.x; x < half.x - .01f; x += .5f)
                {
                    float y0 = Sag(x, z), y1 = Sag(x + .5f, z);
                    if (R() < .05f) continue;
                    b.Box(R() < .3f ? rot2 : cord, new Vector3(x + .25f, ny + (y0 + y1) / 2, z), new Vector3(.52f, .05f, .05f), Quaternion.Euler(0, 0, Mathf.Atan2(y1 - y0, .5f) * Mathf.Rad2Deg));
                }
            var mould = DrainMould; var fungus = Tint(art.stone, new Color(.42f, .4f, .3f));
            for (int k = 0; k < 16; k++)
            {
                float x = (R() - .5f) * 2 * half.x * .9f, z = (R() - .5f) * 2 * half.y * .9f, s = .25f + R() * .5f;
                b.Box(k % 3 == 0 ? mould : fungus, new Vector3(x, ny + Sag(x, z) + .04f, z), new Vector3(s, .08f + R() * .1f, s * (.6f + R() * .6f)), Quaternion.Euler(0, R() * 90, 0));
            }
            // Where the net is made fast: iron pins in the walls and the cords' knots round them.
            for (float x = -half.x; x <= half.x + .01f; x += 1.1f) foreach (int s in new[] { -1, 1 }) b.Box(iron, new Vector3(x, ny + .02f, s * (half.y + .05f)), new Vector3(.12f, .12f, .25f));
            for (float z = -half.y; z <= half.y + .01f; z += 1.1f) foreach (int s in new[] { -1, 1 }) b.Box(iron, new Vector3(s * (half.x + .05f), ny + .02f, z), new Vector3(.25f, .12f, .12f));
            var netBody = new GameObject("The fungal net"); netBody.transform.SetParent(root, false); netBody.transform.localPosition = new Vector3(0, ny - .15f, 0);
            // Wall to wall and under the sheer sides too: whoever slides down a side lands on it.
            netBody.AddComponent<BoxCollider>().size = new Vector3(pit.size.x, .3f, pit.size.y);
            var glow = Glow(root, new Vector3(0, ny + .6f, 0), 7, .25f, MouldGlow, .8f); glow.name = "Mould on the net";

            // The dark under it: fog sheets one under another (the veil's static, near black) and a black floor far down.
            var dark = Tint(art.stone, new Color(.02f, .02f, .025f));
            b.Box(dark, new Vector3(0, -pit.depth + .2f, 0), new Vector3(half.x * 2 + .4f, .3f, half.y * 2 + .4f));
            if (art.veil != null)
            {
                var fog = Tint(art.veil, new Color(.16f, .15f, .14f, .55f)); var fog2 = Tint(art.veil, new Color(.08f, .08f, .08f, .75f));
                int n = 0;
                for (float y = ny - 3.5f; y > -pit.depth + 2; y -= 5.5f, n++)
                    Part(PrimitiveType.Cube, root, new Vector3((R() - .5f) * .6f, y, (R() - .5f) * .6f), new Vector3(half.x * 2, .04f, half.y * 2), n == 0 ? fog : fog2, Quaternion.Euler(0, R() * 4 - 2, 0)).name = "The dark under the net";
            }

            // The iron rungs (GAME-ONLY): up the north wall from the net to a drain-cut a metre under the lip's coping.
            float rungX = half.x * .45f, wallZ = half.y + .02f, top = -2.2f;
            for (float y = ny + .3f; y < top; y += .35f) b.Box(iron, new Vector3(rungX, y, wallZ - .12f), new Vector3(.5f, .045f, .045f));
            foreach (int s in new[] { -1, 1 }) b.Box(iron, new Vector3(rungX + s * .27f, (ny + top) / 2, wallZ - .08f), new Vector3(.05f, top - ny, .05f));
            b.Box(dark, new Vector3(rungX, top + .55f, wallZ + .05f), new Vector3(1.1f, 1.2f, .12f));
            b.Box(cope, new Vector3(rungX, top + 1.2f, wallZ - .05f), new Vector3(1.4f, .15f, .2f));
            var stand = root.TransformPoint(new Vector3(rungX, ny, wallZ - .9f));
            spot.rungs = new ZoneInteractable { name = RungsName, prompt = "Climb the iron rungs", kind = "rungs", position = stand, root = null };
            Interactables.Add(spot.rungs);

            b.Flush(this, root, "Dead Man's Drop");
            LowtownDrop = spot;
            Debug.Log("Lowtowns: Dead Man's Drop at " + centre + ", the net " + NetBelow + " m down, " + pit.depth + " m to the dark.");
        }
    }
}
