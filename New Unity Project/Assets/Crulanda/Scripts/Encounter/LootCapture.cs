using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The loot shots (loot DESIGN.md section 8), run with <c>--crulanda-loot-capture &lt;dir&gt;</c> from a windowed player (the
    /// loot window and tooltips are IMGUI), on the capture's throwaway save (EncounterCapture), so nothing here touches a real
    /// character. Four camp mobs are killed and laid side by side on open ground near the green, each holding one quality:
    /// 01-beams-day and 01-beams-night: the white twinkle, the green glint and column, the blue beam and the purple beam with its
    /// ring (the HUD, the player and the village hidden); 02-loot-window: the window over a body holding coins, a rare blade, an
    /// uncommon helm and junk; 03-compare-tooltip: the rare blade's tooltip against a common blade worn ("+N weapon damage" in
    /// green); 04-upgrade-arrows: the bags beside the window, the pieces that beat what is worn marked with the green arrow.
    /// </summary>
    public sealed class LootCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-loot-capture";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }
        /// <summary>What lies on each of the four bodies: one quality each, poor-and-common to epic.</summary>
        static readonly (string title, int coins, (string item, int count)[] drops)[] Bodies = {
            ("Coins and junk", 6, new[] { ("junk.wolf_fang", 2), ("junk.wolf_pelt", 1) }),
            ("Uncommon", 9, new[] { (ItemDatabase.GearId("hands", 5, 2, 211), 1), ("junk.wolf_fang", 1) }),
            ("Rare", 14, new[] { (ItemDatabase.GearId("mainhand", 7, 3, 4242), 1), ("junk.wolf_pelt", 1) }),
            ("Epic", 31, new[] { (ItemDatabase.GearId("offhand", 9, 4, 977), 1) }),
        };
        EncounterSession session;

        public IEnumerator Run(EncounterSession s, string directory)
        {
            session = s; Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(2);
            var zone = Crulanda.World.ZoneBuilder.Active;
            var camp = s.Enemies.FindAll(e => e != null && e.Camp && !e.Elite && !e.Hidden && e.actor.IsAlive);
            if (s.Items == null || zone == null || camp.Count < Bodies.Length) { Debug.LogError("Loot capture: needs the items, a zone and four camp mobs."); Application.Quit(1); yield break; }
            // Level 8, so every piece on the bodies can be worn, wearing a common blade the rare one beats.
            s.Progress.experience = EncounterProgress.XpForLevel(8); s.Player.SetLevel(8);
            string worn = ItemDatabase.GearId("mainhand", 6, 1, 3);
            if (Inventory.Add(s.Progress, s.Items, worn, 1) == 0) s.EquipFromBag(s.Progress.bag.FindIndex(x => x.item == worn));
            var weather = Crulanda.World.WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            // The rest of the world holds still: every other enemy stands down, and the village is put away.
            foreach (var e in s.Enemies) if (e != null && !camp.GetRange(0, Bodies.Length).Contains(e)) { e.enabled = false; foreach (var r in e.GetComponentsInChildren<Renderer>(true)) r.enabled = false; }
            if (VillageLife.Active != null) VillageLife.Active.gameObject.SetActive(false);
            var spot = WardrobeCapture.FindSpot(zone, out float ground);
            var bodies = new List<EncounterEnemy>();
            for (int i = 0; i < Bodies.Length; i++)
            {
                var e = camp[i]; e.RespawnSeconds = 1e6f;
                var agent = e.GetComponent<NavMeshAgent>(); if (agent != null) agent.enabled = false;
                e.transform.SetPositionAndRotation(zone.Ground(spot + new Vector2((i - 1.5f) * 3, 0), 1), Quaternion.Euler(0, 90, 0));
                e.actor.Health.ApplyDamage(100000);
                bodies.Add(e);
            }
            yield return null;
            for (int i = 0; i < Bodies.Length; i++)
            {
                var drops = new List<LootDrop>(); foreach (var (item, count) in Bodies[i].drops) drops.Add(new LootDrop(item, count));
                s.PutLoot(bodies[i], Bodies[i].coins, drops);
            }
            // 01: the four beacons side by side, at noon and at night, the HUD and the player out of the way.
            EncounterHud.Hidden = true;
            var motor = s.Player.GetComponent<AdventurerMotor>(); motor.enabled = false;
            var own = Array.FindAll(s.Player.GetComponentsInChildren<Renderer>(), r => r.enabled); foreach (var r in own) r.enabled = false;
            var view = s.View; var focus = new Vector3(spot.x, ground + 2.6f, spot.y);
            var cam = focus + new Vector3(0, 1.2f, -15); cam.y = Mathf.Max(cam.y, zone.HeightAt(cam.x, cam.z) + 1.2f);
            foreach (var (name, hour) in new[] { ("day", 12f), ("night", 22.5f) })
            {
                Crulanda.World.WorldClock.Hour = hour;
                for (float w = 0; w < 1.2f; w += Time.deltaTime) { view.transform.SetPositionAndRotation(cam, Quaternion.LookRotation(focus - cam)); Crulanda.World.TreeFade.UpdateAll(cam, focus, focus); yield return null; }
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, "01-beams-" + name + ".png")); yield return new WaitForSeconds(.5f);
            }
            // 02 to 04: back to the player and the HUD, beside the rare body, its loot window open.
            Crulanda.World.WorldClock.Hour = 12; EncounterHud.Hidden = false; foreach (var r in own) r.enabled = true; motor.enabled = true;
            var rare = bodies[2];
            s.PutLoot(rare, 14, new[] { new LootDrop(ItemDatabase.GearId("mainhand", 7, 3, 4242), 1), new LootDrop(ItemDatabase.GearId("head", 6, 2, 88), 1), new LootDrop("junk.wolf_fang", 2), new LootDrop("junk.wolf_pelt", 1) });
            motor.Teleport(rare.transform.position + new Vector3(0, .2f, -2.2f)); motor.SetView(0, 22, 6);
            yield return new WaitForSeconds(.8f);
            s.OpenLoot(rare); yield return new WaitForSeconds(.6f);
            if (!s.LootOpen) Debug.LogError("Loot capture: the loot window did not open.");
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "02-loot-window.png")); yield return new WaitForSeconds(.4f);
            // 03: the rare blade's tooltip as if hovered on its row.
            EncounterHud.PinnedTooltip = ItemDatabase.GearId("mainhand", 7, 3, 4242); EncounterHud.PinnedTooltipAt = new Vector2(1010, 330);
            yield return new WaitForSeconds(.4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "03-compare-tooltip.png")); yield return new WaitForSeconds(.4f);
            EncounterHud.PinnedTooltip = null;
            // 04: the bags beside the window: two pieces that beat what is worn (an empty slot, a better blade) and two that do not.
            foreach (var id in new[] { ItemDatabase.GearId("feet", 6, 2, 31), ItemDatabase.GearId("mainhand", 8, 2, 77), ItemDatabase.GearId("mainhand", 3, 0, 12), ItemDatabase.GearId("legs", 13, 3, 5) })
                Inventory.Add(s.Progress, s.Items, id, 1);
            s.InventoryOpen = true; yield return new WaitForSeconds(.6f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "04-upgrade-arrows.png")); yield return new WaitForSeconds(.4f);
            s.InventoryOpen = false; s.CloseLoot();
            Debug.Log("LOOT_CAPTURE_DONE"); Application.Quit(0);
        }
    }
}
