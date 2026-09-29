using System.Collections.Generic;
using NUnit.Framework;
using Crulanda.Core;

namespace Crulanda.Tests
{
    public class GameTagTests
    {
        [TestCase("Damage", true)]
        [TestCase("Damage.Fire", true)]
        [TestCase("Ability.CrowdControl", true)]
        [TestCase("A1.B2", true)]
        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("damage", false)]
        [TestCase("Damage.", false)]
        [TestCase(".Damage", false)]
        [TestCase("Damage..Fire", false)]
        [TestCase("Damage.fire", false)]
        [TestCase("Damage Fire", false)]
        [TestCase("Damage-Fire", false)]
        public void IsValidPath(string path, bool expected)
        {
            Assert.AreEqual(expected, GameTag.IsValidPath(path));
        }

        [Test]
        public void Tag_matches_itself_and_its_ancestors()
        {
            var fire = new GameTag("Damage.Fire");
            Assert.IsTrue(fire.Matches(new GameTag("Damage.Fire")));
            Assert.IsTrue(fire.Matches(new GameTag("Damage")));
        }

        [Test]
        public void Tag_does_not_match_descendants_siblings_or_lookalikes()
        {
            Assert.IsFalse(new GameTag("Damage").Matches(new GameTag("Damage.Fire")));
            Assert.IsFalse(new GameTag("Damage.Frost").Matches(new GameTag("Damage.Fire")));
            Assert.IsFalse(new GameTag("Damage.FireStorm").Matches(new GameTag("Damage.Fire")));
        }

        [Test]
        public void TagSet_queries_are_hierarchical()
        {
            var set = new TagSet(new[] { GameTags.Damage.Fire, GameTags.Class.Warrior });
            Assert.IsTrue(set.Has(new GameTag("Damage")));
            Assert.IsTrue(set.Has(GameTags.Damage.Fire));
            Assert.IsFalse(set.Has(GameTags.Damage.Frost));
            Assert.IsTrue(set.HasAny(GameTags.Damage.Frost, GameTags.Class.Warrior));
            Assert.IsTrue(set.HasAll(GameTags.Damage.Fire, GameTags.Class.Warrior));
            Assert.IsFalse(set.HasAll(GameTags.Damage.Fire, GameTags.Class.Mage));
        }

        [Test]
        public void TagSet_rejects_duplicates_and_invalid_tags()
        {
            var set = new TagSet();
            Assert.IsTrue(set.Add(GameTags.State.Dead));
            Assert.IsFalse(set.Add(GameTags.State.Dead));
            Assert.IsFalse(set.Add(new GameTag("not valid")));
            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void All_declared_tags_are_valid_and_unique()
        {
            var seen = new HashSet<string>();
            Assert.Greater(GameTags.All.Count, 0);
            foreach (var tag in GameTags.All)
            {
                Assert.IsTrue(tag.IsValid, "Invalid tag: " + tag);
                Assert.IsTrue(seen.Add(tag.Path), "Duplicate tag: " + tag);
                Assert.IsTrue(GameTags.IsKnown(tag));
            }
        }

        [Test]
        public void Undeclared_tag_is_not_known()
        {
            Assert.IsFalse(GameTags.IsKnown(new GameTag("Made.Up")));
        }
    }
}
