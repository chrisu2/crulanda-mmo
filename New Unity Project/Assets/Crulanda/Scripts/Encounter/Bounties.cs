using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A zone's notice board and what hangs on it today (2026-10-03, Chris: "zone bounty boards, rare quests appear 1-2% of the
    /// time giving valuable components for building gear"). Each game day the board draws <see cref="Slots"/> postings from the
    /// zone's pool (quests of kind "bounty" in Quests/bounties.json, giver "board"): village bounties paid in silver crowns, and
    /// Sandthrone contracts (CANON company) that pay more and cost Salt-Mender standing. Each slot has <see cref="RareChance"/>
    /// of being the zone's rare posting instead: a Bureau courier crossing the zone with an Aether-Geode shard (CANON: the
    /// Council's resonance fuel, fossilised memory), the component for resonance-tempered gear at the forge. Bounties are
    /// repeatable: a finished one leaves the board for the day and may be drawn again another day. The draw is seeded by the
    /// zone and the day, so a reload shows the same notices. State lives in Progress.boards (no save format change: new fields
    /// load empty from older saves).
    /// </summary>
    [Serializable] public sealed class BoardState { public string zone; public int day; public List<string> ids = new List<string>(); public List<string> done = new List<string>(); }

    public sealed class Bounties
    {
        public const int Slots = 3;
        /// <summary>The chance, per slot per day, of the rare posting (Chris: 1-2%).</summary>
        public const float RareChance = .015f;
        public const string BoardName = "Notice board";
        readonly QuestLog log;
        public Bounties(QuestLog log) { this.log = log; }
        EncounterProgress Progress { get { return log.Progress; } }

        /// <summary>The zone's pool: its common postings and its rare one.</summary>
        public void Pool(string zoneId, out List<QuestDef> common, out QuestDef rare)
        {
            common = new List<QuestDef>(); rare = null;
            foreach (var q in log.Db.Ordered)
            {
                if (q.kind != "bounty" || q.zone != zoneId) continue;
                if (q.rare) { if (rare == null) rare = q; } else common.Add(q);
            }
        }
        /// <summary>Today's board for a zone, drawn now if the day has turned (or it was never drawn).</summary>
        public BoardState Board(string zoneId)
        {
            if (Progress.boards == null) Progress.boards = new List<BoardState>();
            var b = Progress.boards.Find(x => x.zone == zoneId);
            if (b == null) { b = new BoardState { zone = zoneId, day = -1 }; Progress.boards.Add(b); }
            if (b.day != Progress.days) Draw(b, zoneId, Progress.days);
            return b;
        }
        /// <summary>The draw: seeded by zone and day (a reload shows the same notices); each slot rolls for the rare posting first,
        /// else takes the next of the common pool in a shuffled order; a posting still active from an earlier day stays up.</summary>
        void Draw(BoardState b, string zoneId, int day)
        {
            Pool(zoneId, out var common, out var rare);
            int seed = 17; foreach (char ch in zoneId) seed = seed * 31 + ch;
            var rng = new System.Random(unchecked(seed * 7919 + day * 104729));
            var order = new List<QuestDef>(common);
            for (int i = order.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = order[i]; order[i] = order[j]; order[j] = t; }
            var ids = new List<string>();
            foreach (var old in b.ids) if (log.State(old) != null) ids.Add(old);   // taken on and not yet done: still yours
            int next = 0;
            for (int s = 0; s < Slots && ids.Count < Slots; s++)
            {
                if (rare != null && !ids.Contains(rare.id) && rng.NextDouble() < RareChance) { ids.Add(rare.id); continue; }
                while (next < order.Count && ids.Contains(order[next].id)) next++;
                if (next < order.Count) ids.Add(order[next++].id);
            }
            b.ids = ids; b.done = new List<string>(); b.day = day;
        }
        /// <summary>What the board offers now: postings taken on and finished first (to hand in), then the rest, with their status.
        /// A posting finished today is gone from the board until another day.</summary>
        public List<(QuestDef quest, QuestStatus status)> Entries(string zoneId, int level)
        {
            var b = Board(zoneId); var list = new List<(QuestDef, QuestStatus)>();
            foreach (var id in b.ids)
            {
                var q = log.Def(id); if (q == null || b.done.Contains(id)) continue;
                var st = log.Status(q, zoneId);
                if (st == QuestStatus.ReadyToTurnIn) list.Insert(0, (q, st));
                else if (st == QuestStatus.Available && level >= q.minLevel) list.Add((q, st));
                else if (st == QuestStatus.Active) list.Add((q, st));
            }
            return list;
        }
        /// <summary>A posting handed in: off the board for today.</summary>
        public void Finished(string zoneId, string id) { var b = Board(zoneId); if (!b.done.Contains(id)) b.done.Add(id); }
        /// <summary>The rare posting active in a zone (its courier should be abroad), or null.</summary>
        public QuestDef ActiveRare(string zoneId)
        {
            foreach (var (q, s) in log.Active()) if (q.kind == "bounty" && q.rare && q.zone == zoneId && s.step < q.steps.Length) return q;
            return null;
        }
        /// <summary>The courier's enemy id in a zone: what the rare posting's kill objective names.</summary>
        public static string CourierId(string zoneId) { return "mob.courier." + zoneId.Replace("zone.", "") + ".0.0"; }
        /// <summary>The day turns at six in the morning: the boards redraw, the villages wake.</summary>
        public static bool DayTurned(float lastHour, float hour) { return lastHour < 6 && hour >= 6 && hour - lastHour < 12; }
    }
}
