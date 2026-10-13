#if CRULANDA_CLERIC   // off until the Cleric's content (Encounter.asset class, abilities, unlocks; Talents/cleric.json) is wired: Docs/CLASS_CLERIC_SHAMAN.md
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.Abilities;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Cleric (note 178, Chris 19:18: "still need other healers to test. paladin, cleric (new class) , shaman (new class), salt mender";
    /// GAME-ONLY: no priests or clerics in canon, checked 2026-10-12; PROVISIONAL names): a lay-sister or lay-brother of a hill shrine,
    /// outside the Concord, praying by lantern-light. Grace, a well of 100, pays for every prayer.
    /// Bar: 1 Rebuke, 2 Mend, 3 Long Mend, 4 Vigil, 5 Litany, 6 Ward, 7 Brand, 8 Sunburst, 9 Absolve, 10 Hymn, 11 Sanctuary, 12 Steadfast.
    /// Four paths (Docs/classes/cleric.json): Lantern (single-target healer), Choir (group healer), Censure (dps), Warden (support).
    /// </summary>
    public sealed class ClericKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "ln-steady-hand", "ln-vigil", "ln-thrift", "ln-quick-prayer", "ln-lantern-light", "ln-keeper",
            "ch-full-voice", "ch-wide-ring", "ch-held-note", "ch-answer", "ch-sanctuary", "ch-chorus",
            "cn-hard-word", "cn-deep-brand", "cn-stern", "cn-sunburst", "cn-witness", "cn-judgement",
            "wd-thick-ward", "wd-absolve", "wd-steadfast", "wd-renew", "wd-shared", "wd-bulwark" };
        public static readonly string[] BarIds = { "cleric.rebuke", "cleric.mend", "cleric.long_mend", "cleric.vigil", "cleric.litany", "cleric.ward",
            "cleric.brand", "cleric.sunburst", "cleric.absolve", "cleric.hymn", "cleric.sanctuary", "cleric.steadfast" };
        public const float MendReach = 30, LitanyReach = 10, SanctuaryRadius = 6, SanctuaryCut = .1f, SteadfastCut = .05f;

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        readonly PeriodicEffects periodic = new PeriodicEffects();
        readonly Dictionary<EncounterEnemy, int> brands = new Dictionary<EncounterEnemy, int>();
        float sanctuaryUntil, steadfastUntil, lanternUntil; Vector3 sanctuaryAt; bool keeperSpent, bulwarkSpent;

        public ClericKit(EncounterSession session, ClassDefinition definition, IEnumerable<AbilityDefinition> catalog) : base(session)
        {
            if (definition == null || definition.unlocks == null || catalog == null) throw new ArgumentException("Cleric content missing.");
            foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !abilities.ContainsKey(a.id)) abilities.Add(a.id, a);
            foreach (var u in definition.unlocks)
                if (u == null || string.IsNullOrEmpty(u.abilityId) || u.level < 1 || u.level > 10 || unlocks.ContainsKey(u.abilityId)) throw new ArgumentException("Invalid or duplicate Cleric unlock.");
                else unlocks.Add(u.abilityId, u);
            foreach (var id in BarIds) if (!abilities.ContainsKey(id) || !unlocks.ContainsKey(id)) throw new ArgumentException("Cleric ability '" + id + "' is missing from content or unlocks.");
        }
        public override string[] ImplementedTalents { get { return ImplementedIds; } }
        int R(string id) { return s.TalentRank(id); }
        float Now { get { return Time.time; } }
        int Level { get { return s.Player.Level; } }
        Combatant PlayerCombat { get { return s.Player.GetComponent<Combatant>(); } }
        int Dmg(float amount) { return Mathf.Max(1, Mathf.RoundToInt(amount)); }
        bool Need(bool ok, string message) { if (!ok) s.Message(message); return ok; }
        bool Start(AbilityDefinition a, Action effect) { return s.StartAbility(a, () => { if (s.Player.IsAlive) effect(); }) == AbilityStartResult.Started; }
        static AbilityDefinition With(AbilityDefinition a, int cost, float castTime)
        {
            return new AbilityDefinition { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = cost,
                cooldown = a.cooldown, range = a.range, castTime = castTime, globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId };
        }

        // ---------- the party ----------
        /// <summary>You, Mira (recruited) and the sims in your party.</summary>
        public IEnumerable<Actor> Party()
        {
            yield return s.Player;
            if (s.Progress.recruited && s.Companion != null && s.Companion.gameObject.activeInHierarchy) yield return s.Companion.actor;
            foreach (var c in s.PartySims) if (c != null) yield return c.actor;
        }
        public Actor MostHurt(float reach)
        {
            Actor best = null; float low = 2;
            foreach (var who in Party())
                if (who != null && who.IsAlive && Vector3.Distance(who.transform.position, s.Player.transform.position) <= reach && who.Health.Pool.Ratio < low) { low = who.Health.Pool.Ratio; best = who; }
            return best;
        }
        int Mend(Actor who, int amount)
        {
            if (who == null || !who.IsAlive || amount <= 0) return 0;
            int want = Mathf.RoundToInt(amount * s.PlayerHealScale);
            int missing = who.Health.Pool.Max - who.Health.Pool.Current;
            int healed = who.GetComponent<Combatant>().Heal(want, s.Player);
            if (healed > 0) { HealFx.Show(who.transform); s.AddHealingThreat(healed); s.FloatText(who.transform.position, "+" + healed, new Color(1, .93f, .7f)); }
            if (R("ln-thrift") > 0 && want <= missing) Grace(3 * R("ln-thrift"));   // Thrift of Grace: a heal that wastes nothing
            return healed;
        }
        void Grace(int n) { var p = s.Player.Resource.Pool; p.SetCurrent(Mathf.Min(p.Max, p.Current + n)); }

        // ---------- action bar ----------
        public override int ActionCount { get { return BarIds.Length; } }
        public override AbilityDefinition ActionAt(int slot) { return slot >= 0 && slot < BarIds.Length && abilities.TryGetValue(BarIds[slot], out var a) ? a : null; }
        public override string ActionLockLabel(int slot)
        {
            var a = ActionAt(slot); if (a == null || s.Player == null) return "Unavailable";
            var u = unlocks[a.id];
            if (Level < u.level) return "Level " + u.level;
            if (!string.IsNullOrEmpty(u.talentId) && R(u.talentId) == 0) return "Talent";
            return null;
        }
        public override bool Use(int slot)
        {
            var a = ActionAt(slot); if (a == null) return false;
            switch (a.id)
            {
                case "cleric.rebuke": return Rebuke(a);
                case "cleric.mend": return MendPrayer(a);
                case "cleric.long_mend": return LongMend(a);
                case "cleric.vigil": return Vigil(a);
                case "cleric.litany": return Litany(a);
                case "cleric.ward": return Ward(a);
                case "cleric.brand": return Brand(a);
                case "cleric.sunburst": return Sunburst(a);
                case "cleric.absolve": return Absolve(a);
                case "cleric.hymn": return Hymn(a);
                case "cleric.sanctuary": return Sanctuary(a);
                case "cleric.steadfast": return Steadfast(a);
            }
            return false;
        }

        // ---------- Lantern ----------
        bool MendPrayer(AbilityDefinition a)
        {
            var who = MostHurt(MendReach); if (!Need(who != null, "No one near enough to mend.")) return false;
            bool quick = Now < lanternUntil; var use = quick ? With(a, a.cost, 0) : a;
            return Start(use, () => { if (quick) lanternUntil = 0; Mend(MostHurt(MendReach) ?? who, Dmg((a.power + 2 * Level) * (1 + .04f * R("ln-steady-hand")) * (quick && R("ln-lantern-light") > 1 ? 1.1f : 1))); });
        }
        bool LongMend(AbilityDefinition a)
        {
            var who = MostHurt(MendReach); if (!Need(who != null, "No one near enough to mend.")) return false;
            var use = With(a, a.cost, Mathf.Max(.8f, a.castTime - .2f * R("ln-quick-prayer")));
            return Start(use, () => Mend(MostHurt(MendReach) ?? who, Dmg((a.power + 4 * Level) * (1 + .04f * R("ln-steady-hand")))));
        }
        bool Vigil(AbilityDefinition a)
        {
            var who = MostHurt(MendReach); if (!Need(who != null, "No one near enough.")) return false;
            return Start(a, () => {
                int ticks = Mathf.RoundToInt(a.duration / 2) + R("ln-vigil");
                periodic.Add("vigil", who, Now, 2, Mathf.Max(1, ticks), Dmg((a.power + Level) * (1 + .04f * R("ln-steady-hand"))), (e, i) => Mend((Actor)e.target, e.value) >= 0);
                if (R("ln-lantern-light") > 0) lanternUntil = Now + 8;
            });
        }

        // ---------- Choir ----------
        int MendAround(float reach, int amount) { int sum = 0; foreach (var who in Party().ToList()) if (who != null && Vector3.Distance(who.transform.position, s.Player.transform.position) <= reach) sum += Mend(who, amount); return sum; }
        bool Litany(AbilityDefinition a)
        {
            return Start(a, () => {
                int amount = Dmg((a.power + 2 * Level) * (1 + .04f * R("ch-full-voice")));
                MendAround(LitanyReach + R("ch-wide-ring"), amount);
                if (R("ch-chorus") > 0) foreach (var who in Party().ToList()) periodic.Add("chorus", who, Now, 2, 3, Dmg(amount * .1f), (e, i) => Mend((Actor)e.target, e.value) >= 0);
            });
        }
        bool Hymn(AbilityDefinition a)
        {
            return Start(a, () => {
                int ticks = Mathf.RoundToInt(Mathf.Max(2, a.duration)) + R("ch-held-note");
                periodic.Add("hymn", s.Player, Now, 1, ticks, Dmg((a.power + Level) * (1 + .04f * R("ch-full-voice"))), (e, i) => {
                    MendAround(LitanyReach + R("ch-wide-ring"), e.value); if (R("ch-answer") > 0) Grace(2 * R("ch-answer")); return true; });
            });
        }
        bool Sanctuary(AbilityDefinition a) { return Start(a, () => { sanctuaryAt = s.Player.transform.position; sanctuaryUntil = Now + Mathf.Max(6, a.duration); }); }
        public bool InSanctuary(Actor who) { return Now < sanctuaryUntil && who != null && Vector3.Distance(who.transform.position, sanctuaryAt) <= SanctuaryRadius; }

        // ---------- Censure ----------
        bool Rebuke(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            var use = With(a, a.cost, Mathf.Max(0, a.castTime - .15f * R("cn-stern")));
            return Start(use, () => { if (!s.LandsOn(t, a.range)) return; s.Bolt(t, new Color(1, .9f, .55f), .15f); t.Receive(Dmg((a.power + 2 * Level) * (1 + .04f * R("cn-hard-word"))), s.Player); });
        }
        bool Brand(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                brands[t] = Mathf.Min(3, (brands.TryGetValue(t, out var n) ? n : 0) + 1);
                periodic.Add("brand", t, Now, 2, 4 + R("cn-deep-brand"), Dmg(a.power + Level), (e, i) => { var enemy = (EncounterEnemy)e.target; if (!enemy.actor.IsAlive) { brands.Remove(enemy); return false; } enemy.Receive(e.value, s.Player); return true; });
            });
        }
        bool Sunburst(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            if (!Need(brands.TryGetValue(t, out var n) && n > 0, "Sunburst needs a Brand on the foe.")) return false;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range) || !brands.TryGetValue(t, out var k)) return; brands.Remove(t); periodic.RemoveKey("brand");
                float d = (a.power + 3 * Level) * k * (1 + .2f * R("cn-sunburst")); if (R("cn-judgement") > 0 && t.actor.Health.Pool.Ratio < .25f) d *= 2;
                s.Bolt(t, new Color(1, .95f, .6f), .3f); t.Receive(Dmg(d), s.Player);
            });
        }
        public bool Branded(EncounterEnemy e) { return e != null && brands.ContainsKey(e); }

        // ---------- Warden ----------
        bool Ward(AbilityDefinition a)
        {
            var who = MostHurt(MendReach) ?? s.Player;
            return Start(a, () => {
                int strength = Dmg((a.power + 3 * Level) * (1 + .05f * R("wd-thick-ward")));
                who.GetComponent<Combatant>().AddBarrier(strength, 10);
                int more = R("wd-shared"); if (more > 0) foreach (var o in Party().Where(x => x != who && x.IsAlive).OrderBy(x => x.Health.Pool.Ratio).Take(more).ToList()) o.GetComponent<Combatant>().AddBarrier(strength / 2, 10);
            });
        }
        bool Absolve(AbilityDefinition a)
        {
            var who = MostHurt(MendReach) ?? s.Player;
            var use = With(a, a.cost, a.castTime); use.cooldown = Mathf.Max(1, a.cooldown - 2 * R("wd-absolve"));
            return Start(use, () => who.GetComponent<Combatant>().Statuses.RemoveHarmful());
        }
        bool Steadfast(AbilityDefinition a) { return Start(a, () => steadfastUntil = Now + Mathf.Max(10, a.duration)); }

        // ---------- incoming blows ----------
        public override int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        {
            if (victim == null || !victim.IsAlive) return 0;
            float mult = 1;
            if (InSanctuary(victim)) mult *= 1 - SanctuaryCut - .05f * R("ch-sanctuary");
            if (Now < steadfastUntil && Vector3.Distance(victim.transform.position, s.Player.transform.position) <= 10) mult *= 1 - SteadfastCut - .01f * R("wd-steadfast");
            int amount = Mathf.RoundToInt(raw * mult);
            if (R("ln-keeper") > 0 && !keeperSpent && amount >= victim.Health.Pool.Current)   // Keeper of the Flame
            { keeperSpent = true; amount = Mathf.Max(0, victim.Health.Pool.Current - 1); }
            int dealt = victim.GetComponent<Combatant>().Damage(amount);
            if (dealt > 0 && keeperSpent && victim.Health.Pool.Current == 1) Mend(victim, Mathf.RoundToInt(victim.Health.Pool.Max * .3f));
            if (R("wd-bulwark") > 0 && !bulwarkSpent && victim.Health.Pool.Ratio < .35f)   // Bulwark of Faith
            { bulwarkSpent = true; foreach (var o in Party().ToList()) o.GetComponent<Combatant>().AddBarrier(Mathf.RoundToInt(o.Health.Pool.Max * .2f), 8); }
            return dealt;
        }
        /// <summary>Witness: a Branded foe takes more from the whole party.</summary>
        public override float PartyDamageMultiplier(EncounterEnemy enemy) { return Branded(enemy) ? 1 + .03f * R("cn-witness") : 1; }
        public override bool MeleeAutoAttacks { get { return false; } }

        // ---------- per frame / lifecycle ----------
        public override void Tick(bool inCombat) { periodic.Tick(Now); if (!inCombat) { keeperSpent = bulwarkSpent = false; } }
        public override void ResetState() { periodic.Clear(); brands.Clear(); sanctuaryUntil = steadfastUntil = lanternUntil = 0; keeperSpent = bulwarkSpent = false; }
        public override void ClearTransient() { ResetState(); if (s.Player != null) PlayerCombat.ClearBarrier(); }
        public override string StatusLine
        {
            get
            {
                var parts = new List<string>();
                if (Now < sanctuaryUntil) parts.Add("Sanctuary " + (sanctuaryUntil - Now).ToString("0") + "s");
                if (Now < steadfastUntil) parts.Add("Steadfast " + (steadfastUntil - Now).ToString("0") + "s");
                if (Now < lanternUntil) parts.Add("Lantern Light");
                return parts.Count == 0 ? null : string.Join("  ·  ", parts);
            }
        }
        public override string TargetStatus(EncounterEnemy enemy) { return enemy != null && brands.TryGetValue(enemy, out var n) ? "Branded x" + n : null; }
    }
}
#endif
