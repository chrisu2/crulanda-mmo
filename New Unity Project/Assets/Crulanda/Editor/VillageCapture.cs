using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// The Medieval Village MegaKit's houses as VillageKit.House assembles them (2026-10-07, for Chris to compare with Oakhaven's
    /// painted houses): three sizes, front and back, in the house capture's afternoon light, to hel/work/ui-captures/village/*.png
    /// (run_method VillageCapture.Render -Graphics). Also one with the walls turned round, to see which face is the kit's outside.
    /// </summary>
    public static class VillageCapture
    {
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
            var cam = new GameObject("Camera").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.66f, .76f, .86f); cam.fieldOfView = 30; cam.farClipPlane = 400;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, @"..\..\ui-captures\village")); Directory.CreateDirectory(dir);
            int w = 900, h = 640; var rt = new RenderTexture(w, h, 24) { antiAliasing = 8 }; cam.targetTexture = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var houses = new (string name, int bays, int depth, int storeys, int door, bool faceOut)[] {
                ("cottage-4x3-1", 4, 3, 1, 1, true), ("house-4x3-2", 4, 3, 2, 1, true), ("house-5x4-2", 5, 4, 2, 2, true), ("flipped-4x3-2", 4, 3, 2, 1, false) };
            int n = 0;
            var probe = new GameObject("Probe").transform;
            foreach (var piece in new[] { "Wall_Plaster_Straight", "Wall_UnevenBrick_Door_Round", "Corner_Exterior_Wood", "Floor_WoodDark", "Roof_RoundTiles_6x8", "Prop_Chimney" })
                Debug.Log("VILLAGE_PIECE " + piece + " " + VillageKit.PieceBounds(probe, piece).min.ToString("0.00") + " .. " + VillageKit.PieceBounds(probe, piece).max.ToString("0.00"));
            Object.DestroyImmediate(probe.gameObject);
            foreach (var hs in houses)
            {
                var holder = new GameObject("Holder").transform;
                var house = VillageKit.House(holder, hs.bays, hs.depth, hs.storeys, hs.door, VillageKit.Style.Oakhaven, hs.faceOut);
                var b = new Bounds(); bool any = false;
                foreach (var r in house.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                float radius = b.extents.magnitude;
                foreach (var (yaw, tag) in new[] { (25f, "-front"), (215f, "-back") })
                {
                    var look = Quaternion.Euler(14, yaw, 0);
                    cam.transform.rotation = look; cam.transform.position = b.center - look * Vector3.forward * (radius / Mathf.Sin(cam.fieldOfView * .5f * Mathf.Deg2Rad) * .8f);
                    cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(dir, hs.name + tag + ".png"), tex.EncodeToPNG());
                }
                Debug.Log("VILLAGE " + hs.name + " size " + b.size.ToString("0.0"));
                Object.DestroyImmediate(holder.gameObject); n++;
            }
            Debug.Log("VILLAGE_CAPTURE_DONE " + n + " " + dir);
        }
    }
}
