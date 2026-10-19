using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Lockouts in play (Chris, 2026-10-15: "bosses/elites will not respawn so no one is trying to exploit the gear/loot drops"; "Until a
    /// lockout resets", "Everywhere"; "open world boss is once per week"; "raids once per week"; the rules and the table are Lockouts').
    /// - A boss or elite your party kills (you, Mira, a party sim or a pet tapped it) is locked for this character: a raid's or an open-world
    ///   boss's for a week, a dungeon's for two days, any other elite's for a day, to the six o'clock reset (Progress.lockouts, save format 11).
    /// - Locked, it is not there: on a zone's load it is put away (dead, its body gone, the door its death opens standing open), and one
    ///   killed this visit goes when it would have respawned; its camp's ordinary mobs keep their 20-minute respawn. Checked every few
    ///   seconds: a lockout run out brings it back with its camp.
    /// - Two exceptions bring a locked one back, never its loot: a queued run's dungeon is fresh for the run (EncounterSession.Queue: every
    ///   boss stands, the locked ones drop nothing), and a boss whose kill a quest you hold still needs stays (the safe way: the quest can
    ///   always be done; it gives its experience and its quest item, no loot and no coin, and its lockout is not pushed back).
    /// - A camp released later (a boss's relief, an event's wave) is a fight brought to you: it comes, and a locked elite in it drops nothing.
    /// - Story enemies (a zone's spawns.enemies) are saved dead for good already (Progress.enemies) and take no lockout.
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>A mob that takes a lockout: a camp's elite (its boss, a rare one, the Rock-Eater under Nix), not a game animal.</summary>
        public static bool Lockable(EncounterEnemy e) { return e != null && e.Camp && e.Elite && !e.Game && Lockouts.KeyOf(e.persistentId) != null; }
        /// <summary>Its lockout's kind here: the zone's (a raid, a dungeon), else an open-world boss's (a boss's move) or an elite's.</summary>
        public LockoutKind LockKindOf(EncounterEnemy e) { return Lockouts.KindFor(Zone != null ? Zone.Zone : null, e != null && (e.RaidBoss || (e.Move != null && e.Move.boss))); }   // a raid boss in the open world: a world boss's week
        /// <summary>Locked for this character now.</summary>
        public bool LockedFor(EncounterEnemy e) { return Lockable(e) && Progress != null && Lockouts.Locked(Progress, Lockouts.KeyOf(e.persistentId)); }
        /// <summary>A quest you hold still needs it dead (QuestLog.StillNeedsKill): then it stands, locked or not, and drops no loot while locked.</summary>
        public bool QuestNeeds(EncounterEnemy e) { return e != null && Quests != null && Quests.StillNeedsKill(e.persistentId); }
        /// <summary>Kept away by its lockout: locked, not in a queued run's fresh dungeon, and no quest you hold needs it.</summary>
        public bool KeptAway(EncounterEnemy e) { return LockedFor(e) && !QueueFresh && !QuestNeeds(e) && !e.RaidBoss; }   // the world boss still rises for you locked: no loot (WORLD_BOSS_1.md 1.4)

        /// <summary>
        /// On a zone's load (Start and Load, once the quests are bound): the run-out lockouts dropped, then every boss and elite still locked
        /// for you put away before it is seen, the doors its death opens set open (else a locked boss's door would shut the dungeon for the
        /// whole lockout), and a rider whose beast is put away down out of the saddle. A word in the chat when any are.
        /// </summary>
        public void SweepLockouts()
        {
            if (Progress == null) return;
            Lockouts.Prune(Progress, Lockouts.UtcNow());
            int n = 0;
            foreach (var e in Enemies.ToArray())
            {
                if (e == null || e.Away || !e.actor.IsAlive || e.Engaged || !KeptAway(e)) continue;
                e.PutAway(); OpenDoorsOfLocked(e); n++;
            }
            foreach (var e in Enemies) if (e != null && e.Inside != null && e.Inside.Away) e.Rise(null);
            if (n > 0) Message((n == 1 ? "One boss or elite here is" : n + " bosses and elites here are") + " down for you until the lockout resets (/lockouts).");
            lockoutCheckAt = Time.time + 5;
        }
        /// <summary>The doors a locked boss's death would open stand open, quietly (ZoneGate.OpensOn).</summary>
        void OpenDoorsOfLocked(EncounterEnemy e)
        {
            if (Zone == null || e == null || string.IsNullOrEmpty(e.MobName)) return;
            foreach (var g in Zone.Gates) if (g != null && !g.Open && g.OpensOn == e.MobName) g.SetOpen(true, true);
        }
        float lockoutCheckAt;
        /// <summary>Every few seconds: lockouts run out are dropped, and a boss or elite put away whose lockout has run out (or whose kill a
        /// quest now needs) comes back with its camp.</summary>
        void TickLockouts()
        {
            if (Progress == null || Time.time < lockoutCheckAt) return;
            lockoutCheckAt = Time.time + 5;
            Lockouts.Prune(Progress, Lockouts.UtcNow());
            foreach (var e in Enemies.ToArray()) if (e != null && e.Away && !KeptAway(e)) e.ComeBack();
        }
        /// <summary>A boss or elite your party has just killed: locked for you (EnemyDied; nothing if it was locked already, so a kill inside
        /// a lockout does not push it back).</summary>
        void LockOnKill(EncounterEnemy e)
        {
            if (!Lockable(e) || Progress == null) return;
            var kind = LockKindOf(e); string key = Lockouts.KeyOf(e.persistentId); var now = Lockouts.UtcNow();
            if (Lockouts.Lock(Progress, key, kind, now))
                Message(e.Name + " is locked for you: back, and lootable, in " + Lockouts.Words(Lockouts.Left(Progress, key, now)) + " (" + LockKindWords(kind) + "; /lockouts).");
        }
        static string LockKindWords(LockoutKind k) { return k == Lockouts.Raid ? "a raid's week" : k == Lockouts.Dungeon ? "a dungeon's two days" : k == Lockouts.WorldBoss ? "a world boss's week" : "an elite's day"; }

        /// <summary>The lockouts running, soonest first, for the dungeon window and /lockouts: who, where, and the time left.</summary>
        public List<(string name, string where, TimeSpan left)> LockoutRows()
        {
            var rows = new List<(string, string, TimeSpan)>(); if (Progress == null) return rows;
            foreach (var (key, left) in Lockouts.Running(Progress, Lockouts.UtcNow()))
            {
                string zone = Lockouts.ZoneOf(key), name = key, where = zone ?? "";
                var def = Zone != null && zone != null ? Zone.FindZone("zone." + zone) : null;
                if (def != null)
                {
                    where = def.displayName; var parts = key.Split('.'); string camp = parts[parts.Length - 1];
                    if (camp.StartsWith("m")) name = RockEaterName;   // the beast under its rider ("mob.rockeater.adit.m21")
                    else if (int.TryParse(camp, out int c) && def.camps != null && c >= 0 && c < def.camps.Length && def.camps[c] != null) name = def.camps[c].mob;
                }
                rows.Add((name, where, left));
            }
            return rows;
        }
        /// <summary>/lockouts clear (a tester's, as /level): every lockout ended now; the bosses and elites put away come back within seconds.</summary>
        public void TesterClearLockouts()
        {
            int n = Progress != null && Progress.lockouts != null ? Progress.lockouts.Count : 0; if (Progress != null) Progress.lockouts.Clear();
            lockoutCheckAt = 0; Message("Testing: " + n + (n == 1 ? " lockout" : " lockouts") + " cleared. The bosses and elites come back with their camps.");
        }
        /// <summary>/lockouts: each running lockout in the chat, with its time left.</summary>
        public void ListLockouts()
        {
            var rows = LockoutRows();
            if (rows.Count == 0) { Message("No lockouts: every boss and elite is there for you to loot. (A kill locks one: a dungeon's two days, a world boss's week, an elite's day, to 06:00.)"); return; }
            Message("Lockouts (" + rows.Count + "), reset at 06:00:");
            foreach (var r in rows) Message("  " + r.name + (r.where.Length > 0 ? ", " + r.where : "") + ": " + Lockouts.Words(r.left));
        }
    }
}
