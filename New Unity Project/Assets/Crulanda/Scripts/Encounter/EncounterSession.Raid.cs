using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The raid (WORLD_BOSS_1.md section 2, build plan steps 1 and 2; 2026-10-19): "Form raid" turns your party into Party 1 of a raid of ten
    /// (RaidGroup), new members fill Party 2; the sims in it are the same SimCompanions as a party's (PartySims holds all nine), each with its
    /// raid part and party. Party-wide helps reach the caster's party, raid-wide ones all ten (RaidReach). The ready check, the raid window
    /// (/raid) and the call to arms (/raid call: RaidCall) are here; the frames are EncounterHud.Raid's. A party of five is unchanged: out of a
    /// raid every rule here gives what it gave before. Not saved: a raid is for the session.
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>The raid you lead, or null (a party of five, or alone).</summary>
        public RaidGroup Raid { get; private set; }
        public bool InRaid { get { return Raid != null; } }
        /// <summary>How many sims the group holds: four in a party, nine in a raid.</summary>
        public int MaxGroupSims { get { return InRaid ? RaidGroup.Size - 1 : MaxPartySims; } }
        /// <summary>The raid window (/raid).</summary>
        public bool RaidOpen { get; set; }
        /// <summary>The boss frame's subject: the raid's boss while it is up (step 3 sets the Unraveller); null shows the target's frame as now.</summary>
        public EncounterEnemy RaidBoss { get; set; }
        /// <summary>Her pin state for the boss frame, in seconds left pinned (0: free). The harpoons' timer arrives in step 4.</summary>
        public float RaidBossPinned { get; set; }
        /// <summary>The clock against her enrage (seconds left; 0: none yet).</summary>
        public float RaidBossEnrage { get; set; }

        /// <summary>A raid member's record for a sim with you: its class, level and gear level (the mean of its worn pieces' levels).</summary>
        RaidMember MemberOf(SimCompanion c)
        {
            var levels = new List<int>();
            if (Items != null) foreach (var id in c.sim.wornIds) { var d = Items.Get(id); if (d != null) levels.Add(d.level); }
            return new RaidMember { id = c.sim.id, name = c.sim.name, classId = c.sim.classId, level = c.sim.level, gearLevel = RaidFrames.GearLevel(levels) };
        }
        RaidMember PlayerMember()
        {
            var levels = new List<int>();
            if (Items != null) foreach (var st in Progress.equipment) { if (st.Empty) continue; var d = Items.Get(st.item); if (d != null) levels.Add(d.level); }
            return new RaidMember { id = RaidGroup.You, name = "You", classId = ClassDef != null ? ClassDef.id : Progress.classId, level = Progress.Level, player = true, gearLevel = RaidFrames.GearLevel(levels) };
        }

        /// <summary>"Form raid": your party (you and up to four sims) becomes Party 1 of a raid of ten. False when you are in one already, or queued.</summary>
        public bool FormRaid()
        {
            if (InRaid) { Message("You are already in a raid."); return false; }
            if (QueuedFor != null || QueueRunZone != null) { Message("Not while you are in the dungeon queue."); return false; }
            var party = new List<RaidMember>(); foreach (var c in PartySims) if (c != null) party.Add(MemberOf(c));
            Raid = RaidGroup.Form(PlayerMember(), party);
            Message("You form a raid. Your party is Party 1; new members fill Party 2.");
            ChatSay(ChatChannel.Party, "You", "raid formed");
            return true;
        }
        /// <summary>You leave the raid: every sim goes back to the world where it stands, and the raid is over.</summary>
        public void LeaveRaid()
        {
            if (!InRaid) return;
            calling = false;
            foreach (var c in PartySims.ToArray()) if (c != null) LeaveParty(c.sim.id);
            Raid = null; RaidOpen = false; RaidBoss = null;
            Message("You leave the raid.");
        }
        /// <summary>A sim that joined the group joins the raid too (Invite; its part the first of its class's the raid lacks).</summary>
        void RaidJoined(SimCompanion c) { if (InRaid && c != null && !Raid.Has(c.sim.id)) Raid.Join(MemberOf(c), false); }
        void RaidLeft(string simId) { if (InRaid) Raid.Leave(simId); }

        /// <summary>The raid id of someone in your group: "you" for you and Mira (who keeps at your side), a sim's id, else null.</summary>
        string RaidIdOf(Actor a)
        {
            if (a == null) return null;
            if (a == Player || (Companion != null && a == Companion.actor)) return RaidGroup.You;
            foreach (var c in PartySims) if (c != null && c.actor == a) return c.sim.id;
            foreach (var p in SimPets) if (p != null && p.actor == a && p.Master != null) return p.Master.sim.id;   // a sim's wolf runs with its master's party
            return null;
        }
        /// <summary>Two of the group in the same party: always out of a raid (the whole group is one party); in a raid, both in Party 1 or both in
        /// Party 2. Someone outside the group (a dummy, Mother Quillet) counts as with everyone.</summary>
        public bool SameParty(Actor a, Actor b)
        {
            if (!InRaid || a == b) return true;
            string x = RaidIdOf(a), y = RaidIdOf(b); if (x == null || y == null) return true;
            return Raid.PartyOf(x) == Raid.PartyOf(y);
        }
        /// <summary>Whether a party-wide help of yours (a mending round, a chant, a kept vow) reaches this one: your own party in a raid, everyone otherwise.</summary>
        public bool PartyWide(Actor who) { return SameParty(Player, who); }
        /// <summary>The sims in a sim's own party (all the group's sims out of a raid), itself included.</summary>
        public List<SimCompanion> PartyMatesOf(SimCompanion c)
        {
            if (!InRaid || c == null) return PartySims;
            int p = Raid.PartyOf(c.sim.id); return PartySims.FindAll(x => x != null && Raid.PartyOf(x.sim.id) == p);
        }
        /// <summary>The sims in your own party (all of them out of a raid).</summary>
        public List<SimCompanion> MyPartySims { get { return !InRaid ? PartySims : PartySims.FindAll(x => x != null && Raid.PartyOf(x.sim.id) == Raid.PartyOf(RaidGroup.You)); } }
        /// <summary>A sim's part in the raid (null out of one: SimCompanion.Role reckons it by its party as before).</summary>
        public SimCompanion.PartyRole? RaidRoleOf(SimCompanion c) { var m = InRaid && c != null ? Raid.Find(c.sim.id) : null; return m != null ? m.role : (SimCompanion.PartyRole?)null; }

        // ---------- the ready check ----------
        public const float ReadyCheckSeconds = 30, ReadyReach = 80;
        float readyUntil;
        /// <summary>A ready check (section 2.4): the sims answer in a second or few, ready when alive and near; you are ready as its leader.</summary>
        public bool ReadyCheck()
        {
            if (!InRaid) { Message("You are not in a raid."); return false; }
            if (Raid.Checking) { Message("A ready check is already going."); return false; }
            Raid.BeginReadyCheck(); readyUntil = Time.time + ReadyCheckSeconds;
            Message("You begin a ready check.");
            foreach (var c in PartySims) if (c != null) StartCoroutine(SimReady(c, .5f + Random.value * 2.5f));
            return true;
        }
        IEnumerator SimReady(SimCompanion c, float after)
        {
            yield return new WaitForSeconds(after);
            if (!InRaid || !Raid.Checking || c == null) yield break;
            bool ready = c.actor != null && c.actor.IsAlive && Player != null && Vector3.Distance(c.transform.position, Player.transform.position) <= ReadyReach;
            Raid.Answer(c.sim.id, ready);
            if (!ready) ChatSay(ChatChannel.Party, c.sim.name, c.actor != null && c.actor.IsAlive ? "not ready, still on my way" : "dead, need a res");
            if (Raid.AllAnswered) EndReadyCheck();
        }
        /// <summary>Your own answer, when someone else leads (a sim's raid, later).</summary>
        public void AnswerReadyCheck(bool ready) { if (InRaid && Raid.Checking) { Raid.Answer(RaidGroup.You, ready); if (Raid.AllAnswered) EndReadyCheck(); } }
        void EndReadyCheck()
        {
            if (!InRaid || !Raid.Checking) return;
            var not = Raid.EndReadyCheck();
            Message(not.Count == 0 ? "Everyone is ready." : "Not ready: " + string.Join(", ", not) + ".");
        }
        void TickRaid()
        {
            if (!InRaid) return;
            if (Raid.Checking && Time.time > readyUntil) EndReadyCheck();
        }

        // ---------- the call to arms (step 2) ----------
        /// <summary>Tests shorten the answers' waits (1: as written, 20-60 s here, a minute or two from afar).</summary>
        public static float RaidCallTimeScale = 1;
        bool calling;
        public bool Calling { get { return calling; } }
        /// <summary>The answers of the last call, in order (the raid window and the tests read them).</summary>
        public List<RaidAnswer> LastCall { get; private set; } = new List<RaidAnswer>();
        /// <summary>
        /// The call to arms (section 2.3; the raid window's button, /raid call): forms the raid if you are not in one, posts the LFM line in Zone
        /// chat, and the answers come by RaidCall: your guild, the zone's sims, friends. Each who comes buys its salt and harpoons on the way,
        /// says so, and joins (from another zone after its road). When too few come: "Not enough of the Lowtowns' adventurers are about."
        /// </summary>
        public bool CallToArms()
        {
            if (!InRaid && !FormRaid()) return false;
            if (Raid.Full) { Message("The raid is full."); return false; }
            if (calling) { Message("Your call is already out."); return false; }
            var pop = SimPopulation.Active; float hour = Crulanda.World.WorldClock.Hour;
            ChatSay(ChatChannel.Zone, "You", Raid.LfmLine);
            var roster = new List<RaidCandidate>();
            if (pop != null) foreach (var s in pop.World.sims) roster.Add(RaidCandidate.Of(s, hour, InParty(s.id)));
            LastCall = RaidCall.Answers(Raid, roster, ZoneId, InGuild ? Guild : "", Mathf.RoundToInt(hour * 100) + Raid.Count);
            calling = true; StartCoroutine(Answering(LastCall));
            return true;
        }
        IEnumerator Answering(List<RaidAnswer> answers)
        {
            float waited = 0;
            foreach (var a in answers)
            {
                float wait = a.after * RaidCallTimeScale - waited; if (wait > 0) yield return new WaitForSeconds(wait); waited = a.after * RaidCallTimeScale;
                if (!calling || !InRaid) yield break;
                if (!a.comes) { ChatSay(ChatChannel.Zone, a.name, a.line); continue; }
                if (Raid.Full) break;
                var pop = SimPopulation.Active; var s = pop?.World.sims.Find(x => x.id == a.id); if (s == null || InParty(s.id)) continue;
                RaidCall.BuyKit(s, id => { var d = Items?.Get(id); return d != null ? Mathf.Max(1, d.value) : 1; });
                ChatSay(a.source == RaidSource.Guild ? ChatChannel.Guild : a.source == RaidSource.Friend ? ChatChannel.Whisper : ChatChannel.Zone, s.name, a.line);
                if (!(s.zone == ZoneId && pop.Find(s.id) != null))
                {
                    // From another zone (or not standing here): it comes in by the road's end nearest you, as an invited sim does (Arrive).
                    var at = Player != null ? Player.transform.position : Vector3.zero; float best = float.MaxValue;
                    if (Zone != null && Zone.Zone.exits != null && Player != null) foreach (var e in Zone.Zone.exits) { var p = Zone.Ground(e.at); float d = Vector3.Distance(p, Player.transform.position); if (d < best) { best = d; at = p; } }
                    s.zone = ZoneId; s.x = at.x; s.z = at.z; pop.Refresh();
                    if (pop.Find(s.id) == null) { Message(s.name + " could not get here."); continue; }
                }
                if (Invite(s.id, true)) ChatSay(ChatChannel.Party, s.name, a.role == SimCompanion.PartyRole.Tank ? "here, I'll tank" : a.role == SimCompanion.PartyRole.Healer ? "here, healing" : "here");
            }
            calling = false;
            if (InRaid && !Raid.Full) Message(RaidCall.EmptyZoneLine(ZoneName(ZoneId)));
        }

        /// <summary>/raid [call|ready|arrange|leave|form]: alone the window.</summary>
        public void RaidCommand(string arg)
        {
            switch ((arg ?? "").Trim().ToLowerInvariant())
            {
                case "": RaidOpen = !RaidOpen; return;
                case "form": FormRaid(); RaidOpen = true; return;
                case "call": CallToArms(); return;
                case "ready": case "check": ReadyCheck(); return;
                case "arrange": if (InRaid) { Raid.Arrange(); Message("The parties are set out: a tank and a healer in each."); } else Message("You are not in a raid."); return;
                case "leave": if (InRaid) LeaveRaid(); else Message("You are not in a raid."); return;
                // You in the Unraveller's harpoon rotation, or out of it (WORLD_BOSS_1.md 3.5): your turn shows on the boss frame; set at the pull.
                case "trapper": TrapperId = TrapperId == RaidGroup.You ? null : RaidGroup.You; Message(TrapperId == RaidGroup.You ? "You are the raid's Trapper: lay salt snares on the brood's path (Tibb Marrow sells them)." : "The raid's Ranger is the Trapper again."); return;
                case "harpoon": HarpoonYou = !HarpoonYou; Message(HarpoonYou ? "You are in the harpoon rotation from her next pull: your turn shows on the boss frame." : "You are out of the harpoon rotation."); return;
                default: Message("/raid alone opens the raid window; /raid form, call, ready, arrange, harpoon, leave."); return;
            }
        }
        /// <summary>The raid window's lockout line (section 2.4): whether you roll for her components this week (Lockouts.WorldBoss).</summary>
        public string RaidLockoutLine()
        {
            var left = Lockouts.Left(Progress, RaidGroup.BossLockKey, Lockouts.UtcNow());
            return left > System.TimeSpan.Zero ? "Locked out for " + Lockouts.Words(left) + ": you may fight her, but roll for nothing." : "Not locked out: you roll for her pieces this week.";
        }
    }
}
