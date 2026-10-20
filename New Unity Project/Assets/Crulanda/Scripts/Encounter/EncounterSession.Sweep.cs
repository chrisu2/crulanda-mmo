using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Sweep (Docs/REWORK_LOWTOWNS_HALLOWS.md, Lowtowns pick 9; Docs/ZONE_LOWTOWNS.md "The Sweep"). CANON-EXPANDED:
    /// - the High City's brass horns give "a piercing, deep, two-tone wail" when the snatchers come (book1 ch.3:243);
    /// - a sparked girl backed into a Lowtowns alley with black smoke pooled at her ankles (ch.1:239-251), children "snatched from the dirty
    ///   alleys and the gutters" (ch.13:143);
    /// - Rule One of the Lowtowns, "If you see a Seeker, exist somewhere else", and Rule Two, "Never interfere" (ch.1:257-259);
    /// - the way out over the roofs: a fire escape (ch.1:261) and a rusted rain-spout (ch.1:297).
    /// The game's own (GAME-ONLY): the Council's hired warders do the sweep (conflict 1, Chris's yes; the Seekers are the Transit Hub's), and a
    /// Seeker is only glimpsed on a far roof, never fought; no Copper-Tithe (conflict 2: out). The child, Lissa, and Brother Aldo's soup-hall
    /// as the place she is hidden are GAME-ONLY.
    /// How it plays:
    /// - Come down Pell Row toward its dead west end, out of a fight, and the horns wail: the warders of the sweep (the camp whose after is
    ///   <see cref="SweepAfter"/>) come out at the lane's east end and walk it back and forth; a child hides by the rain-spout.
    /// - E on her: she takes your hand and follows you. E at the spout and she climbs with you; along the walks and the rope bridges she
    ///   follows on the navmesh; down the fire escape (E at its head, added here) or through the washing, and she comes down with you
    ///   (EncounterSession.Roofs.MoveParty). She comes with you on a climb or a drop while she is within <see cref="SweepWithYou"/>.
    /// - A living warder within <see cref="SweepSight"/> of her while she stands in the open lane sees her: the attempt fails, she slips
    ///   away into the alleys, and a little later the lane is quiet again and the sweep can come again. On the roofs nobody sees her.
    /// - Bring her within <see cref="SweepDeliverReach"/> of Brother Aldo and he takes her into the soup-hall: "lowtowns.sweep" is kept for
    ///   good (the quest "The Sweep" at Aldo pays the keepsake), and once a game day the Lowtowners pay coin and standing.
    /// No machines: horns, a lantern, a fire escape of iron and a rain-spout.
    /// </summary>
    public sealed partial class EncounterSession
    {
        public const string SweepKey = "lowtowns.sweep", SweepDayKey = "lowtowns.sweep.day:", SweepAfter = "sweep", SweepChildName = "Lissa",
            SweepKeeper = "Brother Aldo", SweepFaction = "lowtowners", EscapeDownName = "The fire escape, down";
        /// <summary>Where Brother Aldo stands in the zone's data (used when his figure is not found).</summary>
        public static readonly Vector2 SweepKeeperSpot = new Vector2(-51.5f, -133.5f);
        /// <summary>How near the child's hiding place you come before the horns sound; how near a warder must be to see her in the lane;
        /// how near you she must be to climb or drop with you; how near Aldo to be taken in; seconds before a failed sweep's lane is quiet
        /// again; seconds before another sweep after one is over. Tests shorten the waits.</summary>
        public static float SweepReach = 14, SweepSight = 8, SweepWithYou = 10, SweepDeliverReach = 5, SweepResetSeconds = 40, SweepAgainSeconds = 120;
        /// <summary>A sweep's pay, once a game day: crowns and standing with the Lowtowners.</summary>
        public const int SweepGold = 14, SweepStanding = 60;
        /// <summary>Above the lane's ground by more than this, the child is on the roofs, out of a warder's sight.</summary>
        public const float SweepRoofHeight = 3;
        public const string HornsLine = "From far above, out of the white towers of the High City, the brass horns: a piercing, deep, two-tone wail that cuts through the smog. All down Pell Row the shutters bang to. Somebody's mother hisses a name into the dark.";
        public const string SweepRulesLine = "Chalked on the wall by the rain-spout, in a child's hand: RULE ONE IF YOU SEE A SEEKER EXIST SOMEWHERE ELSE. Under it, older, scratched with a nail: RULE TWO NEVER INTERFERE.";
        public const string WardersLine = "Lanterns at the east end of the lane, and boots: the Council's hired warders, a paper held up in front of them. \"Sweep! Doors open, by order of the Council!\" They start down Pell Row, looking in every doorway.";
        public const string ChildFoundLine = "Behind the rain-barrels at the dead end of the row, a girl of nine or ten in ragged cloth, shivering, her hands pressed together as if to hide the light in them. Black smoke pools round her ankles. \"I didn't do anything,\" she whispers.";
        public const string ChildTakenLine = "\"Up,\" you tell her, and point at the rain-spout. She looks at the roofs, and then at you, and puts her cold hand in yours. Her name is Lissa. Keep her off the lane: up and over to Brother Aldo's soup-hall.";
        public const string ChildSeenLine = "\"There! The girl!\" A warder's lantern swings onto her. Lissa tears her hand out of yours and is gone, under a cart and into an alley too narrow for a grown man. They will not catch her tonight. Neither will you. The lane goes quiet a while later.";
        public const string ChildLostLine = "Lissa slips away into the alleys while nobody is looking. She will hide again when the horns next sound.";
        public const string ChildSafeLine = "Brother Aldo sees you coming and has the side door open before you reach it. \"In,\" he says, and the girl is through it and gone among the soup tables, one more child in a room of them. He looks at you over her head. \"I count them,\" he says. \"Tonight the count is right.\"";
        public const string SeekerLine = "On a roof far down the Rows, against the smog, a figure stands quite still and watches the lane: white plate, a smooth gold mask. A Seeker. Rule One: exist somewhere else. It does not come down. It never has to.";

        public enum SweepStage { Quiet, Hiding, Led, Ended }
        /// <summary>Where the sweep is now.</summary>
        public SweepStage SweepNow { get; private set; }
        /// <summary>The child (null while the lane is quiet), her E (find her and take her hand), where she hides, the warders out on the sweep,
        /// and the Seeker glimpsed on a far roof (a figure, nothing more).</summary>
        public Transform SweepChild { get { return sweepChild; } }
        public ZoneInteractable SweepChildThing { get { return sweepThing; } }
        public Vector3 SweepHide { get { return sweepHide; } }
        public IReadOnlyList<EncounterEnemy> SweepWarders { get { return sweepWarders; } }
        public Transform SeekerGlimpse { get { return seekerGlimpse; } }
        /// <summary>How the last sweep ended: true when she was brought to the soup-hall.</summary>
        public bool SweepDelivered { get; private set; }
        Transform sweepChild, seekerGlimpse; NavMeshAgent sweepAgent; ZoneInteractable sweepThing, escapeDown; Vector3 sweepHide; bool haveSweep;
        readonly List<EncounterEnemy> sweepWarders = new List<EncounterEnemy>();
        float sweepQuietAt, nextSweepPath; int sweepRuns; Coroutine seekerLine;

        /// <summary>On arrival (StartAdit): the lane quiet, the child's hiding place found by the rain-spout, and the fire escape's way down.</summary>
        void StartSweep()
        {
            SweepNow = SweepStage.Quiet; SweepDelivered = false; sweepChild = seekerGlimpse = null; sweepAgent = null; sweepThing = escapeDown = null;
            sweepWarders.Clear(); sweepQuietAt = 0; haveSweep = false; sweepRuns = 0; seekerLine = null;
            var roofs = Zone != null ? Zone.LowtownRoofs : null; if (roofs == null || roofs.legs == null) return;
            ZoneBuilder.RoofLeg spout = null, escape = null;
            foreach (var leg in roofs.legs) { if (leg.kind == "climb" && leg.name == ZoneBuilder.SpoutName) spout = leg; if (leg.kind == "climb" && leg.name == ZoneBuilder.EscapeName) escape = leg; }
            if (spout == null || SweepCamp() < 0) return;
            // She hides a few steps past the spout's foot, deeper into the dead end (away from the row's terraces).
            var west = spout.a - spout.b; west.y = 0; var along = Vector3.left;
            var hide = spout.a + along * 2.5f + (west.sqrMagnitude > 1e-4f ? west.normalized * .6f : Vector3.zero);
            if (!NavMesh.SamplePosition(hide, out var hit, 3, NavMesh.AllAreas) && !NavMesh.SamplePosition(spout.a, out hit, 3, NavMesh.AllAreas)) return;
            sweepHide = hit.position; haveSweep = true;
            // The fire escape goes down as well as up: E at its head on the walk (offered only at the walk's height, TickRoofs).
            bool downThere = false; foreach (var kv in roofs.things) if (kv.Value.kind == "down") downThere = true;
            if (escape != null && !downThere)
            {
                escapeDown = new ZoneInteractable { name = EscapeDownName, prompt = "Climb down the fire escape", kind = "roofclimb", position = escape.b };
                Zone.Interactables.Add(escapeDown);
                roofs.things[escapeDown] = new ZoneBuilder.RoofLeg { kind = "down", name = EscapeDownName, a = escape.b, b = escape.a, roof = escape.roof };
            }
        }
        /// <summary>The index of the sweep's warders' camp in the zone's data, or -1.</summary>
        int SweepCamp()
        {
            var camps = Zone != null && Zone.Zone != null ? Zone.Zone.camps : null; if (camps == null) return -1;
            for (int c = 0; c < camps.Length; c++) if (camps[c] != null && camps[c].after == SweepAfter) return c;
            return -1;
        }

        /// <summary>Every frame in the Lowtowns.</summary>
        void TickSweep()
        {
            if (!haveSweep || Player == null) return;
            var me = Player.transform.position;
            switch (SweepNow)
            {
                case SweepStage.Quiet:
                    if (Time.time >= sweepQuietAt && Player.IsAlive && !InCombat && Flat(me - sweepHide) < SweepReach && Mathf.Abs(me.y - sweepHide.y) < SweepRoofHeight) BeginSweep();
                    break;
                case SweepStage.Hiding:
                    // Walked off and left her: the sweep passes, and she stays hidden.
                    if (Flat(me - sweepHide) > 90) EndSweep(ChildLostLine, false);
                    break;
                case SweepStage.Led:
                    if (sweepChild == null) { EndSweep(ChildLostLine, false); break; }
                    if (!Player.IsAlive) { EndSweep(ChildLostLine, false); break; }
                    FollowYou();
                    if (SweepChildSeen() != null) { EndSweep(ChildSeenLine, false); break; }
                    if (Flat(sweepChild.position - SweepKeeperAt()) <= SweepDeliverReach && Mathf.Abs(sweepChild.position.y - SweepKeeperAt().y) < SweepRoofHeight) Delivered();
                    break;
                case SweepStage.Ended:
                    if (Time.time >= sweepQuietAt) ClearSweep();
                    break;
            }
        }

        /// <summary>The horns, the warders out, the child in her hiding place, the Seeker on a far roof.</summary>
        void BeginSweep()
        {
            ClearSweep();
            SweepNow = SweepStage.Hiding; SweepDelivered = false; sweepRuns++;
            Message(HornsLine);
            ShowToast("The horns", "A two-tone wail from the High City");
            Message(SweepRulesLine);
            // The child, crouched in her hiding place (a small villager's figure, cowering).
            var go = new GameObject(SweepChildName);
            go.transform.SetPositionAndRotation(sweepHide + Vector3.up * .66f, Quaternion.LookRotation(Vector3.right));
            var v = ActorVisual.Attach(go, ActorLook.Villager, 717 + sweepRuns, true); v.Pose = ActorPose.Cower;
            sweepChild = go.transform;
            sweepThing = new ZoneInteractable { name = SweepChildName, prompt = "Look behind the rain-barrels", kind = "sweepchild", position = sweepHide, root = sweepChild };
            Zone.Interactables.Add(sweepThing);
            // The warders, out of the lane's east end and walking it (their camp's route).
            int c = SweepCamp();
            if (c >= 0)
            {
                var mobs = SpawnCampMobs(c, new System.Random(Zone.Zone.seed * 31 + 11 + 7919 * (c + 1) + sweepRuns), Zone.Zone.id.Replace("zone.", ""));
                foreach (var e in mobs) { e.RespawnSeconds = 1e6f; sweepWarders.Add(e); }
                Message(WardersLine);
            }
            seekerGlimpse = SpawnSeekerGlimpse();
            if (seekerGlimpse != null) seekerLine = StartCoroutine(SeekerSeen());
        }
        /// <summary>E on her: found, then her hand taken; she follows you from now.</summary>
        bool UseSweepThing(ZoneInteractable i)
        {
            if (i == null || i.kind != "sweepchild") return false;
            if (i != sweepThing || SweepNow != SweepStage.Hiding || sweepChild == null) return true;
            Message(ChildFoundLine);
            Message(ChildTakenLine);
            var v = sweepChild.GetComponent<ActorVisual>(); if (v != null) v.Pose = ActorPose.None;
            var agent = sweepChild.gameObject.AddComponent<NavMeshAgent>();
            agent.baseOffset = .66f; agent.height = 1.3f; agent.radius = Mathf.Min(.3f, ZoneBuilder.NavAgentRadius); agent.speed = 5.2f; agent.acceleration = 20; agent.angularSpeed = 540;
            agent.stoppingDistance = 1.6f; agent.autoRepath = true; agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.areaMask &= ~(1 << EncounterNavigation.DropArea);
            if (NavMesh.SamplePosition(sweepHide, out var hit, 3, NavMesh.AllAreas)) agent.Warp(hit.position);
            sweepAgent = agent; i.hiddenUntil = float.MaxValue;
            SweepNow = SweepStage.Led; nextSweepPath = 0;
            return true;
        }
        /// <summary>She keeps at your heels (four times a second a fresh path to you, when more than a couple of strides off).</summary>
        void FollowYou()
        {
            if (sweepAgent == null || !sweepAgent.isOnNavMesh || Time.time < nextSweepPath) return;
            nextSweepPath = Time.time + .25f;
            var me = Player.transform.position;
            if (Flat(sweepChild.position - me) > 2.2f || Mathf.Abs(sweepChild.position.y - me.y) > 2) sweepAgent.SetDestination(me);
        }
        /// <summary>Whether she stands in the open lane (on its ground, not up on the roofs).</summary>
        public bool SweepChildInLane()
        {
            if (sweepChild == null || Zone == null) return false;
            var p = sweepChild.position; return p.y - .66f - Zone.HeightAt(p.x, p.z) < SweepRoofHeight;
        }
        /// <summary>The warder who sees her now (alive, within sight of her in the open lane, at her level), or null.</summary>
        public EncounterEnemy SweepChildSeen()
        {
            if (sweepChild == null || SweepNow != SweepStage.Led || !SweepChildInLane()) return null;
            var p = sweepChild.position;
            foreach (var e in sweepWarders)
                if (e != null && e.actor != null && e.actor.IsAlive && Flat(e.transform.position - p) < SweepSight && Mathf.Abs(e.transform.position.y - p.y) < SweepRoofHeight) return e;
            return null;
        }
        /// <summary>Whether she climbs or drops with you now (EncounterSession.Roofs.MoveParty): following you, and near.</summary>
        bool SweepChildWithYou()
        {
            return SweepNow == SweepStage.Led && sweepChild != null && sweepAgent != null && Player != null && Vector3.Distance(sweepChild.position, Player.transform.position) <= SweepWithYou;
        }
        /// <summary>Her place beside you after a climb or a drop: the nearest walkable point a stride behind you.</summary>
        void SweepChildTo(Vector3 at, Vector3 side)
        {
            if (sweepAgent == null || !sweepAgent.isOnNavMesh) return;
            if (NavMesh.SamplePosition(at - side * .9f, out var hit, 2.5f, NavMesh.AllAreas)) { sweepAgent.Warp(hit.position); sweepAgent.ResetPath(); nextSweepPath = 0; }
        }
        /// <summary>Where Brother Aldo stands now (his figure, else his place in the data, on the ground).</summary>
        public Vector3 SweepKeeperAt()
        {
            var life = VillageLife.Active; var aldo = life != null ? life.Find(SweepKeeper) : null;
            if (aldo != null) return aldo.transform.position;
            return new Vector3(SweepKeeperSpot.x, Zone.HeightAt(SweepKeeperSpot.x, SweepKeeperSpot.y) + .66f, SweepKeeperSpot.y);
        }
        /// <summary>She is safe in the soup-hall: kept for good, and paid once a game day.</summary>
        void Delivered()
        {
            SweepDelivered = true;
            Message(ChildSafeLine);
            if (!Progress.keys.Contains(SweepKey)) Progress.keys.Add(SweepKey);
            string today = SweepDayKey + Progress.days;
            if (!Progress.keys.Contains(today))
            {
                Progress.keys.RemoveAll(k => k.StartsWith(SweepDayKey, System.StringComparison.Ordinal));
                Progress.keys.Add(today);
                Progress.gold += SweepGold;
                if (Quests != null) Quests.ChangeStanding(SweepFaction, SweepStanding);
                FloatText(Player.transform.position, "+" + SweepGold + " crowns", new Color(1, .82f, .35f));
                Message("The Lowtowners hear of it by morning: a few crowns in a twist of cloth left on your step (" + SweepGold + "), and doors that open a little wider for you.");
            }
            if (sweepAgent != null && sweepAgent.isOnNavMesh && Zone.Anchors != null) sweepAgent.SetDestination(SweepKeeperAt());
            ReconcileQuests();
            EndSweep(null, true);
            Save(false);
        }
        /// <summary>Over: the child gone (into the soup-hall, or away into the alleys), the lane quiet again after a while.</summary>
        void EndSweep(string line, bool safe)
        {
            if (!string.IsNullOrEmpty(line)) Message(line);
            SweepNow = SweepStage.Ended;
            sweepQuietAt = Time.time + (safe ? 6 : SweepResetSeconds);
            if (sweepThing != null) sweepThing.hiddenUntil = float.MaxValue;
            if (!safe && sweepChild != null) { Destroy(sweepChild.gameObject); sweepChild = null; sweepAgent = null; }
        }
        /// <summary>The lane quiet: the child, the Seeker's figure and the warders who are not fighting gone; another sweep may come a while on.</summary>
        void ClearSweep()
        {
            bool after = SweepNow == SweepStage.Ended;
            if (sweepChild != null) Destroy(sweepChild.gameObject);
            sweepChild = null; sweepAgent = null;
            if (sweepThing != null && Zone != null) Zone.Interactables.Remove(sweepThing);
            sweepThing = null;
            if (seekerLine != null) { StopCoroutine(seekerLine); seekerLine = null; }
            if (seekerGlimpse != null) Destroy(seekerGlimpse.gameObject);
            seekerGlimpse = null;
            for (int k = sweepWarders.Count - 1; k >= 0; k--)
            {
                var e = sweepWarders[k];
                // One in a fight with you finishes it.
                if (e != null && e.actor != null && e.actor.IsAlive && e.FightingParty) continue;
                if (e != null) { if (Target == e) { Target = null; AutoAttack = false; } Enemies.Remove(e); Destroy(e.gameObject); }
                sweepWarders.RemoveAt(k);
            }
            SweepNow = SweepStage.Quiet;
            if (after) sweepQuietAt = Time.time + SweepAgainSeconds;
        }

        /// <summary>
        /// The Seeker glimpsed (conflict 1: never fought): a figure in white plate and a gold mask (ch.1:241) on the ridge of a tenement 45 to
        /// 90 m from the child's hiding place, not one of the roof route's, facing the lane. No collider, no agent, no actor: nothing to target.
        /// The look is PROVISIONAL (the plate-armoured figure) until the Seekers have a figure of their own (the Transit Hub's).
        /// </summary>
        Transform SpawnSeekerGlimpse()
        {
            var z = Zone != null ? Zone.Zone : null; if (z == null || z.props == null) return null;
            ZoneProp best = null; float bestD = float.MaxValue;
            foreach (var p in z.props)
            {
                if (p == null || p.kind != "tenement") continue;
                bool onRoute = false; foreach (var r in ZoneBuilder.RoofRow) if (Vector2.Distance(p.at, r) < 1) onRoute = true;
                if (onRoute) continue;
                float d = Vector2.Distance(p.at, new Vector2(sweepHide.x, sweepHide.z));
                // The nearest past 45 m: far enough to be a glimpse, near enough to be seen through the smog.
                if (d >= 45 && d <= 90 && d < bestD) { best = p; bestD = d; }
            }
            if (best == null) return null;
            var size = ZoneBuilder.LowtownSize(best);
            float ridge = Zone.HeightAt(best.at.x, best.at.y) + ZoneBuilder.TenementEaves(best.variant) + ZoneBuilder.TenementRoofRise(size.y);
            var go = new GameObject("A Seeker, far off");
            var at = new Vector3(best.at.x, ridge + 1, best.at.y);
            var look = sweepHide - at; look.y = 0;
            go.transform.SetPositionAndRotation(at, look.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(look) : Quaternion.identity);
            ActorVisual.Attach(go, ActorLook.Paladin, 901);
            return go.transform;
        }
        IEnumerator SeekerSeen()
        {
            yield return new WaitForSeconds(6);
            if (seekerGlimpse != null && (SweepNow == SweepStage.Hiding || SweepNow == SweepStage.Led)) Message(SeekerLine);
            seekerLine = null;
        }
    }
}
