using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Lowtowns' small mechanics in play (Docs/ZONE_LOWTOWNS.md section 4; Phase 9.1; WeaveEaterRules says the rules): salt and the bone
    /// harpoon thrown from the bags at the target, and the night camps of the Drain Courts brought up after dark and sent back down by day.
    /// Blackened steel is applied where a blow lands (EncounterEnemy.Receive).
    /// </summary>
    public sealed partial class EncounterSession
    {
        static readonly Color SaltWhite = new Color(.95f, .95f, .9f), BoneYellow = new Color(.86f, .8f, .62f);
        /// <summary>
        /// Throws one of a thrown consumable (salt, a bone harpoon) at the target: a living foe within WeaveEaterRules.ThrowRange. It stings
        /// (and so draws it), then on a Weave-Eater holds it: salt calcifies (a stun), a harpoon pins (a root). One is used either way.
        /// </summary>
        bool ThrowItem(ItemDef d)
        {
            string kind = WeaveEaterRules.ThrownKind(d); var t = Target;
            if (t == null || t.actor == null || !t.actor.IsAlive || t.Game) { Message(WeaveEaterRules.NoTargetLine); return false; }
            if (Distance(t) > WeaveEaterRules.ThrowRange) { Message("Too far to throw. Come within " + WeaveEaterRules.ThrowRange + " metres."); return false; }
            Inventory.Remove(Progress, d.id, 1);
            bool eater = WeaveEaterRules.IsWeaveEater(t), boss = t.RaidBoss;
            // The throw's rules, the same for a sim's (EncounterSession.UnravellerFight.ThrowAt): the sting, then the hold; on the Unraveller a
            // harpoon pins her (the shortening pin), salt counts toward freeing the swallowed; on her brood salt calcifies it to shatter.
            bool held = ThrowAt(t, kind, Player, Player.Level);
            if (kind == WeaveEaterRules.Harpoon && boss) HarpoonTaken(RaidGroup.You);
            if (!boss || kind == WeaveEaterRules.Harpoon) Message(WeaveEaterRules.ThrowLine(kind, t.actor.DisplayName, eater, held));
            return true;
        }
        float nightCheckAt;
        /// <summary>Twice a few seconds: a night camp's mobs come up when it is dark (WorldClock.IsNight) and go back down when it is not.</summary>
        void TickNight()
        {
            if (Time.time < nightCheckAt) return; nightCheckAt = Time.time + 2;
            bool night = Crulanda.World.WorldClock.IsNight;
            foreach (var e in Enemies) { if (e == null || !e.NightOnly) continue; if (night) e.ComeOut(); else e.GoDown(); }
        }
    }
}
