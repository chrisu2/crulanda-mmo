using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The group run at 10 (Docs/WORLD_BOSS_1.md 3.6 point 3, build step 6): Crulanda.exe --crulanda-run-capture &lt;folder&gt;
    /// --crulanda-zone zone.lowtowns --crulanda-raid [--crulanda-raid-pulls 10] [--crulanda-unraveller-now] (tools\validation\group_run.ps1
    /// -Raid). You (the bot) at 20 and nine sims at 20 (2 tanks, 3 healers, 5 damage with you; the best Lowtowns gear the sims' kit gives, each
    /// with three salt and three harpoons), formed into a raid on the Drain Reach's apron; she is brought up at once; then pull after pull, a
    /// seed each, until the pulls are done (a wipe or the feast ends one; she is brought up again after she sinks). The bot fights her: it
    /// keeps her targeted, steps out of marked ground, runs for the river when hunted, salts her when someone is swallowed and it is asked.
    /// raid-log.txt has every pull's phases, deaths (with what was on at the time: the read round the first death) and end; run-summary.txt
    /// the kill rate against the targets of 3.6 and the mechanics' counts. RUN_CAPTURE_DONE in the log.
    /// </summary>
    public sealed partial class RunCapture
    {
        public const string RaidFlag = "--crulanda-raid", PullsFlag = "--crulanda-raid-pulls";
        public static bool RaidRequested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), RaidFlag) >= 0; } }
        static int PullsArg { get { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, PullsFlag); return i >= 0 && i + 1 < a.Length && int.TryParse(a[i + 1], out var n) ? Mathf.Clamp(n, 1, 50) : 10; } }
        /// <summary>The game minutes one pull may take before it is called (the enrage is 9: a pull past 10 is stuck).</summary>
        public const float PullLimit = 17 * 60;   // the feast at 16:00 (play note 286)

        public IEnumerator RaidRun(EncounterSession s, string directory)
        {
            Directory.CreateDirectory(directory);
            var log = new List<string>(); Action<string> Log = line => { log.Add(line); Debug.Log("Raid capture: " + line); };
            string Clock(float t) { int m = (int)(t / 60); return m + ":" + ((int)(t % 60)).ToString("00"); }
            Crulanda.World.WorldClock.Hour = 11; EncounterHud.UiScale = 1; EncounterHud.DefaultPanels = true; s.MeterOpen = true;
            if (s.ZoneId != Unraveller.Zone || s.Zone == null || s.Zone.SiltBar == null) { Log("Not in the Lowtowns' Drain Reach (run with --crulanda-zone zone.lowtowns)."); RaidFinish(s, directory, log, new List<string> { "No raid: not in the Lowtowns." }); yield break; }
            int level = EncounterProgress.LevelCap;
            s.Progress.experience = EncounterProgress.XpForLevel(level); s.Player.SetLevel(level); s.LevelHeld = true;
            Log("Your talents: " + SpendForRole(s) + ".");
            SimCompanion.KitPlayerLike(s, new SimAdventurer { classId = s.ClassDef.id, level = level, gearSeed = 7, gearBonus = SimGear.RaidGear }, s.PlayerRole == SimCompanion.PartyRole.Tank);   // a tank's kit for the tank bot
            { var w = new SimAdventurer { classId = "class.warrior", level = level, gearSeed = 7, gearBonus = SimGear.RaidGear };
              Log("Your kit (" + s.PlayerRole + "): " + s.Player.Health.Pool.Max + " health, armour " + s.Player.Stats.GetRounded(Crulanda.Core.StatType.Armor) + "; a Warrior sim tank of " + level + " in the same pieces: " + SimCompanion.MaxHealthFor(w, s.Items) + " health, armour " + SimCompanion.ArmourFor(w, s.Items) + "."); }
            Inventory.Add(s.Progress, s.Items, Unraveller.SaltItem, 6); Inventory.Add(s.Progress, s.Items, Unraveller.HarpoonItem, 6);
            // The bot's raid food, elixirs and draughts (raid 6: none were used), enough for every pull.
            { int pullsWanted = PullsArg; var all = s.Items.Items.Values;
              void Give(ItemDef d, int n) { if (d != null) Inventory.Add(s.Progress, s.Items, d.id, n); }
              Give(BossBuffs.FeastFor(level, s.Items), pullsWanted); Give(BossBuffs.PickDish(all, level), pullsWanted);
              Give(BossBuffs.PickElixir(s.PlayerRole, BossBuffs.MainStat(s.ClassDef.id), all, level), pullsWanted); Give(BossBuffs.PickDraught(Consumables.PoolOf(s.ClassDef), all, level), pullsWanted); }
            // The nine: two tanks, three healers, five damage with you, from the roster, all at 20 in the Lowtowns.
            var pop = SimPopulation.Active; if (pop == null) { Log("No sims."); RaidFinish(s, directory, log, new List<string> { "No sims." }); yield break; }
            var you = s.PlayerRole;
            var want = new List<string>();
            for (int i = 0; i < 2 - (you == SimCompanion.PartyRole.Tank ? 1 : 0); i++) want.Add("class.warrior");
            string[] heal = { "class.cleric", "class.druid", "class.shaman" };
            for (int i = 0; i < 3 - (you == SimCompanion.PartyRole.Healer ? 1 : 0); i++) want.Add(heal[i]);
            string[] dps = { "class.ranger", "class.mage", "class.rogue", "class.scoundrel", "class.archivist" };
            for (int i = 0; i < 5 - (you == SimCompanion.PartyRole.Damage ? 1 : 0); i++) want.Add(dps[i]);
            var chosen = new List<SimAdventurer>();
            foreach (var c in want) { var x = pop.World.sims.Find(y => y.classId == c && !chosen.Contains(y)); if (x != null) chosen.Add(x); else Log("no sim of " + c + " in the roster"); }
            var apron = s.Zone.ReachApron; var bar = s.Zone.SiltBar;
            var stand = s.Zone.Ground(apron != null ? new Vector2(apron.center.x, bar.center.y) : bar.center);   // on the apron across from the bar
            if (NavMesh.SamplePosition(stand, out var standHit, 4, NavMesh.AllAreas)) stand = standHit.position;
            var motor = s.Player.GetComponent<AdventurerMotor>(); motor.Teleport(stand + Vector3.up * 1.05f);
            int k = 0;
            foreach (var x in chosen) { x.zone = s.ZoneId; x.onlineFrom = 0; x.onlineHours = 24; x.friendly = .9f; x.level = level; x.bold = .5f + .05f * (k % 5); x.gearBonus = Mathf.Max(x.gearBonus, SimGear.RaidGear); /* raid gear: a rare of its level in every slot */ x.coin = Mathf.Max(x.coin, 400); var at = stand + Quaternion.Euler(0, k * 40, 0) * Vector3.forward * 3; x.x = at.x; x.z = at.z; k++; SimEconomy.Stock(x, s.Items); RaidCall.BuyKit(x, id => s.Items?.Get(id)?.value ?? 1); }
            pop.Refresh(); yield return new WaitForSeconds(.5f);
            s.FormRaid();
            foreach (var x in chosen) if (!s.Invite(x.id, true)) Log("could not invite " + x.name);
            if (s.InRaid) s.Raid.Arrange();
            yield return new WaitForSeconds(.5f);
            Log("Gear: " + string.Join("; ", s.PartySims.Where(c => c != null).Select(c => { var ids = SimGear.For(c.sim, s.Items); var ds = ids.Select(id => s.Items.Get(id)).Where(d => d != null).ToList(); return c.sim.name.Split(' ')[0] + " " + ds.Count + " pieces, level " + (ds.Count > 0 ? ds.Average(d => d.level).ToString("0.0") : "-") + ", " + ds.Count(d => d.quality >= 3) + " rare+"; })) + ".");
            Log("The raid: you (" + s.ClassDef.displayName + ", " + you + ", " + s.Player.Health.Pool.Max + " health), " + string.Join(", ", s.PartySims.Where(c => c != null).Select(c => c.sim.name + " (" + SimRoster.ClassName(c.sim.classId) + ", " + c.Role + ", party " + (s.InRaid ? s.Raid.PartyOf(c.sim.id) + 1 : 1) + ", " + c.actor.Health.Pool.Max + " health)")) + ".");
            aliveSeconds.Clear(); int pulls = PullsArg, wins = 0; var lines = new List<string>(); float killTime = 0; var buffLines = new List<string>();
            var tune = UnravellerTuning.Current;
            Log("Her numbers (EncounterContent/Raids/unraveller.json): health " + tune.health + ", enrage " + tune.enrageSeconds + " s, blink every " + tune.blinkEvery + " s, pins " + tune.pinFirst + " to " + tune.pinLeast + " s.");
            Time.timeScale = Speed; int shots = 0;
            for (int pull = 1; pull <= pulls; pull++)
            {
                UnityEngine.Random.InitState(1000 + pull);
                // Her up, everyone whole and on the apron.
                if (s.UnravellerFoe == null || !s.UnravellerFoe.actor.IsAlive) s.UnravellerSoon(5);
                float waitFrom = Time.time;
                while ((s.UnravellerFoe == null || s.UnravellerRising || !s.UnravellerFoe.actor.IsAlive) && Time.time - waitFrom < 120) yield return null;
                var e = s.UnravellerFoe; if (e == null) { Log("Pull " + pull + ": she did not rise."); break; }
                // Every pull a fresh one (tune-11: her second wipe of the visit sank her at the next pull's start, read as "KILL in 0:01").
                var clock = s.WorldBossNow; if (clock != null) clock.wipes = 0;
                if (!s.Player.IsAlive) s.Recover();
                if (!s.Player.IsAlive) { s.Player.Health.Revive(s.Player.Health.Pool.Max); Log("Pull " + pull + ": you were still down; up by the run"); }
                motor.Teleport(stand + Vector3.up * 1.05f);
                foreach (var c in s.PartySims) if (c != null) { if (!c.actor.IsAlive) c.actor.Health.Revive(c.actor.Health.Pool.Max); c.actor.Health.ApplyHealing(c.actor.Health.Pool.Max); c.actor.Resource?.Pool.Fill(); RaidCall.BuyKit(c.sim, id => s.Items?.Get(id)?.value ?? 1); var ag = c.GetComponent<NavMeshAgent>(); if (ag != null && ag.isOnNavMesh && NavMesh.SamplePosition(stand + UnityEngine.Random.insideUnitSphere * 4, out var h, 4, NavMesh.AllAreas)) ag.Warp(h.position); }
                s.Player.Health.ApplyHealing(s.Player.Health.Pool.Max); s.Player.Resource.Pool.Fill();
                if (Inventory.Count(s.Progress, Unraveller.SaltItem) < 3) Inventory.Add(s.Progress, s.Items, Unraveller.SaltItem, 3);
                yield return new WaitForSeconds(1);
                // The buff-up (raid 6): the sims restock their packs (their coin), everyone buffs, a ready check, and the pull waits for it.
                foreach (var c in s.PartySims) if (c != null) { c.sim.coin = Mathf.Max(c.sim.coin, 400); SimEconomy.Stock(c.sim, s.Items); }
                // Every pull buffs (tune-4's pull 2 buffed nobody: the session was still "in a fight" with a straggler). Anything still on the raid is
                // let go, you stop your attack, and the buff-up waits for the quiet.
                s.StopAutoAttack(); s.Select(null);
                foreach (var o in s.Enemies.ToArray()) if (o != null && o.actor.IsAlive && o.FightingParty) o.ResetFight();
                { float quiet = Time.time; while (s.InCombat && Time.time - quiet < 30) yield return null; if (s.InCombat) Log("Pull " + pull + ": still in a fight after 30 s; buffing all the same"); }   // out of the fight first (tune-2: pull 2's buff-up came while the brood still fought, and nobody buffed)
                foreach (var line in s.RaidBuffUp()) Log("Pull " + pull + " buff-up: " + line);
                yield return new WaitForSeconds(1);
                if (s.ReadyCheck()) { float rc = Time.time; while (s.Raid.Checking && Time.time - rc < 12) yield return null; }
                int cb = 0, fedN = 0, elixN = 0, members = 0;
                foreach (var a in s.PartyActors()) { if (a == null || !a.IsAlive) continue; var b = s.BuffsOn(a); members++; cb += b.classBuffs; if (b.fed) fedN++; if (b.elixir) elixN++; }
                string buffs = members + " up: class buffs " + (members > 0 ? (cb / (float)members).ToString("0.0") : "0") + " each, fed " + fedN + ", elixir " + elixN;
                Log("Pull " + pull + " buffs: " + buffs + "; " + string.Join("; ", s.PartyActors().Where(a => a != null && a.IsAlive).Select(a => { var b = s.BuffsOn(a); return (a == s.Player ? "you" : a.DisplayName.Split(' ')[0]) + " " + b.classBuffs + (b.fed ? " fed" : "") + (b.elixir ? " elixir" : ""); })));
                buffLines.Add("Pull " + pull + ": " + buffs);
                // Her again, as she is now (she may have sunk and risen while the raid buffed): the pull is on her or on nobody.
                e = s.UnravellerFoe;
                if (e == null || !e.actor.IsAlive || s.UnravellerSinking || s.UnravellerRising) { Log("Pull " + pull + ": she was not up at the pull; not counted"); lines.Add("Pull " + pull + ": not pulled (she was not up)"); continue; }
                int killsBefore = clock != null ? clock.kills : 0;
                // The pull: the tanks go first; the bot follows a moment after.
                var tanks = s.PartySims.Where(c => c != null && c.Role == SimCompanion.PartyRole.Tank).ToList();
                foreach (var tk in tanks) e.threat.Add(tk.actor.EntityId.Value, 50 - 10 * tanks.IndexOf(tk));
                s.Select(e); s.BeginAutoAttack();
                Log("Pull " + pull + " (seed " + (1000 + pull) + "): she is pulled.");
                Shot(s, directory, "pull" + pull + "-0-pulled", ref shots, null);
                int rezzesBefore = s.Rezzes;
                float start = Time.time, nextCast = 0, nextLog = Time.time + 30; int phaseSeen = 1; var dead = new HashSet<string>(); string firstDeath = null;
                bool won = false, ended = false; float lastRatio = 1;
                while (Time.time - start < PullLimit)
                {
                    float t = Time.time - start;
                    // A kill only when she was killed (the clock's kills went up): not a sinking, not a body gone (tune-11's "KILL in 0:01").
                    if (e == null || !e.actor.IsAlive) { won = clock == null ? e != null : clock.kills > killsBefore; if (!won) ended = true; break; }
                    if (e.PullAt >= 0) lastRatio = e.actor.Health.Pool.Ratio;   // her health as the fight ends (a wipe makes her whole again)
                    if (Time.time - start > 5 && e.PullAt < 0) { ended = true; break; }   // her fight over without her death: a wipe, the feast, a reset
                    if (e.RaidPhase > phaseSeen) { phaseSeen = e.RaidPhase; Log(Clock(t) + "  phase " + phaseSeen + " (" + UnravellerRules.PhaseNames[phaseSeen] + ") at " + Mathf.RoundToInt(e.actor.Health.Pool.Ratio * 100) + "%"); Shot(s, directory, "pull" + pull + "-phase" + phaseSeen, ref shots, null); }
                    foreach (var a in s.PartyActors())
                    {
                        if (a == null || a.IsAlive || !dead.Add(a.EntityId.Value)) continue;
                        string who = a == s.Player ? "you" : a.DisplayName; s.LastHitBy.TryGetValue(a.EntityId.Value, out var lastHit);
                        string line = Clock(t) + "  DEATH " + who + " (" + dead.Count + " down; her " + Mathf.RoundToInt(e.actor.Health.Pool.Ratio * 100) + "%, phase " + e.RaidPhase + "; pin " + e.PinLeft.ToString("0.0") + " s, unravel on it " + e.UnravelOn(a) + ", feeding " + (e.Feeding ? "yes" : "no") + ", brood up " + s.BroodNow.Count(b => b != null && b.actor.IsAlive) + ", unmade " + s.UnmadePatches + (s.SwallowedActor != null ? ", swallowed " + s.SwallowedActor.DisplayName : "") + (s.HuntedActor != null ? ", hunting " + s.HuntedActor.DisplayName : "") + (lastHit != null ? "; last hit: " + lastHit : "") + ")";
                        Log(line); if (firstDeath == null) firstDeath = line;
                    }
                    if (Time.time >= nextLog) { nextLog = Time.time + 30; Log(Clock(t) + "  her " + Mathf.RoundToInt(e.actor.Health.Pool.Ratio * 100) + "%, enrage in " + Clock(s.RaidBossEnrage) + ", pin " + e.PinLeft.ToString("0") + " s (harpoons " + s.HarpoonsThrown + "), feedings landed " + e.FeedingsLanded + " stopped " + e.FeedingsStopped + ", grown " + Mathf.RoundToInt(e.Growth * 100) + "%; " + s.PartySims.Count(c => c != null && c.actor.IsAlive) + "/" + s.PartySims.Count + " sims up"); }
                    BotRaidStep(s, e, motor, ref nextCast);
                    foreach (var a in s.PartyActors()) if (a != null && a.IsAlive) { aliveSeconds.TryGetValue(a.EntityId.Value, out var was); aliveSeconds[a.EntityId.Value] = was + Time.deltaTime; }
                    yield return null;
                }
                EncounterInput.Scripted = Vector2.zero;
                float took = Time.time - start;
                if (won) { wins++; killTime += took; }
                string end = won ? "KILL in " + Clock(took) : ended ? "WIPE at " + Clock(took) + " (her at " + Mathf.RoundToInt(lastRatio * 100) + "%" + (s.Feasts > 0 ? ", the feast" : "") + ")" : "CALLED at " + Clock(took) + " (stuck)";
                string counts = "brood " + s.BroodRisen + " (killed " + s.BroodKilled + ", holds broken by a blow " + s.BroodBrokeFree + ", holds run out " + s.BroodHoldsRanOut + ", eaten " + s.BroodEaten + "), swallowed freed " + s.SwallowsFreed + " erased " + s.SwallowsErased + ", hunts missed " + s.HuntsMissed + " landed " + s.HuntsLanded + ", harpoons " + s.HarpoonsThrown + ", salts " + s.SaltsThrown + ", pulses " + s.Pulses + ", spike bursts " + s.SpikeBursts + " (hit " + s.SpikeHits + "), raised " + (s.Rezzes - rezzesBefore) + ", leaps " + s.Leaps + ", snares " + s.SnaresLaid + " (caught " + s.SnaresCaught + "); in the red: " + (s.HarmSeconds.Count == 0 ? "nobody" : string.Join(", ", s.HarmSeconds.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value.ToString("0") + " s")));
                Log("Pull " + pull + ": " + end + "; " + counts + (firstDeath != null ? "; first death: " + firstDeath.Trim() : ""));
                lines.Add("Pull " + pull + ": " + end + "; deaths " + dead.Count + "; " + counts);
                Shot(s, directory, "pull" + pull + "-end-" + (won ? "kill" : "wipe"), ref shots, null);
                if (!won && e != null && e.actor.IsAlive && (e.Engaged || e.actor.Health.Pool.Ratio < 1)) e.ResetFight();   // a wipe: she is whole for the next pull
                yield return new WaitForSeconds(8);   // the sims get up, out of the fight
            }
            EncounterInput.Scripted = null; Time.timeScale = 1;
            var summary = new List<string> {
                "Group run at 10: the Unraveller (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")",
                "Raid: you (" + s.ClassDef.displayName + ", " + you + ") and " + string.Join(", ", s.PartySims.Where(c => c != null).Select(c => c.sim.name + " the " + SimRoster.ClassName(c.sim.classId) + " (" + c.Role + ")")),
                "Kills: " + wins + " of " + pulls + " (" + Mathf.RoundToInt(100f * wins / Mathf.Max(1, pulls)) + "%); the target with the bot playing well is about 50% (WORLD_BOSS_1.md 3.6), sims alone 30-40%",
                "Mean kill time: " + (wins > 0 ? Clock(killTime / wins) : "-") + " against the enrage at " + Clock(tune.enrageSeconds),
                "Pulls:" };
            summary.AddRange(lines.Select(l => "  " + l));
            summary.Add("Buffs at each pull (class buffs laid raid-wide, food, elixir): " + string.Join(" | ", buffLines));
            summary.Add("Damage against the bands (BALANCE_REPORT.md: a pure damage-dealer the band, a hybrid " + Mathf.RoundToInt(EncounterSession.HybridDpsShare * 100) + "% of it, the Archivist (control and support) " + Mathf.RoundToInt(SimCompanion.ControlDpsShare * 100) + "%, a tank " + Mathf.RoundToInt(SimCompanion.TankDpsShare * 100) + "%, a healer low; the band at " + level + " is " + SimCompanion.PureDps(level).ToString("0") + " a second on one target; her fight costs everyone some for moving, salting and harpooning):");
            summary.AddRange(BandLines(s, level).Select(l => "  " + l));
            summary.Add("Scorecard:"); summary.AddRange(s.Meter.Scorecard().Select(l => "  " + l));
            RaidFinish(s, directory, log, summary);
        }
        /// <summary>Each member's damage a second over the fighting time against what its class and part should do (the bands), worst first.</summary>
        /// <summary>Each member's living seconds in the pulls (its damage a living second beside its damage a fighting second: tune-4).</summary>
        static readonly Dictionary<string, float> aliveSeconds = new Dictionary<string, float>();
        static List<string> BandLines(EncounterSession s, int level)
        {
            var l = new List<(float share, string line)>(); float secs = s.Meter.CombatSeconds, band = SimCompanion.PureDps(level);
            foreach (var r in s.Meter.Rows)
            {
                string classId = null; SimCompanion.PartyRole role; var sim = s.PartySims.Find(c => c != null && c.actor != null && c.actor.EntityId.Value == r.id);
                if (sim != null) { classId = sim.sim.classId; role = sim.Role; }
                else if (s.Player != null && s.Player.EntityId.Value == r.id) { classId = s.ClassDef.id; role = s.PlayerRole; }
                else continue;   // a pet: in its keeper's
                float want = role == SimCompanion.PartyRole.Tank ? band * SimCompanion.TankDpsShare : role == SimCompanion.PartyRole.Healer ? band * .25f : band * (classId == "class.archivist" ? SimCompanion.ControlDpsShare : EncounterSession.Hybrid(classId) ? EncounterSession.HybridDpsShare : 1);
                float dps = r.Dps(secs), share = want > 0 ? dps / want : 0;
                aliveSeconds.TryGetValue(r.id, out var lived); float livingDps = lived > 1 ? r.damage / lived : 0;
                string note = role == SimCompanion.PartyRole.Healer ? (dps > want * 1.2f ? "HIGH for a healer" : "ok (a healer's)") : share > 1.1f ? "OVER" : share < .7f ? "UNDER" : "in band";
                l.Add((share, r.name + ", " + role + ": " + dps.ToString("0.0") + "/s against " + want.ToString("0") + " (" + Mathf.RoundToInt(share * 100) + "%) " + note + (r.healing > 0 ? "; heals " + r.Hps(secs).ToString("0.0") + "/s" : "") + "; per living second " + livingDps.ToString("0.0") + "/s (" + Mathf.RoundToInt(want > 0 ? 100 * livingDps / want : 0) + "%, alive " + Mathf.RoundToInt(lived) + " s)" + (r.healing > 0 && lived > 1 ? ", heals " + (r.healing / lived).ToString("0.0") + "/s" : "") + "; deaths " + r.deaths));
            }
            return l.OrderBy(x => x.share).Select(x => x.line).ToList();
        }
        static float botTauntAt, botStuckSince = -1; static Vector3 botStuckFrom;
        /// <summary>The bot's frame in her fight: out of marked ground, into the river when hunted, salt on her when asked, else at her (its reach)
        /// and its abilities.</summary>
        static void BotRaidStep(EncounterSession s, EncounterEnemy e, AdventurerMotor motor, ref float nextCast)
        {
            if (!s.Player.IsAlive) { EncounterInput.Scripted = Vector2.zero; botStuckSince = -1; return; }
            // Stuck 10 s far from her (tune-9, Chris: "and stuck"): put back beside the raid.
            {
                var at = s.Player.transform.position; float far = Vector3.Distance(at, e.transform.position);
                if (far < 20 || botStuckSince < 0 || (at - botStuckFrom).magnitude > 1.5f) { botStuckFrom = at; botStuckSince = Time.time; }
                else if (Time.time - botStuckSince > 10)
                {
                    var side = at - e.transform.position; side.y = 0; side = side.sqrMagnitude > .01f ? side.normalized : Vector3.left;
                    if (NavMesh.SamplePosition(e.transform.position + side * (e.BodyRadius + 6), out var back, 6, NavMesh.AllAreas)) { motor.Teleport(back.position + Vector3.up * 1.05f); Debug.Log("Raid capture: the bot was stuck " + Mathf.RoundToInt(far) + " m from her: put back by the raid"); }
                    botStuckSince = Time.time; botStuckFrom = s.Player.transform.position;
                }
            }
            var me = s.Player.transform.position; Vector3? goal = null; float reach; bool inRed = false;
            if (s.HuntedActor == s.Player) { goal = s.NearestWater(me, 25); reach = .5f; }
            else
            {
                var feet = NavMesh.SamplePosition(me, out var under, 2, NavMesh.AllAreas) ? under.position : me - Vector3.up;
                var outOf = GroundMarks.StepOut(feet);
                if (outOf.HasValue) { goal = outOf; reach = .3f; inRed = true; s.CountHarm(s.Player, Time.deltaTime); }
                else
                {
                    goal = e.transform.position; reach = (MeleeClass(s) ? 2.4f : 12) + e.BodyRadius;   // to her edge
                    // Never into the red to strike: a clear spot in reach (Chris: "they need to evacuate").
                    var clear = GroundMarks.StrikeSpot(me, e.transform.position, e.BodyRadius, MeleeClass(s) ? 2.4f : 12);
                    // With marks about her, it stands on the nearest clear spot of her edge (tune-4: the bot tank stopped at her edge in her grey
                    // ground, stepped out, came back to the same spot, and died to it: "last hit: Unmade ground 457-627").
                    if (clear.HasValue) { goal = clear; reach = .5f; }
                }
            }
            // Salt on her when the raid warning asks it of you.
            var w = s.RaidWarningText;
            if (s.SwallowedActor != null && s.SwallowedActor != s.Player && w != null && w.StartsWith("Salt her") && !s.Swallow.SaltedBy(s.Player.EntityId.Value) && Vector3.Distance(me, e.transform.position) <= WeaveEaterRules.ThrowRange)
            {
                int bag = s.Progress.bag.FindIndex(b => !b.Empty && b.item == Unraveller.SaltItem);
                if (bag >= 0) { s.Select(e); s.UseItem(bag); }
            }
            if (s.Target != e) s.Select(e);
            if (!s.AutoAttack) s.BeginAutoAttack();
            // A tank bot takes her as a sim tank does (the swap at four stacks; from a caster she blinked onto): a taunt, every 8 s at most.
            if (Time.time >= botTauntAt && s.TauntsUnraveller(s.Player, e) && Vector3.Distance(me, e.transform.position) < 15)
            { botTauntAt = Time.time + 8; e.threat.Taunt(s.Player.EntityId.Value, Time.time, 4); s.FloatText(me + Vector3.up * 2, "Taunt", new Color(1, .7f, .3f)); }
            if (!inRed && Time.time >= nextCast) { nextCast = Time.time + .4f; BotCast(s); }   // out of the red first, then the abilities
            if (!goal.HasValue) { EncounterInput.Scripted = Vector2.zero; return; }
            var to = goal.Value - me; to.y = 0;
            if (to.magnitude <= reach || s.PlayerStunned || s.PlayerRooted) { EncounterInput.Scripted = Vector2.zero; return; }
            motor.SetView(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 14, 7);
            EncounterInput.Scripted = Vector2.up;
        }
        void RaidFinish(EncounterSession s, string directory, List<string> log, List<string> summary)
        {
            File.WriteAllLines(Path.Combine(directory, "run-summary.txt"), summary);
            File.WriteAllLines(Path.Combine(directory, "raid-log.txt"), log);
            Debug.Log("RUN_CAPTURE_DONE " + string.Join(" | ", summary));
            Application.Quit(0);
        }
    }
}
