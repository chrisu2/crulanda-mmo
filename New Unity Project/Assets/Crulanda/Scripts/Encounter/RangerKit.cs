using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.Abilities;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Ranger (Phase 5.1b, 2026-10-05; provisional GAME-ONLY name and mechanics, CLASS_BUILD_MATRIX: Marksman ranged, Beastbond
    /// pet, Pathfinder control): a bow at 25 m on Focus, with a wolf at heel. Quick Shot starts the bow's own auto-shot (the first
    /// ranged auto-attack: ClassKit.RangedAutoAttacks), Aimed Shot is the drawn 1.5 s arrow, Barbed Arrow bleeds, Hunter's Mark
    /// makes a target take more from the whole party, Snare slows, Disengage leaps back. Call Companion whistles up the wolf
    /// (RangerPet) and Sic sends it at the target. Bar: 1 Quick Shot, 2 Aimed Shot, 3 Barbed Arrow, 4 Hunter's Mark, 5 Snare,
    /// 6 Call Companion, 7 Sic, 8 Disengage, 9 Pin (talent), 0 Snare Trap; the Skirmisher's - Spear Lunge, = Whirling Spear, [ Feint (talent).
    /// Base abilities, rows 0-2 of Talents/ranger.json and the Skirmisher's rows 0-3. Nothing here is saved.
    /// </summary>
    public sealed class RangerKit : ClassKit, IPetMaster
    {
        public static readonly string[] ImplementedIds = {
            "mk-steady-hand", "mk-quick-draw", "mk-piercing", "mk-bleeding-wounds", "mk-snap-shot", "mk-headshot", "mk-double-nock",
            "bb-thick-coat", "bb-sharp-teeth", "bb-mending-bond", "bb-sic-fury", "bb-pack-sense", "bb-howl", "bb-alpha",
            "pf-fleet", "pf-tangling-snare", "pf-pin", "pf-keen-eye", "pf-cover-of-leaves", "pf-trapper", "pf-pathfinder",
            "sk-long-haft", "sk-light-step", "sk-bleeding-point", "sk-quick-recovery", "sk-feint", "sk-pack-tactics", "sk-whirl-edge", "sk-hit-and-run",   // the Skirmisher, tiers 0-3 (2026-10-11)
            "mk-practised-draw", "bb-scarred-hide", "bb-shared-hunt", "pf-lasting-mark", "sk-second-wind"   // the level-20 rows (2026-10-16)
        };
        public const float PractisedDrawPerRank = .1f, ScarredHidePerRank = .03f, LastingMarkPerRank = 3, BreathReach = 5;
        public const int SharedHuntFocus = 2, BreathFocusPerRank = 5;
        public static readonly string[] BarIds = { "ranger.quick_shot", "ranger.aimed_shot", "ranger.barbed_arrow", "ranger.hunters_mark", "ranger.snare", "ranger.call_companion", "ranger.sic", "ranger.disengage", "ranger.pin", "ranger.snare_trap",
            "ranger.spear_lunge", "ranger.whirling_spear", "ranger.feint" };
        public const float BowRange = 25, MarkBonus = .1f, DisengageLeap = 6;

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        readonly PeriodicEffects periodic = new PeriodicEffects();
        readonly Dictionary<EncounterEnemy, float> markedUntil = new Dictionary<EncounterEnemy, float>();
        bool resetting, freeQuick;

        public PeriodicEffects Periodic { get { return periodic; } }
        /// <summary>The wolf at heel (null until called, or once it has fallen and been let go).</summary>
        public RangerPet Wolf { get { return s.Pet; } }
        public bool Marked(EncounterEnemy e) { return e != null && markedUntil.TryGetValue(e, out var until) && Now < until; }

        public RangerKit(EncounterSession session, ClassDefinition definition, IEnumerable<AbilityDefinition> catalog) : base(session)
        {
            if (definition == null || definition.unlocks == null || catalog == null) throw new ArgumentException("Ranger content missing.");
            foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !abilities.ContainsKey(a.id)) abilities.Add(a.id, a);
            foreach (var u in definition.unlocks)
                if (u == null || string.IsNullOrEmpty(u.abilityId) || u.level < 1 || u.level > 10 || unlocks.ContainsKey(u.abilityId))
                    throw new ArgumentException("Invalid or duplicate Ranger unlock.");
                else unlocks.Add(u.abilityId, u);
            foreach (var id in BarIds)
                if (!abilities.ContainsKey(id) || !unlocks.ContainsKey(id)) throw new ArgumentException("Ranger ability '" + id + "' is missing from content or unlocks.");
        }
        public override string[] ImplementedTalents { get { return ImplementedIds; } }
        /// <summary>Its standing party buff at a buff-up before a boss (Chris, 2026-10-15: "food buffs, alchemy buffs, class buffs"; GAME-ONLY):
        /// the hunter's way of moving, shown the party before a stalk. Half an hour, +3% Agility on everyone (BossBuffs).</summary>
        public static readonly ClassBuff Standing = new ClassBuff("class.ranger", "Trail-Sense", Crulanda.Core.StatType.Agility);
        public override ClassBuff PartyBuff { get { return Standing; } }
        int R(string id) { return s.TalentRank(id); }
        float Now { get { return Time.time; } }
        int Level { get { return s.Player.Level; } }
        Combatant PlayerCombat { get { return s.Player.GetComponent<Combatant>(); } }

        // ---------- the bow ----------
        public override bool MeleeAutoAttacks { get { return false; } }
        public override bool RangedAutoAttacks { get { return true; } }
        public override float AutoAttackRange { get { return BowRange; } }
        public override float SwingInterval(float baseInterval) { return baseInterval * (1 - .05f * R("mk-quick-draw")); }
        public override float MoveSpeedMultiplier { get { return 1 + .02f * R("pf-fleet") + (R("sk-light-step") > 0 && FoeWithin(5) ? .01f * R("sk-light-step") : 0); } }   // Light Step: close to a foe
        public override void OnAutoHit() { MendWolf(); if (s.Target != null) struckAt[s.Target] = Now; }
        /// <summary>Hunter's Mark: a marked target takes more from everyone in the party; Pack Tactics: so does one you and the wolf are both on.</summary>
        public override float PartyDamageMultiplier(EncounterEnemy enemy)
        { return (Marked(enemy) ? 1 + MarkBonus + .02f * R("pf-keen-eye") : 1) * (Pack(enemy) ? 1 + PackPerRank * R("sk-pack-tactics") : 1); }
        int Shot(float amount) { return Mathf.Max(1, Mathf.RoundToInt(amount * (1 + .03f * R("mk-steady-hand")))); }
        void MendWolf() { int bond = R("bb-mending-bond"); if (bond > 0 && Wolf != null && Wolf.actor.IsAlive) Wolf.actor.GetComponent<Combatant>().Heal(Mathf.RoundToInt(Wolf.actor.Health.Pool.Max * .02f * bond), s.Player); }

        // ---------- action bar ----------
        public override int ActionCount { get { return BarIds.Length; } }
        public override AbilityDefinition ActionAt(int slot) { return slot >= 0 && slot < BarIds.Length && abilities.TryGetValue(BarIds[slot], out var a) ? a : null; }
        public override string ActionLockLabel(int slot)
        {
            var a = ActionAt(slot); if (a == null || s.Player == null) return "Unavailable";
            var u = unlocks[a.id];
            if (Level < u.level) return "Level " + u.level;
            if (!string.IsNullOrEmpty(u.talentId) && R(u.talentId) == 0) return "Talent";
            if (a.id == "ranger.sic" && (Wolf == null || !Wolf.actor.IsAlive)) return "No wolf";
            return null;
        }
        public override bool Use(int slot)
        {
            var a = ActionAt(slot); if (a == null) return false;
            switch (a.id)
            {
                case "ranger.quick_shot": return QuickShot(a);
                case "ranger.aimed_shot": return AimedShot(a);
                case "ranger.barbed_arrow": return BarbedArrow(a);
                case "ranger.hunters_mark": return HuntersMark(a);
                case "ranger.snare": return Snare(a);
                case "ranger.call_companion": return CallCompanion(a);
                case "ranger.sic": return Sic(a);
                case "ranger.disengage": return Disengage(a);
                case "ranger.pin": return Pin(a);
                case "ranger.snare_trap": return SnareTrap(a);
                case "ranger.spear_lunge": return SpearLunge(a);   // the Skirmisher
                case "ranger.whirling_spear": return WhirlingSpear(a);
                case "ranger.feint": return Feint(a);
            }
            return false;
        }
        static AbilityDefinition With(AbilityDefinition a, int cost, float castTime, float cooldown = -1)
        {
            return new AbilityDefinition { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = cost,
                cooldown = cooldown >= 0 ? cooldown : a.cooldown, range = a.range, castTime = castTime, globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId };
        }
        bool Start(AbilityDefinition a, Action effect) { return s.StartAbility(a, () => { if (s.Player.IsAlive) effect(); }) == AbilityStartResult.Started; }
        bool Need(bool ok, string message) { if (!ok) s.Message(message); return ok; }
        void Hit(EncounterEnemy t, int damage) { struckAt[t] = Now; t.Receive(damage, s.Player); }
        void AfterShot(EncounterEnemy t) { MendWolf(); if (R("bb-pack-sense") > 0 && Wolf != null && Wolf.actor.IsAlive && Wolf.Quarry != t) Wolf.Hunt(t, 1.5f); }   // it turns at once to what you shoot, and bites the harder for it

        // ---------- Marksman ----------
        bool QuickShot(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            int cost = freeQuick ? 0 : Mathf.Max(0, a.cost - 2 * R("mk-quick-draw"));
            return Start(With(a, cost, 0), () => {
                freeQuick = false;
                if (!s.LandsOn(t, a.range)) return;
s.Bolt(t, new Color(.9f, .88f, .8f), .1f, true); s.BeginAutoAttack(); Hit(t, Shot(a.power + s.WeaponDamage * .6f)); AfterShot(t);
            });
        }
        bool AimedShot(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            int draw = R("mk-practised-draw");   // Practised Draw: drawn quicker
            if (draw > 0) a = With(a, a.cost, Mathf.Max(.5f, a.castTime - PractisedDrawPerRank * draw));
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.BeginAutoAttack(); s.Bolt(t, new Color(.9f, .88f, .8f), .1f, true);
                float d = a.power + s.WeaponDamage;
                float ratio = t.actor.Health.Pool.Ratio;
                if (ratio > .7f) d *= 1 + .08f * R("mk-piercing");
                if (ratio < .35f) d *= 1 + .12f * R("mk-headshot");
                int first = Shot(d); Hit(t, first);
                if (R("mk-double-nock") > 0 && t.actor.IsAlive) Hit(t, Mathf.Max(1, first / 2));
                if (R("mk-snap-shot") > 0) freeQuick = true;
                AfterShot(t);
            });
        }
        bool BarbedArrow(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
s.Bolt(t, new Color(.9f, .88f, .8f), .1f, true); s.BeginAutoAttack(); Hit(t, Shot(a.power + s.WeaponDamage * .3f));
                int ticks = 4 + R("mk-bleeding-wounds");
                periodic.Add("bleed", t, Now, 2, ticks, Shot(3 + Level / 2f), (e, i) => { var enemy = (EncounterEnemy)e.target; if (!enemy.actor.IsAlive) return false; Hit(enemy, e.value); return true; });
                AfterShot(t);
            });
        }

        // ---------- Pathfinder ----------
        bool HuntersMark(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!t.actor.IsAlive) return;
                float seconds = MarkSeconds(a);
                markedUntil[t] = Now + seconds;
                if (R("pf-pathfinder") > 0) t.Slow(.15f, seconds);
                s.Message("Hunter's Mark: " + t.actor.DisplayName + " takes more from the party for " + seconds + " seconds.");
            });
        }
        /// <summary>How long Hunter's Mark holds (Lasting Mark: 3 s longer a rank).</summary>
        public float MarkSeconds(AbilityDefinition a) { return a.duration + LastingMarkPerRank * R("pf-lasting-mark"); }
        bool Snare(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            int tangle = R("pf-tangling-snare");
            return Start(a, () => { if (s.LandsOn(t, a.range)) { s.Bolt(t, new Color(.9f, .88f, .8f), .1f, true); t.Slow(.5f + .05f * tangle, a.duration + tangle); } });
        }
        bool Pin(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
s.Bolt(t, new Color(.9f, .88f, .8f), .1f, true); Hit(t, Shot(a.power + s.WeaponDamage * .3f)); t.Root(a.duration + R("pf-trapper")); t.Apply("silence", s.Player, 2);
                s.Message("Pin: the target is held where it stands.");
            });
        }
        bool Disengage(AbilityDefinition a)
        {
            int recovery = R("sk-quick-recovery");   // Quick Recovery: back sooner
            if (recovery > 0) a = With(a, a.cost, a.castTime, Mathf.Max(0, a.cooldown - QuickRecoveryPerRank * recovery));
            return Start(a, () => {
                if (R("sk-hit-and-run") > 0) freeLunge = true;   // Hit and Run: the next Spear Lunge costs nothing
                var player = s.Player.transform;
                var away = s.Target != null ? player.position - s.Target.transform.position : -player.forward; away.y = 0;
                if (away.sqrMagnitude < .01f) away = -player.forward;
                var landing = player.position + away.normalized * DisengageLeap;
                if (UnityEngine.AI.NavMesh.SamplePosition(landing, out var hit, 3, UnityEngine.AI.NavMesh.AllAreas)) landing = hit.position + Vector3.up * 1.05f; else landing.y = player.position.y;
                s.Player.GetComponent<AdventurerMotor>().Teleport(landing);
                int cover = R("pf-cover-of-leaves");
                if (cover > 0) PlayerCombat.AddBarrier(Mathf.RoundToInt(s.Player.Health.Pool.Max * .03f * cover), 6);
            });
        }

        // ---------- the Skirmisher (melee; 2026-10-11, TALENT_DEPTH.md: "spear and short blade, hit-and-run"; GAME-ONLY) ----------
        // Close in with the short spear, strike and get out again: the bow for the opener, the wolf for the pull, the spear for the kill.
        // No new weapon is held: the thrusts are the figure's own (ActorVisual.Thrust), the bow slung while it works close.
        readonly Dictionary<EncounterEnemy, float> feintedUntil = new Dictionary<EncounterEnemy, float>(), struckAt = new Dictionary<EncounterEnemy, float>();
        bool freeLunge;
        public const float LungeScale = 1.6f, WhirlScale = .9f, LungeStop = 1.4f, WhirlEdgePerRank = .1f, PackPerRank = .03f, PackWindow = 3, QuickRecoveryPerRank = 1.5f, WoundPerRank = .02f;
        /// <summary>The next blow this foe aims at you misses (Feint).</summary>
        public bool Feinted(EncounterEnemy e) { return e != null && feintedUntil.TryGetValue(e, out var until) && Now < until; }
        /// <summary>Pack Tactics: you struck it within 3 s and the wolf is on it.</summary>
        public bool Pack(EncounterEnemy e)
        { return e != null && R("sk-pack-tactics") > 0 && Wolf != null && Wolf.actor.IsAlive && Wolf.Quarry == e && struckAt.TryGetValue(e, out var at) && Now - at < PackWindow; }
        public bool FreeLunge { get { return freeLunge; } }
        public override bool OwnBlow(AbilityDefinition a) { return a != null && (a.id == "ranger.spear_lunge" || a.id == "ranger.whirling_spear" || a.id == "ranger.feint"); }
        int Spear(float amount) { return Mathf.Max(1, Mathf.RoundToInt(amount)); }
        bool Foe(EncounterEnemy e) { return e != null && e.actor.IsAlive && !e.Game && !e.Hidden; }
        bool FoeWithin(float reach)
        {
            if (s.Player == null) return false; var me = s.Player.transform.position;
            foreach (var e in s.Enemies) if (Foe(e) && (e.transform.position - me).sqrMagnitude < reach * reach) return true;
            return false;
        }
        /// <summary>Spear Lunge: a bound of up to 8 m (Long Haft: further) to a stride short of the target and a drive of the spear, 1.6
        /// times the weapon's hit; Bleeding Point opens a wound that bleeds 2% of the hit a second for 6 s per rank.</summary>
        bool SpearLunge(AbilityDefinition a)
        {
            float reach = a.range + R("sk-long-haft");
            if (!s.RequireEnemyInRange(reach)) return false; var t = s.Target;
            return Start(With(a, freeLunge ? 0 : a.cost, 0), () => {
                freeLunge = false;
                if (!s.LandsOn(t, reach)) return;
                var me = s.Player.transform; var toward = t.transform.position - me.position; toward.y = 0;
                if (toward.magnitude > LungeStop + .3f)
                {
                    var landing = t.transform.position - toward.normalized * LungeStop;
                    if (UnityEngine.AI.NavMesh.SamplePosition(landing, out var hit, 2, UnityEngine.AI.NavMesh.AllAreas)) { landing = hit.position + Vector3.up * 1.05f; s.Player.GetComponent<AdventurerMotor>().Teleport(landing); }
                }
                if (toward.sqrMagnitude > .01f) me.rotation = Quaternion.LookRotation(toward.normalized);
                s.PlayerFigure?.Thrust(); s.BeginAutoAttack();
                int dealt = Spear(a.power + s.WeaponDamage * LungeScale); Hit(t, dealt);
                int wound = R("sk-bleeding-point");
                if (wound > 0) periodic.Add("wound", t, Now, 1, 6, Mathf.Max(1, Mathf.RoundToInt(dealt * WoundPerRank * wound)), (e, i) => { var enemy = (EncounterEnemy)e.target; if (!enemy.actor.IsAlive) return false; enemy.Receive(e.value, s.Player); return true; });
                AfterShot(t);
            });
        }
        /// <summary>Whirling Spear: the haft swung round, every foe within 3.5 m taking 0.9 times the weapon's hit (Whirl Edge: harder).</summary>
        bool WhirlingSpear(AbilityDefinition a)
        {
            if (!Need(FoeWithin(a.range), "No enemy within " + a.range + " metres.")) return false;
            return Start(a, () => {
                s.PlayerFigure?.Thrust();
                var me = s.Player.transform.position; int dealt = Spear((a.power + s.WeaponDamage * WhirlScale) * (1 + WhirlEdgePerRank * R("sk-whirl-edge")));
                foreach (var e in s.Enemies.ToArray()) if (Foe(e) && (e.transform.position - me).sqrMagnitude <= a.range * a.range) Hit(e, dealt);
            });
        }
        /// <summary>Feint (a talent): a false thrust; the target's next blow at you within 3 s misses.</summary>
        bool Feint(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.PlayerFigure?.Thrust();
                feintedUntil[t] = Now + a.duration;
                s.FloatText(t.transform.position, "Feint", new Color(.85f, .9f, 1));
            });
        }
        /// <summary>Feint: the feinted foe's next blow at you misses.</summary>
        public override int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        {
            if (victim != null && victim == s.Player && Feinted(enemy))
            {
                feintedUntil.Remove(enemy); s.FloatText(s.Player.transform.position, "Miss", new Color(.85f, .9f, 1)); return 0;
            }
            int hide = R("bb-scarred-hide");   // Scarred Hide: the wolf takes less
            if (hide > 0 && victim != null && Wolf != null && victim == Wolf.actor) raw = Mathf.RoundToInt(raw * (1 - ScarredHidePerRank * hide));
            return base.ResolveEnemyHit(enemy, victim, raw);
        }

        // ---------- Beastbond ----------
        public float WolfHealthScale { get { return (1 + .08f * R("bb-thick-coat")) * (R("bb-alpha") > 0 ? 1.25f : 1); } }
        public float WolfBiteScale { get { return (1 + .1f * R("bb-sharp-teeth")) * (R("bb-alpha") > 0 ? 1.25f : 1); } }
        public bool WolfHowls { get { return R("bb-howl") > 0; } }
        public float PetHealthScale { get { return WolfHealthScale; } }
        public float PetBiteScale { get { return WolfBiteScale; } }
        public bool PetHolds { get { return WolfHowls; } }
        /// <summary>Shared Hunt: each of the wolf's bites gives you back 2 Focus.</summary>
        public void OnPetBite(EncounterEnemy target, int bite) { if (R("bb-shared-hunt") > 0 && s.Player != null) s.Player.Resource.Pool.Change(SharedHuntFocus); }
        /// <summary>Catch Your Breath: a foe of yours falling within 5 m gives back 5 Focus a rank.</summary>
        public override void OnFoeDown(EncounterEnemy enemy)
        {
            int breath = R("sk-second-wind");
            if (breath > 0 && enemy != null && s.Player != null && s.Player.IsAlive && Vector3.Distance(enemy.transform.position, s.Player.transform.position) <= BreathReach)
                s.Player.Resource.Pool.Change(BreathFocusPerRank * breath);
        }
        bool CallCompanion(AbilityDefinition a)
        {
            if (Wolf != null && Wolf.actor.IsAlive)
                return Start(With(a, 0, 0), () => { s.DismissPet(); s.Message("Your wolf slips away into the grass."); });
            return Start(a, () => { s.SummonWolf(this); s.Message("Your wolf comes to heel."); });
        }
        bool Sic(AbilityDefinition a)
        {
            if (!Need(Wolf != null && Wolf.actor.IsAlive, "No wolf at heel: Call Companion first.") || !s.RequireEnemyInRange(a.range)) return false;
            var t = s.Target;
            return Start(a, () => { if (t.actor.IsAlive) Wolf.Hunt(t, 1.5f + .25f * R("bb-sic-fury")); });
        }

        // ---------- per frame / lifecycle ----------
        public override void Tick(bool inCombat) { periodic.Tick(Now); TickTrap(); }
        GameObject trap; float trapUntil, trapHold;
        /// <summary>Snare Trap (CC_DESIGN section 2): set at your feet for a minute; the first mob to step in is held 20 s (damage breaks it).</summary>
        bool SnareTrap(AbilityDefinition a)
        {
            return Start(a, () => {
                if (trap != null) UnityEngine.Object.Destroy(trap);
                trap = JawTrap(GroundAtFeet(s.Player.transform), s.Player.transform.eulerAngles.y);
                trapUntil = Time.time + 60; trapHold = a.duration;
                s.Message("Snare Trap set: the first to step in is held " + a.duration + " seconds.");
            });
        }
        /// <summary>
        /// A hunter's jaw trap laid open on the ground (2026-10-14, Chris: "traps need to be something besides a circle on the ground"):
        /// two iron jaws in a ring 70 cm across, their teeth turned up, a round pan in the middle on its cross-bars, a spring at each
        /// hinge, and a short chain to a wooden stake driven in beside it. GAME-ONLY dressing; the trap's reach is TickTrap's.
        /// </summary>
        static GameObject JawTrap(Vector3 at, float yaw)
        {
            var root = new GameObject("Snare trap"); root.transform.position = at + Vector3.up * .01f; root.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var iron = new Material(Shader.Find("Standard")) { color = new Color(.2f, .2f, .22f) }; iron.SetFloat("_Metallic", .6f); iron.SetFloat("_Glossiness", .35f);
            var rust = new Material(Shader.Find("Standard")) { color = new Color(.38f, .24f, .14f) }; rust.SetFloat("_Glossiness", .15f);
            var wood = new Material(Shader.Find("Standard")) { color = new Color(.36f, .26f, .16f) }; wood.SetFloat("_Glossiness", .1f);
            GameObject Bit(PrimitiveType type, Transform parent, Vector3 pos, Vector3 size, Material m, Quaternion? rot = null)
            {
                var o = GameObject.CreatePrimitive(type); UnityEngine.Object.Destroy(o.GetComponent<Collider>());
                o.transform.SetParent(parent, false); o.transform.localPosition = pos; o.transform.localScale = size; if (rot.HasValue) o.transform.localRotation = rot.Value;
                var r = o.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; return o;
            }
            // The cross-bars and the pan.
            Bit(PrimitiveType.Cube, root.transform, new Vector3(0, .015f, 0), new Vector3(.86f, .025f, .07f), iron);
            Bit(PrimitiveType.Cube, root.transform, new Vector3(0, .015f, 0), new Vector3(.07f, .025f, .5f), iron);
            Bit(PrimitiveType.Cylinder, root.transform, new Vector3(0, .035f, 0), new Vector3(.2f, .012f, .2f), rust);
            // Each jaw: a half ring of short bars on its hinge (at the ends of the long bar), its teeth up; laid open flat.
            var jaws = new Transform[2];
            for (int j = 0; j < 2; j++)
            {
                var hinge = new GameObject("Jaw " + j).transform; hinge.SetParent(root.transform, false); hinge.localPosition = new Vector3(0, .03f, 0);
                jaws[j] = hinge; float side = j == 0 ? 1 : -1;
                for (int k = 0; k <= 8; k++)
                {
                    float a = Mathf.PI * k / 8; var p = new Vector3(Mathf.Cos(a) * .36f, 0, side * Mathf.Sin(a) * .36f);
                    float tangent = Mathf.Atan2(side * Mathf.Cos(a), -Mathf.Sin(a)) * Mathf.Rad2Deg;
                    Bit(PrimitiveType.Cube, hinge, p, new Vector3(.03f, .03f, .15f), iron, Quaternion.Euler(0, tangent, 0));
                    if (k > 0 && k < 8) Bit(PrimitiveType.Cube, hinge, p * .93f + Vector3.up * .035f, new Vector3(.02f, .06f, .025f), iron, Quaternion.Euler(0, tangent, side * 12));   // a tooth
                }
                Bit(PrimitiveType.Cylinder, root.transform, new Vector3(side * .44f, .03f, 0), new Vector3(.06f, .07f, .06f), rust, Quaternion.Euler(0, 0, 90));   // the spring at the hinge
            }
            // The chain to its stake.
            for (int k = 0; k < 5; k++) Bit(PrimitiveType.Cube, root.transform, new Vector3(.5f + k * .07f, .012f, -.06f - k * .025f), new Vector3(.06f, .015f, .03f), iron, Quaternion.Euler(0, 20 + (k % 2) * 50, 0));
            Bit(PrimitiveType.Cylinder, root.transform, new Vector3(.9f, .1f, -.2f), new Vector3(.06f, .14f, .06f), wood, Quaternion.Euler(6, 0, -8));
            root.AddComponent<TrapJaws>().jaws = jaws;
            return root;
        }
        /// <summary>The ground under someone's feet: down from the bottom of their capsule to the first thing that is not a person.
        /// (Playtest note 123, Chris, 2026-10-11: "trap set up in the air". It was set at the player's middle, a metre up: the
        /// player's capsule is two metres tall and centred on its position.)</summary>
        public static Vector3 GroundAtFeet(Transform who)
        {
            var cc = who.GetComponent<CharacterController>();
            var feet = cc != null ? new Vector3(who.position.x, cc.bounds.min.y, who.position.z) : who.position;
            RaycastHit best = default; bool found = false;
            foreach (var hit in Physics.RaycastAll(feet + Vector3.up * .5f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponentInParent<Actor>() == null && (!found || hit.distance < best.distance)) { best = hit; found = true; }
            return found ? best.point : feet;
        }
        /// <summary>The trap's teeth: the first living mob within 1.4 m of it.</summary>
        public bool TrapSet { get { return trap != null; } }
        void TickTrap()
        {
            if (trap == null) return;
            if (Time.time > trapUntil) { UnityEngine.Object.Destroy(trap); trap = null; return; }
            foreach (var e in s.Enemies)
            {
                if (e == null || !e.actor.IsAlive || e.Game || e.Hidden) continue;
                var d = e.transform.position - trap.transform.position; d.y = 0; if (d.magnitude > 1.4f) continue;
                string why = e.Apply("incap", s.Player, trapHold);
                s.Message(why == null ? "Snare Trap: " + e.actor.DisplayName + " is caught." : e.actor.DisplayName + " springs the trap: " + why + ".");
                // The jaws snap shut round it and the sprung trap lies there a while (2026-10-14).
                var sprung = trap; trap = null; var snap = sprung.GetComponent<TrapJaws>(); if (snap != null) snap.Spring(e.transform);
                UnityEngine.Object.Destroy(sprung, why == null ? trapHold + 1 : 2); return;
            }
        }
        public override void ResetState()
        {
            resetting = true; periodic.Clear(); resetting = false;
            markedUntil.Clear(); freeQuick = false; feintedUntil.Clear(); struckAt.Clear(); freeLunge = false;
        }
        public override void ClearTransient()
        {
            resetting = true; periodic.Clear(); resetting = false;
            markedUntil.Clear(); freeQuick = false; feintedUntil.Clear(); struckAt.Clear(); freeLunge = false;
            if (s.Player != null) PlayerCombat.ClearBarrier();
            if (Wolf != null) Wolf.Refit();
        }

        // ---------- HUD ----------
        public override string StatusLine
        {
            get
            {
                var parts = new List<string>();
                parts.Add(Wolf != null && Wolf.actor.IsAlive ? "Wolf " + Wolf.actor.Health.Pool.Current + "/" + Wolf.actor.Health.Pool.Max + (Wolf.Quarry != null ? " hunting" : " at heel") : "No wolf");
                if (freeQuick) parts.Add("free Quick Shot");
                if (freeLunge) parts.Add("free Spear Lunge");
                int barrier = PlayerCombat.Barrier; if (barrier > 0) parts.Add("Barrier " + barrier);
                return string.Join("  ·  ", parts);
            }
        }
        public override string TargetStatus(EncounterEnemy enemy)
        {
            var parts = new List<string>();
            if (Marked(enemy)) parts.Add("MARKED");
            if (enemy.Rooted) parts.Add("PINNED " + enemy.RootRemaining.ToString("0.0") + "s");
            else if (enemy.Slowed) parts.Add("SNARED");
            if (periodic.Has("bleed", enemy)) parts.Add("Bleeding");
            if (periodic.Has("wound", enemy)) parts.Add("Wounded");
            if (Feinted(enemy)) parts.Add("FEINTED");
            return parts.Count == 0 ? null : string.Join("  ·  ", parts);
        }
    }

    /// <summary>The jaw trap's two jaws, laid open; sprung, they swing up about the long bar and close (a quarter second).</summary>
    public sealed class TrapJaws : MonoBehaviour
    {
        public Transform[] jaws; float sprungAt = -1; Transform held;
        public void Spring(Transform on) { sprungAt = Time.time; held = on; }
        void Update()
        {
            if (sprungAt < 0 || jaws == null) return;
            float u = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.time - sprungAt) / .25f));
            for (int j = 0; j < jaws.Length; j++) if (jaws[j] != null) jaws[j].localRotation = Quaternion.Euler((j == 0 ? -1 : 1) * 82 * u, 0, 0);
        }
    }
}
