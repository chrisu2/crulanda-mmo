using System;
using System.Collections.Generic;

namespace Crulanda.Encounter
{
    // ============================================================================================
    // The raid group (WORLD_BOSS_1.md section 2.2, build plan step 1; 2026-10-19): ten in two parties of five, you and nine sims, for the
    // Lowtowns' world boss. Chris's answer 7: "2 tanks, 3 healers, 5 damage, in two parties of five" (the recommended answer stands).
    // The rules are here, pure over plain records so they are tested without a scene (RaidGroupTests); the session keeps one while you
    // lead a raid (EncounterSession.Raid) and the HUD draws it (EncounterHud.Raid). GAME-ONLY. Not saved: a raid is for the session.
    // ============================================================================================

    /// <summary>What a help reaches in a raid (section 2.2: "Party-wide heals and buffs reach your own party; raid-wide ones reach all ten").
    /// Out of a raid every reach is the whole party.</summary>
    public enum EffectReach { Single, Party, Raid }

    /// <summary>
    /// Which helps reach the raid and which the party (the tags of section 2.2, kept in one table so the kits and the session ask the same
    /// thing). A class's standing buff goes on all ten (it is laid once at a buff-up, as in the dungeons); a feast feeds the eater's party
    /// (five at a table, Consumables.PartySize); the heals that go out round the healer (a Cleric's mending round, a Salt-Mender's chant, an
    /// Archivist's kept vow, a Paladin's and Rogue's party heals, a sim's Salt Cairn) and the songs reach the healer's own party; a single
    /// heal reaches whoever it is put on, in either party; a circle laid on the ground reaches whoever stands in it.
    /// </summary>
    public static class RaidReach
    {
        public const string ClassBuff = "classbuff", Feast = "feast", GroupHeal = "groupheal", Song = "song", SingleHeal = "singleheal", GroundCircle = "groundcircle";
        public static EffectReach Of(string kind)
        {
            switch (kind)
            {
                case ClassBuff: return EffectReach.Raid;
                case Feast: case GroupHeal: case Song: return EffectReach.Party;
                case GroundCircle: return EffectReach.Raid;
                default: return EffectReach.Single;
            }
        }
    }

    public enum ReadyAnswer { Waiting, Ready, NotReady }

    /// <summary>One of the ten: you ("you") or a sim (its id), its class, level, part, party (0 or 1: Party 1 and Party 2) and gear level
    /// (the mean level of what it wears).</summary>
    [Serializable]
    public sealed class RaidMember
    {
        public string id, name, classId;
        public int level, party;
        public SimCompanion.PartyRole role;
        public float gearLevel;
        public bool player;
        public ReadyAnswer ready;
    }

    /// <summary>A raid of ten in two parties of five (section 2.2).</summary>
    public sealed class RaidGroup
    {
        public const int PartySize = 5, Parties = 2, Size = PartySize * Parties;
        /// <summary>Chris's answer 7: two tanks, three healers, five damage.</summary>
        public const int Tanks = 2, Healers = 3, Damage = 5;
        /// <summary>Sims of 18-20 only (section 2.2, the level gate).</summary>
        public const int MinSimLevel = 18;
        public const string You = "you";
        /// <summary>The boss the raid is for (the LFM line); the Unraveller (section 1.2).</summary>
        public string Boss = "Unraveller";
        /// <summary>The lockout key she will be locked by (step 3 gives her this camp key; Lockouts.KeyOf).</summary>
        public const string BossLockKey = "mob.worldboss.lowtowns.unraveller";
        public readonly List<RaidMember> Members = new List<RaidMember>();
        public string LeaderId = You;
        public int Count { get { return Members.Count; } }
        public bool Full { get { return Members.Count >= Size; } }

        public RaidMember Find(string id) { return Members.Find(m => m.id == id); }
        public bool Has(string id) { return Find(id) != null; }
        /// <summary>The party a member is in (0 for Party 1, 1 for Party 2), -1 for none.</summary>
        public int PartyOf(string id) { var m = Find(id); return m == null ? -1 : m.party; }
        public List<RaidMember> PartyMembers(int party) { return Members.FindAll(m => m.party == party); }
        public int RoleCount(SimCompanion.PartyRole role) { int n = 0; foreach (var m in Members) if (m.role == role) n++; return n; }
        public static int Wanted(SimCompanion.PartyRole role) { return role == SimCompanion.PartyRole.Tank ? Tanks : role == SimCompanion.PartyRole.Healer ? Healers : Damage; }
        public int Lacking(SimCompanion.PartyRole role) { return Math.Max(0, Wanted(role) - RoleCount(role)); }

        /// <summary>The parts a class can play, the one it plays first: a Warrior tanks; a Paladin tanks, else heals, else deals damage; a
        /// Druid, a Salt-Mender or a Shaman heals, else deals damage; a Cleric heals; the rest deal damage (SimCompanion.Role, BossBuffs.NominalRole).</summary>
        public static SimCompanion.PartyRole[] RolesFor(string classId)
        {
            switch (classId)
            {
                case "class.warrior": return new[] { SimCompanion.PartyRole.Tank };
                case "class.paladin": return new[] { SimCompanion.PartyRole.Tank, SimCompanion.PartyRole.Healer, SimCompanion.PartyRole.Damage };
                case "class.druid": case "class.saltmender": case "class.shaman": return new[] { SimCompanion.PartyRole.Healer, SimCompanion.PartyRole.Damage };
                case "class.cleric": return new[] { SimCompanion.PartyRole.Healer };
                default: return new[] { SimCompanion.PartyRole.Damage };
            }
        }
        /// <summary>The part a newcomer of this class would fill: the first of its parts the raid still lacks; null when it lacks none of them.</summary>
        public SimCompanion.PartyRole? RoleToFill(string classId)
        {
            foreach (var r in RolesFor(classId)) if (Lacking(r) > 0) return r;
            return null;
        }
        static string Plural(SimCompanion.PartyRole r) { return r == SimCompanion.PartyRole.Tank ? "tanks" : r == SimCompanion.PartyRole.Healer ? "healers" : "damage"; }

        /// <summary>
        /// "Form raid": your party becomes Party 1 (section 2.2), you its leader. Each takes the first part of its class the raid lacks, else
        /// its own (they were with you already). You play your class's first part.
        /// </summary>
        public static RaidGroup Form(RaidMember you, IEnumerable<RaidMember> party)
        {
            var g = new RaidGroup();
            you.id = You; you.player = true; you.party = 0; you.role = RolesFor(you.classId)[0]; you.ready = ReadyAnswer.Waiting;
            g.Members.Add(you); g.LeaderId = You;
            if (party != null)
                foreach (var m in party)
                {
                    if (m == null || g.Has(m.id) || g.PartyMembers(0).Count >= PartySize) continue;
                    m.party = 0; m.role = g.RoleToFill(m.classId) ?? RolesFor(m.classId)[0]; m.ready = ReadyAnswer.Waiting; g.Members.Add(m);
                }
            return g;
        }

        /// <summary>Why this one would not join (null: it will): already in, the raid full, a sim under 18, or (when <paramref name="roleGate"/>,
        /// as for those answering the call) none of its parts wanted.</summary>
        public string JoinRefusal(RaidMember m, bool roleGate = true)
        {
            if (m == null) return "Nobody to add.";
            if (Has(m.id)) return m.name + " is already in the raid.";
            if (Full) return "The raid is full.";
            if (!m.player && m.level < MinSimLevel) return m.name + " is too green for the raid (level " + MinSimLevel + " and over).";
            if (roleGate && RoleToFill(m.classId) == null) return "The raid has its " + Plural(RolesFor(m.classId)[0]) + " already.";
            return null;
        }
        /// <summary>A newcomer joins: new members fill Party 2 (section 2.2), Party 1 when Party 2 is full. Its part is the first of its class's
        /// the raid lacks, else its own.</summary>
        public bool Join(RaidMember m, bool roleGate = true)
        {
            if (JoinRefusal(m, roleGate) != null) return false;
            m.role = RoleToFill(m.classId) ?? RolesFor(m.classId)[0];
            m.party = PartyMembers(1).Count < PartySize ? 1 : 0; m.ready = ReadyAnswer.Waiting;
            Members.Add(m); return true;
        }
        /// <summary>A member leaves (or is removed); the lead comes back to you when its leader goes. False when it was not in the raid.</summary>
        public bool Leave(string id)
        {
            var m = Find(id); if (m == null) return false;
            Members.Remove(m);
            if (LeaderId == id) LeaderId = Has(You) ? You : (Members.Count > 0 ? Members[0].id : null);
            return true;
        }
        /// <summary>Moves a member to the other party (or the one named), when it has room.</summary>
        public bool Move(string id, int party)
        {
            var m = Find(id); if (m == null || party < 0 || party >= Parties || m.party == party) return false;
            if (PartyMembers(party).Count >= PartySize) return false;
            m.party = party; return true;
        }
        /// <summary>
        /// Sets the parties out (section 2.2, "a tank and one or two healers in each party"): you stay in Party 1; the tanks go one to each
        /// party, the healers to the party with fewer of them, the damage to fill. Each goes to the party with fewer of its part, then the one
        /// with fewer members, while it has room.
        /// </summary>
        public void Arrange()
        {
            var you = Find(You);
            var rest = Members.FindAll(m => m != you);
            var counts = new int[Parties, 3]; var size = new int[Parties];
            if (you != null) { you.party = 0; counts[0, (int)you.role]++; size[0]++; }
            foreach (var role in new[] { SimCompanion.PartyRole.Tank, SimCompanion.PartyRole.Healer, SimCompanion.PartyRole.Damage })
                foreach (var m in rest)
                {
                    if (m.role != role) continue;
                    int a = 0, b = 1, r = (int)role;
                    int pick = counts[a, r] != counts[b, r] ? (counts[a, r] < counts[b, r] ? a : b) : (size[a] <= size[b] ? a : b);
                    if (size[pick] >= PartySize) pick = 1 - pick;
                    m.party = pick; counts[pick, r]++; size[pick]++;
                }
        }
        /// <summary>Whether a help of this reach from one member gets to another: a party-wide one only within the caster's party; a raid-wide
        /// or a single one to anyone in the raid.</summary>
        public bool Reaches(EffectReach reach, string fromId, string toId)
        {
            int a = PartyOf(fromId), b = PartyOf(toId);
            if (a < 0 || b < 0) return false;
            return reach != EffectReach.Party || a == b;
        }
        /// <summary>What the raid still lacks, as the Zone chat says it: "need 1 tank 2 heals 4 dps" (section 2.3); "" when full.</summary>
        public string NeedLine()
        {
            if (Full) return "";
            int t = Lacking(SimCompanion.PartyRole.Tank), h = Lacking(SimCompanion.PartyRole.Healer), d = Lacking(SimCompanion.PartyRole.Damage);
            var parts = new List<string>();
            if (t > 0) parts.Add(t + (t == 1 ? " tank" : " tanks"));
            if (h > 0) parts.Add(h + (h == 1 ? " heal" : " heals"));
            if (d > 0) parts.Add(d + " dps");
            if (parts.Count == 0) parts.Add((Size - Count) + " more");
            return "need " + string.Join(" ", parts);
        }
        /// <summary>The call to arms' line: "LFM Unraveller, need 1 tank 2 heals 4 dps".</summary>
        public string LfmLine { get { return "LFM " + Boss + ", " + NeedLine(); } }

        // ---------- the ready check ----------
        public bool Checking { get; private set; }
        /// <summary>A ready check: the leader is ready, everyone else is asked.</summary>
        public void BeginReadyCheck()
        {
            Checking = true;
            foreach (var m in Members) m.ready = m.id == LeaderId ? ReadyAnswer.Ready : ReadyAnswer.Waiting;
        }
        public void Answer(string id, bool ready) { var m = Find(id); if (m != null && Checking) m.ready = ready ? ReadyAnswer.Ready : ReadyAnswer.NotReady; }
        public bool AllAnswered { get { foreach (var m in Members) if (m.ready == ReadyAnswer.Waiting) return false; return true; } }
        public int ReadyCount { get { int n = 0; foreach (var m in Members) if (m.ready == ReadyAnswer.Ready) n++; return n; } }
        /// <summary>The check ends (all answered, or its time is up): who did not answer counts as not ready. The names not ready.</summary>
        public List<string> EndReadyCheck()
        {
            Checking = false; var not = new List<string>();
            foreach (var m in Members) { if (m.ready == ReadyAnswer.Waiting) m.ready = ReadyAnswer.NotReady; if (m.ready == ReadyAnswer.NotReady) not.Add(m.name); }
            return not;
        }
    }

    /// <summary>The raid frames' and boss frame's words and layout (section 2.4), pure so they are tested without a scene.</summary>
    public static class RaidFrames
    {
        /// <summary>A raid frame's cell (canvas pixels at UiScale 1): two columns of five, one column a party.</summary>
        public const float Left = 20, Top = 200, CellW = 150, CellH = 42, Gap = 4;
        public static UnityEngine.Rect Cell(int party, int index) { return new UnityEngine.Rect(Left + party * (CellW + Gap), Top + index * (CellH + Gap), CellW, CellH); }
        public static string RoleMark(SimCompanion.PartyRole r) { return r == SimCompanion.PartyRole.Tank ? "T" : r == SimCompanion.PartyRole.Healer ? "H" : "D"; }
        /// <summary>The boss frame's pin state (section 2.4, "Pinned 8 s" or "Free"; the harpoons' timer arrives in step 4).</summary>
        public static string PinText(float pinnedSeconds) { return pinnedSeconds > 0 ? "Pinned " + (int)Math.Ceiling(pinnedSeconds) + " s" : "Free"; }
        /// <summary>The clock against the enrage: "7:05", or "" when there is none.</summary>
        public static string Clock(float seconds) { if (seconds <= 0) return ""; int s = (int)Math.Ceiling(seconds); return s / 60 + ":" + (s % 60).ToString("00"); }
        /// <summary>A gear level for the raid window: the mean of the worn pieces' levels, 0 for none.</summary>
        public static float GearLevel(IEnumerable<int> pieceLevels) { int n = 0; float sum = 0; if (pieceLevels != null) foreach (var l in pieceLevels) { sum += l; n++; } return n == 0 ? 0 : sum / n; }
    }
}
