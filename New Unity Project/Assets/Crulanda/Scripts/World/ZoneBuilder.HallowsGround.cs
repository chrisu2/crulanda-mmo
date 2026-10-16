using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A dungeon on open ground (Hallow's Creek, Docs/DUNGEON2_DESIGN.md section 5, N1-N4; all GAME-ONLY): what the Adit keys by cave and
    /// "along" given a world x and z and a height over the yard instead (<see cref="Datum"/>: the land at the zone's start), for a zone whose
    /// ways are streets and a chasm, not tunnels.
    /// - decks (ZoneDeck with no cave: <see cref="ZoneDeck.at"/> to <see cref="ZoneDeck.at2"/>): the broken bridge's slabs and the ramps between
    ///   them, the gatehouse's wall-walk, the Granary's lofts, the Archive's floor and gallery, the tower's flights and landings. Each is one
    ///   block of stone or timber, walked on and in the navmesh (NavWalkable), and a ZoneDeckSpot like a cave's (StandAt, the camps, the run);
    /// - drops (ZoneDrop with no fromCave): a one-way link down (off the third slab to the lower orbit);
    /// - gates (ZoneGateDef with no cave): a ZoneGate standing on the land or a deck, across its way;
    /// - anchors (ZoneAnchor with no cave): named places, on the land or the deck at their height;
    /// - pits (ZonePit): the chasm, a box of land sunk forty metres with sheer sides, and the fog floor in it (EncounterSession.Hallows sends
    ///   anyone who falls into the fog back to the last deck they stood on: nobody falls under the world);
    /// - hazards (ZoneHazard): the hungry shadows, dark strips on the ground that slow and wear (EncounterSession.Hallows) and that agents
    ///   walk round (a navmesh area with a high cost: EncounterNavigation).
    /// No machines (DUNGEON_DESIGN 0.05): stone and timber. Draws nothing from the zone's stream; a zone without these builds as it did.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The height open ground's heights are measured from: the land at the zone's start (the yard).</summary>
        public float Datum { get; private set; }
        /// <summary>A strip of hazard ground as worked out: its data, its ends on the ground plane, its half width and the floor it lies on.</summary>
        public sealed class HazardSpot
        {
            public ZoneHazard def; public Vector2 a, b; public float half, floor;
            /// <summary>Where its ends lie when it has not shifted, and its strips on the ground (moved with it when it shifts).</summary>
            public Vector2 a0, b0; public readonly List<Transform> looks = new List<Transform>();
        }
        /// <summary>
        /// (DUNGEON2_DESIGN 4.1: "At the Waking Hour they shift.") Each hungry shadow slides sideways by <paramref name="metres"/> (one way or the
        /// other by turns, as the light of a waking town comes from somewhere else); 0 puts them back. The navmesh's costly patches stay where
        /// they were baked: a shift of a metre or two still overlaps them, and the strips go back when the town sleeps again.
        /// </summary>
        public void ShiftHazards(float metres)
        {
            for (int i = 0; i < Hazards.Count; i++)
            {
                var h = Hazards[i]; var dir = (h.b0 - h.a0).normalized; var side = new Vector2(dir.y, -dir.x) * (i % 2 == 0 ? metres : -metres);
                var was = h.a; h.a = h.a0 + side; h.b = h.b0 + side; var by = h.a - was;
                foreach (var t in h.looks) if (t != null) t.position += new Vector3(by.x, 0, by.y);
            }
        }
        /// <summary>The zone's hazard strips (the hungry shadows).</summary>
        public readonly List<HazardSpot> Hazards = new List<HazardSpot>();
        /// <summary>The navmesh's costly patches over the hazard strips (EncounterNavigation): centre, rotation, size.</summary>
        public readonly List<(Vector3 centre, Quaternion rot, Vector3 size)> HazardAreas = new List<(Vector3 centre, Quaternion rot, Vector3 size)>();
        /// <summary>Whether a deck stands on open ground (no cave of its own).</summary>
        public static bool OpenDeck(ZoneDeck d) { return d != null && string.IsNullOrEmpty(d.cave); }

        // ---------- the chasm (ZonePit) ----------
        static Vector2 PitLocal(ZonePit p, float x, float z)
        {
            var d = new Vector2(x - p.center.x, z - p.center.y); float a = -p.rotation * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(d.x * c - d.y * s, d.x * s + d.y * c);
        }
        /// <summary>How far in from a pit's edge a point is (negative outside it).</summary>
        static float PitInside(ZonePit p, float x, float z)
        {
            var l = PitLocal(p, x, z); return Mathf.Min(p.size.x / 2 - Mathf.Abs(l.x), p.size.y / 2 - Mathf.Abs(l.y));
        }
        /// <summary>The land sunk where a pit is: down by its depth over its wall's width in from the edge (a sheer fall), flat at its bottom.</summary>
        float Pit(float x, float z, float h)
        {
            foreach (var p in Zone.pits)
            {
                if (p == null) continue;
                float inside = PitInside(p, x, z); if (inside <= 0) continue;
                h = Mathf.Lerp(h, h - p.depth, Mathf.SmoothStep(0, 1, inside / Mathf.Max(.5f, p.wall)));
            }
            return h;
        }
        /// <summary>The fog floor's height at a point over a pit that has one (fog under the yard), or false.</summary>
        public bool FogFloor(Vector2 p, out float y)
        {
            y = float.NegativeInfinity; if (Zone == null || Zone.pits == null) return false;
            foreach (var pit in Zone.pits)
            {
                if (pit == null || Mathf.Abs(pit.fog) < .01f || PitInside(pit, p.x, p.y) <= 0) continue;
                y = Datum + pit.fog; return true;
            }
            return false;
        }

        // ---------- worked out (after the caves' works: PrepareOpenGround) ----------
        void PrepareOpenGround()
        {
            Datum = HeightAt(Zone.spawns.player.x, Zone.spawns.player.y);
            Hazards.Clear(); HazardAreas.Clear();
            if (Zone.decks != null)
                foreach (var d in Zone.decks) { if (!OpenDeck(d)) continue; var spot = PrepareOpenDeck(d); if (spot != null) DeckSpots.Add(spot); }
            if (Zone.drops != null)
                foreach (var d in Zone.drops)
                {
                    if (d == null || !string.IsNullOrEmpty(d.fromCave) || (d.fromAt == Vector2.zero && d.toAt == Vector2.zero)) continue;
                    var from = OpenPoint(d.fromAt, d.fromRise); var to = OpenPoint(d.toAt, d.toRise);
                    if (from.y - to.y < .5f) { Debug.LogWarning(Zone.id + ": the drop '" + d.name + "' does not go down (" + from + " to " + to + ")."); continue; }
                    if (!d.scenery) Drops.Add((from, to, d.width > 0 ? d.width : 2));
                }
            if (Zone.anchors != null)
                foreach (var a in Zone.anchors)
                    if (a != null && !string.IsNullOrEmpty(a.name) && string.IsNullOrEmpty(a.cave)) Anchors[a.name] = OpenPoint(a.at, a.rise);
            if (Zone.hazards != null)
                foreach (var h in Zone.hazards)
                {
                    if (h == null) continue;
                    var mid = (h.from + h.to) / 2; var dir = h.to - h.from; float len = dir.magnitude; if (len < .3f) continue;
                    var spot = new HazardSpot { def = h, a = h.from, b = h.to, a0 = h.from, b0 = h.to, half = Mathf.Max(.5f, h.width / 2), floor = StandAt(mid, float.NegativeInfinity).y };
                    Hazards.Add(spot);
                    HazardAreas.Add((new Vector3(mid.x, spot.floor + .5f, mid.y), Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y)), new Vector3(h.width, 2.5f, len)));
                }
        }
        /// <summary>A point on open ground: the land, or the deck there nearest <paramref name="rise"/> metres over the yard.</summary>
        public Vector3 OpenPoint(Vector2 at, float rise) { return StandAt(at, Datum + rise); }
        /// <summary>Whether a world point lies on a hazard strip (within its floor's reach), and which.</summary>
        public HazardSpot HazardAt(Vector3 p)
        {
            var q = new Vector2(p.x, p.z);
            foreach (var h in Hazards)
            {
                if (Mathf.Abs(p.y - h.floor) > 2.5f) continue;
                var ab = h.b - h.a; float t = Mathf.Clamp01(Vector2.Dot(q - h.a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                if (Vector2.Distance(q, h.a + ab * t) <= h.half) return h;
            }
            return null;
        }
        /// <summary>An open-ground deck worked out: its samples every metre and a half from at to at2 (one for a round deck), each one's floor
        /// (the land, or nothing for forty metres over the chasm) and whether it is built up from it.</summary>
        ZoneDeckSpot PrepareOpenDeck(ZoneDeck d)
        {
            var spot = new ZoneDeckSpot { def = d, cave = null, half = Mathf.Max(.6f, (d.width > 0 ? d.width : 3) / 2) };
            float Under(Vector2 q, float top) { float land = HeightAt(q.x, q.y); return land <= top + .05f ? land : top - 40; }
            if (d.radius > 0)
            {
                float top = Datum + d.rise, under = Under(d.at, top);
                spot.round = true; spot.half = d.radius;
                spot.mid = new[] { new Vector3(d.at.x, top, d.at.y) }; spot.across = new[] { Vector3.right }; spot.under = new[] { under };
                spot.solid = new[] { d.solid || top - under < SolidUnder };
                spot.Seal(); return spot;
            }
            var run = d.at2 - d.at; float length = run.magnitude;
            if (length < .3f) { Debug.LogWarning(Zone.id + ": the deck '" + d.name + "' has no length."); return null; }
            int N = Mathf.Max(1, Mathf.CeilToInt(length / 1.5f));
            spot.mid = new Vector3[N + 1]; spot.across = new Vector3[N + 1]; spot.under = new float[N + 1]; spot.solid = new bool[N + 1];
            var dir = new Vector3(run.x, 0, run.y).normalized; var across = new Vector3(dir.z, 0, -dir.x);
            for (int k = 0; k <= N; k++)
            {
                float u = k / (float)N; var q = Vector2.Lerp(d.at, d.at2, u); float top = Datum + Mathf.Lerp(d.rise, d.rise2, u);
                spot.mid[k] = new Vector3(q.x, top, q.y); spot.across[k] = across; spot.under[k] = Under(q, top);
                spot.solid[k] = d.solid || top - spot.under[k] < SolidUnder;
            }
            spot.Seal(); return spot;
        }

        // ---------- built (after the props: BuildOpenGround) ----------
        void BuildOpenGround()
        {
            bool any = DeckSpots.Exists(s => s.cave == null) || (Zone.gates != null && System.Array.Exists(Zone.gates, g => g != null && string.IsNullOrEmpty(g.cave)))
                || Hazards.Count > 0 || (Zone.pits != null && Zone.pits.Length > 0);
            if (!any) return;
            var root = new GameObject("Open-ground works").transform; root.SetParent(props, false);
            var stone = Dressed(new Color(.6f, .6f, .62f)); var timber = Tint(art.timber, new Color(.36f, .34f, .32f));   // the paused town's timber: grey to the grain
            foreach (var spot in DeckSpots) if (spot.cave == null) BuildOpenDeck(spot, root, spot.def.style == "timber" ? timber : stone);
            if (Zone.gates != null) foreach (var gd in Zone.gates) if (gd != null && string.IsNullOrEmpty(gd.cave) && !string.IsNullOrEmpty(gd.title)) OpenGate(gd, root);
            // The hungry shadows: a strip of ground darker than any shade, its edges soft (two lighter strips either side).
            var dark = Tint(art.stone, new Color(.05f, .05f, .07f)); var dim = Tint(art.stone, new Color(.12f, .12f, .15f));
            foreach (var h in Hazards)
            {
                var ab = h.b - h.a; var mid = (h.a + h.b) / 2; var rot = Quaternion.LookRotation(new Vector3(ab.x, 0, ab.y));
                var at = new Vector3(mid.x, h.floor + .03f, mid.y);
                var strip = Part(PrimitiveType.Cube, root, at, new Vector3(h.half * 2, .02f, ab.magnitude), dark, rot); strip.name = "Shadow: " + h.def.name;
                var edge = Part(PrimitiveType.Cube, root, at - Vector3.up * .005f, new Vector3(h.half * 2 + 1.2f, .02f, ab.magnitude + 1.2f), dim, rot); edge.name = "Shadow's edge";
                h.looks.Add(strip.transform); h.looks.Add(edge.transform);
            }
            // The chasm's fog floor: a still sheet of the grey's static, lit faintly violet from under.
            if (Zone.pits != null)
                foreach (var p in Zone.pits)
                {
                    if (p == null || Mathf.Abs(p.fog) < .01f) continue;
                    var fog = Part(PrimitiveType.Cube, root, new Vector3(p.center.x, Datum + p.fog, p.center.y), new Vector3(p.size.x - .6f, .05f, p.size.y - .6f),
                        art.veil != null ? Tint(art.veil, new Color(.55f, .5f, .7f, .8f)) : Glowing(new Color(.45f, .4f, .6f), .4f), Quaternion.Euler(0, p.rotation, 0));
                    fog.name = "The fog in " + p.name;
                    for (float x = -p.size.x / 2 + 20; x < p.size.x / 2; x += 60) Glow(root, new Vector3(p.center.x + x, Datum + p.fog + 2, p.center.y), 30, .6f, new Color(.6f, .5f, .85f), .9f);
                }
        }
        /// <summary>One open-ground deck: a block of stone (or timber) from its start to its end, its top the walk (NavWalkable), and under it,
        /// where it is built up, a plinth to the land; over the chasm a slab with a broken underside of hanging rubble.</summary>
        void BuildOpenDeck(ZoneDeckSpot spot, Transform root, Material m)
        {
            // style "floor" (the tower's second landing): a floor of dressed stone laid in its hall, no hanging rubble under it, no posts.
            var d = spot.def; float thick = d.style == "timber" ? .3f : d.style == "floor" ? .4f : 1.1f;
            var go = new GameObject("Deck: " + d.name).transform; go.SetParent(root, false); spot.root = go;
            if (spot.round)
            {
                var c = spot.mid[0]; var top = Part(PrimitiveType.Cylinder, go, c - Vector3.up * thick / 2, new Vector3(spot.half * 2, thick / 2, spot.half * 2), m);
                top.AddComponent<MeshCollider>().sharedMesh = top.GetComponent<MeshFilter>().sharedMesh; top.AddComponent<NavWalkable>();
                if (spot.solid[0]) Plinth(go, c - Vector3.up * thick, spot.under[0], new Vector3(spot.half * 1.9f, 0, spot.half * 1.9f), Quaternion.identity, m);
                return;
            }
            var a = spot.mid[0]; var b = spot.mid[spot.mid.Length - 1]; var along = b - a; float len = along.magnitude;
            var rot = Quaternion.LookRotation(along.normalized, Vector3.up);
            // The walk: a box under the line from start to end, its top face the deck's top (slope and all).
            var walk = GameObject.CreatePrimitive(PrimitiveType.Cube); walk.name = "Walk"; walk.transform.SetParent(go, false);
            walk.transform.SetPositionAndRotation((a + b) / 2 - rot * Vector3.up * thick / 2, rot);
            walk.transform.localScale = new Vector3(spot.half * 2, thick, len);
            walk.GetComponent<MeshRenderer>().sharedMaterial = m; walk.AddComponent<NavWalkable>();
            // Built up where it stands low over the land (a plinth, a stair's mass); hanging where it does not.
            bool solid = true; foreach (var s in spot.solid) solid &= s;
            var flat = Quaternion.LookRotation(new Vector3(along.x, 0, along.z).normalized, Vector3.up);
            if (solid)
            {
                float low = Mathf.Min(a.y, b.y) - thick, under = Mathf.Min(spot.under[0], spot.under[spot.under.Length - 1]);
                Plinth(go, new Vector3((a.x + b.x) / 2, low, (a.z + b.z) / 2), under, new Vector3(spot.half * 2 - .1f, 0, new Vector2(along.x, along.z).magnitude - .1f), flat, m);
            }
            else if (d.style == "stone" && spot.under[0] < a.y - 8 && FogFloor(new Vector2(a.x, a.z), out _))
            {
                // A slab of the old bridge, frozen where the blast left it: rubble hanging under it, nothing holding it up.
                var rubble = Tint(art.stone, new Color(.5f, .5f, .53f)); var rr = new System.Random(Mathf.RoundToInt(a.x * 13 + a.z * 7));
                for (int k = 0; k < 6; k++)
                {
                    var p = Vector3.Lerp(a, b, (float)rr.NextDouble()) + flat * new Vector3(((float)rr.NextDouble() - .5f) * spot.half * 1.6f, -thick - .4f - (float)rr.NextDouble() * 1.6f, 0);
                    Part(PrimitiveType.Cube, go, p, Vector3.one * (.5f + (float)rr.NextDouble() * .7f), rubble, Quaternion.Euler(rr.Next(360), rr.Next(360), rr.Next(360)));
                }
            }
            else if (d.style == "timber" && !solid)
            {
                // A loft or a gallery on timber posts down to what is under it (the floor of its hall).
                var post = Tint(art.timber, new Color(.3f, .28f, .26f));
                for (int k = 0; k < spot.mid.Length; k += 3)
                    foreach (int side in new[] { -1, 1 })
                    {
                        var top = spot.mid[k] + spot.across[k] * side * (spot.half - .2f) - Vector3.up * thick; float foot = spot.under[k]; if (top.y - foot < .5f || top.y - foot > 9) continue;   // (the belfry floor hangs from the tower's walls, not on posts through the stair)
                        Part(PrimitiveType.Cube, go, new Vector3(top.x, (top.y + foot) / 2, top.z), new Vector3(.25f, top.y - foot, .25f), post);
                    }
            }
        }
        /// <summary>A plinth from <paramref name="top"/> down to the land at <paramref name="foot"/>: solid (a collider) and kept out of the
        /// navmesh's way (its top is the deck's walk).</summary>
        void Plinth(Transform parent, Vector3 top, float foot, Vector3 size, Quaternion rot, Material m)
        {
            float h = top.y - foot + .3f; if (h < .2f) return;
            var o = Part(PrimitiveType.Cube, parent, new Vector3(top.x, top.y - h / 2, top.z), new Vector3(size.x, h, size.z), m, rot); o.name = "Plinth";
            o.AddComponent<BoxCollider>();
        }

        /// <summary>A gate on open ground (ZoneGateDef with no cave): two stone posts and what shuts the way between them by its look (a door of
        /// planks, a stockade's poles, a bar across a low gate, a door that looks like the wall), registered as the cave gates are (Gates; the
        /// thing to use when it has a prompt). It lifts out of the way as it opens.</summary>
        ZoneGate OpenGate(ZoneGateDef gd, Transform parent)
        {
            var root = new GameObject("Gate: " + gd.title).transform; root.SetParent(parent, false);
            var foot = OpenPoint(gd.at, gd.rise); root.SetPositionAndRotation(foot, Quaternion.Euler(0, gd.rotation, 0));
            float w = gd.width > 0 ? gd.width : 6, high = 3.4f; string kind = string.IsNullOrEmpty(gd.kind) ? "bossdoor" : gd.kind, look = string.IsNullOrEmpty(gd.look) ? "door" : gd.look;
            var stone = Dressed(new Color(.62f, .62f, .64f)); var plank = Tint(art.timber, new Color(.32f, .3f, .28f)); var iron = Tint(art.metal, new Color(.18f, .18f, .2f));
            foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, root, new Vector3(side * (w / 2 + .35f), high / 2 + .2f, 0), new Vector3(.7f, high + .4f, .7f), stone);
            Part(PrimitiveType.Cube, root, new Vector3(0, high + .45f, 0), new Vector3(w + 1.4f, .5f, .8f), stone);   // the lintel
            var bars = new GameObject("Bars").transform; bars.SetParent(root, false);
            switch (look)
            {
                case "stockade":
                    for (float x = -w / 2 + .15f; x < w / 2; x += .3f) Part(PrimitiveType.Cylinder, bars, new Vector3(x, high / 2, 0), new Vector3(.18f, high / 2, .18f), plank);
                    Part(PrimitiveType.Cube, bars, new Vector3(0, high * .7f, -.12f), new Vector3(w, .14f, .1f), iron);
                    break;
                case "bar":
                    Part(PrimitiveType.Cube, bars, new Vector3(0, .9f, 0), new Vector3(w, 1.8f, .12f), plank);
                    Part(PrimitiveType.Cube, bars, new Vector3(0, 1.1f, -.16f), new Vector3(w + .9f, .22f, .22f), plank);   // the bar itself, across both posts
                    break;
                case "grille":
                    for (float x = -w / 2 + .1f; x < w / 2; x += .22f) Part(PrimitiveType.Cube, bars, new Vector3(x, high / 2, 0), new Vector3(.05f, high, .05f), iron);
                    break;
                default:   // a door of planks (the gatehouse's, the tower's ward-locked door: the Bureau's black sunburst burned on it)
                    Part(PrimitiveType.Cube, bars, new Vector3(0, high / 2, 0), new Vector3(w, high, .22f), plank);
                    foreach (float y in new[] { .5f, high - .5f }) Part(PrimitiveType.Cube, bars, new Vector3(0, y, -.13f), new Vector3(w, .12f, .05f), iron);
                    if (kind == "ward")
                    {
                        var black = Glowing(new Color(.05f, .04f, .06f), .2f);
                        Part(PrimitiveType.Cylinder, bars, new Vector3(0, high * .55f, -.14f), new Vector3(.9f, .01f, .9f), black, Quaternion.Euler(90, 0, 0));
                        for (int k = 0; k < 8; k++) Part(PrimitiveType.Cube, bars, new Vector3(0, high * .55f, -.15f), new Vector3(.08f, 1.5f, .02f), black, Quaternion.Euler(0, 0, k * 22.5f));
                    }
                    break;
            }
            var gate = root.gameObject.AddComponent<ZoneGate>(); gate.Title = gd.title; gate.Cave = null; gate.Along = 0; gate.Lord = gd.lord; gate.Grille = bars; gate.ShutsBehind = gd.behind;
            gate.Rise = high + .2f; gate.Kind = kind; gate.FromFar = gd.side == "far"; gate.OpensOn = string.IsNullOrEmpty(gd.opensOn) ? null : gd.opensOn;
            gate.Init(new Vector3(w + .2f, high, .6f));
            Gates.Add(gate);
            string prompt = !string.IsNullOrEmpty(gd.prompt) ? gd.prompt : kind == "ward" ? "Study the ward-lock" : null;
            if (prompt != null) Interactables.Add(new ZoneInteractable { name = gd.title, prompt = prompt, kind = kind, position = foot + root.forward * (gate.FromFar ? 1.2f : -1.2f), root = root, gate = gate });
            return gate;
        }
    }
}
