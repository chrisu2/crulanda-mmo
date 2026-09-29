using NUnit.Framework;
using Crulanda.Abilities;

namespace Crulanda.Tests
{
    public class AbilityRuntimeTests
    {
        static AbilityDefinition Spell(string id = "ability.test")
        {
            return new AbilityDefinition { id = id, cost = 10, cooldown = 5, globalCooldown = 1.5f };
        }
        [Test]
        public void Global_cooldown_blocks_other_spells_without_spending()
        {
            var r = new AbilityRuntime(); int spent = 0;
            Assert.AreEqual(AbilityStartResult.Started, r.TryStart(Spell(), 0, n => { spent += n; return true; }, () => {}));
            Assert.AreEqual(AbilityStartResult.Cooldown, r.TryStart(Spell("other"), 1, n => { spent += n; return true; }, () => {}));
            Assert.AreEqual(10, spent);
            Assert.AreEqual(AbilityStartResult.Started, r.TryStart(Spell("other"), 1.5f, n => true, () => {}));
        }
        [Test]
        public void Off_global_defense_does_not_clear_existing_global_cooldown()
        {
            var r = new AbilityRuntime(); r.TryStart(Spell(), 0, n => true, () => {});
            var defense = Spell("defense"); defense.globalCooldown = 0;
            Assert.AreEqual(AbilityStartResult.Started, r.TryStart(defense, .1f, n => true, () => {}));
            Assert.AreEqual(AbilityStartResult.Cooldown, r.TryStart(Spell("other"), .2f, n => true, () => {}));
        }
        [Test]
        public void Cast_executes_once_after_duration_and_blocks_overlapping_casts()
        {
            var r = new AbilityRuntime(); var a = Spell(); a.castTime = 2; int effects = 0;
            r.TryStart(a, 0, n => true, () => effects++);
            Assert.AreEqual(AbilityStartResult.Casting, r.TryStart(Spell("other"), 1.5f, n => true, () => {}));
            r.Tick(1); Assert.AreEqual(0, effects); Assert.AreEqual(.5f, r.CastProgress(1));
            r.Tick(2); r.Tick(3); Assert.AreEqual(1, effects); Assert.IsFalse(r.IsCasting);
        }
        [Test]
        public void Interrupted_cast_never_executes_and_keeps_cost_and_cooldown()
        {
            var r = new AbilityRuntime(); var a = Spell(); a.castTime = 2; int spent = 0, effects = 0;
            r.TryStart(a, 0, n => { spent += n; return true; }, () => effects++);
            r.Tick(1, false); r.Tick(10);
            Assert.AreEqual(0, effects); Assert.AreEqual(10, spent); Assert.AreEqual(4, r.Remaining(a, 1));
        }
        [Test]
        public void Failed_cost_does_not_start_any_timers()
        {
            var r = new AbilityRuntime(); var a = Spell();
            Assert.AreEqual(AbilityStartResult.InsufficientResource, r.TryStart(a, 0, n => false, () => Assert.Fail()));
            Assert.AreEqual(0, r.Remaining(a, 0));
            Assert.AreEqual(AbilityStartResult.Started, r.TryStart(a, 0, n => true, () => {}));
        }
        [Test]
        public void Reset_cancels_pending_effects_and_cooldowns()
        {
            var r = new AbilityRuntime(); var a = Spell(); a.castTime = 2;
            r.TryStart(a, 0, n => true, () => Assert.Fail()); r.Reset(); r.Tick(10);
            Assert.AreEqual(0, r.Remaining(a, 0)); Assert.IsFalse(r.IsCasting);
        }
        [Test]
        public void Actor_runtimes_do_not_share_cooldowns()
        {
            var first = new AbilityRuntime(); var second = new AbilityRuntime(); var a = Spell();
            first.TryStart(a, 0, n => true, () => {});
            Assert.AreEqual(AbilityStartResult.Started, second.TryStart(a, 0, n => true, () => {}));
        }
        [Test]
        public void Invalid_data_is_rejected_without_spending_or_effects()
        {
            var r = new AbilityRuntime(); var a = Spell(); a.castTime = float.NaN;
            Assert.AreEqual(AbilityStartResult.Invalid, r.TryStart(a, 0, n => { Assert.Fail(); return true; }, () => Assert.Fail()));
        }
    }
}
