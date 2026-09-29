using System;

namespace Crulanda.Core
{
    /// <summary>
    /// Stat identifiers. Values are explicit and stable because they are serialized into assets and saves:
    /// append new stats with new numbers, never renumber or reuse.
    /// </summary>
    public enum StatType
    {
        Strength = 0, Agility = 1, Intellect = 2, Spirit = 3, Stamina = 4,

        MaxHealth = 10, MaxPower = 11,
        AttackPower = 20, SpellPower = 21, Armor = 22,
        CritChance = 23, Accuracy = 24, Dodge = 25, Block = 26, Parry = 27, Haste = 28,
        HealthRegen = 30, PowerRegen = 31
    }

    public enum Disposition { Friendly = 0, Neutral = 1, Hostile = 2 }

    public enum ActorClassification { Normal = 0, Elite = 1, Rare = 2, Boss = 3 }

    /// <summary>PROVISIONAL placeholder resource kinds. Real class resources will come from Crulanda canon.</summary>
    public enum ResourceKind { None = 0, Mana = 1, Vigor = 2, Focus = 3, Breath = 4 }

    /// <summary>Relative level difficulty ("con"). Thresholds live in LevelCon.</summary>
    public enum ConDifficulty { Trivial = 0, Easy = 1, Even = 2, Tough = 3, Dangerous = 4, Deadly = 5 }

    /// <summary>
    /// Canon tracking required by the project brief (section 64). Never silently promote game ideas to canon.
    /// </summary>
    public enum CanonStatus { Provisional = 0, GameOnly = 1, CanonExpanded = 2, Canon = 3 }

    [Serializable]
    public struct StatValue
    {
        public StatType stat;
        public float value;

        public StatValue(StatType stat, float value)
        {
            this.stat = stat;
            this.value = value;
        }
    }
}
