using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Lowtowns' landmarks, turf and crossings (Docs/REWORK_LOWTOWNS_HALLOWS.md, Lowtowns pick 12; Docs/ZONE_LOWTOWNS.md "Landmarks, turf
    /// and crossings"). No machines anywhere: brick, timber, stone, cloth, ochre and rope, all of it worked by hand.
    /// - The Stack (GAME-ONLY): a leaning rookery of nine storeys at the High Rows' north-east, rooms added on rooms and balconies on struts,
    ///   every window lit at night, seen from most of the zone. Only its ground storey is solid (its door is painted shut); the rest is
    ///   for the eye.
    /// - The Rag Bridge of houses (CANON-EXPANDED: a bridge over the Chemical River with a "rotting arch", book1 ch.15:383; the river's banks
    ///   downstream "skeletons of houses, crushed bridges", ch.16:255; houses on the bridge GAME-ONLY): shacks and two-storey houses built
    ///   out over the water on both sides of the deck on timber struts, beams and rag lines across the walk overhead. The deck stays the
    ///   road (the houses stand beyond the parapets).
    /// - Gang turf by the day (GAME-ONLY, seeded by the Iron Teeth's warehouse kept as "neutral ground ... a delicate balance of smuggler
    ///   cartels and heavily armed thieves", book1 ch.7:21): three corners (<see cref="TurfCorners"/>); each game day the Iron Teeth hold
    ///   one (<see cref="TurfCornerOn"/>), their ochre teeth (the warehouse roof's own sign, ch.7:21) on a banner and daubed on the setts,
    ///   and two of their runners loitering (<see cref="TurfFolk"/>, EncounterSession.LowtownMarks). Never at the warehouse.
    /// - The punt ferry (GAME-ONLY): two timber landings either side of the river below the Sett Bridge, a flat punt poled across (E at a
    ///   landing: EncounterSession.LowtownMarks). Nothing drives it but a pole.
    /// - The weir (GAME-ONLY): a low stone weir across the river under the Rag Bridge's eels, its crest a walk (solid, in the navmesh),
    ///   a sluice of stacked boards in stone grooves in the middle, lifted by hand with a hook-pole, a plank over it.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The Stack's foot (GAME-ONLY), at the High Rows' north-east, on the shelf.</summary>
        public static readonly Vector2 StackAt = new Vector2(-62, 212);
        public const int StackStoreys = 9;
        public const float StoreyHeight = 3.4f;

        /// <summary>The punt ferry's two landings (where you stand to take it and where it leaves you), west and east of the river.</summary>
        public static readonly Vector2 PuntWest = new Vector2(-15, -80), PuntEast = new Vector2(11, -80);
        public const string PuntName = "The punt";
        /// <summary>The weir: its line across the river (west end, east end), its crest's width.</summary>
        public static readonly Vector2 WeirWest = new Vector2(-10.5f, 141), WeirEast = new Vector2(11.5f, 141);
        public const float WeirCrest = 1.8f;
        /// <summary>The sluice's width in the weir's middle, and the plank over it (its middle line's offset across the weir from the crest's,
        /// and its width).</summary>
        public const float WeirSluice = 2.2f, WeirPlankAt = -.4f, WeirPlankWide = .9f;
        /// <summary>A run of rail along the weir in its own frame: across, the line its inner face stands on (metres across the weir from
        /// its middle line, + downstream); side, which way is off the walk there (-1 or +1); from and to along it (metres from the weir's
        /// middle toward its east end).</summary>
        public struct WeirRail { public float across, side, from, to; }
        /// <summary>
        /// The weir's rails (2026-10-18, Chris: "falling off ramps and bad pathing"): timber, solid and cut out of the navmesh, just outside
        /// the walk on both sides the whole way across. Along the crest's two lengths both its edges; over the sluice the plank's two
        /// edges (the upstream rail runs straight on; the downstream one steps in to the plank between the sluice's piers, which stand on
        /// the downstream half beside it). Only the banks' ends are open: that is where the way goes on.
        /// </summary>
        public static List<WeirRail> WeirRails()
        {
            float len = Vector2.Distance(WeirWest, WeirEast), half = len / 2, inner = WeirSluice / 2 + .4f;
            float plankIn = WeirPlankAt - WeirPlankWide / 2, plankOut = WeirPlankAt + WeirPlankWide / 2;
            return new List<WeirRail>
            {
                new WeirRail { across = Mathf.Min(-WeirCrest / 2, plankIn), side = -1, from = -half, to = half },
                new WeirRail { across = WeirCrest / 2, side = 1, from = -half, to = -inner },
                new WeirRail { across = WeirCrest / 2, side = 1, from = inner, to = half },
                new WeirRail { across = plankOut, side = 1, from = -inner, to = inner },
            };
        }

        /// <summary>A corner the Iron Teeth may hold: its name, where its banner stands, and its two loiterers (residents) with their spots.</summary>
        public sealed class TurfCorner { public string name; public Vector2 banner, mark; public string[] folk; }
        /// <summary>
        /// The three corners in turn (GAME-ONLY; the plan's "the Sett Bridge's foot, Teeth Lane, the weir"): each far from the Iron Teeth's
        /// warehouse, which is neutral ground (book1 ch.7:21) and never turf.
        /// </summary>
        public static readonly TurfCorner[] TurfCorners =
        {
            new TurfCorner { name = "The Sett Bridge's foot", banner = new Vector2(16.5f, -13.2f), mark = new Vector2(19, -17.4f), folk = new[] { "Dace Mottle", "Rusk Hollin" } },
            new TurfCorner { name = "Teeth Lane", banner = new Vector2(158.5f, 47.5f), mark = new Vector2(159.6f, 50.5f), folk = new[] { "Vell Tarrow", "Brig Carrow" } },
            new TurfCorner { name = "The weir", banner = new Vector2(-14.5f, 135.8f), mark = new Vector2(-11.5f, 141), folk = new[] { "Sloe Mackin", "Jink Pellow" } },
        };
        /// <summary>Every Iron Teeth loiterer of the turf, all three corners'.</summary>
        public static string[] TurfFolk { get { var l = new List<string>(); foreach (var c in TurfCorners) l.AddRange(c.folk); return l.ToArray(); } }
        /// <summary>The corner the Iron Teeth hold on game day <paramref name="day"/> (0, 1 or 2; a new one each day, round in turn).</summary>
        public static int TurfCornerOn(int day) { int n = TurfCorners.Length; return ((day % n) + n) % n; }
        /// <summary>The corner a loiterer belongs to, or -1.</summary>
        public static int TurfCornerOf(string name)
        {
            for (int i = 0; i < TurfCorners.Length; i++) if (System.Array.IndexOf(TurfCorners[i].folk, name) >= 0) return i;
            return -1;
        }

        /// <summary>The ferry as built: its punt and where it lies at each landing (EncounterSession.LowtownMarks poles it across).</summary>
        public PuntFerry LowtownPunt { get; private set; }

        static readonly Color Ochre = new Color(.78f, .6f, .22f), WindowLit = new Color(1f, .74f, .38f);

        /// <summary>Everything of pick 12, in the Lowtowns only (called from LowtownEdge).</summary>
        void BuildLowtownMarks()
        {
            LowtownPunt = null;
            if (Zone == null || Zone.id != "zone.lowtowns") return;
            BuildStack(); BuildRagHouses(); BuildTurf(); BuildPuntFerry(); BuildWeir();
            BuildDrainReach();   // the silt bar and apron where the Unraveller rises (ZoneBuilder.DrainReach)
            Debug.Log("Lowtowns: the Stack, the Rag Bridge's houses, " + TurfCorners.Length + " turf corners, the punt ferry and the weir built.");
        }

        Transform MarksRoot(string name)
        {
            var root = new GameObject(name).transform; root.SetParent(statics, false);
            root.position = Vector3.zero; root.rotation = Quaternion.identity;
            return root;
        }

        // ---------- the Stack ----------
        void BuildStack()
        {
            var root = MarksRoot("The Stack");
            var night = new GameObject("Lit windows").transform; night.SetParent(root, false); night.gameObject.AddComponent<NightOnly>();
            var r = new System.Random(Zone.seed + 1212); float R() { return (float)r.NextDouble(); }
            var b = new Batch(); var lit = new Batch();
            var brick = SootBrick(2); var brick2 = SootBrick(1); var timber = TarredTimber; var slate = Tint(art.stone, new Color(.2f, .21f, .24f));
            var dark = Tint(art.stone, new Color(.05f, .05f, .06f)); var glass = Glowing(WindowLit, 1.6f); var rag = Tint(art.cloth, new Color(.45f, .4f, .32f));
            float y0 = HeightAt(StackAt.x, StackAt.y);
            var lean = new Vector3(.42f, 0, -.18f);
            // The ground storey: solid brick, its door painted shut (GAME-ONLY; nobody goes in from the street).
            SolidBox(root, new Vector3(StackAt.x, y0 + StoreyHeight / 2, StackAt.y), new Vector3(9, StoreyHeight, 8), brick).name = "The Stack's ground storey";
            b.Box(timber, new Vector3(StackAt.x, y0 + 1.1f, StackAt.y - 4.03f), new Vector3(1.3f, 2.2f, .08f));
            b.Box(Tint(art.cloth, new Color(.5f, .48f, .45f)), new Vector3(StackAt.x, y0 + 1.1f, StackAt.y - 4.08f), new Vector3(.12f, 1.6f, .02f), Quaternion.Euler(0, 0, 40));
            b.Box(Tint(art.cloth, new Color(.5f, .48f, .45f)), new Vector3(StackAt.x, y0 + 1.1f, StackAt.y - 4.08f), new Vector3(.12f, 1.6f, .02f), Quaternion.Euler(0, 0, -40));
            for (int k = 1; k < StackStoreys; k++)
            {
                // Each storey a little further out over the one under it: the lean (east and a little south), and narrower as it goes up.
                var c = new Vector3(StackAt.x, y0 + StoreyHeight * (k + .5f), StackAt.y) + lean * k * k * .2f;
                float w = 9 - k * .35f + (R() - .5f) * 1.2f, d = 8 - k * .3f + (R() - .5f) * 1.2f; float twist = (R() - .5f) * 6;
                var q = Quaternion.Euler(0, twist, (R() - .5f) * 2.5f);
                b.Box(k % 2 == 0 ? brick : brick2, c, new Vector3(w, StoreyHeight, d), q);
                // A band of timber where a storey was built on the one below by other hands.
                b.Box(timber, c + Vector3.down * (StoreyHeight / 2 - .1f), new Vector3(w + .3f, .22f, d + .3f), q);
                // Windows on all four faces, most of them lit after dark.
                for (int face = 0; face < 4; face++)
                {
                    var fq = q * Quaternion.Euler(0, face * 90, 0); float half = face % 2 == 0 ? d / 2 : w / 2, span = face % 2 == 0 ? w : d;
                    for (float x = -span / 2 + 1.1f; x < span / 2 - .8f; x += 1.7f + R() * .6f)
                    {
                        var at = c + fq * new Vector3(x, .2f, -half - .03f);
                        if (R() < .7f) lit.Box(glass, at, new Vector3(.7f, 1.1f, .04f), fq); else b.Box(dark, at, new Vector3(.7f, 1.1f, .04f), fq);
                        b.Box(timber, at + Vector3.down * .62f, new Vector3(.9f, .08f, .2f), fq);
                    }
                }
                // An added room hung off one side on struts, now and then a balcony with a rail and washing on it.
                if (R() < .75f)
                {
                    int side = R() < .5f ? -1 : 1; var room = c + q * new Vector3(side * (w / 2 + 1.3f), -.2f, (R() - .5f) * 2);
                    b.Box(R() < .5f ? timber : brick2, room, new Vector3(2.6f, 2.8f, 2.8f + R()), q * Quaternion.Euler(0, 0, side * 3));
                    b.Box(slate, room + Vector3.up * 1.55f, new Vector3(3, .15f, 3.3f), q * Quaternion.Euler(0, 0, -side * 14));
                    foreach (int s in new[] { -1, 1 })
                        b.Box(timber, room + q * new Vector3(-side * .6f, -2.1f, s * 1.1f), new Vector3(.14f, 2.2f, .14f), q * Quaternion.Euler(0, 0, side * 38));
                    if (R() < .7f) lit.Box(glass, room + q * new Vector3(side * 1.32f, .2f, 0), new Vector3(.04f, .8f, .6f), q);
                }
                if (R() < .6f)
                {
                    var face = q * Quaternion.Euler(0, R() < .5f ? 0 : 180, 0); var bal = c + face * new Vector3((R() - .5f) * 2, -StoreyHeight / 2 + .1f, -(d / 2 + .8f));
                    b.Box(timber, bal, new Vector3(3, .12f, 1.6f), face);
                    b.Box(timber, bal + face * new Vector3(0, .5f, -.78f), new Vector3(3, .06f, .06f), face);
                    for (int p = -1; p <= 1; p++) b.Box(timber, bal + face * new Vector3(p * 1.45f, .25f, -.78f), new Vector3(.06f, .5f, .06f), face);
                    b.Box(rag, bal + face * new Vector3(0, .8f, -.78f), new Vector3(2.4f, .7f, .03f), face);
                }
                // A chimney stub of brick now and then, out of the roof line.
                if (k % 3 == 2) b.Box(brick, c + q * new Vector3(w / 2 - .7f, StoreyHeight / 2 + .7f, d / 2 - .7f), new Vector3(.7f, 1.4f, .7f), q);
            }
            // The roof: a crooked slate pitch over the top storey and a last shack of planks on it.
            {
                int k = StackStoreys; var c = new Vector3(StackAt.x, y0 + StoreyHeight * k, StackAt.y) + lean * k * k * .2f;
                foreach (int s in new[] { -1, 1 }) b.Box(slate, c + new Vector3(s * 1.6f, 1.0f, 0), new Vector3(3.8f, .18f, 6.6f), Quaternion.Euler(0, 0, -s * 32));
                b.Box(timber, c + new Vector3(-1.5f, 2.6f, 1.2f), new Vector3(2.2f, 1.8f, 2), Quaternion.Euler(0, 12, 4));
                lit.Box(glass, c + new Vector3(-1.5f, 2.7f, .18f), new Vector3(.5f, .5f, .04f), Quaternion.Euler(0, 12, 4));
                Glow(night, c + Vector3.up * 3.5f, 16, 0, WindowLit, .8f).name = "The Stack's top light";
            }
            b.Flush(this, root, "The Stack"); lit.Flush(this, night, "The Stack's windows");
        }

        // ---------- the Rag Bridge's houses ----------
        void BuildRagHouses()
        {
            ZoneProp bridge = null;
            foreach (var p in Zone.props) if (p != null && p.kind == "brick_bridge" && p.name == "The Rag Bridge") bridge = p;
            if (bridge == null) return;
            var root = MarksRoot("The Rag Bridge's houses");
            var night = new GameObject("Lit windows").transform; night.SetParent(root, false); night.gameObject.AddComponent<NightOnly>();
            float length = bridge.size.x > 0 ? bridge.size.x : 22, width = bridge.size.y > 0 ? bridge.size.y : 6;
            var q = Quaternion.Euler(0, bridge.rotation, 0); var at = new Vector3(bridge.at.x, HeightAt(bridge.at.x, bridge.at.y), bridge.at.y);
            // The deck's height along it, as BrickBridge lays it (an arch rising 1.4 m at the middle; the deck's top .25 over that).
            float Deck(float lz) { float a = Mathf.Clamp01((lz + length / 2) / length); return .4f + Mathf.Sin(a * Mathf.PI) * 1.4f; }
            Vector3 P(float lx, float y, float lz) { return at + q * new Vector3(lx, y, lz); }
            var r = new System.Random(Zone.seed + 9696); float R() { return (float)r.NextDouble(); }
            var b = new Batch(); var lit = new Batch();
            var timber = TarredTimber; var plank = Tint(art.timber, new Color(.3f, .24f, .18f)); var brick = SootBrick(3);
            var slate = Tint(art.stone, new Color(.2f, .21f, .24f)); var glass = Glowing(WindowLit, 1.4f);
            Color[] rags = { new Color(.5f, .2f, .16f), new Color(.42f, .4f, .3f), new Color(.25f, .3f, .38f), new Color(.55f, .45f, .25f) };
            float wall = width / 2 + .5f;
            foreach (int side in new[] { -1, 1 })
            {
                // Houses shoulder to shoulder down each side, one gap each side where the river shows between them.
                int gap = side < 0 ? 2 : 1;
                float lz = -length / 2 + 2.2f; int n = 0;
                while (lz < length / 2 - 2.5f)
                {
                    float w = 3.2f + R() * 1.6f; float mid = lz + w / 2; lz += w + .15f;
                    if (n++ == gap) continue;
                    float deep = 3 + R() * 1.5f, storeys = R() < .55f ? 2 : 1, h = 2.6f * storeys + .2f;
                    float floor = Deck(mid) - .25f; float lx = side * (wall + deep / 2);
                    var hq = q * Quaternion.Euler(0, 0, side * (R() - .5f) * 3);
                    var c = P(lx, floor + h / 2, mid);
                    // The house: plank or brick, an upper storey jettied out over the water, a slate or plank lean-to roof sloping off the deck.
                    b.Box(R() < .6f ? plank : brick, c, new Vector3(deep, h, w), hq);
                    if (storeys > 1) b.Box(plank, P(lx + side * .35f, floor + h - 1.2f, mid), new Vector3(deep + .7f, 2.4f, w + .2f), hq);
                    b.Box(R() < .5f ? slate : timber, P(lx + side * .2f, floor + h + .45f, mid), new Vector3(deep + 1.4f, .14f, w + .3f), hq * Quaternion.Euler(0, 0, -side * 18));
                    // Struts and piles down into the water under the floor.
                    float waterY = HeightAt(c.x, c.z) + 1.5f;
                    foreach (float dz in new[] { -w / 2 + .3f, w / 2 - .3f })
                    {
                        var top = P(lx + side * deep / 2 - side * .2f, floor, mid + dz); float drop = Mathf.Max(1, top.y - waterY + 1.2f);
                        b.Box(timber, top + Vector3.down * drop / 2, new Vector3(.22f, drop, .22f), q);
                        b.Box(timber, P(side * (wall + .9f), floor - .8f, mid + dz), new Vector3(1.6f, .14f, .14f), q * Quaternion.Euler(0, 0, side * 40));
                    }
                    // A door on to the deck (shut planks) and a window or two, lit after dark.
                    b.Box(timber, P(side * (wall + .02f), floor + 1.05f, mid), new Vector3(.06f, 2, 1), q);
                    for (int f = 0; f < storeys; f++)
                    {
                        var win = P(side * (wall + deep + (f > 0 ? .35f : 0) + .03f), floor + 1.4f + f * 2.6f, mid + (R() - .5f) * (w - 1.2f));
                        if (R() < .75f) lit.Box(glass, win, new Vector3(.04f, .7f, .6f), q); else b.Box(timber, win, new Vector3(.05f, .7f, .6f), q);
                    }
                    // Nothing goes into the houses: a blocker over each (beyond the parapet, over the water).
                    Blocker(root, root.InverseTransformPoint(c), new Vector3(deep, h, w), hq);
                }
            }
            // Beams across the walk between the houses, high over the heads of carts, and rag lines strung from them.
            for (float lz = -length / 2 + 4; lz < length / 2 - 3; lz += 5.5f)
            {
                float y = Deck(lz) + 4.4f;
                b.Box(timber, P(0, y, lz), new Vector3(width + 2.2f, .22f, .26f), q);
                for (int k = 0; k < 4; k++)
                {
                    float lx = -width / 2 + .8f + k * (width - 1.6f) / 3;
                    b.Box(Tint(art.cloth, rags[(int)(R() * rags.Length)]), P(lx, y - .55f - R() * .2f, lz + (R() - .5f) * .3f), new Vector3(.45f + R() * .3f, .9f + R() * .5f, .03f), q * Quaternion.Euler(0, 0, (R() - .5f) * 14));
                }
            }
            Glow(night, root.InverseTransformPoint(P(0, Deck(0) + 3.2f, 0)), 12, 0, WindowLit, .7f).name = "Windows over the Rag Bridge";
            b.Flush(this, root, "Rag Bridge houses"); lit.Flush(this, night, "Rag Bridge windows");
        }

        // ---------- the turf ----------
        void BuildTurf()
        {
            var root = MarksRoot("The Iron Teeth's turf");
            var ochre = Tint(art.cloth, Ochre); var paint = Tint(art.stone, Ochre * .9f); var teeth = Tint(art.cloth, new Color(.1f, .09f, .08f)); var timber = TarredTimber;
            for (int i = 0; i < TurfCorners.Length; i++)
            {
                var corner = TurfCorners[i];
                var t = new GameObject(corner.name).transform; t.SetParent(root, false);
                var b = new Batch();
                var foot = Ground(corner.banner);
                // A pole with a ragged ochre banner, the Teeth's jagged teeth on it in black (their sign, book1 ch.7:21).
                b.Box(timber, foot + Vector3.up * 1.9f, new Vector3(.12f, 3.8f, .12f));
                b.Box(timber, foot + new Vector3(.55f, 3.65f, 0), new Vector3(1.2f, .07f, .07f));
                b.Box(ochre, foot + new Vector3(.6f, 2.9f, 0), new Vector3(1.05f, 1.45f, .03f));
                for (int k = 0; k < 4; k++)
                {
                    b.Box(teeth, foot + new Vector3(.24f + k * .24f, 3.15f, -.025f), new Vector3(.13f, .13f, .01f), Quaternion.Euler(0, 0, 45));
                    b.Box(teeth, foot + new Vector3(.36f + k * .24f, 2.75f, -.025f), new Vector3(.13f, .13f, .01f), Quaternion.Euler(0, 0, 45));
                }
                // The teeth daubed in ochre on the setts: a row of zigzag strokes across the corner.
                var m = Ground(corner.mark, .03f);
                for (int k = 0; k < 6; k++)
                    b.Box(paint, m + new Vector3(-1.5f + k * .6f, 0, 0), new Vector3(.12f, .01f, .8f), Quaternion.Euler(0, k % 2 == 0 ? 35 : -35, 0));
                b.Flush(this, t, corner.name + " marks");
                t.gameObject.AddComponent<TurfMarks>().corner = i;
            }
        }

        // ---------- the punt ferry ----------
        void BuildPuntFerry()
        {
            var root = MarksRoot("The punt ferry");
            var b = new Batch(); var timber = TarredTimber; var plank = Tint(art.timber, new Color(.3f, .24f, .18f));
            var west = Ground(PuntWest); var east = Ground(PuntEast);
            var across = (east - west); across.y = 0; across.Normalize();
            var q = Quaternion.LookRotation(across);
            float deckY = Mathf.Max(west.y, east.y) + .2f;
            // Each landing: a little timber stage out over the water from the bank, a mooring post and a rope.
            foreach (var land in new[] { west, east })
            {
                var outward = land == west ? across : -across;
                var mid = land + outward * 3.2f; mid.y = deckY;
                var deck = BoxPart(root, mid, new Vector3(2.2f, .2f, 4.6f), plank, Quaternion.LookRotation(outward));
                deck.AddComponent<BoxCollider>(); deck.AddComponent<NavWalkable>(); deck.name = "Punt landing";
                // (2026-10-18, Chris: "falling off ramps and bad pathing") A timber rail round the stage's three sides over the water,
                // solid and cut out of the navmesh; the bank's end is open. The punt is taken with E at the landing, so the rail on its
                // side does not stand in the way.
                var sideV = Quaternion.LookRotation(outward) * Vector3.right; var top = mid + Vector3.up * .1f;
                Vector3 C(float x, float zz) { return top + sideV * (x * 1.1f) + outward * (zz * 2.3f); }
                LowRail(root, b, "Punt landing", C(-1, -1), C(-1, 1), -sideV, plank, timber, .8f, false);
                LowRail(root, b, "Punt landing", C(1, -1), C(1, 1), sideV, plank, timber, .8f, false);
                LowRail(root, b, "Punt landing", C(-1, 1) - sideV * .22f, C(1, 1) + sideV * .22f, outward, plank, timber, .8f, false);
                foreach (int s in new[] { -1, 1 }) foreach (float f in new[] { 1.4f, 5.2f })
                {
                    var pile = land + outward * f + Quaternion.LookRotation(outward) * new Vector3(s * 1.05f, 0, 0); pile.y = deckY - 1.2f;
                    b.Box(timber, pile, new Vector3(.2f, 2.8f, .2f));
                }
                var post = land + outward * 5.3f; post.y = deckY + .6f;
                b.Box(timber, post, new Vector3(.24f, 1.2f, .24f));
            }
            // The punt: a long flat-bottomed boat of tarred planks, square at both ends, its pole across it.
            var boat = new GameObject("The punt").transform; boat.SetParent(root, false);
            var pb = new Batch();
            pb.Box(timber, new Vector3(0, 0, 0), new Vector3(1.6f, .18f, 6.2f));
            foreach (int s in new[] { -1, 1 }) pb.Box(timber, new Vector3(s * .76f, .2f, 0), new Vector3(.08f, .35f, 6.2f));
            foreach (int s in new[] { -1, 1 }) pb.Box(timber, new Vector3(0, .2f, s * 3.06f), new Vector3(1.6f, .35f, .08f));
            pb.Box(plank, new Vector3(0, .32f, 1.6f), new Vector3(1.5f, .06f, .4f));
            pb.Box(Tint(art.timber, new Color(.45f, .36f, .25f)), new Vector3(.4f, .5f, -.3f), new Vector3(.06f, .06f, 6.5f), Quaternion.Euler(4, 3, 0));
            pb.Flush(this, boat, "The punt");
            var westMoor = west + across * 6.6f + Quaternion.LookRotation(across) * new Vector3(1.9f, 0, 0); westMoor.y = deckY - .45f;
            var eastMoor = east - across * 6.6f + Quaternion.LookRotation(across) * new Vector3(1.9f, 0, 0); eastMoor.y = deckY - .45f;
            boat.position = westMoor; boat.rotation = q;
            var ferry = boat.gameObject.AddComponent<PuntFerry>(); ferry.west = westMoor; ferry.east = eastMoor; ferry.facing = q;
            LowtownPunt = ferry;
            b.Flush(this, root, "Punt landings");
            Interactables.Add(new ZoneInteractable { name = PuntName, prompt = "Take the punt across the river", kind = "punt", position = west + Vector3.up * .1f });
            Interactables.Add(new ZoneInteractable { name = PuntName, prompt = "Take the punt across the river", kind = "punt", position = east + Vector3.up * .1f });
        }

        // ---------- the weir ----------
        void BuildWeir()
        {
            var root = MarksRoot("The weir");
            var b = new Batch(); var stone = Tint(Masonry, new Color(.4f, .39f, .37f)); var wet = Tint(art.stone, new Color(.08f, .09f, .08f));
            var timber = TarredTimber; var plank = Tint(art.timber, new Color(.3f, .24f, .18f));
            var a = Ground(WeirWest); var c = Ground(WeirEast);
            float top = Mathf.Max(a.y, c.y) + .25f;
            var run = c - a; run.y = 0; float len = run.magnitude; var q = Quaternion.LookRotation(run / len);
            var mid = (a + c) / 2; mid.y = top;
            float sluice = WeirSluice;
            // The crest: two lengths of dressed stone either side of the sluice, walkable, solid; the body of the weir under them, and the
            // downstream apron sloping into the river (only to see).
            float part = (len - sluice) / 2 - .4f;
            foreach (int s in new[] { -1, 1 })
            {
                var centre = mid + q * new Vector3(0, -.2f, s * (sluice / 2 + .4f + part / 2));
                var crest = BoxPart(root, centre, new Vector3(WeirCrest, .4f, part), stone, q);
                crest.AddComponent<BoxCollider>(); crest.AddComponent<NavWalkable>(); crest.name = "The weir's crest";
                var under = HeightAt(centre.x, centre.z); float body = Mathf.Max(.5f, top - under - .4f);
                b.Box(stone, new Vector3(centre.x, top - .4f - body / 2, centre.z), new Vector3(WeirCrest, body, part), q);
                b.Box(wet, centre + q * new Vector3(1.6f, -1f, 0), new Vector3(2.2f, .2f, part), q * Quaternion.Euler(0, 0, -28));
            }
            // The sluice: two stone piers with grooves, the boards stacked in them (dropped in and lifted out by hand), a plank walk over.
            // (2026-10-18) The piers stand on the downstream half only, beside the plank: across the whole crest they shut the walk.
            float plankOut = WeirPlankAt + WeirPlankWide / 2, pierW = WeirCrest / 2 + .2f - plankOut - .05f;
            foreach (int s in new[] { -1, 1 })
            {
                var pier = mid + q * new Vector3(plankOut + .05f + pierW / 2, .3f, s * (sluice / 2 + .2f));
                var p = BoxPart(root, pier, new Vector3(pierW, 1.4f, .4f), stone, q); p.AddComponent<BoxCollider>(); p.AddComponent<NavBlocker>();
            }
            for (int k = 0; k < 4; k++) b.Box(plank, mid + q * new Vector3(.3f, -.5f - k * .32f, 0), new Vector3(.12f, .3f, sluice + .1f), q);
            var walk = BoxPart(root, mid + q * new Vector3(WeirPlankAt, .02f, 0), new Vector3(WeirPlankWide, .12f, sluice + .8f), plank, q);
            walk.AddComponent<BoxCollider>(); walk.AddComponent<NavWalkable>(); walk.name = "The plank over the sluice";
            // The hook-pole the sluice-keepers lift the boards with, leaning on a pier.
            b.Box(Tint(art.timber, new Color(.45f, .36f, .25f)), mid + q * new Vector3(.75f, .9f, sluice / 2 + .45f), new Vector3(.06f, 2.6f, .06f), q * Quaternion.Euler(14, 0, 8));
            // The rails (WeirRails, 2026-10-18; they replace the rope hand-line along the plank): timber on posts down to the weir's body.
            foreach (var rl in WeirRails())
            {
                var ra = mid + q * new Vector3(rl.across, .08f, rl.from); var rb = mid + q * new Vector3(rl.across, .08f, rl.to);
                LowRail(root, b, "The weir", ra, rb, q * new Vector3(rl.side, 0, 0), plank, timber, .6f, false);
            }
            b.Flush(this, root, "The weir");
        }
    }

    /// <summary>A turf corner's marks: shown only on the day the Iron Teeth hold it (<see cref="ZoneBuilder.TurfCornerOn"/>, the game's day
    /// from <see cref="CropField.Day"/>), looked at every couple of seconds.</summary>
    public sealed class TurfMarks : MonoBehaviour
    {
        public int corner;
        float nextAt; int shown = -1;
        void Update()
        {
            if (Time.time < nextAt) return; nextAt = Time.time + 2;
            int now = ZoneBuilder.TurfCornerOn(CropField.Day) == corner ? 1 : 0; if (now == shown) return; shown = now;
            foreach (var rr in GetComponentsInChildren<Renderer>(true)) rr.enabled = now == 1;
        }
    }

    /// <summary>
    /// The punt (GAME-ONLY): where it lies at each landing, and its crossing. <see cref="Pole"/> sends it across from one side to the other over
    /// a number of seconds, poled (a slow glide with a dip of the bow at each push); it is only to see: the crossing itself moves the party.
    /// </summary>
    public sealed class PuntFerry : MonoBehaviour
    {
        public Vector3 west, east; public Quaternion facing;
        /// <summary>Whether it lies at the west landing (it starts there).</summary>
        public bool AtWest { get; private set; } = true;
        Vector3 from, to; float start, seconds;
        public void Pole(bool toWest, float over)
        {
            from = transform.position; to = toWest ? west : east; start = Time.time; seconds = Mathf.Max(.1f, over); AtWest = toWest; enabled = true;
        }
        void Update()
        {
            if (seconds <= 0) { enabled = false; return; }
            float u = Mathf.Clamp01((Time.time - start) / seconds);
            // A push every second and a bit: the glide surges and slackens.
            float s = u + Mathf.Sin(u * Mathf.PI * 8) * .015f;
            transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, Mathf.Clamp01(s)));
            transform.rotation = facing * Quaternion.Euler(Mathf.Sin(u * Mathf.PI * 8) * 1.2f, 0, 0);
            if (u >= 1) { seconds = 0; enabled = false; }
        }
    }
}
