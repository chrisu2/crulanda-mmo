using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A swing-load at work (the Adit rework, stage S7; Chris, 2026-10-15, Q4 "Yes, add them"; GAME-ONLY): an ore bucket on its rope swinging
    /// across one of the Geode Foundry's high walks, out over the hall and back, while its rope team on the floor below (two pressed goblins
    /// and a Sandthrone hand) lean back on the tag line as it comes their way and give it line as it goes. It swings like a pendulum (out to
    /// <see cref="amplitude"/> degrees each way, a full swing every <see cref="period"/> seconds), so its bucket is down at the walk only as
    /// it passes through the bottom: there is room to cross between swings. Whoever of your party it meets there is knocked back along the
    /// walk (<see cref="KnockAlong"/>; no damage), never across it: the throw stays on the walk's own level or does not happen, so the bucket
    /// never puts anyone off a ledge. Mobs are not struck. No wheel, winch or crane: a rope, a lashed block and people pulling (memory "NO Machines").
    /// </summary>
    public sealed class AditSwingLoad : MonoBehaviour
    {
        public SwingLoadSpot spot; public EncounterSession session;
        /// <summary>Seconds for a full swing (out and back), how far it swings each way (degrees), and where in its swing it starts (0..1).</summary>
        public float period = 5.6f, amplitude = 50, phase;
        /// <summary>How far a struck one is thrown along the walk, and how long before the same one can be struck again.</summary>
        public static float KnockMetres = 2.5f, Recover = 1.5f;
        /// <summary>How fast the swing's clock runs (1: real time; tests raise it).</summary>
        public static float Speed = 1;
        /// <summary>The bucket meets someone whose middle is within this of its middle on the level, and this up or down.</summary>
        public const float HitReach = .95f, HitHeight = 1.3f;
        /// <summary>The swing's angle now (degrees; + toward the team), and how many it has knocked back this visit.</summary>
        public float Angle { get; private set; }
        public int Hits { get; private set; }
        readonly Dictionary<Actor, float> struck = new Dictionary<Actor, float>();
        readonly List<(Transform t, Quaternion rest)> team = new List<(Transform, Quaternion)>();
        float clock; static bool toldOnce;
        /// <summary>Seconds into its swing (tests set it, with <see cref="Speed"/> 0, to hold the bucket where they want it).</summary>
        public float Clock { get { return clock; } set { clock = value; } }
        /// <summary>How many of its rope team stand on the line.</summary>
        public int Haulers { get { return team.Count; } }

        /// <summary>One of the rope team, standing on the line (it leans with the swing).</summary>
        public void AddHauler(Transform hauler) { if (hauler != null) team.Add((hauler, hauler.rotation)); }
        /// <summary>Where the bucket's middle is now.</summary>
        public Vector3 BucketAt { get { return spot != null && spot.bucket != null ? spot.bucket.position : transform.position; } }

        void Start() { Pose(0); }
        void Update()
        {
            if (spot == null || spot.bucket == null || (session != null && session.Paused)) return;
            clock += Time.deltaTime * Speed;
            Pose(clock);
            if (session != null) Strike();
        }
        /// <summary>Everything where it stands <paramref name="t"/> seconds into the swing: the bucket on its arc (hanging from the block), the
        /// rope, the tag line to the team, the team leaning back as it comes to them.</summary>
        public void Pose(float t)
        {
            float u = 2 * Mathf.PI * (t / Mathf.Max(.5f, period) + phase);
            Angle = amplitude * Mathf.Sin(u); float th = Angle * Mathf.Deg2Rad;
            var at = spot.pivot + (spot.swing * Mathf.Sin(th) - Vector3.up * Mathf.Cos(th)) * spot.rope;
            var up = (spot.pivot - at).normalized;
            spot.bucket.SetPositionAndRotation(at, Quaternion.FromToRotation(Vector3.up, up) * Quaternion.LookRotation(spot.walk));
            Line(spot.ropeLine, spot.pivot, at + up * .8f, .06f);
            var hand = spot.team + Vector3.up * 1.1f + spot.teamFacing * .5f;
            Line(spot.tagLine, hand, at - up * .35f, .035f);
            float lean = 14 * Mathf.Cos(u);   // leaning back as it swings their way (its speed toward them), giving it line as it goes
            foreach (var (h, rest) in team) if (h != null) h.rotation = rest * Quaternion.Euler(-Mathf.Max(0, lean), 0, 0);
        }
        static void Line(Transform line, Vector3 a, Vector3 b, float thick)
        {
            if (line == null) return; var d = b - a; float len = d.magnitude; if (len < .01f) return;
            line.SetPositionAndRotation((a + b) / 2, Quaternion.FromToRotation(Vector3.up, d / len)); line.localScale = new Vector3(thick, len / 2, thick);
        }
        /// <summary>The party where the bucket is: each one it meets is knocked back along the walk (once in <see cref="Recover"/> seconds).</summary>
        void Strike()
        {
            var at = BucketAt;
            foreach (var a in session.PartyActors())
            {
                if (a == null || !a.IsAlive) continue;
                if (struck.TryGetValue(a, out float until) && Time.time < until) continue;
                var p = a.transform.position; var d = at - p;
                if (new Vector2(d.x, d.z).magnitude > HitReach || Mathf.Abs(d.y) > HitHeight) continue;
                struck[a] = Time.time + Recover;
                var away = Vector3.Dot(p - spot.bottom, spot.walk) >= 0 ? spot.walk : -spot.walk;
                if (!KnockAlong(a, away, KnockMetres)) continue;
                Hits++;
                session.FloatText(a.transform.position, "Knocked back", new Color(1, .8f, .5f));
                if (a == session.Player && !toldOnce) { toldOnce = true; session.Message("The ore bucket swings through and knocks you back along the walk. Watch its rope, and cross as it swings away."); }
            }
        }
        /// <summary>
        /// Throws <paramref name="who"/> <paramref name="metres"/> along <paramref name="dir"/> on the walkable mesh (traced along it from under
        /// their feet, stopping at an edge or a rail), and only onto the level they stand on (within 0.6 m): never off a walk, a ledge or a deck.
        /// False when there is no such ground (they stay where they are).
        /// </summary>
        public static bool KnockAlong(Actor who, Vector3 dir, float metres)
        {
            dir.y = 0; if (who == null || metres <= 0 || dir.sqrMagnitude < 1e-4f) return false; dir.Normalize();
            if (!NavMesh.SamplePosition(who.transform.position, out var from, 2.5f, NavMesh.AllAreas)) return false;
            float lift = who.transform.position.y - from.position.y; if (lift < -.5f || lift > 1.6f) return false;   // the floor under their feet, not the hall's below the catwalk
            NavMeshHit hit;
            if (NavMesh.Raycast(from.position, from.position + dir * metres, out var edge, NavMesh.AllAreas)) { if ((edge.position - from.position).magnitude < .4f) return false; hit = edge; }
            else if (!NavMesh.SamplePosition(from.position + dir * metres, out hit, .6f, NavMesh.AllAreas)) return false;
            if (Mathf.Abs(hit.position.y - from.position.y) > .6f) return false;
            var motor = who.GetComponent<AdventurerMotor>();
            if (motor != null) motor.Teleport(hit.position + Vector3.up * (who.transform.position.y - from.position.y));
            else { var agent = who.GetComponent<NavMeshAgent>(); if (agent == null || !agent.isOnNavMesh) return false; agent.Warp(hit.position); }
            return true;
        }
        /// <summary>A new visit: the one-time line may be said again (StartAdit).</summary>
        public static void NewVisit() { toldOnce = false; }
    }
}
