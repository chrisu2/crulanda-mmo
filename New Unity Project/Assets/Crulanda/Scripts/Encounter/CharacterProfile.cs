using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Crulanda.Persistence;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The profile save (profile.save.json, beside the characters' saves): which character (class slot) was played last, and since
    /// format 2 (2026-10-15, the capital's bank) the Treasury Vault every character shares. Each class keeps its own expedition save
    /// (see EncounterSave.SlotFor); this file chooses which one to open and holds what they share. Choosing a character keeps the
    /// vault; an unreadable profile is ignored for the choice, and the vault is not opened from it (so it is never written over).
    /// </summary>
    public static class CharacterProfile
    {
        [Serializable] sealed class Data
        {
            public string lastClass;
            /// <summary>Format 2: the vault's slots (the list index is the slot) and the rows of 12 bought past the first 48.</summary>
            public List<ItemStack> vault = new List<ItemStack>();
            public int vaultRows;
        }
        const string Key = "profile";
        public const string PayloadType = "CrulandaProfile";
        /// <summary>1: lastClass. 2 (2026-10-15): the Treasury Vault (vault, vaultRows); <see cref="AddVaultMigration"/>.</summary>
        public const int FormatVersion = 2;
        static SaveMigrator CreateSteps() { var m = new SaveMigrator(); m.Register(new AddVaultMigration()); return m; }

        /// <summary>Reads the profile, migrated to this format. False (and why) when it is missing, unreadable, of another type or newer.</summary>
        static bool TryLoad(string root, out Data data, out string message, out bool missing)
        {
            data = null; message = null; missing = false;
            try
            {
                var store = new SaveFileStore(root);
                if (!store.Exists(Key) && !System.IO.File.Exists(store.PathFor(Key) + ".bak")) { missing = true; return false; }
                if (!store.TryRead(Key, out var envelope, out message)) return false;
                data = Read(envelope, out message);
                return data != null;
            }
            catch (Exception e) { message = e.Message; return false; }
        }
        static Data Read(SaveEnvelope envelope, out string message)
        {
            message = null;
            if (envelope == null || envelope.payloadType != PayloadType) { message = "Not a profile."; return null; }
            if (envelope.formatVersion < 1) { message = "Unsupported profile."; return null; }
            try
            {
                var migrated = CreateSteps().MigrateToVersion(envelope, FormatVersion);
                var d = JsonUtility.FromJson<Data>(migrated.payloadJson);
                if (d == null) { message = "Empty profile."; return null; }
                if (d.vault == null) d.vault = new List<ItemStack>();
                foreach (var s in d.vault) if (s != null && s.count < 0) { message = "Invalid vault data."; return null; }
                if (d.vaultRows < 0 || d.vaultRows > TreasuryVault.MaxBoughtRows) { message = "Invalid vault data."; return null; }
                return d;
            }
            catch (Exception e) { message = e.Message; return null; }
        }
        static void Write(string root, Data d)
        {
            new SaveFileStore(root).Write(Key, new SaveEnvelope { formatVersion = FormatVersion, payloadType = PayloadType, gameVersion = "0.3.0",
                savedAtUtc = DateTime.UtcNow.ToString("O"), payloadJson = JsonUtility.ToJson(d) });
        }

        public static string LastClass(string root)
        {
            try
            {
                // The choice reads from any profile format (format 1 is lastClass alone, and format 2 keeps it under the same name).
                if (!new SaveFileStore(root).TryRead(Key, out var envelope, out _) || envelope.payloadType != PayloadType) return null;
                return JsonUtility.FromJson<Data>(envelope.payloadJson)?.lastClass;
            }
            catch (Exception) { return null; }
        }

        /// <summary>Remembers the character chosen, keeping the vault. A profile from a newer build is left alone.</summary>
        public static void Remember(string root, string classId)
        {
            try
            {
                if (!TryLoad(root, out var d, out var message, out bool missing))
                {
                    if (!missing && Newer(root)) return;   // a newer build's profile: not written over
                    d = new Data();   // missing or unreadable: a fresh profile, as before (an unreadable vault could not be opened anyway)
                }
                d.lastClass = classId; Write(root, d);
            }
            catch (Exception e) { Debug.LogWarning("Could not remember character: " + e.Message); }
        }
        static bool Newer(string root)
        {
            try { return new SaveFileStore(root).TryRead(Key, out var env, out _) && env.payloadType == PayloadType && env.formatVersion > FormatVersion; }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// The vault, from the profile (an older profile, or none, gives an empty vault of 48 slots). Null, with why, when the profile
        /// is there but cannot be read or is newer: the vault is then not opened, so nothing in it is written over.
        /// </summary>
        public static VaultData LoadVault(string root, out string message)
        {
            if (!TryLoad(root, out var d, out message, out bool missing))
            {
                if (missing) { message = null; return TreasuryVault.Ensure(new VaultData { shared = true }); }
                return null;
            }
            return TreasuryVault.Ensure(new VaultData { slots = d.vault, rowsBought = d.vaultRows, shared = true });
        }
        /// <summary>Writes the vault into the profile, keeping the character choice. False (and why) when the profile is there but
        /// cannot be read or is newer (it is then left as it is).</summary>
        public static bool SaveVault(string root, VaultData vault, out string message)
        {
            message = null;
            try
            {
                if (!TryLoad(root, out var d, out message, out bool missing)) { if (!missing) return false; d = new Data(); message = null; }
                vault = TreasuryVault.Ensure(vault);
                d.vault = new List<ItemStack>(); foreach (var s in vault.slots) d.vault.Add(new ItemStack { item = s.Empty ? "" : s.item, count = s.Empty ? 0 : s.count });
                d.vaultRows = vault.rowsBought;
                Write(root, d); return true;
            }
            catch (Exception e) { message = e.Message; return false; }
        }
    }

    /// <summary>
    /// Profile format 1 → 2 (2026-10-15, the Treasury Vault): inserts <c>"vault":[]</c> and <c>"vaultRows":0</c> before the payload's
    /// closing brace and leaves every other character of the format-1 payload as it was (so <c>lastClass</c> comes through unchanged).
    /// A field already there is left alone. A payload that is not a JSON object is refused.
    /// </summary>
    public sealed class AddVaultMigration : ISaveMigration
    {
        public int FromVersion { get { return 1; } }
        public int ToVersion { get { return 2; } }
        static readonly string[] Fields = { "\"vault\":[]", "\"vaultRows\":0" };
        static readonly Regex[] Has = { new Regex("\"vault\"\\s*:"), new Regex("\"vaultRows\"\\s*:") };
        public string Migrate(string payloadJson)
        {
            string json = (payloadJson ?? "").Trim();
            if (json.Length < 2 || json[0] != '{' || json[json.Length - 1] != '}') throw new SaveMigrationException("The profile's payload is not a JSON object.");
            bool all = true; foreach (var h in Has) all &= h.IsMatch(json);
            if (all) return payloadJson;
            string body = json.Substring(0, json.Length - 1).TrimEnd();
            for (int i = 0; i < Fields.Length; i++) if (!Has[i].IsMatch(json)) body += (body.EndsWith("{") ? "" : ",") + Fields[i];
            return body + "}";
        }
    }
}
