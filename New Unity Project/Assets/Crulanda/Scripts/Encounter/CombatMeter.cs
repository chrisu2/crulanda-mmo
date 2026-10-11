using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The party's damage and healing meter (Chris, 2026-10-11, watching the group run: "need to show a dps meter and a healing meter to
    /// check balance on classes"): every blow a party member lands on a mob (EncounterEnemy.Receive) and every mend one casts
    /// (Combatant.Healed), summed by member, over the seconds the party has been in a fight since the last reset. /meter shows and hides
    /// it, /meter reset clears it; the group run writes it into its summary. Nothing here is saved.
    /// </summary>
    public sealed class CombatMeter
    {
        public sealed class Row { public string id, name; public int damage, healing, deaths; public float Dps(float seconds) { return seconds > 1 ? damage / seconds : 0; } public float Hps(float seconds) { return seconds > 1 ? healing / seconds : 0; } }
        readonly EncounterSession s; readonly Dictionary<string, Row> rows = new Dictionary<string, Row>();
        /// <summary>Seconds the party has spent in a fight since the reset.</summary>
        public float CombatSeconds { get; private set; }
        public CombatMeter(EncounterSession session) { s = session; }
        Row RowFor(Actor by)
        {
            if (by == null || by.EntityId == null) return null;
            string id = by.EntityId.Value; if (s.PartyActor(id) == null) return null;   // a sim fighting in the world is not the party's
            if (!rows.TryGetValue(id, out var r)) rows[id] = r = new Row { id = id, name = NameFor(by) };
            return r;
        }
        /// <summary>The member's name with its class (Chris, 2026-10-11: "put the class next to name in dps meter"): "You (Paladin)",
        /// "Nolan Ashby (Warrior)", "Mira (Druid)", "Ansel's wolf (wolf)".</summary>
        string NameFor(Actor by)
        {
            if (by == s.Player) return "You (" + s.ClassDef.displayName + ")";
            var c = s.PartySims.Find(x => x != null && x.actor == by); if (c != null) return by.DisplayName + " (" + SimRoster.ClassName(c.sim.classId) + ")";
            if (s.Companion != null && by == s.Companion.actor) return by.DisplayName + " (Druid)";
            if (s.Pet != null && by == s.Pet.actor) return by.DisplayName + " (" + s.Pet.Kind + ")";
            foreach (var p in s.SimPets) if (p != null && by == p.actor) return by.DisplayName + " (wolf)";
            return by.DisplayName;
        }
        public void Damage(Actor by, int amount) { var r = RowFor(by); if (r != null && amount > 0) r.damage += amount; }
        public void Death(Actor who) { var r = RowFor(who); if (r != null) r.deaths++; }
        /// <summary>What dropped on the run: the thing, off whom, and who took it (the scorecard; Chris, 2026-10-11: "a run down of dps deaths
        /// loot drops etc. kinda like a scorecard when a dungeon is complete. maybe an mvp").</summary>
        public readonly List<(string item, string from, string to)> Drops = new List<(string, string, string)>();
        public int Coin;
        public void Loot(string item, string from, string to) { if (!string.IsNullOrEmpty(item)) Drops.Add((item, from ?? "a body", to ?? "You")); }
        /// <summary>The run's name and when it began (LeadDungeon; the meter is reset with it).</summary>
        public string RunName; public float RunStartedAt = -1;
        public void BeginRun(string name) { Reset(); RunName = name; RunStartedAt = Time.time; }
        public float RunSeconds { get { return RunStartedAt >= 0 ? Time.time - RunStartedAt : CombatSeconds; } }
        /// <summary>The member of the run: the most damage and healing done together, a death counted against it as a tenth of the best.</summary>
        public Row Mvp()
        {
            Row best = null; float bestScore = float.MinValue, top = 1; foreach (var r in rows.Values) top = Mathf.Max(top, r.damage + r.healing);
            foreach (var r in rows.Values) { float score = r.damage + r.healing - r.deaths * top * .1f; if (score > bestScore) { bestScore = score; best = r; } }
            return best;
        }
        public Row TopHealer() { Row best = null; foreach (var r in rows.Values) if (r.healing > 0 && (best == null || r.healing > best.healing)) best = r; return best; }
        public void Heal(Actor by, int amount) { var r = RowFor(by); if (r != null && amount > 0) r.healing += amount; }
        public void Tick(bool inCombat, float dt) { if (inCombat) CombatSeconds += dt; }
        public void Reset() { rows.Clear(); CombatSeconds = 0; Drops.Clear(); Coin = 0; RunName = null; RunStartedAt = -1; }
        public IEnumerable<Row> Rows { get { return rows.Values.OrderByDescending(r => r.damage + r.healing); } }
        public bool Empty { get { return rows.Count == 0; } }
        /// <summary>One line a member: "Name  1,234 dmg (12.3/s)  567 heal (5.6/s)".</summary>
        public IEnumerable<string> Lines()
        {
            float t = CombatSeconds;
            foreach (var r in Rows) yield return r.name + "  " + r.damage.ToString("N0") + " dmg (" + r.Dps(t).ToString("0.0") + "/s)  " + r.healing.ToString("N0") + " heal (" + r.Hps(t).ToString("0.0") + "/s)" + (r.deaths > 0 ? "  " + r.deaths + (r.deaths == 1 ? " death" : " deaths") : "");
        }
        /// <summary>The scorecard as text (the run capture's summary): the run, the members, the MVP, the loot.</summary>
        public IEnumerable<string> Scorecard()
        {
            yield return (RunName ?? "The run") + ": " + Mathf.FloorToInt(RunSeconds / 60) + " min " + (Mathf.FloorToInt(RunSeconds) % 60).ToString("00") + " s, " + Mathf.RoundToInt(CombatSeconds) + " s of it fighting";
            foreach (var l in Lines()) yield return "  " + l;
            var mvp = Mvp(); if (mvp != null) yield return "MVP: " + mvp.name; var h = TopHealer(); if (h != null) yield return "Healer of the run: " + h.name;
            yield return "Loot: " + (Drops.Count == 0 ? "nothing" : string.Join("; ", Drops.Select(d => d.item + " (" + d.from + ") to " + d.to))) + (Coin > 0 ? "; " + Coin + " coin" : "");
        }
    }
}
