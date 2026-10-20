using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A swing-load as built (the Adit rework, stage S7; Chris, 2026-10-15, Q4 "Yes, add them"; GAME-ONLY): an ore bucket on a rope from a
    /// block lashed under a timber tie-beam, its arc bottoming out across a high walk of the Geode Foundry, swung by hand by a rope team on
    /// the hall floor below who pull it out on a tag line. Nothing turns: a rope, a lashed block, people pulling (no wheel, winch or crane;
    /// memory "NO Machines"). The builder makes the timber, the block and the bucket; EncounterSession (AditSwingLoad) swings it, puts the
    /// team there and knocks back whoever it meets.
    /// </summary>
    public sealed class SwingLoadSpot
    {
        public string name;
        /// <summary>Where the rope hangs from (under the block), the level way it swings (toward the team, out over the hall) and the way
        /// along the walk it crosses, the walk's top under the arc's bottom, and the rope's length (pivot to the bucket's middle).</summary>
        public Vector3 pivot, swing, walk, bottom; public float rope;
        /// <summary>Where the rope team stands on the floor below, and the way it faces (toward the bucket).</summary>
        public Vector3 team, teamFacing;
        /// <summary>The bucket, its rope and the team's tag line (all moved each frame by the session; no colliders).</summary>
        public Transform root, bucket, ropeLine, tagLine;
    }

    /// <summary>
    /// The Sealed Adit's polish (Docs/ADIT_REWORK.md, stage S7, section 21; GAME-ONLY): the living lichen along the run's way (ZoneCrumb), the
    /// Foundry's swing-loads (<see cref="SwingLoadSpot"/>) and the Grey Breach's hidden seeps of static and its lamps greyed behind each grey
    /// line (EncounterSession.GreySpread.cs). Built after the decks (BuildCaveWorks), each from the zone's data and nothing without it, from
    /// streams of their own (the zone's draws are untouched).
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>Anchor names: "swing load 1".. (the walk's top under each bucket's arc), "grey line 1".. (the Breach's lines).</summary>
        public const string SwingLoadAnchor = "swing load ", GreyLineAnchor = "grey line ";
        /// <summary>The zone's swing-loads as built (empty without their anchors).</summary>
        public readonly List<SwingLoadSpot> SwingLoads = new List<SwingLoadSpot>();
        readonly List<GameObject> greySeeps = new List<GameObject>();
        readonly Dictionary<Light, (float day, float night, Color colour)> lightsBefore = new Dictionary<Light, (float, float, Color)>();
        static readonly Color LichenGlow = new Color(.42f, .9f, .62f), StaticGrey = new Color(.66f, .68f, .7f);

        void BuildAditPolish(Transform root, Transform look)
        {
            SwingLoads.Clear(); greySeeps.Clear(); lightsBefore.Clear();
            BuildCrumbs(look);
            BuildSwingLoads(root);
            BuildGreySeeps(root);
        }

        // ---------- the lichen (ZoneCrumb) ----------
        /// <summary>
        /// The living lichen: a faint trail of glowing patches on the floor along each crumb (ZoneCrumb: a cave, from, to, aside), every metre
        /// and a half or so, each drawn out along the way on (so the trail reads as pointing on). None out over another cave's air, in a sunk
        /// pool or under a ramp; and it gives out for a stride or two round a key lying by the way (the rusted key on its nail in the dark
        /// drift: "where the lichen gives out"). One mesh a trail, no light of its own, no collider.
        /// </summary>
        void BuildCrumbs(Transform look)
        {
            if (Zone.crumbs == null || Zone.crumbs.Length == 0) return;
            var keys = new List<Vector3>();
            if (Zone.secrets != null)
                foreach (var s in Zone.secrets)
                {
                    var h = s == null || s.kind != "key" ? null : CaveNamed(s.cave); if (h == null) continue;
                    keys.Add(h.At(s.along, s.aside));
                }
            var lichen = Glowing(LichenGlow, .55f);
            int no = 0;
            foreach (var crumb in Zone.crumbs)
            {
                var h = crumb == null ? null : CaveNamed(crumb.cave); no++; if (h == null) continue;
                var cr = new System.Random(Zone.seed * 17 + 101 * no); float C() { return (float)cr.NextDouble(); }
                var fm = new FaceMesh(); float from = Mathf.Max(0, Mathf.Min(crumb.from, crumb.to)), to = Mathf.Min(h.Length, Mathf.Max(crumb.from, crumb.to));
                for (float s = from; s < to; s += 1.3f + C() * .7f)
                {
                    var w = h.At(s, crumb.aside + (C() - .5f) * .5f); var p = new Vector2(w.x, w.z);
                    if (!h.FloorAt(p, out float fy) || BasinDip(h, p) > .05f) continue;
                    if (DeckTop(p, fy, out float top) && top > fy + .2f && top - fy < 4) continue;   // under a ramp or a plinth
                    bool byKey = false; foreach (var k in keys) if (Vector2.Distance(p, new Vector2(k.x, k.z)) < 2.6f) byKey = true;
                    if (byKey) continue;
                    var on = h.At(s + 1.5f) - h.At(s); on.y = 0; if (on.sqrMagnitude < 1e-4f) continue; on.Normalize();
                    var across = new Vector3(on.z, 0, -on.x);
                    Patch(fm, new Vector3(w.x, fy + .025f, w.z), on, across, .18f + C() * .12f, .08f + C() * .05f);
                    if (C() < .6f) Patch(fm, new Vector3(w.x, fy + .02f, w.z) + on * (.3f + C() * .25f) + across * (C() - .5f) * .3f, on, across, .07f + C() * .05f, .04f + C() * .03f);
                }
                if (fm.Count == 0) continue;
                var trail = MeshPart(fm.Make(h.Name + " lichen"), look, Vector3.zero, lichen);
                trail.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        /// <summary>A flat oval patch on the floor, long along <paramref name="on"/> (a fan of eight).</summary>
        static void Patch(FaceMesh fm, Vector3 c, Vector3 on, Vector3 across, float len, float wide)
        {
            const int Seg = 8;
            for (int k = 0; k < Seg; k++)
            {
                float a0 = k * Mathf.PI * 2 / Seg, a1 = (k + 1) * Mathf.PI * 2 / Seg;
                var p0 = c + on * Mathf.Cos(a0) * len + across * Mathf.Sin(a0) * wide; var p1 = c + on * Mathf.Cos(a1) * len + across * Mathf.Sin(a1) * wide;
                fm.Tri(c, p0, p1, Vector3.up, new Vector2(.5f, .5f), new Vector2(.5f + Mathf.Cos(a0) * .5f, .5f + Mathf.Sin(a0) * .5f), new Vector2(.5f + Mathf.Cos(a1) * .5f, .5f + Mathf.Sin(a1) * .5f));
            }
        }

        // ---------- the swing-loads ----------
        /// <summary>How long a swing-load's rope is (block to the bucket's middle) and how high the bucket's middle passes over its walk.</summary>
        public const float SwingRope = 6.5f, SwingHang = 1.1f;
        /// <summary>
        /// Each "swing load n" anchor (on a walk's top): the walk under it (the deck it stands on), the way across it (the bucket swings in that
        /// plane, out toward the hall's middle where the team stands), a timber tie-beam across the cave at the pivot's height from wall to wall
        /// with a block lashed under it, the rope, the bucket of ore (iron-hooped staves, ore heaped in it) and the team's tag line. The team
        /// itself is the session's (figures are its business).
        /// </summary>
        void BuildSwingLoads(Transform root)
        {
            if (Zone.anchors == null) return;
            var timber = Tint(art.timber, new Color(.33f, .24f, .15f)); var stave = Tint(art.timber, new Color(.42f, .3f, .19f)); var iron = Tint(art.metal, new Color(.22f, .21f, .2f));
            var rope = Tint(art.hay, new Color(.58f, .5f, .34f)); var ore = RockTint(new Color(.36f, .33f, .38f)); var geode = Glowing(new Color(.7f, .42f, 1), .9f);
            foreach (var a in Zone.anchors)
            {
                if (a == null || a.name == null || !a.name.StartsWith(SwingLoadAnchor) || !Anchors.TryGetValue(a.name, out var b)) continue;
                var h = CaveNamed(a.cave); if (h == null) continue;
                // The walk under it: the deck sample nearest its anchor, at its height.
                ZoneDeckSpot deck = null; int at = -1; float bestD = float.MaxValue;
                foreach (var d in DeckSpots)
                {
                    if (d.round || d.mid == null || d.mid.Length < 2) continue;
                    for (int j = 0; j < d.mid.Length; j++)
                    {
                        float dd = Vector2.Distance(new Vector2(d.mid[j].x, d.mid[j].z), new Vector2(b.x, b.z));
                        if (dd < d.half + .6f && Mathf.Abs(d.mid[j].y - b.y) < .7f && dd < bestD) { bestD = dd; deck = d; at = j; }
                    }
                }
                if (deck == null) { Debug.LogWarning(Zone.id + ": the swing load '" + a.name + "' stands on no walk."); continue; }
                var walk = deck.mid[Mathf.Min(deck.mid.Length - 1, at + 1)] - deck.mid[Mathf.Max(0, at - 1)]; walk.y = 0; walk.Normalize();
                var swing = new Vector3(walk.z, 0, -walk.x);
                // Out toward the cave's middle line, where the floor is open below and no deck stands (the team's side).
                float Score(Vector3 dir)
                {
                    var q = b + dir * 9; h.Local(new Vector2(q.x, q.z), out float qa, out float side);
                    // (a tie, as across the cross-walk, goes deeper into the cave: the Foundry's chute-door guards stand at its near end)
                    return Mathf.Abs(side) + (DeckTop(new Vector2(q.x, q.z), b.y - 3, out _) ? 100 : 0) - qa * .001f;
                }
                if (Score(-swing) < Score(swing)) swing = -swing;
                var spot = new SwingLoadSpot { name = a.name, swing = swing, walk = walk, bottom = b, rope = SwingRope };
                spot.pivot = b + Vector3.up * (SwingHang + SwingRope);
                var team = b + swing * 9; var tp = new Vector2(team.x, team.z);
                team.y = FloorBelow(tp, b.y - 1.5f, b.y - 12); spot.team = team;
                var face = b - team; face.y = 0; spot.teamFacing = face.sqrMagnitude > .01f ? face.normalized : -swing;

                var go = new GameObject("Swing load: " + a.name.Substring(SwingLoadAnchor.Length)).transform; go.SetParent(root, false); spot.root = go;
                // The tie-beam across the cave at the pivot's height, wall to wall, and the block lashed under it.
                h.Local(new Vector2(spot.pivot.x, spot.pivot.z), out float along, out _);
                int i = RingIndex(h, along); float up = spot.pivot.y + .35f - h.Centre[i].y;
                var mid = h.At(along); var right = h.At(along, 1) - mid; right.y = 0; right.Normalize();
                float wr = WallOut(h, i, 1, up), wl = WallOut(h, i, -1, up);
                var e0 = new Vector3(mid.x, spot.pivot.y + .35f, mid.z) - right * (wl + .4f); var e1 = new Vector3(mid.x, spot.pivot.y + .35f, mid.z) + right * (wr + .4f);
                Part(PrimitiveType.Cube, go, (e0 + e1) / 2, new Vector3(.42f, .46f, Vector3.Distance(e0, e1)), timber, Quaternion.LookRotation(e1 - e0));
                // (where the bucket's rope meets the beam: the pivot sits under the beam's line, the beam passing through the cave's middle at that along)
                var under = spot.pivot + Vector3.up * .12f;
                Part(PrimitiveType.Cube, go, under, new Vector3(.3f, .34f, .3f), timber, Quaternion.LookRotation(right));   // the block
                foreach (float y in new[] { .2f, .32f })   // its lashing round the beam
                    Part(PrimitiveType.Cylinder, go, under + Vector3.up * y, new Vector3(.5f, .025f, .5f), rope);
                // The bucket: staves, two hoops, a bail, ore heaped in it (a geode or two glinting).
                var bucket = new GameObject("Ore bucket").transform; bucket.SetParent(go, false); bucket.position = b + Vector3.up * SwingHang; spot.bucket = bucket;
                Part(PrimitiveType.Cylinder, bucket, Vector3.zero, new Vector3(.9f, .36f, .9f), stave);
                foreach (float y in new[] { -.2f, .22f }) Part(PrimitiveType.Cylinder, bucket, Vector3.up * y, new Vector3(.94f, .03f, .94f), iron);
                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, bucket, new Vector3(s * .44f, .55f, 0), new Vector3(.04f, .5f, .05f), iron);
                Part(PrimitiveType.Cube, bucket, new Vector3(0, .8f, 0), new Vector3(.92f, .04f, .05f), iron);
                var or = new System.Random(Zone.seed * 7 + a.name.Length * 31 + SwingLoads.Count * 977);
                for (int k = 0; k < 5; k++)
                {
                    float ang = k * 1.26f, rad = k == 0 ? 0 : .22f;
                    Lump(BoulderAt(k), bucket, new Vector3(Mathf.Cos(ang) * rad, .38f + (k == 0 ? .08f : 0), Mathf.Sin(ang) * rad), Vector3.one * (.2f + (float)or.NextDouble() * .1f), k == 2 ? geode : ore, k * 70);
                }
                spot.ropeLine = Part(PrimitiveType.Cylinder, go, (spot.pivot + bucket.position) / 2, new Vector3(.06f, SwingRope / 2, .06f), rope).transform; spot.ropeLine.name = "Rope";
                spot.tagLine = Part(PrimitiveType.Cylinder, go, (team + bucket.position) / 2, new Vector3(.035f, 1, .035f), rope).transform; spot.tagLine.name = "Tag line";
                // A cleat on the floor where the team makes the line fast between swings: a short post and a coil.
                Part(PrimitiveType.Cube, go, team - spot.teamFacing * 1.6f + Vector3.up * .35f, new Vector3(.18f, .7f, .18f), timber);
                Part(PrimitiveType.Cylinder, go, team - spot.teamFacing * 1.1f + Vector3.up * .05f, new Vector3(.6f, .05f, .6f), rope);
                SwingLoads.Add(spot);
            }
        }

        // ---------- the Grey Breach: its seeps and its lamps ----------
        /// <summary>
        /// For each "grey line n" anchor: a seep of static over the 30 m behind the line (patches of unmade rock on its walls, a few faintly
        /// lit, grey stains on its floor), built hidden. <see cref="ShowGreySeep"/> shows it when the line is crossed.
        /// </summary>
        void BuildGreySeeps(Transform root)
        {
            if (Zone.anchors == null) return;
            var lines = new List<(int n, ZoneAnchor a)>();
            foreach (var a in Zone.anchors)
                if (a != null && a.name != null && a.name.StartsWith(GreyLineAnchor) && int.TryParse(a.name.Substring(GreyLineAnchor.Length), out int n)) lines.Add((n, a));
            lines.Sort((x, y) => x.n.CompareTo(y.n));
            var unmade = Tint(art.stone, new Color(.34f, .35f, .37f)); var lit = Glowing(StaticGrey, .35f); var stain = Tint(art.stone, new Color(.42f, .43f, .45f));
            foreach (var (n, a) in lines)
            {
                var h = CaveNamed(a.cave); if (h == null) continue;
                while (greySeeps.Count < n - 1) greySeeps.Add(null);
                var go = new GameObject("Grey seep " + n); go.transform.SetParent(root, false);
                var gr = new System.Random(Zone.seed * 23 + n * 409); float G() { return (float)gr.NextDouble(); }
                for (float s = Mathf.Max(0, a.along - 30); s < a.along; s += 1.4f)
                {
                    var c = h.At(s); if (!h.FloorAt(new Vector2(c.x, c.z), out float fy)) continue;
                    int i = RingIndex(h, s);
                    for (int q = 0; q < 2; q++)
                    {
                        int side = G() < .5f ? -1 : 1; float up = .3f + G() * h.Height[i] * .6f, outw = WallOut(h, i, side, up) - .1f;
                        var at = h.At(s, side * outw); at.y = fy + up;
                        Lump(CragRock((int)(G() * 6)), go.transform, at, new Vector3(.5f + G() * .9f, .4f + G() * .7f, .3f), G() < .3f ? lit : unmade, G() * 360);
                    }
                    if (G() < .45f)
                    {
                        int side = G() < .5f ? -1 : 1; var at = h.At(s, side * h.Half[i] * (.55f + G() * .3f)); at.y = fy + .02f;
                        Part(PrimitiveType.Cylinder, go.transform, at, new Vector3(1.1f + G() * 1.2f, .01f, .8f + G() * .9f), stain, Quaternion.Euler(0, G() * 360, 0));
                    }
                }
                go.SetActive(false);
                if (greySeeps.Count >= n) greySeeps[n - 1] = go; else greySeeps.Add(go);
            }
        }
        /// <summary>Shows the seep behind grey line <paramref name="line"/> (1, 2, 3...). Returns how many pieces it has (0: none, or shown before).</summary>
        public int ShowGreySeep(int line)
        {
            if (line < 1 || line > greySeeps.Count || greySeeps[line - 1] == null || greySeeps[line - 1].activeSelf) return 0;
            greySeeps[line - 1].SetActive(true); return greySeeps[line - 1].transform.childCount;
        }
        /// <summary>Whether the seep behind grey line <paramref name="line"/> shows.</summary>
        public bool GreySeepShown(int line) { return line >= 1 && line <= greySeeps.Count && greySeeps[line - 1] != null && greySeeps[line - 1].activeSelf; }
        /// <summary>
        /// The lamps of the cave named that stand less than <paramref name="behind"/> metres in, greyed: their light at <paramref name="keep"/> of
        /// what it was built at (by day and by night: the clock lights them by these) and drained of its colour. Returns how many.
        /// </summary>
        public int GreyCaveLights(string caveName, float behind, float keep)
        {
            var h = CaveNamed(caveName); if (h == null) return 0; int n = 0;
            foreach (var nl in NightLights)
            {
                if (nl == null || nl.light == null) continue; var p = nl.light.transform.position;
                if (!h.Inside(p, .5f)) continue;
                h.Local(new Vector2(p.x, p.z), out float along, out _); if (along > behind) continue;
                if (!lightsBefore.TryGetValue(nl.light, out var was)) lightsBefore[nl.light] = was = (nl.dayIntensity, nl.nightIntensity, nl.light.color);
                nl.dayIntensity = was.day * keep; nl.nightIntensity = was.night * keep;
                float g = was.colour.grayscale; nl.light.color = Color.Lerp(was.colour, new Color(g, g, g * 1.04f, was.colour.a), .85f);
                n++;
            }
            return n;
        }
        /// <summary>The lamps of the cave named that were greyed brought <paramref name="share"/> of the way back (0..1). Returns how many.</summary>
        public int BringBackCaveLights(string caveName, float share)
        {
            var h = CaveNamed(caveName); if (h == null) return 0; int n = 0;
            foreach (var nl in NightLights)
            {
                if (nl == null || nl.light == null || !lightsBefore.TryGetValue(nl.light, out var was) || !h.Inside(nl.light.transform.position, .5f)) continue;
                nl.dayIntensity = Mathf.Lerp(nl.dayIntensity, was.day, share); nl.nightIntensity = Mathf.Lerp(nl.nightIntensity, was.night, share);
                nl.light.color = Color.Lerp(nl.light.color, was.colour, share); n++;
            }
            return n;
        }
        /// <summary>How the lamp's light stands against what it was built at (1: as built, or never greyed). Tests read it.</summary>
        public float LightShare(Light l)
        {
            if (l == null || !lightsBefore.TryGetValue(l, out var was)) return 1;
            foreach (var nl in NightLights) if (nl != null && nl.light == l) return was.night > 1e-4f ? nl.nightIntensity / was.night : was.day > 1e-4f ? nl.dayIntensity / was.day : 1;
            return 1;
        }
    }
}
