using System;
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
    /// The book of lore (LoreBook; Phase 8.6 step 7, Chris's decision 8 of 2026-10-15: "the Lore tab in the quest book"), without a scene:
    /// - the real lore files are sound: one for every zone, every entry about something its zone has, labelled with one of the four
    ///   labels (CANON only with a book and chapter), every page and find it names real, and no line naming a hidden find (the
    ///   better half may name only the find it is found at); every carving and grave tells the book something, every book to read and
    ///   every dead adventurer gives a page of lore; and about ten pages or finds a zone tell more;
    /// - entries unlock by doing (a place reached, a person spoken to, a creature killed, a node worked, a rare herb found) and their
    ///   better half by finding its page or its find; what doing records lives in EncounterProgress.explored under keys of its own,
    ///   once each, and only for what the zone has, and the places explored are counted as before;
    /// - the lines the book shows, and where it places things (a creature's haunts, a node's ground), never name a hidden find, and a
    ///   rare herb's notes never say where it grows;
    /// - a zone's chapter filled gives its achievement and its title, and bad files are caught.
    /// </summary>
    public class LoreBookTests
    {
        static string Content(string sub) { return Path.Combine(Application.dataPath, "Crulanda", sub); }
        static List<string> Texts(string folder) { return Directory.GetFiles(Content(Path.Combine("EncounterContent", folder)), "*.json").OrderBy(f => f, StringComparer.Ordinal).Select(File.ReadAllText).ToList(); }
        /// <summary>Every zone with its side file merged, as the game has them.</summary>
        static List<ZoneDefinition> Zones()
        {
            var zones = new List<ZoneDefinition>();
            foreach (var json in Texts("Zones"))
            {
                var z = JsonUtility.FromJson<ZoneDefinition>(json); if (z == null || string.IsNullOrEmpty(z.id)) continue;
                string side = Path.Combine(Content("Resources/Lore"), ZoneLore.Short(z.id) + ".json");
                if (File.Exists(side)) ZoneLore.Apply(z, ZoneLore.ParseSide(File.ReadAllText(side)));
                zones.Add(z);
            }
            return zones;
        }
        /// <summary>The real book over the real content, bound to a fresh character.</summary>
        static LoreBook RealBook(out EncounterProgress p)
        {
            var items = LootTestData.Items();
            var book = LoreBook.Parse(Texts("Lore"));
            book.Zones = Zones(); book.Quests = QuestDatabase.Parse(Texts("Quests")); book.Items = items;
            book.Trades = ProfessionDatabase.Parse(Texts("Professions"), items); book.Loot = LootTestData.Loot(items);
            p = EncounterSession.FreshProgress(); book.Bind(p);
            return book;
        }

        // ---------- the data ----------
        [Test] public void The_lore_files_are_sound_every_entry_labelled_and_none_names_a_hidden_find()
        {
            var book = RealBook(out _); var zones = book.Zones;
            Assert.IsEmpty(zones.Where(z => book.FileOf(z.id) == null).Select(z => z.id), "Every zone has its chapter of the book (EncounterContent/Lore/<zone>.json).");
            var problems = LoreBook.Validate(book.Files.Values, zones, book.Quests, book.Trades);
            foreach (var f in book.Files.Values)
            {
                if (!LoreBook.Labelled(f.canonStatus, out var why)) problems.Add(f.zone + "'s file: " + why + ".");
                foreach (var e in f.entries) if (string.IsNullOrWhiteSpace(e.canonStatus)) problems.Add(e.id + ": unlabelled.");
            }
            // A carving or a grave is a line of the book; a book to read and a dead adventurer give a page of it.
            var unlocks = new HashSet<string>(book.Files.Values.SelectMany(f => f.entries).SelectMany(e => new[] { e.page, e.secret }).Where(x => !string.IsNullOrEmpty(x)));
            foreach (var z in zones)
                foreach (var s in z.secrets ?? new ZoneSecret[0])
                {
                    if ((s.kind == "carving" || s.kind == "grave") && !unlocks.Contains(s.id)) problems.Add(s.id + ": a " + s.kind + " that tells the book of lore nothing.");
                    if ((s.kind == "read" || s.kind == "remains") && !(book.Quests.Documents.TryGetValue(s.document ?? "", out var d) && d.IsLore)) problems.Add(s.id + ": its page is no page of the book of lore.");
                }
            Assert.IsEmpty(problems, string.Join("\n", problems));
            // About ten pages or finds a zone tell more (3d: "About 10 pages a zone"); the open zones at least eight.
            foreach (var z in zones.Where(x => !x.dungeon))
            {
                int more = book.FileOf(z.id).entries.Select(e => e.page ?? e.secret).Where(x => !string.IsNullOrEmpty(x)).Distinct().Count();
                Assert.GreaterOrEqual(more, 8, z.displayName + ": pages and finds that tell more.");
            }
        }

        [Test] public void No_line_the_book_shows_names_a_hidden_find_and_a_rare_herb_never_says_where()
        {
            var book = RealBook(out var p); var problems = new List<string>();
            var secrets = book.Zones.SelectMany(z => z.secrets ?? new ZoneSecret[0]).Where(s => s != null && !string.IsNullOrEmpty(s.name)).ToList();
            foreach (var z in book.Zones)
            {
                // Everything known: every page found, every place explored, every person met, every kind killed, every node worked.
                foreach (var l in Achievements.Pois(z)) p.explored.Add(Achievements.PoiKey(z, l));
                foreach (var n in LoreBook.PeopleOf(z)) book.Meet(z, n);
                foreach (var m in LoreBook.CreaturesOf(z)) book.Slay(z, m);
                foreach (var h in LoreBook.HerbsOf(z)) book.Work(z, h);
                foreach (var c in LoreBook.ZoneChapters)
                    foreach (var line in book.Lines(z, c))
                    {
                        Assert.IsTrue(line.known, z.id + " " + c + " " + line.subject + " is known once done.");
                        // Its own line and where the book places it (a creature's haunts, a node's ground). What people tell you (their stories)
                        // and the names of what drops are the game's talk and its items, said elsewhere too: not the book's own placing.
                        foreach (var text in new[] { line.plain }.Concat(line.notes.Where(n => n.StartsWith("Found at ") || n.StartsWith("Found in ") || n.StartsWith("Worked about "))))
                            foreach (var s in secrets) if (DiscoveryLog.Mentions(text, s.name)) problems.Add(z.id + " " + c + " '" + line.subject + "' names the find '" + s.name + "': " + text);
                        if (c == LoreBook.Herbs && !line.subject.StartsWith("node."))
                            foreach (var n in line.notes) foreach (var l in z.landmarks) if (DiscoveryLog.Mentions(n, l.name)) problems.Add(z.id + ": the rare herb " + line.subject + " is placed by its note: " + n);
                    }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        // ---------- the rules ----------
        static ZoneDefinition TestZone()
        {
            return new ZoneDefinition {
                id = "zone.test", displayName = "Testmoor", canonStatus = "GAME-ONLY (test)", canonNote = "A test zone.",
                landmarks = new[] { new ZoneLabel { name = "The Old Mill", text = "It grinds.", at = new Vector2(0, 0), radius = 12 }, new ZoneLabel { name = "Crow Hill", text = "Crows.", at = new Vector2(80, 0), radius = 12 } },
                camps = new[] { new ZoneCamp { name = "Mill rats", mob = "Mill rat", tag = "rat", center = new Vector2(10, 10), levelMin = 1, levelMax = 2 }, new ZoneCamp { name = "The Quiet One", mob = "Quiet One", tag = "quiet", rare = .2f } },
                nodes = new[] { new ZoneNode { node = "node.yarrow", at = new Vector2(4, 4) } },
                secrets = new[] { new ZoneSecret { id = "secret.test.bells", name = "The Bells", kind = "herb", item = "herb.moonbell", prompt = "Pick", at = new Vector2(70, 0) },
                                  new ZoneSecret { id = "secret.test.stone", name = "A Named Stone", kind = "grave", prompt = "Read", text = "A name.", at = new Vector2(20, 0) } },
                life = new ZoneLife { villagers = 0, residents = new[] { new ZoneResident { name = "Ada Test", title = "Miller", stories = new[] { "I grind.", "I sleep." } } } },
            };
        }
        static LoreBook TestBook(ZoneDefinition z, out EncounterProgress p)
        {
            var file = new LoreFile { zone = z.id, canonStatus = "GAME-ONLY (test)", entries = new[] {
                new LoreEntry { id = "lore.test.places.mill", chapter = LoreBook.Places, subject = "The Old Mill", more = "Its wheel was cut from one oak.", page = "doc.oakhaven.acorn_oath", canonStatus = "GAME-ONLY (test)" },
                new LoreEntry { id = "lore.test.people.ada", chapter = LoreBook.People, subject = "Ada Test", more = "Her mother lies under the named stone.", secret = "secret.test.stone", canonStatus = "GAME-ONLY (test)" },
                new LoreEntry { id = "lore.test.creatures.rat", chapter = LoreBook.Creatures, subject = "Mill rat", line = "Rats as big as cats.", canonStatus = "GAME-ONLY (test)" } } };
            var book = LoreBook.Parse(new[] { JsonUtility.ToJson(file) });
            book.Zones = new List<ZoneDefinition> { z };
            p = EncounterSession.FreshProgress(); book.Bind(p);
            return book;
        }

        [Test] public void Entries_unlock_by_doing_and_their_better_half_by_finding()
        {
            var z = TestZone(); var book = TestBook(z, out var p);
            var places = book.Lines(z, LoreBook.Places); Assert.AreEqual(2, places.Count, "Every landmark is a place.");
            var mill = places[0]; Assert.AreEqual("The Old Mill", mill.title); Assert.AreEqual("It grinds.", mill.plain, "The landmark's own line stands.");
            Assert.IsFalse(mill.Shown); Assert.IsTrue(mill.hasMore); Assert.IsFalse(mill.moreKnown);
            StringAssert.Contains("tells more", mill.moreHint);
            p.explored.Add(Achievements.PoiKey(z, z.landmarks[0]));
            mill = book.Lines(z, LoreBook.Places)[0]; Assert.IsTrue(mill.known); Assert.IsFalse(mill.Full, "Its page is not found yet.");
            p.documents.Add("doc.oakhaven.acorn_oath");
            mill = book.Lines(z, LoreBook.Places)[0]; Assert.IsTrue(mill.moreKnown); Assert.IsTrue(mill.Full);

            var ada = book.Lines(z, LoreBook.People).Single(); Assert.AreEqual("Miller, Testmoor.", ada.plain, "A resident's title when the file gives no line.");
            Assert.IsFalse(ada.known); Assert.IsEmpty(ada.notes, "Her stories wait until she has spoken with you.");
            Assert.IsTrue(book.Meet(z, "Ada Test"));
            ada = book.Lines(z, LoreBook.People).Single(); Assert.IsTrue(ada.known); Assert.AreEqual(2, ada.notes.Count, "What she told you.");
            p.discoveries.Add("secret.test.stone");
            Assert.IsTrue(book.Lines(z, LoreBook.People).Single().Full, "A find tells the rest.");

            var creatures = book.Lines(z, LoreBook.Creatures); Assert.AreEqual(1, creatures.Count, "A rare camp's kind is left out (no chapter waits on one).");
            Assert.IsFalse(creatures[0].known); StringAssert.Contains("Mill rats", creatures[0].notes[0], "Where it lives.");
            Assert.IsTrue(book.Slay(z, "Mill rat")); Assert.IsTrue(book.Lines(z, LoreBook.Creatures)[0].Full);

            var herbs = book.Lines(z, LoreBook.Herbs); CollectionAssert.AreEqual(new[] { "node.yarrow", "herb.moonbell" }, herbs.Select(h => h.subject).ToArray(), "Its node kinds, then its rare herbs.");
            Assert.IsTrue(book.Work(z, "node.yarrow"));
            p.discoveries.Add("secret.test.bells");
            Assert.IsTrue(book.Lines(z, LoreBook.Herbs).All(h => h.known), "A rare herb is known once its find is found.");
            p.explored.Add(Achievements.PoiKey(z, z.landmarks[1]));   // the other place
            var count = book.Count(z); Assert.AreEqual(count.total, count.full); Assert.IsTrue(book.Complete(z), "Every line known in full: the zone's chapter is complete.");
        }

        [Test] public void Doing_is_recorded_once_and_only_for_what_the_zone_has_and_places_count_as_before()
        {
            var z = TestZone(); var book = TestBook(z, out var p);
            Assert.IsFalse(book.Meet(z, "Nobody"), "Nobody of this zone."); Assert.IsFalse(book.Slay(z, "Dragon")); Assert.IsFalse(book.Work(z, "node.copper"));
            Assert.IsTrue(book.Meet(z, "Ada Test")); Assert.IsFalse(book.Meet(z, "Ada Test"), "Once.");
            Assert.IsTrue(book.Slay(z, "Mill rat")); Assert.IsFalse(book.Slay(z, "Mill rat"));
            Assert.IsTrue(book.MeetSim("sim-1")); Assert.IsFalse(book.MeetSim("sim-1"));
            CollectionAssert.AreEquivalent(new[] { "lore|met|zone.test|Ada Test", "lore|slain|zone.test|Mill rat", "lore|sim|sim-1" }, p.explored, "Kept in the places-explored list, under keys no place has.");
            var feats = new Achievements(p, new[] { z }, null);
            Assert.AreEqual(0, feats.ExploredIn(z), "No place counts as explored for them.");
            Assert.IsTrue(p.explored.All(k => k.StartsWith(LoreBook.KeyPrefix, StringComparison.Ordinal)));
            // A loaded character: the book reads that one.
            var other = EncounterSession.FreshProgress(); book.Bind(other);
            Assert.IsFalse(book.Lines(z, LoreBook.People).Single().known, "Another character has met nobody.");
        }

        [Test] public void Labels_are_one_of_the_four_and_canon_cites_its_chapter()
        {
            Assert.IsTrue(LoreBook.Labelled("CANON, book1 ch.4", out _));
            Assert.IsTrue(LoreBook.Labelled("CANON (book3 ch.6-7): the cold", out _));
            Assert.IsFalse(LoreBook.Labelled("CANON", out var why)); StringAssert.Contains("book and chapter", why);
            Assert.IsTrue(LoreBook.Labelled("CANON-EXPANDED: the Sandthrone are CANON (world_bible.md)", out _), "Expanded needs no chapter.");
            Assert.IsTrue(LoreBook.Labelled("GAME-ONLY", out _)); Assert.IsTrue(LoreBook.Labelled("PROVISIONAL foreshadowing", out _));
            Assert.IsFalse(LoreBook.Labelled("canon-ish", out _)); Assert.IsFalse(LoreBook.Labelled("", out _)); Assert.IsFalse(LoreBook.Labelled(null, out _));
        }

        [Test] public void Bad_lore_files_are_caught()
        {
            var z = TestZone();
            var bad = new LoreFile { zone = z.id, entries = new[] {
                new LoreEntry { id = "lore.test.places.mill", chapter = LoreBook.Places, subject = "The Old Mill", line = "x", canonStatus = "GAME-ONLY" },
                new LoreEntry { id = "lore.test.places.mill", chapter = LoreBook.Places, subject = "Crow Hill", line = "x", canonStatus = "GAME-ONLY" },
                new LoreEntry { id = "lore.other.x", chapter = "songs", subject = "The Old Mill", line = "x", canonStatus = "GAME-ONLY" },
                new LoreEntry { id = "lore.test.nowhere", chapter = LoreBook.Places, subject = "Atlantis", line = "x", canonStatus = "GAME-ONLY" },
                new LoreEntry { id = "lore.test.quiet", chapter = LoreBook.Creatures, subject = "Mill rat", canonStatus = "GAME-ONLY" },
                new LoreEntry { id = "lore.test.more", chapter = LoreBook.People, subject = "Ada Test", more = "Something.", canonStatus = "GAME-ONLY" },
                new LoreEntry { id = "lore.test.page", chapter = LoreBook.Herbs, subject = "node.yarrow", line = "Yarrow.", page = "doc.nowhere", secret = "secret.nowhere", canonStatus = "GAME-ONLY" },
                new LoreEntry { id = "lore.test.canon", chapter = LoreBook.Herbs, subject = "herb.moonbell", line = "Mention the bells.", canonStatus = "CANON" },
                new LoreEntry { id = "lore.test.tells", chapter = LoreBook.Creatures, subject = "Quiet One", line = "It sits by The Bells.", canonStatus = "GAME-ONLY" } } };
            var all = string.Join("\n", LoreBook.Validate(new[] { bad, new LoreFile { zone = "zone.nowhere" } }, new[] { z }, LootTestData.Quests()));
            foreach (var expected in new[] { "for 'zone.nowhere', which is no zone", "its id is used twice", "its id should be lore.test.<slug>", "unknown chapter 'songs'",
                "'Atlantis' is nothing in Testmoor's places", "says nothing", "its better half needs a page or a secret", "a page or secret with no better half",
                "unknown page 'doc.nowhere'", "unknown secret 'secret.nowhere'", "CANON without a book and chapter", "its line names the hidden find 'The Bells'" })
                StringAssert.Contains(expected, all);
            StringAssert.DoesNotContain("'Quiet One' is nothing", all, "A rare camp's kind may be written.");
        }

        [Test] public void A_zone_chapter_filled_gives_its_achievement_and_title()
        {
            var z = TestZone(); var book = TestBook(z, out var p);
            var feats = new Achievements(p, new[] { z }, null, book); string title = null; feats.Earned = a => title = a.title;
            var deed = feats.All.Single(a => a.id == "lore.test");
            Assert.AreEqual("Chronicler of Testmoor", deed.title); Assert.AreEqual(book.Count(z).total, deed.goal);
            Assert.IsTrue(feats.All.Any(a => a.id == "lore.all" && a.title == "the Rememberer"));
            Assert.IsEmpty(feats.Check().Where(a => a.id.StartsWith("lore.")), "Nothing known yet.");
            foreach (var l in z.landmarks) p.explored.Add(Achievements.PoiKey(z, l));
            book.Meet(z, "Ada Test"); book.Slay(z, "Mill rat"); book.Work(z, "node.yarrow");
            p.documents.Add("doc.oakhaven.acorn_oath"); p.discoveries.Add("secret.test.stone"); p.discoveries.Add("secret.test.bells");
            var got = feats.Check().Select(a => a.id).ToList();
            CollectionAssert.IsSubsetOf(new[] { "lore.test", "lore.all" }, got);
            Assert.IsTrue(feats.Wear("Chronicler of Testmoor"));
            Assert.IsNotNull(new Achievements(p, new[] { z }, null).All, "Without a book there are no lore deeds, and nothing breaks.");
            Assert.IsFalse(new Achievements(p, new[] { z }, null).All.Any(a => a.id.StartsWith("lore.")));
        }

        [Test] public void The_real_book_knows_the_zones_and_reads_a_real_find()
        {
            var book = RealBook(out var p);
            var oak = book.Zone("zone.oakhaven"); Assert.NotNull(oak);
            Assert.AreEqual(Achievements.Pois(oak).Count(), book.Lines(oak, LoreBook.Places).Count, "Every Oakhaven landmark is a place in the book.");
            var oakLine = book.Lines(oak, LoreBook.Places).Single(l => l.subject == "The Great Oak");
            Assert.IsTrue(oakLine.hasMore); Assert.IsFalse(oakLine.Shown);
            p.explored.Add(Achievements.PoiKey(oak, oak.landmarks.First(l => l.name == "The Great Oak"))); p.documents.Add("doc.oakhaven.acorn_oath");
            Assert.IsTrue(book.Lines(oak, LoreBook.Places).Single(l => l.subject == "The Great Oak").Full);
            Assert.Greater(book.FactionLines().Count, 3, "The Standing tab's factions.");
            // A creature's notes say where it lives and what can fall to it.
            Assert.IsTrue(book.Slay(oak, "Old Whitefoot"));
            var wolf = book.Lines(oak, LoreBook.Creatures).Single(l => l.subject == "Old Whitefoot");
            Assert.IsTrue(wolf.known); StringAssert.StartsWith("Found at ", wolf.notes[0]);
            // A node's notes say its trade and what it makes.
            var copper = book.Lines(oak, LoreBook.Herbs).Single(l => l.subject == "node.copper");
            Assert.IsTrue(copper.notes.Any(n => n.StartsWith("Mining 1")), string.Join(" | ", copper.notes));
            Assert.IsTrue(copper.notes.Any(n => n.StartsWith("Made into ")), string.Join(" | ", copper.notes));
            Assert.IsNull(LoreBook.ParseFile("not json")); Assert.IsNull(LoreBook.ParseFile("{\"entries\":[]}"), "A file naming no zone is left out.");
        }
    }
}
