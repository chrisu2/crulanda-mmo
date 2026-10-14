using System;
using System.Collections.Generic;
using Crulanda.Abilities;
using Crulanda.Combat;
using Crulanda.Core;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Spell, area and melee effects for every ability (2026-10-14, Chris: "we need new spell effects also for ALL spells"; "i added
    /// some spell effects, aoe effects, melee effects, fire to myassets look these over to use"): Hovl Studio's Magic Effects FREE
    /// (Resources/Fx/Magic) and Matthew Guz's Slash Effects FREE (Resources/Fx/Slash), played once each (the packs' prefabs loop,
    /// for their showcase: an effect here emits for its hold, stops and clears).
    ///
    /// Each ability has an element by its class and kind (fire, bone, nature, holy, storm, frost, stone, water, wind, spirit, song,
    /// salt, charm, steel), and a look from what it does: a blow's slash at the foe, a bolt's burst where it lands (Bolt.Burst, by the
    /// bolt's colour), a burst on every foe it strikes, a mend's glow on every one it mends (and a circle under the healer for a mend
    /// of the party), a ward's shield, a hex's shadow on the foe, a rally's glow round the caster, a ground effect for what is called
    /// down or worked all round; a few have their own (a thrall's portal, hail, spikes of bone, a smoke for vanishing). A cast glows in
    /// the casting hand, with a circle under the feet for a long one. Cosmetic only: nothing here touches the fight. GAME-ONLY.
    /// </summary>
    public static class SpellFx
    {
        const string M = "Fx/Magic/Prefabs/", S = "Fx/Slash/Prefab/";
        static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
        /// <summary>Effects off (tests that count objects, captures that want none).</summary>
        public static bool Off;
        /// <summary>How many effects have played (tests).</summary>
        public static int Played { get; private set; }

        static GameObject Load(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (!cache.TryGetValue(path, out var go)) cache[path] = go = Resources.Load<GameObject>(path);
            return go;
        }
        /// <summary>True when the effect prefab is in the build.</summary>
        public static bool Has(string path) { return Load(path) != null; }

        /// <summary>Plays an effect once at a point (or following <paramref name="follow"/>): it emits for <paramref name="hold"/>
        /// seconds (0: one cycle of its longest system), stops and is gone when its last particle is.</summary>
        public static GameObject Play(string path, Vector3 at, Quaternion rot, float scale = 1, Transform follow = null, float hold = 0)
        {
            if (Off || !Application.isPlaying) return null;
            var src = Load(path); if (src == null) return null;
            var go = UnityEngine.Object.Instantiate(src, at, rot);
            go.name = "Fx " + src.name;
            if (scale != 1) go.transform.localScale = go.transform.localScale * scale;
            if (follow != null) go.transform.SetParent(follow, true);
            float cycle = 0, linger = 0;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var m = ps.main; m.scalingMode = ParticleSystemScalingMode.Hierarchy; m.stopAction = ParticleSystemStopAction.None;
                cycle = Mathf.Max(cycle, m.duration); linger = Mathf.Max(linger, m.startLifetime.constantMax + m.startDelay.constantMax);
            }
            var life = go.AddComponent<FxLife>(); life.stopAt = Time.time + (hold > 0 ? hold : Mathf.Clamp(cycle * .92f, .3f, 2.5f)); life.linger = Mathf.Clamp(linger, .3f, 4);
            Played++;
            return go;
        }
        /// <summary>Stops an effect now (it fades as its particles die).</summary>
        public static void Stop(GameObject fx) { if (fx != null) { var l = fx.GetComponent<FxLife>(); if (l != null) l.stopAt = Mathf.Min(l.stopAt, Time.time); } }

        // ------------------------------------------------------------------ the elements
        static readonly HashSet<string> Bonecaller = new HashSet<string> { "mage.raise_thrall", "mage.marrow_drain", "mage.bone_spikes", "mage.ash_hex", "mage.bind", "mage.unbind" };
        /// <summary>The ability's element (see the class summary).</summary>
        public static string Element(AbilityDefinition a)
        {
            string id = a != null ? a.id ?? "" : "";
            switch (id)
            {
                case "shaman.hailfall": case "shaman.mist": return "frost";
                case "shaman.earthskin": case "shaman.cairn": case "shaman.rooting_stamp": return "stone";
                case "shaman.tide_mend": case "shaman.rain_call": case "shaman.chain_mend": return "water";
                case "shaman.moor_wind": return "wind";
                case "shaman.barrow_call": return "spirit";
                case "scoundrel.bribe": case "scoundrel.favour": case "scoundrel.glimmer": case "scoundrel.stay": case "scoundrel.jagged_verse":
                case "scoundrel.war_verse": case "scoundrel.rallying_verse": case "scoundrel.jeer": return "charm";
                case "ranger.snare_trap": case "ranger.call_companion": return "stone";
            }
            if (Bonecaller.Contains(id)) return "bone";
            int dot = id.IndexOf('.'); string cls = dot > 0 ? id.Substring(0, dot) : id;
            switch (cls)
            {
                case "mage": return "fire";
                case "druid": return "nature";
                case "cleric": case "paladin": return "holy";
                case "shaman": return "storm";
                case "archivist": return "song";
                case "salt": return "salt";
                default: return "steel";
            }
        }
        static string Hit(string e)
        {
            switch (e)
            {
                case "fire": return M + "Hits and explosions/Explosion";
                case "bone": return M + "Sparks/Sparks explode pink";
                case "nature": return M + "Hits and explosions/Green hit";
                case "holy": return M + "Hits and explosions/Holy hit";
                case "storm": return M + "Hits and explosions/Electro hit";
                case "frost": case "salt": return M + "Hits and explosions/Snow hit";
                case "stone": return M + "Hits and explosions/Stones hit";
                case "water": return M + "Sparks/Sparks explode blue";
                case "song": return M + "Hits and explosions/Star hit";
                case "charm": return M + "Sparks/Sparks explode yellow";
                case "wind": case "spirit": return M + "Sparks/Sparks explode white";
                default: return M + "Sparks/Sparks explode white";
            }
        }
        static float HitScale(string e) { return e == "fire" ? .45f : e == "stone" ? .7f : .8f; }
        static string Glow(string e)
        {
            switch (e)
            {
                case "fire": return M + "Sparks/Sparks flashing red";
                case "bone": return M + "Sparks/Sparks flashing pink";
                case "nature": return M + "Sparks/Sparks flashing green";
                case "holy": case "charm": return M + "Sparks/Sparks flashing yellow";
                case "storm": case "frost": case "water": return M + "Sparks/Sparks flashing blue";
                default: return M + "Sparks/Sparks flashing white";
            }
        }
        static string Shield(string e) { return e == "fire" || e == "holy" || e == "charm" ? M + "Magic shields/Magic shield yellow" : e == "bone" || e == "song" ? M + "Magic shields/Magic shield pink" : M + "Magic shields/Magic shield blue"; }
        static string Ground(string e)
        {
            switch (e)
            {
                case "fire": return M + "AoE effects/Ground AOE explosion";
                case "bone": return M + "AoE effects/Crystals front attack";
                case "nature": return M + "AoE effects/AoE slash green";
                case "holy": return M + "Magic circles/Magic circle";
                case "storm": return M + "AoE effects/Laser AOE";
                case "frost": return M + "AoE effects/Snow AOE";
                case "salt": return M + "Magic circles/Freeze circle";
                case "stone": return M + "Smoke effects/Dust ground";
                case "water": return M + "Magic circles/Healing circle";
                case "wind": return M + "Smoke effects/Smoke vortex";
                case "spirit": return M + "Portals/Portal blue";
                case "song": return M + "AoE effects/Plexus AoE";
                case "charm": return M + "Character auras/Star aura";
                default: return M + "AoE effects/AoE slash orange";
            }
        }
        /// <summary>The few with a look of their own: (where: "foe", "ground" at the foe, "self", "nova" round the caster), the prefab, its scale.</summary>
        static readonly Dictionary<string, (string where, string path, float scale)> Own = new Dictionary<string, (string, string, float)>
        {
            { "mage.raise_thrall", ("self", M + "Portals/Portal green", .6f) }, { "mage.bone_spikes", ("nova", M + "AoE effects/Crystals front attack", .8f) },
            { "mage.cinder_field", ("ground", M + "AoE effects/Ground AOE explosion", .9f) }, { "mage.flare", ("foe", M + "AoE effects/Red energy explosion", .5f) },
            { "shaman.hailfall", ("ground", M + "AoE effects/Snow AOE", .8f) }, { "shaman.thunderhead", ("foe", M + "Character auras/Lightning aura", .8f) },
            { "shaman.barrow_call", ("ground", M + "Portals/Portal blue", .6f) }, { "shaman.moor_wind", ("nova", M + "Smoke effects/Smoke vortex", .8f) },
            { "shaman.mist", ("ground", M + "Smoke effects/Smoke ground", 1) }, { "shaman.rooting_stamp", ("nova", M + "Smoke effects/Dust ground", 1) },
            { "shaman.cairn", ("ground", M + "AoE effects/Crystals crossfade", .5f) }, { "salt.cairn", ("self", M + "AoE effects/Crystals crossfade", .5f) },
            { "salt.sung_wall", ("self", M + "AoE effects/Crystals crossfade 2", .6f) }, { "salt.circle", ("nova", M + "Magic circles/Freeze circle", 1) },
            { "cleric.sanctuary", ("nova", M + "Magic circles/Healing circle", 1) }, { "cleric.sunburst", ("ground", M + "AoE effects/Laser AOE", .7f) },
            { "paladin.consecrate", ("nova", M + "Magic circles/Magic circle", 1) }, { "paladin.colours", ("self", M + "Character auras/Star aura", 1) },
            { "archivist.chorus", ("nova", M + "AoE effects/Plexus AoE", .8f) }, { "archivist.erasure", ("foe", M + "Environment/Teleport", .6f) },
            { "archivist.unwrite", ("foe", M + "Environment/Teleport", .5f) }, { "druid.bramblestorm", ("ground", M + "AoE effects/AoE slash green", .8f) },
            { "ranger.whirling_spear", ("nova", M + "AoE effects/AoE slash orange", .6f) }, { "druid.rending_flurry", ("foe", S + "Multiple Slash 2 ", 1) },
            { "ranger.call_companion", ("self", M + "Smoke effects/Dust puff", 1) }, { "ranger.snare_trap", ("self", M + "Smoke effects/Dust puff", .7f) },
            { "rogue.vanish", ("self", M + "Smoke effects/Smoke puff", 1) }, { "rogue.tumble", ("self", M + "Smoke effects/Dust puff", .7f) },
            { "ranger.disengage", ("self", M + "Smoke effects/Dust puff", .8f) }, { "ability.intercept", ("self", M + "Smoke effects/Dust puff", .8f) },
            { "rogue.blind", ("foe", M + "Smoke effects/Smoke puff", .6f) }, { "scoundrel.bribe", ("foe", M + "Sparks/Sparks explode yellow", .8f) },
            { "scoundrel.favour", ("ground", M + "Portals/Portal yellow", .5f) }, { "scoundrel.glimmer", ("foe", M + "Sparks/Sparks flashing pink", 1) },
            { "ability.breaching_blow", ("foe", M + "Slash effects/Charge slash red", .8f) }, { "ability.shield_bash", ("foe", M + "Slash effects/Stone slash", .8f) },
            { "paladin.rebuke", ("foe", M + "Slash effects/Stone slash", .8f) }, { "paladin.smite", ("foe", M + "Hits and explosions/Holy hit", 1) },
            { "paladin.judgement", ("foe", M + "Hits and explosions/Holy hit", 1.2f) }, { "mage.marrow_drain", ("foe", M + "Character auras/Debuff", .8f) },
        };

        /// <summary>Every prefab an ability's look can use (tests: each must be in the build).</summary>
        public static List<string> PathsFor(AbilityDefinition a)
        {
            var e = Element(a);
            var l = new List<string> { Hit(e), Glow(e), Shield(e), Ground(e), M + "Character auras/Healing", M + "Magic circles/Healing circle", M + "Character auras/Buff",
                M + "Character auras/Debuff", M + "Magic circles/Magic circle 2", M + "Sparks/Sparks explode red", M + "Smoke effects/Dust puff", M + "Sparks/Sparks flashing yellow", S + "Explosion Slash " };
            if (a != null && a.id != null && Own.TryGetValue(a.id, out var o)) l.Add(o.path);
            l.AddRange(Slashes);
            return l;
        }

        // ------------------------------------------------------------------ an ability resolving
        sealed class Resolve { public AbilityDefinition a; public string element; public readonly List<Transform> struck = new List<Transform>(), mended = new List<Transform>(); public bool bolted; }
        static Resolve now;
        /// <summary>True while an ability resolves (its mends and bursts are shown once, by SpellFx).</summary>
        public static bool Resolving { get { return now != null; } }
        static bool hooked;
        static void Hook()
        {
            if (hooked) return; hooked = true;
            Combatant.Healed += (by, who, amount) => { if (now != null && who != null && !now.mended.Contains(who.transform)) now.mended.Add(who.transform); };
        }
        /// <summary>From EncounterEnemy.Receive: a foe struck while an ability resolves.</summary>
        public static void Struck(Transform t) { if (now != null && t != null && !now.struck.Contains(t)) now.struck.Add(t); }
        /// <summary>From Bolt.Fire: a bolt flew for the ability resolving (its own burst shows where it lands).</summary>
        public static void Bolted() { if (now != null) now.bolted = true; }

        /// <summary>Runs an ability's effect (as it resolves: at once, or when its cast completes) and shows it.</summary>
        public static void Run(AbilityDefinition a, EncounterSession s, Action effect)
        {
            Hook();
            var r = new Resolve { a = a, element = Element(a) }; var was = now; now = r;
            try { effect?.Invoke(); }
            finally { now = was; }
            try { Show(r, s); } catch (Exception e) { Debug.LogWarning("SpellFx: " + e.Message); }
        }
        static Vector3 Chest(Transform t) { return t.position + Vector3.up * 1.05f; }
        static void Show(Resolve r, EncounterSession s)
        {
            if (Off || s == null || s.Player == null) return;
            var a = r.a; var me = s.Player.transform; var foe = s.Target != null && s.Target.actor.IsAlive ? s.Target.transform : r.struck.Count > 0 ? r.struck[0] : null;
            string motion = ActorVisual.MotionFor(a); string e = r.element;
            bool melee = a.range <= 5 && a.range > .5f && (motion == null || motion == "heavy" || motion == "bash");
            // A look of its own.
            if (a.id != null && Own.TryGetValue(a.id, out var own))
            {
                if (own.where == "foe" && foe != null) Play(own.path, Chest(foe), Face(me, foe), own.scale);
                else if (own.where == "ground") Play(own.path, (foe != null ? foe.position : me.position + me.forward * 4) + Vector3.up * .05f, Quaternion.identity, own.scale, null, 1.6f);
                else if (own.where == "nova") Play(own.path, me.position + Vector3.up * .05f, me.rotation, own.scale, null, 1.6f);
                else Play(own.path, me.position + Vector3.up * .05f, me.rotation, own.scale, me, 1.4f);
                if (own.where == "foe" || own.where == "self") { foreach (var t in r.struck) if (t != foe) Burst(e, t); }
                else foreach (var t in r.struck) Burst(e, t);
                Mends(r, s); return;
            }
            // A blow: the slash across the foe.
            if (melee && r.struck.Count > 0) { foreach (var t in r.struck) Slash(me, t, motion == "heavy"); if (e == "holy" || e == "nature" || e == "charm") Burst(e, r.struck[0]); }
            // What works all round the caster, or is called down at the foe.
            else if (motion == "omni" && a.effect == AbilityEffect.Damage) { Play(Ground(e), me.position + Vector3.up * .05f, me.rotation, .7f, null, 1.2f); foreach (var t in r.struck) Burst(e, t); }
            else if (motion == "call" && a.effect == AbilityEffect.Damage) { Play(Ground(e), (foe != null ? foe.position : me.position + me.forward * 4) + Vector3.up * .05f, Quaternion.identity, .7f, null, 1.3f); foreach (var t in r.struck) Burst(e, t); }
            // A bolt or a blow from afar: a burst on every foe struck (a bolt's own burst shows where it lands).
            else if (a.effect == AbilityEffect.Damage || a.effect == AbilityEffect.Breach) { if (!r.bolted) foreach (var t in r.struck) Burst(e, t); }
            // A ward: its shield on the caster.
            else if (a.effect == AbilityEffect.Guard) Play(Shield(e), me.position + Vector3.up * .05f, me.rotation, .8f, me, 1.6f);
            // A rally: a glow round the caster.
            else if (a.effect == AbilityEffect.Rally) Play(M + "Character auras/Buff", me.position + Vector3.up * .05f, me.rotation, .9f, me, 1.5f);
            // A taunt: a red flash on the foe.
            else if (a.effect == AbilityEffect.Taunt) { if (foe != null) Play(M + "Sparks/Sparks explode red", Chest(foe), Quaternion.identity, .8f); }
            else if (a.effect == AbilityEffect.Intercept) Play(M + "Smoke effects/Dust puff", me.position, me.rotation, .8f);
            // A hold or a hex on the foe; a blessing on oneself.
            else if (a.effect == AbilityEffect.ApplyStatus)
            {
                if (a.range > .5f && foe != null && motion != "omni") Play(M + "Character auras/Debuff", foe.position + Vector3.up * .05f, Quaternion.identity, .7f, foe, 1.3f);
                else if (motion == "omni") Play(Ground(e), me.position + Vector3.up * .05f, me.rotation, .7f, null, 1.3f);
                else Play(M + "Character auras/Buff", me.position + Vector3.up * .05f, me.rotation, .8f, me, 1.3f);
                foreach (var t in r.struck) Burst(e, t);
            }
            Mends(r, s);
        }
        /// <summary>Every one mended: the healing glow on them; a mend of the party: the healing circle under the healer.</summary>
        static void Mends(Resolve r, EncounterSession s)
        {
            if (r.mended.Count == 0) return;
            foreach (var t in r.mended) Mend(t, r.element);
            if (r.mended.Count >= 2 || ActorVisual.MotionFor(r.a) == "omni") Play(M + "Magic circles/Healing circle", s.Player.transform.position + Vector3.up * .05f, Quaternion.identity, .8f, null, 1.4f);
        }
        static Quaternion Face(Transform from, Transform to) { var d = to.position - from.position; d.y = 0; return d.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(d) : from.rotation; }
        /// <summary>A burst of the element on a foe struck.</summary>
        public static void Burst(string element, Transform t) { if (t != null) Play(Hit(element), Chest(t), Quaternion.identity, HitScale(element)); }
        /// <summary>The healing glow on one mended (holy mends sparkle gold too).</summary>
        public static void Mend(Transform t, string element = null)
        {
            if (t == null) return;
            Play(M + "Character auras/Healing", t.position + Vector3.up * .05f, t.rotation, .7f, t, 1.1f);
            if (element == "holy") Play(M + "Sparks/Sparks flashing yellow", Chest(t), Quaternion.identity, .7f, t, .8f);
        }
        static readonly string[] Slashes = { S + "Basic Slash Blue", S + "Double Slash", S + "Shiny Slash " };
        static int slashTurn;
        /// <summary>A blow's slash across the foe, facing the way it was struck; a heavy one is the exploding slash.</summary>
        public static void Slash(Transform from, Transform to, bool heavy, float scale = 1)
        {
            if (from == null || to == null) return;
            string p = heavy ? S + "Explosion Slash " : Slashes[slashTurn++ % Slashes.Length];
            Play(p, Chest(to) - Face(from, to) * Vector3.forward * .3f, Face(from, to), scale);
        }

        // ------------------------------------------------------------------ a cast being drawn
        /// <summary>A cast drawn: the element's sparks in the casting hand, and for a long cast a circle under the feet. Stop both with <see cref="CastEnd"/>.</summary>
        public static (GameObject hand, GameObject circle) CastBegin(AbilityDefinition a, Transform hand, Transform feet)
        {
            if (a == null || Off) return (null, null);
            string e = Element(a);
            var h = hand != null ? Play(Glow(e), hand.position, Quaternion.identity, .45f, hand, 30) : null;
            var c = feet != null && a.castTime >= 1.5f ? Play(a.effect == AbilityEffect.Heal ? M + "Magic circles/Healing circle" : M + "Magic circles/Magic circle 2", feet.position + Vector3.up * .05f, Quaternion.identity, .55f, feet, 30) : null;
            return (h, c);
        }
        public static void CastEnd((GameObject hand, GameObject circle) fx) { Stop(fx.hand); Stop(fx.circle); }

        /// <summary>For Bolt.Burst: the hit that fits a bolt's colour (null: none fits).</summary>
        public static string HitForColour(Color c)
        {
            Color.RGBToHSV(c, out float h, out float sat, out float v);
            if (sat < .2f) return Hit(v > .7f ? "salt" : "stone");
            if (h < .07f || h > .93f) return Hit("fire");
            if (h < .18f) return Hit("holy");
            if (h < .45f) return Hit("nature");
            if (h < .7f) return Hit(sat < .45f ? "frost" : "storm");
            return Hit("bone");
        }
    }

    /// <summary>An effect's life: emits until <see cref="stopAt"/>, then stops and is gone once its particles have died.</summary>
    public sealed class FxLife : MonoBehaviour
    {
        public float stopAt, linger = 1; bool stopped; float goneAt;
        void Update()
        {
            if (!stopped && Time.time >= stopAt)
            {
                stopped = true; goneAt = Time.time + linger;
                foreach (var ps in GetComponentsInChildren<ParticleSystem>(true)) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                foreach (var l in GetComponentsInChildren<Light>(true)) l.enabled = false;
            }
            if (stopped && Time.time >= goneAt) Destroy(gameObject);
        }
    }
}
