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
    /// (2026-10-15, for tuning the duo quickly; GAME-ONLY) The group run's fight at Nix and the Rock-Eater, alone: the run's party (you a
    /// level-11 Paladin played as the run's bot plays, with a Warrior tank leading, a Druid healer, a Ranger with its wolf and a Rogue, all
    /// level 11 in the run's gear), every other camp down, the party set about 15 m from Nix's camp, the leader pulling, at 3x speed.
    /// A line every 5 s of game time (each member's health, whom the beast and Nix strike, whom the healer mends, the duo's health); passes
    /// when the duo is dead, fails when the party is all down or 240 s of game time pass.
    /// </summary>
    public class AditNixFightTests
    {
        const float Speed = 3, Limit = 240, LogEvery = 5, Off = 15;
        string root; EncounterSession session;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-aditnix-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            EncounterSession.StartClassOverride = "class.paladin";
            ZoneBuilder.RequestedZoneId = "zone.adit";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; EncounterInput.Scripted = null; AdventurerMotor.Running = false;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null; EncounterSession.StartClassOverride = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        int CampIndex(string name) { int c = Array.FindIndex(session.Zone.Zone.camps, x => x != null && x.name == name); Assert.GreaterOrEqual(c, 0, "the Adit has the camp " + name); return c; }

        /// <summary>RunCapture.BotCast, as the run's bot casts (it is private there): a mend when someone is under 45% and the kit has one,
        /// else the first ready damage ability on the bar in its order, else the first slot.</summary>
        static void BotCast(EncounterSession s)
        {
            if (s.PlayerCasting || s.Kit == null) return;
            bool hurt = s.Player.Health.Pool.Ratio < .45f || s.PartySims.Exists(c => c != null && c.actor != null && c.actor.IsAlive && c.actor.Health.Pool.Ratio < .45f);
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < s.Kit.ActionCount; i++)
                {
                    var a = s.Kit.ActionAt(i); if (a == null || s.Kit.ActionLockLabel(i) != null) continue;
                    bool want = pass == 0 ? hurt && a.effect == Crulanda.Abilities.AbilityEffect.Heal : a.effect == Crulanda.Abilities.AbilityEffect.Damage;
                    if (want && s.UseAbility(i)) return;
                }
            s.UseAbility(0);
        }

        static void Put(Component c, Vector3 at)
        {
            var agent = c.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Warp(at); else c.transform.position = at;
        }

        [UnityTest] public IEnumerator The_run_party_beats_Nix_and_the_Rock_Eater()
        {
            yield return null;
            var s = session; Assert.NotNull(s, "the session");
            Assert.AreEqual("class.paladin", s.ClassDef.id, "you are the run's Paladin");
            // As RunCapture: level 11, full, the gates the run's quests open stand open.
            if (s.Progress.Level < 11) { s.Progress.experience = EncounterProgress.XpForLevel(11); s.Player.SetLevel(11); }
            s.Player.Health.ApplyHealing(s.Player.Health.Pool.Max); s.Player.Resource.Pool.Fill();
            foreach (var g in s.Zone.Gates) if (g != null && (g.Kind == "liftgate" || g.Kind == "platformgate")) g.SetOpen(true, true);
            // The run's party: a Warrior to tank and lead, a Druid to heal, a Ranger with its wolf, a Rogue; you the Paladin.
            var pop = SimPopulation.Active; Assert.NotNull(pop, "sims");
            var chosen = new List<SimAdventurer>();
            foreach (var want in new[] { "class.warrior", "class.druid", "class.ranger", "class.rogue", "class.archivist", "class.paladin", "class.mage" })
            {
                if (chosen.Count >= EncounterSession.MaxPartySims) break;
                var x = pop.World.sims.Find(y => y.classId == want && !chosen.Contains(y)); if (x != null) chosen.Add(x);
            }
            var p0 = s.Player.transform.position; int k = 0;
            foreach (var x in chosen) { x.zone = s.ZoneId; x.onlineFrom = 0; x.onlineHours = 24; x.friendly = .9f; x.level = 11; x.bold = x.classId == "class.warrior" ? .95f : .3f; x.gearBonus = 2; x.x = p0.x + 2 + k; x.z = p0.z + (k % 2 == 0 ? 1.5f : -1.5f); k++; }
            pop.Refresh(); yield return new WaitForSeconds(.5f);
            foreach (var x in chosen) Assert.IsTrue(s.Invite(x.id, true), "invited " + x.name + " (" + x.classId + ")");
            s.Player.Stats.SetBase(Crulanda.Core.StatType.Armor, SimCompanion.ArmourFor(new SimAdventurer { classId = s.ClassDef.id, level = s.Progress.Level, gearSeed = 7, gearBonus = 2 }, s.Items));
            yield return new WaitForSeconds(.5f);
            var log = new List<string>(); void Log(string line) { log.Add(line); Debug.Log("AditNixFight " + line); }
            Log("party: you (" + s.ClassDef.displayName + " " + s.Progress.Level + ", " + s.Player.Health.Pool.Max + " health, armour " + s.Player.Stats.GetRounded(Crulanda.Core.StatType.Armor) + "), " + string.Join(", ", s.PartySims.Where(c => c != null).Select(c => c.sim.name + " (" + SimRoster.ClassName(c.sim.classId) + ", " + c.Role + ", " + c.actor.Health.Pool.Max + " health, armour " + c.actor.Stats.GetRounded(Crulanda.Core.StatType.Armor) + ")")) + (s.SimPets.Count > 0 ? "; " + string.Join(", ", s.SimPets.Where(p => p != null).Select(p => p.actor.DisplayName + " " + p.actor.Health.Pool.Max)) : ""));

            int nixCamp = CampIndex("Nix and the Rock-Eater");
            var beast = s.Enemies.Find(e => e != null && e.Camp && e.MobName == EncounterSession.RockEaterName && e.CampIndex == nixCamp); Assert.NotNull(beast, "the Rock-Eater");
            var nix = s.Enemies.Find(e => e != null && e.Camp && e.MobName == EncounterSession.NixName && e.CampIndex == nixCamp); Assert.NotNull(nix, "Nix");
            float keepDelay = EncounterSession.ReleaseDelay; EncounterSession.ReleaseDelay = 1e5f;   // no relief shift walking in behind
            try
            {
                Assert.IsTrue(s.LeadDungeon(), "a sim leads the run");
                var leader = s.PartySims.Find(c => c != null && c.Dungeon != null); Assert.NotNull(leader, "the leader");
                // The way the run comes in: the step before Nix's on the leader's route.
                Vector3? from = null;
                var steps = leader.Route.ToList(); int ni = steps.FindIndex(st => st.camps.Contains(nixCamp));
                for (int i = ni - 1; i >= 0 && from == null; i--) from = steps[i].at;
                // Every camp but Nix's down (his own guards are his fight).
                foreach (var e in s.Enemies.ToArray()) if (e != null && e.Camp && e.actor.IsAlive && e.CampIndex != nixCamp) e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3);
                for (int i = 0; i < 60 && !(leader.Route.Count > 0 && leader.Route.First().camps.Contains(nixCamp)); i++) yield return null;
                if (!(leader.Route.Count > 0 && leader.Route.First().camps.Contains(nixCamp)))
                {
                    Log("the standard run's first step is " + leader.LeadingName + " (" + string.Join(" > ", leader.Route.Select(st => st.name)) + "): led quick instead");
                    leader.StopLeading(); Assert.IsNotNull(leader.LeadDungeon("quick", out var why), why);
                    for (int i = 0; i < 60 && !(leader.Route.Count > 0 && leader.Route.First().camps.Contains(nixCamp)); i++) yield return null;
                }
                Assert.IsTrue(leader.Route.Count > 0 && leader.Route.First().camps.Contains(nixCamp), "Nix's fight is the leader's step: " + leader.LeadingName);
                var camp = leader.Route.First().at;
                // The party about 15 m off his camp, on the way the run comes in, on the camp's level with a clear walk to it.
                Assert.IsTrue(NavMesh.SamplePosition(camp, out var campHit, 4, NavMesh.AllAreas), "Nix's camp is on the navmesh");
                Vector3 want0 = from.HasValue ? from.Value - camp : -beast.transform.forward; want0.y = 0; if (want0.sqrMagnitude < .01f) want0 = Vector3.back;
                float a0 = Mathf.Atan2(want0.x, want0.z) * Mathf.Rad2Deg; Vector3? spot = null;
                foreach (float da in Enumerable.Range(0, 24).Select(i => (i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * 15f))
                {
                    float ang = (a0 + da) * Mathf.Deg2Rad; var p = campHit.position + new Vector3(Mathf.Sin(ang), 0, Mathf.Cos(ang)) * Off;
                    if (!NavMesh.SamplePosition(p, out var h, 2.5f, NavMesh.AllAreas) || Mathf.Abs(h.position.y - campHit.position.y) > 3) continue;
                    if (NavMesh.Raycast(campHit.position, h.position, out _, NavMesh.AllAreas)) continue;
                    spot = h.position; break;
                }
                Assert.IsTrue(spot.HasValue, "a clear spot 15 m off Nix's camp");
                var at = spot.Value; var side = Vector3.Cross(Vector3.up, (campHit.position - at).normalized);
                var motor = s.Player.GetComponent<AdventurerMotor>(); motor.Teleport(at + Vector3.up);
                int slot = 0;
                foreach (var c in s.PartySims) if (c != null) { slot++; var q = at + side * (slot % 2 == 0 ? 1.5f : -1.5f) * ((slot + 1) / 2) - (campHit.position - at).normalized * (c == leader ? -1 : 1); if (NavMesh.SamplePosition(q, out var h, 2, NavMesh.AllAreas)) q = h.position; Put(c, q); }
                foreach (var p in s.SimPets) if (p != null) { var q = at - (campHit.position - at).normalized * 2; if (NavMesh.SamplePosition(q, out var h, 2, NavMesh.AllAreas)) q = h.position; Put(p, q); }
                s.Player.Health.ApplyHealing(s.Player.Health.Pool.Max); s.Player.Resource.Pool.Fill();
                foreach (var c in s.PartySims) if (c != null) c.actor.Health.ApplyHealing(c.actor.Health.Pool.Max);
                yield return null;
                Log("set at " + at.ToString("0.0") + ", " + Vector3.Distance(at, campHit.position).ToString("0") + " m off Nix's camp " + campHit.position.ToString("0.0") + (from.HasValue ? " (on the way in from the step before)" : "") + "; the camp: " + string.Join(", ", s.Enemies.Where(e => e != null && e.Camp && e.CampIndex == nixCamp && e.actor.IsAlive).Select(e => e.Name + " " + e.actor.Health.Pool.Max)));

                // The fight, at the run's speed: the leader pulls; you fight as the run's bot does.
                var members = new List<(string name, Crulanda.Gameplay.Actor actor)> { ("you", s.Player) };
                foreach (var c in s.PartySims) if (c != null) members.Add((c.sim.name.Split(' ')[0] + " (" + SimRoster.ClassName(c.sim.classId) + ")", c.actor));
                foreach (var p in s.SimPets) if (p != null) members.Add((p.actor.DisplayName, p.actor));
                var down = new Dictionary<Crulanda.Gameplay.Actor, float>(); var deaths = new List<string>();
                var healer = s.PartySims.Find(c => c != null && c.Role == SimCompanion.PartyRole.Healer);
                var engagedAt = new Dictionary<EncounterEnemy, float>();
                string Who(Crulanda.Gameplay.Actor a) { if (a == null) return "nobody"; var m = members.Find(x => x.actor == a); return m.actor != null ? m.name : a.DisplayName; }
                string Hp(Crulanda.Gameplay.Actor a) { return a == null ? "-" : !a.IsAlive ? "dead" : a.Health.Pool.Current + "/" + a.Health.Pool.Max; }
                float t = 0, nextLog = 0, nextSwing = 0, pathAt = 0, stuckAt = 0, pulledAt = -1, beastDiedAt = -1, nixDiedAt = -1; Vector3 stuckFrom = s.Player.transform.position, pathGoal = Vector3.zero; NavMeshPath path = null; bool pulledByYou = false;
                AdventurerMotor.Running = true; Time.timeScale = Speed;
                void Line() { Log(t.ToString("0") + "s: " + string.Join(", ", members.Select(m => m.name + " " + Hp(m.actor))) + " | beast " + Hp(beast.actor) + " on " + (beast.actor.IsAlive ? Who(beast.Victim) : "-") + ", Nix " + Hp(nix.actor) + (nix.Inside != null ? " (in the saddle)" : " on " + (nix.actor.IsAlive ? Who(nix.Victim) : "-")) + " | " + (s.DuoState ?? "no duo") + " | healer " + (healer == null ? "none" : healer.actor.IsAlive ? (healer.Activity ?? "-") : "dead") + " | guards up " + s.Enemies.Count(e => e != null && e.Camp && e.CampIndex == nixCamp && e != beast && e != nix && e.actor.IsAlive) + " | leader " + (leader.Activity ?? "-") + " (" + leader.LeadingName + ")" + " | you on " + (s.Target != null ? s.Target.Name : "nothing")); }
                while (t < Limit)
                {
                    if (beast.Engaged || nix.Engaged || nix.Inside == null) { if (pulledAt < 0) { pulledAt = t; Log(t.ToString("0") + "s: the pull" + (pulledByYou ? " (yours)" : "") + ": the beast on " + Who(beast.Victim) + ", Nix " + (nix.Inside != null ? "in the saddle" : "on " + Who(nix.Victim))); } }
                    if (!beast.actor.IsAlive && beastDiedAt < 0) { beastDiedAt = t; Log(t.ToString("0") + "s: the Rock-Eater falls"); }
                    if (!nix.actor.IsAlive && nixDiedAt < 0) { nixDiedAt = t; Log(t.ToString("0") + "s: Nix falls"); }
                    foreach (var m in members) { bool dead = !m.actor.IsAlive; if (dead && !down.ContainsKey(m.actor)) { down[m.actor] = t; deaths.Add(m.name + " at " + t.ToString("0") + "s"); Log(t.ToString("0") + "s: " + m.name + " falls"); } else if (!dead) down.Remove(m.actor); }
                    if (t >= nextLog) { nextLog = t + LogEvery; Line(); }
                    if (!beast.actor.IsAlive && !nix.actor.IsAlive) break;
                    if (!s.Player.IsAlive && s.PartySims.All(c => c == null || !c.actor.IsAlive)) { Log(t.ToString("0") + "s: the party is all down"); break; }
                    var me = s.Player.transform.position;
                    if (!s.Player.IsAlive) { EncounterInput.Scripted = Vector2.zero; t += Time.deltaTime; yield return null; continue; }   // down: you stay down (the run would get up at the stone, out of this fight)
                    // Nobody pulled in 30 s: you do (the run's leader should have).
                    if (pulledAt < 0 && t > 30 && !pulledByYou) { pulledByYou = true; Log(t.ToString("0") + "s: nobody pulled in 30 s (leader: " + leader.Activity + ", " + Vector3.Distance(leader.transform.position, campHit.position).ToString("0") + " m off): you pull"); s.Select(beast); }
                    // RunCapture's foe: what fights the party near you, a mob yours 2.5 s after the party engages it (at once when it is on you).
                    EncounterEnemy foe = null; float best = 14;
                    foreach (var e in s.Enemies)
                    {
                        if (e == null || !e.actor.IsAlive || e.Game || e.Hidden || !e.FightingParty || SimCompanion.Held(e)) continue;
                        if (!engagedAt.TryGetValue(e, out var since)) { engagedAt[e] = since = Time.time; }
                        if (e.Victim != s.Player && Time.time - since < 2.5f) continue;
                        float d = Vector3.Distance(e.transform.position, me); if (d < best) { best = d; foe = e; }
                    }
                    if (foe == null && pulledByYou && pulledAt < 0) foe = beast;
                    Vector3 goal = foe != null ? foe.transform.position : leader.actor.IsAlive ? leader.transform.position : campHit.position; float reach = foe != null ? 2.6f : 4.5f;
                    if (foe != null)
                    {
                        if (s.Target != foe) s.Select(foe);
                        if (Time.time >= nextSwing) { nextSwing = Time.time + .4f; BotCast(s); }
                    }
                    var to = goal - me; to.y = 0; float dist = to.magnitude;
                    if (dist > reach)
                    {
                        if (Time.time >= pathAt || pathGoal != goal) { pathAt = Time.time + .5f; pathGoal = goal; if (path == null) path = new NavMeshPath(); NavMesh.SamplePosition(goal, out var g, 3, NavMesh.AllAreas); NavMesh.CalculatePath(me, g.hit ? g.position : goal, NavMesh.AllAreas, path); }
                        var step = to; if (path != null && path.corners.Length > 1) { int ci = 1; while (ci < path.corners.Length - 1 && Vector3.Distance(new Vector3(path.corners[ci].x, 0, path.corners[ci].z), new Vector3(me.x, 0, me.z)) < .45f) ci++; step = path.corners[ci] - me; step.y = 0; }
                        if (step.sqrMagnitude < .01f) step = to;
                        motor.SetView(Mathf.Atan2(step.x, step.z) * Mathf.Rad2Deg, 14, 7); EncounterInput.Scripted = Vector2.up;
                    }
                    else EncounterInput.Scripted = Vector2.zero;
                    if (Time.time - stuckAt > 4)
                    {
                        if (dist > reach + 1 && (Vector3.Distance(stuckFrom, me) < 1 || motor.Swimming))
                        {
                            var w = goal - to.normalized * Mathf.Min(2.5f, dist * .5f);
                            if (NavMesh.SamplePosition(w, out var hit, 4, NavMesh.AllAreas)) { motor.Teleport(hit.position + Vector3.up * 1.05f); Log(t.ToString("0") + "s: you stuck; warped " + (foe != null ? "to the fight" : "to the leader")); }
                        }
                        stuckAt = Time.time; stuckFrom = me;
                    }
                    t += Time.deltaTime; yield return null;
                }
                Time.timeScale = 1; EncounterInput.Scripted = null; Line();
                bool won = !beast.actor.IsAlive && !nix.actor.IsAlive;
                string report = (won ? "won" : "lost") + ": pulled at " + (pulledAt < 0 ? "never" : pulledAt.ToString("0") + "s" + (pulledByYou ? " (by you)" : "")) + ", the beast down at " + (beastDiedAt < 0 ? "-" : beastDiedAt.ToString("0") + "s") + ", Nix down at " + (nixDiedAt < 0 ? "-" : nixDiedAt.ToString("0") + "s") + ", ended at " + t.ToString("0") + "s (game time, at " + Speed + "x)" + "; deaths: " + (deaths.Count == 0 ? "none" : string.Join(", ", deaths)) + "\nlast lines:\n" + string.Join("\n", log.Skip(Math.Max(0, log.Count - 14)));
                Debug.Log("AditNixFight RESULT " + report);
                foreach (var line in s.Meter.Scorecard()) Debug.Log("AditNixFight meter " + line);
                Assert.IsTrue(won, "the run's party beats Nix and the Rock-Eater: " + report);
            }
            finally { Time.timeScale = 1; EncounterInput.Scripted = null; AdventurerMotor.Running = false; EncounterSession.ReleaseDelay = keepDelay; }
        }
    }
}
#endif
