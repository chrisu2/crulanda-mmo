using System;
using System.Collections.Generic;

namespace Crulanda.Encounter
{
    [Serializable]
    public sealed class EnemyRecord
    {
        public string id;
        public bool dead;
        public bool looted;
    }

    [Serializable]
    public sealed class EncounterProgress
    {
        public string classId = "class.warrior";
        /// <summary>Zone the saved position belongs to. Empty = the original Quiet Trail test map.</summary>
        public string zoneId;
        public List<TalentRank> talents = new List<TalentRank>();
        public string playerId;
        public string companionId;
        public int experience;
        public int health = 180;
        public int mana = 120;
        public int companionHealth = 130;
        public float x, y = 1.1f, z = -13;
        public bool recruited;
        public int relationship;
        public int gold;
        /// <summary>Formats 1-5 only: the old single weapon slot and item list. Moved into bag/equipment on load (format 6).</summary>
        public string equippedItem;
        public List<string> inventory = new List<string>();
        /// <summary>Bag slots (Inventory.BagSize) and worn gear by EquipSlot. Format 6.</summary>
        public List<ItemStack> bag = new List<ItemStack>();
        public List<ItemStack> equipment = new List<ItemStack>();
        public List<EnemyRecord> enemies = new List<EnemyRecord>();
        // ---------- quests (save format 4; older saves load with these empty) ----------
        public List<QuestState> quests = new List<QuestState>();
        public List<string> questsDone = new List<string>();
        public List<FactionStanding> reputation = new List<FactionStanding>();
        /// <summary>Chronicle pages found (the ledger, the lullaby...).</summary>
        public List<string> documents = new List<string>();
        /// <summary>Quest bag: one entry per item carried (repeats = stack).</summary>
        public List<string> questItems = new List<string>();
        /// <summary>One-shot interactables already emptied ("zone|name|x|z"), e.g. a crate you took the iron from.</summary>
        public List<string> usedInteractables = new List<string>();
        // ---------- discoveries (save format 7; older saves load with this empty) ----------
        /// <summary>Hidden finds already found, by id ("secret.&lt;zone&gt;.&lt;slug&gt;"): each pays out once, ever. See DiscoveryLog.</summary>
        public List<string> discoveries = new List<string>();

        // ---------- levels (save format 5) ----------
        public const int LevelCap = 10;
        /// <summary>Experience from level to level+1: 200 at level 1, +90 per level (1,000+ by the late levels).</summary>
        public static int XpToNext(int level) { return 200 + 90 * (Math.Max(1, level) - 1); }
        /// <summary>Total experience at which a level begins (level 1 = 0).</summary>
        public static int XpForLevel(int level)
        {
            int total = 0; for (int l = 1; l < Math.Min(level, LevelCap); l++) total += XpToNext(l);
            return total;
        }
        public int Level
        {
            get { int l = 1; while (l < LevelCap && experience >= XpForLevel(l + 1)) l++; return l; }
        }
        /// <summary>Experience into the current level, and the size of the level.</summary>
        public int XpIntoLevel { get { return experience - XpForLevel(Level); } }
        public int XpLevelSize { get { return Level >= LevelCap ? 1 : XpToNext(Level); } }
        /// <summary>
        /// Experience for a kill. Base 20 + 8 per mob level. Mobs above you give a little more, and mobs below give
        /// less; 4+ levels below give nothing (grey). Elites give double.
        /// </summary>
        public static int KillXp(int mobLevel, int playerLevel, bool elite)
        {
            int diff = mobLevel - playerLevel;
            float mult = diff >= 3 ? 1.25f : diff == 2 ? 1.15f : diff == 1 ? 1.1f : diff == 0 ? 1 : diff == -1 ? .8f : diff == -2 ? .55f : diff == -3 ? .3f : 0;
            return (int)Math.Round((20 + 8 * mobLevel) * mult * (elite ? 2 : 1));
        }
        /// <summary>Old saves (formats 1-4, 60 XP per level): keep the same level and the same fraction of it under the new curve.</summary>
        public static int MigrateExperience(int oldXp)
        {
            int oldLevel = Math.Min(LevelCap, 1 + Math.Max(0, oldXp) / 60);
            float frac = oldLevel >= LevelCap ? 0 : (Math.Max(0, oldXp) % 60) / 60f;
            return XpForLevel(oldLevel) + (int)Math.Round(frac * XpToNext(oldLevel));
        }

        /// <summary>A story enemy (saved) is killed: records it and grants experience once.</summary>
        public bool AwardKill(string id, int xp)
        {
            var record = FindEnemy(id);
            if (record.dead) return false;
            record.dead = true;
            experience += Math.Max(0, xp);
            return true;
        }

        /// <summary>Searches a dead story enemy once: 8 gold (the session adds any item to the bag).</summary>
        public bool Loot(string id)
        {
            var record = FindEnemy(id);
            if (!record.dead || record.looted) return false;
            record.looted = true;
            gold += 8;
            return true;
        }

        public EnemyRecord FindEnemy(string id)
        {
            var found = enemies.Find(e => e.id == id);
            if (found != null) return found;
            found = new EnemyRecord { id = id };
            enemies.Add(found);
            return found;
        }
    }

    /// <summary>Per-enemy threat. Taunt expires independently from accumulated threat.</summary>
    public sealed class EncounterThreat
    {
        readonly Dictionary<string, float> values = new Dictionary<string, float>();
        string forced;
        float until;
        public void Add(string id, float amount)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return;
            values.TryGetValue(id, out var current);
            values[id] = current + amount;
        }
        public void AddProximity(string id, float elapsedSeconds) { Add(id, Math.Max(0, elapsedSeconds)); }
        public void Taunt(string id, float now, float duration = 3)
        {
            float max = 0;
            foreach (var value in values.Values) max = Math.Max(max, value);
            values[id] = max + 1;
            forced = id;
            until = now + Math.Max(0, duration);
        }
        public string Choose(float now, Func<string, bool> alive)
        {
            if (now < until && forced != null && alive(forced)) return forced;
            string best = null;
            float score = -1;
            foreach (var entry in values)
                if (entry.Value > score && alive(entry.Key)) { best = entry.Key; score = entry.Value; }
            return best;
        }
        public void Clear() { values.Clear(); forced = null; until = 0; }
    }
}

