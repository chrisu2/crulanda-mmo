using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>The zone music's sets (2026-10-15, Chris's three packs): every path is a clip in Resources, every zone has a set of two
    /// to four, and the set follows night, the grey's edge and the dungeon (ZoneMusic.TracksFor).</summary>
    public class ZoneMusicTests
    {
        static readonly string[] ZoneIds = { "zone.oakhaven", "zone.khaven", "zone.peaks", "zone.ashrim", "zone.verdant", "zone.adit", "zone.lowtowns" };

        [Test] public void Every_track_in_every_set_is_in_Resources()
        {
            var missing = new System.Text.StringBuilder(); int n = 0;
            void Check(string path) { n++; if (Resources.Load<AudioClip>(path) == null) missing.Append(path).Append("; "); }
            foreach (var kv in ZoneMusic.Sets)
            {
                foreach (var p in kv.Value.day) Check(p); foreach (var p in kv.Value.night) Check(p); foreach (var p in kv.Value.grey) Check(p);
                if (kv.Value.combat != null) Check(kv.Value.combat);
            }
            foreach (var p in ZoneMusic.DungeonTracks) Check(p);
            Check(ZoneMusic.FieldFight); Check(ZoneMusic.DungeonFight);
            Assert.Greater(n, 12, "paths checked");
            Assert.AreEqual("", missing.ToString(), "missing clips: ");
        }

        [Test] public void Every_zone_has_a_set_of_two_to_four_tracks()
        {
            foreach (var id in ZoneIds)
            {
                Assert.IsTrue(ZoneMusic.Sets.ContainsKey(id), id + " has a set");
                var s = ZoneMusic.Sets[id];
                Assert.That(s.day.Length, Is.InRange(2, 4), id + " day tracks");
                Assert.That(s.night.Length, Is.LessThanOrEqualTo(4), id + " night tracks");
                Assert.That(s.grey.Length, Is.LessThanOrEqualTo(4), id + " grey tracks");
            }
        }

        [Test] public void The_set_follows_night_the_grey_and_the_dungeon()
        {
            var oak = new ZoneDefinition { id = "zone.oakhaven", wasting = new ZoneWasting { x = 254 } };
            var at = new Vector3(0, 1, 0);
            Assert.AreSame(ZoneMusic.Sets["zone.oakhaven"].day, ZoneMusic.TracksFor(oak, at, 0, out string fight), "by day");
            Assert.AreEqual(ZoneMusic.FieldFight, fight, "the field's fight track");
            Assert.AreSame(ZoneMusic.Sets["zone.oakhaven"].night, ZoneMusic.TracksFor(oak, at, 1, out fight), "at night");
            Assert.AreSame(ZoneMusic.Sets["zone.oakhaven"].grey, ZoneMusic.TracksFor(oak, new Vector3(254 - ZoneMusic.GreyReach + 1, 1, 0), 0, out fight), "near the grey");
            Assert.AreSame(ZoneMusic.DungeonTracks, ZoneMusic.TracksFor(new ZoneDefinition { id = "zone.adit" }, at, 0, out fight), "the Adit");
            Assert.AreEqual(ZoneMusic.DungeonFight, fight, "the dungeon's fight track");
            Assert.AreSame(ZoneMusic.Sets["zone.khaven"].day, ZoneMusic.TracksFor(new ZoneDefinition { id = "zone.khaven" }, at, 1, out fight), "no night set: the day's");
            Assert.AreSame(ZoneMusic.Sets["zone.oakhaven"].day, ZoneMusic.TracksFor(new ZoneDefinition { id = "zone.nowhere" }, at, 0, out fight), "an unknown zone: Oakhaven's");
        }

        [Test] public void The_volume_is_kept_between_0_and_1()
        {
            float before = PlayerPrefs.GetFloat(ZoneMusic.VolumeKey, ZoneMusic.DefaultVolume);
            try
            {
                ZoneMusic.Volume = 1.7f; Assert.AreEqual(1f, ZoneMusic.Volume, 1e-5f);
                ZoneMusic.Volume = -.3f; Assert.AreEqual(0f, ZoneMusic.Volume, 1e-5f);
                ZoneMusic.Volume = .5f; Assert.AreEqual(.5f, ZoneMusic.Volume, 1e-5f);
            }
            finally { PlayerPrefs.SetFloat(ZoneMusic.VolumeKey, before); }
        }
    }
}
