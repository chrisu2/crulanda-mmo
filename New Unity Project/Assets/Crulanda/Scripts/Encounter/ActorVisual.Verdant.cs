using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Verdant Shore's creatures and people (ActorVisual): a Veridian Keeper (CANON-EXPANDED: Book 3's native wood-beings),
    /// a great forest stag, a forest spider and a bramble-thing (GAME-ONLY). Primitives, like the rest; the walk cycle moves the
    /// same pivots.
    /// </summary>
    public sealed partial class ActorVisual
    {
        /// <summary>
        /// A Veridian Keeper (CANON-EXPANDED: "native wood-beings" with "wooden limbs" that glow "with a desperate, emerald fire" in
        /// the siege, Book 3 ch.11; Oak-Bane and Willow-Whisper are named): about 2.6 m, a figure of bark over a heartwood core,
        /// long-limbed, a crown of leafy twigs for hair, moss on the shoulders, two deep knots for eyes with a faint emerald light in
        /// them, and sap-light in the seams of the bark. Withered (variant odd, a corrupted Keeper): grey bark, dead twigs, the
        /// light gone violet. Builds on the humanoid frame, so it walks and fights as the others do.
        /// </summary>
        void BuildKeeper()
        {
            bool withered = variant % 2 == 1;
            var bark = Mat(withered ? new Color(.36f, .34f, .3f) : new Color(.33f, .24f, .16f), .1f);
            var heart = Mat(withered ? new Color(.26f, .22f, .22f) : new Color(.48f, .34f, .2f), .2f);
            var moss = Mat(withered ? new Color(.3f, .3f, .26f) : new Color(.26f, .4f, .17f), .05f);
            var leaf = Mat(withered ? new Color(.4f, .36f, .28f) : new Color(.3f, .55f, .2f), .05f);
            var sap = Mat(withered ? new Color(.5f, .2f, .7f) : new Color(.25f, .95f, .55f), .8f);
            sap.EnableKeyword("_EMISSION"); sap.SetColor("_EmissionColor", (withered ? new Color(.5f, .15f, .8f) : new Color(.2f, .9f, .45f)) * 1.8f);
            cloth = bark; accent = leaf; stoop = 4;
            // Everything at its true height (feet at -1): a tall figure, so nothing is scaled.
            float scale = 1.3f;
            Transform P(PrimitiveType type, Transform parent, Vector3 pos, Vector3 size, Material m, Vector3? euler = null)
            { return Part(type, parent, pos * scale, size * scale, m, euler); }
            legL = Pivot("Leg L", new Vector3(-.16f, -.08f, 0) * scale); legR = Pivot("Leg R", new Vector3(.16f, -.08f, 0) * scale);
            foreach (var (leg, s) in new[] { (legL, -1), (legR, 1) })
            {
                P(PrimitiveType.Capsule, leg, new Vector3(0, -.36f, 0), new Vector3(.17f, .4f, .17f), bark);                      // a limb of bark
                P(PrimitiveType.Cube, leg, new Vector3(0, -.38f, .07f), new Vector3(.05f, .5f, .04f), sap);                       // sap in the seam
                P(PrimitiveType.Capsule, leg, new Vector3(s * .02f, -.72f, .04f), new Vector3(.15f, .22f, .22f), heart);           // a rooty foot
                for (int k = 0; k < 3; k++) P(PrimitiveType.Capsule, leg, new Vector3((k - 1) * .07f, -.9f, .12f + (k == 1 ? .08f : 0)), new Vector3(.04f, .12f, .04f), bark, new Vector3(70, (k - 1) * 20, 0));   // root toes
            }
            torso = Pivot("Torso", new Vector3(0, .3f, 0) * scale);
            P(PrimitiveType.Capsule, torso, new Vector3(0, .05f, 0), new Vector3(.4f, .4f, .32f), bark);                          // the trunk of the body
            P(PrimitiveType.Cube, torso, new Vector3(0, .08f, .14f), new Vector3(.07f, .6f, .05f), sap);                           // the sap seam down the chest
            foreach (int s in new[] { -1, 1 }) P(PrimitiveType.Cube, torso, new Vector3(s * .1f, .14f, .13f), new Vector3(.05f, .3f, .04f), sap, new Vector3(0, 0, s * 18));
            P(PrimitiveType.Sphere, torso, new Vector3(0, .32f, 0), new Vector3(.52f, .2f, .36f), moss);                           // moss over the shoulders
            foreach (int s in new[] { -1, 1 }) P(PrimitiveType.Sphere, torso, new Vector3(s * .26f, .3f, 0), new Vector3(.2f, .18f, .22f), bark);
            P(PrimitiveType.Cylinder, torso, new Vector3(0, .42f, 0), new Vector3(.14f, .08f, .14f), heart);                      // neck
            head = P(PrimitiveType.Capsule, torso, new Vector3(0, .62f, .02f), new Vector3(.3f, .2f, .3f), bark);                  // a long, narrow head
            foreach (int s in new[] { -1, 1 })
            {
                P(PrimitiveType.Sphere, torso, new Vector3(s * .07f, .66f, .12f), new Vector3(.07f, .05f, .04f), heart);         // the knots
                P(PrimitiveType.Sphere, torso, new Vector3(s * .07f, .66f, .14f), new Vector3(.035f, .025f, .02f), sap);           // lit deep in them
            }
            // A crown of twigs, leafed (or bare and grey when withered), out of the top of the head every which way.
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40 + 11, tilt = 20 + (i * 37 % 40);
                var twig = P(PrimitiveType.Cylinder, torso, new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * .08f, .8f, Mathf.Sin(a * Mathf.Deg2Rad) * .08f - .02f), new Vector3(.025f, .14f, .025f), heart, new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * tilt, 0, -Mathf.Cos(a * Mathf.Deg2Rad) * tilt));
                if (!withered) P(PrimitiveType.Sphere, twig, new Vector3(0, 1, 0), new Vector3(3.2f, .5f, 2.2f), leaf, new Vector3(0, a, 0));
            }
            armL = Pivot("Arm L", new Vector3(-.3f, .62f, 0) * scale); armR = Pivot("Arm R", new Vector3(.3f, .62f, 0) * scale);
            foreach (var (arm, s) in new[] { (armL, -1), (armR, 1) })
            {
                P(PrimitiveType.Capsule, arm, new Vector3(s * .02f, -.38f, 0), new Vector3(.13f, .42f, .13f), bark, new Vector3(0, 0, s * 3));   // long arms, to the knee
                P(PrimitiveType.Cube, arm, new Vector3(s * .06f, -.4f, 0), new Vector3(.03f, .55f, .035f), sap, new Vector3(0, 0, s * 3));
                var hand = P(PrimitiveType.Sphere, arm, new Vector3(s * .04f, -.84f, 0), new Vector3(.13f, .1f, .13f), heart);
                for (int k = 0; k < 4; k++) P(PrimitiveType.Capsule, arm, new Vector3(s * .04f + (k - 1.5f) * .035f, -.95f, .02f), new Vector3(.025f, .09f, .025f), bark, new Vector3(10, 0, (k - 1.5f) * 12));   // twig fingers
            }
        }

        /// <summary>
        /// A great forest stag (GAME-ONLY): taller than a wolf at the shoulder, a deep russet coat with a pale belly and throat, a
        /// long neck, a narrow head with a dark muzzle and ears, and a wide crown of branching antlers. A doe (variant odd) has
        /// no antlers and a lighter coat. On the beast frame: the walk moves diagonal pairs.
        /// </summary>
        void BuildStag()
        {
            bool doe = variant % 2 == 1;
            var coat = Mat(doe ? new Color(.52f, .4f, .28f) : new Color(.45f, .3f, .18f)); var pale = Mat(doe ? new Color(.7f, .62f, .5f) : new Color(.66f, .56f, .42f));
            var dark = Mat(new Color(.18f, .13f, .1f)); var horn = Mat(new Color(.5f, .44f, .36f), .3f); var eye = Mat(new Color(.08f, .06f, .05f), .85f);
            float hipY = -.1f, legLen = .9f, len = 1.15f;
            LieDepth = .55f;
            torso = Part(PrimitiveType.Capsule, body, new Vector3(0, hipY, 0), new Vector3(.4f, len * .55f, .5f), coat, new Vector3(90, 0, 0));
            Part(PrimitiveType.Capsule, body, new Vector3(0, hipY - .15f, .05f), new Vector3(.3f, len * .4f, .3f), pale, new Vector3(90, 0, 0));   // the pale belly
            Part(PrimitiveType.Sphere, body, new Vector3(0, hipY + .08f, len * .36f), new Vector3(.42f, .44f, .4f), coat);                       // the shoulders
            Part(PrimitiveType.Sphere, body, new Vector3(0, hipY + .05f, -len * .34f), new Vector3(.4f, .4f, .36f), coat);                       // the haunches
            float w = .15f, front = len * .4f, hind = -len * .36f;
            armL = Pivot("Leg FL", new Vector3(-w, hipY - .05f, front)); armR = Pivot("Leg FR", new Vector3(w, hipY - .05f, front));
            legL = Pivot("Leg HL", new Vector3(-w, hipY - .05f, hind)); legR = Pivot("Leg HR", new Vector3(w, hipY - .05f, hind));
            foreach (var leg in new[] { armL, armR, legL, legR })
            {
                Part(PrimitiveType.Capsule, leg, new Vector3(0, -legLen * .28f, 0), new Vector3(.11f, legLen * .3f, .12f), coat);
                Part(PrimitiveType.Capsule, leg, new Vector3(0, -legLen * .72f, .02f), new Vector3(.07f, legLen * .27f, .07f), dark);
                Part(PrimitiveType.Cube, leg, new Vector3(0, -legLen, .03f), new Vector3(.08f, .07f, .1f), dark);   // the hoof
            }
            var neckBase = new Vector3(0, hipY + .22f, len * .5f);
            Part(PrimitiveType.Capsule, body, neckBase + new Vector3(0, .3f, .12f), new Vector3(.17f, .36f, .2f), coat, new Vector3(-30, 0, 0));   // the neck, rising
            Part(PrimitiveType.Capsule, body, neckBase + new Vector3(0, .26f, .18f), new Vector3(.11f, .3f, .12f), pale, new Vector3(-30, 0, 0));  // the pale throat
            head = Part(PrimitiveType.Sphere, body, neckBase + new Vector3(0, .66f, .32f), new Vector3(.2f, .22f, .26f), coat);
            Part(PrimitiveType.Cube, head, new Vector3(0, -.2f, .85f), new Vector3(.55f, .5f, .9f), coat);                        // the muzzle
            Part(PrimitiveType.Sphere, head, new Vector3(0, -.15f, 1.3f), Vector3.one * .25f, dark);                               // the nose
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Sphere, head, new Vector3(s * .38f, .25f, .25f), Vector3.one * .16f, eye);
                Part(PrimitiveType.Capsule, head, new Vector3(s * .45f, .7f, -.15f), new Vector3(.22f, .4f, .12f), coat, new Vector3(-20, 0, s * -40));   // the ears
                if (doe) continue;
                // The antlers: a main beam curving up and back, with three tines each.
                var beam = Part(PrimitiveType.Cylinder, head, new Vector3(s * .3f, .9f, -.1f), new Vector3(.12f, .75f, .12f), horn, new Vector3(-25, 0, s * -28));
                foreach (var (y, tilt, l) in new[] { (.35f, 40f, .45f), (.95f, 55f, .55f), (1.5f, 30f, .5f) })
                    Part(PrimitiveType.Cylinder, beam, new Vector3(0, y, 0), new Vector3(.6f, l, .6f), horn, new Vector3(-tilt, 0, s * (tilt - 10)));
                Part(PrimitiveType.Cylinder, beam, new Vector3(0, 1.9f, 0), new Vector3(.5f, .4f, .5f), horn, new Vector3(-50, 0, s * 20));   // the top fork
            }
            Part(PrimitiveType.Capsule, body, new Vector3(0, hipY + .18f, -len * .56f), new Vector3(.07f, .14f, .07f), pale, new Vector3(-40, 0, 0));   // the tail
            cloth = coat; beast = true;
        }

        /// <summary>
        /// A forest spider (GAME-ONLY): a fat dark abdomen patterned pale, a smaller fore-body, eight jointed legs (four pairs:
        /// the walk cycle swings the pivots' two front and two hind pairs, the middle pairs ride still), eight small eyes that
        /// catch the light, and fangs. Low to the ground. A glowing one (variant odd) has a faint green belly-light, as of the
        /// Veridian sap it fed on.
        /// </summary>
        void BuildSpider()
        {
            bool lit = variant % 2 == 1;
            var chitin = Mat(new Color(.12f, .1f, .09f), .45f); var pattern = Mat(new Color(.5f, .42f, .26f), .3f); var fang = Mat(new Color(.25f, .2f, .16f), .6f);
            var eye = Mat(new Color(.6f, .5f, .3f), .95f); eye.EnableKeyword("_EMISSION"); eye.SetColor("_EmissionColor", new Color(.4f, .3f, .15f));
            float hipY = -.62f;
            LieDepth = .2f;
            torso = Part(PrimitiveType.Sphere, body, new Vector3(0, hipY + .1f, -.32f), new Vector3(.7f, .55f, .85f), chitin);        // the abdomen
            Part(PrimitiveType.Sphere, body, new Vector3(0, hipY + .3f, -.3f), new Vector3(.36f, .2f, .5f), pattern);                  // the pale pattern on its back
            if (lit) { var glow = Mat(new Color(.3f, .9f, .5f), .9f); glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(.2f, .9f, .4f) * 1.5f); Part(PrimitiveType.Sphere, body, new Vector3(0, hipY - .08f, -.32f), new Vector3(.4f, .22f, .5f), glow); }
            head = Part(PrimitiveType.Sphere, body, new Vector3(0, hipY + .05f, .3f), new Vector3(.42f, .32f, .45f), chitin);          // the fore-body and head
            for (int i = 0; i < 8; i++) { float a = (i - 3.5f) * 14 * Mathf.Deg2Rad; Part(PrimitiveType.Sphere, head, new Vector3(Mathf.Sin(a) * .5f, .25f + (i % 2) * .15f, .85f + Mathf.Cos(a) * .1f), Vector3.one * (i % 2 == 0 ? .14f : .1f), eye); }
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Capsule, head, new Vector3(s * .22f, -.3f, .95f), new Vector3(.12f, .3f, .12f), fang, new Vector3(30, 0, s * 15));
            // Legs: the front and hind pairs on the walking pivots, the middle two pairs fixed.
            armL = Pivot("Leg FL", new Vector3(-.22f, hipY, .25f)); armR = Pivot("Leg FR", new Vector3(.22f, hipY, .25f));
            legL = Pivot("Leg HL", new Vector3(-.22f, hipY, -.3f)); legR = Pivot("Leg HR", new Vector3(.22f, hipY, -.3f));
            void Leg(Transform at, int s, float yaw)
            {
                var upper = Part(PrimitiveType.Capsule, at, Quaternion.Euler(0, yaw * s, 0) * new Vector3(s * .3f, .18f, 0), new Vector3(.07f, .36f, .07f), chitin, new Vector3(0, yaw * s, s * -60));
                Part(PrimitiveType.Sphere, upper, new Vector3(0, 1, 0), new Vector3(1.3f, .25f, 1.3f), chitin);   // the joint
                Part(PrimitiveType.Capsule, upper, new Vector3(0, 1.45f, 0), new Vector3(.8f, 1.5f, .8f), chitin, new Vector3(0, 0, s * 115));   // down to the ground
            }
            Leg(armL, -1, 25); Leg(armR, 1, 25); Leg(legL, -1, -35); Leg(legR, 1, -35);
            foreach (int s in new[] { -1, 1 }) { var mid = Pivot("Leg M" + s, new Vector3(s * .22f, hipY, 0)); Leg(mid, s, 70); Leg(mid, s, 110); }
            cloth = chitin; beast = true;
        }

        /// <summary>
        /// A bramble-thing (GAME-ONLY): a creeping knot of thorny briar that drags itself along on root-legs, with a dim red glow
        /// deep inside where something woke in it. Low and wide; its walk is a stir of the roots. Withered (variant odd) it is grey
        /// and the light violet, the Pale's touch on the forest.
        /// </summary>
        void BuildBramble()
        {
            bool withered = variant % 2 == 1;
            var briar = Mat(withered ? new Color(.3f, .28f, .26f) : new Color(.2f, .16f, .1f), .15f); var leaf = Mat(withered ? new Color(.38f, .35f, .28f) : new Color(.22f, .38f, .14f), .05f);
            var thorn = Mat(new Color(.5f, .42f, .3f), .4f);
            var glow = Mat(withered ? new Color(.5f, .2f, .7f) : new Color(.9f, .3f, .1f), .9f); glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", (withered ? new Color(.5f, .15f, .8f) : new Color(.9f, .25f, .05f)) * 1.6f);
            float hipY = -.7f; LieDepth = .15f;
            torso = Part(PrimitiveType.Sphere, body, new Vector3(0, hipY + .25f, 0), new Vector3(.9f, .6f, .9f), briar);
            head = Part(PrimitiveType.Sphere, body, new Vector3(0, hipY + .3f, .2f), new Vector3(.3f, .3f, .3f), glow);   // the ember at its heart, half showing through the front
            var rng = new System.Random(variant * 7 + 3);
            for (int i = 0; i < 18; i++)
            {
                // A canes-and-thorns knot: curved canes round the body, a leaf on some, a thorn on most.
                float a = (float)rng.NextDouble() * 360, tilt = (float)rng.NextDouble() * 80 - 10, len = .25f + (float)rng.NextDouble() * .35f;
                var cane = Part(PrimitiveType.Capsule, torso, Quaternion.Euler(tilt, a, 0) * new Vector3(0, .4f, 0), new Vector3(.08f, len, .08f), briar, new Vector3(tilt + 60, a, 0));
                if (i % 3 == 0) Part(PrimitiveType.Sphere, cane, new Vector3(0, 1, 0), new Vector3(2.8f, .4f, 2), leaf, new Vector3(0, a, 0));
                else Part(PrimitiveType.Cylinder, cane, new Vector3(0, 1.1f, 0), new Vector3(.5f, .5f, .5f), thorn);
            }
            armL = Pivot("Leg FL", new Vector3(-.3f, hipY, .3f)); armR = Pivot("Leg FR", new Vector3(.3f, hipY, .3f));
            legL = Pivot("Leg HL", new Vector3(-.3f, hipY, -.3f)); legR = Pivot("Leg HR", new Vector3(.3f, hipY, -.3f));
            foreach (var leg in new[] { armL, armR, legL, legR }) Part(PrimitiveType.Capsule, leg, new Vector3(0, -.15f, 0), new Vector3(.07f, .18f, .07f), briar, new Vector3(0, 0, 25));   // root-legs
            cloth = briar; beast = true;
        }
    }
}
