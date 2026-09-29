namespace Crulanda.Core
{
    /// <summary>
    /// Injectable randomness so loot, hit rolls and sim decisions are testable and reproducible.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Uniform float in [0, 1).</summary>
        float NextFloat();

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        int NextInt(int minInclusive, int maxExclusive);
    }

    public static class RandomSourceExtensions
    {
        /// <summary>True with probability <paramref name="probability"/> (clamped to 0..1).</summary>
        public static bool Chance(this IRandomSource rng, float probability)
        {
            if (probability <= 0f) return false;
            if (probability >= 1f) return true;
            return rng.NextFloat() < probability;
        }
    }
}
