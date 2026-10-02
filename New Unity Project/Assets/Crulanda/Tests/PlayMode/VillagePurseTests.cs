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
    /// you can see it (on the day's stipend Nettie has bought the loaf by then, so Fen carries a bundle of firewood home, and the
    /// Tanner chimney is lit when the wood is in; only with every purse emptied by the test does Nettie go for a loaf on the
    /// player's coin as well); a run given up before pick-up is set out on again; short of coin they go without and say so; a house
    /// that went without firewood stops smoking at dusk while every other chimney keeps on; a zone loaded again finds the wood home;
    /// and the drinkers drink with every purse at nothing.
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

        [UnityTest] public IEnumerator With_the_days_stipend_buying_from_the_leatherworker_at_1500_sends_Fen_for_firewood()
        {
            yield return Open(13.9f);
            var s = Session(); var life = VillageLife.Active;
            var maud = life.Find(Maud); var fen = life.Find("Fen Walker"); var nettie = life.Find("Nettie");
            var tanner = life.HouseholdOf(Maud); var purse = life.PurseOf(tanner);
            Assert.IsTrue(purse.claimed.Contains("bread"), "The stipend stretches to the loaf (" + Tell(purse) + ").");
            SetOff(nettie);
            Assert.AreEqual("bread for the house", nettie.Errand?.id, Tell(nettie));
            Time.timeScale = 4;
            yield return WaitUntil(() => purse.met.Contains("bread"), 150);
            Assert.IsTrue(purse.met.Contains("bread"), "Nettie buys the loaf on the stipend before the player comes (" + Tell(purse) + ", " + Tell(nettie) + ").");
            Time.timeScale = 1;
            var home = new List<(Villager who, Errand e, Vector3 at)>();
            life.HandedOver += (v, e, taker) => { if (e.need != null) home.Add((v, e, v.transform.position)); };

            WorldClock.Hour = 15;
            BuyWallet(s, maud);
            CollectionAssert.AreEquivalent(new[] { "firewood", "eggs" }, purse.claimed, "The player's coin buys the wood and the eggs (" + Tell(purse) + ").");
            Assert.Contains(Maud + ": " + VillageLife.TonightLine, s.Messages);
            SetOff(fen, "tannery"); SetOff(nettie);
            Assert.AreEqual("firewood for the hearth", fen.Errand?.id, Tell(fen));
            Assert.IsFalse(nettie.Errand != null && nettie.Errand.need != null, "Nettie has no shopping left today (" + Tell(nettie) + ").");
            var ama = life.Seller("eggs"); Assert.AreEqual("Ama Rusk", ama?.Name, "Eggs are paid to the Rusks.");
            var stalls = life.OwnPlaces(ama, "stall"); Assert.NotNull(stalls, "Ama keeps a stall.");
            foreach (var at in stalls) Assert.AreEqual("Produce stall", life.WorkplaceAt(at), "and fetched from Ama's own stall.");

            Time.timeScale = 4; float start = WorldClock.Hour; bool wood = false;
            yield return WaitUntil(() => (wood |= fen.Carried == Load.Logs) || WorldClock.Hour > start + 1.5f, 150);
            Assert.IsTrue(wood, "Fen carries a bundle of firewood (" + Tell(fen) + ").");
            Assert.Less(WorldClock.Hour, start + 1, "within a game hour of the purchase.");
            yield return WaitUntil(() => home.Any(h => h.who == fen && h.e.need == "firewood"), 150);
            var drop = home.FirstOrDefault(h => h.who == fen && h.e.need == "firewood");
            Assert.NotNull(drop.who, "Fen brings the firewood home (" + Tell(fen) + ").");
            Assert.Less(Vector3.Distance(drop.at, tanner.house.position), 3.5f, "He hands it over at the Tanner house door.");
            Assert.IsFalse(purse.cold, "Wood in, the fire is lit.");
        }

        /// <summary>Only with every purse emptied by the test (never so in play: the stipend buys the Tanners' loaf by early afternoon)
        /// does the player's coin send Nettie for a loaf as well as Fen for firewood.</summary>
        [UnityTest] public IEnumerator With_every_purse_empty_buying_from_the_leatherworker_at_1500_sends_her_family_for_bread_and_firewood()
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
            Assert.AreEqual("No fire again. I sleep in my coat.", life.PurseLine(life.Find("Nettie")), "Nettie says it of herself.");
            bool heard = false; for (int i = 0; i < 80 && !heard; i++) heard = life.LineFor(maud, true) == life.PurseLine(maud);
            Assert.IsTrue(heard, "Maud says it when you talk to her (one talk in three).");
            Assert.IsNull(life.PurseLine(life.Find("Brannoc Vell")), "A warm house has nothing to say of it.");

            // Paid after the shops shut: the coin is there for tomorrow, and nobody sets out tonight.
            WorldClock.Hour = 18.3f; int before = purse.coin;
            BuyWallet(s, maud);
            Assert.AreEqual(before + 12, purse.coin); Assert.IsFalse(purse.claimed.Contains("firewood"), "Nobody goes for firewood after the shops shut (" + Tell(purse) + ").");
            Assert.Contains(Maud + ": " + VillageLife.TomorrowLine, s.Messages);
            life.Paid(Maud, 5);
            Assert.AreEqual(1, s.Messages.Count(m => m == Maud + ": " + VillageLife.TomorrowLine), "She says it once an evening.");
            life.Paid("Brannoc Vell", 5);   // his wood was bought on the books at 14:00
            Assert.IsFalse(s.Messages.Contains("Brannoc Vell: " + VillageLife.TomorrowLine), "A warm house says nothing of tomorrow's fire (" + Tell(life.PurseOf(life.HouseholdOf("Brannoc Vell"))) + ").");

            // The next morning the coin is there for the wood: the chimney stays out till it is home, but nobody says tonight will be cold.
            WorldClock.Hour = 10;
            yield return WaitUntil(() => purse.claimed.Contains("firewood"), 30);
            Assert.IsTrue(purse.claimed.Contains("firewood"), "A new day: the firewood is claimed (" + Tell(purse) + ").");
            Assert.IsTrue(purse.cold, "The hearth stays cold till the wood is home.");
            Assert.IsNull(life.PurseLine(maud), "With the wood on the way, Maud does not say tonight will be cold.");
        }

        [UnityTest] public IEnumerator A_run_given_up_before_pick_up_is_set_out_on_again_and_one_given_up_after_lights_the_fire()
        {
            yield return Open(14.5f);
            var s = Session(); var life = VillageLife.Active; var maud = life.Find(Maud); var fen = life.Find("Fen Walker");
            var tanner = life.HouseholdOf(Maud); var purse = life.PurseOf(tanner);
            BuyWallet(s, maud);
            Assert.IsTrue(purse.claimed.Contains("firewood"), Tell(purse));
            SetOff(fen, "tannery");
            Assert.AreEqual("firewood for the hearth", fen.Errand?.id, Tell(fen));

            // Called away before he reaches the woodyard (as a flight from the collectors or bedtime would): the coin stays set aside,
            fen.StandAt(fen.transform.position, 0);
            Assert.IsNull(fen.Errand, Tell(fen));
            Assert.IsTrue(purse.claimed.Contains("firewood"), "The coin for the wood is still set aside (" + Tell(purse) + ").");
            // and he sets out for it again.
            fen.Release(fen.transform.position);
            Assert.AreEqual("firewood for the hearth", fen.Errand?.id, "Fen sets out for the wood again (" + Tell(fen) + ").");

            // Called away once the bundle is paid for at the woodyard: it goes home with him, and the fire is lit.
            purse.cold = true; tanner.ApplyHearth(true);
            Assert.IsTrue(life.Fetched(fen, fen.Errand), "Paid at pick-up (" + Tell(purse) + ").");
            fen.StandAt(fen.transform.position, 0);
            Assert.IsFalse(purse.cold, "The wood went home with him: the hearth is lit (" + Tell(purse) + ").");
            Assert.IsTrue(tanner.house.smoke.emission.enabled, "and the Tanner chimney smokes.");
            fen.Release(fen.transform.position);
            Assert.AreNotEqual("firewood for the hearth", fen.Errand?.id, "Bought, it is not fetched again (" + Tell(fen) + ").");
        }

        [UnityTest] public IEnumerator A_zone_loaded_again_finds_wood_bought_while_away_home()
        {
            yield return Open(17.3f);
            var purse = VillageLife.Active.PurseOf(VillageLife.Active.HouseholdOf(Maud));
            // The player leaves while Fen carries home a bundle paid for at the woodyard, on a cold evening.
            purse.cold = true; purse.claimed.Remove("firewood"); purse.met.Add("firewood");
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(root, Session().SaveDirectoryOverride, "This test saves to its own folder.");
            var life = VillageLife.Active; var tanner = life.HouseholdOf(Maud);
            Assert.AreSame(purse, life.PurseOf(tanner), "The purses live for the play session.");
            Assert.IsFalse(purse.cold, "The wood is home: the hearth is lit (" + Tell(purse) + ").");
            Time.timeScale = 4;
            yield return WaitUntil(() => false, 6);   // past a tick of the purses (20 s of game time)
            Assert.IsTrue(tanner.house.smoke.emission.enabled, "and the Tanner chimney smokes (" + Tell(purse) + ").");
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
