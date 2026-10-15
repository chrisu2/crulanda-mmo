using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// What the world says about its secrets and its people (Phase 8.6 3c-3e; Chris, 2026-10-14 23:40: "find recipes/ book of lore/
    /// still havent found many secrets. all this for ALL Zones", 2026-10-15 01:05: "improve polish, secrets, lore, clues, stories from
    /// npcs, etc"):
    /// - every secret has a clue (the Discoveries tab's line for one not yet found, a riddle in the book's voice) and a hint (what
    ///   a villager says about it, in theirs), neither naming a landmark on the map (DiscoveryLog.Validate);
    /// - every named villager and resident has two or three stories, memories, grudges and hopes tied to the canon, said one time in
    ///   three when spoken to (VillageLife.LineFor);
    /// - the zone's data carries them (ZoneSecret.clue and hint, ZoneResident.stories, ZoneLife.stories for the villagers who are not
    ///   residents), and a side file, Resources/Lore/&lt;zone&gt;.json, carries any written apart from the zone file (Oakhaven's,
    ///   written 2026-10-15 while another hand had oakhaven.json; to be folded into it), merged in by <see cref="Apply"/>.
    /// Pure logic over the zone's data, like DiscoveryLog, so it is tested without a scene (ZoneLoreTests).
    /// All the lines are GAME-ONLY; what they tell of (the Wasting, Hallow's Creek, the Council's collectors, the Hollow Men, the
    /// Silent Pilgrims, the Salt-Menders, the Sandthrone, the Pale Things, the Ash-Walkers, the Keepers) is CANON or CANON-EXPANDED
    /// as each zone's canonNote says.
    /// </summary>
    public static class ZoneLore
    {
        /// <summary>A side file: a zone's clues and hints by secret id, and stories by name, written apart from the zone file.</summary>
        [Serializable] public sealed class SideFile { public string zone, canonStatus; public SecretLore[] secrets = new SecretLore[0]; public ZoneStory[] stories = new ZoneStory[0]; }
        [Serializable] public sealed class SecretLore { public string id, clue, hint; }

        static readonly Dictionary<string, SideFile> sides = new Dictionary<string, SideFile>(StringComparer.Ordinal);
        static readonly ConditionalWeakTable<ZoneDefinition, object> applied = new ConditionalWeakTable<ZoneDefinition, object>();

        /// <summary>"zone.oakhaven" to "oakhaven": the side file's name under Resources/Lore.</summary>
        public static string Short(string zoneId) { return string.IsNullOrEmpty(zoneId) ? "" : zoneId.StartsWith("zone.", StringComparison.Ordinal) ? zoneId.Substring(5) : zoneId; }
        /// <summary>The side file for a zone, read once from Resources/Lore/&lt;zone&gt;.json; null when the zone has none.</summary>
        public static SideFile Side(string zoneId)
        {
            string key = Short(zoneId); if (key.Length == 0) return null;
            if (sides.TryGetValue(key, out var side)) return side;
            var text = Resources.Load<TextAsset>("Lore/" + key);
            side = text == null ? null : ParseSide(text.text);
            sides[key] = side;
            return side;
        }
        public static SideFile ParseSide(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonUtility.FromJson<SideFile>(json); } catch (Exception e) { Debug.LogWarning("Unreadable lore side file: " + e.Message); return null; }
        }
        /// <summary>Merges the zone's side file into its data (see <see cref="Apply(ZoneDefinition, SideFile)"/>), once per parsed zone.</summary>
        public static void Apply(ZoneDefinition z)
        {
            if (z == null || applied.TryGetValue(z, out _)) return;
            applied.Add(z, null);
            Apply(z, Side(z.id));
        }
        /// <summary>
        /// Merges a side file into the zone's data: a clue or hint a secret lacks, and the stories of people the zone's own lists
        /// don't give any (a resident's go on the resident, anyone else's on life.stories). The zone's own text always wins, so folding
        /// the side file into the zone file later changes nothing. Harmless done twice.
        /// </summary>
        public static void Apply(ZoneDefinition z, SideFile side)
        {
            if (z == null || side == null) return;
            if (side.secrets != null && z.secrets != null)
                foreach (var l in side.secrets)
                {
                    if (l == null || string.IsNullOrEmpty(l.id)) continue;
                    foreach (var s in z.secrets)
                    {
                        if (s == null || s.id != l.id) continue;
                        if (string.IsNullOrEmpty(s.clue)) s.clue = l.clue;
                        if (string.IsNullOrEmpty(s.hint)) s.hint = l.hint;
                    }
                }
            if (side.stories == null || side.stories.Length == 0) return;
            if (z.life == null) z.life = new ZoneLife();
            var extra = new List<ZoneStory>();
            foreach (var st in side.stories)
            {
                if (st == null || string.IsNullOrEmpty(st.name) || st.lines == null || st.lines.Length == 0) continue;
                if (StoriesOf(z, st.name) != null) continue;
                var r = Resident(z, st.name);
                if (r != null) r.stories = st.lines; else extra.Add(st);
            }
            if (extra.Count > 0)
            {
                var list = new List<ZoneStory>(z.life.stories ?? new ZoneStory[0]); list.AddRange(extra);
                z.life.stories = list.ToArray();
            }
        }

        // ---------- clues ----------
        /// <summary>The book's line for a secret not yet found: its clue, else its hint, else null.</summary>
        public static string Clue(ZoneSecret s) { return s == null ? null : !string.IsNullOrEmpty(s.clue) ? s.clue : !string.IsNullOrEmpty(s.hint) ? s.hint : null; }
        /// <summary>What a villager says of a secret: its hint, else its clue, else null.</summary>
        public static string Hint(ZoneSecret s) { return s == null ? null : !string.IsNullOrEmpty(s.hint) ? s.hint : !string.IsNullOrEmpty(s.clue) ? s.clue : null; }
        /// <summary>
        /// A villager's line about one of this zone's secrets not yet found, chosen at random among those that can be found now (a
        /// chest whose key isn't found yet waits; its key is spoken of instead). Null when every secret is found, or none has a hint.
        /// </summary>
        public static string SecretHint(ZoneDefinition z, DiscoveryLog log, System.Random rng)
        {
            if (z == null || z.secrets == null || rng == null) return null;
            Apply(z);
            var open = new List<string>();
            foreach (var s in z.secrets)
            {
                if (s == null || string.IsNullOrEmpty(s.id)) continue;
                if (log != null && (log.IsFound(s) || !log.Unlocked(s))) continue;
                var h = Hint(s); if (h != null) open.Add(h);
            }
            return open.Count == 0 ? null : open[rng.Next(open.Count)];
        }

        // ---------- stories ----------
        static ZoneResident Resident(ZoneDefinition z, string name)
        {
            if (z.life == null || z.life.residents == null) return null;
            foreach (var r in z.life.residents) if (r != null && r.name == name) return r;
            return null;
        }
        /// <summary>A named person's stories from the zone's data (the resident's own, else life.stories), or null for none.</summary>
        public static string[] StoriesOf(ZoneDefinition z, string name)
        {
            if (z == null || string.IsNullOrEmpty(name)) return null;
            var r = Resident(z, name); if (r != null && r.stories != null && r.stories.Length > 0) return r.stories;
            if (z.life != null && z.life.stories != null)
                foreach (var st in z.life.stories) if (st != null && st.name == name && st.lines != null && st.lines.Length > 0) return st.lines;
            return null;
        }
        /// <summary>One of a named person's stories at random, the side file merged in first; null for someone with none.</summary>
        public static string Story(ZoneDefinition z, string name, System.Random rng)
        {
            if (z == null || rng == null) return null;
            Apply(z);
            var lines = StoriesOf(z, name); if (lines == null) return null;
            var line = lines[rng.Next(lines.Length)];
            return string.IsNullOrEmpty(line) ? null : line;
        }
        /// <summary>Everyone the zone's data gives stories to (residents and life.stories), after the side file.</summary>
        public static List<string> StoryNames(ZoneDefinition z)
        {
            var names = new List<string>(); if (z == null) return names;
            Apply(z);
            if (z.life == null) return names;
            // A person whose lines are all blank has no story (as Story() tells it): the test's "Blank", 2026-10-15 01:55.
            bool Any(string[] lines) { if (lines == null) return false; foreach (var l in lines) if (!string.IsNullOrWhiteSpace(l)) return true; return false; }
            foreach (var r in z.life.residents ?? new ZoneResident[0]) if (r != null && Any(r.stories) && !names.Contains(r.name)) names.Add(r.name);
            foreach (var st in z.life.stories ?? new ZoneStory[0]) if (st != null && Any(st.lines) && !names.Contains(st.name)) names.Add(st.name);
            return names;
        }
    }
}
