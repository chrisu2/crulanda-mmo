using System;

namespace Crulanda.Persistence
{
    /// <summary>
    /// Outer wrapper of every save file. The payload is stored as a JSON *string* so the envelope can be read
    /// and migrated before the payload's type is known (JsonUtility cannot round-trip arbitrary nested JSON).
    /// See Docs/SAVE_FORMAT.md. Field names are part of the file format; do not rename.
    /// </summary>
    [Serializable]
    public sealed class SaveEnvelope
    {
        /// <summary>Schema version of the payload. Bump when payload DTOs change and add an ISaveMigration.</summary>
        public int formatVersion;

        /// <summary>Which DTO the payload holds, e.g. "CharacterSaveData".</summary>
        public string payloadType;

        public string gameVersion;
        public string savedAtUtc;
        public string payloadJson;

        public SaveEnvelope Clone()
        {
            return (SaveEnvelope)MemberwiseClone();
        }
    }
}
