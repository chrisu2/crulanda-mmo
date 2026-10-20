#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>Inviting every way (Chris 12:59, "i cant invite mira ."; the tester's note 118, "/invite Thatch didn't work"): Mira as your
    /// target in the inn, /invite alone for your target, and a sim by its byname.</summary>
    public class InviteTests
    {
        string root; EncounterSession session;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-inv-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.NotNull(session);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        [UnityTest] public IEnumerator Mira_targeted_in_the_inn_joins_by_invite()
        {
            yield return new WaitForSeconds(.5f);
            Assert.NotNull(session.Companion, "Mira is in Oakhaven");
            session.Progress.recruited = false;
            session.Player.GetComponent<AdventurerMotor>().Teleport(session.Companion.transform.position + Vector3.back * 1.5f); yield return null;
            session.SelectFriendly(null, true); Assert.IsTrue(session.FocusMira, "Mira targeted");
            Assert.IsTrue(session.InviteTarget(), "invite your target"); Assert.IsTrue(session.Progress.recruited, "Mira joins");
            Assert.IsFalse(session.InviteByName(""), "/invite alone with Mira already in: a refusal, said aloud");
        }
        /// <summary>Chris 13:09, "the dps/healing meter is missing": shown from the start, on screen for a full party, and /meter toggles it.</summary>
        [Test] public void The_meter_is_shown_by_default_and_on_screen()
        {
            Assert.IsTrue(session.MeterOpen, "shown from the start");
            for (int n = 1; n <= 6; n++) { var r = EncounterHud.MeterRect(n); Assert.IsTrue(r.xMin >= 0 && r.yMin >= 0 && r.xMax <= 1440 && r.yMax <= 900, "on the canvas with " + n + " rows: " + r); }
            Assert.NotNull(session.Meter, "the meter counts");
        }
        [UnityTest] public IEnumerator Invite_by_a_byname_finds_the_sim()
        {
            yield return new WaitForSeconds(.5f);
            for (float w = 0; w < 6 && SimPopulation.Active == null; w += Time.deltaTime) yield return null;
            var pop = SimPopulation.Active; Assert.NotNull(pop, "the sims are about");
            var s = pop.World.sims.Find(x => x.zone == session.ZoneId) ?? pop.World.sims[0];
            s.name = "Wren Thatch"; s.zone = session.ZoneId; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .95f; s.level = session.Progress.Level;
            pop.Refresh(); yield return null; yield return null;
            Assert.IsTrue(EncounterSession.NameFits("Wren Thatch", "Thatch") && EncounterSession.NameFits("Wren Thatch", "wren") && !EncounterSession.NameFits("Wren Thatch", "Mira"));
            Assert.IsTrue(session.InviteByName("Thatch"), "/invite Thatch: " + session.InviteRefusal(s));
            Assert.IsTrue(session.InParty(s.id), "Thatch is in the party");
        }
    }
}
#endif