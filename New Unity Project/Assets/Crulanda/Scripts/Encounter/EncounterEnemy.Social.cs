using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The mobs of one pull, linked: they leash from one place (the home of the mob the pull began with), and when the leash
    /// breaks they all heal and go home together. A mob that only gives up the chase (it cannot reach its target) leaves alone.
    /// </summary>
    public sealed class FightGroup
    {
        public Vector3 anchor;
        public readonly List<EncounterEnemy> members = new List<EncounterEnemy>();
        /// <summary>The group has gone home: anyone still on the way to join it stays where it is.</summary>
        public bool Broken;
    }

    // Social aggro (playtest note 3): the camp a mob belongs to, how it answers a packmate or a call, and the group it fights in.
    // EncounterSession.RaiseAlarm decides who hears; this is the mob's side of it.
    public sealed partial class EncounterEnemy
    {
        /// <summary>Its camp in the zone data (-1 for story enemies and game: they are never linked).</summary>
        public int CampIndex = -1;
        public SocialKind Social = SocialKind.Solitary;
        /// <summary>Its people (SocialAggro.Kin): a call carries to kin in a neighbouring camp.</summary>
        public string Kin;
        /// <summary>The camp's name for it ("Old Whitefoot"), without the nameplate's "(elite)".</summary>
        public string MobName;
        public string Name { get { return string.IsNullOrEmpty(MobName) ? actor.DisplayName : MobName; } }
        /// <summary>The linked pull it is part of (null when it fights alone or not at all).</summary>
        public FightGroup Group { get; private set; }
        /// <summary>Heard a call and is about to come (the short beat between the shout and the charge).</summary>
        public bool Answering { get { return joinAt >= 0; } }
        /// <summary>Going home after a broken leash: it notices nobody and answers no call until this passes (a blow still turns it).</summary>
        public bool Evading { get { return Time.time < evadeUntil; } }
        public Vector3 Home { get { return home; } }
        /// <summary>The least threat a mob that joins holds on whoever pulled (EncounterSession.JoinThreat raises it with Mira's heal).</summary>
        public const float JoinThreat = 30;
        public const float EvadeSeconds = 3;
        bool inFight, alarmed, struck;
        float evadeUntil, joinAt = -1;
        Actor joinFor; FightGroup joinGroup;

        /// <summary>Free to answer a packmate or a call: alive, standing, in no fight, not already answering and not on its way home. One lying
        /// in wait for a moment of the session's (Danner in the dark, D4), not in the grass, is not drawn out by a call.</summary>
        public bool CanAnswer { get { return actor != null && actor.IsAlive && !Controlled && !Game && isActiveAndEnabled && Victim == null && !inFight && joinAt < 0 && !Evading && !Bolting && !(Hidden && !Ambusher); } }
        /// <summary>
        /// Heard <paramref name="caller"/>: after <paramref name="delay"/> seconds (0 = at once) it joins the caller's group and
        /// comes for <paramref name="puller"/>. It raises no alarm of its own, so a pull does not run through a whole camp.
        /// </summary>
        public void Answer(EncounterEnemy caller, Actor puller, float delay)
        {
            if (!CanAnswer || caller == null || caller == this || puller == null) return;
            joinGroup = caller.OwnGroup(); joinFor = puller; joinAt = Time.time + Mathf.Max(0, delay);
            if (delay <= 0) Join();
        }
        FightGroup OwnGroup()
        {
            if (Group == null) { Group = new FightGroup { anchor = home }; Group.members.Add(this); }
            return Group;
        }
        void Join()
        {
            var g = joinGroup; var who = joinFor; joinAt = -1; joinGroup = null; joinFor = null;
            if (g == null || g.Broken || who == null || !who.IsAlive || !actor.IsAlive) return;
            if (Group == null) { Group = g; g.members.Add(this); }
            if (inFight) return;   // it found the fight by itself in the meantime: it only takes the group's leash
            if (Hidden) Unhide();
            threat.Add(who.EntityId.Value, session.JoinThreat);
            // An elite that is drawn in still brings its own guards (Engage raises its alarm); anyone else comes quietly.
            alarmed = !(Elite && Camp);
        }
        /// <summary>The first frame of a fight: an elite's heavy blow starts its count, and the camp hears of it once.</summary>
        void Engage()
        {
            inFight = true; ScaleToGroup();   // stronger for a group (EncounterEnemy.GroupScale)
            if (Move != null) nextBlowAt = Time.time + Move.first;
            if (!alarmed) { alarmed = true; session.RaiseAlarm(this, Victim, !struck); }
        }
        void LeaveGroup() { if (Group == null) return; Group.members.Remove(this); Group = null; }
        /// <summary>Tests and capture tools: stands it on the walkable ground at a point and makes that its home (idle mobs walk back to their home).</summary>
        public void Rehome(Vector3 point)
        {
            if (NavMesh.SamplePosition(point, out var hit, 3, NavMesh.AllAreas)) point = hit.position;
            if (agent != null && agent.isOnNavMesh) agent.Warp(point); else transform.position = point + Vector3.up;
            home = transform.position;
        }
        /// <summary>Everything a fight leaves on a mob, dropped: its group, a call it was answering, a blow half drawn, an enrage.</summary>
        void ClearFight()
        {
            LeaveGroup(); inFight = alarmed = struck = false; joinAt = -1; joinGroup = null; joinFor = null;
            CancelBlow(); Soothe();
        }
        /// <summary>This mob alone gives up: whole again, off home, and deaf to the fight for a few seconds.</summary>
        void GoHome() { ResetFight(); evadeUntil = Time.time + EvadeSeconds; }
        /// <summary>The leash broke: the whole linked group heals and goes home together.</summary>
        void Disengage()
        {
            var g = Group;
            if (g != null)
            {
                g.Broken = true; int others = 0;
                foreach (var m in g.members.ToArray()) if (m != null && m != this && m.actor.IsAlive) { m.GoHome(); others++; }
                if (others > 0) session.Message(Name + " and the rest break off and go back to their places.");
            }
            GoHome();
        }
    }
}
