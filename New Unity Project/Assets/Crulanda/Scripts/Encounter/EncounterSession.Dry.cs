using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Camps that keep to dry ground (ZoneCamp.dry; the Lowtowns' Bank-snouts on the Chemical River's banks, 2026-10-18: in the second
    /// bestiary tour a blue snout stood in the river and a red one on its waterline). Where such a camp's mobs are stood when they spawn,
    /// where they stroll and the loop a roamer walks are never in the zone's water nor within <see cref="DryMargin"/> of it, by the water's
    /// own drawn edge (ZoneWater.NearWater, as the builder keeps its props and plants off it).
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>How far from the waterline a dry camp's mobs keep, in metres.</summary>
        public const float DryMargin = 4;
        /// <summary>True when <paramref name="p"/> is in the zone's water or within <paramref name="margin"/> of it.</summary>
        public bool NearWater(Vector3 p, float margin = DryMargin)
        {
            var w = Zone != null ? Zone.Water : null;
            return w != null && w.NearWater(new Vector2(p.x, p.z), margin);
        }
    }
}
