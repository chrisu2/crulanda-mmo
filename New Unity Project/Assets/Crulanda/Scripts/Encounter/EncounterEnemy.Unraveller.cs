using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Combat;
using Crulanda.Core;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Unraveller's own part of her fight (Docs/WORLD_BOSS_1.md 3.3-3.4; build steps 4 and 5; UnravellerRules says the rules,
    /// UnravellerTuning the numbers). The Weave-Eaters' book rules are hers, scaled (EncounterEnemy.WeaveEater reads her numbers: the stronger
    /// lighthouse, the 9 s blink of 12 m, the wards eaten off all within 10 m), and on top of them:
    /// - the blink's touch (ch.20:133): landing on one who is not a tank, 60% of their health and silenced 3 s;
    /// - the harpoons (ch.20:181-191): each pins her (no blink, no move) a second shorter than the last (12 s down to 7); two in at once and
    ///   she is hauled toward the river's middle and takes 10% more; a fresh harpoon in the Feeding breaks it ("Bone holds");
    /// - steel goes black, three stacks deep on her (ch.20:155);
    /// - Unravel (ch.20:137, "They feed on structure!"): each of her blows on her target a stack, armour down and damage taken up; seven and
    ///   the tank is erased;
    /// - the Feeding (ch.20:141-143): a 3 s channel; landed, it eats every good effect off everyone within 20 m, heals her and makes her blows
    ///   heavier; an interrupt makes her immune to the next for 10 s;
    /// - salt on her does nothing unless someone is swallowed (EncounterSession.UnravellerFight counts the three); the spat-out calcifying
    ///   leaves her taking 50% more for 4 s;
    /// - her brood (the Drain Courts' kind, rising from the river): salt calcifies one, and a calcified one shatters at three times the damage.
    /// The session runs the rest of the fight (phases, brood, unmade ground, Swallowed, the Unravelling, Hunted, the feast) each frame.
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        static UnravellerTuning T { get { return UnravellerTuning.Current; } }
        /// <summary>The harpoons in her this fight.</summary>
        public readonly PinTrack Pins = new PinTrack();
        /// <summary>Unravel's stacks on whom her blows have landed.</summary>
        public readonly UnravelStacks Unravel = new UnravelStacks();
        /// <summary>When the fight with the raid began (-1: none on).</summary>
        public float PullAt { get; set; } = -1;
        /// <summary>Seconds since the pull (0 when no fight is on).</summary>
        public float SincePull { get { return PullAt < 0 ? 0 : Time.time - PullAt; } }
        /// <summary>Her phase now (1-3; 0 out of a fight).</summary>
        public int RaidPhase { get; set; }
        /// <summary>How much heavier her blows have grown this fight (the Feeding's meals, the brood she ate): her blows are x(1 + this).</summary>
        public float Growth { get; private set; }
        /// <summary>Seconds left on her pin (0: free).</summary>
        public float PinLeft { get { return RaidBoss ? Pins.Left(Time.time) : Mathf.Max(0, pinnedUntil - Time.time); } }
        /// <summary>Two or more harpoons in her: she is being hauled.</summary>
        public bool Hauled { get { return RaidBoss && UnravellerRules.Hauled(Pins.In(Time.time), T); } }
        /// <summary>The last interrupt that took (-1: none this fight): the next within 10 s does not.</summary>
        float lastInterruptAt = -1;
        /// <summary>Her interrupt immunity is up (an interrupt within the last 10 s).</summary>
        public bool InterruptImmune { get { return RaidBoss && !UnravellerRules.InterruptTakes(Time.time, lastInterruptAt, T); } }
        /// <summary>She is channelling the Feeding.</summary>
        public bool Feeding { get { return Casting && Cast != null && Cast.kind == FeedKind; } }
        /// <summary>Calcified by salt until then: one of her brood (it shatters), or her having spat out the swallowed (she takes 50% more).</summary>
        public float CalcifiedUntil { get; private set; }
        public bool Calcified { get { return Time.time < CalcifiedUntil; } }
        /// <summary>One of her brood (spawned by EncounterSession.UnravellerFight): salt calcifies it and it shatters.</summary>
        public bool Brood { get; set; }
        /// <summary>Good effects her Feeding has eaten this fight, and how many Feedings landed and were stopped (the log and the tests).</summary>
        public int Eaten { get; private set; }
        public int FeedingsLanded { get; private set; }
        public int FeedingsStopped { get; private set; }
        public const string FeedKind = "feed", UnravelId = "debuff.unravel", UnravelName = "Unravel";

        /// <summary>The Feeding as her cast (MobCast "feed": the cast bar, the interrupts, the sims' kicks work on it as on any cast).</summary>
        public static MobCast FeedingCast(UnravellerTuning t)
        {
            return new MobCast { mob = Unraveller.Name, name = Unraveller.FeedingName, kind = FeedKind, cast = t.feedCast, every = t.feedEvery1, first = t.feedFirst, power = 0, reach = 45 };
        }
        /// <summary>Seconds between her swings in her fight (2.2 s; 0: the content's for everyone else).</summary>
        float RaidSwingEvery { get { return RaidBoss ? T.swingEvery : 0; } }

        // ---------- the harpoons and the salt ----------
        /// <summary>A bone harpoon from <paramref name="by"/> lands in her: pinned (no blink, no move) for the shortening pin, a Feeding broken.
        /// The seconds it pins. On anything else of the Weave-Eaters' kind, the book's 12 s anchor as before.</summary>
        public float Harpooned(Actor by)
        {
            if (!RaidBoss) { if (BookWeaveEater) Pin(WeaveEaterRules.PinSeconds); return WeaveEaterRules.PinSeconds; }
            float now = Time.time, s = Pins.Harpoon(now, T);
            pinnedUntil = Mathf.Max(pinnedUntil, Pins.PinnedUntil); rootUntil = Mathf.Max(rootUntil, Pins.PinnedUntil);
            session.FloatText(transform.position + Vector3.up * 1.2f, "Pinned " + s.ToString("0") + " s", new Color(.86f, .8f, .62f));
            if (Feeding) { CancelCast(true); FeedingsStopped++; session.FloatText(transform.position + Vector3.up * 2f, "Bone holds", new Color(.86f, .8f, .62f)); session.FeedingBroken(this, by, true); }
            if (Hauled) session.FloatText(transform.position + Vector3.up * .6f, "Hauled", new Color(.95f, .75f, .4f));
            return s;
        }
        /// <summary>Salt on her from <paramref name="by"/>: counted toward freeing the swallowed (nothing when nobody is). True when it freed them.</summary>
        public bool SaltedHer(Actor by) { return RaidBoss && session.SaltOnUnraveller(this, by); }
        /// <summary>She calcifies (three salts freed the swallowed): stunned and taking more for <paramref name="seconds"/>, whatever her holds this fight.</summary>
        public void CalcifyHer(float seconds)
        {
            CalcifiedUntil = Time.time + seconds; RawStun(seconds);
            session.FloatText(transform.position + Vector3.up * 1.6f, "Calcified", new Color(.97f, .97f, .92f));
        }
        /// <summary>One of her brood salted: calcified (the salt's stun has gone on), so the next blow shatters it.</summary>
        public void CalcifyBrood(float seconds) { CalcifiedUntil = Time.time + seconds; }
        /// <summary>A stun that is the fight's, not the party's hold (her calcifying, her missed meal): no once-a-fight count.</summary>
        public void RawStun(float seconds)
        {
            if (!actor.IsAlive || seconds <= 0) return;
            stunUntil = Mathf.Max(stunUntil, Time.time + seconds); CancelBlow(); if (Casting) { CancelCast(true); if (RaidBoss) FeedingsStopped++; } wasHeld = true;
        }
        /// <summary>Moved onto <paramref name="to"/> whatever her pin (the hunt: "pinned or not").</summary>
        public bool DisplaceTo(Vector3 to)
        {
            if (agent == null || !agent.isOnNavMesh || !NavMesh.SamplePosition(to, out var hit, 3, NavMesh.AllAreas)) return false;
            var from = transform.position; agent.Warp(hit.position);
            var dir = hit.position - from; dir.y = 0; if (dir.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(dir);
            session.FloatText(from + Vector3.up * .3f, "Displaces", new Color(.55f, .5f, .7f));
            return true;
        }
        /// <summary>Hauled a step (the harpoons' ropes, ch.20:189-191): <paramref name="metres"/> along <paramref name="dir"/>, never off the walkable ground.</summary>
        public void Haul(Vector3 dir, float metres)
        {
            if (agent == null || !agent.isOnNavMesh || metres <= 0 || dir.sqrMagnitude < .01f) return;
            dir.y = 0; agent.Move(dir.normalized * metres);
        }

        // ---------- her blows ----------
        /// <summary>The touch where her blink lands (ch.20:133): on one who is not a tank, 60% of their health and silenced.</summary>
        void RaidTouch(Actor v)
        {
            if (!RaidBoss || v == null || !v.IsAlive) return;
            var c = v.GetComponent<SimCompanion>(); bool tank = c != null ? c.Role == SimCompanion.PartyRole.Tank : v == session.Player && session.PlayerRole == SimCompanion.PartyRole.Tank;
            int dealt = UnravellerRules.TouchDamage(v.Health.Pool.Max, tank, T); if (dealt <= 0) return;
            dealt = CapBlow(dealt, v.Health.Pool.Max, true, false);
            v.Health.ApplyDamage(dealt);
            session.FloatText(v.transform.position + Vector3.up * .4f, "Erasing touch −" + dealt, new Color(.62f, .55f, .8f));
            session.SilenceMember(v, T.touchSilence, Unraveller.Name + "'s touch");
        }
        /// <summary>After her blow on <paramref name="v"/> (her swing): a stack of Unravel; seven and the tank is erased.</summary>
        void RaidSwung(Actor v)
        {
            if (!RaidBoss || v == null || !v.IsAlive) return;
            int n = Unravel.Add(v.EntityId.Value, Time.time, T);
            var c = v.GetComponent<Combatant>();
            if (c != null)
            {
                c.Statuses.Remove(UnravelId);
                c.Apply(new StatusEffectDefinition { id = UnravelId, name = UnravelName + " (" + n + ")", duration = T.unravelSeconds, incomingDamageMultiplier = UnravellerRules.UnravelTaken(n, T),
                    modifiers = new[] { new StatusModifierDefinition { stat = StatType.Armor, operation = ModifierOp.PercentAdd, value = UnravellerRules.UnravelArmour(n, T) } } });
            }
            if (UnravellerRules.Erased(n, T))
            {
                v.Health.ApplyDamage(v.Health.Pool.Current);
                session.FloatText(v.transform.position + Vector3.up * .5f, "Unravelled", new Color(.62f, .55f, .8f));
                session.Message(Unraveller.ErasedLine(v == session.Player ? "You" : v.DisplayName));
            }
            else if (n == T.swapAt && v == session.Player) session.RaidWarning(Unraveller.SwapWarning);
        }
        /// <summary>Unravel's stacks on <paramref name="a"/> now (0 for none).</summary>
        public int UnravelOn(Actor a) { return a == null ? 0 : Unravel.Count(a.EntityId.Value, Time.time); }

        /// <summary>A blow on her or her brood, as her fight takes it: hauled (+10%) or calcified with someone spat out (+50%) on her; on a
        /// calcified brood, it shatters (x3) and the salt is gone.</summary>
        int RaidTaken(int damage)
        {
            if (damage <= 0) return damage;
            if (RaidBoss) return Mathf.RoundToInt(damage * UnravellerRules.TakenScale(Hauled, Calcified, T));
            if (Brood && Calcified)
            {
                CalcifiedUntil = 0;
                session.FloatText(transform.position + Vector3.up * 1.4f, "Shattered", new Color(.97f, .97f, .92f));
                return UnravellerRules.ShatterDamage(damage, true, T);
            }
            return damage;
        }

        /// <summary>How far its body reaches out from its centre (0 for an ordinary mob): melee is measured from its edge, not its middle.
        /// Raid 2: the Rogue at 18 a second and the Scoundrel at 21, a fifth of their band: a blade's 2.4 m from her centre lay inside her
        /// ten-metre spider and inside the unmade ground she lays where she stands, so the melee were crowded out or stepping out all fight.</summary>
        public float BodyRadius;
        /// <summary>Metres from <paramref name="p"/> to its body's edge (its centre for an ordinary mob).</summary>
        public float EdgeDistance(Vector3 p) { return Mathf.Max(0, Vector3.Distance(p, transform.position) - BodyRadius); }

        // ---------- her damage numbers ----------
        int raidFloatSum; float raidFloatAt;
        /// <summary>Raid 2 (Chris's screenshot): ten members' blows floated over her as an unreadable column. On a raid boss your own blows
        /// float as ever; the rest of the raid's are added up and floated once each half second, smaller and to her side. True when it took
        /// the number (the caller floats nothing).</summary>
        bool RaidFloat(int actual, Actor source)
        {
            if (!RaidBoss) return false;
            if (source != null && source == session.Player) return false;
            raidFloatSum += actual;
            if (Time.time >= raidFloatAt)
            {
                raidFloatAt = Time.time + .5f;
                if (raidFloatSum > 0) session.FloatText(transform.position + transform.right * 2.5f + Vector3.up * 1.5f, raidFloatSum.ToString(), new Color(.75f, .7f, .55f, .8f));
                raidFloatSum = 0;
            }
            return true;
        }

        // ---------- the Feeding ----------
        /// <summary>The Feeding lands (ch.20:143): every good effect on everyone of the party within 20 m is eaten (their wards, their buffs);
        /// each heals her and makes her blows heavier for the fight.</summary>
        void FeedLand()
        {
            int eaten = 0;
            foreach (var a in session.PartyActors())
            {
                if (a == null || !a.IsAlive || Vector3.Distance(a.transform.position, transform.position) > T.feedReach) continue;
                var c = a.GetComponent<Combatant>(); if (c == null) continue;
                if (c.Barrier > 0) { c.ClearBarrier(); eaten++; }
                var good = new List<string>();
                c.Statuses.Each(Time.time, (id, left) => { if (!c.Statuses.IsHarmful(id)) good.Add(id); });
                foreach (var id in good) if (c.Statuses.Remove(id)) eaten++;
            }
            Eaten += eaten; FeedingsLanded++;
            if (eaten > 0)
            {
                actor.Health.ApplyHealing(Mathf.RoundToInt(actor.Health.Pool.Max * UnravellerRules.FeedHeal(eaten, T)));
                Grow(UnravellerRules.FeedGrow(eaten, T));
            }
            session.FloatText(transform.position + Vector3.up * 2, Unraveller.FeedingName + (eaten > 0 ? ": " + eaten + " eaten" : ""), new Color(.62f, .55f, .8f));
            session.FeedingLanded(this, eaten);
        }
        /// <summary>Her blows grow by <paramref name="share"/> for the rest of the fight.</summary>
        public void Grow(float share) { Growth += share; BlowScale = 1 + Growth; }
        /// <summary>An interrupt on her took: immune to the next for 10 s.</summary>
        void Interrupted() { lastInterruptAt = Time.time; FeedingsStopped++; session.FeedingBroken(this, null, false); }

        /// <summary>The fight over (won, wiped, reset): her fight's state forgotten, and the session's (brood, ground, the swallowed, the hunt).</summary>
        void ClearRaidFight()
        {
            if (!RaidBoss) return;
            Pins.Clear(); Unravel.Clear(); PullAt = -1; RaidPhase = 0; Growth = 0; BlowScale = 1; lastInterruptAt = -1; CalcifiedUntil = 0;
            Eaten = FeedingsLanded = FeedingsStopped = 0; pinnedUntil = 0;
            if (Cast != null) Cast.every = T.feedEvery1;
            session.EndUnravellerFight(this);
        }
    }
}
