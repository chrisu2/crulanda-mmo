using System;
using UnityEngine;

namespace Crulanda.Core
{
    /// <summary>
    /// Hierarchical gameplay tag, stored as a dotted path such as "Damage.Fire".
    /// A tag "matches" a query if it equals the query or is a descendant of it:
    /// "Damage.Fire" matches "Damage.Fire" and "Damage", but not "Damage.FireStorm".
    /// Known tags live in <see cref="GameTags"/>; see Docs/GAMEPLAY_TAGS.md.
    /// </summary>
    [Serializable]
    public struct GameTag : IEquatable<GameTag>
    {
        [SerializeField] private string _path;

        public GameTag(string path)
        {
            _path = path;
        }

        public string Path { get { return _path ?? string.Empty; } }
        public bool IsValid { get { return IsValidPath(_path); } }

        /// <summary>Segments separated by '.', each starting with an uppercase letter, then letters/digits.</summary>
        public static bool IsValidPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            bool segmentStart = true;
            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];
                if (segmentStart)
                {
                    if (c < 'A' || c > 'Z') return false;
                    segmentStart = false;
                }
                else if (c == '.')
                {
                    segmentStart = true;
                }
                else
                {
                    bool ok = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
                    if (!ok) return false;
                }
            }
            return !segmentStart; // must not end with '.'
        }

        /// <summary>True if this tag equals <paramref name="query"/> or is a descendant of it.</summary>
        public bool Matches(GameTag query)
        {
            string self = Path;
            string q = query.Path;
            if (q.Length == 0 || self.Length < q.Length) return false;
            if (!self.StartsWith(q, StringComparison.Ordinal)) return false;
            return self.Length == q.Length || self[q.Length] == '.';
        }

        public bool Equals(GameTag other)
        {
            return string.Equals(Path, other.Path, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is GameTag && Equals((GameTag)obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Path);
        }

        public override string ToString()
        {
            return Path;
        }

        public static bool operator ==(GameTag a, GameTag b) { return a.Equals(b); }
        public static bool operator !=(GameTag a, GameTag b) { return !a.Equals(b); }
    }
}
