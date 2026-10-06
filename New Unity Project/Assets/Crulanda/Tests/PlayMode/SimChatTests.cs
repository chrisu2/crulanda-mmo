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
    /// <summary>Zone chat (playtest note 62): the sims talk, answer where a place is with its real direction, answer a group call, and the game's own messages land in System.</summary>
    public class SimChatTests
    {
        string root; EncounterSession session; float hourWas;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-chat-" + Guid.NewGuid().ToString("N"));
            hourWas = WorldClock.Hour; SimPopulation.LifeOverride = true;
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(SimChatter.Active, "the sims can talk");
            foreach (var s in SimPopulation.Active.World.sims.Take(6)) { s.zone = "zone.oakhaven"; s.onlineFrom = 0; s.onlineHours = 24; s.chatty = .9f; s.friendly = .9f; s.level = session.Progress.Level; }
            SimPopulation.Active.Refresh();
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            WorldClock.Hour = hourWas; SimPopulation.LifeOverride = null; EncounterInput.Typing = false;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        static bool SimLine(ChatLine l) { return l.channel != ChatChannel.System && l.speaker != "You"; }

        [UnityTest] public IEnumerator The_sims_talk_in_the_zone()
        {
            float t = 0; while (t < 40 && session.Chat.Count(SimLine) < 3) { t += Time.deltaTime; yield return null; }
            var said = session.Chat.Where(SimLine).ToList();
            Assert.GreaterOrEqual(said.Count, 3, "three lines in forty seconds from six chatty sims");
            var names = SimPopulation.Active.World.sims.Select(s => s.name).ToList();
            foreach (var l in said) { Assert.Contains(l.speaker, names, "a sim said it"); Assert.IsFalse(string.IsNullOrWhiteSpace(l.text)); }
        }

        [UnityTest] public IEnumerator Asked_where_a_place_is_a_sim_gives_its_direction()
        {
            var place = session.Zone.Zone.landmarks.First(l => l.name == "The Old Barrow" || l.at.magnitude > 60);
            int before = session.Chat.Count;
            session.PlayerChat("where is " + place.name + "?");
            Assert.AreEqual("You", session.Chat.Last().speaker);
            float t = 0; ChatLine answer = null;
            while (t < 12 && answer == null) { answer = session.Chat.Skip(before).FirstOrDefault(l => SimLine(l) && l.text.Contains(place.name)); t += Time.deltaTime; yield return null; }
            Assert.NotNull(answer, "someone answered about " + place.name);
            StringAssert.Contains("of the village", answer.text, "with a direction");
        }

        [UnityTest] public IEnumerator A_group_call_is_answered_and_messages_land_in_system()
        {
            int before = session.Chat.Count;
            session.PlayerChat("/lfg lfg anyone?");
            Assert.AreEqual(ChatChannel.LFG, session.Chat.Last().channel);
            float t = 0; while (t < 12 && !session.Chat.Skip(before).Any(l => SimLine(l) && (l.text.Contains("inv") || l.text.Contains("join")))) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(session.Chat.Skip(before).Any(l => SimLine(l) && (l.text.Contains("inv") || l.text.Contains("join"))), "a sim of your level offers to join");
            session.Message("A test message.");
            Assert.AreEqual(ChatChannel.System, session.Chat.Last().channel); Assert.AreEqual("A test message.", session.Chat.Last().text);
            session.PlayerChat("/p hello"); StringAssert.Contains("not in a party", session.Messages.Last());
        }
    }
}
#endif
