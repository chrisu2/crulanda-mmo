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
    /// The cave works of the Adit rework (Docs/ADIT_REWORK.md, stage S1), on a small network of test caves added to the Sealed Adit's zone
    /// in its empty south-east (ZoneBuilder.DefinitionFilter; GAME-ONLY test data, built nowhere else): a hall 35 m down; a branch opening
    /// 10 m up its west wall; a loop that leaves its floor and comes back over it as a lip 12 m up its east wall; a bend that comes back
    /// into it level; a timber trestle down its east wall from the lip to the floor, a stone platform on piers; a one-way drop off the
    /// lip into a pool sunk in the floor; a boss door with nothing to use; secrets placed by cave. Every mouth and join is a hole, each
    /// floor is whose it should be, the decks stand and are walked, the drop goes down and never up, the pool is wading-deep and the door
    /// cuts the way. The Adit's own caves are the reworked network (the Workings end in a lip 14 m up the Gallery's wall; the Gallery is
    /// floored everywhere).
    /// </summary>
    public class AditCaveBuildTests
    {
        const string Hub = "Fixture Hub", Ledge = "Fixture Ledge", Loop = "Fixture Loop", Bend = "Fixture Bend";
        string root; EncounterSession session; ZoneBuilder zone;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-cavebuild-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.DefinitionFilter = AddFixture;
            ZoneBuilder.RequestedZoneId = "zone.adit";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); zone = ZoneBuilder.Active;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            ZoneBuilder.DefinitionFilter = null;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        // ---------- the fixture ----------
        static float[] Rows(params float[][] rows) { var flat = new List<float>(); foreach (var r in rows) flat.AddRange(r); return flat.ToArray(); }
        static T[] With<T>(T[] list, params T[] more) { var all = new List<T>(list ?? new T[0]); all.AddRange(more); return all.ToArray(); }
        static ZoneProp Cavern(string name, Vector2 at, float yaw, float[] plan, string within = null, float rise = 0, string joins = null)
        {
            return new ZoneProp { kind = "cavern", name = name, at = at, rotation = yaw, variant = 2, plan = plan, within = within, rise = rise, joins = joins, style = "fixture" };
        }
        /// <summary>The test caves (the hall runs north from (250, -360); its rings start a metre behind that, so along s is z = -361 + s on
        /// its middle line, x = 250 + aside; the branches' mouths below are those points on its floor).</summary>
        static ZoneDefinition AddFixture(ZoneDefinition z)
        {
            if (z == null || z.id != "zone.adit") return z;
            var hub = Cavern(Hub, new Vector2(250, -360), 0, Rows(new[] { 0f, -1, 3, 4, 0 }, new[] { 0f, 4, 3, 4, 0 }, new[] { 0f, 20, 3, 4, -6 }, new[] { 0f, 40, 3, 4, -13 }, new[] { 0f, 60, 3, 4, -20 },
                new[] { 0f, 80, 3.2f, 4.2f, -27 }, new[] { 0f, 95, 4, 5, -33 }, new[] { 0f, 102, 10, 12, -35 }, new[] { 0f, 112, 20, 22, -35 }, new[] { 0f, 132, 22, 24, -35 },
                new[] { 0f, 152, 20, 22, -35 }, new[] { 0f, 162, 10, 12, -35 }, new[] { 0f, 168, .3f, .4f, -35 }));
            var ledge = Cavern(Ledge, new Vector2(238, -221), -90, Rows(new[] { 0f, -1, 3, 4, 0 }, new[] { 0f, 4, 3, 4, 0 }, new[] { 0f, 16, 3, 4, 0 }, new[] { 0f, 26, 3, 4, 0 }, new[] { 0f, 30, .3f, .4f, 0 }), Hub, 10);
            var loop = Cavern(Loop, new Vector2(258, -253), 90, Rows(new[] { 0f, -1, 3, 4, 0 }, new[] { 0f, 8, 3, 4, 0 }, new[] { 0f, 20, 3, 4, 1 }, new[] { -6f, 30, 3, 4, 2.5f }, new[] { -20f, 36, 3, 4, 4.5f },
                new[] { -34f, 34, 3, 4, 7 }, new[] { -42f, 26, 3, 4, 9.5f }, new[] { -44f, 18, 3, 4, 11.5f }, new[] { -44f, 10, 3.5f, 4.5f, 12 }, new[] { -44f, 2, 4, 5, 12 }, new[] { -44f, -6, 4, 5, 12 }), Hub, 0, Hub);
            var bend = Cavern(Bend, new Vector2(240, -245), -90, Rows(new[] { 0f, -1, 3, 4, 0 }, new[] { 0f, 8, 3, 4, 0 }, new[] { 0f, 16, 3, 4, 0 }, new[] { 5f, 24, 3, 4, 0 }, new[] { 12f, 26, 3, 4, 0 },
                new[] { 18f, 22, 3, 4, 0 }, new[] { 20f, 14, 3, 4, 0 }, new[] { 20f, 4, 3, 4, 0 }, new[] { 20f, -2, 3, 4, 0 }), Hub, 0, Hub);
            z.props = With(z.props, hub, ledge, loop, bend);
            z.decks = With(z.decks,
                new ZoneDeck { name = "Fixture trestle", cave = Hub, style = "timber", wall = 1, from = 152, to = 116, rise = 12, rise2 = 0, width = 3, canonStatus = "GAME-ONLY (a test's)" },
                new ZoneDeck { name = "Fixture platform", cave = Hub, style = "stone", from = 125, aside = 0, rise = 3, radius = 2, canonStatus = "GAME-ONLY (a test's)" });
            z.drops = With(z.drops, new ZoneDrop { name = "Fixture chute", fromCave = Loop, from = 90, toCave = Hub, to = 150, toAside = 10, width = 2 });
            z.caveWater = With(z.caveWater, new ZoneCaveWater { name = "Fixture pool", cave = Hub, from = 150, to = 150, aside = 10, radius = 3.5f, depth = 1 });
            z.gates = With(z.gates, new ZoneGateDef { title = "Fixture door", cave = Hub, along = 50, kind = "bossdoor", opensOn = "Fixture warden" });
            z.anchors = With(z.anchors, new ZoneAnchor { name = "fixture.pool", cave = Hub, along = 150, aside = 10 });
            z.secrets = With(z.secrets,
                new ZoneSecret { id = "secret.fixture.ledge", name = "A test bundle", kind = "cache", cave = Ledge, along = 20, xp = 1, canonStatus = "GAME-ONLY (a test's)" },
                new ZoneSecret { id = "secret.fixture.platform", name = "A test tin", kind = "cache", cave = Hub, along = 125, rise = 3, xp = 1, canonStatus = "GAME-ONLY (a test's)" });
            return z;
        }

        // ---------- helpers ----------
        static Hollow Cave(string name) { var h = Hollow.All.Find(o => o.Name == name); Assert.NotNull(h, name); return h; }
        static Vector2 Flat(Vector3 v) { return new Vector2(v.x, v.z); }
        static float PathLength(NavMeshPath path) { float l = 0; for (int i = 1; i < path.corners.Length; i++) l += Vector3.Distance(path.corners[i - 1], path.corners[i]); return l; }
        /// <summary>The caves' rock (their shells and knolls) a ray meets between two points: what the floors' furnishings are (a heap of
        /// rubble the way goes round) is not the wall.</summary>
        static List<string> RockBetween(Vector3 a, Vector3 b)
        {
            return Physics.RaycastAll(a, (b - a).normalized, Vector3.Distance(a, b), ~0, QueryTriggerInteraction.Ignore)
                .Where(x => x.collider.name.EndsWith(" shell") || x.collider.name.EndsWith(" knoll")).Select(x => x.collider.name + " at " + x.point).ToList();
        }
        static Vector3 OnMesh(Vector3 at, float reach, string what) { Assert.IsTrue(NavMesh.SamplePosition(at + Vector3.up * .2f, out var hit, reach, NavMesh.AllAreas), what + " is on the navmesh (" + at + ")"); return hit.position; }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        float HubFloor { get { return Cave(Hub).At(130).y; } }

        [UnityTest] public IEnumerator Mouths_and_joins_are_open_and_each_floor_is_whose_it_should_be()
        {
            yield return null;
            Hollow hub = Cave(Hub), ledge = Cave(Ledge), loop = Cave(Loop), bend = Cave(Bend); float floor = HubFloor;
            Assert.AreSame(hub, ledge.Parent); Assert.IsTrue(ledge.MouthRaised, "the branch opens 10 m up the wall");
            Assert.AreSame(hub, loop.Join); Assert.IsTrue(loop.JoinRaised, "the loop comes back as a lip"); Assert.AreEqual(12, loop.JoinRise, .3f);
            Assert.AreSame(hub, bend.Join); Assert.IsFalse(bend.JoinRaised, "the bend comes back level");
            // Holes: from in the hall up into the branch; from in the loop out over its lip; from in the bend back into the hall.
            Assert.IsEmpty(RockBetween(ledge.At(1) + Vector3.up * 1.4f, ledge.At(16) + Vector3.up * 1.4f), "the branch's mouth is a hole in the hall's wall");
            Assert.IsEmpty(RockBetween(loop.At(loop.Length - 22, 2.5f) + Vector3.up * 1.4f, loop.At(loop.Length - 4, 2.5f) + Vector3.up * 1.4f), "the loop opens over the hall");
            Assert.IsEmpty(RockBetween(bend.At(bend.Length - 20) + Vector3.up * 1.4f, bend.At(bend.Length - 1) + Vector3.up * 1.4f), "the bend opens into the hall");
            // Floors: no branch or loop floor out in the hall's air; theirs where they are their own; the hall's under both, and under the bend's end.
            Assert.IsFalse(ledge.FloorAt(Flat(ledge.At(1)), out _), "no branch floor in the hall's air");
            Assert.IsTrue(ledge.FloorAt(Flat(ledge.At(20)), out float ly), "the branch's own floor"); Assert.AreEqual(floor + 10, ly, .15f);
            Assert.AreEqual(floor, zone.StandAt(Flat(ledge.At(1)), float.NegativeInfinity).y, .15f, "under the branch's mouth: the hall's floor");
            Assert.IsTrue(loop.FloorAt(Flat(loop.At(90)), out float lip), "the lip is the loop's floor"); Assert.AreEqual(floor + 12, lip, .15f);
            Assert.IsFalse(loop.FloorAt(Flat(loop.At(loop.Length - 3)), out _), "past the lip, the loop has no floor");
            Assert.AreEqual(floor, zone.StandAt(Flat(loop.At(loop.Length - 3)), float.NegativeInfinity).y, .15f, "past the lip: the hall's floor, 12 m down");
            Assert.IsTrue(hub.FloorAt(Flat(bend.At(bend.Length - 1)), out _), "the hall keeps its floor where the bend comes in");
            Assert.IsFalse(bend.FloorAt(Flat(bend.At(bend.Length - 1)), out _), "the bend's floor gives way to the hall's");
            for (int i = 0; i < hub.Centre.Count; i++) Assert.IsTrue(hub.Floored(i), "the hall is floored everywhere (ring " + i + ", " + hub.Along[i] + " m in)");
            // Walked: from the bend's far leg round into the hall, level.
            var path = new NavMeshPath();
            var a = OnMesh(bend.At(30), 2, "the bend's far leg"); var b = OnMesh(hub.At(140), 2, "the hall");
            Assert.IsTrue(NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "walked from the bend into the hall");
            // The Adit's own caves: the reworked network (Docs/ADIT_REWORK.md).
            var main = Cave("The Sealed Adit"); var gallery = Cave("The Singing Gallery");
            Assert.AreSame(gallery, main.Join); Assert.IsTrue(main.JoinRaised, "the overlook"); Assert.IsFalse(main.MouthRaised);
            for (int i = 0; i < gallery.Centre.Count; i++) Assert.IsTrue(gallery.Floored(i), "the Gallery is floored everywhere (ring " + i + ")");
            Assert.IsTrue(Cave("The Cutting Drift").MouthRaised, "the Company door 12 m up the Gallery's wall"); Assert.IsTrue(Cave("The Geode Foundry").MouthRaised, "the chute lip");
        }
        [UnityTest] public IEnumerator Decks_stand_where_they_are_drawn_and_are_walked()
        {
            yield return null;
            Hollow hub = Cave(Hub); float floor = HubFloor;
            var trestle = zone.DeckSpots.Find(d => d.def.name == "Fixture trestle"); Assert.NotNull(trestle, "the trestle");
            var platform = zone.DeckSpots.Find(d => d.def.name == "Fixture platform"); Assert.NotNull(platform, "the platform"); Assert.IsTrue(platform.round);
            int N = trestle.mid.Length - 1;
            Assert.AreEqual(floor + 12, trestle.mid[0].y, .05f, "its head at the lip's height"); Assert.AreEqual(floor, trestle.mid[N].y, .05f, "its foot on the floor");
            Assert.AreEqual(floor + 3, platform.mid[0].y, .05f);
            int k = N / 3; var top = trestle.mid[k]; Assert.Greater(top.y - floor, 6, "a high stretch of it");
            Assert.AreEqual(top.y, zone.StandAt(Flat(top), top.y + .3f).y, .05f, "standing on it from above");
            Assert.AreEqual(floor, zone.StandAt(Flat(top), floor + .2f).y, .15f, "standing under it on the floor");
            var floors = new List<float>(); Assert.IsTrue(zone.FloorsAt(Flat(platform.mid[0]), floors)); Assert.AreEqual(2, floors.Count, "the floor and the platform over it");
            Assert.Less(Mathf.Abs(OnMesh(top, .6f, "the trestle's top").y - top.y), .3f, "the navmesh is on the deck, not under it");
            var path = new NavMeshPath(); var foot = OnMesh(trestle.mid[N], 1, "the trestle's foot"); var head = OnMesh(trestle.mid[1], 1, "the trestle's head");
            Assert.IsTrue(NavMesh.CalculatePath(foot, head, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "walked up the trestle");
            // A rail on its open side, none on the wall's.
            float s = Mathf.Lerp(152, 116, k / (float)N); var right = hub.At(s, 1) - hub.At(s, 0); right.y = 0;
            int open = Vector3.Dot(trestle.across[k], right) < 0 ? 1 : -1;
            bool Rail(int side, float reach) { return Physics.RaycastAll(top + Vector3.up, trestle.across[k] * side, reach, ~0, QueryTriggerInteraction.Ignore).Any(x => x.collider.GetComponentInParent<NavBlocker>() != null); }
            Assert.IsTrue(Rail(open, trestle.half + .5f), "a rail along its open side");
            Assert.IsFalse(Rail(-open, trestle.half + .3f), "none along the wall it hugs");
        }
        [UnityTest] public IEnumerator The_drop_goes_down_and_never_up()
        {
            yield return null;
            Hollow hub = Cave(Hub), loop = Cave(Loop);
            Assert.AreEqual(1, zone.Drops.Count(d => Vector3.Distance(d.from, loop.At(90)) < 8), "one way down off the loop's lip (the Adit's own, the chute and the falls, are elsewhere)");
            var lip = OnMesh(loop.At(90), 1.5f, "the lip"); var pool = OnMesh(zone.StandAt(Flat(hub.At(150, 10)), float.NegativeInfinity), 2, "the pool");
            var path = new NavMeshPath();
            Assert.IsTrue(NavMesh.CalculatePath(lip, pool, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "down off the lip");
            Assert.Less(PathLength(path), 25, "straight down the drop (" + PathLength(path) + " m)");
            Assert.IsTrue(NavMesh.CalculatePath(pool, lip, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "back up by the trestle");
            Assert.Greater(PathLength(path), 30, "not up the drop (" + PathLength(path) + " m)");
            Assert.IsTrue(NavMesh.CalculatePath(lip, pool, NavMesh.AllAreas & ~(1 << EncounterNavigation.DropArea), path) && path.status == NavMeshPathStatus.PathComplete, "a camp's mob walks round");
            Assert.Greater(PathLength(path), 30, "the long way, by the trestle (" + PathLength(path) + " m)");
        }
        [UnityTest] public IEnumerator The_pool_is_sunk_into_the_floor_and_wading_deep()
        {
            yield return null;
            Hollow hub = Cave(Hub); float floor = HubFloor; var c = hub.At(150, 10);
            float bed = zone.StandAt(Flat(c), float.NegativeInfinity).y; Assert.AreEqual(floor - 1.12f, bed, .15f, "its bed a metre under the water");
            Assert.IsTrue(zone.WaterAtPoint(new Vector3(c.x, bed + .5f, c.z), out float surface, out float depth), "in the pool");
            Assert.AreEqual(floor - .12f, surface, .05f, "its surface a hand under the floor"); Assert.That(depth, Is.InRange(.8f, 1.2f), "wading deep");
            Assert.IsFalse(zone.WaterAtPoint(new Vector3(c.x, floor + 10, c.z), out _, out _), "dry ten metres over it");
            var dry = hub.At(130); Assert.IsFalse(zone.WaterAtPoint(dry + Vector3.up * .5f, out _, out _), "dry on the hall's floor");
            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(c.x, bed + .2f, c.z), out var hit, 1, NavMesh.AllAreas), "its bed is walked");
            Assert.AreEqual(1 << EncounterNavigation.ShallowsArea, hit.mask, "as shallows: wading costs");
            Assert.IsTrue(zone.Anchors.ContainsKey("fixture.pool")); Assert.AreEqual(bed, zone.Anchors["fixture.pool"].y, .15f, "an anchor on the bed");
        }
        [UnityTest] public IEnumerator The_door_cuts_the_way_until_it_opens_and_secrets_stand_where_placed()
        {
            yield return null;
            Hollow hub = Cave(Hub), ledge = Cave(Ledge); float floor = HubFloor;
            var door = zone.Gates.Find(g => g != null && g.Title == "Fixture door"); Assert.NotNull(door, "the door from the data");
            Assert.AreEqual("bossdoor", door.Kind); Assert.AreEqual("Fixture warden", door.OpensOn); Assert.AreEqual(Hub, door.Cave); Assert.AreEqual(50, door.Along, .01f); Assert.IsFalse(door.Open);
            Assert.IsFalse(zone.Interactables.Exists(i => i.name == "Fixture door"), "nothing to use at a boss's door");
            var path = new NavMeshPath(); var a = OnMesh(hub.At(20), 2, "the drift"); var b = OnMesh(hub.At(130), 2, "the hall");
            Assert.IsFalse(NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "shut, it bars the way");
            door.SetOpen(true, true); yield return Frames(4);
            Assert.IsTrue(NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "open, the way is clear");
            var onLedge = zone.Secrets.Find(x => x.def.id == "secret.fixture.ledge"); Assert.NotNull(onLedge);
            Assert.AreEqual(floor + 10, onLedge.position.y, .3f, "on the branch's floor, not the land"); Assert.AreEqual(ledge.At(20).x, onLedge.position.x, .5f);
            var onPlatform = zone.Secrets.Find(x => x.def.id == "secret.fixture.platform"); Assert.NotNull(onPlatform);
            Assert.AreEqual(floor + 3, onPlatform.position.y, .15f, "on the platform, not the floor under it");
        }
    }
}
#endif
