using System;
using System.Collections.Generic;

namespace Crulanda.Persistence
{
    public sealed class SaveMigrationException : Exception
    {
        public SaveMigrationException(string message) : base(message) { }
    }

    /// <summary>One step that upgrades a payload from one format version to a later one.</summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }
        string Migrate(string payloadJson);
    }

    /// <summary>Chains registered migrations to bring an envelope up to a target format version.</summary>
    public sealed class SaveMigrator
    {
        readonly Dictionary<int, ISaveMigration> _byFromVersion = new Dictionary<int, ISaveMigration>();

        public void Register(ISaveMigration migration)
        {
            if (migration == null) throw new ArgumentNullException("migration");
            if (migration.ToVersion <= migration.FromVersion)
                throw new ArgumentException("Migration must increase the version.");
            if (_byFromVersion.ContainsKey(migration.FromVersion))
                throw new ArgumentException("A migration from version " + migration.FromVersion + " is already registered.");
            _byFromVersion.Add(migration.FromVersion, migration);
        }

        /// <summary>Returns a migrated copy; the input is not modified.</summary>
        public SaveEnvelope MigrateToVersion(SaveEnvelope envelope, int targetVersion)
        {
            if (envelope == null) throw new ArgumentNullException("envelope");

            if (envelope.formatVersion > targetVersion)
                throw new SaveMigrationException("Save format " + envelope.formatVersion +
                    " is newer than this game supports (" + targetVersion + "). Update the game.");

            var result = envelope.Clone();
            while (result.formatVersion < targetVersion)
            {
                ISaveMigration step;
                if (!_byFromVersion.TryGetValue(result.formatVersion, out step))
                    throw new SaveMigrationException("No migration registered from save format " + result.formatVersion + ".");

                if (step.ToVersion > targetVersion)
                    throw new SaveMigrationException("Migration from " + step.FromVersion + " overshoots target " + targetVersion + ".");

                result.payloadJson = step.Migrate(result.payloadJson);
                result.formatVersion = step.ToVersion;
            }
            return result;
        }
    }
}
