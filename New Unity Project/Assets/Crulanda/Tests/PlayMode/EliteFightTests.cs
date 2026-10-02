#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// An elite's moves in the running game (playtest note 2, the owner's words: "elite was too easy for the loot obtain"), with
    /// Old Whitefoot stood on the stage (PullStage) and a player of its level who cannot die:
    /// - it draws back for its heavy blow (a mark on the ground, a line in the chat), stands still while it does, and the blow
    ///   lands on a player who stays in the mark and misses one who steps out;
    /// - Guard blunts the blow;
    /// - enraged at low health it swings faster, and a reset calms it;
    /// - its call brings its kin from beyond a pack's reach;
    /// - an elite above the player's level hits harder and is harder to hurt;
    /// - Mira's health follows the player's level.
    /// Not covered here: the cast bar under the target frame and the look of the mark (OnGUI and rendering: to be looked at).
    /// </summary>
    public class EliteFightTests : PullStage
    {
        /// <summary>Old Whitefoot alone on the stage, the player beside it at its level, and the first blow struck.</summary>
        static EncounterEnemy Begin(EncounterSession s, out int raw)
        {
            var e = Named(s, "Old Whitefoot");
            AtLevel(s, e.actor.Level); Sturdy(s);
            StandDown(s, e);
            Place(s, e, Stage(s));
            PutPlayer(s, e.transform.position, 2, 180);
            raw = Mathf.RoundToInt(e.HitBase * e.Move.blow);
            Assert.AreEqual(0, s.Player.Stats.GetRounded(Crulanda.Core.StatType.Armor), "A new character wears no armour: blows land whole.");
            e.Receive(1, s.Player);
            return e;
        }

        [UnityTest] public IEnumerator An_elite_winds_up_its_blow_and_stepping_away_avoids_it_while_standing_still_takes_it()
        {
            var s = Session(); var e = Begin(s, out int raw); var move = e.Move;
            Assert.AreEqual("Throat-Lunge", move.name);
            // The first blow, taken standing still.
            yield return Until(() => e.WindingUp, move.first + 2 * s.content.enemySwingInterval + 3);
            Assert.IsTrue(e.WindingUp, "It draws back for " + move.name + " a few seconds into the fight.");
            Assert.IsTrue(Said(s, "draws back") && Said(s, move.name), "The chat says so." + Chat(s));
            Assert.IsTrue(s.Floating.Exists(f => f.text == move.name + "!"), "And the move's name floats over it.");
            Assert.NotNull(e.Mark, "The mark is on the ground."); Assert.IsTrue(e.Mark.Showing); Assert.AreEqual(move.reach, e.Mark.Reach, 1e-4f, "It reaches as far as the blow.");
            Assert.Greater(e.WindupRemaining, 0); Assert.Less(e.WindupProgress, 1);
            var stood = e.transform.position; int before = s.Player.Health.Pool.Current; float began = Time.time, fill = e.Mark.Filled;
            yield return Wait(move.windup * .5f);
            Assert.IsTrue(e.WindingUp, "Half way through it is still drawing back."); Assert.Greater(e.Mark.Filled, fill, "The mark fills as the blow gathers.");
            Assert.AreEqual(before, s.Player.Health.Pool.Current, "It does not swing while it draws back.");
            yield return Until(() => !e.WindingUp, move.windup + 1);
            Assert.IsFalse(e.WindingUp, "The blow has fallen."); Assert.That(Time.time - began, Is.InRange(move.windup - .4f, move.windup + .4f), "After its wind-up.");
            Assert.Less(Flat(e.transform.position, stood), .3f, "It stood still while it drew back.");
            int taken = before - s.Player.Health.Pool.Current;
            Assert.AreEqual(raw, taken, "Standing in the mark, you take the whole blow (" + move.blow + " times its swing).");
            Assert.IsTrue(Said(s, "hits you for " + raw), "The chat says what it did." + Chat(s));
            Assert.IsFalse(e.Mark.Showing, "The mark is gone.");
            // The second blow, stepped out of.
            yield return Until(() => e.WindingUp, move.every + 2 * s.content.enemySwingInterval + 3);
            Assert.IsTrue(e.WindingUp, "It draws back again.");
            s.Messages.Clear();
            PutPlayer(s, e.transform.position, move.reach + 3, 180);
            before = s.Player.Health.Pool.Current; stood = e.transform.position;
            yield return Until(() => !e.WindingUp, move.windup + 1);
            Assert.IsFalse(e.WindingUp);
            Assert.AreEqual(before, s.Player.Health.Pool.Current, "Out of the mark, the blow misses you altogether.");
            Assert.IsTrue(Said(s, "You step clear of " + move.name), "The chat says you stepped clear." + Chat(s));
            Assert.Less(Flat(e.transform.position, stood), .3f, "It did not follow you while it drew back.");
            yield return Until(() => Flat(e.transform.position, s.Player.transform.position) < 3, 4);
            Assert.Less(Flat(e.transform.position, s.Player.transform.position), 3, "Then it comes on again.");
        }

        [UnityTest] public IEnumerator Guard_blunts_the_heavy_blow()
        {
            var s = Session(); var e = Begin(s, out int raw);
            Assert.IsNotNull(s.Warrior, "The Warrior is the class with Guard.");
            yield return Until(() => e.WindingUp, e.Move.first + 2 * s.content.enemySwingInterval + 3);
            Assert.IsTrue(e.WindingUp);
            Assert.IsTrue(s.UseAbility(2), "Guard goes up as it draws back.");
            int before = s.Player.Health.Pool.Current;
            yield return Until(() => !e.WindingUp, e.Move.windup + 1);
            int taken = before - s.Player.Health.Pool.Current;
            float guard = s.content.FindStatus("status.guard").incomingDamageMultiplier;
            Assert.AreEqual(Mathf.RoundToInt(raw * guard), taken, 1, "Guard takes the blow at " + guard + " of its weight.");
            Assert.Greater(taken, 0); Assert.Less(taken, raw / 2);
        }

        [UnityTest] public IEnumerator An_enraged_elite_swings_faster_and_a_reset_calms_it()
        {
            var s = Session(); var e = Begin(s, out _); var move = e.Move; float calm = s.content.enemySwingInterval;
            yield return Until(() => e.Engaged, 1);
            Assert.IsFalse(e.Enraged); Assert.AreEqual(calm, e.SwingInterval, 1e-4f, "Calm, it swings like any mob.");
            var size = e.transform.localScale;
            s.Messages.Clear();
            e.actor.Health.ApplyDamage(Mathf.RoundToInt(e.actor.Health.Pool.Max * (1 - move.enrageAt + .05f)));
            Assert.LessOrEqual(e.actor.Health.Pool.Ratio, move.enrageAt);
            yield return Until(() => e.Enraged, 1);
            Assert.IsTrue(e.Enraged, "Under " + move.enrageAt * 100 + "% of its health it enrages.");
            Assert.IsTrue(Said(s, move.enrage), "The chat says so in its own words." + Chat(s));
            Assert.AreEqual(calm * move.enrageHaste, e.SwingInterval, 1e-4f); Assert.Less(e.SwingInterval, calm - .5f);
            Assert.Greater(e.transform.localScale.x, size.x, "It looms a little larger.");
            // Its swings, timed after its first heavy blow (there are several before the next): two in a row land closer together than a calm mob's.
            yield return Until(() => e.WindingUp, move.first + 2 * calm + 3);
            yield return Until(() => !e.WindingUp, move.windup + 1);
            Assert.IsFalse(e.WindingUp, "Its first heavy blow has fallen.");
            var swings = new List<float>(); float last = e.NextSwingIn;
            for (float t = 0; t < 6 && swings.Count < 2 && !e.WindingUp; t += Time.deltaTime)
            {
                yield return null;
                float next = e.NextSwingIn; if (next > last + .5f) swings.Add(Time.time); last = next;
            }
            Assert.AreEqual(2, swings.Count, "Two swings before its next heavy blow.");
            Assert.That(swings[1] - swings[0], Is.InRange(e.SwingInterval - .3f, e.SwingInterval + .3f), "Enraged, its swings come " + e.SwingInterval.ToString("0.00") + " s apart, not " + calm + ".");
            e.ResetFight();
            Assert.IsFalse(e.Enraged, "A reset calms it."); Assert.AreEqual(size.x, e.transform.localScale.x, 1e-4f); Assert.AreEqual(e.actor.Health.Pool.Max, e.actor.Health.Pool.Current, "And makes it whole.");
        }

        [UnityTest] public IEnumerator An_elites_call_brings_its_kin_from_beyond_a_packs_reach()
        {
            var s = Session(); var e = Named(s, "Old Whitefoot");
            var wolves = CampMobs(s, c => c.look == "wolf" && !c.ambush, 2);
            EncounterEnemy a = wolves[0], b = wolves[1];
            Assert.AreEqual(e.Kin, a.Kin, "Wolves are Whitefoot's kin."); Assert.AreNotEqual(e.CampIndex, a.CampIndex);
            AtLevel(s, e.actor.Level); Sturdy(s);
            StandDown(s, e, a, b);
            Place(s, e, Stage(s)); var mid = e.transform.position;
            PlaceNear(s, a, mid, 20, 0, 90); PlaceNear(s, b, mid, 22, 0, 90, 45);
            Assert.Greater(Vector3.Distance(mid, a.transform.position), SocialAggro.GuardReach + 2); Assert.Less(Vector3.Distance(mid, b.transform.position), e.Move.callReach - 2);
            PutPlayer(s, mid, 2, 180);
            e.Receive(1, s.Player);
            yield return Until(() => e.Engaged, 1);
            yield return Wait(1);
            Assert.IsFalse(a.Engaged || b.Engaged || a.Answering || b.Answering, "At the pull, the pack 20 m off hears nothing.");
            s.Messages.Clear();
            e.actor.Health.ApplyDamage(Mathf.RoundToInt(e.actor.Health.Pool.Max * (1 - e.Move.callAt + .05f)));
            yield return Until(() => a.Answering || a.Engaged, 1);
            Assert.IsTrue(Said(s, e.Move.call), "At " + e.Move.callAt * 100 + "% it calls, in the chat." + Chat(s));
            Assert.IsTrue(a.Answering || a.Engaged, "The pack has heard.");
            yield return Until(() => a.Engaged && b.Engaged, SocialAggro.CallBeat + 2);
            Assert.IsTrue(a.Engaged && b.Engaged, "And comes."); Assert.AreSame(s.Player, a.Victim); Assert.AreSame(e.Group, a.Group, "They fight as its group, held by its leash.");
            // They came 20 m from their own homes and are not sent back for it.
            yield return Until(() => Flat(a.transform.position, s.Player.transform.position) < 4, 8);
            Assert.Less(Flat(a.transform.position, s.Player.transform.position), 4, "The wolf reaches you."); Assert.IsTrue(a.Engaged, "And is still in the fight.");
            int calls = s.Messages.FindAll(m => m == e.Move.call).Count;
            Assert.AreEqual(1, calls, "It calls once a fight.");
        }

        [UnityTest] public IEnumerator An_elite_above_your_level_hits_harder_and_is_harder_to_hurt()
        {
            var s = Session(); var e = Named(s, "Old Whitefoot"); int level = e.actor.Level;
            Assert.GreaterOrEqual(level, 3, "Whitefoot is level 3 in a level 1-2 zone.");
            Sturdy(s); StandDown(s, e); Place(s, e, Stage(s));
            // At its level a blow of 100 costs it 100; two levels below, less.
            AtLevel(s, level); Sturdy(s);
            int before = e.actor.Health.Pool.Current; e.Receive(100, s.Player);
            Assert.AreEqual(100, before - e.actor.Health.Pool.Current);
            AtLevel(s, level - 2); Sturdy(s);
            before = e.actor.Health.Pool.Current; e.Receive(100, s.Player);
            Assert.AreEqual(Mathf.RoundToInt(100 * EncounterEnemy.OvermatchTaken(level, level - 2)), before - e.actor.Health.Pool.Current, "Two levels below it, your blows count for less.");
            // And its swing is heavier.
            PutPlayer(s, e.transform.position, 2, 180);
            int health = s.Player.Health.Pool.Current;
            yield return Until(() => s.Player.Health.Pool.Current < health, 3);
            Assert.AreEqual(Mathf.RoundToInt(e.HitBase * EncounterEnemy.OvermatchHit(level, level - 2)), health - s.Player.Health.Pool.Current, "Its swing lands " + EncounterEnemy.OvermatchHit(level, level - 2) + " times as hard.");
            // A normal mob never does this.
            var wolf = CampMobs(s, c => c.look == "wolf" && !c.ambush, 1)[0];
            before = wolf.actor.Health.Pool.Current; AtLevel(s, 1); wolf.Receive(10, s.Player);
            Assert.AreEqual(10, before - wolf.actor.Health.Pool.Current, "Normal mobs are as they were.");
        }

        [UnityTest] public IEnumerator Miras_health_follows_your_level()
        {
            var s = Session(); int baseHealth = s.Companion.actor.Health.Pool.Max;
            Assert.AreEqual(1, s.Progress.Level);
            s.Progress.experience = EncounterProgress.XpForLevel(8);
            s.Save(false); s.Load();
            yield return null; yield return null;
            Assert.AreEqual(8, s.Progress.Level, "The save came back at level 8.");
            Assert.AreEqual(baseHealth + 7 * HealerCompanion.HealthPerLevel, s.Companion.actor.Health.Pool.Max, "Mira has " + HealerCompanion.HealthPerLevel + " more health for each of your levels.");
            Assert.Greater(HealerCompanion.HealFor(s.content.healingAbility.power, 8), s.content.healingAbility.power * 2, "And her heal has more than doubled.");
        }
    }
}
#endif
