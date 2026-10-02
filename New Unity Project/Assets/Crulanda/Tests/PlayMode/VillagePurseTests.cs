#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// The purses in Oakhaven (tools/wip/professions/ADDENDUM.md C): what the player pays the leatherworker, her family spends where
    /// you can see it (Nettie carries a loaf home and Fen a bundle of firewood, and the Tanner chimney is lit when the wood is in);
    /// short of coin they go without and say so; a house that went without firewood stops smoking at dusk while every other chimney
    /// keeps on; and the drinkers drink with every purse at nothing.
    /// </summary>
    public class VillagePurseTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Open(float hour)
        {
            WorldClock.Hour = hour;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-purse-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(root, Session().SaveDirectoryOverride, "This test saves to its own folder.");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; WorldClock.Hour = 8.5f; VillageEconomy.ResetAll();
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        static IEnumerator WaitUntil(Func<bool> done, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!done() && Time.realtimeSinceStartup < end) yield return null;
        }
        const string Maud = "Maud Tanner", Wallet = "bag.simples_wallet";
        static string Tell(Purse p) { return p.household + ": coin " + p.coin + ", claimed " + string.Join("/", p.claimed) + ", met " + string.Join("/", p.met) + (p.cold ? ", cold" : ""); }
        static string Tell(Villager v) { return v.Name + " " + v.Activity + (v.Errand != null ? " on '" + v.Errand.id + "'" : "") + (v.Carrying ? " carrying " + v.Carried : "") + (v.Visible ? "" : " indoors"); }
        /// <summary>Buy the wallet from Maud with gold enough (all in one frame: a merchant's window shuts when you are not beside her).</summary>
        static void BuyWallet(EncounterSession s, Villager maud)
        {
            s.Progress.gold = 100; s.OpenVendor(maud); Assert.AreEqual(Maud, s.VendorNpc, "Her wares are open.");
            s.Buy(Wallet); s.CloseVendor();
            Assert.AreEqual(88, s.Progress.gold, "The wallet is 12 gold.");
        }
        /// <summary>Send someone off afresh from a spot (where a shift had taken them is not the test's business).</summary>
        static void SetOff(Villager v, string from = null) { if (from != null) Assert.IsTrue(v.WorkAt(from), v.Name + " can stand at the " + from); v.Release(v.transform.position); }

        [UnityTest] public IEnumerator Buying_from_the_leatherworker_at_1500_sends_her_family_for_bread_and_firewood()
        {
            VillageEconomy.StartingCoin = 0;   // nothing in any purse: only the player's coin buys today
            yield return Open(13.9f);
            var s = Session(); var life = VillageLife.Active;
            var maud = life.Find(Maud); var fen = life.Find("Fen Walker"); var nettie = life.Find("Nettie");
            var tanner = life.HouseholdOf(Maud); var purse = life.PurseOf(tanner);
            Assert.AreEqual("Tanner", tanner.name); Assert.NotNull(purse, "The Tanners have a purse.");
            Assert.AreEqual(0, purse.coin); Assert.IsEmpty(purse.claimed, Tell(purse));
            Assert.AreSame(nettie, life.Runner(tanner, "bread"), "Nettie goes for the bread."); Assert.AreSame(fen, life.Runner(tanner, "firewood"), "Fen for the firewood.");
            Assert.IsFalse(life.OnTheBooks(tanner, "firewood"), "The Tanners have hands to send: it is carried where you can see it.");
            var home = new List<(Villager who, Errand e, Vector3 at)>();
            life.HandedOver += (v, e, taker) => { if (e.need != null) home.Add((v, e, v.transform.position)); };

            WorldClock.Hour = 15;
            int village = life.Economy.Purses.Sum(p => p.coin);   // no stipend comes in before midnight, and nobody nears the cap
            BuyWallet(s, maud);
            Assert.AreEqual(12, purse.coin, "The wallet's price goes into the Tanners' purse.");
            CollectionAssert.AreEquivalent(new[] { "bread", "firewood", "eggs" }, purse.claimed, "and they set out for all three (" + Tell(purse) + ").");
            Assert.Contains(Maud + ": " + VillageLife.TonightLine, s.Messages);
            SetOff(fen, "tannery"); SetOff(nettie);
            Assert.AreEqual("firewood for the hearth", fen.Errand?.id, Tell(fen)); Assert.AreEqual("bread for the house", nettie.Errand?.id, Tell(nettie));

            Time.timeScale = 4; float start = WorldClock.Hour; bool loaf = false, wood = false;
            yield return WaitUntil(() => { loaf |= nettie.Carried == Load.Bread; wood |= fen.Carried == Load.Logs; return loaf && wood || WorldClock.Hour > start + 1.5f; }, 150);
            Assert.IsTrue(loaf, "Nettie carries a loaf (" + Tell(nettie) + ")."); Assert.IsTrue(wood, "Fen carries a bundle of firewood (" + Tell(fen) + ").");
            Assert.Less(WorldClock.Hour, start + 1, "within a game hour of the purchase.");
            Assert.IsTrue(purse.met.Contains("bread") && purse.met.Contains("firewood"), "Paid for at pick-up (" + Tell(purse) + ").");
            Assert.AreEqual(12 - purse.met.Sum(VillageEconomy.Price), purse.coin, "The Tanners paid for what they fetched (" + Tell(purse) + ").");
            Assert.AreEqual(village + 12, life.Economy.Purses.Sum(p => p.coin), "and the coin stayed in the village: the baker's and the woodcutter's households have it.");

            yield return WaitUntil(() => home.Any(h => h.who == nettie && h.e.need == "bread") && home.Any(h => h.who == fen && h.e.need == "firewood"), 150);
            foreach (var (who, need) in new[] { (nettie, "bread"), (fen, "firewood") })
            {
                var drop = home.FirstOrDefault(h => h.who == who && h.e.need == need);
                Assert.NotNull(drop.who, who.Name + " brings the " + need + " home (" + Tell(who) + ").");
                Assert.Less(Vector3.Distance(drop.at, tanner.house.position), 3.5f, who.Name + " hands it over at the Tanner house door.");
            }
            Assert.IsFalse(purse.cold, "Wood in, the fire is lit.");
        }

        [UnityTest] public IEnumerator Short_of_coin_they_go_without_and_say_so()
        {
            yield return Open(13.9f);
            var s = Session(); var life = VillageLife.Active; var maud = life.Find(Maud); var fen = life.Find("Fen Walker");
            var tanner = life.HouseholdOf(Maud); var purse = life.PurseOf(tanner);
            Assert.AreEqual(3, purse.coin, "With no player the Tanners start the day at their stipend.");
            Assert.IsTrue(purse.claimed.Contains("bread"), "Bread (2) they can stretch to (" + Tell(purse) + ").");
            Assert.AreEqual("firewood", life.Economy.ShortOf(purse), "Firewood (3) they cannot.");

            WorldClock.Hour = 14.05f;   // the firewood's window opens
            SetOff(fen, "tannery");
            Assert.AreEqual("Can't stretch to firewood today.", fen.Bubble, "Fen, who would have gone, says so (" + Tell(fen) + ").");
            Assert.IsFalse(fen.Errand != null && fen.Errand.need != null, "and does not go (" + Tell(fen) + ").");
            fen.Say(null, 0); SetOff(fen, "tannery");
            Assert.IsNull(fen.Bubble, "He says it once a day.");

            WorldClock.Hour = 17.1f; Time.timeScale = 4;   // dusk: the fire goes out
            yield return WaitUntil(() => purse.cold, 30);
            Assert.IsTrue(purse.cold, "No firewood: the Tanner hearth is cold from 17:00 (" + Tell(purse) + ").");
            Assert.AreEqual("We've bread. No fire; Nettie sleeps in her coat.", life.PurseLine(maud), "What the Tanners say to the player.");
            bool heard = false; for (int i = 0; i < 80 && !heard; i++) heard = life.LineFor(maud, true) == life.PurseLine(maud);
            Assert.IsTrue(heard, "Maud says it when you talk to her (one talk in three).");
            Assert.IsNull(life.PurseLine(life.Find("Brannoc Vell")), "A warm house has nothing to say of it.");

            // Paid after the shops shut: the coin is there for tomorrow, and nobody sets out tonight.
            WorldClock.Hour = 18.3f; int before = purse.coin;
            BuyWallet(s, maud);
            Assert.AreEqual(before + 12, purse.coin); Assert.IsFalse(purse.claimed.Contains("firewood"), "Nobody goes for firewood after the shops shut (" + Tell(purse) + ").");
            Assert.Contains(Maud + ": " + VillageLife.TomorrowLine, s.Messages);
        }

        [UnityTest] public IEnumerator A_house_without_firewood_goes_cold_and_the_rest_keep_smoking()
        {
            yield return Open(16.8f);
            var s = Session(); var life = VillageLife.Active; var maud = life.Find(Maud); var fen = life.Find("Fen Walker");
            var tanner = life.HouseholdOf(Maud); var purse = life.PurseOf(tanner); var door = tanner.house;
            Assert.NotNull(door, "The Tanners live behind a door."); Assert.NotNull(door.smoke, "and the Tanner house has a chimney.");
            Assert.IsTrue(door.smoke.emission.enabled, "Smoking in the afternoon, as every chimney does.");
            int chimneys = s.Zone.Doors.Count(d => d.smoke != null); Assert.GreaterOrEqual(chimneys, 10, "Oakhaven's houses have chimneys.");

            WorldClock.Hour = 17.05f; Time.timeScale = 4;
            yield return WaitUntil(() => !door.smoke.emission.enabled, 30);
            Assert.IsFalse(door.smoke.emission.enabled, "The Tanner chimney is cold at dusk (" + Tell(purse) + ").");
            foreach (var d in s.Zone.Doors)
                if (d != door && d.smoke != null) Assert.IsTrue(d.smoke.emission.enabled, d.name + " keeps smoking (" + (life.PurseOf(life.HouseholdAt(d)) is Purse p ? Tell(p) : "nobody's purse") + ").");

            // Paid before the shops shut: Fen fetches a bundle, and the chimney is lit when the wood is home.
            BuyWallet(s, maud);
            Assert.IsTrue(purse.claimed.Contains("firewood"), Tell(purse)); Assert.Contains(Maud + ": " + VillageLife.TonightLine, s.Messages);
            SetOff(fen, "tannery");
            Assert.AreEqual("firewood for the hearth", fen.Errand?.id, Tell(fen));
            yield return WaitUntil(() => fen.Carried == Load.Logs, 120);
            Assert.AreEqual(Load.Logs, fen.Carried, "Fen has the bundle (" + Tell(fen) + ").");
            Assert.IsFalse(door.smoke.emission.enabled, "Not lit till the wood is home.");
            yield return WaitUntil(() => door.smoke.emission.enabled, 120);
            Assert.IsTrue(door.smoke.emission.enabled, "Wood home, the Tanner chimney smokes again (" + Tell(fen) + ").");
            Assert.Less(Vector3.Distance(fen.transform.position, door.position), 4, "Fen is at the door.");
            Assert.IsFalse(purse.cold);
        }

        [UnityTest] public IEnumerator Drinkers_drink_with_every_purse_at_zero()
        {
            VillageEconomy.StartingCoin = 0;
            yield return Open(11.1f);
            var life = VillageLife.Active; var drinkers = life.Villagers.Where(v => v.Role == "drinker").ToList();
            Assert.GreaterOrEqual(drinkers.Count, 2, "Oakhaven has its out-of-work.");
            Assert.IsTrue(life.Economy.Purses.All(p => p.coin == 0 && p.claimed.Count == 0), "Every purse is empty.");
            life.Stock["inn.ale"] = 3; Time.timeScale = 4;
            var before = drinkers.ToDictionary(d => d, d => d.Drunk);
            Villager first = null;
            yield return WaitUntil(() => (first = drinkers.FirstOrDefault(d => d.Drunk > before[d])) != null, 90);
            Assert.NotNull(first, "A drinker reaches the inn and has one.");
            Assert.AreEqual("inn", first.Activity);
            yield return WaitUntil(() => life.Count("inn.ale") == 0 && drinkers.Any(d => d.Spent), 150);
            Assert.AreEqual(0, life.Count("inn.ale"), "They drink the cask dry: drinkers never pay.");
            Assert.IsTrue(drinkers.Any(d => d.Spent), "and call it a day.");
            Assert.IsTrue(life.Economy.Purses.All(p => p.coin == 0), "Nobody's purse was drawn on, or filled.");
        }
    }
}
#endif
