using System;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The world boss's timer as the world slot keeps it (WorldSave format 3; Docs/WORLD_BOSS_1.md 1.4 and 5.1): play-seconds counted since
    /// her last ending, the moment of the cycle she rises at, and her state (waiting, warned, up). Shared by every character (the world is).
    /// </summary>
    [Serializable]
    public sealed class WorldBossState
    {
        public const string Waiting = "waiting", Warned = "warned", Up = "up";
        /// <summary>waiting (the three hours running), warned (the five minutes before she rises), up (risen, for up to 45 minutes).</summary>
        public string state = Waiting;
        /// <summary>Play-seconds since the last kill or sinking (any zone, paused time not counted).</summary>
        public float played;
        /// <summary>The play-second of this cycle she rises at (3 hours and a random moment of the next 30 minutes); -1 until rolled.</summary>
        public float riseAt = -1;
        /// <summary>Seconds she has been up this rising.</summary>
        public float upFor;
        /// <summary>The raid wipes she has had this rising (two and she sinks, fed).</summary>
        public int wipes;
        /// <summary>Risings ended so far (each kill or sinking): the stream each cycle's rising moment is drawn from.</summary>
        public int cycles;
        /// <summary>Kills of her in this world.</summary>
        public int kills;
    }

    /// <summary>
    /// The world boss's clock (Docs/WORLD_BOSS_1.md 1.4; Chris, 2026-10-19: "Every 3 hours"): every 3 hours of play, counted in any zone and
    /// saved, she rises at a random moment of the 30 minutes after the 3 hours are up; the warning comes 5 minutes before; she stays 45 minutes,
    /// or until she is killed or has wiped the raid twice; a kill or her sinking starts the next 3 hours. Pure: the session ticks it with its
    /// play time and acts on the beat it returns (EncounterSession.Unraveller).
    /// </summary>
    public static class WorldBossClock
    {
        /// <summary>Three hours of play; the half hour she may rise in after it; the 45 minutes she stays; the 5-minute warning.</summary>
        public const float Cycle = 3 * 3600, Window = 30 * 60, Stay = 45 * 60, Warning = 5 * 60;
        /// <summary>Wipes of the raid before she sinks, fed.</summary>
        public const int WipesToSink = 2;
        /// <summary>What a tick brought about.</summary>
        public enum Beat { None, Warned, Rose, Sank }

        /// <summary>The play-second of cycle <paramref name="cycle"/> she rises at: 3 hours and a moment of the next 30 minutes, the same for a
        /// world's seed and cycle.</summary>
        public static float RollRise(int seed, int cycle)
        {
            var r = new Random(unchecked(seed * 7919 + cycle * 104729 + 13));
            return Cycle + Window * (float)r.NextDouble();
        }
        /// <summary>Counts <paramref name="seconds"/> of play. At most one beat a tick (a long tick that passes both the warning and the rising
        /// gives the warning now and the rising at the next tick).</summary>
        public static Beat Tick(WorldBossState s, float seconds, int seed)
        {
            if (s == null || seconds <= 0) return Beat.None;
            if (s.riseAt < 0) s.riseAt = RollRise(seed, s.cycles);
            switch (s.state)
            {
                case WorldBossState.Up:
                    s.upFor += seconds;
                    if (s.upFor >= Stay) { End(s, seed); return Beat.Sank; }
                    return Beat.None;
                case WorldBossState.Warned:
                    s.played += seconds;
                    if (s.played >= s.riseAt) { s.state = WorldBossState.Up; s.upFor = Math.Max(0, s.played - s.riseAt); s.wipes = 0; return Beat.Rose; }
                    return Beat.None;
                default:
                    s.state = WorldBossState.Waiting; s.played += seconds;
                    if (s.played >= s.riseAt - Warning) { s.state = WorldBossState.Warned; return Beat.Warned; }
                    return Beat.None;
            }
        }
        /// <summary>Seconds of play until she rises (0 while she is up).</summary>
        public static float UntilRise(WorldBossState s) { return s == null || s.state == WorldBossState.Up ? 0 : Math.Max(0, (s.riseAt < 0 ? Cycle : s.riseAt) - s.played); }
        /// <summary>Seconds she has left up (0 when she is not).</summary>
        public static float UpLeft(WorldBossState s) { return s == null || s.state != WorldBossState.Up ? 0 : Math.Max(0, Stay - s.upFor); }
        /// <summary>She was killed: the next 3 hours start.</summary>
        public static void Killed(WorldBossState s, int seed) { if (s == null) return; s.kills++; End(s, seed); }
        /// <summary>She wiped the raid: true when that was the last she takes (she sinks, fed; the next 3 hours start).</summary>
        public static bool Wiped(WorldBossState s, int seed)
        {
            if (s == null || s.state != WorldBossState.Up) return false;
            s.wipes++;
            if (s.wipes < WipesToSink) return false;
            End(s, seed); return true;
        }
        /// <summary>Her rising over (killed or sunk): waiting again, from nothing, with the next cycle's moment rolled.</summary>
        static void End(WorldBossState s, int seed)
        {
            s.state = WorldBossState.Waiting; s.played = 0; s.upFor = 0; s.wipes = 0; s.cycles++; s.riseAt = RollRise(seed, s.cycles);
        }
    }

    /// <summary>
    /// The Unraveller, the brood's mother (GAME-ONLY name and creature from the CANON Weave-Eaters, book1 ch.1:251, ch.20:113-205; WORLD_BOSS_1.md
    /// 1.2): her name, level and raid size, the placeholder numbers until the raid's data file (step 6 tunes them), and every line she and the
    /// Lowtowns say. Her lines are written in the chat, never voiced (she makes no sound, ch.20:113).
    /// </summary>
    public static class Unraveller
    {
        public const string Name = "The Unraveller", Tag = "unraveller", Id = "mob.unraveller.lowtowns.wb.0", Zone = "zone.lowtowns";
        public const int Level = 30, RaidSize = 10;
        /// <summary>Her health after the +5 clamp is 160,000 for a raid at 20 (WORLD_BOSS_1.md 3.2): the pool is that times what she takes at +5
        /// (EncounterEnemy.OvermatchTaken(30, 20) = 0.7). PROVISIONAL until the harness (step 6).</summary>
        public const int EffectiveHealth = 160000;
        /// <summary>Her health pool: <see cref="EffectiveHealth"/> times what she takes at +5 against the level cap.</summary>
        public static int HealthPool { get { return UnityEngine.Mathf.RoundToInt(EffectiveHealth * EncounterEnemy.OvermatchTaken(Level, EncounterProgress.LevelCap)); } }
        /// <summary>Her swing's base before the +5 clamp's 1.6 and the tank's armour (about 300 on a level-20 tank, 3.2). PROVISIONAL.</summary>
        public const float HitBase = 190;
        /// <summary>How much bigger than a Weave-Eater she stands (three times a man's height, 1.2).</summary>
        public const float Scale = 2.6f;
        /// <summary>Seconds she takes to rise out of the river, and to sink back into it.</summary>
        public const float RiseSeconds = 4, SinkSeconds = 5;
        /// <summary>The villagers within this of the bar go indoors at the warning (1.3).</summary>
        public const float IndoorsRange = 60;
        public const string Tibb = "Tibb Marrow", Ketta = "Ketta Rune";

        // ---------- the warning (5 minutes before; the Lowtowns only) ----------
        public const string WarningSystem = "The Chemical River has gone still above the Rag Bridge. The drains are quiet.";
        public const string TibbCall = "Salt to the river, salt to the river! Two crowns a twist and worth it tonight.";
        public const string KettaCall = "Bone to the Drain Reach! Bring your arms, I'll bring the rope.";
        /// <summary>The sims' chatter in Zone chat at the warning (SimChatter.UnravellerComing).</summary>
        public static readonly string[] SimCalls = { "Unraveller's coming up, who's in?", "LF raid Unraveller, need heals", "Bring salt." };
        // ---------- her rising, her end (System lines, every zone) ----------
        public const string RisenSystem = "The Unraveller has risen in the Drain Reach of the Lowtowns.";
        public const string SunkSystem = "The Unraveller sinks back into the river, fed.";
        public const string OthersKilledSystem = "A raid of the Lowtowns has salted the Unraveller. The drains are quiet.";
        public static string KilledSystem(string leader, string guild) { return (string.IsNullOrEmpty(leader) ? "You" : leader) + " and " + (string.IsNullOrEmpty(guild) ? "your raid" : guild) + " have salted the Unraveller. The Lowtowns' drains are quiet."; }
        // ---------- her own lines (the chat, pale grey; no voice) ----------
        public const string RisingLine = "...hungry...", WipeLine = "...fed...";
        /// <summary>A line of hers as the chat shows it.</summary>
        public static string Says(string line) { return Name + ": " + line; }
    }
}
