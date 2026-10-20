using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Rust &amp; Ruin, the Forge District's inn (inn variant 5; the tavern is CANON, book1 ch.24:81-140: Valen kicks "the thick,
    /// iron-banded oak door" in a "perpetually shadowed alleyway"; inside the Lowtowns' cutpurses, smugglers and Copper-Tithe gather and
    /// Valen says "Ring the bell"; its building and room GAME-ONLY. Chris, 2026-10-19: the inn "Fully its own now"). Like no other inn:
    /// - outside: a long low house of soot-black stone, its walls battered at the foot, a shallow iron roof, two barred slit windows, the
    ///   iron-banded oak double door under a stone lintel, an iron flue standing up through the roof's middle over the fire pit, smoking;
    ///   a board over the door, rust-red, and a broken crucible hung from an iron bracket for a sign;
    /// - inside, under a low beamed ceiling: the bar down the +x end on a stone base with an oak top, casks against the wall behind it, the
    ///   bell on its bracket by the keeper's place; a round fire pit out in the floor under an iron hood hung on four rods, its bench on the
    ///   door's side; the crews' long table across the back of the -x half with a bench down either side; two stone booths against the -x
    ///   end; three small tables on iron posts with low stools; a coal bin by the rooms door; lanterns.
    /// The room is its plan's (<see cref="InnPlan"/>, variant 5): what is built, what the tests measure and what the life reads. Solid: the
    /// walls, the bar, the fire pit's ring. No machines. Draws nothing from the zone's stream.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        void RustRuinInn(Transform t, Vector2 size)
        {
            var plan = InnPlan.For(size, InnPlan.RuinVariant);
            float w = plan.w, d = plan.d, wall = plan.wall, doorW = plan.doorW, doorH = plan.doorH, hx = plan.Inner.x, hz = plan.Inner.y;
            const float H = 3.4f, ceiling = 3.0f, roofH = 1.6f;
            var stone = ForgeStone(2); var dark = ForgeStone(0); var iron = ForgeIron; var rust = Tint(art.metal, new Color(.46f, .2f, .09f));
            var oak = Tint(art.timber, new Color(.22f, .15f, .1f)); var worn = Tint(art.timber, new Color(.34f, .24f, .16f)); var flags = Dressed(new Color(.26f, .24f, .22f));
            var cylinder = PrimitiveMesh(PrimitiveType.Cylinder); var parts = new Batch();
            void Cyl(Material m, Vector3 at, Vector3 scale, Quaternion? rot = null) { parts.AddMesh(cylinder, m, Matrix4x4.TRS(at, rot ?? Quaternion.identity, scale)); }
            GameObject WallPiece(Vector3 pos, Vector3 scale) { var o = BoxPart(t, pos, scale, stone); o.AddComponent<BoxCollider>(); o.AddComponent<NavBlocker>(); return o; }

            // ---------- the shell: flagged floor, stone walls (each its own collider), the low beamed ceiling ----------
            Part(PrimitiveType.Cube, t, new Vector3(0, .02f, 0), new Vector3(w - wall, .05f, d - wall), flags);
            HallFloor(t, w - wall, d - wall, .045f);
            WallPiece(new Vector3(0, H / 2, d / 2), new Vector3(w, H, wall));
            foreach (int s in new[] { -1, 1 }) WallPiece(new Vector3(s * w / 2, H / 2, 0), new Vector3(wall, H, d));
            float side = (w - doorW) / 2;
            foreach (int s in new[] { -1, 1 })
            {
                var jamb = WallPiece(new Vector3(s * (doorW / 2 + side / 2), H / 2, -d / 2), new Vector3(side, H, wall)).GetComponent<BoxCollider>();
                jamb.size -= new Vector3(JambShort, 0, 0); jamb.center += new Vector3(s * JambShort / 2, 0, 0);
            }
            WallPiece(new Vector3(0, doorH + (H - doorH) / 2, -d / 2), new Vector3(doorW, H - doorH, wall));
            Part(PrimitiveType.Cube, t, new Vector3(0, ceiling + .06f, 0), new Vector3(w - wall, .12f, d - wall), Tint(art.timber, new Color(.12f, .1f, .09f))).AddComponent<BoxCollider>();
            for (float x = -hx + .8f; x < hx - .3f; x += 1.6f) parts.Box(oak, new Vector3(x, ceiling - .12f, 0), new Vector3(.22f, .24f, d - wall));
            for (float x = -hx + .8f; x < hx - .3f; x += 3.2f) parts.Box(iron, new Vector3(x, ceiling - .25f, 0), new Vector3(.24f, .02f, d - wall));   // iron straps under the beams
            Footing(t, w + .6f, d + .6f, .35f, .4f, .5f, doorW / 2 + .05f, null, dark);
            // The walls battered at the foot outside: a sloped course of darker stone round three sides.
            foreach (int s in new[] { -1, 1 }) parts.Box(dark, new Vector3(s * (w / 2 + .18f), .45f, 0), new Vector3(.36f, .9f, d + .3f));
            parts.Box(dark, new Vector3(0, .45f, d / 2 + .18f), new Vector3(w + .7f, .9f, .36f));

            // ---------- the roof: shallow, of iron, the gables in stone; the flue through its middle ----------
            float run = d / 2 + .6f;
            MeshPart(ZoneMeshes.GableRoof(w + .9f, d + 1.2f, roofH, .2f, .5f - wall / 2), t, new Vector3(0, H, 0), IronRoof);
            Gables(t, w / 2 + wall / 2, d + wall, H, roofH, run, w / 2 + .45f, stone, 2, .2f, .2f);
            var flue = new Vector3(plan.hearth.x, 0, plan.hearth.y);
            Cyl(iron, flue + new Vector3(0, (ceiling + H + roofH + 1.6f) / 2, 0), new Vector3(.5f, (H + roofH + 1.6f - ceiling) / 2, .5f));
            Cyl(iron, flue + new Vector3(0, H + roofH + 1.6f, 0), new Vector3(.75f, .06f, .75f));
            if (art.particle != null) Smoke(t, flue + new Vector3(0, H + roofH + 1.8f, 0));

            // ---------- the front: barred slits, the iron-banded oak door, the sign ----------
            float face = -(d / 2 + wall / 2);
            foreach (int s in new[] { -1, 1 })
            {
                var slit = new Vector3(s * (doorW / 2 + 2.6f), 1.8f, -d / 2);
                Part(PrimitiveType.Cube, t, slit, new Vector3(.45f, 1, wall + .1f), Tint(art.glass, new Color(1, .58f, .26f)));
                for (int k = -1; k <= 1; k++) parts.Box(iron, slit + new Vector3(k * .13f, 0, -wall / 2 - .06f), new Vector3(.03f, 1.06f, .03f));
                parts.Box(dark, slit + new Vector3(0, .6f, -.04f), new Vector3(.75f, .2f, wall + .14f));
            }
            var door = DoubleDoor(t, -d / 2, doorW, doorH, oak, t.name);
            // The iron bands across each leaf (they swing with it), three to a leaf, and the studs along them.
            foreach (var (hinge, s) in new[] { (door.hinge, -1), (door.hinge2, 1) })
            {
                if (hinge == null) continue;
                foreach (float y in new[] { .4f, 1.2f, 2.0f })
                {
                    Part(PrimitiveType.Cube, hinge, new Vector3(-s * doorW / 4, y, -.07f), new Vector3(doorW / 2 - .08f, .09f, .025f), iron);
                    for (int k = 0; k < 4; k++) Part(PrimitiveType.Sphere, hinge, new Vector3(-s * (.15f + k * .32f), y, -.085f), Vector3.one * .045f, iron);
                }
            }
            DoorSteps(t, 0, face, .05f, doorW + .4f, wall, dark);
            Doors.Add(door); door.SetOpen(true);
            parts.Box(dark, new Vector3(0, doorH + .2f, face - .05f), new Vector3(doorW + .8f, .4f, .2f));   // the stone lintel
            foreach (int s in new[] { -1, 1 }) parts.Box(dark, new Vector3(s * (doorW / 2 + .2f), doorH / 2, face - .05f), new Vector3(.4f, doorH, .2f));
            var board = new Vector3(0, doorH + .75f, face - .08f);
            parts.Box(Tint(art.timber, new Color(.36f, .12f, .06f)), board, new Vector3(3.2f, .5f, .05f));
            parts.Box(rust, board + new Vector3(0, 0, -.03f), new Vector3(2.6f, .08f, .01f));
            foreach (int k in new[] { -1, 1 }) parts.Box(iron, board + new Vector3(0, k * .27f, -.01f), new Vector3(3.3f, .05f, .06f));
            // The sign: a cracked crucible gone to rust, hung from an iron bracket by the door, its lip broken.
            {
                var at = new Vector3(doorW / 2 + 1.3f, 2.9f, face);
                Bar(t, at, at + new Vector3(0, 0, -1.1f), .07f, .07f, iron);
                Bar(t, at + new Vector3(0, -.45f, 0), at + new Vector3(0, -.05f, -.7f), .05f, .05f, iron);
                foreach (float dx in new[] { -.08f, .08f }) Rod(t, at + new Vector3(dx, 0, -.95f), at + new Vector3(0, -.35f, -.95f), .015f, iron);
                var bowl = new[] { new Vector2(.12f, 0), new Vector2(.28f, .12f), new Vector2(.34f, .4f), new Vector2(.3f, .4f), new Vector2(.24f, .14f), new Vector2(.1f, .04f) };
                MeshPart(PropMesh("Rust and Ruin sign", () => Turned(bowl, 12, .5f)), t, at + new Vector3(0, -.8f, -.95f), rust, Quaternion.Euler(8, 0, -5));
            }
            var lamp = new Vector3(-doorW / 2 - .6f, 2.1f, face - .25f);
            Part(PrimitiveType.Cube, t, lamp, new Vector3(.22f, .3f, .22f), Glowing(ForgeFire, 1.4f));
            parts.Box(iron, lamp + new Vector3(0, .2f, .12f), new Vector3(.04f, .04f, .3f));
            Glow(t, lamp, 8, .6f, ForgeFire, 2).name = "Inn lantern";

            // ---------- the bar down the +x end: a stone base, an oak top, casks behind it, the bell ----------
            var bar = BoxPart(t, new Vector3(plan.bar.x, .52f, plan.bar.y), new Vector3(plan.barSize.x, 1.04f, plan.barSize.y), dark, null, 1);
            bar.name = "Bar"; bar.AddComponent<BoxCollider>(); bar.AddComponent<NavBlocker>();
            const float barTop = 1.08f; float barFront = plan.bar.x - plan.barSize.x / 2;
            parts.Box(oak, new Vector3(plan.bar.x - .05f, barTop - .02f, plan.bar.y), new Vector3(plan.barSize.x + .25f, .08f, plan.barSize.y + .2f));
            parts.Box(iron, new Vector3(barFront - .01f, .1f, plan.bar.y), new Vector3(.02f, .16f, plan.barSize.y));
            for (float z = plan.bar.y - plan.barSize.y / 2 + .5f; z < plan.bar.y + plan.barSize.y / 2; z += 1) parts.Box(iron, new Vector3(barFront - .01f, .55f, z), new Vector3(.02f, .9f, .05f));
            for (int i = 0; i < 3; i++) ModelProp(t, "Medieval/Prefabs/MugV2", new Vector3(plan.bar.x - .15f, barTop + .02f, plan.bar.y - 1.2f + i * 1.1f), i * 50, .13f);
            CounterFare(t, new Vector3(plan.bar.x - .15f, barTop + .02f, plan.bar.y - .6f), new Vector3(plan.bar.x - .15f, barTop + .02f, plan.bar.y + .2f), 2, 2);
            float behind = hx - .45f;
            foreach (float z in new[] { -1.4f, -.6f, .2f, 1.0f, 1.8f }) Barrel(t, new Vector3(behind, .045f, z), .8f, z * 30, true);
            parts.Box(oak, new Vector3(hx - .14f, 1.9f, plan.bar.y), new Vector3(.26f, .05f, 3.4f));
            for (int k = 0; k < 8; k++) Cyl(k % 2 == 0 ? Tint(art.stone, new Color(.45f, .38f, .3f)) : Tint(art.metal, new Color(.3f, .26f, .22f)), new Vector3(hx - .14f, 2.03f, plan.bar.y - 1.4f + k * .4f), new Vector3(.13f, .1f, .13f));
            // The bell by the keeper's place ("Ring the bell", book1 ch.24): iron, on a bracket off the back wall, a rope down from it.
            {
                var bell = new Vector3(plan.stand.x - .9f, 2.3f, hz - .45f);
                parts.Box(iron, bell + new Vector3(0, .32f, .22f), new Vector3(.05f, .05f, .5f));
                MeshPart(PropMesh("Inn bell", () => Turned(new[] { new Vector2(.05f, .3f), new Vector2(.12f, .26f), new Vector2(.15f, .08f), new Vector2(.2f, 0), new Vector2(.17f, 0), new Vector2(.11f, .2f) }, 12, .3f)), t, bell, Tint(art.metal, new Color(.55f, .42f, .22f)));
                parts.Box(Tint(art.hay, new Color(.5f, .42f, .28f)), bell + new Vector3(0, -.55f, 0), new Vector3(.025f, 1.1f, .025f));
            }
            Workplace(t, "bar", new Vector3(plan.stand.x, 0, plan.stand.y), new Vector3(plan.bar.x, 1, plan.bar.y + plan.barSize.y / 2));
            for (int i = 0; i < plan.stools.Count; i++)
            {
                var (at, _) = plan.stools[i]; float top = plan.stoolTop - .025f;
                Cyl(worn, new Vector3(at.x, top, at.y), new Vector3(.36f, .025f, .36f)); SeatMark(t, at, plan.stoolTop);
                Cyl(iron, new Vector3(at.x, top / 2, at.y), new Vector3(.06f, top / 2, .06f));
                Cyl(iron, new Vector3(at.x, .02f, at.y), new Vector3(.34f, .02f, .34f));
                Cyl(iron, new Vector3(at.x, .3f, at.y), new Vector3(.26f, .015f, .26f));
            }
            InnerDoor(t, plan.roomsX, hz, oak);
            Doors.Add(new ZoneDoor { name = t.name + ", upstairs", kind = "rooms", openable = false, position = t.TransformPoint(new Vector3(plan.roomsX, 1, d / 2 - .7f)) });
            // A coal bin by the rooms door (the house burns coal, not wood: the Forge District has no trees).
            parts.Box(oak, new Vector3(plan.woodpile.x, .3f, plan.woodpile.y), new Vector3(.9f, .6f, .6f));
            for (int k = 0; k < 6; k++) Part(PrimitiveType.Sphere, t, new Vector3(plan.woodpile.x - .3f + (k % 3) * .3f, .62f, plan.woodpile.y - .1f + (k / 3) * .2f), new Vector3(.22f, .14f, .2f), SlagBlack);

            // ---------- the fire pit out in the floor, under its iron hood; the bench on the door's side ----------
            var hs = plan.hearthSize; var hc = new Vector3(plan.hearth.x, 0, plan.hearth.y); float ring = hs.x / 2;
            var pit = new GameObject("Fire pit"); pit.transform.SetParent(t, false); pit.transform.localPosition = hc;
            var pitBox = pit.AddComponent<BoxCollider>(); pitBox.center = new Vector3(0, .3f, 0); pitBox.size = new Vector3(hs.x, .6f, hs.y); pit.AddComponent<NavBlocker>();
            for (int k = 0; k < 14; k++)
            {
                float a = k * Mathf.PI * 2 / 14;
                parts.Box(dark, hc + new Vector3(Mathf.Cos(a) * (ring - .18f), .3f, Mathf.Sin(a) * (ring - .18f)), new Vector3(.36f, .6f, ring * Mathf.PI * 2 / 14 + .04f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0));
            }
            Part(PrimitiveType.Cylinder, t, hc + new Vector3(0, .32f, 0), new Vector3(hs.x - .7f, .03f, hs.y - .7f), Ember(2f));
            for (int k = 0; k < 5; k++) Part(PrimitiveType.Sphere, t, hc + new Vector3(Mathf.Cos(k * 1.3f) * .3f, .4f, Mathf.Sin(k * 1.3f) * .3f), new Vector3(.24f, .14f, .2f), SlagBlack);
            MeshPart(PropMesh("Fire-pit hood", () => ZoneMeshes.Cone(1.05f, 1, 10)), t, hc + new Vector3(0, ceiling - 1.0f, 0), iron);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Rod(t, hc + new Vector3(sx * .7f, ceiling - .9f, sz * .7f), hc + new Vector3(sx * .5f, ceiling - .1f, sz * .5f), .015f, iron);
            var fire = new GameObject("Hearth fire").AddComponent<Light>(); fire.transform.SetParent(t, false);
            fire.transform.localPosition = hc + Vector3.up * 1.1f; fire.type = LightType.Point; fire.range = 8; fire.intensity = 1.8f; fire.color = new Color(1, .5f, .2f);
            AddStation("fire", t.name, Ground(new Vector2(fire.transform.position.x, fire.transform.position.z)), t);
            StationRoom(t, new Rect(-w / 2, -d / 2, w, d));
            parts.Box(worn, new Vector3(plan.bench.x, plan.benchTop - .03f, plan.bench.y), new Vector3(1.7f, .06f, .38f)); SeatMark(t, plan.bench, plan.benchTop, new Vector2(.75f, 0));
            foreach (float dx in new[] { -.7f, 0, .7f }) parts.Box(oak, new Vector3(plan.bench.x + dx, (plan.benchTop - .06f) / 2, plan.bench.y), new Vector3(.06f, plan.benchTop - .06f, .34f));

            // ---------- the crews' long table, a bench down either side ----------
            var lt = plan.longTable; var ls = plan.longTableSize; const float tableTop = .78f;
            parts.Box(oak, new Vector3(lt.x, tableTop, lt.y), new Vector3(ls.x, .07f, ls.y));
            foreach (int e in new[] { -1, 1 }) parts.Box(dark, new Vector3(lt.x + e * (ls.x / 2 - .4f), tableTop / 2, lt.y), new Vector3(.3f, tableTop, .5f));
            foreach (int sz in new[] { -1, 1 })
            {
                float y = lt.y + sz * .7f;
                parts.Box(worn, new Vector3(lt.x, plan.seatTop - .03f, y), new Vector3(ls.x - .2f, .06f, .34f));
                foreach (int e in new[] { -1, 0, 1 }) parts.Box(oak, new Vector3(lt.x + e * (ls.x / 2 - .4f), (plan.seatTop - .06f) / 2, y), new Vector3(.06f, plan.seatTop - .06f, .3f));
                SeatMark(t, new Vector2(lt.x, y), plan.seatTop, new Vector2((ls.x - .2f) / 2 - .1f, 0));
            }
            for (int i = 0; i < 4; i++) ModelProp(t, "Medieval/Prefabs/MugV2", new Vector3(lt.x - 1.8f + i * 1.2f, tableTop + .04f, lt.y + (i % 2 == 0 ? .15f : -.15f)), i * 45, .13f);
            CounterFare(t, new Vector3(lt.x - 1.6f, tableTop + .04f, lt.y), new Vector3(lt.x + 1.6f, tableTop + .04f, lt.y), 3, 1);
            TableTops.Add(t.TransformPoint(new Vector3(lt.x, tableTop + .04f, lt.y)));

            // ---------- the booths and the small tables ----------
            for (int n = 0; n < plan.tables.Count; n++)
            {
                var at = plan.tables[n]; var c = new Vector3(at.x, 0, at.y);
                bool booth = at.x < -hx + 1.5f;
                if (booth)
                {
                    // A slab on a stone pier against the wall, a stone settle either side with a plank seat.
                    parts.Box(oak, c + new Vector3(0, tableTop, 0), new Vector3(1.4f, .07f, 1.0f));
                    parts.Box(dark, c + new Vector3(0, tableTop / 2, 0), new Vector3(.5f, tableTop, .4f));
                    foreach (int sz in new[] { -1, 1 })
                    {
                        var settle = c + new Vector3(0, 0, sz * 1f);
                        parts.Box(dark, settle + new Vector3(0, (plan.seatTop - .06f) / 2, 0), new Vector3(1.3f, plan.seatTop - .06f, .42f));
                        parts.Box(worn, settle + new Vector3(0, plan.seatTop - .03f, 0), new Vector3(1.3f, .06f, .44f));
                        parts.Box(dark, settle + new Vector3(0, plan.seatTop + .5f, sz * .26f), new Vector3(1.3f, 1.0f, .12f));   // its high back
                        SeatMark(t, new Vector2(settle.x, settle.z), plan.seatTop, new Vector2(.45f, 0));
                    }
                    Glow(t, c + new Vector3(-.2f, 1.9f, 0), 4, .5f, ForgeFire, 1.2f).name = "Booth lamp";
                    Part(PrimitiveType.Cube, t, c + new Vector3(-.4f, 1.9f, 0), new Vector3(.18f, .24f, .18f), Glowing(ForgeFire, 1.2f));
                }
                else
                {
                    Cyl(worn, c + new Vector3(0, tableTop, 0), new Vector3(1.1f, .035f, 1.1f));
                    Cyl(iron, c + new Vector3(0, tableTop / 2, 0), new Vector3(.1f, tableTop / 2, .1f));
                    Cyl(iron, c + new Vector3(0, .02f, 0), new Vector3(.6f, .02f, .6f));
                    ModelProp(t, "Medieval/Prefabs/MugV2", new Vector3(at.x - .2f, tableTop + .04f, at.y + .15f), n * 60, .13f);
                    TableFare(t, new Vector3(at.x + .05f, tableTop + .04f, at.y - .25f), 180, n, .25f);
                    TableTops.Add(t.TransformPoint(new Vector3(at.x, tableTop + .04f, at.y)));
                    foreach (var (s, f) in plan.seats)
                        if (Vector2.Distance(f, at) < .01f)
                        {
                            Cyl(oak, new Vector3(s.x, plan.seatTop - .03f, s.y), new Vector3(.4f, .03f, .4f)); SeatMark(t, s, plan.seatTop);
                            foreach (int k in new[] { 0, 1, 2 }) { float a = k * 2.094f; parts.Box(oak, new Vector3(s.x + Mathf.Cos(a) * .12f, (plan.seatTop - .06f) / 2, s.y + Mathf.Sin(a) * .12f), new Vector3(.05f, plan.seatTop - .06f, .05f)); }
                        }
                }
            }

            // ---------- the light: two lanterns hung from the beams ----------
            foreach (float x in new[] { -3.4f, 4.6f })
            {
                Part(PrimitiveType.Cube, t, new Vector3(x, ceiling - .7f, -1.5f), new Vector3(.22f, .28f, .22f), Glowing(ForgeFire, 1.3f));
                parts.Box(iron, new Vector3(x, ceiling - .4f, -1.5f), new Vector3(.02f, .5f, .02f));
                var l = new GameObject("Taproom light").AddComponent<Light>(); l.transform.SetParent(t, false);
                l.transform.localPosition = new Vector3(x, ceiling - .9f, -1.5f); l.type = LightType.Point; l.range = 8; l.intensity = .9f; l.color = new Color(1, .6f, .3f);
            }
            parts.Flush(this, t, "Rust and Ruin");
            RegisterInn(t, "tavern", door, plan, plan.Inner);
        }
    }
}
