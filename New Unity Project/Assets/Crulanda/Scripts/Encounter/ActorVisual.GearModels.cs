using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Weapons and shields as models (2026-10-05: Chris added Blink's swords and RPG weapons, Lumo-Art's cartoon weapons and SICS
    /// Games' low-poly weapons to the project, "use in the loot table. some of the glowing weapons should be epic or legendary
    /// only"; tools/wip/weapons/weapons_import.py; their prefabs in Resources/Weapons). A look "model.weapon:Sword9_Blue/..." (or
    /// model.tall, model.shield) puts that prefab in the hand: measured from its meshes, its longest axis laid along the weapon
    /// frame's +Y with the grip at the origin (a prefab's own pivot when it sits near an end, else 12% up from the end nearer the
    /// pivot), its width toward +Z and its flats across X, scaled to the length its kind is held at (ModelLength); a shield laid
    /// with its face toward +Y and its top toward +X, scaled to .62 m. The pack's own materials (Standard, their glows) are kept.
    /// Colliders and scripts on the prefab are dropped.
    /// </summary>
    public sealed partial class ActorVisual
    {
        static readonly Dictionary<string, (Quaternion rot, Vector3 offset, float scale)> modelFits = new Dictionary<string, (Quaternion, Vector3, float)>();

        /// <summary>The length a kind of weapon is drawn at, grip to tip, in the weapon frame (the mount scales it up 1.35 more).</summary>
        public static float ModelLength(string name)
        {
            string n = name ?? "";
            if (n.Contains("Dagger")) return .45f;
            if (n.Contains("Wand")) return .42f;
            if (n.Contains("Espadon") || n.Contains("2H") && (n.Contains("Axe") || n.Contains("Mace"))) return 1.15f;
            if (n.Contains("Axe") || n.Contains("Mace")) return .72f;
            if (n.Contains("Spear2H")) return 1.95f;
            if (n.Contains("Spear")) return 1.65f;
            if (n.Contains("Staff")) return 1.65f;
            if (n.Contains("Bow")) return 1.2f;   // the Ranger's bow (ActorVisual.ClassModels.cs), gripped at its centre
            return .95f;   // a sword
        }

        void ModelGear(GearKit k, Transform root, bool shield)
        {
            var src = Resources.Load<GameObject>("Weapons/" + k.l.variant);
            if (src == null) { if (shield) RoundShield(k, root); else SwordArming(k, root); return; }
            var go = Instantiate(src, root, false); go.name = "Model " + k.l.variant;
            foreach (var c in go.GetComponentsInChildren<Component>(true))
                if (!(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer)) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
            string key = k.l.variant + (shield ? "#s" : "");
            if (!modelFits.TryGetValue(key, out var fit)) { fit = ModelFit(go.transform, shield, k.l.variant); modelFits[key] = fit; }
            go.transform.localRotation = fit.rot; go.transform.localScale = Vector3.one * fit.scale; go.transform.localPosition = fit.offset;
        }

        /// <summary>How to lay a prefab in the weapon (or shield) frame: measured once per model from its meshes in its own space.</summary>
        static (Quaternion, Vector3, float) ModelFit(Transform model, bool shield, string name)
        {
            var b = new Bounds(); bool any = false; var inv = model.worldToLocalMatrix;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue; var m = inv * mf.transform.localToWorldMatrix; var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var p = m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            if (!any) return (Quaternion.identity, Vector3.zero, 1);
            // The axes by size: longest, middle, thinnest.
            var size = b.size; var order = new List<int> { 0, 1, 2 }; order.Sort((x, y) => size[y].CompareTo(size[x]));
            Vector3 Axis(int i) { var v = Vector3.zero; v[i] = 1; return v; }
            int lng = order[0], mid = order[1], thin = order[2];
            if (shield)
            {
                // Face toward +Y: the shield's front is the side its bulk stands out to from its pivot (the grip is behind).
                var faceDir = Axis(thin) * (b.center[thin] >= 0 ? 1 : -1); var topDir = Axis(lng);
                var rotS = Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(topDir, faceDir), faceDir)) ;
                // LookRotation(fwd=top x face, up=face) maps Z to that and Y to face; then a turn so the top lies along +X.
                rotS = Quaternion.Euler(0, 90, 0) * rotS;
                float sc = .62f / Mathf.Max(.01f, size[lng]);
                return (rotS, -(rotS * b.center) * sc + Vector3.up * .02f, sc);
            }
            // The grip (WeaponReport.txt): the packs build their weapons up their long axis's + side, the grip at the pivot when the
            // pivot sits low (Blink's blades, axes and maces, SICS's blades); a pivot in the middle (the cartoon pack's, Blink's
            // staves and spears) says nothing, so the grip goes 12% up a one-hander and 40% up a staff or spear (two hands).
            float lo = b.min[lng], len = Mathf.Max(.01f, size[lng]), at = (0 - lo) / len;
            bool tall = name.Contains("Staff") || name.Contains("Spear");
            bool fromLow = at <= .67f;
            float gripAt = at < .35f ? at : at > .67f ? at : tall ? .4f : .12f;
            if (!fromLow) gripAt = at;
            var up = Axis(lng) * (fromLow ? 1 : -1);   // from the grip toward the far end
            var rot = Quaternion.Inverse(Quaternion.LookRotation(Axis(mid), up));
            float scale = ModelLength(name) / len;
            var grip = b.center; grip[lng] = lo + gripAt * len;
            return (rot, -(rot * grip) * scale, scale);
        }
    }
}
