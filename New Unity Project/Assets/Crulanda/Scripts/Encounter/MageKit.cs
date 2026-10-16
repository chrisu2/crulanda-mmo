using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.Abilities;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Mage (Phase 5.1c, 2026-10-05; provisional GAME-ONLY name and mechanics, CLASS_BUILD_MATRIX: Combustion ranged, Heatweaver
    /// control, Spellbinder support): a caster on Mana with a second, visible gauge, Heat (0-100). Ember Bolt (a 1.5 s cast) and
    /// Scorch build it, Flare releases it in one burst, and reaching 100 is an Overload: a burn of your own health, the gauge
    /// emptied and four seconds in which no Heat builds. Cinder Field burns the ground under the target, Smoulder slows, Ember
    /// Ward is a barrier. Bind makes the next Ember Bolt weaker and banks a Charge; Unbind spends it so the whole party hits
    /// harder for a while. Bar: 1 Ember Bolt, 2 Scorch, 3 Flare, 4 Cinder Field, 5 Smoulder, 6 Ember Ward, 7 Bind, 8 Unbind,
    /// 9 Quench (talent), 0 Ash Hex; the Bonecaller's (the necromancer, 2026-10-12) - Raise Bone Thrall, = Marrow Drain, [ Bone Spikes (talent). Base
    /// abilities, rows 0-2 of Talents/mage.json and the Bonecaller's rows 0-3. Nothing here is saved.
    /// </summary>
    public sealed class MageKit : ClassKit, IPetMaster
    {
        public static readonly string[] ImplementedIds = {
            "cb-kindling", "cb-hot-hands", "cb-stoked", "cb-flashpoint", "cb-backdraft", "cb-inferno", "cb-white-heat",
            "hw-cinders", "hw-slow-burn", "hw-wide-field", "hw-embers-underfoot", "hw-heat-haze", "hw-long-burn", "hw-firestorm",
            "sb-reservoir", "sb-ward-weave", "sb-quench", "sb-shared-flame", "sb-steady-mind", "sb-quickening", "sb-binding-oath",
            "bc-hard-bone", "bc-sharp-marrow", "bc-grave-bond", "bc-long-drain", "bc-bone-spikes", "bc-death-echo", "bc-bone-armour", "bc-wide-spikes"   // the Bonecaller, rows 0-3
        };
        public static readonly string[] BarIds = { "mage.ember_bolt", "mage.scorch", "mage.flare", "mage.cinder_field", "mage.smoulder", "mage.ember_ward", "mage.bind", "mage.unbind", "mage.quench", "mage.ash_hex",
            "mage.raise_thrall", "mage.marrow_drain", "mage.bone_spikes" };
        public const int MaxHeat = 100, FlareMinHeat = 30, EmberHeat = 15, ScorchHeat = 10, SmoulderHeat = 5;
        public const float OverloadBurn = .08f, OverloadSeconds = 4, HeatKept = 10, BoundLoss = .4f, UnbindBonus = .15f;

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        readonly PeriodicEffects periodic = new PeriodicEffects();
        bool resetting, backdraft;
        float heatKeptUntil, overloadedUntil, fieldUntil, unboundUntil;
        Vector3 fieldCentre;

        public int Heat { get; private set; }
        /// <summary>Bind is set: the next Ember Bolt is weaker and banks a Charge.</summary>
        public bool Binding { get; private set; }
        public int Charges { get; private set; }
        public bool Overloaded { get { return Now < overloadedUntil; } }
        public bool FieldBurning { get { return Now < fieldUntil; } }
        public bool Unbound { get { return Now < unboundUntil; } }
        public float FieldRadius { get { return 4 + .5f * R("hw-wide-field"); } }
        public PeriodicEffects Periodic { get { return periodic; } }

        public MageKit(EncounterSession session, ClassDefinition definition, IEnumerable<AbilityDefinition> catalog) : base(session)
        {
            if (definition == null || definition.unlocks == null || catalog == null) throw new ArgumentException("Mage content missing.");
            foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !abilities.ContainsKey(a.id)) abilities.Add(a.id, a);
            foreach (var u in definition.unlocks)
                if (u == null || string.IsNullOrEmpty(u.abilityId) || u.level < 1 || u.level > 10 || unlocks.ContainsKey(u.abilityId))
                    throw new ArgumentException("Invalid or duplicate Mage unlock.");
                else unlocks.Add(u.abilityId, u);
            foreach (var id in BarIds)
                if (!abilities.ContainsKey(id) || !unlocks.ContainsKey(id)) throw new ArgumentException("Mage ability '" + id + "' is missing from content or unlocks.");
        }
        public override string[] ImplementedTalents { get { return ImplementedIds; } }
        /// <summary>Its standing party buff at a buff-up before a boss (Chris, 2026-10-15: "food buffs, alchemy buffs, class buffs"; GAME-ONLY):
        /// a spark of the Mage's Heat lent to every mind. Half an hour, +3% Intellect on everyone (BossBuffs).</summary>
        public static readonly ClassBuff Standing = new ClassBuff("class.mage", "Kindled Wits", Crulanda.Core.StatType.Intellect);
        public override ClassBuff PartyBuff { get { return Standing; } }
        public override bool MeleeAutoAttacks { get { return false; } }   // a caster: no swings, a release at range
        int R(string id) { return s.TalentRank(id); }
        float Now { get { return Time.time; } }
        int Level { get { return s.Player.Level; } }
        Combatant PlayerCombat { get { return s.Player.GetComponent<Combatant>(); } }

        // ---------- Heat ----------
        /// <summary>Builds Heat (none while Overloaded); at 100 the Mage Overloads.</summary>
        public void GainHeat(int n)
        {
            if (n <= 0 || Overloaded) return;
            Heat = Mathf.Clamp(Heat + n, 0, MaxHeat); heatKeptUntil = Now + HeatKept;
            if (Heat >= MaxHeat) Overload();
        }
        void Overload()
        {
            Heat = 0; overloadedUntil = Now + OverloadSeconds;
            if (R("cb-white-heat") == 0 && s.Player.IsAlive)
            {
                int burn = Mathf.Max(1, Mathf.RoundToInt(s.Player.Health.Pool.Max * OverloadBurn));
                PlayerCombat.Damage(burn); s.FloatText(s.Player.transform.position, "-" + burn, new Color(1, .5f, .2f));
                s.Message("Overload! The heat burns you for " + burn + "; nothing builds for " + OverloadSeconds + " seconds.");
            }
            else s.Message("Overload: the heat is spent; nothing builds for " + OverloadSeconds + " seconds.");
        }
        bool InField(EncounterEnemy e) { return FieldBurning && e != null && Vector3.Distance(e.transform.position, fieldCentre) <= FieldRadius; }

        // ---------- action bar ----------
        public override int ActionCount { get { return BarIds.Length; } }
        public override AbilityDefinition ActionAt(int slot) { return slot >= 0 && slot < BarIds.Length && abilities.TryGetValue(BarIds[slot], out var a) ? a : null; }
        public override string ActionLockLabel(int slot)
        {
            var a = ActionAt(slot); if (a == null || s.Player == null) return "Unavailable";
            var u = unlocks[a.id];
            if (Level < u.level) return "Level " + u.level;
            if (!string.IsNullOrEmpty(u.talentId) && R(u.talentId) == 0) return "Talent";
            if (a.id == "mage.flare" && Heat < FlareMinHeat) return "Heat " + FlareMinHeat;
            if (a.id == "mage.unbind" && Charges == 0) return "No Charge";
            return null;
        }
        public override bool Use(int slot)
        {
            var a = ActionAt(slot); if (a == null) return false;
            switch (a.id)
            {
                case "mage.ember_bolt": return EmberBolt(a);
                case "mage.scorch": return Scorch(a);
                case "mage.flare": return Flare(a);
                case "mage.cinder_field": return CinderField(a);
                case "mage.smoulder": return Smoulder(a);
                case "mage.ember_ward": return EmberWard(a);
                case "mage.bind": return Bind(a);
                case "mage.unbind": return Unbind(a);
                case "mage.quench": return Quench(a);
                case "mage.ash_hex": return AshHex(a);
                case "mage.raise_thrall": return RaiseThrall(a);   // the Bonecaller
                case "mage.marrow_drain": return MarrowDrain(a);
                case "mage.bone_spikes": return BoneSpikes(a);
            }
            return false;
        }
        static AbilityDefinition With(AbilityDefinition a, int cost, float castTime)
        {
            return new AbilityDefinition { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = cost,
                cooldown = a.cooldown, range = a.range, castTime = castTime, globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId };
        }
        bool Start(AbilityDefinition a, Action effect) { return s.StartAbility(a, () => { if (s.Player.IsAlive) effect(); }) == AbilityStartResult.Started; }
        bool Need(bool ok, string message) { if (!ok) s.Message(message); return ok; }
        int Dmg(float amount) { return Mathf.Max(1, Mathf.RoundToInt(amount)); }
        void Hit(EncounterEnemy t, int damage) { t.Receive(damage, s.Player); }
        int HealActor(Actor target, int amount)
        {
            if (target == null || !target.IsAlive || amount <= 0) return 0;
            int healed = target.GetComponent<Combatant>().Heal(amount, s.Player);
            if (healed > 0) { s.AddHealingThreat(healed); s.FloatText(target.transform.position, "+" + healed, new Color(1, .95f, .6f)); }
            return healed;
        }

        // ---------- Combustion ----------
        bool EmberBolt(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            bool instant = backdraft; bool bound = Binding;
            float cast = instant ? 0 : Mathf.Max(.5f, a.castTime - .1f * R("cb-hot-hands"));
            return Start(With(a, a.cost, cast), () => {
                if (instant) backdraft = false;
                if (!s.LandsOn(t, a.range)) return;
                s.Bolt(t, new Color(1, .55f, .15f), .2f);
                float d = (a.power + 2 * Level) * (1 + .03f * R("cb-kindling"));
                if (bound) { d *= 1 - Mathf.Max(.1f, BoundLoss - .1f * R("sb-steady-mind")); Binding = false; Charges = 1; s.Message("Bound: a Charge is banked for Unbind."); }
                Hit(t, Dmg(d)); GainHeat(EmberHeat); MendHound();
            });
        }
        bool Scorch(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.Bolt(t, new Color(1, .32f, .1f), .16f);
                Hit(t, Dmg(a.power + Level)); GainHeat(ScorchHeat);
                int ticks = 4 + R("cb-stoked");
                periodic.Add("scorch", t, Now, 2, ticks, Dmg(3 + Level / 2f), (e, i) => { var enemy = (EncounterEnemy)e.target; if (!enemy.actor.IsAlive) return false; Hit(enemy, e.value); return true; });
            });
        }
        bool Flare(AbilityDefinition a)
        {
            if (!Need(Heat >= FlareMinHeat, "Flare needs " + FlareMinHeat + " Heat; Ember Bolt and Scorch build it.") || !s.RequireEnemyInRange(a.range)) return false;
            var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range) || Heat < FlareMinHeat) return;
                s.Bolt(t, new Color(1, .72f, .25f), .34f);
                int spent = Heat; Heat = 0;
                int d = Dmg((a.power + Level + spent * .6f) * (1 + .08f * R("cb-flashpoint")));
                Hit(t, d);
                if (spent >= 70 && R("cb-inferno") > 0)
                    foreach (var o in s.Enemies)
                        if (o != t && o.actor.IsAlive && Vector3.Distance(o.transform.position, t.transform.position) <= 4) { Hit(o, Mathf.Max(1, d / 2)); break; }
                if (R("cb-backdraft") > 0) backdraft = true;
                s.Message("Flare: " + spent + " Heat released.");
            });
        }

        // ---------- Heatweaver ----------
        bool CinderField(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!t.actor.IsAlive) return;
                fieldCentre = t.transform.position; int seconds = Mathf.RoundToInt(a.duration) + 2 * R("hw-long-burn"); fieldUntil = Now + seconds;
                int per = Dmg((a.power + Level / 2f) * (1 + .1f * R("hw-cinders"))); int underfoot = R("hw-embers-underfoot");
                periodic.Add("cinder", s.Player, Now, 1, seconds, per, (e, i) => {
                    foreach (var o in s.Enemies)
                        if (o.actor.IsAlive && InField(o)) { Hit(o, e.value); if (underfoot > 0) o.Slow(.2f, 1.2f); }
                    return true;
                }, null, true);
                s.Message("Cinder Field: the ground there burns for " + seconds + " seconds.");
            });
        }
        bool Smoulder(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.Bolt(t, new Color(.55f, .5f, .48f), .14f);
                t.Slow(.5f, a.duration + R("hw-slow-burn")); GainHeat(SmoulderHeat);
                if (R("hw-firestorm") > 0 && InField(t)) { t.Root(2); s.Message("Firestorm: the target is held in the burning ground."); }
            });
        }
        EncounterEnemy hexed;
        /// <summary>Ash Hex (CC_DESIGN section 2): a person or beast turned to smouldering ash for 25 s; damage breaks it; one at a time.</summary>
        bool AshHex(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                if (hexed != null && hexed != t && hexed.Incapacitated) hexed.Release();
                string why = t.Apply("incap", s.Player, a.duration);
                if (why == null) { hexed = t; s.Message("Ash Hex: " + t.actor.DisplayName + " stands as smouldering ash."); } else s.Message(t.actor.DisplayName + ": " + why + ".");
            });
        }
        bool Quench(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.Bolt(t, new Color(.6f, .85f, 1), .16f);
                Hit(t, Dmg(a.power + Level)); t.Stagger(a.duration); t.Apply("silence", s.Player, 4);   // an interrupt (CC_DESIGN section 2)
                overloadedUntil = 0;
                s.Message("Quench: the target's swing is held back for " + a.duration + " seconds; your own heat is steadied.");
            });
        }

        // ---------- Spellbinder ----------
        bool EmberWard(AbilityDefinition a)
        {
            return Start(a, () => {
                PlayerCombat.AddBarrier(Mathf.RoundToInt((a.power + 2 * Level) * (1 + .06f * R("sb-ward-weave"))), a.duration);
                s.Message("Ember Ward: a barrier for " + a.duration + " seconds.");
            });
        }
        bool Bind(AbilityDefinition a)
        {
            if (Binding) { Binding = false; s.Message("Bind released."); return true; }
            if (!Need(Charges == 0, "A Charge is already banked: Unbind first.")) return false;
            return Start(a, () => { Binding = true; s.Message("Bind: the next Ember Bolt is weaker and banks a Charge."); });
        }
        bool Unbind(AbilityDefinition a)
        {
            if (!Need(Charges > 0, "Unbind needs a Charge: Bind, then Ember Bolt.")) return false;
            return Start(a, () => {
                if (Charges == 0) return; Charges = 0;
                float seconds = a.duration + R("sb-reservoir"); unboundUntil = Now + seconds;
                int shared = R("sb-shared-flame");
                if (shared > 0)
                    foreach (var who in new[] { s.Player, s.Progress.recruited && s.Companion != null ? s.Companion.actor : null })
                        if (who != null && who.IsAlive) HealActor(who, Mathf.RoundToInt(who.Health.Pool.Max * .03f * shared));
                s.Message("Unbind: the party hits " + Mathf.RoundToInt(Bonus * 100) + "% harder for " + seconds + " seconds.");
            });
        }
        float Bonus { get { return R("sb-binding-oath") > 0 ? .25f : UnbindBonus; } }
        public override float PartyDamageMultiplier(EncounterEnemy enemy) { return Unbound ? 1 + Bonus : 1; }
        public override float CompanionHaste { get { return Unbound ? .1f * R("sb-quickening") : 0; } }

        // ---------- the Bonecaller (the necromancer; reworked from the Cinderwright, Chris 2026-10-12: "give the mage a necro build",
        // then "Rework existing paths into the new builds"; GAME-ONLY: the books have no necromancy, the game's risen dead (the ossuary
        // knight) are its bones; not the Wasting, which unmakes, where this calls the bones of the dead back up; no machines) ----------
        // A bone thrall at heel (the risen dead's skeleton, a party member like the Ranger's wolf): Raise Bone Thrall calls it, Marrow
        // Drain draws the life out of a foe over time into you and the thrall, Bone Spikes bursts the ground round the thrall.
        public const float DrainEvery = 2, DrainShare = .5f, SpikeReach = 4, SpikeCost = .1f, BoneArmourCut = .3f;
        public const int DrainTicks = 5;
        /// <summary>The bone thrall at heel (null until raised, or once it has fallen).</summary>
        public RangerPet Thrall { get { return s.Pet; } }
        bool ThrallUp { get { return Thrall != null && Thrall.actor != null && Thrall.actor.IsAlive; } }
        public bool Draining { get { foreach (var e in s.Enemies) if (e != null && periodic.Has("marrow", e)) return true; return false; } }
        public float PetHealthScale { get { return 1 + .08f * R("bc-hard-bone"); } }
        public float PetBiteScale { get { return 1 + .1f * R("bc-sharp-marrow"); } }
        public bool PetHolds { get { return false; } }
        /// <summary>Death Echo: each of its blows gives you back Mana.</summary>
        public void OnPetBite(EncounterEnemy target, int bite)
        {
            int k = R("bc-death-echo"); if (k <= 0) return;
            var pool = s.Player.Resource.Pool; pool.SetCurrent(Mathf.Min(pool.Max, pool.Current + k));
        }
        /// <summary>Grave Bond: an Ember Bolt that lands mends the thrall.</summary>
        void MendHound() { int k = R("bc-grave-bond"); if (k > 0 && ThrallUp) Thrall.actor.GetComponent<Combatant>().Heal(Mathf.RoundToInt(Thrall.actor.Health.Pool.Max * .02f * k), s.Player); }
        static readonly Color BoneWhite = new Color(.86f, .84f, .74f), DrainGreen = new Color(.5f, .85f, .55f);
        /// <summary>Raise Bone Thrall: the bones of the dead stood up at your side; again, they are laid down.</summary>
        bool RaiseThrall(AbilityDefinition a)
        {
            if (ThrallUp) return Start(With(a, 0, 0), () => { s.DismissPet(); s.Message("Your bone thrall is laid back down."); });
            return Start(a, () => { s.SummonPet(this, "Bone thrall", "thrall", ActorLook.Skeleton); s.Message("The bones of the dead stand up at your side."); });
        }
        /// <summary>Marrow Drain: the life drawn out of the target every 2 s for 10 s (Long Drain: longer); half of each draw mends you and
        /// your thrall. Bone Armour: while it draws, the thrall takes 30% less.</summary>
        bool MarrowDrain(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.Bolt(t, DrainGreen, .16f);
                ArchivistKit.Aura(t.transform, "Marrow drain", new Color(.55f, .08f, .1f), DrainEvery * (DrainTicks + R("bc-long-drain")) + .3f, .5f, 1.1f, 8);   // the drain shows on the body
                periodic.Add("marrow", t, Now, DrainEvery, DrainTicks + R("bc-long-drain"), Dmg(a.power + Level), (e, i) => {
                    var enemy = (EncounterEnemy)e.target; if (!enemy.actor.IsAlive) return false;
                    Hit(enemy, e.value); int back = Mathf.Max(1, Mathf.RoundToInt(e.value * DrainShare));
                    PlayerCombat.Heal(back, s.Player); if (ThrallUp) Thrall.actor.GetComponent<Combatant>().Heal(back, s.Player);
                    if (resetting == false) s.FloatText(enemy.transform.position + Vector3.up * 1.6f, "drained", DrainGreen);
                    return true;
                });
                s.Message("Marrow Drain: the life drawn out of " + t.actor.DisplayName + ".");
            });
        }
        /// <summary>Bone Spikes (a talent): bone bursts up round the thrall: every foe within 4 m (Wide Spikes: further) is struck and
        /// slowed; it costs the thrall a tenth of its health.</summary>
        bool BoneSpikes(AbilityDefinition a)
        {
            if (!Need(ThrallUp, "No thrall: Raise Bone Thrall first.")) return false;
            return Start(a, () => {
                if (!ThrallUp) return;
                var at = Thrall.transform.position; float reach = SpikeReach + R("bc-wide-spikes"); int dealt = Dmg(a.power + 3 * Level);
                foreach (var e in s.Enemies.ToArray())
                    if (e != null && e.actor.IsAlive && !e.Game && !e.Hidden && (e.transform.position - at).sqrMagnitude <= reach * reach) { Hit(e, dealt); if (e.actor.IsAlive) e.Slow(.4f, 3); }
                Thrall.actor.GetComponent<Combatant>().Damage(Mathf.Max(1, Mathf.RoundToInt(Thrall.actor.Health.Pool.Max * SpikeCost)));
                SpikeFx(at, reach);
                s.FloatText(at, "Bone Spikes", BoneWhite);
            });
        }
        /// <summary>The spikes seen: a ring of bone shards standing out of the ground for a second and a half.</summary>
        static void SpikeFx(Vector3 at, float reach)
        {
            var root = new GameObject("Bone spikes"); root.transform.position = at;
            for (int i = 0; i < 14; i++)
            {
                float t = i * Mathf.PI * 2 / 14, r = reach * (.35f + .55f * ((i * 7) % 5) / 4f);
                var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); UnityEngine.Object.Destroy(g.GetComponent<Collider>());
                g.transform.SetParent(root.transform, false); g.transform.localPosition = new Vector3(Mathf.Cos(t) * r, .45f, Mathf.Sin(t) * r);
                g.transform.localRotation = Quaternion.Euler(Mathf.Sin(t) * 18, 0, Mathf.Cos(t) * -18); g.transform.localScale = new Vector3(.13f, .5f, .13f);   // a metre tall, thick enough to read from the camera (step-4 frames)
                g.GetComponent<Renderer>().material.color = BoneWhite;
            }
            UnityEngine.Object.Destroy(root, 1.5f);
        }

        // ---------- incoming blows ----------
        public override int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        {
            if (victim == null || !victim.IsAlive) return 0;
            int haze = R("hw-heat-haze");
            float mult = haze > 0 && InField(enemy) ? 1 - .04f * haze : 1;
            if (ThrallUp && victim == Thrall.actor && R("bc-bone-armour") > 0 && Draining) mult *= 1 - BoneArmourCut;   // Bone Armour
            return victim.GetComponent<Combatant>().Damage(Mathf.RoundToInt(raw * mult));
        }

        // ---------- per frame / lifecycle ----------
        public override void Tick(bool inCombat)
        {
            periodic.Tick(Now);
            if (!inCombat && Heat > 0 && Now > heatKeptUntil) Heat = 0;
        }
        public override void ResetState()
        {
            resetting = true; periodic.Clear(); resetting = false;
            Heat = Charges = 0; Binding = backdraft = false; heatKeptUntil = overloadedUntil = fieldUntil = unboundUntil = 0;
        }
        public override void ClearTransient()
        {
            resetting = true; periodic.Clear(); resetting = false;
            fieldUntil = unboundUntil = 0; backdraft = false;
            if (s.Player != null) PlayerCombat.ClearBarrier();
            if (Thrall != null) Thrall.Refit();
        }

        // ---------- HUD ----------
        public override string StatusLine
        {
            get
            {
                var parts = new List<string> { "Heat " + Heat + "/" + MaxHeat + (Overloaded ? " OVERLOADED " + (overloadedUntil - Now).ToString("0.0") + "s" : "") };
                if (Binding) parts.Add("Binding"); if (Charges > 0) parts.Add("Charge banked");
                if (Unbound) parts.Add("Unbound " + (unboundUntil - Now).ToString("0.0") + "s");
                if (FieldBurning) parts.Add("Cinder Field");
                if (ThrallUp) parts.Add("Thrall " + Thrall.actor.Health.Pool.Current + "/" + Thrall.actor.Health.Pool.Max);
                int barrier = PlayerCombat.Barrier; if (barrier > 0) parts.Add("Barrier " + barrier);
                return string.Join("  ·  ", parts);
            }
        }
        public override string TargetStatus(EncounterEnemy enemy)
        {
            var parts = new List<string>();
            if (periodic.Has("scorch", enemy)) parts.Add("Scorched");
            if (periodic.Has("marrow", enemy)) parts.Add("Drained");
            if (InField(enemy)) parts.Add("In the cinders");
            if (enemy.Rooted) parts.Add("HELD " + enemy.RootRemaining.ToString("0.0") + "s"); else if (enemy.Slowed) parts.Add("SMOULDERING");
            return parts.Count == 0 ? null : string.Join("  ·  ", parts);
        }
    }
}
