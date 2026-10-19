#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Party sims on a training dummy (2026-10-17; Chris, watching the level-17 Hallow's Creek run's meter: "still not balanced i dont
    /// think"). A sim of each part at levels 11, 15, 17 and 20 sustains its band (SimCompanion.Damage): a pure damage-dealer the pure band
    /// (66-72 at 15, grown as a mob's health), the Ranger with its wolf counted in, a hybrid 90% of it; the tank's three quarters of a
    /// damage-dealer's (Chris, 2026-10-18), the healer's less. And the last mob of a pack, held under the moon, is fought by the whole party, not by the tank alone.
    /// </summary>
    public class SimDummyTests
    {
        string root; EncounterSession session; float hourWas;
        static readonly int[] Levels = { 11, 15, 17, 20 };
        /// <summary>Game seconds measured at each level, and a frame's game time (Time.captureDeltaTime): a blow is due at most a frame late.</summary>
        const float Seconds = 40, Step = .02f;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-simdummy-" + Guid.NewGuid().ToString("N"));
            hourWas = WorldClock.Hour;
            // A Warrior: its swings keep the fight on (auto attack) whatever the scene's default class.
            EncounterSession.StartClassOverride = "class.warrior";
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(SimPopulation.Active);
            if (session.Companion != null) session.Companion.gameObject.SetActive(false);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.captureDeltaTime = 0;
            EncounterSession.StartClassOverride = null;
            WorldClock.Hour = hourWas;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        /// <summary>A sim of this class (and path, if asked) from anywhere in the world, brought to Oakhaven, online, willing and of your level, and invited.</summary>
        SimCompanion Join(string classId, Func<SimAdventurer, bool> fits = null)
        {
            var pop = SimPopulation.Active;
            foreach (var s in pop.World.sims.Where(x => x.classId == classId && !session.InParty(x.id) && (fits == null || fits(x))).OrderBy(x => x.zone == "zone.oakhaven" ? 0 : 1).ToList())
            {
                s.zone = "zone.oakhaven"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = session.Progress.Level;
                pop.Refresh();
                if (session.InviteRefusal(s) == null && session.Invite(s.id)) return session.PartySim(s.id);
            }
            return null;
        }
        /// <summary>A mob made a training dummy: it stands, never strikes, and has health enough for the whole test.</summary>
        EncounterEnemy Dummy()
        {
            var e = session.Enemies.Where(x => x != null && x.actor.IsAlive && !x.Game && !x.Hidden && !x.Elite && x.Inside == null)
                .OrderBy(x => x.transform.position.sqrMagnitude).First();
            e.enabled = false; var ag = e.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (ag != null && ag.isOnNavMesh) ag.isStopped = true;
            // (2026-10-18, the run of 16:09: the Rogue 61.2 a second at 15 with 2.8 s not striking, the Scoundrel 3.0 s, on a Concord collector)
            // A dummy has no moves: no cast (a collector's hold), no trick (a shield-line, a flash) and no elite move, whatever wakes it.
            e.Cast = null; e.Trick = null;
            // No armour, as the balance harness's dummies (BalanceHarness.Dummy): the band is measured on them. An Oakhaven mob's armour
            // took about a tenth off every blow (the first Unity run, 2026-10-17: the level-11 Rogue 46.5 and Mage 46.4 against an aim of 51.5).
            // Its level is set with yours at each level tested (AtLevel).
            e.actor.Stats.SetBase(Crulanda.Core.StatType.Armor, BalanceHarness.DummyArmour);
            e.actor.Health.Pool.SetMax(5000000, false); e.actor.Health.Pool.SetCurrent(5000000);
            // Nothing else near it: no other mob joins, no splash lands elsewhere.
            foreach (var o in session.Enemies)
                if (o != null && o != e && (o.transform.position - e.transform.position).sqrMagnitude < 40 * 40) { o.enabled = false; var oag = o.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (oag != null) oag.enabled = false; o.transform.position += Vector3.forward * 300; }
            return e;
        }
        /// <summary>Damage a second from a meter row's blows: what landed after its first blow, over the time from the first to the last.</summary>
        sealed class Clock { public int first = -1, last; public float firstAt, lastAt; public float Rate { get { return lastAt > firstAt ? (last - first) / (lastAt - firstAt) : 0; } } }
        static int DamageOf(EncounterSession s, string id) { foreach (var r in s.Meter.Rows) if (r.id == id) return r.damage; return 0; }

        readonly Dictionary<string, float> offReach = new Dictionary<string, float>();
        /// <summary>Each sim's sustained damage a second on the dummy, per level (ids: the sims' and their pets').</summary>
        IEnumerator Measure(List<SimCompanion> party, EncounterEnemy dummy, Dictionary<string, float> rates)
        {
            var ids = new List<string>();
            foreach (var c in party) { ids.Add(c.actor.EntityId.Value); var pet = session.SimPets.Find(p => p != null && p.Master == c); if (pet != null) ids.Add(pet.actor.EntityId.Value); }
            var clocks = ids.ToDictionary(x => x, x => new Clock()); var seen = ids.ToDictionary(x => x, x => DamageOf(session, x));
            float end = Time.time + Seconds; offReach.Clear();
            while (Time.time < end)
            {
                yield return null;
                // Diagnosis (2026-10-18: the melee Rogue and Scoundrel 8% under their aim at 15 only): seconds each spent not striking.
                foreach (var c in party) if (c != null && !(c.Activity ?? "").StartsWith("Fighting") && !(c.Activity ?? "").StartsWith("Shooting")) { offReach.TryGetValue(c.sim.name, out var o); offReach[c.sim.name] = o + Time.deltaTime; }
                foreach (var id in ids)
                {
                    int now = DamageOf(session, id); if (now == seen[id]) continue;
                    seen[id] = now; var k = clocks[id];
                    if (k.first < 0) { k.first = now; k.firstAt = Time.time; }
                    k.last = now; k.lastAt = Time.time;
                }
                if (dummy.actor.Health.Pool.Current < 1000000) dummy.actor.Health.Pool.SetCurrent(5000000);
                // Kept still: nothing (its waking from a doze, a level change) sets it going again while the clock runs.
                if (dummy.enabled) dummy.enabled = false;
            }
            foreach (var id in ids) rates[id] = clocks[id].Rate;
        }

        IEnumerator Party(string[] classes, Func<SimAdventurer, bool> fits, List<string> report)
        {
            var party = new List<SimCompanion>();
            foreach (var cls in classes) { var c = Join(cls, fits); Assert.NotNull(c, "a " + cls + " sim joins"); party.Add(c); }
            yield return null;
            var dummy = Dummy();
            var motor = session.Player.GetComponent<AdventurerMotor>();
            motor.Teleport(dummy.transform.position + Vector3.back * 14 + Vector3.up); yield return null;
            session.Select(dummy); session.BeginAutoAttack(); Assert.IsTrue(session.AutoAttack, "the fight is on");
            Time.captureDeltaTime = Step;
            foreach (int level in Levels)
            {
                // You and the dummy at the level tested, as the harness's player and dummies are both 15. A mob over the one it fights takes a
                // tenth less a level (EncounterEnemy.OvermatchTakenNormal, five levels at most); with no one on it, that one is you (FoeLevel):
                // the second Unity run (19:25) had a level-15 dummy against your Oakhaven level, and every blow lost half.
                session.Progress.experience = EncounterProgress.XpForLevel(level); session.Player.SetLevel(level);
                dummy.actor.SetLevel(level); dummy.actor.Health.Pool.SetMax(5000000, false); dummy.actor.Health.Pool.SetCurrent(5000000);
                foreach (var c in party)
                {
                    c.MatchLevel(level); Assert.AreEqual(level, c.sim.level);
                    var ag = c.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    var at = dummy.transform.position + Quaternion.Euler(0, 40 * party.IndexOf(c), 0) * Vector3.back * (c.Melee ? 1.6f : 10);
                    if (UnityEngine.AI.NavMesh.SamplePosition(at, out var hit, 4, UnityEngine.AI.NavMesh.AllAreas)) ag.Warp(hit.position);
                }
                // A blow or two at the new level before the clock starts.
                for (float t = 0; t < 5; t += Time.deltaTime) yield return null;
                var rates = new Dictionary<string, float>();
                yield return Measure(party, dummy, rates);
                float pure = SimCompanion.PureDps(level), scale = SimCompanion.BandScale(level);
                foreach (var c in party)
                {
                    float own = rates[c.actor.EntityId.Value]; var pet = session.SimPets.Find(p => p != null && p.Master == c);
                    float withPet = own + (pet != null ? rates[pet.actor.EntityId.Value] : 0);
                    string who = c.sim.name + " (" + c.sim.classId.Replace("class.", "") + ", " + c.Role + ", level " + level + ")";
                    string line = who + ": " + withPet.ToString("0.0") + " a second" + (pet != null ? " (its pet " + (withPet - own).ToString("0.0") + ")" : "") + "; band middle " + pure.ToString("0.0") + "; " + c.Activity + "; " + (offReach.TryGetValue(c.sim.name, out var off) ? off.ToString("0.0") : "0") + " s not striking; " + Vector3.Distance(c.transform.position, dummy.transform.position).ToString("0.0") + " m off; dummy armour " + dummy.actor.Stats.GetRounded(Crulanda.Core.StatType.Armor);
                    // The line goes into the log now and into a failing assertion's message (the results XML carries the message; the log
                    // written after the party's levels never came, as a failed assertion ends the test first).
                    report.Add(line); Debug.Log("Sim on the dummy: " + line);
                    if (c.Role == SimCompanion.PartyRole.Damage)
                    {
                        bool hybrid = EncounterSession.Hybrid(c.sim.classId); float share = c.sim.classId == "class.archivist" ? SimCompanion.ControlDpsShare : hybrid ? EncounterSession.HybridDpsShare : 1;   // the Archivist: control and support (raid 2)
                        float aim = c.BandDps;
                        Assert.That(aim, Is.InRange(SimCompanion.PureBandLow * scale * share, SimCompanion.PureBandHigh * scale * share), who + ": its aim is in the " + (hybrid ? "hybrid" : "pure") + " band");
                        // Within 3% of its aim (5% for the Unwriting: its unwrite ticks are a third of a blow, rounded down).
                        float within = SimCompanion.Unwriter(c.sim) ? .05f : .03f;
                        Assert.That(withPet, Is.InRange(aim * (1 - within), aim * (1 + within)), who + " sustains its band on the dummy" + (pet != null ? ", its pet counted in" : "") + " [" + line + "]");
                    }
                    // The tank: three quarters of a damage-dealer's (Chris, 2026-10-18: "maybe 3/4 of dps"; SimCompanion.TankDpsShare).
                    else if (c.Role == SimCompanion.PartyRole.Tank) Assert.That(withPet / pure, Is.InRange(.7f, .8f), who + ": a tank deals about three quarters of a damage-dealer's");
                    else Assert.That(withPet / pure, Is.InRange(.1f, .35f), who + ": a healer's damage is less again");
                }
                // The order Chris asked for: every damage-dealer over the tank (the Archivist, control and support, about a tank's: raid 3), the tank over the healer.
                float RateOf(SimCompanion c) { var pet = session.SimPets.Find(p => p != null && p.Master == c); return rates[c.actor.EntityId.Value] + (pet != null ? rates[pet.actor.EntityId.Value] : 0); }
                var tankSim = party.Find(c => c.Role == SimCompanion.PartyRole.Tank); var healSim = party.Find(c => c.Role == SimCompanion.PartyRole.Healer);
                if (tankSim != null)
                {
                    foreach (var c in party) if (c.Role == SimCompanion.PartyRole.Damage && c.sim.classId != "class.archivist") Assert.Greater(RateOf(c), RateOf(tankSim), c.sim.name + " out-damages the tank at " + level);
                    if (healSim != null) Assert.Greater(RateOf(tankSim), RateOf(healSim), "the tank out-damages the healer at " + level);
                }
            }
            Time.captureDeltaTime = 0;
            session.StopAutoAttack();
        }

        /// <summary>The Rogue, the Ranger and its wolf, the Warrior tank and the Druid healer: the Hallow's Creek run's party.</summary>
        [UnityTest, Timeout(600000)] public IEnumerator A_sim_of_each_part_sustains_its_band_on_a_dummy()
        {
            var report = new List<string>();
            yield return Party(new[] { "class.rogue", "class.ranger", "class.warrior", "class.druid" }, null, report);
            Debug.Log("Sims on the dummy:\n" + string.Join("\n", report));
        }
        /// <summary>The other damage-dealers: a Mage, a Scoundrel and an Archivist (a hybrid in the damage part).</summary>
        [UnityTest, Timeout(600000)] public IEnumerator The_other_damage_sims_sustain_theirs()
        {
            var report = new List<string>();
            yield return Party(new[] { "class.mage", "class.scoundrel", "class.archivist" }, null, report);
            Debug.Log("Sims on the dummy:\n" + string.Join("\n", report));
        }

        /// <summary>The last of a pack (the level-17 Hallow's Creek run, 2026-10-17: a quarter of the fighting the Rogue, the Ranger and the
        /// healer stood at your side while the tank fought the last mob alone, the one held under the moon): the held mob waits while another
        /// fights the party, then the party fights it and the moon comes off.</summary>
        [UnityTest] public IEnumerator The_last_mob_held_under_the_moon_is_fought_by_the_party()
        {
            var a = session.Enemies.First(e => e != null && e.actor.IsAlive && e.Camp && !e.Elite && !e.Game && !e.Hidden &&
                session.Enemies.Any(o => o != null && o != e && o.actor.IsAlive && o.Camp && !o.Elite && !o.Game && !o.Hidden && Vector3.Distance(o.transform.position, e.transform.position) < 12));
            var b = session.Enemies.Where(o => o != null && o != a && o.actor.IsAlive && o.Camp && !o.Elite && !o.Game && !o.Hidden).OrderBy(o => Vector3.Distance(o.transform.position, a.transform.position)).First();
            // Only these two: every other mob within 60 m is stilled and taken far off (the second Unity run, 19:25: after the free wolf fell
            // another Grey wolf of the camp came, and the Ranger rightly shot that one first).
            foreach (var o in session.Enemies)
                if (o != null && o != a && o != b && (o.transform.position - a.transform.position).sqrMagnitude < 60 * 60)
                { o.enabled = false; var oag = o.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (oag != null) oag.enabled = false; o.transform.position += Vector3.forward * 400; }
            var c = Join("class.ranger"); Assert.NotNull(c, "a Ranger joins"); yield return null;
            session.Player.GetComponent<AdventurerMotor>().Teleport(a.transform.position + Vector3.back * 6); yield return null;
            c.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(session.Player.transform.position + Vector3.right * 2);
            string me = session.Player.EntityId.Value; a.threat.Add(me, 50); b.threat.Add(me, 50);
            for (float t = 0; t < 3 && !(a.FightingParty && b.FightingParty); t += Time.deltaTime) { a.threat.Add(me, 1); b.threat.Add(me, 1); yield return null; }
            Assert.IsTrue(a.FightingParty && b.FightingParty, "both on the party");
            b.RaidMarked = RaidMark.Moon; Assert.IsNull(b.Apply("incap", session.Player, 20), "held"); yield return null;
            Assert.AreNotEqual(b, c.Quarry, "while another is free, the held one waits");
            Assert.AreEqual(a, c.Quarry, "the free one is fought");
            // Far over its health: its armour takes a share of any blow (the first Unity run: a blow of its health and ten left it standing).
            a.Receive(a.actor.Health.Pool.Max * 100, session.Player); yield return null;
            Assert.IsFalse(a.actor.IsAlive, "the free one falls");
            Assert.IsTrue(b.FightingParty, "the held one is still on the party");
            EncounterEnemy q = null; for (float t = 0; t < 3 && q != b; t += Time.deltaTime) { q = c.Quarry; yield return null; }
            Assert.AreEqual(b, q, "then the party fights the last one (" + c.Activity + ")");
            Assert.AreEqual(RaidMark.None, b.RaidMarked, "and the moon comes off it");
        }
    }
}
#endif
