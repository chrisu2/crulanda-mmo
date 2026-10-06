using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A class kit piece that is a model (Phase 5.1b, 2026-10-05): the Ranger's bow, from the Asset Store packs in
    /// Resources/Weapons, held in the left hand and slung across the back like the Druid's staff, yielding to a main-hand item
    /// (ActorVisual.Gear.cs RefreshHeld). Laid in the hand by the gear models' own fit (ActorVisual.GearModels.cs).
    /// </summary>
    public sealed partial class ActorVisual
    {
        /// <summary>The prefab under a holder at <paramref name="pos"/>/<paramref name="euler"/> on <paramref name="parent"/>, fitted to the weapon frame; null when the prefab is not in the build.</summary>
        /// <paramref name="gripAt"/> is where along its length the hand holds it (the weapon fit puts a one-hander's grip 12% up; a bow is held at its middle).
        Transform HeldModel(string prefab, Transform parent, Vector3 pos, Vector3 euler, float scale = 1, float gripAt = .12f)
        {
            var src = Resources.Load<GameObject>("Weapons/" + prefab); if (src == null) return null;
            var holder = new GameObject("Class " + prefab).transform; holder.SetParent(parent, false);
            holder.localPosition = pos; holder.localRotation = Quaternion.Euler(euler); holder.localScale = Vector3.one * scale;
            var go = Instantiate(src, holder, false); go.name = "Model " + prefab;
            foreach (var c in go.GetComponentsInChildren<Component>(true))
                if (!(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer)) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
            var fit = ModelFit(go.transform, false, prefab);
            go.transform.localRotation = fit.Item1; go.transform.localScale = Vector3.one * fit.Item3;
            go.transform.localPosition = fit.Item2 + Vector3.down * ((gripAt - .12f) * ModelLength(prefab));   // slide the grip point to the hand
            return holder;
        }
    }
}
