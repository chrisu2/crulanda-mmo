using NUnit.Framework;
using Crulanda.Combat;
using Crulanda.Core;
using Crulanda.Encounter;
using Crulanda.Gameplay;
using UnityEngine;

namespace Crulanda.Tests
{
    /// <summary>
    /// The buff row as icons (Chris, 2026-10-18: "for buffs need icons or something bc this is messy and hard to read screen"):
    /// small squares within the party frame's width for six buffs, the corner countdown in minutes or seconds, the kinds and class of a
    /// status by its id, debuffs told apart, and the tooltip's words.
    /// </summary>
    public class BuffIconTests
    {
        [Test]
        public void SixBuffsStayWithinThePartyFrame()
        {
            Assert.That(BuffIcons.Size, Is.InRange(20f, 24f), "Small squares at UiScale 1.");
            Assert.LessOrEqual(BuffIcons.RowWidth(6), BuffIcons.PartyFrameWidth, "Six icons are no wider than the frame.");
            Assert.GreaterOrEqual(BuffIcons.Fits(BuffIcons.PartyFrameWidth), 6, "Six fit.");
            Assert.AreEqual(6, BuffIcons.Shown(6, BuffIcons.PartyFrameWidth), "All six are drawn, no \"+N\".");
            var row = new Rect(388, 206, BuffIcons.PartyFrameWidth, BuffIcons.Size);
            for (int i = 0; i < 6; i++)
            {
                var r = BuffIcons.IconRect(row, i);
                Assert.GreaterOrEqual(r.x, row.x); Assert.LessOrEqual(r.xMax, row.xMax, "Icon " + i + " is inside the row.");
                Assert.AreEqual(row.y, r.y); Assert.AreEqual(BuffIcons.Size, r.height);
            }
            int many = 20, shown = BuffIcons.Shown(many, BuffIcons.PartyFrameWidth);
            Assert.LessOrEqual(BuffIcons.IconRect(row, shown).xMax, row.xMax, "With too many, the \"+N\" slot still sits inside.");
            Assert.LessOrEqual(BuffIcons.RowWidth(12), BuffIcons.PlayerRowWidth + .01f, "Your own row takes twelve.");
        }

        [Test]
        public void CountdownIsShort()
        {
            Assert.AreEqual("12m", BuffIcons.Countdown(741), "741 s reads 12m, not \"741s\".");
            Assert.AreEqual("30m", BuffIcons.Countdown(1800));
            Assert.AreEqual("1m", BuffIcons.Countdown(60));
            Assert.AreEqual("45", BuffIcons.Countdown(44.2f), "Seconds under a minute.");
            Assert.AreEqual("", BuffIcons.Countdown(0), "A song without an end has no number.");
            Assert.AreEqual("12m 21s left", BuffIcons.TimeLeft(741));
            Assert.LessOrEqual(BuffIcons.Countdown(5999).Length, 3, "Fits the corner.");
        }

        [Test]
        public void KindsAndClassesByTheStatusId()
        {
            Assert.AreEqual(BuffKind.Food, BuffIcons.KindOf(Consumables.FedId));
            Assert.AreEqual(BuffKind.Elixir, BuffIcons.KindOf(Consumables.ElixirId));
            Assert.AreEqual(BuffKind.Salve, BuffIcons.KindOf(Consumables.SalveId));
            Assert.AreEqual(BuffKind.ClassBuff, BuffIcons.KindOf("buff.class.paladin"));
            Assert.AreEqual("class.paladin", BuffIcons.ClassOf("buff.class.paladin"));
            Assert.IsNull(BuffIcons.ClassOf(Consumables.FedId));
            Assert.AreEqual(BuffKind.Other, BuffIcons.KindOf("status.something"));
        }

        [Test]
        public void DebuffsAreToldApartAndTheTooltipSaysWhatItDoes()
        {
            var harm = new StatusEffectDefinition { id = "status.sunder", name = "Sunder", duration = 10, incomingDamageMultiplier = 1.2f };
            var help = new StatusEffectDefinition { id = Consumables.FedId, name = "Well fed", duration = 1800, modifiers = new[] {
                new StatusModifierDefinition { stat = StatType.Stamina, operation = ModifierOp.Flat, value = 6 },
                new StatusModifierDefinition { stat = StatType.Agility, operation = ModifierOp.Flat, value = 4 } } };
            var runtime = new StatusEffectRuntime(new StatBlock());
            Assert.IsTrue(runtime.Apply(harm, 1)); Assert.IsTrue(runtime.Apply(help, 1));
            Assert.IsTrue(runtime.IsHarmful("status.sunder"), "Takes more: harmful.");
            Assert.IsFalse(runtime.IsHarmful(Consumables.FedId), "Well fed helps.");
            Assert.IsTrue(StatusEffectRuntime.IsHarmful("status.poison.x", 1, null), "A poison by its id.");
            Assert.AreSame(help, runtime.DefinitionOf(Consumables.FedId));
            Assert.AreEqual("+6 Stamina, +4 Agility", BuffIcons.Effect(help));
            Assert.AreEqual("Takes 20% more damage", BuffIcons.Effect(harm));
            var tip = BuffIcons.Tooltip(new BuffEntry { name = "Well fed", remaining = 741, effect = BuffIcons.Effect(help) });
            StringAssert.Contains("Well fed", tip); StringAssert.Contains("12m 21s left", tip); StringAssert.Contains("+6 Stamina", tip);
        }
    }
}
