#if UNITY_EDITOR
using System;
using System.Collections;
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
    /// <summary>The day/night cycle in Oakhaven: hens roost at night, the hen-wife opens and feeds by day, and rounds them up at dusk.</summary>
    public class VillageDayTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root;
        }
        IEnumerator Load(float hour)
        {
            WorldClock.Hour = hour;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-day-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            VillageEconomy.ResetAll();   // the purses are kept for the play session
            Time.timeScale = 1; WorldClock.Hour = 8.5f;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static IEnumerator WaitUntil(Func<bool> done, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!done() && Time.realtimeSinceStartup < end) yield return null;
        }

        /// <summary>A trade's long tool (the farmer's fork, the woodcutter's axe) is in the right fist, upright like a staff with its head at
        /// the top, and clear of the forearm, the upper arm, the body and the legs while its villager walks, stands or talks; at work it runs
        /// on from the fist, still clear of the arm. Playtest notes 95, 109 and 119 (Chris, 2026-10-11: "farmers hoes still going thru the
        /// arm and not held like a staff"): the check before measured only the tilt, of the woodcutter's axe alone, while the farmers' forks
        /// were built onto the arm and went through it.</summary>
        [UnityTest] public IEnumerator A_long_tool_is_held_in_the_fist_like_a_staff_clear_of_the_arm()
        {
            yield return Load(10);
            for (int i = 0; i < 10; i++) yield return null;
            Transform ShaftOf(Transform tool) { return tool.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x != tool && x.name.Contains("Cylinder")); }
            var holders = UnityEngine.Object.FindObjectsByType<ActorVisual>(FindObjectsSortMode.None)
                .Select(v => (v, tool: v.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Trade tool"), anim: v.GetComponentInChildren<Animator>()))
                .Where(x => x.tool != null && x.anim != null && x.anim.isHuman && ShaftOf(x.tool) != null && ShaftOf(x.tool).localScale.y > .5f).ToList();
            Assert.Greater(holders.Count(x => x.tool.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Tine")), 0, "the farmers' forks are on the tool holder");
            foreach (var x in holders) x.anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // posed in a batch run too (FootTests)
            float Seg(Vector3 q, Vector3 a, Vector3 b) { var ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(q - a, ab) / ab.sqrMagnitude); return Vector3.Distance(q, a + ab * t); }
            var fails = new System.Collections.Generic.List<string>(); int carried = 0, talking = 0, working = 0, staffSeen = 0;
            var last = holders.ToDictionary(x => x.v, x => x.v.transform.position);
            for (int f = 0; f < 270; f++)
            {
                // As they go about, then made to stand and talk, then to work (the farmer's hoeing stroke).
                var pose = f < 90 ? (ActorPose?)null : f < 180 ? ActorPose.Talk : ActorPose.Work;
                if (f == 90 || f == 180) foreach (var x in holders) { var life = x.v.GetComponent<Villager>(); if (life != null) life.enabled = false; var agent = x.v.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (agent != null && agent.isOnNavMesh) agent.isStopped = true; }
                if (pose.HasValue) foreach (var x in holders) x.v.Pose = pose.Value;
                yield return null;
                if (f % 90 < 20) continue;   // the arm settles into its hold
                foreach (var x in holders)
                {
                    var pos = x.v.transform.position; float speed = Vector3.Distance(pos, last[x.v]) / Mathf.Max(Time.deltaTime, 1e-4f); last[x.v] = pos;
                    if (!x.tool.gameObject.activeInHierarchy) continue;   // put away (seated, a load in the arms)
                    var c = ShaftOf(x.tool); Vector3 axis = c.up; float half = c.lossyScale.y;   // Unity's cylinder is two units tall
                    Vector3 b0 = c.position - axis * half, b1 = c.position + axis * half;
                    var A = x.anim; Vector3 B(HumanBodyBones b) { var t = A.GetBoneTransform(b); return t != null ? t.position : new Vector3(float.NaN, 0, 0); }
                    Vector3 sh = B(HumanBodyBones.RightUpperArm), el = B(HumanBodyBones.RightLowerArm), wr = B(HumanBodyBones.RightHand);
                    var fist = wr + (wr - el) * .3f;
                    string who = x.v.name + (x.v.ToolAtWork ? " at work" : pose == ActorPose.Talk ? " talking" : speed > .5f ? " walking" : " standing") + ": ";
                    float inFist = Seg(fist, b0, b1);
                    // Note 119 (Chris, 2026-10-12: "standing weird", "holding the hoe better but still not great"; the frames had the
                    // shaft some 20 cm in front of an open hand): held like a staff, the shaft runs through the closed hand (within 3 cm
                    // of the middle knuckle, the middle fingertip closed round it), the upper arm hangs near straight down and the elbow
                    // is bent only as a relaxed arm is.
                    if (x.v.StaffHoldReport(out float knuckle, out float tip, out float shoulder, out float elbow))
                    {
                        inFist = 0; staffSeen++;
                        if (knuckle > .03f) fails.Add(who + "the shaft is " + (knuckle * 100).ToString("0.0") + " cm from the knuckles (over 3)");
                        if (tip > .04f) fails.Add(who + "the fingers are open: the middle fingertip " + (tip * 100).ToString("0.0") + " cm off the shaft");
                        if (shoulder > 30) fails.Add(who + "the upper arm is held out " + shoulder.ToString("0") + " degrees from hanging down");
                        if (elbow < 20 || elbow > 80) fails.Add(who + "the elbow is bent " + elbow.ToString("0") + " degrees (natural: 20 to 80)");
                    }
                    if (inFist > .07f) fails.Add(who + "the fist is " + inFist.ToString("0.00") + " m off the shaft");
                    float arm = new[] { el, (el + wr) / 2, (sh + el) / 2, sh }.Min(q => Seg(q, b0, b1));
                    if (arm < .06f) fails.Add(who + "the shaft goes through the arm (" + arm.ToString("0.00") + " m from the forearm or upper arm)");
                    if (x.v.ToolAtWork) { working++; continue; }
                    if (pose == ActorPose.Talk && speed < .3f) talking++; else carried++;
                    float tilt = Vector3.Angle(axis, Vector3.up);
                    if (tilt > 20) fails.Add(who + "the shaft leans " + tilt.ToString("0") + " degrees");
                    var head = x.tool.GetComponentsInChildren<Transform>(true).Where(t => t != x.tool && t != c).Select(t => t.position).DefaultIfEmpty(b1).Aggregate(Vector3.zero, (acc, q) => acc + q);
                    int headParts = x.tool.GetComponentsInChildren<Transform>(true).Count(t => t != x.tool && t != c);
                    if (headParts > 0 && (head / headParts).y < fist.y) fails.Add(who + "the head is below the fist");
                    float bodyGap = new[] { B(HumanBodyBones.Hips), B(HumanBodyBones.Spine), B(HumanBodyBones.Chest), B(HumanBodyBones.Head) }.Where(q => !float.IsNaN(q.x)).Min(q => Seg(q, b0, b1));
                    if (bodyGap < .12f) fails.Add(who + "the shaft goes through the body (" + bodyGap.ToString("0.00") + " m)");
                    float legGap = new[] { B(HumanBodyBones.RightLowerLeg), B(HumanBodyBones.RightFoot), (B(HumanBodyBones.RightUpperLeg) + B(HumanBodyBones.RightLowerLeg)) / 2, B(HumanBodyBones.LeftLowerLeg), B(HumanBodyBones.LeftFoot) }.Where(q => !float.IsNaN(q.x)).Min(q => Seg(q, b0, b1));
                    if (legGap < .07f) fails.Add(who + "the shaft goes through a leg (" + legGap.ToString("0.00") + " m)");
                }
            }
            Assert.Greater(staffSeen, 0, "a long tool seen held like a staff in a modelled hand with fingers");
            Assert.Greater(carried, 0, "a long tool seen carried"); Assert.Greater(talking, 0, "a long tool seen while its villager stands and talks"); Assert.Greater(working, 0, "a long tool seen at work");
            Assert.IsEmpty(fails.Distinct().Take(12).ToList(), fails.Count + " bad frames (carried " + carried + ", talking " + talking + ", at work " + working + "):\n" + string.Join("\n", fails.Distinct().Take(12)));
        }
        [UnityTest] public IEnumerator At_night_hens_roost_behind_shut_doors_and_the_village_sleeps()
        {
            yield return Load(23.5f);
            var life = VillageLife.Active; Assert.NotNull(life);
            Assert.AreEqual(3, life.Zone.Coops.Count, "Oakhaven has three hen coops.");
            Assert.IsTrue(life.Zone.Coops.All(c => !c.Open), "Coops are shut at night.");
            var hens = life.Critters.Where(c => c.Coop != null).ToList();
            Assert.AreEqual(14, hens.Count, "Every Oakhaven chicken belongs to a coop.");
            Assert.IsTrue(hens.All(h => h.Roosting), "Hens roost at night.");
            Assert.AreEqual(3, life.Villagers.Count(v => v.Role == "henwife"));
            Assert.IsTrue(life.Villagers.Where(v => v.Role != "drinker" && !v.Resident).All(v => !v.Visible), "Everyone but the last drinkers (and residents at their posts) is abed.");
            Assert.IsTrue(life.Villagers.Where(v => v.Resident).All(v => v.Visible), "Residents keep their posts at night.");
            Assert.Greater(WorldClock.Darkness, .9f);
        }

        [UnityTest] public IEnumerator By_day_the_hen_wife_opens_up_and_scatters_feed()
        {
            yield return Load(8.6f);
            var life = VillageLife.Active;
            Assert.IsTrue(life.Zone.Coops.All(c => c.Open), "Coops are open by day.");
            Assert.IsTrue(life.Critters.Where(c => c.Coop != null).All(h => !h.Roosting), "Hens are out by day.");
            Time.timeScale = 3;
            var harrow = life.Zone.Coops[0];
            yield return WaitUntil(() => harrow.Feeding, 30);
            Assert.IsTrue(harrow.Feeding, "The hen-wife scatters feed in the morning.");
        }

        [UnityTest] public IEnumerator At_dusk_the_hens_go_in_and_the_door_is_shut()
        {
            yield return Load(17.5f);
            var life = VillageLife.Active; var coop = life.Zone.Coops[0];
            yield return new WaitForSeconds(.5f);
            var hens = life.HensOf(coop).ToList();
            Assert.Greater(hens.Count, 0); Assert.IsTrue(hens.All(h => !h.Roosting));
            WorldClock.Hour = 19.6f; Time.timeScale = 3;
            yield return WaitUntil(() => hens.All(h => h.Roosting) && !coop.Open, 45);
            Assert.IsTrue(hens.All(h => h.Roosting), "Every hen is in by nightfall (" + hens.Count(h => h.Roosting) + "/" + hens.Count + ").");
            Assert.IsFalse(coop.Open, "The hen-wife shuts the coop once they're all in.");
        }
    }
}
#endif
