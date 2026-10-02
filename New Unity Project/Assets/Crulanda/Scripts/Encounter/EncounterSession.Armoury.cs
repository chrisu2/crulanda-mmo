using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Armoury in the running game (loot DESIGN.md 4, rows 8-10; step L3; ArmouryLog): it binds to the character when the session
    /// starts and after every load, so what the character already holds counts quietly; TickLoot sweeps the bags and equipment twice
    /// a second, and the first time an appearance enters them a "NEW LOOK" toast names the piece (several at once are one toast) and
    /// the quest book's Armoury tab counts it until the tab is next opened. Camp kills count toward their drop lists (RollCorpse), and
    /// the epic pity counters ride in the save (EncounterProgress.lootLuck).
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>Named gear found, looks seen and loot kill counts (null until the session starts).</summary>
        public ArmouryLog Armoury { get; private set; }
        /// <summary>Pieces that brought something new to the Armoury since its tab was last open (not saved).</summary>
        public int ArmouryUnseen { get; private set; }
        float nextArmourySweep, armouryTalliesAt; List<ArmouryLog.Tally> armouryTallies; EncounterProgress armouryTalliesOf;
        readonly List<string> newLooks = new List<string>(); readonly HashSet<string> armouryFresh = new HashSet<string>();
        void StartArmoury()
        {
            Armoury = new ArmouryLog(Progress, Items, Loot, GearLooks.Load(), AllSecrets());
            Armoury.NewItem = id => armouryFresh.Add(id);
            Armoury.NewLook = id => { armouryFresh.Add(id); newLooks.Add(id); };
        }
        /// <summary>Every zone's hidden finds (a find already found that holds a named piece counts it as found).</summary>
        IEnumerable<Crulanda.World.ZoneSecret> AllSecrets()
        {
            var all = new List<Crulanda.World.ZoneSecret>(); if (Zone == null) return all;
            foreach (var z in Zone.AllZones()) if (z != null && z.secrets != null) all.AddRange(z.secrets);
            return all;
        }
        /// <summary>Twice a second (from TickLoot): what is new in the bags and equipment goes into the Armoury, and new looks raise one toast.</summary>
        void SweepArmoury()
        {
            if (Armoury == null || Time.time < nextArmourySweep) return;
            nextArmourySweep = Time.time + .5f; newLooks.Clear(); armouryFresh.Clear();
            Armoury.Sweep();
            ArmouryUnseen += armouryFresh.Count; if (armouryFresh.Count > 0) armouryTallies = null;
            if (newLooks.Count == 1) ShowToast("NEW LOOK", ItemName(newLooks[0]));
            else if (newLooks.Count > 1) ShowToast(newLooks.Count + " NEW LOOKS", ItemName(newLooks[0]) + " and " + (newLooks.Count - 1) + " more");
        }
        /// <summary>The Armoury tab was looked at: its count clears.</summary>
        public void SeenArmoury() { ArmouryUnseen = 0; }
        /// <summary>
        /// The quest book's Armoury tab: every zone, this one first and the rest by level, then the pieces that drop anywhere, each with
        /// its named pieces (ArmouryLog.TallyOf). Without the named loot there is nothing to list. The book asks for it on every GUI
        /// event, so the list is kept for a quarter of a second (real time: the book may be open while the game is paused).
        /// </summary>
        public List<ArmouryLog.Tally> ArmouryTallies()
        {
            if (armouryTallies != null && armouryTalliesOf == Progress && Time.unscaledTime - armouryTalliesAt < .25f) return armouryTallies;
            var list = armouryTallies = new List<ArmouryLog.Tally>(); armouryTalliesAt = Time.unscaledTime; armouryTalliesOf = Progress;
            if (Armoury == null || Loot == null) return list;
            var zones = Zone != null ? Zone.AllZones().FindAll(z => z != null) : new List<Crulanda.World.ZoneDefinition>();
            string here = Zone != null ? Zone.Zone.id : null;
            zones.Sort((a, b) => (a.id == here ? 0 : 1) != (b.id == here ? 0 : 1) ? (a.id == here ? -1 : 1) : a.levelMin != b.levelMin ? a.levelMin.CompareTo(b.levelMin) : string.CompareOrdinal(a.id, b.id));
            foreach (var z in zones) { var t = Armoury.TallyOf(z.id, z.displayName, z.id == here); if (t.Total > 0) list.Add(t); }
            if (Zone == null) foreach (var (_, z) in ArmouryLog.ZoneShorts) { var t = Armoury.TallyOf(z, char.ToUpperInvariant(z[0]) + z.Substring(1)); if (t.Total > 0) list.Add(t); }
            var world = Armoury.TallyOf(ArmouryLog.World, "Anywhere in the land"); if (world.Total > 0) list.Add(world);
            return list;
        }
    }
}
