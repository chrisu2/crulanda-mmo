using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>A mob's spell (Docs/CC_DESIGN.md section 5, step C5, 2026-10-08; GAME-ONLY): something worth interrupting.</summary>
    public sealed class MobCast
    {
        public string mob, name, kind;   // kind: "heal" (an ally under 70%), "bolt" (its foe), "drain" (its foe, healing itself), "blast" (all near it)
        public float cast = 2.5f, every = 10, first = 4, power = 2.5f, reach = 25;
    }
    /// <summary>The casters, by camp mob name (the Sealed Adit's first; more as dungeons come).</summary>
    public static class MobCasts
    {
        public static readonly MobCast[] All = {
            new MobCast { mob = "Ash mender", name = "Ember Mend", kind = "heal", cast = 2.5f, every = 9, first = 3, power = .35f },
            new MobCast { mob = "Ash initiate", name = "Cinder Bolt", kind = "bolt", cast = 2f, every = 8, first = 4, power = 2.5f },
            new MobCast { mob = "Sandthrone sapper", name = "Short Fuse", kind = "blast", cast = 3f, every = 14, first = 5, power = 3f, reach = 4.5f },
            new MobCast { mob = "Hollow Man", name = "Grey Drain", kind = "drain", cast = 2.5f, every = 10, first = 4, power = 2f },
        };
        public static MobCast For(string mob) { return string.IsNullOrEmpty(mob) ? null : All.FirstOrDefault(c => c.mob == mob); }
    }

    /// <summary>
    /// Casting (step C5): a caster in a fight stops, casts for a couple of seconds (a cast bar on its nameplate and your target frame),
    /// then its spell lands. Any silence (Kick, Hush, Quench, Censure, Shield Bash, Pin), stun, hold or fear interrupts it, and a
    /// silenced mob can't start one. Mira hushes a cast she sees coming; party sims with an interrupt use it.
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        public MobCast Cast { get; set; }
        float castStart = -1, castEnd = -1, nextCastAt; EncounterEnemy castOn;
        public bool Casting { get { return castEnd >= 0; } }
        public float CastProgress { get { return Casting ? Mathf.Clamp01((Time.time - castStart) / Mathf.Max(.01f, castEnd - castStart)) : 0; } }
        public float CastRemaining { get { return Casting ? Mathf.Max(0, castEnd - Time.time) : 0; } }
        /// <summary>How many of its casts were interrupted (for tests and the log).</summary>
        public int Interrupts { get; private set; }

        void CancelCast(bool interrupted)
        {
            if (!Casting) return;
            castStart = castEnd = -1; castOn = null; nextCastAt = Time.time + (Cast != null ? Cast.every * .6f : 5);
            if (interrupted) { Interrupts++; session.FloatText(transform.position + Vector3.up * .6f, "Interrupted!", new Color(1, .9f, .5f)); }
        }
        void ResetCasting() { CancelCast(false); nextCastAt = 0; }
        /// <summary>Each frame in a fight: true while it casts (it stands still and doesn't swing).</summary>
        bool TickCast()
        {
            if (Cast == null || !actor.IsAlive) return false;
            if (Casting)
            {
                if (Controlled || Silenced) { CancelCast(true); return false; }
                if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.velocity = Vector3.zero; }
                if (Time.time < castEnd) return true;
                castStart = castEnd = -1; nextCastAt = Time.time + Cast.every; Land(); return true;
            }
            if (nextCastAt <= 0) { nextCastAt = Time.time + Cast.first; return false; }
            if (Time.time < nextCastAt || Silenced || Victim == null) return false;
            if (Cast.kind == "heal")
            {
                castOn = session.Enemies.Where(e => e != null && e.actor.IsAlive && e.Engaged && e.actor.Health.Pool.Ratio < .7f && Vector3.Distance(e.transform.position, transform.position) <= 20)
                    .OrderBy(e => e.actor.Health.Pool.Ratio).FirstOrDefault();
                if (castOn == null) { nextCastAt = Time.time + 1; return false; }
            }
            else if (Cast.kind != "blast" && Vector3.Distance(Victim.transform.position, transform.position) > Cast.reach) return false;
            castStart = Time.time; castEnd = Time.time + Cast.cast;
            session.FloatText(transform.position + Vector3.up * .9f, Cast.name, new Color(1, .62f, .2f));
            return true;
        }
        void Land()
        {
            switch (Cast.kind)
            {
                case "heal":
                    if (castOn != null && castOn.actor.IsAlive)
                    {
                        int healed = castOn.actor.Health.ApplyHealing(Mathf.RoundToInt(castOn.actor.Health.Pool.Max * Cast.power));
                        session.FloatText(castOn.transform.position, "+" + healed, new Color(.5f, 1, .5f));
                    }
                    break;
                case "bolt":
                case "drain":
                    if (Victim != null && Victim.IsAlive && Vector3.Distance(Victim.transform.position, transform.position) <= Cast.reach + 2)
                    {
                        int dealt = session.Kit.ResolveEnemyHit(this, Victim, Mathf.RoundToInt(HitBase * Cast.power * GroupDamageScale));
                        session.FloatText(Victim.transform.position, Cast.name + " " + dealt, new Color(1, .5f, .3f));
                        if (Cast.kind == "drain" && dealt > 0) actor.Health.ApplyHealing(dealt);
                    }
                    break;
                case "blast":
                    var hit = new HashSet<Actor>();
                    foreach (var who in new[] { session.Player, session.Companion != null ? session.Companion.actor : null }.Concat(session.PartySims.Where(c => c != null).Select(c => c.actor)))
                        if (who != null && who.IsAlive && hit.Add(who) && Vector3.Distance(who.transform.position, transform.position) <= Cast.reach)
                            session.Kit.ResolveEnemyHit(this, who, Mathf.RoundToInt(HitBase * Cast.power * GroupDamageScale));
                    session.FloatText(transform.position + Vector3.up, "BOOM", new Color(1, .45f, .2f));
                    break;
            }
        }
    }
}
