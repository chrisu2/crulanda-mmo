using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// The chase asks for its path again only when it must (MobChase; 2026-10-18, hallows-8: asked afresh every frame, the way up the Bell
    /// Tower's flights was never worked out, and the leader stood 27 minutes at the stair's foot "closing on" the Pact-Captain).
    /// </summary>
    public class MobChaseTests
    {
        static readonly Vector3 Landing = new Vector3(1.6f, 10, 117.4f);
        [Test] public void Nothing_asked_yet_asks() { Assert.IsTrue(MobChase.Ask(false, false, Vector3.zero, Landing, -10, 5)); }
        [Test] public void A_quarry_standing_still_is_not_asked_again()
        {
            Assert.IsFalse(MobChase.Ask(true, false, Landing, Landing, 4.9f, 5), "a path to it already");
            Assert.IsFalse(MobChase.Ask(false, true, Landing, Landing, 4.9f, 5), "being worked out: left to finish");
            Assert.IsFalse(MobChase.Ask(true, false, Landing - Vector3.up * 1.1f, Landing, 0, 5), "a body a metre over the mesh is where it is");
        }
        [Test] public void A_quarry_that_moved_is_asked_again_but_a_pending_way_is_left_a_second()
        {
            var moved = Landing + new Vector3(2, 0, 0);
            Assert.IsTrue(MobChase.Ask(true, false, Landing, moved, 4.9f, 5), "moved two metres");
            Assert.IsTrue(MobChase.Ask(true, false, Landing - Vector3.up * 9, Landing, 4.9f, 5), "up on the landing, the path to the floor below");
            Assert.IsFalse(MobChase.Ask(true, true, Landing, moved, 4.5f, 5), "still being worked out half a second on");
            Assert.IsTrue(MobChase.Ask(true, true, Landing, moved, 3.5f, 5), "a second and a half on");
        }
        [Test] public void The_chases_use_it()
        {
            string dir = System.IO.Path.Combine(Application.dataPath, "Crulanda", "Scripts", "Encounter");
            StringAssert.Contains("MobChase.Ask(agent, Victim.transform.position", System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "EncounterEnemy.cs")));
            StringAssert.Contains("MobChase.Ask(agent, q", System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "SimCompanion.cs")));
        }
    }
}
