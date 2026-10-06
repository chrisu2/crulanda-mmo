using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The who list (Phase 5.2 round 2, 2026-10-06; O): every adventurer online now by the world clock, in your zone first, with
    /// class (in its colour), level and zone, a mark for those in your party, and Invite for those standing in your zone.
    /// </summary>
    public sealed partial class EncounterHud
    {
        static bool whoVisible; static Rect whoRect;
        static bool WhoUiBlocks(Vector2 p) { return whoVisible && whoRect.Contains(p); }
        void DrawWho()
        {
            var pop = SimPopulation.Active; if (pop == null) return;
            var list = pop.World.sims.FindAll(s => s.IsOnlineAt(Crulanda.World.WorldClock.Hour) || session.InParty(s.id));
            string here = session.ZoneId;
            list.Sort((a, b) => { int za = a.zone == here ? 0 : 1, zb = b.zone == here ? 0 : 1; return za != zb ? za.CompareTo(zb) : b.level != a.level ? b.level.CompareTo(a.level) : string.CompareOrdinal(a.name, b.name); });
            float rowH = 26, h = 92 + rowH * Mathf.Max(1, list.Count);
            whoRect = new Rect(430, 120, 580, Mathf.Min(h, 640));
            Frame(whoRect);
            GUI.Label(new Rect(whoRect.x + 24, whoRect.y + 14, 400, 34), "WHO · " + list.Count + " online", heading);
            Shadow(new Rect(whoRect.x + 24, whoRect.y + 50, 520, 20), "Name                                  Class         Level   Where", tiny, new Color(.8f, .8f, .78f));
            float y = whoRect.y + 72;
            foreach (var s in list)
            {
                if (y > whoRect.yMax - rowH) break;
                bool member = session.InParty(s.id), inZone = s.zone == here;
                Shadow(new Rect(whoRect.x + 24, y, 200, 22), s.name + (member ? "  (party)" : ""), tiny, member ? new Color(.55f, 1, .7f) : Color.white);
                Shadow(new Rect(whoRect.x + 236, y, 100, 22), SimRoster.ClassName(s.classId), tiny, ClassColour(s.classId));
                Shadow(new Rect(whoRect.x + 334, y, 40, 22), s.level.ToString(), tiny, ConColor(s.level));
                Shadow(new Rect(whoRect.x + 378, y, 120, 22), session.ZoneName(s.zone), tiny, inZone ? new Color(1, .84f, .45f) : new Color(.8f, .8f, .78f));
                if (!member && inZone && SimPopulation.Active.Find(s.id) != null && GUI.Button(new Rect(whoRect.x + 494, y, 66, 22), "Invite", slim)) session.Invite(s.id);
                y += rowH;
            }
            if (list.Count == 0) Shadow(new Rect(whoRect.x + 24, y, 520, 22), "Nobody else is about at this hour.", tiny, Color.white);
        }
    }
}
