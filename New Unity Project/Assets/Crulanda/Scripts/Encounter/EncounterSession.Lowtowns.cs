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
            Bolt(t, kind == WeaveEaterRules.Harpoon ? BoneYellow : SaltWhite, kind == WeaveEaterRules.Harpoon ? .14f : .22f, kind == WeaveEaterRules.Harpoon);
            bool eater = WeaveEaterRules.IsWeaveEater(t);
            var (sting, hold, seconds) = WeaveEaterRules.Throw(kind, Player.Level, eater);
            t.Receive(sting, Player);   // the sting first: a blow breaks a hold, so the hold goes on after it
            bool held = false;
            if (hold == "stun") held = t.actor.IsAlive && t.Apply("stun", Player, seconds) == null;
            else if (hold == "root" && t.actor.IsAlive) { t.Root(seconds); held = true; FloatText(t.transform.position, "Pinned", new Color(.86f, .8f, .62f)); }
            Message(WeaveEaterRules.ThrowLine(kind, t.actor.DisplayName, eater, held));
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
