using System;
using System.Collections.Generic;
using Crulanda.Core;
using Crulanda.Gameplay;

namespace Crulanda.Combat
{
    [Serializable]
    public sealed class StatusModifierDefinition
    {
        public StatType stat;
        public ModifierOp operation;
        public float value;
    }

    [Serializable]
    public sealed class StatusEffectDefinition
    {
        public string id;
        public string name;
        public float duration = 5;
        public float incomingDamageMultiplier = 1;
        public StatusModifierDefinition[] modifiers = new StatusModifierDefinition[0];
    }

    /// <summary>Temporary per-target effects. Reapplying an ID refreshes time, never stacks magnitude.</summary>
    public sealed class StatusEffectRuntime
    {
        sealed class Active
        {
            public string id;
            public float expires;
            public float damageMultiplier;
        }
        readonly StatBlock stats;
        readonly List<Active> active = new List<Active>();
        public int Count { get { return active.Count; } }
        public float IncomingDamageMultiplier
        {
            get {
                double value = 1;
                foreach (var effect in active) value *= effect.damageMultiplier;
                return (float)Math.Min(1000, value);
            }
        }
        public StatusEffectRuntime(StatBlock stats) { this.stats = stats ?? throw new ArgumentNullException(nameof(stats)); }
        public bool Apply(StatusEffectDefinition definition, float now)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id) || !Finite(now) || now < 0 ||
                !Finite(definition.duration) || definition.duration <= 0 ||
                !Finite(definition.incomingDamageMultiplier) || definition.incomingDamageMultiplier < 0 ||
                !Finite(now + definition.duration)) return false;
            if (definition.modifiers != null)
                foreach (var modifier in definition.modifiers)
                    if (modifier == null || !Finite(modifier.value) ||
                        !Enum.IsDefined(typeof(StatType), modifier.stat) || !Enum.IsDefined(typeof(ModifierOp), modifier.operation)) return false;
            Tick(now);
            var existing = active.Find(e => e.id == definition.id);
            if (existing != null) { existing.expires = now + definition.duration; return true; }
            var entry = new Active { id = definition.id, expires = now + definition.duration, damageMultiplier = definition.incomingDamageMultiplier };
            active.Add(entry);
            if (definition.modifiers != null)
            {
                var modifiers = new List<StatModifier>();
                foreach (var modifier in definition.modifiers)
                    modifiers.Add(new StatModifier(modifier.stat, modifier.operation, modifier.value, entry));
                stats.AddModifiers(modifiers);
            }
            return active.Contains(entry);
        }
        public float Remaining(string id, float now)
        {
            var found = active.Find(e => e.id == id);
            return found == null ? 0 : Math.Max(0, found.expires - now);
        }
        public void Tick(float now)
        {
            // A stat expiry may cause death and clear the remaining effects from a callback.
            foreach (var entry in active.ToArray())
                if (now >= entry.expires) Remove(entry.id);
        }
        public bool Remove(string id)
        {
            int index = active.FindIndex(e => e.id == id);
            if (index < 0) return false;
            RemoveAt(index); return true;
        }
        public void Clear() { while (active.Count > 0) RemoveAt(active.Count - 1); }
        void RemoveAt(int index)
        {
            var entry = active[index]; active.RemoveAt(index);
            stats.RemoveModifiersFromSource(entry);
        }
        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
