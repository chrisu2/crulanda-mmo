using System;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Automated runs are silent (Chris, 2026-10-15: "please turn the music off in future testing i think i asked this before"): the editor's
    /// test runs (batch mode), the camera tours, the captures and the group runs (any --crulanda-...capture or ...tour flag, or the temp
    /// save they all use) start with every sound muted (AudioListener.volume 0). Playing the game normally is untouched.
    /// </summary>
    public static class QuietRuns
    {
        public static bool Active { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Mute()
        {
            Active = Application.isBatchMode;
            if (!Active)
                foreach (var a in Environment.GetCommandLineArgs())
                    if (a.StartsWith("--crulanda-", StringComparison.Ordinal) && (a.Contains("capture") || a.Contains("tour") || a == "--crulanda-temp-save")) { Active = true; break; }
            if (Active) AudioListener.volume = 0;
        }
    }
}
