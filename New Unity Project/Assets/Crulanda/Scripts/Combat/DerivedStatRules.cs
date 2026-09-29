using System;

namespace Crulanda.Combat
{
    [Serializable]
    public sealed class DerivedStatRules
    {
        public int baseHealth = 180;
        public int healthPerLevel = 25;
        public float healthPerStamina = 5;
        public int baseAttackPower = 12;
        public int attackPowerPerLevel = 2;
        public float attackPowerPerStrength = 1;
        public float spellPowerPerIntellect = 1;
        public float armorPerAgility = 1;
    }

    public struct DerivedStatValues
    {
        public int health, attackPower, spellPower, armor;
    }

    public static class DerivedStatCalculator
    {
        public static DerivedStatValues Calculate(DerivedStatRules rules, int level, float stamina,
            float strength, float intellect, float agility, int weaponBonus)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            double levels = Math.Max(0, level - 1);
            return new DerivedStatValues {
                health = Math.Max(1, Rounded(rules.baseHealth + levels * rules.healthPerLevel + Math.Max(0, stamina) * rules.healthPerStamina)),
                attackPower = Rounded(rules.baseAttackPower + levels * rules.attackPowerPerLevel + Math.Max(0, strength) * rules.attackPowerPerStrength + weaponBonus),
                spellPower = Rounded(Math.Max(0, intellect) * rules.spellPowerPerIntellect),
                armor = Rounded(Math.Max(0, agility) * rules.armorPerAgility)
            };
        }
        static int Rounded(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
            return (int)Math.Max(0, Math.Min(int.MaxValue, Math.Round(value, MidpointRounding.AwayFromZero)));
        }
    }

    public static class CombatMath
    {
        public static int Damage(int amount, float armor, float damageMultiplier)
        {
            if (amount < 0 || float.IsNaN(armor) || float.IsInfinity(armor) ||
                float.IsNaN(damageMultiplier) || float.IsInfinity(damageMultiplier) || damageMultiplier < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            double damage = amount * (double)damageMultiplier * 100 / (100 + Math.Max(0, armor));
            return (int)Math.Min(int.MaxValue, Math.Max(0, Math.Round(damage, MidpointRounding.AwayFromZero)));
        }
    }
}
