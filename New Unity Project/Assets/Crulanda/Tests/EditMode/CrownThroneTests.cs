using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The High Councilor on his throne, as data (2026-10-16, Chris: "he needs more regal attire and sitting on a throne";
    /// Docs/CAPITAL_HUB.md): his post is the throne's seat on the keep's dais, kept seated and facing the hall; he wears the
    /// "councilor" robes of state and his clerk the "scholar" robe; the foot of the dais is in talk reach of the seat. The built and
    /// walked side (the floors, the dais, him sat on it) is CrownThronePlayTests.
    /// </summary>
    public class CrownThroneTests
    {
        static ZoneDefinition crown;
        [OneTimeSetUp] public void Load() { crown = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones", "crown.json"))); }
        static ZoneResident Person(string name) { return crown.life.residents.FirstOrDefault(r => r != null && r.name == name); }

        [Test] public void Aethelgard_keeps_a_seated_post_on_the_throne_facing_the_hall()
        {
            var keep = crown.props.First(p => p != null && p.kind == "council_keep"); var size = ZoneBuilder.CitySize(keep);
            var him = Person("High Councilor Aethelgard"); Assert.IsNotNull(him);
            Assert.IsTrue(him.seated, "He sits, never stands."); Assert.IsFalse(him.works, "He keeps his post.");
            var seat = ZoneBuilder.ThroneSeat(size.y); var q = Quaternion.Euler(0, keep.rotation, 0) * seat;
            Assert.Less(Vector2.Distance(him.at, keep.at + new Vector2(q.x, q.z)), .05f, "His post is the throne's seat.");
            Assert.That(Mathf.DeltaAngle(him.facing, keep.rotation + 180), Is.EqualTo(0).Within(1f), "Facing the hall and its door (the keep's -Z).");
            // The seat at a stool's height over the dais (the inn's sitting), the dais a few low steps.
            Assert.That(ZoneBuilder.ThroneSeatHeight, Is.InRange(.42f, .5f));
            Assert.That(ZoneBuilder.DaisRise, Is.LessThanOrEqualTo(.25f), "Steps you walk up (the step offset is .35 m).");
            // From the foot of the dais (0.45 m before its first step, on the floor) to him on his seat: within talk range.
            var you = new Vector3(0, .05f + 1, ZoneBuilder.DaisFront(size.y) - .45f); var at = seat + Vector3.up;
            Assert.Less(Vector3.Distance(you, at), EncounterSession.TalkRange - .1f, "Spoken to from the foot of the dais.");
        }

        [Test] public void Aethelgard_wears_robes_of_state_and_his_clerk_a_scholars_robe()
        {
            var him = Person("High Councilor Aethelgard"); Assert.AreEqual("councilor", him.look, "His robes of state (ActorVisual.Dress).");
            StringAssert.StartsWith("CANON", him.canonStatus); StringAssert.Contains("b1 ch.12:365", him.canonStatus, "His look's source is cited.");
            StringAssert.Contains("GAME-ONLY", him.canonStatus, "The gold, the collar, the chain and the rings are labelled the game's own.");
            Assert.IsTrue(ActorVisual.HasOutfit("councilor") && ActorVisual.HasOutfit("scholar"), "Both are costumes.");
            var clerk = Person("Clerk Odo Vantry"); Assert.IsNotNull(clerk); Assert.AreEqual("scholar", clerk.look); Assert.IsFalse(clerk.seated, "The clerk stands.");
            Assert.Less(Vector2.Distance(clerk.at, him.at), 4, "At his side.");
        }
    }
}
