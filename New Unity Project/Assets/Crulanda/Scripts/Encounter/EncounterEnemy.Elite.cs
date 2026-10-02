using UnityEngine;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    // A camp elite's moves (playtest note 2; EliteMove says what each is): the wound-up heavy blow, the enrage and the call.
    public sealed partial class EncounterEnemy
    {
        /// <summary>Its move (null for everything that is not a camp elite).</summary>
        public EliteMove Move { get; private set; }
        public bool Enraged { get; private set; }
        /// <summary>Drawing back for its heavy blow: it stands still and does not swing until the blow falls.</summary>
        public bool WindingUp { get { return blowAt >= 0; } }
        /// <summary>How far through the wind-up it is (0 to 1), and the seconds left.</summary>
        public float WindupProgress { get { return WindingUp ? Mathf.Clamp01((Time.time - blowStart) / Mathf.Max(.01f, blowAt - blowStart)) : 0; } }
        public float WindupRemaining { get { return WindingUp ? Mathf.Max(0, blowAt - Time.time) : 0; } }
        /// <summary>Seconds until it may next draw back (0 = at its next swing).</summary>
        public float NextBlowIn { get { return Move == null ? float.MaxValue : Mathf.Max(0, nextBlowAt - Time.time); } }
        /// <summary>Seconds between its swings: the content's interval, shorter once it is enraged.</summary>
        public float SwingInterval { get { return session.content.enemySwingInterval * (Enraged && Move != null ? Move.enrageHaste : 1); } }
        /// <summary>What its swing is multiplied by and what the party's blows on it are multiplied by, when it outlevels the player.</summary>
        float OverHitNow { get { return Move != null && session.Player != null ? OvermatchHit(actor.Level, session.Player.Level) : 1; } }
        float OverTakenNow { get { return Move != null && session.Player != null ? OvermatchTaken(actor.Level, session.Player.Level) : 1; } }
        float blowAt = -1, blowStart, nextBlowAt;
        bool rallied, leaning; Quaternion bodyRest; Vector3 calmScale;
        BlowMark mark;
        /// <summary>The mark on the ground under its heavy blow (null until it first draws back; shown only while it does).</summary>
        public BlowMark Mark { get { return mark; } }
        static readonly Color BlowColor = new Color(1, .62f, .15f), RageColor = new Color(1, .3f, .2f);

        /// <summary>Makes this camp mob an elite with a move (EncounterSession sets its health and hit by the move's tier).</summary>
        public void ArmElite(EliteMove move) { Move = move; }
        /// <summary>
        /// The elite's part of a frame, once it has a target. Enrages and calls at their health marks. Returns true while it
        /// is drawing back: the caller then neither chases nor swings.
        /// </summary>
        bool TickElite()
        {
            float ratio = actor.Health.Pool.Ratio;
            if (!Enraged && ratio <= Move.enrageAt) Enrage();
            if (!rallied && ratio <= Move.callAt) { rallied = true; if (!string.IsNullOrEmpty(Move.call)) session.Rally(this, Victim); }
            if (!WindingUp) return false;
            if (agent.isOnNavMesh) { agent.isStopped = true; agent.velocity = Vector3.zero; }
            // It turns with whoever it is fighting, and leans back as the blow gathers.
            var to = Victim.transform.position - transform.position; to.y = 0;
            if (to.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 360 * Time.deltaTime);
            if (leaning) { var body = transform.Find("Body"); if (body != null) body.localRotation = bodyRest * Quaternion.Euler(-18 * WindupProgress, 0, 0); }
            if (mark != null) mark.Show(transform.position - Vector3.up, Move.reach, WindupProgress);
            if (Time.time >= blowAt) ReleaseBlow();
            return true;
        }
        void BeginBlow()
        {
            blowStart = Time.time; blowAt = Time.time + Move.windup;
            var body = transform.Find("Body"); if (body != null) { bodyRest = body.localRotation; leaning = true; }
            if (mark == null) mark = BlowMark.Create();
            mark.Show(transform.position - Vector3.up, Move.reach, 0);
            session.FloatText(transform.position + Vector3.up * .5f, Move.name + "!", BlowColor);
            session.Message(Name + " draws back: " + Move.name + "! Step out of the mark" + (session.Warrior != null ? ", or raise Guard." : "."));
        }
        /// <summary>The blow falls on whoever it is fighting, if they are still inside the mark. Guard, barriers and armour all blunt it.</summary>
        void ReleaseBlow()
        {
            var v = Victim; float reach = Move.reach;
            CancelBlow();
            swing = Time.time + SwingInterval; nextBlowAt = Time.time + Move.every;
            if (v == null || !v.IsAlive) return;
            var gap = v.transform.position - transform.position;
            if (new Vector2(gap.x, gap.z).magnitude > reach || Mathf.Abs(gap.y) > 2.2f)
            {
                session.FloatText(transform.position, "Missed", new Color(.8f, .8f, .8f));
                session.Message(v == session.Player ? "You step clear of " + Move.name + "." : Move.name + " falls on empty ground.");
                return;
            }
            int damage = session.Kit.ResolveEnemyHit(this, v, Mathf.RoundToInt(HitBase * Move.blow * OverHitNow));
            session.FloatText(v.transform.position + Vector3.up * .3f, Move.name + " −" + damage, RageColor);
            session.Message(Name + "'s " + Move.name + " hits " + (v == session.Player ? "you" : v.DisplayName) + " for " + damage + ".");
        }
        /// <summary>Stops a wind-up without the blow (it fell, the fight reset, or the elite died).</summary>
        void CancelBlow()
        {
            if (blowAt < 0 && !leaning) return;
            blowAt = -1;
            if (leaning) { leaning = false; var body = transform.Find("Body"); if (body != null) body.localRotation = bodyRest; }
            if (mark != null) mark.Hide();
        }
        void Enrage()
        {
            Enraged = true; calmScale = transform.localScale; transform.localScale = calmScale * 1.06f;
            if (swing - Time.time > SwingInterval) swing = Time.time + SwingInterval;
            session.FloatText(transform.position + Vector3.up * .5f, "Enraged!", RageColor);
            session.Message(string.IsNullOrEmpty(Move.enrage) ? Name + " is enraged!" : Move.enrage);
        }
        /// <summary>The fight is over for it: no enrage, and it may call again next time.</summary>
        void Soothe()
        {
            rallied = false;
            if (!Enraged) return;
            Enraged = false; transform.localScale = calmScale;
        }
        void OnDestroy() { if (mark != null) Destroy(mark.gameObject); }
    }
}
