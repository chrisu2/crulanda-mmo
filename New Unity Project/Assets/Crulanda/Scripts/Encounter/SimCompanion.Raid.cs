using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A sim's new jobs (Docs/WORLD_BOSS_1.md 3.5; build steps 4 and 5; the choices are RaidJobs', pure and tested):
    /// - out of the ground (shared with the dungeons): a sim standing in any mark the game draws on the ground (GroundMarks: a heavy blow's,
    ///   a warned hold's, a trick's, a Hollow's dust, the loop's snap, her unmade ground and her hunt's five metres) walks to the nearest spot
    ///   clear of them first, a mend in hand landing before it goes;
    /// - in her fight: the harpoon rotation (a harpooner throws when its turn comes and her pin has 2 s left), the interrupt rotation on the
    ///   Feeding (a harpoon in the channel when the kick will not do), the three salts on the swallowed, the brood (the tank not holding her and
    ///   Party 2's damage-dealers: salt, then a blow that shatters), the hunted into the river and out 3 s after, and the tank swap
    ///   (SimCompanion's taunt asks RaidJobs.TauntHer). Salt and harpoons come from its bags (three of each bought on the way, RaidCall.BuyKit),
    ///   and from Tibb's barrel and Ketta's rack on the apron when it runs out.
    /// - mistakes on purpose: now and then a sim is late with its harpoon, does not salt, or stands in the hunt (RaidJobs.Misses: more at 18
    ///   than at 20, more when rash), so the raid is never perfect.
    /// </summary>
    public sealed partial class SimCompanion
    {
        float silencedUntil, groundLookAt, riverUntil, harpoonLateUntil = -1, saltSkipUntil, nextBroodSalt, raidRollAt; bool steppingOut; Actor huntSeen; bool huntIgnored;
        string harpoonTurnSeen; float saltRolledFor = -2; bool saltMissed;
        /// <summary>The classes that work the Weave (her lighthouse, her hunt): the Mage, the Druid, the Cleric, the Shaman, the Salt-Mender, the Archivist.</summary>
        public static bool WorksWeave(string classId) { return classId == "class.mage" || classId == "class.druid" || classId == "class.cleric" || classId == "class.shaman" || classId == "class.saltmender" || classId == "class.archivist"; }
        /// <summary>Silenced (her touch): it neither mends nor casts nor kicks.</summary>
        public bool Silenced { get { return Time.time < silencedUntil; } }
        public void Silence(float seconds) { silencedUntil = Mathf.Max(silencedUntil, Time.time + seconds); }
        /// <summary>Raised in a fight (a Shaman's Barrow-wake): it falls afresh next time.</summary>
        public void Raised() { downSince = -1; }
        /// <summary>A Shaman sim's one raising a fight (play note 272): its charge, the one it is raising and when it lands.</summary>
        public readonly RezCharge Wake = new RezCharge();
        Actor waking; float wakeAt;
        public int Wakes { get; private set; }
        /// <summary>A Shaman sim raises the most needed of the fallen (RaidJobs.RezPick), a 3 s cast, once a fight. True while it casts.</summary>
        bool BarrowWake()
        {
            if (sim.classId != "class.shaman") return false;
            if (!session.InCombat) { Wake.Reset(); waking = null; return false; }
            if (waking != null)
            {
                if (waking.IsAlive) { waking = null; if (visual != null) visual.Casting = false; return false; }
                if (Time.time < wakeAt) { Activity = "Calling " + waking.DisplayName + " back"; return true; }
                var who = waking; waking = null; if (visual != null) visual.Casting = false;
                if (Wake.Spend()) { session.RaiseFallen(who, actor, "Barrow-wake"); Wakes++; }
                return false;
            }
            if (!Wake.Ready || Silenced) return false;
            var pick = session.RezTarget(transform.position, 30); if (pick == null) return false;
            waking = pick; wakeAt = Time.time + 3; if (agent.isOnNavMesh) agent.isStopped = true; if (visual != null) visual.Casting = true;
            session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .65f ? "rezzing " + pick.DisplayName.Split(' ')[0].ToLower() : "Old ones, send " + pick.DisplayName.Split(' ')[0] + " back to us.");
            return true;
        }
        /// <summary>Let go early (spat out of her whole).</summary>
        public void ReleaseHold() { heldUntil = Mathf.Min(heldUntil, Time.time); }
        /// <summary>How many times it stepped out of marked ground, threw a harpoon, salt, kicked the Feeding (the log and the tests).</summary>
        public int StepsOut { get; private set; }
        public int HarpoonsThrown { get; private set; }
        public int SaltsThrown { get; private set; }
        public int FeedingKicks { get; private set; }
        /// <summary>It is stepping out of marked ground now.</summary>
        public bool SteppingOut { get { return steppingOut; } }
        float Roll() { return Random.value; }
        bool Misses() { return RaidJobs.Misses(sim.level, sim.bold, Roll(), UnravellerTuning.Current); }

        /// <summary>Out of marked ground (any mark the game draws): true while it walks clear. Looked at four times a second.</summary>
        bool StepOutOfGround()
        {
            if (session.HuntedActor == actor) return false;   // the hunted runs for the river instead (RaidTurn)
            if (Time.time < groundLookAt) return steppingOut;
            groundLookAt = Time.time + .25f;
            var feet = NavMesh.SamplePosition(transform.position, out var under, 2, NavMesh.AllAreas) ? under.position : transform.position - Vector3.up;
            var to = GroundMarks.StepOut(feet);
            if (!to.HasValue) { steppingOut = false; return false; }
            if (!steppingOut) StepsOut++;
            steppingOut = true;
            agent.isStopped = false; agent.speed = 5.2f; agent.SetDestination(to.Value);
            if (visual != null) visual.Casting = false;
            Activity = "Stepping out of the marked ground";
            return true;
        }

        /// <summary>Its part in her fight this frame: true when it took the frame (running for the river, closing to throw, throwing).</summary>
        bool RaidTurn()
        {
            var e = session.UnravellerFighting; if (e == null) return false;
            var t = UnravellerTuning.Current; float now = Time.time;
            float d = e.EdgeDistance(transform.position);   // throws and kicks reach her edge
            // The hunted: into the river (it hides magic from her, b1 ch.16:75), and out 3 s after she falls.
            var hunted = session.HuntedActor;
            if (hunted != huntSeen) { huntSeen = hunted; huntIgnored = hunted == actor && Misses(); }
            if (hunted == actor && !huntIgnored) riverUntil = now + session.HuntLandsIn + 3;
            if (now < riverUntil)
            {
                var water = session.NearestWater(transform.position, 25);
                if (water.HasValue)
                {
                    agent.isStopped = false; agent.speed = 5.6f; agent.SetDestination(water.Value);
                    if (visual != null) visual.Casting = false; healing = null;
                    Activity = "Into the river";
                    return true;
                }
            }
            // The Feeding: the interrupter on turn kicks; when it cannot (immune, its kick spent), the harpooner on turn throws into it.
            if (e.Feeding && e.CastProgress > .2f && !Silenced)
            {
                string kicker = session.InterruptTurn, harpooner = session.HarpoonTurn;
                var answer = RaidJobs.FeedingAnswer(true, kicker != null && InterruptReady(kicker, e), e.InterruptImmune, harpooner != null);
                if (answer == RaidJobs.Answer.Interrupt && kicker == sim.id)
                {
                    nextKick = now + 4;
                    if (e.Apply("silence", actor, 3) == null)
                    {
                        nextKick = now + 12; FeedingKicks++; session.InterruptTaken(sim.id);
                        session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .75f ? "kicked the feeding" : "The Feeding is stopped.");
                    }
                    return false;
                }
                if (answer == RaidJobs.Answer.Harpoon && harpooner == sim.id && d <= WeaveEaterRules.ThrowRange && Carries(Unraveller.HarpoonItem))
                { Throw(e, WeaveEaterRules.Harpoon); session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .75f ? "harpoon in the feeding" : "Bone holds: harpoon in her Feeding."); return true; }
            }
            // The harpoon rotation: its turn, her pin running out.
            var turn = session.HarpoonTurn;
            if (turn != harpoonTurnSeen) { harpoonTurnSeen = turn; harpoonLateUntil = turn == sim.id && Misses() ? now + 3 : -1; }
            if (turn == sim.id && !Silenced)
            {
                bool has = Carries(Unraveller.HarpoonItem);
                if (e.PinLeft <= t.harpoonLead + 3 && d > WeaveEaterRules.ThrowRange - 1 && has) { CloseTo(e, WeaveEaterRules.ThrowRange - 3); Activity = "Closing to harpoon her"; return true; }
                if (now >= harpoonLateUntil && RaidJobs.HarpoonNow(true, e.PinLeft, has, d <= WeaveEaterRules.ThrowRange, t.harpoonLead)) { Throw(e, WeaveEaterRules.Harpoon); return true; }
            }
            // The salts on the swallowed: those called, each once.
            // The brood held: the raid's control classes, each one at a time (raid 4).
            if (!Silenced && HoldBrood(now)) return true;
            if (session.SaltCall.Contains(sim.id) && now >= saltSkipUntil && !Silenced)
            {
                if (session.Swallow.Since != saltRolledFor) { saltRolledFor = session.Swallow.Since; saltMissed = Misses(); }   // once a swallowing
                if (saltMissed) { saltSkipUntil = now + 4; return false; }
                if (!Carries(Unraveller.SaltItem)) { saltSkipUntil = now + 4; return false; }
                if (d > WeaveEaterRules.ThrowRange - .5f) { CloseTo(e, WeaveEaterRules.ThrowRange - 3); Activity = "Running to salt her"; return true; }
                Throw(e, WeaveEaterRules.Salt);
                session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .75f ? "salted her" : "Salt on her!");
                return true;
            }
            // The brood: salt one that is not calcified, then the blow that shatters it.
            var q = Quarry;
            if (q != null && q.Brood && q.actor.IsAlive && !q.Calcified && now >= nextBroodSalt && Vector3.Distance(transform.position, q.transform.position) <= WeaveEaterRules.ThrowRange && Carries(Unraveller.SaltItem))
            {
                nextBroodSalt = now + 8;
                if (!Misses()) { Throw(q, WeaveEaterRules.Salt); return true; }
            }
            return false;
        }
        /// <summary>The interrupter <paramref name="id"/> can kick her now: alive, free, its kick ready, within its reach of her.</summary>
        float nextBroodHold;
        /// <summary>Brood it has held (the log and the tests).</summary>
        public int BroodHeld { get; private set; }
        /// <summary>A control class's hold on one of her brood (raid 4, "need CC baby"): the nearest brood that is free (not held, not
        /// salted), within its reach, put out of the fight for broodHoldSeconds, marked Moon (or Cross) so the raid sees it; its wait
        /// broodHoldEvery. True when it held one (its frame).</summary>
        bool HoldBrood(float now)
        {
            if (now < nextBroodHold || RaidJobs.BroodHolder(sim.classId, Role) < 0 || session.BroodNow.Count == 0) return false;
            var t = UnravellerTuning.Current;
            EncounterEnemy pick = null; float best = 25;
            foreach (var b in session.BroodNow)
            {
                if (b == null || !b.actor.IsAlive || b.Controlled || b.Calcified || b == session.BroodKill || !b.Takes("incap")) continue;   // never the Skull: it is being killed
                float d = Vector3.Distance(b.transform.position, transform.position); if (d < best) { best = d; pick = b; }
            }
            if (pick == null) return false;
            if (Misses()) { nextBroodHold = now + 3; return false; }
            if (pick.Apply("incap", actor, t.broodHoldSeconds) != null) { nextBroodHold = now + 3; return false; }
            nextBroodHold = now + t.broodHoldEvery; BroodHeld++;
            bool moonTaken = session.BroodNow.Exists(b => b != null && b != pick && b.actor.IsAlive && b.RaidMarked == RaidMark.Moon);
            if (pick.RaidMarked == RaidMark.None) pick.RaidMarked = moonTaken ? RaidMark.Cross : RaidMark.Moon;
            var move = ClassMove; string name = move != null ? move.name : sim.classId == "class.rogue" ? "Gouge" : sim.classId == "class.scoundrel" ? "Lullaby" : "Lull";
            if (move != null) ShowHold(move, pick); else session.FloatText(pick.transform.position + Vector3.up * 1.8f, name, ControlRings.Held);
            if (BroodHeld == 1) session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .65f ? "holding a spider" : "I'll hold one of the brood.");
            return true;
        }
        bool InterruptReady(string id, EncounterEnemy e)
        {
            var c = session.PartySim(id);
            return c != null && c.actor.IsAlive && c.Interrupter && Time.time >= c.nextKick && !c.Silenced && !c.HeldNow && e.EdgeDistance(c.transform.position) <= c.Reach + 1.5f;
        }
        /// <summary>Carries one of <paramref name="item"/>, or takes three from the apron's stock (Tibb's barrel, Ketta's rack) when it has none and they are out.</summary>
        bool Carries(string item)
        {
            if (SimEconomy.Count(sim, item) > 0) return true;
            if (!session.ApronStock) return false;
            // Three at the market's price, on credit when its purse is short (Tibb and Ketta let nobody go without tonight).
            int each = Mathf.Max(1, session.Items?.Get(item)?.value ?? 1);
            sim.coin = Mathf.Max(0, sim.coin - 3 * each); SimEconomy.Add(sim, item, 3);
            session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .75f ? "out of " + (item == Unraveller.HarpoonItem ? "harpoons" : "salt") + ", grabbing more" : "I'm out. Taking more from " + (item == Unraveller.HarpoonItem ? "Ketta's rack." : "Tibb's barrel."));
            return true;
        }
        /// <summary>One of its salt or harpoons thrown at <paramref name="e"/>: from its bags, the same rules as yours (EncounterSession.ThrowAt).</summary>
        void Throw(EncounterEnemy e, string kind)
        {
            string item = kind == WeaveEaterRules.Harpoon ? Unraveller.HarpoonItem : Unraveller.SaltItem;
            if (!Carries(item)) return;
            SimEconomy.Add(sim, item, -1);
            visual?.Strike("throw");
            Bolt.Fire(Hand, e.transform, kind == WeaveEaterRules.Harpoon ? new Color(.86f, .8f, .62f) : new Color(.95f, .95f, .9f), kind == WeaveEaterRules.Harpoon ? .14f : .22f, .3f, kind == WeaveEaterRules.Harpoon);
            session.ThrowAt(e, kind, actor, sim.level);
            if (kind == WeaveEaterRules.Harpoon) { HarpoonsThrown++; if (e.RaidBoss) session.HarpoonTaken(sim.id); }
            else SaltsThrown++;
            session.FloatText(transform.position + Vector3.up * 2.2f, kind == WeaveEaterRules.Harpoon ? "Bone harpoon" : "Salt", kind == WeaveEaterRules.Harpoon ? new Color(.86f, .8f, .62f) : new Color(.95f, .95f, .9f));
        }
        void CloseTo(EncounterEnemy e, float within)
        {
            var to = e.transform.position + (transform.position - e.transform.position).normalized * within;
            if (NavMesh.SamplePosition(to, out var hit, 3, NavMesh.AllAreas)) { agent.isStopped = false; agent.speed = 5.2f; agent.SetDestination(hit.position); }
        }

        /// <summary>Its quarry in her fight, when its part says (3.4): the tank holding her keeps her; the tank not holding her and Party 2's
        /// damage-dealers take the brood. Null: as anywhere else.</summary>
        EncounterEnemy RaidQuarry()
        {
            var e = session.UnravellerFighting; if (e == null) return null;
            bool holds = e.Victim == actor;
            if (Role == PartyRole.Tank && (holds || TauntsHer(e) && !(session.BroodKill != null && session.BroodKill.actor.IsAlive && e.Victim != null && session.IsRaidTank(e.Victim)))) return e;   // holding her, or the one to take her back (a swap, a blink onto a caster); while a wave is up and a tank has her, the off-tank stays on the brood (raid 6)
            int party = session.InRaid ? session.Raid.PartyOf(sim.id) : 0;
            if (!RaidJobs.TakesBrood(Role, party, holds, Melee)) return null;
            // The kill order's Skull (raid 5): only it; a blow on another held one would break it. Chris: "if all is cc'd then the tank still needs
            // to grab ONE and the dps assist him and not break other cc": the off-tank grabs it (its blow breaks the hold, its taunt takes it);
            // the damage-dealers assist the off-tank, and leave a held Skull alone until the off-tank is on it (meanwhile, her).
            var skull = session.BroodKill;
            if (skull != null && skull.actor.IsAlive && Vector3.Distance(skull.transform.position, transform.position) < 40)
            {
                if (Role == PartyRole.Tank) return skull;
                var ot = session.OffTank;
                // Wait for the tank only while it is on its way to the Skull (raid 6: the off-tank went back to her, and the damage-dealers waited
                // on her for good: six brood killed of thirty).
                if (skull.Incapacitated && ot != null && ot != this && ot.actor.IsAlive && ot.Quarry == skull && Vector3.Distance(ot.transform.position, skull.transform.position) > 6) return e;
                return skull;
            }
            EncounterEnemy best = null; float bestD = 40;
            foreach (var b in session.BroodNow)
            {
                if (b == null || !b.actor.IsAlive || b.LeaveAlone) continue;
                float dist = Vector3.Distance(b.transform.position, transform.position); if (dist < bestD) { best = b; bestD = dist; }
            }
            return best;
        }
        /// <summary>No area blow near a held brood while a wave is up (raid 5: it would break the holds).</summary>
        public bool HeldBroodNear(Vector3 at, float reach)
        {
            foreach (var b in session.BroodNow) if (b != null && b.actor.IsAlive && b.Incapacitated && b != session.BroodKill && Vector3.Distance(b.transform.position, at) <= reach) return true;
            return false;
        }
        /// <summary>Its turn at the raid's buff-up before a pull (raid 6, Chris: "are they buffed up? with raid food buffs? elixir? group
        /// buffs?"): as at a dungeon boss's stop (BuffUpNow): food (a feast for its party if one near carries it, else its dish), the elixir
        /// for its part, a draught if low, its class's standing buff on all ten. What it used.</summary>
        public string BuffForRaid() { BuffUpNow(); return BuffNote; }
        /// <summary>Whether this tank taunts her now (the swap at four stacks; RaidJobs.TauntHer).</summary>
        bool TauntsHer(EncounterEnemy e) { return session.TauntsUnraveller(actor, e); }
    }
}
