using Crulanda.Combat;
using Crulanda.Core;
using Crulanda.Gameplay;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Lowtowns' small rules against the Weave-Eaters (Docs/ZONE_LOWTOWNS.md section 4; Phase 9.1). The lore is CANON (book1 ch.1:251,
    /// ch.20:111-203; B1WB:121): Weave-Eaters are shadowy spirits of the drains; thrown salt calcifies them; the bone-harpooners pin them on
    /// amber rope; steel that passes through one comes out black and brittle. The numbers are GAME-ONLY:
    /// - salt (an item with thrown "salt"): thrown at the target within <see cref="ThrowRange"/>, it stings and, on a Weave-Eater, calcifies
    ///   it (a stun of <see cref="SaltSeconds"/>; an elite once a fight, as every hold);
    /// - the bone harpoon (thrown "harpoon"): a heavier throw that pins a spirit where it stands (a root of <see cref="HarpoonSeconds"/>);
    /// - blackened steel: whoever strikes a Weave-Eater from within <see cref="MeleeReach"/> (a blade, a fist, a claw) has their strikes
    ///   blunted, -<see cref="BlackenShare"/> attack power for <see cref="BlackenSeconds"/>, refreshed by every such blow (never stacked);
    /// - night camps (ZoneCamp.night): the Drain Courts are empty by day (EncounterEnemy.Night, EncounterSession.Lowtowns).
    /// Thrown at anything else, salt and a harpoon do their sting and nothing more. Pure rules here; the session applies them.
    /// The rework (Docs/REWORK_LOWTOWNS_HALLOWS.md, Lowtowns pick 4, 2026-10-18; Chris: "keep all"): the Weave-Eaters fight as chapter 20 shows
    /// them (CANON behaviour, GAME-ONLY numbers; EncounterEnemy.WeaveEater applies these):
    /// - the blink: they move "with sickening speed, a sudden, jerking displacement of space" (b1 ch.20:131): a short warp of up to
    ///   <see cref="BlinkReach"/> m toward whoever they fight, every <see cref="BlinkEvery"/> s while that one is more than <see cref="BlinkMin"/> m off;
    /// - the lighthouse: a mage's aura "acted as a lighthouse in a storm, drawing them from miles away" (ch.20:121): every
    ///   <see cref="LighthouseBeat"/> s whoever of the party works magic (a mana or salt pool) or wears a ward within <see cref="LighthouseReach"/> m
    ///   gains <see cref="LighthouseShare"/> of the top threat on it, so it turns on the casters and healers unless the tank keeps ahead;
    /// - the ward-eating: "They are eating the spell as fast as I cast it" (ch.20:143): a ward (a barrier) on whoever it fights within reach is
    ///   eaten, <see cref="WardEatShare"/> of what is left (at least <see cref="WardEatFloor"/>) every second;
    /// - the harpoon's anchor: the harpoons pinned them, "preventing them from displacing" (ch.20:191): a harpooned Weave-Eater does not blink
    ///   for <see cref="PinSeconds"/> s (longer than the root itself);
    /// - salt: "The Weave-Eaters began to calcify ... solid, jagged statues of white crystal" (ch.20:205): the salt's stun, as before.
    /// Only in the zones of <see cref="BookZones"/> (the Lowtowns); the Weave-Eaters elsewhere keep the fights Chris settled.
    /// </summary>
    public static class WeaveEaterRules
    {
        public const float SaltSeconds = 4, HarpoonSeconds = 5, BlackenSeconds = 20, BlackenShare = .1f, ThrowRange = 15, MeleeReach = 4.5f;
        public const string Salt = "salt", Harpoon = "harpoon", Snare = "snare";
        /// <summary>The blink (ch.20:131): its reach, how often, and the gap under which it walks instead.</summary>
        public const float BlinkReach = 7, BlinkEvery = 4.5f, BlinkMin = 5;
        /// <summary>The lighthouse (ch.20:121): how often, how far, and what share of the top threat a magic-worker gains each beat.</summary>
        public const float LighthouseBeat = 2, LighthouseReach = 25, LighthouseShare = .12f;
        /// <summary>The ward-eating (ch.20:143): the share of a ward eaten each second, and the least a bite takes.</summary>
        public const float WardEatShare = .35f; public const int WardEatFloor = 5;
        /// <summary>The harpoon's anchor (ch.20:191): seconds a harpooned Weave-Eater cannot blink.</summary>
        public const float PinSeconds = 12;
        /// <summary>The zones whose Weave-Eaters fight as the book shows them (blink, lighthouse, ward-eating).</summary>
        public static readonly string[] BookZones = { "zone.lowtowns" };
        public const string WardEatenLine = "The Weave-Eater eats the ward as fast as it was laid.";
        public static bool BookZone(string zoneId) { return System.Array.IndexOf(BookZones, zoneId) >= 0; }
        /// <summary>Whether a Weave-Eater blinks toward its foe now: off its pin, not held, the foe <paramref name="gap"/> m off (more than
        /// <see cref="BlinkMin"/>), and its blink ready.</summary>
        public static bool Blinks(float gap, bool rooted, bool pinned, float now, float readyAt) { return !rooted && !pinned && gap > BlinkMin && now >= readyAt; }
        /// <summary>How far a blink goes toward a foe <paramref name="gap"/> m off: up to <see cref="BlinkReach"/>, stopping short of it.</summary>
        public static float BlinkStep(float gap) { return BlinkStep(gap, BlinkReach); }
        /// <summary>A blink of another reach (the Unraveller's 12 m, UnravellerTuning).</summary>
        public static float BlinkStep(float gap, float reach) { return Mathf.Clamp(gap - 1.8f, 0, reach); }
        /// <summary>Whether one of the party draws a Weave-Eater (the lighthouse): a pool of magic (mana, the Salt-Mender's salt) or a ward on.</summary>
        public static bool DrawnTo(ResourceKind pool, int barrier) { return pool == ResourceKind.Mana || pool == ResourceKind.Salt || barrier > 0; }
        /// <summary>The threat the lighthouse adds to a magic-worker each beat: a share of the top threat on the Weave-Eater (at least 1).</summary>
        public static float LighthouseThreat(float top) { return LighthouseThreat(top, LighthouseShare); }
        /// <summary>The lighthouse's threat at another <paramref name="share"/> (the Unraveller's stronger draw, UnravellerTuning).</summary>
        public static float LighthouseThreat(float top, float share) { return Mathf.Max(1, top * share); }
        /// <summary>How much of a ward one second's eating takes (all of it when little is left).</summary>
        public static int WardBite(int barrier) { return barrier <= 0 ? 0 : Mathf.Min(barrier, Mathf.Max(WardEatFloor, Mathf.CeilToInt(barrier * WardEatShare))); }
        public const string BlackenedId = "debuff.blackened_steel", BlackenedName = "Blackened steel";
        public const string BlackenedLine = "Your steel comes out of the Weave-Eater black and brittle. Your blows land softer for a while.";
        public const string NoTargetLine = "Throw it at what? Pick out a foe first.";
        /// <summary>A thrown item's kind the game knows (salt or a harpoon), or null.</summary>
        public static string ThrownKind(ItemDef d) { return d == null || d.kind != "consumable" ? null : d.thrown == Salt || d.thrown == Harpoon || d.thrown == Snare ? d.thrown : null; }
        /// <summary>Whether a mob of this name and look is a Weave-Eater (the Lantern-Eater and the Mother of Drains among them).</summary>
        public static bool IsWeaveEater(string mobName, bool weaveEaterLook)
        {
            if (weaveEaterLook) return true;
            var n = mobName ?? ""; return n.Contains("Weave-Eater") || n.Contains("Lantern-Eater") || n.Contains("Mother of Drains");
        }
        public static bool IsWeaveEater(EncounterEnemy e)
        {
            if (e == null) return false;
            var look = e.GetComponent<ActorVisual>();
            return IsWeaveEater(e.MobName, look != null && look.Look == ActorLook.WeaveEater);
        }
        /// <summary>The blunting a Weave-Eater leaves on the steel that strikes it: -10% attack power for 20 s.</summary>
        public static StatusEffectDefinition Blackened() { return Blackened(1); }
        /// <summary>Blackened steel <paramref name="stacks"/> deep (the Unraveller's three, WORLD_BOSS_1.md 3.3): -10% attack power a stack.</summary>
        public static StatusEffectDefinition Blackened(int stacks) { return Blackened(stacks, BlackenShare); }
        /// <summary>Blackened steel <paramref name="stacks"/> deep at <paramref name="share"/> a stack (the Unraveller's 5%: UnravellerTuning.steelShare).</summary>
        public static StatusEffectDefinition Blackened(int stacks, float share)
        {
            stacks = Mathf.Max(1, stacks);
            return new StatusEffectDefinition { id = BlackenedId, name = stacks > 1 ? BlackenedName + " (" + stacks + ")" : BlackenedName, duration = BlackenSeconds, incomingDamageMultiplier = 1,
                modifiers = new[] { new StatusModifierDefinition { stat = StatType.AttackPower, operation = ModifierOp.PercentAdd, value = -share * stacks } } };
        }
        /// <summary>What a melee blow is multiplied by under blackened steel (1 with none): its attack power lost. A sim's blows read it here
        /// (they do not use attack power), so steel goes black on a sim's blade as on yours.</summary>
        public static float SteelScale(StatusEffectRuntime statuses, float now)
        {
            if (statuses == null || statuses.Remaining(BlackenedId, now) <= 0) return 1;
            var d = statuses.DefinitionOf(BlackenedId); if (d == null || d.modifiers == null || d.modifiers.Length == 0) return 1;
            return Mathf.Clamp01(1 + d.modifiers[0].value);
        }
        /// <summary>How deep the blackened steel on a striker is now (0: none): read off the status's own modifier.</summary>
        public static int SteelStacks(StatusEffectRuntime statuses, float now)
        {
            if (statuses == null || statuses.Remaining(BlackenedId, now) <= 0) return 0;
            var d = statuses.DefinitionOf(BlackenedId); if (d == null || string.IsNullOrEmpty(d.name)) return 1;
            int open = d.name.LastIndexOf('('), close = d.name.LastIndexOf(')');   // "Blackened steel (3)"
            return open >= 0 && close > open && int.TryParse(d.name.Substring(open + 1, close - open - 1), out int n) ? Mathf.Max(1, n) : 1;
        }
        /// <summary>The buff row's name for a status of these rules, or null.</summary>
        public static string StatusName(string id) { return id == BlackenedId ? BlackenedName : null; }
        /// <summary>Whether a blow from <paramref name="by"/> at <paramref name="distance"/> blackens the striker's steel: a Weave-Eater struck
        /// from within reach.</summary>
        public static bool Blackens(bool weaveEater, float distance) { return weaveEater && distance <= MeleeReach; }
        /// <summary>
        /// A blow on <paramref name="e"/> from <paramref name="source"/>: when it blackens, the status goes on (or is refreshed) on the striker.
        /// True when it is fresh (the striker had none on): the session says so once.
        /// </summary>
        public static bool Struck(EncounterEnemy e, Actor source, float now) { return Struck(e, source, now, 1, BlackenShare); }
        /// <summary>Struck, stacking to <paramref name="most"/> (the Unraveller: three).</summary>
        public static bool Struck(EncounterEnemy e, Actor source, float now, int most, float share)
        {
            if (e == null || source == null || source == e.actor) return false;
            if (!Blackens(IsWeaveEater(e), Vector3.Distance(source.transform.position, e.transform.position))) return false;
            var c = source.GetComponent<Combatant>(); if (c == null || c.Statuses == null) return false;
            int had = SteelStacks(c.Statuses, now), next = UnravellerRules.SteelNext(had, most);
            if (next != had && had > 0) c.Statuses.Remove(BlackenedId);   // a deeper stack: put on afresh (Apply only refreshes the time)
            c.Statuses.Apply(Blackened(next, share), now);
            return had == 0;
        }
        /// <summary>What a throw does to a target: its sting (damage at the thrower's level), and the hold it puts on a Weave-Eater ("stun"
        /// for salt, "root" for a harpoon) and for how long; nothing held on anything else.</summary>
        public static (int sting, string hold, float seconds) Throw(string kind, int level, bool weaveEater)
        {
            int sting = Mathf.Max(1, (kind == Harpoon ? 3 : 2) * Mathf.Max(1, level));
            if (!weaveEater) return (sting, null, 0);
            return kind == Harpoon ? (sting, "root", HarpoonSeconds) : (sting, "stun", SaltSeconds);
        }
        /// <summary>The line the throw is told with.</summary>
        public static string ThrowLine(string kind, string target, bool weaveEater, bool held)
        {
            if (kind == Harpoon)
                return !weaveEater ? "The bone harpoon bites " + target + " and comes away on its rope. It is made for spirits." :
                    held ? "The bone harpoon goes through " + target + " and the amber rope goes taut: it is pinned where it is, and cannot displace." : "The harpoon strikes, but " + target + " tears free of it.";
            return !weaveEater ? "The salt stings " + target + "'s eyes, and that is all." :
                held ? "The salt flashes white on " + target + ": it calcifies, a jagged statue of white crystal for a few heartbeats." : "The salt hisses on " + target + ", but it shakes it off.";
        }
    }
}
