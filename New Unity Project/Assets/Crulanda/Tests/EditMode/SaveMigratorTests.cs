using NUnit.Framework;
using Crulanda.Encounter;
using Crulanda.Persistence;

namespace Crulanda.Tests
{
    public class SaveMigratorTests
    {
        sealed class AppendMigration : ISaveMigration
        {
            readonly string _suffix;
            public AppendMigration(int from, int to, string suffix) { FromVersion = from; ToVersion = to; _suffix = suffix; }
            public int FromVersion { get; private set; }
            public int ToVersion { get; private set; }
            public string Migrate(string payloadJson) { return payloadJson + _suffix; }
        }

        static SaveEnvelope Envelope(int version, string payload)
        {
            return new SaveEnvelope { formatVersion = version, payloadType = "Test", payloadJson = payload };
        }

        [Test]
        public void Current_version_is_returned_unchanged()
        {
            var m = new SaveMigrator();
            var result = m.MigrateToVersion(Envelope(3, "x"), 3);
            Assert.AreEqual(3, result.formatVersion);
            Assert.AreEqual("x", result.payloadJson);
        }

        [Test]
        public void Migrations_are_chained_in_order()
        {
            var m = new SaveMigrator();
            m.Register(new AppendMigration(1, 2, "+a"));
            m.Register(new AppendMigration(2, 3, "+b"));

            var result = m.MigrateToVersion(Envelope(1, "x"), 3);
            Assert.AreEqual(3, result.formatVersion);
            Assert.AreEqual("x+a+b", result.payloadJson);
        }

        [Test]
        public void Input_envelope_is_not_mutated()
        {
            var m = new SaveMigrator();
            m.Register(new AppendMigration(1, 2, "+a"));
            var input = Envelope(1, "x");
            m.MigrateToVersion(input, 2);
            Assert.AreEqual(1, input.formatVersion);
            Assert.AreEqual("x", input.payloadJson);
        }

        [Test]
        public void Missing_step_throws()
        {
            var m = new SaveMigrator();
            m.Register(new AppendMigration(1, 2, "+a"));
            Assert.Throws<SaveMigrationException>(() => m.MigrateToVersion(Envelope(1, "x"), 3));
        }

        [Test]
        public void Newer_save_than_game_throws()
        {
            var m = new SaveMigrator();
            Assert.Throws<SaveMigrationException>(() => m.MigrateToVersion(Envelope(5, "x"), 3));
        }

        [Test]
        public void Overshooting_migration_throws()
        {
            var m = new SaveMigrator();
            m.Register(new AppendMigration(1, 4, "+jump"));
            Assert.Throws<SaveMigrationException>(() => m.MigrateToVersion(Envelope(1, "x"), 3));
        }

        [Test]
        public void Duplicate_or_non_increasing_registrations_are_rejected()
        {
            var m = new SaveMigrator();
            m.Register(new AppendMigration(1, 2, "a"));
            Assert.Throws<System.ArgumentException>(() => m.Register(new AppendMigration(1, 3, "b")));
            Assert.Throws<System.ArgumentException>(() => m.Register(new AppendMigration(4, 4, "c")));
        }

        [Test]
        public void Encounter_format_6_to_7_adds_an_empty_discoveries_list_and_keeps_everything_else()
        {
            var step = new AddDiscoveriesMigration();
            Assert.AreEqual(6, step.FromVersion); Assert.AreEqual(7, step.ToVersion);
            string v6 = SaveFixtures.V6Payload, v7 = step.Migrate(v6);
            Assert.AreEqual(v6.Substring(0, v6.Length - 1) + ",\"discoveries\":[]}", v7, "Every character of the v6 payload is kept; the list goes in before the closing brace.");
            Assert.AreEqual(v7, step.Migrate(v7), "A payload that has the list is left alone.");
            Assert.AreEqual("{\"discoveries\":[]}", step.Migrate("{}"));
            Assert.AreEqual("{ \"a\" : 1,\"discoveries\":[]}", step.Migrate("  { \"a\" : 1 }\n"));
            Assert.Throws<SaveMigrationException>(() => step.Migrate("[1,2]"));
            Assert.Throws<SaveMigrationException>(() => step.Migrate(""));

            var m = new SaveMigrator(); m.Register(step);
            var input = new SaveEnvelope { formatVersion = 6, payloadType = "CrulandaEncounter", payloadJson = v6 };
            var result = m.MigrateToVersion(input, EncounterSave.FormatVersion);
            Assert.AreEqual(7, result.formatVersion); Assert.AreEqual(v7, result.payloadJson);
            Assert.AreEqual(6, input.formatVersion); Assert.AreEqual(v6, input.payloadJson);
            Assert.Throws<SaveMigrationException>(() => m.MigrateToVersion(new SaveEnvelope { formatVersion = 5, payloadJson = v6 }, 7), "Formats before 6 are not in this chain (EncounterSave upgrades them in memory).");
        }
    }
}
