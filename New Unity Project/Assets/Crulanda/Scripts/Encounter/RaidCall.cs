using System;
using System.Collections.Generic;

namespace Crulanda.Encounter
{
    /// <summary>Where an answer to the call to arms came from (section 2.3: your guild first, then the zone's sims, then friends anywhere).</summary>
    public enum RaidSource { Guild, Zone, Friend }

    /// <summary>One adventurer as the call sees it: who, its class and level, where it is, its guild, its nerve (bold), whether it is online,
    /// on your friends list (or a friend by regard), a rival, or with you already.</summary>
    public sealed class RaidCandidate
    {
        public string id, name, classId, zone, guild;
        public int level;
        public float bold, chatty;
        public bool online, friend, rival, withYou;
        public static RaidCandidate Of(SimAdventurer s, float hour, bool withYou)
        {
            return new RaidCandidate { id = s.id, name = s.name, classId = s.classId, zone = s.zone, guild = s.guild ?? "", level = s.level, bold = s.bold, chatty = s.chatty,
                online = s.IsOnlineAt(hour), friend = s.friend || SimMemory.Of(s) == SimMemory.Standing.Friend, rival = SimMemory.Refuses(s), withYou = withYou };
        }
    }

    /// <summary>One answer: who, from where, the part it comes for, after how many seconds, whether it comes, and what it says.</summary>
    public sealed class RaidAnswer
    {
        public string id, name, classId;
        public RaidSource source;
        public SimCompanion.PartyRole role;
        public float after;
        public bool comes, travels;
        public string line;
    }

    /// <summary>
    /// The call to arms (WORLD_BOSS_1.md section 2.3, build plan step 2; 2026-10-19): you post in Zone chat ("LFM Unraveller, need 1 tank 2
    /// heals 4 dps", RaidGroup.LfmLine) and the answers come over 20-60 s by part: (1) your guild first (online, 18+, from anywhere: those away
    /// travel a minute or two); (2) the zone's own sims, by nerve, level and part (one with bold under .3 declines with a line); (3) friends
    /// anywhere (18+). The queue's summoning is not used: the zone is seen to rally, and an empty zone at 3 am is part of it. Pure over a
    /// roster so it is tested without a scene (RaidCallTests); the session posts the lines and brings them (EncounterSession.Raid). GAME-ONLY.
    /// </summary>
    public static class RaidCall
    {
        public const float BoldBelow = .3f;
        /// <summary>An answer from someone in the zone: 20-60 s; one from another zone travels: 60-120 s (section 2.3).</summary>
        public const float AnswerFrom = 20, AnswerTo = 60, TravelFrom = 60, TravelTo = 120;
        /// <summary>What a sim answering buys on the way (section 2.3): three twists of salt and three bone harpoons (Tibb Marrow's and Ketta
        /// Rune's, EncounterContent/Items/lowtowns.json).</summary>
        public static readonly (string item, int count)[] Kit = { ("throw.salt_twist", 3), ("throw.bone_harpoon", 3) };

        /// <summary>
        /// Who answers the call, in order of arrival: the guild (online, 18+, any zone), then those in <paramref name="zone"/> (online, 18+; bold
        /// under <see cref="BoldBelow"/> declines with a line), then friends anywhere (online, 18+). Each comes for the first of its class's parts
        /// the raid still lacks (a part already filled turns it away unsaid); rivals and those with you already are passed over. No more come than
        /// the raid has places. Fixed for a seed.
        /// </summary>
        public static List<RaidAnswer> Answers(RaidGroup raid, IList<RaidCandidate> roster, string zone, string yourGuild, int seed)
        {
            var answers = new List<RaidAnswer>(); if (raid == null || roster == null) return answers;
            var rng = new Random(seed);
            var lack = new Dictionary<SimCompanion.PartyRole, int>();
            foreach (SimCompanion.PartyRole r in Enum.GetValues(typeof(SimCompanion.PartyRole))) lack[r] = raid.Lacking(r);
            int places = RaidGroup.Size - raid.Count;
            var asked = new HashSet<string>();
            SimCompanion.PartyRole? Fill(string classId) { foreach (var r in RaidGroup.RolesFor(classId)) if (lack[r] > 0) return r; return null; }
            float Delay(bool travels) { double t = rng.NextDouble(); return travels ? (float)(TravelFrom + t * (TravelTo - TravelFrom)) : (float)(AnswerFrom + t * (AnswerTo - AnswerFrom)); }
            bool Able(RaidCandidate c) { return c != null && c.online && !c.withYou && !c.rival && !raid.Has(c.id) && c.level >= RaidGroup.MinSimLevel && !asked.Contains(c.id); }
            void Take(RaidCandidate c, RaidSource src)
            {
                if (places <= 0) return;
                var role = Fill(c.classId); if (role == null) return;
                bool travels = c.zone != zone;
                if (src == RaidSource.Zone && c.bold < BoldBelow)
                {
                    asked.Add(c.id);
                    answers.Add(new RaidAnswer { id = c.id, name = c.name, classId = c.classId, source = src, role = role.Value, after = Delay(false), comes = false,
                        line = c.chatty > .65f ? "nah, not her, she ate my mate last time" : "Not me. I've seen what comes up out of that river." });
                    return;
                }
                asked.Add(c.id); lack[role.Value]--; places--;
                answers.Add(new RaidAnswer { id = c.id, name = c.name, classId = c.classId, source = src, role = role.Value, after = Delay(travels), comes = true, travels = travels,
                    line = Line(src, role.Value, travels, c.chatty) });
            }
            // (1) The guild, the highest level first.
            if (!string.IsNullOrEmpty(yourGuild))
                foreach (var c in Ordered(roster)) if (Able(c) && c.guild == yourGuild) Take(c, RaidSource.Guild);
            // (2) The zone's own, the boldest first.
            var here = new List<RaidCandidate>(); foreach (var c in roster) if (Able(c) && c.zone == zone) here.Add(c);
            here.Sort((a, b) => b.bold != a.bold ? b.bold.CompareTo(a.bold) : string.CompareOrdinal(a.id, b.id));
            foreach (var c in here) Take(c, RaidSource.Zone);
            // (3) Friends anywhere.
            foreach (var c in Ordered(roster)) if (Able(c) && c.friend) Take(c, RaidSource.Friend);
            answers.Sort((a, b) => a.after.CompareTo(b.after));
            return answers;
        }
        static List<RaidCandidate> Ordered(IList<RaidCandidate> roster)
        {
            var l = new List<RaidCandidate>(roster); l.RemoveAll(c => c == null);
            l.Sort((a, b) => b.level != a.level ? b.level.CompareTo(a.level) : string.CompareOrdinal(a.id, b.id)); return l;
        }
        static string Line(RaidSource src, SimCompanion.PartyRole role, bool travels, float chatty)
        {
            string part = role == SimCompanion.PartyRole.Tank ? "tank" : role == SimCompanion.PartyRole.Healer ? "heals" : "dps";
            if (src == RaidSource.Guild) return travels ? (chatty > .65f ? "omw, " + part + ", give me a couple of minutes" : "Coming from the road. I'll " + (role == SimCompanion.PartyRole.Damage ? "bring my blade" : "take " + part) + ".") : "here, " + part;
            if (src == RaidSource.Friend) return travels ? "for you? on my way, " + part : "I'm in. " + part;
            return chatty > .65f ? part + " here, inv" : "I'll come. Put me down for " + part + ".";
        }
        /// <summary>How many of the answers come.</summary>
        public static int Comers(List<RaidAnswer> answers) { int n = 0; if (answers != null) foreach (var a in answers) if (a.comes) n++; return n; }
        /// <summary>The call falls short: fewer come than the raid has places.</summary>
        public static bool Short(RaidGroup raid, List<RaidAnswer> answers) { return raid != null && Comers(answers) < RaidGroup.Size - raid.Count; }
        /// <summary>The empty zone at 3 am (section 2.3): "Not enough of the Lowtowns' adventurers are about. Try your guild, or wait for the evening."</summary>
        public static string EmptyZoneLine(string zoneName)
        {
            string n = string.IsNullOrEmpty(zoneName) ? "this place" : zoneName;
            if (n.StartsWith("The ", StringComparison.Ordinal)) n = "the " + n.Substring(4);
            return "Not enough of " + n + (n.EndsWith("s") ? "'" : "'s") + " adventurers are about. Try your guild, or wait for the evening.";
        }
        /// <summary>A sim answering buys its salt and harpoons (section 2.3): what it lacks of <see cref="Kit"/>, while its coin lasts. The coin spent.</summary>
        public static int BuyKit(SimAdventurer s, Func<string, int> price)
        {
            if (s == null) return 0; int spent = 0;
            foreach (var (item, count) in Kit)
            {
                int want = count - SimEconomy.Count(s, item); if (want <= 0) continue;
                int each = Math.Max(1, price != null ? price(item) : 1);
                int can = Math.Min(want, each > 0 ? s.coin / each : want); if (can <= 0) continue;
                s.coin -= can * each; spent += can * each; SimEconomy.Add(s, item, can);
            }
            return spent;
        }
    }
}
