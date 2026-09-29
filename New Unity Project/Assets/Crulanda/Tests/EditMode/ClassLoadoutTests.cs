using System;
using NUnit.Framework;
using Crulanda.Abilities;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    public class ClassLoadoutTests
    {
        static AbilityDefinition[] Catalog() { return new[] {
            new AbilityDefinition { id = "ability.strike" },
            new AbilityDefinition { id = "ability.challenge" },
            new AbilityDefinition { id = "ability.guard" }
        }; }
        [Test]
        public void Class_slots_use_identity_not_catalog_order()
        {
            var catalog = Catalog(); Array.Reverse(catalog);
            var loadout = new ClassLoadout(new ClassDefinition(), catalog);
            Assert.AreEqual("ability.strike", loadout.At(0).id);
            Assert.IsFalse(loadout.Owns("ability.light_bolt", 10));
            Assert.IsNull(loadout.At(-1)); Assert.IsFalse(loadout.CanUse(3, 10));
        }
        [Test]
        public void Unlock_gates_change_at_the_required_level()
        {
            var definition = new ClassDefinition(); definition.unlocks[2].level = 4;
            var loadout = new ClassLoadout(definition, Catalog());
            Assert.IsFalse(loadout.CanUse(2, 3)); Assert.IsTrue(loadout.CanUse(2, 4));
            Assert.AreEqual(4, loadout.UnlockLevel(2));
        }
        [Test]
        public void Duplicate_or_unknown_unlocks_are_rejected()
        {
            var definition = new ClassDefinition(); definition.unlocks[2].abilityId = "ability.strike";
            Assert.Throws<ArgumentException>(() => new ClassLoadout(definition, Catalog()));
            definition.unlocks[2].abilityId = "ability.missing";
            Assert.Throws<ArgumentException>(() => new ClassLoadout(definition, Catalog()));
        }
        [Test]
        public void Invalid_resource_or_catalog_is_rejected()
        {
            var definition = new ClassDefinition(); definition.maxResource = -1;
            Assert.Throws<ArgumentException>(() => new ClassLoadout(definition, Catalog()));
            var catalog = Catalog(); catalog[1].id = catalog[0].id;
            Assert.Throws<ArgumentException>(() => new ClassLoadout(new ClassDefinition(), catalog));
        }
    }
}
