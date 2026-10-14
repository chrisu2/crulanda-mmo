using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Beasts as models (2026-10-03; <see cref="ModelBeast"/>): wolves on the pack's wolf, grey (darker in Khaven's dusk), and
    /// ash hounds (variant odd) on it too, charcoal with eyes like embers; the forest stag on its stag, and the doe (variant odd)
    /// on its deer. The model plays its own walk and gallop with the ground, grazes or looks about while it stands, puts its head
    /// down while it lies in wait, attacks as its blow lands (<see cref="Strike"/>), flinches when struck (<see cref="Flinch"/>)
    /// and falls in its own death where it stood (the body is not tipped over). Boars, spiders, brambles and Weave-Eaters keep
    /// their own bodies (the pack has none of them).
    /// </summary>
    public sealed partial class ActorVisual
    {
        ModelBeast beastModel;
        /// <summary>The beast's model (null unless this is a modelled beast).</summary>
        public ModelBeast BeastModel { get { return beastModel; } }
        EncounterEnemy enemyRef; bool hitLeft; float beastPhase;
        /// <summary>The pack's animal a beast wears: the wolf for wolves and ash hounds; the stag, or the deer for a doe.</summary>
        static string BeastKind(ActorLook look, int variant)
        {
            switch (look)
            {
                case ActorLook.Wolf: return "Wolf";
                case ActorLook.Stag: return variant % 2 == 1 ? "Deer" : "Stag";
                case ActorLook.Boar: return "Boar";
                case ActorLook.Bear: return "Bear";         // Blink's stylized bear (Asset Store)
                case ActorLook.RockEater: return "Bear";    // the Rock-Eater: the bear's body, grown and turned to stone (BuildModelBeast)
                case ActorLook.Spider: return "Spider";     // ChillLands' crypt spider (Ashen Marches)
                case ActorLook.Skeleton: return "Skeleton"; // ChillLands' ossuary knight: the risen dead
                default: return null;
            }
        }
        /// <summary>
        /// A Veridian Keeper as a treant (2026-10-04: Chris, "we need some better models for the tree people"; Tennessippi Studios'
        /// Treant Pack, CC0, Resources/Creatures/Treant1-2 and Resources/CreatureSkins/Treant): the slighter one or the broader by its
        /// name (the great withered ones, Greyheart and the Root-Warden, the broader and taller; the withered of the camps the
        /// slighter). Each kind its own bark (Chris: "color them differently based on their type"; tools/wip/treants/skins.py):
        /// a living Keeper the pack's warm dark bark, a faint green sap-light in its moss; the Greying's withered (variant odd) ash
        /// grey, the light gone out of them; Greyheart bleached to bone, a cold pale light in it; the withered down in the deep under
        /// the Temple blackened, violet in the moss; the Hollow Root-Warden rotted black, the void burning violet in it.
        /// </summary>
        string KeeperKind()
        {
            string n = name ?? "";
            if (n.StartsWith("Greyheart") || n.Contains("Root-Warden")) return "Treant2";
            if (variant % 2 == 1) return "Treant1";
            return (FigureSeed() & 2) == 0 ? "Treant1" : "Treant2";
        }
        ModelBeast.Coat KeeperBark(string kind)
        {
            string n = name ?? ""; bool withered = variant % 2 == 1;
            var up = transform.position + Vector3.up;
            bool deep = Crulanda.World.Hollow.All.Count > 0 && (Crulanda.World.Hollow.DepthAt(up) > .2f || Crulanda.World.Hollow.InsideAny(up, .5f));
            string bark; Color tint = Color.white, glow;
            if (n.Contains("Root-Warden")) { bark = "_Rot"; tint = new Color(.78f, .72f, .82f); glow = new Color(.7f, .3f, 1) * 2.4f; }
            else if (n.StartsWith("Greyheart")) { bark = "_Grey"; tint = new Color(.95f, .97f, 1); glow = new Color(.6f, .8f, 1) * 1.8f; }
            else if (withered && deep) { bark = "_Rot"; glow = new Color(.6f, .25f, .9f) * 1.3f; }
            else if (withered) { bark = "_Grey"; tint = new Color(.8f, .81f, .84f); glow = Color.black; }
            else { bark = "_Dark"; glow = new Color(.25f, .95f, .5f) * .9f; }
            var skin = Resources.Load<Texture2D>("CreatureSkins/Treant/" + kind + bark);
            if (skin == null) return null;
            return new ModelBeast.Coat().Skin(skin, Resources.Load<Texture2D>("CreatureSkins/Treant/" + kind + "_Normal"), tint)
                .Lit(Resources.Load<Texture2D>("CreatureSkins/Treant/" + kind + "_Glow"), glow);
        }
        /// <summary>The whole-orc model for an orc of this look: the Orc Shaman for the casters (the Shaman, in the Druid's look, and the
        /// Mage), the stylized axe orc for the Warrior, the Ranger and the Rogue; null without them (the orc's head on the people's body).</summary>
        public static string OrcKind(ActorLook look)
        {
            if ((look == ActorLook.Druid || look == ActorLook.Mage) && ModelBeast.Available("OrcShaman")) return "OrcShaman";
            if ((look == ActorLook.Warrior || look == ActorLook.Ranger) && ModelBeast.Available("OrcBrute")) return "OrcBrute";   // the Warrior, the Ranger and the Rogue: the axe orc
            return null;
        }
        /// <summary>The Orc Shaman's skin, from the pack's: mostly greens (CANON: orcs are green), now and then the grey or the dark; its staff's own map.</summary>
        ModelBeast.Coat OrcCoat()
        {
            string[] skins = { "Green03", "Green", "Green02", "Green03", "Grey", "Green", "Dark" };
            var skin = Resources.Load<Texture2D>("CreatureSkins/OrcShaman/Skin_" + skins[Mathf.Abs(variant) % skins.Length]) ?? Resources.Load<Texture2D>("CreatureSkins/OrcShaman/Skin_Green03");
            // By the file's own material names (the pack's prefab gives them these maps): _Armas1 its staff, _CueroMAT and _CueroMAT2 its
            // leathers, the rest (phong2, lambert11) its skin, eyes and teeth.
            return new ModelBeast.Coat().Map("Armas", Resources.Load<Texture2D>("CreatureSkins/OrcShaman/Weapon")).Map("CueroMAT2", Resources.Load<Texture2D>("CreatureSkins/OrcShaman/Leather02"))
                .Map("Cuero", Resources.Load<Texture2D>("CreatureSkins/OrcShaman/Leather01")).Map("", skin);
        }
        /// <summary>Builds a wolf, ash hound, stag or doe as a model (when models are on and the animal is in the build); false
        /// leaves it to the old body.</summary>
        bool BuildModelBeast(ActorLook look)
        {
            string kind = orc && OrcKind(look) != null ? OrcKind(look) : look == ActorLook.Keeper ? KeeperKind() : BeastKind(look, variant);
            if ((!Models && look != ActorLook.Keeper) || !ModelBeast.Available(kind)) return false;   // (a Keeper has no other body)
            ModelBeast.Coat coat = null; float height, walk, run;
            bool elite = name != null && name.EndsWith("(elite)");   // a camp's elite (EncounterSession.SpawnCamps): a size bigger than its pack
            if (look == ActorLook.Wolf)
            {
                // Khaven's dusk (gloom): darker hides, or the wolves vanish into its pale earth and dry grass (as the old body did).
                var zone = Crulanda.World.ZoneBuilder.Active; bool gloom = zone != null && zone.Zone != null && zone.Zone.biome == "gloom";
                // The Mage's ember hound (the Cinderwright, GAME-ONLY): a fire spirit in a hound's shape, charred black with its light parts
                // glowing like coals and its eyes alight.
                if (name != null && name.StartsWith("Ember hound"))
                    coat = new ModelBeast.Coat().Set("Main", new Color(.13f, .06f, .04f)).Shine("Main_Light", new Color(1, .42f, .1f), new Color(1, .32f, .06f) * 1.6f)
                        .Shine("Eyes_Black", new Color(1, .85f, .3f), new Color(1, .7f, .2f) * 2.5f);
                else if (variant % 2 == 1)
                    coat = new ModelBeast.Coat().Set("Main", new Color(.17f, .16f, .16f)).Set("Main_Light", new Color(.31f, .29f, .28f)).Set("Nose", new Color(.05f, .05f, .05f))
                        .Shine("Eyes_Black", new Color(1, .45f, .15f), new Color(1, .4f, .1f) * 2);
                // Old Whitefoot of the Old Fold (GAME-ONLY): grizzled with age, his muzzle, chest and feet gone white.
                else if (name != null && name.StartsWith("Old Whitefoot")) coat = new ModelBeast.Coat().Set("Main", new Color(.5f, .49f, .46f)).Set("Main_Light", new Color(.92f, .91f, .87f));
                else if (gloom) coat = new ModelBeast.Coat().Set("Main", new Color(.28f, .26f, .25f)).Set("Main_Light", new Color(.45f, .42f, .39f));
                else coat = new ModelBeast.Coat().Set("Main", new Color(.4f, .39f, .37f)).Set("Main_Light", new Color(.64f, .62f, .58f));   // a grey wolf (the file's is near white)
                height = elite ? 1.12f : 1; walk = 1.2f; run = 5; LieDepth = .3f;
            }
            else if (look == ActorLook.Boar)
            {
                // CraftPix's boar (moved by the code: ModelBeast.Proc.cs), its hide by its kind: the carrion boars ashen, the mire boars
                // muddy, the Peaks' rockhides stone grey; darker in Khaven's dusk.
                var zone = Crulanda.World.ZoneBuilder.Active; bool gloom = zone != null && zone.Zone != null && zone.Zone.biome == "gloom";
                // (The palette's boar is a pale farm pig's beige: a wild boar is dark, grey-brown.)
                string n = name ?? ""; Color hide = new Color(.56f, .49f, .43f);
                if (n.Contains("Carrion")) hide = new Color(.66f, .62f, .6f);
                else if (n.Contains("Mire")) hide = new Color(.5f, .5f, .4f);
                else if (n.Contains("Rockhide") || n.Contains("Scree")) hide = new Color(.6f, .6f, .64f);
                if (gloom) hide *= .8f;
                coat = new ModelBeast.Coat().Set("Wild_animals_map", new Color(hide.r, hide.g, hide.b, 1));
                height = elite ? 1.1f : .95f; walk = 1.1f; run = 4.2f; LieDepth = .2f;
            }
            else if (look == ActorLook.Keeper)
            {
                bool great = (name ?? "").StartsWith("Greyheart") || (name ?? "").Contains("Root-Warden");
                coat = KeeperBark(kind); if (coat == null) return false;
                height = (kind == "Treant2" ? 3f : 2.7f) * (great ? 1.25f : elite ? 1.12f : 1); walk = 2f; run = 99; LieDepth = 0;
            }
            else if (look == ActorLook.Bear)
            {
                // A brown bear of the woods (Oakhaven), or a mountain bear of the Peaks, its coat greyed with the scree and the cold;
                // an old one a size bigger and grizzled.
                var zone = Crulanda.World.ZoneBuilder.Active; bool mountain = zone != null && zone.Zone != null && zone.Zone.biome == "mountain";
                var skin = Resources.Load<Texture2D>("CreatureSkins/Bear/Bear_Albedo"); if (skin == null) return false;
                bool old = (name ?? "").StartsWith("Old ");
                coat = new ModelBeast.Coat().Skin(skin, Resources.Load<Texture2D>("CreatureSkins/Bear/Bear_Normal"), old ? new Color(.86f, .84f, .8f) : mountain ? new Color(.72f, .7f, .7f) : Color.white);
                height = elite || old ? 1.75f : 1.45f; walk = 1.3f; run = 5; LieDepth = .4f;
            }
            else if (look == ActorLook.RockEater)
            {
                // The Rock-Eater (GAME-ONLY; Chris, 2026-10-11: no machines, "a great burrowing beast" Nix rides and drives): a cave beast
                // on the bear's body, two and a half times its size, its hide gone the grey of the rock it eats, the violet of the geodes
                // it swallows growing out through its back (RockEaterGear), a saddle on it.
                var skin = Resources.Load<Texture2D>("CreatureSkins/Bear/Bear_Albedo"); if (skin == null) return false;
                coat = new ModelBeast.Coat().Skin(skin, Resources.Load<Texture2D>("CreatureSkins/Bear/Bear_Normal"), new Color(.52f, .5f, .56f));
                height = RockEaterHeight; walk = 1.1f; run = 4; LieDepth = 0;
            }
            else if (look == ActorLook.Spider)
            {
                // The crypt spider in the colours of where it lives: Khaven's charnel spiders bone and ash, the ridge's basalt
                // spiders black stone, the canopy spiders moss green (tools/wip/animals/palettes.py).
                string n = name ?? "", tint = n.Contains("Charnel") ? "_charnel" : n.Contains("Basalt") ? "_basalt" : n.Contains("Canopy") ? "_canopy" : "";
                var pal = Resources.Load<Texture2D>("Creatures/Spider_palette" + tint) ?? Resources.Load<Texture2D>("Creatures/Spider_palette");
                if (pal != null) coat = new ModelBeast.Coat().Skin(pal, null, Color.white);
                height = elite ? .95f : .8f; walk = 1.4f; run = 4.5f; LieDepth = .15f;
            }
            else if (kind == "OrcBrute")
            {
                // The stylized orc of the first pack, whole, with its axe, on its own clips (idle, walk, run, two blows, a death); its pink
                // skin is the green of Resources/Characters/Orc/OrcSkin (recoloured: canon orcs are green).
                coat = new ModelBeast.Coat().Map("Axe", Resources.Load<Texture2D>("CreatureSkins/OrcBrute/Axe")).Map("", Resources.Load<Texture2D>("Characters/Orc/OrcSkin"));
                height = 2.1f + Mathf.Abs(variant % 5) * .02f; walk = 1.4f; run = 5.5f; LieDepth = 0;
            }
            else if (kind == "OrcShaman")
            {
                // A whole orc (Chris, 2026-10-13: "we had whole orc assets", "that was the whole point of adding those asset"): the pack's
                // Orc Shaman in its own robes and with its own staff, on its own clips; the canon's orcs stand about seven feet.
                coat = OrcCoat(); height = 2.08f + Mathf.Abs(variant % 5) * .02f; walk = 1.3f; run = 5.5f; LieDepth = 0;
            }
            else if (look == ActorLook.Skeleton)
            {
                // Khaven's drowned dead: bone stained by the creek mud they rose out of.
                var pal = Resources.Load<Texture2D>("Creatures/Skeleton_palette");
                if (pal != null) coat = new ModelBeast.Coat().Skin(pal, null, new Color(.84f, .86f, .76f));
                height = elite ? 1.95f : 1.82f; walk = 1.2f; run = 3.6f; LieDepth = 0;
            }
            else { bool doe = variant % 2 == 1; height = (doe ? 1.6f : 1.9f) * (elite ? 1.1f : 1); walk = 1.4f; run = 4.5f; LieDepth = .45f; }
            int seed = FigureSeed(); float shade = .94f + Mathf.Abs(seed % 13) * .01f; beastPhase = (seed & 255) * .37f;
            beastModel = ModelBeast.Build(body, kind, -1, height, coat, shade);
            if (beastModel == null) return false;
            beastModel.WalkPace = walk; beastModel.RunPace = run;
            beastModel.DeathSlot = Mathf.Abs(seed % 3) == 0 ? "death" : Mathf.Abs(seed % 3) == 1 ? "death2" : "death3";
            beastModel.Crumbles = look == ActorLook.Skeleton;   // the drowned dead fall apart into a heap of bones (ModelBeast.Crumble.cs)
            beast = true;
            beastModel.StartMotion(name + "#" + variant);
            if (look == ActorLook.RockEater)
            {
                // Its bones always posed, seen or not: it carries a rider and its gear on its back, and a bear's animator stops posing what
                // no camera sees, so in the game (where it stands out of sight when the Adit is built) its gear was fitted to the file's rest
                // pose and swung to its mouth when the idle took over (2026-10-11 captures; the editor's tests, with no camera, never saw it).
                if (beastModel.Animator != null) beastModel.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                // A seat to ride on at once (Nix climbs in as the camp spawns), then the gear fitted two frames on, on its idle as it stands.
                RiderSeat = new GameObject("Rider seat").transform; RiderSeat.SetParent(body, false); RiderSeat.localPosition = Vector3.up * RockEaterHeight * .8f;
                if (Application.isPlaying) rockGearAt = Time.frameCount + 2; else RockEaterGear();
            }
            return true;
        }
        int rockGearAt = -1;
        /// <summary>Whether the Rock-Eater's gear is on its back yet (fitted two frames after it is built).</summary>
        public bool GearFitted { get; private set; }
        /// <summary>How tall the Rock-Eater stands (a bear is 1.45, an old one 1.75).</summary>
        public const float RockEaterHeight = 3.6f;
        /// <summary>Where its rider sits (Nix, EncounterEnemy.Ride): on the saddle, facing the way it faces; null for any other figure.</summary>
        public Transform RiderSeat { get; private set; }
        /// <summary>The Rock-Eater's back, fitted to its hide as its idle stands it and carried by its spine: a ridge of violet geode crystal out
        /// through the stone of its hide (the geodes it eats), glowing faintly; a saddle on a blanket, its girth down both flanks; and the seat.</summary>
        void RockEaterGear()
        {
            // Posed first and measured off the hide itself (2026-10-11 captures: set by the bear's rest pose and its back bone's place, the
            // saddle came out at its mouth, a girth strap stood up off its back like a pole and the crystals were at its tail).
            beastModel.Settle();
            GearFitted = true;
            var hide = new HideSurface(beastModel.Renderers, beastModel.Height);
            if (hide.Empty) return;
            Vector3 up = body.up, ahead = body.forward, side = body.right, o = body.position;
            float back = float.MaxValue, front = float.MinValue, left = float.MaxValue, right = float.MinValue;
            foreach (var v in hide.Points) { var d = v - o; float f = Vector3.Dot(d, ahead), x = Vector3.Dot(d, side); back = Mathf.Min(back, f); front = Mathf.Max(front, f); left = Mathf.Min(left, x); right = Mathf.Max(right, x); }
            float length = front - back, mid = (left + right) / 2;
            Vector3 On(float along, float across) { return o + ahead * along + side * (mid + across); }
            float TopAt(Vector3 at) { return hide.TopAt(at) ?? o.y + RockEaterHeight * .7f; }
            // Carried by the nearest bone of its spine (not the head's or the neck's, which turn as it looks about), so all of it moves with
            // the back as it walks and breathes.
            var spine = new System.Collections.Generic.List<Transform>();
            for (var bone = beastModel.Head != null ? beastModel.Head.parent : null; bone != null && bone != beastModel.Model; bone = bone.parent)
                if (bone.name.IndexOf("Neck", System.StringComparison.OrdinalIgnoreCase) < 0) spine.Add(bone);
            if (spine.Count == 0 && beastModel.Hips != null) spine.Add(beastModel.Hips);
            Transform OnBack(Transform t, string what)
            {
                t.name = what; Transform best = body; float near = float.MaxValue;
                foreach (var bone in spine) { float d = (bone.position - t.position).sqrMagnitude; if (d < near) { near = d; best = bone; } }
                t.SetParent(best, true); return t;
            }
            var crystal = Mat(new Color(.62f, .4f, .95f), .9f, .2f); crystal.EnableKeyword("_EMISSION"); crystal.SetColor("_EmissionColor", new Color(.55f, .25f, 1) * 1.1f);
            var leather = Mat(new Color(.3f, .19f, .11f), .1f, .3f); var blanket = Mat(new Color(.42f, .12f, .1f), .05f, .1f);
            // The saddle a little behind the middle of the hide's length (the head and neck make the front quarter): behind the shoulders' hump.
            float seatAlong = back + length * .46f; var seatAt = On(seatAlong, 0); seatAt.y = TopAt(seatAt);
            // The ridge of crystal down the spine behind the saddle, biggest first and smaller toward the rump, leaning out a little either
            // way, each sunk into the hide where it stands; and a few small shards out of the shoulders to the sides (none in front of the rider).
            for (int k = 0; k < 7; k++)
            {
                float along = seatAlong - .8f - k * length * .045f, size = 1 - k * .1f; if (along < back + length * .1f) break;
                var foot = On(along, k % 2 == 0 ? .08f : -.08f); foot.y = TopAt(foot) - .12f * size;
                var c = Part(PrimitiveType.Cube, body, Vector3.zero, new Vector3(.16f * size, .75f * size, .16f * size), crystal);
                var lean = Quaternion.AngleAxis((k % 2 == 0 ? -1 : 1) * (10 + k * 3), ahead) * Quaternion.AngleAxis(-12, side);
                c.rotation = lean * Quaternion.AngleAxis(45, up) * body.rotation; c.position = foot + c.up * .375f * size; OnBack(c, "Geode crystal");
            }
            foreach (int s in new[] { -1, 1 })
                for (int k = 0; k < 2; k++)
                {
                    var foot = On(seatAlong + .75f + k * .3f, s * (.32f + k * .1f)); foot.y = TopAt(foot) - .08f;
                    var c = Part(PrimitiveType.Cube, body, Vector3.zero, new Vector3(.1f, .4f - k * .1f, .1f), crystal);
                    c.rotation = Quaternion.AngleAxis(s * -35, ahead) * Quaternion.AngleAxis(30, up) * body.rotation; c.position = foot + c.up * (.2f - k * .05f); OnBack(c, "Geode crystal");
                }
            // The saddle: a blanket, the seat and its cantle.
            var pad = Part(PrimitiveType.Cube, body, Vector3.zero, new Vector3(1.1f, .06f, 1.1f), blanket); pad.position = seatAt + up * .01f; pad.rotation = body.rotation; OnBack(pad, "Saddle blanket");
            var seat = Part(PrimitiveType.Cube, body, Vector3.zero, new Vector3(.7f, .16f, .8f), leather); seat.position = seatAt + up * .1f; seat.rotation = body.rotation; OnBack(seat, "Saddle");
            var cantle = Part(PrimitiveType.Cube, body, Vector3.zero, new Vector3(.7f, .3f, .1f), leather); cantle.position = seatAt + up * .22f - ahead * .38f; cantle.rotation = body.rotation; OnBack(cantle, "Cantle");
            // The girth down each flank, strap by strap over the hide's own curve (each strap from one point on the hide to the next, found
            // by a ray in from the side): from just under the blanket down to where the belly turns under.
            float halfWidth = (right - left) / 2;
            foreach (int s in new[] { -1, 1 })
            {
                Vector3? from = null;
                for (float drop = .1f; drop < RockEaterHeight * .45f; drop += .22f)
                {
                    float level = seatAt.y - drop;
                    var outside = On(seatAlong, s * (halfWidth + 2)); outside.y = level;
                    var met = hide.Cast(outside, -side * s);
                    if (!met.HasValue || (Vector3.Dot(met.Value - o, side) - mid) * s < .2f) break;   // under the belly, or between the legs
                    var to = met.Value + side * s * .03f;
                    if (from.HasValue)
                    {
                        var span = to - from.Value; if (span.sqrMagnitude < .0004f) continue;
                        var strap = Part(PrimitiveType.Cube, body, Vector3.zero, new Vector3(.06f, span.magnitude + .04f, .14f), leather);
                        strap.position = (from.Value + to) / 2; strap.rotation = Quaternion.LookRotation(ahead, span.normalized); OnBack(strap, "Girth");
                    }
                    from = to;
                }
            }
            Debug.Log("ROCKEATER_GEAR hide " + hide.Points.Length + " points (bake scaled " + hide.Corrected.ToString("0.00") + "), length " + length.ToString("0.00") + " (back " + back.ToString("0.00") + ", front " + front.ToString("0.00") + "), seat " + (seatAt - o).ToString("F2") + " from its body, on " + seat.parent.name + ", frame " + Time.frameCount + ", cull " + (beastModel.Animator != null ? beastModel.Animator.cullingMode.ToString() : "-"));
            if (RiderSeat == null) RiderSeat = new GameObject("Rider seat").transform;
            RiderSeat.SetParent(seat.parent, true); RiderSeat.position = seatAt + up * .18f; RiderSeat.rotation = body.rotation;
        }
        /// <summary>A modelled beast's frame: its pace from how far it went; dead, its death (the body EncounterEnemy tipped over
        /// set upright again); lying in wait, head down; standing, it grazes (deer) or looks about (wolves), or watches what it
        /// fights.</summary>
        void BeastLate()
        {
            if (rockGearAt >= 0 && Time.frameCount >= rockGearAt) { rockGearAt = -1; RockEaterGear(); }   // posed by its idle now (LateUpdate)
            float dt = Time.deltaTime;
            var delta = transform.position - lastPosition; delta.y = 0; lastPosition = transform.position;
            float target = dt > 0 ? delta.magnitude / dt : 0;
            speed = Mathf.Lerp(speed, target, dt * 8);
            if (actorRef == null) actorRef = GetComponent<Crulanda.Gameplay.Actor>();
            if (actorRef != null && !actorRef.IsAlive)
            {
                body.localRotation = Quaternion.identity; body.localPosition = Vector3.zero;
                beastModel.Die(); beastModel.Drive(0, "idle"); return;
            }
            if (beastModel.Dead) beastModel.Revive();
            if (enemyRef == null) enemyRef = GetComponent<EncounterEnemy>();
            bool fighting = enemyRef != null && enemyRef.Engaged;
            float t = Time.time + beastPhase;
            string rest = LyingLow ? "headlow" : fighting ? "idle" : beastModel.Kind == "Wolf" ? (Mathf.Sin(t * .21f) > .6f ? "idle2" : "idle") : (Mathf.Sin(t * .3f) > 0 ? "eat" : "idle");
            beastModel.Drive(LyingLow ? 0 : speed, rest);
        }
        /// <summary>A blow as it lands (EncounterEnemy): a modelled beast attacks (a wolf bites, a stag drives its antlers, a doe kicks).</summary>
        public void Strike()
        {
            if (beastModel != null)
            {
                // A treant's blows go round its three (a sweep, an overhead smash, a jab with the branch).
                if (beastModel.Has("attack2")) { blow = (blow + 1) % 3; beastModel.Play(blow == 0 ? "attack" : blow == 1 ? "attack2" : "attack3"); return; }
                beastModel.Play(beastModel.Kind == "Deer" ? "kick" : "attack"); return;
            }
            // A modelled person (playtest note 25): a sword's swing armed (anything held), a jab or a cross bare-handed.
            if (model != null) model.Act(held != null && held.Length > 0 && !gearStowed ? "swing" : (punchLeft = !punchLeft) ? "jab" : "cross");
        }
        bool punchLeft; int blow;
        /// <summary>A thrust whatever the hands hold (the Ranger's spear work, the bow slung: the jab and the cross in turn).</summary>
        public void Thrust() { if (model != null) model.Act((punchLeft = !punchLeft) ? "jab" : "cross"); else Strike(); }
        /// <summary>A spell let fly (an instant one, or a cast as it completes): the release over the walk.</summary>
        public void CastRelease() { if (BowReady) { Shoot(); return; } if (model != null) model.Act("castshot"); else if (beastModel != null && beastModel.Has("cast")) beastModel.Play("cast"); }   // a bow looses an arrow (and comes out to)
        /// <summary>A cast being drawn: the spell pose held over the walk.</summary>
        bool castingNow;
        public bool Casting { get { return castingNow; } set { castingNow = value; bool bow = value && BowReady; drawing = bow; if (model != null) model.Casting = value && !bow; } }   // a bow is drawn, not a spell
        /// <summary>Struck (EncounterEnemy.Receive): a modelled beast flinches, one way then the other, unless it is mid-attack.</summary>
        public void Flinch()
        {
            if (beastModel != null) { if (!beastModel.Busy && !beastModel.Dead) beastModel.Play((hitLeft = !hitLeft) ? "hitL" : "hitR"); return; }
            if (model != null) model.Act((hitLeft = !hitLeft) ? "hit" : "hithead");
        }
        /// <summary>Edit mode (captures, tests): a modelled beast posed <paramref name="time"/> seconds into one of its motions.</summary>
        public void BeastPreview(string slot, float time) { if (beastModel != null) beastModel.Sample(slot, time); }
    }

    /// <summary>A baked hide's surface to measure against (the Rock-Eater's saddle, RockEaterGear, and its test): rays on its own
    /// triangles, as it stands now. (The vertices of a low-poly hide grown two and a half times lie too far apart to measure by: 2026-10-11.)</summary>
    public sealed class HideSurface
    {
        public readonly Vector3[] Points; readonly int[] tris;
        /// <summary>How much the bake had to be scaled to stand at the beast's own height (1 when it came out right).</summary>
        public readonly float Corrected = 1;
        /// <param name="height">The beast's height (ModelBeast.Height), feet to the top: the bake is scaled to it. In the editor a bake
        /// comes out without the model's scale and in the game with it (2026-10-11: the game's Rock-Eater measured two and a half times
        /// its size, 13.5 m long, so its saddle went where its mouth is), so neither is trusted: the hide is made the height it is built to.</param>
        public HideSurface(System.Collections.Generic.IEnumerable<Renderer> renderers, float height = 0)
        {
            var pts = new System.Collections.Generic.List<Vector3>(); var tri = new System.Collections.Generic.List<int>();
            var spans = new System.Collections.Generic.List<(Vector3 pivot, int from, int to)>(); var scales = new System.Collections.Generic.List<float>();
            foreach (var r in renderers)
            {
                if (!(r is SkinnedMeshRenderer sk) || sk.sharedMesh == null) continue;
                var baked = new Mesh(); sk.BakeMesh(baked); var toWorld = sk.transform.localToWorldMatrix; int first = pts.Count;   // baked unscaled (in the editor), then its whole transform
                foreach (var v in baked.vertices) pts.Add(toWorld.MultiplyPoint3x4(v));
                foreach (var i in baked.triangles) tri.Add(first + i);
                spans.Add((sk.transform.position, first, pts.Count)); scales.Add(sk.transform.lossyScale.x);
                if (Application.isPlaying) UnityEngine.Object.Destroy(baked); else UnityEngine.Object.DestroyImmediate(baked);
            }
            if (height > 0 && pts.Count > 0)
            {
                float low = float.MaxValue, high = float.MinValue; foreach (var q in pts) { low = Mathf.Min(low, q.y); high = Mathf.Max(high, q.y); }
                float k = high > low ? height / (high - low) : 1;
                if (k < .75f || k > 1.33f)   // a pose stands a little lower or higher than its build; a bake scaled twice is far out
                {
                    float own = scales.Count > 0 ? scales[0] : 1; if (own > 0 && Mathf.Abs(k * own - 1) < .15f) k = 1 / own;   // its scale taken twice
                    Corrected = k;
                    foreach (var (pivot, from, to) in spans) for (int i = from; i < to; i++) pts[i] = pivot + (pts[i] - pivot) * k;
                }
            }
            Points = pts.ToArray(); tris = tri.ToArray();
        }
        public bool Empty { get { return Points.Length == 0; } }
        /// <summary>Where a ray first meets the hide (<paramref name="dir"/> of unit length), or null.</summary>
        public Vector3? Cast(Vector3 from, Vector3 dir)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                Vector3 a = Points[tris[i]], e1 = Points[tris[i + 1]] - a, e2 = Points[tris[i + 2]] - a, p = Vector3.Cross(dir, e2);
                float det = Vector3.Dot(e1, p); if (det > -1e-8f && det < 1e-8f) continue;
                float inv = 1 / det; Vector3 t = from - a; float u = Vector3.Dot(t, p) * inv; if (u < 0 || u > 1) continue;
                Vector3 q = Vector3.Cross(t, e1); float v = Vector3.Dot(dir, q) * inv; if (v < 0 || u + v > 1) continue;
                float d = Vector3.Dot(e2, q) * inv; if (d > 0 && d < best) best = d;
            }
            return best < float.MaxValue ? from + dir * best : (Vector3?)null;
        }
        /// <summary>The top of the hide over a point (straight down from well above it), or null off it.</summary>
        public float? TopAt(Vector3 at) { var hit = Cast(new Vector3(at.x, at.y + 20, at.z), Vector3.down); return hit.HasValue ? hit.Value.y : (float?)null; }
    }
}
