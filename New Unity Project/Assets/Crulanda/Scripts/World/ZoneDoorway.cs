using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The way through an inn's door, and who is on it (Phase 8.6, 2026-10-15; Chris, note 205: "running into each other and sometimes
    /// get stuck or delayed at door of inn"). Two lanes run straight through the double door, an outside point and an inside point
    /// 1.2 m apart on each: by day folk come in on the right-hand lane (seen from outside) and go out on the left; after the
    /// innkeeper's bedtime one leaf is shut and the one lane serves both ways. Whoever is on a lane goes first; the rest wait at
    /// their place in line a metre and more off it (the coop's RampQueue and QueueSpot, the hens' line, made for people), the most
    /// pressing first: a carrier with a load and the innkeeper, then villagers, then sims, then idlers. Plain logic with the time
    /// passed in, so the EditMode tests drive it without a scene. GAME-ONLY.
    /// </summary>
    public sealed class ZoneDoorway
    {
        /// <summary>Half the lane's length (the outside and inside points 1.2 m apart) and each lane's line off the door's middle.</summary>
        public const float Reach = .6f, LaneOff = .45f;
        /// <summary>How far off the lane's end the first in line waits, and the step back to each one after.</summary>
        public const float WaitOff = 1f, WaitStep = .75f;
        /// <summary>A lane is let go when its holder has not been heard from for this long (gone in, gone home, gone), or has held it this
        /// long (stuck on it): nobody waits on a lane that nobody is walking.</summary>
        public const float Silence = 1.5f, HoldLimit = 8f;
        /// <summary>Who is most pressed (lowest first).</summary>
        public const int Carrier = 0, Villager = 1, Sim = 2, Idler = 3;
        /// <summary>Waiting moves one up the order every this many seconds, so nobody waits for ever behind a stream of the more pressed
        /// (a sim behind the whole village coming in for supper).</summary>
        public const float AgeStep = 2;

        public string name;
        /// <summary>The door line's middle at its sill, the flat way in (outside to inside) and the flat way along the door (local +x:
        /// right, seen from outside).</summary>
        public Vector3 middle, inward, across;
        /// <summary>The opening (jamb to jamb) and the navmesh's walkable width through it (ZoneBuilder.NavGap), metres.</summary>
        public float width, walkable;
        /// <summary>One leaf shut (ZoneDoor.SetHalfShut): one lane, both ways.</summary>
        public bool Single;

        sealed class Waiter { public object who; public int rank; public bool goingIn; public float since, seen; }
        sealed class Lane { public object holder; public bool goingIn; public float since, seen; }
        readonly Lane[] lanes = { new Lane(), new Lane() };
        readonly List<Waiter> line = new List<Waiter>();

        int LaneOf(bool goingIn) { return Single || goingIn ? 0 : 1; }
        /// <summary>Which side of the middle a lane runs (+1 right, -1 left, seen from outside): in on the right, out on the left, and
        /// the open (right) leaf's alone at night.</summary>
        public float Side(bool goingIn) { return LaneOf(goingIn) == 0 ? 1 : -1; }
        Vector3 Line(bool goingIn) { return middle + across * Side(goingIn) * LaneOff; }
        /// <summary>The outside and the inside point of the middle line (the door's own "outside" and "inside").</summary>
        public Vector3 Outside { get { return middle - inward * Reach; } }
        public Vector3 Inside { get { return middle + inward * Reach; } }
        /// <summary>Where someone going this way steps on to the lane, and where they step off it.</summary>
        public Vector3 Entry(bool goingIn) { return Line(goingIn) + inward * (goingIn ? -Reach : Reach); }
        public Vector3 Exit(bool goingIn) { return Line(goingIn) + inward * (goingIn ? Reach : -Reach); }
        /// <summary>Where the <paramref name="place"/>th in line (0: the next) waits to go this way: back from the lane's end, the way they
        /// came, and a little to its own side, so nobody stepping off the other lane walks into the line.</summary>
        public Vector3 QueueSpot(bool goingIn, int place)
        {
            var back = goingIn ? -inward : inward;
            return Entry(goingIn) + back * (WaitOff + WaitStep * Mathf.Max(0, place)) + across * Side(goingIn) * .6f;
        }

        /// <summary>
        /// Asks for the lane going this way: true when <paramref name="who"/> has it (it is free, they are first in line by rank and by how
        /// long they have waited, and <paramref name="ready"/>: at the lane's end, so the lane is held only for the 1.2 m across), or
        /// already held it (heard from again). Otherwise they are in line, <paramref name="place"/>th.
        /// </summary>
        public bool Request(object who, int rank, bool goingIn, float now, out int place, bool ready = true)
        {
            place = 0; if (who == null) return false;
            Prune(now);
            var lane = lanes[LaneOf(goingIn)];
            if (lane.holder == who) { lane.seen = now; return true; }
            foreach (var l in lanes) if (l.holder == who) l.holder = null;   // on the other lane: done with it
            var w = line.Find(x => x.who == who);
            if (w == null) { w = new Waiter { who = who, since = now, goingIn = goingIn }; line.Add(w); }
            if (w.goingIn != goingIn) w.since = now;   // turned round: back of the line
            w.rank = rank; w.goingIn = goingIn; w.seen = now;
            var mine = Waiting(goingIn, now);
            if (lane.holder == null && ready && mine.Count > 0 && mine[0] == w)
            {
                lane.holder = who; lane.goingIn = goingIn; lane.since = lane.seen = now; line.Remove(w);
                return true;
            }
            place = Mathf.Max(0, mine.IndexOf(w));
            return false;
        }
        /// <summary>Off the lane (or out of the line): the next may go.</summary>
        public void Release(object who)
        {
            foreach (var l in lanes) if (l.holder == who) l.holder = null;
            line.RemoveAll(x => x.who == who);
        }
        /// <summary>Who has the lane going this way now (null: free).</summary>
        public object Holder(bool goingIn) { return lanes[LaneOf(goingIn)].holder; }
        /// <summary>How many wait for the lane going this way.</summary>
        public int InLine(bool goingIn) { return line.FindAll(x => LaneOf(x.goingIn) == LaneOf(goingIn)).Count; }
        /// <summary>The line for the lane going this way (at night, both ways share it): the most pressed first, each waiter a place up for
        /// every <see cref="AgeStep"/> seconds waited, then first come.</summary>
        List<Waiter> Waiting(bool goingIn, float now)
        {
            int lane = LaneOf(goingIn);
            var list = line.FindAll(x => LaneOf(x.goingIn) == lane);
            float Key(Waiter x) { return x.rank - (now - x.since) / AgeStep; }
            list.Sort((a, b) => Key(a) != Key(b) ? Key(a).CompareTo(Key(b)) : a.since.CompareTo(b.since));
            return list;
        }
        void Prune(float now)
        {
            foreach (var l in lanes) if (l.holder != null && (now - l.seen > Silence || now - l.since > HoldLimit)) l.holder = null;
            line.RemoveAll(x => now - x.seen > Silence);
        }
    }

    /// <summary>A seat at an inn: where a sitter stands (on the floor) and the point they face (the table, the bar, the fire).</summary>
    public struct InnSeat { public Vector3 at, face; }

    /// <summary>
    /// A walk-in inn as the builder made it (Phase 8.6, 2026-10-15): its door and the doorway lane, the room inside the walls, its
    /// seats at the tables (fifteen in a tavern), the stools at its bar, the bench by its hearth, and what kind of house it is. The
    /// village's life reads the room from here (VillageLife.FindPlaces), so the room and the life cannot drift apart again (the seat
    /// layout was written twice before: ZoneBuilder.Inn and WorldLife, "same layout as ZoneBuilder.Inn"). GAME-ONLY.
    /// </summary>
    public sealed class ZoneInn
    {
        /// <summary>"tavern" (a bar, a cask and an innkeeper: the Golden Cask, the Cracked Hearth), "city" (the same, in white stone round a
        /// galleried taproom: the Crown's Gilded Lantern, inn variant 3), "waystation" (one storey, a long table
        /// and bunks: Pilgrims' Rest, Wain's Rest) or "hearth" (the Keepers' hearth-house in the Hearth-Tree).</summary>
        public string name, style;
        public ZoneDoor door;
        /// <summary>The room's own frame (its middle, turned with it) and its half-size inside the walls; a round room's radius is half.x.</summary>
        public Transform frame; public Vector2 half; public bool round;
        public readonly List<InnSeat> seats = new List<InnSeat>(), bar = new List<InnSeat>(), bench = new List<InnSeat>();
        /// <summary>Where the house's lodgers sleep (a waystation's bunks), or null.</summary>
        public ZoneDoor bunks;
        /// <summary>A bar, a cask and someone to keep them: the inn's news, its ale and its drinkers.</summary>
        public bool Staffed { get { return style == "tavern" || style == "city"; } }
        /// <summary>Whether a point stands inside the walls (<paramref name="margin"/> metres in from them).</summary>
        public bool Contains(Vector3 world, float margin = 0)
        {
            if (frame == null) return false;
            var p = frame.InverseTransformPoint(world);
            if (round) return p.x * p.x + p.z * p.z < (half.x - margin) * (half.x - margin);
            return Mathf.Abs(p.x) < half.x - margin && Mathf.Abs(p.z) < half.y - margin;
        }
    }
}
