using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Crulanda.Core;
using Crulanda.Data;
using Crulanda.Encounter;
using Crulanda.Abilities;

namespace Crulanda.EditorTools
{
    public static class EncounterBuilder
    {
        const string Root = "Assets/Crulanda/EncounterContent";
        public const string ScenePath = "Assets/Crulanda/Scenes/PlayableEncounter.unity";
        [MenuItem("Crulanda/Build Playable Encounter")]
        public static void Build()
        {
            if (File.Exists(ScenePath)) { Debug.Log("Encounter already exists; preserving it."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var content = ScriptableObject.CreateInstance<EncounterContent>();
            content.player = Archetype("player", "Adventurer", Disposition.Friendly, ResourceKind.Vigor, 180, 100);
            content.healer = Archetype("healer", "Mira", Disposition.Friendly, ResourceKind.Mana, 130, 120);
            content.enemy = Archetype("enemy", "Sentry", Disposition.Hostile, ResourceKind.None, 240, 0);
            content.abilities = new[] {
                new AbilitySpec { id="ability.strike",effect=AbilityEffect.Damage,name="Strike",description="A heavy strike. Starts automatic weapon attacks while in melee range.",power=12,cost=15,cooldown=5,range=3.2f },
                new AbilitySpec { id="ability.challenge",effect=AbilityEffect.Taunt,duration=3,name="Challenge",description="Forces your target to attack you for 3 seconds. Use it to protect Mira.",cost=10,cooldown=8,range=10 },
                new AbilitySpec { id="ability.guard",effect=AbilityEffect.ApplyStatus,statusId="status.guard",duration=5,globalCooldown=0,name="Guard",description="Reduce incoming damage for 5 seconds.",cost=20,cooldown=12,range=0 },
                new AbilitySpec { id="ability.intercept",effect=AbilityEffect.Intercept,duration=3,name="Intercept",description="Dash to Mira and take the next blow aimed at her within 3 seconds at Guard strength. Talent.",cost=20,cooldown=15,range=0 },
                new AbilitySpec { id="ability.breaching_blow",effect=AbilityEffect.Breach,duration=4,name="Breaching Blow",description="Spend all weapon pressure for 12 damage per pressure and Expose the target for 4 seconds. Talent.",power=12,cost=0,cooldown=10,range=3.2f },
                new AbilitySpec { id="ability.muster",effect=AbilityEffect.Rally,duration=6,name="Muster",description="Give you and nearby Mira a 20-point barrier for 6 seconds. Talent.",power=20,cost=25,cooldown=30,range=12 }
            };
            content.playerClass.unlocks = new[] {
                new ClassAbilityUnlock { abilityId="ability.strike" }, new ClassAbilityUnlock { abilityId="ability.challenge" },
                new ClassAbilityUnlock { abilityId="ability.guard" },
                new ClassAbilityUnlock { abilityId="ability.intercept", talentId="tk-intercept" },
                new ClassAbilityUnlock { abilityId="ability.breaching_blow", talentId="dp-breaching-blow" },
                new ClassAbilityUnlock { abilityId="ability.muster", talentId="sp-muster" }
            };
            content.talentTree = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Talents/warrior.json");
            AssetDatabase.CreateAsset(content, Root + "/Encounter.asset");
            var db = ScriptableObject.CreateInstance<ContentDatabase>();
            db.EditorSetDefinitions(new DefinitionBase[] { content.player, content.healer, content.enemy });
            AssetDatabase.CreateAsset(db, Root + "/Database.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var world = new GameObject("Playable Encounter");
            world.AddComponent<EncounterNavigation>();
            var session = world.AddComponent<EncounterSession>(); session.content = content;
            world.AddComponent<EncounterHud>().session = session;
            var camera = new GameObject("Main Camera"); camera.tag = "MainCamera";
            var view = camera.AddComponent<Camera>(); view.fieldOfView = 55; view.farClipPlane = 140;
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.39f,.52f,.58f);
            camera.AddComponent<AudioListener>(); camera.transform.position = new Vector3(0,8,-23); camera.transform.rotation = Quaternion.Euler(32,0,0);
            var sun = new GameObject("Late afternoon sun").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.intensity = 1.05f; sun.color = new Color(1,.89f,.7f); sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(45,-35,0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.43f,.5f,.57f);
            RenderSettings.fog = true; RenderSettings.fogColor = view.backgroundColor; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 38; RenderSettings.fogEndDistance = 85;
            var grass = Material("Grass",new Color(.25f,.36f,.27f));
            var path = Material("Trail",new Color(.53f,.46f,.33f));
            var wood = Material("Timber",new Color(.23f,.16f,.12f));
            var leaves = Material("Foliage",new Color(.16f,.29f,.23f));
            var stone = Material("Stone",new Color(.4f,.45f,.45f));
            var canvas = Material("Canvas",new Color(.69f,.54f,.31f));
            Shape("Ground",PrimitiveType.Cube,new Vector3(0,-.25f,0),new Vector3(64,.5f,64),grass,true);
            Shape("Old trail",PrimitiveType.Cube,new Vector3(0,.005f,3),new Vector3(5,.02f,47),path,false);
            Shape("Camp clearing",PrimitiveType.Cylinder,new Vector3(0,.025f,-13),new Vector3(16,.025f,12),path,false);
            for(int i=0;i<24;i++)
            {
                float angle=i*Mathf.PI*2/24; var p=new Vector3(Mathf.Cos(angle)*25,0,Mathf.Sin(angle)*25);
                Shape("Tree trunk",PrimitiveType.Cylinder,p+Vector3.up*2,new Vector3(.65f,2,.65f),wood,false);
                Shape("Tree crown",PrimitiveType.Sphere,p+Vector3.up*5,new Vector3(4,6,4),leaves,false);
            }
            for(int i=0;i<10;i++)
                Shape("Ridge stone",PrimitiveType.Sphere,new Vector3(i%2==0?-18:18,1.2f,i*4-16),new Vector3(3,2.5f,4),stone,false);
            Shape("Supply tent",PrimitiveType.Cube,new Vector3(-8,1.1f,-13),new Vector3(4,2.2f,3),canvas,false);
            Shape("Tent roof",PrimitiveType.Cube,new Vector3(-8,2.4f,-13),new Vector3(4.6f,.3f,3.6f),wood,false);
            Shape("Camp crate",PrimitiveType.Cube,new Vector3(-5,.5f,-15),Vector3.one,wood,false);
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Boundary",PrimitiveType.Cube,new Vector3(side*31,1,0),new Vector3(1,4,64),stone,true);
                Shape("Boundary",PrimitiveType.Cube,new Vector3(0,1,side*31),new Vector3(64,4,1),stone,true);
                Shape("Ruin pillar",PrimitiveType.Cylinder,new Vector3(side*4,2,22),new Vector3(1.2f,2,1.2f),stone,false);
            }
            Shape("Ruin lintel",PrimitiveType.Cube,new Vector3(0,4.2f,22),new Vector3(10,1,1.5f),stone,false);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
            AssetDatabase.SaveAssets();
            Debug.Log("PLAYABLE_ENCOUNTER_BUILT");
        }
        static ActorArchetypeDefinition Archetype(string id,string name,Disposition disposition,ResourceKind resource,int hp,int power)
        {
            var a=ScriptableObject.CreateInstance<ActorArchetypeDefinition>();
            a.EditorSetIdentity(new ContentId("encounter."+id),name,CanonStatus.GameOnly);
            a.EditorConfigure(1,disposition,ActorClassification.Normal,resource,new[] {new StatValue(StatType.MaxHealth,hp),new StatValue(StatType.MaxPower,power)});
            AssetDatabase.CreateAsset(a,Root+"/"+id+".asset"); return a;
        }
        static Material Material(string name,Color color)
        {
            var m=new Material(Shader.Find("Standard"));m.color=color;m.SetFloat("_Glossiness",.12f);
            AssetDatabase.CreateAsset(m,Root+"/"+name+".mat");return m;
        }
        static void Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material,bool collision)
        {
            var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.position=position;o.transform.localScale=scale;
            o.GetComponent<Renderer>().sharedMaterial=material;if(!collision) Object.DestroyImmediate(o.GetComponent<Collider>());
        }
    }
}
