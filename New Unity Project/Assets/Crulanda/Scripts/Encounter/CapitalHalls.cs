using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The people of the capital's halls (CAPITAL_HUB.md step 4; Chris, 2026-10-15: "i really want the castle done asap so double down
    /// on it"), by the role the zone gives a resident (ZoneResident.role), a kind and, after a colon, what it is of:
    /// - "councilor": High Councilor Aethelgard, an audience (<see cref="CouncilAudience"/>); never fought, never killed;
    /// - "council-clerk": the Council's errand board beside him (its errands are quests of kind "bounty", poster "council");
    /// - "archivist": the Archive's reading room (<see cref="ArchiveRoom"/>);
    /// - "guildmaster:&lt;guild slug&gt;" (guildmaster:lantern-watch): a guild's hall (<see cref="GuildHalls"/>);
    /// - "trainer" or "trainer:&lt;class id&gt;" (trainer:class.warrior): a talent reset for coin and the class's abilities by level
    ///   (<see cref="TrainerRules"/>); a bare "trainer" trains whoever speaks to them;
    /// - "dresser": the Armoury (the quest book's Armoury tab, the looks seen);
    /// - "profession-trainer:&lt;trade id&gt;" (profession-trainer:mining): that trade taken up, put aside or its tool bought there.
    /// Pure logic over the progress and the content, like DiscoveryLog, so it is tested without a scene (CapitalHallTests).
    /// All of it GAME-ONLY but Aethelgard (CANON) and the Council (CANON); each line says its own lore status.
    /// </summary>
    public static class HallRoles
    {
        public const string Councilor = "councilor", CouncilClerk = "council-clerk", Archivist = "archivist", Guildmaster = "guildmaster",
            Trainer = "trainer", Dresser = "dresser", ProfessionTrainer = "profession-trainer";
        static readonly string[] Kinds = { Councilor, CouncilClerk, Archivist, Guildmaster, Trainer, Dresser, ProfessionTrainer };
        /// <summary>"guildmaster:lantern-watch" to ("guildmaster", "lantern-watch"); a bare kind has an empty arg. False for any other role.</summary>
        public static bool Parse(string role, out string kind, out string arg)
        {
            kind = null; arg = "";
            if (string.IsNullOrEmpty(role)) return false;
            int colon = role.IndexOf(':');
            string k = colon < 0 ? role : role.Substring(0, colon);
            if (Array.IndexOf(Kinds, k) < 0) return false;
            kind = k; arg = colon < 0 ? "" : role.Substring(colon + 1).Trim();
            return true;
        }
        public static bool IsHall(string role) { return Parse(role, out _, out _); }
        /// <summary>The title under a hall person's name when the zone gives none (&lt;Archivist&gt;).</summary>
        public static string Title(string role)
        {
            if (!Parse(role, out var kind, out var arg)) return null;
            switch (kind)
            {
                case Councilor: return "High Councilor";
                case CouncilClerk: return "Clerk of the Council";
                case Archivist: return "Archivist";
                case Guildmaster: { var g = GuildHalls.FromSlug(arg); return g != null ? "Master of " + g : "Guild Master"; }
                case Trainer: return string.IsNullOrEmpty(arg) ? "Trainer" : SimRoster.ClassName(arg) + " Trainer";
                case Dresser: return "Dresser";
                case ProfessionTrainer: return string.IsNullOrEmpty(arg) ? "Trade Trainer" : char.ToUpperInvariant(arg[0]) + arg.Substring(1) + " Trainer";
                default: return null;
            }
        }
        /// <summary>
        /// Whom no blow can touch: the councilor (CAPITAL_HUB.md: "visited, never fought"). Every villager and resident is already out of
        /// the fight (no Actor, no Combatant, no collider: nothing to target, strike or hurt), and the session never lets a hall person be
        /// made a target; this names the rule so a later change that gives people bodies keeps him out of it.
        /// </summary>
        public static bool Untouchable(string role) { return Parse(role, out var kind, out _) && kind == Councilor; }
    }

    /// <summary>
    /// What the halls remember on the character, as keys in EncounterProgress.explored with a prefix of their own ("hall|copied|doc.x",
    /// "hall|told|secret.x", "hall|errand|&lt;quest&gt;|&lt;day&gt;", "hall|declined|&lt;quest&gt;|&lt;day&gt;", "hall|resets|3"), the
    /// way the book of lore keeps its own ("lore|..."): no place key can be one ("zone.id|name"), so the save format is unchanged and an
    /// older save simply has none.
    /// </summary>
    public static class HallMarks
    {
        public const string Prefix = "hall|";
        static List<string> List(EncounterProgress p) { if (p.explored == null) p.explored = new List<string>(); return p.explored; }
        public static bool Has(EncounterProgress p, string key) { return p != null && List(p).Contains(Prefix + key); }
        public static bool Add(EncounterProgress p, string key) { if (p == null || Has(p, key)) return false; List(p).Add(Prefix + key); return true; }
        /// <summary>Drops every mark that starts with this (an old day's errands).</summary>
        public static void Drop(EncounterProgress p, Predicate<string> which) { if (p != null) List(p).RemoveAll(k => k.StartsWith(Prefix, StringComparison.Ordinal) && which(k.Substring(Prefix.Length))); }
        public static int Number(EncounterProgress p, string name)
        {
            if (p == null) return 0; string head = Prefix + name + "|";
            foreach (var k in List(p)) if (k.StartsWith(head, StringComparison.Ordinal) && int.TryParse(k.Substring(head.Length), out int n)) return n;
            return 0;
        }
        public static void SetNumber(EncounterProgress p, string name, int n)
        {
            if (p == null) return; string head = Prefix + name + "|";
            List(p).RemoveAll(k => k.StartsWith(head, StringComparison.Ordinal)); List(p).Add(head + n);
        }
    }

    /// <summary>
    /// An audience with High Councilor Aethelgard (CANON: world_bible.md, "the face of the Council"; book1 ch.12:31, "The Greater Good";
    /// ch.12:365-393, his look and voice: ancient, pale skin pulled tight, white silk, a dry voice "like dead leaves", everything a
    /// calculation). The game is set before Book 1: nothing he says names what the books bring after. He answers by topic, and the line
    /// is the first of the topic's whose rule fits who you are: your level, what you have done (quests seen through, provinces reached,
    /// the Bureau's ledger read) and your standing with the Council (<see cref="Faction"/>).
    /// </summary>
    public static class CouncilAudience
    {
        public const string Name = "High Councilor Aethelgard";
        /// <summary>His clerk, who keeps the Council's errand board beside him (GAME-ONLY name).</summary>
        public const string Clerk = "Clerk Odo Vantry";
        /// <summary>The Council's standing in the Crown (halls.json): the court's regard, apart from the Concord's collectors, who hunt you whatever you do.</summary>
        public const string Faction = "council";
        public const string Greeting = "greeting", Who = "who", Good = "good", Grey = "grey", Bureau = "bureau", Deeds = "deeds", Errands = "errands", Farewell = "farewell";
        /// <summary>The topics in the window's order (the greeting is said on arrival), with what you ask.</summary>
        public static readonly (string topic, string ask)[] Topics = {
            (Who, "Who are you?"), (Good, "What is the Greater Good?"), (Grey, "What of the grey, the Wasting?"), (Bureau, "Why do the collectors take children?"),
            (Deeds, "What do you make of me?"), (Errands, "Has the Council work for me?"), (Farewell, "Take your leave"),
        };
        public const string VoiceLabel = "CANON-EXPANDED: Aethelgard's voice and \"The Greater Good\" (book1 ch.12:31, 365-393); the words GAME-ONLY";
        public const string FaceLabel = "CANON-EXPANDED: \"the face of the Council\" (world_bible.md, High Concord); the words GAME-ONLY";
        public const string BureauLabel = "CANON-EXPANDED: the Bureau's collections and the Oakhaven ledger (book1 ch.4); the words GAME-ONLY";
        /// <summary>The page that tells what the collectors are for (oakhaven.json): read it and he knows you have.</summary>
        public const string LedgerPage = "doc.oakhaven.ledger";

        /// <summary>Who you are to him.</summary>
        public struct Context { public int level, standing, questsDone, zonesReached; public bool readLedger; }
        /// <summary>A line, its lore status, and the rule's name (tests read it).</summary>
        public struct Line { public string text, label, rule; }

        /// <summary>Who you are, from the progress (standing from the quest log; the Council's start when there is none).</summary>
        public static Context Of(EncounterProgress p, QuestLog log, string hereZone = null)
        {
            var c = new Context { level = p != null ? p.Level : 1, standing = log != null ? log.Standing(Faction) : StartStanding };
            if (p == null) return c;
            c.questsDone = p.questsDone != null ? p.questsDone.Count : 0;
            c.readLedger = p.documents != null && p.documents.Contains(LedgerPage);
            c.zonesReached = ZonesReached(p, hereZone);
            return c;
        }
        /// <summary>The Council's standing a character starts with (halls.json): Neutral, a little in hand.</summary>
        public const int StartStanding = 200;
        /// <summary>The provinces a character has been to: those with a place explored, and the one they stand in.</summary>
        public static int ZonesReached(EncounterProgress p, string hereZone = null)
        {
            var zones = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(hereZone)) zones.Add(hereZone);
            if (p != null && p.explored != null)
                foreach (var k in p.explored)
                {
                    if (k == null || !k.StartsWith("zone.", StringComparison.Ordinal)) continue;
                    int bar = k.IndexOf('|'); if (bar > 0) zones.Add(k.Substring(0, bar));
                }
            return zones.Count;
        }

        /// <summary>What he says on a topic: the first rule of the topic that fits.</summary>
        public static Line Say(string topic, Context c)
        {
            switch (topic)
            {
                case Greeting:
                    if (c.standing < 0) return L("distrusted", "You turned the Council's work away, and still you come to its hall. Curious. Be brief.");
                    if (c.standing >= 3000) return L("honoured", "You have served the Greater Good without once asking to understand it. Few manage as much. Sit, if you wish.");
                    if (c.level >= 11) return L("high", "You have walked further than most of my wardens. The Council values what is useful, and you are becoming useful.");
                    if (c.standing >= 1000) return L("trusted", "My clerks speak well of you. That is rare. I do not say it lightly, and I will not say it twice.");
                    if (c.level >= 6) return L("mid", "I have heard your name twice this season. Once is noise. Twice is a pattern.");
                    return L("low", "A petitioner from the roads. The young are a variable, and I have little use for variables. Speak.");
                case Who:
                    return L("face", "Aethelgard. I am the face the Council shows the land. A face is useful: while they look at mine, they do not look at the rest of us.", FaceLabel);
                case Good:
                    if (c.standing < 0) return L("distrusted", "Something you have chosen not to serve. The sum goes on without you. It always does.");
                    if (c.level >= 11) return L("high", "You have seen enough to know the sum is never kind. It only has to be right.");
                    return L("low", "Order is a sum, not a feeling. A village lost, a hundred kept: the arithmetic does not ask how I sleep.");
                case Grey:
                    if (c.zonesReached >= 4) return L("travelled", "You have walked in the grey and come back. Then you know it does not argue. Neither should we.");
                    return L("untravelled", "The Wasting comes where it comes. The Council measures it, and acts. Measuring is more than the villages do.");
                case Bureau:
                    if (c.readLedger) return L("ledger", "You have read a ledger that was not yours. Figures are not cruelty, traveller. Figures are only honest.", BureauLabel);
                    return L("unread", "The Bureau gathers what the land would waste. A spark in a child is wasted behind a plough.", BureauLabel);
                case Deeds:
                    if (c.questsDone >= 20) return L("many", "You finish what you begin. That is the rarest thing in Crulanda, and the most useful. " + c.questsDone + " tasks, " + Provinces(c.zonesReached) + ". The clerks keep count. So do I.");
                    if (c.questsDone >= 5) return L("some", c.questsDone + " tasks seen through, " + Provinces(c.zonesReached) + ". Thin, still. But a ledger is read a line at a time.");
                    return L("few", "Your ledger is thin. I do not judge a thin ledger. I only read it.");
                case Errands:
                    if (c.standing < 0) return L("distrusted", "My clerk has work even for the unwilling: the tithes, at least. The Council forgets nothing, but it can be paid.");
                    return L("neutral", "My clerk keeps the Council's errands. Do them, and the Council remembers. Refuse them, and the Council also remembers.");
                case Farewell:
                    return L("farewell", "Go, then. The Greater Good does not wait on either of us.");
                default:
                    return L("none", "He does not answer that. He is already looking past you.");
            }
        }
        static string Provinces(int n) { return n <= 1 ? "in one province" : "in " + n + " provinces"; }
        static Line L(string rule, string text, string label = VoiceLabel) { return new Line { rule = rule, text = text, label = label }; }
    }

    /// <summary>
    /// The Council's errands (halls.json: quests of kind "bounty", poster "council", given and taken back by <see cref="CouncilAudience.Clerk"/>):
    /// repeatable, each once a game day; turning one in raises the Council's standing (its rewards), refusing one lowers it
    /// (<see cref="DeclineCost"/>, once a day an errand, so a second "no" the same day costs nothing more).
    /// </summary>
    public static class CouncilErrands
    {
        public const string Poster = "council";
        public const int DeclineCost = 50;
        public static bool IsErrand(QuestDef q) { return q != null && q.kind == "bounty" && q.poster == Poster; }
        static string DayKey(string what, string id, int day) { return what + "|" + id + "|" + day; }
        public static bool DoneToday(EncounterProgress p, string id) { return p != null && HallMarks.Has(p, DayKey("errand", id, p.days)); }
        /// <summary>Marks an errand handed in today, and forgets the marks of other days.</summary>
        public static void MarkDone(EncounterProgress p, string id)
        {
            if (p == null) return; int today = p.days;
            HallMarks.Drop(p, k => (k.StartsWith("errand|", StringComparison.Ordinal) || k.StartsWith("declined|", StringComparison.Ordinal)) && !k.EndsWith("|" + today, StringComparison.Ordinal));
            HallMarks.Add(p, DayKey("errand", id, today));
        }
        /// <summary>What a giver's list shows today: an errand handed in today is left off until tomorrow.</summary>
        public static void Filter(List<(QuestDef quest, QuestStatus status)> list, EncounterProgress p)
        {
            if (list == null || p == null) return;
            list.RemoveAll(e => IsErrand(e.quest) && e.status == QuestStatus.Available && DoneToday(p, e.quest.id));
        }
        /// <summary>Refused: the Council's standing goes down by <see cref="DeclineCost"/>, once a day for each errand. True when it went down.</summary>
        public static bool Declined(QuestLog log, QuestDef q)
        {
            if (log == null || !IsErrand(q) || log.Status(q, null) != QuestStatus.Available) return false;
            var p = log.Progress; if (!HallMarks.Add(p, DayKey("declined", q.id, p.days))) return false;
            log.ChangeStanding(CouncilAudience.Faction, -DeclineCost);
            return true;
        }
    }

    /// <summary>
    /// The Archive's reading room (CAPITAL_HUB.md: the High City's archives, CANON-EXPANDED from world_bible.md; the room and the
    /// archivist GAME-ONLY): a shelf for each zone with every page found there to read (the book of lore's pages and the Chronicle's
    /// letters and ledgers), how many are still lost there and how many hidden finds, and one clue (the Discoveries tab's own, of a find
    /// not yet found that can be found now). The archivist's trade: a page you have found lent to be copied, once each, buys a word of
    /// a find not yet found and not shut (DiscoveryLog.Unlocked), never one told before.
    /// </summary>
    public static class ArchiveRoom
    {
        public const string Elsewhere = "";
        public sealed class Shelf
        {
            public string zoneId, zoneName; public readonly List<DocumentDef> pages = new List<DocumentDef>();
            public int pagesLost, findsHidden; public string clue;
            /// <summary>What the archivist has told you of this zone's finds (the hints bought).</summary>
            public readonly List<string> told = new List<string>();
        }
        /// <summary>Where each page is to be had: the zone of the hidden find that holds it, else the zone of the quest that gives it.</summary>
        public static Dictionary<string, string> PageZones(QuestDatabase db, IEnumerable<ZoneDefinition> zones)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (zones != null)
                foreach (var z in zones)
                    if (z != null && z.secrets != null)
                        foreach (var s in z.secrets) if (s != null && !string.IsNullOrEmpty(s.document) && !map.ContainsKey(s.document)) map[s.document] = z.id;
            if (db != null)
                foreach (var q in db.Ordered)
                {
                    if (q == null) continue;
                    foreach (var d in q.rewards?.documents ?? new string[0]) if (!string.IsNullOrEmpty(d) && !map.ContainsKey(d)) map[d] = q.zone ?? Elsewhere;
                    foreach (var st in q.steps ?? new QuestStepDef[0])
                        foreach (var o in st?.objectives ?? new QuestObjectiveDef[0])
                            if (o != null && !string.IsNullOrEmpty(o.document) && !map.ContainsKey(o.document)) map[o.document] = !string.IsNullOrEmpty(o.zone) ? o.zone : q.zone ?? Elsewhere;
                }
            return map;
        }
        /// <summary>The shelves, in the zones' order, then "Elsewhere" for pages of no zone (when any are found or lost).</summary>
        public static List<Shelf> Shelves(QuestDatabase db, IList<ZoneDefinition> zones, EncounterProgress p, DiscoveryLog log)
        {
            var list = new List<Shelf>(); if (db == null || p == null) return list;
            var where = PageZones(db, zones);
            var byZone = new Dictionary<string, Shelf>(StringComparer.Ordinal);
            if (zones != null)
                foreach (var z in zones)
                {
                    if (z == null || byZone.ContainsKey(z.id)) continue;
                    ZoneLore.Apply(z);
                    var shelf = new Shelf { zoneId = z.id, zoneName = z.displayName }; byZone[z.id] = shelf; list.Add(shelf);
                    foreach (var s in z.secrets ?? new ZoneSecret[0])
                    {
                        if (s == null || string.IsNullOrEmpty(s.id)) continue;
                        bool found = log != null ? log.IsFound(s) : p.discoveries != null && p.discoveries.Contains(s.id);
                        if (found) continue;
                        shelf.findsHidden++;
                        bool open = log == null || log.Unlocked(s);
                        if (shelf.clue == null && open) shelf.clue = ZoneLore.Clue(s);
                        if (HallMarks.Has(p, "told|" + s.id)) { var h = ZoneLore.Hint(s); if (h != null) shelf.told.Add(h); }
                    }
                }
            var elsewhere = new Shelf { zoneId = Elsewhere, zoneName = "Elsewhere" };
            foreach (var d in db.Documents.Values)
            {
                if (d == null) continue;
                string zid = where.TryGetValue(d.id, out var z) ? z ?? Elsewhere : Elsewhere;
                var shelf = byZone.TryGetValue(zid, out var sh) ? sh : elsewhere;
                if (p.documents != null && p.documents.Contains(d.id)) shelf.pages.Add(d); else shelf.pagesLost++;
            }
            if (elsewhere.pages.Count > 0 || elsewhere.pagesLost > 0) list.Add(elsewhere);
            return list;
        }
        /// <summary>Whether a page can be lent to be copied: found, and not copied before.</summary>
        public static bool CanCopy(EncounterProgress p, string doc) { return p != null && !string.IsNullOrEmpty(doc) && p.documents != null && p.documents.Contains(doc) && !HallMarks.Has(p, "copied|" + doc); }
        /// <summary>The next find the archivist will tell of: in the zones' order, the first not found, not shut, not told, with a word to say.</summary>
        public static (ZoneDefinition zone, ZoneSecret secret) NextClue(IList<ZoneDefinition> zones, EncounterProgress p, DiscoveryLog log)
        {
            if (zones == null || p == null) return (null, null);
            foreach (var z in zones)
            {
                if (z == null || z.secrets == null) continue;
                ZoneLore.Apply(z);
                foreach (var s in z.secrets)
                {
                    if (s == null || string.IsNullOrEmpty(s.id) || HallMarks.Has(p, "told|" + s.id)) continue;
                    if (log != null ? log.IsFound(s) || !log.Unlocked(s) : p.discoveries != null && (p.discoveries.Contains(s.id) || (!string.IsNullOrEmpty(s.needs) && !p.discoveries.Contains(s.needs)))) continue;
                    if (ZoneLore.Hint(s) != null) return (z, s);
                }
            }
            return (null, null);
        }
        /// <summary>
        /// The trade: the page is copied (marked, once) and the next find told (marked). Returns the word ("In the Mastwood: ..."), or null
        /// with why: the page not found or copied already, or nothing left to tell (then the page is not taken).
        /// </summary>
        public static string Copy(EncounterProgress p, string doc, IList<ZoneDefinition> zones, DiscoveryLog log, out string why)
        {
            why = null;
            if (!CanCopy(p, doc)) { why = p != null && p.documents != null && p.documents.Contains(doc) ? CopiedLine : NotYoursLine; return null; }
            var (z, s) = NextClue(zones, p, log);
            if (s == null) { why = NothingLeftLine; return null; }
            HallMarks.Add(p, "copied|" + doc); HallMarks.Add(p, "told|" + s.id);
            return "In " + z.displayName + ": " + ZoneLore.Hint(s);
        }
        public const string CopiedLine = "\"We have a copy of that one already. Bring me another.\"";
        public const string NotYoursLine = "\"You cannot lend what you have not found.\"";
        public const string NothingLeftLine = "\"I have nothing left to tell you. You have found all I know of, or what is left is shut to you yet.\"";
    }

    /// <summary>
    /// The guild halls on the Guild Row (CAPITAL_HUB.md; the guilds of round 27, GAME-ONLY): each guild's master ("guildmaster:&lt;slug&gt;"),
    /// its hall's spots where its members idle when they are about (the zone's "hall.&lt;slug&gt;" spots), its roster and its notice board
    /// of asks: real quests (halls.json, kind "bounty", poster "guild:&lt;slug&gt;", given and taken back by <see cref="BoardName"/>).
    /// </summary>
    public static class GuildHalls
    {
        /// <summary>"The Lantern Watch" to "lantern-watch": lower case, words joined by '-', a leading "the" dropped.</summary>
        public static string Slug(string guild)
        {
            if (string.IsNullOrEmpty(guild)) return "";
            var s = guild.Trim().ToLowerInvariant(); if (s.StartsWith("the ", StringComparison.Ordinal)) s = s.Substring(4);
            var sb = new System.Text.StringBuilder(); bool dash = false;
            foreach (char ch in s) { if (char.IsLetterOrDigit(ch)) { if (dash && sb.Length > 0) sb.Append('-'); sb.Append(ch); dash = false; } else dash = true; }
            var slug = sb.ToString(); return slug.StartsWith("the-", StringComparison.Ordinal) ? slug.Substring(4) : slug;   // "the-lantern-watch" is the Lantern Watch too
        }
        /// <summary>The guild of a slug ("lantern-watch", "the-lantern-watch" or the name itself), or null.</summary>
        public static string FromSlug(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return null; string want = Slug(slug);
            foreach (var g in SimGuilds.All) if (Slug(g.name) == want) return g.name;
            return null;
        }
        /// <summary>The place name of a guild's hall spots: "hall.lantern-watch".</summary>
        public static string HallPlace(string guild) { return "hall." + Slug(guild); }
        /// <summary>The name its notice board goes by, the asks' giver and turn-in ("The Lantern Watch notice board").</summary>
        public static string BoardName(string guild) { return guild + " notice board"; }
        public static string PosterOf(string guild) { return "guild:" + Slug(guild); }
        public static bool IsAsk(QuestDef q) { return q != null && q.kind == "bounty" && q.poster != null && q.poster.StartsWith("guild:", StringComparison.Ordinal); }
        /// <summary>A member as the guild window shows them.</summary>
        public struct Member { public SimAdventurer sim; public bool online, master; }
        /// <summary>The roster: those about now (or in your party) first, then by level; the master (its highest level) marked.</summary>
        public static List<Member> Roster(WorldSave w, string guild, float hour, Func<string, bool> inParty = null)
        {
            var list = new List<Member>(); if (w == null || string.IsNullOrEmpty(guild)) return list;
            var lead = SimGuilds.Leader(w, guild);
            foreach (var s in SimGuilds.Members(w, guild)) list.Add(new Member { sim = s, online = s.IsOnlineAt(hour) || (inParty != null && inParty(s.id)), master = s == lead });
            list.Sort((a, b) => a.online != b.online ? (a.online ? -1 : 1) : a.sim.level != b.sim.level ? b.sim.level.CompareTo(a.sim.level) : string.CompareOrdinal(a.sim.name, b.sim.name));
            return list;
        }
        /// <summary>The asks of a guild's board, in the content's order.</summary>
        public static List<QuestDef> Asks(QuestDatabase db, string guild)
        {
            var list = new List<QuestDef>(); if (db == null || string.IsNullOrEmpty(guild)) return list;
            string poster = PosterOf(guild), board = BoardName(guild);
            foreach (var q in db.Ordered) if (IsAsk(q) && q.poster == poster && q.giver == board) list.Add(q);
            return list;
        }
    }

    /// <summary>
    /// The class trainers (CAPITAL_HUB.md, "class trainers (talent resets for coin)"; GAME-ONLY): every talent point back for coin, the
    /// price rising with each reset bought (<see cref="ResetPrice"/>), and the class's abilities by the level they come at.
    /// </summary>
    public static class TrainerRules
    {
        public const string ResetsMark = "resets";
        /// <summary>Resets bought so far (the price's step).</summary>
        public static int Resets(EncounterProgress p) { return HallMarks.Number(p, ResetsMark); }
        /// <summary>The price of the next reset: 10 crowns and 5 a level, times the resets bought plus one, at most eight times.</summary>
        public static int ResetPrice(int level, int resets) { return (10 + 5 * Math.Max(1, level)) * Math.Min(1 + Math.Max(0, resets), 8); }
        public static int ResetPrice(EncounterProgress p) { return ResetPrice(p != null ? p.Level : 1, Resets(p)); }
        public const string NothingLine = "You have spent no talent points. There is nothing to unlearn.";
        /// <summary>
        /// A reset bought: every point refunded and the price taken. Refused, with why and nothing changed, when no point is spent or the
        /// purse is short. The session clears the kit's state and saves.
        /// </summary>
        public static bool PaidReset(EncounterProgress p, out int price, out string why)
        {
            price = ResetPrice(p); why = null;
            if (p == null) { why = "No character."; return false; }
            if (TalentTree.Spent(p) <= 0) { why = NothingLine; return false; }
            if (p.gold < price) { why = "A reset costs " + price + " crowns. You have " + p.gold + "."; return false; }
            p.gold -= price; p.talents.Clear(); HallMarks.SetNumber(p, ResetsMark, Resets(p) + 1);
            return true;
        }
        /// <summary>A class's abilities as a trainer lists them: the level each comes at, its name, and the talent that grants it (or null).</summary>
        public static List<(int level, string name, string talent)> Abilities(ClassDefinition c, IEnumerable<Crulanda.Abilities.AbilityDefinition> catalog)
        {
            var list = new List<(int, string, string)>(); if (c == null || c.unlocks == null) return list;
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            if (catalog != null) foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !names.ContainsKey(a.id)) names[a.id] = a.name;
            foreach (var u in c.unlocks)
                if (u != null && !string.IsNullOrEmpty(u.abilityId))
                    list.Add((u.level, names.TryGetValue(u.abilityId, out var n) && !string.IsNullOrEmpty(n) ? n : u.abilityId, string.IsNullOrEmpty(u.talentId) ? null : u.talentId));
            list.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            return list;
        }
    }
}
