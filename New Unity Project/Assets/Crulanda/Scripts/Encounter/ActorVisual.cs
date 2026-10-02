using UnityEngine;

namespace Crulanda.Encounter
{
    public enum ActorLook { Warrior, Druid, Healer, Collector, Warden, Sentry, Outrider, Pale, Villager, Hollow, Cultist, Wolf, Boar, WeaveEater, Deserter, BanditKing, Keeper, Stag, Spider, Bramble }
    /// <summary>Body language layered over walking: working a hoe or bucket, talking, sitting, cowering.</summary>
    public enum ActorPose { None, Work, Talk, Sit, Cower, Hammer, Chop, Gather, Knead, Swim, Sneak, Drink, Slump }

    /// <summary>
    /// Placeholder humanoid built from primitives under the actor's "Body" (so death poses and form scaling still apply
    /// to the whole figure). Walk/idle motion is driven by real movement. Stand-in until authored models exist.
    /// </summary>
    public sealed partial class ActorVisual : MonoBehaviour
    {
        Transform body, legL, legR, armL, armR, torso, head;
        /// <summary>The smooth figure's other bones (playtest note 12; SmoothBody): the spine, the elbows, wrists, knees and ankles.
        /// Null on a block figure (<see cref="Smooth"/> off) and on beasts, the Pale and the Keeper.</summary>
        Transform spine, headBone, foreL, foreR, handL, handR, shinL, shinR, footL, footR;
        /// <summary>People are built smooth (SmoothBody); off, the old block figure (for comparison). Read when a figure is built.</summary>
        public static bool Smooth = true;
        Material cloth, accent;
        Vector3 lastPosition; float phase, speed;

        public ActorPose Pose;
        int variant; bool child, posed; string role; float stoop, swimLean, swimPhase;
        /// <summary>Held weapon/shield/staff parts, and the same kit slung on the back (shown instead while swimming).</summary>
        Transform[] held, stowed; bool gearStowed;
        /// <summary>Class kit that worn gear replaces: the Warrior's shoulder pads, the Druid's hood. See ActorVisual.Gear.cs.</summary>
        Transform[] classKit; ActorLook built;
        /// <summary>The bare figure's parts that worn armour recolours, hides or tucks away (see ActorVisual.GearArmor.cs): torso, chest and shoulders; sleeves; hands; legs and hips; boots; the belt; the hair (the long fall and the bun tuck under a cap); the Druid's cloak.</summary>
        Renderer[] baseChest, baseSleeves, baseHands, baseLegs, baseBoots; Renderer baseBelt; Transform[] hairParts; Transform hairLong, hairBun, druidCloak;
        /// <summary>Villagers pass their trade (<paramref name="role"/>) to get its outfit and tool; see <see cref="Dress"/>.</summary>
        public static ActorVisual Attach(GameObject actor, ActorLook look, int variant = 0, bool child = false, string role = null)
        {
            var v = actor.AddComponent<ActorVisual>(); v.variant = variant; v.child = child; v.role = role; v.Build(look); return v;
        }
        /// <summary>Trades with a recognisable outfit. Others (gossip, drinker, child) wear plain clothes with a random hat or apron.</summary>
        public static bool HasOutfit(string role)
        {
            switch (role)
            {
                case "blacksmith": case "merchant": case "baker": case "henwife": case "farmer": case "hunter": case "leatherworker":
                case "skinner": case "lumberjack": case "herbalist": case "miller": case "elder": case "drinker": case "stranger": case "warden": case "pilgrim": case "innkeeper": return true;
                default: return false;
            }
        }
        // Dyed wool (madder, weld green, woad, ochre, russet, heather) and one undyed oatmeal (playtest note 13: high fantasy, not pale).
        static readonly Color[] VillagerCloth = { new Color(.56f, .15f, .12f), new Color(.21f, .42f, .17f), new Color(.16f, .28f, .52f), new Color(.7f, .49f, .15f),
            new Color(.54f, .27f, .1f), new Color(.42f, .22f, .43f), new Color(.7f, .62f, .45f) };
        static readonly Color[] Skins = { new Color(.8f, .64f, .52f), new Color(.68f, .52f, .4f), new Color(.52f, .38f, .28f), new Color(.86f, .72f, .6f) };
        /// <summary>Recolours the outfit's main cloth (e.g. Druid forms).</summary>
        public void SetClothColor(Color c) { if (cloth != null) cloth.color = c; }
        /// <summary>The right arm's shoulder pivot (the hand is .62 down it): what a villager's tankard hangs from.</summary>
        public Transform RightArm { get { return armR; } }
        /// <summary>0 sober to 1 reeling: the body rolls and pitches with the stride (a drinker walking home).</summary>
        public float Stagger;
        bool staggering;

        static Material Mat(Color c, float smooth = .15f, float metal = 0)
        { var m = new Material(Shader.Find("Standard")) { color = c }; m.SetFloat("_Glossiness", smooth); m.SetFloat("_Metallic", metal); return m; }
        Transform Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3? euler = null)
        {
            var o = GameObject.CreatePrimitive(type);
            if (Application.isPlaying) Destroy(o.GetComponent<Collider>()); else DestroyImmediate(o.GetComponent<Collider>());   // edit mode tests build figures too
            o.transform.SetParent(parent, false); o.transform.localPosition = pos; o.transform.localScale = scale;
            if (euler.HasValue) o.transform.localEulerAngles = euler.Value;
            o.GetComponent<Renderer>().sharedMaterial = m; return o.transform;
        }
        Transform Pivot(string name, Vector3 pos) { var t = new GameObject(name).transform; t.SetParent(body, false); t.localPosition = pos; return t; }

        static readonly Color[] HairColors = { new Color(.22f, .14f, .08f), new Color(.08f, .07f, .06f), new Color(.62f, .48f, .26f), new Color(.45f, .2f, .1f), new Color(.34f, .24f, .14f), new Color(.7f, .68f, .64f) };
        /// <summary>
        /// Softer figure and a face: neck, rounded chest and shoulders, eyes with pupils, brows, nose, mouth, ears, and hair
        /// in a few styles (short, long, tied back, cropped, bald). Colour and style come from the variant. Stone Hollow Men
        /// get blank grey eyes; helmets and hoods simply sit over the hair.
        /// </summary>
        void Features(ActorLook look, Material skin)
        {
            bool stone = look == ActorLook.Hollow;
            Part(PrimitiveType.Cylinder, body, new Vector3(0, .64f, 0), new Vector3(.15f, .06f, .15f), skin);                          // neck
            var chest = Part(PrimitiveType.Sphere, body, new Vector3(0, .44f, .02f), new Vector3(.47f, .34f, .31f), cloth).GetComponent<Renderer>();   // chest
            var shoulders = System.Array.ConvertAll(new[] { -1, 1 }, s => Part(PrimitiveType.Sphere, body, new Vector3(s * .24f, .55f, 0), new Vector3(.2f, .16f, .22f), cloth).GetComponent<Renderer>()); // shoulders
            baseChest = new[] { torso != null ? torso.GetComponent<Renderer>() : null, chest, shoulders[0], shoulders[1] };
            var eyeWhite = Mat(stone ? new Color(.45f, .45f, .47f) : new Color(.95f, .94f, .9f), .6f);
            var pupil = Mat(stone ? new Color(.3f, .3f, .32f) : new Color(.12f, .09f, .07f), .8f);
            var brow = Mat(stone ? new Color(.35f, .35f, .36f) : HairColors[Mathf.Abs(variant * 3 + 1) % HairColors.Length] * .8f);
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Sphere, body, new Vector3(s * .062f, .835f, .128f), new Vector3(.055f, .045f, .03f), eyeWhite);
                if (!stone) Part(PrimitiveType.Sphere, body, new Vector3(s * .062f, .834f, .142f), Vector3.one * .026f, pupil);
                Part(PrimitiveType.Cube, body, new Vector3(s * .064f, .872f, .133f), new Vector3(.06f, .013f, .02f), brow, new Vector3(0, 0, s * -8));
                Part(PrimitiveType.Sphere, body, new Vector3(s * .152f, .8f, 0), new Vector3(.05f, .08f, .05f), skin);                  // ears
            }
            Part(PrimitiveType.Sphere, body, new Vector3(0, .8f, .15f), new Vector3(.05f, .07f, .06f), skin);                           // nose
            Part(PrimitiveType.Cube, body, new Vector3(0, .745f, .137f), new Vector3(.075f, .012f, .01f), Mat(stone ? new Color(.3f, .3f, .31f) : new Color(.45f, .2f, .18f))); // mouth
            if (stone) return;
            // Hair: style and colour by variant (elders go grey via their outfit's own hair piece).
            var hair = Mat(HairColors[Mathf.Abs(variant * 3 + 1) % HairColors.Length], .25f);
            switch (Mathf.Abs(variant) % 5)
            {
                case 0: hairParts = new[] { Part(PrimitiveType.Sphere, body, new Vector3(0, .87f, -.02f), new Vector3(.32f, .22f, .32f), hair) }; break;   // short
                case 1:
                    hairParts = new[] { Part(PrimitiveType.Sphere, body, new Vector3(0, .87f, -.02f), new Vector3(.33f, .24f, .33f), hair),
                        hairLong = Part(PrimitiveType.Cube, body, new Vector3(0, .7f, -.12f), new Vector3(.3f, .34f, .08f), hair) }; break;          // long
                case 2:
                    hairParts = new[] { Part(PrimitiveType.Sphere, body, new Vector3(0, .87f, -.02f), new Vector3(.32f, .22f, .32f), hair),
                        hairBun = Part(PrimitiveType.Sphere, body, new Vector3(0, .84f, -.17f), Vector3.one * .12f, hair) }; break;                  // tied back
                case 3: hairParts = new[] { Part(PrimitiveType.Sphere, body, new Vector3(0, .9f, -.01f), new Vector3(.3f, .15f, .3f), hair) }; break;    // cropped
                default: break;                                                                                                                  // bald
            }
        }
        /// <summary>Not humanoid: no clothes or kit, and a little lighter in health (EncounterEnemy.MobHealth). Weave-Eaters count, though they are no animal.</summary>
        public static bool IsBeast(ActorLook look) { return look == ActorLook.Wolf || look == ActorLook.Boar || look == ActorLook.WeaveEater || look == ActorLook.Stag || look == ActorLook.Spider || look == ActorLook.Bramble; }

        /// <summary>
        /// Four-legged creatures. The front legs use the arm pivots and the hind legs the leg pivots, so the walk cycle
        /// moves diagonal pairs.
        /// - Wolf: a grey-brown hunter. Charcoal with ember eyes when variant is odd ("Ash hound").
        /// - Boar: heavy, bristled, tusked.
        /// Weave-Eaters are not built here (no legs, no head): see <see cref="BuildEater"/>.
        /// </summary>
        void BuildBeast(ActorLook look)
        {
            bool wolf = look == ActorLook.Wolf, boar = look == ActorLook.Boar;
            bool ash = variant % 2 == 1;
            // Khaven's dusk (gloom): darker hides, or wolves and boars vanish into its pale earth and dry grass.
            var zone = Crulanda.World.ZoneBuilder.Active; bool gloom = zone != null && zone.Zone != null && zone.Zone.biome == "gloom";
            Color coat = wolf ? (ash ? new Color(.16f, .15f, .15f) : gloom ? new Color(.25f, .23f, .22f) : new Color(.45f, .41f, .36f))
                : gloom ? new Color(.19f, .14f, .11f) : new Color(.33f, .24f, .18f);   // boar (only wolves and boars come here)
            var fur = Mat(coat); var dark = Mat(coat * .6f); var eye = Mat(wolf && !ash ? new Color(.9f, .75f, .3f) : new Color(1, .45f, .15f), .8f);
            if (ash) { eye.EnableKeyword("_EMISSION"); eye.SetColor("_EmissionColor", new Color(1, .4f, .1f) * 2); }
            float hipY = boar ? -.55f : -.42f, legLen = boar ? .42f : .55f;
            float len = boar ? .95f : .9f;
            LieDepth = boar ? .21f : .39f;   // how far to sink so the belly rests on the ground (lying in wait: EncounterEnemy.Hide)
            // Body.
            torso = Part(PrimitiveType.Capsule, body, new Vector3(0, hipY + (boar ? .05f : 0), 0), boar ? new Vector3(.62f, len * .62f, .58f) : new Vector3(.34f, len * .58f, .38f), fur, new Vector3(90, 0, 0));
            // Legs: front on the arm pivots, hind on the leg pivots.
            float w = boar ? .2f : .13f, front = len * .42f, hind = -len * .38f;
            armL = Pivot("Leg FL", new Vector3(-w, hipY, front)); armR = Pivot("Leg FR", new Vector3(w, hipY, front));
            legL = Pivot("Leg HL", new Vector3(-w, hipY, hind)); legR = Pivot("Leg HR", new Vector3(w, hipY, hind));
            foreach (var leg in new[] { armL, armR, legL, legR })
            {
                float l = legLen;
                Part(PrimitiveType.Capsule, leg, new Vector3(0, -l / 2, 0), new Vector3(boar ? .13f : .1f, l / 2, boar ? .13f : .1f), fur);
                Part(PrimitiveType.Sphere, leg, new Vector3(0, -l, .03f), new Vector3(.11f, .07f, .14f), dark);
            }
            // Head.
            var neck = boar ? new Vector3(0, hipY - .02f, len * .62f) : new Vector3(0, hipY + .2f, len * .6f);
            head = Part(PrimitiveType.Sphere, body, neck, boar ? new Vector3(.36f, .34f, .42f) : new Vector3(.26f, .25f, .3f), fur);
            if (wolf)
            {
                Part(PrimitiveType.Cube, head, new Vector3(0, -.15f, .75f), new Vector3(.5f, .45f, .8f), fur);                       // muzzle
                Part(PrimitiveType.Sphere, head, new Vector3(0, -.05f, 1.15f), Vector3.one * .22f, Mat(new Color(.05f, .05f, .05f)));  // nose
                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, head, new Vector3(s * .3f, .55f, -.1f), new Vector3(.25f, .5f, .12f), dark, new Vector3(0, 0, s * -12));
                Part(PrimitiveType.Capsule, body, new Vector3(0, hipY + .05f, -len * .62f), new Vector3(.1f, .3f, .1f), dark, new Vector3(-55, 0, 0)); // tail
                Part(PrimitiveType.Sphere, body, new Vector3(0, hipY + .12f, len * .38f), new Vector3(.36f, .3f, .3f), dark);              // ruff
            }
            else
            {
                Part(PrimitiveType.Cylinder, head, new Vector3(0, -.12f, .62f), new Vector3(.42f, .15f, .4f), dark, new Vector3(90, 0, 0)); // snout
                foreach (int s in new[] { -1, 1 })
                {
                    Part(PrimitiveType.Cube, head, new Vector3(s * .25f, -.2f, .72f), new Vector3(.07f, .35f, .07f), Mat(new Color(.9f, .86f, .75f), .5f), new Vector3(-20, 0, s * 20)); // tusks
                    Part(PrimitiveType.Cube, head, new Vector3(s * .32f, .45f, -.05f), new Vector3(.2f, .3f, .12f), dark, new Vector3(0, 0, s * -25));
                }
                for (int i = 0; i < 6; i++) Part(PrimitiveType.Cube, body, new Vector3(0, hipY + .3f, len * .35f - i * .14f), new Vector3(.05f, .14f, .1f), dark); // bristle ridge
                Part(PrimitiveType.Capsule, body, new Vector3(0, hipY + .1f, -len * .6f), new Vector3(.05f, .12f, .05f), dark, new Vector3(-30, 0, 0));
            }
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Sphere, head, new Vector3(s * .3f, .12f, .38f), Vector3.one * .12f, eye);
            cloth = fur; beast = true;
        }
        bool beast;
        /// <summary>How far EncounterEnemy.Hide sinks this body to lie in wait: beasts rest the belly on the ground (0: crouch like a person).</summary>
        public float LieDepth { get; private set; }
        /// <summary>Lying low in the grass (set by EncounterEnemy while hidden): beasts fold their legs under them.</summary>
        public bool LyingLow;

        // Weave-Eater state (BuildEater / Drift).
        Transform core, inner; Transform[] flicker; Renderer[] eaterParts; Material[] eaterMats; Material crystal; Crulanda.Gameplay.Actor self;
        Vector3 anchor, glitchScale; float nextJerk, glitchUntil, glitchYaw, eaterSeed; bool calcified;
        /// <summary>Standard shader in Fade mode, with the Wasting veil material's keyword set so the variant is in every build.</summary>
        static Material Fade(Color c, Color emission)
        {
            var m = Mat(c, .45f); m.SetFloat("_Mode", 2); m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON"); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }
        /// <summary>
        /// A Weave-Eater (CANON, book1 ch.20): a manifestation of the Wasting, born from the frayed threads of the broken Weave,
        /// with no face and no limbs, that drifts and moves by sudden jerking displacement. Here: a loose, half-seen tangle of
        /// torn threads, each bent along its own arc and radiating every way (some trailing under it, none bunched), round a
        /// small dim violet core buried inside; a few thread tips still burn violet. It hangs in the air and never walks:
        /// <see cref="Drift"/> bobs and turns it slowly and moves it by displacement. Salted and broken it sets into jagged
        /// white crystal (CANON), so its corpse is a white statue. The tangle comes from a fixed per-actor seed. GAME-ONLY design.
        /// </summary>
        void BuildEater()
        {
            self = GetComponent<Crulanda.Gameplay.Actor>();
            // Seeded by name and spawn spot: the same eater always frays the same way, and no other random stream is touched.
            var at = transform.position; int seed = 17; foreach (char ch in name) seed = seed * 31 + ch;
            var rng = new System.Random(seed ^ (Mathf.RoundToInt(at.x * 10) * 73856093) ^ (Mathf.RoundToInt(at.z * 10) * 19349663));
            float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
            eaterSeed = R(0, 10);
            var fray = Fade(new Color(.5f, .44f, .6f, .55f), new Color(.1f, .04f, .16f)); var faint = Fade(new Color(.66f, .56f, .82f, .3f), new Color(.16f, .06f, .26f));
            var voidM = Mat(new Color(.1f, .05f, .14f), .8f); voidM.EnableKeyword("_EMISSION"); voidM.SetColor("_EmissionColor", new Color(.24f, .07f, .4f));
            var glow = Mat(new Color(.8f, .5f, 1f), .9f); glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(.7f, .35f, 1f) * 2.2f);
            cloth = fray;
            core = new GameObject("Core").transform; core.SetParent(body, false); core.localPosition = new Vector3(0, .5f, 0);
            inner = new GameObject("Inner").transform; inner.SetParent(core, false);   // turns against the rest (Drift), so the tangle never sets
            head = Part(PrimitiveType.Sphere, inner, Vector3.zero, new Vector3(.3f, .4f, .3f), voidM);   // the void at the heart: small, dim, buried
            var flick = new System.Collections.Generic.List<Transform>(); var rings = new float[8]; for (int i = 0; i < 8; i++) rings[i] = i / 7f;
            for (int i = 0; i < 30; i++)
            {
                // A thread: out of the core along dir, bent on an arc in its own plane, tapering to nothing; every third one trails down.
                bool down = i % 3 == 0; var dir = Quaternion.Euler(down ? R(35, 80) : R(-80, 50), R(0, 360), 0) * Vector3.forward;
                var side = Vector3.Cross(dir, Quaternion.Euler(R(0, 360), R(0, 360), 0) * Vector3.forward);
                if (side.sqrMagnitude < .01f) side = Vector3.Cross(dir, Mathf.Abs(dir.y) < .9f ? Vector3.up : Vector3.right);
                side.Normalize();
                float len = down ? R(.6f, 1.05f) : R(.35f, .95f), bend = len * R(.15f, .55f), thick = R(.012f, .03f);
                Vector3 root = dir * .1f, ctrl = root + dir * (len * .5f) + side * bend, end = root + dir * (len * .9f) + side * (bend * 1.7f);
                Vector3 Bez(float t) { float u = 1 - t; return root * (u * u) + ctrl * (2 * u * t) + end * (t * t); }
                var mesh = Crulanda.World.ZoneMeshes.Tube(Bez, (t, a) => thick * Mathf.Clamp01((1 - t) * 3), rings, 5, Vector3.Cross(dir, side));
                var thread = new GameObject("Thread", typeof(MeshFilter), typeof(MeshRenderer)).transform; thread.SetParent(i % 4 == 1 ? inner : core, false);
                thread.GetComponent<MeshFilter>().sharedMesh = mesh; thread.GetComponent<MeshRenderer>().sharedMaterial = i % 5 == 2 ? faint : fray;
                if (i % 4 == 3) flick.Add(Part(PrimitiveType.Sphere, thread.parent, end, Vector3.one * R(.03f, .05f), glow));   // a tip still burning
                else if (i % 7 == 5) flick.Add(thread);
            }
            flicker = flick.ToArray();
            crystal = Mat(new Color(.9f, .93f, .97f), .92f); crystal.EnableKeyword("_EMISSION"); crystal.SetColor("_EmissionColor", new Color(.22f, .23f, .26f));
            // includeInactive: SpawnActor builds the look while the actor is still switched off, so without it this finds nothing.
            eaterParts = body.GetComponentsInChildren<Renderer>(true); eaterMats = System.Array.ConvertAll(eaterParts, r => r.sharedMaterial);
            anchor = transform.position;
        }
        /// <summary>
        /// The Weave-Eater's motion: it drifts (a slow bob and wander about its anchor, the tangle turning one way and its inner
        /// threads the other), then is simply somewhere else (CANON: "a sudden, jerking displacement of space"). The figure
        /// keeps its anchor while the agent moves on beneath it and catches up in one jump, with a flicker of its lit threads
        /// and a torn stretch for a moment. Dead, it is a white crystal statue standing on its threads.
        /// </summary>
        void Drift()
        {
            bool dead = self != null && !self.IsAlive;
            if (dead != calcified)
            {
                calcified = dead; nextJerk = 0;
                for (int i = 0; i < eaterParts.Length; i++) if (eaterParts[i] != null) eaterParts[i].sharedMaterial = dead ? crystal : eaterMats[i];
                foreach (var f in flicker) f.gameObject.SetActive(true);
            }
            if (calcified)
            {
                // EncounterEnemy tips a dead body over; a statue stays upright, set down on its threads.
                body.localRotation = Quaternion.identity; body.localPosition = new Vector3(0, -.3f, 0);
                core.localRotation = inner.localRotation = Quaternion.identity; core.localScale = Vector3.one; return;
            }
            float t = Time.time, dt = Time.deltaTime; var at = transform.position;
            bool moving = (at - lastPosition).sqrMagnitude > .25f * dt * dt; lastPosition = at;   // faster than .5 m/s, at any frame rate
            if (t >= nextJerk || (at - anchor).sqrMagnitude > .8f)
            {
                anchor = at + new Vector3(Random.Range(-.12f, .12f), 0, Random.Range(-.12f, .12f));
                glitchUntil = t + Random.Range(.05f, .12f); glitchYaw = Random.Range(-70f, 70f);
                glitchScale = new Vector3(Random.Range(.7f, 1.35f), Random.Range(.75f, 1.3f), Random.Range(.7f, 1.35f));
                nextJerk = t + (moving ? Random.Range(.12f, .3f) : Random.Range(.6f, 2.2f));
                foreach (var f in flicker) f.gameObject.SetActive(Random.value > .3f);
            }
            bool glitch = t < glitchUntil;
            body.localRotation = Quaternion.identity;
            // Adrift between jerks: a slow bob with a faint ripple on it, and a slower wander round the anchor.
            body.position = anchor + new Vector3(Mathf.Sin(t * .41f + eaterSeed) * .14f, .16f + Mathf.Sin(t * .8f + eaterSeed) * .16f + Mathf.Sin(t * 2.6f + eaterSeed * 2) * .03f, Mathf.Cos(t * .33f + eaterSeed * 1.7f) * .14f);
            core.localScale = glitch ? glitchScale : Vector3.one * (1 + Mathf.Sin(t * 9 + eaterSeed) * .02f);
            core.localRotation = Quaternion.Euler(Mathf.Sin(t * .7f + eaterSeed) * 9, t * 14 + eaterSeed * 36 + (glitch ? glitchYaw : 0), Mathf.Cos(t * .55f + eaterSeed) * 9);
            inner.localRotation = Quaternion.Euler(Mathf.Cos(t * .45f + eaterSeed) * 12, -t * 24 + eaterSeed * 50, Mathf.Sin(t * .6f + eaterSeed) * 12);
        }

        static Mesh robeCone;
        /// <summary>
        /// A Pale watcher. The Pale Things are cosmic watchers and auditors (CANON, world_bible.md); the Pale King seen in book1
        /// is impossibly tall and wears a featureless gold mask with no eyes (CANON). This figure is GAME-ONLY: about 2.5 m,
        /// gaunt, in a narrow pale floor-length robe (the hem 1.3x the shoulders: a column, not a pawn) and a deep tall hood
        /// with a small eyeless gold mask set back in it, stooped a little at the waist, arms too long, and no legs showing,
        /// so it glides (Khaven: "It leaves no footprints"), faintly pale-lit at dusk. Nothing is scaled: every part sits at its
        /// true height (feet at -1), so the hood rests on the collar and the swinging arms don't shear.
        /// </summary>
        void BuildPale()
        {
            var robe = Mat(new Color(.88f, .89f, .92f), .3f); var fold = Mat(new Color(.7f, .72f, .78f), .2f);
            robe.EnableKeyword("_EMISSION"); robe.SetColor("_EmissionColor", new Color(.1f, .1f, .12f));
            var skin = Mat(new Color(.93f, .94f, .97f), .7f); var gold = Mat(new Color(.82f, .66f, .28f), .75f, .6f);
            cloth = robe; accent = gold;
            // A tall narrow cone from the hem to the throat: robe, waist and high collar in one taper.
            if (robeCone == null) robeCone = Crulanda.World.ZoneMeshes.Cone(1, 1, 14);
            var gown = new GameObject("Robe", typeof(MeshFilter), typeof(MeshRenderer)).transform; gown.SetParent(body, false);
            gown.localPosition = new Vector3(0, -.97f, 0); gown.localScale = new Vector3(.3f, 2.38f, .27f);   // apex stays inside the hood
            gown.GetComponent<MeshFilter>().sharedMesh = robeCone; gown.GetComponent<MeshRenderer>().sharedMaterial = robe;
            Part(PrimitiveType.Cylinder, body, new Vector3(0, -.955f, 0), new Vector3(.62f, .015f, .56f), fold);          // hem
            // Everything above the waist hangs from a spine pivot pitched forward: a slight stoop, bent as if to look down at you.
            var spine = Pivot("Spine", new Vector3(0, .2f, 0)); spine.localEulerAngles = new Vector3(8, 0, 0);
            Transform Up(PrimitiveType type, Vector3 pos, Vector3 scale, Material m, Vector3? euler = null) { return Part(type, spine, pos - new Vector3(0, .2f, 0), scale, m, euler); }
            Up(PrimitiveType.Cube, new Vector3(0, .56f, 0), new Vector3(.3f, .72f, .18f), robe);                      // narrow chest
            Up(PrimitiveType.Sphere, new Vector3(0, .9f, 0), new Vector3(.46f, .14f, .24f), robe);                   // high, sloping shoulders
            Up(PrimitiveType.Cylinder, new Vector3(0, .98f, 0), new Vector3(.2f, .06f, .2f), robe);                  // collar
            head = Up(PrimitiveType.Sphere, new Vector3(0, 1.28f, -.05f), new Vector3(.3f, .46f, .32f), robe);        // tall hood, resting on the collar
            // A deep cowl: a peak and two cheek flaps reach forward past the mask, so the face sits back in the hood and no
            // gold shows round the head from behind (the mask used to poke out past the hood as a sliver).
            Up(PrimitiveType.Cube, new Vector3(0, 1.44f, .08f), new Vector3(.28f, .05f, .18f), robe, new Vector3(12, 0, 0));
            foreach (int s in new[] { -1, 1 }) Up(PrimitiveType.Cube, new Vector3(s * .13f, 1.24f, .08f), new Vector3(.05f, .34f, .18f), robe, new Vector3(0, s * -12, 0));
            Up(PrimitiveType.Sphere, new Vector3(0, 1.25f, .1f), new Vector3(.18f, .28f, .08f), gold);              // the mask: no eyes, nothing on it
            Up(PrimitiveType.Sphere, new Vector3(0, .9f, .12f), Vector3.one * .06f, gold);                           // clasp
            legL = Pivot("Leg L", new Vector3(-.1f, -.08f, 0)); legR = Pivot("Leg R", new Vector3(.1f, -.08f, 0));       // under the robe: nothing shows
            // The arms hang straight down (body space) from the stooped shoulders, so they don't trail behind the lean.
            armL = Pivot("Arm L", new Vector3(-.24f, .89f, .1f)); armR = Pivot("Arm R", new Vector3(.24f, .89f, .1f));
            float sn = Mathf.Sin(5 * Mathf.Deg2Rad);
            foreach (int s in new[] { -1, 1 })
            {
                // Held 5 degrees out from the body (2 once the walk cycle's inward 3 is added): the sleeves and hands hang just
                // clear of the narrow robe instead of sinking into it.
                var arm = s < 0 ? armL : armR; var tilt = new Vector3(0, 0, s * 5);
                Part(PrimitiveType.Capsule, arm, new Vector3(s * .53f * sn, -.53f, 0), new Vector3(.1f, .53f, .1f), robe, tilt);     // sleeves to mid-thigh
                Part(PrimitiveType.Cylinder, arm, new Vector3(s * .97f * sn, -.97f, 0), new Vector3(.16f, .07f, .16f), fold, tilt);  // cuff
                Part(PrimitiveType.Sphere, arm, new Vector3(s * 1.15f * sn, -1.15f, 0), new Vector3(.08f, .24f, .06f), skin, tilt);   // long, thin hands
            }
        }

        /// <summary>The old block figure (Smooth off): capsule limbs on four pivots, a cube torso, hips and boots, a sphere head.</summary>
        void BuildBlock(ActorLook look, Material skin, Material legs, Material boots)
        {

            // Legs and arms hang from pivots so they can swing.
            legL = Pivot("Leg L", new Vector3(-.12f, -.08f, 0)); legR = Pivot("Leg R", new Vector3(.12f, -.08f, 0));
            baseLegs = new Renderer[3]; baseBoots = new Renderer[2];
            for (int i = 0; i < 2; i++)
            {
                var leg = i == 0 ? legL : legR;
                baseLegs[i] = Part(PrimitiveType.Capsule, leg, new Vector3(0, -.44f, 0), new Vector3(.18f, .44f, .18f), legs).GetComponent<Renderer>();
                baseBoots[i] = Part(PrimitiveType.Cube, leg, new Vector3(0, -.86f, .05f), new Vector3(.17f, .12f, .28f), boots).GetComponent<Renderer>();
            }
            baseLegs[2] = Part(PrimitiveType.Cube, body, new Vector3(0, -.02f, 0), new Vector3(.44f, .2f, .27f), legs).GetComponent<Renderer>();           // hips
            torso = Part(PrimitiveType.Cube, body, new Vector3(0, .3f, 0), new Vector3(.48f, .6f, .29f), cloth);
            baseBelt = Part(PrimitiveType.Cube, body, new Vector3(0, .06f, 0), new Vector3(.5f, .07f, .31f), Mat(new Color(.22f, .16f, .11f))).GetComponent<Renderer>(); // belt
            head = Part(PrimitiveType.Sphere, body, new Vector3(0, .8f, 0), new Vector3(.3f, .32f, .3f), skin);
            Features(look, skin);
            armL = Pivot("Arm L", new Vector3(-.31f, .53f, 0)); armR = Pivot("Arm R", new Vector3(.31f, .53f, 0));
            baseSleeves = new Renderer[2]; baseHands = new Renderer[2];
            for (int i = 0; i < 2; i++)
            {
                var arm = i == 0 ? armL : armR;
                baseSleeves[i] = Part(PrimitiveType.Capsule, arm, new Vector3(0, -.3f, 0), new Vector3(.14f, .3f, .14f), cloth).GetComponent<Renderer>();
                baseHands[i] = Part(PrimitiveType.Sphere, arm, new Vector3(0, -.62f, 0), Vector3.one * .12f, skin).GetComponent<Renderer>();
            }
        }
        /// <summary>
        /// The smooth figure (playtest note 12; SmoothBody): one skinned body per region on a skeleton whose shoulder and hip pivots
        /// are the old ones ("Arm L/R", "Leg L/R"), with elbows, wrists, knees and ankles under them; the head, face and hair ride
        /// the Head bone. The regions worn gear covers are the base* renderers, one each.
        /// </summary>
        void BuildSmooth(ActorLook look, Material skin, Material legs, Material boots)
        {
            var belt = Mat(new Color(.22f, .16f, .11f));
            var mats = new Material[SmoothBody.RegionCount];
            mats[(int)SmoothBody.Region.Chest] = cloth; mats[(int)SmoothBody.Region.Sleeves] = cloth; mats[(int)SmoothBody.Region.Belt] = belt;
            mats[(int)SmoothBody.Region.Legs] = legs; mats[(int)SmoothBody.Region.Boots] = boots; mats[(int)SmoothBody.Region.Hands] = skin; mats[(int)SmoothBody.Region.Neck] = skin;
            var bones = SmoothBody.Build(body, mats, skin, out var rs, out var headPart);
            spine = bones[SmoothBody.Spine]; headBone = bones[SmoothBody.Head];
            armL = bones[SmoothBody.ArmL]; armR = bones[SmoothBody.ArmR]; foreL = bones[SmoothBody.ForearmL]; foreR = bones[SmoothBody.ForearmR]; handL = bones[SmoothBody.HandL]; handR = bones[SmoothBody.HandR];
            legL = bones[SmoothBody.LegL]; legR = bones[SmoothBody.LegR]; shinL = bones[SmoothBody.ShinL]; shinR = bones[SmoothBody.ShinR]; footL = bones[SmoothBody.FootL]; footR = bones[SmoothBody.FootR];
            baseChest = new[] { rs[(int)SmoothBody.Region.Chest] }; baseSleeves = new[] { rs[(int)SmoothBody.Region.Sleeves] }; baseHands = new[] { rs[(int)SmoothBody.Region.Hands] };
            baseLegs = new[] { rs[(int)SmoothBody.Region.Legs] }; baseBoots = new[] { rs[(int)SmoothBody.Region.Boots] }; baseBelt = rs[(int)SmoothBody.Region.Belt];
            // Where the old torso cube stood: the walk's bob moves it (and the spine with it), and GearBob reads it.
            torso = new GameObject("Torso").transform; torso.SetParent(body, false); torso.localPosition = new Vector3(0, .3f, 0);
            head = headPart;
            SmoothFace(look, skin);
        }
        /// <summary>The face on the smooth head (the old one's eyes, brows, ears, nose and mouth, set on its surface) and the hair:
        /// a cap (short), the cap and a fall down the back (long), the cap and a bun (tied back), cropped short, or bald.</summary>
        void SmoothFace(ActorLook look, Material skin)
        {
            bool stone = look == ActorLook.Hollow; var hb = headBone; var o = -SmoothBody.Rest[SmoothBody.Head];
            var eyeWhite = Mat(stone ? new Color(.45f, .45f, .47f) : new Color(.95f, .94f, .9f), .6f);
            var pupil = Mat(stone ? new Color(.3f, .3f, .32f) : new Color(.12f, .09f, .07f), .8f);
            var brow = Mat(stone ? new Color(.35f, .35f, .36f) : HairColors[Mathf.Abs(variant * 3 + 1) % HairColors.Length] * .8f);
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Sphere, hb, new Vector3(s * .058f, .838f, .14f) + o, new Vector3(.052f, .044f, .03f), eyeWhite);
                if (!stone) Part(PrimitiveType.Sphere, hb, new Vector3(s * .058f, .837f, .154f) + o, Vector3.one * .025f, pupil);
                Part(PrimitiveType.Cube, hb, new Vector3(s * .06f, .874f, .146f) + o, new Vector3(.058f, .013f, .02f), brow, new Vector3(0, 0, s * -8));
                Part(PrimitiveType.Sphere, hb, new Vector3(s * .143f, .815f, .0f) + o, new Vector3(.045f, .078f, .052f), skin);                  // ears
            }
            Part(PrimitiveType.Sphere, hb, new Vector3(0, .8f, .164f) + o, new Vector3(.048f, .068f, .058f), skin);                           // nose
            Part(PrimitiveType.Cube, hb, new Vector3(0, .748f, .152f) + o, new Vector3(.07f, .012f, .01f), Mat(stone ? new Color(.3f, .3f, .31f) : new Color(.45f, .2f, .18f))); // mouth
            if (stone) return;
            var hair = Mat(HairColors[Mathf.Abs(variant * 3 + 1) % HairColors.Length], .25f);
            Transform Hair(Mesh m) { var g = new GameObject(m.name); g.transform.SetParent(hb, false); g.AddComponent<MeshFilter>().sharedMesh = m; g.AddComponent<MeshRenderer>().sharedMaterial = hair; return g.transform; }
            switch (Mathf.Abs(variant) % 5)
            {
                case 0: hairParts = new[] { Hair(SmoothBody.HairMesh(false)) }; break;                                                              // short
                case 1: hairParts = new[] { Hair(SmoothBody.HairMesh(false)), hairLong = Hair(SmoothBody.HairFallMesh) }; break;                    // long
                case 2: hairParts = new[] { Hair(SmoothBody.HairMesh(false)), hairBun = Part(PrimitiveType.Sphere, hb, new Vector3(0, .83f, -.18f) + o, Vector3.one * .13f, hair) }; break;   // tied back
                case 3: hairParts = new[] { Hair(SmoothBody.HairMesh(true)) }; break;                                                               // cropped
                default: break;                                                                                                                    // bald
            }
        }
        void Build(ActorLook look)
        {
            body = transform.Find("Body"); if (body == null) return;
            built = look;
            var capsule = body.GetComponent<MeshRenderer>(); if (capsule != null) capsule.enabled = false;
            if (look == ActorLook.WeaveEater) { BuildEater(); lastPosition = transform.position; return; }
            if (look == ActorLook.Pale) { BuildPale(); lastPosition = transform.position; return; }
            if (look == ActorLook.Stag) { BuildStag(); lastPosition = transform.position; return; }
            if (look == ActorLook.Spider) { BuildSpider(); lastPosition = transform.position; return; }
            if (look == ActorLook.Bramble) { BuildBramble(); lastPosition = transform.position; return; }
            if (look == ActorLook.Keeper) { BuildKeeper(); lastPosition = transform.position; return; }
            if (IsBeast(look)) { BuildBeast(look); lastPosition = transform.position; return; }
            Material skin = Mat(new Color(.76f, .6f, .48f));
            Color clothC, accentC, legC;
            switch (look)
            {
                case ActorLook.Warrior: clothC = new Color(.08f, .2f, .56f); accentC = new Color(.76f, .78f, .82f); legC = new Color(.32f, .2f, .12f); break;
                case ActorLook.Druid: clothC = new Color(.1f, .38f, .16f); accentC = new Color(.56f, .24f, .09f); legC = new Color(.34f, .23f, .13f); break;
                case ActorLook.Healer: clothC = new Color(.1f, .44f, .36f); accentC = new Color(.95f, .86f, .6f); legC = new Color(.1f, .3f, .27f); break;
                case ActorLook.Collector: clothC = new Color(.14f, .22f, .54f); accentC = new Color(.76f, .78f, .82f); legC = new Color(.12f, .14f, .24f); break;
                case ActorLook.Warden: clothC = new Color(.08f, .15f, .46f); accentC = new Color(.8f, .82f, .86f); legC = new Color(.09f, .1f, .2f); break;
                case ActorLook.Outrider: clothC = new Color(.74f, .42f, .12f); accentC = new Color(.56f, .36f, .2f); legC = new Color(.4f, .25f, .14f); break;
                case ActorLook.Hollow: clothC = new Color(.5f, .5f, .51f); accentC = new Color(.4f, .4f, .42f); legC = new Color(.44f, .44f, .45f); skin = Mat(new Color(.56f, .56f, .57f), .05f); break;
                case ActorLook.Cultist: clothC = new Color(.11f, .08f, .13f); accentC = new Color(.88f, .84f, .72f); legC = new Color(.1f, .07f, .11f); break;
                // Sandthrone sand gone to dirt (tinted per figure in BuildDeserter); faces weathered to any shade.
                case ActorLook.Deserter:
                    clothC = new Color(.6f, .46f, .26f); accentC = new Color(.44f, .44f, .46f); legC = new Color(.34f, .25f, .17f);
                    skin = Mat(Skins[(FigureSeed() & 0x7fffffff) % Skins.Length]); break;
                // The coat's near-black brown (torso and sleeves), dark breeches, a face gone red-brown in the wind.
                case ActorLook.BanditKing: clothC = new Color(.13f, .09f, .09f); accentC = new Color(.84f, .6f, .2f); legC = new Color(.36f, .1f, .08f); skin = Mat(new Color(.7f, .53f, .4f)); break;
                case ActorLook.Villager:
                    clothC = VillagerCloth[Mathf.Abs(variant) % VillagerCloth.Length]; accentC = VillagerCloth[Mathf.Abs(variant * 3 + 2) % VillagerCloth.Length] * .8f;
                    legC = new Color(.3f, .25f, .2f) * (.8f + (variant % 3) * .15f); skin = Mat(Skins[Mathf.Abs(variant * 7) % Skins.Length]); break;
                default: clothC = new Color(.6f, .27f, .18f); accentC = new Color(.4f, .35f, .3f); legC = new Color(.28f, .2f, .16f); break;
            }
            cloth = Mat(clothC); accent = Mat(accentC, look == ActorLook.Warrior || look == ActorLook.Warden || look == ActorLook.Collector ? .55f : .15f,
                look == ActorLook.Warrior || look == ActorLook.Warden || look == ActorLook.Collector ? .5f : 0);
            var legs = Mat(legC); var boots = Mat(new Color(.18f, .13f, .1f));
            if (look == ActorLook.Warden) body.localScale = new Vector3(1.08f, 1.12f, 1.08f);

            if (Smooth) BuildSmooth(look, skin, legs, boots);
            else BuildBlock(look, skin, legs, boots);
            switch (look)
            {
                case ActorLook.Warrior:
                {
                    classKit = new[] { Part(PrimitiveType.Sphere, body, new Vector3(-.31f, .58f, 0), new Vector3(.26f, .18f, .26f), accent), Part(PrimitiveType.Sphere, body, new Vector3(.31f, .58f, 0), new Vector3(.26f, .18f, .26f), accent) };   // pads
                    var hilt = Mat(new Color(.4f, .3f, .15f)); var boards = Mat(new Color(.36f, .25f, .15f));
                    held = new[] {
                        Part(PrimitiveType.Cube, armR, new Vector3(0, -.66f, .38f), new Vector3(.05f, .06f, .9f), accent, new Vector3(20, 0, 0)),         // sword
                        Part(PrimitiveType.Cube, armR, new Vector3(0, -.64f, -.04f), new Vector3(.22f, .04f, .06f), hilt),                                 // crossguard
                        Part(PrimitiveType.Cylinder, armL, new Vector3(-.1f, -.38f, .05f), new Vector3(.5f, .03f, .5f), boards, new Vector3(0, 0, 90)),   // shield
                        Part(PrimitiveType.Cylinder, armL, new Vector3(-.13f, -.38f, .05f), new Vector3(.18f, .02f, .18f), accent, new Vector3(0, 0, 90)) };
                    // Slung for swimming: the sword across the back, hilt over the right shoulder, the shield flat over it.
                    stowed = new[] {
                        Part(PrimitiveType.Cube, body, new Vector3(0, .3f, -.18f), new Vector3(.05f, .9f, .06f), accent, new Vector3(0, 0, -35)),
                        Part(PrimitiveType.Cube, body, new Vector3(.24f, .64f, -.18f), new Vector3(.22f, .04f, .06f), hilt, new Vector3(0, 0, -35)),
                        Part(PrimitiveType.Cylinder, body, new Vector3(0, .3f, -.24f), new Vector3(.5f, .03f, .5f), boards, new Vector3(90, 0, 0)),
                        Part(PrimitiveType.Cylinder, body, new Vector3(0, .3f, -.27f), new Vector3(.18f, .02f, .18f), accent, new Vector3(90, 0, 0)) };
                    break;
                }
                case ActorLook.Druid:
                    classKit = new[] { Part(PrimitiveType.Sphere, body, new Vector3(0, .84f, -.03f), new Vector3(.36f, .38f, .38f), cloth) };   // hood
                    druidCloak = Part(PrimitiveType.Cube, body, new Vector3(0, .2f, -.17f), new Vector3(.52f, .95f, .05f), accent, new Vector3(-6, 0, 0)); // cloak
                {
                    var staff = Mat(new Color(.35f, .25f, .15f)); var orb = Mat(new Color(.3f, .8f, .4f), .6f);
                    held = new[] { Part(PrimitiveType.Cylinder, armR, new Vector3(0, -.45f, .08f), new Vector3(.06f, .85f, .06f), staff, new Vector3(8, 0, 0)),   // staff
                        Part(PrimitiveType.Sphere, armR, new Vector3(0, .38f, .15f), Vector3.one * .13f, orb) };
                    stowed = new[] { Part(PrimitiveType.Cylinder, body, new Vector3(0, .25f, -.28f), new Vector3(.06f, .85f, .06f), staff, new Vector3(0, 0, -40)),   // across the back (behind the cloak) for swimming
                        Part(PrimitiveType.Sphere, body, new Vector3(.55f, .9f, -.28f), Vector3.one * .13f, orb) };
                }
                    break;
                case ActorLook.Healer:
                    Part(PrimitiveType.Cylinder, body, new Vector3(0, -.5f, 0), new Vector3(.52f, .45f, .44f), cloth);                  // robe skirt
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .84f, -.03f), new Vector3(.35f, .37f, .37f), accent);                 // hood
                    Part(PrimitiveType.Cube, body, new Vector3(0, .3f, .15f), new Vector3(.12f, .55f, .02f), accent);                     // stole
                    break;
                case ActorLook.Collector:
                case ActorLook.Warden:
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .86f, 0), new Vector3(.34f, .24f, .34f), accent);                     // helmet
                    Part(PrimitiveType.Cube, body, new Vector3(0, .15f, .15f), new Vector3(.36f, .7f, .03f), Mat(new Color(.93f, .93f, .95f))); // Concord tabard
                    Part(PrimitiveType.Cube, body, new Vector3(0, .32f, .165f), new Vector3(.14f, .14f, .01f), Mat(new Color(.98f, .76f, .2f), .7f, .6f)); // sigil
                    float pole = look == ActorLook.Warden ? 1.1f : .9f;
                    Part(PrimitiveType.Cylinder, armR, new Vector3(0, -.35f, .1f), new Vector3(.05f, pole, .05f), Mat(new Color(.3f, .22f, .14f)));
                    Part(PrimitiveType.Cube, armR, new Vector3(0, -.35f + pole, .1f), look == ActorLook.Warden ? new Vector3(.05f, .3f, .28f) : new Vector3(.04f, .22f, .06f), accent);
                    if (look == ActorLook.Warden) Part(PrimitiveType.Cube, body, new Vector3(0, .25f, -.17f), new Vector3(.56f, .9f, .04f), cloth, new Vector3(-5, 0, 0));
                    break;
                case ActorLook.Outrider:
                    // Sandthrone mercenary: sand-coloured wraps, a head scarf and a bearded axe.
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .84f, 0), new Vector3(.34f, .26f, .34f), cloth);
                    Part(PrimitiveType.Cube, body, new Vector3(0, .1f, 0), new Vector3(.54f, .45f, .33f), accent);
                    Part(PrimitiveType.Cylinder, armR, new Vector3(0, -.5f, .2f), new Vector3(.05f, .5f, .05f), Mat(new Color(.3f, .22f, .14f)), new Vector3(60, 0, 0));
                    Part(PrimitiveType.Cube, armR, new Vector3(0, -.45f, .55f), new Vector3(.04f, .34f, .26f), Mat(new Color(.55f, .56f, .58f), .5f, .5f), new Vector3(60, 0, 0));
                    break;
                case ActorLook.Villager when HasOutfit(role):
                    Dress(role, skin, legs);
                    break;
                case ActorLook.Villager:
                    // Plain folk: an apron or a hat or a headscarf, chosen by variant; children are smaller.
                    if (variant % 3 == 0) Part(PrimitiveType.Cube, body, new Vector3(0, -.05f, .15f), new Vector3(.4f, .6f, .02f), Mat(new Color(.78f, .74f, .64f)));
                    if (variant % 4 == 1) { Part(PrimitiveType.Cylinder, body, new Vector3(0, .95f, 0), new Vector3(.46f, .02f, .46f), Mat(new Color(.62f, .52f, .3f))); Part(PrimitiveType.Cylinder, body, new Vector3(0, 1.0f, 0), new Vector3(.26f, .07f, .26f), Mat(new Color(.62f, .52f, .3f))); }
                    if (variant % 4 == 2) Part(PrimitiveType.Sphere, body, new Vector3(0, .84f, -.02f), new Vector3(.33f, .3f, .34f), accent);
                    if (child) body.localScale = new Vector3(.68f, .66f, .68f);
                    break;
                case ActorLook.Hollow:
                    // A Hollow Man (CANON, book1 ch.4): a villager turned to grey stone, cracked, with a violet shard where the heart was.
                    for (int i = 0; i < 5; i++) Part(PrimitiveType.Cube, body, new Vector3((i - 2) * .08f, .25f + (i % 2) * .15f, .15f), new Vector3(.02f, .25f, .01f), Mat(new Color(.22f, .22f, .23f)), new Vector3(0, 0, i * 30 - 60));
                    var shard = Mat(new Color(.7f, .4f, 1f), .9f); shard.EnableKeyword("_EMISSION"); shard.SetColor("_EmissionColor", new Color(.65f, .3f, 1f) * 2.5f);
                    Part(PrimitiveType.Cube, body, new Vector3(-.06f, .42f, .15f), new Vector3(.07f, .12f, .04f), shard, new Vector3(0, 0, 20));
                    body.localEulerAngles = new Vector3(10, 0, 0); stoop = 10;
                    break;
                case ActorLook.Cultist:
                    // Cult of Ash (CANON cult, book1 series bible): bone mask, purple tear, soot-black robes. GAME-ONLY styling.
                    Part(PrimitiveType.Cylinder, body, new Vector3(0, -.5f, 0), new Vector3(.54f, .45f, .46f), cloth);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .85f, -.04f), new Vector3(.37f, .39f, .39f), cloth);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .8f, .1f), new Vector3(.26f, .3f, .14f), accent);                         // bone mask
                    var tear = Mat(new Color(.55f, .2f, .8f), .8f); tear.EnableKeyword("_EMISSION"); tear.SetColor("_EmissionColor", new Color(.5f, .15f, .8f) * 1.8f);
                    Part(PrimitiveType.Cube, body, new Vector3(.06f, .74f, .17f), new Vector3(.03f, .1f, .01f), tear);
                    Part(PrimitiveType.Cylinder, armR, new Vector3(0, -.45f, .08f), new Vector3(.05f, .85f, .05f), Mat(new Color(.18f, .15f, .12f)));
                    Part(PrimitiveType.Sphere, armR, new Vector3(0, .42f, .08f), new Vector3(.16f, .2f, .16f), accent);                      // skull knob
                    break;
                case ActorLook.Deserter: BuildDeserter(); break;
                case ActorLook.BanditKing: BuildBanditKing(); break;
            }
            if (stowed != null) foreach (var g in stowed) g.gameObject.SetActive(false);   // shown only while swimming
            lastPosition = transform.position;
        }

        /// <summary>A seed from the figure's name and spawn spot: the same deserter is always put together the same way, and no shared random stream is touched.</summary>
        int FigureSeed()
        {
            var at = transform.position; int seed = 17; foreach (char ch in name) seed = seed * 31 + ch;
            return seed ^ (Mathf.RoundToInt(at.x * 10) * 73856093) ^ (Mathf.RoundToInt(at.z * 10) * 19349663);
        }
        static Color Dim(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k); }
        static readonly Color[] Scarves = { new Color(.6f, .2f, .12f), new Color(.7f, .46f, .14f), new Color(.5f, .18f, .12f) };   // madder, ochre, brick

        /// <summary>
        /// A Sandthrone deserter (CANON company, GAME-ONLY band): the company's sand gone to dirt. A torn sand tabard over company
        /// mail ripped at the hem, a leather baldric, one leather pauldron and one mail sleeve, rags of the old waist-wrap, an
        /// ochre scarf over nose and mouth, a hood or the torn Sandthrone head-wrap, and a short falchion or an iron-studded club.
        /// Each is put together differently (<see cref="FigureSeed"/>), so a camp reads as a ragged band, not the uniformed
        /// company that keeps the Peaks toll (Outrider). GAME-ONLY styling.
        /// </summary>
        void BuildDeserter()
        {
            var rng = new System.Random(FigureSeed());
            float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
            Material M(float r, float g, float b, float smooth = .15f, float metal = 0) { return Mat(new Color(r, g, b), smooth, metal); }
            bool hooded = rng.NextDouble() < .6, club = rng.NextDouble() < .45; int side = rng.Next(2) == 0 ? 1 : -1;
            float tint = R(.88f, 1.04f); var sandC = new Color(.55f * tint, .45f * tint, .3f * tint);
            cloth.color = sandC; var sand = cloth;   // torso, sleeves, chest and shoulders share it
            var mail = M(.44f, .44f, .45f, .35f, .55f); var leather = M(.36f, .23f, .13f); var dark = M(.25f, .17f, .11f);
            var grime = M(.27f, .21f, .15f); var iron = M(.4f, .4f, .42f, .45f, .6f); var wood = M(.36f, .26f, .16f);
            var scarf = Mat(Scarves[rng.Next(Scarves.Length)]);
            // Company mail over the tunic, ripped at the hem.
            Part(PrimitiveType.Cube, body, new Vector3(0, .37f, 0), new Vector3(.52f, .42f, .38f), mail);
            for (int k = 0; k < 4; k++) { float l = R(.05f, .15f); Part(PrimitiveType.Cube, body, new Vector3(-.18f + k * .12f, .16f - l / 2, .18f), new Vector3(.1f, l, .02f), mail); }
            for (int k = 0; k < 3; k++) { float l = R(.05f, .13f); Part(PrimitiveType.Cube, body, new Vector3(-.13f + k * .13f, .16f - l / 2, -.18f), new Vector3(.11f, l, .02f), mail); }
            // The company's sand tabard, front and back, slipped to one side and torn to strips at the hem.
            float tx = side * .035f;
            Part(PrimitiveType.Cube, body, new Vector3(tx, .33f, .205f), new Vector3(.3f, .46f, .02f), sand);
            for (int k = 0; k < 3; k++) { float l = R(.08f, .24f); Part(PrimitiveType.Cube, body, new Vector3(tx - .1f + k * .1f, .1f - l / 2, .205f), new Vector3(.085f, l, .016f), sand, new Vector3(0, 0, R(-6, 6))); }
            Part(PrimitiveType.Cube, body, new Vector3(tx + .05f * side, .42f, .217f), new Vector3(.08f, .07f, .006f), grime);
            Part(PrimitiveType.Cube, body, new Vector3(-tx, .33f, -.205f), new Vector3(.3f, .46f, .02f), sand);
            for (int k = 0; k < 2; k++) { float l = R(.08f, .22f); Part(PrimitiveType.Cube, body, new Vector3(-tx - .06f + k * .12f, .1f - l / 2, -.205f), new Vector3(.1f, l, .016f), sand, new Vector3(0, 0, R(-6, 6))); }
            // A leather baldric across the chest; a leather pauldron where it meets the shoulder, a mail sleeve on the other arm.
            foreach (float z in new[] { .23f, -.215f }) Part(PrimitiveType.Cube, body, new Vector3(0, .34f, z), new Vector3(.055f, .66f, .016f), leather, new Vector3(0, 0, side * 36));
            int p = -side;   // the baldric's upper end
            Part(PrimitiveType.Sphere, body, new Vector3(p * .29f, .6f, 0), new Vector3(.25f, .17f, .27f), leather);
            Part(PrimitiveType.Sphere, body, new Vector3(p * .3f, .663f, .09f), Vector3.one * .035f, iron);                 // rivet
            Part(PrimitiveType.Capsule, p > 0 ? armL : armR, new Vector3(0, -.2f, 0), new Vector3(.17f, .2f, .17f), mail);
            Part(PrimitiveType.Cylinder, p > 0 ? armR : armL, new Vector3(0, -.47f, 0), new Vector3(.16f, .07f, .16f), dark);   // bracer
            // Rags of the old waist-wrap.
            foreach (var (x, z, w, d) in new[] { (-.15f, .155f, .09f, .02f), (.13f, .155f, .08f, .02f), (-.235f, .03f, .02f, .09f), (.05f, -.155f, .1f, .02f) })
            { float l = R(.22f, .34f); Part(PrimitiveType.Cube, body, new Vector3(x, .03f - l / 2, z), new Vector3(w, l, d), sand, new Vector3(0, 0, R(-5, 5))); }
            // A hood with its cowl fallen on the shoulders, or the torn Sandthrone head-wrap trailing its ends.
            if (hooded)
            {
                var hood = Mat(Dim(sandC, .8f));
                Part(PrimitiveType.Sphere, body, new Vector3(0, .86f, -.05f), new Vector3(.37f, .35f, .36f), hood);
                Part(PrimitiveType.Cube, body, new Vector3(0, .66f, -.15f), new Vector3(.34f, .16f, .08f), hood, new Vector3(-12, 0, 0));
            }
            else
            {
                var wrap = Mat(Dim(sandC, .92f));
                Part(PrimitiveType.Sphere, body, new Vector3(0, .9f, -.01f), new Vector3(.33f, .2f, .33f), wrap);
                Part(PrimitiveType.Cube, body, new Vector3(.05f, .74f, -.17f), new Vector3(.07f, .26f, .025f), wrap, new Vector3(10, 0, -6));
                Part(PrimitiveType.Cube, body, new Vector3(-.03f, .7f, -.165f), new Vector3(.06f, .2f, .02f), wrap, new Vector3(6, 0, 8));
            }
            // The scarf: pulled up to the bridge of the nose, wound round the jaw, one end hanging down the back.
            Part(PrimitiveType.Cube, body, new Vector3(0, .7575f, .135f), new Vector3(.24f, .115f, .1f), scarf);
            Part(PrimitiveType.Cylinder, body, new Vector3(0, .66f, 0), new Vector3(.25f, .045f, .25f), scarf);
            Part(PrimitiveType.Cube, body, new Vector3(side * .07f, .57f, -.228f), new Vector3(.07f, .2f, .02f), scarf, new Vector3(8, 0, side * 12));
            // A short falchion or an iron-studded club, carried point-forward and a little down.
            var hand = new Vector3(0, -.62f, 0); var along = new Vector3(0, -Mathf.Sin(20 * Mathf.Deg2Rad), Mathf.Cos(20 * Mathf.Deg2Rad));
            Vector3 At(float t) { return hand + along * t; }
            if (!club)
            {
                Part(PrimitiveType.Cylinder, armR, At(.02f), new Vector3(.035f, .075f, .035f), dark, new Vector3(110, 0, 0));                        // grip
                Part(PrimitiveType.Sphere, armR, At(-.075f), Vector3.one * .045f, iron);                                                               // pommel
                Part(PrimitiveType.Cube, armR, At(.1f), new Vector3(.13f, .03f, .035f), iron, new Vector3(20, 0, 0));                                 // guard
                Part(PrimitiveType.Cube, armR, At(.36f), new Vector3(.035f, .07f, .5f), iron, new Vector3(20, 0, 0)).name = "Falchion";               // blade
                Part(PrimitiveType.Cube, armR, At(.5f) + new Vector3(0, -.019f, -.007f), new Vector3(.034f, .1f, .2f), iron, new Vector3(20, 0, 0));  // the belly toward the point
            }
            else
            {
                var across = new Vector3(0, Mathf.Cos(20 * Mathf.Deg2Rad), Mathf.Sin(20 * Mathf.Deg2Rad));
                Part(PrimitiveType.Cylinder, armR, At(0), Vector3.one * .05f, dark, new Vector3(110, 0, 0));                                          // grip wrap
                Part(PrimitiveType.Cylinder, armR, At(.2f), new Vector3(.04f, .26f, .04f), wood, new Vector3(110, 0, 0));                             // haft
                Part(PrimitiveType.Capsule, armR, At(.5f), new Vector3(.12f, .16f, .12f), M(.29f, .21f, .13f), new Vector3(110, 0, 0)).name = "Club";  // knob
                for (int k = 0; k < 4; k++)
                    Part(PrimitiveType.Cube, armR, At(.44f + (k % 2) * .1f) + (k < 2 ? new Vector3(k == 0 ? .06f : -.06f, 0, 0) : across * (k == 2 ? .06f : -.06f)),
                        Vector3.one * .03f, iron, new Vector3(20, 0, 45));                                                                                // studs
            }
        }

        /// <summary>
        /// Caddock, the Bandit King (GAME-ONLY): the deserter sergeant who crowned himself. Heavier than his men, in a long dark
        /// coat flaring to the knee and hanging open over company mail, his torn Sandthrone sash across it and knotted at the hip,
        /// one battered shoulder-plate, riding boots, a black beard, and a heavy two-handed cleaver carried low. What you see first
        /// across his hall is the crown: a hammered ring of tin with seven uneven prongs and a bead of red glass, worn crooked.
        /// The camp scales him 1.18 as an elite on top of his own build. GAME-ONLY styling.
        /// </summary>
        void BuildBanditKing()
        {
            Material M(float r, float g, float b, float smooth = .15f, float metal = 0) { return Mat(new Color(r, g, b), smooth, metal); }
            body.localScale = new Vector3(1.16f, 1.03f, 1.12f);
            var coat = cloth; coat.SetFloat("_Glossiness", .3f);   // oiled leather
            var collar = M(.22f, .17f, .14f); var sash = M(.6f, .46f, .27f); var stain = M(.34f, .2f, .12f);
            var iron = M(.36f, .36f, .38f, .4f, .6f); var leather = M(.2f, .14f, .1f); var mail = M(.42f, .42f, .43f, .35f, .55f); var beard = M(.1f, .08f, .07f, .2f);
            // Dull tin that still catches the torchlight, and a bead of red glass.
            var tin = M(.66f, .67f, .64f, .4f, .45f); tin.EnableKeyword("_EMISSION"); tin.SetColor("_EmissionColor", new Color(.07f, .07f, .065f));
            var glass = M(.55f, .1f, .07f, .85f); glass.EnableKeyword("_EMISSION"); glass.SetColor("_EmissionColor", new Color(.14f, .02f, .01f));
            // The long coat: skirts to the knee that flare out, open at the front, and company mail where it hangs open.
            Part(PrimitiveType.Cube, body, new Vector3(0, -.24f, -.19f), new Vector3(.5f, .6f, .035f), coat, new Vector3(8, 0, 0));
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, body, new Vector3(s * .26f, -.24f, 0), new Vector3(.035f, .6f, .36f), coat, new Vector3(0, 0, s * 8));
                Part(PrimitiveType.Cube, body, new Vector3(s * .23f, -.24f, .17f), new Vector3(.09f, .6f, .035f), coat, new Vector3(-8, 0, 0));
            }
            Part(PrimitiveType.Sphere, body, new Vector3(0, .42f, .03f), new Vector3(.19f, .38f, .33f), mail);
            Part(PrimitiveType.Cube, body, new Vector3(0, .66f, -.165f), new Vector3(.34f, .17f, .045f), collar, new Vector3(-14, 0, 0));   // high collar
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, body, new Vector3(s * .15f, .645f, -.07f), new Vector3(.045f, .15f, .17f), collar, new Vector3(-8, 0, 0));
            // The torn Sandthrone sash, right shoulder to left hip: up the chest, over the shoulder, across the back; knotted, the end ripped.
            Part(PrimitiveType.Cube, body, new Vector3(-.08f, .2f, .175f), new Vector3(.1f, .5f, .02f), sash, new Vector3(2.9f, 0, -35));
            Part(PrimitiveType.Cube, body, new Vector3(.14f, .5f, .1325f), new Vector3(.1f, .3f, .02f), sash, new Vector3(-27.8f, 0, -35.3f));
            Part(PrimitiveType.Cube, body, new Vector3(.21f, .635f, -.04f), new Vector3(.1f, .02f, .26f), sash, new Vector3(0, 0, -10));
            Part(PrimitiveType.Cube, body, new Vector3(-.01f, .29f, -.16f), new Vector3(.1f, .74f, .02f), sash, new Vector3(0, 0, -35.9f));
            Part(PrimitiveType.Sphere, body, new Vector3(-.21f, .02f, .175f), new Vector3(.1f, .09f, .06f), sash);                          // knot
            Part(PrimitiveType.Cube, body, new Vector3(-.23f, -.13f, .205f), new Vector3(.08f, .28f, .018f), sash, new Vector3(0, 0, 5));     // ripped ends
            Part(PrimitiveType.Cube, body, new Vector3(-.16f, -.09f, .21f), new Vector3(.06f, .2f, .018f), sash, new Vector3(0, 0, -9));
            Part(PrimitiveType.Cube, body, new Vector3(-.02f, .26f, .19f), new Vector3(.07f, .06f, .006f), stain, new Vector3(2.9f, 0, -35));
            Part(PrimitiveType.Cube, body, new Vector3(0, .06f, .165f), new Vector3(.08f, .065f, .015f), tin);                               // buckle
            // One battered shoulder-plate, tin-riveted; riding-boot tops; a black beard.
            Part(PrimitiveType.Sphere, body, new Vector3(-.3f, .6f, 0), new Vector3(.27f, .19f, .29f), iron);
            Part(PrimitiveType.Sphere, body, new Vector3(-.3f, .665f, .1f), Vector3.one * .035f, tin);
            Part(PrimitiveType.Sphere, body, new Vector3(-.385f, .649f, .085f), Vector3.one * .03f, tin);
            foreach (var leg in new[] { legL, legR }) Part(PrimitiveType.Cylinder, leg, new Vector3(0, -.6f, 0), new Vector3(.22f, .07f, .22f), leather);
            Part(PrimitiveType.Cube, body, new Vector3(0, .66f, .12f), new Vector3(.2f, .14f, .08f), beard);
            Part(PrimitiveType.Cube, body, new Vector3(0, .767f, .148f), new Vector3(.11f, .022f, .02f), beard);                            // moustache
            // The crown of beaten tin: a ring of twelve hammered plates, seven prongs of uneven height, worn crooked.
            var crown = new GameObject("Tin crown").transform; crown.SetParent(body, false);
            crown.localPosition = new Vector3(0, .925f, -.02f); crown.localEulerAngles = new Vector3(4, 0, -6);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30;
                Part(PrimitiveType.Cube, crown, new Vector3(Mathf.Sin(a * Mathf.Deg2Rad), 0, Mathf.Cos(a * Mathf.Deg2Rad)) * .165f, new Vector3(.092f, .05f, .024f), tin, new Vector3(0, a, 0));
            }
            float[] off = { 0, 7, -5, 4, -8, 6, -3 }, tall = { .145f, .1f, .125f, .11f, .15f, .095f, .12f };
            for (int i = 0; i < 7; i++)
            {
                float a = i * 360f / 7 + off[i]; var up = Quaternion.Euler(8, a, 0) * Vector3.up;   // each prong leans a little outward
                var foot = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * .168f, .02f, Mathf.Cos(a * Mathf.Deg2Rad) * .168f);
                Part(PrimitiveType.Cube, crown, foot + up * (tall[i] / 2), new Vector3(.036f, tall[i], .02f), tin, new Vector3(8, a, 0));
                Part(PrimitiveType.Cube, crown, foot + up * tall[i], new Vector3(.028f, .028f, .02f), tin, new Vector3(8, a, 45));        // hammered point
            }
            Part(PrimitiveType.Sphere, crown, new Vector3(0, 0, .18f), new Vector3(.045f, .045f, .03f), glass);
            // The cleaver: a long grip, a broad square blade with a honed edge, carried low and point-forward.
            var cleaver = new GameObject("Cleaver").transform; cleaver.SetParent(armR, false);
            cleaver.localPosition = new Vector3(0, -.62f, 0); cleaver.localEulerAngles = new Vector3(115, 0, 0);   // local y runs along the weapon, z toward its edge
            Part(PrimitiveType.Cylinder, cleaver, new Vector3(0, .12f, 0), new Vector3(.05f, .2f, .05f), leather);                                      // grip
            Part(PrimitiveType.Sphere, cleaver, new Vector3(0, -.1f, 0), Vector3.one * .075f, iron);                                                      // pommel
            Part(PrimitiveType.Cube, cleaver, new Vector3(0, .34f, .04f), new Vector3(.05f, .045f, .17f), iron);                                         // guard
            Part(PrimitiveType.Cube, cleaver, new Vector3(0, .72f, .1f), new Vector3(.035f, .74f, .25f), iron);                                          // blade
            Part(PrimitiveType.Cube, cleaver, new Vector3(0, .72f, .232f), new Vector3(.03f, .74f, .03f), M(.74f, .74f, .76f, .7f, .7f));               // edge
            Part(PrimitiveType.Cube, cleaver, new Vector3(0, 1.07f, -.04f), new Vector3(.03f, .06f, .06f), iron, new Vector3(45, 0, 0));                // spur on the spine
            Part(PrimitiveType.Cylinder, cleaver, new Vector3(0, .98f, .05f), new Vector3(.06f, .02f, .06f), M(.08f, .07f, .07f), new Vector3(0, 0, 90)); // hanging hole
            Part(PrimitiveType.Cube, cleaver, new Vector3(0, .52f, .16f), new Vector3(.037f, .14f, .1f), M(.35f, .2f, .11f));                            // rust
        }

        /// <summary>
        /// Trade outfits: each has a distinct silhouette (hat or hood, apron or coat) and a tool, so a smith reads as a smith
        /// from across the green. Built on the plain villager body (torso/arms use <see cref="cloth"/>). GAME-ONLY styling.
        /// </summary>
        void Dress(string trade, Material skin, Material legs)
        {
            Material M(float r, float g, float b, float smooth = .15f, float metal = 0) { return Mat(new Color(r, g, b), smooth, metal); }
            var iron = M(.32f, .32f, .34f, .55f, .7f); var wood = M(.42f, .3f, .18f); var leather = M(.46f, .24f, .11f);
            void Apron(Material m, float length, float width = .42f) { Part(PrimitiveType.Cube, body, new Vector3(0, .42f - length / 2, .16f), new Vector3(width, length, .03f), m); }
            void Brim(Material m, float width, float crown, float crownH) { Part(PrimitiveType.Cylinder, body, new Vector3(0, .94f, 0), new Vector3(width, .015f, width), m); Part(PrimitiveType.Cylinder, body, new Vector3(0, .94f + crownH, 0), new Vector3(crown, crownH, crown), m); }
            void Beard(Color c, float size) { Part(PrimitiveType.Cube, body, new Vector3(0, .68f - size * .2f, .12f), new Vector3(.22f, .12f + size * .2f, .08f), Mat(c)); }
            void Skirt(Material m) { Part(PrimitiveType.Cylinder, body, new Vector3(0, -.45f, 0), new Vector3(.5f, .42f, .42f), m); }
            // A tool held in the right hand, pointing forward like a carried hammer or axe (it swings with the arm).
            void HandTool(Material handle, float length, Material head, Vector3 headScale, float headAt = 1)
            {
                Part(PrimitiveType.Cylinder, armR, new Vector3(0, -.64f, length / 2 - .08f), new Vector3(.045f, length / 2, .045f), handle, new Vector3(90, 0, 0));
                if (head != null) Part(PrimitiveType.Cube, armR, new Vector3(0, -.64f + headScale.y * .25f, (length - .08f) * headAt), headScale, head);
            }
            switch (trade)
            {
                case "blacksmith":
                    // Broad, soot-dark shirt with bare forearms, a long leather apron, a skullcap, and a hammer.
                    cloth.color = new Color(.24f, .22f, .22f); legs.color = new Color(.2f, .17f, .15f);
                    body.localScale = new Vector3(1.14f, 1.02f, 1.1f);
                    foreach (var arm in new[] { armL, armR }) Part(PrimitiveType.Capsule, arm, new Vector3(0, -.47f, 0), new Vector3(.15f, .15f, .15f), skin);
                    Apron(leather, 1.02f, .46f);
                    Part(PrimitiveType.Cube, body, new Vector3(0, .5f, .16f), new Vector3(.03f, .2f, .02f), leather);       // apron strap
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .87f, -.01f), new Vector3(.31f, .2f, .31f), M(.7f, .13f, .09f)); // skullcap
                    Beard(new Color(.18f, .12f, .08f), .5f);
                    HandTool(wood, .5f, iron, new Vector3(.1f, .1f, .16f));
                    break;
                case "merchant":
                    // Plump, in a long burgundy coat with gold buttons, a feathered wide hat and a coin purse.
                    cloth.color = new Color(.5f, .05f, .12f); legs.color = new Color(.16f, .12f, .2f);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .26f, .06f), new Vector3(.5f, .5f, .38f), cloth);           // belly
                    Part(PrimitiveType.Cylinder, body, new Vector3(0, -.34f, 0), new Vector3(.52f, .3f, .38f), cloth);           // coat skirts
                    var gold = M(.98f, .76f, .22f, .7f, .8f);
                    for (int i = 0; i < 4; i++) Part(PrimitiveType.Sphere, body, new Vector3(0, .42f - i * .14f, .25f - Mathf.Abs(i - 1.5f) * .02f), Vector3.one * .05f, gold);
                    Part(PrimitiveType.Cube, body, new Vector3(0, .6f, .12f), new Vector3(.3f, .08f, .08f), M(.95f, .93f, .86f)); // collar
                    var hatM = M(.12f, .1f, .14f); Brim(hatM, .56f, .28f, .09f);
                    Part(PrimitiveType.Cube, body, new Vector3(.14f, 1.12f, -.08f), new Vector3(.03f, .3f, .06f), M(.1f, .62f, .4f), new Vector3(-30, 0, -25)); // feather
                    Part(PrimitiveType.Sphere, body, new Vector3(-.27f, -.05f, .1f), new Vector3(.12f, .15f, .1f), M(.6f, .42f, .16f)); // purse
                    break;
                case "baker":
                    // All in flour-white: a white cap puffed on top, a white apron, and a loaf under the arm.
                    cloth.color = new Color(.92f, .9f, .84f); legs.color = new Color(.3f, .42f, .62f);
                    var white = M(.97f, .96f, .93f);
                    Apron(white, .92f);
                    Part(PrimitiveType.Cylinder, body, new Vector3(0, .98f, 0), new Vector3(.28f, .08f, .28f), white);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, 1.1f, 0), new Vector3(.38f, .2f, .38f), white);
                    Part(PrimitiveType.Capsule, armL, new Vector3(0, -.55f, .12f), new Vector3(.15f, .2f, .15f), M(.72f, .48f, .22f), new Vector3(90, 0, 0)); // loaf
                    break;
                case "henwife":
                    // Long skirt, a shawl and a red kerchief; a feed pouch on the apron.
                    cloth.color = new Color(.58f, .24f, .2f); Skirt(M(.17f, .29f, .54f));
                    Apron(M(.9f, .86f, .74f), .8f, .36f);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .86f, -.02f), new Vector3(.33f, .28f, .34f), M(.82f, .14f, .1f));   // kerchief
                    Part(PrimitiveType.Cube, body, new Vector3(0, .54f, -.01f), new Vector3(.56f, .16f, .34f), M(.8f, .6f, .2f));     // shawl
                    Part(PrimitiveType.Cube, body, new Vector3(.1f, -.1f, .19f), new Vector3(.16f, .14f, .05f), M(.72f, .62f, .44f)); // feed pouch
                    break;
                case "farmer":
                    // Wide straw hat, a denim bib over a work shirt, and a pitchfork.
                    cloth.color = variant % 2 == 0 ? new Color(.74f, .52f, .2f) : new Color(.3f, .52f, .26f); legs.color = new Color(.17f, .28f, .52f);
                    Part(PrimitiveType.Cube, body, new Vector3(0, .2f, .15f), new Vector3(.36f, .44f, .02f), M(.17f, .28f, .52f));      // bib
                    foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, body, new Vector3(s * .12f, .5f, .15f), new Vector3(.04f, .2f, .02f), M(.17f, .28f, .52f));
                    Brim(M(.9f, .74f, .32f), .62f, .26f, .07f);
                    Part(PrimitiveType.Cylinder, armR, new Vector3(0, -.45f, .08f), new Vector3(.035f, .85f, .035f), wood);
                    for (int i = -1; i <= 1; i++) Part(PrimitiveType.Cube, armR, new Vector3(i * .05f, .47f, .08f), new Vector3(.02f, .2f, .02f), iron);
                    Part(PrimitiveType.Cube, armR, new Vector3(0, .38f, .08f), new Vector3(.14f, .02f, .02f), iron);
                    break;
                case "hunter":
                    // Green hood and cape, leather jerkin, bow and quiver on the back.
                    cloth.color = new Color(.18f, .42f, .17f); legs.color = new Color(.36f, .24f, .13f);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .85f, -.03f), new Vector3(.36f, .37f, .38f), M(.12f, .36f, .15f));  // hood
                    Part(PrimitiveType.Cube, body, new Vector3(0, .32f, .01f), new Vector3(.5f, .44f, .31f), leather);                 // jerkin
                    Part(PrimitiveType.Cube, body, new Vector3(0, .25f, -.17f), new Vector3(.5f, .8f, .03f), M(.12f, .36f, .15f), new Vector3(-6, 0, 0)); // cape
                    Part(PrimitiveType.Cylinder, body, new Vector3(.14f, .45f, -.22f), new Vector3(.13f, .24f, .13f), leather, new Vector3(0, 0, -18)); // quiver
                    for (int i = 0; i < 3; i++) Part(PrimitiveType.Cube, body, new Vector3(.2f + i * .03f, .72f, -.22f), new Vector3(.03f, .06f, .03f), M(.82f, .18f, .12f), new Vector3(0, 0, -18));
                    Part(PrimitiveType.Cube, body, new Vector3(-.05f, .3f, -.21f), new Vector3(.035f, 1.25f, .035f), wood, new Vector3(0, 0, 24));   // bow
                    Part(PrimitiveType.Cube, body, new Vector3(-.02f, .3f, -.19f), new Vector3(.008f, 1.15f, .008f), M(.85f, .82f, .7f), new Vector3(0, 0, 24)); // string
                    break;
                case "leatherworker":
                    // Tan work clothes, a leather apron and cap, a satchel on a cross-strap, and a roll of hide.
                    cloth.color = new Color(.76f, .52f, .24f); legs.color = new Color(.36f, .24f, .14f);
                    Apron(M(.56f, .24f, .12f), .8f);
                    Part(PrimitiveType.Cylinder, body, new Vector3(0, .93f, 0), new Vector3(.31f, .05f, .31f), leather);                // cap
                    Part(PrimitiveType.Cube, body, new Vector3(0, .32f, .165f), new Vector3(.05f, .75f, .02f), leather, new Vector3(0, 0, 38)); // strap
                    Part(PrimitiveType.Cube, body, new Vector3(-.3f, -.02f, 0), new Vector3(.1f, .24f, .28f), leather);                 // satchel
                    Part(PrimitiveType.Cylinder, armL, new Vector3(0, -.58f, .12f), new Vector3(.14f, .22f, .14f), M(.7f, .55f, .36f), new Vector3(90, 0, 0)); // hide roll
                    break;
                case "skinner":
                    // Fur hat, a stained apron, pelts over the shoulder and a skinning knife.
                    cloth.color = new Color(.44f, .3f, .2f); legs.color = new Color(.27f, .2f, .15f);
                    Apron(M(.7f, .62f, .46f), .9f);
                    var stain = M(.5f, .08f, .06f);
                    Part(PrimitiveType.Cube, body, new Vector3(.08f, .05f, .18f), new Vector3(.14f, .1f, .01f), stain);
                    Part(PrimitiveType.Cube, body, new Vector3(-.1f, -.25f, .18f), new Vector3(.1f, .14f, .01f), stain);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .9f, 0), new Vector3(.36f, .26f, .36f), M(.5f, .38f, .26f));        // fur hat
                    Part(PrimitiveType.Sphere, body, new Vector3(-.22f, .55f, -.1f), new Vector3(.34f, .16f, .5f), M(.55f, .42f, .3f), new Vector3(0, 0, 30)); // pelt
                    Part(PrimitiveType.Sphere, body, new Vector3(-.12f, .45f, -.18f), new Vector3(.4f, .12f, .3f), M(.4f, .4f, .38f), new Vector3(0, 0, 20));  // pelt
                    HandTool(wood, .22f, M(.75f, .76f, .78f, .7f, .8f), new Vector3(.02f, .05f, .2f), 1.6f);
                    break;
                case "lumberjack":
                    // Red plaid shirt, knit cap, big beard, and an axe.
                    cloth.color = new Color(.64f, .1f, .08f); legs.color = new Color(.18f, .26f, .46f);
                    body.localScale = new Vector3(1.1f, 1.04f, 1.08f);
                    var check = M(.1f, .06f, .06f);
                    foreach (float y in new[] { .15f, .42f }) foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, body, new Vector3(0, y, s * .147f), new Vector3(.49f, .05f, .005f), check);
                    foreach (float x in new[] { -.12f, .12f }) foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, body, new Vector3(x, .3f, s * .148f), new Vector3(.04f, .6f, .005f), check);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .9f, 0), new Vector3(.32f, .26f, .32f), M(.16f, .38f, .2f));        // knit cap
                    Part(PrimitiveType.Cylinder, body, new Vector3(0, .84f, 0), new Vector3(.33f, .04f, .33f), M(.13f, .32f, .17f));
                    Beard(new Color(.42f, .24f, .12f), 1);
                    HandTool(wood, .8f, iron, new Vector3(.04f, .22f, .18f));
                    break;
                case "herbalist":
                    // Sage robe with a moss shawl and hood, a herb satchel with sprigs, and a small sickle.
                    cloth.color = new Color(.3f, .52f, .26f); Skirt(M(.2f, .42f, .22f));
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .85f, -.03f), new Vector3(.35f, .36f, .37f), M(.16f, .42f, .2f));    // hood
                    Part(PrimitiveType.Cube, body, new Vector3(0, .54f, -.01f), new Vector3(.56f, .16f, .34f), M(.16f, .42f, .2f));      // shawl
                    Part(PrimitiveType.Cube, body, new Vector3(-.3f, -.02f, .02f), new Vector3(.12f, .22f, .26f), M(.55f, .48f, .32f)); // satchel
                    for (int i = 0; i < 4; i++) Part(PrimitiveType.Cube, body, new Vector3(-.3f, .14f, -.06f + i * .05f), new Vector3(.02f, .14f, .02f), M(.3f, .7f, .22f), new Vector3(i * 8 - 12, 0, 0));
                    Part(PrimitiveType.Sphere, body, new Vector3(-.3f, .21f, 0), Vector3.one * .05f, M(.96f, .76f, .18f));                // a flower
                    HandTool(wood, .2f, M(.7f, .7f, .72f, .6f, .7f), new Vector3(.02f, .14f, .16f), 1.5f);
                    break;
                case "miller":
                    // Flour-dusted shirt and cap, a sack of meal on the shoulder.
                    cloth.color = new Color(.86f, .82f, .72f); legs.color = new Color(.5f, .36f, .2f);
                    Part(PrimitiveType.Cylinder, body, new Vector3(0, .93f, .02f), new Vector3(.32f, .05f, .34f), M(.26f, .4f, .62f));
                    Part(PrimitiveType.Cube, body, new Vector3(0, .93f, .16f), new Vector3(.22f, .02f, .1f), M(.26f, .4f, .62f));       // cap peak
                    Part(PrimitiveType.Sphere, body, new Vector3(-.22f, .74f, -.06f), new Vector3(.3f, .4f, .26f), M(.74f, .66f, .5f), new Vector3(0, 0, 20)); // sack
                    Apron(M(.92f, .9f, .82f), .7f);
                    break;
                case "elder":
                    // Grey hair, a shawl, a walking stick and a slight stoop.
                    cloth.color = new Color(.4f, .22f, .48f);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .84f, -.03f), new Vector3(.31f, .3f, .32f), M(.78f, .78f, .76f));   // grey hair
                    Part(PrimitiveType.Cube, body, new Vector3(0, .54f, -.01f), new Vector3(.56f, .16f, .34f), M(.66f, .38f, .18f));     // shawl
                    Beard(new Color(.8f, .8f, .78f), .8f);
                    Part(PrimitiveType.Cylinder, armR, new Vector3(0, -.95f, .08f), new Vector3(.03f, .36f, .03f), wood);
                    stoop = 8;
                    break;
                case "stranger":
                    // A Salt-Mender contact: charcoal cloak and deep hood hiding the face, a salt pouch at the belt.
                    cloth.color = new Color(.2f, .2f, .22f); legs.color = new Color(.16f, .15f, .15f);
                    var cloak = M(.17f, .17f, .19f);
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .85f, -.02f), new Vector3(.38f, .4f, .4f), cloak);                 // hood
                    Part(PrimitiveType.Cube, body, new Vector3(0, .8f, .13f), new Vector3(.22f, .24f, .06f), M(.04f, .04f, .05f));     // shadowed face
                    Part(PrimitiveType.Cube, body, new Vector3(0, .1f, -.17f), new Vector3(.56f, 1.15f, .05f), cloak, new Vector3(-5, 0, 0));
                    foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, body, new Vector3(s * .27f, .15f, 0), new Vector3(.05f, 1.05f, .3f), cloak);
                    Part(PrimitiveType.Sphere, body, new Vector3(.22f, -.02f, .14f), new Vector3(.12f, .13f, .1f), M(.92f, .92f, .9f)); // salt pouch
                    break;
                case "pilgrim":
                    // A Silent Pilgrim (CANON look, book3 ch.5): layered linen robes in ochre and sand, a polished silver mask over mouth and
                    // nose, and a long brass listening-tube in the hand.
                    cloth.color = new Color(.86f, .58f, .16f); legs.color = new Color(.7f, .56f, .34f);
                    Part(PrimitiveType.Cube, body, new Vector3(0, .12f, 0), new Vector3(.52f, .9f, .34f), M(.9f, .76f, .44f));                 // the outer robe, layered
                    Part(PrimitiveType.Cube, body, new Vector3(0, -.2f, 0), new Vector3(.56f, .5f, .38f), M(.76f, .44f, .12f));
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .86f, -.02f), new Vector3(.34f, .3f, .34f), M(.84f, .62f, .24f));              // a linen head-wrap
                    Part(PrimitiveType.Cube, body, new Vector3(0, .77f, .14f), new Vector3(.17f, .1f, .05f), M(.85f, .86f, .9f, .9f, .8f));       // the silver mask
                    Part(PrimitiveType.Cylinder, armR, new Vector3(0, -.62f, .1f), new Vector3(.035f, .42f, .035f), M(.9f, .62f, .2f, .7f, .7f), new Vector3(70, 0, 0));   // the listening-tube
                    Part(PrimitiveType.Cylinder, armR, new Vector3(0, -.62f + .4f * Mathf.Cos(70 * Mathf.Deg2Rad), .1f + .4f * Mathf.Sin(70 * Mathf.Deg2Rad)), new Vector3(.08f, .04f, .08f), M(.9f, .62f, .2f, .7f, .7f), new Vector3(70, 0, 0));   // its bell
                    break;
                case "warden":
                    // A Preservationist Warden (CANON look, book1 ch.5): green and brown, armour of living vine, an iron-wood staff.
                    cloth.color = new Color(.2f, .46f, .18f); legs.color = new Color(.36f, .24f, .13f);
                    var vine = M(.12f, .5f, .15f); var bark = M(.3f, .22f, .14f);
                    for (int i = 0; i < 5; i++) Part(PrimitiveType.Cube, body, new Vector3(0, .05f + i * .13f, 0), new Vector3(.5f, .05f, .31f), vine, new Vector3(0, 0, i % 2 == 0 ? 12 : -12));
                    foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Sphere, body, new Vector3(s * .31f, .58f, 0), new Vector3(.24f, .16f, .24f), bark);
                    for (int i = 0; i < 6; i++) Part(PrimitiveType.Sphere, body, new Vector3(Mathf.Sin(i) * .24f, .1f + i * .1f, .16f), Vector3.one * .06f, M(.4f, .74f, .2f)); // leaves
                    Part(PrimitiveType.Sphere, body, new Vector3(0, .86f, -.02f), new Vector3(.34f, .3f, .35f), M(.16f, .38f, .14f));   // hood
                    Part(PrimitiveType.Cylinder, armR, new Vector3(0, -.4f, .08f), new Vector3(.06f, .95f, .06f), M(.24f, .24f, .22f, .4f, .3f)); // iron-wood staff
                    Part(PrimitiveType.Sphere, armR, new Vector3(0, .55f, .08f), new Vector3(.14f, .1f, .14f), vine);
                    break;
                case "innkeeper":
                    // Shirt-sleeves rolled to the elbow, a brown waistcoat, a long white apron, a red cloth over the shoulder and keys at the belt.
                    cloth.color = new Color(.9f, .86f, .74f); legs.color = new Color(.26f, .2f, .17f);
                    foreach (var arm in new[] { armL, armR }) Part(PrimitiveType.Capsule, arm, new Vector3(0, -.47f, 0), new Vector3(.15f, .15f, .15f), skin);
                    Part(PrimitiveType.Cube, body, new Vector3(0, .36f, 0), new Vector3(.5f, .46f, .31f), M(.13f, .38f, .24f));                 // waistcoat
                    Apron(M(.95f, .93f, .86f), .86f);
                    Part(PrimitiveType.Cube, body, new Vector3(-.19f, .6f, 0), new Vector3(.1f, .04f, .34f), M(.82f, .14f, .1f));            // the cloth over the shoulder
                    Part(PrimitiveType.Cube, body, new Vector3(-.19f, .48f, .17f), new Vector3(.1f, .22f, .02f), M(.82f, .14f, .1f));
                    Part(PrimitiveType.Cylinder, body, new Vector3(.25f, -.04f, .08f), new Vector3(.1f, .01f, .1f), iron, new Vector3(0, 0, 90)); // ring of keys
                    Beard(new Color(.36f, .24f, .14f), .3f);
                    break;
                case "drinker":
                    // (The tankard is not part of the outfit: it is in the hand only at the inn, see Villager.DrinkRound.)
                    if (variant % 2 == 0) Part(PrimitiveType.Sphere, body, new Vector3(0, .84f, -.02f), new Vector3(.33f, .3f, .34f), accent);
                    break;
            }
            body.localEulerAngles = new Vector3(stoop, 0, 0);
        }

        void LateUpdate()
        {
            if (core != null) { Drift(); return; }
            if (body == null || legL == null) return;
            // Swimming: the kit goes on the back, so no sword stands out of the water like a mast.
            bool stow = Pose == ActorPose.Swim;
            if (held != null && stow != gearStowed)
            {
                gearStowed = stow;
                foreach (var g in held) g.gameObject.SetActive(!stow);
                if (stowed != null) foreach (var g in stowed) g.gameObject.SetActive(stow);
            }
            var delta = transform.position - lastPosition; delta.y = 0; lastPosition = transform.position;
            float target = Time.deltaTime > 0 ? delta.magnitude / Time.deltaTime : 0;
            speed = Mathf.Lerp(speed, target, Time.deltaTime * 8);
            float stride = Mathf.Clamp01(speed / 4.5f);
            phase += Time.deltaTime * (2.2f + speed * 1.6f);
            float swing = Mathf.Sin(phase) * 34 * stride, idle = Mathf.Sin(Time.time * 1.7f) * 2 * (1 - stride);
            legL.localEulerAngles = new Vector3(swing, 0, 0); legR.localEulerAngles = new Vector3(-swing, 0, 0);
            armL.localEulerAngles = new Vector3(-swing * .8f + idle, 0, 3); armR.localEulerAngles = new Vector3(swing * .8f - idle, 0, -3);
            if (torso != null && !beast) torso.localPosition = new Vector3(0, .3f + Mathf.Abs(Mathf.Sin(phase)) * .025f * stride, 0);
            if (spine != null) { spine.localPosition = SmoothBody.Rest[SmoothBody.Spine] + new Vector3(0, torso.localPosition.y - .3f, 0); WalkJoints(stride); }
            if (gearDriven && torso != null) GearBob(torso.localPosition.y - .3f);
            if (beast && LyingLow) { armL.localEulerAngles = armR.localEulerAngles = new Vector3(80, 0, 0); legL.localEulerAngles = legR.localEulerAngles = new Vector3(-80, 0, 0); }   // lying in wait: legs folded under the belly
            if (Pose == ActorPose.None)   // combatants never use poses (their death pose moves the body)
            {
                if (posed) { posed = false; swimLean = 0; body.localPosition = Vector3.zero; body.localEulerAngles = new Vector3(stoop, 0, 0); }
                if (Stagger > 0) { staggering = true; body.localEulerAngles = new Vector3(stoop + Mathf.Sin(phase * .5f) * 6 * Stagger, 0, Mathf.Sin(phase * .5f + 1.1f) * 14 * Stagger); }
                else if (staggering) { staggering = false; body.localEulerAngles = new Vector3(stoop, 0, 0); }
                return;
            }
            posed = true;
            bool travelPose = Pose == ActorPose.Swim || Pose == ActorPose.Sneak;   // these belong to moving, not standing still
            if (stride > .3f && !travelPose) { body.localPosition = Vector3.zero; body.localEulerAngles = new Vector3(stoop, 0, 0); return; }   // moving: walking wins over any pose
            float t = Time.time + variant * .7f; float lean = stoop;
            switch (Pose)
            {
                case ActorPose.Swim:     // breaststroke-ish: lying forward, arms sweeping in turn, legs kicking
                {
                    swimPhase += Time.deltaTime * (stride > .1f ? 4.5f : 2);
                    armL.localEulerAngles = new Vector3(-120 + Mathf.Sin(swimPhase) * 70, 0, 25); armR.localEulerAngles = new Vector3(-120 + Mathf.Sin(swimPhase + Mathf.PI) * 70, 0, -25);
                    legL.localEulerAngles = new Vector3(Mathf.Sin(swimPhase * 1.6f) * 25, 0, 0); legR.localEulerAngles = new Vector3(-Mathf.Sin(swimPhase * 1.6f) * 25, 0, 0);
                    Bend(18 + Mathf.Max(0, Mathf.Cos(swimPhase * 1.6f)) * 30, 18 + Mathf.Max(0, -Mathf.Cos(swimPhase * 1.6f)) * 30, -20, -20);
                    swimLean = Mathf.MoveTowards(swimLean, stride > .1f ? 55 : 18, 150 * Time.deltaTime);
                    // Lean about the chest, not the hips, so the head stays up out of the water.
                    var chest = new Vector3(0, .45f, 0); var tilt = Quaternion.Euler(swimLean, 0, 0);
                    body.localRotation = tilt; body.localPosition = chest - tilt * chest;
                    return;
                }
                case ActorPose.Sneak:    // crouched, knees bent, arms held close
                    legL.localEulerAngles = new Vector3(-35 + swing * .6f, 0, 0); legR.localEulerAngles = new Vector3(-35 - swing * .6f, 0, 0);
                    armL.localEulerAngles = new Vector3(-30, 0, 10); armR.localEulerAngles = new Vector3(-30, 0, -10);
                    Bend(55 - swing * .5f, 55 + swing * .5f, -35, -35);
                    lean += 22; break;
                case ActorPose.Hammer:   // steady strikes on the anvil with the right hand; the left holds the work
                {
                    float k = Mathf.Repeat(t * 1.3f, 1), lift = k < .7f ? Mathf.SmoothStep(0, 1, k / .7f) : 1 - (k - .7f) / .3f;
                    armR.localEulerAngles = new Vector3(-20 - lift * 110, 0, -8); armL.localEulerAngles = new Vector3(-45, 0, 12); lean += 8; break;
                }
                case ActorPose.Chop:     // a big overhead swing, then a pause to set the next log
                {
                    float k = Mathf.Repeat(t * .8f, 1), axe = k < .55f ? Mathf.SmoothStep(0, 1, k / .55f) * 160 : Mathf.Max(0, 160 - (k - .55f) * 900);
                    armL.localEulerAngles = armR.localEulerAngles = new Vector3(-axe, 0, 0); lean += axe < 40 ? 14 : 0; break;
                }
                case ActorPose.Gather:   // bent over, picking at the ground
                    Bend(25, 25, -25, -30);
                    armL.localEulerAngles = new Vector3(-50 + Mathf.Sin(t * 2) * 12, 0, 8); armR.localEulerAngles = new Vector3(-55 + Mathf.Sin(t * 2.6f + 1) * 16, 0, -8);
                    lean += 38; break;
                case ActorPose.Knead:    // both hands pushing forward in turn (dough, hides, a stall's wares)
                    armL.localEulerAngles = new Vector3(-60 - Mathf.Max(0, Mathf.Sin(t * 3)) * 25, 0, 6); armR.localEulerAngles = new Vector3(-60 - Mathf.Max(0, Mathf.Sin(t * 3 + Mathf.PI)) * 25, 0, -6);
                    lean += 10; break;
                case ActorPose.Work:   // both arms chop down together, like hoeing or hauling a bucket
                    float chop = Mathf.Abs(Mathf.Sin(t * 2.4f)) * 70;
                    armL.localEulerAngles = new Vector3(-chop, 0, 10); armR.localEulerAngles = new Vector3(-chop, 0, -10); break;
                case ActorPose.Talk:   // an occasional gesture with one hand
                    float gesture = Mathf.Max(0, Mathf.Sin(t * 1.3f)) * 55;
                    armR.localEulerAngles = new Vector3(-gesture, 0, -12 - gesture * .2f); break;
                case ActorPose.Sit:
                    legL.localEulerAngles = legR.localEulerAngles = new Vector3(-80, 0, 0); Bend(85, 85, -30, -30);
                    armL.localEulerAngles = new Vector3(-35, 0, 8); armR.localEulerAngles = new Vector3(-35 + Mathf.Sin(t) * 10, 0, -8); break;
                case ActorPose.Cower:
                    armL.localEulerAngles = new Vector3(-150, 0, 25); armR.localEulerAngles = new Vector3(-150, 0, -25); break;
                case ActorPose.Drink:    // seated with a tankard: the right arm lifts it to the mouth every few seconds, the head tipping back
                {
                    legL.localEulerAngles = legR.localEulerAngles = new Vector3(-80, 0, 0); Bend(85, 85, -30, -20);
                    float k = Mathf.Repeat(t * .28f, 1), lift = k < .18f ? Mathf.SmoothStep(0, 1, k / .18f) : k < .45f ? 1 : k < .6f ? 1 - Mathf.SmoothStep(0, 1, (k - .45f) / .15f) : 0;
                    armL.localEulerAngles = new Vector3(-35, 0, 8); armR.localEulerAngles = new Vector3(-40 - lift * 85, 0, -8 + lift * 18);
                    lean -= lift * 8; break;
                }
                case ActorPose.Slump:    // passed out over the table: folded forward, arms out, breathing slow
                    legL.localEulerAngles = legR.localEulerAngles = new Vector3(-80, 0, 0); Bend(85, 85, -15, -15);
                    armL.localEulerAngles = new Vector3(-110, 0, 22); armR.localEulerAngles = new Vector3(-105, 0, -26);
                    lean += 62 + Mathf.Sin(t * .6f) * 1.5f; break;
            }
            // Sitting lowers the whole figure onto the seat; everything else stands.
            body.localPosition = new Vector3(0, Pose == ActorPose.Sit || Pose == ActorPose.Drink || Pose == ActorPose.Slump ? -.45f * (child ? .66f : 1) : Pose == ActorPose.Sneak ? -.28f : Pose == ActorPose.Swim ? -.15f : 0, 0);
            body.localEulerAngles = new Vector3(lean, 0, 0);
        }
        /// <summary>
        /// The smooth figure's knees, ankles and elbows for this frame of the walk (playtest note 12): a knee bends most as its leg
        /// swings through and a little as it takes the weight, the foot stays nearly level, an elbow trails more as its arm swings
        /// forward. A limb that carries something rigid (a weapon or shield in hand, worn armour, a tankard) stays nearly straight,
        /// so the thing still sits on it, until worn gear follows the joints.
        /// </summary>
        void WalkJoints(float stride)
        {
            float c = Mathf.Cos(phase), kneeL = (Mathf.Max(0, -c) * 50 + 5) * stride, kneeR = (Mathf.Max(0, c) * 50 + 5) * stride;
            float elbowL = -(5 + Mathf.Max(0, -Signed(armL.localEulerAngles.x)) * .55f), elbowR = -(5 + Mathf.Max(0, -Signed(armR.localEulerAngles.x)) * .55f);
            Bend(kneeL, kneeR, elbowL, elbowR);
        }
        /// <summary>Sets the knees (positive bends the shin back), the elbows (negative brings the forearm forward) and levels the
        /// feet; a limb carrying something rigid keeps within a few degrees of straight.</summary>
        void Bend(float kneeL, float kneeR, float elbowL, float elbowR)
        {
            if (shinL == null) return;
            if (Carries(legL)) kneeL = Mathf.Min(kneeL, 6); if (Carries(legR)) kneeR = Mathf.Min(kneeR, 6);
            if (Carries(armL)) elbowL = Mathf.Max(elbowL, -5); if (Carries(armR)) elbowR = Mathf.Max(elbowR, -5);
            shinL.localEulerAngles = new Vector3(kneeL, 0, 0); shinR.localEulerAngles = new Vector3(kneeR, 0, 0);
            footL.localEulerAngles = new Vector3(-(Signed(legL.localEulerAngles.x) + kneeL) * .6f, 0, 0);
            footR.localEulerAngles = new Vector3(-(Signed(legR.localEulerAngles.x) + kneeR) * .6f, 0, 0);
            foreL.localEulerAngles = new Vector3(elbowL, 0, 0); foreR.localEulerAngles = new Vector3(elbowR, 0, 0);
        }
        static float Signed(float degrees) { return degrees > 180 ? degrees - 360 : degrees; }
        /// <summary>True when something rigid hangs from this shoulder or hip pivot besides the limb's own lower bone: a held
        /// weapon or shield, worn armour's limb root, a tankard.</summary>
        bool Carries(Transform pivot)
        {
            for (int i = 0; i < pivot.childCount; i++)
            {
                var c = pivot.GetChild(i);
                if (c == foreL || c == foreR || c == shinL || c == shinR || !c.gameObject.activeSelf) continue;
                if (c.childCount > 0 || c.GetComponent<Renderer>() != null) return true;
            }
            return false;
        }
    }
}
