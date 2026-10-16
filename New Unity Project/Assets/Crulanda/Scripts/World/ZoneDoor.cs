using UnityEngine;
using UnityEngine.AI;

namespace Crulanda.World
{
    /// <summary>
    /// A door the player can interact with. Openable doors swing on a hinge and stop blocking; barred doors only
    /// answer with a line of flavour text. The zone builder registers every door it makes in ZoneBuilder.Doors.
    /// </summary>
    public sealed class ZoneDoor
    {
        public string name;
        /// <summary>What the door is: "house" (a home's barred front door), "rooms" (the door to an inn's rooms upstairs), "home"
        /// (a door added to a barn somebody lives in) or "bunk" (a waystation's bunks: where its lodger goes to bed; no door to knock
        /// at). An openable door is an inn's own (kind "inn").</summary>
        public string kind = "house";
        /// <summary>The chimney smoke of the house behind this door, when it has a chimney (a cold hearth stops it).</summary>
        public ParticleSystem smoke;
        public bool openable;
        public Transform hinge;
        public Collider blocker;
        /// <summary>The second leaf of a double door (Phase 8.6, 2026-10-15, Chris: "prob need double doors and inn a bit bigger"): its
        /// hinge on the other jamb and its slab's collider; null for a door of one leaf.</summary>
        public Transform hinge2;
        public Collider blocker2;
        /// <summary>Each leaf's obstacle while it stands shut (not carving: the walkers' avoidance steers round it, the doorway lane
        /// keeps them to the open leaf).</summary>
        public NavMeshObstacle stop, stop2;
        public Vector3 position;
        /// <summary>The lane through the doorway and who is on it (an inn's door; null for a barred one).</summary>
        public ZoneDoorway Way;
        public bool Open { get; private set; }
        /// <summary>One leaf shut and the other open: an inn after the innkeeper's bedtime, so the late drinkers go out one at a time
        /// (Phase 8.6 plan 3a: "the look of a village at night, not a bug").</summary>
        public bool HalfShut { get; private set; }
        public void SetOpen(bool open)
        {
            if (!openable || hinge == null) return;
            Open = open; HalfShut = false;
            Swing(hinge, blocker, stop, open, -1);
            if (hinge2 != null) Swing(hinge2, blocker2, stop2, open, 1);
            if (Way != null) Way.Single = false;
        }
        /// <summary>Shuts the first leaf (or opens it again) while the door stands open; a door of one leaf, or a shut one, is left be.</summary>
        public void SetHalfShut(bool shut)
        {
            if (!openable || hinge == null || hinge2 == null || !Open || HalfShut == shut) return;
            HalfShut = shut;
            Swing(hinge, blocker, stop, !shut, -1);
            if (Way != null) Way.Single = shut;
        }
        /// <summary>A leaf swung in (open: back against its jamb, 105 degrees, its collider and obstacle off) or shut in the frame. The
        /// first leaf (hinged on the left seen from outside) turns one way and the second the other, both into the room.</summary>
        static void Swing(Transform leaf, Collider block, NavMeshObstacle obstacle, bool open, int turn)
        {
            leaf.localRotation = Quaternion.Euler(0, open ? 105 * turn : 0, 0);
            if (block != null) block.enabled = !open;
            if (obstacle != null) obstacle.enabled = !open;
        }
    }

    /// <summary>
    /// Where a trade is worked: the smithy's anvil, a market counter, the bake oven, the tannery, the woodpile.
    /// Villagers of that trade stand at <see cref="stand"/> and face <see cref="look"/>. Registered in ZoneBuilder.Workplaces.
    /// </summary>
    public sealed class ZoneWorkplace
    {
        public string kind, name;
        public Vector3 stand, look;
    }
}
