using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The smugglers' drowned vaults (Docs/REWORK_LOWTOWNS_HALLOWS.md, Lowtowns pick 11 and conflict 4; Docs/ZONE_LOWTOWNS.md "The drowned
    /// vaults"). CANON-EXPANDED: the Warrens' "drowned lower levels", a haven for "smugglers, fences" at the edge of the underground river,
    /// "a narrow, precarious causeway that hugged the crumbling edge of a massive, black-water canal", faint green light on "high, arched
    /// ceilings" (book1 ch.6:185-187), the "greenish glow of bioluminescent mold clinging to the archways" (ch.6:129), and a black skiff,
    /// its sails furled, moored "to a rotting, iron-ringed wooden pier" (ch.6:203). Here they are the Warrens' upper edge under the Rust
    /// Market (conflict 4's answer): rubble-stone vaults, never the Sludge-Tunnels' red sewer brick.
    /// The caves are three "cavern" props of style <see cref="VaultStyle"/> in the zone's data (the cave tech the Adit and Hallow's Creek's
    /// cellars use): the vaults themselves, down a cellar stair from the market's north edge; the drain crawl, from the Drain Courts under
    /// the High Rows and the river into the vaults' west end; and the river culvert, off the river's east bank into the crawl. So there
    /// are three ways in, and a loop under the river. The black canal is cave water of the Chemical River's kind (it poisons and hides you
    /// as the river does). Here, for the eye (no colliders but the boat's hull):
    /// - rubble-stone ribs across the vaults every few metres, each one unbroken band from the floor up one wall, over the roof and down
    ///   the other, on a footing at each foot, and a heavier arch at each mouth (the cave builder leaves out its dripstone, outcrops and
    ///   rock ledges in a vault: ZoneBuilder.CaveDressing, ZoneBuilder.Adit's FloorClutter; a little fallen rubble stays at the walls);
    /// - the green mould as thin pads on the ribs' stones, thicker on every third rib with a dim green light there;
    /// - a broken stone kerb along the canal's edge (the causeway's crumbling edge);
    /// - stone treads down the cellar stair and the other mouths' slopes;
    /// - the fence's pier and black skiff, its sail furled, crates of contraband, a lantern. No machines: stone, timber, rope, iron.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The style of the vaults' caverns in the zone's data.</summary>
        public const string VaultStyle = "vaults";
        /// <summary>The three caves: the vaults, the crawl from the Drain Courts, the culvert from the river bank.</summary>
        public const string VaultsCave = "The drowned vaults", CrawlCave = "The drain crawl", CulvertCave = "The river culvert";
        /// <summary>The fence whose skiff is moored at the pier (a resident with a post under the land; GAME-ONLY).</summary>
        public const string FenceName = "Ottie Brack";
        /// <summary>The canal's name in the zone's cave water.</summary>
        public const string CanalName = "The black canal";

        static bool IsVault(Hollow h) { return h != null && h.Style == VaultStyle; }

        void BuildDrownedVaults()
        {
            var vaults = Hollow.All.FindAll(IsVault); if (vaults.Count == 0) return;
            var root = new GameObject("The drowned vaults").transform; root.SetParent(statics, false); root.position = Vector3.zero; root.rotation = Quaternion.identity;
            var r = new System.Random(Zone.seed + 4111); float R() { return (float)r.NextDouble(); }
            var b = new Batch();
            var rubble = Tint(art.stone, new Color(.34f, .32f, .29f)); var rubble2 = Tint(art.stone, new Color(.27f, .26f, .24f));
            var cope = WallCoping; var mould = DrainMould; var plank = Tint(art.timber, new Color(.26f, .21f, .16f)); var timber = TarredTimber; var iron = SootIron;
            var hull = Tint(art.timber, new Color(.045f, .045f, .05f)); var sail = Tint(art.cloth, new Color(.2f, .19f, .17f)); var crate = Tint(art.timber, new Color(.36f, .27f, .18f));
            var rope = Tint(art.cloth, new Color(.42f, .36f, .26f));
            var green = new Color(.35f, 1f, .55f);
            int RingAt(Hollow h, float s) { int i = 0; while (i + 1 < h.Along.Count && h.Along[i + 1] <= s) i++; return i; }
            Vector3 Forward(Hollow h, float s) { var f = h.At(s + .5f) - h.At(Mathf.Max(0, s - .5f)); f.y = 0; return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward; }

            foreach (var h in vaults)
            {
                float len = h.Length;
                // A rib of rubble stone across the passage at s: one unbroken band of stones from the floor at one wall, up the wall as a
                // pier, over the roof and down to the floor at the other, drawn a little inside the shell's own profile (the rock's bumps hide
                // it here and there). A heavier footing at each foot. heavy: a mouth's portal. lit: a thick growth of the green mould on it
                // at about head height on one side, and its dim light.
                void Rib(float s, bool heavy, bool lit)
                {
                    int i = RingAt(h, s); if (!h.Floored(i) || h.Height[i] < 2.4f) return;
                    // Not in another cave's air (where two meet).
                    var c = h.At(s); if (Hollow.OpenInOther(h, c + Vector3.up * 1.2f, .98f)) return;
                    var f = Forward(h, s); var right = new Vector3(f.z, 0, -f.x);
                    float half = h.Half[i] * .9f, high = h.Height[i] * .92f, deep = heavy ? 1.1f : .7f, block = heavy ? .7f : .5f;
                    var axis = c + Vector3.up * high * .4f;
                    Vector3 Point(float th)
                    {
                        float cs = Mathf.Cos(th), sn = Mathf.Sin(th);
                        float side = (half - block / 2) * Mathf.Sign(cs) * Mathf.Pow(Mathf.Abs(cs), .75f), up = (high - block / 2) * Mathf.Pow(Mathf.Max(0, sn), .85f);
                        return c + right * side + Vector3.up * up;
                    }
                    const int N = 22;
                    int litAt = lit ? (R() < .5f ? 4 : N - 5) : -1;
                    for (int k = 0; k < N; k++)
                    {
                        var p0 = Point(k * Mathf.PI / N); var p1 = Point((k + 1) * Mathf.PI / N);
                        var run = p1 - p0; float length = run.magnitude; if (length < .01f) continue;
                        var mid = (p0 + p1) / 2; var tangent = run / length;
                        var inward = Vector3.Cross(f, tangent).normalized; if (Vector3.Dot(inward, axis - mid) < 0) inward = -inward;
                        var q = Quaternion.LookRotation(f, inward);
                        // The stones overlap a little at each joint, so the band runs without a gap.
                        b.Box((k * 7 + (int)(s * 3)) % 3 == 0 ? rubble2 : rubble, mid, new Vector3(length + block * .35f, block * (.9f + R() * .2f), deep * (.92f + R() * .16f)), q);
                        // The mould clinging to the archways (book1 ch.6:129): thin pads on the stones' inner faces, here and there.
                        bool thick = k == litAt || k == litAt + 1;
                        if (thick || R() < .22f)
                        {
                            int pads = thick ? 4 : 1 + (int)(R() * 2);
                            for (int m = 0; m < pads; m++)
                            {
                                var along = tangent * (R() - .5f) * length * .7f + f * (R() - .5f) * deep * .6f;
                                b.Box(mould, mid + along + inward * (block * .5f + .012f), new Vector3(.18f + R() * .35f, .025f, .15f + R() * .3f), q * Quaternion.Euler(0, R() * 90, 0));
                            }
                        }
                        if (k == litAt) Glow(root, mid + inward * .8f, 8, .55f, green, .55f);
                    }
                    // The footings: a broader course of stone where each pier meets the floor.
                    foreach (int sd in new[] { -1, 1 })
                    {
                        var foot = c + right * sd * (half - block * .6f);
                        b.Box(rubble2, foot + Vector3.up * .35f, new Vector3(block * 1.7f, .7f, deep * 1.25f), Quaternion.LookRotation(f));
                    }
                }
                Rib(1.6f, true, false);
                int every = h.Name == VaultsCave ? 3 : 5, no = 0;
                for (float s = 6; s < len - 2.5f; s += 4.2f, no++) Rib(s, false, no % every == 1);

                // Treads down the stair and the mouths' slopes: where the floor falls more than a step in a metre and a half.
                for (float s = 1; s < Mathf.Min(len - 2, 45); s += .55f)
                {
                    var a0 = h.At(s); var a1 = h.At(s + .55f); if (a0.y - a1.y < .2f) continue;
                    int i = RingAt(h, s); var f = Forward(h, s);
                    b.Box(cope, new Vector3(a0.x, Mathf.Max(a0.y, a1.y) + .04f, a0.z), new Vector3(h.Half[i] * 1.6f, .14f, .32f), Quaternion.LookRotation(f));
                }
            }

            // The canal's crumbling kerb on the causeway side, and the fence's pier and skiff.
            var vaultsCave = vaults.Find(h => h.Name == VaultsCave);
            ZoneCaveWater canal = null; if (Zone.caveWater != null) foreach (var w in Zone.caveWater) if (w != null && w.name == CanalName) canal = w;
            if (vaultsCave != null && canal != null)
            {
                var h = vaultsCave; float edge = canal.aside - canal.half - .15f;
                for (float s = canal.from; s < canal.to; s += 1.25f)
                {
                    // Crumbled away here and there.
                    if (R() < .12f) continue;
                    var f = Forward(h, s); var at = h.At(s, edge);
                    b.Box(R() < .5f ? cope : rubble2, at + Vector3.up * .06f, new Vector3(.38f, .2f, 1.2f), Quaternion.LookRotation(f) * Quaternion.Euler(0, (R() - .5f) * 6, 0));
                }
                ZoneResident fence = null; if (Zone.life != null && Zone.life.residents != null) foreach (var p in Zone.life.residents) if (p != null && p.name == FenceName) fence = p;
                if (fence != null)
                {
                    h.Local(fence.at, out float along, out _);
                    var f = Forward(h, along); var q = Quaternion.LookRotation(f); var right = new Vector3(f.z, 0, -f.x);
                    float water = h.At(along).y - .12f;
                    // The pier: planks on posts along the canal's edge, the posts down into the black water, iron rings on them.
                    var pierMid = h.At(along, canal.aside - canal.half + .55f);
                    b.Box(plank, new Vector3(pierMid.x, water + .3f, pierMid.z), new Vector3(1.3f, .1f, 7.5f), q);
                    for (float k = -3.5f; k <= 3.6f; k += 1.75f)
                    {
                        var post = h.At(along + k, canal.aside - canal.half + 1.1f);
                        b.Box(timber, new Vector3(post.x, water - .2f, post.z), new Vector3(.22f, 1.5f, .22f), q);
                        b.Box(iron, new Vector3(post.x, water + .5f, post.z) + right * .13f, new Vector3(.04f, .26f, .26f), q);
                    }
                    // The skiff: long, low and black, its sail furled along the boom under the low roof (ch.6:203).
                    var mid = h.At(along, canal.aside + .2f); var deck = new Vector3(mid.x, water + .35f, mid.z);
                    b.Box(hull, deck + Vector3.down * .3f, new Vector3(1.7f, .7f, 5.6f), q);
                    b.Box(hull, deck + Vector3.down * .3f + f * 3.2f, new Vector3(1.1f, .66f, 1.2f), q * Quaternion.Euler(0, 45, 0));
                    b.Box(plank, deck + Vector3.up * .06f, new Vector3(1.45f, .05f, 5.2f), q);
                    float roof = h.Height[RingAt(h, along)] * .85f;
                    float mast = Mathf.Clamp(roof - .9f, 1.4f, 3.2f);
                    b.Box(timber, deck + Vector3.up * mast / 2 + f * .8f, new Vector3(.12f, mast, .12f), q);
                    b.Box(timber, deck + Vector3.up * 1.1f - f * .9f, new Vector3(.08f, .08f, 3.4f), q);
                    b.AddMesh(Resources.GetBuiltinResource<Mesh>("Cylinder.fbx"), sail, Matrix4x4.TRS(deck + Vector3.up * 1.22f - f * .9f, q * Quaternion.Euler(90, 0, 0), new Vector3(.24f, 1.6f, .24f)));
                    for (int k = 0; k < 4; k++) b.Box(crate, deck + Vector3.up * .32f + f * (-1.6f + k * .75f) + right * (R() - .5f) * .6f, Vector3.one * (.45f + R() * .15f), q * Quaternion.Euler(0, (R() - .5f) * 30, 0));
                    // Contraband stacked on the pier, either side of the fence's place.
                    foreach (float k in new[] { -2.6f, 2.8f })
                    {
                        var at = h.At(along + k, canal.aside - canal.half - .6f);
                        b.Box(crate, new Vector3(at.x, h.At(along + k).y + .3f, at.z), new Vector3(.6f, .6f, .6f), q);
                        b.Box(crate, new Vector3(at.x, h.At(along + k).y + .82f, at.z), new Vector3(.45f, .45f, .45f), q * Quaternion.Euler(0, 20, 0));
                        // The fence's hoard on the stacks (the kit's, playtest note 234): coin and a purse on one, a cup and keys on the other.
                        var top = root.InverseTransformPoint(new Vector3(at.x, h.At(along + k).y + 1.045f, at.z)); float yaw = q.eulerAngles.y;
                        if (k < 0) { SmallPiece(ModelProp(root, "Fantasy/Chalice", top + new Vector3(.08f, 0, .05f), yaw, .2f)); SmallPiece(ModelProp(root, "Fantasy/Key_Metal", top + new Vector3(-.1f, 0, -.08f), yaw + 60, .012f)); }
                        else { SmallPiece(ModelProp(root, "Fantasy/Coin_Pile_2", top, yaw + 20, .06f)); SmallPiece(ModelProp(root, "Fantasy/Pouch_Large", top + new Vector3(.1f, 0, .12f), yaw - 30, .13f)); }
                    }
                    // Mooring lines, bow and stern, to the pier's end posts.
                    foreach (float k in new[] { -2.4f, 2.4f })
                    {
                        var a = deck + f * k + Vector3.up * .1f; var post = h.At(along + k * 1.4f, canal.aside - canal.half + 1.1f); var p2 = new Vector3(post.x, water + .5f, post.z);
                        var run = p2 - a; if (run.sqrMagnitude > .01f) b.Box(rope, (a + p2) / 2, new Vector3(.04f, .04f, run.magnitude), Quaternion.LookRotation(run));
                    }
                    // The lantern on the stern: the only warm light down here.
                    var lamp = deck + Vector3.up * 1.4f - f * 2.4f;
                    b.Box(iron, lamp + Vector3.down * .6f, new Vector3(.05f, 1.2f, .05f), q);
                    Part(PrimitiveType.Sphere, root, lamp, Vector3.one * .16f, Glowing(new Color(1, .7f, .35f), 2));
                    Glow(root, lamp, 9, .9f, new Color(1, .7f, .35f), .9f);
                    // The hull is solid (agents and you go round it, not through it).
                    Blocker(root, deck + Vector3.down * .1f, new Vector3(1.7f, 1.2f, 6.2f), q);
                }
            }
            b.Flush(this, root, "The drowned vaults");
        }
    }
}
