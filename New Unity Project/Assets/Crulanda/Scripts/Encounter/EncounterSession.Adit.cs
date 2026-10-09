using System.Collections.Generic;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Sealed Adit's dungeon objects (Docs/DUNGEON_DESIGN.md section 9, step D3; Chris, 2026-10-09: "gate on the stair", "keep them"):
    /// - the rail sigils its three branch bosses carry (ZoneCamp.key), taken from the body at the first kill and kept for good
    ///   (Progress.keys);
    /// - the cage-lift gate at the head of the stair: the three sigils set in its frame raise it, once and for good ("adit.lift");
    /// - the gate onto the Rail Hall's platform, shut again on every visit: a goblin freed in "The Pressed" hums its lock open
    ///   (the quiet way), or blasting powder from the keg up the stair blows it (the loud way: the Quartermaster's people come);
    /// - the Gallery's spirit stone: touched once, it is where you wake in the Adit (Progress.spiritStones, RecoveryPoint);
    /// - rare camps (ZoneCamp.rare: the Quiet Miner, one visit in five), rolled with <see cref="RareDice"/>.
    /// </summary>
    public sealed partial class EncounterSession
    {
        public const string LiftWoken = "adit.lift", PressedQuest = "side.adit.pressed";
        /// <summary>A rare camp is there when this comes under its chance (0..1). Tests set it.</summary>
        public static System.Func<float> RareDice = () => Random.value;
        /// <summary>A keg of blasting powder in hand (taken up the stair; spent on the platform gate). Not saved: the gate shuts again.</summary>
        public bool CarryingPowder { get; private set; }

        /// <summary>On arrival: a cage-lift woken on an earlier visit stands open, its sigils lit.</summary>
        void StartAdit()
        {
            if (Zone == null) return;
            foreach (var g in Zone.Gates) if (g != null) g.Recarve();   // the navmesh is built after the gates: cut them into it now
            foreach (var i in Zone.Interactables) if (i.kind == "liftgate" && i.gate != null && Progress.keys.Contains(LiftWoken)) i.gate.SetOpen(true, true);
            StartWeaver();   // the Weaver's unlocking (EncounterSession.Weaver.cs)
        }
        /// <summary>What E says at a usable prop (the HUD's prompt; the Adit's things say how things stand).</summary>
        public string InteractPromptFor(ZoneInteractable i) { return i == null ? null : AditPrompt(i) ?? i.prompt; }
        ZoneCamp CampOf(EncounterEnemy e) { var camps = Zone != null ? Zone.Zone.camps : null; return e != null && camps != null && e.CampIndex >= 0 && e.CampIndex < camps.Length ? camps[e.CampIndex] : null; }
        /// <summary>The zone's keyed camps (the sigil bosses), and how many of their keys you hold.</summary>
        List<ZoneCamp> KeyCamps() { var list = new List<ZoneCamp>(); if (Zone != null && Zone.Zone.camps != null) foreach (var c in Zone.Zone.camps) if (c != null && !string.IsNullOrEmpty(c.key)) list.Add(c); return list; }
        int KeysHeld(List<ZoneCamp> keyed) { int n = 0; foreach (var c in keyed) if (Progress.keys.Contains(c.key)) n++; return n; }

        /// <summary>An elite carrying a key, dead by your hand or your party's: the key is yours, once.</summary>
        void TakeKey(EncounterEnemy e)
        {
            var camp = CampOf(e); if (camp == null || string.IsNullOrEmpty(camp.key) || !e.Elite || Progress.keys.Contains(camp.key)) return;
            Progress.keys.Add(camp.key);
            var keyed = KeyCamps(); int have = KeysHeld(keyed);
            FloatText(e.transform.position + Vector3.up * 1.2f, char.ToUpperInvariant(camp.keyName[0]) + camp.keyName.Substring(1), new Color(1, .82f, .35f));
            Message("You take " + camp.keyName + " from " + camp.mob + ". Rail sigils " + have + "/" + keyed.Count +
                (have < keyed.Count ? ": the others are down the other ways off the Gallery." : ": set them in the frame by the cage-lift, at the head of the stair."));
        }

        /// <summary>What E says at one of the Adit's things, or null for the prop's own prompt.</summary>
        string AditPrompt(ZoneInteractable i)
        {
            switch (i.kind)
            {
                case "liftgate": { var keyed = KeyCamps(); return "Set the rail sigils (" + KeysHeld(keyed) + "/" + keyed.Count + ")"; }
                case "platformgate": return Quests != null && Quests.IsDone(PressedQuest) ? "Call the goblins to the gate (quiet)" : CarryingPowder ? "Blow the gate (loud)" : "Try the gate";
                case "powder": return CarryingPowder ? "Powder on your shoulder" : null;
                case "stone": return Progress.spiritStones.Contains(i.Key(Zone.Zone.id)) ? "The spirit stone hums" : i.prompt;
                case "weaver": return Weaver != null && Weaver.Now == AditWeaver.Stage.Waiting ? "Talk to " + AditWeaver.Name + " (walk her to the carriage)" : i.prompt;
                default: return null;
            }
        }
        /// <summary>Uses one of the Adit's things; false when it is not one of them.</summary>
        bool UseAditThing(ZoneInteractable i)
        {
            switch (i.kind)
            {
                case "liftgate": UseLiftGate(i); return true;
                case "platformgate": UsePlatformGate(i); return true;
                case "powder":
                    if (CarryingPowder) Message("You have a keg on your shoulder already.");
                    else { CarryingPowder = true; Message("You heave a keg of blasting powder onto your shoulder. Mind the lamps."); }
                    return true;
                case "stone": TouchStone(i); return true;
                case "weaver": TalkToWeaver(); return true;
                default: return false;
            }
        }
        void UseLiftGate(ZoneInteractable i)
        {
            var g = i.gate; if (g == null || g.Open) return;
            var keyed = KeyCamps(); int have = KeysHeld(keyed);
            if (have < keyed.Count)
            {
                Message("Three sockets in the frame, worn smooth: amber, red and grey. " + (have == 0
                    ? "The rail sigils that fit them are carried by the bosses down the three ways off the Gallery."
                    : "You have " + have + " of the " + keyed.Count + " rail sigils; the rest are down the other ways off the Gallery."));
                return;
            }
            if (!Progress.keys.Contains(LiftWoken)) Progress.keys.Add(LiftWoken);
            g.SetOpen(true);
            Message("You set the three sigils in the frame. Deep in the shaft something wakes; the chains take up, and the cage-gate climbs into its frame.");
            Save(false);
        }
        void UsePlatformGate(ZoneInteractable i)
        {
            var g = i.gate; if (g == null || g.Open) return;
            if (Quests != null && Quests.IsDone(PressedQuest))
            {
                g.SetOpen(true);
                Message("A goblin you freed from the cages slips out of the dark, lays a hand on the lock and hums. The bolts let go without a sound. Nobody in the hall has heard." +
                    (EscortPossible ? " She stays by the gate: the old one from the fifth cage, Mother Quillet, and she has business with that carriage." : ""));
                SpawnWeaver(g);   // the escort (EncounterSession.Weaver.cs)
                return;
            }
            if (!CarryingPowder)
            {
                Message("Barred from the hall's side, and the lock is goblin work. Blasting powder would do it (there was a keg up the stair), or a goblin who knows the lock.");
                return;
            }
            CarryingPowder = false; g.SetOpen(true);
            FloatText(g.transform.position + Vector3.up * 1.5f, "BOOM", new Color(1, .6f, .25f));
            Message("You pack the powder against the bars and light it. The gate goes up in a roar the whole hall hears.");
            SendWave(g);
        }
        /// <summary>
        /// The loud way in: the gate's lord (the Quartermaster) sends his guards and the people nearest the gate (camps whose ground is
        /// within 26 m of it, not the elites) at you, a beat apart. Returns how many come.
        /// </summary>
        public int SendWave(ZoneGate g)
        {
            if (g == null || Player == null) return 0;
            EncounterEnemy lord = null;
            foreach (var e in Enemies) if (e != null && e.Camp && e.Elite && e.actor.IsAlive && CampOf(e)?.mob == g.Lord) { lord = e; break; }
            var at = new Vector2(g.transform.position.x, g.transform.position.z); var coming = new List<EncounterEnemy>();
            foreach (var e in Enemies)
            {
                if (e == null || !e.Camp || e.Elite || !e.actor.IsAlive || !e.CanAnswer) continue;
                if (!(lord != null && GuardOf(e, lord)) && Vector2.Distance(e.CampCenter, at) > 26) continue;
                coming.Add(e);
            }
            if (coming.Count == 0) return 0;
            var caller = lord != null ? lord : coming[0];
            if (caller == coming[0]) caller.threat.Add(Player.EntityId.Value, JoinThreat);   // nobody to call it: it comes of itself
            for (int k = 0; k < coming.Count; k++) if (coming[k] != caller) coming[k].Answer(caller, Player, SocialAggro.CallBeat + SocialAggro.CallStagger * k);
            Message(lord != null ? lord.Name + " bellows from the platform: \"Powder at the gate! Platform, on them!\"" : "The hall roars and comes for you.");
            return coming.Count;
        }
        void TouchStone(ZoneInteractable i)
        {
            var key = i.Key(Zone.Zone.id);
            if (Progress.spiritStones.Contains(key)) { Message("The stone hums under your hand. If you fall in here, you will wake beside it."); return; }
            Progress.spiritStones.Add(key);
            Message("You lay a hand on the spirit stone and it hums with the Gallery's song. If you fall in " + ZoneTitle + ", you will wake here.");
            Save(false);
        }
        /// <summary>The spirit stone you touched in this zone, or null: where you wake after a fall here.</summary>
        ZoneInteractable BoundStone()
        {
            if (Zone == null) return null;
            foreach (var i in Zone.Interactables) if (i.kind == "stone" && Progress.spiritStones.Contains(i.Key(Zone.Zone.id))) return i;
            return null;
        }
        /// <summary>Whether a camp stands past a shut gate in its cave (the run a sim leads stops there).</summary>
        public ZoneGate GateBefore(ZoneCamp camp)
        {
            if (Zone == null || camp == null || string.IsNullOrEmpty(camp.cave)) return null;
            foreach (var g in Zone.Gates) if (g != null && !g.Open && g.Cave == camp.cave && camp.along > g.Along) return g;
            return null;
        }
    }
}
