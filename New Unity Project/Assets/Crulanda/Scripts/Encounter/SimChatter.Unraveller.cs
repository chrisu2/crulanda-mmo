using System.Collections.Generic;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The sims at the Unraveller's warning (Docs/WORLD_BOSS_1.md 1.4): in Zone chat, a few of the Lowtowns' sims online call the raid in
    /// their own way ("Unraveller's coming up, who's in?", "LF raid Unraveller, need heals", "Bring salt."), the chattiest first, a beat apart.
    /// Only while the sims live their day (SimPopulation.Lively), as all their chatter.
    /// </summary>
    public sealed partial class SimChatter
    {
        /// <summary>The warning has sounded in this zone: up to three sims here say their line.</summary>
        public void UnravellerComing()
        {
            if (Population == null || !Population.Lively) return;
            var here = Here(); here.RemoveAll(s => s.chatty < .15f);
            here.Sort((a, b) => b.chatty.CompareTo(a.chatty));
            var lines = new List<string>(Unraveller.SimCalls);
            for (int i = 0; i < here.Count && lines.Count > 0; i++) { string line = lines[0]; lines.RemoveAt(0); Later(ChatChannel.Zone, here[i].name, line, i * 3); }
        }
    }
}
