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
    /// </summary>
    public static class WeaveEaterRules
    {
        public const float SaltSeconds = 4, HarpoonSeconds = 5, BlackenSeconds = 20, BlackenShare = .1f, ThrowRange = 15, MeleeReach = 4.5f;
        public const string Salt = "salt", Harpoon = "harpoon";
        public const string BlackenedId = "debuff.blackened_steel", BlackenedName = "Blackened steel";
        public const string BlackenedLine = "Your steel comes out of the Weave-Eater black and brittle. Your blows land softer for a while.";
        public const string NoTargetLine = "Throw it at what? Pick out a foe first.";
        /// <summary>A thrown item's kind the game knows (salt or a harpoon), or null.</summary>
        public static string ThrownKind(ItemDef d) { return d == null || d.kind != "consumable" ? null : d.thrown == Salt || d.thrown == Harpoon ? d.thrown : null; }
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
        public static StatusEffectDefinition Blackened()
        {
            return new StatusEffectDefinition { id = BlackenedId, name = BlackenedName, duration = BlackenSeconds, incomingDamageMultiplier = 1,
                modifiers = new[] { new StatusModifierDefinition { stat = StatType.AttackPower, operation = ModifierOp.PercentAdd, value = -BlackenShare } } };
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
        public static bool Struck(EncounterEnemy e, Actor source, float now)
        {
            if (e == null || source == null || source == e.actor) return false;
            if (!Blackens(IsWeaveEater(e), Vector3.Distance(source.transform.position, e.transform.position))) return false;
            var c = source.GetComponent<Combatant>(); if (c == null || c.Statuses == null) return false;
            bool fresh = c.Statuses.Remaining(BlackenedId, now) <= 0;
            c.Statuses.Apply(Blackened(), now);
            return fresh;
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
                    held ? "The bone harpoon goes through " + target + " and the amber rope goes taut: it is pinned where it is." : "The harpoon strikes, but " + target + " tears free of it.";
            return !weaveEater ? "The salt stings " + target + "'s eyes, and that is all." :
                held ? "The salt bites: " + target + " goes white and still, crusted like a rock-pool." : "The salt hisses on " + target + ", but it shakes it off.";
        }
    }
}
