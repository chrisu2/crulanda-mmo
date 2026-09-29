using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Gameplay
{
    /// <summary>
    /// Registry of all *physically present* actors. Actors register on enable and unregister on disable.
    /// Used by the dev console now and by targeting/AI queries later. Offscreen SimAdventurers are data,
    /// not Actors, so they never appear here (brief section 40).
    /// </summary>
    public static class ActorRegistry
    {
        static readonly List<Actor> _actors = new List<Actor>();

        public static IReadOnlyList<Actor> Actors { get { return _actors; } }
        public static int Count { get { return _actors.Count; } }

        public static void Register(Actor actor)
        {
            if (actor != null && !_actors.Contains(actor)) _actors.Add(actor);
        }

        public static void Unregister(Actor actor)
        {
            _actors.Remove(actor);
        }

        /// <summary>Case-insensitive exact-name match; falls back to a unique prefix match. Null if none or ambiguous.</summary>
        public static Actor FindByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            Actor prefixMatch = null;
            int prefixCount = 0;
            for (int i = 0; i < _actors.Count; i++)
            {
                string actorName = _actors[i].DisplayName ?? string.Empty;
                if (string.Equals(actorName, name, StringComparison.OrdinalIgnoreCase)) return _actors[i];
                if (actorName.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    prefixMatch = _actors[i];
                    prefixCount++;
                }
            }
            return prefixCount == 1 ? prefixMatch : null;
        }

        public static void Clear()
        {
            _actors.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Clear();
        }
    }
}
