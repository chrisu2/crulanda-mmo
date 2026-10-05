using NUnit.Framework;
using Crulanda.Core;
using Crulanda.Gameplay;

namespace Crulanda.Tests
{
    public class LevelConTests
    {
        [TestCase(10, 3, ConDifficulty.Trivial)]
        [TestCase(10, 5, ConDifficulty.Trivial)]
        [TestCase(10, 6, ConDifficulty.Easy)]
        [TestCase(10, 8, ConDifficulty.Easy)]
        [TestCase(10, 9, ConDifficulty.Even)]
        [TestCase(10, 10, ConDifficulty.Even)]
        [TestCase(10, 11, ConDifficulty.Even)]
        [TestCase(10, 12, ConDifficulty.Tough)]
        [TestCase(10, 13, ConDifficulty.Dangerous)]
        [TestCase(10, 14, ConDifficulty.Dangerous)]
        [TestCase(10, 15, ConDifficulty.Dangerous)]
        [TestCase(10, 16, ConDifficulty.Deadly)]
        [TestCase(1, 40, ConDifficulty.Deadly)]
        public void Evaluate_matches_thresholds(int viewer, int target, ConDifficulty expected)
        {
            Assert.AreEqual(expected, LevelCon.Evaluate(viewer, target));
        }
    }
}
