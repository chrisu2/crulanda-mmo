#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Weaver's unlocking (DUNGEON_DESIGN.md section 4, dungeon step D4): with The Pressed done, Danner lies in the dark and the goblin who
    /// opens the platform gate quietly is Mother Quillet, who waits by it as one of your party; spoken to, she walks to the carriage's lock with
    /// two waves coming for her and Danner standing up at half the lock; matched, the geodes go dark, "adit.lock" is kept and Pib's quest moves
    /// on; if she falls the waves go back into the dark and she is by the gate again a little later.
    /// </summary>
    public class AditWeaverTests
    {
        string root; EncounterSession session;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-aditweaver-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return Load();
            // The Pressed handed in before this visit: the escort can happen.
            session.Progress.questsDone.Add(EncounterSession.PressedQuest); session.Save(false);
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
            AditWeaver.MatchSeconds = 36; AditWeaver.WaitFor = 16; AditWeaver.BackAfter = 30;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        ZoneInteractable Thing(string kind) { var i = session.Zone.Interactables.Find(x => x.kind == kind); Assert.NotNull(i, "the Adit has its " + kind); return i; }
        EncounterEnemy Danner() { return session.Enemies.Find(e => e != null && e.Elite && e.actor.IsAlive && e.MobName == EncounterSession.DannerName); }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        bool CarriageDark { get { return session.Zone.RailGeodes.Select(g => g.GetComponent<Light>()).Where(l => l != null).All(l => !l.enabled); } }
        /// <summary>Everyone in the hall past the platform gate down, but Danner: the hall cleared before the walk.</summary>
        void ClearHall(ZoneGate gate)
        {
            foreach (var e in session.Enemies.ToArray())
            {
                if (e == null || !e.Camp || !e.actor.IsAlive || e.MobName == EncounterSession.DannerName || e.CampIndex < 0 || e.CampIndex >= session.Zone.Zone.camps.Length) continue;
                var c = session.Zone.Zone.camps[e.CampIndex];
                if (c.cave == gate.Cave && c.along > gate.Along) e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3);
            }
        }
        /// <summary>You a step behind her (so she never waits for you) every frame, until <paramref name="done"/> or the time is up.</summary>
        IEnumerator Walk(AditWeaver w, float seconds, Func<bool> done, Action each = null)
        {
            var motor = session.Player.GetComponent<AdventurerMotor>(); float until = Time.time + seconds;
            while (Time.time < until && !done())
            {
                var ahead = w.LockAt - w.transform.position; ahead.y = 0;
                var back = w.transform.position - (ahead.sqrMagnitude > .01f ? ahead.normalized : Vector3.zero) * 2;
                if (NavMesh.SamplePosition(back, out var hit, 2, NavMesh.AllAreas)) motor.Teleport(hit.position + Vector3.up * 1.05f);
                each?.Invoke();
                yield return null;
            }
        }

        [UnityTest] public IEnumerator A_quiet_gate_brings_Mother_Quillet_and_Danner_lies_in_the_dark()
        {
            var danner = Danner(); Assert.NotNull(danner, "Danner is in the hall");
            Assert.IsTrue(danner.Hidden, "he lies in the dark while the escort can happen");
            Assert.IsFalse(danner.CanAnswer, "and no call draws him out");
            Assert.IsNull(session.Weaver, "nobody at the gate yet");
            var gate = Thing("platformgate");
            session.UseInteractable(gate); yield return Frames(3);
            Assert.IsTrue(gate.gate.Open, "opened the quiet way");
            var w = session.Weaver; Assert.NotNull(w, "Mother Quillet stays by the gate");
            Assert.AreEqual(AditWeaver.Stage.Waiting, w.Now);
            Assert.Less(Vector3.Distance(w.transform.position, gate.gate.transform.position), 5, "by the gate");
            Assert.AreSame(w.actor, session.PartyActor(w.actor.EntityId.Value), "one of your party for every mob, Mira and the sims");
            Assert.AreEqual("Talk to " + AditWeaver.Name + " (walk her to the carriage)", session.InteractPromptFor(w.talk));
            var path = new NavMeshPath();
            Assert.IsTrue(NavMesh.CalculatePath(w.Gate, w.LockAt, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "a way down the platform to the lock");
            Assert.Less(Vector3.Distance(w.LockAt, session.Zone.RailLock), 2, "she stands at the lock, by the carriage");
            Assert.Greater(Vector3.Distance(w.LockAt, danner.transform.position), 9, "out of Danner's notice: he waits for the half-way mark");
            Assert.IsFalse(CarriageDark, "the carriage's geodes glow");
        }

        [UnityTest, Timeout(360000)] public IEnumerator She_walks_to_the_lock_through_two_waves_and_Danner_and_the_geodes_go_dark()
        {
            float realMatch = AditWeaver.MatchSeconds;   // the game's time at the lock (the test's is short)
            AditWeaver.MatchSeconds = 4;
            session.Progress.experience = EncounterProgress.XpForLevel(11);   // Pib's quests are for level 10 and up
            var quests = session.Quests; Assert.IsTrue(quests.Accept(quests.Def(EncounterSession.WeaverQuest), "zone.peaks"), "Pib's quest taken");
            var gate = Thing("platformgate"); ClearHall(gate.gate);
            session.UseInteractable(gate); yield return Frames(3);
            var w = session.Weaver; var danner = Danner(); Assert.NotNull(danner);
            // A sim in the party joins the escort (D7): it guards her while she walks.
            var pop = SimPopulation.Active; var s = pop.World.sims.First(); s.zone = "zone.adit"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = 11;
            var pp = session.Player.transform.position; s.x = pp.x + 2; s.z = pp.z; pop.Refresh(); yield return null;
            Assert.IsTrue(session.Invite(s.id, true)); var guard = session.PartySim(s.id); bool guarded = false;
            w.GetComponent<NavMeshAgent>().speed = 7;   // a quick walk for the test
            session.UseInteractable(w.talk); yield return null;
            Assert.AreEqual(AditWeaver.Stage.Walking, w.Now, "spoken to, she sets off");
            // Each of the waves' people is cut down as soon as it picks someone to fight; Danner too, once he is up.
            var cameFor = new Dictionary<EncounterEnemy, Actor>(); float leftAtFirst = -1, leftAtSecond = -1, matchedWhenRose = -1; Actor dannerFor = null;
            // On until the lock is matched AND both waves have come for her (the test's lock is short: the second wave, out of the far dark,
            // can still be on its way when she is done), with a time limit (2026-10-10: the drawn-out Adit; a review Chris pasted).
            float waveSpeed = 0;
            yield return Walk(w, 240, () => w.Now == AditWeaver.Stage.Done && cameFor.Count >= 2 * EncounterSession.WaveSize, () => {
                // How far from the lock she was when each wave came out (the list keeps every one spawned; only a fall clears it).
                var left = w.LockAt - w.transform.position; left.y = 0;
                if (leftAtFirst < 0 && session.WeaverWaves.Count > 0) leftAtFirst = left.magnitude;
                if (leftAtSecond < 0 && session.WeaverWaves.Count > EncounterSession.WaveSize) leftAtSecond = left.magnitude;
                foreach (var e in session.WeaverWaves) if (e != null && waveSpeed == 0) { var ag = e.GetComponent<NavMeshAgent>(); if (ag != null) waveSpeed = ag.speed; }
                foreach (var e in session.WeaverWaves)
                {
                    if (e == null || cameFor.ContainsKey(e)) continue;
                    if (e.Victim == null) continue;
                    cameFor[e] = e.Victim; e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3);
                }
                if (matchedWhenRose < 0 && !danner.Hidden) matchedWhenRose = w.Matched;
                if (guard != null && guard.Activity == "Guarding " + AditWeaver.Name) guarded = true;
                if (dannerFor == null && danner.actor.IsAlive && danner.Victim != null) { dannerFor = danner.Victim; danner.actor.Health.ApplyDamage(danner.actor.Health.Pool.Max * 3); }
            });
            Assert.AreEqual(AditWeaver.Stage.Done, w.Now, "the lock is matched");
            // The second wave must be able to reach her while she is still at the lock in play, or it never threatens it: its run from where
            // it comes out of the dark to the lock is well inside the game's time at the lock.
            var flat = new Vector3(1, 0, 1); float run = Vector3.Distance(Vector3.Scale(session.Zone.RailWaves[1], flat), Vector3.Scale(session.Zone.RailLock, flat));
            Assert.Greater(waveSpeed, 0, "the waves walk");
            Assert.Less(run / waveSpeed, realMatch * .5f, "the second wave reaches the lock (" + run.ToString("0.0") + " m at " + waveSpeed + " m/s) in the first half of her " + realMatch + " s there");
            string waves = string.Join("; ", session.WeaverWaves.Select(e => e == null ? "gone" : (e.actor.IsAlive ? "alive" : "dead") + " victim " + (e.Victim != null ? e.Victim.name : "none") + " " + Vector3.Distance(e.transform.position, w.transform.position).ToString("0.0") + " m from her, path " + (e.GetComponent<NavMeshAgent>() is NavMeshAgent ag && ag.isOnNavMesh ? ag.pathStatus.ToString() : "off the mesh")));
            Assert.AreEqual(2 * EncounterSession.WaveSize, cameFor.Count, "two waves of three [" + waves + "] (spawned " + session.WeaverWaves.Count + ", metres from the lock at the first " + leftAtFirst + ", at the second " + leftAtSecond + ", the waves' places " + string.Join(" ", session.Zone.RailWaves.Select(p => p.ToString())) + ", the lock " + session.Zone.RailLock + ")");
            Assert.IsTrue(cameFor.Values.All(a => a == w.actor), "every one of them came for her");

            Assert.That(leftAtFirst, Is.InRange(AditWeaver.Wave1At - 3, AditWeaver.Wave1At + .5f), "the first wave as she nears the carriage");
            Assert.That(leftAtSecond, Is.InRange(AditWeaver.Wave2At - 3, AditWeaver.Wave2At + .5f), "the second when she is close");
            Assert.That(matchedWhenRose, Is.InRange(.5f, .7f), "Danner stood up with the lock half matched");
            Assert.AreSame(w.actor, dannerFor, "and came for her");
            Assert.IsTrue(guarded, "the sim guarded her on the way (D7)");
            Assert.Contains(EncounterSession.WeaverLock, session.Progress.keys, "kept for good");
            Assert.IsTrue(CarriageDark, "the geodes have gone dark");
            Assert.AreEqual(1, quests.State(EncounterSession.WeaverQuest).step, "Pib's quest: back to Pib");
            // A new visit: the carriage is still dark, Danner is on his feet, and the goblin at the gate has nothing left to do here.
            // Save as a player would, once the fight is over (a save is refused in combat: the test now runs on through the second wave).
            for (float until = Time.time + 20; session.InCombat && Time.time < until;) yield return null;
            Assert.IsFalse(session.InCombat, "the fight is over before the save (still fighting: " + string.Join(", ", session.Enemies.Where(e => e != null && e.FightingParty).Select(e => e.Name + (e.actor.IsAlive ? "" : " (dead)") + " " + Vector3.Distance(e.transform.position, session.Player.transform.position).ToString("0") + " m")) + "; target " + session.FightingTarget + ")");
            Assert.IsTrue(session.Player.IsAlive, "you are alive to save (health " + session.Player.Health.Pool.Current + ")");
            Assert.IsTrue(session.Progress.keys.Contains(EncounterSession.WeaverLock), "the lock is in the progress being saved");
            session.Save(false);
            var onDisk = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Where(f => File.ReadAllText(f).Contains(EncounterSession.WeaverLock)).Select(Path.GetFileName).ToList();
            Assert.IsNotEmpty(onDisk, "the save on disk holds the lock (files: " + string.Join(", ", Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(Path.GetFileName)) + ")");
            yield return Load(); yield return Frames(10);
            string after = "reloaded from " + session.SaveRoot + ": zone " + session.Progress.zoneId + ", xp " + session.Progress.experience + ", keys " + session.Progress.keys.Count + " [" + string.Join(", ", session.Progress.keys) + "]; on disk: " + string.Join(", ", Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(f => Path.GetFileName(f) + " " + new FileInfo(f).Length + " B " + File.GetLastWriteTime(f).ToString("HH:mm:ss.fff")));
            var geodeLights = session.Zone.RailGeodes.Select(g => g.GetComponent<Light>()).Where(l => l != null).ToList();
            Assert.IsTrue(CarriageDark, "still dark (" + after + "; the lock kept: " + session.Progress.keys.Contains(EncounterSession.WeaverLock) + "; geode lights " + geodeLights.Count + ", on: " + string.Join(", ", geodeLights.Where(l => l.enabled).Select(l => l.name + " " + l.intensity.ToString("0.00"))) + ")");
            Assert.IsFalse(Danner().Hidden, "no more lying in wait");
            session.UseInteractable(Thing("platformgate")); yield return Frames(3);
            Assert.IsTrue(Thing("platformgate").gate.Open);
            Assert.IsNull(session.Weaver, "her work is done");
        }

        [UnityTest] public IEnumerator If_she_falls_the_waves_go_and_she_is_back_by_the_gate()
        {
            AditWeaver.BackAfter = 1;
            var gate = Thing("platformgate"); ClearHall(gate.gate);
            session.UseInteractable(gate); yield return Frames(3);
            var w = session.Weaver; w.GetComponent<NavMeshAgent>().speed = 7;
            session.UseInteractable(w.talk);
            yield return Walk(w, 60, () => session.WeaverWaves.Count > 0);   // (the first wave comes 27 m from the lock, some 20 s down the drawn-out hall)
            var wave = session.WeaverWaves.ToList();
            Assert.AreEqual(EncounterSession.WaveSize, wave.Count(e => e != null && e.actor.IsAlive), "the first wave is out");
            w.actor.Health.ApplyDamage(w.actor.Health.Pool.Max * 3); yield return Frames(2);
            Assert.AreEqual(AditWeaver.Stage.Fallen, w.Now);
            Assert.IsTrue(wave.All(e => e == null), "the waves went back into the dark");
            Assert.IsFalse(session.Enemies.Any(e => wave.Contains(e)), "and are no longer in the hall");
            Assert.AreNotSame(w.talk, session.NearbyInteractable, "nothing to say to her while she is down");
            yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(AditWeaver.Stage.Waiting, w.Now, "back, to try again");
            Assert.IsTrue(w.actor.IsAlive); Assert.AreEqual(w.actor.Health.Pool.Max, w.actor.Health.Pool.Current, "whole");
            Assert.Less(Vector3.Distance(w.transform.position, w.Gate), 1.5f, "by the gate");
            Assert.AreEqual(0f, w.Walked);
            session.UseInteractable(w.talk); yield return null;
            Assert.AreEqual(AditWeaver.Stage.Walking, w.Now, "and off again when spoken to");
        }
    }
}
#endif
