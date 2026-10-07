using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// Batch (tools/validation/run_method.ps1 -Graphics): the bag icons of the model weapons and shields (2026-10-05; the painted
    /// icons are drawn by tools/art/make_icons.py, which cannot draw a model). Each model is drawn alone, lit from the upper left
    /// with a cool rim from behind, laid corner to corner (a blade from lower left to upper right; a shield face on), on the
    /// painted icons' dark vignette, 64 px: Resources/Icons/model/&lt;prefab&gt;.png for every model a look may name (generated gear
    /// finds its icon there, IconDb.Item), and Resources/Icons/item/&lt;id&gt;.png for each named item that wears one.
    /// </summary>
    public static class ModelIcons
    {
        const int Size = 64, Super = 4;
        public static void Render()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Icon camera").AddComponent<Camera>(); cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0); cam.nearClipPlane = .01f; cam.farClipPlane = 20; cam.transform.position = new Vector3(0, 0, -5);
            var key = new GameObject("Key").AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.25f; key.color = new Color(1, .95f, .86f); key.transform.rotation = Quaternion.Euler(35, 35, 0);
            var rim = new GameObject("Rim").AddComponent<Light>(); rim.type = LightType.Directional; rim.intensity = .7f; rim.color = new Color(.6f, .75f, 1); rim.transform.rotation = Quaternion.Euler(-20, 200, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.42f, .42f, .46f);
            string root = Path.Combine(Application.dataPath, "Crulanda/Resources/Icons");
            Directory.CreateDirectory(Path.Combine(root, "model"));
            int n = 0;
            foreach (var name in Models()) { if (Draw(cam, name, IsShield(name), Path.Combine(root, "model", name + ".png"), IsHelm(name))) n++; }
            // Named items that wear a model: their own icon (IconCoverageTests asks every item for one).
            var named = new System.Text.RegularExpressions.Regex(@"\{""id"": ""([^""]+)"", ""look"": ""(model\.[a-z]+):([^/]+)/");
            foreach (var file in Directory.GetFiles(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Items"), "loot.*.json"))
                foreach (System.Text.RegularExpressions.Match m in named.Matches(File.ReadAllText(file)))
                    if (Draw(cam, m.Groups[3].Value, m.Groups[2].Value == "model.shield", Path.Combine(root, "item", m.Groups[1].Value.Replace('.', '-') + ".png"), m.Groups[2].Value == "model.helm")) n++;
            Debug.Log("MODEL_ICONS_DONE " + n);
        }
        static IEnumerable<string> Models()
        {
            foreach (var s in GearLooks.ModelWeapons) yield return s;
            foreach (var s in GearLooks.ModelTall) yield return s;
            foreach (var s in GearLooks.ModelShields) yield return s;
            foreach (var s in GearLooks.ModelHelms) yield return s;
        }
        static bool IsShield(string name) { return System.Array.IndexOf(GearLooks.ModelShields, name) >= 0; }
        static bool IsHelm(string name) { return System.Array.IndexOf(GearLooks.ModelHelms, name) >= 0; }

        static bool Draw(Camera cam, string name, bool shield, string path, bool helm = false)
        {
            var src = Resources.Load<GameObject>("Weapons/" + name); if (src == null) { Debug.LogWarning("MODEL_ICONS no prefab " + name); return false; }
            var holder = new GameObject("Holder").transform; var go = Object.Instantiate(src, holder, false);
            // Laid as in the hand (ActorVisual's fit), then turned: a weapon corner to corner, a shield face on.
            var b = Bounds(go.transform); var size = b.size; int lng = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
            int thin = size.x <= size.y && size.x <= size.z ? 0 : size.y <= size.z ? 1 : 2;
            Vector3 Axis(int i) { var v = Vector3.zero; v[i] = 1; return v; }
            Quaternion toView = helm ? Quaternion.Euler(15, 180, 0) : shield ? Quaternion.FromToRotation(Axis(thin), Vector3.back) : Quaternion.Euler(0, 0, -45) * Quaternion.FromToRotation(Axis(lng), Vector3.up) * Quaternion.Euler(0, 0, 0);
            if (!shield && !helm) toView = Quaternion.AngleAxis(25, toView * Axis(lng)) * toView;   // a helm faces the camera (2026-10-07), a little from above   // a quarter turn about its length so the flat catches the light
            go.transform.localRotation = toView * go.transform.localRotation; go.transform.localPosition = -(toView * b.center);   // (the prefab's own turn kept: the bounds were measured with it)
            var view = Bounds(holder); float half = Mathf.Max(view.extents.x, view.extents.y) * 1.12f; cam.orthographicSize = half; cam.transform.position = new Vector3(view.center.x, view.center.y, -5);
            int px = Size * Super; var rt = new RenderTexture(px, px, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var big = new Texture2D(px, px, TextureFormat.RGBA32, false); big.ReadPixels(new Rect(0, 0, px, px), 0, 0); big.Apply();
            RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(holder.gameObject);
            var icon = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    // The model, downsampled, over the painted icons' ground: a dark vignette, a little warmer at the middle.
                    Color sum = Color.clear;
                    for (int j = 0; j < Super; j++) for (int i = 0; i < Super; i++) sum += big.GetPixel(x * Super + i, y * Super + j);
                    sum /= Super * Super;
                    float dx = (x + .5f) / Size - .5f, dy = (y + .5f) / Size - .5f, r = Mathf.Sqrt(dx * dx + dy * dy) / .7071f;
                    var ground = Color.Lerp(new Color(.2f, .17f, .15f), new Color(.05f, .045f, .05f), Mathf.SmoothStep(0, 1, r));
                    float a = Mathf.Clamp01(sum.a);
                    var c = a > 0 ? new Color(sum.r / Mathf.Max(a, 1e-4f), sum.g / Mathf.Max(a, 1e-4f), sum.b / Mathf.Max(a, 1e-4f)) : Color.black;
                    icon.SetPixel(x, y, new Color(Mathf.Lerp(ground.r, c.r, a), Mathf.Lerp(ground.g, c.g, a), Mathf.Lerp(ground.b, c.b, a), 1));
                }
            icon.Apply(); File.WriteAllBytes(path, icon.EncodeToPNG());
            Object.DestroyImmediate(big); Object.DestroyImmediate(icon);
            return true;
        }
        static Bounds Bounds(Transform t)
        {
            var b = new Bounds(); bool any = false;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return b;
        }
    }
}
