using System;
using System.Collections.Generic;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Armoury (loot DESIGN.md 4, rows 8-10; step L3): every named piece of gear in the land, by zone and source, and what the
    /// character has found of it. A named piece counts as found the first time it is in the bags or worn, and stays found after it is
    /// sold; every appearance (GearLooks.AppearanceKey) that has been in the bags is a look seen. Both are kept in the save
    /// (EncounterProgress.armoury and looks, format 9), and so are the kills of each loot source (lootLuck): killing a mob whose drop
    /// list names a zone, camp or mob makes that list's pieces known by name in the book (so does finding one of its pieces, which
    /// is how a save from before the kills were counted knows its boss), and an epic's run of kills without it
    /// makes it certain in the end (LootDatabase.Roll). Hidden finds and rare world drops stay unknown until found, and a hidden
    /// find never names its place. Sweep looks over the bags and equipment (the session calls it twice a second), so buying,
    /// crafting, quest rewards and loot are all noticed without a hook in their code; Bind does the same quietly, so what the
    /// character already has never raises a toast. Pure logic over the progress, like DiscoveryLog, so it is tested without a scene.
    /// </summary>
    public sealed class ArmouryLog
    {
        /// <summary>Unknown: a silhouette with its slot and the kind of source. Known: its source is met, so its name shows in grey. Found: held at least once.</summary>
        public enum State { Unknown, Known, Found }
        /// <summary>The Armoury's group for pieces that drop anywhere (world sources), beside the five zones.</summary>
        public const string World = "world";
        /// <summary>The short zone names in loot ids ("loot.oak.") and the zones they stand for.</summary>
        public static readonly (string shortId, string zone)[] ZoneShorts = { ("oak", "oakhaven"), ("kha", "khaven"), ("pea", "peaks"), ("ash", "ashrim"), ("ver", "verdant"), ("adit", "adit") };   // the Sealed Adit (dungeon D6)
        /// <summary>The kinds of source in the order the book lists them, and how an unknown piece of each kind is described.</summary>
        public static readonly (string kind, string heading, string unknown)[] Kinds = {
            ("boss", "Bosses", "Dropped by a boss"), ("mob", "Camps", "Dropped in the camps"), ("quest", "Quest rewards", "A quest reward"),
            ("vendor", "Merchants", "Sold by a merchant"), ("secret", "Hidden finds", "Hidden somewhere"), ("world", "Rare finds", "A rare find anywhere") };

        public EncounterProgress Progress { get; private set; }
        /// <summary>Items, the named loot and the looks. Without the loot nothing is named (looks are still seen); without the looks no look is seen.</summary>
        public readonly ItemDatabase Items; public readonly LootDatabase Loot; public readonly GearLooks Looks;
        /// <summary>A named piece was found for the first time (its item id).</summary>
        public Action<string> NewItem = id => { };
        /// <summary>A piece brought a look not seen before (its item id): the "NEW LOOK" toast.</summary>
        public Action<string> NewLook = id => { };
        readonly List<ZoneSecret> secrets = new List<ZoneSecret>();
        readonly HashSet<string> found = new HashSet<string>(StringComparer.Ordinal), seenLooks = new HashSet<string>(StringComparer.Ordinal), swept = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>For each named piece, the drop lists (naming a zone, camp or mob) whose kills make it known.</summary>
        readonly Dictionary<string, List<string>> revealedBy = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        /// <summary>For each of those drop lists, the named pieces it drops: finding one of them meets the list's source too.</summary>
        readonly Dictionary<string, List<string>> picksOf = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        /// <summary>Each named piece's group in the book (ZoneOf), worked out once.</summary>
        readonly Dictionary<string, string> zoneOf = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <param name="secrets">Every zone's hidden finds (optional): a find already found that holds a named piece counts that piece as found.</param>
        public ArmouryLog(EncounterProgress progress, ItemDatabase items, LootDatabase loot, GearLooks looks, IEnumerable<ZoneSecret> secrets = null)
        {
            Items = items; Loot = loot; Looks = looks;
            if (secrets != null) foreach (var s in secrets) if (s != null) this.secrets.Add(s);
            if (Loot != null)
                foreach (var d in Loot.Drops)
                {
                    if (!Reveals(d)) continue;
                    var picks = picksOf[d.id] = new List<string>();
                    foreach (var g in d.groups) foreach (var k in g.pick)
                    {
                        if (!revealedBy.TryGetValue(k.item, out var lists)) revealedBy[k.item] = lists = new List<string>();
                        if (!lists.Contains(d.id)) lists.Add(d.id);
                        if (!picks.Contains(k.item)) picks.Add(k.item);
                    }
                }
            Bind(progress);
        }
        /// <summary>
        /// Binds to a character (a new session or a load): the three lists are made if missing, and what the character already has
        /// counts, quietly (no NewItem or NewLook): named gear and looks in the bags or worn, and the named gear of hidden finds
        /// already found. Run on every bind, so a migrated save's first load marks what it holds.
        /// </summary>
        public void Bind(EncounterProgress progress)
        {
            Progress = progress;
            if (Progress.armoury == null) Progress.armoury = new List<string>();
            if (Progress.looks == null) Progress.looks = new List<string>();
            if (Progress.lootLuck == null) Progress.lootLuck = new List<LootLuck>();
            found.Clear(); seenLooks.Clear(); swept.Clear();
            foreach (var id in Progress.armoury) if (!string.IsNullOrEmpty(id)) found.Add(id);
            foreach (var key in Progress.looks) if (!string.IsNullOrEmpty(key)) seenLooks.Add(key);
            Sweep(false);
            var finds = Progress.discoveries ?? new List<string>();
            if (Loot != null)
                foreach (var g in Loot.GearOrder)
                    if (LootDatabase.SourceKind(g.source) == "secret" && finds.Contains(g.source.Substring("secret:".Length))) Record(g.id, false);
            foreach (var s in secrets)
                if (!string.IsNullOrEmpty(s.item) && finds.Contains(s.id) && Loot != null && Loot.Meta(s.item) != null) Record(s.item, false);
        }

        // ---------- finding ----------
        /// <summary>
        /// Looks over the bags and equipment for what is new: a named piece not found before (NewItem) and a look not seen before
        /// (NewLook, once per look). Each item id is looked at once per bind, so a sweep over things already seen costs next to
        /// nothing. Returns how many new entries and looks it made.
        /// </summary>
        public int Sweep() { return Sweep(true); }
        int Sweep(bool tell)
        {
            int fresh = 0;
            if (Progress.bag != null) foreach (var s in Progress.bag) fresh += Take(s, tell);
            if (Progress.equipment != null) foreach (var s in Progress.equipment) fresh += Take(s, tell);
            return fresh;
        }
        int Take(ItemStack s, bool tell)
        {
            if (s == null || s.Empty || Items == null || !swept.Add(s.item)) return 0;
            var d = Items.Get(s.item); if (d == null || d.kind != "gear") return 0;
            int n = 0;
            if (Loot != null && Loot.Meta(d.id) != null && Record(d.id, tell)) n++;
            var key = LookOf(d);
            if (key != null && seenLooks.Add(key)) { Progress.looks.Add(key); n++; if (tell) NewLook(d.id); }
            return n;
        }
        bool Record(string id, bool tell)
        {
            if (!found.Add(id)) return false;
            Progress.armoury.Add(id); if (tell) NewItem(id);
            return true;
        }
        /// <summary>A piece of gear's appearance (GearLooks.AppearanceKey of its resolved look: no colours, quality or tier), or null for anything else or without the looks.</summary>
        public string LookOf(ItemDef d) { return Looks == null || d == null || d.kind != "gear" ? null : GearLooks.AppearanceKey(Looks.Resolve(d)); }

        /// <summary>
        /// A camp mob died: each drop list it rolls that names a zone, camp or mob counts the kill (lootLuck, by the list's id), which
        /// makes the list's pieces known by name. World lists (a level band alone) count nothing, so their pieces stay unknown.
        /// </summary>
        public void Killed(LootContext c)
        {
            if (Loot == null) return;
            foreach (var d in Loot.Drops)
            {
                if (!Reveals(d) || !LootDatabase.Matches(d, c)) continue;
                var counter = Progress.lootLuck.Find(x => x != null && x.source == d.id);
                if (counter == null) Progress.lootLuck.Add(counter = new LootLuck { source = d.id });
                counter.kills++;
            }
        }
        static bool Reveals(DropDef d) { return !string.IsNullOrEmpty(d.zone) || !string.IsNullOrEmpty(d.tag) || !string.IsNullOrEmpty(d.mob); }
        /// <summary>Kills counted for a loot source (a drop list id, or a list id and group for pity); 0 when none.</summary>
        public int KillsOf(string source) { var c = Progress.lootLuck.Find(x => x != null && x.source == source); return c != null ? c.kills : 0; }

        // ---------- queries ----------
        public bool IsFound(string itemId) { return itemId != null && found.Contains(itemId); }
        public bool HasSeenLook(string key) { return key != null && seenLooks.Contains(key); }
        public int LooksSeen { get { return seenLooks.Count; } }
        /// <summary>
        /// Found once it has been held. Otherwise known when its source has been met: a boss or camp piece once a list that drops it
        /// has been met, which is when a mob of that list has died or one of the list's pieces has been found, a quest reward once the quest is taken or done, a merchant's piece always (it is on the shelf). Hidden
        /// finds and world drops are unknown until found. Anything without a gear entry is unknown.
        /// </summary>
        public State StateOf(string itemId)
        {
            if (IsFound(itemId)) return State.Found;
            var g = Loot != null ? Loot.Meta(itemId) : null; if (g == null) return State.Unknown;
            string kind = LootDatabase.SourceKind(g.source);
            switch (kind)
            {
                case "boss": case "mob": return revealedBy.TryGetValue(itemId, out var lists) && lists.Exists(l => KillsOf(l) > 0 || (picksOf.TryGetValue(l, out var ps) && ps.Exists(IsFound))) ? State.Known : State.Unknown;
                case "quest":
                    {
                        string q = g.source.Substring(kind.Length + 1);
                        return (Progress.questsDone != null && Progress.questsDone.Contains(q)) || (Progress.quests != null && Progress.quests.Exists(s => s != null && s.id == q)) ? State.Known : State.Unknown;
                    }
                case "vendor": return State.Known;
                default: return State.Unknown;
            }
        }

        /// <summary>One piece as the book shows it. While it is unknown only its slot and kind of source are given: no id, name or place.</summary>
        public sealed class Entry
        {
            public State state;
            /// <summary>The slot id ("mainhand") and the kind of source ("boss", "mob", "quest", "vendor", "secret", "world").</summary>
            public string slot, sourceKind;
            /// <summary>Once known: the item id, its name, its quality and where it comes from in words (LootDatabase.SourceText). Null (quality -1) while unknown.</summary>
            public string id, name, source; public int quality = -1;
        }
        public Entry EntryOf(GearMeta g)
        {
            var d = Items != null ? Items.Get(g.id) : null; var e = new Entry { state = StateOf(g.id), slot = d != null ? d.slot : null, sourceKind = LootDatabase.SourceKind(g.source) };
            if (e.state == State.Unknown) return e;
            e.id = g.id; e.name = d != null ? d.name : g.id; e.quality = d != null ? d.quality : 1; e.source = LootDatabase.SourceText(g.source, Loot.ZoneName);
            return e;
        }
        /// <summary>How an unknown piece of this kind of source is described ("Dropped by a boss"); a hidden find says only "Hidden somewhere".</summary>
        public static string UnknownLine(string sourceKind) { foreach (var k in Kinds) if (k.kind == sourceKind) return k.unknown; return "Somewhere"; }

        /// <summary>One zone (or the world) in the Armoury tab: every named piece that comes from there, in file order, and how many are found.</summary>
        public sealed class Tally
        {
            public string zone, zoneName; public bool here; public int found;
            public readonly List<Entry> entries = new List<Entry>();
            public int Total { get { return entries.Count; } }
        }
        /// <summary>The named pieces of a zone ("oakhaven" or "zone.oakhaven", or <see cref="World"/>), with the found counted.</summary>
        public Tally TallyOf(string zone, string zoneName = null, bool here = false)
        {
            zone = Short(zone); var t = new Tally { zone = zone, zoneName = zoneName ?? zone, here = here };
            if (Loot == null) return t;
            foreach (var g in Loot.GearOrder)
            {
                if (!zoneOf.TryGetValue(g.id, out var z)) zoneOf[g.id] = z = ZoneOf(g, Loot);
                if (z != zone) continue;
                var e = EntryOf(g); t.entries.Add(e); if (e.state == State.Found) t.found++;
            }
            return t;
        }
        static string Short(string zone) { return zone != null && zone.StartsWith("zone.", StringComparison.Ordinal) ? zone.Substring(5) : zone; }
        /// <summary>
        /// The zone a named piece belongs to (its id without "zone.", or <see cref="World"/>): a camp source's zone, a hidden find's
        /// zone, a boss's zone (from its drop list), world sources the world; otherwise the zone in its id ("loot.oak.") or in its
        /// quest's id ("main.oakhaven.1"), else the world.
        /// </summary>
        public static string ZoneOf(GearMeta g, LootDatabase loot)
        {
            string kind = LootDatabase.SourceKind(g.source), rest = kind != null ? g.source.Substring(kind.Length + 1) : "";
            if (kind == "world") return World;
            if (kind == "mob") return rest.Substring(rest.IndexOf('@') + 1);
            if (kind == "secret") { var parts = rest.Split('.'); if (parts.Length >= 3 && parts[0] == "secret") return parts[1]; }
            if (kind == "boss" && loot != null) { var d = loot.Drops.Find(x => x.mob == rest && !string.IsNullOrEmpty(x.zone)); if (d != null) return d.zone; }
            var id = (g.id ?? "").Split('.');
            if (id.Length > 2 && id[0] == "loot") foreach (var (s, z) in ZoneShorts) if (id[1] == s) return z;
            if (kind == "quest") foreach (var part in rest.Split('.')) foreach (var (_, z) in ZoneShorts) if (part == z) return z;
            return World;
        }
    }
}
