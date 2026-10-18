using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Sealed Adit's dungeon objects (Docs/DUNGEON_DESIGN.md section 9, step D3; Chris, 2026-10-09: "gate on the stair", "keep them"):
    /// - the rail sigils its three branch bosses carry (ZoneCamp.key), taken from the body at the first kill and kept for good
    ///   (Progress.keys);
    /// - the singing door at the Gallery's north neck, the head of the shaft stair (the Adit rework; it was the winch gate): the three sigils
    ///   set in its sockets wake it, once and for good ("adit.lift");
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
            StartEvents();   // (the Adit rework) the Wake, started where its anchor is reached once its wing is open (EncounterSession.Release.cs)
            StartGreySeep();   // (S7) the Grey Spreads' lines across the Breach (EncounterSession.GreySpread.cs)
            StartSwingLoads();   // (S7) the Foundry's swing-loads and their rope teams (AditSwingLoad)
            StartHaul();     // (S6) the barge aground in the Black Cove and the rope teams on her warps (EncounterSession.Haul.cs)
            StartHallows();  // (Hallow's Creek) the paused townsfolk, the wagon's mules, the Warden (EncounterSession.Hallows.cs)
            // (the Lowtowns) the Rusty Spoon's Triad table, its window and its brawl (EncounterSession.Spoon.cs)
            StartSpoon();
            // The pressed goblins in their cages, and the doors of those opened before standing open (playtest note 125).
            foreach (var i in Zone.Interactables)
            {
                if (i.kind != "cage" || i.root == null) continue;
                if (Progress.usedInteractables.Contains(i.Key(Zone.Zone.id))) { OpenCageDoor(i, false); continue; }
                caged[i] = PressedGoblin(i.root, caged.Count);
            }
            // The carriage's team: two pit mules standing in their traces up the line from it (Chris, 2026-10-11, no machines).
            if (Zone.RailTeam != null)
                for (int k = 0; k < Zone.RailTeam.Length; k++)
                {
                    var mule = new GameObject("Pit mule").transform; mule.SetPositionAndRotation(Zone.RailTeam[k], Zone.RailTeamFacing);
                    mule.gameObject.AddComponent<PitMule>().body = CritterBody.Build(mule, "donkey", .2f + k * .45f, 11 + k * 7);
                    var box = mule.gameObject.AddComponent<BoxCollider>(); box.size = new Vector3(.55f, 1.15f, 1.6f); box.center = new Vector3(0, .575f, 0); mule.gameObject.layer = 2;   // solid, never a click target (as the farm's)
                }
        }
        readonly Dictionary<ZoneInteractable, GameObject> caged = new Dictionary<ZoneInteractable, GameObject>();
        /// <summary>You, your party sims and Mira (when she is with you), the living and the dead (the bucket and the grey lines ask).</summary>
        public List<Actor> PartyActors()
        {
            partyActors.Clear();
            if (Player != null) partyActors.Add(Player);
            foreach (var c in PartySims) if (c != null && c.actor != null) partyActors.Add(c.actor);
            if (Companion != null && Progress != null && Progress.recruited && Companion.actor != null) partyActors.Add(Companion.actor);
            return partyActors;
        }
        readonly List<Actor> partyActors = new List<Actor>();
        /// <summary>The Foundry's swing-loads at work this visit (S7), one for each the zone built (ZoneBuilder.SwingLoads).</summary>
        public readonly List<AditSwingLoad> SwingLoads = new List<AditSwingLoad>();
        /// <summary>
        /// (S7; Chris, 2026-10-15, Q4 "Yes, add them") Each swing-load the builder made set swinging, out of step with the others, with its rope
        /// team on the floor below: two pressed goblins and a Sandthrone hand on the tag line, one behind another (PROVISIONAL looks: the
        /// goblins as the caged ones, the hand as a deserter). Figures only: no collider, no fight.
        /// </summary>
        void StartSwingLoads()
        {
            SwingLoads.Clear(); AditSwingLoad.NewVisit();
            if (Zone == null) return;
            for (int k = 0; k < Zone.SwingLoads.Count; k++)
            {
                var spot = Zone.SwingLoads[k]; if (spot == null || spot.root == null || spot.bucket == null) continue;
                var swing = spot.root.gameObject.AddComponent<AditSwingLoad>();
                swing.spot = spot; swing.session = this; swing.phase = k * .37f; swing.period = 5.6f + k * .45f;
                var aside = Vector3.Cross(Vector3.up, spot.teamFacing).normalized;
                for (int m = 0; m < 3; m++)
                    swing.AddHauler(Hauler(spot.team - spot.teamFacing * (m * .95f) + aside * (m % 2 == 0 ? .12f : -.12f), spot.teamFacing, k * 3 + m, m == 1));
                SwingLoads.Add(swing);
            }
        }
        /// <summary>One of a rope team, facing the bucket with the line in hand.</summary>
        Transform Hauler(Vector3 feet, Vector3 facing, int seed, bool company)
        {
            var go = new GameObject(company ? "Sandthrone rope-hand" : "Pressed goblin on the rope");
            go.transform.SetPositionAndRotation(feet + Vector3.up * (company ? 1f : .66f), Quaternion.LookRotation(facing));
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; Destroy(body.GetComponent<Collider>()); body.transform.SetParent(go.transform, false);
            var v = company ? ActorVisual.Attach(go, ActorLook.Deserter, 60 + seed * 5) : ActorVisual.Attach(go, ActorLook.Villager, 41 + seed * 6, true, "goblin");
            v.Pose = ActorPose.Work;
            return go.transform;
        }
        /// <summary>A pressed goblin huddled in a cage (PROVISIONAL look: the small villager figure, grey-green, long-eared, in rags, until
        /// the goblins have a figure of their own; the goblins are CANON, the Weavers of the Warrens).</summary>
        GameObject PressedGoblin(Transform cage, int k)
        {
            var go = new GameObject("Pressed goblin"); go.transform.SetPositionAndRotation(cage.position + Vector3.up * .66f, cage.rotation * Quaternion.Euler(0, k % 2 == 0 ? 70 : -110, 0));
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; Destroy(body.GetComponent<Collider>()); body.transform.SetParent(go.transform, false);
            var v = ActorVisual.Attach(go, ActorLook.Villager, 41 + k * 6, true, "goblin"); v.Pose = ActorPose.Cower;
            return go;
        }
        /// <summary>The cage's door swung open (slowly, as you open it; at once for one opened before).</summary>
        void OpenCageDoor(ZoneInteractable cage, bool slowly)
        {
            var door = cage.root != null ? cage.root.Find("Door") : null; if (door == null) return;
            float side = door.localPosition.x > 0 ? 1 : -1; var open = Quaternion.Euler(0, side * 105, 0);
            if (slowly) StartCoroutine(SwingDoor(door, open)); else door.localRotation = open;
        }
        System.Collections.IEnumerator SwingDoor(Transform door, Quaternion to)
        {
            var from = door.localRotation;
            for (float u = 0; u < 1; u += Time.deltaTime / .7f) { if (door == null) yield break; door.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0, 1, u)); yield return null; }
            if (door != null) door.localRotation = to;
        }
        /// <summary>A cage opened (EncounterSession.Interact, after the quest has counted it): the door swings open, and its goblin gets up,
        /// slips out past you and is gone into the dark.</summary>
        void FreeGoblin(ZoneInteractable cage)
        {
            OpenCageDoor(cage, true);
            if (!caged.TryGetValue(cage, out var goblin) || goblin == null) return;
            caged.Remove(cage); StartCoroutine(GoblinOut(goblin, cage.root));
        }
        System.Collections.IEnumerator GoblinOut(GameObject goblin, Transform cage)
        {
            var v = goblin.GetComponent<ActorVisual>(); if (v != null) v.Pose = ActorPose.None;
            yield return new WaitForSeconds(.6f);
            var door = cage.Find("Door"); var outward = door != null ? cage.TransformDirection(new Vector3(door.localPosition.x > 0 ? 1 : -1, 0, 0)) : cage.forward;
            var from = goblin.transform.position; var to = from + outward * 2.2f + cage.forward * 1.5f;
            goblin.transform.rotation = Quaternion.LookRotation(new Vector3(outward.x, 0, outward.z));
            for (float u = 0; u < 1; u += Time.deltaTime / 1.4f) { if (goblin == null) yield break; goblin.transform.position = Vector3.Lerp(from, to, u); yield return null; }
            Message("The goblin squeezes out past you, says something quick and grateful in the Warrens' tongue, and is gone into the dark.");
            yield return new WaitForSeconds(1.5f);
            if (goblin != null) Destroy(goblin);
        }
        /// <summary>What E says at a usable prop (the HUD's prompt; the Adit's things say how things stand).</summary>
        public string InteractPromptFor(ZoneInteractable i) { return i == null ? null : AditPrompt(i) ?? SpoonPrompt(i) ?? i.prompt; }
        ZoneCamp CampOf(EncounterEnemy e) { var camps = Zone != null ? Zone.Zone.camps : null; return e != null && camps != null && e.CampIndex >= 0 && e.CampIndex < camps.Length ? camps[e.CampIndex] : null; }
        /// <summary>The zone's keyed camps (the sigil bosses), and how many of their keys you hold.</summary>
        List<ZoneCamp> KeyCamps() { var list = new List<ZoneCamp>(); if (Zone != null && Zone.Zone.camps != null) foreach (var c in Zone.Zone.camps) if (c != null && !string.IsNullOrEmpty(c.key)) list.Add(c); return list; }
        int KeysHeld(List<ZoneCamp> keyed) { int n = 0; foreach (var c in keyed) if (Progress.keys.Contains(c.key)) n++; return n; }

        /// <summary>An elite carrying a key, dead by your hand or your party's: the key is yours, once.</summary>
        void TakeKey(EncounterEnemy e)
        {
            var camp = CampOf(e); if (camp == null || string.IsNullOrEmpty(camp.key) || !NamedElite(e) || Progress.keys.Contains(camp.key)) return;   // Nix carries it, not his engine
            Progress.keys.Add(camp.key);
            var keyed = KeyCamps(); int have = KeysHeld(keyed);
            FloatText(e.transform.position + Vector3.up * 1.2f, char.ToUpperInvariant(camp.keyName[0]) + camp.keyName.Substring(1), new Color(1, .82f, .35f));
            Message("You take " + camp.keyName + " from " + camp.mob + ". Rail sigils " + have + "/" + keyed.Count +
                (have < keyed.Count ? ": the others are down the other ways off the Gallery." : ": set them in the sockets of the singing door, at the Gallery's north end."));
        }

        /// <summary>What E says at one of the Adit's things, or null for the prop's own prompt.</summary>
        string AditPrompt(ZoneInteractable i)
        {
            switch (i.kind)
            {
                case "liftgate": { var keyed = KeyCamps(); return "Set the rail sigils (" + KeysHeld(keyed) + "/" + keyed.Count + ")"; }
                case "cage": return Quests != null && Quests.State(PressedQuest) != null ? i.prompt : "A goblin huddles in the cage (Pib, in the Peaks, wants them out)";
                case "platformgate": return Quests != null && Quests.IsDone(PressedQuest) ? "Call the goblins to the gate (quiet)" : CarryingPowder ? "Blow the gate (loud)" : "Try the gate";
                case "powder": return CarryingPowder ? "Powder on your shoulder" : null;
                case "stone": return Progress.spiritStones.Contains(i.Key(Zone.Zone.id)) ? "The spirit stone hums" : i.prompt;
                case "weaver": return Weaver != null && Weaver.Now == AditWeaver.Stage.Waiting ? "Talk to " + AditWeaver.Name + " (walk her to the carriage)" : i.prompt;
                case "carriage": return RideAllowed ? i.prompt + " (one ride out)" : "The carriage is loading: its lock still sings";
                case "ride": case "ward": case "warden": case "pen": case "bar": case "hiddendoor": return HallowsPrompt(i);   // (Hallow's Creek, and the hand-worked gates: EncounterSession.Hallows.cs)
                // (S6) Nothing to cut once the haul is over: a line saying how she lies (a null here fell back to the prop's own "Cut the
                // warps", R132), and the bollards are no longer offered (EncounterSession.Haul.HideWarps).
                case "warps":
                    if (Haul == null) return "The warps hang slack";
                    switch (Haul.Now)
                    {
                        case AditHaul.Stage.Stopped: return Haul.Cut ? "The warps are cut" : "The warps hang slack: she is fast on the bar";
                        case AditHaul.Stage.Floated: return "The warps are taut: she is afloat";
                        default: return i.prompt;
                    }
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
                    else
                    {
                        CarryingPowder = true; Message("You heave a keg of blasting powder onto your shoulder. Mind the lamps.");
                        // (The Adit rework, ADIT_REWORK.md E4) Taking the keg brings Powder-Sergeant Hobb and his sappers down the stair behind you.
                        ReleaseAfter("event:powder", Player, null, "Boots on the stair above: Powder-Sergeant Hobb and his sappers come down after the keg.", ReleaseDelay);
                    }
                    return true;
                case "stone": TouchStone(i); return true;
                case "weaver": TalkToWeaver(); return true;
                case "carriage": RideCarriage(); return true;
                case "warps": CutWarps(i); return true;   // (S6) the barge's warps at the bollards (EncounterSession.Haul.cs)
                case "ride": RideWagon(i); return true;   // (Hallow's Creek) the black-iron wagon home (EncounterSession.Hallows.cs)
                case "ward": StudyWard(i); return true;
                case "warden": return TalkToWarden(i);
                // (Hallow's Creek's people and places, EncounterSession.Hallows) Gable's pie in the Glitch; a paused townsperson or a place looked at.
                case "pie": TakeThePie(); return true;
                case "paused": case "look": return LookAtHallows(i);
                case "pen": case "bar": case "hiddendoor": return UseHandGate(i);   // gates worked by hand (the Adit rework's and Hallow's Creek's)
                case "cage":   // the lock is the Company's: without Pib's quest, the goblin only shrinks back (with it, the quest counts it and it opens)
                    if (Quests != null && Quests.State(PressedQuest) != null) return false;
                    Message("The goblin inside flinches back from the bars. The padlock is the Company's: Pib, in the Peaks, knows how these open."); return true;
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
            Message("You set the three sigils in their sockets. The door takes up their three notes and sings them back, louder, and the stone grinds up into the rock.");
            Save(false);
            StartEvent(WakeEvent);   // (the Adit rework, E1) the door's song wakes the Gallery: the Wake, when the zone has it (EncounterSession.Release.cs)
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
                if (!(lord != null && GuardOf(e, lord)) && Vector2.Distance(e.CampCenter, at) > 78) continue;   // (26 m in the first Adit; drawn out three times, 2026-10-10)
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
        /// <summary>How long the ride up the line takes on the bar (D8; tests shorten it).</summary>
        public static float RideSeconds = 5;
        /// <summary>
        /// The ride out (dungeon step D8): with the lock matched, the goblins take the mules' heads and walk the carriage back up the line
        /// to a siding under the Adit yard. E at its step: a few seconds on the bar (moving or a blow stops it, as with any work), then you,
        /// Mira and the sims stand at the yard. Before the lock is matched the carriage is still the Company's. No machines (Chris, 2026-10-11).
        /// (S6) With Danner's finale in the Black Cove (CoveFinale), his death frees it as well as the lock does (RideAllowed).
        /// </summary>
        void RideCarriage()
        {
            if (!RideAllowed)
            {
                Message("The carriage is loading, its geodes glowing in their crates, and the lock on its side still sings the Company's note. " +
                    (CoveFinale ? "Stop it first: the Weaver knows the way, or bring down Danner on his barge in the Black Cove, down the line." : "Stop it first: the Weaver knows the way."));
                return;
            }
            if (InCombat) { Message(FightingLine); return; }
            Message("You climb up onto the carriage. Two goblins take the mules' heads, cluck at them, and the wagons creak off up the line.");
            StartWork("Riding the line", RideSeconds, RideOut);
        }
        void RideOut()
        {
            var siding = StartPoint;
            Player.GetComponent<AdventurerMotor>().Teleport(siding);
            if (Companion != null) { var ma = Companion.GetComponent<NavMeshAgent>(); if (ma != null) ma.Warp(siding + Vector3.right * 2.5f - Vector3.up * .1f); }
            foreach (var c in PartySims) { if (c == null) continue; var agent = c.GetComponent<NavMeshAgent>(); if (agent != null) agent.Warp(siding + Vector3.left * (2 + c.slot) - Vector3.up * .1f); }
            foreach (var e in Enemies) if (e != null && e.Victim != null) e.ResetFight();
            Message("A long, slow climb in the dark behind the mules, the Gallery's song far off through the rock, then grey daylight: the carriage stops at a siding under the Adit yard, and the goblins wave you off. The line is theirs now.");
            Save(false);
        }
        /// <summary>Whether a camp stands past a shut gate in its cave (the run a sim leads waits there).</summary>
        public ZoneGate GateBefore(ZoneCamp camp)
        {
            if (Zone == null || camp == null) return null;
            if (!string.IsNullOrEmpty(camp.gate)) { var named = GateAhead(camp); return named != null && !named.Open ? named : null; }   // (the Adit rework) the gate its data names
            if (string.IsNullOrEmpty(camp.cave)) return null;
            foreach (var g in Zone.Gates) if (g != null && !g.Open && g.Cave == camp.cave && camp.along > g.Along) return g;
            return null;
        }
        /// <summary>The last gate across the cave before a camp, open or shut (D7: the run a sim leads waits at it while it is shut), or null.
        /// (The Adit rework) A camp whose data names its gate (ZoneCamp.gate, by title; "none": no gate) has that one: in a web of caves the
        /// way to a camp need not come up its own cave from the mouth (the Geode Foundry's hall is reached from its far end, past the chute
        /// door by how far in it stands but never through it).</summary>
        public ZoneGate GateAhead(ZoneCamp camp)
        {
            if (Zone == null || camp == null) return null;
            if (!string.IsNullOrEmpty(camp.gate)) return camp.gate == "none" ? null : Zone.Gates.Find(g => g != null && g.Title == camp.gate);
            if (string.IsNullOrEmpty(camp.cave)) return null;
            ZoneGate last = null;
            foreach (var g in Zone.Gates) if (g != null && g.Cave == camp.cave && camp.along > g.Along && (last == null || g.Along > last.Along)) last = g;
            return last;
        }
    }
    /// <summary>A mule in the Sealed Adit's carriage traces (EncounterSession.StartAdit): it stands, shifts and lowers its head as a farm
    /// animal does at rest.</summary>
    public sealed class PitMule : MonoBehaviour { public CritterBody body; void Update() { body?.Rest(); } }
}
