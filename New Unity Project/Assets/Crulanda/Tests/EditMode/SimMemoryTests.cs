using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The sims' memory of you (5.5): regard from deeds, standings, whisper lines.</summary>
    public class SimMemoryTests
    {
        [Test] public void Deeds_move_regard_and_standings_follow()
        {
            var s = new SimAdventurer { id = "sim.t", name = "Tam Reed", classId = "class.ranger" };
            Assert.AreEqual(SimMemory.Standing.Stranger, SimMemory.Of(s));
            SimMemory.Note(s, SimMemory.Deed.MinuteTogether, 6);
            Assert.AreEqual(SimMemory.Standing.Acquaintance, SimMemory.Of(s), "six minutes grouped: an acquaintance");
            SimMemory.Note(s, SimMemory.Deed.KillsTogether, 10); SimMemory.Note(s, SimMemory.Deed.PassedToIt, 3);
            Assert.AreEqual(SimMemory.Standing.Friend, SimMemory.Of(s), "kills together and loot passed on: a friend");
            Assert.AreEqual(25, s.regard);
            for (int i = 0; i < 9; i++) SimMemory.Note(s, SimMemory.Deed.NeededOverIt);
            Assert.AreEqual(SimMemory.Standing.Rival, SimMemory.Of(s), "needing over its need nine times: a rival");
            Assert.IsTrue(SimMemory.Refuses(s));
            SimMemory.Note(s, SimMemory.Deed.NeededOverIt, 100); Assert.AreEqual(-100, s.regard, "clamped");
        }
        [Test] public void A_whisper_line_shows_its_direction_and_the_channel_has_a_colour()
        {
            Assert.AreEqual("To Wren Wick: hello", new ChatLine { channel = ChatChannel.Whisper, speaker = "You", to = "Wren Wick", text = "hello" }.Shown);
            Assert.AreEqual("Wren Wick whispers: hi", new ChatLine { channel = ChatChannel.Whisper, speaker = "Wren Wick", text = "hi" }.Shown);
            Assert.AreNotEqual(ZoneChat.Colour(ChatChannel.Zone), ZoneChat.Colour(ChatChannel.Whisper));
            Assert.AreEqual("Whisper", ZoneChat.Label(ChatChannel.Whisper));
        }
    }
}
