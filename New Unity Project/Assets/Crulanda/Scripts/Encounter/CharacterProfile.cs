using System;
using UnityEngine;
using Crulanda.Persistence;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Remembers which character (class slot) was played last. Each class keeps its own expedition save
    /// (see EncounterSave.SlotFor); this file only chooses which one to open. Unreadable profiles are ignored.
    /// </summary>
    public static class CharacterProfile
    {
        [Serializable] sealed class Data { public string lastClass; }
        const string Key = "profile";

        public static string LastClass(string root)
        {
            try
            {
                if (!new SaveFileStore(root).TryRead(Key, out var envelope, out _) || envelope.payloadType != "CrulandaProfile") return null;
                return JsonUtility.FromJson<Data>(envelope.payloadJson)?.lastClass;
            }
            catch (Exception) { return null; }
        }

        public static void Remember(string root, string classId)
        {
            try
            {
                new SaveFileStore(root).Write(Key, new SaveEnvelope { formatVersion = 1, payloadType = "CrulandaProfile", gameVersion = "0.3.0",
                    savedAtUtc = DateTime.UtcNow.ToString("O"), payloadJson = JsonUtility.ToJson(new Data { lastClass = classId }) });
            }
            catch (Exception e) { Debug.LogWarning("Could not remember character: " + e.Message); }
        }
    }
}
