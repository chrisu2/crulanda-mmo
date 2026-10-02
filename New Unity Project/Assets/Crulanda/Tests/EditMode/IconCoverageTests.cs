using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// Every item, recipe output, quest item, ability, talent and trade has a painted icon of its own under Resources/Icons
    /// (tools/art/make_icons.py), and generated gear resolves to a family-and-palette picture. The HUD falls back to letters
    /// for anything missing, so this is the test that keeps that fallback unused.
    /// </summary>
    public class IconCoverageTests
    {
        static string Content { get { return Path.Combine(Application.dataPath, "Crulanda", "EncounterContent"); } }
        static List<string> Texts(string folder) { return Directory.GetFiles(Path.Combine(Content, folder), "*.json").OrderBy(p => p, StringComparer.Ordinal).Select(File.ReadAllText).ToList(); }

        [SetUp] public void Fresh() { IconDb.Clear(); }

        [Test]
        public void EveryItemAndRecipeOutputHasItsOwnIcon()
        {
            var db = ItemDatabase.Parse(Texts("Items")); var missing = new List<string>();
            foreach (var d in db.Items.Values) if (IconDb.Load(IconDb.PathFor("item", d.id)) == null) missing.Add("item " + d.id);
            var professions = ProfessionDatabase.Parse(Texts("Professions"), db);
            foreach (var r in professions.Recipes) if (IconDb.Load(IconDb.PathFor("item", r.output)) == null) missing.Add("recipe output " + r.output + " (" + r.id + ")");
            Assert.IsEmpty(missing, "Icons missing (run tools/art/make_icons.py):\n" + string.Join("\n", missing));
        }

        [Test]
        public void EveryQuestItemHasItsOwnIcon()
        {
            var quests = QuestDatabase.Parse(Texts("Quests")); var missing = new List<string>();
            foreach (var id in quests.Items.Keys) if (IconDb.QuestItem(id) == null) missing.Add("quest item " + id);
            Assert.IsEmpty(missing, "Icons missing:\n" + string.Join("\n", missing));
        }

        [Test]
        public void EveryAbilityTalentAndTradeHasItsOwnIcon()
        {
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            Assert.NotNull(content); var missing = new List<string>();
            foreach (var a in content.abilities) if (IconDb.Ability(a.id) == null) missing.Add("ability " + a.id);
            foreach (var text in Texts("Talents"))
                foreach (var b in JsonUtility.FromJson<TalentTreeData>(text).branches)
                    foreach (var n in b.nodes) if (IconDb.Talent(n.id) == null) missing.Add("talent " + n.id);
            var db = ItemDatabase.Parse(Texts("Items"));
            foreach (var p in ProfessionDatabase.Parse(Texts("Professions"), db).Order) if (IconDb.Trade(p.id) == null) missing.Add("trade " + p.id);
            Assert.IsEmpty(missing, "Icons missing:\n" + string.Join("\n", missing));
        }

        [Test]
        public void GeneratedGearResolvesToAFamilyPictureNeverASlotOrKind()
        {
            var db = ItemDatabase.Parse(Texts("Items")); var missing = new HashSet<string>();
            foreach (var slot in ItemDatabase.SlotIds)
                for (int level = 1; level <= EncounterProgress.LevelCap; level++)
                    for (int seed = 0; seed < 40; seed++)
                    {
                        var d = db.Get(ItemDatabase.GearId(slot, level, 1, seed)); var l = GearLooks.Load().Resolve(d);
                        if (IconDb.Load("Icons/gear/" + l.family.Replace('.', '-') + "__" + l.palette) == null) missing.Add(l.family + "/" + l.palette);
                        Assert.NotNull(IconDb.Item(d), d.id);
                    }
            Assert.IsEmpty(missing, "Generated looks without a picture:\n" + string.Join("\n", missing));
            foreach (var slot in ItemDatabase.SlotIds) Assert.NotNull(IconDb.Slot(slot), "slot " + slot);
            foreach (var kind in new[] { "ore", "timber", "herb", "larder", "hide", "food", "potion", "tool", "bag", "junk", "material", "quest" }) Assert.NotNull(IconDb.Load("Icons/kind/" + kind), "kind " + kind);
        }

        [Test]
        public void IconsAreSmallSquaresWithoutMipmaps()
        {
            foreach (var t in Resources.LoadAll<Texture2D>("Icons"))
            {
                Assert.AreEqual(t.width, t.height, t.name + " is square.");
                Assert.LessOrEqual(t.width, 96, t.name + " is small.");
                Assert.AreEqual(1, t.mipmapCount, t.name + " has no mipmaps.");
            }
        }
    }
}
