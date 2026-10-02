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
    /// Hunting for hides in the running game (Oakhaven; BUILD_PLAN step 11, ADDENDUM G): a hill deer can be targeted, killed and
    /// skinned for its hide, with no experience and no coin; game watches and bolts, and a sneaking player gets closer; a fallen
    /// animal comes back; hens, sheep and cats are never game; and game is never an enemy to the village or to "zone clear".
    /// Each test saves to its own folder (SaveDirectoryOverride), never the real one.
    /// </summary>
    public class HuntTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11; GameAnimal.SneakingOverride = null;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-hunt-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = null;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null; GameAnimal.SneakingOverride = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static GameAnimal Animal(EncounterEnemy e) { return e.GetComponent<GameAnimal>(); }
        static float Flat(Vector3 a, Vector3 b) { return new Vector2(a.x - b.x, a.z - b.z).magnitude; }

        /// <summary>
        /// A living game animal of this kind out in the open: not down a cave, well clear of the roads out, with no enemy within
        /// <paramref name="clear"/> metres. Every enemy is stood down so nothing joins in.
        /// </summary>
        static EncounterEnemy OpenAnimal(EncounterSession s, string kind, float clear = 35)
        {
            var e = s.Game.FirstOrDefault(g => g.actor.IsAlive && Animal(g).Kind == kind && !Hollow.InsideAny(g.transform.position, 0)
                && s.Zone.Zone.exits.All(x => Vector2.Distance(x.at, new Vector2(g.transform.position.x, g.transform.position.z)) > x.radius + 8)
                && s.Enemies.All(en => Flat(en.transform.position, g.transform.position) > clear));
            Assert.NotNull(e, "Oakhaven has a " + kind + " out in the open, away from the camps.");
            foreach (var en in s.Enemies) en.enabled = false;
            return e;
        }
        /// <summary>The player stood on the ground <paramref name="distance"/> m from the animal along <paramref name="dir"/>.</summary>
        static void Put(EncounterSession s, EncounterEnemy e, Vector3 dir, float distance)
        {
            var at = e.transform.position + dir * distance;
            s.Player.GetComponent<AdventurerMotor>().Teleport(s.Zone.Ground(new Vector2(at.x, at.z), 1.1f));
        }
        /// <summary>The player walks in on the animal from <paramref name="from"/> to <paramref name="to"/> metres at <paramref name="pace"/> m/s.</summary>
        static IEnumerator Approach(EncounterSession s, EncounterEnemy e, Vector3 dir, float from, float to, float pace)
        {
            for (float d = from; d > to; d -= pace * Mathf.Max(Time.deltaTime, .001f)) { Put(s, e, dir, d); yield return null; }
        }

        [UnityTest] public IEnumerator A_deer_can_be_targeted_killed_and_skinned()
        {
            var s = Session(); var p = s.Progress; Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            Assert.IsNotEmpty(s.Game, "Oakhaven's deer and rabbits are game.");
            var deer = OpenAnimal(s, "deer");
            Assert.IsTrue(deer.Game && deer.Camp, "A game animal, back like a camp mob."); CollectionAssert.DoesNotContain(s.Enemies, deer, "Never one of the enemies.");
            Assert.AreEqual("Hill deer", deer.actor.DisplayName); Assert.AreEqual(GameAnimals.Health("deer"), deer.actor.Health.Pool.Max); Assert.AreEqual(70, deer.actor.Health.Pool.Max);
            // Clickable: a collider about its body (a click's ray finds it), and Tab finds it when no enemy is near.
            var box = deer.GetComponent<Collider>(); Assert.NotNull(box); Assert.IsFalse(box.isTrigger);
            var side = deer.transform.right;
            Assert.IsTrue(box.Raycast(new Ray(deer.transform.position - Vector3.up * .1f + side * 3, -side), out _, 5), "A ray at its flank hits it.");
            GameAnimal.SneakingOverride = true; var dir = -deer.transform.forward;
            Put(s, deer, dir, 5); yield return null;
            s.CycleTarget();
            Assert.NotNull(s.Target, "Tab picks something."); Assert.IsTrue(s.Target.Game, "With no enemy near, Tab picks the game.");
            s.Select(deer); s.BeginAutoAttack();
            Assert.IsFalse(s.InCombat, "Swinging at a deer is not a fight: you can still eat and save.");
            // Hit, it bolts; killed, it lies on its side and holds its hide, and nothing else.
            int xp = p.experience, gold = p.gold, records = p.enemies.Count;
            deer.Receive(10, s.Player); yield return null;
            Assert.IsTrue(Animal(deer).Bolting, "Hit, it bolts."); Assert.Less(deer.actor.Health.Pool.Current, 70);
            deer.actor.Health.ApplyDamage(100000); yield return null;
            Assert.IsFalse(deer.actor.IsAlive);
            Assert.AreEqual(xp, p.experience, "No experience from game."); Assert.AreEqual(records, p.enemies.Count, "Nothing about it is saved.");
            Assert.IsFalse(s.AutoAttack, "The swing stops with it.");
            Assert.IsTrue(s.Messages.Any(m => m.StartsWith("Hill deer killed")), "The kill is told.");
            Assert.IsTrue(s.Messages.Where(m => m.StartsWith("Hill deer")).All(m => !m.Contains("XP") && m.Contains("no experience")), "And no experience is.");
            Assert.AreEqual(0, deer.Coins, "No coin on a deer.");
            Assert.AreEqual(1, deer.Drops.Count); Assert.AreEqual("hide.hill_deer", deer.Drops[0].item); Assert.AreEqual(1, deer.Drops[0].count);
            Assert.AreEqual(90, Animal(deer).Body.Root.localEulerAngles.z, 1, "On its side.");
            Assert.Less(Animal(deer).Body.Root.position.y, deer.transform.position.y, "On the ground, not where an actor's body would tip.");
            Assert.AreEqual(1, LootBeacon.Showing(deer), "A white twinkle on the body.");
            // Skinned: E beside the body ("Skin the body") puts the hide in the bags with the one press.
            Put(s, deer, dir, 1.6f); s.SelectFriendly(null, false);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(GameAnimals.SkinPrompt, s.InteractPrompt); Assert.AreEqual("Skin the body", s.InteractPrompt);
            s.Interact();
            Assert.IsFalse(s.LootOpen, "A hide alone is taken at once.");
            Assert.AreEqual(1, Inventory.Count(p, "hide.hill_deer")); Assert.AreEqual(gold, p.gold, "No coin.");
            Assert.Contains("Looted: Hill-deer hide.", s.Messages);
            Assert.IsFalse(s.CanLoot(deer), "Skinned."); Assert.AreEqual(-1, LootBeacon.Showing(deer));
            Assert.AreNotEqual(GameAnimals.SkinPrompt, s.InteractPrompt);
            // The hide is Maud's material: "Sell junk" keeps it, and a right-click names who works it.
            Assert.IsFalse(s.EquipFromBag(p.bag.FindIndex(x => x.item == "hide.hill_deer"))); Assert.AreEqual(EncounterSession.HideLine, s.Messages.Last());
        }

        [UnityTest] public IEnumerator A_wolf_body_is_skinned_and_a_deserter_searched()
        {
            var s = Session(); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            Assert.IsTrue(s.Enemies.Any(e => e.Camp && !e.Skinnable), "Oakhaven's deserters have no hide: their bodies are searched.");
            Assert.IsTrue(s.Game.All(g => g.Skinnable), "Every game animal is skinned.");
            // A camp wolf out in the open, killed, with its pelt on the body and the player beside it.
            var wolf = s.Enemies.FirstOrDefault(e => e.Camp && e.Skinnable && !e.Elite && !e.Hidden && e.actor.IsAlive && e.persistentId.Contains("wolf") && !Hollow.InsideAny(e.transform.position, 0)
                && s.Zone.Zone.exits.All(x => Vector2.Distance(x.at, new Vector2(e.transform.position.x, e.transform.position.z)) > x.radius + 6));
            Assert.NotNull(wolf, "Oakhaven has a camp wolf out in the open.");
            foreach (var e in s.Enemies) e.enabled = false;
            wolf.RespawnSeconds = 600; wolf.actor.Health.ApplyDamage(100000); yield return null;
            Assert.IsFalse(wolf.actor.IsAlive);
            s.PutLoot(wolf, 0, new[] { new LootDrop("junk.wolf_pelt", 1) });
            s.Player.GetComponent<AdventurerMotor>().Teleport(wolf.transform.position + new Vector3(0, .1f, -1.6f));
            if (VillageLife.Active != null)
                foreach (var v in VillageLife.Active.Villagers)
                    if (Vector3.Distance(v.transform.position, wolf.transform.position) < 8) v.StandAt(wolf.transform.position + Vector3.back * 30 + Vector3.right * 2 * VillageLife.Active.Villagers.IndexOf(v), 0);
            s.SelectFriendly(null, false);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsTrue(s.CanLoot(wolf)); Assert.AreEqual(GameAnimals.SkinPrompt, s.InteractPrompt, "A wolf's body is skinned for its pelt.");
        }

        [UnityTest] public IEnumerator Game_bolts_and_sneaking_gets_closer()
        {
            var s = Session(); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            var deer = OpenAnimal(s, "deer"); var a = Animal(deer); var dir = -deer.transform.forward;
            Assert.AreEqual(16, a.NoticeDistance(false), .01f, "A deer watches a walking player from 16 m"); Assert.AreEqual(8, a.NoticeDistance(true), .01f, "and a sneaking one from 8.");
            // Sneaking (Ctrl): from 12 m in to 9 m it never looks up.
            GameAnimal.SneakingOverride = true;
            yield return Approach(s, deer, dir, 12, 9, 2.6f);
            Assert.IsFalse(a.Watching || a.Bolting, "Outside 8 m a sneaking player goes unnoticed."); Assert.AreEqual(0, a.Alertness, .01f);
            // Inside 8 m it looks up and watches, but a short stalk gets you to striking distance before it goes.
            yield return Approach(s, deer, dir, 7.5f, 3, 2.6f);
            Assert.IsTrue(a.Watching, "It has seen you and watches."); Assert.IsFalse(a.Bolting, "Sneaking, you are within 3 m and it has not bolted.");
            Assert.Less(Flat(s.Player.transform.position, deer.transform.position), 3.6f);
            // Stand up and walk on: it bolts at once.
            GameAnimal.SneakingOverride = false;
            var before = deer.transform.position;
            yield return Approach(s, deer, dir, 3, 2.5f, 5.2f);
            for (int i = 0; i < 3 && !a.Bolting; i++) yield return null;
            Assert.IsTrue(a.Bolting, "Walking in close, it bolts.");
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(Flat(deer.transform.position, before), 4, "and is away across the ground.");
            Assert.IsTrue(deer.actor.IsAlive, "Frightened, not hurt.");

            // A walking player is seen from twice as far, and a moment is enough.
            var rabbit = OpenAnimal(s, "rabbit"); var r = Animal(rabbit); var rdir = -rabbit.transform.forward;
            Assert.AreEqual(7, r.NoticeDistance(false), .01f); Assert.AreEqual(3.5f, r.NoticeDistance(true), .01f);
            yield return Approach(s, rabbit, rdir, 12, 6, 5.2f);
            float t = 0; while (!r.Bolting && t < 1.5f) { Put(s, rabbit, rdir, 6 - t); t += Time.deltaTime; yield return null; }
            Assert.IsTrue(r.Bolting, "A walking player at 6 m sends a rabbit off within a second.");
            // A blow sends a calm animal off at once.
            var calm = s.Game.FirstOrDefault(g => g.actor.IsAlive && Animal(g).Kind == "rabbit" && g != rabbit && !Animal(g).Bolting && Flat(g.transform.position, s.Player.transform.position) > 20);
            Assert.NotNull(calm, "Another rabbit, far off and grazing.");
            calm.Receive(1, s.Player); yield return null;
            Assert.IsTrue(Animal(calm).Bolting, "Hit, it bolts."); Assert.IsTrue(calm.actor.IsAlive);
            Assert.IsNull(calm.threat.Choose(Time.time, s.IsLivingPartyMember), "It holds no grudge (no threat).");
        }

        [UnityTest] public IEnumerator Game_returns_after_its_respawn()
        {
            var s = Session(); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            Assert.IsTrue(s.Game.All(g => Mathf.Approximately(g.RespawnSeconds, GameAnimals.RespawnSeconds)), "Every game animal comes back after four minutes.");
            Assert.AreEqual(240, GameAnimals.RespawnSeconds);
            var rabbit = OpenAnimal(s, "rabbit"); var a = Animal(rabbit);
            Assert.AreEqual(15, rabbit.actor.Health.Pool.Max);
            rabbit.RespawnSeconds = 3;
            rabbit.actor.Health.ApplyDamage(100000); yield return null;
            Assert.IsFalse(rabbit.actor.IsAlive); Assert.NotNull(rabbit.Drops); Assert.AreEqual("hide.coney", rabbit.Drops[0].item);
            Assert.AreEqual(90, a.Body.Root.localEulerAngles.z, 1, "It lies on its side.");
            yield return new WaitForSeconds(3.6f);
            Assert.IsTrue(rabbit.actor.IsAlive, "It is back."); Assert.AreEqual(15, rabbit.actor.Health.Pool.Current, "Whole.");
            Assert.IsNull(rabbit.Drops, "With nothing on it"); Assert.IsFalse(rabbit.Looted); Assert.AreEqual(-1, LootBeacon.Showing(rabbit), "and no beacon.");
            Assert.Less(Quaternion.Angle(Quaternion.identity, a.Body.Root.localRotation), 1, "On its feet.");
            Assert.LessOrEqual(Vector2.Distance(rabbit.CampCenter, new Vector2(rabbit.transform.position.x, rabbit.transform.position.z)), rabbit.CampRadius * 1.5f + 3, "Somewhere in its group's circle.");
            Assert.IsFalse(s.Zone.WaterAt(new Vector2(rabbit.transform.position.x, rabbit.transform.position.z), out _, out float depth) && depth > .15f, "Not in the water.");
            Assert.Contains(rabbit, s.Game); CollectionAssert.DoesNotContain(s.Enemies, rabbit);
            // A load rebuilds the game with the party: the same animals, alive.
            int count = s.Game.Count; s.Save(false); s.Load();
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(count, s.Game.Count, "The same game after a load.");
            Assert.IsTrue(s.Game.All(g => g != null && g.actor.IsAlive && g.Game), "All alive, all game.");
        }

        [UnityTest] public IEnumerator Chickens_sheep_and_cats_cannot_be_targeted()
        {
            var s = Session(); var life = VillageLife.Active; Assert.NotNull(life);
            foreach (var kind in new[] { "chicken", "sheep", "cat" }) Assert.IsTrue(life.Critters.Any(c => c.Kind == kind), "Oakhaven has its " + kind + "s.");
            foreach (var c in life.Critters)
            {
                Assert.IsFalse(GameAnimals.IsGame(c.Kind), c.Kind + " is not game."); CollectionAssert.Contains(GameAnimals.NeverHunted, c.Kind);
                Assert.IsEmpty(c.GetComponentsInChildren<Collider>(true), c.Kind + ": nothing to click on.");
                Assert.IsNull(c.GetComponentInParent<EncounterEnemy>(), c.Kind + " can never be a target."); Assert.IsNull(c.GetComponentInParent<Crulanda.Gameplay.Actor>(), c.Kind + " has no health.");
            }
            Assert.IsFalse(life.Critters.Any(c => c.Kind == "deer" || c.Kind == "rabbit"), "Deer and rabbits are game now, not critters.");
            // The game is the zone's deer and rabbits (a few may find no dry footing) and nothing else.
            int expected = s.Zone.Zone.life.critters.Where(g => GameAnimals.IsGame(g.kind)).Sum(g => g.count);
            Assert.That(s.Game.Count, Is.InRange((int)(expected * .9f), expected), "Oakhaven's deer and rabbits are all there.");
            Assert.IsTrue(s.Game.All(g => g.Game && (Animal(g).Kind == "deer" || Animal(g).Kind == "rabbit")));
            // Tab among the sheep of the Old Fold and the hens of Harrow farm finds no critter: only an enemy or game, if anything.
            foreach (var spot in new[] { new Vector2(-156, 64), new Vector2(-89, 54), new Vector2(0, -18) })
            {
                s.Player.GetComponent<AdventurerMotor>().Teleport(s.Zone.Ground(spot, 1.1f)); yield return null;
                s.Select(null); s.CycleTarget();
                Assert.IsTrue(s.Target == null || s.Enemies.Contains(s.Target) || s.Game.Contains(s.Target), "Tab at " + spot + " picked something that is not an enemy or game.");
                // A click's ray straight down onto each critter nearby hits nothing that is a target.
                foreach (var c in life.Critters.Where(c => Flat(c.transform.position, s.Player.transform.position) < 25))
                {
                    var hits = Physics.RaycastAll(c.transform.position + Vector3.up * 3, Vector3.down, 4, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    Assert.IsFalse(hits.Any(h => h.collider.GetComponentInParent<EncounterEnemy>() != null && Flat(h.collider.transform.position, c.transform.position) < .5f), c.Kind + " at " + c.transform.position + " is not clickable.");
                }
            }
        }

        [UnityTest] public IEnumerator Game_never_counts_as_an_enemy_for_the_village()
        {
            var s = Session(); var life = VillageLife.Active; Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            var deer = OpenAnimal(s, "deer", 30);
            CollectionAssert.IsEmpty(s.Enemies.Where(e => e.Game), "No game animal among the enemies.");
            // A villager beside a frightened, wounded deer: nobody runs for home.
            var v = life.Villagers.First(x => x.Visible && x.Role != "drinker" && !x.Resident);
            v.Release(deer.transform.position + deer.transform.right * 3 - Vector3.up);   // there, and going about the day
            yield return null;
            deer.Receive(20, s.Player); yield return null;
            Assert.IsTrue(Animal(deer).Bolting);
            Assert.IsFalse(life.Danger(deer.transform.position), "A bolting deer is no danger."); Assert.IsFalse(life.Danger(v.transform.position), "Not where the villager stands either.");
            Assert.IsFalse(life.EnemyNear(deer.transform.position, 10), "Nor an enemy near."); Assert.IsFalse(deer.Engaged, "It never takes anyone on.");
            for (float t = 0; t < 1.5f; t += Time.deltaTime)
            {
                Assert.IsFalse(v.Bubble == "Run! Get inside!" || v.Bubble == "Collectors! Hide!", v.Name + " does not cry out and run for home.");
                yield return null;
            }
            Assert.AreNotEqual("fled", v.Activity);
            // Killed, it counts for nothing a fight counts for: no "zone clear", no story enemy, no saved record.
            bool cleared = life.EnemiesCleared; int story = s.StoryEnemies.Count, deadStory = s.StoryEnemies.Count(e => !e.actor.IsAlive), records = s.Progress.enemies.Count;
            deer.actor.Health.ApplyDamage(100000); yield return null;
            Assert.AreEqual(cleared, life.EnemiesCleared, "The village's \"is it over?\" is about enemies only.");
            Assert.AreEqual(story, s.StoryEnemies.Count); Assert.AreEqual(deadStory, s.StoryEnemies.Count(e => !e.actor.IsAlive));
            Assert.AreEqual(records, s.Progress.enemies.Count);
            Assert.IsFalse(s.Messages.Any(m => m.Contains("is clear for now")), "A dead deer never clears the zone.");
            Assert.IsFalse(s.InCombat);
        }
    }
}
#endif
