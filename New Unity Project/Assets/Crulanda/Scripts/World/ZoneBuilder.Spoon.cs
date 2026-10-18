using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Rusty Spoon (inn variant 4; the inn is CANON, book1 ch.1:9: "a decrepit establishment in the Lowtowns known for watered-down
    /// ale and high-stakes games"; its building and rooms GAME-ONLY, p9-spoon). Chris, 2026-10-16: "same damn inn in the lowlands as well.
    /// each zone has to be unique not reused assets". Its own house, inside and out, and like no other inn:
    /// - outside: the ground floor of an old soot-brick warehouse, its loading arch bricked up and a barred window grimed orange; over it
    ///   an upper floor of tarred weatherboard jettied out over the lane, and the whole roof sways down in the middle (each half of the
    ///   upper floor and roof hangs from its gable end, 2.2 degrees down to the middle); two raking shores prop the -x end; patches of
    ///   tarred boards where the slates are gone; a crooked brick stack over the hearth; a pentice of tarred boards on two crooked posts
    ///   over the double door (the rain's shelter, as UnderPorch has it); a painted board over the door, and the sign: a great rusted iron
    ///   spoon hung from a bracket; the cellar flap round the west side (the Spoon's slate hangs by it); puddles at the door;
    /// - inside, a low room (beams at 2.6 m on a sagging girder): the long scarred bar down the -x end with casks behind it, the water
    ///   butt among them, the cellar trapdoor, and Mother Sall's slate of debts with its BROKEN CHAIRS column run off onto the brick; the
    ///   dice pit in the middle of the floor, a ring of planks round the sand, kegs to squat on; five mismatched tables (round, square, a
    ///   cask with a board on it) and mismatched chairs, one leg short on every third; the brick hearth on the +x end with its bench; the
    ///   back room in boards for the high stakes, a curtain over its way in, a card table under one low lamp, a strongbox; orange lamps.
    /// The room is its plan's (<see cref="InnPlan"/>, variant 4): what is built, what the tests measure and what the life reads (its seats,
    /// stools and bench; ZoneInn "tavern", staffed). Solid: the walls, the bar, the hearth, the back room's board walls, the pentice's
    /// posts. The kitchen lean-to on the back wall is the zone's "kitchen" prop (ZoneBuilder.Workshops), in tarred timber here. No
    /// machines. Draws nothing from the zone's stream.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The Spoon's front window, its boards and broken glass after a brawl, and the lane outside it where the thrown one lands
        /// (EncounterSession.Spoon finds them by these names under the inn's frame).</summary>
        public const string SpoonWindowName = "Spoon front window", SpoonBoardsName = "Spoon window boards", SpoonLaneName = "Spoon window lane", SpoonCardsName = "Spoon card table";
        /// <summary>Old iron gone to rust: the Spoon's sign.</summary>
        Material SpoonRust { get { return Tint(art.metal, new Color(.5f, .23f, .1f)); } }

        void SpoonInn(Transform t, Vector2 size)
        {
            var plan = InnPlan.For(size, InnPlan.SpoonVariant);
            float w = plan.w, d = plan.d, wall = plan.wall, doorW = plan.doorW, doorH = plan.doorH, hx = plan.Inner.x, hz = plan.Inner.y;
            // The ground floor's brick to just over the ceiling; the upper floor and roof above it are for the eye (and the camera).
            const float storey = 2.9f, brickTop = 3.2f, H = 5.5f, roofH = 3.4f, sag = 2.2f;
            float face = -(d / 2 + wall / 2);
            var brick = SootBrick(5); var band = SootBrick(3); var tar = TarredTimber; var iron = SootIron; var rust = SpoonRust;
            var boards = Tint(art.timber, new Color(.25f, .18f, .12f)); var worn = Tint(art.timber, new Color(.36f, .26f, .17f));
            Material[] woods = { Tint(art.timber, new Color(.42f, .3f, .19f)), Tint(art.timber, new Color(.24f, .17f, .11f)), Tint(art.timber, new Color(.5f, .4f, .28f)), Tint(art.timber, new Color(.33f, .2f, .14f)) };
            var cylinder = PrimitiveMesh(PrimitiveType.Cylinder); var sphere = PrimitiveMesh(PrimitiveType.Sphere);
            var parts = new Batch();
            void Cyl(Material m, Vector3 at, Vector3 scale, Quaternion? rot = null) { parts.AddMesh(cylinder, m, Matrix4x4.TRS(at, rot ?? Quaternion.identity, scale)); }
            GameObject WallPiece(Vector3 pos, Vector3 scale) { var o = BoxPart(t, pos, scale, brick); o.AddComponent<BoxCollider>(); o.AddComponent<NavBlocker>(); return o; }

            // ---------- the shell: the floor, the brick walls (each its own collider and navmesh blocker), the low ceiling ----------
            Part(PrimitiveType.Cube, t, new Vector3(0, .02f, 0), new Vector3(w - wall, .05f, d - wall), boards);
            HallFloor(t, w - wall, d - wall, .045f);
            WallPiece(new Vector3(0, brickTop / 2, d / 2), new Vector3(w, brickTop, wall));
            foreach (int s in new[] { -1, 1 }) WallPiece(new Vector3(s * w / 2, brickTop / 2, 0), new Vector3(wall, brickTop, d));
            float side = (w - doorW) / 2;
            foreach (int s in new[] { -1, 1 })
            {
                // The jambs' colliders stop JambShort short of the opening, as every inn's (the navmesh's way through is NavGap's).
                var jamb = WallPiece(new Vector3(s * (doorW / 2 + side / 2), brickTop / 2, -d / 2), new Vector3(side, brickTop, wall)).GetComponent<BoxCollider>();
                jamb.size -= new Vector3(JambShort, 0, 0); jamb.center += new Vector3(s * JambShort / 2, 0, 0);
            }
            WallPiece(new Vector3(0, doorH + (brickTop - doorH) / 2, -d / 2), new Vector3(doorW, brickTop - doorH, wall));
            // The ceiling, solid so the camera pulls in under it: dark boards on heavy beams across the room (their feet at 2.49 m), the one
            // over the middle sagging (two halves, each a degree and a half down to the middle).
            Part(PrimitiveType.Cube, t, new Vector3(0, storey - .1f, 0), new Vector3(w - wall, .15f, d - wall), Tint(art.timber, new Color(.17f, .13f, .1f))).AddComponent<BoxCollider>();
            for (float x = -hx + .9f; x < hx - .3f; x += 1.4f)
            {
                if (Mathf.Abs(x + .35f) > .05f) { parts.Box(tar, new Vector3(x, storey - .29f, 0), new Vector3(.2f, .24f, d - wall)); continue; }
                float half = (d - wall) / 2;
                foreach (int s in new[] { -1, 1 }) parts.Box(tar, new Vector3(x, storey - .29f - half / 2 * Mathf.Sin(1.5f * Mathf.Deg2Rad), s * half / 2), new Vector3(.2f, .24f, half + .02f), Quaternion.Euler(-s * 1.5f, 0, 0));
            }

            // ---------- outside: the footing, the brick front with its bricked-up loading arch and grimed window, the door ----------
            float? kitchen = KitchenBehind(t, d / 2 + wall / 2);
            Footing(t, w + .5f, d + .5f, .3f, .35f, .5f, doorW / 2 + .05f, kitchen.HasValue ? new Vector2(kitchen.Value - KitchenSize.x / 2 - .1f, kitchen.Value + KitchenSize.x / 2 + .1f) : (Vector2?)null, band);
            // The loading arch, bricked up: two piers, a ring of voussoirs, the newer brick filling it, a small barred light high in it.
            var arch = new Vector3(w / 2 - 2.6f, 1.5f, face - .03f); const float span = 1.2f;
            foreach (int s in new[] { -1, 1 }) parts.Box(band, arch + new Vector3(s * (span + .14f), -.75f + .02f, 0), new Vector3(.28f, 1.54f, .06f));
            for (int k = 0; k <= 8; k++)
            {
                float a = Mathf.Lerp(10, 170, k / 8f) * Mathf.Deg2Rad;
                parts.Box(band, arch + new Vector3(Mathf.Cos(a) * (span + .14f), Mathf.Sin(a) * (span + .14f), 0), new Vector3(.3f, .24f, .06f), Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90));
            }
            parts.Box(SootBrick(0), arch + new Vector3(0, -.15f, .015f), new Vector3(span * 2, 1.8f, .03f));
            parts.Box(SootBrick(0), arch + new Vector3(0, .65f, .015f), new Vector3(span * 1.6f, .5f, .03f));
            parts.Box(DarkPane, arch + new Vector3(0, .45f, -.01f), new Vector3(.5f, .3f, .03f));
            for (int k = -1; k <= 1; k++) parts.Box(iron, arch + new Vector3(k * .13f, .45f, -.035f), new Vector3(.025f, .34f, .025f));
            // The taproom's front window left of the door: wide, low, barred, its glass grimed to orange (it passes through the wall).
            var win = new Vector3(-w / 2 + 2.7f, 1.45f, -d / 2);
            Part(PrimitiveType.Cube, t, win, new Vector3(1.6f, .85f, wall + .1f), Tint(art.glass, new Color(1, .7f, .4f))).name = SpoonWindowName;
            // (The rework, Lowtowns pick 5, 2026-10-18) The window a brawl ends through, "through the shattered front window ... into the cold mud
            // of the alleyway" (CANON, b1 ch.1:121): planks nailed over the hole and the glass in the lane's mud, hidden until a brawl breaks it
            // (EncounterSession.Spoon shows them, and puts the pane back when Snivels has had it glazed); where the thrown one lands, outside it.
            {
                var boarded = new GameObject(SpoonBoardsName).transform; boarded.SetParent(t, false);
                for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, boarded, win + new Vector3(0, -.28f + k * .28f, -wall / 2 - .1f), new Vector3(1.8f, .2f, .04f), boards, Quaternion.Euler(0, 0, (k - 1) * 4.5f));
                for (int k = 0; k < 7; k++) Part(PrimitiveType.Cube, boarded, new Vector3(win.x - .7f + (k * 37 % 14) * .1f, .02f, -d / 2 - .5f - (k * 53 % 9) * .12f), new Vector3(.14f + (k % 3) * .05f, .01f, .1f), Tint(art.glass, new Color(1, .7f, .4f)), Quaternion.Euler(0, k * 47, 0));
                boarded.gameObject.SetActive(false);
                new GameObject(SpoonLaneName).transform.SetParent(t, false);
                t.Find(SpoonLaneName).localPosition = new Vector3(win.x, 0, -d / 2 - 1.8f);
            }
            parts.Box(band, win + new Vector3(0, .54f, -.02f), new Vector3(1.9f, .22f, wall + .14f));
            parts.Box(band, win + new Vector3(0, -.5f, -.05f), new Vector3(1.8f, .1f, wall + .2f));
            for (int k = 0; k < 5; k++) parts.Box(iron, win + new Vector3(-.64f + k * .32f, 0, -wall / 2 - .07f), new Vector3(.03f, .92f, .03f));
            // Small barred lights: the -x end over the casks, the +x end into the back room, the back wall by the rooms door.
            foreach (var (at, alongX) in new[] { (new Vector3(-w / 2, 2.2f, -.6f), false), (new Vector3(w / 2, 2.1f, 2.8f), false), (new Vector3(-4.6f, 1.7f, d / 2), true) })
            {
                Part(PrimitiveType.Cube, t, at, alongX ? new Vector3(.7f, .5f, wall + .1f) : new Vector3(wall + .1f, .5f, .7f), Tint(art.glass, new Color(1, .66f, .36f)));
                float o = (wall / 2 + .06f) * (at.x < -w / 2 + .1f ? -1 : at.x > w / 2 - .1f ? 1 : 0);
                float oz = alongX ? wall / 2 + .06f : 0;
                for (int k = -1; k <= 1; k++) parts.Box(iron, at + (alongX ? new Vector3(k * .2f, 0, oz) : new Vector3(o, 0, k * .2f)), new Vector3(.025f, .56f, .025f));
                parts.Box(band, at + new Vector3(0, .33f, 0), alongX ? new Vector3(.9f, .14f, wall + .12f) : new Vector3(wall + .12f, .14f, .9f));
            }
            // The door: two leaves of tarred planks on hinges at the jambs (DoubleDoor), its posts and lintel of tarred timber with iron
            // straps, on a stone threshold.
            var innDoor = DoubleDoor(t, -d / 2, doorW, doorH, tar, t.name);
            DoorSteps(t, 0, face, .05f, doorW + .4f, wall, band);
            Doors.Add(innDoor); innDoor.SetOpen(true);
            foreach (int s in new[] { -1, 1 })
            {
                parts.Box(tar, new Vector3(s * (doorW / 2 + .08f), (doorH + .1f) / 2, -d / 2 - .03f), new Vector3(.16f, doorH + .1f, wall + .1f));
                foreach (float y in new[] { .5f, 1.9f }) parts.Box(iron, new Vector3(s * (doorW / 2 + .08f), y, face - .09f), new Vector3(.2f, .06f, .02f));
            }
            parts.Box(tar, new Vector3(0, doorH + .1f, -d / 2 - .03f), new Vector3(doorW + .36f, .2f, wall + .1f));
            // The painted board over the door, nailed across the jetty's weatherboard above the pentice: oxblood, a cream spoon painted on
            // it end to end, a border.
            var board = new Vector3(0, 3.55f, -(d / 2 + .39f));
            parts.Box(Tint(art.timber, new Color(.38f, .1f, .07f)), board, new Vector3(3.8f, .42f, .04f));
            parts.Box(tar, board + new Vector3(0, .23f, -.01f), new Vector3(3.9f, .05f, .06f));
            parts.Box(tar, board + new Vector3(0, -.23f, -.01f), new Vector3(3.9f, .05f, .06f));
            var cream = Tint(art.plaster, new Color(.86f, .8f, .64f));
            parts.Box(cream, board + new Vector3(-.35f, 0, -.03f), new Vector3(1.9f, .07f, .01f));
            parts.AddMesh(sphere, cream, Matrix4x4.TRS(board + new Vector3(.9f, 0, -.03f), Quaternion.identity, new Vector3(.62f, .28f, .01f)));
            // The pentice over the door: tarred boards falling from the wall to a beam on two crooked posts (each solid, its own hole in
            // the navmesh), a ceiling overhead (WorldWeather looks up; no rain under it: UnderPorch).
            const float deep = 2.5f, high = 2.78f, low = 2.42f, px = 2.9f; float zp = face - deep + .2f;
            float fall = Mathf.Atan2(high - low, deep) * Mathf.Rad2Deg;
            var pentice = BoxPart(t, new Vector3(0, (high + low) / 2 + .05f, face - deep / 2), new Vector3(px * 2 + .6f, .08f, Mathf.Sqrt(deep * deep + (high - low) * (high - low))), tar, Quaternion.Euler(-fall, 0, 0), 1);
            pentice.name = "Pentice"; pentice.AddComponent<BoxCollider>();
            foreach (int k in new[] { -1, 1 })
            {
                float g = LocalGround(t, k * px, zp), lean = k < 0 ? 3 : -1.5f;
                var post = Stake(t, new Vector3(k * px, g - .1f, zp), .2f, low - .1f - g + .1f, tar, Quaternion.Euler(0, 0, lean)); post.AddComponent<BoxCollider>(); post.AddComponent<NavBlocker>();
            }
            parts.Box(tar, new Vector3(0, low - .1f, zp), new Vector3(px * 2 + .3f, .16f, .16f));
            foreach (int k in new[] { -1, 1 }) Bar(t, new Vector3(k * px, low - .7f, zp), new Vector3(k * (px - .6f), low - .12f, zp), .1f, .08f, tar);
            // The door's lantern on an iron bracket, lit orange (faint by day).
            var lamp = new Vector3(doorW / 2 + .55f, 2.05f, face - .3f);
            Part(PrimitiveType.Cube, t, lamp, new Vector3(.22f, .3f, .22f), Tint(art.glass, new Color(1, .62f, .3f)));
            parts.Box(iron, lamp + new Vector3(0, .18f, 0), new Vector3(.28f, .05f, .28f));
            parts.Box(iron, lamp + new Vector3(0, .22f, .16f), new Vector3(.04f, .04f, .34f));
            var lantern = Glow(t, lamp, 8, .6f, LampOrange, 2.2f); lantern.name = "Inn lantern";
            // Puddles at the door, past the pentice's drip (flat black water on the setts).
            foreach (var (x, z, r, turn) in new[] { (-.7f, -2.9f, .75f, 20f), (1.2f, -3.5f, .5f, -35f), (-2.3f, -3.3f, .4f, 70f) })
                Part(PrimitiveType.Cylinder, t, new Vector3(x, LocalGround(t, x, face + z) + .085f, face + z), new Vector3(r * 2, .004f, r * 1.4f), BlackWater, Quaternion.Euler(0, turn, 0));
            // The cellar flap round the west side, flush with the ground (the Spoon's slate hangs on a nail by it): two iron-banded leaves.
            {
                float fx = -w / 2 - .78f, fz = 3.95f, g = LocalGround(t, fx, fz) + .03f;
                parts.Box(band, new Vector3(fx, g - .01f, fz), new Vector3(.9f, .04f, 1.3f));
                foreach (int s in new[] { -1, 1 })
                {
                    parts.Box(tar, new Vector3(fx, g + .02f, fz + s * .29f), new Vector3(.76f, .04f, .54f));
                    parts.Box(iron, new Vector3(fx, g + .045f, fz + s * .29f), new Vector3(.7f, .01f, .06f));
                }
                Cyl(iron, new Vector3(fx + .25f, g + .05f, fz), new Vector3(.12f, .01f, .12f));
            }
            // Two raking shores of tarred timber prop the -x end (it leans): from sole plates on the ground up to plates on the wall.
            foreach (float z in new[] { -1.5f, -3.6f })
            {
                float fx = -w / 2 - 1.55f, g = LocalGround(t, fx, z);
                Bar(t, new Vector3(fx, g + .05f, z), new Vector3(-w / 2 - .32f, 3.5f, z), .22f, .2f, tar);
                parts.Box(tar, new Vector3(fx + .1f, g + .06f, z), new Vector3(.7f, .14f, .3f));
                parts.Box(tar, new Vector3(-w / 2 - .3f, 3.2f, z), new Vector3(.08f, 1.4f, .3f));
                Blocker(t, new Vector3(fx + .25f, g + .5f, z), new Vector3(.5f, 1, .4f));
            }

            // ---------- the upper floor and the roof, swayed: each half hangs from its gable end and drops toward the middle ----------
            float run = d / 2 + .7f, pitch = Mathf.Atan2(roofH, run) * Mathf.Rad2Deg, reach = w / 2 + .6f, length = reach + .15f;
            var weather = Tint(art.timber, new Color(.15f, .13f, .11f));
            foreach (int s in new[] { -1, 1 })
            {
                var half = new GameObject(s < 0 ? "Upper floor (-x)" : "Upper floor (+x)").transform; half.SetParent(t, false);
                half.localPosition = new Vector3(s * reach, H, 0); half.localRotation = Quaternion.Euler(0, 0, s * sag);
                var halfParts = new Batch();
                // Along this half: u metres in from the gable end (the pivot), as a local x.
                float U(float u) { return -s * u; }
                MeshPart(ZoneMeshes.GableRoof(length, d + 1.4f, roofH, .25f, 0), half, new Vector3(U(length / 2), 0, 0), LowSlate);
                halfParts.Box(tar, new Vector3(U(length / 2), roofH + .05f, 0), new Vector3(length, .12f, .3f));
                // The weatherboarded front and back, jettied .2 out over the brick, their tops tucked up under the roof's slab; a sagging
                // bressumer along their foot.
                float from = .31f, to = reach + .1f, mid = (from + to) / 2, len = to - from, top = .2f, foot = brickTop - H;
                foreach (int sz in new[] { -1, 1 })
                {
                    halfParts.Box(weather, new Vector3(U(mid), (top + foot) / 2, sz * (d / 2 + .28f)), new Vector3(len, top - foot, .14f));
                    for (float y = foot + .3f; y < top - .1f; y += .3f) halfParts.Box(tar, new Vector3(U(mid), y, sz * (d / 2 + .36f)), new Vector3(len, .03f, .03f));
                    halfParts.Box(tar, new Vector3(U(mid), foot + .1f, sz * (d / 2 + .3f)), new Vector3(len, .24f, .24f));
                    // Its windows: one lit, one boarded up, front and back (the back's dark).
                    foreach (var (u, kind) in new[] { (2.3f, s < 0 ? 0 : 1), (5.3f, s < 0 ? 1 : 2) })
                    {
                        var at = new Vector3(U(u), foot + 1.35f, sz * (d / 2 + .28f));
                        bool lit = sz < 0 && kind == 0, cracked = sz < 0 && kind == 2;
                        halfParts.Box(lit || cracked ? Tint(art.glass, new Color(1, .64f, .32f)) : DarkPane, at, new Vector3(.7f, .8f, .18f));
                        if (cracked) halfParts.Box(DarkPane, at + new Vector3(-s * .15f, .18f, sz * .02f), new Vector3(.32f, .36f, .18f), Quaternion.Euler(0, 0, 14));
                        foreach (int k in new[] { -1, 1 }) halfParts.Box(tar, at + new Vector3(0, k * .44f, sz * .04f), new Vector3(.86f, .08f, .16f));
                        if (sz > 0 || kind == 1)
                        {
                            // Boarded: two planks nailed across it.
                            foreach (int k in new[] { -1, 1 }) halfParts.Box(worn, at + new Vector3(0, 0, sz * .11f), new Vector3(.9f, .1f, .03f), Quaternion.Euler(0, 0, k * 32));
                        }
                    }
                }
                // Tarred boards where slates are gone: one patch on the front slope of each half, one on the back of the +x half.
                foreach (var (u, f, back) in s < 0 ? new[] { (3.2f, .4f, false) } : new[] { (5.6f, .55f, false), (2.6f, .35f, true) })
                {
                    int sz = back ? 1 : -1;
                    var spot = new Vector3(U(u), roofH * f + .04f, sz * run * (1 - f)); var slope = Quaternion.Euler(back ? pitch : -pitch, 0, 0);
                    halfParts.Box(tar, spot, new Vector3(1.3f, .05f, 1.1f), slope);
                    for (int k = -1; k <= 1; k++) halfParts.Box(boards, spot + slope * new Vector3(k * .42f, .03f, 0), new Vector3(.06f, .02f, 1.1f), slope);
                }
                halfParts.Flush(this, half, "Spoon upper floor");
                // The gable end: weatherboard from the brick up to the eaves, and the gable over it (at the pivot: it does not sway).
                var endWall = BoxPart(t, new Vector3(s * (w / 2 + .22f), (storey + H) / 2, 0), new Vector3(.14f, H - storey, d + .56f), weather, null, 1);
                endWall.name = "Gable end";
                MeshPart(ZoneMeshes.Gable(d + .56f, roofH, .14f, 1, run - (d / 2 + .28f)), t, new Vector3(s * (w / 2 + .22f), H, 0), weather, Quaternion.Euler(0, s * 90, 0));
                for (float y = storey + .3f; y < H + roofH * .7f; y += .3f)
                {
                    float wide = 2 * Mathf.Min(d / 2 + .28f, run * (1 - (y - H) / roofH)) - .2f;
                    if (wide > .3f) parts.Box(tar, new Vector3(s * (w / 2 + .3f), y, 0), new Vector3(.03f, .03f, wide));
                }
            }
            // The upper floor's bulk, for the camera (nothing walks up there).
            var upper = new GameObject("Upper storey"); upper.transform.SetParent(t, false);
            var upperBox = upper.AddComponent<BoxCollider>(); upperBox.center = new Vector3(0, (brickTop + H) / 2 - .2f, 0); upperBox.size = new Vector3(w, H - brickTop - .4f, d);
            // The chimney stack over the hearth: brick, its top knocked askew, two pots, smoke.
            {
                var stack = new Vector3(w / 2 - .45f, 0, plan.hearth.y);
                BoxPart(t, stack + new Vector3(0, 5.6f, 0), new Vector3(.9f, 5.4f, 1), brick, null, 1);
                var cap = new GameObject("Chimney top").transform; cap.SetParent(t, false); cap.localPosition = stack + new Vector3(0, 8.3f, 0); cap.localRotation = Quaternion.Euler(4, 0, -6);
                BoxPart(cap, new Vector3(0, .4f, 0), new Vector3(.86f, .8f, .96f), brick, null, 1);
                BoxPart(cap, new Vector3(0, .84f, 0), new Vector3(1.02f, .1f, 1.12f), band, null, 1);
                foreach (float z in new[] { -.22f, .22f }) Part(PrimitiveType.Cylinder, cap, new Vector3(0, 1.08f, z), new Vector3(.24f, .2f, .24f), ClayBrick);
                if (art.particle != null) Smoke(cap, new Vector3(0, 1.4f, .22f));
            }
            // The sign: a great iron spoon gone to rust, hung by two chains from a bracket on the upper floor, bowl down.
            {
                float sx = w / 2 - 3.4f, drop = (reach - sx) * Mathf.Tan(sag * Mathf.Deg2Rad), sy = 4.1f - drop, sface = -(d / 2 + .35f);
                parts.Box(iron, new Vector3(sx, sy, sface - .02f), new Vector3(.12f, .5f, .05f));
                Bar(t, new Vector3(sx, sy, sface), new Vector3(sx, sy, sface - 1.3f), .07f, .07f, iron);
                Bar(t, new Vector3(sx, sy - .45f, sface), new Vector3(sx, sy - .05f, sface - .75f), .05f, .05f, iron);
                Part(PrimitiveType.Sphere, t, new Vector3(sx, sy + .02f, sface - 1.3f), Vector3.one * .1f, iron);
                float zs = sface - 1.05f, hangTop = sy - .05f, handleTop = sy - .3f;
                foreach (float dz in new[] { -.06f, .06f }) Rod(t, new Vector3(sx, hangTop, zs + dz), new Vector3(sx, handleTop, zs), .015f, iron);
                var spoon = new GameObject("Rusty spoon").transform; spoon.SetParent(t, false); spoon.localPosition = new Vector3(sx, handleTop, zs); spoon.localRotation = Quaternion.Euler(0, 90, 4);
                Part(PrimitiveType.Cube, spoon, new Vector3(0, -.4f, 0), new Vector3(.1f, .8f, .035f), rust);
                Part(PrimitiveType.Sphere, spoon, new Vector3(0, -.02f, 0), new Vector3(.14f, .1f, .04f), rust);
                Part(PrimitiveType.Sphere, spoon, new Vector3(0, -1.1f, 0), new Vector3(.46f, .64f, .12f), rust);
                Part(PrimitiveType.Sphere, spoon, new Vector3(0, -1.12f, -.035f), new Vector3(.36f, .52f, .06f), Tint(art.metal, new Color(.3f, .14f, .07f)));
            }

            // ---------- the bar down the -x end: long, scarred, the casks behind it ----------
            var bar = BoxPart(t, new Vector3(plan.bar.x, .53f, plan.bar.y), new Vector3(plan.barSize.x, 1.06f, plan.barSize.y), boards, null, 1);
            bar.name = "Bar"; bar.AddComponent<BoxCollider>(); bar.AddComponent<NavBlocker>();
            float barFront = plan.bar.x + plan.barSize.x / 2, barTop = 1.1f;
            parts.Box(worn, new Vector3(plan.bar.x + .05f, barTop - .02f, plan.bar.y), new Vector3(plan.barSize.x + .25f, .07f, plan.barSize.y + .2f));
            // Its scars: knife cuts and burns across the top, every one a little different.
            for (int k = 0; k < 9; k++)
            {
                float z = plan.bar.y - plan.barSize.y / 2 + .35f + k * .52f, x = plan.bar.x + ((k * 37) % 5 - 2) * .07f;
                parts.Box(k % 3 == 0 ? Tint(art.timber, new Color(.1f, .07f, .05f)) : Tint(art.timber, new Color(.55f, .44f, .32f)), new Vector3(x, barTop + .016f, z), new Vector3(.32f + (k % 4) * .06f, .005f, .02f), Quaternion.Euler(0, 25 + k * 31 % 120, 0));
            }
            // The front's planks (seams), a kick of iron along its foot.
            for (float z = plan.bar.y - plan.barSize.y / 2 + .25f; z < plan.bar.y + plan.barSize.y / 2; z += .5f) parts.Box(tar, new Vector3(barFront + .005f, .53f, z), new Vector3(.01f, 1.0f, .025f));
            parts.Box(iron, new Vector3(barFront + .01f, .08f, plan.bar.y), new Vector3(.02f, .14f, plan.barSize.y));
            // On the bar: two tap-casks, mugs, a dice cup.
            foreach (float z in new[] { plan.bar.y + 1.6f, plan.bar.y - 1.9f })
            {
                Cyl(boards, new Vector3(plan.bar.x - .1f, barTop + .22f, z), new Vector3(.4f, .25f, .4f), Quaternion.Euler(90, 0, 0));
                foreach (float dz in new[] { -.16f, .16f }) Cyl(iron, new Vector3(plan.bar.x - .1f, barTop + .22f, z + dz), new Vector3(.42f, .015f, .42f), Quaternion.Euler(90, 0, 0));
                parts.Box(iron, new Vector3(plan.bar.x - .1f, barTop + .12f, z - .27f), new Vector3(.04f, .04f, .1f));
                parts.Box(boards, new Vector3(plan.bar.x - .1f, barTop + .04f, z), new Vector3(.36f, .08f, .3f));
            }
            for (int i = 0; i < 3; i++) ModelProp(t, "Medieval/Prefabs/MugV2", new Vector3(plan.bar.x + .15f, barTop + .015f, plan.bar.y - .8f + i * .9f), i * 70, .13f);
            Cyl(Tint(art.timber, new Color(.15f, .1f, .07f)), new Vector3(plan.bar.x + .2f, barTop + .1f, plan.bar.y + .7f), new Vector3(.14f, .09f, .14f));
            // Behind it: the ale casks along the wall, the water butt among them (the ale's watered: Mother Sall says so), a shelf of jugs
            // over them, the cellar's trapdoor in the floor.
            float behind = -hx + .5f;
            foreach (float z in new[] { -2.3f, -1.5f, -.7f }) Barrel(t, new Vector3(behind, .045f, z), .8f, z * 40, true);
            Barrel(t, new Vector3(behind, .045f, .1f), .95f, 10, true);
            Cyl(art.water, new Vector3(behind, .045f + .93f, .1f), new Vector3(.5f, .005f, .5f));
            parts.Box(boards, new Vector3(-hx + .14f, 1.9f, -1.0f), new Vector3(.26f, .05f, 2.6f));
            for (int k = 0; k < 7; k++) Cyl(k % 2 == 0 ? Tint(art.stone, new Color(.55f, .45f, .32f)) : Tint(art.stone, new Color(.4f, .36f, .3f)), new Vector3(-hx + .14f, 2.03f, -2.1f + k * .36f), new Vector3(.14f, .1f, .14f));
            parts.Box(tar, new Vector3(behind, .055f, 1.5f), new Vector3(.8f, .02f, 1.0f));
            foreach (float z in new[] { 1.2f, 1.8f }) parts.Box(iron, new Vector3(behind, .068f, z), new Vector3(.82f, .01f, .06f));
            Cyl(iron, new Vector3(behind + .25f, .07f, 1.5f), new Vector3(.1f, .008f, .1f));
            // Mother Sall's slate of debts on the back wall by her place: the BROKEN CHAIRS column runs off the slate onto the brick.
            {
                var slate = new Vector3(plan.stand.x - .1f, 1.75f, hz - .03f);
                parts.Box(Tint(art.stone, new Color(.13f, .13f, .14f)), slate, new Vector3(.8f, .6f, .03f));
                parts.Box(tar, slate + new Vector3(0, .32f, -.01f), new Vector3(.86f, .05f, .04f));
                var chalk = Tint(art.plaster, new Color(.88f, .87f, .84f));
                for (int k = 0; k < 6; k++) parts.Box(chalk, slate + new Vector3(-.2f, .2f - k * .08f, -.02f), new Vector3(.24f + (k % 3) * .05f, .012f, .005f));
                for (int k = 0; k < 13; k++) parts.Box(chalk, slate + new Vector3(.22f + (k % 4) * .04f, .2f - k * .09f, -.02f), new Vector3(.012f, .06f, .005f), Quaternion.Euler(0, 0, 12));
            }
            Workplace(t, "bar", new Vector3(plan.stand.x, 0, plan.stand.y), new Vector3(plan.bar.x, 1, plan.bar.y + plan.barSize.y / 2));
            // The stools before the bar: tall, no two alike.
            for (int i = 0; i < plan.stools.Count; i++)
            {
                var (at, _) = plan.stools[i]; var m = woods[i % woods.Length]; float tall = .72f + (i % 2) * .05f;
                Cyl(m, new Vector3(at.x, tall, at.y), new Vector3(.38f, .025f, .38f));
                for (int k = 0; k < 3; k++)
                {
                    float a = (k * 120 + i * 25) * Mathf.Deg2Rad;
                    parts.Box(m, new Vector3(at.x + Mathf.Cos(a) * .12f, tall / 2, at.y + Mathf.Sin(a) * .12f), new Vector3(.04f, tall, .04f), Quaternion.Euler(Mathf.Sin(a) * 6, 0, -Mathf.Cos(a) * 6));
                }
            }
            // The rooms door past Sall's place (barred to the player: the lodgers go up by it), and the kitchen's door if it stands there.
            InnerDoor(t, plan.roomsX, hz, tar);
            Doors.Add(new ZoneDoor { name = t.name + ", upstairs", kind = "rooms", openable = false, position = t.TransformPoint(new Vector3(plan.roomsX, 1, d / 2 - .7f)) });
            if (kitchen.HasValue) InnerDoor(t, kitchen.Value - KitchenDoorX, hz, tar);

            // ---------- the hearth: rough brick on the +x end, an iron grate, a kettle; its bench ----------
            var hearth = BoxPart(t, new Vector3(plan.hearth.x, .75f, plan.hearth.y), new Vector3(plan.hearthSize.x, 1.5f, plan.hearthSize.y), brick, null, 1);
            hearth.name = "Hearth"; hearth.AddComponent<BoxCollider>(); hearth.AddComponent<NavBlocker>();
            float fireFace = plan.hearth.x - plan.hearthSize.x / 2;
            BoxPart(t, new Vector3(plan.hearth.x + .1f, (1.5f + storey - .17f) / 2, plan.hearth.y), new Vector3(.7f, storey - .17f - 1.5f, 1.5f), brick, null, 1);
            Part(PrimitiveType.Cube, t, new Vector3(fireFace - .01f, .5f, plan.hearth.y), new Vector3(.04f, .6f, 1.1f), Tint(art.glass, new Color(1, .5f, .2f)));
            for (int k = -2; k <= 2; k++) parts.Box(iron, new Vector3(fireFace - .05f, .45f, plan.hearth.y + k * .2f), new Vector3(.03f, .5f, .03f));
            parts.Box(iron, new Vector3(fireFace - .05f, .7f, plan.hearth.y), new Vector3(.04f, .04f, 1.0f));
            parts.Box(band, new Vector3(fireFace - .08f, 1.1f, plan.hearth.y), new Vector3(.2f, .16f, plan.hearthSize.y + .1f));
            Cyl(iron, new Vector3(fireFace - .05f, 1.3f, plan.hearth.y + .5f), new Vector3(.3f, .14f, .3f));
            var fire = new GameObject("Hearth fire").AddComponent<Light>(); fire.transform.SetParent(t, false);
            fire.transform.localPosition = new Vector3(plan.fire.x, 1, plan.fire.y); fire.type = LightType.Point; fire.range = 8; fire.intensity = 1.7f; fire.color = new Color(1, .52f, .22f);
            AddStation("fire", t.name, Ground(new Vector2(fire.transform.position.x, fire.transform.position.z)), t);
            StationRoom(t, new Rect(-w / 2, -d / 2, w, d));
            // The bench: a plank on three trestles, facing the fire.
            parts.Box(worn, new Vector3(plan.bench.x, .44f, plan.bench.y), new Vector3(.38f, .06f, 1.7f));
            foreach (float dz in new[] { -.7f, 0, .7f }) parts.Box(boards, new Vector3(plan.bench.x, .21f, plan.bench.y + dz), new Vector3(.34f, .42f, .06f));
            // Firewood stacked in the front corner by the hearth.
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 4 - row; i++)
                    Cyl(art.bark != null ? art.bark : boards, new Vector3(plan.woodpile.x - .35f + row * .11f + i * .22f, .12f + row * .2f, plan.woodpile.y), new Vector3(.2f, .3f, .2f), Quaternion.Euler(90, 0, 0));

            // ---------- the back room for the high stakes: board walls, a curtain over the way in ----------
            float wallTop = storey - .17f;
            foreach (var (at, h) in plan.partitions)
            {
                var piece = BoxPart(t, new Vector3(at.x, wallTop / 2, at.y), new Vector3(h.x * 2, wallTop, h.y * 2), tar, null, 1);
                piece.name = "Back room wall"; piece.AddComponent<BoxCollider>(); piece.AddComponent<NavBlocker>();
                bool alongX = h.x > h.y; float l = alongX ? h.x * 2 : h.y * 2;
                for (float u = -l / 2 + .25f; u < l / 2; u += .3f)
                    foreach (int sd in new[] { -1, 1 })
                        parts.Box(boards, new Vector3(at.x + (alongX ? u : sd * (h.x + .006f)), wallTop / 2, at.y + (alongX ? sd * (h.y + .006f) : u)), alongX ? new Vector3(.03f, wallTop - .1f, .01f) : new Vector3(.01f, wallTop - .1f, .03f));
            }
            {
                float a = plan.partitions[1].at.x + plan.partitions[1].half.x, b = plan.partitions[2].at.x - plan.partitions[2].half.x, z = plan.backRoom.yMin - .075f;
                var cloth = Tint(art.cloth, new Color(.36f, .1f, .09f));
                parts.Box(cloth, new Vector3((a + b) / 2, wallTop - .16f, z - .04f), new Vector3(b - a + .1f, .3f, .03f));
                foreach (float x in new[] { a + .13f, b - .13f }) parts.Box(cloth, new Vector3(x, (wallTop - .3f) / 2 + .1f, z - .04f), new Vector3(.22f, wallTop - .3f - .2f, .05f));
                foreach (float x in new[] { a + .13f, b - .13f }) parts.Box(Tint(art.hay, new Color(.5f, .42f, .26f)), new Vector3(x, 1.1f, z - .07f), new Vector3(.26f, .05f, .03f));
                parts.Box(iron, new Vector3((a + b) / 2, wallTop - .02f, z - .04f), new Vector3(b - a + .2f, .025f, .025f));
            }

            // ---------- the floor: the dice pit, the tables and their mismatched chairs, the card table in the back room ----------
            void Chair(Vector2 at, Vector2 toward, int k, bool highBack)
            {
                var dir = toward - at; float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg; bool shortLeg = k % 3 == 1;
                var q = Quaternion.Euler(shortLeg ? 3.5f : 0, yaw, shortLeg ? -2.5f : 0); var m = woods[(k * 3 + 1) % woods.Length];
                var o = new Vector3(at.x, shortLeg ? .03f : 0, at.y); const float seat = .46f;
                parts.Box(m, o + q * new Vector3(0, seat, 0), new Vector3(.44f, .05f, .42f), q);
                foreach (int sx in new[] { -1, 1 })
                    foreach (int sz in new[] { -1, 1 })
                    {
                        // The short leg: an inch off the one front corner, the chair rocking on it.
                        float leg = shortLeg && sx > 0 && sz > 0 ? seat - .06f : seat;
                        parts.Box(m, o + q * new Vector3(sx * .18f, leg / 2, sz * .17f), new Vector3(.045f, leg, .045f), q);
                    }
                if (k % 4 == 3) return;
                float back = highBack ? .8f : .42f + (k % 2) * .1f;
                parts.Box(m, o + q * new Vector3(0, seat + back / 2, -.19f), new Vector3(.42f, back, .04f), q);
            }
            var tableTop = .78f; var felt = Tint(art.cloth, new Color(.12f, .24f, .15f)); var bone = Tint(art.plaster, new Color(.9f, .86f, .76f)); var coin = Tint(art.metal, new Color(.72f, .56f, .24f));
            for (int n = 0; n < plan.tables.Count; n++)
            {
                var at = plan.tables[n]; var c = new Vector3(at.x, 0, at.y);
                if (n == 0)
                {
                    // The dice pit: sand inside a ring of planks, dice and coins on it, kegs round it to squat on, a lantern low over it.
                    Cyl(Tint(art.hay, new Color(.42f, .36f, .26f)), c + new Vector3(0, .052f, 0), new Vector3(1.36f, .005f, 1.36f));
                    for (int k = 0; k < 12; k++)
                    {
                        float a = k * 30 * Mathf.Deg2Rad;
                        parts.Box(woods[k % 2 == 0 ? 1 : 3], c + new Vector3(Mathf.Cos(a) * .72f, .09f, Mathf.Sin(a) * .72f), new Vector3(.4f, .14f, .08f), Quaternion.Euler(0, -(k * 30 + 90), 0));
                    }
                    foreach (var (dx, dz, turn) in new[] { (.12f, .05f, 20f), (-.08f, -.14f, 55f), (.26f, -.2f, 10f) }) parts.Box(bone, c + new Vector3(dx, .085f, dz), Vector3.one * .06f, Quaternion.Euler(0, turn, 0));
                    foreach (var (dx, dz) in new[] { (-.3f, .2f), (-.25f, .26f), (.05f, .38f), (.33f, .12f), (-.4f, -.15f) }) Cyl(coin, c + new Vector3(dx, .058f, dz), new Vector3(.05f, .004f, .05f));
                    foreach (var (s, f) in plan.seats)
                        if (Vector2.Distance(f, at) < .01f)
                        {
                            Cyl(boards, new Vector3(s.x, .22f, s.y), new Vector3(.42f, .22f, .42f));
                            Cyl(iron, new Vector3(s.x, .36f, s.y), new Vector3(.44f, .015f, .44f));
                        }
                    Part(PrimitiveType.Cube, t, c + new Vector3(0, 2.3f, 0), new Vector3(.2f, .24f, .2f), Tint(art.glass, new Color(1, .62f, .3f)));
                    parts.Box(iron, c + new Vector3(0, 2.44f, 0), new Vector3(.26f, .04f, .26f)); parts.Box(iron, c + new Vector3(0, 2.6f, 0), new Vector3(.02f, .28f, .02f));
                    Glow(t, c + new Vector3(0, 2.2f, 0), 5, .55f, LampOrange, 1.3f).name = "Pit lantern";
                    continue;
                }
                bool cards = n == plan.tables.Count - 1;
                if (cards)
                {
                    // The card table: square, baize on it, cards and stacked coin, a strongbox in the corner, one low lamp.
                    parts.Box(boards, c + new Vector3(0, tableTop, 0), new Vector3(1.2f, .06f, 1.2f));
                    parts.Box(felt, c + new Vector3(0, tableTop + .033f, 0), new Vector3(1.05f, .01f, 1.05f));
                    foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) parts.Box(boards, c + new Vector3(sx * .5f, tableTop / 2, sz * .5f), new Vector3(.07f, tableTop, .07f));
                    foreach (var (dx, dz, turn) in new[] { (-.2f, .1f, 10f), (-.12f, .14f, 35f), (.25f, -.2f, -20f), (.1f, .3f, 80f), (-.3f, -.28f, 5f), (0f, 0f, 45f) }) parts.Box(bone, c + new Vector3(dx, tableTop + .04f, dz), new Vector3(.07f, .004f, .1f), Quaternion.Euler(0, turn, 0));
                    foreach (var (dx, dz, tall) in new[] { (.3f, .3f, .06f), (-.34f, .3f, .1f), (.32f, -.34f, .04f) }) Cyl(coin, c + new Vector3(dx, tableTop + .038f + tall / 2, dz), new Vector3(.06f, tall / 2, .06f));
                    Part(PrimitiveType.Cube, t, c + new Vector3(0, 1.85f, 0), new Vector3(.22f, .26f, .22f), Tint(art.glass, new Color(1, .6f, .28f)));
                    parts.Box(iron, c + new Vector3(0, 2.0f, 0), new Vector3(.28f, .04f, .28f)); parts.Box(iron, c + new Vector3(0, 2.38f, 0), new Vector3(.02f, .7f, .02f));
                    Glow(t, c + new Vector3(0, 1.75f, 0), 4.5f, .6f, LampOrange, 1.4f).name = "Card table lamp";
                    // Where the Triad table's game is played from (EncounterSession.Spoon; Triad, b1 ch.1:45).
                    var cardsMark = new GameObject(SpoonCardsName).transform; cardsMark.SetParent(t, false); cardsMark.localPosition = c;
                    var box = new Vector3(plan.backRoom.xMax - .35f, 0, plan.backRoom.yMax - .3f);
                    parts.Box(boards, box + new Vector3(0, .2f, 0), new Vector3(.6f, .4f, .4f));
                    foreach (float dx in new[] { -.18f, .18f }) parts.Box(iron, box + new Vector3(dx, .2f, 0), new Vector3(.05f, .42f, .42f));
                }
                else
                {
                    switch (n % 3)
                    {
                        case 0:
                            // A round table on one post.
                            Cyl(woods[n % woods.Length], c + new Vector3(0, tableTop, 0), new Vector3(1.15f, .03f, 1.15f));
                            Cyl(boards, c + new Vector3(0, tableTop / 2, 0), new Vector3(.14f, tableTop / 2, .14f));
                            Cyl(boards, c + new Vector3(0, .03f, 0), new Vector3(.6f, .03f, .6f));
                            break;
                        case 1:
                            // A square one on four legs, set crooked.
                            var q = Quaternion.Euler(0, 15 + n * 11, 0);
                            parts.Box(woods[n % woods.Length], c + new Vector3(0, tableTop, 0), new Vector3(1.05f, .06f, 1.05f), q);
                            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) parts.Box(boards, c + q * new Vector3(sx * .45f, tableTop / 2, sz * .45f), new Vector3(.07f, tableTop, .07f), q);
                            break;
                        default:
                            // A cask stood on end with a round board on it.
                            Cyl(boards, c + new Vector3(0, .37f, 0), new Vector3(.66f, .37f, .66f));
                            foreach (float y in new[] { .15f, .6f }) Cyl(iron, c + new Vector3(0, y, 0), new Vector3(.68f, .02f, .68f));
                            Cyl(woods[n % woods.Length], c + new Vector3(0, tableTop, 0), new Vector3(1.05f, .03f, 1.05f));
                            break;
                    }
                    ModelProp(t, "Medieval/Prefabs/MugV2", new Vector3(at.x - .2f, tableTop + .03f, at.y + .15f), n * 60, .13f);
                    Cyl(Tint(art.stone, new Color(.5f, .42f, .3f)), c + new Vector3(.22f, tableTop + .13f, -.1f), new Vector3(.16f, .11f, .16f));
                }
                int seat = 0;
                for (int i = 0; i < plan.seats.Count; i++) if (Vector2.Distance(plan.seats[i].face, at) < .01f) Chair(plan.seats[i].at, at, n * 4 + seat++, cards);
            }
            // A chair broken in the front corner by the bar, its legs beside it (Sall keeps a cellar of legs).
            {
                var heap = new Vector3(-hx + .7f, 0, -hz + .7f);
                parts.Box(woods[2], heap + new Vector3(0, .25f, 0), new Vector3(.44f, .05f, .42f), Quaternion.Euler(0, 30, 80));
                parts.Box(woods[2], heap + new Vector3(.1f, .2f, .15f), new Vector3(.42f, .42f, .04f), Quaternion.Euler(0, 30, 80));
                for (int k = 0; k < 4; k++) parts.Box(woods[k % woods.Length], heap + new Vector3(.3f + k * .08f, .025f, -.25f + k * .07f), new Vector3(.045f, .045f, .46f), Quaternion.Euler(0, 20 + k * 25, 0));
            }

            // ---------- the light: one dim orange lamp over the room, the lanterns over the pit and the card table, the fire ----------
            var room = new GameObject("Taproom light").AddComponent<Light>(); room.transform.SetParent(t, false);
            room.transform.localPosition = new Vector3(-2, storey - .5f, .2f); room.type = LightType.Point; room.range = 9; room.intensity = .95f; room.color = new Color(1, .64f, .34f);
            Part(PrimitiveType.Cube, t, new Vector3(plan.bar.x + .3f, 2.25f, plan.bar.y), new Vector3(.2f, .26f, .2f), Tint(art.glass, new Color(1, .62f, .3f)));
            parts.Box(iron, new Vector3(plan.bar.x + .3f, 2.55f, plan.bar.y), new Vector3(.02f, .3f, .02f));
            parts.Flush(this, t, "Rusty Spoon");
            // Registered: its seats, its stools and its bench, for the Lowtowns' life and the sims (VillageLife.FindPlaces, SimFigure.Inn).
            RegisterInn(t, "tavern", innDoor, plan, plan.Inner);
        }
    }
}
