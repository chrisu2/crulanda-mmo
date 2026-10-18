using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Core;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Lowtowns' rework, picks 4 and 5 (Docs/REWORK_LOWTOWNS_HALLOWS.md; Chris, 2026-10-18: "keep all", "snivel is fine"), checked against
    /// Book 1 (b1 ch.N:L is D:\code\crulanda\book1\chapter_N.md at line L):
    /// - the Weave-Eaters as chapter 20 shows them (WeaveEaterRules): they blink toward their foe (ch.20:131), the party's magic-workers draw
    ///   them (ch.20:121), they eat a ward (ch.20:143), a harpoon stops the blink a while (ch.20:191), salt calcifies (ch.20:205); only in the
    ///   Lowtowns;
    /// - the Rusty Spoon as chapter 1 tells it (SpoonRules): Triad's three cards with the Crown unbeatable (ch.1:45-57), the stake, the
    ///   brawl's blows that never take you under a third of your health, the drunk's ballad without verses (ch.1:59), the smell (ch.1:7);
    /// - the Spoon's people in the zone's data: Snivels Gear-Grinder the goblin bartender at the bar (ch.1:105; ch.4:13), Gorm's line about the
    ///   boy he dug out of Hallow's Creek's ash with the boy unnamed (ch.19:39), the corner drunk, Mother Sall kept as keeper (the books name
    ///   none), each labelled with the book.
    /// </summary>
    public class LowtownsReworkTests
    {
        static ZoneDefinition Zone(string file) { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones", file + ".json"))); }

        // ---------- the Weave-Eaters (pick 4) ----------
        [Test] public void Weave_Eaters_blink_toward_their_foe_unless_held_or_pinned()
        {
            Assert.IsTrue(WeaveEaterRules.Blinks(10, false, false, 5, 5), "Ten metres off and ready: it displaces (ch.20:131).");
            Assert.IsFalse(WeaveEaterRules.Blinks(3, false, false, 5, 5), "Close: it walks.");
            Assert.IsFalse(WeaveEaterRules.Blinks(10, true, false, 5, 5), "Rooted: no blink.");
            Assert.IsFalse(WeaveEaterRules.Blinks(10, false, true, 5, 5), "Harpooned: no displacing (ch.20:191).");
            Assert.IsFalse(WeaveEaterRules.Blinks(10, false, false, 4, 5), "Not ready yet.");
            Assert.AreEqual(WeaveEaterRules.BlinkReach, WeaveEaterRules.BlinkStep(30), 1e-4f, "A short blink, not across the zone.");
            Assert.That(WeaveEaterRules.BlinkReach, Is.InRange(6f, 8f), "6-8 m (the plan, 5A).");
            Assert.AreEqual(4.2f, WeaveEaterRules.BlinkStep(6), 1e-4f, "It stops short of its foe.");
            Assert.Greater(WeaveEaterRules.PinSeconds, WeaveEaterRules.HarpoonSeconds, "The harpoon stops the blink longer than it roots.");
        }

        [Test] public void Magic_draws_them_and_they_eat_wards()
        {
            Assert.IsTrue(WeaveEaterRules.DrawnTo(ResourceKind.Mana, 0), "A mage's aura, a lighthouse (ch.20:121).");
            Assert.IsTrue(WeaveEaterRules.DrawnTo(ResourceKind.Salt, 0), "The Salt-Mender's craft is magic too.");
            Assert.IsTrue(WeaveEaterRules.DrawnTo(ResourceKind.Vigor, 40), "A ward on anyone draws them.");
            Assert.IsFalse(WeaveEaterRules.DrawnTo(ResourceKind.Vigor, 0)); Assert.IsFalse(WeaveEaterRules.DrawnTo(ResourceKind.Focus, 0));
            Assert.AreEqual(12, WeaveEaterRules.LighthouseThreat(100), 1e-3f); Assert.AreEqual(1, WeaveEaterRules.LighthouseThreat(0), 1e-3f, "Something even with no threat on.");
            Assert.AreEqual(0, WeaveEaterRules.WardBite(0));
            Assert.AreEqual(35, WeaveEaterRules.WardBite(100), "A third of a ward a second (ch.20:143).");
            Assert.AreEqual(3, WeaveEaterRules.WardBite(3), "The last of it all at once.");
            int ward = 200, seconds = 0; while (ward > 0 && seconds < 60) { ward -= WeaveEaterRules.WardBite(ward); seconds++; }
            Assert.LessOrEqual(seconds, 12, "A big ward is gone in seconds without a blow.");
        }

        [Test] public void Only_the_Lowtowns_Weave_Eaters_fight_as_the_book_shows_them()
        {
            Assert.IsTrue(WeaveEaterRules.BookZone("zone.lowtowns"));
            foreach (var other in new[] { "zone.ashrim", "zone.verdant", "zone.oakhaven", "zone.hallows" }) Assert.IsFalse(WeaveEaterRules.BookZone(other), other + " keeps the fight Chris settled.");
            StringAssert.Contains("calcif", WeaveEaterRules.ThrowLine(WeaveEaterRules.Salt, "the Weave-Eater", true, true), "Salt calcifies (ch.20:205).");
            StringAssert.Contains("cannot displace", WeaveEaterRules.ThrowLine(WeaveEaterRules.Harpoon, "the Weave-Eater", true, true), "The harpoon stops it displacing (ch.20:191).");
            var z = Zone("lowtowns");
            foreach (var c in z.camps.Where(c => c.look == "weaveeater" && !c.elite && c.rare <= 0)) StringAssert.Contains("ch.20:131", c.canonStatus, c.name + " cites the blink.");
        }

        // ---------- the Rusty Spoon (pick 5) ----------
        static SpoonRules.Card C(int rank, int suit = 0) { return new SpoonRules.Card(rank, suit); }

        [Test] public void Triad_is_three_cards_high_card_takes_it_and_the_Crown_beats_all()
        {
            CollectionAssert.AreEquivalent(new[] { "Stones", "Cups", "Swords", "Wyrms", "Coins" }, SpoonRules.Suits, "The book's suits (ch.1:47, 71).");
            var crown = new[] { C(SpoonRules.King, 0), C(SpoonRules.King, 1), C(SpoonRules.King, 2) };
            Assert.IsTrue(SpoonRules.Crown(crown)); Assert.AreEqual("King of Stones", crown[0].ToString());
            Assert.AreEqual(1, SpoonRules.Compare(crown, new[] { C(SpoonRules.King, 3), C(SpoonRules.King, 4), C(10) }), "The Crown. Unbeatable (ch.1:57).");
            Assert.AreEqual(-1, SpoonRules.Compare(new[] { C(2), C(5), C(7) }, new[] { C(8), C(3), C(2) }), "High card takes it (ch.1:45).");
            Assert.AreEqual(1, SpoonRules.Compare(new[] { C(9), C(6), C(2) }, new[] { C(9), C(5), C(4) }), "Ties go to the next card.");
            Assert.AreEqual(0, SpoonRules.Compare(new[] { C(9, 0), C(6, 1), C(2, 2) }, new[] { C(9, 3), C(6, 4), C(2, 1) }), "The same ranks: a push.");
            Assert.AreEqual(-1, SpoonRules.Compare(new[] { C(SpoonRules.Ace), C(2), C(3) }, new[] { C(4), C(2), C(3) }), "The Ace is low: the King tops the deck.");
            var rng = new System.Random(7);
            for (int i = 0; i < 200; i++)
            {
                var (mine, theirs) = SpoonRules.Deal(rng);
                var all = mine.Concat(theirs).Select(x => x.rank * 10 + x.suit).ToList();
                Assert.AreEqual(6, all.Distinct().Count(), "Six different cards from one deck.");
                Assert.IsTrue(mine.Concat(theirs).All(x => x.rank >= 1 && x.rank <= SpoonRules.Ranks && x.suit >= 0 && x.suit < 5));
            }
            Assert.AreEqual(15, SpoonRules.Stake(15)); Assert.AreEqual(5, SpoonRules.Stake(1), "Never under five crowns.");
            Assert.AreEqual(30, SpoonRules.Payout(15, 1, false)); Assert.AreEqual(60, SpoonRules.Payout(15, 1, true), "The Crown pays three times over.");
            Assert.AreEqual(15, SpoonRules.Payout(15, 0, false)); Assert.AreEqual(0, SpoonRules.Payout(15, -1, false));
            StringAssert.Contains("Page forty-two", SpoonRules.CheatLine, "The Grand Emperor's Flush (ch.1:87).");
            Assert.That(SpoonRules.CheatChance, Is.InRange(.01f, .1f), "Now and then, not every hand.");
        }

        [Test] public void The_brawl_hurts_but_never_kills_and_the_ballad_has_no_verses()
        {
            for (int hp = 1000; hp > 0; hp -= 7)
            {
                int blow = SpoonRules.JoinBlow(hp, 1000);
                Assert.GreaterOrEqual(blow, 0); Assert.GreaterOrEqual(hp - blow, Mathf.Min(hp, 350), "Never under a third and a bit of your health.");
            }
            Assert.AreEqual(50, SpoonRules.JoinBlow(1000, 1000)); Assert.AreEqual(0, SpoonRules.JoinBlow(350, 1000), "At the floor, no more.");
            foreach (var l in SpoonRules.BalladLines) { StringAssert.Contains("oat", l); Assert.Less(l.Length, 80, "A line, not a verse: the book gives only the title."); }
            StringAssert.Contains("wet dog and stale beer", SpoonRules.SmellLine, "ch.1:7.");
            StringAssert.Contains("through the front window", SpoonRules.ThrowLine("Wat", true), "ch.1:121."); StringAssert.Contains("Gorm", SpoonRules.ThrowLine("Wat", true));
            Assert.Greater(SpoonRules.BrawlEveryMin, 120, "A brawl most nights, not every minute.");
            Assert.Greater(SpoonRules.WindowRepairSeconds, SpoonRules.BrawlSeconds, "The window stays boarded a while after.");
        }

        [Test] public void Snivels_Gorm_the_drunk_and_Sall_are_at_the_Spoon_as_the_books_have_them()
        {
            var z = Zone("lowtowns");
            var r = z.life.residents.ToDictionary(x => x.name);
            var spoon = z.props.Single(p => p != null && p.name == "The Rusty Spoon");
            var plan = ZoneBuilder.InnPlan.For(spoon.size, spoon.variant);
            Vector2 Local(Vector2 at) { return at - spoon.at; }
            // Snivels, the goblin bartender (ch.1:105), at the bar.
            Assert.IsTrue(r.ContainsKey(SpoonRules.Bartender), "Snivels pours (Chris: \"snivel is fine\").");
            var snivels = r[SpoonRules.Bartender];
            Assert.AreEqual("goblin", snivels.look); Assert.IsFalse(snivels.works, "He keeps his post at the bar.");
            StringAssert.Contains("ch.1:105", snivels.canonStatus); StringAssert.Contains("ch.4:13", snivels.canonStatus);
            Assert.IsTrue(snivels.stories.Any(s => s.Contains("east")), "He knows the forgotten eastern paths (ch.4:13).");
            var at = Local(snivels.at);
            Assert.Less(Mathf.Abs(at.x), plan.Inner.x - .3f); Assert.Less(Mathf.Abs(at.y), plan.Inner.y - .3f, "Inside the Spoon.");
            Assert.Less(Vector2.Distance(at, plan.bar) - plan.barSize.magnitude / 2, 1.2f, "By the bar.");
            foreach (var (what, c, half) in plan.Furniture())
                Assert.Greater(new Vector2(Mathf.Max(0, Mathf.Abs(at.x - c.x) - half.x), Mathf.Max(0, Mathf.Abs(at.y - c.y) - half.y)).magnitude, .35f, "Snivels stands clear of " + what + ".");
            // The drunk in the corner (ch.1:59).
            var drunk = r[SpoonRules.Drunk]; StringAssert.Contains("ch.1:59", drunk.canonStatus);
            var d = Local(drunk.at); Assert.Less(Mathf.Abs(d.x), plan.Inner.x - .3f); Assert.Less(Mathf.Abs(d.y), plan.Inner.y - .3f, "The drunk is in the Spoon.");
            foreach (var (what, c, half) in plan.Furniture())
                Assert.Greater(new Vector2(Mathf.Max(0, Mathf.Abs(d.x - c.x) - half.x), Mathf.Max(0, Mathf.Abs(d.y - c.y) - half.y)).magnitude, .35f, "The drunk stands clear of " + what + ".");
            // Gorm: rust, and the boy in the ash, unnamed (ch.1:133; ch.19:39).
            var gorm = r[SpoonRules.Gorm];
            Assert.IsTrue(gorm.stories.Any(s => s.Contains("rust")), "Ale tastes like rust (ch.1:133).");
            Assert.IsTrue(gorm.stories.Any(s => s.Contains("Hallow's Creek") && s.Contains("Ten years")), "The boy he dug out (ch.19:39).");
            Assert.IsFalse(gorm.stories.Any(s => s.Contains("Valen")), "The boy is not named.");
            StringAssert.Contains("ch.19:39", gorm.canonStatus);
            // Mother Sall keeps the house: the books name no keeper.
            Assert.AreEqual("bar", r[SpoonRules.Keeper].place); StringAssert.Contains("GAME-ONLY", r[SpoonRules.Keeper].canonStatus);
            var spoonMark = z.landmarks.Single(l => l.name == "The Rusty Spoon");
            StringAssert.Contains("wet dog and stale beer", spoonMark.text); StringAssert.Contains("ch.1:107", spoonMark.canonStatus);
        }
    }
}
