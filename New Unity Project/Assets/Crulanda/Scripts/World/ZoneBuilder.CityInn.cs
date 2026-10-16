using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The city inn (inn variant 3; GAME-ONLY, p9-cityinn; Chris, 2026-10-16: "dont need the same exact inn in the castle. needs a
    /// redesign"): the Crown's Gilded Lantern, an inn of the High City and not the village's timber house. White stone with gilt string
    /// courses and a cornice under a blue-grey slate roof, two storeys of tall windows, and on the ridge the lantern it is named for: a
    /// gilt cupola of glass, lit at night. A portico of two marble columns over a grand double door (two leaves of 1.6 m: DoubleDoor,
    /// the doorway's lanes and all), lanterns either side, the name in gilt and a gilt lantern hung from the entablature, bay trees in
    /// tubs. Inside, a taproom two storeys high with a gallery round three walls on marble columns and stone corbels, reached by a stair
    /// along the front wall (walked: its ramp and the gallery's floor are in the navmesh, NavWalkable); the long polished bar under the
    /// back gallery with its stools, the barrels and bottles behind it; the hearth with a carved mantel and overmantel on the +x end, its
    /// bench before it and the kitchen's range in the corner behind; three high-backed booths along the -x end; five round tables on
    /// the floor; chandeliers and sconces. The room is its plan's (<see cref="InnPlan"/>, variant 3): what the builder lays out, the
    /// tests measure and the village's life reads (its seats, the bar, the kitchen, the hatch and the servers' places; ZoneInn "city").
    /// Only the walls, the bar, the hearth, the range, the columns and the stair's ramp are solid; the doorway is step-free (the footing
    /// and the steps stop at the jambs). No machines. Draws nothing random.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        Material InnBoards { get { return Tint(art.timber, new Color(.36f, .25f, .16f)); } }
        Material Cushion { get { return Tint(art.cloth, new Color(.55f, .12f, .14f)); } }   // the booths' and stools' red

        void CityInn(Transform t, Vector2 size)
        {
            var plan = InnPlan.For(size, 3);
            float w = plan.w, d = plan.d, wall = plan.wall, doorW = plan.doorW, doorH = plan.doorH, gy = plan.galleryY, g = plan.galleryDepth;
            float hx = plan.Inner.x, hz = plan.Inner.y, H = 7.4f, wallH = 8.2f, storey = 4.1f;
            var white = CityWhite; var oak = DarkOak;
            GameObject WallPiece(Vector3 pos, Vector3 scale) { var o = BoxPart(t, pos, scale, white); o.AddComponent<BoxCollider>(); o.AddComponent<NavBlocker>(); return o; }
            // ---------- the shell: a marble floor, the walls (each its own collider and navmesh blocker), the ceiling ----------
            Part(PrimitiveType.Cube, t, new Vector3(0, .025f, 0), new Vector3(w - wall, .05f, d - wall), Marble);
            HallFloor(t, w - wall, d - wall, .05f);   // solid underfoot, as every hall of the Crown (CrownThronePlayTests: none here)
            WallPiece(new Vector3(0, wallH / 2, d / 2), new Vector3(w, wallH, wall));
            foreach (int s in new[] { -1, 1 }) WallPiece(new Vector3(s * w / 2, wallH / 2, 0), new Vector3(wall, wallH, d));
            float side = (w - doorW) / 2;
            foreach (int s in new[] { -1, 1 })
            {
                // The jambs' colliders stop JambShort short of the opening, as every inn's (the navmesh's way through is NavGap's).
                var jamb = WallPiece(new Vector3(s * (doorW / 2 + side / 2), wallH / 2, -d / 2), new Vector3(side, wallH, wall)).GetComponent<BoxCollider>();
                jamb.size -= new Vector3(JambShort, 0, 0); jamb.center += new Vector3(s * JambShort / 2, 0, 0);
            }
            WallPiece(new Vector3(0, doorH + (wallH - doorH) / 2, -d / 2), new Vector3(doorW, wallH - doorH, wall));
            // The ceiling over the taproom, solid so the camera pulls in under it; coffered with dark oak beams and a gilt rose for each chandelier.
            Part(PrimitiveType.Cube, t, new Vector3(0, H + .1f, 0), new Vector3(w - wall, .2f, d - wall), Tint(art.plaster, new Color(.93f, .91f, .85f))).AddComponent<BoxCollider>();
            for (float x = -hx + 2.2f; x < hx - 1; x += 2.2f) Part(PrimitiveType.Cube, t, new Vector3(x, H - .08f, 0), new Vector3(.2f, .2f, d - wall), oak);
            foreach (float z in new[] { -hz / 2, hz / 2 }) Part(PrimitiveType.Cube, t, new Vector3(0, H - .1f, z), new Vector3(w - wall, .16f, .16f), oak);
            // ---------- outside: the footing (open at the door), string course, cornice, pilasters, windows, roof and lantern ----------
            Footing(t, w + .7f, d + .7f, .3f, .35f, .5f, doorW / 2 + .05f, null, CityShade);
            foreach (int sz in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(0, storey, sz * (d / 2 + .08f)), new Vector3(w + .16f, .18f, .16f), Gilt);   // the string course between the storeys
                Part(PrimitiveType.Cube, t, new Vector3(0, wallH - .25f, sz * (d / 2 + .15f)), new Vector3(w + .5f, .5f, .3f), white);   // the cornice
                Part(PrimitiveType.Cube, t, new Vector3(0, wallH - .62f, sz * (d / 2 + .07f)), new Vector3(w + .14f, .14f, .14f), Gilt);
            }
            foreach (int sx in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .08f), storey, 0), new Vector3(.16f, .18f, d + .16f), Gilt);
                Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .15f), wallH - .25f, 0), new Vector3(.3f, .5f, d + .5f), white);
                Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .07f), wallH - .62f, 0), new Vector3(.14f, .14f, d + .14f), Gilt);
                foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .02f), wallH / 2, sz * (d / 2 + .02f)), new Vector3(.62f, wallH, .62f), CityShade);   // quoins at the corners
            }
            foreach (float x in new[] { -w / 2 + 3.6f, -doorW / 2 - 1.1f, doorW / 2 + 1.1f, w / 2 - 3.6f })   // pilasters on the front
                Part(PrimitiveType.Cube, t, new Vector3(x, wallH / 2, -d / 2 - .12f), new Vector3(.5f, wallH - .6f, .25f), white);
            // Windows: tall ones on the ground floor (the front either side of the door, the -x end by the booths), and the upper
            // storey all round; they glow through the wall at night.
            // The front's between its pilasters; the back's and the ends' upper ones over the gallery's doors.
            float bay = (doorW / 2 + 1.1f + w / 2 - 3.6f) / 2, outer = (w / 2 - 3.6f + w / 2 - .3f) / 2;
            foreach (int s in new[] { -1, 1 }) foreach (float x in new[] { bay, outer }) { CityWindow(t, new Vector3(s * x, 2.1f, -d / 2), 0, 1.1f, 2.3f, wall); CityWindow(t, new Vector3(s * x, 5.9f, -d / 2), 0, 1, 2, wall); }
            foreach (float z in new[] { -4.6f, -1.6f, 1.4f }) CityWindow(t, new Vector3(-w / 2, 2.1f, z), 90, .9f, 1.6f, wall);
            foreach (int s in new[] { -1, 1 }) foreach (float x in new[] { 1.6f, 4.8f, 8f }) CityWindow(t, new Vector3(s * x, 6.4f, d / 2), 180, 1, 1.4f, wall);
            foreach (int s in new[] { -1, 1 }) foreach (float z in new[] { -3.4f, 0f, 3.4f }) CityWindow(t, new Vector3(s * w / 2, 6.4f, z), s < 0 ? 90 : 270, 1, 1.4f, wall);
            float roofH = Mathf.Max(3.2f, d * .3f);
            HallRoof(t, w, d, wallH, roofH, BlueSlate);
            // The lantern on the ridge (the inn's name): an octagon of glass on a white drum under a gilt cap, lit after dark.
            var cupola = new Vector3(0, wallH + roofH - .3f, 0);
            Part(PrimitiveType.Cylinder, t, cupola + Vector3.up * .5f, new Vector3(2.4f, .5f, 2.4f), white);
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Part(PrimitiveType.Cube, t, cupola + new Vector3(Mathf.Cos(a) * .95f, 1.75f, Mathf.Sin(a) * .95f), new Vector3(.62f, 1.3f, .06f), art.glass, Quaternion.Euler(0, 90 - a * Mathf.Rad2Deg, 0)); Part(PrimitiveType.Cube, t, cupola + new Vector3(Mathf.Cos(a + Mathf.PI / 8) * 1.02f, 1.75f, Mathf.Sin(a + Mathf.PI / 8) * 1.02f), new Vector3(.1f, 1.4f, .1f), Gilt); }
            Part(PrimitiveType.Cylinder, t, cupola + Vector3.up * 2.45f, new Vector3(2.5f, .1f, 2.5f), Gilt);
            GiltSpire(t, cupola + Vector3.up * 2.5f, 1.3f, 2.2f, 8);
            Glow(t, cupola + Vector3.up * 1.75f, 14, .2f, new Color(1, .78f, .42f), 2.4f).name = "The lantern";
            foreach (int s in new[] { -1, 1 })   // chimney stacks over the hearth's end and the kitchen's corner
            {
                var stack = new Vector3(w / 2 - 1.2f, wallH + roofH * .55f, s < 0 ? plan.hearth.y : plan.range.y);
                BoxPart(t, stack, new Vector3(.9f, roofH * 1.2f, .9f), white, null, 1); Part(PrimitiveType.Cube, t, stack + Vector3.up * (roofH * .6f + .06f), new Vector3(1.1f, .12f, 1.1f), Gilt);
                if (art.particle != null) Smoke(t, stack + Vector3.up * (roofH * .65f));
            }
            // ---------- the front: the portico, the grand door, the lanterns, the name ----------
            float front = -d / 2, pz = front - 2.1f, colH = 4.6f;
            foreach (int s in new[] { -1, 1 })
            {
                var c = new Vector3(s * (doorW / 2 + .95f), 0, pz);
                Solidify(Part(PrimitiveType.Cylinder, t, c + Vector3.up * colH / 2, new Vector3(.55f, colH / 2, .55f), Marble));
                Part(PrimitiveType.Cube, t, c + Vector3.up * .18f, new Vector3(.85f, .36f, .85f), CityShade); Part(PrimitiveType.Cube, t, c + Vector3.up * (colH - .14f), new Vector3(.8f, .28f, .8f), Gilt);
            }
            float span = doorW + 3.2f;
            BoxPart(t, new Vector3(0, colH + .4f, front - 1.2f), new Vector3(span, .8f, 2.6f), white);   // the entablature, on the wall and the columns
            Part(PrimitiveType.Cube, t, new Vector3(0, colH + .02f, front - 1.2f), new Vector3(span - .3f, .05f, 2.3f), Tint(art.plaster, new Color(.94f, .92f, .86f)));   // its soffit
            MeshPart(ZoneMeshes.Gable(span, 1.5f, .4f), t, new Vector3(0, colH + .8f, pz - .3f), white);
            MeshPart(ZoneMeshes.GableRoof(span + .3f, 2.9f, .01f, .18f), t, new Vector3(0, colH + .8f, front - 1.2f), BlueSlate);
            Lettering(t, "The Gilded Lantern", new Vector3(0, colH + .4f, pz - .62f), span * .9f);
            // The sign: a gilt lantern hung on a chain from the entablature over the door, lit after dark.
            var sign = new Vector3(0, colH - .9f, pz + .4f);
            Part(PrimitiveType.Cylinder, t, sign + Vector3.up * .55f, new Vector3(.03f, .3f, .03f), Gilt);
            Part(PrimitiveType.Cube, t, sign, new Vector3(.5f, .62f, .5f), art.glass); Part(PrimitiveType.Cube, t, sign - Vector3.up * .34f, new Vector3(.6f, .08f, .6f), Gilt);
            MeshPart(PropMesh("City lantern roof", () => ZoneMeshes.Cone(.36f, .34f, 4)), t, sign + Vector3.up * .3f, Gilt, Quaternion.Euler(0, 45, 0));
            Glow(t, sign, 8, .5f, new Color(1, .74f, .4f), 2).name = "Inn lantern";
            // The door's gilt frame, a fanlight over it, and lanterns on brackets either side.
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * (doorW / 2 + .12f), doorH / 2, front - wall / 2 - .06f), new Vector3(.24f, doorH, .12f), Gilt);
            Part(PrimitiveType.Cube, t, new Vector3(0, doorH + .14f, front - wall / 2 - .06f), new Vector3(doorW + .48f, .28f, .12f), Gilt);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, doorH + .62f, front - wall / 2 + .02f), new Vector3(doorW * .8f, .04f, .8f), art.glass, Quaternion.Euler(90, 0, 0));
            foreach (int s in new[] { -1, 1 })
            {
                var lamp = new Vector3(s * (doorW / 2 + .55f), 2.9f, front - wall / 2 - .3f);
                Part(PrimitiveType.Cube, t, lamp, new Vector3(.26f, .36f, .26f), art.glass); Part(PrimitiveType.Cube, t, lamp + Vector3.up * .22f, new Vector3(.34f, .06f, .34f), Gilt);
                Part(PrimitiveType.Cube, t, lamp + new Vector3(0, .06f, .2f), new Vector3(.05f, .05f, .3f), Gilt);
                Glow(t, lamp, 7, .4f, new Color(1, .74f, .42f), 1.8f);
            }
            var innDoor = DoubleDoor(t, front, doorW, doorH, oak, t.name);
            DoorSteps(t, 0, front - wall / 2, .05f, doorW + .4f, wall, CityShade);
            Doors.Add(innDoor); innDoor.SetOpen(true);   // the Lantern keeps its doors open; the city comes and goes
            // Bay trees in tubs either side of the portico (each solid at its tub).
            foreach (int s in new[] { -1, 1 })
            {
                var tub = new Vector3(s * (doorW / 2 + 3.1f), 0, front - 1.1f);
                Part(PrimitiveType.Cylinder, t, tub + Vector3.up * .35f, new Vector3(.8f, .35f, .8f), white); Part(PrimitiveType.Cylinder, t, tub + Vector3.up * .66f, new Vector3(.86f, .03f, .86f), Gilt);
                Part(PrimitiveType.Cylinder, t, tub + Vector3.up * 1.1f, new Vector3(.08f, .5f, .08f), Tint(art.bark != null ? art.bark : art.timber, new Color(.4f, .3f, .2f)));
                Part(PrimitiveType.Sphere, t, tub + Vector3.up * 1.9f, Vector3.one * 1.1f, Tint(art.foliage, new Color(.16f, .34f, .14f)));
                Blocker(t, tub + Vector3.up * .35f, new Vector3(.8f, .7f, .8f));
            }
            // ---------- the gallery: floors on corbels and columns, a balustrade, the rooms' doors; the stair up to it ----------
            var floor = InnBoards; float slab = .25f;
            GameObject Deck(Vector3 at, Vector3 sz)
            {
                var o = Part(PrimitiveType.Cube, t, at, sz, floor); o.name = "Gallery floor"; o.AddComponent<BoxCollider>(); o.AddComponent<NavWalkable>(); return o;
            }
            Deck(new Vector3(0, gy - slab / 2, hz - g / 2), new Vector3(2 * (hx - g), slab, g));   // the back, between the ends
            foreach (int s in new[] { -1, 1 }) Deck(new Vector3(s * (hx - g / 2), gy - slab / 2, 0), new Vector3(g, slab, 2 * hz));
            for (float x = -hx + 1; x <= hx - 1; x += 2) Part(PrimitiveType.Cube, t, new Vector3(x, gy - slab - .3f, hz - .3f), new Vector3(.3f, .6f, .6f), white);   // corbels under the gallery's back
            foreach (int s in new[] { -1, 1 }) for (float z = -hz + 1; z <= hz - 1; z += 2) Part(PrimitiveType.Cube, t, new Vector3(s * (hx - .3f), gy - slab - .3f, z), new Vector3(.6f, .6f, .3f), white);
            Part(PrimitiveType.Cube, t, new Vector3(0, gy - slab - .05f, hz - g + .05f), new Vector3(2 * (hx - g), .12f, .12f), Gilt);   // a gilt fillet along the gallery's edge
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * (hx - g + .05f), gy - slab - .05f, 0), new Vector3(.12f, .12f, 2 * hz), Gilt);
            foreach (var c in plan.columns)
            {
                float ch = gy - slab;
                Solidify(Part(PrimitiveType.Cylinder, t, new Vector3(c.x, ch / 2, c.y), new Vector3(InnPlan.ColumnRadius * 2, ch / 2, InnPlan.ColumnRadius * 2), Marble));
                Part(PrimitiveType.Cube, t, new Vector3(c.x, .15f, c.y), new Vector3(.9f, .3f, .9f), CityShade); Part(PrimitiveType.Cube, t, new Vector3(c.x, ch - .15f, c.y), new Vector3(.85f, .3f, .85f), Gilt);
            }
            // The balustrade round the gallery's inner edge (white balusters, a gilt rail; solid so nobody steps off), open at the stair's head.
            var stairTop = plan.stair.x + plan.stairSize.x / 2; float stairFrom = plan.stair.y - plan.stairSize.y / 2, stairTo = plan.stair.y + plan.stairSize.y / 2;
            void Rail(Vector3 a, Vector3 b)
            {
                var mid = (a + b) / 2; var len = Vector3.Distance(a, b); if (len < .2f) return; var q = Quaternion.LookRotation(b - a);
                Part(PrimitiveType.Cube, t, mid + Vector3.up * (gy + .95f), new Vector3(.1f, .08f, len), Gilt, q);
                for (float u = .15f; u < len; u += .3f) Part(PrimitiveType.Cylinder, t, Vector3.Lerp(a, b, u / len) + Vector3.up * (gy + .45f), new Vector3(.08f, .45f, .08f), white);
                var col = new GameObject("Balustrade"); col.transform.SetParent(t, false); col.transform.localPosition = mid + Vector3.up * (gy + .5f); col.transform.localRotation = q;
                col.AddComponent<BoxCollider>().size = new Vector3(.16f, 1, len);
            }
            float ex = hx - g;
            Rail(new Vector3(-ex, 0, hz - g), new Vector3(ex, 0, hz - g));
            Rail(new Vector3(-ex, 0, -hz), new Vector3(-ex, 0, hz - g));
            Rail(new Vector3(ex, 0, stairTo + .05f), new Vector3(ex, 0, hz - g));
            // The rooms' doors along the gallery's walls (barred: the lodgers' rooms), each in a gilt frame, a sconce between.
            var holderDoors = new List<(Vector3 at, float yaw)>();
            for (float x = -ex + 1.5f; x <= ex - 1.5f; x += 3.2f) holderDoors.Add((new Vector3(x, gy + 1.05f, hz - .04f), 0));
            foreach (int s in new[] { -1, 1 }) for (float z = -hz + 2.6f; z <= hz - g - 1; z += 3.4f) holderDoors.Add((new Vector3(s * (hx - .04f), gy + 1.05f, z), s < 0 ? -90 : 90));
            foreach (var (at, yaw) in holderDoors)
            {
                var h = new GameObject("Room door").transform; h.SetParent(t, false); h.localPosition = at; h.localRotation = Quaternion.Euler(0, yaw, 0);
                PlankDoor(h, Vector3.zero, .9f, 2.1f, oak);
                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, h, new Vector3(s * .52f, .04f, -.04f), new Vector3(.12f, 2.2f, .08f), Gilt);
                Part(PrimitiveType.Cube, h, new Vector3(0, 1.12f, -.04f), new Vector3(1.16f, .12f, .08f), Gilt);
            }
            // The stair: white treads up the front wall to the +x gallery over a ramp that is walked (solid and in the navmesh), a rail on its open side.
            {
                float x0 = plan.stair.x - plan.stairSize.x / 2, x1 = stairTop, len = Mathf.Sqrt((x1 - x0) * (x1 - x0) + gy * gy), pitch = Mathf.Atan2(gy, x1 - x0) * Mathf.Rad2Deg;
                var q = Quaternion.Euler(0, 0, pitch); var normal = q * Vector3.up;
                var ramp = Part(PrimitiveType.Cube, t, new Vector3((x0 + x1) / 2, gy / 2, plan.stair.y) - normal * .15f, new Vector3(len, .3f, plan.stairSize.y), Tint(art.plaster, new Color(.86f, .84f, .8f)), q);
                ramp.name = "Stair ramp"; ramp.AddComponent<BoxCollider>(); ramp.AddComponent<NavWalkable>();
                var treads = new List<CombineInstance>(); int n = Mathf.Max(4, Mathf.RoundToInt(gy / .18f)); float go = (x1 - x0) / n;
                for (int k = 0; k < n; k++)
                {
                    float rise = (k + 1) * gy / n, xa = x0 + k * go;
                    treads.Add(Ashlar(new Vector3(xa + go / 2, rise / 2 + .01f, plan.stair.y), new Vector3(go, rise, plan.stairSize.y), 1.75f));
                }
                Joined32("Stair treads", t, Dressed(new Color(.88f, .86f, .82f)), treads);
                // The rail on the open side, rising with the stair, and its newel at the foot.
                var railA = new Vector3(x0 + .3f, 1, stairTo); var railB = new Vector3(x1, gy + 1, stairTo); var rq = Quaternion.LookRotation(railB - railA);
                Part(PrimitiveType.Cube, t, (railA + railB) / 2, new Vector3(.1f, .08f, Vector3.Distance(railA, railB)), Gilt, rq);
                for (int k = 1; k < n; k += 2) { float u = (float)k / n; Part(PrimitiveType.Cylinder, t, new Vector3(x0 + u * (x1 - x0), u * gy + .5f, stairTo), new Vector3(.07f, .5f, .07f), white); }
                Part(PrimitiveType.Cube, t, new Vector3(x0 + .3f, .6f, stairTo), new Vector3(.24f, 1.2f, .24f), white); Part(PrimitiveType.Sphere, t, new Vector3(x0 + .3f, 1.28f, stairTo), Vector3.one * .26f, Gilt);
                var guard = new GameObject("Stair rail"); guard.transform.SetParent(t, false); guard.transform.localPosition = (railA + railB) / 2 - Vector3.up * .45f; guard.transform.localRotation = rq;
                guard.AddComponent<BoxCollider>().size = new Vector3(.16f, 1, Vector3.Distance(railA, railB));
            }
            // ---------- the bar: long, polished, a brass foot-rail; the barrels and bottles behind it ----------
            var bar = BoxPart(t, new Vector3(plan.bar.x, .55f, plan.bar.y), new Vector3(plan.barSize.x, 1.1f, plan.barSize.y), oak);
            bar.AddComponent<BoxCollider>(); bar.AddComponent<NavBlocker>();
            var barTop = Part(PrimitiveType.Cube, t, new Vector3(plan.bar.x, 1.13f, plan.bar.y - .05f), new Vector3(plan.barSize.x + .2f, .07f, plan.barSize.y + .25f), Tint(art.timber, new Color(.42f, .2f, .1f)));
            Part(PrimitiveType.Cube, t, new Vector3(plan.bar.x, 1.1f, plan.bar.y - plan.barSize.y / 2 - .14f), new Vector3(plan.barSize.x + .2f, .04f, .03f), Gilt);   // the gilt edge
            Part(PrimitiveType.Cylinder, t, new Vector3(plan.bar.x, .22f, plan.bar.y - plan.barSize.y / 2 - .18f), new Vector3(.05f, plan.barSize.x / 2, .05f), Gilt, Quaternion.Euler(0, 0, 90));   // the foot-rail
            for (float x = plan.bar.x - plan.barSize.x / 2 + .5f; x < plan.bar.x + plan.barSize.x / 2; x += 1.2f) Part(PrimitiveType.Cube, t, new Vector3(x, .55f, plan.bar.y - plan.barSize.y / 2 - .01f), new Vector3(.08f, 1, .03f), Gilt);   // panels
            float top = TopIn(t, barTop);
            for (int i = 0; i < 4; i++) ModelProp(t, "Medieval/Prefabs/MugV2", new Vector3(plan.bar.x - plan.barSize.x / 2 + 1 + i * 2.3f, top, plan.bar.y - .1f), i * 50, .13f);
            ModelProp(t, "Medieval/Prefabs/BottleV1", new Vector3(plan.bar.x + 1.2f, top, plan.bar.y), 0, .28f); ModelProp(t, "Medieval/Prefabs/CandleV1", new Vector3(plan.bar.x - 2.6f, top, plan.bar.y + .1f), 0, .2f);
            for (int i = 0; i < 4; i++) Barrel(t, new Vector3(plan.bar.x - plan.barSize.x / 2 + 1 + i * 1.1f, .05f, hz - .5f), .9f, i * 40);
            foreach (float y in new[] { 1.5f, 2.15f })   // shelves of bottles behind the bar, under the gallery
            {
                Part(PrimitiveType.Cube, t, new Vector3(plan.bar.x + 2.3f, y, hz - .18f), new Vector3(4.2f, .06f, .3f), oak);
                for (int k = 0; k < 12; k++) Part(PrimitiveType.Cylinder, t, new Vector3(plan.bar.x + .5f + k * .33f, y + .17f, hz - .18f), new Vector3(.09f, .14f, .09f), k % 3 == 0 ? Tint(art.glass, new Color(.3f, .5f, .3f)) : k % 3 == 1 ? Tint(art.glass, new Color(.5f, .3f, .2f)) : art.glass);
            }
            foreach (var (at, _) in plan.stools)
            {
                Part(PrimitiveType.Cylinder, t, new Vector3(at.x, .36f, at.y), new Vector3(.08f, .36f, .08f), Gilt);
                Part(PrimitiveType.Cylinder, t, new Vector3(at.x, .74f, at.y), new Vector3(.44f, .05f, .44f), Cushion);
            }
            Workplace(t, "bar", new Vector3(plan.stand.x, 0, plan.stand.y), new Vector3(plan.bar.x, 1, plan.bar.y));
            // The rooms door behind the innkeeper (barred to the player: the lodgers go up by the back stair through it).
            InnerDoor(t, plan.roomsX, hz, oak);
            Doors.Add(new ZoneDoor { name = t.name + ", upstairs", kind = "rooms", openable = false, position = t.TransformPoint(new Vector3(plan.roomsX, 1, hz - .7f)) });
            // ---------- the hearth: white marble, a carved mantel and overmantel with the inn's lantern in gilt ----------
            var hearth = BoxPart(t, new Vector3(plan.hearth.x, 1.1f, plan.hearth.y), new Vector3(plan.hearthSize.x, 2.2f, plan.hearthSize.y), Marble, null, 1);
            hearth.AddComponent<BoxCollider>(); hearth.AddComponent<NavBlocker>();
            float face = plan.hearth.x - plan.hearthSize.x / 2;
            Part(PrimitiveType.Cube, t, new Vector3(face - .02f, .6f, plan.hearth.y), new Vector3(.06f, .9f, 1.4f), art.glass);   // the fire's glow in the firebox
            Part(PrimitiveType.Cube, t, new Vector3(face - .04f, .6f, plan.hearth.y), new Vector3(.03f, 1.1f, 1.6f), Tint(art.metal, new Color(.18f, .18f, .2f)));   // the fire-back's iron, round it
            Part(PrimitiveType.Cube, t, new Vector3(face - .15f, 1.42f, plan.hearth.y), new Vector3(.4f, .14f, plan.hearthSize.y + .5f), Marble);   // the mantel shelf
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(face - .1f, .7f, plan.hearth.y + s * (plan.hearthSize.y / 2 + .05f)), new Vector3(.3f, 1.4f, .3f), Marble);   // the carved jambs
            Part(PrimitiveType.Cube, t, new Vector3(face - .05f, 1.34f, plan.hearth.y), new Vector3(.1f, .06f, plan.hearthSize.y + .4f), Gilt);
            Part(PrimitiveType.Cube, t, new Vector3(plan.hearth.x - .1f, 2.6f, plan.hearth.y), new Vector3(.7f, 1.4f, plan.hearthSize.y - .4f), white);   // the overmantel
            var relief = new Vector3(face + .18f, 2.6f, plan.hearth.y);
            Part(PrimitiveType.Cube, t, relief, new Vector3(.04f, .55f, .4f), Gilt); MeshPart(PropMesh("City lantern roof", () => ZoneMeshes.Cone(.36f, .34f, 4)), t, relief + Vector3.up * .3f, Gilt, Quaternion.Euler(0, 45, 0));
            for (int k = 0; k < 3; k++) ModelProp(t, "Medieval/Prefabs/CandleV1", new Vector3(face - .2f, 1.5f, plan.hearth.y - .9f + k * .9f), 0, .2f);
            var fire = new GameObject("Hearth fire").AddComponent<Light>(); fire.transform.SetParent(t, false);
            fire.transform.localPosition = new Vector3(plan.fire.x, 1, plan.fire.y); fire.type = LightType.Point; fire.range = 10; fire.intensity = 1.9f; fire.color = new Color(1, .55f, .25f);
            AddStation("fire", t.name, Ground(new Vector2(fire.transform.position.x, fire.transform.position.z)), t);   // the hearth is a fire to cook at, named after the inn
            StationRoom(t, new Rect(-w / 2, -d / 2, w, d));
            CityBench(t, new Vector3(plan.bench.x, 0, plan.bench.y), 270, 2.2f);   // its back to the room, facing the fire
            ModelProp(t, "Medieval/Prefabs/PileOfWoodV1", new Vector3(plan.woodpile.x, .045f, plan.woodpile.y), 0, .5f);
            // ---------- the kitchen's corner: the range with its pot, the cook's place, the hatch where goods are handed in ----------
            var range = BoxPart(t, new Vector3(plan.range.x, .55f, plan.range.y), new Vector3(plan.rangeSize.x, 1.1f, plan.rangeSize.y), Masonry, null, 1);
            range.AddComponent<BoxCollider>(); range.AddComponent<NavBlocker>();
            Part(PrimitiveType.Cube, t, new Vector3(plan.range.x - plan.rangeSize.x / 2 - .02f, .5f, plan.range.y), new Vector3(.05f, .4f, .8f), art.glass);
            Part(PrimitiveType.Cylinder, t, new Vector3(plan.range.x, 1.3f, plan.range.y + .3f), new Vector3(.5f, .2f, .5f), Tint(art.metal, new Color(.2f, .2f, .22f)));   // the pot
            Glow(t, new Vector3(plan.range.x - .6f, .8f, plan.range.y), 5, .9f, new Color(1, .55f, .25f), 1.2f);
            Workplace(t, "kitchen", new Vector3(plan.cook.x, 0, plan.cook.y), new Vector3(plan.range.x, 1, plan.range.y));
            Workplace(t, "kitchendoor", new Vector3(plan.hatch.x, 0, plan.hatch.y), new Vector3(plan.cook.x, 1, plan.cook.y));
            foreach (var s in plan.serve) Workplace(t, "serve", new Vector3(s.x, 0, s.y), new Vector3(plan.bar.x, 1, plan.bar.y));
            // ---------- the booths and the tables ----------
            bool kitTables = HasProp("Fantasy/Table_Large");
            for (int n = 0; n < plan.tables.Count; n++)
            {
                var at = plan.tables[n];
                if (n < 3)
                {
                    // A booth: a table against the wall, a high-backed settle either side (red cushions), a sconce over it.
                    Part(PrimitiveType.Cube, t, new Vector3(at.x, .76f, at.y), new Vector3(1.5f, .07f, .9f), oak);
                    Part(PrimitiveType.Cube, t, new Vector3(at.x + .3f, .38f, at.y), new Vector3(.12f, .74f, .5f), oak);
                    foreach (int sz in new[] { -1, 1 })
                    {
                        Part(PrimitiveType.Cube, t, new Vector3(at.x, .45f, at.y + sz * 1.0f), new Vector3(1.7f, .1f, .55f), Cushion);
                        Part(PrimitiveType.Cube, t, new Vector3(at.x, .2f, at.y + sz * 1.0f), new Vector3(1.7f, .4f, .5f), oak);
                        Part(PrimitiveType.Cube, t, new Vector3(at.x, .95f, at.y + sz * 1.32f), new Vector3(1.75f, 1.9f, .1f), oak);   // the high back
                        Part(PrimitiveType.Cube, t, new Vector3(at.x, 1.92f, at.y + sz * 1.32f), new Vector3(1.8f, .06f, .14f), Gilt);
                    }
                    ModelProp(t, "Medieval/Prefabs/CandleV1", new Vector3(at.x + .2f, .8f, at.y), 0, .2f);
                    Glow(t, new Vector3(-hx + .2f, 2.3f, at.y), 4, .35f, new Color(1, .76f, .45f), .9f);
                    continue;
                }
                GameObject table = kitTables ? ModelProp(t, "Fantasy/Table_Large", new Vector3(at.x, .045f, at.y), 0, .8f, false, 1.5f) : null;
                if (table == null)
                {
                    table = Part(PrimitiveType.Cylinder, t, new Vector3(at.x, .78f, at.y), new Vector3(1.3f, .04f, 1.3f), oak);
                    Part(PrimitiveType.Cylinder, t, new Vector3(at.x, .4f, at.y), new Vector3(.16f, .38f, .16f), Gilt);
                }
                float tableTop = TopIn(t, table);
                for (int k = 0; k < 4; k++)
                {
                    var seat = plan.seats[12 + (n - 3) * 4 + k].at; var toward = at - seat; float yaw = Mathf.Atan2(toward.x, toward.y) * Mathf.Rad2Deg;
                    if (ModelProp(t, "KayKit/chair", new Vector3(seat.x, .045f, seat.y), yaw, 1.05f) == null)
                    { Part(PrimitiveType.Cylinder, t, new Vector3(seat.x, .25f, seat.y), new Vector3(.42f, .25f, .42f), oak); Part(PrimitiveType.Cylinder, t, new Vector3(seat.x, .52f, seat.y), new Vector3(.44f, .03f, .44f), Cushion); }
                }
                ModelProp(t, "Fantasy/CandleStick", new Vector3(at.x + .15f, tableTop, at.y - .1f), 0, .22f); ModelProp(t, "Fantasy/Mug", new Vector3(at.x - .3f, tableTop, at.y + .25f), n * 70, .12f);
            }
            // ---------- the light: two chandeliers in the open, sconces on the walls ----------
            foreach (float x in new[] { -3.2f, 3.2f }) { ModelProp(t, "Fantasy/Chandelier", new Vector3(x, H - 1.9f, -1.6f), 0, 1.2f); Part(PrimitiveType.Cylinder, t, new Vector3(x, H - .02f, -1.6f), new Vector3(.8f, .02f, .8f), Gilt); }
            var room = new GameObject("Taproom light").AddComponent<Light>(); room.transform.SetParent(t, false);
            room.transform.localPosition = new Vector3(0, H - 1.6f, -1.6f); room.type = LightType.Point; room.range = 16; room.intensity = 1.3f; room.color = new Color(1, .82f, .56f);
            HallLight(t, new Vector3(0, gy + 2.2f, hz - 1.3f), 10, .8f);   // over the gallery and the bar
            foreach (int s in new[] { -1, 1 }) Glow(t, new Vector3(s * (hx - .15f), gy + 2.2f, -2), 6, .35f, new Color(1, .76f, .45f), 1);
            // Registered: its seats, its stools and its bench, for the city's life and the sims (VillageLife.FindPlaces, SimFigure.Inn).
            RegisterInn(t, "city", innDoor, plan, plan.Inner);
        }
    }
}
