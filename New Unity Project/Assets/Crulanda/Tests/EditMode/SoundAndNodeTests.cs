using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>The sound mixer's sums and the worked node's population-scaled return (Chris, 2026-10-15).</summary>
    public class SoundAndNodeTests
    {
        [Test]
        public void MutingAChannelOrTheMasterSilencesIt()
        {
            var keep = new float[AudioMix.All.Length]; var mute = new bool[AudioMix.All.Length];
            for (int i = 0; i < AudioMix.All.Length; i++) { keep[i] = AudioMix.Level(AudioMix.All[i]); mute[i] = AudioMix.Muted(AudioMix.All[i]); }
            try
            {
                foreach (var c in AudioMix.All) AudioMix.SetMuted(c, false);
                AudioMix.SetLevel(AudioMix.Channel.Master, .5f); AudioMix.SetLevel(AudioMix.Channel.Effects, .8f);
                Assert.AreEqual(.4f, AudioMix.Effective(AudioMix.Channel.Effects), 1e-4f, "a channel plays at its level times the master's");
                AudioMix.SetMuted(AudioMix.Channel.Effects, true);
                Assert.AreEqual(0, AudioMix.Effective(AudioMix.Channel.Effects), "a muted channel is silent");
                Assert.AreEqual(.8f, AudioMix.Level(AudioMix.Channel.Effects), 1e-4f, "muting keeps the level");
                AudioMix.SetMuted(AudioMix.Channel.Effects, false); AudioMix.SetMuted(AudioMix.Channel.Master, true);
                Assert.AreEqual(0, AudioMix.Effective(AudioMix.Channel.CombatMusic), "the master's mute silences every channel");
                AudioMix.SetMuted(AudioMix.Channel.Master, false); AudioMix.SetLevel(AudioMix.Channel.Music, .3f);
                Assert.AreEqual(.3f, ZoneMusic.Volume, 1e-4f, "the music channel is the /music command's level");
            }
            finally { for (int i = 0; i < AudioMix.All.Length; i++) { AudioMix.SetLevel(AudioMix.All[i], keep[i]); AudioMix.SetMuted(AudioMix.All[i], mute[i]); } }
        }

        [Test]
        public void AWorkedNodeComesBackSoonerWithMorePeopleAbout()
        {
            Assert.AreEqual(180, Crulanda.Encounter.EncounterSession.NodeRespawn(180, 0), 1e-3f, "alone: its own respawn");
            Assert.AreEqual(90, Crulanda.Encounter.EncounterSession.NodeRespawn(180, 10), 1e-3f, "ten about: half");
            Assert.AreEqual(72, Crulanda.Encounter.EncounterSession.NodeRespawn(180, 40), 1e-3f, "never under two fifths");
            Assert.Less(Crulanda.Encounter.EncounterSession.NodeRespawn(180, 5), 180);
        }
    }
}
