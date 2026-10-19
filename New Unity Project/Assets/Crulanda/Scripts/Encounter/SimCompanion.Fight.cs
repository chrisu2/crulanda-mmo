using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A party sim's hold and its area blow (Chris, 2026-10-15, watching the group runs: "also most mobs dont seem to be cc or aoe still a
    /// thing?" and "i do see a cc / i assume by rogue"). GAME-ONLY. Until now only a Rogue, an Archivist or a Scoundrel held anything (the
    /// moon, HoldTheMoon, unchanged). Now every other class has one hold of its own (CC_DESIGN section 2 where it names one: Ash Hex, Sleep
    /// of the Wood, Snare Trap, Rebuke; Shackle Cast from the Warrior's kit, Binding Salt from the Salt-Mender's; Lantern Glare and Barrow
    /// Dread new), used in the moon's spirit:
    /// - only with two or more mobs on the party; never on a boss, the mob on the tank, the tank's quarry (a long hold), your target or the
    ///   skull; on a mob nobody holds and that will take it (EncounterEnemy.Takes: a boss's immunity, the diminishing returns);
    /// - each class on its own wait of 20-28 s, said in Party the first time in a fight, its name over the mob, its look from the kits'
    ///   effects (a bolt of its colour, a burst of its element, the blow for a melee one);
    /// - a long hold (a hex, a sleep, a trap, a glare, salt, a fear) keeps two of a pack of three or more in front of the party (the tank's
    ///   and one for the area blows): the party holds the rest. A stun (Rebuke, Shackle Cast) is short, does not break and takes nobody out
    ///   of the fight for long, so it is not counted; the Warrior's chain goes on an add hitting someone else, the healer first.
    /// The area blow: a damage-dealer (and a tank, its swing a light cleave) every 10-15 s, with three or more mobs on the party within
    /// <see cref="AreaReach"/> of its quarry, strikes its quarry and each of them not held or marked to be left alone for
    /// <see cref="AreaShare"/> of its blow (a tank's cleave <see cref="CleaveShare"/>, with threat on each). A held mob is never struck.
    /// Against one or two mobs nothing changes, so the role balance on a single target (a tank about 70% of a damage-dealer) holds.
    /// </summary>
    public sealed partial class SimCompanion
    {
        /// <summary>A class's hold: its name, the kind EncounterEnemy.Apply takes ("incap", "stun", "fear"), seconds, its wait, its reach, its element (SpellFx).</summary>
        public sealed class HoldMove
        {
            public readonly string name, kind, element; public readonly float seconds, cooldown, reach;
            public HoldMove(string name, string kind, float seconds, float cooldown, float reach, string element)
            { this.name = name; this.kind = kind; this.seconds = seconds; this.cooldown = cooldown; this.reach = reach; this.element = element; }
            /// <summary>Takes a mob out of the fight (and breaks on a blow, or three for a fear): counted against the two kept in front.</summary>
            public bool Long { get { return kind == "incap" || kind == "fear"; } }
        }
        static readonly Dictionary<string, HoldMove> holds = new Dictionary<string, HoldMove>
        {
            { "class.mage", new HoldMove("Ash Hex", "incap", 15, 25, 25, "fire") },            // a person or beast turned to smouldering ash
            { "class.druid", new HoldMove("Sleep of the Wood", "incap", 15, 28, 25, "nature") },
            { "class.ranger", new HoldMove("Snare Trap", "incap", 12, 25, 25, "stone") },      // thrown at its feet, the jaws shut
            { "class.cleric", new HoldMove("Lantern Glare", "incap", 10, 25, 25, "holy") },    // the shrine lantern's flare: dazzled, it stands (a disorient)
            { "class.saltmender", new HoldMove("Binding Salt", "incap", 10, 25, 25, "salt") }, // held in white crystal
            { "class.shaman", new HoldMove("Barrow Dread", "fear", 4, 22, 20, "spirit") },     // the old dead's chill: it runs, a short way (not into the next camp)
            { "class.paladin", new HoldMove("Rebuke", "stun", 3, 20, 8, "holy") },
            { "class.warrior", new HoldMove("Shackle Cast", "stun", 3, 20, 20, "steel") },     // the chain hurled at an add on someone else
        };
        /// <summary>A class's hold, or null (the Rogue, the Archivist and the Scoundrel hold the moon instead: HoldTheMoon).</summary>
        public static HoldMove HoldFor(string classId) { return classId != null && holds.TryGetValue(classId, out var m) ? m : null; }
        public HoldMove ClassMove { get { return sim != null ? HoldFor(sim.classId) : null; } }
        /// <summary>The area blow's reach about the quarry, and its share of a blow: a damage-dealer's, a tank's light cleave.</summary>
        public const float AreaReach = 6, AreaShare = .7f, CleaveShare = .6f, AreaEveryMin = 10, AreaEveryMax = 15;
        float nextClassHold, nextHoldLook, nextArea; bool spokeHold;
        /// <summary>Its class's holds that took, and its area blows (the tests; the run's log).</summary>
        public int Holds { get; private set; }
        public int AreaSwings { get; private set; }
        /// <summary>The mob its class's hold last took.</summary>
        public EncounterEnemy LastHeld { get; private set; }

        /// <summary>Its class's hold, looked at twice a second (Update): the mob it fits best, held, shown and said.</summary>
        void HoldOne(EncounterEnemy quarry)
        {
            nextHoldLook = Time.time + .5f;
            if (!session.InCombat) { spokeHold = false; return; }   // a new fight: it says its first hold again
            var move = ClassMove; if (move == null || Time.time < nextClassHold) return;
            var pick = HoldPick(move, quarry); if (pick == null) return;
            if (pick.Apply(move.kind, actor, move.seconds) != null) { nextClassHold = Time.time + 6; return; }   // it did not take after all: a while before the next try
            nextClassHold = Time.time + move.cooldown; Holds++; LastHeld = pick;
            ShowHold(move, pick);
            if (!spokeHold) { spokeHold = true; session.ChatSay(ChatChannel.Party, sim.name, HoldLine(pick)); }
        }
        /// <summary>The mob its hold fits best now, or null: none while fewer than two mobs are on the party, and a long hold none while only
        /// two of a pack of three or more stand free. A mob marked for holding (cross, or the moon with no Rogue or Archivist along) first, then
        /// a caster, then one on the healer, then one on anyone but the tank; the nearer the better.</summary>
        EncounterEnemy HoldPick(HoldMove move, EncounterEnemy quarry)
        {
            int fighting = 0, free = 0;
            foreach (var e in session.Enemies) { if (e == null || !e.actor.IsAlive || !e.FightingParty) continue; fighting++; if (!Held(e)) free++; }
            if (fighting < 2) return null;
            if (move.Long && (free < 2 || fighting > 2 && free <= 2)) return null;
            EncounterEnemy best = null; float bestScore = float.MinValue;
            foreach (var e in session.Enemies)
            {
                if (!MayHold(e, move, quarry)) continue;
                float score = -.1f * Vector3.Distance(e.transform.position, transform.position);
                if (e.RaidMarked == RaidMark.Cross || e.RaidMarked == RaidMark.Moon) score += 50;
                if (e.Cast != null) score += 10;
                var v = e.Victim; if (v != null && IsHealer(v)) score += 20; else if (v != null && v != actor) score += 5;
                if (score > bestScore) { bestScore = score; best = e; }
            }
            return best;
        }
        /// <summary>Whether its class's hold may go on <paramref name="e"/> now (the tests: never a boss).</summary>
        public bool MayHold(EncounterEnemy e) { var move = ClassMove; return move != null && MayHold(e, move, Quarry); }
        bool MayHold(EncounterEnemy e, HoldMove move, EncounterEnemy quarry)
        {
            if (e == null || !e.actor.IsAlive || !e.FightingParty || e.Game || e.Hidden || e.Inside != null) return false;
            if (e.Boss || e.Controlled || !e.Takes(move.kind)) return false;   // never a boss; never one held, stunned or running already; nor one immune now
            if (e.RaidMarked == RaidMark.Skull || e == session.Target) return false;   // the kill, and your target, are fought
            if (e.RaidMarked == RaidMark.Moon && session.PartySims.Exists(c => c != null && c != this && c.Controller)) return false;   // the moon is the Rogue's (or the Archivist's)
            if (Vector3.Distance(e.transform.position, transform.position) > move.reach) return false;
            var v = e.Victim;
            if (v != null && IsTank(v)) return false;   // the mob on the tank is the tank's
            if (move.Long)
            {
                if (e == quarry) return false;   // its own next blow would break it
                foreach (var p in session.PartySims)
                    if (p != null && p != this && p.actor != null && p.actor.IsAlive && (p.unwriting == e || p.Role == PartyRole.Tank && p.Quarry == e)) return false;   // the tank's quarry, and one being unwritten (each tick a blow)
            }
            else if (sim.classId == "class.warrior" && (v == null || v == actor)) return false;   // the chain: an add on someone else
            return true;
        }
        bool IsTank(Actor a) { var c = a != null ? a.GetComponent<SimCompanion>() : null; return c != null && c.Role == PartyRole.Tank; }
        bool IsHealer(Actor a)
        {
            if (a == null) return false;
            if (session.Companion != null && a == session.Companion.actor) return true;   // Mira
            var c = a.GetComponent<SimCompanion>(); return c != null && c.Role == PartyRole.Healer;
        }
        /// <summary>The hold seen: a melee one's blow (Rebuke) or a thrown or cast one's bolt of its colour, a burst of its element on the mob,
        /// and its name over the mob in the colour its ring takes (ControlRings).</summary>
        void ShowHold(HoldMove move, EncounterEnemy e)
        {
            var ring = move.kind == "stun" ? ControlRings.Stun : move.kind == "fear" ? ControlRings.Fear : ControlRings.Held;
            if (Melee && move.reach <= 8) { visual?.Strike("bash"); SpellFx.Burst(move.element, e.transform); }
            else
            {
                if (Melee) visual?.Strike("throw"); else visual?.CastRelease();
                Bolt.Fire(Hand, e.transform, ElementColour(move.element), .16f, .3f, false);   // bursts in its colour where it lands
            }
            session.FloatText(e.transform.position + Vector3.up * 1.8f, move.name, ring);
        }
        static Color ElementColour(string element)
        {
            switch (element)
            {
                case "fire": return new Color(1, .55f, .15f);
                case "nature": return new Color(.45f, .85f, .3f);
                case "holy": return new Color(1, .9f, .55f);
                case "salt": return new Color(.97f, .97f, .92f);
                case "spirit": return new Color(.78f, .84f, 1);
                case "stone": return new Color(.55f, .5f, .42f);
                default: return new Color(.55f, .58f, .62f);   // steel: the chain
            }
        }
        /// <summary>What it says in Party with its first hold of a fight.</summary>
        string HoldLine(EncounterEnemy e)
        {
            bool chatty = sim.chatty > .65f; string n = e.actor.DisplayName, low = n.ToLower();
            var v = e.Victim; string on = v == null ? "us" : v == session.Player ? "you" : v.DisplayName;
            switch (sim.classId)
            {
                case "class.mage": return chatty ? "hexed the " + low + ", leave it" : "Ash Hex on the " + n + ". Leave it be.";
                case "class.druid": return chatty ? "sleeping the " + low + ", dont wake it" : "The " + n + " sleeps. Don't wake it.";
                case "class.ranger": return chatty ? "trapped the " + low + ", leave it" : "Trap down on the " + n + ". Leave it.";
                case "class.cleric": return chatty ? "dazzled the " + low + ", let it be" : "The " + n + " is dazzled by the lantern. Let it be.";
                case "class.saltmender": return chatty ? "salted the " + low + ", dont hit it" : "I bind the " + n + " in salt. Leave it be.";
                case "class.shaman": return chatty ? "sent the " + low + " running" : "The old dead send the " + n + " running.";
                case "class.paladin": return chatty ? "stunned the " + low : "Rebuked the " + n + ". It reels.";
                case "class.warrior": return chatty ? "chained the one on " + on : "Chained the " + n + " off " + on + ".";
                default: return ClassMove.name + " on the " + n + ".";
            }
        }

        /// <summary>The mobs its area blow would strike about <paramref name="quarry"/> (it first), or null: a healer has none; it needs three or
        /// more mobs on the party within <see cref="AreaReach"/> of its quarry (held ones count) and two or more of them to strike. A mob held,
        /// running or marked to be left alone is never struck; hidden ones, riders and game neither.</summary>
        public List<EncounterEnemy> WideTargets(EncounterEnemy quarry)
        {
            if (quarry == null || Role == PartyRole.Healer || !quarry.actor.IsAlive || !quarry.FightingParty || Held(quarry) || quarry.LeaveAlone) return null;
            if (quarry.RaidBoss) return null;   // a world boss is a single target: no area blow off her onto her brood (run 1: the Archivist topped the meter on it)
            int near = 0; var hit = new List<EncounterEnemy> { quarry };
            foreach (var e in session.Enemies)
            {
                if (e == null || !e.actor.IsAlive || !e.FightingParty || e.Game || e.Hidden || e.Inside != null) continue;
                if (Vector3.Distance(e.transform.position, quarry.transform.position) > AreaReach) continue;
                near++;
                if (e != quarry && !Held(e) && !e.LeaveAlone && !e.RaidBoss) hit.Add(e);   // nor onto her from her brood
            }
            return near >= 3 && hit.Count >= 2 ? hit : null;
        }
        /// <summary>Its area blow on the pack about <paramref name="quarry"/>, if there is one (its wait aside: Update keeps that): each struck
        /// for its share of a blow, a slash on each (melee) or a bolt or arrow to each (ranged), its name over it. How many it struck (0: none).</summary>
        public int SwingWide(EncounterEnemy quarry)
        {
            var foes = WideTargets(quarry); if (foes == null) return 0;
            nextArea = Time.time + Random.Range(AreaEveryMin, AreaEveryMax); AreaSwings++;
            bool tank = Role == PartyRole.Tank;
            int each = Mathf.Max(1, Mathf.RoundToInt(Hit * (tank ? CleaveShare : AreaShare)));
            if (Melee) visual?.Strike("heavy"); else visual?.CastRelease();
            var from = Hand; bool arrow = Arrow; var colour = BoltColour; string element = sim.classId == "class.paladin" ? "holy" : sim.classId == "class.scoundrel" ? "charm" : "steel";
            int n = 0;
            foreach (var e in foes)
            {
                if (e == null || !e.actor.IsAlive) continue;
                if (Melee) SpellFx.Slash(transform, e.transform, false, 1, element);
                else Bolt.Fire(from, e.transform, colour, arrow ? .1f : .18f, arrow ? .22f : .28f, arrow);
                e.Receive(each, actor); n++;
                if (tank) e.threat.Add(actor.EntityId.Value, BaseHit * (e == quarry ? 3f : 1.5f));   // the tank's cleave holds the pack
            }
            session.FloatText(transform.position + Vector3.up * 2.2f, AreaName(sim.classId), colour);
            return n;
        }
        /// <summary>The area blow's name by class (GAME-ONLY).</summary>
        public static string AreaName(string classId)
        {
            switch (classId)
            {
                case "class.warrior": return "Wide Swing";
                case "class.paladin": return "Wide Judgement";
                case "class.rogue": return "Shard Spray";
                case "class.scoundrel": return "Knife Round";
                case "class.ranger": return "Spread Loose";
                case "class.mage": return "Spreading Flame";
                case "class.archivist": return "Echo Burst";
                case "class.saltmender": return "Salt Spray";
                default: return "Wide Blow";
            }
        }
        /// <summary>Its bolt's colour (a Mage's fire, a Druid's thorns, an Archivist's song, a Salt-Mender's salt; arrows and the rest pale).</summary>
        Color BoltColour
        {
            get
            {
                switch (sim.classId)
                {
                    case "class.mage": return new Color(1, .55f, .15f);
                    case "class.druid": return new Color(.45f, .85f, .3f);
                    case "class.archivist": return new Color(.6f, .8f, 1);
                    case "class.saltmender": return new Color(.97f, .97f, .92f);
                    default: return new Color(.9f, .88f, .8f);
                }
            }
        }
        bool Arrow { get { return sim.classId == "class.ranger"; } }
        /// <summary>Where its bolts leave from: its right hand, or its chest.</summary>
        Vector3 Hand { get { var hand = visual != null ? visual.RightHandle : null; return hand != null ? hand.position : transform.position + Vector3.up * 1.3f; } }
    }
}
