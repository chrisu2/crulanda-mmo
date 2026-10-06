using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Inviting sims (Phase 5.2b, 2026-10-06): click a sim and Invite from its frame; it accepts unless it is far from your level
    /// or busy, and joins as a SimCompanion (up to <see cref="MaxPartySims"/>, a party of five with you and Mira). Leave party
    /// (from its party frame) sends it back to the world where it stands. The party is not saved.
    /// </summary>
    public sealed partial class EncounterSession
    {
        public const int MaxPartySims = 3, InviteLevelGap = 5;
        public const float BusyBelow = .15f;
        public readonly List<SimCompanion> PartySims = new List<SimCompanion>();
        /// <summary>The who list (O): every adventurer online (EncounterHud.Who).</summary>
        public bool WhoOpen { get; set; }

        // ---------- the sim you clicked ----------
        public string FocusSimId { get; private set; }
        public SimAdventurer FocusSim { get { var p = SimPopulation.Active; return FocusSimId == null || p == null ? null : p.World.sims.Find(s => s.id == FocusSimId); } }
        /// <summary>The focused sim's body: its figure in the world, or itself in your party.</summary>
        public Transform FocusSimBody
        {
            get
            {
                if (FocusSimId == null) return null;
                var c = PartySims.Find(x => x != null && x.sim.id == FocusSimId); if (c != null) return c.transform;
                var f = SimPopulation.Active?.Find(FocusSimId); return f != null ? f.transform : null;
            }
        }
        public bool InParty(string simId) { return PartySims.Exists(x => x != null && x.sim.id == simId); }
        public SimCompanion PartySim(string simId) { return PartySims.Find(x => x != null && x.sim.id == simId); }
        public void SelectSim(string simId) { FocusSimId = simId; if (simId != null) { FocusVillager = null; FocusMira = false; Target = null; AutoAttack = false; } }

        /// <summary>Why this sim would not join (null: it will).</summary>
        public string InviteRefusal(SimAdventurer s)
        {
            if (s == null) return "Nobody to invite.";
            if (InParty(s.id)) return s.name + " is already in your party.";
            if (InCombat) return "Not while you are fighting.";
            if (PartySims.Count >= MaxPartySims) return "Your party is full.";
            if (Mathf.Abs(s.level - Progress.Level) > InviteLevelGap) return s.name + " declines: \"We're too far apart, friend. Find someone nearer your level.\"";
            if (s.friendly < BusyBelow) return s.name + " declines: \"Not right now. I'm in the middle of something.\"";
            var fig = SimPopulation.Active?.Find(s.id);
            if (fig != null && fig.InFight) return s.name + " is busy fighting.";
            if (fig != null && fig.Hidden) return s.name + " is inside the inn.";
            return null;
        }
        public bool Invite(string simId)
        {
            var pop = SimPopulation.Active; var s = pop?.World.sims.Find(x => x.id == simId);
            var why = InviteRefusal(s); if (why != null) { Message(why); return false; }
            var fig = pop.Find(s.id); if (fig == null) { Message(s.name + " is not here."); return false; }
            var at = fig.transform.position; pop.Figures.Remove(fig); Destroy(fig.gameObject);
            if (NavMesh.SamplePosition(at, out var hit, 6, NavMesh.AllAreas)) at = hit.position + Vector3.up;
            var a = SpawnActor(s.name, content.player, at, EncounterHud.ClassColour(s.classId), s.id, LookForClass(s.classId), s.level, s.variant);
            AddAgent(a.gameObject, 4.6f);
            var c = a.gameObject.AddComponent<SimCompanion>(); a.gameObject.SetActive(true); c.Init(a, this, s, FreeSlot());
            SimGear.Dress(a.GetComponent<ActorVisual>(), s, Items);
            PartySims.Add(c);
            Message(s.name + " has joined your party.");
            return true;
        }
        int FreeSlot() { for (int i = 0; ; i++) if (!PartySims.Exists(x => x != null && x.slot == i)) return i; }
        /// <summary>Sends a party sim back to the world where it stands.</summary>
        public void LeaveParty(string simId)
        {
            var c = PartySim(simId); if (c == null) return;
            foreach (var e in Enemies) if (e != null && e.Victim == c.actor) e.ResetFight();
            c.sim.x = c.transform.position.x; c.sim.z = c.transform.position.z;
            PartySims.Remove(c); Destroy(c.gameObject);
            Message(c.sim.name + " leaves your party.");
            SimPopulation.Active?.Refresh();
        }
        void HealPartySimsAfterRecover()
        {
            foreach (var c in PartySims)
            {
                if (c == null) continue;
                if (!c.actor.IsAlive) c.actor.Health.Revive(c.actor.Health.Pool.Max); else c.actor.Health.ApplyHealing(c.actor.Health.Pool.Max);
                var agent = c.GetComponent<NavMeshAgent>(); if (agent != null) agent.Warp(RecoveryPoint + Vector3.left * (2 + c.slot) - Vector3.up * .1f);
            }
        }
    }
}
