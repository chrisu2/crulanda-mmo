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
    /// The ways through the doors and gates (Phase 8.6 step 2, 2026-10-15; Chris, note 205: "running into each other and sometimes get
    /// stuck or delayed at door of inn"):
    /// - every inn's door (the taverns', the waystations', the Hearth-Tree's) and every gate (a fortified gate's, a cave's in the Sealed
    ///   Adit) leaves the navmesh a way through it over 1.2 m wide, worked out from the geometry the builder makes;
    /// - the doorway's lane lets one on at a time going each way, the most pressed first (a carrier, then a villager, then a sim, then
    ///   an idler), the next waiting off it; after the innkeeper's bedtime one lane serves both ways; a holder gone quiet lets it go;
    /// - the stuck watchdog counts headway (a quarter metre moved, or a quarter metre less to go): a step aside after 3 s without, the
    ///   errand given up after 10.
    /// </summary>
    public class DoorwayTests
    {
        static readonly string[] Files = { "oakhaven", "khaven", "peaks", "ashrim", "verdant", "adit" };
        static ZoneDefinition Zone(string file) { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Zones/" + file + ".json"))); }
        /// <summary>A cave's half-width at a distance along its plan's rows (x, z, half-width, height, floor drop), or -1 with no plan.</summary>
        static float HalfAlong(ZoneProp cave, float along)
        {
            if (cave == null || cave.plan == null || cave.plan.Length < 10) return -1;
            float s = 0; float best = cave.plan[2];
            for (int i = 5; i + 4 < cave.plan.Length; i += 5)
            {
                var a = new Vector2(cave.plan[i - 5], cave.plan[i - 4]); var b = new Vector2(cave.plan[i], cave.plan[i + 1]); float seg = Vector2.Distance(a, b);
                if (s + seg >= along) return Mathf.Lerp(cave.plan[i - 3], cave.plan[i + 2], seg > 0 ? (along - s) / seg : 0);
                s += seg; best = cave.plan[i + 2];
            }
            return best;
        }

        [Test] public void Every_inn_door_and_gate_is_wide_enough_to_pass_someone()
        {
            const float pass = 1.2f; var problems = new List<string>(); int doors = 0, gates = 0;
            foreach (var file in Files)
            {
                var z = Zone(file);
                foreach (var p in z.props.Where(p => p != null))
                {
                    if (p.kind == "inn")
                    {
                        var plan = ZoneBuilder.InnPlan.For(p.size.x > 0 ? p.size : new Vector2(12, 8), p.variant); doors++;
                        if (plan.NavGap <= pass) problems.Add(file + ": " + p.name + "'s door leaves " + plan.NavGap.ToString("0.00") + " m");
                    }
                    if (p.kind == "treehouse" && p.style == "inn")
                    {
                        doors++; float gap = ZoneBuilder.NavGap(ZoneBuilder.InnDoorW);
                        if (gap <= pass) problems.Add(file + ": " + p.name + "'s door leaves " + gap.ToString("0.00") + " m");
                    }
                    if (p.kind == "gate")
                    {
                        gates++; float gap = ZoneBuilder.GateOpening(p.size.x > 0 ? p.size.x : 6.4f);
                        if (gap <= pass) problems.Add(file + ": the gate '" + p.name + "' leaves " + gap.ToString("0.00") + " m");
                    }
                }
                // A cave's gate (ZoneGate) climbs or slides clear of the way: the way is the passage, its walls a metre in from its sides
                // (the navmesh's agent radius each side). Measured on the cave's own plan where it has one.
                foreach (var g in z.gates ?? new ZoneGateDef[0])
                {
                    if (g == null) continue;
                    var cave = z.props.FirstOrDefault(p => p != null && p.kind == "cavern" && p.name == g.cave);
                    float half = HalfAlong(cave, g.along); if (half < 0) continue;
                    gates++; float gap = 2 * half - 2 * ZoneBuilder.NavAgentRadius;
                    if (gap <= pass) problems.Add(file + ": the gate '" + g.title + "' in " + g.cave + " leaves " + gap.ToString("0.00") + " m");
                }
            }
            Assert.AreEqual(5, doors, "The Golden Cask, the Cracked Hearth, Pilgrims' Rest, Wain's Rest and the Hearth-Tree.");
            Assert.GreaterOrEqual(gates, 5, "The Peaks' toll gate and the Sealed Adit's gates were measured.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
            Assert.Less(ZoneBuilder.NavGap(1.6f), pass, "The old 1.6 m inn door: one at a time, nobody past anybody.");
        }

        static ZoneDoorway Way() { return new ZoneDoorway { name = "test", middle = Vector3.zero, inward = Vector3.forward, across = Vector3.right, width = ZoneBuilder.InnDoorW, walkable = ZoneBuilder.NavGap(ZoneBuilder.InnDoorW) }; }

        [Test] public void The_lane_lets_one_on_at_a_time_the_most_pressed_first()
        {
            var way = Way(); object sim = "sim", villager = "villager", carrier = "carrier", idler = "idler";
            Assert.IsTrue(way.Request(sim, ZoneDoorway.Sim, true, 0, out _), "A free lane: on it.");
            Assert.IsFalse(way.Request(villager, ZoneDoorway.Villager, true, .1f, out int place), "Taken: the next waits.");
            Assert.AreEqual(0, place, "First in line.");
            Assert.IsFalse(way.Request(idler, ZoneDoorway.Idler, true, .2f, out place)); Assert.AreEqual(1, place, "An idler behind the villager.");
            Assert.IsFalse(way.Request(carrier, ZoneDoorway.Carrier, true, .3f, out place)); Assert.AreEqual(0, place, "A carrier with a load goes to the front of the line.");
            Assert.IsTrue(way.Request(villager, ZoneDoorway.Villager, false, .4f, out _), "Going out is the other lane by day: free.");
            way.Release(villager);
            way.Release(sim);
            Assert.IsFalse(way.Request(idler, ZoneDoorway.Idler, true, .5f, out _), "The lane's free, but the carrier is first.");
            Assert.IsTrue(way.Request(carrier, ZoneDoorway.Carrier, true, .5f, out _), "The carrier goes.");
            Assert.AreSame(carrier, way.Holder(true));
            way.Release(carrier);
            Assert.IsTrue(way.Request(idler, ZoneDoorway.Idler, true, .6f, out _), "Then the idler.");
            // Where they wait: off the lane's end, back the way they came and to the lane's own side, a step further for each.
            var first = way.QueueSpot(true, 0); var second = way.QueueSpot(true, 1);
            Assert.Less(first.z, way.Entry(true).z - .9f, "Outside, a metre and more back from the lane's end.");
            Assert.Greater(first.x * way.Side(true), ZoneDoorway.LaneOff, "Beside the lane, not on it.");
            Assert.Less(second.z, first.z, "The second behind the first.");
            Assert.AreEqual(1.2f, Vector3.Distance(way.Entry(true), way.Exit(true)), 1e-4f, "The lane's ends 1.2 m apart.");
            Assert.Greater(way.Entry(true).x * way.Entry(false).x, -1, "In on one side, out on the other.");
            Assert.Less(way.Entry(true).x * way.Entry(false).x, 0);
        }

        [Test] public void At_night_one_lane_serves_both_ways_and_a_quiet_holder_lets_it_go()
        {
            var way = Way(); way.Single = true; object a = "in", b = "out";
            Assert.IsTrue(way.Request(a, ZoneDoorway.Villager, true, 0, out _), "In on the open leaf's lane.");
            Assert.IsFalse(way.Request(b, ZoneDoorway.Villager, false, .1f, out _), "Out waits for it: one leaf, one at a time.");
            Assert.AreEqual(way.Entry(true).x, way.Exit(false).x, 1e-4f, "Both ways on the same line.");
            Assert.Greater(way.Entry(true).x, 0, "The right-hand leaf stays open (ZoneDoor.SetHalfShut shuts the first, on the left).");
            // The holder goes quiet (hidden indoors, gone): after a second and a half the lane is let go.
            Assert.IsFalse(way.Request(b, ZoneDoorway.Villager, false, 1, out _), "Still held at 1 s.");
            Assert.IsTrue(way.Request(b, ZoneDoorway.Villager, false, .1f + ZoneDoorway.Silence + .2f, out _), "Let go after a silence.");
            // Held too long (stuck on it): let go after HoldLimit even while heard from.
            var stuck = Way(); object c = "stuck", d = "next";
            Assert.IsTrue(stuck.Request(c, ZoneDoorway.Sim, true, 0, out _));
            for (float t = 1; t < ZoneDoorway.HoldLimit; t += 1) { stuck.Request(c, ZoneDoorway.Sim, true, t, out _); stuck.Request(d, ZoneDoorway.Sim, true, t, out _); }
            Assert.IsTrue(stuck.Request(d, ZoneDoorway.Sim, true, ZoneDoorway.HoldLimit + .5f, out _), "Nobody waits on a lane nobody is walking.");
        }

        [Test] public void The_watchdog_counts_headway()
        {
            var h = new Headway(); var at = Vector3.zero;
            h.Reset(at, 0);
            h.Tick(at + Vector3.right * .1f, 5, 1); Assert.AreEqual(1, h.StillFor(1), 1e-4f, "A tenth of a metre is not headway.");
            Assert.IsFalse(h.StepDue(2.9f)); Assert.IsTrue(h.StepDue(3.1f), "Three seconds without: a step aside."); Assert.IsFalse(h.StepDue(3.2f), "Once a stall.");
            Assert.IsFalse(h.Lost(9.9f)); Assert.IsTrue(h.Lost(10.1f), "Ten seconds without: give it up.");
            h.Tick(at + Vector3.right * .4f, 5, 10.2f); Assert.AreEqual(0, h.StillFor(10.2f), 1e-4f, "Moved: headway again.");
            h.Tick(at + Vector3.right * .45f, 4.6f, 11); h.Tick(at + Vector3.right * .45f, 4.3f, 12); Assert.AreEqual(0, h.StillFor(12), 1e-4f, "Less to go (round a corner, a pushing crowd) is headway too.");
        }
    }
}
