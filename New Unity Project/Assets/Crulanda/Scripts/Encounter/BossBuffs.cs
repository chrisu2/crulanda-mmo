using System;
using System.Collections.Generic;
using Crulanda.Combat;
using Crulanda.Core;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    // ============================================================================================
    // Buffing up before the bosses (Chris, 2026-10-15 ~23:25: "we should use food buffs, alchemy buffs, class buffs,etc to beat the
    // bosses"; Docs/PHASE8_6_LIVING_WORLD.md, "Buffs to beat the bosses"; PROVISIONAL numbers, GAME-ONLY names). Before a boss or an
    // elite camp the party's leader stops short of the pull and says "buff up": every sim eats (a party feast when one of them carries
    // one, else its own dish), drinks the elixir for its part (a tank's salve or Bear's-wort, a healer's Hearth-ease, a damage-dealer's
    // tincture of its main stat) and its class draught when low, and each lays its class's standing buff on the party; then the pull.
    // The rules are here, pure over the items and a status runtime so they are tested without a scene (BuffUpRulesTests); the stop is
    // SimCompanion's, your own buff-up (/buff, the run bot) the session's (EncounterSession.Buffs).
    // ============================================================================================

    /// <summary>
    /// A class's standing party buff, laid on the whole party at a buff-up (half an hour; a small share of one stat, as a PercentAdd).
    /// Each kit holds its own (ClassKit.PartyBuff: WarriorKit.Standing and the rest), named in the class's voice; GAME-ONLY. The
    /// classes' own party workings in a fight (the Colours, the songs, the verses) are not these and stay as they are.
    /// </summary>
    public sealed class ClassBuff
    {
        public readonly string classId, id, name; public readonly StatType stat; public readonly float share;
        public ClassBuff(string classId, string name, StatType stat)
        {
            this.classId = classId; this.name = name; this.stat = stat; share = BossBuffs.ClassBuffShare;
            id = "buff.class." + (classId != null && classId.StartsWith("class.") ? classId.Substring(6) : classId);
        }
        /// <summary>The status it is carried as: its id, half an hour, the stat raised by its share.</summary>
        public StatusEffectDefinition Definition()
        {
            return new StatusEffectDefinition { id = id, name = name, duration = BossBuffs.ClassBuffSeconds, incomingDamageMultiplier = 1,
                modifiers = new[] { new StatusModifierDefinition { stat = stat, operation = ModifierOp.PercentAdd, value = share } } };
        }
        /// <summary>The buff row's words ("+3% Armour").</summary>
        public string Line { get { return "+" + Math.Round(share * 100) + "% " + (stat == StatType.Armor ? "armour" : stat.ToString()); } }
    }

    /// <summary>Every class's standing buff, from its kit (ClassKit.PartyBuff), by class id.</summary>
    public static class ClassBuffs
    {
        public static readonly string[] ClassIds = { "class.warrior", "class.paladin", "class.druid", "class.ranger", "class.mage", "class.rogue",
            "class.archivist", "class.saltmender", "class.cleric", "class.scoundrel", "class.shaman" };
        public static ClassBuff For(string classId)
        {
            switch (classId)
            {
                case "class.warrior": return WarriorKit.Standing;
                case "class.paladin": return PaladinKit.Standing;
                case "class.druid": return DruidKit.Standing;
                case "class.ranger": return RangerKit.Standing;
                case "class.mage": return MageKit.Standing;
                case "class.rogue": return RogueKit.Standing;
                case "class.archivist": return ArchivistKit.Standing;
                case "class.saltmender": return SaltMenderKit.Standing;
                case "class.cleric": return ClericKit.Standing;
                case "class.scoundrel": return ScoundrelKit.Standing;
                case "class.shaman": return ShamanKit.Standing;
                default: return null;
            }
        }
        /// <summary>A class buff by its status id ("buff.class.paladin"), or null.</summary>
        public static ClassBuff ById(string statusId)
        {
            if (statusId == null || !statusId.StartsWith("buff.class.")) return null;
            foreach (var c in ClassIds) { var b = For(c); if (b != null && b.id == statusId) return b; }
            return null;
        }
        /// <summary>The buff row's name for a class buff's status, or null for an id that is not one.</summary>
        public static string StatusName(string statusId) { var b = ById(statusId); return b != null ? b.name : null; }
    }

    public static class BossBuffs
    {
        /// <summary>The leader stops this far short of a boss's or an elite camp's centre (the camp's mobs stand within 16 m of it and notice
        /// you at 5-8: out of its reach), no nearer than <see cref="StopMin"/>; it waits for the party to buff, <see cref="StopWait"/> at most.</summary>
        public const float StopShort = 30, StopMin = 25, StopWait = 12, StopAtLeast = 3;
        /// <summary>How far a class buff reaches from the one laying it (the party gathered at the stop).</summary>
        public const float BuffReach = 30;
        /// <summary>A buff with less than this left (seconds) is taken again at a stop; more, and it is kept (the stock is not wasted).</summary>
        public const float Refresh = 600;
        /// <summary>A class buff: half an hour, 3% of one stat (Chris's brief: "small").</summary>
        public const float ClassBuffSeconds = 1800, ClassBuffShare = .03f;
        /// <summary>The most any one stat's buffs add to a sim's blow, mend or health (the buffs together worth 10-15%: the brief).</summary>
        public const float MaxEdge = .15f;
        /// <summary>A draught is drunk at a stop under this share of the pool.</summary>
        public const float DraughtBelow = .6f;

        // ---------- who wants what ----------
        /// <summary>A class's part when the party has not said otherwise (a sim's stock is bought for it): Warriors and Paladins tank; Druids,
        /// Clerics, Shamans and Salt-Menders heal; the rest deal damage. In a party SimCompanion.Role decides.</summary>
        public static SimCompanion.PartyRole NominalRole(string classId)
        {
            if (classId == "class.warrior" || classId == "class.paladin") return SimCompanion.PartyRole.Tank;
            if (classId == "class.druid" || classId == "class.cleric" || classId == "class.shaman" || classId == "class.saltmender") return SimCompanion.PartyRole.Healer;
            return SimCompanion.PartyRole.Damage;
        }
        /// <summary>The stat a class's blows ride on: Strength for the Warrior and the Paladin, Agility for the Ranger, the Rogue and the
        /// Scoundrel, Intellect for the casters.</summary>
        public static StatType MainStat(string classId)
        {
            if (classId == "class.warrior" || classId == "class.paladin") return StatType.Strength;
            if (classId == "class.ranger" || classId == "class.rogue" || classId == "class.scoundrel") return StatType.Agility;
            return StatType.Intellect;
        }
        /// <summary>The pool a class's draught fills, as Encounter.asset gives the classes (Consumables.PoolOf over the content says the
        /// same; this is for a sim with no content to hand).</summary>
        public static string PoolOf(string classId)
        {
            switch (classId)
            {
                case "class.warrior": return "vigor";
                case "class.druid": return "breath";
                case "class.ranger": case "class.rogue": return "focus";
                case "class.scoundrel": return "nerve";
                case "class.saltmender": return "salt";
                case null: return null;
                default: return "mana";
            }
        }

        public static bool IsDish(ItemDef d) { return d != null && d.food && Consumables.HasBuff(d) && !d.buff.party; }
        public static bool IsFeast(ItemDef d) { return d != null && d.food && Consumables.HasBuff(d) && d.buff.party; }
        public static bool IsSalve(ItemDef d) { return d != null && Consumables.HasBuff(d) && d.buff.kind == "salve"; }
        public static bool IsElixir(ItemDef d, StatType stat) { return d != null && Consumables.HasBuff(d) && d.buff.kind == "elixir" && Consumables.TryStat(d.buff.stat, out var s) && s == stat; }
        public static bool IsDraught(ItemDef d, string pool) { return d != null && d.restore > 0 && !string.IsNullOrEmpty(pool) && d.pool == pool; }
        static int Worth(ItemDef d) { return d == null ? 0 : d.buff != null ? d.buff.amount + d.buff.stamina : d.restore; }

        /// <summary>The best of a kind: from what is carried, the one that gives most; from the whole item list (<paramref name="level"/> &gt; 0),
        /// the highest-level one at or under that level (the tier for it).</summary>
        public static ItemDef Best(IEnumerable<ItemDef> from, Func<ItemDef, bool> kind, int level = 0)
        {
            ItemDef best = null; if (from == null) return null;
            foreach (var d in from)
            {
                if (d == null || !kind(d) || (level > 0 && d.level > level)) continue;
                if (best == null || (level > 0 && d.level > best.level) || ((level <= 0 || d.level == best.level) && Worth(d) > Worth(best))) best = d;
            }
            return best;
        }
        /// <summary>
        /// The elixir a part drinks, from what is carried: a tank its armour salve, else Bear's-wort (Stamina); a healer Hearth-ease (Spirit,
        /// the mending's), else Owl-light; a damage-dealer the tincture of its main stat. Null when it carries none of them.
        /// </summary>
        public static ItemDef PickElixir(SimCompanion.PartyRole role, StatType main, IEnumerable<ItemDef> carried, int level = 0)
        {
            switch (role)
            {
                case SimCompanion.PartyRole.Tank: return Best(carried, IsSalve, level) ?? Best(carried, d => IsElixir(d, StatType.Stamina), level);
                case SimCompanion.PartyRole.Healer: return Best(carried, d => IsElixir(d, StatType.Spirit), level) ?? Best(carried, d => IsElixir(d, StatType.Intellect), level);
                default: return Best(carried, d => IsElixir(d, main), level);
            }
        }
        public static ItemDef PickDish(IEnumerable<ItemDef> carried, int level = 0) { return Best(carried, IsDish, level); }
        public static ItemDef PickFeast(IEnumerable<ItemDef> carried, int level = 0) { return Best(carried, IsFeast, level); }
        public static ItemDef PickDraught(string pool, IEnumerable<ItemDef> carried, int level = 0) { return Best(carried, d => IsDraught(d, pool), level); }
        /// <summary>Whether a buff of this status is worth taking now: not on, or with less than <see cref="Refresh"/> left.</summary>
        public static bool Wants(StatusEffectRuntime on, string statusId, float now) { return on != null && statusId != null && on.Remaining(statusId, now) < Refresh; }

        // ---------- a sim's stock (SimEconomy.Stock) ----------
        /// <summary>
        /// What a sim keeps in its pack for the bosses, at its level's tier: two of its dish, two of its part's elixir and one draught for
        /// its pool (a feast is bought on top, by the rich: <see cref="FeastFor"/>). Ids and counts; what the item list lacks is left out.
        /// </summary>
        public static List<(string id, int count)> Pack(string classId, int level, ItemDatabase items)
        {
            var pack = new List<(string, int)>(); if (items == null) return pack;
            var all = items.Items.Values;
            var dish = PickDish(all, level); if (dish != null) pack.Add((dish.id, 2));
            var elixir = PickElixir(NominalRole(classId), MainStat(classId), all, level); if (elixir != null) pack.Add((elixir.id, 2));
            var draught = PickDraught(PoolOf(classId), all, level); if (draught != null) pack.Add((draught.id, 1));
            return pack;
        }
        /// <summary>The feast of a level's tier, or null below the first.</summary>
        public static ItemDef FeastFor(int level, ItemDatabase items) { return items == null ? null : PickFeast(items.Items.Values, level); }
        /// <summary>What a sim pays another adventurer for a consumable: twice its value (as you pay a sim: EncounterSession.VendorPrice).</summary>
        public static int FairPrice(ItemDef d) { return d == null ? 0 : Math.Max(1, d.value * 2); }
        /// <summary>A feast is bought by a sim with this many times its price to spare after its pack.</summary>
        public const int FeastPurse = 4;

        // ---------- what the buffs are worth to a sim ----------
        /// <summary>
        /// What one stat's buffs on a sim add to what rides on it (its blow for its main stat, its mend for Spirit, its health for Stamina),
        /// as a share: a class buff's PercentAdd as it stands, and the flat points of a dish or an elixir against a player's of the sim's
        /// level (a point of a main stat worth 1/(20(L+2)), so the level's dish and elixir together are some 7%; a point of Stamina
        /// 1/(6(L+10)): a dish's some 5-7% health). At most <see cref="MaxEdge"/>. Armour needs none of this: a salve's armour is armour.
        /// </summary>
        public static float Edge(StatBlock stats, StatusEffectRuntime on, StatType stat, int level)
        {
            if (stats == null) return 0;
            float pct = 0;
            if (on != null)
                foreach (var c in ClassBuffs.ClassIds) { var b = ClassBuffs.For(c); if (b != null && b.stat == stat && on.Has(b.id)) pct += b.share; }
            float flat = stats.Get(stat) / (1 + pct) - stats.GetBase(stat);
            int L = Math.Max(1, level);
            float per = stat == StatType.Stamina ? 6f * (L + 10) : 20f * (L + 2);
            float edge = pct + Math.Max(0, flat) / per;
            return Math.Min(MaxEdge, Math.Max(0, edge));
        }
    }
}
