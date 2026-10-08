using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Core;
using Crulanda.Persistence;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A SimAdventurer's profile (Phase 5.2 round 1, 2026-10-06; GAME-BRIEF section 13, Docs/SIMPLAYER_DESIGN.md): one of the
    /// twenty other adventurers who play alongside you. Stable id, a name and where they are from (GAME-ONLY names; the folk
    /// labels are origins, not canon races, until the canon is checked), one of the five class kits, a level with a home zone to
    /// match, a personality (three weights the later phases' choices and chat read), the hours they are online (world-clock
    /// hours), and where they were last seen. Saved in the world slot (WorldSave), shared by every character.
    /// </summary>
    [Serializable]
    public sealed class SimAdventurer
    {
        public string id, name, folk, classId, homeZone, zone;
        public int level = 1, variant, gearSeed;
        /// <summary>0-1 each: bold (fights above their level), friendly (helps, greets), chatty (talks in the channels).</summary>
        public float bold, friendly, chatty;
        /// <summary>Online from this world-clock hour for <see cref="onlineHours"/> hours (wrapping past midnight).</summary>
        public float onlineFrom = 8, onlineHours = 8;
        public float x, z;
        // 5.3b-c (2026-10-06): experience (level stays the saved truth; XpForLevel(level) is its floor), the purse, what it carries
        // (parallel lists: JsonUtility keeps no dictionaries), the gear it forged (slot and item id), the upgrades bought, and the
        // world-clock hour it next thinks of moving zone while unseen. A world slot from before reads them as empty (format 1 still).
        public int experience, coin, gearBonus;
        public List<string> goodIds = new List<string>(); public List<int> goodCounts = new List<int>();
        public List<string> wornSlots = new List<string>(); public List<string> wornIds = new List<string>();
        public float nextTravelHour = -1;
        /// <summary>5.4: the world-clock hour its unseen life was last reckoned (SimPopulation.LiveAway); -1 never.</summary>
        public float lastAwayHour = -1;
        /// <summary>5.5 (2026-10-07): what it makes of you, from what you have done together (SimMemory); 0 a stranger.</summary>
        public int regard;
        /// <summary>On your friends list (/friend).</summary>
        public bool friend;
        /// <summary>The last world-clock hour it greeted you, so a friend says hello once a session, not every refresh.</summary>
        public float greetedHour = -1;
        /// <summary>Its guild (round 27; SimGuilds), "" none.</summary>
        public string guild = "";
        public bool IsOnline(float hour) { return Crulanda.World.WorldClock.Between(onlineFrom, Mathf.Repeat(onlineFrom + onlineHours, 24)) || onlineHours >= 24; }
        public bool IsOnlineAt(float hour) { float to = Mathf.Repeat(onlineFrom + onlineHours, 24); return onlineHours >= 24 || (onlineFrom <= to ? hour >= onlineFrom && hour < to : hour >= onlineFrom || hour < to); }
    }

    /// <summary>The world's own save (slot "world", beside the characters' slots): the sims. Format 1.</summary>
    [Serializable]
    public sealed class WorldSave
    {
        public const int FormatVersion = 1;
        public const string PayloadType = "CrulandaWorld";
        public int seed;
        public List<SimAdventurer> sims = new List<SimAdventurer>();
        /// <summary>The sims in your party when last saved (round 27): they are with you again in the next zone or the next session.</summary>
        public List<string> party = new List<string>();
        /// <summary>How many sims (in roster order) have had their guild chosen (round 27): an older world assigns them on load.</summary>
        public int guildedUpTo;
    }

    /// <summary>Makes, reads and writes the roster.</summary>
    public static class SimRoster
    {
        public const int Count = 40;   // round 27 (sims item 6): forty, the second twenty mostly at the higher levels
        public const string Slot = "world";
        public static readonly string[] ClassIds = { "class.warrior", "class.druid", "class.paladin", "class.ranger", "class.mage" };
        /// <summary>Home zones by level band (WORLD_ZONES.md): the village for the low levels, the Shore for the cap.</summary>
        public static readonly (string zone, string folk, int lo, int hi)[] Homes = {
            ("zone.oakhaven", "Oakhaven folk", 1, 5), ("zone.khaven", "Khaven folk", 5, 9), ("zone.peaks", "Peaks folk", 9, 12), ("zone.ashrim", "Rim folk", 12, 15), ("zone.verdant", "Shore folk", 13, 15) };   // round 29 bands
        // GAME-ONLY names, plain and of the Trail: none from the books.
        static readonly string[] First = { "Ansel", "Bryn", "Cato", "Della", "Edric", "Fenna", "Garrick", "Hollis", "Isolde", "Jory", "Kestrel", "Lowen", "Maren", "Nolan", "Orla", "Piran", "Quill", "Rhosyn", "Sedge", "Tamsin", "Ulric", "Vesna", "Wren", "Yorath" };
        static readonly string[] Bynames = { "Ashby", "Brook", "Coombe", "Dray", "Fallow", "Greave", "Hale", "Kettle", "Larkin", "Marl", "Nettle", "Oxley", "Pike", "Rooke", "Sallow", "Thatch", "Underhill", "Wick" };

        /// <summary>The forty, the same for a seed: names unique, every class at least three times, levels spread 1-13 with homes to match.
        /// The first twenty are those of a world made before round 27 (the same draws), so an old world grows by the second twenty.</summary>
        public static List<SimAdventurer> Generate(int seed)
        {
            var r = new SeededRandom(seed); var sims = new List<SimAdventurer>(Count); var used = new HashSet<string>();
            for (int i = 0; i < Count; i++)
            {
                var s = new SimAdventurer { id = "sim." + (i + 1).ToString("00"), classId = ClassIds[i % ClassIds.Length] };
                // Levels: a spread over the bands, more at the bottom where the player starts (1-5: 8, 4-8: 5, 7-10: 3, 9-12: 2, 11-13: 2).
                int band = i < 8 ? 0 : i < 13 ? 1 : i < 16 ? 2 : i < 18 ? 3 : i < 20 ? 4 : (i - 20) % 5;   // the second twenty: four a band (12/9/7/6/6 in all)
                var home = Homes[band];
                s.level = home.lo + r.NextInt(0, home.hi - home.lo + 1);
                s.homeZone = s.zone = home.zone; s.folk = home.folk;
                string name;
                do { name = First[r.NextInt(0, First.Length)] + " " + Bynames[r.NextInt(0, Bynames.Length)]; } while (!used.Add(name));
                s.name = name; s.variant = r.NextInt(0, 1000); s.gearSeed = r.NextInt(0, 100000);
                s.bold = r.NextFloat(); s.friendly = r.NextFloat(); s.chatty = r.NextFloat();
                // Hours: most play evenings, some mornings, a few all hours (the ones you always see).
                float kind = r.NextFloat();
                if (kind < .15f) { s.onlineFrom = 0; s.onlineHours = 24; }
                else if (kind < .4f) { s.onlineFrom = 6 + r.NextInt(0, 4); s.onlineHours = 5 + r.NextInt(0, 5); }
                else { s.onlineFrom = 15 + r.NextInt(0, 5); s.onlineHours = 4 + r.NextInt(0, 7); }
                sims.Add(s);
            }
            return sims;
        }

        public static WorldSave Load(string root, out string message)
        {
            message = null;
            var store = new SaveFileStore(root);
            if (!store.Exists(Slot)) return null;
            if (!store.TryRead(Slot, out var env, out message)) return null;
            if (env.payloadType != WorldSave.PayloadType || env.formatVersion < 1 || env.formatVersion > WorldSave.FormatVersion) { message = "Unsupported world save."; return null; }
            try
            {
                var w = JsonUtility.FromJson<WorldSave>(env.payloadJson);
                if (w == null || w.sims == null || w.sims.Count == 0) { message = "Empty world save."; return null; }
                foreach (var s in w.sims) if (s == null || string.IsNullOrEmpty(s.id) || string.IsNullOrEmpty(s.name) || Array.IndexOf(ClassIds, s.classId) < 0 || s.level < 1 || s.level > EncounterProgress.LevelCap) { message = "Invalid sim in the world save."; return null; }
                return w;
            }
            catch (Exception e) { message = e.Message; return null; }
        }
        public static void Save(string root, WorldSave world)
        {
            new SaveFileStore(root).Write(Slot, new SaveEnvelope {
                formatVersion = WorldSave.FormatVersion, payloadType = WorldSave.PayloadType, gameVersion = "0.3.0",
                savedAtUtc = DateTime.UtcNow.ToString("O"), payloadJson = JsonUtility.ToJson(world) });
        }
        /// <summary>The saved world, or a new one (seeded from the clock) saved at once.</summary>
        public static WorldSave LoadOrCreate(string root)
        {
            var w = Load(root, out _);
            if (w != null) { if (Grow(w) | SimGuilds.Assign(w)) Save(root, w); return w; }
            w = new WorldSave { seed = Environment.TickCount & 0x7fffffff }; w.sims = Generate(w.seed); SimGuilds.Assign(w); Save(root, w); return w;
        }
        /// <summary>A world with fewer sims than <see cref="Count"/> (made before round 27) gains the rest, as its seed makes them; a
        /// name already taken is drawn again. True when it grew.</summary>
        public static bool Grow(WorldSave w)
        {
            if (w == null || w.sims.Count >= Count) return false;
            var all = Generate(w.seed); var used = new HashSet<string>(); var ids = new HashSet<string>();
            foreach (var s in w.sims) { used.Add(s.name); ids.Add(s.id); }
            var r = new SeededRandom(w.seed + 4111);
            for (int i = w.sims.Count; i < Count; i++)
            {
                var s = all[i]; if (ids.Contains(s.id)) continue;
                while (!used.Add(s.name)) s.name = First[r.NextInt(0, First.Length)] + " " + Bynames[r.NextInt(0, Bynames.Length)];
                w.sims.Add(s);
            }
            return true;
        }
        public static string ClassName(string classId)
        {
            switch (classId) { case "class.druid": return "Druid"; case "class.paladin": return "Paladin"; case "class.ranger": return "Ranger"; case "class.mage": return "Mage"; case "class.rogue": return "Rogue"; default: return "Warrior"; }
        }
    }
}
