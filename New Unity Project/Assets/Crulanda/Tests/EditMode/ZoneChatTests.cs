using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>What you type goes to its channel (zone chat, playtest note 62).</summary>
    public class ZoneChatTests
    {
        [Test] public void Prefixes_pick_the_channel_and_plain_text_is_zone()
        {
            Assert.AreEqual((ChatChannel.Zone, "hello all"), ZoneChat.Parse("hello all"));
            Assert.AreEqual((ChatChannel.Zone, "where is the mill?"), ZoneChat.Parse("/z where is the mill?"));
            Assert.AreEqual((ChatChannel.Trade, "wts copper ore"), ZoneChat.Parse("/t wts copper ore"));
            Assert.AreEqual((ChatChannel.LFG, "lf1m wolves"), ZoneChat.Parse("/lfg lf1m wolves"));
            Assert.AreEqual((ChatChannel.Party, "inc"), ZoneChat.Parse("/p inc"));
            Assert.AreEqual((ChatChannel.Say, "hi there"), ZoneChat.Parse("/s hi there"));
            Assert.AreEqual((ChatChannel.Zone, ""), ZoneChat.Parse("   "));
            Assert.AreEqual((ChatChannel.Zone, "/dance"), ZoneChat.Parse("/dance"), "an unknown command is said in Zone as typed");
        }
        [Test] public void A_line_shows_its_channel_and_speaker()
        {
            Assert.AreEqual("[Trade] Wren Wick: WTS ore", new ChatLine { channel = ChatChannel.Trade, speaker = "Wren Wick", text = "WTS ore" }.Shown);
            Assert.AreEqual("Expedition saved.", new ChatLine { channel = ChatChannel.System, text = "Expedition saved." }.Shown);
        }
    }
}
