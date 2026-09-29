using NUnit.Framework;
using Crulanda.Core;

namespace Crulanda.Tests
{
    public class SeededRandomTests
    {
        [Test]
        public void Same_seed_gives_same_sequence()
        {
            var a = new SeededRandom(1234);
            var b = new SeededRandom(1234);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(a.NextInt(0, 1000), b.NextInt(0, 1000));
        }

        [Test]
        public void Different_seeds_diverge()
        {
            var a = new SeededRandom(1);
            var b = new SeededRandom(2);
            bool anyDifferent = false;
            for (int i = 0; i < 20; i++)
                if (a.NextInt(0, 1000000) != b.NextInt(0, 1000000)) anyDifferent = true;
            Assert.IsTrue(anyDifferent);
        }

        [Test]
        public void NextFloat_stays_in_unit_interval()
        {
            var r = new SeededRandom(7);
            for (int i = 0; i < 10000; i++)
            {
                float f = r.NextFloat();
                Assert.GreaterOrEqual(f, 0f);
                Assert.Less(f, 1f);
            }
        }

        [Test]
        public void NextInt_respects_bounds_including_negative_ranges()
        {
            var r = new SeededRandom(99);
            for (int i = 0; i < 10000; i++)
            {
                int v = r.NextInt(-5, 5);
                Assert.GreaterOrEqual(v, -5);
                Assert.Less(v, 5);
            }
        }

        [Test]
        public void NextInt_rejects_empty_range()
        {
            Assert.Throws<System.ArgumentException>(() => new SeededRandom(1).NextInt(5, 5));
        }

        [Test]
        public void Chance_handles_the_extremes()
        {
            var r = new SeededRandom(3);
            Assert.IsFalse(r.Chance(0f));
            Assert.IsTrue(r.Chance(1f));
        }

        [Test]
        public void Chance_is_roughly_calibrated()
        {
            var r = new SeededRandom(2026);
            int hits = 0;
            for (int i = 0; i < 20000; i++) if (r.Chance(0.25f)) hits++;
            Assert.AreEqual(5000, hits, 400);
        }
    }
}
