#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// <summary>Party loot (Round 25, playtest note 72): coins split round the party; uncommon and better rolled for, Need over Greed, the winner takes it (a sim wearing what it needed); all passing leaves it to you.</summary>
    public class PartyLootTests
    {
        string root; EncounterSession session; float hourWas;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-ploot-" + Guid.NewGuid().ToString("N"));
            hourWas = WorldClock.Hour;
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var pop = SimPopulation.Active; int n = 0;
            foreach (var x in pop.World.sims) { if (n >= 2) break; x.zone = "zone.oakhaven"; x.onlineFrom = 0; x.onlineHours = 24; x.friendly = .9f; x.level = session.Progress.Level; x.classId = n == 0 ? "class.warrior" : "class.mage"; x.wornSlots.Clear(); x.wornIds.Clear(); x.coin = 0; n++; }
            pop.Refresh(); yield return null;
            foreach (var x in pop.World.sims.Take(2)) Assert.IsTrue(session.Invite(x.id), x.name);
            Assert.AreEqual(2, session.PartySims.Count);
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
        EncounterEnemy Body(int coins, params string[] items)
        {
            var e = session.Enemies.First(x => x != null && x.Camp && !x.Game && x.actor.IsAlive);
            e.actor.Health.ApplyDamage(100000);
            session.PutLoot(e, coins, items.Select(i => new LootDrop(i, 1)));
            session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * 2);
            return e;
        }

        [UnityTest] public IEnumerator Coins_split_and_an_uncommon_hauberk_goes_to_the_warrior_who_needs_it()
        {
            string hauberk = ItemDatabase.GearId("chest", session.Progress.Level, 2, 7); var def = session.Items.Get(hauberk);
            for (int seed = 8; def == null || !def.name.Contains("Hauberk"); seed++) { hauberk = ItemDatabase.GearId("chest", session.Progress.Level, 2, seed); def = session.Items.Get(hauberk); }
            var e = Body(30, hauberk); yield return null;
            int goldBefore = session.Progress.gold; var warrior = session.PartySims.First(c => c.sim.classId == "class.warrior"); var mage = session.PartySims.First(c => c.sim.classId == "class.mage");
            session.OpenLoot(e); session.TakeLootCoins();
            Assert.AreEqual(goldBefore + 10, session.Progress.gold, "your third"); Assert.AreEqual(10, warrior.sim.coin); Assert.AreEqual(10, mage.sim.coin);
            Assert.IsTrue(session.Rolled(hauberk), "uncommon in a party is rolled for");
            session.TakeLoot(0);
            Assert.NotNull(session.Roll, "a roll began"); Assert.AreEqual(3, session.Roll.members.Count);
            Assert.AreEqual(RollChoice.Need, warrior.RollFor(def), "the warrior needs mail"); Assert.AreEqual(RollChoice.Greed, mage.RollFor(def), "the mage would sell it");
            session.ChooseRoll("you", RollChoice.Pass);
            float t = 0; while (t < 8 && !session.Roll.Done) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(session.Roll.Done, "resolved once all had chosen"); Assert.AreEqual(warrior.sim.id, session.Roll.Winner, "Need beats Greed");
            Assert.AreEqual(hauberk, SimEconomy.Worn(warrior.sim, "chest"), "worn by the winner");
            Assert.IsTrue(session.Chat.Any(l => l.channel == ChatChannel.Party && l.text.Contains("won")), "said in Party");
        }

        [UnityTest] public IEnumerator All_passing_leaves_it_to_you_and_a_common_is_simply_taken()
        {
            string rare = ItemDatabase.GearId("neck", session.Progress.Level, 3, 3), common = ItemDatabase.GearId("hands", session.Progress.Level, 1, 2);
            var e = Body(0, rare, common); yield return null;
            session.OpenLoot(e); session.TakeLoot(1);
            Assert.IsTrue(Inventory.Has(session.Progress, common), "a common piece is taken outright"); Assert.IsNull(session.Roll);
            session.TakeLoot(0); Assert.NotNull(session.Roll);
            foreach (var m in session.Roll.members.ToArray()) session.ChooseRoll(m.id, RollChoice.Pass);
            Assert.IsTrue(session.Roll.Done); Assert.AreEqual("you", session.Roll.Winner); Assert.IsTrue(Inventory.Has(session.Progress, rare), "nobody wanted it: yours");
        }
    }
}
#endif
