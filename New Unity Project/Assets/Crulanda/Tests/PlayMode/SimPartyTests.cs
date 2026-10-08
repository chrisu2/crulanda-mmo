#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>Inviting sims (Phase 5.2b): one joins, follows, fights your target and can be turned on; the busy and the far-off decline; Leave party sends it back to the world.</summary>
    public class SimPartyTests
    {
        string root; EncounterSession session; float hourWas;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-party-" + Guid.NewGuid().ToString("N"));
            hourWas = WorldClock.Hour;
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(SimPopulation.Active);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            WorldClock.Hour = hourWas;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        /// <summary>An Oakhaven sim, online now, made willing and of the player's level, with a figure standing.</summary>
        SimAdventurer Willing(string classId = null)
        {
            var pop = SimPopulation.Active;
            var s = pop.World.sims.First(x => x.zone == "zone.oakhaven" && (classId == null || x.classId == classId) || x == pop.World.sims.Last());
            s.zone = "zone.oakhaven"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = session.Progress.Level;
            pop.Refresh(); Assert.NotNull(pop.Find(s.id), s.name + " stands in Oakhaven"); return s;
        }

        [UnityTest] public IEnumerator A_sim_joins_follows_and_leaves_again()
        {
            var s = Willing(); yield return null;
            Assert.IsNull(session.InviteRefusal(s));
            Assert.IsTrue(session.Invite(s.id), "joins");
            var c = session.PartySim(s.id); Assert.NotNull(c); Assert.IsNull(SimPopulation.Active.Find(s.id), "no second figure in the world");
            Assert.AreSame(c.actor, session.PartyActor(s.id), "a party member the enemies can turn on");
            Assert.AreEqual(SimCompanion.MaxHealthFor(s), c.actor.Health.Pool.Max);
            // Follows: the player steps away, the sim comes after.
            var motor = session.Player.GetComponent<AdventurerMotor>();
            motor.Teleport(c.transform.position + Vector3.forward * 12);
            float before = Vector3.Distance(c.transform.position, session.Player.transform.position);
            // Up to eight seconds of game time (four was tight under a full run's load, c46): it closes three metres of the twelve.
            for (float w = 0; w < 8 && Vector3.Distance(c.transform.position, session.Player.transform.position) >= before - 3; w += Time.deltaTime) yield return null;
            Assert.Less(Vector3.Distance(c.transform.position, session.Player.transform.position), before - 3, "it follows");
            session.LeaveParty(s.id);
            Assert.IsNull(session.PartySim(s.id)); Assert.NotNull(SimPopulation.Active.Find(s.id), "back in the world as a figure");
        }

        [UnityTest] public IEnumerator The_busy_and_the_far_off_decline_and_the_party_holds_three()
        {
            var s = Willing(); yield return null;
            s.friendly = .05f; StringAssert.Contains("declines", session.InviteRefusal(s)); Assert.IsFalse(session.Invite(s.id));
            s.friendly = .9f; s.level = session.Progress.Level + EncounterSession.InviteLevelGap + 1; StringAssert.Contains("too far apart", session.InviteRefusal(s));
            s.level = session.Progress.Level;
            var pop = SimPopulation.Active; int joined = 0;
            foreach (var x in pop.World.sims.Take(5)) { x.zone = "zone.oakhaven"; x.onlineFrom = 0; x.onlineHours = 24; x.friendly = .9f; x.level = session.Progress.Level; }
            pop.Refresh(); yield return null;
            foreach (var x in pop.World.sims.Take(5)) if (session.Invite(x.id)) joined++;
            Assert.AreEqual(EncounterSession.MaxPartySims, joined); Assert.AreEqual("Your party is full.", session.InviteRefusal(pop.World.sims[4]));
        }

        [UnityTest] public IEnumerator A_party_sim_fights_your_target()
        {
            var s = Willing("class.ranger"); yield return null;
            Assert.IsTrue(session.Invite(s.id)); var c = session.PartySim(s.id);
            var e = session.Enemies.Find(x => x != null && x.actor.IsAlive && !x.Game); Assert.NotNull(e);
            session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * 12);
            c.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(e.transform.position + Vector3.back * 11 + Vector3.left * 2);
            session.Select(e); e.Receive(1, session.Player);   // the fight is on
            for (float w = 0; w < 2 && e.GroupShares <= 0; w += Time.deltaTime) yield return null;   // scaled to the group as it starts (EncounterEnemy.GroupScale)
            int hp = e.actor.Health.Pool.Current;
            float t = 0; while (t < 6 && e.actor.Health.Pool.Current > hp - c.Hit) { t += Time.deltaTime; yield return null; }
            Assert.LessOrEqual(e.actor.Health.Pool.Current, hp - c.Hit, s.name + " (" + c.Activity + ") shot at it");
        }

        /// <summary>5.6: roles by class; a sim leads a run to a camp and calls it when the camp is cleared; /assist takes the party's fight; /lead when nothing fits says so.</summary>
        [UnityTest] public IEnumerator A_warrior_tanks_and_leads_a_run_to_a_camp()
        {
            var s = Willing("class.warrior"); yield return null;
            Assert.IsTrue(session.Invite(s.id)); var c = session.PartySim(s.id);
            Assert.AreEqual(SimCompanion.PartyRole.Tank, c.Role);
            session.PlayerChat("/assist"); StringAssert.Contains("Nobody in the party is fighting", session.Messages.Last());
            Assert.IsTrue(session.Lead(s.name), session.Messages.Last()); Assert.IsTrue(c.Leading.HasValue, "a camp picked");
            Assert.IsTrue(session.Chat.Any(l => l.channel == ChatChannel.Party && l.speaker == s.name && l.text.ToLower().Contains("follow me")), "said in Party");
            float t = 0; while (t < 4 && !c.Activity.StartsWith("Leading")) { t += Time.deltaTime; yield return null; }
            StringAssert.StartsWith("Leading you to the", c.Activity);
            // The camp falls (to the test): the run is called done.
            var camp = c.Leading.Value;
            foreach (var e in session.Enemies) if (e != null && e.actor.IsAlive && e.Camp && !e.Game && Vector3.Distance(e.transform.position, camp) < 16) e.actor.Health.ApplyDamage(100000);
            session.Player.GetComponent<AdventurerMotor>().Teleport(c.transform.position + Vector3.back * 3);
            t = 0; while (t < 6 && c.Leading.HasValue) { t += Time.deltaTime; yield return null; }
            Assert.IsFalse(c.Leading.HasValue, "the run is done"); StringAssert.Contains("cleared", session.Messages.Last());
        }

        /// <summary>Round 27: the party is saved with the world and comes back with you; a sim's invitation is accepted from the screen.</summary>
        [UnityTest] public IEnumerator The_party_is_kept_and_a_sim_can_invite_you()
        {
            var s = Willing("class.ranger"); yield return null;
            Assert.IsTrue(session.Invite(s.id)); SimPopulation.Active.Persist();
            CollectionAssert.Contains(SimPopulation.Active.World.party, s.id, "saved with the world");
            // As after a zone change: the party gone from the scene, the saved list brings it back without asking.
            var kept = new System.Collections.Generic.List<string>(SimPopulation.Active.World.party);
            session.LeaveParty(s.id); Assert.IsFalse(session.InParty(s.id));
            SimPopulation.Active.RestoreParty(kept); yield return null;
            Assert.IsTrue(session.InParty(s.id), "with you again"); StringAssert.Contains(s.name, session.Messages.Last());
            session.LeaveParty(s.id); yield return null;
            // An invitation from a sim: Accept brings it into the party.
            session.InvitedBy(s); Assert.AreSame(s, session.PendingInvite);
            session.AnswerInvite(true); yield return null;
            Assert.IsTrue(session.InParty(s.id), "joined by accepting"); Assert.IsNull(session.PendingInvite);
        }
        /// <summary>Round 27: a guild sim asks you in; accepting puts you in its guild (not its group), the guild welcomes you and answers
        /// you in Guild chat; /guild lists it; /gquit leaves. /g says so when you are in none.</summary>
        [UnityTest] public IEnumerator Joining_a_guild_and_talking_in_it()
        {
            var s = Willing("class.paladin"); s.guild = "The Lantern Watch"; s.chatty = .9f; yield return null;
            Assert.IsFalse(session.InGuild);
            session.PlayerChat("/g hello"); StringAssert.Contains("not in a guild", session.Messages.Last());
            session.GuildInvitedBy(s); Assert.AreSame(s, session.PendingInvite); Assert.AreEqual("The Lantern Watch", session.PendingGuild);
            session.AnswerInvite(true); yield return null;
            Assert.AreEqual("The Lantern Watch", session.Guild); Assert.IsTrue(session.SameGuild(s)); Assert.IsNull(session.PendingInvite);
            Assert.IsFalse(session.InParty(s.id), "a guild, not a group");
            float until = Time.time + 12; while (Time.time < until && !session.Chat.Any(l => l.channel == ChatChannel.Guild && l.speaker == s.name)) yield return null;
            Assert.IsTrue(session.Chat.Any(l => l.channel == ChatChannel.Guild && l.speaker == s.name), "welcomed in Guild");
            session.PlayerChat("/g hello all"); var said = session.Chat.Last(); Assert.AreEqual(ChatChannel.Guild, said.channel); Assert.AreEqual("You", said.speaker);
            until = Time.time + 12; while (Time.time < until && session.Chat.Last().speaker == "You") yield return null;
            var answer = session.Chat.Last(); Assert.AreEqual(ChatChannel.Guild, answer.channel, "a guild-mate answers"); Assert.AreNotEqual("You", answer.speaker);
            session.PlayerChat("/guild"); StringAssert.Contains("<The Lantern Watch>", session.Messages.Last()); StringAssert.Contains(s.name, session.Messages.Last());
            session.PlayerChat("/gquit"); Assert.IsFalse(session.InGuild);
        }
        /// <summary>Round 27: /dungeon routes the run through Crowsfoot Hollow's camps in the passage's order, the boss (Caddock's hall) last.</summary>
        [UnityTest] public IEnumerator A_sim_leads_a_dungeon_run_camp_by_camp()
        {
            var s = Willing("class.warrior"); yield return null; Assert.IsTrue(session.Invite(s.id)); var c = session.PartySim(s.id);
            session.Progress.experience = EncounterProgress.XpForLevel(5);
            Assert.IsNotNull(c.LeadDungeon(out var why), why); Assert.AreEqual("Crowsfoot Hollow", c.Dungeon);
            Assert.GreaterOrEqual(c.CampsLeft, 5, "the hollow's camps");
            Assert.AreEqual("Hollow lookouts", c.LeadingName, "the first camp in from the mouth");
            Assert.IsTrue(session.Chat.Any(l => l.channel == ChatChannel.Party && l.speaker == s.name && l.text.Contains("Crowsfoot Hollow")), "said in Party");
            c.StopLeading(); session.Progress.experience = 0; Assert.IsNull(c.LeadDungeon(out why), "too low"); StringAssert.Contains("too much", why);
        }
        /// <summary>Round 27: buying from a sim moves its goods and your coin to each other; selling it ore pays half as much again.</summary>
        [UnityTest] public IEnumerator Trading_with_a_sim_moves_goods_and_coin()
        {
            var s = Willing("class.warrior"); yield return null; var fig = SimPopulation.Active.Find(s.id);
            session.Player.GetComponent<AdventurerMotor>().Teleport(fig.transform.position + Vector3.back * 2); yield return null;
            s.goodIds.Clear(); s.goodCounts.Clear(); SimEconomy.Add(s, "mat.copper_ore", 3); s.coin = 100; session.Progress.gold = 50;
            Assert.IsTrue(session.OpenSimTrade(s, fig.transform.position)); CollectionAssert.Contains(session.VendorStock, "mat.copper_ore");
            var ore = session.Items.Get("mat.copper_ore"); int price = session.VendorPrice(ore);
            Assert.AreEqual(Mathf.Max(1, ore.value * 2), price, "twice its value");
            session.Buy("mat.copper_ore");
            Assert.AreEqual(50 - price, session.Progress.gold); Assert.AreEqual(100 + price, s.coin); Assert.AreEqual(2, SimEconomy.Count(s, "mat.copper_ore"));
            int slot = session.Progress.bag.FindIndex(b => !b.Empty && b.item == "mat.copper_ore"); Assert.GreaterOrEqual(slot, 0, "in your bags");
            int offer = session.SimOffer(s, ore, 1); Assert.AreEqual(Mathf.Max(1, Mathf.RoundToInt(ore.value * 1.5f)), offer, "a smith pays more for ore");
            session.SellBag(slot); Assert.AreEqual(3, SimEconomy.Count(s, "mat.copper_ore"), "back to it");
            session.CloseVendor(); Assert.IsNull(session.VendorSim);
        }
        [UnityTest] public IEnumerator Mobs_grow_with_the_group_by_its_levels()
        {
            var e = session.Enemies.Find(x => x != null && x.actor.IsAlive && !x.Game); Assert.NotNull(e);
            int mobLevel = e.actor.Level, alone = e.actor.Health.Pool.Max;
            float mira = session.Companion != null && session.Companion.actor.IsAlive && session.Progress.recruited ? Mathf.Clamp((float)session.Progress.Level / Mathf.Max(1, mobLevel), EncounterEnemy.MinShare, EncounterEnemy.MaxShare) : 0;   // Mira is a member like any other
            Assert.AreEqual(mira, EncounterEnemy.SharesFor(session, mobLevel), 1e-4f, "alone: only Mira's share, if she is with you");
            var pop = SimPopulation.Active; var two = pop.World.sims.Take(2).ToList();
            foreach (var x in two) { x.zone = "zone.oakhaven"; x.onlineFrom = 0; x.onlineHours = 24; x.friendly = .9f; x.level = session.Progress.Level; }
            pop.Refresh(); yield return null;
            foreach (var x in two) Assert.IsTrue(session.Invite(x.id), x.name);
            // Each sim's share is its level over the mob's, a quarter to one and a quarter.
            float shares = mira + two.Sum(x => Mathf.Clamp((float)x.level / mobLevel, EncounterEnemy.MinShare, EncounterEnemy.MaxShare));
            Assert.AreEqual(shares, EncounterEnemy.SharesFor(session, mobLevel), 1e-4f);
            two[1].level = 1; Assert.Less(EncounterEnemy.SharesFor(session, Mathf.Max(8, mobLevel)), shares + .001f, "a low sim adds little");
            two[1].level = session.Progress.Level;
            session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * 3); session.Select(e);
            e.Receive(1, session.Player);
            float t = 0; while (t < 3 && e.GroupShares <= 0) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(shares, e.GroupShares, 1e-3f, "scaled when the fight starts");
            Assert.AreEqual(Mathf.Round(alone * (1 + EncounterEnemy.HealthPerShare * shares)), e.actor.Health.Pool.Max, 1, "health grows by the shares");
            StringAssert.Contains("group of " + (3 + (mira > 0 ? 1 : 0)), e.GroupNote);
            e.ResetFight();
            Assert.AreEqual(alone, e.actor.Health.Pool.Max, "and is itself again when the fight resets");
        }
    }
}
#endif
