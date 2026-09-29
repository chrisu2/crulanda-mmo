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
    /// Every road out of every zone works the way a player uses it: walk from well inside the zone into the exit (the real
    /// character controller against the real scenery, no teleport onto the spot), see the travel prompt, press E, and
    /// arrive in the linked zone standing on its ground, able to walk on. Names whatever blocks the way.
    /// </summary>
    public class ZoneExitTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-exits-" + Guid.NewGuid().ToString("N"));
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

        /// <summary>Walks the player (controller only, motor off) toward a point; returns where it stopped.</summary>
        static IEnumerator Walk(EncounterSession s, Vector2 to, Func<bool> arrived, List<string> log)
        {
            var body = s.Player.GetComponent<CharacterController>(); var last = body.transform.position; int still = 0;
            for (int step = 0; step < 400 && !arrived(); step++)
            {
                var p = body.transform.position; var d = new Vector3(to.x - p.x, 0, to.y - p.z);
                if (d.magnitude < .2f) break;
                body.Move(d.normalized * .2f + Vector3.down * .15f);   // 4 m/s at a 0.05 s step, pressed to the ground
                if ((body.transform.position - last).sqrMagnitude < .0004f) still++; else still = 0;
                last = body.transform.position;
                if (still > 25)
                {
                    string what = "nothing (slope or step?)";
                    if (Physics.SphereCast(p + Vector3.up * .2f, .35f, d.normalized, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                        what = "'" + hit.collider.name + "' under '" + (hit.collider.transform.parent != null ? hit.collider.transform.parent.name : "-") + "'";
                    log.Add("stuck at (" + p.x.ToString("0.0") + ", " + p.z.ToString("0.0") + "), blocked by " + what);
                    yield break;
                }
                if (step % 4 == 0) yield return null;
            }
        }

        [UnityTest] public IEnumerator Every_exit_can_be_walked_into_and_travelled()
        {
            var problems = new List<string>();
            var first = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var defs = first.Zone.AllZones();   // plain data: still valid after this scene is replaced
            foreach (var def in defs)
            {
                string id = def.id; var exits = def.exits;
                for (int k = 0; k < exits.Length; k++)
                {
                    ZoneBuilder.RequestedZoneId = id;
                    yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
                    for (int i = 0; i < 3; i++) yield return null;
                    var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var exit = s.Zone.Zone.exits[k];
                    string name = s.Zone.Zone.displayName + " -> " + exit.name + ": ";
                    s.Player.GetComponent<AdventurerMotor>().enabled = false;
                    // Start 14 m inside the zone on the line from the exit toward the centre, then walk in.
                    var inward = (-exit.at).normalized; var start = exit.at + inward * 14;
                    s.Player.GetComponent<AdventurerMotor>().Teleport(s.Zone.Ground(start, 1.1f));
                    yield return null;
                    var log = new List<string>();
                    yield return Walk(s, exit.at, () => s.NearbyExit == exit, log);
                    if (s.NearbyExit != exit) { problems.Add(name + "never reached the exit radius; " + (log.Count > 0 ? log[0] : "ended " + s.Player.transform.position)); continue; }
                    if (s.InteractPrompt != exit.name) problems.Add(name + "E prompt is '" + s.InteractPrompt + "', not the road");
                    var near = s.Enemies.Where(e => e != null && e.Engaged && s.Distance(e) < 40).Select(e => e.actor.DisplayName + " at " + s.Distance(e).ToString("0") + " m").ToList();
                    if (near.Count > 0) { problems.Add(name + "a fight nearby (" + string.Join(", ", near) + ") refuses travel"); continue; }
                    s.Interact();
                    for (int i = 0; i < 30; i++) yield return null;
                    var t = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
                    if (t == null || t.Zone == null || t.Zone.Zone.id != exit.to) { problems.Add(name + "pressing E did not load " + exit.to); continue; }
                    var at = t.Player.transform.position;
                    if (Vector2.Distance(new Vector2(at.x, at.z), exit.arrive) > 3) problems.Add(name + "arrived at " + at + ", not at " + exit.arrive);
                    if (Mathf.Abs(at.y - (t.Zone.HeightAt(at.x, at.z) + 1)) > 1.5f) problems.Add(name + "arrived off the ground (y " + at.y + ")");
                    if (t.NearbyExit != null) problems.Add(name + "arrived inside the exit '" + t.NearbyExit.name + "' (would bounce straight back)");
                    // And can walk on from the arrival point toward the zone's centre.
                    t.Player.GetComponent<AdventurerMotor>().enabled = false;
                    var from = new Vector2(at.x, at.z); var on = from + (-from).normalized * 8; var walkLog = new List<string>();
                    yield return Walk(t, on, () => false, walkLog);
                    if (walkLog.Count > 0) problems.Add(name + "on arrival in " + t.Zone.Zone.displayName + ", " + walkLog[0]);
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
#endif
