using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The new motions and effects (2026-10-14, Chris: "we need more spell animations and melee animations also", "we need new
    /// spell effects also for ALL spells"): every ability's effects are in the build, every motion an ability plays has Kevin's clip
    /// for both bodies, and a bolt of any colour bursts with one of the pack's hits.</summary>
    public class SpellFxTests
    {
        static EncounterContent Content { get { return UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset"); } }

        [Test] public void Every_ability_has_its_effects_in_the_build()
        {
            var missing = new List<string>();
            foreach (var a in Content.abilities)
                foreach (var p in SpellFx.PathsFor(a))
                    if (Resources.Load<GameObject>(p) == null) missing.Add(a.id + ": " + p);
            Assert.IsEmpty(missing.Distinct().ToList());
            Assert.AreEqual(148, Content.abilities.Length, "the abilities counted when the effects were made (add new ones to SpellFx's table if they want a look of their own)");
        }

        static readonly Dictionary<string, string> Clip = new Dictionary<string, string>
        {
            { "boltR", "MagicAttackDirect1H01_R" }, { "boltL", "MagicAttackDirect1H01_L" }, { "bolt2", "MagicAttackDirect2H01" }, { "call", "MagicAttackCall1H01_L" },
            { "omni", "MagicAttackOmni01" }, { "throw", "ThrowWeapon01_R" }, { "throwball", "ThrowBall01_R" }, { "throwspear", "ThrowSpear01_R" },
            { "heavy", "Attack2H01" }, { "bash", "AttackShield01" },
        };
        static bool Has(string body, string file) { return Resources.LoadAll<AnimationClip>("Characters/Animations/KI/" + body + "/Human" + body[0] + "@" + file).Length > 0; }

        [Test] public void Every_motion_an_ability_plays_has_its_clip_for_both_bodies()
        {
            var missing = new List<string>();
            foreach (var a in Content.abilities)
            {
                var m = ActorVisual.MotionFor(a); if (m == null) continue;
                Assert.IsTrue(Clip.ContainsKey(m), a.id + " plays an unknown motion '" + m + "'");
                foreach (var body in new[] { "Male", "Female" }) if (!Has(body, Clip[m])) missing.Add(a.id + " " + body + " " + Clip[m]);
            }
            foreach (var file in new[] { "Attack1H01_R", "Attack1H01_L", "AttackPolearm01", "CastingEnter01", "CastingIdle01", "CombatDamage01", "FarmingWithPlow01_R - Loop", "Gathering01", "MiningOneHand01_R - Wall", "MiningOneHand01_R - Ground" })
                foreach (var body in new[] { "Male", "Female" }) if (!Has(body, file)) missing.Add(body + " " + file);
            Assert.IsEmpty(missing);
        }

        [Test] public void A_bolt_of_any_colour_bursts_with_a_hit_from_the_pack()
        {
            foreach (var c in new[] { new Color(1, .45f, .1f), new Color(.45f, 1, .6f), new Color(1, .9f, .4f), new Color(.4f, .6f, 1), new Color(.75f, .85f, 1), new Color(.7f, .3f, .9f), new Color(.9f, .88f, .8f), new Color(.5f, .45f, .4f) })
                Assert.IsNotNull(Resources.Load<GameObject>(SpellFx.HitForColour(c)), "a hit for " + c);
        }

        [Test] public void Abilities_get_the_motion_their_kind_wants()
        {
            var by = Content.abilities.ToDictionary(a => a.id);
            Assert.AreEqual("call", ActorVisual.MotionFor(by["mage.raise_thrall"]));
            Assert.AreEqual("omni", ActorVisual.MotionFor(by["cleric.sanctuary"]));
            Assert.AreEqual("throw", ActorVisual.MotionFor(by["rogue.throw_knife"]));
            Assert.AreEqual("bash", ActorVisual.MotionFor(by["ability.shield_bash"]));
            Assert.AreEqual("boltL", ActorVisual.MotionFor(by["cleric.mend"]), "a mend from the left hand");
            Assert.AreEqual("bolt2", ActorVisual.MotionFor(by["cleric.long_mend"]), "a long cast from both hands");
            Assert.AreEqual("boltR", ActorVisual.MotionFor(by["mage.ember_bolt"]));
            Assert.IsNull(ActorVisual.MotionFor(by["rogue.sinister_strike"]), "a blow: whatever the hands hold");
            Assert.AreEqual("fire", SpellFx.Element(by["mage.ember_bolt"])); Assert.AreEqual("bone", SpellFx.Element(by["mage.raise_thrall"]));
            Assert.AreEqual("frost", SpellFx.Element(by["shaman.hailfall"])); Assert.AreEqual("holy", SpellFx.Element(by["paladin.smite"]));
        }
    }
}
