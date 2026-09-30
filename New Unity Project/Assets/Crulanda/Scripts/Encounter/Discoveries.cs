using System;
using System.Collections.Generic;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Hidden finds (ZoneSecret): places on no map that pay out once when found. A lookout (vista) is found by standing on it; a
    /// cache, note, herb, chest or key by searching it (E). Finding one records its id in the save (EncounterProgress.discoveries,
    /// format 7) and pays its XP, gold, an item into the bags and a Chronicle page; the session shows the "Discovered" toast and
    /// levels you up the way a quest reward does. A secret that needs another (a chest and its key) stays shut until that one is
    /// found, and a find whose item won't fit in full bags waits, untouched, until there is room.
    /// Pure logic over the progress, like QuestLog, so it is tested without a scene.
    /// </summary>
    public sealed class DiscoveryLog
    {
        public const string LockedLine = "Locked. The key must be somewhere near.";
        public const string NotYetLine = "You can't get at it yet. Something else must be found first.";
        public const string BagsFullLine = "Your bags are full. Make room, then search again.";
        public static readonly string[] Kinds = { "vista", "cache", "note", "herb", "chest", "key" };
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
        }

        // ---------- queries ----------
        public bool IsFound(ZoneSecret s) { return s != null && !string.IsNullOrEmpty(s.id) && Progress.discoveries.Contains(s.id); }
        /// <summary>A lookout: found by standing within its radius, not by searching.</summary>
        public static bool IsVista(ZoneSecret s) { return s != null && s.kind == "vista"; }
        /// <summary>Things you take with you (a page, a key, a rare plant) vanish once found; chests, caches and cairns stay.</summary>
        public static bool Pocketed(ZoneSecret s) { return s != null && (s.kind == "note" || s.kind == "key" || s.kind == "herb"); }
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
            if (s.gold > 0) r += "  +" + s.gold + " gold";
            if (item != null) r += "  +" + item.name;
            return r;
        }
        string PageTitle(string doc) { return Content != null && Content.Documents.TryGetValue(doc, out var d) && !string.IsNullOrEmpty(d.title) ? d.title : doc; }

        // ---------- the quest book's Discoveries tab ----------
        /// <summary>
        /// One zone in the Discoveries tab: how many secrets it holds and the ones found. The rest are only counted: nothing else
        /// about them (name, place or kind) is kept here, so the book can't give them away.
        /// </summary>
        public sealed class Tally
        {
            public string zoneId, zoneName; public bool here; public int total;
            public readonly List<ZoneSecret> found = new List<ZoneSecret>();
            public int Hidden { get { return total - found.Count; } }
        }
        public Tally TallyOf(string zoneId, string zoneName, IEnumerable<ZoneSecret> secrets, bool here = false)
        {
            var t = new Tally { zoneId = zoneId, zoneName = zoneName, here = here }; var seen = new HashSet<string>(StringComparer.Ordinal);
            if (secrets != null)
                foreach (var s in secrets)
                {
                    if (s == null || string.IsNullOrEmpty(s.id) || !seen.Add(s.id)) continue;
                    t.total++; if (IsFound(s)) t.found.Add(s);
                }
            return t;
        }

        // ---------- content checks ----------
        /// <summary>
        /// Problems with the zones' secrets, one line each (none = sound):
        /// - an id missing, not "secret.&lt;zone&gt;.&lt;slug&gt;" for its own zone, or used twice anywhere;
        /// - an unknown kind; no name or canonStatus; a searchable with no prompt; a lookout with no radius; negative rewards;
        /// - needs naming no secret or itself, or needs that never end (a circle); a key that no chest needs;
        /// - an item or Chronicle page that doesn't exist (checked when those databases are given);
        /// - a landmark with a secret's name: landmarks are written on the zone map and over the world, so it would give it away.
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
                }
            }
            return problems;
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
