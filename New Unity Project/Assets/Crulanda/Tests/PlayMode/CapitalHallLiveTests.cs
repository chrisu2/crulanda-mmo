#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Combat;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The capital's hall people in a live session (CAPITAL_HUB.md step 4; EncounterSession.Halls), stood beside you in Oakhaven until
    /// the Crown is built: Aethelgard's audience opens on his greeting for your level, and he has no body any blow can land on (no
    /// Actor, no Combatant, no collider) and is never made a target; a trade's trainer counts as its own person, so the craft is taken
    /// up and put aside beside them; a guild's master opens its window and lets one in no guild join.
    /// </summary>
    public class CapitalHallLiveTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root;
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 10;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-halls-" + Guid.NewGuid().ToString("N"));
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
        static Villager Beside(EncounterSession s, string name, string role)
        {
            var life = VillageLife.Active; Assert.NotNull(life, "Oakhaven has its village.");
            var at = s.Player.transform.position + s.Player.transform.right * 1.2f + Vector3.down;
            var v = Villager.Spawn(life, name, role, 90 + life.Villagers.Count, null, at);
            life.Villagers.Add(v);
            return v;
        }

        [UnityTest] public IEnumerator Aethelgard_holds_an_audience_and_no_blow_can_touch_him()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.NotNull(s.Player);
            var a = Beside(s, CouncilAudience.Name, HallRoles.Councilor);
            yield return null;
            Assert.IsNull(a.GetComponent<Crulanda.Gameplay.Actor>(), "No Actor: nothing to hurt.");
            Assert.IsNull(a.GetComponent<Combatant>(), "No Combatant: no blow resolves on him.");
            Assert.IsNull(a.GetComponentInChildren<Collider>(), "No collider: nothing to click as a foe or hit with a bolt.");
            s.SelectFriendly(a, false); Assert.IsNull(s.Target, "Selecting him never makes him a target.");
            Assert.IsTrue(HallRoles.Untouchable(a.Role));
            Assert.IsTrue(s.HallTalk(a), "He holds an audience.");
            Assert.NotNull(s.Hall); Assert.AreEqual(HallRoles.Councilor, s.Hall.kind);
            Assert.AreEqual(CouncilAudience.Say(CouncilAudience.Greeting, s.AudienceContext()).text, s.Hall.line.text);
            Assert.AreEqual(s.Progress.Level >= 11 ? "high" : s.Progress.Level >= 6 ? "mid" : "low", s.Hall.line.rule, "His greeting is for your level.");
            s.AudienceAsk(CouncilAudience.Good); Assert.AreEqual(CouncilAudience.Good, s.Hall.topic);
            s.AudienceAsk(CouncilAudience.Farewell); Assert.IsNull(s.Hall, "Taking your leave shuts the audience.");
        }

        [UnityTest] public IEnumerator A_trades_trainer_in_the_castle_teaches_the_craft()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.NotNull(s.Professions, "The trades are loaded.");
            const string trade = "blacksmithing";
            Assert.IsNull(s.HallTradeTrainerNear(trade), "No castle trainer in Oakhaven yet.");
            var t = Beside(s, "Test Smith", HallRoles.ProfessionTrainer + ":" + trade);
            yield return null;
            Assert.AreSame(t, s.HallTradeTrainerNear(trade), "The trainer beside you counts.");
            Assert.IsTrue(s.CanTakeUp(trade, out var why), "Taken up beside the trainer: " + why);
            Assert.IsTrue(s.HallTalk(t)); Assert.AreEqual(HallRoles.ProfessionTrainer, s.Hall.kind);
            Assert.IsTrue(s.LearnCraft(trade)); Assert.IsTrue(s.Professions.Has(trade));
            Assert.IsTrue(s.ForgetCraft(trade)); Assert.IsFalse(s.Professions.Has(trade), "And put aside there.");
        }

        [UnityTest] public IEnumerator A_guild_master_opens_the_guild_window_and_takes_you_in()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            if (s.InGuild) s.LeaveGuild();
            var m = Beside(s, "Test Master", HallRoles.Guildmaster + ":lantern-watch");
            yield return null;
            Assert.IsTrue(s.HallTalk(m)); Assert.AreEqual(HallRoles.Guildmaster, s.Hall.kind);
            Assert.AreEqual("The Lantern Watch", GuildHalls.FromSlug(s.Hall.arg));
            Assert.IsTrue(s.JoinHallGuild("The Lantern Watch")); Assert.AreEqual("The Lantern Watch", s.Guild);
            Assert.IsFalse(s.JoinHallGuild("Scree Hounds"), "One guild at a time.");
            s.Escape(); Assert.IsNull(s.Hall, "Esc shuts the hall window.");
        }
    }
}
#endif
