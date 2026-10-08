using System;
using System.Collections.Generic;
using System.Linq;
using Crulanda.World;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>One achievement: what it is called and asks, its points, the title it gives (or null), and how far along it is.</summary>
    public sealed class AchievementDef
    {
        public string id, name, description, title, group; public int points, goal; public Func<int> progress;
        public int Progress { get { return Mathf.Min(goal, progress != null ? progress() : 0); } }
    }

    /// <summary>
    /// Points of interest and achievements (Chris, 2026-10-03: "need to implement POI in game for discovery and achievement
    /// points"; chosen: every landmark a POI, an Achievements tab with points and titles).
    /// POIs: every zone's landmarks. Walking into one the first time explores it (EncounterProgress.explored, "zone.id|name"),
    /// pays exploration experience by the zone's level and names it on the maps (unexplored places are a "?" there).
    /// Achievements: worked out from the content, not listed by hand: in each zone, explore every place, find every secret,
    /// finish every quest (bounties aside) and kill every camp elite; across the world, all of each, notice-board bounties, a rare
    /// posting and a trade's skill. Each pays points once (EncounterProgress.achievements); some give a title the player can wear
    /// (EncounterProgress.title, shown in the player frame). Pure logic over the progress, like DiscoveryLog: tested without a scene.
    /// GAME-ONLY (names and titles).
    /// </summary>
    public sealed class Achievements
    {
        public const string Kicker = "ACHIEVEMENT";
        public EncounterProgress Progress { get; private set; }
        public readonly List<AchievementDef> All = new List<AchievementDef>();
        public readonly List<ZoneDefinition> Zones;
        readonly QuestDatabase quests;
        /// <summary>An achievement was just earned (the session raises its toast).</summary>
        public Action<AchievementDef> Earned = a => { };

        public Achievements(EncounterProgress progress, IEnumerable<ZoneDefinition> zones, QuestDatabase questDb)
        {
            /* a dungeon gets its deeds with its quests (the Sealed Adit, D6) */ Zones = (zones ?? new ZoneDefinition[0]).Where(z => z != null && !z.dungeon).OrderBy(z => z.levelMin).ThenBy(z => z.id, StringComparer.Ordinal).ToList();
            quests = questDb; Bind(progress); Build();
        }
        public void Bind(EncounterProgress progress)
        {
            Progress = progress;
            if (Progress.explored == null) Progress.explored = new List<string>();
            if (Progress.achievements == null) Progress.achievements = new List<string>();
            if (Progress.elitesSlain == null) Progress.elitesSlain = new List<string>();
        }

        // ---------- points of interest ----------
        public static string PoiKey(ZoneDefinition z, ZoneLabel l) { return z.id + "|" + l.name; }
        public static IEnumerable<ZoneLabel> Pois(ZoneDefinition z)
        {
            var seen = new HashSet<string>();
            foreach (var l in z.landmarks ?? new ZoneLabel[0]) if (l != null && !string.IsNullOrEmpty(l.name) && seen.Add(l.name)) yield return l;
        }
        /// <summary>How close counts as being there: the landmark's own radius, kept between 6 and 18 m.</summary>
        public static float Reach(ZoneLabel l) { return Mathf.Clamp(l.radius, 6, 18); }
        /// <summary>Exploration experience: more in the higher zones (16 at Oakhaven's level 1, 63 at the Peaks' level 6).</summary>
        public static int PoiXp(ZoneDefinition z) { return 15 + 8 * Mathf.Max(1, z.levelMin); }
        public bool Explored(ZoneDefinition z, ZoneLabel l) { return z != null && l != null && Progress.explored.Contains(PoiKey(z, l)); }
        /// <summary>Marks the place explored. False when it already was.</summary>
        public bool Explore(ZoneDefinition z, ZoneLabel l)
        {
            if (z == null || l == null || string.IsNullOrEmpty(l.name) || Explored(z, l)) return false;
            Progress.explored.Add(PoiKey(z, l)); return true;
        }
        public int PoisIn(ZoneDefinition z) { return Pois(z).Count(); }
        public int ExploredIn(ZoneDefinition z) { return Pois(z).Count(l => Explored(z, l)); }

        // ---------- elites ----------
        static string Short(ZoneDefinition z) { return z.id.StartsWith("zone.") ? z.id.Substring(5) : z.id; }
        /// <summary>A camp elite's key, from a mob's persistent id ("mob.captain.peaks.3.0" -> "mob.captain.peaks.3").</summary>
        public static string EliteKey(string persistentId)
        {
            if (string.IsNullOrEmpty(persistentId)) return null; int i = persistentId.LastIndexOf('.');
            return i > 0 ? persistentId.Substring(0, i) : persistentId;
        }
        public static List<string> EliteKeys(ZoneDefinition z)
        {
            var list = new List<string>(); if (z.camps == null) return list;
            for (int i = 0; i < z.camps.Length; i++) if (z.camps[i] != null && z.camps[i].elite) list.Add("mob." + z.camps[i].tag + "." + Short(z) + "." + i);
            return list;
        }
        public void Slain(string persistentId) { var k = EliteKey(persistentId); if (k != null && !Progress.elitesSlain.Contains(k)) Progress.elitesSlain.Add(k); }

        // ---------- counts ----------
        List<string> ZoneQuests(ZoneDefinition z) { return quests == null ? new List<string>() : quests.Ordered.Where(q => q != null && q.zone == z.id && q.kind != "bounty").Select(q => q.id).ToList(); }
        static List<string> ZoneSecrets(ZoneDefinition z) { return (z.secrets ?? new ZoneSecret[0]).Where(s => s != null && !string.IsNullOrEmpty(s.id)).Select(s => s.id).Distinct().ToList(); }
        int Done(List<string> ids, List<string> have) { return have == null ? 0 : ids.Count(have.Contains); }
        int TopSkill() { return Progress.professions == null || Progress.professions.Count == 0 ? 0 : Progress.professions.Max(p => p.skill); }

        void Add(string id, string name, string description, int points, string group, int goal, Func<int> progress, string title = null)
        { if (goal > 0) All.Add(new AchievementDef { id = id, name = name, description = description, points = points, group = group, goal = goal, progress = progress, title = title }); }
        void Build()
        {
            foreach (var z in Zones)
            {
                var zone = z; string g = z.displayName; var qs = ZoneQuests(z); var secrets = ZoneSecrets(z); var elites = EliteKeys(z);
                Add("explore." + Short(z), "Explorer of " + g, "Visit every place in " + g + ".", 10, g, PoisIn(z), () => ExploredIn(zone));
                Add("secrets." + Short(z), "Secrets of " + g, "Find everything hidden in " + g + ".", 10, g, secrets.Count, () => Done(secrets, Progress.discoveries));
                Add("quests." + Short(z), "The Deeds of " + g, "Finish every quest in " + g + " (bounties aside).", 15, g, qs.Count, () => Done(qs, Progress.questsDone));
                Add("elites." + Short(z), "Terror of " + g, "Kill every camp elite in " + g + ".", 10, g, elites.Count, () => Done(elites, Progress.elitesSlain));
            }
            const string W = "Crulanda";
            Add("explore.all", "The Wayfarer", "Visit every place in every zone.", 25, W, Zones.Sum(PoisIn), () => Zones.Sum(ExploredIn), "the Wayfarer");
            var allSecrets = Zones.SelectMany(ZoneSecrets).Distinct().ToList();
            Add("secrets.all", "Keeper of Secrets", "Find everything hidden in every zone.", 25, W, allSecrets.Count, () => Done(allSecrets, Progress.discoveries), "Keeper of Secrets");
            var allQuests = Zones.SelectMany(ZoneQuests).Distinct().ToList();
            Add("quests.all", "The Steadfast", "Finish every quest in every zone (bounties aside).", 25, W, allQuests.Count, () => Done(allQuests, Progress.questsDone), "the Steadfast");
            var allElites = Zones.SelectMany(EliteKeys).ToList();
            Add("elites.all", "The Unbowed", "Kill every camp elite in every zone.", 25, W, allElites.Count, () => Done(allElites, Progress.elitesSlain), "the Unbowed");
            Add("bounty.10", "Paid in Crowns", "Hand in ten notice-board bounties.", 10, W, 10, () => Progress.bounties);
            Add("bounty.50", "Bounty Hunter", "Hand in fifty notice-board bounties.", 25, W, 50, () => Progress.bounties, "Bounty Hunter");
            Add("rare.1", "The Courier's Bane", "See a rare posting through: the Bureau courier and the shard he carries.", 10, W, 1, () => Progress.rares, "the Courier's Bane");
            Add("trade.75", "Journeyman", "Raise a trade to 75 skill.", 10, W, 75, TopSkill);
            Add("trade.100", "Master of a Trade", "Raise a trade to 100 skill.", 20, W, 100, TopSkill, "Master of a Trade");
        }

        // ---------- earning ----------
        public bool Has(string id) { return Progress.achievements.Contains(id); }
        public int Points { get { return All.Where(a => Has(a.id)).Sum(a => a.points); } }
        public int Total { get { return All.Sum(a => a.points); } }
        public List<string> Groups { get { return All.Select(a => a.group).Distinct().ToList(); } }
        /// <summary>Earns every achievement now met. Quiet (a save just loaded: what was done before is recorded without a toast each)
        /// raises nothing. Returns what was earned.</summary>
        public List<AchievementDef> Check(bool quiet = false)
        {
            var got = new List<AchievementDef>();
            foreach (var a in All)
            {
                if (Has(a.id) || a.Progress < a.goal) continue;
                Progress.achievements.Add(a.id); got.Add(a);
                if (!quiet) Earned(a);
            }
            return got;
        }
        public List<string> Titles { get { return All.Where(a => a.title != null && Has(a.id)).Select(a => a.title).ToList(); } }
        /// <summary>Wears an earned title, or none (null).</summary>
        public bool Wear(string title)
        {
            if (title != null && !Titles.Contains(title)) return false;
            Progress.title = title; return true;
        }
    }
}
