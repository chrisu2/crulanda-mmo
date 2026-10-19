using UnityEngine;
using Crulanda.Core;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The raid boss's rank (Docs/WORLD_BOSS_1.md 1.2 and 3.1; the Unraveller, the first): a rank above elite, the actor's
    /// ActorClassification.Boss. A skull on her plate and "Raid (10)" on her frame (EncounterHud). The game's level math is kept as built: she
    /// shows her own level (30), and the gap to whom she fights counts five levels at most, as an elite's (she hits x1.6 and takes x0.7 from
    /// a raid at 20). No group scaling: she is set for ten, fixed. No blow takes more than <see cref="MaxBlowShare"/> of a member's health,
    /// except the few blows meant to kill, which carry the cap-passing flag (<see cref="CapBlow"/>; her touch on the hunted, the swallowing's
    /// end, the feast: steps 4-5).
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        /// <summary>A raid boss (the actor's rank is Boss).</summary>
        public bool RaidBoss { get { return actor != null && actor.Classification == ActorClassification.Boss; } }
        /// <summary>The raid she is set for.</summary>
        public int RaidSize = Unraveller.RaidSize;
        /// <summary>The frame's rank words: "Raid (10)"; null for anything that is not a raid boss.</summary>
        public string RankNote { get { return RaidBoss ? RankWords(RaidSize) : null; } }
        public static string RankWords(int raidSize) { return "Raid (" + raidSize + ")"; }
        /// <summary>Whether a party's size raises it (GroupScale): everything but a game animal and a raid boss.</summary>
        public bool ScalesWithGroup { get { return !Game && !RaidBoss && !Brood; } }   // her brood are set for the raid as they are (tune-3: scaled for ten, a wave outlived its holds)
        /// <summary>Makes this a raid boss for <paramref name="raidSize"/>: its rank, its level shown as it is.</summary>
        public void MakeRaidBoss(int raidSize) { RaidSize = raidSize; actor.SetClassification(ActorClassification.Boss); UnscaleFromGroup(); }
        /// <summary>
        /// A blow on <paramref name="partyMember"/> (one of your party) capped at <see cref="MaxBlowShare"/> of its health (never under 1);
        /// a blow on anyone else, or one that <paramref name="passesCap"/> (a raid boss's blow meant to kill), lands whole.
        /// </summary>
        public static int CapBlow(int raw, float maxHealth, bool partyMember, bool passesCap)
        {
            if (!partyMember || passesCap) return raw;
            return Mathf.Min(raw, Mathf.Max(1, Mathf.RoundToInt(maxHealth * MaxBlowShare)));
        }
        /// <summary><see cref="CappedBlow(Actor, int)"/> with the cap-passing flag: true only for a raid boss's few blows meant to kill.</summary>
        public int CappedBlow(Actor victim, int raw, bool passesCap)
        {
            if (victim == null || victim.Health == null) return raw;
            return CapBlow(raw, victim.Health.Pool.Max, session.PartyActor(victim.EntityId.Value) != null, passesCap && RaidBoss);
        }
    }
}
