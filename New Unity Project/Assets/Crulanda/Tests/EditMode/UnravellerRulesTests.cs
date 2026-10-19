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
    /// The Unraveller's fight, rule by rule (Docs/WORLD_BOSS_1.md 3.3-3.4; build steps 4 and 5; UnravellerRules, UnravellerTuning), without a
    /// scene: the numbers come from EncounterContent/Raids/unraveller.json; the phases at 70% and 40%; the harpoons' pins shortening 12, 11 ...
    /// 7 and the hauling with two in; her touch on whom she blinks to (the top of her threat, not one hidden in the river); Unravel's stacks
    /// and the erasing at seven; blackened steel three deep; the Feeding's meal and the 10 s interrupt immunity; the brood shattering three
    /// times over; the three salts that free the swallowed (three different hands within 12 s), and the erasing at 12 s; the hunt and the
    /// river; the Unravelling's growing pulse; the feast at 9:00.
    /// </summary>
    public class UnravellerRulesTests
    {
        static UnravellerTuning T { get { return new UnravellerTuning(); } }
        const string File_ = "Assets/Crulanda/EncounterContent/Raids/unraveller.json";

        [Test] public void The_numbers_come_from_the_raid_file_and_only_what_it_says_changes()
        {
            Assert.IsTrue(File.Exists(File_), "the data file");
            var t = UnravellerTuning.Parse(File.ReadAllText(File_));
            var d = new UnravellerTuning();
            Assert.AreEqual(d.health, t.health); Assert.AreEqual(175000, t.health, "tune-3: the fast pulls had her at 18-23% by 4:50, to die only to the pulse");
            Assert.AreEqual(540, t.enrageSeconds, "the feast at 9:00");
            Assert.AreEqual(9, t.blinkEvery); Assert.AreEqual(12, t.blinkReach); Assert.AreEqual(.1f, t.lighthouseShare, 1e-4f, "run 1: .18 lost her to the casters");
            Assert.AreEqual(12, t.pinFirst); Assert.AreEqual(7, t.pinLeast); Assert.AreEqual(3, t.steelStacks);
            Assert.AreEqual(4, t.swapAt); Assert.AreEqual(7, t.erasedAt); Assert.AreEqual(3, t.saltsToFree); Assert.AreEqual(12, t.saltWindow); Assert.AreEqual(10, t.swallowSeconds, "raid 3");
            // The file and the defaults agree everywhere (a build without the file plays the same fight).
            Assert.AreEqual(JsonUtility.ToJson(d), JsonUtility.ToJson(t), "the file's numbers are the defaults' (UnravellerTuning)");
            var tuned = UnravellerTuning.Parse("{\"health\": 90000, \"pinLeast\": 6}");
            Assert.AreEqual(90000, tuned.health); Assert.AreEqual(6, tuned.pinLeast); Assert.AreEqual(12, tuned.pinFirst, "the rest as the defaults");
            Assert.AreEqual(new UnravellerTuning().health, UnravellerTuning.Parse("not json").health, "a broken file plays the defaults");
            Assert.AreEqual(15, t.FeedEvery(1)); Assert.AreEqual(20, t.FeedEvery(2)); Assert.AreEqual(20, t.FeedEvery(3));
            Assert.AreEqual(0, t.BroodEvery(1)); Assert.AreEqual(30, t.BroodEvery(2)); Assert.AreEqual(40, t.BroodEvery(3));
            Assert.AreEqual(0, t.SwallowEvery(1)); Assert.AreEqual(35, t.SwallowEvery(2)); Assert.AreEqual(40, t.SwallowEvery(3));
        }

        [Test] public void Three_phases_at_seventy_and_forty()
        {
            Assert.AreEqual(1, UnravellerRules.Phase(1, T)); Assert.AreEqual(1, UnravellerRules.Phase(.71f, T));
            Assert.AreEqual(2, UnravellerRules.Phase(.7f, T)); Assert.AreEqual(2, UnravellerRules.Phase(.41f, T));
            Assert.AreEqual(3, UnravellerRules.Phase(.4f, T)); Assert.AreEqual(3, UnravellerRules.Phase(.01f, T));
            // Run 1: the brood she ate healed her back over 70% and the phase went back to 1. A phase once reached holds.
            Assert.AreEqual(2, UnravellerRules.PhaseNow(2, .72f, T), "healed back to 72%: still The Brood");
            Assert.AreEqual(3, UnravellerRules.PhaseNow(3, .5f, T)); Assert.AreEqual(2, UnravellerRules.PhaseNow(1, .6f, T)); Assert.AreEqual(1, UnravellerRules.PhaseNow(0, 1, T), "the pull");
        }

        [Test] public void Each_harpoon_pins_her_a_second_shorter_down_to_seven_and_two_in_haul_her()
        {
            var p = new PinTrack(); var t = T; float now = 100;
            var got = new List<float>();
            for (int i = 0; i < 8; i++) { got.Add(p.Harpoon(now, t)); now += got[i]; }
            CollectionAssert.AreEqual(new[] { 12f, 11, 10, 9, 8, 7, 7, 7 }, got, "12, 11, 10 ... never under 7");
            var q = new PinTrack(); Assert.IsFalse(q.Pinned(0), "free before any harpoon");
            q.Harpoon(0, t); Assert.IsTrue(q.Pinned(5)); Assert.AreEqual(7, q.Left(5), 1e-4f, "the pin's timer");
            Assert.AreEqual(1, q.In(5)); Assert.IsFalse(UnravellerRules.Hauled(q.In(5), t), "one harpoon: pinned, not hauled");
            q.Harpoon(10, t);   // a second before the first shakes loose (11 s from 10)
            Assert.AreEqual(2, q.In(10.5f)); Assert.IsTrue(UnravellerRules.Hauled(q.In(10.5f), t), "two in at once: hauled");
            Assert.AreEqual(1, q.In(12.5f), "the first shaken loose at 12"); Assert.IsFalse(UnravellerRules.Hauled(q.In(12.5f), t));
            Assert.AreEqual(21, q.PinnedUntil, 1e-4f, "pinned till the second's 11 s are out");
            Assert.AreEqual(1.1f, UnravellerRules.TakenScale(true, false, t), 1e-4f, "hauled: 10% more");
            Assert.AreEqual(1.5f, UnravellerRules.TakenScale(false, true, t), 1e-4f, "calcified: 50% more");
            Assert.AreEqual(1, UnravellerRules.TakenScale(false, false, t), 1e-4f);
        }

        [Test] public void She_blinks_onto_the_top_of_her_threat_and_her_touch_erases_any_but_a_tank()
        {
            var all = new List<UnravellerCandidate> {
                new UnravellerCandidate { id = "tank", threat = 900, distance = 3, tank = true },
                new UnravellerCandidate { id = "mage", threat = 1200, distance = 14, magic = true },
                new UnravellerCandidate { id = "healer", threat = 1500, distance = 18, magic = true, inRiver = true },
                new UnravellerCandidate { id = "dead", threat = 5000, distance = 5, alive = false } };
            Assert.AreEqual("mage", UnravellerRules.BlinkTarget(all), "the top she can see: the healer is hidden in the river, the dead are not hers");
            all[2].distance = 2.5f;
            Assert.AreEqual("healer", UnravellerRules.BlinkTarget(all), "in the river but within touching: not hidden");
            Assert.AreEqual(600, UnravellerRules.TouchDamage(1000, false, T), "60% of a member's health");
            Assert.AreEqual(0, UnravellerRules.TouchDamage(1000, true, T), "a tank takes her as a tank does");
            Assert.IsNull(UnravellerRules.BlinkTarget(new List<UnravellerCandidate>()));
        }

        [Test] public void Unravel_stacks_on_her_target_and_seven_erase_it()
        {
            var u = new UnravelStacks(); var t = T;
            t.unravelSeconds = 20;   // the doc's 20 s here; the file's is shorter (run 1)
            for (int i = 1; i <= 4; i++) Assert.AreEqual(i, u.Add("tank", i * 2.2f, t));
            Assert.AreEqual(4, u.Count("tank", 9)); Assert.AreEqual(0, u.Count("other", 9));
            Assert.AreEqual(-.24f, UnravellerRules.UnravelArmour(4, t), 1e-4f, "6% less armour a stack");
            Assert.AreEqual(1.2f, UnravellerRules.UnravelTaken(4, t), 1e-4f, "5% more taken a stack");
            Assert.AreEqual(3, u.Count("tank", 2.2f + 20.01f), "each stack falls off 20 s after it went on");
            Assert.AreEqual(0, u.Count("tank", 8.8f + 20.01f), "all gone 20 s after the last");
            Assert.IsFalse(UnravellerRules.Erased(6, t)); Assert.IsTrue(UnravellerRules.Erased(7, t), "no swap: seven and the tank is erased");
        }

        [Test] public void Steel_goes_black_three_stacks_deep_on_her_and_one_on_a_Weave_Eater()
        {
            Assert.AreEqual(1, UnravellerRules.SteelNext(0, 3)); Assert.AreEqual(2, UnravellerRules.SteelNext(1, 3)); Assert.AreEqual(3, UnravellerRules.SteelNext(2, 3));
            Assert.AreEqual(3, UnravellerRules.SteelNext(3, 3), "never past three");
            Assert.AreEqual(1, UnravellerRules.SteelNext(1, 1), "a Weave-Eater's: refreshed, never stacked (as built)");
            Assert.AreEqual(-.3f, WeaveEaterRules.Blackened(3).modifiers[0].value, 1e-4f, "-10% attack power a stack");
            Assert.AreEqual(-.1f, WeaveEaterRules.Blackened().modifiers[0].value, 1e-4f, "the Weave-Eaters' as it was");
            Assert.AreEqual("Blackened steel (3)", WeaveEaterRules.Blackened(3).name);
        }

        [Test] public void The_Feeding_feeds_her_and_an_interrupt_makes_her_immune_ten_seconds()
        {
            var t = T;
            Assert.AreEqual(.03f, UnravellerRules.FeedHeal(10, t), 1e-5f, "0.3% of her a good effect eaten");
            Assert.AreEqual(.2f, UnravellerRules.FeedGrow(10, t), 1e-5f, "2% heavier blows a good effect eaten");
            Assert.IsTrue(UnravellerRules.InterruptTakes(50, -1, t), "the first interrupt takes");
            Assert.IsFalse(UnravellerRules.InterruptTakes(59.9f, 50, t), "the next within 10 s does not");
            Assert.IsTrue(UnravellerRules.InterruptTakes(60, 50, t), "after 10 s it does: interrupters take turns");
            Assert.AreEqual(EncounterEnemy.FeedKind, EncounterEnemy.FeedingCast(t).kind);
            Assert.AreEqual(3, EncounterEnemy.FeedingCast(t).cast, 1e-4f, "a 3 s channel");
            CollectionAssert.Contains(MobCasts.Kinds, EncounterEnemy.FeedKind);
        }

        [Test] public void Unmade_ground_bites_harder_the_longer_one_stands_in_it()
        {
            var t = T;
            Assert.AreEqual(.15f, UnravellerRules.GroundBite(0, t), 1e-4f, "15% the first second (raid 4: 10% was too little)");
            Assert.AreEqual(.25f, UnravellerRules.GroundBite(2, t), 1e-4f, "5% more each second stood");
            Assert.AreEqual(EncounterEnemy.MaxBlowShare, UnravellerRules.GroundBite(20, t), 1e-4f, "never past the 60% cap");
            Assert.AreEqual(3, t.broodCount, "three brood a wave (Chris, raid 7: \"mare down to 3 spiders per add\"; before: \"ok maybe 5 is too many. drop to 4\")");
        }

        [Test] public void A_salted_brood_shatters_at_three_times_and_one_that_reaches_her_is_eaten()
        {
            var t = T;
            Assert.AreEqual(300, UnravellerRules.ShatterDamage(100, true, t)); Assert.AreEqual(100, UnravellerRules.ShatterDamage(100, false, t));
            Assert.IsTrue(UnravellerRules.BroodEaten(2.5f, t)); Assert.IsFalse(UnravellerRules.BroodEaten(2.6f, t));
        }

        [Test] public void Three_salts_from_three_hands_within_twelve_seconds_free_the_swallowed()
        {
            var t = T; var s = new SwallowSalts();
            Assert.IsFalse(s.Salt("a", 0, t), "salt with nobody swallowed does nothing");
            s.Begin("victim", 0);
            Assert.IsFalse(s.Salt("a", 1, t)); Assert.IsFalse(s.Salt("a", 2, t), "the same hand twice counts once");
            Assert.AreEqual(1, s.Salts(2, t));
            Assert.IsFalse(s.Salt("victim", 2.5f, t), "the swallowed salts nothing from inside");
            Assert.IsFalse(s.Salt("b", 3, t));
            Assert.IsTrue(s.Salt("c", 4, t), "the third hand frees them");
            var late = new SwallowSalts(); late.Begin("v", 0);
            late.Salt("a", 0, t); late.Salt("b", 5, t);
            Assert.IsFalse(late.Salt("c", 12.5f, t), "the first salt out of the 12 s window: two, not three");
            Assert.IsFalse(late.Erased(t.swallowSeconds - .1f, t)); Assert.IsTrue(late.Erased(t.swallowSeconds, t), "10 s inside her (raid 3; the doc's 12) and they are erased");
        }

        [Test] public void The_hunt_falls_on_the_brightest_and_the_river_makes_her_miss()
        {
            var all = new List<UnravellerCandidate> {
                new UnravellerCandidate { id = "tank", threat = 3000, tank = true, victim = true },
                new UnravellerCandidate { id = "rogue", threat = 2500, magic = false, distance = 2 },
                new UnravellerCandidate { id = "mage", threat = 1800, magic = true, distance = 15 },
                new UnravellerCandidate { id = "cleric", threat = 1200, magic = true, distance = 22 } };
            Assert.AreEqual("mage", UnravellerRules.HuntPick(all), "the most magic: never her target, never one who works none");
            all[2].swallowed = true; Assert.AreEqual("cleric", UnravellerRules.HuntPick(all), "nor the swallowed");
            Assert.AreEqual("rogue", UnravellerRules.SwallowPick(all), "she swallows the nearest who is not her target");
            var t = T;
            Assert.IsTrue(UnravellerRules.HuntMissed(true)); Assert.IsFalse(UnravellerRules.HuntMissed(false));
            Assert.AreEqual(0, UnravellerRules.HuntDamage(1000, 0, true, true, t), "the hunted in the river is not struck");
            Assert.AreEqual(800, UnravellerRules.HuntDamage(1000, 0, true, false, t), "on the bank: 80%");
            Assert.AreEqual(800, UnravellerRules.HuntDamage(1000, 4.9f, false, true, t), "one who stood with them and did not move");
            Assert.AreEqual(0, UnravellerRules.HuntDamage(1000, 5.1f, false, false, t), "out of the 5 m");
            Assert.AreEqual(1000, EncounterEnemy.CapBlow(1000, 1000, true, true), "the hunt's blow passes the 60% cap");
        }

        [Test] public void The_Unravelling_grows_each_pulse_and_the_feast_comes_at_nine_minutes()
        {
            var t = T;
            Assert.AreEqual(.012f, UnravellerRules.PulseShare(0, t), 1e-5f, "1.2% at first (tune-3: 2% killed the fast pulls; the doc said 1.5%)");
            Assert.AreEqual(.036f, UnravellerRules.PulseShare(60, t), 1e-5f, "about 3.6% three minutes in (sixty pulses)");
            Assert.AreEqual(540, UnravellerRules.EnrageLeft(0, t)); Assert.AreEqual(40, UnravellerRules.EnrageLeft(500, t), 1e-4f); Assert.AreEqual(0, UnravellerRules.EnrageLeft(600, t));
            Assert.IsFalse(UnravellerRules.Feast(539.9f, t)); Assert.IsTrue(UnravellerRules.Feast(540, t));
        }

        [Test] public void The_void_wrap_is_a_thin_edge_under_the_bodys_height_and_a_half_on_any_body()
        {
            var host = new GameObject("Wrapped body"); host.transform.localScale = Vector3.one * 2.6f;   // her actor's scale
            try
            {
                var w = VoidWrap.Wrap(host.transform, UnravellerBody.WrapHeight, 3.5f, -1, 5);
                Assert.AreEqual(1, w.transform.lossyScale.y, 1e-3f, "built in the world's metres, her scale undone");
                Assert.That(w.ShardCount, Is.InRange(10, 14), "a few jagged shards, not a forest (raid 2)");
                Assert.IsFalse(w.transform.Find("Shards").gameObject.activeSelf, "not out of the ground all the time (raid 3: every 5-7 s)");
                Assert.That(VoidWrap.Rise + VoidWrap.Stand + VoidWrap.Sink, Is.InRange(1f, 1.5f), "an eruption of a second or so");
                Assert.AreEqual(5, VoidWrap.EruptMin); Assert.AreEqual(7, VoidWrap.EruptMax);
                // Her crown (raid 4: "make it smaller and blink on and off randomly"): its own size, shown briefly, gone longer.
                Assert.LessOrEqual(UnravellerBody.CrownScale, 1.01f, "a third of the first crown's 3x");
                Assert.That(UnravellerBody.CrownOn.x, Is.InRange(.5f, 1f)); Assert.That(UnravellerBody.CrownOn.y, Is.InRange(1f, 1.6f), "on 0.6-1.5 s");
                Assert.That(UnravellerBody.CrownOff.x, Is.InRange(1.9f, 2.1f)); Assert.That(UnravellerBody.CrownOff.y, Is.InRange(4.9f, 5.1f), "off 2-5 s");
                Assert.That(UnravellerBody.CrownFade, Is.InRange(.05f, .3f), "a quick fade, not a pop");
                w.Erupt(true);
                Assert.IsTrue(w.transform.Find("Shards").gameObject.activeSelf, "erupting"); Assert.AreEqual(1, w.Eruptions);
                var rs = w.GetComponentsInChildren<Renderer>(true).Where(r => r is MeshRenderer).ToList();
                Assert.AreEqual(2 * w.ShardCount, rs.Count, "each shard a void core and an aether rim: cheap to draw");
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                Assert.LessOrEqual(b.max.y - (host.transform.position.y - 1), UnravellerBody.WrapLimit, "nothing of the wrap over 1.5 times her body (raid 2: it hid her and the view)");
                Assert.Greater(b.size.y, 1, "but there");
                Assert.Less(VoidWrap.Void.a, .6f, "the void translucent: the body shows through");
                var mid = new Vector2(host.transform.position.x, host.transform.position.z);
                Assert.IsTrue(w.GetComponentsInChildren<Transform>().Where(x => x.name == "Shard").All(x => Vector2.Distance(new Vector2(x.position.x, x.position.z), mid) > 1.5f), "round her edge, none in the middle in front of her");
                Assert.Less(b.min.y, host.transform.position.y - .5f, "from the ground (a metre under her origin)");
                Assert.IsTrue(rs.All(r => r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off), "no shadows from the wrap");
                Assert.IsNotNull(w.GetComponentInChildren<ParticleSystem>(), "the ash where she moves");
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test] public void The_raids_stewpot_is_cooked_feeds_all_ten_for_an_hour_and_stays_in_the_brief()
        {
            // Raid 6, Chris: "need to wait or build a raid buff food".
            var items = ProfessionDataTests.Items(); var pot = items.Get(EncounterSession.RaidFeastItem);
            Assert.IsNotNull(pot, "the Drain Reach stewpot (Items/lowtowns.json)"); StringAssert.StartsWith("GAME-ONLY", pot.canonStatus);
            Assert.IsTrue(BossBuffs.IsRaidFeast(pot)); Assert.IsFalse(BossBuffs.IsFeast(pot), "not a party's feast: a dungeon's buff-up never takes it");
            var recipe = ProfessionDataTests.Db(items).RecipesFor("cooking").FirstOrDefault(r => r.output == pot.id);
            Assert.IsNotNull(recipe, "a cook makes it (recipe.reach_stewpot)"); Assert.AreEqual("fire", recipe.station);
            Assert.AreEqual(3600, pot.buff.seconds, "an hour");
            // All ten eat (a party's feast feeds five).
            var bodies = new List<Crulanda.Combat.StatusEffectRuntime>();
            for (int i = 0; i < 12; i++) { var st = new Crulanda.Gameplay.StatBlock(); foreach (var s in new[] { Crulanda.Core.StatType.Spirit, Crulanda.Core.StatType.Stamina }) st.SetBase(s, 10); bodies.Add(new Crulanda.Combat.StatusEffectRuntime(st)); }
            Assert.AreEqual(Consumables.RaidSize, Consumables.Feed(pot, bodies, 5), "the raid of ten fed");
            Assert.IsTrue(bodies[9].Has(Consumables.FedId)); Assert.IsFalse(bodies[10].Has(Consumables.FedId), "ten, no more");
            // A little more than a party's best feast, inside the buffs' brief (BossBuffs.MaxEdge, 15%).
            foreach (var d in items.Items.Values.Where(BossBuffs.IsFeast)) { Assert.Greater(pot.buff.stamina, d.buff.stamina, d.id); Assert.GreaterOrEqual(pot.buff.amount, d.buff.amount, d.id); }
            var body = new Crulanda.Gameplay.StatBlock(); body.SetBase(Crulanda.Core.StatType.Stamina, 10); body.SetBase(Crulanda.Core.StatType.Spirit, 10);
            var on = new Crulanda.Combat.StatusEffectRuntime(body); Consumables.Apply(pot, on, 0);
            Assert.That(BossBuffs.Edge(body, on, Crulanda.Core.StatType.Stamina, 20), Is.InRange(.03f, BossBuffs.MaxEdge), "its health share at 20");
            Assert.That(BossBuffs.Edge(body, on, Crulanda.Core.StatType.Spirit, 20), Is.LessThanOrEqualTo(BossBuffs.MaxEdge), "its mend share at 20");
            Assert.IsTrue(FoodModels.Dishes.ContainsKey(pot.id), "a pot on a board in the world (FoodModels)");
        }

        [Test] public void The_ground_spikes_are_warned_and_never_burst_on_a_held_brood()
        {
            var t = T;
            Assert.That(t.spikeWarn, Is.InRange(1.5f, 2.5f), "a crack shown about 2 s before (raid 7: a sign or warning)");
            Assert.That(t.spikeShare, Is.InRange(.25f, .3f), "a solid hit, 25-30%"); Assert.Less(t.spikeShare, EncounterEnemy.MaxBlowShare);
            Assert.AreEqual(5, t.spikeEveryMin); Assert.AreEqual(7, t.spikeEveryMax);
            var held = new List<Vector2> { new Vector2(10, 0) };
            Assert.IsFalse(UnravellerRules.SpikeSpotClear(new Vector2(11, 1), held, t), "not on a held brood (its hold kept)");
            Assert.IsTrue(UnravellerRules.SpikeSpotClear(new Vector2(0, 0), held, t));
            Assert.IsTrue(UnravellerRules.SpikeSpotClear(new Vector2(0, 0), null, t));
        }

        [Test] public void Each_erupting_shard_stands_on_the_floor_at_its_spot()
        {
            // The tuning batch, Chris: "the spikes are launching in the air. not on the ground". Her origin 2.6 m up (her scale), the floor 0.3.
            var host = new GameObject("Wrapped body"); host.transform.position = new Vector3(0, 2.9f, 0); host.transform.localScale = Vector3.one * 2.6f;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.transform.position = new Vector3(0, .3f, 0); floor.transform.localScale = Vector3.one * 4;
            try
            {
                Physics.SyncTransforms();
                var w = VoidWrap.Wrap(host.transform, UnravellerBody.WrapHeight, 3.5f, -1, 5);
                foreach (var full in new[] { true })
                {
                    w.Erupt(full); Physics.SyncTransforms();
                    CheckOnFloor(w, .3f, "round her");
                    w.EruptAt(new List<Vector3> { new Vector3(6, 2, 1), new Vector3(-5, 1, -4) }, 2.2f);
                    foreach (Transform s in w.transform.Find("Shards")) s.localScale = s.localScale;   // (at rest: no frame has run)
                    CheckOnFloor(w, .3f, "at the cracks");
                }
            }
            finally { Object.DestroyImmediate(host); Object.DestroyImmediate(floor); }
        }
        static void CheckOnFloor(VoidWrap w, float floorY, string where)
        {
            foreach (Transform shard in w.transform.Find("Shards"))
            {
                float low = float.MaxValue;
                foreach (var mf in shard.GetComponentsInChildren<MeshFilter>(true)) foreach (var v in mf.sharedMesh.vertices) low = Mathf.Min(low, mf.transform.TransformPoint(v).y);
                Assert.AreEqual(floorY, low, .05f, "a shard's lowest point on the floor (" + where + "), not in the air: " + low);
            }
        }

        [Test] public void A_rotation_goes_round_passing_over_those_who_cannot()
        {
            var r = new RaidRotation(new[] { "a", "b", "c" });
            Assert.AreEqual("a", r.Current(null));
            r.Took("a"); Assert.AreEqual("b", r.Current(null));
            Assert.AreEqual("c", r.Current(id => id != "b"), "b is down or swallowed: c's turn");
            r.Took("c"); Assert.AreEqual("a", r.Current(null), "round again");
            r.Skip(); Assert.AreEqual("b", r.Current(null), "a turn passed on (you away from the keys)");
            Assert.IsNull(r.Current(id => false), "nobody able");
            Assert.IsNull(new RaidRotation(null).Current(null));
        }
    }
}
