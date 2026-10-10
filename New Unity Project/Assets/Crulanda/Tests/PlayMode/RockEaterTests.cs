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
    /// <summary>
    /// Nix's two-part fight (DUNGEON_DESIGN.md 3a; dungeon step D5): the Geode Floor's workshop has the Rock-Eater, an engine Nix rides,
    /// hidden inside it (unseen, out of reach, not drawn out by walking up to it); when the engine breaks he jumps out at the wreck and
    /// comes for whoever broke it; the Amber Sigil and the camp's deed come from him, not the machine; both come back with the camp,
    /// Nix inside again.
    /// </summary>
    public class RockEaterTests
    {
        string root; EncounterSession session;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-rockeater-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.adit"; PlayerPrefs.SetInt("test.adit", 1);   // level 11 on arrival (the /adit hook): a level-1 character dies to one swing of a level-11 elite
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        EncounterEnemy Find(string mobName) { var e = session.Enemies.Find(x => x != null && x.Camp && x.Elite && x.MobName == mobName); Assert.NotNull(e, mobName); return e; }
        static bool Visible(EncounterEnemy e) { return e.transform.Find("Body").GetComponentsInChildren<Renderer>(true).Any(r => r.enabled); }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        void Kill(EncounterEnemy e) { e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3); }

        [UnityTest] public IEnumerator Nix_rides_the_Rock_Eater_and_jumps_out_when_it_breaks()
        {
            var machine = Find(EncounterSession.RockEaterName); var nix = Find(EncounterSession.NixName);
            Assert.AreEqual(nix.CampIndex, machine.CampIndex, "the machine is his camp's");
            Assert.AreSame(machine, nix.Inside); Assert.IsTrue(nix.Hidden, "inside it"); Assert.IsFalse(Visible(nix), "and unseen");
            Assert.IsTrue(Visible(machine)); Assert.IsFalse(nix.CanAnswer, "no call brings him out");
            Assert.AreEqual(ActorLook.RockEater, machine.GetComponent<ActorVisual>().Look);
            Assert.IsNull(LootContext.From(machine.persistentId, session.Zone.Zone.camps, 11, true).mob, "the machine's corpse rolls no named piece of Nix's");
            // Walking up to the engine (well inside the 8 m a hidden mob pounces from) does not bring him out, and nothing reaches him inside it.
            Assert.AreEqual(11, session.Progress.Level);
            session.Player.GetComponent<AdventurerMotor>().Teleport(machine.transform.position + machine.transform.forward * 4f);
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(nix.Hidden, "still inside"); Assert.AreSame(machine, nix.Inside);
            int before = nix.actor.Health.Pool.Current; nix.Receive(50, session.Player); Assert.AreEqual(before, nix.actor.Health.Pool.Current, "beyond harm");
            // The engine breaks: no sigil from it; Nix is out at the wreck, fighting whoever broke it.
            machine.Receive(1, session.Player); Kill(machine); yield return Frames(2);
            Assert.IsFalse(machine.actor.IsAlive); Assert.IsFalse(session.Progress.keys.Contains("sigil.amber"), "the sigil is Nix's, not the machine's");
            Assert.IsFalse(session.Progress.elitesSlain.Any(k => k.Contains("rockeater")), "no deed for the machine");
            Assert.IsNull(nix.Inside); Assert.IsFalse(nix.Hidden, "out"); Assert.IsTrue(Visible(nix), "and seen");
            Assert.Less(Vector3.Distance(nix.transform.position, machine.transform.position), 4, "at the wreck");
            float t = 0; while (t < 3 && nix.Victim != session.Player) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(session.Player, nix.Victim, "he comes for you");
            Kill(nix); yield return Frames(2);
            Assert.Contains("sigil.amber", session.Progress.keys, "the Amber Sigil from Nix");
            Assert.IsTrue(session.Progress.elitesSlain.Any(k => k.EndsWith("." + nix.CampIndex) && k.Contains("nix")), "and the camp's deed");
        }
        [UnityTest] public IEnumerator Both_come_back_with_the_camp_and_Nix_is_inside_again()
        {
            var machine = Find(EncounterSession.RockEaterName); var nix = Find(EncounterSession.NixName);
            machine.RespawnSeconds = nix.RespawnSeconds = 1;
            Kill(machine); yield return Frames(2); Kill(nix); yield return Frames(2);
            Assert.IsFalse(machine.actor.IsAlive); Assert.IsFalse(nix.actor.IsAlive);
            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(machine.actor.IsAlive, "the engine is back"); Assert.IsTrue(nix.actor.IsAlive, "and so is Nix");
            Assert.AreSame(machine, nix.Inside, "inside it again"); Assert.IsTrue(nix.Hidden); Assert.IsFalse(Visible(nix));
            Assert.IsTrue(Visible(machine));
            Assert.IsFalse(machine.transform.Find("Body").GetComponent<MeshRenderer>().enabled, "the engine's placeholder capsule stays off");
            Assert.IsFalse(nix.transform.Find("Body").GetComponent<MeshRenderer>().enabled, "and Nix's");
        }
    }
}
#endif
