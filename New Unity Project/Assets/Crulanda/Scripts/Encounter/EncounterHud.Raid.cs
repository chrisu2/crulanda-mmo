using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The raid's HUD (WORLD_BOSS_1.md section 2.4, build plan step 1; 2026-10-19): the raid frames in place of the party frames (two columns
    /// of five, one a party: name in class colour, a role mark, health, a mana sliver for healers, a row kept for the fight's marks; click one
    /// to target it), the boss frame (her health, phase, cast, her pin state and the clock against the enrage), the ready check's line, and the
    /// raid window (/raid: the roster by party with parts and gear levels, your lockout, the call to arms, the ready check, leave).
    /// </summary>
    public sealed partial class EncounterHud
    {
        static bool raidVisible; static Rect raidRect;
        static bool RaidUiBlocks(Vector2 p) { return raidVisible && raidRect.Contains(p); }
        static readonly Color TankBlue = new Color(.8f, .85f, 1), HealGreen = new Color(.6f, 1, .7f), DamageOrange = new Color(1, .8f, .6f);
        static Color RoleColour(SimCompanion.PartyRole r) { return r == SimCompanion.PartyRole.Tank ? TankBlue : r == SimCompanion.PartyRole.Healer ? HealGreen : DamageOrange; }

        /// <summary>The ten in two columns (one per party), in place of the party frames.</summary>
        void DrawRaidFrames()
        {
            var raid = session.Raid; if (raid == null) return;
            for (int p = 0; p < RaidGroup.Parties; p++)
            {
                Shadow(new Rect(RaidFrames.Left + p * (RaidFrames.CellW + RaidFrames.Gap), RaidFrames.Top - 22, RaidFrames.CellW, 20), "Party " + (p + 1), tiny, new Color(1, .84f, .45f));
                var members = raid.PartyMembers(p);
                for (int i = 0; i < members.Count; i++)
                {
                    var m = members[i]; var r = RaidFrames.Cell(p, i);
                    Crulanda.Gameplay.Actor a = m.player ? session.Player : session.PartySim(m.id)?.actor;
                    bool alive = a != null && a.IsAlive;
                    Fill(r, new Color(0, 0, 0, .55f));
                    if (raid.Checking || m.ready == ReadyAnswer.NotReady) Fill(new Rect(r.xMax - 6, r.y, 6, r.height), m.ready == ReadyAnswer.Ready ? HealthGreen : m.ready == ReadyAnswer.NotReady ? new Color(.9f, .25f, .2f) : new Color(.9f, .8f, .3f));
                    Shadow(new Rect(r.x + 4, r.y, 18, 18), RaidFrames.RoleMark(m.role), tiny, RoleColour(m.role));
                    Shadow(new Rect(r.x + 20, r.y, r.width - 28, 18), (m.id == raid.LeaderId ? "* " : "") + m.name, tiny, alive ? ClassColour(m.classId) : new Color(.55f, .55f, .55f));
                    UnitBar(new Rect(r.x + 4, r.y + 19, r.width - 12, 10), a != null ? a.Health.Pool.Ratio : 0, HealthGreen, "");
                    if (m.role == SimCompanion.PartyRole.Healer && a != null && a.Resource != null) UnitBar(new Rect(r.x + 4, r.y + 31, r.width - 12, 4), a.Resource.Pool.Ratio, ResourceColor(Crulanda.Core.ResourceKind.Mana), "");
                    // The fight's marks on this member (Unravel stacks, Hunted, Swallowed, blackened steel: EncounterSession.RaidMarksOf).
                    var marks = alive ? session.RaidMarksOf(a) : null;
                    if (marks != null) Shadow(new Rect(r.x + 4, r.y + 27, r.width - 10, 16), marks, tiny, marks.Contains("Swallowed") || marks.Contains("Hunted") ? new Color(1, .45f, .4f) : new Color(.8f, .72f, 1));
                    if (!m.player && GUI.Button(r, GUIContent.none, GUIStyle.none)) session.SelectSim(m.id);   // click to target
                }
            }
            if (raid.Checking) Shadow(new Rect(RaidFrames.Left, RaidFrames.Top + 5 * (RaidFrames.CellH + RaidFrames.Gap), 320, 20), "Ready check: " + raid.ReadyCount + " of " + raid.Count + " ready", tiny, new Color(.9f, .8f, .3f));
        }

        /// <summary>The boss frame (section 2.4): the raid's boss, else a raid's elite or boss target. Her health, the phase, her cast, her pin
        /// state ("Pinned 8 s" or "Free") and the clock against the enrage.</summary>
        void DrawBossFrame()
        {
            DrawRaidWarning();
            if (!session.InRaid) return;
            var b = session.RaidBoss ?? (session.Target != null && session.Target.Elite ? session.Target : null);
            if (b == null || b.actor == null) return;
            var a = b.actor; var r = new Rect(560, 100, 330, b.RaidBoss ? 98 : 78);
            Fill(r, new Color(0, 0, 0, .6f));
            Shadow(new Rect(r.x + 8, r.y + 2, 220, 20), a.DisplayName, frameName, new Color(1, .55f, .45f));
            Shadow(new Rect(r.x + 230, r.y + 2, 92, 20), RaidFrames.Clock(session.RaidBossEnrage), tiny, new Color(1, .84f, .45f));
            UnitBar(new Rect(r.x + 8, r.y + 24, r.width - 16, 16), a.Health.Pool.Ratio, HealthGreen, Mathf.RoundToInt(a.Health.Pool.Ratio * 100) + "%");
            if (b.Casting) UnitBar(new Rect(r.x + 8, r.y + 44, r.width - 16, 10), b.CastProgress, new Color(.95f, .7f, .2f), "");
            string pin = RaidFrames.PinText(session.RaidBossPinned);
            Shadow(new Rect(r.x + 8, r.y + 56, 160, 20), pin, tiny, session.RaidBossPinned > 0 ? new Color(.95f, .85f, .55f) : new Color(1, .45f, .4f));
            int phase = b.RaidBoss && b.RaidPhase > 0 ? b.RaidPhase : UnravellerRules.Phase(a.Health.Pool.Ratio, UnravellerTuning.Current);
            Shadow(new Rect(r.x + 170, r.y + 56, 150, 20), "Phase " + phase + (b.RaidBoss ? ": " + UnravellerRules.PhaseNames[phase] : ""), tiny, new Color(.85f, .85f, .85f));
            if (!b.RaidBoss) return;
            // Whose harpoon is next (yours in gold), and the hauling.
            var turn = session.HarpoonTurnLine;
            if (turn != null) Shadow(new Rect(r.x + 8, r.y + 76, 200, 20), turn, tiny, turn.StartsWith("Your") ? new Color(1, .84f, .3f) : new Color(.86f, .8f, .62f));
            if (b.Hauled) Shadow(new Rect(r.x + 210, r.y + 76, 110, 20), "Hauled", tiny, new Color(.95f, .75f, .4f));
            // The brood's kill order (raid 5): the Skull, then the held by their marks.
            var kill = session.BroodKillLine;
            if (kill != null) { Fill(new Rect(r.x, r.yMax, r.width, 22), new Color(0, 0, 0, .6f)); Shadow(new Rect(r.x + 8, r.yMax + 1, r.width - 16, 20), kill, tiny, new Color(1, .9f, .9f)); }
        }
        /// <summary>The raid warning (2.4): a large centred line for 2 s, for what is about to kill you.</summary>
        void DrawRaidWarning()
        {
            var w = session.RaidWarningText; if (w == null) return;
            var keep = GUI.matrix; At(null);
            var style = new GUIStyle(heading) { alignment = TextAnchor.MiddleCenter, fontSize = 30, wordWrap = true };
            Shadow(new Rect(220, 200, 1000, 50), w, style, new Color(1, .35f, .3f));
            GUI.matrix = keep;
        }

        /// <summary>The raid window (/raid): the roster by party, each one's part, class, level and gear level; your lockout; the call to arms,
        /// the ready check, arranging the parties, leaving. Out of a raid: Form raid.</summary>
        void DrawRaidWindow()
        {
            var raid = session.Raid;
            raidRect = new Rect(300, 110, 840, raid == null ? 170 : 500);
            Frame(raidRect);
            GUI.Label(new Rect(raidRect.x + 24, raidRect.y + 14, 500, 34), raid == null ? "RAID" : "RAID · " + raid.Count + " of " + RaidGroup.Size + (raid.Full ? "" : " · " + raid.NeedLine()), heading);
            if (GUI.Button(new Rect(raidRect.xMax - 44, raidRect.y + 12, 32, 28), "X", slim)) session.RaidOpen = false;
            if (raid == null)
            {
                Shadow(new Rect(raidRect.x + 24, raidRect.y + 60, 780, 22), "A raid is ten: two parties of five, 2 tanks, 3 healers and 5 damage. Your party becomes Party 1.", tiny, Color.white);
                if (GUI.Button(new Rect(raidRect.x + 24, raidRect.y + 100, 180, 34), "Form raid", button)) session.FormRaid();
                if (GUI.Button(new Rect(raidRect.x + 216, raidRect.y + 100, 220, 34), "Call to arms", button)) session.CallToArms();
                return;
            }
            var head = new Color(.8f, .8f, .78f);
            for (int p = 0; p < RaidGroup.Parties; p++)
            {
                float x = raidRect.x + 24 + p * 400, y = raidRect.y + 56;
                Shadow(new Rect(x, y, 200, 22), "Party " + (p + 1), tiny, new Color(1, .84f, .45f));
                foreach (var (dx, label) in new[] { (0f, "Name"), (170f, "Part"), (230f, "Level"), (280f, "Gear") }) Shadow(new Rect(x + dx, y + 22, 80, 20), label, tiny, head);
                y += 46;
                foreach (var m in raid.PartyMembers(p))
                {
                    Shadow(new Rect(x, y, 168, 22), (m.id == raid.LeaderId ? "* " : "") + m.name, tiny, ClassColour(m.classId));
                    Shadow(new Rect(x + 170, y, 60, 22), m.role.ToString(), tiny, RoleColour(m.role));
                    Shadow(new Rect(x + 230, y, 40, 22), m.level.ToString(), tiny, ConColor(m.level));
                    Shadow(new Rect(x + 280, y, 40, 22), m.gearLevel > 0 ? m.gearLevel.ToString("0.0") : "-", tiny, Color.white);
                    if (!m.player && GUI.Button(new Rect(x + 318, y, 30, 22), p == 0 ? ">" : "<", slim)) raid.Move(m.id, 1 - p);
                    if (!m.player && GUI.Button(new Rect(x + 350, y, 26, 22), "x", slim)) session.LeaveParty(m.id);
                    y += 28;
                }
            }
            float by = raidRect.y + 56 + 46 + 5 * 28 + 16;
            Shadow(new Rect(raidRect.x + 24, by, 790, 22), session.RaidLockoutLine(), tiny, new Color(.85f, .9f, .85f));
            by += 34;
            if (GUI.Button(new Rect(raidRect.x + 24, by, 180, 34), session.Calling ? "Calling..." : "Call to arms", button) && !session.Calling) session.CallToArms();
            if (GUI.Button(new Rect(raidRect.x + 214, by, 160, 34), "Ready check", button)) session.ReadyCheck();
            if (GUI.Button(new Rect(raidRect.x + 384, by, 200, 34), "Arrange parties", button)) session.RaidCommand("arrange");
            if (GUI.Button(new Rect(raidRect.x + 594, by, 160, 34), "Leave raid", button)) session.LeaveRaid();
            by += 46;
            int shown = 0;
            foreach (var a in session.LastCall)
            {
                if (shown >= 4) break;
                Shadow(new Rect(raidRect.x + 24, by, 790, 20), a.name + ": " + (a.comes ? (session.InParty(a.id) ? "with you" : "on the way") : "declined") + " (" + (a.source == RaidSource.Guild ? "guild" : a.source == RaidSource.Friend ? "friend" : "the zone") + ")", tiny, a.comes ? new Color(.55f, 1, .7f) : new Color(.8f, .8f, .78f));
                by += 20; shown++;
            }
        }
    }
}
