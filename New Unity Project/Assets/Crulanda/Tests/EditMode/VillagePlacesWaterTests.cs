using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Nobody stands, works or is lined up in a river (play note 236, 2026-10-18: "some professions in the water under bridge"). What
    /// Chris saw was the world capture's trades line-up (EncounterCapture): it stood the trades round the zone's middle, a village's
    /// green, which in the Lowtowns is the Chemical River under the Sett Bridge. Read from the zones' data and ZoneWater without
    /// building a zone:
    /// - the line-up's usual ground is wet in the Lowtowns, and the ground it moves to is dry (EncounterCapture.LineupDry);
    /// - the villagers' thrown "wander" spots (VillageLife.FindPlaces; the Lowtowns, with no green, falls back to them all day) do
    ///   reach the river, and a spot on its bed is wet while a bridge's deck over it is dry (VillageLife.Wet, which
    ///   VillageLife.AddPlace drops thrown spots by: the river's shallows are walkable, so the navmesh alone kept them);
    /// - in every zone with water, no resident's post, crafting station or trade's building stands in a creek's or a lake's drawn water.
    /// </summary>
    public class VillagePlacesWaterTests
    {
        static ZoneDefinition Load(string id) { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones", id + ".json"))); }
        static ZoneWater WaterOf(ZoneDefinition z)
        {
            var w = new ZoneWater();
            w.Prepare(z, (x, y) => z.terraces != null && z.terraces.Length > 0 ? ZoneBuilder.TerraceGround(z.terraces, x, y, 0) : 0);
            return w;
        }
        /// <summary>The props a trade works at or a stall-keeper stands by (ZoneBuilder's Workplace registrations and the markets).</summary>
        static readonly string[] TradeKinds = { "kitchen", "oven", "leathershop", "tannery", "dye_vat", "forge", "stall", "inn", "dryhut", "mill", "market_hall", "soup_hall", "gamerack", "well", "market", "shard_rack" };

        [Test] public void The_capture_line_ups_keep_out_of_the_Lowtowns_river()
        {
            // The world capture lined the trades up round the zone's middle (EncounterCapture): a village's green, but the Lowtowns'
            // Chemical River under the Sett Bridge, where Chris saw the baker, the smith and the merchant standing in it.
            var z = Load("lowtowns"); var water = WaterOf(z);
            Assert.IsFalse(EncounterCapture.LineupDry(water, Vector2.zero), "The Lowtowns' middle is the river: the line-up must move.");
            Assert.IsTrue(EncounterCapture.LineupDry(water, z.spawns.recovery) || EncounterCapture.LineupDry(water, z.spawns.player), "and the recovery spot or the player's spawn, where it moves to, is dry.");
        }

        [Test] public void The_wander_throw_reaches_the_river_and_a_spot_in_it_is_wet()
        {
            var z = Load("lowtowns"); var water = WaterOf(z);
            var r = new System.Random(z.seed + 7); int wet = 0, thrown = 0;
            for (int i = 0; i < 2000; i++)
            {
                // As VillageLife.FindPlaces throws "wander": anywhere over 1.6 flat radii square.
                var p = new Vector2(((float)r.NextDouble() - .5f) * z.flatRadius * 1.6f, ((float)r.NextDouble() - .5f) * z.flatRadius * 1.6f);
                if (Mathf.Abs(p.x) > z.size / 2 || Mathf.Abs(p.y) > z.size / 2) continue;
                thrown++;
                if (!water.InDrawnWater(p, out float surface)) continue;
                wet++;
                Assert.IsTrue(VillageLife.Wet(water, new Vector3(p.x, surface - .6f, p.y)), "A spot on the river's bed at " + p + " is wet: nobody is sent to stand there.");
                Assert.IsFalse(VillageLife.Wet(water, new Vector3(p.x, surface + 1.2f, p.y)), "A bridge's deck or a quay over the water at " + p + " is dry.");
            }
            Assert.Greater(wet, thrown / 100, "The wander throw lands in the Lowtowns' river (" + wet + " of " + thrown + "): why VillageLife.AddPlace must drop wet spots.");
            // The town's dry streets stay places: the Rusty Spoon's door, the Rust Market.
            foreach (var dry in new[] { new Vector2(-125, 8), new Vector2(110, 60) })
                Assert.IsFalse(water.InDrawnWater(dry, out _), dry + " is dry ground in the town.");
        }

        [Test] public void No_post_station_or_trade_stands_in_a_river()
        {
            var problems = new List<string>(); int zones = 0;
            foreach (var path in Directory.GetFiles(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones"), "*.json"))
            {
                var z = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(path));
                if (z == null || ((z.water == null || z.water.Length == 0) && (z.lakes == null || z.lakes.Length == 0))) continue;
                zones++; var water = WaterOf(z); string id = Path.GetFileNameWithoutExtension(path);
                void Check(string what, Vector2 at) { if (water.InDrawnWater(at, out _)) problems.Add(id + ": " + what + " at " + at + " stands in the water"); }
                if (z.life != null && z.life.residents != null)
                    foreach (var res in z.life.residents.Where(res => res != null && res.at != Vector2.zero && !res.under)) Check("the post of " + res.name, res.at);
                if (z.stations != null) foreach (var s in z.stations.Where(s => s != null)) Check("the station '" + s.name + "'", s.at);
                if (z.props != null) foreach (var p in z.props.Where(p => p != null && TradeKinds.Contains(p.kind))) Check("the " + p.kind + " '" + p.name + "'", p.at);
            }
            Assert.Greater(zones, 3, "The zones with water were read (Oakhaven, Verdant Shore, Khaven, Hallow's Creek, the Lowtowns).");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
