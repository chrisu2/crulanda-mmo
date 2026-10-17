using UnityEngine;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A party sim's damage, set from the balance band (2026-10-17; Chris, watching the level-17 Hallow's Creek run's meter: "still not
    /// balanced i dont think": you, the Judicator, 63.8 a second, the Rogue 42.2, the Ranger 36.7 and its wolf 13.9). GAME-ONLY.
    /// - A sim in the damage part sustains the pure damage band on a training dummy: 66-72 a second at level 15 (aimed at 69, the
    ///   harness's single-target band, BALANCE_REPORT.md), a hybrid class 90% of it (EncounterSession.HybridDpsShare). At other levels the
    ///   band grows as a normal mob's health does (EncounterEnemy.MobHealth), so a damage-dealer takes as long to kill a mob of its own level
    ///   at 11 as at 20. Before, its blow was 1.6 times a formula (12 + 4 a level in melee, 10 + 3.6 at range) that sat under the band at
    ///   every level: a Rogue 60 at 15 and a Mage 45 (a Ranger 63 only with its wolf).
    /// - A pet's bites (the Ranger's wolf, the Bonecaller's thrall) are part of its keeper's damage: they come off its own blows, so the
    ///   two together are in the band. The Unwriting's unwrite is counted the same way.
    /// - Its practice at the yard moves it about the band's middle, 4% either way.
    /// - The tank and the healer keep their blows (half the formula's): a tank's damage is modest, a healer's work is its mending.
    /// Where the run's time went (the same run): about a quarter of the fighting time the Rogue, the Ranger and the healer stood at your
    /// side while the tank alone fought the last mob of a pack, the one the Rogue had held under the moon (a moon or a cross stayed "leave
    /// it alone" after the hold ran out); and the boss's roots stopped the Rogue's blows as well as its feet. Both are fixed here.
    /// </summary>
    public sealed partial class SimCompanion
    {
        /// <summary>The pure damage band at level 15 on a training dummy (BalanceHarness: single target).</summary>
        public const float PureBandLow = 66, PureBandHigh = 72, PureBandMid = 69;
        /// <summary>How far its yard practice moves a damage sim about the band's middle: 4% either way.</summary>
        public const float PracticeSpread = .08f;
        /// <summary>The band at <paramref name="level"/> over the band at 15: as a normal mob's health grows.</summary>
        public static float BandScale(int level)
        {
            return (float)EncounterEnemy.MobHealth(Mathf.Max(1, level), false, false, false) / EncounterEnemy.MobHealth(15, false, false, false);
        }
        /// <summary>What a pure damage sim of <paramref name="level"/> sustains on a dummy, the middle of its band.</summary>
        public static float PureDps(int level) { return PureBandMid * BandScale(level); }
        /// <summary>What this sim in the damage part sustains on a dummy, with its pet: the band's middle, a hybrid's share, its practice.</summary>
        public float BandDps
        {
            get
            {
                float share = EncounterSession.Hybrid(sim.classId) ? EncounterSession.HybridDpsShare : 1;
                return PureDps(sim.level) * share * (1 + PracticeSpread * (Mathf.Clamp01(sim.practice) - .5f));
            }
        }
        /// <summary>Its pet's bites a second, or 0 (a sim Ranger's wolf; a Bonecaller's thrall).</summary>
        public float PetDps
        {
            get { return sim.classId == "class.ranger" || Bonecaller(sim) ? RangerPet.BiteAt(sim.level, 1) / RangerPet.BiteInterval : 0; }
        }
        /// <summary>Its blows a second on one foe: one each Interval, and the Unwriting's unwrite (a third of a blow every 2 s; five of the
        /// six land each 12 s, the sixth falling due as it is cast again: PathRotation).</summary>
        public float BlowsPerSecond { get { return 1 / Interval + (Unwriter(sim) ? 5 / 36f : 0); } }
        /// <summary>Its blow in the damage part, before its buffs: its share of the band after its pet's, over its blows a second.</summary>
        public float DamageBlow { get { return Mathf.Max(1, (BandDps - PetDps) / BlowsPerSecond); } }

        /// <summary>Faces its quarry and, when its blow is due, strikes or shoots it (or its area blow goes across the pack about it).
        /// <paramref name="say"/>: what it is doing shows (not while rooted: "Rooted" stays).</summary>
        void StrikeAt(EncounterEnemy quarry, bool say)
        {
            var face = quarry.transform.position - transform.position; face.y = 0;
            if (face.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), Time.deltaTime * 8);
            if (say) Activity = (Melee ? "Fighting " : "Shooting at ") + quarry.actor.DisplayName;
            if (Time.time < nextAct) return;
            nextAct = Time.time + Interval;
            // A pack about its quarry: this blow goes across them all (SimCompanion.Fight).
            if (Time.time >= nextArea && SwingWide(quarry) > 0) return;
            int dmg = Hit;
            if (Melee) visual?.Strike();
            else
            {
                visual?.CastRelease();
                bool arrow = Arrow;
                Bolt.Fire(Hand, quarry.transform, BoltColour, arrow ? .1f : .18f, arrow ? .22f : .28f, arrow);
            }
            quarry.Receive(dmg, actor);
            // A tank's blows hold attention: three of its full blows a swing on top of the half it lands, over the damage-dealers' 1.6 (2026-10-15).
            if (Role == PartyRole.Tank) quarry.threat.Add(actor.EntityId.Value, BaseHit * 3f);
        }

        /// <summary>
        /// The last of a fight (2026-10-17): when nothing free is still on the party, the nearest mob within 30 m that is held, or marked to
        /// be left alone (the moon, the cross), and on the party. Its mark comes off: the party kills it. Null while anything free is on the
        /// party (the held one waits its turn, as before) or when there is no such mob. Before, a mob under the moon stayed "leave it alone"
        /// after its hold ran out, and the tank (who pulls what is left of its camp) and you fought it alone while the others stood by.
        /// </summary>
        EncounterEnemy LeftForLast()
        {
            EncounterEnemy pick = null; float bestD = 30;
            foreach (var e in session.Enemies)
            {
                if (e == null || !e.actor.IsAlive || !e.FightingParty || e.Game || e.Hidden || e.Inside != null) continue;
                // Something free still fights the party: the held one waits.
                if (!Held(e) && !e.LeaveAlone) return null;
                float d = Vector3.Distance(e.transform.position, transform.position); if (d < bestD) { pick = e; bestD = d; }
            }
            if (pick != null && (pick.RaidMarked == RaidMark.Moon || pick.RaidMarked == RaidMark.Cross)) pick.RaidMarked = RaidMark.None;
            return pick;
        }
    }
}
