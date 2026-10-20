using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>The zone music's sets (2026-10-15, Chris's three packs): every path is a clip in Resources, every zone has a set of two
    /// to four, and the set follows night, the grey's edge and the dungeon (ZoneMusic.TracksFor).</summary>
    public class ZoneMusicTests
    {
        static readonly string[] ZoneIds = { "zone.oakhaven", "zone.khaven", "zone.peaks", "zone.ashrim", "zone.verdant", "zone.adit", "zone.lowtowns", "zone.hallows", "zone.crown" };   // Hallow's Creek plays the dungeon's set (2026-10-16); the Crown, a city

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
            foreach (var p in ZoneMusic.FieldFights) Check(p); foreach (var p in ZoneMusic.DungeonFights) Check(p); Check(ZoneMusic.WorldBossFight);
            Assert.Greater(n, 40, "paths checked");
            Assert.AreEqual("", missing.ToString(), "missing clips (Vol. 3's WAVs are not in git: python tools/import_music_vol3.py): ");
        }

        /// <summary>Play note 251 (2026-10-19, alkakrab's Vol. 3): every zone and dungeon has pieces of its own, no Vol. 3 piece in two
        /// zones, no two zones with the same set; all 25 pieces brought in are used; the world boss's track is kept for her alone.</summary>
        [Test] public void Each_zone_has_its_own_Vol3_pieces_and_the_world_boss_track_is_kept()
        {
            var owner = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var id in ZoneIds)
            {
                var s = ZoneMusic.Sets[id]; int own = 0;
                foreach (var list in new[] { s.day, s.night, s.grey })
                    foreach (var p in list)
                    {
                        if (!p.StartsWith(ZoneMusic.Fantasy3)) continue;
                        if (owner.TryGetValue(p, out string other) && other != id) Assert.Fail(p + " is in " + other + " and " + id);
                        owner[p] = id; own++;
                    }
                Assert.Greater(own, 0, id + " has Vol. 3 pieces of its own");
                foreach (var other in ZoneIds)
                    if (other != id) Assert.IsFalse(System.Linq.Enumerable.SequenceEqual(s.day, ZoneMusic.Sets[other].day), id + " and " + other + " share a day set");
            }
            var used = new System.Collections.Generic.HashSet<string>(owner.Keys);
            used.UnionWith(ZoneMusic.FieldFights); used.UnionWith(ZoneMusic.DungeonFights);
            Assert.IsFalse(used.Contains(ZoneMusic.WorldBossFight), "the world boss's track plays nowhere else");
            used.Add(ZoneMusic.WorldBossFight);
            var clips = Resources.LoadAll<AudioClip>(ZoneMusic.Fantasy3.TrimEnd('/'));
            Assert.AreEqual(25, clips.Length, "Vol. 3's 25 pieces are in Resources/Music/Fantasy3 (python tools/import_music_vol3.py)");
            foreach (var c in clips) Assert.IsTrue(used.Contains(ZoneMusic.Fantasy3 + c.name), c.name + " is used");
            CollectionAssert.Contains(ZoneMusic.FightsFor(ZoneMusic.FieldFight), ZoneMusic.FieldFight);
            CollectionAssert.Contains(ZoneMusic.FightsFor(ZoneMusic.DungeonFight), ZoneMusic.DungeonFight);
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
            Assert.AreSame(ZoneMusic.Sets["zone.adit"].day, ZoneMusic.TracksFor(new ZoneDefinition { id = "zone.adit" }, at, 1, out fight), "the Adit: its own dungeon set, day or night");
            CollectionAssert.IsSubsetOf(ZoneMusic.DungeonTracks, ZoneMusic.Sets["zone.adit"].day, "the crypts still among the Adit's");
            Assert.AreEqual(ZoneMusic.DungeonFight, fight, "the dungeon's fight track");
            Assert.AreSame(ZoneMusic.Sets["zone.khaven"].day, ZoneMusic.TracksFor(new ZoneDefinition { id = "zone.khaven" }, at, 1, out fight), "no night set: the day's");
            Assert.AreSame(ZoneMusic.Sets["zone.oakhaven"].day, ZoneMusic.TracksFor(new ZoneDefinition { id = "zone.nowhere" }, at, 0, out fight), "an unknown zone: Oakhaven's");
            // Hallow's Creek is silent out of doors (REWORK_LOWTOWNS_HALLOWS pick 2; b1 ch.21:19, "The unnatural silence here was absolute"),
            // the fight track still coming up in a fight; its cellars play the dungeon's set.
            Assert.AreSame(ZoneMusic.Silence, ZoneMusic.TracksFor(new ZoneDefinition { id = "zone.hallows" }, new Vector3(0, 1, -60), 0, out fight), "Hallow's Creek: no music on its streets");
            Assert.AreEqual(ZoneMusic.DungeonFight, fight, "and the dungeon's fight track in a fight");
            Assert.IsTrue(ZoneMusic.Sets["zone.hallows"].quiet);
            Assert.AreSame(ZoneMusic.Silence, ZoneMusic.TracksFor(new ZoneDefinition { id = "zone.hallows" }, new Vector3(0, 1, -60), 1, out fight), "and none at night either");
            Assert.IsTrue(ZoneMusic.Sets["zone.hallows"].dungeon, "its cellars play its own dungeon set");
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
