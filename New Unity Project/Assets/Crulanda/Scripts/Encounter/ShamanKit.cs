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
    /// The Shaman (note 178; Chris 23:00 "Shaman is orc-only": CANON-EXPANDED, orcs are canon (Gorm, Krunk), orc shamanism is not, signed
    /// off by Chris; PROVISIONAL names): an orc weather-speaker who calls on rain, stone and the old dead. Standing stones raised from the
    /// ground, not built (no machines). Breath, a well of 100, pays for every call.
    /// Bar: 1 Squall, 2 Tide Mend, 3 Rain Call, 4 Chain Mend, 5 Thunderhead, 6 Hailfall, 7 Earthskin, 8 Rooting Stamp, 9 Cairn,
    /// 10 Moor Wind, 11 Mist, 12 Barrow-call.
    /// Four paths: Rainspeaker (healer), Stormcaller (ranged dps), Stonebound (tank), Gravewind (control).
    /// </summary>
    public sealed class ShamanKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "rs-deep-rain", "rs-long-rain", "rs-tide", "rs-chain", "rs-springs", "rs-ancestor",
            "sc-gale", "sc-charge", "sc-hail", "sc-quick-squall", "sc-eye", "sc-thunderclap",
            "sb-earthskin", "sb-stamp", "sb-cairn", "sb-rooted", "sb-stone-blood", "sb-mountain",
            "gw-moor-wind", "gw-mist", "gw-barrow", "gw-cold", "gw-long-dark", "gw-wailing" };
        public static readonly string[] BarIds = { "shaman.squall", "shaman.tide_mend", "shaman.rain_call", "shaman.chain_mend", "shaman.thunderhead", "shaman.hailfall",
            "shaman.earthskin", "shaman.rooting_stamp", "shaman.cairn", "shaman.moor_wind", "shaman.mist", "shaman.barrow_call" };
        public const float MendReach = 30, RainRadius = 8, ChainReach = 12, HailRadius = 6, StampRadius = 8, WindRadius = 8, EarthskinCut = .1f;

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        readonly PeriodicEffects periodic = new PeriodicEffects();
        int charge; float earthskinUntil; bool ancestorSpent, mountainSpent;

        public ShamanKit(EncounterSession session, ClassDefinition definition, IEnumerable<AbilityDefinition> catalog) : base(session)
        {
            if (definition == null || definition.unlocks == null || catalog == null) throw new ArgumentException("Shaman content missing.");
            foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !abilities.ContainsKey(a.id)) abilities.Add(a.id, a);
            foreach (var u in definition.unlocks)
                if (u == null || string.IsNullOrEmpty(u.abilityId) || u.level < 1 || u.level > 10 || unlocks.ContainsKey(u.abilityId)) throw new ArgumentException("Invalid or duplicate Shaman unlock.");
                else unlocks.Add(u.abilityId, u);
            foreach (var id in BarIds) if (!abilities.ContainsKey(id) || !unlocks.ContainsKey(id)) throw new ArgumentException("Shaman ability '" + id + "' is missing from content or unlocks.");
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
        IEnumerable<EncounterEnemy> FoesNear(Vector3 at, float r) { return s.Enemies.Where(e => e != null && e.actor.IsAlive && !e.Game && Vector3.Distance(e.transform.position, at) <= r).ToList(); }

        // ---------- the party ----------
        public IEnumerable<Actor> Party()
        {
            yield return s.Player;
            if (s.Progress.recruited && s.Companion != null && s.Companion.gameObject.activeInHierarchy) yield return s.Companion.actor;
            foreach (var c in s.PartySims) if (c != null) yield return c.actor;
        }
        public Actor MostHurt(float reach, Actor not = null)
        {
            Actor best = null; float low = 2;
            foreach (var who in Party())
                if (who != null && who != not && who.IsAlive && Vector3.Distance(who.transform.position, s.Player.transform.position) <= reach && who.Health.Pool.Ratio < low) { low = who.Health.Pool.Ratio; best = who; }
            return best;
        }
        int Mend(Actor who, int amount)
        {
            if (who == null || !who.IsAlive || amount <= 0) return 0;
            int healed = who.GetComponent<Combatant>().Heal(Mathf.RoundToInt(amount * s.PlayerHealScale), s.Player);
            if (healed > 0) { HealFx.Show(who.transform); s.AddHealingThreat(healed); s.FloatText(who.transform.position, "+" + healed, new Color(.6f, .85f, 1)); }
            return healed;
        }
        void Breath(int n) { var p = s.Player.Resource.Pool; p.SetCurrent(Mathf.Min(p.Max, p.Current + n)); }

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
                case "shaman.squall": return Squall(a);
                case "shaman.tide_mend": return TideMend(a);
                case "shaman.rain_call": return RainCall(a);
                case "shaman.chain_mend": return ChainMend(a);
                case "shaman.thunderhead": return Thunderhead(a);
                case "shaman.hailfall": return Hailfall(a);
                case "shaman.earthskin": return Earthskin(a);
                case "shaman.rooting_stamp": return RootingStamp(a);
                case "shaman.cairn": return Cairn(a);
                case "shaman.moor_wind": return MoorWind(a);
                case "shaman.mist": return Mist(a);
                case "shaman.barrow_call": return BarrowCall(a);
            }
            return false;
        }

        // ---------- Rainspeaker ----------
        bool TideMend(AbilityDefinition a)
        {
            var who = MostHurt(MendReach); if (!Need(who != null, "No one near enough to mend.")) return false;
            return Start(a, () => { Mend(MostHurt(MendReach) ?? who, Dmg((a.power + 3 * Level) * (1 + .04f * R("rs-tide")))); if (R("rs-springs") > 0) Breath(2 * R("rs-springs")); });
        }
        bool RainCall(AbilityDefinition a)
        {
            return Start(a, () => {
                var at = s.Player.transform.position; int ticks = Mathf.RoundToInt(Mathf.Max(3, a.duration / 2)) + R("rs-long-rain");
                periodic.Add("rain", s.Player, Now, 2, ticks, Dmg((a.power + Level) * (1 + .05f * R("rs-deep-rain"))), (e, i) => {
                    foreach (var who in Party().ToList()) if (who != null && Vector3.Distance(who.transform.position, at) <= RainRadius) Mend(who, e.value); return true; });
            });
        }
        bool ChainMend(AbilityDefinition a)
        {
            var first = MostHurt(MendReach); if (!Need(first != null, "No one near enough to mend.")) return false;
            return Start(a, () => {
                float amount = (a.power + 2 * Level) * (1 + .04f * R("rs-tide")); var hit = new HashSet<Actor>(); var at = first;
                for (int j = 0; j < 3 + R("rs-chain") && at != null; j++)
                {
                    hit.Add(at); Mend(at, Dmg(amount)); amount *= .8f;
                    var from = at; at = Party().Where(x => x != null && x.IsAlive && !hit.Contains(x) && Vector3.Distance(x.transform.position, from.transform.position) <= ChainReach).OrderBy(x => x.Health.Pool.Ratio).FirstOrDefault();
                }
            });
        }

        // ---------- Stormcaller ----------
        bool Squall(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            var use = With(a, a.cost, Mathf.Max(0, a.castTime - .15f * R("sc-quick-squall")));
            return Start(use, () => { if (!s.LandsOn(t, a.range)) return; s.Bolt(t, new Color(.7f, .8f, 1), .15f); t.Receive(Dmg((a.power + 2 * Level) * (1 + .04f * R("sc-gale"))), s.Player); charge = Mathf.Min(5, charge + 1); });
        }
        bool Thunderhead(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            if (!Need(charge > 0, "Thunderhead needs a Squall's charge.")) return false;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return; int k = charge; charge = 0;
                float d = (a.power + 2 * Level) * k * (1 + .1f * R("sc-charge")); if (R("sc-eye") > 0 && t.actor.Health.Pool.Ratio < .3f) d *= 1.25f;
                s.Bolt(t, new Color(.85f, .9f, 1), .3f); t.Receive(Dmg(d), s.Player);
                if (R("sc-thunderclap") > 0) foreach (var e in FoesNear(t.transform.position, 5)) if (e != t) e.Receive(Dmg(d * .3f), s.Player);
            });
        }
        bool Hailfall(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var at = s.Target.transform.position;
            return Start(a, () => {
                periodic.Add("hail", s.Player, Now, 1, Mathf.RoundToInt(Mathf.Max(3, a.duration)), Dmg((a.power + Level) * (1 + .05f * R("sc-hail"))), (e, i) => {
                    foreach (var f in FoesNear(at, HailRadius)) f.Receive(e.value, s.Player); return true; });
            });
        }

        // ---------- Stonebound ----------
        bool Earthskin(AbilityDefinition a) { return Start(a, () => { earthskinUntil = Now + Mathf.Max(10, a.duration); PlayerCombat.AddBarrier(Dmg((a.power + 2 * Level) * (1 + .05f * R("sb-earthskin"))), 10); }); }
        bool RootingStamp(AbilityDefinition a)
        {
            return Start(a, () => {
                foreach (var e in FoesNear(s.Player.transform.position, StampRadius))
                { e.Receive(Dmg((a.power + Level) * (1 + .04f * R("sb-stamp")) * 1.5f), s.Player); e.Slow(.3f + .05f * R("sb-rooted"), 4); }   // the heavy blow draws them to you
            });
        }
        bool Cairn(AbilityDefinition a)
        {
            var who = MostHurt(MendReach) ?? s.Player;
            return Start(a, () => who.GetComponent<Combatant>().AddBarrier(Dmg((a.power + 4 * Level) * (1 + .08f * R("sb-cairn"))), 12));
        }

        // ---------- Gravewind ----------
        bool MoorWind(AbilityDefinition a)
        {
            return Start(a, () => {
                foreach (var e in FoesNear(s.Player.transform.position, WindRadius)) { e.Receive(Dmg(a.power + Level), s.Player); e.Slow(.4f + .05f * R("gw-moor-wind") + .05f * R("gw-cold"), 5); }
            });
        }
        bool Mist(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => { foreach (var e in FoesNear(t.transform.position, 4 + R("gw-mist"))) e.Root(3 + .5f * R("gw-long-dark")); });
        }
        bool BarrowCall(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return; t.Root(5 + R("gw-barrow") + .5f * R("gw-long-dark"));
                if (R("gw-wailing") > 0) periodic.Add("wail", t, Now, 2, 3, Dmg(a.power + Level), (e, i) => { var en = (EncounterEnemy)e.target; if (!en.actor.IsAlive) return false; en.Receive(e.value, s.Player); return true; });
            });
        }

        // ---------- incoming blows ----------
        public override int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        {
            if (victim == null || !victim.IsAlive) return 0;
            float mult = victim == s.Player && Now < earthskinUntil ? 1 - EarthskinCut - .02f * R("sb-stone-blood") : 1;
            int amount = Mathf.RoundToInt(raw * mult);
            if (R("rs-ancestor") > 0 && !ancestorSpent && amount >= victim.Health.Pool.Current) { ancestorSpent = true; amount = Mathf.Max(0, victim.Health.Pool.Current - 1); }   // the ancestors hold them
            int dealt = victim.GetComponent<Combatant>().Damage(amount);
            if (R("sb-mountain") > 0 && !mountainSpent && victim == s.Player && victim.Health.Pool.Ratio < .3f) { mountainSpent = true; PlayerCombat.AddBarrier(Mathf.RoundToInt(victim.Health.Pool.Max * .3f), 8); }
            return dealt;
        }
        public override bool MeleeAutoAttacks { get { return false; } }

        // ---------- per frame / lifecycle ----------
        public override void Tick(bool inCombat) { periodic.Tick(Now); if (!inCombat) { ancestorSpent = mountainSpent = false; charge = 0; } }
        public override void ResetState() { periodic.Clear(); charge = 0; earthskinUntil = 0; ancestorSpent = mountainSpent = false; }
        public override void ClearTransient() { ResetState(); if (s.Player != null) PlayerCombat.ClearBarrier(); }
        public override string StatusLine
        {
            get
            {
                var parts = new List<string>();
                if (charge > 0) parts.Add("Charge " + charge);
                if (Now < earthskinUntil) parts.Add("Earthskin " + (earthskinUntil - Now).ToString("0") + "s");
                return parts.Count == 0 ? null : string.Join("  ", parts);
            }
        }
    }
}
