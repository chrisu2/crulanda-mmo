using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Chemical River's rules (Lowtowns pick 6; Docs/ZONE_LOWTOWNS.md "The Chemical River"). The river is CANON: black, iridescent sludge
    /// that "bisected Argentis like an infected artery" (book1 ch.16:5), toxic ("It tasted of vinegar, burnt rubber, and alchemical waste",
    /// ch.16:5), and its "chemical runoff interferes with magical tracking. We're invisible to the Seekers for now" (ch.16:75).
    /// The game's reading (GAME-ONLY numbers):
    /// - It poisons: whoever of the party wades or swims in it loses <see cref="BitePerSecond"/> of their health a second, a bite each
    ///   <see cref="TickSeconds"/>; it never takes the last of it (a poison to get out of, not a death on its own).
    /// - It hides you from magical tracking: while you are in it, a Weave-Eater (it hunts by the Weave, "drawn to a mage's aura as a
    ///   lighthouse", ch.20:121) loses you unless it is close enough to touch you (<see cref="MeleeReach"/>), and a caster whose spell reaches
    ///   far (<see cref="CasterReach"/> or more) loses you unless you are within <see cref="CasterLoses"/>; each forgets you for
    ///   <see cref="LostSeconds"/> (the Rogue's Vanish, EncounterEnemy.LoseSight). Anything that simply sees you (a hound, a thug at your
    ///   elbow) keeps after you. Out of the water, they can find you again.
    /// </summary>
    public static class ChemicalRiver
    {
        public const float TickSeconds = .5f, BitePerSecond = .012f, LostSeconds = 6, MeleeReach = 3, CasterReach = 8, CasterLoses = 6;
        /// <summary>The health one tick of the river takes from someone of <paramref name="maxHealth"/>: at least one.</summary>
        public static int Bite(int maxHealth) { return Mathf.Max(1, Mathf.RoundToInt(maxHealth * BitePerSecond * TickSeconds)); }
        /// <summary>What the bite leaves: never under one (it does not kill).</summary>
        public static int BiteFrom(int current, int maxHealth) { return Mathf.Clamp(current - 1, 0, Bite(maxHealth)); }
        /// <summary>Whether a mob hunting someone in the river loses them: a Weave-Eater beyond touching, or a long-range caster beyond
        /// <see cref="CasterLoses"/>. <paramref name="castReach"/>: its spell's reach (0: no spell).</summary>
        public static bool LosesTrack(bool weaveEater, float castReach, float distance)
        {
            if (weaveEater) return distance > MeleeReach;
            return castReach >= CasterReach && distance > CasterLoses;
        }
        public const string EnterLine = "The black water closes over your boots, cold and slick as oil, and it burns. Whatever hunts by the Weave loses the scent of you in it.";
        public static string LostLine(string mob) { return mob + " casts about over the black water, and loses you."; }
    }
}
