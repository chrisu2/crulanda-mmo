using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A walk through an inn's doorway (Phase 8.6 step 2, 2026-10-15; Chris, note 205: "running into each other and sometimes get
    /// stuck or delayed at door of inn"): a villager or a sim whose way runs through an inn's walls (one end inside the room, the
    /// other out) walks to the doorway lane's end, asks for the lane (ZoneDoorway: whoever is on it goes first, the rest wait in
    /// line a metre and more off it, the most pressed first), walks it, steps off it and goes on. A shut door is opened by whoever
    /// comes to it. Waited 25 s and still not given the lane, or held it 8 s and not off it, they go on through as everyone went before.
    /// </summary>
    public sealed class DoorwayRoute
    {
        public readonly ZoneInn Inn; public readonly bool In;
        /// <summary>Where they were going: they are sent on there once through.</summary>
        public readonly Vector3 Final;
        enum Leg { Approach, Wait, Cross, Done }
        Leg leg; float legSince = -1; Vector3 aim = Vector3.positiveInfinity;
        /// <summary>In line for the lane at a place off it (standing still on purpose: no watchdog), and on it.</summary>
        public bool Waiting { get { return leg == Leg.Wait && queued; } }
        public bool Crossing { get { return leg == Leg.Cross; } }
        /// <summary>How near the lane's end they ask for it, and how long they wait for it before going on through.</summary>
        public const float AskWithin = 3.5f, WaitGiveUp = 25;
        DoorwayRoute(ZoneInn inn, bool goingIn, Vector3 final) { Inn = inn; In = goingIn; Final = final; }

        /// <summary>The route from one point to another through the doorway of whichever inn's walls lie between them, or null.</summary>
        public static DoorwayRoute Plan(ZoneBuilder zone, Vector3 from, Vector3 to)
        {
            if (zone == null) return null;
            foreach (var inn in zone.Inns)
            {
                if (inn == null || inn.door == null || inn.door.Way == null) continue;
                bool a = inn.Contains(from, .05f), b = inn.Contains(to, .05f);
                if (a != b) return new DoorwayRoute(inn, b, to);
            }
            return null;
        }
        /// <summary>The first place to walk to: the lane's end on their side.</summary>
        public Vector3 Start { get { return Inn.door.Way.Entry(In); } }

        /// <summary>Steers the agent; true while the route still has it, false once it is through (sent on to <see cref="Final"/>).</summary>
        public bool Tick(NavMeshAgent agent, object who, int rank, float now)
        {
            if (leg == Leg.Done || agent == null || !agent.isOnNavMesh) return false;
            var way = Inn.door.Way; var pos = agent.transform.position;
            if (legSince < 0) legSince = now;
            if (leg == Leg.Cross)
            {
                way.Request(who, rank, In, now, out _);   // still on it: heard from
                var exit = way.Exit(In); float past = Vector3.Dot(pos - way.middle, way.inward) * (In ? 1 : -1);
                if (Flat(pos, exit) < .5f || past > ZoneDoorway.Reach - .15f || now - legSince > ZoneDoorway.HoldLimit) return Finish(agent, way, who);
                Aim(agent, exit); return true;
            }
            var entry = way.Entry(In); float d = Flat(pos, entry);
            if (leg == Leg.Approach && d > AskWithin) { Aim(agent, entry); return true; }
            // In line from AskWithin; on the lane only from its end, or from the first place in line (ReadyWithin), so it is held just
            // for the step to its end and the walk across.
            bool ready = d < ReadyWithin || queued && lastPlace == 0 && Flat(pos, way.QueueSpot(In, 0)) < ReadyWithin;
            if (way.Request(who, rank, In, now, out int place, ready))
            {
                if (!Inn.door.Open) Inn.door.SetOpen(true);   // a shut door: whoever comes to it opens it
                leg = Leg.Cross; legSince = now; queued = false; Aim(agent, way.Exit(In)); return true;
            }
            if (leg == Leg.Approach) { leg = Leg.Wait; legSince = now; }
            if (now - legSince > WaitGiveUp) return Finish(agent, way, who);
            queued = place > 0 || way.Holder(In) != null; lastPlace = place;   // the next with the lane free walks on to its end; the rest wait off it
            Aim(agent, queued ? way.QueueSpot(In, place) : entry); return true;
        }
        /// <summary>How near the lane's end (or the first place in line, a metre back and a little aside) one must be to step on to it:
        /// the lane is taken the moment it is free, and held only for the step to its end and the walk across.</summary>
        public const float ReadyWithin = 1.3f;
        bool queued; int lastPlace = -1;
        bool Finish(NavMeshAgent agent, ZoneDoorway way, object who)
        {
            way.Release(who); leg = Leg.Done;
            if (agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(Final); }
            return false;
        }
        /// <summary>Off the route (fled, called away, gone in): the lane and the line let go.</summary>
        public void Drop(object who) { if (leg != Leg.Done && Inn.door.Way != null) Inn.door.Way.Release(who); leg = Leg.Done; }
        void Aim(NavMeshAgent agent, Vector3 to)
        {
            bool moved = (aim - to).sqrMagnitude > .04f;
            if (!moved && (agent.hasPath || agent.pathPending || Flat(agent.transform.position, to) < .5f)) return;
            aim = to; agent.isStopped = false; agent.SetDestination(to);
        }
        static float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }
    }

    /// <summary>
    /// The stuck watchdog (Phase 8.6 step 2: sims had one, villagers had none): headway is a quarter metre moved, or a quarter metre less
    /// to go. With none for 3 s while walking, a step of 0.6 m to the side with room (over a third of a second, not a hop) and the
    /// path asked for again; with none for 10 s the walker gives the errand up, as a path that cannot reach (Stranded) does. One
    /// helper for both: the villagers' walk and the sims' "jammed" at the inn's door (SimFigure.AtInn). The same step carries a
    /// villager or a sim aside for you and your party (<see cref="GiveWay"/>).
    /// </summary>
    public sealed class Headway
    {
        public const float StepAfter = 3, GiveUpAfter = 10, Progress = .25f, Aside = .6f;
        Vector3 mark, aside, resume; float markAt = -1, left = float.MaxValue; bool stepped, stepping, rePath;
        public void Reset(Vector3 at, float now) { mark = at; markAt = now; left = float.MaxValue; stepped = false; }
        public void Tick(Vector3 at, float remaining, float now)
        {
            if (markAt < 0) Reset(at, now);
            if (left == float.MaxValue) left = remaining;   // the first reading of the way left is not headway
            var d = at - mark; d.y = 0;
            if (d.sqrMagnitude > Progress * Progress || remaining < left - Progress) { mark = at; markAt = now; left = remaining; stepped = false; }
        }
        /// <summary>How long it has gone without headway.</summary>
        public float StillFor(float now) { return markAt < 0 ? 0 : now - markAt; }
        /// <summary>Three seconds without headway: a step aside is due (once a stall).</summary>
        public bool StepDue(float now) { if (stepped || StillFor(now) < StepAfter) return false; stepped = true; return true; }
        /// <summary>Ten seconds without headway: give it up.</summary>
        public bool Lost(float now) { return StillFor(now) >= GiveUpAfter; }
        public bool Stepping { get { return stepping; } }
        /// <summary>Starts a step to the side of the way it was going (the side with room on the navmesh), or along <paramref name="way"/>;
        /// the path is asked for again once the step is done.</summary>
        public void BeginStep(NavMeshAgent agent, Vector3? way = null, float length = Aside)
        {
            if (agent == null || !agent.isOnNavMesh) return;
            var at = agent.transform.position; Vector3 s;
            if (way.HasValue) s = way.Value.normalized * length;
            else
            {
                var fwd = agent.steeringTarget - at; fwd.y = 0; if (fwd.sqrMagnitude < .01f) fwd = agent.transform.forward; fwd.y = 0;
                s = Vector3.Cross(Vector3.up, fwd.normalized) * length;
                if (NavMesh.Raycast(at, at + s, out _, NavMesh.AllAreas)) s = -s;
            }
            if (NavMesh.Raycast(at, at + s, out var hit, NavMesh.AllAreas)) s = (hit.position - at) * .9f;
            s.y = 0; if (s.sqrMagnitude < .01f) return;
            aside = s; stepping = true; rePath = agent.hasPath; resume = agent.destination;
        }
        /// <summary>Carries a step on (call it every frame).</summary>
        public void Steer(NavMeshAgent agent, float dt)
        {
            if (!stepping || agent == null || !agent.isOnNavMesh) { stepping = false; return; }
            var m = Vector3.ClampMagnitude(aside, 1.8f * dt); agent.Move(m); aside -= m;
            if (aside.sqrMagnitude > .0004f) return;
            stepping = false; if (rePath) agent.SetDestination(resume);
        }
    }

    /// <summary>
    /// Stepping aside for you and your party, and keeping out of the rain (Phase 8.6 decision 13, Chris 2026-10-15: "Yes, your defaults":
    /// "villagers step aside and shelter from rain"; GAME-ONLY): a villager or a sim with you or one of yours (Mira, a party sim)
    /// walking straight at them within 3 m steps a metre off your line; and when it rains the village's idle ones go under a roof
    /// (the inn, or home) and the sims rest at the inn.
    /// </summary>
    public static class GiveWay
    {
        static int frame = -1; static float lastTime;
        static readonly Dictionary<Transform, Vector3> last = new Dictionary<Transform, Vector3>();
        static readonly List<(Vector3 at, Vector3 v)> movers = new List<(Vector3, Vector3)>();
        /// <summary>How near one of yours must come, how fast at them, and how close their line must pass, before someone steps aside.</summary>
        public const float Near = 3, Closing = .5f, Line = 1, Step = 1;
        static void Refresh(EncounterSession s)
        {
            if (frame == Time.frameCount) return;
            float dt = Time.time - lastTime; frame = Time.frameCount; lastTime = Time.time; movers.Clear();
            if (s == null) return;
            void Add(Transform t)
            {
                if (t == null) return; var at = t.position;
                if (last.TryGetValue(t, out var was) && dt > 1e-4f && dt < .5f) movers.Add((at, (at - was) / dt));
                last[t] = at;
            }
            if (s.Player != null) Add(s.Player.transform);
            if (s.Companion != null) Add(s.Companion.transform);
            foreach (var c in s.PartySims) if (c != null) Add(c.transform);
            if (last.Count > 16) { var gone = new List<Transform>(); foreach (var k in last.Keys) if (k == null) gone.Add(k); foreach (var k in gone) last.Remove(k); }
        }
        /// <summary>Whether one of yours is walking at this point (within <see cref="Near"/>, closing faster than <see cref="Closing"/>, their
        /// line passing within <see cref="Line"/>), and which way is off their line.</summary>
        public static bool Due(EncounterSession s, Vector3 me, out Vector3 aside)
        {
            aside = Vector3.zero; Refresh(s);
            foreach (var (at, v) in movers)
            {
                var rel = me - at; rel.y = 0; var vel = v; vel.y = 0; float dist = rel.magnitude, speed = vel.magnitude;
                if (dist > Near || dist < .2f || speed < Closing) continue;
                var dir = vel / speed; if (Vector3.Dot(dir, rel / dist) * speed < Closing) continue;
                var off = rel - dir * Vector3.Dot(rel, dir); if (off.magnitude > Line) continue;
                aside = off.sqrMagnitude > .0025f ? off.normalized : Vector3.Cross(Vector3.up, dir);
                return true;
            }
            return false;
        }
        /// <summary>Rain enough to send folk under a roof (WorldWeather: rain, or a storm).</summary>
        public static bool Raining { get { return WorldWeather.Now.rain > .35f; } }
        /// <summary>A shift's places as the weather has them: in the rain the open places (the green, the well, wandering) give way to the
        /// inn (a child, and anyone where there is none, home); in the dry, as they are.</summary>
        public static string[] Shelter(string[] places, string role, bool inn)
        {
            if (places == null || !Raining) return places;
            var dry = new string[places.Length];
            for (int i = 0; i < places.Length; i++)
                dry[i] = places[i] == "green" || places[i] == "well" || places[i] == "wander" ? (role == "child" || !inn ? "home" : "inn") : places[i];
            return dry;
        }
    }
}
