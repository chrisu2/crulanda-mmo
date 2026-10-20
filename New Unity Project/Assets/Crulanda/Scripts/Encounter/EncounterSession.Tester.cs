using System;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Tester's commands that work in every build (Chris, 2026-10-13: "i need a way to fast level for testing", "and then reset to 1 when
    /// not testing", "f keys do nothing": F10 and the console are for development builds only). /level n sets the level, up or down (down:
    /// the talents are refunded if they no longer fit, the AA slots too below the cap); /aa n gives n alternate advancement points at the cap.
    /// </summary>
    public partial class EncounterSession
    {
        void TesterLevel(string arg)
        {
            if (!int.TryParse(arg, out int n)) { Message("Usage: /level <1-" + EncounterProgress.LevelCap + ">, e.g. /level 15 to test, /level 1 to start over."); return; }
            if (InCombat) { Message("Not in a fight."); return; }
            n = Mathf.Clamp(n, 1, EncounterProgress.LevelCap); int was = Progress.Level;
            Progress.experience = EncounterProgress.XpForLevel(n);
            if (n < EncounterProgress.LevelCap) Progress.aaSlots = 0;
            if (Talents != null && Talents.Available(Progress) < 0) ResetTalents();   // more spent than the new level allows: all refunded
            ApplyLevel(); if (Player != null && Player.IsAlive) Player.Health.ApplyHealing(Player.Health.Pool.Max);
            Save(false);
            Message("Testing: level " + was + " to " + n + "." + (n < was ? " Talents refunded if they no longer fit." : " Spend your talents with B.") + " /level 1 starts over.");
        }
        void TesterAa(string arg)
        {
            if (!int.TryParse(arg, out int n) || n < 1) { Message("Usage: /aa <points>, at level " + EncounterProgress.LevelCap + "."); return; }
            if (Progress.Level < EncounterProgress.LevelCap) { Message("Alternate advancement comes at level " + EncounterProgress.LevelCap + " (/level " + EncounterProgress.LevelCap + ")."); return; }
            Progress.experience += Math.Min(50, n) * ActionBarRules.AaPointXp; Save(false);
            Message("Testing: " + Math.Min(50, n) + " AA points given; " + AaAvailable + " to spend. Buy bar slots in the spellbook (P).");
        }
    }
}
