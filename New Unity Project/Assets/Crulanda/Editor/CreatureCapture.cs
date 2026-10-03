using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// Batch (tools/validation/run_method.ps1 -Graphics): the animals as models (2026-10-03; ModelBeast) posed in edit mode and drawn
    /// offscreen, side on, for a look without a player build. Writes hel/work/ui-captures/figures/beasts-*.png: the line-up with a
    /// person for scale, the old bodies beside the new, and each animal's motions.
    /// </summary>
    public static class CreatureCapture
    {
        const string Out = @"C:\Users\chris\Documents\Codex\2026-09-28\hel\work\ui-captures\figures";
        static Camera cam;
        static Transform stage;

        public static void Run()
        {
            Directory.CreateDirectory(Out); CharacterImport.Prepare();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(38, -32, 0);
            sun.intensity = 1.2f; sun.color = new Color(1, .95f, .86f); sun.shadows = LightShadows.Soft; sun.shadowStrength = .7f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.56f, .62f, .74f); RenderSettings.ambientEquatorColor = new Color(.46f, .46f, .44f); RenderSettings.ambientGroundColor = new Color(.24f, .22f, .19f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.localScale = Vector3.one * 8;
            var gm = new Material(Shader.Find("Standard")) { color = new Color(.36f, .42f, .28f) }; gm.SetFloat("_Glossiness", .05f); ground.GetComponent<Renderer>().sharedMaterial = gm;
            cam = new GameObject("Camera").AddComponent<Camera>(); cam.fieldOfView = 28; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.62f, .7f, .8f);
            cam.nearClipPlane = .05f; cam.farClipPlane = 200;
            QualitySettings.shadowDistance = 60;

            Row("beasts-1", new[] { P(ActorLook.Warrior, 0, null, 0), B(ActorLook.Wolf, 0, "idle", .5f), B(ActorLook.Wolf, 1, "idle", 1.1f), Named(B(ActorLook.Wolf, 0, "idle", .8f), "Old Whitefoot (elite)"), B(ActorLook.Stag, 0, "idle", .5f), B(ActorLook.Stag, 1, "idle", .8f), D(.2f, "eat", 1), D(.8f, "idle", .4f) }, 1.9f);
            Row("beasts-before", new[] { Old(ActorLook.Wolf, 0), B(ActorLook.Wolf, 0, "idle", .5f), Old(ActorLook.Wolf, 1), B(ActorLook.Wolf, 1, "idle", .5f), Old(ActorLook.Stag, 0), B(ActorLook.Stag, 0, "idle", .5f), Old(ActorLook.Stag, 1), B(ActorLook.Stag, 1, "idle", .5f) }, 1.7f);
            Row("beasts-round", new[] { F(B(ActorLook.Wolf, 0, "idle", .5f)), B(ActorLook.Wolf, 0, "idle", .5f), F(B(ActorLook.Stag, 0, "idle", .5f)), B(ActorLook.Stag, 0, "idle", .5f), F(B(ActorLook.Stag, 1, "idle", .5f)), B(ActorLook.Stag, 1, "idle", .5f) }, 2f);
            Row("beasts-wolf", new[] { B(ActorLook.Wolf, 0, "walk", .3f), B(ActorLook.Wolf, 0, "run", .2f), B(ActorLook.Wolf, 0, "attack", .35f), B(ActorLook.Wolf, 0, "hitL", .2f), B(ActorLook.Wolf, 0, "headlow", .5f), B(ActorLook.Wolf, 0, "death", 10) }, 1.8f);
            Row("beasts-stag", new[] { B(ActorLook.Stag, 0, "walk", .3f), B(ActorLook.Stag, 0, "run", .25f), B(ActorLook.Stag, 0, "attack", .45f), B(ActorLook.Stag, 0, "eat", 1f), B(ActorLook.Stag, 1, "kick", .4f), B(ActorLook.Stag, 0, "death", 10) }, 2.6f);
            Debug.Log("CREATURE_CAPTURE_DONE");
        }

        sealed class Spec { public ActorLook look; public int variant; public string slot; public float time; public bool person, old, deer, flat; public float r; public string name; }
        /// <summary>The same, its mesh left as the file has it (ModelBeast.Round off).</summary>
        static Spec F(Spec s) { s.flat = true; s.name += " (file)"; return s; }
        static Spec Named(Spec s, string name) { s.name = name; return s; }
        static Spec P(ActorLook look, int variant, string slot, float time) { return new Spec { look = look, variant = variant, person = true, time = time, name = look.ToString() }; }
        static Spec B(ActorLook look, int variant, string slot, float time) { return new Spec { look = look, variant = variant, slot = slot, time = time, name = look + " " + variant }; }
        static Spec Old(ActorLook look, int variant) { return new Spec { look = look, variant = variant, old = true, name = "Old " + look + " " + variant }; }
        static Spec D(float r, string slot, float time) { return new Spec { deer = true, r = r, slot = slot, time = time, name = "Hill deer " + r }; }

        /// <summary>A row of animals side on (facing +X), <paramref name="gap"/> m apart, drawn from the side, and the shot saved.</summary>
        static void Row(string shot, Spec[] specs, float gap)
        {
            if (stage != null) Object.DestroyImmediate(stage.gameObject);
            stage = new GameObject("Stage").transform;
            float x0 = -(specs.Length - 1) * gap / 2, top = 1;
            for (int i = 0; i < specs.Length; i++)
            {
                var s = specs[i];
                var go = new GameObject(s.name); go.transform.SetParent(stage, false);
                if (s.deer)
                {
                    go.transform.SetPositionAndRotation(new Vector3(x0 + i * gap, 0, 0), Quaternion.Euler(0, 90, 0));
                    var c = CritterBody.Build(go.transform, "deer", s.r, 3 + i, 0);
                    if (c.Model != null) { c.Model.Sample(s.slot, s.time); top = Mathf.Max(top, c.Model.Height + .6f); }
                    continue;
                }
                go.transform.SetPositionAndRotation(new Vector3(x0 + i * gap, 1, 0), Quaternion.Euler(0, s.person ? 180 : 90, 0));
                new GameObject("Body").transform.SetParent(go.transform, false);
                bool models = ActorVisual.Models, round = ModelBeast.Round; if (s.old) ActorVisual.Models = false; if (s.flat) ModelBeast.Round = false;
                var v = ActorVisual.Attach(go, s.look, s.variant);
                ActorVisual.Models = models; ModelBeast.Round = round;
                if (s.person) v.Preview(ActorPose.None, 0, s.time);
                else if (v.BeastModel != null) { v.BeastPreview(s.slot, s.time); top = Mathf.Max(top, v.BeastModel.Height + (v.BeastModel.Kind == "Stag" ? .7f : .1f)); }
            }
            top = Mathf.Max(top, 1.9f);
            float width = specs.Length * gap, halfH = Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad), aspect = 16f / 9;
            float dist = Mathf.Max((width / 2) / (halfH * aspect), (top * .62f) / halfH);
            var at = new Vector3(0, top * .45f, 0);
            Shot(shot, at + new Vector3(0, .5f, -dist), at);
        }
        static void Shot(string name, Vector3 eye, Vector3 at)
        {
            const int w = 1600, h = 900;
            cam.transform.position = eye; cam.transform.LookAt(at);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        }
    }
}
