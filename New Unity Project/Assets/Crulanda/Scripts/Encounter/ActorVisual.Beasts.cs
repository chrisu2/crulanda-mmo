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
        /// <summary>Builds a wolf, ash hound, stag or doe as a model (when models are on and the animal is in the build); false
        /// leaves it to the old body.</summary>
        bool BuildModelBeast(ActorLook look)
        {
            string kind = look == ActorLook.Keeper ? KeeperKind() : BeastKind(look, variant);
            if ((!Models && look != ActorLook.Keeper) || !ModelBeast.Available(kind)) return false;   // (a Keeper has no other body)
            ModelBeast.Coat coat = null; float height, walk, run;
            bool elite = name != null && name.EndsWith("(elite)");   // a camp's elite (EncounterSession.SpawnCamps): a size bigger than its pack
            if (look == ActorLook.Wolf)
            {
                // Khaven's dusk (gloom): darker hides, or the wolves vanish into its pale earth and dry grass (as the old body did).
                var zone = Crulanda.World.ZoneBuilder.Active; bool gloom = zone != null && zone.Zone != null && zone.Zone.biome == "gloom";
                if (variant % 2 == 1)
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
            else if (look == ActorLook.Spider)
            {
                // The crypt spider in the colours of where it lives: Khaven's charnel spiders bone and ash, the ridge's basalt
                // spiders black stone, the canopy spiders moss green (tools/wip/animals/palettes.py).
                string n = name ?? "", tint = n.Contains("Charnel") ? "_charnel" : n.Contains("Basalt") ? "_basalt" : n.Contains("Canopy") ? "_canopy" : "";
                var pal = Resources.Load<Texture2D>("Creatures/Spider_palette" + tint) ?? Resources.Load<Texture2D>("Creatures/Spider_palette");
                if (pal != null) coat = new ModelBeast.Coat().Skin(pal, null, Color.white);
                height = elite ? .95f : .8f; walk = 1.4f; run = 4.5f; LieDepth = .15f;
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
            return true;
        }
        /// <summary>A modelled beast's frame: its pace from how far it went; dead, its death (the body EncounterEnemy tipped over
        /// set upright again); lying in wait, head down; standing, it grazes (deer) or looks about (wolves), or watches what it
        /// fights.</summary>
        void BeastLate()
        {
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
        /// <summary>A spell let fly (an instant one, or a cast as it completes): the release over the walk.</summary>
        public void CastRelease() { if (model != null) model.Act("castshot"); }
        /// <summary>A cast being drawn: the spell pose held over the walk.</summary>
        bool castingNow;
        public bool Casting { get { return castingNow; } set { castingNow = value; if (model != null) model.Casting = value; } }
        /// <summary>Struck (EncounterEnemy.Receive): a modelled beast flinches, one way then the other, unless it is mid-attack.</summary>
        public void Flinch()
        {
            if (beastModel != null) { if (!beastModel.Busy && !beastModel.Dead) beastModel.Play((hitLeft = !hitLeft) ? "hitL" : "hitR"); return; }
            if (model != null) model.Act((hitLeft = !hitLeft) ? "hit" : "hithead");
        }
        /// <summary>Edit mode (captures, tests): a modelled beast posed <paramref name="time"/> seconds into one of its motions.</summary>
        public void BeastPreview(string slot, float time) { if (beastModel != null) beastModel.Sample(slot, time); }
    }
}
