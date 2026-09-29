using Crulanda.Core;

namespace Crulanda.Gameplay
{
    public enum ModifierOp
    {
        /// <summary>Added to the base value.</summary>
        Flat = 0,
        /// <summary>Additive percentage; all PercentAdd modifiers on a stat are summed. 0.10 = +10%.</summary>
        PercentAdd = 1,
        /// <summary>Multiplicative percentage; each is applied separately. 0.10 = x1.10.</summary>
        PercentMult = 2
    }

    public struct StatModifier
    {
        public readonly StatType Stat;
        public readonly ModifierOp Op;
        public readonly float Value;

        /// <summary>Whatever applied this modifier (equipment instance, buff instance...). Used for removal.</summary>
        public readonly object Source;

        public StatModifier(StatType stat, ModifierOp op, float value, object source)
        {
            Stat = stat;
            Op = op;
            Value = value;
            Source = source;
        }
    }
}
