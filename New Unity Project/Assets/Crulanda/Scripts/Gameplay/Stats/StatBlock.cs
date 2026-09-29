using System;
using System.Collections.Generic;
using Crulanda.Core;

namespace Crulanda.Gameplay
{
    /// <summary>
    /// Pure C# stat container: base values plus modifiers, with lazily cached final values.
    /// Final = (Base + sum(Flat)) * (1 + sum(PercentAdd)) * product(1 + PercentMult).
    /// Contains no formulas that derive one stat from another (e.g. health from stamina); those belong in
    /// dedicated gameplay systems (brief section 9).
    /// </summary>
    public sealed class StatBlock
    {
        static readonly int StatCount = ComputeStatCount();

        readonly float[] _base = new float[StatCount];
        readonly List<StatModifier> _modifiers = new List<StatModifier>();
        readonly float[] _cache = new float[StatCount];
        readonly bool[] _dirty = new bool[StatCount];

        /// <summary>Raised after a base value or modifier for the stat changed.</summary>
        public event Action<StatType> Changed;

        public StatBlock()
        {
            for (int i = 0; i < StatCount; i++) _dirty[i] = true;
        }

        static int ComputeStatCount()
        {
            int max = 0;
            foreach (int v in Enum.GetValues(typeof(StatType)))
                if (v > max) max = v;
            return max + 1;
        }

        public float GetBase(StatType stat)
        {
            return _base[(int)stat];
        }

        public void SetBase(StatType stat, float value)
        {
            _base[(int)stat] = value;
            Invalidate(stat);
        }

        public void LoadBase(IEnumerable<StatValue> values)
        {
            if (values == null) return;
            foreach (var v in values) SetBase(v.stat, v.value);
        }

        public void AddModifier(StatModifier modifier)
        {
            _modifiers.Add(modifier);
            Invalidate(modifier.Stat);
        }
        public void AddModifiers(IEnumerable<StatModifier> modifiers)
        {
            var changed = new HashSet<StatType>();
            foreach (var modifier in modifiers) { _modifiers.Add(modifier); changed.Add(modifier.Stat); }
            NotifyBatch(changed);
        }

        /// <summary>Removes every modifier applied by <paramref name="source"/>. Returns how many were removed.</summary>
        public int RemoveModifiersFromSource(object source)
        {
            int removed = 0;
            var changed = new HashSet<StatType>();
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_modifiers[i].Source, source)) continue;
                var stat = _modifiers[i].Stat;
                _modifiers.RemoveAt(i);
                removed++;
                changed.Add(stat);
            }
            // Notify after mutation: a health change can expire other effects reentrantly.
            NotifyBatch(changed);
            return removed;
        }
        void NotifyBatch(HashSet<StatType> changed)
        {
            // Callbacks may read several related stats. Invalidate all caches before notifying any.
            foreach (var stat in changed) _dirty[(int)stat] = true;
            foreach (var stat in changed) { var handler = Changed; if (handler != null) handler(stat); }
        }

        public float Get(StatType stat)
        {
            int index = (int)stat;
            if (_dirty[index])
            {
                _cache[index] = Recompute(stat);
                _dirty[index] = false;
            }
            return _cache[index];
        }

        /// <summary>Final value rounded to the nearest whole number (halves away from zero).</summary>
        public int GetRounded(StatType stat)
        {
            return (int)Math.Round(Get(stat), MidpointRounding.AwayFromZero);
        }

        float Recompute(StatType stat)
        {
            float flat = 0f;
            float percentAdd = 0f;
            float percentMult = 1f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                var m = _modifiers[i];
                if (m.Stat != stat) continue;
                switch (m.Op)
                {
                    case ModifierOp.Flat: flat += m.Value; break;
                    case ModifierOp.PercentAdd: percentAdd += m.Value; break;
                    case ModifierOp.PercentMult: percentMult *= 1f + m.Value; break;
                }
            }

            return (_base[(int)stat] + flat) * (1f + percentAdd) * percentMult;
        }

        void Invalidate(StatType stat)
        {
            _dirty[(int)stat] = true;
            var handler = Changed;
            if (handler != null) handler(stat);
        }
    }
}
