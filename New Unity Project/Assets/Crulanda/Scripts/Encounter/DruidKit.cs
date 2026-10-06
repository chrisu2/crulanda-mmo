using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.Abilities;

namespace Crulanda.Encounter
{
    public enum DruidForm { Barkhide = 0, Thornclaw = 1, Rootmend = 2, Thornsong = 3 }

    /// <summary>
    /// Druid: four forms (tank / melee / healer / ranged). Shift Breath (the actor resource) pays for changing form;
    /// each form has its own pool filled only by that form's actions, and shifting never refills a pool.
    /// Bar: 1-4 shift, 5-8 current form's actions (8 = the form's talent action), 9 Swiftroot, 0 Stillroot.
    /// Heals use a smart target (lowest-health living party member in range) until friendly targeting exists.
    /// Base abilities and rows 0-2 of EncounterContent/Talents/druid.json. Nothing here is saved.
    /// </summary>
    public sealed class DruidKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "bh-ringed-hide", "bh-rooted-stance", "bh-mosscoat", "bh-heartwood-brace", "bh-thorned-bark", "bh-sapwood-core", "bh-lodged-roots",
            "tc-chain-pounce", "tc-keen-claws", "tc-scent-of-blood", "tc-rending-flurry", "tc-quickpad", "tc-bramble-feint", "tc-feral-cadence",
            "rm-slow-sap", "rm-pre-bloom", "rm-dew-cleanse", "rm-burst-bloom", "rm-canopy", "rm-grafting", "rm-sap-thrift",
            "ts-germinate", "ts-green-voice", "ts-pollen-veil", "ts-bramblestorm", "ts-stillroot-cast", "ts-cadence-thread", "ts-thornbound-channel"
        };
        static readonly string[] ShiftIds = { "druid.barkhide", "druid.thornclaw", "druid.rootmend", "druid.thornsong" };
        static readonly string[][] FormIds = {
            new[] { "druid.bough_strike", "druid.bellowing_roar", "druid.barkmend", "druid.heartwood_brace" },
            new[] { "druid.rake", "druid.lunge", "druid.tear", "druid.rending_flurry" },
            new[] { "druid.seedling", "druid.quickbloom", "druid.verdant_ward", "druid.burst_bloom" },
            new[] { "druid.seedshot", "druid.thornbolt", "druid.briar_snare", "druid.bramblestorm" }
        };
        static readonly string[] UtilityIds = { "druid.swiftroot", "druid.stillroot" };
        public const int PoolCap = 100, MaxFang = 5, ShiftCost = 25, HealRange = 25;
        static readonly Color[] FormTint = { new Color(.45f, .33f, .2f), new Color(.78f, .55f, .2f), new Color(.35f, .68f, .38f), new Color(.3f, .55f, .6f) };
        /// <summary>How a form changes the figure (the wardrobe line-up shows the same): its build, as the Body's scale.</summary>
        public static Vector3 FormScale(DruidForm f) { return f == DruidForm.Barkhide ? new Vector3(1.3f, 1.1f, 1.3f) : f == DruidForm.Thornclaw ? new Vector3(1.1f, .8f, 1.3f) : Vector3.one; }
        /// <summary>The colour a form gives the Druid's own cloth (whatever armour does not cover).</summary>
        public static Color FormColor(DruidForm f) { return FormTint[(int)f]; }

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        readonly PeriodicEffects periodic = new PeriodicEffects();
        readonly object formSource = new object();
        readonly Dictionary<EncounterEnemy, float> veiledUntil = new Dictionary<EncounterEnemy, float>();
        readonly Dictionary<EncounterEnemy, float> roaredUntil = new Dictionary<EncounterEnemy, float>();
        readonly HashSet<EncounterEnemy> seeded = new HashSet<EncounterEnemy>();
        bool resetting;
        float lastMovedAt, fangKeptUntil, lungeAt = -99, feintReady, braceUntil, bracePrevented, nextDecay;
        int rakes, alternationStreak;
        string lastAlternation;
        Vector3 stormCenter;

        public DruidForm Form { get; private set; } = DruidForm.Thornsong;
        public int Bark { get; private set; }
        public int Fang { get; private set; }
        public int Sap { get; private set; }
        public int Glimmer { get; private set; }
        public int CadenceCharges { get; private set; }
        public PeriodicEffects Periodic { get { return periodic; } }

        public DruidKit(EncounterSession session, ClassDefinition definition, IEnumerable<AbilityDefinition> catalog) : base(session)
        {
            if (definition == null || definition.unlocks == null || catalog == null) throw new ArgumentException("Druid content missing.");
            foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !abilities.ContainsKey(a.id)) abilities.Add(a.id, a);
            foreach (var u in definition.unlocks)
                if (u == null || string.IsNullOrEmpty(u.abilityId) || u.level < 1 || u.level > 10 || unlocks.ContainsKey(u.abilityId))
                    throw new ArgumentException("Invalid or duplicate Druid unlock.");
                else unlocks.Add(u.abilityId, u);
            var all = new List<string>(ShiftIds); foreach (var f in FormIds) all.AddRange(f); all.AddRange(UtilityIds);
            foreach (var id in all)
                if (!abilities.ContainsKey(id) || !unlocks.ContainsKey(id)) throw new ArgumentException("Druid ability '" + id + "' is missing from content or unlocks.");
        }
        public override string[] ImplementedTalents { get { return ImplementedIds; } }
        int R(string id) { return s.TalentRank(id); }
        float Now { get { return Time.time; } }
        int Level { get { return s.Player.Level; } }
        Combatant PlayerCombat { get { return s.Player.GetComponent<Combatant>(); } }
        int Clamp(int v, int max) { return Mathf.Clamp(v, 0, max); }
        public void GainBark(int n) { Bark = Clamp(Bark + n, PoolCap); }
        public void GainFang(int n) { if (n <= 0) return; Fang = Clamp(Fang + n, MaxFang); fangKeptUntil = Now + 8; }
        public void GainSap(int n) { Sap = Clamp(Sap + n, PoolCap); }
        public void GainGlimmer(int n) { Glimmer = Clamp(Glimmer + n, PoolCap); }

        // ---------- action bar ----------
        public override int ActionCount { get { return 10; } }
        public override AbilityDefinition ActionAt(int slot)
        {
            string id = slot < 4 ? ShiftIds[slot] : slot < 8 ? FormIds[(int)Form][slot - 4] : slot < 10 ? UtilityIds[slot - 8] : null;
            return id != null && abilities.TryGetValue(id, out var a) ? a : null;
        }
        public override string ActionLockLabel(int slot)
        {
            var a = ActionAt(slot); if (a == null || s.Player == null) return "Unavailable";
            var u = unlocks[a.id];
            if (Level < u.level) return "Level " + u.level;
            if (!string.IsNullOrEmpty(u.talentId) && R(u.talentId) == 0) return "Talent";
            if (slot < 4 && (int)Form == slot) return "Active";
            if (a.id == "druid.swiftroot" && Form != DruidForm.Rootmend && R("rm-dew-cleanse") == 0) return "Rootmend";
            if (a.id == "druid.stillroot" && Form != DruidForm.Thornsong && R("ts-stillroot-cast") == 0) return "Thornsong";
            return null;
        }
        public override bool Use(int slot)
        {
            var a = ActionAt(slot);
            if (slot < 4) return Shift((DruidForm)slot, a);
            switch (a.id)
            {
                case "druid.bough_strike": return BoughStrike(a);
                case "druid.bellowing_roar": return Roar(a);
                case "druid.barkmend": return Barkmend(a);
                case "druid.heartwood_brace": return Brace(a);
                case "druid.rake": return Rake(a);
                case "druid.lunge": return Lunge(a);
                case "druid.tear": return Tear(a);
                case "druid.rending_flurry": return Flurry(a);
                case "druid.seedling": return Seedling(a);
                case "druid.quickbloom": return Quickbloom(a);
                case "druid.verdant_ward": return Ward(a);
                case "druid.burst_bloom": return BurstBloom(a);
                case "druid.seedshot": return Seedshot(a);
                case "druid.thornbolt": return Thornbolt(a);
                case "druid.briar_snare": return BriarSnare(a);
                case "druid.bramblestorm": return Bramblestorm(a);
                case "druid.swiftroot": return Swiftroot(a);
                case "druid.stillroot": return Stillroot(a);
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

        // ---------- forms ----------
        public int ShiftCostFor(DruidForm to)
        { return to == DruidForm.Barkhide ? Mathf.RoundToInt(ShiftCost * (1 - .1f * R("bh-mosscoat"))) : ShiftCost; }
        bool Shift(DruidForm to, AbilityDefinition a)
        {
            return Start(With(a, ShiftCostFor(to), 0), () => {
                periodic.RemoveKey("flurry"); periodic.RemoveKey("bramblestorm");
                Form = to; ApplyStats();
                if (!MeleeAutoAttacks) s.StopAutoAttack();
                if (to == DruidForm.Barkhide && R("bh-mosscoat") > 0)
                    PlayerCombat.AddBarrier(Mathf.RoundToInt(s.Player.Health.Pool.Max * .03f * R("bh-mosscoat")), 8);
                s.Message("Shifted into " + to + ".");
            });
        }
        public override bool MeleeAutoAttacks { get { return Form == DruidForm.Barkhide || Form == DruidForm.Thornclaw; } }
        public override float MoveSpeedMultiplier
        {
            get
            {
                float quick = .05f * R("tc-quickpad");
                return Form == DruidForm.Barkhide ? .9f + quick : Form == DruidForm.Thornclaw ? 1.1f + quick : 1;
            }
        }
        public override float SwingInterval(float baseInterval) { return Form == DruidForm.Thornclaw ? baseInterval * .8f : baseInterval; }
        public override void ApplyStats()
        {
            s.Player.Stats.RemoveModifiersFromSource(formSource);
            var mods = new List<StatModifier>();
            if (Form == DruidForm.Barkhide)
            {
                mods.Add(new StatModifier(StatType.Armor, ModifierOp.Flat, 30, formSource));
                mods.Add(new StatModifier(StatType.Armor, ModifierOp.PercentAdd, .02f * R("bh-ringed-hide"), formSource));
                mods.Add(new StatModifier(StatType.MaxHealth, ModifierOp.PercentAdd, .2f, formSource));
            }
            if (Form == DruidForm.Thornclaw) mods.Add(new StatModifier(StatType.AttackPower, ModifierOp.Flat, 6, formSource));
            if (mods.Count > 0) s.Player.Stats.AddModifiers(mods);
            var body = s.Player.transform.Find("Body");
            if (body != null)
            {
                body.localScale = FormScale(Form);
                var visual = s.Player.GetComponent<ActorVisual>(); if (visual != null) visual.SetClothColor(FormColor(Form));
            }
        }
        float FormDamage { get { return Form == DruidForm.Thornclaw ? 1 + .03f * R("tc-keen-claws") : Form == DruidForm.Thornsong ? 1 + .02f * R("ts-green-voice") : 1; } }
        int Dmg(float amount) { return Mathf.Max(1, Mathf.RoundToInt(amount * FormDamage)); }

        // ---------- Barkhide ----------
        bool BoughStrike(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.BeginAutoAttack(); int d = Dmg(a.power + s.WeaponDamage * .5f);
                t.Receive(d, s.Player); t.threat.Add(s.Player.EntityId.Value, d); GainBark(12);
                int extra = R("bh-rooted-stance");
                if (extra > 0 && Now - lastMovedAt >= 2)
                    foreach (var o in s.Enemies)
                    {
                        if (extra <= 0) break;
                        if (o == t || !o.actor.IsAlive || Vector3.Distance(o.transform.position, t.transform.position) > 4) continue;
                        o.Receive(Mathf.Max(1, d / 2), s.Player); o.threat.Add(s.Player.EntityId.Value, d / 2); extra--;
                    }
            });
        }
        bool Roar(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                t.threat.Taunt(s.Player.EntityId.Value, Now, a.duration);
                foreach (var o in s.Enemies)
                    if (o.actor.IsAlive && s.Distance(o) <= 8) o.threat.Add(s.Player.EntityId.Value, 20);
                if (R("bh-lodged-roots") > 0) { t.Slow(.2f * R("bh-lodged-roots"), 4); roaredUntil[t] = Now + 4; }
                s.Message("Bellowing Roar: attention forced for " + a.duration + " seconds.");
            });
        }
        bool Barkmend(AbilityDefinition a)
        {
            if (!Need(Bark >= 30, "Barkmend needs 30 Bark.")) return false;
            return Start(a, () => {
                if (Bark < 30) return; Bark -= 30;
                int healed = PlayerCombat.Heal(Mathf.RoundToInt(s.Player.Health.Pool.Max * .12f));
                s.AddHealingThreat(healed); s.FloatText(s.Player.transform.position, "+" + healed, new Color(.3f, 1, .7f));
            });
        }
        bool Brace(AbilityDefinition a)
        {
            if (!Need(Bark >= 40, "Heartwood Brace needs 40 Bark.")) return false;
            return Start(a, () => {
                if (Bark < 40) return; Bark -= 40;
                PlayerCombat.Apply(new StatusEffectDefinition { id = "status.heartwood_brace", name = "Heartwood Brace", duration = 6, incomingDamageMultiplier = .65f });
                braceUntil = Now + 6; bracePrevented = 0;
                s.Message("Heartwood Brace: damage taken reduced for 6 seconds.");
            });
        }
        void EndBrace()
        {
            braceUntil = 0; int pct = R("bh-sapwood-core"); int total = Mathf.RoundToInt(bracePrevented * .1f * pct); bracePrevented = 0;
            if (pct <= 0 || total <= 0) return;
            int per = Mathf.Max(1, total / 4);
            periodic.Add("sapwood", s.Player, Now, 2, 4, per, (e, i) => { HealActor(s.Player, e.value); return true; });
        }

        // ---------- Thornclaw ----------
        bool Rake(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.BeginAutoAttack(); t.Receive(Dmg(a.power + s.WeaponDamage * .6f), s.Player);
                bool low = t.actor.Health.Pool.Ratio < .35f;
                int scent = R("tc-scent-of-blood");
                int bleed = Dmg((3 + Level / 2f) * (low ? 1 + .1f * scent : 1));
                periodic.Add("bleed", t, Now, 2, 3, bleed, (e, i) => { var enemy = (EncounterEnemy)e.target; if (!enemy.actor.IsAlive) return false; enemy.Receive(e.value, s.Player); return true; });
                rakes++;
                GainFang(1 + (low && scent >= 2 ? 1 : 0) + (R("tc-keen-claws") >= 5 && rakes % 3 == 0 ? 1 : 0));
            });
        }
        bool Lunge(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                var from = s.Player.transform.position; var offset = from - t.transform.position; offset.y = 0;
                var landing = t.transform.position + (offset.sqrMagnitude > .01f ? offset.normalized : Vector3.back) * 1.6f; landing.y = from.y;
                s.Player.GetComponent<AdventurerMotor>().Teleport(landing);
                lungeAt = Now; GainFang(1); s.BeginAutoAttack();
            });
        }
        int FinisherFang { get { return Mathf.Min(MaxFang, Fang + (Now - lungeAt <= 3 ? R("tc-chain-pounce") : 0)); } }
        bool Tear(AbilityDefinition a)
        {
            if (!Need(Fang >= 1, "Tear needs Fang; Rake or Lunge first.") || !s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range) || Fang < 1) return;
                int f = FinisherFang; Fang = 0; lungeAt = -99;
                t.Receive(Dmg(a.power * f + s.WeaponDamage * .5f), s.Player);
            });
        }
        bool Flurry(AbilityDefinition a)
        {
            if (!Need(FinisherFang >= 3, "Rending Flurry needs 3 Fang.") || !s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range) || FinisherFang < 3) return;
                int f = FinisherFang; Fang = 0; lungeAt = -99; int strikes = 2 * f - 2;
                int each = Dmg(a.power + s.WeaponDamage * .3f);
                periodic.Add("flurry", t, Now, 2f / strikes, strikes, each, (e, i) => {
                    var enemy = (EncounterEnemy)e.target; if (!enemy.actor.IsAlive) return false; enemy.Receive(e.value, s.Player); return true;
                }, (e, completed) => {
                    var enemy = (EncounterEnemy)e.target;
                    if (!resetting && completed && enemy.actor.IsAlive && R("tc-feral-cadence") > 0) GainFang(R("tc-feral-cadence"));
                }, true);
            });
        }

        // ---------- Rootmend ----------
        /// <summary>Lowest-health living party member within heal range (you or recruited Mira).</summary>
        public Actor HealTarget
        {
            get
            {
                Actor best = s.Player;
                if (s.Progress.recruited && s.Companion != null && s.Companion.actor.IsAlive &&
                    Vector3.Distance(s.Player.transform.position, s.Companion.transform.position) <= HealRange &&
                    s.Companion.actor.Health.Pool.Ratio < s.Player.Health.Pool.Ratio) best = s.Companion.actor;
                return best;
            }
        }
        int HealActor(Actor target, int amount)
        {
            if (target == null || !target.IsAlive || amount <= 0) return 0;
            int healed = target.GetComponent<Combatant>().Heal(amount);
            if (healed > 0) { s.AddHealingThreat(healed); s.FloatText(target.transform.position, "+" + healed, new Color(.3f, 1, .7f)); }
            return healed;
        }
        public int SeedlingTick { get { return Mathf.RoundToInt((6 + Level) * (1 + .04f * R("rm-slow-sap"))); } }
        void PlantSeedling(Actor target, int ticks)
        {
            periodic.Add("seedling", target, Now, 2, ticks, SeedlingTick, (e, i) => {
                var who = (Actor)e.target; if (!who.IsAlive) return false;
                int prebloom = R("rm-pre-bloom"); int stored = e.data is int n ? n : 0;
                if (prebloom > 0 && who.Health.Pool.Ratio >= 1 && stored < prebloom) { e.data = stored + 1; GainSap(4); return true; }
                int healed = HealActor(who, e.value);
                GainSap(4 + Mathf.RoundToInt((e.value - healed) * .1f * R("rm-sap-thrift")));
                return true;
            });
        }
        bool Seedling(AbilityDefinition a)
        {
            var target = HealTarget;
            return Start(a, () => { PlantSeedling(target, 5); s.Message("Seedling planted on " + (target == s.Player ? "you" : "Mira") + "."); });
        }
        bool Quickbloom(AbilityDefinition a)
        {
            if (!Need(Sap >= 25, "Quickbloom needs 25 Sap; Seedling ticks build Sap.")) return false;
            Sap -= 25; // like other costs, committed at cast start and not refunded if the cast is interrupted
            return Start(a, () => HealActor(HealTarget, a.power + 2 * Level)) || Refund(25);
        }
        bool Refund(int sap) { Sap = Clamp(Sap + sap, PoolCap); return false; }
        bool Ward(AbilityDefinition a)
        {
            var target = HealTarget;
            return Start(a, () => { if (target.IsAlive) target.GetComponent<Combatant>().AddBarrier(a.power + Level, 8); });
        }
        bool BurstBloom(AbilityDefinition a)
        {
            int cost = 30 + (R("rm-grafting") > 0 ? 5 : 0); var target = HealTarget; var seed = periodic.Find("seedling", target);
            if (!Need(seed != null, "Burst Bloom needs a Seedling on your heal target.") || !Need(Sap >= cost, "Burst Bloom needs " + cost + " Sap.")) return false;
            return Start(a, () => {
                seed = periodic.Find("seedling", target); if (seed == null || Sap < cost) return;
                Sap -= cost; int heal = Mathf.RoundToInt(seed.ticksLeft * seed.value * 1.25f);
                periodic.Remove("seedling", target); HealActor(target, heal);
                int graft = R("rm-grafting"); if (graft > 0) PlantSeedling(target, Mathf.CeilToInt((1 + 2 * graft) / 2f));
            });
        }
        bool Swiftroot(AbilityDefinition a)
        {
            int cost = Form == DruidForm.Rootmend ? 0 : 20 - 5 * R("rm-dew-cleanse");
            return Start(With(a, cost, 0), () => HealActor(HealTarget, a.power + Level));
        }

        // ---------- Thornsong ----------
        void Alternate(string kind)
        {
            if (lastAlternation != null && lastAlternation != kind)
            {
                int green = R("ts-green-voice");
                GainGlimmer(10 + (green >= 3 ? 5 : 0) + (green >= 5 ? 5 : 0));
                alternationStreak++;
                if (R("ts-cadence-thread") > 0 && alternationStreak % 3 == 0) CadenceCharges = Mathf.Min(R("ts-cadence-thread"), CadenceCharges + 1);
            }
            else alternationStreak = 0;
            lastAlternation = kind;
        }
        bool Seedshot(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                s.Bolt(t, new Color(.55f, .9f, .3f), .15f); t.Receive(Dmg(a.power + Level), s.Player); Alternate("seedshot");
                if (R("ts-pollen-veil") > 0) veiledUntil[t] = Now + 6;
                if (R("ts-germinate") > 0) seeded.Add(t);
                else periodic.Add("thornrot", t, Now, 2, 2, Dmg(3 + Level / 3f), (e, i) => {
                    var enemy = (EncounterEnemy)e.target; if (!enemy.actor.IsAlive) return false; enemy.Receive(e.value, s.Player); return true; });
            });
        }
        bool Thornbolt(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            bool instant = CadenceCharges > 0;
            var def = instant ? With(a, a.cost, 0) : a;
            return Start(def, () => {
                if (instant) CadenceCharges = Mathf.Max(0, CadenceCharges - 1);
                if (!s.LandsOn(t, a.range)) return;
                float bonus = seeded.Remove(t) ? 1 + .15f * R("ts-germinate") : 1;
                s.Bolt(t, new Color(.4f, .8f, .25f), .2f); t.Receive(Dmg((a.power + 2 * Level) * bonus), s.Player); Alternate("thornbolt");
            });
        }
        bool BriarSnare(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => { if (s.LandsOn(t, a.range)) t.Slow(.4f, a.duration); });
        }
        bool Stillroot(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, 0, 0), () => { if (s.LandsOn(t, a.range)) { t.Root(a.duration + R("ts-stillroot-cast")); s.Message("Stillroot holds the target in place."); } });
        }
        bool Bramblestorm(AbilityDefinition a)
        {
            int thornbound = R("ts-thornbound-channel"); int min = 30 + (thornbound > 0 ? 10 : 0);
            if (!Need(Glimmer >= min, "Bramblestorm needs " + min + " Glimmer; alternate Seedshot and Thornbolt.") || !s.RequireEnemyInRange(a.range)) return false;
            var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range) || Glimmer < min) return;
                int spent = Glimmer; Glimmer = 0; stormCenter = t.transform.position;
                int per = Dmg(spent * .8f / 6);
                var motor = s.Player.GetComponent<AdventurerMotor>();
                periodic.Add("bramblestorm", s.Player, Now, .5f, 6, per, (e, i) => {
                    bool moving = motor.Moving;
                    if (moving && thornbound == 0) { s.Message("Bramblestorm broken by movement."); return false; }
                    int d = moving ? Mathf.Max(1, Mathf.RoundToInt(e.value * (1 - (.55f - .15f * thornbound)))) : e.value;
                    foreach (var o in s.Enemies) if (o.actor.IsAlive && Vector3.Distance(o.transform.position, stormCenter) <= 5) o.Receive(d, s.Player);
                    return true;
                });
            });
        }

        // ---------- incoming blows ----------
        public override int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        {
            if (victim == null || !victim.IsAlive) return 0;
            float mult = 1;
            int canopy = R("rm-canopy"); if (canopy > 0 && periodic.Has("seedling", victim)) mult -= .03f * canopy;
            if (victim != s.Player && roaredUntil.TryGetValue(enemy, out var roared) && Now < roared) mult -= .05f;
            if (victim == s.Player && veiledUntil.TryGetValue(enemy, out var veiled) && Now < veiled) mult -= .03f * R("ts-pollen-veil");
            int dealt = victim.GetComponent<Combatant>().Damage(Mathf.RoundToInt(raw * mult));
            if (victim != s.Player) return dealt;
            if (Now < braceUntil) bracePrevented += dealt * .35f / .65f;
            if (Form == DruidForm.Barkhide)
            {
                GainBark(5 + R("bh-ringed-hide"));
                int thorns = R("bh-thorned-bark");
                if (thorns > 0)
                {
                    int reflect = Mathf.Max(1, Mathf.RoundToInt(3 * thorns + .05f * thorns * s.Player.Stats.Get(StatType.Armor)));
                    enemy.Receive(reflect, s.Player); enemy.threat.Add(s.Player.EntityId.Value, reflect);
                }
            }
            if (Form == DruidForm.Thornclaw && R("tc-bramble-feint") > 0 && Now >= feintReady)
            { GainFang(1); feintReady = Now + (R("tc-bramble-feint") >= 2 ? 4 : 6); }
            return dealt;
        }

        // ---------- per frame / lifecycle ----------
        public override void Tick(bool inCombat)
        {
            if (s.Player.GetComponent<AdventurerMotor>().Moving) lastMovedAt = Now;
            periodic.Tick(Now);
            if (braceUntil > 0 && Now >= braceUntil) EndBrace();
            if (R("rm-pre-bloom") > 0)
                foreach (var who in new[] { s.Player, s.Companion != null ? s.Companion.actor : null })
                {
                    var seed = who == null ? null : periodic.Find("seedling", who);
                    if (seed != null && seed.data is int stored && stored > 0 && who.IsAlive && who.Health.Pool.Ratio < .6f)
                    { seed.data = 0; HealActor(who, stored * seed.value); }
                }
            if (!inCombat)
            {
                // Out of combat, battle pools drain 5 per second (time-based, not per frame). Sap is kept.
                if (Now >= nextDecay) { nextDecay = Now + 1; Bark = Clamp(Bark - 5, PoolCap); Glimmer = Clamp(Glimmer - 5, PoolCap); }
                if (Fang > 0 && Now > fangKeptUntil) Fang = 0;
            }
        }
        public override void ResetState()
        {
            resetting = true;
            periodic.Clear(); veiledUntil.Clear(); roaredUntil.Clear(); seeded.Clear();
            resetting = false;
            Form = DruidForm.Thornsong; Bark = Fang = Sap = Glimmer = CadenceCharges = 0; rakes = alternationStreak = 0; lastAlternation = null;
            lungeAt = -99; feintReady = braceUntil = bracePrevented = 0; lastMovedAt = Time.time;
        }
        public override void ClearTransient()
        {
            // Respec keeps the current form and pools (they are resources), but drops every talent-driven effect.
            resetting = true; periodic.Clear(); resetting = false;
            veiledUntil.Clear(); roaredUntil.Clear(); seeded.Clear(); CadenceCharges = 0; braceUntil = bracePrevented = 0;
            if (s.Player != null) { PlayerCombat.Statuses.Remove("status.heartwood_brace"); PlayerCombat.ClearBarrier(); }
            if (s.Companion != null) s.Companion.actor.GetComponent<Combatant>().ClearBarrier();
        }

        // ---------- HUD ----------
        public override string StatusLine
        {
            get
            {
                string pool = Form == DruidForm.Barkhide ? "Bark " + Bark + "/" + PoolCap : Form == DruidForm.Thornclaw ? "Fang " + EncounterHud.Pips(Fang, MaxFang) :
                    Form == DruidForm.Rootmend ? "Sap " + Sap + "/" + PoolCap : "Glimmer " + Glimmer + "/" + PoolCap + (CadenceCharges > 0 ? "  ·  instant Thornbolt" : "");
                int barrier = PlayerCombat.Barrier;
                return Form + "  ·  " + pool + (barrier > 0 ? "  ·  Barrier " + barrier : "");
            }
        }
        public override string TargetStatus(EncounterEnemy enemy)
        {
            var parts = new List<string>();
            if (enemy.Rooted) parts.Add("ROOTED " + enemy.RootRemaining.ToString("0.0") + "s");
            else if (enemy.Slowed) parts.Add("SLOWED");
            if (periodic.Has("bleed", enemy)) parts.Add("Bleeding");
            if (seeded.Contains(enemy)) parts.Add("Seeded");
            return parts.Count == 0 ? null : string.Join("  ·  ", parts);
        }
    }
}
