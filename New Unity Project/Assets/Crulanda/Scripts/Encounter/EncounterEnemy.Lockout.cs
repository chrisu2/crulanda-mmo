using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A boss or elite under a lockout (Lockouts; EncounterSession.Lockout.cs; Chris, 2026-10-15: "bosses/elites will not respawn"). One
    /// locked for this character is put away: dead, its body gone (the object switched off), nothing to loot, and it does not come back with
    /// its camp. Its lockout run out (or a quest you hold wanting it after all), the session brings it back as a camp mob respawns.
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        /// <summary>Put away by its lockout: dead, its body gone, not coming back until the session says.</summary>
        public bool Away { get; private set; }
        /// <summary>Killed while it was locked (a queued run's fresh dungeon, a quest's kill): its body holds no loot.</summary>
        public bool NoLoot;
        bool layingAway;
        /// <summary>How long a body under a lockout waits before the session looks again (it is put away, or comes back if the lockout ran out).</summary>
        public const float LockoutRecheck = 30;

        /// <summary>Dead, the body gone, nothing on it (on a zone's load, or where it would have respawned): no death call, no kill, no loot.</summary>
        public void PutAway()
        {
            if (Away) return;
            SetDormant(false);
            if (actor.IsAlive) { layingAway = true; try { actor.Health.ApplyDamage(actor.Health.Pool.Max); } finally { layingAway = false; } }
            Away = true; NoLoot = false;
            ClearFight(); threat.Clear(); Victim = null; TappedBy = null; RaidMarked = RaidMark.None;
            Looted = true; Drops = null; Coins = 0; LootBeacon.Clear(this);
            if (Inside != null) { Inside = null; Shown(true); if (agent != null) agent.enabled = true; var look = GetComponent<ActorVisual>(); if (look != null) look.Riding = false; }   // a rider put away in the saddle: down off its mount
            respawnAt = float.MaxValue;
            gameObject.SetActive(false);
        }
        /// <summary>Back from its lockout: switched on and respawned in its camp, whole, as a camp mob comes back.</summary>
        public void ComeBack()
        {
            if (!Away) return;
            Away = false; gameObject.SetActive(true);
            Respawn();
        }
    }
}
