using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Crulanda.Encounter;
using Crulanda.Gameplay;

namespace Crulanda.Tests
{
    /// <summary>
    /// The call to arms (WORLD_BOSS_1.md section 2.3, build plan step 2) on a fixed roster: who answers and in what order (your guild first,
    /// then the Lowtowns' own by nerve, then friends anywhere), for which part, the level and nerve gates, the empty zone at 3 am, and the
    /// salt and harpoons a sim buys on the way (the real items, kept from the stall).
    /// </summary>
    public class RaidCallTests
    {
        const string Low = "zone.lowtowns";
        static RaidCandidate C(string id, string classId, int level, string zone, float bold = .6f, string guild = "", bool friend = false, bool online = true, bool rival = false)
        {
            return new RaidCandidate { id = id, name = id, classId = classId, level = level, zone = zone, bold = bold, guild = guild, friend = friend, online = online, rival = rival };
        }
        static RaidGroup Alone() { return RaidGroup.Form(new RaidMember { name = "You", classId = "class.mage", level = 20 }, null); }
        static List<RaidCandidate> Roster()
        {
            return new List<RaidCandidate> {
                C("g1", "class.warrior", 19, "zone.ashrim", guild: "Ember Oath"),        // guild, away: travels
                C("g2", "class.cleric", 18, Low, guild: "Ember Oath"),                    // guild, here
                C("g3", "class.mage", 17, Low, guild: "Ember Oath"),                      // guild, too green
                C("z1", "class.paladin", 20, Low, bold: .8f),                             // the second tank
                C("z2", "class.druid", 19, Low, bold: .2f),                               // too timid: declines with a line
                C("z3", "class.shaman", 18, Low, bold: .5f),                              // a healer
                C("z4", "class.rogue", 20, Low, bold: .9f),
                C("z5", "class.ranger", 19, Low, bold: .6f),
                C("z6", "class.warrior", 20, Low, bold: .7f),                             // the tanks are full by then: nothing said
                C("z7", "class.mage", 15, Low, bold: .9f),                                // too green
                C("z8", "class.rogue", 19, Low, online: false),                           // asleep
                C("z9", "class.scoundrel", 20, Low, rival: true),                         // a rival
                C("f1", "class.saltmender", 18, "zone.verdant", friend: true),            // a friend away: the third healer
                C("f2", "class.ranger", 20, "zone.crown", friend: true, bold: .1f),       // a friend comes whatever its nerve
            };
        }

        [Test] public void The_guild_answers_first_then_the_zone_then_friends_each_for_a_part_the_raid_lacks()
        {
            var g = Alone(); var answers = RaidCall.Answers(g, Roster(), Low, "Ember Oath", 7);
            var comes = answers.Where(a => a.comes).ToDictionary(a => a.id);
            CollectionAssert.AreEquivalent(new[] { "g1", "g2", "z1", "z3", "z4", "z5", "f1", "f2" }, comes.Keys);
            Assert.AreEqual(RaidSource.Guild, comes["g1"].source); Assert.AreEqual(RaidSource.Guild, comes["g2"].source);
            Assert.AreEqual(RaidSource.Zone, comes["z1"].source); Assert.AreEqual(RaidSource.Friend, comes["f1"].source);
            Assert.AreEqual(SimCompanion.PartyRole.Tank, comes["g1"].role);
            Assert.AreEqual(SimCompanion.PartyRole.Tank, comes["z1"].role, "the Paladin takes the second tank");
            Assert.AreEqual(SimCompanion.PartyRole.Healer, comes["g2"].role);
            Assert.AreEqual(SimCompanion.PartyRole.Healer, comes["z3"].role);
            Assert.AreEqual(SimCompanion.PartyRole.Healer, comes["f1"].role, "the friend Salt-Mender is the third healer");
            Assert.AreEqual(SimCompanion.PartyRole.Damage, comes["f2"].role);
            // The gates.
            Assert.IsFalse(answers.Any(a => a.id == "g3" || a.id == "z7"), "under 18 do not answer");
            Assert.IsFalse(answers.Any(a => a.id == "z8"), "nobody asleep answers");
            Assert.IsFalse(answers.Any(a => a.id == "z9"), "a rival does not answer");
            Assert.IsFalse(answers.Any(a => a.id == "z6"), "a third tank is not wanted, and says nothing");
            var timid = answers.Single(a => a.id == "z2");
            Assert.IsFalse(timid.comes, "bold under .3 declines"); Assert.IsFalse(string.IsNullOrEmpty(timid.line), "with a line");
            // The times: 20-60 s here, a minute or two from afar; in order of arrival.
            Assert.IsTrue(comes["g1"].travels); Assert.That(comes["g1"].after, Is.InRange(60f, 120f));
            Assert.IsFalse(comes["g2"].travels); Assert.That(comes["g2"].after, Is.InRange(20f, 60f));
            Assert.That(comes["z4"].after, Is.InRange(20f, 60f));
            Assert.IsTrue(comes["f2"].travels);
            for (int i = 1; i < answers.Count; i++) Assert.LessOrEqual(answers[i - 1].after, answers[i].after);
            // Eight come for nine places: the call falls short by one damage.
            Assert.AreEqual(8, RaidCall.Comers(answers)); Assert.IsTrue(RaidCall.Short(g, answers));
        }

        [Test] public void The_guild_comes_before_a_bolder_sim_of_the_zone_for_the_last_tank()
        {
            var g = RaidGroup.Form(new RaidMember { name = "You", classId = "class.warrior", level = 20 }, null);   // you tank: one tank wanted
            var roster = new List<RaidCandidate> { C("zw", "class.warrior", 20, Low, bold: 1f), C("gw", "class.warrior", 18, "zone.peaks", guild: "Ember Oath") };
            var answers = RaidCall.Answers(g, roster, Low, "Ember Oath", 1);
            Assert.AreEqual("gw", answers.Single(a => a.comes).id, "your guild first");
            Assert.IsFalse(answers.Any(a => a.id == "zw"));
            // Without a guild the zone's warrior comes.
            Assert.AreEqual("zw", RaidCall.Answers(g, roster, Low, "", 1).Single(a => a.comes).id);
        }

        [Test] public void Never_more_come_than_the_raid_has_places_and_the_same_seed_gives_the_same_call()
        {
            var g = Alone();
            var many = new List<RaidCandidate>();
            for (int i = 0; i < 30; i++) many.Add(C("d" + i.ToString("00"), i % 3 == 0 ? "class.warrior" : i % 3 == 1 ? "class.cleric" : "class.rogue", 19, Low));
            var a = RaidCall.Answers(g, many, Low, "", 42);
            Assert.AreEqual(9, RaidCall.Comers(a));
            Assert.AreEqual(2, a.Count(x => x.comes && x.role == SimCompanion.PartyRole.Tank));
            Assert.AreEqual(3, a.Count(x => x.comes && x.role == SimCompanion.PartyRole.Healer));
            Assert.AreEqual(4, a.Count(x => x.comes && x.role == SimCompanion.PartyRole.Damage), "four with you, a Mage, make five");
            Assert.IsFalse(RaidCall.Short(g, a));
            var b = RaidCall.Answers(g, many, Low, "", 42);
            CollectionAssert.AreEqual(a.Select(x => x.id + x.after).ToList(), b.Select(x => x.id + x.after).ToList());
        }

        [Test] public void At_3_am_the_Lowtowns_are_empty_and_the_call_says_so()
        {
            // The real kind of roster: sims online in the day (8:00 for eight hours), asked at 3 am.
            var sims = new List<SimAdventurer>();
            for (int i = 0; i < 12; i++) sims.Add(new SimAdventurer { id = "sim" + i, name = "Sim " + i, classId = i % 2 == 0 ? "class.warrior" : "class.cleric", level = 19, zone = Low, bold = .9f, onlineFrom = 8, onlineHours = 8 });
            var g = Alone();
            var night = RaidCall.Answers(g, sims.Select(s => RaidCandidate.Of(s, 3f, false)).ToList(), Low, "", 3);
            Assert.AreEqual(0, RaidCall.Comers(night), "nobody is about at 3 am");
            Assert.IsTrue(RaidCall.Short(g, night));
            Assert.AreEqual("Not enough of the Lowtowns' adventurers are about. Try your guild, or wait for the evening.", RaidCall.EmptyZoneLine("The Lowtowns"));
            var noon = RaidCall.Answers(g, sims.Select(s => RaidCandidate.Of(s, 12f, false)).ToList(), Low, "", 3);
            Assert.Greater(RaidCall.Comers(noon), 0, "at noon they answer");
        }

        [Test] public void A_sim_answering_buys_three_salt_and_three_harpoons_and_keeps_them_from_the_stall()
        {
            var items = ProfessionDataTests.Items();
            var salt = items.Get("throw.salt_twist"); var harpoon = items.Get("throw.bone_harpoon");
            Assert.NotNull(salt, "Tibb Marrow's salt twist"); Assert.NotNull(harpoon, "Ketta Rune's bone harpoon");
            var s = new SimAdventurer { id = "x", name = "X", classId = "class.rogue", level = 19, coin = 1000 };
            SimEconomy.Add(s, "throw.salt_twist", 1);
            int spent = RaidCall.BuyKit(s, id => items.Get(id).value);
            Assert.AreEqual(3, SimEconomy.Count(s, "throw.salt_twist")); Assert.AreEqual(3, SimEconomy.Count(s, "throw.bone_harpoon"));
            Assert.AreEqual(2 * salt.value + 3 * harpoon.value, spent, "it buys only what it lacks");
            Assert.AreEqual(1000 - spent, s.coin);
            Assert.AreEqual(3, SimEconomy.Kept(s, items, "throw.salt_twist"), "kept for the raid, not sold");
            Assert.AreEqual(3, SimEconomy.Kept(s, items, "throw.bone_harpoon"));
            Assert.AreEqual(0, RaidCall.BuyKit(s, id => items.Get(id).value), "nothing more to buy");
            var poor = new SimAdventurer { id = "p", name = "P", classId = "class.rogue", level = 19, coin = 0 };
            Assert.AreEqual(0, RaidCall.BuyKit(poor, id => items.Get(id).value)); Assert.AreEqual(0, poor.coin, "no coin, nothing bought");
        }
    }
}
