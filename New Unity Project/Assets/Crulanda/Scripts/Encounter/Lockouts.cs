using System;
using System.Collections.Generic;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>One boss or elite this character has killed and may not loot again yet: its key (<see cref="Lockouts.KeyOf"/>) and when the
    /// lockout ends, as UTC ticks. Saved in <see cref="EncounterProgress.lockouts"/> (save format 11).</summary>
    [Serializable] public sealed class Lockout { public string id; public long until; }

    /// <summary>A kind of lockout and how many resets it lasts (the table below; Chris, 2026-10-15).</summary>
    public sealed class LockoutKind
    {
        public string id, name; public int days;
        public LockoutKind(string id, string name, int days) { this.id = id; this.name = name; this.days = days; }
    }

    /// <summary>
    /// Lockouts (Chris, 2026-10-15, verbatim: "bosses/elites will not respawn so no one is trying to exploit the gear/loot drops"; asked,
    /// "Until a lockout resets" and "Everywhere"; then "the dungeons should include an instance que that can be done every 2 days for loot",
    /// "open world boss is once per week" and "raids once per week"). Every boss and elite your party kills, in a dungeon or out in the
    /// world, is killed and looted once a lockout by this character: it stays dead for you until the lockout resets, at six in the morning
    /// local time. How long, by the table (<see cref="Kinds"/>): a raid zone's (ZoneDefinition.raid; none built yet) a week, a dungeon's
    /// bosses and elites two days, an open-world boss (an elite with a boss's move, EliteMove.boss: Caddock, the Hollow Root-Warden) a week,
    /// any other open-world elite a day. The ordinary mobs keep their camp respawn. Pure logic over the progress (tested without a scene,
    /// LockoutTests); the session applies it (EncounterSession.Lockout.cs). GAME-ONLY.
    /// </summary>
    public static class Lockouts
    {
        /// <summary>The hour of the morning, local time, that every lockout resets at.</summary>
        public const int ResetHour = 6;
        public static readonly LockoutKind Raid = new LockoutKind("raid", "Raid", 7), Dungeon = new LockoutKind("dungeon", "Dungeon", 2),
            WorldBoss = new LockoutKind("worldboss", "World boss", 7), WorldElite = new LockoutKind("elite", "Elite", 1);
        /// <summary>The lockout table, the first that fits wins (<see cref="KindFor"/>).</summary>
        public static readonly LockoutKind[] Kinds = { Raid, Dungeon, WorldBoss, WorldElite };
        /// <summary>The clock (tests set it). UTC.</summary>
        public static Func<DateTime> UtcNow = () => DateTime.UtcNow;
        /// <summary>The player's time zone, for the six o'clock reset (tests set it).</summary>
        public static TimeZoneInfo Local = TimeZoneInfo.Local;

        /// <summary>Which lockout a kill in this zone takes: a raid zone's, a dungeon's, an open-world boss's (<paramref name="boss"/>: an
        /// elite with a boss's move) or an open-world elite's.</summary>
        public static LockoutKind KindFor(ZoneDefinition zone, bool boss) { return KindFor(zone != null && zone.raid, zone != null && zone.dungeon, boss); }
        public static LockoutKind KindFor(bool raid, bool dungeon, bool boss) { return raid ? Raid : dungeon ? Dungeon : boss ? WorldBoss : WorldElite; }

        /// <summary>The key a boss or elite is locked by: its camp's ("mob.captain.peaks.3" from "mob.captain.peaks.3.0", as the deeds
        /// count it, Achievements.EliteKey). Null for an id that is not a camp mob's.</summary>
        public static string KeyOf(string persistentId)
        {
            if (string.IsNullOrEmpty(persistentId) || !persistentId.StartsWith("mob.", StringComparison.Ordinal)) return null;
            return Achievements.EliteKey(persistentId);
        }
        /// <summary>The zone a key's mob belongs to ("peaks" from "mob.captain.peaks.3"), or null.</summary>
        public static string ZoneOf(string key) { var parts = (key ?? "").Split('.'); return parts.Length >= 4 ? parts[2] : null; }

        /// <summary>
        /// When a lockout taken at <paramref name="utcNow"/> ends: the first six o'clock (local) after the kill, and <paramref name="days"/> - 1
        /// days after that. So a day's lockout taken at 23:00 ends at 06:00 the next morning, and one taken at 07:00 at 06:00 the morning after;
        /// a two-day one taken on a Monday evening ends at 06:00 on Wednesday; a week's ends seven resets on. UTC.
        /// </summary>
        public static DateTime ResetAfter(DateTime utcNow, int days, TimeZoneInfo zone = null)
        {
            zone = zone ?? Local; if (utcNow.Kind != DateTimeKind.Utc) utcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
            var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone);
            var reset = local.Date.AddHours(ResetHour); if (reset <= local) reset = reset.AddDays(1);
            reset = DateTime.SpecifyKind(reset.AddDays(Math.Max(1, days) - 1), DateTimeKind.Unspecified);
            try { return TimeZoneInfo.ConvertTimeToUtc(reset, zone); }
            catch (ArgumentException) { return TimeZoneInfo.ConvertTimeToUtc(reset.AddHours(1), zone); }   // six o'clock skipped by a clock change: an hour on
        }

        static List<Lockout> ListOf(EncounterProgress p) { if (p.lockouts == null) p.lockouts = new List<Lockout>(); return p.lockouts; }
        /// <summary>The lockout on this key, if it has not run out by <paramref name="utcNow"/>.</summary>
        public static Lockout Find(EncounterProgress p, string key, DateTime utcNow)
        {
            if (p == null || string.IsNullOrEmpty(key)) return null;
            var l = ListOf(p).Find(x => x != null && x.id == key);
            return l != null && l.until > utcNow.Ticks ? l : null;
        }
        public static bool Locked(EncounterProgress p, string key, DateTime utcNow) { return Find(p, key, utcNow) != null; }
        public static bool Locked(EncounterProgress p, string key) { return Locked(p, key, UtcNow()); }
        /// <summary>Locks a key from <paramref name="utcNow"/> for its kind (false, and nothing changed, if it is locked already: a kill
        /// inside a lockout does not push it back).</summary>
        public static bool Lock(EncounterProgress p, string key, LockoutKind kind, DateTime utcNow, TimeZoneInfo zone = null)
        {
            if (p == null || string.IsNullOrEmpty(key) || kind == null || Locked(p, key, utcNow)) return false;
            var list = ListOf(p); list.RemoveAll(x => x == null || x.id == key);
            list.Add(new Lockout { id = key, until = ResetAfter(utcNow, kind.days, zone).Ticks });
            return true;
        }
        /// <summary>Drops the lockouts that have run out (and blanks). How many went.</summary>
        public static int Prune(EncounterProgress p, DateTime utcNow) { return p == null ? 0 : ListOf(p).RemoveAll(x => x == null || string.IsNullOrEmpty(x.id) || x.until <= utcNow.Ticks); }
        /// <summary>How long until the lockout on a key ends (zero when there is none).</summary>
        public static TimeSpan Left(EncounterProgress p, string key, DateTime utcNow)
        {
            var l = Find(p, key, utcNow); return l == null ? TimeSpan.Zero : TimeSpan.FromTicks(l.until - utcNow.Ticks);
        }
        /// <summary>A time left in words: "1 d 4 h", "3 h 12 m", "9 m", "under a minute".</summary>
        public static string Words(TimeSpan t)
        {
            if (t.TotalMinutes < 1) return "under a minute";
            if (t.TotalDays >= 1) return (int)t.TotalDays + " d " + t.Hours + " h";
            if (t.TotalHours >= 1) return (int)t.TotalHours + " h " + t.Minutes + " m";
            return (int)t.TotalMinutes + " m";
        }
        /// <summary>
        /// Whether a kill rolls its loot: always for a mob that takes no lockout (<paramref name="lockable"/> false: an ordinary mob, a game
        /// animal); for a boss or elite, only when it is not locked for this character. A locked boss met anyway (a queued run's fresh dungeon,
        /// or one a quest you hold still needs, EncounterSession.KeptAway) gives its experience and its quest item, never its loot.
        /// </summary>
        public static bool RollsLoot(EncounterProgress p, string persistentId, bool lockable, DateTime utcNow)
        {
            return !lockable || !Locked(p, KeyOf(persistentId), utcNow);
        }
        /// <summary>The lockouts still running, soonest to end first: (key, time left).</summary>
        public static List<(string key, TimeSpan left)> Running(EncounterProgress p, DateTime utcNow)
        {
            var list = new List<(string, TimeSpan)>(); if (p == null) return list;
            foreach (var l in ListOf(p)) if (l != null && !string.IsNullOrEmpty(l.id) && l.until > utcNow.Ticks) list.Add((l.id, TimeSpan.FromTicks(l.until - utcNow.Ticks)));
            list.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            return list;
        }
        /// <summary>A save's list made sound on load: none is empty; a blank id, an expiry of zero or less, and a later entry for a key
        /// already listed are dropped (the first is kept). Run-out lockouts are kept until the session prunes them.</summary>
        public static void Normalize(EncounterProgress p)
        {
            if (p == null) return; var list = ListOf(p);
            list.RemoveAll(l => l == null || string.IsNullOrEmpty(l.id) || l.until <= 0);
            var once = new HashSet<string>(StringComparer.Ordinal); list.RemoveAll(l => !once.Add(l.id));
        }
    }
}
