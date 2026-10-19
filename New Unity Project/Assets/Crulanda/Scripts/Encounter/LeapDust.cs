using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Her landing (tune-8, Chris: "leaps are too quick and not properly telegraphed"): a ring of grey dust and silt thrown up round where she
    /// comes down, spreading and fading over a second, and the view shaken a moment when you are near. Nothing left behind.
    /// </summary>
    [DefaultExecutionOrder(10000)]   // after the camera has placed itself: the shake is only this frame's
    public sealed class LeapDust : MonoBehaviour
    {
        public const float Seconds = 1.2f;
        readonly List<(Transform puff, Vector3 dir)> puffs = new List<(Transform, Vector3)>();
        Material mat; float born, shake;

        public static LeapDust Burst(Vector3 at, float radius, float shakeSeconds)
        {
            var go = new GameObject("Her landing's dust"); go.transform.position = at;
            var d = go.AddComponent<LeapDust>(); d.born = Time.time; d.shake = Mathf.Max(0, shakeSeconds);
            var shader = Shader.Find("Sprites/Default");
            d.mat = new Material(shader != null ? shader : Shader.Find("Standard")) { name = "Landing dust", color = new Color(.55f, .52f, .48f, .6f) };
            for (int i = 0; i < 16; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Sphere); p.name = "Dust"; var c = p.GetComponent<Collider>(); if (c != null) Destroy(c);
                p.transform.SetParent(go.transform, false);
                var dir = Quaternion.Euler(0, i * 360f / 16 + Random.Range(-8f, 8f), 0) * Vector3.forward;
                p.transform.localPosition = dir * radius * .4f + Vector3.up * .3f; p.transform.localScale = Vector3.one * .6f;
                var r = p.GetComponent<MeshRenderer>(); r.sharedMaterial = d.mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                d.puffs.Add((p.transform, dir * radius));
            }
            return d;
        }
        void LateUpdate()
        {
            float k = (Time.time - born) / Seconds;
            if (k >= 1) { Destroy(gameObject); return; }
            foreach (var p in puffs) { if (p.puff == null) continue; p.puff.localPosition = p.dir * (.4f + .8f * k) + Vector3.up * (.3f + 1.2f * k); p.puff.localScale = Vector3.one * (.6f + 1.6f * k); }
            if (mat != null) mat.color = new Color(.55f, .52f, .48f, .6f * (1 - k));
            float s = Time.time - born; var cam = Camera.main;
            if (cam != null && s < shake) cam.transform.position += Random.insideUnitSphere * .18f * (1 - s / shake);
        }
        void OnDestroy() { if (mat != null) Destroy(mat); }
    }
}
