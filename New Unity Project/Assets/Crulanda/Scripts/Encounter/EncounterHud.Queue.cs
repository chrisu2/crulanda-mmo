using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The dungeon queue's window, "Gather a Party" (J, /queue; EncounterSession.Queue): the part you take (tank, healer, damage), each
    /// dungeon with its levels and how it stands with your lockouts and a Queue button, "Any for my level", what the queue is doing now
    /// (finding a group, the run under way and its leader, with Leave), and under it the Lockouts list (Lockouts): every boss and elite locked
    /// for you, where, and the time left to the 06:00 reset. GAME-ONLY.
    /// </summary>
    public sealed partial class EncounterHud
    {
        static bool queueVisible; static Rect queueRect;
        static bool QueueUiBlocks(Vector2 p) { return queueVisible && queueRect.Contains(p); }
        void DrawQueue()
        {
            var dungeons = session.QueueDungeons(); var locks = session.LockoutRows();
            float rowH = 28; int shownLocks = Mathf.Min(locks.Count, 8);
            float h = 250 + rowH * Mathf.Max(1, dungeons.Count) + 24 * Mathf.Max(1, shownLocks);
            queueRect = new Rect(330, 110, 780, Mathf.Min(h, 720));
            Frame(queueRect);
            var r = queueRect; var grey = new Color(.8f, .8f, .78f);
            GUI.Label(new Rect(r.x + 24, r.y + 14, 520, 34), "GATHER A PARTY · the dungeon queue", heading);
            if (GUI.Button(new Rect(r.xMax - 112, r.y + 16, 88, 28), "Close", slim)) session.QueueOpen = false;
            // Your part.
            float y = r.y + 56;
            Shadow(new Rect(r.x + 24, y + 4, 120, 22), "You go as:", tiny, grey);
            var roles = new[] { SimCompanion.PartyRole.Tank, SimCompanion.PartyRole.Healer, SimCompanion.PartyRole.Damage };
            for (int i = 0; i < roles.Length; i++)
            {
                bool on = session.QueueRole == roles[i]; var at = new Rect(r.x + 130 + i * 112, y, 104, 26);
                if (on) Fill(new Rect(at.x - 2, at.y - 2, at.width + 4, at.height + 4), gold);
                if (GUI.Button(at, EncounterSession.RoleWord(roles[i]), slim)) session.QueueRole = roles[i];
            }
            Shadow(new Rect(r.x + 470, y + 4, 290, 22), "The queue fills the rest by role.", tiny, grey);
            // The dungeons.
            y += 40;
            foreach (var (x, label) in new[] { (24f, "Dungeon"), (270f, "Levels"), (350f, "Your lockouts") }) Shadow(new Rect(r.x + x, y, 200, 20), label, tiny, new Color(.8f, .8f, .78f));
            y += 22; int level = session.Progress.Level; bool busy = session.QueuedFor != null || session.QueueRunZone != null;
            foreach (var d in dungeons)
            {
                bool fits = EncounterSession.QueueFits(d, level); var st = session.QueueLockState(d);
                Shadow(new Rect(r.x + 24, y + 3, 240, 22), d.displayName, tiny, fits ? Color.white : grey);
                Shadow(new Rect(r.x + 270, y + 3, 80, 22), d.levelMin + "-" + d.levelMax, tiny, ConColor(d.levelMin));
                string state = st.locked == 0 ? "loot ready" : st.locked + " of " + st.bosses + " locked: no loot from them for " + Lockouts.Words(st.left);
                Shadow(new Rect(r.x + 350, y + 3, 300, 22), state, tiny, st.locked == 0 ? new Color(.55f, 1, .7f) : new Color(1, .75f, .45f));
                GUI.enabled = fits && !busy;
                if (GUI.Button(new Rect(r.xMax - 112, y, 88, 24), "Queue", slim)) session.JoinQueue(d.id);
                GUI.enabled = true;
                y += rowH;
            }
            if (dungeons.Count == 0) { Shadow(new Rect(r.x + 24, y + 3, 500, 22), "No dungeon in this build.", tiny, grey); y += rowH; }
            GUI.enabled = !busy && session.QueueDungeonFor(level) != null;
            if (GUI.Button(new Rect(r.x + 24, y + 4, 200, 28), "Any for my level", slim)) session.JoinQueue("any");
            GUI.enabled = true;
            // What the queue is doing.
            string now = null; bool leave = false;
            if (session.QueuedFor != null) { now = "Finding a group for " + session.ZoneName(session.QueuedFor) + "... " + Mathf.CeilToInt(session.QueueWait) + " s"; leave = true; }
            else if (session.QueueRunZone != null)
            {
                var lead = session.QueueLeader != null ? session.PartySim(session.QueueLeader) : null;
                now = "In a queued run of " + session.ZoneName(session.QueueRunZone) + (lead != null ? ", led by " + lead.sim.name : "") + "."; leave = true;
            }
            if (now != null) Shadow(new Rect(r.x + 240, y + 9, 400, 22), now, tiny, new Color(1, .84f, .45f));
            if (leave && GUI.Button(new Rect(r.xMax - 172, y + 4, 148, 28), session.QueuedFor != null ? "Leave the queue" : "Leave the party", slim))
            { if (session.QueuedFor != null) session.LeaveQueue(); else session.DisbandQueue("You leave the queued party."); }
            // The lockouts.
            y += 44;
            Fill(new Rect(r.x + 24, y, r.width - 48, 1), gold);
            Shadow(new Rect(r.x + 24, y + 6, 520, 22), "LOCKOUTS · each boss and elite once a lockout, reset at 06:00", tiny, Color.white);
            y += 30;
            if (locks.Count == 0) Shadow(new Rect(r.x + 24, y, 700, 22), "None: every boss and elite is there for you to loot.", tiny, grey);
            for (int i = 0; i < shownLocks && y < r.yMax - 24; i++)
            {
                var l = locks[i];
                Shadow(new Rect(r.x + 24, y, 300, 22), l.name, tiny, Color.white);
                Shadow(new Rect(r.x + 330, y, 220, 22), l.where, tiny, grey);
                Shadow(new Rect(r.x + 560, y, 180, 22), Lockouts.Words(l.left), tiny, new Color(1, .75f, .45f));
                y += 24;
            }
            if (locks.Count > shownLocks) Shadow(new Rect(r.x + 24, y, 500, 22), "and " + (locks.Count - shownLocks) + " more (/lockouts lists them all).", tiny, grey);
        }
    }
}
