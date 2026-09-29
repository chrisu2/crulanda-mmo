using System.Collections.Generic;
using NUnit.Framework;
using Crulanda.Core;

namespace Crulanda.Tests
{
    public class ContentIdTests
    {
        [TestCase("item.iron_sword", true)]
        [TestCase("a", true)]
        [TestCase("ability.warrior.strike-1", true)]
        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("Item.Sword", false)]
        [TestCase("1item", false)]
        [TestCase("item sword", false)]
        [TestCase("item/sword", false)]
        public void IsValidFormat(string value, bool expected)
        {
            Assert.AreEqual(expected, ContentId.IsValidFormat(value));
        }

        [Test]
        public void Too_long_ids_are_invalid()
        {
            Assert.IsFalse(ContentId.IsValidFormat(new string('a', ContentId.MaxLength + 1)));
            Assert.IsTrue(ContentId.IsValidFormat(new string('a', ContentId.MaxLength)));
        }

        [Test]
        public void Equality_and_hashing_follow_the_string_value()
        {
            var a = new ContentId("item.x");
            var b = new ContentId("item.x");
            var c = new ContentId("item.y");
            Assert.IsTrue(a == b);
            Assert.IsTrue(a != c);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());

            var dict = new Dictionary<ContentId, int> { { a, 1 } };
            Assert.AreEqual(1, dict[b]);
        }

        [Test]
        public void Default_and_empty_ids_are_invalid_and_equal()
        {
            Assert.IsFalse(default(ContentId).IsValid);
            Assert.IsTrue(default(ContentId) == ContentId.Empty);
        }

        [Test]
        public void EntityId_New_is_valid_and_unique()
        {
            var a = EntityId.New();
            var b = EntityId.New();
            Assert.IsTrue(a.IsValid);
            Assert.IsTrue(a != b);
            Assert.AreEqual(32, a.Value.Length);
            Assert.IsFalse(default(EntityId).IsValid);
        }
    }
}
