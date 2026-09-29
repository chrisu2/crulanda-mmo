using System;
using Crulanda.Core;

namespace Crulanda.Gameplay
{
    /// <summary>
    /// Relative difficulty of a target for a viewer ("con" colour in classic MMOs).
    /// Thresholds are PROVISIONAL and expected to be tuned in Phase 1. Pure and unit-tested.
    /// </summary>
    public static class LevelCon
    {
        public static ConDifficulty Evaluate(int viewerLevel, int targetLevel)
        {
            int diff = targetLevel - viewerLevel;
            if (diff <= -5) return ConDifficulty.Trivial;
            if (diff <= -2) return ConDifficulty.Easy;
            if (diff <= 1) return ConDifficulty.Even;
            if (diff <= 3) return ConDifficulty.Tough;
            if (diff <= 5) return ConDifficulty.Dangerous;
            return ConDifficulty.Deadly;
        }
    }
}
