using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// Where a stuck sim pet is put beside its quarry (RangerPet.WarpSpot, 2026-10-18): the 2026-10-17 fix put the Bonecaller's thrall
    /// 1.4 m off the dummy on the side it came from, about 1 m from the Scoundrel there, and the two shoved the Scoundrel out of its reach
    /// (60.7 a second against its 64.3-68.2). Now on a ring round the quarry, clear of everyone, the far side from the party first.
    /// </summary>
    public class PetWarpSpotTests
    {
        static Vector3? Flat(Vector3 p) { return new Vector3(p.x, 0, p.z); }

        /// <summary>The SimDummyTests layout: the Mage 10 m back, the Scoundrel 1.6 m off at 40 degrees, the Archivist 10 m off at 80, you 14 m back.</summary>
        static List<Vector3> DummyParty(Vector3 dummy)
        {
            return new List<Vector3>
            {
                dummy + Vector3.back * 10,
                dummy + Quaternion.Euler(0, 40, 0) * Vector3.back * 1.6f,
                dummy + Quaternion.Euler(0, 80, 0) * Vector3.back * 10,
                dummy + Vector3.back * 14,
            };
        }

        [Test] public void The_spot_is_clear_of_everyone_and_within_a_bite()
        {
            var dummy = Vector3.zero; var party = DummyParty(dummy);
            Assert.IsTrue(RangerPet.WarpSpot(dummy, party, Flat, out var spot));
            float off = Vector3.Distance(spot, dummy);
            Assert.That(off, Is.InRange(RangerPet.WarpRingNear - .01f, RangerPet.WarpRingFar + .01f), "on the ring");
            Assert.Less(off, RangerPet.BiteReach, "within a bite");
            foreach (var p in party) Assert.GreaterOrEqual(Vector3.Distance(spot, p), RangerPet.WarpClear, "clear of " + p);
        }

        [Test] public void The_far_side_from_the_party_first()
        {
            var dummy = Vector3.zero; var party = DummyParty(dummy);
            Assert.IsTrue(RangerPet.WarpSpot(dummy, party, Flat, out var spot));
            var middle = Vector3.zero; foreach (var p in party) middle += p; middle /= party.Count;
            Assert.Greater(Vector3.Dot((spot - dummy).normalized, (dummy - middle).normalized), .5f, "away from the party");
        }

        [Test] public void Off_the_floor_and_crowded_spots_are_skipped()
        {
            var dummy = Vector3.zero;
            // Only the north half is floor.
            Vector3? north(Vector3 p) { return p.z > .1f ? Flat(p) : null; }
            Assert.IsTrue(RangerPet.WarpSpot(dummy, new List<Vector3>(), north, out var spot));
            Assert.Greater(spot.z, .1f, "on the floor");
            // Someone at every quarter on the ring's middle, and the floor only there: nowhere.
            var ring = new List<Vector3>();
            for (int i = 0; i < 12; i++) ring.Add(Quaternion.Euler(0, i * 30, 0) * Vector3.forward * 1.4f);
            Assert.IsFalse(RangerPet.WarpSpot(dummy, ring, Flat, out _), "every spot crowded");
        }
    }
}
