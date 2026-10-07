using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// The Stylized Megapack's buildings, one picture each (2026-10-07, for Chris to compare with the painted houses): each model on a
    /// grass-coloured ground in afternoon light, from three-quarters front and a little above, framed to its bounds, to
    /// hel/work/ui-captures/houses/&lt;name&gt;.png (run_method HouseCapture.Render -Graphics).
    /// </summary>
    public static class HouseCapture
    {
        static readonly string[] Houses = {
            "Buildings/Buidling_1", "Buildings/Buidling_2", "Buildings/Buiilding_6_1", "Buildings/Buiilding_6_2", "Buildings/Building 10_1",
            "Buildings/Building_11", "Buildings/Building_11_2", "Buildings/Building_5", "Buildings/Building_8", "Buildings/Building_9_1",
            "Buildings/Building 3Base", "Buildings/Stable_1", "Buildings/Tower_1", "Buildings/Windmill", "Barn_1", "Stall_1",
        };
        public static void Render()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.15f; sun.color = new Color(1, .95f, .84f);
            sun.transform.rotation = Quaternion.Euler(42, -35, 0); sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.62f, .7f, .8f); RenderSettings.ambientEquatorColor = new Color(.5f, .55f, .5f); RenderSettings.ambientGroundColor = new Color(.3f, .33f, .25f);
            QualitySettings.shadowDistance = 120;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.localScale = Vector3.one * 20;
            ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(.38f, .52f, .26f) };
            var cam = new GameObject("Camera").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.66f, .76f, .86f);
            cam.fieldOfView = 30; cam.farClipPlane = 400;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, @"..\..\ui-captures\houses")); Directory.CreateDirectory(dir);
            int w = 900, h = 640, n = 0;
            var rt = new RenderTexture(w, h, 24) { antiAliasing = 8 }; cam.targetTexture = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            foreach (var name in Houses)
            {
                var root = new GameObject("House").transform;
                var go = ZoneBuilder.ModelProp(root, "Megapack/Models/" + name, Vector3.zero, 0, 1);   // height 1 for now: rescaled below to its own size
                if (go == null) { Debug.LogWarning("HOUSE_CAPTURE missing " + name); Object.DestroyImmediate(root.gameObject); continue; }
                Object.DestroyImmediate(root.gameObject);
                // At its own scale (the pack is in metres): placed without the height fit, bottom on the ground.
                var src = ZoneBuilder.PropSource("Megapack/Models/" + name); var inst = Object.Instantiate(src); inst.transform.position = Vector3.zero;
                var b = new Bounds(); bool any = false;
                foreach (var r in inst.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                inst.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z); b.center = new Vector3(0, b.extents.y, 0);
                float radius = b.extents.magnitude;
                // Two views: from behind and to one side (215 degrees) and its front, the -Z face a builder puts its door on (25 degrees).
                foreach (var (yaw, tag) in new[] { (215f, ""), (25f, "-front") })
                {
                    var look = Quaternion.Euler(14, yaw, 0);
                    cam.transform.rotation = look; cam.transform.position = b.center - look * Vector3.forward * (radius / Mathf.Sin(cam.fieldOfView * .5f * Mathf.Deg2Rad) * .78f);
                    cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(dir, name.Replace("Buildings/", "").Replace(' ', '_') + tag + ".png"), tex.EncodeToPNG());
                }
                Debug.Log("HOUSE " + name + " size " + b.size.ToString("0.0"));
                Object.DestroyImmediate(inst); n++;
            }
            Debug.Log("HOUSE_CAPTURE_DONE " + n + " " + dir);
        }
    }
}
