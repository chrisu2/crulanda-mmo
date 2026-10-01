using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A workshop for each trade that had none of its own (GAME-ONLY): the leatherworker's shop, the herbalist's drying hut, the
    /// inn's kitchen lean-to and the hunter's game rack. Each is built from the painted building parts the houses use, draws
    /// from its own random stream (BuildProps' private-stream list), and ends by registering where its villager stands
    /// (<see cref="Workplace"/>). Fronts face -Z.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The walls' footprint of each roofed workshop (x along its front, y its depth). Their props carry no size: a
        /// sized prop would stop the creek's swing and clear a wider ring of grove trees.</summary>
        public static readonly Vector2 LeatherShopSize = new Vector2(6, 5), DryingHutSize = new Vector2(5, 4.5f), KitchenSize = new Vector2(5, 3);
        /// <summary>Where the kitchen's back door stands along the inn's wall (the kitchen's local x; the inn's x is the kitchen's
        /// middle less this, the two facing opposite ways).</summary>
        public const float KitchenDoorX = 1.9f;
        static readonly Color[] HideTints = { new Color(.62f, .48f, .32f), new Color(.5f, .36f, .22f), new Color(.7f, .6f, .46f), new Color(.4f, .27f, .17f), new Color(.56f, .4f, .26f) };
        /// <summary>Leather and hides: tan, brown, pale, dark, chestnut.</summary>
        Material HideOf(int i) { return Tint(art.cloth, HideTints[((i % HideTints.Length) + HideTints.Length) % HideTints.Length]); }
        static readonly Color[] HerbHeads = { new Color(.93f, .92f, .84f), new Color(.92f, .78f, .26f), new Color(.6f, .42f, .74f), new Color(.5f, .62f, .32f), new Color(.8f, .46f, .3f) };

        /// <summary>
        /// Where a kitchen lean-to stands against an inn's back wall: the inn's local x of the kitchen's middle, or null when the
        /// zone has none there. <paramref name="face"/> is the back wall's outer face (the inn's local z).
        /// </summary>
        float? KitchenBehind(Transform inn, float face)
        {
            foreach (var p in Zone.props)
            {
                if (p == null || p.kind != "kitchen") continue;
                var local = inn.InverseTransformPoint(Ground(p.at));
                if (Mathf.Abs(local.z - (face + KitchenSize.y / 2)) < .5f) return local.x;
            }
            return null;
        }
        /// <summary>A barred plank door in its frame, standing against a wall face at local z = <paramref name="face"/> and looking
        /// along -z (an inn's inner back wall seen from the taproom; the same wall's outer face seen from the kitchen).</summary>
        void InnerDoor(Transform t, float x, float face, Material planks)
        {
            PlankDoor(t, new Vector3(x, 1.05f, face - .04f), .8f, 2.1f, planks);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(x + s * .46f, 1.09f, face - .05f), new Vector3(.12f, 2.18f, .1f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(x, 2.18f, face - .05f), new Vector3(1.04f, .12f, .1f), art.timber);
        }
        /// <summary>A bunch of herbs tied by the stalks and hung head down from <paramref name="hook"/>: a cord, the bound stalks
        /// flaring downward and the dried flower heads at the bottom (kind: 0 yarrow white, 1 feverfew yellow, 2 comfrey violet,
        /// 3 sage green, 4 marigold rust). It hangs .46 m at size 1.</summary>
        void HerbBundle(Transform t, Vector3 hook, int kind, float size = 1)
        {
            var bunch = PropMesh("Herb bunch", () => ZoneMeshes.Cone(.1f, .36f, 7));
            var o = MeshPart(bunch, t, hook + new Vector3(0, -.42f * size, 0), Tint(art.foliage, kind % 2 == 0 ? new Color(.44f, .5f, .28f) : new Color(.52f, .5f, .3f)), Quaternion.Euler(0, kind * 37, 0));
            o.transform.localScale = Vector3.one * size;
            Part(PrimitiveType.Cube, t, hook + new Vector3(0, -.04f * size, 0), new Vector3(.012f, .08f * size, .012f), Tint(art.hay, new Color(.6f, .54f, .42f)));
            Part(PrimitiveType.Sphere, t, hook + new Vector3(0, -.41f * size, 0), new Vector3(.23f, .1f, .23f) * size, Tint(art.foliage, HerbHeads[((kind % HerbHeads.Length) + HerbHeads.Length) % HerbHeads.Length]));
        }
        /// <summary>A hare hung by its hind legs from <paramref name="hook"/> on a cord, head down: .87 m from the hook to the tips of its ears.</summary>
        void HungHare(Transform t, Vector3 hook, float yaw = 0)
        {
            var fur = Tint(art.cloth, new Color(.5f, .41f, .3f));
            var o = new GameObject("Hare").transform; o.SetParent(t, false); o.localPosition = hook; o.localRotation = Quaternion.Euler(0, yaw, 0);
            Part(PrimitiveType.Cube, o, new Vector3(0, -.07f, 0), new Vector3(.012f, .14f, .012f), Tint(art.hay, new Color(.6f, .54f, .42f)));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Capsule, o, new Vector3(s * .03f, -.22f, 0), new Vector3(.045f, .11f, .045f), fur, Quaternion.Euler(0, 0, s * 8));   // hind legs, tied at the hocks
            Part(PrimitiveType.Capsule, o, new Vector3(0, -.47f, 0), new Vector3(.14f, .2f, .13f), fur);
            Part(PrimitiveType.Sphere, o, new Vector3(0, -.71f, -.01f), new Vector3(.1f, .13f, .1f), fur);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Capsule, o, new Vector3(s * .025f, -.81f, .01f), new Vector3(.03f, .07f, .015f), fur);   // ears
            Part(PrimitiveType.Sphere, o, new Vector3(0, -.3f, .07f), Vector3.one * .06f, Tint(art.cloth, new Color(.9f, .87f, .8f)));   // scut
        }
        /// <summary>A lidded clay jar standing on <paramref name="foot"/>, .22 m tall at size 1, with a cloth tied over its mouth.</summary>
        void Jar(Transform t, Vector3 foot, Color clay, float size = 1)
        {
            var jar = PropMesh("Jar", () => Turned(new[] { new Vector2(0, 0), new Vector2(.06f, 0), new Vector2(.09f, .06f), new Vector2(.09f, .14f), new Vector2(.055f, .2f), new Vector2(.065f, .22f), new Vector2(0, .22f) }, 9));
            MeshPart(jar, t, foot, Tint(art.stone, clay)).transform.localScale = Vector3.one * size;
            Part(PrimitiveType.Cylinder, t, foot + new Vector3(0, .225f * size, 0), new Vector3(.15f, .012f, .15f) * size, Tint(art.cloth, new Color(.82f, .76f, .62f)));
        }

        // ---------- the leatherworker's shop ----------
        /// <summary>One of the four trade bags hung by its thong from <paramref name="hook"/>, its flap to the front (-z): 0 a small
        /// simples-wallet, 1 a wide log-sling, 2 a larder-scrip (a satchel), 3 an ore-poke (a round drawstring sack).</summary>
        void HungBag(Transform t, Vector3 hook, int kind)
        {
            var leather = HideOf(kind + 1); var flap = HideOf(kind + 3); var brass = Tint(art.metal, new Color(.72f, .58f, .28f));
            Part(PrimitiveType.Cube, t, hook + new Vector3(0, -.09f, 0), new Vector3(.02f, .18f, .012f), HideOf(3));
            if (kind == 3)
            {
                Part(PrimitiveType.Sphere, t, hook + new Vector3(0, -.38f, 0), new Vector3(.3f, .38f, .26f), leather);
                Part(PrimitiveType.Cylinder, t, hook + new Vector3(0, -.2f, 0), new Vector3(.11f, .03f, .11f), flap);   // the drawn neck
                return;
            }
            float wide = kind == 0 ? .22f : kind == 1 ? .44f : .32f, tall = kind == 0 ? .2f : kind == 1 ? .18f : .3f;
            Part(PrimitiveType.Cube, t, hook + new Vector3(0, -.18f - tall / 2, 0), new Vector3(wide, tall, .1f), leather);
            Part(PrimitiveType.Cube, t, hook + new Vector3(0, -.18f - tall * .24f, -.055f), new Vector3(wide + .02f, tall * .5f, .02f), flap);
            Part(PrimitiveType.Cube, t, hook + new Vector3(0, -.18f - tall * .5f, -.07f), new Vector3(.045f, .05f, .012f), brass);   // the buckle
        }
        /// <summary>
        /// The leatherworker's shop (GAME-ONLY): plank walls on a stone sill on three sides under a thatched gable, the front open.
        /// A doorway on the left, a counter on the right under a hide pentice with the four trade bags hung over it and a
        /// lantern behind; inside, a rail of belts and a bridle on the back wall, a cutting bench with a hide across it, a
        /// stitching horse, a shelf of rolled hides and a stack of them; a satchel on the hanging sign at the corner. The walls,
        /// the counter and the bench are solid; the leatherworker stands behind the counter, at the bench or at the stitching.
        /// </summary>
        void LeatherShop(Transform t)
        {
            float w = LeatherShopSize.x, d = LeatherShopSize.y; const float sill = .4f, wallH = 2.3f, top = sill + wallH, roofH = 2.4f, counterX = .9f;
            var boards = Tint(art.timber, new Color(.46f, .33f, .21f)); var dark = Tint(art.timber, new Color(.26f, .17f, .11f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            float drop = FootDrop(t, w + .3f, d + .3f);
            // Stone sill under the three walls (the side sills stop where the back one starts), plank walls on it, battens over the
            // plank joints outside, a post at each corner and a plank floor.
            BoxPart(t, new Vector3(0, (sill - drop) / 2, d / 2 - .15f), new Vector3(w + .3f, sill + drop, .5f), Masonry, null, 1.5f);
            foreach (int sx in new[] { -1, 1 }) BoxPart(t, new Vector3(sx * (w / 2 - .1f), (sill - drop) / 2, -.275f), new Vector3(.5f, sill + drop, d - .25f), Masonry, null, 1.5f);
            BoxPart(t, new Vector3(0, sill + wallH / 2, d / 2 - .1f), new Vector3(w, wallH, .2f), boards);
            foreach (int sx in new[] { -1, 1 }) BoxPart(t, new Vector3(sx * (w / 2 - .1f), sill + wallH / 2, -.1f), new Vector3(.2f, wallH, d - .2f), boards);
            for (float x = -w / 2 + .6f; x < w / 2; x += 1.2f) Part(PrimitiveType.Cube, t, new Vector3(x, sill + wallH / 2, d / 2 + .03f), new Vector3(.1f, wallH, .06f), dark);
            foreach (int sx in new[] { -1, 1 }) for (float z = -d / 2 + .65f; z < d / 2; z += 1.2f) Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 + .03f), sill + wallH / 2, z), new Vector3(.06f, wallH, .1f), dark);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * w / 2, (top - drop) / 2, sz * d / 2), new Vector3(.26f, top + drop, .26f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(0, .02f, -.1f), new Vector3(w - .4f, .05f, d - .2f), Tint(art.timber, new Color(.36f, .27f, .19f)));
            // The open front: a post either side of the counter, a deep lintel over all (the eaves' rafter ends die into it), the
            // counter itself and a low plank wall from it to the corner.
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(counterX + s * 1.4f, top / 2, -d / 2), new Vector3(.2f, top, .2f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(0, top - .2f, -d / 2), new Vector3(w, .4f, .22f), art.timber);
            BoxPart(t, new Vector3(counterX, .5f, -d / 2 + .3f), new Vector3(2.6f, 1, .6f), Tint(art.timber, new Color(.45f, .32f, .2f)), null, 1);
            Part(PrimitiveType.Cube, t, new Vector3(counterX, 1.03f, -d / 2 + .3f), new Vector3(2.6f, .08f, .8f), Tint(art.timber, new Color(.35f, .25f, .16f)));
            BoxPart(t, new Vector3(counterX + 1.735f, .5f, -d / 2), new Vector3(.47f, 1, .16f), boards);   // from the post to the corner post
            // A boarded ceiling on three joists under the thatch, and the lantern that hangs from it over the counter's inner edge.
            Part(PrimitiveType.Cube, t, new Vector3(0, top - .3f, 0), new Vector3(w - .4f, .04f, d - .3f), dark);
            foreach (float x in new[] { -1.7f, 0, 1.7f }) Part(PrimitiveType.Cube, t, new Vector3(x, top - .38f, 0), new Vector3(.12f, .12f, d - .3f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(counterX, top - .42f, -1.95f), new Vector3(.015f, .24f, .015f), iron);
            MeshPart(PropMesh("Lantern roof", () => ZoneMeshes.Cone(.27f, .2f, 4)), t, new Vector3(counterX, top - .66f, -1.95f), iron, Quaternion.Euler(0, 45, 0)).transform.localScale = Vector3.one * .6f;
            Part(PrimitiveType.Cube, t, new Vector3(counterX, top - .76f, -1.95f), new Vector3(.16f, .2f, .16f), art.glass);
            Glow(t, new Vector3(counterX, top - .8f, -1.8f), 5, .8f, new Color(1, .72f, .42f), 1.6f);
            // The roof: thatch, its eaves and boarded gables as a house has them.
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH, .25f, .6f), t, new Vector3(0, top, 0), art.thatch);
            Eaves(t, w, d, top, roofH, art.thatch);
            Gables(t, w / 2, d, top, roofH, d / 2 + .7f, w / 2 + .6f, boards, 2, .2f);
            // A pentice over the counter: two stitched hides on poles from the posts, a metre out, braced back to them. It passes
            // under the eave's fascia and stays above head height.
            foreach (int s in new[] { -1, 1 })
            {
                float x = counterX + s * 1.4f;
                Rod(t, new Vector3(x, 2.2f, -d / 2 - .1f), new Vector3(x, 2, -d / 2 - 1.1f), .07f, dark);
                Rod(t, new Vector3(x, 1.72f, -d / 2 - .1f), new Vector3(x, 2.08f, -d / 2 - .7f), .05f, dark);
                Part(PrimitiveType.Cube, t, new Vector3(counterX + s * .73f, 2.15f, -d / 2 - .6f), new Vector3(1.46f, .03f, 1.02f), HideOf(s > 0 ? 0 : 2), Quaternion.Euler(-11.3f, 0, 0));
            }
            Rod(t, new Vector3(counterX - 1.55f, 2, -d / 2 - 1.1f), new Vector3(counterX + 1.55f, 2, -d / 2 - 1.1f), .06f, dark);
            for (int i = 0; i < 5; i++) Part(PrimitiveType.Cube, t, new Vector3(counterX, 2.168f - (i - 2) * .044f, -d / 2 - .6f - (i - 2) * .22f), new Vector3(.03f, .012f, .1f), HideOf(3), Quaternion.Euler(-11.3f, 0, 0));   // the stitches down the seam
            // The four trade bags, hung from the lintel over the counter.
            for (int i = 0; i < 4; i++) HungBag(t, new Vector3(counterX - .95f + i * .64f, top - .4f, -d / 2), i);
            // On the counter: a folded hide, a coiled belt, a purse and the tally board.
            Part(PrimitiveType.Cube, t, new Vector3(counterX - .8f, 1.1f, -d / 2 + .32f), new Vector3(.5f, .05f, .4f), HideOf(0), Quaternion.Euler(0, 8, 0));
            Part(PrimitiveType.Cube, t, new Vector3(counterX - .78f, 1.15f, -d / 2 + .3f), new Vector3(.44f, .05f, .36f), HideOf(1), Quaternion.Euler(0, -5, 0));
            MeshPart(PropMesh("Coiled belt", () => Turned(new[] { new Vector2(.07f, 0), new Vector2(.11f, 0), new Vector2(.11f, .045f), new Vector2(.07f, .045f), new Vector2(.07f, 0) }, 12)), t, new Vector3(counterX + .05f, 1.07f, -d / 2 + .3f), HideOf(3));
            Part(PrimitiveType.Sphere, t, new Vector3(counterX + .6f, 1.13f, -d / 2 + .36f), new Vector3(.15f, .13f, .15f), HideOf(4));
            Part(PrimitiveType.Cylinder, t, new Vector3(counterX + .6f, 1.2f, -d / 2 + .36f), new Vector3(.06f, .02f, .06f), HideOf(3));
            Part(PrimitiveType.Cube, t, new Vector3(counterX + 1, 1.08f, -d / 2 + .34f), new Vector3(.2f, .02f, .3f), boards, Quaternion.Euler(0, -12, 0));
            // The back wall: a rail on two pegs hung with six belts, and a bridle (headstall, noseband, bit and reins).
            Part(PrimitiveType.Cube, t, new Vector3(-.3f, 2, d / 2 - .3f), new Vector3(3.4f, .06f, .06f), dark);
            foreach (float x in new[] { -1.9f, 1.3f }) Part(PrimitiveType.Cube, t, new Vector3(x, 2, d / 2 - .25f), new Vector3(.06f, .06f, .12f), dark);
            for (int i = 0; i < 6; i++)
            {
                float x = -1.75f + i * .27f, len = .7f + (i * 5 % 3) * .13f;
                Part(PrimitiveType.Cube, t, new Vector3(x, 1.97f - len / 2, d / 2 - .34f), new Vector3(.05f, len, .012f), HideOf(i + (i % 2) * 2));
                Part(PrimitiveType.Cube, t, new Vector3(x, 1.95f, d / 2 - .35f), new Vector3(.075f, .06f, .02f), Tint(art.metal, new Color(.72f, .58f, .28f)));
            }
            var loop = PropMesh("Strap loop", () => Turned(new[] { new Vector2(.15f, -.012f), new Vector2(.18f, -.012f), new Vector2(.18f, .012f), new Vector2(.15f, .012f), new Vector2(.15f, -.012f) }, 14));
            MeshPart(loop, t, new Vector3(.55f, 1.74f, d / 2 - .34f), HideOf(3), Quaternion.Euler(90, 0, 0));
            MeshPart(loop, t, new Vector3(.55f, 1.46f, d / 2 - .34f), HideOf(3), Quaternion.Euler(90, 0, 0)).transform.localScale = new Vector3(.62f, 1, .62f);
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(.55f + s * .1f, 1.56f, d / 2 - .34f), new Vector3(.02f, .16f, .012f), HideOf(3));     // cheek straps
                Part(PrimitiveType.Cube, t, new Vector3(.55f + s * .14f, 1.02f, d / 2 - .34f), new Vector3(.02f, .86f, .012f), HideOf(1));    // reins
            }
            Rod(t, new Vector3(.42f, 1.46f, d / 2 - .35f), new Vector3(.68f, 1.46f, d / 2 - .35f), .02f, iron);   // the bit
            // The cutting bench under the rail: a hide laid across it and hanging over the front, a round knife and a mallet.
            Part(PrimitiveType.Cube, t, new Vector3(-1.6f, .86f, 1.75f), new Vector3(1.6f, .08f, .8f), art.timber);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(-1.6f + sx * .72f, .41f, 1.75f + sz * .32f), new Vector3(.08f, .82f, .08f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(-1.6f, .3f, 1.75f), new Vector3(1.44f, .06f, .06f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(-1.55f, .91f, 1.72f), new Vector3(1.2f, .02f, .7f), HideOf(2), Quaternion.Euler(0, 4, 0));
            Part(PrimitiveType.Cube, t, new Vector3(-1.55f, .74f, 1.33f), new Vector3(1.05f, .36f, .02f), HideOf(2));
            Part(PrimitiveType.Cylinder, t, new Vector3(-1.2f, .93f, 1.7f), new Vector3(.2f, .005f, .2f), art.metal);
            Rod(t, new Vector3(-1.2f, .94f, 1.7f), new Vector3(-1.06f, .94f, 1.84f), .03f, dark);
            Part(PrimitiveType.Cylinder, t, new Vector3(-2, .96f, 1.6f), new Vector3(.1f, .07f, .1f), art.timber, Quaternion.Euler(90, 30, 0));
            Rod(t, new Vector3(-2, .96f, 1.6f), new Vector3(-1.82f, .94f, 1.42f), .03f, dark);
            // The stitching horse by the right wall: a plank seat on splayed legs, two jaws holding a strap.
            Part(PrimitiveType.Cube, t, new Vector3(2, .5f, .8f), new Vector3(.3f, .05f, 1), art.timber);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(2 + sx * .14f, .24f, .8f + sz * .4f), new Vector3(.05f, .5f, .05f), dark, Quaternion.Euler(sz * -8, 0, sx * 8));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(2 + s * .03f, .78f, .42f), new Vector3(.03f, .56f, .12f), art.timber, Quaternion.Euler(0, 0, s * -2));
            Part(PrimitiveType.Cube, t, new Vector3(2, 1.1f, .42f), new Vector3(.012f, .18f, .2f), HideOf(1));
            // A shelf of rolled hides on the right wall, more standing in the corner, and a stack of flat ones by them.
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 - .38f, 1.6f, .9f), new Vector3(.36f, .04f, 1.7f), dark);
            foreach (float z in new[] { .2f, 1.6f }) Part(PrimitiveType.Cube, t, new Vector3(w / 2 - .34f, 1.48f, z), new Vector3(.26f, .05f, .05f), dark, Quaternion.Euler(0, 0, 35));
            for (int i = 0; i < 3; i++) Part(PrimitiveType.Cylinder, t, new Vector3(w / 2 - .38f, i == 2 ? 1.87f : 1.71f, i == 2 ? .9f : .5f + i * .8f), new Vector3(.18f, .32f, .18f), HideOf(i), Quaternion.Euler(90, 0, 0));
            for (int i = 0; i < 3; i++) Part(PrimitiveType.Cylinder, t, new Vector3(2.5f - (i % 2) * .22f, .5f + i * .04f, 2 - i * .2f), new Vector3(.2f, .48f + i * .04f, .2f), HideOf(i + 2), Quaternion.Euler(i * 3 - 3, 0, 4 - i * 4));
            for (int i = 0; i < 4; i++) Part(PrimitiveType.Cube, t, new Vector3(.9f + (i % 2) * .08f, .08f + i * .07f, 1.8f), new Vector3(1, .07f, .7f), HideOf(i), Quaternion.Euler(0, i * 13 - 6, 0));
            // The hanging sign at the doorway corner, edge on to the wall so it reads from up and down the road: a satchel.
            Part(PrimitiveType.Cube, t, new Vector3(-w / 2, 2.2f, -d / 2 - .7f), new Vector3(.1f, .1f, 1.4f), art.timber);
            Bar(t, new Vector3(-w / 2, 1.75f, -d / 2 - .1f), new Vector3(-w / 2, 2.16f, -d / 2 - .55f), .08f, .08f, art.timber);
            foreach (float z in new[] { -.8f, -1.2f }) Part(PrimitiveType.Cube, t, new Vector3(-w / 2, 2.08f, -d / 2 + z), new Vector3(.02f, .16f, .02f), iron);
            Part(PrimitiveType.Cube, t, new Vector3(-w / 2, 1.75f, -d / 2 - 1), new Vector3(.06f, .5f, .62f), Tint(art.timber, new Color(.5f, .36f, .2f)));
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(-w / 2 + s * .035f, 1.7f, -d / 2 - 1), new Vector3(.02f, .22f, .3f), HideOf(1));
                Part(PrimitiveType.Cube, t, new Vector3(-w / 2 + s * .045f, 1.77f, -d / 2 - 1), new Vector3(.02f, .1f, .32f), HideOf(2));
                Part(PrimitiveType.Cube, t, new Vector3(-w / 2 + s * .035f, 1.88f, -d / 2 - 1), new Vector3(.02f, .03f, .22f), HideOf(3));
            }
            Solid(t, new Vector3(0, top / 2, d / 2 - .1f), new Vector3(w, top, .2f));
            foreach (int sx in new[] { -1, 1 }) Solid(t, new Vector3(sx * (w / 2 - .1f), top / 2, -.1f), new Vector3(.2f, top, d - .2f));
            Solid(t, new Vector3(counterX + .24f, .55f, -d / 2 + .3f), new Vector3(3.5f, 1.1f, .8f));   // the counter, its posts and the low wall
            Solid(t, new Vector3(-1.6f, .45f, 1.75f), new Vector3(1.6f, .9f, .8f));
            // Behind the counter twice over: she keeps shop more than she cuts or stitches.
            for (int i = 0; i < 2; i++) Workplace(t, "leathershop", new Vector3(counterX, 0, -1.2f), new Vector3(counterX, 1.1f, -3.2f));
            Workplace(t, "leathershop", new Vector3(-1.6f, 0, .6f), new Vector3(-1.6f, .9f, 1.75f));
            Workplace(t, "leathershop", new Vector3(1.25f, 0, .55f), new Vector3(2, .9f, .45f));
        }

        // ---------- the herbalist's drying hut ----------
        /// <summary>A wattle panel in a wall: woven withies between upright stakes that stand a little proud of both faces, with
        /// three bands of thicker weavers. <paramref name="size"/>: x or z its length (the other its thickness), y its height.</summary>
        void Wattle(Transform t, Vector3 centre, Vector3 size, Material weave, Material stake)
        {
            BoxPart(t, centre, size, weave, null, 1);
            bool alongX = size.x > size.z; float len = alongX ? size.x : size.z, thick = alongX ? size.z : size.x;
            int n = Mathf.Max(2, Mathf.RoundToInt(len / .45f));
            for (int i = 0; i <= n; i++)
            {
                float a = -len / 2 + len * i / n;
                Part(PrimitiveType.Cylinder, t, centre + (alongX ? new Vector3(a, 0, 0) : new Vector3(0, 0, a)), new Vector3(thick + .04f, size.y / 2, thick + .04f), stake);
            }
            foreach (float f in new[] { -.3f, 0, .3f })
                Part(PrimitiveType.Cube, t, centre + new Vector3(0, size.y * f, 0), alongX ? new Vector3(len, .06f, thick + .025f) : new Vector3(thick + .025f, .06f, len), stake);
        }
        /// <summary>A drying rack: two A-frames, a top rail and a rail down each side, hung with bunches of herbs.</summary>
        void DryingRack(Transform t, Vector3 at, float yaw, int first)
        {
            var r = new GameObject("Drying rack").transform; r.SetParent(t, false); r.localPosition = at; r.localRotation = Quaternion.Euler(0, yaw, 0);
            var pole = Tint(art.timber, new Color(.42f, .31f, .2f));
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Rod(r, new Vector3(sx * .8f, -.05f, sz * .42f), new Vector3(sx * .8f, 1.6f, sz * -.02f), .06f, pole);
            Rod(r, new Vector3(-.98f, 1.52f, 0), new Vector3(.98f, 1.52f, 0), .05f, pole);
            foreach (int sz in new[] { -1, 1 })
            {
                Rod(r, new Vector3(-.95f, 1, sz * .17f), new Vector3(.95f, 1, sz * .17f), .04f, pole);
                Rod(r, new Vector3(-.9f, .4f, sz * .33f), new Vector3(.9f, .4f, sz * .33f), .04f, pole);   // the spreader that keeps the legs apart
                for (int i = 0; i < 4; i++) HerbBundle(r, new Vector3(-.54f + i * .36f, .98f, sz * .17f), first + i + (sz > 0 ? 2 : 0), .9f);
            }
            for (int i = 0; i < 5; i++) HerbBundle(r, new Vector3(-.68f + i * .34f, 1.5f, 0), first + i * 2 + 1);
        }
        /// <summary>
        /// The herbalist's drying hut (GAME-ONLY): wattle panels in a timber frame on a stone footing under a steep thatch, the
        /// right half of the front open. Bunches of herbs hang under the front eave and from a pole inside; two laden drying
        /// racks stand out in front; inside, a bench along the right wall carries a mortar, jars and a small still over a
        /// glowing pan of coals, with a shelf of jars on the back wall and baskets in the corner. The walls and the bench are
        /// solid; the herbalist stands at the bench or at the racks.
        /// </summary>
        void DryingHut(Transform t)
        {
            float w = DryingHutSize.x, d = DryingHutSize.y; const float foot = .25f, top = 2.6f, roofH = 2.8f, doorX = -.3f;
            var weave = Tint(art.hay, new Color(.52f, .42f, .28f)); var withy = Tint(art.timber, new Color(.34f, .25f, .16f)); var dark = Tint(art.timber, new Color(.26f, .17f, .11f));
            var copper = Tint(art.metal, new Color(.72f, .42f, .24f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            float drop = FootDrop(t, w + .3f, d + .3f), wallH = top - foot, mid = foot + wallH / 2;
            // Stone footing under the walls (each strip stops where the next starts), the frame, and the wattle between its posts.
            BoxPart(t, new Vector3(0, (foot - drop) / 2, d / 2), new Vector3(w + .3f, foot + drop, .3f), Masonry, null, 1.5f);
            foreach (int sx in new[] { -1, 1 }) BoxPart(t, new Vector3(sx * w / 2, (foot - drop) / 2, -.125f), new Vector3(.3f, foot + drop, d - .05f), Masonry, null, 1.5f);
            BoxPart(t, new Vector3((-w / 2 + .15f + doorX) / 2, (foot - drop) / 2, -d / 2), new Vector3(doorX + w / 2 - .15f, foot + drop, .3f), Masonry, null, 1.5f);
            Wattle(t, new Vector3(0, mid, d / 2), new Vector3(w - .2f, wallH, .12f), weave, withy);
            foreach (int sx in new[] { -1, 1 }) Wattle(t, new Vector3(sx * w / 2, mid, 0), new Vector3(.12f, wallH, d - .2f), weave, withy);
            Wattle(t, new Vector3((-w / 2 + doorX) / 2, mid, -d / 2), new Vector3(doorX + w / 2 - .2f, wallH, .12f), weave, withy);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * w / 2, (top - drop) / 2, sz * d / 2), new Vector3(.22f, top + drop, .22f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(doorX, top / 2, -d / 2), new Vector3(.2f, top, .2f), art.timber);
            foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(0, top - .2f, sz * d / 2), new Vector3(w, .4f, .2f), art.timber);   // deep plates front and back: the rafter ends die into them
            foreach (int sx in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * w / 2, top - .1f, 0), new Vector3(.2f, .2f, d), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(0, .02f, 0), new Vector3(w - .2f, .05f, d - .2f), Tint(art.hay, new Color(.6f, .52f, .36f)));   // rushes on the floor
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH, .25f, .6f), t, new Vector3(0, top, 0), art.thatch);
            Eaves(t, w, d, top, roofH, art.thatch);
            Gables(t, w / 2, d, top, roofH, d / 2 + .7f, w / 2 + .6f, weave, 1, .18f);
            // Herbs: four bunches from a batten tied under the front rafter ends over the closed panel (clear of the corner and
            // door posts), five on a pole under the ceiling inside.
            Rod(t, new Vector3(-2.35f, top - .38f, -d / 2 - .22f), new Vector3(-.45f, top - .38f, -d / 2 - .22f), .05f, withy);
            for (int i = 0; i < 4; i++) HerbBundle(t, new Vector3(-2.05f + i * .48f, top - .4f, -d / 2 - .22f), i);   // hung from the batten; the heads clear the wattle's stakes
            Rod(t, new Vector3(-w / 2 + .1f, top - .32f, .4f), new Vector3(w / 2 - .1f, top - .32f, .4f), .05f, withy);
            for (int i = 0; i < 5; i++) HerbBundle(t, new Vector3(-1.7f + i * .8f, top - .34f, .4f), i + 2);
            // The bench along the right wall, from the back: a mortar and pestle, three jars, and the still at the doorway end
            // (a copper pot on a pan of coals, its arm running down to a flask).
            float bx = w / 2 - .42f;
            Part(PrimitiveType.Cube, t, new Vector3(bx, .9f, .5f), new Vector3(.6f, .07f, 2.2f), art.timber);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(bx + sx * .24f, .43f, .5f + sz * 1.02f), new Vector3(.08f, .86f, .08f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(bx, .35f, .5f), new Vector3(.5f, .04f, 2.04f), dark);
            MeshPart(PropMesh("Mortar", () => Turned(new[] { new Vector2(0, 0), new Vector2(.08f, 0), new Vector2(.13f, .12f), new Vector2(.13f, .15f), new Vector2(.1f, .15f), new Vector2(.06f, .05f), new Vector2(0, .05f) }, 10)), t, new Vector3(bx, .935f, 1.3f), Tint(art.stone, new Color(.62f, .6f, .56f)));
            Rod(t, new Vector3(bx, 1, 1.3f), new Vector3(bx - .1f, 1.16f, 1.22f), .035f, Tint(art.stone, new Color(.7f, .68f, .62f)));
            Jar(t, new Vector3(bx + .05f, .935f, .85f), new Color(.62f, .4f, .28f));
            Jar(t, new Vector3(bx - .08f, .935f, .6f), new Color(.5f, .46f, .36f), .85f);
            Jar(t, new Vector3(bx + .06f, .935f, .36f), new Color(.36f, .44f, .4f), 1.15f);
            Part(PrimitiveType.Cylinder, t, new Vector3(bx, .975f, -.25f), new Vector3(.3f, .04f, .3f), iron);                 // the pan of coals
            Part(PrimitiveType.Cylinder, t, new Vector3(bx, 1.02f, -.25f), new Vector3(.24f, .008f, .24f), art.glass);
            foreach (int k in new[] { 0, 1, 2 }) Rod(t, new Vector3(bx + Mathf.Cos(k * 2.094f) * .12f, .99f, -.25f + Mathf.Sin(k * 2.094f) * .12f), new Vector3(bx + Mathf.Cos(k * 2.094f) * .1f, 1.1f, -.25f + Mathf.Sin(k * 2.094f) * .1f), .02f, iron);   // the trivet
            MeshPart(PropMesh("Still", () => Turned(new[] { new Vector2(0, 0), new Vector2(.09f, 0), new Vector2(.13f, .08f), new Vector2(.12f, .16f), new Vector2(.05f, .24f), new Vector2(.07f, .3f), new Vector2(.03f, .36f), new Vector2(0, .37f) }, 10)), t, new Vector3(bx, 1.1f, -.25f), copper);
            Rod(t, new Vector3(bx, 1.42f, -.25f), new Vector3(bx - .04f, 1.12f, -.62f), .025f, copper);
            Jar(t, new Vector3(bx - .04f, .935f, -.66f), new Color(.4f, .5f, .46f), .8f);
            Glow(t, new Vector3(bx - .25f, 1.15f, -.25f), 3.5f, .9f, new Color(1, .6f, .3f), 1.5f);
            // A shelf of jars on the back wall; a stool, two baskets of cut herbs and a sack in the back corner.
            Part(PrimitiveType.Cube, t, new Vector3(-.7f, 1.45f, d / 2 - .2f), new Vector3(2.2f, .04f, .24f), dark);
            foreach (float x in new[] { -1.6f, .2f }) Part(PrimitiveType.Cube, t, new Vector3(x, 1.36f, d / 2 - .17f), new Vector3(.05f, .05f, .2f), dark, Quaternion.Euler(35, 0, 0));
            for (int i = 0; i < 6; i++) Jar(t, new Vector3(-1.6f + i * .36f, 1.47f, d / 2 - .2f), i % 3 == 0 ? new Color(.62f, .4f, .28f) : i % 3 == 1 ? new Color(.5f, .46f, .36f) : new Color(.36f, .44f, .4f), .8f + (i * 7 % 3) * .12f);
            var basket = PropMesh("Herb basket", () => Turned(new[] { new Vector2(0, 0), new Vector2(.2f, 0), new Vector2(.27f, .26f), new Vector2(.24f, .26f), new Vector2(.18f, .04f), new Vector2(0, .04f) }, 12, .3f));
            for (int i = 0; i < 2; i++)
            {
                var at = new Vector3(-1.9f + i * .62f, .045f, 1.7f - i * .12f);
                MeshPart(basket, t, at, art.hay);
                Part(PrimitiveType.Sphere, t, at + new Vector3(0, .22f, 0), new Vector3(.44f, .2f, .44f), Tint(art.foliage, i == 0 ? new Color(.36f, .5f, .24f) : new Color(.5f, .54f, .3f)));
            }
            Part(PrimitiveType.Sphere, t, new Vector3(-2, .3f, .9f), new Vector3(.5f, .62f, .46f), Tint(art.cloth, new Color(.7f, .63f, .48f)));
            Part(PrimitiveType.Cylinder, t, new Vector3(-2, .62f, .9f), new Vector3(.15f, .05f, .15f), Tint(art.cloth, new Color(.7f, .63f, .48f)));
            Part(PrimitiveType.Cylinder, t, new Vector3(.2f, .4f, 1.6f), new Vector3(.36f, .03f, .36f), art.timber);
            foreach (int k in new[] { 0, 1, 2 }) Rod(t, new Vector3(.2f + Mathf.Cos(k * 2.094f) * .16f, 0, 1.6f + Mathf.Sin(k * 2.094f) * .16f), new Vector3(.2f + Mathf.Cos(k * 2.094f) * .1f, .4f, 1.6f + Mathf.Sin(k * 2.094f) * .1f), .04f, dark);
            // Two drying racks out in front of the closed panel, in the sun.
            DryingRack(t, new Vector3(-1.45f, LocalGround(t, -1.45f, -d / 2 - 1), -d / 2 - 1), 4, 0);
            DryingRack(t, new Vector3(-3.7f, LocalGround(t, -3.7f, -d / 2 - .7f), -d / 2 - .7f), -14, 3);
            Solid(t, new Vector3(0, top / 2, d / 2), new Vector3(w, top, .2f));
            foreach (int sx in new[] { -1, 1 }) Solid(t, new Vector3(sx * w / 2, top / 2, 0), new Vector3(.2f, top, d));
            Solid(t, new Vector3((-w / 2 + doorX) / 2, top / 2, -d / 2), new Vector3(doorX + w / 2 + .2f, top, .2f));
            Solid(t, new Vector3(bx, .45f, .5f), new Vector3(.6f, .9f, 2.2f));
            Workplace(t, "dryhut", new Vector3(bx - .95f, 0, .5f), new Vector3(bx, 1, .5f));
            Workplace(t, "dryhut", new Vector3(-1.45f, 0, -d / 2 - 2), new Vector3(-1.45f, 1.1f, -d / 2 - 1));
        }

        // ---------- the inn's kitchen ----------
        /// <summary>
        /// The inn's kitchen (GAME-ONLY): a lean-to on the inn's back wall (the prop's +z edge meets that wall; see
        /// <see cref="KitchenBehind"/>). A slate roof falls from the wall to a beam on a post; the far end is a stone wall with
        /// the range against it, a hooded chimney up through the roof, a pot on a crane and a pan on the plate; along the inn's
        /// wall a stack of firewood, a work table under a shelf of crocks, and the back door into the taproom; two hares hang
        /// from the beam and a water butt stands by the post. The end wall, range, table, post and butt are solid. The cook
        /// stands at the range or the table; goods are handed over at three spots out in front ("kitchendoor").
        /// </summary>
        void Kitchen(Transform t)
        {
            float w = KitchenSize.x, d = KitchenSize.y; const float wallTop = 2.5f, slope = .2794f, pitch = 15.6f;   // the roof drops .95 m over 3.4 m
            var dark = Tint(art.timber, new Color(.26f, .17f, .11f)); var boards = Tint(art.timber, new Color(.4f, .28f, .18f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            float Under(float z) { return 2.863f + (z + .2f) * slope; }   // the roof slab's underside over local z
            float drop = FootDrop(t, w + .3f, d + .3f);
            BoxPart(t, new Vector3(.2f, .02f, .05f), new Vector3(w - .5f, .05f, d - .2f), Tint(Masonry, new Color(.62f, .6f, .56f)), null, 1.2f);   // flagged floor
            // The stone end wall, boarded from its top up to the roof; the roof on a ledger along the inn's wall, five rafters
            // and a beam carried by the end wall and one post.
            BoxPart(t, new Vector3(-w / 2 + .175f, (wallTop - drop) / 2, .05f), new Vector3(.35f, wallTop + drop, d - .1f), Masonry, null, 1.5f);
            MeshPart(PropMesh("Kitchen gable", () => Cutout(new[] { new Vector2(-1.45f, wallTop), new Vector2(1.5f, wallTop), new Vector2(1.5f, 3.34f), new Vector2(-1.45f, 2.52f) }, .12f)), t, new Vector3(-w / 2 + .175f, 0, 0), boards, Quaternion.Euler(0, -90, 0));
            BoxPart(t, new Vector3(.1f, 2.925f, -.2f), new Vector3(w + .8f, .12f, 3.53f), art.slate, Quaternion.Euler(-pitch, 0, 0), 2.5f);
            Part(PrimitiveType.Cube, t, new Vector3(.1f, 3.3f, d / 2 - .06f), new Vector3(w + .6f, .14f, .12f), dark);
            foreach (float x in new[] { -1.6f, -.6f, .4f, 1.4f, 2.35f }) Bar(t, new Vector3(x, Under(1.42f) - .06f, 1.42f), new Vector3(x, Under(-1.7f) - .06f, -1.7f), .12f, .08f, dark);
            Part(PrimitiveType.Cube, t, new Vector3(.1f, 2.38f, -1.4f), new Vector3(w - .1f, .16f, .16f), art.timber);
            Stake(t, new Vector3(2.35f, -.1f - drop, -1.4f), .2f, 2.44f + drop, art.timber);
            Bar(t, new Vector3(2.35f, 1.8f, -1.4f), new Vector3(1.87f, 2.3f, -1.4f), .08f, .08f, art.timber);
            // The range: a stone block with its fire mouth to the room, an iron plate, the hood and the chimney above.
            BoxPart(t, new Vector3(-1.75f, .45f, 0), new Vector3(.8f, .9f, 1.8f), Masonry, null, 1);
            Part(PrimitiveType.Cube, t, new Vector3(-1.34f, .38f, 0), new Vector3(.04f, .42f, .7f), Tint(art.timber, new Color(.08f, .05f, .04f)));
            Part(PrimitiveType.Cube, t, new Vector3(-1.325f, .27f, 0), new Vector3(.04f, .16f, .5f), art.glass);
            Part(PrimitiveType.Cube, t, new Vector3(-1.75f, .92f, 0), new Vector3(.74f, .04f, 1.7f), iron);
            BoxPart(t, new Vector3(-1.95f, 2, -.2f), new Vector3(.4f, 1, 1.3f), Masonry, null, 1);
            BoxPart(t, new Vector3(-2.2f, 3.5f, -.45f), new Vector3(.6f, 2.2f, .7f), Masonry, null, 1);
            BoxPart(t, new Vector3(-2.2f, 4.64f, -.45f), new Vector3(.76f, .12f, .86f), Tint(Masonry, new Color(.45f, .44f, .4f)), null, 1);
            if (art.particle != null) Smoke(t, new Vector3(-2.2f, 4.8f, -.45f));
            Glow(t, new Vector3(-1, .7f, 0), 5, 1, new Color(1, .55f, .25f), 1.8f);
            // The pot on its crane (an iron arm swung out from the end wall), its stew, and a pan on the plate.
            var pot = PropMesh("Cook pot", () => Turned(new[] { new Vector2(0, 0), new Vector2(.15f, 0), new Vector2(.23f, .1f), new Vector2(.23f, .24f), new Vector2(.19f, .3f), new Vector2(.21f, .33f), new Vector2(.17f, .33f), new Vector2(.17f, .27f), new Vector2(0, .27f) }, 10));
            MeshPart(pot, t, new Vector3(-1.75f, .94f, -.4f), iron);
            Part(PrimitiveType.Cylinder, t, new Vector3(-1.75f, 1.225f, -.4f), new Vector3(.34f, .006f, .34f), Tint(art.cloth, new Color(.46f, .3f, .16f)));
            Rod(t, new Vector3(-2.12f, .94f, -.82f), new Vector3(-2.12f, 1.48f, -.82f), .035f, iron);   // the pivot, up from the plate
            Rod(t, new Vector3(-2.12f, 1.45f, -.82f), new Vector3(-1.75f, 1.45f, -.4f), .035f, iron);   // the arm, just under the hood (its bottom at 1.5)
            foreach (int s in new[] { -1, 1 }) Rod(t, new Vector3(-1.75f, 1.45f, -.4f), new Vector3(-1.75f + s * .2f, 1.27f, -.4f), .018f, iron);   // the pot's bail, from the arm's end
            Part(PrimitiveType.Cylinder, t, new Vector3(-1.75f, .96f, .5f), new Vector3(.32f, .02f, .32f), iron);
            Rod(t, new Vector3(-1.62f, .97f, .5f), new Vector3(-1.3f, 1, .62f), .025f, iron);
            // Firewood in the corner between the range and the inn's wall, its cut ends to the room.
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 4 - row; i++)
                    Part(PrimitiveType.Cylinder, t, new Vector3(-2.02f + row * .12f + i * .24f, .12f + row * .21f, 1.2f), new Vector3(.23f, .26f, .23f), art.bark, Quaternion.Euler(90, 0, 0));
            // The work table against the inn's wall: a loaf, a bowl, a cabbage, a knife and a jug; a shelf of crocks over it and
            // a ladle and a skillet on pegs beside.
            Part(PrimitiveType.Cube, t, new Vector3(-.2f, .82f, 1.05f), new Vector3(1.5f, .07f, .75f), art.timber);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(-.2f + sx * .67f, .39f, 1.05f + sz * .3f), new Vector3(.08f, .78f, .08f), dark);
            Part(PrimitiveType.Capsule, t, new Vector3(-.7f, .92f, 1), new Vector3(.2f, .17f, .2f), Tint(art.hay, new Color(.72f, .48f, .22f)), Quaternion.Euler(90, 25, 0));
            MeshPart(PropMesh("Mortar", () => Turned(new[] { new Vector2(0, 0), new Vector2(.08f, 0), new Vector2(.13f, .12f), new Vector2(.13f, .15f), new Vector2(.1f, .15f), new Vector2(.06f, .05f), new Vector2(0, .05f) }, 10)), t, new Vector3(-.25f, .855f, 1.15f), Tint(art.stone, new Color(.62f, .4f, .28f))).transform.localScale = Vector3.one * 1.3f;
            Part(PrimitiveType.Sphere, t, new Vector3(.2f, .95f, 1.1f), Vector3.one * .2f, Tint(art.foliage, new Color(.42f, .6f, .28f)));
            Part(PrimitiveType.Cube, t, new Vector3(-.2f, .87f, .82f), new Vector3(.26f, .01f, .035f), art.metal, Quaternion.Euler(0, 20, 0));
            Jar(t, new Vector3(.42f, .855f, 1.25f), new Color(.5f, .46f, .36f), 1.4f);
            Part(PrimitiveType.Cube, t, new Vector3(-.2f, 1.6f, d / 2 - .16f), new Vector3(1.5f, .04f, .26f), dark);
            foreach (float x in new[] { -.8f, .4f }) Part(PrimitiveType.Cube, t, new Vector3(x, 1.5f, d / 2 - .13f), new Vector3(.05f, .05f, .22f), dark, Quaternion.Euler(35, 0, 0));
            for (int i = 0; i < 4; i++) Jar(t, new Vector3(-.75f + i * .37f, 1.62f, d / 2 - .16f), i % 2 == 0 ? new Color(.62f, .4f, .28f) : new Color(.5f, .46f, .36f), .9f + (i % 3) * .15f);
            Part(PrimitiveType.Cylinder, t, new Vector3(.95f, 1.5f, d / 2 - .06f), new Vector3(.3f, .012f, .3f), iron, Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Cube, t, new Vector3(.95f, 1.76f, d / 2 - .06f), new Vector3(.03f, .24f, .02f), iron);
            Part(PrimitiveType.Cube, t, new Vector3(1.2f, 1.62f, d / 2 - .06f), new Vector3(.025f, .4f, .02f), art.timber);
            Part(PrimitiveType.Sphere, t, new Vector3(1.2f, 1.4f, d / 2 - .07f), new Vector3(.09f, .09f, .05f), art.metal);
            // The back door into the taproom, on the inn's wall, and its step.
            InnerDoor(t, KitchenDoorX, d / 2 - .03f, dark);
            BoxPart(t, new Vector3(KitchenDoorX, .06f, d / 2 - .4f), new Vector3(1.1f, .12f, .5f), Masonry, null, 1);
            // Two hares from the beam, a string of onions down the post, the water butt and its bucket.
            HungHare(t, new Vector3(.9f, 2.3f, -1.4f), 20); HungHare(t, new Vector3(1.25f, 2.3f, -1.4f), -35);
            for (int i = 0; i < 6; i++) Part(PrimitiveType.Sphere, t, new Vector3(2.35f + (i % 2 == 0 ? -.05f : .05f), 2 - i * .09f, -1.53f), Vector3.one * .1f, Tint(art.foliage, new Color(.78f, .6f, .34f)));
            Barrel(t, new Vector3(2.85f, LocalGround(t, 2.85f, -.9f), -.9f), 1.1f, 20);
            Part(PrimitiveType.Cylinder, t, new Vector3(2.75f, LocalGround(t, 2.75f, -1.75f) + .15f, -1.75f), new Vector3(.3f, .15f, .3f), art.timber);
            Part(PrimitiveType.Cylinder, t, new Vector3(2.75f, LocalGround(t, 2.75f, -1.75f) + .28f, -1.75f), new Vector3(.25f, .01f, .25f), art.water);
            Solid(t, new Vector3(-w / 2 + .175f, wallTop / 2, .05f), new Vector3(.35f, wallTop, d - .1f));
            Solid(t, new Vector3(-1.75f, .45f, 0), new Vector3(.8f, .9f, 1.8f));
            Solid(t, new Vector3(-.2f, .42f, 1.05f), new Vector3(1.5f, .85f, .75f));
            Solid(t, new Vector3(2.6f, .6f, -1.15f), new Vector3(.8f, 1.2f, .75f));   // the post and the butt beside it
            Workplace(t, "kitchen", new Vector3(-.75f, 0, -.2f), new Vector3(-1.75f, .95f, -.2f));
            Workplace(t, "kitchen", new Vector3(-.2f, 0, 0), new Vector3(-.2f, .9f, 1.05f));
            foreach (float x in new[] { -1f, 0, 1 }) Workplace(t, "kitchendoor", new Vector3(x, 0, -2.2f), new Vector3(x, 1, 0));
        }

        // ---------- the hunter's game rack ----------
        /// <summary>
        /// The hunter's game rack (GAME-ONLY): two forked poles and a crossbar hung with a hill deer, two hares and a brace of
        /// pheasants; behind it a rail of pelts and a hide laced in a frame; a butcher's block with a cleaver in it and a ring
        /// of cold stones in front. Every foot is set on its own ground (it stands on a hillside). Only the block is solid.
        /// </summary>
        void GameRack(Transform t)
        {
            var pole = Tint(art.timber, new Color(.36f, .27f, .18f)); var dark = Tint(art.timber, new Color(.26f, .18f, .12f)); var cord = Tint(art.hay, new Color(.6f, .54f, .42f));
            float G(float x, float z) { return LocalGround(t, x, z); }
            float bar = 2.6f + Mathf.Max(0, G(-.6f, 0), G(-.6f, .15f));   // the deer's antlers stay clear of the hillside under them
            // The rack: each pole forks at the head and the crossbar lies in the forks.
            foreach (int s in new[] { -1, 1 })
            {
                float g = G(s * 1.5f, 0) - .15f;
                Stake(t, new Vector3(s * 1.5f, g, 0), .14f, bar - .1f - g, pole);
                foreach (int f in new[] { -1, 1 }) Rod(t, new Vector3(s * 1.5f, bar - .2f, 0), new Vector3(s * 1.5f, bar + .16f, f * .12f), .06f, pole);
            }
            Rod(t, new Vector3(-1.75f, bar, 0), new Vector3(1.75f, bar, 0), .09f, pole);
            // The deer, hung by the hind legs: haunches, barrel, the pale belly to the front, neck, head and a young buck's
            // antlers, the forelegs hanging.
            var coat = Tint(art.cloth, new Color(.52f, .37f, .23f)); var pale = Tint(art.cloth, new Color(.86f, .8f, .68f)); const float dx = -.6f;
            foreach (int s in new[] { -1, 1 })
            {
                Rod(t, new Vector3(dx + s * .09f, bar - .04f, 0), new Vector3(dx + s * .13f, bar - .64f, .02f), .07f, coat);
                Rod(t, new Vector3(dx + s * .1f, bar - 1.4f, -.06f), new Vector3(dx + s * .14f, bar - 1.92f, -.14f), .055f, coat);
                Rod(t, new Vector3(dx + s * .05f, bar - 2.1f, .04f), new Vector3(dx + s * .2f, bar - 2.28f, .12f), .025f, Bone);
                Rod(t, new Vector3(dx + s * .13f, bar - 2.2f, .08f), new Vector3(dx + s * .15f, bar - 2.3f, -.02f), .02f, Bone);
            }
            Part(PrimitiveType.Cube, t, new Vector3(dx, bar - .06f, 0), new Vector3(.3f, .03f, .1f), cord);                       // the lashing
            Part(PrimitiveType.Capsule, t, new Vector3(dx, bar - 1.05f, 0), new Vector3(.4f, .52f, .34f), coat);
            Part(PrimitiveType.Sphere, t, new Vector3(dx, bar - 1.05f, -.1f), new Vector3(.26f, .82f, .18f), pale);
            Part(PrimitiveType.Capsule, t, new Vector3(dx, bar - 1.72f, .03f), new Vector3(.17f, .24f, .17f), coat);
            Part(PrimitiveType.Sphere, t, new Vector3(dx, bar - 2.02f, .02f), new Vector3(.17f, .28f, .2f), coat);
            Part(PrimitiveType.Sphere, t, new Vector3(dx, bar - .52f, .15f), new Vector3(.1f, .15f, .08f), pale);                // the scut
            // Two hares and a brace of pheasants along the rest of the bar.
            HungHare(t, new Vector3(.25f, bar - .04f, 0), 15); HungHare(t, new Vector3(.52f, bar - .04f, 0), -30);
            foreach (int s in new[] { -1, 1 })
            {
                var hook = new Vector3(1.05f + s * .07f, bar - .04f, s * .02f);
                Part(PrimitiveType.Cube, t, hook + new Vector3(0, -.05f, 0), new Vector3(.012f, .1f, .012f), cord);
                Part(PrimitiveType.Sphere, t, hook + new Vector3(0, -.13f, 0), Vector3.one * .075f, Tint(art.cloth, new Color(.14f, .3f, .22f)));
                Part(PrimitiveType.Sphere, t, hook + new Vector3(0, -.32f, 0), new Vector3(.14f, .3f, .15f), Tint(art.cloth, s > 0 ? new Color(.56f, .32f, .16f) : new Color(.48f, .36f, .22f)));
                Part(PrimitiveType.Cube, t, hook + new Vector3(0, -.62f, .02f), new Vector3(.035f, .36f, .012f), Tint(art.cloth, new Color(.42f, .3f, .18f)), Quaternion.Euler(4, 0, s * 5));
            }
            // Behind the rack: three pelts over a rail (wolf, fox, coney), and a hide laced into a frame to dry.
            foreach (float x in new[] { -1.75f, -.25f }) { float g = G(x, .9f) - .12f; Stake(t, new Vector3(x, g, .9f), .1f, 1.28f - g, dark); }
            Rod(t, new Vector3(-1.9f, 1.25f, .9f), new Vector3(-.1f, 1.25f, .9f), .06f, pole);
            var pelts = new[] { new Color(.44f, .44f, .46f), new Color(.64f, .37f, .2f), new Color(.72f, .66f, .56f) };
            for (int i = 0; i < 3; i++)
            {
                float x = -1.45f + i * .45f, wide = i == 2 ? .26f : .38f, front = i == 2 ? .34f : .6f; var fur = Tint(art.cloth, pelts[i]);
                Part(PrimitiveType.Cube, t, new Vector3(x, 1.29f, .9f), new Vector3(wide, .025f, .13f), fur);
                Part(PrimitiveType.Cube, t, new Vector3(x, 1.29f - front / 2, .9f - .055f), new Vector3(wide, front, .025f), fur);
                Part(PrimitiveType.Cube, t, new Vector3(x, 1.29f - front * .35f, .9f + .055f), new Vector3(wide, front * .7f, .025f), fur);
            }
            foreach (float x in new[] { .4f, 1.8f }) { float g = G(x, .9f) - .12f; Stake(t, new Vector3(x, g, .9f), .1f, 2.04f - g, dark); }
            foreach (float y in new[] { .4f, 1.9f }) Part(PrimitiveType.Cube, t, new Vector3(1.1f, y, .9f), new Vector3(1.6f, .07f, .07f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(1.1f, 1.15f, .9f), new Vector3(1.15f, 1.25f, .03f), HideOf(0), Quaternion.Euler(0, 0, -4));
            for (int k = 0; k < 4; k++) foreach (float y in new[] { .48f, 1.82f }) Part(PrimitiveType.Cube, t, new Vector3(.65f + k * .3f, y, .9f), new Vector3(.02f, .12f, .02f), cord);
            // The butcher's block, a cleaver left in it.
            float bg = G(1.05f, -.85f);
            MeshPart2(PropMesh("Butcher's block", () => TwoTone(Turned(new[] { new Vector2(.4f, -.2f), new Vector2(.36f, .2f), new Vector2(.35f, .62f) }, 12), Turned(new[] { new Vector2(.35f, .62f), new Vector2(0, .62f) }, 12))), t, new Vector3(1.05f, bg, -.85f), art.bark, Tint(art.timber, new Color(.62f, .5f, .34f)));
            Part(PrimitiveType.Cube, t, new Vector3(1.05f, bg + .7f, -.85f), new Vector3(.22f, .16f, .02f), art.metal, Quaternion.Euler(0, 30, 12));
            Part(PrimitiveType.Cube, t, new Vector3(.9f, bg + .78f, -.76f), new Vector3(.16f, .035f, .035f), dark, Quaternion.Euler(0, 30, 12));
            // The ring of cold stones: ash, two charred ends, seven stones.
            float fx = -1.45f, fz = -1.3f, fg = G(fx, fz); var stone = RockTint(new Color(.5f, .49f, .46f)); var charred = Tint(art.timber, new Color(.07f, .06f, .06f));
            Part(PrimitiveType.Cylinder, t, new Vector3(fx, fg + .012f, fz), new Vector3(.74f, .012f, .74f), Tint(art.stone, new Color(.3f, .29f, .28f)));
            Part(PrimitiveType.Cylinder, t, new Vector3(fx - .05f, fg + .07f, fz), new Vector3(.1f, .26f, .1f), charred, Quaternion.Euler(90, 35, 0));
            Part(PrimitiveType.Cylinder, t, new Vector3(fx + .06f, fg + .08f, fz + .04f), new Vector3(.09f, .22f, .09f), charred, Quaternion.Euler(84, -50, 0));
            for (int k = 0; k < 7; k++)
            {
                float a = k * Mathf.PI * 2 / 7 + .3f; var at = new Vector3(fx + Mathf.Cos(a) * .44f, 0, fz + Mathf.Sin(a) * .44f); at.y = G(at.x, at.z) + .05f;
                Lump(BoulderAt(k), t, at, new Vector3(.3f + (k % 3) * .04f, .2f + (k % 2) * .04f, .26f), stone, k * 53);
            }
            Solid(t, new Vector3(1.05f, bg + .3f, -.85f), new Vector3(.7f, .7f, .7f));
            Workplace(t, "lodge", new Vector3(-.5f, 0, -1.3f), new Vector3(-.6f, 1.3f, 0));
        }
    }
}
