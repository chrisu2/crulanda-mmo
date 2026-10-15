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
    /// The Sealed Adit's polish (Docs/ADIT_REWORK.md stage S7, section 21; written 2026-10-15, not yet run in Unity; GAME-ONLY): the Grey
    /// Spreads (crossing a grey line greys the Breach's lamps behind it, shows its static and lets its seep of crawlers out; the Foreman's death
    /// stops it and brings the lamps back a little); the Foundry's swing-loads (a bucket over each high walk, swinging out and back by hand,
    /// its rope team below, knocking back whoever it meets along the walk and never off it, with no damage); the living lichen along the run.
    /// </summary>
    public class AditPolishTests
    {
        string root; EncounterSession session;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-aditpolish-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.adit";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            AditSwingLoad.Speed = 1;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        int CampIndex(string name) { int c = Array.FindIndex(session.Zone.Zone.camps, x => x != null && x.name == name); Assert.GreaterOrEqual(c, 0, "the Adit has the camp " + name); return c; }
        List<EncounterEnemy> Mine(int camp) { return session.Enemies.Where(e => e != null && e.Camp && e.CampIndex == camp).ToList(); }
        static Hollow Cave(string name) { var h = Hollow.All.Find(o => o.Name == name); Assert.NotNull(h, name); return h; }
        /// <summary>You, standing on the cave's floor <paramref name="along"/> metres in (the body a metre over it).</summary>
        IEnumerator StandIn(Hollow h, float along)
        {
            var at = h.At(along); Assert.IsTrue(NavMesh.SamplePosition(at + Vector3.up * .3f, out var hit, 2, NavMesh.AllAreas), h.Name + " at " + along + " m is walkable");
            session.Player.GetComponent<AdventurerMotor>().Teleport(hit.position + Vector3.up); yield return null;
        }
        /// <summary>A lamp of the cave this side of <paramref name="before"/> metres in (or past it), from the zone's lamps.</summary>
        Light Lamp(Hollow h, float before, bool past)
        {
            foreach (var nl in session.Zone.NightLights)
            {
                if (nl == null || nl.light == null || !h.Inside(nl.light.transform.position, .5f)) continue;
                var p = nl.light.transform.position; h.Local(new Vector2(p.x, p.z), out float along, out _);
                if (past ? along > before + 5 : along < before - 5) return nl.light;
            }
            return null;
        }

        [UnityTest] public IEnumerator Each_grey_line_greys_the_lamps_behind_it_and_lets_its_seep_out_until_the_Foreman_falls()
        {
            yield return null;
            var breach = Cave(EncounterSession.GreyCave);
            Assert.AreEqual(3, session.GreyLines.Count, "three lines across the Breach"); Assert.AreEqual(0, session.GreyLinesCrossed);
            int seep1 = CampIndex("The grey seep (first)"), seep2 = CampIndex("The grey seep (second)");
            Assert.IsEmpty(Mine(seep1), "no seep at the start"); Assert.IsFalse(session.Zone.GreySeepShown(1), "its static hidden");
            var behind = Lamp(breach, session.GreyLines[0], false); var ahead = Lamp(breach, session.GreyLines[0], true);
            Assert.NotNull(behind, "a lamp short of the first line"); Assert.NotNull(ahead, "a lamp past it");
            // Over the first line: the lamps behind go grey, the static shows, the seep comes up the drift after you.
            yield return StandIn(breach, session.GreyLines[0] + 2); yield return new WaitForSeconds(.7f);
            Assert.AreEqual(1, session.GreyLinesCrossed, "the first line crossed");
            Assert.IsTrue(session.Zone.GreySeepShown(1), "its static shows"); Assert.IsFalse(session.Zone.GreySeepShown(2));
            Assert.AreEqual(EncounterSession.GreyKeep, session.Zone.LightShare(behind), .01f, "the lamp behind it greyed");
            Assert.AreEqual(1, session.Zone.LightShare(ahead), .01f, "the lamp ahead as it was");
            Assert.IsTrue(session.Released(seep1)); Assert.AreEqual(2, Mine(seep1).Count, "its two crawlers");
            foreach (var e in Mine(seep1)) Assert.IsTrue(e.actor.IsAlive && NavMesh.SamplePosition(e.transform.position, out _, 1.5f, NavMesh.AllAreas), e.persistentId + " stands on the navmesh");
            Assert.IsTrue(session.Chat.Any(l => l.channel == ChatChannel.System && l.text.Contains("lamps go grey")), "the line is said");
            // The Foreman falls: the seep stops; past the second line nothing comes, and the lamps come back a little.
            var foreman = session.Enemies.Find(x => x != null && x.Camp && x.Elite && x.MobName == EncounterSession.GreyForeman); Assert.NotNull(foreman, "the Foreman");
            foreman.actor.Health.ApplyDamage(foreman.actor.Health.Pool.Max * 3); yield return null;
            Assert.IsTrue(session.GreySeepStopped, "his death stops it");
            Assert.AreEqual(EncounterSession.GreyKeep + (1 - EncounterSession.GreyKeep) * EncounterSession.GreyBack, session.Zone.LightShare(behind), .02f, "the lamps back a little");
            yield return StandIn(breach, session.GreyLines[1] + 2); yield return new WaitForSeconds(.7f);
            Assert.AreEqual(1, session.GreyLinesCrossed, "no line after his death");
            Assert.IsFalse(session.Released(seep2)); Assert.IsEmpty(Mine(seep2), "no second seep"); Assert.IsFalse(session.Zone.GreySeepShown(2));
        }

        [UnityTest] public IEnumerator The_swing_loads_swing_over_their_walks_and_knock_back_along_them_never_off()
        {
            yield return null;
            var z = session.Zone;
            Assert.AreEqual(3, z.SwingLoads.Count, "a bucket over each of the three places"); Assert.AreEqual(3, session.SwingLoads.Count, "each set swinging");
            foreach (var s in session.SwingLoads)
            {
                var spot = s.spot;
                Assert.AreEqual(3, s.Haulers, spot.name + ": its rope team");
                Assert.Less(spot.team.y, spot.bottom.y - 5, spot.name + ": the team on the floor below the walk");
                Assert.IsTrue(z.DeckTop(new Vector2(spot.bottom.x, spot.bottom.z), spot.bottom.y, out float top) && Mathf.Abs(top - spot.bottom.y) < .2f, spot.name + ": the arc bottoms out over a walk");
                Assert.IsNull(spot.bucket.GetComponentInChildren<Collider>(), spot.name + ": the bucket is in nobody's way but by its swing");
                // Down at the walk only as it passes the bottom; out over the hall at the swing's end.
                AditSwingLoad.Speed = 0; s.phase = 0;
                s.Clock = 0; s.Pose(0); Assert.AreEqual((spot.bottom + Vector3.up * ZoneBuilder.SwingHang - s.BucketAt).magnitude, 0, .05f, spot.name + ": at the bottom, over the walk");
                float quarter = s.period / 4; s.Pose(quarter);
                var off = s.BucketAt - spot.bottom; off.y = 0;
                Assert.Greater(off.magnitude, 4, spot.name + ": out at the end of its swing"); Assert.Greater(s.BucketAt.y, spot.bottom.y + 3, spot.name + ": over head height there");
                Assert.Greater(Vector3.Dot(off, spot.swing), 0, spot.name + ": out toward its team");
            }
            // Struck at the bottom: knocked back along the walk, on its level, unhurt.
            var load = session.SwingLoads[0]; var sp = load.spot;
            load.Clock = 0; load.Pose(0);
            Assert.IsTrue(NavMesh.SamplePosition(sp.bottom + Vector3.up * .2f, out var on, 1, NavMesh.AllAreas), "the walk under the bucket is walkable");
            // (chainR129: "before" was read a frame after the teleport, when the bucket had already thrown you: moved 0. Read it here.)
            var before = on.position + sp.walk * .3f + Vector3.up; int health = session.Player.Health.Pool.Current;
            session.Player.GetComponent<AdventurerMotor>().Teleport(before); yield return null;
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(1, load.Hits, "the bucket meets you");
            var moved = session.Player.transform.position - before;
            Assert.Greater(Mathf.Abs(Vector3.Dot(moved, sp.walk)), 1.2f, "knocked back along the walk (" + moved + ")");
            Assert.Less(Mathf.Abs(moved.y), .6f, "on the walk's own level");
            Assert.AreEqual(health, session.Player.Health.Pool.Current, "no damage");
            // Thrown across the walk toward the open hall: the rail or the edge stops it; never off.
            session.Player.GetComponent<AdventurerMotor>().Teleport(on.position + Vector3.up); yield return null;
            float y = session.Player.transform.position.y;
            AditSwingLoad.KnockAlong(session.Player, sp.swing, 6);
            Assert.Less(Mathf.Abs(session.Player.transform.position.y - y), .6f, "never off a ledge");
        }

        /// <summary>(adit-final group run, 2026-10-15) Nix's death opens the chute door: the beast first, Nix leaping down, then Nix.</summary>
        [UnityTest] public IEnumerator Nixs_death_opens_the_chute_door()
        {
            yield return null;
            var door = session.Zone.Gates.Find(g => g != null && g.Title == "The chute door"); Assert.NotNull(door); Assert.AreEqual(EncounterSession.NixName, door.OpensOn); Assert.IsFalse(door.Open);
            var beast = session.Enemies.Find(e => e != null && e.Camp && e.MobName == EncounterSession.RockEaterName); Assert.NotNull(beast, "the Rock-Eater");
            var nix = session.Enemies.Find(e => e != null && e.Camp && e.MobName == EncounterSession.NixName); Assert.NotNull(nix, "Nix");
            beast.actor.Health.ApplyDamage(beast.actor.Health.Pool.Max * 3); yield return null; yield return null;
            Assert.IsFalse(door.Open, "not on the beast's death");
            Assert.IsTrue(nix.actor.IsAlive); nix.actor.Health.ApplyDamage(nix.actor.Health.Pool.Max * 3); yield return null;
            Assert.IsTrue(door.Open, "open on Nix's");
        }

        /// <summary>
        /// (adit-final group run, 2026-10-15) A leader sent a long way through the caves gets there: woken at the yard after a wipe at Nix, the
        /// leader's path to his camp 880 m off came back partial (the navmesh's search gives up on a long way), it stood at the path's end and
        /// 45 s on left the camp as out of reach; Nix never fell, the chute door never opened. Now it walks the caves' own way leg by leg
        /// (CaveWay). Here: every camp before Nix's down, the party at the yard; within 300 s (game time, run at 4x) the leader is within 25 m
        /// of Nix's camp and Nix's fight is still its step.
        /// </summary>
        [UnityTest] public IEnumerator A_leader_sent_from_the_yard_to_Nix_gets_there_and_does_not_give_him_up()
        {
            yield return null;
            var pop = SimPopulation.Active; var s = pop.World.sims.First();
            s.zone = "zone.adit"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = 11; session.Progress.experience = EncounterProgress.XpForLevel(9);
            var p = session.Player.transform.position; s.x = p.x + 2; s.z = p.z; pop.Refresh(); yield return null;
            Assert.IsTrue(session.Invite(s.id, true)); var c = session.PartySim(s.id);
            Assert.IsNotNull(c.LeadDungeon("quick", out var why), why);
            int nixCamp = CampIndex("Nix and the Rock-Eater");
            float keepDelay = EncounterSession.ReleaseDelay; EncounterSession.ReleaseDelay = 1e5f;   // no relief shift walking in behind
            try
            {
                // Every mob but Nix's camp and the ones after him down, so the run's next fight is his.
                var after = new HashSet<int>(); bool seen = false;
                foreach (var st in c.Route) { if (st.camps.Contains(nixCamp)) seen = true; if (seen) foreach (int k in st.camps) after.Add(k); }
                foreach (var e in session.Enemies.ToArray()) if (e != null && e.Camp && e.actor.IsAlive && !after.Contains(e.CampIndex)) e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3);
                for (int i = 0; i < 30 && !c.Route.First().camps.Contains(nixCamp); i++) yield return null;
                Assert.IsTrue(c.Route.First().camps.Contains(nixCamp), "Nix's fight is the leader's step: " + c.LeadingName);
                var at = c.Route.First().at; var motor = session.Player.GetComponent<AdventurerMotor>();
                Time.timeScale = 4; float t = 0, closest = float.MaxValue; bool legged = false;
                while (t < 300 && Vector3.Distance(c.transform.position, at) > 25)
                {
                    // You keep up: a pace behind the leader.
                    var behind = c.transform.position - c.transform.forward * 2.5f;
                    if (Vector3.Distance(session.Player.transform.position, c.transform.position) > 6 && NavMesh.SamplePosition(behind, out var hit, 3, NavMesh.AllAreas)) motor.Teleport(hit.position + Vector3.up);
                    legged |= c.Legging; closest = Mathf.Min(closest, Vector3.Distance(c.transform.position, at));
                    Assert.IsTrue(c.Route.Count > 0 && c.Route.First().camps.Contains(nixCamp), "Nix's fight is not given up (at " + t.ToString("0") + " s, " + closest.ToString("0") + " m off at the closest)");
                    t += Time.deltaTime; yield return null;
                }
                Assert.LessOrEqual(Vector3.Distance(c.transform.position, at), 25, "the leader reached Nix's camp (" + closest.ToString("0") + " m off at the closest; walked the caves' way: " + legged + ")");
            }
            finally { Time.timeScale = 1; EncounterSession.ReleaseDelay = keepDelay; }
        }

        [UnityTest] public IEnumerator The_lichen_runs_along_the_way_on()
        {
            yield return null;
            var trails = session.Zone.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.gameObject.name.EndsWith(" lichen")).ToList();
            Assert.GreaterOrEqual(trails.Count, 4, "a trail in the dark drift, on the falls stair, through the Breach, up the cloister stair and the shaft stair: " + string.Join(", ", trails.Select(t => t.name)));
            Assert.IsTrue(trails.Exists(t => t.name == "The Sealed Adit lichen"), "in the dark drift, where no lamp is");
            foreach (var t in trails) Assert.IsNull(t.GetComponent<Collider>(), t.name + " is nothing to trip on");
        }
    }
}
#endif
