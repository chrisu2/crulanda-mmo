using System;
using System.Collections.Generic;
using Crulanda.Combat;
using Crulanda.Core;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    // ============================================================================================
    // Consumables with a lasting good (Phase 8.6 step 4, Docs/PHASE8_6_LIVING_WORLD.md 3b; Chris's decision 10 of 2026-10-15: "Well-fed
    // and elixirs 30 minutes, one stat; a draught for each class's pool"). The buffs ride the status effects (StatusEffectRuntime, the
    // HUD's buff row) and are never saved: a reload, a fall or a zone change starts the character hungry again. GAME-ONLY throughout:
    // the village's simples are not the Council's alchemy.
    // ============================================================================================
    /// <summary>
    /// What a consumable gives for a while. kind: food (well fed: Stamina and one stat, from every dish from Cooking 20; eaten out of
    /// combat only; party: a feast, which feeds the party of five), elixir (a tincture or a cordial: one stat) or salve (armour). stat
    /// names a StatType (Strength, Agility, Intellect, Spirit, Stamina or Armor); amount is that stat; stamina is a dish's Stamina;
    /// seconds how long it lasts (1800, half an hour); name what the buff row calls it (empty: "Well fed", the stat, "Salve").
    /// </summary>
    [Serializable] public sealed class ItemBuff { public string kind, stat, name; public int amount, stamina; public float seconds; public bool party; }

    /// <summary>
    /// The rules for a consumable's buff, a draught's pool and a cure, pure over the item and a status runtime so they are tested
    /// without a scene (ConsumableBuffTests); the session (EncounterSession.UseItem) applies them to the player, Mira and the party.
    /// One buff of each kind at a time: a second dish's well fed replaces the first's, a tincture another tincture, a salve a salve;
    /// the three kinds sit together.
    /// </summary>
    public static class Consumables
    {
        public const string FedId = "buff.fed", ElixirId = "buff.elixir", SalveId = "buff.salve";
        public const string FedName = "Well fed", SalveName = "Salve";
        public static readonly string[] BuffKinds = { "food", "elixir", "salve" };
        /// <summary>The stats a buff may raise.</summary>
        public static readonly StatType[] BuffStats = { StatType.Strength, StatType.Agility, StatType.Intellect, StatType.Spirit, StatType.Stamina, StatType.Armor };
        /// <summary>The pools a draught may fill, by what each class calls its own (Encounter.asset: Mana, Vigor, Focus, the Scoundrel's
        /// Nerve, the Druid's Breath, the Salt-Mender's Salt).</summary>
        public static readonly string[] Pools = { "mana", "vigor", "focus", "nerve", "breath", "salt" };
        /// <summary>A feast feeds this many: the eater and four more of the party (DUNGEON_DESIGN: five-player).</summary>
        public const int PartySize = 5;
        /// <summary>How near the eater the party must be to share a feast (metres).</summary>
        public const float FeastRange = 12;
        public const string NoEatingLine = "You can't eat while fighting.";

        /// <summary>Whether an item carries a buff the game can apply: a known kind, a stat or Stamina to give, and a time.</summary>
        public static bool HasBuff(ItemDef d)
        {
            var b = d?.buff;
            return b != null && b.seconds > 0 && Array.IndexOf(BuffKinds, b.kind) >= 0 && ((b.amount > 0 && TryStat(b.stat, out _)) || b.stamina > 0);
        }
        public static bool TryStat(string s, out StatType stat)
        {
            stat = StatType.Stamina;
            return !string.IsNullOrEmpty(s) && Enum.TryParse(s, out stat) && Array.IndexOf(BuffStats, stat) >= 0;
        }
        /// <summary>The status a buff of this kind is carried as: one of each kind at a time.</summary>
        public static string StatusId(ItemBuff b) { return b == null ? null : b.kind == "food" ? FedId : b.kind == "salve" ? SalveId : ElixirId; }
        /// <summary>What the buff row calls it: its name, else "Well fed" for food, "Salve" for a salve, the stat for an elixir.</summary>
        public static string BuffName(ItemBuff b)
        {
            if (b == null) return null;
            if (!string.IsNullOrEmpty(b.name)) return b.name;
            return b.kind == "food" ? FedName : b.kind == "salve" ? SalveName : b.stat;
        }
        /// <summary>The last name each buff status was put on with (the buff row reads it: EncounterSession.StatusName).</summary>
        static readonly Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.Ordinal) { { FedId, FedName }, { SalveId, SalveName }, { ElixirId, "Tincture" } };
        /// <summary>A buff status's name for the buff row, or null for an id that is not a consumable's.</summary>
        public static string StatusName(string id) { return id != null && names.TryGetValue(id, out var n) ? n : null; }

        /// <summary>The status a consumable's buff puts on: its kind's id, the item's seconds, and Flat modifiers of the dish's Stamina and
        /// the stat's amount. Null when the item has none.</summary>
        public static StatusEffectDefinition Definition(ItemDef d)
        {
            if (!HasBuff(d)) return null;
            var b = d.buff; var mods = new List<StatusModifierDefinition>();
            if (b.stamina > 0) mods.Add(new StatusModifierDefinition { stat = StatType.Stamina, operation = ModifierOp.Flat, value = b.stamina });
            if (b.amount > 0 && TryStat(b.stat, out var stat)) mods.Add(new StatusModifierDefinition { stat = stat, operation = ModifierOp.Flat, value = b.amount });
            return new StatusEffectDefinition { id = StatusId(b), name = BuffName(b), duration = b.seconds, incomingDamageMultiplier = 1, modifiers = mods.ToArray() };
        }
        /// <summary>Puts a consumable's buff on someone in place of any of its kind already on (a second dish's well fed replaces the first's,
        /// stats and all). False when the item has none, or there is nobody.</summary>
        public static bool Apply(ItemDef d, StatusEffectRuntime on, float now)
        {
            var def = Definition(d); if (def == null || on == null) return false;
            on.Remove(def.id); names[def.id] = def.name;
            return on.Apply(def, now);
        }
        /// <summary>A feast: its buff on the eater and the party, the first <see cref="PartySize"/> in the list (the eater first; the session
        /// lists Mira and the party sims within <see cref="FeastRange"/>). Returns how many were fed.</summary>
        public static int Feed(ItemDef d, IList<StatusEffectRuntime> party, float now)
        {
            int fed = 0; if (party == null || !HasBuff(d)) return 0;
            var seen = new HashSet<StatusEffectRuntime>();
            foreach (var who in party)
            {
                if (fed >= PartySize) break;
                if (who == null || !seen.Add(who)) continue;
                if (Apply(d, who, now)) fed++;
            }
            return fed;
        }

        /// <summary>The pool a class fills from a draught: the name the HUD gives it when it has one of its own (the Scoundrel's Nerve), else
        /// its kind ("mana", "vigor", "focus", "breath", "salt"); null for none.</summary>
        public static string PoolOf(ClassDefinition c)
        {
            if (c == null) return null;
            if (!string.IsNullOrEmpty(c.resourceName)) return c.resourceName.ToLowerInvariant();
            return c.resource == ResourceKind.None ? null : c.resource.ToString().ToLowerInvariant();
        }
        /// <summary>A pool's word as the tooltip says it: "Mana", "Nerve", "Breath".</summary>
        public static string PoolWord(string pool) { return string.IsNullOrEmpty(pool) ? "" : char.ToUpperInvariant(pool[0]) + pool.Substring(1); }
        /// <summary>
        /// Why a consumable can't be used now, or null when it can: food is never eaten in a fight ("You can't eat while fighting."); a
        /// draught is drunk only by a class whose pool it fills ("That draught fills Mana. Yours is Nerve."). The potion cooldown is the
        /// session's to say.
        /// </summary>
        public static string Refusal(ItemDef d, bool inCombat, ClassDefinition cls)
        {
            if (d == null || d.kind != "consumable") return "That can't be used.";
            if (d.food && inCombat) return NoEatingLine;
            if (d.restore > 0 && !string.IsNullOrEmpty(d.pool))
            {
                string mine = PoolOf(cls);
                if (mine != d.pool) return "That draught fills " + PoolWord(d.pool) + "." + (string.IsNullOrEmpty(mine) ? "" : " Yours is " + PoolWord(mine) + ".");
            }
            return null;
        }
        /// <summary>Whether drinking it waits on the potion cooldown: what heals, fills a pool or cures at once. Food, tinctures and
        /// salves do not.</summary>
        public static bool OnPotionCooldown(ItemDef d) { return d != null && !d.food && (d.heal > 0 || d.restore > 0 || d.cure); }

        /// <summary>What a consumable is called on its tooltip and in the loot window: Food, Salve, Tincture (a tincture or a cordial),
        /// Draught (one that fills a pool or cures) or Potion (one that heals).</summary>
        public static string KindWord(ItemDef d)
        {
            if (d == null) return "Potion";
            if (d.food) return "Food";
            if (HasBuff(d)) return d.buff.kind == "salve" ? "Salve" : "Tincture";
            return (d.restore > 0 || d.cure) && d.heal <= 0 ? "Draught" : "Potion";
        }
        /// <summary>The tooltip's lines for a consumable's buff, draught and cure ("Well fed: +6 Stamina, +4 Agility for 30 min").</summary>
        public static List<string> Lines(ItemDef d)
        {
            var l = new List<string>(); if (d == null) return l;
            if (HasBuff(d))
            {
                var b = d.buff; var parts = new List<string>();
                if (b.stamina > 0) parts.Add("+" + b.stamina + " Stamina");
                if (b.amount > 0 && TryStat(b.stat, out var stat)) parts.Add("+" + b.amount + (stat == StatType.Armor ? " armour" : " " + stat));
                string time = b.seconds >= 60 ? Math.Round(b.seconds / 60f) + " min" : b.seconds + " s";
                string what = b.kind == "food" ? FedName : b.kind == "salve" ? SalveName : "Tincture";
                l.Add(what + ": " + string.Join(", ", parts) + " for " + time + (b.party ? " (the party of five, gathered round)" : ""));
            }
            if (d.restore > 0 && !string.IsNullOrEmpty(d.pool)) l.Add("Restores " + d.restore + " " + PoolWord(d.pool));
            if (d.cure) l.Add("Lifts poisons, bleeds and the grey");
            return l;
        }

        /// <summary>The item file's checks for these fields (ItemDatabase.Parse): a buff only on a consumable, of a known kind, with a stat
        /// the game carries and a time; food buffs on food only; a feast is food; a draught names a known pool.</summary>
        public static void Check(ItemDef i, List<string> errors)
        {
            var b = i.buff; string p = "Item '" + i.id + "': ";
            bool any = b != null && (!string.IsNullOrEmpty(b.kind) || b.amount != 0 || b.stamina != 0 || b.seconds != 0);
            if (any)
            {
                if (i.kind != "consumable") errors.Add(p + "a buff on something that is not a consumable.");
                if (Array.IndexOf(BuffKinds, b.kind) < 0) errors.Add(p + "unknown buff kind '" + b.kind + "' (" + string.Join(", ", BuffKinds) + ").");
                if (b.amount > 0 && !TryStat(b.stat, out _)) errors.Add(p + "buff stat '" + b.stat + "' is not one a buff may raise.");
                if (!(b.seconds > 0)) errors.Add(p + "a buff with no time.");
                if (b.amount < 0 || b.stamina < 0 || (b.amount == 0 && b.stamina == 0)) errors.Add(p + "a buff that gives nothing.");
                if ((b.kind == "food") != i.food) errors.Add(p + (i.food ? "food with a buff that is not well fed." : "well fed on something that is not food."));
                if (b.party && b.kind != "food") errors.Add(p + "only a feast feeds the party.");
            }
            if (i.restore < 0 || (i.restore > 0 && Array.IndexOf(Pools, i.pool) < 0)) errors.Add(p + "a draught for pool '" + i.pool + "' (" + string.Join(", ", Pools) + ").");
            if ((i.restore > 0 || i.cure) && i.kind != "consumable") errors.Add(p + "a draught or cure that is not a consumable.");
        }
    }
}
