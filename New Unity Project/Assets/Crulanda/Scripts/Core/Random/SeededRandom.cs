using System;

namespace Crulanda.Core
{
    /// <summary>
    /// Small deterministic PRNG (SplitMix32-style) with identical output on every platform/runtime,
    /// unlike System.Random whose algorithm is not guaranteed across runtimes.
    /// </summary>
    public sealed class SeededRandom : IRandomSource
    {
        uint _state;

        public SeededRandom(int seed)
        {
            _state = unchecked((uint)seed);
        }

        uint NextUInt()
        {
            unchecked
            {
                _state += 0x9E3779B9u;
                uint z = _state;
                z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
                z = (z ^ (z >> 13)) * 0xC2B2AE35u;
                return z ^ (z >> 16);
            }
        }

        public float NextFloat()
        {
            // 24 random bits -> [0,1) exactly representable in float.
            return (NextUInt() >> 8) * (1.0f / 16777216.0f);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentException("maxExclusive must be greater than minInclusive.");

            uint range = unchecked((uint)(maxExclusive - minInclusive));
            return minInclusive + (int)(NextUInt() % range);
        }
    }
}
