using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Deadmines end-piece (the Adit rework, stage S6, Docs/ADIT_REWORK.md sections 11 and 20; Chris 2026-10-15, Q3 "Build the Cove"; all
    /// GAME-ONLY; the Deadmines is a lesson in shape only): the Dead Line runs on past the Rail Hall into the Black Cove, where Rail-Captain
    /// Danner keeps the stern castle of a moored ore barge.
    /// - <see cref="CoveFinale"/>: Danner's camp stands in the Cove (the layout's COVE_FINALE, tools/wip/dungeon/adit_layout.py). Then he does not
    ///   lie in the dark for the Weaver's escort nor stand up at half the lock (EncounterSession.Weaver), and the carriage rides you out once the
    ///   lock is matched or he is dead (<see cref="RideAllowed"/>). Without it (his camp in the Rail Hall) everything is as before S6.
    /// - The Haul (AditHaul): the rope teams heave the barge off the bar; you stop it by killing them or cutting the warps at the bollards
    ///   ("warps", ZoneBuilder.CoveWarps). If she floats, her crew comes up out of the hold to each of Danner's calls, two at a time (the camps
    ///   released by "event:haul1" and "event:haul2", through <see cref="HaulCall"/> from EncounterSession.Rally).
    /// Nothing here is saved (no save-format change): the barge is aground and the warps whole on every visit.
    /// </summary>
    public sealed partial class EncounterSession
    {
        public const string HaulEvent = "haul", HaulTag = "bargeman", HaulAnchor = "haul";
        /// <summary>The Haul in the Black Cove this visit (null where the zone has no rope teams).</summary>
        public AditHaul Haul { get; private set; }
        int haulCalls;

        /// <summary>Danner's camp stands in the Black Cove (a cave of style "cove"): the S6 finale. Read from the data, so the layout's one switch
        /// decides it.</summary>
        public bool CoveFinale
        {
            get
            {
                var camps = Zone != null ? Zone.Zone.camps : null; if (camps == null) return false;
                foreach (var c in camps)
                {
                    if (c == null || c.tag != "railcaptain" || string.IsNullOrEmpty(c.cave)) continue;
                    var h = Hollow.All.Find(o => o.Name == c.cave);
                    return h != null && h.Style == "cove";
                }
                return false;
            }
        }
        /// <summary>Danner fell this visit (his body lies where he fell; camps are back on every visit).</summary>
        public bool DannerDead
        {
            get
            {
                bool seen = false;
                foreach (var e in Enemies) { if (e == null || !e.Camp || !e.Elite || e.MobName != DannerName) continue; seen = true; if (e.actor.IsAlive) return false; }
                return seen;
            }
        }
        /// <summary>The carriage will take you up the line: the lock matched, or (the Cove's finale) Danner dead.</summary>
        public bool RideAllowed { get { return Progress.keys.Contains(WeaverLock) || (CoveFinale && DannerDead); } }

        /// <summary>The rope teams' camps: the bargemen there from the start (not the ones the hold sends up later).</summary>
        List<int> RopeTeamCamps()
        {
            var list = new List<int>(); var camps = Zone != null ? Zone.Zone.camps : null; if (camps == null) return list;
            for (int c = 0; c < camps.Length; c++) if (camps[c] != null && camps[c].tag == HaulTag && string.IsNullOrEmpty(camps[c].after)) list.Add(c);
            return list;
        }
        /// <summary>On arrival (StartAdit): the barge aground, the warps whole, the rope teams on them.</summary>
        void StartHaul()
        {
            haulCalls = 0;
            if (Haul != null) { Destroy(Haul.gameObject); Haul = null; }
            if (Zone == null || !Zone.Anchors.TryGetValue(HaulAnchor, out var at)) return;
            var teams = RopeTeamCamps(); if (teams.Count == 0) return;
            var haulers = Enemies.FindAll(e => e != null && e.Camp && teams.Contains(e.CampIndex));
            if (haulers.Count == 0) { StartCoroutine(HaulWhenTheTeamsStand(at, teams)); return; }   // (R129: their camps not spawned yet on this load)
            MakeHaul(at, haulers);
        }
        System.Collections.IEnumerator HaulWhenTheTeamsStand(Vector3 at, List<int> teams)
        {
            for (float until = Time.time + 30; Time.time < until; )
            {
                yield return null;
                if (Haul != null || Zone == null) yield break;
                var haulers = Enemies.FindAll(e => e != null && e.Camp && teams.Contains(e.CampIndex));
                if (haulers.Count > 0) { MakeHaul(at, haulers); yield break; }
            }
        }
        void MakeHaul(Vector3 at, List<EncounterEnemy> haulers)
        {
            if (Zone.CoveWarps != null) foreach (var w in Zone.CoveWarps) if (w != null) w.SetActive(true);
            foreach (var i in Zone.Interactables) if (i.kind == "warps") i.hiddenUntil = 0;
            var go = new GameObject("The Haul"); go.transform.SetParent(transform, false);
            Haul = go.AddComponent<AditHaul>(); Haul.Init(this, at, haulers);
        }
        public void HaulBegun(AditHaul h)
        {
            Message(DannerName + " bellows from the stern castle: \"Haul! Get her off the bar before they reach us!\" On the quay the rope teams spit on their hands and lay back on the warps.");
        }
        public void HaulMark(AditHaul h, int third)
        {
            Message(third == 1 ? "The barge groans on the bar. The warps creak round the bollards, and the bargemen haul in time, hand over hand."
                               : "Her bow lifts. The rope teams are singing the stroke now: one more good heave and she floats.");
        }
        public void HaulStopped(AditHaul h, string why)
        {
            Message(why == "cut" ? "The warps part under the blade and whip away into the black water. The barge settles back on the bar: she stays fast."
                                 : "The last of the rope teams is down. The warps go slack, and the barge stays fast on the bar.");
        }
        public void HaulFloated(AditHaul h)
        {
            Message("With a long grinding lurch the barge comes off the bar and rides free on her warps. Below decks a hatch bangs open: her crew is up, and Danner has more to call on now.");
        }
        /// <summary>One of Danner's calls (EncounterSession.Rally) with the barge afloat: two of her crew up out of the hold, at most twice a visit
        /// (the camps after "event:haul1" and "event:haul2"). Returns how many come.</summary>
        int HaulCall(EncounterEnemy lord, Actor foe)
        {
            if (Haul == null || Haul.Now != AditHaul.Stage.Floated || lord == null || lord.MobName != DannerName || haulCalls >= 2) return 0;
            haulCalls++;
            return ReleaseAfter("event:" + HaulEvent + haulCalls, foe, lord, "Up through the hatch: two more of the barge's crew, boat-hooks in hand.");
        }
        /// <summary>E at the bollards: the warps cut, unless a hauler is on that rope beside you.</summary>
        void CutWarps(ZoneInteractable i)
        {
            if (Haul == null) { Message("The warps are slack and wet; there is nothing on them."); return; }
            if (Haul.Now == AditHaul.Stage.Stopped || Haul.Now == AditHaul.Stage.Floated) { Message(Haul.Now == AditHaul.Stage.Floated ? "She is afloat already: cutting her loose now would only send her down the cove." : "The warps are slack: she is fast on the bar."); return; }
            foreach (var e in Haul.Haulers)
                if (e != null && e.actor.IsAlive && e.Victim == null && Vector3.Distance(e.transform.position, i.position) < AditHaul.RopeReach)
                { Message("A bargeman has his hands on that warp. Deal with him first."); return; }
            Haul.CutWarps();
            if (Zone.CoveWarps != null) foreach (var w in Zone.CoveWarps) if (w != null) w.SetActive(false);
            i.hiddenUntil = float.MaxValue;
        }
    }
}
