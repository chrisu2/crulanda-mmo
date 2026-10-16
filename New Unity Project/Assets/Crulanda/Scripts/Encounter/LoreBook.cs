using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A zone's chapter of the book of lore (EncounterContent/Lore/&lt;zone&gt;.json; Phase 8.6 step 7): the lines written for the
    /// zone's places, people, creatures, herbs and ores, each with its canonStatus. See <see cref="LoreEntry"/>.
    /// </summary>
    [Serializable] public sealed class LoreFile { public string zone, canonStatus; public LoreEntry[] entries = new LoreEntry[0]; }
    /// <summary>
    /// One line of the book of lore about one thing in its zone. chapter: places, people, creatures or herbs (Herbs and Ores).
    /// subject: what it is about, as the game names it: a landmark's name, a person's name, a camp's mob name ("Grey wolf"), a node
    /// kind ("node.yarrow") or a rare herb's item ("herb.moonbell"). line: the plain half, known by doing (reaching the place,
    /// speaking to the person, killing one, working one); empty, the game's own line stands (the landmark's text, the person's
    /// trade). more: the better half, known when its page is found (page: a Chronicle page's id, a read shelf's, a remains' or a
    /// note's) or its hidden find is found (secret: a carving's or a grave's id; 2026-10-15, decision 6/7). canonStatus as
    /// everywhere: CANON only with a book and chapter cited; CANON-EXPANDED round canon places and factions; GAME-ONLY for the
    /// villages' people, the metals and the herbs; PROVISIONAL for what foreshadows the books (the game is set before them).
    /// </summary>
    [Serializable] public sealed class LoreEntry { public string id, chapter, subject, line, more, page, secret, canonStatus; }

    /// <summary>
    /// The book of lore (the quest book's Lore tab; Phase 8.6 step 7; Chris, 2026-10-14 23:40: "find recipes/ book of lore/ still
    /// havent found many secrets. all this for ALL Zones"; decision 8, 2026-10-15: "the Lore tab in the quest book"). Its chapters:
    /// Places (every landmark, known when reached, from the zone's canonNote and a line each), People (every named villager and
    /// resident spoken to, with the stories they tell, and the adventurers met), Creatures (each kind killed: where it lives and what
    /// it drops), Herbs and Ores (each node kind worked, and the rare herbs found: where they grow, what they make), Factions (the
    /// Standing tab's words), Pages (the documents found) and Leaves (the recipes found: the found-recipe work's, a hook here).
    /// Doing fills the plain half of a line; the better half waits on a page or a hidden find in the world.
    /// The save: what is known by doing is recorded in EncounterProgress.explored, the list of places explored, with keys of its
    /// own ("lore|met|zone.khaven|Wenna Coyle", "lore|sim|&lt;id&gt;", "lore|slain|zone.oakhaven|Grey wolf", "lore|worked|zone.peaks|node.tarnwort"),
    /// which no place key can be ("zone.id|name"): an existing list, so the save format (10) is unchanged and an older save simply
    /// knows nothing yet. Places come from the explored places themselves, pages from Progress.documents, finds from
    /// Progress.discoveries. Pure logic over the progress and the content, like DiscoveryLog, so it is tested without a scene
    /// (LoreBookTests).
    /// </summary>
    public sealed class LoreBook
    {
        public const string Places = "places", People = "people", Creatures = "creatures", Herbs = "herbs", Factions = "factions", Pages = "pages", Leaves = "leaves";
        /// <summary>The chapters in the book's order.</summary>
        public static readonly string[] Chapters = { Places, People, Creatures, Herbs, Factions, Pages, Leaves };
        /// <summary>The chapters a zone fills (the rest are the world's): what completes a zone's chapter of the book.</summary>
        public static readonly string[] ZoneChapters = { Places, People, Creatures, Herbs };
        /// <summary>The four lore labels (CRULANDA_CANON.md).</summary>
        public static readonly string[] Labels = { "CANON", "CANON-EXPANDED", "GAME-ONLY", "PROVISIONAL" };
        /// <summary>The key prefix in EncounterProgress.explored for what the book knows by doing.</summary>
        public const string KeyPrefix = "lore|";
        public static string MetKey(string zoneId, string name) { return KeyPrefix + "met|" + zoneId + "|" + name; }
        public static string SimKey(string simId) { return KeyPrefix + "sim|" + simId; }
        public static string SlainKey(string zoneId, string mob) { return KeyPrefix + "slain|" + zoneId + "|" + mob; }
        public static string WorkedKey(string zoneId, string subject) { return KeyPrefix + "worked|" + zoneId + "|" + subject; }
        public static string ChapterName(string chapter)
        {
            switch (chapter)
            {
                case Places: return "Places"; case People: return "People"; case Creatures: return "Creatures"; case Herbs: return "Herbs and Ores";
                case Factions: return "Factions"; case Pages: return "Pages"; case Leaves: return "Leaves";
                default: return chapter;
            }
        }

        /// <summary>The lore files by zone id.</summary>
        public readonly Dictionary<string, LoreFile> Files = new Dictionary<string, LoreFile>(StringComparer.Ordinal);
        public EncounterProgress Progress { get; private set; }
        /// <summary>The world's zones (the session's AllZones), the quest content (pages, factions), the trades (nodes, recipes), the
        /// loot (drops) and the items (names). Any may be null: what it would add is left out.</summary>
        public List<ZoneDefinition> Zones = new List<ZoneDefinition>();
        public QuestDatabase Quests; public ProfessionDatabase Trades; public LootDatabase Loot; public ItemDatabase Items;
        /// <summary>A living villager's trade title by name ("Blacksmith"), from the scene (VillageLife); null outside play.</summary>
        public Func<string, string> TitleOf;

        // ---------- loading ----------
        /// <summary>Reads the lore files; an unreadable one, or one naming no zone, is left out with a warning (the book is never worth a
        /// broken session). A zone's second file adds its entries to the first's.</summary>
        public static LoreBook Parse(IEnumerable<string> jsonFiles)
        {
            var book = new LoreBook();
            foreach (var json in jsonFiles ?? new string[0])
            {
                var f = ParseFile(json); if (f == null) continue;
                if (book.Files.TryGetValue(f.zone, out var had))
                {
                    var all = new List<LoreEntry>(had.entries ?? new LoreEntry[0]); all.AddRange(f.entries ?? new LoreEntry[0]); had.entries = all.ToArray();
                }
                else book.Files[f.zone] = f;
            }
            return book;
        }
        public static LoreFile ParseFile(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            LoreFile f;
            try { f = JsonUtility.FromJson<LoreFile>(json); } catch (Exception e) { Debug.LogWarning("Unreadable lore file: " + e.Message); return null; }
            if (f == null || string.IsNullOrEmpty(f.zone)) { Debug.LogWarning("A lore file names no zone."); return null; }
            if (f.entries == null) f.entries = new LoreEntry[0];
            return f;
        }
        public LoreBook Bind(EncounterProgress progress)
        {
            Progress = progress;
            if (Progress != null && Progress.explored == null) Progress.explored = new List<string>();
            return this;
        }
        public LoreFile FileOf(string zoneId) { return zoneId != null && Files.TryGetValue(zoneId, out var f) ? f : null; }
        /// <summary>The zone's entry for a subject in a chapter, or null.</summary>
        public LoreEntry Entry(string zoneId, string chapter, string subject)
        {
            var f = FileOf(zoneId); if (f == null || subject == null) return null;
            foreach (var e in f.entries) if (e != null && e.chapter == chapter && e.subject == subject) return e;
            return null;
        }
        public ZoneDefinition Zone(string zoneId) { return Zones.Find(z => z != null && z.id == zoneId); }

        // ---------- what doing records (EncounterProgress.explored) ----------
        public bool Knows(string key) { return Progress != null && Progress.explored != null && Progress.explored.Contains(key); }
        bool Record(string key) { if (Progress == null || Knows(key)) return false; Progress.explored.Add(key); return true; }
        /// <summary>Spoken to one of the zone's named people (true the first time).</summary>
        public bool Meet(ZoneDefinition z, string name) { return z != null && !string.IsNullOrEmpty(name) && PeopleOf(z).Contains(name) && Record(MetKey(z.id, name)); }
        /// <summary>Spoken to an adventurer (a sim, by its id).</summary>
        public bool MeetSim(string simId) { return !string.IsNullOrEmpty(simId) && Record(SimKey(simId)); }
        /// <summary>Killed one of the zone's camp mobs, by its camp's mob name (true for the first of its kind).</summary>
        public bool Slay(ZoneDefinition z, string mob) { return z != null && !string.IsNullOrEmpty(mob) && CreaturesOf(z).Contains(mob) && Record(SlainKey(z.id, mob)); }
        /// <summary>Worked one of the zone's node kinds, or picked one of its rare herbs (by its item id).</summary>
        public bool Work(ZoneDefinition z, string subject) { return z != null && !string.IsNullOrEmpty(subject) && HerbsOf(z).Contains(subject) && Record(WorkedKey(z.id, subject)); }

        // ---------- who and what each chapter lists ----------
        /// <summary>The zone's named people: its villagers (its own names, as many as it spawns), its residents, a hen-wife for each coop
        /// and a shepherd for each fold, as ZoneLoreTests counts them.</summary>
        public static List<string> PeopleOf(ZoneDefinition z)
        {
            var list = new List<string>(); if (z == null || z.life == null) return list;
            void Add(string n) { if (!string.IsNullOrEmpty(n) && !list.Contains(n)) list.Add(n); }
            var names = VillageLife.VillagerNames(z.life);
            for (int i = 0; i < z.life.villagers && i < names.Length; i++) Add(names[i]);
            foreach (var r in z.life.residents ?? new ZoneResident[0]) if (r != null) Add(r.name);
            int coops = 0, folds = 0;
            foreach (var p in z.props ?? new ZoneProp[0]) if (p != null && p.kind == "coop") coops++;
            foreach (var c in z.life.critters ?? new ZoneCritters[0]) if (c != null && c.kind == "sheep") folds++;
            for (int i = 0; i < coops && i < VillageLife.KeeperNames.Length; i++) Add(VillageLife.KeeperNames[i]);
            for (int i = 0; i < folds && i < VillageLife.ShepherdNames.Length; i++) Add(VillageLife.ShepherdNames[i]);
            return list;
        }
        /// <summary>The zone's creatures by camp mob name, in camp order, each once. A rare camp's (the Quiet Miner, one visit in five) are
        /// left out unless asked for: no chapter waits on one.</summary>
        public static List<string> CreaturesOf(ZoneDefinition z, bool rare = false)
        {
            var list = new List<string>(); if (z == null) return list;
            foreach (var c in z.camps ?? new ZoneCamp[0]) if (c != null && !string.IsNullOrEmpty(c.mob) && (rare || c.rare <= 0) && !list.Contains(c.mob)) list.Add(c.mob);
            return list;
        }
        /// <summary>The zone's herbs and ores: its node kinds (the nodes, then the props worked as nodes), each once, then the rare herbs
        /// its hidden finds hold, by item id.</summary>
        public static List<string> HerbsOf(ZoneDefinition z)
        {
            var list = new List<string>(); if (z == null) return list;
            foreach (var n in z.nodes ?? new ZoneNode[0]) if (n != null && !string.IsNullOrEmpty(n.node) && !list.Contains(n.node)) list.Add(n.node);
            foreach (var p in z.props ?? new ZoneProp[0]) if (p != null && !string.IsNullOrEmpty(p.node) && !list.Contains(p.node)) list.Add(p.node);
            foreach (var s in z.secrets ?? new ZoneSecret[0]) if (s != null && s.kind == "herb" && !string.IsNullOrEmpty(s.item) && !list.Contains(s.item)) list.Add(s.item);
            return list;
        }
        static bool IsRareHerb(string subject) { return subject != null && !subject.StartsWith("node.", StringComparison.Ordinal); }

        // ---------- a chapter's lines ----------
        /// <summary>
        /// One line of a chapter as the book shows it. known: the plain half is known (by doing; a faction's always). hasMore: there
        /// is a better half; moreKnown: it is (its page or find is found). full: everything there is to know of it is known.
        /// moreHint: where the better half lies, told without naming the spot ("Something written in Khaven Village tells more.").
        /// notes: what the game knows of it (a person's stories, a creature's haunts and drops, a herb's ground and what it makes).
        /// </summary>
        public sealed class Line
        {
            public string zoneId, chapter, subject, title, plain, more, moreHint, canonStatus;
            public bool known, hasMore, moreKnown;
            public bool Full { get { return known && (!hasMore || moreKnown); } }
            /// <summary>Shown in the book: the plain half is known, or the better half was found first.</summary>
            public bool Shown { get { return known || moreKnown; } }
            public readonly List<string> notes = new List<string>();
        }
        /// <summary>Every line of one of a zone's chapters, known or not, in the zone's order. notes: false leaves the notes out (counting).</summary>
        public List<Line> Lines(ZoneDefinition z, string chapter, bool notes = true)
        {
            var list = new List<Line>(); if (z == null) return list;
            switch (chapter)
            {
                case Places:
                    foreach (var l in Achievements.Pois(z))
                    {
                        var e = Entry(z.id, Places, l.name);
                        var line = Make(z, Places, l.name, l.name, e, !string.IsNullOrEmpty(e?.line) ? e.line : l.text,
                            !string.IsNullOrEmpty(l.canonStatus) ? l.canonStatus : z.canonStatus, notes);
                        line.known = Progress != null && Progress.explored.Contains(Achievements.PoiKey(z, l));
                        list.Add(line);
                    }
                    break;
                case People:
                    if (notes) ZoneLore.Apply(z);
                    foreach (var name in PeopleOf(z))
                    {
                        var e = Entry(z.id, People, name);
                        var line = Make(z, People, name, name, e, !string.IsNullOrEmpty(e?.line) ? e.line : Who(z, name), "GAME-ONLY: the village's people", notes);
                        line.known = Knows(MetKey(z.id, name));
                        if (notes && line.known) { var told = ZoneLore.StoriesOf(z, name); if (told != null) foreach (var t in told) if (!string.IsNullOrWhiteSpace(t)) line.notes.Add("“" + t + "”"); }
                        list.Add(line);
                    }
                    break;
                case Creatures:
                    foreach (var mob in CreaturesOf(z))
                    {
                        var e = Entry(z.id, Creatures, mob);
                        var line = Make(z, Creatures, mob, mob, e, e?.line ?? "", "GAME-ONLY: the zone's camps", notes);
                        line.known = Knows(SlainKey(z.id, mob));
                        if (notes) CreatureNotes(z, mob, line.notes);
                        list.Add(line);
                    }
                    break;
                case Herbs:
                    foreach (var subject in HerbsOf(z))
                    {
                        var e = Entry(z.id, Herbs, subject); bool rare = IsRareHerb(subject);
                        var node = rare ? null : Trades?.Node(subject); var item = Items?.Get(rare ? subject : node?.item);
                        string title = node != null ? node.name : item != null ? item.name : subject;
                        var line = Make(z, Herbs, subject, title, e, e?.line ?? "", "GAME-ONLY: the trades' herbs and metals", notes);
                        line.known = Knows(WorkedKey(z.id, subject)) || (rare && RareHerbFound(z, subject));
                        if (notes) HerbNotes(z, subject, node, item, line.notes);
                        list.Add(line);
                    }
                    break;
            }
            return list;
        }
        Line Make(ZoneDefinition z, string chapter, string subject, string title, LoreEntry e, string plain, string label, bool hint = true)
        {
            var line = new Line { zoneId = z.id, chapter = chapter, subject = subject, title = title, plain = plain ?? "", canonStatus = !string.IsNullOrEmpty(e?.canonStatus) ? e.canonStatus : label };
            if (e != null && !string.IsNullOrEmpty(e.more) && (!string.IsNullOrEmpty(e.page) || !string.IsNullOrEmpty(e.secret)))
            {
                line.hasMore = true; line.more = e.more;
                line.moreKnown = Progress != null && ((!string.IsNullOrEmpty(e.page) && Progress.documents != null && Progress.documents.Contains(e.page)) ||
                    (!string.IsNullOrEmpty(e.secret) && Progress.discoveries != null && Progress.discoveries.Contains(e.secret)));
                if (hint) line.moreHint = MoreHint(e, z);
            }
            return line;
        }
        /// <summary>Where a better half lies, told by zone only: a page in a hidden find there, a find there, or a page still to come.</summary>
        string MoreHint(LoreEntry e, ZoneDefinition home)
        {
            foreach (var z in Zones)
                foreach (var s in z?.secrets ?? new ZoneSecret[0])
                    if (s != null && ((!string.IsNullOrEmpty(e.secret) && s.id == e.secret) || (!string.IsNullOrEmpty(e.page) && s.document == e.page)))
                        return (string.IsNullOrEmpty(e.secret) ? "Something written down in " : "Something hidden in ") + z.displayName + " tells more.";
            return string.IsNullOrEmpty(e.page) ? "Something hidden tells more." : "A page you have not found yet tells more" + (home != null ? " of " + home.displayName : "") + ".";
        }
        /// <summary>A person's plain line when the lore file gives none: a resident's title, else their household ("Of the Harrow farm,
        /// its head"), else their trade as the village has them now, else where they live.</summary>
        string Who(ZoneDefinition z, string name)
        {
            foreach (var r in z.life?.residents ?? new ZoneResident[0]) if (r != null && r.name == name && !string.IsNullOrEmpty(r.title)) return r.title + ", " + Short(z) + ".";
            if (Array.IndexOf(VillageLife.KeeperNames, name) >= 0) return "A hen-wife of " + Short(z) + ".";
            if (Array.IndexOf(VillageLife.ShepherdNames, name) >= 0) return "A shepherd of " + Short(z) + ".";
            string trade = TitleOf?.Invoke(name);
            foreach (var h in z.life?.households ?? new ZoneHousehold[0])
                foreach (var m in h?.members ?? new ZoneMember[0])
                    if (m != null && m.name == name)
                        return (string.IsNullOrEmpty(trade) ? "" : trade + ". ") + "Of the " + h.name + (h.name.EndsWith("farm") || h.name.StartsWith("The ") ? "" : " household") +
                            (string.IsNullOrEmpty(m.kin) ? "" : ", " + (m.kin == "head" ? "its head" : "the " + m.kin)) + ".";
            return string.IsNullOrEmpty(trade) ? "Of " + Short(z) + "." : trade + " of " + Short(z) + ".";
        }
        /// <summary>"Oakhaven", "Khaven", "the Shattered Peaks": a zone's name inside a sentence.</summary>
        static string Short(ZoneDefinition z)
        {
            string n = z.displayName ?? z.id; if (n.EndsWith(" Village")) n = n.Substring(0, n.Length - 8);
            return n.StartsWith("The ") ? "the " + n.Substring(4) : n;
        }
        /// <summary>Where a creature lives (its camps and their levels) and what it drops (the named things of the loot's drop lists that
        /// can fall to it), five at most.</summary>
        void CreatureNotes(ZoneDefinition z, string mob, List<string> notes)
        {
            var camps = new List<string>(); int lo = int.MaxValue, hi = 0; ZoneCamp first = null; bool elite = false;
            foreach (var c in z.camps ?? new ZoneCamp[0])
            {
                if (c == null || c.mob != mob) continue;
                if (first == null) first = c;
                if (!string.IsNullOrEmpty(c.name) && !camps.Contains(c.name)) camps.Add(c.name);
                lo = Math.Min(lo, c.levelMin); hi = Math.Max(hi, c.levelMax); elite |= c.elite;
            }
            if (first == null) return;
            notes.Add((camps.Count > 0 ? "Found at " + Join(camps, 3) : "Found in " + Short(z)) + (lo <= hi ? ", level " + (lo == hi ? lo.ToString() : lo + "-" + hi) : "") + (elite ? " (elite)" : "") + ".");
            if (Loot == null) return;
            var drops = new List<string>(); string zone = ZoneLore.Short(z.id), tag = LootContext.CampTag(first);
            foreach (var d in Loot.Drops)
            {
                if (d == null || (!string.IsNullOrEmpty(d.zone) && d.zone != zone) || (!string.IsNullOrEmpty(d.tag) && d.tag != tag) || (!string.IsNullOrEmpty(d.mob) && d.mob != mob)) continue;
                if (!string.IsNullOrEmpty(d.rank) && (d.rank == "elite") != elite) continue;
                if (d.levelMax < lo || d.levelMin > hi) continue;
                foreach (var g in d.groups ?? new DropGroup[0])
                    if (g != null && !g.classSet)
                        foreach (var p in g.pick ?? new DropPick[0])
                        {
                            var item = p == null ? null : Items?.Get(p.item);
                            if (item != null && !drops.Contains(item.name)) drops.Add(item.name);
                        }
            }
            if (drops.Count > 0) notes.Add("Drops " + Join(drops, 5) + ".");
        }
        /// <summary>A herb's or ore's trade and skill, where it grows (the places nearest its nodes; never a rare herb's, which is a hidden
        /// find's spot), and what it is made into.</summary>
        void HerbNotes(ZoneDefinition z, string subject, NodeDef node, ItemDef item, List<string> notes)
        {
            if (node != null)
            {
                var trade = Trades?.Profession(node.profession);
                notes.Add((trade != null ? trade.name : node.profession) + " " + node.skill + (item != null ? ", for " + item.name.ToLowerInvariant() : "") + ".");
                var near = new List<string>();
                void Near(Vector2 at) { string best = null; float d = float.MaxValue; foreach (var l in z.landmarks ?? new ZoneLabel[0]) if (l != null && Vector2.Distance(l.at, at) < d) { d = Vector2.Distance(l.at, at); best = l.name; } if (best != null && !near.Contains(best)) near.Add(best); }
                foreach (var n in z.nodes ?? new ZoneNode[0]) if (n != null && n.node == subject) Near(n.at);
                foreach (var p in z.props ?? new ZoneProp[0]) if (p != null && p.node == subject) Near(p.at);
                if (near.Count > 0) notes.Add("Worked about " + Join(near, 3) + ".");
            }
            else if (IsRareHerb(subject)) notes.Add("Picked with Herbalism; it grows back a day after it is picked, where it was first found.");
            string input = node != null ? node.item : subject;
            if (Trades == null || string.IsNullOrEmpty(input)) return;
            var makes = new List<string>();
            foreach (var r in Trades.Recipes)
                foreach (var i in r?.inputs ?? new RecipeInput[0])
                    if (i != null && i.item == input) { var o = Items?.Get(r.output); string n = o != null ? o.name : r.name; if (!string.IsNullOrEmpty(n) && !makes.Contains(n)) makes.Add(n); }
            if (makes.Count > 0) notes.Add("Made into " + Join(makes, 5) + ".");
        }
        bool RareHerbFound(ZoneDefinition z, string item)
        {
            if (Progress?.discoveries == null) return false;
            foreach (var s in z.secrets ?? new ZoneSecret[0]) if (s != null && s.kind == "herb" && s.item == item && Progress.discoveries.Contains(s.id)) return true;
            return false;
        }
        static string Join(List<string> items, int most)
        {
            if (items.Count == 0) return "";
            var shown = items.GetRange(0, Math.Min(most, items.Count)); string rest = items.Count > most ? " and more" : "";
            if (shown.Count == 1) return shown[0] + rest;
            if (rest.Length > 0) return string.Join(", ", shown) + rest;
            return string.Join(", ", shown.GetRange(0, shown.Count - 1)) + " and " + shown[shown.Count - 1];
        }

        /// <summary>The Factions chapter: the Standing tab's words for every faction, always known.</summary>
        public List<Line> FactionLines()
        {
            var list = new List<Line>(); if (Quests == null) return list;
            foreach (var f in Quests.FactionOrder)
                if (f != null) list.Add(new Line { chapter = Factions, subject = f.id, title = f.name, plain = f.description ?? "", canonStatus = f.canonStatus, known = true });
            return list;
        }
        /// <summary>
        /// HOOK (Phase 8.6 step 5, found recipes: another hand's): the Leaves chapter, a line for each recipe found ("a smith's leaf:
        /// Bog-iron coif"). Set by the found-recipe work; until then the chapter says what it will hold and lists nothing.
        /// </summary>
        public Func<List<Line>> LeafLines;

        // ---------- a zone's chapter, whole ----------
        /// <summary>How far a zone's chapter of the book has come: lines known in full, lines shown, and lines in all (its four chapters).</summary>
        public (int full, int shown, int total) Count(ZoneDefinition z)
        {
            int full = 0, shown = 0, total = 0;
            foreach (var c in ZoneChapters)
                foreach (var l in Lines(z, c, false)) { total++; if (l.Full) full++; if (l.Shown) shown++; }
            return (full, shown, total);
        }
        public bool Complete(ZoneDefinition z) { var c = Count(z); return c.total > 0 && c.full == c.total; }

        // ---------- checks ----------
        static readonly Regex Cited = new Regex(@"book\s*\d+\s*ch\.?\s*\d+", RegexOptions.IgnoreCase);
        /// <summary>Whether a label is one of the four, and a bare CANON cites a book and chapter ("CANON, book1 ch.4").</summary>
        public static bool Labelled(string label, out string why)
        {
            why = null;
            if (string.IsNullOrWhiteSpace(label)) { why = "no canonStatus"; return false; }
            string head = label.TrimStart();
            bool any = false; foreach (var l in Labels) if (head.StartsWith(l, StringComparison.Ordinal)) any = true;
            if (!any) { why = "its canonStatus '" + label + "' is not CANON, CANON-EXPANDED, GAME-ONLY or PROVISIONAL"; return false; }
            if (head.StartsWith("CANON", StringComparison.Ordinal) && !head.StartsWith("CANON-EXPANDED", StringComparison.Ordinal) && !Cited.IsMatch(head))
            { why = "it is CANON without a book and chapter cited"; return false; }
            return true;
        }
        /// <summary>
        /// Problems with the lore files, one line each (none = sound):
        /// - a file for no zone; an entry with no id, an id used twice, or one not "lore.&lt;zone&gt;.&lt;slug&gt;" for its own zone;
        /// - an unknown chapter; a subject that is nothing in its chapter (no such landmark, person, camp mob, node or rare herb), or
        ///   written twice;
        /// - no line and no more; a more with neither page nor secret, or a page or secret with no more; a page that is no Chronicle
        ///   page, a secret that is no hidden find (checked when the content is given);
        /// - a label that is not one of the four, or CANON without a book and chapter (<see cref="Labelled"/>);
        /// - a line that names a hidden find (its name; Validate keeps landmarks out of the clues, this keeps finds out of the book), or
        ///   a more that names one other than the find it is found at.
        /// </summary>
        public static List<string> Validate(IEnumerable<LoreFile> files, IEnumerable<ZoneDefinition> zones, QuestDatabase quests = null, ProfessionDatabase trades = null)
        {
            var problems = new List<string>(); var list = new List<ZoneDefinition>(); var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var z in zones ?? new ZoneDefinition[0]) if (z != null) list.Add(z);
            var secrets = new Dictionary<string, ZoneSecret>(StringComparer.Ordinal);
            foreach (var z in list) foreach (var s in z.secrets ?? new ZoneSecret[0]) if (s != null && !string.IsNullOrEmpty(s.id)) secrets[s.id] = s;
            foreach (var f in files ?? new LoreFile[0])
            {
                if (f == null) continue;
                var z = list.Find(x => x.id == f.zone);
                if (z == null) { problems.Add("A lore file for '" + f.zone + "', which is no zone."); continue; }
                string prefix = "lore." + ZoneLore.Short(z.id) + ".";
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var e in f.entries ?? new LoreEntry[0])
                {
                    if (e == null) continue;
                    if (string.IsNullOrEmpty(e.id)) { problems.Add(z.id + ": a lore entry has no id."); continue; }
                    string p = "Lore '" + e.id + "': ";
                    if (!ids.Add(e.id)) problems.Add(p + "its id is used twice.");
                    if (!e.id.StartsWith(prefix, StringComparison.Ordinal) || e.id.Length == prefix.Length) problems.Add(p + "its id should be " + prefix + "<slug>.");
                    if (Array.IndexOf(ZoneChapters, e.chapter) < 0) problems.Add(p + "unknown chapter '" + e.chapter + "' (places, people, creatures, herbs).");
                    else if (!seen.Add(e.chapter + "|" + e.subject)) problems.Add(p + "'" + e.subject + "' is written twice in " + e.chapter + ".");
                    else if (!Subject(z, e.chapter, e.subject, trades)) problems.Add(p + "'" + e.subject + "' is nothing in " + z.displayName + "'s " + e.chapter + ".");
                    if (string.IsNullOrWhiteSpace(e.line) && string.IsNullOrWhiteSpace(e.more)) problems.Add(p + "says nothing.");
                    bool unlock = !string.IsNullOrEmpty(e.page) || !string.IsNullOrEmpty(e.secret);
                    if (!string.IsNullOrWhiteSpace(e.more) && !unlock) problems.Add(p + "its better half needs a page or a secret to be found at.");
                    if (unlock && string.IsNullOrWhiteSpace(e.more)) problems.Add(p + "a page or secret with no better half to give.");
                    if (quests != null && !string.IsNullOrEmpty(e.page) && !quests.Documents.ContainsKey(e.page)) problems.Add(p + "unknown page '" + e.page + "'.");
                    if (!string.IsNullOrEmpty(e.secret) && !secrets.ContainsKey(e.secret)) problems.Add(p + "unknown secret '" + e.secret + "'.");
                    if (!Labelled(e.canonStatus, out var why)) problems.Add(p + why + ".");
                    // No line gives a hidden find away by name; a better half may name only the find it is found at.
                    foreach (var s in secrets.Values)
                    {
                        if (string.IsNullOrEmpty(s.name)) continue;
                        bool own = s.id == e.secret || (!string.IsNullOrEmpty(e.page) && s.document == e.page);
                        if (DiscoveryLog.Mentions(e.line, s.name)) problems.Add(p + "its line names the hidden find '" + s.name + "' (" + s.id + ").");
                        if (!own && DiscoveryLog.Mentions(e.more, s.name)) problems.Add(p + "its better half names the hidden find '" + s.name + "' (" + s.id + "), which it is not found at.");
                    }
                }
            }
            return problems;
        }
        static bool Subject(ZoneDefinition z, string chapter, string subject, ProfessionDatabase trades)
        {
            if (string.IsNullOrEmpty(subject)) return false;
            switch (chapter)
            {
                case Places: foreach (var l in z.landmarks ?? new ZoneLabel[0]) if (l != null && l.name == subject) return true; return false;
                case People: return PeopleOf(z).Contains(subject);
                case Creatures: return CreaturesOf(z, true).Contains(subject);
                case Herbs: return HerbsOf(z).Contains(subject) && (trades == null || IsRareHerb(subject) || trades.Node(subject) != null);
                default: return false;
            }
        }
    }
}
