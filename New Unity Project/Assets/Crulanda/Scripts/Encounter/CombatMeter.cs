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
        public sealed class Row { public string id, name; public int damage, healing; public float Dps(float seconds) { return seconds > 1 ? damage / seconds : 0; } public float Hps(float seconds) { return seconds > 1 ? healing / seconds : 0; } }
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
        public void Heal(Actor by, int amount) { var r = RowFor(by); if (r != null && amount > 0) r.healing += amount; }
        public void Tick(bool inCombat, float dt) { if (inCombat) CombatSeconds += dt; }
        public void Reset() { rows.Clear(); CombatSeconds = 0; }
        public IEnumerable<Row> Rows { get { return rows.Values.OrderByDescending(r => r.damage + r.healing); } }
        public bool Empty { get { return rows.Count == 0; } }
        /// <summary>One line a member: "Name  1,234 dmg (12.3/s)  567 heal (5.6/s)".</summary>
        public IEnumerable<string> Lines()
        {
            float t = CombatSeconds;
            foreach (var r in Rows) yield return r.name + "  " + r.damage.ToString("N0") + " dmg (" + r.Dps(t).ToString("0.0") + "/s)  " + r.healing.ToString("N0") + " heal (" + r.Hps(t).ToString("0.0") + "/s)";
        }
    }
}
