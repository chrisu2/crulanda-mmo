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
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Sealed Adit's camps released later (the Adit rework, Docs/ADIT_REWORK.md section 4, E1, E2 and E4; EncounterSession.Release.cs;
    /// written 2026-10-15, not yet run in Unity): a camp whose data says what brings it (ZoneCamp.after) is not there at the start (AditTests
    /// says so) and comes when that happens, on the navmesh where its data stands it, with its camp's ids, sent at the party. Lusk's death brings
    /// his relief shift a few seconds on; the Wake's waves come in order at their seconds once the event starts, and Brother Hollowsong looses his
    /// choir at half; taking the keg brings Powder-Sergeant Hobb and his sappers.
    /// </summary>
    public class AditReleaseTests
    {
        string root; EncounterSession session;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-aditrelease-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.adit";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            EncounterSession.ReleaseDelay = 3; EncounterSession.EventSpeed = 1; EncounterSession.EventReach = 5;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        ZoneInteractable Thing(string kind) { var i = session.Zone.Interactables.Find(x => x.kind == kind); Assert.NotNull(i, "the Adit has its " + kind); return i; }
        EncounterEnemy Boss(string name) { var e = session.Enemies.Find(x => x != null && x.Camp && x.Elite && x.actor.DisplayName.StartsWith(name)); Assert.NotNull(e, name); return e; }
        int CampIndex(string name) { int c = Array.FindIndex(session.Zone.Zone.camps, x => x != null && x.name == name); Assert.GreaterOrEqual(c, 0, "the Adit has the camp " + name); return c; }
        ZoneCamp Camp(string name) { return session.Zone.Zone.camps[CampIndex(name)]; }
        /// <summary>The live session mobs of a camp (by index).</summary>
        List<EncounterEnemy> Mine(int camp) { return session.Enemies.Where(e => e != null && e.Camp && e.CampIndex == camp).ToList(); }
        bool Walkable(Vector2 to)
        {
            var z = session.Zone; var path = new NavMeshPath();
            return NavMesh.SamplePosition(z.Ground(z.Zone.spawns.player, .2f), out var a, 2.5f, NavMesh.AllAreas) && NavMesh.SamplePosition(z.StandAt(to, float.NegativeInfinity), out var b, 2.5f, NavMesh.AllAreas)
                && NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }
        /// <summary>Every mob of a released camp stands on the navmesh, within its camp's circle, with its camp's id (mob.&lt;tag&gt;.adit.&lt;camp&gt;.&lt;n&gt;), and never comes back.</summary>
        void Stood(string campName, int count)
        {
            int c = CampIndex(campName); var camp = session.Zone.Zone.camps[c]; var mine = Mine(c);
            Assert.AreEqual(count, mine.Count, campName + ": " + count + " of them");
            string tag = string.IsNullOrEmpty(camp.tag) ? (camp.look ?? "mob") : camp.tag;
            foreach (var e in mine)
            {
                Assert.IsTrue(e.actor.IsAlive, campName + " stands");
                Assert.IsTrue(NavMesh.SamplePosition(e.transform.position, out var hit, 1.5f, NavMesh.AllAreas) && Vector3.Distance(hit.position, e.transform.position) < 1.5f, campName + " is on the navmesh at " + e.transform.position);
                Assert.Less(Vector2.Distance(new Vector2(e.transform.position.x, e.transform.position.z), camp.center), camp.radius * 2 + 4, campName + " stands in its camp");   // the spawn's spread is a box twice the radius, then the navmesh's nearest point (8.2 m for a radius of 3: the first run, 01:20)
                StringAssert.StartsWith("mob." + tag + ".adit." + c + ".", e.persistentId, campName + ": its camp's id");
                Assert.GreaterOrEqual(e.RespawnSeconds, 1e5f, campName + " comes once a visit");
            }
            Assert.IsTrue(session.Released(c), campName + " is marked released");
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>kill:&lt;mob&gt;: Lusk's relief shift (3 overseers, after kill:Gang-Boss Haddo Lusk) is not there at the start, appears in the bowl a
        /// few seconds after his death with his deathText, can be walked to, answers him (sent at you), and comes only once.</summary>
        [UnityTest] public IEnumerator Lusks_death_releases_his_relief_shift_a_few_seconds_on()
        {
            int relief = CampIndex("Lusk's relief shift"); var camp = Camp("Lusk's relief shift");
            Assert.AreEqual("kill:Gang-Boss Haddo Lusk", camp.after); Assert.AreEqual(3, camp.count);
            Assert.IsEmpty(Mine(relief), "not there at the start");
            Assert.IsTrue(Walkable(camp.center), "the bowl, where they appear, is walkable from the yard");
            EncounterSession.ReleaseDelay = .3f;
            // You stand by the tally stage, where he fell: the shift walks in behind you.
            var lusk = Boss("Gang-Boss Haddo Lusk");
            session.Player.GetComponent<AdventurerMotor>().Teleport(lusk.transform.position + Vector3.right * 2.5f + Vector3.up); yield return null;
            lusk.actor.Health.ApplyDamage(lusk.actor.Health.Pool.Max * 3); yield return null;
            Assert.IsFalse(lusk.actor.IsAlive); Assert.IsEmpty(Mine(relief), "not in the same breath: a few seconds on");
            Assert.IsTrue(session.Released(relief), "but spoken for the moment he fell");
            yield return new WaitForSeconds(.8f);
            Stood("Lusk's relief shift", 3);
            Assert.IsTrue(session.Chat.Any(l => l.channel == ChatChannel.System && l.text.Contains("relief shift")), "his deathText is the line");
            // Sent at you: after the call's beat they have you and are on their way, leashed to his fight, not to the bowl.
            yield return new WaitForSeconds(SocialAggro.CallBeat + SocialAggro.CallStagger * 3 + .5f);
            foreach (var e in Mine(relief)) Assert.IsTrue(e.Victim == session.Player || e.Answering, e.persistentId + " comes for you");
            Assert.IsTrue(Walkable(camp.center), "the bowl is still walkable (they stood on its floor)");
            Assert.AreEqual(0, session.ReleaseAfter("kill:Gang-Boss Haddo Lusk", session.Player, null), "once a visit");
            yield return null; Assert.AreEqual(3, Mine(relief).Count, "no second shift");
        }

        /// <summary>event:&lt;id&gt;&lt;n&gt;: the Wake (ZoneEvent "wake") started, its four waves come in the data's order at their seconds (porters, zealots,
        /// crawlers, Brother Hollowsong), each where its camp stands; loose:&lt;elite&gt;: Hollowsong's choir waits for his call at half and comes then;
        /// his death (and the choir's) ends the Wake.</summary>
        [UnityTest] public IEnumerator The_Wakes_waves_come_in_order_and_Hollowsong_looses_his_choir_at_half()
        {
            var ev = session.Zone.Zone.events.FirstOrDefault(e => e != null && e.id == EncounterSession.WakeEvent); Assert.NotNull(ev, "the Adit has its Wake");
            Assert.AreEqual(4, ev.waves.Length);
            foreach (var w in ev.waves) Assert.IsEmpty(Mine(CampIndex(w.camp)), w.camp + " is not there at the start");
            int choir = CampIndex("Hollowsong's choir"); Assert.IsEmpty(Mine(choir));
            Assert.IsFalse(session.EventDone(EncounterSession.WakeEvent)); Assert.IsNull(session.EventRunning);
            // The party holds the island (the event's anchor): a wave leashes to it, so it walks the whole way.
            Assert.IsTrue(session.Zone.Anchors.TryGetValue(ev.anchor, out var island), "the island anchor stands");
            session.Player.GetComponent<AdventurerMotor>().Teleport(island + Vector3.up); yield return null;
            EncounterSession.EventSpeed = 30;   // 150 s of singing in 5
            Assert.IsTrue(session.StartEvent(EncounterSession.WakeEvent)); Assert.AreEqual(EncounterSession.WakeEvent, session.EventRunning);
            Assert.IsFalse(session.StartEvent(EncounterSession.WakeEvent), "one at a time");
            var order = new List<string>(); var at = new List<float>(); float t = 0;
            while (t < 9 && order.Count < ev.waves.Length)
            {
                foreach (var w in ev.waves) if (!order.Contains(w.camp) && Mine(CampIndex(w.camp)).Count > 0) { order.Add(w.camp); at.Add(session.EventClock); }
                t += Time.deltaTime; yield return null;
            }
            CollectionAssert.AreEqual(ev.waves.Select(w => w.camp).ToList(), order, "the waves in the data's order");
            for (int k = 0; k < ev.waves.Length; k++) Assert.GreaterOrEqual(at[k] + .5f, ev.waves[k].at, order[k] + " not before its second");
            Assert.AreEqual(ev.waves.Length, session.EventWave);
            foreach (var w in ev.waves) Stood(w.camp, Camp(w.camp).count);
            var hollowsong = Mine(CampIndex("Brother Hollowsong")).First(); Assert.IsTrue(hollowsong.Elite, "the Wake's boss"); Assert.IsNotNull(hollowsong.Move);
            Assert.AreEqual(.5f, hollowsong.Move.callAt, 1e-5f, "his call at half (ADIT_REWORK section 9)");
            Assert.AreEqual("loose:Brother Hollowsong", Camp("Hollowsong's choir").after); Assert.IsEmpty(Mine(choir), "the choir waits for his call");
            // Sent at you: he comes down the Gallery for the island.
            yield return new WaitForSeconds(SocialAggro.CallBeat + .6f);
            Assert.AreEqual(session.Player, hollowsong.Victim, "Hollowsong comes for you");
            hollowsong.actor.Health.ApplyDamage(Mathf.CeilToInt(hollowsong.actor.Health.Pool.Max * .55f)); yield return Frames(3);
            Assert.Less(hollowsong.actor.Health.Pool.Ratio, .5f);
            Stood("Hollowsong's choir", 3);
            Assert.IsFalse(session.EventDone(EncounterSession.WakeEvent), "not over while he and the choir stand");
            // His death and the choir's end the Wake; the waves' fallen stay where they fell.
            foreach (var w in ev.waves) foreach (var e in Mine(CampIndex(w.camp))) e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3);
            foreach (var e in Mine(choir)) e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3);
            yield return Frames(3);
            Assert.IsTrue(session.EventDone(EncounterSession.WakeEvent), "the Wake is over"); Assert.IsNull(session.EventRunning);
            Assert.IsFalse(session.StartEvent(EncounterSession.WakeEvent), "and does not start again this visit");
            Assert.IsTrue(session.Chat.Any(l => l.channel == ChatChannel.System && l.text.Contains("shaft stair")), "the end is said");
        }

        /// <summary>event:powder (E4, no ZoneEvent: the code's own): taking the keg brings Powder-Sergeant Hobb and his sappers a few seconds on, his
        /// sappers his guards; the camps named "loose" by a boss's phase (Sabel's urns) are the data's.</summary>
        [UnityTest] public IEnumerator Taking_the_keg_releases_Hobb_and_his_sappers_behind_you()
        {
            int hobb = CampIndex("Powder-Sergeant Hobb"), sappers = CampIndex("Hobb's sappers");
            Assert.AreEqual("event:powder", Camp("Powder-Sergeant Hobb").after); Assert.AreEqual("event:powder", Camp("Hobb's sappers").after);
            Assert.IsEmpty(Mine(hobb)); Assert.IsEmpty(Mine(sappers));
            EncounterSession.ReleaseDelay = .3f;
            session.UseInteractable(Thing("powder")); Assert.IsTrue(session.CarryingPowder);
            Assert.IsTrue(session.Released(hobb) && session.Released(sappers), "spoken for as the keg is taken");
            yield return new WaitForSeconds(.8f);
            Stood("Powder-Sergeant Hobb", 1); Stood("Hobb's sappers", 2);
            var lord = Mine(hobb)[0]; Assert.IsTrue(lord.Elite && lord.Move != null && lord.Move.mob == "Powder-Sergeant Hobb", "Hobb armed with his move");
            foreach (var s in Mine(sappers)) Assert.IsTrue(session.GuardOf(s, lord), "his sappers guard him");
            Assert.IsTrue(session.Chat.Any(l => l.channel == ChatChannel.System && l.text.Contains("Hobb")), "the line");
            session.UseInteractable(Thing("powder")); yield return new WaitForSeconds(.5f);
            Assert.AreEqual(1, Mine(hobb).Count, "a second E at the keg brings nobody");
            // The "loose" phases name camps of the zone (Sabel's urn-risen wait on "loose:Urn-Keeper Sabel"; Corvane's hound stands from the start).
            foreach (var m in EliteMoves.All)
                if (m.phases != null) foreach (var p in m.phases) if (p.kind == "loose") Assert.GreaterOrEqual(Array.FindIndex(session.Zone.Zone.camps, c => c != null && c.name == p.camp), 0, m.mob + " looses " + p.camp);
            var sabel = EliteMoves.For("Urn-Keeper Sabel", false);
            CollectionAssert.AreEqual(new[] { "Urn-risen (first)", "Urn-risen (second)" }, sabel.phases.Where(p => p.kind == "loose").Select(p => p.camp).ToList());
            foreach (var p in sabel.phases) Assert.AreEqual("loose:Urn-Keeper Sabel", Camp(p.camp).after);
        }
    }
}
#endif
