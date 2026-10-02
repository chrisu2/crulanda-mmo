using System.Linq;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// Better or worse (loot DESIGN.md 4, feature 4, step L1): LootJudge, which marks the upgrade arrow on item squares and writes
    /// the green and red lines on a gear tooltip. An empty slot makes any wearable piece an upgrade; the differences are listed
    /// stat by stat as gains and losses; a piece above your level is never marked; Intellect and Spirit score nothing, because
    /// nothing in the game reads them yet. Pure logic: no scene, no session and no save.
    /// </summary>
    public class LootJudgeTests
    {
        static ItemDef Gear(string slot, int level = 1, int weaponDamage = 0, int armor = 0, int stamina = 0, int strength = 0, int agility = 0, int intellect = 0, int spirit = 0, int quality = 1)
        {
            return new ItemDef { id = "test." + slot, kind = "gear", slot = slot, level = level, quality = quality, weaponDamage = weaponDamage, armor = armor,
                stamina = stamina, strength = strength, agility = agility, intellect = intellect, spirit = spirit };
        }

        [Test] public void An_empty_slot_makes_any_wearable_piece_an_upgrade()
        {
            Assert.IsTrue(LootJudge.IsUpgrade(Gear("head", armor: 1), null, 1), "A common cap beats a bare head.");
            Assert.IsTrue(LootJudge.IsUpgrade(Gear("mainhand", weaponDamage: 2, quality: 0), null, 1), "Even a worn-out blade beats empty hands.");
            Assert.IsTrue(LootJudge.IsUpgrade(Gear("neck", intellect: 3), null, 1), "A piece that scores nothing still fills an empty slot.");
            Assert.IsFalse(LootJudge.IsUpgrade(new ItemDef { id = "junk.test", kind = "junk", quality = 0 }, null, 1), "Junk is never gear.");
            Assert.IsFalse(LootJudge.IsUpgrade(new ItemDef { id = "potion.test", kind = "consumable" }, null, 1), "Nor is a potion.");
            Assert.IsFalse(LootJudge.IsUpgrade(null, null, 1));
            // The generated gear the bodies drop is judged the same way.
            var db = new ItemDatabase(); var blade = db.Get(ItemDatabase.GearId("mainhand", 3, 2, 42));
            Assert.NotNull(blade); Assert.IsTrue(LootJudge.IsUpgrade(blade, null, 3));
        }

        [Test] public void Deltas_list_gains_and_losses_by_stat()
        {
            var candidate = Gear("mainhand", weaponDamage: 10, stamina: 2, agility: 3);
            var worn = Gear("mainhand", weaponDamage: 7, stamina: 4, strength: 1, agility: 3);
            var lines = LootJudge.DeltaLines(candidate, worn);
            CollectionAssert.AreEqual(new[] { "+3 weapon damage", "-2 Stamina", "-1 Strength" }, lines.Select(l => l.line).ToArray(), "Changed stats only, in the tooltip's order.");
            CollectionAssert.AreEqual(new[] { true, false, false }, lines.Select(l => l.gain).ToArray(), "The weapon damage is a gain; the Stamina and Strength are losses.");
            // Against an empty slot every stat is a gain.
            var bare = LootJudge.DeltaLines(Gear("chest", armor: 9, stamina: 2), null);
            CollectionAssert.AreEqual(new[] { "+9 armor", "+2 Stamina" }, bare.Select(l => l.line).ToArray());
            Assert.IsTrue(bare.All(l => l.gain));
            // The same piece: nothing to say.
            CollectionAssert.IsEmpty(LootJudge.DeltaLines(worn, worn));
            // +3 weapon damage and -2 Stamina is better on balance: 3 - 1 - 1 = +1.
            Assert.AreEqual(1f, LootJudge.Compare(candidate, worn), 1e-4f);
            Assert.IsTrue(LootJudge.IsUpgrade(candidate, worn, 1));
            Assert.IsFalse(LootJudge.IsUpgrade(worn, candidate, 1), "And the other way round it is not.");
        }

        [Test] public void A_piece_above_your_level_is_not_marked_as_an_upgrade()
        {
            var big = Gear("mainhand", level: 5, weaponDamage: 20, strength: 6);
            Assert.IsFalse(LootJudge.IsUpgrade(big, null, 4), "Not yet: it wants level 5.");
            Assert.IsFalse(LootJudge.IsUpgrade(big, Gear("mainhand", weaponDamage: 4), 4));
            Assert.IsTrue(LootJudge.IsUpgrade(big, Gear("mainhand", weaponDamage: 4), 5), "At level 5 it is.");
            Assert.Greater(LootJudge.Compare(big, Gear("mainhand", weaponDamage: 4)), 0, "The score itself does not care about level.");
        }

        [Test] public void Intellect_and_Spirit_do_not_count()
        {
            Assert.AreEqual(0f, LootJudge.Score(Gear("neck", intellect: 8, spirit: 5)));
            var plain = Gear("head", armor: 4); var owl = Gear("head", armor: 4, intellect: 6, spirit: 3);
            Assert.AreEqual(0f, LootJudge.Compare(owl, plain), 1e-4f, "An Owl cap scores the same as the plain one.");
            Assert.IsFalse(LootJudge.IsUpgrade(owl, plain, 1), "So it is not marked as an upgrade.");
            Assert.IsTrue(LootJudge.DeltaLines(owl, plain).Any(l => l.line == "+6 Intellect" && l.gain), "The tooltip still shows the difference.");
            // The stats that count: weapon damage and Strength fully, Stamina half, armour and Agility a quarter.
            Assert.AreEqual(10 + 4 + 3 + 2 + 1.5f, LootJudge.Score(Gear("mainhand", weaponDamage: 10, strength: 4, stamina: 6, armor: 8, agility: 6)), 1e-4f);
        }
    }
}
