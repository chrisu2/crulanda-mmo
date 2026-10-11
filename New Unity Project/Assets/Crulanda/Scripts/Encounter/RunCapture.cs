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
    /// </summary>
    public sealed class RunCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-run-capture";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }
        public const float Speed = 3, RealLimitSeconds = 50 * 60, ShotEvery = 240;

        public IEnumerator Run(EncounterSession s, string directory)
        {
            Directory.CreateDirectory(directory);
            var log = new List<string>(); Action<string> Log = line => { log.Add(line); Debug.Log("Run capture: " + line); };
            string Clock(float t) { int m = (int)(t / 60); return m + ":" + ((int)(t % 60)).ToString("00"); }
            Crulanda.World.WorldClock.Hour = 11; EncounterHud.UiScale = 1; EncounterHud.DefaultPanels = true; s.MeterOpen = true;   // the HUD in frame at 1440x900 (the party frame was cut off), the meter up
            var weather = Crulanda.World.WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            if (s.Progress.Level < 11) { s.Progress.experience = EncounterProgress.XpForLevel(11); s.Player.SetLevel(11); }
            s.Player.Health.ApplyHealing(s.Player.Health.Pool.Max); s.Player.Resource.Pool.Fill();
            s.Zone.OpenGates();
            Log("The " + (s.Zone.Zone.displayName ?? s.ZoneId) + ": a level-" + s.Progress.Level + " " + s.ClassDef.displayName + " with three sims; the gates open (the run's quests are not done here).");
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
            foreach (var x in chosen) { x.zone = s.ZoneId; x.onlineFrom = 0; x.onlineHours = 24; x.friendly = .9f; x.level = 11; x.bold = x.classId == "class.warrior" ? .95f : .3f; x.gearBonus = 2; x.x = p0.x + 2 + k; x.z = p0.z + (k % 2 == 0 ? 1.5f : -1.5f); k++; }   // gearBonus: bought upgrades, the better pieces of its level (Chris: "give the warrior some better gear")
            pop.Refresh(); yield return new WaitForSeconds(.5f);
            foreach (var x in chosen) if (!s.Invite(x.id, true)) Log("could not invite " + x.name);
            // You in your class's kit at this level too (the temp save has only the starter gear): the armour of a sim's pieces.
            s.Player.Stats.SetBase(Crulanda.Core.StatType.Armor, SimCompanion.ArmourFor(new SimAdventurer { classId = s.ClassDef.id, level = s.Progress.Level, gearSeed = 7, gearBonus = 2 }, s.Items));
            yield return new WaitForSeconds(.5f);
            Log("The party: you (" + s.ClassDef.displayName + ", " + s.Player.Health.Pool.Max + " health, armour " + s.Player.Stats.GetRounded(Crulanda.Core.StatType.Armor) + "), " + string.Join(", ", s.PartySims.Where(c => c != null).Select(c => c.sim.name + " (" + SimRoster.ClassName(c.sim.classId) + ", " + c.Role + ", " + c.actor.Health.Pool.Max + " health, armour " + c.actor.Stats.GetRounded(Crulanda.Core.StatType.Armor) + ")")) + (s.SimPets.Count > 0 ? "; " + string.Join(", ", s.SimPets.Select(p => p.actor.DisplayName)) : "") + ".");
            // Dressed for the Adit: the temp save has no gear, so each party member's own level-11 kit (the sims' SimGear is on them already).
            if (!s.LeadDungeon()) { Log("Nobody could lead the run."); Finish(s, directory, log, 0, 0, 0, 0, 0, false); yield break; }
            var leader = s.PartySims.Find(c => c != null && c.Dungeon != null);
            int camps = leader.CampsLeft, left = camps; Log("Leader: " + leader.sim.name + ", " + camps + " camps.");
            var motor = s.Player.GetComponent<AdventurerMotor>(); AdventurerMotor.Running = true;
            int deaths = 0, warps = 0, shots = 0, elitesSeen = 0, mends = 0, looted = 0; var lootTally = new SortedDictionary<string, int>(); float lootUntil = 0; var seen = new HashSet<string>(); var mending = new Dictionary<SimCompanion, string>(); var engagedAt = new Dictionary<EncounterEnemy, float>(); var searched = new HashSet<EncounterEnemy>();
            float start = Time.time, realStart = Time.realtimeSinceStartup, nextShot = Time.time + 20, nextSwing = 0, stuckAt = Time.time, deadAt = -1; var stuckFrom = s.Player.transform.position;
            string leadingName = leader.LeadingName;
            Time.timeScale = Speed;
            Shot(s, directory, "00-" + Clock(0) + "-setting-out", ref shots, leader);
            while (leader != null && leader.Dungeon != null && Time.realtimeSinceStartup - realStart < RealLimitSeconds)
            {
                float t = Time.time - start; var me = s.Player.transform.position;
                // A camp cleared: the leader moves its mark on.
                if (leader.CampsLeft < left) { Log(Clock(t) + "  camp " + (camps - leader.CampsLeft) + "/" + camps + " cleared: " + leadingName + "; next " + leader.LeadingName); left = leader.CampsLeft; }
                if (leader.LeadingName != leadingName)
                {
                    leadingName = leader.LeadingName;
                    bool elite = leader.Leading.HasValue && s.Enemies.Exists(e => e != null && e.Elite && e.actor.IsAlive && Vector3.Distance(e.transform.position, leader.Leading.Value) < 20);
                    if (elite && seen.Add(leadingName)) { elitesSeen++; Shot(s, directory, Clock(t).Replace(':', 'm') + "-" + Slug(leadingName), ref shots, leader); }
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
                    else if (Time.time - deadAt > 4) { s.Recover(); deadAt = -1; }
                    EncounterInput.Scripted = Vector2.zero; yield return null; continue;
                }
                deadAt = -1;
                // Fight what fights the party near you (letting the tank take the first blows: a mob is yours two and a half seconds after
                // the party engages it, or at once when it is on you); else keep up with the leader.
                EncounterEnemy foe = null; float best = 14;
                foreach (var e in s.Enemies)
                {
                    if (e == null || !e.actor.IsAlive || e.Game || e.Hidden || !e.FightingParty) continue;
                    if (!engagedAt.TryGetValue(e, out var since)) { engagedAt[e] = since = Time.time; }
                    if (e.Victim != s.Player && Time.time - since < 2.5f) continue;
                    float d = Vector3.Distance(e.transform.position, me); if (d < best) { best = d; foe = e; }
                }
                // Nothing on you: search the bodies (Chris, 2026-10-11: "they need to loot to test the loot db"), the nearest with anything on
                // it within 12 m, before going on; each thing taken is logged.
                EncounterEnemy corpse = null;
                if (foe == null)
                {
                    float nearest = 12;
                    foreach (var e in s.Enemies) if (e != null && !e.actor.IsAlive && e.Camp && !e.Game && !searched.Contains(e) && (e.Drops == null || e.Drops.Count > 0 || e.Coins > 0)) { float d = Vector3.Distance(e.transform.position, me); if (d < nearest) { nearest = d; corpse = e; } }
                    var body = s.LootableCorpse;
                    if (body != null && Time.time >= lootUntil)
                    {
                        if (!s.LootOpen) s.Interact();
                        if (s.LootOpen && s.LootBody == body)
                        {
                            string Named(string id) { var d = s.Items.Get(id); return d != null ? d.name : id; }
                            var had = body.Drops != null ? body.Drops.Select(d => Named(d.item) + (d.count > 1 ? " x" + d.count : "")).ToList() : new List<string>();
                            foreach (var d in (body.Drops ?? new List<LootDrop>())) { var key = Named(d.item); lootTally.TryGetValue(key, out var n); lootTally[key] = n + d.count; }
                            int coinBefore = s.Progress.gold; s.TakeAllLoot(); looted++; searched.Add(body);
                            Log(Clock(t) + "  searched " + body.Name + ": " + (had.Count > 0 ? string.Join(", ", had) : "nothing") + (s.Progress.gold > coinBefore ? ", " + (s.Progress.gold - coinBefore) + " coin" : "") + (body.Drops != null && body.Drops.Count > 0 ? " (left on it: " + string.Join(", ", body.Drops.Select(d => Named(d.item))) + ")" : ""));
                            // Anything better than what you wear goes on (the loot's worth, seen in play).
                            for (int bi = 0; bi < s.Progress.bag.Count; bi++)
                            {
                                var stack = s.Progress.bag[bi]; if (stack.Empty) continue; var d = s.Items.Get(stack.item); if (d == null || d.kind != "gear" || !Inventory.CanEquip(d, s.Progress.Level, out _)) continue;
                                int slot = ItemDatabase.SlotIndex(d.slot); if (slot < 0 || slot >= s.Progress.equipment.Count) continue;
                                var worn = s.Progress.equipment[slot].Empty ? null : s.Items.Get(s.Progress.equipment[slot].item);
                                if (worn != null && Worth(worn) >= Worth(d)) continue;
                                if (s.EquipFromBag(bi)) Log(Clock(t) + "  equipped " + d.name + " (" + d.slot + ", armour " + d.armor + (worn != null ? ", was " + worn.name : "") + ")");
                            }
                        }
                        lootUntil = Time.time + 1.5f; if (!s.LootOpen && s.LootBody == null) searched.Add(body);   // nothing on it (CanTakeAny): not again
                        if (corpse == body) corpse = null;
                    }
                }
                Vector3 goal = foe != null ? foe.transform.position : corpse != null ? corpse.transform.position : leader.transform.position; float reach = foe != null ? 2.6f : corpse != null ? 2.5f : 4.5f;
                if (foe != null)
                {
                    if (s.Target != foe) s.Select(foe);
                    if (Time.time >= nextSwing) { nextSwing = Time.time + .6f; s.UseAbility(0); s.UseAbility(2); s.UseAbility(1); }
                }
                var to = goal - me; to.y = 0; float dist = to.magnitude;
                if (dist > reach) { motor.SetView(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 14, 7); EncounterInput.Scripted = Vector2.up; }
                else EncounterInput.Scripted = Vector2.zero;
                // Stuck behind something (the caves' props): a warp to the leader's heels.
                if (Time.time - stuckAt > 6)
                {
                    if (foe == null && Vector3.Distance(leader.transform.position, me) > 12 && Vector3.Distance(stuckFrom, me) < 1.5f)
                    {
                        var at = leader.transform.position - leader.transform.forward * 2.5f;
                        if (NavMesh.SamplePosition(at, out var hit, 4, NavMesh.AllAreas)) { motor.Teleport(hit.position + Vector3.up * 1.05f); warps++; Log(Clock(t) + "  stuck; warped to the leader (" + warps + ")"); }
                    }
                    stuckAt = Time.time; stuckFrom = me;
                }
                if (Time.time >= nextShot) { nextShot = Time.time + ShotEvery; Shot(s, directory, Clock(t).Replace(':', 'm') + "-" + Slug(leadingName), ref shots, leader); }
                yield return null;
            }
            EncounterInput.Scripted = null; Time.timeScale = 1;
            float total = Time.time - start; bool done = leader != null && leader.Dungeon == null;
            Shot(s, directory, "99-" + Clock(total).Replace(':', 'm') + "-" + (done ? "cleared" : "stopped"), ref shots, leader);
            Log(Clock(total) + "  " + (done ? "the run is done: every camp cleared." : "stopped (the real-time limit): " + (camps - left) + " of " + camps + " camps cleared.") + " Mends seen: " + mends + "; bodies searched: " + looted + ".");
            Log("Loot taken: " + (lootTally.Count == 0 ? "nothing" : string.Join(", ", lootTally.Select(kv => kv.Key + (kv.Value > 1 ? " x" + kv.Value : "")))) + "; coin " + s.Progress.gold + ".");
            Log("Meter (" + Mathf.RoundToInt(s.Meter.CombatSeconds) + " s of fighting):"); foreach (var line in s.Meter.Lines()) Log("  " + line);
            Finish(s, directory, log, total, camps, camps - left, deaths, warps, done);
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
        void Finish(EncounterSession s, string directory, List<string> log, float total, int camps, int cleared, int deaths, int warps, bool done)
        {
            int slain = s.Enemies.Count(e => e != null && e.Camp && !e.actor.IsAlive), elites = s.Progress.elitesSlain.Count;
            var summary = new List<string> {
                "Group run of " + (s.Zone.Zone.displayName ?? s.ZoneId) + " (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")",
                "Party: you (" + s.ClassDef.displayName + ", level " + s.Progress.Level + ") and " + string.Join(", ", s.PartySims.Where(c => c != null).Select(c => c.sim.name + " the " + SimRoster.ClassName(c.sim.classId))),
                "Game time: " + (int)(total / 60) + " min " + ((int)(total % 60)).ToString("00") + " s (run at " + Speed + "x; the time is the game's)",
                "Camps cleared: " + cleared + " of " + camps + (done ? " (the run done)" : " (stopped before the end)"),
                "Mobs down at the end: " + slain + "; elites slain: " + elites,
                "Your deaths: " + deaths + "; warps to the leader when stuck: " + warps,
                "The gates were opened for the run (their quests are not done here).",
                "Meter over " + Mathf.RoundToInt(s.Meter.CombatSeconds) + " s of fighting:" };
            summary.AddRange(s.Meter.Lines().Select(l => "  " + l));
            File.WriteAllLines(Path.Combine(directory, "run-summary.txt"), summary);
            File.WriteAllLines(Path.Combine(directory, "run-log.txt"), log);
            Debug.Log("RUN_CAPTURE_DONE " + string.Join(" | ", summary));
            Application.Quit(0);
        }
    }
}
