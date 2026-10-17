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
    /// A group's run of the zone's dungeon, timed and photographed (Chris, 2026-10-11: "need to show a test run of the dungeon with a
    /// group"): Crulanda.exe --crulanda-run-capture &lt;folder&gt; --crulanda-zone zone.adit, windowed. You at level 11 with three sims
    /// invited (a Druid to heal, a Mage and a Ranger), the gates open (the run's quests are not done here), a sim leading the run camp by
    /// camp (/dungeon); you follow and fight; dead, you recover at the stone and the run goes on. The game runs at three times speed; the
    /// times written are the game's. run-log.txt has a line per camp cleared and per death, run-summary.txt the whole; a shot every four
    /// game minutes and at each elite. RUN_CAPTURE_DONE in the log.
    /// (The Adit rework) A dungeon of wings is run as the leader plans it (DungeonPlan): a line as each wing begins, each camp's place in its
    /// wing, each regroup and each wait at a gate. --crulanda-run-mode quick|full picks the run (the standard run without it). The gates the
    /// run's quests or hands open stand open; the bosses' doors are left to the bosses (a door still shut after a minute's wait is opened for
    /// the run, and the log says so: a boss's death should have opened it).
    /// (Buffs to beat the bosses, 2026-10-15; BossBuffs) Before a boss or an elite camp the leader stops short to buff up: the bot buffs
    /// from its bags as /buff does (the run gives it a feast, a dish, its part's elixir and its draught of its level's tier at the start;
    /// the sims carry packs bought with their coin), and the log has a "buffed:" line for each member at each stop.
    /// </summary>
    public sealed class RunCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-run-capture", ModeFlag = "--crulanda-run-mode";
        /// <summary>The run asked for (--crulanda-run-mode quick|full), or null for the standard run.</summary>
        static string ModeArg { get { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, ModeFlag); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; } }
        string wingsRun;
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }
        public const float Speed = 3, RealLimitSeconds = 50 * 60, ShotEvery = 240;

        public IEnumerator Run(EncounterSession s, string directory)
        {
            Directory.CreateDirectory(directory);
            var log = new List<string>(); Action<string> Log = line => { log.Add(line); Debug.Log("Run capture: " + line); };
            string Clock(float t) { int m = (int)(t / 60); return m + ":" + ((int)(t % 60)).ToString("00"); }
            Crulanda.World.WorldClock.Hour = 11; EncounterHud.UiScale = 1; EncounterHud.DefaultPanels = true; s.MeterOpen = true;   // the HUD in frame at 1440x900 (the party frame was cut off), the meter up
            var weather = Crulanda.World.WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            // The party's level: the middle of the zone's band, rounded down (the Adit 10-12: eleven, as before; Hallow's Creek 15-20:
            // seventeen, the level it is entered at; nineteen walked over its first wings, Chris 2026-10-16: "still not balanced").
            var zd = s.Zone.Zone; int runLevel = Mathf.Clamp(zd.levelMax > 0 ? (zd.levelMin + zd.levelMax) / 2 : 11, 2, EncounterProgress.LevelCap);
            if (s.Progress.Level < runLevel) { s.Progress.experience = EncounterProgress.XpForLevel(runLevel); s.Player.SetLevel(runLevel); }
            s.Player.Health.ApplyHealing(s.Player.Health.Pool.Max); s.Player.Resource.Pool.Fill();
            // The gates the run's quests open (the singing door's sigils, the platform gate's goblin) stand open, and for the full run those worked
            // by hand (a pen's bar, a bookcase); a boss's door is left to its boss, and a bar worked from its far side stays as it is (the Adit rework).
            bool everything = DungeonPlan.ParseMode(ModeArg) == DungeonPlan.Mode.Full;
            foreach (var g in s.Zone.Gates) if (g != null && (g.Kind == "liftgate" || g.Kind == "platformgate" || everything && (g.Kind == "pen" || g.Kind == "hiddendoor"))) g.SetOpen(true, true);
            Log("The " + (s.Zone.Zone.displayName ?? s.ZoneId) + ": a level-" + s.Progress.Level + " " + s.ClassDef.displayName + " with three sims; the gates its quests open stand open (the run's quests are not done here), the bosses' doors are left to the bosses.");
            // The group: a Druid, a Mage and a Ranger from the sim roster, brought here at level 11.
            var pop = SimPopulation.Active;
            if (pop == null) { Debug.LogError("Run capture: no sims."); Application.Quit(1); yield break; }
            var chosen = new List<SimAdventurer>();
            // A party of five (Chris, 2026-10-11: "need a paladin, ranger pet, etc", "need a cc class also"): a Warrior to tank and lead
            // (the boldest leads), a Druid to heal, a Ranger with its wolf, a Rogue for the holds (the sims' crowd control: it saps and
            // gouges the marked mob); you are the fifth, the Paladin (--crulanda-class class.paladin).
            foreach (var want in new[] { "class.warrior", "class.druid", "class.ranger", "class.rogue", "class.archivist", "class.paladin", "class.mage" })
            {
                if (chosen.Count >= EncounterSession.MaxPartySims) break;
                var x = pop.World.sims.Find(y => y.classId == want && !chosen.Contains(y)); if (x != null) chosen.Add(x);
            }
            var p0 = s.Player.transform.position; int k = 0;
            foreach (var x in chosen) { x.zone = s.ZoneId; x.onlineFrom = 0; x.onlineHours = 24; x.friendly = .9f; x.level = runLevel; x.bold = x.classId == "class.warrior" ? .95f : .3f; x.gearBonus = 2; x.x = p0.x + 2 + k; x.z = p0.z + (k % 2 == 0 ? 1.5f : -1.5f); k++; }   // gearBonus: bought upgrades, the better pieces of its level (Chris: "give the warrior some better gear")
            // Their packs for the bosses (BossBuffs; Chris, 2026-10-15: "food buffs, alchemy buffs, class buffs"): bought with their own coin at
            // the level-11 tier, a purse of 120 at the least (as gearBonus above: what a sim of eleven would have put by), so a richer one carries a feast.
            foreach (var x in chosen) { x.coin = Mathf.Max(x.coin, 120); SimEconomy.Stock(x, s.Items); }
            pop.Refresh(); yield return new WaitForSeconds(.5f);
            foreach (var x in chosen) if (!s.Invite(x.id, true)) Log("could not invite " + x.name);
            Log("Packs for the bosses: " + string.Join("; ", chosen.Select(x => x.name.Split(' ')[0] + ": " + Pack(s, x.goodIds, x.goodCounts))) + ".");
            // Yours (the run gives the bot a few of its level's tier): a feast, a dish, the elixir for your part and your class's draught.
            var mine = new List<string>();
            void Give(ItemDef d, int n) { if (d != null && Inventory.Add(s.Progress, s.Items, d.id, n) < n) mine.Add(d.name + " x" + n); }
            var all = s.Items.Items.Values; int lvl = s.Progress.Level;
            Give(BossBuffs.FeastFor(lvl, s.Items), 2); Give(BossBuffs.PickDish(all, lvl), 2);
            Give(BossBuffs.PickElixir(s.PlayerRole, BossBuffs.MainStat(s.ClassDef.id), all, lvl), 3); Give(BossBuffs.PickDraught(Consumables.PoolOf(s.ClassDef), all, lvl), 2);
            Log("Your bags for the bosses: " + (mine.Count > 0 ? string.Join(", ", mine) : "nothing of your tier") + ".");
            // Your talent points spent as a player of your level would have them: every point in your class's path for the part you play
            // (2026-10-16, the Hallow's Creek run at nineteen: the bot had none, so the Paladin's damage path's scale, x0.71 in
            // EncounterSession.RoleScale, never applied, and the untalented Paladin out-hit the Rogue and the Ranger).
            Log("Your talents: " + SpendForRole(s) + ".");
            // You in your class's kit at this level too (the temp save has only the starter gear): the armour and the Stamina of a sim's
            // pieces (the pieces only: the tank's edges are the Warrior sim's), as the sims' health counts theirs (2026-10-16).
            SimCompanion.KitPlayerLike(s, new SimAdventurer { classId = s.ClassDef.id, level = s.Progress.Level, gearSeed = 7, gearBonus = 2 });
            yield return new WaitForSeconds(.5f);
            Log("The party: you (" + s.ClassDef.displayName + ", " + s.Player.Health.Pool.Max + " health, armour " + s.Player.Stats.GetRounded(Crulanda.Core.StatType.Armor) + "), " + string.Join(", ", s.PartySims.Where(c => c != null).Select(c => c.sim.name + " (" + SimRoster.ClassName(c.sim.classId) + ", " + c.Role + ", " + c.actor.Health.Pool.Max + " health, armour " + c.actor.Stats.GetRounded(Crulanda.Core.StatType.Armor) + ")")) + (s.SimPets.Count > 0 ? "; " + string.Join(", ", s.SimPets.Select(p => p.actor.DisplayName)) : "") + ".");
            // Dressed for the Adit: the temp save has no gear, so each party member's own level-11 kit (the sims' SimGear is on them already).
            if (!s.LeadDungeon(ModeArg)) { Log("Nobody could lead the run."); Finish(s, directory, log, 0, 0, 0, 0, 0, false); yield break; }
            var leader = s.PartySims.Find(c => c != null && c.Dungeon != null);
            int camps = leader.CampsLeft, left = camps;
            if (leader.WingOrder.Count > 0) wingsRun = DungeonPlan.Describe(leader.RunMode) + ": " + string.Join(", ", leader.WingOrder);
            Log("Leader: " + leader.sim.name + ", " + camps + " camps" + (wingsRun != null ? "; " + wingsRun : "") + ".");
            string wingNow = null; int wingNo = 0, wingDone = 0, wingCamps = 0; float gateSince = -1; bool regrouping = false;
            var motor = s.Player.GetComponent<AdventurerMotor>(); AdventurerMotor.Running = true;
            int deaths = 0, warps = 0, shots = 0, elitesSeen = 0, mends = 0, looted = 0; var lootTally = new SortedDictionary<string, int>(); float lootUntil = 0; var seen = new HashSet<string>(); var mending = new Dictionary<SimCompanion, string>(); var engagedAt = new Dictionary<EncounterEnemy, float>(); var searched = new HashSet<EncounterEnemy>(); float idleLogAt = 0, pathAt = 0; Vector3 pathGoal = Vector3.zero; NavMeshPath path = null; int holds = 0; string lastHold = null; var simHeld = new Dictionary<SimCompanion, bool>();
            float start = Time.time, realStart = Time.realtimeSinceStartup, nextShot = Time.time + 20, nextSwing = 0, stuckAt = Time.time, deadAt = -1, atCorpseSince = -1, headBest = 0, headAt = Time.time; UnityEngine.Object headFor = null; var stuckFrom = s.Player.transform.position;
            string leadingName = leader.LeadingName;
            bool wasFalling = false; float fogFellAt = -99;
            // The bot's steering (2026-10-17, hallows-4: stuck and warped twenty times where the sims walked): the path's corner it is on, why
            // it last re-pathed, its kerb hops and side-steps round what stands in the way, and the mobs it found no way to (skipped a while).
            int pathCorner = 1, hops = 0, sidesteps = 0, hopsNow = 0, sidestepsNow = 0, sideTries = 0, outOfReach = 0; bool repathNow = false, pathOk = false;
            Vector3 pathDest = Vector3.zero, blockFrom = s.Player.transform.position; float blockAt = Time.time, sideUntil = -1, sideSign = 1, hopAt = -1;
            var unreachable = new Dictionary<EncounterEnemy, float>(); var unreachableLogged = new HashSet<EncounterEnemy>(); string goalLabel = "the leader";
            var controller = s.Player.GetComponent<CharacterController>();
            // (A dungeon's checkpoints, EncounterSession.Checkpoints) the ones reached so far on the log.
            int checkpointsLogged = 0;
            int botBuffed = -1, buffStops = 0; var simBuffed = new Dictionary<SimCompanion, int>();
            Time.timeScale = Speed;
            Shot(s, directory, "00-" + Clock(0) + "-setting-out", ref shots, leader);
            while (leader != null && leader.Dungeon != null && Time.realtimeSinceStartup - realStart < RealLimitSeconds)
            {
                float t = Time.time - start; var me = s.Player.transform.position;
                // (Hallow's Creek) Falling into the chasm's fog twice in half a minute: the way the bot steers goes over a slab's edge, the fog
                // puts it back on the lip, and it walks off again (hallows-1, 2026-10-16: four minutes of it after the far Table). The second
                // time it is warped to beside the leader. Ten metres over the fog is under every slab: only a fall is down there.
                bool falling = s.Zone != null && s.Zone.FogFloor(new Vector2(me.x, me.z), out float fogY) && me.y < fogY + 10;
                if (falling && !wasFalling)
                {
                    bool again = Time.time - fogFellAt < 30; fogFellAt = Time.time;
                    // Every fall on the record, where it went over and what it was after (the first was silent: hallows-4's Mason fight).
                    Log(Clock(t) + "  fell into the fog at " + At(me) + " going to " + goalLabel + (again ? " (the second in half a minute)" : ""));
                    if (again && NavMesh.SamplePosition(leader.transform.position, out var byLeader, 3, NavMesh.AllAreas))
                    {
                        motor.Teleport(byLeader.position + Vector3.up * 1.05f); warps++; fogFellAt = -99; me = s.Player.transform.position;
                        Log(Clock(t) + "  fell into the fog twice; warped to the leader (" + warps + ")");
                    }
                }
                wasFalling = falling;
                // The leader's state when nothing moves (every 20 s of nobody fighting), to see what holds the run.
                if (Time.time >= idleLogAt) { idleLogAt = Time.time + 20; Log(Clock(t) + (s.InCombat ? "  fighting: " : "  quiet: ") + string.Join("; ", s.PartySims.Where(c => c != null).Select(c => c.sim.name.Split(' ')[0] + " " + (c.Activity ?? "").ToLower() + " (" + Vector3.Distance(c.transform.position, me).ToString("0") + " m)")) + "; bags " + s.Progress.bag.Take(Inventory.BagSize).Count(b => !b.Empty) + "/" + Inventory.BagSize + (s.DuoState != null ? "; " + s.DuoState : "")); }
                // A camp cleared: the leader moves its mark on.
                if (leader.CampsLeft < left) { wingDone++; Log(Clock(t) + "  camp " + (camps - leader.CampsLeft) + "/" + camps + " cleared" + (wingNow != null ? " (wing " + wingNow + ": camp " + wingDone + "/" + wingCamps + ")" : "") + ": " + leadingName + "; next " + leader.LeadingName); left = leader.CampsLeft; }
                // The wing it is in now, a regroup at a wing's home, a wait at a shut gate (the Adit rework).
                if (leader.Wing != wingNow) { wingNow = leader.Wing; wingDone = 0; wingCamps = leader.WingCampsLeft; if (wingNow != null) { wingNo++; Log(Clock(t) + "  wing " + wingNo + ": " + wingNow + " (" + wingCamps + " camps)"); } }
                if (leader.Regrouping != regrouping) { regrouping = leader.Regrouping; if (regrouping) Log(Clock(t) + "  regrouping at the " + leader.LeadingName); }
                // A checkpoint reached (Chris, 2026-10-16: three in each dungeon; dead, you wake at the latest).
                while (checkpointsLogged < s.CheckpointsReached.Count) Log(Clock(t) + "  checkpoint reached: " + s.CheckpointsReached[checkpointsLogged++]);
                if (!leader.WaitingAtGate) gateSince = -1;
                else if (gateSince < 0) { gateSince = Time.time; Log(Clock(t) + "  waiting at " + leader.StoppedBy); }
                else if (Time.time - gateSince > 60)
                {
                    var shut = s.Zone.Gates.Find(g => g != null && !g.Open && g.Title == leader.StoppedBy);
                    if (shut != null) { shut.SetOpen(true); Log(Clock(t) + "  " + shut.Title + " still shut after a minute (it opens " + (string.IsNullOrEmpty(shut.OpensOn) ? "by hand" : "when " + shut.OpensOn + " falls") + "): opened for the run"); }
                    gateSince = -1;
                }
                // A buff-up before a boss (BossBuffs): the bot buffs from its bags as you would (/buff), and every member's buffing is on the record.
                if (s.BuffingUp && botBuffed != s.BuffStop)
                {
                    botBuffed = s.BuffStop; buffStops++; Log(Clock(t) + "  buff up before " + leader.LeadingName + " (" + Vector3.Distance(leader.transform.position, leader.Leading ?? leader.transform.position).ToString("0") + " m short)");
                    var used = s.BuffUp(); Log(Clock(t) + "  buffed: you: " + (used.Count > 0 ? string.Join(", ", used) : "nothing needed"));
                }
                foreach (var c in s.PartySims)
                {
                    if (c == null || c.BuffedStop < 0) continue; simBuffed.TryGetValue(c, out var was);
                    if (c.BuffedStop != was) { simBuffed[c] = c.BuffedStop; Log(Clock(t) + "  buffed: " + c.sim.name + ": " + c.BuffNote); }
                }
                if (leader.LeadingName != leadingName)
                {
                    leadingName = leader.LeadingName;
                    bool elite = leader.Leading.HasValue && s.Enemies.Exists(e => e != null && e.Elite && e.actor.IsAlive && Vector3.Distance(e.transform.position, leader.Leading.Value) < 20);
                    if (elite && seen.Add(leadingName)) { elitesSeen++; Shot(s, directory, Clock(t).Replace(':', 'm') + "-" + Slug(leadingName), ref shots, leader); }
                }
                // Every hold a boss puts on the party, as it lands (Chris, 2026-10-11: "are the bosses cc party members?"; the 20-second
                // snapshots missed holds of two to four seconds).
                var hold = s.PlayerHoldLabel; if (hold != null && !hold.StartsWith(lastHold ?? " ")) { holds++; Log(Clock(t) + "  you: " + hold.Split(' ')[0].ToLower() + " (" + leadingName + ")"); }
                lastHold = hold != null ? hold.Split(' ')[0] : null;
                foreach (var c in s.PartySims)
                {
                    if (c == null) continue; bool now = c.HeldNow; simHeld.TryGetValue(c, out var was);
                    if (now && !was) { holds++; Log(Clock(t) + "  " + c.sim.name + ": " + (c.Activity ?? "held").ToLower() + " (" + leadingName + ")"); }
                    simHeld[c] = now;
                }
                // Every mend a sim starts (its Activity "Mending ..."), by name, so the healing is on the record (Chris, 2026-10-11:
                // "so once again no healing from the druid??").
                foreach (var c in s.PartySims)
                {
                    if (c == null) continue; mending.TryGetValue(c, out var was); var now = c.Activity ?? "";
                    if (now.StartsWith("Mending") && now != was) { mends++; Log(Clock(t) + "  " + c.sim.name + " " + now.ToLower() + " (you " + s.Player.Health.Pool.Current + "/" + s.Player.Health.Pool.Max + ")"); }
                    mending[c] = now;
                }
                // Dead: four seconds down, then up at the stone (the run waits).
                if (!s.Player.IsAlive)
                {
                    if (deadAt < 0)
                    {
                        deadAt = Time.time; deaths++;
                        var healer = s.PartySims.Find(c => c != null && c.Role == SimCompanion.PartyRole.Healer);
                        Log(Clock(t) + "  you fell at " + leadingName + " (death " + deaths + "; " + s.Player.Health.Pool.Max + " health; the healer " + (healer == null ? "none" : healer.sim.name + " " + (healer.actor.IsAlive ? Vector3.Distance(healer.transform.position, me).ToString("0") + " m off, " + (healer.Activity ?? "").ToLower() : "dead")) + ")");
                    }
                    else if (Time.time - deadAt > 4) { s.Recover(); deadAt = -1; Log(Clock(t) + "  recovered at " + s.WokeAt); }
                    EncounterInput.Scripted = Vector2.zero; yield return null; continue;
                }
                deadAt = -1;
                // Fight what fights the party near you (letting the tank take the first blows: a mob is yours two and a half seconds after
                // the party engages it, or at once when it is on you); else keep up with the leader.
                EncounterEnemy foe = null; float best = 14;
                foreach (var e in s.Enemies)
                {
                    if (e == null || !e.actor.IsAlive || e.Game || e.Hidden || !e.FightingParty || SimCompanion.Held(e)) continue;   // a held mob is left held
                    if (!engagedAt.TryGetValue(e, out var since)) { engagedAt[e] = since = Time.time; }
                    if (e.Victim != s.Player && Time.time - since < 2.5f) continue;
                    // A mob the paths do not reach (on a deck or a ledge over you) is left a few seconds (the bot stood under it and was warped).
                    if (unreachable.TryGetValue(e, out var skipTill) && Time.time < skipTill) continue;
                    float d = Vector3.Distance(e.transform.position, me); if (d < best) { best = d; foe = e; }
                }
                // Assist the tank (2026-10-15): what it holds before the nearest (the bot went for Nix while he was free and died at 15 s,
                // every fight); a held mob is still left alone.
                { var tank = s.PartySims.Find(c => c != null && c.actor.IsAlive && c.Role == SimCompanion.PartyRole.Tank);
                  if (tank != null) { var held = s.Enemies.Find(e => e != null && e.actor.IsAlive && !e.Hidden && e.Victim == tank.actor && !SimCompanion.Held(e) && Vector3.Distance(e.transform.position, me) < 30 && !(unreachable.TryGetValue(e, out var till) && Time.time < till)); if (held != null) foe = held; } }
                // Nothing on you: search the bodies (Chris, 2026-10-11: "they need to loot to test the loot db"), the nearest with anything on
                // it within 12 m, before going on; each thing taken is logged.
                EncounterEnemy corpse = null;
                if (foe == null)
                {
                    float nearest = 12;
                    foreach (var e in s.Enemies) if (e != null && e.Camp && !e.Game && s.CanLoot(e) && s.CanTakeAny(e) && !searched.Contains(e)) { float d = Vector3.Distance(e.transform.position, me); if (d < nearest) { nearest = d; corpse = e; } }   // (2026-10-15, adit-final: a body with nothing you may take, its pieces rolled to others, held the bot beside it for an hour)
                    var body = s.LootableCorpse;
                    if (body != null && Time.time >= lootUntil && !searched.Contains(body))
                    {
                        // Bags full: the cheapest thing not worn goes (the run would stand at a body it could not take from).
                        if (s.Progress.bag.Take(Inventory.BagSize).All(b => !b.Empty))
                        {
                            int cheapest = -1, worth = int.MaxValue;
                            for (int bi = 0; bi < s.Progress.bag.Count && bi < Inventory.BagSize; bi++) { var st = s.Progress.bag[bi]; if (st.Empty) continue; var d = s.Items.Get(st.item); int v = d != null ? d.value : 0; if (v < worth) { worth = v; cheapest = bi; } }
                            if (cheapest >= 0) { var d = s.Items.Get(s.Progress.bag[cheapest].item); Log(Clock(t) + "  bags full: " + (d != null ? d.name : s.Progress.bag[cheapest].item) + " thrown away"); s.Progress.bag[cheapest] = new ItemStack(); }
                        }
                        // A body with only common things is emptied in the one press (OpenLoot); one with gear opens the window: either way,
                        // what landed in the bags is the record.
                        string Named(string id) { var d = s.Items.Get(id); return d != null ? d.name : id; }
                        var before = new Dictionary<string, int>(); foreach (var st in s.Progress.bag) if (!st.Empty) { before.TryGetValue(st.item, out var n); before[st.item] = n + st.count; }
                        int coinBefore = s.Progress.gold;
                        var onIt = body.Drops != null ? body.Drops.Where(d => d.count > 0).Select(d => Named(d.item)).ToList() : new List<string>();   // what lay on it (a rolled piece goes to whoever wins it)
                        s.Interact(); if (s.LootOpen && s.LootBody == body) s.TakeAllLoot();
                        var gained = new List<string>();
                        var after = new Dictionary<string, int>(); foreach (var st in s.Progress.bag) if (!st.Empty) { after.TryGetValue(st.item, out var n); after[st.item] = n + st.count; }
                        foreach (var kv in after) { before.TryGetValue(kv.Key, out var had); if (kv.Value > had) { gained.Add(Named(kv.Key) + (kv.Value - had > 1 ? " x" + (kv.Value - had) : "")); var key = Named(kv.Key); lootTally.TryGetValue(key, out var n); lootTally[key] = n + kv.Value - had; } }
                        int coin = s.Progress.gold - coinBefore; looted++; searched.Add(body);
                        Log(Clock(t) + "  searched " + body.Name + ": " + (gained.Count > 0 ? string.Join(", ", gained) + (coin > 0 ? ", " + coin + " coin" : "") : coin > 0 ? coin + " coin" : "nothing") + (onIt.Count > gained.Count ? " (on it: " + string.Join(", ", onIt) + "; the rest rolled for)" : "") + (body.Drops != null && body.Drops.Count > 0 ? " (left on it: " + string.Join(", ", body.Drops.Select(d => Named(d.item))) + ")" : ""));
                        // Anything better than what you wear goes on (the loot's worth, seen in play).
                        for (int bi = 0; bi < s.Progress.bag.Count; bi++)
                        {
                            var stack = s.Progress.bag[bi]; if (stack.Empty) continue; var d = s.Items.Get(stack.item); if (d == null || d.kind != "gear" || !Inventory.CanEquip(d, s.Progress.Level, out _)) continue;
                            int slot = ItemDatabase.SlotIndex(d.slot); if (slot < 0 || slot >= s.Progress.equipment.Count) continue;
                            var worn = s.Progress.equipment[slot].Empty ? null : s.Items.Get(s.Progress.equipment[slot].item);
                            if (worn != null && Worth(worn) >= Worth(d)) continue;
                            if (s.EquipFromBag(bi)) Log(Clock(t) + "  equipped " + d.name + " (" + d.slot + ", armour " + d.armor + (worn != null ? ", was " + worn.name : "") + ")");
                        }
                        lootUntil = Time.time + 1.5f;
                        if (corpse == body) corpse = null;
                    }
                }
                // A caster or an archer fights from range (2026-10-14: the bot walked every class into melee, so the Shaman's run was 14 a second).
                Vector3 goal = foe != null ? foe.transform.position : corpse != null ? corpse.transform.position : leader.transform.position; float reach = foe != null ? (MeleeClass(s) ? 2.6f : 12f) : corpse != null ? 2.5f : 4.5f;
                if (foe != null)
                {
                    if (s.Target != foe) s.Select(foe);
                    if (Time.time >= nextSwing) { nextSwing = Time.time + .4f; BotCast(s); }
                }
                var to = goal - me; to.y = 0;
                // The whole distance, not the flat one (2026-10-17, hallows-4): the leader a flight up the bell tower, or a mob on the loft over
                // you, is not reached by standing under it. (The leader on another level, 2026-10-15, adit-final8: the bot stood on the grass of
                // the yard with the tank 50 m below in the cave, "there" by the flat distance, while the tank waited for it for twenty minutes.)
                // Ten seconds with no headway on another level is stuck too.
                float dist = Vector3.Distance(goal, me);
                bool otherLevel = foe == null && corpse == null && Mathf.Abs(goal.y - me.y) > 4;
                UnityEngine.Object goalOf = foe != null ? (UnityEngine.Object)foe : corpse != null ? corpse : leader;
                goalLabel = foe != null ? foe.Name + " (the fight)" : corpse != null ? "the body of " + corpse.Name : leader.sim.name + " (the leader, " + (leader.Activity ?? "").ToLower() + ")";
                if (goalOf != headFor || dist <= reach + 1) { headFor = goalOf; headBest = dist; headAt = Time.time; }
                else if (dist < headBest - 2) { headBest = dist; headAt = Time.time; }
                // Held by a boss (stunned or rooted) the bot stands as you would: no steering, and it is not stuck (it was warped out of roots).
                bool heldNow = s.PlayerStunned || s.PlayerRooted;
                // Along the paths, corner by corner, as the sims go (a straight line stopped at every wall and pot: Chris, 2026-10-11, "stuck on
                // wall"). The steering of a player at the keys (2026-10-17, hallows-4: twenty warps where the sims walked on):
                //   the path from the navmesh under the feet (a bot brushing a wall or a parapet stands off the navmesh, which keeps half a
                //   metre from them, and a path from there could fail: no corners, so it walked the straight line into the wall) to the
                //   navmesh under what it is after (a body on a parapet's kerb, a mob a metre off the walk);
                //   a new path every half second, when the goal has moved a metre, and at once when it is pushed more than a metre and a half
                //   off its leg;
                //   a corner taken when it is within 0.45 m or passed (at three times speed a frame's step can overshoot the 0.45 m, and the
                //   bot turned back for the corner it had passed); never cut (at 1.2 m it cut the decks' turns and stepped off their open
                //   junctions: adit-final3, a wipe at Vosk);
                //   no way there (the path ends short): a body is passed by, a mob is left a few seconds;
                //   blocked (pressing on and not 0.25 m on in 0.4 s): over something low it hops, as a player hops a kerb (the paths climb
                //   0.75 m, the bot's step is 0.35: the broken parapet at the jump off the third slab is 0.45, a ramp's or a stair's side
                //   where it leaves the ground is up to 0.75); round anything else, a party member or a post, it side-steps to the freer side
                //   and takes a new path.
                if (dist > reach && !heldNow)
                {
                    var feet = me - Vector3.up;
                    int n = path != null ? path.corners.Length : 0;
                    if (n > 1 && pathCorner >= 1 && pathCorner < n && FlatToLeg(me, path.corners[pathCorner - 1], path.corners[pathCorner]) > 1.5f) repathNow = true;
                    if (Time.time >= pathAt || (goal - pathGoal).sqrMagnitude > 1 || repathNow)
                    {
                        pathAt = Time.time + .5f; pathGoal = goal; repathNow = false; if (path == null) path = new NavMeshPath();
                        var from = OnNavMesh(feet, .8f, 2.5f, 3f);
                        pathDest = OnNavMesh(goal - Vector3.up, 1.2f, 3f, 2.5f);
                        pathOk = NavMesh.CalculatePath(from, pathDest, NavMesh.AllAreas, path); pathCorner = 1; n = path.corners.Length;
                        // No way all the way there: what the path ends short of.
                        if (!pathOk || path.status != NavMeshPathStatus.PathComplete)
                        {
                            float shortBy = Vector3.Distance(n > 0 ? path.corners[n - 1] : from, pathDest);
                            if (foe == null && corpse != null && shortBy > 2) { searched.Add(corpse); outOfReach++; Log(Clock(t) + "  passed by the body of " + corpse.Name + " at " + At(corpse.transform.position) + ": no way to it (the path " + (pathOk ? path.status.ToString() : "failed") + ", " + shortBy.ToString("0.0") + " m short)"); }
                            else if (foe != null && shortBy > reach)
                            {
                                unreachable[foe] = Time.time + 6; outOfReach++;
                                if (unreachableLogged.Add(foe)) Log(Clock(t) + "  no way to " + foe.Name + " at " + At(foe.transform.position) + " (the path " + (pathOk ? path.status.ToString() : "failed") + ", " + shortBy.ToString("0.0") + " m short): left a while");
                            }
                        }
                    }
                    var step = to;
                    if (n > 1)
                    {
                        int ci = Mathf.Clamp(pathCorner, 1, n - 1);
                        while (ci < n - 1)
                        {
                            Vector3 c = path.corners[ci], prev = path.corners[ci - 1];
                            var leg = new Vector2(c.x - prev.x, c.z - prev.z); var past = new Vector2(me.x - c.x, me.z - c.z);
                            bool passed = leg.sqrMagnitude > .01f && Vector2.Dot(past, leg) > 0 && FlatToLeg(me, prev, c) < 1;
                            if (past.magnitude < .45f || passed) ci++; else break;
                        }
                        pathCorner = ci; step = path.corners[ci] - me; step.y = 0;
                    }
                    if (step.sqrMagnitude < .01f) step = to;
                    var ahead = step.normalized;
                    motor.SetView(Mathf.Atan2(step.x, step.z) * Mathf.Rad2Deg, 14, 7);
                    // A side-step: across the way and a little on, the camera kept on the way.
                    EncounterInput.Scripted = Time.time < sideUntil ? new Vector2(sideSign, .3f) : Vector2.up;
                    if (motor.Airborne || motor.Swimming) { blockAt = Time.time; blockFrom = me; }
                    else if (Time.time - blockAt > .4f)
                    {
                        float went = new Vector2(me.x - blockFrom.x, me.z - blockFrom.z).magnitude;
                        if (went < .25f && Time.time >= sideUntil)
                        {
                            var q = QueryTriggerInteraction.Ignore;
                            bool low = Physics.Raycast(feet + Vector3.up * .3f, ahead, .9f, ~0, q) && !Physics.Raycast(feet + Vector3.up * .85f, ahead, .9f, ~0, q);
                            if (low && Time.time >= hopAt) { motor.Jump(); hopAt = Time.time + .8f; hops++; hopsNow++; }
                            else
                            {
                                // The freer side (what is beside the bot at the chest, out to 1.5 m), the other side on the next try; never a
                                // side with no walk a metre over (off a deck's open edge into the fog). A side-step is a third of a second, about
                                // a metre and a half: past a party member, not off the deck.
                                var right = new Vector3(ahead.z, 0, -ahead.x); var chest = feet + Vector3.up;
                                float r = Physics.Raycast(chest, right, out var rh, 1.5f, ~0, q) ? rh.distance : 1.5f, l = Physics.Raycast(chest, -right, out var lh, 1.5f, ~0, q) ? lh.distance : 1.5f;
                                bool Walk(Vector3 side) { return NavMesh.SamplePosition(feet + side * 1.2f, out var w, .6f, NavMesh.AllAreas) && Mathf.Abs(w.position.y - feet.y) < .6f; }
                                bool okR = Walk(right), okL = Walk(-right);
                                if (okR || okL)
                                {
                                    sideSign = okR != okL ? (okR ? 1 : -1) : sideTries % 2 == 0 ? (r >= l ? 1 : -1) : -sideSign; sideTries++;
                                    sideUntil = Time.time + .3f; sidesteps++; sidestepsNow++;
                                }
                                repathNow = true;
                            }
                        }
                        else if (went > 1) sideTries = 0;
                        blockAt = Time.time; blockFrom = me;
                    }
                }
                else { EncounterInput.Scripted = Vector2.zero; blockAt = Time.time; blockFrom = me; }
                // At a body four seconds and it is not searched (out of reach of the search: on a deck over you, or nothing on it is yours):
                // it is passed by, so the bot never stands at one for good while the leader waits for the party (adit-final, 24:41 on).
                if (foe == null && corpse != null && dist <= reach) { if (atCorpseSince < 0) atCorpseSince = Time.time; else if (Time.time - atCorpseSince > 4) { searched.Add(corpse); atCorpseSince = -1; } }
                else atCorpseSince = -1;
                // Held by a boss is not stuck: the four seconds start again when it lets go.
                if (heldNow) { stuckAt = Time.time; stuckFrom = me; }
                // Stuck all the same (a prop the paths run through): a warp to beside what you are after, in a fight or out of one.
                if (Time.time - stuckAt > 4)
                {
                    // Swimming counts as stuck too: the paths never run through swim-deep water, so the bot has no way out of a pool it has
                    // fallen or been knocked into, and it bobs about more than a metre (adit-final2, 2026-10-15: "just standing in water").
                    // Swimming is stuck however near the leader it is (adit-final9, 2026-10-15: in the chute pool 4 m from the tank, the healer and the
                    // archer stopped at the shore 13 m back, the tank waited for them and the run stood: "stuck in the water again").
                    if (motor.Swimming || dist > reach + 1 && (Vector3.Distance(stuckFrom, me) < 1 || otherLevel && Time.time - headAt > 10))
                    {
                        // What held it, on the record before the warp (2026-10-17: hallows-4's twenty warps said only "stuck").
                        string why = Stuck(me, goal, dist, controller, path, pathOk, pathCorner, pathDest, hopsNow, sidestepsNow, motor.Swimming);
                        var at = goal - to.normalized * Mathf.Min(2.5f, dist * .5f);
                        // Onto the level of what it is after (checked against at.y, a point off a sloping cave floor, it refused every rescue in adit-final8).
                        if (NavMesh.SamplePosition(at, out var hit, 4, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - goal.y) < 3f) { motor.Teleport(hit.position + Vector3.up * 1.05f); warps++; headAt = Time.time; repathNow = true; Log(Clock(t) + "  stuck; warped " + (foe != null ? "to the fight" : corpse != null ? "to the body" : "to the leader") + " (" + warps + "): going to " + goalLabel + "; " + why); }
                        else Log(Clock(t) + "  stuck, and no navmesh beside " + goalLabel + " to warp to: " + why);
                    }
                    stuckAt = Time.time; stuckFrom = me; hopsNow = sidestepsNow = 0;
                }
                if (Time.time >= nextShot) { nextShot = Time.time + ShotEvery; Shot(s, directory, Clock(t).Replace(':', 'm') + "-" + Slug(leadingName), ref shots, leader); }
                yield return null;
            }
            EncounterInput.Scripted = null; Time.timeScale = 1;
            float total = Time.time - start; bool done = leader != null && leader.Dungeon == null;
            Shot(s, directory, "98-" + Clock(total).Replace(':', 'm') + "-" + (done ? "cleared" : "stopped"), ref shots, leader);
            if (!s.ScorecardOpen) s.ShowScorecard((s.Zone.Zone.displayName ?? s.ZoneId) + (done ? " cleared" : " so far"));
            yield return null; yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "99-scorecard.png")); yield return new WaitForSeconds(.6f);
            Log(Clock(total) + "  " + (done ? "the run is done: every camp on it cleared." : "stopped (the real-time limit): " + (camps - left) + " of " + camps + " camps cleared.") + " Mends seen: " + mends + "; bodies searched: " + looted + "; holds the bosses put on the party: " + holds + "; buff-ups before bosses: " + buffStops + "; kerbs hopped: " + hops + "; side-steps round what stood in the way: " + sidesteps + "; bodies and mobs with no way to them: " + outOfReach + ".");
            Log("Loot taken: " + (lootTally.Count == 0 ? "nothing" : string.Join(", ", lootTally.Select(kv => kv.Key + (kv.Value > 1 ? " x" + kv.Value : "")))) + "; coin " + s.Progress.gold + ".");
            foreach (var line in s.Meter.Scorecard()) Log(line);
            Finish(s, directory, log, total, camps, camps - left, deaths, warps, done);
        }
        /// <summary>A sim's goods that are consumables, by name and count ("Shore feast x1, Scree salve x2").</summary>
        static string Pack(EncounterSession s, List<string> ids, List<int> counts)
        {
            var l = new List<string>();
            for (int i = 0; i < ids.Count; i++) { var d = s.Items.Get(ids[i]); if (d != null && d.kind == "consumable") l.Add(d.name + " x" + counts[i]); }
            return l.Count > 0 ? string.Join(", ", l) : "nothing";
        }
        static int Worth(ItemDef d) { return d == null ? 0 : d.armor + 2 * (d.stamina + d.strength + d.agility + d.intellect + d.spirit) + 3 * d.weaponDamage + d.heal; }
        static string Slug(string name) { return (name ?? "camp").ToLowerInvariant().Replace(' ', '-').Replace("'", "").Replace(",", ""); }
        void Shot(EncounterSession s, string directory, string name, ref int shots, SimCompanion leader)
        {
            var motor = s.Player.GetComponent<AdventurerMotor>();
            var look = leader != null && leader.Leading.HasValue ? leader.Leading.Value : s.Player.transform.position + s.Player.transform.forward;
            var to = look - s.Player.transform.position; to.y = 0; if (to.sqrMagnitude > .1f) motor.SetView(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 12, 8);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, (shots < 10 ? "0" : "") + shots + "-" + name + ".png")); shots++;
        }
        /// <summary>A point for the log: "(x, y, z)" to a tenth of a metre.</summary>
        static string At(Vector3 p) { return "(" + p.x.ToString("0.0") + ", " + p.y.ToString("0.0") + ", " + p.z.ToString("0.0") + ")"; }
        /// <summary>The navmesh under <paramref name="p"/>: within <paramref name="near"/>, else within <paramref name="far"/> if no more than
        /// <paramref name="maxRise"/> up or down (not the flight over it in the bell tower), else the point itself.</summary>
        static Vector3 OnNavMesh(Vector3 p, float near, float far, float maxRise)
        {
            if (NavMesh.SamplePosition(p, out var hit, near, NavMesh.AllAreas)) return hit.position;
            if (NavMesh.SamplePosition(p, out hit, far, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - p.y) < maxRise) return hit.position;
            return p;
        }
        /// <summary>How far <paramref name="p"/> is from the leg <paramref name="a"/> to <paramref name="b"/>, on the flat.</summary>
        static float FlatToLeg(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = new Vector2(b.x - a.x, b.z - a.z); var ap = new Vector2(p.x - a.x, p.z - a.z);
            float k = ab.sqrMagnitude > 1e-4f ? Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude) : 0;
            return (ap - ab * k).magnitude;
        }
        /// <summary>What a collider belongs to, for the log: a mob (or its body), a party member, else the object and the two above it.</summary>
        static string Named(Collider c)
        {
            if (c == null) return "nothing";
            var e = c.GetComponentInParent<EncounterEnemy>(); if (e != null) return e.Name + (e.actor.IsAlive ? "" : " (a body)");
            var sim = c.GetComponentInParent<SimCompanion>(); if (sim != null && sim.sim != null) return sim.sim.name;
            var t = c.transform; string name = "'" + t.name + "'";
            for (int i = 0; i < 2 && t.parent != null; i++) { t = t.parent; name += " in '" + t.name + "'"; }
            return name;
        }
        /// <summary>
        /// What held the bot when it is warped for being stuck (2026-10-17: hallows-4's twenty warps said only "stuck"): where it stood and
        /// what it stood on, how far the navmesh was, the path (its state, the corner it was on and how far up that is, how short of the goal
        /// it ends), what is in the way at the knee, the waist and the head, whether it pressed on a side, and its hops and side-steps.
        /// </summary>
        static string Stuck(Vector3 me, Vector3 goal, float dist, CharacterController cc, NavMeshPath path, bool pathOk, int corner, Vector3 dest, int hops, int sidesteps, bool swimming)
        {
            var feet = me - Vector3.up; var q = QueryTriggerInteraction.Ignore; var parts = new List<string>();
            parts.Add("at " + At(me) + ", " + dist.ToString("0.0") + " m from it at " + At(goal));
            parts.Add(Physics.Raycast(me, Vector3.down, out var g, 4, ~0, q) ? "on " + Named(g.collider) + " (" + (feet.y - g.point.y).ToString("0.00") + " m under the feet)" : "nothing under it within 3 m of the feet");
            parts.Add(NavMesh.SamplePosition(feet, out var nh, 3, NavMesh.AllAreas) ? "the navmesh " + Vector3.Distance(nh.position, feet).ToString("0.00") + " m off" : "no navmesh within 3 m");
            int n = path != null ? path.corners.Length : 0, c = Mathf.Clamp(corner, 1, Mathf.Max(1, n - 1));
            if (path == null) parts.Add("no path yet");
            else
            {
                string line = "the path " + (pathOk ? path.status.ToString() : "failed") + ", " + n + " corners";
                if (n > 1)
                {
                    var k = path.corners[c];
                    line += ", on corner " + c + " at " + At(k) + " (" + new Vector2(k.x - me.x, k.z - me.z).magnitude.ToString("0.0") + " m on, " + (k.y - feet.y).ToString("+0.00;-0.00") + " m up)";
                    line += ", its end " + Vector3.Distance(path.corners[n - 1], dest).ToString("0.0") + " m short of the goal";
                }
                parts.Add(line);
            }
            var way = n > 1 ? path.corners[c] - me : goal - me; way.y = 0;
            if (way.sqrMagnitude > 1e-4f)
            {
                string ahead = null; var d = way.normalized;
                foreach (var h in new[] { .3f, .85f, 1.5f })
                    if (Physics.Raycast(feet + Vector3.up * h, d, out var a, 1.5f, ~0, q)) { ahead = Named(a.collider) + " " + a.distance.ToString("0.0") + " m ahead at " + h.ToString("0.00") + " m up, its top " + (a.collider.bounds.max.y - feet.y).ToString("0.00") + " m over the feet"; break; }
                parts.Add(ahead != null ? "in the way: " + ahead : "nothing in the way at the knee, the waist or the head");
            }
            if (cc != null) parts.Add((cc.isGrounded ? "grounded" : "in the air") + ((cc.collisionFlags & CollisionFlags.Sides) != 0 ? ", pressing on a side" : ""));
            parts.Add(hops + " hops and " + sidesteps + " side-steps in the four seconds" + (swimming ? "; swimming" : ""));
            return string.Join("; ", parts);
        }
        static bool MeleeClass(EncounterSession s) { var id = s.ClassDef != null ? s.ClassDef.id : ""; return id == "class.warrior" || id == "class.paladin" || id == "class.rogue" || id == "class.scoundrel"; }
        /// <summary>
        /// Every talent point the bot has, spent as the balance harness buys a build: all in its class's first path for the part it plays
        /// (EncounterSession.PlayerRole: a tank's path, a healer's or a damage path; the Paladin with a Warrior along deals damage, the
        /// Judicator). What was bought, for the log.
        /// </summary>
        static string SpendForRole(EncounterSession s)
        {
            if (s.Talents == null || s.Progress == null) return "no talent tree";
            var role = s.PlayerRole;
            string want = role == SimCompanion.PartyRole.Tank ? "tank" : role == SimCompanion.PartyRole.Healer ? "healer" : "dps";
            var branch = s.Talents.Branches.FirstOrDefault(b => b.role == want) ?? s.Talents.Branches.FirstOrDefault();
            if (branch == null) return "no path";
            s.Progress.talents = new List<TalentRank>();
            var ids = branch.nodes.Select(n => n.id).ToList();
            for (int pass = 0; pass < 40 && s.Talents.Available(s.Progress) > 0; pass++)
                foreach (var id in ids) if (s.Talents.Propose(s.Progress, id, 1, out var next, out _)) s.Progress.talents = next;
            s.Kit.ClearTransient(); s.Kit.ApplyStats();
            return branch.name + " (" + branch.role + "), " + s.Progress.talents.Sum(t => t.rank) + " points; your blows x" + s.PlayerDamageScale.ToString("0.00");
        }
        /// <summary>The bot's turn (2026-10-14, Chris: the Shaman "plays poorly ... 14 damage a second against the Paladin's 33"; it pressed
        /// slots 1-3 whatever they were, and for a Shaman two of them are mends): a mend when someone of the party is under 45% and the kit
        /// has one, else the first ready damage ability on the bar in its order, else the first slot.</summary>
        static void BotCast(EncounterSession s)
        {
            if (s.PlayerCasting || s.Kit == null) return;
            bool hurt = s.Player.Health.Pool.Ratio < .45f || s.PartySims.Exists(c => c != null && c.actor != null && c.actor.IsAlive && c.actor.Health.Pool.Ratio < .45f);
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < s.Kit.ActionCount; i++)
                {
                    var a = s.Kit.ActionAt(i); if (a == null || s.Kit.ActionLockLabel(i) != null) continue;
                    bool want = pass == 0 ? hurt && a.effect == Crulanda.Abilities.AbilityEffect.Heal : a.effect == Crulanda.Abilities.AbilityEffect.Damage;
                    if (want && s.UseAbility(i)) return;
                }
            s.UseAbility(0);
        }
        void Finish(EncounterSession s, string directory, List<string> log, float total, int camps, int cleared, int deaths, int warps, bool done)
        {
            int slain = s.Enemies.Count(e => e != null && e.Camp && !e.actor.IsAlive), elites = s.Progress.elitesSlain.Count;
            var summary = new List<string> {
                "Group run of " + (s.Zone.Zone.displayName ?? s.ZoneId) + " (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")",
                "Party: you (" + s.ClassDef.displayName + ", level " + s.Progress.Level + ") and " + string.Join(", ", s.PartySims.Where(c => c != null).Select(c => c.sim.name + " the " + SimRoster.ClassName(c.sim.classId))),
                "Game time: " + (int)(total / 60) + " min " + ((int)(total % 60)).ToString("00") + " s (run at " + Speed + "x; the time is the game's)",
                "Camps cleared: " + (done ? camps : cleared) + " of " + camps + (done ? " (the run done)" : " (stopped before the end)"),
                "Wings, in the order run: " + (wingsRun ?? "(a dungeon without wings)"),
                "Mobs down at the end: " + slain + "; elites slain: " + elites,
                "Your deaths: " + deaths + "; warps to the leader when stuck: " + warps,
                "The gates the run's quests open were opened for it (the quests are not done here); the bosses' doors were left to the bosses.",
                "Scorecard:" };
            summary.AddRange(s.Meter.Scorecard().Select(l => "  " + l));
            File.WriteAllLines(Path.Combine(directory, "run-summary.txt"), summary);
            File.WriteAllLines(Path.Combine(directory, "run-log.txt"), log);
            Debug.Log("RUN_CAPTURE_DONE " + string.Join(" | ", summary));
            Application.Quit(0);
        }
    }
}
