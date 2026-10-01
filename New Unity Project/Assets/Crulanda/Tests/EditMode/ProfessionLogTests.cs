using System.Collections.Generic;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>What a character knows of the trades (ProfessionLog) over the real content: what everyone starts with, and tools
    /// that teach a skill once and are used up.</summary>
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
    }
}
