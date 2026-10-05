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
            // The farm animals (2026-10-03): a villager for scale, the horse's four coats, the donkey's two, the cow's four.
            Row("beasts-farm", new[] { P(ActorLook.Villager, 23, null, 0), K("horse", .1f, "idle", .5f), K("horse", .3f, "eat", 1), K("horse", .6f, "idle", .9f), K("horse", .9f, "walk", .3f),
                K("donkey", .2f, "idle", .4f), K("donkey", .7f, "eat", 1.4f) }, 2.8f);
            Row("beasts-cows", new[] { P(ActorLook.Villager, 23, null, 0), K("cow", .1f, "idle", .5f), K("cow", .3f, "eat", 2), K("cow", .6f, "idle", 1.2f), K("cow", .9f, "walk", .4f) }, 3f);
            // The boar (CraftPix, moved by the code): the old one beside the new, the kinds, and its motions.
            Row("beasts-boar", new[] { Old(ActorLook.Boar, 0), Named(B(ActorLook.Boar, 0, "idle", .5f), "Wild boar"), Named(B(ActorLook.Boar, 0, "idle", .5f), "Carrion boar"),
                Named(B(ActorLook.Boar, 0, "idle", .5f), "Mire boar"), Named(B(ActorLook.Boar, 0, "idle", .5f), "Rockhide boar"), Named(B(ActorLook.Boar, 0, "idle", .5f), "Old Scree-Tusk (elite)"), P(ActorLook.Warrior, 0, null, 0) }, 1.6f);
            Row("beasts-boar-moves", new[] { B(ActorLook.Boar, 0, "walk", .1f), B(ActorLook.Boar, 0, "walk", .3f), B(ActorLook.Boar, 0, "run", .1f), B(ActorLook.Boar, 0, "eat", 1),
                B(ActorLook.Boar, 0, "attack", .1f), B(ActorLook.Boar, 0, "attack", .26f), B(ActorLook.Boar, 0, "headlow", 1), B(ActorLook.Boar, 0, "death", 2) }, 1.5f);
            // The Keepers as treants (2026-10-04): the old bark figure beside them, living and withered, the great ones, and their motions.
            Row("beasts-treants", new[] { P(ActorLook.Warrior, 0, null, 0), Named(B(ActorLook.Keeper, 0, "idle", .5f), "Oak-Bane"), Named(B(ActorLook.Keeper, 2, "idle", .9f), "Willow-Whisper"),
                Named(B(ActorLook.Keeper, 1, "idle", .3f), "Withered Keeper"), Named(B(ActorLook.Keeper, 1, "idle", .7f), "Withered Keeper (elite)"), Named(B(ActorLook.Keeper, 1, "idle", .5f), "Greyheart"),
                Named(B(ActorLook.Keeper, 1, "idle", .4f), "The Hollow Root-Warden") }, 2.6f);
            Row("beasts-treant-moves", new[] { B(ActorLook.Keeper, 0, "walk", .3f), B(ActorLook.Keeper, 0, "walk", .9f), B(ActorLook.Keeper, 0, "attack", .7f), B(ActorLook.Keeper, 0, "attack2", .5f),
                B(ActorLook.Keeper, 0, "attack3", .6f), B(ActorLook.Keeper, 1, "death", 10), B(ActorLook.Keeper, 1, "death2", 10), B(ActorLook.Keeper, 1, "death3", 10) }, 2.8f);
            // The new creatures in the game (2026-10-04): bears (Blink), spiders, the drowned dead and the crows (ChillLands' Ashen Marches).
            Row("beasts-bears", new[] { P(ActorLook.Warrior, 0, null, 0), Named(B(ActorLook.Bear, 0, "idle", .5f), "Brown bear"), Named(B(ActorLook.Bear, 0, "eat", 1), "Brown bear"),
                Named(B(ActorLook.Bear, 0, "idle", .8f), "Old Hazelmaw"), Named(B(ActorLook.Bear, 0, "walk", .3f), "Brown bear"), Named(B(ActorLook.Bear, 0, "run", .2f), "Brown bear"),
                Named(B(ActorLook.Bear, 0, "attack", .4f), "Brown bear"), Named(B(ActorLook.Bear, 0, "attack2", .5f), "Brown bear"), Named(B(ActorLook.Bear, 0, "death", 10), "Brown bear") }, 2.6f);
            Row("beasts-dead", new[] { P(ActorLook.Warrior, 0, null, 0), Named(B(ActorLook.Spider, 0, "idle", .5f), "Charnel spider"), Named(B(ActorLook.Spider, 0, "walk", .3f), "Basalt spider"),
                Named(B(ActorLook.Spider, 0, "attack", .4f), "Canopy spider"), Named(B(ActorLook.Spider, 0, "death", 10), "Canopy spider"),
                Named(B(ActorLook.Skeleton, 0, "idle", .5f), "Drowned dead"), Named(B(ActorLook.Skeleton, 0, "walk", .4f), "Drowned dead"), Named(B(ActorLook.Skeleton, 0, "attack", .4f), "Drowned dead"),
                Named(B(ActorLook.Skeleton, 0, "death", .35f), "Drowned dead"), Named(B(ActorLook.Skeleton, 0, "death", 10), "Drowned dead"), K("crow", .3f, "idle", .5f), K("crow", .6f, "run", .3f), K("crow", .8f, "death", 10) }, 1.9f);
            Debug.Log("CREATURE_CAPTURE_DONE");
        }

        sealed class Spec { public ActorLook look; public int variant; public string slot; public float time; public bool person, old, deer, flat; public float r; public string name, kind = "deer", raw; }
        /// <summary>The same, its mesh left as the file has it (ModelBeast.Round off).</summary>
        static Spec F(Spec s) { s.flat = true; s.name += " (file)"; return s; }
        static Spec Named(Spec s, string name) { s.name = name; return s; }
        static Spec P(ActorLook look, int variant, string slot, float time) { return new Spec { look = look, variant = variant, person = true, time = time, name = look.ToString() }; }
        static Spec B(ActorLook look, int variant, string slot, float time) { return new Spec { look = look, variant = variant, slot = slot, time = time, name = look + " " + variant }; }
        static Spec Old(ActorLook look, int variant) { return new Spec { look = look, variant = variant, old = true, name = "Old " + look + " " + variant }; }
        /// <summary>A model as it comes (ModelBeast.Build, no look), <paramref name="height"/> m tall.</summary>
        static Spec R(string kind, string slot, float time, float height) { return new Spec { raw = kind, slot = slot, time = time, r = height, name = kind + " " + slot }; }
        static Spec D(float r, string slot, float time) { return new Spec { deer = true, r = r, slot = slot, time = time, name = "Hill deer " + r }; }
        /// <summary>A village or game animal of this kind (CritterBody), coloured by <paramref name="r"/>.</summary>
        static Spec K(string kind, float r, string slot, float time) { return new Spec { deer = true, kind = kind, r = r, slot = slot, time = time, name = kind + " " + r }; }

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
                if (s.raw != null)
                {
                    go.transform.SetPositionAndRotation(new Vector3(x0 + i * gap, 0, 0), Quaternion.Euler(0, 90, 0));
                    var m = ModelBeast.Build(go.transform, s.raw, 0, s.r);
                    if (m != null) { m.Sample(s.slot, s.time); top = Mathf.Max(top, s.r + .3f); }
                    else Debug.Log("CREATURE_CAPTURE no model " + s.raw);
                    continue;
                }
                if (s.deer)
                {
                    go.transform.SetPositionAndRotation(new Vector3(x0 + i * gap, 0, 0), Quaternion.Euler(0, 90, 0));
                    var c = CritterBody.Build(go.transform, s.kind, s.r, 3 + i, 0);
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
