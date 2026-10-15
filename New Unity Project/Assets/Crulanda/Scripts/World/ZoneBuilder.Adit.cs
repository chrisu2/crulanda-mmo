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
        public const string AditLiftGate = "The singing door", AditPlatformGate = "The platform gate", AditPowder = "A keg of blasting powder",
            AditSpiritStone = "The Gallery's spirit stone", AditQuartermaster = "Quartermaster Brannigan Sorrel", AditCarriage = "The ore carriage",
            AditRailStone = "The Rail Hall's spirit stone";
        /// <summary>The Rail Hall's part in the Weaver's unlocking (dungeon step D4, EncounterSession.Weaver): where Mother Quillet stands to match
        /// the carriage's resonance lock, where the two waves come from (behind the carriage, then the far end where the line runs on into the
        /// dark), and the carriage's geodes with their light and the lock's eye, which go dark when it is matched. Unset outside the Adit.</summary>
        public Vector3 RailLock; public Vector3[] RailWaves; public GameObject[] RailGeodes;
        /// <summary>Where the carriage's two pit mules stand in their traces, up the line from it, and the way they face (the session puts
        /// them there: EncounterSession.Adit). Unset outside the Adit.</summary>
        public Vector3[] RailTeam; public Quaternion RailTeamFacing;
        /// <summary>The lock matched (D4): the carriage's geodes go dull and their light goes out.</summary>
        public void QuietCarriage()
        {
            if (RailGeodes == null) return;
            var dull = Tint(art.stone, new Color(.22f, .2f, .25f));
            foreach (var g in RailGeodes)
            {
                if (g == null) continue;
                var r = g.GetComponent<Renderer>(); if (r != null) r.sharedMaterial = dull;
                var l = g.GetComponent<Light>(); if (l == null) continue;
                foreach (var night in NightLights) if (night.light == l) night.dayIntensity = night.nightIntensity = 0;   // the clock lights it by these
                l.intensity = 0; l.enabled = false;
            }
        }
        static readonly Color LumGreen = new Color(.45f, 1, .55f), GeodeViolet = new Color(.7f, .42f, 1), EmberRed = new Color(1, .45f, .15f),
            GreyLight = new Color(.74f, .77f, .82f), RailBlue = new Color(.55f, .75f, 1), GalleryLight = new Color(.62f, .72f, .88f);
        /// <summary>A cave's lining colour by its part (variant 2, <see cref="AditPart"/>): the workings' grey-brown, a violet cast, the vent's
        /// scorched rock, the grey.</summary>
        static Color AditLining(string part)
        {
            switch (part)
            {
                case "geode": case "foundry": return new Color(.5f, .46f, .52f);
                case "vent": case "ashgarden": case "scriptorium": case "musteryard": case "chapel": return new Color(.44f, .34f, .28f);
                case "breach": return new Color(.55f, .55f, .56f);
                case "river": return new Color(.47f, .49f, .48f);
                default: return new Color(.54f, .49f, .43f);
            }
        }
        /// <summary>
        /// Which part of the Adit a cave is dressed as: its style (ZoneProp.style, the Adit rework) or, with none, its name as the first
        /// Adit's four caves were named: "main" (the Workings, the Gallery, the stair and the Rail Hall), "geode", "vent", "breach", or
        /// "plain" for any other (lamps, rubble and the camps' things only, until it has dressing of its own: the main way's gates and
        /// Rail Hall are never built in a cave that is not it).
        /// The Adit rework (Docs/ADIT_REWORK.md) splits the main way into caves of their own: "workings" (the mouth, the drifts' timber, rails
        /// and lum-tubes, the cages, the gang-boss's hall), "gallery" (the Singing Gallery: its light, dripstone, echo-jars, the lamps at its
        /// ways on and its spirit stone) and "deep" (the singing door, the shaft stair, the platform gate, the powder and the Rail Hall); the
        /// foundry hall ("foundry"); the chapter-house a room to a cave ("ashgarden", "scriptorium", "musteryard", "chapel"); and the river
        /// caves ("river": lamps of their own colour).
        /// </summary>
        static string AditPart(Hollow h)
        {
            switch (string.IsNullOrEmpty(h.Style) ? h.Name : h.Style)
            {
                case "The Geode Floor": case "geode": return "geode";
                case "foundry": return "foundry";
                case "The Ember Vent": case "vent": return "vent";
                case "ashgarden": return "ashgarden";
                case "scriptorium": return "scriptorium";
                case "musteryard": return "musteryard";
                case "chapel": return "chapel";
                case "The Grey Breach": case "breach": return "breach";
                case "river": return "river";
                case "The Sealed Adit": case "main": return "main";
                case "workings": return "workings";
                case "gallery": return "gallery";
                case "deep": return "deep";
                default: return "plain";
            }
        }
        /// <summary>The colour of the lamps at a way on out of the Gallery, by the part of the Adit it leads to.</summary>
        static Color WayLight(string part)
        {
            switch (part)
            {
                case "geode": case "foundry": return GeodeViolet;
                case "vent": case "ashgarden": case "scriptorium": case "musteryard": case "chapel": return EmberRed;
                case "deep": return RailBlue;
                default: return GreyLight;
            }
        }
        /// <summary>The river caves' lamps: a cold blue-green over the water (the Adit rework).</summary>
        static readonly Color RiverLight = new Color(.45f, .85f, .78f);
        void AditInterior(Transform t, Hollow h, Vector3[] c, Vector3[] right, Vector3[,] ring, int n, int P, Func<float, float, float> FloorY, Func<float, int> RingAt, Func<float, float, Vector3> On, Func<float, Quaternion> Along, Action<Vector3, Vector3, Quaternion> Block)
        {
            var ar = TreeRandom(t.position + new Vector3(3.3f, 0, 1.7f)); float A() { return (float)ar.NextDouble(); }
            // The full-length Adit (2026-10-10, Chris: "the whole place needs to be bigger", "3x, full-length run"): the layout is drawn
            // out (tools/wip/dungeon/adit_layout.py: LEN 3, chambers 2.5 times as wide and twice as tall, drifts 1.5 times as wide). The
            // places below are written in the first Adit's metres and drawn out by these: X along the way, CW across a chamber, DW across
            // a drift, CH up a chamber. Lamps and glows stand further apart than three times as many would.
            const float X = 3, CW = 2.5f, DW = 1.5f, CH = 2;
            // Nothing in another cave's air, nor (the Adit rework) in a deck's way (on it, under a low one, or up against it) or in the water.
            bool Free(Vector3 local) { var w = t.TransformPoint(local); return !Hollow.OpenInOther(h, w + Vector3.up * .5f, .98f) && !InDeckWay(w, .6f) && !InCaveWater(h, w, .6f); }
            int Widest(float from, float to) { int best = -1; for (int i = 0; i < n; i++) if (h.Along[i] >= from && h.Along[i] <= to && (best < 0 || h.Half[i] > h.Half[best])) best = i; return best; }
            // A chamber by its anchor in this cave (ZoneAnchor, the Adit rework: the layout names it), else the widest ring in the stretch.
            int Named(string anchor, float from, float to) { var a = AnchorIn(h, anchor); return a != null ? RingAt(a.along) : Widest(from, to); }
            string part = AditPart(h);
            var timber = Tint(art.timber, new Color(.34f, .25f, .16f)); var dark = Tint(art.timber, new Color(.2f, .14f, .09f));
            var iron = Tint(art.metal, new Color(.22f, .21f, .2f)); var rust = Tint(art.metal, new Color(.36f, .23f, .15f));
            var stone = Tint(art.stone, new Color(.4f, .38f, .35f)); var straw = Tint(art.hay, new Color(.62f, .54f, .37f));
            var geode = Glowing(GeodeViolet, 1.5f);
            // ---------- the dungeon pieces (Chris's download of 2026-10-10, Resources/Props/Dungeon) ----------
            // He chose to dress the whole Adit with them and to build its halls from them (the Adit had no masonry: every space is a
            // lofted rock tube, so the halls are walled where a mine would have built: the Rail Hall, the gates, the mouth). All of it under
            // one root, static-batched at the end; the pieces are for the eye, with a Block where one would be walked into. Its own stream
            // (kr), so nothing placed before moves.
            Transform kit = null; var kr = TreeRandom(t.position + new Vector3(7.1f, 0, 2.9f)); float K() { return (float)kr.NextDouble(); }
            // The kit's pale flags in the cave's own tone (2026-10-11, Chris's screenshots: a near-white tiled floor in the dark cave).
            var shaded = new System.Collections.Generic.Dictionary<Material, Material>();
            void Shade(GameObject piece, float k)
            {
                foreach (var r in piece.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int m = 0; m < mats.Length; m++)
                    {
                        if (mats[m] == null) continue;
                        if (!shaded.TryGetValue(mats[m], out var dim)) { dim = new Material(mats[m]) { name = mats[m].name + " (cave)" }; if (dim.HasProperty("_Color")) dim.color = mats[m].color * new Color(k, k * .97f, k * .93f, 1); shaded[mats[m]] = dim; }
                        mats[m] = dim;
                    }
                    r.sharedMaterials = mats;
                }
            }
            const float FloorShade = .4f;   // the kit's floor flags at two fifths of their pale grey (half still read pale in the 14:59 captures)
            GameObject Piece(string name, Vector3 foot, float yaw, float height, float? width = null, Vector3? box = null)
            {
                if (kit == null) { kit = new GameObject("Adit kit").transform; kit.SetParent(t, false); }
                var piece = ModelProp(kit, "Dungeon/" + name, foot, yaw, height, false, width, box);
                if (piece != null && name == "ModularFloor") Shade(piece, FloorShade);
                return piece;
            }
            float YawAt(float s) { return Along(s).eulerAngles.y; }
            var torchFire = new Color(1, .62f, .3f);
            // A torch in its iron bracket on a wall face (foot: the wall's face at the floor; out: the way into the room), 1.5 m up, its
            // fire lit (with a light when `lit`).
            void Torch(Vector3 wall, Vector3 out_, bool lit)
            {
                out_.y = 0; if (out_.sqrMagnitude < .01f) return; out_.Normalize();
                var foot = wall + out_ * .3f + Vector3.up * 1.5f; if (!Free(foot)) return;
                float yaw = Quaternion.LookRotation(out_).eulerAngles.y - 90;   // the bracket's +X out from the wall
                if (Piece("Torch_wall", foot, yaw, 1.0f) == null) return;
                if (lit) Glow(t, foot + out_ * .25f + Vector3.up * 1.05f, 9, .95f, torchFire, .95f).gameObject.AddComponent<Flicker>();
            }
            // A torch on the cave's wall at s on one side (the rock at about 2 m up, as WallLamp finds it).
            void WallTorch(float s, int side, bool lit)
            {
                int i = RingAt(s), best = side > 0 ? 1 : P - 2;
                for (int k = 1; k < P - 1; k++)
                    if ((side > 0 ? k <= P / 2 : k >= P / 2) && Mathf.Abs(ring[i, k].y - c[i].y - 2f) < Mathf.Abs(ring[i, best].y - c[i].y - 2f)) best = k;
                var wall = ring[i, best]; var out_ = c[i] - wall; wall.y = c[i].y;
                Torch(wall, out_, lit);
            }
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
            void Crate(Vector3 at, Quaternion q, int high, bool geodes, System.Collections.Generic.List<GameObject> into = null)
            {
                if (!Free(at)) return;
                for (int k = 0; k < high; k++) Part(PrimitiveType.Cube, t, at + Vector3.up * (.4f + k * .8f), Vector3.one * .8f, timber, q * Quaternion.Euler(0, k * 11, 0));
                if (geodes) for (int k = 0; k < 4; k++) { var g = Part(PrimitiveType.Sphere, t, at + Vector3.up * (high * .8f + .02f) + q * new Vector3((A() - .5f) * .5f, 0, (A() - .5f) * .5f), new Vector3(.22f, .16f, .2f), geode); into?.Add(g); }
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

            // ---------- the room builder (D9, 2026-10-10; DUNGEON_DESIGN.md 0.2-0.3) ----------
            // Chris's brief: the first dungeon is a mix of the Deadmines, Wailing Caverns and Scarlet Monastery, and every space of the
            // Adit was a rock tube, so it read as one long cave. A hall lines a stretch of a cave in cut stone from his kit: a tiled
            // floor, walls of two courses a set distance either side of the way (where the rock stands clear of them; where it bulges
            // in, the rock shows), a column before the walls every few metres with a torch between, and end walls with an arched door
            // where the way goes on. Its furnishings take a place each (Room): none on another, on a camp's spawn circle or on the way
            // down the middle. Their own random stream, so nothing placed before them moves.
            var hallRand = TreeRandom(t.position + new Vector3(5.3f, 0, 8.7f)); float R9() { return (float)hallRand.NextDouble(); }
            var cutStone = Tint(art.stone, new Color(.44f, .42f, .39f));
            const float NaveW = 12 * CW;   // the chapter-house's nave: its walls this far either side of the way
            float LakeRz(int lake) { return Mathf.Min(13 * X, h.Half[lake] * .9f); }   // the lava lake's half-length down the hall
            var taken = new System.Collections.Generic.List<Vector3>();   // (along, aside, radius) of what stands on the floor
            var hallSpans = new System.Collections.Generic.List<Vector2>();   // the built halls' stretches (from, to), kept clear by FloorClutter
            bool Room(float s, float a, float r, bool middle = false)
            {
                if (!middle && Mathf.Abs(a) - r < 1.6f) return false;
                if (Zone.camps != null)
                    foreach (var cp in Zone.camps)
                        if (cp != null && cp.cave == h.Name && new Vector2(cp.along - s, cp.aside - a).magnitude < cp.radius + r + .8f) return false;
                foreach (var o in taken) if (new Vector2(o.x - s, o.y - a).magnitude < o.z + r + .3f) return false;
                if (!Free(On(s, a))) return false;
                taken.Add(new Vector3(s, a, r)); return true;
            }
            // The stretch of [from, to] where the rock stands at least w off the way (by the 0.82 of its half-width the Rail Hall keeps).
            Vector2 Span(float from, float to, float w)
            {
                float a = -1, b = -1;
                for (int i = 0; i < n; i++) if (h.Along[i] >= from && h.Along[i] <= to && h.Half[i] * .82f >= w) { if (a < 0) a = h.Along[i]; b = h.Along[i]; }
                return new Vector2(a, b);
            }
            int hallTorches = 0;
            // A wall across the way at s, out to w either side: 2 m modules out from the middle, an arched door 3.6 m wide when `door`.
            void EndWall(float s, float w, bool door, int courses = 2)
            {
                var q = Along(s); float from = door ? 2.3f : 0;
                foreach (int side in new[] { -1, 1 })
                    for (float a = from + 1; a < w + .6f; a += 2)
                    {
                        var foot = On(s, side * a); if (!Free(foot) || !Free(foot + Vector3.up * 3.5f)) continue;
                        // The kit's wall is dressed on one face (its back is flat): an end wall is seen from both sides, so two, back to back
                        // (Chris, 2026-10-11, "looks terrible": two flat black slabs round an arch, the walls' backs).
                        if (Piece("ModularStoneWall", foot, YawAt(s) + 90, 2.02f) == null) continue;
                        Piece("ModularStoneWall", foot, YawAt(s) - 90, 2.02f);
                        for (int k = 1; k < courses; k++) { Piece("ModularStoneWall", foot + Vector3.up * 2 * k, YawAt(s) + 90, 2.02f); Piece("ModularStoneWall", foot + Vector3.up * 2 * k, YawAt(s) - 90, 2.02f); }
                        Block(foot, new Vector3(2.05f, 2 * courses, .7f), q);
                    }
                if (!door) return;
                Piece("Entrance", On(s, 0), YawAt(s) + 90, 3.4f, null, new Vector3(.6f, 3.4f, 4.6f));
                foreach (int side in new[] { -1, 1 }) Block(On(s, side * 2.05f), new Vector3(.6f, 3.4f, .6f), q);
            }
            // A hall from s0 to s1, its walls w either side of the way.
            // (The Adit rework) columns false: no columns before the walls nor torches between them (the foundry's catwalks run along its walls).
            void Hall(float s0, float s1, float w, bool wallIn, bool doorIn, bool wallOut, bool doorOut, float columnsEvery = 6, Func<float, float, bool> bare = null, int courses = 2, bool columns = true)
            {
                if (s0 < 0 || s1 - s0 < 6) return;
                hallSpans.Add(new Vector2(s0 - 3, s1 + 3));
                for (float s = s0 + 1; s < s1 - .5f; s += 2)
                    for (float a = -w + 1; a < w - .5f; a += 2)
                    {
                        if (bare != null && bare(s, a)) continue;   // no flags under the lake
                        var at = On(s, a); if (Free(at)) Piece("ModularFloor", at + Vector3.down * .33f, YawAt(s), .36f);
                    }
                for (float s = s0 + 1; s < s1; s += 2)
                    foreach (int side in new[] { -1, 1 })
                    {
                        if (h.Half[RingAt(s)] * .82f < w - .2f) continue;   // the rock bulges in here: it shows
                        var foot = On(s, side * w); if (!Free(foot) || !Free(foot + Vector3.up * 3.5f)) continue;
                        float yaw = YawAt(s) + (side > 0 ? 0 : 180);   // the dressed face into the hall (it faced the rock: the hall showed flat backs)
                        if (Piece("ModularStoneWall", foot, yaw, 2.02f) == null) continue;
                        for (int k = 1; k < courses; k++) Piece("ModularStoneWall", foot + Vector3.up * 2 * k, yaw, 2.02f);
                        Block(foot, new Vector3(.7f, 2 * courses, 2.05f), Along(s));
                    }
                for (float s = s0 + 3; columns && s < s1 - 2; s += columnsEvery)
                    foreach (int side in new[] { -1, 1 })
                    {
                        if (h.Half[RingAt(s)] * .82f < w - .2f) continue;
                        float a = side * (w - 1.05f); if (!Room(s, a, .8f)) continue;
                        var foot = On(s, a); Piece("Column", foot, YawAt(s), 2.15f * courses); Block(foot, new Vector3(1.2f, 2.15f * courses, 1.2f), Along(s));
                        float st = s + columnsEvery / 2; if (st > s1 - 2) continue;
                        var face = On(st, side * (w - .36f)); Torch(face, On(st, 0) - face, hallTorches++ % 2 == 0);
                    }
                if (wallIn) EndWall(s0, w, doorIn, courses);
                if (wallOut) EndWall(s1, w, doorOut, courses);
                Glow(t, On((s0 + s1) / 2, 0) + Vector3.up * (2 * courses), Mathf.Max(s1 - s0, w * 2) * .9f, .8f, torchFire, .8f);
            }
            // ---- furnishings (cut stone, timber and iron; the kit's books, candles and candelabra) ----
            var urnClay = Tint(art.stone, new Color(.45f, .3f, .22f)); var ashGrey = Tint(art.stone, new Color(.2f, .19f, .19f));
            var sack = Tint(art.hay, new Color(.6f, .5f, .33f));
            Material[] bindings = { Tint(art.cloth, new Color(.42f, .1f, .08f)), Tint(art.cloth, new Color(.22f, .16f, .1f)), Tint(art.cloth, new Color(.15f, .2f, .28f)) };
            // An urn of the order's dead on a plinth.
            void Urn(float s, float a)
            {
                if (!Room(s, a, .45f)) return; var at = On(s, a); var q = Along(s);
                Part(PrimitiveType.Cube, t, at + Vector3.up * .3f, new Vector3(.6f, .6f, .6f), cutStone, q);
                Part(PrimitiveType.Sphere, t, at + Vector3.up * .88f, new Vector3(.44f, .58f, .44f), urnClay);
                Part(PrimitiveType.Cylinder, t, at + Vector3.up * 1.2f, new Vector3(.24f, .05f, .24f), urnClay);
                Block(at, new Vector3(.65f, 1.25f, .65f), q);
            }
            // A grave: a slab and its headstone toward the hall's end.
            void Grave(float s, float a)
            {
                if (!Room(s, a, .95f)) return; var at = On(s, a); var q = Along(s);
                Part(PrimitiveType.Cube, t, at + Vector3.up * .06f, new Vector3(.9f, .12f, 1.8f), ashGrey, q);
                Part(PrimitiveType.Cube, t, at + q * new Vector3(0, .5f, .9f), new Vector3(.8f, 1, .15f), cutStone, q);
                Block(at + q * new Vector3(0, 0, .9f), new Vector3(.8f, 1, .2f), q);
            }
            // A bookcase against the wall on `side`, its shelves of books facing in.
            void Shelf(float s, int side, float w)
            {
                float a = side * (w - .75f); if (!Room(s, a, .95f)) return; var at = On(s, a); var q = Along(s);
                Part(PrimitiveType.Cube, t, at + Vector3.up * 1.1f, new Vector3(.45f, 2.2f, 1.8f), dark, q);
                for (int r = 0; r < 4; r++)
                {
                    Part(PrimitiveType.Cube, t, at + q * new Vector3(-side * .24f, .3f + r * .5f, 0), new Vector3(.05f, .05f, 1.7f), timber, q);
                    for (int b = 0; b < 5; b++)
                        Part(PrimitiveType.Cube, t, at + q * new Vector3(-side * .2f, .5f + r * .5f, -.66f + b * .32f + R9() * .08f), new Vector3(.2f, .28f + R9() * .08f, .07f + R9() * .05f), bindings[(int)(R9() * 3) % 3], q * Quaternion.Euler(R9() * 8 - 4, 0, 0));
                }
                Block(at, new Vector3(.5f, 2.2f, 1.85f), q);
            }
            // A lectern with an open book and a candle.
            void Lectern(float s, float a)
            {
                if (!Room(s, a, .5f)) return; var at = On(s, a); var q = Along(s);
                Part(PrimitiveType.Cube, t, at + Vector3.up * .55f, new Vector3(.14f, 1.1f, .14f), dark, q);
                Part(PrimitiveType.Cube, t, at + Vector3.up * 1.12f, new Vector3(.6f, .06f, .45f), timber, q * Quaternion.Euler(-20, 0, 0));
                Piece("Book_Open", at + Vector3.up * 1.18f, YawAt(s), .06f);
                Piece("Candle", at + q * new Vector3(.24f, 1.16f, .18f), 0, .18f);
                Block(at, new Vector3(.6f, 1.2f, .5f), q);
            }
            // A weapon rack against the wall on `side`: a frame and five spears.
            void Rack(float s, int side, float w)
            {
                float a = side * (w - .6f); if (!Room(s, a, .95f)) return; var at = On(s, a); var q = Along(s);
                foreach (int e in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, at + q * new Vector3(0, .8f, e * .8f), new Vector3(.12f, 1.6f, .12f), dark, q);
                Part(PrimitiveType.Cube, t, at + q * new Vector3(0, 1.5f, 0), new Vector3(.12f, .1f, 1.8f), dark, q);
                Part(PrimitiveType.Cube, t, at + q * new Vector3(0, .4f, 0), new Vector3(.3f, .08f, 1.8f), dark, q);
                for (int k = 0; k < 5; k++)
                {
                    var spear = at + q * new Vector3(-side * .05f, .95f, -.6f + k * .3f);
                    Part(PrimitiveType.Cylinder, t, spear, new Vector3(.04f, .9f, .04f), timber, q * Quaternion.Euler(0, 0, side * 8));
                    Part(PrimitiveType.Cube, t, spear + Vector3.up * .95f, new Vector3(.06f, .18f, .02f), iron, q);
                }
                Block(at, new Vector3(.4f, 1.6f, 1.9f), q);
            }
            // A training dummy: a post, a straw body and head, a cross-arm.
            void Dummy(float s, float a)
            {
                if (!Room(s, a, .45f)) return; var at = On(s, a); var q = Along(s) * Quaternion.Euler(0, R9() * 40 - 20, 0);
                Part(PrimitiveType.Cylinder, t, at + Vector3.up * .8f, new Vector3(.12f, .8f, .12f), dark, q);
                Part(PrimitiveType.Cylinder, t, at + Vector3.up * 1.25f, new Vector3(.45f, .35f, .45f), sack, q);
                Part(PrimitiveType.Sphere, t, at + Vector3.up * 1.75f, new Vector3(.3f, .32f, .3f), sack, q);
                Part(PrimitiveType.Cube, t, at + Vector3.up * 1.35f, new Vector3(1.1f, .1f, .1f), dark, q);
                Block(at, new Vector3(.5f, 1.9f, .5f), Along(s));
            }
            // A pew facing on down the way (to the altar).
            void Pew(float s, float a, float len)
            {
                if (!Room(s, a, len / 2)) return; var at = On(s, a); var q = Along(s);
                Part(PrimitiveType.Cube, t, at + Vector3.up * .45f, new Vector3(len, .08f, .45f), timber, q);
                Part(PrimitiveType.Cube, t, at + q * new Vector3(0, .8f, -.2f), new Vector3(len, .7f, .06f), timber, q);
                foreach (int e in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, at + q * new Vector3(e * (len / 2 - .05f), .4f, 0), new Vector3(.08f, .8f, .45f), dark, q);
                Block(at, new Vector3(len, .9f, .5f), q);
            }
            // A forge: a stone hearth of glowing coals under an iron hood, an anvil beside it toward the way.
            void Forge(float s, int side, float w)
            {
                float a = side * (w - 1.6f); if (!Room(s, a, 1.1f)) return; var at = On(s, a); var q = Along(s);
                Part(PrimitiveType.Cube, t, at + Vector3.up * .45f, new Vector3(1.4f, .9f, 1.6f), cutStone, q);
                Part(PrimitiveType.Cube, t, at + Vector3.up * .92f, new Vector3(1.1f, .08f, 1.3f), Glowing(EmberRed, 2.2f), q);
                foreach (int e in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, at + q * new Vector3(side * .55f, 1.7f, e * .7f), new Vector3(.14f, 1.6f, .14f), iron, q);
                Part(PrimitiveType.Cube, t, at + q * new Vector3(side * .1f, 2.6f, 0), new Vector3(1.3f, .5f, 1.8f), iron, q);
                Glow(t, at + Vector3.up * 1.3f, 8, 1.1f, new Color(1, .5f, .2f), 1.1f).gameObject.AddComponent<Flicker>();
                Block(at, new Vector3(1.5f, 1, 1.7f), q);
                float sa = s, aa = a - side * 1.35f;
                if (Mathf.Abs(aa) > 1.9f && Room(sa, aa, .4f, true))
                {
                    var anvil = On(sa, aa);
                    Part(PrimitiveType.Cube, t, anvil + Vector3.up * .3f, new Vector3(.35f, .6f, .35f), dark, q);
                    Part(PrimitiveType.Cube, t, anvil + Vector3.up * .68f, new Vector3(.7f, .16f, .3f), iron, q);
                    Block(anvil, new Vector3(.75f, .8f, .45f), q);
                }
            }
            // A catwalk along the wall on `side` at 2.7 m, its posts at the wall and braces out to its edge, a rail on the inner edge.
            void Catwalk(float s0, float s1, int side, float w)
            {
                const float deck = 4.5f; float mid = side * (w - .85f);
                for (float s = s0; s < s1 - 1; s += 2)
                {
                    var q = Along(s + 1); var at = On(s + 1, mid); if (!Free(at + Vector3.up * deck)) continue;
                    Part(PrimitiveType.Cube, t, at + Vector3.up * deck, new Vector3(1.1f, .1f, 2.05f), timber, q);
                    Part(PrimitiveType.Cube, t, at + q * new Vector3(-side * .53f, deck + .55f, 0), new Vector3(.06f, .06f, 2.05f), dark, q);
                    Part(PrimitiveType.Cube, t, at + q * new Vector3(-side * .53f, deck + .28f, -.95f), new Vector3(.06f, .55f, .06f), dark, q);
                }
                for (float s = s0; s <= s1; s += 3)
                {
                    float a = side * (w - .32f); var q = Along(s); if (!Room(s, a, .2f)) continue; var p = On(s, a);
                    Part(PrimitiveType.Cube, t, p + Vector3.up * deck / 2, new Vector3(.2f, deck, .2f), dark, q);
                    Part(PrimitiveType.Cube, t, p + q * new Vector3(-side * .4f, deck - .45f, 0), new Vector3(.9f, .1f, .1f), dark, q * Quaternion.Euler(0, 0, side * 35));
                    Block(p, new Vector3(.25f, deck, .25f), q);
                }
            }
            // A gantry over the bay at s: iron posts at the walls, a beam across, a chain and hook.
            void Gantry(float s, float w)
            {
                var q = Along(s); const float top = 11;
                foreach (int side in new[] { -1, 1 })
                {
                    float a = side * (w - 1); if (!Room(s, a, .35f)) continue; var p = On(s, a);
                    Part(PrimitiveType.Cube, t, p + Vector3.up * top / 2, new Vector3(.4f, top, .4f), iron, q); Block(p, new Vector3(.5f, top, .5f), q);
                }
                Part(PrimitiveType.Cube, t, On(s, 0) + Vector3.up * top, new Vector3(w * 2 - 1.6f, .45f, .45f), rust, q);
                Part(PrimitiveType.Cylinder, t, On(s, 1.5f) + Vector3.up * (top - 1.2f), new Vector3(.05f, 1.2f, .05f), iron, q);
                Part(PrimitiveType.Cube, t, On(s, 1.5f) + Vector3.up * (top - 2.5f), new Vector3(.3f, .3f, .1f), iron, q);
            }
            switch (part)
            {
                case "geode": GeodeFloor(); break;
                case "foundry": Foundry(); break;
                case "vent": EmberVent(); break;
                case "ashgarden": case "scriptorium": case "musteryard": ChapterRoom(); break;
                case "chapel": ChapelRoom(); break;
                case "breach": GreyBreach(); break;
                case "river": PlainCave(RiverLight); break;
                case "plain": PlainCave(GalleryLight); break;
                case "workings": MainAdit(true, false, false); break;
                case "gallery": MainAdit(false, true, false); break;
                case "deep": MainAdit(false, false, true); break;
                default: MainAdit(true, true, true); break;
            }
            DataGates();   // (the Adit rework) the gates the zone's data puts in this cave: before the floors are filled, which keep clear of gates
            KitDressing();
            FloorClutter();
            CampDressing();
            if (kit != null && Application.isPlaying) StaticBatchingUtility.Combine(kit.gameObject);

            // The floors filled (Chris, 2026-10-11: "room full of nothing", "more nothingness"): drawn out two and a half times as wide,
            // the chambers kept their rubble at the walls' feet only and the floor between lay bare. Rock outcrops, rubble, dripstone and
            // fallen timber over the open floor of every wide stretch (geode crystal on the Geode Floor, basalt in the Vent, grey rock in
            // the Breach), each in a place of its own (Room: off the way down the middle, off the camps and off what stands), never in a
            // built hall, at a gate or on the lava; the big ones solid. Its own stream, so nothing placed before it moves.
            // The camps lived in (2026-10-11, the captures: the drawn-out halls read as bare floor with people standing on it): at the edge of
            // each camp of people a brazier burning (its warm light against the cave's cold), barrels and crates of their stores and bedrolls;
            // by each camp of beasts, bones. Each in a place of its own (Room), so none stands on a camp, the way or anything else. Its own
            // stream, after the floors, so nothing placed before moves.
            void CampDressing()
            {
                if (Zone.camps == null) return;
                var cr = TreeRandom(t.position + new Vector3(9.1f, 0, 4.3f)); float C() { return (float)cr.NextDouble(); }
                var cloth = Tint(art.hay, new Color(.45f, .36f, .26f)); var blanketRed = Tint(art.hay, new Color(.45f, .16f, .12f));
                foreach (var cp in Zone.camps)
                {
                    if (cp == null || cp.cave != h.Name || cp.elite || (cp.route != null && cp.route.Length > 1)) continue;   // lords keep their halls; patrols walk
                    bool people = cp.look == "deserter" || cp.look == "outrider" || cp.look == "cultist";
                    float turn = C() * Mathf.PI * 2;
                    for (int k = 0; k < (people ? 5 : 3); k++)
                    {
                        float ang = turn + k * 1.37f, r = k == 0 ? .7f : .55f, edge = cp.radius + r + 1.1f + C() * .8f;
                        float s = cp.along + Mathf.Cos(ang) * edge, a = cp.aside + Mathf.Sin(ang) * edge;
                        if (s < 2 || s > h.Length - 2 || !Room(s, a, r)) continue;
                        var at = On(s, a); at.y = FloorY(at.x, at.z);
                        if (cp.rise > .5f)   // (the Adit rework) a camp up on a deck: its things on the deck, only where the deck is
                        {
                            var w = t.TransformPoint(at); float want = w.y + cp.rise;
                            if (!DeckTop(new Vector2(w.x, w.z), want, out float top) || Mathf.Abs(top - want) > .6f) continue;
                            at.y += (top - w.y) / Mathf.Max(.01f, t.lossyScale.y);
                        }
                        if (!people) { Piece(C() < .5f ? "Bones" : "Bones2", at, C() * 360, .34f + C() * .12f); continue; }
                        if (k == 0) { Brazier(t, at, null); Block(at, new Vector3(.9f, 1.1f, .9f), Along(s)); continue; }
                        if (k <= 2)
                        {
                            if (C() < .5f) { var b = Piece("Barrel", at, C() * 360, .95f); if (b != null) Block(at, new Vector3(.9f, .95f, .9f), Along(s)); }
                            else { Crate(at, Along(s) * Quaternion.Euler(0, C() * 40 - 20, 0), 1 + (int)(C() * 2), false); Block(at, new Vector3(1.1f, 1.2f, 1.1f), Along(s)); }
                            continue;
                        }
                        // A bedroll: a blanket laid out and a rolled cloak for a pillow.
                        var bq = Along(s) * Quaternion.Euler(0, C() * 360, 0);
                        Part(PrimitiveType.Cube, t, at + Vector3.up * .03f, new Vector3(.8f, .05f, 1.9f), C() < .5f ? cloth : blanketRed, bq);
                        Part(PrimitiveType.Cylinder, t, at + bq * new Vector3(0, .1f, .78f), new Vector3(.2f, .38f, .2f), cloth, bq * Quaternion.Euler(0, 0, 90));
                    }
                }
            }
            void FloorClutter()
            {
                var fr = TreeRandom(t.position + new Vector3(3.7f, 0, 11.3f)); float F() { return (float)fr.NextDouble(); }
                int lake = part == "vent" ? Named("lake", 26 * X, h.Length - 8 * X) : part == "chapel" ? AnchorRing("lake") : -1;
                float lakeS = lake >= 0 ? h.Along[lake] : -999, lakeR = lake >= 0 ? LakeRz(lake) + 4 : 0;
                var rock = Tint(art.stone, AditLining(part) * .55f); var rubbleRock = Tint(art.stone, AditLining(part) * .45f);   // (at .8 they read near white in the cave's light)
                bool geodes = part == "geode" || part == "foundry", drips = part == "main" || part == "breach" || part == "plain" || part == "workings" || part == "gallery" || part == "deep" || part == "river";
                for (float s = 6; s < h.Length - 4; s += 2.2f)
                {
                    int i = RingAt(s); float half = h.Half[i];
                    bool skip = half < 6 || Mathf.Abs(s - lakeS) < lakeR;   // a drift keeps its rubble at the walls only
                    foreach (var span in hallSpans) if (s > span.x && s < span.y) skip = true;
                    foreach (var g in Gates) if (g != null && g.Cave == h.Name && Mathf.Abs(s - g.Along) < 8) skip = true;
                    int tries = 2 + (int)(half / 3.5f);   // more across the wider chambers (fewer left them bare: 2026-10-11 captures, 12:00 and 14:59)
                    for (int k = 0; k < tries; k++)
                    {
                        float a = (F() * 2 - 1) * half * .8f, roll = F(), size = F(), turn = F() * 360;
                        if (skip) continue;
                        bool wall = Mathf.Abs(a) > half * .5f;   // toward the walls, more and bigger outcrops
                        bool big = roll < (wall ? .45f : .25f); float r = big ? (wall ? 1.4f + size * 1.8f : 1.1f + size * 1.2f) : .55f + size * .45f;
                        if (!Room(s, a, r)) continue;
                        var at = On(s, a); at.y = FloorY(at.x, at.z) - .06f;
                        if (big)
                        {
                            // An outcrop: a boulder sunk into the floor with a smaller one leaning on it.
                            Lump(BoulderAt((int)(size * 97)), t, at, new Vector3(r * 1.4f, r * (.75f + roll), r * 1.2f), rock, turn);
                            Lump(BoulderAt((int)(size * 97) + 3), t, at + Along(s) * new Vector3(r * .7f, 0, r * .4f), Vector3.one * r * .55f, rubbleRock, turn + 70);
                            Block(at, new Vector3(r * 1.7f, r * 1.1f, r * 1.5f), Along(s));
                        }
                        else if (geodes && roll < .55f)
                        {
                            // A clutch of geode crystal out of the floor, lit faintly.
                            for (int q = 0; q < 4; q++)
                            {
                                var dir = (Vector3.up + new Vector3(Mathf.Sin(turn + q * 1.7f), 0, Mathf.Cos(turn + q * 1.7f)) * .45f).normalized; float len = .4f + ((q * 37 + (int)(size * 50)) % 7) * .1f;
                                Part(PrimitiveType.Cube, t, at + dir * len / 2, new Vector3(.14f, len, .14f), geode, Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(0, turn + q * 40, 0));
                            }
                        }
                        else if (drips && roll < .55f)
                        {
                            // Dripstone: two or three stalagmites, a tall one and its shorter neighbours.
                            for (int q = 0; q < 3; q++)
                            {
                                var foot = at + Along(s) * new Vector3((q - 1) * r * .7f, 0, (q % 2) * r * .5f); float tall = (q == 1 ? 1.4f : .7f) + size * 1.6f;
                                MeshPart(Drip(i + q), t, foot, rock).transform.localScale = new Vector3(.35f + size * .3f, tall, .35f + size * .3f);
                            }
                            Block(at, new Vector3(r * 1.6f, 2, r * 1.6f), Along(s));
                        }
                        else if (roll < .75f)
                        {
                            // A rubble heap: three stones.
                            for (int q = 0; q < 3; q++) Lump(BoulderAt(q + (int)(size * 13)), t, at + Along(s) * new Vector3((q - 1) * r * .6f, 0, ((q * 2) % 3 - 1) * r * .4f), Vector3.one * r * (.35f + q * .1f), rubbleRock, turn + q * 50);
                        }
                        else
                        {
                            // Fallen timber: a pit prop and a cap-piece, broken and grey with dust.
                            Part(PrimitiveType.Cube, t, at + Vector3.up * .14f, new Vector3(.24f, .24f, 2.4f + size * 1.5f), dark, Quaternion.Euler(0, turn, 3));
                            Part(PrimitiveType.Cube, t, at + Vector3.up * .32f + Quaternion.Euler(0, turn, 0) * new Vector3(.4f, 0, .3f), new Vector3(.22f, .22f, 1.6f), timber, Quaternion.Euler(0, turn + 35, 8));
                        }
                    }
                }
            }

            // Bones and fallen rock at the walls' feet the length of the cave, a column fallen in the Breach and the Gallery, and the
            // chests, barrels, candelabra, books and carpet of the rooms people kept.
            void KitDressing()
            {
                string[] rubble = { "Rock1", "Rock2", "Rock3", "Rock4", "Rock5", "Rock1", "Rock3", "Bones", "Bones2" };
                for (float s = 4; s < h.Length - 3; s += 3.5f)
                {
                    if (K() < .45f) { K(); K(); continue; }   // (rubble as dense as ever: three times as much of it down the drawn-out caves)
                    int i = RingAt(s); int side = K() < .5f ? 1 : -1; string what = rubble[(int)(K() * rubble.Length) % rubble.Length];
                    if (part == "breach" && K() < .2f) what = "Column_Broken";
                    var at = On(s, side * h.Half[i] * (.74f + K() * .08f)); at.y = FloorY(at.x, at.z) - .04f; if (!Free(at)) continue;
                    bool bones = what.StartsWith("Bones"), column = what.StartsWith("Column");
                    var rubbleGo = Piece(what, at, K() * 360, bones ? .32f + K() * .12f : column ? .9f : .22f + K() * .2f);
                    if (rubbleGo != null && !bones) foreach (var r in rubbleGo.GetComponentsInChildren<Renderer>()) r.sharedMaterial = stone;   // the cave's own rock, not the kit's pale grey (it read as crystal in the dark)
                }
                switch (part)
                {
                    case "vent":
                    {
                        // The Ash cell's altar: a red carpet up to it (short of the lake's rim), a tall candelabrum either side.
                        float sa = h.Length - 8 * X; var qa = Along(sa); var altar = On(sa, 0);
                        if (!Free(altar)) break;
                        Piece("Carpet", On(sa - 4.5f, 0) + Vector3.up * .01f, YawAt(sa), .06f, null, new Vector3(2.4f, .06f, 6.6f));
                        foreach (int side in new[] { -1, 1 }) { var cd = On(sa, side * 2.1f); if (Free(cd)) { Piece("Candelabrum_tall", cd, YawAt(sa), 1.9f); Block(cd, new Vector3(.6f, 1.9f, .6f), qa); } }
                        break;
                    }
                    case "breach":
                    {
                        // The Foreman's room: his strongbox against the wall, gilt gone dull, and a barrel.
                        int foreman = Named("foreman", 46 * X, h.Length - 3 * X); if (foreman < 0) break;
                        float s0 = h.Along[foreman] + 3 * X; var q = Along(s0); var box = On(s0, h.Half[foreman] * .62f); box.y = FloorY(box.x, box.z);
                        if (Free(box)) { Piece("Chest_gold", box, YawAt(s0) - 90, .85f); Block(box, new Vector3(1, .9f, 1), q); }
                        var barrel = On(s0 + 1.6f, h.Half[foreman] * .66f); barrel.y = FloorY(barrel.x, barrel.z);
                        if (Free(barrel)) { Piece("Barrel", barrel, K() * 360, 1f); Block(barrel, new Vector3(.9f, 1, .9f), q); }
                        break;
                    }
                    case "workings": case "main":
                    {
                        // The gang-boss's hall: a candelabrum and his open book on the table (the lantern stays), a chest by the crates.
                        int boss = Named("boss", 44 * X, 64 * X); if (boss < 0) break;
                        float s0 = h.Along[boss]; var q = Along(s0); var table = On(s0 + 1.5f, h.Half[boss] * .45f);
                        if (Free(table))
                        {
                            Piece("Candelabrum", table + Vector3.up * .84f + q * new Vector3(.15f, 0, .9f), YawAt(s0), .5f);
                            Piece("Book_Open", table + Vector3.up * .84f + q * new Vector3(-.1f, 0, -.1f), YawAt(s0) + 90, .06f);
                        }
                        var chest = On(s0 - 1.2f, -h.Half[boss] * .7f); chest.y = FloorY(chest.x, chest.z);
                        if (Free(chest)) { Piece("Chest", chest, YawAt(s0) + 90, .8f); Block(chest, new Vector3(.9f, .8f, .9f), q); }
                        // Torches in the hall and by the cages, warm against the lum-tubes' green.
                        foreach (int side in new[] { -1, 1 }) WallTorch(s0 + side * 5 * X, side, side > 0);
                        int cages = Named("cages", 18 * X, 36 * X); if (cages >= 0) WallTorch(h.Along[cages] + 6 * X, 1, true);
                        break;
                    }
                    default: break;   // (the geode floor, the foundry, the chapter-house's rooms, the gallery, the deep way, the river, plain caves)
                }
            }
            // (The Adit rework) The ring of this cave's anchor of that name, or -1 when the data names none here.
            int AnchorRing(string name) { var a = AnchorIn(h, name); return a != null ? RingAt(a.along) : -1; }

            // The main way: in the first Adit one cave from the mouth to the Rail Hall (all three parts); in the Adit rework three caves of their
            // own, each dressed with its part: the workings (the mouth, the drifts, the cages, the gang-boss's hall), the hub (the Singing
            // Gallery) and the deep way (the singing door, the shaft stair, the platform gate and the Rail Hall).
            void MainAdit(bool workings, bool hub, bool deep)
            {
                bool whole = workings && hub && deep;
                int gallery = whole || hub ? Named("gallery", 80 * X, 120 * X) : -1, hall = whole || deep ? Named("hall", 176 * X, h.Length - 4 * X) : -1;
                float galleryFrom = gallery >= 0 ? h.Along[gallery] - 16 * X : 85 * X, galleryTo = gallery >= 0 ? h.Along[gallery] + 15 * X : 117 * X, hallFrom = hall >= 0 ? h.Along[hall] - 16 * X : 178 * X;
                if (!whole)
                {
                    // A part in a cave of its own: the workings run the cave's whole length; the deep way starts at its mouth.
                    if (workings) { galleryFrom = galleryTo = h.Length; hallFrom = float.MaxValue; }
                    if (deep) { galleryFrom = galleryTo = 0; }
                    if (hub)
                    {
                        // The way down to it (the Gallery's sump drift): pale lamps on the walls, every fourteen metres.
                        int sumpNo = 0;
                        for (float s = 4; s < galleryFrom - 6; s += 14, sumpNo++) WallLamp(RingAt(s), sumpNo % 2 == 0 ? 1 : -1, GalleryLight, 10, .7f);
                    }
                }
                // The mouth: a heavy timber portal, the old seal's boards broken and thrown down, a lantern either side.
                if (workings)
                {
                    var q = Along(1); float w = h.Half[RingAt(1)] * .9f;
                    // A cut-stone arch (the kit's Entrance, fitted to the drift), or the old timber portal without the kit.
                    if (Piece("Entrance", On(1, 0), YawAt(1) + 90, 3.1f, null, new Vector3(.5f, 3.1f, w * 2 + .7f)) == null)
                    {
                        foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, On(1, side * w) + Vector3.up * 1.4f, new Vector3(.34f, 2.8f, .34f), dark, q);
                        Part(PrimitiveType.Cube, t, On(1, 0) + Vector3.up * 2.85f, new Vector3(w * 2 + .7f, .38f, .4f), dark, q);
                    }
                    for (int k = 0; k < 4; k++) Part(PrimitiveType.Cube, t, On(-1.5f - k * .4f, (k - 1.5f) * .7f) + Vector3.up * .06f, new Vector3(.32f, .06f, 2 + A()), timber, q * Quaternion.Euler(0, (A() - .5f) * 70, 0));
                    // Torches on the arch's face either side, looking out to the yard (their light where the lanterns' was).
                    foreach (int side in new[] { -1, 1 }) Torch(On(.7f, side * (w + .15f)), q * Vector3.back, true);
                }
                // Timber sets down the drifts and the stair (not in the chambers): two posts and a cap every 2.4 m.
                for (float s = 3; (workings || deep) && s < h.Length - 2; s += 3.6f)
                {
                    if (s > galleryFrom - 2 * X && s < galleryTo + 2 * X || s > hallFrom - 2 * X) continue;
                    int i = RingAt(s); if (h.Half[i] > 3.4f * DW) continue;
                    var q = Along(s); float w = h.Half[i] * .78f, top = Mathf.Min(h.Height[i] * .8f, 3.8f);
                    var l = On(s, -w); var r = On(s, w); if (!Free(l) || !Free(r)) continue;
                    Part(PrimitiveType.Cube, t, l + Vector3.up * top / 2, new Vector3(.24f, top, .24f), timber, q);
                    Part(PrimitiveType.Cube, t, r + Vector3.up * top / 2, new Vector3(.24f, top, .24f), timber, q);
                    Part(PrimitiveType.Cube, t, (l + r) / 2 + Vector3.up * (top + .12f), new Vector3(w * 2 + .5f, .24f, .26f), timber, q);
                }
                // The Workings' rails, the carts on them, and the lum-tubes: green light every eight metres.
                if (workings)
                {
                    Rails(2, galleryFrom - 1, 0);
                    Cart(Mathf.Min(galleryFrom - 6 * X, 27 * X), -1.6f * CW, false); Cart(Mathf.Min(galleryFrom - 30 * X, 57 * X), 1.6f * CW, true);   // in the hall, off the way (a cart in the narrow drift cut the navmesh)
                    int lumNo = 0;
                    for (float s = 4; s < galleryFrom; s += 14, lumNo++) WallLamp(RingAt(s), lumNo % 2 == 0 ? 1 : -1, LumGreen, 10, .8f);
                }
                // The cages: three along the left wall of the first chamber and two on the right, a pressed goblin huddled in each
                // (EncounterSession.Adit puts them there). Each is usable (E, "Open the cage"): the quest "The Pressed" (Quests/adit.json) has
                // you open all five; once opened its door stands open. (Playtest note 125, Chris, 2026-10-11: "says open the cage but it is
                // obviously broken and nothing in it": bars on two sides only, the straw a slab off the floor, nobody inside.) Barred all
                // round on the cave's floor, its door on the side toward the way, hinged at a corner ("Door").
                int cages = workings ? Named("cages", 18 * X, 36 * X) : -1;
                if (cages >= 0)
                {
                    float s0 = h.Along[cages];
                    for (int k = 0; k < 5; k++)
                    {
                        float s = k < 3 ? s0 - 3.5f * X + k * 3.2f * X : s0 - 1.9f * X + (k - 3) * 3.2f * X; int side = k < 3 ? -1 : 1;
                        var q = Along(s); var at = On(s, side * h.Half[RingAt(s)] * .62f); if (!Free(at)) continue;
                        at.y = FloorY(at.x, at.z);   // on the floor where it stands (the ring's height left the straw a slab in the air)
                        var cage = new GameObject(AditCage).transform; cage.SetParent(t, false); cage.localPosition = at; cage.localRotation = q;
                        Interactables.Add(new ZoneInteractable { name = AditCage, prompt = "Open the cage", kind = "cage", once = true, position = cage.position, root = cage });
                        foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                                Part(PrimitiveType.Cube, cage, new Vector3(sx * .8f, .95f, sz * .8f), new Vector3(.12f, 1.9f, .12f), iron);
                        Part(PrimitiveType.Cube, cage, new Vector3(0, 1.92f, 0), new Vector3(1.75f, .08f, 1.75f), iron);   // the lid
                        Part(PrimitiveType.Cube, cage, new Vector3(0, .04f, 0), new Vector3(1.75f, .08f, 1.75f), iron);    // the floor frame
                        float doorX = -side * .8f;   // the door on the side toward the way
                        for (int b = -2; b <= 2; b++)
                        {
                            foreach (float z in new[] { .8f, -.8f }) Part(PrimitiveType.Cylinder, cage, new Vector3(b * .32f, .95f, z), new Vector3(.04f, .95f, .04f), iron);   // the ends
                            Part(PrimitiveType.Cylinder, cage, new Vector3(-doorX, .95f, b * .32f), new Vector3(.04f, .95f, .04f), iron);   // the back, against the wall
                        }
                        // The door: hinged at its corner, four bars between two rails, a padlock on its catch.
                        var hinge = new GameObject("Door").transform; hinge.SetParent(cage, false); hinge.localPosition = new Vector3(doorX, 0, -.74f);
                        for (int b = 0; b < 4; b++) Part(PrimitiveType.Cylinder, hinge, new Vector3(0, .95f, .24f + b * .32f), new Vector3(.04f, .9f, .04f), iron);
                        foreach (float y in new[] { .18f, 1.75f }) Part(PrimitiveType.Cube, hinge, new Vector3(0, y, .74f), new Vector3(.05f, .06f, 1.48f), iron);
                        Part(PrimitiveType.Cube, hinge, new Vector3(0, 1, 1.42f), new Vector3(.08f, .14f, .1f), rust);   // the padlock
                        // Inside: straw on the floor, a tin bowl, a chain from the back bars.
                        Part(PrimitiveType.Cube, cage, new Vector3(0, .09f, 0), new Vector3(1.4f, .03f, 1.4f), straw, Quaternion.Euler(0, 20, 0));
                        Part(PrimitiveType.Cylinder, cage, new Vector3(-doorX * .4f, .13f, .45f), new Vector3(.22f, .03f, .22f), rust);
                        for (int l = 0; l < 5; l++) Part(PrimitiveType.Cube, cage, new Vector3(-doorX * (.75f - l * .06f), .55f - l * .1f, -.3f), new Vector3(.03f, .08f, .05f), iron, Quaternion.Euler(0, 0, l * 30));
                        Block(at, new Vector3(1.75f, 2, 1.75f), q);
                    }
                }
                // The gang-boss's hall: a long table on trestles with his tally book and a lantern, crates and a weapon rack.
                int boss = workings ? Named("boss", 44 * X, 64 * X) : -1;
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
                    for (int k = -1; k <= 1; k++) { int i = RingAt(h.Along[gallery] + k * 9 * X); Glow(t, c[i] + Vector3.up * h.Height[i] * .78f, 26 * CW, .9f, GalleryLight, .9f); }
                    for (int i = 0; i < n; i++)
                    {
                        if (h.Along[i] < galleryFrom || h.Along[i] > galleryTo || h.Half[i] < 8 * CW || i % 2 == 1) continue;
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
                    for (int k = 0; k < 4; k++) { int i = RingAt(galleryFrom + 6 * X + k * 6 * X); Part(PrimitiveType.Sphere, t, c[i] + Vector3.up * h.Height[i] * .97f + right[i] * (k - 1.5f) * 3 * CW, new Vector3(3.2f, .5f, 2.6f), Tint(art.stone, new Color(.03f, .03f, .04f))); }
                    // The pilgrims' echo-jars: seven clay jars set down round the Gallery's floor where the singing is loudest, a faint
                    // light in each. Taken (E) while "Echoes in the Stone" wants one, a jar is gone for a while and comes back (kind crates).
                    // Fixed places, no draws from the stream, so nothing else in the cave moves.
                    var clay = Tint(art.stone, new Color(.62f, .5f, .38f)); var echo = Glowing(new Color(1, .82f, .9f), .9f);
                    for (int k = 0; k < 7; k++)
                    {
                        float s = galleryFrom + 3 * X + k * (galleryTo - galleryFrom - 6 * X) / 6f; int i = RingAt(s);
                        var at = On(s, (k % 2 == 0 ? -1 : 1) * h.Half[i] * (.42f + (k % 3) * .06f)); at.y = FloorY(at.x, at.z); if (!Free(at)) continue;
                        var jar = new GameObject(AditEchoJar).transform; jar.SetParent(t, false); jar.localPosition = at; jar.localRotation = Along(s) * Quaternion.Euler(0, k * 47, 0);
                        Part(PrimitiveType.Sphere, jar, new Vector3(0, .22f, 0), new Vector3(.34f, .42f, .34f), clay);
                        Part(PrimitiveType.Cylinder, jar, new Vector3(0, .47f, 0), new Vector3(.15f, .06f, .15f), clay);
                        Part(PrimitiveType.Sphere, jar, new Vector3(0, .53f, 0), new Vector3(.1f, .05f, .1f), echo);   // what it holds, showing at the neck
                        Interactables.Add(new ZoneInteractable { name = AditEchoJar, prompt = "Take the echo-jar", item = "item.adit_echo_jar", kind = "crates", position = jar.position, root = jar });
                    }
                    foreach (var o in Hollow.All)
                    {
                        if (o.Parent != h || !whole) continue;   // (the Adit rework: the Gallery's lamp rings stand where its data puts them, below)
                        string way = AditPart(o); var col = WayLight(way);
                        float s = Mathf.Min(6 * X, o.Length * .1f), side = o.Half[o.Nearest(new Vector2(o.At(s).x, o.At(s).z), out _)] + .2f;
                        foreach (int k in new[] { -1, 1 })
                        {
                            var foot = t.InverseTransformPoint(o.At(s, k * side)); foot.y = FloorY(foot.x, foot.z);
                            Part(PrimitiveType.Cylinder, t, foot + Vector3.up * .9f, new Vector3(.1f, .9f, .1f), iron);
                            Lamp(foot + Vector3.up * 1.95f, col, 9, 1.1f, .16f);
                        }
                    }
                    // (The Adit rework) The lamp rings at the Gallery's ways on (ZoneAnchor "lamp ring amber", "red", "grey"): six glowing
                    // stones round a light on the floor, each the colour of where its way goes, so a lost party finds the wing it wants.
                    if (!whole && Zone.anchors != null)
                        foreach (var a in Zone.anchors)
                        {
                            if (a == null || a.cave != h.Name || a.name == null || !a.name.StartsWith("lamp ring")) continue;
                            var col = a.name.EndsWith("amber") ? new Color(1, .72f, .25f) : a.name.EndsWith("red") ? EmberRed : a.name.EndsWith("blue") ? RailBlue : GreyLight;
                            var mid = On(a.along, a.aside); mid.y = FloorY(mid.x, mid.z); var glowStone = Glowing(col, 1.4f);
                            for (int k = 0; k < 6; k++)
                            {
                                float ang = k * Mathf.PI / 3; var st = mid + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * 2.2f; if (!Free(st)) continue;
                                Lump(BoulderAt(k), t, st, new Vector3(.5f, .35f, .45f), glowStone, k * 60);
                            }
                            Glow(t, mid + Vector3.up * 1.6f, 12, 1.1f, col, 1.1f);
                        }
                }
                // The winch gate at the head of the stair (dungeon step D3): shut until the three rail sigils are set in the frame on
                // its left post, three sockets that light amber, red and grey when they are (EncounterSession.Adit). Its grille is wound up
                // by hand on a timber windlass on the right (Chris, 2026-10-11, no machines: it had been "the cage-lift").
                // (The Adit rework) The singing door at the Gallery's north neck: an old goblin resonance door of stone (look "song"), the three
                // sockets on its left post; nothing winds it (song, not gear), so no windlass stands by it.
                if (deep)
                {
                    var liftDef = GateIn(h, AditLiftGate);   // (the Adit rework) where the zone's data puts it, when it does
                    float s = liftDef != null ? liftDef.along : galleryTo + 4 * X; var gate = Gate(s, AditLiftGate, "liftgate", "Set the rail sigils", null, liftDef != null ? liftDef.look : null);
                    if (liftDef != null) gate.OpensOn = liftDef.opensOn;
                    var q = Along(s); float w = h.Half[RingAt(s)] * .9f; var plate = On(s - .3f, -w + .55f) + Vector3.up * 1.45f;
                    if (liftDef == null || liftDef.look != "song")
                    {
                        var winch = On(s - 1.1f, w - .7f);
                        foreach (float pz in new[] { -.45f, .45f }) Part(PrimitiveType.Cube, t, winch + q * new Vector3(0, .6f, pz), new Vector3(.18f, 1.2f, .18f), timber, q);   // its two posts
                        Part(PrimitiveType.Cylinder, t, winch + q * new Vector3(0, 1.05f, 0), new Vector3(.32f, .5f, .32f), timber, q * Quaternion.Euler(90, 0, 0));   // the drum
                        Part(PrimitiveType.Cylinder, t, winch + q * new Vector3(0, 1.05f, 0), new Vector3(.36f, .2f, .36f), straw, q * Quaternion.Euler(90, 0, 0));   // the rope wound on it
                        foreach (float pz in new[] { -.6f, .6f }) for (int spoke = 0; spoke < 4; spoke++) Part(PrimitiveType.Cube, t, winch + q * new Vector3(0, 1.05f, pz) + q * Quaternion.Euler(spoke * 45, 0, 0) * new Vector3(0, .42f, 0), new Vector3(.06f, .84f, .06f), timber, q * Quaternion.Euler(spoke * 45, 0, 0));   // the spokes
                        var ropeTop = On(s, w - .9f) + Vector3.up * (h.Height[RingAt(s)] * .82f - .3f); var ropeFoot = winch + Vector3.up * 1.2f;
                        var rope = Part(PrimitiveType.Cylinder, t, (ropeTop + ropeFoot) / 2, new Vector3(.03f, Vector3.Distance(ropeTop, ropeFoot) / 2, .03f), straw);
                        rope.transform.localRotation = Quaternion.FromToRotation(Vector3.up, ropeTop - ropeFoot);
                        Block(winch, new Vector3(.6f, 1.3f, 1.4f), q);
                    }
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
                // (The Adit rework) Where the zone's data puts it (ZoneAnchor "spirit stone"), and a second in the Rail Hall just inside the
                // platform gate ("rail spirit stone"), each on the floor, its touching place a step toward the way.
                void SpiritStone(string name, float s, float a)
                {
                    var q = Along(s); var foot = On(s, a); foot.y = FloorY(foot.x, foot.z);
                    var marker = new GameObject(name).transform; marker.SetParent(t, false); marker.localPosition = foot; marker.localRotation = q * Quaternion.Euler(0, 15, 0);
                    Part(PrimitiveType.Cube, marker, new Vector3(0, 1.1f, 0), new Vector3(.9f, 2.2f, .55f), Tint(art.stone, new Color(.58f, .6f, .66f)), Quaternion.Euler(0, 0, 4));
                    Part(PrimitiveType.Cube, marker, new Vector3(0, .12f, 0), new Vector3(1.4f, .24f, 1), Tint(art.stone, new Color(.4f, .41f, .44f)));
                    Part(PrimitiveType.Sphere, marker, new Vector3(0, 1.45f, .29f), new Vector3(.22f, .3f, .04f), Glowing(new Color(.7f, .85f, 1), 2));   // the rune
                    Glow(marker, new Vector3(0, 1.6f, .8f), 9, .9f, new Color(.7f, .85f, 1), .9f);
                    Block(foot, new Vector3(1, 2.2f, .7f), q);
                    var stand = On(s, a - (a < 0 ? -1.3f : 1.3f));
                    Interactables.Add(new ZoneInteractable { name = name, prompt = "Touch the spirit stone", kind = "stone", position = t.TransformPoint(stand), root = marker });
                }
                if (gallery >= 0)
                {
                    var sa = whole ? null : AnchorIn(h, "spirit stone");
                    if (sa != null) SpiritStone(AditSpiritStone, sa.along, sa.aside);
                    else { float s = galleryFrom + 4 * X; SpiritStone(AditSpiritStone, s, h.Half[RingAt(s)] * .74f); }
                }
                if (deep && !whole) { var ra = AnchorIn(h, "rail spirit stone"); if (ra != null) SpiritStone(AditRailStone, ra.along, ra.aside); }
                // The stair: plank treads where it runs steep, a rope rail on posts, a cold lamp every nine metres.
                float lastPost = -9; int stairLamp = 0;
                for (float s = galleryTo + 5 * X; deep && s < hallFrom; s += .55f)
                {
                    int i = RingAt(s), j = Mathf.Min(n - 1, i + 2); float run = h.Along[j] - h.Along[i];
                    if (run <= 0 || (c[i].y - c[j].y) / run < .18f) continue;   // (drawn out, the stair falls a third less steeply)
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
                    float foot = galleryTo + 5 * X, narrow = 9 * CW;
                    for (int i = 0; i < n; i++) if (h.Along[i] > hallFrom - 22 * X && h.Along[i] < hallFrom - 6 * X && h.Half[i] < narrow) { narrow = h.Half[i]; foot = h.Along[i]; }
                    var platDef = GateIn(h, AditPlatformGate); if (platDef != null) foot = platDef.along;   // (the Adit rework) the data's place for it
                    Gate(foot, AditPlatformGate, "platformgate", "Open the gate", platDef != null && !string.IsNullOrEmpty(platDef.lord) ? platDef.lord : AditQuartermaster);
                    float sk = foot - 7 * X; int ik = RingAt(sk); var q = Along(sk); var keg = On(sk, h.Half[ik] * .5f); keg.y = FloorY(keg.x, keg.z);
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
            // (The Adit rework) look: what shuts the way (grille; door, a timber drop-door; boards, sluice-boards; bar, a timber bar; stockade,
            // a pen's stakes; bookcase, a case of books that slides aside; song, a goblin resonance door of stone); empty: by its kind. far:
            // worked from beyond it (the lamps and the thing to use there). No prompt: nothing to use (it opens on its own).
            ZoneGate Gate(float s, string title, string kind, string prompt, string lord, string look = null, bool far = false)
            {
                var q = Along(s); int i = RingAt(s); float w = h.Half[i] * .9f, top = h.Height[i] * .82f, high = top - .25f;
                if (string.IsNullOrEmpty(look)) look = kind == "bossdoor" ? "door" : kind == "bar" ? "bar" : kind == "pen" ? "stockade" : kind == "hiddendoor" ? "bookcase" : "grille";
                float near = far ? 1 : -1;   // the side it is worked from
                // A cut-stone arch round the grille (the kit's Entrance2 fitted to the way), or the old timber frame without the kit. (A
                // bookcase stands in the rock of the passage itself: no arch.)
                if (look != "bookcase" && Piece("Entrance2", On(s, 0), YawAt(s) + 90, top + .45f, null, new Vector3(.6f, top + .45f, w * 2 + .8f)) == null)
                {
                    foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, On(s, side * w) + Vector3.up * top / 2, new Vector3(.4f, top, .4f), dark, q);
                    Part(PrimitiveType.Cube, t, On(s, 0) + Vector3.up * top, new Vector3(w * 2 + .8f, .45f, .45f), dark, q);
                }
                if (look == "grille" || look == "song") foreach (int side in new[] { -1, 1 }) Lamp(On(s + near * .6f, side * (w - .3f)) + Vector3.up * 2.1f, look == "song" ? GalleryLight : RailBlue, 8, .9f);
                var root = new GameObject(title).transform; root.SetParent(t, false); root.localPosition = On(s, 0); root.localRotation = q;
                var grille = new GameObject("Grille").transform; grille.SetParent(root, false);
                float rise = high - .4f; var slide = Vector3.zero;
                switch (look)
                {
                    case "door":   // planks on end, iron-banded, dropped in the frame
                    {
                        int np = Mathf.Max(4, Mathf.CeilToInt(w * 2 / .3f));
                        for (int b = 0; b < np; b++) Part(PrimitiveType.Cube, grille, new Vector3(-w + (b + .5f) * w * 2 / np, high / 2, 0), new Vector3(w * 2 / np - .02f, high, .14f), b % 3 == 0 ? dark : timber);
                        foreach (float y in new[] { .5f, high * .5f, high - .5f }) Part(PrimitiveType.Cube, grille, new Vector3(0, y, -.09f), new Vector3(w * 2, .12f, .04f), iron);
                        break;
                    }
                    case "boards":   // sluice-boards: planks stacked edge on edge between the posts
                        for (float y = .15f; y < high; y += .32f) Part(PrimitiveType.Cube, grille, new Vector3(0, y, 0), new Vector3(w * 2, .3f, .12f), y % .96f < .32f ? dark : timber);
                        break;
                    case "bar":   // a heavy timber bar across the way at hip and chest height, in iron staples
                        foreach (float y in new[] { .9f, 1.6f }) Part(PrimitiveType.Cube, grille, new Vector3(0, y, 0), new Vector3(w * 2 + .3f, .26f, .26f), dark);
                        foreach (int side in new[] { -1, 1 }) foreach (float y in new[] { .9f, 1.6f }) Part(PrimitiveType.Cube, grille, new Vector3(side * (w - .1f), y, near * .16f), new Vector3(.12f, .36f, .08f), iron);
                        rise = Mathf.Min(rise, 2.2f);
                        break;
                    case "stockade":   // a pen's gate of stakes, sharpened
                    {
                        int ns = Mathf.Max(5, Mathf.CeilToInt(w * 2 / .24f));
                        for (int b = 0; b < ns; b++)
                        {
                            float x = -w + (b + .5f) * w * 2 / ns, tall = Mathf.Min(high, 2.6f) - (b % 2) * .15f;
                            Part(PrimitiveType.Cylinder, grille, new Vector3(x, tall / 2, 0), new Vector3(.16f, tall / 2, .16f), dark);
                            Part(PrimitiveType.Cube, grille, new Vector3(x, tall + .08f, 0), new Vector3(.11f, .2f, .11f), timber, Quaternion.Euler(0, 45, 0));
                        }
                        foreach (float y in new[] { .5f, 1.7f }) Part(PrimitiveType.Cube, grille, new Vector3(0, y, near * .1f), new Vector3(w * 2, .12f, .06f), timber);
                        break;
                    }
                    case "bookcase":   // shelves of books across the way, the face toward the side it is worked from; it slides into the rock
                    {
                        Part(PrimitiveType.Cube, grille, new Vector3(0, high / 2, 0), new Vector3(w * 2, high, .5f), dark);
                        for (int r = 0; r < 5 && .35f + r * .5f < high - .2f; r++)
                        {
                            Part(PrimitiveType.Cube, grille, new Vector3(0, .3f + r * .5f, near * .26f), new Vector3(w * 2 - .1f, .05f, .06f), timber);
                            for (float x = -w + .2f; x < w - .2f; x += .16f + R9() * .06f)
                                Part(PrimitiveType.Cube, grille, new Vector3(x, .5f + r * .5f, near * .22f), new Vector3(.07f + R9() * .04f, .26f + R9() * .08f, .2f), bindings[(int)(R9() * 3) % 3], Quaternion.Euler(0, 0, R9() * 6 - 3));
                        }
                        slide = new Vector3(w * 2 + .3f, 0, 0); rise = slide.magnitude;
                        break;
                    }
                    case "song":   // a slab of the old dark stone, lines of the goblins' tuning cut in it and faintly lit
                    {
                        Part(PrimitiveType.Cube, grille, new Vector3(0, high / 2, 0), new Vector3(w * 2, high, .4f), Tint(art.stone, new Color(.2f, .21f, .24f)));
                        var vein = Glowing(new Color(.6f, .8f, 1), 1.2f);
                        for (int k = 0; k < 7; k++) Part(PrimitiveType.Cube, grille, new Vector3(-w * .8f + k * w * .27f, high * (.3f + (k % 3) * .2f), near * .21f), new Vector3(.05f, high * (.3f + (k % 2) * .25f), .02f), vein);
                        break;
                    }
                    default:   // grille
                    {
                        int nb = Mathf.Max(7, Mathf.CeilToInt(w / .45f));   // a bar every 45 cm or so, however wide the way
                        for (int b = -nb; b <= nb; b++) Part(PrimitiveType.Cylinder, grille, new Vector3(b * w / (nb + .5f), high / 2, 0), new Vector3(.07f, high / 2, .07f), iron);
                        foreach (float y in new[] { .35f, high * .5f, high - .3f }) Part(PrimitiveType.Cube, grille, new Vector3(0, y, 0), new Vector3(w * 2, .1f, .08f), iron);
                        break;
                    }
                }
                var gate = root.gameObject.AddComponent<ZoneGate>(); gate.Title = title; gate.Cave = h.Name; gate.Along = s; gate.Lord = lord; gate.Grille = grille; gate.Rise = rise;
                gate.Kind = kind; gate.FromFar = far; gate.Slide = slide;
                gate.Init(new Vector3(h.Half[i] * 2 + 1, top, .5f));
                Gates.Add(gate);
                if (!string.IsNullOrEmpty(prompt)) Interactables.Add(new ZoneInteractable { name = title, prompt = prompt, kind = kind, position = t.TransformPoint(On(s + near * 1.3f, 0)), gate = gate });
                return gate;
            }
            // (The Adit rework) The gates the zone's data puts in this cave (ZoneDefinition.gates) and the code has not built already (the
            // winch gate and the platform gate are built where MainAdit dresses them, at the data's place when it gives one).
            void DataGates()
            {
                if (Zone.gates == null) return;
                foreach (var gd in Zone.gates)
                {
                    if (gd == null || gd.cave != h.Name || string.IsNullOrEmpty(gd.title) || Gates.Exists(g => g != null && g.Cave == h.Name && g.Title == gd.title)) continue;
                    var gate = Gate(gd.along, gd.title, string.IsNullOrEmpty(gd.kind) ? "bossdoor" : gd.kind, gd.prompt, gd.lord, gd.look, gd.side == "far");
                    gate.OpensOn = gd.opensOn;
                }
            }
            // A cave of a style with no dressing of its own yet (the Adit rework): lamps down its walls and a pale light high in its wide
            // chambers; the floors and the camps are filled after, as in every cave.
            // (The Adit rework) In the lamps' colour: pale in a plain cave, blue-green over the Sunless River's water.
            void PlainCave(Color lamp)
            {
                int no = 0;
                for (float s = 6; s < h.Length - 3; s += 14, no++) WallLamp(RingAt(s), no % 2 == 0 ? 1 : -1, lamp, 10, .8f);
                for (float s = 10; s < h.Length - 6; s += 30) { int i = RingAt(s); if (h.Half[i] >= 10) Glow(t, c[i] + Vector3.up * h.Height[i] * .75f, h.Half[i] * 2.2f, .8f, lamp, .8f); }
            }

            // ---------- the Adit rework's rooms (Docs/ADIT_REWORK.md section 3; GAME-ONLY; lessons only, no machines) ----------
            // Red lamps down the walls and embers in the rock: the Cult of Ash's caves, as the Ember Vent's.
            void EmberWalls()
            {
                var ember = Glowing(EmberRed, 1.8f); int no = 0;
                for (float s = 5; s < h.Length - 3; s += 14, no++) WallLamp(RingAt(s), no % 2 == 0 ? 1 : -1, EmberRed, 11, .95f);
                for (float s = 3; s < h.Length - 3; s += 3.3f)
                {
                    int i = RingAt(s); if (A() < .4f) continue; int side = A() < .5f ? 1 : -1; int k = side > 0 ? 1 + (int)(A() * 5) : P - 2 - (int)(A() * 5);
                    var at = ring[i, k]; var inward = c[i] + Vector3.up * (at.y - c[i].y) - at; inward.y = 0; inward.Normalize(); if (!Free(at + inward * .3f)) continue;
                    for (int q = 0; q < 3; q++) Part(PrimitiveType.Cube, t, at + inward * .04f + new Vector3((A() - .5f) * .8f, (A() - .5f) * .9f, (A() - .5f) * .8f), new Vector3(.06f, .4f + A() * .6f, .06f), ember, Quaternion.LookRotation(inward) * Quaternion.Euler(0, 0, A() * 180));
                }
            }
            // The room's walls this far off the way: where the cave is widest, short of its rock (the Rail Hall's 0.82 of the half-width).
            float RoomWidth(float most) { int wide = Widest(0, h.Length); return wide < 0 ? -1 : Mathf.Min(most, h.Half[wide] * .82f - .2f); }

            // The Geode Foundry (the Deadmines' lesson: enter high and work down round the edge): a hall of cut stone six courses (12 m) high
            // whose walls the catwalk and the walkway run along (the decks are the zone's data, ZoneBuilder.AditDecks), so no columns stand
            // before them; hand forges down the right-hand wall under the catwalk; the crack-furnace in the middle of the floor with its slag pit
            // beside it; and past the hall, the Rock-Eater's berth with racks of the cutters' tools. Violet light, the furnace's orange.
            void Foundry()
            {
                int no = 0;
                for (float s = 6; s < h.Length - 3; s += 14, no++) WallLamp(RingAt(s), no % 2 == 0 ? 1 : -1, GeodeViolet, 11, .9f);
                for (float s = 3; s < h.Length - 3; s += 3.9f) if (A() < .7f) Crystals(RingAt(s), A() < .5f ? 1 : -1, 3 + (int)(A() * 4));
                float w = RoomWidth(8 * CW); var hall = w > 0 ? Span(0, h.Length, w) : new Vector2(-1, -1);
                if (hall.x >= 0 && hall.y - hall.x >= 12)
                {
                    Hall(hall.x, hall.y, w, true, true, true, true, 9, null, 6, false);
                    for (float s = hall.x + 3; s < hall.y - 3; s += 5) Forge(s, 1, w);   // hand forges and their bellows, worked by hand
                    // The crack-furnace: a squat stone furnace with a glowing mouth toward the way.
                    int fi = AnchorRing("furnace"); float sf = fi >= 0 ? h.Along[fi] : (hall.x + hall.y) / 2;
                    if (Room(sf, 0, 2.6f, true))
                    {
                        var qf = Along(sf); var at = On(sf, 0); var furnaceStone = Tint(art.stone, new Color(.3f, .28f, .27f));
                        Part(PrimitiveType.Cube, t, at + Vector3.up * 1.5f, new Vector3(3.6f, 3, 3.6f), furnaceStone, qf);
                        Part(PrimitiveType.Cube, t, at + Vector3.up * 3.4f, new Vector3(2.6f, .8f, 2.6f), furnaceStone, qf);
                        Part(PrimitiveType.Cube, t, at + qf * new Vector3(0, 1.1f, -1.82f), new Vector3(1.4f, 1.2f, .06f), Glowing(new Color(1, .5f, .15f), 2), qf);   // its mouth
                        Glow(t, at + qf * new Vector3(0, 1.6f, -3f), 14, 1.3f, new Color(1, .55f, .2f), 1.3f).gameObject.AddComponent<Flicker>();
                        Block(at, new Vector3(3.7f, 3.8f, 3.7f), qf);
                    }
                    // The slag pit: a disc of cooling slag, glowing at its heart, a rim of black stone; nobody walks it.
                    int si = AnchorRing("slag pit"); var sp = AnchorIn(h, "slag pit"); float ss = si >= 0 ? h.Along[si] : sf - 12, sa = sp != null ? sp.aside : -6;
                    if (Room(ss, sa, 3.4f, true))
                    {
                        var qs = Along(ss); var at = On(ss, sa); at.y = FloorY(at.x, at.z);
                        Part(PrimitiveType.Cylinder, t, at + Vector3.up * .03f, new Vector3(6, .02f, 6), Glowing(new Color(1, .35f, .08f), .8f), qs);
                        Part(PrimitiveType.Cylinder, t, at + Vector3.up * .05f, new Vector3(3, .02f, 3), Glowing(new Color(1, .6f, .2f), .9f), qs);
                        var black = Tint(art.stone, new Color(.12f, .11f, .1f));
                        for (int k = 0; k < 20; k++) { float a = k / 20f * Mathf.PI * 2; Lump(BoulderAt(k), t, at + new Vector3(Mathf.Cos(a) * 3.15f, .05f, Mathf.Sin(a) * 3.15f), new Vector3(.8f, .35f, .7f), black, k * 18); }
                        Glow(t, at + Vector3.up * 2, 10, .9f, new Color(1, .45f, .15f), .9f).gameObject.AddComponent<Flicker>();
                        Block(at, new Vector3(5.6f, 1.2f, 5.6f), qs);
                    }
                }
                // The Rock-Eater's berth past the hall: racks of the cutters' tools down its walls, crates, scrap, a violet light (the beast
                // is the camp's: EncounterSession.RockEater).
                int berth = AnchorRing("berth");
                if (berth >= 0)
                {
                    float s0 = h.Along[berth], bw = h.Half[berth] * .8f;
                    for (float s = s0 - 12; s < s0 + 10; s += 3) foreach (int side in new[] { -1, 1 }) Rack(s, side, bw);
                    for (int k = 0; k < 4; k++) Crate(On(s0 - 9 + k * 1.3f, -bw * .55f), Along(s0), 1 + k % 2, k % 2 == 0);
                    for (int k = 0; k < 6; k++) Lump(BoulderAt(k), t, On(s0 + 6 + A() * 6, bw * (.2f + A() * .3f)) + Vector3.up * .1f, Vector3.one * (.25f + A() * .2f), rust, A() * 360);   // scrap
                    Glow(t, On(s0, 0) + Vector3.up * 7, 18 * CW, 1, new Color(.9f, .6f, 1), 1);
                }
            }

            // The Cult of Ash's chapter-house, a room to a cave (the Scarlet Monastery's lesson: wings you know from one look): the ash garden of
            // the order's dead (urns, graves, ash on the flags), the scriptorium (shelves, lecterns, reading desks) and the muster yard (racks
            // of weapons, ranks of dummies). The old chapter-house's pieces (ChapterHouse), each where its cave is widest.
            void ChapterRoom()
            {
                EmberWalls();
                float w = RoomWidth(6.5f * CW); if (w <= 0) return;
                var span = Span(0, h.Length, w); if (span.x < 0 || span.y - span.x < 6) return;
                switch (part)
                {
                    case "ashgarden":
                        Hall(span.x, span.y, w, true, true, false, false, 9, null, 3);   // the gatehouse arch in; open at its far end (the ways on leave from there)
                        for (float s = span.x + 1.2f; s < span.y - .8f; s += 2.4f) foreach (int side in new[] { -1, 1 }) Urn(s, side * (w - 1.2f));
                        for (float s = span.x + 1.2f; s < span.y - .8f; s += 4.8f) foreach (int side in new[] { -1, 1 }) Urn(s, side * (w - 3.4f));   // a second row of urns
                        for (float s = span.x + 2; s < span.y - 1.5f; s += 3.6f) foreach (int side in new[] { -1, 1 }) { Grave(s, side * w * .3f); Grave(s + 1.8f, side * w * .55f); }
                        for (int k = 0; k < 14; k++) { float s = span.x + 1 + R9() * (span.y - span.x - 2); Part(PrimitiveType.Cylinder, t, On(s, (R9() - .5f) * w * 1.4f) + Vector3.up * .01f, new Vector3(1.5f + R9() * 2, .01f, 1.5f + R9() * 2), ashGrey); }
                        break;
                    case "scriptorium":
                        Hall(span.x, span.y, w, true, true, true, true, 9, null, 3);
                        for (float s = span.x + 1; s < span.y - 1; s += 2.6f) foreach (int side in new[] { -1, 1 }) Shelf(s, side, w);
                        for (float s = span.x + 2.5f; s < span.y - 2; s += 4.5f) foreach (int side in new[] { -1, 1 }) Lectern(s, side * (w - 2.8f));
                        for (float s = span.x + 4; s < span.y - 3; s += 6) { Lectern(s, w * .32f); Lectern(s + 3, -w * .32f); }   // reading desks out on the floor
                        break;
                    case "musteryard":
                        Hall(span.x, span.y, w, true, true, true, true, 9, null, 3);
                        for (float s = span.x + 1; s < span.y - 1; s += 3) foreach (int side in new[] { -1, 1 }) Rack(s, side, w);
                        for (float s = span.x + 2.5f; s < span.y - 2; s += 3.5f) foreach (int side in new[] { -1, 1 }) { Dummy(s, side * (w - 3.6f)); Dummy(s + 1.7f, side * (w - 6.5f)); }   // two ranks of dummies either side
                        break;
                }
            }

            // The Chapel of Embers (the Scarlet Monastery's cathedral, as a lesson): a nave of cut stone four courses high round a lake of fire
            // with a way round both sides, pews before the lake and between it and the altar, the altar in the apse (ZoneAnchor "lake",
            // "altar"), braziers and banners, and the apse's end wall with a door in it (the cloister door, the zone's data) and the great window
            // over it with the fire behind. Past it the cloister stair runs home with its red lamps and embers only.
            void ChapelRoom()
            {
                EmberWalls();
                float cw = RoomWidth(6.5f * CW); if (cw <= 0) return;
                var nave = Span(0, h.Length, cw); if (nave.x < 0 || nave.y - nave.x < 12) return;
                int lake = AnchorRing("lake"), altarI = AnchorRing("altar");
                float altarAt = altarI >= 0 ? h.Along[altarI] : nave.y - 6, e = Mathf.Min(nave.y, altarAt + 5);
                float lakeAt = lake >= 0 ? h.Along[lake] : -1, rx = lake >= 0 ? h.Half[lake] * .42f : 0, rz = lake >= 0 ? Mathf.Min(8, h.Half[lake] * .45f) : 0;
                // The lake leaves a walkway 2.6 m wide either side of it, whatever the ring's width (its rim and its blocks spanned the whole
                // nave at x about 110: the cloister stair and the chapel's west half were cut off from the altar, the R91 picture).
                rz = Mathf.Min(rz, (cw - 2.6f) / 1.5f); rx = Mathf.Min(rx, rz * 1.8f);
                float lrx = rx + 1, lrz = rz + 1;
                Hall(nave.x, e, cw, true, true, true, true, 9, (s, a) => lake >= 0 && Mathf.Pow((s - lakeAt) / lrz, 2) + Mathf.Pow(a / lrx, 2) < 1, 4);
                var basalt = Tint(art.stone, new Color(.18f, .15f, .14f)); var cloth = Tint(art.cloth, new Color(.45f, .08f, .06f));
                if (lake >= 0)
                {
                    // The lake of fire mid-nave, a rim of black rock round it, nobody walks it; braziers and banners round it.
                    var q = Along(lakeAt); var mid = On(lakeAt, 0);
                    Part(PrimitiveType.Cylinder, t, mid + Vector3.up * .03f, new Vector3(rx * 2, .02f, rz * 2), Glowing(new Color(1, .32f, .04f), .7f), q);
                    Part(PrimitiveType.Cylinder, t, mid + Vector3.up * .05f, new Vector3(rx * 1.3f, .02f, rz * 1.3f), Glowing(new Color(1, .55f, .14f), .6f), q * Quaternion.Euler(0, 12, 0));
                    for (int k = 0; k < 48; k++)
                    {
                        float a = k / 48f * Mathf.PI * 2; var rim = mid + q * new Vector3(Mathf.Cos(a) * rx * 1.04f, .05f, Mathf.Sin(a) * rz * 1.04f);
                        Lump(BoulderAt(k % 40), t, rim, new Vector3(1.2f + A() * 1f, .45f + A() * .35f, 1f + A() * .8f), basalt, A() * 360);
                    }
                    Block(mid, new Vector3(rx * 1.9f, 1.2f, rz * 1.0f), q); Block(mid, new Vector3(rx * 1.2f, 1.2f, rz * 1.5f), q);   // within the walkway's edge (rz * 1.5 at most)
                    for (int k = -1; k <= 1; k++) Glow(t, mid + q * new Vector3(0, 3.5f, k * rz * .6f), 14 * CW, 1.1f, new Color(1, .45f, .12f), 1.1f).gameObject.AddComponent<Flicker>();
                    for (int k = 0; k < 4; k++)
                    {
                        float s = lakeAt - rz + 1 + k * (rz * 2 - 2) / 3; int side = k % 2 == 0 ? 1 : -1; float ba = side * (cw - 1.6f); var b = On(s, ba);
                        if (!Room(s, ba, .5f)) continue; Brazier(t, b, null); Block(b, new Vector3(.8f, 1.2f, .8f), Quaternion.identity);
                        Banner(t, On(s + 1.2f, side * (cw - .45f)), Along(s).eulerAngles.y, cloth, dark, 6.4f);
                    }
                }
                // Two blocks of pews either side of the carpet, before the lake and between it and the altar, room to cross round the lake.
                for (float s = nave.x + 3; s < altarAt - 4; s += 1.3f)
                {
                    if (lake >= 0 && Mathf.Abs(s - lakeAt) < rz + 3) continue;
                    foreach (int side in new[] { -1, 1 }) { Pew(s, side * 3.4f, 3.2f); Pew(s, side * 7.6f, 3.2f); }
                }
                // The altar, the carpet up to it, a tall candelabrum either side.
                {
                    var qa = Along(altarAt); var altar = On(altarAt, 0);
                    if (Room(altarAt, 0, 1.4f, true))
                    {
                        Part(PrimitiveType.Cube, t, altar + Vector3.up * .5f, new Vector3(2.4f, 1, 1.2f), basalt, qa);
                        Part(PrimitiveType.Cube, t, altar + Vector3.up * 1.03f, new Vector3(2.6f, .08f, 1.4f), Tint(art.cloth, new Color(.3f, .06f, .05f)), qa);
                        for (int g = 0; g < 3; g++) Part(PrimitiveType.Sphere, t, altar + qa * new Vector3(-.6f + g * .6f, 1.15f, 0), new Vector3(.2f, .14f, .18f), Glowing(new Color(.55f, .3f, .8f), .6f));   // geodes set out for the fire, gone dim
                        Lamp(altar + qa * new Vector3(0, 1.4f, -.5f), EmberRed, 10, 1.2f, .1f);
                        Block(altar, new Vector3(2.5f, 1.1f, 1.3f), qa);
                    }
                    Piece("Carpet", On(altarAt - 4.5f, 0) + Vector3.up * .01f, YawAt(altarAt), .06f, null, new Vector3(2.4f, .06f, 6.6f));
                    foreach (int side in new[] { -1, 1 }) { var cd = On(altarAt, side * 2.1f); if (Free(cd)) { Piece("Candelabrum_tall", cd, YawAt(altarAt), 1.9f); Block(cd, new Vector3(.6f, 1.9f, .6f), qa); } }
                }
                // The great window over the cloister door in the apse's wall: a pane of the fire behind it in a dark stone frame, banners either side.
                {
                    var q = Along(e); var win = On(e - .45f, 0) + Vector3.up * 6.1f; var frame = Tint(art.stone, new Color(.16f, .14f, .13f));
                    Part(PrimitiveType.Cube, t, win, new Vector3(2.6f, 3.2f, .04f), Glowing(new Color(1, .42f, .12f), 1.2f), q);
                    foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, win + q * new Vector3(side * 1.5f, 0, -.05f), new Vector3(.4f, 3.7f, .25f), frame, q);
                    foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, win + q * new Vector3(0, side * 1.8f, -.05f), new Vector3(3.4f, .4f, .25f), frame, q);
                    Part(PrimitiveType.Cube, t, win + q * new Vector3(0, 0, -.06f), new Vector3(.16f, 3.2f, .12f), frame, q);
                    Glow(t, win + q * new Vector3(0, 0, -1.5f), 10, .9f, new Color(1, .5f, .2f), .9f).gameObject.AddComponent<Flicker>();
                    foreach (int side in new[] { -1, 1 }) Banner(t, On(e - 1, side * (cw - .6f)), YawAt(e), cloth, dark, 6.4f);
                }
            }

            void RailHall(float from)
            {
                hallSpans.Add(new Vector2(from - 6, h.Length + 1));   // the platform and the line: the escort's ground
                float to = h.Length - 3 * X, line = -h.Half[Widest(from, to)] * .55f;
                // The line: down the hall's left side, buffers at the far end, and a dark mouth in the end wall where it runs on south.
                Rails(from + 1, to - 1, line, .7f);
                var qEnd = Along(to - 2); var buffer = On(to - 2.2f, line);
                Part(PrimitiveType.Cube, t, buffer + Vector3.up * .5f, new Vector3(2.2f, 1, .5f), dark, qEnd); Block(buffer, new Vector3(2.2f, 1, .5f), qEnd);
                foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, t, buffer + qEnd * new Vector3(side * .7f, .6f, -.35f), new Vector3(.3f, .2f, .3f), iron, qEnd * Quaternion.Euler(90, 0, 0));
                Part(PrimitiveType.Sphere, t, On(to + 2, line) + Vector3.up * 4.8f, new Vector3(8, 10, .6f), Tint(art.stone, new Color(.02f, .02f, .025f)), qEnd);
                // The platform: cut-stone tiles from the kerb to the right-hand wall (flags without the kit), the kerb on the line's side.
                float wallAt = .82f;   // the hall's walls stand this far out of its half-width (clear of the rock's bulges)
                bool tiled = false;
                for (float s = from + 4 * X; s < to - 4 * X; s += 2)
                {
                    int i = RingAt(s); var q = Along(s);
                    for (float a = line * .6f + 1.2f; a < h.Half[i] * wallAt - 1.2f; a += 2)
                    {
                        var at = On(s, a); if (!Free(at)) continue;
                        if (Piece("ModularFloor", at + Vector3.down * .33f, YawAt(s), .36f) != null) tiled = true;
                    }
                    if (Mathf.Repeat(s - from - 4, 3) < 2) Part(PrimitiveType.Cube, t, On(s, line * .6f) + Vector3.up * .1f, new Vector3(.3f, .2f, 3), Tint(art.stone, new Color(.3f, .29f, .28f)), q);
                }
                if (!tiled)
                    for (float s = from + 4 * X; s < to - 4 * X; s += 3)
                        Part(PrimitiveType.Cube, t, On(s, line * -.35f) + Vector3.up * .025f, new Vector3(-line * 1.4f, .05f, 3), Tint(art.stone, new Color(.36f, .35f, .34f)), Along(s));
                // The hall's walls: two courses of cut stone down both sides (2 m modules, 4 m high), a column every 6 m before them, and
                // a torch between each pair of columns, every other one lit; the rock above. Without the kit, the old lamps on iron posts.
                bool walled = false;
                for (float s = from + 3 * X; s < to - 3 * X; s += 2)
                    foreach (int side in new[] { -1, 1 })
                    {
                        int i = RingAt(s); var foot = On(s, side * h.Half[i] * wallAt); if (!Free(foot) || !Free(foot + Vector3.up * 3.5f)) continue;
                        float yaw = YawAt(s) + (side > 0 ? 0 : 180);   // the dressed face into the hall (as the halls')
                        if (Piece("ModularStoneWall", foot, yaw, 2.02f) == null) continue;
                        Piece("ModularStoneWall", foot + Vector3.up * 2, yaw, 2.02f); Piece("ModularStoneWall", foot + Vector3.up * 4, yaw, 2.02f); walled = true;   // three courses: the hall is twice as tall
                        Block(foot, new Vector3(.7f, 6, 2.05f), Along(s));
                    }
                int k2 = 0;
                if (walled)
                    for (float s = from + 4 * X; s < to - 3 * X; s += 9, k2++)
                        foreach (int side in new[] { -1, 1 })
                        {
                            int i = RingAt(s); var foot = On(s, side * (h.Half[i] * wallAt - 1.6f)); if (!Free(foot)) continue;
                            Piece("Column", foot, YawAt(s), 6.4f); Block(foot, new Vector3(2.2f, 6.4f, 2.2f), Along(s));
                            float st = s + 4.5f; if (st > to - 3 * X) continue; int it = RingAt(st);
                            var face = On(st, side * (h.Half[it] * wallAt - .36f));
                            Torch(face, On(st, 0) - face, (k2 + (side > 0 ? 1 : 0)) % 2 == 0);
                        }
                else
                    for (float s = from + 4 * X; s < to - 2 * X; s += 10, k2++)
                        foreach (int side in new[] { -1, 1 })
                        {
                            int i = RingAt(s); var foot = On(s, side * (h.Half[i] - 2)); if (!Free(foot)) continue;
                            Part(PrimitiveType.Cylinder, t, foot + Vector3.up * 1.6f, new Vector3(.12f, 1.6f, .12f), iron);
                            Lamp(foot + Vector3.up * 3.3f, RailBlue, 12, 1.1f, .2f);
                            Block(foot, new Vector3(.35f, 3.2f, .35f), Quaternion.identity);
                        }
                // Barrels stood on the platform against the right-hand wall.
                if (walled) for (int k = 0; k < 3; k++) { float s = from + 6 * X + k * 1.2f; var b = On(s, h.Half[RingAt(s)] * wallAt - 1.2f); if (Free(b)) { Piece("Barrel", b, K() * 360, .95f); Block(b, new Vector3(.9f, .95f, .9f), Along(s)); } }
                for (float s = from + 8 * X; s < to; s += 30) { int i = RingAt(s); Glow(t, c[i] + Vector3.up * h.Height[i] * .7f, 30 * CW, .7f, RailBlue, .7f); }
                // The carriage (Chris, 2026-10-11: "NO Machines"; it had been an armoured rail-car with a cab, rivets and levers): two timber
                // ore wagons coupled on the old rails, plank-sided and iron-banded on wooden wheels, the geodes in their crates in the rear one,
                // a driver's bench up front, and the traces out to a pair of pit mules standing up the line (RailTeam).
                // The carriage stands where Rail-Captain Danner keeps, 14 m short of his camp as in the first Adit (to - 16 there): drawn
                // out, "to - 16 * X" put it 38 m from him, past the reach of a mob's leash, and he never came for the Weaver at the lock.
                var danner = Zone.camps == null ? null : System.Array.Find(Zone.camps, cp => cp != null && cp.cave == h.Name && cp.tag == "railcaptain");
                float sc = danner != null ? Mathf.Min(danner.along - 14, to - 12) : to - 16 * X; var qc = Along(sc); var car = On(sc, line);
                var boards = Tint(art.timber, new Color(.46f, .33f, .21f));
                void Wagon(float wz0, float wz1)
                {
                    float wzc = (wz0 + wz1) / 2, wlen = wz1 - wz0;
                    Part(PrimitiveType.Cube, t, car + qc * new Vector3(0, .75f, wzc), new Vector3(2.6f, .12f, wlen), boards, qc);   // the floor
                    foreach (int wside in new[] { -1, 1 })
                    {
                        Part(PrimitiveType.Cube, t, car + qc * new Vector3(wside * 1.25f, 1.2f, wzc), new Vector3(.1f, .8f, wlen), boards, qc);   // the sides
                        for (float wz = wz0 + .3f; wz < wz1; wz += 1.5f) Part(PrimitiveType.Cube, t, car + qc * new Vector3(wside * 1.31f, 1.2f, wz), new Vector3(.04f, .84f, .1f), dark, qc);   // iron bands
                        foreach (float wz in new[] { wz0 + .8f, wz1 - .8f })
                        {
                            Part(PrimitiveType.Cylinder, t, car + qc * new Vector3(wside * 1.05f, .45f, wz), new Vector3(.8f, .06f, .8f), boards, qc * Quaternion.Euler(0, 0, 90));   // a wooden wheel
                            Part(PrimitiveType.Cylinder, t, car + qc * new Vector3(wside * 1.09f, .45f, wz), new Vector3(.84f, .02f, .84f), dark, qc * Quaternion.Euler(0, 0, 90));   // its iron tyre
                        }
                    }
                    foreach (float wz in new[] { wz0, wz1 }) Part(PrimitiveType.Cube, t, car + qc * new Vector3(0, 1.2f, wz), new Vector3(2.6f, .8f, .1f), boards, qc);   // the ends
                }
                Wagon(-.6f, 5.2f); Wagon(-7.2f, -1.2f);
                Part(PrimitiveType.Cube, t, car + qc * new Vector3(0, .6f, -.9f), new Vector3(.12f, .08f, .8f), dark, qc);   // the coupling
                Part(PrimitiveType.Cube, t, car + qc * new Vector3(0, 1.75f, -6.8f), new Vector3(2, .1f, .5f), boards, qc);   // the driver's bench
                foreach (int tside in new[] { -1, 1 })   // the traces, out to the mules
                {
                    Part(PrimitiveType.Cube, t, car + qc * new Vector3(tside * .55f, .95f, -9.4f), new Vector3(.08f, .08f, 4.4f), boards, qc);
                    Part(PrimitiveType.Cylinder, t, car + qc * new Vector3(tside * .65f, 1.05f, -11.5f), new Vector3(.02f, .9f, .02f), straw, qc * Quaternion.Euler(90, 0, 0));
                }
                RailTeam = new[] { t.TransformPoint(car + qc * new Vector3(-.65f, 0, -12.6f)), t.TransformPoint(car + qc * new Vector3(.65f, 0, -12.6f)) };
                RailTeamFacing = t.rotation * qc * Quaternion.Euler(0, 180, 0);
                var lit = new System.Collections.Generic.List<GameObject>();
                for (int k = 0; k < 6; k++) Crate(car + qc * new Vector3((k % 2 - .5f) * 1.1f, .85f, -2.4f - (k / 2) * 1.1f), qc, 1 + (k + 1) % 2, true, lit);
                lit.Add(Glow(t, car + qc * new Vector3(0, 2.2f, -3.4f), 8, .9f, GeodeViolet, .9f).gameObject);
                // The resonance lock (D4): an iron plate on the front wagon's platform side with a violet eye, and where the Weaver stands to match it.
                // The waves come from behind the carriage and from the far end, where the line runs on into the dark. Fixed places, no draws.
                Part(PrimitiveType.Cylinder, t, car + qc * new Vector3(1.33f, 1.3f, 1.2f), new Vector3(.75f, .03f, .75f), iron, qc * Quaternion.Euler(0, 0, 90));
                foreach (float r in new[] { .5f, .3f }) Part(PrimitiveType.Cylinder, t, car + qc * new Vector3(1.36f, 1.3f, 1.2f), new Vector3(r, .02f, r), rust, qc * Quaternion.Euler(0, 0, 90));
                lit.Add(Part(PrimitiveType.Sphere, t, car + qc * new Vector3(1.39f, 1.3f, 1.2f), new Vector3(.06f, .16f, .16f), geode, qc));
                lit.Add(Glow(t, car + qc * new Vector3(2.1f, 1.6f, 1.2f), 4, .7f, GeodeViolet, .7f).gameObject);
                RailGeodes = lit.ToArray(); RailLock = t.TransformPoint(On(sc + 1.2f, line + 2.6f));
                // The second wave out of the dark toward the far end, 14 m past the carriage (the far end itself is 33 m past it now: they came
                // too late to matter), by the line where the hall has narrowed (drawn out, the line runs 23 m off the way: there it is in the rock).
                float s2 = Mathf.Min(to - 4.5f * X, sc + 14);   // 13 m from the lock when it comes (AditWeaver.Wave2At), she is then some 26 m from them: inside the leash's 30
                RailWaves = new[] { t.TransformPoint(On(sc - 9.5f, line + 1)), t.TransformPoint(On(s2, Mathf.Max(line + 2, -h.Half[RingAt(s2)] * .5f))) };
                // The way out (D8): the step up at the carriage's side, on the platform side. With the lock matched the goblins take the mules'
                // heads and walk the carriage back up the line to a siding under the yard (EncounterSession.Adit, kind "carriage").
                Interactables.Add(new ZoneInteractable { name = AditCarriage, prompt = "Ride the carriage up the line", kind = "carriage", position = t.TransformPoint(On(sc - 3f, line + 2.4f)) });
                Block(car + qc * new Vector3(0, 0, 2.2f), new Vector3(2.9f, 2.2f, 6.2f), qc);
                Block(car + qc * new Vector3(0, 0, -3.8f), new Vector3(2.9f, 1, 4.6f), qc);
                // Crates and barrels on the platform (the loading crane is gone: no machines). Its spot's check is kept, so nothing else moves.
                var crane = On(sc - 7, line * .15f); Free(crane);
                for (int k = 0; k < 8; k++)
                {
                    float s = from + 8 * X + k * 5.5f * X; if (s > sc - 9 * X) break;
                    Crate(On(s, -line * (.5f + A() * .4f)), Along(s), 1 + (int)(A() * 2), A() < .4f);
                }
                for (int k = 0; k < 5; k++) { A(); A(); A(); }   // (the steam is gone: it read as white blobs; its draws are kept)
            }

            void GeodeFloor()
            {
                int cut = Widest(18 * X, 40 * X), shop = Widest(48 * X, h.Length - 3 * X);
                int no = 0;
                for (float s = 6; s < h.Length - 3; s += 14, no++) WallLamp(RingAt(s), no % 2 == 0 ? 1 : -1, GeodeViolet, 11, .9f);
                for (float s = 3; s < h.Length - 3; s += 3.9f) if (A() < .7f) Crystals(RingAt(s), A() < .5f ? 1 : -1, 3 + (int)(A() * 4));
                if (cut >= 0)
                {
                    float s0 = h.Along[cut]; var q = Along(s0);
                    for (int k = 0; k < 3; k++)
                    {
                        var bench = On(s0 - 4 * X + k * 4 * X, (k % 2 == 0 ? 1 : -1) * h.Half[cut] * .5f); if (!Free(bench)) continue;
                        taken.Add(new Vector3(s0 - 4 * X + k * 4 * X, (k % 2 == 0 ? 1 : -1) * h.Half[cut] * .5f, .9f));
                        Part(PrimitiveType.Cube, t, bench + Vector3.up * .45f, new Vector3(1.6f, .9f, .9f), timber, q);
                        Part(PrimitiveType.Cylinder, t, bench + q * new Vector3(.4f, 1.2f, 0), new Vector3(.6f, .06f, .6f), stone, q * Quaternion.Euler(0, 0, 90));   // the cutting wheel
                        for (int g = 0; g < 3; g++) Part(PrimitiveType.Sphere, t, bench + q * new Vector3(-.4f + g * .2f, .97f, (A() - .5f) * .4f), new Vector3(.18f, .12f, .16f), geode);
                        Block(bench, new Vector3(1.7f, 1, 1), q);
                    }
                    var trough = On(s0 + 4 * X, -h.Half[cut] * .55f);
                    if (Free(trough))
                    {
                        taken.Add(new Vector3(s0 + 4 * X, -h.Half[cut] * .55f, 1.8f));
                        Part(PrimitiveType.Cube, t, trough + Vector3.up * .35f, new Vector3(1, .7f, 3.4f), dark, q);
                        for (int g = 0; g < 9; g++) Part(PrimitiveType.Sphere, t, trough + q * new Vector3((A() - .5f) * .7f, .66f, (A() - .5f) * 3), new Vector3(.18f, .1f, .16f), geode);
                        Block(trough, new Vector3(1.1f, .8f, 3.5f), q);
                    }
                    Glow(t, On(s0, 0) + Vector3.up * 7, 16 * CW, 1, GeodeViolet, 1);
                }
                if (shop >= 0)
                {
                    // The Rock-Eater, the goblin boring-engine (GAME-ONLY), is no longer a prop here: it is the camp's machine, which Nix
                    // rides (EncounterSession.RockEater, ActorLook.RockEater; D5). The workshop keeps its crates, scrap and light.
                    float s0 = h.Along[shop] + 3 * X; var q = Along(s0);
                    for (int k = 0; k < 4; k++) { Crate(On(s0 - 7 * X + k * 1.3f, -h.Half[shop] * .6f), q, 1 + k % 2, k % 2 == 0); taken.Add(new Vector3(s0 - 7 * X + k * 1.3f, -h.Half[shop] * .6f, .6f)); }
                    for (int k = 0; k < 6; k++) Lump(BoulderAt(k), t, On(s0 - 3 * X + A() * 6 * X, -h.Half[shop] * (.2f + A() * .3f)) + Vector3.up * .1f, Vector3.one * (.25f + A() * .2f), rust, A() * 360);   // scrap
                    Glow(t, On(s0, 0) + Vector3.up * 8, 18 * CW, 1, new Color(.9f, .6f, 1), 1);
                }
                // D9, the Deadmines' part made of stone: the cutting hall is the goblins' foundry (a hall with a door at either end, a
                // catwalk along its left wall over the benches, forges down its right), the workshop their Rock-Eater's berth (a hall
                // the rock closes at the far end, the line laid into it, a gantry over the Rock-Eater's berth, racks of tools on the right).
                float fw = 6 * CW, bw = 7 * CW;   // the foundry's and the bay's walls off the way
                var foundry = Span(20 * X, 42 * X, fw);
                Hall(foundry.x, foundry.y, fw, true, true, true, true, 9, null, 3);
                if (foundry.x >= 0)
                {
                    Catwalk(foundry.x + 1.5f, foundry.y - 1.5f, -1, fw);
                    for (float s = foundry.x + 2; s < foundry.y - 1.5f; s += 2.5f) Forge(s, 1, fw);
                    for (float s = foundry.x + 3; s < foundry.y - 3; s += 5) Forge(s, -1, fw * .55f);   // a second row of hearths in the middle of the floor
                }
                var bay = Span(48 * X, 72 * X, bw);
                Hall(bay.x, bay.y, bw, true, true, false, false, 9, null, 3);   // the far end is the rock's (a wall there shut off the cave's last metres)
                if (bay.x >= 0)
                {
                    Rails(bay.x + 1, bay.y - 1.5f, 0, .7f);
                    int nix = Widest(60 * X, 70 * X); if (nix >= 0) Gantry(Mathf.Min(h.Along[nix] + 3 * X, bay.y - 2), bw);
                    for (float s = bay.x + 2; s < bay.y - 1; s += 3) Rack(s, 1, bw);
                }
            }

            void EmberVent()
            {
                int lake = Widest(26 * X, h.Length - 8 * X);
                var ember = Glowing(EmberRed, 1.8f); var basalt = Tint(art.stone, new Color(.18f, .15f, .14f));
                int no = 0;
                for (float s = 5; s < h.Length - 3; s += 14, no++) WallLamp(RingAt(s), no % 2 == 0 ? 1 : -1, EmberRed, 11, .95f);
                // Embers in the walls: glowing cracks, thicker toward the hall.
                for (float s = 3; s < h.Length - 3; s += 3.3f)
                {
                    int i = RingAt(s); if (A() < .4f) continue; int side = A() < .5f ? 1 : -1; int k = side > 0 ? 1 + (int)(A() * 5) : P - 2 - (int)(A() * 5);
                    var at = ring[i, k]; var inward = c[i] + Vector3.up * (at.y - c[i].y) - at; inward.y = 0; inward.Normalize(); if (!Free(at + inward * .3f)) continue;
                    for (int q = 0; q < 3; q++) Part(PrimitiveType.Cube, t, at + inward * .04f + new Vector3((A() - .5f) * .8f, (A() - .5f) * .9f, (A() - .5f) * .8f), new Vector3(.06f, .4f + A() * .6f, .06f), ember, Quaternion.LookRotation(inward) * Quaternion.Euler(0, 0, A() * 180));
                }
                if (lake >= 0)
                {
                    // The lake of molten rock in the middle of the great hall, a rim of black rock round it, the way round on both sides.
                    float s0 = h.Along[lake]; var q = Along(s0); var mid = On(s0, 0);
                    float rx = h.Half[lake] * .42f, rz = LakeRz(lake);
                    // (2026-10-10: at 2.6 and 2.2 the lake burned out to a white blob in every shot of the hall; molten orange now)
                    Part(PrimitiveType.Cylinder, t, mid + Vector3.up * .03f, new Vector3(rx * 2, .02f, rz * 2), Glowing(new Color(1, .32f, .04f), .7f), q);
                    Part(PrimitiveType.Cylinder, t, mid + Vector3.up * .05f, new Vector3(rx * 1.3f, .02f, rz * 1.3f), Glowing(new Color(1, .55f, .14f), .6f), q * Quaternion.Euler(0, 12, 0));   // the brighter heart of it
                    for (int k = 0; k < 80; k++)
                    {
                        float a = k / 80f * Mathf.PI * 2; var rim = mid + q * new Vector3(Mathf.Cos(a) * rx * 1.04f, .05f, Mathf.Sin(a) * rz * 1.04f);
                        Lump(BoulderAt(k % 40), t, rim, new Vector3(1.4f + A() * 1.2f, .5f + A() * .4f, 1.2f + A() * 1f), basalt, A() * 360);
                    }
                    Block(mid, new Vector3(rx * 1.9f, 1.2f, rz * 1.1f), q); Block(mid, new Vector3(rx * 1.1f, 1.2f, rz * 1.9f), q);   // nobody walks the lake (the knockback into it is a later step)
                    for (int k = -1; k <= 1; k++) Glow(t, mid + q * new Vector3(0, 3.5f, k * rz * .6f), 18 * CW, 1.2f, new Color(1, .45f, .12f), 1.2f).gameObject.AddComponent<Flicker>();
                    // The cell's things: braziers round the hall, red banners, the altar at the far end where the Cinder-Warden keeps.
                    var cloth = Tint(art.cloth, new Color(.45f, .08f, .06f));
                    for (int k = 0; k < 4; k++)
                    {
                        float s = s0 - rz + 2 + k * rz * .6f; int side = k % 2 == 0 ? 1 : -1; float ba = side * Mathf.Min(h.Half[RingAt(s)] - 1.6f, NaveW - 1.6f); var b = On(s, ba);
                        if (!Free(b)) continue; Brazier(t, b, null); Block(b, new Vector3(.8f, 1.2f, .8f), Quaternion.identity); taken.Add(new Vector3(s, ba, .5f));
                        Banner(t, On(s + 1.2f, side * Mathf.Min(h.Half[RingAt(s)] - 1, NaveW - .45f)), Along(s).eulerAngles.y, cloth, dark, 6.4f);
                    }
                    float sa = h.Length - 8 * X; var qa = Along(sa); var altar = On(sa, 0);
                    if (Free(altar))
                    {
                        Part(PrimitiveType.Cube, t, altar + Vector3.up * .5f, new Vector3(2.4f, 1, 1.2f), basalt, qa);
                        Part(PrimitiveType.Cube, t, altar + Vector3.up * 1.03f, new Vector3(2.6f, .08f, 1.4f), Tint(art.cloth, new Color(.3f, .06f, .05f)), qa);
                        for (int g = 0; g < 3; g++) Part(PrimitiveType.Sphere, t, altar + qa * new Vector3(-.6f + g * .6f, 1.15f, 0), new Vector3(.2f, .14f, .18f), Glowing(new Color(.55f, .3f, .8f), .6f));   // geodes set out for the fire, gone dim
                        Lamp(altar + qa * new Vector3(0, 1.4f, -.5f), EmberRed, 10, 1.2f, .1f);
                        Block(altar, new Vector3(2.5f, 1.1f, 1.3f), qa);
                        taken.Add(new Vector3(sa, 0, 1.4f));
                    }
                }
                ChapterHouse(lake >= 0 ? h.Along[lake] : -1);
            }
            // D9, Scarlet Monastery's part: the Cult of Ash's chapter-house built in the vent. The vestibule is the ash-garden of the
            // order's dead (urns along its walls, graves either side of the way); the nave round the lake has the scriptorium down its
            // left (bookcases, lecterns) and the drill yard down its right (weapon racks, training dummies); the chapel at the far end
            // has pews before the altar and a great window in the end wall with the fire behind it.
            void ChapterHouse(float lakeAt)
            {
                int li = lakeAt > 0 ? RingAt(lakeAt) : -1; float rz0 = li >= 0 ? LakeRz(li) : 0;
                float vw = 6.5f * CW; var vest = Span(24 * X, 40 * X, vw); float v1 = lakeAt > 0 ? Mathf.Min(vest.y, lakeAt - rz0 - .2f) : vest.y;
                Hall(vest.x, v1, vw, true, true, false, false, 9, null, 3);   // open into the nave: a wall there left no way round the lake's near end
                if (vest.x >= 0 && v1 - vest.x >= 6)
                {
                    for (float s = vest.x + 1.2f; s < v1 - .8f; s += 2.4f) foreach (int side in new[] { -1, 1 }) Urn(s, side * (vw - 1.2f));
                    for (float s = vest.x + 1.2f; s < v1 - .8f; s += 4.8f) foreach (int side in new[] { -1, 1 }) Urn(s, side * (vw - 3.4f));   // a second row of urns
                    for (float s = vest.x + 2; s < v1 - 1.5f; s += 3.6f) foreach (int side in new[] { -1, 1 }) { Grave(s, side * vw * .3f); Grave(s + 1.8f, side * vw * .55f); }
                    for (int k = 0; k < 14; k++) { float s = vest.x + 1 + R9() * (v1 - vest.x - 2); Part(PrimitiveType.Cylinder, t, On(s, (R9() - .5f) * vw * 1.4f) + Vector3.up * .01f, new Vector3(1.5f + R9() * 2, .01f, 1.5f + R9() * 2), ashGrey); }
                }
                var nave = Span(lakeAt > 0 ? lakeAt - rz0 : 30 * X, h.Length - 6 * X, NaveW);
                float lrx = li >= 0 ? h.Half[li] * .42f + 1 : 0, lrz = rz0 + 1;
                Hall(nave.x, nave.y, NaveW, false, false, false, false, 9, (s, a) => li >= 0 && Mathf.Pow((s - lakeAt) / lrz, 2) + Mathf.Pow(a / lrx, 2) < 1, 3);
                if (nave.x >= 0)
                {
                    for (float s = nave.x + 1; s < nave.y - 1; s += 2.6f) Shelf(s, -1, NaveW);
                    for (float s = nave.x + 2.5f; s < nave.y - 2; s += 4.5f) Lectern(s, -(NaveW - 2.8f));
                    for (float s = nave.x + 1; s < nave.y - 1; s += 3) Rack(s, 1, NaveW);
                    for (float s = nave.x + 2.5f; s < nave.y - 2; s += 3.5f) { Dummy(s, NaveW - 3.6f); Dummy(s + 1.7f, NaveW - 6.5f); }   // two ranks of dummies
                    for (float s = nave.x + 4; s < nave.y - 3; s += 6) { Lectern(s, -(NaveW - 6.5f)); Shelf(s + 3, -1, NaveW - 7.5f); }   // reading desks and a second run of shelves out on the floor
                }
                float cw = 6.5f * CW; float c0 = Mathf.Max(nave.y > 0 ? nave.y : 0, lakeAt > 0 ? lakeAt + rz0 + .4f : 0); var chapel = Span(c0, h.Length - 3, cw);
                if (chapel.x >= 0 && chapel.y - chapel.x >= 4)
                {
                    float e = Mathf.Max(chapel.y, h.Length - 3.3f);   // the window wall at the cave's very end, behind all its floor
                    // Two blocks of pews either side of the carpet, before the altar only (they had run on past it, round the Warden's place, and
                    // shut her in), with room to cross between the lake's end and the first row.
                    float altarAt = h.Length - 8 * X;
                    for (float s = chapel.x + 3; s < altarAt - 4; s += 1.3f) foreach (int side in new[] { -1, 1 }) { Pew(s, side * 3.4f, 3.2f); Pew(s, side * 7.6f, 3.2f); }
                    EndWall(e, cw, false, 4);
                    // The great window: a tall pane of the fire behind the wall in a dark stone frame, a cross of mullions over it (built,
                    // not the kit's window, which did not read against the lit wall). On the wall's face, which stands .35 short of e.
                    var q = Along(e); var win = On(e - .45f, 0) + Vector3.up * 4.4f; var frame = Tint(art.stone, new Color(.16f, .14f, .13f));
                    Part(PrimitiveType.Cube, t, win, new Vector3(3, 5.2f, .04f), Glowing(new Color(1, .42f, .12f), 1.2f), q);
                    foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, win + q * new Vector3(side * 1.7f, 0, -.05f), new Vector3(.45f, 5.8f, .25f), frame, q);
                    foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, win + q * new Vector3(0, side * 2.8f, -.05f), new Vector3(3.9f, .45f, .25f), frame, q);
                    Part(PrimitiveType.Cube, t, win + q * new Vector3(0, 0, -.06f), new Vector3(.18f, 5.2f, .12f), frame, q);
                    Part(PrimitiveType.Cube, t, win + q * new Vector3(0, .7f, -.06f), new Vector3(3, .18f, .12f), frame, q);
                    Glow(t, win + q * new Vector3(0, 0, -1.5f), 10, .9f, new Color(1, .5f, .2f), .9f).gameObject.AddComponent<Flicker>();
                    foreach (int side in new[] { -1, 1 }) Banner(t, On(e - 1, side * (cw - .6f)), YawAt(e), Tint(art.cloth, new Color(.45f, .08f, .06f)), dark, 6.4f);
                }
            }

            void GreyBreach()
            {
                int foreman = Widest(46 * X, h.Length - 3 * X);
                var unmade = Tint(art.stone, new Color(.34f, .35f, .37f)); var hole = Tint(art.stone, new Color(.015f, .015f, .02f));   // the grey: dull rock gone colourless, not white cards (2026-10-08)
                // Little light: a pale glow every twelve metres, and the old miners' things left where they dropped them.
                int no = 0;
                for (float s = 6; s < h.Length - 3; s += 20, no++) WallLamp(RingAt(s), no % 2 == 0 ? 1 : -1, GreyLight, 10, .5f);
                Cart(Mathf.Min(16 * X, h.Length * .25f), .8f * DW, true);
                for (int k = 0; k < 2; k++)
                {
                    float s = 12 * X + k * 22 * X; if (s > h.Length - 6 * X) break; var q = Along(s); var l = On(s, -h.Half[RingAt(s)] * .7f);
                    if (Free(l)) Part(PrimitiveType.Cube, t, l + Vector3.up * .25f, new Vector3(.24f, .24f, 2.6f), timber, q * Quaternion.Euler(0, 30, 80));   // a fallen post
                }
                // The grey: patches of static where the rock has been unmade, more of them the deeper you go.
                for (float s = 10 * X; s < h.Length - 2; s += 2.7f)
                {
                    float deep = Mathf.InverseLerp(10 * X, h.Length, s); if (A() > .25f + deep * .6f) continue;
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
                    Glow(t, On(s0, 0) + Vector3.up * 6, 14 * CW, .6f, GreyLight, .6f);
                }
            }
        }
    }
}
