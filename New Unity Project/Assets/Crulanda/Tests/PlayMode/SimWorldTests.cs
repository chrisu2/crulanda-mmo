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
    /// <summary>5.3b and 5.3c in play: a sim levels and dings (and is congratulated), dies and runs back for its corpse, takes a road out of the zone (and the unseen move), sells at the stall and buys better gear, and a smith forges at the forge.</summary>
    public class SimWorldTests
    {
        string root; EncounterSession session; float hourWas;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-world-" + Guid.NewGuid().ToString("N"));
            hourWas = WorldClock.Hour; SimPopulation.LifeOverride = true;
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(SimPopulation.Active);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            WorldClock.Hour = hourWas; SimPopulation.LifeOverride = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        SimFigure SimAt(Vector3 at, int level, string classId = "class.warrior", int which = 0)
        {
            var pop = SimPopulation.Active; var s = pop.World.sims[which];
            s.zone = "zone.oakhaven"; s.onlineFrom = 0; s.onlineHours = 24; s.level = level; s.classId = classId; s.x = at.x; s.z = at.z; s.chatty = .9f; s.friendly = .9f;
            s.experience = 0; s.coin = 0; s.gearBonus = 0; s.goodIds.Clear(); s.goodCounts.Clear(); s.wornSlots.Clear(); s.wornIds.Clear();
            var old = pop.Find(s.id); if (old != null) { pop.Figures.Remove(old); UnityEngine.Object.Destroy(old.gameObject); }
            pop.Refresh(); var f = pop.Find(s.id); Assert.NotNull(f); return f;
        }
        void Chatty() { foreach (var s in SimPopulation.Active.World.sims.Take(4)) { s.zone = "zone.oakhaven"; s.onlineFrom = 0; s.onlineHours = 24; s.chatty = .9f; } SimPopulation.Active.Refresh(); }

        [UnityTest] public IEnumerator A_sim_levels_from_its_kills_and_dings()
        {
            Chatty(); var f = SimAt(Vector3.zero, 2, "class.mage", 1); yield return null;
            int hp = f.actor.Health.Pool.Max;
            f.GainXp(EncounterProgress.XpToNext(2));
            Assert.AreEqual(3, f.sim.level, "level 3"); Assert.Greater(f.actor.Health.Pool.Max, hp, "more health at the new level");
            Assert.IsTrue(session.Chat.Any(l => l.speaker == f.sim.name && l.text.Contains("ding")), "ding in the chat");
            float t = 0; while (t < 10 && !session.Chat.Any(l => l.text.Contains("grats") || l.text.Contains("gz"))) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(session.Chat.Any(l => l.text.Contains("grats") || l.text.Contains("gz")), "a grats from another sim");
        }

        [UnityTest] public IEnumerator A_beaten_sim_comes_to_at_the_recovery_point_and_runs_back_for_its_corpse()
        {
            var far = session.RecoveryPoint + Vector3.forward * 25; var f = SimAt(far, 3); yield return null;
            var fell = f.transform.position;
            f.actor.Health.ApplyDamage(100000); yield return null;
            Assert.IsFalse(f.actor.IsAlive); Assert.AreEqual(SimFigure.Doing.Down, f.Activity);
            float t = 0; while (t < 14 && !f.actor.IsAlive) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(f.actor.IsAlive, "up again"); Assert.AreEqual(SimFigure.Doing.CorpseRun, f.Activity);
            Assert.Less(Vector3.Distance(f.transform.position, session.RecoveryPoint), 8, "at the recovery point");
            Assert.NotNull(GameObject.Find("Corpse of " + f.sim.name), "its corpse lies where it fell");
            t = 0; while (t < 40 && f.Activity == SimFigure.Doing.CorpseRun) { t += Time.deltaTime; yield return null; }
            Assert.AreNotEqual(SimFigure.Doing.CorpseRun, f.Activity, "recovered its corpse"); Assert.Less(Vector3.Distance(f.transform.position, fell), 4);
            Assert.IsNull(GameObject.Find("Corpse of " + f.sim.name));
        }

        [UnityTest] public IEnumerator A_sim_takes_the_road_out_and_the_unseen_move_too()
        {
            var exit = session.Zone.Zone.exits.First(); var at = session.Zone.Ground(exit.at);
            var f = SimAt(at + Vector3.forward * 6, 5, "class.ranger", 2); yield return null;
            f.Begin(SimFigure.Doing.Travel, exit: exit);
            StringAssert.Contains("off to", f.Doings);
            float t = 0; while (t < 30 && SimPopulation.Active.Find(f.sim.id) != null) { t += Time.deltaTime; yield return null; }
            var s = SimPopulation.Active.World.sims[2];
            Assert.IsNull(SimPopulation.Active.Find(s.id), "its figure is gone"); Assert.AreEqual(exit.to, s.zone, "in the other zone"); Assert.AreEqual(exit.arrive.x, s.x, 1e-3f);
            // Unseen: a level-12 sim stuck in Oakhaven's data moves to a zone that suits it when its hour comes.
            var away = SimPopulation.Active.World.sims[3]; away.zone = "zone.khaven"; away.level = 12; away.onlineFrom = 0; away.onlineHours = 24; away.nextTravelHour = WorldClock.Hour;
            SimPopulation.Active.Refresh();
            Assert.AreNotEqual("zone.khaven", away.zone, "took a road toward its level: " + away.zone);
        }

        [UnityTest] public IEnumerator A_sim_sells_at_the_stall_buys_an_upgrade_and_a_smith_forges()
        {
            var stall = VillageLife.Active.Places["stall"][0]; var f = SimAt(stall + Vector3.right * 3, 3); yield return null;
            SimEconomy.Add(f.sim, "mat.copper_ore", 30); f.sim.coin = SimEconomy.UpgradeCost(f.sim) - 10;
            f.Begin(SimFigure.Doing.Trade);
            float t = 0; while (t < 20 && SimEconomy.GoodsCount(f.sim) > 0) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(0, SimEconomy.GoodsCount(f.sim), "sold"); Assert.AreEqual(1, f.sim.gearBonus, "and bought an upgrade with the coin");
            var forge = VillageLife.Active.Places["forge"][0]; var g = SimAt(forge + Vector3.right * 3, 3, "class.warrior", 1); yield return null;
            SimEconomy.Add(g.sim, "mat.copper_bar", 2); SimEconomy.Add(g.sim, "mat.oak_log", 1);
            g.Begin(SimFigure.Doing.Craft);
            Assert.AreEqual(SimFigure.Doing.Craft, g.Activity); Assert.AreEqual("craft.copper_cudgel", g.Recipe?.output);
            t = 0; while (t < 25 && SimEconomy.Worn(g.sim, "mainhand") == null) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual("craft.copper_cudgel", SimEconomy.Worn(g.sim, "mainhand"), "forged and worn (" + g.Doings + ")");
        }
    }
}
#endif
