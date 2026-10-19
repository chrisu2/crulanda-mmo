using UnityEngine;
using UnityEngine.Rendering;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Her brood's body (Chris, play note 260: "she needs adds. smaller spiders with the original texture but purple"): the Free Fantasy
    /// Spider again (UnravellerBody.ModelPath), about <see cref="Length"/> m long, in the package's own painted hide (spider_01.tga) tinted
    /// purple, scuttling on the walk clip, biting on attack1/attack2, dying on death1. Salt turns it white crystal while it stands calcified
    /// (EncounterEnemy.Calcified: the next blow shatters it, ch.20:205-207), and a shattered one dies white. The Weave-Eater's own tangle
    /// it was built with is hidden (the Weave-Eaters' rules still hold it: its name is a Weave-Eater's). GAME-ONLY in its making.
    /// </summary>
    public sealed class BroodBody : MonoBehaviour
    {
        public const string TexturePath = "Props/fantasySpider/spider_01";
        /// <summary>Its length, nose to rump with its legs (metres).</summary>
        public const float Length = 1.3f;
        public static readonly Color Purple = new Color(.72f, .42f, 1f), Glow = new Color(.18f, .05f, .3f);
        EncounterEnemy enemy; Transform spider; Animation anim; Renderer[] parts; Vector3 lastPos; float lastSwingIn; bool died, white;
        public bool HasSpider { get { return spider != null; } }
        public string Playing { get; private set; }

        public static BroodBody Dress(EncounterEnemy e) { var b = e.gameObject.AddComponent<BroodBody>(); b.enemy = e; b.Build(); return b; }
        static Material hide, crystal;
        static Material Hide()
        {
            if (hide != null) return hide;
            var m = new Material(Shader.Find("Standard")) { name = "Brood hide" };
            var tex = Resources.Load<Texture2D>(TexturePath); if (tex != null) m.mainTexture = tex;
            m.SetFloat("_Mode", 1); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cutoff", .18f); m.renderQueue = 2450;
            m.color = Purple; m.SetFloat("_Glossiness", .3f); m.SetFloat("_Metallic", 0);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Glow);
            return hide = m;
        }
        static Material Crystal()
        {
            if (crystal != null) return crystal;
            var m = new Material(Shader.Find("Standard")) { name = "Brood salted", color = new Color(.92f, .94f, .97f) };
            m.SetFloat("_Glossiness", .9f); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.22f, .23f, .26f));
            return crystal = m;
        }
        void Build()
        {
            var src = Resources.Load<GameObject>(UnravellerBody.ModelPath); if (src == null) return;
            var root = new GameObject("Brood body").transform; root.SetParent(transform, false);
            var s = transform.lossyScale; root.localScale = new Vector3(1 / Mathf.Max(.01f, s.x), 1 / Mathf.Max(.01f, s.y), 1 / Mathf.Max(.01f, s.z));
            spider = Instantiate(src, root, false).transform; spider.name = "Spider"; spider.localRotation = Quaternion.Euler(0, UnravellerBody.FaceYaw, 0);
            foreach (var c in spider.GetComponentsInChildren<Collider>(true)) Destroy(c);
            parts = spider.GetComponentsInChildren<Renderer>(true);
            var b = new Bounds(); bool any = false;
            foreach (var r in parts)
            {
                var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = Hide(); r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.Off;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (any)
            {
                float k = Length / Mathf.Max(.01f, Mathf.Max(b.size.x, b.size.z));
                spider.localScale *= k;
                spider.localPosition = new Vector3(0, -1 - (b.min.y - root.position.y) * k, 0);   // its feet on the ground (an actor stands a metre over its feet)
            }
            // The Weave-Eater's tangle it was made with: hidden (the spider is its body now).
            var body = transform.Find("Body");
            if (body != null) foreach (var r in body.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            anim = spider.GetComponentInChildren<Animation>();
            Play("idle"); lastPos = transform.position;
        }
        void Play(string clip, float fade = .15f)
        {
            if (anim == null || anim[clip] == null || Playing == clip && anim.IsPlaying(clip)) return;
            Playing = clip; anim.CrossFade(clip, fade);
        }
        void SetWhite(bool on)
        {
            if (white == on || parts == null) return; white = on;
            foreach (var r in parts) { if (r == null) continue; var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = on ? Crystal() : Hide(); r.sharedMaterials = mats; }
            if (anim != null) foreach (AnimationState st in anim) st.speed = on ? 0 : 1;   // salted still
        }
        void LateUpdate()
        {
            if (enemy == null || spider == null) return;
            if (!enemy.actor.IsAlive)
            {
                if (!died) { died = true; bool shattered = enemy.Calcified || white; if (anim != null && anim["death1"] != null) { foreach (AnimationState st in anim) st.speed = 1; anim.Stop(); anim.Play("death1"); Playing = "death1"; } SetWhite(false); if (shattered) SetWhite(true); }
                return;
            }
            died = false;
            SetWhite(enemy.Calcified);
            if (white) return;
            var at = transform.position; float moved = (at - lastPos).magnitude; lastPos = at;
            float swingIn = enemy.NextSwingIn;
            if (swingIn > lastSwingIn + .5f && enemy.Engaged) Play(Random.value < .5f ? "attack1" : "attack2", .08f);
            else if (Playing == null || !Playing.StartsWith("attack") || anim == null || !anim.IsPlaying(Playing)) Play(moved > Time.deltaTime * .4f ? "walk" : "idle");
            lastSwingIn = swingIn;
        }
    }
}
