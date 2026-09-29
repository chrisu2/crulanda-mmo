using System.Collections.Generic;
using UnityEngine;
using Crulanda.Core;

namespace Crulanda.Data
{
    /// <summary>
    /// Static template for an actor: its default level, disposition, classification and base stats.
    /// Players, enemies and SimAdventurers all start from an archetype (class/race data layers on later).
    /// </summary>
    [CreateAssetMenu(menuName = "Crulanda/Actor Archetype", fileName = "actor_new")]
    public sealed class ActorArchetypeDefinition : DefinitionBase
    {
        [Header("Actor")]
        [SerializeField, Min(1)] private int _level = 1;
        [SerializeField] private Disposition _disposition = Disposition.Hostile;
        [SerializeField] private ActorClassification _classification = ActorClassification.Normal;
        [SerializeField] private ResourceKind _resourceKind = ResourceKind.None;

        [Header("Base stats")]
        [SerializeField] private List<StatValue> _baseStats = new List<StatValue>();

        public int Level { get { return _level; } }
        public Disposition Disposition { get { return _disposition; } }
        public ActorClassification Classification { get { return _classification; } }
        public ResourceKind ResourceKind { get { return _resourceKind; } }
        public IReadOnlyList<StatValue> BaseStats { get { return _baseStats; } }

#if UNITY_EDITOR
        /// <summary>Editor tooling and tests only; compiled out of player builds.</summary>
        public void EditorConfigure(int level, Disposition disposition, ActorClassification classification,
                                    ResourceKind resourceKind, IEnumerable<StatValue> baseStats)
        {
            _level = level;
            _disposition = disposition;
            _classification = classification;
            _resourceKind = resourceKind;
            _baseStats = new List<StatValue>(baseStats);
        }
#endif
    }
}
