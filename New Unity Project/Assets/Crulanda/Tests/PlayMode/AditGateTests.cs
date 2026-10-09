#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Sealed Adit's dungeon objects (DUNGEON_DESIGN.md section 9, step D3): the cage-lift gate shut until the three rail sigils
    /// are set (taken from the branch bosses, kept for good), the platform gate opened quietly by a freed goblin or loudly with powder
    /// (the hall comes), a sim's run stopping at a shut gate, the Gallery's spirit stone as where you wake, and the rare Quiet Miner.
    /// </summary>
    public class AditGateTests
    {
        string root; EncounterSession session;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-aditgate-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return Load();
        }
        IEnumerator Load()
        {
            ZoneBuilder.RequestedZoneId = "zone.adit";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            EncounterSession.RareDice = () => UnityEngine.Random.value;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        ZoneInteractable Thing(string kind) { var i = session.Zone.Interactables.Find(x => x.kind == kind); Assert.NotNull(i, "the Adit has its " + kind); return i; }
        EncounterEnemy Boss(string name) { var e = session.Enemies.Find(x => x != null && x.Camp && x.Elite && x.actor.DisplayName.StartsWith(name)); Assert.NotNull(e, name); return e; }
        bool Walkable(Vector2 to)
        {
            var z = session.Zone; var path = new NavMeshPath();
            return NavMesh.SamplePosition(z.Ground(z.Zone.spawns.player, .2f), out var a, 2.5f, NavMesh.AllAreas) && NavMesh.SamplePosition(z.StandAt(to, float.NegativeInfinity), out var b, 2.5f, NavMesh.AllAreas)
                && NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }
        Vector2 CampAt(string name) { return session.Zone.Zone.camps.First(c => c.name == name).center; }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest] public IEnumerator The_gates_are_shut_and_a_sim_run_stops_at_the_lift()
        {
            var lift = Thing("liftgate").gate; var platform = Thing("platformgate").gate;
            Assert.IsFalse(lift.Open); Assert.IsFalse(platform.Open);
            Assert.Less(lift.Along, platform.Along, "the lift at the stair's head, the platform gate at its foot");
            Assert.IsTrue(Walkable(CampAt("Gallery watch")), "the Gallery is open");
            Assert.IsFalse(Walkable(CampAt("Stair watch")), "nobody walks through the cage-lift gate");
            Assert.AreEqual("Set the rail sigils (0/3)", session.InteractPromptFor(Thing("liftgate")));
            var pop = SimPopulation.Active; var s = pop.World.sims.First();
            s.zone = "zone.adit"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = 11; session.Progress.experience = EncounterProgress.XpForLevel(9);
            var p = session.Player.transform.position; s.x = p.x + 2; s.z = p.z; pop.Refresh(); yield return null;
            Assert.IsTrue(session.Invite(s.id, true)); var c = session.PartySim(s.id);
            Assert.IsNotNull(c.LeadDungeon(out var why), why);
            Assert.AreEqual(ZoneBuilder.AditLiftGate, c.StoppedBy, "the run stops at the lift");
            int before = session.Zone.Zone.camps.Count(cp => cp.cave != "The Sealed Adit" || cp.along < lift.Along);
            Assert.AreEqual(before, c.CampsLeft, "every camp this side of the lift, the branches too");
        }
        [UnityTest] public IEnumerator The_sigils_are_won_from_the_branch_bosses_and_wake_the_lift_for_good()
        {
            var i = Thing("liftgate");
            foreach (var (boss, key) in new[] { ("Nix", "sigil.amber"), ("Cinder-Warden Ysolt", "sigil.ember") })
            {
                var e = Boss(boss); e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3); yield return null;
                Assert.Contains(key, session.Progress.keys, boss + " carried " + key);
            }
            session.UseInteractable(i); yield return null;
            Assert.IsFalse(i.gate.Open, "two sigils are not enough");
            var foreman = Boss("The Foreman Who Forgot"); foreman.actor.Health.ApplyDamage(foreman.actor.Health.Pool.Max * 3); yield return null;
            Assert.Contains("sigil.grey", session.Progress.keys);
            Assert.AreEqual("Set the rail sigils (3/3)", session.InteractPromptFor(i));
            session.UseInteractable(i); yield return Frames(3);
            Assert.IsTrue(i.gate.Open); Assert.Contains(EncounterSession.LiftWoken, session.Progress.keys);
            Assert.IsTrue(i.gate.Lights.All(l => l.activeSelf), "the three sigils lit in their sockets");
            Assert.IsTrue(Walkable(CampAt("Stair watch")), "the stair is open");
            Assert.IsFalse(Walkable(CampAt("Platform guards")), "the platform gate still bars the hall");
            // Kept for good: a new visit finds the lift open.
            session.Save(false); yield return Load();
            Assert.IsTrue(Thing("liftgate").gate.Open, "the lift stays woken");
        }
        [UnityTest] public IEnumerator Powder_blows_the_platform_gate_and_the_hall_comes()
        {
            var gate = Thing("platformgate");
            session.UseInteractable(gate); yield return null;
            Assert.IsFalse(gate.gate.Open, "barred, with no powder and no goblin");
            Assert.AreEqual("Try the gate", session.InteractPromptFor(gate));
            session.UseInteractable(Thing("powder")); Assert.IsTrue(session.CarryingPowder);
            Assert.AreEqual("Blow the gate (loud)", session.InteractPromptFor(gate));
            int ready = session.Enemies.Count(e => e != null && e.Camp && !e.Elite && e.CanAnswer);
            session.UseInteractable(gate); yield return Frames(3);
            Assert.IsTrue(gate.gate.Open); Assert.IsFalse(session.CarryingPowder, "the powder is spent");
            int coming = ready - session.Enemies.Count(e => e != null && e.Camp && !e.Elite && e.CanAnswer);
            Assert.GreaterOrEqual(coming, 3, "the Quartermaster's people come for you");
        }
        [UnityTest] public IEnumerator A_freed_goblin_opens_the_platform_gate_quietly()
        {
            session.Progress.questsDone.Add(EncounterSession.PressedQuest);
            var gate = Thing("platformgate");
            Assert.AreEqual("Call the goblins to the gate (quiet)", session.InteractPromptFor(gate));
            int ready = session.Enemies.Count(e => e != null && e.Camp && !e.Elite && e.CanAnswer);
            session.UseInteractable(gate); yield return Frames(3);
            Assert.IsTrue(gate.gate.Open);
            Assert.AreEqual(ready, session.Enemies.Count(e => e != null && e.Camp && !e.Elite && e.CanAnswer), "nobody in the hall heard");
        }
        [UnityTest] public IEnumerator The_spirit_stone_is_where_you_wake()
        {
            var stone = Thing("stone"); var outside = session.RecoveryPoint;
            session.UseInteractable(stone);
            Assert.AreEqual(1, session.Progress.spiritStones.Count);
            Assert.Less(Vector3.Distance(session.RecoveryPoint, stone.position), 2, "you wake beside it");
            Assert.Greater(Vector3.Distance(session.RecoveryPoint, outside), 30, "not out in the yard");
            session.Player.Health.ApplyDamage(session.Player.Health.Pool.Max * 3); yield return null;
            session.Recover(); yield return Frames(2);
            Assert.Less(Vector3.Distance(session.Player.transform.position, stone.position), 3, "woken beside the stone");
            Assert.IsTrue(NavMesh.SamplePosition(session.Player.transform.position, out _, 2, NavMesh.AllAreas), "on the Gallery's floor");
        }
        [UnityTest] public IEnumerator The_Quiet_Miner_is_there_one_visit_in_five()
        {
            var camp = session.Zone.Zone.camps.Last(); Assert.AreEqual("The Quiet Miner", camp.mob); Assert.AreEqual(.2f, camp.rare, 1e-5f);
            Assert.IsFalse(Achievements.EliteKeys(session.Zone.Zone).Any(k => k.Contains("quietminer")), "no deed waits on a rare one");
            EncounterSession.RareDice = () => 0f; yield return Load();
            Assert.IsTrue(session.Enemies.Exists(e => e != null && e.Elite && e.actor.DisplayName.StartsWith("The Quiet Miner")), "there on a lucky visit");
            int with = session.Enemies.Count(e => e != null && e.Camp);
            EncounterSession.RareDice = () => .99f; yield return Load();
            Assert.IsFalse(session.Enemies.Exists(e => e != null && e.actor.DisplayName.StartsWith("The Quiet Miner")), "not there on the others");
            Assert.AreEqual(with - 1, session.Enemies.Count(e => e != null && e.Camp), "and nobody else is missing");
        }
    }
}
#endif
