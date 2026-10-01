using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A door the player can interact with. Openable doors swing on a hinge and stop blocking; barred doors only
    /// answer with a line of flavour text. The zone builder registers every door it makes in ZoneBuilder.Doors.
    /// </summary>
    public sealed class ZoneDoor
    {
        public string name;
        /// <summary>What the door is: "house" (a home's barred front door), "rooms" (the door to an inn's rooms upstairs) or "home"
        /// (a door added to a barn somebody lives in). An openable door is the inn's own.</summary>
        public string kind = "house";
        /// <summary>The chimney smoke of the house behind this door, when it has a chimney (a cold hearth stops it).</summary>
        public ParticleSystem smoke;
        public bool openable;
        public Transform hinge;
        public Collider blocker;
        public Vector3 position;
        public bool Open { get; private set; }
        public void SetOpen(bool open)
        {
            if (!openable || hinge == null) return;
            Open = open;
            hinge.localRotation = Quaternion.Euler(0, open ? -105 : 0, 0);
            if (blocker != null) blocker.enabled = !open;
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
