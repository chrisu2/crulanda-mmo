using System;
using System.Linq;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The playable peoples (Docs/RACES.md; Chris 2026-10-12 "Shaman is orc-only"). Code only for now: the save field and the class
    /// rule. No orc figure, character-creation pick or sim mix yet. Humans: CANON. Orcs: CANON (Gorm, Krunk); orc shamanism
    /// CANON-EXPANDED (signed off). A save or sim with no race is human.
    /// </summary>
    public static class Races
    {
        public const string Human = "race.human", Orc = "race.orc";
        public sealed class Race { public string id, name, canonStatus; public string[] classes; }
        static readonly string[] HumanClasses = { "class.warrior", "class.druid", "class.paladin", "class.ranger", "class.mage", "class.rogue", "class.archivist", "class.saltmender", "class.cleric" };
        static readonly string[] OrcClasses = { "class.warrior", "class.druid", "class.ranger", "class.rogue", "class.shaman" };   // PROVISIONAL list: the Shaman is the one that is orc-only
        public static readonly Race[] All = {
            new Race { id = Human, name = "Human", canonStatus = "CANON", classes = HumanClasses },
            new Race { id = Orc, name = "Orc", canonStatus = "CANON", classes = OrcClasses } };
        public static string Normalize(string raceId) { return All.Any(r => r.id == raceId) ? raceId : Human; }
        public static Race Get(string raceId) { var id = Normalize(raceId); return All.First(r => r.id == id); }
        public static string Name(string raceId) { return Get(raceId).name; }
        public static bool Allows(string raceId, string classId) { return Array.IndexOf(Get(raceId).classes, classId) >= 0; }
        /// <summary>The race a class needs, when only one people plays it (the Shaman: orcs), else null.</summary>
        public static string Requires(string classId) { var r = All.Where(x => Array.IndexOf(x.classes, classId) >= 0).ToArray(); return r.Length == 1 ? r[0].id : null; }
        /// <summary>A race for a class: the given one when it allows the class, else the one that does (an old save, a Shaman sim).</summary>
        public static string For(string classId, string raceId = null) { raceId = Normalize(raceId); return Allows(raceId, classId) ? raceId : Requires(classId) ?? All.First(x => Array.IndexOf(x.classes, classId) >= 0 || x.id == Human).id; }
    }
}