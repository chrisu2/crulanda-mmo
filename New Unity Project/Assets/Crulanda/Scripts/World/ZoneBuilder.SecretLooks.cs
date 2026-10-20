using System;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The looks of the hidden finds Phase 8.6 adds (step 8, 2026-10-15; GAME-ONLY), built by <see cref="BuildSecrets"/> like the rest:
    /// small, no colliders, from the find's own stream, its frame facing -Z the way you come to it. Each builds what it stands on
    /// (its own stone, post, lectern or shrine), so it needs nothing round it and is moved clear of trunks and rocks like a herb.
    /// - carving: a standing slab of the zone's stone (or a weathered post, when its prompt or name says post, trunk, stump, tree or
    ///   driftwood) with marks cut in its face: tally notches ("tally", "count", "notch", "marks"), heights ticked up one edge
    ///   ("height"), or rows of cut letters; in a cave, the marks on the rock wall itself;
    /// - read: a wooden lectern with an open book; a cairn with a book wrapped on its top under a slate ("cairn"); or slates leaned
    ///   on a stone ("slate", "tablet");
    /// - grave: a headstone with a pale name-panel, a low mound before it, pebbles on its top;
    /// - remains: bones in the grass (they stay) and what you take (the find): a satchel with a page out of it and a dented cap;
    /// - shrine: a wayshrine: a post with a little roofed niche and its figure, a ledge with a dish, a crown in it and a candle stub
    ///   burning low.
    /// No glint on any of them (decision 6, 2026-10-15: no chime, no glint): they are found by looking, and by what people say.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The finds that build what they stand on, and how much clear ground each wants round it.</summary>
        static bool StandsAlone(string kind) { return kind == "carving" || kind == "read" || kind == "grave" || kind == "remains" || kind == "shrine"; }
        static float AloneClear(string kind) { return kind == "remains" ? .9f : kind == "grave" ? .75f : kind == "carving" ? .6f : .5f; }
        Material CutMark { get { return Tint(art.stone, new Color(.09f, .085f, .08f)); } }

        /// <summary>A carving: marks cut in a standing stone, a post or (in a cave) the wall behind it.</summary>
        void SecretCarving(Transform item, Func<float> R, string words, bool onWall)
        {
            bool Says(params string[] any) { foreach (var w in any) if (words.Contains(w)) return true; return false; }
            string marks = Says("tally", "count", "notch", "marks") ? "tally" : Says("height") ? "heights" : "letters";
            var face = new GameObject("Carved face").transform; face.SetParent(item, false);
            float faceZ, bottom, top, width;
            if (onWall)
            {
                // The wall is the frame's z = 0 plane (the rock behind, +Z): the marks are cut into it at chest height.
                faceZ = .01f; bottom = .7f; top = 1.6f; width = .5f;
            }
            else if (Says("post", "trunk", "stump", "tree", "driftwood"))
            {
                // A weathered post (or a snapped trunk), grey and split, leaning a little; the marks on its near side.
                var wood = Tint(art.timber, Zone.biome == "ash" || Gloom ? new Color(.46f, .44f, .41f) : new Color(.42f, .36f, .28f));
                face.localPosition = new Vector3(0, LocalGround(item, 0, .18f) - .05f, .18f); face.localRotation = Quaternion.Euler((R() - .5f) * 6, (R() - .5f) * 20, (R() - .5f) * 6);
                Part(PrimitiveType.Cylinder, face, new Vector3(0, .62f, 0), new Vector3(.34f, .62f, .34f), wood);
                Part(PrimitiveType.Cube, face, new Vector3(.05f, 1.27f, 0), new Vector3(.1f, .14f, .08f), wood, Quaternion.Euler(0, 30, 12));   // the split top
                Part(PrimitiveType.Cube, face, new Vector3(-.06f, 1.24f, .03f), new Vector3(.08f, .1f, .07f), wood, Quaternion.Euler(0, -20, -9));
                faceZ = -.17f; bottom = .35f; top = 1.05f; width = .2f;
            }
            else
            {
                // A standing slab of the zone's stone, taller than it is wide, leaning a little, its top worn round.
                var stone = SecretStone(1.08f);
                face.localPosition = new Vector3(0, LocalGround(item, 0, .14f) - .06f, .14f); face.localRotation = Quaternion.Euler((R() - .5f) * 7, (R() - .5f) * 24, (R() - .5f) * 6);
                Part(PrimitiveType.Cube, face, new Vector3(0, .55f, 0), new Vector3(.52f, 1.1f, .22f), stone);
                Part(PrimitiveType.Cylinder, face, new Vector3(0, 1.1f, 0), new Vector3(.52f, .11f, .52f), stone, Quaternion.Euler(90, 0, 0));
                faceZ = -.112f; bottom = .3f; top = 1f; width = .38f;
            }
            var cut = CutMark;
            if (marks == "tally")
            {
                // Rows of five: four strokes and the fifth across them, the last row short.
                int rows = 3 + (int)(R() * 2), last = 1 + (int)(R() * 4);
                for (int r = 0; r < rows; r++)
                {
                    float y = top - .08f - r * .16f; int n = r == rows - 1 ? last : 5;
                    for (int k = 0; k < Mathf.Min(n, 4); k++) Part(PrimitiveType.Cube, face, new Vector3(-width / 2 + .05f + k * .045f, y, faceZ), new Vector3(.012f, .1f, .008f), cut);
                    if (n == 5) Part(PrimitiveType.Cube, face, new Vector3(-width / 2 + .115f, y, faceZ - .002f), new Vector3(.2f, .012f, .008f), cut, Quaternion.Euler(0, 0, 28));
                }
            }
            else if (marks == "heights")
            {
                // A tick up one edge for every year someone stood against it, and a letter or two beside each; they stop.
                int n = 6 + (int)(R() * 4);
                for (int k = 0; k < n; k++)
                {
                    float y = bottom + .1f + k * (top - bottom - .2f) / n + (R() - .5f) * .03f;
                    Part(PrimitiveType.Cube, face, new Vector3(-width / 2 + .07f, y, faceZ), new Vector3(.1f, .01f, .008f), cut);
                    Part(PrimitiveType.Cube, face, new Vector3(-width / 2 + .15f, y + .005f, faceZ), new Vector3(.025f, .03f, .008f), cut);
                }
            }
            else
            {
                // Rows of cut letters: short strokes at odd angles, the rows a little uneven.
                for (int r = 0; r < 4; r++)
                {
                    float y = top - .1f - r * .13f; float x = -width / 2 + .04f;
                    while (x < width / 2 - .04f)
                    {
                        Part(PrimitiveType.Cube, face, new Vector3(x, y + (R() - .5f) * .01f, faceZ), new Vector3(.012f, .06f, .008f), cut, Quaternion.Euler(0, 0, (R() - .5f) * 50));
                        x += .03f + R() * .03f; if (R() < .18f) x += .04f;   // a space between words
                    }
                }
            }
        }

        /// <summary>Something to read: a lectern and its open book, a cairn with a wrapped book on top, or slates leaned on a stone.</summary>
        void SecretRead(Transform item, Func<float> R, string words)
        {
            bool Says(params string[] any) { foreach (var w in any) if (words.Contains(w)) return true; return false; }
            var leather = Tint(art.cloth, Wither(new Color(.32f, .2f, .12f))); var paper = Tint(art.plaster, new Color(.9f, .86f, .72f));
            if (Says("cairn"))
            {
                // Four stones stacked broad to small, the top one flat: on it, a book in oilcloth tied with cord, a slate laid over it.
                var stone = SecretStone(); float at = LocalGround(item, 0, .1f) - .03f; float[] w = { .7f, .6f, .48f, .5f }, h = { .3f, .26f, .22f, .1f };
                for (int k = 0; k < 4; k++) { Lump(BoulderAt((int)(R() * 6)), item, new Vector3((R() - .5f) * .05f, at + .18f * h[k], .1f + (R() - .5f) * .05f), new Vector3(w[k], h[k], w[k] * .9f), stone, R() * 360); at += .58f * h[k]; }
                var book = new GameObject("Book").transform; book.SetParent(item, false); book.localPosition = new Vector3(0, at + .02f, .1f); book.localRotation = Quaternion.Euler(0, (R() - .5f) * 40, 0);
                Part(PrimitiveType.Cube, book, new Vector3(0, .03f, 0), new Vector3(.24f, .06f, .3f), Tint(art.cloth, Wither(new Color(.27f, .26f, .2f))));
                Part(PrimitiveType.Cube, book, new Vector3(0, .031f, 0), new Vector3(.25f, .064f, .015f), Tint(art.cloth, new Color(.55f, .46f, .3f)));   // the cord
                Part(PrimitiveType.Cube, book, new Vector3(.02f, .072f, .02f), new Vector3(.3f, .02f, .26f), SecretStone(.7f), Quaternion.Euler(2, 12, -3));   // the slate over it
                return;
            }
            if (Says("slate", "tablet"))
            {
                // A low stone, and three slates leaned against its near side, chalked; a fourth flat at its foot.
                var stone = SecretStone(.95f);
                Lump(BoulderAt((int)(R() * 6)), item, new Vector3(0, LocalGround(item, 0, .3f) + .08f, .3f), new Vector3(.8f, .45f, .5f), stone, R() * 360);
                var slate = Tint(art.stone, new Color(.2f, .21f, .23f)); var chalk = Tint(art.plaster, new Color(.86f, .86f, .82f));
                for (int k = 0; k < 4; k++)
                {
                    var sl = new GameObject("Slate").transform; sl.SetParent(item, false);
                    sl.localPosition = k < 3 ? new Vector3(-.22f + k * .22f, LocalGround(item, -.22f + k * .22f, .08f) + .17f, .08f) : new Vector3(.05f, LocalGround(item, .05f, -.22f) + .012f, -.22f);
                    sl.localRotation = k < 3 ? Quaternion.Euler(-16 - R() * 8, (R() - .5f) * 16, (R() - .5f) * 6) : Quaternion.Euler(90, R() * 40, 0);
                    Part(PrimitiveType.Cube, sl, Vector3.zero, new Vector3(.2f, .3f, .018f), slate);
                    for (int r = 0; r < 4; r++) Part(PrimitiveType.Cube, sl, new Vector3((R() - .5f) * .02f, .09f - r * .055f, -.011f), new Vector3(.12f + R() * .04f, .008f, .004f), chalk);
                }
                return;
            }
            // A lectern of weathered boards: a foot, a post and a sloped top, and the book lying open on it with its ribbon hanging.
            var wood = Tint(art.timber, Wither(new Color(.4f, .3f, .2f)));
            var stand = new GameObject("Lectern").transform; stand.SetParent(item, false); stand.localPosition = new Vector3(0, LocalGround(item, 0, .25f) - .04f, .25f); stand.localRotation = Quaternion.Euler(0, (R() - .5f) * 10, 0);
            Part(PrimitiveType.Cube, stand, new Vector3(0, .04f, 0), new Vector3(.42f, .08f, .1f), wood);
            Part(PrimitiveType.Cube, stand, new Vector3(0, .04f, 0), new Vector3(.1f, .08f, .36f), wood);
            Part(PrimitiveType.Cube, stand, new Vector3(0, .5f, 0), new Vector3(.09f, .9f, .09f), wood);
            var top = new GameObject("Top").transform; top.SetParent(stand, false); top.localPosition = new Vector3(0, .98f, 0); top.localRotation = Quaternion.Euler(-28, 0, 0);
            Part(PrimitiveType.Cube, top, Vector3.zero, new Vector3(.5f, .03f, .36f), wood);
            Part(PrimitiveType.Cube, top, new Vector3(0, .02f, -.17f), new Vector3(.5f, .03f, .025f), wood);   // the lip the book rests on
            Part(PrimitiveType.Cube, top, new Vector3(0, .022f, 0), new Vector3(.44f, .012f, .32f), leather);   // the cover
            foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, top, new Vector3(side * .105f, .04f, 0), new Vector3(.2f, .026f, .3f), paper, Quaternion.Euler(0, 0, -side * 5));
            Part(PrimitiveType.Cube, top, new Vector3(.01f, .03f, -.2f), new Vector3(.015f, .004f, .12f), Tint(art.cloth, new Color(.55f, .12f, .1f)), Quaternion.Euler(70, 0, 0));   // the ribbon
        }

        /// <summary>A grave: a headstone with a pale panel cut with a name, a low mound before it, pebbles left on its top.</summary>
        void SecretGrave(Transform item, Func<float> R)
        {
            var stone = SecretStone(1.12f); var panel = SecretStone(1.45f); var cut = CutMark;
            bool ash = Zone.biome == "ash";
            var turf = ash ? Tint(art.ash, new Color(.46f, .45f, .44f)) : Tint(art.soil, Wither(new Color(.25f, .3f, .17f)));
            var head = new GameObject("Headstone").transform; head.SetParent(item, false);
            head.localPosition = new Vector3(0, LocalGround(item, 0, .75f) - .08f, .75f); head.localRotation = Quaternion.Euler(-4 - R() * 6, (R() - .5f) * 14, (R() - .5f) * 8);
            Part(PrimitiveType.Cube, head, new Vector3(0, .38f, 0), new Vector3(.52f, .76f, .13f), stone);
            Part(PrimitiveType.Cylinder, head, new Vector3(0, .76f, 0), new Vector3(.52f, .065f, .52f), stone, Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Cube, head, new Vector3(0, .5f, -.068f), new Vector3(.36f, .34f, .006f), panel);
            for (int r = 0; r < 3; r++) Part(PrimitiveType.Cube, head, new Vector3((R() - .5f) * .03f, .6f - r * .085f, -.072f), new Vector3(r == 0 ? .26f : .18f + R() * .08f, .016f, .004f), cut);
            for (int k = 0; k < 3; k++) Part(PrimitiveType.Sphere, head, new Vector3(-.12f + k * .11f + (R() - .5f) * .03f, .81f, (R() - .5f) * .04f), new Vector3(.05f, .035f, .045f), SecretStone(.8f + R() * .4f));   // pebbles left by visitors
            Part(PrimitiveType.Sphere, item, new Vector3(0, LocalGround(item, 0, 0) - .06f, -.05f), new Vector3(.7f, .22f, 1.45f), turf);   // the mound
            if (!ash && !Gloom)
                for (int k = 0; k < 4; k++) Part(PrimitiveType.Sphere, item, new Vector3((R() - .5f) * .4f, LocalGround(item, 0, 0) + .05f, -.4f + R() * .6f), Vector3.one * .03f, Tint(art.foliage, new Color(.9f, .86f, .5f)));
        }

        /// <summary>A dead adventurer: bones in the grass (they stay) and, as the find, the satchel by them with a page out of its flap and a
        /// dented cap beside it.</summary>
        void SecretRemains(Transform item, Transform t, Func<float> R)
        {
            var bone = Bone; float g = LocalGround(t, 0, .5f);
            var lie = Quaternion.Euler(0, 70 + (R() - .5f) * 50, 0);   // the body lies across the way, its head to one side
            Vector3 At(float x, float z) { var p = new Vector3(0, 0, .5f) + lie * new Vector3(x, 0, z); p.y = LocalGround(t, p.x, p.z); return p; }
            Part(PrimitiveType.Sphere, t, At(0, .7f) + Vector3.up * .08f, new Vector3(.17f, .15f, .2f), bone, lie);   // the skull
            Part(PrimitiveType.Cube, t, At(0, .63f) + Vector3.up * .03f, new Vector3(.12f, .04f, .07f), bone, lie * Quaternion.Euler(20, 0, 0));   // its jaw, fallen
            for (int k = 0; k < 9; k++) Part(PrimitiveType.Cube, t, At(0, .45f - k * .07f) + Vector3.up * .025f, new Vector3(.04f, .035f, .045f), bone, lie);   // the spine
            for (int k = 0; k < 5; k++) foreach (int side in new[] { -1, 1 })
                Part(PrimitiveType.Cylinder, t, At(side * .12f, .38f - k * .07f) + Vector3.up * .05f, new Vector3(.018f, .13f, .018f), bone, lie * Quaternion.Euler(0, 0, side * (60 + k * 4)));   // ribs, fallen in
            foreach (int side in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cylinder, t, At(side * .1f, -.42f) + Vector3.up * .03f, new Vector3(.03f, .2f, .03f), bone, lie * Quaternion.Euler(90, side * (8 + R() * 10), 0));   // thigh bones
                Part(PrimitiveType.Cylinder, t, At(side * (.14f + R() * .1f), -.8f) + Vector3.up * .025f, new Vector3(.025f, .18f, .025f), bone, lie * Quaternion.Euler(90, side * (14 + R() * 20), 0));
                Part(PrimitiveType.Cylinder, t, At(side * .3f, .2f + R() * .1f) + Vector3.up * .025f, new Vector3(.022f, .15f, .022f), bone, lie * Quaternion.Euler(90, side * (40 + R() * 30), 0));   // an arm
            }
            // The find: a satchel against the bones, its flap open and a folded page standing out of it; a dented cap beside it.
            var bag = new GameObject("Satchel").transform; bag.SetParent(item, false); bag.localPosition = new Vector3(0, LocalGround(item, 0, 0), 0); bag.localRotation = Quaternion.Euler(0, (R() - .5f) * 40, (R() - .5f) * 10);
            var hide = Tint(art.cloth, Wither(new Color(.33f, .22f, .13f)));
            Part(PrimitiveType.Cube, bag, new Vector3(0, .1f, 0), new Vector3(.3f, .2f, .12f), hide);
            Part(PrimitiveType.Cube, bag, new Vector3(0, .17f, -.08f), new Vector3(.3f, .02f, .12f), hide, Quaternion.Euler(-35, 0, 0));   // the flap, thrown open
            Part(PrimitiveType.Cube, bag, new Vector3(.03f, .23f, .01f), new Vector3(.12f, .1f, .006f), Tint(art.plaster, new Color(.88f, .84f, .7f)), Quaternion.Euler(-8, 0, 6));   // the page
            Part(PrimitiveType.Cube, bag, new Vector3(-.17f, .14f, 0), new Vector3(.02f, .22f, .02f), hide);   // the strap
            Part(PrimitiveType.Cube, bag, new Vector3(0, .08f, -.062f), new Vector3(.035f, .03f, .006f), Tint(art.metal, new Color(.6f, .47f, .22f)));   // the buckle
            var cap = new GameObject("Cap").transform; cap.SetParent(item, false); cap.localPosition = new Vector3(.38f, LocalGround(item, .38f, .05f) - .03f, .05f); cap.localRotation = Quaternion.Euler(25 + R() * 30, R() * 360, 0);
            var iron = Tint(art.metal, new Color(.3f, .27f, .24f));
            Part(PrimitiveType.Sphere, cap, new Vector3(0, .07f, 0), new Vector3(.24f, .17f, .26f), iron);
            Part(PrimitiveType.Cylinder, cap, new Vector3(0, .02f, 0), new Vector3(.26f, .01f, .28f), iron);
            Part(PrimitiveType.Sphere, cap, new Vector3(.08f, .12f, -.05f), new Vector3(.06f, .03f, .06f), Tint(art.metal, new Color(.36f, .2f, .12f)));   // rust where it took the blow
        }

        /// <summary>A wayshrine: a post, a little roofed niche with its carved figure, a ledge before it with a dish (a crown in it) and a
        /// candle stub burning low. Moss on the roof in the green zones, bone-pale wood in the ash.</summary>
        void SecretShrine(Transform item, Func<float> R)
        {
            bool ash = Zone.biome == "ash";
            var wood = Tint(art.timber, ash ? new Color(.62f, .6f, .56f) : Wither(new Color(.38f, .28f, .18f)));
            var dark = Tint(art.stone, new Color(.06f, .055f, .05f));
            var s = new GameObject("Wayshrine").transform; s.SetParent(item, false);
            s.localPosition = new Vector3(0, LocalGround(item, 0, .22f) - .05f, .22f); s.localRotation = Quaternion.Euler((R() - .5f) * 3, (R() - .5f) * 12, (R() - .5f) * 3);
            Part(PrimitiveType.Cube, s, new Vector3(0, .66f, 0), new Vector3(.13f, 1.32f, .13f), wood);   // the post
            Part(PrimitiveType.Cube, s, new Vector3(0, 1.48f, 0), new Vector3(.36f, .34f, .26f), wood);   // the niche
            Part(PrimitiveType.Cube, s, new Vector3(0, 1.47f, -.128f), new Vector3(.26f, .25f, .01f), dark);
            Part(PrimitiveType.Capsule, s, new Vector3(0, 1.46f, -.11f), new Vector3(.065f, .085f, .05f), Bone);   // the figure in it
            var roof = ash ? wood : Tint(art.thatch != null ? art.thatch : art.timber, Wither(new Color(.36f, .3f, .22f)));
            foreach (int side in new[] { -1, 1 }) Part(PrimitiveType.Cube, s, new Vector3(side * .12f, 1.72f, 0), new Vector3(.3f, .03f, .36f), roof, Quaternion.Euler(0, 0, -side * 36));
            if (!ash && (Zone.biome == "meadow" || Zone.life != null && Zone.life.mood == "keepers"))
                Part(PrimitiveType.Cube, s, new Vector3(.08f, 1.76f, .03f), new Vector3(.16f, .02f, .22f), Tint(art.foliage, new Color(.22f, .36f, .16f)), Quaternion.Euler(0, 0, -36));   // moss
            Part(PrimitiveType.Cube, s, new Vector3(0, .92f, -.12f), new Vector3(.32f, .03f, .18f), wood);   // the ledge
            Part(PrimitiveType.Cylinder, s, new Vector3(-.04f, .945f, -.13f), new Vector3(.16f, .012f, .16f), Tint(art.soil, new Color(.42f, .3f, .2f)));   // the dish
            var coin = Part(PrimitiveType.Cylinder, s, new Vector3(-.03f, .953f, -.13f), new Vector3(.034f, .003f, .034f), Tint(art.metal, new Color(.7f, .55f, .25f)));
            Part(PrimitiveType.Cylinder, s, new Vector3(.1f, .965f, -.12f), new Vector3(.03f, .03f, .03f), Tint(art.plaster, new Color(.9f, .86f, .74f)));   // the candle stub
            Part(PrimitiveType.Sphere, s, new Vector3(.1f, 1.01f, -.12f), new Vector3(.018f, .03f, .018f), Glowing(new Color(1, .76f, .4f), .7f)).name = "Flame";
            coin.name = "Crown";
        }
    }
}
