using UnityEngine;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Lowtowns' turf and crossings in play (Lowtowns pick 12; Docs/ZONE_LOWTOWNS.md "Landmarks, turf and crossings"; the builder is
    /// ZoneBuilder.LowtownMarks).
    /// - The turf by the day (GAME-ONLY, seeded by book1 ch.7:21): the Iron Teeth's two runners stand at the corner they hold today
    ///   (ZoneBuilder.TurfCornerOn of the game's day count) and the other corners' are gone; the marks switch themselves (TurfMarks).
    ///   Never at the warehouse, which is neutral ground.
    /// - The punt ferry (GAME-ONLY): E at a landing, and you are poled across the river to the other one (<see cref="PuntSeconds"/>), the
    ///   party with you. A pole and a flat boat; nothing else.
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>How long the crossing takes, in seconds.</summary>
        public const float PuntSeconds = 5;
        public const string PuntLine = "A dock-rat with a face like a wet boot takes your copper without a word, leans on the pole, and the punt slides out over the black water. It barely ripples. Nobody trails a hand.";
        public const string PuntLandLine = "The punt noses into the far landing. You step off on to timber that gives a little, and the dock-rat is already poling back.";

        float turfAt; int turfShown = -1;
        /// <summary>Every two seconds: the Iron Teeth's runners at today's corner, the others gone.</summary>
        void TickTurf()
        {
            if (Time.time < turfAt || Zone == null) return; turfAt = Time.time + 2;
            var life = VillageLife.Active; if (life == null || Zone.Zone.life == null || Zone.Zone.life.residents == null) return;
            int today = ZoneBuilder.TurfCornerOn(Progress.days); if (today == turfShown) return;
            bool first = turfShown < 0; int found = 0;
            foreach (var r in Zone.Zone.life.residents)
            {
                if (r == null) continue;
                int corner = ZoneBuilder.TurfCornerOf(r.name); if (corner < 0) continue;
                var v = life.Find(r.name); if (v == null) continue;
                found++;
                if (corner != today) v.Park();
                // Already standing at their corner when the zone loads.
                else if (!first) v.Release(Zone.Ground(r.at));
            }
            // Not this zone's, or the townsfolk not yet out: look again next time.
            if (found > 0) turfShown = today;
        }

        /// <summary>E at a punt landing. True: handled.</summary>
        bool UsePuntThing(ZoneInteractable i)
        {
            if (i == null || i.kind != "punt") return false;
            if (InCombat) { Message(FightingLine); return true; }
            var p = new Vector2(i.position.x, i.position.z);
            bool fromWest = Vector2.Distance(p, ZoneBuilder.PuntWest) < Vector2.Distance(p, ZoneBuilder.PuntEast);
            Message(PuntLine);
            var punt = Zone != null ? Zone.LowtownPunt : null;
            if (punt != null) punt.Pole(!fromWest, PuntSeconds);
            StartWork("Poled across the river", PuntSeconds, () => CrossByPunt(fromWest));
            return true;
        }
        void CrossByPunt(bool fromWest)
        {
            var at = Zone.Ground(fromWest ? ZoneBuilder.PuntEast : ZoneBuilder.PuntWest) + Vector3.up * .1f;
            Player.GetComponent<AdventurerMotor>().Teleport(at);
            if (Companion != null) { var ma = Companion.GetComponent<NavMeshAgent>(); if (ma != null && ma.isOnNavMesh) ma.Warp(at + Vector3.forward * 2.5f - Vector3.up * .1f); }
            foreach (var c in PartySims) { if (c == null) continue; var agent = c.GetComponent<NavMeshAgent>(); if (agent != null && agent.isOnNavMesh) agent.Warp(at + Vector3.back * (2 + c.slot) - Vector3.up * .1f); }
            Message(PuntLandLine);
            Save(false);
        }
    }
}
