using UnityEngine;
using Crulanda.Core;

namespace Crulanda.Gameplay
{
    /// <summary>
    /// An actor's class resource (mana etc.). Phase 0 keeps this generic; per-class mechanics
    /// (decay, generation) arrive with the class system in Phase 2.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResourcePool : MonoBehaviour
    {
        public VitalPool Pool { get; private set; }
        public ResourceKind Kind { get; private set; }

        void Awake()
        {
            if (Pool == null) Pool = new VitalPool(0, true);
        }

        internal void Bind(ResourceKind kind, int maxPower)
        {
            if (Pool == null) Pool = new VitalPool(0, true);
            Kind = kind;
            Pool.SetMax(kind == ResourceKind.None ? 0 : maxPower, false);
            Pool.Fill();
        }
    }
}
