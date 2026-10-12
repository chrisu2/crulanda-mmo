using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>Step-4 review: the Unwriting / Still Vow / Marrow Drain rings sit round the target's body (they were metres off), and the
    /// pet frame names a thrall a thrall.</summary>
    public class AuraTests
    {
        [UnityTest] public IEnumerator An_aura_ring_sits_round_the_body_of_a_scaled_moving_actor()
        {
            var who = new GameObject("Target"); who.transform.position = new Vector3(5, 0, -3); who.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f); who.transform.rotation = Quaternion.Euler(0, 70, 0);
            var ring = ArchivistKit.Aura(who.transform, "Unwriting", Color.grey, 5, .55f, 1.4f, 8);
            yield return null;
            void Check(string when)
            {
                foreach (Transform m in ring.transform)
                {
                    var d = m.position - who.transform.position; float flat = new Vector2(d.x, d.z).magnitude;
                    Assert.That(flat, Is.InRange(.4f, .75f), when + ": a mote " + flat.ToString("0.00") + " m from the body's line");
                    Assert.That(d.y, Is.InRange(1.3f, 1.6f), when + ": a mote " + d.y.ToString("0.00") + " m up");
                    Assert.That(m.lossyScale.x, Is.LessThan(.2f), when + ": a mote's size");
                }
            }
            Check("cast");
            who.transform.position += new Vector3(4, 0, 2); yield return null; yield return null;
            Check("after the target moved");
            ArchivistKit.Aura(who.transform, "Unwriting", Color.grey, 5, .55f, 1.4f, 8); yield return null;
            Assert.AreEqual(1, AuraSpin.All.FindAll(a => a.Follow == who.transform && a.name == "Unwriting").Count, "recast replaces, not stacks");
            Object.Destroy(who); yield return null; yield return null;
            Assert.AreEqual(0, AuraSpin.All.FindAll(a => a.Follow == null).Count, "gone with its actor");
        }
        [Test] public void The_pet_frame_names_the_thrall() { Assert.AreEqual("Thrall", EncounterHud.PetLabel("thrall")); Assert.AreEqual("Wolf", EncounterHud.PetLabel("wolf")); }
    }
}