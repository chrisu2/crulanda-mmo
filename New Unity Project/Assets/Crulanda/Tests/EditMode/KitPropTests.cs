using System.Text;
using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>The kit props stand up when ModelProp places them (Chris, 2026-10-14: the inn's tables and benches on their sides).</summary>
    public class KitPropTests
    {
        /// <summary>The Fantasy pack is Blender's, Z up in every file (each node turned -90 about x): placed upright, every mesh's local
        /// z points at the sky, whether the file's turn sits on the prefab root (a one-mesh file) or on a child.</summary>
        [Test] public void Every_Fantasy_prop_stands_up()
        {
            var root = new GameObject("kit").transform; int meshes = 0; var wrong = new StringBuilder();
            foreach (var (path, height) in ZoneBuilder.KitProps)
            {
                if (!path.StartsWith("Fantasy/")) continue;
                var go = ZoneBuilder.ModelProp(root, path, Vector3.zero, 0, height); Assert.NotNull(go, path);
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    float up = Vector3.Dot(mf.transform.forward, Vector3.up); meshes++;
                    if (up < .99f) wrong.Append(path + " (" + mf.name + ") z to the sky " + up.ToString("0.00") + ", root " + go.transform.GetChild(0).localRotation.eulerAngles + "; ");
                }
            }
            Object.DestroyImmediate(root.gameObject);
            Assert.Greater(meshes, 10, "the Fantasy props loaded");
            Assert.AreEqual("", wrong.ToString(), "on their sides: ");
        }
    }
}
