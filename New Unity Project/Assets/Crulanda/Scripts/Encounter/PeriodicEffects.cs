using System;
using System.Collections.Generic;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Timed repeating effects owned by a class kit: damage/healing over time, channels and multi-hit flurries.
    /// Keyed by (key, target): re-applying the same key to the same target refreshes it instead of stacking,
    /// matching the status rule. Time is supplied by the caller, so the logic is testable without a scene.
    /// </summary>
    public sealed class PeriodicEffects
    {
        public sealed class Effect
        {
            public string key;
            public object target;
            public float interval, next;
            public int ticksLeft, totalTicks;
            /// <summary>Called on each tick with the tick index (0-based). Return false to end the effect early.</summary>
            public Func<Effect, int, bool> onTick;
            /// <summary>Called once when the effect ends for any reason (ran out, cancelled, refreshed away).</summary>
            public Action<Effect, bool> onEnd;
            public int value;
            public object data;
            public int TicksDone { get { return totalTicks - ticksLeft; } }
        }
        readonly List<Effect> effects = new List<Effect>();
        public int Count { get { return effects.Count; } }

        public Effect Add(string key, object target, float now, float interval, int ticks, int value, Func<Effect, int, bool> onTick, Action<Effect, bool> onEnd = null, bool tickImmediately = false)
        {
            if (string.IsNullOrEmpty(key) || target == null || interval <= 0 || ticks <= 0 || onTick == null) throw new ArgumentException("Invalid periodic effect.");
            Remove(key, target, false);
            var e = new Effect { key = key, target = target, interval = interval, next = tickImmediately ? now : now + interval,
                ticksLeft = ticks, totalTicks = ticks, value = value, onTick = onTick, onEnd = onEnd };
            effects.Add(e); return e;
        }
        public Effect Find(string key, object target) { return effects.Find(e => e.key == key && Equals(e.target, target)); }
        public bool Has(string key, object target) { return Find(key, target) != null; }
        public bool HasKey(string key) { return effects.Exists(e => e.key == key); }
        /// <summary>Ends an effect early. completed=false tells onEnd it was cancelled, not finished.</summary>
        public bool Remove(string key, object target, bool completed = false)
        {
            var e = Find(key, target); if (e == null) return false;
            effects.Remove(e); e.onEnd?.Invoke(e, completed); return true;
        }
        public void RemoveKey(string key) { foreach (var e in effects.FindAll(x => x.key == key)) { effects.Remove(e); e.onEnd?.Invoke(e, false); } }
        public void RemoveTarget(object target) { foreach (var e in effects.FindAll(x => Equals(x.target, target))) { effects.Remove(e); e.onEnd?.Invoke(e, false); } }
        public void Clear() { var all = effects.ToArray(); effects.Clear(); foreach (var e in all) e.onEnd?.Invoke(e, false); }
        public void Tick(float now)
        {
            // Callbacks may add or remove effects (e.g. a tick killing its target), so iterate over a snapshot.
            foreach (var e in effects.ToArray())
            {
                while (effects.Contains(e) && now >= e.next && e.ticksLeft > 0)
                {
                    int index = e.TicksDone; e.ticksLeft--; e.next += e.interval;
                    bool keep = e.onTick(e, index);
                    if (!keep || e.ticksLeft <= 0) { if (effects.Remove(e)) e.onEnd?.Invoke(e, keep && e.ticksLeft <= 0); break; }
                }
            }
        }
    }
}
