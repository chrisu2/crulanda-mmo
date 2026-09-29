using System;
using System.Collections.Generic;

namespace Crulanda.Abilities
{
    public enum AbilityStartResult { Started, Invalid, Casting, Cooldown, InsufficientResource }

    /// <summary>
    /// Per-actor timing and casting state. Uses caller-supplied simulation time for deterministic tests.
    /// Target validation and effects belong to the host. Costs/cooldowns begin on cast start;
    /// interrupted casts consume their cost and cooldown and never execute their effect.
    /// </summary>
    public sealed class AbilityRuntime
    {
        readonly Dictionary<string, float> ready = new Dictionary<string, float>();
        float globalReady, castStarted, castEnds;
        Action pendingEffect;
        public AbilityDefinition Casting { get; private set; }
        public bool IsCasting { get { return Casting != null; } }

        public float Remaining(AbilityDefinition definition, float now)
        {
            if (definition == null || string.IsNullOrEmpty(definition.id)) return 0;
            ready.TryGetValue(definition.id, out var personal);
            return Math.Max(0, Math.Max(personal, definition.globalCooldown > 0 ? globalReady : 0) - now);
        }
        public float CastProgress(float now)
        {
            if (!IsCasting) return 0;
            return Math.Max(0, Math.Min(1, (now - castStarted) / Math.Max(.001f, castEnds - castStarted)));
        }
        public AbilityStartResult TryStart(AbilityDefinition definition, float now, Func<int, bool> spend, Action effect)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id) || spend == null || effect == null ||
                definition.cost < 0 || !ValidTime(now) || !ValidTime(definition.cooldown) ||
                !ValidTime(definition.castTime) || !ValidTime(definition.globalCooldown)) return AbilityStartResult.Invalid;
            if (IsCasting) return AbilityStartResult.Casting;
            if (Remaining(definition, now) > 0) return AbilityStartResult.Cooldown;
            if (!spend(definition.cost)) return AbilityStartResult.InsufficientResource;
            ready[definition.id] = now + definition.cooldown;
            if (definition.globalCooldown > 0) globalReady = now + definition.globalCooldown;
            if (definition.castTime > 0)
            {
                Casting = definition; castStarted = now; castEnds = now + definition.castTime; pendingEffect = effect;
            }
            else effect();
            return AbilityStartResult.Started;
        }
        public void Tick(float now, bool canContinue = true)
        {
            if (!IsCasting) return;
            if (!canContinue) { Interrupt(); return; }
            if (now < castEnds) return;
            var effect = pendingEffect;
            Interrupt();
            effect();
        }
        public void Interrupt() { Casting = null; pendingEffect = null; castStarted = castEnds = 0; }
        public void Reset() { Interrupt(); ready.Clear(); globalReady = 0; }
        static bool ValidTime(float value) { return value >= 0 && !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
