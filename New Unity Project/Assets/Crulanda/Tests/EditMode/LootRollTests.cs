using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Rolling the loot database (loot DESIGN.md 3.4 and 3.6, step A3, against the drafted files; nothing in the game rolls it
    /// until step L2): a seed gives the same body; named items stay inside their caps; signature lists give what you do not own
    /// and replace the old 100% trophies; an elite never leaves only junk; rates match the table; luck lifts only lucky groups
    /// and is capped; elites sometimes drop a generated epic; pity makes an epic certain and owning it makes it rarer; set bonuses
    /// and effects total, cap and clear. Pure logic: no scene, no session and no save.
    /// </summary>
    public class LootRollTests
    {
        static ItemDatabase items; static LootDatabase loot; static List<ZoneDefinition> zones;
        [OneTimeSetUp] public void Load() { items = LootTestData.Items(); loot = LootTestData.Loot(items); zones = LootTestData.Zones(); }

        /// <summary>A camp mob of the named camp (by mob name, the first such camp) at the camp's top level; the elite unless elite is false.</summary>
        static LootContext Mob(string zone, string mob, bool elite = true, int level = 0)
        {
            var z = zones.First(x => x.id == "zone." + zone); int c = Array.FindIndex(z.camps, x => x != null && x.mob == mob);
            Assert.GreaterOrEqual(c, 0, mob + " has a camp in " + zone + ".");
            var camp = z.camps[c];
            return LootContext.From("mob." + camp.tag + "." + zone + "." + c + "." + (elite && camp.elite ? 0 : 1), z.camps, level > 0 ? level : camp.levelMax, elite && camp.elite);
        }
        static bool Named(string id) { return loot.Meta(id) != null; }
        static bool Epic(string id) { var d = items.Get(id); return d != null && d.quality == 4; }
        static string Key(List<LootDrop> drops) { return string.Join(",", drops.Select(d => d.ToString())); }
        /// <summary>How often each item drops over n seeded rolls (seeds 0..n-1).</summary>
        static Dictionary<string, int> Rates(LootContext c, int n, Func<string, bool> owned = null, float luck = 0)
        {
            var counts = new Dictionary<string, int>();
            for (int seed = 0; seed < n; seed++)
                foreach (var d in loot.Roll(c, items, owned, luck, null, new System.Random(seed))) { counts.TryGetValue(d.item, out int k); counts[d.item] = k + 1; }
            return counts;
        }
        static float Rate(Dictionary<string, int> counts, string item, int n) { return counts.TryGetValue(item, out int k) ? (float)k / n : 0; }
        static float Rate(Dictionary<string, int> counts, IEnumerable<string> any, int n) { return any.Sum(i => Rate(counts, i, n)); }
        static void Near(float expected, float actual, float tolerance, string what) { Assert.That(actual, Is.InRange(expected * (1 - tolerance), expected * (1 + tolerance)), what + ": expected about " + expected + ", got " + actual + "."); }
        static void Wear(EncounterProgress p, params string[] ids)
        {
            Inventory.Ensure(p); for (int s = 0; s < p.equipment.Count; s++) p.equipment[s] = new ItemStack();
            foreach (var id in ids) p.equipment[ItemDatabase.SlotIndex(items.Get(id).slot)] = new ItemStack { item = id, count = 1 };
        }

        [Test] public void Roll_is_deterministic_for_a_seed_and_respects_the_caps()
        {
            var caddock = Mob("oakhaven", "Caddock, the Bandit King");
            Assert.AreEqual("Caddock, the Bandit King", caddock.mob); Assert.AreEqual("oakhaven", caddock.zone); Assert.AreEqual("banditking", caddock.tag); Assert.IsTrue(caddock.elite);
            var wolf = Mob("oakhaven", "Grey wolf", false);
            for (int seed = 0; seed < 50; seed++)
            {
                Assert.AreEqual(Key(loot.Roll(caddock, items, null, 0, null, new System.Random(seed))), Key(loot.Roll(caddock, items, null, 0, null, new System.Random(seed))), "Same seed, same body.");
                Assert.AreEqual(Key(loot.Roll(wolf, items, null, .2f, null, new System.Random(seed))), Key(loot.Roll(wolf, items, null, .2f, null, new System.Random(seed))));
            }
            // Every camp of every zone, at full luck: one named item at most from a normal mob, two from an elite; epics do not count.
            foreach (var z in zones)
                for (int c = 0; c < z.camps.Length; c++)
                    foreach (bool elite in z.camps[c].elite ? new[] { true, false } : new[] { false })
                    {
                        var ctx = LootContext.From("mob." + z.camps[c].tag + "." + z.id.Replace("zone.", "") + "." + c + "." + (elite ? 0 : 1), z.camps, z.camps[c].levelMax, elite);
                        for (int seed = 0; seed < 300; seed++)
                        {
                            var drops = loot.Roll(ctx, items, null, 1, null, new System.Random(seed));
                            int named = drops.Count(d => Named(d.item) && !Epic(d.item));
                            Assert.LessOrEqual(named, elite ? LootDatabase.EliteCap : LootDatabase.NormalCap, z.camps[c].mob + " gave " + Key(drops) + ".");
                            Assert.AreEqual(drops.Count, drops.Select(d => d.item).Distinct().Count(), "No item twice on one body: " + Key(drops));
                        }
                    }
        }

        [Test] public void A_signature_kill_gives_an_unowned_piece_until_the_list_is_owned()
        {
            // Caddock's list pays every kill: three kills, three different pieces.
            var caddock = Mob("oakhaven", "Caddock, the Bandit King");
            var list = new[] { "item.tin_crown", "loot.oak.due_cleaver", "loot.oak.due_coat" }; var owned = new HashSet<string>();
            for (int kill = 0; kill < 3; kill++)
            {
                var got = loot.Roll(caddock, items, owned.Contains, 0, null, new System.Random(100 + kill)).Select(d => d.item).Where(list.Contains).ToList();
                Assert.AreEqual(1, got.Count, "Kill " + (kill + 1) + " gives one piece of the list.");
                Assert.IsFalse(owned.Contains(got[0]), "Kill " + (kill + 1) + " gives a piece not yet owned: " + got[0] + ".");
                owned.Add(got[0]);
            }
            CollectionAssert.AreEquivalent(list, owned, "Three kills, the whole list.");
            // Owning them all, the list still pays: any one of them at 35%.
            int n = 20000; var counts = Rates(caddock, n, owned.Contains);
            Near(LootDatabase.SignatureOwnedShare, Rate(counts, list, n), .08f, "Caddock's list once all is owned");
            // Old Whitefoot's list pays half the time, and never the mantle you hold while the fang is still out there.
            var whitefoot = Mob("oakhaven", "Old Whitefoot"); Assert.AreEqual("wolf", whitefoot.tag);
            var mantle = new HashSet<string> { "loot.oak.whitefoot_mantle" };
            counts = Rates(whitefoot, n, mantle.Contains);
            Assert.AreEqual(0, Rate(counts, "loot.oak.whitefoot_mantle", n), "The owned mantle does not drop while the fang is unowned.");
            Near(.5f, Rate(counts, "loot.oak.whitefoot_fang", n), .06f, "The fang");
        }

        [Test] public void Old_guaranteed_entries_do_not_double_drop()
        {
            foreach (var (zone, mob, trophy) in new[] { ("oakhaven", "Caddock, the Bandit King", "item.tin_crown"), ("verdant", "Greyheart", "item.greyheart_stave"),
                ("verdant", "Old Ninebranch", "item.ninebranch_tine"), ("verdant", "The Hollow Root-Warden", "item.wardens_crown") })
            {
                var c = Mob(zone, mob);
                Assert.IsTrue(items.RollLoot(c.tag, c.level, true, new System.Random(1)).Any(d => d.item == trophy), "items.json still drops " + trophy + " every time (it is not edited).");
                var owned = new HashSet<string> { trophy };
                for (int seed = 0; seed < 400; seed++)
                {
                    var drops = loot.Roll(c, items, owned.Contains, 0, null, new System.Random(seed));
                    Assert.IsFalse(drops.Any(d => d.item == trophy), mob + ": the owned trophy stays on the list, not on every body, while other pieces are unowned (" + Key(drops) + ").");
                }
                for (int seed = 0; seed < 400; seed++) Assert.LessOrEqual(loot.Roll(c, items, null, 0, null, new System.Random(seed)).Count(d => d.item == trophy), 1, mob + " drops the trophy once at most.");
            }
        }

        [Test] public void A_boss_never_drops_only_junk()
        {
            foreach (var z in zones)
                for (int c = 0; c < z.camps.Length; c++)
                {
                    if (!z.camps[c].elite) continue;
                    var ctx = LootContext.From("mob." + z.camps[c].tag + "." + z.id.Replace("zone.", "") + "." + c + ".0", z.camps, z.camps[c].levelMax, true);
                    var all = loot.Matching(ctx).SelectMany(d => d.groups).SelectMany(g => g.pick).Select(k => k.item).ToList();
                    Func<string, bool> ownsAll = all.Contains;   // everything owned: the list pays rarely, so the rescue piece shows
                    for (int seed = 0; seed < 500; seed++)
                    {
                        var drops = loot.Roll(ctx, items, ownsAll, 0, null, new System.Random(seed));
                        Assert.IsTrue(drops.Any(d => items.Get(d.item) != null && items.Get(d.item).kind == "gear"), z.camps[c].mob + " left only junk: " + Key(drops));
                    }
                }
        }

        [Test] public void Drop_rates_match_the_table()
        {
            const int n = 200000;
            // A zone table (3%), and a world drop (0.25%) that loses the kills the zone table and the Harrow knot already took (the cap).
            var counts = Rates(Mob("oakhaven", "Grey wolf", false, 2), n);
            Near(.03f, Rate(counts, "loot.oak.hollin_pitchfork", n), .1f, "Oakhaven wolves' pitchfork (3%)");
            Near(.004f, Rate(counts, "loot.oak.harrow_luck_knot", n), .25f, "The Harrow luck-knot (0.4%)");
            Near(.0025f * (1 - .03f - .004f), Rate(counts, "loot.world.golden_cask_tankard", n), .2f, "The Golden Cask's tankard (0.25%)");
            // An outdoor elite: its list half the time, its rare table 15%.
            counts = Rates(Mob("oakhaven", "Old Whitefoot"), n);
            Near(.5f, Rate(counts, new[] { "loot.oak.whitefoot_mantle", "loot.oak.whitefoot_fang" }, n), .03f, "Old Whitefoot's list (50%)");
            Near(.15f, Rate(counts, "loot.oak.den_mothers_wraps", n), .06f, "Old Whitefoot's rare table (15%)");
            // Epics: 4% outdoors, 10% at a dungeon's end; both on top of the list.
            counts = Rates(Mob("khaven", "The Pale Reckoner"), n);
            Near(.04f, Rate(counts, "loot.kha.the_unpaid_debt", n), .08f, "The Pale Reckoner's epic (4%)");
            counts = Rates(Mob("oakhaven", "Caddock, the Bandit King"), n);
            Near(.10f, Rate(counts, "loot.oak.broken_oath_sabre", n), .05f, "Caddock's epic (10%)");
            Near(1f, Rate(counts, new[] { "item.tin_crown", "loot.oak.due_cleaver", "loot.oak.due_coat" }, n), .001f, "Caddock's list (every kill)");
            // A zone rare on a slow camp (5%) and a world drop off an elite (2%).
            counts = Rates(Mob("verdant", "Withered Keeper", false, 13), n / 4);
            Assert.AreEqual(0, Rate(counts, "loot.ver.vigil_root_grips", n / 4), "The Greying's keepers are not the Deep's.");
            var deep = zones.First(z => z.id == "zone.verdant"); int gallery = Array.FindIndex(deep.camps, x => x.tag == "deepwithered");
            counts = Rates(LootContext.From("mob.deepwithered.verdant." + gallery + ".0", deep.camps, 13, false), n / 4);
            Near(.05f, Rate(counts, "loot.ver.vigil_root_grips", n / 4), .1f, "Vigil Root-Grips in the Deep (5%)");
            counts = Rates(Mob("ashrim", "The Ash-Deacon"), n / 4);
            Near(.02f, Rate(counts, "loot.world.hares_foot_torc", n / 4), .2f, "A world drop off an elite (2%)");
        }

        [Test] public void Luck_raises_only_lucky_groups_and_is_capped()
        {
            const int n = 100000; var whitefoot = Mob("oakhaven", "Old Whitefoot"); var list = new[] { "loot.oak.whitefoot_mantle", "loot.oak.whitefoot_fang" };
            var plain = Rates(whitefoot, n); var lucky = Rates(whitefoot, n, null, .1f); var capped = Rates(whitefoot, n, null, 5);
            Near(.15f, Rate(plain, "loot.oak.den_mothers_wraps", n), .05f, "No luck");
            Near(.165f, Rate(lucky, "loot.oak.den_mothers_wraps", n), .05f, "+10% luck");
            Near(.195f, Rate(capped, "loot.oak.den_mothers_wraps", n), .05f, "Luck is capped at +30%");
            Near(Rate(plain, list, n), Rate(capped, list, n), .02f, "Luck does not touch the signature list");
            Assert.AreEqual(Key(loot.Roll(whitefoot, items, null, -1, null, new System.Random(3))), Key(loot.Roll(whitefoot, items, null, 0, null, new System.Random(3))), "Negative luck counts as none.");
        }

        [Test] public void Elites_sometimes_drop_a_generated_epic()
        {
            const int n = 100000; var caddock = Mob("oakhaven", "Caddock, the Bandit King"); int epics = 0;
            for (int seed = 0; seed < n; seed++)
                foreach (var d in loot.Roll(caddock, items, null, 0, null, new System.Random(seed)))
                {
                    if (!d.item.StartsWith("gen.", StringComparison.Ordinal) || items.Get(d.item).quality != 4) continue;
                    epics++; var g = items.Get(d.item); var parts = d.item.Split('.');
                    Assert.AreEqual("5", parts[2], "At the elite's level."); Assert.AreEqual("epic", ItemDatabase.QualityNames[g.quality].ToLowerInvariant());
                    Assert.NotNull(items.Get(string.Join(".", parts[0], parts[1], parts[2], "3", parts[4])), "The rare it was, same slot, level and seed.");
                }
            // 70% gear, 35% of it rare, 6% of those epic: about 1.5% of kills.
            Near(.7f * .35f * .06f, (float)epics / n, .2f, "Generated epics from Caddock");
            var wolf = Mob("oakhaven", "Grey wolf", false);
            for (int seed = 0; seed < 20000; seed++)
                Assert.IsFalse(loot.Roll(wolf, items, null, 0, null, new System.Random(seed)).Any(d => d.item.StartsWith("gen.", StringComparison.Ordinal) && items.Get(d.item).quality == 4), "Normal mobs never drop a generated epic.");
        }

        [Test] public void The_epic_is_certain_by_its_pity_count()
        {
            foreach (var (zone, mob, epic, count) in new[] { ("oakhaven", "Caddock, the Bandit King", "loot.oak.broken_oath_sabre", 10), ("peaks", "Old Scree-Tusk", "loot.pea.rockfall", 25) })
            {
                var c = Mob(zone, mob); var pity = new List<LootLuck>(); int dry = 0, longest = 0, found = 0;
                for (int seed = 0; seed < 3000; seed++)
                {
                    bool got = loot.Roll(c, items, null, 0, pity, new System.Random(seed)).Any(d => d.item == epic);
                    if (got) { found++; dry = 0; } else longest = Math.Max(longest, ++dry);
                }
                Assert.Less(longest, count, mob + ": never " + count + " kills in a row without the epic (longest dry run " + longest + ").");
                Assert.AreEqual(1, pity.Count, "One counter for " + mob + "'s epic.");
                Assert.AreEqual(3000, pity[0].kills); Assert.AreEqual(dry, pity[0].dry, "The counter holds the current dry run.");
                Assert.Greater(found, 0);
            }
        }

        [Test] public void An_owned_epic_drops_a_quarter_as_often()
        {
            const int n = 100000; var caddock = Mob("oakhaven", "Caddock, the Bandit King"); var owned = new HashSet<string> { "loot.oak.broken_oath_sabre" };
            Near(.10f * LootDatabase.EpicOwnedShare, Rate(Rates(caddock, n, owned.Contains), "loot.oak.broken_oath_sabre", n), .1f, "Caddock's sabre once owned");
            var pity = new List<LootLuck>();
            for (int seed = 0; seed < 200; seed++) loot.Roll(caddock, items, owned.Contains, 0, pity, new System.Random(seed));
            Assert.AreEqual(0, pity.Count, "No pity for an epic you already own.");
        }

        [Test] public void Set_bonuses_switch_on_and_off_with_pieces_worn()
        {
            var p = EncounterSession.FreshProgress(); var source = new object();
            float Bonus(GearEffectTotals t, StatType s) { return t.modifiers.Where(m => m.Stat == s && m.Op == ModifierOp.Flat).Sum(m => m.Value); }
            Wear(p, "loot.oak.due_cleaver");
            var t = GearEffects.Compute(p, items, loot, source);
            Assert.AreEqual(0, t.modifiers.Count, "One piece: no bonus.");
            Inventory.Add(p, items, "loot.oak.due_coat", 1);
            Assert.AreEqual(0, GearEffects.Compute(p, items, loot, source).modifiers.Count, "A piece in the bags does not count.");
            Wear(p, "loot.oak.due_cleaver", "loot.oak.due_coat");
            t = GearEffects.Compute(p, items, loot, source);
            Assert.AreEqual(30, Bonus(t, StatType.MaxHealth), "Two pieces: +30 health."); Assert.AreEqual(0, Bonus(t, StatType.AttackPower));
            StringAssert.Contains("The Deserter King's Due (2/4)", loot.TooltipLines("loot.oak.due_coat", p));
            Wear(p, "loot.oak.due_cleaver", "loot.oak.due_coat", "item.tin_crown");
            t = GearEffects.Compute(p, items, loot, source);
            Assert.AreEqual(30, Bonus(t, StatType.MaxHealth)); Assert.AreEqual(6, Bonus(t, StatType.AttackPower), "Three pieces: +6 attack power as well.");
            Assert.IsTrue(t.modifiers.All(m => m.Source == source), "Every modifier comes from the one source, so they clear together.");
            Wear(p, "loot.oak.due_coat", "item.tin_crown");
            t = GearEffects.Compute(p, items, loot, source);
            Assert.AreEqual(30, Bonus(t, StatType.MaxHealth)); Assert.AreEqual(0, Bonus(t, StatType.AttackPower), "Back to two: the three-piece bonus is off.");
            Wear(p, "loot.ver.vigil_bark_plate", "loot.ver.vigil_root_grips", "loot.ver.vigil_sap_treads", "item.wardens_crown");
            t = GearEffects.Compute(p, items, loot, source);
            Assert.AreEqual(60, Bonus(t, StatType.MaxHealth)); Assert.AreEqual(12, Bonus(t, StatType.AttackPower)); Assert.AreEqual(25, t.onKillHeal, "Four Vigil pieces: each kill restores 25 health.");
            Wear(p);
            Assert.AreEqual(0, GearEffects.Compute(p, items, loot, source).modifiers.Count, "Nothing worn, nothing on.");
        }

        [Test] public void Gear_effects_total_and_clear()
        {
            var p = EncounterSession.FreshProgress(); var source = new object();
            Wear(p, "loot.kha.the_unpaid_debt", "loot.pea.rockfall");
            var t = GearEffects.Compute(p, items, loot, source);
            Assert.IsTrue(t.modifiers.Any(m => m.Stat == StatType.MaxHealth && m.Op == ModifierOp.PercentAdd && Math.Abs(m.Value - .05f) < 1e-5f), "+5% maximum health.");
            Assert.IsTrue(t.modifiers.Any(m => m.Stat == StatType.Armor && m.Op == ModifierOp.PercentAdd && Math.Abs(m.Value - .1f) < 1e-5f), "+10% armour.");
            Wear(p, "loot.ash.ember_that_remembers", "loot.world.pilgrims_last_mile", "loot.oak.broken_oath_sabre", "loot.world.assessors_honest_scale");
            t = GearEffects.Compute(p, items, loot, source);
            Assert.AreEqual(16, t.restRegen, "10 + 6 health per rest tick."); Assert.AreEqual(12, t.onKillHeal); Assert.AreEqual(.2f, t.coins, 1e-5f);
            Assert.AreEqual(0, t.modifiers.Count);
            Wear(p, "loot.pea.signet_of_the_toll_road", "loot.world.assessors_honest_scale");
            Assert.AreEqual(GearEffects.CoinsCap, GearEffects.Compute(p, items, loot, source).coins, 1e-5f, "15% + 20% coins is capped at 30%.");
            Wear(p, "loot.world.hares_foot_torc");
            Assert.AreEqual(.15f, GearEffects.Compute(p, items, loot, source).luck, 1e-5f);
            Wear(p, "loot.world.golden_cask_tankard", "loot.ver.the_last_green_leaf");
            t = GearEffects.Compute(p, items, loot, source);
            Assert.AreEqual(5, t.onKillPower); Assert.IsTrue(t.modifiers.Any(m => m.Stat == StatType.MaxPower && m.Op == ModifierOp.Flat && m.Value == 20), "+20 maximum resource.");
            Wear(p);
            t = GearEffects.Compute(p, items, loot, source);
            Assert.AreEqual(0, t.modifiers.Count); Assert.AreEqual(0, t.onKillHeal + t.onKillPower + t.restRegen); Assert.AreEqual(0, t.coins + t.luck, "Taking it all off clears every effect.");
        }
    }
}
