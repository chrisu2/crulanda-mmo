using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.Persistence;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Unraveller's loot and the legendary weapons (WORLD_BOSS_1.md section 4; build steps 7 and 8): the drops over 10,000 kills with
    /// the bad-luck counters capped at six kills, the component bag and its kind, her epics, the save's format 12 round trip and the step
    /// from 11, the eleven weapons' stats and looks in their band, each effect's numbers and worth in its band, and the recipe at Tynara
    /// Embercoil's forge with the awake key (the test Note standing in for the Verses' bosses, 4.5).
    /// </summary>
    public class LegendaryTests
    {
        static string LegendaryPath { get { return Path.Combine(LootTestData.Content, "Items", "legendary.json"); } }
        static ItemDatabase Items() { return LootTestData.Items(); }
        static readonly string[] Classes = SimRoster.ClassIds;

        // ---------- the drops ----------
        [Test] public void The_counters_climb_fifteen_a_kill_and_the_sixth_kill_is_certain()
        {
            CollectionAssert.AreEqual(new[] { 25, 40, 55, 70, 85, 100 }, Enumerable.Range(0, 6).Select(d => Mathf.RoundToInt(100 * UnravellerLoot.Chance(UnravellerLoot.BlankChance, d))).ToArray(), "the blank");
            CollectionAssert.AreEqual(new[] { 20, 35, 50, 65, 80, 100 }, Enumerable.Range(0, 6).Select(d => Mathf.RoundToInt(100 * UnravellerLoot.Chance(UnravellerLoot.HeartChance, d))).ToArray(), "the Heart: certain on the sixth, never a seventh");
            Assert.AreEqual(1, UnravellerLoot.Chance(UnravellerLoot.HeartChance, 9), "past the cap, still certain");
        }

        [Test] public void Ten_thousand_kills_a_crystal_each_and_never_more_than_six_kills_for_a_piece()
        {
            var items = Items(); var rng = new System.Random(20261019);
            foreach (var cls in Classes)
            {
                var bag = new List<ItemStack>(); var luck = new List<LootLuck>(); string blank = UnravellerLoot.BlankFor(cls);
                Assert.NotNull(items.Get(blank), blank + " is an item");
                int crystals = 0, blanks = 0, hearts = 0, sinceBlank = 0, sinceHeart = 0, worstBlank = 0, worstHeart = 0;
                for (int kill = 0; kill < 10000; kill++)
                {
                    var drops = UnravellerLoot.Roll(cls, bag, luck, false, rng);
                    sinceBlank++; sinceHeart++;
                    foreach (var d in drops)
                    {
                        if (d.item == UnravellerLoot.Crystal) crystals++;
                        else if (d.item == blank) { blanks++; worstBlank = Math.Max(worstBlank, sinceBlank); sinceBlank = 0; }
                        else if (d.item == UnravellerLoot.Heart) { hearts++; worstHeart = Math.Max(worstHeart, sinceHeart); sinceHeart = 0; }
                        else Assert.Fail(cls + ": " + d.item + " is not hers to give this class");
                    }
                    foreach (var l in luck) Assert.LessOrEqual(l.dry, UnravellerLoot.MostKills - 1, cls + ": " + l.source + " never more than five kills dry");
                    // Each piece is spent at the forge (or the run would stop dropping it): it rolls again from nothing.
                    bag.Clear();
                }
                Assert.AreEqual(10000, crystals, cls + ": a crystal every kill");
                Assert.LessOrEqual(worstBlank, UnravellerLoot.MostKills, cls + ": the blank never took more than six kills");
                Assert.LessOrEqual(worstHeart, UnravellerLoot.MostKills, cls + ": the Heart never took more than six kills");
                // The average wait is about 2.4 kills for the blank and about 2.7 for the Heart (the table above); a wide band for the seed.
                Assert.That(10000f / blanks, Is.InRange(2.1f, 2.8f), cls + ": the blank's average wait");
                Assert.That(10000f / hearts, Is.InRange(2.3f, 3.1f), cls + ": the Heart's average wait");
                Assert.AreEqual(10000, luck.Find(l => l.source == blank).kills, "every kill counted");
            }
        }

        [Test] public void A_piece_held_or_a_weapon_forged_stops_its_drop_but_never_the_crystal()
        {
            var items = Items(); var rng = new System.Random(3);
            var bag = new List<ItemStack>(); var luck = new List<LootLuck>();
            ComponentBag.Add(bag, items.Get(UnravellerLoot.Heart), 1); ComponentBag.Add(bag, items.Get("comp.blank.mage"), 1);
            for (int i = 0; i < 50; i++) CollectionAssert.AreEqual(new[] { UnravellerLoot.Crystal }, UnravellerLoot.Roll("class.mage", bag, luck, false, rng).Select(d => d.item).ToArray());
            var empty = new List<ItemStack>();
            for (int i = 0; i < 50; i++) CollectionAssert.AreEqual(new[] { UnravellerLoot.Crystal }, UnravellerLoot.Roll("class.mage", empty, luck, true, rng).Select(d => d.item).ToArray(), "forged: crystals only");
            Assert.IsEmpty(luck, "no counter moves for a piece not rolled");
            StringAssert.Contains("Brood-Crystals 0/3", UnravellerLoot.CountersLine("class.mage", empty, luck, true));
            StringAssert.Contains("Blank: 25% next kill", UnravellerLoot.CountersLine("class.mage", empty, luck, false));
            StringAssert.Contains("Heart: 20% next kill", UnravellerLoot.CountersLine("class.mage", empty, luck, false));
        }

        [Test] public void Her_epics_are_two_of_eight_level_twenty_epics()
        {
            var items = Items(); var rng = new System.Random(8); var seen = new HashSet<string>();
            Assert.AreEqual(8, UnravellerLoot.Epics.Length);
            for (int i = 0; i < 500; i++)
            {
                var two = UnravellerLoot.RollEpics(rng).Select(d => d.item).ToList();
                Assert.AreEqual(2, two.Distinct().Count(), "two different pieces a kill"); two.ForEach(x => seen.Add(x));
            }
            Assert.AreEqual(8, seen.Count, "every one of the eight comes");
            foreach (var id in UnravellerLoot.Epics)
            {
                var d = items.Get(id); Assert.NotNull(d, id);
                Assert.AreEqual("gear", d.kind, id); Assert.AreEqual(4, d.quality, id + " is epic"); Assert.AreEqual(20, d.level, id + " is level 20");
                Assert.AreEqual(LegendaryWeapons.EpicBudget, d.stamina + d.strength + d.agility + d.intellect + d.spirit, id + " spends an epic's budget at 20");
                float mult = d.slot == "chest" || d.slot == "legs" ? 2.2f : d.slot == "offhand" ? 2.6f : d.slot == "neck" ? 0 : 1.4f;
                Assert.AreEqual(Mathf.RoundToInt(20 * 2.1f * mult), d.armor, id + ": armour on the epic curve at 20");
                Assert.IsTrue(string.IsNullOrEmpty(d.classOnly), id + " is anyone's (need and greed)");
            }
        }

        // ---------- the component bag ----------
        [Test] public void Components_go_in_their_own_bag_bound_and_stacked()
        {
            var items = Items(); var p = EncounterSession.FreshProgress("class.ranger"); Inventory.Ensure(p);
            var before = p.bag.Select(s => s.item + s.count).ToList();
            Assert.AreEqual(0, Inventory.Add(p, items, UnravellerLoot.Crystal, 5));
            Assert.AreEqual(5, ComponentBag.Count(p, UnravellerLoot.Crystal)); CollectionAssert.AreEqual(before, p.bag.Select(s => s.item + s.count).ToList(), "no bag slot taken");
            Assert.AreEqual(0, Inventory.Add(p, items, UnravellerLoot.Heart, 1)); Assert.AreEqual(1, Inventory.Add(p, items, UnravellerLoot.Heart, 1), "one Heart is all anyone holds");
            Assert.AreEqual(0, Inventory.Room(p, items, UnravellerLoot.Heart)); Assert.AreEqual(94, Inventory.Room(p, items, UnravellerLoot.Crystal));
            foreach (var d in items.Items.Values.Where(ComponentBag.IsComponent))
            {
                Assert.AreEqual(0, d.value, d.id + " is bound: no value"); Assert.IsFalse(Inventory.IsJunk(d), d.id);
                Assert.IsFalse(Inventory.CanEquip(d, 20, out _), d.id); Assert.IsFalse(string.IsNullOrEmpty(d.canonStatus), d.id + " is labelled");
            }
            Assert.AreEqual(3 + 11 + 3, items.Items.Values.Count(ComponentBag.IsComponent), "the crystal, the Heart, the Note, eleven blanks, three Verses");
            Assert.Throws<ArgumentException>(() => ItemDatabase.Parse(new[] { "{\"items\":[{\"id\":\"comp.x\",\"name\":\"X\",\"kind\":\"component\",\"value\":3}]}" }), "a component with a price is refused");
            var bag = new List<ItemStack> { new ItemStack { item = "a", count = 2 }, null, new ItemStack { item = "", count = 1 }, new ItemStack { item = "a", count = 1 }, new ItemStack { item = "b", count = 0 } };
            ComponentBag.Normalize(bag); Assert.AreEqual(1, bag.Count); Assert.AreEqual(3, bag[0].count, "repeats merged, blanks and empties dropped");
        }

        // ---------- the save ----------
        [Test] public void Format_12_round_trips_the_pieces_counters_and_awake_key()
        {
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-legend-" + Guid.NewGuid().ToString("N"));
            try
            {
                var p = EncounterSession.FreshProgress();
                p.components.Add(new ItemStack { item = UnravellerLoot.Crystal, count = 2 }); p.components.Add(new ItemStack { item = "comp.blank.warrior", count = 1 });
                p.componentLuck.Add(new LootLuck { source = UnravellerLoot.Heart, kills = 4, dry = 4 });
                p.keys.Add(LegendaryWeapons.ForgedKey("legend.second_truth")); p.keys.Add(LegendaryWeapons.AwakeKey("legend.second_truth"));
                var save = new EncounterSave(root, TestTalents.Warrior()); save.Write(p);
                Assert.IsTrue(new SaveFileStore(root).TryRead("encounter", out var envelope, out _));
                Assert.AreEqual(12, EncounterSave.FormatVersion); Assert.AreEqual(12, envelope.formatVersion);
                StringAssert.Contains("\"components\":[{\"item\":\"comp.brood_crystal\",\"count\":2}", envelope.payloadJson);
                StringAssert.Contains("\"componentLuck\":[{\"source\":\"comp.unraveller_heart\",\"kills\":4,\"dry\":4}],\"lockouts\":", envelope.payloadJson, "before the lockouts");
                StringAssert.Contains("],\"recipes\":[],\"discoveries\":[]", envelope.payloadJson, "the recipes still come before the discoveries");
                StringAssert.EndsWith("\"lootLuck\":[]}", envelope.payloadJson, "a payload still ends as it did");
                Assert.AreEqual(envelope.payloadJson, new AddComponentsMigration().Migrate(envelope.payloadJson), "a format-12 payload needs nothing from the step");
                Assert.IsTrue(save.Read(out var loaded, out var message), message);
                Assert.AreEqual(JsonUtility.ToJson(p), JsonUtility.ToJson(loaded), "it reads back the same");
                Assert.IsTrue(LegendaryWeapons.IsAwake(loaded, "legend.second_truth"));
                // A count below zero is not a save this game wrote.
                loaded.components[0].count = -1; save.Write(loaded);
                Assert.IsFalse(save.Read(out _, out message), "a negative count is refused");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test] public void A_format_11_save_migrates_to_12_and_keeps_every_character()
        {
            var step = new AddComponentsMigration(); Assert.AreEqual(11, step.FromVersion); Assert.AreEqual(12, step.ToVersion);
            string v11 = new AddRecipesMigration().Migrate(new SteeperCurveMigration().Migrate(new AddArmouryMigration().Migrate(OwnerSaveFixtures.V8Payload))), v12 = step.Migrate(v11);
            Assert.AreEqual(v11.Substring(0, v11.Length - 1) + ",\"components\":[],\"componentLuck\":[]}", v12, "every character of the v11 payload kept");
            Assert.AreEqual(v12, step.Migrate(v12));
            Assert.AreEqual("{\"componentLuck\":[],\"components\":[]}", step.Migrate("{\"componentLuck\":[]}"), "only the missing list");
            Assert.AreEqual("{\"components\":[],\"componentLuck\":[]}", step.Migrate("{}"));
            Assert.Throws<SaveMigrationException>(() => step.Migrate("[1]")); Assert.Throws<SaveMigrationException>(() => step.Migrate(null));
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-legend11-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SaveFileStore(root);
                store.Write("encounter", new SaveEnvelope { formatVersion = 11, payloadType = "CrulandaEncounter", gameVersion = "0.3.0", savedAtUtc = "2026-10-19T20:00:00Z", payloadJson = v11 });
                string onDisk = File.ReadAllText(store.PathFor("encounter"));
                var save = new EncounterSave(root, TestTalents.Warrior());
                Assert.IsTrue(save.Read(out var p, out var message), message);
                Assert.AreEqual(onDisk, File.ReadAllText(store.PathFor("encounter")), "read in memory: the file untouched until the next save");
                Assert.NotNull(p.components); Assert.IsEmpty(p.components); Assert.NotNull(p.componentLuck); Assert.IsEmpty(p.componentLuck);
                p.components.Add(new ItemStack { item = UnravellerLoot.Crystal, count = 1 });
                save.Write(p); Assert.IsTrue(store.TryRead("encounter", out var envelope, out _)); Assert.AreEqual(12, envelope.formatVersion);
                Assert.IsTrue(save.Read(out var again, out message), message); Assert.AreEqual(1, ComponentBag.Count(again, UnravellerLoot.Crystal));
                // The whole chain from the owner's morning (format 6) ends with the two lists.
                var from6 = new SaveMigrator(); foreach (var s in new ISaveMigration[] { new AddDiscoveriesMigration(), new AddProfessionsMigration(), new AddArmouryMigration(), new SteeperCurveMigration(), new AddRecipesMigration(), step }) from6.Register(s);
                StringAssert.EndsWith(",\"recipes\":[],\"lockouts\":[],\"components\":[],\"componentLuck\":[]}", from6.MigrateToVersion(new SaveEnvelope { formatVersion = 6, payloadType = "CrulandaEncounter", payloadJson = OwnerSaveFixtures.V6Payload }, 12).payloadJson);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        // ---------- the weapons ----------
        [Test] public void Eleven_weapons_one_a_class_each_an_epic_at_twenty_on_the_curve()
        {
            var items = Items(); var loot = LootDatabase.Parse(new[] { File.ReadAllText(LegendaryPath) }, items, LootTestData.Looks());
            Assert.AreEqual(11, LegendaryWeapons.All.Length); CollectionAssert.AreEquivalent(Classes, LegendaryWeapons.All.Select(w => w.classId));
            Assert.AreEqual(11, LegendaryWeapons.All.Select(w => w.effect).Distinct().Count(), "one effect each, none shared");
            Assert.AreEqual(70, LegendaryWeapons.EpicDamage, "epic at 20: about 70 (4.6)"); Assert.AreEqual(123, LegendaryWeapons.AwakeDamage, "legendary at 30: about 123");
            Assert.AreEqual(63, LegendaryWeapons.EpicBudget);
            var looks = new HashSet<string>();
            foreach (var w in LegendaryWeapons.All)
            {
                var d = items.Get(w.id); Assert.NotNull(d, w.id);
                Assert.AreEqual("gear", d.kind); Assert.AreEqual("mainhand", d.slot, w.id); Assert.AreEqual(w.classId, d.classOnly, w.id + " is its class's only");
                Assert.AreEqual(4, d.quality, w.id + " is epic asleep"); Assert.AreEqual(20, d.level, w.id + " is usable at 20");
                Assert.AreEqual(LegendaryWeapons.EpicDamage, d.weaponDamage, w.id); Assert.AreEqual(LegendaryWeapons.EpicBudget, d.stamina + d.strength + d.agility + d.intellect + d.spirit, w.id + " spends the budget");
                Assert.AreEqual(w.name, d.name); Assert.IsFalse(string.IsNullOrEmpty(d.canonStatus), w.id + " is labelled");
                Assert.AreEqual(w.classId == "class.cleric" ? "GAME-ONLY" : "CANON-EXPANDED", d.canonStatus, w.id);
                Assert.NotNull(items.Get(w.Blank), w.Blank);
                var g = loot.Meta(w.id); Assert.NotNull(g, w.id + " has a gear entry"); Assert.IsTrue(g.unique, w.id + " is unique");
                Assert.AreEqual("Remade at Tynara Embercoil's night forge", LootDatabase.SourceText(g.source));
                Assert.IsTrue(looks.Add(g.look), w.id + ": each its own look");
                Assert.IsFalse(string.IsNullOrEmpty(w.modelNote), w.id + " says which model it should get");
                foreach (var word in new[] { "engine", "gear", "spring", "clockwork", "machine", "piston" }) StringAssert.DoesNotContain(word, (d.name + d.description).ToLowerInvariant(), "no machines");
            }
            foreach (var id in UnravellerLoot.Epics) Assert.AreEqual("Dropped by The Unraveller", LootDatabase.SourceText(loot.Meta(id).source));
        }

        [Test] public void Each_effect_is_worth_about_three_to_five_percent()
        {
            foreach (var w in LegendaryWeapons.All)
            {
                float share = LegendEffects.EstimatedShare(w.effect);
                Assert.That(share, Is.InRange(.02f, .06f), w.name + " (" + w.effect + "): " + share.ToString("P1") + " of the build's output");
                Assert.IsFalse(string.IsNullOrEmpty(LegendEffects.Words(w.effect)), w.effect + " has its words");
            }
            // The doc's numbers (4.6).
            Assert.AreEqual(5, LegendEffects.AfterimageEvery); Assert.AreEqual(.4f, LegendEffects.AfterimageShare);
            Assert.AreEqual(.3f, LegendEffects.AnchorBelow); Assert.AreEqual(.75f, LegendEffects.AnchorTaken); Assert.AreEqual(6, LegendEffects.AnchorSeconds); Assert.AreEqual(60, LegendEffects.AnchorEvery);
            Assert.AreEqual(.2f, LegendEffects.BlinkBlowShare); Assert.AreEqual(5, LegendEffects.BlinkMeters); Assert.AreEqual(30, LegendEffects.BlinkEvery);
            Assert.AreEqual(1.2f, LegendEffects.HoldStretch); Assert.AreEqual(1.3f, LegendEffects.BreakBonus);
            Assert.AreEqual(6, LegendEffects.SaltTipEvery); Assert.AreEqual(1.5f, LegendEffects.SaltTipScale);
            Assert.AreEqual(3, LegendEffects.SunfireRadius); Assert.AreEqual(4, LegendEffects.SunfireSeconds);
            Assert.AreEqual(20, LegendEffects.AmberEvery); Assert.AreEqual(1.5f, LegendEffects.AmberRoot);
            Assert.AreEqual(8, LegendEffects.EchoEvery); Assert.AreEqual(.5f, LegendEffects.EchoShare);
            Assert.AreEqual(20, LegendEffects.SaltLineEvery); Assert.AreEqual(2, LegendEffects.SaltLineStun);
            Assert.AreEqual(20, LegendEffects.GroundingEvery); Assert.AreEqual(3, LegendEffects.GroundingSeconds);
            Assert.AreEqual(.1f, LegendEffects.KeptLightShare); Assert.AreEqual(8, LegendEffects.KeptLightSeconds);
        }

        [Test] public void Each_effect_fires_as_its_words_say()
        {
            var a = new LegendState(LegendEffect.Afterimage);
            CollectionAssert.AreEqual(new[] { 0f, 0, 0, 0, .4f, 0, 0, 0, 0, .4f }, Enumerable.Range(0, 10).Select(_ => a.Swing()).ToArray(), "every 5th blow, 40%");
            Assert.AreEqual(0, new LegendState(LegendEffect.SaltTipped).Swing(), "only Second Truth's");
            var s = new LegendState(LegendEffect.SaltTipped);
            CollectionAssert.AreEqual(new[] { false, false, false, false, false, true, false }, Enumerable.Range(0, 7).Select(_ => s.Shot()).ToArray(), "every 6th shot");
            var anchor = new LegendState(LegendEffect.Anchor);
            Assert.IsFalse(anchor.Anchor(0, .5f, .35f), "not under 30% yet"); Assert.IsTrue(anchor.Anchor(1, .35f, .25f), "crossing under 30%");
            Assert.IsFalse(anchor.Anchor(30, .4f, .2f), "once a minute"); Assert.IsTrue(anchor.Anchor(61.5f, .4f, .2f)); Assert.IsFalse(anchor.Anchor(200, .25f, .2f), "only on crossing");
            var rogue = new LegendState(LegendEffect.ExistElsewhere);
            Assert.IsFalse(rogue.Blink(0, .2f), "over a fifth, not a fifth"); Assert.IsTrue(rogue.Blink(0, .21f)); Assert.IsFalse(rogue.Blink(29, .5f), "30 s"); Assert.IsTrue(rogue.Blink(30, .5f));
            var sift = new LegendState(LegendEffect.Sifting);
            Assert.AreEqual(1.2f, sift.HoldScale); Assert.AreEqual(1.3f, sift.BreakScale(true)); Assert.AreEqual(1f, sift.BreakScale(false)); Assert.AreEqual(1f, new LegendState().HoldScale);
            var echo = new LegendState(LegendEffect.EchoJar);
            var shares = Enumerable.Range(0, 18).Select(_ => echo.Spell()).ToArray();
            Assert.AreEqual(.5f, shares[8], "the spell after the 8th is echoed at 50%"); Assert.AreEqual(2, shares.Count(x => x > 0), "twice in 18");
            var salt = new LegendState(LegendEffect.SaltLine); Assert.IsTrue(salt.SaltLine(0)); Assert.IsFalse(salt.SaltLine(19)); Assert.IsTrue(salt.SaltLine(20));
            var ground = new LegendState(LegendEffect.Grounding); Assert.IsTrue(ground.Ground(5)); Assert.IsFalse(ground.Ground(24)); Assert.IsTrue(ground.Ground(25));
            var amber = new LegendState(LegendEffect.Amber); Assert.IsTrue(amber.Amber(0)); Assert.IsFalse(amber.Amber(10), "a heal's ward or a blow's root, every 20 s"); Assert.IsTrue(amber.Amber(20));
            Assert.AreEqual(50, LegendState.AmberWard(1000), "a small ward: 5%");
            var sun = new LegendState(LegendEffect.Sunfire); Assert.IsTrue(sun.Sunfire(true)); Assert.IsFalse(sun.Sunfire(false), "only at full Heat"); Assert.AreEqual(10, LegendState.SunfireTick(20));
            var light = new LegendState(LegendEffect.KeptLight);
            Assert.AreEqual(40, light.KeptLight(100, 60, 1000), "the overheal kept"); Assert.AreEqual(100, light.KeptLight(400, 50, 1000), "capped at a tenth of the target's health"); Assert.AreEqual(0, light.KeptLight(100, 100, 1000));
            Assert.AreEqual(0, new LegendState(LegendEffect.Amber).KeptLight(100, 0, 1000), "only the Lantern's");
        }

        // ---------- the forge ----------
        [Test] public void The_recipe_at_the_forge_then_the_awake_key()
        {
            var items = Items(); var p = EncounterSession.FreshProgress("class.paladin"); Inventory.Ensure(p);
            var w = LegendaryWeapons.For("class.paladin"); Assert.AreEqual("legend.iron_anchor", w.id);
            Assert.IsFalse(LegendaryWeapons.Forge(p, items, out var why)); StringAssert.Contains("3 more Calcified Brood-Crystals", why); StringAssert.Contains("Uneaten Glass-Steel Hammer Head", why);
            ComponentBag.Add(p, items.Get(UnravellerLoot.Crystal), 3); ComponentBag.Add(p, items.Get(w.Blank), 1); ComponentBag.Add(p, items.Get(UnravellerLoot.Heart), 1);
            p.gold = LegendaryWeapons.ForgeCoin - 1;
            Assert.IsFalse(LegendaryWeapons.Forge(p, items, out why)); StringAssert.Contains("500 crowns", why); StringAssert.Contains("Slag-iron", why, "and the bar");
            p.gold = 600; Inventory.Add(p, items, LegendaryWeapons.Bar, 2);
            Assert.IsEmpty(LegendaryWeapons.Missing(p, items));
            Assert.IsTrue(LegendaryWeapons.Forge(p, items, out why), why);
            Assert.AreEqual(1, Inventory.Count(p, w.id), "the weapon, asleep, in the bags"); Assert.AreEqual(100, p.gold); Assert.AreEqual(1, Inventory.Count(p, LegendaryWeapons.Bar));
            Assert.AreEqual(0, ComponentBag.Count(p, UnravellerLoot.Crystal)); Assert.AreEqual(0, ComponentBag.Count(p, w.Blank)); Assert.AreEqual(0, ComponentBag.Count(p, UnravellerLoot.Heart));
            Assert.IsTrue(LegendaryWeapons.Forged(p, w.id)); Assert.IsTrue(LegendaryWeapons.ForgedFor(p)); Assert.IsFalse(LegendaryWeapons.IsAwake(p, w.id));
            Assert.IsNull(LegendaryWeapons.Missing(p, items), "made: nothing more wanted");
            Assert.IsFalse(LegendaryWeapons.Forge(p, items, out _), "one a character");
            StringAssert.StartsWith("Sleeps.", LegendaryWeapons.StageLine(p, w.id));
            // Stage 2: no Note, no waking; the test Note (until the Verses' bosses exist) wakes it with the key.
            Assert.IsFalse(LegendaryWeapons.Wake(p, items, out why)); StringAssert.Contains("note of the Original Song", why);
            LegendaryWeapons.GiveTestNote(p, items); Assert.AreEqual(1, ComponentBag.Count(p, LegendaryWeapons.Note));
            Assert.IsTrue(LegendaryWeapons.Wake(p, items, out why), why);
            CollectionAssert.Contains(p.keys, "legend.awake:legend.iron_anchor", "the awake key"); Assert.AreEqual(0, ComponentBag.Count(p, LegendaryWeapons.Note), "the Note spent");
            Assert.IsNull(LegendaryWeapons.AwakeInHand(p), "carried, not in hand");
            int bag = p.bag.FindIndex(x => x.item == w.id); p.experience = EncounterProgress.XpForLevel(20);
            Assert.IsTrue(Inventory.Equip(p, items, bag, 20, out why), why);
            Assert.AreEqual(w, LegendaryWeapons.AwakeInHand(p)); Assert.AreEqual(LegendEffect.Anchor, LegendaryWeapons.AwakeInHand(p).effect);
            Assert.AreEqual(0, LegendaryWeapons.AwakeBonus(p), "a legendary's damage waits for level 30 (the cap is 20)");
            Assert.AreEqual(LegendaryWeapons.EpicDamage, Inventory.Totals(p, items).weaponDamage);
            StringAssert.StartsWith("Awake: legendary.", LegendaryWeapons.StageLine(p, w.id));
            Assert.IsFalse(LegendaryWeapons.Wake(p, items, out _), "awake once");
            // Three Verses bind into a Note.
            var q = EncounterSession.FreshProgress("class.mage");
            foreach (var v in LegendaryWeapons.Verses) ComponentBag.Add(q, items.Get(v), 1);
            Assert.IsTrue(LegendaryWeapons.BindVerses(q, items)); Assert.AreEqual(1, ComponentBag.Count(q, LegendaryWeapons.Note)); Assert.IsFalse(LegendaryWeapons.Verses.Any(v => ComponentBag.Count(q, v) > 0));
            Assert.IsTrue(LegendaryWeapons.ForgeOpen(21)); Assert.IsTrue(LegendaryWeapons.ForgeOpen(3)); Assert.IsFalse(LegendaryWeapons.ForgeOpen(6)); Assert.IsFalse(LegendaryWeapons.ForgeOpen(14));
        }

        [Test] public void Tynara_and_her_forge_stand_by_the_Forge_District_Gate()
        {
            var z = LootTestData.Zones().Find(x => x.id == "zone.lowtowns");
            var gate = z.landmarks.First(l => l.name == "The Forge District Gate").at;
            var smith = z.life.residents.FirstOrDefault(r => r.name == LegendaryWeapons.Smith); Assert.NotNull(smith, "Tynara lives in the Lowtowns");
            var forge = z.stations.FirstOrDefault(s => s.name == "Tynara's night forge"); Assert.NotNull(forge); Assert.AreEqual("forge", forge.kind);
            Assert.Less(Vector2.Distance(smith.at, gate), 20, "by the gate"); Assert.Less(Vector2.Distance(forge.at, smith.at), 5, "at her forge");
            Assert.Less(Mathf.Abs(forge.at.x - 206), 10, "under the Forge District's wall (x 206)");
            foreach (var p in z.props) Assert.Greater(Vector2.Distance(p.at, forge.at), 2.5f, "nothing on her forge: " + p.kind + " " + p.name);
            StringAssert.Contains("book1 world_bible.md:31", smith.canonStatus); StringAssert.Contains("CANON-EXPANDED", smith.canonStatus);
            Assert.GreaterOrEqual(smith.stories.Length, 3);
        }
    }
}
