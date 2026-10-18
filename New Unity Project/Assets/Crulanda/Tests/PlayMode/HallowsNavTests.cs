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
    /// Hallow's Creek built and walked (Docs/DUNGEON2_DESIGN.md; the Adit's tests' patterns): the zone builds on open ground with its decks,
    /// gates and anchors (N1, N2), the slabs carry the navmesh across the chasm and nothing reaches its floor (N3), every camp's mobs can be
    /// walked to from the yard with the gates open, a fall into the fog comes back (N3), a hungry shadow slows (N4), the Warden's cell opens
    /// on the Registrar's death and she unbinds the ward-lock at the tower, the wagon takes you home once the Picker-Master is down (N7), and a
    /// sim leads the run: road, bridge, street, Archive, tower, waiting only at the ward-lock.
    /// </summary>
    public class HallowsNavTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-hallows-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.hallows";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null; EncounterSession.ReleaseDelay = 3; EncounterSession.RideSeconds = 5;
            EncounterSession.EventSpeed = 1; EncounterSession.SnapWarnSeconds = 3; EncounterSession.WhisperEvery = 22; EncounterSession.WhisperLineEvery = 75; Time.timeScale = 1;
            EncounterSession.SquallWarnSeconds = 10; EncounterSession.SquallSeconds = 20; EncounterSession.SquallFirst = 75;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static Vector3 On(Vector3 p, string what, float reach = 3) { Assert.IsTrue(NavMesh.SamplePosition(p, out var hit, reach, NavMesh.AllAreas), what + " is on the navmesh (" + p + ")"); return hit.position; }

        [UnityTest, Timeout(300000)] public IEnumerator It_builds_on_open_ground_and_the_slabs_carry_the_way_over_the_chasm()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.hallows", zone.Zone.id); Assert.IsTrue(zone.Zone.dungeon);
            yield return Frames(10);
            Assert.Greater(zone.DeckSpots.Count(d => d.cave == null), 40, "the open-ground decks");
            foreach (var a in new[] { "the gatehouse", "the Warden's cell", "the tower door", "the belfry stair-head", "the wagon", "the Cut-Store" }) Assert.IsTrue(zone.Anchors.ContainsKey(a), "the anchor " + a);
            Assert.AreEqual(18, zone.Anchors["the belfry stair-head"].y - zone.Datum, .5f, "the belfry stair-head is 18 m up");
            Assert.AreEqual(2, zone.Anchors["the Warden's cell"].y - zone.Datum, .4f, "the Warden's cell is on the Archive's floor");
            foreach (var t in new[] { "Tredway's gate", EncounterSession.WardenCell, EncounterSession.WardGate, "The yard bar", "The dark cellar door" }) Assert.IsTrue(zone.Gates.Exists(g => g.Title == t), "the gate " + t);
            Assert.Greater(zone.PausedSpots.Count, 15, "the paused townsfolk");
            // The chasm: its floor is forty metres down and out of the navmesh; the slabs carry the way from the lip to the gatehouse.
            var pit = zone.Zone.pits[0]; Assert.Less(zone.HeightAt(pit.center.x + 40, pit.center.y), zone.Datum - 30, "the chasm is deep");
            Assert.IsFalse(NavMesh.SamplePosition(new Vector3(pit.center.x + 40, zone.Datum - pit.depth, pit.center.y), out _, 3, NavMesh.AllAreas), "no navmesh at the chasm's floor");
            // The parapets (hallows-3, 2026-10-16: people fell off the stair of small slabs and the run bot was warped eight times round the
            // slabs): a wall along every open edge of the bridge's decks, solid and cut out of the navmesh, broken down to its kerb only at
            // the jump off the third slab; no agent's navmesh comes to an open edge.
            Assert.Greater(zone.Parapets.Count, 40, "the parapets along the slabs, the ramps and the stair of small slabs");
            var broken = zone.Parapets.Where(r => r.broken).ToList(); Assert.AreEqual(1, broken.Count, "one broken stretch: the jump"); Assert.AreEqual("The third slab", broken[0].deck);
            foreach (var name in new[] { "The first stair", "The second stair", "The third stair", "The first step-stone", "The second step-stone", "The Mason's slab", "The far Table" })
                Assert.IsTrue(zone.Parapets.Exists(r => r.deck == name && !r.broken), name + " has a parapet");
            var parapets = GameObject.Find("The bridge's parapets"); Assert.NotNull(parapets, "the parapets are built");
            Assert.AreEqual(zone.Parapets.Count, parapets.GetComponentsInChildren<NavBlocker>().Length, "each parapet solid and a nav blocker");
            var near = new List<string>();
            foreach (var r in zone.Parapets)
            {
                if (Vector3.Distance(r.a, r.b) < 2) continue;
                var mid = (r.a + r.b) / 2 + Vector3.up * .1f;
                if (NavMesh.SamplePosition(mid, out var hit, .45f, NavMesh.AllAreas)) near.Add(r.deck + " at " + mid.ToString("0.0") + ": navmesh at " + hit.position.ToString("0.0"));
            }
            Assert.IsEmpty(near, "navmesh at an open edge of the bridge: " + string.Join("; ", near));
            zone.OpenGates(); yield return Frames(60);
            var yard = On(zone.Ground(zone.Zone.spawns.player, .2f), "the yard");
            var gatehouse = On(zone.Anchors["the gatehouse"] + Vector3.up * .2f, "the gatehouse");
            float over = NavReach.Length(yard, gatehouse); Assert.Less(over, 400, "over the slabs from the yard to the gatehouse (" + over.ToString("0") + " m)");
            // The lower orbit: down the stair of small slabs from the fifth slab and back up it. The jump off the third slab is a player's
            // (the low slab runs under its lip), not a way the paths take: as a navmesh link over a six-metre gap the leader took it and the
            // run bot walked off after it into the fog for four minutes (hallows-1, 2026-10-16).
            Assert.IsFalse(zone.Drops.Exists(d => Vector2.Distance(new Vector2(d.from.x, d.from.z), new Vector2(-6.2f, -179.36f)) < 3), "no navmesh link off the third slab");
            var third = zone.DeckSpots.Find(d => d.def != null && d.def.name == "The third slab"); var mason = zone.DeckSpots.Find(d => d.def != null && d.def.name == "The Mason's slab");
            var top = On(third.mid[third.mid.Length / 2] + Vector3.up * .2f, "the third slab"); var low = On(mason.mid[mason.mid.Length / 2] + Vector3.up * .2f, "the Mason's slab");
            Assert.Less(NavReach.Length(top, low), 90, "down to the Mason by the stair of small slabs"); Assert.Less(NavReach.Length(low, top), 120, "and back up the stair of small slabs");
            // The belfry by the tower's flights, and the Archive's floor by its steps.
            Assert.Less(NavReach.Length(On(zone.Anchors["the tower door"] + Vector3.up * .2f, "the tower door"), On(zone.Anchors["the belfry stair-head"] + Vector3.up * .2f, "the belfry stair-head")), 120, "up the tower to the belfry");
            Assert.Less(NavReach.Length(On(zone.Anchors["the Archive's steps"] + Vector3.up * .2f, "the Archive's steps"), On(zone.Anchors["the Warden's cell"] + Vector3.up * .2f, "the Warden's cell")), 120, "into the Archive and to the Warden's cell");
            Assert.Less(NavReach.Length(yard, On(zone.Anchors["the Cut-Store"] + Vector3.up * .2f, "the Cut-Store", 5)), 700, "down the Cellars to the Cut-Store");
        }

        [UnityTest, Timeout(300000)] public IEnumerator Every_camp_can_be_walked_to_from_the_yard()
        {
            var s = Session(); Assert.NotNull(s); Assert.AreEqual("zone.hallows", s.ZoneId);
            s.Zone.OpenGates(); yield return Frames(60);
            var start = On(s.Player.transform.position, "the yard", 4);
            var bad = new List<string>(); int n = 0;
            foreach (var e in s.Enemies)
            {
                if (e == null || !e.Camp || e.Game) continue; n++;
                if (!NavMesh.SamplePosition(e.transform.position, out var at, 4f, NavMesh.AllAreas)) { bad.Add(e.Name + " at " + e.transform.position.ToString("0.0") + ": off the mesh"); continue; }
                if (float.IsPositiveInfinity(NavReach.Length(start, at.position)) && float.IsPositiveInfinity(NavReach.Length(at.position, start))) bad.Add(e.Name + " at " + at.position.ToString("0.0") + ": no way there");
                if (n % 10 == 0) yield return null;
            }
            Assert.Greater(n, 80, "Hallow's Creek's camp mobs");
            Assert.IsEmpty(bad, "camp mobs no one can walk to:\n" + string.Join("\n", bad));
            // Every raised camp stands up on its deck (a slab, a loft, the belfry), not on the land under it.
            foreach (var cp in s.Zone.Zone.camps.Where(c => string.IsNullOrEmpty(c.cave) && Mathf.Abs(c.rise) > .01f && string.IsNullOrEmpty(c.after)))
            {
                int c = Array.IndexOf(s.Zone.Zone.camps, cp); var mine = s.Enemies.FindAll(e => e != null && e.CampIndex == c);
                if (mine.Count == 0) continue;   // (a rare not here this visit)
                float want = s.Zone.Datum + cp.rise;
                Assert.IsTrue(mine.Exists(e => Mathf.Abs(e.transform.position.y - 1 - want) < 1.2f), cp.name + " stands on its deck at " + want.ToString("0.0") + ": " + string.Join(", ", mine.Select(e => (e.transform.position.y - 1).ToString("0.0"))));
            }
        }

        [UnityTest] public IEnumerator A_fall_into_the_fog_comes_back_and_a_shadow_slows()
        {
            var s = Session(); var zone = s.Zone; yield return Frames(5);
            var motor = s.Player.GetComponent<AdventurerMotor>();
            var first = zone.DeckSpots.Find(d => d.def != null && d.def.name == "The near Table");
            motor.Teleport(first.mid[0] + Vector3.up * .3f); yield return Frames(20);
            var stood = s.Player.transform.position;
            var pit = zone.Zone.pits[0];
            motor.Teleport(new Vector3(pit.center.x + 30, zone.Datum + pit.fog - 3, pit.center.y)); yield return Frames(4);
            Assert.Greater(s.Player.transform.position.y, zone.Datum + pit.fog + 1, "the fog put you back");
            Assert.Less(Vector3.Distance(s.Player.transform.position, stood), 6, "where you last stood");
            var shadow = zone.Hazards[0]; var mid = (shadow.a + shadow.b) / 2;
            motor.Teleport(new Vector3(mid.x, shadow.floor + .3f, mid.y)); yield return Frames(10);
            Assert.Less(s.GroundSlow, 1, "the shadow slows you");
            motor.Teleport(zone.Ground(zone.Zone.spawns.player, .3f)); yield return Frames(10);
            Assert.AreEqual(1, s.GroundSlow, 1e-4f, "and out of it you are your own pace again");
        }

        [UnityTest, Timeout(300000)] public IEnumerator The_warden_walks_to_the_tower_and_unbinds_the_ward_lock()
        {
            var s = Session(); var zone = s.Zone; yield return Frames(5);
            var ward = zone.Gates.Find(g => g.Title == EncounterSession.WardGate); var cell = zone.Gates.Find(g => g.Title == EncounterSession.WardenCell);
            Assert.IsFalse(ward.Open, "the ward-lock shut"); Assert.IsFalse(cell.Open, "the Warden's cell shut");
            // The Registrar down: his death opens her cell, and she sets off.
            var pardew = s.Enemies.Find(e => e != null && e.MobName == "Registrar Dunstan Pardew"); Assert.NotNull(pardew);
            foreach (var e in s.Enemies.ToArray()) if (e != null && e.Camp && e.actor.IsAlive && e.MobName != "Registrar Dunstan Pardew") e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 10);   // nobody to fight on her way
            yield return Frames(5);
            pardew.Receive(1000000, s.Player); yield return Frames(30);
            Assert.IsTrue(cell.Open, "the cell opens as the Registrar falls");
            Assert.NotNull(s.Warden, "the Warden walks"); Assert.AreEqual(EncounterSession.WalkEvent, s.EventRunning, "the walk's waves are coming");
            Assert.AreEqual(HallowsWarden.Stage.Walking, s.Warden.Now);
            Assert.AreSame(s.Warden.actor, s.PartyActor(s.Warden.actor.EntityId.Value), "she is one of the party for every mob (the waves come at her)");
            var motor = s.Player.GetComponent<AdventurerMotor>();
            // She waits for the party: with you far off she does not walk on.
            motor.Teleport(zone.Ground(zone.Zone.spawns.player, .3f)); yield return Frames(30);
            Assert.IsTrue(s.Warden.Holding, "she waits while none of the party is near");
            // Struck down, she falls, the walk's waves go back, and a little later she is in her cell to be walked again.
            HallowsWarden.BackAfter = 1;
            s.Warden.actor.Health.ApplyDamage(s.Warden.actor.Health.Pool.Max * 10); yield return Frames(5);
            Assert.AreEqual(HallowsWarden.Stage.Fallen, s.Warden.Now, "she can fall"); Assert.IsNull(s.EventRunning, "the walk falls with her");
            for (float w = 0; w < 5 && s.Warden.Now != HallowsWarden.Stage.Waiting; w += Time.deltaTime) yield return null;
            Assert.AreEqual(HallowsWarden.Stage.Waiting, s.Warden.Now, "back in her cell"); Assert.IsTrue(s.Warden.actor.IsAlive);
            var talk = zone.Interactables.Find(i => i.kind == "warden"); Assert.NotNull(talk);
            s.UseInteractable(talk); yield return Frames(3);
            Assert.AreEqual(HallowsWarden.Stage.Walking, s.Warden.Now, "spoken to, she walks again"); Assert.AreEqual(EncounterSession.WalkEvent, s.EventRunning);
            // Kept company, she gets to the tower's door.
            for (float w = 0; w < 150 && !s.Warden.Arrived; w += Time.deltaTime)
            {
                Time.timeScale = 4;
                if (Vector3.Distance(s.Player.transform.position, s.Warden.transform.position) > 6) motor.Teleport(s.Warden.transform.position + Vector3.up * .2f - s.Warden.transform.forward * 2);
                yield return null;
            }
            Time.timeScale = 1; HallowsWarden.BackAfter = 30;
            Assert.IsTrue(s.Warden.Arrived, "she gets to the tower's door");
            Assert.IsTrue(ward.Open, "and the ward-lock is unbound"); Assert.IsTrue(s.Progress.keys.Contains(EncounterSession.WardKey), "for good");
        }

        /// <summary>The Pact-Captain holds the tower's second landing (DUNGEON2_DESIGN 2.7): a floor at +9 walked to from the tower's door up the
        /// stairs round the walls, she stands on it, and the belfry is above it.</summary>
        [UnityTest, Timeout(300000)] public IEnumerator The_pact_captains_landing_is_reached_from_the_towers_door()
        {
            var s = Session(); var zone = s.Zone; zone.OpenGates(); yield return Frames(60);
            var landing = zone.DeckSpots.Find(d => d.def != null && d.def.name == "The second landing"); Assert.NotNull(landing, "the second landing's floor");
            Assert.AreEqual("floor", landing.def.style); Assert.GreaterOrEqual(landing.def.width, 7.5f, "a floor to fight on, not a corner of the stair");
            Assert.AreEqual(9, zone.Anchors["the second landing"].y - zone.Datum, .4f, "at +9");
            var door = On(zone.Anchors["the tower door"] + Vector3.up * .2f, "the tower door"); var up = On(zone.Anchors["the second landing"] + Vector3.up * .2f, "the second landing");
            Assert.AreEqual(9, up.y - zone.Datum, .6f, "the navmesh is on the landing, not the ground under it");
            float walk = NavReach.Length(door, up); Assert.Less(walk, 80, "up the stairs from the door to the landing (" + walk.ToString("0") + " m)");
            Assert.Less(NavReach.Length(up, On(zone.Anchors["the belfry stair-head"] + Vector3.up * .2f, "the belfry stair-head")), 80, "and on up to the belfry");
            var kersey = s.Enemies.Find(e => e != null && e.MobName == "Pact-Captain Rowena Kersey"); Assert.NotNull(kersey);
            Assert.AreEqual(9, kersey.transform.position.y - 1 - zone.Datum, 1.2f, "the Pact-Captain stands on the landing");
            var move = EliteMoves.For(kersey.MobName, false); Assert.AreEqual("root", move.cc); Assert.AreEqual("Form the line", move.ccName); Assert.Greater(move.ccReach, 0, "on everyone near her");
        }

        /// <summary>The bars (sections 3 and 5): the gatehouse's sally-port lifts from the town side only; the Cellars' far trapdoor lifts from
        /// below only and bars again behind the party once all are up on the Square.</summary>
        [UnityTest] public IEnumerator The_bars_lift_from_their_own_side_and_the_trapdoor_bars_behind()
        {
            var s = Session(); var zone = s.Zone; yield return Frames(30);
            var motor = s.Player.GetComponent<AdventurerMotor>();
            foreach (var e in s.Enemies.ToArray()) if (e != null && e.Camp && e.actor.IsAlive) e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 10);   // nobody in the way
            yield return Frames(5);
            IEnumerator Beside(ZoneGate g, float side) { motor.Teleport(On(g.transform.position + g.transform.forward * side + Vector3.up * .3f, g.Title + "'s side", 4) + Vector3.up * .3f); yield return Frames(5); }
            var sally = zone.Gates.Find(g => g.Title == "The sally-port bar"); Assert.NotNull(sally, "the gatehouse's town-side bar");
            Assert.IsTrue(sally.FromFar); var sallyUse = zone.Interactables.Find(i => i.gate == sally); Assert.NotNull(sallyUse);
            yield return Beside(sally, -2.5f); s.UseInteractable(sallyUse); yield return Frames(2);
            Assert.IsFalse(sally.Open, "barred from the town side: not lifted from inside the gatehouse");
            yield return Beside(sally, 2.5f); s.UseInteractable(sallyUse); yield return Frames(2);
            Assert.IsTrue(sally.Open, "lifted from Main Street");
            var trap = zone.Gates.Find(g => g.Title == "The well-house trapdoor"); Assert.NotNull(trap, "the Cellars' far trapdoor");
            Assert.IsTrue(trap.FromFar && trap.ShutsBehind); var trapUse = zone.Interactables.Find(i => i.gate == trap); Assert.NotNull(trapUse);
            yield return Beside(trap, -4); s.UseInteractable(trapUse); yield return Frames(2);
            Assert.IsFalse(trap.Open, "never come into from the Square");
            yield return Beside(trap, 2.5f); s.UseInteractable(trapUse); yield return Frames(2);
            Assert.IsTrue(trap.Open, "lifted from below");
            motor.Teleport(zone.Anchors["the well-house"] + Vector3.up * .3f); yield return Frames(10);
            Assert.IsFalse(trap.Open, "and it bars again behind you: the Cellars are never walked back");
        }

        [UnityTest] public IEnumerator The_wagon_takes_you_home_once_the_picker_master_is_down()
        {
            var s = Session(); var zone = s.Zone; yield return Frames(5); EncounterSession.RideSeconds = .2f;
            var wagon = zone.Interactables.Find(i => i.kind == "ride"); Assert.NotNull(wagon, "the black-iron wagon");
            StringAssert.Contains("Picker-Master", s.InteractPromptFor(wagon), "his, while he stands");
            var yelland = s.Enemies.Find(e => e != null && e.MobName == "Picker-Master Fulke Yelland"); Assert.NotNull(yelland);
            yelland.Receive(1000000, s.Player); yield return Frames(10);
            // (The yard's teamsters stand at the wagon, and the ride is refused in a fight: the yard is cleared first, as a party would.)
            foreach (var e in s.Enemies.ToArray()) if (e != null && e.Camp && e.actor.IsAlive && Vector3.Distance(e.transform.position, wagon.position) < 45) e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 10);
            yield return Frames(5);
            foreach (var e in s.Enemies.ToArray()) if (e != null && e.actor.IsAlive) e.ResetFight();
            yield return Frames(10);
            s.Player.GetComponent<AdventurerMotor>().Teleport(wagon.position + Vector3.up * .3f + Vector3.right * 1.5f); yield return Frames(5);
            s.UseInteractable(wagon);
            for (float w = 0; w < 10 && Vector2.Distance(new Vector2(s.Player.transform.position.x, s.Player.transform.position.z), zone.Zone.ride.arrive) > 5; w += Time.deltaTime) yield return null;
            Assert.Less(Vector2.Distance(new Vector2(s.Player.transform.position.x, s.Player.transform.position.z), zone.Zone.ride.arrive), 5, "at the milestone");
        }

        /// <summary>
        /// The town as the books write it (REWORK_LOWTOWNS_HALLOWS picks 1, 2, 3 and 12): white picket fences and quartz roses built, no smoke
        /// from a chimney; the bubble of still air over Main Street (no wind, the fog further off, no music) and not over the yard; the Square's
        /// obsidian bowl (never under the land, fifty feet deep), its mirror and its probe, the red door and its knocker, the debris orbiting; the
        /// chalk, the willow, the chapel's glass; and the named people looked at with E.
        /// </summary>
        [UnityTest, Timeout(300000)] public IEnumerator The_town_is_white_and_still_and_the_square_is_the_books()
        {
            var s = Session(); var zone = s.Zone; yield return Frames(10);
            var motor = s.Player.GetComponent<AdventurerMotor>();
            // (By the objects' names: the static scenery's batching swaps their meshes.)
            var all = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            Assert.Greater(all.Count(t => t.name == "Picket fence"), 20, "white picket fences");
            Assert.Greater(all.Count(t => t.name == "Quartz roses"), 20, "red-quartz roses at the houses");
            foreach (var n in new[] { "The red door", "The knocker", "The floating marble", "The debris field", "The obsidian bowl", "The mirror floor", "The still reflection", "The chalked equations", "The weeping willow", "The chapel's glass, in the air" })
                Assert.NotNull(GameObject.Find(n), n + " is built");
            Assert.NotNull(GameObject.Find("The debris field").GetComponent<Spinner>(), "the debris orbits");
            Assert.NotNull(GameObject.Find("The still reflection").GetComponent<ReflectionProbe>(), "the mirror's reflection, rendered once");
            // The bowl: never under the land, and deep (fifty feet, b1 ch.21:134).
            var bowl = GameObject.Find("The obsidian bowl").GetComponent<MeshFilter>(); float low = float.MaxValue; var under = new List<string>();
            foreach (var v in bowl.sharedMesh.vertices)
            {
                var w = bowl.transform.TransformPoint(v); low = Mathf.Min(low, w.y);
                if (w.y < zone.HeightAt(w.x, w.z) - .02f) under.Add(w.ToString("0.0"));
            }
            var mirror = GameObject.Find("The mirror floor").GetComponent<MeshFilter>(); foreach (var v in mirror.sharedMesh.vertices) low = Mathf.Min(low, mirror.transform.TransformPoint(v).y);
            Assert.IsEmpty(under, "the land shows through the glass at " + string.Join(", ", under.Take(8)));
            Assert.Less(low, zone.Datum - 13, "the bowl is fifty feet deep");
            // The bubble: still on Main Street, the fog further off, no music; the yard outside it.
            yield return Frames(30);
            motor.Teleport(zone.Ground(new Vector2(0, -60), .3f)); yield return Frames(30);
            Assert.AreEqual(1, WorldWeather.Still, .01f, "no wind on Main Street");
            Assert.AreEqual(ZoneMusic.Silence, ZoneMusic.TracksFor(zone.Zone, s.Player.transform.position, 0, out _), "no music on Main Street");
            float inside = RenderSettings.fogEndDistance;
            motor.Teleport(zone.Ground(zone.Zone.spawns.player, .3f)); yield return Frames(30);
            Assert.AreEqual(0, WorldWeather.Still, .01f, "the yard is outside the bubble");
            Assert.Greater(inside, RenderSettings.fogEndDistance * 1.3f, "the fog stands further off inside it");
            // The people, looked at with E.
            foreach (var n in new[] { EncounterSession.GableName, "Mrs Miller", "Talla", "The crater's rim", "The chalked equations", "The ancient town well", "The chapel's glass" })
                Assert.IsTrue(zone.Interactables.Exists(i => i.name == n && !string.IsNullOrEmpty(i.text)), n + " is looked at with E");
            s.UseInteractable(zone.Interactables.Find(i => i.name == "Talla")); yield return null;
            Assert.IsTrue(s.Messages.Exists(m => m.Contains("TALLA")), "the stone girl is Talla");
            Assert.IsTrue(s.PausedFolk.Any(f => f != null && f.name == EncounterSession.GableName), "Mr Gable stands in his doorway");
        }

        /// <summary>
        /// The bakery's Glitch (b1 ch.21:82-112, 370-454): Mr Gable wakes and holds out his pie; taken ("Take it. Break the loop."), the loop's
        /// snap leaves him at rest for good: stone, never woken again, kept in the progress.
        /// </summary>
        [UnityTest, Timeout(300000)] public IEnumerator Mr_gable_sells_his_pie_in_the_glitch_and_rests_when_it_snaps()
        {
            var s = Session(); var zone = s.Zone; yield return Frames(30);
            Assert.NotNull(s.Gable, "Mr Gable"); Assert.NotNull(s.Pie, "his pie");
            Assert.Less(Time.time, s.Pie.hiddenUntil, "no pie to take while he is stone");
            var motor = s.Player.GetComponent<AdventurerMotor>();
            motor.Teleport(zone.Ground(new Vector2(-6.5f, -76.5f), .3f)); yield return Frames(10);
            Assert.IsTrue(s.StartEvent(EncounterSession.GlitchEvent)); yield return Frames(5);
            Assert.IsTrue(s.TownAwake && s.Gable.Awake, "the Glitch wakes him");
            Assert.GreaterOrEqual(Time.time, s.Pie.hiddenUntil, "the pie is held out");
            s.UseInteractable(s.Pie); yield return null;
            Assert.IsTrue(s.Messages.Exists(m => m.Contains("Break the loop")), "Take it. Break the loop.");
            Assert.IsFalse(s.GableAtRest, "not until the loop snaps");
            // The loop runs out fast; the Hollow up the trapdoor are put down as they come.
            EncounterSession.EventSpeed = 30; EncounterSession.SnapWarnSeconds = .3f;
            for (float w = 0; w < 20 && s.TownAwake; w += Time.deltaTime)
            {
                foreach (var e in s.Enemies.ToArray()) if (e != null && e.Camp && e.actor.IsAlive && Vector3.Distance(e.transform.position, s.Player.transform.position) < 40) e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 10);
                yield return null;
            }
            EncounterSession.EventSpeed = 1; EncounterSession.SnapWarnSeconds = 3;
            Assert.IsFalse(s.TownAwake, "the loop snapped");
            Assert.IsTrue(s.GableAtRest, "Mr Gable is at rest"); Assert.IsTrue(s.Progress.keys.Contains(EncounterSession.GableKey), "for good");
            yield return Frames(3);
            Assert.IsTrue(s.Gable.Stone, "stone"); s.Gable.Wake(); Assert.IsFalse(s.Gable.Awake, "and no Glitch wakes him again");
            Assert.Less(Time.time, s.Pie.hiddenUntil, "no pie now");
        }

        /// <summary>
        /// Pick 6, the Hollow as chapter 4 shows them (HollowRules): a Hollow camp pulled in the Cellars brings one more up a cellar trapdoor
        /// behind the party (ch.4:161), standing on the cellar floor; a Hollow cut down beside the anchor-well lies a moment and stands up again
        /// (ch.4:191-197), and broken, the well raises no more; a Hollow cut down elsewhere may split, its half crawling on low and slow (ch.4:181).
        /// </summary>
        [UnityTest, Timeout(300000)] public IEnumerator The_hollow_come_up_the_trapdoors_rise_at_the_well_and_crawl_on_in_halves()
        {
            var s = Session(); var zone = s.Zone; zone.OpenGates(); yield return Frames(30);
            EncounterSession.ReleaseDelay = .1f;
            var camps = zone.Zone.camps;
            int pulled = Array.FindIndex(camps, c => c.name == "Hollow in the long cellar"), door = Array.FindIndex(camps, c => c.name == "Hollow up the cellar trapdoor");
            Assert.GreaterOrEqual(pulled, 0); Assert.GreaterOrEqual(door, 0);
            Assert.IsFalse(s.Enemies.Exists(e => e != null && e.CampIndex == door), "nobody stands at the trapdoor before the pull");
            var motor = s.Player.GetComponent<AdventurerMotor>();
            var first = s.Enemies.Find(e => e != null && e.CampIndex == pulled && e.actor.IsAlive); Assert.NotNull(first);
            Assert.IsTrue(first.BookHollow, "a Hallow's Creek Hollow fights as chapter 4 shows them");
            motor.Teleport(first.transform.position + Vector3.up * .2f - first.transform.forward * 2); yield return Frames(5);
            first.threat.Add(s.Player.EntityId.Value, 50);
            for (float w = 0; w < 3 && !s.Enemies.Exists(e => e != null && e.CampIndex == door); w += Time.deltaTime) yield return null;
            var up = s.Enemies.FindAll(e => e != null && e.CampIndex == door);
            Assert.AreEqual(1, up.Count, "one comes up the trapdoor when its camp is pulled");
            Assert.IsTrue(NavMesh.SamplePosition(up[0].transform.position, out var at, 2.5f, NavMesh.AllAreas), "on the cellar floor");
            Assert.IsTrue(Crulanda.World.Hollow.InsideAny(at.position, 1), "inside the Cellars, not on the street above");
            Assert.IsNotNull(GameObject.Find("Cellar trapdoor (Hollow up the cellar trapdoor)"), "its trapdoor stands thrown open");
            foreach (var e in s.Enemies.ToArray()) if (e != null && e.Camp && e.actor.IsAlive) e.ResetFight();
            yield return Frames(5);

            // The anchor-well: whole, pulsing, under the well-house end of the Cellars.
            Assert.NotNull(s.AnchorWell, "the anchor-well"); Assert.IsTrue(s.WellWhole);
            int stair = Array.FindIndex(camps, c => c.name == "Hollow at the far stair");
            var near = s.Enemies.Find(e => e != null && e.CampIndex == stair && e.actor.IsAlive); Assert.NotNull(near);
            Assert.Less(s.DistanceToWell(near.transform.position), HollowRules.WellReach, "the far stair's Hollow are within its reach");
            HollowRules.Dice = () => .99f;
            try
            {
                motor.Teleport(near.transform.position + Vector3.up * .2f - near.transform.forward * 2); yield return Frames(5);
                near.threat.Add(s.Player.EntityId.Value, 50); yield return Frames(3);
                near.actor.Health.ApplyDamage(near.actor.Health.Pool.Max * 10); yield return null;
                Assert.IsTrue(near.RisingAtWell, "cut down beside the well, it lies a moment");
                // the player kept alive (the far stair's other Hollow may notice and fight): the run 2026-10-18 11:44 died here
                for (float w = 0; w < HollowRules.WellRiseSeconds + 2 && !near.actor.IsAlive; w += Time.deltaTime) { s.Player.Health.ApplyHealing(s.Player.Health.Pool.Max); yield return null; }
                Assert.IsTrue(near.actor.IsAlive, "and stands up again");
                Assert.LessOrEqual(near.actor.Health.Pool.Current, HollowRules.Share(near.actor.Health.Pool.Max, HollowRules.WellRiseHealth) + 1, "weaker");
                near.actor.Health.ApplyDamage(near.actor.Health.Pool.Max * 10); yield return null;
                Assert.IsFalse(near.actor.IsAlive || near.RisingAtWell, "only once: the second time it stays down");
                // Broken (the work done), the well raises no more.
                var other = s.Enemies.Find(e => e != null && e.CampIndex == stair && e.actor.IsAlive && e != near); Assert.NotNull(other);
                s.BreakWell(); Assert.IsFalse(s.WellWhole, "broken");
                Assert.Less(other.actor.Health.Pool.Current, other.actor.Health.Pool.Max, "the Hollow near it crumble");
                other.actor.Health.ApplyDamage(other.actor.Health.Pool.Max * 10); yield return null;
                Assert.IsFalse(other.RisingAtWell || other.actor.IsAlive, "the broken well raises nobody");

                // Elsewhere, the dice for a split: the half crawls on, low, slow and weak, and does not split again.
                HollowRules.Dice = () => 0;
                var cut = s.Enemies.Find(e => e != null && e.BookHollow && e.actor.IsAlive && s.DistanceToWell(e.transform.position) > HollowRules.WellReach); Assert.NotNull(cut);
                float hit = cut.HitBase;
                s.Player.Health.ApplyHealing(s.Player.Health.Pool.Max);
                // read at once: the split is in the death itself, before any frame can reset its fight
                cut.actor.Health.ApplyDamage(cut.actor.Health.Pool.Max * 10);
                Assert.IsTrue(cut.actor.IsAlive && cut.Half, "its half crawls on");
                Assert.LessOrEqual(cut.actor.Health.Pool.Current, HollowRules.Share(cut.actor.Health.Pool.Max, HollowRules.HalfHealth) + 1, "low");
                Assert.AreEqual(hit * HollowRules.HalfHit, cut.HitBase, 1e-3f, "its blow halved");
                // a fight given up does not make it whole again
                cut.ResetFight();
                Assert.LessOrEqual(cut.actor.Health.Pool.Current, HollowRules.Share(cut.actor.Health.Pool.Max, HollowRules.HalfHealth) + 1, "reset, still a half's health");
                cut.actor.Health.ApplyDamage(cut.actor.Health.Pool.Max * 10); yield return null;
                Assert.IsFalse(cut.actor.IsAlive, "a half does not split again");
            }
            finally { HollowRules.Dice = () => UnityEngine.Random.value; }
        }

        /// <summary>Pass close to a paused townsperson and it whispers "Help us" (b1 ch.21:38-40).</summary>
        [UnityTest] public IEnumerator A_paused_townsperson_whispers_help_us()
        {
            var s = Session(); var zone = s.Zone; yield return Frames(70);
            foreach (var e in s.Enemies.ToArray()) if (e != null && e.Camp && e.actor.IsAlive) e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 10);
            yield return Frames(5);
            EncounterSession.WhisperEvery = .1f; EncounterSession.WhisperLineEvery = 0;
            // One on Main Street's way (out of the gardens), and you a stride from it toward the middle of the street.
            var f = s.PausedFolk.First(x => x != null && x.Stone && !x.AtRest && Mathf.Abs(x.transform.position.x) < 6 && Mathf.Abs(x.transform.position.z + 40) < 40);
            var at = new Vector2(f.transform.position.x, f.transform.position.z); at.x += at.x > 0 ? -1.6f : 1.6f;
            s.Player.GetComponent<AdventurerMotor>().Teleport(zone.Ground(at, .3f));
            bool heard = false;
            for (float w = 0; w < 3 && !heard; w += Time.deltaTime) { heard = s.Floating.Exists(t => t.text == "Help us"); yield return null; }
            Assert.IsTrue(heard, "Help us, over the stone at your shoulder");
            Assert.IsTrue(s.Messages.Exists(m => m.Contains("Help us")), "and in the chat, the copper taste");
        }

        /// <summary>A sim leads the run (DungeonPlan on open ground): road, bridge, street, the Archive, the tower; Tredway's gate opens on the way,
        /// so the run waits only at the ward-lock; the standard run takes the Granary's bosses and the Reader, the full run the Cellars too.</summary>
        [UnityTest] public IEnumerator A_sim_leads_the_run_wing_by_wing()
        {
            yield return null; var session = Session(); var zone = ZoneBuilder.Active.Zone; var camps = zone.camps;
            var pop = SimPopulation.Active; var s = pop.World.sims.First();
            s.zone = "zone.hallows"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = 17; session.Progress.experience = EncounterProgress.XpForLevel(16);
            var p = session.Player.transform.position; s.x = p.x + 2; s.z = p.z; pop.Refresh(); yield return null;
            Assert.IsTrue(session.Invite(s.id, true)); var c = session.PartySim(s.id);
            Assert.IsNotNull(c.LeadDungeon("quick", out var why), why); Assert.AreEqual("Hallow's Creek", c.Dungeon);
            var wings = c.WingOrder.ToList(); Debug.Log("HallowsNavTests: the quick run's wings: " + string.Join(", ", wings) + "; " + c.CampsLeft + " camps");
            CollectionAssert.AreEqual(new[] { "The Ash Road", "The Bridge of Shattered Glass", "Main Street", "The Old Archive", "The Bell Tower" }, wings);
            Assert.AreEqual(EncounterSession.WardGate, c.StoppedBy, "the run waits at the ward-lock, and only there");
            var steps = c.Route.ToList();
            Assert.IsTrue(steps.Last(x => !x.regroup).camps.Any(i => camps[i].mob == EncounterSession.ExaminerName), "the Examiner last");
            Assert.AreEqual("Tredway's gate", steps.First(x => x.wing == "street").gate.Title, "Main Street behind Tredway's gate");
            int quick = c.CampsLeft;
            c.StopLeading(); Assert.IsNotNull(c.LeadDungeon(null, out why), why); Assert.Greater(c.CampsLeft, quick, "the standard run takes more");
            Assert.IsTrue(c.Route.Any(x => x.camps.Any(i => camps[i].mob == "Picker-Master Fulke Yelland")), "the Granary's Picker-Master");
            c.StopLeading(); Assert.IsNotNull(c.LeadDungeon("full", out why), why);
            Assert.IsTrue(c.Route.Any(x => x.camps.Any(i => camps[i].mob == "First-Cut")), "the Cellars in the full run");
            // The queue offers it at 15-20, and its door is where Oakhaven's road into it arrives.
            var d = session.QueueDungeons().Find(z => z.id == "zone.hallows"); Assert.NotNull(d, "the queue lists Hallow's Creek");
            Assert.IsTrue(EncounterSession.QueueFits(d, 17)); Assert.Less(Vector2.Distance(session.QueueDoor(d), zone.spawns.player), 6, "the door: the yard at the milestone");
        }

        /// <summary>
        /// The Ash Road's time-squall (REWORK_LOWTOWNS_HALLOWS pick 7; b1 ch.17:79-121): warned in words and a silver shimmer, then the
        /// sparks fall; out on the road they bite (never below a sixth of your health), under the rock overhang they do not; and it passes.
        /// The slab that tips (pick 10; ch.17:243): it dips under a foot, tips to forty-five once you are past, rights itself as you come
        /// back, and its walk keeps its collider and its navmesh the whole time.
        /// </summary>
        [UnityTest, Timeout(120000)] public IEnumerator A_time_squall_bites_in_the_open_not_under_the_overhang_and_the_fifth_slab_tips_behind_you()
        {
            var s = Session(); var zone = s.Zone; yield return Frames(10);
            var motor = s.Player.GetComponent<AdventurerMotor>();
            EncounterSession.SquallWarnSeconds = .5f; EncounterSession.SquallSeconds = 4;
            motor.Teleport(zone.Ground(new Vector2(0, -290), .3f)); yield return Frames(10);
            Assert.IsTrue(s.OnAshRoad(s.Player.transform.position), "the road at z -290 (clear of the camps) is the Ash Road");
            s.StartSquall(); Assert.AreEqual(EncounterSession.SquallState.Warned, s.Squall, "warned first");
            StringAssert.Contains("time-squall", string.Join(" ", s.Messages), "a warning line");
            yield return new WaitForSeconds(.8f);
            Assert.AreEqual(EncounterSession.SquallState.Falling, s.Squall, "then the sparks fall");
            float before = s.Player.Health.Pool.Current; yield return new WaitForSeconds(1.5f);
            Assert.Less((float)s.Player.Health.Pool.Current, before, "out on the road they bite");
            var cave = Hollow.All.Find(h => h.Name == EncounterSession.SquallShelter); Assert.NotNull(cave, "the overhang");
            var inside = cave.Centre[cave.Centre.Count / 2];
            motor.Teleport(inside + Vector3.up * .4f); yield return Frames(10);
            Assert.IsTrue(s.Sheltered(s.Player.transform.position), "under the overhang (" + s.Player.transform.position + ")");
            before = s.Player.Health.Pool.Current; yield return new WaitForSeconds(1.5f);
            Assert.GreaterOrEqual((float)s.Player.Health.Pool.Current, before, "and under the overhang they do not");
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(EncounterSession.SquallState.None, s.Squall, "and it passes");

            var slab = s.TipSlab; Assert.NotNull(slab, "the fifth slab tips");
            var spot = zone.DeckSpots.Find(d => d.def != null && d.def.name == "The fifth slab");
            var walk = spot.root.Find("Walk"); Assert.NotNull(walk.GetComponent<Collider>());
            motor.Teleport(spot.mid[spot.mid.Length / 2] + Vector3.up * .3f); yield return Frames(5); yield return new WaitForSeconds(.6f);
            Assert.IsTrue(slab.Holds(s.Player.transform.position), "standing on it");
            Assert.Greater(slab.Angle, 1, "it dips under you"); Assert.Less(slab.Angle, 8, "a few degrees, no more");
            // Away up the road (clear of every camp: by the gatehouse its guards could kill you, and a dead player keeps nothing level).
            motor.Teleport(zone.Ground(new Vector2(0, -290), .3f)); yield return new WaitForSeconds(3f);
            Assert.IsTrue(slab.IsTipped, "once you are past it tips"); Assert.Greater(slab.Angle, 40, "to forty-five degrees");
            Assert.IsTrue(walk.GetComponent<Collider>().enabled, "its walk stays solid");
            On(spot.mid[spot.mid.Length / 2], "the tipped slab's walk");
            var ramp = zone.DeckSpots.Find(d => d.def != null && d.def.name == "Ramp to the last slab");
            motor.Teleport(ramp.mid[ramp.mid.Length / 2] + Vector3.up * .3f); yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(s.Player.IsAlive, "alive to come back"); Assert.Less(slab.Angle, 6, "coming back, it rights itself");
        }
    }
}
#endif
