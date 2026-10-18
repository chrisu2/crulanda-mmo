using UnityEngine;
using UnityEngine.AI;

namespace Crulanda.Encounter
{
    /// <summary>
    /// When a chaser asks for its path again (2026-10-18, hallows-8: the level-17 group run stood 27 minutes at the foot of the Bell
    /// Tower's stair, the leader "closing on" the Pact-Captain on the landing 9 m up and she never coming down). A path is worked out over
    /// frames, within the frame's share of the navmesh's search (NavMesh.pathfindingIterationsPerFrame, shared by every agent); asked for
    /// afresh every frame, as the mobs' chase and the sims' closing did, a long way (up four flights and their corners) was started over
    /// before it was ever done, and the agent stood. Now it is asked again only when the one chased has moved a metre from where the agent
    /// is going (or three metres up or down), and while one is still being worked out, not more than once a second.
    /// </summary>
    public static class MobChase
    {
        /// <summary>Moved this far across the ground (or <see cref="Climb"/> up or down) from the agent's destination: asked again.</summary>
        public const float Moved = 1, Climb = 3;
        /// <summary>A path still being worked out is asked for again no sooner than this.</summary>
        public const float PendingWait = 1;

        public static bool Ask(NavMeshAgent agent, Vector3 target, float askedAt, float now)
        {
            if (agent == null) return false;
            return Ask(agent.hasPath, agent.pathPending, agent.destination, target, askedAt, now);
        }
        /// <summary>The rule itself, for tests: true when the path should be asked for now.</summary>
        public static bool Ask(bool hasPath, bool pending, Vector3 destination, Vector3 target, float askedAt, float now)
        {
            if (!hasPath && !pending) return true;
            var d = destination - target;
            bool moved = new Vector2(d.x, d.z).magnitude > Moved || Mathf.Abs(d.y) > Climb;
            if (!moved) return false;
            return !pending || now - askedAt > PendingWait;
        }
    }
}
