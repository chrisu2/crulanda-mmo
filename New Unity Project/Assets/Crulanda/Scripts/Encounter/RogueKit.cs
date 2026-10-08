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
    /// 2 Eviscerate, 3 Sap, 4 Gouge, 5 Kick, 6 Blind, 7 Vanish. Talents: Talents/rogue.json, all 21 implemented (2026-10-08).
    /// </summary>
    public sealed class RogueKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "tg-long-sap", "tg-quiet-hands", "tg-light-step", "tg-second-sap", "tg-shadow-shift", "tg-pickpocket", "tg-graces-end",
            "sw-keen-edge", "sw-ruthless", "sw-shard-poison", "sw-quick-blades", "sw-relentless", "sw-spent-shards", "sw-cut-deep",
            "lk-quick-kick", "lk-dust", "lk-feel-the-pins", "lk-ward-break", "lk-tumblers", "lk-hum", "lk-vibration" };
        public static readonly string[] BarIds = { "rogue.sinister_strike", "rogue.eviscerate", "rogue.sap", "rogue.gouge", "rogue.kick", "rogue.blind", "rogue.vanish" };
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
        public override float SwingInterval(float baseInterval) { return baseInterval * .85f * (1 - .05f * R("sw-quick-blades")); }   // quick blades
        public override float MoveSpeedMultiplier { get { return Sneaking ? 1 + .04f * R("tg-light-step") : 1; } }
        public override void ResetState() { Combo = 0; comboOn = null; graceUsed = false; bleeds.Clear(); }
        public override string StatusLine { get { return Combo > 0 ? "Combo " + new string('*', Combo) + new string('-', MaxCombo - Combo) : null; } }
        public override string TargetStatus(EncounterEnemy enemy) { return enemy != null ? enemy.ControlLabel : null; }

        public override int ActionCount { get { return BarIds.Length; } }
        public override AbilityDefinition ActionAt(int slot) { return slot >= 0 && slot < BarIds.Length && abilities.TryGetValue(BarIds[slot], out var a) ? a : null; }
        public override string ActionLockLabel(int slot)
        {
            var a = ActionAt(slot); if (a == null || s.Player == null) return "Unavailable";
            var u = unlocks[a.id];
            if (Level < u.level) return "Level " + u.level;
            if (!string.IsNullOrEmpty(u.talentId) && R(u.talentId) == 0) return "Talent";
            if (a.id == "rogue.eviscerate" && ComboOn(s.Target) == 0) return "No combo";
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
            return Start(a, () => { s.BeginAutoAttack(); t.Receive(Dmg((a.power + s.WeaponDamage) * (1 + .04f * R("sw-keen-edge"))), s.Player); AddCombo(t, 1); });
        }
        bool Eviscerate(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target; int cp = ComboOn(t);
            if (cp == 0) { s.Message("No combo points on " + t.actor.DisplayName + "."); return false; }
            return Start(a, () => {
                float per = a.power * (1 + .06f * R("sw-ruthless"));
                float spent = R("sw-spent-shards") > 0 && (t.Feared || t.Silenced || t.Stunned) ? 1.3f : 1;   // Spent Shards
                t.Receive(Dmg((per * cp + s.WeaponDamage) * spent), s.Player); Combo = 0;
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
