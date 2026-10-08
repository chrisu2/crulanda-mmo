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
    /// 2 Eviscerate, 3 Sap, 4 Gouge, 5 Kick, 6 Blind, 7 Vanish. Talents: Talents/rogue.json (the implemented ones below).
    /// </summary>
    public sealed class RogueKit : ClassKit
    {
        public static readonly string[] ImplementedIds = { "tg-quiet-hands", "tg-long-sap", "sw-keen-edge", "sw-ruthless", "lk-quick-kick", "lk-dust" };
        public static readonly string[] BarIds = { "rogue.sinister_strike", "rogue.eviscerate", "rogue.sap", "rogue.gouge", "rogue.kick", "rogue.blind", "rogue.vanish" };
        public const int MaxCombo = 5;
        public const float SapSeconds = 40, GougeSeconds = 4, BlindSeconds = 10, KickSilence = 3;

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        EncounterEnemy comboOn;
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
        public override float SwingInterval(float baseInterval) { return baseInterval * .85f; }   // quick blades
        public override void ResetState() { Combo = 0; comboOn = null; }
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
                t.Receive(Dmg(per * cp + s.WeaponDamage), s.Player); Combo = 0;
            });
        }
        /// <summary>Sap: out of combat, a mob not yet in a fight is held out of the coming one (breaks on damage).</summary>
        bool Sap(AbilityDefinition a)
        {
            if (s.InCombat) { s.Message("Not in a fight: Sap is for before the pull."); return false; }
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            if (t.Engaged) { s.Message(t.actor.DisplayName + " is already fighting."); return false; }
            return Start(a, () => Held(t, t.Apply("incap", s.Player, SapSeconds + 10 * R("tg-long-sap"))));
        }
        bool Gouge(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => { t.Receive(Dmg(a.power), s.Player); if (Held(t, t.Apply("incap", s.Player, GougeSeconds))) AddCombo(t, 1); });
        }
        bool Kick(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cooldown - 2 * R("lk-quick-kick")), () => { t.Receive(Dmg(a.power), s.Player); Held(t, t.Apply("silence", s.Player, KickSilence)); });
        }
        bool Blind(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => Held(t, t.Apply("fear", s.Player, BlindSeconds + 2 * R("lk-dust"))));
        }
        /// <summary>Vanish: every mob after you loses interest (its threat on you is gone); out of the fight you can sneak and Sap again.</summary>
        bool Vanish(AbilityDefinition a)
        {
            return Start(a, () => {
                int n = 0;
                foreach (var e in s.Enemies.ToArray()) if (e != null && e.actor.IsAlive && e.Engaged && e.Victim == s.Player) { e.LoseSight(s.Player, R("tg-quiet-hands") > 0 ? 10 : 6); n++; }
                s.StopAutoAttack(); s.FloatText(s.Player.transform.position, "Vanished", new Color(.7f, .7f, .8f));
                s.Message(n > 0 ? "You slip away: " + n + (n == 1 ? " foe loses" : " foes lose") + " sight of you." : "You slip into the shadows.");
            });
        }
        static AbilityDefinition With(AbilityDefinition a, float cooldown)
        {
            return new AbilityDefinition { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = a.cost,
                cooldown = Mathf.Max(1, cooldown), range = a.range, castTime = a.castTime, globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId };
        }
    }
}
