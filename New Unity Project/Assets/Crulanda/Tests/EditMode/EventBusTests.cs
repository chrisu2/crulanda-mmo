using NUnit.Framework;
using Crulanda.Core;

namespace Crulanda.Tests
{
    public class EventBusTests
    {
        struct Ping { public int Value; }

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            CrulandaLog.ResetToDefaults();
            CrulandaLog.Sink = (entry, ctx) => { }; // keep expected errors out of the Unity console
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            CrulandaLog.ResetToDefaults();
        }

        [Test]
        public void Subscribers_receive_published_events()
        {
            int received = 0;
            EventBus.Subscribe<Ping>(p => received = p.Value);
            EventBus.Publish(new Ping { Value = 42 });
            Assert.AreEqual(42, received);
        }

        [Test]
        public void Unsubscribe_stops_delivery()
        {
            int calls = 0;
            System.Action<Ping> handler = p => calls++;
            EventBus.Subscribe(handler);
            EventBus.Unsubscribe(handler);
            EventBus.Publish(new Ping());
            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, EventBus.SubscriberCount<Ping>());
        }

        [Test]
        public void Throwing_handler_does_not_block_other_handlers()
        {
            int calls = 0;
            EventBus.Subscribe<Ping>(p => { throw new System.InvalidOperationException("boom"); });
            EventBus.Subscribe<Ping>(p => calls++);
            EventBus.Publish(new Ping());
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Handler_may_unsubscribe_itself_during_publish()
        {
            int calls = 0;
            System.Action<Ping> handler = null;
            handler = p => { calls++; EventBus.Unsubscribe(handler); };
            EventBus.Subscribe(handler);

            EventBus.Publish(new Ping());
            EventBus.Publish(new Ping());
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Publishing_without_subscribers_is_a_no_op()
        {
            Assert.DoesNotThrow(() => EventBus.Publish(new Ping()));
        }
    }
}
