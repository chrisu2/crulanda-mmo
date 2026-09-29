using System;

namespace Crulanda.Gameplay
{
    /// <summary>
    /// A bounded integer pool (health, mana, ...). Pure C#; owns clamping and change notification only.
    /// Damage/healing rules live in combat systems, not here.
    /// </summary>
    public sealed class VitalPool
    {
        public int Max { get; private set; }
        public int Current { get; private set; }

        public bool IsEmpty { get { return Current <= 0; } }
        public bool IsFull { get { return Current >= Max; } }
        public float Ratio { get { return Max <= 0 ? 0f : (float)Current / Max; } }

        /// <summary>(pool, oldCurrent, newCurrent). Raised when Current or Max changes.</summary>
        public event Action<VitalPool, int, int> Changed;

        public VitalPool(int max, bool startFull)
        {
            Max = Math.Max(0, max);
            Current = startFull ? Max : 0;
        }

        /// <summary>Sets the maximum. If <paramref name="preserveRatio"/>, Current scales with it; otherwise it is only clamped.</summary>
        public void SetMax(int newMax, bool preserveRatio)
        {
            newMax = Math.Max(0, newMax);
            if (newMax == Max) return;

            int old = Current;
            float ratio = Ratio;
            Max = newMax;
            Current = preserveRatio
                ? (int)Math.Round(ratio * newMax, MidpointRounding.AwayFromZero)
                : Math.Min(Current, Max);
            Current = Math.Max(0, Math.Min(Current, Max));
            Raise(old);
        }

        public void SetCurrent(int value)
        {
            int old = Current;
            Current = Math.Max(0, Math.Min(value, Max));
            if (Current != old) Raise(old);
        }

        public void Fill()
        {
            SetCurrent(Max);
        }

        /// <summary>Adds <paramref name="delta"/> (negative to drain), clamped. Returns the delta actually applied.</summary>
        public int Change(int delta)
        {
            int old = Current;
            Current = (int)Math.Max(0L, Math.Min((long)Current + delta, (long)Max));
            if (Current != old) Raise(old);
            return Current - old;
        }

        void Raise(int oldCurrent)
        {
            var handler = Changed;
            if (handler != null) handler(this, oldCurrent, Current);
        }
    }
}
