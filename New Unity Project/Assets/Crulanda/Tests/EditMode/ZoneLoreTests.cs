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
    /// The clues, hints, stories and lore pages of 2026-10-15 (ZoneLore; Chris: "still havent found many secrets", "stories from npcs",
    /// "book of lore"): every secret in every zone has a clue for the book and a hint for a villager, neither naming a landmark; a
    /// hint speaks only of what can be found now; every named person has two or three stories and nobody else has any; the side
    /// file merges into its zone without overriding it; the book lists a clue for each unfound secret only; and every page of the
    /// book of lore lies at a hidden find.
    /// </summary>
    public class ZoneLoreTests
    {
        static string Content(string sub) { return Path.Combine(Application.dataPath, "Crulanda", sub); }
        static List<string> Texts(string folder) { return Directory.GetFiles(Content(Path.Combine("EncounterContent", folder)), "*.json").OrderBy(f => f).Select(File.ReadAllText).ToList(); }
        /// <summary>Every zone, its side file (Resources/Lore/&lt;zone&gt;.json) merged in the way the game does it.</summary>
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
        static ZoneSecret Secret(string slug, string kind, string hint, string needs = null)
        {
            return new ZoneSecret { id = "secret.test." + slug, name = "The " + slug, kind = kind, prompt = "Search", radius = 3, hint = hint, needs = needs, canonStatus = "GAME-ONLY (test)" };
        }

        // ---------- the data ----------
        [Test] public void Every_secret_has_a_clue_and_a_hint_and_none_names_a_landmark()
        {
            var zones = Zones(); var problems = new List<string>();
            foreach (var z in zones)
                foreach (var s in z.secrets ?? new ZoneSecret[0])
                {
                    if (s == null) continue;
                    if (string.IsNullOrEmpty(s.clue)) problems.Add(s.id + ": no clue for the book.");
                    if (string.IsNullOrEmpty(s.hint)) problems.Add(s.id + ": no hint for a villager.");
                    if (!string.IsNullOrEmpty(s.clue) && s.clue == s.hint) problems.Add(s.id + ": the clue and the hint are the same line.");
                }
            // Validate names a clue or hint that carries a landmark's name (the map would give the spot away).
            problems.AddRange(DiscoveryLog.Validate(zones, ItemDatabase.Parse(Texts("Items")), QuestDatabase.Parse(Texts("Quests"))));
            Assert.IsEmpty(problems, string.Join("\n", problems));
            Assert.IsTrue(DiscoveryLog.Mentions("Lift the stone at the Watchtower's foot", "The Watchtower"), "In any case.");
            Assert.IsFalse(DiscoveryLog.Mentions("the tower east of the village", "The Watchtower"));
            // Secrets a zone (2026-10-15: two lore pages each in Khaven, the Peaks, the Rim and the Shore; Oakhaven's fifteen grew to 25
            // the same night by another hand, and may grow on: each new one needs its lines in Resources/Lore/oakhaven.json, which
            // the check above names). Phase 8.6 step 8 (decision 7, about 90 in all): three more each in Khaven, the Peaks and the Rim,
            // three on the Shore and three in its Root-Mother's Deep; the Sealed Adit's thirteen have their lines in Resources/Lore/adit.json.
            var counts = zones.ToDictionary(z => z.id, z => (z.secrets ?? new ZoneSecret[0]).Count(s => s != null));
            Assert.GreaterOrEqual(counts["zone.oakhaven"], 25); Assert.AreEqual(13, counts["zone.khaven"]); Assert.AreEqual(13, counts["zone.peaks"]);
            Assert.AreEqual(13, counts["zone.ashrim"]); Assert.AreEqual(14, counts["zone.verdant"]); Assert.AreEqual(13, counts["zone.adit"]);
        }

        [Test] public void Every_named_person_in_every_zone_has_two_or_three_stories_and_nobody_else_has_any()
        {
            var problems = new List<string>();
            foreach (var z in Zones())
            {
                if (z.dungeon || z.life == null) continue;
                var people = new List<string>();
                people.AddRange(VillageLife.VillagerNames(z.life).Take(z.life.villagers));
                people.AddRange((z.life.residents ?? new ZoneResident[0]).Where(r => r != null).Select(r => r.name));
                int coops = (z.props ?? new ZoneProp[0]).Count(p => p != null && p.kind == "coop"), folds = (z.life.critters ?? new ZoneCritters[0]).Count(c => c != null && c.kind == "sheep");
                people.AddRange(VillageLife.KeeperNames.Take(coops)); people.AddRange(VillageLife.ShepherdNames.Take(folds));
                foreach (var name in people)
                {
                    var lines = ZoneLore.StoriesOf(z, name);
                    if (lines == null) { problems.Add(z.id + ": " + name + " has no stories."); continue; }
                    if (lines.Length < 2 || lines.Length > 3) problems.Add(z.id + ": " + name + " has " + lines.Length + " stories, not two or three.");
                    if (lines.Any(string.IsNullOrWhiteSpace)) problems.Add(z.id + ": " + name + " has a blank story.");
                }
                foreach (var name in ZoneLore.StoryNames(z)) if (!people.Contains(name)) problems.Add(z.id + ": stories for " + name + ", who is nobody here.");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test] public void Every_page_of_the_book_of_lore_lies_at_a_hidden_find()
        {
            var quests = QuestDatabase.Parse(Texts("Quests")); var zones = Zones();
            var lore = quests.Documents.Values.Where(d => d.IsLore).Select(d => d.id).ToList();
            Assert.AreEqual(20, lore.Count, "Twenty pages of the book of lore: two each in Khaven, the Peaks, the Rim and the Shore (2026-10-15), six at the books, shelves and dead of Phase 8.6, and the Crown's six by its halls (2026-10-16).");
            var held = new Dictionary<string, ZoneSecret>();
            foreach (var z in zones) foreach (var s in z.secrets ?? new ZoneSecret[0]) if (s != null && !string.IsNullOrEmpty(s.document)) held[s.document] = s;
            foreach (var id in lore)
            {
                Assert.IsTrue(held.ContainsKey(id), id + " lies at no hidden find.");
                CollectionAssert.Contains(new[] { "note", "read", "remains" }, held[id].kind, id + ": a page lies in a note, a book to read or a dead adventurer's satchel (there is no 'page' kind).");
                Assert.IsFalse(string.IsNullOrEmpty(quests.Documents[id].canonStatus), id + ": a lore page says what it is.");
            }
            Assert.IsFalse(quests.Documents["doc.khaven.low_row"].IsLore, "A Chronicle page is not lore.");
        }

        // ---------- the rules ----------
        [Test] public void A_hint_speaks_only_of_what_can_be_found_now()
        {
            var key = Secret("key", "key", "the key"); var chest = Secret("chest", "chest", "the chest", key.id);
            var view = Secret("view", "vista", "the view"); var mute = Secret("mute", "cache", null);
            var z = new ZoneDefinition { id = "zone.test", secrets = new[] { key, chest, view, mute, null } };
            var log = new DiscoveryLog(EncounterSession.FreshProgress(), null, null);
            var rng = new System.Random(3);
            HashSet<string> Draws() { var set = new HashSet<string>(); for (int i = 0; i < 200; i++) { var h = ZoneLore.SecretHint(z, log, rng); if (h != null) set.Add(h); } return set; }
            CollectionAssert.AreEquivalent(new[] { "the key", "the view" }, Draws(), "The chest waits for its key; the mute one has nothing to say.");
            log.Discover(key);
            CollectionAssert.AreEquivalent(new[] { "the chest", "the view" }, Draws(), "Found things are not spoken of; the chest can be now.");
            log.Discover(chest); log.Discover(view);
            Assert.IsNull(ZoneLore.SecretHint(z, log, rng), "Everything with a hint is found.");
            Assert.AreEqual("the view", ZoneLore.Hint(view)); Assert.AreEqual("the view", ZoneLore.Clue(view), "The hint stands in for a missing clue, and the other way about.");
            Assert.IsNull(ZoneLore.Clue(mute));
            Assert.IsNotNull(ZoneLore.SecretHint(z, null, rng), "Without a log nothing is found yet.");
        }

        [Test] public void A_story_is_one_of_the_persons_own_lines_and_nobody_else_has_any()
        {
            var z = new ZoneDefinition { id = "zone.test", life = new ZoneLife {
                residents = new[] { new ZoneResident { name = "Quill", stories = new[] { "q1", "q2" } }, new ZoneResident { name = "Ivel" } },
                stories = new[] { new ZoneStory { name = "Pim", lines = new[] { "p1" } }, new ZoneStory { name = "Blank", lines = new[] { "" } } } } };
            var rng = new System.Random(5); var seen = new HashSet<string>();
            for (int i = 0; i < 50; i++) seen.Add(ZoneLore.Story(z, "Quill", rng));
            CollectionAssert.AreEquivalent(new[] { "q1", "q2" }, seen);
            Assert.AreEqual("p1", ZoneLore.Story(z, "Pim", rng));
            Assert.IsNull(ZoneLore.Story(z, "Ivel", rng), "A resident with no stories."); Assert.IsNull(ZoneLore.Story(z, "Nobody", rng)); Assert.IsNull(ZoneLore.Story(z, "Blank", rng));
            CollectionAssert.AreEquivalent(new[] { "Quill", "Pim" }, ZoneLore.StoryNames(z));
        }

        [Test] public void The_side_file_merges_into_its_zone_once_and_the_zones_own_text_wins()
        {
            var own = Secret("own", "cache", "own hint"); own.clue = "own clue"; var bare = Secret("bare", "cache", null);
            var z = new ZoneDefinition { id = "zone.oakhaven", secrets = new[] { own, bare }, life = new ZoneLife {
                residents = new[] { new ZoneResident { name = "Quill" }, new ZoneResident { name = "Ivel", stories = new[] { "mine" } } } } };
            var side = ZoneLore.ParseSide("{\"zone\":\"zone.oakhaven\",\"secrets\":[{\"id\":\"secret.test.own\",\"clue\":\"side clue\",\"hint\":\"side hint\"},{\"id\":\"secret.test.bare\",\"clue\":\"bare clue\",\"hint\":\"bare hint\"},{\"id\":\"secret.test.none\",\"clue\":\"x\"}]," +
                "\"stories\":[{\"name\":\"Quill\",\"lines\":[\"s1\"]},{\"name\":\"Ivel\",\"lines\":[\"side\"]},{\"name\":\"Pim\",\"lines\":[\"p1\"]}]}");
            Assert.NotNull(side); Assert.AreEqual("zone.oakhaven", side.zone);
            ZoneLore.Apply(z, side); ZoneLore.Apply(z, side);
            Assert.AreEqual("own clue", own.clue); Assert.AreEqual("own hint", own.hint); Assert.AreEqual("bare clue", bare.clue); Assert.AreEqual("bare hint", bare.hint);
            CollectionAssert.AreEqual(new[] { "s1" }, z.life.residents[0].stories, "The resident gets the side file's stories.");
            CollectionAssert.AreEqual(new[] { "mine" }, z.life.residents[1].stories, "The zone's own stories win.");
            Assert.AreEqual(1, z.life.stories.Length, "Pim, who is no resident, goes on life.stories; applied twice adds nothing."); Assert.AreEqual("Pim", z.life.stories[0].name);
            Assert.IsNull(ZoneLore.ParseSide("")); Assert.IsNull(ZoneLore.ParseSide("not json"));
            // The real side file is on disk, the game reads it from Resources, and every id in it is one of Oakhaven's.
            var real = ZoneLore.Side("zone.oakhaven"); Assert.NotNull(real, "Resources/Lore/oakhaven.json loads.");
            var oak = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Content("EncounterContent/Zones"), "oakhaven.json")));
            var ids = new HashSet<string>(oak.secrets.Select(s => s.id));
            foreach (var l in real.secrets) Assert.IsTrue(ids.Contains(l.id), l.id + " is not an Oakhaven secret.");
            StringAssert.Contains("GAME-ONLY", real.canonStatus);
            Assert.AreEqual("oakhaven", ZoneLore.Short("zone.oakhaven")); Assert.AreEqual("", ZoneLore.Short(null)); Assert.IsNull(ZoneLore.Side("zone.nowhere"));
        }

        [Test] public void The_book_lists_a_clue_for_each_unfound_secret_only()
        {
            var a = Secret("a", "vista", "clue a"); var b = Secret("b", "cache", "clue b"); var c = Secret("c", "herb", null);
            var log = new DiscoveryLog(EncounterSession.FreshProgress(), null, null);
            var t = log.TallyOf("zone.test", "Test", new[] { a, b, c });
            Assert.AreEqual(3, t.total); Assert.AreEqual(3, t.Hidden); CollectionAssert.AreEqual(new[] { "clue a", "clue b" }, t.clues, "A clue each for the unfound that have one.");
            log.Discover(b);
            t = log.TallyOf("zone.test", "Test", new[] { a, b, c });
            CollectionAssert.AreEqual(new[] { b }, t.found); CollectionAssert.AreEqual(new[] { "clue a" }, t.clues, "A found secret's clue is gone from the list.");
        }
    }
}
