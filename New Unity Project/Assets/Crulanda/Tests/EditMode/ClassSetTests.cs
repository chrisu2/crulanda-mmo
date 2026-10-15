using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Crulanda.Abilities;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The class sets (loot DESIGN.md 3.8; Chris, 2026-10-15: "set pieces like 6 pieces. 2/6 give 1 special, 4/6 another special
    /// ability 6/6 full set 3 special abilities and these are class specific"): one tier 1 (dungeon) set of six for every class, each
    /// piece from a different boss of the Sealed Adit; a piece only its class can wear; the specials switch on at 2, 4 and 6 worn and
    /// off again as pieces come off; every special names one of its class's own abilities; the new effect kinds are carried out
    /// (GearEffects.Adjust, PowerScale and Proc, the session's StartAbility hook); and a boss gives the pieces of the classes in the
    /// party, the player's own most often and certain on the pity-th dry kill. Pure logic and data: no scene, no session, no save.
    /// </summary>
    public class ClassSetTests
    {
        static ItemDatabase items; static LootDatabase loot; static List<ZoneDefinition> zones;
        static readonly string[] Slots = { "head", "shoulders", "chest", "hands", "legs", "feet" };
        [OneTimeSetUp] public void Load() { items = LootTestData.Items(); loot = LootTestData.Loot(items); zones = LootTestData.Zones(); }

        static SetDef SetFor(string cls, int tier = 1) { return loot.SetOrder.Single(s => s.cls == cls && s.tier == tier); }
        static void Wear(EncounterProgress p, params string[] ids)
        {
            Inventory.Ensure(p); for (int s = 0; s < p.equipment.Count; s++) p.equipment[s] = new ItemStack();
            foreach (var id in ids) p.equipment[ItemDatabase.SlotIndex(items.Get(id).slot)] = new ItemStack { item = id, count = 1 };
        }
        static LootContext Boss(string mob)
        {
            var z = zones.Single(x => x.id == "zone.adit"); var c = z.camps.First(x => x != null && x.elite && x.mob == mob);
            return new LootContext { zone = "adit", tag = LootContext.CampTag(c), mob = mob, level = c.levelMax, elite = true };
        }

        [Test] public void Every_class_has_one_tier_one_set_of_six_pieces_in_six_slots()
        {
            CollectionAssert.AreEquivalent(SimRoster.ClassIds, loot.SetOrder.Where(s => s.tier == 1 && !string.IsNullOrEmpty(s.cls)).Select(s => s.cls), "One dungeon set per class.");
            foreach (var cls in SimRoster.ClassIds)
            {
                var s = SetFor(cls);
                Assert.AreEqual("set.t1." + cls.Substring("class.".Length), s.id);
                Assert.AreEqual("Dungeon", s.tierName, s.id); Assert.AreEqual("GAME-ONLY", s.canonStatus, s.id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(s.name), s.id + " has a name.");
                Assert.AreEqual(6, s.pieces.Length, s.id + " has six pieces.");
                CollectionAssert.AreEquivalent(Slots, s.pieces.Select(p => items.Get(p).slot), s.id + ": head, shoulders, chest, hands, legs and feet.");
                CollectionAssert.AreEqual(new[] { 2, 4, 6 }, s.bonuses.Select(b => b.count), s.id + ": a special at 2, 4 and 6 worn.");
                Assert.IsTrue(s.bonuses.All(b => b.effects.Length == 1), s.id + ": one special each, three with the whole set.");
                foreach (var p in s.pieces)
                {
                    var d = items.Get(p); var g = loot.Meta(p);
                    Assert.AreEqual(cls, d.classOnly, p + " is its class's only.");
                    Assert.AreEqual("GAME-ONLY", d.canonStatus, p);
                    Assert.AreEqual(s.id, g.set, p + " names its set."); Assert.AreSame(s, loot.SetOf(p));
                    Assert.IsTrue(g.unique && g.boss, p + " is a unique boss piece.");
                    StringAssert.StartsWith("loot.set.t1_", p);
                }
                // A set reads as a set: one palette over all six pieces.
                var palettes = s.pieces.Select(p => { GearLooks.TryParseLook(loot.Meta(p).look, out _, out _, out var pal, out _); return pal; }).Distinct().ToList();
                Assert.AreEqual(1, palettes.Count, s.id + " wears one palette: " + string.Join(", ", palettes));
            }
        }

        [Test] public void Every_piece_comes_from_an_Adit_boss_on_its_class_set_group()
        {
            var adit = zones.Single(z => z.id == "zone.adit");
            List<string> bossesOfFirst = null;
            foreach (var cls in SimRoster.ClassIds)
            {
                var s = SetFor(cls); var bosses = new List<string>();
                foreach (var p in s.pieces)
                {
                    var g = loot.Meta(p); Assert.AreEqual("boss", LootDatabase.SourceKind(g.source), p);
                    string mob = g.source.Substring("boss:".Length); bosses.Add(mob);
                    Assert.IsTrue(adit.camps.Any(c => c != null && c.elite && c.mob == mob), p + ": " + mob + " is an elite of the Sealed Adit.");
                    Assert.AreNotEqual("Rail-Captain Orsk Danner", mob, p + ": not Danner (his place is moving with the Cove work).");
                    var groups = loot.Drops.Where(d => d.mob == mob && d.zone == "adit").SelectMany(d => d.groups).Where(gr => gr.pick.Any(k => k.item == p)).ToList();
                    Assert.AreEqual(1, groups.Count, p + " is on one group of " + mob + "'s."); Assert.IsTrue(groups[0].classSet, p + "'s group is a class set group.");
                    Assert.Greater(groups[0].pity, 0, p + ": a pity count, so the set takes runs, not luck alone.");
                }
                Assert.AreEqual(6, bosses.Distinct().Count(), s.id + ": each piece from a different boss.");
                if (bossesOfFirst == null) bossesOfFirst = bosses; else CollectionAssert.AreEqual(bossesOfFirst, bosses, s.id + ": the same boss gives the same slot to every class.");
            }
            var problems = LootDatabase.Validate(loot, zones, LootTestData.Quests());
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test] public void Every_special_works_on_one_of_its_own_classes_abilities()
        {
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            Assert.NotNull(content, "Encounter.asset loads.");
            foreach (var cls in SimRoster.ClassIds)
            {
                var def = content.FindClass(cls); Assert.NotNull(def, cls); var own = new HashSet<string>(def.definition.unlocks.Select(u => u.abilityId));
                foreach (var b in SetFor(cls).bonuses)
                    foreach (var e in b.effects)
                    {
                        CollectionAssert.Contains(LootDatabase.AbilityKinds, e.kind, cls + ": a special changes an ability, not a plain stat (" + e.text + ").");
                        Assert.IsTrue(e.ability == LootDatabase.AutoTrigger || own.Contains(e.ability), cls + ": '" + e.ability + "' is the class's own (" + e.text + ").");
                        if (!string.IsNullOrEmpty(e.other)) Assert.IsTrue(own.Contains(e.other), cls + ": '" + e.other + "' is the class's own.");
                        Assert.LessOrEqual(e.amount, GearEffects.Cap(e.kind, e.percent), e.text);
                    }
                var six = SetFor(cls).bonuses.Last().effects[0];
                CollectionAssert.Contains(LootDatabase.ProcKinds, six.kind, cls + ": the whole set's special is a proc on the class's core loop.");
            }
        }

        [Test] public void Specials_switch_on_at_two_four_and_six_and_off_as_pieces_come_off()
        {
            var s = SetFor("class.mage"); var p = EncounterSession.FreshProgress(); p.classId = "class.mage"; var source = new object();
            int On() { return GearEffects.Compute(p, items, loot, source).abilities.Count; }
            var expect = new[] { 0, 0, 1, 1, 2, 2, 3 };
            for (int n = 0; n <= 6; n++) { Wear(p, s.pieces.Take(n).ToArray()); Assert.AreEqual(expect[n], On(), n + " worn."); }
            StringAssert.Contains(s.name + " (6/6)", loot.TooltipLines(s.pieces[0], p));
            StringAssert.Contains("tier 1", loot.TooltipLines(s.pieces[0], p), "The tooltip names the set's tier.");
            for (int n = 6; n >= 0; n--) { Wear(p, s.pieces.Skip(6 - n).ToArray()); Assert.AreEqual(expect[n], On(), n + " worn, taking pieces off."); }
            Inventory.Add(p, items, s.pieces[0], 1); Inventory.Add(p, items, s.pieces[1], 1); Wear(p);
            Assert.AreEqual(0, On(), "Pieces in the bags do not count.");
            // Two sets of two (the mage's and, from an old save, the warrior's) are two first specials.
            Wear(p, s.pieces[0], s.pieces[1], SetFor("class.warrior").pieces[2], SetFor("class.warrior").pieces[3]);
            Assert.AreEqual(2, On());
        }

        [Test] public void A_set_piece_is_worn_only_by_its_class()
        {
            var piece = SetFor("class.rogue").pieces[0]; var d = items.Get(piece);
            var p = EncounterSession.FreshProgress(); Inventory.Ensure(p); p.classId = "class.mage";
            Inventory.Add(p, items, piece, 1); int bag = p.bag.FindIndex(x => x.item == piece);
            Assert.IsFalse(Inventory.Equip(p, items, bag, 13, out var why), "A mage cannot wear the rogue's piece.");
            Assert.AreEqual("Only a Rogue can wear that.", why);
            Assert.IsTrue(p.equipment.All(x => x.Empty || x.item != piece));
            Assert.IsFalse(LootJudge.IsUpgrade(d, null, 13, "class.mage"), "Another class's piece is never called an upgrade.");
            Assert.IsTrue(LootJudge.IsUpgrade(d, null, 13, "class.rogue"));
            p.classId = "class.rogue";
            Assert.IsTrue(Inventory.Equip(p, items, bag, 13, out why), why);
            Assert.AreEqual(piece, p.equipment[ItemDatabase.SlotIndex(d.slot)].item);
            Assert.IsFalse(Inventory.CanEquip(items.Get(SetFor("class.archivist").pieces[0]), 13, "class.rogue", out why)); Assert.AreEqual("Only an Archivist can wear that.", why);
            Assert.IsTrue(Inventory.CanEquip(d, 13, out _), "Without a class (the old call) only the level is checked.");
            Assert.IsTrue(Inventory.ClassMayWear(items.Get("loot.oak.whitefoot_mantle"), "class.mage"), "Ordinary gear is anyone's.");
        }

        [Test] public void Each_effect_kind_is_carried_out_by_the_hook()
        {
            var bolt = new AbilityDefinition { id = "test.bolt", name = "Bolt", power = 10, cost = 20, cooldown = 8, castTime = 1.5f, globalCooldown = 1.5f, range = 30 };
            var t = new GearEffectTotals();
            Assert.AreSame(bolt, GearEffects.Adjust(bolt, t), "Nothing worn: the ability itself.");
            t.abilities.Add(new GearEffect { kind = "abilityCost", ability = "test.bolt", amount = 5 });
            t.abilities.Add(new GearEffect { kind = "abilityCooldown", ability = "test.bolt", amount = 2 });
            t.abilities.Add(new GearEffect { kind = "abilityCastTime", ability = "test.bolt", amount = .5f });
            t.abilities.Add(new GearEffect { kind = "abilityPower", ability = "test.bolt", amount = 20 });
            var use = GearEffects.Adjust(bolt, t);
            Assert.AreEqual("test.bolt", use.id, "Same id: the same cooldown."); Assert.AreEqual(15, use.cost); Assert.AreEqual(6, use.cooldown, 1e-4f); Assert.AreEqual(1f, use.castTime, 1e-4f);
            Assert.AreEqual(20, bolt.cost, "The ability itself is not changed.");
            Assert.AreEqual(1.2f, GearEffects.PowerScale(t, "test.bolt"), 1e-4f); Assert.AreEqual(1f, GearEffects.PowerScale(t, "test.other"));
            t.abilities.Add(new GearEffect { kind = "abilityCost", ability = "test.bolt", amount = 50, percent = true });
            t.abilities.Add(new GearEffect { kind = "abilityCastTime", ability = "test.bolt", amount = 5 });
            use = GearEffects.Adjust(bolt, t);
            Assert.AreEqual(8, use.cost, "Flat first, then the percentage: (20 - 5) x 50%, rounded."); Assert.AreEqual(GearEffects.MinCastTime, use.castTime, 1e-4f, "A cast never drops under the floor.");
            Assert.AreSame(bolt.name, use.name); Assert.AreEqual(bolt.power, use.power, "Power is the scale's, not the copy's.");
            // The runtime honours the adjusted copy: cost, and a cooldown that is the same one as the ability's own.
            var runtime = new AbilityRuntime(); int spent = 0;
            Assert.AreEqual(AbilityStartResult.Started, runtime.TryStart(use, 0, c => { spent = c; return true; }, () => { }));
            Assert.AreEqual(8, spent); Assert.AreEqual(6, runtime.Remaining(bolt, 0), 1e-4f, "The cooldown the HUD reads (by the ability's own definition) is the shorter one.");
            // The procs: chance on the roll, resource, a cooldown cut, an echo; nothing on another trigger.
            t.abilities.Add(new GearEffect { kind = "procResource", ability = "test.bolt", amount = 12, chance = 25, text = "Grace: x" });
            t.abilities.Add(new GearEffect { kind = "procReset", ability = "test.bolt", other = "test.other", amount = 600, chance = 25, text = "Reset: x" });
            t.abilities.Add(new GearEffect { kind = "procEcho", ability = "test.bolt", amount = 100, chance = 25, text = "Echo: x" });
            t.abilities.Add(new GearEffect { kind = "procResource", ability = LootDatabase.AutoTrigger, amount = 8, chance = 20, text = "Nerve: x" });
            var fired = GearEffects.Proc(t, "test.bolt", () => .1);
            Assert.AreEqual(12, fired.resource); CollectionAssert.AreEqual(new[] { ("test.other", 600f) }, fired.resets); Assert.AreEqual(1f, fired.echo); Assert.AreEqual(3, fired.fired.Count);
            var missed = GearEffects.Proc(t, "test.bolt", () => .3);
            Assert.IsFalse(missed.Any, "Over the chance: nothing.");
            Assert.AreEqual(8, GearEffects.Proc(t, LootDatabase.AutoTrigger, () => 0).resource, "A landed swing fires the auto procs.");
            Assert.IsFalse(GearEffects.Proc(t, "test.other", () => 0).Any);
            runtime.Shorten("test.bolt", 600, 1); Assert.AreEqual(0, runtime.Remaining(bolt, 1.6f), 1e-4f, "A reset makes the ability ready at once.");
            // Every kind the sets use is one of these.
            foreach (var e in loot.SetOrder.Where(s => !string.IsNullOrEmpty(s.cls)).SelectMany(s => s.bonuses).SelectMany(b => b.effects))
                CollectionAssert.Contains(new[] { "abilityCost", "abilityCooldown", "abilityCastTime", "abilityPower", "procResource", "procReset", "procEcho" }, e.kind);
        }

        [Test] public void The_real_sets_change_the_real_abilities()
        {
            var p = EncounterSession.FreshProgress(); p.classId = "class.warrior"; var s = SetFor("class.warrior");
            Wear(p, s.pieces); var t = GearEffects.Compute(p, items, loot, new object());
            var bash = new AbilityDefinition { id = "ability.shield_bash", cost = 10, cooldown = 12, globalCooldown = 1.5f, range = 3.2f };
            Assert.AreEqual(9, GearEffects.Adjust(bash, t).cooldown, 1e-4f, "Two pieces: Shield Bash 3 s sooner.");
            Assert.AreEqual(1.2f, GearEffects.PowerScale(t, "ability.strike"), 1e-4f, "Four: Strike 20% harder.");
            Assert.AreEqual(10, GearEffects.Proc(t, LootDatabase.AutoTrigger, () => 0).resource, "Six: a landed swing may give 10 vigor.");
        }

        [Test] public void A_boss_gives_pieces_for_the_classes_in_the_party()
        {
            var lusk = Boss("Gang-Boss Haddo Lusk");
            string Hands(string cls) { return SetFor(cls).pieces.Single(x => items.Get(x).slot == "hands"); }
            bool IsClassPiece(LootDrop x) { var d = items.Get(x.item); return d != null && !string.IsNullOrEmpty(d.classOnly); }
            for (int seed = 0; seed < 200; seed++)
                Assert.IsFalse(loot.Roll(lusk, items, null, 0, null, new Random(seed)).Any(IsClassPiece), "Without the party's classes no class piece rolls (the older callers and tests).");
            int mine = 0, sims = 0, other = 0;
            var party = new List<string> { "class.warrior", "class.mage", "class.cleric" };
            for (int seed = 0; seed < 2000; seed++)
                foreach (var x in loot.Roll(lusk, items, null, 0, null, new Random(seed), null, party).Where(IsClassPiece))
                {
                    if (x.item == Hands("class.warrior")) mine++; else if (x.item == Hands("class.mage") || x.item == Hands("class.cleric")) sims++; else other++;
                }
            Assert.AreEqual(0, other, "Only the party's classes."); Assert.Greater(mine, sims * .9f, "The player's class three to the two sims' one each (3 : 2)."); Assert.Greater(sims, 0);
            float rate = (mine + sims) / 2000f; Assert.That(rate, Is.InRange(.33f, .47f), "About 40% of kills give a class piece.");
            // Owned: the player's piece leaves the pool; the sims' still drop.
            for (int seed = 0; seed < 300; seed++)
                Assert.IsFalse(loot.Roll(lusk, items, id => id == Hands("class.warrior"), 0, null, new Random(seed), null, party).Any(x => x.item == Hands("class.warrior")));
            // Pity: the player's own unowned piece is certain on the 8th kill without it.
            var pity = new List<LootLuck>(); int dry = 0, longest = 0; var rng = new Random(7);
            for (int kill = 0; kill < 400; kill++)
            {
                bool got = loot.Roll(lusk, items, null, 0, pity, rng, null, new List<string> { "class.warrior", "class.mage", "class.mage", "class.mage", "class.mage" }).Any(x => x.item == Hands("class.warrior"));
                dry = got ? 0 : dry + 1; longest = Math.Max(longest, dry);
            }
            Assert.Less(longest, 8, "Never eight kills in a row without the player's own piece.");
            Assert.IsTrue(pity.Exists(x => x.source.StartsWith("drop.set.t1.", StringComparison.Ordinal)), "The counter lives with the other pity counters (lootLuck: no new save field).");
        }

        [Test] public void The_parser_refuses_a_broken_class_set()
        {
            string Item(string id, string slot, string cls) { return "{\"id\":\"" + id + "\",\"name\":\"" + id + "\",\"kind\":\"gear\",\"slot\":\"" + slot + "\",\"quality\":3,\"level\":11,\"classOnly\":\"" + cls + "\"}"; }
            var db = ItemDatabase.Parse(new[] { "{\"items\":[" + Item("t.head", "head", "class.mage") + "," + Item("t.feet", "feet", "class.rogue") + "]}" });
            string File(string effect, string cls = "class.mage", string feet = "t.feet")
            {
                return "{\"gear\":[{\"id\":\"t.head\",\"look\":\"head.cap:plain/oakhaven\",\"source\":\"boss:X\",\"set\":\"s\"},{\"id\":\"" + feet + "\",\"look\":\"feet.shoes:plain/oakhaven\",\"source\":\"boss:X\",\"set\":\"s\"}]," +
                    "\"sets\":[{\"id\":\"s\",\"name\":\"S\",\"cls\":\"" + cls + "\",\"tier\":1,\"tierName\":\"Dungeon\",\"canonStatus\":\"GAME-ONLY\",\"pieces\":[\"t.head\",\"" + feet + "\"],\"bonuses\":[{\"count\":2,\"effects\":[" + effect + "]}]}]}";
            }
            string ok = "{\"kind\":\"abilityCooldown\",\"ability\":\"mage.scorch\",\"amount\":2,\"text\":\"t\"}";
            var e = Assert.Throws<ArgumentException>(() => LootDatabase.Parse(new[] { File(ok) }, db)); StringAssert.Contains("is for 'class.rogue'", e.Message, "Another class's piece in the set.");
            e = Assert.Throws<ArgumentException>(() => LootDatabase.Parse(new[] { File("{\"kind\":\"procEcho\",\"ability\":\"mage.scorch\",\"amount\":100,\"text\":\"t\"}") }, db)); StringAssert.Contains("chance", e.Message, "A proc needs a chance.");
            e = Assert.Throws<ArgumentException>(() => LootDatabase.Parse(new[] { File("{\"kind\":\"abilityCost\",\"ability\":\"auto\",\"amount\":5,\"text\":\"t\"}") }, db)); StringAssert.Contains("only procs fire", e.Message);
            e = Assert.Throws<ArgumentException>(() => LootDatabase.Parse(new[] { File("{\"kind\":\"procReset\",\"ability\":\"mage.scorch\",\"amount\":600,\"chance\":20,\"text\":\"t\"}") }, db)); StringAssert.Contains("no other ability", e.Message);
            e = Assert.Throws<ArgumentException>(() => LootDatabase.Parse(new[] { File("{\"kind\":\"abilityPower\",\"ability\":\"mage.scorch\",\"amount\":80,\"text\":\"t\"}") }, db)); StringAssert.Contains("over its cap", e.Message);
            e = Assert.Throws<ArgumentException>(() => LootDatabase.Parse(new[] { File("{\"kind\":\"abilityPower\",\"amount\":20,\"text\":\"t\"}") }, db)); StringAssert.Contains("names no ability", e.Message);
        }
    }
}
