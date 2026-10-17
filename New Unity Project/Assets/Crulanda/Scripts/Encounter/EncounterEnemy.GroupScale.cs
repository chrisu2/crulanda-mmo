using UnityEngine;
using Crulanda.Core;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Mobs scale to your group (2026-10-06; Chris: "when group mobs should scale based on # in group and avg level of group
    /// members", and chose strength over level): a mob keeps its zone level, but each sim in your party adds a share, its level
    /// over the mob's (a quarter to a share and a quarter), and each share adds <see cref="HealthPerShare"/> to the mob's health and
    /// <see cref="DamagePerShare"/> to its blows. You and Mira are what the mobs were tuned for, so alone (with Mira) nothing
    /// changes. Set when the fight starts (and raised if the party grows during it); undone when it resets.
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        // Raised (playtest note 91, 2026-10-07: "mobs still way too easy in group, can level without healer or CC"): a sim adds about
        // a player's worth of health to the mob and nearly half again of its blows, so a group's tank needs a healer and the pulls a plan.
        // Eased 2026-10-11 for the party of five (the group run of the Adit, RunCapture: four level-11 sims made each level-10 picket
        // 2,106 health hitting for 72 against people of 415, and a full group with a Druid mending every 2.5 s died ten times at the first
        // camp without clearing it): three fifths of a player's health and a fifth of its blows per share, so five against a camp of three
        // is a fight a healer can keep up with.
        public const float HealthPerShare = .6f, DamagePerShare = .2f, MinShare = .25f, MaxShare = 1.25f;
        float unscaledHealth = -1;
        /// <summary>The group shares this mob is scaled for (0: not scaled).</summary>
        public float GroupShares { get; private set; }
        /// <summary>The health shares it is scaled for: a sim in the damage part weighs <see cref="DamagePartHealth"/> (HealthSharesFor).</summary>
        public float HealthShares { get; private set; }
        public float GroupHealthScale { get { return 1 + HealthPerShare * HealthShares; } }
        /// <summary>
        /// A damage-part sim's share of a mob's health, over a tank's or a healer's (2026-10-17): the damage sims were raised to the pure
        /// damage band (SimCompanion.Damage, about an eighth more for a Rogue or a Ranger with its wolf at 15-20), so the mobs they fight
        /// stand a little longer and the fights' length holds. Its blows are not touched (SharesFor).
        /// </summary>
        public const float DamagePartHealth = 1.2f;
        /// <summary>The health shares a party brings against a mob of <paramref name="mobLevel"/>: SharesFor, a damage-part sim's weighed
        /// <see cref="DamagePartHealth"/>.</summary>
        public static float HealthSharesFor(EncounterSession session, int mobLevel)
        {
            float shares = SharesFor(session, mobLevel); int lv = Mathf.Max(1, mobLevel);
            foreach (var c in session.PartySims)
                if (c != null && c.actor != null && c.actor.IsAlive && c.Role == SimCompanion.PartyRole.Damage) shares += (DamagePartHealth - 1) * Mathf.Clamp((float)c.sim.level / lv, MinShare, MaxShare);
            return shares;
        }
        /// <summary>
        /// Its blows' scale: a share's worth more for each, and as much again as the sims' gear makes them sturdier (GroupToughness). The
        /// shares were tuned (2026-10-11) for sims whose health counted none of their gear's Stamina; Chris, 2026-10-16 ("Fix it now"), had
        /// it counted (SimCompanion.MaxHealthFor), so the blows rise with it and a party's fight is as hard as it was. A sim healer's mend
        /// (16% of the hurt one's health: SimCompanion.MendFor) rises with the health it mends.
        /// </summary>
        public float GroupDamageScale { get { return (1 + DamagePerShare * GroupShares) * GroupToughness; } }
        /// <summary>The party's sims' mean gear toughness (SimCompanion.GearToughness) when it was scaled; 1 when not scaled.</summary>
        public float GroupToughness { get; private set; } = 1;
        /// <summary>The mean gear toughness of the party's living sims (1 with none).</summary>
        public static float ToughnessOf(EncounterSession session)
        {
            float sum = 0; int n = 0;
            foreach (var c in session.PartySims) if (c != null && c.actor != null && c.actor.IsAlive) { sum += SimCompanion.GearToughness(c.sim, session.Items); n++; }
            return n > 0 ? sum / n : 1;
        }
        /// <summary>The shares a party brings against a mob of <paramref name="mobLevel"/>: one per sim, by its level over the mob's.</summary>
        public static float SharesFor(EncounterSession session, int mobLevel)
        {
            float shares = 0; int lv = Mathf.Max(1, mobLevel);
            foreach (var c in session.PartySims) if (c != null && c.actor != null && c.actor.IsAlive) shares += Mathf.Clamp((float)c.sim.level / lv, MinShare, MaxShare);
            // Mira counts as a member like any other (Chris, 2026-10-08: "she should be same as me"): at your level she is a share.
            if (session.Companion != null && session.Companion.actor != null && session.Companion.actor.IsAlive && session.Progress != null && session.Progress.recruited)
                shares += Mathf.Clamp((float)session.Progress.Level / lv, MinShare, MaxShare);
            return shares;
        }
        /// <summary>Scales to the party now; only ever up during a fight (a sim falling does not heal the mob).</summary>
        void ScaleToGroup()
        {
            if (Game || !actor.IsAlive || !FightingParty) return;   // a sim on its own fights it as it is (you and Mira are a party of two)
            float shares = SharesFor(session, actor.Level);
            if (shares <= GroupShares + .001f) return;
            GroupShares = shares; HealthShares = Mathf.Max(HealthShares, HealthSharesFor(session, actor.Level)); GroupToughness = Mathf.Max(GroupToughness, ToughnessOf(session));
            float baseMax = unscaledHealth > 0 ? unscaledHealth : actor.Stats.GetBase(StatType.MaxHealth);
            if (baseMax <= 0) return;
            if (unscaledHealth < 0) unscaledHealth = baseMax;
            actor.Stats.SetBase(StatType.MaxHealth, Mathf.Round(unscaledHealth * GroupHealthScale));   // the pool keeps its ratio
        }
        void UnscaleFromGroup()
        {
            GroupShares = 0; HealthShares = 0; GroupToughness = 1;
            if (unscaledHealth > 0) { actor.Stats.SetBase(StatType.MaxHealth, unscaledHealth); unscaledHealth = -1; }
        }
        /// <summary>The target frame's line: "Scaled for your group (3)".</summary>
        public string GroupNote { get { return GroupShares > 0 ? "Scaled for your group of " + (1 + session.PartySims.Count + (session.Companion != null && session.Progress.recruited ? 1 : 0)) : null; } }
    }
}
