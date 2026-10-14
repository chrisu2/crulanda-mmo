using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>Races, code only (Docs/RACES.md): an old save reads as human, the Shaman is orc-only, and a fresh save gets a race its class allows.</summary>
    public class RaceTests
    {
        [Test] public void An_old_save_with_no_race_is_human()
        {
            var p = JsonUtility.FromJson<EncounterProgress>("{\"classId\":\"class.mage\"}");
            Assert.AreEqual(Races.Human, Races.Normalize(p.raceId));
            Assert.AreEqual(Races.Human, Races.Normalize(""));
            Assert.AreEqual(Races.Human, Races.Normalize("race.nobody"));
        }
        [Test] public void The_shaman_is_orc_only()
        {
            Assert.IsFalse(Races.Allows(Races.Human, "class.shaman"));
            Assert.IsTrue(Races.Allows(Races.Orc, "class.shaman"));
            Assert.AreEqual(Races.Orc, Races.Requires("class.shaman"));
            Assert.IsNull(Races.Requires("class.warrior"));
        }
        [Test] public void Orcs_play_warrior_ranger_rogue_mage_and_shaman_not_druid()   // Chris 23:31
        {
            foreach (var c in new[] { "class.warrior", "class.ranger", "class.rogue", "class.mage", "class.shaman" }) Assert.IsTrue(Races.Allows(Races.Orc, c), c);
            foreach (var c in new[] { "class.druid", "class.paladin", "class.archivist", "class.saltmender", "class.cleric" }) Assert.IsFalse(Races.Allows(Races.Orc, c), c);
            Assert.AreEqual(Races.Human, EncounterSession.FreshProgress("class.druid", Races.Orc).raceId, "an orc cannot be a Druid");
            // The Scoundrel: races open until all are defined (Chris, 2026-10-13): both peoples for now.
            Assert.IsTrue(Races.Allows(Races.Human, "class.scoundrel")); Assert.IsTrue(Races.Allows(Races.Orc, "class.scoundrel")); Assert.IsNull(Races.Requires("class.scoundrel"));
        }
        [Test] public void A_fresh_save_gets_a_race_its_class_allows()
        {
            Assert.AreEqual(Races.Human, EncounterSession.FreshProgress("class.mage").raceId);
            Assert.AreEqual(Races.Orc, EncounterSession.FreshProgress("class.shaman").raceId);
            Assert.AreEqual(Races.Orc, EncounterSession.FreshProgress("class.shaman", Races.Human).raceId, "a human cannot be a Shaman");
            Assert.AreEqual(Races.Orc, EncounterSession.FreshProgress("class.warrior", Races.Orc).raceId);
        }
        [Test] public void A_sim_with_no_race_follows_its_class()
        {
            Assert.AreEqual(Races.Human, new SimAdventurer { classId = "class.rogue" }.Race);
            Assert.AreEqual(Races.Orc, new SimAdventurer { classId = "class.shaman" }.Race);
        }
    }
}