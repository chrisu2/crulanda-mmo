using System.Collections.Generic;
using UnityEngine;
using Crulanda.Combat;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A spirit at your shoulder that nobody commands (Chris, 2026-10-13: "give the shaman dps a pet (uncontrollable that throws out small
    /// dots and heals)", "have the druid healer have a fairy to help heal to catch up #s", "if not a druid fairy then something canon related
    /// for druid that has the same functionality", "the shaman should find something canon related also"). It comes with the build you
    /// have spent most in and goes when you change it:
    /// - The Stormcaller's storm spirit: stings the foes in your fight with a little damage over time and mends the most hurt of your party
    ///   a little.
    /// - The Rootmend's glade-fire (CANON-EXPANDED: the witch-fire wisp, a hovering orb of light bound to its maker's will, book 1 ch. 13, lit
    ///   with the Whispering Glade's regenerative gold and silver light on the moss, book 3 ch. 5): mends the most hurt of your party.
    /// It cannot be ordered, targeted or struck; its blows count as yours for threat and take your build's scale, its mends take your heal
    /// scale, and the meter shows it on its own row. A pet build is measured with its pet: player and pet together on 62-72 (Chris).
    /// </summary>
    public sealed class SpiritCompanion : MonoBehaviour
    {
        public enum Kinds { Storm, GladeFire }
        public Kinds Kind { get; private set; }
        public const float Reach = 30, StingEvery = 3, StingInterval = 2, StormMendEvery = 5, GladeMendEvery = 2, SpreadReach = 8;
        public const int StingTicks = 3;
        /// <summary>The names on the meter and in chat (the storm spirit's is PROVISIONAL until its canon root is chosen).</summary>
        public static string NameOf(Kinds k) { return k == Kinds.Storm ? "storm spirit" : "glade-fire"; }
        EncounterSession s; CombatMeter.Credit credit; readonly PeriodicEffects periodic = new PeriodicEffects();
        float nextSting, nextMend, phase; Light glow;
        int Level { get { return s.Player != null ? s.Player.Level : 1; } }
        /// <summary>Damage a tick of the storm spirit's sting (before your build's scale).</summary>
        public int StingValue { get { return 3 + Level; } }
        /// <summary>A mend (before your heal scale): the storm spirit's is small, the glade-fire's is its whole purpose.</summary>
        public int MendValue { get { return Kind == Kinds.Storm ? 5 + Level : 6 + Level; } }
        public string DisplayName { get { return "Your " + NameOf(Kind); } }
        public static Color Colour(Kinds k) { return k == Kinds.Storm ? new Color(.6f, .75f, 1f) : new Color(.85f, 1f, .55f); }

        public void Init(EncounterSession session, Kinds kind)
        {
            s = session; Kind = kind; phase = Random.value * 10;
            credit = new CombatMeter.Credit { id = "spirit." + (s.Progress != null ? s.Progress.playerId : "you"), name = DisplayName + " (spirit)" };
            Build();
            if (s.Player != null) transform.position = Shoulder();
            nextSting = nextMend = Time.time + 1;
        }
        void Build()
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            void Ball(Vector3 at, Vector3 size, Color c)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(b.GetComponent<Collider>());
                b.transform.SetParent(transform, false); b.transform.localPosition = at; b.transform.localScale = size;
                var r = b.GetComponent<MeshRenderer>(); r.sharedMaterial = new Material(mat) { color = c, renderQueue = 3100 };
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            }
            if (Kind == Kinds.Storm)
            {
                var cloud = new Color(.34f, .38f, .46f, .92f);
                Ball(Vector3.zero, new Vector3(.42f, .26f, .34f), cloud); Ball(new Vector3(.2f, .04f, .02f), new Vector3(.3f, .22f, .26f), cloud);
                Ball(new Vector3(-.19f, .02f, -.03f), new Vector3(.28f, .2f, .24f), cloud); Ball(new Vector3(0, -.06f, 0), new Vector3(.12f, .06f, .12f), new Color(.7f, .82f, 1f, .9f));
            }
            else
            {
                Ball(Vector3.zero, Vector3.one * .13f, new Color(1f, 1f, .8f, 1f));
                Ball(Vector3.zero, Vector3.one * .3f, new Color(.75f, .95f, .45f, .35f)); Ball(Vector3.zero, Vector3.one * .46f, new Color(.85f, .88f, .95f, .14f));
            }
            glow = gameObject.AddComponent<Light>(); glow.type = LightType.Point; glow.color = Colour(Kind); glow.range = 3.5f; glow.intensity = .8f; glow.shadows = LightShadows.None;
        }
        Vector3 Shoulder()
        {
            var p = s.Player.transform; float t = Time.time + phase;
            var side = Kind == Kinds.Storm ? p.right * -.9f : p.right * .8f;
            return p.position + side + Vector3.up * (Kind == Kinds.Storm ? 2.4f : 1.9f) + new Vector3(Mathf.Sin(t * .9f) * .15f, Mathf.Sin(t * 1.7f) * .12f, Mathf.Cos(t * .8f) * .15f);
        }

        void Update()
        {
            if (s == null || s.Player == null) return;
            transform.position = Vector3.Lerp(transform.position, Shoulder(), 1 - Mathf.Exp(-6 * Time.deltaTime));
            if (glow != null) glow.intensity = Mathf.MoveTowards(glow.intensity, .8f, Time.deltaTime * 3);
            periodic.Tick(Time.time);
            if (!s.Player.IsAlive) return;
            float now = Time.time;
            if (Kind == Kinds.Storm && now >= nextSting) { var t = StingTarget(); if (t != null) { Sting(t); nextSting = now + StingEvery; } }
            if (now >= nextMend) { var who = MostHurt(); if (who != null) { Mend(who); nextMend = now + (Kind == Kinds.Storm ? StormMendEvery : GladeMendEvery); } }
        }

        // ---------- the storm spirit's sting ----------
        /// <summary>In your fight: struck or striking in the last few seconds, or fighting your party. It never starts a fight.</summary>
        static bool InTheFight(EncounterEnemy e) { return e != null && e.actor != null && e.actor.IsAlive && !e.Game && (e.FightingParty || !e.Quiet); }
        EncounterEnemy StingTarget()
        {
            var t = s.Target; bool targetIn = InTheFight(t) && Near(t.transform.position);
            if (targetIn && !periodic.Has("sting", t)) return t;
            // Spread: another foe in the fight near your target (or near you) that has no sting yet.
            var from = targetIn ? t.transform.position : s.Player.transform.position; EncounterEnemy best = null; float bestD = SpreadReach;
            foreach (var e in s.Enemies)
            {
                if (!InTheFight(e) || periodic.Has("sting", e) || !Near(e.transform.position)) continue;
                float d = Vector3.Distance(e.transform.position, from); if (d < bestD) { bestD = d; best = e; }
            }
            if (best != null) return best;
            return targetIn ? t : null;   // every one stung: keep your target's going
        }
        bool Near(Vector3 p) { return Vector3.Distance(p, s.Player.transform.position) <= Reach; }
        void Sting(EncounterEnemy t)
        {
            Bolt.Fire(transform.position, t.transform, Colour(Kind), .1f, .25f);
            if (glow != null) glow.intensity = 2.2f;
            periodic.Add("sting", t, Time.time, StingInterval, StingTicks, StingValue, (e, i) => {
                var enemy = (EncounterEnemy)e.target; if (enemy == null || !enemy.actor.IsAlive) return false;
                As(() => enemy.Receive(e.value, s.Player)); return true;
            });
        }

        // ---------- mends ----------
        IEnumerable<Actor> Party()
        {
            yield return s.Player;
            if (s.Progress != null && s.Progress.recruited && s.Companion != null && s.Companion.gameObject.activeInHierarchy) yield return s.Companion.actor;
            foreach (var c in s.PartySims) if (c != null && c.actor != null) yield return c.actor;
        }
        Actor MostHurt()
        {
            Actor best = null; float low = Kind == Kinds.Storm ? .9f : .97f;
            foreach (var who in Party())
                if (who != null && who.IsAlive && who.Health.Pool.Ratio < low && Near(who.transform.position)) { best = who; low = who.Health.Pool.Ratio; }
            return best;
        }
        void Mend(Actor who)
        {
            var c = who.GetComponent<Combatant>(); if (c == null) return;
            Bolt.Fire(transform.position, who.transform, HealFx.Green, .08f, .3f);
            int healed = 0; As(() => healed = c.Heal(Mathf.RoundToInt(MendValue * s.PlayerHealScale), s.Player));
            if (healed > 0) { HealFx.Show(who.transform); s.FloatText(who.transform.position, "+" + healed, new Color(.3f, 1, .7f)); }
        }
        /// <summary>Done on your behalf (threat, scales), counted on the spirit's own row of the meter.</summary>
        void As(System.Action act) { var m = s.Meter; m.Redirect = credit; try { act(); } finally { m.Redirect = null; } }
        void OnDestroy() { periodic.Clear(); }
    }
}
