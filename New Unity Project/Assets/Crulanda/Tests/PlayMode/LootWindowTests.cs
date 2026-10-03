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
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Loot that feels like loot, in the running game (Oakhaven; loot DESIGN.md 4, step L1): a camp mob's loot is rolled as it
    /// dies and lights a beacon on its root in the colour of the best thing on it; a body of coins and junk empties with one press
    /// of E; a body holding gear opens the loot window, and E takes everything; with full bags the piece stays on the body (its
    /// beacon still lit) after the window shuts; walking off shuts the window; a respawn clears the body and puts the beacon out.
    /// Each test saves to its own folder (SaveDirectoryOverride), never the real one.
    /// </summary>
    public class LootWindowTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-loot-" + Guid.NewGuid().ToString("N"));
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
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static readonly string Blade = ItemDatabase.GearId("mainhand", 3, 3, 4242), Helm = ItemDatabase.GearId("head", 3, 2, 88);
        const string Fang = "junk.wolf_fang";
        /// <summary>What E says at this body: a beast with a hide or pelt is skinned, anything else is searched.</summary>
        static string BodyPrompt(EncounterEnemy e) { return e.Skinnable ? GameAnimals.SkinPrompt : "Search the body"; }

        /// <summary>
        /// A camp mob killed outright (combat has its own tests), with every other enemy stood down so nothing joins in, and the
        /// player stood beside the body with nobody near enough to talk to. Its respawn waits <paramref name="respawn"/> seconds.
        /// </summary>
        static IEnumerator KillOne(EncounterSession s, Action<EncounterEnemy> got, float respawn = 600)
        {
            // Out in the open: not down a cave, and well clear of the roads out (E would take the road before the body).
            var mob = s.Enemies.FirstOrDefault(e => e.Camp && !e.Elite && !e.Hidden && e.actor.IsAlive && !Hollow.InsideAny(e.transform.position, 0)
                && s.Zone.Zone.exits.All(x => Vector2.Distance(x.at, new Vector2(e.transform.position.x, e.transform.position.z)) > x.radius + 6));
            Assert.NotNull(mob, "Oakhaven has a camp mob out in the open to kill.");
            foreach (var e in s.Enemies) if (e != mob) e.enabled = false;
            mob.RespawnSeconds = respawn;
            mob.actor.Health.ApplyDamage(100000);
            yield return null;
            Assert.IsFalse(mob.actor.IsAlive);
            s.Player.GetComponent<AdventurerMotor>().Teleport(mob.transform.position + new Vector3(0, .1f, -1.6f));
            if (VillageLife.Active != null)
                foreach (var v in VillageLife.Active.Villagers)
                    if (Vector3.Distance(v.transform.position, mob.transform.position) < 8) v.StandAt(mob.transform.position + Vector3.back * 30 + Vector3.right * 2 * VillageLife.Active.Villagers.IndexOf(v), 0);
            s.SelectFriendly(null, false);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsTrue(s.CanLoot(mob)); Assert.AreEqual(BodyPrompt(mob), s.InteractPrompt);
            got(mob);
        }

        /// <summary>Waits (a few seconds at most) for a "RARE" toast to come up: one already showing when it was raised goes first.</summary>
        static IEnumerator RareToast(EncounterSession s)
        {
            for (float t = 0; t < EncounterSession.ToastSeconds * 2 + 1 && s.ToastKicker != "RARE"; t += Time.deltaTime) yield return null;
        }

        [UnityTest] public IEnumerator A_dead_camp_mob_carries_its_drops_and_a_beacon()
        {
            var s = Session(); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            int gold = s.Progress.gold;
            EncounterEnemy mob = null; yield return KillOne(s, m => mob = m);
            Assert.NotNull(mob.Drops, "The loot was rolled as it died.");
            Assert.Greater(mob.Coins, 0, "A camp body always holds some coins.");
            Assert.IsTrue(mob.Drops.All(d => s.Items.Get(d.item) != null && d.count > 0), "Everything on it is a real item.");
            var beacon = LootBeacon.Of(mob);
            Assert.NotNull(beacon, "Its body shows a beacon.");
            Assert.AreEqual(s.BestQuality(mob.Drops), beacon.Quality, "In the colour of the best thing on it.");
            Assert.AreSame(mob.transform, beacon.transform.parent, "On the enemy's root, not the Body that tips over.");
            Assert.AreEqual(mob.transform.position.y - 1, beacon.transform.position.y, .01f, "Standing on the ground.");
            Assert.IsTrue(beacon.GetComponentsInChildren<Collider>(true).Length == 0, "Clicks and sight lines go through it.");
            Assert.IsTrue(beacon.GetComponentsInChildren<Light>(true).Length == 0, "No lights.");
            Assert.AreEqual(gold, s.Progress.gold, "Nothing is in your purse until you take it.");
        }

        [UnityTest] public IEnumerator Beacon_colour_is_the_best_quality_on_the_body()
        {
            var s = Session(); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            EncounterEnemy mob = null; yield return KillOne(s, m => mob = m);
            s.Progress.experience = EncounterProgress.XpForLevel(3); s.Player.SetLevel(3);
            s.PutLoot(mob, 4, new[] { new LootDrop(Fang, 1) });
            Assert.AreEqual(0, LootBeacon.Showing(mob), "Junk: the white twinkle.");
            Assert.IsNull(LootBeacon.Of(mob).transform.Find("Beam"), "A twinkle has no beam.");
            s.PutLoot(mob, 4, new[] { new LootDrop(Fang, 1), new LootDrop(Helm, 1), new LootDrop(Blade, 1) });
            Assert.AreEqual(3, LootBeacon.Showing(mob), "A rare blade among junk and an uncommon helm: the blue beam.");
            var beam = LootBeacon.Of(mob).transform.Find("Beam");
            Assert.NotNull(beam); Assert.AreEqual(4, beam.localScale.y, .01f, "Four metres high.");
            Assert.AreEqual(LootBeacon.Colour(3).b, 1, .01f, "Blue.");
            // Taking the blade leaves the helm: the beacon falls to green.
            s.OpenLoot(mob); Assert.IsTrue(s.LootOpen, "Gear opens the window.");
            int blade = s.LootItems.ToList().FindIndex(d => d.item == Blade);
            Assert.IsTrue(s.TakeLoot(blade)); Assert.AreEqual(1, Inventory.Count(s.Progress, Blade));
            Assert.AreEqual(2, LootBeacon.Showing(mob), "The uncommon helm is the best left: green.");
            Assert.AreEqual(1.2f, LootBeacon.Of(mob).transform.Find("Beam").localScale.y, .01f, "A short column.");
            string line = "Looted: " + s.Items.Get(Blade).name + ".";
            Assert.Contains(line, s.Messages, "A chat line names it");
            Assert.AreEqual(LootBeacon.Colour(3), s.LineColour(line, Color.white), "in the rare colour.");
            yield return RareToast(s);
            Assert.AreEqual(s.Items.Get(Blade).name, s.ToastName, "The rare blade raised a RARE toast over its name.");
            // An epic: the purple beam, seven metres, with its ring.
            s.PutLoot(mob, 0, new[] { new LootDrop(ItemDatabase.GearId("offhand", 3, 4, 7), 1) });
            Assert.AreEqual(4, LootBeacon.Showing(mob));
            Assert.AreEqual(7, LootBeacon.Of(mob).transform.Find("Beam").localScale.y, .01f);
            Assert.NotNull(LootBeacon.Of(mob).transform.Find("Ring"), "An epic has a ring on the ground.");
        }

        [UnityTest] public IEnumerator Junk_only_bodies_loot_with_one_press()
        {
            var s = Session(); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            EncounterEnemy mob = null; yield return KillOne(s, m => mob = m);
            s.PutLoot(mob, 5, new[] { new LootDrop(Fang, 2) });
            int gold = s.Progress.gold;
            s.Interact();   // E
            Assert.IsFalse(s.LootOpen, "No window for coins and junk.");
            Assert.AreEqual(gold + 5, s.Progress.gold);
            Assert.AreEqual(2, Inventory.Count(s.Progress, Fang));
            Assert.IsFalse(s.CanLoot(mob), "The body is empty.");
            Assert.AreEqual(-1, LootBeacon.Showing(mob), "Its beacon is out.");
            Assert.Contains("Looted 5 crowns.", s.Messages);
            Assert.Contains("Looted: " + s.Items.Get(Fang).name + " x2.", s.Messages);
        }

        [UnityTest] public IEnumerator Gear_opens_the_window_and_E_takes_all()
        {
            var s = Session(); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            EncounterEnemy mob = null; yield return KillOne(s, m => mob = m);
            s.PutLoot(mob, 7, new[] { new LootDrop(Blade, 1), new LootDrop(Fang, 1) });
            int gold = s.Progress.gold;
            s.Interact();   // E: the window
            Assert.IsTrue(s.LootOpen); Assert.AreSame(mob, s.LootBody);
            Assert.AreEqual(2, s.LootItems.Count); Assert.AreEqual(7, s.LootCoins);
            Assert.AreEqual("Take all", s.InteractPrompt, "E now takes everything.");
            Assert.AreEqual(gold, s.Progress.gold, "Nothing is taken by opening it.");
            yield return null;
            s.Interact();   // E again: Take all
            Assert.IsFalse(s.LootOpen);
            Assert.AreEqual(gold + 7, s.Progress.gold);
            Assert.AreEqual(1, Inventory.Count(s.Progress, Blade)); Assert.AreEqual(1, Inventory.Count(s.Progress, Fang));
            Assert.IsFalse(s.CanLoot(mob)); Assert.AreEqual(-1, LootBeacon.Showing(mob));
            yield return RareToast(s);
            Assert.AreEqual("RARE", s.ToastKicker, "The rare blade is called out.");
            // Esc shuts the window too: a fresh body, opened and shut.
            s.PutLoot(mob, 1, new[] { new LootDrop(Helm, 1) });
            s.Interact(); Assert.IsTrue(s.LootOpen);
            s.CloseLoot(); Assert.IsFalse(s.LootOpen); Assert.IsTrue(s.CanLoot(mob), "Shutting the window takes nothing.");
        }

        [UnityTest] public IEnumerator Full_bags_leave_the_item_on_the_body()
        {
            var s = Session(); var p = s.Progress; Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            EncounterEnemy mob = null; yield return KillOne(s, m => mob = m);
            for (int i = 0; i < Inventory.BagSize; i++) if (p.bag[i].Empty) { p.bag[i].item = ItemDatabase.GearId("feet", 1, 1, 500 + i); p.bag[i].count = 1; }
            Assert.AreEqual(0, Inventory.FreeSlots(p), "The bags are full.");
            Assert.AreEqual(0, Inventory.Room(p, s.Items, Blade));
            s.PutLoot(mob, 3, new[] { new LootDrop(Blade, 1) });
            int gold = p.gold;
            s.Interact(); Assert.IsTrue(s.LootOpen);
            s.Interact();   // Take all
            Assert.IsFalse(s.LootOpen, "The window shuts.");
            Assert.AreEqual(gold + 3, p.gold, "The coins need no room.");
            Assert.AreEqual(0, Inventory.Count(p, Blade));
            Assert.Contains(EncounterSession.BodyKeepsLine, s.Messages);
            Assert.AreEqual(1, mob.Drops.Count, "The blade is still on the body"); Assert.AreEqual(Blade, mob.Drops[0].item);
            Assert.AreEqual(0, mob.Coins);
            Assert.IsTrue(s.CanLoot(mob), "and the body can still be searched,");
            Assert.AreEqual(3, LootBeacon.Showing(mob), "its beacon lit for the blade.");
            Assert.AreNotEqual(BodyPrompt(mob), s.InteractPrompt, "A body with nothing that fits does not take E (Mira, nodes and doors keep it).");
            // Room made: E opens it again and takes the blade.
            p.bag[0].item = ""; p.bag[0].count = 0;
            yield return null;
            Assert.AreEqual(BodyPrompt(mob), s.InteractPrompt, "With room, the body takes E again.");
            s.Interact(); Assert.IsTrue(s.LootOpen);
            s.Interact();
            Assert.AreEqual(1, Inventory.Count(p, Blade)); Assert.IsFalse(s.CanLoot(mob)); Assert.AreEqual(-1, LootBeacon.Showing(mob));
        }

        [UnityTest] public IEnumerator The_window_closes_when_you_walk_away()
        {
            var s = Session(); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            EncounterEnemy mob = null; yield return KillOne(s, m => mob = m);
            s.PutLoot(mob, 2, new[] { new LootDrop(Helm, 1) });
            s.Interact(); Assert.IsTrue(s.LootOpen);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsTrue(s.LootOpen, "Standing beside it, it stays open.");
            s.Player.GetComponent<AdventurerMotor>().Teleport(mob.transform.position + new Vector3(0, .1f, -(EncounterSession.LootReach + 3)));
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsFalse(s.LootOpen, "Walked off: it shuts.");
            Assert.IsTrue(s.CanLoot(mob), "And the helm is still on the body.");
            Assert.AreEqual(2, LootBeacon.Showing(mob));
        }

        [UnityTest] public IEnumerator The_window_shuts_the_windows_under_it_and_gives_way_to_them()
        {
            var s = Session(); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            EncounterEnemy mob = null; yield return KillOne(s, m => mob = m);
            s.PutLoot(mob, 2, new[] { new LootDrop(Helm, 1) });
            // Windows drawn before the loot window would take its clicks: opening it shuts them.
            s.Conversation = new EncounterSession.QuestConversation { npc = "Mira", where = s.Player.transform.position }; s.QuestBookOpen = true;
            s.OpenLoot(mob); Assert.IsTrue(s.LootOpen);
            Assert.IsNull(s.Conversation, "The conversation shut."); Assert.IsFalse(s.QuestBookOpen, "The quest book shut.");
            yield return null;
            Assert.IsTrue(s.LootOpen, "Nothing else open: it stays.");
            // One of them opened while it is up: the loot window gives way, and the helm stays on the body.
            s.QuestBookOpen = true;
            for (int i = 0; i < 2; i++) yield return null;
            Assert.IsFalse(s.LootOpen, "The quest book opened over it: it shut.");
            Assert.IsTrue(s.CanLoot(mob)); Assert.AreEqual(2, LootBeacon.Showing(mob));
        }

        [UnityTest] public IEnumerator An_elite_body_beacon_stands_on_the_ground()
        {
            var s = Session(); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            EncounterEnemy mob = null; yield return KillOne(s, m => mob = m);
            // An elite's root rides 1.18 m above its feet (its agent's base offset of 1, times its scale).
            mob.transform.localScale = Vector3.one * 1.18f;
            LootBeacon.Clear(mob); LootBeacon.Show(mob, 4);
            var beacon = LootBeacon.Of(mob); Assert.NotNull(beacon);
            Assert.AreEqual(mob.transform.position.y - 1.18f, beacon.transform.position.y, .02f, "The beacon's foot is on the ground, not in the air.");
            Assert.AreEqual(1, beacon.transform.lossyScale.y, .01f, "At world scale.");
            yield return null;
        }

        [UnityTest] public IEnumerator Respawn_clears_drops_and_beacon()
        {
            var s = Session(); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            EncounterEnemy mob = null; yield return KillOne(s, m => mob = m, 4);
            s.PutLoot(mob, 2, new[] { new LootDrop(Blade, 1) });
            s.Interact(); Assert.IsTrue(s.LootOpen);
            Assert.AreEqual(3, LootBeacon.Showing(mob));
            Assert.IsFalse(mob.actor.IsAlive, "Still down while the window is open.");
            yield return new WaitForSeconds(4.5f);
            Assert.IsTrue(mob.actor.IsAlive, "It is back on its feet.");
            Assert.IsNull(mob.Drops, "Its drops are gone"); Assert.AreEqual(0, mob.Coins);
            Assert.AreEqual(-1, LootBeacon.Showing(mob), "and its beacon is out,");
            Assert.IsFalse(s.LootOpen, "and the window shut with it.");
            Assert.AreEqual(0, Inventory.Count(s.Progress, Blade));
        }
    }
}
#endif
