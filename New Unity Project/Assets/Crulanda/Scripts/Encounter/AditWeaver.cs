using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Weaver's unlocking (Docs/DUNGEON_DESIGN.md section 4, the Rail Hall; dungeon step D4): Mother Quillet (GAME-ONLY), the eldest
    /// of the goblins pressed into the Adit, waits by the platform gate she hummed open. Spoken to, she walks down the hall to the
    /// carriage's resonance lock and matches it, a little over half a minute's work. On the way the Sandthrone come for her: a wave from
    /// the carriage a third of the way down, the dockers from the far tunnel at two thirds; with the lock half matched Rail-Captain Danner
    /// stands up out of the dark. She counts as one of your party for every mob, for Mira and for the sims (EncounterSession.PartyActor),
    /// so they fight over her and defend her. She waits while you are far behind or something is on her, and stops matching while you
    /// are down. Matched, the geodes go dark and "adit.lock" is kept; if she falls she is back by the gate a little later. The session
    /// spawns the waves and raises Danner (EncounterSession.Weaver.cs); this walks her and keeps the time.
    /// </summary>
    public sealed class AditWeaver : MonoBehaviour
    {
        public enum Stage { Waiting, Walking, Matching, Done, Fallen }
        public const string Name = "Mother Quillet";
        /// <summary>How long the lock takes to match, how far behind you may fall before she waits for you, and how long after a fall
        /// she is back by the gate. Tests shorten them.</summary>
        public static float MatchSeconds = 36, WaitFor = 16, BackAfter = 30;
        public Actor actor;
        /// <summary>Her "Talk to Mother Quillet" (kind "weaver"): it goes where she goes.</summary>
        public Crulanda.World.ZoneInteractable talk;
        public Stage Now { get; private set; }
        /// <summary>How far down the hall she has come (0..1), and how much of the lock is matched (0..1).</summary>
        public float Walked { get; private set; }
        public float Matched { get; private set; }
        /// <summary>Stopped for you or for a fight on her.</summary>
        public bool Holding { get; private set; }
        public Vector3 Gate { get { return gate; } }
        public Vector3 LockAt { get { return lockAt; } }
        EncounterSession session; NavMeshAgent agent; Vector3 gate, lockAt; float total; bool wave1, wave2, risen; float fellAt;

        public void Init(Actor a, EncounterSession s, Vector3 lockPoint)
        {
            actor = a; session = s; agent = GetComponent<NavMeshAgent>(); lockAt = lockPoint;
            gate = NavMesh.SamplePosition(transform.position, out var hit, 3, NavMesh.AllAreas) ? hit.position : transform.position;
            agent.speed = 2.2f; agent.stoppingDistance = .6f; Now = Stage.Waiting;
        }
        /// <summary>Spoken to at the gate: she sets off. False when she is already on her way, done, or down.</summary>
        public bool Begin()
        {
            if (Now != Stage.Waiting || !actor.IsAlive || agent == null || !agent.isOnNavMesh) return false;
            agent.isStopped = false; agent.SetDestination(lockAt); total = Mathf.Max(1, Flat(lockAt - transform.position)); Now = Stage.Walking;
            Say("Keep them off me, then. The lock's on the carriage. Don't let me stop to think.");
            return true;
        }
        /// <summary>Back by the gate after a fall, whole, to start again when spoken to.</summary>
        public void Restart()
        {
            Now = Stage.Waiting; Walked = Matched = 0; wave1 = wave2 = risen = false; Holding = false;
            if (agent != null && agent.isOnNavMesh) { agent.Warp(gate); agent.isStopped = true; } else transform.position = gate;
        }
        /// <summary>The HUD's line under her health.</summary>
        public string Status
        {
            get
            {
                switch (Now)
                {
                    case Stage.Waiting: return "Waiting by the gate";
                    case Stage.Walking: return Holding ? (Beset() ? "Ducking: get them off her" : "Waiting for you") : "Walking to the carriage " + Mathf.RoundToInt(Walked * 100) + "%";
                    case Stage.Matching: return (Holding ? "Waiting for you · " : "Matching the lock ") + Mathf.RoundToInt(Matched * 100) + "%";
                    case Stage.Fallen: return "Fallen: back by the gate soon";
                    default: return "The lock is matched";
                }
            }
        }
        void Update()
        {
            if (session == null || actor == null) return;
            if (talk != null) { talk.position = transform.position; talk.hiddenUntil = Now == Stage.Waiting || Now == Stage.Done ? 0 : float.MaxValue; }   // E is for the bodies round her while she works
            if (!actor.IsAlive && Now != Stage.Fallen && Now != Stage.Done) { Fall(); return; }
            switch (Now)
            {
                case Stage.Walking:
                    {
                        bool far = session.Player == null || !session.Player.IsAlive || Flat(session.Player.transform.position - transform.position) > WaitFor;
                        Holding = far || Beset();
                        if (agent.isOnNavMesh) agent.isStopped = Holding;
                        Walked = Mathf.Clamp01(1 - Flat(lockAt - transform.position) / total);
                        if (!wave1 && Walked >= 1 / 3f) { wave1 = true; session.WeaverWave(this, 1); }
                        if (!wave2 && Walked >= 2 / 3f) { wave2 = true; session.WeaverWave(this, 2); }
                        if (Flat(lockAt - transform.position) < 1.2f && !agent.pathPending)
                        {
                            Now = Stage.Matching; Walked = 1; if (agent.isOnNavMesh) agent.isStopped = true;
                            Say("There. Hush now. It's singing the wrong note, and I have to find the right one.");
                        }
                        break;
                    }
                case Stage.Matching:
                    {
                        Holding = session.Player == null || !session.Player.IsAlive;   // down: she waits for you to come back
                        if (Holding) break;
                        Matched = Mathf.Clamp01(Matched + Time.deltaTime / MatchSeconds);
                        if (!risen && Matched >= .5f) { risen = true; session.WeaverHalfway(this); }
                        if (Matched >= 1) { Now = Stage.Done; Holding = false; session.WeaverDone(this); }
                        break;
                    }
                case Stage.Fallen:
                    if (Time.time - fellAt > BackAfter) session.WeaverBack(this);
                    break;
            }
        }
        /// <summary>Something is on her within reach: she ducks and waits for it to be dealt with.</summary>
        bool Beset()
        {
            foreach (var e in session.Enemies)
                if (e != null && e.Victim == actor && e.actor.IsAlive && Flat(e.transform.position - transform.position) < 5) return true;
            return false;
        }
        void Fall()
        {
            Now = Stage.Fallen; fellAt = Time.time; Holding = false; if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            session.WeaverFell(this);
        }
        public void Say(string line) { session.Message(Name + ": \"" + line + "\""); }
        static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }
    }
}
