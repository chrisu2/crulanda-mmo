using NUnit.Framework;
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
    }
}
