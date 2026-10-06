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
        public const float HealthPerShare = .6f, DamagePerShare = .15f, MinShare = .25f, MaxShare = 1.25f;
        float unscaledHealth = -1;
        /// <summary>The group shares this mob is scaled for (0: not scaled).</summary>
        public float GroupShares { get; private set; }
        public float GroupHealthScale { get { return 1 + HealthPerShare * GroupShares; } }
        public float GroupDamageScale { get { return 1 + DamagePerShare * GroupShares; } }
        /// <summary>The shares a party brings against a mob of <paramref name="mobLevel"/>: one per sim, by its level over the mob's.</summary>
        public static float SharesFor(EncounterSession session, int mobLevel)
        {
            float shares = 0; int lv = Mathf.Max(1, mobLevel);
            foreach (var c in session.PartySims) if (c != null && c.actor != null && c.actor.IsAlive) shares += Mathf.Clamp((float)c.sim.level / lv, MinShare, MaxShare);
            return shares;
        }
        /// <summary>Scales to the party now; only ever up during a fight (a sim falling does not heal the mob).</summary>
        void ScaleToGroup()
        {
            if (Game || !actor.IsAlive || !FightingParty) return;   // a sim on its own fights it as it is
            float shares = SharesFor(session, actor.Level);
            if (shares <= GroupShares + .001f) return;
            GroupShares = shares;
            float baseMax = unscaledHealth > 0 ? unscaledHealth : actor.Stats.GetBase(StatType.MaxHealth);
            if (baseMax <= 0) return;
            if (unscaledHealth < 0) unscaledHealth = baseMax;
            actor.Stats.SetBase(StatType.MaxHealth, Mathf.Round(unscaledHealth * GroupHealthScale));   // the pool keeps its ratio
        }
        void UnscaleFromGroup()
        {
            GroupShares = 0;
            if (unscaledHealth > 0) { actor.Stats.SetBase(StatType.MaxHealth, unscaledHealth); unscaledHealth = -1; }
        }
        /// <summary>The target frame's line: "Scaled for your group (3)".</summary>
        public string GroupNote { get { return GroupShares > 0 ? "Scaled for your group of " + (1 + session.PartySims.Count) : null; } }
    }
}
