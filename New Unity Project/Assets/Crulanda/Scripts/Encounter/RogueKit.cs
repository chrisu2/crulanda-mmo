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
    /// Rogue (Docs/CC_DESIGN.md section 0, 2026-10-08; Valen's line from book 1: Thief's Grace, spent shards, vibration-locking; the kit
    /// GAME-ONLY until checked against the books): the first crowd-control class. Melee on Focus, with combo points (0-5) built by
    /// Sinister Strike and Gouge and spent by Eviscerate. Sap holds a mob out of the fight for 40 s from out of combat (best from a
    /// sneak, Ctrl); Gouge holds the one in front of you briefly; Blind sends one wandering; Kick stops a blow and silences; Vanish
    /// drops every mob's interest in you. The holds are the CC engine's (EncounterEnemy.Control). Bar: 1 Sinister Strike,
    /// 2 Eviscerate, 3 Sap, 4 Gouge, 5 Kick, 6 Blind, 7 Vanish; the Swashbuckler's 8 Opening Cut, 9 Rallying Flourish, 0 Tumble (talent).
    /// Talents: Talents/rogue.json, the first three branches' 21 (2026-10-08) and the Swashbuckler's rows 0-3 (2026-10-11).
    /// </summary>
    public sealed class RogueKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "tg-long-sap", "tg-quiet-hands", "tg-light-step", "tg-second-sap", "tg-shadow-shift", "tg-pickpocket", "tg-graces-end",
            "sw-keen-edge", "sw-ruthless", "sw-shard-poison", "sw-quick-blades", "sw-relentless", "sw-spent-shards", "sw-cut-deep",
            "lk-quick-kick", "lk-dust", "lk-feel-the-pins", "lk-ward-break", "lk-tumblers", "lk-hum", "lk-vibration",
            "bk-showmanship", "bk-light-blade", "bk-from-behind", "bk-long-flourish", "bk-tumble", "bk-wide-open", "bk-encore", "bk-dashing" };   // the Swashbuckler, rows 0-3
        public static readonly string[] BarIds = { "rogue.sinister_strike", "rogue.eviscerate", "rogue.sap", "rogue.gouge", "rogue.kick", "rogue.blind", "rogue.vanish",
            "rogue.opening_cut", "rogue.rallying_flourish", "rogue.tumble", "rogue.throw_knife" };
        public const int MaxCombo = 5;
        public const float SapSeconds = 40, GougeSeconds = 4, BlindSeconds = 10, KickSilence = 3;

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        EncounterEnemy comboOn;
        bool graceUsed;
        readonly List<(EncounterEnemy e, int left, float next, int dmg)> bleeds = new List<(EncounterEnemy, int, float, int)>();
        /// <summary>Combo points on the current target (they stay on the mob they were built on).</summary>
        public int Combo { get; private set; }

        public RogueKit(EncounterSession session, ClassDefinition definition, IEnumerable<AbilityDefinition> catalog) : base(session)
        {
            if (definition == null || definition.unlocks == null || catalog == null) throw new ArgumentException("Rogue content missing.");
            foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !abilities.ContainsKey(a.id)) abilities.Add(a.id, a);
            foreach (var u in definition.unlocks)
                if (u == null || string.IsNullOrEmpty(u.abilityId) || u.level < 1 || unlocks.ContainsKey(u.abilityId)) throw new ArgumentException("Invalid or duplicate Rogue unlock.");
                else unlocks.Add(u.abilityId, u);
            foreach (var id in BarIds)
                if (!abilities.ContainsKey(id) || !unlocks.ContainsKey(id)) throw new ArgumentException("Rogue ability '" + id + "' is missing from content or unlocks.");
        }
        public override string[] ImplementedTalents { get { return ImplementedIds; } }
        int R(string id) { return s.TalentRank(id); }
        int Level { get { return s.Player.Level; } }
        bool Sneaking { get { var m = s.Player != null ? s.Player.GetComponent<AdventurerMotor>() : null; return m != null && m.Sneaking; } }
        public override float SwingInterval(float baseInterval) { return baseInterval * .85f * (1 - .05f * R("sw-quick-blades")) * (1 - Haste); }   // quick blades; the Flourish
        public override float MoveSpeedMultiplier { get { return Sneaking ? 1 + .04f * R("tg-light-step") : 1; } }
        public override void ResetState() { Combo = 0; comboOn = null; graceUsed = false; bleeds.Clear(); exposedUntil.Clear(); flourishUntil = tumbleUntil = 0; flourish = 0; }
        public override string StatusLine
        {
            get
            {
                var parts = new List<string>();
                if (Combo > 0) parts.Add("Combo " + new string('*', Combo) + new string('-', MaxCombo - Combo));
                if (Haste > 0) parts.Add("Flourish " + Mathf.RoundToInt(Haste * 100) + "% " + (flourishUntil - Time.time).ToString("0") + "s");
                if (Tumbled) parts.Add("Tumbled: strike from behind");
                return parts.Count == 0 ? null : string.Join("  ·  ", parts);
            }
        }
        public override string TargetStatus(EncounterEnemy enemy)
        {
            if (enemy == null) return null;
            string held = enemy.ControlLabel, open = Exposed(enemy) ? "EXPOSED " + (exposedUntil[enemy] - Time.time).ToString("0.0") + "s" : null;
            return held != null && open != null ? held + "  ·  " + open : held ?? open;
        }

        public override int ActionCount { get { return BarIds.Length; } }
        public override AbilityDefinition ActionAt(int slot) { return slot >= 0 && slot < BarIds.Length && abilities.TryGetValue(BarIds[slot], out var a) ? a : null; }
        public override string ActionLockLabel(int slot)
        {
            var a = ActionAt(slot); if (a == null || s.Player == null) return "Unavailable";
            var u = unlocks[a.id];
            if (Level < u.level) return "Level " + u.level;
            if (!string.IsNullOrEmpty(u.talentId) && R(u.talentId) == 0) return "Talent";
            if ((a.id == "rogue.eviscerate" || a.id == "rogue.rallying_flourish") && ComboOn(s.Target) == 0) return "No combo";
            if (a.id == "rogue.sap" && s.InCombat) return "In combat";
            return null;
        }
        public override bool Use(int slot)
        {
            var a = ActionAt(slot); if (a == null) return false;
            switch (a.id)
            {
                case "rogue.sinister_strike": return SinisterStrike(a);
                case "rogue.eviscerate": return Eviscerate(a);
                case "rogue.sap": return Sap(a);
                case "rogue.gouge": return Gouge(a);
                case "rogue.kick": return Kick(a);
                case "rogue.blind": return Blind(a);
                case "rogue.vanish": return Vanish(a);
                case "rogue.opening_cut": return OpeningCut(a);   // the Swashbuckler
                case "rogue.rallying_flourish": return RallyingFlourish(a);
                case "rogue.tumble": return Tumble(a);
                case "rogue.throw_knife": return ThrowKnife(a);
            }
            return false;
        }
        bool Start(AbilityDefinition a, Action effect) { return s.StartAbility(a, () => { if (s.Player.IsAlive) effect(); }) == AbilityStartResult.Started; }
        int Dmg(float amount) { return Mathf.Max(1, Mathf.RoundToInt(amount)); }
        int ComboOn(EncounterEnemy t) { return t != null && t == comboOn ? Combo : 0; }
        void AddCombo(EncounterEnemy t, int n) { if (t != comboOn) { comboOn = t; Combo = 0; } Combo = Mathf.Clamp(Combo + n, 0, MaxCombo); }
        bool Held(EncounterEnemy t, string why) { if (why == null) return true; s.Message(t.actor.DisplayName + ": " + why + "."); return false; }
        void GainFocus(int n) { if (n <= 0) return; var p = s.Player.Resource.Pool; p.SetCurrent(Mathf.Min(p.Max, p.Current + n)); }

        bool SinisterStrike(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => { s.BeginAutoAttack(); t.Receive(Dmg((a.power + s.WeaponDamage) * (1 + .04f * R("sw-keen-edge")) * Flank(t)), s.Player); AddCombo(t, 1); });
        }
        bool Eviscerate(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target; int cp = ComboOn(t);
            if (cp == 0) { s.Message("No combo points on " + t.actor.DisplayName + "."); return false; }
            return Start(a, () => {
                float per = a.power * (1 + .06f * R("sw-ruthless"));
                float spent = R("sw-spent-shards") > 0 && (t.Feared || t.Silenced || t.Stunned) ? 1.3f : 1;   // Spent Shards
                t.Receive(Dmg((per * cp + s.WeaponDamage) * spent * Flank(t)), s.Player); Combo = 0;
                GainFocus(4 * cp * R("sw-relentless"));   // Relentless
                if (cp >= MaxCombo && R("sw-cut-deep") > 0 && t.actor.IsAlive) bleeds.Add((t, 4, Time.time + 2, Dmg(2 + Level / 2f)));   // Cut Deep
            });
        }
        /// <summary>Sap: out of combat, a mob not yet in a fight is held out of the coming one (breaks on damage).</summary>
        bool Sap(AbilityDefinition a)
        {
            if (s.InCombat) { s.Message("Not in a fight: Sap is for before the pull."); return false; }
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            if (t.Engaged) { s.Message(t.actor.DisplayName + " is already fighting."); return false; }
            return Start(With(a, a.cost - 5 * R("tg-second-sap"), a.cooldown), () => {
                if (!Held(t, t.Apply("incap", s.Player, SapSeconds + 10 * R("tg-long-sap")))) return;
                int coins = R("tg-pickpocket") > 0 ? UnityEngine.Random.Range(1, 2 + 2 * R("tg-pickpocket")) : 0;   // Light Fingers
                if (coins > 0) { s.Progress.gold += coins; s.Message("Light fingers: " + coins + " coin from " + t.actor.DisplayName + "'s pocket."); }
            });
        }
        bool Gouge(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cost - 5 * R("lk-feel-the-pins"), a.cooldown), () => {
                t.Receive(Dmg(a.power), s.Player);
                if (Held(t, t.Apply("incap", s.Player, GougeSeconds + R("lk-tumblers")))) AddCombo(t, 1);
            });
        }
        bool Kick(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cost, a.cooldown - 2 * R("lk-quick-kick")), () => {
                t.Receive(Dmg(a.power * (1 + .5f * R("lk-ward-break"))), s.Player);
                Held(t, t.Apply("silence", s.Player, KickSilence * (R("lk-vibration") > 0 ? 2 : 1)));
                if (R("lk-hum") > 0 && t.actor.IsAlive) t.Apply("stun", s.Player, 1);   // The Hum
            });
        }
        bool Blind(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => Held(t, t.Apply("fear", s.Player, BlindSeconds + 2 * R("lk-dust"))));
        }
        /// <summary>Vanish: every mob after you loses sight of you; with nobody else to fight it goes home. Out of the fight you can Sap again.</summary>
        bool Vanish(AbilityDefinition a)
        {
            return Start(With(a, a.cost, a.cooldown - 30 * R("tg-shadow-shift")), () => {
                int n = 0;
                foreach (var e in s.Enemies.ToArray()) if (e != null && e.actor.IsAlive && e.Engaged && e.Victim == s.Player) { e.LoseSight(s.Player, 6 + 4 * R("tg-quiet-hands")); n++; }
                s.StopAutoAttack(); s.FloatText(s.Player.transform.position, "Vanished", new Color(.7f, .7f, .8f));
                s.Message(n > 0 ? "You slip away: " + n + (n == 1 ? " foe loses" : " foes lose") + " sight of you." : "You slip into the shadows.");
            });
        }
        // ---------- the Swashbuckler (support; 2026-10-11, TALENT_DEPTH.md: "flanking, exposing, party haste"; GAME-ONLY) ----------
        // A duellist who opens a foe up for the party and sets its pace: Opening Cut Exposes the target (the party's blows on it land
        // harder), Rallying Flourish spends the combo on haste (your swings and Mira's casts), Tumble rolls you to its back.
        readonly Dictionary<EncounterEnemy, float> exposedUntil = new Dictionary<EncounterEnemy, float>();
        float flourishUntil, flourish, tumbleUntil;
        public const float ExposedBonus = .08f, WideOpenPerRank = .02f, FromBehindPerRank = .04f, TumbleBonus = .3f, HastePerPoint = .04f, EncoreMend = .05f;
        public bool Exposed(EncounterEnemy e) { return e != null && exposedUntil.TryGetValue(e, out var until) && Time.time < until; }
        /// <summary>The Flourish's haste while it lasts (a share off every swing, and Mira's casts).</summary>
        public float Haste { get { return Time.time < flourishUntil ? flourish : 0; } }
        public bool Tumbled { get { return Time.time < tumbleUntil; } }
        /// <summary>Behind it: on the far side of where it faces.</summary>
        public bool Behind(EncounterEnemy t)
        {
            if (t == null || s.Player == null) return false;
            var to = s.Player.transform.position - t.transform.position; to.y = 0; var ahead = t.transform.forward; ahead.y = 0;
            return to.sqrMagnitude > .01f && ahead.sqrMagnitude > .01f && Vector3.Dot(ahead.normalized, to.normalized) < -.3f;
        }
        /// <summary>A blow of yours from behind: From Behind's share, and Tumble's (spent on the first).</summary>
        float Flank(EncounterEnemy t)
        {
            bool tumbled = Tumbled; if (!tumbled && !Behind(t)) return 1;   // a foe turns to face whoever it fights: the roll's opening counts as behind
            float m = 1 + FromBehindPerRank * R("bk-from-behind");
            if (Tumbled) { m += TumbleBonus; tumbleUntil = 0; }
            return m;
        }
        public override float PartyDamageMultiplier(EncounterEnemy enemy) { return Exposed(enemy) ? 1 + ExposedBonus + WideOpenPerRank * R("bk-wide-open") : 1; }
        public override float CompanionHaste { get { return Haste; } }
        IEnumerable<Actor> Party()
        {
            yield return s.Player;
            if (s.Progress.recruited && s.Companion != null) yield return s.Companion.actor;
            foreach (var c in s.PartySims) if (c != null && c.actor != null) yield return c.actor;
        }
        /// <summary>Opening Cut: a quick cut that Exposes the target for 8 s (Showmanship: longer); a combo point.</summary>
        bool OpeningCut(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cost - 5 * R("bk-light-blade"), a.cooldown), () => {
                s.BeginAutoAttack(); t.Receive(Dmg((a.power + s.WeaponDamage * .5f) * Flank(t)), s.Player); AddCombo(t, 1);
                if (!t.actor.IsAlive) return;
                exposedUntil[t] = Time.time + a.duration + 2 * R("bk-showmanship");
                s.FloatText(t.transform.position, "Exposed", new Color(1, .75f, .35f));
            });
        }
        /// <summary>Rallying Flourish: the combo on the target spent on pace: for 10 s (Long Flourish: longer) you swing and Mira casts 4%
        /// faster per point; Encore: five points also mend the party 5%.</summary>
        bool RallyingFlourish(AbilityDefinition a)
        {
            var t = s.Target; int cp = ComboOn(t);
            if (cp == 0) { s.Message("No combo points to spend: build them with Sinister Strike or Opening Cut."); return false; }
            return Start(a, () => {
                flourish = HastePerPoint * cp; flourishUntil = Time.time + a.duration + 3 * R("bk-long-flourish"); Combo = 0;
                if (cp >= MaxCombo && R("bk-encore") > 0)
                    foreach (var who in Party()) if (who != null && who.IsAlive) who.GetComponent<Combatant>().Heal(Mathf.Max(1, Mathf.RoundToInt(who.Health.Pool.Max * EncoreMend)), s.Player);
                s.FloatText(s.Player.transform.position, "Flourish!", new Color(1, .82f, .35f));
                s.Message("Rallying Flourish: you and Mira " + Mathf.RoundToInt(flourish * 100) + "% quicker for " + (flourishUntil - Time.time).ToString("0") + " seconds" + (cp >= MaxCombo && R("bk-encore") > 0 ? ", and the party mended." : "."));
            });
        }
        /// <summary>Tumble (a talent): a roll past the target to its back; your next blow within 4 s lands 30% harder, as from behind.</summary>
        bool Tumble(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cost, a.cooldown - 3 * R("bk-dashing")), () => {
                if (!t.actor.IsAlive) return;
                var ahead = t.transform.forward; ahead.y = 0; if (ahead.sqrMagnitude < .01f) ahead = Vector3.forward;
                var landing = t.transform.position - ahead.normalized * 1.5f;
                if (UnityEngine.AI.NavMesh.SamplePosition(landing, out var hit, 2, UnityEngine.AI.NavMesh.AllAreas))
                {
                    s.Player.GetComponent<AdventurerMotor>().Teleport(hit.position + Vector3.up * 1.05f);
                    var face = t.transform.position - s.Player.transform.position; face.y = 0; if (face.sqrMagnitude > .01f) s.Player.transform.rotation = Quaternion.LookRotation(face);
                }
                tumbleUntil = Time.time + a.duration;
                s.FloatText(s.Player.transform.position, "Tumble", new Color(.85f, .9f, 1));
            });
        }
        /// <summary>Throw Knife: a knife thrown at up to 20 m, light damage, the pull (Chris, 2026-10-11: "maybe a crossbow to pull but mainly
        /// melee"; a thrown knife, no mechanism). A combo point.</summary>
        bool ThrowKnife(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.Bolt(t, new Color(.8f, .82f, .85f), .1f, true); s.BeginAutoAttack();
                t.Receive(Dmg(a.power + s.WeaponDamage * .4f), s.Player); AddCombo(t, 1);
            });
        }
        /// <summary>Shard Poison: a swing sometimes leaves a burning shard in the wound.</summary>
        public override void OnAutoHit()
        {
            var t = s.Target; int r = R("sw-shard-poison");
            if (r > 0 && t != null && t.actor.IsAlive && UnityEngine.Random.value < .1f * r) { t.Receive(Dmg(3 + Level), s.Player); s.FloatText(t.transform.position, "Shard", new Color(.6f, .9f, .4f)); }
        }
        /// <summary>Thief's Grace: once a fight, a blow that would drop you misses.</summary>
        public override int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        {
            if (victim == s.Player && victim != null && victim.IsAlive && !graceUsed && R("tg-graces-end") > 0 && raw >= victim.Health.Pool.Current)
            { graceUsed = true; s.FloatText(victim.transform.position, "Thief's Grace", new Color(.95f, .85f, .35f)); s.Message("Thief's Grace: the blow that would have dropped you misses."); return 0; }
            return base.ResolveEnemyHit(enemy, victim, raw);
        }
        public override void Tick(bool inCombat)
        {
            if (!inCombat) graceUsed = false;
            for (int i = bleeds.Count - 1; i >= 0; i--)
            {
                var b = bleeds[i];
                if (b.e == null || !b.e.actor.IsAlive || b.left <= 0) { bleeds.RemoveAt(i); continue; }
                if (Time.time < b.next) continue;
                b.e.Receive(b.dmg, s.Player); bleeds[i] = (b.e, b.left - 1, Time.time + 2, b.dmg);
            }
        }
        static AbilityDefinition With(AbilityDefinition a, int cost, float cooldown)
        {
            return new AbilityDefinition { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = Mathf.Max(0, cost),
                cooldown = Mathf.Max(a.cooldown > 0 ? 1 : 0, cooldown), range = a.range, castTime = a.castTime, globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId };
        }
    }
}
