using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// A look at the kit props (2026-10-07): every model in ZoneBuilder.KitProps stood in a row at the height the builders give it,
    /// a metre rule beside each, from three-quarters front, to hel/work/ui-captures/props-row.png (run_method PropCapture.Render
    /// -Graphics). For checking a model's size and which way it faces before it goes into a village.
    /// </summary>
    public static class PropCapture
    {
        public static void Render()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var key = new GameObject("Key").AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.2f; key.color = new Color(1, .95f, .86f); key.transform.rotation = Quaternion.Euler(40, 30, 0);
            var fill = new GameObject("Fill").AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = .5f; fill.color = new Color(.7f, .8f, 1); fill.transform.rotation = Quaternion.Euler(-10, 210, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.4f, .4f, .44f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.transform.localScale = new Vector3(20, 1, 2); floor.transform.position = new Vector3(ZoneBuilder.KitProps.Length, 0, 0);
            floor.GetComponent<Renderer>().sharedMaterial.color = new Color(.35f, .33f, .3f);
            var root = new GameObject("Props").transform; int n = 0, missing = 0;
            for (int i = 0; i < ZoneBuilder.KitProps.Length; i++)
            {
                var (path, height) = ZoneBuilder.KitProps[i]; var foot = new Vector3(i * 2.2f, 0, 0);
                var go = ZoneBuilder.ModelProp(root, path, foot, 0, height);
                if (go == null) { missing++; Debug.LogWarning("PROP_CAPTURE missing " + path); }
                else
                {
                    n++; var raw = ZoneBuilder.PropBounds(go.transform); var world = new Bounds(); bool any = false;
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (!any) { world = r.bounds; any = true; } else world.Encapsulate(r.bounds); }
                    Debug.Log("PROP " + path + " raw " + raw.size.ToString("0.000") + " scale " + go.transform.localScale.x.ToString("0.0000") + " stands " + world.size.ToString("0.00") + " foot " + (world.min.y - foot.y).ToString("0.00") + " wanted " + height);
                    var mats = new System.Text.StringBuilder();
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) foreach (var m in r.sharedMaterials) if (m != null) mats.Append(m.name + "[" + (m.shader != null ? m.shader.name : "?") + ", tex " + (m.mainTexture != null ? m.mainTexture.name : "none") + ", q " + m.renderQueue + "] ");
                    Debug.Log("PROPMAT " + path + " " + mats);
                }
                // A metre rule (a thin white post) and an arrow on the ground pointing the model's -Z, where a front would be.
                var rule = GameObject.CreatePrimitive(PrimitiveType.Cube); rule.transform.position = foot + new Vector3(.9f, .5f, 0); rule.transform.localScale = new Vector3(.04f, 1, .04f);
                var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube); arrow.transform.position = foot + new Vector3(0, .01f, -1.1f); arrow.transform.localScale = new Vector3(.08f, .02f, .5f);
                arrow.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(1, .4f, .2f) };
            }
            var cam = new GameObject("Camera").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.12f, .13f, .16f);
            int w = 240 * ZoneBuilder.KitProps.Length, h = 520; w = Mathf.Min(w, 8192);
            cam.orthographic = true; cam.aspect = (float)w / h; cam.farClipPlane = 60;
            cam.orthographicSize = ZoneBuilder.KitProps.Length * 2.2f / 2 / cam.aspect;   // the row fills the width: 240 px a prop
            float mid = (ZoneBuilder.KitProps.Length - 1) * 1.1f;
            cam.transform.position = new Vector3(mid, cam.orthographicSize + 1.2f, -9); cam.transform.rotation = Quaternion.Euler(12, 0, 0);
            var rt = new RenderTexture(w, h, 24) { antiAliasing = 4 }; cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = null;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, @"..\..\ui-captures")); Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, "props-row.png"), tex.EncodeToPNG());
            // And one from the front, straight on, so a model's facing shows.
            cam.transform.position = new Vector3(mid, cam.orthographicSize - .3f, -9); cam.transform.rotation = Quaternion.identity;
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(dir, "props-row-front.png"), tex.EncodeToPNG());
            Debug.Log("PROP_CAPTURE_DONE " + n + " placed, " + missing + " missing, " + dir);
        }
    }
}
