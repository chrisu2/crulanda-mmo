using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Sweep (Lowtowns pick 9; EncounterSession.Sweep; Docs/ZONE_LOWTOWNS.md "The Sweep"), its data:
    /// - the sweep's warders are a camp out only while it runs (after "sweep"), hired warders (conflict 1), walking Pell Row's west stretch;
    /// - a careful way exists: the rain-spout's foot is out of a warder's sight from the patrol's road, and the fire escape's foot well clear of it;
    /// - the quest at Brother Aldo: a flag on "lowtowns.sweep", coin, standing with the Lowtowners and a keepsake that is a quest item;
    /// - the words cite the books (the horns' two-tone wail, ch.3:243; Rule One and Rule Two, ch.1:257-259), leave the Copper-Tithe out
    ///   (conflict 2) and name no machine.
    /// </summary>
    public class LowtownsSweepTests
    {
        static ZoneDefinition z; static List<ZoneBuilder.RoofLeg> legs;
        static float H(float x, float y) { return ZoneBuilder.TerraceGround(z.terraces, x, y, 0); }
        static string Content(params string[] path) { return File.ReadAllText(Path.Combine(new[] { Application.dataPath, "Crulanda", "EncounterContent" }.Concat(path).ToArray())); }
        [OneTimeSetUp] public void Load()
        {
            z = JsonUtility.FromJson<ZoneDefinition>(Content("Zones", "lowtowns.json"));
            legs = ZoneBuilder.RoofRoute(z, H);
        }
        static ZoneCamp Camp() { return z.camps.Single(c => c.after == EncounterSession.SweepAfter); }
        static float Flat(Vector3 a, Vector2 b) { return Vector2.Distance(new Vector2(a.x, a.z), b); }
        /// <summary>Distance in plan from a point to a road's middle line.</summary>
        static float ToRoad(ZonePath r, Vector2 p)
        {
            float best = float.MaxValue;
            for (int k = 0; k + 1 < r.points.Length; k++)
            {
                Vector2 a = r.points[k], b = r.points[k + 1], ab = b - a;
                float u = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * u));
            }
            return best;
        }

        [Test] public void The_sweep_warders_are_out_only_while_it_runs_and_walk_Pell_Row()
        {
            var c = Camp();
            Assert.AreEqual("Hired warder", c.mob, "the Council's hired warders do the sweep (conflict 1), not Seekers");
            Assert.AreEqual("warden", c.look); Assert.AreEqual("warder", c.tag);
            Assert.GreaterOrEqual(c.count, 2); Assert.IsFalse(c.elite);
            Assert.That(c.levelMin, Is.InRange(15, 20)); Assert.That(c.levelMax, Is.InRange(c.levelMin, 20));
            StringAssert.Contains("ch.3:243", c.canonStatus); StringAssert.Contains("conflict 1", c.canonStatus);
            Assert.IsNotNull(c.route); Assert.GreaterOrEqual(c.route.Length, 2, "a patrol's road");
            var pell = z.roads.Single(r => r.name == "Pell Row");
            foreach (var p in c.route) Assert.Less(ToRoad(pell, p), 3, "the road " + p + " is on Pell Row");
            // The rare still comes last (LowtownsZoneTests).
            Assert.Greater(z.camps.Last().rare, 0);
        }

        [Test] public void A_careful_way_over_the_roofs_is_out_of_the_warders_sight()
        {
            Assert.NotNull(legs, "the roof route");
            var c = Camp();
            var spout = legs.Single(l => l.kind == "climb" && l.name == ZoneBuilder.SpoutName);
            var escape = legs.Single(l => l.kind == "climb" && l.name == ZoneBuilder.EscapeName);
            foreach (var p in c.route)
            {
                Assert.Greater(Flat(spout.a, p), EncounterSession.SweepSight + 1, "the rain-spout's foot is out of sight of the road at " + p + " (a warder there does not see her climb)");
                Assert.Greater(Flat(escape.a, p), EncounterSession.SweepSight * 2, "the fire escape's foot is well clear of the road at " + p);
            }
            // The soup-hall (Brother Aldo's) is far down the lane from the sweep.
            foreach (var p in c.route) Assert.Greater(Vector2.Distance(EncounterSession.SweepKeeperSpot, p), 40);
            Assert.IsTrue(z.props.Any(p => p.kind == "soup_hall" && Vector2.Distance(p.at, EncounterSession.SweepKeeperSpot) < 16), "Brother Aldo stands at the soup-hall");
            Assert.IsTrue(z.life.residents.Any(s => s.name == EncounterSession.SweepKeeper), "Brother Aldo is in the data");
        }

        [Test] public void The_quest_pays_coin_standing_and_a_keepsake_and_cites_the_books()
        {
            var db = QuestDatabase.Parse(Directory.GetFiles(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Quests"), "*.json").Select(File.ReadAllText));
            var q = db.Quests["side.aldo.sweep"];
            Assert.AreEqual("The Sweep", q.title); Assert.AreEqual(EncounterSession.SweepKeeper, q.giver); Assert.AreEqual(EncounterSession.SweepKeeper, q.turnIn);
            var o = q.steps.Single().objectives.Single(); Assert.AreEqual("flag", o.type); Assert.AreEqual("key:" + EncounterSession.SweepKey, o.target);
            Assert.Greater(q.rewards.gold, 0); Assert.Greater(q.rewards.xp, 0);
            Assert.IsTrue(q.rewards.reputation.Any(r => r.faction == EncounterSession.SweepFaction && r.amount > 0), "standing with the Lowtowners");
            Assert.IsTrue(db.Factions.ContainsKey(EncounterSession.SweepFaction));
            Assert.AreEqual(1, q.rewards.items.Length, "a small keepsake"); Assert.IsTrue(db.Items.ContainsKey(q.rewards.items[0]), "the keepsake is a quest item");
            StringAssert.StartsWith("CANON-EXPANDED", q.canonStatus);
            foreach (var cite in new[] { "ch.3:243", "ch.1:257-259", "ch.1:239" }) StringAssert.Contains(cite, q.canonStatus);
            Assert.Greater(EncounterSession.SweepGold, 0); Assert.Greater(EncounterSession.SweepStanding, 0);
        }

        [Test] public void The_words_have_the_horns_and_the_rules_no_Copper_Tithe_and_no_machines()
        {
            StringAssert.Contains("two-tone wail", EncounterSession.HornsLine);
            StringAssert.Contains("brass horns", EncounterSession.HornsLine);
            StringAssert.Contains("RULE ONE IF YOU SEE A SEEKER EXIST SOMEWHERE ELSE", EncounterSession.SweepRulesLine);
            StringAssert.Contains("NEVER INTERFERE", EncounterSession.SweepRulesLine);
            var words = new[] { EncounterSession.HornsLine, EncounterSession.SweepRulesLine, EncounterSession.WardersLine, EncounterSession.ChildFoundLine, EncounterSession.ChildTakenLine,
                EncounterSession.ChildSeenLine, EncounterSession.ChildLostLine, EncounterSession.ChildSafeLine, EncounterSession.SeekerLine, EncounterSession.EscapeDownLine };
            var quest = Content("Quests", "lowtowns.json"); int at = quest.IndexOf("\"side.aldo.sweep\""); Assert.Greater(at, 0);
            var sweep = quest.Substring(at, quest.IndexOf("\"side.nan.lamp\"") - at);
            foreach (var w in words.Append(sweep))
            {
                StringAssert.DoesNotContain("Copper-Tithe", w, "conflict 2: the Copper-Tithe are out");
                foreach (var m in new[] { "machine", "engine", "clockwork", "gears", "piston" }) StringAssert.DoesNotContain(m, w.ToLowerInvariant(), "no machines");
            }
        }
    }
}
