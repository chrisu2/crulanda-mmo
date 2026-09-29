using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Core;
using Crulanda.Data;

namespace Crulanda.Tests
{
    public class ContentRegistryTests
    {
        readonly List<Object> _created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            CrulandaLog.ResetToDefaults();
            CrulandaLog.Sink = (entry, ctx) => { }; // expected errors should not fail the test run
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created) Object.DestroyImmediate(o);
            _created.Clear();
            CrulandaLog.ResetToDefaults();
        }

        ActorArchetypeDefinition Make(string id)
        {
            var def = ScriptableObject.CreateInstance<ActorArchetypeDefinition>();
            def.EditorSetIdentity(new ContentId(id), id, CanonStatus.GameOnly);
            _created.Add(def);
            return def;
        }

        [Test]
        public void Added_definitions_can_be_found_by_id()
        {
            var registry = new ContentRegistry();
            var def = Make("actor.a");
            Assert.IsTrue(registry.TryAdd(def));

            ActorArchetypeDefinition found;
            Assert.IsTrue(registry.TryGet(new ContentId("actor.a"), out found));
            Assert.AreSame(def, found);
            Assert.AreEqual(1, registry.Count);
        }

        [Test]
        public void Duplicate_ids_are_rejected_and_first_wins()
        {
            var registry = new ContentRegistry();
            var first = Make("actor.a");
            var second = Make("actor.a");
            Assert.IsTrue(registry.TryAdd(first));
            Assert.IsFalse(registry.TryAdd(second));
            Assert.AreSame(first, registry.Get<ActorArchetypeDefinition>(new ContentId("actor.a")));
        }

        [Test]
        public void Invalid_ids_and_nulls_are_rejected()
        {
            var registry = new ContentRegistry();
            Assert.IsFalse(registry.TryAdd(Make("Bad Id")));
            Assert.IsFalse(registry.TryAdd(null));
            Assert.AreEqual(0, registry.Count);
        }

        [Test]
        public void Get_throws_for_unknown_id()
        {
            Assert.Throws<KeyNotFoundException>(() =>
                new ContentRegistry().Get<ActorArchetypeDefinition>(new ContentId("actor.missing")));
        }

        [Test]
        public void All_returns_definitions_of_the_requested_type()
        {
            var registry = new ContentRegistry();
            registry.TryAdd(Make("actor.a"));
            registry.TryAdd(Make("actor.b"));
            Assert.AreEqual(2, registry.All<ActorArchetypeDefinition>().Count());
        }
    }
}
