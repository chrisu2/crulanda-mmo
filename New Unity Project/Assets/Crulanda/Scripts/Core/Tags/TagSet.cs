using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Core
{
    /// <summary>
    /// A serializable set of <see cref="GameTag"/>s with hierarchical queries.
    /// If the set contains "Damage.Fire", then Has("Damage") and Has("Damage.Fire") are true.
    /// </summary>
    [Serializable]
    public sealed class TagSet
    {
        [SerializeField] private List<GameTag> _tags = new List<GameTag>();

        public int Count { get { return _tags.Count; } }
        public IReadOnlyList<GameTag> Tags { get { return _tags; } }

        public TagSet() { }

        public TagSet(IEnumerable<GameTag> tags)
        {
            if (tags == null) return;
            foreach (var t in tags) Add(t);
        }

        public bool Add(GameTag tag)
        {
            if (!tag.IsValid || _tags.Contains(tag)) return false;
            _tags.Add(tag);
            return true;
        }

        public bool Remove(GameTag tag)
        {
            return _tags.Remove(tag);
        }

        public void Clear()
        {
            _tags.Clear();
        }

        /// <summary>True if any tag in the set equals or descends from <paramref name="query"/>.</summary>
        public bool Has(GameTag query)
        {
            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i].Matches(query)) return true;
            return false;
        }

        public bool HasAny(params GameTag[] queries)
        {
            for (int i = 0; i < queries.Length; i++)
                if (Has(queries[i])) return true;
            return false;
        }

        public bool HasAll(params GameTag[] queries)
        {
            for (int i = 0; i < queries.Length; i++)
                if (!Has(queries[i])) return false;
            return true;
        }
    }
}
