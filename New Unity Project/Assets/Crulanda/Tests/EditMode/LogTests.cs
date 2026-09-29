using System.Collections.Generic;
using NUnit.Framework;
using Crulanda.Core;

namespace Crulanda.Tests
{
    public class LogTests
    {
        List<LogEntry> _captured;

        [SetUp]
        public void SetUp()
        {
            CrulandaLog.ResetToDefaults();
            _captured = new List<LogEntry>();
            CrulandaLog.Sink = (entry, ctx) => _captured.Add(entry);
            CrulandaLog.SetAllLevels(LogLevel.Info);
        }

        [TearDown]
        public void TearDown()
        {
            CrulandaLog.ResetToDefaults();
        }

        [Test]
        public void Messages_below_the_category_level_are_dropped()
        {
            CrulandaLog.Verbose(LogCategory.Combat, "hidden");
            CrulandaLog.Info(LogCategory.Combat, "shown");
            Assert.AreEqual(1, _captured.Count);
            Assert.AreEqual("shown", _captured[0].Message);
        }

        [Test]
        public void Levels_are_per_category()
        {
            CrulandaLog.SetLevel(LogCategory.AI, LogLevel.Verbose);
            CrulandaLog.Verbose(LogCategory.AI, "ai");
            CrulandaLog.Verbose(LogCategory.Combat, "combat");
            Assert.AreEqual(1, _captured.Count);
            Assert.AreEqual(LogCategory.AI, _captured[0].Category);
        }

        [Test]
        public void Off_silences_a_category_even_for_errors()
        {
            CrulandaLog.SetLevel(LogCategory.UI, LogLevel.Off);
            CrulandaLog.Error(LogCategory.UI, "nope");
            Assert.AreEqual(0, _captured.Count);
        }

        [Test]
        public void Recent_buffer_keeps_the_newest_entries_in_order()
        {
            for (int i = 0; i < CrulandaLog.RecentCapacity + 5; i++)
                CrulandaLog.Info(LogCategory.Core, "m" + i);

            var recent = new List<LogEntry>();
            CrulandaLog.GetRecent(recent, 3);
            Assert.AreEqual(3, recent.Count);
            Assert.AreEqual("m" + (CrulandaLog.RecentCapacity + 2), recent[0].Message);
            Assert.AreEqual("m" + (CrulandaLog.RecentCapacity + 4), recent[2].Message);
        }

        [Test]
        public void Parse_helpers_are_case_insensitive_and_reject_numbers()
        {
            LogLevel level; LogCategory category;
            Assert.IsTrue(CrulandaLog.TryParseLevel("warning", out level));
            Assert.AreEqual(LogLevel.Warning, level);
            Assert.IsTrue(CrulandaLog.TryParseCategory("COMBAT", out category));
            Assert.IsFalse(CrulandaLog.TryParseLevel("99", out level));
            Assert.IsFalse(CrulandaLog.TryParseCategory("nonsense", out category));
        }
    }
}
