using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.Combat;
using EntityId = Crulanda.Core.EntityId;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Hunting for hides (BUILD_PLAN step 11; ADDENDUM G). The deer and rabbit groups of a zone's critters are game animals: actors
    /// with health, a collider and a navmesh agent (EncounterEnemy with Game set, driven by GameAnimal), so they can be clicked, Tab
    /// targeted (when no enemy is near), hit and killed. They live in <see cref="Game"/>, never in Enemies, so the village's fear of
    /// enemies, "zone is clear" and being in a fight never see them. A body is skinned through the loot window ("Skin the body"):
    /// its hide, no coin, and no experience for the kill. They come back after GameAnimals.RespawnSeconds. Nothing here is saved.
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>This zone's game animals, living and dead (rebuilt with the party on a load).</summary>
        public readonly List<EncounterEnemy> Game = new List<EncounterEnemy>();

        /// <summary>
        /// Spawns the zone's game: each deer or rabbit group of its life.critters, count animals on dry, walkable ground inside the
        /// group's circle, from a random stream of their own (the zone's layout and its camps draw nothing more). Ids:
        /// game.&lt;kind&gt;.&lt;zone&gt;.&lt;group&gt;.&lt;n&gt;.
        /// </summary>
        void SpawnGame()
        {
            Game.Clear();
            var groups = Zone != null && Zone.Zone.life != null ? Zone.Zone.life.critters : null; if (groups == null) return;
            var rng = new System.Random(Zone.Zone.seed * 37 + 11);
            float R() { return (float)rng.NextDouble(); }
            string zoneShort = Zone.Zone.id.Replace("zone.", "");
            int level = Mathf.Max(1, Zone.Zone.levelMin);
            for (int g = 0; g < groups.Length; g++)
            {
                var group = groups[g]; if (group == null || !GameAnimals.IsGame(group.kind)) continue;
                for (int n = 0; n < group.count; n++)
                {
                    // Start on dry, walkable ground (never on a creek or pond bed), as a critter does.
                    var spot = group.center + new Vector2(R() - .5f, R() - .5f) * group.radius * 1.6f;
                    for (int tries = 0; tries < 12 && Zone.WaterAt(spot, out _, out _); tries++) spot = group.center + new Vector2(R() - .5f, R() - .5f) * group.radius * 1.6f;
                    float look = R(), seed = R() * 100, turn = R() * 360;
                    if (!NavMesh.SamplePosition(Zone.Ground(spot), out var hit, 3, NavMesh.AllAreas) || Zone.WaterAt(new Vector2(hit.position.x, hit.position.z), out _, out _)) continue;
                    SpawnGameAnimal(group, "game." + group.kind + "." + zoneShort + "." + g + "." + n, hit.position + Vector3.up, Quaternion.Euler(0, turn, 0), level, look, seed);
                }
            }
        }
        /// <summary>One game animal: an actor with a collider about its body and a small agent, its body and behaviour (GameAnimal).</summary>
        EncounterEnemy SpawnGameAnimal(Crulanda.World.ZoneCritters group, string id, Vector3 point, Quaternion facing, int level, float look, float seed)
        {
            bool deer = group.kind == "deer"; string label = GameAnimals.Name(group.kind);
            var go = new GameObject(label); go.SetActive(false);
            go.transform.SetParent(actorsRoot.transform); go.transform.position = point; go.transform.rotation = facing;
            var a = go.AddComponent<Actor>(); a.Initialize(content.enemy, label, level, new EntityId(id));
            go.AddComponent<Combatant>();
            // A box about the body for clicks and the player's footing (the transform rides 1 m over the feet).
            var box = go.AddComponent<BoxCollider>();
            box.size = deer ? new Vector3(.6f, 1.8f, 1.7f) : new Vector3(.36f, .5f, .5f); box.center = new Vector3(0, -1 + box.size.y / 2, deer ? .1f : 0);
            var agent = go.AddComponent<NavMeshAgent>(); agent.speed = deer ? 1.2f : 1.1f; agent.angularSpeed = 540; agent.acceleration = 16;
            agent.radius = deer ? .4f : .2f; agent.height = deer ? 1.8f : .5f; agent.baseOffset = 1; agent.stoppingDistance = .3f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            var e = go.AddComponent<EncounterEnemy>(); e.actor = a; e.persistentId = id; e.session = this;
            e.Camp = true; e.Game = true; e.RespawnSeconds = GameAnimals.RespawnSeconds; e.CampCenter = group.center; e.CampRadius = group.radius;
            GameAnimal.Attach(e, group.kind, group.center, group.radius, look, seed);
            go.SetActive(true); e.Initialize(); Game.Add(e);
            a.Stats.SetBase(StatType.MaxHealth, GameAnimals.Health(group.kind));
            a.Health.ApplyHealing(a.Health.Pool.Max);
            return e;
        }
        /// <summary>A game animal fell: its hide on the body (no coin), no experience, and the kill told to the quests.</summary>
        void GameDied(EncounterEnemy animal)
        {
            var kind = animal.GetComponent<GameAnimal>()?.Kind;
            PutLoot(animal, 0, GameAnimals.RollHide(Items, kind, animal.actor.Level, new System.Random(Random.Range(0, int.MaxValue))));
            Message(animal.actor.DisplayName + " killed · no experience from game · press E at the body to skin it.");
            if (Target == animal) AutoAttack = false;
            if (Quests != null) { Quests.Notify("kill", animal.persistentId); ReconcileQuests(); }
        }
        /// <summary>Tab with no enemy near: the living game animals within 25 m, nearest first.</summary>
        List<EncounterEnemy> NearbyGame() { var near = Game.FindAll(e => e != null && e.actor.IsAlive && Distance(e) < 25); near.Sort((a, b) => Distance(a).CompareTo(Distance(b))); return near; }
        /// <summary>A game animal's body in reach that E would skin (as for a camp body: something on it that fits).</summary>
        EncounterEnemy SkinnableBody { get { return Game.Find(e => e != null && CanLoot(e) && Distance(e) < 3.6f && CanTakeAny(e)); } }
        /// <summary>Whether the target you are swinging at is an enemy (a game animal you are hunting is not a fight).</summary>
        bool FightingTarget { get { return AutoAttack && Target != null && Target.actor.IsAlive && !Target.Game; } }
    }
}
