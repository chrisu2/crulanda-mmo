using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// Batch (tools/validation/run_method.ps1 -Graphics): the modelled people (playtest note 12) posed in edit mode
    /// (ActorVisual.Preview) and drawn offscreen, for a look without a player build. Writes hel/work/ui-captures/figures/*.png:
    /// the classes and factions, the village trades, the poses, a full armour kit front and side, and faces.
    /// </summary>
    public static class FigureCapture
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
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.localScale = Vector3.one * 6;
            var gm = new Material(Shader.Find("Standard")) { color = new Color(.36f, .42f, .28f) }; gm.SetFloat("_Glossiness", .05f); ground.GetComponent<Renderer>().sharedMaterial = gm;
            cam = new GameObject("Camera").AddComponent<Camera>(); cam.fieldOfView = 28; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.62f, .7f, .8f);
            cam.nearClipPlane = .05f; cam.farClipPlane = 200;
            QualitySettings.shadowDistance = 40;

            var db = new ItemDatabase(); var looks = GearLooks.Parse(Resources.Load<TextAsset>("Gear/looks").text);
            Row("people-1", new[] { P(ActorLook.Warrior), P(ActorLook.Druid), P(ActorLook.Healer), P(ActorLook.Collector), P(ActorLook.Warden), P(ActorLook.Outrider) });
            Row("people-2", new[] { P(ActorLook.Cultist), P(ActorLook.Deserter, 1, null, "Deserter a"), P(ActorLook.Deserter, 2, null, "Deserter bb"), P(ActorLook.BanditKing), P(ActorLook.Hollow), P(ActorLook.Sentry), P(ActorLook.Paladin), P(ActorLook.Ranger), P(ActorLook.Mage) });
            Row("trades-1", new[] { V("blacksmith", "Brannoc Vell", 3), V("merchant", "Wil Carder", 8), V("baker", "Ama Rusk", 13), V("henwife", "Hedda Thorne", 40), V("farmer", "Garet Moss", 23), V("hunter", "Corwin Ashby", 28) });
            Row("trades-2", new[] { V("herbalist", "Lisbet Crane", 33), V("miller", "Aldo Crisp", 38), V("elder", "Old Tobin", 43), V("stranger", "A Stranger", 48), V("pilgrim", "Pilgrim", 53), V("innkeeper", "Maud Tanner", 58) });
            Row("trades-3", new[] { V("leatherworker", "Edda Pell", 63), V("lumberjack", "Hob Linden", 68), V("skinner", "Osk Farrow", 73), V("warden", "Fen Walker", 78), V("drinker", "Jory", 83), V("gossip", "Grete Lowe", 88), V("child", "Pim", 93) });
            // Poses, side on (facing +X).
            Row("poses-1", new[] { Po(ActorPose.None, 1.5f, .3f), Po(ActorPose.None, 3.6f, .2f), Po(ActorPose.None, 6.5f, .2f), Po(ActorPose.Sneak, 1.2f, .5f), Po(ActorPose.Sneak, 0, .5f), Po(ActorPose.Swim, 1.6f, .4f), Po(ActorPose.Swim, 0, .8f) }, 90);
            Row("poses-2", new[] { Po(ActorPose.Sit, 0, .5f), Po(ActorPose.Talk, 0, 1.2f), Po(ActorPose.Gather, 0, 2.5f), Po(ActorPose.Cower, 0, .5f), Po(ActorPose.Hammer, 0, .45f), Po(ActorPose.Chop, 0, .55f) }, 90);
            Row("poses-3", new[] { Po(ActorPose.Knead, 0, .3f), Po(ActorPose.Work, 0, .3f), Po(ActorPose.Drink, 0, 1.2f), Po(ActorPose.Slump, 0, .5f), Po(ActorPose.None, 0, .5f, dead: true) }, 90);
            // Carrying, side on, walking: a basket of eggs, a bucket, bread, a sack of grain, logs, goods.
            Row("carry", new[] { Ca(Load.Eggs, 5, ActorVisual.Carrying.Side), Ca(Load.Bucket, 1, ActorVisual.Carrying.Side), Ca(Load.Bread, 3, ActorVisual.Carrying.Front),
                Ca(Load.Grain, 1, ActorVisual.Carrying.Shoulder), Ca(Load.Logs, 3, ActorVisual.Carrying.Shoulder), Ca(Load.Goods, 1, ActorVisual.Carrying.Front) }, 90);
            // Armour: the martial kit and the cloth kit, on the warrior and the druid, front and walking side on.
            var martial = Kit(db, 7, 3, "Cap", "Torc", "Pauldrons", "Hauberk", "Gauntlets", "Greaves", "Sabatons", "Blade", "Shield");
            var cloth = Kit(db, 5, 2, "Hood", "Pendant", "Mantle", "Tunic", "Wraps", "Breeches", "Shoes", "Cudgel", "Lantern");
            var late = Kit(db, 11, 4, "Cap", "Torc", "Spaulders", "Hauberk", "Gauntlets", "Greaves", "Sabatons", "Blade", "Shield");
            foreach (var (shot, yaw, walk) in new[] { ("armour-front", 0f, 0f), ("armour-walk", 90f, 1.6f) })
                Row(shot, new[] { P(ActorLook.Warrior, 0, null, null, martial, db, looks, walk), P(ActorLook.Warrior, 1, null, null, late, db, looks, walk), P(ActorLook.Druid, 0, null, null, cloth, db, looks, walk), P(ActorLook.Warrior, 2, null, null, new string[0], db, looks, walk) }, yaw);
            // Fighting (playtest note 25): the warrior armed, a sword's swing through, a jab, a flinch, a spell drawn and let fly.
            Spec A(string act, float t, string[] gear = null) { var x = P(ActorLook.Warrior, 0, null, act + " " + t, gear ?? martial, db, looks); x.act = act; x.time = t; return x; }
            // Human Basic Motions (Kevin Iglesias): a man and a woman walking forward, back, left and right, running and sprinting, side on.
            Spec K(string clip, float t, bool woman = false) { var x = woman ? V("baker", "Ama Rusk", 13) : P(ActorLook.Warrior, 0, null, clip + " " + t); x.act = "ki:" + (woman ? "F:" : "M:") + clip; x.time = t; return x; }
            Row("motions-ki", new[] { K("Walk01_Forward", .3f), K("Walk01_Backward", .3f), K("Walk01_Left", .3f), K("Walk01_Right", .3f), K("Run01_Forward", .2f), K("Sprint01_Forward", .2f),
                K("Idle02", 1.5f), K("Walk01_Forward", .3f, true), K("Run01_Forward", .2f, true) }, 90);
            // Model weapons (2026-10-05): uncommon, rare and epic blades, hatchets, cudgels and shields from the Asset Store packs.
            Row("weapons-models", new[] { P(ActorLook.Warrior, 0, null, "Uncommon", Hands(db, 3, 2, "Blade", "Shield"), db, looks), P(ActorLook.Warrior, 1, null, "Rare", Hands(db, 6, 3, "Blade", "Buckler"), db, looks),
                P(ActorLook.Warrior, 2, null, "Epic", Hands(db, 9, 4, "Blade", "Shield"), db, looks), P(ActorLook.Warrior, 0, null, "Rare hatchet", Hands(db, 5, 3, "Hatchet", "Shield"), db, looks),
                P(ActorLook.Warrior, 1, null, "Epic cudgel", Hands(db, 10, 4, "Cudgel", "Buckler"), db, looks), P(ActorLook.Druid, 0, null, "Epic blade", Hands(db, 12, 4, "Blade", null), db, looks) }, 0);
            Row("fight", new[] { A("swing", .15f), A("swing", .35f), A("swing", .55f), A("jab", .2f, new string[0]), A("hit", .2f), A("castloop", .5f, new string[0]), A("castenter", 9f, new string[0]), A("castshot", .25f, new string[0]) }, 90);
            // Named head pieces on the players, close (playtest note 31: "tin crown is also way too big", "orbits my head").
            var named = ItemDatabase.Parse(new System.Collections.Generic.List<string> { System.IO.File.ReadAllText("Assets/Crulanda/EncounterContent/Items/items.json") });
            Row("helms-named", new[] { P(ActorLook.Warrior, 0, null, "Tin crown", new[] { "item.tin_crown" }, named, looks), P(ActorLook.Druid, 0, null, "Tin crown (druid)", new[] { "item.tin_crown" }, named, looks),
                P(ActorLook.Warrior, 1, null, "Bare", new string[0], named, looks) }, 0, close: true);
            // Every head piece, close, on a man (the Ranger kit) and a woman (the Peasant kit), front and side (2026-10-06: "still many
            // helms that dont fit on the head").
            var helmLooks = Crulanda.Encounter.WardrobeCapture.HeadLooks; var helmDb = new ItemDatabase(); var helmIds = new List<string>();
            foreach (var hl in helmLooks) { string id = "helmfit." + helmIds.Count; looks.Register(id, hl); helmDb.Items[id] = new ItemDef { id = id, name = hl, kind = "gear", slot = "head", quality = 2, level = 6, canonStatus = "GAME-ONLY" }; helmIds.Add(id); }
            for (int i = 0; i < helmIds.Count; i += 6)
            {
                var chunk = helmIds.GetRange(i, Mathf.Min(6, helmIds.Count - i));
                foreach (var (who, look) in new[] { ("m", ActorLook.Warrior), ("f", ActorLook.Mage) })
                    foreach (var (view, yaw) in new[] { ("front", 0f), ("side", 90f) })
                        Row("helms-fit-" + (i / 6 + 1) + "-" + who + "-" + view, chunk.ConvertAll(id => P(look, 0, null, helmLooks[helmIds.IndexOf(id)].Split('/')[0], new[] { id }, helmDb, looks)).ToArray(), yaw, close: true);
            }
            // Hats, close (Chris: "the hats definitely do not fit properly on the heads").
            Row("hats-1", new[] { V("blacksmith", "Brannoc Vell", 3), V("merchant", "Wil Carder", 8), V("baker", "Ama Rusk", 13), V("farmer", "Garet Moss", 23), V("lumberjack", "Hob Linden", 68) }, 0, close: true);
            Row("hats-2", new[] { V("skinner", "Osk Farrow", 73), V("leatherworker", "Edda Pell", 63), V("miller", "Aldo Crisp", 38), V("gossip", "Grete Lowe", 5), P(ActorLook.Collector), P(ActorLook.BanditKing) }, 0, close: true);
            // Faces: close.
            Row("faces", new[] { V("blacksmith", "Brannoc Vell", 3), V("henwife", "Hedda Thorne", 40), V("farmer", "Ama Rusk", 24), P(ActorLook.Warrior), P(ActorLook.Druid) }, 0, close: true);
            Debug.Log("FIGURE_CAPTURE_DONE");
        }

        sealed class Spec { public string act; public ActorLook look; public int variant; public string role, name; public ActorPose pose; public float walk, time = .4f; public bool dead; public string[] gear; public ItemDatabase db; public GearLooks looks; public Load load; public int count; public ActorVisual.Carrying carry; }
        static Spec Ca(Load load, int count, ActorVisual.Carrying carry) { return new Spec { look = ActorLook.Villager, variant = 13, role = "baker", name = "Ama Rusk", walk = 1.5f, time = .3f, load = load, count = count, carry = carry }; }
        static Spec P(ActorLook look, int variant = 0, string role = null, string name = null, string[] gear = null, ItemDatabase db = null, GearLooks looks = null, float walk = 0)
        { return new Spec { look = look, variant = variant, role = role, name = name ?? look.ToString(), gear = gear, db = db, looks = looks, walk = walk }; }
        static Spec V(string role, string name, int variant) { return new Spec { look = ActorLook.Villager, variant = variant, role = role, name = name }; }
        static Spec Po(ActorPose pose, float walk, float time, bool dead = false) { return new Spec { look = ActorLook.Villager, variant = 23, role = "farmer", name = "Garet Moss", pose = pose, walk = walk, time = time, dead = dead }; }

        /// <summary>A row of figures, a metre and a bit apart, drawn from the front (or the side), and the shot saved.</summary>
        static void Row(string shot, Spec[] specs, float yaw = 0, bool close = false)
        {
            if (stage != null) Object.DestroyImmediate(stage.gameObject);
            stage = new GameObject("Stage").transform;
            float gap = close ? .62f : 1.1f, x0 = -(specs.Length - 1) * gap / 2;
            for (int i = 0; i < specs.Length; i++)
            {
                var s = specs[i];
                var go = new GameObject(s.name); go.transform.SetParent(stage, false);
                go.transform.SetPositionAndRotation(new Vector3(x0 + i * gap, 1, 0), Quaternion.Euler(0, 180 + yaw, 0));   // facing the camera (-Z), or side on
                new GameObject("Body").transform.SetParent(go.transform, false);
                var v = ActorVisual.Attach(go, s.look, s.variant, s.role == "child", s.role);
                if (s.gear != null) v.ApplyGearIds(s.gear, s.db, s.looks);
                if (s.carry != ActorVisual.Carrying.None) { LoadProps.Build(go.transform, s.load, s.count); v.Carry = s.carry; }
                if (s.dead) v.PreviewDead(); else v.Preview(s.pose, s.walk, s.time);
                if (s.act != null) v.PreviewAct(s.act, s.time);
            }
            float width = specs.Length * gap + .3f, halfH = Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad), aspect = 16f / 9;
            float dist = close ? (width / 2) / (halfH * aspect) : Mathf.Max((width / 2) / (halfH * aspect), 1.15f / halfH);
            var at = new Vector3(0, close ? 1.62f : .95f, 0);
            Shot(shot, at + new Vector3(0, close ? .05f : .35f, -dist), at);
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
        /// <summary>A generated kit: for each slot in order (head ... off hand), the first item whose piece word is the one given.</summary>
        /// <summary>A generated main-hand piece (and off-hand, or null) of these piece words, level and quality.</summary>
        static string[] Hands(ItemDatabase db, int level, int quality, string main, string off)
        {
            var ids = new List<string>();
            foreach (var (slot, piece) in new[] { ("mainhand", main), ("offhand", off) })
            {
                if (piece == null) continue;
                for (int seed = 0; seed < 5000; seed++)
                {
                    string id = ItemDatabase.GearId(slot, level, quality, seed);
                    if (GearLooks.TrySplitGenerated(db.Get(id), out _, out var p, out _, out _) && p == piece) { ids.Add(id); break; }
                }
            }
            return ids.ToArray();
        }
        static string[] Kit(ItemDatabase db, int level, int quality, params string[] pieces)
        {
            var ids = new List<string>();
            for (int s = 0; s < pieces.Length; s++)
                for (int seed = 0; seed < 5000; seed++)
                {
                    string id = ItemDatabase.GearId(ItemDatabase.SlotIds[s], level, quality, seed);
                    if (GearLooks.TrySplitGenerated(db.Get(id), out _, out var p, out _, out _) && p == pieces[s]) { ids.Add(id); break; }
                }
            return ids.ToArray();
        }
    }
}
