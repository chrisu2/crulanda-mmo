using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The Living Birds hop and fly in place: the clips carried the bird forward and snapped it back at the loop (Chris,
    /// 2026-10-14: "birds hop around funny..looks like maybe back and forth", "birds still hopping"); the travel sits on Bip01, a child of
    /// the file's root, so the root locks did nothing until Bip01 was made the motion node (CreatureImport 7). The code moves the bird.</summary>
    public class BirdMotionTests
    {
        GameObject go;
        [TearDown] public void Off() { if (go != null) Object.Destroy(go); }

        [UnityTest] public IEnumerator A_crow_hops_in_place()
        {
            Assume.That(ModelBeast.Available("BirdCrow"), "the Living Birds are in the build");
            go = new GameObject("Bird motion"); var body = new GameObject("Body").transform; body.SetParent(go.transform, false);
            var m = ModelBeast.Build(body, "BirdCrow", 0, .36f); Assert.NotNull(m, "a crow");
            m.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var bip = m.Model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Bip01"); Assert.NotNull(bip, "Bip01, the biped's root");
            m.Drive(1.2f, "idle");   // at a walking pace the hop plays
            yield return null; yield return null;
            Vector3 Flat(Vector3 v) { v.y = 0; return v; }
            var first = Flat(go.transform.InverseTransformPoint(bip.position)); var model0 = m.Model.localPosition; float most = 0, root = 0;
            for (float t = 0; t < 2.5f; t += Time.deltaTime)
            {
                yield return null;
                most = Mathf.Max(most, Vector3.Distance(first, Flat(go.transform.InverseTransformPoint(bip.position))));
                root = Mathf.Max(root, Vector3.Distance(Flat(model0), Flat(m.Model.localPosition)));
            }
            Assert.Less(most, .15f, "the hop stays where the code puts the bird: Bip01 strayed " + most.ToString("0.00") + " m sideways over 2.5 s");
            Assert.Less(root, .05f, "the model's root does not travel (" + root.ToString("0.00") + " m)");
        }
    }
}
