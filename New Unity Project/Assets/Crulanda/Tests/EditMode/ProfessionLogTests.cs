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
    }
}
