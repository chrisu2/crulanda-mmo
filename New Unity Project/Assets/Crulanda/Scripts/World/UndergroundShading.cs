using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Underground there is no sun (Chris, 2026-10-15, of the Flooded Working's pool: "looks bad with these white ripples"): the water
    /// shader lit its painted highlights, the sun's glints and the caustics on the bed by the sun even in a cave, as white streaks on
    /// black water. Each frame this eases the global _CrulandaUnderground to 1 while the camera is inside a cave (Hollow.InsideAny)
    /// and to 0 outside, and the water shader drops those three by it.
    /// </summary>
    public sealed class UndergroundShading : MonoBehaviour
    {
        static readonly int Id = Shader.PropertyToID("_CrulandaUnderground");
        float now;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            Shader.SetGlobalFloat(Id, 0);
            var go = new GameObject("Underground shading") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go); go.AddComponent<UndergroundShading>();
        }
        void Update()
        {
            var cam = Camera.main; bool inside = cam != null && Hollow.All.Count > 0 && Hollow.UndergroundAt(cam.transform.position, 1);
            now = Mathf.MoveTowards(now, inside ? 1 : 0, Time.unscaledDeltaTime * 2);
            Shader.SetGlobalFloat(Id, now);
        }
    }
}
