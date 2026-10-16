namespace Crulanda.Encounter
{
    /// <summary>
    /// A night camp's mob (ZoneCamp.night; the Lowtowns' Drain Courts, Phase 9.1, GAME-ONLY): the Weave-Eaters come up out of the drains after
    /// dark and go back down by day. Gone, it is switched off where it stood, as a mob put away by its lockout is (EncounterEnemy.Lockout),
    /// but not dead: nothing to loot, no kill. It goes only when it is alive and has nobody to fight, so a fight at dawn is finished first;
    /// it comes back up at nightfall as a camp mob respawns, somewhere in its camp, whole. EncounterSession.TickNight calls both.
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        /// <summary>Of a night camp: about only after dark.</summary>
        public bool NightOnly;
        /// <summary>Gone back down by day (switched off), until the night brings it up.</summary>
        public bool Down { get; private set; }
        /// <summary>Back down the drains: alive, fighting nobody, not called; else it stays until it is.</summary>
        public void GoDown()
        {
            if (Down || Away || actor == null || !actor.IsAlive || Victim != null || threat.Any || joinAt >= 0 || Inside != null) return;
            SetDormant(false); ClearFight(); TappedBy = null; LootBeacon.Clear(this);
            Down = true; respawnAt = float.MaxValue;
            gameObject.SetActive(false);
        }
        /// <summary>Up again at nightfall: switched on and respawned in its camp, whole (a boss still locked for you stays away).</summary>
        public void ComeOut()
        {
            if (!Down) return;
            Down = false; if (Away) return;
            gameObject.SetActive(true);
            Respawn();
        }
    }
}
