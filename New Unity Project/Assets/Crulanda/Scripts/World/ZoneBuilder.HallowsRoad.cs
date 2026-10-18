using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Ash Road's dangers and the bridge as the book left it (Docs/REWORK_LOWTOWNS_HALLOWS.md, Hallow's Creek picks 7 and 10; book1
    /// ch.17). The prop kinds built here (none of them a machine, DUNGEON_DESIGN 0.05; none with a collider, so the navmesh is as it was):
    /// - "fissure": a crack in the ground along the road giving off a pale green light ("fissures emitting a pale green light", ch.17:185);
    /// - "wrongshadow": a petrified stump whose shadow lies toward the sun, not away from it ("shadows ... an absence of time", ch.17:11;
    ///   "stretching the wrong way at noon", lore #5);
    /// - "stoneleaves": a thicket of stone leaves "sharp enough to cut skin" (ch.17:23), off the road; walking through one bleeds you a
    ///   little (EncounterSession.AshRoad reads <see cref="LeafThickets"/>);
    /// - "driftslab": a slab of the broken bridge hanging beside the walk (ch.17:193): variant 0 tilted and still, 1 slowly spinning
    ///   ("Some stones were tilted sideways. Some were spinning slowly.", ch.17:201); its height over the yard is the prop's rise;
    /// - "tipslab": names one of the walk's own decks (the prop's name) that groans and dips under a foot and tips to forty-five degrees
    ///   once the party is past it (ch.17:243; EncounterSession.AshRoad puts a <see cref="TippingSlab"/> on it).
    /// Each kind draws from its own stream (HallowsKinds), so nothing else in the zone moves.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The thickets of stone leaves: centre (world), facing and half size (x across, y along).</summary>
        public readonly List<(Vector3 centre, Quaternion facing, Vector2 half)> LeafThickets = new List<(Vector3, Quaternion, Vector2)>();
        /// <summary>The names of the bridge's decks that tip (a "tipslab" prop each).</summary>
        public readonly List<string> TipSlabs = new List<string>();
        /// <summary>Whether a world point stands in a thicket of stone leaves.</summary>
        public bool InStoneLeaves(Vector3 p)
        {
            foreach (var t in LeafThickets)
            {
                var l = Quaternion.Inverse(t.facing) * (p - t.centre);
                if (Mathf.Abs(l.x) <= t.half.x && Mathf.Abs(l.z) <= t.half.y && Mathf.Abs(l.y) < 3) return true;
            }
            return false;
        }
        /// <summary>The pale green of the fissures' light (ch.17:185).</summary>
        static readonly Color FissureGreen = new Color(.62f, .95f, .66f);

        /// <summary>Builds an Ash Road or bridge kind; false for any other kind.</summary>
        bool AshRoadProp(ZoneProp p, Transform t)
        {
            switch (p.kind)
            {
                case "fissure": Fissure(t, p.size.x > 0 ? p.size.x : 7); return true;
                case "wrongshadow": WrongShadow(t, p.size.x > 0 ? p.size.x : 7); return true;
                case "stoneleaves": StoneLeaves(t, p.size.x > 0 ? p.size : new Vector2(6, 8)); return true;
                case "driftslab": DriftSlab(t, p); return true;
                case "tipslab": if (!string.IsNullOrEmpty(p.name)) TipSlabs.Add(p.name); return true;
            }
            return false;
        }

        /// <summary>A jagged crack along the prop's z, its lips dark and its depth lit pale green, with a faint green light over it.</summary>
        void Fissure(Transform t, float length)
        {
            var parts = new Parts(); var glow = Glowing(FissureGreen, 1.6f); var lip = Tint(art.stone, new Color(.18f, .18f, .2f));
            var rr = new System.Random(Mathf.RoundToInt(t.position.x * 31 + t.position.z * 17));
            float x = 0; int n = Mathf.Max(3, Mathf.RoundToInt(length / 1.2f));
            for (int k = 0; k < n; k++)
            {
                float z0 = -length / 2 + length * k / n, z1 = -length / 2 + length * (k + 1) / n;
                float x1 = Mathf.Clamp(x + ((float)rr.NextDouble() - .5f) * .9f, -1.2f, 1.2f);
                var a = new Vector3(x, 0, z0); var b = new Vector3(x1, 0, z1); var mid = (a + b) / 2; var d = b - a;
                var rot = Quaternion.LookRotation(d.normalized);
                float wide = .12f + (float)rr.NextDouble() * .16f;
                // The glow down in the crack, a hair over the ground so it reads from above, and the dark lips either side of it.
                parts.Add(PrimitiveType.Cube, mid + Vector3.up * .025f, new Vector3(wide, .03f, d.magnitude + .05f), glow, rot);
                parts.Add(PrimitiveType.Cube, mid + rot * new Vector3(wide * .5f + .08f, .02f, 0), new Vector3(.16f, .03f, d.magnitude), lip, rot);
                parts.Add(PrimitiveType.Cube, mid + rot * new Vector3(-wide * .5f - .08f, .02f, 0), new Vector3(.16f, .03f, d.magnitude), lip, rot);
                x = x1;
            }
            Flush(parts, t, "A green fissure");
            Glow(t, new Vector3(0, .6f, 0), Mathf.Max(5, length * .8f), .5f, FissureGreen, .9f);
        }

        /// <summary>
        /// A petrified stump and its shadow lying toward the sun: a strip darker than any shade from the stump's foot to where the light
        /// comes from (the zone's sun at noon), its edge soft. Dressing only: the hungry shadows that slow you are the zone's hazards.
        /// </summary>
        void WrongShadow(Transform t, float length)
        {
            var stone = Tint(art.stone, new Color(.42f, .42f, .44f));
            float sunYaw = (Zone.lighting != null ? Zone.lighting.sunYaw : 0) + 180;
            // The way toward the sun, in the prop's own space (the root may be turned).
            var toSun = Quaternion.Inverse(t.rotation) * (Quaternion.Euler(0, sunYaw, 0) * Vector3.forward);
            var rot = Quaternion.LookRotation(new Vector3(toSun.x, 0, toSun.z).normalized);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .9f, 0), new Vector3(.55f, .9f, .55f), stone);
            Part(PrimitiveType.Cylinder, t, new Vector3(.15f, 1.9f, .05f), new Vector3(.22f, .35f, .22f), stone, Quaternion.Euler(0, 0, 24));
            var dark = Tint(art.stone, new Color(.04f, .04f, .06f)); var dim = Tint(art.stone, new Color(.11f, .11f, .14f));
            var mid = rot * new Vector3(0, .03f, length / 2);
            var strip = Part(PrimitiveType.Cube, t, mid, new Vector3(.8f, .02f, length), dark, rot); strip.name = "A shadow toward the sun";
            Part(PrimitiveType.Cube, t, mid - Vector3.up * .005f, new Vector3(1.4f, .02f, length + .5f), dim, rot).name = "Its soft edge";
        }

        /// <summary>A thicket of stone leaves: low grey stems and blades turned to rock, thin and sharp-edged, packed over w x d.</summary>
        void StoneLeaves(Transform t, Vector2 size)
        {
            var parts = new Parts(); var leaf = Tint(art.stone, new Color(.5f, .51f, .53f)); var stem = Tint(art.stone, new Color(.36f, .36f, .38f));
            var rr = new System.Random(Mathf.RoundToInt(t.position.x * 13 + t.position.z * 29));
            int bushes = Mathf.Clamp(Mathf.RoundToInt(size.x * size.y / 3f), 6, 40);
            for (int b = 0; b < bushes; b++)
            {
                var c = new Vector3(((float)rr.NextDouble() - .5f) * size.x, 0, ((float)rr.NextDouble() - .5f) * size.y);
                c.y = LocalGround(t, c.x, c.z);
                float h = .5f + (float)rr.NextDouble() * .8f;
                parts.Add(PrimitiveType.Cylinder, c + Vector3.up * h / 2, new Vector3(.08f, h / 2, .08f), stem);
                for (int k = 0; k < 7; k++)
                {
                    // A leaf: a thin blade on edge, tilted out from the stem, its point up (sharp enough to cut skin, ch.17:23).
                    float yaw = rr.Next(360), tilt = 20 + rr.Next(45), up = h * (.4f + (float)rr.NextDouble() * .7f);
                    var rot = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(tilt, 0, 0);
                    parts.Add(PrimitiveType.Cube, c + Vector3.up * up + rot * new Vector3(0, .18f, 0), new Vector3(.02f, .38f, .14f), leaf, rot * Quaternion.Euler(0, 0, 8));
                }
            }
            Flush(parts, t, "Stone leaves");
            LeafThickets.Add((t.position, t.rotation, size / 2));
        }

        /// <summary>A slab of the broken bridge hanging in the air beside the walk: paving on top, its broken underside rubble. Variant 1
        /// turns slowly (<see cref="SlowSpin"/>); variant 0 hangs tilted and still. No collider: no one stands on it.</summary>
        void DriftSlab(Transform t, ZoneProp p)
        {
            t.position = new Vector3(t.position.x, Datum + p.rise, t.position.z);
            var size = p.size.x > 0 ? p.size : new Vector2(5, 4);
            var rr = new System.Random(Mathf.RoundToInt(p.at.x * 11 + p.at.y * 23));
            var pivot = new GameObject("The slab").transform; pivot.SetParent(t, false);
            pivot.localRotation = Quaternion.Euler(-25 + rr.Next(50), rr.Next(360), -30 + rr.Next(60));
            var paving = Dressed(new Color(.6f, .6f, .62f)); var rubble = Tint(art.stone, new Color(.5f, .5f, .53f));
            Part(PrimitiveType.Cube, pivot, Vector3.zero, new Vector3(size.x, .9f, size.y), paving);
            for (int k = 0; k < 4; k++)
                Part(PrimitiveType.Cube, pivot, new Vector3(((float)rr.NextDouble() - .5f) * size.x * .8f, -.7f - (float)rr.NextDouble(), ((float)rr.NextDouble() - .5f) * size.y * .8f),
                    Vector3.one * (.4f + (float)rr.NextDouble() * .6f), rubble, Quaternion.Euler(rr.Next(360), rr.Next(360), rr.Next(360)));
            if (p.variant == 1)
            {
                var spin = pivot.gameObject.AddComponent<SlowSpin>();
                spin.axis = new Vector3(((float)rr.NextDouble() - .5f) * .6f, 1, ((float)rr.NextDouble() - .5f) * .6f).normalized;
                spin.degreesPerSecond = (rr.Next(2) == 0 ? -1 : 1) * (5 + (float)rr.NextDouble() * 7);
            }
        }
    }
}
