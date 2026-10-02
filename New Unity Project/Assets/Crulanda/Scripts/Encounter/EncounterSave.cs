using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Crulanda.Persistence;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Encounter save envelope. Format 3 stores data-driven talent ids (EncounterContent/Talents/*.json); format 4 adds
    /// quests, faction standing, Chronicle pages and the quest bag (empty when loading 1-3; the Chronicle catches up from
    /// what the character has already done). v1 (no talents) and v2 (9-node prototype ids) migrate on read; the migrated
    /// form is written on the next save. From format 6 on, each format step is a SaveMigrator step on the payload text
    /// (6 → 7: <see cref="AddDiscoveriesMigration"/>, 7 → 8: <see cref="AddProfessionsMigration"/>, 8 → 9: <see cref="AddArmouryMigration"/>),
    /// run before the payload is read.
    /// </summary>
    public sealed class EncounterSave
    {
        public const int FormatVersion = 9;   // 5: level curve (experience migrated). 6: bag + equipment slots (old item list moved in). 7: discoveries. 8: trades (professions, pouches). 9: the Armoury (armoury, looks, lootLuck)
        /// <summary>The most trade bags a save may list as worn.</summary>
        public const int MaxPouches = 8;
        /// <summary>The payload steps from format 6 on. Formats 1-5 are older than this chain and are upgraded in memory in Read.</summary>
        static readonly SaveMigrator Steps = CreateSteps();
        static SaveMigrator CreateSteps() { var m = new SaveMigrator(); m.Register(new AddDiscoveriesMigration()); m.Register(new AddProfessionsMigration()); m.Register(new AddArmouryMigration()); return m; }
        /// <summary>9-node prototype ids (format 2) -> the nodes they became in the data-driven Warrior tree.</summary>
        public static readonly Dictionary<string, string> LegacyTalentIds = new Dictionary<string, string>(StringComparer.Ordinal) {
            { "tank.armor", "tk-tempered-armor" }, { "tank.challenge", "tk-steady-challenge" }, { "tank.bulwark", "tk-bulwark" },
            { "dps.power", "dp-weapon-pressure" }, { "dps.execute", "dp-finishing-strike" }, { "dps.cleave", "dp-sweeping-strike" },
            { "support.reserve", "sp-rally-reserve" }, { "support.challenge", "sp-rallying-challenge" }, { "support.shelter", "sp-shared-shelter" }
        };
        readonly SaveFileStore store;
        readonly TalentTree talents;
        readonly string slot;
        /// <summary>One expedition save per class. Warrior keeps the original "encounter" slot so older saves stay put.</summary>
        public static string SlotFor(string classId)
        {
            return classId == "class.warrior" ? "encounter" : "encounter-" + (classId ?? "").Replace("class.", "");
        }
        public EncounterSave(string root, TalentTree talents, string slot = "encounter")
        {
            store = new SaveFileStore(root);
            this.talents = talents ?? throw new ArgumentNullException(nameof(talents));
            this.slot = string.IsNullOrWhiteSpace(slot) ? throw new ArgumentException("Save slot missing.") : slot;
        }
        public void Write(EncounterProgress progress)
        {
            if (!talents.Validate(progress, out var reason)) throw new InvalidOperationException(reason);
            store.Write(slot, new SaveEnvelope {
                formatVersion = FormatVersion, payloadType = "CrulandaEncounter", gameVersion = "0.3.0",
                savedAtUtc = DateTime.UtcNow.ToString("O"), payloadJson = JsonUtility.ToJson(progress)
            });
        }
        public bool Read(out EncounterProgress progress, out string message)
        {
            progress = null;
            if (!store.TryRead(slot, out var envelope, out message)) return false;
            if (envelope.formatVersion < 1 || envelope.formatVersion > FormatVersion || envelope.payloadType != "CrulandaEncounter")
            { message = "Unsupported save format; the save has not been changed."; return false; }
            int from = envelope.formatVersion;
            try
            {
                // Format 6 and later: the payload text is brought up to date step by step (in memory; the file is untouched).
                if (from >= 6) envelope = Steps.MigrateToVersion(envelope, FormatVersion);
                var p = JsonUtility.FromJson<EncounterProgress>(envelope.payloadJson);
                if (p == null || !Guid.TryParse(p.playerId, out _) || !Guid.TryParse(p.companionId, out _) ||
                    p.enemies == null || p.experience < 0 || p.gold < 0 ||
                    float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) ||
                    float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z) ||
                    Math.Abs(p.x) > 500 || Math.Abs(p.z) > 500 || p.y < -60 || p.y > 60)   // (a cave's floor may lie well under the land)
                    throw new InvalidOperationException("Invalid encounter data.");
                // Level curve changed in format 5: convert experience first so every level-based check sees the same level.
                if (from < 5) p.experience = EncounterProgress.MigrateExperience(p.experience);
                // Keep any store note (e.g. restored from backup) unless a migration has something more important to say.
                if (from == 1)
                {
                    p.classId = "class.warrior"; p.talents = new List<TalentRank>();
                    message = "Legacy expedition upgraded: Warrior talent points are available [B].";
                }
                else if (from == 2) message = MigrateV2(p);
                // Quest lists are absent before format 4 (JsonUtility leaves them null or empty): start them empty.
                if (p.quests == null) p.quests = new List<QuestState>();
                if (p.questsDone == null) p.questsDone = new List<string>();
                if (p.reputation == null) p.reputation = new List<FactionStanding>();
                if (p.documents == null) p.documents = new List<string>();
                if (p.questItems == null) p.questItems = new List<string>();
                if (p.usedInteractables == null) p.usedInteractables = new List<string>();
                foreach (var q in p.quests) if (q == null || string.IsNullOrEmpty(q.id) || q.step < 0) throw new InvalidOperationException("Invalid quest data.");
                foreach (var q in p.quests) if (q.counts == null) q.counts = new List<int>();
                // Format 7: hidden finds already found. Formats 1-5 have no list; a blank id can't be a find, so it is dropped.
                if (p.discoveries == null) p.discoveries = new List<string>();
                p.discoveries.RemoveAll(string.IsNullOrEmpty);
                // Format 8: trades learned and trade bags worn. Formats 1-5 have neither list. A blank id is nothing and is dropped,
                // and so is a second entry for an id already listed. A trade this content doesn't know is kept (ProfessionLog
                // ignores it), but a skill outside 0-100 or more worn bags than there could be is not a save this game wrote.
                if (p.professions == null) p.professions = new List<ProfessionSkill>();
                p.professions.RemoveAll(s => s == null || string.IsNullOrEmpty(s.id));
                foreach (var s in p.professions) if (s.skill < 0 || s.skill > ProfessionDatabase.MaxSkill) throw new InvalidOperationException("Invalid profession data.");
                var known = new HashSet<string>(StringComparer.Ordinal);
                p.professions.RemoveAll(s => !known.Add(s.id));
                if (p.pouches == null) p.pouches = new List<string>();
                p.pouches.RemoveAll(string.IsNullOrEmpty);
                var worn = new HashSet<string>(StringComparer.Ordinal);
                p.pouches.RemoveAll(id => !worn.Add(id));
                if (p.pouches.Count > MaxPouches) throw new InvalidOperationException("Invalid item data.");
                // Format 9: the Armoury (named gear found, looks seen) and the loot kill counts. Formats 1-8 have none of the three.
                // A blank id or source is nothing and is dropped, and so is a later entry for one already listed. An id this content
                // doesn't know is kept (ArmouryLog ignores it), but a negative count is not a save this game wrote.
                if (p.armoury == null) p.armoury = new List<string>();
                if (p.looks == null) p.looks = new List<string>();
                if (p.lootLuck == null) p.lootLuck = new List<LootLuck>();
                foreach (var list in new[] { p.armoury, p.looks }) { list.RemoveAll(string.IsNullOrEmpty); var once = new HashSet<string>(StringComparer.Ordinal); list.RemoveAll(id => !once.Add(id)); }
                p.lootLuck.RemoveAll(l => l == null || string.IsNullOrEmpty(l.source));
                foreach (var l in p.lootLuck) if (l.kills < 0 || l.dry < 0) throw new InvalidOperationException("Invalid loot data.");
                var sources = new HashSet<string>(StringComparer.Ordinal);
                p.lootLuck.RemoveAll(l => !sources.Add(l.source));
                // Format 6: the old item list and single weapon become bag slots and the main-hand slot.
                Inventory.Ensure(p);
                if (from < 6)
                {
                    if (p.inventory != null)
                        foreach (var id in p.inventory)
                        {
                            if (string.IsNullOrEmpty(id) || id == p.equippedItem) continue;
                            int free = p.bag.FindIndex(s => s.Empty); if (free >= 0) p.bag[free] = new ItemStack { item = id, count = 1 };
                        }
                    if (!string.IsNullOrEmpty(p.equippedItem)) p.equipment[(int)EquipSlot.MainHand] = new ItemStack { item = p.equippedItem, count = 1 };
                    p.inventory = new List<string>(); p.equippedItem = null;
                }
                if (p.bag.Count > Inventory.BagSize * 4 || p.equipment.Count != ItemDatabase.SlotIds.Length) throw new InvalidOperationException("Invalid item data.");
                if (!talents.Validate(p, out var reason)) throw new InvalidOperationException(reason);
                progress = p;
                return true;
            }
            catch (Exception e) { message = e.Message; return false; }
        }
        string MigrateV2(EncounterProgress p)
        {
            if (p.classId != talents.ClassId || p.talents == null) throw new InvalidOperationException("Unsupported class or missing talent allocation.");
            var mapped = new List<TalentRank>();
            foreach (var t in p.talents)
            {
                if (t == null || !LegacyTalentIds.TryGetValue(t.id ?? "", out var id)) throw new InvalidOperationException("Invalid or unknown legacy talent.");
                mapped.Add(new TalentRank { id = id, rank = t.rank });
            }
            p.talents = mapped;
            if (talents.Validate(p, out _)) return "Talent tree expanded: your choices carried over. New talents are available [B].";
            p.talents = new List<TalentRank>();
            return "Talent tree expanded and its tier rules changed: your talent points were refunded [B]. Nothing else changed.";
        }
    }

    /// <summary>
    /// Save format 6 → 7: adds an empty <c>discoveries</c> list (hidden finds, DiscoveryLog). An edit on the payload text: the list
    /// goes in before the closing brace and every other character of the format-6 payload stays exactly as it was. A payload that
    /// already has the list is left alone; one that isn't a JSON object is refused (the save is then not loaded, not changed).
    /// </summary>
    public sealed class AddDiscoveriesMigration : ISaveMigration
    {
        public int FromVersion { get { return 6; } }
        public int ToVersion { get { return 7; } }
        static readonly Regex HasList = new Regex("\"discoveries\"\\s*:");
        public string Migrate(string payloadJson)
        {
            string json = (payloadJson ?? "").Trim();
            if (json.Length < 2 || json[0] != '{' || json[json.Length - 1] != '}') throw new SaveMigrationException("The save's payload is not a JSON object.");
            if (HasList.IsMatch(json)) return payloadJson;
            string body = json.Substring(0, json.Length - 1).TrimEnd();
            return body + (body.EndsWith("{") ? "" : ",") + "\"discoveries\":[]}";
        }
    }

    /// <summary>
    /// Save format 7 → 8: adds the two empty lists of the trades, <c>professions</c> (skills learned, ProfessionLog) and
    /// <c>pouches</c> (trade bags worn). An edit on the payload text, like <see cref="AddDiscoveriesMigration"/>: each list that is
    /// missing goes in before the closing brace, in that order, and every other character of the format-7 payload stays exactly
    /// as it was. A payload that already has both is left alone; one that isn't a JSON object is refused (the save is then not
    /// loaded, not changed).
    /// </summary>
    public sealed class AddProfessionsMigration : ISaveMigration
    {
        public int FromVersion { get { return 7; } }
        public int ToVersion { get { return 8; } }
        static readonly Regex HasProfessions = new Regex("\"professions\"\\s*:"), HasPouches = new Regex("\"pouches\"\\s*:");
        public string Migrate(string payloadJson)
        {
            string json = (payloadJson ?? "").Trim();
            if (json.Length < 2 || json[0] != '{' || json[json.Length - 1] != '}') throw new SaveMigrationException("The save's payload is not a JSON object.");
            bool professions = HasProfessions.IsMatch(json), pouches = HasPouches.IsMatch(json);
            if (professions && pouches) return payloadJson;
            string body = json.Substring(0, json.Length - 1).TrimEnd();
            if (!professions) body += (body.EndsWith("{") ? "" : ",") + "\"professions\":[]";
            if (!pouches) body += (body.EndsWith("{") ? "" : ",") + "\"pouches\":[]";
            return body + "}";
        }
    }

    /// <summary>
    /// Save format 8 → 9: adds the three empty lists of the Armoury, <c>armoury</c> (named gear found), <c>looks</c> (appearances
    /// seen) and <c>lootLuck</c> (loot kill counts and pity). An edit on the payload text, like <see cref="AddProfessionsMigration"/>:
    /// each list that is missing goes in before the closing brace, in that order, and every other character of the format-8 payload
    /// stays exactly as it was. A payload that already has all three is left alone; one that isn't a JSON object is refused (the save
    /// is then not loaded, not changed). What the character already holds is marked found when the Armoury first binds to it
    /// (ArmouryLog.Bind), not here.
    /// </summary>
    public sealed class AddArmouryMigration : ISaveMigration
    {
        public int FromVersion { get { return 8; } }
        public int ToVersion { get { return 9; } }
        static readonly string[] Lists = { "armoury", "looks", "lootLuck" };
        static readonly Regex[] Has = { new Regex("\"armoury\"\\s*:"), new Regex("\"looks\"\\s*:"), new Regex("\"lootLuck\"\\s*:") };
        public string Migrate(string payloadJson)
        {
            string json = (payloadJson ?? "").Trim();
            if (json.Length < 2 || json[0] != '{' || json[json.Length - 1] != '}') throw new SaveMigrationException("The save's payload is not a JSON object.");
            bool all = true; foreach (var h in Has) all &= h.IsMatch(json);
            if (all) return payloadJson;
            string body = json.Substring(0, json.Length - 1).TrimEnd();
            for (int i = 0; i < Lists.Length; i++) if (!Has[i].IsMatch(json)) body += (body.EndsWith("{") ? "" : ",") + "\"" + Lists[i] + "\":[]";
            return body + "}";
        }
    }
}
