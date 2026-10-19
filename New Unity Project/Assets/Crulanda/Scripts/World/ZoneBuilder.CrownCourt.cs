using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Crafters' Court of the Crown (p9-court, 2026-10-16; Chris, of the court's frame, trainers on bare paving by one stall and a
    /// cookfire: "need more profession tools/small huts/vendored items"; before it, "make sure all professions are located in the castle").
    /// All of it GAME-ONLY.
    /// - A workshop for every trade round the court (<see cref="CourtTrades"/>, prop kind court_workshop, its variant the trade's index): a
    ///   small hut open at the front, white stone to the sill and timber-framed plaster above, under blue slate or a striped canvas, the
    ///   trade's name in gilt on its front beam and its sign hung from an iron bracket at the corner; inside, its tools and stock from the
    ///   packs and painted pieces of our own. The back and side walls, the corner posts, the counter and the heavy pieces are solid; the
    ///   roof fades where it hides you (RoofFade, open).
    /// - The trade's station (forge, bench or fire) stands inside it: the zone's station of the trade's kind standing on the workshop's
    ///   station spot (<see cref="CourtWorkshopOf"/>), drawn by BuildStations as the trade's own (<see cref="CourtStation"/>: an anvil and
    ///   its quench tub, a still-table, a raised hearth, a cutting bench, a hand loom with its warp weights, a bowyer's bench), with no
    ///   collider and nothing in the navmesh as every zone station; the workshop makes its footing solid round it.
    /// - Its people: the trainer at the station facing out, a stall-keeper behind the counter (the vendors of Items/court.json), and an
    ///   apprentice's bench at the back. Each post is registered as a workplace (the trainer's and the keeper's "lodge": no station and no
    ///   city trade goes there), its look inside the hut, so the city's rule (Villager.StartActivity: a post faces away from what is at
    ///   its back) turns them out to the court; an apprentice's bench is a "tannery" (the working pose), its look set behind the
    ///   apprentice so the same rule turns them to the work.
    /// - The court between the workshops: a well with a rope over a bar and a trough (no windlass, no crank), a board of crafting orders,
    ///   benches, and crates, barrels, sacks and firewood by the workshops (court_well, court_board, court_bench, court_goods).
    /// No machines (Chris, 2026-10-11): a hand loom hung with clay weights, bellows worked by hand, a saw-horse and a drawknife.
    /// Each kind draws only from a stream of its own keyed on where it stands, so nothing else in the zone moves for it.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>One trade's workshop in the Court: its trade, the profession it teaches, the hut's name and sign, the station kind
        /// inside (null for a gathering trade with none), its roof, its size (w across the open front, d front to back) and, in its own
        /// frame (x across, y from the front at -d/2 to the back at +d/2), where the station, the trainer, the stall-keeper and the
        /// counter stand, and the apprentice's place and the work in front of them.</summary>
        public sealed class CourtTrade
        {
            public string id, profession, name, sign, station; public int roof; public float w = 6, d = 4.5f;
            public Vector2 stationAt = new Vector2(-1.2f, .85f), trainerAt = new Vector2(-1.2f, -.5f), vendorAt = new Vector2(1.7f, -.45f), counterAt = new Vector2(1.7f, -1.35f),
                apprenticeAt = new Vector2(1.7f, .85f), workAt = new Vector2(1.7f, 1.75f);
        }
        public const int RoofGable = 0, RoofSlatePent = 1, RoofCanvas = 2;
        /// <summary>The Court's trades, in the order of their workshops' variants (GAME-ONLY).</summary>
        public static readonly CourtTrade[] CourtTrades = {
            new CourtTrade { id = "smith", profession = "blacksmithing", name = "The Court Smithy", sign = "Smithy", station = "forge", roof = RoofGable, w = 7, d = 5,
                stationAt = new Vector2(-1f, .55f), trainerAt = new Vector2(-1f, -.9f), vendorAt = new Vector2(2.2f, -.75f), counterAt = new Vector2(2.2f, -1.65f), apprenticeAt = new Vector2(.55f, 1.15f), workAt = new Vector2(1.5f, 1.45f) },
            new CourtTrade { id = "leather", profession = "leatherworking", name = "The Court Leather-shop", sign = "Leathers", station = "bench", roof = RoofSlatePent },
            new CourtTrade { id = "weaver", profession = "weaving", name = "The Court Loom-house", sign = "Weavers", station = "bench", roof = RoofCanvas },
            new CourtTrade { id = "simpler", profession = "alchemy", name = "The Court Still-room", sign = "Simples", station = "bench", roof = RoofSlatePent },
            new CourtTrade { id = "cook", profession = "cooking", name = "The Court Cookshop", sign = "Cookshop", station = "fire", roof = RoofGable,
                stationAt = new Vector2(-1.2f, 1.15f), trainerAt = new Vector2(-1.2f, -.2f), apprenticeAt = new Vector2(1.7f, .75f), workAt = new Vector2(1.7f, 1.6f) },
            new CourtTrade { id = "bowyer", profession = "woodcutting", name = "The Court Bowyer's", sign = "Bowyer", station = "bench", roof = RoofCanvas },
            new CourtTrade { id = "miner", profession = "mining", name = "The Court Ore-house", sign = "Miners", roof = RoofSlatePent },
            new CourtTrade { id = "herbalist", profession = "herbalism", name = "The Court Herbary", sign = "Herbs", roof = RoofCanvas },
        };
        /// <summary>The trade of a court_workshop prop, or null for any other prop.</summary>
        public static CourtTrade CourtTradeOf(ZoneProp p) { return p != null && p.kind == "court_workshop" && p.variant >= 0 && p.variant < CourtTrades.Length ? CourtTrades[p.variant] : null; }
        /// <summary>The Court workshop a zone station stands in: the court_workshop whose trade works at a station of the station's kind
        /// and whose station spot is within 30 cm of it; null for any other station.</summary>
        public static ZoneProp CourtWorkshopOf(ZoneDefinition zone, ZoneStation s)
        {
            if (zone?.props == null || s == null) return null;
            foreach (var p in zone.props)
            {
                var tr = CourtTradeOf(p);
                if (tr != null && tr.station == s.kind && Vector2.Distance(CourtWorld(p, tr.stationAt), s.at) < .3f) return p;
            }
            return null;
        }
        /// <summary>A point of a prop's own frame (x across, y its front to back) in the zone (as the root's turn sets it).</summary>
        public static Vector2 CourtWorld(ZoneProp p, Vector2 local)
        {
            float r = p.rotation * Mathf.Deg2Rad; return p.at + new Vector2(Mathf.Cos(r), -Mathf.Sin(r)) * local.x + new Vector2(Mathf.Sin(r), Mathf.Cos(r)) * local.y;
        }
        /// <summary>What a Court prop stands on, across (x) and front to back (y), round its middle: a workshop's walls and posts, the well
        /// with its trough, the board, a heap of goods; zero for a bench (stepped round, not solid) and for anything else.</summary>
        public static Vector2 CourtSize(ZoneProp p)
        {
            var trade = CourtTradeOf(p); if (trade != null) return new Vector2(trade.w, trade.d);
            switch (p?.kind)
            {
                case "court_well": return new Vector2(2.4f, 3.4f);
                case "court_board": return new Vector2(1.9f, .4f);
                case "court_goods": return new Vector2(1.4f, 1.1f);
                default: return Vector2.zero;
            }
        }

        /// <summary>The Court's ways (the zone's x, y), kept clear of everything the Court builds: in from the Crown Square south and north
        /// of the Treasury, across it west and east of the well, out to the east colonnade between the Still-room and the Herbary, and
        /// along the north colonnade between its columns as far as the east colonnade's corner column (CrownCourtTests, CrownCourtPlayTests).</summary>
        public static readonly (string name, Vector2[] points)[] CourtWays = {
            ("the south way in", new[] { new Vector2(63, 65), new Vector2(91, 65) }),
            ("the north way in", new[] { new Vector2(63, 93), new Vector2(68, 93), new Vector2(68, 86), new Vector2(91, 86) }),
            ("the cross, west of the well", new[] { new Vector2(79, 65), new Vector2(79, 86) }),
            ("the cross, east of the well", new[] { new Vector2(91, 65), new Vector2(91, 86) }),
            ("the way to the east colonnade", new[] { new Vector2(91, 76), new Vector2(104, 76) }),
            ("the north colonnade", new[] { new Vector2(68, 93), new Vector2(72, 97.5f), new Vector2(100, 97.5f) }) };   // to the column where it meets the east colonnade, not through it
        /// <summary>Points every 25 cm along the Court's ways, with each way's name.</summary>
        public static IEnumerable<(string name, Vector2 at)> CourtWayPoints()
        {
            foreach (var (name, pts) in CourtWays)
                for (int i = 0; i + 1 < pts.Length; i++)
                {
                    int n = Mathf.CeilToInt(Vector2.Distance(pts[i], pts[i + 1]) / .25f);
                    for (int k = 0; k <= n; k++) yield return (name, Vector2.Lerp(pts[i], pts[i + 1], k / (float)n));
                }
        }

        /// <summary>Builds a Court kind (CityProp's default); false for a kind that is none of them.</summary>
        bool CourtProp(Transform t, ZoneProp p)
        {
            switch (p.kind)
            {
                case "court_workshop": CourtWorkshop(t, p); return true;
                case "court_well": CourtWell(t); return true;
                case "court_board": CourtBoard(t); return true;
                case "court_goods": CourtGoods(t, p.variant); return true;
                case "court_bench": CityBench(t, Vector3.zero, 0, p.size.x > 0 ? p.size.x : 1.8f); return true;
                default: return false;
            }
        }

        // ---------- the Court's stuff ----------
        Material CourtIron { get { return Tint(art.metal, new Color(.22f, .22f, .24f)); } }
        Material CourtCopper { get { return Tint(art.metal, new Color(.74f, .44f, .24f)); } }
        Material CourtPlank { get { return Tint(art.timber, new Color(.5f, .37f, .24f)); } }
        Material CourtPlaster { get { return Tint(art.plaster, new Color(.94f, .91f, .84f)); } }
        Material CourtGlass { get { return Tint(art.stone, new Color(.34f, .5f, .44f)); } }
        Material CourtCoal { get { return Tint(art.stone, new Color(.11f, .1f, .1f)); } }
        Material CourtEmber { get { return Glowing(new Color(1, .42f, .12f), 2.4f); } }
        /// <summary>Dyed cloth (madder, woad, weld, a heather and an oatmeal): the weaver's bolts and the canvas roofs.</summary>
        static readonly Color[] CourtDyes = { new Color(.62f, .14f, .12f), new Color(.16f, .28f, .56f), new Color(.82f, .64f, .18f), new Color(.44f, .24f, .44f), new Color(.86f, .8f, .66f) };
        /// <summary>Bars of the smith's metals: copper, bog-iron, ridge-steel, ash-steel.</summary>
        static readonly Color[] CourtMetals = { new Color(.74f, .44f, .24f), new Color(.3f, .28f, .27f), new Color(.56f, .6f, .66f), new Color(.4f, .38f, .36f) };
        /// <summary>Ore as it comes out of the seam: copper green-stained, bog-iron rust, adit iron grey, cinder black and red, Veridian green.</summary>
        static readonly Color[] CourtOres = { new Color(.42f, .5f, .38f), new Color(.5f, .3f, .18f), new Color(.42f, .42f, .44f), new Color(.24f, .16f, .14f), new Color(.24f, .5f, .38f) };
        float Rf(System.Random r) { return (float)r.NextDouble(); }

        // ---------- the workshops ----------
        /// <summary>
        /// One trade's workshop (see the class's summary): the hut (<see cref="CourtHut"/>), its sign, its counter with the stall's wares, the
        /// trade's own furnishings (<see cref="CourtFurnish"/>), a lantern under the front beam lit after dark, the people's places, and the
        /// roof's fade. Fronts face -Z.
        /// </summary>
        void CourtWorkshop(Transform t, ZoneProp p)
        {
            int i = Mathf.Clamp(p.variant, 0, CourtTrades.Length - 1); var tr = CourtTrades[i]; var r = CityRandom(t, 0x5c0a7 + i);
            float front = CourtHut(t, tr);
            CourtSign(t, tr, i, front);
            CourtCounter(t, tr, i, r);
            CourtFurnish(t, tr, i, r);
            // Its lantern, hung from the front beam over the counter's end.
            var lamp = new Vector3(tr.counterAt.x - .9f, front - .62f, -tr.d / 2 + .15f);
            if (ModelProp(t, "Medieval/Prefabs/LanternV1", lamp, 0, .36f) == null) Part(PrimitiveType.Cube, t, lamp + Vector3.up * .18f, new Vector3(.2f, .3f, .2f), art.glass);
            Rod(t, lamp + Vector3.up * .36f, lamp + Vector3.up * .5f, .015f, CourtIron);
            Glow(t, lamp + Vector3.up * .15f, 7, 0, new Color(1, .78f, .45f), 1.1f);
            // Where its people stand (the class's summary): the trainer before the station, the keeper behind the counter, the apprentice at the work.
            Vector3 L(Vector2 v, float y = 0) { return new Vector3(v.x, y, v.y); }
            Workplace(t, "lodge", L(tr.trainerAt), L(tr.stationAt));
            Workplace(t, "lodge", L(tr.vendorAt), L(tr.vendorAt + new Vector2(0, 1)));
            Workplace(t, "tannery", L(tr.apprenticeAt), L(tr.apprenticeAt + (tr.apprenticeAt - tr.workAt)));
            var fade = t.gameObject.AddComponent<RoofFade>(); fade.Half = new Vector2(tr.w / 2, tr.d / 2); fade.Walled = false;
        }
        /// <summary>
        /// The hut: a flagged floor; the back wall, white stone to 1.4 m and timber-framed plaster above; the side walls the same along the
        /// back half, a post at their open ends; two hewn corner posts and a beam across the front; a gable of blue slate with a gilt ridge
        /// (roof 0) or a pent falling to the front, slate on rafters (1) or striped canvas with a valance (2). The walls are each one
        /// collider, the posts their own. Returns the front beam's top.
        /// </summary>
        float CourtHut(Transform t, CourtTrade tr)
        {
            float w = tr.w, d = tr.d; const float th = .35f, front = 2.75f; bool gable = tr.roof == RoofGable;
            float back = gable ? front + .35f : 3.45f, zf = -d / 2 + .15f, zb = d / 2 - th / 2, k = gable ? 0 : (back - front) / (zb - zf);
            float H(float z) { return front + (z - zf) * k; }   // the eaves over local z (a pent roof rises to the back)
            var oak = DarkOak; var plaster = CourtPlaster;
            Part(PrimitiveType.Cube, t, new Vector3(0, .02f, 0), new Vector3(w - .3f, .04f, d - .3f), Tint(Masonry, new Color(.62f, .6f, .56f)));
            // The back wall.
            BoxPart(t, new Vector3(0, .7f, zb), new Vector3(w, 1.4f, th), CityWhite, null, 1.5f);
            BoxPart(t, new Vector3(0, (1.4f + back) / 2, zb), new Vector3(w - .1f, back - 1.4f, th - .08f), plaster, null, 1);
            foreach (float face in new[] { zb - th / 2 + .02f, zb + th / 2 - .02f })
            {
                Part(PrimitiveType.Cube, t, new Vector3(0, 1.45f, face), new Vector3(w, .14f, .06f), oak);   // the sill beam
                Part(PrimitiveType.Cube, t, new Vector3(0, back - .08f, face), new Vector3(w, .14f, .06f), oak);   // the wall plate
                for (float x = -w / 2 + .1f; x <= w / 2 - .05f; x += (w - .2f) / 4) Part(PrimitiveType.Cube, t, new Vector3(x, (1.45f + back) / 2, face), new Vector3(.12f, back - 1.45f, .06f), oak);
                Part(PrimitiveType.Cube, t, new Vector3(-w / 4, (1.45f + back) / 2, face), new Vector3(.1f, Mathf.Sqrt((w / 4) * (w / 4) + (back - 1.45f) * (back - 1.45f)) * .9f, .05f), oak, Quaternion.Euler(0, 0, Mathf.Atan2(w / 4, back - 1.45f) * Mathf.Rad2Deg));   // a brace
            }
            Blocker(t, new Vector3(0, back / 2, zb), new Vector3(w, back, th));
            // The side walls along the back half, the corner posts at the front.
            foreach (int s in new[] { -1, 1 })
            {
                float x = s * (w / 2 - th / 2);
                BoxPart(t, new Vector3(x, .6f, d / 4), new Vector3(th, 1.2f, d / 2), CityWhite, null, 1.5f);
                MeshPart(Cutout(new[] { new Vector2(0, 1.2f), new Vector2(d / 2, 1.2f), new Vector2(d / 2, H(d / 2)), new Vector2(0, H(0)) }, th - .08f), t, new Vector3(x, 0, 0), plaster, Quaternion.Euler(0, -90, 0)).name = "Side wall";
                Part(PrimitiveType.Cube, t, new Vector3(x, 1.25f, d / 4), new Vector3(th + .04f, .12f, d / 2), oak);
                Stake(t, new Vector3(x, -.05f, .08f), .26f, H(.08f) - .02f, oak);
                Blocker(t, new Vector3(x, H(0) / 2, d / 4), new Vector3(th, H(0), d / 2));
                Solidify(Stake(t, new Vector3(s * (w / 2 - .15f), -.05f, zf), .26f, front - .2f, oak));
                Bar(t, new Vector3(s * (w / 2 - .15f), front - .1f, zf), new Vector3(s * (w / 2 - .15f), H(zb) - .1f, zb), .2f, .2f, oak);   // the side beam to the back wall
            }
            BoxPart(t, new Vector3(0, front - .13f, zf), new Vector3(w + .1f, .26f, .24f), oak, null, 1);   // the front beam
            if (gable)
            {
                MeshPart(ZoneMeshes.GableRoof(w + .9f, d + 1f, 1.35f, .22f), t, new Vector3(0, front, 0), BlueSlate);
                Part(PrimitiveType.Cube, t, new Vector3(0, front + 1.38f, 0), new Vector3(w + 1f, .14f, .18f), Gilt);
                return front;
            }
            // A pent: a slab from over the back wall down past the front beam.
            float Y(float z) { return H(z) + .06f; }
            float z0 = d / 2 + .3f, z1 = -d / 2 - .5f, len = Mathf.Sqrt((z0 - z1) * (z0 - z1) + (Y(z0) - Y(z1)) * (Y(z0) - Y(z1)));
            var q = Quaternion.Euler(-Mathf.Atan(k) * Mathf.Rad2Deg, 0, 0); var mid = new Vector3(0, (Y(z0) + Y(z1)) / 2, (z0 + z1) / 2);
            if (tr.roof == RoofSlatePent)
            {
                BoxPart(t, mid, new Vector3(w + .6f, .1f, len), BlueSlate, q, 1.5f);
                for (float x = -w / 2 + .3f; x <= w / 2 - .25f; x += (w - .6f) / 4) Bar(t, new Vector3(x, Y(zb) - .12f, zb), new Vector3(x, Y(z1 + .15f) - .12f, z1 + .15f), .12f, .09f, oak);
                Part(PrimitiveType.Cube, t, new Vector3(0, Y(z1) - .05f, z1 - .02f), new Vector3(w + .62f, .16f, .05f), oak);   // the fascia
            }
            else
            {
                // Striped canvas in the trade's two dyes, and a valance of tabs along its front edge.
                int n = 8; float sw = (w + .6f) / n; var c0 = Tint(art.cloth, CourtDyes[Array.IndexOf(CourtTrades, tr) % CourtDyes.Length]); var c1 = Tint(art.cloth, new Color(.92f, .88f, .76f));
                for (int i = 0; i < n; i++) Part(PrimitiveType.Cube, t, mid + q * new Vector3(-(w + .6f) / 2 + sw * (i + .5f), 0, 0), new Vector3(sw + .005f, .05f, len), i % 2 == 0 ? c0 : c1, q);
                for (float x = -w / 2 - .2f; x <= w / 2 + .2f; x += .5f) Part(PrimitiveType.Cube, t, new Vector3(x, Y(z1) - .17f, z1 - .01f), new Vector3(.44f, .3f, .02f), Mathf.RoundToInt((x + w) * 2) % 2 == 0 ? c0 : c1);
                Part(PrimitiveType.Cylinder, t, new Vector3(0, Y(z1) - .01f, z1), new Vector3(.06f, (w + .6f) / 2, .06f), oak, Quaternion.Euler(0, 0, 90));   // the pole it is laced to
            }
            return front;
        }
        /// <summary>The trade's name in gilt on the front beam, and its sign: a board hung by two chains from an iron bracket at the right
        /// corner, gilt-edged, its emblem standing through it so it reads from either side (<see cref="CourtEmblem"/>).</summary>
        void CourtSign(Transform t, CourtTrade tr, int i, float front)
        {
            float w = tr.w, d = tr.d, zf = -d / 2 + .15f; var iron = CourtIron;
            Lettering(t, tr.sign, new Vector3(0, front - .13f, zf - .13f), w * .55f);
            float bx = w / 2 - .15f, by = front - .2f, z1 = zf - .95f;   // the board's foot 1.9 m up: walked under
            Bar(t, new Vector3(bx, by, zf - .1f), new Vector3(bx, by, z1), .05f, .04f, iron);
            Rod(t, new Vector3(bx, by - .45f, zf - .02f), new Vector3(bx, by - .02f, zf - .5f), .03f, iron);   // its brace
            var at = new Vector3(bx, by - .42f, z1 + .38f);
            foreach (float z in new[] { -.24f, .24f }) Rod(t, new Vector3(bx, by - .02f, at.z + z), new Vector3(bx, at.y + .25f, at.z + z), .014f, iron);
            Part(PrimitiveType.Cube, t, at, new Vector3(.05f, .46f, .64f), DarkOak);
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, at + new Vector3(0, s * .24f, 0), new Vector3(.07f, .03f, .68f), Gilt);
                Part(PrimitiveType.Cube, t, at + new Vector3(0, 0, s * .33f), new Vector3(.07f, .5f, .03f), Gilt);
            }
            CourtEmblem(t, i, at);
        }
        /// <summary>A trade's emblem in the plane of its sign (y up, z across), .09 thick so it shows on both faces: the anvil, a stretched
        /// hide, a shuttle over a spool, a flask, a pot and ladle, a bow and arrow, two crossed picks, a leaf on its sprig.</summary>
        void CourtEmblem(Transform t, int i, Vector3 c)
        {
            var gold = Gilt; var iron = CourtIron; const float th = .09f;
            switch (CourtTrades[i].id)
            {
                case "smith":
                    Part(PrimitiveType.Cube, t, c + new Vector3(0, .06f, 0), new Vector3(th, .07f, .32f), gold);
                    Part(PrimitiveType.Cube, t, c + new Vector3(0, -.02f, .02f), new Vector3(th, .09f, .12f), gold);
                    Part(PrimitiveType.Cube, t, c + new Vector3(0, -.1f, .02f), new Vector3(th, .05f, .24f), gold);
                    Part(PrimitiveType.Cube, t, c + new Vector3(0, .07f, -.2f), new Vector3(th, .04f, .1f), gold);   // the horn
                    break;
                case "leather":
                    Part(PrimitiveType.Sphere, t, c, new Vector3(th, .3f, .24f), HideOf(0));
                    foreach (int a in new[] { -1, 1 }) foreach (int b in new[] { -1, 1 }) Part(PrimitiveType.Sphere, t, c + new Vector3(0, a * .13f, b * .12f), new Vector3(th, .07f, .07f), HideOf(0));
                    break;
                case "weaver":
                    Part(PrimitiveType.Capsule, t, c + new Vector3(0, .07f, 0), new Vector3(th, .2f, th), gold, Quaternion.Euler(90, 0, 0));
                    Part(PrimitiveType.Cylinder, t, c + new Vector3(0, -.08f, 0), new Vector3(.12f, th / 2 + .01f, .12f), Tint(art.cloth, CourtDyes[0]), Quaternion.Euler(0, 0, 90));
                    break;
                case "simpler":
                    Part(PrimitiveType.Sphere, t, c + new Vector3(0, -.05f, 0), new Vector3(th, .2f, .2f), CourtGlass);
                    Part(PrimitiveType.Cube, t, c + new Vector3(0, .1f, 0), new Vector3(th, .12f, .06f), CourtGlass);
                    Part(PrimitiveType.Cube, t, c + new Vector3(0, .17f, 0), new Vector3(th + .01f, .03f, .08f), gold);
                    break;
                case "cook":
                    Part(PrimitiveType.Sphere, t, c + new Vector3(0, -.04f, 0), new Vector3(th, .18f, .28f), iron);
                    Part(PrimitiveType.Cube, t, c + new Vector3(0, .05f, 0), new Vector3(th + .01f, .03f, .3f), gold);
                    Part(PrimitiveType.Cube, t, c + new Vector3(0, .1f, .1f), new Vector3(th, .2f, .025f), gold, Quaternion.Euler(-20, 0, 0));   // the ladle
                    break;
                case "bowyer":
                    for (int k = -1; k <= 1; k++) Part(PrimitiveType.Cube, t, c + new Vector3(0, k * .12f, .05f - Mathf.Abs(k) * .05f), new Vector3(th, .14f, .03f), gold, Quaternion.Euler(-k * 28, 0, 0));
                    Part(PrimitiveType.Cube, t, c + new Vector3(0, 0, .09f), new Vector3(th - .02f, .38f, .008f), iron);   // the string
                    Part(PrimitiveType.Cube, t, c, new Vector3(th - .01f, .015f, .3f), gold);   // the arrow
                    break;
                case "miner":
                    foreach (int s in new[] { -1, 1 })
                    {
                        var q = Quaternion.Euler(s * 40, 0, 0);
                        Part(PrimitiveType.Cube, t, c, new Vector3(th, .36f, .03f), DarkOak, q);
                        Part(PrimitiveType.Cube, t, c + q * new Vector3(0, .17f, 0), new Vector3(th + .01f, .04f, .2f), gold, q);
                    }
                    break;
                default:
                    Part(PrimitiveType.Sphere, t, c + new Vector3(0, .03f, 0), new Vector3(th, .3f, .13f), Tint(art.foliage, new Color(.3f, .52f, .24f)), Quaternion.Euler(30, 0, 0));
                    Part(PrimitiveType.Cube, t, c + new Vector3(0, -.12f, -.05f), new Vector3(th - .02f, .14f, .015f), gold, Quaternion.Euler(30, 0, 0));
                    break;
            }
        }
        /// <summary>The stall-keeper's counter: a plank front on a stone kick, an oak top, solid; on it the wares (<see cref="CourtWares"/>).</summary>
        void CourtCounter(Transform t, CourtTrade tr, int i, System.Random r)
        {
            var c = new Vector3(tr.counterAt.x, 0, tr.counterAt.y); const float len = 1.6f;
            BoxPart(t, c + new Vector3(0, .47f, 0), new Vector3(len, .86f, .5f), CourtPlank, null, 1);
            Part(PrimitiveType.Cube, t, c + new Vector3(0, .93f, -.03f), new Vector3(len + .12f, .06f, .62f), DarkOak);
            Part(PrimitiveType.Cube, t, c + new Vector3(0, .07f, -.02f), new Vector3(len + .04f, .14f, .54f), CityShade);
            foreach (float x in new[] { -len / 2 + .05f, len / 2 - .05f }) Part(PrimitiveType.Cube, t, c + new Vector3(x, .47f, -.26f), new Vector3(.08f, .86f, .04f), DarkOak);
            Blocker(t, c + new Vector3(0, .48f, 0), new Vector3(len + .12f, .96f, .62f));
            CourtWares(t, c + new Vector3(0, .96f, -.03f), i, r);
        }
        /// <summary>What lies on a stall's counter (top centre at <paramref name="top"/>, 1.6 long), by trade.</summary>
        void CourtWares(Transform t, Vector3 top, int i, System.Random r)
        {
            var iron = CourtIron;
            switch (CourtTrades[i].id)
            {
                case "smith":
                    for (int k = 0; k < 6; k++) Part(PrimitiveType.Cube, t, top + new Vector3(-.6f + (k % 3) * .1f, .03f + (k / 3) * .06f, -.05f + (k / 3) * .02f), new Vector3(.08f, .05f, .26f), Tint(art.metal, CourtMetals[k % 2]));   // bars
                    for (int k = 0; k < 3; k++) Part(PrimitiveType.Cylinder, t, top + new Vector3(-.05f + k * .14f, .012f, .1f), new Vector3(.12f, .012f, .12f), iron);   // horseshoes, laid flat
                    Part(PrimitiveType.Cube, t, top + new Vector3(.45f, .03f, -.05f), new Vector3(.36f, .04f, .04f), DarkOak, Quaternion.Euler(0, 20, 0));
                    Part(PrimitiveType.Cube, t, top + new Vector3(.6f, .04f, .0f), new Vector3(.06f, .06f, .2f), iron, Quaternion.Euler(0, 20, 0));   // a pick's head on its haft
                    break;
                case "leather":
                    for (int k = 0; k < 4; k++) Part(PrimitiveType.Cube, t, top + new Vector3(-.5f, .015f + k * .03f, 0), new Vector3(.5f, .025f, .38f), HideOf(k), Quaternion.Euler(0, k * 6 - 9, 0));   // folded hides
                    if (ModelProp(t, "Megapack/Models/LeatherBag_1", top + new Vector3(.15f, 0, 0), 160, .32f) == null) Part(PrimitiveType.Cube, t, top + new Vector3(.15f, .14f, 0), new Vector3(.24f, .28f, .12f), HideOf(2));
                    for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, t, top + new Vector3(.55f, .01f + k * .02f, -.1f + k * .1f), new Vector3(.4f, .015f, .05f), HideOf(k + 3));   // belts
                    break;
                case "weaver":
                    for (int k = 0; k < 5; k++) Part(PrimitiveType.Cube, t, top + new Vector3(-.5f + (k % 3) * .02f, .04f + k * .07f, 0), new Vector3(.5f, .07f, .36f), Tint(art.cloth, CourtDyes[k % CourtDyes.Length]));   // folded cloth
                    for (int k = 0; k < 4; k++) Part(PrimitiveType.Cylinder, t, top + new Vector3(.2f + (k % 2) * .2f, .05f, -.1f + (k / 2) * .2f), new Vector3(.14f, .05f, .14f), Tint(art.cloth, CourtDyes[(k + 1) % CourtDyes.Length]), Quaternion.Euler(90, 0, 0));   // skeins
                    break;
                case "simpler":
                    for (int k = 0; k < 6; k++) { var v = top + new Vector3(-.6f + k * .09f, 0, -.12f); Part(PrimitiveType.Cylinder, t, v + new Vector3(0, .06f, 0), new Vector3(.05f, .06f, .05f), CourtGlass); Part(PrimitiveType.Cylinder, t, v + new Vector3(0, .135f, 0), new Vector3(.025f, .015f, .025f), Tint(art.timber, new Color(.6f, .46f, .3f))); }
                    Jar(t, top + new Vector3(.05f, 0, .1f), new Color(.62f, .4f, .28f)); Jar(t, top + new Vector3(.3f, 0, .08f), new Color(.36f, .44f, .4f), 1.2f);
                    if (ModelProp(t, "Medieval/Prefabs/BottleV1", top + new Vector3(.6f, 0, 0), 0, .28f) == null) Part(PrimitiveType.Cylinder, t, top + new Vector3(.6f, .12f, 0), new Vector3(.08f, .12f, .08f), CourtGlass);
                    break;
                case "cook":
                    for (int k = 0; k < 3; k++) if (ModelProp(t, k == 1 ? "Megapack/Models/Market/Bread_2" : "Megapack/Models/Market/Bread_1", top + new Vector3(-.55f + k * .22f, 0, (k % 2) * .1f), k * 40, .11f) == null) Part(PrimitiveType.Capsule, t, top + new Vector3(-.55f + k * .22f, .05f, 0), new Vector3(.12f, .12f, .1f), Tint(art.hay, new Color(.7f, .48f, .26f)), Quaternion.Euler(0, 0, 90));
                    if (ModelProp(t, "Megapack/Models/Market/Cheese_1", top + new Vector3(.15f, 0, 0), 0, .15f) == null) Part(PrimitiveType.Cylinder, t, top + new Vector3(.15f, .07f, 0), new Vector3(.3f, .07f, .3f), Tint(art.hay, new Color(.92f, .76f, .36f)));
                    MeshPart(PropMesh("Herb basket", () => Turned(new[] { new Vector2(0, 0), new Vector2(.2f, 0), new Vector2(.27f, .26f), new Vector2(.24f, .26f), new Vector2(.18f, .04f), new Vector2(0, .04f) }, 12, .3f)), t, top + new Vector3(.55f, 0, 0), art.hay).transform.localScale = Vector3.one * .7f;
                    for (int k = 0; k < 5; k++) Part(PrimitiveType.Sphere, t, top + new Vector3(.55f + (k % 3 - 1) * .06f, .12f + (k / 3) * .04f, (k % 2) * .05f - .02f), new Vector3(.06f, .075f, .06f), Tint(art.plaster, new Color(.9f, .84f, .7f)));   // eggs
                    break;
                case "bowyer":
                    for (int k = 0; k < 9; k++) Part(PrimitiveType.Cylinder, t, top + new Vector3(-.45f + (k % 3) * .015f, .03f + (k / 3) * .02f, -.05f + (k % 3) * .03f), new Vector3(.012f, .38f, .012f), Tint(art.timber, new Color(.7f, .56f, .36f)), Quaternion.Euler(0, 0, 90));   // arrow shafts
                    for (int k = 0; k < 2; k++) Part(PrimitiveType.Cylinder, t, top + new Vector3(.2f, .03f + k * .05f, -.08f + k * .12f), new Vector3(.04f, .75f, .04f), Tint(art.timber, new Color(.62f, .42f, .22f)), Quaternion.Euler(0, 5 - k * 10, 90));   // staves
                    Part(PrimitiveType.Cylinder, t, top + new Vector3(.62f, .02f, .12f), new Vector3(.12f, .02f, .12f), Tint(art.cloth, new Color(.86f, .82f, .7f)));   // a coil of bowstring
                    break;
                case "miner":
                    for (int k = 0; k < 8; k++) Lump(BoulderAt(k), t, top + new Vector3(-.5f + (k % 4) * .12f, .05f + (k / 4) * .05f, (k % 2) * .1f - .05f), new Vector3(.12f, .09f, .11f), RockTint(CourtOres[k % CourtOres.Length]), k * 41);
                    Part(PrimitiveType.Cube, t, top + new Vector3(.3f, .03f, 0), new Vector3(.5f, .04f, .04f), DarkOak, Quaternion.Euler(0, -15, 0));
                    Part(PrimitiveType.Cube, t, top + new Vector3(.52f, .05f, -.06f), new Vector3(.06f, .06f, .3f), iron, Quaternion.Euler(0, -15, 0));
                    break;
                default:
                    for (int k = 0; k < 5; k++) Part(PrimitiveType.Capsule, t, top + new Vector3(-.6f + k * .14f, .04f, (k % 2) * .08f - .04f), new Vector3(.08f, .16f, .08f), Tint(art.foliage, new Color(.36f + k * .03f, .5f, .24f)), Quaternion.Euler(0, 60 + k * 9, 90));   // cut bunches
                    MeshPart(PropMesh("Herb basket", () => Turned(new[] { new Vector2(0, 0), new Vector2(.2f, 0), new Vector2(.27f, .26f), new Vector2(.24f, .26f), new Vector2(.18f, .04f), new Vector2(0, .04f) }, 12, .3f)), t, top + new Vector3(.4f, 0, 0), art.hay).transform.localScale = Vector3.one * .8f;
                    Part(PrimitiveType.Sphere, t, top + new Vector3(.4f, .2f, 0), new Vector3(.36f, .14f, .36f), Tint(art.foliage, HerbHeads[1]));
                    break;
            }
        }

        // ---------- each trade's furnishings ----------
        /// <summary>The trade's own pieces in its workshop (round its station, which BuildStations draws, and its people's places).</summary>
        void CourtFurnish(Transform t, CourtTrade tr, int i, System.Random r)
        {
            var s = new Vector3(tr.stationAt.x, 0, tr.stationAt.y); var work = new Vector3(tr.workAt.x, 0, tr.workAt.y); float d = tr.d, w = tr.w, wall = d / 2 - .35f;
            var iron = CourtIron; var oak = DarkOak;
            // The station's footing made solid (BuildStations leaves its own without a collider, as every zone station).
            if (tr.station != null) Blocker(t, s + new Vector3(0, .45f, tr.id == "cook" ? .1f : 0), tr.id == "smith" ? new Vector3(.7f, .9f, .9f) : tr.id == "cook" ? new Vector3(1.7f, .9f, 1f) : tr.id == "weaver" ? new Vector3(1.8f, 1.6f, .5f) : new Vector3(1.8f, .9f, .66f));
            switch (tr.id)
            {
                case "smith":
                {
                    // The hearth against the back wall at the right: its stone, a bed of coals, the hood and the stack up through the roof.
                    var h = new Vector3(2.3f, 0, 1.55f);
                    SolidBox(t, h + new Vector3(0, .5f, 0), new Vector3(1.6f, 1, 1.2f), CityShade);
                    foreach (int k in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, h + new Vector3(0, 1.04f, k * .4f), new Vector3(1.3f, .08f, .1f), Tint(Masonry, new Color(.4f, .38f, .35f)));
                    var ember = CourtEmber; var coal = CourtCoal;
                    for (int k = 0; k < 14; k++) { float a = k * 2.39996f, rr = .07f * Mathf.Sqrt(k + 1), size = .13f + (k % 3) * .03f; Lump(BoulderAt(k % 6), t, h + new Vector3(Mathf.Cos(a) * rr * 1.5f, 1.05f, Mathf.Sin(a) * rr), new Vector3(size, size * .6f, size), k % 3 == 0 ? ember : coal, k * 47); }
                    Part(PrimitiveType.Cube, t, h + new Vector3(0, 1.62f, -.55f), new Vector3(1.5f, .14f, .16f), oak);   // the mantel
                    MeshPart(PropMesh("Forge hood", () => Turned(new[] { new Vector2(.99f, 0), new Vector2(.45f, .9f), new Vector2(.36f, .9f), new Vector2(.9f, 0) }, 4)), t, h + new Vector3(0, 1.62f, 0), Masonry, Quaternion.Euler(0, 45, 0));
                    BoxPart(t, h + new Vector3(0, 3.55f, 0), new Vector3(.64f, 2.1f, .64f), CityWhite, null, 1);
                    BoxPart(t, h + new Vector3(0, 4.66f, 0), new Vector3(.84f, .12f, .84f), CityShade, null, 1);
                    if (art.particle != null) Smoke(t, h + new Vector3(0, 4.9f, 0));
                    Glow(t, h + new Vector3(0, 1.5f, -.6f), 7, 1.3f, new Color(1, .5f, .2f), 2.1f);
                    // Hand bellows on the hearth's left end, worked by the apprentice.
                    if (ModelProp(t, "Megapack/Models/Bellows", h + new Vector3(-.6f, 1.0f, .2f), 90, .3f) == null)
                    {
                        Part(PrimitiveType.Cube, t, h + new Vector3(-.6f, 1.12f, .2f), new Vector3(.3f, .2f, .6f), HideOf(1));
                        Rod(t, h + new Vector3(-.6f, 1.12f, -.1f), h + new Vector3(-.4f, 1.12f, -.35f), .04f, iron);
                    }
                    // Tongs, hammers and files on a rail on the back wall, left of the hearth.
                    Part(PrimitiveType.Cube, t, new Vector3(-1.4f, 1.95f, wall - .03f), new Vector3(2.6f, .08f, .06f), oak);
                    for (int k = 0; k < 7; k++)
                    {
                        float x = -2.5f + k * .36f; Part(PrimitiveType.Cube, t, new Vector3(x, 1.93f, wall - .08f), new Vector3(.03f, .03f, .08f), iron);
                        switch (k % 3)
                        {
                            case 0: foreach (int s2 in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(x + s2 * .025f, 1.66f, wall - .08f), new Vector3(.025f, .52f, .02f), iron, Quaternion.Euler(0, 0, s2 * 4)); break;
                            case 1: Part(PrimitiveType.Cube, t, new Vector3(x, 1.72f, wall - .08f), new Vector3(.035f, .4f, .035f), art.timber); Part(PrimitiveType.Cube, t, new Vector3(x, 1.5f, wall - .08f), new Vector3(.15f, .065f, .065f), iron); break;
                            default: Part(PrimitiveType.Cube, t, new Vector3(x, 1.74f, wall - .08f), new Vector3(.03f, .36f, .015f), iron); break;
                        }
                    }
                    // Bars and ingots stacked on a bench along the left wall; a weapon stand of finished work at the front.
                    CityTable(t, new Vector3(-w / 2 + .75f, 0, 1.0f), 90, 1.5f, .6f);
                    for (int k = 0; k < 18; k++) Part(PrimitiveType.Cube, t, new Vector3(-w / 2 + .62f + (k % 3) * .11f, .86f + (k / 9) * .06f, .45f + ((k / 3) % 3) * .38f), new Vector3(.09f, .05f, .28f), Tint(art.metal, CourtMetals[(k / 3) % CourtMetals.Length]));
                    // A rack of finished work at the front left (the kit's weapon stand, or a painted rack of blades and axe heads when the
                    // kit is missing), solid; a bucket of coal by the hearth (the kit's, or a painted pail).
                    var rack = new Vector3(-w / 2 + .6f, 0, -.7f);
                    if (ModelProp(t, "Fantasy/WeaponStand", rack + Vector3.up * .05f, 90, 1.6f, true, .9f) == null)
                    {
                        foreach (int k in new[] { -1, 1 }) Stake(t, rack + new Vector3(0, -.05f, k * .42f), .1f, 1.5f, oak);
                        foreach (float y in new[] { .55f, 1.2f }) Part(PrimitiveType.Cube, t, rack + new Vector3(0, y, 0), new Vector3(.08f, .06f, .95f), oak);
                        for (int k = 0; k < 4; k++)
                        {
                            float z = -.3f + k * .2f;
                            Part(PrimitiveType.Cube, t, rack + new Vector3(.06f, .85f, z), new Vector3(.02f, .75f, .05f), iron, Quaternion.Euler(0, 0, 4));   // a blade
                            Part(PrimitiveType.Cube, t, rack + new Vector3(.06f, 1.27f, z), new Vector3(.03f, .1f, .14f), Tint(art.metal, CourtMetals[1 + k % 2]));   // its hilt
                        }
                        Blocker(t, rack + new Vector3(0, .75f, 0), new Vector3(.3f, 1.5f, .95f));
                    }
                    var pail = new Vector3(1.25f, .05f, .55f);
                    if (ModelProp(t, "Fantasy/Bucket_Metal", pail, 0, .38f) == null)
                    {
                        MeshPart(PropMesh("Coal pail", () => Turned(new[] { new Vector2(0, 0), new Vector2(.15f, 0), new Vector2(.18f, .36f), new Vector2(.16f, .36f), new Vector2(.14f, .04f), new Vector2(0, .04f) }, 10)), t, pail, iron);
                        Rod(t, pail + new Vector3(-.17f, .36f, 0), pail + new Vector3(.17f, .36f, 0), .015f, iron);
                    }
                        for (int k = 0; k < 4; k++) Lump(BoulderAt(k), t, new Vector3(1.25f + (k % 2 - .5f) * .1f, .36f, .55f + (k / 2 - .5f) * .1f), new Vector3(.1f, .06f, .1f), coal, k * 33);
                    break;
                }
                case "leather":
                {
                    // A hide laced in its stretching frame at the back right (the apprentice scrapes it), the trade bags on pegs over the bench,
                    // a tanning tub at the front left, rolled hides by the counter.
                    var f = work + new Vector3(0, 0, .1f); var pole = Tint(art.timber, new Color(.42f, .31f, .2f));
                    foreach (int k in new[] { -1, 1 }) Rod(t, f + new Vector3(k * .7f, -.05f, 0), f + new Vector3(k * .7f, 1.85f, 0), .07f, pole);
                    foreach (float y in new[] { .3f, 1.75f }) Rod(t, f + new Vector3(-.78f, y, 0), f + new Vector3(.78f, y, 0), .05f, pole);
                    Part(PrimitiveType.Cube, t, f + new Vector3(0, 1.02f, -.02f), new Vector3(1.05f, 1.15f, .03f), HideOf(r.Next(5)), Quaternion.Euler(0, 0, 3));
                    for (int k = 0; k < 5; k++) foreach (int sx in new[] { -1, 1 }) Rod(t, f + new Vector3(sx * .53f, .55f + k * .24f, -.02f), f + new Vector3(sx * .7f, .55f + k * .24f, 0), .01f, Tint(art.hay, new Color(.6f, .54f, .42f)));   // the lacing
                    Part(PrimitiveType.Cube, t, new Vector3(s.x, 2.02f, wall - .04f), new Vector3(1.9f, .06f, .05f), oak);
                    for (int k = 0; k < 4; k++) HungBag(t, new Vector3(s.x - .7f + k * .46f, 2.0f, wall - .1f), k);
                    var tub = new Vector3(-w / 2 + .75f, 0, -1.35f);
                    MeshPart(PropMesh("Tanning tub", () => Turned(new[] { new Vector2(0, 0), new Vector2(.42f, 0), new Vector2(.46f, .55f), new Vector2(.4f, .55f), new Vector2(.37f, .08f), new Vector2(0, .08f) }, 14, .5f)), t, tub, CourtPlank);
                    Part(PrimitiveType.Cylinder, t, tub + new Vector3(0, .45f, 0), new Vector3(.78f, .01f, .78f), Tint(art.cloth, new Color(.36f, .24f, .12f)));   // the tan liquor
                    foreach (float y in new[] { .12f, .45f }) Part(PrimitiveType.Cylinder, t, tub + new Vector3(0, y, 0), new Vector3(.9f + y * .1f, .02f, .9f + y * .1f), iron);
                    Rod(t, tub + new Vector3(-.1f, .4f, 0), tub + new Vector3(.25f, 1.15f, .1f), .04f, art.timber);   // a paddle in it
                    Blocker(t, tub + new Vector3(0, .3f, 0), new Vector3(.95f, .6f, .95f));
                    for (int k = 0; k < 2; k++) if (ModelProp(t, "Megapack/Models/Market/Skin_Roll_1", new Vector3(w / 2 - .45f, .04f, -.15f + k * .3f), k * 50, 1.0f) == null) Part(PrimitiveType.Cylinder, t, new Vector3(w / 2 - .45f, .5f, -.15f + k * .3f), new Vector3(.2f, .5f, .2f), HideOf(k));
                    break;
                }
                case "weaver":
                {
                    // Bolts of cloth on a shelf at the back right (the apprentice winds the yarn), dye pots at the front left, lengths of dyed
                    // cloth hung to dry along the left wall, standing rolls by the counter.
                    CourtShelf(t, work + new Vector3(0, 0, .25f), 1.4f, 3, (sh, y, k) =>
                    {
                        for (int b = 0; b < 4; b++) Part(PrimitiveType.Cylinder, sh, new Vector3(-.5f + b * .32f, y + .1f, 0), new Vector3(.18f, .26f, .18f), Tint(art.cloth, CourtDyes[(b + k) % CourtDyes.Length]), Quaternion.Euler(0, 0, 90));
                    });
                    var dye = new[] { new Vector3(-w / 2 + .7f, 0, -1.6f), new Vector3(-w / 2 + .7f, 0, -.95f), new Vector3(-w / 2 + 1.3f, 0, -1.75f) };
                    for (int k = 0; k < dye.Length; k++)
                    {
                        MeshPart(PropMesh("Dye pot", () => Turned(new[] { new Vector2(0, 0), new Vector2(.2f, 0), new Vector2(.27f, .2f), new Vector2(.24f, .42f), new Vector2(.27f, .46f), new Vector2(.22f, .46f), new Vector2(.2f, .4f), new Vector2(0, .4f) }, 12, .5f)), t, dye[k], Tint(art.stone, new Color(.52f, .36f, .26f)));
                        Part(PrimitiveType.Cylinder, t, dye[k] + new Vector3(0, .41f, 0), new Vector3(.4f, .01f, .4f), Tint(art.cloth, CourtDyes[k]));
                    }
                    Rod(t, dye[0] + new Vector3(.05f, .35f, 0), dye[0] + new Vector3(.2f, 1.0f, .15f), .03f, art.timber);   // a stirring stick
                    Blocker(t, new Vector3(-w / 2 + 1f, .25f, -1.35f), new Vector3(1.1f, .5f, 1.1f));
                    Rod(t, new Vector3(-w / 2 + .45f, 2.25f, .15f), new Vector3(-w / 2 + .45f, 2.25f, 1.9f), .05f, oak);
                    for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, t, new Vector3(-w / 2 + .45f, 1.75f, .45f + k * .55f), new Vector3(.03f, 1f, .45f), Tint(art.cloth, CourtDyes[(k + 1) % CourtDyes.Length]));
                    for (int k = 0; k < 2; k++) if (ModelProp(t, "Megapack/Models/Market/Material_Roll_2", new Vector3(w / 2 - .45f, .04f, -.15f + k * .3f), k * 70, 1.05f) == null) Part(PrimitiveType.Cylinder, t, new Vector3(w / 2 - .45f, .52f, -.15f + k * .3f), new Vector3(.22f, .5f, .22f), Tint(art.cloth, CourtDyes[k + 2]));
                    break;
                }
                case "simpler":
                {
                    // Jars and bottles on a shelf at the back right (the apprentice sorts them), herbs drying from a pole under the eaves, a
                    // cask of spirit at the front left.
                    CourtShelf(t, work + new Vector3(0, 0, .25f), 1.4f, 3, (sh, y, k) =>
                    {
                        for (int b = 0; b < 4; b++) Jar(sh, new Vector3(-.5f + b * .32f, y + .03f, 0), new Color(.4f + (b + k) % 3 * .1f, .38f + b * .04f, .3f), .9f + (b % 2) * .25f);
                    });
                    Rod(t, new Vector3(-w / 2 + .4f, 2.35f, -1.7f), new Vector3(-.2f, 2.35f, -1.7f), .04f, oak);
                    for (int k = 0; k < 6; k++) HerbBundle(t, new Vector3(-w / 2 + .6f + k * .4f, 2.33f, -1.7f), k + r.Next(3), .9f);
                    if (ModelProp(t, WoodBarrelPath, new Vector3(-w / 2 + .65f, .04f, -1.35f), 90, .8f) == null) Barrel(t, new Vector3(-w / 2 + .65f, .04f, -1.35f), .8f);   // the wood barrel (play note 247), .8 m as the beer barrel stood
                    Blocker(t, new Vector3(-w / 2 + .65f, .4f, -1.35f), new Vector3(.75f, .8f, .75f));
                    break;
                }
                case "cook":
                {
                    // The hearth's hood and stack up through the roof (BuildStations draws the hearth and its fire), hams, sausages and herbs
                    // hung from a beam across, the work table at the back right (the apprentice kneads), pans on the wall, a water barrel.
                    var h = s + new Vector3(0, 0, .25f);
                    Part(PrimitiveType.Cube, t, h + new Vector3(0, .95f, .55f), new Vector3(1.5f, 1.1f, .06f), Tint(Masonry, new Color(.3f, .28f, .26f)));   // the fireback
                    Part(PrimitiveType.Cube, t, h + new Vector3(0, 1.72f, -.42f), new Vector3(1.6f, .14f, .16f), DarkOak);
                    MeshPart(PropMesh("Forge hood", () => Turned(new[] { new Vector2(.99f, 0), new Vector2(.45f, .9f), new Vector2(.36f, .9f), new Vector2(.9f, 0) }, 4)), t, h + new Vector3(0, 1.72f, .1f), Masonry, Quaternion.Euler(0, 45, 0));
                    BoxPart(t, h + new Vector3(0, 3.6f, .1f), new Vector3(.62f, 2f, .62f), CityWhite, null, 1);
                    BoxPart(t, h + new Vector3(0, 4.66f, .1f), new Vector3(.82f, .12f, .82f), CityShade, null, 1);
                    if (art.particle != null) Smoke(t, h + new Vector3(0, 4.9f, .1f));
                    Part(PrimitiveType.Cube, t, new Vector3(0, 2.42f, .2f), new Vector3(w - .5f, .12f, .12f), oak);   // the beam across
                    var ham = Tint(art.cloth, new Color(.58f, .34f, .26f)); var cord = Tint(art.hay, new Color(.6f, .54f, .42f));
                    for (int k = 0; k < 7; k++)
                    {
                        float x = -w / 2 + .7f + k * .72f; var hook = new Vector3(x, 2.36f, .2f);
                        if (k % 3 == 0) { Part(PrimitiveType.Cube, t, hook + new Vector3(0, -.08f, 0), new Vector3(.015f, .16f, .015f), cord); Part(PrimitiveType.Capsule, t, hook + new Vector3(0, -.36f, 0), new Vector3(.2f, .2f, .17f), ham); }
                        else if (k % 3 == 1) { if (ModelProp(t, "Megapack/Models/Market/Sausage_2", hook + new Vector3(0, -.62f, 0), k * 30, .6f) == null) Part(PrimitiveType.Capsule, t, hook + new Vector3(0, -.3f, 0), new Vector3(.05f, .3f, .05f), ham); }
                        else HerbBundle(t, hook, k, .9f);
                    }
                    CityTable(t, new Vector3(work.x, 0, work.z - .05f), 0, 1.7f, .6f);
                    Part(PrimitiveType.Sphere, t, new Vector3(work.x - .2f, .86f, work.z - .1f), new Vector3(.36f, .1f, .28f), Tint(art.plaster, new Color(.93f, .88f, .76f)));   // the dough
                    Rod(t, new Vector3(work.x + .2f, .86f, work.z - .2f), new Vector3(work.x + .5f, .86f, work.z - .05f), .05f, art.timber);   // its pin
                    if (ModelProp(t, "Megapack/Models/Market/Plate", new Vector3(work.x - .65f, .83f, work.z), 0, .04f) == null) Part(PrimitiveType.Cylinder, t, new Vector3(work.x - .65f, .84f, work.z), new Vector3(.3f, .01f, .3f), Tint(art.stone, new Color(.8f, .76f, .7f)));
                    for (int k = 0; k < 3; k++) { var pan = new Vector3(work.x - .5f + k * .5f, 1.75f, wall - .06f); Part(PrimitiveType.Cylinder, t, pan, new Vector3(.3f - k * .04f, .015f, .3f - k * .04f), CourtIron, Quaternion.Euler(90, 0, 0)); Part(PrimitiveType.Cube, t, pan + new Vector3(0, .26f, 0), new Vector3(.03f, .24f, .015f), CourtIron); }
                    Barrel(t, new Vector3(-w / 2 + .6f, .04f, -1.4f), .8f, 30);
                    Part(PrimitiveType.Cylinder, t, new Vector3(-w / 2 + .6f, .8f, -1.4f), new Vector3(.46f, .005f, .46f), art.water != null ? art.water : art.glass);
                    Blocker(t, new Vector3(-w / 2 + .6f, .4f, -1.4f), new Vector3(.7f, .8f, .7f));
                    break;
                }
                case "bowyer":
                {
                    // Split staves and logs racked at the back right (the apprentice sorts them), bows on pegs over the bench, two axes leant on
                    // the left wall, a saw-horse with a log on it at the front left.
                    var bark = art.bark; var wood = Tint(art.timber, new Color(.66f, .5f, .32f));
                    foreach (int k in new[] { -1, 1 }) { Bar(t, work + new Vector3(k * .6f, 0, -.25f), work + new Vector3(k * .6f, .9f, .1f), .08f, .08f, DarkOak); Bar(t, work + new Vector3(k * .6f, 0, .35f), work + new Vector3(k * .6f, .9f, .1f), .08f, .08f, DarkOak); }
                    for (int k = 0; k < 7; k++) Part(PrimitiveType.Cylinder, t, work + new Vector3(0, .25f + (k / 3) * .2f, -.12f + (k % 3) * .14f), new Vector3(.15f - (k % 2) * .03f, .7f, .15f - (k % 2) * .03f), k % 2 == 0 ? bark : wood, Quaternion.Euler(0, 0, 90));
                    Blocker(t, work + new Vector3(0, .45f, .05f), new Vector3(1.5f, .9f, .7f));
                    Part(PrimitiveType.Cube, t, new Vector3(s.x, 2.0f, wall - .04f), new Vector3(1.9f, .06f, .05f), DarkOak);
                    for (int k = 0; k < 3; k++) Part(PrimitiveType.Cylinder, t, new Vector3(s.x - .6f + k * .6f, 1.45f, wall - .1f), new Vector3(.035f, .62f, .035f), Tint(art.timber, new Color(.62f - k * .05f, .42f, .22f)), Quaternion.Euler(0, 0, k * 6 - 6));
                    for (int k = 0; k < 2; k++) if (ModelProp(t, "Megapack/Models/Axe_1", new Vector3(-w / 2 + .5f, .04f, -.15f - k * .35f), 90, .9f) == null)
                    { Part(PrimitiveType.Cube, t, new Vector3(-w / 2 + .45f, .45f, -.15f - k * .35f), new Vector3(.04f, .9f, .04f), art.timber, Quaternion.Euler(0, 0, -8)); Part(PrimitiveType.Cube, t, new Vector3(-w / 2 + .45f, .88f, -.15f - k * .35f), new Vector3(.2f, .1f, .03f), CourtIron); }
                    var horse = new Vector3(-w / 2 + 1.05f, 0, -1.35f);
                    foreach (int k in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Bar(t, horse + new Vector3(k * .45f, 0, sz * .25f), horse + new Vector3(k * .45f, .75f, -sz * .1f), .06f, .06f, DarkOak);
                    Part(PrimitiveType.Cylinder, t, horse + new Vector3(0, .82f, 0), new Vector3(.24f, .7f, .24f), bark, Quaternion.Euler(0, 0, 90));
                    Part(PrimitiveType.Cube, t, horse + new Vector3(.1f, .97f, -.14f), new Vector3(.6f, .12f, .015f), CourtIron, Quaternion.Euler(0, 0, 12));   // a frame saw's blade, resting
                    Blocker(t, horse + new Vector3(0, .5f, 0), new Vector3(1.4f, 1f, .6f));
                    break;
                }
                case "miner":
                {
                    // The sorting table where the station stands in the others, a rack of picks at the back right (the apprentice sharpens),
                    // two crates of ore at the front left.
                    CityTable(t, s, 0, 1.7f, .7f);
                    for (int k = 0; k < 14; k++) Lump(BoulderAt(k), t, s + new Vector3(-.7f + (k % 7) * .2f, .87f + (k / 7) * .02f, (k / 7) * .25f - .12f), new Vector3(.13f, .09f, .12f), RockTint(CourtOres[(k + k / 7) % CourtOres.Length]), k * 37);
                    Part(PrimitiveType.Cylinder, t, s + new Vector3(.55f, .85f, .15f), new Vector3(.36f, .04f, .36f), CourtPlank);   // a sieve
                    var rack = work + new Vector3(0, 0, .15f);
                    Part(PrimitiveType.Cube, t, rack + new Vector3(0, 1.6f, 0), new Vector3(1.4f, .08f, .08f), DarkOak);
                    Part(PrimitiveType.Cube, t, rack + new Vector3(0, .25f, -.1f), new Vector3(1.4f, .08f, .08f), DarkOak);
                    for (int k = 0; k < 4; k++)
                    {
                        float x = -.55f + k * .37f;
                        Part(PrimitiveType.Cube, t, rack + new Vector3(x, .95f, -.06f), new Vector3(.05f, 1.3f, .05f), art.timber, Quaternion.Euler(-4, 0, 0));
                        Part(PrimitiveType.Cube, t, rack + new Vector3(x, 1.52f, -.08f), new Vector3(.44f, .07f, .07f), CourtIron, Quaternion.Euler(0, 0, k % 2 == 0 ? 8 : -8));
                    }
                    for (int k = 0; k < 2; k++)
                    {
                        var at = new Vector3(-w / 2 + .85f, k * .4f, -1.35f + k * .05f);
                        if (ModelProp(t, "Megapack/Models/Market/Crate_1", at, k * 8, .38f) == null) Crate(t, at + Vector3.up * .2f, .4f, k * 8);
                        for (int j = 0; j < 6; j++) Lump(BoulderAt(j + k), t, at + new Vector3(-.3f + (j % 3) * .3f, .36f, (j / 3) * .28f - .14f), new Vector3(.2f, .12f, .18f), RockTint(CourtOres[(j + k * 2) % CourtOres.Length]), j * 53 + k);
                    }
                    Blocker(t, new Vector3(-w / 2 + .85f, .45f, -1.33f), new Vector3(1f, .9f, .8f));
                    break;
                }
                default:
                {
                    // The herbalist's: a laden drying rack where the station stands in the others, baskets of herbs on a low bench at the back
                    // right (the apprentice sorts them), bunches hung under the front beam.
                    DryingRack(t, s + new Vector3(0, 0, .1f), 0, r.Next(5));
                    Blocker(t, s + new Vector3(0, .8f, .1f), new Vector3(2f, 1.6f, .85f));
                    Part(PrimitiveType.Cube, t, work + new Vector3(0, .4f, .1f), new Vector3(1.5f, .06f, .5f), CourtPlank);
                    foreach (int k in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, work + new Vector3(k * .65f, .2f, .1f), new Vector3(.06f, .4f, .45f), DarkOak);
                    for (int k = 0; k < 3; k++)
                    {
                        var b = work + new Vector3(-.5f + k * .5f, .43f, .1f);
                        MeshPart(PropMesh("Herb basket", () => Turned(new[] { new Vector2(0, 0), new Vector2(.2f, 0), new Vector2(.27f, .26f), new Vector2(.24f, .26f), new Vector2(.18f, .04f), new Vector2(0, .04f) }, 12, .3f)), t, b, art.hay);
                        Part(PrimitiveType.Sphere, t, b + new Vector3(0, .23f, 0), new Vector3(.44f, .2f, .44f), Tint(art.foliage, k == 1 ? HerbHeads[2] : new Color(.36f + k * .05f, .5f, .26f)));
                    }
                    Blocker(t, work + new Vector3(0, .35f, .1f), new Vector3(1.5f, .7f, .55f));
                    Rod(t, new Vector3(-w / 2 + .3f, 2.42f, -d / 2 + .35f), new Vector3(w / 2 - .3f, 2.42f, -d / 2 + .35f), .04f, DarkOak);
                    for (int k = 0; k < 9; k++) HerbBundle(t, new Vector3(-w / 2 + .6f + k * .6f, 2.4f, -d / 2 + .35f), k + r.Next(2), .85f);
                    break;
                }
            }
        }
        /// <summary>A shelf of three or four boards against the back wall (its back at +z of <paramref name="foot"/>), solid; fill lays out each
        /// board's things (its holder, the board's top, the board's index).</summary>
        void CourtShelf(Transform t, Vector3 foot, float len, int boards, Action<Transform, float, int> fill)
        {
            var sh = new GameObject("Shelf").transform; sh.SetParent(t, false); sh.localPosition = foot;
            foreach (int k in new[] { -1, 1 }) Part(PrimitiveType.Cube, sh, new Vector3(k * len / 2, .9f, 0), new Vector3(.06f, 1.8f, .36f), DarkOak);
            for (int k = 0; k < boards; k++) { float y = .3f + k * .55f; Part(PrimitiveType.Cube, sh, new Vector3(0, y, 0), new Vector3(len, .04f, .36f), CourtPlank); fill(sh, y + .02f, k); }
            Blocker(t, foot + new Vector3(0, .9f, 0), new Vector3(len + .06f, 1.8f, .4f));
        }

        // ---------- the stations (BuildStations: the zone's stations standing in a Court workshop; no colliders) ----------
        /// <summary>Draws a Court workshop's station as its trade's own, turned with its workshop (the class's summary); false for any
        /// other station.</summary>
        bool CourtStation(Transform t, ZoneStation s, Func<float> R)
        {
            var p = CourtWorkshopOf(Zone, s); if (p == null) return false;
            t.rotation = Quaternion.Euler(0, p.rotation, 0);
            switch (CourtTradeOf(p).id)
            {
                case "smith": CourtAnvil(t, R); break;
                case "leather": CourtCuttingBench(t, R); break;
                case "weaver": CourtLoom(t, R); break;
                case "simpler": CourtStill(t, R); break;
                case "cook": CourtHearth(t, R); break;
                default: CourtBowBench(t, R); break;
            }
            return true;
        }
        /// <summary>A bench top and four legs with a stretcher, its top at <paramref name="top"/> (len along x, wide along z).</summary>
        void CourtBenchFrame(Transform t, float len, float wide, float top, Material wood)
        {
            Part(PrimitiveType.Cube, t, new Vector3(0, top - .035f, 0), new Vector3(len, .07f, wide), wood);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * (len / 2 - .1f), (top - .07f) / 2, sz * (wide / 2 - .08f)), new Vector3(.08f, top - .07f, .08f), DarkOak);
            Part(PrimitiveType.Cube, t, new Vector3(0, .25f, 0), new Vector3(len - .2f, .05f, .06f), DarkOak);
        }
        /// <summary>The smith's anvil on a squared oak block bound in iron, a glowing bar held in tongs on its face, a hammer by it, more
        /// tongs against the block and a quench tub of water beside.</summary>
        void CourtAnvil(Transform t, Func<float> R)
        {
            var iron = CourtIron; const float top = .5f;
            Part(PrimitiveType.Cube, t, new Vector3(0, top / 2, 0), new Vector3(.52f, top, .52f), Tint(art.timber, new Color(.4f, .3f, .2f)), Quaternion.Euler(0, (R() - .5f) * 10, 0));
            foreach (float y in new[] { .1f, top - .08f }) Part(PrimitiveType.Cube, t, new Vector3(0, y, 0), new Vector3(.55f, .05f, .55f), iron);
            Part(PrimitiveType.Cube, t, new Vector3(0, top + .05f, 0), new Vector3(.34f, .1f, .5f), iron);
            Part(PrimitiveType.Cube, t, new Vector3(0, top + .16f, 0), new Vector3(.18f, .12f, .34f), iron);
            Part(PrimitiveType.Cube, t, new Vector3(0, top + .28f, -.04f), new Vector3(.3f, .12f, .6f), iron);
            MeshPart(PropMesh("Anvil horn", () => ZoneMeshes.Cone(.07f, .3f, 8)), t, new Vector3(0, top + .27f, .26f), iron, Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Cube, t, new Vector3(.02f, top + .36f, -.12f), new Vector3(.05f, .04f, .34f), Glowing(new Color(1, .45f, .15f), 2), Quaternion.Euler(0, 12, 0));   // the bar, hot
            foreach (int s in new[] { -1, 1 }) Bar(t, new Vector3(.05f + s * .02f, top + .37f, .06f), new Vector3(.12f + s * .03f, top + .3f, .5f), .025f, .02f, iron);   // the tongs holding it
            Rod(t, new Vector3(-.12f, top + .36f, -.3f), new Vector3(-.3f, top + .36f, -.05f), .03f, art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(-.1f, top + .375f, -.33f), new Vector3(.07f, .07f, .15f), iron, Quaternion.Euler(0, -36, 0));   // the hammer
            foreach (float x in new[] { -.32f, -.28f }) Bar(t, new Vector3(x, .02f, -.32f), new Vector3(x + .06f, top - .05f, -.2f), .03f, .02f, iron);
            // The quench tub.
            var tub = new Vector3(.75f, 0, .3f);
            MeshPart(PropMesh("Quench tub", () => Turned(new[] { new Vector2(0, 0), new Vector2(.28f, 0), new Vector2(.31f, .5f), new Vector2(.27f, .5f), new Vector2(.25f, .06f), new Vector2(0, .06f) }, 12, .5f)), t, tub, Tint(art.timber, new Color(.38f, .28f, .18f)));
            foreach (float y in new[] { .1f, .4f }) Part(PrimitiveType.Cylinder, t, tub + new Vector3(0, y, 0), new Vector3(.6f + y * .06f, .02f, .6f + y * .06f), iron);
            Part(PrimitiveType.Cylinder, t, tub + new Vector3(0, .44f, 0), new Vector3(.52f, .005f, .52f), art.water != null ? art.water : art.glass);
        }
        /// <summary>The still-table of the Simpler's craft: a bench, a little brazier of coals with a copper pot and its domed head, the pipe
        /// down to a glass flask cooling in a basin, stoppered vials, jars and a mortar.</summary>
        void CourtStill(Transform t, Func<float> R)
        {
            const float top = .85f; var copper = CourtCopper; var iron = CourtIron;
            CourtBenchFrame(t, 1.7f, .62f, top, CourtPlank);
            var b = new Vector3(-.45f, top, .05f);
            MeshPart(PropMesh("Still brazier", () => Turned(new[] { new Vector2(0, 0), new Vector2(.12f, 0), new Vector2(.17f, .1f), new Vector2(.15f, .1f), new Vector2(0, .03f) }, 10)), t, b, iron);
            for (int k = 0; k < 5; k++) Lump(BoulderAt(k), t, b + new Vector3((k % 3 - 1) * .05f, .08f, (k / 3) * .05f - .02f), new Vector3(.06f, .04f, .06f), k % 2 == 0 ? CourtEmber : CourtCoal, k * 51);
            MeshPart(PropMesh("Still pot", () => Turned(new[] { new Vector2(0, 0), new Vector2(.1f, 0), new Vector2(.15f, .08f), new Vector2(.15f, .2f), new Vector2(.08f, .26f), new Vector2(0, .26f) }, 12)), t, b + new Vector3(0, .12f, 0), copper);
            Part(PrimitiveType.Sphere, t, b + new Vector3(0, .4f, 0), new Vector3(.18f, .2f, .18f), copper);   // the head
            Rod(t, b + new Vector3(.06f, .45f, 0), new Vector3(.3f, top + .32f, .05f), .025f, copper);   // the pipe down to the flask
            var basin = new Vector3(.32f, top, .05f);
            MeshPart(PropMesh("Still basin", () => Turned(new[] { new Vector2(0, 0), new Vector2(.15f, 0), new Vector2(.18f, .12f), new Vector2(.16f, .12f), new Vector2(.14f, .02f), new Vector2(0, .02f) }, 12)), t, basin, Tint(art.stone, new Color(.62f, .6f, .56f)));
            Part(PrimitiveType.Cylinder, t, basin + new Vector3(0, .1f, 0), new Vector3(.3f, .005f, .3f), art.water != null ? art.water : art.glass);
            Part(PrimitiveType.Sphere, t, basin + new Vector3(0, .14f, 0), new Vector3(.17f, .17f, .17f), CourtGlass);
            Part(PrimitiveType.Cylinder, t, basin + new Vector3(0, .27f, 0), new Vector3(.04f, .06f, .04f), CourtGlass);
            for (int k = 0; k < 5; k++) { var v = new Vector3(.5f + k * .065f, top, -.2f); Part(PrimitiveType.Cylinder, t, v + new Vector3(0, .06f, 0), new Vector3(.045f, .06f, .045f), CourtGlass); Part(PrimitiveType.Cylinder, t, v + new Vector3(0, .13f, 0), new Vector3(.022f, .012f, .022f), Tint(art.timber, new Color(.6f, .46f, .3f))); }
            Jar(t, new Vector3(-.75f, top, .18f), new Color(.62f, .4f, .28f)); Jar(t, new Vector3(.68f, top, .18f), new Color(.36f, .44f, .4f), 1.15f);
            MeshPart(PropMesh("Mortar", () => Turned(new[] { new Vector2(0, 0), new Vector2(.08f, 0), new Vector2(.13f, .12f), new Vector2(.13f, .15f), new Vector2(.1f, .15f), new Vector2(.06f, .05f), new Vector2(0, .05f) }, 10)), t, new Vector3(-.05f, top, -.18f), Tint(art.stone, new Color(.62f, .6f, .56f)));
            Rod(t, new Vector3(-.05f, top + .07f, -.18f), new Vector3(.03f, top + .22f, -.1f), .035f, Tint(art.stone, new Color(.7f, .68f, .62f)));
            Glow(t, b + new Vector3(0, .3f, -.2f), 2.5f, .4f, new Color(1, .55f, .25f), .9f);
        }
        /// <summary>The cook's hearth: a raised bed of stone with a kerb, a fire of logs on its coals, an iron bar across on two standards with
        /// a pot on a hook and a kettle, a skillet and a ladle by it. Its fireback, hood and stack are the workshop's.</summary>
        void CourtHearth(Transform t, Func<float> R)
        {
            var iron = CourtIron; const float bed = .45f; var c = new Vector3(0, 0, .25f);
            Part(PrimitiveType.Cube, t, c + new Vector3(0, bed / 2, 0), new Vector3(1.6f, bed, 1f), CityShade);
            Part(PrimitiveType.Cube, t, c + new Vector3(0, bed + .04f, -.44f), new Vector3(1.6f, .08f, .12f), Tint(Masonry, new Color(.45f, .43f, .4f)));   // the kerb
            Part(PrimitiveType.Cube, t, c + new Vector3(0, bed + .005f, .05f), new Vector3(1.2f, .01f, .7f), Tint(art.stone, new Color(.22f, .21f, .2f)));   // the ash
            for (int k = 0; k < 10; k++) { float a = k * 2.39996f, rr = .05f * Mathf.Sqrt(k + 1); Lump(BoulderAt(k % 6), t, c + new Vector3(Mathf.Cos(a) * rr * 1.6f, bed + .03f, .05f + Mathf.Sin(a) * rr), new Vector3(.11f, .06f, .11f), k % 2 == 0 ? CourtEmber : CourtCoal, k * 61); }
            for (int k = 0; k < 3; k++) { var q = Quaternion.Euler(0, k * 120 + R() * 20, 0); Rod(t, c + new Vector3(0, bed + .04f, .05f) + q * new Vector3(0, 0, .3f), c + new Vector3(0, bed + .16f, .05f) + q * new Vector3(0, 0, .04f), .09f, art.bark); }
            var fire = Glow(t, c + new Vector3(0, bed + .45f, 0), 5, 1.1f, new Color(1, .55f, .25f), 1.9f);
            if (!RealFire(t, c + new Vector3(0, bed + .05f, .05f), .32f))
            {
                var tongue = PropMesh("Flame", () => ZoneMeshes.Cone(.1f, .34f, 6)); var flame = Glowing(new Color(1, .55f, .2f), 2.2f);
                for (int k = 0; k < 3; k++) { var lick = MeshPart(tongue, t, c + new Vector3((k - 1) * .07f, bed + .06f, .05f + (k % 2) * .06f - .03f), flame, Quaternion.Euler((k - 1) * 8, k * 50, (k - 1) * -10)); lick.transform.localScale = new Vector3(1, .8f + k % 2 * .45f, 1); CandleFlame.Attach(lick, fire); }
            }
            // The bar on its standards, the pot on its hook and the kettle.
            foreach (int s in new[] { -1, 1 }) Rod(t, c + new Vector3(s * .7f, bed, .05f), c + new Vector3(s * .7f, 1.3f, .05f), .04f, iron);
            Rod(t, c + new Vector3(-.75f, 1.28f, .05f), c + new Vector3(.75f, 1.28f, .05f), .035f, iron);
            Rod(t, c + new Vector3(-.2f, 1.28f, .05f), c + new Vector3(-.2f, .98f, .05f), .015f, iron);
            MeshPart(PropMesh("Cook pot", () => Turned(new[] { new Vector2(0, 0), new Vector2(.15f, 0), new Vector2(.23f, .1f), new Vector2(.23f, .24f), new Vector2(.19f, .3f), new Vector2(.21f, .33f), new Vector2(.17f, .33f), new Vector2(.17f, .27f), new Vector2(0, .27f) }, 10)), t, c + new Vector3(-.2f, .65f, .05f), iron);
            Part(PrimitiveType.Cylinder, t, c + new Vector3(-.2f, .935f, .05f), new Vector3(.34f, .006f, .34f), Tint(art.cloth, new Color(.46f, .3f, .16f)));   // the stew
            foreach (int s in new[] { -1, 1 }) Rod(t, c + new Vector3(-.2f, .98f, .05f), c + new Vector3(-.2f + s * .2f, .95f, .05f), .015f, iron);
            Part(PrimitiveType.Sphere, t, c + new Vector3(.4f, bed + .14f, .1f), new Vector3(.24f, .2f, .24f), iron);   // the kettle on the coals' edge
            Rod(t, c + new Vector3(.52f, bed + .16f, .1f), c + new Vector3(.62f, bed + .24f, .06f), .03f, iron);
            Part(PrimitiveType.Cylinder, t, c + new Vector3(.86f, .45f, -.35f), new Vector3(.3f, .015f, .3f), iron, Quaternion.Euler(80, 0, 10));   // a skillet leant on the bed
            Rod(t, c + new Vector3(-.85f, bed, -.4f), c + new Vector3(-.82f, bed + .35f, -.48f), .02f, art.timber);   // a ladle
            Part(PrimitiveType.Sphere, t, c + new Vector3(-.85f, bed + .03f, -.38f), new Vector3(.08f, .03f, .08f), iron);
        }
        /// <summary>The leatherworker's cutting bench: a hide spread on it with a piece cut from it, round knives and a straight one, an awl, a
        /// mallet, a spool of waxed thread, and straps over the end.</summary>
        void CourtCuttingBench(Transform t, Func<float> R)
        {
            const float top = .82f; var iron = CourtIron; var handle = Tint(art.timber, new Color(.4f, .3f, .2f));
            CourtBenchFrame(t, 1.8f, .7f, top, CourtPlank);
            Part(PrimitiveType.Cube, t, new Vector3(-.15f, top + .01f, .02f), new Vector3(1.1f, .015f, .6f), HideOf((int)(R() * 5)), Quaternion.Euler(0, (R() - .5f) * 8, 0));
            Part(PrimitiveType.Cube, t, new Vector3(.62f, top + .012f, -.1f), new Vector3(.3f, .015f, .22f), HideOf(2));
            Part(PrimitiveType.Cylinder, t, new Vector3(.3f, top + .03f, .18f), new Vector3(.16f, .005f, .16f), iron, Quaternion.Euler(0, 0, 0));   // a round knife
            Rod(t, new Vector3(.3f, top + .03f, .18f), new Vector3(.3f, top + .03f, .32f), .03f, handle);
            Part(PrimitiveType.Cube, t, new Vector3(.55f, top + .01f, .2f), new Vector3(.18f, .01f, .03f), iron); Part(PrimitiveType.Cube, t, new Vector3(.7f, top + .02f, .2f), new Vector3(.12f, .03f, .035f), handle);
            Part(PrimitiveType.Cylinder, t, new Vector3(.75f, top + .03f, .05f), new Vector3(.03f, .03f, .03f), handle, Quaternion.Euler(0, 0, 90)); Part(PrimitiveType.Cube, t, new Vector3(.82f, top + .03f, .05f), new Vector3(.08f, .006f, .006f), iron);   // the awl
            Part(PrimitiveType.Cylinder, t, new Vector3(-.75f, top + .06f, -.2f), new Vector3(.09f, .06f, .09f), Tint(art.timber, new Color(.62f, .5f, .34f)), Quaternion.Euler(0, 0, 90)); Rod(t, new Vector3(-.75f, top + .06f, -.2f), new Vector3(-.75f, top + .04f, .05f), .03f, handle);   // the mallet
            Part(PrimitiveType.Cylinder, t, new Vector3(-.75f, top + .05f, .2f), new Vector3(.08f, .05f, .08f), Tint(art.cloth, new Color(.86f, .8f, .6f)));   // the spool
            for (int k = 0; k < 3; k++) Part(PrimitiveType.Cube, t, new Vector3(-.88f, top - .25f, -.15f + k * .1f), new Vector3(.015f, .5f, .04f), HideOf(k + 1));   // straps over the end
        }
        /// <summary>A hand loom of the old kind (no machine): two uprights leant back a little with the cloth beam across their tops, the
        /// woven cloth down from it in the trade's dyes, the warp threads below held taut by clay weights, the shed and heddle rods on their
        /// forks, a stool before it with the shuttle on it and a basket of yarn.</summary>
        void CourtLoom(Transform t, Func<float> R)
        {
            var oak = DarkOak; var clay = Tint(art.stone, new Color(.6f, .42f, .3f)); var warp = Tint(art.cloth, new Color(.88f, .84f, .72f));
            foreach (int s in new[] { -1, 1 }) Bar(t, new Vector3(s * .8f, 0, .45f), new Vector3(s * .8f, 2.05f, .15f), .1f, .1f, oak);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 2.0f, .15f), new Vector3(.12f, .9f, .12f), oak, Quaternion.Euler(0, 0, 90));
            int dye = (int)(R() * CourtDyes.Length);
            for (int k = 0; k < 4; k++) Part(PrimitiveType.Cube, t, new Vector3(0, 1.88f - k * .22f, .17f + k * .03f), new Vector3(1.4f, .22f, .02f), Tint(art.cloth, CourtDyes[(dye + (k % 2) * 2) % CourtDyes.Length]), Quaternion.Euler(-8, 0, 0));
            for (int k = 0; k < 14; k++)
            {
                float x = -.65f + k * .1f;
                Part(PrimitiveType.Cube, t, new Vector3(x, .75f, .32f), new Vector3(.008f, .8f, .008f), warp, Quaternion.Euler(-8, 0, 0));
                if (k % 2 == 0) Part(PrimitiveType.Sphere, t, new Vector3(x + .05f, .3f, .38f), Vector3.one * .1f, clay);   // a weight on each pair
            }
            foreach (var (y, z) in new[] { (.95f, .27f), (.8f, .2f) }) Rod(t, new Vector3(-.82f, y, z), new Vector3(.82f, y, z), .035f, oak);   // the shed rod and the heddle rod
            // The stool, the shuttle and the yarn.
            var st = new Vector3(0, 0, -.5f);
            Part(PrimitiveType.Cylinder, t, st + new Vector3(0, .45f, 0), new Vector3(.38f, .03f, .38f), CourtPlank);
            for (int k = 0; k < 3; k++) { float a = k * 2.094f; Rod(t, st + new Vector3(Mathf.Cos(a) * .15f, .43f, Mathf.Sin(a) * .15f), st + new Vector3(Mathf.Cos(a) * .2f, 0, Mathf.Sin(a) * .2f), .04f, oak); }
            Part(PrimitiveType.Capsule, t, st + new Vector3(0, .5f, 0), new Vector3(.05f, .14f, .05f), Tint(art.timber, new Color(.62f, .5f, .34f)), Quaternion.Euler(0, 10, 90));
            var basket = new Vector3(.6f, 0, -.4f);
            MeshPart(PropMesh("Herb basket", () => Turned(new[] { new Vector2(0, 0), new Vector2(.2f, 0), new Vector2(.27f, .26f), new Vector2(.24f, .26f), new Vector2(.18f, .04f), new Vector2(0, .04f) }, 12, .3f)), t, basket, art.hay);
            for (int k = 0; k < 4; k++) Part(PrimitiveType.Sphere, t, basket + new Vector3((k % 2 - .5f) * .14f, .24f + (k / 2) * .06f, (k / 2 - .5f) * .1f), Vector3.one * .13f, Tint(art.cloth, CourtDyes[k % CourtDyes.Length]));
        }
        /// <summary>The bowyer's bench: staves laid along it, one half-shaped held under a peg, a drawknife and its shavings, a pot of glue, a
        /// strung bow leant at its end and a sheaf of arrow shafts.</summary>
        void CourtBowBench(Transform t, Func<float> R)
        {
            const float top = .8f; var iron = CourtIron; var yew = Tint(art.timber, new Color(.66f, .44f, .24f)); var pale = Tint(art.timber, new Color(.82f, .7f, .5f));
            CourtBenchFrame(t, 1.9f, .6f, top, CourtPlank);
            for (int k = 0; k < 3; k++) Part(PrimitiveType.Cylinder, t, new Vector3(-.05f, top + .03f, -.15f + k * .12f), new Vector3(.045f - k * .005f, .82f, .045f), k == 0 ? pale : yew, Quaternion.Euler(0, (R() - .5f) * 6, 90));
            Part(PrimitiveType.Cube, t, new Vector3(-.75f, top + .08f, -.15f), new Vector3(.08f, .1f, .12f), DarkOak);   // the peg holding the stave
            Part(PrimitiveType.Cube, t, new Vector3(.45f, top + .02f, .18f), new Vector3(.3f, .015f, .04f), iron);   // the drawknife
            foreach (int s in new[] { -1, 1 }) Rod(t, new Vector3(.45f + s * .15f, top + .03f, .18f), new Vector3(.45f + s * .22f, top + .03f, .1f), .03f, Tint(art.timber, new Color(.4f, .3f, .2f)));
            for (int k = 0; k < 7; k++) Part(PrimitiveType.Cube, t, new Vector3(.1f + R() * .7f, top + .005f, -.2f + R() * .4f), new Vector3(.08f, .006f, .02f), pale, Quaternion.Euler(0, R() * 180, 0));   // shavings
            for (int k = 0; k < 5; k++) Part(PrimitiveType.Cube, t, new Vector3(R() * 1.4f - .7f, .006f, -.4f + R() * .2f), new Vector3(.08f, .006f, .02f), pale, Quaternion.Euler(0, R() * 180, 0));
            Part(PrimitiveType.Cylinder, t, new Vector3(.8f, top + .06f, -.15f), new Vector3(.12f, .06f, .12f), Tint(art.stone, new Color(.5f, .38f, .26f)));   // the glue pot
            // A strung bow leant at the bench's end: three staves bent to an arc and its string.
            var foot = new Vector3(1.05f, 0, .1f);
            for (int k = -1; k <= 1; k++) Part(PrimitiveType.Cube, t, foot + new Vector3(0, .8f + k * .55f, -.08f - (1 - Mathf.Abs(k)) * .12f), new Vector3(.04f, .58f, .04f), yew, Quaternion.Euler(k * 20 - 6, 0, 0));
            Rod(t, foot + new Vector3(0, .05f, -.02f), foot + new Vector3(0, 1.55f, -.12f), .008f, Tint(art.cloth, new Color(.9f, .88f, .8f)));
            for (int k = 0; k < 8; k++) Rod(t, new Vector3(-1.05f + (k % 3) * .03f, 0, .05f + (k / 3) * .03f), new Vector3(-1.0f + (k % 4) * .04f, .78f, .12f + (k % 2) * .03f), .012f, pale);   // arrow shafts in a sheaf
        }

        // ---------- the court ----------
        /// <summary>The court's well (GAME-ONLY): a ring of white stone, its water, two posts and a bar with a rope over it to a bucket on the
        /// rim (drawn by hand: no windlass), a stone trough before it and a bucket by it. Solid; the water-carriers and gardeners come to it.</summary>
        void CourtWell(Transform t)
        {
            var ring = new Vector3(0, 0, .7f); var iron = CourtIron;
            MeshPart(PropMesh("Court well ring", () => Turned(new[] { new Vector2(.95f, 0), new Vector2(.95f, .8f), new Vector2(.7f, .8f), new Vector2(.7f, .2f), new Vector2(0, .2f) }, 20, 1.2f)), t, ring, CityWhite);
            Part(PrimitiveType.Cylinder, t, ring + new Vector3(0, .84f, 0), new Vector3(2f, .04f, 2f), Gilt).transform.localScale = new Vector3(1.96f, .03f, 1.96f);
            Part(PrimitiveType.Cylinder, t, ring + new Vector3(0, .45f, 0), new Vector3(1.38f, .005f, 1.38f), art.water != null ? art.water : art.glass);
            foreach (int s in new[] { -1, 1 }) Stake(t, ring + new Vector3(s * .82f, .75f, 0), .16f, 1.6f, DarkOak);
            Rod(t, ring + new Vector3(-.9f, 2.25f, 0), ring + new Vector3(.9f, 2.25f, 0), .1f, DarkOak);
            Rod(t, ring + new Vector3(.2f, 2.25f, 0), ring + new Vector3(.62f, .95f, -.55f), .015f, Tint(art.hay, new Color(.6f, .54f, .42f)));   // the rope, over the bar to the bucket
            if (ModelProp(t, "Megapack/Models/Bucket_1", ring + new Vector3(.62f, .8f, -.62f), 0, .38f) == null) Part(PrimitiveType.Cylinder, t, ring + new Vector3(.62f, .98f, -.62f), new Vector3(.3f, .18f, .3f), CourtPlank);
            Blocker(t, ring + new Vector3(0, .45f, 0), new Vector3(1.9f, .9f, 1.9f));
            // The trough.
            var tr = new Vector3(0, 0, -1.05f);
            Stonework("Court trough", t, CityShade, Ashlar(tr + new Vector3(0, .07f, 0), new Vector3(2f, .14f, .56f), 1), Ashlar(tr + new Vector3(0, .36f, -.25f), new Vector3(2f, .46f, .07f), 1),
                Ashlar(tr + new Vector3(0, .36f, .25f), new Vector3(2f, .46f, .07f), 1), Ashlar(tr + new Vector3(-.97f, .36f, 0), new Vector3(.07f, .46f, .44f), 1), Ashlar(tr + new Vector3(.97f, .36f, 0), new Vector3(.07f, .46f, .44f), 1));
            Part(PrimitiveType.Cube, t, tr + new Vector3(0, .5f, 0), new Vector3(1.86f, .02f, .44f), art.water != null ? art.water : art.glass);
            Blocker(t, tr + new Vector3(0, .3f, 0), new Vector3(2.05f, .6f, .6f));
            if (ModelProp(t, "Megapack/Models/Bucket_1", tr + new Vector3(1.35f, 0, .1f), 30, .38f) == null) Part(PrimitiveType.Cylinder, t, tr + new Vector3(1.35f, .19f, .1f), new Vector3(.3f, .19f, .3f), CourtPlank);
            foreach (int s in new[] { -1, 1 }) Workplace(t, "well", new Vector3(s * 1.55f, 0, .7f), ring + Vector3.up * .8f);
        }
        /// <summary>The board of crafting orders (GAME-ONLY): the notice board with its pent roof, more orders pinned on it and a gilt crown
        /// over them (the Council's commissions among them). Solid at its posts. Read-only scenery.</summary>
        void CourtBoard(Transform t)
        {
            NoticeBoard(t);
            var paper = Tint(art.timber, new Color(.86f, .8f, .66f));
            foreach (var (x, y, tilt) in new[] { (-.25f, 1.08f, 5f), (.62f, 1.18f, -4f), (-.65f, 1.05f, -2f) }) Part(PrimitiveType.Cube, t, new Vector3(x, y, -.045f), new Vector3(.26f, .3f, .01f), paper, Quaternion.Euler(0, 0, tilt));
            CrownDevice(t, new Vector3(0, 1.7f, -.07f), 0, .2f, Gilt);
            Blocker(t, new Vector3(0, 1f, 0), new Vector3(1.7f, 2f, .3f));
        }
        /// <summary>Goods stacked by a workshop, solid: 0 crates and a sack of charcoal (the smith's), 1 crates of ore, 2 barrels, 3 sacks
        /// and standing bolts (the weaver's), 4 a stack of firewood (the cook's), 5 a mix of crates and a barrel.</summary>
        void CourtGoods(Transform t, int variant)
        {
            var r = CityRandom(t, 0x600d5 + variant);
            switch (variant)
            {
                case 1:
                    for (int k = 0; k < 2; k++) { var at = new Vector3(k * .2f - .1f, k * .4f, k * .05f); if (ModelProp(t, "Megapack/Models/Market/Crate_1", at, k * 8 - 4, .38f) == null) Crate(t, at + Vector3.up * .2f, .4f); for (int j = 0; j < 6; j++) Lump(BoulderAt(j), t, at + new Vector3(-.3f + (j % 3) * .3f, .36f, (j / 3) * .28f - .14f), new Vector3(.2f, .12f, .18f), RockTint(CourtOres[(j + k) % CourtOres.Length]), j * 41 + k); }
                    break;
                case 2:
                    for (int k = 0; k < 3; k++) Barrel(t, new Vector3(k * .72f - .72f, 0, (k % 2) * .3f - .15f), k == 1 ? .9f : 1, Rf(r) * 360);
                    break;
                case 3:
                    for (int k = 0; k < 3; k++) if (ModelProp(t, k == 1 ? "Megapack/Models/Bag_2" : "Megapack/Models/Bag_1", new Vector3(k * .45f - .45f, 0, (k % 2) * .2f), Rf(r) * 360, k == 1 ? .6f : .85f) == null) Part(PrimitiveType.Sphere, t, new Vector3(k * .45f - .45f, .4f, 0), new Vector3(.5f, .8f, .45f), Tint(art.cloth, new Color(.5f, .44f, .34f)));
                    for (int k = 0; k < 2; k++) if (ModelProp(t, "Megapack/Models/Market/Material_Roll_2", new Vector3(.55f, 0, -.3f + k * .25f), k * 40, 1.05f) == null) Part(PrimitiveType.Cylinder, t, new Vector3(.55f, .52f, -.3f + k * .25f), new Vector3(.22f, .5f, .22f), Tint(art.cloth, CourtDyes[k]));
                    break;
                case 4:
                    if (ModelProp(t, "Medieval/Prefabs/PileOfWoodV1", Vector3.zero, 0, .7f, false, 1.3f) == null)
                        for (int row = 0; row < 3; row++) for (int k = 0; k < 4 - row; k++) Part(PrimitiveType.Cylinder, t, new Vector3(-.45f + row * .15f + k * .3f, .15f + row * .26f, 0), new Vector3(.26f, .45f, .26f), art.bark, Quaternion.Euler(90, 0, 0));
                    break;
                case 5:
                    Crate(t, new Vector3(-.35f, .41f, 0), .82f, Rf(r) * 20 - 10); Barrel(t, new Vector3(.45f, 0, .1f), .9f, Rf(r) * 360);
                    break;
                default:
                    Crate(t, new Vector3(-.35f, .41f, 0), .82f, Rf(r) * 20 - 10); Crate(t, new Vector3(-.3f, 1.13f, .02f), .6f, Rf(r) * 30);
                    if (ModelProp(t, "Megapack/Models/Bag_1", new Vector3(.4f, 0, 0), Rf(r) * 360, .8f) == null) Part(PrimitiveType.Sphere, t, new Vector3(.4f, .4f, 0), new Vector3(.5f, .8f, .45f), Tint(art.cloth, new Color(.3f, .28f, .26f)));
                    for (int k = 0; k < 4; k++) Lump(BoulderAt(k), t, new Vector3(.4f + (k % 2 - .5f) * .12f, .82f, (k / 2 - .5f) * .1f), new Vector3(.1f, .06f, .1f), CourtCoal, k * 29);
                    break;
            }
            Blocker(t, new Vector3(0, .5f, 0), new Vector3(1.3f, 1f, 1f));
        }
    }
}
