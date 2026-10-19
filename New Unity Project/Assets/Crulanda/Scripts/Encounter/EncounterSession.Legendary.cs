using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Combat;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Unraveller's loot and the legendary weapons in play (WORLD_BOSS_1.md section 4; build steps 7 and 8; the rules are Legendary.cs').
    /// - Her body (RollCorpse): coins, two of her eight epics (need and greed among the raid, as any uncommon or better) and your own
    ///   components (UnravellerLoot.Roll on your counters; never rolled for: PartyLoot.Rolled). Each sim of the raid rolls its own on its own
    ///   counters (SimAdventurer.components, the world slot). Locked out for her week, her body gives you nothing (Lockouts).
    /// - Tynara Embercoil's night forge (GAME-ONLY placement): by the Forge District's wall in the Lowtowns, at its gate, until the Forge
    ///   District opens (that zone is being built: her forge moves inside its gate then). 21:00-06:00; talk to her to forge, then to wake.
    /// - An awake weapon in hand: its one effect (LegendState), and blackened steel does not take (SteelProof).
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>Your awake weapon's effect state (None without one in hand).</summary>
        public LegendState Legend { get; private set; } = new LegendState();
        static EncounterSession legendHealer; static bool legendHealHooked;
        readonly Dictionary<Actor, float> amberUntil = new Dictionary<Actor, float>();
        readonly List<(Vector3 at, float until, float nextTick)> sunfire = new List<(Vector3, float, float)>();
        float legendLastRatio = -1;

        /// <summary>After the equipment changes (ApplyGearEffects): the awake weapon in hand, its effect.</summary>
        void RefreshLegend()
        {
            var w = LegendaryWeapons.AwakeInHand(Progress); var e = w != null ? w.effect : LegendEffect.None;
            if (Legend == null || Legend.effect != e) Legend = new LegendState(e);
            legendHealer = this;
            if (!legendHealHooked) { legendHealHooked = true; Combatant.HealAttempted += (by, who, asked, got) => { if (legendHealer != null) legendHealer.LegendHealed(by, who, asked, got); }; }
        }
        /// <summary>An awake legendary in your hand: steel does not blacken on her (it was remade with salt's crystal, 4.2).</summary>
        public bool SteelProof(Actor source) { return source != null && Player != null && source == Player && LegendaryWeapons.AwakeInHand(Progress) != null; }
        public float LegendHoldScale { get { return Legend != null ? Legend.HoldScale : 1; } }
        public float LegendBreakScale(EncounterEnemy e, Actor source) { return Legend != null && source != null && source == Player && e != null ? Legend.BreakScale(e.Incapacitated) : 1; }
        public bool AmberWarded(Actor a) { return a != null && amberUntil.TryGetValue(a, out var t) && Time.time < t; }
        bool FullHeat { get { return Kit is MageKit m && m.Heat >= MageKit.MaxHeat; } }

        /// <summary>A landed swing or auto-shot: Second Truth's afterimage, the Salt-Wall Bow's salt tip, the Crook's root.</summary>
        void LegendSwing(int swing)
        {
            if (Legend == null || Legend.effect == LegendEffect.None || Target == null || !Target.actor.IsAlive) return;
            float again = Legend.Swing();
            if (again > 0) { Target.Receive(Mathf.Max(1, Mathf.RoundToInt(swing * again)), Player); FloatText(Target.transform.position + Vector3.up * .6f, "Afterimage", new Color(1, .86f, .4f)); }
            if (Legend.Shot())
            {
                Target.Receive(Mathf.Max(1, Mathf.RoundToInt(swing * (LegendEffects.SaltTipScale - 1))), Player);
                int stripped = StripGood(Target, WeaveEaterRules.IsWeaveEater(Target) ? 2 : 1);
                FloatText(Target.transform.position + Vector3.up * .6f, "Salt-Tipped" + (stripped > 0 ? " (stripped)" : ""), new Color(.92f, .95f, 1));
            }
            if (Legend.effect == LegendEffect.Amber && Target.actor.IsAlive && Legend.Amber(Time.time)) { Target.Root(LegendEffects.AmberRoot); FloatText(Target.transform.position, "Amber", new Color(1, .7f, .3f)); }
        }
        /// <summary>Takes up to n good effects off a foe (a salt tip). Returns how many.</summary>
        static int StripGood(EncounterEnemy e, int n)
        {
            var c = e != null ? e.actor.GetComponent<Combatant>() : null; if (c == null || c.Statuses == null) return 0;
            var good = new List<string>(); c.Statuses.Each(Time.time, (id, left) => { if (!c.Statuses.IsHarmful(id)) good.Add(id); });
            int done = 0; foreach (var id in good) { if (done >= n) break; if (c.Statuses.Remove(id)) done++; }
            return done;
        }
        /// <summary>An ability resolved (ResolveWithGear): the First Verse's echo, the Sun-Staff's sunfire, Grohl's Harpoon's pin, the Mender's salt line.</summary>
        void LegendSpell(Crulanda.Abilities.AbilityDefinition a, System.Action effect, bool fullHeat, float power)
        {
            if (Legend == null || Legend.effect == LegendEffect.None || a == null || Player == null || !Player.IsAlive) return;
            float echo = Legend.Spell();
            if (echo > 0 && effect != null)
            {
                float was = GearPowerNow; GearPowerNow = power * echo;
                try { effect(); } finally { GearPowerNow = was; }
                FloatText(Player.transform.position + Vector3.up * .5f, "Echo-Jar", new Color(.8f, .85f, 1));
            }
            bool atFoe = Target != null && Target.actor.IsAlive && !Target.Game && (a.effect == Crulanda.Abilities.AbilityEffect.Damage || a.effect == Crulanda.Abilities.AbilityEffect.ApplyStatus);
            if (Legend.Sunfire(fullHeat) && atFoe) { sunfire.Add((Target.transform.position, Time.time + LegendEffects.SunfireSeconds, Time.time)); FloatText(Target.transform.position, "Sunfire", new Color(1, .75f, .3f)); }
            if (atFoe && Legend.effect == LegendEffect.Grounding && Legend.Ground(Time.time))
            {
                Target.Pin(LegendEffects.GroundingSeconds); Target.Root(LegendEffects.GroundingSeconds);
                if (WeaveEaterRules.IsWeaveEater(Target)) Target.Apply("silence", Player, LegendEffects.GroundingSeconds);
                FloatText(Target.transform.position, "Grounded", new Color(1, .8f, .45f));
            }
            if (atFoe && Legend.effect == LegendEffect.SaltLine && Legend.SaltLine(Time.time)) { Target.Apply("stun", Player, LegendEffects.SaltLineStun); FloatText(Target.transform.position, "Salt line", new Color(.95f, .97f, 1)); }
        }
        /// <summary>A mend of yours (Combatant.HealAttempted): the Lantern's kept light, the Crook's amber.</summary>
        void LegendHealed(Actor by, Actor who, int asked, int got)
        {
            if (this == null || Legend == null || Player == null || by != Player || who == null || !who.IsAlive) return;
            var c = who.GetComponent<Combatant>(); if (c == null) return;
            int kept = Legend.KeptLight(asked, got, who.Health.Pool.Max);
            if (kept > 0 && c.AddBarrier(kept, LegendEffects.KeptLightSeconds)) FloatText(who.transform.position, "Kept Light " + kept, new Color(1, .93f, .6f));
            if (Legend.effect == LegendEffect.Amber && got > 0 && Legend.Amber(Time.time) && c.AddBarrier(LegendState.AmberWard(who.Health.Pool.Max), LegendEffects.AmberWardSeconds))
            { amberUntil[who] = Time.time + LegendEffects.AmberWardSeconds; FloatText(who.transform.position, "Amber", new Color(1, .7f, .3f)); }
        }
        /// <summary>Each frame: the Anchor and Exist Elsewhere (read off your health as it falls), and the sunfire patches burning.</summary>
        void TickLegend()
        {
            if (Player == null || Legend == null) return;
            float ratio = Player.IsAlive ? Player.Health.Pool.Ratio : 0;
            if (legendLastRatio >= 0 && Player.IsAlive && ratio < legendLastRatio)
            {
                if (Legend.Anchor(Time.time, legendLastRatio, ratio))
                {
                    Player.GetComponent<Combatant>()?.Apply(new StatusEffectDefinition { id = "buff.legend_anchor", name = "The Anchor", duration = LegendEffects.AnchorSeconds, incomingDamageMultiplier = LegendEffects.AnchorTaken });
                    HoldPlayer("root", LegendEffects.AnchorSeconds, Player.transform.position, "The Anchor holds you");
                }
                if (Legend.Blink(Time.time, legendLastRatio - ratio) && Target != null && Target.actor.IsAlive)
                {
                    var behind = Target.transform.position - Target.transform.forward * LegendEffects.BlinkMeters;
                    if (NavMesh.SamplePosition(behind, out var hit, 3, NavMesh.AllAreas)) { Player.GetComponent<AdventurerMotor>()?.Teleport(hit.position); FloatText(hit.position, "Exist Elsewhere", new Color(.8f, .85f, 1)); }
                }
            }
            legendLastRatio = ratio;
            for (int i = sunfire.Count - 1; i >= 0; i--)
            {
                var s = sunfire[i]; if (Time.time >= s.until) { sunfire.RemoveAt(i); continue; }
                if (Time.time < s.nextTick) continue;
                sunfire[i] = (s.at, s.until, s.nextTick + 1);
                int burn = LegendState.SunfireTick(Progress.Level);
                foreach (var e in Enemies.ToArray()) if (e != null && e.actor.IsAlive && !e.Game && !e.Away && Vector3.Distance(e.transform.position, s.at) <= LegendEffects.SunfireRadius) e.Receive(burn, Player);
            }
        }

        // ---------- her loot ----------
        /// <summary>Her body: two epics (rolled for by the raid) and your components; each sim of the raid rolls its own components now.</summary>
        List<LootDrop> UnravellerDrops(EncounterEnemy corpse)
        {
            var rng = new System.Random(Random.Range(0, int.MaxValue));
            var drops = UnravellerLoot.RollEpics(rng);
            ComponentBag.Of(Progress); if (Progress.componentLuck == null) Progress.componentLuck = new List<LootLuck>();
            drops.AddRange(UnravellerLoot.Roll(Progress.classId, Progress.components, Progress.componentLuck, LegendaryWeapons.ForgedFor(Progress), rng));
            foreach (var c in PartySims)
            {
                if (c == null || c.sim == null) continue;
                var got = SimComponents(c.sim, rng);
                foreach (var d in got) if (d.item != UnravellerLoot.Crystal) ChatSay(ChatChannel.Party, c.sim.name, "got my " + ItemName(d.item) + "!");
            }
            Message("The Unraveller's body: your own pieces lie on it for you alone; her gear is rolled for. " + LegendCounters);
            return drops;
        }
        /// <summary>A sim's own roll (its counters in the world slot); its pieces go straight into its component bag.</summary>
        List<LootDrop> SimComponents(SimAdventurer sim, System.Random rng)
        {
            if (sim.components == null) sim.components = new List<ItemStack>(); if (sim.componentLuck == null) sim.componentLuck = new List<LootLuck>();
            var got = UnravellerLoot.Roll(sim.classId, sim.components, sim.componentLuck, false, rng);
            foreach (var d in got) { var def = Items?.Get(d.item); if (def != null) ComponentBag.Add(sim.components, def, d.count); }
            return got;
        }
        /// <summary>The counters for you, in words (the loot line and the Legendary tab).</summary>
        public string LegendCounters { get { return Progress == null ? "" : UnravellerLoot.CountersLine(Progress.classId, Progress.components, Progress.componentLuck, LegendaryWeapons.ForgedFor(Progress)); } }
        void TookComponent(string item)
        {
            var w = LegendaryWeapons.For(Progress.classId);
            var missing = LegendaryWeapons.Missing(Progress, Items);
            Message("Into your Legendary pieces (bags, Legendary tab): " + ItemName(item) + ". " + LegendCounters + (missing != null && missing.Count == 0 && w != null ? " Everything for " + w.name + ": take it to " + LegendaryWeapons.Smith + "'s night forge." : ""));
        }

        // ---------- Tynara Embercoil's night forge ----------
        public const string TynaraNight = "By day I work for the Council's forges. After nine the forge is mine. Come back then.";
        public const string TynaraForged = "She lays the crystal in the fire and her hands move over the glass without touching it. It comes out pale, and asleep.";
        public const string TynaraWoken = "She sets the note against the seam and the crystal in it lights from the inside. It is awake.";
        /// <summary>Talking to Tynara: the forge's recipe, the forging, the waking; by day she sends you away. False for anyone else.</summary>
        bool TynaraTalk(Villager v)
        {
            if (v == null || v.Name != LegendaryWeapons.Smith) return false;
            string Say(string line) { ChatSay(ChatChannel.Say, LegendaryWeapons.Smith, line); return line; }
            if (!LegendaryWeapons.ForgeOpen(Crulanda.World.WorldClock.Hour)) { Say(TynaraNight); return true; }
            var w = LegendaryWeapons.For(Progress.classId);
            if (w == null) { Say("Nothing she ate is yours to have back."); return true; }
            if (!LegendaryWeapons.Forged(Progress, w.id))
            {
                if (LegendaryWeapons.Forge(Progress, Items, out var why)) { Message(TynaraForged); Received(w.id, 1); Say(w.name + ". It sleeps. Find a note of the Original Song and it will wake."); Save(false); }
                else Say("Three Brood-Crystals, your " + ItemName(w.Blank) + ", her Heart, " + LegendaryWeapons.ForgeCoin + " crowns and a slag-iron bar. " + why);
                return true;
            }
            if (LegendaryWeapons.IsAwake(Progress, w.id)) { Say(w.name + " is awake. Keep it out of the river."); return true; }
            if (LegendaryWeapons.Wake(Progress, Items, out var not)) { Message(TynaraWoken); ShowToast("LEGENDARY", w.name); Say(w.name + " is awake."); ApplyEquipment(); Save(false); }
            else Say(not);
            return true;
        }
        /// <summary>/legend: your pieces and counters; "/legend note" is a tester's (the Note until the Verses' bosses exist, 4.5), as /level.</summary>
        public void LegendCommand(string arg)
        {
            if (string.Equals(arg, "note", System.StringComparison.OrdinalIgnoreCase)) { LegendaryWeapons.GiveTestNote(Progress, Items); Message("Testing: a Note of the Original Song in your Legendary pieces."); return; }
            var bag = ComponentBag.Of(Progress);
            Message("Legendary pieces" + (bag.Count == 0 ? ": none yet." : ":"));
            foreach (var s in bag) Message("  " + ItemName(s.item) + (s.count > 1 ? " x" + s.count : ""));
            Message(LegendCounters);
            var w = LegendaryWeapons.For(Progress.classId);
            if (w != null) Message(w.name + ": " + (LegendaryWeapons.IsAwake(Progress, w.id) ? "awake." : LegendaryWeapons.Forged(Progress, w.id) ? "forged, asleep." : "not yet forged (" + LegendaryWeapons.Smith + ", by the Forge District Gate, 21:00-06:00)."));
        }
    }
}
