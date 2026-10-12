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
    /// <summary>Playtest note 150 (Chris, 2026-10-12: he needs to see farmers digging and ploughing the field): a farmer at work in a
    /// field hoes with a real two-handed stroke, and a plough team works the furrows of a bare field.</summary>
    public class FieldWorkTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(float hour)
        {
            WorldClock.Hour = hour;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-field-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 5; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            PlowTeam.Forced = false; VillageEconomy.ResetAll(); Time.timeScale = 1; WorldClock.Hour = 8.5f;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }

        /// <summary>A farmer at work in a field: a stroke every 1 to 3 s, the fork's tines within 10 cm of the ground (or into it) at the
        /// strike, both hands within 4 cm of the shaft through the stroke, and a step along the row between strokes.</summary>
        [UnityTest] public IEnumerator A_farmer_at_work_in_a_field_hoes_with_a_two_handed_stroke()
        {
            yield return Load(10);
            Assert.Greater(CropField.All.Count, 0, "Oakhaven has crop fields");
            var farmer = UnityEngine.Object.FindObjectsByType<Villager>(FindObjectsSortMode.None).FirstOrDefault(v => v.Role == "farmer" && v.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Hoe blade"));
            Assert.NotNull(farmer, "a farmer with a hoe");
            var anim = farmer.GetComponentInChildren<Animator>(); if (anim != null) anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var vis = farmer.GetComponent<ActorVisual>(); var agent = farmer.GetComponent<UnityEngine.AI.NavMeshAgent>();
            farmer.enabled = false; if (farmer.Carrying) farmer.PutDownLoad();
            var field = CropField.All[0];
            if (agent != null && agent.isOnNavMesh) { agent.Warp(field.transform.position); agent.isStopped = true; }
            for (int i = 0; i < 10; i++) yield return null;
            vis.Pose = ActorPose.Work;
            int start = -1; float t0 = 0; var fails = new List<string>(); int framesHoeing = 0; var gaps = new List<float>(); var draws = new List<float>(); float lowestDraw = 9, groundY = 0; bool inSoil = false; float enterAt = 0, minAt = 0;
            float end = Time.time + 9;
            while (Time.time < end)
            {
                yield return null;
                if (!vis.Hoeing) continue; framesHoeing++;
                // The draw (note 155): in the soil the blade travels toward the body, not just down.
                var flat = vis.BladeEdge - farmer.transform.position; flat.y = 0; float out_ = Vector3.Dot(flat, farmer.transform.forward);
                groundY = farmer.transform.position.y - (agent != null ? agent.baseOffset : 1); if (vis.StrokePhase > .5f && vis.StrokePhase < .78f) lowestDraw = Mathf.Min(lowestDraw, vis.BladeEdge.y - groundY);
                if (vis.BladeInSoil) { if (!inSoil) { enterAt = out_; minAt = out_; inSoil = true; } else minAt = Mathf.Min(minAt, out_); }
                else if (inSoil) { inSoil = false; draws.Add(enterAt - minAt); }
                if (vis.HoeReport(out float l, out float r))
                {
                    if (l > .04f) fails.Add("left hand " + (l * 100).ToString("0.0") + " cm off the shaft");
                    if (r > .04f) fails.Add("right hand " + (r * 100).ToString("0.0") + " cm off the shaft");
                    if (vis.HandSpread < .45f || vis.HandSpread > .6f) fails.Add("hands " + (vis.HandSpread * 100).ToString("0") + " cm apart on the shaft (45-60 wanted)");
                }
                if (start < 0 && vis.StrokeCount > 0) { start = vis.StrokeCount; t0 = Time.time; }
                if (start >= 0 && vis.StrokeCount > start + gaps.Count) gaps.Add(vis.StrikeGap);
            }
            Assert.Greater(framesHoeing, 30, "the farmer hoes (a long tool, at work, standing)");
            int strokes = vis.StrokeCount - start; Assert.GreaterOrEqual(strokes, 2, "strokes struck in nine seconds");
            float period = (Time.time - t0) / strokes;
            Assert.That(period, Is.InRange(1f, 3.2f), "a stroke every 1 to 3 s");
            Assert.GreaterOrEqual(draws.Count, 2, "draws seen (the blade in the soil, then out); strike gaps " + string.Join(",", gaps.Select(g => g.ToString("0.00"))) + "; lowest edge in the draw " + lowestDraw.ToString("0.00") + "; ground " + groundY.ToString("0.00"));
            foreach (var dr in draws) if (dr < .3f) fails.Add("the draw brought the blade only " + (dr * 100).ToString("0") + " cm toward the feet (30 wanted)");
            foreach (var g in gaps) if (g > .10f || g < -.3f) fails.Add("the tines " + (g * 100).ToString("0") + " cm over the ground at the strike");
            Assert.IsEmpty(fails.Distinct().Take(12).ToList(), string.Join("; ", fails.Distinct().Take(12)));
        }

        /// <summary>The plough team: out on a field by day, walking its furrows faster than 20 m a minute, staying in the field (the
        /// headlands included), the ploughman's hands on the handles.</summary>
        [UnityTest] public IEnumerator A_plough_team_works_the_rows_of_a_field()
        {
            yield return Load(10);
            PlowTeam.Forced = true;
            for (int i = 0; i < 30 && PlowTeam.Active == null; i++) yield return null;
            Assert.NotNull(PlowTeam.Active, "a plough team is made where there are fields");
            var team = PlowTeam.Active; team.CheckNow();
            Assert.IsTrue(team.Out, "out on a field (forced)");
            var f = team.Field; var anim = team.Ploughman.GetComponentInChildren<Animator>(); if (anim != null) anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            float travelled = 0; var last = team.Horse.position; var fails = new List<string>(); float t0 = Time.time;
            while (Time.time < t0 + 12)
            {
                yield return null;
                var p = team.Horse.position; var d = p - last; d.y = 0; travelled += d.magnitude; last = p;
                foreach (var tr in new[] { team.Horse, team.Plough, team.Ploughman.transform })
                {
                    var local = f.transform.InverseTransformPoint(tr.position);
                    if (Mathf.Abs(local.x) > f.Field.size.x / 2 + 2 || Mathf.Abs(local.z) > f.Field.size.y / 2 + 2) fails.Add(tr.name + " left the field (" + local.x.ToString("0.0") + ", " + local.z.ToString("0.0") + ")");
                }
                if (Time.time > t0 + 1.5f && !(team.Ploughman.HandsGap < .12f)) fails.Add("the ploughman's hands " + team.Ploughman.HandsGap.ToString("0.00") + " m off the handles");
            }
            float perMinute = travelled / (Time.time - t0) * 60;
            Assert.Greater(perMinute, 20f, "the team walks the rows (m/min)");
            Assert.IsEmpty(fails.Distinct().Take(10).ToList(), string.Join("; ", fails.Distinct().Take(10)));
        }
    }
}
#endif