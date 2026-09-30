using System;
using System.Linq;
using NUnit.Framework;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The weather schedule: deterministic under a seed, only a zone's own kinds, one step of severity at a time, smooth turns,
    /// a calm start, and names that parse.
    /// </summary>
    public class WeatherScheduleTests
    {
        static readonly string[] Biomes = { "meadow", "gloom", "mountain", "ash" };
        static WeatherSchedule For(string biome, int seed) { return new WeatherSchedule(seed, WeatherSchedule.Defaults(biome)); }

        [Test] public void The_same_seed_gives_the_same_weather_read_in_any_order()
        {
            var a = For("meadow", 1771); var b = For("meadow", 1771);
            for (int i = 0; i < 300; i++) Assert.AreEqual(a.Spell(i), b.Spell(i), "spell " + i);
            var c = For("meadow", 1771);
            Assert.AreEqual(a.Spell(250), c.Spell(250), "Reading ahead first gives the same spell.");
            Assert.AreEqual(a.Spell(3), c.Spell(3));
        }

        [Test] public void Different_seeds_give_different_weather()
        {
            var a = For("meadow", 1771); var b = For("meadow", 1772);
            Assert.IsTrue(Enumerable.Range(0, 200).Any(i => a.Spell(i) != b.Spell(i)));
        }

        [Test] public void A_zone_only_gets_the_weather_in_its_table_and_all_of_it()
        {
            foreach (var biome in Biomes)
            {
                var s = For(biome, 42); var kinds = s.Table.Select(t => t.kind).ToList();
                var seen = Enumerable.Range(0, 600).Select(s.Spell).Distinct().ToList();
                foreach (var k in seen) Assert.Contains(k, kinds, biome + " never has " + k);
                foreach (var k in kinds) Assert.Contains(k, seen, biome + ": " + k + " comes round in 600 spells");
            }
            Assert.IsFalse(Enumerable.Range(0, 600).Select(For("ash", 9).Spell).Any(k => k == WeatherKind.Rain || k == WeatherKind.Storm), "It never rains on the Ashland Rim.");
        }

        [Test] public void Weather_turns_one_step_of_severity_at_a_time()
        {
            foreach (var biome in Biomes)
            {
                var s = For(biome, 7);
                for (int i = 1; i < 600; i++)
                    Assert.LessOrEqual(Math.Abs(WeatherSchedule.Severity(s.Spell(i)) - WeatherSchedule.Severity(s.Spell(i - 1))), 1, biome + " " + s.Spell(i - 1) + " to " + s.Spell(i));
            }
        }

        [Test] public void A_turn_blends_from_the_last_spell_into_the_next()
        {
            var s = For("meadow", 1771);
            int i = 1; while (WeatherLook.Of(s.Spell(i)).clouds == WeatherLook.Of(s.Spell(i - 1)).clouds) i++;
            float from = WeatherLook.Of(s.Spell(i - 1)).clouds, to = WeatherLook.Of(s.Spell(i)).clouds;
            double start = i * (double)WeatherSchedule.SpellSeconds;
            Assert.AreEqual(from, s.LookAt(start, out _).clouds, 1e-4f, "It starts from the last spell's look.");
            Assert.AreEqual(to, s.LookAt(start + WeatherSchedule.TurnSeconds, out _).clouds, 1e-4f, "The turn ends on the new spell's look.");
            float last = from;
            for (int k = 1; k <= 20; k++)
            {
                float now = s.LookAt(start + WeatherSchedule.TurnSeconds * k / 20.0, out var kind).clouds;
                Assert.AreEqual(s.Spell(i), kind);
                Assert.IsTrue(to > from ? now >= last - 1e-5f : now <= last + 1e-5f, "The turn runs one way, without jumps.");
                last = now;
            }
        }

        [Test] public void A_calm_start_lands_in_clear_or_fair_weather()
        {
            foreach (var biome in Biomes)
                for (int k = 0; k < 20; k++)
                {
                    var s = For(biome, 100 + k);
                    double t = s.CalmFrom(k * 3.3 * WeatherSchedule.SpellSeconds);
                    s.LookAt(t, out var kind);
                    Assert.LessOrEqual(WeatherSchedule.Severity(kind), 1, biome + ": " + kind);
                }
        }

        [Test] public void Every_kind_parses_by_its_name_and_its_player_facing_name()
        {
            foreach (WeatherKind k in Enum.GetValues(typeof(WeatherKind)))
            {
                Assert.IsTrue(WeatherSchedule.TryParse(k.ToString().ToLowerInvariant(), out var a) && a == k, k.ToString());
                Assert.IsTrue(WeatherSchedule.TryParse(WeatherSchedule.Name(k), out var b) && b == k, WeatherSchedule.Name(k));
            }
            Assert.IsTrue(WeatherSchedule.TryParse("flurries", out var f) && f == WeatherKind.Snow);
            Assert.IsTrue(WeatherSchedule.TryParse("ash_squall", out var q) && q == WeatherKind.AshSquall);
            Assert.IsFalse(WeatherSchedule.TryParse("hail", out _));
        }

        [Test] public void Every_look_keeps_its_amounts_in_range_and_the_fog_near_as_set()
        {
            foreach (WeatherKind k in Enum.GetValues(typeof(WeatherKind)))
            {
                var w = WeatherLook.Of(k);
                foreach (var v in new[] { w.clouds, w.dim, w.rain, w.snow, w.ash, w.mist, w.wind }) Assert.That(v, Is.InRange(0f, 1f), k.ToString());
                Assert.That(w.fog, Is.InRange(.5f, 1f), k + ": the fog closes in, but not to a wall");
            }
        }
    }
}
