using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Haul (the Adit rework, stage S6, Docs/ADIT_REWORK.md E5 and section 20; Chris 2026-10-15, Q3 "Build the Cove"; GAME-ONLY): in the
    /// Black Cove the laden ore barge lies aground on the bar beside the scaffold, and two rope teams of Sandthrone bargemen on the quay heave
    /// her off by her warps, hand over hand round the timber bollards. No engine, no capstan, no machine of any kind (Chris, 2026-10-11: "i
    /// told you NO Machines!"): people on ropes.
    /// - It starts when you come onto the quay (within <see cref="StartReach"/> of the "haul" anchor), or when anyone lays a hand on a hauler.
    /// - It goes on only while bargemen are on the ropes: alive, not fighting, and within <see cref="RopeReach"/> of their place; as fast as the
    ///   share of them still hauling (all of them: <see cref="HaulSeconds"/> to float her).
    /// - Every hauler dead, or the warps cut at the bollards (E, with nobody on that rope), and she stays fast: the haul is over.
    /// - Floated, her crew comes up out of the hold, and each of Danner's calls brings two more of them (EncounterSession.Haul).
    /// Nothing is saved: the barge is aground again on the next visit. The session makes it (EncounterSession.StartHaul) and answers its moments.
    /// </summary>
    public sealed class AditHaul : MonoBehaviour
    {
        public enum Stage { Waiting, Hauling, Stopped, Floated }
        /// <summary>How long the haul takes with every hauler on the ropes; how near the quay's north end you come to start it; how far from
        /// their place a hauler may stray and still be on the rope (metres). Tests shorten the first.</summary>
        public static float HaulSeconds = 75, StartReach = 26, RopeReach = 8;
        public Stage Now { get; private set; }
        /// <summary>How far she is hauled off the bar (0..1), and how many are on the ropes this frame.</summary>
        public float Hauled { get; private set; }
        public int Pulling { get; private set; }
        /// <summary>The warps cut at the bollards.</summary>
        public bool Cut { get; private set; }
        /// <summary>Why it stopped ("cut", "haulers"), or null.</summary>
        public string StoppedBy { get; private set; }
        public IReadOnlyList<EncounterEnemy> Haulers { get { return haulers; } }
        EncounterSession session; Vector3 start; readonly List<EncounterEnemy> haulers = new List<EncounterEnemy>(); int marks;

        public void Init(EncounterSession s, Vector3 startAt, IEnumerable<EncounterEnemy> team)
        {
            session = s; start = startAt; haulers.Clear(); haulers.AddRange(team); Now = Stage.Waiting; Hauled = 0; marks = 0;
        }
        /// <summary>The bargemen on the ropes now: alive, nobody to fight, at their place.</summary>
        int OnTheRopes()
        {
            int n = 0;
            foreach (var e in haulers)
            {
                if (e == null || e.actor == null || !e.actor.IsAlive || e.Victim != null) continue;
                var p = e.transform.position; if (Vector2.Distance(new Vector2(p.x, p.z), e.CampCenter) > RopeReach) continue;
                n++;
            }
            return n;
        }
        bool AnyAlive() { foreach (var e in haulers) if (e != null && e.actor != null && e.actor.IsAlive) return true; return false; }
        bool AnyFighting() { foreach (var e in haulers) if (e != null && e.actor != null && e.actor.IsAlive && e.Victim != null) return true; return false; }
        /// <summary>Starts it now (you on the quay, a hauler struck; tests). False unless it was waiting.</summary>
        public bool Begin()
        {
            if (Now != Stage.Waiting || session == null) return false;
            Now = Stage.Hauling; session.HaulBegun(this); return true;
        }
        /// <summary>The warps cut: she stays fast (false when the haul is over already or someone is on the rope by them).</summary>
        public bool CutWarps()
        {
            if (Now == Stage.Stopped || Now == Stage.Floated) return false;
            Cut = true; Stop("cut"); return true;
        }
        void Stop(string why)
        {
            Now = Stage.Stopped; StoppedBy = why; Pulling = 0; Pose(false);
            session.HaulStopped(this, why);
        }
        /// <summary>The haulers' hands on the ropes (the working pose) while they haul.</summary>
        void Pose(bool hauling)
        {
            foreach (var e in haulers)
            {
                if (e == null) continue; var v = e.GetComponent<ActorVisual>(); if (v == null) continue;
                bool on = hauling && e.actor != null && e.actor.IsAlive && e.Victim == null;
                if (on && v.Pose == ActorPose.None) v.Pose = ActorPose.Work; else if (!on && v.Pose == ActorPose.Work) v.Pose = ActorPose.None;
            }
        }
        void Update()
        {
            if (session == null || session.Paused) return;
            switch (Now)
            {
                case Stage.Waiting:
                    {
                        var p = session.Player;
                        if ((p != null && p.IsAlive && Vector3.Distance(p.transform.position, start) <= StartReach) || AnyFighting()) Begin();
                        break;
                    }
                case Stage.Hauling:
                    {
                        if (!AnyAlive()) { Stop("haulers"); break; }
                        Pulling = OnTheRopes(); Pose(true);
                        Hauled = Mathf.Clamp01(Hauled + Time.deltaTime / Mathf.Max(.01f, HaulSeconds) * Pulling / Mathf.Max(1, haulers.Count));
                        while (marks < 2 && Hauled >= (marks + 1) / 3f) { marks++; session.HaulMark(this, marks); }
                        if (Hauled >= 1) { Now = Stage.Floated; Pulling = 0; Pose(false); session.HaulFloated(this); }
                        break;
                    }
            }
        }
        /// <summary>The HUD's or a test's line: how the haul stands.</summary>
        public string Status
        {
            get
            {
                switch (Now)
                {
                    case Stage.Waiting: return "The barge lies aground";
                    case Stage.Hauling: return (Pulling > 0 ? "Hauling her off the bar " : "The ropes are slack ") + Mathf.RoundToInt(Hauled * 100) + "%";
                    case Stage.Floated: return "Afloat on her warps";
                    default: return Cut ? "The warps are cut: she stays fast" : "Nobody on the ropes: she stays fast";
                }
            }
        }
    }
}
