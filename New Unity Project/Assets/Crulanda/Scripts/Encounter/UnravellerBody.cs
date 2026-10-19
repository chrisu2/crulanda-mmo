using UnityEngine;
using UnityEngine.Rendering;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Unraveller's body (Chris, play note 256: "need a better mob skin/monster skin for the boss"; then "maybe use this with the purple
    /// spike or some conglomeration. myassets free fantasy spider"; and for her look, "Both": a creature body wrapped in the books' look).
    /// A conglomeration, GAME-ONLY in its making, of three things:
    /// - the body: Gergely Zsolnay's Free Fantasy Spider (Asset Store, Kalamona; Resources/Props/fantasySpider, its legacy clips: idle, walk,
    ///   attack1/2, hit1/2, death1), huge (about <see cref="SpiderHeight"/> m to its back, legs spread to about ten), its hide turned to void:
    ///   near black with a faint violet light in it;
    /// - the purple spikes Chris liked: the Weave-Eaters' own tangle of fraying threads with burning tips (ActorVisual's Weave-Eater core)
    ///   on her back as a small crown (its own size: raid 4, Chris: "make it smaller and blink on and off randomly"), showing for a short
    ///   random spell and gone for a longer one, fading in and out (<see cref="CrownOn"/>, <see cref="CrownOff"/>), apart from the wrap's eruptions;
    /// - the books' wrap (VoidWrap; b1 ch.20:119): a thin translucent flickering edge of jagged void round her legs and back, a shimmering
    ///   aether rim, ash where she moves, the jerk of her blink (ch.20:131); never over 1.5 times her height, so the spider reads first.
    /// Her rising and sinking (EncounterSession.Unraveller) lift and lower the whole; the spider walks when she moves, strikes when she swings,
    /// jolts when she blinks, and dies in its own death (the crown and the wrap gone to crystal: salt's work, ch.20:205). Built in world units
    /// under her (her actor's 2.6 scale undone). With the package missing, the crown and the wrap alone.
    /// </summary>
    public sealed class UnravellerBody : MonoBehaviour
    {
        public const string ModelPath = "Props/fantasySpider/spider_myOldOne", MaterialPath = "Props/fantasySpider/Materials/spider_black_Std", TexturePath = "Props/fantasySpider/spider_01_black";
        /// <summary>Ground to the spider's back, and the most its legs may spread across, in metres; the wrap's height.</summary>
        public const float SpiderHeight = 3.4f, SpiderSpan = 10, CrownScale = 1;
        /// <summary>The crown's blink: seconds shown (least, most), seconds gone (least, most), and its fade.</summary>
        public static readonly Vector2 CrownOn = new Vector2(.6f, 1.5f), CrownOff = new Vector2(2, 5); public const float CrownFade = .15f;
        float crownShowFrom, crownHideFrom; Renderer[] crownParts;
        /// <summary>How much of the crown shows now (0 gone, 1 whole).</summary>
        public float CrownShown { get; private set; }
        /// <summary>Her body's reach out from her centre: melee strikes her from there (EncounterEnemy.BodyRadius; raid 2).</summary>
        public const float BodyRadius = 2.5f;
        /// <summary>The wrap's tallest shard: never over one and a half times her body (raid 2: the seven-metre wrap hid the spider and the view).</summary>
        public const float WrapHeight = SpiderHeight * 1.2f, WrapLimit = SpiderHeight * 1.5f;
        /// <summary>The model's turn to face her forward (the file's front is not checked in a build yet: PROVISIONAL).</summary>
        public static float FaceYaw = 0;
        public static readonly Color Hide = new Color(.16f, .12f, .22f), HideGlow = new Color(.16f, .05f, .3f);
        EncounterEnemy enemy; Transform root, spider, crown; Animation anim; VoidWrap wrap; Vector3 lastPos; float lastSwingIn, jolted; bool died;
        /// <summary>The spider came in (the package is there).</summary>
        public bool HasSpider { get { return spider != null; } }
        public VoidWrap Wrap { get { return wrap; } }
        /// <summary>The clip playing now (the tests and the capture).</summary>
        public string Playing { get; private set; }

        /// <summary>Builds her body on <paramref name="e"/> (after her scale is set).</summary>
        public static UnravellerBody Dress(EncounterEnemy e)
        {
            var b = e.gameObject.AddComponent<UnravellerBody>(); b.enemy = e; b.Build(); return b;
        }
        void Build()
        {
            root = new GameObject("Unraveller body").transform; root.SetParent(transform, false);
            var s = transform.lossyScale; root.localScale = new Vector3(1 / Mathf.Max(.01f, s.x), 1 / Mathf.Max(.01f, s.y), 1 / Mathf.Max(.01f, s.z));
            float ground = -1;   // an actor stands a metre over its feet
            var src = Resources.Load<GameObject>(ModelPath);
            if (src != null)
            {
                spider = Instantiate(src, root, false).transform; spider.name = "Spider"; spider.localRotation = Quaternion.Euler(0, FaceYaw, 0);
                foreach (var c in spider.GetComponentsInChildren<Collider>(true)) Destroy(c);
                var hide = HideMaterial();
                var b = new Bounds(); bool any = false;
                foreach (var r in spider.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = hide; r.sharedMaterials = mats;
                    r.shadowCastingMode = ShadowCastingMode.On; r.lightProbeUsage = LightProbeUsage.BlendProbes;
                    if (r is SkinnedMeshRenderer sk) sk.updateWhenOffscreen = false;
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
                if (any && b.size.y > .01f)
                {
                    float k = Mathf.Min(SpiderHeight / b.size.y, SpiderSpan / Mathf.Max(.01f, Mathf.Max(b.size.x, b.size.z)));
                    spider.localScale *= k;
                    float feet = (b.min.y - root.position.y) * k;   // the model's lowest point, scaled about its pivot, from her origin
                    spider.localPosition = new Vector3(0, ground - feet, 0);
                }
                anim = spider.GetComponentInChildren<Animation>();
                Play("idle");
            }
            // The purple spikes: the Weave-Eater's tangle (ActorVisual's core) moved to her back as a crown.
            var body = transform.Find("Body"); var core = body != null ? body.Find("Core") : null;
            crown = new GameObject("Crown").transform; crown.SetParent(root, false);
            crown.localPosition = new Vector3(0, ground + (spider != null ? SpiderHeight * .9f : 2.6f), spider != null ? -.4f : 0); crown.localScale = Vector3.one * CrownScale;
            if (core != null) { core.SetParent(crown, false); core.localPosition = Vector3.zero; }
            crownParts = crown.GetComponentsInChildren<Renderer>(true);
            crownShowFrom = Time.time + Random.Range(CrownOff.x, CrownOff.y); crownHideFrom = crownShowFrom + Random.Range(CrownOn.x, CrownOn.y); SetCrown(0);
            wrap = VoidWrap.Wrap(root, WrapHeight, spider != null ? SpiderSpan * .35f : 1.8f, ground, enemy != null ? enemy.persistentId.GetHashCode() : 7);   // round her legs' and back's edge
            lastPos = transform.position;
        }
        static Material hideMat;
        /// <summary>The spider's hide turned to void: the package's Standard material (its cutout, its normal map), near black, a violet glow in it.</summary>
        static Material HideMaterial()
        {
            if (hideMat != null) return hideMat;
            var pack = Resources.Load<Material>(MaterialPath);
            var m = pack != null ? new Material(pack) : new Material(Shader.Find("Standard"));
            if (pack == null)
            {
                var tex = Resources.Load<Texture2D>(TexturePath); if (tex != null) m.mainTexture = tex;
                m.SetFloat("_Mode", 1); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cutoff", .18f); m.renderQueue = 2450;
            }
            m.name = "Unraveller hide"; m.color = Hide; m.SetFloat("_Glossiness", .35f); m.SetFloat("_Metallic", .1f);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", HideGlow);
            return hideMat = m;
        }
        /// <summary>The crown blinks on and off at random, fading quickly; dead, it stands (crystal).</summary>
        void TickCrown()
        {
            if (crown == null) return;
            float t = Time.time, show;
            if (!enemy.actor.IsAlive) show = 1;
            else
            {
                if (t >= crownHideFrom + CrownFade) { crownShowFrom = t + Random.Range(CrownOff.x, CrownOff.y); crownHideFrom = crownShowFrom + Random.Range(CrownOn.x, CrownOn.y); }
                show = t < crownShowFrom ? 0 : t < crownShowFrom + CrownFade ? (t - crownShowFrom) / CrownFade : t < crownHideFrom ? 1 : 1 - (t - crownHideFrom) / CrownFade;
            }
            SetCrown(Mathf.Clamp01(show));
        }
        void SetCrown(float show)
        {
            CrownShown = show; crown.localScale = Vector3.one * CrownScale * Mathf.Max(.001f, show);
            bool on = show > 0; if (crownParts != null) foreach (var r in crownParts) if (r != null && r.enabled != on) r.enabled = on;
        }
        void Play(string clip, float fade = .25f)
        {
            if (anim == null || anim[clip] == null || Playing == clip && anim.IsPlaying(clip)) return;
            Playing = clip; anim.CrossFade(clip, fade);
        }
        void LateUpdate()
        {
            if (enemy == null || root == null) return;
            var s = enemy.session;
            // Rising out of the river and sinking back into it: the whole body, from under the water.
            root.localPosition = new Vector3(0, s != null && s.UnravellerFoe == enemy ? s.UnravellerDepth / Mathf.Max(.01f, transform.lossyScale.y) : 0, 0);
            TickCrown();
            if (!enemy.actor.IsAlive)
            {
                if (!died) { died = true; if (anim != null && anim["death1"] != null) { anim.Stop(); anim.Play("death1"); Playing = "death1"; } }
                return;
            }
            died = false;
            var at = transform.position; float moved = (at - lastPos).magnitude; lastPos = at;
            float swingIn = enemy.NextSwingIn;
            // A swing: its timer jumped up. The blink: more than 2.5 m in a frame (the wrap leaves its afterimage): a jolt.
            if (moved > 2.5f) { jolted = Time.time + .35f; if (anim != null && anim["hit2"] != null) { anim.Stop(); anim.Play("hit2"); Playing = "hit2"; } }
            else if (swingIn > lastSwingIn + .5f && enemy.Engaged) Play(Random.value < .5f ? "attack1" : "attack2", .1f);
            else if (Time.time > jolted && (Playing == null || !Playing.StartsWith("attack") || anim == null || !anim.IsPlaying(Playing)))
                Play(moved > Time.deltaTime * .4f ? "walk" : "idle");
            lastSwingIn = swingIn;
        }
    }
}
