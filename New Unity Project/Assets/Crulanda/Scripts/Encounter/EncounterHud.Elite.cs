using UnityEngine;

namespace Crulanda.Encounter
{
    // The target frame's part of an elite's moves (playtest note 2): its heavy blow as a cast bar, and its enrage.
    public sealed partial class EncounterHud
    {
        static readonly Color BlowBar = new Color(.95f, .58f, .1f);
        /// <summary>
        /// While the target draws back for its heavy blow: a cast bar under its health (in the swing bar's place) with the move's
        /// name, and under it the seconds left and what to do. Returns false when it is not winding up (the swing bar is drawn).
        /// </summary>
        bool DrawBlowBar(EncounterEnemy t)
        {
            if (t == null || t.Move == null || !t.WindingUp) return false;
            UnitBar(new Rect(378, 66, 244, 14), t.WindupProgress, BlowBar, t.Move.name);
            Shadow(new Rect(378, 80, 330, 20), t.WindupRemaining.ToString("0.0") + "s  ·  step out of the mark" + (session.Warrior != null ? " or raise Guard" : ""), tiny, new Color(1, .86f, .5f));
            return true;
        }
        /// <summary>The target frame's status words with an elite's enrage in front.</summary>
        static string WithEnrage(EncounterEnemy t, string extra)
        {
            if (t != null && t.GroupNote != null) extra = string.IsNullOrEmpty(extra) ? t.GroupNote : extra + "  ·  " + t.GroupNote;   // scaled for your group
            return t == null || !t.Enraged ? extra : "ENRAGED" + (string.IsNullOrEmpty(extra) ? "" : "  ·  " + extra);
        }
    }
}
