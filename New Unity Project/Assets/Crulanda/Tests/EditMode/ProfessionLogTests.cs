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

        [Test] public void Gather_UnderSkill_IsHardGoing_YieldsOne_AlwaysSkillsUp()
        {
            var log = Fresh(out _); var p = log.Progress; var cap = log.Db.Node("node.mourners_cap"); var ups = new List<int>();
            log.SkillUp = (id, skill) => { Assert.AreEqual("herbalism", id); ups.Add(skill); };
            Assert.AreEqual(20, cap.skill);
            // Low skill never blocks a node: a herbalist of 1 picks mourner's cap at once, slowly, one at a time.
            Assert.IsTrue(log.CanGather(cap, out bool hard, out var why), why); Assert.IsTrue(hard, "Herbalism 1 against a node of 20 is hard going.");
            Assert.AreEqual(3f, log.WorkSeconds(cap, true), "Herbs: 1.5 s, twice that when hard going."); Assert.AreEqual(4f, log.WorkSeconds(log.Db.Node("node.bogiron"), true), "Ore: 2 s, twice that.");
            var rng = new System.Random(7);
            for (int i = 0; i < 19; i++)
            {
                int before = log.Skill("herbalism");
                Assert.AreEqual(1, log.Gather(cap, rng, out why), "Hard going yields exactly one (gather " + i + ").");
                Assert.AreEqual(before + 1, log.Skill("herbalism"), "Every hard-going gather teaches.");
            }
            Assert.AreEqual(19, Inventory.Count(p, "mat.mourners_cap")); Assert.AreEqual(20, log.Skill("herbalism"));
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }, ups);
            // At the node's own skill it is no longer hard going: the yield is the node's range (1-2).
            Assert.IsTrue(log.CanGather(cap, out hard, out _)); Assert.IsFalse(hard);
            var seen = new HashSet<int>(); for (int i = 0; i < 60; i++) seen.Add(log.RollGather(cap, rng).count);
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
    }
}
