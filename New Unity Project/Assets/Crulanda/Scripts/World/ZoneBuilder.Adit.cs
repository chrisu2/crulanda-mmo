using System;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Sealed Adit's insides (Docs/DUNGEON_DESIGN.md; dungeon step D1): the old mine under the Shattered Peaks (CANON, book2 ch.19)
    /// and what the Sandthrone, the Ash cell and the Wasting have made of it (GAME-ONLY). Cavern variant 2, one cave of the network
    /// at a time (tools/wip/dungeon/adit_layout.py lays them out), each part lit its own colour so you always know where you are:
    /// - the Sealed Adit itself: the Old Workings (timber sets, rails and ore carts, green lum-tubes, the cages, the gang-boss's
    ///   hall), the Singing Gallery (high pale light, dripstone, a pair of coloured lamps at each way on), the cage gate, the shaft
    ///   stair (treads and a rope rail, cold lamps) and the Rail Hall (the old line, a flagged platform, lamps on iron posts, the
    ///   armoured carriage loaded with geodes, a dark mouth where the line runs on);
    /// - the Geode Floor: violet crystal in the walls, cutting benches, troughs of shards, the Rock-Eater's boring engine;
    /// - the Ember Vent: a lake of molten rock in the great hall with a way round it, embers in the walls, the cell's braziers,
    ///   banners and altar;
    /// - the Grey Breach: dim, the walls going grey, patches of unmade static, and a stretch of floor that is not there.
    /// Nothing of one cave stands in another's way (Hollow.OpenInOther). Its own stream (TreeRandom by the cave's root).
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The usable things the Adit's quests name (Quests/adit.json): the pressed goblins' cages in the Workings ("The Pressed",
        /// five, each opened once) and the pilgrims' echo-jars round the Gallery ("Echoes in the Stone", seven, each back after a while).</summary>
        public const string AditCage = "A goblin cage", AditEchoJar = "A pilgrim's echo-jar";
        /// <summary>Dungeon step D3: the gates across the main way (ZoneGate), the powder for the loud way through the second, the
        /// Gallery's spirit stone, and the elite whose people answer the loud way (EncounterSession.Adit).</summary>
        public const string AditLiftGate = "The cage-lift gate", AditPlatformGate = "The platform gate", AditPowder = "A keg of blasting powder",
            AditSpiritStone = "The Gallery's spirit stone", AditQuartermaster = "Quartermaster Brannigan Sorrel";
        static readonly Color LumGreen = new Color(.45f, 1, .55f), GeodeViolet = new Color(.7f, .42f, 1), EmberRed = new Color(1, .45f, .15f),
            GreyLight = new Color(.74f, .77f, .82f), RailBlue = new Color(.55f, .75f, 1), GalleryLight = new Color(.62f, .72f, .88f);
        /// <summary>A cave's lining colour by its part (variant 2): the workings' grey-brown, a violet cast, the vent's scorched rock, the grey.</summary>
        static Color AditLining(string name)
        {
            switch (name)
            {
                case "The Geode Floor": return new Color(.5f, .46f, .52f);
                case "The Ember Vent": return new Color(.44f, .34f, .28f);
                case "The Grey Breach": return new Color(.55f, .55f, .56f);
                default: return new Color(.54f, .49f, .43f);
            }
        }
        void AditInterior(Transform t, Hollow h, Vector3[] c, Vector3[] right, Vector3[,] ring, int n, int P, Func<float, float, float> FloorY, Func<float, int> RingAt, Func<float, float, Vector3> On, Func<float, Quaternion> Along, Action<Vector3, Vector3, Quaternion> Block)
        {
            var ar = TreeRandom(t.position + new Vector3(3.3f, 0, 1.7f)); float A() { return (float)ar.NextDouble(); }
            bool Free(Vector3 local) { return !Hollow.OpenInOther(h, t.TransformPoint(local) + Vector3.up * .5f, .98f); }
            int Widest(float from, float to) { int best = -1; for (int i = 0; i < n; i++) if (h.Along[i] >= from && h.Along[i] <= to && (best < 0 || h.Half[i] > h.Half[best])) best = i; return best; }
            var timber = Tint(art.timber, new Color(.34f, .25f, .16f)); var dark = Tint(art.timber, new Color(.2f, .14f, .09f));
            var iron = Tint(art.metal, new Color(.22f, .21f, .2f)); var rust = Tint(art.metal, new Color(.36f, .23f, .15f));
            var stone = Tint(art.stone, new Color(.4f, .38f, .35f)); var straw = Tint(art.hay, new Color(.62f, .54f, .37f));
            var geode = Glowing(GeodeViolet, 1.5f);
            // A lamp: a small glowing source and its light.
            void Lamp(Vector3 at, Color col, float range, float power, float size = .12f)
            {
                Part(PrimitiveType.Sphere, t, at, Vector3.one * size, Glowing(col, 2.2f));
                Glow(t, at + Vector3.up * .1f, range * 1.4f, power * 1.9f, col, power * 1.9f);   // brighter and further: the caves read too dark (2026-10-08)
            }
            // A lamp on the wall at about head height, on the given side of ring i.
            void WallLamp(int i, int side, Color col, float range, float power)
            {
                int best = side > 0 ? 1 : P - 2;
                for (int k = 1; k < P - 1; k++)
                    if ((side > 0 ? k <= P / 2 : k >= P / 2) && Mathf.Abs(ring[i, k].y - c[i].y - 2.1f) < Mathf.Abs(ring[i, best].y - c[i].y - 2.1f)) best = k;
                var wall = ring[i, best]; var inward = c[i] + Vector3.up * 2.1f - wall; inward.y = 0; inward.Normalize();
                var at = wall + inward * .25f; if (!Free(at)) return;
                Part(PrimitiveType.Cube, t, wall + inward * .08f, new Vector3(.1f, .1f, .3f), iron, Quaternion.LookRotation(inward));
                Lamp(at, col, range, power);
            }
            // Rails down the floor from s0 to s1, `aside` off the middle: two rails and a sleeper every 1.2 m.
            void Rails(float s0, float s1, float aside, float gauge = .55f)
            {
                for (float s = s0; s < s1 - 1.2f; s += 1.2f)
                {
                    var q = Along(s); var mid = On(s, aside); if (!Free(mid)) continue;
                    Part(PrimitiveType.Cube, t, mid + Vector3.up * .04f, new Vector3(gauge * 2 + .5f, .07f, .2f), dark, q);
                    foreach (int side in new[] { -1, 1 })
                    {
                        var a = On(s, aside + side * gauge) + Vector3.up * .11f; var b = On(s + 1.2f, aside + side * gauge) + Vector3.up * .11f; var d = b - a;
                        if (d.sqrMagnitude < .01f) continue;
                        Part(PrimitiveType.Cube, t, (a + b) / 2, new Vector3(.07f, .08f, d.magnitude + .02f), iron, Quaternion.LookRotation(d));
                    }
                }
            }
            // An ore cart on the rails at s: a tipped box on four wheels.
            void Cart(float s, float aside, bool spill)
            {
                var q = Along(s); var at = On(s, aside); if (!Free(at)) return;
                Part(PrimitiveType.Cube, t, at + q * new Vector3(0, .62f, 0), new Vector3(1, .7f, 1.4f), rust, q);
                Part(PrimitiveType.Cube, t, at + q * new Vector3(0, .9f, 0), new Vector3(.86f, .12f, 1.26f), Tint(art.stone, new Color(.3f, .27f, .24f)), q);   // ore heaped in it
                foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                        Part(PrimitiveType.Cylinder, t, at + q * new Vector3(sx * .5f, .24f, sz * .45f), new Vector3(.36f, .04f, .36f), iron, q * Quaternion.Euler(0, 0, 90));
                if (spill) for (int k = 0; k < 4; k++) Lump(BoulderAt(k), t, at + q * new Vector3((A() - .5f) * 1.4f, .08f, 1 + A()), Vector3.one * (.2f + A() * .15f), stone, A() * 360);
                Block(at, new Vector3(1.1f, 1, 1.5f), q);
            }
            // A crate (or a stack), its top open on glowing geodes when `geodes`.
            void Crate(Vector3 at, Quaternion q, int high, bool geodes)
            {
                if (!Free(at)) return;
                for (int k = 0; k < high; k++) Part(PrimitiveType.Cube, t, at + Vector3.up * (.4f + k * .8f), Vector3.one * .8f, timber, q * Quaternion.Euler(0, k * 11, 0));
                if (geodes) for (int k = 0; k < 4; k++) Part(PrimitiveType.Sphere, t, at + Vector3.up * (high * .8f + .02f) + q * new Vector3((A() - .5f) * .5f, 0, (A() - .5f) * .5f), new Vector3(.22f, .16f, .2f), geode);
                Block(at, new Vector3(.9f, .8f * high, .9f), q);
            }
            // Crystal in the wall: a cluster of thin violet prisms leaning out of the rock.
            void Crystals(int i, int side, int count)
            {
                int k = side > 0 ? 2 + (int)(A() * 4) : P - 3 - (int)(A() * 4); var at = ring[i, k]; var inward = c[i] + Vector3.up * (at.y - c[i].y) - at; inward.y = 0; inward.Normalize();
                if (!Free(at + inward * .4f)) return;
                for (int q = 0; q < count; q++)
                {
                    var dir = (inward + new Vector3((A() - .5f) * .9f, .3f + A() * .9f, (A() - .5f) * .9f)).normalized; float len = .35f + A() * .8f;
                    Part(PrimitiveType.Cube, t, at - inward * .05f + dir * len / 2, new Vector3(.12f + A() * .08f, len, .12f + A() * .08f), geode, Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(0, A() * 60, 0));
                }
            }
            switch (h.Name)
            {
                case "The Geode Floor": GeodeFloor(); break;
                case "The Ember Vent": EmberVent(); break;
                case "The Grey Breach": GreyBreach(); break;
                default: MainAdit(); break;
            }

            void MainAdit()
            {
                int gallery = Widest(80, 120), hall = Widest(176, h.Length - 4);
                float galleryFrom = gallery >= 0 ? h.Along[gallery] - 16 : 85, galleryTo = gallery >= 0 ? h.Along[gallery] + 15 : 117, hallFrom = hall >= 0 ? h.Along[hall] - 16 : 178;
                // The mouth: a heavy timber portal, the old seal's boards broken and thrown down, a lantern either side.
                {
                    var q = Along(1); float w = h.Half[RingAt(1)] * .9f;
                    foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, On(1, side * w) + Vector3.up * 1.4f, new Vector3(.34f, 2.8f, .34f), dark, q);
                    Part(PrimitiveType.Cube, t, On(1, 0) + Vector3.up * 2.85f, new Vector3(w * 2 + .7f, .38f, .4f), dark, q);
                    for (int k = 0; k < 4; k++) Part(PrimitiveType.Cube, t, On(-1.5f - k * .4f, (k - 1.5f) * .7f) + Vector3.up * .06f, new Vector3(.32f, .06f, 2 + A()), timber, q * Quaternion.Euler(0, (A() - .5f) * 70, 0));
                    foreach (int side in new[] { -1, 1 }) Lamp(On(1.2f, side * (w + .3f)) + Vector3.up * 2.2f, new Color(1, .7f, .4f), 7, .9f);
                }
                // Timber sets down the drifts and the stair (not in the chambers): two posts and a cap every 2.4 m.
                for (float s = 3; s < h.Length - 2; s += 2.4f)
                {
                    if (s > galleryFrom - 2 && s < galleryTo + 2 || s > hallFrom - 2) continue;
                    int i = RingAt(s); if (h.Half[i] > 3.4f) continue;
                    var q = Along(s); float w = h.Half[i] * .78f, top = Mathf.Min(h.Height[i] * .8f, 2.7f);
                    var l = On(s, -w); var r = On(s, w); if (!Free(l) || !Free(r)) continue;
                    Part(PrimitiveType.Cube, t, l + Vector3.up * top / 2, new Vector3(.24f, top, .24f), timber, q);
                    Part(PrimitiveType.Cube, t, r + Vector3.up * top / 2, new Vector3(.24f, top, .24f), timber, q);
                    Part(PrimitiveType.Cube, t, (l + r) / 2 + Vector3.up * (top + .12f), new Vector3(w * 2 + .5f, .24f, .26f), timber, q);
                }
                // The Workings' rails, the carts on them, and the lum-tubes: green light every eight metres.
                Rails(2, galleryFrom - 1, 0);
                Cart(Mathf.Min(galleryFrom - 6, 27), -1.6f, false); Cart(Mathf.Min(galleryFrom - 30, 57), 1.6f, true);   // in the hall, off the way (a cart in the narrow drift cut the navmesh)
                int lumNo = 0;
                for (float s = 4; s < galleryFrom; s += 8, lumNo++) WallLamp(RingAt(s), lumNo % 2 == 0 ? 1 : -1, LumGreen, 8, .75f);
                // The cages: three along the left wall of the first chamber and two on the right, straw inside, doors hanging. Each is
                // usable (E, "Open the cage"): the quest "The Pressed" (Quests/adit.json) has you open all five; once opened it stays open.
                int cages = Widest(18, 36);
                if (cages >= 0)
                {
                    float s0 = h.Along[cages];
                    for (int k = 0; k < 5; k++)
                    {
                        float s = k < 3 ? s0 - 3.5f + k * 3.2f : s0 - 1.9f + (k - 3) * 3.2f; int side = k < 3 ? -1 : 1;
                        var q = Along(s); var at = On(s, side * h.Half[RingAt(s)] * .62f); if (!Free(at)) continue;
                        var cage = new GameObject(AditCage).transform; cage.SetParent(t, false); cage.localPosition = at; cage.localRotation = q;
                        Interactables.Add(new ZoneInteractable { name = AditCage, prompt = "Open the cage", kind = "cage", once = true, position = cage.position, root = cage });
                        foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                                Part(PrimitiveType.Cube, t, at + q * new Vector3(sx * .8f, .95f, sz * .8f), new Vector3(.12f, 1.9f, .12f), iron, q);
                        Part(PrimitiveType.Cube, t, at + Vector3.up * 1.92f, new Vector3(1.75f, .08f, 1.75f), iron, q);
                        for (int b = -2; b <= 2; b++) { Part(PrimitiveType.Cylinder, t, at + q * new Vector3(b * .32f, .95f, .8f), new Vector3(.04f, .95f, .04f), iron, q); Part(PrimitiveType.Cylinder, t, at + q * new Vector3(b * .32f, .95f, -.8f), new Vector3(.04f, .95f, .04f), iron, q); }
                        Part(PrimitiveType.Cube, t, at + Vector3.up * .05f, new Vector3(1.4f, .1f, 1.4f), straw, q * Quaternion.Euler(0, 20, 0));
                        Block(at, new Vector3(1.75f, 2, 1.75f), q);
                    }
                }
                // The gang-boss's hall: a long table on trestles with his tally book and a lantern, crates and a weapon rack.
                int boss = Widest(44, 64);
                if (boss >= 0)
                {
                    float s0 = h.Along[boss]; var q = Along(s0); var table = On(s0 + 1.5f, h.Half[boss] * .45f);
                    if (Free(table))
                    {
                        foreach (int b in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, table + q * new Vector3(0, .38f, b * .9f), new Vector3(.9f, .76f, .12f), dark, q);
                        Part(PrimitiveType.Cube, t, table + Vector3.up * .8f, new Vector3(1.1f, .08f, 2.6f), timber, q);
                        Part(PrimitiveType.Cube, t, table + q * new Vector3(.1f, .87f, -.5f), new Vector3(.36f, .06f, .28f), Tint(art.cloth, new Color(.4f, .22f, .14f)), q);   // the tally book
                        Lamp(table + q * new Vector3(-.2f, 1.05f, .6f), new Color(1, .72f, .4f), 9, 1.1f, .09f);
                        Block(table, new Vector3(1.2f, .9f, 2.7f), q);
                    }
                    for (int k = 0; k < 3; k++) Crate(On(s0 - 4 + k * 1.1f, -h.Half[boss] * .7f), q, 1 + k % 2, false);
                    var rack = On(s0 - 2, h.Half[boss] * .75f);
                    if (Free(rack)) { Part(PrimitiveType.Cube, t, rack + Vector3.up * .9f, new Vector3(.12f, 1.8f, 1.6f), dark, q); for (int k = 0; k < 4; k++) Part(PrimitiveType.Cylinder, t, rack + q * new Vector3(.12f, 1, -.6f + k * .4f), new Vector3(.05f, .8f, .05f), iron, q * Quaternion.Euler(8, 0, 0)); }
                }
                // The Singing Gallery: pale light high up, dripstone, the old miners' vent-holes dark in the roof, and at each way on a
                // pair of lamps in its own colour.
                if (gallery >= 0)
                {
                    for (int k = -1; k <= 1; k++) { int i = RingAt(h.Along[gallery] + k * 9); Glow(t, c[i] + Vector3.up * h.Height[i] * .78f, 26, .8f, GalleryLight, .8f); }
                    for (int i = 0; i < n; i++)
                    {
                        if (h.Along[i] < galleryFrom || h.Along[i] > galleryTo || h.Half[i] < 8 || i % 2 == 1) continue;
                        foreach (int side in new[] { -1, 1 })
                        {
                            if (A() < .45f) continue;
                            float off = h.Half[i] * (.7f + A() * .2f) * side, tall = 1 + A() * 2.2f;
                            var foot = c[i] + right[i] * off; foot.y = c[i].y - .1f; if (!Free(foot)) continue;
                            MeshPart(Drip(i), t, foot, stone).transform.localScale = new Vector3(.4f + A() * .4f, tall, .4f + A() * .4f);
                            var hang = c[i] + right[i] * (off * .7f) + Vector3.up * h.Height[i] * .9f;
                            MeshPart(Drip(i + 1), t, hang, stone, Quaternion.Euler(180, A() * 360, 0)).transform.localScale = new Vector3(.3f + A() * .3f, 1 + A() * 1.8f, .3f + A() * .3f);
                        }
                    }
                    for (int k = 0; k < 4; k++) { int i = RingAt(galleryFrom + 6 + k * 6); Part(PrimitiveType.Sphere, t, c[i] + Vector3.up * h.Height[i] * .97f + right[i] * (k - 1.5f) * 3, new Vector3(1.6f, .3f, 1.3f), Tint(art.stone, new Color(.03f, .03f, .04f))); }
                    // The pilgrims' echo-jars: seven clay jars set down round the Gallery's floor where the singing is loudest, a faint
                    // light in each. Taken (E) while "Echoes in the Stone" wants one, a jar is gone for a while and comes back (kind crates).
                    // Fixed places, no draws from the stream, so nothing else in the cave moves.
                    var clay = Tint(art.stone, new Color(.62f, .5f, .38f)); var echo = Glowing(new Color(1, .82f, .9f), .9f);
                    for (int k = 0; k < 7; k++)
                    {
                        float s = galleryFrom + 3 + k * (galleryTo - galleryFrom - 6) / 6f; int i = RingAt(s);
                        var at = On(s, (k % 2 == 0 ? -1 : 1) * h.Half[i] * (.42f + (k % 3) * .06f)); at.y = FloorY(at.x, at.z); if (!Free(at)) continue;
                        var jar = new GameObject(AditEchoJar).transform; jar.SetParent(t, false); jar.localPosition = at; jar.localRotation = Along(s) * Quaternion.Euler(0, k * 47, 0);
                        Part(PrimitiveType.Sphere, jar, new Vector3(0, .22f, 0), new Vector3(.34f, .42f, .34f), clay);
                        Part(PrimitiveType.Cylinder, jar, new Vector3(0, .47f, 0), new Vector3(.15f, .06f, .15f), clay);
                        Part(PrimitiveType.Sphere, jar, new Vector3(0, .53f, 0), new Vector3(.1f, .05f, .1f), echo);   // what it holds, showing at the neck
                        Interactables.Add(new ZoneInteractable { name = AditEchoJar, prompt = "Take the echo-jar", item = "item.adit_echo_jar", kind = "crates", position = jar.position, root = jar });
                    }
                    foreach (var o in Hollow.All)
                    {
                        if (o.Parent != h) continue;
                        var col = o.Name == "The Geode Floor" ? GeodeViolet : o.Name == "The Ember Vent" ? EmberRed : GreyLight;
                        float s = Mathf.Min(6, o.Length * .1f), side = o.Half[o.Nearest(new Vector2(o.At(s).x, o.At(s).z), out _)] + .2f;
                        foreach (int k in new[] { -1, 1 })
                        {
                            var foot = t.InverseTransformPoint(o.At(s, k * side)); foot.y = FloorY(foot.x, foot.z);
                            Part(PrimitiveType.Cylinder, t, foot + Vector3.up * .9f, new Vector3(.1f, .9f, .1f), iron);
                            Lamp(foot + Vector3.up * 1.95f, col, 9, 1.1f, .16f);
                        }
                    }
                }
                // The cage-lift gate at the head of the stair (dungeon step D3): shut until the three rail sigils are set in the frame on
                // its left post, three sockets that light amber, red and grey when they are (EncounterSession.Adit).
                {
                    float s = galleryTo + 4; var gate = Gate(s, AditLiftGate, "liftgate", "Set the rail sigils", null);
                    var q = Along(s); float w = h.Half[RingAt(s)] * .9f; var plate = On(s - .3f, -w + .55f) + Vector3.up * 1.45f;
                    Part(PrimitiveType.Cube, t, plate, new Vector3(.8f, .5f, .08f), iron, q);
                    var lit = new System.Collections.Generic.List<GameObject>();
                    var socket = Tint(art.stone, new Color(.08f, .08f, .09f));
                    for (int k = 0; k < 3; k++)
                    {
                        var at = plate + q * new Vector3(-.24f + k * .24f, 0, -.06f);
                        Part(PrimitiveType.Sphere, t, at, Vector3.one * .13f, socket);
                        var sigil = Part(PrimitiveType.Sphere, t, at + q * new Vector3(0, 0, -.02f), Vector3.one * .14f, Glowing(k == 0 ? new Color(1, .72f, .25f) : k == 1 ? EmberRed : GreyLight, 2.4f));
                        sigil.SetActive(false); lit.Add(sigil);
                    }
                    gate.Lights = lit.ToArray();
                }
                // The Gallery's spirit stone (D3): a standing stone against the right wall just inside the Gallery, pale with its song,
                // clear of the camps and of the three ways on (the Geode Floor opens further along this wall). Touched once, it is where
                // you wake in the Adit. Fixed place, no draws from the stream.
                if (gallery >= 0)
                {
                    float s = galleryFrom + 4; int i = RingAt(s); var q = Along(s); var foot = On(s, h.Half[i] * .74f); foot.y = FloorY(foot.x, foot.z);
                    var marker = new GameObject(AditSpiritStone).transform; marker.SetParent(t, false); marker.localPosition = foot; marker.localRotation = q * Quaternion.Euler(0, 15, 0);
                    Part(PrimitiveType.Cube, marker, new Vector3(0, 1.1f, 0), new Vector3(.9f, 2.2f, .55f), Tint(art.stone, new Color(.58f, .6f, .66f)), Quaternion.Euler(0, 0, 4));
                    Part(PrimitiveType.Cube, marker, new Vector3(0, .12f, 0), new Vector3(1.4f, .24f, 1), Tint(art.stone, new Color(.4f, .41f, .44f)));
                    Part(PrimitiveType.Sphere, marker, new Vector3(0, 1.45f, .29f), new Vector3(.22f, .3f, .04f), Glowing(new Color(.7f, .85f, 1), 2));   // the rune
                    Glow(marker, new Vector3(0, 1.6f, .8f), 9, .9f, new Color(.7f, .85f, 1), .9f);
                    Block(foot, new Vector3(1, 2.2f, .7f), q);
                    var stand = On(s, h.Half[i] * .74f - 1.3f);
                    Interactables.Add(new ZoneInteractable { name = AditSpiritStone, prompt = "Touch the spirit stone", kind = "stone", position = t.TransformPoint(stand), root = marker });
                }
                // The stair: plank treads where it runs steep, a rope rail on posts, a cold lamp every nine metres.
                float lastPost = -9; int stairLamp = 0;
                for (float s = galleryTo + 5; s < hallFrom; s += .55f)
                {
                    int i = RingAt(s), j = Mathf.Min(n - 1, i + 2); float run = h.Along[j] - h.Along[i];
                    if (run <= 0 || (c[i].y - c[j].y) / run < .25f) continue;
                    var q = Along(s); float w = h.Half[i] * .62f;
                    Part(PrimitiveType.Cube, t, On(s, 0) + Vector3.up * .04f, new Vector3(w * 2, .07f, .3f), timber, q);
                    if (s - lastPost < 2.4f) continue;
                    var post = On(s, -h.Half[i] * .72f);
                    Part(PrimitiveType.Cylinder, t, post + Vector3.up * .55f, new Vector3(.07f, .55f, .07f), timber);
                    if (lastPost > 0) { var prev = On(lastPost, -h.Half[RingAt(lastPost)] * .72f) + Vector3.up * 1.05f; var tp = post + Vector3.up * 1.05f; var d = tp - prev;
                        Part(PrimitiveType.Cylinder, t, (prev + tp) / 2, new Vector3(.035f, d.magnitude / 2, .035f), straw, Quaternion.FromToRotation(Vector3.up, d)); }
                    lastPost = s;
                    if (stairLamp++ % 4 == 0) WallLamp(i, 1, RailBlue, 9, .8f);
                }
                // The gate onto the platform at the stair's foot (D3), where the way is narrowest before the hall opens, barred from the
                // hall's side: a goblin freed in "The Pressed" hums its lock open, or powder from the keg up the stair blows it (and the
                // Quartermaster's people come for you).
                if (hall >= 0)
                {
                    float foot = galleryTo + 5, narrow = 9;
                    for (int i = 0; i < n; i++) if (h.Along[i] > hallFrom - 22 && h.Along[i] < hallFrom - 6 && h.Half[i] < narrow) { narrow = h.Half[i]; foot = h.Along[i]; }
                    Gate(foot, AditPlatformGate, "platformgate", "Open the gate", AditQuartermaster);
                    float sk = foot - 7; int ik = RingAt(sk); var q = Along(sk); var keg = On(sk, h.Half[ik] * .5f); keg.y = FloorY(keg.x, keg.z);
                    var kegs = new GameObject(AditPowder).transform; kegs.SetParent(t, false); kegs.localPosition = keg; kegs.localRotation = q;
                    for (int k = 0; k < 2; k++)
                    {
                        var at = new Vector3(0, .33f, k * .6f - .3f);
                        Part(PrimitiveType.Cylinder, kegs, at, new Vector3(.5f, .33f, .5f), dark);
                        foreach (float y in new[] { -.2f, .2f }) Part(PrimitiveType.Cylinder, kegs, at + Vector3.up * y, new Vector3(.53f, .03f, .53f), iron);
                    }
                    Part(PrimitiveType.Cube, kegs, new Vector3(0, .7f, 0), new Vector3(.35f, .04f, .25f), Tint(art.cloth, new Color(.6f, .15f, .1f)));   // a red-marked lid: powder
                    Interactables.Add(new ZoneInteractable { name = AditPowder, prompt = "Take blasting powder", kind = "powder", position = t.TransformPoint(keg), root = kegs });
                    RailHall(hallFrom);
                }
            }
            // A gate across the way at s: a heavy frame, an iron grille shut across it (ZoneGate: it stops you and every agent until it
            // opens, then climbs into the frame), cold lamps on the near side, and the thing to use at its foot on the near side.
            ZoneGate Gate(float s, string title, string kind, string prompt, string lord)
            {
                var q = Along(s); int i = RingAt(s); float w = h.Half[i] * .9f, top = h.Height[i] * .82f, high = top - .25f;
                foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, On(s, side * w) + Vector3.up * top / 2, new Vector3(.4f, top, .4f), dark, q);
                Part(PrimitiveType.Cube, t, On(s, 0) + Vector3.up * top, new Vector3(w * 2 + .8f, .45f, .45f), dark, q);
                foreach (int side in new[] { -1, 1 }) Lamp(On(s - .6f, side * (w - .3f)) + Vector3.up * 2.1f, RailBlue, 8, .9f);
                var root = new GameObject(title).transform; root.SetParent(t, false); root.localPosition = On(s, 0); root.localRotation = q;
                var grille = new GameObject("Grille").transform; grille.SetParent(root, false);
                for (int b = -7; b <= 7; b++) Part(PrimitiveType.Cylinder, grille, new Vector3(b * w / 7.5f, high / 2, 0), new Vector3(.07f, high / 2, .07f), iron);
                foreach (float y in new[] { .35f, high * .5f, high - .3f }) Part(PrimitiveType.Cube, grille, new Vector3(0, y, 0), new Vector3(w * 2, .1f, .08f), iron);
                var gate = root.gameObject.AddComponent<ZoneGate>(); gate.Title = title; gate.Cave = h.Name; gate.Along = s; gate.Lord = lord; gate.Grille = grille; gate.Rise = high - .4f;
                gate.Init(new Vector3(h.Half[i] * 2 + 1, top, .5f));
                Gates.Add(gate);
                Interactables.Add(new ZoneInteractable { name = title, prompt = prompt, kind = kind, position = t.TransformPoint(On(s - 1.3f, 0)), gate = gate });
                return gate;
            }

            void RailHall(float from)
            {
                float to = h.Length - 3, line = -h.Half[Widest(from, to)] * .55f;
                // The line: down the hall's left side, buffers at the far end, and a dark mouth in the end wall where it runs on south.
                Rails(from + 1, to - 1, line, .7f);
                var qEnd = Along(to - 2); var buffer = On(to - 2.2f, line);
                Part(PrimitiveType.Cube, t, buffer + Vector3.up * .5f, new Vector3(2.2f, 1, .5f), dark, qEnd); Block(buffer, new Vector3(2.2f, 1, .5f), qEnd);
                foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, t, buffer + qEnd * new Vector3(side * .7f, .6f, -.35f), new Vector3(.3f, .2f, .3f), iron, qEnd * Quaternion.Euler(90, 0, 0));
                Part(PrimitiveType.Sphere, t, On(h.Length - .6f, line) + Vector3.up * 2.4f, new Vector3(5.5f, 5, .6f), Tint(art.stone, new Color(.02f, .02f, .025f)), qEnd);
                // The platform: flags along the middle of the hall, a kerb on the line's side.
                for (float s = from + 4; s < to - 4; s += 3)
                {
                    var q = Along(s); var mid = On(s, line * -.35f);
                    Part(PrimitiveType.Cube, t, mid + Vector3.up * .025f, new Vector3(-line * 1.4f, .05f, 3), Tint(art.stone, new Color(.36f, .35f, .34f)), q);
                    Part(PrimitiveType.Cube, t, On(s, line * .6f) + Vector3.up * .1f, new Vector3(.3f, .2f, 3), Tint(art.stone, new Color(.3f, .29f, .28f)), q);
                }
                // Lamps on iron posts down both sides, cold light, and high lights over the whole hall.
                int k2 = 0;
                for (float s = from + 4; s < to - 2; s += 10, k2++)
                    foreach (int side in new[] { -1, 1 })
                    {
                        int i = RingAt(s); var foot = On(s, side * (h.Half[i] - 2)); if (!Free(foot)) continue;
                        Part(PrimitiveType.Cylinder, t, foot + Vector3.up * 1.6f, new Vector3(.12f, 1.6f, .12f), iron);
                        Lamp(foot + Vector3.up * 3.3f, RailBlue, 12, 1.1f, .2f);
                        Block(foot, new Vector3(.35f, 3.2f, .35f), Quaternion.identity);
                    }
                for (float s = from + 8; s < to; s += 16) { int i = RingAt(s); Glow(t, c[i] + Vector3.up * h.Height[i] * .7f, 30, .55f, RailBlue, .55f); }
                // The carriage: an armoured body on eight wheels, plates riveted down its side, the rear open on stacked crates of
                // glowing geodes, a gangway up from the platform.
                float sc = to - 16; var qc = Along(sc); var car = On(sc, line);
                var plate = Tint(art.metal, new Color(.26f, .27f, .29f));
                Part(PrimitiveType.Cube, t, car + qc * new Vector3(0, 1.75f, 2.2f), new Vector3(2.8f, 2.4f, 6), plate, qc);   // the armoured cab
                Part(PrimitiveType.Cube, t, car + qc * new Vector3(0, 3.05f, 2.2f), new Vector3(3, .2f, 6.2f), dark, qc);
                Part(PrimitiveType.Cube, t, car + qc * new Vector3(0, .7f, -1), new Vector3(2.8f, .3f, 12.4f), iron, qc);   // the bed
                for (int k = 0; k < 4; k++) foreach (int side in new[] { -1, 1 })
                        Part(PrimitiveType.Cylinder, t, car + qc * new Vector3(side * 1.05f, .45f, -5.6f + k * 3.7f), new Vector3(.8f, .06f, .8f), rust, qc * Quaternion.Euler(0, 0, 90));
                for (int k = 0; k < 6; k++) foreach (int side in new[] { -1, 1 })
                        Part(PrimitiveType.Sphere, t, car + qc * new Vector3(side * 1.42f, 1.2f + (k % 2) * .8f, -.4f + k * .9f), Vector3.one * .1f, rust, qc);   // rivets
                for (int k = 0; k < 6; k++) Crate(car + qc * new Vector3((k % 2 - .5f) * 1.1f, .85f, -2.4f - (k / 2) * 1.1f), qc, 1 + (k + 1) % 2, true);
                Glow(t, car + qc * new Vector3(0, 2.2f, -3.4f), 8, .9f, GeodeViolet, .9f);
                Block(car + qc * new Vector3(0, 0, 2.2f), new Vector3(2.9f, 3.2f, 6.2f), qc);
                Block(car + qc * new Vector3(0, 0, -3.8f), new Vector3(2.9f, 1, 4.6f), qc);
                // The loading crane by the carriage, crates and barrels on the platform, steam hanging over the line.
                var crane = On(sc - 7, line * .15f);
                if (Free(crane))
                {
                    Part(PrimitiveType.Cube, t, crane + Vector3.up * 2.4f, new Vector3(.4f, 4.8f, .4f), dark, qc);
                    Part(PrimitiveType.Cube, t, crane + qc * new Vector3(line * .4f, 4.6f, 0), new Vector3(Mathf.Abs(line) * .9f, .3f, .3f), dark, qc);
                    Part(PrimitiveType.Cylinder, t, crane + qc * new Vector3(line * .8f, 3.4f, 0), new Vector3(.03f, 1.2f, .03f), straw, qc);
                    Block(crane, new Vector3(.6f, 4.8f, .6f), qc);
                }
                for (int k = 0; k < 8; k++)
                {
                    float s = from + 8 + k * 5.5f; if (s > sc - 9) break;
                    Crate(On(s, -line * (.5f + A() * .4f)), Along(s), 1 + (int)(A() * 2), A() < .4f);
                }
                for (int k = 0; k < 5; k++) { A(); A(); A(); }   // (the steam is gone: it read as white blobs; its draws are kept)
            }

            void GeodeFloor()
            {
                int cut = Widest(18, 40), shop = Widest(48, h.Length - 3);
                int no = 0;
                for (float s = 6; s < h.Length - 3; s += 8, no++) WallLamp(RingAt(s), no % 2 == 0 ? 1 : -1, GeodeViolet, 9, .85f);
                for (float s = 3; s < h.Length - 3; s += 2.6f) if (A() < .7f) Crystals(RingAt(s), A() < .5f ? 1 : -1, 3 + (int)(A() * 4));
                if (cut >= 0)
                {
                    float s0 = h.Along[cut]; var q = Along(s0);
                    for (int k = 0; k < 3; k++)
                    {
                        var bench = On(s0 - 4 + k * 4, (k % 2 == 0 ? 1 : -1) * h.Half[cut] * .5f); if (!Free(bench)) continue;
                        Part(PrimitiveType.Cube, t, bench + Vector3.up * .45f, new Vector3(1.6f, .9f, .9f), timber, q);
                        Part(PrimitiveType.Cylinder, t, bench + q * new Vector3(.4f, 1.2f, 0), new Vector3(.6f, .06f, .6f), stone, q * Quaternion.Euler(0, 0, 90));   // the cutting wheel
                        for (int g = 0; g < 3; g++) Part(PrimitiveType.Sphere, t, bench + q * new Vector3(-.4f + g * .2f, .97f, (A() - .5f) * .4f), new Vector3(.18f, .12f, .16f), geode);
                        Block(bench, new Vector3(1.7f, 1, 1), q);
                    }
                    var trough = On(s0 + 4, -h.Half[cut] * .55f);
                    if (Free(trough))
                    {
                        Part(PrimitiveType.Cube, t, trough + Vector3.up * .35f, new Vector3(1, .7f, 3.4f), dark, q);
                        for (int g = 0; g < 9; g++) Part(PrimitiveType.Sphere, t, trough + q * new Vector3((A() - .5f) * .7f, .66f, (A() - .5f) * 3), new Vector3(.18f, .1f, .16f), geode);
                        Block(trough, new Vector3(1.1f, .8f, 3.5f), q);
                    }
                    Glow(t, On(s0, 0) + Vector3.up * 3.5f, 16, .9f, GeodeViolet, .9f);
                }
                if (shop >= 0)
                {
                    // The Rock-Eater: a goblin boring-engine (GAME-ONLY) at rest against the far wall, its drill toward the rock.
                    float s0 = h.Along[shop] + 3; var q = Along(s0); var at = On(s0, h.Half[shop] * .35f);
                    if (Free(at))
                    {
                        Part(PrimitiveType.Cube, t, at + q * new Vector3(0, 1.1f, 0), new Vector3(2.2f, 1.6f, 2.8f), rust, q);
                        Part(PrimitiveType.Cube, t, at + q * new Vector3(0, 2.1f, -.4f), new Vector3(1.2f, .6f, 1.2f), iron, q);   // the pilot's seat
                        if (cone == null) cone = ZoneMeshes.Cone(1, 1);
                        MeshPart(cone, t, at + q * new Vector3(0, 1.1f, 1.4f), iron, q * Quaternion.Euler(90, 0, 0)).transform.localScale = new Vector3(.9f, 1.6f, .9f);   // the drill
                        foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, at + q * new Vector3(side * 1.2f, .4f, 0), new Vector3(.4f, .8f, 3), dark, q);   // tracks
                        Part(PrimitiveType.Cylinder, t, at + q * new Vector3(.6f, 2.4f, -1), new Vector3(.25f, .6f, .25f), iron, q);   // its stack
                        Block(at, new Vector3(2.8f, 2.4f, 4.4f), q);
                    }
                    for (int k = 0; k < 4; k++) Crate(On(s0 - 7 + k * 1.3f, -h.Half[shop] * .6f), q, 1 + k % 2, k % 2 == 0);
                    for (int k = 0; k < 6; k++) Lump(BoulderAt(k), t, On(s0 - 3 + A() * 6, -h.Half[shop] * (.2f + A() * .3f)) + Vector3.up * .1f, Vector3.one * (.25f + A() * .2f), rust, A() * 360);   // scrap
                    Glow(t, On(s0, 0) + Vector3.up * 4, 18, .9f, new Color(.9f, .6f, 1), .9f);
                }
            }

            void EmberVent()
            {
                int lake = Widest(26, h.Length - 8);
                var ember = Glowing(EmberRed, 1.8f); var basalt = Tint(art.stone, new Color(.18f, .15f, .14f));
                int no = 0;
                for (float s = 5; s < h.Length - 3; s += 7, no++) WallLamp(RingAt(s), no % 2 == 0 ? 1 : -1, EmberRed, 9, .9f);
                // Embers in the walls: glowing cracks, thicker toward the hall.
                for (float s = 3; s < h.Length - 3; s += 2.2f)
                {
                    int i = RingAt(s); if (A() < .4f) continue; int side = A() < .5f ? 1 : -1; int k = side > 0 ? 1 + (int)(A() * 5) : P - 2 - (int)(A() * 5);
                    var at = ring[i, k]; var inward = c[i] + Vector3.up * (at.y - c[i].y) - at; inward.y = 0; inward.Normalize(); if (!Free(at + inward * .3f)) continue;
                    for (int q = 0; q < 3; q++) Part(PrimitiveType.Cube, t, at + inward * .04f + new Vector3((A() - .5f) * .8f, (A() - .5f) * .9f, (A() - .5f) * .8f), new Vector3(.06f, .4f + A() * .6f, .06f), ember, Quaternion.LookRotation(inward) * Quaternion.Euler(0, 0, A() * 180));
                }
                if (lake >= 0)
                {
                    // The lake of molten rock in the middle of the great hall, a rim of black rock round it, the way round on both sides.
                    float s0 = h.Along[lake]; var q = Along(s0); var mid = On(s0, 0);
                    float rx = h.Half[lake] * .42f, rz = Mathf.Min(13, h.Half[lake] * .9f);
                    Part(PrimitiveType.Cylinder, t, mid + Vector3.up * .03f, new Vector3(rx * 2, .02f, rz * 2), Glowing(new Color(1, .4f, .06f), 2.6f), q);
                    Part(PrimitiveType.Cylinder, t, mid + Vector3.up * .05f, new Vector3(rx * 1.3f, .02f, rz * 1.3f), Glowing(new Color(1, .75f, .25f), 2.2f), q * Quaternion.Euler(0, 12, 0));   // the brighter heart of it
                    for (int k = 0; k < 40; k++)
                    {
                        float a = k / 40f * Mathf.PI * 2; var rim = mid + q * new Vector3(Mathf.Cos(a) * rx * 1.04f, .05f, Mathf.Sin(a) * rz * 1.04f);
                        Lump(BoulderAt(k), t, rim, new Vector3(.7f + A() * .6f, .35f + A() * .3f, .6f + A() * .5f), basalt, A() * 360);
                    }
                    Block(mid, new Vector3(rx * 1.9f, 1.2f, rz * 1.1f), q); Block(mid, new Vector3(rx * 1.1f, 1.2f, rz * 1.9f), q);   // nobody walks the lake (the knockback into it is a later step)
                    for (int k = -1; k <= 1; k++) Glow(t, mid + q * new Vector3(0, 2.2f, k * rz * .6f), 18, 1.6f, new Color(1, .45f, .12f), 1.6f).gameObject.AddComponent<Flicker>();
                    // The cell's things: braziers round the hall, red banners, the altar at the far end where the Cinder-Warden keeps.
                    var cloth = Tint(art.cloth, new Color(.45f, .08f, .06f));
                    for (int k = 0; k < 4; k++)
                    {
                        float s = s0 - rz + 2 + k * rz * .6f; int side = k % 2 == 0 ? 1 : -1; var b = On(s, side * (h.Half[RingAt(s)] - 1.6f));
                        if (!Free(b)) continue; Brazier(t, b, null); Block(b, new Vector3(.8f, 1.2f, .8f), Quaternion.identity);
                        Banner(t, On(s + 1.2f, side * (h.Half[RingAt(s)] - 1)), Along(s).eulerAngles.y, cloth, dark, 3.2f);
                    }
                    float sa = h.Length - 8; var qa = Along(sa); var altar = On(sa, 0);
                    if (Free(altar))
                    {
                        Part(PrimitiveType.Cube, t, altar + Vector3.up * .5f, new Vector3(2.4f, 1, 1.2f), basalt, qa);
                        Part(PrimitiveType.Cube, t, altar + Vector3.up * 1.03f, new Vector3(2.6f, .08f, 1.4f), Tint(art.cloth, new Color(.3f, .06f, .05f)), qa);
                        for (int g = 0; g < 3; g++) Part(PrimitiveType.Sphere, t, altar + qa * new Vector3(-.6f + g * .6f, 1.15f, 0), new Vector3(.2f, .14f, .18f), Glowing(new Color(.55f, .3f, .8f), .6f));   // geodes set out for the fire, gone dim
                        Lamp(altar + qa * new Vector3(0, 1.4f, -.5f), EmberRed, 10, 1.2f, .1f);
                        Block(altar, new Vector3(2.5f, 1.1f, 1.3f), qa);
                    }
                }
            }

            void GreyBreach()
            {
                int foreman = Widest(46, h.Length - 3);
                var unmade = Tint(art.stone, new Color(.34f, .35f, .37f)); var hole = Tint(art.stone, new Color(.015f, .015f, .02f));   // the grey: dull rock gone colourless, not white cards (2026-10-08)
                // Little light: a pale glow every twelve metres, and the old miners' things left where they dropped them.
                int no = 0;
                for (float s = 6; s < h.Length - 3; s += 12, no++) WallLamp(RingAt(s), no % 2 == 0 ? 1 : -1, GreyLight, 8, .45f);
                Cart(Mathf.Min(16, h.Length * .25f), .8f, true);
                for (int k = 0; k < 2; k++)
                {
                    float s = 12 + k * 22; if (s > h.Length - 6) break; var q = Along(s); var l = On(s, -h.Half[RingAt(s)] * .7f);
                    if (Free(l)) Part(PrimitiveType.Cube, t, l + Vector3.up * .25f, new Vector3(.24f, .24f, 2.6f), timber, q * Quaternion.Euler(0, 30, 80));   // a fallen post
                }
                // The grey: patches of static where the rock has been unmade, more of them the deeper you go.
                for (float s = 10; s < h.Length - 2; s += 1.8f)
                {
                    float deep = Mathf.InverseLerp(10, h.Length, s); if (A() > .25f + deep * .6f) continue;
                    int i = RingAt(s), k = 1 + (int)(A() * (P - 2)); var at = ring[i, k]; var inward = c[i] + Vector3.up * h.Height[i] * .4f - at; inward.Normalize();
                    if (!Free(at + inward * .3f)) continue;
                    Lump(CragRock((int)(A() * 6)), t, at - inward * .1f, new Vector3(.6f + A() * .9f, .5f + A() * .8f, .4f), unmade, A() * 360);   // a patch of the rock gone grey
                }
                if (foreman >= 0)
                {
                    // A stretch of floor that is not there: a black hole at the side of the Foreman's drift, ringed with grey.
                    float s0 = h.Along[foreman]; var q = Along(s0); var at = On(s0 - 2, -h.Half[foreman] * .5f);
                    if (Free(at))
                    {
                        Part(PrimitiveType.Cylinder, t, at + Vector3.up * .02f, new Vector3(3.2f, .01f, 2.4f), hole, q);
                        for (int k = 0; k < 12; k++) { float a = k / 12f * Mathf.PI * 2; Lump(BoulderAt(k), t, at + q * new Vector3(Mathf.Cos(a) * 1.75f, .05f, Mathf.Sin(a) * 1.35f), new Vector3(.5f, .25f, .4f), unmade, k * 30); }   // a broken rim
                        Block(at, new Vector3(2.6f, 1.2f, 2), q);
                    }
                    Glow(t, On(s0, 0) + Vector3.up * 3, 14, .5f, GreyLight, .5f);
                }
            }
        }
    }
}
