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
    /// Loads every registered zone and checks it holds together in play:
    /// - camps spawn on walkable ground;
    /// - every quest person and prop exists in the zone the quest points at;
    /// - arrival points from other zones are walkable;
    /// - the player starts on walkable ground.
    /// </summary>
    public class ZoneContentTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-zonecontent-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [UnityTest] public IEnumerator Every_zone_builds_with_its_camps_people_props_and_roads()
        {
            var first = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var zones = first.Zone.AllZones().Select(z => z.id).ToList();
            Assert.GreaterOrEqual(zones.Count, 4, "Oakhaven, Khaven, the Peaks and the Ashland Rim are registered.");
            var db = first.Quests.Db; var problems = new List<string>();
            var arrivals = new Dictionary<string, List<(string from, Vector2 at)>>();
            foreach (var z in first.Zone.AllZones()) foreach (var e in z.exits) { if (!arrivals.ContainsKey(e.to)) arrivals[e.to] = new List<(string, Vector2)>(); arrivals[e.to].Add((z.id, e.arrive)); }
            foreach (var id in zones)
            {
                ZoneBuilder.RequestedZoneId = id;
                yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
                for (int i = 0; i < 4; i++) yield return null;
                var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
                var z = s.Zone.Zone; string p = z.displayName + ": ";
                if (z.id != id) { problems.Add(p + "built " + z.id + " instead of " + id); continue; }
                // The ground wears its painted material (a lost assignment draws it magenta).
                var groundLook = s.Zone.GroundMesh != null ? s.Zone.GroundMesh.GetComponent<Renderer>() : null;
                if (groundLook == null || groundLook.sharedMaterial == null || groundLook.sharedMaterial != s.Zone.GroundMaterial || groundLook.sharedMaterial.mainTexture == null)
                    problems.Add(p + "the ground has no painted material");
                // Player start and arrivals are walkable.
                if (!NavMesh.SamplePosition(s.Zone.Ground(z.spawns.player), out _, 2.5f, NavMesh.AllAreas)) problems.Add(p + "player start is not walkable");
                if (arrivals.TryGetValue(id, out var ins))
                    foreach (var (from, at) in ins) if (!NavMesh.SamplePosition(s.Zone.Ground(at), out _, 2.5f, NavMesh.AllAreas)) problems.Add(p + "arrival from " + from + " at " + at + " is not walkable");
                foreach (var e in z.exits) if (!s.Zone.HasZone(e.to)) problems.Add(p + "exit to unknown zone " + e.to);
                // Every exit and camp can be walked to from the player's start: no wall, crag or relief cuts the zone in two (its gates
                // open: the Sealed Adit's are shut until their keys are found, AditGateTests).
                s.Zone.OpenGates(); for (int i = 0; i < 3; i++) yield return null;
                if (NavMesh.SamplePosition(s.Zone.Ground(z.spawns.player), out var start, 2.5f, NavMesh.AllAreas))
                {
                    // The places reached so far (hop starts) and those the single walk from the start missed.
                    var reached = new List<Vector3> { start.position };
                    var missed = new List<(string what, Vector3 at)>();
                    foreach (var e in z.exits)
                    {
                        if (!NavMesh.SamplePosition(s.Zone.Ground(e.at), out var to, Mathf.Max(2.5f, e.radius), NavMesh.AllAreas)) { problems.Add(p + "exit to " + e.to + " can't be walked to from the start (no navmesh there)"); continue; }
                        if (NavReach.Walkable(start.position, to.position)) reached.Add(to.position);
                        else missed.Add(("exit to " + e.to, to.position));
                    }
                    // Where the camp stands: on the land, or on a cave's floor under it, or up on its deck there (StandAt, as it spawns:
                    // the Adit rework's trestle, bridge tower and catwalks).
                    foreach (var camp in z.camps)
                    {
                        if (!NavMesh.SamplePosition(s.Zone.StandAt(camp.center, EncounterSession.DeckHeight(camp, camp.center)), out var to, Mathf.Max(2.5f, camp.radius), NavMesh.AllAreas)) { problems.Add(p + "camp '" + camp.name + "' can't be walked to from the start (no navmesh at its ground)"); continue; }
                        if (NavReach.Walkable(start.position, to.position)) reached.Add(to.position);
                        else missed.Add(("camp '" + camp.name + "'", to.position));
                    }
                    // One search from the start gives up part way on the Adit's long cave web (PathPartial) though parties reach every
                    // camp there in play. A missed place counts as reached when a chain of hops arrives: each hop a complete path from a
                    // place already reached (the start, a reached camp or exit, a cave breadcrumb the start reaches by complete hops), so
                    // every hop starts where the one before ended. Places nothing reaches stay problems: a genuinely cut-off place fails.
                    if (missed.Count > 0) foreach (var (what, at) in HopReach(start.position, reached, missed)) problems.Add(p + what + " can't be walked to from the start (its ground " + at + "; no chain of complete hops arrives)");
                }
                // Camps: most of each pack must find walkable ground; levels inside the zone's band.
                string shortId = id.Replace("zone.", "");
                for (int c = 0; c < z.camps.Length; c++)
                {
                    var camp = z.camps[c];
                    int spawned = s.Enemies.Count(e => e.Camp && e.persistentId.Contains("." + shortId + "." + c + "."));
                    if (camp.rare <= 0 && string.IsNullOrEmpty(camp.after) && spawned < Mathf.CeilToInt(camp.count * .75f)) problems.Add(p + "camp '" + camp.name + "' spawned " + spawned + "/" + camp.count);
                    if (camp.levelMin < z.levelMin - 1 || camp.levelMax > z.levelMax + (camp.harder ? 3 : 1)) problems.Add(p + "camp '" + camp.name + "' levels " + camp.levelMin + "-" + camp.levelMax + " outside " + z.levelMin + "-" + z.levelMax);
                }
                // Quest people and props that the zone is responsible for.
                var life = VillageLife.Active;
                foreach (var q in db.Ordered)
                {
                    string qz = string.IsNullOrEmpty(q.zone) ? null : q.zone;
                    // A bounty is posted on and paid at the zone's notice board; its courier spawns only when the rare
                    // posting is taken (BountyBoardTests covers that). The zone owes it a board and nothing else.
                    if (q.kind == "bounty") { if (qz == id && !s.Zone.Interactables.Exists(x => x.name == Bounties.BoardName)) problems.Add(p + q.id + " is posted on a notice board this zone has not got"); continue; }
                    if (qz == id)
                    {
                        if (q.giver != "auto" && q.giver != "Mira" && (life == null || life.Find(q.giver) == null)) problems.Add(p + q.id + " giver '" + q.giver + "' missing");
                    }
                    foreach (var step in q.steps)
                        foreach (var o in step.objectives)
                        {
                            string oz = !string.IsNullOrEmpty(o.zone) ? o.zone : qz;
                            if (oz != id) continue;
                            if ((o.type == "talk" || o.type == "deliver" || o.type == "bring") && o.target != "Mira" && s.ZoneOfPerson(o.target) == null) problems.Add(p + q.id + " talks to '" + o.target + "', who lives nowhere");
                            if ((o.type == "interact" || (o.type == "collect" && o.target != null && !o.target.StartsWith("kill:"))) && !s.Zone.Interactables.Exists(x => x.name == o.target))
                                problems.Add(p + q.id + " needs prop '" + o.target + "' (not in this zone)");
                            if (o.type == "kill" && !s.Enemies.Exists(e => QuestLog.Matches(o.target, e.persistentId))) problems.Add(p + q.id + " kills '" + o.target + "' but nothing here matches");
                            if (o.type == "collect" && o.target != null && o.target.StartsWith("kill:") && !s.Enemies.Exists(e => QuestLog.Matches(o.target.Substring(5), e.persistentId))) problems.Add(p + q.id + " loots from '" + o.target + "' but nothing here matches");
                            if (o.type == "visit" && !NavMesh.SamplePosition(s.Zone.Ground(o.at), out _, Mathf.Max(3, o.radius), NavMesh.AllAreas)) problems.Add(p + q.id + " visit point " + o.at + " unreachable");
                        }
                    // Whoever takes the quest back must live somewhere.
                    if (q.turnIn != "Mira" && s.ZoneOfPerson(q.turnIn) == null) problems.Add(q.id + " turn-in '" + q.turnIn + "' lives nowhere");
                }
                if (life != null) foreach (var v in life.Villagers) if (!NavMesh.SamplePosition(v.transform.position, out _, 2, NavMesh.AllAreas)) problems.Add(p + v.Name + " stands off the navmesh");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>The missed places no chain of complete hops reaches. The hop starts are the reached places, plus (underground) the
        /// cave and deck breadcrumbs the start reaches crumb to crumb by complete paths (NavReach.CaveCrumbs, the hop map of
        /// AditNavDiagnostics). A missed place is reached when a complete path, or legs each picking up where a partial one ended,
        /// joins it to a hop start; it then becomes a hop start itself, pass after pass until nothing changes.</summary>
        static List<(string what, Vector3 at)> HopReach(Vector3 start, List<Vector3> reached, List<(string what, Vector3 at)> missed)
        {
            var from = new List<Vector3>(reached);
            if (Hollow.All != null && Hollow.All.Count > 0)
            {
                var crumbs = NavReach.CaveCrumbs(start, out var reach);
                foreach (var i in reach) from.Add(crumbs[i]);
            }
            var left = new List<(string what, Vector3 at)>(missed);
            var path = new NavMeshPath(); bool changed = true;
            while (changed && left.Count > 0)
            {
                changed = false;
                for (int m = left.Count - 1; m >= 0; m--)
                {
                    var target = left[m].at;
                    // The nearest dozen hop starts: a short hop is the one the search finishes.
                    foreach (var a in from.OrderBy(v => (v - target).sqrMagnitude).Take(12).ToList())
                    {
                        if (!Legs(a, target, path)) continue;
                        from.Add(target); left.RemoveAt(m); changed = true; break;
                    }
                }
            }
            return left;
        }

        /// <summary>True when the navmesh joins <paramref name="a"/> to <paramref name="b"/>: a complete path, or partial legs each
        /// starting at the last corner of the one before and getting somewhere, the last of them complete.</summary>
        static bool Legs(Vector3 a, Vector3 b, NavMeshPath path)
        {
            for (int leg = 0; leg < 16; leg++)
            {
                if (!NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path) || path.corners.Length == 0) return false;
                if (path.status == NavMeshPathStatus.PathComplete) return true;
                var end = path.corners[path.corners.Length - 1];
                if ((end - a).sqrMagnitude < 1) return false;
                a = end;
            }
            return false;
        }
    }
}
#endif
