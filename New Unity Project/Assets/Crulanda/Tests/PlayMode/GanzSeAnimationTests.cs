#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// A man on GanzSe's body plays our clips (Phase 8.5 step 1; wardrobe-ganzse-1, 2026-10-18: every GanzSe figure stood in a T-pose
    /// while the Quaternius figures beside it idled). Built with the switch on, standing in front of a camera, he is watched for two
    /// seconds: his upper arms hang down (the bind pose holds them level) and his hands move from frame to frame. Twice: with the
    /// Animator set to animate always (the avatar binds the copied skeleton and the clips reach it), then culling as the game builds
    /// it (CullUpdateTransforms: posed only when its renderers are drawn; an Animator added before its renderers never was).
    /// </summary>
    public class GanzSeAnimationTests
    {
        static float Down(Transform upper, Transform lower) { return Vector3.Dot((lower.position - upper.position).normalized, Vector3.down); }

        [UnityTest] public IEnumerator A_GanzSe_man_plays_our_clips_and_drops_his_arms()
        {
            Assume.That(ModelFigure.Available && ModelFigure.GanzSeAvailable, "Both kits are in the project.");
            bool was = GanzSeKit.On, models = ActorVisual.Models; GanzSeKit.On = true; ActorVisual.Models = true;
            var made = new List<Object>();
            try
            {
                foreach (bool always in new[] { true, false })
                {
                    var go = new GameObject("GanzSe animation figure"); made.Add(go); new GameObject("Body").transform.SetParent(go.transform, false);
                    go.transform.position = new Vector3(always ? 0 : 6, 1, 400);
                    var v = ActorVisual.Attach(go, ActorLook.Warrior, 3); var m = v.Model;
                    Assert.IsTrue(m != null && m.GanzSe, "built on the pack's body");
                    Assert.AreEqual(AnimatorCullingMode.CullUpdateTransforms, m.Animator.cullingMode, "it culls as the game's figures do");
                    Assert.Less(Down(m.UpperArmL, m.LowerArmL), .35f, "built in the bind pose, arms out (the measure is sound)");
                    if (always) m.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    else
                    {
                        Assume.That(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "drawing needs a graphics device (a culling Animator is posed only when drawn)");
                        var cam = new GameObject("GanzSe test camera").AddComponent<Camera>(); made.Add(cam.gameObject);
                        var rt = new RenderTexture(256, 256, 16); made.Add(rt); cam.targetTexture = rt;
                        cam.transform.position = go.transform.position + new Vector3(0, .2f, 4); cam.transform.LookAt(go.transform.position);
                    }
                    var hands = new List<Vector3>(); float worstL = 1, worstR = 1;
                    for (float t = 0; t < 2.5f; t += Time.deltaTime)
                    {
                        yield return null;
                        if (t < .5f) continue;   // the idle blended in
                        worstL = Mathf.Min(worstL, Down(m.UpperArmL, m.LowerArmL)); worstR = Mathf.Min(worstR, Down(m.UpperArmR, m.LowerArmR));
                        hands.Add(go.transform.InverseTransformPoint(m.HandR.position));
                    }
                    string how = always ? "animating always" : "culling as built, in a camera's view";
                    Assert.Greater(worstL, .6f, how + ": the left upper arm hangs down (dot with down " + worstL + "; a T-pose is about 0)");
                    Assert.Greater(worstR, .6f, how + ": the right upper arm hangs down (" + worstR + ")");
                    float moved = 0; foreach (var p in hands) moved = Mathf.Max(moved, Vector3.Distance(p, hands[0]));
                    Assert.Greater(moved, .002f, how + ": the hand moves as the idle plays (" + moved + " m)");
                }
            }
            finally
            {
                foreach (var o in made) if (o != null) Object.Destroy(o);
                GanzSeKit.On = was; ActorVisual.Models = models;
            }
        }
    }
}
#endif
