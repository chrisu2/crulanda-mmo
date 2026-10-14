using System.Collections.Generic;
using Crulanda.Abilities;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Which of Kevin Iglesias's motions a blow or a spell plays (2026-10-14, Chris: "we need more spell animations and melee animations
    /// also"; his Human Melee, Spellcasting and Throwing Animations FREE, ModelFigure.KiActs). A blow follows what is in the hands: a
    /// two-handed weapon or a staff swings two-handed, a polearm thrusts, two weapons swing right and left in turn, one hand swings
    /// from the right and now and then overhead; a shield bashes for a shield's blow. A spell follows what it does: a bolt from the
    /// right hand, a mend or a hex from the left, a long cast from both hands, a thing called down or raised (a thrall, hail, a cairn)
    /// with the hand raised, all round for what works round the caster (a shout, a song, a ward on the ground); a knife, a flask, a
    /// net, a handful of salt or coin thrown. The table names the ones the rule would get wrong; a bow still shoots.
    /// </summary>
    public sealed partial class ActorVisual
    {
        readonly string[] handFamily = new string[2];
        /// <summary>The look family of what is in a hand ("sword.great", "shield.kite"; null: nothing equipped there).</summary>
        string HandFamily(int hand) { return hand >= 0 && hand < handFamily.Length ? handFamily[hand] : null; }

        static readonly Dictionary<string, string> MotionOf = new Dictionary<string, string>
        {
            // heavy blows and finishers
            { "ability.breaching_blow", "heavy" }, { "paladin.smite", "heavy" }, { "paladin.censure", "heavy" }, { "rogue.eviscerate", "heavy" },
            { "scoundrel.settle_score", "heavy" }, { "ranger.whirling_spear", "heavy" }, { "druid.rending_flurry", "heavy" },
            // a shield's blow
            { "ability.shield_bash", "bash" }, { "paladin.rebuke", "bash" },
            // thrown
            { "rogue.throw_knife", "throw" }, { "warrior.net", "throw" }, { "warrior.shackle", "throw" }, { "scoundrel.poisoned_wine", "throwball" },
            { "scoundrel.bribe", "throwball" }, { "salt.binding", "throwball" }, { "archivist.echo_jar", "throwball" }, { "rogue.blind", "throwball" },
            // called down or raised
            { "mage.raise_thrall", "call" }, { "ranger.call_companion", "call" }, { "shaman.barrow_call", "call" }, { "scoundrel.favour", "call" },
            { "shaman.cairn", "call" }, { "salt.cairn", "call" }, { "druid.seedling", "call" }, { "mage.bone_spikes", "call" }, { "shaman.hailfall", "call" },
            { "shaman.thunderhead", "call" }, { "mage.cinder_field", "call" }, { "druid.bramblestorm", "call" }, { "cleric.sunburst", "call" },
            { "ranger.snare_trap", "call" },
            // all round the caster
            { "paladin.consecrate", "omni" }, { "shaman.moor_wind", "omni" }, { "shaman.rooting_stamp", "omni" }, { "druid.bellowing_roar", "omni" },
            { "ability.shout", "omni" }, { "ability.muster", "omni" }, { "paladin.press_on", "omni" }, { "paladin.colours", "omni" }, { "paladin.aegis", "omni" },
            { "paladin.mending_oath", "omni" }, { "archivist.chorus", "omni" }, { "archivist.song_of_mending", "omni" }, { "archivist.cadence", "omni" },
            { "archivist.dirge", "omni" }, { "archivist.mending_verse", "omni" }, { "salt.circle", "omni" }, { "salt.sung_wall", "omni" }, { "salt.sifting", "omni" },
            { "salt.wound_seal", "omni" }, { "cleric.hymn", "omni" }, { "cleric.sanctuary", "omni" }, { "cleric.litany", "omni" }, { "cleric.steadfast", "omni" },
            { "shaman.rain_call", "omni" }, { "druid.burst_bloom", "omni" }, { "scoundrel.jagged_verse", "omni" }, { "scoundrel.war_verse", "omni" },
            { "scoundrel.rallying_verse", "omni" }, { "rogue.rallying_flourish", "omni" }, { "mage.ember_ward", "omni" }, { "shaman.earthskin", "omni" },
            // from both hands
            { "mage.marrow_drain", "bolt2" }, { "archivist.unwrite", "bolt2" }, { "archivist.erasure", "bolt2" }, { "mage.flare", "bolt2" },
        };
        /// <summary>The motion an ability plays: "heavy", "bash", "throw", "throwball", "throwspear", "boltR", "boltL", "bolt2", "call",
        /// "omni", or null (a blow: whatever the hands hold; a self-buff with no motion of its own: none).</summary>
        public static string MotionFor(AbilityDefinition a)
        {
            if (a == null) return null;
            if (!string.IsNullOrEmpty(a.id) && MotionOf.TryGetValue(a.id, out var m)) return m;
            if (a.range <= .5f) return null;
            if (a.range <= 5) return null;   // a blow in reach
            if (a.effect == AbilityEffect.Heal) return a.castTime >= 2 ? "bolt2" : "boltL";
            if (a.effect != AbilityEffect.Damage) return "boltL";
            return a.castTime >= 2 ? "bolt2" : "boltR";
        }
        /// <summary>True when an ability with no target of its own still shows its motion (what it does round the caster).</summary>
        public static bool ShowsUntargeted(AbilityDefinition a) { var m = MotionFor(a); return a != null && a.range <= .5f && (m == "omni" || m == "call"); }

        int kiBlow;
        /// <summary>The upper-body slot for a blow now (Strike), from what the hands hold and the ability's motion.</summary>
        string MeleeSlot(string motion)
        {
            if (motion != null && motion.StartsWith("throw") && model.HasAct(motion)) return motion;
            bool armed = held != null && held.Length > 0 && !gearStowed;
            if (!armed) return (punchLeft = !punchLeft) ? "jab" : "cross";
            if (!model.HasAct("slashR")) return "swing";
            string main = HandFamily(0), off = HandFamily(1);
            bool staffKit = main == null && (built == ActorLook.Druid || built == ActorLook.Mage || built == ActorLook.SaltMender);
            bool shieldKit = main == null && (built == ActorLook.Warrior || built == ActorLook.Paladin);
            bool shield = shieldKit || (off != null && (off.StartsWith("shield") || off == "model.shield"));
            bool pole = main != null && main.StartsWith("polearm");
            bool two = staffKit || (main != null && (main.StartsWith("sword.great") || main.StartsWith("hammer.war") || main.StartsWith("staff") || main.StartsWith("axe.crescent") || main == "model.tall"));
            bool dual = !shield && off != null && !off.StartsWith("offhand") && !off.StartsWith("shield");
            kiBlow++;
            if (motion == "bash" && shield && model.HasAct("bash")) return "bash";
            if (pole && model.HasAct("thrustP")) return kiBlow % 3 == 2 ? "cleave" : "thrustP";
            if (two) return "cleave";
            if (dual) return (punchLeft = !punchLeft) ? "slashR" : "slashL";
            if (motion == "heavy") return "swing";   // the overhead blow
            return kiBlow % 3 == 2 ? "swing" : "slashR";
        }
        bool releaseLeft;
        /// <summary>The release for a spell with no motion of its own: from the right hand and the left in turn.</summary>
        string DefaultRelease() { return (releaseLeft = !releaseLeft) ? "boltR" : "boltL"; }
    }
}
