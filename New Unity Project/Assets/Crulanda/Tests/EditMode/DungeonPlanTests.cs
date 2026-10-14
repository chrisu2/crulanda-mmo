using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The order of a dungeon run through a web of wings (DungeonPlan; the Adit rework, Docs/ADIT_REWORK.md section 9; GAME-ONLY), on small
    /// made-up dungeons laid out on a line (straight-line distances, no navmesh): the nearest open wing next, a wing opening only after those
    /// it comes after, one side of a fork (the side a live quest wants), both in the full run and the second straight after the first;
    /// keeping to the height the party is at; each wing's fights in its own order, a boss and his guards one fight; the quick run what must be
    /// done, the standard run the side bosses in its wings too, the full run every side room, each fitted in where it costs least walking;
    /// a regroup where a wing's way out comes down, none back toward its first fight; what is down left out.
    /// </summary>
    public class DungeonPlanTests
    {
        readonly List<ZoneCamp> camps = new List<ZoneCamp>(); readonly List<Vector3> at = new List<Vector3>();
        readonly HashSet<int> down = new HashSet<int>(), wanted = new HashSet<int>();
        readonly Dictionary<string, Vector3> anchors = new Dictionary<string, Vector3>();
        [SetUp] public void Clear() { camps.Clear(); at.Clear(); down.Clear(); wanted.Clear(); anchors.Clear(); }

        int Camp(string name, string wing, int step, Vector3 where, bool elite = false, bool optional = false, string after = null)
        {
            camps.Add(new ZoneCamp { name = name, mob = name, wing = wing, step = step, elite = elite, optional = optional, after = after, levelMin = 10, levelMax = 10, count = 1 });
            at.Add(where); return camps.Count - 1;
        }
        static ZoneWing Wing(string id, string after = null, string group = null, bool optional = false, string exit = null, string home = null)
        { return new ZoneWing { id = id, name = "The " + id, after = after, group = group, optional = optional, exit = exit, home = home }; }
        List<RouteStep> Plan(ZoneWing[] wings, DungeonPlan.Mode mode, Vector3 from, List<string> order)
        {
            var zone = new ZoneDefinition { id = "zone.test", wings = wings, camps = camps.ToArray() };
            return DungeonPlan.Plan(zone, mode, from, c => at[c], n => anchors.TryGetValue(n, out var p) ? p : (Vector3?)null, Vector3.Distance, c => !down.Contains(c), c => wanted.Contains(c), order);
        }
        static Vector3 X(float x, float y = 0, float z = 0) { return new Vector3(x, y, z); }

        /// <summary>A web of five wings and a fork: the start, then the nearest open wing each time; a fork's one side, and the wing after the
        /// fork only once a side and the others it waits for are done.</summary>
        ZoneWing[] Web()
        {
            Camp("a1", "a", 1, X(0)); Camp("a2", "a", 2, X(10));
            Camp("b1", "b", 1, X(20));
            Camp("c1", "c", 1, X(200));
            Camp("f1 guard", "f1", 1, X(30)); Camp("f1 boss", "f1", 1, X(31), elite: true);
            Camp("f2 boss", "f2", 1, X(60), elite: true);
            Camp("z1", "z", 1, X(300));
            return new[] { Wing("a"), Wing("b", "a"), Wing("c", "a"), Wing("f1", "b", "fork"), Wing("f2", "b", "fork"), Wing("z", "fork,c") };
        }
        [Test] public void The_nearest_open_wing_goes_next_and_one_side_of_a_fork_is_enough()
        {
            var order = new List<string>(); var steps = Plan(Web(), DungeonPlan.Mode.Quick, X(-10), order);
            CollectionAssert.AreEqual(new[] { "The a", "The b", "The f1", "The c", "The z" }, order, "a; then b (nearer than c); then the fork's nearer side; then c; z only after the fork and c");
            CollectionAssert.AreEqual(new[] { "a1", "a2", "b1", "f1 boss", "c1", "z1" }, steps.Select(s => s.name).ToArray(), "each wing's fights in its order");
            var fork = steps.First(s => s.wing == "f1");
            Assert.AreEqual(2, fork.camps.Length, "the boss and his guard are one fight"); Assert.AreEqual("f1 boss", camps[fork.Camp].name, "led by the boss"); Assert.IsTrue(fork.boss);
            Assert.IsFalse(steps.Exists(s => s.wing == "f2"), "the other side of the fork is left");
        }
        [Test] public void The_full_run_takes_both_sides_of_the_fork_one_after_the_other()
        {
            var order = new List<string>(); Plan(Web(), DungeonPlan.Mode.Full, X(-10), order);
            CollectionAssert.AreEqual(new[] { "The a", "The b", "The f1", "The f2", "The c", "The z" }, order);
        }
        [Test] public void A_side_of_the_fork_a_live_quest_wants_decides_it()
        {
            var web = Web(); wanted.Add(camps.FindIndex(c => c.name == "f2 boss"));
            var order = new List<string>(); Plan(web, DungeonPlan.Mode.Quick, X(-10), order);
            CollectionAssert.AreEqual(new[] { "The a", "The b", "The f2", "The c", "The z" }, order, "the farther side, for the quest");
        }
        [Test] public void The_run_keeps_to_the_height_it_is_at()
        {
            Camp("start", "s", 1, X(0, 12));
            Camp("level", "level", 1, X(40, 12));   // across the bridge, at the same height
            Camp("below", "below", 1, X(-25, 0));   // a little nearer, but 12 m down the trestle
            var order = new List<string>(); Plan(new[] { Wing("s"), Wing("level", "s"), Wing("below", "s") }, DungeonPlan.Mode.Quick, X(-5, 12), order);
            CollectionAssert.AreEqual(new[] { "The s", "The level", "The below" }, order, "enter high, work down");
        }
        /// <summary>A wing of three fights with a side boss off its middle, a side camp, and a side room (an optional wing) off its start.</summary>
        ZoneWing[] WithSides()
        {
            Camp("w1", "w", 1, X(0)); Camp("w2", "w", 2, X(100)); Camp("w3", "w", 3, X(200));
            Camp("side boss", "w", 0, X(105, 0, 15), elite: true, optional: true);
            Camp("side camp", "w", 0, X(150, 0, 10), optional: true);
            Camp("room guard", "room", 0, X(45, 0, 30), optional: true); Camp("room boss", "room", 0, X(50, 0, 30), elite: true, optional: true);
            Camp("released", "w", 0, X(5), after: "kill:w1");
            return new[] { Wing("w"), Wing("room", "w", optional: true) };
        }
        [Test] public void Quick_standard_and_full_runs_take_more_and_fit_each_side_fight_in_where_it_costs_least()
        {
            var names = Plan(WithSides(), DungeonPlan.Mode.Quick, X(-10), null).Select(s => s.name).ToArray();
            CollectionAssert.AreEqual(new[] { "w1", "w2", "w3" }, names, "the quick run: what must be done");
            names = Plan(WithSides(), DungeonPlan.Mode.Standard, X(-10), null).Select(s => s.name).ToArray();
            CollectionAssert.AreEqual(new[] { "w1", "w2", "side boss", "w3" }, names, "the standard run: the side boss as it passes him");
            var steps = Plan(WithSides(), DungeonPlan.Mode.Full, X(-10), null);
            CollectionAssert.AreEqual(new[] { "w1", "room boss", "w2", "side boss", "side camp", "w3" }, steps.Select(s => s.name).ToArray(), "the full run: the side room off the start as it passes, everything else in its place");
            var room = steps.First(s => s.name == "room boss");
            Assert.AreEqual(2, room.camps.Length, "the room's boss and his guard, one fight"); Assert.IsTrue(room.optional); Assert.AreEqual("room", room.wing, "a side room keeps its own wing's name");
            Assert.IsFalse(steps.Exists(s => s.camps.Any(c => camps[c].name == "released")), "a camp released later is never a step");
        }
        [Test] public void A_regroup_where_a_wings_way_out_comes_down_and_none_back_toward_its_start()
        {
            Camp("d1", "drop", 1, X(0)); Camp("d2", "drop", 2, X(40)); anchors["pool"] = X(10, -8);   // a drop back down near the start: still the regroup
            Camp("b1", "back", 1, X(60)); Camp("b2", "back", 2, X(90)); anchors["landing"] = X(60);   // a walk home that is back at its first fight: none
            Camp("o1", "on", 1, X(120)); Camp("o2", "on", 2, X(130)); anchors["foot"] = X(170);   // a walk home beyond its last fight: the regroup
            var wings = new[] { Wing("drop", exit: "drop", home: "pool"), Wing("back", "drop", exit: "walk", home: "landing"), Wing("on", "back", exit: "walk", home: "foot") };
            var steps = Plan(wings, DungeonPlan.Mode.Quick, X(-10), null);
            CollectionAssert.AreEqual(new[] { "d1", "d2", "pool", "b1", "b2", "o1", "o2", "foot" }, steps.Select(s => s.name).ToArray());
            Assert.IsTrue(steps[2].regroup && steps[7].regroup); Assert.AreEqual(0, steps[2].camps.Length, "a regroup is no fight");
            Assert.AreEqual(X(10, -8), steps[2].at, "at its home");
        }
        [Test] public void What_is_down_is_left_out_and_a_wing_with_nothing_standing_gives_nothing()
        {
            Camp("a1", "a", 1, X(0)); int a2 = Camp("a2", "a", 2, X(10));
            int g1 = Camp("gone1", "gone", 1, X(20)); int g2 = Camp("gone2", "gone", 2, X(30)); anchors["gone home"] = X(40);
            Camp("last", "last", 1, X(50));
            down.Add(a2); down.Add(g1); down.Add(g2);
            var order = new List<string>();
            var steps = Plan(new[] { Wing("a"), Wing("gone", "a", exit: "drop", home: "gone home"), Wing("last", "gone") }, DungeonPlan.Mode.Quick, X(-10), order);
            CollectionAssert.AreEqual(new[] { "a1", "last" }, steps.Select(s => s.name).ToArray(), "a cleared camp is no step, and an emptied wing has no regroup");
            CollectionAssert.AreEqual(new[] { "The a", "The last" }, order, "nor a place in the order");
        }
        [Test] public void Modes_read_from_words()
        {
            Assert.AreEqual(DungeonPlan.Mode.Quick, DungeonPlan.ParseMode("quick"));
            Assert.AreEqual(DungeonPlan.Mode.Full, DungeonPlan.ParseMode(" Full "));
            Assert.AreEqual(DungeonPlan.Mode.Standard, DungeonPlan.ParseMode(""));
            Assert.AreEqual(DungeonPlan.Mode.Standard, DungeonPlan.ParseMode(null));
        }
    }
}
