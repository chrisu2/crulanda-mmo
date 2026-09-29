using System;
using UnityEngine;

namespace Crulanda.Core
{
    /// <summary>
    /// Stable identifier for a *persistent runtime entity* (a SimAdventurer, a named world NPC,
    /// a dropped-item instance...). Backed by a GUID string ("N" format, 32 hex chars).
    /// Distinct from <see cref="ContentId"/>, which identifies static definitions.
    /// </summary>
    [Serializable]
    public struct EntityId : IEquatable<EntityId>
    {
        [SerializeField] private string _value;

        public EntityId(string value)
        {
            _value = value;
        }

        public string Value { get { return _value ?? string.Empty; } }
        public bool IsValid { get { return !string.IsNullOrEmpty(_value); } }

        public static EntityId New()
        {
            return new EntityId(Guid.NewGuid().ToString("N"));
        }

        public bool Equals(EntityId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is EntityId && Equals((EntityId)obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(EntityId a, EntityId b) { return a.Equals(b); }
        public static bool operator !=(EntityId a, EntityId b) { return !a.Equals(b); }
    }
}
