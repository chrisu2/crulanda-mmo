using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Core;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    // Social aggro and elites (playtest notes 2 and 3), the session's side: which camp mobs hear one of theirs join a fight
    // (SocialAggro holds the rules, EncounterEnemy.Social the mob's side), who answers an elite's call, and the set-up of both
    // on each camp mob as it spawns. Nothing here is saved and nothing draws from the zone's random stream.
    public sealed partial class EncounterSession
    {
        List<int>[] guardCamps; Crulanda.World.ZoneDefinition guardCampsOf;
        NavMeshPath socialPath; float lastShoutAt = -99;
        /// <summary>
        /// Threat a mob that joins a fight holds on whoever pulled: three times what one of Mira's heals draws at this level. A
        /// heal's threat is shared out among the mobs in the fight (<see cref="HealThreat"/>), so the mobs of a pull stay on the
        /// puller while he works through them, and only a long fight with no blow landed on one lets her heals turn it.
        /// </summary>
        public float JoinThreat { get { return Mathf.Max(EncounterEnemy.JoinThreat, 1.5f * HealerCompanion.HealFor(content.healingAbility.power, Player != null ? Player.Level : 1)); } }
        /// <summary>
        /// A companion's heal draws threat: half of what it healed, shared out among the mobs in the fight (one mob takes all of
        /// it, as it always did; each of four takes a quarter). Given to each in full it turned every mob of a pull on the healer
        /// by her second heal, since the puller's blows land on one mob at a time.
        /// </summary>
        public void HealThreat(Actor healer, int healed)
        {
            if (healer == null || healed <= 0) return;
            int n = 0; foreach (var enemy in Enemies) if (enemy != null && enemy.FightingParty) n++;
            if (n == 0) return;
            foreach (var enemy in Enemies) if (enemy != null && enemy.FightingParty) enemy.threat.Add(healer.EntityId.Value, healed * .5f / n);
        }
        /// <summary>
        /// Mira's health as the save holds it. A save from before she grew with your level holds her old full health (her level-1
        /// health, whatever your level): that is read as whole, not as wounded. No format change: the field means what it did.
        /// </summary>
        int CompanionHealthFromSave()
        {
            int saved = Progress.companionHealth;
            return saved == Companion.BaseHealth ? Companion.actor.Health.Pool.Max : saved;
        }
        /// <summary>For each camp of this zone, the camps that guard it (SocialAggro.GuardCamps), worked out once a zone.</summary>
        List<int>[] GuardCamps
        {
            get
            {
                var zone = Zone != null ? Zone.Zone : null;
                if (guardCamps == null || guardCampsOf != zone) { guardCampsOf = zone; guardCamps = SocialAggro.GuardCamps(zone != null ? zone.camps : null); }
                return guardCamps;
            }
        }
        /// <summary>
        /// A camp mob's social set-up as it spawns: its camp, kind and kin, and, for the camp's elite, its move and its tier's
        /// health and hit (EncounterEnemy.MobHealth and MobHit give a named elite's; a dungeon's end boss has more of both).
        /// </summary>
        void ConfigureSocial(EncounterEnemy enemy, Crulanda.World.ZoneCamp camp, int campIndex, int level, bool beast)
        {
            enemy.CampIndex = campIndex; enemy.Social = SocialAggro.KindFor(camp); enemy.Kin = SocialAggro.Kin(camp.look); enemy.MobName = camp.mob;
            if (!enemy.Elite) return;
            var move = EliteMoves.For(camp.mob, beast); enemy.ArmElite(move);
            enemy.actor.Stats.SetBase(StatType.MaxHealth, Mathf.RoundToInt(EncounterEnemy.MobHealth(level, false, true, beast) * move.health));
            enemy.actor.Health.ApplyHealing(enemy.actor.Health.Pool.Max);
            enemy.HitBase = EncounterEnemy.MobHit(level, false, true) * move.hit;
        }
        /// <summary>True when <paramref name="e"/> stands guard for the elite <paramref name="lord"/>: its own camp, or a camp paired with it.</summary>
        public bool GuardOf(EncounterEnemy e, EncounterEnemy lord)
        {
            if (e == null || lord == null || !lord.Elite || lord.CampIndex < 0 || e.CampIndex < 0) return false;
            if (e.CampIndex == lord.CampIndex) return true;
            var g = GuardCamps; return lord.CampIndex < g.Length && g[lord.CampIndex] != null && g[lord.CampIndex].Contains(e.CampIndex);
        }
        /// <summary>Two camps that fight as one: the same camp, or an elite's camp and a camp that guards it.</summary>
        bool CampsLinked(int a, int b)
        {
            if (a < 0 || b < 0) return false; if (a == b) return true;
            var g = GuardCamps;
            return (a < g.Length && g[a] != null && g[a].Contains(b)) || (b < g.Length && g[b] != null && g[b].Contains(a));
        }
        /// <summary>
        /// Near enough to answer: within reach in a straight line, on much the same level (4 m, or half the reach on a long call
        /// across a hillside), and not a long walk round (the next gallery of a cave can be three metres through rock). When the
        /// navmesh cannot say, the straight line decides.
        /// </summary>
        bool SocialNear(EncounterEnemy from, EncounterEnemy to, float reach)
        {
            Vector3 a = from.transform.position, b = to.transform.position;
            if (Mathf.Abs(a.y - b.y) > Mathf.Max(SocialAggro.MaxClimb, reach * .5f) || Vector3.Distance(a, b) > reach) return false;
            if (!NavMesh.SamplePosition(a, out var ha, 2.5f, NavMesh.AllAreas) || !NavMesh.SamplePosition(b, out var hb, 2.5f, NavMesh.AllAreas)) return true;
            if (socialPath == null) socialPath = new NavMeshPath();
            if (!NavMesh.CalculatePath(ha.position, hb.position, NavMesh.AllAreas, socialPath)) return true;
            if (socialPath.status != NavMeshPathStatus.PathComplete) return false;
            var corners = socialPath.corners; float walk = 0;
            for (int i = 1; i < corners.Length; i++) walk += Vector3.Distance(corners[i - 1], corners[i]);
            return walk <= reach * 2 + 4;
        }
        /// <summary>
        /// A camp mob has joined a fight (it noticed the player, was hit, or sprang from the grass): those of its camp near it
        /// join too, by its kind. A pack comes at once. People call out (a line in the chat, a word over the caller) and those in
        /// earshot come after a beat, kin in a neighbouring camp among them. A solitary beast brings nobody. When the player was
        /// sneaking and the mob only noticed them (<paramref name="noticed"/>), the reach is short: the edge of a camp can be
        /// peeled. An elite's guards (its camp and the camps paired with it) always come, from further, however it was pulled.
        /// The elite itself does not come running for a guard or for kin unless the caller stands right beside it
        /// (<see cref="SocialAggro.LordReach"/>): a careful player clears the guards first and then fights the lord.
        /// </summary>
        public void RaiseAlarm(EncounterEnemy caller, Actor puller, bool noticed)
        {
            if (caller == null || puller == null || caller.CampIndex < 0 || Player == null) return;
            var motor = Player.GetComponent<AdventurerMotor>();
            bool lord = caller.Elite && caller.Camp;
            bool sneaking = GameAnimal.SneakingOverride ?? (motor != null && motor.Sneaking);   // the override is the tests' Ctrl
            float reach = SocialAggro.Reach(caller.Social, noticed && sneaking);
            // A party draws more of a camp (playtest note 91, "mobs still way too easy in group"): the call carries half as far again.
            if (PartySims.Count > 0 && !(noticed && sneaking)) reach *= SocialAggro.PartyReach;
            if (reach <= 0 && !lord) return;
            List<EncounterEnemy> heard = null;
            foreach (var e in Enemies)
            {
                if (e == null || e == caller || e.CampIndex < 0 || !e.CanAnswer) continue;
                if (!lord && e.Elite && e.Camp && Vector3.Distance(caller.transform.position, e.transform.position) > SocialAggro.LordReach) continue;
                float r = 0;
                if (lord && GuardOf(e, caller)) r = SocialAggro.GuardReach;
                else if (reach > 0 && e.Social != SocialKind.Solitary &&
                    (CampsLinked(e.CampIndex, caller.CampIndex) || (caller.Social == SocialKind.Call && e.Social == SocialKind.Call && e.Kin == caller.Kin))) r = reach;
                if (r <= 0 || !SocialNear(caller, e, r)) continue;
                if (heard == null) heard = new List<EncounterEnemy>();
                heard.Add(e);
            }
            if (heard == null) return;
            bool calls = caller.Social == SocialKind.Call;
            if (calls) Shout(caller);
            for (int i = 0; i < heard.Count; i++) heard[i].Answer(caller, puller, calls ? SocialAggro.CallBeat + SocialAggro.CallStagger * i : 0);
        }
        /// <summary>The caller's shout: a word over its head, and a line in the chat (one line at most every second and a half).</summary>
        void Shout(EncounterEnemy caller)
        {
            var (chat, over) = SocialAggro.Shout(caller.Kin, Time.frameCount + caller.CampIndex);
            FloatText(caller.transform.position + Vector3.up * .5f, over, new Color(1, .82f, .35f));
            if (Time.time - lastShoutAt < 1.5f) return;
            lastShoutAt = Time.time; Message(caller.Name + chat);
        }
        /// <summary>
        /// An elite's call (EliteMove.call, once a fight): its guards and its kin within the move's reach who are not yet in the
        /// fight come after a beat. Every guard who hears comes; of its other kin the nearest, and no more than the move's
        /// callMost when it sets one. Nothing is said when nobody can answer. Returns how many are coming.
        /// </summary>
        public int Rally(EncounterEnemy lord, Actor puller)
        {
            if (lord == null || lord.Move == null || puller == null || lord.CampIndex < 0) return 0;
            int n = 0; List<EncounterEnemy> kin = null;
            foreach (var e in Enemies)
            {
                if (e == null || e == lord || e.CampIndex < 0 || !e.CanAnswer) continue;
                bool guard = GuardOf(e, lord);
                if (!guard && (string.IsNullOrEmpty(lord.Kin) || e.Kin != lord.Kin)) continue;
                if (!SocialNear(lord, e, lord.Move.callReach)) continue;
                if (guard) { e.Answer(lord, puller, SocialAggro.CallBeat + SocialAggro.CallStagger * n); n++; }
                else { if (kin == null) kin = new List<EncounterEnemy>(); kin.Add(e); }
            }
            if (kin != null)
            {
                var at = lord.transform.position;
                kin.Sort((a, b) => (a.transform.position - at).sqrMagnitude.CompareTo((b.transform.position - at).sqrMagnitude));
                int most = lord.Move.callMost > 0 ? Mathf.Min(lord.Move.callMost, kin.Count) : kin.Count;
                for (int i = 0; i < most; i++) { kin[i].Answer(lord, puller, SocialAggro.CallBeat + SocialAggro.CallStagger * n); n++; }
            }
            if (n == 0) return 0;
            if (!string.IsNullOrEmpty(lord.Move.callShort)) FloatText(lord.transform.position + Vector3.up * .5f, lord.Move.callShort, new Color(1, .82f, .35f));
            Message(lord.Move.call);
            return n;
        }
    }
}
