using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Crulanda.EditorTools
{
    /// <summary>A kit's imported hierarchy, written to a file (2026-10-07: the Modular Hero helmets did not show in the wardrobe line-up).
    /// <c>-executeMethod Crulanda.EditorTools.KitDiag.ListArmorParts</c>; the file is kit-diag.txt beside the project.</summary>
    public static class KitDiag
    {
        public static void ListArmorParts()
        {
            var sb = new StringBuilder();
            var kit = Resources.Load<GameObject>("Props/ModularHero/Models/Armor Parts/Armor Parts");
            sb.AppendLine("Resources.Load Props/ModularHero/Models/Armor Parts/Armor Parts: " + (kit == null ? "NULL" : kit.name));
            if (kit != null)
            {
                int n = 0;
                foreach (var t in kit.GetComponentsInChildren<Transform>(true))
                {
                    var mf = t.GetComponent<MeshFilter>(); var mr = t.GetComponent<MeshRenderer>();
                    if (n++ < 40 || t.name.StartsWith("Headgear"))
                        sb.AppendLine(t.name + " | parent " + (t.parent ? t.parent.name : "-") + " | pos " + t.localPosition + " rot " + t.localEulerAngles + " scale " + t.localScale
                            + (mf != null && mf.sharedMesh != null ? " | mesh " + mf.sharedMesh.name + " verts " + mf.sharedMesh.vertexCount + " bounds " + mf.sharedMesh.bounds : " | no mesh")
                            + (mr != null && mr.sharedMaterial != null ? " | mat " + mr.sharedMaterial.name : ""));
                }
                sb.AppendLine("transforms: " + n);
            }
            var path = Path.Combine(Directory.GetCurrentDirectory(), "kit-diag.txt"); File.WriteAllText(path, sb.ToString()); Debug.Log("KitDiag wrote " + path);
        }
    }
}
