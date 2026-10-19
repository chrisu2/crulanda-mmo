using UnityEngine;

namespace Crulanda.Encounter
{
    // The target frame's part of an elite's moves (playtest note 2): its heavy blow as a cast bar, and its enrage.
    public sealed partial class EncounterHud
    {
        static readonly Color BlowBar = new Color(.95f, .58f, .1f), CastBar = new Color(.85f, .45f, 1);
        /// <summary>
        /// While the target draws back for its heavy blow: a cast bar under its health (in the swing bar's place) with the move's
        /// name, and under it the seconds left and what to do. Returns false when it is not winding up (the swing bar is drawn).
        /// </summary>
        bool DrawBlowBar(EncounterEnemy t)
        {
            if (t != null && t.Casting && t.Cast != null)   // a mob's spell (CC step C5): interrupt it
            {
                UnitBar(new Rect(378, 66, 244, 14), t.CastProgress, CastBar, t.Cast.name);
                Shadow(new Rect(378, 80, 330, 20), t.CastRemaining.ToString("0.0") + "s  ·  interrupt it", tiny, new Color(1, .86f, .5f));
                return true;
            }
            if (t == null || t.Move == null || !t.WindingUp) return false;
            UnitBar(new Rect(378, 66, 244, 14), t.WindupProgress, BlowBar, t.Move.name);
            Shadow(new Rect(378, 80, 330, 20), t.WindupRemaining.ToString("0.0") + "s  ·  step out of the mark" + (session.Warrior != null ? " or raise Guard" : ""), tiny, new Color(1, .86f, .5f));
            return true;
        }
        /// <summary>A raid boss's skull (the raid mark's icon; a white [SKULL] where the icon is missing).</summary>
        void RaidSkull(Rect r)
        {
            var icon = Mark("skull");
            if (icon != null) { Fill(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), new Color(0, 0, 0, .55f)); GUI.DrawTexture(r, icon); }
            else Outlined(new Rect(r.x - 30, r.y, r.width + 30, r.height), "[SKULL]", tiny, Color.white);
        }
        /// <summary>The target frame's status words with an elite's enrage in front.</summary>
        static string WithEnrage(EncounterEnemy t, string extra)
        {
            if (t != null && t.GroupNote != null) extra = string.IsNullOrEmpty(extra) ? t.GroupNote : extra + "  ·  " + t.GroupNote;   // scaled for your group
            if (t != null && t.RankNote != null) extra = string.IsNullOrEmpty(extra) ? t.RankNote : t.RankNote + "  ·  " + extra;   // a raid boss: "Raid (10)" (EncounterEnemy.Raid)
            return t == null || !t.Enraged ? extra : "ENRAGED" + (string.IsNullOrEmpty(extra) ? "" : "  ·  " + extra);
        }
    }
}
