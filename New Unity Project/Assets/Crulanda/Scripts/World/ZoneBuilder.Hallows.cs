using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Hallow's Creek, the town the Wasting paused (Docs/DUNGEON2_DESIGN.md; CANON, book1 ch.15-21: the white fences, the baker in his
    /// doorway, the ball in the air, the broken bridge's slabs, the crater and the red door, the Old Archive): the prop kinds its zone file
    /// names, each built from its own stream (BuildProps gives new kinds one), none of them a machine (DUNGEON_DESIGN 0.05):
    /// - "paused": a townsperson stone to the skin, where the Wasting caught them (the session stands the figure there: EncounterSession.Hallows;
    ///   variant 1 the baker with his tray, 2 a woman with a kettle, 3 one marked for the Bureau's cut, 4 the farm's stone dog, built here);
    /// - "hangball": the ball hanging in the air over the street; "laundry": the laundry yard's lines, taut as wire, the sheets hanging
    ///   straight down (no wind: ZoneBuilder.HallowsBooks); paused variant 5 a child caught running, 6 a dog stopped in mid-leap, 7 the
    ///   stone girl by the willow (Talla, Chris's decision of 2026-10-18), bent over the dug-out hollow;
    /// - "crater": where the town hall stood, a hemisphere of glassy obsidian down to a mirror floor, the red door on its floating marble at
    ///   its heart and the hall's debris orbiting it, a low wall of white kerb-stones round its rim (seen from the rim and the belfry; never
    ///   reached: decision 7); "look": a place E looks at (its text the prop's); "chapel", "hollow", "handcart" (ZoneBuilder.HallowsBooks);
    /// - "wellhouse": the roof over the well-house stair (the Cellars' far end); "stone": a spirit stone;
    /// - "gatehouse", "archive", "granary", "belfry": the built places' walls, doors, shelves and roofs (their floors, lofts, galleries
    ///   and stairs are the zone's decks, ZoneBuilder.HallowsGround), each with a RoofFade so the camera sees in;
    /// - "copydesk": a Bureau copying desk; "ride": the black-iron wagon (the way home, ZoneRide).
    /// A prop of style "stone" (the fossilised farm outside the town, b1 ch.17:45) is turned to grey stone after it is built: every door
    /// and window with it. One of style "white" (the town's houses, fences, gardens and yards) is whitewashed instead: the books' town is
    /// white and perfect, and only its people are stone (b1 ch.17:269; ch.21:11-25; ZoneBuilder.HallowsBooks).
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The prop kinds built here (BuildProps).</summary>
        public static readonly HashSet<string> HallowsKinds = new HashSet<string> { "paused", "hangball", "laundry", "crater", "wellhouse", "stone", "gatehouse", "archive", "granary", "belfry", "copydesk", "ride", "satchel", "townlife", "look", "chapel", "hollow", "handcart",
            // (REWORK_LOWTOWNS_HALLOWS picks 7 and 10, ZoneBuilder.HallowsRoad) the Ash Road's dangers and the bridge as the book left it
            "fissure", "wrongshadow", "stoneleaves", "driftslab", "tipslab" };
        /// <summary>A paused townsperson's place (EncounterSession.Hallows stands a stone figure there): where, facing, the pose (the prop's
        /// variant) and the name it is known by (or null).</summary>
        public readonly List<(Vector3 at, Quaternion facing, int pose, string name)> PausedSpots = new List<(Vector3, Quaternion, int, string)>();
        /// <summary>Where the wagon's two mules stand in their traces and the way they face (the session puts them there), for a "ride" prop.</summary>
        public readonly List<(Vector3 at, Quaternion facing)> RideTeam = new List<(Vector3, Quaternion)>();
        /// <summary>The paused town's stone: a grey a little warm, as the books' white fences gone to stone.</summary>
        static readonly Color PausedStone = new Color(.66f, .65f, .64f);

        /// <summary>Builds a Hallow's Creek prop kind; false for any other kind.</summary>
        bool HallowsProp(ZoneProp p, Transform t)
        {
            // The Ash Road's and the bridge's kinds (ZoneBuilder.HallowsRoad).
            if (AshRoadProp(p, t)) return true;
            switch (p.kind)
            {
                case "paused":
                    if (p.variant == 4) { StoneDog(t); return true; }
                    if (p.variant == 6) { StoneDogLeap(t); return true; }
                    PausedSpots.Add((t.position, t.rotation, p.variant, p.name)); return true;
                // (REWORK_LOWTOWNS_HALLOWS picks 3 and 12, ZoneBuilder.HallowsBooks) a place looked at with E (nothing built: its text is the
                // prop's), the chapel, the dug-out hollow by the willow, the handcart (the book's bicycle).
                case "look": return true;
                case "chapel": Chapel(t, p.size.x > 0 ? p.size : new Vector2(7, 12)); return true;
                case "hollow": DugHollow(t); return true;
                case "handcart": Handcart(t); return true;
                case "hangball": Part(PrimitiveType.Sphere, t, new Vector3(0, 2.3f, 0), Vector3.one * .24f, Tint(art.stone, new Color(.7f, .3f, .25f))); return true;
                case "laundry": Laundry(t); return true;
                case "crater": Crater(t, p.size.x > 0 ? p.size.x : 24, p.size.y > 0 ? p.size.y : 25); return true;
                case "wellhouse": WellHouse(t); return true;
                case "stone": SpiritStone(t); return true;
                case "gatehouse": Gatehouse(t, p.size.x > 0 ? p.size : new Vector2(16, 14)); return true;
                case "archive": HallowsArchive(t, p.size.x > 0 ? p.size : new Vector2(50, 24)); return true;
                case "granary": Granary(t, p.size.x > 0 ? p.size : new Vector2(40, 18)); return true;
                case "belfry": Belfry(t, p.size.x > 0 ? p.size.x : 12); return true;
                case "copydesk": CopyDesk(t); return true;
                case "satchel": Satchel(t); return true;
                case "townlife": TownLife(t, p.variant); return true;
                case "ride":
                    Wagon(t);
                    for (int k = 0; k < 2; k++) RideTeam.Add((t.TransformPoint(new Vector3((k == 0 ? -.55f : .55f), 0, 3.4f)), t.rotation));
                    return true;
            }
            return false;
        }
        /// <summary>Everything under a prop turned to the paused town's stone (style "stone"): each part's own lightness kept, its colour gone.</summary>
        void Petrify(Transform t)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) { r.enabled = false; continue; }   // no smoke from a paused chimney
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    float g = mats[i] != null && mats[i].HasProperty("_Color") ? mats[i].color.grayscale : .6f;
                    mats[i] = Tint(art.stone, Color.Lerp(PausedStone * .7f, PausedStone * 1.15f, Mathf.Clamp01(g * 1.3f)));
                }
                r.sharedMaterials = mats;
            }
            foreach (var l in t.GetComponentsInChildren<Light>(true)) l.enabled = false;   // no lamp lit, no window warm
        }

        // ---------- the town's small things ----------
        /// <summary>The farm's stone dog in its yard (never fought): sitting, head up, the way it was when the minute stopped.</summary>
        void StoneDog(Transform t)
        {
            var s = Tint(art.stone, PausedStone * .9f);
            Part(PrimitiveType.Capsule, t, new Vector3(0, .42f, 0), new Vector3(.32f, .36f, .32f), s, Quaternion.Euler(70, 0, 0));
            Part(PrimitiveType.Sphere, t, new Vector3(0, .78f, .2f), new Vector3(.24f, .22f, .3f), s);
            Part(PrimitiveType.Capsule, t, new Vector3(0, .74f, .36f), new Vector3(.1f, .1f, .14f), s, Quaternion.Euler(90, 0, 0));
            foreach (int side in new[] { -1, 1 }) { Part(PrimitiveType.Cube, t, new Vector3(side * .08f, .92f, .16f), new Vector3(.05f, .1f, .04f), s); Part(PrimitiveType.Capsule, t, new Vector3(side * .1f, .14f, .14f), new Vector3(.07f, .14f, .07f), s); }
            Solid(t, new Vector3(0, .45f, .05f), new Vector3(.4f, .9f, .7f));
        }
        /// <summary>
        /// The crater where the town hall stood (b1 ch.21:120-230; its bowl the zone's sunk shape, the prop's root on its floor): the bowl a
        /// hemisphere of smooth glassy obsidian down to a mirror floor whose reflection does not move (ObsidianBowl, ZoneBuilder.HallowsBooks);
        /// a low wall of white kerb-stones round its rim (nobody goes down: decision 7); the red door, "Bright, wet, impossible red", upright
        /// on a slab of floating marble at its heart, its brass knocker a face with its lips sewn shut (ch.21:130, 230); and round it, in a
        /// slow orbit "like a miniature asteroid belt" (ch.21:136), the hall's chairs, desks and bricks, a petrified horse, the corner of a
        /// bank vault with its rusted rebar, a park bench and a twisted wrought-iron lamppost (ch.21:188), and down in the lower orbit three
        /// foundation stones (ch.21:200). The air coming up smells of ozone and old paper (ch.21:222: the rim's look, in the zone file).
        /// </summary>
        void Crater(Transform t, float rim, float wall)
        {
            var kerb = Dressed(new Color(.88f, .87f, .84f));
            // The root stands on the bowl's floor; the rim is the yard's height.
            float up = Datum - t.position.y;
            ObsidianBowl(t, wall - .8f, up);
            int n = 40;
            for (int k = 0; k < n; k++)
            {
                float a0 = k * Mathf.PI * 2 / n, a1 = (k + 1) * Mathf.PI * 2 / n;
                var p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * wall; var p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * wall;
                var mid = (p0 + p1) / 2; var len = (p1 - p0).magnitude; var rot = Quaternion.LookRotation(p1 - p0);
                // The low wall, a kerb-post on it, and one solid over the stretch.
                Part(PrimitiveType.Cube, t, mid + Vector3.up * (up + .25f), new Vector3(.5f, .5f, len), kerb, rot);
                Part(PrimitiveType.Cylinder, t, p0 + Vector3.up * (up + .55f), new Vector3(.45f, .55f, .45f), kerb);
                Solid(t, mid + Vector3.up * (up + .7f), new Vector3(Mathf.Abs(p1.x - p0.x) + .6f, 1.4f, Mathf.Abs(p1.z - p0.z) + .6f));
            }
            // The red door on its slab of floating marble, a little under the rim's height at the bowl's heart.
            var marble = Tint(art.stone, new Color(.93f, .92f, .89f)); var vein = Tint(art.stone, new Color(.62f, .62f, .64f));
            var slab = new GameObject("The floating marble").transform; slab.SetParent(t, false); slab.localPosition = new Vector3(0, up - 1.6f, 0); slab.localRotation = Quaternion.Euler(0, 20, 0);
            Part(PrimitiveType.Cube, slab, new Vector3(0, -.3f, 0), new Vector3(3.2f, .6f, 2.6f), marble);
            for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, slab, new Vector3(-1 + k * .9f, .002f, -.2f + k * .3f), new Vector3(.03f, .004f, 1.6f), vein, Quaternion.Euler(0, 25 + k * 30, 0));
            // Its broken underside.
            for (int k = 0; k < 5; k++) Part(PrimitiveType.Cube, slab, new Vector3(-1.2f + k * .6f, -.75f - (k % 2) * .2f, (k % 3 - 1) * .7f), new Vector3(.5f, .4f, .45f), marble, Quaternion.Euler(k * 37, k * 53, k * 19));
            // Wet red: "Bright, wet, impossible red" (ch.21:130), a single wooden door, its boards and nothing round it.
            var red = Glowing(new Color(.78f, .06f, .06f), .45f); if (red.HasProperty("_Glossiness")) red.SetFloat("_Glossiness", .88f);
            var seam = Tint(art.timber, new Color(.38f, .05f, .05f));
            var door = new GameObject("The red door").transform; door.SetParent(slab, false); door.localPosition = Vector3.zero;
            Part(PrimitiveType.Cube, door, new Vector3(0, 1.1f, 0), new Vector3(1.1f, 2.2f, .1f), red);
            for (int k = 1; k < 4; k++) Part(PrimitiveType.Cube, door, new Vector3(-.55f + k * .275f, 1.1f, -.052f), new Vector3(.012f, 2.18f, .006f), seam);
            // The knocker: a brass face, its eyes shut, its lips sewn shut with five stitches; no handle, no keyhole (ch.21:230).
            var brass = Tint(art.metal, new Color(.78f, .6f, .26f)); var shade = Tint(art.metal, new Color(.25f, .18f, .08f));
            var face = new GameObject("The knocker").transform; face.SetParent(door, false); face.localPosition = new Vector3(0, 1.35f, -.07f);
            Part(PrimitiveType.Sphere, face, Vector3.zero, new Vector3(.22f, .27f, .07f), brass);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, face, new Vector3(s * .045f, .04f, -.03f), new Vector3(.04f, .008f, .01f), shade);
            Part(PrimitiveType.Sphere, face, new Vector3(0, -.005f, -.035f), new Vector3(.03f, .05f, .03f), brass);
            Part(PrimitiveType.Cube, face, new Vector3(0, -.065f, -.03f), new Vector3(.08f, .008f, .01f), shade);
            for (int k = 0; k < 5; k++) Part(PrimitiveType.Cube, face, new Vector3(-.032f + k * .016f, -.065f, -.034f), new Vector3(.004f, .03f, .006f), shade);
            // The ring under its chin.
            Part(PrimitiveType.Cylinder, face, new Vector3(0, -.17f, -.02f), new Vector3(.12f, .008f, .12f), brass, Quaternion.Euler(90, 0, 0));
            Glow(door, new Vector3(0, 1.2f, -.8f), 14, .9f, new Color(1, .25f, .2f), 1.1f);
            // The debris field, orbiting the door slowly (ch.21:136): turned by a Spinner about the bowl's upright axis.
            var field = new GameObject("The debris field").transform; field.SetParent(t, false); field.localPosition = Vector3.zero;
            var spin = field.gameObject.AddComponent<Spinner>(); spin.axis = Vector3.up; spin.degreesPerSecond = .7f;
            var wood = Tint(art.timber, new Color(.36f, .27f, .19f)); var oak = Tint(art.timber, new Color(.3f, .21f, .13f)); var brick = Tint(art.stone, new Color(.55f, .36f, .29f));
            var stone = Tint(art.stone, PausedStone * .92f); var iron = Tint(art.metal, new Color(.16f, .16f, .18f)); var rust = Tint(art.metal, new Color(.45f, .22f, .1f)); var masonry = Dressed(new Color(.6f, .58f, .55f));
            var rr = new System.Random(4471);
            float R() { return (float)rr.NextDouble(); }
            Transform Piece(string name, float r, float y)
            {
                float a = (float)(rr.NextDouble() * Mathf.PI * 2);
                var p = new GameObject(name).transform; p.SetParent(field, false);
                p.localPosition = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); p.localRotation = Quaternion.Euler(rr.Next(360), rr.Next(360), rr.Next(360));
                return p;
            }
            float Ring() { return 6 + R() * (rim - 9); }
            float High() { return up - 9 + R() * 10; }
            for (int k = 0; k < 7; k++)
            {
                var chair = Piece("A chair, orbiting", Ring(), High());
                Part(PrimitiveType.Cube, chair, Vector3.zero, new Vector3(.45f, .06f, .45f), wood); Part(PrimitiveType.Cube, chair, new Vector3(0, .3f, -.2f), new Vector3(.45f, .6f, .05f), wood);
                foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, chair, new Vector3(sx * .19f, -.22f, sz * .19f), new Vector3(.05f, .44f, .05f), wood);
            }
            for (int k = 0; k < 4; k++)
            {
                var desk = Piece("A heavy oak desk, orbiting", Ring(), High());
                Part(PrimitiveType.Cube, desk, Vector3.zero, new Vector3(1.5f, .07f, .75f), oak);
                Part(PrimitiveType.Cube, desk, new Vector3(.45f, -.33f, 0), new Vector3(.5f, .6f, .7f), oak);
                foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, desk, new Vector3(-.68f, -.36f, sz * .32f), new Vector3(.07f, .7f, .07f), oak);
            }
            for (int k = 0; k < 22; k++) Part(PrimitiveType.Cube, Piece("A brick", Ring(), High()), Vector3.zero, new Vector3(.24f, .1f, .12f) * (1 + k % 2), brick);
            // The petrified horse (ch.21:188, "a floating, petrified statue of a horse").
            var horse = Piece("A stone horse, orbiting", rim - 8, up - 3);
            Part(PrimitiveType.Capsule, horse, Vector3.zero, new Vector3(.62f, .85f, .62f), stone, Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Capsule, horse, new Vector3(0, .45f, .75f), new Vector3(.3f, .45f, .3f), stone, Quaternion.Euler(35, 0, 0));
            Part(PrimitiveType.Cube, horse, new Vector3(0, .8f, 1.05f), new Vector3(.24f, .26f, .55f), stone, Quaternion.Euler(20, 0, 0));
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Capsule, horse, new Vector3(sx * .18f, -.65f, sz * .55f), new Vector3(.13f, .48f, .13f), stone);
            Part(PrimitiveType.Capsule, horse, new Vector3(0, .05f, -.9f), new Vector3(.08f, .3f, .08f), stone, Quaternion.Euler(-30, 0, 0));
            // The bank vault's reinforced corner, its rusted rebar sticking out like broken bones (ch.21:188).
            var vault = Piece("A vault's corner, orbiting", rim - 6, up - 5);
            Part(PrimitiveType.Cube, vault, new Vector3(0, 0, .9f), new Vector3(.5f, 1.8f, 2.2f), masonry); Part(PrimitiveType.Cube, vault, new Vector3(.85f, 0, 0), new Vector3(2.2f, 1.8f, .5f), masonry);
            for (int k = 0; k < 6; k++)
            {
                var a = new Vector3(-.1f + (k % 3) * .7f, -.7f + (k / 3) * 1.3f, k % 2 == 0 ? 2 : -.1f);
                Rod(vault, a, a + new Vector3(R() * .6f - .3f, R() * .4f - .2f, k % 2 == 0 ? .8f : -.7f), .04f, rust);
            }
            // A petrified park bench, and a twisted wrought-iron lamppost (ch.21:188).
            var bench = Piece("A park bench, orbiting", Ring(), High());
            Part(PrimitiveType.Cube, bench, Vector3.zero, new Vector3(1.7f, .07f, .45f), stone); Part(PrimitiveType.Cube, bench, new Vector3(0, .3f, -.22f), new Vector3(1.7f, .42f, .06f), stone);
            foreach (int sx in new[] { -1, 1 }) Part(PrimitiveType.Cube, bench, new Vector3(sx * .7f, -.22f, 0), new Vector3(.08f, .44f, .42f), stone);
            var lamp = Piece("A lamppost, orbiting", Ring(), High());
            var prev = new Vector3(0, -1.8f, 0);
            for (int k = 1; k <= 8; k++) { var next = new Vector3(Mathf.Sin(k * 1.3f) * .12f, -1.8f + k * .45f, Mathf.Cos(k * 1.3f) * .12f); Rod(lamp, prev, next, .07f, iron); prev = next; }
            Part(PrimitiveType.Cube, lamp, prev + new Vector3(0, .18f, 0), new Vector3(.3f, .36f, .3f), iron, Quaternion.Euler(0, 45, 0));
            // The lower orbit: three of the hall's foundation stones, down near the glass (ch.21:200).
            for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, Piece("A foundation stone, in the lower orbit", 5 + k * 2.5f, 2.5f + k * 1.2f), Vector3.zero, new Vector3(2, 1.1f, 1.4f), masonry);
        }
        /// <summary>The well-house: four posts and a stone-tiled roof over the stair down (the Cellars' far end comes up here).</summary>
        void WellHouse(Transform t)
        {
            var post = Tint(art.timber, PausedStone * .8f); var roof = Tint(art.slate != null ? art.slate : art.stone, PausedStone * .8f);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, new Vector3(sx * 2.9f, 1.4f, sz * 2.9f), new Vector3(.22f, 2.8f, .22f), post);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.9f, 0), new Vector3(6.6f, .2f, 6.6f), roof);
            Part(PrimitiveType.Cube, t, new Vector3(0, 3.3f, 0), new Vector3(4.6f, .6f, 4.6f), roof);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Solid(t, new Vector3(sx * 2.9f, 1.4f, sz * 2.9f), new Vector3(.3f, 2.8f, .3f));
        }
        /// <summary>A spirit stone: a tall grey standing stone with its marks faintly lit (where you wake if you fall in here once you have touched it).</summary>
        void SpiritStone(Transform t)
        {
            var rock = RockTint(new Color(.55f, .55f, .58f));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.1f, 0), new Vector3(.8f, 2.2f, .5f), rock, Quaternion.Euler(0, 0, 3));
            var mark = Glowing(new Color(.65f, .8f, 1f), 1.1f);
            for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, t, new Vector3(0, .7f + k * .45f, -.26f), new Vector3(.3f - k * .06f, .05f, .02f), mark);
            Glow(t, new Vector3(0, 1.3f, -.8f), 6, .7f, new Color(.6f, .75f, 1f), .9f);
            Solid(t, new Vector3(0, 1.1f, 0), new Vector3(.9f, 2.2f, .6f));
        }
        /// <summary>The Warden's satchel on her cell's floor (the Archive's floor, two metres up: set down on the deck there).</summary>
        void Satchel(Transform t)
        {
            t.position = StandAt(new Vector2(t.position.x, t.position.z), Datum + 2.5f);
            var leather = Tint(art.timber, new Color(.3f, .27f, .24f)); var tape = Tint(art.plaster, new Color(.62f, .62f, .64f));
            Part(PrimitiveType.Cube, t, new Vector3(0, .17f, 0), new Vector3(.5f, .34f, .2f), leather, Quaternion.Euler(0, 15, 0));
            Part(PrimitiveType.Cube, t, new Vector3(.05f, .03f, .1f), new Vector3(.3f, .06f, .22f), tape, Quaternion.Euler(0, -10, 0));
        }
        void CopyDesk(Transform t)
        {
            var wood = Tint(art.timber, new Color(.28f, .22f, .17f)); var paper = Tint(art.plaster, new Color(.9f, .88f, .8f)); var ink = Tint(art.stone, new Color(.05f, .05f, .07f));
            Part(PrimitiveType.Cube, t, new Vector3(0, .78f, 0), new Vector3(1.6f, .08f, .8f), wood, Quaternion.Euler(-8, 0, 0));
            foreach (int sx in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * .72f, .38f, 0), new Vector3(.08f, .76f, .7f), wood);
            for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, t, new Vector3(-.45f + k * .4f, .86f + k * .03f, .05f), new Vector3(.3f, .04f + k * .05f, .4f), paper);
            Part(PrimitiveType.Cylinder, t, new Vector3(.6f, .88f, -.2f), new Vector3(.08f, .05f, .08f), ink);
            Solid(t, new Vector3(0, .45f, 0), new Vector3(1.7f, .9f, .9f));
        }

        // ---------- the built places ----------
        /// <summary>A wall of a built place from a to b (the root's own x and z), from <paramref name="y0"/> up to <paramref name="y1"/>,
        /// solid and in the navmesh's way.</summary>
        void HWall(Transform t, Vector2 a, Vector2 b, float y0, float y1, float thick, Material m)
        {
            var d = b - a; float len = d.magnitude; if (len < .05f || y1 - y0 < .05f) return;
            var rot = Quaternion.LookRotation(new Vector3(d.x, 0, d.y)); var mid = new Vector3((a.x + b.x) / 2, (y0 + y1) / 2, (a.y + b.y) / 2);
            BoxPart(t, mid, new Vector3(thick, y1 - y0, len), m, rot);
            var box = t.gameObject.AddComponent<BoxCollider>(); box.center = mid;
            box.size = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new Vector3(len, y1 - y0, thick) : new Vector3(thick, y1 - y0, len);
            if (t.GetComponent<NavBlocker>() == null) t.gameObject.AddComponent<NavBlocker>();
        }
        /// <summary>A straight wall with doorways in it (each its centre along the wall from a, its width and its height over y0), the wall
        /// over each doorway carried on above it.</summary>
        void HWallDoors(Transform t, Vector2 a, Vector2 b, float y0, float y1, float thick, Material m, params (float at, float width, float high)[] doors)
        {
            var dir = (b - a).normalized; float len = (b - a).magnitude, from = 0;
            var sorted = new List<(float at, float width, float high)>(doors); sorted.Sort((x, y) => x.at.CompareTo(y.at));
            foreach (var (at, width, high) in sorted)
            {
                float d0 = Mathf.Max(from, at - width / 2), d1 = Mathf.Min(len, at + width / 2);
                HWall(t, a + dir * from, a + dir * d0, y0, y1, thick, m);
                if (y0 + high < y1) HWall(t, a + dir * d0, a + dir * d1, y0 + high, y1, thick, m);
                from = d1;
            }
            HWall(t, a + dir * from, b, y0, y1, thick, m);
        }
        /// <summary>A roof over a built place that fades where it hides you (RoofFade), and the walls with it.</summary>
        void Fades(Transform t, Vector2 size)
        {
            var fade = t.gameObject.AddComponent<RoofFade>(); fade.Half = size / 2 - Vector2.one * .3f; fade.Walled = true;
        }
        /// <summary>A ring of pale lamps round a built place's walls (the maze lesson, ADIT_REWORK L9): you always know which wall you are by.</summary>
        void LampRing(Transform t, Vector2 size, float y, Color c, int each)
        {
            var glass = Glowing(c, 1.2f);
            for (int k = 0; k < each; k++)
                foreach (var (x, z) in new[] { (Mathf.Lerp(-size.x / 2 + 2, size.x / 2 - 2, (k + .5f) / each), size.y / 2 - .9f), (Mathf.Lerp(-size.x / 2 + 2, size.x / 2 - 2, (k + .5f) / each), -size.y / 2 + .9f) })
                {
                    Part(PrimitiveType.Cube, t, new Vector3(x, y, z), new Vector3(.18f, .26f, .18f), glass);
                    if (k % 2 == 0) Glow(t, new Vector3(x, y, z * .9f), 12, .7f, c, .9f);
                }
        }

        /// <summary>The bridge-head gatehouse (16 x 14, 8 m high): open to the bridge on the south, its gate in the north wall (the zone's gate
        /// there, Tredway's), battlements along the top; the wall-walk inside the south wall and its stair are decks.</summary>
        void Gatehouse(Transform t, Vector2 size)
        {
            var wall = Dressed(PausedStone * .95f); float w = size.x / 2, d = size.y / 2, h = 8;
            HWallDoors(t, new Vector2(-w, -d), new Vector2(w, -d), 0, h, 1.2f, wall, (w, 6, 4.4f));   // the south face: open to the bridge
            // The north face: a doorway for each of the zone's gates that stands in it (Tredway's gate; the sally-port beside it, barred from the
            // town side: DUNGEON2_DESIGN section 3), or the gate's doorway alone where the data has none.
            var doors = new List<(float at, float width, float high)>();
            if (Zone.gates != null)
                foreach (var gd in Zone.gates)
                {
                    if (gd == null || !string.IsNullOrEmpty(gd.cave) || gd.at == Vector2.zero) continue;
                    var local = t.InverseTransformPoint(new Vector3(gd.at.x, t.position.y, gd.at.y));
                    if (Mathf.Abs(local.z - d) < 1.5f && Mathf.Abs(local.x) < w - .5f) doors.Add((local.x + w, (gd.width > 0 ? gd.width : 6) + 1, 4.4f));
                }
            if (doors.Count == 0) doors.Add((w, 7, 4.4f));
            HWallDoors(t, new Vector2(-w, d), new Vector2(w, d), 0, h, 1.2f, wall, doors.ToArray());
            HWall(t, new Vector2(-w, -d), new Vector2(-w, d), 0, h, 1.2f, wall); HWall(t, new Vector2(w, -d), new Vector2(w, d), 0, h, 1.2f, wall);
            for (float x = -w; x <= w; x += 2) foreach (float z in new[] { -d, d }) Part(PrimitiveType.Cube, t, new Vector3(x, h + .4f, z), new Vector3(1, .8f, 1.2f), wall);
            Glow(t, new Vector3(0, 6.5f, 0), 14, .6f, new Color(.85f, .82f, .9f), .9f);
            Fades(t, size);
        }
        /// <summary>
        /// The Old Archive (CANON, book1 ch.18: a First Era library, columns twisted like pulled taffy, the crest of an open book with an eye,
        /// its doors blown in, a maze of petrified shelves): a hall 50 x 24 and 12 m high, its roof broken through over the reading hall, its floor two metres up (the zone's
        /// deck), a doorless front on the west, the shelves standing three metres high as the walls of the maze, twisted columns along the walls,
        /// the Registrar's desk in the apse, and off the north wall three cells (the middle one the Warden's, behind its pen gate).
        /// </summary>
        void HallowsArchive(Transform t, Vector2 size)
        {
            float w = size.x / 2, d = size.y / 2, h = 12, f = 2;
            var wall = Dressed(new Color(.6f, .58f, .55f));
            HWallDoors(t, new Vector2(-w, -d), new Vector2(-w, d), 0, h, 1, wall, (d, 7, 7));                              // the west front: its doors long gone
            HWall(t, new Vector2(-w, -d), new Vector2(w, -d), 0, h, 1, wall); HWall(t, new Vector2(w, -d), new Vector2(w, d), 0, h, 1, wall);
            HWallDoors(t, new Vector2(-w, d), new Vector2(w, d), 0, h, 1, wall, (w - 8, 2.4f, f + 2.8f), (w, 2.4f, f + 2.8f), (w + 8, 2.4f, f + 2.8f));   // the north wall and the three cells' doors
            // The cells: 6 x 5 rooms behind the north wall, a flat roof over them.
            HWall(t, new Vector2(-11, d + 5), new Vector2(11, d + 5), 0, f + 4, .8f, wall);
            foreach (float x in new[] { -11f, -5f, -3f, 3f, 5f, 11f }) HWall(t, new Vector2(x, d), new Vector2(x, d + 5), 0, f + 4, .6f, wall);
            Part(PrimitiveType.Cube, t, new Vector3(0, f + 4.1f, d + 2.5f), new Vector3(22.6f, .25f, 5.4f), wall);
            // Twisted columns along the long walls (the taffy: drums each turned a little more than the one under it).
            for (float x = -w + 4; x < w - 2; x += 6)
                foreach (float z in new[] { -d + 1.2f, d - 1.2f })
                    for (int k = 0; k < 8; k++) Part(PrimitiveType.Cube, t, new Vector3(x, f + .6f + k * 1.15f, z), new Vector3(.9f, 1.15f, .9f), wall, Quaternion.Euler(0, k * 11, k % 2 == 0 ? 3 : -3));
            // The shelves: three metres of stone ledgers, the maze's walls, with gaps for the ways through (ArchiveStack: a frame and
            // its books on both faces, ZoneBuilder.HallowsTown).
            var stacks = new System.Random(6143);
            void Shelf(float x, float z0, float z1)
            {
                var mid = new Vector3(x, f + 1.5f, (z0 + z1) / 2); float len = z1 - z0;
                ArchiveStack(t, x, z0, z1, f, stacks);
                Solid(t, mid, new Vector3(.75f, 3, len));
            }
            foreach (float x in new[] { -12f, 0f, 9f }) { Shelf(x, -d + 3, -2.5f); Shelf(x, 2.5f, d - 3); }
            Shelf(-20, -d + 4, -6); Shelf(-20, 6, d - 4);
            // The apse desk, and the crest of the open book with an eye over it on the east wall.
            var wood = Tint(art.timber, new Color(.32f, .26f, .2f));
            Part(PrimitiveType.Cube, t, new Vector3(w - 4, f + .55f, 0), new Vector3(1.2f, 1.1f, 4), wood); Solid(t, new Vector3(w - 4, f + .55f, 0), new Vector3(1.3f, 1.1f, 4.1f));
            var crest = Tint(art.stone, new Color(.78f, .74f, .62f));
            Part(PrimitiveType.Cube, t, new Vector3(w - .55f, 9, -.8f), new Vector3(.1f, 1.6f, 1.5f), crest, Quaternion.Euler(0, 0, 0));
            Part(PrimitiveType.Cube, t, new Vector3(w - .55f, 9, .8f), new Vector3(.1f, 1.6f, 1.5f), crest);
            Part(PrimitiveType.Sphere, t, new Vector3(w - .6f, 10.2f, 0), new Vector3(.12f, .5f, .8f), Glowing(new Color(.9f, .85f, .6f), .6f));
            // Its roof (broken over the reading hall), gables, windows, portal, blown-in doors, cases, desks, papers and lamps.
            ArchiveDressing(t, w, d, h, f);
            LampRing(t, size, f + 3.2f, new Color(.95f, .9f, .7f), 6);
            Fades(t, size);
        }
        /// <summary>The Granary on the race (GAME-ONLY): the town's tithe barn, 40 x 18 and 10 m high, its two lofts the zone's decks; a door on the
        /// west to the footbridge and one on the south to the wagon yard, a gabled roof, the sorting floor's tables and the Bureau's crates.</summary>
        void Granary(Transform t, Vector2 size)
        {
            float w = size.x / 2, d = size.y / 2, h = 10;
            var wall = Dressed(new Color(.6f, .58f, .54f)); var roof = Tint(art.slate != null ? art.slate : art.stone, new Color(.4f, .4f, .44f)); var wood = Tint(art.timber, new Color(.34f, .3f, .26f));
            HWallDoors(t, new Vector2(-w, -d), new Vector2(-w, d), 0, h, .9f, wall, (d, 4.5f, 4.2f));
            HWall(t, new Vector2(w, -d), new Vector2(w, d), 0, h, .9f, wall);
            HWall(t, new Vector2(-w, d), new Vector2(w, d), 0, h, .9f, wall);
            HWallDoors(t, new Vector2(-w, -d), new Vector2(w, -d), 0, h, .9f, wall, (w, 5.5f, 4.6f));
            // The two slopes down from the ridge to the eaves (each turned so its outer edge is the low one: they had been turned the other
            // way, a V standing open to the sky, frames of 2026-10-16), and the gables under the ridge at each end.
            foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(0, h + 2.2f, side * d * .5f), new Vector3(size.x + 1.2f, .3f, d * 1.25f), roof, Quaternion.Euler(side * 26, 0, 0));
            foreach (int side in new[] { -1, 1 }) MeshPart(ZoneMeshes.Gable(2 * d + .9f, 4.5f, .9f, 2, 0, h), t, new Vector3(side * w, h, 0), wall, Quaternion.Euler(0, 90, 0));
            for (int k = 0; k < 4; k++) { var at = new Vector3(-8 + k * 5.5f, .5f, 0); Part(PrimitiveType.Cube, t, at, new Vector3(3.2f, 1, 1.4f), wood); Solid(t, at, new Vector3(3.3f, 1, 1.5f)); }
            LampRing(t, size, 3.2f, new Color(.95f, .85f, .6f), 4);
            Fades(t, size);
        }
        /// <summary>
        /// The Bell Tower (CANON-EXPANDED, book1 ch.21's clock tower as a bell tower; decision 4): 12 x 12 and 24 m to the belfry's lintels, a
        /// door on the south face (the ward-lock), arched openings at the belfry, a pyramid roof; inside, the flights and landings and the
        /// belfry floor are the zone's decks. The great bell hangs frozen mid-swing; on the south face, a painted dial a minute short of noon:
        /// paint on stone, no works, nothing turns (DUNGEON_DESIGN 0.05).
        /// </summary>
        void Belfry(Transform t, float size)
        {
            // (24 to the lintels' top: the openings over the parapet are tall enough for round heads, and the bell shows through them.)
            float w = size / 2, h = 24, belfry = 18;
            var wall = Dressed(new Color(.64f, .62f, .6f));
            // Below the belfry: solid walls, the door in the south one; at the belfry, a pier at each corner and an arch over each opening.
            HWallDoors(t, new Vector2(-w, -w), new Vector2(w, -w), 0, belfry, 1, wall, (w, 3, 3.6f));
            HWall(t, new Vector2(-w, w), new Vector2(w, w), 0, belfry, 1, wall);
            HWall(t, new Vector2(-w, -w), new Vector2(-w, w), 0, belfry, 1, wall); HWall(t, new Vector2(w, -w), new Vector2(w, w), 0, belfry, 1, wall);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) HWall(t, new Vector2(sx * (w - 1), sz * w), new Vector2(sx * w, sz * w), belfry, h, 1, wall);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) HWall(t, new Vector2(sx * w, sz * (w - 1)), new Vector2(sx * w, sz * w), belfry, h, 1, wall);
            foreach (int s in new[] { -1, 1 })
            {
                HWall(t, new Vector2(-w, s * w), new Vector2(w, s * w), h - 1.2f, h, 1, wall); HWall(t, new Vector2(s * w, -w), new Vector2(s * w, w), h - 1.2f, h, 1, wall);
                // a parapet at the belfry floor (no knockback near the arches: nobody falls out of them)
                HWall(t, new Vector2(-w, s * w), new Vector2(w, s * w), belfry, belfry + 1.1f, .6f, wall); HWall(t, new Vector2(s * w, -w), new Vector2(s * w, w), belfry, belfry + 1.1f, .6f, wall);
            }
            // The base course, buttresses, quoins, string course, slits, the belfry's arches and the pyramid roof (ZoneBuilder.HallowsTown).
            BelfryDressing(t, w, belfry, h);
            // The great bell, frozen mid-swing on its beam (a dark bronze, the stone not on it: it is the one thing that still rings).
            var bronze = Tint(art.metal, new Color(.42f, .32f, .2f)); var beam = Tint(art.timber, new Color(.3f, .26f, .22f));
            Part(PrimitiveType.Cube, t, new Vector3(0, h - 1.6f, 0), new Vector3(size - 1.5f, .45f, .45f), beam);
            var bell = new GameObject("The great bell").transform; bell.SetParent(t, false); bell.localPosition = new Vector3(0, h - 1.8f, 0); bell.localRotation = Quaternion.Euler(24, 0, 0);
            Part(PrimitiveType.Cylinder, bell, new Vector3(0, -1.1f, 0), new Vector3(1.6f, .9f, 1.6f), bronze);
            Part(PrimitiveType.Sphere, bell, new Vector3(0, -.3f, 0), new Vector3(1.5f, .9f, 1.5f), bronze);
            Part(PrimitiveType.Cylinder, bell, new Vector3(0, -2.05f, 0), new Vector3(2.1f, .08f, 2.1f), bronze);
            // The painted dial on the south face, a minute short of noon.
            var face = Tint(art.plaster, new Color(.86f, .84f, .78f)); var paint = Tint(art.stone, new Color(.08f, .08f, .1f));
            var dial = new GameObject("The painted dial").transform; dial.SetParent(t, false); dial.localPosition = new Vector3(0, 14, -w - .52f);
            Part(PrimitiveType.Cylinder, dial, Vector3.zero, new Vector3(3.4f, .02f, 3.4f), face, Quaternion.Euler(90, 0, 0));
            for (int k = 0; k < 12; k++) { float a = k * 30 * Mathf.Deg2Rad; Part(PrimitiveType.Cube, dial, new Vector3(Mathf.Sin(a) * 1.45f, Mathf.Cos(a) * 1.45f, -.03f), new Vector3(.08f, .26f, .01f), paint, Quaternion.Euler(0, 0, -k * 30)); }
            Part(PrimitiveType.Cube, dial, new Vector3(Mathf.Sin(-.5f * Mathf.Deg2Rad) * .45f, .45f, -.04f), new Vector3(.12f, .9f, .01f), paint, Quaternion.Euler(0, 0, .5f));   // the hour, at noon all but a minute
            Part(PrimitiveType.Cube, dial, new Vector3(Mathf.Sin(-6 * Mathf.Deg2Rad) * .62f, Mathf.Cos(-6 * Mathf.Deg2Rad) * .62f, -.05f), new Vector3(.07f, 1.24f, .01f), paint, Quaternion.Euler(0, 0, 6));   // the minute, one short
            Glow(t, new Vector3(0, belfry + 2.5f, 0), 14, .8f, new Color(.85f, .82f, .92f), 1);
            for (float y = 3; y < belfry; y += 4.5f) Glow(t, new Vector3(0, y, 0), 9, .6f, new Color(.95f, .88f, .7f), .9f);
            Fades(t, new Vector2(size, size));
        }
    }
}
