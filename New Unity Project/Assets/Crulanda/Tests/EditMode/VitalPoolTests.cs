using NUnit.Framework;
using Crulanda.Gameplay;

namespace Crulanda.Tests
{
    public class VitalPoolTests
    {
        [Test]
        public void Starts_full_or_empty_as_requested()
        {
            Assert.AreEqual(100, new VitalPool(100, true).Current);
            Assert.AreEqual(0, new VitalPool(100, false).Current);
        }

        [Test]
        public void Change_clamps_to_max_and_reports_applied_amount()
        {
            var p = new VitalPool(100, false);
            p.SetCurrent(90);
            Assert.AreEqual(10, p.Change(25));
            Assert.AreEqual(100, p.Current);
        }

        [Test]
        public void Change_clamps_to_zero_and_reports_applied_amount()
        {
            var p = new VitalPool(100, true);
            p.SetCurrent(30);
            Assert.AreEqual(-30, p.Change(-50));
            Assert.AreEqual(0, p.Current);
            Assert.IsTrue(p.IsEmpty);
        }

        [Test]
        public void SetMax_without_preserve_only_clamps()
        {
            var p = new VitalPool(100, true);
            p.SetMax(50, false);
            Assert.AreEqual(50, p.Current);
            p.SetMax(200, false);
            Assert.AreEqual(50, p.Current);
        }

        [Test]
        public void SetMax_with_preserve_keeps_the_ratio()
        {
            var p = new VitalPool(100, true);
            p.SetCurrent(50);
            p.SetMax(200, true);
            Assert.AreEqual(100, p.Current);
        }

        [Test]
        public void Changed_fires_with_old_and_new_values()
        {
            var p = new VitalPool(100, true);
            int oldValue = -1, newValue = -1, calls = 0;
            p.Changed += (pool, o, n) => { oldValue = o; newValue = n; calls++; };

            p.Change(-40);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(100, oldValue);
            Assert.AreEqual(60, newValue);
        }

        [Test]
        public void Changed_does_not_fire_when_nothing_changes()
        {
            var p = new VitalPool(100, true);
            int calls = 0;
            p.Changed += (pool, o, n) => calls++;
            p.Change(10); // already full
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Ratio_is_zero_when_max_is_zero()
        {
            Assert.AreEqual(0f, new VitalPool(0, true).Ratio);
        }
    }
}
