using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    // ============================================================================================
    // The legendary weapons and the Unraveller's loot (WORLD_BOSS_1.md section 4 and 5.1; build steps 7 and 8). The books: what a
    // Weave-Eater cannot eat (bone, amber, salt; steel goes black, book1 ch.20:155-205), glass-steel the First Era's lost art (book1
    // ch.7:149), Tynara Embercoil the Forgeborn metal-shaper (book1 world_bible.md:31), the Original Song (world_bible.md:74-78).
    // The reading that she swallowed what she could not eat, the components, the eleven weapons' names and every number: GAME-ONLY
    // (the Cleric's weapon GAME-ONLY outright; the rest CANON-EXPANDED). The Verses and the Note: PROVISIONAL.
    // ============================================================================================

    /// <summary>
    /// The component bag (save format 12): the legendary pieces outside the bags, with no slot, shown in the bags' Legendary tab. The
    /// item kind "component": bound (never sold, traded, banked or listed: it never enters the bags, so none of those can reach it),
    /// stacking to its stack. Works on any list (yours in EncounterProgress.components, a sim's in SimAdventurer.components).
    /// </summary>
    public static class ComponentBag
    {
        public const string Kind = "component";
        public static bool IsComponent(ItemDef d) { return d != null && d.kind == Kind; }
        public static List<ItemStack> Of(EncounterProgress p) { if (p.components == null) p.components = new List<ItemStack>(); return p.components; }
        public static int Count(List<ItemStack> bag, string id) { int n = 0; if (bag != null) foreach (var s in bag) if (s != null && s.item == id) n += Math.Max(0, s.count); return n; }
        public static int Count(EncounterProgress p, string id) { return Count(p != null ? p.components : null, id); }
        /// <summary>Adds up to the item's stack (a second Heart has no room: one is all anyone needs). Returns how many did not go in.</summary>
        public static int Add(List<ItemStack> bag, ItemDef d, int count)
        {
            if (bag == null || d == null || count <= 0) return count;
            var s = bag.Find(x => x != null && x.item == d.id);
            if (s == null) { s = new ItemStack { item = d.id, count = 0 }; bag.Add(s); }
            int take = Math.Min(count, Math.Max(1, d.stack) - s.count); if (take < 0) take = 0;
            s.count += take; if (s.count <= 0) bag.Remove(s);
            return count - take;
        }
        public static int Add(EncounterProgress p, ItemDef d, int count) { return Add(Of(p), d, count); }
        /// <summary>Takes n away; false (and nothing taken) when there are fewer.</summary>
        public static bool Remove(List<ItemStack> bag, string id, int n)
        {
            if (Count(bag, id) < n) return false;
            for (int i = bag.Count - 1; i >= 0 && n > 0; i--)
            {
                var s = bag[i]; if (s == null || s.item != id) continue;
                int take = Math.Min(n, s.count); s.count -= take; n -= take; if (s.count <= 0) bag.RemoveAt(i);
            }
            return true;
        }
        /// <summary>After a load: blank ids, empty stacks and nulls dropped, repeats merged (the first kept). A count below zero is not a save
        /// this game wrote (the read refuses it before this).</summary>
        public static void Normalize(List<ItemStack> bag)
        {
            if (bag == null) return;
            bag.RemoveAll(s => s == null || string.IsNullOrEmpty(s.item) || s.count <= 0);
            for (int i = 0; i < bag.Count; i++)
                for (int j = bag.Count - 1; j > i; j--)
                    if (bag[j].item == bag[i].item) { bag[i].count += bag[j].count; bag.RemoveAt(j); }
        }
    }

    /// <summary>
    /// What the Unraveller drops for each member not locked out (WORLD_BOSS_1.md 4.4, personal: nothing to win or lose against anyone):
    /// a Calcified Brood-Crystal always; the class's uneaten blank at 25%, 15% more for each kill without it; the Heart at 20%, 15% more a
    /// kill without it; neither ever takes more than <see cref="MostKills"/> kills (the sixth is certain). The blank and the Heart roll only
    /// while you have neither that piece nor the weapon forged (one weapon a character); a crystal comes every kill. Her gear: two of her
    /// eight epics, by need and greed (EncounterSession.Rolled). The counters are { component id, kills without it } (LootLuck: source, dry).
    /// </summary>
    public static class UnravellerLoot
    {
        public const string Crystal = "comp.brood_crystal", Heart = "comp.unraveller_heart";
        public const float BlankChance = .25f, HeartChance = .20f, ChanceStep = .15f;
        /// <summary>The most kills either piece can take: the sixth without it is certain.</summary>
        public const int MostKills = 6;
        /// <summary>Her epics a kill (4.7), rolled by need and greed among the raid.</summary>
        public const int EpicsAKill = 2;
        public static readonly string[] Epics = { "raid.unr.harpooners_coat", "raid.unr.harpooners_mail_coat", "raid.unr.harpooners_robe", "raid.unr.salt_crusted_mantle",
            "raid.unr.brood_bone_gauntlets", "raid.unr.drain_reach_waders", "raid.unr.knotted_amber_cord", "raid.unr.bone_buckler" };
        public const string Waders = "raid.unr.drain_reach_waders";
        /// <summary>The Waders: the river's poison this much as strong on you (4.7).</summary>
        public const float WadersPoison = .5f;
        /// <summary>The class's uneaten blank ("comp.blank.warrior").</summary>
        public static string BlankFor(string classId) { return string.IsNullOrEmpty(classId) ? null : "comp.blank." + classId.Replace("class.", ""); }
        /// <summary>The chance on the next kill after <paramref name="dry"/> kills without it: base, 15% more each, certain on the sixth.</summary>
        public static float Chance(float baseChance, int dry) { dry = Math.Max(0, dry); return dry + 1 >= MostKills ? 1 : Mathf.Min(1, baseChance + ChanceStep * dry); }
        public static LootLuck Counter(List<LootLuck> luck, string id)
        {
            var c = luck.Find(x => x != null && x.source == id);
            if (c == null) { c = new LootLuck { source = id }; luck.Add(c); }
            return c;
        }
        static int Dry(List<LootLuck> luck, string id) { var c = luck != null ? luck.Find(x => x != null && x.source == id) : null; return c != null ? c.dry : 0; }
        /// <summary>Whether a piece still rolls: not held and its weapon not forged.</summary>
        public static bool Wants(List<ItemStack> bag, string id, bool forged) { return !forged && ComponentBag.Count(bag, id) == 0; }
        /// <summary>One kill's components for one member (the counters in <paramref name="luck"/> move). <paramref name="forged"/>: that
        /// character has its class's weapon already.</summary>
        public static List<LootDrop> Roll(string classId, List<ItemStack> bag, List<LootLuck> luck, bool forged, System.Random rng)
        {
            var drops = new List<LootDrop> { new LootDrop(Crystal, 1) };
            string blank = BlankFor(classId);
            foreach (var (id, chance) in new[] { (blank, BlankChance), (Heart, HeartChance) })
            {
                if (id == null || !Wants(bag, id, forged)) continue;
                var c = Counter(luck, id); c.kills++;
                bool hit = rng.NextDouble() < Chance(chance, c.dry);
                c.dry = hit ? 0 : c.dry + 1;
                if (hit) drops.Add(new LootDrop(id, 1));
            }
            return drops;
        }
        /// <summary>The loot window's line: "Blank: 40% next kill · Heart: 35% next kill" (what you still need), or the crystals' count.</summary>
        public static string CountersLine(string classId, List<ItemStack> bag, List<LootLuck> luck, bool forged)
        {
            var parts = new List<string>(); string blank = BlankFor(classId);
            if (blank != null && Wants(bag, blank, forged)) parts.Add("Blank: " + Mathf.RoundToInt(100 * Chance(BlankChance, Dry(luck, blank))) + "% next kill");
            if (Wants(bag, Heart, forged)) parts.Add("Heart: " + Mathf.RoundToInt(100 * Chance(HeartChance, Dry(luck, Heart))) + "% next kill");
            parts.Add("Brood-Crystals " + ComponentBag.Count(bag, Crystal) + "/" + LegendaryWeapons.Crystals);
            return string.Join(" · ", parts);
        }
        /// <summary>Two of her eight epics, different ones.</summary>
        public static List<LootDrop> RollEpics(System.Random rng)
        {
            var pool = new List<string>(Epics); var drops = new List<LootDrop>();
            for (int i = 0; i < EpicsAKill && pool.Count > 0; i++) { int k = rng.Next(pool.Count); drops.Add(new LootDrop(pool[k], 1)); pool.RemoveAt(k); }
            return drops;
        }
    }

    /// <summary>One of the eleven (WORLD_BOSS_1.md 4.3): its item, class, blank, effect and the model it should have.</summary>
    public sealed class LegendWeapon
    {
        public readonly string id, classId, name; public readonly LegendEffect effect;
        /// <summary>What it wears now (the look in legendary.json) and the model it should get when one is made for it (each its own design).</summary>
        public readonly string modelNote;
        public LegendWeapon(string id, string classId, string name, LegendEffect effect, string modelNote) { this.id = id; this.classId = classId; this.name = name; this.effect = effect; this.modelNote = modelNote; }
        public string Blank { get { return UnravellerLoot.BlankFor(classId); } }
    }

    /// <summary>
    /// The forge's recipe and the weapons' two stages (4.6). Stage 1, at Tynara Embercoil's night forge (21:00-06:00): three Brood-Crystals,
    /// the class's blank, the Heart, <see cref="ForgeCoin"/> crowns and a slag-iron bar make the weapon asleep: an epic of level 20, its
    /// effect greyed. Stage 2: a Note of the Original Song at the same forge wakes it; the save marks it awake with the progress key
    /// <c>legend.awake:&lt;item id&gt;</c> (no item-state change: the same item id). Awake: its one effect works, blackened steel does not
    /// take (it was remade with salt's crystal: 4.2), and from <see cref="AwakeLevel"/> its weapon damage is a legendary's at 30.
    /// "legend.forged:&lt;item id&gt;" marks a weapon made (the blank and the Heart stop dropping). Both keys live in EncounterProgress.keys.
    /// </summary>
    public static class LegendaryWeapons
    {
        public static readonly LegendWeapon[] All = {
            new LegendWeapon("legend.second_truth", "class.warrior", "Second Truth", LegendEffect.Afterimage, "model.weapon Sword8_White now; its own glass-steel longsword model later (pale, a salt-white seam)"),
            new LegendWeapon("legend.iron_anchor", "class.paladin", "The Iron Anchor", LegendEffect.Anchor, "model.weapon Mace2H_Epic now; a glass-steel war hammer later (KayKit's hammer heads if that pack comes in)"),
            new LegendWeapon("legend.two_rules", "class.rogue", "The Two Rules", LegendEffect.ExistElsewhere, "model.weapon Sics_Dagger in the main hand now (the off hand keeps its dirk); a matched glass-steel pair later"),
            new LegendWeapon("legend.song_of_sifting", "class.scoundrel", "The Song of Sifting", LegendEffect.Sifting, "model.weapon Ashen_nth_equi_rondel_dagger now; a thin glass-steel stiletto later"),
            new LegendWeapon("legend.salt_wall_bow", "class.ranger", "The Salt-Wall Bow", LegendEffect.SaltTipped, "model.weapon Bow_Epic now; a bone-and-glass-steel longbow with amber string later"),
            new LegendWeapon("legend.sun_staff", "class.mage", "The Sun-Staff", LegendEffect.Sunfire, "model.tall Staff_Epic now; a double-ended staff on a dark haft later"),
            new LegendWeapon("legend.amber_wood_crook", "class.druid", "Amber-Wood Crook", LegendEffect.Amber, "drawn staff:crook (veridian) now; an amber-wood crook model later"),
            new LegendWeapon("legend.first_verse", "class.archivist", "The First Verse", LegendEffect.EchoJar, "model.tall Cartoon_Staff_05 now; a bone staff with an echo-jar head later"),
            new LegendWeapon("legend.menders_first_tool", "class.saltmender", "Mender's First Tool", LegendEffect.SaltLine, "drawn hammer.war:maul (pale) now; a salt-stone maul model later"),
            new LegendWeapon("legend.grohls_harpoon", "class.shaman", "Grohl's Harpoon", LegendEffect.Grounding, "drawn polearm:harpoon (ashwalker) now; a bone harpoon on amber rope model later"),
            new LegendWeapon("legend.hill_shrine_lantern", "class.cleric", "The Hill-Shrine Lantern", LegendEffect.KeptLight, "model.weapon Mace1H_Epic now; a mace with a hung lantern later"),
        };
        public const int Crystals = 3, ForgeCoin = 500;   // a week's coin at 20, about (PROVISIONAL)
        public const string Bar = "mat.slagiron_bar", Note = "comp.note_original_song", Smith = "Tynara Embercoil";
        public static readonly string[] Verses = { "comp.verse.forge", "comp.verse.deep", "comp.verse.crown" };
        public const string AwakePrefix = "legend.awake:", ForgedPrefix = "legend.forged:";
        /// <summary>Her forge burns from nine at night to six in the morning (by day she works for the Council, book1 ch.2:7).</summary>
        public const float OpensAt = 21, ShutsAt = 6;
        public const int EpicLevel = 20, AwakeLevel = 30;
        /// <summary>A weapon's damage at its stage, on the generated curve (ItemDatabase.Generate): epic at 20 (x2.1: 70), legendary at 30 (x2.5: 123).</summary>
        public static int CurveDamage(int level, int quality) { float power = level * (quality >= 5 ? 2.5f : quality == 4 ? 2.1f : 1.7f); return Mathf.Max(1, Mathf.RoundToInt(3 + power * 1.6f)); }
        public static int EpicDamage { get { return CurveDamage(EpicLevel, 4); } }
        public static int AwakeDamage { get { return CurveDamage(AwakeLevel, 5); } }
        /// <summary>A forged weapon's stat budget at the epic stage (the named gear's: 6 points per power/4 at epic).</summary>
        public static int EpicBudget { get { return Mathf.RoundToInt(6 * Mathf.Max(1, EpicLevel * 2.1f / 4)); } }

        public static LegendWeapon For(string classId) { return Array.Find(All, w => w.classId == classId); }
        public static LegendWeapon ById(string id) { return string.IsNullOrEmpty(id) ? null : Array.Find(All, w => w.id == id); }
        public static string AwakeKey(string id) { return AwakePrefix + id; }
        public static string ForgedKey(string id) { return ForgedPrefix + id; }
        static List<string> Keys(EncounterProgress p) { if (p.keys == null) p.keys = new List<string>(); return p.keys; }
        public static bool IsAwake(EncounterProgress p, string id) { return p != null && p.keys != null && p.keys.Contains(AwakeKey(id)); }
        public static bool Forged(EncounterProgress p, string id) { return p != null && p.keys != null && p.keys.Contains(ForgedKey(id)); }
        public static bool ForgedFor(EncounterProgress p) { var w = p != null ? For(p.classId) : null; return w != null && Forged(p, w.id); }
        public static bool ForgeOpen(float hour) { return hour >= OpensAt || hour < ShutsAt; }
        /// <summary>The awake weapon in the main hand, or null.</summary>
        public static LegendWeapon AwakeInHand(EncounterProgress p)
        {
            if (p == null || p.equipment == null || p.equipment.Count <= (int)EquipSlot.MainHand) return null;
            var w = ById(p.equipment[(int)EquipSlot.MainHand].item); return w != null && IsAwake(p, w.id) ? w : null;
        }
        /// <summary>The awake weapon's weapon damage over its epic damage (added in Inventory.Totals), from <see cref="AwakeLevel"/> only.</summary>
        public static int AwakeBonus(EncounterProgress p) { return AwakeInHand(p) != null && p.Level >= AwakeLevel ? AwakeDamage - EpicDamage : 0; }

        /// <summary>What is still wanting for the forge, in words; empty when it can be made. Null when this class has no weapon or it is made.</summary>
        public static List<string> Missing(EncounterProgress p, ItemDatabase db)
        {
            var w = For(p.classId); if (w == null || Forged(p, w.id)) return null;
            var l = new List<string>(); string Name(string id) { var d = db?.Get(id); return d != null ? d.name : id; }
            int c = ComponentBag.Count(p, UnravellerLoot.Crystal); if (c < Crystals) l.Add((Crystals - c) + " more " + Name(UnravellerLoot.Crystal) + (Crystals - c > 1 ? "s" : ""));
            if (ComponentBag.Count(p, w.Blank) < 1) l.Add(Name(w.Blank));
            if (ComponentBag.Count(p, UnravellerLoot.Heart) < 1) l.Add(Name(UnravellerLoot.Heart));
            if (p.gold < ForgeCoin) l.Add(ForgeCoin + " crowns (you have " + p.gold + ")");
            if (Inventory.Count(p, Bar) < 1) l.Add("a " + Name(Bar));
            return l;
        }
        /// <summary>Stage 1: the pieces, the coin and the bar spent, the weapon (asleep) in the bags and the forged key set. False with why.</summary>
        public static bool Forge(EncounterProgress p, ItemDatabase db, out string why)
        {
            why = null; var w = For(p.classId);
            if (w == null || db == null || db.Get(w.id) == null) { why = "Tynara has nothing of yours to remake."; return false; }
            if (Forged(p, w.id)) { why = "She has made yours already."; return false; }
            var missing = Missing(p, db); if (missing.Count > 0) { why = "Still wanting: " + string.Join(", ", missing) + "."; return false; }
            if (Inventory.Room(p, db, w.id) < 1) { why = "Your bags are full."; return false; }
            ComponentBag.Remove(p.components, UnravellerLoot.Crystal, Crystals); ComponentBag.Remove(p.components, w.Blank, 1); ComponentBag.Remove(p.components, UnravellerLoot.Heart, 1);
            p.gold -= ForgeCoin; Inventory.Remove(p, Bar, 1);
            Inventory.Add(p, db, w.id, 1); Keys(p).Add(ForgedKey(w.id));
            return true;
        }
        /// <summary>Three Verses make one Note (in the component bag). False when they are not all there.</summary>
        public static bool BindVerses(EncounterProgress p, ItemDatabase db)
        {
            foreach (var v in Verses) if (ComponentBag.Count(p, v) < 1) return false;
            var note = db?.Get(Note); if (note == null || ComponentBag.Count(p, Note) > 0) return false;
            foreach (var v in Verses) ComponentBag.Remove(p.components, v, 1);
            ComponentBag.Add(p, note, 1); return true;
        }
        /// <summary>Stage 2: the Note spent and the weapon woken (the awake key). It must be yours (made, and carried or worn).</summary>
        public static bool Wake(EncounterProgress p, ItemDatabase db, out string why)
        {
            why = null; var w = For(p.classId);
            if (w == null || !Forged(p, w.id)) { why = "Nothing of yours sleeps at her forge yet."; return false; }
            if (IsAwake(p, w.id)) { why = w.name + " is awake already."; return false; }
            if (!Inventory.Has(p, w.id)) { why = "Bring " + w.name + " to the forge."; return false; }
            BindVerses(p, db);
            if (ComponentBag.Count(p, Note) < 1) { why = w.name + " sleeps: a note of the Original Song would wake it."; return false; }
            ComponentBag.Remove(p.components, Note, 1); Keys(p).Add(AwakeKey(w.id));
            return true;
        }
        /// <summary>The test fixture until the Verses' bosses exist (4.5; tests and /legend note): a Note in the component bag.</summary>
        public static void GiveTestNote(EncounterProgress p, ItemDatabase db) { var n = db?.Get(Note); if (n != null && ComponentBag.Count(p, Note) == 0) ComponentBag.Add(p, n, 1); }
        /// <summary>Its tooltip line: asleep (greyed) or awake.</summary>
        public static string StageLine(EncounterProgress p, string id)
        {
            var w = ById(id); if (w == null) return null;
            return IsAwake(p, id) ? "Awake: legendary. " + LegendEffects.Words(w.effect) + " Blackened steel does not take on it." + (p != null && p.Level < AwakeLevel ? " (Legendary damage from level " + AwakeLevel + ".)" : "")
                : "Sleeps. A note of the Original Song would wake it.";
        }
    }

    public enum LegendEffect { None, Afterimage, Anchor, ExistElsewhere, Sifting, SaltTipped, Sunfire, Amber, EchoJar, SaltLine, Grounding, KeptLight }

    /// <summary>
    /// The eleven effects (4.6; one each, about 3-5% of the build's output, for every path of the class; GAME-ONLY numbers, the harness
    /// measures them with a test Note at the cap later). The numbers, the words, the estimated worth (<see cref="EstimatedShare"/>, from
    /// the stated shares of a build's output; the harness's measure replaces them), and the state each needs (<see cref="LegendState"/>).
    /// </summary>
    public static class LegendEffects
    {
        public const int AfterimageEvery = 5; public const float AfterimageShare = .4f;
        public const float AnchorBelow = .3f, AnchorTaken = .75f, AnchorSeconds = 6, AnchorEvery = 60;
        public const float BlinkBlowShare = .2f, BlinkMeters = 5, BlinkEvery = 30;
        public const float HoldStretch = 1.2f, BreakBonus = 1.3f;
        public const int SaltTipEvery = 6; public const float SaltTipScale = 1.5f;
        public const float SunfireRadius = 3, SunfireSeconds = 4, SunfirePerLevel = .5f;   // a second, on every foe in it (10 at 20)
        public const float AmberEvery = 20, AmberWardShare = .05f, AmberWardSeconds = 8, AmberRoot = 1.5f;
        public const int EchoEvery = 8; public const float EchoShare = .5f;
        public const float SaltLineEvery = 20, SaltLineStun = 2;
        public const float GroundingEvery = 20, GroundingSeconds = 3;
        public const float KeptLightShare = .1f, KeptLightSeconds = 8;
        /// <summary>The shares of a build's output the estimates take (PROVISIONAL, from the meter's runs): weapon swings and auto-shots of a
        /// weapon build, spells of a caster, how often a Mage is at full Heat (a spell in this many), how much healing goes past full.</summary>
        public const float SwingShare = .45f, SpellShare = .6f, FullHeatEvery = 12, Overheal = .3f, HoldShare = .25f, TakenUnder30 = .25f;
        public static string Words(LegendEffect e)
        {
            switch (e)
            {
                case LegendEffect.Afterimage: return "Afterimage: every 5th blow leaves a gold afterimage that strikes again for 40%.";
                case LegendEffect.Anchor: return "The Anchor: falling under 30% health roots you and takes 25% off every blow for 6 s, once a minute.";
                case LegendEffect.ExistElsewhere: return "Exist Elsewhere: a blow of over 20% of your health moves you 5 m behind your foe, once every 30 s.";
                case LegendEffect.Sifting: return "Sifting: your holds last 20% longer, and the first blow on a held foe as it breaks does 30% more.";
                case LegendEffect.SaltTipped: return "Salt-Tipped: every 6th shot does 50% more and strips one good effect (double on things of the Void).";
                case LegendEffect.Sunfire: return "Sunfire: a spell cast at full Heat leaves a 3 m patch of sunfire for 4 s (foes only).";
                case LegendEffect.Amber: return "Amber: every 20 s your next heal wraps its target in amber (a ward no Weave-Eater can eat), or your next blow roots for 1.5 s.";
                case LegendEffect.EchoJar: return "Echo-Jar: every 8th spell is kept in the jar, and your next spell is echoed at 50%.";
                case LegendEffect.SaltLine: return "Salt Line: a salt working also calcifies its foe for 2 s, once every 20 s.";
                case LegendEffect.Grounding: return "Grounding: every 20 s your next spell pins its target 3 s (no blink, no move); things of the Void are silenced too.";
                case LegendEffect.KeptLight: return "Kept Light: healing past full becomes a ward of up to 10% of the target's health for 8 s.";
                default: return "";
            }
        }
        /// <summary>
        /// What each is worth, as a share of the build's output (damage, healing, or damage not taken), from the shares above. The band test
        /// holds every one inside 2-6% ("about 3-5%"). For a hold or a pin: the share of a foe's output it stops while it lasts.
        /// </summary>
        public static float EstimatedShare(LegendEffect e)
        {
            switch (e)
            {
                case LegendEffect.Afterimage: return AfterimageShare / AfterimageEvery * SwingShare;   // .036
                case LegendEffect.Anchor: return (1 - AnchorTaken) * AnchorSeconds / AnchorEvery * (1 + TakenUnder30 * 4) * .5f;   // the 6 s come when it matters most: .025
                case LegendEffect.ExistElsewhere: return BlinkBlowShare * .5f * 10 / BlinkEvery * .66f;   // half the next heavy blows of a 10 s window behind it turned, once in 30 s: .022
                case LegendEffect.Sifting: return (HoldStretch - 1) * HoldShare * .4f + (BreakBonus - 1) * .1f;   // .02 + .03 = .05 (control paths count the longer holds)
                case LegendEffect.SaltTipped: return (SaltTipScale - 1) / SaltTipEvery * SwingShare;   // .0375 (the strip not counted)
                case LegendEffect.Sunfire: return SunfirePerLevel * SunfireSeconds / (FullHeatEvery * 2.6f) * SpellShare;   // a spell does about 2.6 a level: one patch (2 a level) in twelve spells, on one foe: .0385
                case LegendEffect.Amber: return AmberWardShare * 1 / (AmberEvery / 2.5f) * 4.5f;   // a 5% ward every 20 s against a heal of about 7.5% every 2.5 s: .028
                case LegendEffect.EchoJar: return EchoShare / EchoEvery * SpellShare;   // .0375
                case LegendEffect.SaltLine: return SaltLineStun / SaltLineEvery * .4f;   // a 2 s calcify in 20 s on what you fight: .04
                case LegendEffect.Grounding: return GroundingSeconds / GroundingEvery * .25f;   // a 3 s pin in 20 s (a blink or a charge stopped): .0375
                case LegendEffect.KeptLight: return KeptLightShare * Overheal * 1.2f;   // the overheal kept, capped: .036
                default: return 0;
            }
        }
    }

    /// <summary>One wielder's effect state (the session keeps yours): counts and cooldowns. Pure: the caller carries the result out.</summary>
    public sealed class LegendState
    {
        public LegendEffect effect;
        int blows, shots, spells; bool echoNext;
        float anchorReady = float.MinValue, blinkReady = float.MinValue, amberReady = float.MinValue, saltReady = float.MinValue, groundReady = float.MinValue;
        public LegendState(LegendEffect effect = LegendEffect.None) { this.effect = effect; }
        /// <summary>A landed swing: the afterimage's share of it to strike again (Warrior), or 0.</summary>
        public float Swing() { if (effect != LegendEffect.Afterimage) return 0; return ++blows % LegendEffects.AfterimageEvery == 0 ? LegendEffects.AfterimageShare : 0; }
        /// <summary>A landed shot: salt-tipped (Ranger: 50% more and a good effect stripped).</summary>
        public bool Shot() { return effect == LegendEffect.SaltTipped && ++shots % LegendEffects.SaltTipEvery == 0; }
        /// <summary>How much longer your holds last (Scoundrel 1.2).</summary>
        public float HoldScale { get { return effect == LegendEffect.Sifting ? LegendEffects.HoldStretch : 1; } }
        /// <summary>A blow on a held foe as it breaks (Scoundrel 1.3).</summary>
        public float BreakScale(bool held) { return effect == LegendEffect.Sifting && held ? LegendEffects.BreakBonus : 1; }
        /// <summary>The Anchor: crossing under 30% health with it ready (then once a minute).</summary>
        public bool Anchor(float now, float ratioBefore, float ratioAfter)
        {
            if (effect != LegendEffect.Anchor || now < anchorReady || !(ratioBefore >= LegendEffects.AnchorBelow && ratioAfter < LegendEffects.AnchorBelow) || ratioAfter <= 0) return false;
            anchorReady = now + LegendEffects.AnchorEvery; return true;
        }
        /// <summary>Exist Elsewhere: a blow of over a fifth of your health, ready (then 30 s).</summary>
        public bool Blink(float now, float blowShare)
        {
            if (effect != LegendEffect.ExistElsewhere || now < blinkReady || blowShare <= LegendEffects.BlinkBlowShare) return false;
            blinkReady = now + LegendEffects.BlinkEvery; return true;
        }
        /// <summary>Amber (Druid): the next heal's ward or the next blow's root, every 20 s.</summary>
        public bool Amber(float now) { if (effect != LegendEffect.Amber || now < amberReady) return false; amberReady = now + LegendEffects.AmberEvery; return true; }
        /// <summary>A spell resolves: the echo's share to run it again at (Archivist: the one after every 8th), else 0.</summary>
        public float Spell()
        {
            if (effect != LegendEffect.EchoJar) return 0;
            if (echoNext) { echoNext = false; return LegendEffects.EchoShare; }
            if (++spells % LegendEffects.EchoEvery == 0) echoNext = true;
            return 0;
        }
        /// <summary>Salt Line (Salt-Mender): a salt working at a foe calcifies it, every 20 s.</summary>
        public bool SaltLine(float now) { if (effect != LegendEffect.SaltLine || now < saltReady) return false; saltReady = now + LegendEffects.SaltLineEvery; return true; }
        /// <summary>Grounding (Shaman): the next spell at a foe pins it, every 20 s.</summary>
        public bool Ground(float now) { if (effect != LegendEffect.Grounding || now < groundReady) return false; groundReady = now + LegendEffects.GroundingEvery; return true; }
        /// <summary>Sunfire (Mage): a spell cast at full Heat.</summary>
        public bool Sunfire(bool fullHeat) { return effect == LegendEffect.Sunfire && fullHeat; }
        /// <summary>Sunfire's burn a second on each foe in it, by your level.</summary>
        public static int SunfireTick(int level) { return Mathf.Max(1, Mathf.RoundToInt(LegendEffects.SunfirePerLevel * Mathf.Max(1, level))); }
        /// <summary>Kept Light (Cleric): the ward from a heal's overheal, capped at a tenth of the target's health.</summary>
        public int KeptLight(int asked, int got, int targetMax) { if (effect != LegendEffect.KeptLight) return 0; return Mathf.Clamp(asked - got, 0, Mathf.RoundToInt(LegendEffects.KeptLightShare * targetMax)); }
        /// <summary>Amber's ward on a heal's target.</summary>
        public static int AmberWard(int targetMax) { return Mathf.Max(1, Mathf.RoundToInt(LegendEffects.AmberWardShare * targetMax)); }
    }
}
