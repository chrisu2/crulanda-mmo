using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Abilities;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Encounter
{
    // ============================================================================================
    // The loot database (loot DESIGN.md section 3): named gear by zone and boss, with looks, sources, drop lists, sets and the
    // few effects the game can carry out. Data: loot.<zone>.json, ordinary item files (items, vendors) with three more sections
    // (gear, drops, sets) that ItemDatabase ignores and LootDatabase reads. Since step L2 they sit in EncounterContent/Items with
    // the other item files, and EncounterSession.LoadLoot reads them after the items.
    // ============================================================================================
    [Serializable] public sealed class LootFile
    {
        public GearMeta[] gear = new GearMeta[0];
        public DropDef[] drops = new DropDef[0];
        public SetDef[] sets = new SetDef[0];
    }
    /// <summary>
    /// What a named item is beyond its ItemDef: its look (see GearLooks), where it comes from, whether it is unique (one at a time)
    /// or a boss's, its set and its effects. source: boss:&lt;mob&gt;, mob:&lt;tag&gt;@&lt;zone&gt; (tag "any" for every camp of the zone),
    /// quest:&lt;quest id&gt;, vendor:&lt;npc&gt;, world:&lt;min&gt;-&lt;max&gt; or secret:&lt;secret id&gt;. legacy: one of the twelve named items
    /// that were in the game before the loot database (look, source and set only; their stats stay as they were).
    /// </summary>
    [Serializable] public sealed class GearMeta
    {
        public string id, look, source, set;
        public bool unique, boss, legacy;
        public GearEffect[] effects = new GearEffect[0];
    }
    /// <summary>
    /// kind: stat (stat names a StatType: MaxHealth, MaxPower, AttackPower, Armor or a primary stat; percent makes amount a
    /// percentage), onKillHeal, onKillPower, restRegen (health on the out-of-combat tick), coins or luck (percent on coins from
    /// bodies and on lucky drop groups). text is the tooltip line.
    /// The class sets' kinds (DESIGN.md 3.8) change one of the wearer's own abilities, named by its id in ability: abilityCost
    /// (amount less resource, or amount% with percent), abilityCooldown (amount seconds shorter, or amount%), abilityCastTime (amount
    /// seconds quicker, or amount%), abilityPower (its hits and heals as it resolves amount% stronger). The procs fire as ability
    /// resolves (ability "auto": a weapon swing or auto-shot that lands), chance% of the time: procResource (amount resource back),
    /// procReset (amount seconds off the cooldown of the ability named in other: a large amount makes it ready) and procEcho (the
    /// ability resolves a second time, free, at amount% power). GearEffects.Adjust, PowerScale and Proc work them out; the
    /// session's StartAbility, the one door every kit's abilities pass through, carries them out.
    /// </summary>
    [Serializable] public sealed class GearEffect { public string kind, stat, text, ability, other; public float amount, chance; public bool percent; }
    /// <summary>
    /// A drop list and the camp mobs it applies to: zone (the zone id without "zone."), tag, mob (the camp's mob name) and rank
    /// (elite, normal, or empty for both) narrow it when given; levelMin and levelMax bound the mob's level.
    /// </summary>
    [Serializable] public sealed class DropDef
    {
        public string id, zone, tag, mob, rank;
        public int levelMin = 1, levelMax = EncounterProgress.LevelCap;
        public DropGroup[] groups = new DropGroup[0];
    }
    /// <summary>
    /// One roll: chance, then one weighted pick. signature: a boss's own list, which gives a piece you do not own while there is
    /// one. lucky: worn luck raises the chance. pity: an epic that is certain on that many kills without it (while unowned).
    /// </summary>
    /// <summary>
    /// classSet: a class set's group (DESIGN.md 3.8). Its picks are class pieces (ItemDef.classOnly), and only those of a class in
    /// the party can drop: the player's class weighs <see cref="LootDatabase.ClassSetOwnWeight"/> times one sim's, a piece the player
    /// owns leaves the pool, and pity counts the kills without the player's own unowned piece, which is certain on the pity-th. It
    /// neither spends nor is held back by the named-item cap, and it rolls only when the caller passes the party's classes.
    /// </summary>
    [Serializable] public sealed class DropGroup { public float chance; public bool signature, lucky, classSet; public int pity; public DropPick[] pick = new DropPick[0]; }
    [Serializable] public sealed class DropPick { public string item; public int weight = 1; }
    /// <summary>
    /// A set: its pieces (each in its own slot) and the bonuses that switch on at a count of pieces worn. A class set (DESIGN.md 3.8)
    /// names its class in cls (every piece is that class's only: ItemDef.classOnly), its tier (1: the dungeon sets of the Sealed
    /// Adit; 2 and up: the raids', none built yet) with tierName for the tooltip ("Dungeon", "Raid"), and its canonStatus.
    /// </summary>
    [Serializable] public sealed class SetDef { public string id, name, cls, tierName, canonStatus; public int tier; public string[] pieces = new string[0]; public SetBonus[] bonuses = new SetBonus[0]; }
    [Serializable] public sealed class SetBonus { public int count; public GearEffect[] effects = new GearEffect[0]; }
    /// <summary>A pity counter: kills of a source (drop id and group) and how many of them in a row gave nothing. Saved from step L3.</summary>
    [Serializable] public sealed class LootLuck { public string source; public int kills, dry; }

    /// <summary>One thing on a body: an item id and how many.</summary>
    public struct LootDrop
    {
        public string item; public int count;
        public LootDrop(string item, int count) { this.item = item; this.count = count; }
        public override string ToString() { return item + (count > 1 ? " x" + count : ""); }
    }

    /// <summary>Who died: its zone (without "zone."), tag, camp mob name, level and whether it was the camp's elite.</summary>
    public struct LootContext
    {
        public string zone, tag, mob; public int level; public bool elite;
        /// <summary>
        /// From a camp mob's id ("mob.&lt;tag&gt;.&lt;zone&gt;.&lt;camp&gt;.&lt;n&gt;") and its zone's camps: the mob name is the camp's. Any other
        /// id gives its second part as the tag ("any" if there is none) and no zone or mob.
        /// </summary>
        public static LootContext From(string persistentId, ZoneCamp[] camps, int level, bool elite)
        {
            var c = new LootContext { level = level, elite = elite, tag = "any" };
            var parts = (persistentId ?? "").Split('.');
            if (parts.Length > 1 && parts[1].Length > 0) c.tag = parts[1];
            if (parts.Length == 5 && parts[0] == "mob")
            {
                c.zone = parts[2];
                if (int.TryParse(parts[3], out int camp) && camps != null && camp >= 0 && camp < camps.Length && camps[camp] != null) c.mob = camps[camp].mob;
            }
            return c;
        }
        /// <summary>The tag a camp's mobs carry in their ids: its tag, else its look, else "mob" (as EncounterSession.SpawnCamps names them).</summary>
        public static string CampTag(ZoneCamp c) { return string.IsNullOrEmpty(c.tag) ? (c.look ?? "mob") : c.tag; }
    }

    public sealed class LootDatabase
    {
        public readonly Dictionary<string, GearMeta> Gear = new Dictionary<string, GearMeta>(StringComparer.Ordinal);
        /// <summary>The gear entries in file order (the wardrobe and the Armoury list them this way).</summary>
        public readonly List<GearMeta> GearOrder = new List<GearMeta>();
        public readonly List<DropDef> Drops = new List<DropDef>();
        public readonly Dictionary<string, SetDef> Sets = new Dictionary<string, SetDef>(StringComparer.Ordinal);
        public readonly List<SetDef> SetOrder = new List<SetDef>();
        /// <summary>The item database the loot was checked against (core items and the loot files' own).</summary>
        public ItemDatabase Items { get; private set; }

        /// <summary>The kinds of effect the game can carry out (DESIGN.md 3.6).</summary>
        public static readonly string[] EffectKinds = { "stat", "onKillHeal", "onKillPower", "restRegen", "coins", "luck",
            "abilityCost", "abilityCooldown", "abilityCastTime", "abilityPower", "procResource", "procReset", "procEcho", "procRoot" };
        /// <summary>The kinds that work on one of the wearer's abilities (DESIGN.md 3.8): they name it in ability.</summary>
        public static readonly string[] AbilityKinds = { "abilityCost", "abilityCooldown", "abilityCastTime", "abilityPower", "procResource", "procReset", "procEcho", "procRoot" };
        /// <summary>The procs: they carry a chance (percent) and may fire on <see cref="AutoTrigger"/>. (Hallow's Creek, the Mortar Ring)
        /// procRoot: the target struck is rooted for its amount in seconds.</summary>
        public static readonly string[] ProcKinds = { "procResource", "procReset", "procEcho", "procRoot" };
        /// <summary>A proc's trigger for the weapon's own blows (a swing or an auto-shot that lands).</summary>
        public const string AutoTrigger = "auto";
        /// <summary>In a class set group the player's own class weighs this many times one sim's.</summary>
        public const int ClassSetOwnWeight = 3;
        /// <summary>The stats an effect of kind "stat" may change.</summary>
        public static readonly StatType[] EffectStats = { StatType.MaxHealth, StatType.MaxPower, StatType.AttackPower, StatType.Armor, StatType.Strength, StatType.Agility, StatType.Stamina, StatType.Intellect, StatType.Spirit };
        public static readonly string[] SourceKinds = { "boss", "mob", "quest", "vendor", "world", "secret" };
        /// <summary>Once every piece of a signature list is owned, the list still pays out at this share of its chance.</summary>
        public const float SignatureOwnedShare = .35f;
        /// <summary>An epic you own drops at this share of its chance.</summary>
        public const float EpicOwnedShare = .25f;
        /// <summary>A legendary's chance by night over by day (.5% becomes .65%; WorldClock.IsNight).</summary>
        public const float LegendaryNight = 1.3f;
        /// <summary>Share of an elite's generated rares that come out epic instead (same slot, level and seed).</summary>
        public const float GeneratedEpicChance = .06f;
        /// <summary>The generated piece an elite adds when it would drop only junk is rare this often, else uncommon.</summary>
        public const float RescueRareChance = .35f;
        /// <summary>Named items per kill (epics do not count): one from a normal mob, two from an elite.</summary>
        public const int NormalCap = 1, EliteCap = 2;

        /// <summary>
        /// Reads the loot sections of item files (files without them are fine, so every item file may be passed). Items named
        /// anywhere must exist in <paramref name="items"/>. Looks are checked (palettes too when <paramref name="looks"/> is
        /// given) and registered with it. Every problem is collected and thrown together as one ArgumentException.
        /// </summary>
        public static LootDatabase Parse(IEnumerable<string> jsonFiles, ItemDatabase items, GearLooks looks = null)
        {
            if (items == null) throw new ArgumentException("The loot database needs the item database.");
            var db = new LootDatabase { Items = items }; var errors = new List<string>(); var dropIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var json in jsonFiles)
            {
                if (string.IsNullOrWhiteSpace(json)) continue;
                LootFile f; try { f = JsonUtility.FromJson<LootFile>(json); } catch (Exception e) { errors.Add("Unreadable loot file: " + e.Message); continue; }
                if (f == null) continue;
                foreach (var g in f.gear ?? new GearMeta[0])
                {
                    if (g == null || string.IsNullOrEmpty(g.id)) { errors.Add("A gear entry has no id."); continue; }
                    string p = "Gear '" + g.id + "': ";
                    if (db.Gear.ContainsKey(g.id)) { errors.Add(p + "listed twice."); continue; }
                    var d = items.Get(g.id);
                    if (d == null) errors.Add(p + "no such item.");
                    else if (d.kind != "gear") errors.Add(p + "is not gear.");
                    if (!GearLooks.TryParseLook(g.look, out var family, out _, out var palette, out _)) errors.Add(p + "bad look '" + g.look + "'.");
                    else if (looks != null && !looks.IsValid(g.look, out var why)) errors.Add(p + why);
                    else if (d != null && GearLooks.Family(family).slot != d.slot) errors.Add(p + "look " + family + " is a " + GearLooks.Family(family).slot + " family, the item is " + d.slot + ".");
                    if (SourceKind(g.source) == null) errors.Add(p + "bad source '" + g.source + "' (" + string.Join(", ", SourceKinds) + ").");
                    foreach (var e in g.effects ?? new GearEffect[0]) CheckEffect(e, p, errors);
                    db.Gear[g.id] = g; db.GearOrder.Add(g);
                }
                foreach (var d in f.drops ?? new DropDef[0])
                {
                    if (d == null || string.IsNullOrEmpty(d.id)) { errors.Add("A drop list has no id."); continue; }
                    string p = "Drops '" + d.id + "': ";
                    if (!dropIds.Add(d.id)) { errors.Add(p + "listed twice."); continue; }
                    if (!string.IsNullOrEmpty(d.rank) && d.rank != "elite" && d.rank != "normal") errors.Add(p + "rank '" + d.rank + "' (elite, normal or none).");
                    if (d.levelMin < 1 || d.levelMax > EncounterProgress.LevelCap || d.levelMin > d.levelMax) errors.Add(p + "levels " + d.levelMin + "-" + d.levelMax + " are not inside 1-" + EncounterProgress.LevelCap + ".");
                    if (d.groups == null || d.groups.Length == 0) errors.Add(p + "no groups.");
                    foreach (var g in d.groups ?? new DropGroup[0])
                    {
                        if (g == null) { errors.Add(p + "an empty group."); continue; }
                        if (!(g.chance > 0 && g.chance <= 1)) errors.Add(p + "chance " + g.chance + " is not inside (0, 1].");
                        if (g.pity < 0) errors.Add(p + "negative pity.");
                        if (g.pick == null || g.pick.Length == 0) errors.Add(p + "a group picks nothing.");
                        if (g.classSet && g.signature) errors.Add(p + "a class set group is not a signature list.");
                        foreach (var k in g.pick ?? new DropPick[0])
                        {
                            if (k == null || items.Get(k.item) == null) errors.Add(p + "drops unknown item '" + k?.item + "'.");
                            else if (k.weight < 1) errors.Add(p + "'" + k.item + "' has weight " + k.weight + ".");
                            else if (g.classSet && string.IsNullOrEmpty(items.Get(k.item).classOnly)) errors.Add(p + "'" + k.item + "' is on a class set group but is no class's piece.");
                        }
                    }
                    db.Drops.Add(d);
                }
                foreach (var s in f.sets ?? new SetDef[0])
                {
                    if (s == null || string.IsNullOrEmpty(s.id)) { errors.Add("A set has no id."); continue; }
                    string p = "Set '" + s.id + "': ";
                    if (db.Sets.ContainsKey(s.id)) { errors.Add(p + "listed twice."); continue; }
                    if (string.IsNullOrEmpty(s.name)) errors.Add(p + "no name.");
                    var slots = new HashSet<string>();
                    foreach (var piece in s.pieces ?? new string[0])
                    {
                        var d = items.Get(piece);
                        if (d == null || d.kind != "gear") errors.Add(p + "piece '" + piece + "' is not a known piece of gear.");
                        else if (!slots.Add(d.slot)) errors.Add(p + "two pieces for the " + d.slot + " slot.");
                        else if ((d.classOnly ?? "") != (s.cls ?? "")) errors.Add(p + "piece '" + piece + "' is for '" + d.classOnly + "', the set for '" + s.cls + "'.");
                    }
                    if (!string.IsNullOrEmpty(s.cls) && (s.tier < 1 || string.IsNullOrEmpty(s.tierName) || string.IsNullOrEmpty(s.canonStatus))) errors.Add(p + "a class set needs a tier (1 and up), a tierName and a canonStatus.");
                    int last = 1;
                    foreach (var b in s.bonuses ?? new SetBonus[0])
                    {
                        if (b == null || b.count <= last || b.count > (s.pieces ?? new string[0]).Length) { errors.Add(p + "bonus counts must rise from 2 to the number of pieces."); continue; }
                        last = b.count;
                        if (b.effects == null || b.effects.Length == 0) errors.Add(p + "the " + b.count + "-piece bonus does nothing.");
                        foreach (var e in b.effects ?? new GearEffect[0]) CheckEffect(e, p, errors);
                    }
                    db.Sets[s.id] = s; db.SetOrder.Add(s);
                }
            }
            if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
            if (looks != null) foreach (var g in db.GearOrder) looks.Register(g.id, g.look);
            return db;
        }
        static void CheckEffect(GearEffect e, string p, List<string> errors)
        {
            if (e == null || Array.IndexOf(EffectKinds, e.kind ?? "") < 0) { errors.Add(p + "unknown effect kind '" + e?.kind + "' (" + string.Join(", ", EffectKinds) + ")."); return; }
            if (string.IsNullOrEmpty(e.text)) errors.Add(p + "an effect has no text.");
            if (!(e.amount > 0)) errors.Add(p + "an effect amount must be above 0.");
            if (e.kind == "stat" && !(Enum.TryParse(e.stat ?? "", out StatType s) && Array.IndexOf(EffectStats, s) >= 0)) errors.Add(p + "effect stat '" + e.stat + "' is not one the game carries.");
            if (e.percent && !GearEffects.TakesPercent(e.kind)) errors.Add(p + "only stat, abilityCost, abilityCooldown and abilityCastTime effects may be percentages.");
            float cap = GearEffects.Cap(e.kind, e.percent); if (e.amount > cap) errors.Add(p + e.kind + " " + e.amount + (e.percent ? "%" : "") + " is over its cap of " + cap + ".");
            bool ability = Array.IndexOf(AbilityKinds, e.kind) >= 0, proc = Array.IndexOf(ProcKinds, e.kind) >= 0;
            if (ability && string.IsNullOrEmpty(e.ability)) errors.Add(p + "a " + e.kind + " effect names no ability.");
            if (ability && !proc && e.ability == AutoTrigger) errors.Add(p + "only procs fire on '" + AutoTrigger + "'.");
            if (proc && !(e.chance > 0 && e.chance <= 100)) errors.Add(p + "a proc's chance " + e.chance + " is not inside (0, 100].");
            if (!proc && e.chance != 0) errors.Add(p + "only procs have a chance.");
            if (e.kind == "procReset" && string.IsNullOrEmpty(e.other)) errors.Add(p + "a procReset names no other ability.");
        }
        /// <summary>The kind of a source string (boss, mob, quest, vendor, world, secret), or null when it is malformed.</summary>
        public static string SourceKind(string source)
        {
            if (string.IsNullOrEmpty(source)) return null;
            int colon = source.IndexOf(':'); if (colon <= 0 || colon == source.Length - 1) return null;
            string kind = source.Substring(0, colon), rest = source.Substring(colon + 1);
            if (Array.IndexOf(SourceKinds, kind) < 0) return null;
            if (kind == "mob") { int at = rest.IndexOf('@'); if (at <= 0 || at == rest.Length - 1) return null; }
            if (kind == "world" && !TryWorldBand(rest, out _, out _)) return null;
            return kind;
        }
        static bool TryWorldBand(string band, out int min, out int max)
        {
            min = max = 0; var parts = band.Split('-');
            return parts.Length == 2 && int.TryParse(parts[0], out min) && int.TryParse(parts[1], out max) && min >= 1 && min <= max && max <= EncounterProgress.LevelCap;
        }

        public GearMeta Meta(string id) { return id != null && Gear.TryGetValue(id, out var g) ? g : null; }
        /// <summary>True for an item whose gear entry is unique: you may carry one at a time.</summary>
        public bool IsUnique(string id) { var g = Meta(id); return g != null && g.unique; }
        /// <summary>The set an item belongs to (the set that lists it), or null.</summary>
        public SetDef SetOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var s in SetOrder) if (Array.IndexOf(s.pieces, id) >= 0) return s;
            return null;
        }
        /// <summary>True when a camp mob of this context rolls this drop list.</summary>
        public static bool Matches(DropDef d, LootContext c)
        {
            return (string.IsNullOrEmpty(d.zone) || d.zone == c.zone) && (string.IsNullOrEmpty(d.tag) || d.tag == c.tag) && (string.IsNullOrEmpty(d.mob) || d.mob == c.mob)
                && (string.IsNullOrEmpty(d.rank) || (d.rank == "elite") == c.elite) && c.level >= d.levelMin && c.level <= d.levelMax;
        }
        public List<DropDef> Matching(LootContext c) { return Drops.FindAll(d => Matches(d, c)); }

        /// <summary>
        /// Everything on a camp mob's body (DESIGN.md 3.4): the base roll (ItemDatabase.RollLoot: the tag's junk and maybe one
        /// generated piece), less any item on the mob's signature list (those come from the list now); then each matching group
        /// in file order: signature lists give a piece not owned while there is one (all owned: the list pays at 35% of its chance),
        /// lucky groups scale by 1 + luck (luck capped at +30%), an owned epic drops a quarter as often, and an unowned epic with a
        /// pity count is certain on that kill once that many kills in a row gave nothing (the counters live in
        /// <paramref name="pity"/>; null skips them). At most one named item from a normal kill and two from an elite; epics do not
        /// count. A unique item <paramref name="held"/> says you carry now is left out of each group's pick, so it neither spends the
        /// cap nor moves a pity count, and is taken off the body if the base roll left it there (you could not take it; step L2
        /// passes the bags and equipment, null keeps it). An elite whose body would hold no gear gets a generated piece at its
        /// level (rare 35%, else uncommon), and 6% of an elite's generated rares come out epic. Named items the base roll already
        /// left on the body count toward the cap. owned may be null (nothing owned). The same seed gives the same drops.
        /// <paramref name="classes"/>: the party's classes, the player's first and then each sim's (a class twice when two play it);
        /// class set groups roll only with it (see <see cref="DropGroup.classSet"/> and RollClassSet).
        /// </summary>
        public List<LootDrop> Roll(LootContext c, ItemDatabase items, Func<string, bool> owned, float luck, List<LootLuck> pity, System.Random rng, Func<string, bool> held = null, IList<string> classes = null)
        {
            items = items ?? Items; owned = owned ?? (_ => false);
            var matched = Matching(c); var drops = new List<LootDrop>();
            var signature = new HashSet<string>(StringComparer.Ordinal);
            foreach (var d in matched) foreach (var g in d.groups) if (g.signature) foreach (var k in g.pick) signature.Add(k.item);
            foreach (var (item, count) in items.RollLoot(string.IsNullOrEmpty(c.tag) ? "any" : c.tag, c.level, c.elite, rng))
                if (!signature.Contains(item)) drops.Add(new LootDrop(item, count));
            float lucky = 1 + Mathf.Clamp(luck, 0, GearEffects.LuckCap);
            int named = drops.FindAll(x => Meta(x.item) != null && Quality(items, x.item) < 4).Count, cap = c.elite ? EliteCap : NormalCap;
            foreach (var d in matched)
                for (int gi = 0; gi < d.groups.Length; gi++)
                {
                    var g = d.groups[gi];
                    if (g.classSet) { RollClassSet(d.id + "#" + gi, g, items, classes, owned, lucky, pity, rng, held, drops); continue; }
                    var pool = new List<DropPick>(); foreach (var k in g.pick) if (!drops.Exists(x => x.item == k.item) && !(held != null && IsUnique(k.item) && held(k.item))) pool.Add(k);
                    if (pool.Count == 0) continue;
                    bool epic = pool.TrueForAll(k => Quality(items, k.item) >= 4);
                    if (!epic && named >= cap) continue;
                    float chance = g.chance;
                    if (g.signature)
                    {
                        var unowned = pool.FindAll(k => !owned(k.item));
                        if (unowned.Count > 0) pool = unowned; else chance *= SignatureOwnedShare;
                    }
                    if (g.lucky) chance *= lucky;
                    // A legendary comes a little more often at night (Chris, 2026-10-05: ".5% during the day and .65% at night").
                    if (pool.TrueForAll(k => Quality(items, k.item) >= 5) && Crulanda.World.WorldClock.IsNight) chance *= LegendaryNight;
                    bool allOwned = pool.TrueForAll(k => owned(k.item));
                    if (epic && allOwned) chance *= EpicOwnedShare;
                    LootLuck counter = null;
                    if (pity != null && g.pity > 0 && !allOwned)
                    {
                        string key = d.id + "#" + gi;
                        counter = pity.Find(x => x != null && x.source == key);
                        if (counter == null) { counter = new LootLuck { source = key }; pity.Add(counter); }
                        counter.kills++;
                    }
                    bool hit = rng.NextDouble() < chance || (counter != null && counter.dry + 1 >= g.pity);
                    if (counter != null) counter.dry = hit ? 0 : counter.dry + 1;
                    if (!hit) continue;
                    string picked = Pick(pool, rng);
                    drops.Add(new LootDrop(picked, 1));
                    if (Quality(items, picked) < 4) named++;
                }
            if (held != null) drops.RemoveAll(x => IsUnique(x.item) && held(x.item));
            if (c.elite && !drops.Exists(x => { var i = items.Get(x.item); return i != null && i.kind == "gear"; }))
            {
                int q = rng.NextDouble() < RescueRareChance ? 3 : 2;
                drops.Add(new LootDrop(ItemDatabase.GearId(ItemDatabase.SlotIds[rng.Next(ItemDatabase.SlotIds.Length)], c.level, q, rng.Next(10000)), 1));
            }
            if (c.elite)
                for (int i = 0; i < drops.Count; i++)
                {
                    var parts = drops[i].item.Split('.');
                    if (parts.Length != 5 || parts[0] != "gen" || parts[3] != "3") continue;
                    if (rng.NextDouble() < GeneratedEpicChance) { parts[3] = "4"; drops[i] = new LootDrop(string.Join(".", parts), drops[i].count); }
                }
            return drops;
        }
        /// <summary>
        /// A class set group (DESIGN.md 3.8): the pieces for the classes in the party, weighted by how many play each (the player's
        /// class <see cref="ClassSetOwnWeight"/> times), less the pieces the player owns of their own class and any unique one held.
        /// The chance is the group's (luck scales it when lucky). Pity counts the kills on which the player's own unowned piece was
        /// in the pool and did not drop; on the pity-th it is certain. Nothing without <paramref name="classes"/>.
        /// </summary>
        void RollClassSet(string key, DropGroup g, ItemDatabase items, IList<string> classes, Func<string, bool> owned, float lucky, List<LootLuck> pity, System.Random rng, Func<string, bool> held, List<LootDrop> drops)
        {
            if (classes == null || classes.Count == 0 || string.IsNullOrEmpty(classes[0])) return;
            string mine = classes[0]; var pool = new List<DropPick>(); DropPick own = null;
            foreach (var k in g.pick)
            {
                var d = items.Get(k.item); if (d == null || string.IsNullOrEmpty(d.classOnly) || drops.Exists(x => x.item == k.item)) continue;
                if (held != null && IsUnique(k.item) && held(k.item)) continue;
                int players = 0; for (int i = 0; i < classes.Count; i++) if (classes[i] == d.classOnly) players += i == 0 ? ClassSetOwnWeight : 1;
                if (players == 0 || (d.classOnly == mine && owned(k.item))) continue;
                pool.Add(new DropPick { item = k.item, weight = Math.Max(1, k.weight) * players });
                if (d.classOnly == mine && own == null) own = k;
            }
            if (pool.Count == 0) return;
            LootLuck counter = null;
            if (pity != null && g.pity > 0 && own != null)
            {
                counter = pity.Find(x => x != null && x.source == key);
                if (counter == null) { counter = new LootLuck { source = key }; pity.Add(counter); }
                counter.kills++;
            }
            string picked = null;
            if (counter != null && counter.dry + 1 >= g.pity) picked = own.item;
            else if (rng.NextDouble() < g.chance * (g.lucky ? lucky : 1)) picked = Pick(pool, rng);
            if (counter != null) counter.dry = own != null && picked == own.item ? 0 : counter.dry + 1;
            if (picked != null) drops.Add(new LootDrop(picked, 1));
        }
        static int Quality(ItemDatabase items, string id) { var d = items.Get(id); return d != null ? d.quality : 0; }
        static string Pick(List<DropPick> pool, System.Random rng)
        {
            int total = 0; foreach (var k in pool) total += Math.Max(1, k.weight);
            int r = rng.Next(total);
            foreach (var k in pool) { r -= Math.Max(1, k.weight); if (r < 0) return k.item; }
            return pool[pool.Count - 1].item;
        }

        /// <summary>What a tooltip line from <see cref="TooltipParts"/> is, so the HUD can colour it.</summary>
        public enum TipLine { Unique, Effect, Set, PieceWorn, PieceMissing, BonusOn, BonusOff, Source }
        /// <summary>
        /// The tooltip's loot lines for an item: "Unique", its effects, its set with the pieces worn and each bonus (marked when
        /// it is on), and where it comes from (a hidden find never names its place). Empty for an item without a gear entry.
        /// </summary>
        public string TooltipLines(string id, EncounterProgress p)
        {
            var l = new List<string>();
            foreach (var (line, kind) in TooltipParts(id, p))
                l.Add(kind == TipLine.PieceWorn ? "  * " + line : kind == TipLine.PieceMissing ? "    " + line : kind == TipLine.BonusOn ? "  " + line : kind == TipLine.BonusOff ? "  [" + line.Substring(1, line.IndexOf(')') - 1) + "]" + line.Substring(line.IndexOf(')') + 1) : line);
            return string.Join("\n", l);
        }
        /// <summary>The lines of <see cref="TooltipLines"/> without their marks, each with its kind. A bonus line reads "(2) +30 health", on or off.</summary>
        public List<(string line, TipLine kind)> TooltipParts(string id, EncounterProgress p)
        {
            var l = new List<(string, TipLine)>();
            var g = Meta(id); if (g == null) return l;
            if (g.unique) l.Add(("Unique", TipLine.Unique));
            foreach (var e in g.effects ?? new GearEffect[0]) l.Add((e.text, TipLine.Effect));
            var s = SetOf(id);
            if (s != null)
            {
                int worn = SetWorn(s, p);
                l.Add((s.name + " (" + worn + "/" + s.pieces.Length + ")" + (s.tier > 0 ? "  ·  " + (string.IsNullOrEmpty(s.tierName) ? "" : s.tierName + " set, ") + "tier " + s.tier : ""), TipLine.Set));
                foreach (var piece in s.pieces) { var d = Items.Get(piece); l.Add((d != null ? d.name : piece, p != null && Inventory.IsEquipped(p, piece) ? TipLine.PieceWorn : TipLine.PieceMissing)); }
                foreach (var b in s.bonuses) foreach (var e in b.effects) l.Add(("(" + b.count + ") " + e.text, worn >= b.count ? TipLine.BonusOn : TipLine.BonusOff));
            }
            string from = SourceText(g.source, ZoneName); if (from != null) l.Add((from, TipLine.Source));
            return l;
        }
        /// <summary>Names a short zone id ("ashrim") for the tooltip ("The Ashland Rim"); set by whoever loads the zones. Null shows the id capitalised.</summary>
        public Func<string, string> ZoneName;
        /// <summary>
        /// A source in words: "Dropped by Old Whitefoot", "Sold by Ama Rusk", "A quest reward"... A mob source names its zone
        /// through <paramref name="zoneName"/> (short id to display name), else the short id capitalised.
        /// </summary>
        public static string SourceText(string source, Func<string, string> zoneName = null)
        {
            string kind = SourceKind(source); if (kind == null) return null;
            string rest = source.Substring(kind.Length + 1);
            switch (kind)
            {
                case "boss": return "Dropped by " + rest;
                case "mob":
                    {
                        string z = rest.Substring(rest.IndexOf('@') + 1), n = zoneName != null ? zoneName(z) : null;
                        return "Dropped in " + (!string.IsNullOrEmpty(n) ? n : z.Length > 0 ? char.ToUpperInvariant(z[0]) + z.Substring(1) : z);
                    }
                case "quest": return "A quest reward";
                case "vendor": return "Sold by " + rest;
                case "world": return "A rare find anywhere (levels " + rest + ")";
                default: return "Hidden somewhere";
            }
        }
        /// <summary>How many distinct pieces of a set are worn.</summary>
        public static int SetWorn(SetDef s, EncounterProgress p)
        {
            if (s == null || p == null || p.equipment == null) return 0;
            int n = 0; foreach (var piece in s.pieces) if (Inventory.IsEquipped(p, piece)) n++;
            return n;
        }

        /// <summary>
        /// Checks the loot against the world and the quests (DESIGN.md 7, LootDataTests): every source exists (a boss is an elite
        /// camp whose drop list has the item; a mob source is a camp tag in that zone whose list has it; a quest id is known; a
        /// vendor stocks it and lives in a zone; a world band is a zone-less list with that band; a secret holds it); every elite
        /// camp has a signature list; every drop list names a real zone, tag and mob; every dropped item has a gear entry; sets
        /// and their pieces' set fields agree. Returns the problems (empty when all is well).
        /// </summary>
        public static List<string> Validate(LootDatabase loot, IEnumerable<ZoneDefinition> zones, QuestDatabase quests)
        {
            var problems = new List<string>(); var zoneList = new List<ZoneDefinition>();
            foreach (var z in zones) if (z != null) zoneList.Add(z);
            ZoneDefinition ZoneOf(string shortId) { return zoneList.Find(z => z.id == "zone." + shortId); }
            bool Drops(string item, Func<DropDef, bool> where) { return loot.Drops.Exists(d => where(d) && Array.Exists(d.groups, g => Array.Exists(g.pick, k => k.item == item))); }
            bool Lives(string npc)
            {
                foreach (var z in zoneList)
                {
                    if (z.life == null) continue;
                    var names = z.life.names != null && z.life.names.Length > 0 ? z.life.names : VillageLife.DefaultNames;
                    if (Array.IndexOf(names, npc) >= 0) return true;
                    foreach (var r in z.life.residents ?? new ZoneResident[0]) if (r != null && r.name == npc) return true;
                    foreach (var h in z.life.households ?? new ZoneHousehold[0]) if (h != null && Array.Exists(h.members ?? new ZoneMember[0], m => m != null && m.name == npc)) return true;
                }
                return false;
            }
            foreach (var g in loot.GearOrder)
            {
                string p = "Gear '" + g.id + "': ", kind = SourceKind(g.source); if (kind == null) { problems.Add(p + "bad source."); continue; }
                string rest = g.source.Substring(kind.Length + 1);
                switch (kind)
                {
                    case "boss":
                        if (!zoneList.Exists(z => Array.Exists(z.camps ?? new ZoneCamp[0], c => c != null && c.elite && c.mob == rest))) problems.Add(p + "no elite camp '" + rest + "'.");
                        if (!Drops(g.id, d => d.mob == rest)) problems.Add(p + rest + "'s drop lists do not have it.");
                        break;
                    case "mob":
                        {
                            string tag = rest.Substring(0, rest.IndexOf('@')), zone = rest.Substring(rest.IndexOf('@') + 1); var z = ZoneOf(zone);
                            if (z == null) { problems.Add(p + "no zone '" + zone + "'."); break; }
                            if (tag != "any" && !Array.Exists(z.camps ?? new ZoneCamp[0], c => c != null && LootContext.CampTag(c) == tag)) problems.Add(p + "no '" + tag + "' camp in " + zone + ".");
                            if (!Drops(g.id, d => d.zone == zone && (tag == "any" ? string.IsNullOrEmpty(d.tag) : d.tag == tag))) problems.Add(p + "no " + tag + " drop list in " + zone + " has it.");
                            break;
                        }
                    case "quest":
                        if (quests == null || !quests.Quests.ContainsKey(rest)) problems.Add(p + "no quest '" + rest + "'.");
                        break;
                    case "vendor":
                        if (!loot.Items.StockFor(rest, null, 1).Contains(g.id)) problems.Add(p + rest + " does not sell it.");
                        if (!Lives(rest)) problems.Add(p + "nobody called " + rest + " lives in any zone.");
                        break;
                    case "world":
                        TryWorldBand(rest, out int min, out int max);
                        if (!Drops(g.id, d => string.IsNullOrEmpty(d.zone) && string.IsNullOrEmpty(d.tag) && string.IsNullOrEmpty(d.mob) && d.levelMin == min && d.levelMax == max)) problems.Add(p + "no world drop list for levels " + rest + " has it.");
                        break;
                    case "secret":
                        if (!zoneList.Exists(z => Array.Exists(z.secrets ?? new ZoneSecret[0], s => s != null && s.id == rest && s.item == g.id))) problems.Add(p + "no secret '" + rest + "' holds it.");
                        break;
                }
                if (!string.IsNullOrEmpty(g.set) && (!loot.Sets.TryGetValue(g.set, out var set) || Array.IndexOf(set.pieces, g.id) < 0)) problems.Add(p + "set '" + g.set + "' does not list it.");
            }
            foreach (var s in loot.SetOrder)
                foreach (var piece in s.pieces) { var m = loot.Meta(piece); if (m == null || m.set != s.id) problems.Add("Set '" + s.id + "': piece '" + piece + "' does not name the set in its gear entry."); }
            foreach (var d in loot.Drops)
            {
                string p = "Drops '" + d.id + "': ";
                var z = string.IsNullOrEmpty(d.zone) ? null : ZoneOf(d.zone);
                if (!string.IsNullOrEmpty(d.zone) && z == null) problems.Add(p + "no zone '" + d.zone + "'.");
                var camps = new List<ZoneCamp>(); foreach (var zz in z != null ? new List<ZoneDefinition> { z } : zoneList) foreach (var c in zz.camps ?? new ZoneCamp[0]) if (c != null) camps.Add(c);
                if (!string.IsNullOrEmpty(d.tag) && !camps.Exists(c => LootContext.CampTag(c) == d.tag)) problems.Add(p + "no '" + d.tag + "' camp.");
                if (!string.IsNullOrEmpty(d.mob) && !camps.Exists(c => c.mob == d.mob)) problems.Add(p + "no camp of '" + d.mob + "'.");
                foreach (var g in d.groups) foreach (var k in g.pick) if (loot.Meta(k.item) == null) problems.Add(p + "'" + k.item + "' has no gear entry.");
            }
            foreach (var z in zoneList)
                foreach (var c in z.camps ?? new ZoneCamp[0])
                {
                    if (c == null || !c.elite) continue;
                    var ctx = new LootContext { zone = z.id.Replace("zone.", ""), tag = LootContext.CampTag(c), mob = c.mob, level = c.levelMax, elite = true };
                    if (!loot.Drops.Exists(d => d.mob == c.mob && Matches(d, ctx) && Array.Exists(d.groups, g => g.signature))) problems.Add("Elite '" + c.mob + "' (" + z.id + ") has no signature list.");
                }
            return problems;
        }
    }

    /// <summary>What worn named gear and sets add up to: stat modifiers (from one source object, so they clear together) and the other effects, capped.</summary>
    public sealed class GearEffectTotals
    {
        public readonly List<StatModifier> modifiers = new List<StatModifier>();
        public int onKillHeal, onKillPower, restRegen;
        /// <summary>Fractions: .15 is +15% coins from bodies, or +15% on lucky drop chances.</summary>
        public float coins, luck;
        /// <summary>Each switched-on set bonus and worn effect, as its tooltip text.</summary>
        public readonly List<string> lines = new List<string>();
        /// <summary>The switched-on effects on the wearer's abilities (LootDatabase.AbilityKinds), for GearEffects.Adjust, PowerScale and Proc.</summary>
        public readonly List<GearEffect> abilities = new List<GearEffect>();
    }

    /// <summary>What the procs of one ability resolving (or one landed swing) came to: resource back, cooldowns cut, an echo.</summary>
    public sealed class GearProcs
    {
        public int resource;
        /// <summary>Ability id and seconds off its cooldown.</summary>
        public readonly List<(string ability, float seconds)> resets = new List<(string, float)>();
        /// <summary>The power of the echo (1 = full), 0 for none.</summary>
        public float echo;
        /// <summary>Seconds the target struck is rooted (procRoot: the Mortar Ring), 0 for none.</summary>
        public float root;
        /// <summary>The tooltip texts of the procs that fired.</summary>
        public readonly List<string> fired = new List<string>();
        public bool Any { get { return resource > 0 || resets.Count > 0 || echo > 0 || root > 0; } }
    }

    /// <summary>Gear effects (DESIGN.md 3.6): pure sums over what is worn, with the caps applied.</summary>
    public static class GearEffects
    {
        public const float CoinsCap = .3f, LuckCap = .3f;
        public const int RestRegenCap = 20, OnKillHealCap = 60;
        /// <summary>The class sets' caps (DESIGN.md 3.8): any percentage on an ability 50%, a cost 50 resource less, a cooldown 60 s
        /// shorter, a cast 2 s quicker, procResource 50 resource, procReset 600 s (ready at once), procEcho 100% power.</summary>
        public const float AbilityPercentCap = 50, AbilityCostCap = 50, AbilityCooldownCap = 60, AbilityCastCap = 2, ProcResourceCap = 50, ProcResetCap = 600, ProcEchoCap = 100;
        /// <summary>The longest a procRoot holds the target (seconds).</summary>
        public const float ProcRootCap = 4;
        /// <summary>A cast made quicker never drops under this (seconds), unless it was instant already.</summary>
        public const float MinCastTime = .3f;
        /// <summary>The kinds whose amount may be a percentage.</summary>
        public static bool TakesPercent(string kind) { return kind == "stat" || kind == "abilityCost" || kind == "abilityCooldown" || kind == "abilityCastTime"; }
        /// <summary>The most one effect of a kind may give (coins and luck in percent). Stats and resource have no cap.</summary>
        public static float Cap(string kind) { return Cap(kind, false); }
        /// <summary>The cap for a flat amount or (<paramref name="percent"/>) a percentage.</summary>
        public static float Cap(string kind, bool percent)
        {
            if (percent && kind != "stat") return AbilityPercentCap;
            switch (kind)
            {
                case "coins": return CoinsCap * 100; case "luck": return LuckCap * 100; case "restRegen": return RestRegenCap; case "onKillHeal": return OnKillHealCap;
                case "abilityCost": return AbilityCostCap; case "abilityCooldown": return AbilityCooldownCap; case "abilityCastTime": return AbilityCastCap; case "abilityPower": return AbilityPercentCap;
                case "procResource": return ProcResourceCap; case "procReset": return ProcResetCap; case "procEcho": return ProcEchoCap; case "procRoot": return ProcRootCap;
                default: return float.MaxValue;
            }
        }

        /// <summary>
        /// The ability as the worn set bonuses make it (DESIGN.md 3.8): a copy with the same id (so its cooldown is the same one)
        /// with the cost, cooldown and cast time the abilityCost, abilityCooldown and abilityCastTime effects on its id give (flat
        /// first, then percentages; never under 0, and a cast never under <see cref="MinCastTime"/>). The ability itself when none
        /// touch it. Power is not changed here: it is the session's scale while the ability resolves (<see cref="PowerScale"/>).
        /// </summary>
        public static AbilityDefinition Adjust(AbilityDefinition a, GearEffectTotals t)
        {
            if (a == null || t == null || t.abilities.Count == 0 || string.IsNullOrEmpty(a.id)) return a;
            float costFlat = 0, costPct = 0, cdFlat = 0, cdPct = 0, castFlat = 0, castPct = 0; bool any = false;
            foreach (var e in t.abilities)
            {
                if (e.ability != a.id) continue;
                switch (e.kind)
                {
                    case "abilityCost": if (e.percent) costPct += e.amount; else costFlat += e.amount; any = true; break;
                    case "abilityCooldown": if (e.percent) cdPct += e.amount; else cdFlat += e.amount; any = true; break;
                    case "abilityCastTime": if (e.percent) castPct += e.amount; else castFlat += e.amount; any = true; break;
                }
            }
            if (!any) return a;
            float cast = a.castTime <= 0 ? a.castTime : Mathf.Max(Mathf.Min(MinCastTime, a.castTime), (a.castTime - castFlat) * (1 - Mathf.Min(castPct, AbilityPercentCap) / 100f));
            return new AbilityDefinition
            {
                id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, range = a.range, globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId,
                cost = Mathf.Max(0, Mathf.RoundToInt((a.cost - costFlat) * (1 - Mathf.Min(costPct, AbilityPercentCap) / 100f))),
                cooldown = Mathf.Max(0, (a.cooldown - cdFlat) * (1 - Mathf.Min(cdPct, AbilityPercentCap) / 100f)),
                castTime = cast,
            };
        }
        /// <summary>The scale on an ability's hits and heals while it resolves: 1 + the abilityPower percentages on its id (capped at +50%).</summary>
        public static float PowerScale(GearEffectTotals t, string abilityId)
        {
            if (t == null || string.IsNullOrEmpty(abilityId)) return 1;
            float pct = 0; foreach (var e in t.abilities) if (e.kind == "abilityPower" && e.ability == abilityId) pct += e.amount;
            return 1 + Mathf.Min(pct, AbilityPercentCap) / 100f;
        }
        /// <summary>
        /// The procs on <paramref name="trigger"/> (an ability's id as it resolves, or LootDatabase.AutoTrigger for a landed swing):
        /// each rolls its chance on <paramref name="roll"/> (a number in [0, 1)) and, when it fires, adds its resource, its cut to
        /// the other ability's cooldown or its echo (the strongest echo wins; an echo never echoes again: the session does not call
        /// this for one).
        /// </summary>
        public static GearProcs Proc(GearEffectTotals t, string trigger, Func<double> roll)
        {
            var o = new GearProcs(); if (t == null || string.IsNullOrEmpty(trigger) || roll == null) return o;
            foreach (var e in t.abilities)
            {
                if (e.ability != trigger || Array.IndexOf(LootDatabase.ProcKinds, e.kind) < 0) continue;
                if (!(roll() < e.chance / 100f)) continue;
                switch (e.kind)
                {
                    case "procResource": o.resource += Mathf.RoundToInt(e.amount); break;
                    case "procReset": o.resets.Add((e.other, e.amount)); break;
                    case "procEcho": o.echo = Mathf.Max(o.echo, Mathf.Min(e.amount, ProcEchoCap) / 100f); break;
                    case "procRoot": o.root = Mathf.Max(o.root, Mathf.Min(e.amount, ProcRootCap)); break;
                }
                if (!string.IsNullOrEmpty(e.text)) o.fired.Add(e.text);
            }
            return o;
        }
        /// <summary>
        /// Every effect of the named gear worn and of each set bonus whose count of pieces worn is reached, totalled and capped
        /// (coins and luck +30%, restRegen +20, onKillHeal 60). Stat effects become modifiers from <paramref name="source"/>:
        /// Flat, or PercentAdd for percentages. Nothing is kept: call it again whenever the equipment changes.
        /// </summary>
        public static GearEffectTotals Compute(EncounterProgress p, ItemDatabase items, LootDatabase loot, object source)
        {
            var t = new GearEffectTotals();
            if (p == null || p.equipment == null || loot == null) return t;
            var sets = new List<SetDef>();
            foreach (var s in p.equipment)
            {
                if (s == null || s.Empty || (items != null && items.Get(s.item) == null)) continue;
                var g = loot.Meta(s.item); if (g == null) continue;
                foreach (var e in g.effects ?? new GearEffect[0]) Add(t, e, source);
                var set = loot.SetOf(s.item); if (set != null && !sets.Contains(set)) sets.Add(set);
            }
            foreach (var set in sets)
            {
                int worn = LootDatabase.SetWorn(set, p);
                foreach (var b in set.bonuses) if (worn >= b.count) foreach (var e in b.effects) Add(t, e, source);
            }
            t.coins = Mathf.Min(t.coins, CoinsCap); t.luck = Mathf.Min(t.luck, LuckCap);
            t.restRegen = Mathf.Min(t.restRegen, RestRegenCap); t.onKillHeal = Mathf.Min(t.onKillHeal, OnKillHealCap);
            return t;
        }
        static void Add(GearEffectTotals t, GearEffect e, object source)
        {
            if (e == null) return;
            switch (e.kind)
            {
                case "stat":
                    if (!Enum.TryParse(e.stat ?? "", out StatType stat)) return;
                    t.modifiers.Add(new StatModifier(stat, e.percent ? ModifierOp.PercentAdd : ModifierOp.Flat, e.percent ? e.amount / 100f : e.amount, source)); break;
                case "onKillHeal": t.onKillHeal += Mathf.RoundToInt(e.amount); break;
                case "onKillPower": t.onKillPower += Mathf.RoundToInt(e.amount); break;
                case "restRegen": t.restRegen += Mathf.RoundToInt(e.amount); break;
                case "coins": t.coins += e.amount / 100f; break;
                case "luck": t.luck += e.amount / 100f; break;
                case "abilityCost": case "abilityCooldown": case "abilityCastTime": case "abilityPower": case "procResource": case "procReset": case "procEcho": case "procRoot": t.abilities.Add(e); break;
                default: return;
            }
            if (!string.IsNullOrEmpty(e.text)) t.lines.Add(e.text);
        }
    }

    /// <summary>
    /// Better or worse (DESIGN.md 4, step L1): a piece's score is weapon damage + Strength + half its Stamina + a quarter of its
    /// armour and Agility. Intellect and Spirit score nothing, because nothing in the game reads them yet.
    /// </summary>
    public static class LootJudge
    {
        public static float Score(ItemDef d)
        {
            if (d == null) return 0;
            return d.weaponDamage + d.strength + .5f * d.stamina + .25f * d.armor + .25f * d.agility;
        }
        /// <summary>The candidate's score less the worn piece's (an empty slot scores 0).</summary>
        public static float Compare(ItemDef candidate, ItemDef worn) { return Score(candidate) - Score(worn); }
        /// <summary>Gear you can wear now that scores above what you wear in its slot (anything wearable beats an empty slot).</summary>
        public static bool IsUpgrade(ItemDef candidate, ItemDef worn, int playerLevel, string classId = null)
        {
            if (candidate == null || candidate.kind != "gear" || candidate.level > playerLevel) return false;
            if (!Inventory.ClassMayWear(candidate, classId)) return false;   // another class's set piece (DESIGN.md 3.8)
            return worn == null || Compare(candidate, worn) > 0;
        }
        /// <summary>The differences stat by stat ("+3 weapon damage", "-2 Stamina"), each marked as a gain or a loss. Unchanged stats are left out.</summary>
        public static List<(string line, bool gain)> DeltaLines(ItemDef candidate, ItemDef worn)
        {
            var l = new List<(string, bool)>(); if (candidate == null) return l;
            void D(int a, int b, string what) { int d = a - b; if (d != 0) l.Add(((d > 0 ? "+" : "") + d + " " + what, d > 0)); }
            D(candidate.weaponDamage, worn?.weaponDamage ?? 0, "weapon damage"); D(candidate.armor, worn?.armor ?? 0, "armor");
            D(candidate.stamina, worn?.stamina ?? 0, "Stamina"); D(candidate.strength, worn?.strength ?? 0, "Strength"); D(candidate.agility, worn?.agility ?? 0, "Agility");
            D(candidate.intellect, worn?.intellect ?? 0, "Intellect"); D(candidate.spirit, worn?.spirit ?? 0, "Spirit");
            return l;
        }
    }
}
