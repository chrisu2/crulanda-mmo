using System;
using UnityEngine;

namespace Crulanda.Core
{
    /// <summary>
    /// Stable string identifier for a *content definition* (item, ability, archetype, quest...).
    /// Saves reference definitions by ContentId, never by asset path or Unity GUID.
    /// Format: lowercase letters, digits, '_', '-', '.'; starts with a letter; max 64 chars.
    /// Example: "item.iron_sword", "ability.warrior.strike". Changing an id after release breaks saves.
    /// </summary>
    [Serializable]
    public struct ContentId : IEquatable<ContentId>, IComparable<ContentId>
    {
        public const int MaxLength = 64;

        [SerializeField] private string _value;

        public ContentId(string value)
        {
            _value = value;
        }

        public string Value { get { return _value ?? string.Empty; } }
        public bool IsValid { get { return IsValidFormat(_value); } }

        public static ContentId Empty { get { return new ContentId(string.Empty); } }

        public static bool IsValidFormat(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxLength) return false;
            if (value[0] < 'a' || value[0] > 'z') return false;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-' || c == '.';
                if (!ok) return false;
            }
            return true;
        }

        public bool Equals(ContentId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentId && Equals((ContentId)obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public int CompareTo(ContentId other)
        {
            return string.CompareOrdinal(Value, other.Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(ContentId a, ContentId b) { return a.Equals(b); }
        public static bool operator !=(ContentId a, ContentId b) { return !a.Equals(b); }
    }
}
