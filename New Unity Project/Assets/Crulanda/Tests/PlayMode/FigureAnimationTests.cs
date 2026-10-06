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

namespace Crulanda.Tests
{
    /// <summary>
    /// No one stands in the bind pose (2026-10-06, "people t pose too much"; playtest note 51 before it): every modelled figure in
    /// Oakhaven, in each kit, is driven by its clips. The bind pose has the hands 1.28 m (a woman) to 1.48 m apart; any clip brings them well in.
    /// </summary>
    public class FigureAnimationTests
    {
        string root;
        static Transform Find(Transform t, string n) { if (t.name == n) return t; foreach (Transform c in t) { var r = Find(c, n); if (r != null) return r; } return null; }
        static float Span(Transform t) { var l = Find(t, "hand_l"); var r = Find(t, "hand_r"); return l != null && r != null ? Vector3.Distance(l.position, r.position) : -1; }
        /// <summary>What a figure is playing: its heaviest motion slots and weights, and the upper layer's weight.</summary>
        static string Playing(Transform t)
        {
            var av = t.GetComponent<ActorVisual>(); if (av == null) return "";
            var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var m = typeof(ActorVisual).GetField("model", bf).GetValue(av) as ModelFigure; if (m == null) return "";
            var w = typeof(ModelFigure).GetField("weights", bf).GetValue(m) as float[];
            var slotOf = typeof(ModelFigure).GetField("slotOf", bf).GetValue(m) as Dictionary<string, int>;
            var lw = (float)typeof(ModelFigure).GetField("layerWeight", bf).GetValue(m);
            var top = slotOf.OrderByDescending(kv => w[kv.Value]).Take(3).Where(kv => w[kv.Value] > .01f).Select(kv => kv.Key + " " + w[kv.Value].ToString("0.00"));
            return "[" + string.Join(", ", top) + "; upper " + lw.ToString("0.00") + "; pose " + av.Pose + "; avatar " + (m.Animator.avatar != null ? m.Animator.avatar.name : "none") + "]";
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }

        [UnityTest] public IEnumerator Nobody_stands_in_the_bind_pose([Values("class.warrior", "class.mage")] string classId)
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-anim-" + Guid.NewGuid().ToString("N"));
            EncounterSession.StartClassOverride = classId;
            SceneManager.sceneLoaded += OnLoaded;
            try
            {
                yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
                yield return null;
                var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var pop = SimPopulation.Active;
                foreach (var x in pop.World.sims.Take(4)) { x.zone = "zone.oakhaven"; x.onlineFrom = 0; x.onlineHours = 24; x.friendly = .9f; x.level = session.Progress.Level; x.x = x.z = 0; }
                pop.Refresh(); yield return null;
                foreach (var f in pop.Figures.ToArray()) if (session.PartySims.Count < 2) session.Invite(f.sim.id);
                // Batch mode has no camera on screen, and an Animator culled off screen leaves its bones as they are: animate them all here.
                foreach (var an in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None)) an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                yield return new WaitForSeconds(2);
                var who = new List<(string, Transform)> { ("you (" + classId + ")", session.Player.transform), ("Mira", session.Companion.transform) };
                foreach (var c in session.PartySims) who.Add(("party " + c.sim.name + " (" + c.sim.classId + ")", c.transform));
                foreach (var f in pop.Figures) who.Add(("sim " + f.sim.name + " (" + f.sim.classId + ")", f.transform));
                foreach (var e in session.Enemies.Where(e => e != null && !e.Game).Take(6)) who.Add(("enemy " + e.actor.DisplayName, e.transform));
                foreach (var v in UnityEngine.Object.FindObjectsByType<Villager>(FindObjectsSortMode.None).Take(8)) who.Add(("villager " + v.Name, v.transform));
                Assert.GreaterOrEqual(who.Count(w => Span(w.Item2) > 0), 6, "figures measured");
                // Ten seconds, every half second: an idle loops through its whole clip in that time for most, so a bad stretch shows.
                var stiff = new List<string>();
                for (int i = 0; i < 20; i++)
                {
                    foreach (var w in who)
                    {
                        if (w.Item2 == null || stiff.Exists(x => x.StartsWith(w.Item1 + " "))) continue;
                        float sp = Span(w.Item2); if (sp > 1.1f) stiff.Add(w.Item1 + " " + sp.ToString("0.00") + " m at " + (i * .5f) + " s " + Playing(w.Item2));
                    }
                    yield return new WaitForSeconds(.5f);
                }
                CollectionAssert.IsEmpty(stiff, "in the bind pose");
            }
            finally { EncounterSession.StartClassOverride = null; SceneManager.sceneLoaded -= OnLoaded; }
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
#endif
