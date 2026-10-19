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
    /// The Unraveller fought by a raid of ten sims' worth (Docs/WORLD_BOSS_1.md 3.3-3.6; build steps 4 and 5), in the Lowtowns at the Drain
    /// Reach, on a fast clock (four times speed; the fight's waits shortened by the test's numbers, her health made deep so she lives through
    /// it, her phases reached by taking her health down). You stand off on the apron; nine sims at 20 fight her: 2 tanks, 3 healers, 4 damage
    /// (you the fifth), each with salt and harpoons.
    /// - Phase 1 and the all-fight rules: the harpoon rotation keeps her pinned, each pin shorter than the last; the Feeding is stopped (a kick
    ///   or a harpoon); steel goes black more than one stack deep on a melee sim; Unravel stacks on the tank and the tanks swap; her frame's pin
    ///   timer and enrage clock run; the raid frames show the marks.
    /// - Phases 2 and 3: the brood rise out of the river and are salted; the unmade ground is laid and sims step out of it; someone is swallowed
    ///   and three salts free them; the Unravelling pulses; she hunts and the hunted runs for the river; the feast at the (shortened) enrage
    ///   wipes the bar and counts a wipe.
    /// Run by the lead in Unity (WRITTEN, NOT RUN at the time of writing).
    /// </summary>
    public class UnravellerFightTests
    {
        string root; float hourWas; UnravellerTuning tuningWas;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load()
        {
            hourWas = WorldClock.Hour; WorldClock.Hour = 11; ZoneBuilder.RequestedZoneId = "zone.lowtowns";
            root = Path.Combine(Path.GetTempPath(), "Crulanda-unraveller-fight-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 30; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; if (tuningWas != null) UnravellerTuning.Current = tuningWas;
            VillageEconomy.ResetAll(); WorldClock.Hour = hourWas; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static IEnumerator Until(Func<bool> done, float seconds) { for (float t = 0; t < seconds && !done(); t += Time.deltaTime) yield return null; }

        /// <summary>The fight's numbers for a test: her health deep (she must live through it), the waits shorter, the rest as the file's.</summary>
        UnravellerTuning Quick(float enrage)
        {
            tuningWas = UnravellerTuning.Current;
            var t = UnravellerTuning.Parse(JsonUtility.ToJson(tuningWas));
            t.health = 5000000; t.enrageSeconds = enrage;
            t.broodEvery2 = t.broodEvery3 = 20; t.unmadeEvery = 8; t.swallowEvery2 = t.swallowEvery3 = 18; t.huntEvery = 12;
            t.simMissAt18 = t.simMissAt20 = t.simMissCareless = 0;   // no mistakes on purpose: each mechanic answered when it can be
            UnravellerTuning.Current = t; return t;
        }
        /// <summary>The Lowtowns, you at 20 on the apron, nine sims at 20 in a raid round you, her risen at the bar.</summary>
        IEnumerator Raid(float enrage)
        {
            yield return Load();
            Quick(enrage);   // after the session has read the data file (EncounterSession.Start), before she rises
            var s = Session(); Assert.NotNull(s); Assert.AreEqual("zone.lowtowns", ZoneBuilder.Active.Zone.id);
            s.Progress.experience = EncounterProgress.XpForLevel(20); s.Player.SetLevel(20);
            var zone = ZoneBuilder.Active; var apron = zone.ReachApron; Assert.NotNull(apron, "the apron");
            var stand = zone.Ground(apron.center + new Vector2(-2, -12));   // off at the apron's south end: out of her reach and her pulse's
            Assert.IsTrue(NavMesh.SamplePosition(stand, out var hit, 4, NavMesh.AllAreas)); stand = hit.position;
            s.Player.GetComponent<AdventurerMotor>().Teleport(stand + Vector3.up * 1.05f);
            var pop = SimPopulation.Active; Assert.NotNull(pop);
            string[] classes = { "class.warrior", "class.warrior", "class.cleric", "class.druid", "class.shaman", "class.ranger", "class.mage", "class.rogue", "class.scoundrel" };
            var near = zone.Ground(zone.SiltBar.center);
            var used = new HashSet<SimAdventurer>();
            for (int i = 0; i < classes.Length; i++)
            {
                var sim = pop.World.sims.First(x => !used.Contains(x) && x.classId == classes[i]); used.Add(sim);
                var at = near + Quaternion.Euler(0, i * 40, 0) * Vector3.forward * 6; if (NavMesh.SamplePosition(at, out var h, 5, NavMesh.AllAreas)) at = h.position;
                sim.zone = "zone.lowtowns"; sim.onlineFrom = 0; sim.onlineHours = 24; sim.level = 20; sim.friendly = .9f; sim.bold = .6f; sim.regard = 0; sim.x = at.x; sim.z = at.z; sim.gearBonus = 2; sim.coin = 500;
                RaidCall.BuyKit(sim, id => 1);
                var old = pop.Find(sim.id); if (old != null) { pop.Figures.Remove(old); UnityEngine.Object.Destroy(old.gameObject); }
            }
            pop.Refresh(); yield return null;
            Assert.IsTrue(s.FormRaid());
            foreach (var sim in used) Assert.IsTrue(s.Invite(sim.id, true), "invite " + sim.name);
            s.Raid.Arrange();
            yield return null;
            foreach (var c in s.PartySims) { var ag = c.GetComponent<NavMeshAgent>(); var at = near + UnityEngine.Random.insideUnitSphere * 6; if (NavMesh.SamplePosition(at, out var h, 6, NavMesh.AllAreas)) ag.Warp(h.position); }
            Assert.AreEqual(9, s.PartySims.Count, "nine sims with you");
            Assert.AreEqual(2, s.PartySims.Count(c => c.Role == SimCompanion.PartyRole.Tank), "two tanks");
            s.UnravellerSoon(1);
            yield return Until(() => s.UnravellerFoe != null && !s.UnravellerRising, 15);
            Assert.NotNull(s.UnravellerFoe, "she rises"); Assert.IsFalse(s.UnravellerRising);
        }
        /// <summary>The tanks pull her.</summary>
        static void Pull(EncounterSession s, EncounterEnemy e)
        {
            var tanks = s.PartySims.Where(c => c.Role == SimCompanion.PartyRole.Tank).ToList();
            e.threat.Add(tanks[0].actor.EntityId.Value, 200); e.threat.Add(tanks[1].actor.EntityId.Value, 100);
            s.Select(e);
        }
        static void TakeTo(EncounterEnemy e, float ratio)
        {
            int down = e.actor.Health.Pool.Current - Mathf.RoundToInt(e.actor.Health.Pool.Max * ratio);
            if (down > 0) e.actor.Health.ApplyDamage(down);
        }

        [UnityTest, Timeout(600000)] public IEnumerator Phase_one_pins_her_stops_the_Feeding_and_the_tanks_swap()
        {
            yield return Raid(900); var t = UnravellerTuning.Current;
            var s = Session(); var e = s.UnravellerFoe;
            Assert.AreEqual(Mathf.RoundToInt(t.health * EncounterEnemy.OvermatchTaken(30, 20)), e.actor.Health.Pool.Max, "her health from the data file");
            Assert.IsNotNull(e.Cast); Assert.AreEqual(EncounterEnemy.FeedKind, e.Cast.kind, "the Feeding is her cast");
            // Her body (play note 256): the spider, the purple crown, the void wrap, about 6-8 m tall, risen out of the water.
            var look = e.GetComponent<UnravellerBody>(); Assert.IsNotNull(look, "her body");
            Assert.IsTrue(look.HasSpider, "the Free Fantasy Spider came in (Resources/Props/fantasySpider)");
            Assert.IsNotNull(look.Wrap); Assert.That(look.Wrap.ShardCount, Is.InRange(10, 14));
            var parts = e.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToList();
            var all = parts[0].bounds; foreach (var r in parts) all.Encapsulate(r.bounds);
            Assert.That(all.size.y, Is.InRange(3.5f, UnravellerBody.WrapLimit + 2.5f), "the spider and its crown, the wrap under 1.5 times her: " + all.size.y);
            Assert.That(Mathf.Max(all.size.x, all.size.z), Is.GreaterThan(6f), "legs spread wide");
            Assert.IsNotNull(look.Playing, "a clip playing (idle)");
            Time.timeScale = 4;
            Pull(s, e);
            yield return Until(() => e.PullAt >= 0, 10);
            Assert.GreaterOrEqual(e.PullAt, 0, "the pull");
            Assert.IsNotNull(s.HarpoonRotation); Assert.GreaterOrEqual(s.HarpoonRotation.Order.Count, 2, "two or three harpooners set");
            Assert.GreaterOrEqual(s.InterruptRotation.Order.Count, 2, "two or three interrupters set");
            var tanks = s.PartySims.Where(c => c.Role == SimCompanion.PartyRole.Tank).Select(c => c.actor).ToList();
            var held = new HashSet<Actor>(); int maxSteel = 0, maxUnravel = 0; bool framesSeen = false;
            float start = Time.time;
            while (Time.time - start < 150 && e.actor.IsAlive)
            {
                if (e.Victim != null && tanks.Contains(e.Victim)) held.Add(e.Victim);
                foreach (var c in s.PartySims) if (c.Melee) maxSteel = Mathf.Max(maxSteel, WeaveEaterRules.SteelStacks(c.actor.GetComponent<Crulanda.Combat.Combatant>().Statuses, Time.time));
                foreach (var a in tanks) maxUnravel = Mathf.Max(maxUnravel, e.UnravelOn(a));
                if (s.RaidBossEnrage > 0 && s.RaidBossPinned > 0 && s.RaidMarksOf(e.Victim) != null) framesSeen = true;
                TakeTo(e, .75f + .2f * (1 - (Time.time - start) / 150));   // she stays in phase 1
                yield return null;
            }
            Assert.IsTrue(e.actor.IsAlive, "she lives through it (her health made deep)");
            Assert.GreaterOrEqual(e.Pins.Landed, 4, "the rotation threw harpoon after harpoon: " + e.Pins.Landed);
            Assert.Less(UnravellerRules.PinSeconds(e.Pins.Landed, t), t.pinFirst, "each pin shorter than the last");
            Assert.Greater(s.PartySims.Sum(c => c.HarpoonsThrown), 1, "more than one sim threw");
            Assert.GreaterOrEqual(e.FeedingsStopped, 1, "the Feeding stopped (a kick, or a harpoon in it)");
            Assert.GreaterOrEqual(maxSteel, 2, "steel black more than one stack deep on a melee sim");
            Assert.GreaterOrEqual(maxUnravel, 2, "Unravel stacks on a tank");
            Assert.AreEqual(2, held.Count, "the tanks swapped her between them");
            Assert.IsTrue(framesSeen, "the boss frame's pin timer and clock ran, and the raid frames showed the marks");
            Assert.Less(s.RaidBossEnrage, t.enrageSeconds, "the enrage clock counts down");
        }

        [UnityTest, Timeout(900000)] public IEnumerator Phases_two_and_three_brood_ground_swallowed_hunt_and_the_feast()
        {
            yield return Raid(240); var t = UnravellerTuning.Current;
            var s = Session(); var e = s.UnravellerFoe;
            Time.timeScale = 4;
            Pull(s, e);
            yield return Until(() => e.PullAt >= 0, 10);
            TakeTo(e, .65f);
            yield return Until(() => e.RaidPhase == 2, 5);
            Assert.AreEqual(2, e.RaidPhase, "The Brood at 70%");
            float start = Time.time;
            while (Time.time - start < 70 && (s.BroodRisen == 0 || s.UnmadePatches == 0 || s.SwallowsFreed + s.SwallowsErased == 0)) { TakeTo(e, .6f); yield return null; }
            Assert.GreaterOrEqual(s.BroodRisen, 3, "three brood out of the river");
            Assert.Greater(s.PartySims.Sum(c => c.SaltsThrown), 0, "sims threw salt (the brood, the swallowed)");
            Assert.Greater(s.UnmadePatches, 0, "unmade ground laid where she floats");
            Assert.Greater(s.PartySims.Sum(c => c.StepsOut), 0, "sims stepped out of marked ground");
            Assert.GreaterOrEqual(s.SwallowsFreed + s.SwallowsErased, 1, "someone swallowed and it ended");
            Assert.GreaterOrEqual(s.SwallowsFreed, 1, "three salts freed them");
            TakeTo(e, .35f);
            yield return Until(() => e.RaidPhase == 3, 5);
            Assert.AreEqual(3, e.RaidPhase, "The Unravelling at 40%");
            start = Time.time;
            while (Time.time - start < 60 && (s.HuntsMissed + s.HuntsLanded == 0 || s.Pulses < 3)) { TakeTo(e, .3f); yield return null; }
            Assert.GreaterOrEqual(s.Pulses, 3, "the pulse");
            Assert.GreaterOrEqual(s.HuntsMissed + s.HuntsLanded, 1, "she hunted");
            Assert.GreaterOrEqual(s.HuntsMissed, 1, "the hunted ran for the river and she missed her meal");
            var w = s.WorldBossNow; int wipesBefore = w.wipes;
            yield return Until(() => s.Feasts > 0, t.enrageSeconds);
            Assert.AreEqual(1, s.Feasts, "the feast at the enrage");
            Assert.AreEqual(wipesBefore + 1, w.wipes, "a wipe counted");
            Assert.IsFalse(s.PartySims.Any(c => c.actor.IsAlive && !ZoneBuilder.Active.ChemicalAt(c.transform.position) && Vector3.Distance(c.transform.position, e.transform.position) < 20), "nobody left standing on the bar");
            // Nothing of the fight stays on screen (Chris: "nothing should be permanent onscreen"): her harmful marks, the brood, the cracks,
            // the afterimages, all cleared with the fight's end.
            for (float waited = 0; waited < 3; waited += Time.deltaTime) yield return null;
            Assert.AreEqual(0, BlowMark.Live.Count(m => m != null && m.Showing && m.Kind == GroundKind.Harmful), "no harmful mark left on the ground");
            Assert.AreEqual(0, s.BroodNow.Count, "no brood left"); Assert.AreEqual(0, s.UnmadePatches, "no unmade ground left");
            Assert.IsNull(GameObject.Find("Void afterimage"), "no afterimage left");
            Assert.IsNull(s.SwallowedActor); Assert.IsNull(s.HuntedActor);
        }
    }
}
#endif
