using UnityEngine;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Dead Man's Drop and the Night Market in play (Docs/ZONE_LOWTOWNS.md "Dead Man's Drop and the Night Market"; Lowtowns picks 10 and 8;
    /// the builders are ZoneBuilder.DeadMansDrop and ZoneBuilder.NightMarket).
    /// - The Drop: step off the lip where the lane ends and you fall onto the fungal net three stories down (book1 ch.1:347): a little of
    ///   your health is taken (<see cref="DropBite"/>, never the last of it) and the book's line is said. On the net, E at the iron rungs
    ///   climbs out to Cooper's Row (ZoneBuilder.DropOut), the party with you. Anyone who somehow goes past the net is set back on it.
    /// - The Night Market: its keepers (ZoneBuilder.NightFolk) are out after dark (WorldClock.IsNight) and gone by day; their stalls'
    ///   night and day looks switch themselves (NightOnly, LightButterflies).
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>What a fall onto the net takes: this share of your health, never the last of it (GAME-ONLY).</summary>
        public const float DropShare = .08f;
        /// <summary>How long climbing the rungs takes, in seconds.</summary>
        public const float RungsSeconds = 4;
        public const string DropLandLine = "The wind shrieks past your ears as the walls of the drop rush by; then you crash heavily into a thick, rotting fungal-net suspended three stories down. You lie tangled in the damp webbing, ribs aching. Alive. Miraculously, absurdly alive.";
        public const string RungsLine = "You haul yourself up the rusted rungs to a drain-cut in the tower's wall and crawl along it in the dark, until it lets you out, filthy, onto Cooper's Row under the market.";

        /// <summary>The health a fall onto the net takes from a pool of <paramref name="max"/> with <paramref name="current"/> left: a share of
        /// the most, at least one, and never the last point.</summary>
        public static int DropBite(int current, int max)
        {
            int bite = Mathf.Max(1, Mathf.RoundToInt(max * DropShare));
            return Mathf.Clamp(bite, 0, Mathf.Max(0, current - 1));
        }

        bool dropFalling;
        /// <summary>Every frame in the Lowtowns: the fall, the landing, the rungs offered only on the net.</summary>
        void TickDrop()
        {
            var d = Zone != null ? Zone.LowtownDrop : null; if (d == null || Player == null) return;
            var p = Player.transform.position;
            bool over = d.Over(p);
            // The rungs are reached from the net, never from the lip over them.
            if (d.rungs != null) d.rungs.hiddenUntil = over && p.y < d.net + 3 ? 0 : Time.time + 1;
            if (!over) { dropFalling = false; return; }
            if (p.y < d.lip - 1.5f && p.y > d.net + 2.5f) dropFalling = true;
            var cc = Player.GetComponent<CharacterController>();
            if (dropFalling && cc != null && cc.isGrounded && p.y < d.net + 3)
            {
                dropFalling = false;
                if (Player.IsAlive)
                {
                    var hp = Player.Health; int bite = DropBite(Mathf.RoundToInt(hp.Pool.Current), Mathf.RoundToInt(hp.Pool.Max));
                    if (bite > 0) hp.ApplyDamage(bite);
                    FloatText(p, "Caught in the net", new Color(.6f, .75f, .55f));
                }
                Message(DropLandLine);
            }
            // Past the net (a gap in the weave, a slip down a side): set back on it.
            if (p.y < d.net - 3)
            {
                dropFalling = false;
                Player.GetComponent<AdventurerMotor>().Teleport(d.centre + Vector3.up * (d.net - d.lip + 1.2f));
                Message("Something gives under you and the dark takes you a moment; then your hands find cords, and you haul yourself back up onto the net.");
            }
        }

        /// <summary>E on the Drop's things (the rungs). True: handled.</summary>
        bool UseDropThing(ZoneInteractable i)
        {
            if (i == null || i.kind != "rungs") return false;
            if (InCombat) { Message(FightingLine); return true; }
            Message("You take hold of the first rung. It is cold, and flakes rust into your palm.");
            StartWork("Climbing the iron rungs", RungsSeconds, ClimbOutOfTheDrop);
            return true;
        }
        void ClimbOutOfTheDrop()
        {
            var at = Zone.Ground(ZoneBuilder.DropOut) + Vector3.up * .1f;
            Player.GetComponent<AdventurerMotor>().Teleport(at);
            if (Companion != null) { var ma = Companion.GetComponent<NavMeshAgent>(); if (ma != null && ma.isOnNavMesh) ma.Warp(at + Vector3.right * 2.5f - Vector3.up * .1f); }
            foreach (var c in PartySims) { if (c == null) continue; var agent = c.GetComponent<NavMeshAgent>(); if (agent != null && agent.isOnNavMesh) agent.Warp(at + Vector3.left * (2 + c.slot) - Vector3.up * .1f); }
            dropFalling = false;
            Message(RungsLine);
            Save(false);
        }

        float nightMarketAt; int nightMarketOpen = -1;
        /// <summary>Every two seconds: the Night Market's keepers out after dark and gone by day.</summary>
        void TickNightMarket()
        {
            if (Time.time < nightMarketAt || Zone == null) return; nightMarketAt = Time.time + 2;
            var life = VillageLife.Active; if (life == null || Zone.Zone.life == null || Zone.Zone.life.residents == null) return;
            int open = WorldClock.IsNight ? 1 : 0; if (open == nightMarketOpen) return;
            bool first = nightMarketOpen < 0; int found = 0;
            foreach (var r in Zone.Zone.life.residents)
            {
                if (r == null || System.Array.IndexOf(ZoneBuilder.NightFolk, r.name) < 0) continue;
                var v = life.Find(r.name); if (v == null) continue;
                found++;
                if (open == 0) v.Park();
                // Already standing at their posts when the zone loads after dark.
                else if (!first) v.Release(Zone.Ground(r.at));
            }
            // Not this zone's (none of them here), or the townsfolk not yet out: look again next time.
            if (found > 0) nightMarketOpen = open;
        }
    }
}
