using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A dungeon's checkpoints in play (GAME-ONLY; Chris, 2026-10-16: "one thing to help respawns is to have checkpoints in the dungeon so
    /// they don't have to run all the back from the original spawn area. maybe 3 checkpoints in dungeons"; Docs/DUNGEON_DESIGN.md section 0):
    /// - each of the zone's checkpoints (ZoneDefinition.checkpoints, built by ZoneBuilder.Checkpoints) is reached when you, a party sim or Mira
    ///   comes within <see cref="CheckpointReach"/> m of it, alive: a line in the chat ("Checkpoint reached: ...");
    /// - the latest reached is the furthest along the run (its place in the zone's list), not the last touched: walking back past an earlier
    ///   one does not move where you wake;
    /// - dead, Recover wakes you there (<see cref="RecoverAt"/>) instead of at the zone's start or its spirit stone, and the fallen of the
    ///   party (sims, Mira) wake beside you, as at the stone;
    /// - kept for the visit (this session in this zone), not saved: a new visit starts with none reached. Outside a dungeon with checkpoints
    ///   nothing changes.
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>How near (flat metres; and within three up or down) a party member comes to a checkpoint to reach it. Twelve, not four
        /// (2026-10-17, hallows-5: the lamp at the lip stands 7 m off the Ash Road's middle, where the leader walks, so the party passed it
        /// and nobody reached it until the bot ran back from the yard after the Fallen Mason; the Adit's cairns stand 10 to 13 m off the way
        /// between their camps): a checkpoint is reached by walking past it on the way, as a player expects.</summary>
        public const float CheckpointReach = 12;
        readonly HashSet<int> checkpointsReached = new HashSet<int>();
        readonly Dictionary<ZoneCheckpointSpot, Vector3> checkpointWake = new Dictionary<ZoneCheckpointSpot, Vector3>();
        float checkpointCheckAt;
        /// <summary>The checkpoint furthest along the run reached this visit, or null.</summary>
        public ZoneCheckpointSpot LatestCheckpoint { get; private set; }
        /// <summary>The checkpoints' names in the order they were reached this visit (the group run logs them).</summary>
        public readonly List<string> CheckpointsReached = new List<string>();
        /// <summary>Where the last Recover woke you, in words ("the Overlook cairn", "the spirit stone", "camp").</summary>
        public string WokeAt { get; private set; }

        /// <summary>On arrival (after the navmesh is built): none reached; where each one's sleepers wake, a step in front of it on the floor.</summary>
        void StartCheckpoints()
        {
            checkpointsReached.Clear(); checkpointWake.Clear(); CheckpointsReached.Clear(); LatestCheckpoint = null; checkpointCheckAt = 0;
            if (Zone == null) return;
            foreach (var c in Zone.Checkpoints)
            {
                if (c == null) continue;
                var front = c.root != null ? c.position + c.root.forward * 1.5f : c.position;
                if (NavMesh.SamplePosition(front + Vector3.up * .3f, out var hit, 2.5f, NavMesh.AllAreas) || NavMesh.SamplePosition(c.position + Vector3.up * .3f, out hit, 3.5f, NavMesh.AllAreas))
                    checkpointWake[c] = hit.position + Vector3.up * 1.1f;
                else checkpointWake[c] = c.position + Vector3.up * 1.1f;
            }
        }
        /// <summary>Where you wake at a checkpoint (on the navmesh in front of its marker, at a body's height as RecoveryPoint is).</summary>
        public Vector3 CheckpointWake(ZoneCheckpointSpot c)
        {
            if (c == null) return RecoveryPoint;
            return checkpointWake.TryGetValue(c, out var p) ? p : c.position + Vector3.up * 1.1f;
        }
        /// <summary>Where Recover wakes you: the latest checkpoint reached this visit, else the zone's recovery point (its spirit stone, its camp).</summary>
        public Vector3 RecoverAt { get { return LatestCheckpoint != null ? CheckpointWake(LatestCheckpoint) : RecoveryPoint; } }
        /// <summary>Where one waking with you stands: <paramref name="offset"/> from where you wake, kept on the navmesh at a checkpoint (a
        /// cave's drift is narrow: two metres to the west can be rock). Elsewhere just the offset, as it always was.</summary>
        public Vector3 WakeBeside(Vector3 offset)
        {
            var at = RecoverAt; var p = at + offset;
            if (LatestCheckpoint == null) return p;
            if (NavMesh.SamplePosition(p, out var hit, 3, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - (at.y - 1.1f)) < 3) return hit.position;
            return at - Vector3.up * 1.1f;
        }

        /// <summary>A few times a second: a checkpoint any living party member is near is reached.</summary>
        void TickCheckpoints()
        {
            if (Zone == null || Zone.Checkpoints.Count == 0 || Time.time < checkpointCheckAt) return;
            checkpointCheckAt = Time.time + .25f;
            foreach (var c in Zone.Checkpoints)
            {
                if (c == null || checkpointsReached.Contains(c.index)) continue;
                bool near = Player != null && Player.IsAlive && Near(Player.transform.position, c.position);
                if (!near) foreach (var s in PartySims) if (s != null && s.actor.IsAlive && Near(s.transform.position, c.position)) { near = true; break; }
                if (!near && Progress != null && Progress.recruited && Companion != null && Companion.actor.IsAlive) near = Near(Companion.transform.position, c.position);
                if (near) ReachCheckpoint(c);
            }
        }
        static bool Near(Vector3 who, Vector3 at)
        {
            return Mathf.Abs(who.y - at.y) < 3 + 1.1f && new Vector2(who.x - at.x, who.z - at.z).sqrMagnitude <= CheckpointReach * CheckpointReach;
        }
        /// <summary>A checkpoint reached (the tick, and tests): remembered for the visit; the latest if it is the furthest along.</summary>
        public void ReachCheckpoint(ZoneCheckpointSpot c)
        {
            if (c == null || !checkpointsReached.Add(c.index)) return;
            CheckpointsReached.Add(c.def.name);
            if (LatestCheckpoint == null || c.index > LatestCheckpoint.index) { LatestCheckpoint = c; Message("Checkpoint reached: " + c.def.name + ". If you fall, you will wake here."); }
            else Message("Checkpoint reached: " + c.def.name + " (you will still wake at " + LatestCheckpoint.def.name + ", further on).");
        }
    }
}
