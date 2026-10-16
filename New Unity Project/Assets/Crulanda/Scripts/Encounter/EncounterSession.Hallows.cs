using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Hallow's Creek's dungeon objects (Docs/DUNGEON2_DESIGN.md sections 3 and 4; GAME-ONLY mechanics over a CANON place, book1 ch.15-21):
    /// - the paused townsfolk: stone figures where the builder found them (ZoneBuilder.PausedSpots), no agent, no collider, their first frame
    ///   frozen; the party never fights them (N5's look);
    /// - the town waking (section 4.2, N5): a Glitch (the bakery's) or the Waking Hour (the great bell) brings the colour back (ZonePost.Swing,
    ///   a post-process swing), wakes the paused near it (the whole town at the Hour) so they move, and shifts the hungry shadows (4.1); after
    ///   its hold (ZoneEvent.hold) the loop snaps: three seconds before, a ring is warned round every townsperson awake near you (and round the
    ///   ringers still climbing), and whoever stands in one at the snap is paused three seconds (never while someone is held already); the
    ///   ringers go back to stone, the colour drains, and at the Hour the Examiner stands stone for five seconds;
    /// - the pause (4.3, N6): a Bureau hold (EliteMove.ccLook "stone": the Assessor's, the Examiner's) turns the one held to grey stone while it
    ///   lasts (StoneHold), you or a sim alike;
    /// - the Warden's walk (E2; sections 3 and 6): Registrar Pardew's death (or the cell's bar lifted) frees Warden Sigrid Elvet of the
    ///   Preservationists, who walks from her cell to the Bell Tower's door at her own pace, stopping while the party fights or falls behind;
    ///   she is one of the party for every mob, for Mira and the sims (PartyActor), and the walk's waves come at her at a third and two thirds
    ///   of the way (each only while its boss stands, ZoneWave.unless). If she falls, whatever was on her lets go, the waves go back into the
    ///   grey, and a little later she is back in her cell to be walked again (talk to her); with the party down she goes back to it too. At the
    ///   door she unbinds the Bureau's ward-lock, kept for good (Progress.keys "hallows.ward"), and keeps the door after: on later visits she
    ///   stands there;
    /// - the chasm's fog (N3): anyone who falls into it is set back on the last deck or ground they stood on, never under the world;
    /// - the hungry shadows (N4): standing in one slows you and wears at you (never the last of your health);
    /// - the wagon home (N7, ZoneRide): once the Picker-Master is down, the black-iron wagon takes the party to the milestone;
    /// - gates worked by hand (pen, bar, hidden door; the Adit's too): E opens them, from the right side, once whoever holds them is down; a
    ///   bar that bars behind (the Cellars' far trapdoor, ZoneGate.ShutsBehind) drops again once the whole party is through.
    /// No machines (DUNGEON_DESIGN 0.05): mules, a bell rung by hand, a painted dial.
    /// </summary>
    public sealed partial class EncounterSession
    {
        public const string WardKey = "hallows.ward", WardenName = "Warden Sigrid Elvet", ExaminerName = "Bureau Examiner Thurstan Vowell",
            WardGate = "The ward-lock", WardenCell = "The Warden's cell", WalkEvent = "walk", WakingEvent = "waking", GlitchEvent = "glitch", WardenPapers = "item.hallows_wardens_papers";
        /// <summary>How fast you move over the ground you stand on (a hungry shadow slows you): 1 elsewhere.</summary>
        public float GroundSlow { get; private set; } = 1;
        /// <summary>The Warden this visit (null until her cell opens; on a visit after the ward was unbound, standing at the tower's door).</summary>
        public HallowsWarden Warden { get; private set; }
        /// <summary>Seconds the Examiner stands stone when the Waking Hour's loop snaps (tests read it).</summary>
        public const float SnapSeconds = 5;
        /// <summary>Seconds a townsperson's ring is warned before the loop snaps, its reach, and how long it pauses whoever stands in it
        /// (section 4.2: "a 1.5 m circle, warned for 3 s ... paused 3 s"). Tests shorten the warning.</summary>
        public static float SnapWarnSeconds = 3;
        public const float SnapRing = 1.5f, SnapPause = 3;
        /// <summary>How far the hungry shadows slide while the town is awake (metres).</summary>
        public const float ShadowShift = 1.8f;
        /// <summary>The paused townsfolk standing this visit.</summary>
        public IReadOnlyList<PausedStone> PausedFolk { get { return pausedFolk; } }
        /// <summary>Whether the town is awake now (a Glitch or the Waking Hour).</summary>
        public bool TownAwake { get { return wokenBy != null; } }
        /// <summary>The townsfolk's rings warned before a snap (their centres), while the warning lasts; empty otherwise.</summary>
        public IReadOnlyList<Vector3> SnapRings { get { return snapAt; } }
        readonly List<PausedStone> pausedFolk = new List<PausedStone>();
        readonly List<Vector3> snapAt = new List<Vector3>(); readonly List<BlowMark> snapMarks = new List<BlowMark>(); readonly List<Transform> snapFollow = new List<Transform>();
        bool wardenFreed; Vector3 lastSafe; bool haveSafe; float nextHazard, swingTarget, snapWarnFrom = -1; string wokenBy;

        /// <summary>On arrival (StartAdit): the paused stand where they stood, the wagon's mules in their traces, the ward-lock open if it was unbound
        /// (and the Warden keeping its door), the town asleep and grey.</summary>
        void StartHallows()
        {
            GroundSlow = 1; wardenFreed = false; Warden = null; haveSafe = false; pausedFolk.Clear(); wokenBy = null; swingTarget = 0; ZonePost.Swing = 0; ClearSnap();
            if (Zone == null) return;
            for (int k = 0; k < Zone.PausedSpots.Count; k++) { var s = Zone.PausedSpots[k]; pausedFolk.Add(PausedFigure(s.at, s.facing, s.pose, s.name, k)); }
            for (int k = 0; k < Zone.RideTeam.Count; k++)
            {
                var mule = new GameObject("Wagon mule").transform; mule.SetPositionAndRotation(Zone.RideTeam[k].at, Zone.RideTeam[k].facing);
                mule.gameObject.AddComponent<PitMule>().body = CritterBody.Build(mule, "donkey", .3f + k * .4f, 23 + k * 5);
                var box = mule.gameObject.AddComponent<BoxCollider>(); box.size = new Vector3(.55f, 1.15f, 1.6f); box.center = new Vector3(0, .575f, 0); mule.gameObject.layer = 2;
            }
            if (Progress != null && Progress.keys.Contains(WardKey))
            {
                var g = Zone.Gates.Find(x => x != null && x.Title == WardGate); if (g != null) g.SetOpen(true, true);
                if (Zone.Anchors.TryGetValue("the tower door", out var door)) { Warden = SpawnWarden(door); if (Warden != null) Warden.KeepDoor(); }   // she keeps the door she unbound
            }
        }
        /// <summary>A paused townsperson: a villager's figure turned to stone and stopped in its first frame (no agent, no collider).</summary>
        PausedStone PausedFigure(Vector3 feet, Quaternion facing, int pose, string name, int k)
        {
            var go = new GameObject(string.IsNullOrEmpty(name) ? "A paused townsperson" : name);
            go.transform.SetPositionAndRotation(feet + Vector3.up, facing);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; Destroy(body.GetComponent<Collider>()); body.transform.SetParent(go.transform, false);
            var v = ActorVisual.Attach(go, ActorLook.Villager, 300 + k * 7, false, pose == 1 ? "baker" : null);
            v.Pose = pose == 1 ? ActorPose.Work : pose == 2 ? ActorPose.Talk : pose == 3 ? ActorPose.Cower : ActorPose.None;
            return go.AddComponent<PausedStone>();
        }

        // ---------- each frame ----------
        void TickHallows()
        {
            if (Zone == null || Player == null) return;
            // The colour swing (ZonePost.Swing) toward the town's state: two seconds to wake, a second and a half to drain.
            ZonePost.Swing = Mathf.MoveTowards(ZonePost.Swing, swingTarget, Time.deltaTime / (swingTarget > ZonePost.Swing ? 2f : 1.5f));
            for (int i = 0; i < snapMarks.Count; i++)
            {
                if (snapMarks[i] == null) continue;
                if (snapFollow[i] != null) snapAt[i] = snapFollow[i].position - Vector3.up;   // a ringer's ring goes with the ringer
                snapMarks[i].Show(snapAt[i], SnapRing, Mathf.Clamp01((Time.time - snapWarnFrom) / Mathf.Max(.01f, SnapWarnSeconds)));
            }
            // The Warden's cell open (Pardew down, or the bar lifted): she comes out and the walk begins.
            if (!wardenFreed)
            {
                var cell = Zone.Gates.Find(g => g != null && g.Title == WardenCell);
                if (cell != null && cell.Open) FreeWarden(cell);
            }
            // A bar that bars behind (the Cellars' far trapdoor): it drops again once the whole party is through to its near side.
            foreach (var g in Zone.Gates) if (g != null && g.ShutsBehind && g.Open && PartyThrough(g)) BarBehind(g);
            // The chasm's fog: a fall into it sets you back where you last stood.
            var p = Player.transform.position; var q = new Vector2(p.x, p.z);
            bool overFog = Zone.FogFloor(q, out float fog);
            var cc = Player.GetComponent<CharacterController>();
            if (cc != null && cc.isGrounded && Player.IsAlive && (!overFog || p.y > fog + 3)) { lastSafe = p; haveSafe = true; }
            if (overFog && p.y < fog && haveSafe)
            {
                Player.GetComponent<AdventurerMotor>().Teleport(lastSafe + Vector3.up * .3f);
                Message("The fog takes you, cold and grey, a minute that does not pass; and then you are standing where you stood before, and your heart is going very fast.");
            }
            // The hungry shadows: slowed in one, and worn a little each half second.
            var shadow = Zone.Hazards.Count > 0 ? Zone.HazardAt(p) : null;
            float slow = shadow != null ? Mathf.Clamp(shadow.def.slow, .2f, 1) : 1;
            if (slow < 1 && GroundSlow >= 1) Message("You step into the shadow and the cold of it takes a minute from you. Your legs are slow to answer.");
            GroundSlow = slow;
            if (shadow != null && Time.time >= nextHazard && Player.IsAlive)
            {
                nextHazard = Time.time + .5f; var hp = Player.Health; int bite = Mathf.Max(1, Mathf.RoundToInt(hp.Pool.Max * shadow.def.tick * .5f));
                if (hp.Pool.Current - bite > hp.Pool.Max * .15f) hp.ApplyDamage(bite);
            }
        }
        /// <summary>Whether everyone of the party standing is on a gate's near side (more than a stride from it): the gate may bar behind them.</summary>
        bool PartyThrough(ZoneGate g)
        {
            foreach (var a in PartyActors())
            {
                if (a == null || !a.IsAlive) continue;
                var d = a.transform.position - g.transform.position;
                if (Vector3.Dot(d, g.transform.forward) > -1.5f || d.magnitude < 2.5f) return false;
            }
            return true;
        }
        void BarBehind(ZoneGate g)
        {
            g.SetOpen(false);
            Message(g.Title.IndexOf("trapdoor", System.StringComparison.OrdinalIgnoreCase) >= 0
                ? "Behind you the trapdoor drops, and something below drops its bar back into the brackets. The cellars are shut under the town."
                : g.Title + " swings to behind you, and the bar falls back into its brackets.");
        }

        // ---------- the Warden's walk (E2) ----------
        /// <summary>Warden Elvet, an actor the mobs may strike and the party mends (PartyActor), standing at <paramref name="at"/>.</summary>
        HallowsWarden SpawnWarden(Vector3 at)
        {
            if (!NavMesh.SamplePosition(at, out var hit, 4, NavMesh.AllAreas)) return null;
            var a = SpawnActor(WardenName, content.healer, hit.position + Vector3.up, new Color(.35f, .5f, .32f), "npc.hallows.elvet", ActorLook.Villager, 19, 61, false, "warden");
            AddAgent(a.gameObject, HallowsWarden.Pace, .4f);
            a.gameObject.SetActive(true);
            a.Stats.SetBase(StatType.MaxHealth, EncounterEnemy.MobHealth(19, false, true, false)); a.Health.ApplyHealing(a.Health.Pool.Max);   // an elite's health: she can take a beating, not a long one
            var w = a.gameObject.AddComponent<HallowsWarden>();
            Zone.Anchors.TryGetValue("the tower door", out var door); Zone.Anchors.TryGetValue("the Warden's cell", out var cell);
            w.Init(a, this, NavMesh.SamplePosition(door, out var d, 4, NavMesh.AllAreas) ? d.position : door, NavMesh.SamplePosition(cell, out var c, 4, NavMesh.AllAreas) ? c.position : hit.position);
            w.talk = new ZoneInteractable { name = WardenName, prompt = "Talk to Warden Elvet", kind = "warden", position = a.transform.position, root = a.transform };
            Zone.Interactables.Add(w.talk);
            return w;
        }
        void FreeWarden(ZoneGate cell)
        {
            wardenFreed = true;
            if (Warden != null) { Message("The cell is empty: Warden Elvet keeps the Bell Tower's door now, as she has since you walked her there."); return; }
            if (!Zone.Anchors.TryGetValue("the Warden's cell", out var from)) from = cell.transform.position;
            Warden = SpawnWarden(from); if (Warden == null) return;
            Message("The bar drops. A woman in a Preservationist's grey steps out of the cell, thin and steady: Warden Sigrid Elvet. \"The tower,\" she says. \"The Bureau has a ward on its door, and I know its knots. Walk with me, and keep them off me.\"");
            Warden.Begin();
            StartEvent(WalkEvent);
        }
        bool TalkToWarden(ZoneInteractable i)
        {
            var w = Warden; if (w == null) return true;
            switch (w.Now)
            {
                case HallowsWarden.Stage.Waiting:
                    if (w.Begin()) { w.Say("Again, then. Keep them off me this time."); StartEvent(WalkEvent); }
                    else w.Say("Not now. Give me a moment.");
                    break;
                case HallowsWarden.Stage.Arrived:
                    w.Say("Go up. Stop the bell. I will keep the door, and the paused will keep me; they always have."); QuestUse(i); break;
                default: w.Say("Walk, don't talk. They are watching from every doorstep."); break;
            }
            return true;
        }
        /// <summary>The Warden at the tower's door: the ward-lock unbound, for good.</summary>
        public void WardenArrived(HallowsWarden w)
        {
            var g = Zone != null ? Zone.Gates.Find(x => x != null && x.Title == WardGate) : null;
            if (g != null && !g.Open) g.SetOpen(true);
            if (Progress != null && !Progress.keys.Contains(WardKey)) Progress.keys.Add(WardKey);
            Message("Warden Elvet lays both hands on the black sunburst and says something under her breath, a list, it sounds like, of the town's dead. The ward lets go like a held breath, and the tower door stands open.");
            ReconcileQuests(); Save(false);
        }
        /// <summary>She fell: whatever was on her lets go, the walk's waves go back into the grey, and her cell takes her back a little later.</summary>
        public void WardenFell(HallowsWarden w)
        {
            foreach (var e in Enemies) if (e != null && e.actor.IsAlive && e.Victim == w.actor) e.ResetFight();
            AbandonEvent(WalkEvent);
            Message("Warden Elvet goes down on the cobbles. The Pact and the Hollow melt back into the grey; a little later two of the paused, it seems, have carried her back to her cell. Talk to her when you are ready to walk her again.");
        }
        /// <summary>Back in her cell, whole, to set off again when spoken to.</summary>
        public void WardenBack(HallowsWarden w)
        {
            w.actor.Health.Revive(w.actor.Health.Pool.Max); w.actor.Health.ApplyHealing(w.actor.Health.Pool.Max);
            w.Restart();
        }
        void StudyWard(ZoneInteractable i)
        {
            var g = i.gate; if (g != null && g.Open) return;
            Message("A black sunburst burned into the boards, and round it a ward of the Bureau's: knots of grey that slide away when you look at them. A Preservationist could unbind it. The Archive's cells are said to hold one.");
        }
        /// <summary>How far through her walk the Warden is (0 to 1): the walk's clock (an escort's waves come at its share).</summary>
        float EscortShare() { return Warden != null ? Warden.Walked : 0; }
        /// <summary>Whom an escort's waves are sent at: the Warden while she walks, else you.</summary>
        Actor EscortFoe() { return Warden != null && Warden.actor != null && Warden.actor.IsAlive && Warden.Now == HallowsWarden.Stage.Walking ? Warden.actor : LiveFoe(); }

        // ---------- the town waking: a Glitch and the Waking Hour (section 4.2) ----------
        /// <summary>An event begins (EncounterSession.Release.StartEvent): a Glitch or the Hour wakes the town.</summary>
        void EventBegun(string id)
        {
            if (id != GlitchEvent && id != WakingEvent) return;
            wokenBy = id; swingTarget = 1; ClearSnap();
            // The Hour wakes the whole town; the bakery's bell only the street round the bakery.
            var ev = EventNamed(id); Vector3 near = Player != null ? Player.transform.position : Vector3.zero;
            if (ev != null && !string.IsNullOrEmpty(ev.anchor) && Zone.Anchors.TryGetValue(ev.anchor, out var at)) near = at;
            foreach (var f in pausedFolk) if (f != null && (id == WakingEvent || Flat(f.transform.position - near) < 45)) f.Wake();
            Zone.ShiftHazards(ShadowShift);
        }
        /// <summary>The snap's warning (EncounterSession.Release.RunEvent, its hold nearly out): a ring round every townsperson awake within 40 m
        /// of you and every ringer of the event still standing. Not while anyone of the party is held already (it waits for them). True once
        /// the warning is under way.</summary>
        bool SnapWarning(EventRunView run)
        {
            if (snapWarnFrom >= 0) return true;
            if (StoneHold.AnyIn(PartyActors()) || PlayerHeld) return false;
            snapWarnFrom = Time.time;
            var me = Player != null ? Player.transform.position : Vector3.zero;
            foreach (var f in pausedFolk) if (f != null && f.Awake && Vector3.Distance(f.transform.position, me) < 40) AddSnapRing(f.transform.position - Vector3.up, null);
            foreach (var e in Enemies)
                if (e != null && e.Camp && e.actor.IsAlive && run.Camps.Contains(e.CampIndex) && Zone.Zone.camps[e.CampIndex].look == "pale") AddSnapRing(e.transform.position - Vector3.up, e.transform);
            Message("The loop is closing: the air goes tight, and round every townsperson who woke a ring of grey light settles on the ground. Do not be standing in one when it snaps.");
            return true;
        }
        /// <summary>Whether the snap's warning has run its seconds.</summary>
        bool SnapWarned() { return snapWarnFrom >= 0 && Time.time - snapWarnFrom >= SnapWarnSeconds; }
        void AddSnapRing(Vector3 feet, Transform follow) { snapAt.Add(feet); snapFollow.Add(follow); var m = BlowMark.Create(); m.Show(feet, SnapRing, 0); snapMarks.Add(m); }
        void ClearSnap()
        {
            foreach (var m in snapMarks) if (m != null) Destroy(m.gameObject);
            snapMarks.Clear(); snapAt.Clear(); snapFollow.Clear(); snapWarnFrom = -1;
        }
        /// <summary>The loop snaps (an event with a hold over): whoever stands in a ring is paused, the colour drains, the woken go back to stone,
        /// the shadows lie where they lay, and at the Hour its ringers still standing are stone again and the Examiner stands stone.</summary>
        void Snap(EventRunView run)
        {
            int caught = 0; bool you = false;
            foreach (var a in PartyActors())
            {
                if (a == null || !a.IsAlive) continue;
                bool inRing = false; foreach (var c in snapAt) if (Flat(a.transform.position - Vector3.up - c) <= SnapRing) inRing = true;
                if (!inRing) continue;
                if (a == Player) { HoldPlayer("stun", SnapPause, a.transform.position, "The loop's snap"); you = true; }
                else { var sim = a.GetComponent<SimCompanion>(); if (sim != null) sim.HoldUntil("stun", SnapPause, a.transform.position); else continue; }
                StoneHold.Apply(a.gameObject, SnapPause); caught++;
            }
            if (caught > 0) Message(you ? "The loop snaps shut on you where you stand, and for three seconds you are stone with the rest of the town." : "The loop snaps shut, and whoever stood in a townsperson's ring is stone with the town for three seconds.");
            ClearSnap(); SleepTown();
            if (run.Id != WakingEvent) return;
            // The ringers still on the stair are townsfolk: stone again where they stand.
            foreach (var e in Enemies.ToArray())
            {
                if (e == null || !e.Camp || !e.actor.IsAlive || !run.Camps.Contains(e.CampIndex)) continue;
                if (Target == e) { Target = null; AutoAttack = false; }
                var feet = e.transform.position - Vector3.up; var facing = e.transform.rotation;
                Enemies.Remove(e); Destroy(e.gameObject);
                pausedFolk.Add(PausedFigure(feet, facing, 0, "A paused ringer", 900 + pausedFolk.Count));
            }
            foreach (var e in Enemies)
            {
                if (e == null || !e.actor.IsAlive || e.MobName != ExaminerName) continue;
                e.StandStone(SnapSeconds);
                // (Section 6) A marked window: the skull on him, so every party sim turns on him while he stands stone, and the leader says so.
                e.RaidMarked = RaidMark.Skull;
                foreach (var c in PartySims) if (c != null && c.actor != null && c.actor.IsAlive) { ChatSay(ChatChannel.Party, c.sim.name, c.sim.chatty > .65f ? "hes stone! everyone on him NOW" : "He is stone. Everyone on the Examiner, now!"); break; }
                Message("The loop snaps. The colour drains out of the town like water out of a print, and the Examiner stops with his hand on the rope: stone, for a breath or five. Now.");
            }
        }
        /// <summary>The town grey and still again: the colour drained, the woken stone, the shadows back where they lay.</summary>
        void SleepTown()
        {
            if (wokenBy == null) return;
            wokenBy = null; swingTarget = 0;
            foreach (var f in pausedFolk) if (f != null) f.Sleep();
            if (Zone != null) Zone.ShiftHazards(0);
        }
        /// <summary>An event over (EncounterSession.Release.FinishEvent): a held one's loop snaps.</summary>
        void EventOver(EventRunView run) { if (run.Id == GlitchEvent || run.Id == WakingEvent) Snap(run); }
        /// <summary>An event fallen (the party down, or the Warden): the town sleeps again, and the Warden goes back to her cell to wait.</summary>
        void EventFailed(string id)
        {
            if (id == GlitchEvent || id == WakingEvent) { ClearSnap(); SleepTown(); }
            if (id == WalkEvent && Warden != null && Warden.Now == HallowsWarden.Stage.Walking)
            {
                WardenBack(Warden);
                Message("With nobody standing to keep them off her, Warden Elvet goes back into her cell and pulls the bar to. She will walk again when you ask her.");
            }
        }
        /// <summary>The words of Hallow's Creek's events as they start, end or fall; null for any other event.</summary>
        static string HallowsEventLine(string id, string moment)
        {
            switch (id)
            {
                case GlitchEvent:
                    return moment == "start" ? "Somewhere in the bakery the shop bell gives one note. For a heartbeat the room is warm and smells of bread, and out on the street the paused begin to move." :
                        moment == "end" ? "The bell is still. On the counter, two copper pennies, warm to the touch." : "The bakery goes grey again with nobody in it to hear the bell.";
                case WalkEvent:
                    return moment == "start" ? "Warden Elvet sets off across the square toward the Bell Tower, not fast, not slow, the way you walk in a town where everyone is watching." :
                        moment == "end" ? "Nothing more comes after the Warden. The way to the tower is clear." : "The Warden's walk falls silent; whatever came after her goes back into the grey.";
                case WakingEvent:
                    return moment == "start" ? "The Examiner hauls on the bell rope and the great bell speaks. Colour comes back into the town all at once, and the square below starts to move." :
                        moment == "end" ? "The bell's note dies in the arches." : "The bell's note dies with nobody standing to hear it. The town goes grey again.";
            }
            return null;
        }
        /// <summary>Whether a mob of that name stands this visit (alive, of a camp): a boss down is not, nor a rare not here.</summary>
        bool MobStanding(string mob)
        {
            foreach (var e in Enemies) if (e != null && e.Camp && e.actor != null && e.actor.IsAlive && e.MobName == mob) return true;
            return false;
        }
        static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }

        // ---------- the wagon home (N7) ----------
        bool RideOpen { get { var r = Zone != null ? Zone.Zone.ride : null; return r != null && (string.IsNullOrEmpty(r.opensOn) || !MobStanding(r.opensOn)); } }
        void RideWagon(ZoneInteractable i)
        {
            var r = Zone.Zone.ride; if (r == null || string.IsNullOrEmpty(r.name)) { Message("Nobody will drive it."); return; }
            if (!RideOpen) { Message(string.IsNullOrEmpty(r.shut) ? "Not yet." : r.shut); return; }
            if (InCombat) { Message(FightingLine); return; }
            Message("You climb up onto the wagon. Someone takes the mules' heads.");
            StartWork("Riding the wagon", r.seconds > 0 ? r.seconds : RideSeconds, RideHome);
        }
        void RideHome()
        {
            var r = Zone.Zone.ride; var at = r != null && r.arrive != Vector2.zero ? Zone.StandAt(r.arrive, float.NegativeInfinity) : StartPoint;
            Player.GetComponent<AdventurerMotor>().Teleport(at);
            if (Companion != null) { var ma = Companion.GetComponent<NavMeshAgent>(); if (ma != null) ma.Warp(at + Vector3.right * 2.5f - Vector3.up * .1f); }
            foreach (var c in PartySims) { if (c == null) continue; var agent = c.GetComponent<NavMeshAgent>(); if (agent != null) agent.Warp(at + Vector3.left * (2 + c.slot) - Vector3.up * .1f); }
            foreach (var e in Enemies) if (e != null && e.Victim != null) e.ResetFight();
            if (r != null && !string.IsNullOrEmpty(r.text)) Message(r.text);
            Save(false);
        }

        // ---------- what E says and does ----------
        string HallowsPrompt(ZoneInteractable i)
        {
            switch (i.kind)
            {
                case "ride": return RideOpen ? i.prompt + " (the way home)" : "The wagon is the Picker-Master's";
                case "ward": return i.gate != null && i.gate.Open ? null : "The Bureau's ward-lock (a Preservationist could unbind it)";
                case "warden": return Warden != null && Warden.Now == HallowsWarden.Stage.Waiting ? "Talk to Warden Elvet (walk her to the Bell Tower)" : "Talk to Warden Elvet";
                default:
                    var g = i.gate; if (g != null && !string.IsNullOrEmpty(g.OpensOn) && MobStanding(g.OpensOn)) return "Chained shut (" + g.OpensOn + " has the key)";
                    return null;
            }
        }
        /// <summary>A gate worked by hand (pen, bar, hidden door): opened from the right side, once whoever holds it shut is down. True: handled.</summary>
        bool UseHandGate(ZoneInteractable i)
        {
            var g = i.gate; if (g == null) return false; if (g.Open) return true;
            if (!string.IsNullOrEmpty(g.OpensOn) && MobStanding(g.OpensOn)) { Message("Chained shut. " + g.OpensOn + " has the key, and is not done with it yet."); return true; }
            if (!WorksFrom(g, Player.transform.position)) { Message("It is barred from the other side."); return true; }
            g.SetOpen(true);
            Message(g.Kind == "hiddendoor" ? "Your fingers find the edge of a door in the stone, and it swings in onto the dark."
                : g.Title.IndexOf("trapdoor", System.StringComparison.OrdinalIgnoreCase) >= 0 ? "You lift the trapdoor's bar out of its brackets and heave the boards up: grey daylight, and the well-house roof."
                : g.Kind == "bar" ? "You lift the bar out of its brackets and swing the gate open." : "You lift the bar and the gate swings open.");
            return true;
        }
        /// <summary>Whether a gate worked by hand can be worked from where someone stands (a bar lifted from its far side only from there).</summary>
        static bool WorksFrom(ZoneGate g, Vector3 at) { return !g.FromFar || Vector3.Dot(at - g.transform.position, g.transform.forward) >= 0; }
        /// <summary>
        /// (Section 6; the run a sim leads) A bar across its way that the leader can lift itself: a bar (not a boss's door, not a pen or a hidden
        /// door), nobody standing who holds it shut, and the leader on the side it lifts from, within reach. Lifts it and says so; false when it
        /// cannot (the run then waits for you, D7).
        /// </summary>
        public bool LiftBar(ZoneGate g, SimCompanion by)
        {
            if (g == null || g.Open || by == null || g.Kind != "bar" || (!string.IsNullOrEmpty(g.OpensOn) && MobStanding(g.OpensOn))) return false;
            var at = by.transform.position; if (!WorksFrom(g, at) || Flat(at - g.transform.position) > 4.5f) return false;
            g.SetOpen(true);
            ChatSay(ChatChannel.Party, by.sim.name, by.sim.chatty > .65f ? "got the bar. up we go" : "I have the bar. Through, all of you.");
            return true;
        }
    }

    /// <summary>The view of a running event the Hallows code reads (EncounterSession.Release's EventRun): its id and the camps it released.</summary>
    public sealed class EventRunView { public string Id; public HashSet<int> Camps; }

    /// <summary>A paused townsperson (Hallow's Creek, N5): once the figure is built (two frames on), every part of it turned to grey stone and
    /// its motion stopped, so it stands in its first frame. A Glitch or the Waking Hour wakes it (<see cref="Wake"/>: its colours back and its
    /// pose moving) until the loop snaps (<see cref="Sleep"/>). No collider, no agent: nobody fights it.</summary>
    public sealed class PausedStone : MonoBehaviour
    {
        static Material stone;
        int frames; bool stoned, awake;
        readonly List<(Renderer r, Material[] mats)> colours = new List<(Renderer, Material[])>();
        /// <summary>Whether it is awake now.</summary>
        public bool Awake { get { return awake; } }
        /// <summary>Whether it stands stone now.</summary>
        public bool Stone { get { return stoned; } }
        void LateUpdate()
        {
            if (stoned || awake || ++frames < 3) return;
            Turn(true); enabled = false;
        }
        /// <summary>Its colours and its motion back (a Glitch, the Waking Hour).</summary>
        public void Wake() { awake = true; if (stoned) Turn(false); }
        /// <summary>Stone again and still (the loop snapped).</summary>
        public void Sleep() { awake = false; if (!stoned) Turn(true); }
        void Turn(bool toStone)
        {
            if (toStone)
            {
                colours.Clear();
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    if (stone == null && r.sharedMaterial != null)
                    {
                        stone = new Material(r.sharedMaterial) { name = "Paused stone", color = new Color(.63f, .62f, .61f) };
                        if (stone.HasProperty("_MainTex")) stone.mainTexture = null;
                        if (stone.HasProperty("_EmissionColor")) stone.SetColor("_EmissionColor", Color.black);
                    }
                    if (stone == null) continue;
                    colours.Add((r, r.sharedMaterials));
                    var mats = r.sharedMaterials; for (int k = 0; k < mats.Length; k++) mats[k] = stone; r.sharedMaterials = mats;
                }
            }
            else foreach (var (r, mats) in colours) if (r != null) r.sharedMaterials = mats;
            stoned = toStone;
            foreach (var a in GetComponentsInChildren<Animator>(true)) a.enabled = !toStone;
            foreach (var a in GetComponentsInChildren<Animation>(true)) a.enabled = !toStone;
            var v = GetComponent<ActorVisual>(); if (v != null) v.enabled = !toStone;
        }
    }

    /// <summary>
    /// The pause on one of the party (Hallow's Creek, DUNGEON2_DESIGN 4.3, N6: the Bureau's hold, the loop's snap): every part of the held
    /// one grey stone and their motion stopped for its seconds, then their own colours and motion back. The hold itself (a stun) is the
    /// session's (HoldPlayer) or the sim's (HoldUntil); this is the look, and what the bosses ask ("none starts while someone is held").
    /// </summary>
    public sealed class StoneHold : MonoBehaviour
    {
        static Material grey;
        float until;
        readonly List<(Renderer r, Material[] mats)> kept = new List<(Renderer, Material[])>();
        readonly List<(Animator a, float speed)> animators = new List<(Animator, float)>();
        ActorVisual visual; bool visualWas;
        /// <summary>Turns <paramref name="who"/> to stone for <paramref name="seconds"/> (longer if it is stone already).</summary>
        public static void Apply(GameObject who, float seconds)
        {
            if (who == null || seconds <= 0) return;
            var h = who.GetComponent<StoneHold>(); if (h == null) h = who.AddComponent<StoneHold>();
            h.until = Mathf.Max(h.until, Time.time + seconds); h.enabled = true;
        }
        /// <summary>Whether this one stands stone now.</summary>
        public static bool On(GameObject who) { var h = who != null ? who.GetComponent<StoneHold>() : null; return h != null && h.enabled && Time.time < h.until; }
        /// <summary>Whether any of these stands stone now.</summary>
        public static bool AnyIn(IEnumerable<Actor> actors) { if (actors == null) return false; foreach (var a in actors) if (a != null && a.IsAlive && On(a.gameObject)) return true; return false; }
        void OnEnable()
        {
            if (kept.Count > 0) return;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r is ParticleSystemRenderer || r.sharedMaterial == null) continue;
                if (grey == null)
                {
                    grey = new Material(r.sharedMaterial) { name = "Paused stone (held)", color = new Color(.6f, .6f, .62f) };
                    if (grey.HasProperty("_MainTex")) grey.mainTexture = null;
                    if (grey.HasProperty("_EmissionColor")) grey.SetColor("_EmissionColor", Color.black);
                }
                kept.Add((r, r.sharedMaterials));
                var mats = r.sharedMaterials; for (int k = 0; k < mats.Length; k++) mats[k] = grey; r.sharedMaterials = mats;
            }
            foreach (var a in GetComponentsInChildren<Animator>(true)) { animators.Add((a, a.speed)); a.speed = 0; }
            visual = GetComponent<ActorVisual>(); if (visual != null) { visualWas = visual.enabled; visual.enabled = false; }
        }
        void Update() { if (Time.time >= until) enabled = false; }
        void OnDisable()
        {
            foreach (var (r, mats) in kept) if (r != null) r.sharedMaterials = mats;
            foreach (var (a, speed) in animators) if (a != null) a.speed = speed;
            if (visual != null) visual.enabled = visualWas;
            kept.Clear(); animators.Clear(); visual = null;
        }
    }

    /// <summary>Warden Sigrid Elvet of the Preservationists (GAME-ONLY), walking from her cell in the Old Archive to the Bell Tower's door (E2):
    /// at her own pace, stopping while the party fights, while something is on her or while none of the party is near, and unbinding the
    /// ward-lock when she gets there. She can be struck and can fall (the waves come at her: DUNGEON2_DESIGN section 6, "the waves come to
    /// her"); fallen, she is back in her cell a little later, to be walked again.</summary>
    public sealed class HallowsWarden : MonoBehaviour
    {
        public enum Stage { Waiting, Walking, Arrived, Fallen }
        public const float Pace = 1.7f;
        /// <summary>How far the nearest of the party may be before she waits for them, and how long after a fall she is back in her cell.
        /// Tests shorten them.</summary>
        public static float WaitFor = 16, BackAfter = 30;
        public Actor actor;
        /// <summary>Her "Talk to Warden Elvet" (kind "warden"): it goes where she goes.</summary>
        public ZoneInteractable talk;
        public Stage Now { get; private set; }
        /// <summary>How far through the walk she is (0 to 1).</summary>
        public float Walked { get; private set; }
        /// <summary>Stopped for the party or for a fight.</summary>
        public bool Holding { get; private set; }
        public bool Arrived { get { return Now == Stage.Arrived; } }
        public Vector3 Goal { get { return goal; } }
        EncounterSession session; NavMeshAgent agent; Vector3 goal, cell; float total, fellAt;

        public void Init(Actor a, EncounterSession s, Vector3 to, Vector3 home)
        {
            actor = a; session = s; goal = to; cell = home; agent = GetComponent<NavMeshAgent>();
            if (agent != null) { agent.speed = Pace; agent.stoppingDistance = .5f; agent.areaMask &= ~(1 << EncounterNavigation.DropArea); }
            Now = Stage.Waiting;
        }
        /// <summary>Sets off for the tower's door. False when she is already on her way, there, or down.</summary>
        public bool Begin()
        {
            if (Now != Stage.Waiting || !actor.IsAlive || agent == null || !agent.isOnNavMesh) return false;
            agent.isStopped = false; agent.SetDestination(goal); total = Mathf.Max(1, Flat(goal - transform.position)); Walked = 0; Now = Stage.Walking;
            return true;
        }
        /// <summary>Back in her cell, waiting to be walked again.</summary>
        public void Restart()
        {
            Now = Stage.Waiting; Walked = 0; Holding = false;
            if (agent != null && agent.isOnNavMesh) { agent.Warp(cell); agent.isStopped = true; } else transform.position = cell + Vector3.up;
        }
        /// <summary>At the tower's door for good (a visit after she unbound the ward).</summary>
        public void KeepDoor() { Now = Stage.Arrived; Walked = 1; if (agent != null && agent.isOnNavMesh) agent.isStopped = true; }
        /// <summary>The HUD's line under her health.</summary>
        public string Status
        {
            get
            {
                switch (Now)
                {
                    case Stage.Waiting: return "Waiting in her cell";
                    case Stage.Walking: return Holding ? (Beset() ? "Beset: get them off her" : "Waiting for you") : "Walking to the Bell Tower " + Mathf.RoundToInt(Walked * 100) + "%";
                    case Stage.Fallen: return "Fallen: back in her cell soon";
                    default: return "Keeping the tower door";
                }
            }
        }
        void Update()
        {
            if (session == null || actor == null) return;
            if (talk != null) { talk.position = transform.position - Vector3.up; talk.hiddenUntil = Now == Stage.Walking || Now == Stage.Fallen ? float.MaxValue : 0; }
            if (!actor.IsAlive && Now != Stage.Fallen && Now != Stage.Arrived) { Fall(); return; }
            switch (Now)
            {
                case Stage.Walking:
                {
                    Holding = session.InCombat || Beset() || !PartyNear();
                    if (agent.isOnNavMesh) agent.isStopped = Holding;
                    Walked = Mathf.Max(Walked, Mathf.Clamp01(1 - Flat(goal - transform.position) / total));
                    if (Flat(goal - transform.position) < 2.5f && !agent.pathPending)
                    {
                        Now = Stage.Arrived; Walked = 1; Holding = false; if (agent.isOnNavMesh) agent.isStopped = true;
                        session.WardenArrived(this);
                    }
                    break;
                }
                case Stage.Fallen:
                    if (Time.time - fellAt > BackAfter) session.WardenBack(this);
                    break;
            }
        }
        /// <summary>Something is on her within reach: she stops and waits for it to be dealt with.</summary>
        bool Beset()
        {
            foreach (var e in session.Enemies)
                if (e != null && e.Victim == actor && e.actor.IsAlive && Flat(e.transform.position - transform.position) < 5) return true;
            return false;
        }
        /// <summary>Anyone of the party within <see cref="WaitFor"/> of her (you or a sim): she does not walk on alone.</summary>
        bool PartyNear()
        {
            foreach (var a in session.PartyActors()) if (a != null && a != actor && a.IsAlive && Flat(a.transform.position - transform.position) <= WaitFor) return true;
            return false;
        }
        void Fall()
        {
            Now = Stage.Fallen; fellAt = Time.time; Holding = false; if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            session.WardenFell(this);
        }
        public void Say(string line) { session.Message(EncounterSession.WardenName + ": \"" + line + "\""); }
        static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }
    }
}
