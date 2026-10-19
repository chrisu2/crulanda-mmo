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
        public int BroodRisen, BroodEaten, SwallowsFreed, SwallowsErased, HuntsMissed, HuntsLanded, Pulses, Feasts, HarpoonsThrown, SaltsThrown;
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
            var t = UT;
            RaidBoss = e;
            // The phase, and the Feeding's beat with it.
            int phase = UnravellerRules.Phase(e.actor.Health.Pool.Ratio, t);
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
            if (!feasted && UnravellerRules.Feast(e.SincePull, t)) Feast(e);
        }
        /// <summary>The pull: the clock begins, the rotations are set, her track comes up.</summary>
        void BeginUnravellerFight(EncounterEnemy e)
        {
            float now = Time.time; e.PullAt = now; e.RaidPhase = 0; feasted = false;
            BroodRisen = BroodEaten = SwallowsFreed = SwallowsErased = HuntsMissed = HuntsLanded = Pulses = Feasts = HarpoonsThrown = SaltsThrown = 0;
            SetRotations();
            if (Unraveller.FightTrackReady) Crulanda.World.ZoneMusic.FightOverride = Unraveller.FightTrack;
            if (HarpoonRotation != null && HarpoonRotation.Order.Count > 0) Message("Harpoons, in turn: " + string.Join(", ", HarpoonRotation.Order.Select(NameOfRaidId)) + ". Interrupts on the Feeding: " + (InterruptRotation != null && InterruptRotation.Order.Count > 0 ? string.Join(", ", InterruptRotation.Order.Select(NameOfRaidId)) : "nobody") + ".");
        }
        /// <summary>The fight over (EncounterEnemy.ClearRaidFight: won, wiped or reset): the brood sink, the ground is whole, the swallowed and the hunted let go.</summary>
        public void EndUnravellerFight(EncounterEnemy e)
        {
            foreach (var b in BroodNow) if (b != null) { Enemies.Remove(b); if (Target == b) Target = null; Destroy(b.gameObject); }
            BroodNow.Clear();
            foreach (var u in unmade) if (u.mark != null) Destroy(u.mark.gameObject);
            unmade.Clear();
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
        void TickBrood(EncounterEnemy e, int phase, float now)
        {
            var t = UT;
            float every = t.BroodEvery(phase);
            if (every > 0 && now >= nextBrood) { nextBrood = now + every; SpawnBrood(e, t.broodCount); }
            for (int i = BroodNow.Count - 1; i >= 0; i--)
            {
                var b = BroodNow[i]; if (b == null) { BroodNow.RemoveAt(i); continue; }
                if (!b.actor.IsAlive) continue;
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
                    float side = k % 2 == 0 ? 1 : -1, along = (k / 2 - .5f) * bar.size.y * .35f + Random.Range(-3f, 3f);
                    var off = Quaternion.Euler(0, bar.rotation, 0) * new Vector3(side * (bar.size.x / 2 + 3), 0, along);
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
                var m = BlowMark.Create(); m.Show(at, t.unmadeRadius, 0);
                unmade.Add((at, now + t.unmadeWarn, m));
                if (Player != null && Flat(Player.transform.position - at) <= t.unmadeRadius + 1) RaidWarning(Unraveller.UnmadeWarning);
            }
            for (int i = 0; i < unmade.Count; i++) { var u = unmade[i]; if (u.mark != null) u.mark.Show(u.at, t.unmadeRadius, Mathf.Clamp01(1 - (u.live - now) / Mathf.Max(.01f, t.unmadeWarn))); }
            if (now < nextGroundBite) return;
            nextGroundBite = now + 1;
            // Once a second: the unmade ground bites (8% of health a second) whoever stands in it.
            foreach (var a in PartyActors().ToArray())
            {
                if (a == null || !a.IsAlive) continue;
                foreach (var u in unmade) if (now >= u.live && Flat(a.transform.position - Vector3.up - u.at) <= t.unmadeRadius && Mathf.Abs(a.transform.position.y - 1 - u.at.y) < GroundMarks.Height) { Hurt(a, t.unmadeShare, Unraveller.UnmadeName, false); break; }
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
            if (huntMark == null) huntMark = BlowMark.Create();
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
        bool IsRaidTank(Actor a) { if (a == Player) return PlayerRole == SimCompanion.PartyRole.Tank; var c = a != null ? a.GetComponent<SimCompanion>() : null; return c != null && c.Role == SimCompanion.PartyRole.Tank; }
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
            for (float r = 2; r <= reach; r += 1.5f)
                for (int k = 0; k < 16; k++)
                {
                    var p = from + Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward * r;
                    if (!Zone.ChemicalAt(p)) continue;
                    if (NavMesh.SamplePosition(p, out var hit, 1.5f, NavMesh.AllAreas) && Zone.ChemicalAt(hit.position)) return hit.position;
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
