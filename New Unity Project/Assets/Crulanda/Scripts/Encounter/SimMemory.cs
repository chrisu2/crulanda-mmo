using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// What the sims make of you (Phase 5.5, 2026-10-07): a regard score on each from what you have done together, and the standing
    /// it adds up to. Grouping is the main road to friendship (a point a minute together, a point for every few kills), passing on
    /// loot a sim needed counts for it, needing against its need counts against you, an answered whisper a little; a friend
    /// greets you when you meet, whispers you when it logs on, asks you to group, and gives you the loot it would have greeded;
    /// a rival will not group with you and does not answer. Saved with the sim (WorldSave).
    /// </summary>
    public static class SimMemory
    {
        public enum Standing { Rival, Stranger, Acquaintance, Friend }
        public enum Deed { MinuteTogether, KillsTogether, PassedToIt, NeededOverIt, WhisperAnswered, Befriended, Kicked }
        public const int FriendAt = 25, AcquaintanceAt = 6, RivalAt = -10;
        public static Standing Of(SimAdventurer s)
        {
            if (s == null) return Standing.Stranger;
            if (s.regard <= RivalAt) return Standing.Rival;
            if (s.regard >= FriendAt) return Standing.Friend;
            return s.regard >= AcquaintanceAt ? Standing.Acquaintance : Standing.Stranger;
        }
        public static string Label(Standing st) { return st == Standing.Rival ? "rival" : st == Standing.Friend ? "friend" : st == Standing.Acquaintance ? "acquaintance" : "stranger"; }
        public static int Worth(Deed d)
        {
            switch (d)
            {
                case Deed.MinuteTogether: return 1;
                case Deed.KillsTogether: return 1;
                case Deed.PassedToIt: return 3;
                case Deed.NeededOverIt: return -4;
                case Deed.WhisperAnswered: return 1;
                case Deed.Befriended: return 2;
                case Deed.Kicked: return -3;
            }
            return 0;
        }
        /// <summary>A deed done: the regard moves, and a standing crossed is said.</summary>
        public static Standing Note(SimAdventurer s, Deed d, int times = 1)
        {
            if (s == null) return Standing.Stranger;
            var was = Of(s);
            s.regard = Mathf.Clamp(s.regard + Worth(d) * Mathf.Max(1, times), -100, 100);
            var now = Of(s);
            if (now != was) SimChatter.Active?.StandingChanged(s, was, now);
            return now;
        }
        /// <summary>A friend will not be refused for being a little far from your level; a rival refuses outright.</summary>
        public static bool Refuses(SimAdventurer s) { return Of(s) == Standing.Rival; }
    }
}
