using System;
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
            var fig = SimPopulation.Active?.Find(s.id);
            if (fig != null && fig.Hidden) return s.name + " is inside the inn.";   // where it is comes before its mood (a full run of 2026-10-07 caught the order)
            if (fig != null && fig.InFight) return s.name + " is busy fighting.";
            if (SimMemory.Refuses(s)) return s.name + " declines: \"Not with you.\"";   // a rival (5.5)
            bool friend = SimMemory.Of(s) == SimMemory.Standing.Friend || SameGuild(s);   // a friend (or a guild-mate, round 27) stretches a little further, and is never too busy for you
            if (Mathf.Abs(s.level - Progress.Level) > InviteLevelGap + (friend ? 2 : 0)) return s.name + " declines: \"We're too far apart, friend. Find someone nearer your level.\"";
            if (s.friendly < BusyBelow && !friend) return s.name + " declines: \"Not right now. I'm in the middle of something.\"";
            return null;
        }
        public bool Invite(string simId, bool returning = false)
        {
            var pop = SimPopulation.Active; var s = pop?.World.sims.Find(x => x.id == simId);
            var why = returning ? (s == null ? "Nobody to invite." : InParty(s.id) ? s.name + " is already in your party." : PartySims.Count >= MaxPartySims ? "Your party is full." : null) : InviteRefusal(s);   // a sim coming back to the party it was in (round 27) is not asked again
            if (why != null) { Message(why); return false; }
            var fig = pop.Find(s.id); if (fig == null) { Message(s.name + " is not here."); return false; }
            var at = fig.transform.position; pop.Figures.Remove(fig); Destroy(fig.gameObject);
            if (NavMesh.SamplePosition(at, out var hit, 6, NavMesh.AllAreas)) at = hit.position + Vector3.up;
            var a = SpawnActor(s.name, content.player, at, EncounterHud.ClassColour(s.classId), s.id, LookForClass(s.classId), s.level, s.variant);
            AddAgent(a.gameObject, 4.6f);
            var c = a.gameObject.AddComponent<SimCompanion>(); a.gameObject.SetActive(true); c.Init(a, this, s, FreeSlot());
            SimGear.Dress(a.GetComponent<ActorVisual>(), s, Items);
            PartySims.Add(c);
            if (!returning) Message(s.name + " has joined your party.");
            return true;
        }
        /// <summary>How long a sim takes to come from another zone when invited (seconds; tests shorten it).</summary>
        public static float InviteTravelSeconds = 30;
        readonly List<string> coming = new List<string>();
        /// <summary>/invite by name (Round 24, playtest note 67): a sim standing here joins at once; one online elsewhere accepts by
        /// the same rules, sets out, and arrives at the road's end nearest you after <see cref="InviteTravelSeconds"/>.</summary>
        public bool InviteByName(string name)
        {
            var pop = SimPopulation.Active; if (pop == null || string.IsNullOrEmpty(name)) { Message("Invite whom? /invite <name>."); return false; }
            // The name as typed, else by its start (a first name): among those online, one standing here before one away (first names repeat).
            var s = pop.World.sims.Find(x => string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));
            if (s == null)
            {
                var hour = Crulanda.World.WorldClock.Hour;
                var like = pop.World.sims.FindAll(x => x.name.StartsWith(name, StringComparison.OrdinalIgnoreCase) && (x.IsOnlineAt(hour) || InParty(x.id)));
                s = like.Find(x => x.zone == ZoneId && pop.Find(x.id) != null) ?? (like.Count > 0 ? like[0] : pop.World.sims.Find(x => x.name.StartsWith(name, StringComparison.OrdinalIgnoreCase)));
            }
            if (s == null) { Message("Nobody called " + name + "."); return false; }
            if (!s.IsOnlineAt(Crulanda.World.WorldClock.Hour) && !InParty(s.id)) { Message(s.name + " is not online."); return false; }
            if (s.zone == ZoneId && pop.Find(s.id) != null) return Invite(s.id);
            var why = InviteRefusal(s); if (why != null) { Message(why); return false; }
            if (coming.Contains(s.id)) { Message(s.name + " is already on the way."); return false; }
            coming.Add(s.id); ChatSay(ChatChannel.Party, s.name, "on my way from " + ZoneName(s.zone) + ", " + Mathf.RoundToInt(InviteTravelSeconds) + "s");
            StartCoroutine(Arrive(s));
            return true;
        }
        System.Collections.IEnumerator Arrive(SimAdventurer s)
        {
            yield return new WaitForSeconds(InviteTravelSeconds);
            coming.Remove(s.id);
            var pop = SimPopulation.Active; if (pop == null || Player == null) yield break;
            // At the road's end nearest you (or by you, where the zone has no roads).
            var at = Player.transform.position; float best = float.MaxValue;
            if (Zone != null && Zone.Zone.exits != null) foreach (var e in Zone.Zone.exits) { var p = Zone.Ground(e.at); float d = Vector3.Distance(p, Player.transform.position); if (d < best) { best = d; at = p; } }
            s.zone = ZoneId; s.x = at.x; s.z = at.z; s.onlineFrom = s.onlineFrom; pop.Refresh();
            if (pop.Find(s.id) == null) { Message(s.name + " could not get here."); yield break; }
            if (Invite(s.id)) ChatSay(ChatChannel.Party, s.name, "here, where to?");
        }
        /// <summary>A sim's invitation to you (round 27): who, and until when it stands (thirty seconds). The HUD shows Accept / Decline.</summary>
        public SimAdventurer PendingInvite { get; private set; }
        float pendingUntil;
        public void InvitedBy(SimAdventurer s)
        {
            if (s == null || InParty(s.id) || PartySims.Count >= MaxPartySims || PendingInvite != null) return;
            PendingInvite = s; PendingGuild = null; pendingUntil = Time.time + 30;
            Message(s.name + " invites you to a group. (Accept or decline above the bars.)");
        }
        /// <summary>Accepting a sim's invitation: it joins you, here at once or from another zone by the road (as /invite).</summary>
        public void AnswerInvite(bool accept)
        {
            var s = PendingInvite; var guild = PendingGuild; PendingInvite = null; PendingGuild = null; if (s == null) return;
            if (guild != null) { if (accept) JoinGuild(guild, s); else ChatSay(ChatChannel.Whisper, s.name, s.chatty > .65f ? "np, offer stands" : "No matter. The offer stands."); return; }   // round 27
            if (!accept) { ChatSay(ChatChannel.Whisper, s.name, s.chatty > .65f ? "np, another time" : "Another time, then."); return; }
            if (s.zone == ZoneId && SimPopulation.Active?.Find(s.id) != null) { if (Invite(s.id, true)) Message("You join " + s.name + "'s group."); }
            else InviteByName(s.name);
        }
        void TickInvite() { if (PendingInvite != null && Time.time > pendingUntil) { Message(PendingInvite.name + "'s invitation lapses."); PendingInvite = null; PendingGuild = null; } }
        int FreeSlot() { for (int i = 0; ; i++) if (!PartySims.Exists(x => x != null && x.slot == i)) return i; }
        /// <summary>/assist [name] (5.6): your target becomes what that party member (or any of them) is fighting.</summary>
        public bool Assist(string name = null)
        {
            SimCompanion c = string.IsNullOrEmpty(name) ? null : PartySims.Find(x => x != null && x.sim.name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(name) && c == null) { Message("Nobody called " + name + " is in your party."); return false; }
            EncounterEnemy q = c != null ? c.Quarry : null;
            if (q == null) foreach (var p in PartySims) { if (p == null) continue; q = p.Quarry; if (q != null) break; }
            if (q == null || !q.actor.IsAlive) { Message("Nobody in the party is fighting anything."); return false; }
            Select(q); Message("Assisting: " + q.actor.DisplayName + "."); return true;
        }
        /// <summary>/lead [name] (5.6): a party sim leads a run to a camp near its level (its bold ones gladly; the rest do too, less sure).</summary>
        public bool Lead(string name = null)
        {
            if (PartySims.Count == 0) { Message("You are not in a party: /invite someone first."); return false; }
            SimCompanion c = string.IsNullOrEmpty(name) ? null : PartySims.Find(x => x != null && x.sim.name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(name) && c == null) { Message("Nobody called " + name + " is in your party."); return false; }
            if (c == null) { c = PartySims.Find(x => x != null && x.sim.bold > .5f) ?? PartySims.Find(x => x != null); }
            if (c == null) return false;
            foreach (var p in PartySims) if (p != null && p != c) p.StopLeading();
            if (InCombat) { Message("Not while you are fighting."); return false; }
            // Two or more sims with you and a dungeon near your level: the run goes into it (round 27); otherwise a camp.
            if (PartySims.Count >= 2 && c.LeadDungeon(out _) != null) { Message(c.sim.name + " leads the way into " + c.Dungeon + "."); return true; }
            var camp = c.Lead();
            if (camp == null) { Message(c.sim.name + " knows no camp fit for the party near here."); return false; }
            Message(c.sim.name + " leads the way to the " + camp.ToLower() + ".");
            return true;
        }
        /// <summary>/dungeon (round 27): a party sim (the boldest) leads you through the zone's dungeon, camp by camp.</summary>
        public bool LeadDungeon()
        {
            if (PartySims.Count == 0) { Message("You are not in a party: /invite someone first."); return false; }
            if (InCombat) { Message("Not while you are fighting."); return false; }
            var c = PartySims.Find(x => x != null && x.sim.bold > .5f) ?? PartySims.Find(x => x != null); if (c == null) return false;
            foreach (var p in PartySims) if (p != null && p != c) p.StopLeading();
            if (c.LeadDungeon(out var why) == null) { Message(c.sim.name + ": " + why); return false; }
            Message(c.sim.name + " leads the way into " + c.Dungeon + " (" + c.CampsLeft + " camps)."); return true;
        }
        /// <summary>Sends a party sim back to the world where it stands.</summary>
        public void LeaveParty(string simId, bool kicked = false)
        {
            var c = PartySim(simId); if (c == null) return;
            c.Remember(kicked);
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
