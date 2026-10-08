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
            float t = 0; while (t < 75 && session.Chat.Count(SimLine) < 3) { t += Time.deltaTime; yield return null; }   // a line every 8-23 s with six sims about (SimChatter): forty seconds was borderline (c43)
            var said = session.Chat.Where(SimLine).ToList();
            Assert.GreaterOrEqual(said.Count, 3, "three lines in seventy-five seconds from six chatty sims");
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

        /// <summary>5.5: a whisper is answered by whisper, /r answers back, /friend lists, a rival stays silent and refuses.</summary>
        [UnityTest] public IEnumerator A_whisper_is_answered_and_a_friend_is_listed()
        {
            var s = SimPopulation.Active.World.sims.First(x => x.zone == "zone.oakhaven" && x.IsOnlineAt(WorldClock.Hour));
            int before = session.Chat.Count;
            Assert.IsTrue(session.Whisper(s.name + " hello there"), "whispered by full name");
            Assert.AreEqual(ChatChannel.Whisper, session.Chat.Last().channel); Assert.AreEqual(s.name, session.Chat.Last().to);
            float t = 0; ChatLine answer = null;
            while (t < 12 && answer == null) { answer = session.Chat.Skip(before).FirstOrDefault(l => l.channel == ChatChannel.Whisper && l.speaker == s.name); t += Time.deltaTime; yield return null; }
            Assert.NotNull(answer, s.name + " answered by whisper"); Assert.IsNull(answer.to); Assert.AreEqual(s.name, session.LastWhisperer);
            Assert.GreaterOrEqual(s.regard, 1, "an answered whisper counts a little");
            before = session.Chat.Count; session.PlayerChat("/r ty"); Assert.AreEqual(s.name, session.Chat.Last(l => l.speaker == "You").to, "/r answers the last whisperer");
            t = 0; while (t < 12 && !session.Chat.Skip(before).Any(l => l.speaker == s.name && l.channel == ChatChannel.Whisper)) { t += Time.deltaTime; yield return null; }   // its whispered "np" lands before the rival part (its zone chatter does not count: c49)
            session.PlayerChat("/friend " + s.name); Assert.IsTrue(s.friend, "listed"); session.PlayerChat("/friends"); StringAssert.Contains(s.name, session.Messages.Last());
            s.regard = SimMemory.RivalAt; before = session.Chat.Count;
            session.Whisper(s.name + " hey"); yield return new WaitForSeconds(8);
            Assert.IsFalse(session.Chat.Skip(before).Any(l => l.channel == ChatChannel.Whisper && l.speaker == s.name), "a rival does not answer (its zone chatter goes on)");
            StringAssert.Contains("Not with you", session.InviteRefusal(s), "nor group with you");
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
            // The sticky channel (note 71): "/lfg" alone switches, plain words then go there, "/z" brings Zone back.
            session.PlayerChat("/lfg"); Assert.AreEqual(ChatChannel.LFG, session.ChatDefault);
            session.PlayerChat("lf1m wolves"); Assert.AreEqual(ChatChannel.LFG, session.Chat.Last(l => l.speaker == "You").channel);
            session.PlayerChat("/z"); Assert.AreEqual(ChatChannel.Zone, session.ChatDefault);
            // /invite across zones (note 67): a sim online elsewhere sets out and joins.
            EncounterSession.InviteTravelSeconds = 1;
            var far = SimPopulation.Active.World.sims.First(s => s.zone != "zone.oakhaven"); far.onlineFrom = 0; far.onlineHours = 24; far.friendly = .9f; far.level = session.Progress.Level;
            Assert.IsTrue(session.InviteByName(far.name), "invited by name from " + far.zone + ": " + string.Join(" | ", session.Messages));
            float tt = 0; while (tt < 8 && !session.InParty(far.id)) { tt += Time.deltaTime; yield return null; }
            Assert.IsTrue(session.InParty(far.id), far.name + " came from " + far.zone + " and joined");
            EncounterSession.InviteTravelSeconds = 30;
            session.PlayerChat("/dance"); StringAssert.Contains("Unknown command", session.Messages.Last());
        }
    }
}
#endif
