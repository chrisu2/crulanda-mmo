using System.IO;
using NUnit.Framework;
using Crulanda.Persistence;

namespace Crulanda.Tests
{
    public class SaveFileStoreTests
    {
        string _dir;
        SaveFileStore _store;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "crulanda_tests_" + System.Guid.NewGuid().ToString("N"));
            _store = new SaveFileStore(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        static SaveEnvelope Make(string payload)
        {
            return new SaveEnvelope
            {
                formatVersion = 1, payloadType = "Test", gameVersion = "0.0.1",
                savedAtUtc = "2026-01-01T00:00:00Z", payloadJson = payload
            };
        }

        [Test]
        public void Write_then_read_round_trips()
        {
            _store.Write("slot1", Make("{\"a\":1}"));

            SaveEnvelope loaded; string error;
            Assert.IsTrue(_store.TryRead("slot1", out loaded, out error), error);
            Assert.AreEqual(1, loaded.formatVersion);
            Assert.AreEqual("{\"a\":1}", loaded.payloadJson);
            Assert.IsNull(error);
        }

        [Test]
        public void Overwrite_keeps_a_backup_and_new_data_wins()
        {
            _store.Write("slot1", Make("first"));
            _store.Write("slot1", Make("second"));

            SaveEnvelope loaded; string error;
            Assert.IsTrue(_store.TryRead("slot1", out loaded, out error));
            Assert.AreEqual("second", loaded.payloadJson);
            Assert.IsTrue(File.Exists(_store.PathFor("slot1") + ".bak"));
        }

        [Test]
        public void Corrupt_primary_falls_back_to_backup()
        {
            _store.Write("slot1", Make("good"));
            _store.Write("slot1", Make("newer"));
            File.WriteAllText(_store.PathFor("slot1"), "{ this is not json");

            SaveEnvelope loaded; string error;
            Assert.IsTrue(_store.TryRead("slot1", out loaded, out error));
            Assert.AreEqual("good", loaded.payloadJson);
            Assert.IsNotNull(error); // caller is told the backup was used
        }

        [Test]
        public void Missing_slot_fails_cleanly()
        {
            SaveEnvelope loaded; string error;
            Assert.IsFalse(_store.TryRead("nope", out loaded, out error));
            Assert.IsNull(loaded);
            Assert.IsNotNull(error);
        }

        [Test]
        public void Invalid_slot_names_are_rejected()
        {
            Assert.Throws<System.ArgumentException>(() => _store.PathFor("../evil"));
            Assert.Throws<System.ArgumentException>(() => _store.PathFor(""));
            Assert.IsTrue(SaveFileStore.IsValidSlotName("auto_1-A"));
        }

        [Test]
        public void ListSlots_and_Delete_work()
        {
            _store.Write("b", Make("x"));
            _store.Write("a", Make("x"));
            CollectionAssert.AreEqual(new[] { "a", "b" }, _store.ListSlots());

            _store.Delete("a");
            CollectionAssert.AreEqual(new[] { "b" }, _store.ListSlots());
        }
    }
}
