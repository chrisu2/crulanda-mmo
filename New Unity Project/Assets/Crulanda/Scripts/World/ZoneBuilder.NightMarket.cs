using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Night Market (Docs/REWORK_LOWTOWNS_HALLOWS.md, Lowtowns pick 8; Docs/ZONE_LOWTOWNS.md "Dead Man's Drop and the Night Market").
    /// CANON: "The Rust Market did not sleep" (book1 ch.3:5); its skewered-rat and aether-spiced wine (ch.3:7). From the Lowtowns' tales
    /// (crulanda\lore_stories.md): the street vendor who sells jars of "premium nothingness" (#9, "The Merchant of Nothing") and the candlemaker
    /// whose striker made "little blue butterflies made of light" that popped into sulphur smoke (#6, "The Glitch").
    /// The game: a row of night stalls on the market's south edge above Skewer Lane, shuttered by day (a tarp over each counter) and open
    /// after dark (<see cref="WorldClock.IsNight"/>): the Merchant of Nothing's counter of empty jars, the candlemaker's candles with his
    /// light-butterflies round them (<see cref="LightButterflies"/>), and a chalk ring (GAME-ONLY) where a card-reader tells fortunes; a
    /// string of lanterns over the row, and the skewer and wine stalls lit. Their keepers (residents in the zone's data, <see cref="NightFolk"/>)
    /// are out only at night (EncounterSession.NightMarket). Everything here is visual but the counters (solid, cut out of the navmesh).
    /// No machines: timber, cloth, tallow, glass, chalk.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The Night Market's keepers (residents in the Lowtowns' data, out only after dark).</summary>
        public const string MerchantOfNothing = "The Merchant of Nothing", Candlemaker = "Abel Snuff", CardReader = "Mother Gant";
        public static readonly string[] NightFolk = { MerchantOfNothing, Candlemaker, CardReader };
        /// <summary>The chalk ring's centre and radius (GAME-ONLY), in front of the card-reader.</summary>
        public static readonly Vector2 ChalkRing = new Vector2(119, 48.4f);
        public const float ChalkRingRadius = 2.1f;
        static readonly Color ButterflyBlue = new Color(.45f, .7f, 1f);

        /// <summary>The resident named <paramref name="name"/> in the zone's data, or null.</summary>
        ZoneResident ResidentNamed(string name)
        {
            if (Zone.life == null || Zone.life.residents == null) return null;
            foreach (var r in Zone.life.residents) if (r != null && r.name == name) return r;
            return null;
        }

        void BuildNightMarket()
        {
            var merchant = ResidentNamed(MerchantOfNothing); if (merchant == null) return;
            var root = new GameObject("The Night Market").transform; root.SetParent(statics, false);
            var night = new GameObject("By night").transform; night.SetParent(root, false); night.gameObject.AddComponent<NightOnly>();
            var day = new GameObject("By day").transform; day.SetParent(root, false); day.gameObject.AddComponent<NightOnly>().byDay = true;
            var r = new System.Random(Zone.seed + 8181); float R() { return (float)r.NextDouble(); }
            var timber = TarredTimber; var cloth = Tint(art.cloth, new Color(.2f, .14f, .2f)); var tarp = Tint(art.cloth, new Color(.27f, .25f, .2f));
            var nb = new Batch(); var db = new Batch(); var sb = new Batch();
            Vector3 W(Vector2 p, float y) { return new Vector3(p.x, HeightAt(p.x, p.y) + y, p.y); }

            // A night stall in front of a keeper: a plank counter (solid), four posts and a dark cloth over them; by day a tarp lashed over
            // the counter. Returns the counter's top centre and its frame (forward toward the lane).
            (Vector3 top, Quaternion q) Stall(ZoneResident who, float width)
            {
                var q = Quaternion.Euler(0, who.facing, 0); var fwd = q * Vector3.forward;
                var feet = W(who.at, 0); var c = feet + fwd * 1.15f;
                var counter = SolidBox(root, root.InverseTransformPoint(c + Vector3.up * .48f), new Vector3(width, .96f, .7f), timber, q); counter.name = who.name + "'s counter";
                foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                    sb.Box(timber, root.InverseTransformPoint(c + q * new Vector3(sx * (width / 2 - .05f), 1.2f, sz * .35f - .45f)), new Vector3(.09f, 2.4f, .09f), q);
                sb.Box(cloth, root.InverseTransformPoint(c + q * new Vector3(0, 2.42f, -.45f)), new Vector3(width + .3f, .05f, 1.6f), q * Quaternion.Euler(-8, 0, 0));
                db.Box(tarp, root.InverseTransformPoint(c + Vector3.up * 1.0f), new Vector3(width + .1f, .08f, .8f), q);
                foreach (int s in new[] { -1, 1 }) db.Box(tarp, root.InverseTransformPoint(c + q * new Vector3(0, .62f, s * .4f)), new Vector3(width + .1f, .75f, .04f), q);
                return (c + Vector3.up * .97f, q);
            }

            // The Merchant of Nothing's jars (lore #9): rows of stoppered glass jars with nothing in them, a few on a shelf behind him.
            {
                var (top, q) = Stall(merchant, 2.2f);
                var jar = Tint(art.metal, new Color(.62f, .66f, .64f)); var cork = Tint(art.timber, new Color(.45f, .32f, .2f));
                for (int i = 0; i < 9; i++)
                {
                    var at = top + q * new Vector3(-.85f + (i % 5) * .42f + (i / 5) * .2f, .12f, i < 5 ? .15f : -.12f);
                    nb.Box(jar, root.InverseTransformPoint(at), new Vector3(.15f, .24f, .15f), q * Quaternion.Euler(0, 45, 0));
                    nb.Box(cork, root.InverseTransformPoint(at + Vector3.up * .14f), new Vector3(.09f, .05f, .09f), q);
                }
                var glow = Glow(night, root.InverseTransformPoint(top + q * new Vector3(.9f, 1.1f, -.3f)), 7, 0, new Color(.85f, .9f, 1f), 1.1f); glow.name = "The jars' lamp";
                nb.Box(Glowing(new Color(.85f, .9f, 1f), 1.6f), root.InverseTransformPoint(top + q * new Vector3(.9f, 1.1f, -.3f)), new Vector3(.16f, .22f, .16f), q);
            }
            // The candlemaker (lore #6): candles in every height, a lit few, and his light-butterflies about the stall at night.
            var candle = ResidentNamed(Candlemaker);
            if (candle != null)
            {
                var (top, q) = Stall(candle, 2.0f);
                var wax = Tint(art.cloth, new Color(.86f, .82f, .7f)); var flame = Glowing(new Color(1, .7f, .3f), 2.2f);
                for (int i = 0; i < 14; i++)
                {
                    float h = .08f + R() * .3f; var at = top + q * new Vector3(-.85f + R() * 1.7f, h / 2, -.25f + R() * .5f);
                    sb.Box(wax, root.InverseTransformPoint(at), new Vector3(.05f, h, .05f), q);
                    if (i % 3 == 0) nb.Box(flame, root.InverseTransformPoint(at + Vector3.up * (h / 2 + .03f)), new Vector3(.025f, .05f, .025f), q);
                }
                Glow(night, root.InverseTransformPoint(top + Vector3.up * .4f), 6, 0, new Color(1, .72f, .38f), 1f).name = "Candle-light";
                var flutter = new GameObject("Light-butterflies").transform; flutter.SetParent(night, false); flutter.position = top + Vector3.up * .5f;
                flutter.gameObject.AddComponent<LightButterflies>().Init(Glowing(ButterflyBlue, 2.6f), Tint(art.stone, new Color(.5f, .48f, .4f)), Zone.seed);
                Glow(night, flutter.localPosition + Vector3.up * .5f, 5, 0, ButterflyBlue, .7f).name = "Butterfly-light";
            }
            // The chalk ring (GAME-ONLY) and the card-reader beside it: a circle of chalk on the setts, four stubs of candle on it, an
            // upturned crate for a table with three cards face up (the books' Triad: Wyrms and Coins, book1 ch.1:47).
            {
                var chalk = Tint(art.stone, new Color(.86f, .86f, .82f)); float y = HeightAt(ChalkRing.x, ChalkRing.y) + .02f;
                for (int k = 0; k < 32; k++)
                {
                    float a = k * Mathf.PI * 2 / 32; var at = new Vector3(ChalkRing.x + Mathf.Cos(a) * ChalkRingRadius, y, ChalkRing.y + Mathf.Sin(a) * ChalkRingRadius);
                    sb.Box(chalk, root.InverseTransformPoint(at), new Vector3(.07f, .01f, ChalkRingRadius * Mathf.PI * 2 / 32 + .03f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0));
                }
                for (int k = 0; k < 5; k++)
                {
                    float a = k * Mathf.PI * 2 / 5 + .3f, a2 = a + Mathf.PI * 4 / 5;
                    var p0 = new Vector3(ChalkRing.x + Mathf.Cos(a) * ChalkRingRadius * .8f, y, ChalkRing.y + Mathf.Sin(a) * ChalkRingRadius * .8f);
                    var p1 = new Vector3(ChalkRing.x + Mathf.Cos(a2) * ChalkRingRadius * .8f, y, ChalkRing.y + Mathf.Sin(a2) * ChalkRingRadius * .8f);
                    var mid = (p0 + p1) / 2; var run = p1 - p0;
                    sb.Box(chalk, root.InverseTransformPoint(mid), new Vector3(.05f, .01f, run.magnitude), Quaternion.LookRotation(run));
                }
                var wax = Tint(art.cloth, new Color(.86f, .82f, .7f)); var flame = Glowing(new Color(1, .7f, .3f), 2.2f);
                for (int k = 0; k < 4; k++)
                {
                    float a = k * Mathf.PI / 2 + .78f; var at = new Vector3(ChalkRing.x + Mathf.Cos(a) * ChalkRingRadius, y + .06f, ChalkRing.y + Mathf.Sin(a) * ChalkRingRadius);
                    nb.Box(wax, root.InverseTransformPoint(at), new Vector3(.06f, .12f, .06f));
                    nb.Box(flame, root.InverseTransformPoint(at + Vector3.up * .09f), new Vector3(.025f, .05f, .025f));
                }
                var crate = new Vector3(ChalkRing.x, y, ChalkRing.y + ChalkRingRadius * .35f);
                sb.Box(Tint(art.timber, new Color(.3f, .24f, .18f)), root.InverseTransformPoint(crate + Vector3.up * .25f), new Vector3(.55f, .5f, .45f));
                var card = Tint(art.cloth, new Color(.8f, .78f, .7f));
                for (int k = 0; k < 3; k++) nb.Box(card, root.InverseTransformPoint(crate + new Vector3(-.15f + k * .15f, .51f, 0)), new Vector3(.08f, .005f, .12f), Quaternion.Euler(0, k * 12 - 12, 0));
                Glow(night, root.InverseTransformPoint(crate + Vector3.up * 1.2f), 6, 0, new Color(1, .66f, .34f), .9f).name = "The ring's candles";
            }
            // A string of lanterns over the row on two poles behind the stalls (behind the keepers' backs, clear of the lane).
            {
                var a = W(new Vector2(96.5f, 53.4f), 0); var c = W(new Vector2(124.5f, 53.4f), 0);
                foreach (var p in new[] { a, c }) sb.Box(timber, root.InverseTransformPoint(p + Vector3.up * 1.7f), new Vector3(.14f, 3.4f, .14f));
                var lantern = Glowing(new Color(1, .62f, .28f), 1.8f); var line = Tint(art.cloth, new Color(.15f, .13f, .12f));
                int n = 12;
                for (int k = 0; k < n; k++)
                {
                    float u0 = k / (float)n, u1 = (k + 1) / (float)n;
                    var p0 = Vector3.Lerp(a, c, u0) + Vector3.up * (3.3f - Mathf.Sin(u0 * Mathf.PI) * .8f);
                    var p1 = Vector3.Lerp(a, c, u1) + Vector3.up * (3.3f - Mathf.Sin(u1 * Mathf.PI) * .8f);
                    sb.Box(line, root.InverseTransformPoint((p0 + p1) / 2), new Vector3(.025f, .025f, (p1 - p0).magnitude), Quaternion.LookRotation(p1 - p0));
                    if (k > 0) nb.Box(lantern, root.InverseTransformPoint(p0 + Vector3.down * .22f), new Vector3(.2f, .26f, .2f), Quaternion.Euler(0, R() * 30, 0));
                }
                Glow(night, root.InverseTransformPoint(Vector3.Lerp(a, c, .5f) + Vector3.up * 2.2f), 12, 0, new Color(1, .62f, .3f), 1.1f).name = "Lantern-string light";
            }
            // The skewer and wine stalls lit at dusk (book1 ch.3:7): a brazier's glow at each.
            foreach (var p in Zone.props)
                if (p != null && p.kind == "stall" && (p.name == "Skewers" || p.name == "Aether wine"))
                    Glow(night, root.InverseTransformPoint(W(p.at, 2.1f)), 8, 0, p.name == "Skewers" ? new Color(1, .5f, .22f) : new Color(.85f, .5f, 1f), 1f).name = p.name + " by night";

            nb.Flush(this, night, "Night Market by night"); db.Flush(this, day, "Night Market by day"); sb.Flush(this, root, "Night Market");
            Debug.Log("Lowtowns: the Night Market built (" + NightFolk.Length + " keepers, out after dark).");
        }
    }

    /// <summary>Shows its renderers only at night (or, <see cref="byDay"/>, only by day), looked at every couple of seconds.</summary>
    public sealed class NightOnly : MonoBehaviour
    {
        public bool byDay;
        float nextAt; int shown = -1;
        void Update()
        {
            if (Time.time < nextAt) return; nextAt = Time.time + 2;
            int now = WorldClock.IsNight != byDay ? 1 : 0; if (now == shown) return; shown = now;
            foreach (var rr in GetComponentsInChildren<Renderer>(true)) rr.enabled = now == 1;
        }
    }

    /// <summary>
    /// The candlemaker's light-butterflies (CANON lore, lore_stories.md #6: "little blue butterflies made of light. They flew around the
    /// room and then popped into smoke that smelled like sulfur"): a few small blue glowing moths wheeling round the stall after dark, their
    /// wings beating; now and then one pops into a puff of yellow-grey smoke and another flutters up from the candles. Harmless and pretty.
    /// By day there are none.
    /// </summary>
    public sealed class LightButterflies : MonoBehaviour
    {
        const int Count = 7;
        sealed class Fly { public Transform body, left, right, puff; public float phase, speed, radius, height, popAt, puffUntil; }
        readonly List<Fly> flies = new List<Fly>();
        System.Random r; float nextCheck; bool lit;
        float R() { return (float)r.NextDouble(); }

        public void Init(Material wing, Material smoke, int seed)
        {
            r = new System.Random(seed + 606);
            for (int i = 0; i < Count; i++)
            {
                var f = new Fly { body = new GameObject("Butterfly").transform };
                f.body.SetParent(transform, false);
                f.left = Wing(f.body, wing, -1); f.right = Wing(f.body, wing, 1);
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere); DestroyImmediate(puff.GetComponent<Collider>());
                puff.name = "Sulphur puff"; puff.transform.SetParent(transform, false); puff.GetComponent<Renderer>().sharedMaterial = smoke; puff.transform.localScale = Vector3.zero;
                f.puff = puff.transform;
                Reset(f, true); flies.Add(f);
            }
            Show(false);
        }
        static Transform Wing(Transform body, Material m, int side)
        {
            var hinge = new GameObject(side < 0 ? "Left wing" : "Right wing").transform; hinge.SetParent(body, false);
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube); DestroyImmediate(w.GetComponent<Collider>());
            w.transform.SetParent(hinge, false); w.transform.localPosition = new Vector3(side * .035f, 0, 0); w.transform.localScale = new Vector3(.07f, .004f, .055f);
            w.GetComponent<Renderer>().sharedMaterial = m; return hinge;
        }
        void Reset(Fly f, bool first)
        {
            f.phase = R() * 6.28f; f.speed = .6f + R() * .7f; f.radius = .6f + R() * 1.4f; f.height = R() * 1.6f;
            f.popAt = Time.time + (first ? R() * 12 : 6 + R() * 14);
        }
        void Show(bool on)
        {
            lit = on;
            foreach (var rr in GetComponentsInChildren<Renderer>(true)) rr.enabled = on;
        }
        void Update()
        {
            if (Time.time >= nextCheck) { nextCheck = Time.time + 2; bool night = WorldClock.IsNight; if (night != lit) Show(night); }
            if (!lit) return;
            float t = Time.time;
            foreach (var f in flies)
            {
                float a = f.phase + t * f.speed;
                f.body.localPosition = new Vector3(Mathf.Cos(a) * f.radius, f.height + Mathf.Sin(a * 2.3f) * .25f, Mathf.Sin(a * 1.3f) * f.radius * .7f);
                f.body.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                float beat = Mathf.Sin(t * 22 + f.phase) * 55;
                f.left.localRotation = Quaternion.Euler(0, 0, -beat); f.right.localRotation = Quaternion.Euler(0, 0, beat);
                // The pop: the moth gone in a puff that swells and thins, and a new one up from the candles.
                if (t >= f.popAt)
                {
                    f.puff.localPosition = f.body.localPosition; f.puffUntil = t + 1.2f;
                    Reset(f, false); f.height = 0; f.body.localPosition = Vector3.zero;
                }
                float left = f.puffUntil - t;
                f.puff.localScale = left > 0 ? Vector3.one * (.25f - left * .15f) : Vector3.zero;
                if (left > 0) f.puff.localPosition += Vector3.up * Time.deltaTime * .3f;
                if (f.height < 1.6f) f.height += Time.deltaTime * .25f;
            }
        }
    }
}
