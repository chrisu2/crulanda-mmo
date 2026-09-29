using EntityId = Crulanda.Core.EntityId;
using System;
using UnityEngine;
using Crulanda.Core;
using Crulanda.Data;

namespace Crulanda.Gameplay
{
    /// <summary>
    /// Root component of anything that fights or can be targeted: player, enemy, NPC, or the
    /// physical representation of a SimAdventurer. It composes identity, stats, health and resource;
    /// it deliberately contains no combat, AI, or class logic (each gets its own component later).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(ResourcePool))]
    public sealed class Actor : MonoBehaviour, ITargetable
    {
        [Header("Definition")]
        [SerializeField] private ActorArchetypeDefinition _archetype;

        [Header("Instance (overrides archetype when set)")]
        [SerializeField] private string _displayName;
        [SerializeField, Min(0)] private int _levelOverride;
        [SerializeField] private ContentId _faction;
        [SerializeField] private EntityId _entityId;

        [Header("Runtime tags")]
        [SerializeField] private TagSet _tags = new TagSet();

        Disposition _disposition = Disposition.Neutral;
        ActorClassification _classification = ActorClassification.Normal;
        int _level = 1;
        string _resolvedName;

        public StatBlock Stats { get; private set; }
        public Health Health { get; private set; }
        public ResourcePool Resource { get; private set; }
        public event Action<Actor> Rebuilt;
        public event Action<int> LevelChanged;

        public EntityId EntityId { get { return _entityId; } }
        public TagSet Tags { get { return _tags; } }
        public ActorArchetypeDefinition Archetype { get { return _archetype; } }

        // ITargetable
        public string DisplayName { get { return _resolvedName; } }
        public int Level { get { return _level; } }
        public Disposition Disposition { get { return _disposition; } }
        public ActorClassification Classification { get { return _classification; } }
        public ContentId Faction { get { return _faction; } }
        public bool IsAlive { get { return Health != null && !Health.IsDead; } }
        public Transform Transform { get { return transform; } }

        bool _initialized;

        void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>Idempotent: safe to call from Awake and from Initialize in any order.</summary>
        void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            Stats = new StatBlock();
            Health = GetComponent<Health>();
            Resource = GetComponent<ResourcePool>();

            if (!_entityId.IsValid) _entityId = EntityId.New();

            Rebuild();

        }

        void OnEnable()
        {
            ActorRegistry.Register(this);
        }

        void OnDisable()
        {
            ActorRegistry.Unregister(this);
        }

        /// <summary>Runtime setup for spawners. Call right after AddComponent/Instantiate.</summary>
        public void Initialize(ActorArchetypeDefinition archetype, string displayName, int levelOverride)
        {
            _archetype = archetype;
            _displayName = displayName;
            _levelOverride = levelOverride;

            if (_initialized) Rebuild();
            else EnsureInitialized();
        }

        /// <summary>Restores identity before activation; avoids registering a temporary identity.</summary>
        public void Initialize(ActorArchetypeDefinition archetype, string displayName, int levelOverride, EntityId persistentId)
        {
            if (gameObject.activeInHierarchy) throw new System.InvalidOperationException("Restore actors while inactive, then activate them.");
            if (!persistentId.IsValid) throw new System.ArgumentException("Persistent ID is required.", "persistentId");
            _entityId = persistentId;
            Initialize(archetype, displayName, levelOverride);
        }

        void Rebuild()
        {
            _resolvedName = !string.IsNullOrEmpty(_displayName)
                ? _displayName
                : (_archetype != null ? _archetype.DisplayName : gameObject.name);

            _level = _levelOverride > 0 ? _levelOverride : (_archetype != null ? _archetype.Level : 1);
            _disposition = _archetype != null ? _archetype.Disposition : Disposition.Neutral;
            _classification = _archetype != null ? _archetype.Classification : ActorClassification.Normal;

            Stats.Changed -= OnStatChanged;
            Stats = new StatBlock();
            if (_archetype != null) Stats.LoadBase(_archetype.BaseStats);
            Stats.Changed += OnStatChanged;

            int maxHealth = Mathf.Max(1, Stats.GetRounded(StatType.MaxHealth));
            int maxPower = Mathf.Max(0, Stats.GetRounded(StatType.MaxPower));
            Health.Bind(this, maxHealth);
            Resource.Bind(_archetype != null ? _archetype.ResourceKind : ResourceKind.None, maxPower);
            Rebuilt?.Invoke(this);
        }

        void OnStatChanged(StatType stat)
        {
            // Keep pools in sync when equipment/buffs change max values; preserve the current ratio.
            if (stat == StatType.MaxHealth)
                Health.SetMaximum(Mathf.Max(1, Stats.GetRounded(StatType.MaxHealth)));
            else if (stat == StatType.MaxPower && Resource.Kind != ResourceKind.None)
                Resource.Pool.SetMax(Mathf.Max(0, Stats.GetRounded(StatType.MaxPower)), true);
        }

        /// <summary>Apply a class resource profile during spawn; starts the pool full.</summary>
        public void ConfigureResource(ResourceKind kind, int maximum)
        {
            if (!Enum.IsDefined(typeof(ResourceKind), kind) || maximum < 0) throw new ArgumentException("Invalid resource profile.");
            EnsureInitialized();
            Stats.SetBase(StatType.MaxPower, kind == ResourceKind.None ? 0 : maximum);
            Resource.Bind(kind, maximum);
        }

        public void SetLevel(int level)
        {
            int next = Mathf.Max(1, level);
            if (_level == next) return;
            _level = next; LevelChanged?.Invoke(_level);
        }

        public override string ToString()
        {
            return DisplayName + " (L" + Level + " " + Disposition + ")";
        }

#if UNITY_EDITOR
        /// <summary>Editor tooling only: configures a scene-placed actor's serialized fields.</summary>
        public void EditorSetup(ActorArchetypeDefinition archetype, string displayName, int levelOverride)
        {
            _archetype = archetype;
            _displayName = displayName;
            _levelOverride = levelOverride;
        }
#endif
    }
}

