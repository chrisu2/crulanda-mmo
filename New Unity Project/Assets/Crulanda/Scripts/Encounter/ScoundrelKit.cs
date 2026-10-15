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
    /// The Scoundrel (Docs/CLASS_SCOUNDREL.md; Chris 2026-10-13: "assassin type rogue with charisma for songs/merchants/wheeling and
    /// dealing"; CANON-EXPANDED: Valen, the canon's own street-level scoundrel (world_bible.md), sang a jagged verse when his hands were
    /// gone (book 3 ch. 4); the Glass Coast's Glimmer, the sound of trade, laughter and the scoundrel's wit (book 3 ch. 11); Matriarch
    /// Vala's Command Voice, "Stay." (book 1 ch. 23); no bard order in the canon; names PROVISIONAL). A dirk in one hand and a voice:
    /// singing, no instrument, no machines. Nerve (a Focus pool of 100, named on the HUD by ClassDefinition.resourceName) is built by
    /// blade strikes (Quick Cut, every swing) and spent on verses and finishers.
    /// Bar: 1 Quick Cut, 2 Jeer (every path); the Cutthroat's 3 Settle the Score, 4 Poisoned Wine, 5 Mark the Debt; the Silver Tongue's
    /// 6 Glimmer, 7 Jagged Verse, 8 Stay; the Fixer's 9 War-Verse, 10 Rallying Verse, 11 Bribe, 12 Call in a Favour. Each path's
    /// abilities are learnt by that path's own talents (2026-10-13: the Cleric's and Shaman's bars locked nothing, so every build cast
    /// everything). Three paths (Docs/classes: Talents/scoundrel.json): Cutthroat (dps), Silver Tongue (control), Fixer (support).
    /// </summary>
    public sealed class ScoundrelKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "ct-settle", "ct-keen-dirk", "ct-steady-nerve", "ct-poisoned-wine", "ct-strong-vintage", "ct-mark-debt", "ct-compounding", "ct-paid-in-full",
            "st-glimmer", "st-long-glimmer", "st-silver-voice", "st-jagged-verse", "st-sour-note", "st-stay", "st-held-word", "st-whole-room",
            "fx-war-verse", "fx-rousing", "fx-good-company", "fx-rallying-verse", "fx-bribe", "fx-deep-pockets", "fx-favour", "fx-old-friends", "fx-everyones-friend" };
        public static readonly string[] BarIds = { "scoundrel.quick_cut", "scoundrel.jeer", "scoundrel.settle_score", "scoundrel.poisoned_wine", "scoundrel.mark_debt",
            "scoundrel.glimmer", "scoundrel.jagged_verse", "scoundrel.stay", "scoundrel.war_verse", "scoundrel.rallying_verse", "scoundrel.bribe", "scoundrel.favour" };
        /// <summary>The path each gated ability belongs to (its talent is in that branch of Talents/scoundrel.json); Quick Cut and Jeer are every path's.</summary>
        public static readonly Dictionary<string, string> PathOf = new Dictionary<string, string>(StringComparer.Ordinal) {
            { "scoundrel.settle_score", "cutthroat" }, { "scoundrel.poisoned_wine", "cutthroat" }, { "scoundrel.mark_debt", "cutthroat" },
            { "scoundrel.glimmer", "silver-tongue" }, { "scoundrel.jagged_verse", "silver-tongue" }, { "scoundrel.stay", "silver-tongue" },
            { "scoundrel.war_verse", "fixer" }, { "scoundrel.rallying_verse", "fixer" }, { "scoundrel.bribe", "fixer" }, { "scoundrel.favour", "fixer" } };
        public const int QuickCutNerve = 8, SwingNerve = 3, SteadyNervePerRank = 2, GlimmerMost = 4, SilverVoicePerRank = 3;
        public const float QuickCutWeapon = .2f, JeerWeapon = .4f, SettleWeapon = .5f, SettlePer = 30, JaggedWeapon = .3f, KeenPerRank = .04f, VintagePerRank = .1f;
        public const float DebtShare = .2f, CompoundingPerRank = .05f, PaidInFullBelow = .35f;
        public const float GlimmerReach = 8, JaggedReach = 6, JaggedSlow = .4f, JaggedSlowSeconds = 5, FavourSeconds = 10, OldFriendsPerRank = 3, DeepPocketsPerRank = 5;
        public const float WarLift = .06f, RousingPerRank = .01f, GoodCompanyPerRank = .02f, GoodCompanyReach = 15, FriendHaste = .1f;
        public enum Verses { None, War }
        static readonly Color VerseColour = new Color(.95f, .6f, .7f), DebtColour = new Color(1, .82f, .35f);

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        readonly PeriodicEffects periodic = new PeriodicEffects();
        /// <summary>Marked debts: when each comes due, and what your blows on it have run up so far.</summary>
        readonly Dictionary<EncounterEnemy, (float due, float owed)> debts = new Dictionary<EncounterEnemy, (float, float)>();
        /// <summary>The verse being sung (one at a time; the War-Verse is the only one kept up).</summary>
        public Verses Verse { get; private set; }
        float rallyUntil, fightAt, drain; Crulanda.Gameplay.VitalPool wokePool;

        public ScoundrelKit(EncounterSession session, ClassDefinition definition, IEnumerable<AbilityDefinition> catalog) : base(session)
        {
            if (definition == null || definition.unlocks == null || catalog == null) throw new ArgumentException("Scoundrel content missing.");
            foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !abilities.ContainsKey(a.id)) abilities.Add(a.id, a);
            foreach (var u in definition.unlocks)
                if (u == null || string.IsNullOrEmpty(u.abilityId) || u.level < 1 || u.level > 10 || unlocks.ContainsKey(u.abilityId)) throw new ArgumentException("Invalid or duplicate Scoundrel unlock.");
                else unlocks.Add(u.abilityId, u);
            foreach (var id in BarIds) if (!abilities.ContainsKey(id) || !unlocks.ContainsKey(id)) throw new ArgumentException("Scoundrel ability '" + id + "' is missing from content or unlocks.");
        }
        public override string[] ImplementedTalents { get { return ImplementedIds; } }
        int R(string id) { return s.TalentRank(id); }
        float Now { get { return Time.time; } }
        int Level { get { return s.Player.Level; } }
        int Dmg(float amount) { return Mathf.Max(1, Mathf.RoundToInt(amount)); }
        bool Need(bool ok, string message) { if (!ok) s.Message(message); return ok; }
        bool Start(AbilityDefinition a, Action effect) { return s.StartAbility(a, () => { if (s.Player.IsAlive) effect(); }) == AbilityStartResult.Started; }
        static AbilityDefinition With(AbilityDefinition a, int cost, float cooldown)
        {
            return new AbilityDefinition { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = Mathf.Max(0, cost),
                cooldown = Mathf.Max(a.cooldown > 0 ? 1 : 0, cooldown), range = a.range, castTime = a.castTime, globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId };
        }
        bool Held(EncounterEnemy t, string why) { if (why == null) return true; s.Message(t.actor.DisplayName + ": " + why + "."); return false; }
        void Nerve(int n) { if (n <= 0) return; var p = s.Player.Resource.Pool; p.SetCurrent(Mathf.Min(p.Max, p.Current + n)); }
        List<EncounterEnemy> FoesNear(Vector3 at, float r) { return s.Enemies.Where(e => e != null && e.actor.IsAlive && !e.Game && Vector3.Distance(e.transform.position, at) <= r).ToList(); }
        /// <summary>One of your blows on <paramref name="t"/>: dealt, and run up on its debt if it is marked.</summary>
        void Hit(EncounterEnemy t, float amount) { if (t == null || !t.actor.IsAlive) return; int d = Dmg(amount); t.Receive(d, s.Player); Owe(t, d); }
        void Owe(EncounterEnemy t, int dealt) { if (t != null && dealt > 0 && debts.TryGetValue(t, out var debt)) debts[t] = (debt.due, debt.owed + dealt); }
        float Keen { get { return 1 + KeenPerRank * R("ct-keen-dirk"); } }

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
                case "scoundrel.quick_cut": return QuickCut(a);
                case "scoundrel.jeer": return Jeer(a);
                case "scoundrel.settle_score": return SettleScore(a);
                case "scoundrel.poisoned_wine": return PoisonedWine(a);
                case "scoundrel.mark_debt": return MarkDebt(a);
                case "scoundrel.glimmer": return Glimmer(a);
                case "scoundrel.jagged_verse": return JaggedVerse(a);
                case "scoundrel.stay": return Stay(a);
                case "scoundrel.war_verse": return WarVerse(a);
                case "scoundrel.rallying_verse": return RallyingVerse(a);
                case "scoundrel.bribe": return Bribe(a);
                case "scoundrel.favour": return Favour(a);
            }
            return false;
        }

        // ---------- every path (GAME-ONLY) ----------
        /// <summary>Quick Cut: a quick dirk cut that builds Nerve (Steady Nerve: more) and starts the swings.</summary>
        bool QuickCut(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => { s.BeginAutoAttack(); Hit(t, (a.power + s.WeaponDamage * QuickCutWeapon) * Keen); Nerve(QuickCutNerve + SteadyNervePerRank * R("ct-steady-nerve")); });
        }
        /// <summary>Jeer: a mocking verse shouted at up to 20 m; light damage, and the foe comes for you (the pull: a voice, not a bow).</summary>
        bool Jeer(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.BeginAutoAttack(); s.FloatText(t.transform.position + Vector3.up * .4f, "Jeered", VerseColour);
                Hit(t, a.power + s.WeaponDamage * JeerWeapon);
            });
        }

        // ---------- the Cutthroat (dps, single target; GAME-ONLY) ----------
        /// <summary>Settle the Score: a finisher that takes all your Nerve (its cost at least) into one cut; the more Nerve, the harder.</summary>
        bool SettleScore(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                var pool = s.Player.Resource.Pool; int extra = pool.Current; pool.Change(-extra); int spent = a.cost + extra;
                Hit(t, (a.power + s.WeaponDamage * SettleWeapon) * spent / SettlePer * Keen);
                s.FloatText(t.transform.position + Vector3.up * .4f, "Settled", new Color(.9f, .3f, .35f));
            });
        }
        /// <summary>Poisoned Wine: damage every 2 s for its duration (Strong Vintage: more).</summary>
        bool PoisonedWine(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!t.actor.IsAlive) return;
                periodic.Add("wine", t, Now, 2, Mathf.Max(1, Mathf.RoundToInt(a.duration / 2)), Dmg((a.power + .6f * Level) * (1 + VintagePerRank * R("ct-strong-vintage"))),
                    (e, i) => { var enemy = (EncounterEnemy)e.target; if (!enemy.actor.IsAlive) return false; Hit(enemy, e.value); return true; });
            });
        }
        public bool Poisoned(EncounterEnemy e) { return e != null && periodic.Has("wine", e); }
        /// <summary>Mark the Debt: for its duration a share of your blows on the target is run up; when the mark ends the debt comes due in one blow.</summary>
        bool MarkDebt(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!t.actor.IsAlive) return;
                debts[t] = (Now + a.duration, 0f); s.FloatText(t.transform.position + Vector3.up * .4f, "Marked", DebtColour);
            });
        }
        public bool Indebted(EncounterEnemy e) { return e != null && debts.ContainsKey(e); }
        /// <summary>What your blows on a marked foe have run up so far (0 when it carries no debt).</summary>
        public float Owed(EncounterEnemy e) { return e != null && debts.TryGetValue(e, out var d) ? d.owed : 0; }
        /// <summary>The debt comes due: the mark's power and level, and a share of what was run up (Compounding: more; Paid in Full: double under 35%).</summary>
        void Collect(EncounterEnemy t, float owed)
        {
            var a = abilities["scoundrel.mark_debt"];
            float blow = a.power + 2 * Level + owed * (DebtShare + CompoundingPerRank * R("ct-compounding"));
            if (R("ct-paid-in-full") > 0 && t.actor.Health.Pool.Ratio < PaidInFullBelow) blow *= 2;
            t.Receive(Dmg(blow), s.Player);   // not run up again
            s.FloatText(t.transform.position + Vector3.up * .6f, "The debt comes due", DebtColour);
        }

        // ---------- the Silver Tongue (crowd control; CANON-EXPANDED) ----------
        int VerseCost(AbilityDefinition a) { return a.cost - SilverVoicePerRank * R("st-silver-voice"); }
        /// <summary>Glimmer (the Glass Coast's Glimmer, book 3 ch. 11): the target and up to three more within 8 m of it are dazed (the CC
        /// engine's incap; any blow wakes the one it lands on). Long Glimmer: longer. The Whole Room: a fifth, and 2 m further.</summary>
        bool Glimmer(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, VerseCost(a), a.cooldown), () => {
                if (!t.actor.IsAlive) return;
                bool room = R("st-whole-room") > 0; float reach = GlimmerReach + (room ? 2 : 0), seconds = a.duration + R("st-long-glimmer"); int most = GlimmerMost + (room ? 1 : 0);
                var near = FoesNear(t.transform.position, reach).Where(e => e != t).OrderBy(e => Vector3.Distance(e.transform.position, t.transform.position)).Take(most - 1).ToList();
                near.Insert(0, t); int n = 0;
                foreach (var e in near) if (e.Apply("incap", s.Player, seconds) == null) n++;
                s.Message(n == 0 ? "The Glimmer finds no one to take." : "Glimmer: " + n + (n == 1 ? " foe stands" : " foes stand") + " dazed.");
            });
        }
        /// <summary>Jagged Verse (Valen's jagged, dissonant verse, book 3 ch. 4): light damage; the target's cast is stopped and it is
        /// silenced; foes within 6 m of it are slowed. Sour Note: slows more, back sooner.</summary>
        bool JaggedVerse(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, VerseCost(a), a.cooldown - R("st-sour-note")), () => {
                if (!s.LandsOn(t, a.range)) return;
                Hit(t, a.power + s.WeaponDamage * JaggedWeapon);
                if (t.actor.IsAlive) Held(t, t.Apply("silence", s.Player, a.duration));
                float slow = JaggedSlow + .05f * R("st-sour-note");
                foreach (var e in FoesNear(t.transform.position, JaggedReach)) e.Slow(slow, JaggedSlowSeconds);
            });
        }
        /// <summary>Stay (Matriarch Vala's Command Voice, book 1 ch. 23, borrowed): the target is stunned; blows do not break it. Held Word: longer.</summary>
        bool Stay(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.FloatText(t.transform.position + Vector3.up * .4f, "\"Stay.\"", VerseColour);
                Held(t, t.Apply("stun", s.Player, a.duration + .5f * R("st-held-word")));
            });
        }

        // ---------- the Fixer (support; GAME-ONLY) ----------
        float WarBonus { get { return WarLift + RousingPerRank * R("fx-rousing"); } }
        public bool Singing { get { return Verse != Verses.None; } }
        /// <summary>The War-Verse: while sung, the party's blows land harder (PartyDamageMultiplier); use again to stop.</summary>
        bool WarVerse(AbilityDefinition a)
        {
            if (Verse == Verses.War) { Verse = Verses.None; s.Message("You let the War-Verse fall silent."); return true; }
            return Start(a, () => {
                Verse = Verses.War; s.FloatText(s.Player.transform.position, "War-Verse", VerseColour);
                s.Message("You strike up the War-Verse: the party's blows land " + Mathf.RoundToInt(100 * WarBonus) + "% harder while you sing.");
            });
        }
        /// <summary>The Rallying Verse: for its duration you gain its power in Nerve each second, and Mira as much mana.</summary>
        bool RallyingVerse(AbilityDefinition a)
        {
            return Start(a, () => {
                int ticks = Mathf.Max(1, Mathf.RoundToInt(a.duration)); rallyUntil = Now + ticks;
                periodic.Add("rally", s.Player, Now, 1, ticks, Mathf.Max(1, a.power), (e, i) => {
                    Nerve(e.value);
                    if (s.Progress.recruited && s.Companion != null && s.Companion.gameObject.activeInHierarchy && s.Companion.actor.IsAlive) s.Companion.actor.Resource.Pool.Change(e.value);
                    return true; });
                s.FloatText(s.Player.transform.position, "Rallying Verse", VerseColour);
            });
        }
        /// <summary>Who takes a bribe: not an elite (nor an elite-strength group mob), not game, not a beast or a dead or unmade thing.</summary>
        public static bool Bribable(EncounterEnemy t)
        {
            if (t == null || t.actor == null || !t.actor.IsAlive || t.Elite || t.Tough || t.Game) return false;
            var figure = t.GetComponent<ActorVisual>(); if (figure == null) return true;
            var look = figure.Look;
            return !ActorVisual.IsBeast(look) && look != ActorLook.Pale && look != ActorLook.Hollow && look != ActorLook.Keeper && look != ActorLook.Skeleton && look != ActorLook.RockEater;
        }
        /// <summary>Bribe: a foe that can be bought takes your crowns (its level) and stops fighting (the CC engine's incap; any blow ends it). Deep Pockets: longer.</summary>
        bool Bribe(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            if (!Need(Bribable(t), t.actor.DisplayName + " can't be bought.")) return false;
            int crowns = Mathf.Max(1, t.actor.Level);
            if (!Need(s.Progress.gold >= crowns, "You haven't the coin: " + crowns + " crowns.")) return false;
            return Start(a, () => {
                if (!t.actor.IsAlive || s.Progress.gold < crowns) return;
                if (!Held(t, t.Apply("incap", s.Player, a.duration + DeepPocketsPerRank * R("fx-deep-pockets")))) return;
                s.Progress.gold -= crowns;
                s.FloatText(t.transform.position + Vector3.up * .4f, "Bribed", DebtColour);
                s.Message("Bribe: " + crowns + " crowns, and " + t.actor.DisplayName + " looks the other way.");
            });
        }
        /// <summary>Call in a Favour: of your allies within reach (Mira, the sims in your party), the one with the longest wait has it cut
        /// (Old Friends: more). Needs an ally who is waiting on something.</summary>
        bool Favour(AbilityDefinition a)
        {
            float reach = Mathf.Max(1, a.range); var me = s.Player.transform.position;
            HealerCompanion mira = s.Progress.recruited && s.Companion != null && s.Companion.gameObject.activeInHierarchy && s.Companion.actor.IsAlive && Vector3.Distance(s.Companion.transform.position, me) <= reach ? s.Companion : null;
            bool anyone = mira != null; SimCompanion sim = null; float simWait = 0, miraWait = mira != null ? mira.LongestWait : 0;
            foreach (var c in s.PartySims)
                if (c != null && c.actor != null && c.actor.IsAlive && Vector3.Distance(c.transform.position, me) <= reach)
                { anyone = true; float w = c.LongestWait; if (w > simWait) { simWait = w; sim = c; } }
            if (!Need(anyone, "No one here owes you a favour.")) return false;
            if (!Need(miraWait > 0 || simWait > 0, "Nobody near you is waiting on anything.")) return false;
            float seconds = FavourSeconds + OldFriendsPerRank * R("fx-old-friends");
            return Start(a, () => {
                bool toMira = mira != null && (sim == null || miraWait >= simWait);
                float cut = toMira ? mira.CallInFavour(seconds) : sim.CallInFavour(seconds);
                s.Message("You call in a favour: " + (toMira ? "Mira" : sim.sim.name) + " is ready " + cut.ToString("0") + " seconds sooner.");
            });
        }

        // ---------- the party, the blows, the swings ----------
        public override float PartyDamageMultiplier(EncounterEnemy enemy) { return Verse == Verses.War ? 1 + WarBonus : 1; }
        public override float CompanionHaste { get { return Verse == Verses.War && R("fx-everyones-friend") > 0 ? FriendHaste : 0; } }
        /// <summary>Every swing of the dirk builds Nerve, and is run up on the target's debt.</summary>
        // A swing builds no Nerve (Chris, 2026-10-14 23:20: "shouldn't get nerve on auto attack"): Nerve comes from the cuts and the
        // verses, so a finisher is earned, not waited for. The swing still marks the debt.
        public override void OnAutoHit() { var t = s.Target; if (t != null) Owe(t, s.WeaponDamage); }
        /// <summary>Good Company: while the War-Verse is sung, the party near you takes less.</summary>
        public override int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        {
            if (victim == null || !victim.IsAlive) return 0;
            int company = R("fx-good-company");
            float mult = Verse == Verses.War && company > 0 && Vector3.Distance(victim.transform.position, s.Player.transform.position) <= GoodCompanyReach ? 1 - GoodCompanyPerRank * company : 1;
            return victim.GetComponent<Combatant>().Damage(Mathf.RoundToInt(raw * mult));
        }

        // ---------- per frame / lifecycle ----------
        // Nothing is wiped when the session is out of combat (2026-10-13, the balance harness: dummies that do not fight back leave the
        // session out of combat, and the Shaman's charge was wiped every tick). Debts end when they come due; the verse when stopped.
        public override void Tick(bool inCombat)
        {
            periodic.Tick(Now);
            // Nerve is a fight's own (Chris, 2026-10-14 23:15: "scoundrel nerve should start at 0. you shouldnt be able to use a finisher
            // at start of fight"): it starts at nothing when the kit wakes, and six seconds after the last blow struck or taken it drains
            // away at ten a second. Not while auto-attacking a dummy that does not fight back (the balance harness and the capture: the
            // session is out of combat then, FightingTarget not).
            var pool = s.Player != null ? s.Player.Resource.Pool : null;
            if (pool != null)
            {
                if (pool != wokePool) { wokePool = pool; pool.SetCurrent(0); fightAt = Now; }   // a fresh pool (the kit's first tick, a respawn, a load) is bound full: emptied
                if (inCombat || s.FightingTarget) { fightAt = Now; drain = 0; }
                else if (Now - fightAt > 6 && pool.Current > 0)
                {
                    drain += Time.deltaTime * 10; int d = (int)drain;
                    if (d > 0) { pool.Change(-Mathf.Min(d, pool.Current)); drain -= d; }
                }
            }
            if (debts.Count == 0) return;
            foreach (var kv in debts.ToList())
            {
                var e = kv.Key;
                if (e == null || e.actor == null || !e.actor.IsAlive) { debts.Remove(kv.Key); continue; }
                if (Now < kv.Value.due) continue;
                debts.Remove(kv.Key); Collect(e, kv.Value.owed);
            }
        }
        public override bool RestingResourceRegen { get { return false; } }
        public override void ResetState() { periodic.Clear(); debts.Clear(); Verse = Verses.None; rallyUntil = 0; wokePool = null; drain = 0; }
        public override string StatusLine
        {
            get
            {
                var parts = new List<string>();
                if (Verse == Verses.War) parts.Add("Singing the War-Verse");
                if (Now < rallyUntil) parts.Add("Rallying " + (rallyUntil - Now).ToString("0") + "s");
                return parts.Count == 0 ? null : string.Join("  ·  ", parts);
            }
        }
        public override IEnumerable<(string name, float remaining)> PartyBuffs()
        {
            if (Verse == Verses.War) yield return ("War-Verse", 0);
            if (Now < rallyUntil) yield return ("Rallying Verse", rallyUntil - Now);
        }
        public override string TargetStatus(EncounterEnemy enemy)
        {
            if (enemy == null) return null;
            var parts = new List<string>(); var held = enemy.ControlLabel; if (held != null) parts.Add(held);
            if (debts.TryGetValue(enemy, out var d)) parts.Add("Debt due " + Mathf.Max(0, d.due - Now).ToString("0.0") + "s");
            if (Poisoned(enemy)) parts.Add("Poisoned");
            return parts.Count == 0 ? null : string.Join("  ·  ", parts);
        }
    }
}
