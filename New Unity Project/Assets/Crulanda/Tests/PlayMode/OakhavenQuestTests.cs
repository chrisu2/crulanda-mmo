#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>Plays the Oakhaven Chronicle in the real scene: every quest person exists, and chapter one runs end to end.</summary>
    public class OakhavenQuestTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root;
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 10;   // daytime: everyone is up and about
            root = Path.Combine(Path.GetTempPath(), "Crulanda-quests-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            WorldClock.Hour = 8.5f;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [UnityTest] public IEnumerator Every_quest_giver_lives_in_Oakhaven_and_every_quest_place_exists()
        {
            var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session.Quests, "Quest content loaded.");
            var life = VillageLife.Active;
            foreach (var q in session.Quests.Db.Ordered)
            {
                if (q.zone != "zone.oakhaven") continue;
                foreach (var who in new[] { q.giver, q.turnIn })
                    if (who != "auto" && who != "Mira") Assert.NotNull(life.Find(who), q.id + ": nobody called '" + who + "' in Oakhaven.");
                foreach (var step in q.steps)
                    foreach (var o in step.objectives)
                    {
                        if (o.type == "talk" || o.type == "deliver") { if (o.target != "Mira") Assert.NotNull(life.Find(o.target), q.id + ": no '" + o.target + "'."); }
                        if (o.type == "interact" || (o.type == "collect" && !o.target.StartsWith("kill:")))
                            Assert.IsTrue(session.Zone.Interactables.Exists(i => i.name == o.target), q.id + ": no interactable '" + o.target + "'.");
                    }
            }
            Assert.AreEqual(3, session.Zone.Interactables.FindAll(i => i.name == "Tithe crate").Count);
            Assert.GreaterOrEqual(session.Zone.Interactables.FindAll(i => i.name == "Yarrow").Count, 5);
            yield return null;
        }

        [UnityTest] public IEnumerator Quest_givers_stand_still_while_you_talk_and_the_selected_friend_is_who_E_talks_to()
        {
            var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var corwin = VillageLife.Active.Find("Corwin Ashby");
            // Catch him mid-walk, open a conversation, and he stays put facing us.
            var motor = session.Player.GetComponent<AdventurerMotor>();
            motor.Teleport(corwin.transform.position + Vector3.right * 2 + Vector3.up * .1f);
            session.Quests.Progress.questsDone.Add("main.oakhaven.1");   // so he has quests to offer
            Assert.IsTrue(session.QuestTalk(corwin.Name, corwin.transform.position)); Assert.NotNull(session.Conversation);
            yield return null;
            var at = corwin.transform.position;
            yield return new WaitForSeconds(2);
            // Walking away would cover 3+ m in 2 s; crowd avoidance may still nudge him a little in a busy inn.
            Assert.Less(Vector3.Distance(at, corwin.transform.position), 1f, "He waits while the conversation is open.");
            session.Conversation = null;

            // Mira is closest, so E defaults to her; selecting a villager makes E talk to them instead. On the open green (Corwin may be
            // anywhere in the inn by now, and a warp beside a table misses the navmesh), with both of them straight ahead.
            var sel = VillageLife.Active.Find("Sel Harrow");
            motor.Teleport(session.Zone.Ground(session.Zone.Zone.spawns.recovery) + Vector3.up * .1f); yield return null;
            var p = session.Player.transform.position; var fwd = session.Player.transform.forward; fwd.y = 0; fwd.Normalize();
            session.Companion.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(p + fwd * 1.2f - Vector3.up * .1f);
            sel.StandAt(p + fwd * 2.6f - Vector3.up * .1f, 180);
            // Anyone else who happens to be passing (the village's day moves people about) steps well clear first.
            foreach (var v in VillageLife.Active.Villagers)
                if (v != sel && v != corwin && Vector3.Distance(v.transform.position, p) < 6) v.StandAt(p + Vector3.back * 25 + Vector3.right * 3 * VillageLife.Active.Villagers.IndexOf(v), 0);
            yield return null;
            StringAssert.Contains("Mira", session.InteractPrompt);
            session.SelectFriendly(sel, false);
            Assert.AreEqual("Talk to Sel Harrow", session.InteractPrompt);
            session.SelectFriendly(null, true);
            StringAssert.Contains("Mira", session.InteractPrompt);
        }

        [UnityTest] public IEnumerator Chronicle_one_from_Mira_to_the_ledger_and_the_elder()
        {
            var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var log = session.Quests; var chronicle = log.Def("main.oakhaven.1");
            Assert.AreEqual(QuestStatus.Active, log.Status(chronicle, session.ZoneId), "The Chronicle starts by itself.");
            var motor = session.Player.GetComponent<AdventurerMotor>();

            // 1. Mira.
            motor.Teleport(session.Companion.transform.position + Vector3.right * 1.5f); yield return null;
            session.SelectFriendly(null, true);   // click Mira, then E (the inn is busy; E alone talks to whoever is nearest)
            session.Interact(); Assert.IsTrue(session.Progress.recruited);
            Assert.AreEqual(1, log.State(chronicle.id).step);
            // 2. The collectors (killed outright; combat has its own tests).
            foreach (var e in session.Enemies) e.actor.Health.ApplyDamage(100000);
            yield return null;
            Assert.AreEqual(2, log.State(chronicle.id).step);
            // 3. The blade.
            Assert.IsTrue(session.Progress.Loot(session.Enemies[0].persistentId)); Inventory.Add(session.Progress, session.Items, session.content.itemId, 1);
            session.Equip();
            Assert.AreEqual(3, log.State(chronicle.id).step);
            // 4. The wagon: the ledger, and the page opens to read.
            var wagon = session.Zone.Interactables.Find(i => i.name == "Bureau wagon");
            motor.Teleport(wagon.position + (session.Zone.Ground(new Vector2(wagon.position.x, wagon.position.z - 2.6f), 1.1f) - wagon.position));
            yield return null;
            Assert.AreEqual(wagon, session.NearbyInteractable, "Standing at the wagon.");
            session.Interact();
            Assert.AreEqual(1, log.ItemCount("item.bureau_ledger"));
            Assert.AreEqual("doc.oakhaven.ledger", session.ReadingDocument); Assert.IsTrue(session.QuestBookOpen);
            Assert.IsNull(session.NearbyInteractable, "The wagon has nothing more to give.");
            // 5. The elder: hand over the ledger and complete the chapter; chapter two is offered straight after.
            var corwin = VillageLife.Active.Find("Corwin Ashby");
            Assert.IsTrue(session.QuestTalk(corwin.Name, corwin.transform.position));
            Assert.NotNull(session.Conversation);
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, log.Status(chronicle, session.ZoneId));
            int xp = session.Progress.experience;
            session.CompleteQuest(chronicle);
            Assert.IsTrue(log.IsDone(chronicle.id)); Assert.Greater(session.Progress.experience, xp);
            Assert.NotNull(session.Conversation, "Corwin has more to ask.");
            Assert.IsTrue(session.Conversation.entries.Exists(x => x.quest.id == "main.oakhaven.2"));
            session.AcceptQuest(log.Def("main.oakhaven.2"));
            Assert.AreEqual(QuestStatus.Active, log.Status(log.Def("main.oakhaven.2"), session.ZoneId));
            // Saved with the character.
            Assert.IsTrue(File.ReadAllText(Directory.GetFiles(root, "encounter*", SearchOption.AllDirectories)[0]).Contains("main.oakhaven.2"));
        }
    }
}
#endif
