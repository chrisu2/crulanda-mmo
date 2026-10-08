using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// Gear looks (loot step A1): the looks file is whole; every generated item, crafted piece and named item gets a real look
    /// (never the fallback), the same id always the same one; generated gear's variant comes from its seed and is never a
    /// named-only variant; quality raises the trim and only rare and epic glow; the level band sets the tier; look strings parse
    /// strictly.
    /// </summary>
    public class GearLookTests
    {
        static GearLooks Looks()
        {
            var text = Resources.Load<TextAsset>("Gear/looks"); Assert.NotNull(text, "Resources/Gear/looks.json is there.");
            return GearLooks.Parse(text.text);
        }
        /// <summary>The Blacksmith's 21 pieces (professions DESIGN.md 5.2) and their slots, before any of them exists in items.json.</summary>
        static readonly (string id, string slot)[] Crafted = {
            ("craft.copper_cudgel", "mainhand"), ("craft.copper_buckler", "offhand"), ("craft.copper_gauntlets", "hands"), ("craft.copper_jerkin", "chest"),
            ("craft.bogiron_hatchet", "mainhand"), ("craft.bogiron_helm", "head"), ("craft.bogiron_greaves", "legs"), ("craft.bogiron_hauberk", "chest"),
            ("craft.ridgesteel_blade", "mainhand"), ("craft.ridgesteel_shield", "offhand"), ("craft.ridgesteel_pauldrons", "shoulders"), ("craft.ridgesteel_cuirass", "chest"),
            ("craft.ashsteel_cleaver", "mainhand"), ("craft.ashsteel_helm", "head"), ("craft.ashsteel_sabatons", "feet"), ("craft.ashsteel_hauberk", "chest"),
            ("craft.veridian_warblade", "mainhand"), ("craft.veridian_shield", "offhand"), ("craft.veridian_legplates", "legs"), ("craft.veridian_breastplate", "chest"),
            ("craft.heartwood_greatblade", "mainhand")
        };
        static IEnumerable<ItemDef> Generated(ItemDatabase db, int seeds)
        {
            foreach (var slot in ItemDatabase.SlotIds)
                for (int level = 1; level <= EncounterProgress.LevelCap; level++)
                    for (int q = 0; q <= 4; q++)
                        for (int seed = 0; seed < seeds; seed++) yield return db.Get(ItemDatabase.GearId(slot, level, q, seed));
        }
        static ItemDef Named(GearLooks looks, string look, string slot, int quality)
        {
            string id = "test.look." + look + "." + quality; looks.Register(id, look);
            return new ItemDef { id = id, name = look, kind = "gear", slot = slot, quality = quality };
        }
        static float Luma(Color c) { return c.r * .3f + c.g * .59f + c.b * .11f; }

        [Test] public void Looks_file_parses_and_every_family_and_palette_named_exists()
        {
            var looks = Looks();   // Parse refuses unknown families, palettes and variants anywhere in the file
            CollectionAssert.IsSubsetOf(new[] { "oakhaven", "concord", "sandthrone", "khaven", "tollroad", "pilgrim", "ashwalker", "cult", "veridian", "pale" }, new List<string>(looks.PaletteIds));
            Assert.AreEqual(58, GearLooks.Families.Length, "54 shape families (DESIGN.md 2.3), the three model families (2026-10-05) and model.helm (2026-10-07).");
            int variants = 0; var names = new HashSet<string>();
            foreach (var f in GearLooks.Families)
            {
                Assert.IsTrue(names.Add(f.name), "One " + f.name + ".");
                Assert.GreaterOrEqual(ItemDatabase.SlotIndex(f.slot), 0, f.name + " is in a real slot.");
                Assert.That(f.gen, Is.InRange(0, f.variants.Length), f.name + " lets generated gear use some of its variants.");
                Assert.AreEqual(f.variants.Length, new HashSet<string>(f.variants).Count, f.name + " names each variant once.");
                if (!f.name.StartsWith("model.")) variants += f.variants.Length;   // the drawn ones (the models are the packs')
            }
            Assert.AreEqual(146, variants, "146 variants (DESIGN.md 2.3).");
            foreach (var kv in looks.Explicit) Assert.IsTrue(looks.IsValid(kv.Value, out var why), kv.Key + ": " + why);
            foreach (var slot in ItemDatabase.SlotIds) Assert.IsFalse(looks.Resolve(new ItemDef { id = "test.nothing", slot = slot }).family == null, slot + " has a fallback.");
        }

        [Test] public void Every_generated_name_resolves_to_a_look()
        {
            var looks = Looks(); var db = new ItemDatabase(); int n = 0;
            foreach (var d in Generated(db, 40))
            {
                Assert.NotNull(d); var l = looks.Resolve(d); n++;
                Assert.IsFalse(l.fallback, d.id + " (" + d.name + ") found no look: its words are missing from looks.json.");
                Assert.AreEqual(d.slot, GearLooks.Family(l.family).slot, d.id + " wears a " + d.slot + " family.");
            }
            Assert.AreEqual(9 * EncounterProgress.LevelCap * 5 * 40, n);   // nine words, every level, five qualities, forty draws
        }

        [Test] public void Every_crafted_piece_has_a_look()
        {
            var looks = Looks();
            foreach (var (id, slot) in Crafted)
            {
                var l = looks.Resolve(new ItemDef { id = id, name = id, kind = "gear", slot = slot, quality = id.Contains("heartwood") ? 3 : 2 });
                Assert.IsFalse(l.fallback, id + " has its own look.");
                Assert.AreEqual(slot, GearLooks.Family(l.family).slot, id);
            }
            // Each metal tier has its tint.
            ColorUtility.TryParseHtmlString("#B87345", out var copper); ColorUtility.TryParseHtmlString("#3E3B3D", out var ash);
            Assert.That(Vector4.Distance(copper, looks.Resolve(new ItemDef { id = "craft.copper_cudgel", slot = "mainhand", quality = 2 }).metal), Is.LessThan(.01f), "Copper.");
            Assert.That(Vector4.Distance(ash, looks.Resolve(new ItemDef { id = "craft.ashsteel_cleaver", slot = "mainhand", quality = 2 }).metal), Is.LessThan(.01f), "Ash-steel.");
            Assert.IsTrue(looks.Resolve(new ItemDef { id = "craft.heartwood_greatblade", slot = "mainhand", quality = 3 }).forceGlow, "The capstone glows.");
        }

        [Test] public void Named_items_already_in_the_game_have_their_own_looks()
        {
            var looks = Looks();
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Items");
            var texts = new List<string>(); foreach (var f in Directory.GetFiles(dir, "*.json")) texts.Add(File.ReadAllText(f));
            var db = ItemDatabase.Parse(texts); int named = 0;
            foreach (var d in db.Items.Values)
            {
                if (d.kind != "gear" || !d.id.StartsWith("item.")) continue;
                named++; var l = looks.Resolve(d);
                Assert.IsFalse(l.fallback, d.id + " has its own look.");
                Assert.AreEqual(d.slot, GearLooks.Family(l.family).slot, d.id);
            }
            Assert.GreaterOrEqual(named, 12);
            Assert.AreEqual("offhand.hung", looks.Resolve(db.Get("item.hesks_lantern")).family, "Hesk's lantern hangs from the hand.");
        }

        [Test] public void Same_id_always_gives_the_same_look()
        {
            var a = Looks(); var b = Looks(); var dbA = new ItemDatabase(); var dbB = new ItemDatabase();
            foreach (var d in Generated(dbA, 8))
            {
                var other = dbB.Get(d.id);
                Assert.AreEqual(GearLooks.LookKey(a.Resolve(d)), GearLooks.LookKey(b.Resolve(other)), d.id);
                Assert.AreEqual(GearLooks.LookKey(a.Resolve(d)), GearLooks.LookKey(a.Resolve(d)), d.id);
            }
            // Two looks that differ only in their detail (a trim band on the grip, or none) build different parts, so keep apart.
            var plain = a.Resolve(dbA.Get(ItemDatabase.GearId("mainhand", 5, 2, 0))); var banded = plain; plain.detail = "none"; banded.detail = "band";
            Assert.AreNotEqual(GearLooks.LookKey(plain), GearLooks.LookKey(banded), "The detail is part of the key.");
            // Two items with the same words differ at most in the variant (the seed), never in family or palette.
            var seen = new Dictionary<string, GearLook>();
            foreach (var d in Generated(dbA, 40))
            {
                if (d.slot != "mainhand" || !GearLooks.TrySplitGenerated(d, out var m, out var p, out int level, out _)) continue;
                string key = m + " " + p + " " + GearLooks.BandOf(level) + " " + d.quality; var l = a.Resolve(d);
                if (seen.TryGetValue(key, out var first)) { Assert.AreEqual(first.family, l.family, d.id); Assert.AreEqual(first.palette, l.palette, d.id); }
                else seen[key] = l;
            }
        }

        [Test] public void Variant_comes_from_the_seed_and_generated_gear_never_gets_a_named_variant()
        {
            var looks = Looks(); var db = new ItemDatabase(); var variantsSeen = new Dictionary<string, HashSet<string>>();
            foreach (var d in Generated(db, 70))
            {
                var l = looks.Resolve(d); var fam = GearLooks.Family(l.family); int index = Array.IndexOf(fam.variants, l.variant);
                if (l.family.StartsWith("model.")) { Assert.GreaterOrEqual(index, 0, d.id + ": its model is one of the family's."); continue; }   // chosen by quality (GearLooks.GeneratedModel)
                GearLooks.TrySplitGenerated(d, out _, out _, out _, out int seed);
                Assert.That(index, Is.InRange(0, fam.gen - 1), d.id + " got " + l.family + ":" + l.variant + ", which is for named items only.");
                Assert.AreEqual(seed / 7 % fam.gen, index, d.id + ": variant = (seed / 7) % gen.");
                if (!variantsSeen.TryGetValue(l.family, out var set)) variantsSeen[l.family] = set = new HashSet<string>();
                set.Add(l.variant);
            }
            Assert.AreEqual(3, variantsSeen["sword.arming"].Count, "Arming swords come straight, curved and disc-guarded.");
            Assert.AreEqual(3, variantsSeen["club"].Count, "Clubs come plain, studded and bound.");
        }

        [Test] public void Quality_raises_trim_and_only_rare_and_epic_glow()
        {
            var looks = Looks(); var q = new GearLook[5];
            for (int i = 0; i <= 4; i++) q[i] = looks.Resolve(Named(looks, "sword.arming:straight/tollroad", "mainhand", i));
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 2 }, Array.ConvertAll(q, l => l.accents), "Glowing accents: none, none, none, one, two.");
            CollectionAssert.AreEqual(new[] { false, true, true, true, true }, Array.ConvertAll(q, l => l.hasTrim), "Poor has no trim.");
            for (int i = 2; i <= 4; i++) Assert.Greater(q[i].trimMetal, q[i - 1].trimMetal, "Better trim at " + ItemDatabase.QualityNames[i] + ".");
            Assert.IsTrue(q[0].worn); Assert.IsFalse(q[1].worn); Assert.IsFalse(q[1].extra); Assert.IsTrue(q[2].extra);
            CollectionAssert.AreEqual(new[] { false, false, false, false, true }, Array.ConvertAll(q, l => l.pulse), "Only epic pulses.");
            Assert.Less(Luma(q[0].cloth), Luma(q[1].cloth), "Poor is faded and darker.");
            Assert.Greater(q[4].glowPower, q[3].glowPower);
            // +glow lights an accent whatever the quality.
            var forced = looks.Resolve(Named(looks, "knife:glass/pale+glow", "mainhand", 1));
            Assert.IsTrue(forced.forceGlow); Assert.AreEqual(1, forced.accents);
            // Generated gear follows the same rule.
            var db = new ItemDatabase();
            foreach (var d in Generated(db, 3)) Assert.AreEqual(d.quality >= 3, looks.Resolve(d).accents > 0, d.id);
        }

        [Test] public void Tier_follows_the_item_level()
        {
            var looks = Looks(); var db = new ItemDatabase();
            for (int level = 1; level <= EncounterProgress.LevelCap; level++)
                Assert.AreEqual(GearLooks.BandOf(level), looks.Resolve(db.Get(ItemDatabase.GearId("chest", level, 2, 3))).tier, "Generated gear's tier is its level band (level " + level + ").");
            var named = new ItemDef { id = "craft.veridian_breastplate", name = "Breastplate", kind = "gear", slot = "chest", quality = 2, level = 14 };   // curve 15: the top band (round 29)
            Assert.AreEqual(4, looks.Resolve(named).tier, "A named piece's tier comes from its curve level, the required level + 1.");
            named.level = 1; Assert.AreEqual(0, looks.Resolve(named).tier);
            Assert.AreNotEqual(GearLooks.LookKey(looks.Resolve(db.Get(ItemDatabase.GearId("chest", 2, 2, 3)))), GearLooks.LookKey(looks.Resolve(db.Get(ItemDatabase.GearId("chest", 13, 2, 3)))), "The tier is part of the key.");
        }

        [Test] public void Unknown_item_falls_back_to_the_slot_default()
        {
            var looks = Looks();
            var shield = looks.Resolve(new ItemDef { id = "item.nothing_like_it", name = "Nothing", slot = "offhand", quality = 2 });
            Assert.IsTrue(shield.fallback); Assert.AreEqual("shield.round", shield.family); Assert.AreEqual("oakhaven", shield.palette);
            Assert.AreEqual("sword.arming", looks.Resolve(null).family, "No item at all: a main-hand fallback, never a crash.");
            Assert.IsTrue(looks.Resolve(new ItemDef { id = "gen.chest.3.1.5", name = "Odd Thing", slot = "chest" }).fallback, "Words the file does not know.");
            looks.Register("test.wrong_slot", "sword.short:plain/oakhaven");
            var wrong = looks.Resolve(new ItemDef { id = "test.wrong_slot", slot = "head" });
            Assert.IsTrue(wrong.fallback, "A look for another slot is ignored."); Assert.AreEqual("head.cap", wrong.family);
        }

        [Test] public void Look_strings_parse_and_reject_unknown_families()
        {
            Assert.IsTrue(GearLooks.TryParseLook("shield.round:hide/sandthrone", out var f, out var v, out var p, out bool g));
            Assert.AreEqual(("shield.round", "hide", "sandthrone", false), (f, v, p, g));
            Assert.IsTrue(GearLooks.TryParseLook("knife:glass/pale+glow", out f, out v, out p, out g)); Assert.AreEqual(("knife", "glass", "pale", true), (f, v, p, g));
            Assert.IsTrue(GearLooks.TryParseLook("club/oakhaven", out f, out v, out _, out _)); Assert.AreEqual("plain", v, "No variant: the family's first.");
            foreach (var bad in new[] { "sword.spork/oakhaven", "sword.arming:wavy/oakhaven", "sword.arming", "sword.arming:/oakhaven", "/oakhaven", "sword.arming:straight/", "sword.arming:straight/oak/haven", "sword.arming:straight:x/oakhaven", "sword.arming/oakhaven+shine", "", null })
                Assert.IsFalse(GearLooks.TryParseLook(bad, out _, out _, out _, out _), "Refused: '" + bad + "'.");
            var looks = Looks();
            Assert.IsFalse(looks.IsValid("sword.arming/nowhere", out var why)); StringAssert.Contains("nowhere", why);
            Assert.Throws<ArgumentException>(() => looks.Register("test.bad", "axe.giant/oakhaven"));
            var e = Assert.Throws<ArgumentException>(() => GearLooks.Parse("{\"palettes\":[{\"id\":\"oakhaven\",\"cloth\":\"#000000\",\"cloth2\":\"#000000\",\"leather\":\"#000000\",\"metal\":\"#000000\",\"trim\":\"#000000\",\"wood\":\"#000000\",\"glow\":\"#000000\"}],"
                + "\"fallbacks\":[{\"slot\":\"mainhand\",\"family\":\"axe.giant\"}],\"looks\":[{\"item\":\"x\",\"look\":\"sword.spork/oakhaven\"}]}"));
            StringAssert.Contains("axe.giant", e.Message); StringAssert.Contains("sword.spork", e.Message); StringAssert.Contains("No fallback family for slot 'head'", e.Message);
        }

        /// <summary>Playtest note 13 ("need high fantasy not pale"): a shade-1 material word wears the palette's second dye, the
        /// palette id (and so the saved appearance key) is unchanged, rarer cloth is richer, and poor cloth is drab.</summary>
        [Test] public void Shade_words_wear_the_palettes_other_dyes_and_quality_deepens_the_cloth()
        {
            var looks = Looks();
            GearLook Of(string name, int q) { return looks.Resolve(new ItemDef { id = "gen.chest.5." + q + ".14", name = name, kind = "gear", slot = "chest", quality = q, level = 4 }); }
            ColorUtility.TryParseHtmlString("#8A3212", out var russet);
            Assert.That(Vector4.Distance(russet, Of("Frayed Tunic", 2).cloth), Is.LessThan(.01f), "Frayed (shade 1) wears oakhaven's second dye.");
            Assert.AreEqual(Of("Homespun Tunic", 2).palette, Of("Frayed Tunic", 2).palette, "The palette id, and so the saved appearance key, is unchanged.");
            float Sat(Color c) { Color.RGBToHSV(c, out _, out float sat, out _); return sat; }
            for (int q = 2; q <= 4; q++) Assert.GreaterOrEqual(Sat(Of("Homespun Tunic", q).cloth), Sat(Of("Homespun Tunic", q - 1).cloth), "Richer cloth at quality " + q + ".");
            Assert.Less(Sat(Of("Homespun Tunic", 0).cloth), Sat(Of("Homespun Tunic", 1).cloth), "Poor is drab.");
        }
    }
}
