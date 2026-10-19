using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Combat;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The camps' tricks (MobTricks; REWORK_LOWTOWNS_HALLOWS.md Lowtowns pick 7 and Hallow's Creek pick 8, 2026-10-18): the warders' and
    /// the Pact's shield-lines, the Teeth's thrown slates, the hounds that circle, the dock-rats' boat-hook, the Grey-touched's flash, the
    /// cutters against the clock, the ringers' echoes, the crossbows on the wall and the clerks' "Struck from the record". Each shows
    /// before it lands (a line the first time in a fight, a float over the mob, a ring on the ground) and each has an answer.
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        public MobTrick Trick { get; set; }
        float trickNextAt, trickWindupEnd = -1, circleUntil = -1, roofUnseenSince = -1, lastSwingSeen;
        bool trickTold, shieldsLocked;
        Actor trickOn; Vector3 trickAt;
        /// <summary>Its shield is locked with another of its line (for tests and the HUD).</summary>
        public bool ShieldsLocked { get { return shieldsLocked; } }
        /// <summary>Winding a trick up (a hold or a silence stops it).</summary>
        public bool TrickWinding { get { return trickWindupEnd >= 0; } }
        /// <summary>A Pact spear's extra reach while its wall stands.</summary>
        float TrickReach { get { return Trick != null && Trick.kind == "shieldline" && shieldsLocked ? Trick.metres : 0; } }

        // The cutters' clock, shared by one camp's cutters for one fight.
        sealed class CutClock { public float left; public bool broke, told10, told5; public int frame = -1; }
        CutClock clock; bool shellBroke;

        // Rings on the ground that land later: a slate falling, an echo of a blow.
        sealed class Pending { public Vector3 at; public float when, radius, power, knock; public string name; public BlowMark mark; public float from; }
        readonly List<Pending> pending = new List<Pending>();
        readonly List<BlowMark> spareMarks = new List<BlowMark>();

        // Who is dazzled by a Grey-touched's flash, until when, and how likely their blows go wide (by entity id).
        static readonly Dictionary<string, (float until, float chance)> dazzled = new Dictionary<string, (float, float)>();
        /// <summary>Whether <paramref name="who"/> is dazzled now (their blows may go wide).</summary>
        public static bool Dazzled(Actor who) { return who != null && dazzled.TryGetValue(who.EntityId.Value, out var d) && Time.time < d.until; }

        void TellTrick()
        {
            if (trickTold || Trick == null || string.IsNullOrEmpty(Trick.line)) return;
            trickTold = true;
            // One line a kind each fight is enough: the camp mates of the same trick stay quiet.
            foreach (var e in session.Enemies) if (e != null && e != this && e.Trick == Trick && e.inFight) e.trickTold = true;
            session.Message(Trick.line);
        }

        /// <summary>Each frame, whatever holds it: the rings already on the ground still land.</summary>
        void TickTrickMarks()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var p = pending[i];
                if (p.mark != null) p.mark.Show(p.at, p.radius, Mathf.InverseLerp(p.from, p.when, Time.time));
                if (Time.time < p.when) continue;
                pending.RemoveAt(i); Spare(p.mark);
                int hits = 0;
                foreach (var who in session.PartyAround(p.at, p.radius + .4f))
                {
                    int dealt = session.Kit.ResolveEnemyHit(this, who, CappedBlow(who, Mathf.RoundToInt(HitBase * p.power * GroupDamageScale)));
                    session.FloatText(who.transform.position, p.name + " " + dealt, new Color(1, .55f, .35f));
                    if (p.knock > 0 && who.IsAlive) KnockFrom(who, p.at, p.knock);
                    hits++;
                }
                if (hits == 0) session.FloatText(p.at + Vector3.up * .3f, p.name + " misses", new Color(.8f, .8f, .8f));
            }
        }
        BlowMark TrickRing() { if (spareMarks.Count > 0) { var m = spareMarks[spareMarks.Count - 1]; spareMarks.RemoveAt(spareMarks.Count - 1); return m; } return BlowMark.Create(GroundKind.Harmful, 12, "A trick's ring"); }
        void Spare(BlowMark m) { if (m == null) return; m.Hide(); spareMarks.Add(m); }
        void Lay(Vector3 at, float delay, float radius, float power, float knock, string name)
        {
            pending.Add(new Pending { at = at, from = Time.time, when = Time.time + delay, radius = radius, power = power, knock = knock, name = name, mark = TrickRing() });
        }

        /// <summary>In a fight, after its cast: true while a trick holds it (it does not chase or swing).</summary>
        bool TickTrick()
        {
            if (Trick == null || !actor.IsAlive || Victim == null) return false;
            switch (Trick.kind)
            {
                case "shieldline": ShieldLine(); return false;
                case "clock": CutterClock(); return false;
                case "echo": Echo(); return false;
                case "circle": return Circle();
                case "roof": return Roof();
                case "slate": case "hook": case "flash": case "strike": return Windup();
                default: Debug.LogWarning("[MobTrick] " + Trick.mob + ": no such kind \"" + Trick.kind + "\" (MobTricks.Kinds)."); return false;
            }
        }

        // ---------- The shield-line (the warders; the Pact's with spears) ----------
        void ShieldLine()
        {
            bool was = shieldsLocked;
            shieldsLocked = !Controlled && session.Enemies.Any(e => e != null && e != this && e.Trick == Trick && e.actor.IsAlive && e.inFight && !e.Controlled
                && Vector3.Distance(e.transform.position, transform.position) <= MobTricks.LockGap);
            if (shieldsLocked && !was) { session.FloatText(transform.position + Vector3.up * .8f, "Shields locked", new Color(.75f, .85f, 1)); TellTrick(); }
            // They advance together, at the line's pace.
            if (shieldsLocked && agent != null) agent.speed *= MobTricks.MarchPace;
        }
        /// <summary>What reaches it of a blow (Receive): a locked shield turns a frontal one, a dazzled striker may miss.</summary>
        int TrickTaken(int damage, Actor source)
        {
            if (damage <= 0 || source == null) return damage;
            if (Dazzled(source) && dazzled.TryGetValue(source.EntityId.Value, out var d) && Random.value < d.chance)
            {
                session.FloatText(transform.position, "Miss (dazzled)", new Color(.85f, .85f, .95f));
                return 0;
            }
            if (Trick != null && Trick.kind == "shieldline")
            {
                bool frontal = MobTricks.Frontal(transform.forward, source.transform.position - transform.position);
                int taken = MobTricks.ShieldTaken(damage, shieldsLocked, frontal, Trick.share);
                if (taken < damage && Random.value < .25f) session.FloatText(transform.position + Vector3.up * .5f, "Shield", new Color(.75f, .85f, 1));
                return taken;
            }
            return damage;
        }

        // ---------- The cutters against the clock ----------
        void CutterClock()
        {
            if (clock == null)
            {
                clock = session.Enemies.Where(e => e != null && e != this && e.Trick == Trick && e.CampCenter == CampCenter && e.inFight && e.clock != null).Select(e => e.clock).FirstOrDefault();
                if (clock == null) { clock = new CutClock { left = Trick.seconds }; TellTrick(); session.FloatText(transform.position + Vector3.up, "The chisels bite", new Color(1, .8f, .4f)); }
            }
            if (clock.broke) { if (!shellBroke) BreakShell(); return; }
            // Once a frame for the camp, by whichever free cutter comes first (a held one does not reach here).
            if (clock.frame == Time.frameCount) return;
            clock.frame = Time.frameCount;
            clock.left = MobTricks.ClockLeft(clock.left, Time.deltaTime, Controlled ? 0 : 1);
            if (!clock.told10 && clock.left <= 10) { clock.told10 = true; session.FloatText(transform.position + Vector3.up, "The shell cracks (10 s)", new Color(1, .7f, .3f)); }
            if (!clock.told5 && clock.left <= 5) { clock.told5 = true; session.FloatText(transform.position + Vector3.up, "The shell splits (5 s)", new Color(1, .5f, .2f)); }
            if (clock.left <= 0 && !clock.broke)
            {
                clock.broke = true; BreakShell();
                session.Message("The shell breaks under the chisels, and the soul in it with it. The cutters turn on you, flushed with the work.");
            }
        }
        void BreakShell()
        {
            shellBroke = true;
            BlowScale *= 1 + Trick.share;
            session.FloatText(transform.position + Vector3.up, "The shell breaks", new Color(1, .4f, .25f));
        }

        // ---------- The ringers' echoes ----------
        void Echo()
        {
            // A swing landed since the last frame (its next swing moved on): its echo falls on the same spot 2 s later.
            if (swing == lastSwingSeen) return;
            lastSwingSeen = swing;
            if (Time.time < trickNextAt || Victim == null) return;
            trickNextAt = Time.time + Trick.every;
            TellTrick();
            Lay(Victim.transform.position - Vector3.up, Trick.windup, Trick.area, Trick.power, 0, Trick.name);
            session.FloatText(Victim.transform.position + Vector3.up * .3f, "An echo lingers", new Color(.8f, .8f, .85f));
        }

        // ---------- The hounds that circle ----------
        bool Circle()
        {
            var v = Victim;
            if (circleUntil < 0)
            {
                if (trickNextAt <= 0) { trickNextAt = Time.time + Trick.first; return false; }
                if (Time.time < trickNextAt || Rooted || Vector3.Distance(v.transform.position, transform.position) > Trick.reach) return false;
                circleUntil = Time.time + Trick.windup;
                TellTrick();
                session.FloatText(transform.position + Vector3.up * .6f, "Circles behind", new Color(1, .75f, .4f));
            }
            var behind = v.transform.position - Flat(v.transform.forward).normalized * 1.8f;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false; agent.speed *= 1.6f;
                if (NavMesh.SamplePosition(behind, out var hit, 1.5f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
            }
            bool there = Across(behind - transform.position) < 1.1f;
            if (!there && Time.time < circleUntil) return true;
            circleUntil = -1; trickNextAt = Time.time + Trick.every;
            if (MobTricks.Behind(v.transform.forward, transform.position - v.transform.position) && Across(v.transform.position - transform.position) < 3)
            {
                if (Figure != null) Figure.Strike();
                int dealt = session.Kit.ResolveEnemyHit(this, v, CappedBlow(v, Mathf.RoundToInt(HitBase * Trick.power * OverHitNow * GroupDamageScale * BlowScale)));
                session.FloatText(v.transform.position, Trick.name + " " + dealt, new Color(1, .45f, .3f));
                swing = Time.time + SwingInterval;
            }
            else session.FloatText(transform.position + Vector3.up * .5f, "Finds no back to bite", new Color(.8f, .8f, .8f));
            return false;
        }

        // ---------- The crossbows on the wall ----------
        bool Roof()
        {
            var v = Victim;
            float above = transform.position.y - v.transform.position.y;
            if (above < Trick.metres || Across(v.transform.position - transform.position) < 3 || Vector3.Distance(v.transform.position, transform.position) > Trick.reach) { roofUnseenSince = -1; trickWindupEnd = -1; return false; }
            if (!Sees(v.transform.position))
            {
                // Out of its sight a while: it comes down after you.
                if (roofUnseenSince < 0) roofUnseenSince = Time.time;
                trickWindupEnd = -1;
                return Time.time - roofUnseenSince < 3;
            }
            roofUnseenSince = -1;
            if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.velocity = Vector3.zero; }
            var to = Flat(v.transform.position - transform.position);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 360 * Time.deltaTime);
            TellTrick();
            if (trickWindupEnd < 0)
            {
                if (trickNextAt <= 0) trickNextAt = Time.time + Trick.first;
                if (Time.time < Mathf.Max(trickNextAt, swing)) return true;
                trickWindupEnd = Time.time + Trick.windup;
                session.FloatText(transform.position + Vector3.up * .8f, "Takes aim", new Color(1, .75f, .4f));
                return true;
            }
            if (Controlled || Silenced) { trickWindupEnd = -1; trickNextAt = Time.time + Trick.every; return false; }
            if (Time.time < trickWindupEnd) return true;
            trickWindupEnd = -1; trickNextAt = Time.time + Trick.every; swing = Time.time + SwingInterval; MarkBlow();
            int dealt = session.Kit.ResolveEnemyHit(this, v, CappedBlow(v, Mathf.RoundToInt(HitBase * Trick.power * OverHitNow * GroupDamageScale * BlowScale)));
            session.FloatText(v.transform.position, Trick.name + " " + dealt, new Color(1, .5f, .3f));
            return true;
        }

        // ---------- The wind-ups: the slate, the boat-hook, the flash, "Struck from the record" ----------
        bool Windup()
        {
            if (trickWindupEnd >= 0)
            {
                if (Controlled || Silenced)
                {
                    // A slate never thrown never lands: its ring goes with the throw.
                    if (Trick.kind == "slate") for (int i = pending.Count - 1; i >= 0; i--) if (pending[i].name == Trick.name && pending[i].from >= trickWindupEnd - Trick.windup - .01f) { Spare(pending[i].mark); pending.RemoveAt(i); }
                    trickWindupEnd = -1; trickNextAt = Time.time + Trick.every * .6f; Interrupts++;
                    session.FloatText(transform.position + Vector3.up * .6f, "Interrupted!", new Color(1, .9f, .5f));
                    return false;
                }
                if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.velocity = Vector3.zero; }
                var face = Flat((trickOn != null ? trickOn.transform.position : trickAt) - transform.position);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), 360 * Time.deltaTime);
                if (Time.time < trickWindupEnd) return true;
                trickWindupEnd = -1; trickNextAt = Time.time + Trick.every;
                LandTrick();
                return true;
            }
            if (trickNextAt <= 0) { trickNextAt = Time.time + Trick.first; return false; }
            if (Time.time < trickNextAt || Silenced || Casting) return false;
            trickOn = null;
            switch (Trick.kind)
            {
                case "slate":
                    // At whoever hangs back (the farthest of the party in its reach, past 5 m): the healer, the bow.
                    trickOn = session.PartyAround(transform.position, Trick.reach).Where(a => Vector3.Distance(a.transform.position, transform.position) > 5 && Sees(a.transform.position))
                        .OrderByDescending(a => Vector3.Distance(a.transform.position, transform.position)).FirstOrDefault();
                    if (trickOn == null) { trickNextAt = Time.time + 1; return false; }
                    trickAt = trickOn.transform.position - Vector3.up;
                    Lay(trickAt, Trick.windup, Trick.area, Trick.power, Trick.metres, Trick.name);
                    break;
                case "hook":
                    // Whoever stands off from it within the hook's reach (the farthest): dragged in to the barge's edge.
                    trickOn = session.PartyAround(transform.position, Trick.reach).Where(a => Across(a.transform.position - transform.position) > 3.5f && Sees(a.transform.position))
                        .OrderByDescending(a => Vector3.Distance(a.transform.position, transform.position)).FirstOrDefault();
                    if (trickOn == null) { trickNextAt = Time.time + 1; return false; }
                    break;
                case "flash":
                    if (Vector3.Distance(Victim.transform.position, transform.position) > Trick.reach) return false;
                    trickOn = Victim; break;
                case "strike":
                    trickOn = session.PartyAround(transform.position, Trick.reach).FirstOrDefault(a => HelpfulOn(a) != null);
                    if (trickOn == null) { trickNextAt = Time.time + 2; return false; }
                    break;
            }
            TellTrick();
            trickWindupEnd = Time.time + Trick.windup;
            string begins = Trick.kind == "slate" ? "Hefts a slate" : Trick.kind == "hook" ? "Swings the boat-hook" : Trick.kind == "flash" ? "Light gathers in its face" : "Reads you out of the record";
            session.FloatText(transform.position + Vector3.up * .9f, begins, new Color(1, .62f, .2f));
            return true;
        }
        void LandTrick()
        {
            switch (Trick.kind)
            {
                case "slate":
                    // The ring on the ground lands it (TickTrickMarks).
                    break;
                case "hook":
                    if (trickOn == null || !trickOn.IsAlive || Vector3.Distance(trickOn.transform.position, transform.position) > Trick.reach + 1.5f) { session.FloatText(transform.position + Vector3.up * .5f, Trick.name + " misses", new Color(.8f, .8f, .8f)); break; }
                    int dealt = session.Kit.ResolveEnemyHit(this, trickOn, CappedBlow(trickOn, Mathf.RoundToInt(HitBase * Trick.power * GroupDamageScale)));
                    session.FloatText(trickOn.transform.position, Trick.name + " " + dealt, new Color(1, .55f, .3f));
                    if (trickOn.IsAlive) Pull(trickOn, Trick.metres);
                    break;
                case "flash":
                    int blinded = 0;
                    foreach (var who in session.PartyAround(transform.position, Trick.reach))
                    {
                        // You (only you: the sims fight facing it) can turn your back to the flash.
                        if (who == session.Player && MobTricks.LookedAway(who.transform.forward, transform.position - who.transform.position)) { session.FloatText(who.transform.position, "Looked away", Color.white); continue; }
                        dazzled[who.EntityId.Value] = (Time.time + Trick.seconds, Trick.share);
                        if (dazzled.Count > 64) foreach (var k in dazzled.Where(x => x.Value.until < Time.time).Select(x => x.Key).ToList()) dazzled.Remove(k);
                        session.FloatText(who.transform.position, "Dazzled", new Color(.85f, .8f, 1));
                        if (who == session.Player) session.Message("Someone else's memory flashes through you: your blows may go wide for " + Trick.seconds.ToString("0.#") + " s.");
                        blinded++;
                    }
                    session.FloatText(transform.position + Vector3.up, Trick.name + (blinded > 0 ? "!" : ""), new Color(.85f, .75f, 1));
                    break;
                case "strike":
                    var c = trickOn != null && trickOn.IsAlive ? trickOn.GetComponent<Combatant>() : null;
                    string id = trickOn != null ? HelpfulOn(trickOn) : null;
                    if (c == null || id == null) { session.FloatText(transform.position + Vector3.up * .5f, "Nothing to strike", new Color(.8f, .8f, .8f)); break; }
                    c.Statuses.Remove(id);
                    string lost = session.StatusName(id);
                    session.FloatText(trickOn.transform.position, "Struck: " + lost, new Color(.7f, .7f, .75f));
                    if (trickOn == session.Player) session.Message(Name + " strikes " + lost + " from the record.");
                    break;
            }
        }
        /// <summary>The helpful working with the most time left on <paramref name="who"/> (null: none).</summary>
        string HelpfulOn(Actor who)
        {
            var c = who != null ? who.GetComponent<Combatant>() : null; if (c == null || c.Statuses == null) return null;
            string best = null; float most = 0;
            c.Statuses.Each(Time.time, (id, left) => { if (!MobTricks.Harmful(id, c.Statuses.MultiplierOf(id)) && left > most) { most = left; best = id; } });
            return best;
        }

        /// <summary>Drags <paramref name="who"/> toward it (a boat-hook), only along walkable ground on the same floor, stopping short of it.</summary>
        void Pull(Actor who, float metres)
        {
            var gap = Flat(transform.position - who.transform.position); float across = new Vector2(gap.x, gap.z).magnitude;
            metres = Mathf.Min(metres, across - 1.6f); if (metres < .3f) return;
            Shift(who, gap.normalized, metres, "Hooked");
        }
        /// <summary>Throws <paramref name="who"/> back from a point (a slate's fall), only along walkable ground on the same floor.</summary>
        void KnockFrom(Actor who, Vector3 from, float metres)
        {
            var dir = Flat(who.transform.position - from); if ((who.transform.position - from).sqrMagnitude < .04f) dir = Flat(who.transform.position - transform.position);
            Shift(who, dir.normalized, metres, "Knocked back");
        }
        void Shift(Actor who, Vector3 dir, float metres, string text)
        {
            if (who == null || !who.IsAlive || metres <= 0) return;
            // As Knock: from the floor under their feet, onto the same floor, never through a wall or off an edge.
            if (!NavMesh.SamplePosition(who.transform.position, out var from, 2.5f, NavMesh.AllAreas)) return;
            float lift = who.transform.position.y - from.position.y; if (lift < -.5f || lift > 1.6f) return;
            NavMeshHit hit;
            if (NavMesh.Raycast(from.position, from.position + dir * metres, out var edge, NavMesh.AllAreas)) { if ((edge.position - from.position).magnitude < .5f) return; hit = edge; }
            else if (!NavMesh.SamplePosition(from.position + dir * metres, out hit, .8f, NavMesh.AllAreas)) return;
            if (Mathf.Abs(hit.position.y - from.position.y) > 1f) return;
            var motor = who.GetComponent<AdventurerMotor>();
            if (motor != null) motor.Teleport(hit.position + Vector3.up * lift);
            else { var a = who.GetComponent<NavMeshAgent>(); if (a != null && a.isOnNavMesh) a.Warp(hit.position); }
            session.FloatText(who.transform.position, text, new Color(1, .8f, .5f));
        }

        /// <summary>A fight's end, a death or a respawn: the tricks start again from nothing.</summary>
        void ResetTrick()
        {
            trickNextAt = 0; trickWindupEnd = circleUntil = roofUnseenSince = -1; trickTold = shieldsLocked = false; trickOn = null;
            if (shellBroke && Trick != null) BlowScale /= 1 + Trick.share;
            shellBroke = false; clock = null;
            foreach (var p in pending) Spare(p.mark);
            pending.Clear();
        }
        void DestroyTrickMarks()
        {
            foreach (var p in pending) if (p.mark != null) Destroy(p.mark.gameObject);
            foreach (var m in spareMarks) if (m != null) Destroy(m.gameObject);
            pending.Clear(); spareMarks.Clear();
        }
    }
}
