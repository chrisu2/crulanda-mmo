using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.Abilities;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Paladin (Phase 5.1, 2026-10-05; provisional GAME-ONLY name and mechanics, CLASS_BUILD_MATRIX: Oathguard tank, Judicator
    /// melee, Sanctuary healer): a mailed hybrid on Mana with a second, visible pool, Conviction (0-3 pips). Smite in melee builds
    /// it, blows taken under the Ward build it, and Judgement (a thrown light) or Lay On (a great heal) spend it. Oath of Ward
    /// holds a foe's attention, Consecrate burns the ground round the Paladin, Mend and Aegis keep the party up. Bar: 1 Smite,
    /// 2 Oath of Ward, 3 Mend, 4 Ward, 5 Judgement, 6 Consecrate, 7 Lay On, 8 Aegis, 9 Censure (talent), 0 Rebuke, - Plant the Colours,
    /// = Close Ranks (talent), [ Press On. The fourth path, the Vanguard (2026-10-09, TALENT_DEPTH.md; CANON-EXPANDED: a field
    /// commander, as the books' generals rally, with no church in it): the Colours planted where the party stands (10% less damage
    /// under them), Close Ranks (barriers on all, Conviction for each ally rallied) and Press On (the party's blows harder, Mira
    /// faster). Base abilities and tiers 0-3 of EncounterContent/Talents/paladin.json; tiers 4-6 are written, not built. Nothing here is saved.
    /// </summary>
    public sealed class PaladinKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "og-steadfast", "og-bulwark-of-faith", "og-answering-light", "og-censure", "og-unbroken-oath", "og-vigil", "og-hallowed-ground",
            "ju-zeal", "ju-righteous-rhythm", "ju-weighted-judgement", "ju-burning-light", "ju-swift-verdict", "ju-executioner", "ju-crusade",
            "sa-devotion", "sa-light-of-dawn", "sa-grace", "sa-aegis-of-faith", "sa-beacon", "sa-mercy", "sa-sanctuary",
            "va-colours-held", "va-press-harder", "va-rampart", "va-close-ranks", "va-press-longer", "va-steady-ranks", "va-comrades", "va-under-the-colours", "va-last-stand"
        };
        public static readonly string[] BarIds = { "paladin.smite", "paladin.oath", "paladin.mend", "paladin.ward", "paladin.judgement", "paladin.consecrate", "paladin.lay_on", "paladin.aegis", "paladin.censure", "paladin.rebuke",
            "paladin.colours", "paladin.close_ranks", "paladin.press_on" };
        public const int MaxConviction = 3, HealRange = 25;
        public const float WardMultiplier = .6f, ConsecrateRadius = 5, ColoursRadius = 8, PressHaste = .15f;
        const float ConvictionKept = 10;

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        readonly PeriodicEffects periodic = new PeriodicEffects();
        readonly object talentSource = new object();
        bool resetting;
        float convictionKeptUntil, wardUntil, wardConvictionAt, answeringAt, consecrateUntil, coloursUntil, pressUntil, pressBonus;
        int smites;
        Vector3 consecrateCentre, coloursCentre;
        readonly List<Actor> underColours = new List<Actor>();

        public int Conviction { get; private set; }
        public float WardRemaining { get { return Mathf.Max(0, wardUntil - Now); } }
        public bool Consecrating { get { return Now < consecrateUntil; } }
        /// <summary>The Vanguard's standard stands (Plant the Colours), and Press On lasts.</summary>
        public bool ColoursUp { get { return Now < coloursUntil; } }
        public float ColoursRemaining { get { return Mathf.Max(0, coloursUntil - Now); } }
        public bool Pressing { get { return Now < pressUntil; } }
        public float PressRemaining { get { return Mathf.Max(0, pressUntil - Now); } }
        float ColoursReach { get { return ColoursRadius + .5f * R("va-colours-held"); } }
        public PeriodicEffects Periodic { get { return periodic; } }

        public PaladinKit(EncounterSession session, ClassDefinition definition, IEnumerable<AbilityDefinition> catalog) : base(session)
        {
            if (definition == null || definition.unlocks == null || catalog == null) throw new ArgumentException("Paladin content missing.");
            foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !abilities.ContainsKey(a.id)) abilities.Add(a.id, a);
            foreach (var u in definition.unlocks)
                if (u == null || string.IsNullOrEmpty(u.abilityId) || u.level < 1 || u.level > 10 || unlocks.ContainsKey(u.abilityId))
                    throw new ArgumentException("Invalid or duplicate Paladin unlock.");
                else unlocks.Add(u.abilityId, u);
            foreach (var id in BarIds)
                if (!abilities.ContainsKey(id) || !unlocks.ContainsKey(id)) throw new ArgumentException("Paladin ability '" + id + "' is missing from content or unlocks.");
        }
        public override string[] ImplementedTalents { get { return ImplementedIds; } }
        int R(string id) { return s.TalentRank(id); }
        float Now { get { return Time.time; } }
        int Level { get { return s.Player.Level; } }
        Combatant PlayerCombat { get { return s.Player.GetComponent<Combatant>(); } }
        public void GainConviction(int n) { if (n <= 0) return; Conviction = Mathf.Clamp(Conviction + n, 0, MaxConviction); convictionKeptUntil = Now + ConvictionKept; }

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
                case "paladin.smite": return Smite(a);
                case "paladin.oath": return Oath(a);
                case "paladin.mend": return Mend(a);
                case "paladin.ward": return Ward(a);
                case "paladin.judgement": return Judgement(a);
                case "paladin.consecrate": return Consecrate(a);
                case "paladin.lay_on": return LayOn(a);
                case "paladin.aegis": return Aegis(a);
                case "paladin.censure": return Censure(a);
                case "paladin.rebuke": return Rebuke(a);
                case "paladin.colours": return Colours(a);
                case "paladin.close_ranks": return CloseRanks(a);
                case "paladin.press_on": return PressOn(a);
            }
            return false;
        }
        /// <summary>You, Mira when she is with you, and the sims in your party.</summary>
        IEnumerable<Actor> Party()
        {
            yield return s.Player;
            if (s.Progress.recruited && s.Companion != null) yield return s.Companion.actor;
            foreach (var c in s.PartySims) if (c != null && c.actor != null) yield return c.actor;
        }
        Actor MostHurt()
        {
            Actor best = null; float low = 1;
            foreach (var who in Party()) if (who != null && who.IsAlive && who.Health.Pool.Ratio < low) { best = who; low = who.Health.Pool.Ratio; }
            return best;
        }
        static AbilityDefinition With(AbilityDefinition a, int cost, float castTime)
        {
            return new AbilityDefinition { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = cost,
                cooldown = a.cooldown, range = a.range, castTime = castTime, globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId };
        }
        bool Start(AbilityDefinition a, Action effect) { return s.StartAbility(a, () => { if (s.Player.IsAlive) effect(); }) == AbilityStartResult.Started; }
        bool Need(bool ok, string message) { if (!ok) s.Message(message); return ok; }
        int Dmg(float amount) { return Mathf.Max(1, Mathf.RoundToInt(amount)); }
        void Hit(EncounterEnemy t, int damage) { t.Receive(damage, s.Player); }

        /// <summary>Lowest-health living party member within heal range (you or recruited Mira).</summary>
        public Actor HealTarget
        {
            get
            {
                Actor best = s.Player;
                if (s.Progress.recruited && s.Companion != null && s.Companion.actor.IsAlive &&
                    Vector3.Distance(s.Player.transform.position, s.Companion.transform.position) <= HealRange &&
                    s.Companion.actor.Health.Pool.Ratio < s.Player.Health.Pool.Ratio) best = s.Companion.actor;
                return best;
            }
        }
        int HealActor(Actor target, int amount)
        {
            if (target == null || !target.IsAlive || amount <= 0) return 0;
            int healed = target.GetComponent<Combatant>().Heal(amount); if (healed > 0) HealFx.Show(target.transform);
            if (healed > 0) { s.AddHealingThreat(healed); s.FloatText(target.transform.position, "+" + healed, new Color(1, .95f, .6f)); }
            return healed;
        }
        static string Who(Actor a, EncounterSession s) { return a == s.Player ? "you" : "Mira"; }

        // ---------- Judicator: Smite, Judgement, Censure ----------
        bool Smite(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.BeginAutoAttack();
                int d = Dmg((a.power + s.WeaponDamage) * (1 + .03f * R("ju-zeal")));
                Hit(t, d); smites++;
                // Righteous Rhythm: every fourth, third or second Smite carries a second pip.
                int rhythm = R("ju-righteous-rhythm");
                GainConviction(1 + (rhythm > 0 && smites % (5 - rhythm) == 0 ? 1 : 0));
                // Comrades (Vanguard): while Press On lasts, each Smite mends the most hurt of the party a little.
                if (Pressing && R("va-comrades") > 0) { var hurt = MostHurt(); if (hurt != null && hurt.Health.Pool.Ratio < 1) HealActor(hurt, Mathf.Max(1, Mathf.RoundToInt(hurt.Health.Pool.Max * .03f))); }
                if (R("ju-crusade") > 0)
                    foreach (var o in s.Enemies)
                        if (o != t && o.actor.IsAlive && Vector3.Distance(o.transform.position, t.transform.position) <= 4) { Hit(o, Mathf.Max(1, d / 2)); break; }
            });
        }
        bool Judgement(AbilityDefinition a)
        {
            if (!Need(Conviction >= 1, "Judgement needs Conviction; Smite first.") || !s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range) || Conviction < 1) return;
                int pips = Conviction; Conviction = 0;
                float per = a.power + 6 * R("ju-weighted-judgement");
                float d = per * pips + s.WeaponDamage * .5f;
                if (t.actor.Health.Pool.Ratio < .35f) d *= 1 + .1f * R("ju-executioner");
                s.Bolt(t, new Color(1, .9f, .5f), .22f); Hit(t, Dmg(d));
                int burn = R("ju-burning-light");
                if (burn > 0) periodic.Add("burn", t, Now, 2, 3, Dmg((2 + Level / 2f) * burn), (e, i) => { var enemy = (EncounterEnemy)e.target; if (!enemy.actor.IsAlive) return false; Hit(enemy, e.value); return true; });
                if (pips >= MaxConviction && R("ju-swift-verdict") > 0) s.Player.Resource.Pool.Change(10);
            });
        }
        /// <summary>Rebuke (CC_DESIGN section 2): the shield's edge, a 4 s stun; damage doesn't break it; bosses take it too.</summary>
        bool Rebuke(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.BeginAutoAttack(); Hit(t, Dmg(a.power));
                string why = t.Apply("stun", s.Player, a.duration); if (why != null) s.Message(t.actor.DisplayName + ": " + why + ".");
            });
        }
        bool Censure(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.BeginAutoAttack(); Hit(t, Dmg(a.power + s.WeaponDamage * .4f)); t.Stagger(a.duration); t.Apply("silence", s.Player, 3);   // an interrupt
                s.Message("Censure: the target's swing is held back for " + a.duration + " seconds.");
            });
        }

        // ---------- Oathguard: Oath of Ward, Ward, Consecrate ----------
        bool Oath(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                t.threat.Taunt(s.Player.EntityId.Value, Now, a.duration);
                int threat = Mathf.RoundToInt(20 * (1 + .1f * R("og-steadfast"))), vigil = R("og-vigil");
                foreach (var o in s.Enemies)
                {
                    if (!o.actor.IsAlive || s.Distance(o) > 8) continue;
                    o.threat.Add(s.Player.EntityId.Value, threat);
                    if (o != t && vigil > 0 && s.Distance(o) <= 6) { o.threat.Taunt(s.Player.EntityId.Value, Now, a.duration); vigil--; }
                }
                s.Message("Oath of Ward: attention held for " + a.duration + " seconds.");
            });
        }
        bool Ward(AbilityDefinition a)
        {
            return Start(a, () => {
                float seconds = a.duration + R("og-unbroken-oath");
                PlayerCombat.Apply(new StatusEffectDefinition { id = "status.oathward", name = "Ward", duration = seconds, incomingDamageMultiplier = WardMultiplier });
                wardUntil = Now + seconds;
                int bulwark = R("og-bulwark-of-faith");
                if (bulwark > 0) PlayerCombat.AddBarrier(Mathf.RoundToInt(s.Player.Health.Pool.Max * .03f * bulwark), 8);
                s.Message("Ward: damage taken reduced for " + seconds + " seconds.");
            });
        }
        bool Consecrate(AbilityDefinition a)
        {
            return Start(a, () => {
                consecrateCentre = s.Player.transform.position; consecrateUntil = Now + a.duration;
                int per = Dmg(a.power + Level / 2f), sanctuary = R("sa-sanctuary");
                periodic.Add("consecrate", s.Player, Now, 1, Mathf.RoundToInt(a.duration), per, (e, i) => {
                    foreach (var o in s.Enemies) if (o.actor.IsAlive && Vector3.Distance(o.transform.position, consecrateCentre) <= ConsecrateRadius) Hit(o, e.value);
                    if (sanctuary > 0)
                        foreach (var who in new[] { s.Player, s.Progress.recruited && s.Companion != null ? s.Companion.actor : null })
                            if (who != null && who.IsAlive && Vector3.Distance(who.transform.position, consecrateCentre) <= ConsecrateRadius)
                                HealActor(who, Mathf.RoundToInt(who.Health.Pool.Max * .03f));
                    return true;
                }, null, true);
                s.Message("Consecrate: the ground here burns for " + a.duration + " seconds.");
            });
        }

        // ---------- Sanctuary: Mend, Lay On, Aegis ----------
        public int MendAmount(AbilityDefinition a) { return Mathf.RoundToInt((a.power + 2 * Level) * (1 + .04f * R("sa-devotion"))); }
        bool Mend(AbilityDefinition a)
        {
            var target = HealTarget;
            return Start(With(a, Mathf.Max(0, a.cost - 2 * R("sa-grace")), a.castTime), () => {
                int heal = MendAmount(a); HealActor(target, heal);
                int dawn = R("sa-light-of-dawn");
                if (dawn > 0) periodic.Add("dawn", target, Now, 2, 2, Mathf.Max(1, Mathf.RoundToInt(heal * .1f * dawn)), (e, i) => { var who = (Actor)e.target; if (!who.IsAlive) return false; HealActor(who, e.value); return true; });
            });
        }
        bool LayOn(AbilityDefinition a)
        {
            if (!Need(Conviction >= MaxConviction, "Lay On needs " + MaxConviction + " Conviction.")) return false;
            var target = HealTarget;
            return Start(a, () => {
                if (Conviction < MaxConviction) return; Conviction = 0;
                HealActor(target, Mathf.RoundToInt(target.Health.Pool.Max * (.25f + .08f * R("sa-mercy"))));
                if (R("sa-beacon") > 0 && abilities.TryGetValue("paladin.aegis", out var aegis)) GrantAegis(target, aegis);
                s.Message("Lay On: " + Who(target, s) + (target == s.Player ? " are" : " is") + " made whole.");
            });
        }
        void GrantAegis(Actor target, AbilityDefinition a)
        {
            if (target == null || !target.IsAlive) return;
            int faith = R("sa-aegis-of-faith");
            target.GetComponent<Combatant>().AddBarrier(Mathf.RoundToInt((a.power + Level) * (1 + .15f * faith)), a.duration + 2 * faith);
        }
        bool Aegis(AbilityDefinition a)
        {
            var target = HealTarget;
            return Start(a, () => GrantAegis(target, a));
        }

        // ---------- Vanguard: Plant the Colours, Close Ranks, Press On ----------
        /// <summary>The standard planted where you stand: for its while, every second, the party within its reach is Under the Colours
        /// (a short status, refreshed, so stepping out of reach loses it), Rampart's barrier on planting, Last Stand's mending when it falls.</summary>
        bool Colours(AbilityDefinition a)
        {
            return Start(a, () => {
                coloursCentre = s.Player.transform.position; float seconds = a.duration + R("va-colours-held"); coloursUntil = Now + seconds;
                float mult = 1 - a.power / 100f; int rampart = R("va-rampart"); float reach = ColoursReach;
                underColours.Clear();
                periodic.Add("colours", s.Player, Now, 1, Mathf.Max(1, Mathf.RoundToInt(seconds)), 0, (e, i) => {
                    underColours.Clear();
                    foreach (var who in Party())
                    {
                        if (who == null || !who.IsAlive || Vector3.Distance(who.transform.position, coloursCentre) > reach) continue;
                        who.GetComponent<Combatant>().Apply(new StatusEffectDefinition { id = "status.colours", name = "Under the Colours", duration = 1.6f, incomingDamageMultiplier = mult });
                        underColours.Add(who);
                    }
                    return true;
                }, (e, finished) => {
                    if (finished && !resetting && R("va-last-stand") > 0)
                        foreach (var who in underColours) if (who != null && who.IsAlive) HealActor(who, Mathf.Max(1, Mathf.RoundToInt(who.Health.Pool.Max * .1f)));
                    underColours.Clear(); coloursUntil = 0;
                }, true);
                if (rampart > 0)
                    foreach (var who in Party())
                        if (who != null && who.IsAlive && Vector3.Distance(who.transform.position, coloursCentre) <= reach)
                            who.GetComponent<Combatant>().AddBarrier(Mathf.Max(1, Mathf.RoundToInt(who.Health.Pool.Max * .02f * rampart)), 8);
                s.FloatText(coloursCentre + Vector3.up * 1.5f, "The Colours!", new Color(1, .82f, .35f));
                s.Message("You plant the Colours: the party within " + reach.ToString("0.#") + " m takes " + a.power + "% less for " + seconds + " seconds.");
            });
        }
        /// <summary>The muster: a barrier on every party member within reach, and Conviction for each ally who rallies (you are not an ally).</summary>
        bool CloseRanks(AbilityDefinition a)
        {
            return Start(a, () => {
                int steady = R("va-steady-ranks"), allies = 0;
                foreach (var who in Party())
                {
                    if (who == null || !who.IsAlive || Vector3.Distance(who.transform.position, s.Player.transform.position) > a.range) continue;
                    who.GetComponent<Combatant>().AddBarrier(Mathf.Max(1, Mathf.RoundToInt((a.power + Level) * (1 + .25f * steady))), a.duration);
                    if (who != s.Player) allies++;
                }
                GainConviction(Mathf.Min(MaxConviction, allies));
                s.Message("Close Ranks: " + (allies == 0 ? "nobody at your side, so a barrier on you alone." : allies + (allies == 1 ? " ally closes" : " allies close") + " on you: barriers on all, and " + allies + " Conviction."));
            });
        }
        /// <summary>Press On: the party's blows land harder (PartyDamageMultiplier) and Mira casts faster (CompanionHaste) for a while.</summary>
        bool PressOn(AbilityDefinition a)
        {
            return Start(a, () => {
                float seconds = a.duration + 2 * R("va-press-longer"); pressUntil = Now + seconds; pressBonus = (a.power + 2 * R("va-press-harder")) / 100f;
                s.FloatText(s.Player.transform.position + Vector3.up * 1.5f, "Press on!", new Color(1, .82f, .35f));
                s.Message("Press On! The party's blows hit " + Mathf.RoundToInt(pressBonus * 100) + "% harder, and Mira casts faster, for " + seconds + " seconds.");
            });
        }
        public override float PartyDamageMultiplier(EncounterEnemy enemy)
        {
            float m = 1;
            if (Pressing) m += pressBonus;
            if (ColoursUp && s.Player != null && Vector3.Distance(s.Player.transform.position, coloursCentre) <= ColoursReach) m += .02f * R("va-under-the-colours");
            return m;
        }
        public override float CompanionHaste { get { return Pressing ? PressHaste : 0; } }

        // ---------- incoming blows ----------
        public override int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        {
            if (victim == null || !victim.IsAlive) return 0;
            float mult = 1;
            if (Consecrating && R("og-hallowed-ground") > 0 && Vector3.Distance(enemy.transform.position, consecrateCentre) <= ConsecrateRadius) mult -= .1f;
            int dealt = victim.GetComponent<Combatant>().Damage(Mathf.RoundToInt(raw * mult));
            if (victim != s.Player || Now >= wardUntil) return dealt;
            // Under the Ward every blow taken steadies the Paladin (a pip at most every 1.5 s) and, with Answering Light, heals.
            if (Now >= wardConvictionAt) { GainConviction(1); wardConvictionAt = Now + 1.5f; }
            int light = R("og-answering-light");
            if (light > 0 && Now >= answeringAt) { HealActor(s.Player, Mathf.RoundToInt(s.Player.Health.Pool.Max * .02f * light)); answeringAt = Now + 2; }
            return dealt;
        }

        // ---------- per frame / lifecycle ----------
        public override void Tick(bool inCombat)
        {
            periodic.Tick(Now);
            if (!inCombat && Conviction > 0 && Now > convictionKeptUntil) Conviction = 0;
        }
        public override void ResetState()
        {
            resetting = true; periodic.Clear(); resetting = false;
            Conviction = 0; smites = 0; wardUntil = wardConvictionAt = answeringAt = consecrateUntil = coloursUntil = pressUntil = 0; underColours.Clear();
        }
        public override void ClearTransient()
        {
            resetting = true; periodic.Clear(); resetting = false;
            wardUntil = consecrateUntil = coloursUntil = pressUntil = 0; underColours.Clear();
            if (s.Player != null) { PlayerCombat.Statuses.Remove("status.oathward"); PlayerCombat.Statuses.Remove("status.colours"); PlayerCombat.ClearBarrier(); }
            if (s.Companion != null) { var mc = s.Companion.actor.GetComponent<Combatant>(); mc.Statuses.Remove("status.colours"); mc.ClearBarrier(); }
            foreach (var c in s.PartySims) if (c != null && c.actor != null) { var cc = c.actor.GetComponent<Combatant>(); if (cc != null) { cc.Statuses.Remove("status.colours"); cc.ClearBarrier(); } }
        }

        // ---------- HUD ----------
        public override string StatusLine
        {
            get
            {
                var parts = new List<string> { "Conviction " + EncounterHud.Pips(Conviction, MaxConviction) };
                if (WardRemaining > 0) parts.Add("Ward " + WardRemaining.ToString("0.0") + "s");
                if (Consecrating) parts.Add("Consecrated ground");
                if (ColoursUp) parts.Add("Colours " + ColoursRemaining.ToString("0") + "s");
                if (Pressing) parts.Add("Press On " + PressRemaining.ToString("0") + "s");
                int barrier = PlayerCombat.Barrier; if (barrier > 0) parts.Add("Barrier " + barrier);
                return string.Join("  ·  ", parts);
            }
        }
        public override string TargetStatus(EncounterEnemy enemy)
        {
            var parts = new List<string>();
            if (periodic.Has("burn", enemy)) parts.Add("Burning");
            if (Consecrating && Vector3.Distance(enemy.transform.position, consecrateCentre) <= ConsecrateRadius) parts.Add("On consecrated ground");
            return parts.Count == 0 ? null : string.Join("  ·  ", parts);
        }
    }
}
