using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Hallow's Creek's dungeon objects (Docs/DUNGEON2_DESIGN.md sections 3 and 4; GAME-ONLY mechanics over a CANON place, book1 ch.15-21):
    /// - the paused townsfolk: stone figures where the builder found them (ZoneBuilder.PausedSpots), no agent, no collider, their first frame
    ///   frozen; the party never fights them (N5's look; the colour swing of the Waking Hour is still to come, S7);
    /// - the Warden's walk (E2): Registrar Pardew's death opens the Warden's cell; Warden Sigrid Elvet of the Preservationists walks from it to
    ///   the Bell Tower's door at her own pace (waiting while the party fights), the walk's waves come at her (each only while its boss stands,
    ///   ZoneWave.unless), and at the door she unbinds the Bureau's ward-lock, kept for good (Progress.keys "hallows.ward");
    /// - the Waking Hour (E3): the Examiner's bell at half his health starts it (a boss phase of kind "event"); when its last ringer is down
    ///   the loop snaps and he stands stone for five seconds;
    /// - the chasm's fog (N3): anyone who falls into it is set back on the last deck or ground they stood on, never under the world;
    /// - the hungry shadows (N4): standing in one slows you and wears at you (never the last of your health);
    /// - the wagon home (N7, ZoneRide): once the Picker-Master is down, the black-iron wagon takes the party to the milestone;
    /// - gates worked by hand (pen, bar, hidden door; the Adit's too): E opens them, from the right side, once whoever holds them is down.
    /// No machines (DUNGEON_DESIGN 0.05): mules, a bell rung by hand, a painted dial.
    /// </summary>
    public sealed partial class EncounterSession
    {
        public const string WardKey = "hallows.ward", WardenName = "Warden Sigrid Elvet", ExaminerName = "Bureau Examiner Thurstan Vowell",
            WardGate = "The ward-lock", WardenCell = "The Warden's cell", WalkEvent = "walk", WakingEvent = "waking", GlitchEvent = "glitch", WardenPapers = "item.hallows_wardens_papers";
        /// <summary>How fast you move over the ground you stand on (a hungry shadow slows you): 1 elsewhere.</summary>
        public float GroundSlow { get; private set; } = 1;
        /// <summary>The Warden walking to the tower (null until her cell opens this visit).</summary>
        public HallowsWarden Warden { get; private set; }
        /// <summary>Seconds the Examiner stands stone when the Waking Hour's loop snaps (tests read it).</summary>
        public const float SnapSeconds = 5;
        readonly List<GameObject> pausedFigures = new List<GameObject>();
        bool wardenFreed; Vector3 lastSafe; bool haveSafe; float nextHazard;

        /// <summary>On arrival (StartAdit): the paused stand where they stood, the wagon's mules in their traces, the ward-lock open if it was unbound.</summary>
        void StartHallows()
        {
            GroundSlow = 1; wardenFreed = false; Warden = null; haveSafe = false; pausedFigures.Clear();
            if (Zone == null) return;
            for (int k = 0; k < Zone.PausedSpots.Count; k++) { var s = Zone.PausedSpots[k]; pausedFigures.Add(PausedFigure(s.at, s.facing, s.pose, s.name, k)); }
            for (int k = 0; k < Zone.RideTeam.Count; k++)
            {
                var mule = new GameObject("Wagon mule").transform; mule.SetPositionAndRotation(Zone.RideTeam[k].at, Zone.RideTeam[k].facing);
                mule.gameObject.AddComponent<PitMule>().body = CritterBody.Build(mule, "donkey", .3f + k * .4f, 23 + k * 5);
                var box = mule.gameObject.AddComponent<BoxCollider>(); box.size = new Vector3(.55f, 1.15f, 1.6f); box.center = new Vector3(0, .575f, 0); mule.gameObject.layer = 2;
            }
            if (Progress != null && Progress.keys.Contains(WardKey)) { var g = Zone.Gates.Find(x => x != null && x.Title == WardGate); if (g != null) g.SetOpen(true, true); }
        }
        /// <summary>A paused townsperson: a villager's figure turned to stone and stopped in its first frame (no agent, no collider).</summary>
        GameObject PausedFigure(Vector3 feet, Quaternion facing, int pose, string name, int k)
        {
            var go = new GameObject(string.IsNullOrEmpty(name) ? "A paused townsperson" : name);
            go.transform.SetPositionAndRotation(feet + Vector3.up, facing);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; Destroy(body.GetComponent<Collider>()); body.transform.SetParent(go.transform, false);
            var v = ActorVisual.Attach(go, ActorLook.Villager, 300 + k * 7, false, pose == 1 ? "baker" : null);
            v.Pose = pose == 1 ? ActorPose.Work : pose == 2 ? ActorPose.Talk : pose == 3 ? ActorPose.Cower : ActorPose.None;
            go.AddComponent<PausedStone>();
            return go;
        }

        // ---------- each frame ----------
        void TickHallows()
        {
            if (Zone == null || Player == null) return;
            // The Warden's cell open (Pardew down, or the bar lifted): she comes out and the walk begins.
            if (!wardenFreed)
            {
                var cell = Zone.Gates.Find(g => g != null && g.Title == WardenCell);
                if (cell != null && cell.Open) FreeWarden(cell);
            }
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

        // ---------- the Warden's walk (E2) ----------
        void FreeWarden(ZoneGate cell)
        {
            wardenFreed = true;
            if (!Zone.Anchors.TryGetValue("the Warden's cell", out var from)) from = cell.transform.position;
            if (!Zone.Anchors.TryGetValue("the tower door", out var to)) return;
            var go = new GameObject(WardenName); go.transform.position = from + Vector3.up;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; Destroy(body.GetComponent<Collider>()); body.transform.SetParent(go.transform, false);
            ActorVisual.Attach(go, ActorLook.Villager, 61, false, "warden");
            Warden = go.AddComponent<HallowsWarden>(); Warden.Begin(this, to);
            Message("The bar drops. A woman in a Preservationist's grey steps out of the cell, thin and steady: Warden Sigrid Elvet. \"The tower,\" she says. \"The Bureau has a ward on its door, and I know its knots. Walk with me.\"");
            StartEvent(WalkEvent);
        }
        /// <summary>The Warden at the tower's door: the ward-lock unbound, for good.</summary>
        public void WardenArrived(HallowsWarden w)
        {
            var g = Zone != null ? Zone.Gates.Find(x => x != null && x.Title == WardGate) : null;
            if (g != null && !g.Open) g.SetOpen(true);
            if (Progress != null && !Progress.keys.Contains(WardKey)) Progress.keys.Add(WardKey);
            Message("Warden Elvet lays both hands on the black sunburst and says something under her breath, a list, it sounds like, of the town's dead. The ward lets go like a held breath, and the tower door stands open.");
            Zone.Interactables.Add(new ZoneInteractable { name = WardenName, prompt = "Talk to Warden Elvet", kind = "warden", position = w.transform.position - Vector3.up, root = w.transform });
            ReconcileQuests(); Save(false);
        }
        bool TalkToWarden(ZoneInteractable i)
        {
            Message("Warden Elvet: \"Go up. Stop the bell. I will keep the door, and the paused will keep me; they always have.\"");
            QuestUse(i); return true;
        }
        void StudyWard(ZoneInteractable i)
        {
            var g = i.gate; if (g != null && g.Open) return;
            Message("A black sunburst burned into the boards, and round it a ward of the Bureau's: knots of grey that slide away when you look at them. A Preservationist could unbind it. The Archive's cells are said to hold one.");
        }

        // ---------- the Waking Hour (E3) and the events' words ----------
        /// <summary>An event over (EncounterSession.Release.FinishEvent): the Waking Hour's loop snaps and the Examiner stands stone.</summary>
        void EventOver(string id)
        {
            if (id != WakingEvent) return;
            foreach (var e in Enemies)
            {
                if (e == null || !e.actor.IsAlive || e.MobName != ExaminerName) continue;
                e.StandStone(SnapSeconds);
                Message("The loop snaps. The colour drains out of the town like water out of a print, and the Examiner stops with his hand on the rope: stone, for a breath or five. Now.");
            }
        }
        /// <summary>The words of Hallow's Creek's events as they start, end or fall; null for any other event.</summary>
        static string HallowsEventLine(string id, string moment)
        {
            switch (id)
            {
                case GlitchEvent:
                    return moment == "start" ? "Somewhere in the bakery the shop bell gives one note. For a heartbeat the room is warm and smells of bread." :
                        moment == "end" ? "The bell is still. On the counter, two copper pennies, warm to the touch." : "The bakery goes grey again with nobody in it to hear the bell.";
                case WalkEvent:
                    return moment == "start" ? "Warden Elvet sets off across the square toward the Bell Tower, not fast, not slow, the way you walk in a town where everyone is watching." :
                        moment == "end" ? "Nothing more comes after the Warden. The way to the tower is clear." : "The Warden's walk falls silent; whatever came after her goes back into the grey.";
                case WakingEvent:
                    return moment == "start" ? "The Examiner hauls on the bell rope and the great bell speaks. Colour comes back into the town all at once, and the square below starts to move." :
                        moment == "end" ? "The last of the ringers stops on the stair, stone again, and the bell's note dies in the arches." : "The bell's note dies with nobody standing to hear it. The town goes grey again.";
            }
            return null;
        }
        /// <summary>Whether a mob of that name stands this visit (alive, of a camp): a boss down is not, nor a rare not here.</summary>
        bool MobStanding(string mob)
        {
            foreach (var e in Enemies) if (e != null && e.Camp && e.actor != null && e.actor.IsAlive && e.MobName == mob) return true;
            return false;
        }

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
                case "warden": return "Talk to Warden Elvet";
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
            if (g.FromFar && Vector3.Dot(Player.transform.position - g.transform.position, g.transform.forward) < 0) { Message("It is barred from the other side."); return true; }
            g.SetOpen(true);
            Message(g.Kind == "hiddendoor" ? "Your fingers find the edge of a door in the stone, and it swings in onto the dark." : g.Kind == "bar" ? "You lift the bar out of its brackets and swing the gate open." : "You lift the bar and the gate swings open.");
            return true;
        }
    }

    /// <summary>A paused townsperson (Hallow's Creek, N5): once the figure is built (two frames on), every part of it turned to grey stone and
    /// its motion stopped, so it stands in its first frame for good. No collider, no agent: nobody fights it.</summary>
    public sealed class PausedStone : MonoBehaviour
    {
        static Material stone;
        int frames;
        void LateUpdate()
        {
            if (++frames < 3) return;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (stone == null && r.sharedMaterial != null)
                {
                    stone = new Material(r.sharedMaterial) { name = "Paused stone", color = new Color(.63f, .62f, .61f) };
                    if (stone.HasProperty("_MainTex")) stone.mainTexture = null;
                    if (stone.HasProperty("_EmissionColor")) stone.SetColor("_EmissionColor", Color.black);
                }
                if (stone == null) continue;
                var mats = r.sharedMaterials; for (int k = 0; k < mats.Length; k++) mats[k] = stone; r.sharedMaterials = mats;
            }
            foreach (var a in GetComponentsInChildren<Animator>(true)) a.enabled = false;
            foreach (var a in GetComponentsInChildren<Animation>(true)) a.enabled = false;
            var v = GetComponent<ActorVisual>(); if (v != null) v.enabled = false;
            enabled = false;
        }
    }

    /// <summary>Warden Sigrid Elvet of the Preservationists (GAME-ONLY), walking from her cell in the Old Archive to the Bell Tower's door (E2):
    /// at her own pace, stopping while the party fights, and unbinding the ward-lock when she gets there. She is not fought and takes no harm
    /// (PROVISIONAL: the walk's waves come at the party, not at her).</summary>
    public sealed class HallowsWarden : MonoBehaviour
    {
        public const float Pace = 1.7f;
        EncounterSession session; Vector3 goal; NavMeshAgent agent; bool there;
        public bool Arrived { get { return there; } }
        public Vector3 Goal { get { return goal; } }
        public void Begin(EncounterSession s, Vector3 to)
        {
            session = s; goal = to;
            agent = gameObject.AddComponent<NavMeshAgent>(); agent.speed = Pace; agent.radius = .4f; agent.height = 2; agent.baseOffset = 1; agent.stoppingDistance = .5f;
            agent.areaMask &= ~(1 << EncounterNavigation.DropArea);
            if (NavMesh.SamplePosition(transform.position, out var hit, 4, NavMesh.AllAreas)) agent.Warp(hit.position);
            if (agent.isOnNavMesh) agent.SetDestination(goal);
        }
        void Update()
        {
            if (there || session == null || agent == null || !agent.isOnNavMesh) return;
            agent.isStopped = session.InCombat;   // she waits while the party fights
            var d = transform.position - Vector3.up - goal; d.y = 0;
            if (d.magnitude < 2.5f) { there = true; agent.isStopped = true; session.WardenArrived(this); }
        }
    }
}
