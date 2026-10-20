using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Combat;
using Crulanda.Core;
using Crulanda.Encounter;
using Crulanda.Gameplay;

namespace Crulanda.Tests
{
    /// <summary>
    /// Buffing up before the bosses (Chris, 2026-10-15: "we should use food buffs, alchemy buffs, class buffs,etc to beat the bosses";
    /// BossBuffs), over the real content without a scene: what each part drinks and at what tier, a feast feeding five, a standing party
    /// buff for every class (half an hour, 3% of one stat, its icon painted), what the buffs are worth to a sim (about a tenth, never past
    /// 15%), and the sims' packs: bought with their coin, so a richer sim is better buffed, and never sold at the stall.
    /// </summary>
    public class BuffUpRulesTests
    {
        static ItemDatabase items;
        static ItemDatabase Items() { return items ?? (items = ProfessionDataTests.Items()); }
        static IEnumerable<ItemDef> All() { return Items().Items.Values; }
        static StatusEffectRuntime Body(out StatBlock stats)
        {
            stats = new StatBlock();
            foreach (var s in new[] { StatType.Strength, StatType.Agility, StatType.Intellect, StatType.Spirit, StatType.Stamina }) stats.SetBase(s, 10);
            stats.SetBase(StatType.Armor, 100);
            return new StatusEffectRuntime(stats);
        }

        /// <summary>A tank drinks its armour salve (else Bear's-wort), a healer Hearth-ease (else Owl-light), a damage-dealer the tincture of its
        /// main stat; from the item list, the tier of its level (a cordial from 11, a tincture from 4, nothing under).</summary>
        [Test] public void Each_part_drinks_its_own_elixir_at_its_level()
        {
            var tank = SimCompanion.PartyRole.Tank; var healer = SimCompanion.PartyRole.Healer; var dps = SimCompanion.PartyRole.Damage;
            Assert.AreEqual("salve.scree", BossBuffs.PickElixir(tank, StatType.Strength, All()).id, "A tank: the best salve carried.");
            Assert.AreEqual("potion.cordial_bear", BossBuffs.PickElixir(tank, StatType.Strength, All().Where(d => !BossBuffs.IsSalve(d))).id, "No salve: Bear's-wort.");
            Assert.AreEqual("potion.cordial_hearth", BossBuffs.PickElixir(healer, StatType.Intellect, All()).id, "A healer: Hearth-ease.");
            Assert.AreEqual("potion.cordial_owl", BossBuffs.PickElixir(healer, StatType.Intellect, All().Where(d => !BossBuffs.IsElixir(d, StatType.Spirit))).id, "No Hearth-ease: Owl-light.");
            Assert.AreEqual("potion.cordial_ox", BossBuffs.PickElixir(dps, BossBuffs.MainStat("class.warrior"), All()).id);
            Assert.AreEqual("potion.cordial_hare", BossBuffs.PickElixir(dps, BossBuffs.MainStat("class.rogue"), All()).id);
            Assert.AreEqual("potion.cordial_owl", BossBuffs.PickElixir(dps, BossBuffs.MainStat("class.mage"), All()).id);
            Assert.AreEqual("potion.tincture_ox", BossBuffs.PickElixir(dps, StatType.Strength, All(), 6).id, "Level 6: the tincture.");
            Assert.AreEqual("potion.cordial_ox", BossBuffs.PickElixir(dps, StatType.Strength, All(), 11).id, "Level 11: the cordial.");
            Assert.IsNull(BossBuffs.PickElixir(dps, StatType.Strength, All(), 3), "Nothing under the first tier.");
            Assert.AreEqual("salve.tallow", BossBuffs.PickElixir(tank, StatType.Strength, All(), 8).id, "A tank at 8: the tallow salve.");
            Assert.IsNull(BossBuffs.PickElixir(dps, StatType.Strength, new[] { Items().Get("food.cask_stew"), Items().Get("potion.tincture_owl") }), "Nothing of its stat carried: none.");
            // The pack a sim keeps: its dish, its elixir, its draught, of its level's tier.
            var pack = BossBuffs.Pack("class.warrior", 11, Items());
            Assert.IsTrue(pack.Any(p => p.id == "salve.scree" && p.count == 2), "A warrior's salve: " + string.Join(", ", pack));
            Assert.IsTrue(pack.Any(p => p.id == "potion.draught_vigor"), "and its Vigor draught");
            var dish = pack.Select(p => Items().Get(p.id)).FirstOrDefault(BossBuffs.IsDish); Assert.NotNull(dish, "and a dish"); Assert.LessOrEqual(dish.level, 11);
            Assert.AreEqual("potion.draught_nerve", BossBuffs.Pack("class.scoundrel", 11, Items()).Last().id, "A Scoundrel's draught fills Nerve.");
            Assert.AreEqual(SimCompanion.PartyRole.Healer, BossBuffs.NominalRole("class.shaman")); Assert.AreEqual(SimCompanion.PartyRole.Tank, BossBuffs.NominalRole("class.paladin"));
        }

        /// <summary>A feast feeds five of a party of six (Consumables.PartySize), each well fed for half an hour; a sim picks a feast before a dish.</summary>
        [Test] public void A_feast_feeds_five_and_comes_before_a_dish()
        {
            var feast = Items().Get("food.passfire_feast"); Assert.IsTrue(BossBuffs.IsFeast(feast)); Assert.IsFalse(BossBuffs.IsDish(feast));
            var bodies = new List<StatusEffectRuntime>(); for (int i = 0; i < 6; i++) bodies.Add(Body(out _));
            Assert.AreEqual(5, Consumables.Feed(feast, bodies, 10), "Five fed.");
            Assert.AreEqual(5, bodies.Count(b => b.Has(Consumables.FedId))); Assert.IsFalse(bodies[5].Has(Consumables.FedId), "The sixth eats its own.");
            Assert.AreEqual(1800, bodies[0].Remaining(Consumables.FedId, 10), 1e-3f);
            var carried = new[] { Items().Get("food.bear_broth"), feast, Items().Get("potion.tincture_ox") };
            Assert.AreSame(feast, BossBuffs.PickFeast(carried)); Assert.AreEqual("food.bear_broth", BossBuffs.PickDish(carried).id);
            Assert.AreEqual("food.harvest_feast", BossBuffs.FeastFor(8, Items()).id, "Level 8: the harvest feast.");
            Assert.IsNull(BossBuffs.FeastFor(5, Items()), "None under the first.");
            var fed = Body(out _); Consumables.Apply(feast, fed, 0);
            Assert.IsFalse(BossBuffs.Wants(fed, Consumables.FedId, 100), "Fed with most of the half hour left: kept.");
            Assert.IsTrue(BossBuffs.Wants(fed, Consumables.FedId, 1800 - BossBuffs.Refresh + 1), "Under ten minutes left: eaten again.");
        }

        /// <summary>Every class has a standing party buff in its kit: its own id and name, half an hour, +3% of one stat, named on the buff
        /// row, its icon painted (tools/art/make_icons.py, Icons/ability/buff-class-*).</summary>
        [Test] public void Every_class_has_a_small_party_buff_with_an_icon()
        {
            var ids = new HashSet<string>(); var names = new HashSet<string>();
            foreach (var c in ClassBuffs.ClassIds)
            {
                var b = ClassBuffs.For(c); Assert.NotNull(b, c + " has a party buff");
                Assert.AreEqual(c, b.classId); Assert.IsTrue(ids.Add(b.id), "its own id: " + b.id); Assert.IsTrue(names.Add(b.name), "its own name: " + b.name);
                StringAssert.StartsWith("buff.class.", b.id); Assert.IsFalse(string.IsNullOrWhiteSpace(b.name));
                var def = b.Definition();
                Assert.AreEqual(1800, def.duration, b.name + ": half an hour"); Assert.AreEqual(1, def.incomingDamageMultiplier);
                Assert.AreEqual(1, def.modifiers.Length); Assert.AreEqual(ModifierOp.PercentAdd, def.modifiers[0].operation); Assert.AreEqual(.03f, def.modifiers[0].value, 1e-5f, b.name + ": 3%");
                var body = Body(out var stats); float before = stats.Get(b.stat);
                Assert.IsTrue(body.Apply(def, 0)); Assert.AreEqual(before * 1.03f, stats.Get(b.stat), 1e-3f, b.name + " raises " + b.stat + " by 3%");
                Assert.AreEqual(b.name, ClassBuffs.StatusName(b.id)); Assert.AreSame(b, ClassBuffs.ById(b.id));
                var icon = Path.Combine(Application.dataPath, "Crulanda", "Resources", "Icons", "ability", b.id.Replace('.', '-') + ".png");
                Assert.IsTrue(File.Exists(icon), b.name + "'s icon: " + icon);
                Assert.IsTrue(File.Exists(icon + ".meta"), "with its .meta");
            }
            Assert.AreSame(PaladinKit.Standing, ClassBuffs.For("class.paladin"), "The kit holds it.");
            Assert.IsNull(ClassBuffs.For("class.nobody")); Assert.IsNull(ClassBuffs.StatusName(Consumables.FedId));
        }

        /// <summary>What the buffs are worth to a sim of 11: its level's dish and cordial on its main stat some 7%, a class buff 3% more, its dish's
        /// Stamina some 5% of its health; never past BossBuffs.MaxEdge; nothing unbuffed.</summary>
        [Test] public void The_buffs_are_worth_about_a_tenth_to_a_sim()
        {
            var body = Body(out var stats); float now = 0;
            Assert.AreEqual(0, BossBuffs.Edge(stats, body, StatType.Strength, 11), 1e-5f, "Unbuffed: nothing.");
            Consumables.Apply(Items().Get("food.smoked_loin"), body, now); Consumables.Apply(Items().Get("potion.cordial_ox"), body, now);
            float consumables = BossBuffs.Edge(stats, body, StatType.Strength, 11);
            Assert.That(consumables, Is.InRange(.05f, .09f), "the dish and the cordial: " + consumables);
            body.Apply(WarriorKit.Standing.Definition(), now);
            float all = BossBuffs.Edge(stats, body, StatType.Strength, 11);
            Assert.AreEqual(consumables + .03f, all, 1e-3f, "and the Warrior's 3%: " + all);
            Assert.That(all, Is.InRange(.08f, BossBuffs.MaxEdge));
            float health = BossBuffs.Edge(stats, body, StatType.Stamina, 11);
            Assert.That(health, Is.InRange(.03f, .08f), "the dish's Stamina as health: " + health);
            body.Apply(new StatusEffectDefinition { id = "test.huge", duration = 10, modifiers = new[] { new StatusModifierDefinition { stat = StatType.Strength, operation = ModifierOp.Flat, value = 500 } } }, now);
            Assert.AreEqual(BossBuffs.MaxEdge, BossBuffs.Edge(stats, body, StatType.Strength, 11), 1e-5f, "Capped.");
        }

        /// <summary>The sims' packs (SimEconomy.Stock): a poor sim buys nothing, one of middling coin its pack at a fair price (the elixir first),
        /// a rich one a feast on top; the stall never takes the pack, only what is over it.</summary>
        [Test] public void A_richer_sim_is_better_buffed_and_keeps_its_pack()
        {
            SimAdventurer Sim(int coin) { return new SimAdventurer { id = "sim.t", name = "Test", classId = "class.warrior", level = 11, coin = coin }; }
            var pack = BossBuffs.Pack("class.warrior", 11, Items()); int cost = pack.Sum(p => p.count * BossBuffs.FairPrice(Items().Get(p.id)));
            var poor = Sim(0); Assert.AreEqual(0, SimEconomy.Stock(poor, Items())); Assert.AreEqual(0, SimEconomy.GoodsCount(poor), "No coin, no pack.");
            var some = Sim(BossBuffs.FairPrice(Items().Get("salve.scree")) * 2); SimEconomy.Stock(some, Items());
            Assert.AreEqual(2, SimEconomy.Count(some, "salve.scree"), "The elixir first."); Assert.AreEqual(0, some.coin);
            var middling = Sim(cost); Assert.AreEqual(cost, SimEconomy.Stock(middling, Items()));
            foreach (var p in pack) Assert.AreEqual(p.count, SimEconomy.Count(middling, p.id), p.id);
            var feast = BossBuffs.FeastFor(11, Items()); Assert.AreEqual(0, SimEconomy.Count(middling, feast.id), "No feast without coin to spare.");
            var rich = Sim(cost + BossBuffs.FairPrice(feast) * BossBuffs.FeastPurse); SimEconomy.Stock(rich, Items());
            Assert.AreEqual(1, SimEconomy.Count(rich, feast.id), "The rich carry a feast.");
            Assert.AreEqual(0, SimEconomy.Stock(rich, Items()), "A full pack buys nothing more.");
            SimEconomy.Add(rich, "salve.scree", 3); SimEconomy.Add(rich, "mat.copper_ore", 4);
            int sold = SimEconomy.SellAll(rich, Items());
            Assert.AreEqual(3 * Items().Get("salve.scree").value + 4 * Items().Get("mat.copper_ore").value, sold, "Only what is over the pack is sold.");
            foreach (var p in pack) Assert.AreEqual(p.count, SimEconomy.Count(rich, p.id), p.id + " kept");
            Assert.AreEqual(1, SimEconomy.Count(rich, feast.id), "The feast kept.");
            Assert.AreEqual(0, SimEconomy.SellableCount(rich, Items()), "Nothing left to take to the stall.");
        }
    }
}
