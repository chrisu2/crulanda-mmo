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
    /// Crowsfoot Hollow, the Sandthrone deserters' cave in Oakhaven's north hills, and its quest "The Tin Crown" (GAME-ONLY):
    /// - the quest waits for level 3 (minLevel): no offer, no ! and no accepting at 2; its giver offers it at 3;
    /// - its kill targets are the hollow's camps (tags deserter and banditking), which wear their own looks, and its plunder is a usable prop;
    /// - played through: six deserters and Caddock, the plunder, and the hand-in;
    /// - a mob doesn't notice you through solid scenery (the rock between the chambers and the hillside).
    /// </summary>
    public class HollowQuestTests
    {
        const string QuestId = "side.oakhaven.crowsfoot", Plunder = "The deserters' plunder";
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindAnyObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;   // daytime: the giver is up and about
            root = Path.Combine(Path.GetTempPath(), "Crulanda-hollow-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session() { return UnityEngine.Object.FindAnyObjectByType<EncounterSession>(); }
        /// <summary>Puts the character at the start of a level (quests read the level from its experience).</summary>
        static void AtLevel(EncounterSession s, int level)
        {
            s.Progress.experience = EncounterProgress.XpForLevel(level); s.Player.SetLevel(level);
            Assert.AreEqual(level, s.Progress.Level);
        }

        [UnityTest] public IEnumerator The_tin_crown_waits_for_level_3_and_its_giver_lives_in_Oakhaven()
        {
            var s = Session(); Assert.NotNull(s.Quests, "Quest content loaded.");
            var q = s.Quests.Def(QuestId);
            Assert.NotNull(q, "The Crowsfoot Hollow quest is defined.");
            Assert.AreEqual(3, q.minLevel, "It waits for level 3.");
            Assert.AreEqual(4, q.level, "The quest sits in the middle of the dungeon's levels (3-5).");
            Assert.AreEqual("zone.oakhaven", q.zone);
            StringAssert.Contains("GAME-ONLY", q.canonStatus);
            Assert.NotNull(VillageLife.Active.Find(q.giver), "Nobody called '" + q.giver + "' in Oakhaven.");
            Assert.AreEqual(q.giver, q.turnIn);
            Assert.AreEqual(QuestId, VillageLife.HollowQuest, "The villagers' talk of the raids turns when this quest is done.");
            yield return null;
        }

        [UnityTest] public IEnumerator Not_offered_or_marked_at_level_2_and_offered_by_its_giver_at_level_3()
        {
            var s = Session(); var log = s.Quests; var q = log.Def(QuestId);
            var giver = VillageLife.Active.Find(q.giver);

            AtLevel(s, 2);
            Assert.IsFalse(log.For(q.giver, s.ZoneId, 2).Exists(o => o.quest == q), "Not offered at level 2.");
            Assert.AreNotEqual('!', log.Marker(q.giver, s.ZoneId, 2, out _), "No ! over the giver at level 2, not even a grey one.");
            Assert.IsFalse(log.Accept(q, s.ZoneId), "Can't be taken at level 2.");
            s.QuestTalk(giver.Name, giver.transform.position);
            Assert.IsTrue(s.Conversation == null || !s.Conversation.entries.Exists(o => o.quest == q), "Talking to the giver at level 2 doesn't bring it up.");
            s.Conversation = null;

            AtLevel(s, 3);
            Assert.IsTrue(log.For(q.giver, s.ZoneId, 3).Exists(o => o.quest == q && o.status == QuestStatus.Available), "Offered at level 3.");
            Assert.AreEqual('!', log.Marker(q.giver, s.ZoneId, 3, out bool grey)); Assert.IsFalse(grey, "A gold !, not a grey one.");
            Assert.IsTrue(s.QuestTalk(giver.Name, giver.transform.position), q.giver + " has the quest to give.");
            Assert.NotNull(s.Conversation); Assert.IsTrue(s.Conversation.entries.Exists(o => o.quest == q));
            s.AcceptQuest(q);
            Assert.AreEqual(QuestStatus.Active, log.Status(q, s.ZoneId));
            yield return null;
        }

        [UnityTest] public IEnumerator Its_kill_targets_are_the_hollow_camps_and_its_plunder_can_be_searched()
        {
            var s = Session(); var q = s.Quests.Def(QuestId); var camps = s.Zone.Zone.camps;
            var kills = q.steps.SelectMany(st => st.objectives).Where(o => o.type == "kill").ToList();
            Assert.AreEqual(2, kills.Count, "The deserters, and their king.");
            foreach (var tag in new[] { "deserter", "banditking" })
            {
                int c = Array.FindIndex(camps, x => x != null && x.tag == tag);
                Assert.GreaterOrEqual(c, 0, "Oakhaven has a camp tagged '" + tag + "'.");
                string id = "mob." + tag + ".oakhaven." + c + ".0";   // how EncounterSession.SpawnCamps names camp mobs
                var counting = kills.Where(o => QuestLog.Matches(o.target, id)).ToList();
                Assert.AreEqual(1, counting.Count, "Exactly one kill objective counts the '" + tag + "' camp.");
                Assert.IsTrue(s.Enemies.Exists(e => e.Camp && QuestLog.Matches(counting[0].target, e.persistentId)), "The '" + tag + "' camp spawned something to kill.");
            }
            Assert.IsTrue(camps.First(x => x != null && x.tag == "banditking").elite, "Caddock is an elite.");
            // Their own looks (ActorLook.Deserter / BanditKing), not the collector a mistyped look falls back to.
            foreach (var e in s.Enemies.Where(e => e.persistentId.StartsWith("mob.deserter.")))
                Assert.IsTrue(e.transform.Find("Body/Arm R/Falchion") != null || e.transform.Find("Body/Arm R/Club") != null, e.persistentId + " carries a deserter's falchion or club.");
            var king = s.Enemies.First(e => e.persistentId.StartsWith("mob.banditking."));
            Assert.NotNull(king.transform.Find("Body").GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Tin crown"), "Caddock wears the tin crown (on a model it sits in his fitted hat, on his head).");
            Assert.NotNull(king.transform.Find("Body/Arm R/Cleaver"), "Caddock carries the cleaver.");
            // The plunder in the deep chamber.
            var plunder = s.Zone.Interactables.Find(i => i.name == Plunder);
            Assert.NotNull(plunder, "'" + Plunder + "' is a usable prop in Oakhaven.");
            Assert.IsFalse(string.IsNullOrEmpty(plunder.prompt), "It has an E prompt.");
            Assert.IsTrue(q.steps.SelectMany(st => st.objectives).Any(o => o.type == "interact" && o.target == Plunder), "The quest sends you to search it.");
            yield return null;
        }

        [UnityTest] public IEnumerator Clear_the_hollow_search_the_plunder_and_hand_it_in()
        {
            var s = Session(); var log = s.Quests; var q = log.Def(QuestId);
            AtLevel(s, 3);
            Assert.IsTrue(log.Accept(q, s.ZoneId));
            var band = q.steps[0].objectives.First(o => o.target.StartsWith("mob.deserter."));
            var crown = q.steps[0].objectives.First(o => o.target.StartsWith("mob.banditking."));
            // Killed outright (combat has its own tests): every deserter in the hollow first, then Caddock.
            var deserters = s.Enemies.Where(e => e.actor.IsAlive && QuestLog.Matches(band.target, e.persistentId)).ToList();
            Assert.GreaterOrEqual(deserters.Count, band.count, "Enough deserters in the hollow to finish without waiting for respawns.");
            foreach (var e in deserters) e.actor.Health.ApplyDamage(100000);
            yield return null;
            Assert.AreEqual(band.count, log.Count(log.State(q.id), Array.IndexOf(q.steps[0].objectives, band)), "Six deserters counted, and no more.");
            Assert.AreEqual(0, log.State(q.id).step, "Caddock still stands.");
            s.Enemies.First(e => e.actor.IsAlive && QuestLog.Matches(crown.target, e.persistentId)).actor.Health.ApplyDamage(100000);
            yield return null;
            Assert.AreEqual(1, log.State(q.id).step, "The hollow is cleared: on to the plunder.");
            // Searched directly: the bodies lying round it would take E first.
            s.UseInteractable(s.Zone.Interactables.Find(i => i.name == Plunder));
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, log.Status(q, s.ZoneId));
            // Back to the giver.
            var giver = VillageLife.Active.Find(q.turnIn);
            int xp = s.Progress.experience, gold = s.Progress.gold, standing = log.Standing("oakhaven");
            int earned = q.rewards.reputation.Where(r => r.faction == "oakhaven").Sum(r => r.amount);
            Assert.IsTrue(s.QuestTalk(giver.Name, giver.transform.position));
            s.CompleteQuest(q);
            Assert.IsTrue(log.IsDone(q.id));
            Assert.AreEqual(xp + q.rewards.xp, s.Progress.experience);
            Assert.AreEqual(gold + q.rewards.gold, s.Progress.gold);
            Assert.AreEqual(standing + earned, log.Standing("oakhaven"));
        }

        [UnityTest] public IEnumerator A_mob_does_not_notice_you_through_solid_scenery()
        {
            var s = Session();
            // A camp mob in the open (deserters first) and a walkable spot 3.5 m off it with a clear line.
            EncounterEnemy mob = null; Vector3 spot = default;
            foreach (var e in s.Enemies.Where(e => e.Camp && e.actor.IsAlive && !e.Hidden && !e.Engaged).OrderBy(e => e.persistentId.StartsWith("mob.deserter.") ? 0 : 1))
            {
                for (int k = 0; k < 8; k++)
                {
                    var dir = Quaternion.Euler(0, k * 45, 0) * Vector3.forward; var at = e.transform.position;
                    var p = s.Zone.Ground(new Vector2(at.x + dir.x * 3.5f, at.z + dir.z * 3.5f), 1.1f);
                    if (!NavMesh.SamplePosition(p, out _, 1.6f, NavMesh.AllAreas)) continue;   // p stands 1.1 m over the ground
                    Vector3 eye = at + Vector3.up * .6f, chest = p + Vector3.up * .4f;   // as EncounterEnemy.Sees looks
                    if (!Open(eye, chest) || !Open(chest, eye)) continue;
                    mob = e; spot = p; break;
                }
                if (mob != null) break;
            }
            Assert.NotNull(mob, "A camp mob with open ground round it.");
            // Its pack steps away for the test: every camp is a pack, and packmates would come for you on their own or pull it in.
            var parked = s.Enemies.FindAll(o => o != null && o != mob && o.gameObject.activeSelf && Vector3.Distance(o.transform.position, mob.transform.position) < 14);
            foreach (var o in parked) o.gameObject.SetActive(false);
            try
            {
            // A wall of rock between you: it doesn't come for you, even at 3.5 m.
            var line = spot - mob.transform.position; line.y = 0;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Test rock";
            wall.transform.position = (mob.transform.position + spot) / 2 + Vector3.up * .5f;
            wall.transform.rotation = Quaternion.LookRotation(line.normalized); wall.transform.localScale = new Vector3(4, 5, .3f);
            s.Player.GetComponent<AdventurerMotor>().Teleport(spot);
            Physics.SyncTransforms();
            for (float t = 0; t < 1.5f && !mob.Engaged; t += Time.deltaTime) yield return null;   // by time: noticing builds up per second
            Assert.IsFalse(mob.Engaged, mob.actor.DisplayName + " noticed you through the rock.");
            // Take the rock away and it sees you at once.
            UnityEngine.Object.Destroy(wall);
            yield return null; Physics.SyncTransforms();
            for (float t = 0; t < 2 && !mob.Engaged; t += Time.deltaTime) yield return null;
            Assert.IsTrue(mob.Engaged, "With the rock gone, " + mob.actor.DisplayName + " comes for you.");
            }
            finally { foreach (var o in parked) if (o != null) o.gameObject.SetActive(true); }
        }
        /// <summary>Nothing but actors (the mob itself, anyone passing) between two points.</summary>
        static bool Open(Vector3 from, Vector3 to)
        {
            foreach (var hit in Physics.RaycastAll(from, to - from, Vector3.Distance(from, to), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponentInParent<Crulanda.Gameplay.Actor>() == null) return false;
            return true;
        }
    }
}
#endif
