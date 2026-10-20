using UnityEngine;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The roofs over Pell Row in play (Docs/ZONE_LOWTOWNS.md "The roofs"; Lowtowns pick 3; the builder is ZoneBuilder.Roofs).
    /// - Up: E at the rusted rain-spout (book1 ch.1:297) or the fire escape (ch.1:261) climbs onto the lead walk over the gutter, the party
    ///   and Mira with you.
    /// - Along: the walks and the rope bridges are in the navmesh; you and the party walk them.
    /// - Down: E on the walk where washing hangs below drops you through it into the lane (ch.1:225). The laundry takes the fall: no harm.
    /// - The water tower (ch.7:41): E climbs it for the view.
    /// Each is offered only at its own height (the prompt finds things by distance in plan, so the lane's climb would show on the roof).
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>How long a climb up takes, in seconds.</summary>
        public const float RoofClimbSeconds = 3;
        /// <summary>A drop through the washing does no harm (GAME-ONLY: the laundry breaks the fall).</summary>
        public const int LaundryDropDamage = 0;
        public const string SpoutLine = "You get your fingers round the rusted spout and climb, the brackets creaking, rust flaking into your eyes, and roll over the edge onto the lead of the gutter.";
        public const string EscapeLine = "You go up the fire escape two iron steps at a time, landing after landing, and out onto the lead walk along the gutter.";
        public const string LaundryLine = "You step off the gutter. Sheets and shirts slap at you, a line snaps, another catches you and lets you go, and you land on the setts in a tangle of somebody's washing. Nothing broken.";
        public const string EscapeDownLine = "You go down the fire escape, iron ringing under your boots, landing after landing, and step off into the lane.";
        public const string TowerLine = "From the top of the water tower the Lowtowns spread out under the smog: a sea of slate roofs and chimneys, neon moss on the slopes, laundry between the blocks, and the High City's lights far above. The whole thing sways under you. You climb back down.";

        /// <summary>Every frame in the Lowtowns: each roof thing offered only at its own height.</summary>
        void TickRoofs()
        {
            var roofs = Zone != null ? Zone.LowtownRoofs : null; if (roofs == null || Player == null) return;
            float y = Player.transform.position.y;
            foreach (var kv in roofs.things) kv.Key.hiddenUntil = Mathf.Abs(y - kv.Key.position.y) < 2.2f ? 0 : Time.time + 1;
        }

        /// <summary>E on the roofs' things. True: handled.</summary>
        bool UseRoofThing(ZoneInteractable i)
        {
            var roofs = Zone != null ? Zone.LowtownRoofs : null;
            if (i == null || roofs == null || !roofs.things.TryGetValue(i, out var leg)) return false;
            if (leg.kind == "climb")
            {
                if (InCombat) { Message(FightingLine); return true; }
                bool spout = leg.name == ZoneBuilder.SpoutName;
                StartWork(spout ? "Climbing the rain-spout" : "Climbing the fire escape", RoofClimbSeconds, () => { MoveParty(leg.b, leg.b - leg.a); Message(spout ? SpoutLine : EscapeLine); Save(false); });
                return true;
            }
            // (Pick 9, the Sweep) Down the fire escape from its head on the walk, the party (and a child you lead) with you.
            if (leg.kind == "down")
            {
                if (InCombat) { Message(FightingLine); return true; }
                StartWork("Climbing down the fire escape", RoofClimbSeconds, () => { MoveParty(leg.b, leg.b - leg.a); Message(EscapeDownLine); Save(false); });
                return true;
            }
            if (leg.kind == "drop")
            {
                // A way out of a fight too: the drop is quick and does no harm.
                MoveParty(leg.b, leg.b - leg.a);
                FloatText(leg.b, "Through the washing", new Color(.75f, .72f, .62f));
                Message(LaundryLine);
                Save(false);
                return true;
            }
            if (leg.kind == "tower")
            {
                if (InCombat) { Message(FightingLine); return true; }
                StartWork("Climbing the water tower", RoofClimbSeconds, () => Message(TowerLine));
                return true;
            }
            return false;
        }

        /// <summary>You to <paramref name="at"/>, and Mira and the party onto the navmesh near you, spread along <paramref name="way"/>'s
        /// cross (a gutter walk is narrow: each finds the nearest walkable point).</summary>
        void MoveParty(Vector3 at, Vector3 way)
        {
            // (Pick 9, the Sweep) A child you lead, near you, comes too (EncounterSession.Sweep).
            bool child = SweepChildWithYou();
            Player.GetComponent<AdventurerMotor>().Teleport(at + Vector3.up * .1f);
            way.y = 0; var side = way.sqrMagnitude > 1e-4f ? Vector3.Cross(Vector3.up, way.normalized) : Vector3.right;
            if (child) SweepChildTo(at, way.sqrMagnitude > 1e-4f ? way.normalized : Vector3.forward);
            void Put(NavMeshAgent agent, float off)
            {
                if (agent == null || !agent.isOnNavMesh) return;
                if (NavMesh.SamplePosition(at + side * off, out var hit, 2.5f, NavMesh.AllAreas)) agent.Warp(hit.position);
            }
            if (Companion != null) Put(Companion.GetComponent<NavMeshAgent>(), 1.5f);
            foreach (var c in PartySims) if (c != null) Put(c.GetComponent<NavMeshAgent>(), -(1.5f + c.slot));
        }
    }
}
