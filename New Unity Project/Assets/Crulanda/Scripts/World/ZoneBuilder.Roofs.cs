using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The roofs of the Tenement Rows (Docs/REWORK_LOWTOWNS_HALLOWS.md, Lowtowns pick 3; Docs/ZONE_LOWTOWNS.md "The roofs").
    /// CANON-EXPANDED: the books give the seed and the game grows it. Valen goes up "a rusted rain-spout" and over the rooftop edge
    /// (book1 ch.1:297), a fire escape (ch.1:261), laundry strung between two tenement blocks (ch.1:225; ch.7:35), and from above the
    /// steep slate, brick chimneys, iron gutters and "a precarious water tower" (ch.7:35-41).
    /// The game, over Pell Row in the Tenement Rows (the route is <see cref="RoofRoute"/>, worked out from the zone's data so a test can
    /// walk it without building the zone):
    /// - four terraces of three storeys (<see cref="RoofRow"/>), three in the row north of Pell Row and one across it;
    /// - on each, a lead walk along the front gutter (GAME-ONLY: lead flats on iron brackets over the iron gutter, a low rail at the
    ///   lane side), in the navmesh (NavWalkable) so you and the party walk them; the slope behind is a wall you cannot climb;
    /// - rope bridges (GAME-ONLY) of planks and rope over the two alleys between the row's terraces and over Pell Row itself;
    /// - up by the rusted rain-spout at the row's west end and by a fire escape at the far terrace's east end (E: you climb, the party
    ///   with you);
    /// - down through the washing lines (E on the walk where a line hangs below): a short drop into the lane that does no harm, the
    ///   laundry breaking the fall;
    /// - a precarious water tower on Tolley's roof (a wooden barrel on timber legs, leaning, guyed to a chimney), and a view from it.
    /// No machines: brick, slate, lead, iron, timber, rope and cloth.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The terraces the route crosses, by where they stand in the zone's data: west to east along the north side of Pell Row,
        /// then the one across the lane (all three storeys, so their gutters stand level).</summary>
        public static readonly Vector2[] RoofRow = { new Vector2(-170, -133), new Vector2(-150, -132), new Vector2(-130, -136), new Vector2(-123, -153) };
        /// <summary>Where the rope bridge crosses Pell Row (its x).</summary>
        public const float LaneBridgeX = -126;
        /// <summary>How far over a terrace's eaves the lead walk's top stands (over the terrace's solid block and its navmesh cut).</summary>
        public const float RoofDeckLift = .5f;
        /// <summary>The lead walk's width and the rope bridges' (both wider than an agent, radius .5, so the navmesh holds a lane).</summary>
        public const float RoofDeckWide = 2f, RoofBridgeWide = 1.8f;
        public const string SpoutName = "The rusted rain-spout", EscapeName = "The fire escape", TowerName = "The water tower", LaundryName = "The washing lines";
        /// <summary>The route's label (Docs/REWORK_LOWTOWNS_HALLOWS.md, the rules on labels).</summary>
        public const string RoofsCanon = "CANON-EXPANDED (book1 ch.1:225 laundry between tenement blocks, ch.1:261 a fire escape, ch.1:297 a rusted rain-spout over the rooftop edge; ch.7:35-41 steep slate, brick chimneys, iron gutters, a precarious water tower); the lead walks along the gutters and the rope bridges GAME-ONLY";

        /// <summary>The storeys of a tenement of a variant (odd four, even three) and its eaves over its foot (ZoneBuilder.Tenement).</summary>
        public static int TenementStoreys(int variant) { return 3 + Mathf.Abs(variant) % 2; }
        public static float TenementEaves(int variant) { return .5f + TenementStoreys(variant) * 3; }
        /// <summary>A tenement's roof from its eaves to its ridge, for its depth front to back.</summary>
        public static float TenementRoofRise(float depth) { return Mathf.Max(2, depth * .42f); }

        /// <summary>One terrace of the route: where it stands and how it is turned, and its lead walk along the front gutter.</summary>
        public sealed class RoofTop
        {
            public ZoneProp prop; public Vector3 root; public Quaternion rot; public Vector2 size; public float eaves;
            public Vector3 World(float x, float y, float z) { return root + rot * new Vector3(x, y, z); }
            /// <summary>The walk's middle line, out from the front wall (front -z) over the gutter.</summary>
            public float DeckZ { get { return -(size.y / 2 + .4f); } }
            public float DeckTop { get { return eaves + RoofDeckLift; } }
            public Vector3 Deck(float x) { return World(x, DeckTop, DeckZ); }
            /// <summary>Out from the front, into the lane.</summary>
            public Vector3 Out { get { return rot * Vector3.back; } }
            /// <summary>The walk's local x under a world x.</summary>
            public float LocalX(float wx) { var right = rot * Vector3.right; var fwd = rot * Vector3.forward; return (wx - root.x - DeckZ * fwd.x) / right.x; }
            /// <summary>Whether a world point stands within its walls (in plan).</summary>
            public bool Within(Vector3 p, float pad) { var l = Quaternion.Inverse(rot) * (p - root); return Mathf.Abs(l.x) <= size.x / 2 + pad && Mathf.Abs(l.z) <= size.y / 2 + pad; }
        }
        /// <summary>
        /// One leg of the roof route. Kinds: "deck" (a lead walk, a to b), "bridge" (a rope bridge, a to b), "climb" (a on the lane, b on a
        /// walk: the rain-spout or the fire escape), "drop" (a on a walk, b on the lane: through the washing), "tower" (a on the walk at the
        /// tower's foot, b its top).
        /// </summary>
        public sealed class RoofLeg
        {
            public string kind, name; public Vector3 a, b; public float width; public int roof = -1;
            /// <summary>For a drop: a washing line is strung under it by the builder (else one stands there in the zone's data).</summary>
            public bool ownLine;
            public float Length { get { return Vector3.Distance(a, b); } }
            /// <summary>How far a point is from this leg's middle line in plan, and its height off it there.</summary>
            public float Off(Vector3 p, out float dy)
            {
                var a2 = new Vector2(a.x, a.z); var ab = new Vector2(b.x, b.z) - a2; var q = new Vector2(p.x, p.z);
                float t = ab.sqrMagnitude < 1e-6f ? 0 : Mathf.Clamp01(Vector2.Dot(q - a2, ab) / ab.sqrMagnitude);
                dy = p.y - Mathf.Lerp(a.y, b.y, t);
                return Vector2.Distance(q, a2 + ab * t);
            }
        }
        /// <summary>The route as built: its terraces and legs, and what E does at each of its interactables.</summary>
        public sealed class RoofSpot
        {
            public List<RoofTop> roofs; public List<RoofLeg> legs;
            public readonly Dictionary<ZoneInteractable, RoofLeg> things = new Dictionary<ZoneInteractable, RoofLeg>();
        }
        /// <summary>The Lowtowns' roofs, or null in any other zone.</summary>
        public RoofSpot LowtownRoofs { get; private set; }

        /// <summary>The terraces of <see cref="RoofRow"/> in a zone's data, on the ground <paramref name="ground"/> gives; null if any is missing.</summary>
        public static List<RoofTop> RoofTops(ZoneDefinition z, System.Func<float, float, float> ground)
        {
            if (z == null || z.props == null) return null;
            var list = new List<RoofTop>();
            foreach (var at in RoofRow)
            {
                ZoneProp found = null;
                foreach (var p in z.props) if (p != null && p.kind == "tenement" && Vector2.Distance(p.at, at) < .6f) { found = p; break; }
                if (found == null) return null;
                list.Add(new RoofTop { prop = found, root = new Vector3(found.at.x, ground(found.at.x, found.at.y), found.at.y), rot = Quaternion.Euler(0, found.rotation, 0), size = LowtownSize(found), eaves = TenementEaves(found.variant) });
            }
            return list;
        }

        /// <summary>The roof route over Pell Row, worked out from the zone's data (null if its terraces are not there).</summary>
        public static List<RoofLeg> RoofRoute(ZoneDefinition z, System.Func<float, float, float> ground)
        {
            var tops = RoofTops(z, ground); if (tops == null) return null;
            var legs = new List<RoofLeg>();
            var decks = new List<RoofLeg>();
            for (int i = 0; i < tops.Count; i++)
            {
                var t = tops[i]; float hw = t.size.x / 2 - .3f;
                Vector3 a = t.Deck(-hw), b = t.Deck(hw);
                // West to east.
                if (a.x > b.x) { var s = a; a = b; b = s; }
                var name = string.IsNullOrEmpty(t.prop.name) || t.prop.name == "tenement" ? "A tenement's gutter" : t.prop.name + "'s gutter";
                var d = new RoofLeg { kind = "deck", name = name, a = a, b = b, width = RoofDeckWide, roof = i };
                decks.Add(d); legs.Add(d);
            }
            // Rope bridges over the two alleys between the row's terraces, gutter end to gutter end.
            for (int i = 0; i + 1 < 3; i++) legs.Add(new RoofLeg { kind = "bridge", name = "A rope bridge", a = decks[i].b, b = decks[i + 1].a, width = RoofBridgeWide });
            // And over Pell Row, from the row's third terrace to the one across the lane.
            legs.Add(new RoofLeg { kind = "bridge", name = "The rope bridge over Pell Row", a = tops[2].Deck(tops[2].LocalX(LaneBridgeX)), b = tops[3].Deck(tops[3].LocalX(LaneBridgeX)), width = RoofBridgeWide });

            Vector3 Along(RoofLeg d, bool fromA, float m) { var dir = (d.b - d.a).normalized; return fromA ? d.a + dir * m : d.b - dir * m; }
            Vector3 OnGround(Vector3 p) { return new Vector3(p.x, ground(p.x, p.z), p.z); }
            // Up: the rusted rain-spout at the row's west end (ch.1:297), and the fire escape at the far terrace's east end (ch.1:261).
            var top0 = Along(decks[0], true, .8f);
            legs.Add(new RoofLeg { kind = "climb", name = SpoutName, a = OnGround(top0 + tops[0].Out * 1.7f), b = top0, roof = 0 });
            var top3 = Along(decks[3], false, 1.2f);
            legs.Add(new RoofLeg { kind = "climb", name = EscapeName, a = OnGround(top3 + tops[3].Out * 1.9f), b = top3, roof = 3 });
            // Down through the washing (ch.1:225): from Tolley's gutter into Pell Row (a line strung there), and from the far terrace's west
            // end, where a line of the zone's own hangs across the lane.
            var drop1 = tops[1].Deck(tops[1].size.x * .25f);
            legs.Add(new RoofLeg { kind = "drop", name = LaundryName, a = drop1, b = OnGround(drop1 + tops[1].Out * 2.8f), roof = 1, ownLine = true });
            var drop3 = Along(decks[3], true, 1.4f);
            legs.Add(new RoofLeg { kind = "drop", name = LaundryName, a = drop3, b = OnGround(drop3 + tops[3].Out * 2.8f), roof = 3 });
            // The water tower on Tolley's roof (ch.7:41), between two of its chimney stacks.
            float tx = TowerX(tops[1].size.x);
            legs.Add(new RoofLeg { kind = "tower", name = TowerName, a = tops[1].Deck(tx), b = tops[1].World(tx, tops[1].eaves + TenementRoofRise(tops[1].size.y) + 4.2f, 0), roof = 1 });
            return legs;
        }
        /// <summary>Where on a terrace <paramref name="w"/> wide the water tower stands: the middle of the widest gap between the chimney
        /// stacks on its ridge (they stand every 5.5 m from 2.2 m in, ZoneBuilder.Tenement).</summary>
        public static float TowerX(float w)
        {
            var xs = new List<float> { -w / 2 };
            for (float x = -w / 2 + 2.2f; x < w / 2 - 1; x += 5.5f) xs.Add(x);
            xs.Add(w / 2);
            float best = 0, wide = -1;
            for (int i = 0; i + 1 < xs.Count; i++) { float g = xs[i + 1] - xs[i] - (i == 0 || i + 2 == xs.Count ? 2.2f : 0); if (g > wide) { wide = g; best = (xs[i] + xs[i + 1]) / 2; } }
            return best;
        }

        void BuildRoofs()
        {
            LowtownRoofs = null;
            var tops = RoofTops(Zone, (x, z) => HeightAt(x, z)); if (tops == null) return;
            var legs = RoofRoute(Zone, (x, z) => HeightAt(x, z));
            var spot = new RoofSpot { roofs = tops, legs = legs };
            var root = new GameObject("The roofs").transform; root.SetParent(statics, false);
            var r = new System.Random(Zone.seed + 4747); float R() { return (float)r.NextDouble(); }
            var b = new Batch();
            var lead = Tint(art.stone, new Color(.2f, .21f, .23f)); var iron = SootIron; var rust = Tint(art.timber, new Color(.34f, .17f, .09f));
            var plank = Tint(art.timber, new Color(.3f, .24f, .17f)); var plank2 = Tint(art.timber, new Color(.24f, .19f, .14f));
            var rope = Tint(art.hay, new Color(.4f, .37f, .3f)); var timber = TarredTimber;
            // A rope between two points, sagging a little, as short straight pieces.
            void Rope(Vector3 p, Vector3 q, float sag, float thick)
            {
                const int n = 6; Vector3 Pt(float u) { return Vector3.Lerp(p, q, u) + Vector3.down * Mathf.Sin(u * Mathf.PI) * sag; }
                for (int i = 0; i < n; i++) { var s = Pt(i / (float)n); var e = Pt((i + 1) / (float)n); var d = e - s; if (d.sqrMagnitude < 1e-6f) continue; b.Box(rope, (s + e) / 2, new Vector3(thick, thick, d.magnitude + thick), Quaternion.LookRotation(d)); }
            }
            // A walked slab: its own object (out of the static batch), solid, in the navmesh.
            GameObject Walk(string name, Vector3 a, Vector3 c, float wide, Material m, bool show)
            {
                var d = c - a; var flat = new Vector3(d.x, 0, d.z);
                var o = Part(PrimitiveType.Cube, root, (a + c) / 2 + Vector3.down * .1f, new Vector3(wide, .2f, flat.magnitude), m, Quaternion.LookRotation(flat));
                o.name = name; o.AddComponent<BoxCollider>(); o.AddComponent<NavWalkable>();
                if (!show) o.GetComponent<Renderer>().enabled = false;
                return o;
            }

            foreach (var leg in legs)
            {
                if (leg.kind == "deck")
                {
                    var t = tops[leg.roof]; var dir = (leg.b - leg.a).normalized; var side = Quaternion.LookRotation(dir); var outv = t.Out;
                    // The lead walk, a little past each gable end so the bridges land on it.
                    Walk(leg.name, leg.a - dir * .3f, leg.b + dir * .3f, RoofDeckWide, lead, true);
                    float len = leg.Length + .6f; var mid = (leg.a + leg.b) / 2;
                    // The iron gutter under its lane edge and the brackets that carry it, one every metre and a half.
                    b.Box(iron, mid + outv * (RoofDeckWide / 2 + .05f) + Vector3.down * .32f, new Vector3(.22f, .18f, len), side);
                    for (float s = -len / 2 + .3f; s < len / 2; s += 1.5f)
                    {
                        var at = mid + dir * s;
                        b.Box(iron, at + Vector3.down * .35f - outv * .2f, new Vector3(.06f, .06f, RoofDeckWide + .3f), Quaternion.LookRotation(outv));
                        b.Box(iron, at + Vector3.down * .7f - outv * (RoofDeckWide / 2 - .15f), new Vector3(.06f, .7f, .06f));
                    }
                    // The rail at the lane side (2026-10-18, Chris: "falling off ramps and bad pathing"; was a low rail you could step
                    // off): iron, solid and cut out of the navmesh, just outside the walk's edge, the whole length but a gap where a rope
                    // bridge leaves it over the lane. The drops through the washing are E (a rail stands there too: the drop is the
                    // interaction, not a step off).
                    float half = len / 2;
                    var gaps = new List<Vector2>();
                    foreach (var o in legs)
                    {
                        if (o.kind != "bridge") continue;
                        foreach (var end in new[] { o.a, o.b })
                        {
                            float s0 = Vector3.Dot(end - mid, dir); float off = Vector3.Dot(end - mid, outv);
                            if (Mathf.Abs(s0) < half - .5f && Mathf.Abs(off) < RoofDeckWide) gaps.Add(new Vector2(s0 - RoofBridgeWide / 2 - .1f, s0 + RoofBridgeWide / 2 + .1f));
                        }
                    }
                    var edge0 = mid + outv * (RoofDeckWide / 2);
                    // (2026-10-18, SweepPlayTests: Lissa stopped where the first alley's bridge lands on Tolley's walk) A rope bridge landing
                    // on this walk's end from the lane side crosses the walk's lane edge on the slant, and the walk's rail running on to the
                    // end stood in the bridge's way: with the navmesh's half-metre agent it left about a quarter of a metre, under a voxel's
                    // width. The walk's rail now stops where the bridge's lane-side rail meets the walk's edge: the two rails close the
                    // corner between them and the bridge keeps its width.
                    foreach (var o in legs)
                    {
                        if (o.kind != "bridge") continue;
                        foreach (var (end, other) in new[] { (o.a, o.b), (o.b, o.a) })
                        {
                            float s0 = Vector3.Dot(end - mid, dir);
                            if (Mathf.Abs(s0) < half - .5f || Mathf.Abs(s0) > half + .5f || Mathf.Abs(Vector3.Dot(end - mid, outv)) > RoofDeckWide / 2 || Vector3.Dot(other - end, outv) <= 0) continue;
                            var bdir = other - end; bdir.y = 0; bdir.Normalize();
                            var bAcross = Vector3.Cross(Vector3.up, bdir); if (Vector3.Dot(bAcross, outv) < 0) bAcross = -bAcross;
                            // Where the line of the bridge's lane-side rail (end + bAcross * half its width + bdir * t) meets the walk's edge.
                            var p0 = end + bAcross * (RoofBridgeWide / 2) - edge0; float along = Vector3.Dot(bdir, outv);
                            if (Mathf.Abs(along) < .05f) continue;
                            float meet = -Vector3.Dot(p0, outv) / along; float cut = Vector3.Dot(p0 + bdir * meet, dir);
                            if (Mathf.Abs(cut) > half) continue;
                            gaps.Add(s0 < 0 ? new Vector2(-half - 1, cut) : new Vector2(cut, half + 1));
                        }
                    }
                    float from = -half;
                    gaps.Sort((x, y) => x.x.CompareTo(y.x));
                    foreach (var g in gaps) { if (g.x > from) LowRail(root, b, leg.name, edge0 + dir * from, edge0 + dir * g.x, outv, rust, rust, .3f, true); from = Mathf.Max(from, g.y); }
                    if (half > from) LowRail(root, b, leg.name, edge0 + dir * from, edge0 + dir * half, outv, rust, rust, .3f, true);
                    // Across each end of the walk with no rope bridge landing on it (the row's west end, the far terrace's ends): the
                    // gable end is a drop to the alley or the lane.
                    foreach (int e in new[] { -1, 1 })
                    {
                        var endAt = mid + dir * (e * half);
                        bool bridged = false;
                        foreach (var o in legs) if (o.kind == "bridge" && (Vector3.Distance(o.a, endAt) < 1.2f || Vector3.Distance(o.b, endAt) < 1.2f)) bridged = true;
                        if (bridged) continue;
                        LowRail(root, b, leg.name + "'s end", endAt - outv * (RoofDeckWide / 2), endAt + outv * (RoofDeckWide / 2 + .12f), dir * e, rust, rust, .3f, true);
                    }
                    // The slope behind is not for walking: a wall you cannot see at the walk's inner edge (not in the navmesh: the walk's own
                    // inner edge already stops the agents).
                    var wall = new GameObject("The slope"); wall.transform.SetParent(root, false);
                    wall.transform.position = mid - outv * (RoofDeckWide / 2 + .05f) + Vector3.up * .8f; wall.transform.rotation = side;
                    wall.AddComponent<BoxCollider>().size = new Vector3(.1f, 1.6f, len);
                    // Slates slipped onto the lead here and there, and a patch of the neon moss (book1 ch.7:17).
                    for (int k = 0; k < 4; k++) b.Box(LowSlate, mid + dir * ((R() - .5f) * (len - 1)) + outv * ((R() - .5f) * 1.2f) + Vector3.up * .02f, new Vector3(.3f, .025f, .45f), Quaternion.Euler(0, R() * 180, 0));
                    if (R() < .7f) b.Box(NeonMoss, mid + dir * ((R() - .5f) * (len - 2)) - outv * .7f + Vector3.up * .02f, new Vector3(.5f + R() * .6f, .04f, .3f + R() * .3f), Quaternion.Euler(0, R() * 180, 0));
                }
                else if (leg.kind == "bridge")
                {
                    var flat = new Vector3(leg.b.x - leg.a.x, 0, leg.b.z - leg.a.z); var dir = flat.normalized; var q = Quaternion.LookRotation(dir);
                    var across = Vector3.Cross(Vector3.up, dir);
                    // The walk itself, onto the lead at each end; the planks over it (some missing, some split).
                    Walk(leg.name, leg.a - dir * .6f, leg.b + dir * .6f, RoofBridgeWide, plank, false);
                    float len = flat.magnitude;
                    for (float s = .15f; s < len; s += .32f)
                    {
                        if (R() < .06f) continue;
                        var at = leg.a + dir * s + Vector3.down * .05f;
                        b.Box(R() < .5f ? plank : plank2, at, new Vector3(RoofBridgeWide - R() * .2f, .06f, .26f), q * Quaternion.Euler(0, (R() - .5f) * 6, 0));
                    }
                    // Two foot ropes under the planks' ends, two hand ropes, and stays between them every metre.
                    foreach (int s in new[] { -1, 1 })
                    {
                        var o = across * s * (RoofBridgeWide / 2 - .05f);
                        Rope(leg.a + o + Vector3.down * .1f, leg.b + o + Vector3.down * .1f, .05f, .05f);
                        Rope(leg.a + o + Vector3.up * 1.05f, leg.b + o + Vector3.up * 1.05f, .25f, .045f);
                        for (float k = 1; k < len; k += 1)
                        {
                            float u = k / len, sag = Mathf.Sin(u * Mathf.PI) * .25f;
                            var foot = leg.a + o + dir * k + Vector3.down * .1f; var hand = foot + Vector3.up * (1.15f - sag);
                            b.Box(rope, (foot + hand) / 2, new Vector3(.03f, (hand - foot).magnitude, .03f));
                        }
                        // A timber post at each end, the ropes made fast round it.
                        foreach (var end in new[] { leg.a, leg.b }) b.Box(timber, end + o + Vector3.up * .55f, new Vector3(.14f, 1.3f, .14f));
                        // (2026-10-18) The hand ropes made solid: a rail you cannot see along each side just outside the planks, cut out of
                        // the navmesh, so nobody steps off the bridge between the stays.
                        // It starts where the bridge leaves the lead walk: a bridge across the lane leaves it from the walk's middle line, so
                        // its rail starts the walk's half width out, clear of the walk.
                        float Trim(Vector3 end)
                        {
                            foreach (var d in legs)
                                if (d.kind == "deck" && d.Off(end, out float dy) < 1.2f && Mathf.Abs(dy) < 1)
                                    return (1 - Mathf.Abs(Vector3.Dot(dir, (d.b - d.a).normalized))) * (RoofDeckWide / 2 + .05f);
                            return 0;
                        }
                        float ta = Trim(leg.a), tb = Trim(leg.b);
                        if (len - ta - tb > .5f)
                        {
                            var edge = across * s * (RoofBridgeWide / 2);
                            var ra = Vector3.Lerp(leg.a, leg.b, ta / len); var rb = Vector3.Lerp(leg.a, leg.b, 1 - tb / len);
                            LowRail(root, null, leg.name, ra + edge, rb + edge, across * s, null, null, 0, true);
                        }
                    }
                }
                else if (leg.kind == "climb" && leg.name == SpoutName) RainSpout(b, tops[leg.roof], leg, iron, rust);
                else if (leg.kind == "climb") FireEscape(b, tops[leg.roof], leg, iron, rust);
                else if (leg.kind == "drop" && leg.ownLine)
                {
                    // A washing line across the lane under the drop, from this terrace's front to the one across (ch.1:225).
                    var t = tops[leg.roof]; var line = new GameObject("Washing over Pell Row").transform; line.SetParent(root, false);
                    var mid = leg.b + t.Out * 2.6f; line.position = new Vector3(mid.x, leg.b.y, mid.z);
                    line.rotation = Quaternion.LookRotation(Vector3.Cross(t.Out, Vector3.up));
                    WashingLine(line, 10.5f, 6.6f, r);
                }
                else if (leg.kind == "tower") WaterTower(b, root, tops[leg.roof], leg, timber, iron, rope);
            }

            // The things E works: climb, drop, the tower. Each is offered only at its own height (EncounterSession.Roofs).
            foreach (var leg in legs)
            {
                ZoneInteractable i = null;
                if (leg.kind == "climb") i = new ZoneInteractable { name = leg.name, prompt = leg.name == SpoutName ? "Climb the rusted rain-spout" : "Climb the fire escape", kind = "roofclimb", position = leg.a };
                else if (leg.kind == "drop") i = new ZoneInteractable { name = leg.name, prompt = "Drop through the washing", kind = "roofdrop", position = leg.a };
                else if (leg.kind == "tower") i = new ZoneInteractable { name = leg.name, prompt = "Climb the water tower", kind = "rooftower", position = leg.a };
                if (i == null) continue;
                Interactables.Add(i); spot.things[i] = leg;
            }
            b.Flush(this, root, "The roofs");
            LowtownRoofs = spot;
            Debug.Log("Lowtowns: the roofs over Pell Row, " + legs.Count + " legs.");
        }

        /// <summary>The rusted rain-spout (book1 ch.1:297): an iron down-pipe at the terrace's corner from the gutter to the lane, its
        /// brackets, the hopper-head at the top and the shoe at the foot; flaking rust.</summary>
        void RainSpout(Batch b, RoofTop t, RoofLeg leg, Material iron, Material rust)
        {
            var dir = (leg.b - leg.a); dir.y = 0; dir.Normalize();
            // The pipe stands against the front wall, at the walk's end.
            var top = leg.b + t.Out * (-(RoofDeckWide / 2) + .25f) + Vector3.down * (RoofDeckLift + .2f);
            var foot = new Vector3(top.x, leg.a.y + .2f, top.z);
            b.Box(rust, (top + foot) / 2, new Vector3(.16f, top.y - foot.y, .16f));
            b.Box(iron, top + Vector3.up * .05f, new Vector3(.4f, .4f, .4f));
            b.Box(iron, foot + Vector3.down * .1f + t.Out * .12f, new Vector3(.2f, .14f, .4f));
            for (float y = foot.y + 1.2f; y < top.y; y += 1.6f) b.Box(iron, new Vector3(top.x, y, top.z), new Vector3(.26f, .07f, .26f));
            // Streaks of rust down the brick beside it.
            b.Box(rust, (top + foot) / 2 - t.Out * .1f + Vector3.Cross(Vector3.up, t.Out) * .2f, new Vector3(.12f, (top.y - foot.y) * .6f, .03f), t.rot);
        }

        /// <summary>A fire escape (book1 ch.1:261): iron landings at each floor up the terrace's front, flights of iron steps between them
        /// turning back and forth, a rail, and the last flight up onto the lead walk. The bottom ladder hangs short of the lane.</summary>
        void FireEscape(Batch b, RoofTop t, RoofLeg leg, Material iron, Material rust)
        {
            var dirW = leg.b - leg.a; dirW.y = 0;
            // Along the front, toward the terrace's middle (away from its end).
            var along = t.rot * Vector3.right; if (Vector3.Dot(along, (t.root - leg.b)) < 0) along = -along; along.y = 0; along.Normalize();
            var q = Quaternion.LookRotation(along);
            int storeys = TenementStoreys(t.prop.variant);
            var wallFoot = new Vector3(leg.b.x, t.root.y, leg.b.z) - t.Out * (RoofDeckWide / 2 + .4f);
            var face = wallFoot + t.Out * .7f;
            for (int s = 1; s < storeys; s++)
            {
                float y = .5f + s * 3 - .1f;
                var landing = face + Vector3.up * y + along * 1.2f;
                b.Box(iron, landing, new Vector3(1.2f, .07f, 3.0f), q);
                b.Box(rust, landing + t.Out * .58f + Vector3.up * .5f, new Vector3(.04f, .04f, 3.0f), q);
                for (float k = -1.4f; k <= 1.41f; k += .7f) b.Box(rust, landing + t.Out * .58f + along * k + Vector3.up * .25f, new Vector3(.04f, .5f, .04f));
            }
            // The flights: from each landing up to the next, turning back and forth along the front.
            for (int s = 1; s < storeys; s++)
            {
                float y0 = .5f + s * 3 - .1f, y1 = s + 1 < storeys ? y0 + 3 : t.eaves + RoofDeckLift;
                bool back = s % 2 == 0;
                var lo = face + Vector3.up * y0 + along * (back ? 2.4f : 0f); var hi = face + Vector3.up * y1 + along * (back ? 0f : 2.4f);
                var d = hi - lo;
                b.Box(iron, (lo + hi) / 2, new Vector3(.8f, .06f, d.magnitude), Quaternion.LookRotation(d));
                for (float u = .1f; u < 1; u += .12f) b.Box(rust, Vector3.Lerp(lo, hi, u) + Vector3.up * .04f, new Vector3(.8f, .04f, .2f), q);
            }
            // The drop ladder from the first landing, hanging two metres over the lane.
            var ladderTop = face + Vector3.up * (.5f + 3 - .1f) + along * 2.2f; var ladderFoot = ladderTop + Vector3.down * 1.6f;
            foreach (int s in new[] { -1, 1 }) b.Box(iron, (ladderTop + ladderFoot) / 2 + along * .25f * s, new Vector3(.04f, 1.6f, .04f));
            for (float y = .2f; y < 1.6f; y += .3f) b.Box(iron, ladderFoot + Vector3.up * y, new Vector3(.04f, .04f, .5f), q);
        }

        /// <summary>"A precarious water tower" (book1 ch.7:41): a barrel of staves and iron hoops on four timber legs astride the ridge,
        /// leaning a little, guyed with rope to a chimney stack; a ladder down to the gutter walk; moss and a dark stain where it leaks.
        /// Nothing in it moves. Seen, not walked.</summary>
        void WaterTower(Batch b, Transform root, RoofTop t, RoofLeg leg, Material timber, Material iron, Material rope)
        {
            float rise = TenementRoofRise(t.size.y), half = t.size.y / 2 + .4f;
            float tx = TowerX(t.size.x);
            var tower = new GameObject(TowerName).transform; tower.SetParent(root, false);
            tower.position = t.World(tx, t.eaves + rise, 0); tower.rotation = t.rot * Quaternion.Euler(3.5f, 0, -4.5f);
            var staves = Tint(art.timber, new Color(.27f, .2f, .14f)); var wet = Tint(art.stone, new Color(.1f, .1f, .1f));
            // The legs: from the slope on either side of the ridge up to the tank's floor, braced crosswise.
            float lx = 1.1f, lz = 1.4f, floorY = 1.6f;
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                {
                    float footY = -rise * (lz / half);
                    var foot = tower.TransformPoint(new Vector3(sx * lx, footY, sz * lz)); var head = tower.TransformPoint(new Vector3(sx * lx * .85f, floorY, sz * lz * .85f));
                    var d = head - foot; b.Box(timber, (foot + head) / 2, new Vector3(.18f, d.magnitude, .18f), Quaternion.FromToRotation(Vector3.up, d));
                }
            foreach (int sz in new[] { -1, 1 })
            {
                var p = tower.TransformPoint(new Vector3(-lx, .1f, sz * lz * .92f)); var q = tower.TransformPoint(new Vector3(lx, floorY - .2f, sz * lz * .92f));
                b.Box(timber, (p + q) / 2, new Vector3(.1f, .1f, (q - p).magnitude), Quaternion.LookRotation(q - p));
            }
            b.Box(timber, tower.TransformPoint(new Vector3(0, floorY, 0)), new Vector3(2.8f, .16f, 3.1f), tower.rotation);
            // The barrel: the wood barrel (play note 247, 2026-10-19) fitted to the old tank, 2.6 m tall and 2.4 across (a squat butt);
            // else staves and three iron hoops. A shallow cone of a lid.
            var tank = ModelProp(tower, WoodBarrelPath, new Vector3(0, floorY + .1f, 0), 0, 2.6f, false, null, new Vector3(2.4f, 2.6f, 2.4f));
            if (tank == null)
            {
                tank = Part(PrimitiveType.Cylinder, tower, new Vector3(0, floorY + 1.4f, 0), new Vector3(2.4f, 1.3f, 2.4f), staves);
                foreach (float y in new[] { .35f, 1.4f, 2.45f }) Part(PrimitiveType.Cylinder, tower, new Vector3(0, floorY + y, 0), new Vector3(2.46f, .05f, 2.46f), iron);
            }
            tank.name = "The tank";
            Part(PrimitiveType.Cylinder, tower, new Vector3(0, floorY + 2.78f, 0), new Vector3(2.6f, .06f, 2.6f), timber);
            Part(PrimitiveType.Cylinder, tower, new Vector3(0, floorY + 2.95f, 0), new Vector3(1.6f, .12f, 1.6f), timber);
            Part(PrimitiveType.Cylinder, tower, new Vector3(0, floorY + 3.1f, 0), new Vector3(.7f, .1f, .7f), timber);
            // A stain where it weeps down a leg onto the slates, and moss at its foot.
            Part(PrimitiveType.Cube, tower, new Vector3(.9f, floorY + .6f, -.85f), new Vector3(.3f, 1.2f, .05f), wet, Quaternion.Euler(0, 45, 0));
            b.Box(NeonMoss, tower.TransformPoint(new Vector3(-lx, -rise * (lz / half) + .05f, -lz)), new Vector3(.6f, .05f, .4f), tower.rotation);
            // Guy ropes to the nearest chimney stack and to an iron pin at the gutter: what keeps it standing, just.
            var guyFrom = tower.TransformPoint(new Vector3(0, floorY + 2.2f, 0));
            void Guy(Vector3 to) { var d = to - guyFrom; b.Box(rope, (guyFrom + to) / 2, new Vector3(.035f, .035f, d.magnitude), Quaternion.LookRotation(d)); }
            Guy(t.World(tx - 2.6f, t.eaves + rise + 1.5f, 0));
            Guy(t.World(tx + 1.5f, t.eaves + .3f, t.size.y / 2 + .2f));
            // The ladder down the front slope to the gutter walk.
            var ladTop = tower.TransformPoint(new Vector3(0, floorY, -1.5f)); var ladFoot = leg.a + Vector3.up * .05f - t.Out * .5f;
            var ld = ladTop - ladFoot; var across = Vector3.Cross(Vector3.up, ld).normalized;
            foreach (int s in new[] { -1, 1 }) b.Box(timber, (ladTop + ladFoot) / 2 + across * .24f * s, new Vector3(.07f, .07f, ld.magnitude), Quaternion.LookRotation(ld));
            for (float u = .08f; u < 1; u += .09f) b.Box(timber, Vector3.Lerp(ladFoot, ladTop, u), new Vector3(.05f, .05f, .5f), Quaternion.LookRotation(across));
        }
    }
}
