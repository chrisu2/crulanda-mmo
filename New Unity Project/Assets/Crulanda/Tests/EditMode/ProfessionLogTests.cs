using System;
using System.Collections.Generic;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>What a character knows of the trades (ProfessionLog) over the real content: what everyone starts with, tools that
    /// teach a skill once and are used up, gathering, and making things at a station (charcoal: the inputs, the room, the station,
    /// the skill, and the colours of what still teaches).</summary>
    public class ProfessionLogTests
    {
        static ProfessionLog Fresh(out List<string> said)
        {
            var items = ProfessionDataTests.Items();
            var log = new ProfessionLog(ProfessionDataTests.Db(items), items, EncounterSession.FreshProgress());
            var lines = new List<string>(); log.Say = lines.Add; said = lines;
            return log;
        }
        static int Slot(EncounterProgress p, string item) { return p.bag.FindIndex(s => s.item == item); }

        [Test] public void Everyone_starts_with_herbalism_and_cooking_and_nothing_else()
        {
            var log = Fresh(out _); var p = log.Progress;
            Assert.IsTrue(log.Has("herbalism")); Assert.AreEqual(1, log.Skill("herbalism"));
            Assert.IsTrue(log.Has("cooking")); Assert.AreEqual(1, log.Skill("cooking"));
            foreach (var id in new[] { "mining", "woodcutting", "blacksmithing", "alchemy" }) { Assert.IsFalse(log.Has(id), id); Assert.AreEqual(0, log.Skill(id), id); }
            Assert.AreEqual(2, p.professions.Count); Assert.AreEqual(0, log.CraftSlotsUsed);
            Assert.IsFalse(log.Has("fletching")); Assert.IsFalse(log.Has(null)); Assert.AreEqual(0, log.Skill("fletching"));
            // Binding again (a load) adds nothing twice.
            log.Bind(p); Assert.AreEqual(2, p.professions.Count);
        }

        [Test] public void Tool_TeachesSkill_AndIsConsumed()
        {
            var log = Fresh(out var said); var p = log.Progress;
            Assert.AreEqual(0, Inventory.Add(p, log.Items, "tool.pick", 1));
            int slot = Slot(p, "tool.pick"); Assert.GreaterOrEqual(slot, 0);
            Assert.IsFalse(log.Has("mining"));
            Assert.IsTrue(log.UseTool(slot, out var why), why); Assert.IsNull(why);
            Assert.IsTrue(log.Has("mining")); Assert.AreEqual(1, log.Skill("mining"));
            Assert.IsTrue(p.bag[slot].Empty, "The pick leaves the bags."); Assert.AreEqual(0, Inventory.Count(p, "tool.pick"));
            Assert.AreEqual(Inventory.BagSize, Inventory.FreeSlots(p), "It hangs at the belt: no bag slot is spent on it.");
            CollectionAssert.AreEqual(new[] { "You hang the pick at your belt. You can now mine." }, said);
            Assert.IsFalse(log.Has("woodcutting"), "A pick teaches mining only.");
            // The hatchet is its own skill.
            Inventory.Add(p, log.Items, "tool.hatchet", 1);
            Assert.IsTrue(log.UseTool(Slot(p, "tool.hatchet"), out why), why);
            Assert.IsTrue(log.Has("woodcutting")); Assert.AreEqual(1, log.Skill("woodcutting")); Assert.AreEqual(0, Inventory.Count(p, "tool.hatchet"));
            Assert.AreEqual("You hang the hatchet at your belt. You can now cut timber.", said[1]);
            Assert.AreEqual(4, p.professions.Count, "Herbalism, cooking, mining, woodcutting."); Assert.AreEqual(0, log.CraftSlotsUsed, "Gathering takes no craft slot.");
        }

        [Test] public void SecondTool_IsKept()
        {
            var log = Fresh(out var said); var p = log.Progress;
            Inventory.Add(p, log.Items, "tool.pick", 1); Assert.IsTrue(log.UseTool(Slot(p, "tool.pick"), out _));
            Inventory.Add(p, log.Items, "tool.pick", 1); int slot = Slot(p, "tool.pick");
            Assert.IsFalse(log.UseTool(slot, out var why));
            Assert.AreEqual("You already carry one.", why);
            Assert.AreEqual("tool.pick", p.bag[slot].item); Assert.AreEqual(1, p.bag[slot].count, "The second pick stays in the bags.");
            Assert.AreEqual(1, p.professions.FindAll(s => s.id == "mining").Count); Assert.AreEqual(1, log.Skill("mining"));
            Assert.AreEqual(1, said.Count, "Nothing is said twice.");
        }

        [Test] public void Only_a_tool_can_be_used_as_one()
        {
            var log = Fresh(out var said); var p = log.Progress;
            Inventory.Add(p, log.Items, "mat.copper_ore", 3); Inventory.Add(p, log.Items, "potion.minor", 1);
            Assert.IsFalse(log.UseTool(Slot(p, "mat.copper_ore"), out var why)); Assert.AreEqual(ProfessionLog.NotAToolLine, why);
            Assert.IsFalse(log.UseTool(Slot(p, "potion.minor"), out why)); Assert.AreEqual(ProfessionLog.NotAToolLine, why);
            Assert.IsFalse(log.UseTool(Slot(p, "") < 0 ? 5 : Slot(p, ""), out why), "An empty slot.");
            Assert.IsFalse(log.UseTool(-1, out why)); Assert.IsFalse(log.UseTool(999, out why));
            Assert.IsFalse(log.UseTool((ItemDef)null, out why)); Assert.AreEqual(ProfessionLog.NotAToolLine, why);
            Assert.IsFalse(log.UseTool(new ItemDef { id = "tool.lute", kind = "tool", teaches = "minstrelsy" }, out why), "A tool for a trade this content does not have.");
            Assert.AreEqual(3, Inventory.Count(p, "mat.copper_ore")); Assert.AreEqual(1, Inventory.Count(p, "potion.minor"));
            Assert.AreEqual(2, p.professions.Count); Assert.IsEmpty(said);
        }

        [Test] public void Bind_keeps_unknown_trades_and_brings_known_skills_into_range()
        {
            var items = ProfessionDataTests.Items(); var db = ProfessionDataTests.Db(items); var p = EncounterSession.FreshProgress();
            p.professions.Add(new ProfessionSkill { id = "mining", skill = 0 });
            p.professions.Add(new ProfessionSkill { id = "fletching", skill = 55 });
            p.professions.Add(new ProfessionSkill { id = "blacksmithing", skill = 47 });
            p.professions.Add(new ProfessionSkill { id = "", skill = 9 });
            p.professions.Add(null);
            var log = new ProfessionLog(db, items, p);
            Assert.AreEqual(1, log.Skill("mining"), "A skill is never below 1 once learned.");
            Assert.AreEqual(47, log.Skill("blacksmithing")); Assert.AreEqual(1, log.CraftSlotsUsed);
            Assert.IsFalse(log.Has("fletching"), "A trade this content does not have is ignored...");
            var kept = p.professions.Find(s => s != null && s.id == "fletching"); Assert.NotNull(kept, "...and kept in the save."); Assert.AreEqual(55, kept.skill);
            Assert.IsTrue(log.Has("herbalism")); Assert.IsTrue(log.Has("cooking"));
            Assert.AreEqual(5, p.professions.Count, "Mining, fletching, blacksmithing, herbalism, cooking: the blank and the null are gone.");
            // A null list (a save from before format 8 that skipped the reader) is made.
            var bare = EncounterSession.FreshProgress(); bare.professions = null;
            log.Bind(bare); Assert.AreSame(bare, log.Progress); Assert.AreEqual(2, bare.professions.Count);
        }

        // ---------- gathering (DESIGN 3) ----------
        static void Learn(ProfessionLog log, string tool) { Inventory.Add(log.Progress, log.Items, tool, 1); Assert.IsTrue(log.UseTool(Slot(log.Progress, tool), out var why), why); }
        static ProfessionSkill Entry(ProfessionLog log, string id) { return log.Progress.professions.Find(s => s.id == id); }

        [Test] public void Gather_WithoutTool_IsRefused()
        {
            var log = Fresh(out var said); var p = log.Progress; var copper = log.Db.Node("node.copper"); var oak = log.Db.Node("node.oak");
            Assert.IsFalse(log.CanGather(copper, out _, out var why)); Assert.AreEqual("You need a miner's pick. Merchants sell them.", why);
            Assert.IsFalse(log.CanGather(oak, out _, out why)); Assert.AreEqual("You need a woodcutter's hatchet. Merchants sell them.", why);
            Assert.AreEqual(0, log.Gather(copper, new System.Random(1), out why)); Assert.AreEqual("You need a miner's pick. Merchants sell them.", why);
            Assert.AreEqual(0, Inventory.Count(p, "mat.copper_ore")); Assert.IsFalse(log.Has("mining"), "Trying does not teach it.");
            Assert.IsFalse(log.CanGather(null, out _, out why)); Assert.IsNotNull(why);
            // Herbs are bare hands: everyone can pick them from the start, and at skill 1 yarrow is not hard going.
            Assert.IsTrue(log.CanGather(log.Db.Node("node.yarrow"), out bool hard, out why), why); Assert.IsFalse(hard);
            // With the pick at the belt the seam can be worked.
            Learn(log, "tool.pick");
            Assert.IsTrue(log.CanGather(copper, out hard, out why), why); Assert.IsFalse(hard, "A copper seam comes easily at Mining 1.");
            Assert.AreEqual(2f, log.WorkSeconds(copper, false)); Assert.AreEqual(1.5f, log.WorkSeconds(log.Db.Node("node.yarrow"), false));
        }

        [Test] public void Gather_UnderSkill_Refuses()
        {
            var log = Fresh(out _); var p = log.Progress; var cap = log.Db.Node("node.mourners_cap"); int ups = 0; log.SkillUp = (id, skill) => ups++;
            Assert.AreEqual(20, cap.skill);
            // A node above the skill refuses, for every trade (Chris, 2026-10-05): a herbalist of 1 can't pick mourner's cap.
            Assert.IsFalse(log.CanGather(cap, out _, out var why)); Assert.AreEqual("Requires Herbalism 20.", why);
            Assert.AreEqual(0, log.Gather(cap, new System.Random(7), out why), "Nothing gathered."); Assert.AreEqual(0, ups, "Nothing learned.");
            Assert.AreEqual(0, Inventory.Count(p, "mat.mourners_cap"));
            // At the node's own skill it can be worked: the yield is the node's range (1-2).
            p.professions.Find(e => e.id == "herbalism").skill = 20;
            Assert.IsTrue(log.CanGather(cap, out bool hard, out why), why); Assert.IsFalse(hard);
            var rng = new System.Random(7); var seen = new HashSet<int>(); for (int i = 0; i < 60; i++) seen.Add(log.RollGather(cap, rng).count);
            CollectionAssert.IsSubsetOf(seen, new[] { 1, 2 }); Assert.AreEqual(2, seen.Count, "Both 1 and 2 turn up.");
        }

        [Test] public void Gather_FullBags_GivesNothing_NoSkill()
        {
            var log = Fresh(out _); var p = log.Progress; var yarrow = log.Db.Node("node.yarrow"); int ups = 0; log.SkillUp = (id, s) => ups++;
            for (int i = 0; i < Inventory.BagSize; i++) Assert.AreEqual(0, Inventory.Add(p, log.Items, "tool.hatchet", 1));   // tools stack to one: every slot taken
            Assert.AreEqual(0, Inventory.FreeSlots(p));
            Assert.AreEqual(0, log.Gather(yarrow, new System.Random(3), out var why)); Assert.AreEqual(ProfessionLog.BagsFullLine, why);
            Assert.AreEqual(0, Inventory.Count(p, "mat.yarrow")); Assert.AreEqual(1, log.Skill("herbalism")); Assert.AreEqual(0, ups);
            // A stack with room left still takes the yield.
            p.bag[0] = new ItemStack { item = "mat.yarrow", count = 19 };
            Assert.AreEqual(1, log.Gather(yarrow, new System.Random(3), out why), "Only one more fits on the stack of 19.");
            Assert.AreEqual(20, Inventory.Count(p, "mat.yarrow")); Assert.AreEqual(2, log.Skill("herbalism"), "Something went in, so the skill rolls.");
        }

        [Test] public void SkillUp_Bands_WithSeededRng()
        {
            Assert.AreEqual(1f, ProfessionLog.GatherUpChance(1, 1)); Assert.AreEqual(1f, ProfessionLog.GatherUpChance(20, 1), "Under 20 over the node: certain.");
            Assert.AreEqual(.5f, ProfessionLog.GatherUpChance(21, 1)); Assert.AreEqual(.5f, ProfessionLog.GatherUpChance(40, 1), "Under 40 over: half the time.");
            Assert.AreEqual(0f, ProfessionLog.GatherUpChance(41, 1)); Assert.AreEqual(0f, ProfessionLog.GatherUpChance(100, 60), "From 40 over: never.");
            Assert.AreEqual(1f, ProfessionLog.GatherUpChance(5, 20), "Under the node's skill (hard going): certain.");
            var log = Fresh(out _); var p = log.Progress; var copper = log.Db.Node("node.copper"); var veridian = log.Db.Node("node.veridian");
            Learn(log, "tool.pick"); var mining = Entry(log, "mining");
            // Half: from 21 to 40 over a copper seam, about one gather in two teaches.
            int rose = 0; var rng = new System.Random(11);
            for (int i = 0; i < 400; i++) { mining.skill = 30; if (log.Gathered(copper, rng)) rose++; }
            Assert.That(rose, Is.InRange(160, 240), "About half of 400 (" + rose + ").");
            // None: 41 over a copper seam never teaches.
            mining.skill = 50; for (int i = 0; i < 100; i++) Assert.IsFalse(log.Gathered(copper, rng));
            Assert.AreEqual(50, log.Skill("mining"));
            // Never above 100: a Veridian seam (80) takes the skill to 100 and no further.
            mining.skill = 99; Assert.IsTrue(log.Gathered(veridian, rng)); Assert.AreEqual(100, log.Skill("mining"));
            for (int i = 0; i < 50; i++) Assert.IsFalse(log.Gathered(veridian, rng));
            Assert.AreEqual(100, log.Skill("mining"));
            // The same seed gives the same rolls.
            int Run(int seed) { var r = new System.Random(seed); int n = 0; for (int i = 0; i < 50; i++) { mining.skill = 30; if (log.Gathered(copper, r)) n++; } return n; }
            Assert.AreEqual(Run(5), Run(5));
            Assert.AreEqual(0, Inventory.Count(p, "mat.copper_ore"), "The skill roll alone puts nothing in the bags.");
        }

        [Test] public void Gather_Yield_IsTheNodesRange_WithABonusFromTwentyOver()
        {
            var log = Fresh(out _); Learn(log, "tool.pick"); var mining = Entry(log, "mining"); var copper = log.Db.Node("node.copper"); var rich = log.Db.Node("node.copper_rich");
            var rng = new System.Random(21); var seen = new HashSet<int>(); var seenRich = new HashSet<int>();
            for (int i = 0; i < 300; i++) { seen.Add(log.RollGather(copper, rng).count); seenRich.Add(log.RollGather(rich, rng).count); }
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, seen, "A copper seam gives 1-3 at Mining 1."); CollectionAssert.AreEquivalent(new[] { 2, 3, 4 }, seenRich, "A rich seam 2-4.");
            Assert.AreEqual("mat.copper_ore", log.RollGather(copper, rng).item);
            mining.skill = 21; seen.Clear();
            for (int i = 0; i < 400; i++) seen.Add(log.RollGather(copper, rng).count);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, seen, "From 20 over, one more now and then.");
        }

        // ---------- making things at a station (DESIGN 4, 5, 6.1; BUILD_PLAN step 9) ----------
        static readonly Func<string, bool> AtForge = k => k == "forge", AtFire = k => k == "fire", Nowhere = k => false;
        static ProfessionLog Woodcutter(out List<string> said) { var log = Fresh(out said); Learn(log, "tool.hatchet"); said.Clear(); return log; }
        static string Snapshot(EncounterProgress p) { return UnityEngine.JsonUtility.ToJson(p); }

        [Test] public void Craft_RemovesInputs_AddsOutput()
        {
            var log = Woodcutter(out var said); var p = log.Progress; var oak = log.Db.Recipe("recipe.charcoal_oak"); var ups = new List<int>();
            log.SkillUp = (id, skill) => { Assert.AreEqual("woodcutting", id); ups.Add(skill); };
            Assert.AreEqual("forge|fire", oak.station); Assert.AreEqual("mat.charcoal", oak.output); Assert.AreEqual(1, oak.count);
            Assert.AreEqual(0, Inventory.Add(p, log.Items, "mat.oak_log", 3));
            Assert.AreEqual(3, log.CanMake(oak));
            Assert.IsTrue(log.CanCraft(oak, AtForge, out var why), why); Assert.IsNull(why);
            Assert.IsTrue(log.Craft(oak, AtForge, new System.Random(1), out why), why);
            Assert.AreEqual(2, Inventory.Count(p, "mat.oak_log"), "One log burnt."); Assert.AreEqual(1, Inventory.Count(p, "mat.charcoal"), "One charcoal made.");
            Assert.AreEqual(2, log.Skill("woodcutting"), "Woodcutting 1 at a recipe of 1 teaches every time."); CollectionAssert.AreEqual(new[] { 2 }, ups);
            Assert.IsEmpty(said, "The session says what was made; the log says nothing.");
            // A fire does as well as a forge.
            Assert.IsTrue(log.Craft(oak, AtFire, new System.Random(1), out why), why);
            Assert.AreEqual(1, Inventory.Count(p, "mat.oak_log")); Assert.AreEqual(2, Inventory.Count(p, "mat.charcoal"));
            // A better wood makes more: a black pine log makes two at Woodcutting 20.
            Entry(log, "woodcutting").skill = 20; Inventory.Add(p, log.Items, "mat.blackpine_log", 1);
            Assert.IsTrue(log.Craft(log.Db.Recipe("recipe.charcoal_blackpine"), AtForge, new System.Random(1), out why), why);
            Assert.AreEqual(4, Inventory.Count(p, "mat.charcoal")); Assert.AreEqual(0, Inventory.Count(p, "mat.blackpine_log"));
            Assert.AreEqual(1, p.bag.FindAll(s => s.item == "mat.charcoal").Count, "The charcoal stacks.");
        }

        [Test] public void Craft_PutsCharcoal_InAWornOrePoke()
        {
            var log = Woodcutter(out _); var p = log.Progress; var oak = log.Db.Recipe("recipe.charcoal_oak");
            Inventory.Add(p, log.Items, "bag.ore_poke", 1); Assert.IsTrue(Inventory.Wear(p, log.Items, p.bag.FindIndex(s => s.item == "bag.ore_poke"), out var why), why);
            Inventory.Add(p, log.Items, "mat.oak_log", 2);
            Assert.IsTrue(log.Craft(oak, AtForge, new System.Random(2), out why), why);
            int at = p.bag.FindIndex(s => s.item == "mat.charcoal");
            Assert.GreaterOrEqual(at, Inventory.BagSize, "Charcoal is the ore-poke's: it goes in the poke's slots, not the ordinary ones.");
            Assert.AreEqual("bag.ore_poke", Inventory.PouchAt(p, log.Items, at)?.id);
        }

        [Test] public void Craft_MissingReagent_ChangesNothing()
        {
            var log = Woodcutter(out var said); var p = log.Progress; var oak = log.Db.Recipe("recipe.charcoal_oak"); int ups = 0; log.SkillUp = (id, s) => ups++;
            Inventory.Add(p, log.Items, "mat.blackpine_log", 2);   // another wood is not this recipe's
            string before = Snapshot(p);
            Assert.AreEqual(0, log.CanMake(oak));
            Assert.IsFalse(log.CanCraft(oak, AtForge, out var why)); Assert.AreEqual("You need Harrow oak log.", why);
            Assert.IsFalse(log.Craft(oak, AtForge, new System.Random(3), out why)); Assert.AreEqual("You need Harrow oak log.", why);
            Assert.AreEqual(before, Snapshot(p), "Nothing in the bags or the skills changed."); Assert.AreEqual(0, ups); Assert.IsEmpty(said);
            // A recipe taking two of one thing wants both.
            var two = new RecipeDef { id = "recipe.test_two", name = "Test", profession = "woodcutting", skill = 1, station = "forge", output = "mat.charcoal", inputs = new[] { new RecipeInput { item = "mat.oak_log", count = 2 } } };
            Inventory.Add(p, log.Items, "mat.oak_log", 1);
            Assert.AreEqual(0, log.CanMake(two)); Assert.IsFalse(log.Craft(two, AtForge, new System.Random(3), out why)); Assert.AreEqual("You need Harrow oak log x2.", why);
            Assert.AreEqual(1, Inventory.Count(p, "mat.oak_log"));
        }

        [Test] public void Craft_FullBags_ChangesNothing()
        {
            var log = Woodcutter(out _); var p = log.Progress; var oak = log.Db.Recipe("recipe.charcoal_oak"); int ups = 0; log.SkillUp = (id, s) => ups++;
            Assert.AreEqual(0, Inventory.Add(p, log.Items, "mat.oak_log", 1));
            for (int i = 0; i < Inventory.BagSize; i++) if (p.bag[i].Empty) p.bag[i] = new ItemStack { item = "tool.hatchet", count = 1 };   // tools stack to one: every slot taken
            Assert.AreEqual(0, Inventory.Room(p, log.Items, "mat.charcoal"));
            string before = Snapshot(p);
            Assert.IsFalse(log.CanCraft(oak, AtForge, out var why)); Assert.AreEqual(ProfessionLog.BagsFullLine, why);
            Assert.IsFalse(log.Craft(oak, AtForge, new System.Random(4), out why)); Assert.AreEqual(ProfessionLog.BagsFullLine, why);
            Assert.AreEqual(before, Snapshot(p), "The log is not burnt when the charcoal has nowhere to go."); Assert.AreEqual(0, ups);
            // A stack of charcoal with room on it takes what is made.
            p.bag[5] = new ItemStack { item = "mat.charcoal", count = 19 };
            Assert.IsTrue(log.Craft(oak, AtForge, new System.Random(4), out why), why);
            Assert.AreEqual(20, p.bag[5].count); Assert.AreEqual(0, Inventory.Count(p, "mat.oak_log"));
        }

        [Test] public void CanMake_CountsAcrossSplitStacks()
        {
            var log = Woodcutter(out _); var p = log.Progress; var oak = log.Db.Recipe("recipe.charcoal_oak");
            Inventory.Add(p, log.Items, "bag.log_sling", 1); Assert.IsTrue(Inventory.Wear(p, log.Items, p.bag.FindIndex(s => s.item == "bag.log_sling"), out var why), why);
            p.bag[0] = new ItemStack { item = "mat.oak_log", count = 20 };
            p.bag[7] = new ItemStack { item = "mat.oak_log", count = 3 };
            int sling = Inventory.BagSize; Assert.AreEqual("bag.log_sling", Inventory.PouchAt(p, log.Items, sling)?.id);
            p.bag[sling] = new ItemStack { item = "mat.oak_log", count = 4 };
            Assert.AreEqual(27, Inventory.Count(p, "mat.oak_log"));
            Assert.AreEqual(27, log.CanMake(oak), "Every stack counts: two in the bags and one in the log-sling.");
            // Two inputs: the scarcer one decides; an item named on two lines is summed.
            var mixed = new RecipeDef { id = "recipe.test_mixed", profession = "woodcutting", station = "forge", output = "mat.charcoal",
                inputs = new[] { new RecipeInput { item = "mat.oak_log", count = 1 }, new RecipeInput { item = "mat.oak_log", count = 1 }, new RecipeInput { item = "mat.flour", count = 1 } } };
            Assert.AreEqual(0, log.CanMake(mixed), "No flour.");
            p.bag[3] = new ItemStack { item = "mat.flour", count = 2 }; p.bag[9] = new ItemStack { item = "mat.flour", count = 3 };
            Assert.AreEqual(5, log.CanMake(mixed), "Five flour, against thirteen times two logs.");
            p.bag[3].count = 20;
            Assert.AreEqual(13, log.CanMake(mixed), "Twenty-three flour: now the logs decide, two a time.");
            Assert.AreEqual(0, log.CanMake(null)); Assert.AreEqual(0, log.CanMake(new RecipeDef { id = "recipe.empty" }));
            // Making spends from the stacks and the count follows.
            Assert.IsTrue(log.Craft(oak, AtForge, new System.Random(5), out why), why); Assert.AreEqual(26, log.CanMake(oak));
        }

        [Test] public void Craft_BelowSkill_IsRefused()
        {
            var log = Fresh(out var said); var p = log.Progress; var pine = log.Db.Recipe("recipe.charcoal_blackpine");
            Inventory.Add(p, log.Items, "mat.blackpine_log", 2); Inventory.Add(p, log.Items, "mat.oak_log", 2);
            // No hatchet: charcoal is Woodcutting's, so the hatchet comes first.
            Assert.IsFalse(log.CanCraft(log.Db.Recipe("recipe.charcoal_oak"), AtForge, out var why)); Assert.AreEqual("You need a woodcutter's hatchet. Merchants sell them.", why);
            Assert.AreEqual(ProfessionLog.Difficulty.Locked, log.DifficultyOf(log.Db.Recipe("recipe.charcoal_oak")));
            Learn(log, "tool.hatchet");
            Assert.AreEqual(ProfessionLog.Difficulty.Orange, log.DifficultyOf(log.Db.Recipe("recipe.charcoal_oak")));
            // Woodcutting 1 against a recipe of 20: refused, and nothing changes.
            string before = Snapshot(p);
            Assert.AreEqual(ProfessionLog.Difficulty.Locked, log.DifficultyOf(pine));
            Assert.AreEqual(2, log.CanMake(pine), "The bags hold the makings all the same.");
            Assert.IsFalse(log.CanCraft(pine, AtForge, out why)); Assert.AreEqual("That wants Woodcutting 20.", why);
            Assert.IsFalse(log.Craft(pine, AtForge, new System.Random(6), out why)); Assert.AreEqual("That wants Woodcutting 20.", why);
            Assert.AreEqual(before, Snapshot(p));
            // A craft not taken up says so.
            var smelt = new RecipeDef { id = "recipe.test_smelt", profession = "blacksmithing", skill = 1, station = "forge", output = "mat.charcoal", inputs = new[] { new RecipeInput { item = "mat.oak_log" } } };
            Assert.IsFalse(log.CanCraft(smelt, AtForge, out why)); Assert.AreEqual("You have not taken up Blacksmithing.", why);
            Assert.IsFalse(log.CanCraft(null, AtForge, out why)); Assert.IsNotNull(why);
            Assert.AreEqual(ProfessionLog.Difficulty.Locked, log.DifficultyOf(smelt));
        }

        [Test] public void Craft_WithoutStation_IsRefused()
        {
            var log = Woodcutter(out _); var p = log.Progress; var oak = log.Db.Recipe("recipe.charcoal_oak");
            Inventory.Add(p, log.Items, "mat.oak_log", 2); string before = Snapshot(p);
            Assert.IsFalse(log.CanCraft(oak, Nowhere, out var why)); Assert.AreEqual("You need a forge or a fire nearby.", why);
            Assert.IsFalse(log.CanCraft(oak, null, out why)); Assert.AreEqual("You need a forge or a fire nearby.", why);
            Assert.IsFalse(log.Craft(oak, k => k == "bench", new System.Random(7), out why), "A herbalist's bench is no place to burn charcoal."); Assert.AreEqual("You need a forge or a fire nearby.", why);
            Assert.AreEqual(before, Snapshot(p));
            Assert.AreEqual("You need a herbalist's bench nearby.", ProfessionLog.StationWanted("bench"));
            Assert.AreEqual("a forge or a fire", ProfessionLog.StationWords("forge|fire"));
            // With a station it goes.
            Assert.IsTrue(log.Craft(oak, AtFire, new System.Random(7), out why), why);
        }

        /// <summary>
        /// Cooking is everyone's from the start (BUILD_PLAN step 10, DESIGN 5.4): with no tool and nothing taken up, two boar meat make
        /// a Boar stew at a fire and nowhere else; the meat comes out of a worn larder-scrip and the stew goes in the ordinary bags; and
        /// the hearth-cake waits on Cooking 5, takes the hen-wife's eggs and makes two.
        /// </summary>
        [Test] public void Cooking_BoarStew_AtAnyFire_FromTheStart()
        {
            var log = Fresh(out var said); var p = log.Progress; var stew = log.Db.Recipe("recipe.boar_stew"); var ups = new List<int>();
            log.SkillUp = (id, skill) => { Assert.AreEqual("cooking", id); ups.Add(skill); };
            Assert.NotNull(stew); Assert.AreEqual(ProfessionLog.Difficulty.Orange, log.DifficultyOf(stew), "Cooking 1 makes it at once and learns from it.");
            Inventory.Add(p, log.Items, "bag.larder_scrip", 1); Assert.IsTrue(Inventory.Wear(p, log.Items, Slot(p, "bag.larder_scrip"), out var why), why);
            Assert.AreEqual(0, Inventory.Add(p, log.Items, "junk.boar_meat", 5));
            Assert.GreaterOrEqual(Slot(p, "junk.boar_meat"), Inventory.BagSize, "Boar meat is the larder-scrip's.");
            Assert.AreEqual(2, log.CanMake(stew), "Five meat make two stews.");
            string before = Snapshot(p);
            Assert.IsFalse(log.CanCraft(stew, AtForge, out why)); Assert.AreEqual("You need a fire nearby.", why);
            Assert.IsFalse(log.Craft(stew, k => k == "bench", new System.Random(8), out why)); Assert.AreEqual("You need a fire nearby.", why);
            Assert.AreEqual(before, Snapshot(p), "Nothing is cooked away from a fire.");
            Assert.IsTrue(log.Craft(stew, AtFire, new System.Random(8), out why), why);
            Assert.AreEqual(3, Inventory.Count(p, "junk.boar_meat")); Assert.AreEqual(1, Inventory.Count(p, "food.boar_stew"));
            Assert.Less(Slot(p, "food.boar_stew"), Inventory.BagSize, "Cooked food is no larder stuff: it goes in the ordinary bags.");
            Assert.AreEqual(2, log.Skill("cooking")); CollectionAssert.AreEqual(new[] { 2 }, ups);
            // The hearth-cake wants Cooking 5, flour and the hen-wife's eggs, and makes two.
            var cake = log.Db.Recipe("recipe.hearth_cake"); Inventory.Add(p, log.Items, "mat.flour", 1); Inventory.Add(p, log.Items, "food.fresh_eggs", 1);
            Assert.AreEqual(ProfessionLog.Difficulty.Locked, log.DifficultyOf(cake));
            Assert.IsFalse(log.CanCraft(cake, AtFire, out why)); Assert.AreEqual("That wants Cooking 5.", why);
            Entry(log, "cooking").skill = 5;
            Assert.IsTrue(log.Craft(cake, AtFire, new System.Random(8), out why), why);
            Assert.AreEqual(2, Inventory.Count(p, "food.hearth_cake")); Assert.AreEqual(0, Inventory.Count(p, "mat.flour")); Assert.AreEqual(0, Inventory.Count(p, "food.fresh_eggs"));
            Assert.AreEqual(1, p.bag.FindAll(s => s.item == "food.hearth_cake").Count, "The two stack.");
            Assert.IsEmpty(said, "The session says what was made; the log says nothing.");
        }

        [Test] public void Craft_SkillUp_FollowsTheColours()
        {
            Assert.AreEqual(1f, ProfessionLog.UpChance(1, 1)); Assert.AreEqual(1f, ProfessionLog.UpChance(10, 1), "Under 10 over the recipe: every time (orange).");
            Assert.AreEqual(.5f, ProfessionLog.UpChance(11, 1)); Assert.AreEqual(.5f, ProfessionLog.UpChance(20, 1), "Under 20 over: half the time (yellow).");
            Assert.AreEqual(.1f, ProfessionLog.UpChance(21, 1)); Assert.AreEqual(.1f, ProfessionLog.UpChance(30, 1), "Under 30 over: one in ten (green).");
            Assert.AreEqual(0f, ProfessionLog.UpChance(31, 1)); Assert.AreEqual(0f, ProfessionLog.UpChance(110, 80), "From 30 over: never (grey).");
            var log = Woodcutter(out _); var p = log.Progress; var oak = log.Db.Recipe("recipe.charcoal_oak"); var wood = Entry(log, "woodcutting");
            foreach (var (skill, colour) in new[] { (1, ProfessionLog.Difficulty.Orange), (10, ProfessionLog.Difficulty.Orange), (11, ProfessionLog.Difficulty.Yellow), (21, ProfessionLog.Difficulty.Green), (31, ProfessionLog.Difficulty.Grey) })
            { wood.skill = skill; Assert.AreEqual(colour, log.DifficultyOf(oak), "Woodcutting " + skill); }
            // Yellow: about half of 400 makings teach (the skill is put back each time).
            int rose = 0; var rng = new System.Random(13);
            for (int i = 0; i < 400; i++)
            {
                wood.skill = 15; Inventory.Add(p, log.Items, "mat.oak_log", 1);
                Assert.IsTrue(log.Craft(oak, AtForge, rng, out var why), why);
                if (wood.skill == 16) rose++;
                Inventory.Remove(p, "mat.charcoal", 1);
            }
            Assert.That(rose, Is.InRange(160, 240), "About half of 400 (" + rose + ").");
            // Grey teaches nothing, and nothing passes 100.
            wood.skill = 40; for (int i = 0; i < 50; i++) { Inventory.Add(p, log.Items, "mat.oak_log", 1); Assert.IsTrue(log.Craft(oak, AtForge, rng, out _)); Inventory.Remove(p, "mat.charcoal", 1); }
            Assert.AreEqual(40, wood.skill);
            wood.skill = 100; Inventory.Add(p, log.Items, "mat.ghostoak_log", 1);
            Assert.IsTrue(log.Craft(log.Db.Recipe("recipe.charcoal_ghostoak"), AtForge, rng, out var w), w); Assert.AreEqual(100, wood.skill);
            Assert.AreEqual(5, Inventory.Count(p, "mat.charcoal"), "A ghost-oak log makes five.");
        }

        // ---------- crafts: three at most (Phase 8.6, decision 3; two until then), taken up and forgotten (DESIGN 4; BUILD_PLAN step 12) ----------
        /// <summary>
        /// The owner's rule: three crafts at once since Phase 8.6 (decision 3: "Three craft slots"; it was two). Blacksmithing, Simpling
        /// (Alchemy's id) and Leatherworking taken up leave no slot, so Weaving is refused and nothing changes; forgetting one frees the slot
        /// for it. A trade that is not a craft is never taken up: Cooking and Herbalism are everyone's, Mining comes with its pick.
        /// </summary>
        [Test] public void FourthCraft_IsRefused()
        {
            var items = ProfessionDataTests.Items(); var db = ProfessionDataTests.Db(items);
            Assert.AreEqual(3, db.CraftSlots); Assert.AreEqual("craft", db.Profession("leatherworking").kind); Assert.AreEqual("craft", db.Profession("weaving").kind);
            var log = new ProfessionLog(db, items, EncounterSession.FreshProgress()); var said = new List<string>(); log.Say = said.Add;
            Assert.AreEqual(0, log.CraftSlotsUsed);
            Assert.IsTrue(log.CanLearn("blacksmithing", out var why), why); Assert.IsNull(why);
            Assert.IsTrue(log.Learn("blacksmithing", out why), why);
            Assert.IsTrue(log.Has("blacksmithing")); Assert.AreEqual(1, log.Skill("blacksmithing"), "A craft starts at 1."); Assert.AreEqual(1, log.CraftSlotsUsed);
            Assert.IsFalse(log.Learn("blacksmithing", out why)); Assert.AreEqual("You have taken up Blacksmithing already.", why);
            Assert.AreEqual(1, log.Progress.professions.FindAll(s => s.id == "blacksmithing").Count, "Taken up once.");
            Assert.IsTrue(log.Learn("alchemy", out why), why); Assert.AreEqual(2, log.CraftSlotsUsed);
            Assert.IsTrue(log.Learn("leatherworking", out why), why); Assert.AreEqual(3, log.CraftSlotsUsed);
            // Every slot used: the fourth is refused, and nothing changes.
            string before = Snapshot(log.Progress);
            Assert.IsFalse(log.CanLearn("weaving", out why)); Assert.AreEqual("Three crafts already. Forget one first.", why); Assert.AreEqual(log.CraftsFullLine, why);
            Assert.IsFalse(log.Learn("weaving", out why)); Assert.AreEqual("Three crafts already. Forget one first.", why);
            Assert.AreEqual(before, Snapshot(log.Progress)); Assert.IsFalse(log.Has("weaving")); Assert.AreEqual(0, log.Skill("weaving"));
            // Not crafts: never taken up.
            Assert.IsFalse(log.CanLearn("cooking", out why)); Assert.AreEqual("Everyone knows Cooking from the start.", why);
            Assert.IsFalse(log.CanLearn("herbalism", out why)); Assert.AreEqual("Everyone knows Herbalism from the start.", why);
            Assert.IsFalse(log.CanLearn("mining", out why)); Assert.AreEqual("You need a miner's pick. Merchants sell them.", why);
            Assert.IsFalse(log.CanLearn("fletching", out why)); Assert.AreEqual("There is no such trade.", why);
            Assert.IsFalse(log.CanLearn(null, out why)); Assert.IsNotNull(why);
            Assert.AreEqual(before, Snapshot(log.Progress));
            // Forgetting one frees its slot, and the fourth can be taken up.
            Assert.IsTrue(log.Forget("alchemy")); Assert.AreEqual(2, log.CraftSlotsUsed);
            Assert.IsTrue(log.Learn("weaving", out why), why); Assert.AreEqual(3, log.CraftSlotsUsed); Assert.IsTrue(log.Has("weaving"));
            Assert.IsFalse(log.CanLearn("alchemy", out why)); Assert.AreEqual("Three crafts already. Forget one first.", why);
            Assert.IsEmpty(said, "The log says nothing; the session speaks for the trainer.");
        }

        /// <summary>
        /// Forget: the craft's entry and skill are gone and its slot is free; the other craft stays; it can be taken up again, from 1; its
        /// recipes go with it (a copper bar is refused); and a save written after it binds without it.
        /// </summary>
        [Test] public void Forget_FreesSlot_AndDropsSkill()
        {
            var log = Fresh(out var said); var p = log.Progress;
            Assert.IsTrue(log.Learn("blacksmithing", out var why), why); Entry(log, "blacksmithing").skill = 47;
            Assert.IsTrue(log.Learn("alchemy", out why), why); Entry(log, "alchemy").skill = 12;
            Assert.AreEqual(2, log.CraftSlotsUsed);
            Assert.IsTrue(log.CanForget("blacksmithing", out why), why); Assert.IsNull(why);
            Assert.IsTrue(log.Forget("blacksmithing"));
            Assert.IsFalse(log.Has("blacksmithing")); Assert.AreEqual(0, log.Skill("blacksmithing")); Assert.AreEqual(1, log.CraftSlotsUsed, "The slot is free.");
            Assert.IsNull(p.professions.Find(s => s.id == "blacksmithing"), "The entry leaves the save.");
            Assert.IsTrue(log.Has("alchemy")); Assert.AreEqual(12, log.Skill("alchemy"), "The other craft is kept as it was.");
            Assert.IsFalse(log.Forget("blacksmithing"), "Forgetting it twice changes nothing.");
            Assert.IsFalse(log.CanForget("blacksmithing", out why)); Assert.AreEqual("You have not taken up Blacksmithing.", why);
            // Its recipes went with it.
            Inventory.Add(p, log.Items, "mat.copper_ore", 2); Inventory.Add(p, log.Items, "mat.charcoal", 1);
            Assert.IsFalse(log.CanCraft(log.Db.Recipe("recipe.copper_bar"), AtForge, out why)); Assert.AreEqual("You have not taken up Blacksmithing.", why);
            Assert.AreEqual(ProfessionLog.Difficulty.Locked, log.DifficultyOf(log.Db.Recipe("recipe.copper_bar")));
            // A save written now binds without it, and Alchemy is still there.
            var copy = UnityEngine.JsonUtility.FromJson<EncounterProgress>(Snapshot(p)); log.Bind(copy);
            Assert.IsFalse(log.Has("blacksmithing")); Assert.AreEqual(12, log.Skill("alchemy")); Assert.AreEqual(1, log.CraftSlotsUsed);
            // Taken up again, it starts from 1.
            Assert.IsTrue(log.Learn("blacksmithing", out why), why); Assert.AreEqual(1, log.Skill("blacksmithing"));
            Assert.IsEmpty(said);
        }

        /// <summary>Cooking stays for good (DESIGN 4), and so do the gathering skills: Forget refuses them with why and changes nothing.
        /// A craft not taken up and a trade the content does not have are refused too.</summary>
        [Test] public void Cooking_CannotBeForgotten()
        {
            var log = Fresh(out _); var p = log.Progress; Learn(log, "tool.pick");
            Entry(log, "cooking").skill = 30; Entry(log, "mining").skill = 12;
            string before = Snapshot(p);
            Assert.IsFalse(log.CanForget("cooking", out var why)); Assert.AreEqual("Cooking stays with you. It can't be forgotten.", why);
            Assert.IsFalse(log.Forget("cooking")); Assert.AreEqual(30, log.Skill("cooking"));
            Assert.IsFalse(log.CanForget("herbalism", out why)); Assert.AreEqual("Herbalism stays with you. It can't be forgotten.", why); Assert.IsFalse(log.Forget("herbalism"));
            Assert.IsFalse(log.Forget("mining")); Assert.AreEqual(12, log.Skill("mining"), "The pick stays at the belt.");
            Assert.IsFalse(log.CanForget("alchemy", out why)); Assert.AreEqual("You have not taken up Simpling.", why); Assert.IsFalse(log.Forget("alchemy"));
            Assert.IsFalse(log.Forget("fletching")); Assert.IsFalse(log.Forget(null));
            Assert.AreEqual(before, Snapshot(p), "Nothing changed.");
        }

        /// <summary>
        /// The owner's check, in the log (BUILD_PLAN step 12): taken up at 1, Blacksmithing smelts Crowsfoot copper into bars at a forge
        /// and nowhere else; the Copper-shod cudgel waits on Blacksmithing 5 ("That wants Blacksmithing 5."), then takes two bars and an
        /// oak log; the bars go in a worn ore-poke and the cudgel in the ordinary bags; and the cudgel can be worn at level 1.
        /// </summary>
        [Test] public void Blacksmithing_SmeltsCopper_ThenMakesTheCudgel()
        {
            var log = Fresh(out _); var p = log.Progress; var bar = log.Db.Recipe("recipe.copper_bar"); var cudgel = log.Db.Recipe("recipe.copper_cudgel"); var ups = new List<int>();
            log.SkillUp = (id, skill) => { Assert.AreEqual("blacksmithing", id); ups.Add(skill); };
            Inventory.Add(p, log.Items, "mat.copper_ore", 8); Inventory.Add(p, log.Items, "mat.charcoal", 4); Inventory.Add(p, log.Items, "mat.oak_log", 1);
            Assert.IsFalse(log.CanCraft(bar, AtForge, out var why)); Assert.AreEqual("You have not taken up Blacksmithing.", why);
            Assert.IsTrue(log.Learn("blacksmithing", out why), why);
            Assert.AreEqual(ProfessionLog.Difficulty.Orange, log.DifficultyOf(bar)); Assert.AreEqual(4, log.CanMake(bar));
            Assert.IsFalse(log.Craft(bar, AtFire, new System.Random(1), out why), "A fire is no forge."); Assert.AreEqual("You need a forge nearby.", why);
            Inventory.Add(p, log.Items, "bag.ore_poke", 1); Assert.IsTrue(Inventory.Wear(p, log.Items, Slot(p, "bag.ore_poke"), out why), why);
            for (int i = 0; i < 4; i++) Assert.IsTrue(log.Craft(bar, AtForge, new System.Random(i), out why), why);
            Assert.AreEqual(4, Inventory.Count(p, "mat.copper_bar")); Assert.AreEqual(0, Inventory.Count(p, "mat.copper_ore")); Assert.AreEqual(0, Inventory.Count(p, "mat.charcoal"));
            Assert.GreaterOrEqual(Slot(p, "mat.copper_bar"), Inventory.BagSize, "Bars go in the ore-poke.");
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5 }, ups, "Every copper bar teaches at Blacksmithing 1 to 4.");
            // The cudgel at Blacksmithing 5.
            Entry(log, "blacksmithing").skill = 4; string before = Snapshot(p);
            Assert.AreEqual(ProfessionLog.Difficulty.Locked, log.DifficultyOf(cudgel));
            Assert.IsFalse(log.CanCraft(cudgel, AtForge, out why)); Assert.AreEqual("That wants Blacksmithing 5.", why); Assert.AreEqual(before, Snapshot(p));
            Entry(log, "blacksmithing").skill = 5;
            Assert.AreEqual(ProfessionLog.Difficulty.Orange, log.DifficultyOf(cudgel)); Assert.AreEqual(1, log.CanMake(cudgel), "Four bars but one log: one cudgel.");
            Assert.IsTrue(log.Craft(cudgel, AtForge, new System.Random(9), out why), why);
            Assert.AreEqual(1, Inventory.Count(p, "craft.copper_cudgel")); Assert.AreEqual(2, Inventory.Count(p, "mat.copper_bar")); Assert.AreEqual(0, Inventory.Count(p, "mat.oak_log"));
            Assert.Less(Slot(p, "craft.copper_cudgel"), Inventory.BagSize, "Gear goes in the ordinary bags.");
            Assert.AreEqual(6, log.Skill("blacksmithing"));
            var d = log.Items.Get("craft.copper_cudgel");
            Assert.IsTrue(Inventory.CanEquip(d, 1, out why), why); Assert.AreEqual("mainhand", d.slot); Assert.AreEqual(9, d.weaponDamage, "Re-cut a step over the generated uncommon (Phase 8.6; it was 7).");
        }

        /// <summary>Alchemy (BUILD_PLAN step 13): taken up, two yarrow and a vial make a Minor healing draught at a herbalist's bench and
        /// nowhere else; the Tarnwater draught waits on Alchemy 40.</summary>
        [Test] public void Alchemy_MakesAMinorDraught_AtTheBench()
        {
            var log = Fresh(out _); var p = log.Progress; var minor = log.Db.Recipe("recipe.potion_minor"); var tarn = log.Db.Recipe("recipe.potion_tarn");
            Inventory.Add(p, log.Items, "mat.yarrow", 5); Inventory.Add(p, log.Items, "mat.vial", 2);
            Assert.IsFalse(log.CanCraft(minor, k => k == "bench", out var why)); Assert.AreEqual("You have not taken up Simpling.", why);   // Alchemy's name since Phase 8.6 (decision 5)
            Assert.IsTrue(log.Learn("alchemy", out why), why);
            Assert.AreEqual(2, log.CanMake(minor), "Five yarrow and two vials: two draughts.");
            Assert.IsFalse(log.Craft(minor, AtForge, new System.Random(1), out why)); Assert.AreEqual("You need a herbalist's bench nearby.", why);
            Assert.IsTrue(log.Craft(minor, k => k == "bench", new System.Random(1), out why), why);
            Assert.AreEqual(1, Inventory.Count(p, "potion.minor")); Assert.AreEqual(3, Inventory.Count(p, "mat.yarrow")); Assert.AreEqual(1, Inventory.Count(p, "mat.vial"));
            Assert.AreEqual(2, log.Skill("alchemy"));
            Assert.AreEqual(ProfessionLog.Difficulty.Locked, log.DifficultyOf(tarn)); Assert.IsFalse(log.CanCraft(tarn, k => k == "bench", out why)); Assert.AreEqual("That wants Simpling 40.", why);
        }

        // ---------- found recipes: leaves (Phase 8.6 step 5; save format 11) ----------
        /// <summary>
        /// A found recipe is unknown until its leaf is read, and learned once: Ox-root cordial (Simpling 52) is locked and refused ("You have
        /// not learned that. A leaf teaches it.") even at the skill and with the makings in the bags; the herbalist's leaf, used from the
        /// bags, teaches it (into Progress.recipes, the leaf used up, "You learn Ox-root cordial.") and it is made; a second leaf is refused
        /// and kept ("You know that one already."). A taught recipe needs no leaf.
        /// </summary>
        [Test] public void A_found_recipe_is_unknown_until_its_leaf_is_read_and_learned_once()
        {
            var log = Fresh(out var said); var p = log.Progress; var cordial = log.Db.Recipe("recipe.cordial_ox"); var learned = new List<string>(); log.Learned = r => learned.Add(r.id);
            Assert.NotNull(cordial); Assert.IsTrue(cordial.found, "The Ox-root cordial is a found recipe.");
            var leafDef = ProfessionDatabase.LeafFor(log.Items, cordial); Assert.NotNull(leafDef); Assert.AreEqual("leaf.cordial_ox", leafDef.id);
            Assert.AreEqual("recipe", leafDef.kind); Assert.AreEqual("a herbalist's leaf: Ox-root cordial", leafDef.name, "Decision 9: \"a smith's leaf / a cook's leaf\".");
            Assert.IsTrue(log.Learn("alchemy", out var why), why); Entry(log, "alchemy").skill = 60;
            foreach (var i in cordial.inputs) Inventory.Add(p, log.Items, i.item, i.count * 2);
            Assert.IsFalse(log.Knows(cordial)); Assert.IsTrue(log.Knows(log.Db.Recipe("recipe.potion_minor")), "A taught recipe is known by the skill.");
            Assert.AreEqual(ProfessionLog.Difficulty.Locked, log.DifficultyOf(cordial), "Unknown: shown as a grey ? in the list, and locked.");
            Assert.IsFalse(log.CanCraft(cordial, k => k == "bench", out why)); Assert.AreEqual(ProfessionLog.UnknownLine, why);
            string before = Snapshot(p);
            Assert.IsFalse(log.Craft(cordial, k => k == "bench", new System.Random(1), out why)); Assert.AreEqual(before, Snapshot(p), "Nothing made, nothing spent.");
            // The leaf, from the bags.
            Inventory.Add(p, log.Items, leafDef.id, 2); int slot = Slot(p, leafDef.id); Assert.GreaterOrEqual(slot, 0);
            Assert.IsTrue(log.UseLeaf(slot, out why), why); Assert.IsNull(why);
            CollectionAssert.AreEqual(new[] { "recipe.cordial_ox" }, p.recipes, "Learned, in the save's list.");
            Assert.AreEqual(1, Inventory.Count(p, leafDef.id), "One leaf is used up.");
            Assert.Contains("You learn Ox-root cordial.", said); CollectionAssert.AreEqual(new[] { "recipe.cordial_ox" }, learned, "The session hears of it (a toast).");
            Assert.IsTrue(log.Knows(cordial)); Assert.AreNotEqual(ProfessionLog.Difficulty.Locked, log.DifficultyOf(cordial));
            Assert.IsTrue(log.Craft(cordial, k => k == "bench", new System.Random(1), out why), why);
            Assert.AreEqual(1, Inventory.Count(p, "potion.cordial_ox"));
            // A second leaf is refused and kept; learned once.
            before = Snapshot(p);
            Assert.IsFalse(log.UseLeaf(Slot(p, leafDef.id), out why)); Assert.AreEqual(ProfessionLog.AlreadyKnownLine, why); Assert.AreEqual("You know that one already.", why);
            Assert.AreEqual(before, Snapshot(p)); Assert.AreEqual(1, p.recipes.Count);
            // Not a leaf.
            Inventory.Add(p, log.Items, "potion.minor", 1);
            Assert.IsFalse(log.UseLeaf(Slot(p, "potion.minor"), out why)); Assert.AreEqual(ProfessionLog.NotALeafLine, why);
            Assert.IsFalse(log.UseLeaf(-1, out why)); Assert.IsFalse(log.UseLeaf((ItemDef)null, out why));
        }

        /// <summary>
        /// A leaf wants its trade: a smith's leaf is refused, and kept, without Blacksmithing ("That is Blacksmithing's work. Take up
        /// Blacksmithing to learn it."); a bowyer's leaf without a hatchet names the tool. The skill is not asked: a leaf learned at 1 shows
        /// its recipe red ("That wants Blacksmithing 17.") until the skill reaches it. Forgetting the craft keeps the leaf learned, and a
        /// reload (Bind) keeps the list, dropping blanks and repeats and keeping an id this content does not have.
        /// </summary>
        [Test] public void A_leaf_wants_its_trade_and_stays_learned()
        {
            var log = Fresh(out _); var p = log.Progress; var cap = log.Db.Recipe("recipe.tempered_copper_cap"); var leafDef = ProfessionDatabase.LeafFor(log.Items, cap);
            Assert.NotNull(leafDef); Assert.AreEqual("a smith's leaf: Resonance-tempered cap", leafDef.name);
            Assert.IsFalse(log.UseLeaf(leafDef, out var why)); Assert.AreEqual("That is Blacksmithing's work. Take up Blacksmithing to learn it.", why);
            var bow = ProfessionDatabase.LeafFor(log.Items, log.Db.Recipe("recipe.tempered_copper_bow")); Assert.AreEqual("a bowyer's leaf: Seasoned oak bow", bow.name);
            Assert.IsFalse(log.UseLeaf(bow, out why)); Assert.AreEqual("You need a woodcutter's hatchet. Merchants sell them.", why);
            Assert.IsEmpty(p.recipes);
            Assert.IsTrue(log.Learn("blacksmithing", out why), why);
            Assert.IsTrue(log.UseLeaf(leafDef, out why), why); Assert.IsTrue(log.Knows(cap));
            Assert.IsFalse(log.CanCraft(cap, AtForge, out why)); Assert.AreEqual("That wants Blacksmithing " + cap.skill + ".", why, "Known, but the skill comes first.");
            Assert.IsTrue(log.Forget("blacksmithing")); Assert.IsTrue(log.Knows(cap), "The page stays read when the hand is put aside.");
            p.recipes.Add(""); p.recipes.Add("recipe.tempered_copper_cap"); p.recipes.Add("recipe.gone_from_this_build");
            var copy = UnityEngine.JsonUtility.FromJson<EncounterProgress>(Snapshot(p)); log.Bind(copy);
            CollectionAssert.AreEqual(new[] { "recipe.tempered_copper_cap", "recipe.gone_from_this_build" }, copy.recipes, "Blanks and repeats dropped, an unknown id kept.");
            Assert.IsTrue(log.Knows(cap));
        }

        /// <summary>Every found recipe has exactly one leaf, named for its trade in the village's words (decision 9; never "pattern",
        /// "formula", "plans" or "schematic"), coloured by what it teaches, and labelled GAME-ONLY; 30-40 of them (the plan's six to eight a zone).</summary>
        [Test] public void Every_found_recipe_has_one_leaf_in_the_village_words()
        {
            var items = ProfessionDataTests.Items(); var db = ProfessionDataTests.Db(items);
            var words = new Dictionary<string, string> { { "blacksmithing", "a smith's leaf: " }, { "leatherworking", "a tanner's leaf: " }, { "weaving", "a weaver's leaf: " },
                { "woodcutting", "a bowyer's leaf: " }, { "alchemy", "a herbalist's leaf: " }, { "cooking", "a cook's leaf: " } };
            var found = db.Recipes.FindAll(r => r.found);
            Assert.That(found.Count, Is.InRange(30, 40), "30-40 found recipes (" + found.Count + ").");
            foreach (var r in found)
            {
                var leaves = new List<ItemDef>(); foreach (var d in items.Items.Values) if (d.kind == "recipe" && d.teaches == r.id) leaves.Add(d);
                Assert.AreEqual(1, leaves.Count, r.id + " has one leaf.");
                var l = leaves[0];
                Assert.AreEqual(words[r.profession] + r.name, l.name, r.id);
                foreach (var bad in new[] { "pattern", "formula", "plans", "schematic", "recipe:" }) StringAssert.DoesNotContain(bad, l.name.ToLowerInvariant(), r.id);
                Assert.AreEqual(System.Math.Max(2, items.Get(r.output).quality), l.quality, r.id + ": the leaf's colour is what it teaches.");
                Assert.AreEqual("GAME-ONLY", l.canonStatus, r.id); Assert.IsFalse(string.IsNullOrEmpty(l.description), r.id);
                Assert.AreEqual(1, l.stack, r.id);
            }
            foreach (var r in db.Recipes) if (!r.found) Assert.IsNull(ProfessionDatabase.LeafFor(items, r), r.id + " is taught: no leaf.");
            // A leaf that teaches nothing, or a taught recipe, and a found recipe without a leaf, are refused.
            string bad1 = "{ \"items\": [ { \"id\": \"leaf.x\", \"name\": \"a smith's leaf: X\", \"kind\": \"recipe\", \"teaches\": \"recipe.copper_bar\" }, { \"id\": \"leaf.y\", \"name\": \"Y\", \"kind\": \"recipe\", \"teaches\": \"recipe.none\" } ] }";
            var texts = ProfessionDataTests.Texts("Items"); texts.Add(bad1);
            var trades = ProfessionDataTests.Texts("Professions");
            trades.Add("{ \"recipes\": [ { \"id\": \"recipe.z\", \"name\": \"Z\", \"profession\": \"cooking\", \"station\": \"fire\", \"output\": \"food.boar_stew\", \"found\": true, \"inputs\": [ { \"item\": \"junk.boar_meat\" } ] } ] }");
            var e = Assert.Throws<System.ArgumentException>(() => ProfessionDatabase.Parse(trades, ItemDatabase.Parse(texts)));
            StringAssert.Contains("Leaf 'leaf.x' teaches 'recipe.copper_bar', which is taught by the skill, not found.", e.Message);
            StringAssert.Contains("Leaf 'leaf.y' teaches unknown recipe 'recipe.none'.", e.Message);
            StringAssert.Contains("Recipe 'recipe.z': found, but no leaf teaches it.", e.Message);
        }
    }
}
