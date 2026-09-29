using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Persistence;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Encounter save envelope. Format 3 stores data-driven talent ids (EncounterContent/Talents/*.json); format 4 adds
    /// quests, faction standing, Chronicle pages and the quest bag (empty when loading 1-3; the Chronicle catches up from
    /// what the character has already done). v1 (no talents) and v2 (9-node prototype ids) migrate on read; the migrated
    /// form is written on the next save.
    /// </summary>
    public sealed class EncounterSave
    {
        public const int FormatVersion = 6;   // 5: level curve (experience migrated). 6: bag + equipment slots (old item list moved in)
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
            try
            {
                var p = JsonUtility.FromJson<EncounterProgress>(envelope.payloadJson);
                if (p == null || !Guid.TryParse(p.playerId, out _) || !Guid.TryParse(p.companionId, out _) ||
                    p.enemies == null || p.experience < 0 || p.gold < 0 ||
                    float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) ||
                    float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z) ||
                    Math.Abs(p.x) > 500 || Math.Abs(p.z) > 500 || p.y < -20 || p.y > 60)
                    throw new InvalidOperationException("Invalid encounter data.");
                // Level curve changed in format 5: convert experience first so every level-based check sees the same level.
                if (envelope.formatVersion < 5) p.experience = EncounterProgress.MigrateExperience(p.experience);
                // Keep any store note (e.g. restored from backup) unless a migration has something more important to say.
                if (envelope.formatVersion == 1)
                {
                    p.classId = "class.warrior"; p.talents = new List<TalentRank>();
                    message = "Legacy expedition upgraded: Warrior talent points are available [B].";
                }
                else if (envelope.formatVersion == 2) message = MigrateV2(p);
                // Quest lists are absent before format 4 (JsonUtility leaves them null or empty): start them empty.
                if (p.quests == null) p.quests = new List<QuestState>();
                if (p.questsDone == null) p.questsDone = new List<string>();
                if (p.reputation == null) p.reputation = new List<FactionStanding>();
                if (p.documents == null) p.documents = new List<string>();
                if (p.questItems == null) p.questItems = new List<string>();
                if (p.usedInteractables == null) p.usedInteractables = new List<string>();
                foreach (var q in p.quests) if (q == null || string.IsNullOrEmpty(q.id) || q.step < 0) throw new InvalidOperationException("Invalid quest data.");
                foreach (var q in p.quests) if (q.counts == null) q.counts = new List<int>();
                // Format 6: the old item list and single weapon become bag slots and the main-hand slot.
                Inventory.Ensure(p);
                if (envelope.formatVersion < 6)
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
}
