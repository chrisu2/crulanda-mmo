using System;
using UnityEngine;
using M = Crulanda.Encounter.GearMeshes;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The main-hand and off-hand families (loot DESIGN.md section 2.3), each a few painted parts at classic, slightly chunky
    /// proportions. Weapon frame: the grip at the origin, the weapon along +Y, its edge toward +Z, its flats facing +-X. Shield
    /// frame: the face toward +Y, the top of the shield toward +X (up the forearm), forward +Z. Hung frame: hanging down -Y from
    /// the hand. Every variant changes a feature you can see. Quality adds trim, an extra part, a wear mark on poor gear and
    /// glowing accents on rare and epic; at most 12 parts a piece. GAME-ONLY designs.
    /// </summary>
    public sealed partial class ActorVisual
    {
        static Mesh Cube { get { return M.Prim(PrimitiveType.Cube); } }
        static Mesh Sphere { get { return M.Prim(PrimitiveType.Sphere); } }
        static Mesh Cylinder { get { return M.Prim(PrimitiveType.Cylinder); } }
        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }
        static Vector3 S(float s) { return Vector3.one * s; }
        /// <summary>Turns a plate cut in (u up the weapon, v toward the edge) into the weapon frame: thickness across X.</summary>
        static readonly Quaternion Upright = Quaternion.Euler(0, 0, 90);
        static Vector2[] Pts(params float[] uv) { var p = new Vector2[uv.Length / 2]; for (int i = 0; i < p.Length; i++) p[i] = new Vector2(uv[2 * i], uv[2 * i + 1]); return p; }

        void BuildGear(GearLook l, Transform root)
        {
            var k = Kit(l);
            switch (l.family)
            {
                case "sword.short": SwordShort(k, root); break;
                case "sword.arming": SwordArming(k, root); break;
                case "sword.falchion": Falchion(k, root); break;
                case "sword.sabre": Sabre(k, root); break;
                case "sword.leaf": SwordLeaf(k, root); break;
                case "sword.great": Greatsword(k, root); break;
                case "knife": Knife(k, root); break;
                case "axe.hand": HandAxe(k, root); break;
                case "axe.bearded": BeardedAxe(k, root); break;
                case "axe.crescent": CrescentAxe(k, root); break;
                case "cleaver": Cleaver(k, root); break;
                case "club": Club(k, root); break;
                case "mace.flanged": FlangedMace(k, root); break;
                case "hammer.war": WarHammer(k, root); break;
                case "mace.root": RootMace(k, root); break;
                case "polearm": Polearm(k, root); break;
                case "staff": Staff(k, root); break;
                case "shield.buckler": Buckler(k, root); break;
                case "shield.round": RoundShield(k, root); break;
                case "shield.heater": HeaterShield(k, root); break;
                case "shield.kite": KiteShield(k, root); break;
                case "shield.leaf": LeafShield(k, root); break;
                case "offhand.hung": HungPiece(k, root); break;
                default: return;   // armour families: step A2
            }
            Finish(k, root);
        }

        // ---------- swords and knives ----------
        /// <summary>A grip with a leather wrap, and the generated material's mark on it.</summary>
        static void Grip(GearKit k, Transform r, float y0, float y1, float radius, Material m = null)
        {
            GRod(r, m ?? k.leather, y0, y1, radius); Detail(k, r, (y0 + y1) / 2, radius);
        }
        /// <summary>The usual quality marks on a blade starting at y0 of length len: the ricasso plate, rust, a rune and a gem at the guard.</summary>
        static void BladeMarks(GearKit k, Transform r, float y0, float len, float thick, float guardY, float guardThick)
        {
            Extra(k, r, Cube, V(0, y0 + .04f, 0), V(thick * 1.25f, .07f, .045f));
            Wear(k, r, V(0, y0 + len * .45f, .005f), V(thick * 1.12f, len * .16f, .045f));
            Accent(k, 1, r, Cube, V(0, y0 + len * .42f, 0), V(thick * 1.15f, len * .34f, .011f));
            Accent(k, 2, r, Sphere, V(0, guardY, 0), V(guardThick * 1.3f, .045f, .045f));
        }
        void SwordShort(GearKit k, Transform r)
        {
            Grip(k, r, -.09f, .09f, .033f);
            GBall(r, k.trim, V(0, -.125f, 0), S(.085f));
            GBox(r, k.trim, V(0, .11f, 0), V(.05f, .042f, .2f));
            GPart(r, M.Straight("short.blade", .55f, .09f, .032f, .22f), k.metal, V(0, .13f, 0), Vector3.one);
            if (k.l.variant == "notched")
                GPart(r, M.Many("short.notches", Cube, M.At(V(0, .3f, .043f), V(45, 0, 0), V(.036f, .03f, .03f)), M.At(V(0, .41f, .041f), V(45, 0, 0), V(.036f, .026f, .026f))), k.dark, V(0, 0, 0), Vector3.one);
            BladeMarks(k, r, .13f, .55f, .032f, .11f, .05f);
        }
        static float ArmingW(float t) { float u = Mathf.Clamp01((t - .8f) / .2f); return .039f * (1 - .12f * t) * (1 - u); }
        void SwordArming(GearKit k, Transform r)
        {
            Grip(k, r, -.1f, .1f, .033f);
            GPrim(PrimitiveType.Cylinder, r, k.trim, V(0, -.135f, 0), V(.1f, .024f, .1f), V(0, 0, 90));                               // wheel pommel
            switch (k.l.variant)
            {
                case "curved":
                    GPart(r, M.Many("arming.guard.curved", Cube, M.At(V(0, .135f, .075f), V(-24, 0, 0), V(.045f, .04f, .15f)), M.At(V(0, .135f, -.075f), V(24, 0, 0), V(.045f, .04f, .15f))), k.trim, Vector3.zero, Vector3.one);
                    break;
                case "disc": GPrim(PrimitiveType.Cylinder, r, k.trim, V(0, .12f, 0), V(.17f, .016f, .17f)); break;
                default: GBox(r, k.trim, V(0, .12f, 0), V(.05f, .045f, .27f)); break;
            }
            bool curved = k.l.variant == "curved";
            var blade = curved ? M.Blade("arming.blade.curved", .82f, ArmingW, ArmingW, .03f, t => -.07f * t * t) : M.Straight("arming.blade", .82f, .078f, .03f, .2f);
            GPart(r, blade, k.metal, V(0, .14f, 0), Vector3.one);
            if (!curved) GPart(r, M.Many("arming.fuller", Cube, M.At(V(.0145f, 0, 0), Vector3.zero, V(.004f, 1, .016f)), M.At(V(-.0145f, 0, 0), Vector3.zero, V(.004f, 1, .016f))), k.dark, V(0, .14f + .3f, 0), V(1, .5f, 1));
            BladeMarks(k, r, .14f, .82f, .03f, .12f, .05f);
        }
        void Falchion(GearKit k, Transform r)
        {
            bool heavy = k.l.variant == "heavy";
            float w0 = heavy ? .05f : .036f, grow = heavy ? .07f : .05f, spine = heavy ? .026f : .022f, tipZ = .02f;
            Func<float, float> edge = t => t <= .85f ? w0 + grow * t : Mathf.Lerp(w0 + grow * .85f, tipZ, Mathf.SmoothStep(0, 1, (t - .85f) / .15f));
            Func<float, float> back = t => t <= .78f ? spine : Mathf.Lerp(spine, -tipZ, (t - .78f) / .22f);   // the clipped point
            Grip(k, r, -.09f, .09f, .034f);
            GBall(r, k.trim, V(0, -.125f, 0), S(.08f));
            GPart(r, M.Many("falchion.sguard", Cube, M.At(V(0, .115f, .065f), V(-25, 0, 0), V(.045f, .035f, .13f)), M.At(V(0, .115f, -.065f), V(-25, 0, 0), V(.045f, .035f, .13f))), k.trim, Vector3.zero, Vector3.one);
            GPart(r, M.Blade("falchion.blade." + k.l.variant, heavy ? .74f : .7f, edge, back, heavy ? .04f : .034f, null, true), k.metal, V(0, .13f, 0), Vector3.one);
            if (heavy) GPart(r, M.Many("falchion.rivets", Sphere, M.At(V(0, .17f, .02f), Vector3.zero, V(.05f, .022f, .022f)), M.At(V(0, .17f, .05f), Vector3.zero, V(.05f, .022f, .022f))), k.trim, Vector3.zero, Vector3.one);
            BladeMarks(k, r, .13f, .7f, .034f, .115f, .045f);
        }
        void Sabre(GearKit k, Transform r)
        {
            bool broken = k.l.variant == "broken";
            float h = .033f; Func<float, float> bend = t => -.11f * t * t;
            float U(float t) { return Mathf.Clamp01((t - .82f) / .18f); }
            Grip(k, r, -.09f, .09f, .032f, broken ? k.leather : k.cloth2);
            GBall(r, k.trim, V(0, -.115f, .01f), V(.065f, .075f, .075f));
            GBox(r, k.trim, V(0, .113f, .025f), V(.05f, .034f, .13f));                                                                   // guard plate
            GPart(r, M.Rod("sabre.bow", new[] { V(0, .113f, .085f), V(0, .07f, .17f), V(0, -.09f, .15f), V(0, -.115f, .035f) }, .011f, .009f), k.trim, Vector3.zero, Vector3.one);   // knuckle bow
            GPart(r, M.Blade("sabre.blade", .84f, t => h * (1 - .1f * t) * (1 - 2 * U(t) * U(t)), t => h * (1 - .1f * t), .03f, bend, true), k.metal, V(0, .13f, 0), Vector3.one);
            if (broken)   // snapped and forged whole again: a seam across the blade, still glowing
            {
                float t = .45f; GBox(r, k.Lit(1.6f), V(0, .13f + .84f * t, bend(t)), V(.036f, .016f, .08f), V(-10, 0, 0));
                GBox(r, k.dark, V(0, .13f + .84f * t - .03f, bend(t) - .004f), V(.034f, .03f, .07f), V(-10, 0, 0));
            }
            else GPrim(PrimitiveType.Capsule, r, k.cloth2, V(0, -.17f, .02f), V(.03f, .05f, .03f));                                       // the officer's tassel
            BladeMarks(k, r, .13f, .5f, .03f, .113f, .05f);
        }
        static float LeafW(float t) { return t < .6f ? Mathf.Lerp(.026f, .047f, Mathf.SmoothStep(0, 1, t / .6f)) : .047f * Mathf.Sqrt(Mathf.Max(0, 1 - (t - .6f) * (t - .6f) / .16f)); }
        void SwordLeaf(GearKit k, Transform r)
        {
            bool root = k.l.variant == "root";
            Grip(k, r, -.09f, .09f, .033f, root ? k.wood : k.leather);
            if (root) GPart(r, M.Lump("leaf.knot", 5, .5f), k.wood, V(0, -.12f, 0), V(.09f, .08f, .09f));
            else GBall(r, k.trim, V(0, -.12f, 0), S(.08f));
            if (root)
            {
                GPart(r, M.Rod("leaf.curl.a", new[] { V(0, .09f, 0), V(0, .1f, .1f), V(0, .2f, .12f), V(0, .21f, .06f) }, .017f, .006f), k.wood, Vector3.zero, Vector3.one);
                GPart(r, M.Rod("leaf.curl.b", new[] { V(0, .09f, 0), V(0, .1f, -.1f), V(0, .2f, -.12f), V(0, .21f, -.06f) }, .017f, .006f), k.wood, Vector3.zero, Vector3.one);
            }
            else GBox(r, k.metal, V(0, .11f, 0), V(.05f, .04f, .15f));
            GPart(r, M.Blade("leaf.blade", .74f, LeafW, LeafW, .032f), k.metal, V(0, .12f, 0), Vector3.one);
            GBox(r, root ? GearMats.Get(Color.Lerp(k.l.wood, new Color(.3f, .45f, .22f), .5f), .3f) : k.trim, V(0, .12f + .3f, 0), V(.036f, .52f, .011f));   // midrib
            BladeMarks(k, r, .12f, .74f, .032f, .11f, .05f);
        }
        void Greatsword(GearKit k, Transform r)
        {
            string v = k.l.variant; bool wooden = v != "steel";
            Grip(k, r, -.13f, .13f, .036f);
            GBall(r, k.trim, V(0, -.17f, 0), V(.1f, .11f, .1f));
            GBox(r, k.trim, V(0, .16f, 0), V(.06f, .055f, .34f));
            GPart(r, M.Straight("great.blade", 1.05f, .1f, .036f, .16f), wooden ? k.wood : k.metal, V(0, .18f, 0), Vector3.one);
            if (v == "steel") GPart(r, M.Many("great.fuller", Cube, M.At(V(.0172f, 0, 0), Vector3.zero, V(.004f, 1, .02f)), M.At(V(-.0172f, 0, 0), Vector3.zero, V(.004f, 1, .02f))), k.dark, V(0, .18f + .38f, 0), V(1, .64f, 1));
            else GPart(r, M.Many("great.edges", Cube, M.At(V(0, 0, .047f), Vector3.zero, V(.022f, 1, .012f)), M.At(V(0, 0, -.047f), Vector3.zero, V(.022f, 1, .012f))), k.metal, V(0, .18f + .44f, 0), V(1, .86f, 1));   // metal-shod edges
            if (v == "living")   // sprouting twigs with lit buds
            {
                var twig = M.Rod("great.twig", new[] { V(0, 0, 0), V(0, .06f, .03f), V(0, .12f, .09f) }, .012f, .004f);
                GPart(r, M.Many("great.twigs", twig, M.At(V(.01f, .52f, -.045f), V(0, 180, 0), Vector3.one), M.At(V(-.01f, .78f, .045f), Vector3.zero, Vector3.one), M.At(V(.01f, .95f, -.04f), V(0, 180, 0), V(.8f, .8f, .8f))), k.wood, Vector3.zero, Vector3.one);
                GPart(r, M.Many("great.buds", Sphere, M.At(V(.01f, .64f, -.135f), Vector3.zero, S(.032f)), M.At(V(-.01f, .9f, .135f), Vector3.zero, S(.032f)), M.At(V(.01f, 1.046f, -.112f), Vector3.zero, S(.026f))), k.Lit(1.8f), Vector3.zero, Vector3.one);
            }
            BladeMarks(k, r, .18f, 1.05f, .036f, .16f, .06f);
        }
        void Knife(GearKit k, Transform r)
        {
            string v = k.l.variant;
            float U(float t) { return Mathf.Clamp01((t - .7f) / .3f); }
            Grip(k, r, -.075f, .075f, .028f);
            GBall(r, k.trim, V(0, -.1f, 0), S(.065f));
            switch (v)
            {
                case "needle":
                    GPrim(PrimitiveType.Cylinder, r, k.trim, V(0, .088f, 0), V(.1f, .012f, .1f));
                    GPart(r, M.Blade("knife.needle", .36f, t => .018f * (1 - t), t => .018f * (1 - t), .022f), k.metal, V(0, .1f, 0), Vector3.one);
                    break;
                case "glass":   // a shard of glowing glass
                    GBox(r, k.metal, V(0, .088f, 0), V(.04f, .03f, .1f));
                    GPart(r, M.Blade("knife.glass", .34f, t => .032f * (1 - t) * (1 + .3f * Mathf.Sin(t * 19)), t => .03f * (1 - t) * (1 + .3f * Mathf.Sin(t * 13 + 2)), .02f), GearMats.Glass(Color.Lerp(k.l.glow, Color.white, .5f), .6f, k.l.glow * .9f), V(0, .1f, 0), Vector3.one);
                    break;
                case "sickle":
                    GBand(r, k.metal, .09f, .03f, .03f);
                    GPart(r, M.Arc("knife.sickle", .13f, .165f, 180, 430, .014f), k.metal, V(0, .1f + .165f, 0), Vector3.one, V(0, 0, 90));
                    GPart(r, M.Arc("knife.sickle.edge", .123f, .133f, 200, 425, .006f), k.edge, V(0, .1f + .165f, 0), Vector3.one, V(0, 0, 90));
                    break;
                default:   // hooked: the point turns down to the edge
                    GBox(r, k.trim, V(0, .088f, 0), V(.04f, .03f, .1f));
                    GPart(r, M.Blade("knife.hooked", .3f, t => .025f * (1 - .6f * U(t) * U(t)), t => .02f * (1 - U(t)), .026f, t => .085f * U(t) * U(t), true), k.metal, V(0, .1f, 0), Vector3.one);
                    break;
            }
            Wear(k, r, V(0, .2f, .005f), V(.03f, .05f, .03f));
            Extra(k, r, Cylinder, V(0, .068f, 0), V(.07f, .01f, .07f));
            Accent(k, 1, r, Sphere, V(0, -.1f, 0), V(.07f, .04f, .04f));
            Accent(k, 2, r, Sphere, V(0, .088f, 0), V(.05f, .035f, .035f));
        }

        // ---------- axes, cleavers ----------
        /// <summary>A wooden haft from y0 to y1 with a leather wrap at the hand and an iron butt knob.</summary>
        static void Haft(GearKit k, Transform r, float y0, float y1, float radius)
        {
            GRod(r, k.wood, y0, y1, radius); GRod(r, k.leather, -.09f, .09f, radius * 1.18f);
            GBall(r, k.dark, V(0, y0, 0), S(radius * 2.3f)); Detail(k, r, .14f, radius);
        }
        static void HeadMarks(GearKit k, Transform r, Vector3 head, float thick, float capY, float radius)
        {
            Extra(k, r, Cylinder, V(0, capY, 0), V(radius * 2.3f, .014f, radius * 2.3f));
            Wear(k, r, head + V(0, -.01f, .03f), V(thick * 1.1f, .05f, .05f));
            Accent(k, 1, r, Cube, head + V(0, 0, .04f), V(thick * 1.1f, .05f, .014f));
            Accent(k, 2, r, Cylinder, V(0, capY - .2f, 0), V(radius * 2.4f, .008f, radius * 2.4f));
        }
        void HandAxe(GearKit k, Transform r)
        {
            bool hatchet = k.l.variant == "hatchet";
            Haft(k, r, -.14f, .5f, .028f);
            var head = V(0, .43f, 0);
            var outline = hatchet ? Pts(-.035f, -.03f, .035f, -.03f, .05f, .06f, .062f, .13f, -.07f, .13f, -.045f, .06f) : Pts(-.042f, -.036f, .042f, -.036f, .062f, .07f, .078f, .155f, -.078f, .155f, -.062f, .07f);
            GPart(r, M.Plate("axe.hand.head." + k.l.variant, outline, .05f, 0, null, null, 0, .3f, Upright), k.metal, head, Vector3.one);
            GBox(r, k.edge, head + V(0, hatchet ? 0 : 0, hatchet ? .128f : .152f), V(.017f, hatchet ? .125f : .15f, .012f));
            if (hatchet) GBox(r, k.metal, head + V(0, 0, -.055f), V(.05f, .055f, .05f));                                                   // the hammer poll
            HeadMarks(k, r, head, .05f, .505f, .028f);
        }
        void BeardedAxe(GearKit k, Transform r)
        {
            bool hooked = k.l.variant == "hooked";
            Haft(k, r, -.14f, .6f, .029f);
            var head = V(0, .48f, 0);
            var outline = hooked
                ? Pts(.055f, -.035f, .07f, .08f, .095f, .21f, -.21f, .22f, -.235f, .165f, -.17f, .14f, -.15f, .12f, -.08f, .06f, -.045f, .02f, -.045f, -.035f)
                : Pts(.055f, -.035f, .07f, .08f, .095f, .21f, -.21f, .22f, -.15f, .12f, -.08f, .06f, -.045f, .02f, -.045f, -.035f);
            GPart(r, M.Plate("axe.bearded.head." + k.l.variant, outline, .05f, 0, null, null, 0, .35f, Upright, new Vector2(0, .09f)), k.metal, head, Vector3.one);
            var edge = hooked ? new[] { V(0, .095f, .212f), V(0, -.06f, .24f), V(0, -.2f, .225f), V(0, -.235f, .168f) } : new[] { V(0, .095f, .212f), V(0, -.06f, .24f), V(0, -.21f, .222f) };
            GPart(r, M.Rod("axe.bearded.edge." + k.l.variant, edge, .009f, .007f), k.edge, head, Vector3.one);
            HeadMarks(k, r, head, .05f, .605f, .029f);
        }
        void CrescentAxe(GearKit k, Transform r)
        {
            Haft(k, r, -.14f, .6f, .029f);
            var head = V(0, .46f, -.02f);
            GBox(r, k.metal, V(0, .46f, 0), V(.06f, .13f, .065f));                                                                            // socket
            GPart(r, M.Arc("axe.crescent.head", .1f, .19f, 20, 160, .036f), k.metal, head, Vector3.one, V(0, 0, 90));
            GPart(r, M.Arc("axe.crescent.edge", .182f, .197f, 22, 158, .018f), k.edge, head, Vector3.one, V(0, 0, 90));
            if (k.l.variant == "spiked") GPart(r, M.Cone(), k.metal, V(0, .46f, -.03f), V(.055f, .14f, .055f), V(-90, 0, 0));
            HeadMarks(k, r, V(0, .46f, .08f), .036f, .605f, .029f);
        }
        void Cleaver(GearKit k, Transform r)
        {
            GRod(r, k.wood, -.1f, .14f, .03f); GBall(r, k.dark, V(0, -.11f, 0), S(.06f)); Detail(k, r, 0, .03f);
            GBox(r, k.metal, V(0, .13f, 0), V(.042f, .05f, .05f));                                                                            // ferrule
            GPart(r, M.Plate("cleaver.blade", Pts(.12f, -.012f, .52f, -.012f, .535f, .17f, .13f, .18f), .032f, 0, null, null, 0, .4f, Upright), k.metal, Vector3.zero, Vector3.one);
            GBox(r, k.edge, V(0, .33f, .172f), V(.014f, .4f, .012f));
            GPrim(PrimitiveType.Cylinder, r, k.dark, V(0, .46f, .11f), V(.045f, .02f, .045f), V(0, 0, 90));                                    // hanging hole
            if (k.l.variant == "notched") GPart(r, M.Many("cleaver.notches", Cube, M.At(V(0, .25f, -.012f), V(45, 0, 0), S(.03f)), M.At(V(0, .34f, -.012f), V(45, 0, 0), S(.026f)), M.At(V(0, .43f, -.012f), V(45, 0, 0), S(.032f))), k.dark, Vector3.zero, Vector3.one);
            else GPart(r, M.Many("cleaver.rivets", Sphere, M.At(V(0, -.02f, 0), Vector3.zero, V(.068f, .02f, .02f)), M.At(V(0, .07f, 0), Vector3.zero, V(.068f, .02f, .02f))), k.trim, Vector3.zero, Vector3.one);
            Extra(k, r, Cube, V(0, .33f, -.008f), V(.036f, .38f, .012f));
            Wear(k, r, V(0, .3f, .08f), V(.036f, .08f, .06f));
            Accent(k, 1, r, Cube, V(0, .3f, .07f), V(.036f, .14f, .016f));
            Accent(k, 2, r, Sphere, V(0, .46f, .11f), V(.05f, .03f, .03f));
        }

        // ---------- clubs, maces, hammers ----------
        void Club(GearKit k, Transform r)
        {
            string v = k.l.variant;
            if (v == "tusk")
            {
                GPart(r, M.Rod("club.tusk", new[] { V(0, -.13f, 0), V(0, .2f, .02f), V(0, .44f, .1f), V(0, .55f, .21f) }, .046f, .011f, 9, 12), k.bone, Vector3.zero, Vector3.one);
                GRod(r, k.leather, -.1f, .1f, .052f); Detail(k, r, .13f, .05f);
                GBand(r, k.cloth2, .2f, .049f, .03f);
                Accent(k, 1, r, Sphere, V(0, .3f, .045f), V(.05f, .05f, .03f));
            }
            else if (v == "tankard")   // a pewter tankard, swung by its handle
            {
                var pewter = GearMats.Get(Color.Lerp(k.l.metal, new Color(.66f, .67f, .66f), .6f), .55f, .55f);
                var mug = V(0, .115f, .09f);   // its foot, below the hand; it stands up along -Z (world up)
                GPart(r, M.Lathe("tankard.mug", Pts(0, 0, .072f, 0, .076f, .015f, .068f, .16f, .074f, .175f, .062f, .19f, 0, .2f), 16), pewter, mug, Vector3.one, V(-90, 0, 0));
                GPart(r, M.Rod("tankard.handle", new[] { V(0, .045f, .06f), V(0, -.035f, .07f), V(0, -.035f, -.07f), V(0, .045f, -.06f) }, .013f, .013f), pewter, V(0, 0, -.01f), Vector3.one);
                GBall(r, k.trim, V(0, .07f, -.115f), V(.03f, .03f, .02f));                                                                     // thumb-lift
                GPart(r, M.Many("tankard.hoops", Cylinder, M.At(V(0, 0, -.03f), V(90, 0, 0), V(.16f, .008f, .16f)), M.At(V(0, 0, -.15f), V(90, 0, 0), V(.155f, .008f, .155f))), k.trim, mug, Vector3.one);
                Wear(k, r, mug + V(.06f, 0, -.09f), V(.03f, .05f, .05f));
                Accent(k, 1, r, Sphere, mug + V(0, .073f, -.1f), V(.035f, .02f, .035f));
                Accent(k, 2, r, Sphere, mug + V(0, -.073f, -.1f), V(.035f, .02f, .035f));
                return;
            }
            else
            {
                GPart(r, M.Lathe("club.body", Pts(0, -.14f, .03f, -.14f, .032f, .05f, .05f, .3f, .068f, .46f, .06f, .53f, 0, .56f), 12, true), k.wood, Vector3.zero, Vector3.one);
                GRod(r, k.leather, -.1f, .08f, .036f); Detail(k, r, .12f, .038f);
                if (v == "studded")
                {
                    var studs = new Matrix4x4[8];
                    for (int i = 0; i < 8; i++) { float a = i * 45 + (i % 2) * 22.5f, y = i % 2 == 0 ? .37f : .46f, rr = i % 2 == 0 ? .06f : .069f; studs[i] = M.At(V(Mathf.Cos(a * Mathf.Deg2Rad) * rr, y, Mathf.Sin(a * Mathf.Deg2Rad) * rr), V(0, -a, 45), S(.032f)); }
                    GPart(r, M.Many("club.studs", Cube, studs), k.dark, Vector3.zero, Vector3.one);
                }
                if (v == "bound") GPart(r, M.Many("club.bands", Cylinder, M.At(V(0, .2f, 0), Vector3.zero, V(.1f, .012f, .1f)), M.At(V(0, .33f, 0), Vector3.zero, V(.12f, .012f, .12f)), M.At(V(0, .45f, 0), Vector3.zero, V(.142f, .012f, .142f))), k.cloth2, Vector3.zero, Vector3.one);
                Accent(k, 1, r, Cylinder, V(0, .4f, 0), V(.135f, .01f, .135f));
            }
            Extra(k, r, Cylinder, V(0, .09f, 0), V(.08f, .012f, .08f));
            Wear(k, r, V(.045f, .3f, 0), V(.03f, .08f, .05f));
            Accent(k, 2, r, Sphere, V(0, -.12f, 0), S(.045f));
        }
        void FlangedMace(GearKit k, Transform r)
        {
            GRod(r, k.wood, -.13f, .45f, .025f); GRod(r, k.leather, -.09f, .09f, .03f); GBall(r, k.dark, V(0, -.14f, 0), S(.055f)); Detail(k, r, .14f, .025f);
            if (k.l.variant == "fist")   // a hand of stone
            {
                var stone = GearMats.Get(Color.Lerp(k.l.metal, new Color(.55f, .55f, .55f), .4f), .1f);
                GPart(r, M.Lump("mace.fist", 7, .35f), stone, V(0, .5f, .01f), V(.15f, .15f, .13f));
                GPart(r, M.Many("mace.knuckles", Sphere, M.At(V(-.048f, .01f, 0), Vector3.zero, S(.05f)), M.At(V(-.016f, .018f, 0), Vector3.zero, S(.055f)), M.At(V(.016f, .016f, 0), Vector3.zero, S(.055f)), M.At(V(.048f, .006f, 0), Vector3.zero, S(.05f))), stone, V(0, .5f, .065f), Vector3.one);
                GPart(r, M.Many("mace.thumb", Sphere, M.At(Vector3.zero, Vector3.zero, V(.04f, .07f, .04f))), stone, V(.06f, .46f, .03f), Vector3.one, V(0, 0, 30));
            }
            else
            {
                GBall(r, k.metal, V(0, .46f, 0), V(.075f, .17f, .075f));
                var flange = M.Plate("mace.flange", Pts(-.1f, .02f, .1f, .02f, .065f, .088f, .0f, .1f, -.07f, .084f), .017f, 0, null, null, 0, 1, Upright);
                var places = new Matrix4x4[6]; for (int i = 0; i < 6; i++) places[i] = M.At(Vector3.zero, V(0, i * 60, 0), Vector3.one);
                GPart(r, M.Many("mace.flanges", flange, places), k.metal, V(0, .46f, 0), Vector3.one);
                GBall(r, k.trim, V(0, .575f, 0), S(.05f));
            }
            Extra(k, r, Cylinder, V(0, .35f, 0), V(.065f, .014f, .065f));
            Wear(k, r, V(0, .46f, .05f), V(.05f, .06f, .05f));
            Accent(k, 1, r, Cylinder, V(0, .46f, 0), V(.098f, .012f, .098f));
            Accent(k, 2, r, Sphere, V(0, .6f, 0), S(.035f));
        }
        void WarHammer(GearKit k, Transform r)
        {
            bool maul = k.l.variant == "maul";
            Haft(k, r, -.14f, .58f, .027f);
            GPart(r, M.Many("hammer.langets", Cube, M.At(V(.028f, 0, 0), Vector3.zero, V(.008f, 1, .02f)), M.At(V(-.028f, 0, 0), Vector3.zero, V(.008f, 1, .02f))), k.metal, V(0, .45f, 0), V(1, .16f, 1));
            if (maul)
            {
                GBox(r, k.metal, V(0, .56f, 0), V(.14f, .14f, .25f));
                GPart(r, M.Many("hammer.faces", Cube, M.At(V(0, 0, .125f), Vector3.zero, V(.15f, .15f, .02f)), M.At(V(0, 0, -.125f), Vector3.zero, V(.15f, .15f, .02f))), k.trim, V(0, .56f, 0), Vector3.one);
            }
            else
            {
                GBox(r, k.metal, V(0, .56f, .03f), V(.1f, .1f, .17f));
                GBox(r, k.trim, V(0, .56f, .118f), V(.11f, .11f, .02f));                                                                     // striking face
                GPart(r, M.Cone(), k.metal, V(0, .56f, -.05f), V(.07f, .17f, .07f), V(-90, 0, 0));                                          // the pick
            }
            Extra(k, r, Cylinder, V(0, .62f, 0), V(.07f, .012f, .07f));
            Wear(k, r, V(0, .6f, .02f), V(.05f, .04f, .08f));
            Accent(k, 1, r, Cube, V(0, .56f, .02f), V(maul ? .145f : .105f, .03f, .06f));
            Accent(k, 2, r, Sphere, V(0, .63f, 0), S(.035f));
        }
        void RootMace(GearKit k, Transform r)
        {
            string v = k.l.variant;
            var rings = new float[14]; for (int i = 0; i < 14; i++) rings[i] = i / 13f;
            var haft = M.Cached("root.haft", () => Crulanda.World.ZoneMeshes.Tube(t => V(Mathf.Sin(t * 6) * .006f, -.14f + t * .58f, Mathf.Cos(t * 5) * .006f), (t, a) => .026f + .006f * Mathf.Sin(3 * a + t * 14), rings, 8, Vector3.right));
            GPart(r, haft, k.wood, Vector3.zero, Vector3.one); GRod(r, k.leather, -.09f, .09f, .033f); Detail(k, r, .14f, .03f);
            var burlWood = GearMats.Get(k.l.wood * .8f, .2f);
            var thorn = M.Cone(6);
            if (v == "briar")   // a thorned ball
            {
                GPart(r, M.Lump("root.briar", 4, .3f), GearMats.Get(Color.Lerp(k.l.cloth, k.l.wood, .4f), .2f), V(0, .5f, 0), S(.17f));
                var ts = new Matrix4x4[10];
                for (int i = 0; i < 10; i++) { var d = Quaternion.Euler(i * 37 % 180 - 60, i * 101, 0) * Vector3.up; ts[i] = Matrix4x4.TRS(d * .07f, Quaternion.FromToRotation(Vector3.up, d), V(.03f, .07f, .03f)); }
                GPart(r, M.Many("root.briar.thorns", thorn, ts), burlWood, V(0, .5f, 0), Vector3.one);
            }
            else
            {
                GPart(r, M.Lump("root.burl", 11, .55f), burlWood, V(0, .5f, 0), v == "antler" ? V(.14f, .12f, .13f) : V(.2f, .19f, .18f));
                if (v == "antler")   // forked tines
                {
                    var tine = M.Rod("root.tine", new[] { V(0, 0, 0), V(0, .08f, .03f), V(0, .17f, .0f) }, .017f, .005f);
                    GPart(r, M.Many("root.tines", tine, M.At(Vector3.zero, V(-25, 0, 0), Vector3.one), M.At(Vector3.zero, V(25, 0, 20), V(.85f, .85f, .85f)), M.At(Vector3.zero, V(5, 60, -30), V(.75f, .75f, .75f)), M.At(Vector3.zero, V(10, -120, 25), V(.7f, .7f, .7f))), k.bone, V(0, .53f, 0), Vector3.one);
                }
                else GPart(r, M.Many("root.thorns", thorn, M.At(V(.085f, 0, 0), V(0, 0, -80), V(.04f, .09f, .04f)), M.At(V(-.05f, .04f, .06f), V(50, 0, 60), V(.04f, .09f, .04f)), M.At(V(-.03f, -.02f, -.08f), V(-70, 0, 20), V(.04f, .09f, .04f))), k.metal, V(0, .5f, 0), Vector3.one);
            }
            Extra(k, r, Cylinder, V(0, .4f, 0), V(.07f, .014f, .07f));
            Wear(k, r, V(0, .3f, .02f), V(.04f, .06f, .04f));
            Accent(k, 1, r, Sphere, V(0, .5f, .085f), S(.04f));
            Accent(k, 2, r, Sphere, V(0, .5f, -.085f), S(.04f));
        }

        // ---------- staves and polearms ----------
        void Polearm(GearKit k, Transform r)
        {
            string v = k.l.variant; float top = .95f;
            GRod(r, k.wood, -.58f, top, .026f); GRod(r, k.leather, -.1f, .1f, .031f); Detail(k, r, .2f, .026f);
            GRod(r, k.dark, -.6f, -.54f, .03f);                                                                                              // butt cap
            switch (v)
            {
                case "harpoon":   // a barbed bone head, lashed on
                    GBand(r, k.cloth2, top - .02f, .031f, .05f);
                    GPart(r, M.Straight("harpoon.head", .28f, .07f, .03f, .45f), k.bone, V(0, top, 0), Vector3.one);
                    GPart(r, M.Many("harpoon.barbs", Cube, M.At(V(0, top + .07f, .05f), V(-40, 0, 0), V(.022f, .09f, .02f)), M.At(V(0, top + .07f, -.05f), V(40, 0, 0), V(.022f, .09f, .02f))), k.bone, Vector3.zero, Vector3.one);
                    break;
                case "fork":   // two tines, one bent
                    GBox(r, k.metal, V(0, top + .02f, 0), V(.03f, .03f, .17f));
                    GPart(r, M.Many("fork.tines", Cylinder, M.At(V(0, top + .14f, .075f), V(9, 0, 0), V(.022f, .13f, .022f)), M.At(V(0, top + .14f, -.075f), Vector3.zero, V(.022f, .13f, .022f))), k.metal, Vector3.zero, Vector3.one);
                    break;
                case "billhook":
                    GBox(r, k.metal, V(0, top + .02f, 0), V(.045f, .08f, .05f));
                    GPart(r, M.Plate("bill.blade", Pts(0, -.025f, .34f, -.025f, .39f, .03f, .36f, .09f, .2f, .085f, .05f, .06f, 0, .03f), .032f, 0, null, null, 0, .4f, Upright), k.metal, V(0, top + .04f, 0), Vector3.one);
                    GPart(r, M.Rod("bill.hook", new[] { V(0, top + .38f, .085f), V(0, top + .45f, .15f), V(0, top + .38f, .21f) }, .017f, .005f), k.edge, Vector3.zero, Vector3.one);
                    GPart(r, M.Cone(), k.metal, V(0, top + .2f, -.02f), V(.04f, .1f, .04f), V(-90, 0, 0));
                    break;
                case "spade":   // a grave-digger's spade
                    GBox(r, k.metal, V(0, top + .03f, 0), V(.04f, .1f, .05f));
                    GPart(r, M.Plate("spade.blade", Pts(0, -.07f, .2f, -.085f, .27f, -.06f, .3f, 0, .27f, .06f, .2f, .085f, 0, .07f), .022f, 0, null, null, 0, 1, Upright), k.metal, V(0, top + .07f, 0), Vector3.one);
                    GBox(r, k.edge, V(0, top + .07f + .297f, 0), V(.018f, .012f, .1f));
                    break;
                default:   // spear: a long leaf point on an iron socket
                    GPart(r, M.Cone(), k.metal, V(0, top - .02f, 0), V(.06f, .09f, .06f));
                    GPart(r, M.Blade("spear.head", .32f, t => LeafW(t) * .9f, t => LeafW(t) * .9f, .03f), k.metal, V(0, top + .05f, 0), Vector3.one);
                    break;
            }
            Extra(k, r, Cylinder, V(0, top - .06f, 0), V(.07f, .014f, .07f));
            Wear(k, r, V(0, top + .1f, .01f), V(.035f, .06f, .05f));
            Accent(k, 1, r, Cylinder, V(0, top - .1f, 0), V(.064f, .012f, .064f));
            Accent(k, 2, r, Cylinder, V(0, -.2f, 0), V(.064f, .012f, .064f));
        }
        void Staff(GearKit k, Transform r)
        {
            string v = k.l.variant; float top = .85f;
            GRod(r, k.wood, -.6f, top, .027f); Detail(k, r, 0, .027f);
            GPart(r, M.Many("staff.ferrules", Cylinder, M.At(V(0, top - .25f, 0), Vector3.zero, V(.064f, .012f, .064f)), M.At(V(0, -.56f, 0), Vector3.zero, V(.064f, .02f, .064f))), k.metal, Vector3.zero, Vector3.one);
            Vector3 crown;
            switch (v)
            {
                case "crook":
                    GPart(r, M.Rod("staff.crook", new[] { V(0, top - .02f, 0), V(0, top + .2f, 0), V(0, top + .28f, .12f), V(0, top + .17f, .2f), V(0, top + .1f, .15f) }, .027f, .022f, 7, 14), k.wood, Vector3.zero, Vector3.one);
                    crown = V(0, top + .18f, 0); break;
                case "forked":   // two prongs holding a crystal of salt
                    var prong = M.Rod("staff.prong", new[] { V(0, 0, 0), V(0, .11f, .035f), V(0, .22f, .08f), V(0, .28f, .065f) }, .024f, .01f);
                    GPart(r, M.Many("staff.prongs", prong, M.At(Vector3.zero, Vector3.zero, Vector3.one), M.At(Vector3.zero, V(0, 180, 0), Vector3.one)), k.wood, V(0, top - .02f, 0), Vector3.one);
                    crown = V(0, top + .15f, 0);
                    if (k.l.accents < 1) GPart(r, M.Lump("staff.crystal", 9, .5f, true), GearMats.Get(Color.Lerp(k.l.trim, Color.white, .4f), .85f), crown, V(.06f, .09f, .06f));
                    break;
                case "skull":   // the Cult's skull knob
                    GBall(r, k.bone, V(0, top + .1f, 0), V(.16f, .2f, .16f));
                    GPart(r, M.Many("staff.sockets", Sphere, M.At(V(.035f, 0, 0), Vector3.zero, S(.045f)), M.At(V(-.035f, 0, 0), Vector3.zero, S(.045f))), GearMats.Get(new Color(.08f, .06f, .07f)), V(0, top + .11f, .065f), Vector3.one);
                    GBox(r, k.bone, V(0, top + .01f, .035f), V(.09f, .04f, .07f));
                    crown = V(0, top + .1f, .075f); break;
                default:   // a turned knob
                    GPart(r, M.Lump("staff.knob", 3, .35f), k.wood, V(0, top + .05f, 0), V(.13f, .14f, .13f));
                    GBand(r, k.metal, top - .02f, .034f, .03f);
                    crown = V(0, top + .12f, 0); break;
            }
            Extra(k, r, Cylinder, V(0, top - .3f, 0), V(.066f, .014f, .066f));
            Wear(k, r, V(0, .3f, .02f), V(.04f, .1f, .04f));
            Accent(k, 1, r, v == "forked" ? M.Lump("staff.crystal", 9, .5f, true) : Sphere, crown, v == "forked" ? V(.06f, .09f, .06f) : S(.05f));
            Accent(k, 2, r, Sphere, V(0, top - .25f, .032f), S(.03f));
        }

        // ---------- shields ----------
        static Vector2[] Circle(float radius, int n) { var p = new Vector2[n]; for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; p[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius; } return p; }
        /// <summary>A flat paint (or strap) on a domed face: the same dome, just proud of it.</summary>
        static Mesh Paint(string key, Vector2[] outline, float faceThick, float bulge, Vector2 centre, Vector2 radius, float raise = .0015f, float depth = .002f)
        { return M.Plate(key, outline, depth, bulge, centre, radius, faceThick / 2 + raise - depth / 2 + .0005f); }
        /// <summary>Several flat paints in one mesh.</summary>
        static Mesh Paints(string key, float faceThick, float bulge, Vector2 centre, Vector2 radius, params Vector2[][] outlines)
        {
            return M.Cached(key, () =>
            {
                var c = new CombineInstance[outlines.Length];
                for (int i = 0; i < outlines.Length; i++) c[i] = new CombineInstance { mesh = Paint(key + "." + i, outlines[i], faceThick, bulge, centre, radius), transform = Matrix4x4.identity };
                var m = new Mesh(); m.CombineMeshes(c, true, true); m.RecalculateBounds(); return m;
            });
        }
        static Vector2[] Sector(float r0, float r1, float a0, float a1, int n)
        {
            var p = new Vector2[n + 2]; p[0] = new Vector2(Mathf.Cos(a0 * Mathf.Deg2Rad), Mathf.Sin(a0 * Mathf.Deg2Rad)) * r0;
            for (int i = 0; i <= n; i++) { float a = Mathf.Lerp(a0, a1, (float)i / n) * Mathf.Deg2Rad; p[i + 1] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r1; }
            return p;
        }
        static Vector2[] Strip(float x0, float x1, float z, float w) { return Pts(x0, z - w / 2, x1, z - w / 2, x1, z + w / 2, x0, z + w / 2); }
        /// <summary>A domed face's height at (x, z): what a plate cut with these numbers has on top.</summary>
        static Func<float, float, float> Dome(float th, float bulge, Vector2 c, Vector2 radius)
        { return (x, z) => th / 2 + bulge * Mathf.Max(0, 1 - ((x - c.x) / radius.x) * ((x - c.x) / radius.x) - ((z - c.y) / radius.y) * ((z - c.y) / radius.y)); }
        /// <summary>A shield's quality marks on its face (heights from <paramref name="surface"/>): rust, a gem on the boss, two gems either side.</summary>
        static void ShieldMarks(GearKit k, Transform r, Func<float, float, float> surface, float reach, float bossTop)
        {
            float wx = reach * .5f, wz = reach * .35f, gz = reach * .8f;
            Wear(k, r, V(wx, surface(wx, wz), wz), V(.08f, .008f, .06f), V(0, 30, 0));
            Accent(k, 1, r, Sphere, V(0, bossTop - .006f, 0), V(.05f, .03f, .05f));
            Accent(k, 2, r, M.Many("shield.gems." + Mathf.RoundToInt(gz * 1000), Sphere, M.At(V(0, 0, gz), Vector3.zero, V(.035f, .03f, .035f)), M.At(V(0, 0, -gz), Vector3.zero, V(.035f, .03f, .035f))), V(0, surface(0, gz) + .004f, 0), Vector3.one);
        }
        void Buckler(GearKit k, Transform r)
        {
            string v = k.l.variant;
            var face = v == "tusk" ? k.wood : v == "chitin" ? GearMats.Get(Color.Lerp(k.l.cloth, new Color(.15f, .2f, .12f), .5f), .75f, .1f) : k.metal;
            GPart(r, M.Lathe("buckler.dome", Pts(0, -.012f, .17f, -.012f, .176f, 0, .15f, .035f, .08f, .055f, 0, .06f), 20, true), face, Vector3.zero, Vector3.one);
            GPart(r, M.Lathe("buckler.rim", Pts(.16f, -.014f, .188f, -.014f, .188f, .018f, .163f, .03f, .16f, -.014f), 20), v == "tusk" ? k.dark : k.trim, Vector3.zero, Vector3.one);
            float Surface(float x, float z)   // the dome's profile, by distance from the middle
            {
                float d = Mathf.Sqrt(x * x + z * z);
                return d < .08f ? Mathf.Lerp(.06f, .055f, d / .08f) : d < .15f ? Mathf.Lerp(.055f, .035f, (d - .08f) / .07f) : Mathf.Lerp(.035f, 0, (d - .15f) / .026f);
            }
            float bossTop = .096f;
            if (v == "chitin")
            {
                GPart(r, M.Lathe("buckler.ridge.a", Pts(.07f, .05f, .085f, .05f, .085f, .063f, .07f, .065f, .07f, .05f), 18), face, Vector3.zero, Vector3.one);
                GPart(r, M.Lathe("buckler.ridge.b", Pts(.12f, .04f, .135f, .038f, .135f, .052f, .12f, .054f, .12f, .04f), 18), face, Vector3.zero, Vector3.one);
                GPart(r, M.Cone(), k.bone, V(0, .055f, 0), V(.06f, .08f, .06f));                                                             // spike boss
                bossTop = .13f;
            }
            else
            {
                GPart(r, M.Lathe("buckler.boss", Pts(0, .045f, .056f, .045f, .05f, .07f, .026f, .09f, 0, .096f), 14, true), k.trim, Vector3.zero, Vector3.one);
                if (v == "tusk")   // two tusks across a barrel lid
                {
                    var tusk = M.Rod("buckler.tusk", new[] { V(-.15f, .05f, -.05f), V(-.02f, .1f, -.03f), V(.12f, .08f, .04f), V(.17f, .06f, .1f) }, .022f, .006f, 7, 10);
                    GPart(r, M.Many("buckler.tusks", tusk, M.At(Vector3.zero, Vector3.zero, Vector3.one), M.At(Vector3.zero, V(0, 180, 0), Vector3.one)), k.bone, Vector3.zero, Vector3.one);
                }
                else
                {
                    var rivets = new Matrix4x4[6]; for (int i = 0; i < 6; i++) { float a = i * 60 * Mathf.Deg2Rad; rivets[i] = M.At(V(Mathf.Cos(a) * .14f, .036f, Mathf.Sin(a) * .14f), Vector3.zero, S(.022f)); }
                    GPart(r, M.Many("buckler.rivets", Sphere, rivets), k.dark, Vector3.zero, Vector3.one);
                }
            }
            Extra(k, r, M.Lathe("buckler.boss.band", Pts(.055f, .05f, .066f, .05f, .066f, .062f, .055f, .062f, .055f, .05f), 16), Vector3.zero, Vector3.one);
            ShieldMarks(k, r, Surface, .14f, bossTop);
        }
        void RoundShield(GearKit k, Transform r)
        {
            string v = k.l.variant; float R = .29f, th = .03f, bulge = .028f; var c = Vector2.zero; var radius = new Vector2(R, R);
            var outline = Circle(R, 28); var surface = Dome(th, bulge, c, radius);
            var face = v == "hide" ? GearMats.Get(Color.Lerp(k.l.leather, k.l.cloth, .55f), .2f) : v == "lid" ? GearMats.Get(k.l.wood * .8f, .15f) : k.wood;
            GPart(r, M.Plate("round.face", outline, th, bulge, c, radius), face, Vector3.zero, Vector3.one);
            float faceTop = surface(0, 0);
            switch (v)
            {
                case "hide":   // stretched hide, a painted roundel, laced round the rim
                    GPart(r, Paint("round.roundel", Circle(.12f, 20), th, bulge, c, radius), k.cloth2, Vector3.zero, Vector3.one);
                    var laces = new Matrix4x4[14]; for (int i = 0; i < 14; i++) { float a = i * 360f / 14; laces[i] = M.At(V(Mathf.Cos(a * Mathf.Deg2Rad) * .262f, surface(.262f, 0) + .002f, Mathf.Sin(a * Mathf.Deg2Rad) * .262f), V(0, -a, 0), V(.03f, .012f, .012f)); }
                    GPart(r, M.Many("round.laces", Cube, laces), k.leather, Vector3.zero, Vector3.one);
                    GPart(r, M.Rim("round.rim.hide", outline, .022f, .012f, th / 2, bulge, c, radius), k.leather, Vector3.zero, Vector3.one);
                    break;
                case "cask":   // the end of a salt cask: staves, an iron hoop, the cooper's stamp
                    GPart(r, Paints("round.cask.seams", th, bulge, c, radius, Strip(-.27f, .27f, -.1f, .01f), Strip(-.28f, .28f, 0, .01f), Strip(-.27f, .27f, .1f, .01f)), k.dark, Vector3.zero, Vector3.one);
                    var stamp = Circle(.055f, 16); for (int i = 0; i < stamp.Length; i++) stamp[i] += new Vector2(-.12f, .06f);
                    GPart(r, Paint("round.cask.stamp", stamp, th, bulge, c, radius, .0025f), GearMats.Get(k.l.wood * .45f, .1f), Vector3.zero, Vector3.one);
                    GPart(r, M.Rim("round.rim.hoop", outline, .04f, .014f, th / 2, bulge, c, radius), k.metal, Vector3.zero, Vector3.one);
                    break;
                case "lid":   // a strongbox lid: iron straps and a lock plate
                    GPart(r, Paints("round.lid.straps", th, bulge, c, radius, Strip(-.24f, .24f, -.12f, .05f), Strip(-.24f, .24f, .12f, .05f)), k.metal, Vector3.zero, Vector3.one);
                    GPart(r, Paint("round.lid.lock", Pts(-.05f, -.04f, .05f, -.04f, .05f, .04f, -.05f, .04f), th, bulge, c, radius, .004f, .006f), k.trim, Vector3.zero, Vector3.one);
                    GBox(r, GearMats.Get(new Color(.05f, .04f, .04f)), V(0, faceTop + .004f, 0), V(.03f, .006f, .012f));                               // keyhole
                    GPart(r, M.Rim("round.rim", outline, .025f, .013f, th / 2, bulge, c, radius), k.metal, Vector3.zero, Vector3.one);
                    break;
                default:   // boards: three seams, two quarters painted, an iron boss
                    GPart(r, Paints("round.quarters", th, bulge, c, radius, Sector(0, .265f, 0, 90, 8), Sector(0, .265f, 180, 270, 8)), k.cloth, Vector3.zero, Vector3.one);
                    GPart(r, Paints("round.seams", th, bulge, c, radius, Strip(-.26f, .26f, -.1f, .008f), Strip(-.27f, .27f, 0, .008f), Strip(-.26f, .26f, .1f, .008f)), k.dark, Vector3.zero, Vector3.one);
                    GPart(r, M.Rim("round.rim", outline, .025f, .013f, th / 2, bulge, c, radius), k.metal, Vector3.zero, Vector3.one);
                    break;
            }
            if (v != "lid") GPart(r, M.Lathe("round.boss", Pts(0, .035f, .075f, .035f, .072f, .05f, .05f, .085f, 0, .1f), 16, true), v == "hide" ? k.metal : k.trim, Vector3.zero, Vector3.one);
            Extra(k, r, M.Lathe("round.boss.band", Pts(.074f, .038f, .086f, .038f, .086f, .05f, .074f, .05f, .074f, .038f), 16), Vector3.zero, Vector3.one);
            ShieldMarks(k, r, surface, .22f, v == "lid" ? faceTop + .012f : .1f);
        }
        static readonly Vector2[] HeaterOutline = Pts(.24f, -.21f, .24f, .21f, .06f, .21f, -.1f, .17f, -.22f, .09f, -.29f, 0, -.22f, -.09f, -.1f, -.17f, .06f, -.21f);
        void HeaterShield(GearKit k, Transform r)
        {
            string v = k.l.variant; float th = .028f, bulge = .03f; var c = Vector2.zero; var radius = new Vector2(.32f, .26f); var surface = Dome(th, bulge, c, radius);
            bool glass = v == "glass";
            GPart(r, M.Plate("heater.face", HeaterOutline, th, bulge, c, radius), glass ? GearMats.Glass(Color.Lerp(k.l.cloth, k.l.glow, .3f), .5f, k.l.glow * .5f) : k.cloth, Vector3.zero, Vector3.one);
            if (glass) GPart(r, M.Many("heater.leading", Cube, M.At(V(-.02f, surface(0, 0) - .004f, 0), Vector3.zero, V(.5f, .012f, .014f)), M.At(V(.06f, surface(.06f, 0) - .004f, 0), Vector3.zero, V(.014f, .012f, .4f))), k.metal, Vector3.zero, Vector3.one);   // the leading across the pane
            else
            {
                GPart(r, Paint("heater.chief", Pts(.236f, -.206f, .236f, .206f, .14f, .206f, .14f, -.206f), th, bulge, c, radius), k.cloth2, Vector3.zero, Vector3.one);
                if (v == "striped") GPart(r, Paint("heater.pale", Pts(.14f, -.035f, .14f, .035f, -.2f, .035f, -.255f, 0, -.2f, -.035f), th, bulge, c, radius), k.cloth2, Vector3.zero, Vector3.one);
                else GPart(r, Paint("heater.charge", Pts(.06f, 0, -.01f, .05f, -.08f, 0, -.01f, -.05f), th, bulge, c, radius, .003f, .004f), k.trim, Vector3.zero, Vector3.one);   // a lozenge
            }
            GPart(r, M.Rim("heater.rim", HeaterOutline, .022f, .011f, th / 2, bulge, c, radius), k.metal, Vector3.zero, Vector3.one);
            Extra(k, r, M.Many("heater.studs", Sphere, M.At(V(.21f, surface(.21f, .17f), -.17f), Vector3.zero, S(.022f)), M.At(V(.21f, surface(.21f, .17f), .17f), Vector3.zero, S(.022f)), M.At(V(-.23f, surface(-.23f, 0), 0), Vector3.zero, S(.022f))), Vector3.zero, Vector3.one);
            ShieldMarks(k, r, surface, .2f, surface(0, 0) + .012f);
        }
        static readonly Vector2[] KiteOutline = Pts(.24f, 0, .22f, .1f, .16f, .18f, .06f, .2f, -.08f, .17f, -.22f, .11f, -.34f, .05f, -.42f, 0, -.34f, -.05f, -.22f, -.11f, -.08f, -.17f, .06f, -.2f, .16f, -.18f, .22f, -.1f);
        void KiteShield(GearKit k, Transform r)
        {
            if (k.l.variant == "slab")   // a slab of slate on a strap
            {
                var slate = GearMats.Get(Color.Lerp(k.l.metal, new Color(.36f, .37f, .4f), .5f), .25f);
                GPart(r, M.Lump("kite.slab", 21, .45f, true), slate, V(-.05f, 0, 0), V(.66f, .08f, .42f));
                GBox(r, k.leather, V(.1f, .04f, 0), V(.07f, .02f, .44f));
                GBox(r, k.metal, V(.1f, .052f, .12f), V(.05f, .012f, .04f));
                Wear(k, r, V(-.2f, .035f, .08f), V(.06f, .01f, .04f));
                Accent(k, 1, r, M.Many("kite.cracks", Cube, M.At(V(-.06f, 0, 0), V(0, 35, 0), V(.16f, 1, .014f)), M.At(V(-.2f, 0, .05f), V(0, -20, 0), V(.12f, 1, .012f))), V(0, .038f, 0), V(1, .012f, 1));
                Accent(k, 2, r, Sphere, V(.1f, .055f, -.12f), S(.03f));
                return;
            }
            float th = .028f, bulge = .035f; var c = new Vector2(-.05f, 0); var radius = new Vector2(.4f, .23f); var surface = Dome(th, bulge, c, radius);
            GPart(r, M.Plate("kite.face", KiteOutline, th, bulge, c, radius), k.cloth, Vector3.zero, Vector3.one);
            GPart(r, Paint("kite.pale", Pts(.2f, -.04f, .2f, .04f, -.3f, .04f, -.37f, 0, -.3f, -.04f), th, bulge, c, radius), k.cloth2, Vector3.zero, Vector3.one);
            GPart(r, Paint("kite.rib", Pts(.21f, -.011f, .21f, .011f, -.39f, .011f, -.39f, -.011f), th, bulge, c, radius, .009f, .012f), k.metal, Vector3.zero, Vector3.one);
            GPart(r, M.Rim("kite.rim", KiteOutline, .022f, .011f, th / 2, bulge, c, radius), k.metal, Vector3.zero, Vector3.one);
            GPart(r, M.Lathe("kite.boss", Pts(0, .045f, .05f, .045f, .045f, .06f, 0, .08f), 14, true), k.trim, V(.02f, 0, 0), Vector3.one);
            Extra(k, r, M.Many("kite.studs", Sphere, M.At(V(.18f, surface(.18f, .13f), -.13f), Vector3.zero, S(.022f)), M.At(V(.18f, surface(.18f, .13f), .13f), Vector3.zero, S(.022f)), M.At(V(-.3f, surface(-.3f, 0) + .009f, 0), Vector3.zero, S(.022f))), Vector3.zero, Vector3.one);
            ShieldMarks(k, r, surface, .2f, .08f);
        }
        static readonly Vector2[] LeafOutline = Pts(.28f, 0, .2f, .1f, .08f, .18f, -.04f, .2f, -.16f, .16f, -.26f, .08f, -.32f, 0, -.26f, -.08f, -.16f, -.16f, -.04f, -.2f, .08f, -.18f, .2f, -.1f);
        void LeafShield(GearKit k, Transform r)
        {
            bool bark = k.l.variant == "bark";
            float th = .03f, bulge = .035f; var c = new Vector2(-.02f, 0); var radius = new Vector2(.32f, .22f); var surface = Dome(th, bulge, c, radius);
            var face = bark ? k.wood : k.metal; var vein = bark ? GearMats.Get(k.l.wood * .6f, .1f) : GearMats.Get(k.l.metal * .7f, .4f, .5f);
            GPart(r, M.Plate("leaf.face", LeafOutline, th, bulge, c, radius), face, Vector3.zero, Vector3.one);
            GPart(r, Paint("leaf.midrib", Pts(.26f, -.012f, .26f, .012f, -.3f, .012f, -.3f, -.012f), th, bulge, c, radius, .007f, .01f), bark ? GearMats.Get(Color.Lerp(k.l.cloth, new Color(.35f, .55f, .25f), .4f), .3f) : k.trim, Vector3.zero, Vector3.one);
            // Veins out from the midrib toward the tip, each ending well inside the edge (the leaf is about .2 (1 - ((x + .02) / .3)^2) across at x).
            var veins = new Vector2[8][];
            for (int i = 0; i < 4; i++)
            {
                float x = .16f - i * .12f, xe = x + .06f, len = .7f * .2f * (1 - (xe + .02f) * (xe + .02f) / .09f);
                veins[2 * i] = Pts(x - .008f, .01f, x + .008f, .01f, xe + .007f, len, xe - .007f, len);
                veins[2 * i + 1] = Pts(x - .008f, -.01f, xe - .007f, -len, xe + .007f, -len, x + .008f, -.01f);
            }
            GPart(r, Paints("leaf.veins", th, bulge, c, radius, veins), vein, Vector3.zero, Vector3.one);
            GPart(r, M.Rim("leaf.rim", LeafOutline, .016f, .01f, th / 2, bulge, c, radius), bark ? vein : k.trim, Vector3.zero, Vector3.one);
            if (bark) GPart(r, M.Many("leaf.moss", Sphere, M.At(V(-.2f, surface(-.2f, .09f), .09f), Vector3.zero, V(.07f, .02f, .05f)), M.At(V(.12f, surface(.12f, -.1f), -.1f), Vector3.zero, V(.05f, .018f, .04f))), GearMats.Get(new Color(.3f, .45f, .22f), .1f), Vector3.zero, Vector3.one);
            Extra(k, r, Sphere, V(-.02f, surface(-.02f, 0) + .006f, 0), V(.05f, .02f, .05f));
            ShieldMarks(k, r, surface, .2f, surface(-.02f, 0) + .016f);
        }

        // ---------- hung from the hand ----------
        void HungPiece(GearKit k, Transform r)
        {
            string v = k.l.variant;
            var flame = k.Lit(1.6f);
            switch (v)
            {
                case "censer":   // a perforated globe on a short chain, smouldering
                    GPart(r, M.Many("censer.chain", Cube, M.At(V(0, -.04f, 0), V(0, 0, 0), V(.012f, .04f, .006f)), M.At(V(0, -.09f, 0), V(0, 90, 0), V(.012f, .04f, .006f)), M.At(V(0, -.14f, 0), Vector3.zero, V(.012f, .04f, .006f)), M.At(V(0, -.19f, 0), V(0, 90, 0), V(.012f, .04f, .006f))), k.metal, Vector3.zero, Vector3.one);
                    GPart(r, M.Lathe("censer.globe", Pts(0, -.37f, .05f, -.36f, .08f, -.31f, .07f, -.25f, .04f, -.22f, 0, -.215f), 14, true), k.metal, Vector3.zero, Vector3.one);
                    var holes = new Matrix4x4[8]; for (int i = 0; i < 8; i++) { float a = i * 45 * Mathf.Deg2Rad; holes[i] = M.At(V(Mathf.Cos(a) * .071f, i % 2 == 0 ? -.3f : -.27f, Mathf.Sin(a) * .071f), Vector3.zero, S(.026f)); }
                    GPart(r, M.Many("censer.holes", Sphere, holes), flame, Vector3.zero, Vector3.one);
                    GBall(r, k.trim, V(0, -.21f, 0), S(.035f));
                    GBall(r, k.trim, V(0, -.375f, 0), V(.04f, .02f, .04f));
                    break;
                case "scale":   // a balance: a beam and two pans on strings
                    GRod(r, k.metal, -.11f, -.01f, .008f);
                    GBox(r, k.metal, V(0, -.11f, 0), V(.36f, .014f, .016f));
                    GPart(r, M.Many("scale.pans", M.Lathe("scale.pan", Pts(0, 0, .05f, .005f, .065f, .025f, .058f, .027f, 0, .008f), 14), M.At(V(-.17f, -.27f, 0), Vector3.zero, Vector3.one), M.At(V(.17f, -.27f, 0), Vector3.zero, Vector3.one)), k.trim, Vector3.zero, Vector3.one);
                    var strings = new Matrix4x4[6];
                    for (int i = 0; i < 6; i++) { float side = i < 3 ? -.17f : .17f, a = (i % 3) * 120 * Mathf.Deg2Rad; strings[i] = Stick(V(side, -.11f, 0), V(side + Mathf.Cos(a) * .058f, -.245f, Mathf.Sin(a) * .058f), .004f); }
                    GPart(r, M.Many("scale.strings", Cube, strings), GearMats.Get(new Color(.75f, .7f, .6f)), Vector3.zero, Vector3.one);
                    GBall(r, k.trim, V(0, -.1f, .012f), S(.03f));
                    break;
                default:   // a lantern: lantern (open cage), shuttered, or moss under glass
                    GPart(r, M.Rod("lantern.bail", new[] { V(0, -.13f, -.04f), V(0, -.01f, -.045f), V(0, -.01f, .045f), V(0, -.13f, .04f) }, .006f, .006f), k.dark, Vector3.zero, Vector3.one);
                    GPart(r, M.Cone(10), k.metal, V(0, -.15f, 0), V(.14f, .06f, .14f));
                    GPrim(PrimitiveType.Cylinder, r, k.metal, V(0, -.288f, 0), V(.13f, .01f, .13f));
                    if (v == "moss")   // lantern-moss under a glass dome
                    {
                        GPart(r, M.Lathe("lantern.dome", Pts(0, -.28f, .055f, -.28f, .055f, -.2f, .04f, -.165f, 0, -.155f), 14, true), GearMats.Glass(new Color(.8f, .9f, .85f), .3f, Color.black), Vector3.zero, Vector3.one);
                        GPart(r, M.Lump("lantern.moss", 6, .6f), GearMats.Get(Color.Lerp(k.l.cloth, new Color(.4f, .8f, .4f), .4f), .3f, 0, Color.Lerp(k.l.glow, new Color(.4f, 1, .5f), .5f) * (k.l.quality == 0 ? .6f : 1.1f)), V(0, -.255f, 0), V(.08f, .05f, .08f));
                    }
                    else
                    {
                        var bars = new Matrix4x4[4]; for (int i = 0; i < 4; i++) { float a = (45 + i * 90) * Mathf.Deg2Rad; bars[i] = M.At(V(Mathf.Cos(a) * .056f, -.218f, Mathf.Sin(a) * .056f), Vector3.zero, V(.012f, .135f, .012f)); }
                        GPart(r, M.Many("lantern.bars", Cube, bars), k.metal, Vector3.zero, Vector3.one);
                        GPart(r, M.Lathe("lantern.glass", Pts(0, -.284f, .05f, -.284f, .05f, -.152f, 0, -.152f), 12), GearMats.Glass(Color.Lerp(k.l.glow, Color.white, .5f), .3f, k.l.glow * .25f), Vector3.zero, Vector3.one);
                        GBall(r, flame, V(0, -.22f, 0), V(.05f, .07f, .05f));
                        if (v == "shuttered") GPart(r, M.Many("lantern.shutters", Cube, M.At(V(.058f, -.218f, 0), V(0, 0, 0), V(.006f, .125f, .06f)), M.At(V(-.05f, -.218f, .03f), V(0, -30, 0), V(.006f, .125f, .06f))), k.metal, Vector3.zero, Vector3.one);
                    }
                    break;
            }
            Extra(k, r, Sphere, V(0, v == "scale" ? -.004f : v == "censer" ? -.004f : -.09f, 0), S(.03f));
            Wear(k, r, V(.03f, -.17f, .03f), V(.03f, .02f, .03f));
            Accent(k, 1, r, Sphere, V(0, v == "scale" ? -.13f : v == "censer" ? -.39f : -.3f, 0), S(.028f));
            float gx = v == "censer" ? .082f : .05f;
            Accent(k, 2, r, M.Many("hung.gems." + v, Sphere, M.At(V(gx, 0, 0), Vector3.zero, S(.022f)), M.At(V(-gx, 0, 0), Vector3.zero, S(.022f))), V(0, v == "scale" ? -.11f : v == "censer" ? -.29f : -.155f, 0), Vector3.one);
        }
    }
}
