using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.Abilities;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Ranger (Phase 5.1b, 2026-10-05; provisional GAME-ONLY name and mechanics, CLASS_BUILD_MATRIX: Marksman ranged, Beastbond
    /// pet, Pathfinder control): a bow at 25 m on Focus, with a wolf at heel. Quick Shot starts the bow's own auto-shot (the first
    /// ranged auto-attack: ClassKit.RangedAutoAttacks), Aimed Shot is the drawn 1.5 s arrow, Barbed Arrow bleeds, Hunter's Mark
    /// makes a target take more from the whole party, Snare slows, Disengage leaps back. Call Companion whistles up the wolf
    /// (RangerPet) and Sic sends it at the target. Bar: 1 Quick Shot, 2 Aimed Shot, 3 Barbed Arrow, 4 Hunter's Mark, 5 Snare,
    /// 6 Call Companion, 7 Sic, 8 Disengage, 9 Pin (talent). Base abilities and rows 0-2 of Talents/ranger.json. Nothing here is saved.
    /// </summary>
    public sealed class RangerKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "mk-steady-hand", "mk-quick-draw", "mk-piercing", "mk-bleeding-wounds", "mk-snap-shot", "mk-headshot", "mk-double-nock",
            "bb-thick-coat", "bb-sharp-teeth", "bb-mending-bond", "bb-sic-fury", "bb-pack-sense", "bb-howl", "bb-alpha",
            "pf-fleet", "pf-tangling-snare", "pf-pin", "pf-keen-eye", "pf-cover-of-leaves", "pf-trapper", "pf-pathfinder"
        };
        public static readonly string[] BarIds = { "ranger.quick_shot", "ranger.aimed_shot", "ranger.barbed_arrow", "ranger.hunters_mark", "ranger.snare", "ranger.call_companion", "ranger.sic", "ranger.disengage", "ranger.pin" };
        public const float BowRange = 25, MarkBonus = .1f, DisengageLeap = 6;

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        readonly PeriodicEffects periodic = new PeriodicEffects();
        readonly Dictionary<EncounterEnemy, float> markedUntil = new Dictionary<EncounterEnemy, float>();
        bool resetting, freeQuick;

        public PeriodicEffects Periodic { get { return periodic; } }
        /// <summary>The wolf at heel (null until called, or once it has fallen and been let go).</summary>
        public RangerPet Wolf { get { return s.Pet; } }
        public bool Marked(EncounterEnemy e) { return e != null && markedUntil.TryGetValue(e, out var until) && Now < until; }

        public RangerKit(EncounterSession session, ClassDefinition definition, IEnumerable<AbilityDefinition> catalog) : base(session)
        {
            if (definition == null || definition.unlocks == null || catalog == null) throw new ArgumentException("Ranger content missing.");
            foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !abilities.ContainsKey(a.id)) abilities.Add(a.id, a);
            foreach (var u in definition.unlocks)
                if (u == null || string.IsNullOrEmpty(u.abilityId) || u.level < 1 || u.level > 10 || unlocks.ContainsKey(u.abilityId))
                    throw new ArgumentException("Invalid or duplicate Ranger unlock.");
                else unlocks.Add(u.abilityId, u);
            foreach (var id in BarIds)
                if (!abilities.ContainsKey(id) || !unlocks.ContainsKey(id)) throw new ArgumentException("Ranger ability '" + id + "' is missing from content or unlocks.");
        }
        public override string[] ImplementedTalents { get { return ImplementedIds; } }
        int R(string id) { return s.TalentRank(id); }
        float Now { get { return Time.time; } }
        int Level { get { return s.Player.Level; } }
        Combatant PlayerCombat { get { return s.Player.GetComponent<Combatant>(); } }

        // ---------- the bow ----------
        public override bool MeleeAutoAttacks { get { return false; } }
        public override bool RangedAutoAttacks { get { return true; } }
        public override float AutoAttackRange { get { return BowRange; } }
        public override float SwingInterval(float baseInterval) { return baseInterval * (1 - .05f * R("mk-quick-draw")); }
        public override float MoveSpeedMultiplier { get { return 1 + .02f * R("pf-fleet"); } }
        public override void OnAutoHit() { MendWolf(); }
        /// <summary>Hunter's Mark: a marked target takes more from everyone in the party.</summary>
        public override float PartyDamageMultiplier(EncounterEnemy enemy) { return Marked(enemy) ? 1 + MarkBonus + .02f * R("pf-keen-eye") : 1; }
        int Shot(float amount) { return Mathf.Max(1, Mathf.RoundToInt(amount * (1 + .03f * R("mk-steady-hand")))); }
        void MendWolf() { int bond = R("bb-mending-bond"); if (bond > 0 && Wolf != null && Wolf.actor.IsAlive) Wolf.actor.GetComponent<Combatant>().Heal(Mathf.RoundToInt(Wolf.actor.Health.Pool.Max * .02f * bond)); }

        // ---------- action bar ----------
        public override int ActionCount { get { return BarIds.Length; } }
        public override AbilityDefinition ActionAt(int slot) { return slot >= 0 && slot < BarIds.Length && abilities.TryGetValue(BarIds[slot], out var a) ? a : null; }
        public override string ActionLockLabel(int slot)
        {
            var a = ActionAt(slot); if (a == null || s.Player == null) return "Unavailable";
            var u = unlocks[a.id];
            if (Level < u.level) return "Level " + u.level;
            if (!string.IsNullOrEmpty(u.talentId) && R(u.talentId) == 0) return "Talent";
            if (a.id == "ranger.sic" && (Wolf == null || !Wolf.actor.IsAlive)) return "No wolf";
            return null;
        }
        public override bool Use(int slot)
        {
            var a = ActionAt(slot); if (a == null) return false;
            switch (a.id)
            {
                case "ranger.quick_shot": return QuickShot(a);
                case "ranger.aimed_shot": return AimedShot(a);
                case "ranger.barbed_arrow": return BarbedArrow(a);
                case "ranger.hunters_mark": return HuntersMark(a);
                case "ranger.snare": return Snare(a);
                case "ranger.call_companion": return CallCompanion(a);
                case "ranger.sic": return Sic(a);
                case "ranger.disengage": return Disengage(a);
                case "ranger.pin": return Pin(a);
            }
            return false;
        }
        static AbilityDefinition With(AbilityDefinition a, int cost, float castTime)
        {
            return new AbilityDefinition { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = cost,
                cooldown = a.cooldown, range = a.range, castTime = castTime, globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId };
        }
        bool Start(AbilityDefinition a, Action effect) { return s.StartAbility(a, () => { if (s.Player.IsAlive) effect(); }) == AbilityStartResult.Started; }
        bool Need(bool ok, string message) { if (!ok) s.Message(message); return ok; }
        void Hit(EncounterEnemy t, int damage) { t.Receive(damage, s.Player); }
        void AfterShot(EncounterEnemy t) { MendWolf(); if (R("bb-pack-sense") > 0 && Wolf != null && Wolf.actor.IsAlive && Wolf.Quarry != t) Wolf.Hunt(t, 1.5f); }   // it turns at once to what you shoot, and bites the harder for it

        // ---------- Marksman ----------
        bool QuickShot(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            int cost = freeQuick ? 0 : Mathf.Max(0, a.cost - 2 * R("mk-quick-draw"));
            return Start(With(a, cost, 0), () => {
                freeQuick = false;
                if (!s.LandsOn(t, a.range)) return;
s.Bolt(t, new Color(.9f, .88f, .8f), .1f, true); s.BeginAutoAttack(); Hit(t, Shot(a.power + s.WeaponDamage * .6f)); AfterShot(t);
            });
        }
        bool AimedShot(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.BeginAutoAttack(); s.Bolt(t, new Color(.9f, .88f, .8f), .1f, true); 
                float d = a.power + s.WeaponDamage;
                float ratio = t.actor.Health.Pool.Ratio;
                if (ratio > .7f) d *= 1 + .08f * R("mk-piercing");
                if (ratio < .35f) d *= 1 + .12f * R("mk-headshot");
                int first = Shot(d); Hit(t, first);
                if (R("mk-double-nock") > 0 && t.actor.IsAlive) Hit(t, Mathf.Max(1, first / 2));
                if (R("mk-snap-shot") > 0) freeQuick = true;
                AfterShot(t);
            });
        }
        bool BarbedArrow(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
s.Bolt(t, new Color(.9f, .88f, .8f), .1f, true); s.BeginAutoAttack(); Hit(t, Shot(a.power + s.WeaponDamage * .3f));
                int ticks = 4 + R("mk-bleeding-wounds");
                periodic.Add("bleed", t, Now, 2, ticks, Shot(3 + Level / 2f), (e, i) => { var enemy = (EncounterEnemy)e.target; if (!enemy.actor.IsAlive) return false; Hit(enemy, e.value); return true; });
                AfterShot(t);
            });
        }

        // ---------- Pathfinder ----------
        bool HuntersMark(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!t.actor.IsAlive) return;
                markedUntil[t] = Now + a.duration;
                if (R("pf-pathfinder") > 0) t.Slow(.15f, a.duration);
                s.Message("Hunter's Mark: " + t.actor.DisplayName + " takes more from the party for " + a.duration + " seconds.");
            });
        }
        bool Snare(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            int tangle = R("pf-tangling-snare");
            return Start(a, () => { if (s.LandsOn(t, a.range)) { s.Bolt(t, new Color(.9f, .88f, .8f), .1f, true); t.Slow(.5f + .05f * tangle, a.duration + tangle); } });
        }
        bool Pin(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
s.Bolt(t, new Color(.9f, .88f, .8f), .1f, true); Hit(t, Shot(a.power + s.WeaponDamage * .3f)); t.Root(a.duration + R("pf-trapper"));
                s.Message("Pin: the target is held where it stands.");
            });
        }
        bool Disengage(AbilityDefinition a)
        {
            return Start(a, () => {
                var player = s.Player.transform;
                var away = s.Target != null ? player.position - s.Target.transform.position : -player.forward; away.y = 0;
                if (away.sqrMagnitude < .01f) away = -player.forward;
                var landing = player.position + away.normalized * DisengageLeap;
                if (UnityEngine.AI.NavMesh.SamplePosition(landing, out var hit, 3, UnityEngine.AI.NavMesh.AllAreas)) landing = hit.position + Vector3.up * 1.05f; else landing.y = player.position.y;
                s.Player.GetComponent<AdventurerMotor>().Teleport(landing);
                int cover = R("pf-cover-of-leaves");
                if (cover > 0) PlayerCombat.AddBarrier(Mathf.RoundToInt(s.Player.Health.Pool.Max * .03f * cover), 6);
            });
        }

        // ---------- Beastbond ----------
        public float WolfHealthScale { get { return (1 + .08f * R("bb-thick-coat")) * (R("bb-alpha") > 0 ? 1.25f : 1); } }
        public float WolfBiteScale { get { return (1 + .1f * R("bb-sharp-teeth")) * (R("bb-alpha") > 0 ? 1.25f : 1); } }
        public bool WolfHowls { get { return R("bb-howl") > 0; } }
        bool CallCompanion(AbilityDefinition a)
        {
            if (Wolf != null && Wolf.actor.IsAlive)
                return Start(With(a, 0, 0), () => { s.DismissPet(); s.Message("Your wolf slips away into the grass."); });
            return Start(a, () => { s.SummonWolf(this); s.Message("Your wolf comes to heel."); });
        }
        bool Sic(AbilityDefinition a)
        {
            if (!Need(Wolf != null && Wolf.actor.IsAlive, "No wolf at heel: Call Companion first.") || !s.RequireEnemyInRange(a.range)) return false;
            var t = s.Target;
            return Start(a, () => { if (t.actor.IsAlive) Wolf.Hunt(t, 1.5f + .25f * R("bb-sic-fury")); });
        }

        // ---------- per frame / lifecycle ----------
        public override void Tick(bool inCombat) { periodic.Tick(Now); }
        public override void ResetState()
        {
            resetting = true; periodic.Clear(); resetting = false;
            markedUntil.Clear(); freeQuick = false;
        }
        public override void ClearTransient()
        {
            resetting = true; periodic.Clear(); resetting = false;
            markedUntil.Clear(); freeQuick = false;
            if (s.Player != null) PlayerCombat.ClearBarrier();
            if (Wolf != null) Wolf.Refit();
        }

        // ---------- HUD ----------
        public override string StatusLine
        {
            get
            {
                var parts = new List<string>();
                parts.Add(Wolf != null && Wolf.actor.IsAlive ? "Wolf " + Wolf.actor.Health.Pool.Current + "/" + Wolf.actor.Health.Pool.Max + (Wolf.Quarry != null ? " hunting" : " at heel") : "No wolf");
                if (freeQuick) parts.Add("free Quick Shot");
                int barrier = PlayerCombat.Barrier; if (barrier > 0) parts.Add("Barrier " + barrier);
                return string.Join("  ·  ", parts);
            }
        }
        public override string TargetStatus(EncounterEnemy enemy)
        {
            var parts = new List<string>();
            if (Marked(enemy)) parts.Add("MARKED");
            if (enemy.Rooted) parts.Add("PINNED " + enemy.RootRemaining.ToString("0.0") + "s");
            else if (enemy.Slowed) parts.Add("SNARED");
            if (periodic.Has("bleed", enemy)) parts.Add("Bleeding");
            return parts.Count == 0 ? null : string.Join("  ·  ", parts);
        }
    }
}
