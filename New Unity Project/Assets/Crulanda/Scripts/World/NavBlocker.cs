using UnityEngine;

namespace Crulanda.World
{
    /// <summary>Marks a solid prop whose renderer bounds must be cut out of the zone navigation mesh.</summary>
    public sealed class NavBlocker : MonoBehaviour { }

    /// <summary>Marks a walkable surface (e.g. a bridge deck) that must be added to the zone navigation mesh.</summary>
    public sealed class NavWalkable : MonoBehaviour { }
}
