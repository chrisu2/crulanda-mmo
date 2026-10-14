#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// The whole orcs move in play (2026-10-13, Chris playing the orc Shaman: "the only thing on orc that move is belt, staff and teeth",
    /// "orc has no walking animations"): an orc Shaman and an axe orc, built as the game builds them, walked across the ground for two
    /// seconds; their arms and legs swing (each hand and foot moves against the body), and their avatar is not a human one.
    /// </summary>
    public class OrcMotionTests
    {
        readonly List<GameObject> made = new List<GameObject>();
        [TearDown] public void Off() { foreach (var g in made) if (g != null) Object.Destroy(g); made.Clear(); }

        IEnumerator Walk(ActorLook look, string kind, bool orc = true, string name = null)
        {
            Assume.That(ModelBeast.Available(kind), kind + " is in the build");
            var go = new GameObject(name ?? "Orc motion " + kind); new GameObject("Body").transform.SetParent(go.transform, false); made.Add(go);
            var v = ActorVisual.Attach(go, look, 0, false, null, orc);
            Assert.NotNull(v.BeastModel, "a model"); Assert.AreEqual(kind, v.BeastModel.Kind);
            var anim = v.BeastModel.Animator; Assert.IsFalse(anim.avatar != null && anim.avatar.isHuman, kind + ": not a human avatar");
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // no camera in a test: animate unseen
            var all = v.BeastModel.Model.GetComponentsInChildren<Transform>(); var start = all.Select(t => (t.localPosition, t.localRotation)).ToArray();
            var limbs = v.BeastModel.Model.GetComponentsInChildren<Transform>().Where(t => { var n = t.name.ToLowerInvariant(); return (n.Contains("hand") || n.Contains("wrist") || n.Contains("foot") || n.Contains("ankle")) && !n.Contains("index") && !n.Contains("thumb") && !n.Contains("middle") && !n.Contains("ring") && !n.Contains("pinky") && !n.Contains("ik"); }).ToList();
            Assert.GreaterOrEqual(limbs.Count, 2, kind + ": hands and feet (" + string.Join(", ", v.BeastModel.Model.GetComponentsInChildren<Transform>().Select(t => t.name).Take(60)) + ")");
            var body = go.transform; var first = limbs.Select(t => body.InverseTransformPoint(t.position)).ToList(); float most = 0;
            for (float t = 0; t < 2; t += Time.deltaTime)
            {
                go.transform.position += Vector3.forward * 3 * Time.deltaTime;   // walking: the visual reads its pace from how it moves
                yield return null;
                for (int i = 0; i < limbs.Count; i++) most = Mathf.Max(most, Vector3.Distance(first[i], body.InverseTransformPoint(limbs[i].position)));
            }
            var moved = all.Where((t, i) => Vector3.Distance(t.localPosition, start[i].localPosition) > .001f || Quaternion.Angle(t.localRotation, start[i].localRotation) > 2).Select(t => t.name).ToList();
            Assert.Greater(most, .08f, kind + ": its hands and feet swing as it walks (" + most.ToString("0.000") + " m; " + moved.Count + " of " + all.Length + " bones moved: " + string.Join(", ", moved.Take(14)) + "; avatar " + (anim.avatar != null ? anim.avatar.name + " human " + anim.avatar.isHuman : "none") + ")");
        }
        [UnityTest] public IEnumerator An_orc_shaman_walks_on_its_own_legs() { yield return Walk(ActorLook.Druid, "OrcShaman"); }
        [UnityTest] public IEnumerator An_axe_orc_walks_on_its_own_legs() { yield return Walk(ActorLook.Warrior, "OrcBrute"); }
        [UnityTest] public IEnumerator The_bone_thrall_walks_on_its_own_legs() { yield return Walk(ActorLook.Skeleton, "UndeadUnit", false, "Bone thrall"); }
    }
}
#endif
