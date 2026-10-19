using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// The raid group (WORLD_BOSS_1.md section 2.2, build plan step 1; Chris's answer 7: "2 tanks, 3 healers, 5 damage, in two parties of
    /// five"): forming from your party, joining (Party 2 first), the level and part gates, leaving, the party of each member, what reaches whom
    /// (party-wide within a party, raid-wide to all ten), setting the parties out, the LFM line, the ready check and the frames' words.
    /// </summary>
    public class RaidGroupTests
    {
        static RaidMember M(string id, string classId, int level = 19) { return new RaidMember { id = id, name = id, classId = classId, level = level }; }
        static RaidMember You(string classId = "class.mage") { return new RaidMember { name = "You", classId = classId, level = 20 }; }
        /// <summary>You (a Mage) and four: a Warrior, a Cleric, a Rogue and a Ranger.</summary>
        static RaidGroup Formed()
        {
            return RaidGroup.Form(You(), new[] { M("war1", "class.warrior"), M("cle1", "class.cleric"), M("rog1", "class.rogue"), M("ran1", "class.ranger") });
        }

        [Test] public void Forming_a_raid_makes_your_party_Party_1_with_you_leading()
        {
            var g = Formed();
            Assert.AreEqual(5, g.Count);
            Assert.IsTrue(g.Members.All(m => m.party == 0), "your party is Party 1");
            Assert.AreEqual(RaidGroup.You, g.LeaderId);
            Assert.IsTrue(g.Find(RaidGroup.You).player);
            Assert.AreEqual(SimCompanion.PartyRole.Tank, g.Find("war1").role);
            Assert.AreEqual(SimCompanion.PartyRole.Healer, g.Find("cle1").role);
            Assert.AreEqual(SimCompanion.PartyRole.Damage, g.Find(RaidGroup.You).role, "a Mage deals damage");
            Assert.AreEqual(10, RaidGroup.Size); Assert.AreEqual(5, RaidGroup.PartySize);
            Assert.AreEqual(4, EncounterSession.MaxPartySims, "a party of five is unchanged (the dungeons are five-player)");
        }

        [Test] public void Newcomers_fill_Party_2_then_Party_1_and_the_eleventh_is_refused()
        {
            var g = RaidGroup.Form(You(), new[] { M("war1", "class.warrior") });
            string[] classes = { "class.paladin", "class.cleric", "class.druid", "class.shaman", "class.rogue", "class.ranger", "class.mage", "class.scoundrel" };
            for (int i = 0; i < classes.Length; i++) Assert.IsTrue(g.Join(M("s" + i, classes[i]), false), classes[i] + " joins");
            Assert.AreEqual(10, g.Count); Assert.IsTrue(g.Full);
            Assert.AreEqual(5, g.PartyMembers(1).Count, "Party 2 filled first");
            Assert.AreEqual(1, g.PartyOf("s0")); Assert.AreEqual(1, g.PartyOf("s4"));
            Assert.AreEqual(0, g.PartyOf("s5"), "Party 1 takes the rest once Party 2 is full");
            Assert.IsFalse(g.Join(M("late", "class.rogue"), false), "an eleventh is refused");
            Assert.AreEqual("The raid is full.", g.JoinRefusal(M("late", "class.rogue"), false));
            Assert.AreEqual(-1, g.PartyOf("late"));
        }

        [Test] public void Sims_under_18_are_refused_and_you_are_not()
        {
            var g = Formed();
            StringAssert.Contains("too green", g.JoinRefusal(M("kid", "class.cleric", 17)));
            Assert.IsFalse(g.Join(M("kid", "class.cleric", 17)));
            Assert.IsTrue(g.Join(M("adult", "class.cleric", 18)));
            Assert.AreEqual(RaidGroup.MinSimLevel, 18);
        }

        [Test] public void Parts_fill_two_tanks_three_healers_five_damage_and_the_flexible_flex()
        {
            var g = Formed();   // Warrior tank, Cleric healer, you + Rogue + Ranger damage
            Assert.IsTrue(g.Join(M("war2", "class.warrior")));
            Assert.AreEqual(2, g.RoleCount(SimCompanion.PartyRole.Tank));
            Assert.IsNull(g.RoleToFill("class.warrior"));
            StringAssert.Contains("tanks already", g.JoinRefusal(M("war3", "class.warrior")), "a third tank is turned away by the part gate");
            Assert.IsTrue(g.Join(M("pal1", "class.paladin")));
            Assert.AreEqual(SimCompanion.PartyRole.Healer, g.Find("pal1").role, "a Paladin heals when the tanks are full");
            Assert.IsTrue(g.Join(M("dru1", "class.druid")));
            Assert.AreEqual(3, g.RoleCount(SimCompanion.PartyRole.Healer));
            Assert.IsTrue(g.Join(M("sha1", "class.shaman")));
            Assert.AreEqual(SimCompanion.PartyRole.Damage, g.Find("sha1").role, "a Shaman deals damage when the healers are full");
            Assert.IsTrue(g.Join(M("mag1", "class.mage")));
            Assert.AreEqual(10, g.Count);
            Assert.AreEqual(2, g.RoleCount(SimCompanion.PartyRole.Tank)); Assert.AreEqual(3, g.RoleCount(SimCompanion.PartyRole.Healer)); Assert.AreEqual(5, g.RoleCount(SimCompanion.PartyRole.Damage));
            Assert.IsTrue(g.Join(M("war9", "class.warrior"), false) == false, "full");
        }

        [Test] public void Leaving_takes_one_out_and_the_lead_comes_back_to_you()
        {
            var g = Formed(); g.Join(M("pal1", "class.paladin"));
            Assert.AreEqual(1, g.PartyOf("pal1"));
            g.LeaderId = "pal1";
            Assert.IsTrue(g.Leave("pal1"));
            Assert.AreEqual(-1, g.PartyOf("pal1")); Assert.AreEqual(5, g.Count);
            Assert.AreEqual(RaidGroup.You, g.LeaderId, "the lead comes back to you");
            Assert.IsFalse(g.Leave("nobody"));
            Assert.IsTrue(g.Leave("rog1")); Assert.AreEqual(4, g.PartyMembers(0).Count);
        }

        [Test] public void Party_wide_helps_stay_in_the_party_and_raid_wide_reach_all_ten()
        {
            var g = Formed(); g.Join(M("pal1", "class.paladin")); g.Join(M("dru1", "class.druid"));
            Assert.AreEqual(0, g.PartyOf("cle1")); Assert.AreEqual(1, g.PartyOf("pal1"));
            Assert.IsTrue(g.Reaches(EffectReach.Party, "cle1", "war1"), "the Cleric's mending round reaches its own party");
            Assert.IsFalse(g.Reaches(EffectReach.Party, "cle1", "pal1"), "and not Party 2");
            Assert.IsTrue(g.Reaches(EffectReach.Party, "pal1", "dru1"));
            Assert.IsTrue(g.Reaches(EffectReach.Raid, "cle1", "pal1"), "a raid-wide buff reaches the other party");
            Assert.IsTrue(g.Reaches(EffectReach.Single, "dru1", RaidGroup.You), "a single heal reaches anyone in the raid");
            Assert.IsFalse(g.Reaches(EffectReach.Raid, "cle1", "stranger"), "nobody outside the raid");
            // The tags.
            Assert.AreEqual(EffectReach.Raid, RaidReach.Of(RaidReach.ClassBuff));
            Assert.AreEqual(EffectReach.Party, RaidReach.Of(RaidReach.Feast));
            Assert.AreEqual(EffectReach.Party, RaidReach.Of(RaidReach.GroupHeal));
            Assert.AreEqual(EffectReach.Party, RaidReach.Of(RaidReach.Song));
            Assert.AreEqual(EffectReach.Single, RaidReach.Of(RaidReach.SingleHeal));
            Assert.AreEqual(EffectReach.Raid, RaidReach.Of(RaidReach.GroundCircle));
        }

        [Test] public void Moving_and_arranging_put_a_tank_and_a_healer_in_each_party()
        {
            var g = RaidGroup.Form(You(), new[] { M("war1", "class.warrior"), M("war2", "class.warrior"), M("cle1", "class.cleric"), M("cle2", "class.cleric") });
            g.Join(M("cle3", "class.cleric")); g.Join(M("rog1", "class.rogue")); g.Join(M("rog2", "class.rogue")); g.Join(M("ran1", "class.ranger")); g.Join(M("mag1", "class.mage"));
            Assert.AreEqual(2, g.PartyMembers(0).Count(m => m.role == SimCompanion.PartyRole.Tank), "before: both tanks in Party 1");
            g.Arrange();
            for (int p = 0; p < 2; p++)
            {
                var party = g.PartyMembers(p);
                Assert.AreEqual(5, party.Count, "five in Party " + (p + 1));
                Assert.AreEqual(1, party.Count(m => m.role == SimCompanion.PartyRole.Tank), "a tank in Party " + (p + 1));
                Assert.That(party.Count(m => m.role == SimCompanion.PartyRole.Healer), Is.InRange(1, 2), "one or two healers in Party " + (p + 1));
            }
            Assert.AreEqual(0, g.PartyOf(RaidGroup.You), "you stay in Party 1");
            var dps = g.PartyMembers(1).First(m => m.role == SimCompanion.PartyRole.Damage);
            Assert.IsFalse(g.Move(dps.id, 0), "Party 1 is full");
            Assert.IsFalse(g.Move(dps.id, 1), "already there");
        }

        [Test] public void The_LFM_line_says_what_the_raid_lacks()
        {
            var g = RaidGroup.Form(You(), new[] { M("war1", "class.warrior"), M("cle1", "class.cleric") });
            Assert.AreEqual("LFM Unraveller, need 1 tank 2 heals 4 dps", g.LfmLine);
            g.Join(M("war2", "class.warrior")); g.Join(M("cle2", "class.cleric")); g.Join(M("cle3", "class.cleric"));
            Assert.AreEqual("need 4 dps", g.NeedLine());
        }

        [Test] public void A_ready_check_asks_everyone_and_names_who_is_not_ready()
        {
            var g = Formed();
            g.BeginReadyCheck();
            Assert.IsTrue(g.Checking);
            Assert.AreEqual(ReadyAnswer.Ready, g.Find(RaidGroup.You).ready, "the leader is ready");
            Assert.AreEqual(1, g.ReadyCount); Assert.IsFalse(g.AllAnswered);
            g.Answer("war1", true); g.Answer("cle1", false); g.Answer("rog1", true);
            Assert.IsFalse(g.AllAnswered, "the Ranger has not answered");
            var not = g.EndReadyCheck();
            CollectionAssert.AreEquivalent(new[] { "cle1", "ran1" }, not, "the one who said no and the one who never answered");
            Assert.IsFalse(g.Checking);
        }

        [Test] public void The_frames_words_and_cells()
        {
            Assert.AreEqual("Pinned 8 s", RaidFrames.PinText(7.2f));
            Assert.AreEqual("Free", RaidFrames.PinText(0));
            Assert.AreEqual("7:05", RaidFrames.Clock(425));
            Assert.AreEqual("", RaidFrames.Clock(0));
            Assert.AreEqual("T", RaidFrames.RoleMark(SimCompanion.PartyRole.Tank)); Assert.AreEqual("H", RaidFrames.RoleMark(SimCompanion.PartyRole.Healer));
            var a = RaidFrames.Cell(0, 4); var b = RaidFrames.Cell(1, 0);
            Assert.Greater(b.x, a.xMax, "Party 2 is its own column, to the right of Party 1");
            Assert.AreEqual(RaidFrames.Top, b.y);
            Assert.AreEqual(17.5f, RaidFrames.GearLevel(new List<int> { 15, 20 }), 1e-4f);
            Assert.AreEqual(0, RaidFrames.GearLevel(new List<int>()));
        }
    }
}
