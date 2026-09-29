using System;

namespace Crulanda.Abilities
{
    public enum AbilityEffect { Damage = 0, Taunt = 1, Guard = 2, Heal = 3, ApplyStatus = 4, Intercept = 5, Breach = 6, Rally = 7 }

    /// <summary>Static, serializable ability data embedded in a content asset.</summary>
    [Serializable]
    public class AbilityDefinition
    {
        public string id;
        public string name;
        public string description;
        public AbilityEffect effect;
        public int power;
        public int cost;
        public float cooldown;
        public float range = 3.2f;
        public float castTime;
        public float globalCooldown = 1.5f;
        public float duration;
        public string statusId;
    }
}
