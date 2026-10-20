using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The sound mixer (Chris, 2026-10-15: "need to be able to mute/adjust sliders for music, sounds, enviromental sounds, combat
    /// music, etc."). Five channels, each a level 0..1 and a mute, kept in PlayerPrefs. What a source plays at is its channel's
    /// level times the master's, or nothing when either is muted. Music keeps its old key ("music", the /music command's).
    /// Sound effects and ambient sounds have no sources yet (the game's only sound is the zone music); anything that plays one
    /// asks Effective(Effects) or Effective(Ambient).
    /// </summary>
    public static class AudioMix
    {
        public enum Channel { Master, Music, CombatMusic, Effects, Ambient }
        public static readonly Channel[] All = { Channel.Master, Channel.Music, Channel.CombatMusic, Channel.Effects, Channel.Ambient };
        public static string Label(Channel c)
        {
            switch (c)
            {
                case Channel.Master: return "Master";
                case Channel.Music: return "Music";
                case Channel.CombatMusic: return "Combat music";
                case Channel.Effects: return "Sound effects";
                default: return "Ambient sounds";
            }
        }
        static string Key(Channel c) { return c == Channel.Music ? ZoneMusic.VolumeKey : "audio." + c.ToString().ToLowerInvariant(); }
        static float Default(Channel c) { return c == Channel.Music ? ZoneMusic.DefaultVolume : c == Channel.Master ? 1 : .7f; }
        public static float Level(Channel c)
        {
            try { return Mathf.Clamp01(PlayerPrefs.GetFloat(Key(c), Default(c))); } catch { return Default(c); }
        }
        public static void SetLevel(Channel c, float v)
        {
            try { PlayerPrefs.SetFloat(Key(c), Mathf.Clamp01(v)); PlayerPrefs.Save(); } catch { }
        }
        public static bool Muted(Channel c)
        {
            try { return PlayerPrefs.GetInt(Key(c) + ".mute", 0) != 0; } catch { return false; }
        }
        public static void SetMuted(Channel c, bool m)
        {
            try { PlayerPrefs.SetInt(Key(c) + ".mute", m ? 1 : 0); PlayerPrefs.Save(); } catch { }
        }
        /// <summary>What a source on this channel plays at: 0 when it or the master is muted.</summary>
        public static float Effective(Channel c)
        {
            if (Muted(Channel.Master) || Muted(c)) return 0;
            return c == Channel.Master ? Level(Channel.Master) : Level(c) * Level(Channel.Master);
        }
    }
}
