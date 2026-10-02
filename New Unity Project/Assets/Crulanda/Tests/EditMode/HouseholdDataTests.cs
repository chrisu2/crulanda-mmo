using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The households in the zones' data (tools/wip/professions/ADDENDUM.md A.1, A.2 and A.5): every villager of Oakhaven and
    /// Khaven who does not keep a post lives in exactly one household, every household names a house that stands in the zone, no
    /// two households share a house, and the zones that name none still parse.
    /// </summary>
    public class HouseholdDataTests
    {
        static ZoneDefinition Zone(string file) { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Zones/" + file + ".json"))); }
        /// <summary>Everyone VillageLife.Init spawns who needs a home: the villagers by count (the zone's names or the default list),
        /// a hen-wife per coop, and residents who work. Residents keeping a post are left out.</summary>
        static List<string> Folk(ZoneDefinition z)
        {
            var names = z.life.names != null && z.life.names.Length > 0 ? z.life.names : VillageLife.DefaultNames;
            var folk = new List<string>();
            for (int i = 0; i < z.life.villagers; i++) folk.Add(names[i % names.Length]);
            int coops = z.props.Count(p => p != null && p.kind == "coop");
            for (int i = 0; i < coops; i++) folk.Add(VillageLife.KeeperNamesList[i % VillageLife.KeeperNamesList.Length]);
            foreach (var r in z.life.residents) if (r != null && r.works) folk.Add(r.name);
            return folk;
        }
        static List<string> PostResidents(ZoneDefinition z) { return z.life.residents.Where(r => r != null && !r.works).Select(r => r.name).ToList(); }
        static void EveryoneHousedOnce(ZoneDefinition z)
        {
            var folk = Folk(z); var posts = PostResidents(z);
            foreach (var name in folk)
            {
                int n = z.life.households.Count(h => h.members.Any(m => m.name == name));
                Assert.AreEqual(1, n, z.id + ": " + name + " lives in exactly one household (in " + n + ").");
            }
            foreach (var h in z.life.households)
            {
                Assert.Greater(h.members.Length, 0, h.name + " has members.");
                foreach (var m in h.members)
                {
                    Assert.IsTrue(folk.Contains(m.name) || posts.Contains(m.name) || m.kin == "lodger", z.id + ": " + h.name + "'s " + m.name + " is someone the village spawns (or a lodger who is not a villager).");
                    Assert.IsFalse(string.IsNullOrEmpty(m.kin), h.name + ": " + m.name + " is kin of some kind.");
                }
                Assert.AreEqual(1, h.members.Count(m => m.kin == "head"), h.name + " has one head.");
                Assert.AreEqual("GAME-ONLY", h.canonStatus, h.name + " is labelled.");
            }
        }

        [Test] public void Oakhaven_every_villager_is_in_exactly_one_household()
        {
            var z = Zone("oakhaven");
            Assert.AreEqual(24, Folk(z).Count, "Twenty villagers, three hen-wives and the innkeeper.");
            EveryoneHousedOnce(z);
            Assert.AreEqual(16, z.life.households.Length, "Sixteen households, the Golden Cask's among them.");
            Assert.AreEqual("The Golden Cask", z.life.households.Single(h => h.members.Any(m => m.name == "Hob Linden")).house, "The innkeeper lives over his inn.");
            var tanner = z.life.households.Single(h => h.name == "Tanner");
            CollectionAssert.AreEqual(new[] { "Maud Tanner", "Fen Walker", "Nettie" }, tanner.members.Select(m => m.name).ToArray(), "Maud, her husband the skinner and their daughter.");
            Assert.AreEqual("Tanner house", tanner.house);
            Assert.AreEqual("Moss's lodge", z.life.households.Single(h => h.members.Any(m => m.name == "Garet Moss")).house, "The hunter lives at his lodge.");
            Assert.AreEqual("Crisp cottage", z.life.households.Single(h => h.members.Any(m => m.name == "Aldo Crisp")).house, "The miller lives apart from his mill.");
        }

        [Test] public void Every_household_house_names_a_prop_and_no_house_is_shared()
        {
            foreach (var file in new[] { "oakhaven", "khaven" })
            {
                var z = Zone(file); var houses = new HashSet<string>(); var names = new HashSet<string>();
                foreach (var h in z.life.households)
                {
                    Assert.IsTrue(names.Add(h.name), file + ": the household name '" + h.name + "' is used once.");
                    Assert.IsTrue(houses.Add(h.house), file + ": '" + h.house + "' is the house of one household only.");
                    var props = z.props.Where(p => p != null && p.name == h.house).ToList();
                    Assert.AreEqual(1, props.Count, file + ": " + h.name + "'s house '" + h.house + "' is one prop of the zone.");
                    CollectionAssert.Contains(new[] { "house", "barn", "mill", "inn" }, props[0].kind, file + ": " + h.house + " is a house, barn, mill or inn.");
                }
            }
        }

        [Test] public void Khaven_households_cover_its_six_villagers()
        {
            var z = Zone("khaven");
            Assert.AreEqual(6, Folk(z).Count, "Khaven has six villagers and no coop.");
            EveryoneHousedOnce(z);
            Assert.AreEqual(5, z.life.households.Length);
            CollectionAssert.AreEquivalent(new[] { "Dorra Vey", "Old Kestrel" }, z.life.households.Single(h => h.name == "Vey").members.Select(m => m.name).ToArray());
            CollectionAssert.AreEquivalent(new[] { "Mattock Jenn", "Siv Harl" }, z.life.households.Single(h => h.name == "Jenn").members.Select(m => m.name).ToArray());
            Assert.AreEqual("Crypt-Keeper's Hovel", z.life.households.Single(h => h.members.Any(m => m.name == "Ansel Morrow")).house, "The Crypt-Keeper is named at his hovel (he keeps his post).");
        }

        [Test] public void Zones_without_households_still_parse()
        {
            foreach (var file in new[] { "peaks", "ashrim", "verdant" })
            {
                var z = Zone(file);
                Assert.IsNotNull(z.life, file + " has a life block.");
                Assert.IsNotNull(z.life.households, file + ": households is an empty list, not missing."); Assert.AreEqual(0, z.life.households.Length, file);
                Assert.IsNotNull(z.life.workshops, file + ": workshops too."); Assert.AreEqual(0, z.life.workshops.Length, file);
                Assert.IsTrue(z.life.residents.All(r => !r.works), file + ": its residents keep their posts.");
            }
            // A household with only a name and a house takes the purse's defaults.
            var h = JsonUtility.FromJson<ZoneHousehold>("{ \"name\": \"Test\", \"house\": \"Test house\" }");
            Assert.AreEqual(6, h.stipend); CollectionAssert.AreEqual(new[] { "bread", "firewood", "eggs" }, h.needs); Assert.IsNotNull(h.members); Assert.AreEqual(0, h.members.Length);
        }
    }
}
