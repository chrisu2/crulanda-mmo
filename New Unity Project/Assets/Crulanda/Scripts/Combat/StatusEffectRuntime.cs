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
            public StatusEffectDefinition definition;
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
            if (existing != null) { existing.expires = now + definition.duration; existing.definition = definition; return true; }
            var entry = new Active { id = definition.id, expires = now + definition.duration, damageMultiplier = definition.incomingDamageMultiplier, definition = definition };
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
        /// <summary>Every effect on, with its time left (the HUD's buff row, 2026-10-14).</summary>
        public void Each(float now, Action<string, float> visit)
        {
            foreach (var entry in active) if (entry.expires > now) visit(entry.id, entry.expires - now);
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
        /// <summary>The incoming-damage multiplier of the status of this id (1 when it is not on): above 1 it is harmful.</summary>
        public float MultiplierOf(string id)
        {
            var found = active.Find(e => e.id == id);
            return found == null ? 1 : found.damageMultiplier;
        }
        /// <summary>The definition a status of this id was put on with (the buff icon's tooltip reads its modifiers), or null.</summary>
        public StatusEffectDefinition DefinitionOf(string id) { var found = active.Find(e => e.id == id); return found?.definition; }
        /// <summary>Whether a status is harmful (a debuff: red on the buff row): one that makes its bearer take more, lowers a stat, or by
        /// its id the grey, the Wasting, a poison or a bleed (the same test as <see cref="RemoveHarmful"/>, plus a lowered stat).</summary>
        public static bool IsHarmful(string id, float damageMultiplier, StatusEffectDefinition definition)
        {
            if (damageMultiplier > 1) return true;
            if (id != null && (id.Contains("grey") || id.Contains("wasting") || id.Contains("poison") || id.Contains("bleed"))) return true;
            if (definition?.modifiers != null) foreach (var m in definition.modifiers) if (m != null && m.value < 0) return true;
            return false;
        }
        /// <summary>Whether the status of this id now on is harmful (false when it is not on).</summary>
        public bool IsHarmful(string id) { var found = active.Find(e => e.id == id); return found != null && IsHarmful(found.id, found.damageMultiplier, found.definition); }
        /// <summary>Whether a status of this id is on (expired ones are dropped at the next Tick).</summary>
        public bool Has(string id) { return active.Exists(e => e.id == id); }
        public bool Remove(string id)
        {
            int index = active.FindIndex(e => e.id == id);
            if (index < 0) return false;
            RemoveAt(index); return true;
        }
        /// <summary>Lifts every harmful working (one that makes its bearer take more, or the Wasting's grey: an id with "grey" or "wasting";
        /// and, by its id, a poison or a bleed, for the village's cure of Phase 8.6): the Salt Cairn's sifting (2026-10-12). Returns how
        /// many were lifted.</summary>
        public int RemoveHarmful()
        {
            int n = 0;
            foreach (var e in active.ToArray())
                if (e.damageMultiplier > 1 || e.id.Contains("grey") || e.id.Contains("wasting") || e.id.Contains("poison") || e.id.Contains("bleed")) { Remove(e.id); n++; }
            return n;
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
