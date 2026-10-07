#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Named loot in the running game (Oakhaven; loot DESIGN.md 9, step L2): the loot files load with the items and a new
    /// character starts with the Tempered Trailblade in hand; Caddock gives his crown, cleaver or coat, never one you hold, until
    /// you hold all three, and still leaves gear after; Old Whitefoot's two pieces come one after the other; two pieces of the
    /// Deserter King's Due add 30 health and say so on the tooltip, and taking one off takes it away; a unique you hold stays on
    /// the body; the Broken-Oath Sabre heals on a kill; Ama Rusk sells the Market-Day Breeches. Each test saves to its own folder
    /// (SaveDirectoryOverride), never the real one.
    /// </summary>
    public class NamedLootTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-named-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = null;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            StringAssert.StartsWith(Path.GetTempPath(), s.SaveDirectoryOverride ?? "", "This test saves to its own folder.");
            Assert.NotNull(s.Items, "The game has its items."); Assert.NotNull(s.Loot, "The named loot is loaded.");
            return s;
        }
        const string Crown = "item.tin_crown", Cleaver = "loot.oak.due_cleaver", Coat = "loot.oak.due_coat", Sabre = "loot.oak.broken_oath_sabre";
        static readonly string[] CaddockList = { Crown, Cleaver, Coat };
        /// <summary>The camp mob of this name (the elite when it is one), every other enemy stood down.</summary>
        static EncounterEnemy Camp(EncounterSession s, string mob)
        {
            var e = s.Enemies.FirstOrDefault(x => x.Camp && x.actor.IsAlive && LootContext.From(x.persistentId, s.Zone.Zone.camps, 1, x.Elite).mob == mob && (x.Elite || !s.Enemies.Any(y => y.Camp && y.Elite && LootContext.From(y.persistentId, s.Zone.Zone.camps, 1, true).mob == mob)));
            Assert.NotNull(e, mob + " is in Oakhaven.");
            foreach (var x in s.Enemies) if (x != e) x.enabled = false;
            e.RespawnSeconds = 600;
            return e;
        }
        /// <summary>Character level (quests and gear read it from the experience); at the cap no kill levels you up.</summary>
        static void AtLevel(EncounterSession s, int level) { s.Progress.experience = EncounterProgress.XpForLevel(level); s.Player.SetLevel(level); }
        /// <summary>Everything on the body into the bags, as E and Take all [E] would.</summary>
        static void TakeAll(EncounterSession s, EncounterEnemy body) { s.OpenLoot(body); if (s.LootOpen) s.TakeAllLoot(); }
        static void Equip(EncounterSession s, string id)
        {
            if (!Inventory.Has(s.Progress, id)) Assert.AreEqual(0, Inventory.Add(s.Progress, s.Items, id, 1));
            Assert.IsTrue(s.EquipFromBag(s.Progress.bag.FindIndex(b => b.item == id)), id + " equipped.");
        }

        [UnityTest] public IEnumerator Named_loot_is_live_and_a_new_character_holds_the_Trailblade()
        {
            var s = Session(); yield return null;
            Assert.AreEqual(122, s.Loot.Gear.Count, "104 named items, the five legendaries, the twelve older ones and the Sentinel's Helm (2026-10-07).");
            Assert.NotNull(s.Items.Get("loot.oak.whitefoot_mantle"), "The loot files are item files.");
            Assert.AreEqual(s.content.itemId, s.Progress.equipment[(int)EquipSlot.MainHand].item, "A new character starts with the Tempered Trailblade in hand.");
            Assert.AreEqual(0, Inventory.Count(s.Progress, s.content.itemId), "Worn, not in the bags.");
            Assert.AreEqual(s.Items.Get(s.content.itemId).weaponDamage, Inventory.Totals(s.Progress, s.Items).weaponDamage, "Its damage counts.");
            StringAssert.EndsWith("Dropped in Oakhaven", s.Loot.TooltipLines("loot.oak.harrow_luck_knot", s.Progress), "Sources name the zone by its display name.");
        }

        [UnityTest] public IEnumerator Caddock_gives_crown_cleaver_or_coat_never_one_you_hold_until_all_three_are_yours()
        {
            var s = Session(); var caddock = Camp(s, "Caddock, the Bandit King"); Assert.IsTrue(caddock.Elite);
            AtLevel(s, EncounterProgress.LevelCap);
            caddock.actor.Health.ApplyDamage(1000000);
            yield return null;
            Assert.IsFalse(caddock.actor.IsAlive);
            for (int kill = 0; kill < 3; kill++)
            {
                if (kill > 0) s.RollCorpse(caddock);   // the next kill's body
                var got = caddock.Drops.Select(d => d.item).Where(CaddockList.Contains).ToList();
                Assert.AreEqual(1, got.Count, "Kill " + (kill + 1) + " gives one piece of his list: " + string.Join(", ", caddock.Drops));
                Assert.IsFalse(Inventory.Has(s.Progress, got[0]), "Kill " + (kill + 1) + " gives one you do not hold: " + got[0] + ".");
                Assert.AreEqual(0, caddock.Drops.Count(d => d.item == Crown) - (got[0] == Crown ? 1 : 0), "The old 100% crown does not come as well.");
                TakeAll(s, caddock);
                Assert.IsTrue(Inventory.Has(s.Progress, got[0]), got[0] + " is in the bags.");
            }
            Assert.IsTrue(CaddockList.All(id => Inventory.Has(s.Progress, id)), "Three kills, all three pieces.");
            for (int kill = 0; kill < 20; kill++)
            {
                s.RollCorpse(caddock);
                Assert.IsFalse(caddock.Drops.Any(d => CaddockList.Contains(d.item)), "Nothing of his you hold comes again: " + string.Join(", ", caddock.Drops));
                Assert.IsTrue(caddock.Drops.Any(d => s.Items.Get(d.item).kind == "gear"), "He still leaves gear: " + string.Join(", ", caddock.Drops));
            }
        }

        [UnityTest] public IEnumerator Old_Whitefoot_gives_his_mantle_and_his_fang_one_after_the_other()
        {
            var s = Session(); var wolf = Camp(s, "Old Whitefoot"); Assert.IsTrue(wolf.Elite);
            AtLevel(s, EncounterProgress.LevelCap);
            var pair = new[] { "loot.oak.whitefoot_mantle", "loot.oak.whitefoot_fang" };
            wolf.actor.Health.ApplyDamage(1000000);
            yield return null;
            for (int piece = 0; piece < 2; piece++)
            {
                // His list pays half the time: kill him again until it does (60 dry kills in a row is a chance in 10^18).
                for (int kill = 0; kill < 60 && !wolf.Drops.Any(d => pair.Contains(d.item)); kill++) s.RollCorpse(wolf);
                var got = wolf.Drops.Select(d => d.item).Where(pair.Contains).ToList();
                Assert.AreEqual(1, got.Count, "One of his pieces."); Assert.IsFalse(Inventory.Has(s.Progress, got[0]), "Not the one you hold.");
                TakeAll(s, wolf); Assert.IsTrue(Inventory.Has(s.Progress, got[0]));
                s.RollCorpse(wolf);
            }
            Assert.IsTrue(pair.All(id => Inventory.Has(s.Progress, id)), "Both of Old Whitefoot's pieces held.");
        }

        /// <summary>The health that worn gear effects and set bonuses add as flat modifiers.</summary>
        static float SetHealth(EncounterSession s) { return s.GearFx.modifiers.Where(m => m.Stat == Crulanda.Core.StatType.MaxHealth && m.Op == ModifierOp.Flat).Sum(m => m.Value); }

        [UnityTest] public IEnumerator Two_pieces_of_the_Deserter_Kings_Due_add_30_health_and_the_tooltip_says_so()
        {
            var s = Session(); AtLevel(s, 6); yield return null;
            Equip(s, Cleaver);
            Assert.AreEqual(s.content.itemId, s.Progress.bag.First(b => !b.Empty && b.item == s.content.itemId).item, "The Trailblade went back to the bags.");
            int one = s.Player.Health.Pool.Max;
            Assert.IsFalse(s.GearFx.lines.Contains("+30 health."), "One piece: no bonus."); Assert.AreEqual(0f, SetHealth(s), "No health modifier.");
            Equip(s, Coat);
            int two = s.Player.Health.Pool.Max, fromStamina = Mathf.RoundToInt(s.Items.Get(Coat).stamina * s.ClassDef.stats.healthPerStamina);
            Assert.AreEqual(one + fromStamina + 30, two, 1, "Two pieces: the coat's Stamina and +30 health.");
            Assert.Contains("+30 health.", s.GearFx.lines); Assert.AreEqual(30f, SetHealth(s), "One +30 health modifier.");
            var tip = s.Loot.TooltipLines(Coat, s.Progress);
            StringAssert.Contains("The Deserter King's Due (2/4)", tip); StringAssert.Contains("  (2) +30 health", tip); StringAssert.Contains("  [3] +6 attack power", tip);
            StringAssert.Contains("Unique", tip); StringAssert.Contains("Dropped by Caddock, the Bandit King", tip);
            Assert.IsTrue(s.UnequipSlot((int)EquipSlot.Chest), "The coat comes off.");
            Assert.AreEqual(one, s.Player.Health.Pool.Max, 1, "And the bonus with it.");
            Assert.IsFalse(s.GearFx.lines.Contains("+30 health.")); Assert.AreEqual(0f, SetHealth(s), "The modifier went too.");
        }

        [UnityTest] public IEnumerator A_unique_you_hold_stays_on_the_body()
        {
            var s = Session(); AtLevel(s, EncounterProgress.LevelCap);
            Assert.AreEqual(0, Inventory.Add(s.Progress, s.Items, Cleaver, 1));
            var mob = Camp(s, "Grey wolf"); mob.actor.Health.ApplyDamage(1000000);
            yield return null;
            s.PutLoot(mob, 3, new[] { new LootDrop(Cleaver, 1), new LootDrop("junk.wolf_fang", 1) });
            TakeAll(s, mob);
            Assert.AreEqual(1, Inventory.Count(s.Progress, Cleaver), "Still only the one.");
            Assert.AreEqual(1, Inventory.Count(s.Progress, "junk.wolf_fang"), "The rest was taken.");
            Assert.IsTrue(mob.Drops.Any(d => d.item == Cleaver), "The second cleaver stays on the body.");
            Assert.Contains(EncounterSession.UniqueLine(s.Items.Get(Cleaver).name), s.Messages);
            Assert.IsFalse(s.Messages.Contains(EncounterSession.BodyKeepsLine), "It is not about room in the bags.");
            Assert.IsTrue(s.HoldsUnique(Cleaver)); Assert.IsFalse(s.HoldsUnique("junk.wolf_fang"));
        }

        [UnityTest] public IEnumerator The_Broken_Oath_Sabre_heals_on_a_kill()
        {
            var s = Session(); AtLevel(s, EncounterProgress.LevelCap); yield return null;
            Equip(s, Sabre);
            Assert.AreEqual(12, s.GearFx.onKillHeal);
            s.Player.Health.ApplyDamage(80);
            int hurt = s.Player.Health.Pool.Current;
            var mob = Camp(s, "Grey wolf"); mob.actor.Health.ApplyDamage(1000000);
            Assert.AreEqual(hurt + 12, s.Player.Health.Pool.Current, "Each kill restores 12 health.");
            Assert.IsTrue(s.UnequipSlot((int)EquipSlot.MainHand));
            Assert.AreEqual(0, s.GearFx.onKillHeal, "Off with the sabre.");
        }

        [UnityTest] public IEnumerator Ama_Rusk_sells_the_Market_Day_Breeches()
        {
            var s = Session(); yield return null;
            var ama = VillageLife.Active.Find("Ama Rusk"); Assert.NotNull(ama, "Ama Rusk lives in Oakhaven.");
            Assert.IsTrue(s.IsVendor(ama));
            s.OpenVendor(ama);
            CollectionAssert.Contains(s.VendorStock, "loot.oak.market_day_breeches");
            s.Progress.gold = 500; s.Buy("loot.oak.market_day_breeches");
            Assert.AreEqual(1, Inventory.Count(s.Progress, "loot.oak.market_day_breeches"), "Bought.");
            s.CloseVendor();
        }
    }
}
#endif
