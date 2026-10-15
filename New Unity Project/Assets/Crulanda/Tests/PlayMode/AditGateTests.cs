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
    /// The Sealed Adit's dungeon objects (DUNGEON_DESIGN.md section 9, step D3; ADIT_REWORK.md section 8): the singing door (it was the
    /// winch gate) shut until the three rail sigils are set (taken from the wings' bosses, kept for good), the platform gate opened quietly
    /// by a freed goblin or loudly with powder (the hall comes), a sim's run stopping at a shut gate, the Gallery's spirit stone as where
    /// you wake, and the rare Quiet Miner.
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
            EncounterSession.RareDice = () => UnityEngine.Random.value; EncounterSession.RideSeconds = 5;
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
        /// <summary>The doors the wings' bosses open by dying, the bars and the pen opened, as a run past them finds them (the lift and the
        /// platform gate stay as they are).</summary>
        void OpenTheBossDoors() { foreach (var g in session.Zone.Gates) if (g != null && g.Kind != "liftgate" && g.Kind != "platformgate") g.SetOpen(true, true); }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest] public IEnumerator The_gates_are_shut_and_a_sim_run_stops_at_the_lift()
        {
            var lift = Thing("liftgate").gate; var platform = Thing("platformgate").gate;
            Assert.IsFalse(lift.Open); Assert.IsFalse(platform.Open);
            Assert.Less(lift.Along, platform.Along, "the lift at the stair's head, the platform gate at its foot");
            Assert.IsTrue(Walkable(CampAt("Spoil heaps")), "the Workings are open to Lusk's door");
            Assert.IsFalse(Walkable(CampAt("Pool watch")), "Lusk's door is shut until he falls");
            OpenTheBossDoors(); yield return Frames(60);
            Assert.IsTrue(Walkable(CampAt("Pool watch")), "the Gallery is open");
            Assert.IsFalse(Walkable(CampAt("Stair guard")), "nobody walks through the singing door");
            Assert.AreEqual("Set the rail sigils (0/3)", session.InteractPromptFor(Thing("liftgate")));
            var pop = SimPopulation.Active; var s = pop.World.sims.First();
            s.zone = "zone.adit"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = 11; session.Progress.experience = EncounterProgress.XpForLevel(9);
            var p = session.Player.transform.position; s.x = p.x + 2; s.z = p.z; pop.Refresh(); yield return null;
            Assert.IsTrue(session.Invite(s.id, true)); var c = session.PartySim(s.id);
            Assert.IsNotNull(c.LeadDungeon(out var why), why);
            Assert.AreEqual(ZoneBuilder.AditLiftGate, c.StoppedBy, "the run will wait at the lift (the bosses' doors stand open)");
            foreach (var past in new[] { "Stair guard", "Lower stair guard", "Quartermaster Brannigan Sorrel", "Rail-Captain Orsk Danner" })
                Assert.IsTrue(c.Route.Any(x => x.name == past), past + ": the fights past the gates are on the run too, which waits at them (D7)");
            Assert.AreEqual(c.Route.Count(x => !x.regroup), c.CampsLeft, "every fight counted, the regroups not");
        }
        /// <summary>(The Adit rework) A boss's death opens his door (Lusk's door, the chute door, the cloister door, the sluice-boards: shut on
        /// every visit), and a run that comes to one still shut (its boss left standing) waits at it, saying whose death opens it.</summary>
        [UnityTest] public IEnumerator A_bosss_death_opens_his_door_and_a_run_waits_at_one_still_shut()
        {
            Assert.IsFalse(Walkable(CampAt("Pool watch")), "Lusk's door is shut until he falls");
            foreach (var (boss, door) in new[] { ("Gang-Boss Haddo Lusk", "Lusk's door"), ("Nix", "The chute door"), ("Cinder-Warden Ysolt", "The cloister door"), ("The Foreman Who Forgot", "The sluice-boards") })
            {
                var g = session.Zone.Gates.Find(x => x != null && x.Title == door); Assert.NotNull(g, door);
                Assert.AreEqual("bossdoor", g.Kind, door); Assert.IsFalse(g.Open, door + " is shut at first");
                var e = Boss(boss); e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3); yield return null;
                Assert.IsTrue(g.Open, door + " opens as " + boss + " falls");
            }
            yield return Frames(60);
            Assert.IsTrue(Walkable(CampAt("Pool watch")), "through Lusk's door to the Gallery");
            // A run that finds Lusk's door shut again (as it would with Lusk left standing) waits at it.
            var lusk = session.Zone.Gates.Find(x => x != null && x.Title == "Lusk's door"); lusk.SetOpen(false, true); yield return Frames(2);
            var pop = SimPopulation.Active; var s = pop.World.sims.First();
            s.zone = "zone.adit"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = 11; session.Progress.experience = EncounterProgress.XpForLevel(9);
            var p = session.Player.transform.position; s.x = p.x + 2; s.z = p.z; pop.Refresh(); yield return null;
            Assert.IsTrue(session.Invite(s.id, true)); var c = session.PartySim(s.id);
            Assert.IsNotNull(c.LeadDungeon("quick", out var why), why);
            var camps = session.Zone.Zone.camps;   // the Workings this side of the door cleared: the run comes straight to it
            foreach (var e in session.Enemies.ToArray())
                if (e != null && e.Camp && e.actor.IsAlive && e.CampIndex >= 0 && e.CampIndex < camps.Length && camps[e.CampIndex].cave == lusk.Cave && camps[e.CampIndex].along < lusk.Along)
                    e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3);
            float t = 0; while (t < 8 && !c.WaitingAtGate) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(c.WaitingAtGate, "the run waits at Lusk's door"); Assert.AreEqual("Lusk's door", c.StoppedBy);
            Assert.IsTrue(session.Chat.Any(l => l.channel == ChatChannel.Party && l.text.ToLower().Contains("gang-boss haddo lusk")), "it says whose death opens it");
            lusk.SetOpen(true, true); yield return Frames(60);
            Assert.IsFalse(c.WaitingAtGate, "open: on we go"); Assert.IsNull(c.StoppedBy);
        }
        [UnityTest] public IEnumerator The_run_waits_at_the_lift_and_goes_on_when_it_opens()
        {
            var lift = Thing("liftgate"); OpenTheBossDoors();
            var pop = SimPopulation.Active; var s = pop.World.sims.First();
            s.zone = "zone.adit"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = 11; session.Progress.experience = EncounterProgress.XpForLevel(9);
            var p = session.Player.transform.position; s.x = p.x + 2; s.z = p.z; pop.Refresh(); yield return null;
            Assert.IsTrue(session.Invite(s.id, true)); var c = session.PartySim(s.id);
            Assert.IsNotNull(c.LeadDungeon(out var why), why);
            // Everything this side of the lift falls (the branches too): the run walks its route down to the gate and waits there.
            var camps = session.Zone.Zone.camps;
            foreach (var e in session.Enemies.ToArray())
                if (e != null && e.Camp && e.actor.IsAlive && e.CampIndex >= 0 && e.CampIndex < camps.Length && !(camps[e.CampIndex].cave == lift.gate.Cave && camps[e.CampIndex].along > lift.gate.Along))
                    e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3);
            var near = lift.gate.transform.position - lift.gate.transform.forward * 3f;
            session.Player.GetComponent<AdventurerMotor>().Teleport(near + Vector3.up); if (NavMesh.SamplePosition(near, out var hit, 3, NavMesh.AllAreas)) c.GetComponent<NavMeshAgent>().Warp(hit.position);
            float t = 0; while (t < 8 && !c.WaitingAtGate) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(c.WaitingAtGate, "the run waits at the lift"); StringAssert.EndsWith("singing door", c.LeadingName); Assert.AreEqual(ZoneBuilder.AditLiftGate, c.StoppedBy);
            t = 0; while (t < 4 && !c.Activity.StartsWith("Waiting at")) { t += Time.deltaTime; yield return null; }
            StringAssert.StartsWith("Waiting at the", c.Activity);
            Assert.IsTrue(session.Chat.Any(l => l.channel == ChatChannel.Party && l.text.Contains("rail sigils")), "it says what the gate wants");
            foreach (var k in new[] { "sigil.amber", "sigil.ember", "sigil.grey" }) session.Progress.keys.Add(k);
            session.UseInteractable(lift); yield return Frames(60);
            Assert.IsTrue(lift.gate.Open); Assert.IsFalse(c.WaitingAtGate, "open: on we go"); Assert.IsNull(c.StoppedBy);
            Assert.AreEqual("Stair guard", c.LeadingName, "the first camp past the lift (the Adit rework)");
        }
        [UnityTest] public IEnumerator The_sigils_are_won_from_the_branch_bosses_and_wake_the_lift_for_good()
        {
            var i = Thing("liftgate"); OpenTheBossDoors();
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
            session.UseInteractable(i); yield return Frames(60);
            Assert.IsTrue(i.gate.Open); Assert.Contains(EncounterSession.LiftWoken, session.Progress.keys);
            Assert.IsTrue(i.gate.Lights.All(l => l.activeSelf), "the three sigils lit in their sockets");
            Assert.IsTrue(Walkable(CampAt("Stair guard")), "the stair is open");
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
            session.UseInteractable(gate); yield return Frames(60);
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
            session.UseInteractable(gate); yield return Frames(60);
            Assert.IsTrue(gate.gate.Open);
            Assert.AreEqual(ready, session.Enemies.Count(e => e != null && e.Camp && !e.Elite && e.CanAnswer), "nobody in the hall heard");
        }
        [UnityTest] public IEnumerator The_carriage_rides_you_out_once_the_lock_is_matched()
        {
            EncounterSession.RideSeconds = .5f;
            var ride = Thing("carriage"); var yard = session.Zone.Ground(session.Zone.Zone.spawns.player, 1.1f);
            Assert.Greater(Vector3.Distance(ride.position, yard), 60, "the gangway is down in the hall");
            var motor = session.Player.GetComponent<AdventurerMotor>(); motor.Teleport(ride.position + Vector3.up);
            StringAssert.Contains("loading", session.InteractPromptFor(ride));
            session.UseInteractable(ride); yield return Frames(2);
            Assert.IsFalse(session.Working, "the Company's carriage takes nobody up");
            session.Progress.keys.Add(EncounterSession.WeaverLock);
            StringAssert.Contains("one ride out", session.InteractPromptFor(ride));
            session.UseInteractable(ride); Assert.IsTrue(session.Working, "on the bar");
            yield return new WaitForSeconds(.8f);
            Assert.IsFalse(session.Working);
            Assert.Less(Vector3.Distance(session.Player.transform.position, yard), 4, "at the siding under the yard");
            Assert.Less(Vector3.Distance(session.Companion.transform.position, yard), 6, "Mira too");
        }
        /// <summary>The cages (playtest note 125, Chris, 2026-10-11: "says open the cage but it is obviously broken and nothing in it"): barred
        /// on all four sides with a door, a goblin huddled in each; without Pib's quest the lock holds and says so; with it the door swings
        /// open and the goblin is gone.</summary>
        [UnityTest] public IEnumerator Each_cage_holds_a_goblin_and_opens_with_the_quest()
        {
            var cages = session.Zone.Interactables.Where(i => i.kind == "cage").ToList();
            Assert.AreEqual(5, cages.Count, "five cages");
            var goblins = UnityEngine.Object.FindObjectsByType<ActorVisual>(FindObjectsSortMode.None).Where(v => v.name == "Pressed goblin").ToList();
            float Across(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
            foreach (var c in cages)
            {
                Assert.NotNull(c.root.Find("Door"), "a door on each cage");
                Assert.IsTrue(goblins.Any(g => Across(g.transform.position, c.root.position) < .7f), "a goblin in the cage at " + c.root.position);
                foreach (var side in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                {
                    var mid = c.root.TransformPoint(side * .8f + Vector3.up * .95f);
                    Assert.IsTrue(c.root.GetComponentsInChildren<Renderer>(true).Any(r => r.name.Contains("Cylinder") && Vector3.Distance(r.bounds.center, mid) < .45f), "bars on its " + side + " side");
                }
            }
            var first = cages[0]; var door = first.root.Find("Door"); var shut = door.localRotation;
            session.Player.GetComponent<AdventurerMotor>().Teleport(first.position + Vector3.up); yield return Frames(2);
            // Without Pib's quest the lock holds, and the prompt says whose it is to open.
            StringAssert.Contains("Pib", session.InteractPromptFor(first));
            session.UseInteractable(first); yield return Frames(60);
            Assert.AreEqual(shut, door.localRotation, "still shut without the quest");
            Assert.IsFalse(session.Progress.usedInteractables.Contains(first.Key(session.Zone.Zone.id)), "not counted as opened");
            // With it: the door swings open and the goblin slips away.
            session.Progress.experience = EncounterProgress.XpForLevel(11);
            Assert.IsTrue(session.Quests.Accept(session.Quests.Def(EncounterSession.PressedQuest), "zone.peaks"), "Pib's quest taken");
            Assert.AreEqual("Open the cage", session.InteractPromptFor(first));
            session.UseInteractable(first); yield return new WaitForSeconds(1.2f);
            Assert.Greater(Quaternion.Angle(shut, door.localRotation), 80, "the door swings open");
            yield return new WaitForSeconds(3.5f);
            Assert.IsFalse(UnityEngine.Object.FindObjectsByType<ActorVisual>(FindObjectsSortMode.None).Any(v => v.name == "Pressed goblin" && Across(v.transform.position, first.root.position) < 1.2f), "its goblin is gone");
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
