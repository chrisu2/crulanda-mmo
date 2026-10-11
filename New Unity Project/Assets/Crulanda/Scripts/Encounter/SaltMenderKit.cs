using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.Abilities;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Salt-Mender (Phase 9.2, 2026-10-11; CANON roots, GAME-ONLY kit, PROVISIONAL names; CLASS_BUILD_MATRIX.md "Phase 9.2 and 9.3 from
    /// the canon"): the salt-magic of the West. Book 1's Salt-Speaker throws salt and chants to bind the Chaos and seal the Wound,
    /// calcifying Weave-Eaters into white crystal (B1F:5486-5498); book 3's Salt-Menders sing a wall into place, raise a Salt-Wall that takes
    /// the enemy's force and grounds it, and loose salt-crystal heads that dissolve the Void's things; Klyther lays the salt-stone. Not a
    /// healer: no salt-magic heals in the books. On Salt, a pouch of 100 spent by every working and refilled from the earth out of a fight.
    /// Bar: 1 Salt Shard, 2 Binding Salt, 3 Salt-Ward, 4 Seal the Wound, 5 Draw the Line, 6 Sung Wall, 7 Song of Sifting, 8 Calcify
    /// (talent). Three paths, rows 0-2 (Talents/saltmender.json): the Salt-Speaker (control), the Mason (tank) and the Sifter (ranged).
    /// The Wasting's things (Weave-Eaters, the Hollow Men) take more from salt. Nothing here is saved.
    /// </summary>
    public sealed class SaltMenderKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "ss-long-binding", "ss-quick-chant", "ss-first-sea", "ss-calcify", "ss-white-crust", "ss-deep-crystal", "ss-sealed",
            "ms-thick-salt", "ms-grounding", "ms-mortar", "ms-line-held", "ms-spiked-wall", "ms-wide-wall", "ms-mason-of-the-weave",
            "sf-sharp-salt", "sf-sifting-song", "sf-bane", "sf-light-hand", "sf-brine", "sf-crystal-rain", "sf-split-shard" };
        public static readonly string[] BarIds = { "salt.shard", "salt.binding", "salt.ward", "salt.seal", "salt.line", "salt.sung_wall", "salt.sifting", "salt.calcify" };
        public const float BaneBonus = .25f, WallReach = 8, WallCut = .15f, SiftReach = 4, BindSeconds = 8, SealSilence = 3, CalcifyStun = 3, WardGround = 3;

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        readonly PeriodicEffects periodic = new PeriodicEffects();
        readonly Dictionary<EncounterEnemy, float> heldUntil = new Dictionary<EncounterEnemy, float>();
        float wardUntil, wallUntil;

        public SaltMenderKit(EncounterSession session, ClassDefinition definition, IEnumerable<AbilityDefinition> catalog) : base(session)
        {
            if (definition == null || definition.unlocks == null || catalog == null) throw new ArgumentException("Salt-Mender content missing.");
            foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !abilities.ContainsKey(a.id)) abilities.Add(a.id, a);
            foreach (var u in definition.unlocks)
                if (u == null || string.IsNullOrEmpty(u.abilityId) || u.level < 1 || u.level > 10 || unlocks.ContainsKey(u.abilityId))
                    throw new ArgumentException("Invalid or duplicate Salt-Mender unlock.");
                else unlocks.Add(u.abilityId, u);
            foreach (var id in BarIds)
                if (!abilities.ContainsKey(id) || !unlocks.ContainsKey(id)) throw new ArgumentException("Salt-Mender ability '" + id + "' is missing from content or unlocks.");
        }
        public override string[] ImplementedTalents { get { return ImplementedIds; } }
        int R(string id) { return s.TalentRank(id); }
        float Now { get { return Time.time; } }
        int Level { get { return s.Player.Level; } }
        Combatant PlayerCombat { get { return s.Player.GetComponent<Combatant>(); } }
        public PeriodicEffects Periodic { get { return periodic; } }
        public bool Warded { get { return Now < wardUntil && PlayerCombat.Barrier > 0; } }
        public bool WallSung { get { return Now < wallUntil; } }
        /// <summary>Held by your salt: bound (Binding Salt), sealed (Seal the Wound) or calcified.</summary>
        public bool Holds(EncounterEnemy e) { return e != null && heldUntil.TryGetValue(e, out var t) && Now < t; }
        void Hold(EncounterEnemy e, float seconds) { float until = Now + seconds; if (!heldUntil.TryGetValue(e, out var h) || h < until) heldUntil[e] = until; }
        /// <summary>The Wasting's things, on which salt bites deepest (the Weave-Eaters calcified in Book 1; the Hollow Men).</summary>
        public static bool Unmade(EncounterEnemy e)
        {
            if (e == null) return false;
            var look = e.GetComponent<ActorVisual>(); if (look != null && (look.Look == ActorLook.WeaveEater || look.Look == ActorLook.Hollow)) return true;
            var name = e.MobName ?? ""; return name.Contains("Weave-Eater") || name.Contains("Hollow");
        }
        float Bane(EncounterEnemy e) { return Unmade(e) ? 1 + BaneBonus + .08f * R("sf-bane") : 1; }
        int Dmg(float amount) { return Mathf.Max(1, Mathf.RoundToInt(amount)); }
        void Hit(EncounterEnemy t, int damage) { t.Receive(damage, s.Player); }
        bool Held(EncounterEnemy t, string why) { if (why == null) return true; s.Message(t.actor.DisplayName + ": " + why + "."); return false; }
        bool Need(bool ok, string message) { if (!ok) s.Message(message); return ok; }
        bool Start(AbilityDefinition a, Action effect) { return s.StartAbility(a, () => { if (s.Player.IsAlive) effect(); }) == AbilityStartResult.Started; }
        static AbilityDefinition With(AbilityDefinition a, int cost, float cooldown)
        {
            return new AbilityDefinition { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = Mathf.Max(0, cost),
                cooldown = Mathf.Max(a.cooldown > 0 ? 1 : 0, cooldown), range = a.range, castTime = a.castTime, globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId };
        }
        IEnumerable<Actor> Party()
        {
            yield return s.Player;
            if (s.Progress.recruited && s.Companion != null) yield return s.Companion.actor;
            foreach (var c in s.PartySims) if (c != null && c.actor != null) yield return c.actor;
        }
        static readonly Color SaltWhite = new Color(.95f, .95f, .9f);

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
                case "salt.shard": return SaltShard(a);
                case "salt.binding": return BindingSalt(a);
                case "salt.ward": return SaltWard(a);
                case "salt.seal": return SealTheWound(a);
                case "salt.line": return DrawTheLine(a);
                case "salt.sung_wall": return SungWall(a);
                case "salt.sifting": return SongOfSifting(a);
                case "salt.calcify": return Calcify(a);
            }
            return false;
        }

        // ---------- the Sifter ----------
        /// <summary>Salt Shard: a shard of salt crystal thrown at up to 25 m (Sharp Salt: harder; Brine: the target slowed; Split Shard: a
        /// second at half on a foe near it). Harder on the Wasting's things.</summary>
        bool SaltShard(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cost - 2 * R("sf-light-hand"), a.cooldown), () => {
                if (!s.LandsOn(t, a.range)) return;
                s.Bolt(t, SaltWhite, .14f);
                int dealt = Dmg((a.power + 2 * Level) * (1 + .03f * R("sf-sharp-salt")) * Bane(t)); Hit(t, dealt);
                if (R("sf-brine") > 0 && t.actor.IsAlive) t.Slow(.1f * R("sf-brine"), 3);
                if (R("sf-split-shard") > 0)
                    foreach (var o in s.Enemies.ToArray())
                        if (o != null && o != t && o.actor.IsAlive && !o.Game && !o.Hidden && Vector3.Distance(o.transform.position, t.transform.position) <= 5) { Hit(o, Mathf.Max(1, dealt / 2)); break; }
            });
        }
        /// <summary>Song of Sifting: salt sung down over the target's ground: every foe within 4 m of where it stood (Crystal Rain: wider)
        /// burns each second for 5 s (Sifting Song: longer).</summary>
        bool SongOfSifting(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                var centre = t.transform.position; float reach = SiftReach + R("sf-crystal-rain"); int ticks = Mathf.RoundToInt(a.duration) + R("sf-sifting-song");
                s.FloatText(centre, "Song of Sifting", SaltWhite);
                periodic.Add("sifting", t, Now, 1, ticks, Dmg(a.power + Level), (e, i) => {
                    foreach (var o in s.Enemies.ToArray())
                        if (o != null && o.actor.IsAlive && !o.Game && !o.Hidden && Vector3.Distance(o.transform.position, centre) <= reach) Hit(o, Dmg(e.value * Bane(o)));
                    return true;
                });
            });
        }

        // ---------- the Salt-Speaker ----------
        /// <summary>Binding Salt: a handful thrown with the chant: the target held in white crystal for 8 s (Long Binding: longer; it breaks
        /// when struck, as a hold does).</summary>
        bool BindingSalt(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cost - 5 * R("ss-first-sea"), a.cooldown), () => {
                if (!s.LandsOn(t, a.range)) return;
                s.Bolt(t, SaltWhite, .2f);
                float seconds = BindSeconds + R("ss-long-binding");
                if (Held(t, t.Apply("incap", s.Player, seconds))) { Hold(t, seconds); s.Message("\"I bind the Chaos!\" " + t.actor.DisplayName + " is held in white crystal."); }
            });
        }
        /// <summary>Seal the Wound: a sharp handful that silences for 3 s (Sealed: twice as long), the interrupt (Quick Chant: sooner back).</summary>
        bool SealTheWound(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cost, a.cooldown - 2 * R("ss-quick-chant")), () => {
                if (!s.LandsOn(t, a.range)) return;
                Hit(t, Dmg((a.power + Level) * Bane(t)));
                float seconds = SealSilence * (R("ss-sealed") > 0 ? 2 : 1);
                if (Held(t, t.Apply("silence", s.Player, seconds))) Hold(t, seconds);
            });
        }
        /// <summary>Calcify (a talent): the target crusted into salt and stunned 3 s (Deep Crystal: longer); the Wasting's things 2 s more.</summary>
        bool Calcify(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.Bolt(t, SaltWhite, .22f); Hit(t, Dmg(a.power + Level));
                float seconds = CalcifyStun + .5f * R("ss-deep-crystal") + (Unmade(t) ? 2 : 0);
                if (Held(t, t.Apply("stun", s.Player, seconds))) Hold(t, seconds);
            });
        }

        // ---------- the Mason ----------
        /// <summary>Salt-Ward: a ward of salt-stone on you (Thick Salt: stronger) for 10 s; each blow it takes is grounded into Salt (Grounding:
        /// more), and with Spiked Wall bites back; Mason of the Weave wards the party near you at half.</summary>
        bool SaltWard(AbilityDefinition a)
        {
            return Start(a, () => {
                int worth = Dmg((a.power + 4 * Level) * (1 + .08f * R("ms-thick-salt")));
                PlayerCombat.AddBarrier(worth, a.duration); wardUntil = Now + a.duration;
                if (R("ms-mason-of-the-weave") > 0)
                    foreach (var who in Party())
                        if (who != null && who != s.Player && who.IsAlive && Vector3.Distance(who.transform.position, s.Player.transform.position) <= WallReach) who.GetComponent<Combatant>().AddBarrier(Mathf.Max(1, worth / 2), a.duration);
                s.FloatText(s.Player.transform.position, "Salt-Ward", SaltWhite);
            });
        }
        /// <summary>Draw the Line: a line of salt across the ground: the target must come for you (a taunt; Line Held: longer, and more hate).</summary>
        bool DrawTheLine(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!t.actor.IsAlive) return;
                s.BeginAutoAttack();
                t.threat.Taunt(s.Player.EntityId.Value, Now, a.duration + R("ms-line-held")); t.threat.Add(s.Player.EntityId.Value, 30 + 20 * R("ms-line-held"));
                s.FloatText(t.transform.position, "Drawn to you", SaltWhite);
            });
        }
        /// <summary>Sung Wall: a wall of salt sung up around you: the party within 8 m (Wide Wall: further, and stronger) takes 15% less for 8 s.</summary>
        bool SungWall(AbilityDefinition a)
        {
            return Start(a, () => {
                float reach = WallReach + 2 * R("ms-wide-wall"), cut = WallCut + .03f * R("ms-wide-wall"); int n = 0;
                foreach (var who in Party())
                {
                    if (who == null || !who.IsAlive || Vector3.Distance(who.transform.position, s.Player.transform.position) > reach) continue;
                    who.GetComponent<Combatant>().Apply(new StatusEffectDefinition { id = "status.sung_wall", name = "Behind the Sung Wall", duration = a.duration, incomingDamageMultiplier = 1 - cut });
                    n++;
                }
                wallUntil = Now + a.duration;
                s.FloatText(s.Player.transform.position, "Sung Wall", SaltWhite);
                s.Message("Sung Wall: " + n + (n == 1 ? " of you stands" : " of you stand") + " behind the salt, taking " + Mathf.RoundToInt(cut * 100) + "% less for " + a.duration + " seconds.");
            });
        }

        // ---------- incoming blows, the party's blows ----------
        public override int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        {
            if (victim == null || !victim.IsAlive) return 0;
            float mult = victim == s.Player ? 1 - .02f * R("ms-mortar") : 1;   // Mortar
            bool warded = victim == s.Player && Warded;
            int dealt = victim.GetComponent<Combatant>().Damage(Mathf.RoundToInt(raw * mult));
            if (warded)
            {
                var pool = s.Player.Resource.Pool; pool.SetCurrent(Mathf.Min(pool.Max, pool.Current + Mathf.RoundToInt(WardGround + R("ms-grounding"))));   // grounded into Salt
                if (R("ms-spiked-wall") > 0 && enemy != null && enemy.actor.IsAlive) enemy.Receive(3 + 2 * R("ms-spiked-wall"), s.Player);   // Spiked Wall
            }
            return dealt;
        }
        /// <summary>White Crust: a foe your salt holds takes more from the whole party.</summary>
        public override float PartyDamageMultiplier(EncounterEnemy enemy) { return Holds(enemy) ? 1 + .03f * R("ss-white-crust") : 1; }

        // ---------- per frame / lifecycle ----------
        public override void Tick(bool inCombat) { periodic.Tick(Now); }
        public override void ResetState() { periodic.Clear(); heldUntil.Clear(); wardUntil = wallUntil = 0; }
        public override void ClearTransient() { ResetState(); if (s.Player != null) PlayerCombat.ClearBarrier(); }

        // ---------- HUD ----------
        public override string StatusLine
        {
            get
            {
                var parts = new List<string>();
                int barrier = PlayerCombat.Barrier; if (barrier > 0) parts.Add((Warded ? "Salt-Ward " : "Barrier ") + barrier);
                if (WallSung) parts.Add("Sung Wall " + (wallUntil - Now).ToString("0") + "s");
                return parts.Count == 0 ? null : string.Join("  ·  ", parts);
            }
        }
        public override string TargetStatus(EncounterEnemy enemy)
        {
            if (enemy == null) return null;
            var parts = new List<string>();
            if (enemy.ControlLabel != null) parts.Add(enemy.ControlLabel);
            if (periodic.Has("sifting", enemy)) parts.Add("Sifted");
            if (Unmade(enemy)) parts.Add("Of the Wasting");
            return parts.Count == 0 ? null : string.Join("  ·  ", parts);
        }
    }
}
