using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Crulanda.Combat;
using Crulanda.Core;
using Crulanda.Encounter;
using Crulanda.Gameplay;

namespace Crulanda.Tests
{
    /// <summary>
    /// Phase 8.6 step 4 (Docs/PHASE8_6_LIVING_WORLD.md 3b; decision 10): consumables with a lasting good, over the real content and a
    /// status runtime without a scene. Every dish from Cooking 20 leaves you well fed (Stamina and one stat, half an hour, more by the
    /// dish's level), never eaten in a fight; a feast feeds the party of five; a tincture or cordial raises one stat and a salve armour,
    /// each kind one at a time and the three together; a draught fills each class's own pool and is refused by the others; a cure lifts
    /// poisons, bleeds and the grey. None of it is saved (it rides the status effects).
    /// </summary>
    public class ConsumableBuffTests
    {
        static ItemDatabase items;
        static ItemDatabase Items() { return items ?? (items = ProfessionDataTests.Items()); }
        static StatusEffectRuntime Body(out StatBlock stats)
        {
            stats = new StatBlock();
            foreach (var s in new[] { StatType.Strength, StatType.Agility, StatType.Intellect, StatType.Spirit, StatType.Stamina }) stats.SetBase(s, 10);
            stats.SetBase(StatType.Armor, 100);
            return new StatusEffectRuntime(stats);
        }

        [Test] public void A_dish_from_cooking_twenty_leaves_you_well_fed_for_half_an_hour()
        {
            var skewer = Items().Get("food.wolf_skewer"); Assert.IsTrue(Consumables.HasBuff(skewer));
            var def = Consumables.Definition(skewer);
            Assert.AreEqual(Consumables.FedId, def.id); Assert.AreEqual("Well fed", def.name); Assert.AreEqual(1800, def.duration, "Half an hour.");
            Assert.AreEqual(1, def.incomingDamageMultiplier);
            var body = Body(out var stats);
            Assert.IsTrue(Consumables.Apply(skewer, body, 100));
            Assert.AreEqual(10 + skewer.buff.stamina, stats.Get(StatType.Stamina), "Stamina, as the dish gives it.");
            Assert.AreEqual(10 + skewer.buff.amount, stats.Get(StatType.Agility), "And one stat by dish: the skewer's Agility.");
            Assert.AreEqual(10, stats.Get(StatType.Strength), "No other stat.");
            Assert.AreEqual(1800, body.Remaining(Consumables.FedId, 100), 1e-3f);
            Assert.AreEqual("Well fed", Consumables.StatusName(Consumables.FedId), "The buff row names it.");
            body.Tick(100 + 1799); Assert.IsTrue(body.Has(Consumables.FedId), "Still on a second before the half hour.");
            body.Tick(100 + 1800); Assert.IsFalse(body.Has(Consumables.FedId), "Gone at the half hour.");
            Assert.AreEqual(10, stats.Get(StatType.Stamina)); Assert.AreEqual(10, stats.Get(StatType.Agility), "Its stats go with it.");
            StringAssert.Contains("Well fed: +" + skewer.buff.stamina + " Stamina, +" + skewer.buff.amount + " Agility for 30 min", ItemDatabase.StatLines(skewer), "The tooltip says so.");
        }

        /// <summary>Every dish whose recipe is Cooking 20 or more feeds you well (Stamina and one of the five stats, 1800 s, the dish's buff no
        /// smaller than a lower-level dish's); the three under 20 (griddle bread, boar stew, hearth-cake) do not; only food is well fed.</summary>
        [Test] public void Every_dish_from_cooking_twenty_is_well_fed_and_tiered_by_level()
        {
            var db = ProfessionDataTests.Db(Items()); var dishes = new List<ItemDef>();
            foreach (var r in db.RecipesFor("cooking"))
            {
                var d = Items().Get(r.output); Assert.IsTrue(d.food, r.output + " is eaten.");
                if (r.skill < 20) { Assert.IsFalse(Consumables.HasBuff(d), r.output + " (Cooking " + r.skill + ") only heals."); continue; }
                Assert.IsTrue(Consumables.HasBuff(d), r.output + " (Cooking " + r.skill + ") leaves you well fed.");
                Assert.AreEqual("food", d.buff.kind, r.output); Assert.AreEqual(d.buff.raid ? 3600 : 1800, d.buff.seconds, r.output + (d.buff.raid ? " (the raid's stewpot: an hour)" : ""));
                Assert.Greater(d.buff.stamina, 0, r.output + ": Stamina."); Assert.Greater(d.buff.amount, 0, r.output + ": and one stat.");
                Assert.IsTrue(Consumables.TryStat(d.buff.stat, out var stat) && stat != StatType.Armor && stat != StatType.Stamina, r.output + ": one of the four stats besides Stamina.");
                dishes.Add(d);
            }
            Assert.GreaterOrEqual(dishes.Count, 7, "The seven dishes from Cooking 20 and the found ones.");
            foreach (var a in dishes) foreach (var b in dishes)
                if (a.level < b.level) { Assert.LessOrEqual(a.buff.stamina, b.buff.stamina, a.id + " vs " + b.id); Assert.LessOrEqual(a.buff.amount, b.buff.amount, a.id + " vs " + b.id); }
            foreach (var d in Items().Items.Values) if (Consumables.HasBuff(d) && d.buff.kind == "food") Assert.IsTrue(d.food, d.id + ": well fed comes only from food.");
        }

        [Test] public void Food_is_never_eaten_in_a_fight_and_the_rest_may_be()
        {
            var skewer = Items().Get("food.wolf_skewer"); var tincture = Items().Get("potion.tincture_ox"); var salve = Items().Get("salve.tallow"); var feast = Items().Get("food.harvest_feast");
            Assert.AreEqual(Consumables.NoEatingLine, Consumables.Refusal(skewer, true, null)); Assert.AreEqual("You can't eat while fighting.", Consumables.NoEatingLine);
            Assert.AreEqual(Consumables.NoEatingLine, Consumables.Refusal(feast, true, null), "Nor a feast.");
            Assert.IsNull(Consumables.Refusal(skewer, false, null), "Out of a fight it is eaten.");
            Assert.IsNull(Consumables.Refusal(tincture, true, null), "A tincture is drunk in a fight.");
            Assert.IsNull(Consumables.Refusal(salve, true, null), "A salve too.");
            Assert.IsFalse(Consumables.OnPotionCooldown(skewer)); Assert.IsFalse(Consumables.OnPotionCooldown(tincture), "Tinctures and salves wait on no cooldown.");
            Assert.IsTrue(Consumables.OnPotionCooldown(Items().Get("potion.minor")), "A healing draught does, as before.");
            Assert.AreEqual("That can't be used.", Consumables.Refusal(Items().Get("mat.yarrow"), false, null));
        }

        [Test] public void One_buff_of_each_kind_at_a_time_and_the_three_kinds_together()
        {
            var body = Body(out var stats);
            var skewer = Items().Get("food.wolf_skewer"); var loin = Items().Get("food.smoked_loin");
            Assert.IsTrue(Consumables.Apply(skewer, body, 0)); Assert.IsTrue(Consumables.Apply(loin, body, 10));
            Assert.AreEqual(10 + loin.buff.stamina, stats.Get(StatType.Stamina), "The second dish's Stamina, not both.");
            Assert.AreEqual(10, stats.Get(StatType.Agility), "The first dish's Agility is gone.");
            Assert.AreEqual(10 + loin.buff.amount, stats.Get(StatType.Strength), "The loin's Strength.");
            Assert.AreEqual(1800, body.Remaining(Consumables.FedId, 10), 1e-3f, "And its half hour from now.");
            var ox = Items().Get("potion.tincture_ox"); var owl = Items().Get("potion.tincture_owl"); var salve = Items().Get("salve.tallow");
            Assert.AreEqual("elixir", ox.buff.kind); Assert.AreEqual(1800, ox.buff.seconds);
            Assert.IsTrue(Consumables.Apply(ox, body, 20)); Assert.IsTrue(Consumables.Apply(salve, body, 20));
            Assert.AreEqual(3, body.Count, "Well fed, a tincture and a salve sit together.");
            Assert.AreEqual(10 + loin.buff.amount + ox.buff.amount, stats.Get(StatType.Strength), "The loin's and the tincture's Strength add.");
            Assert.AreEqual(100 + salve.buff.amount, stats.Get(StatType.Armor), "The salve's armour.");
            Assert.IsTrue(Consumables.Apply(owl, body, 30));
            Assert.AreEqual(3, body.Count, "A second tincture replaces the first.");
            Assert.AreEqual(10 + loin.buff.amount, stats.Get(StatType.Strength)); Assert.AreEqual(10 + owl.buff.amount, stats.Get(StatType.Intellect));
            Assert.AreEqual("Owl-light", Consumables.StatusName(Consumables.ElixirId), "The buff row names the tincture on.");
            Assert.IsFalse(Consumables.Apply(Items().Get("potion.minor"), body, 30), "A healing draught has no buff.");
            Assert.IsFalse(Consumables.Apply(skewer, null, 30));
        }

        /// <summary>A feast feeds the party of five: the eater and the first four with them; a sixth and seventh go without, and nobody is fed twice.</summary>
        [Test] public void The_party_feast_feeds_five()
        {
            var feast = Items().Get("food.harvest_feast"); Assert.IsTrue(feast.buff.party, "The Harrow harvest feast is for the party.");
            Assert.AreEqual(5, Consumables.PartySize);
            var party = new List<StatusEffectRuntime>(); var stats = new List<StatBlock>();
            for (int i = 0; i < 7; i++) { party.Add(Body(out var s)); stats.Add(s); }
            var withRepeat = new List<StatusEffectRuntime>(party); withRepeat.Insert(1, party[0]);
            Assert.AreEqual(5, Consumables.Feed(feast, withRepeat, 0), "Five are fed.");
            for (int i = 0; i < 5; i++) { Assert.IsTrue(party[i].Has(Consumables.FedId), "fed " + i); Assert.AreEqual(10 + feast.buff.stamina, stats[i].Get(StatType.Stamina), "fed " + i); }
            for (int i = 5; i < 7; i++) Assert.IsFalse(party[i].Has(Consumables.FedId), "not fed " + i);
            Assert.AreEqual(1, Consumables.Feed(feast, new List<StatusEffectRuntime> { party[6] }, 0), "Alone, the eater is fed.");
            Assert.AreEqual(0, Consumables.Feed(Items().Get("potion.minor"), party, 0), "Nothing to share in a healing draught.");
            var feasts = Items().Items.Values.Where(d => Consumables.HasBuff(d) && d.buff.party).ToList();
            Assert.GreaterOrEqual(feasts.Count, 3, "A feast for the early, middle and late zones.");
            foreach (var d in feasts) { Assert.IsTrue(d.food, d.id); Assert.AreEqual("food", d.buff.kind, d.id); }
        }

        /// <summary>A draught for each class's pool (decision 10): one for each pool word; each class in the content fills its own (the
        /// Scoundrel's Nerve, the Druid's Breath, the Salt-Mender's Salt), and another class's is refused with why.</summary>
        [Test] public void A_draught_for_each_class_pool()
        {
            foreach (var pool in Consumables.Pools)
            {
                var draughts = Items().Items.Values.Where(d => d.restore > 0 && d.pool == pool).ToList();
                Assert.AreEqual(1, draughts.Count, "One draught fills " + pool + ".");
                Assert.IsTrue(Consumables.OnPotionCooldown(draughts[0]), pool + ": on the potion's cooldown.");
                StringAssert.Contains("Restores " + draughts[0].restore + " " + Consumables.PoolWord(pool), ItemDatabase.StatLines(draughts[0]));
            }
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset"); Assert.NotNull(content);
            int classes = 0;
            foreach (var c in content.AllClasses())
            {
                classes++; string pool = Consumables.PoolOf(c.definition);
                Assert.Contains(pool, Consumables.Pools, c.definition.id + "'s pool has a draught.");
                var mine = Items().Items.Values.First(d => d.restore > 0 && d.pool == pool);
                Assert.IsNull(Consumables.Refusal(mine, true, c.definition), c.definition.id + " drinks its own, even in a fight.");
            }
            Assert.GreaterOrEqual(classes, 11);
            var warrior = new ClassDefinition { id = "class.warrior", resource = ResourceKind.Vigor };
            var scoundrel = new ClassDefinition { id = "class.scoundrel", resource = ResourceKind.Focus, resourceName = "Nerve" };
            Assert.AreEqual("vigor", Consumables.PoolOf(warrior)); Assert.AreEqual("nerve", Consumables.PoolOf(scoundrel), "A pool with a name of its own is filled by that name.");
            Assert.AreEqual("That draught fills Mana. Yours is Vigor.", Consumables.Refusal(Items().Get("potion.draught_mana"), false, warrior));
            Assert.AreEqual("That draught fills Focus. Yours is Nerve.", Consumables.Refusal(Items().Get("potion.draught_focus"), false, scoundrel));
        }

        [Test] public void A_cure_lifts_poisons_bleeds_and_the_grey_and_nothing_else()
        {
            var cure = Items().Get("potion.clean_blood"); Assert.IsTrue(cure.cure); Assert.IsTrue(Consumables.OnPotionCooldown(cure));
            StringAssert.Contains("Lifts poisons, bleeds and the grey", ItemDatabase.StatLines(cure));
            var body = Body(out _);
            body.Apply(new StatusEffectDefinition { id = "status.spider_poison", name = "Poison", duration = 20 }, 0);
            body.Apply(new StatusEffectDefinition { id = "status.rake_bleed", name = "Bleeding", duration = 20 }, 0);
            body.Apply(EncounterEnemy.GreyDefinition, 0);
            Consumables.Apply(Items().Get("food.wolf_skewer"), body, 0);
            Assert.AreEqual(4, body.Count);
            Assert.AreEqual(3, body.RemoveHarmful(), "The poison, the bleed and the grey.");
            Assert.IsTrue(body.Has(Consumables.FedId), "Well fed stays.");
        }

        /// <summary>The item file refuses a buff it cannot carry out: on gear, of an unknown kind or stat, with no time or nothing to give,
        /// well fed on a potion, a feast that is not food, a draught for a pool nobody has.</summary>
        [Test] public void A_bad_buff_is_refused_with_every_problem_named()
        {
            string bad = "{ \"items\": [" +
                "{ \"id\": \"t.gear\", \"name\": \"G\", \"kind\": \"gear\", \"slot\": \"head\", \"buff\": { \"kind\": \"elixir\", \"stat\": \"Strength\", \"amount\": 3, \"seconds\": 60 } }," +
                "{ \"id\": \"t.kind\", \"name\": \"K\", \"kind\": \"consumable\", \"buff\": { \"kind\": \"blessing\", \"stat\": \"Strength\", \"amount\": 3, \"seconds\": 60 } }," +
                "{ \"id\": \"t.stat\", \"name\": \"S\", \"kind\": \"consumable\", \"buff\": { \"kind\": \"elixir\", \"stat\": \"Luck\", \"amount\": 3, \"seconds\": 60 } }," +
                "{ \"id\": \"t.time\", \"name\": \"T\", \"kind\": \"consumable\", \"buff\": { \"kind\": \"elixir\", \"stat\": \"Strength\", \"amount\": 3 } }," +
                "{ \"id\": \"t.fed\", \"name\": \"F\", \"kind\": \"consumable\", \"buff\": { \"kind\": \"food\", \"stamina\": 3, \"seconds\": 60 } }," +
                "{ \"id\": \"t.feast\", \"name\": \"P\", \"kind\": \"consumable\", \"buff\": { \"kind\": \"elixir\", \"stat\": \"Spirit\", \"amount\": 2, \"seconds\": 60, \"party\": true } }," +
                "{ \"id\": \"t.pool\", \"name\": \"D\", \"kind\": \"consumable\", \"pool\": \"rage\", \"restore\": 40 }," +
                "{ \"id\": \"t.leaf\", \"name\": \"L\", \"kind\": \"recipe\" } ] }";
            var e = Assert.Throws<System.ArgumentException>(() => ItemDatabase.Parse(new[] { bad }));
            foreach (var expected in new[] { "Item 't.gear': a buff on something that is not a consumable", "Item 't.kind': unknown buff kind 'blessing'", "Item 't.stat': buff stat 'Luck'",
                "Item 't.time': a buff with no time", "Item 't.fed': well fed on something that is not food", "Item 't.feast': only a feast feeds the party", "Item 't.pool': a draught for pool 'rage'",
                "Leaf 't.leaf' teaches no recipe" })
                StringAssert.Contains(expected, e.Message);
        }
    }
}
