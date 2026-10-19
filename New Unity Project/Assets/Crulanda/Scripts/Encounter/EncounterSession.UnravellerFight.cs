using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Core;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Unraveller's fight as the session runs it (Docs/WORLD_BOSS_1.md 3.3-3.5; build steps 4 and 5; her own part is
    /// EncounterEnemy.Unraveller, the rules UnravellerRules, the numbers UnravellerTuning, the sims' parts SimCompanion.Raid):
    /// - the pull: the clock against the enrage on the boss frame, her pin timer, the rotations the leader sets (two or three harpooners, two
    ///   or three interrupters: the sims of the raid best placed for it; you join the harpoons with /raid harpoon), her action track;
    /// - phase 2, "The Brood" (70%): three of her brood out of the river on both sides of the bar every 30 s (one that reaches her is eaten:
    ///   she heals and hits harder); unmade ground (ch.20:119) every 20 s where she floats, shown 3 s before, there for the fight; Swallowed every
    ///   35 s (three salts from three members within 12 s free them; 12 s and they are erased);
    /// - phase 3, "The Unravelling" (40%): the pulse every 3 s, growing; Hunted every 20 s (into the river and she misses, b1 ch.16:75); the
    ///   brood and the swallowing every 40 s;
    /// - the feast at 9:00: everyone on the bar erased, a wipe counted, and she is whole again (or sinks, at the second).
    /// Everything it puts on you is told: the raid warning, a large line for 2 s ("She is hunting YOU: get in the river!").
    /// </summary>
    public sealed partial class EncounterSession
    {
        static UnravellerTuning UT { get { return UnravellerTuning.Current; } }
        /// <summary>Her brood up now (alive or fallen this fight).</summary>
        public readonly List<EncounterEnemy> BroodNow = new List<EncounterEnemy>();
        readonly List<(Vector3 at, float live, BlowMark mark)> unmade = new List<(Vector3, float, BlowMark)>();
        readonly Dictionary<string, int> groundSeconds = new Dictionary<string, int>();
        /// <summary>The swallowing now (who, since when, who has salted her).</summary>
        public readonly SwallowSalts Swallow = new SwallowSalts();
        /// <summary>The one inside her now, or null.</summary>
        public Actor SwallowedActor { get; private set; }
        /// <summary>The one she hunts now (phase 3), or null; and when she falls on them.</summary>
        public Actor HuntedActor { get; private set; }
        public float HuntLandsIn { get { return HuntedActor == null ? 0 : Mathf.Max(0, huntAt - Time.time); } }
        float huntAt = -1, nextBrood, nextUnmade, nextSwallow, nextPulse, nextHunt, nextGroundBite, nextHaulLook, nextSaltCall, pinFreeSince = -1; bool feasted;
        Vector3 haulDir; BlowMark huntMark;
        /// <summary>The leader's rotations (3.5): the harpoons and the interrupts on the Feeding.</summary>
        public RaidRotation HarpoonRotation { get; private set; }
        public RaidRotation InterruptRotation { get; private set; }
        /// <summary>You in the harpoon rotation (/raid harpoon).</summary>
        public bool HarpoonYou { get; set; }
        /// <summary>The sims asked to salt her now (the three nearest with salt, less you when you are one of them).</summary>
        public readonly List<string> SaltCall = new List<string>();
        /// <summary>What happened this fight, for the log and the tests.</summary>
        public int BroodKilled, BroodBrokeFree, BroodRisen, BroodEaten, SwallowsFreed, SwallowsErased, HuntsMissed, HuntsLanded, Pulses, Feasts, HarpoonsThrown, SaltsThrown;
        /// <summary>Unmade patches lying now (shown or warned).</summary>
        public int UnmadePatches { get { return unmade.Count; } }
        public IEnumerable<Vector3> UnmadeAt { get { return unmade.Select(u => u.at); } }
        string raidWarning; float raidWarningUntil;
        /// <summary>The raid warning showing now (a large centred line for 2 s), or null.</summary>
        public string RaidWarningText { get { return Time.time < raidWarningUntil ? raidWarning : null; } }
        /// <summary>Tells you, in the large centred line, what is about to kill you (2 s); and in the chat.</summary>
        public void RaidWarning(string line) { if (string.IsNullOrEmpty(line)) return; raidWarning = line; raidWarningUntil = Time.time + 2; Message(line); }
        /// <summary>Her, in a fight with the raid now (null otherwise).</summary>
        public EncounterEnemy UnravellerFighting { get { var e = UnravellerFoe; return e != null && e.actor != null && e.actor.IsAlive && e.PullAt >= 0 ? e : null; } }
        /// <summary>You silenced (her touch): no spell, until then.</summary>
        public float PlayerSilencedUntil { get; private set; }
        public bool PlayerSilenced { get { return Time.time < PlayerSilencedUntil; } }

        /// <summary>Each frame of her up (TickUnravellerFoe): her fight, while she fights the party.</summary>
        void TickUnravellerFight(EncounterEnemy e)
        {
            if (e == null || !e.actor.IsAlive || !e.FightingParty) return;
            float now = Time.time;
            if (e.PullAt < 0) BeginUnravellerFight(e);
            // The whole raid down: the pull is over (a wipe); she is whole again and goes home, and nobody gets up until it is.
            if (!PartyActors().Exists(x => x != null && x.IsAlive)) { e.ResetFight(); return; }
            var t = UT;
            RaidBoss = e;
            // The phase, and the Feeding's beat with it.
            int phase = UnravellerRules.PhaseNow(e.RaidPhase, e.actor.Health.Pool.Ratio, t);   // once reached, held (run 1: healed back over 70%, it went back to 1)
            if (phase != e.RaidPhase) EnterPhase(e, phase);
            if (e.Cast != null) e.Cast.every = t.FeedEvery(phase);
            // The hauling: two harpoons in her, a metre a second toward the river's middle.
            if (e.Hauled)
            {
                if (now >= nextHaulLook) { nextHaulLook = now + 1; haulDir = RiverWay(e.transform.position); }
                e.Haul(haulDir, t.haulPace * Time.deltaTime);
            }
            TickBrood(e, phase, now);
            TickUnmade(e, phase, now);
            TickSwallow(e, phase, now);
            TickHunt(e, phase, now);
            if (phase >= 3 && now >= nextPulse)
            {
                nextPulse = now + t.pulseEvery;
                float share = UnravellerRules.PulseShare(Pulses, t); Pulses++;
                foreach (var a in PartyActors().ToArray()) if (a != null && a.IsAlive && Vector3.Distance(a.transform.position, e.transform.position) < 60) Hurt(a, share, null, false);
                FloatText(e.transform.position + Vector3.up * 2.5f, Unraveller.PulseName + " " + Mathf.RoundToInt(share * 1000) / 10f + "%", new Color(.62f, .55f, .8f));
            }
            // The frames: her pin, the clock; the harpoon rotation's turn passed on from one who is not throwing (you away from the keys).
            RaidBossPinned = e.PinLeft; RaidBossEnrage = UnravellerRules.EnrageLeft(e.SincePull, t);
            if (e.PinLeft <= 0) { if (pinFreeSince < 0) pinFreeSince = now; else if (now - pinFreeSince > 1.5f && HarpoonRotation != null && HarpoonRotation.Current(HarpoonAble) == RaidGroup.You) { HarpoonRotation.Skip(); pinFreeSince = now; } }
            else pinFreeSince = -1;
            TickSpikes(e, now); TickSnares();
            if (!feasted && UnravellerRules.Feast(e.SincePull, t)) Feast(e);
        }
        // ---------- the ground spikes (raid 7): warned, then they burst ----------
        readonly List<(Vector3 at, BlowMark mark)> spikeSpots = new List<(Vector3, BlowMark)>();
        float spikeAt = -1, nextSpike;
        public int SpikeBursts, SpikeHits;
        VoidWrap WrapOf(EncounterEnemy e) { var b = e != null ? e.GetComponent<UnravellerBody>() : null; return b != null ? b.Wrap : null; }
        void TickSpikes(EncounterEnemy e, float now)
        {
            var t = UT; var wrap = WrapOf(e); if (wrap != null) wrap.AutoErupt = false;
            if (spikeAt < 0 && now >= nextSpike) BeginSpikes(e, SpikeSpots(e));
            if (spikeAt < 0) return;
            foreach (var s in spikeSpots) if (s.mark != null) s.mark.Show(s.at, t.spikeRadius, Mathf.Clamp01(1 - (spikeAt - now) / Mathf.Max(.01f, t.spikeWarn)));
            if (now < spikeAt) return;
            // They burst: the shards out of the ground there, and whoever of the raid stands in a crack is struck and thrown up.
            var spots = spikeSpots.Select(s => s.at).ToList(); SpikeBursts++;
            if (wrap != null) wrap.EruptAt(spots, t.spikeRadius);
            foreach (var a in PartyActors().ToArray())
            {
                if (a == null || !a.IsAlive || a == SwallowedActor) continue;
                bool hit = false; foreach (var p in spots) if (Flat(a.transform.position - Vector3.up - p) <= t.spikeRadius && Mathf.Abs(a.transform.position.y - 1 - p.y) < GroundMarks.Height) hit = true;
                if (!hit) continue;
                SpikeHits++; Hurt(a, t.spikeShare, Unraveller.SpikesName, false);
                if (a == Player) StunPlayer(t.spikeLift, Unraveller.SpikesThrown); else a.GetComponent<SimCompanion>()?.HoldUntil("stun", t.spikeLift, a.transform.position);
            }
            foreach (var s in spikeSpots) if (s.mark != null) Destroy(s.mark.gameObject);
            spikeSpots.Clear(); spikeAt = -1;
            nextSpike = now + Random.Range(t.spikeEveryMin, t.spikeEveryMax);
        }
        /// <summary>The spikes' two spots: where one of the raid stands (not the swallowed), and a point at her edge; never by a held brood.</summary>
        List<Vector3> SpikeSpots(EncounterEnemy e)
        {
            var t = UT; var spots = new List<Vector3>();
            var held = BroodNow.Where(b => b != null && b.actor.IsAlive && b.Incapacitated).Select(b => new Vector2(b.transform.position.x, b.transform.position.z)).ToList();
            var near = PartyActors().Where(a => a != null && a.IsAlive && a != SwallowedActor && Vector3.Distance(a.transform.position, e.transform.position) < 30).ToList();
            for (int k = 0; k < 6 && near.Count > 0; k++)
            {
                var p = near[Random.Range(0, near.Count)].transform.position - Vector3.up;
                if (UnravellerRules.SpikeSpotClear(new Vector2(p.x, p.z), held, t)) { spots.Add(p); break; }
            }
            for (int k = 0; k < 6; k++)
            {
                var p = e.transform.position + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * (e.BodyRadius + 1.5f);
                if (NavMesh.SamplePosition(p, out var hit, 2, NavMesh.AllAreas)) p = hit.position;
                if (UnravellerRules.SpikeSpotClear(new Vector2(p.x, p.z), held, t)) { spots.Add(p); break; }
            }
            return spots;
        }
        void BeginSpikes(EncounterEnemy e, List<Vector3> spots)
        {
            if (spots.Count == 0) { nextSpike = Time.time + 1; return; }
            foreach (var p in spots) { var m = BlowMark.Create(GroundKind.Harmful, UT.spikeWarn + 1, "Violet cracks"); m.Show(p, UT.spikeRadius, 0); spikeSpots.Add((p, m)); }
            spikeAt = Time.time + UT.spikeWarn;
            if (Player != null && spots.Exists(p => Flat(Player.transform.position - Vector3.up - p) <= UT.spikeRadius + .5f)) RaidWarning(Unraveller.SpikesWarning);
        }
        /// <summary>Where her blink lands: the spikes there too, warned as ever (raid 7).</summary>
        public void SpikesAt(Vector3 at)
        {
            if (UnravellerFighting == null) return;
            if (NavMesh.SamplePosition(at, out var hit, 3, NavMesh.AllAreas)) at = hit.position;
            var held = BroodNow.Where(b => b != null && b.actor.IsAlive && b.Incapacitated).Select(b => new Vector2(b.transform.position.x, b.transform.position.z)).ToList();
            if (!UnravellerRules.SpikeSpotClear(new Vector2(at.x, at.z), held, UT)) return;
            if (spikeAt < 0) BeginSpikes(UnravellerFighting, new List<Vector3> { at });
            else { var m = BlowMark.Create(GroundKind.Harmful, UT.spikeWarn + 1, "Violet cracks"); m.Show(at, UT.spikeRadius, 0); spikeSpots.Add((at, m)); }
        }
        /// <summary>The pull: the clock begins, the rotations are set, her track comes up.</summary>
        void BeginUnravellerFight(EncounterEnemy e)
        {
            float now = Time.time; e.PullAt = now; e.RaidPhase = 0; feasted = false; nextSpike = now + Random.Range(UT.spikeEveryMin, UT.spikeEveryMax); nextBrood = now + Mathf.Max(10, UT.broodEvery1);   // phase-1 adds (off by default)
            HarmSeconds.Clear(); SnaresLaid = SnaresCaught = BroodHoldsRanOut = 0;
            SpikeBursts = SpikeHits = BroodKilled = BroodBrokeFree = BroodRisen = BroodEaten = SwallowsFreed = SwallowsErased = HuntsMissed = HuntsLanded = Pulses = Feasts = HarpoonsThrown = SaltsThrown = 0;
            SetRotations();
            if (Unraveller.FightTrackReady) Crulanda.World.ZoneMusic.FightOverride = Unraveller.FightTrack;
            if (HarpoonRotation != null && HarpoonRotation.Order.Count > 0) Message("Harpoons, in turn: " + string.Join(", ", HarpoonRotation.Order.Select(NameOfRaidId)) + ". Interrupts on the Feeding: " + (InterruptRotation != null && InterruptRotation.Order.Count > 0 ? string.Join(", ", InterruptRotation.Order.Select(NameOfRaidId)) : "nobody") + ".");
        }
        /// <summary>The fight over (EncounterEnemy.ClearRaidFight: won, wiped or reset): the brood sink, the ground is whole, the swallowed and the hunted let go.</summary>
        public void EndUnravellerFight(EncounterEnemy e)
        {
            foreach (var b in BroodNow) if (b != null) { Enemies.Remove(b); if (Target == b) Target = null; Destroy(b.gameObject); }
            BroodNow.Clear(); BroodKill = null; broodWas.Clear(); broodDeadAt.Clear();
            foreach (var u in unmade) if (u.mark != null) Destroy(u.mark.gameObject);
            unmade.Clear(); groundSeconds.Clear();
            foreach (var sn in snares) if (sn.mark != null) Destroy(sn.mark.gameObject);
            snares.Clear();
            foreach (var s in spikeSpots) if (s.mark != null) Destroy(s.mark.gameObject);
            spikeSpots.Clear(); spikeAt = -1; nextSpike = 0;
            var wrap = WrapOf(e); if (wrap != null) wrap.AutoErupt = true;
            if (SwallowedActor != null) Spit(e, SwallowedActor);
            Swallow.End(); SwallowedActor = null; SaltCall.Clear();
            HuntedActor = null; huntAt = -1; if (huntMark != null) huntMark.Hide();
            RaidBossPinned = 0; RaidBossEnrage = 0; pinFreeSince = -1;
            if (Crulanda.World.ZoneMusic.FightOverride == Unraveller.FightTrack) Crulanda.World.ZoneMusic.FightOverride = null;
        }
        void EnterPhase(EncounterEnemy e, int phase)
        {
            float now = Time.time; int was = e.RaidPhase; e.RaidPhase = phase;
            if (was == 0) return;   // the pull itself: phase 1, nothing to say
            if (phase == 2) { ChatSay(ChatChannel.Say, Unraveller.Name, Unraveller.BroodLine); nextBrood = now + 3; nextUnmade = now + 5; nextSwallow = now + 10; }
            if (phase == 3) { ChatSay(ChatChannel.Say, Unraveller.Name, Unraveller.MoreLine); nextPulse = now + UT.pulseEvery; nextHunt = now + 6; if (was < 2) { nextBrood = now + 3; nextUnmade = now + 5; nextSwallow = now + 10; } }
            Message(Unraveller.PhaseSystem(phase));
        }

        // ---------- the brood (phase 2 on) ----------
        /// <summary>
        /// The raid's buff-up before a pull (raid 6): every member buffs from its bags as at a dungeon boss's stop (you as /buff does, each sim
        /// its turn: food, the elixir for its part, a draught if low, its class's standing buff on all ten); then what each carries, by name:
        /// how many class buffs, fed or not, an elixir (or salve) or not. Out of a fight only.
        /// </summary>
        public List<string> RaidBuffUp()
        {
            var lines = new List<string>(); if (InCombat) return lines;
            // The raid's stewpot first (raid 6, Chris: "need to wait or build a raid buff food"): whoever carries one sets it down; with none
            // carried, the sim with the most coin buys one of Mother Sall at the Rusty Spoon (on the way: it is there for the pull).
            if (InRaid && raidFeast == null)
            {
                var pot = Items?.Get(RaidFeastItem);
                if (pot != null)
                {
                    int bag = Progress.bag.FindIndex(s => !s.Empty && s.item == RaidFeastItem);
                    if (bag >= 0) { UseItem(bag); lines.Add("you set down a " + pot.name); }
                    else
                    {
                        SimCompanion cook = null;
                        foreach (var c in PartySims) if (c != null && c.actor.IsAlive && SimEconomy.Count(c.sim, RaidFeastItem) > 0) { cook = c; break; }
                        if (cook == null) { foreach (var c in PartySims) if (c != null && c.actor.IsAlive && (cook == null || c.sim.coin > cook.sim.coin)) cook = c;
                            if (cook != null) { cook.sim.coin = Mathf.Max(0, cook.sim.coin - BossBuffs.FairPrice(pot)); SimEconomy.Add(cook.sim, RaidFeastItem, 1); } }
                        if (cook != null) { SimEconomy.Add(cook.sim, RaidFeastItem, -1); SetDownRaidFeast(pot, cook.actor); lines.Add(cook.sim.name + " set down a " + pot.name + " (from the Rusty Spoon)"); ChatSay(ChatChannel.Party, cook.sim.name, cook.sim.chatty > .65f ? "stew's down, eat up before the pull" : "Mother Sall's stew is on the board. Eat before we pull."); }
                    }
                    FeedFromRaidFeast();
                }
            }
            var mine = BuffUp(); lines.Add("you used: " + (mine.Count > 0 ? string.Join(", ", mine) : "nothing"));
            foreach (var c in PartySims) if (c != null && c.actor != null && c.actor.IsAlive) lines.Add(c.sim.name + " used: " + c.BuffForRaid());
            return lines;
        }
        /// <summary>The raid's feast's item (Items/lowtowns.json; Professions: recipe.reach_stewpot, Cooking 100).</summary>
        public const string RaidFeastItem = "food.reach_stewpot";
        ItemDef raidFeast; Vector3 raidFeastAt; float raidFeastUntil; readonly HashSet<Actor> raidFeastFed = new HashSet<Actor>();
        /// <summary>The raid's feast out now (its item), or null.</summary>
        public ItemDef RaidFeastOut { get { return raidFeast != null && Time.time < raidFeastUntil ? raidFeast : null; } }
        /// <summary>
        /// The raid's stewpot set down by <paramref name="cook"/> (raid 6): the pot on its board where the cook stands, for
        /// Consumables.RaidFeastSeconds; everyone of the raid (all ten) who comes within Consumables.FeastRange of it while it is out eats once
        /// and is well fed for an hour (its buff, a little more than a dish's). Out of a fight only.
        /// </summary>
        public bool SetDownRaidFeast(ItemDef d, Actor cook)
        {
            if (d == null || cook == null) return false;
            raidFeast = d; raidFeastAt = cook.transform.position - Vector3.up; raidFeastUntil = Time.time + Consumables.RaidFeastSeconds; raidFeastFed.Clear();
            var go = SetDish(d.id, raidFeastAt + cook.transform.forward * 1.2f, cook.transform.position);
            if (go != null) Destroy(go, Consumables.RaidFeastSeconds);
            Message((cook == Player ? "You set" : cook.DisplayName + " sets") + " down a " + d.name + " for the raid: come and eat (" + Mathf.RoundToInt(Consumables.RaidFeastSeconds) + " s).");
            FeedFromRaidFeast();
            return true;
        }
        /// <summary>Those of the raid within reach of the stewpot who have not eaten from it yet eat now (ten at most).</summary>
        void FeedFromRaidFeast()
        {
            if (RaidFeastOut == null) return;
            var list = new List<Crulanda.Combat.StatusEffectRuntime>(); var who = new List<Actor>();
            foreach (var a in PartyActors())
            {
                if (a == null || !a.IsAlive || raidFeastFed.Contains(a) || Vector3.Distance(a.transform.position - Vector3.up, raidFeastAt) > Consumables.FeastRange) continue;
                var c = a.GetComponent<Crulanda.Combat.Combatant>(); if (c == null || c.Statuses == null) continue;
                list.Add(c.Statuses); who.Add(a);
            }
            if (list.Count == 0 || raidFeastFed.Count >= Consumables.RaidSize) return;
            Consumables.Feed(raidFeast, list.Take(Consumables.RaidSize - raidFeastFed.Count).ToList(), Time.time);
            foreach (var a in who.Take(Consumables.RaidSize - raidFeastFed.Count).ToList()) raidFeastFed.Add(a);
        }
        float raidFeastLook;
        /// <summary>Each frame (TickUnraveller): the raid comes and eats while the pot is out.</summary>
        void TickRaidFeast() { if (raidFeast == null || Time.time < raidFeastLook) return; raidFeastLook = Time.time + .5f; if (RaidFeastOut == null) { raidFeast = null; return; } FeedFromRaidFeast(); }
        /// <summary>What <paramref name="a"/> carries now: its class buffs, fed, an elixir or salve.</summary>
        public (int classBuffs, bool fed, bool elixir) BuffsOn(Actor a)
        {
            var st = a != null ? a.GetComponent<Crulanda.Combat.Combatant>()?.Statuses : null; if (st == null) return (0, false, false);
            int n = 0; foreach (var id in ClassBuffs.ClassIds) { var b = ClassBuffs.For(id); if (b != null && st.Has(b.id)) n++; }
            return (n, st.Has(Consumables.FedId), st.Has(Consumables.ElixirId) || st.Has(Consumables.SalveId));
        }
        /// <summary>Her unmade ground's grey (it keeps it under the harmful red-violet edge: play note 275).</summary>
        public static readonly Color UnmadeGrey = new Color(.42f, .4f, .46f, .55f);
        // ---------- helpful ground (play note 275): one look for every friendly ground effect ----------
        readonly List<(BlowMark mark, Vector3 at, float radius, float until, Actor owner)> helpful = new List<(BlowMark, Vector3, float, float, Actor)>();
        /// <summary>A helpful ground effect, shown as one (soft gold or green, smooth, calm), for <paramref name="seconds"/>, gone with its owner's
        /// death; an ally's fainter. Every class's ground ability lays its look here.</summary>
        public BlowMark HelpfulGround(Vector3 at, float radius, float seconds, string label, Actor owner, bool green)
        {
            if (seconds <= 0 || radius <= 0) return null;
            var feet = at - Vector3.up; if (NavMesh.SamplePosition(at, out var hit, 2.5f, NavMesh.AllAreas)) feet = hit.position;
            var m = BlowMark.Create(GroundKind.Helpful, seconds + 1, label, green); m.Ally = owner != Player; m.Show(feet, radius, 1);
            helpful.Add((m, feet, radius, Time.time + seconds, owner));
            return m;
        }
        void TickHelpfulGround()
        {
            for (int i = helpful.Count - 1; i >= 0; i--)
            {
                var h = helpful[i];
                if (h.mark == null || Time.time >= h.until || h.owner != null && !h.owner.IsAlive) { if (h.mark != null) Destroy(h.mark.gameObject); helpful.RemoveAt(i); continue; }
                h.mark.Show(h.at, h.radius, 1);
            }
        }
        /// <summary>The HUD's line by your health (play note 275): the harmful ground you stand in, in red ("Unmade ground: move!"), else the
        /// helpful ground, in green ("In Consecrated ground"); null for neither.</summary>
        public (string line, bool harmful)? GroundUnderYou
        {
            get
            {
                if (Player == null || !Player.IsAlive) return null;
                var feet = Player.transform.position - Vector3.up; var p = new Vector2(feet.x, feet.z);
                BlowMark help = null;
                foreach (var m in BlowMark.Live)
                {
                    if (m == null || !m.Showing || m.Reach <= 0 || Mathf.Abs(m.Feet.y - feet.y) > GroundMarks.Height) continue;
                    if (Vector2.Distance(p, new Vector2(m.Feet.x, m.Feet.z)) > m.Reach) continue;
                    if (m.Kind == GroundKind.Harmful) return ((m.Label ?? "Danger") + ": move!", true);
                    if (help == null) help = m;
                }
                return help != null ? ("In " + (help.Label ?? "blessed ground"), false) : ((string, bool)?)null;
            }
        }
        // ---------- the salt snare (play note 281; CANON: salt and bone hold the Weave-Eaters, b1 ch.20:181-205; the snare GAME-ONLY) ----------
        readonly List<(Vector3 at, float until, BlowMark mark)> snares = new List<(Vector3, float, BlowMark)>();
        public int SnaresLaid, SnaresCaught;
        /// <summary>The raid's Trapper (a sim's id, "you", or null: the first Ranger of the raid; /raid trapper).</summary>
        public string TrapperId { get; set; }
        public string Trapper { get { if (TrapperId != null) return TrapperId; var r = PartySims.Find(c => c != null && c.actor.IsAlive && c.sim.classId == "class.ranger"); return r != null ? r.sim.id : null; } }
        public int SnaresOut { get { return snares.Count; } }
        /// <summary>A salt snare laid at <paramref name="at"/> by <paramref name="by"/>: a white ring of salt on bone stakes (a helpful mark).</summary>
        public void LaySnare(Vector3 at, Actor by)
        {
            var t = UT; if (NavMesh.SamplePosition(at, out var hit, 2.5f, NavMesh.AllAreas)) at = hit.position;
            var m = BlowMark.Create(GroundKind.Helpful, t.snareSeconds + 1, "A salt snare", true); m.Ally = by != Player; m.Show(at, t.snareRadius, 1);
            snares.Add((at, Time.time + t.snareSeconds, m)); SnaresLaid++;
            FloatText(at + Vector3.up, "Salt snare", new Color(.95f, .95f, .9f));
        }
        void TickSnares()
        {
            var t = UT;
            for (int i = snares.Count - 1; i >= 0; i--)
            {
                var s = snares[i]; bool done = Time.time >= s.until;
                if (!done)
                    foreach (var b in BroodNow)
                    {
                        if (b == null || !b.actor.IsAlive || b.Controlled || b.Calcified || Flat(b.transform.position - Vector3.up - s.at) > t.snareRadius) continue;
                        b.RawStun(t.snareHold); b.CalcifyBrood(t.snareHold); SnaresCaught++; done = true;   // white crystal in the salt: held, then shattered in the kill order
                        FloatText(b.transform.position + Vector3.up * 1.5f, "Snared", new Color(.97f, .97f, .92f));
                        break;
                    }
                if (done) { if (s.mark != null) Destroy(s.mark.gameObject); snares.RemoveAt(i); }
                else if (s.mark != null) s.mark.Show(s.at, t.snareRadius, 1);
            }
        }
        /// <summary>The last thing that struck each of the raid (the run's death lines: what killed them).</summary>
        public readonly Dictionary<string, string> LastHitBy = new Dictionary<string, string>();
        public void NoteHit(Actor victim, string what) { if (victim != null && what != null) LastHitBy[victim.EntityId.Value] = what; }
        /// <summary>Seconds each of the raid (and a pet) stood in harmful ground this pull (the run's log).</summary>
        public readonly Dictionary<string, float> HarmSeconds = new Dictionary<string, float>();
        public void CountHarm(Actor a, float seconds)
        {
            if (a == null || seconds <= 0) return; string n = a == Player ? "you" : a.DisplayName.Split(' ')[0] + (a.DisplayName.Contains("'s ") ? " " + a.DisplayName.Split(' ').Last() : "");
            HarmSeconds.TryGetValue(n, out var was); HarmSeconds[n] = was + seconds;
        }
        /// <summary>Raisings this session (the Shaman's Barrow-wake; the run's log).</summary>
        public int Rezzes;
        /// <summary>The fallen one a Shaman at <paramref name="from"/> raises (RaidJobs.RezPick: a tank, a healer, a damage-dealer; not in marked ground).</summary>
        public Actor RezTarget(Vector3 from, float reach)
        {
            var list = new List<(string, SimCompanion.PartyRole, bool, bool, float)>(); var by = new Dictionary<string, Actor>();
            foreach (var a in PartyActors())
            {
                if (a == null) continue;
                var c = a.GetComponent<SimCompanion>(); var role = a == Player ? PlayerRole : c != null ? c.Role : SimCompanion.PartyRole.Damage;
                var feet = a.transform.position - Vector3.up; bool danger = GroundMarks.Inside(new Vector2(feet.x, feet.z), GroundMarks.Near(feet.y)) >= 0;
                list.Add((a.EntityId.Value, role, !a.IsAlive, danger, Vector3.Distance(from, a.transform.position))); by[a.EntityId.Value] = a;
            }
            var id = RaidJobs.RezPick(list, reach); return id != null && by.TryGetValue(id, out var who) ? who : null;
        }
        /// <summary>Raises <paramref name="who"/> at 40% of its health and a third of its pool (the Barrow-wake), said and counted.</summary>
        public void RaiseFallen(Actor who, Actor by, string what)
        {
            if (who == null || who.IsAlive) return;
            who.Health.Revive(Mathf.RoundToInt(who.Health.Pool.Max * .4f));
            if (who.Resource != null) { var p = who.Resource.Pool; p.SetCurrent(Mathf.Max(p.Current, p.Max / 3)); }
            who.GetComponent<SimCompanion>()?.Raised();
            Rezzes++; HealFx.Show(who.transform);
            FloatText(who.transform.position + Vector3.up, what, new Color(.78f, .84f, 1));
            Message((by == Player ? "Your " : by.DisplayName + "'s ") + what + ": the old dead send " + (who == Player ? "you" : who.DisplayName) + " back up out of the dark.");
        }
        /// <summary>The off-tank: the raid's tank sim she is not on (it takes the brood), or null.</summary>
        public SimCompanion OffTank
        {
            get
            {
                var e = UnravellerFighting; if (e == null) return null;
                foreach (var c in PartySims) if (c != null && c.actor.IsAlive && c.Role == SimCompanion.PartyRole.Tank && e.Victim != c.actor) return c;
                return null;
            }
        }
        /// <summary>The brood the raid kills now (the Skull; raid 5's kill order), or null.</summary>
        public EncounterEnemy BroodKill { get; private set; }
        /// <summary>The boss frame's kill-order line: "Kill: Skull, then Cross, Moon (3 held)"; null with no brood up.</summary>
        public string BroodKillLine
        {
            get
            {
                if (BroodKill == null) return null;
                int held = 0; var marks = new List<string>();
                foreach (var b in BroodNow) if (b != null && b.actor.IsAlive && b != BroodKill) { if (b.Incapacitated) held++; if (b.RaidMarked == RaidMark.Cross || b.RaidMarked == RaidMark.Moon) marks.Add(b.RaidMarked.ToString()); }
                marks.Sort((x, y) => x == y ? 0 : x == "Cross" ? -1 : 1);
                return "Kill: Skull" + (marks.Count > 0 ? ", then " + string.Join(", ", marks) : "") + (held > 0 ? " (" + held + " held)" : "");
            }
        }
        readonly Dictionary<EncounterEnemy, float> broodDeadAt = new Dictionary<EncounterEnemy, float>();
        readonly Dictionary<EncounterEnemy, (bool alive, bool held, float left)> broodWas = new Dictionary<EncounterEnemy, (bool, bool, float)>();
        /// <summary>Holds on the brood that ran their time out (not broken: BroodBrokeFree counts those broken by a blow).</summary>
        public int BroodHoldsRanOut;
        /// <summary>Each brood's last frame: killed (alive to dead) and broke free (its hold ran out, not the Skull's broken by the raid).</summary>
        void CountBrood()
        {
            foreach (var b in BroodNow)
            {
                if (b == null) continue;
                bool alive = b.actor.IsAlive, held = b.Incapacitated; float left = held ? b.ControlSecondsLeft : 0;
                if (broodWas.TryGetValue(b, out var was))
                {
                    if (was.alive && !alive) BroodKilled++;
                    else if (alive && was.held && !held && b != BroodKill) { if (was.left > .3f) BroodBrokeFree++; else BroodHoldsRanOut++; }   // broken by a blow, or its time out
                }
                broodWas[b] = (alive, held, left);
            }
        }
        /// <summary>The kill order kept: the Skull on the first of it (raid 5).</summary>
        void PickBroodKill()
        {
            var list = new List<(string id, bool held, RaidMark mark, float holdLeft)>();
            foreach (var b in BroodNow) if (b != null && b.actor.IsAlive) list.Add((b.persistentId, b.Incapacitated, b == BroodKill ? RaidMark.Skull : b.RaidMarked, b.Incapacitated ? b.ControlSecondsLeft : 0));
            var order = RaidJobs.KillOrder(list);
            var next = order.Count > 0 ? BroodNow.Find(b => b != null && b.persistentId == order[0]) : null;
            if (next == BroodKill) return;
            if (BroodKill != null && BroodKill.actor.IsAlive && BroodKill.RaidMarked == RaidMark.Skull) BroodKill.RaidMarked = RaidMark.None;
            BroodKill = next; if (next != null) next.RaidMarked = RaidMark.Skull;
        }
        void TickBrood(EncounterEnemy e, int phase, float now)
        {
            var t = UT;
            CountBrood();
            // The kill order: a free one first, then the held by their marks (raid 5). A dead Skull passes it on.
            if (BroodKill == null || !BroodKill.actor.IsAlive || !BroodKill.Incapacitated && BroodKill.RaidMarked != RaidMark.Skull || BroodKill.Incapacitated && BroodNow.Exists(b => b != null && b.actor.IsAlive && !b.Incapacitated && !b.Calcified)) PickBroodKill();
            float every = t.BroodEvery(phase);
            if (every > 0 && now >= nextBrood) { nextBrood = now + every; SpawnBrood(e, t.broodCount); }
            for (int i = BroodNow.Count - 1; i >= 0; i--)
            {
                var b = BroodNow[i]; if (b == null) { BroodNow.RemoveAt(i); continue; }
                // A dead brood (its white crystal) is gone 8 s on: nothing of the fight stays on screen ("nothing should be permanent onscreen").
                if (!b.actor.IsAlive) { if (!broodDeadAt.TryGetValue(b, out var died)) broodDeadAt[b] = now; else if (now - died > 8) { BroodNow.RemoveAt(i); broodDeadAt.Remove(b); broodWas.Remove(b); Enemies.Remove(b); if (Target == b) Target = null; Destroy(b.gameObject); } continue; }
                if (!UnravellerRules.BroodEaten(Vector3.Distance(b.transform.position, e.transform.position), t)) continue;
                // A brood that reaches her is eaten: she heals 3% and her blows grow 10%.
                BroodNow.RemoveAt(i); Enemies.Remove(b); if (Target == b) Target = null; Destroy(b.gameObject); BroodEaten++;
                e.actor.Health.ApplyHealing(Mathf.RoundToInt(e.actor.Health.Pool.Max * t.broodEatenHeal)); e.Grow(t.broodEatenGrow);
                FloatText(e.transform.position + Vector3.up * 2, Unraveller.EatsBroodFloat, new Color(.62f, .55f, .8f));
                Message(Unraveller.EatsBroodLine);
            }
        }
        /// <summary>Her brood out of the river (the Drain Courts' kind, level 22, a fifth of a camp elite's health, the Weave-Eaters' book rules),
        /// on both sides of the bar, going for the healers.</summary>
        public List<EncounterEnemy> SpawnBrood(EncounterEnemy mother, int count)
        {
            var made = new List<EncounterEnemy>(); var bar = Zone != null ? Zone.SiltBar : null; var t = UT;
            var healers = PartyActors().Where(a => a != null && a.IsAlive && IsRaidHealer(a)).ToList();
            for (int k = 0; k < count; k++)
            {
                Vector3 want;
                if (bar != null)
                {
                    // Clear of her (run 1: risen beside the bar's middle, they passed by her on the way to the healers and she ate 23 of 36): out
                    // of the river off the bar's corners, one side and the other, one end and the other.
                    float side = k % 2 == 0 ? 1 : -1, end = (k / 2) % 2 == 0 ? 1 : -1;
                    var off = Quaternion.Euler(0, bar.rotation, 0) * new Vector3(side * (bar.size.x / 2 + 3), 0, end * (bar.size.y * .5f - 2) + Random.Range(-2f, 2f));
                    want = Zone.Ground(bar.center) + off;
                }
                else want = mother.transform.position + Quaternion.Euler(0, k * 120, 0) * Vector3.forward * 12;
                if (!NavMesh.SamplePosition(want, out var hit, 5, NavMesh.AllAreas)) continue;
                string id = "mob.unraveller.brood." + BroodRisen;
                var a = SpawnActor(Unraveller.BroodName, content.enemy, hit.position + Vector3.up, Color.grey, id, ActorLook.WeaveEater, t.broodLevel);
                AddAgent(a.gameObject, 3.4f, .45f);
                var b = a.gameObject.AddComponent<EncounterEnemy>(); b.actor = a; b.persistentId = id; b.session = this;
                b.Camp = true; b.RespawnSeconds = 1e9f; b.CampCenter = new Vector2(hit.position.x, hit.position.z); b.CampRadius = 2; b.MobName = Unraveller.BroodName; b.Brood = true; b.NoLoot = true;
                a.gameObject.SetActive(true); b.Initialize(); Enemies.Add(b);
                a.Stats.SetBase(StatType.MaxHealth, Mathf.Max(50, Mathf.RoundToInt(EncounterEnemy.MobHealth(t.broodLevel, false, true, true) * t.broodHealthShare))); a.Health.ApplyHealing(a.Health.Pool.Max);
                b.HitBase = EncounterEnemy.MobHit(t.broodLevel, false, false);
                var goFor = healers.Count > 0 ? healers[k % healers.Count] : Player;
                if (goFor != null) b.threat.Add(goFor.EntityId.Value, 60);
                BroodBody.Dress(b);   // a small purple spider (Chris, play note 260)
                BroodNow.Add(b); made.Add(b); BroodRisen++;
            }
            if (made.Count > 0) { Message(Unraveller.BroodRiseLine); if (PlayerRole == SimCompanion.PartyRole.Healer) RaidWarning(Unraveller.BroodWarning); }
            return made;
        }

        // ---------- unmade ground (phase 2 on) ----------
        void TickUnmade(EncounterEnemy e, int phase, float now)
        {
            var t = UT;
            if (phase >= 2 && now >= nextUnmade)
            {
                nextUnmade = now + t.unmadeEvery;
                var at = e.transform.position; if (NavMesh.SamplePosition(at, out var hit, 3, NavMesh.AllAreas)) at = hit.position;
                // No more than unmadeMost at once (Chris: "some reds under her are staying a long time"): the oldest goes.
                while (unmade.Count >= t.unmadeMost) { if (unmade[0].mark != null) Destroy(unmade[0].mark.gameObject); unmade.RemoveAt(0); }
                var m = BlowMark.Create(GroundKind.Harmful, t.unmadeWarn + t.unmadeLife + 1, Unraveller.UnmadeName); m.BodyColour = UnmadeGrey; m.Show(at, t.unmadeRadius, 0);
                unmade.Add((at, now + t.unmadeWarn, m));
                if (Player != null && Flat(Player.transform.position - at) <= t.unmadeRadius + 1) RaidWarning(Unraveller.UnmadeWarning);
            }
            // Each patch lasts unmadeLife after it bites (Chris, the tuning batch: "also should not stay on the ground permanently": seventeen patches
            // for good had run into one great disc over the bar).
            for (int i = unmade.Count - 1; i >= 0; i--) { var u = unmade[i]; if (now >= u.live + t.unmadeLife) { if (u.mark != null) Destroy(u.mark.gameObject); unmade.RemoveAt(i); } }
            for (int i = 0; i < unmade.Count; i++) { var u = unmade[i]; if (u.mark != null) u.mark.Show(u.at, t.unmadeRadius, Mathf.Clamp01(1 - (u.live - now) / Mathf.Max(.01f, t.unmadeWarn))); }
            if (now < nextGroundBite) return;
            nextGroundBite = now + 1;
            // Once a second: the unmade ground bites (8% of health a second) whoever stands in it.
            foreach (var a in PartyActors().ToArray())
            {
                if (a == null || !a.IsAlive) continue;
                bool inIt = false;
                foreach (var u in unmade) if (now >= u.live && Flat(a.transform.position - Vector3.up - u.at) <= t.unmadeRadius && Mathf.Abs(a.transform.position.y - 1 - u.at.y) < GroundMarks.Height) { inIt = true; break; }
                string id = a.EntityId.Value; groundSeconds.TryGetValue(id, out int stood);
                if (!inIt) { groundSeconds.Remove(id); continue; }
                // The longer one stands in it the worse (raid 4, Chris: "ground damage should be increased"): 15%, then 20%, 25% ... a second.
                Hurt(a, UnravellerRules.GroundBite(stood, t), Unraveller.UnmadeName, false); groundSeconds[id] = stood + 1;
            }
            // And the swallowed, inside her, 4% a second (TickSwallow keeps them there).
            if (SwallowedActor != null && SwallowedActor.IsAlive) Hurt(SwallowedActor, t.swallowShare, null, false);
        }

        // ---------- Swallowed (phase 2 on) ----------
        void TickSwallow(EncounterEnemy e, int phase, float now)
        {
            var t = UT;
            if (SwallowedActor != null)
            {
                if (!SwallowedActor.IsAlive) { Swallow.End(); SwallowedActor = null; SaltCall.Clear(); return; }
                if (Swallow.Erased(now, t))
                {
                    var gone = SwallowedActor; SwallowsErased++;
                    Swallow.End(); SwallowedActor = null; SaltCall.Clear(); ReleaseHeld(gone);
                    gone.Health.ApplyDamage(gone.Health.Pool.Current);
                    Message(Unraveller.SwallowErasedLine(gone == Player ? "You" : gone.DisplayName));
                    return;
                }
                KeepInside(e, SwallowedActor);
                if (now >= nextSaltCall) { nextSaltCall = now + 1; CallSalt(e); }
                return;
            }
            float every = t.SwallowEvery(phase);
            if (every <= 0 || now < nextSwallow) return;
            nextSwallow = now + every;
            var pick = UnravellerRules.SwallowPick(Candidates(e)); var who = ActorOfEntity(pick); if (who == null) return;
            SwallowedActor = who; Swallow.Begin(pick, now);
            if (who == Player) StunPlayer(t.swallowSeconds, Unraveller.Name + " swallows you");
            else { var c = who.GetComponent<SimCompanion>(); if (c != null) c.HoldUntil("stun", t.swallowSeconds, e.transform.position); }
            KeepInside(e, who);
            FloatText(who.transform.position + Vector3.up, "Swallowed", new Color(.62f, .55f, .8f));
            CallSalt(e);
            RaidWarning(who == Player ? Unraveller.SwallowedYou : Unraveller.SaltHer(who.DisplayName));
        }
        /// <summary>The salts asked for (3.5): the three nearest with salt; when you are one of them, two sims and the warning to you.</summary>
        void CallSalt(EncounterEnemy e)
        {
            int need = UT.saltsToFree - Swallow.Salts(Time.time, UT); SaltCall.Clear(); if (need <= 0) return;
            var near = new List<(string id, float distance, bool salt, bool you)>();
            foreach (var c in PartySims)
            {
                if (c == null || c.actor == null || !c.actor.IsAlive || c.actor == SwallowedActor || Swallow.SaltedBy(c.actor.EntityId.Value) || c.HeldNow) continue;
                near.Add((c.sim.id, Vector3.Distance(c.transform.position, e.transform.position), SimEconomy.Count(c.sim, Unraveller.SaltItem) > 0 || ApronStock, false));
            }
            if (Player != null && Player.IsAlive && Player != SwallowedActor && !Swallow.SaltedBy(Player.EntityId.Value))
                near.Add((RaidGroup.You, Vector3.Distance(Player.transform.position, e.transform.position), Inventory.Count(Progress, Unraveller.SaltItem) > 0, true));
            var (sims, you) = RaidJobs.SaltThrowers(near, need);
            SaltCall.AddRange(sims);
            if (you && RaidWarningText == null) RaidWarning(Unraveller.SaltHer(SwallowedActor != null ? (SwallowedActor == Player ? "you" : SwallowedActor.DisplayName) : "someone"));
        }
        /// <summary>Salt thrown at her by <paramref name="by"/> (EncounterEnemy.SaltedHer): counted toward the three; the third frees the swallowed.</summary>
        public bool SaltOnUnraveller(EncounterEnemy e, Actor by)
        {
            if (by == null) return false; SaltsThrown++;
            if (!Swallow.Active) { if (by == Player) Message(Unraveller.SaltIdleLine); return false; }
            bool freed = Swallow.Salt(by.EntityId.Value, Time.time, UT);
            FloatText(e.transform.position + Vector3.up * 1.4f, "Salt " + Swallow.Salts(Time.time, UT) + "/" + UT.saltsToFree, new Color(.97f, .97f, .92f));
            SaltCall.Remove(RaidIdOf(by) ?? "");
            if (!freed) return false;
            var who = SwallowedActor; SwallowsFreed++;
            Swallow.End(); SwallowedActor = null; SaltCall.Clear();
            e.CalcifyHer(UT.calcifySeconds);
            Spit(e, who);
            Message(Unraveller.SpatOutLine(who == Player ? "you" : who != null ? who.DisplayName : "them"));
            return true;
        }
        /// <summary>The swallowed held inside her (where she floats; she may be hauled).</summary>
        void KeepInside(EncounterEnemy e, Actor who)
        {
            if (who == null) return;
            if (!NavMesh.SamplePosition(e.transform.position, out var hit, 3, NavMesh.AllAreas)) return;
            if (Vector3.Distance(who.transform.position, hit.position + Vector3.up) < .8f) return;
            if (who == Player) { var motor = Player.GetComponent<AdventurerMotor>(); if (motor != null) motor.Teleport(hit.position + Vector3.up * 1.05f); }
            else { var ag = who.GetComponent<NavMeshAgent>(); if (ag != null && ag.isOnNavMesh) ag.Warp(hit.position); }
        }
        /// <summary>Spat out whole: let go, two metres clear of her.</summary>
        void Spit(EncounterEnemy e, Actor who)
        {
            if (who == null) return;
            ReleaseHeld(who);
            if (!who.IsAlive || e == null) return;
            var dir = who.transform.position - e.transform.position; dir.y = 0; if (dir.sqrMagnitude < .01f) dir = Vector3.forward;
            var to = e.transform.position + dir.normalized * 3;
            if (!NavMesh.SamplePosition(to, out var hit, 2.5f, NavMesh.AllAreas)) return;
            if (who == Player) { var motor = Player.GetComponent<AdventurerMotor>(); if (motor != null) motor.Teleport(hit.position + Vector3.up * 1.05f); }
            else { var ag = who.GetComponent<NavMeshAgent>(); if (ag != null && ag.isOnNavMesh) ag.Warp(hit.position); }
        }
        void ReleaseHeld(Actor who)
        {
            if (who == null) return;
            if (who == Player) PlayerStunnedUntil = Mathf.Min(PlayerStunnedUntil, Time.time);
            else who.GetComponent<SimCompanion>()?.ReleaseHold();
        }

        // ---------- Hunted (phase 3) ----------
        void TickHunt(EncounterEnemy e, int phase, float now)
        {
            var t = UT;
            if (HuntedActor != null)
            {
                var h = HuntedActor;
                if (huntMark != null && h.IsAlive) huntMark.Show(h.transform.position - Vector3.up, t.huntRadius, Mathf.Clamp01(1 - (huntAt - now) / Mathf.Max(.01f, t.huntWarn)));
                if (now < huntAt) return;
                // She falls on them, pinned or not; the river hides them from her (b1 ch.16:75): she lands on empty water, and misses her meal.
                var landing = h.transform.position; bool inRiver = h.IsAlive && Zone != null && Zone.ChemicalAt(landing);
                HuntedActor = null; huntAt = -1; if (huntMark != null) huntMark.Hide();
                e.DisplaceTo(landing);
                if (UnravellerRules.HuntMissed(inRiver)) { HuntsMissed++; e.RawStun(t.missStun); FloatText(e.transform.position + Vector3.up * 2, Unraveller.MissedFloat, new Color(.75f, .75f, .8f)); Message(Unraveller.MissedLine); }
                else HuntsLanded++;
                foreach (var a in PartyActors().ToArray())
                {
                    if (a == null || !a.IsAlive) continue;
                    int dealt = UnravellerRules.HuntDamage(a.Health.Pool.Max, Flat(a.transform.position - landing), a == h, inRiver, t);
                    if (dealt <= 0) continue;
                    dealt = EncounterEnemy.CapBlow(dealt, a.Health.Pool.Max, true, true);   // a blow meant to kill: it passes the 60% cap
                    a.Health.ApplyDamage(dealt);
                    FloatText(a.transform.position + Vector3.up * .5f, Unraveller.HuntName + " −" + dealt, new Color(.62f, .55f, .8f));
                }
                return;
            }
            if (phase < 3 || now < nextHunt) return;
            nextHunt = now + t.huntEvery;
            var pick = UnravellerRules.HuntPick(Candidates(e)); var who = ActorOfEntity(pick); if (who == null) return;
            HuntedActor = who; huntAt = now + t.huntWarn;
            if (huntMark == null) huntMark = BlowMark.Create(GroundKind.Harmful, UT.huntWarn + 1, "Her hunt");
            huntMark.Show(who.transform.position - Vector3.up, t.huntRadius, 0);
            FloatText(who.transform.position + Vector3.up * 1.2f, "Hunted", new Color(.62f, .55f, .8f));
            if (who == Player) RaidWarning(Unraveller.HuntedYou);
            else Message(Unraveller.HuntedLine(who.DisplayName));
        }

        // ---------- the feast (9:00) ----------
        void Feast(EncounterEnemy e)
        {
            feasted = true; Feasts++;
            ChatSay(ChatChannel.Say, Unraveller.Name, Unraveller.FeastLine);
            Message(Unraveller.FeastSystem);
            foreach (var a in PartyActors().ToArray())
            {
                if (a == null || !a.IsAlive) continue;
                if (Zone != null && Zone.ChemicalAt(a.transform.position)) continue;   // in the river, off the bar
                a.Health.ApplyDamage(a.Health.Pool.Current);
            }
            // A wipe counted here (her fight ends with the raid gone), and she is whole again (or sinks, fed, at the second).
            unravellerOnParty = false;
            var s = WorldBossNow;
            if (s != null && WorldBossClock.Wiped(s, WorldSeed)) UnravellerEnded(Unraveller.SunkSystem);
            else if (e.actor.IsAlive) e.ResetFight();
        }

        // ---------- the rotations (3.5) ----------
        void SetRotations()
        {
            var t = UT;
            var sims = PartySims.Where(c => c != null && c.actor != null && c.actor.IsAlive).ToList();
            // The harpooners: damage-dealers first, those who stand off (an archer, a caster) before those in her reach; you with /raid harpoon.
            var harp = sims.Where(c => c.Role == SimCompanion.PartyRole.Damage).OrderBy(c => c.Melee ? 1 : 0).ThenBy(c => c.sim.id).Select(c => c.sim.id).Take(t.harpooners).ToList();
            if (harp.Count < t.harpooners) harp.AddRange(sims.Where(c => c.Role == SimCompanion.PartyRole.Healer && !harp.Contains(c.sim.id)).Select(c => c.sim.id).Take(t.harpooners - harp.Count));
            if (HarpoonYou) harp.Insert(Mathf.Min(1, harp.Count), RaidGroup.You);
            HarpoonRotation = new RaidRotation(harp);
            // The interrupters: those with a kick, damage-dealers before tanks; never one of the harpooners when another will do.
            var kick = sims.Where(c => c.Interrupter).OrderBy(c => c.Role == SimCompanion.PartyRole.Tank ? 1 : 0).ThenBy(c => harp.Contains(c.sim.id) ? 1 : 0).ThenBy(c => c.sim.id).Select(c => c.sim.id).Take(t.interrupters).ToList();
            InterruptRotation = new RaidRotation(kick);
        }
        /// <summary>Whether a member of the harpoon rotation can take its turn: alive, free (not swallowed, not held), with a harpoon (or the apron's rack out).</summary>
        bool HarpoonAble(string id)
        {
            if (id == RaidGroup.You) return Player != null && Player.IsAlive && Player != SwallowedActor && !PlayerStunned && Inventory.Count(Progress, Unraveller.HarpoonItem) > 0;
            var c = PartySim(id); return c != null && c.actor.IsAlive && c.actor != SwallowedActor && !c.HeldNow && (SimEconomy.Count(c.sim, Unraveller.HarpoonItem) > 0 || ApronStock);
        }
        bool InterruptAble(string id) { var c = PartySim(id); return c != null && c.actor.IsAlive && c.actor != SwallowedActor && !c.HeldNow; }
        /// <summary>Whose turn it is to harpoon her now (a raid id: a sim's, or "you"); null with no rotation.</summary>
        public string HarpoonTurn { get { return HarpoonRotation?.Current(HarpoonAble); } }
        public string InterruptTurn { get { return InterruptRotation?.Current(InterruptAble); } }
        /// <summary>A harpoon of the rotation's thrown: the next one's turn.</summary>
        public void HarpoonTaken(string id) { HarpoonRotation?.Took(id); HarpoonsThrown++; }
        public void InterruptTaken(string id) { InterruptRotation?.Took(id); }
        /// <summary>Tibb's barrel and Ketta's rack are out on the apron (the warning or her rising): a sim out of salt or harpoons takes more there.</summary>
        public bool ApronStock { get { return Zone != null && Zone.ReachStockOut; } }
        /// <summary>The Feeding stopped (an interrupt, or a harpoon in it: "Bone holds").</summary>
        public void FeedingBroken(EncounterEnemy e, Actor by, bool harpoon) { if (!harpoon) return; }
        /// <summary>The Feeding landed: what it ate, said once in the chat.</summary>
        public void FeedingLanded(EncounterEnemy e, int eaten) { if (eaten > 0) Message(Unraveller.FedLine(eaten)); }

        // ---------- shared ----------
        /// <summary>Her blow that is a share of the health of <paramref name="a"/> (the ground, the pulse, the swallowing): under the 60% cap unless it passes it.</summary>
        void Hurt(Actor a, float share, string what, bool passesCap)
        {
            if (a == null || !a.IsAlive || share <= 0) return;
            int raw = Mathf.Max(1, Mathf.RoundToInt(a.Health.Pool.Max * share));
            int dealt = EncounterEnemy.CapBlow(raw, a.Health.Pool.Max, true, passesCap);
            NoteHit(a, (what ?? (Pulses > 0 && share < .1f ? Unraveller.PulseName : "her")) + " " + dealt);
            a.Health.ApplyDamage(dealt);
            if (what != null || a == Player) FloatText(a.transform.position, (what != null ? what + " " : "") + "−" + dealt, new Color(.62f, .55f, .8f));
        }
        /// <summary>Silenced (her touch): you can use no spell, a sim neither mends nor casts, for <paramref name="seconds"/>.</summary>
        public void SilenceMember(Actor v, float seconds, string why)
        {
            if (v == null || !v.IsAlive || seconds <= 0) return;
            if (v == Player) { PlayerSilencedUntil = Mathf.Max(PlayerSilencedUntil, Time.time + seconds); Message(why + ": you are silenced for " + seconds.ToString("0") + " seconds."); }
            else v.GetComponent<SimCompanion>()?.Silence(seconds);
            FloatText(v.transform.position + Vector3.up * .8f, "Silenced", new Color(.75f, .85f, 1));
        }
        /// <summary>The raid as her rules see it (blink, hunt, swallow choices).</summary>
        List<UnravellerCandidate> Candidates(EncounterEnemy e)
        {
            var l = new List<UnravellerCandidate>();
            foreach (var a in PartyActors())
            {
                if (a == null) continue;
                var c = a.GetComponent<Crulanda.Combat.Combatant>();
                l.Add(new UnravellerCandidate
                {
                    id = a.EntityId.Value, alive = a.IsAlive, threat = e.threat.Of(a.EntityId.Value), distance = Vector3.Distance(a.transform.position, e.transform.position),
                    tank = IsRaidTank(a), inRiver = Zone != null && Zone.ChemicalAt(a.transform.position), victim = e.Victim == a, swallowed = a == SwallowedActor, hunted = a == HuntedActor,
                    magic = WorksMagic(a)
                });
            }
            return l;
        }
        /// <summary>
        /// Whether the tank <paramref name="me"/> (you or a sim) takes her now (RaidJobs.TauntHer): the other tank is whoever else of the raid
        /// tanks, you included (run 1: the sim tank looked only among the sims, found none with you tanking, never gave her up and was erased at
        /// 0:21 with seven stacks).
        /// </summary>
        public bool TauntsUnraveller(Actor me, EncounterEnemy e)
        {
            if (me == null || e == null || !IsRaidTank(me)) return false;
            var t = UT; Actor other = null;
            foreach (var a in PartyActors()) if (a != null && a != me && a.IsAlive && IsRaidTank(a)) { other = a; break; }
            int mine = e.UnravelOn(me), theirs = other != null ? e.UnravelOn(other) : 0;
            bool preferred = other == null || mine < theirs || mine == theirs && string.CompareOrdinal(me.EntityId.Value, other.EntityId.Value) < 0;
            return RaidJobs.TauntHer(mine, theirs, e.Victim == me, other != null && e.Victim == other, preferred, t.swapAt, t.erasedAt);
        }
        /// <summary>Whether <paramref name="a"/> works magic as her lighthouse sees it (ch.20:121): you by your pool (mana, salt) or a ward; a sim
        /// by its class (a Mage, Druid, Cleric, Shaman, Salt-Mender or Archivist: the sims all carry the same pool) or a ward.</summary>
        public bool WorksMagic(Actor a)
        {
            if (a == null) return false;
            var c = a.GetComponent<Crulanda.Combat.Combatant>(); int ward = c != null ? c.Barrier : 0;
            var sim = a.GetComponent<SimCompanion>();
            if (sim != null) return ward > 0 || SimCompanion.WorksWeave(sim.sim.classId);
            return WeaveEaterRules.DrawnTo(a.Resource != null ? a.Resource.Kind : ResourceKind.None, ward);
        }
        Actor ActorOfEntity(string id) { if (id == null) return null; foreach (var a in PartyActors()) if (a != null && a.EntityId.Value == id) return a; return null; }
        public bool IsRaidTank(Actor a) { if (a == Player) return PlayerRole == SimCompanion.PartyRole.Tank; var c = a != null ? a.GetComponent<SimCompanion>() : null; return c != null && c.Role == SimCompanion.PartyRole.Tank; }
        bool IsRaidHealer(Actor a) { if (a == Player) return PlayerRole == SimCompanion.PartyRole.Healer; if (Companion != null && a == Companion.actor) return true; var c = a != null ? a.GetComponent<SimCompanion>() : null; return c != null && c.Role == SimCompanion.PartyRole.Healer; }
        string NameOfRaidId(string id) { if (id == RaidGroup.You) return "you"; var c = PartySim(id); return c != null ? c.sim.name : id; }
        /// <summary>The way toward the river's middle from <paramref name="from"/>: to the nearest chemical water within 25 m (zero: none).</summary>
        Vector3 RiverWay(Vector3 from)
        {
            var near = NearestWater(from, 25); if (!near.HasValue) return Vector3.zero;
            var d = near.Value - from; d.y = 0; return d.sqrMagnitude > .01f ? d.normalized : Vector3.zero;
        }
        /// <summary>The nearest walkable point in the river's black water within <paramref name="reach"/> of <paramref name="from"/> (the hunted's
        /// escape; her hauling), or null.</summary>
        public Vector3? NearestWater(Vector3 from, float reach)
        {
            if (Zone == null || !Zone.HasChemical) return null;
            if (Zone.ChemicalAt(from)) return from;
            // Each spot's walkable ground, asked of at an actor's height over it (an actor stands a metre over its feet: ChemicalAt reads it so;
            // run 1's hunted found no water, the point asked at the bank's height read dry over the river).
            for (float r = 2; r <= reach; r += 1.5f)
                for (int k = 0; k < 16; k++)
                {
                    var p = from + Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward * r;
                    if (!NavMesh.SamplePosition(p, out var hit, 2.5f, NavMesh.AllAreas)) continue;
                    if (Zone.ChemicalAt(hit.position + Vector3.up * 1.0f)) return hit.position;
                }
            return null;
        }
        /// <summary>The fight's marks on a raid member for its frame's row (2.4): Unravel stacks, blackened steel, Hunted, Swallowed.</summary>
        public string RaidMarksOf(Actor a)
        {
            if (a == null) return null;
            var e = UnravellerFoe; var parts = new List<string>();
            if (a == SwallowedActor) parts.Add("Swallowed");
            if (a == HuntedActor) parts.Add("Hunted");
            int u = e != null ? e.UnravelOn(a) : 0; if (u > 0) parts.Add("Unravel " + u);
            var c = a.GetComponent<Crulanda.Combat.Combatant>(); int steel = c != null ? WeaveEaterRules.SteelStacks(c.Statuses, Time.time) : 0; if (steel > 0) parts.Add("Steel " + steel);
            return parts.Count > 0 ? string.Join(" · ", parts) : null;
        }
        /// <summary>The boss frame's turn line: whose harpoon is next ("Your harpoon!" when it is yours).</summary>
        public string HarpoonTurnLine { get { var id = HarpoonTurn; return id == null ? null : id == RaidGroup.You ? "Your harpoon!" : "Harpoon: " + NameOfRaidId(id); } }

        /// <summary>
        /// A throw of salt or a bone harpoon at <paramref name="t"/> by <paramref name="by"/> (you from your bags, a sim from its: the same rules;
        /// WeaveEaterRules): the sting, then on a Weave-Eater the hold. On her: a harpoon pins her (the shortening pin, a Feeding broken); salt
        /// counts toward freeing the swallowed and is lost otherwise. On her brood: salt calcifies it to shatter. What the hold was, for the line.
        /// </summary>
        public bool ThrowAt(EncounterEnemy t, string kind, Actor by, int level)
        {
            if (t == null || !t.actor.IsAlive || by == null) return false;
            if (by == Player) Bolt(t, kind == WeaveEaterRules.Harpoon ? BoneYellow : SaltWhite, kind == WeaveEaterRules.Harpoon ? .14f : .22f, kind == WeaveEaterRules.Harpoon);
            bool eater = WeaveEaterRules.IsWeaveEater(t);
            var (sting, hold, seconds) = WeaveEaterRules.Throw(kind, level, eater);
            t.Receive(sting, by);   // the sting first: a blow breaks a hold, so the hold goes on after it
            if (!t.actor.IsAlive) return false;
            if (t.RaidBoss)
            {
                if (kind == WeaveEaterRules.Harpoon) { t.Harpooned(by); return true; }
                return t.SaltedHer(by) || Swallow.Active;
            }
            if (hold == "stun")
            {
                bool held = t.Apply("stun", by, seconds) == null;
                if (held && t.Brood) t.CalcifyBrood(seconds);
                return held;
            }
            if (hold == "root")
            {
                t.Root(seconds); FloatText(t.transform.position, "Pinned", BoneYellow);
                t.Harpooned(by);   // the harpoon's anchor (b1 ch.20:191): no blink for a while after
                return true;
            }
            return false;
        }
    }
}
