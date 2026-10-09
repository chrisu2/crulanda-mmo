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
        // ---------- notice boards (2026-10-03; older saves load with zero days and no boards, and draw on first use) ----------
        /// <summary>Game days passed (the clock crossing six in the morning while playing): what the boards draw by.</summary>
        public int days;
        /// <summary>Round 27: the guild you are in ("" none; SimGuilds). Older saves load with none.</summary>
        public string guild = "";
        /// <summary>Each zone's notice board: the day it was drawn, its postings, the ones handed in today. See Bounties.</summary>
        public List<BoardState> boards = new List<BoardState>();
        // ---------- points of interest and achievements (2026-10-03; older saves load with these empty and record past deeds) ----------
        /// <summary>Places explored, "zone.id|landmark name": each paid its exploration experience once. See Achievements.</summary>
        public List<string> explored = new List<string>();
        /// <summary>Achievements earned, by id ("explore.oakhaven", "bounty.10"...), in the order earned.</summary>
        public List<string> achievements = new List<string>();
        /// <summary>The title worn (an earned achievement's), or empty.</summary>
        public string title;
        /// <summary>Notice-board bounties handed in, and rare postings seen through.</summary>
        public int bounties, rares;
        /// <summary>Camp elites killed, by camp ("mob.captain.peaks.3").</summary>
        public List<string> elitesSlain = new List<string>();
        /// <summary>Keys kept for good (dungeon step D3; older saves load with none; here, not at the end, so older payloads still end as they did): the Sealed Adit's rail sigils ("sigil.amber",
        /// "sigil.ember", "sigil.grey", taken from its branch bosses) and "adit.lift" once they are set in the cage-lift's frame.</summary>
        public List<string> keys = new List<string>();
        /// <summary>The spirit stones touched (ZoneInteractable.Key): where you wake in that zone after a fall (D3).</summary>
        public List<string> spiritStones = new List<string>();
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
        // ---------- trades (save format 8; older saves load with both empty) ----------
        /// <summary>Trades learned and the skill in each (1-100): gathering skills, Cooking and crafts. See ProfessionLog.</summary>
        public List<ProfessionSkill> professions = new List<ProfessionSkill>();
        /// <summary>Trade bags worn, by item id, in the order they were put on. Each adds its slots to the end of <see cref="bag"/>.</summary>
        public List<string> pouches = new List<string>();
        // ---------- the Armoury (save format 9; older saves load with all three empty) ----------
        /// <summary>Named gear ever found (in the bags or worn at least once), by item id, in the order found. See ArmouryLog.</summary>
        public List<string> armoury = new List<string>();
        /// <summary>Appearances ever held, by appearance key (GearLooks.AppearanceKey: no colours, quality or tier), in the order first seen. Each gave a "NEW LOOK" toast.</summary>
        public List<string> looks = new List<string>();
        /// <summary>
        /// Kill counts by loot source: a drop list id (its kills, which name its pieces in the Armoury) or a drop list id and group
        /// ("drop.oak.caddock#1": an epic's kills and the current run without it, for pity). See LootDatabase.Roll and ArmouryLog.
        /// </summary>
        public List<LootLuck> lootLuck = new List<LootLuck>();

        // ---------- levels (save format 5) ----------
        /// <summary>The highest level (13 since the Verdant Shore, 2026-09-30; it was 10 for the first four zones). Talent points stop
        /// at <see cref="TalentCap"/>: the trees were designed for ten levels' worth, so the last levels bring health and hit, not points.</summary>
        public const int LevelCap = 15;   // 15 now, 30 in the end (Chris, 2026-10-07; ROADMAP: Phase 9)
        public const int TalentCap = 10;
        /// <summary>Experience from level to level+1: 200 at level 1, +90 per level (1,000+ by the late levels).</summary>
        public static int XpToNext(int level) { return 400 + 170 * (Math.Max(1, level) - 1); }   // round 29: "difficult and slow leveling, grind it out" (was 200 + 90 a level)
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
            int oldLevel = Math.Min(10, 1 + Math.Max(0, oldXp) / 60);   // the old curve's cap was 10
            float frac = oldLevel >= 10 ? 0 : (Math.Max(0, oldXp) % 60) / 60f;
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
        public void Remove(string id) { if (id != null) values.Remove(id); if (forced == id) { forced = null; until = 0; } }
        public bool IsEmpty { get { return values.Count == 0; } }
    }
}

