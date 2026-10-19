#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Ten in the Lowtowns (WORLD_BOSS_1.md section 2, build plan steps 1 and 2): a party of five is as it was (a sixth is refused); "Form raid"
    /// makes it Party 1 and five more fill Party 2 (an eleventh and an under-18 refused); each sim plays the part the raid gave it; a class
    /// buff reaches all ten, a feast and a party heal only their own party; a ready check of ten; a roll for loot among ten; leaving. And the
    /// call to arms: the LFM line in Zone chat, the bold come with salt and harpoons, the timid decline, and the empty-zone line when too few.
    /// </summary>
    public class RaidFormTests
    {
        string root; float hourWas;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load()
        {
            hourWas = WorldClock.Hour; WorldClock.Hour = 11; ZoneBuilder.RequestedZoneId = "zone.lowtowns";
            root = Path.Combine(Path.GetTempPath(), "Crulanda-raid-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 30; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            EncounterSession.RaidCallTimeScale = 1;
            WorldClock.Hour = hourWas; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static void Level20(EncounterSession s) { s.Progress.experience = EncounterProgress.XpForLevel(20); s.Player.SetLevel(20); }
        /// <summary>A sim made of this class and level, online all day, willing, standing near you in the Lowtowns.</summary>
        static SimAdventurer Near(EncounterSession s, SimAdventurer sim, string classId, int level, int k)
        {
            var pop = SimPopulation.Active; var me = s.Player.transform.position;
            var at = me + Quaternion.Euler(0, k * 36, 0) * Vector3.forward * 3;
            if (NavMesh.SamplePosition(at, out var hit, 4, NavMesh.AllAreas)) at = hit.position;
            sim.zone = "zone.lowtowns"; sim.onlineFrom = 0; sim.onlineHours = 24; sim.level = level; sim.classId = classId; sim.friendly = .9f; sim.bold = .8f; sim.regard = 0; sim.x = at.x; sim.z = at.z;
            var old = pop.Find(sim.id); if (old != null) { pop.Figures.Remove(old); UnityEngine.Object.Destroy(old.gameObject); }
            pop.Refresh(); return sim;
        }
        /// <summary>The classes that make 2 tanks, 3 healers and 5 damage with you.</summary>
        static List<string> Fill(SimCompanion.PartyRole you)
        {
            int t = 2 - (you == SimCompanion.PartyRole.Tank ? 1 : 0), h = 3 - (you == SimCompanion.PartyRole.Healer ? 1 : 0), d = 5 - (you == SimCompanion.PartyRole.Damage ? 1 : 0);
            var l = new List<string>();
            for (int i = 0; i < t; i++) l.Add("class.warrior");
            for (int i = 0; i < h; i++) l.Add("class.cleric");
            string[] dps = { "class.rogue", "class.ranger", "class.mage", "class.scoundrel", "class.rogue" };
            for (int i = 0; i < d; i++) l.Add(dps[i]);
            return l;
        }

        [UnityTest, Timeout(300000)] public IEnumerator Ten_form_a_raid_in_the_Lowtowns_in_two_parties_of_five()
        {
            yield return Load();
            var s = Session(); Assert.NotNull(s); Assert.AreEqual("zone.lowtowns", ZoneBuilder.Active.Zone.id); Level20(s);
            var pop = SimPopulation.Active; Assert.NotNull(pop);
            var classId = s.ClassDef != null ? s.ClassDef.id : s.Progress.classId;
            var classes = Fill(RaidGroup.RolesFor(classId)[0]);
            var sims = pop.World.sims.Take(11).ToList();
            for (int i = 0; i < 9; i++) Near(s, sims[i], classes[i], 19, i);
            Near(s, sims[9], "class.rogue", 17, 9);    // too green for a raid
            Near(s, sims[10], "class.rogue", 19, 10);  // the eleventh
            yield return null;

            // A party of five is as it was.
            for (int i = 0; i < 4; i++) Assert.IsTrue(s.Invite(sims[i].id), sims[i].name + " joins the party");
            Assert.AreEqual(4, s.PartySims.Count); Assert.IsFalse(s.InRaid);
            Assert.IsFalse(s.Invite(sims[4].id), "a sixth is refused in a party");
            Assert.IsTrue(s.Messages.Any(m => m.Contains("Your party is full")));

            // Form raid: the party is Party 1.
            Assert.IsTrue(s.FormRaid()); Assert.IsTrue(s.InRaid);
            Assert.AreEqual(5, s.Raid.Count); Assert.IsTrue(s.Raid.Members.All(m => m.party == 0));
            Assert.AreEqual(RaidGroup.You, s.Raid.LeaderId);
            Assert.IsFalse(s.Invite(sims[9].id), "an under-18 is refused for the raid");
            Assert.IsTrue(s.Messages.Any(m => m.Contains("too green")));
            for (int i = 4; i < 9; i++) Assert.IsTrue(s.Invite(sims[i].id), sims[i].name + " joins the raid");
            Assert.AreEqual(10, s.Raid.Count); Assert.AreEqual(9, s.PartySims.Count);
            Assert.AreEqual(5, s.Raid.PartyMembers(1).Count, "the five newcomers are Party 2");
            Assert.IsFalse(s.Invite(sims[10].id), "an eleventh is refused"); Assert.IsTrue(s.Messages.Any(m => m.Contains("The raid is full")));
            Assert.AreEqual(2, s.Raid.RoleCount(SimCompanion.PartyRole.Tank)); Assert.AreEqual(3, s.Raid.RoleCount(SimCompanion.PartyRole.Healer)); Assert.AreEqual(5, s.Raid.RoleCount(SimCompanion.PartyRole.Damage));
            s.RaidCommand("arrange");
            for (int p = 0; p < 2; p++) Assert.AreEqual(1, s.Raid.PartyMembers(p).Count(m => m.role == SimCompanion.PartyRole.Tank), "a tank in Party " + (p + 1));
            foreach (var c in s.PartySims) Assert.AreEqual(s.Raid.Find(c.sim.id).role, c.Role, c.sim.name + " plays the part the raid gave it");
            Assert.IsFalse(s.JoinQueue("any"), "no dungeon queue as a raid");

            // Bring all ten together.
            int k = 0;
            foreach (var c in s.PartySims) { var at = s.Player.transform.position + Quaternion.Euler(0, k++ * 40, 0) * Vector3.forward * 2.5f; if (NavMesh.SamplePosition(at, out var hit, 3, NavMesh.AllAreas)) c.GetComponent<NavMeshAgent>().Warp(hit.position); }
            yield return null;
            var p2 = s.PartySims.First(c => s.Raid.PartyOf(c.sim.id) == 1); var p1 = s.PartySims.First(c => s.Raid.PartyOf(c.sim.id) == 0);
            Assert.IsTrue(s.SameParty(s.Player, p1.actor)); Assert.IsFalse(s.SameParty(s.Player, p2.actor));
            Assert.IsTrue(s.PartyWide(p1.actor)); Assert.IsFalse(s.PartyWide(p2.actor), "your party heals stop at Party 1");
            // A class buff is raid-wide; a feast party-wide.
            int laid = s.LayClassBuff(ClassBuffs.For("class.warrior"), s.Player);
            Assert.AreEqual(10 + (s.Progress.recruited && s.Companion != null && s.Companion.gameObject.activeInHierarchy ? 1 : 0), laid, "a class buff reaches all ten");
            var circle = s.FeastCircle(p2.actor);
            Assert.IsFalse(circle.Contains(s.Player.GetComponent<Crulanda.Combat.Combatant>().Statuses), "a feast in Party 2 does not feed you");
            Assert.LessOrEqual(circle.Count, 5);
            Assert.IsTrue(s.PartyMatesOf(p2).All(c => s.Raid.PartyOf(c.sim.id) == 1));

            // A ready check of ten.
            Assert.IsTrue(s.ReadyCheck());
            yield return new WaitForSeconds(4);
            Assert.IsFalse(s.Raid.Checking); Assert.IsTrue(s.Messages.Any(m => m.Contains("Everyone is ready")), "all ten ready: " + string.Join(" | ", s.Messages));

            // A roll for loot among ten (need and greed, PartyLoot).
            var gear = s.Items.Items.Values.First(d => d.kind == "gear" && d.quality >= 3);
            typeof(EncounterSession).GetMethod("QueueRoll", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(s, new object[] { gear.id, 1, "test" });
            Assert.NotNull(s.Roll); Assert.AreEqual(10, s.Roll.members.Count, "all ten roll");
            s.ChooseRoll("you", RollChoice.Pass);

            // Leaving.
            s.LeaveParty(p2.sim.id); Assert.AreEqual(9, s.Raid.Count); Assert.AreEqual(-1, s.Raid.PartyOf(p2.sim.id));
            s.LeaveRaid(); Assert.IsFalse(s.InRaid); Assert.AreEqual(0, s.PartySims.Count);
        }

        [UnityTest, Timeout(300000)] public IEnumerator The_call_to_arms_rallies_the_bold_and_says_when_the_zone_is_too_empty()
        {
            yield return Load();
            var s = Session(); Assert.NotNull(s); Level20(s);
            var pop = SimPopulation.Active;
            foreach (var x in pop.World.sims) { x.onlineFrom = 0; x.onlineHours = 0; x.friend = false; x.regard = 0; x.guild = ""; }   // nobody about...
            var sims = pop.World.sims.Take(4).ToList();
            var tank = Near(s, sims[0], "class.warrior", 20, 0); tank.coin = 500; tank.gearBonus = RaidCall.CallGear;   // the call: geared level-20 sims only
            var healer = Near(s, sims[1], "class.cleric", 20, 1); healer.coin = 500; healer.gearBonus = RaidCall.CallGear;
            var timid = Near(s, sims[2], "class.druid", 20, 2); timid.bold = .1f; timid.gearBonus = RaidCall.CallGear;
            var green = Near(s, sims[3], "class.rogue", 15, 3);
            yield return null;
            EncounterSession.RaidCallTimeScale = .02f;
            Assert.IsTrue(s.CallToArms(), "the call forms the raid and goes out");
            Assert.IsTrue(s.InRaid);
            Assert.IsTrue(s.Chat.Any(l => l.channel == ChatChannel.Zone && l.speaker == "You" && l.text.StartsWith("LFM Unraveller, need")), "the LFM line in Zone chat");
            float end = Time.time + 10; while (s.Calling && Time.time < end) yield return null;
            Assert.IsFalse(s.Calling);
            Assert.IsTrue(s.InParty(tank.id), "the bold Warrior comes"); Assert.IsTrue(s.InParty(healer.id), "the bold Cleric comes");
            Assert.AreEqual(SimCompanion.PartyRole.Tank, s.Raid.Find(tank.id).role); Assert.AreEqual(SimCompanion.PartyRole.Healer, s.Raid.Find(healer.id).role);
            Assert.IsFalse(s.InParty(timid.id), "the timid Druid stays");
            Assert.IsTrue(s.Chat.Any(l => l.channel == ChatChannel.Zone && l.speaker == timid.name), "and says why");
            Assert.IsFalse(s.InParty(green.id), "too green to be asked");
            Assert.AreEqual(3, SimEconomy.Count(tank, "throw.salt_twist")); Assert.AreEqual(3, SimEconomy.Count(tank, "throw.bone_harpoon"), "salt and harpoons bought on the way");
            Assert.Less(tank.coin, 500);
            Assert.IsTrue(s.Chat.Any(l => l.channel == ChatChannel.System && l.text.StartsWith("Not enough of the Lowtowns' adventurers are about")), "the zone is too empty for ten");
        }
    }
}
#endif
