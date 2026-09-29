using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Crulanda.Persistence
{
    /// <summary>
    /// Reads and writes <see cref="SaveEnvelope"/> files in a directory. Writes are atomic (temp file then
    /// replace) and the previous file is kept as ".bak" so a crash mid-save cannot destroy a save.
    /// Pass Application.persistentDataPath + "/saves" as the root in the game; tests pass a temp folder.
    /// </summary>
    public sealed class SaveFileStore
    {
        const string Extension = ".save.json";
        const string BackupExtension = ".bak";

        readonly string _root;

        public SaveFileStore(string rootDirectory)
        {
            if (string.IsNullOrEmpty(rootDirectory)) throw new ArgumentException("rootDirectory is required.");
            _root = rootDirectory;
        }

        public static bool IsValidSlotName(string slot)
        {
            if (string.IsNullOrEmpty(slot) || slot.Length > 64) return false;
            for (int i = 0; i < slot.Length; i++)
            {
                char c = slot[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
                if (!ok) return false;
            }
            return true;
        }

        public string PathFor(string slot)
        {
            if (!IsValidSlotName(slot)) throw new ArgumentException("Invalid slot name: " + slot);
            return Path.Combine(_root, slot + Extension);
        }

        public bool Exists(string slot)
        {
            return File.Exists(PathFor(slot));
        }

        public void Write(string slot, SaveEnvelope envelope)
        {
            if (envelope == null) throw new ArgumentNullException("envelope");

            string path = PathFor(slot);
            Directory.CreateDirectory(_root);

            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(envelope, true));

            if (File.Exists(path)) File.Replace(temp, path, path + BackupExtension);
            else File.Move(temp, path);
        }

        /// <summary>Reads a slot, falling back to its ".bak" if the main file is missing or corrupt.</summary>
        public bool TryRead(string slot, out SaveEnvelope envelope, out string error)
        {
            string path = PathFor(slot);
            string primaryError;
            if (TryReadFile(path, out envelope, out primaryError))
            {
                error = null;
                return true;
            }

            string backupError;
            if (File.Exists(path + BackupExtension) && TryReadFile(path + BackupExtension, out envelope, out backupError))
            {
                error = "Primary save unreadable (" + primaryError + "); loaded backup.";
                return true;
            }

            envelope = null;
            error = primaryError;
            return false;
        }

        static bool TryReadFile(string path, out SaveEnvelope envelope, out string error)
        {
            envelope = null;
            try
            {
                if (!File.Exists(path)) { error = "File not found."; return false; }
                envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
                if (envelope == null || string.IsNullOrEmpty(envelope.payloadJson))
                {
                    envelope = null;
                    error = "File is empty or malformed.";
                    return false;
                }
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                envelope = null;
                error = ex.Message;
                return false;
            }
        }

        public void Delete(string slot)
        {
            string path = PathFor(slot);
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + BackupExtension)) File.Delete(path + BackupExtension);
        }

        public List<string> ListSlots()
        {
            var slots = new List<string>();
            if (!Directory.Exists(_root)) return slots;

            foreach (var file in Directory.GetFiles(_root, "*" + Extension))
            {
                string name = Path.GetFileName(file);
                slots.Add(name.Substring(0, name.Length - Extension.Length));
            }
            slots.Sort(StringComparer.Ordinal);
            return slots;
        }
    }
}
