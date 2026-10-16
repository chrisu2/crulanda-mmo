using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Combat;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Hidden finds (ZoneSecret): places on no map that pay out once when found. A lookout (vista) is found by standing on it; a
    /// cache, note, herb, chest or key by searching it (E). Finding one records its id in the save (EncounterProgress.discoveries,
    /// format 7) and pays its XP, gold, an item into the bags and a Chronicle page; the session shows the "Discovered" toast and
    /// levels you up the way a quest reward does. A secret that needs another (a chest and its key) stays shut until that one is
    /// found, and a find whose item won't fit in full bags waits, untouched, until there is room.
    /// Phase 8.6 (step 8, 2026-10-15; decisions 6 and 7: about 90 secrets, villagers' hints and a count by region, no chime, no glint)
    /// adds five kinds: carving (a mark cut in a stone or a trunk: a line for the book of lore), read (a book, a cairn's book or slates:
    /// a page), grave (a named stone: a line for the book), remains (a dead adventurer: gear and a page; the satchel is taken, the
    /// bones stay) and shrine (a wayshrine: found, it blesses you; after that a crown left in its dish blesses you again). Two kinds
    /// stay useful once found (<see cref="Lingers"/>): a rare herb grows back a game day after it is picked (<see cref="Regrown"/>;
    /// picked with Herbalism, <see cref="HerbNode"/>), and a shrine takes its crown. When a herb was picked is kept here, not in the
    /// save: a load counts it as picked that day, as a worked node rests again after a load.
    /// Pure logic over the progress, like QuestLog, so it is tested without a scene.
    /// </summary>
    public sealed class DiscoveryLog
    {
        public const string LockedLine = "Locked. The key must be somewhere near.";
        public const string NotYetLine = "You can't get at it yet. Something else must be found first.";
        public const string BagsFullLine = "Your bags are full. Make room, then search again.";
        public static readonly string[] Kinds = { "vista", "cache", "note", "herb", "chest", "key", "carving", "read", "grave", "remains", "shrine" };
        public enum Result { Found, AlreadyFound, Locked, BagsFull }

        public EncounterProgress Progress { get; private set; }
        /// <summary>Items and Chronicle pages. Either may be null (no content): the item is then left out, the page named by its id.</summary>
        public ItemDatabase Items; public QuestDatabase Content;
        /// <summary>Chat lines.</summary>
        public Action<string> Say = t => { };
        /// <summary>A secret was just found (the session raises its toast).</summary>
        public Action<ZoneSecret> Found = s => { };

        public DiscoveryLog(EncounterProgress progress, ItemDatabase items, QuestDatabase content) { Items = items; Content = content; Bind(progress); }
        public void Bind(EncounterProgress progress)
        {
            Progress = progress;
            if (Progress.discoveries == null) Progress.discoveries = new List<string>();
            if (Progress.documents == null) Progress.documents = new List<string>();
            pickedOn.Clear();   // another character's (or a reloaded save's) herbs
        }

        // ---------- queries ----------
        public bool IsFound(ZoneSecret s) { return s != null && !string.IsNullOrEmpty(s.id) && Progress.discoveries.Contains(s.id); }
        /// <summary>A lookout: found by standing within its radius, not by searching.</summary>
        public static bool IsVista(ZoneSecret s) { return s != null && s.kind == "vista"; }
        /// <summary>Things you take with you (a page, a key, a rare plant, a dead adventurer's satchel) vanish once found; chests, caches,
        /// cairns, carvings, books, graves, shrines and the bones stay.</summary>
        public static bool Pocketed(ZoneSecret s) { return s != null && (s.kind == "note" || s.kind == "key" || s.kind == "herb" || s.kind == "remains"); }
        /// <summary>Still of use once found: a rare herb (it grows back) and a wayshrine (a crown for its blessing).</summary>
        public static bool Lingers(ZoneSecret s) { return s != null && ((s.kind == "herb" && !string.IsNullOrEmpty(s.item)) || s.kind == "shrine"); }
        /// <summary>Whether what it needs first (a chest's key) has been found.</summary>
        public bool Unlocked(ZoneSecret s) { return s != null && (string.IsNullOrEmpty(s.needs) || Progress.discoveries.Contains(s.needs)); }
        /// <summary>What searching a still-shut secret says.</summary>
        public static string ShutLine(ZoneSecret s) { return s != null && s.kind == "chest" ? LockedLine : NotYetLine; }
        public static string Name(ZoneSecret s) { return s == null || string.IsNullOrEmpty(s.name) ? "A hidden place" : s.name; }

        // ---------- finding ----------
        /// <summary>
        /// Finds a secret: records it and pays out, once ever. Nothing happens when it is AlreadyFound, Locked (what it needs isn't
        /// found yet) or BagsFull (its item won't fit; it can be found once there is room).
        /// </summary>
        public Result Discover(ZoneSecret s)
        {
            if (s == null || string.IsNullOrEmpty(s.id) || IsFound(s)) return Result.AlreadyFound;
            if (!Unlocked(s)) return Result.Locked;
            var item = string.IsNullOrEmpty(s.item) || Items == null ? null : Items.Get(s.item);
            if (item != null && Inventory.Room(Progress, Items, item.id) < 1) return Result.BagsFull;
            Progress.discoveries.Add(s.id);
            Progress.experience += Math.Max(0, s.xp); Progress.gold += Math.Max(0, s.gold);
            if (item != null) Inventory.Add(Progress, Items, item.id, 1);
            if (s.kind == "herb") Picked(s);   // it grows back from tomorrow
            Say("Discovered: " + Name(s) + "." + (string.IsNullOrEmpty(s.text) ? "" : " " + s.text) + Rewards(s, item));
            if (!string.IsNullOrEmpty(s.document) && !Progress.documents.Contains(s.document))
            {
                Progress.documents.Add(s.document);
                Say("New page in your Chronicle: " + PageTitle(s.document) + " [L]");
            }
            Found(s);
            return Result.Found;
        }
        /// <summary>"  +40 XP  +12 gold  +Salt-cured Knife", or empty when it pays nothing.</summary>
        static string Rewards(ZoneSecret s, ItemDef item)
        {
            string r = "";
            if (s.xp > 0) r += "  +" + s.xp + " XP";
            if (s.gold > 0) r += "  +" + s.gold + " crowns";
            if (item != null) r += "  +" + item.name;
            return r;
        }
        string PageTitle(string doc) { return Content != null && Content.Documents.TryGetValue(doc, out var d) && !string.IsNullOrEmpty(d.title) ? d.title : doc; }

        // ---------- rare herbs that grow back (Phase 8.6 3e: "the reason to come back with Herbalism") ----------
        readonly Dictionary<string, int> pickedOn = new Dictionary<string, int>(StringComparer.Ordinal);
        /// <summary>The game day a found herb was last picked; first asked after a load, today (it rests a day, as if just picked).</summary>
        public int PickedOn(ZoneSecret s)
        {
            if (s == null || string.IsNullOrEmpty(s.id)) return int.MaxValue;
            if (!pickedOn.TryGetValue(s.id, out int day)) pickedOn[s.id] = day = Progress.days;
            return day;
        }
        /// <summary>A found rare herb grown back: a game day (the clock past six in the morning) has gone since it was picked.</summary>
        public bool Regrown(ZoneSecret s) { return s != null && s.kind == "herb" && !string.IsNullOrEmpty(s.item) && IsFound(s) && Progress.days > PickedOn(s); }
        /// <summary>It has just been picked: gone until tomorrow.</summary>
        public void Picked(ZoneSecret s) { if (s != null && !string.IsNullOrEmpty(s.id)) pickedOn[s.id] = Progress.days; }
        /// <summary>The Herbalism skill a rare herb asks, by its item's level: moonbell (1) 10, widow's-lamp (5) 30, frostbell (9) 50,
        /// last-light (12) 65, lantern-moss (14) 75.</summary>
        public static int HerbSkill(ItemDef item) { return item == null ? 1 : Math.Max(1, Math.Min(ProfessionDatabase.MaxSkill - 10, 5 * Math.Max(1, item.level) + 5)); }
        /// <summary>
        /// What picking a grown-back herb works as: the trades' own Herbalism node for its item when the content has one (the rare herbs'
        /// nodes may come with their flasks, 3b), else one made here: Herbalism at <see cref="HerbSkill"/>, one of its item a picking.
        /// </summary>
        public static NodeDef HerbNode(ZoneSecret s, ProfessionDatabase trades, ItemDatabase items)
        {
            if (s == null || string.IsNullOrEmpty(s.item) || string.IsNullOrEmpty(s.id)) return null;
            if (trades != null) foreach (var n in trades.Nodes) if (n != null && n.item == s.item && n.profession == "herbalism") return n;
            var item = items?.Get(s.item); string slug = s.id.Substring(s.id.LastIndexOf('.') + 1);
            return new NodeDef { id = "node.rare." + slug, name = item != null ? item.name : Name(s), profession = "herbalism", item = s.item, look = "herb",
                skill = HerbSkill(item), min = 1, max = 1, respawn = 1, seconds = 1.5f, prompt = string.IsNullOrEmpty(s.prompt) ? "Pick it" : s.prompt };
        }

        // ---------- wayshrines (Phase 8.6 3e: "a wayshrine's offering: a coin for a short blessing") ----------
        public const string BlessingId = "status.wayshrine_blessing", BlessingName = "Wayshrine's Blessing";
        /// <summary>The blessing: a little less taken from every blow (3%) for ten minutes. Small on purpose.</summary>
        public const float BlessingSeconds = 600, BlessingWard = .97f;
        /// <summary>A crown in the dish, after the first time (the first blessing is the find's).</summary>
        public const int Offering = 1;
        public const string NoCrownLine = "You have no crown to leave.";
        public const string StillBlessedLine = "The shrine's blessing is on you still.";
        public static StatusEffectDefinition Blessing() { return new StatusEffectDefinition { id = BlessingId, name = BlessingName, duration = BlessingSeconds, incomingDamageMultiplier = BlessingWard }; }
        /// <summary>A crown left at a found wayshrine: taken from the purse when there is one. False with why (none to leave, or not a found
        /// shrine). The session lays the blessing on.</summary>
        public bool Offer(ZoneSecret s, out string why)
        {
            why = null;
            if (s == null || s.kind != "shrine" || !IsFound(s)) { why = "Nothing to leave an offering at."; return false; }
            if (Progress.gold < Offering) { why = NoCrownLine; return false; }
            Progress.gold -= Offering; return true;
        }

        // ---------- the quest book's Discoveries tab ----------
        /// <summary>
        /// One zone in the Discoveries tab: how many secrets it holds, the ones found, and a clue each for the rest (ZoneSecret.clue,
        /// written to point at the region and the hiding place without naming the spot; 2026-10-15, Chris: "still havent found many
        /// secrets"). Nothing else about the unfound (name, place or kind) is kept here, so the book can't give them away.
        /// </summary>
        public sealed class Tally
        {
            public string zoneId, zoneName; public bool here; public int total;
            public readonly List<ZoneSecret> found = new List<ZoneSecret>();
            /// <summary>The clues of the secrets not found, in the zone's order (one without a clue is counted and no more).</summary>
            public readonly List<string> clues = new List<string>();
            /// <summary>How many are still hidden about each of the zone's great places (decision 6, 2026-10-15: "3 hidden in the hills"),
            /// most first; empty when the zone's places were not given.</summary>
            public readonly List<(string place, int count)> regions = new List<(string place, int count)>();
            public int Hidden { get { return total - found.Count; } }
        }
        public Tally TallyOf(string zoneId, string zoneName, IEnumerable<ZoneSecret> secrets, bool here = false, ZoneLabel[] landmarks = null)
        {
            var t = new Tally { zoneId = zoneId, zoneName = zoneName, here = here }; var seen = new HashSet<string>(StringComparer.Ordinal);
            var hidden = new List<ZoneSecret>();
            if (secrets != null)
                foreach (var s in secrets)
                {
                    if (s == null || string.IsNullOrEmpty(s.id) || !seen.Add(s.id)) continue;
                    t.total++;
                    if (IsFound(s)) t.found.Add(s);
                    else { hidden.Add(s); var c = ZoneLore.Clue(s); if (c != null) t.clues.Add(c); }
                }
            if (landmarks != null) t.regions.AddRange(Regions(hidden, landmarks));
            return t;
        }
        /// <summary>
        /// The count by region (decision 6): each secret not found goes to the nearest of the zone's great places (a landmark of 12 m
        /// or more, where a zone has three such; else to any landmark), and the places are listed with their counts, most first. The
        /// map shows those places already; nothing narrower than "about the Mastwood" is said.
        /// </summary>
        public static List<(string place, int count)> Regions(IEnumerable<ZoneSecret> hidden, ZoneLabel[] landmarks)
        {
            var list = new List<(string place, int count)>(); if (hidden == null || landmarks == null) return list;
            var great = new List<ZoneLabel>(); var any = new List<ZoneLabel>();
            foreach (var l in landmarks) if (l != null && !string.IsNullOrEmpty(l.name)) { any.Add(l); if (l.radius >= 12) great.Add(l); }
            var from = great.Count >= 3 ? great : any; if (from.Count == 0) return list;
            var counts = new Dictionary<string, int>(StringComparer.Ordinal); var order = new List<string>();
            foreach (var s in hidden)
            {
                if (s == null) continue;
                ZoneLabel best = null; float d = float.MaxValue;
                foreach (var l in from) { float dl = Vector2.Distance(l.at, s.at); if (dl < d) { d = dl; best = l; } }
                if (!counts.ContainsKey(best.name)) { counts[best.name] = 0; order.Add(best.name); }
                counts[best.name]++;
            }
            foreach (var n in order) list.Add((n, counts[n]));
            list.Sort((a, b) => a.count != b.count ? b.count.CompareTo(a.count) : string.CompareOrdinal(a.place, b.place));
            return list;
        }
        /// <summary>"3 hidden about the Mastwood": a place's name inside a sentence (its "The" made small).</summary>
        public static string RegionLine(string place, int count)
        {
            string at = place != null && place.StartsWith("The ", StringComparison.Ordinal) ? "the " + place.Substring(4) : place;
            return count + " hidden about " + at;
        }

        // ---------- content checks ----------
        /// <summary>
        /// Problems with the zones' secrets, one line each (none = sound):
        /// - an id missing, not "secret.&lt;zone&gt;.&lt;slug&gt;" for its own zone, or used twice anywhere;
        /// - an unknown kind; no name or canonStatus; a searchable with no prompt; a lookout with no radius; negative rewards;
        /// - needs naming no secret or itself, or needs that never end (a circle); a key that no chest needs;
        /// - an item or Chronicle page that doesn't exist (checked when those databases are given);
        /// - a landmark with a secret's name: landmarks are written on the zone map and over the world, so it would give it away;
        /// - a clue or hint that names a landmark (2026-10-15): the same give-away, in words. (That every secret has a clue is
        ///   ZoneLoreTests' check, after the side files are merged in.)
        /// - (Phase 8.6) a book to read or a dead adventurer with no page; a carving or grave with nothing written (its text); a rare
        ///   herb with no item to grow back; a shrine that gives an item (its gift is the blessing).
        /// </summary>
        public static List<string> Validate(IEnumerable<ZoneDefinition> zones, ItemDatabase items = null, QuestDatabase content = null)
        {
            var problems = new List<string>(); var list = new List<ZoneDefinition>();
            var all = new Dictionary<string, ZoneSecret>(StringComparer.Ordinal);
            foreach (var z in zones) if (z != null) list.Add(z);
            foreach (var z in list)
                foreach (var s in z.secrets ?? new ZoneSecret[0])
                {
                    if (s == null) continue;
                    if (string.IsNullOrEmpty(s.id)) { problems.Add(z.id + ": a secret has no id."); continue; }
                    if (all.ContainsKey(s.id)) problems.Add("Duplicate secret id '" + s.id + "'."); else all[s.id] = s;
                }
            foreach (var z in list)
            {
                string prefix = "secret." + (z.id ?? "").Replace("zone.", "") + ".";
                var landmarks = new HashSet<string>(StringComparer.Ordinal);
                foreach (var l in z.landmarks ?? new ZoneLabel[0]) if (l != null && !string.IsNullOrEmpty(l.name)) landmarks.Add(l.name);
                foreach (var s in z.secrets ?? new ZoneSecret[0])
                {
                    if (s == null || string.IsNullOrEmpty(s.id)) continue;
                    string p = "Secret '" + s.id + "': ";
                    if (!s.id.StartsWith(prefix, StringComparison.Ordinal) || s.id.Length == prefix.Length) problems.Add(p + "its id should be " + prefix + "<slug>.");
                    if (Array.IndexOf(Kinds, s.kind) < 0) problems.Add(p + "unknown kind '" + s.kind + "'.");
                    if (string.IsNullOrEmpty(s.name)) problems.Add(p + "no name.");
                    if (string.IsNullOrEmpty(s.canonStatus)) problems.Add(p + "no canonStatus.");
                    if (IsVista(s) && !(s.radius > 0)) problems.Add(p + "a lookout needs a radius.");
                    if (!IsVista(s) && string.IsNullOrEmpty(s.prompt)) problems.Add(p + "no prompt for E.");
                    if (s.xp < 0 || s.gold < 0) problems.Add(p + "negative rewards.");
                    if ((s.kind == "read" || s.kind == "remains") && string.IsNullOrEmpty(s.document)) problems.Add(p + "a " + s.kind + " with no page.");
                    if ((s.kind == "carving" || s.kind == "grave") && string.IsNullOrEmpty(s.text)) problems.Add(p + "a " + s.kind + " with nothing written on it.");
                    if (s.kind == "herb" && string.IsNullOrEmpty(s.item)) problems.Add(p + "a herb with no item, so nothing grows back.");
                    if (s.kind == "shrine" && !string.IsNullOrEmpty(s.item)) problems.Add(p + "a shrine gives its blessing, not an item.");
                    if (!string.IsNullOrEmpty(s.needs))
                    {
                        if (s.needs == s.id) problems.Add(p + "needs itself.");
                        else if (!all.ContainsKey(s.needs)) problems.Add(p + "needs unknown secret '" + s.needs + "'.");
                        else if (Circular(s, all)) problems.Add(p + "its needs go round in a circle, so it can never be found.");
                    }
                    if (s.kind == "key")
                    {
                        bool opens = false; foreach (var o in all.Values) if (o.kind == "chest" && o.needs == s.id) opens = true;
                        if (!opens) problems.Add(p + "a key that no chest needs.");
                    }
                    if (items != null && !string.IsNullOrEmpty(s.item) && items.Get(s.item) == null) problems.Add(p + "unknown item '" + s.item + "'.");
                    if (content != null && !string.IsNullOrEmpty(s.document) && !content.Documents.ContainsKey(s.document)) problems.Add(p + "unknown Chronicle page '" + s.document + "' (add it to a quest file's documents).");
                    if (!string.IsNullOrEmpty(s.name) && landmarks.Contains(s.name)) problems.Add(p + "a landmark has its name, so the map would give it away.");
                    foreach (var l in landmarks)
                        if (Mentions(s.clue, l) || Mentions(s.hint, l)) problems.Add(p + "its clue or hint names the landmark '" + l + "', which the map shows.");
                }
            }
            return problems;
        }
        /// <summary>Whether a clue or hint carries a landmark's name, in any case ("the watchtower" gives away The Watchtower).</summary>
        public static bool Mentions(string line, string landmark)
        {
            return !string.IsNullOrEmpty(line) && !string.IsNullOrEmpty(landmark) && line.IndexOf(landmark, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        /// <summary>Following needs from this secret comes back round to one already passed.</summary>
        static bool Circular(ZoneSecret s, Dictionary<string, ZoneSecret> all)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal) { s.id };
            for (var at = s; at != null && !string.IsNullOrEmpty(at.needs); all.TryGetValue(at.needs, out at))
                if (!seen.Add(at.needs)) return true;
            return false;
        }
    }
}
