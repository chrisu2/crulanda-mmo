using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// What lives in the Chemical River (Lowtowns pick 6): "Absolutely nothing natural lived in the toxic depths of the Chemical River. Except
    /// things that shouldn't exist. Sludge-eels. Mutated crocodiles." (CANON creatures, book1 ch.15:391). Their bodies are GAME-ONLY, built of
    /// primitives like the spider and the briar (the packs have no eel and no crocodile):
    /// - <see cref="BuildEel"/>: a sludge-eel, a long black body slick as the river, a round sucking mouth, small sickly eyes, a ragged fin
    ///   down its back; its run of body on the four walking pivots, so it humps and rolls through the sludge as it moves.
    /// - <see cref="BuildCroc"/>: a mutated crocodile, long and low on four splayed legs, its hide black-olive and wet, a long jaw of yellowed
    ///   teeth, a row of pale bony growths down its back where the scutes went wrong, one eye larger than the other, and blisters along its
    ///   flanks glowing the river's sick green.
    /// </summary>
    public sealed partial class ActorVisual
    {
        void BuildRiverBeast(ActorLook look)
        {
            if (look == ActorLook.Eel) BuildEel(); else BuildCroc();
        }

        static Material Glows(Color c, Color emission, float smooth)
        {
            var m = Mat(c, smooth); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission); return m;
        }

        /// <summary>A sludge-eel (CANON creature, book1 ch.15:391): about two and a half metres of black, oily eel, low in the water.</summary>
        void BuildEel()
        {
            var skin = Mat(new Color(.06f, .07f, .06f), .88f); var belly = Mat(new Color(.26f, .28f, .2f), .5f); var lip = Mat(new Color(.42f, .32f, .3f), .6f);
            var eye = Glows(new Color(.8f, .75f, .3f), new Color(.6f, .55f, .1f), .9f);
            float y = -.78f; LieDepth = .08f;
            torso = Part(PrimitiveType.Capsule, body, new Vector3(0, y + .12f, .25f), new Vector3(.34f, .42f, .3f), skin, new Vector3(90, 0, 0));   // the fore-body
            head = Part(PrimitiveType.Sphere, body, new Vector3(0, y + .18f, .78f), new Vector3(.3f, .25f, .44f), skin);
            Part(PrimitiveType.Cylinder, head, new Vector3(0, -.12f, .48f), new Vector3(.62f, .08f, .62f), lip, new Vector3(90, 0, 0));   // the round sucking mouth
            Part(PrimitiveType.Cylinder, head, new Vector3(0, -.12f, .5f), new Vector3(.36f, .09f, .36f), Mat(new Color(.03f, .02f, .02f)), new Vector3(90, 0, 0));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Sphere, head, new Vector3(s * .38f, .2f, .22f), Vector3.one * .14f, eye);
            Part(PrimitiveType.Sphere, torso, new Vector3(0, 0, .3f), new Vector3(.7f, .9f, .5f), belly);   // the paler throat under it
            // The rest of the body on the walking pivots, front to tail, each segment a little thinner: it rolls as it goes.
            armL = Pivot("Eel 1", new Vector3(0, y + .1f, -.05f)); armR = Pivot("Eel 2", new Vector3(0, y + .08f, -.55f));
            legL = Pivot("Eel 3", new Vector3(0, y + .06f, -1.02f)); legR = Pivot("Eel 4", new Vector3(0, y + .04f, -1.45f));
            var segs = new[] { armL, armR, legL, legR };
            for (int i = 0; i < segs.Length; i++)
            {
                float w = .3f - i * .05f;
                Part(PrimitiveType.Capsule, segs[i], new Vector3(0, 0, -.25f), new Vector3(w, .32f, w * .9f), skin, new Vector3(90, 0, 0));
                Part(PrimitiveType.Cube, segs[i], new Vector3(0, w * .55f, -.25f), new Vector3(.025f, .12f, .45f), belly, new Vector3(0, 0, (i % 2 == 0 ? 6 : -6)));   // the ragged fin
            }
            Part(PrimitiveType.Cube, legR, new Vector3(0, 0, -.62f), new Vector3(.03f, .22f, .3f), belly);   // the tail's flat end
            cloth = skin; beast = true;
        }

        /// <summary>A mutated crocodile (CANON creature, book1 ch.15:391): about four metres of it, low and long on splayed legs.</summary>
        void BuildCroc()
        {
            var hide = Mat(new Color(.09f, .11f, .07f), .72f); var belly = Mat(new Color(.36f, .36f, .26f), .45f); var bone = Mat(new Color(.62f, .6f, .5f), .35f);
            var teeth = Mat(new Color(.78f, .72f, .52f), .5f); var eye = Glows(new Color(.7f, .9f, .3f), new Color(.45f, .8f, .1f), .9f);
            var blister = Glows(new Color(.35f, .7f, .25f), new Color(.2f, .7f, .15f) * .9f, .8f);
            float hipY = -.72f, len = 1.9f; LieDepth = .18f;
            torso = Part(PrimitiveType.Capsule, body, new Vector3(0, hipY + .1f, 0), new Vector3(.95f, len * .62f, .55f), hide, new Vector3(90, 0, 0));
            Part(PrimitiveType.Capsule, body, new Vector3(0, hipY - .02f, 0), new Vector3(.8f, len * .55f, .4f), belly, new Vector3(90, 0, 0));
            // The head and the long jaw, the teeth along both sides of it.
            head = Part(PrimitiveType.Sphere, body, new Vector3(0, hipY + .12f, len * .62f), new Vector3(.6f, .4f, .62f), hide);
            Part(PrimitiveType.Cube, head, new Vector3(0, -.15f, 1.05f), new Vector3(.62f, .42f, 1.5f), hide);   // the snout
            Part(PrimitiveType.Cube, head, new Vector3(0, -.48f, .95f), new Vector3(.58f, .18f, 1.4f), belly);   // the jaw
            for (int k = 0; k < 6; k++)
                foreach (int s in new[] { -1, 1 })
                    Part(PrimitiveType.Cube, head, new Vector3(s * .3f, -.36f, .45f + k * .22f), new Vector3(.05f, .14f, .05f), teeth, new Vector3(0, 0, s * 10));
            Part(PrimitiveType.Sphere, head, new Vector3(-.3f, .3f, .2f), Vector3.one * .2f, eye);
            Part(PrimitiveType.Sphere, head, new Vector3(.3f, .32f, .22f), Vector3.one * .32f, eye);   // the other eye, grown too big
            // The tail: three tapering lengths, fixed (it drags).
            for (int k = 0; k < 3; k++)
            {
                float w = .5f - k * .14f;
                Part(PrimitiveType.Capsule, body, new Vector3(0, hipY + .05f - k * .04f, -len * .6f - k * .62f), new Vector3(w, .42f, w * .7f), hide, new Vector3(90, 0, 0));
            }
            // The scutes gone wrong: pale bony growths down the back, some doubled, and blisters along the flanks glowing the river's green.
            for (int k = 0; k < 9; k++)
            {
                float z = len * .45f - k * .42f, h = .12f + (k % 3) * .06f;
                Part(PrimitiveType.Cube, body, new Vector3((k % 2 == 0 ? .08f : -.08f), hipY + .38f - (k > 5 ? .1f : 0), z), new Vector3(.12f, h, .16f), bone, new Vector3(0, 0, k % 2 == 0 ? 12 : -12));
                if (k % 3 == 1) Part(PrimitiveType.Sphere, body, new Vector3((k % 2 == 0 ? .44f : -.44f), hipY + .12f, z), Vector3.one * .14f, blister);
            }
            // Four short legs splayed out to the sides, on the walking pivots.
            float w0 = .42f, front = len * .38f, hind = -len * .34f;
            armL = Pivot("Leg FL", new Vector3(-w0, hipY, front)); armR = Pivot("Leg FR", new Vector3(w0, hipY, front));
            legL = Pivot("Leg HL", new Vector3(-w0, hipY, hind)); legR = Pivot("Leg HR", new Vector3(w0, hipY, hind));
            foreach (var (leg, s) in new[] { (armL, -1), (armR, 1), (legL, -1), (legR, 1) })
            {
                Part(PrimitiveType.Capsule, leg, new Vector3(s * .16f, -.12f, 0), new Vector3(.16f, .18f, .16f), hide, new Vector3(0, 0, s * 55));
                Part(PrimitiveType.Sphere, leg, new Vector3(s * .3f, -.26f, .06f), new Vector3(.2f, .06f, .26f), hide);   // the clawed foot, flat
            }
            cloth = hide; beast = true;
        }
    }
}
