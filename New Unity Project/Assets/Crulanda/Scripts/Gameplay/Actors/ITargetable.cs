using UnityEngine;
using Crulanda.Core;

namespace Crulanda.Gameplay
{
    /// <summary>What the targeting system and target frame need to know about anything selectable (brief section 5).</summary>
    public interface ITargetable
    {
        string DisplayName { get; }
        int Level { get; }
        Disposition Disposition { get; }
        ActorClassification Classification { get; }
        ContentId Faction { get; }
        bool IsAlive { get; }
        Transform Transform { get; }
    }
}
