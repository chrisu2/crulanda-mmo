#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.Abilities;
using Crulanda.Combat;
using Crulanda.Gameplay;

namespace Crulanda.Tests
{
    /// <summary>
    /// The balance harness (note 174, Chris 17:13: "test all the new classes and all the new test builds and compare dps and hps to see if
    /// they are viable builds"). Every class, every talent branch bought out at level 15, on the game's own kits in a live session:
    /// a single-target dummy, three dummies together, and (for a branch whose role heals) the player hurt every second. The rotation is
    /// crude on purpose and the same for all: every tick the first slot that will go, goes. Seeded runs; numbers to
    /// D:\crulanda-work\outputs\balance\balance.csv and BALANCE_TABLE.md. Explicit: run by name only.
    /// </summary>
    [Explicit("long: the balance harness, run by name"), Category("Balance")]
    public class BalanceHarness
    {
        public static float FightSeconds = 180, Step = .05f; public static int Seeds = 3;   // three minutes: long enough to average the cooldown cycles (the training-dummy practice Chris pointed to, 2026-10-13); was 90   // game seconds a frame (Time.captureDeltaTime): as fast as the editor runs, the same steps every run
        static readonly string[] AllClasses = { "class.warrior", "class.druid", "class.paladin", "class.ranger", "class.mage", "class.rogue", "class.archivist", "class.saltmender", "class.cleric", "class.shaman" };
        /// <summary>CRULANDA_BALANCE_CLASSES=mage,archivist runs those only (about two a chunk); each set writes its own balance-<set>.csv.</summary>
        static string[] Classes { get { var f = Environment.GetEnvironmentVariable("CRULANDA_BALANCE_CLASSES"); if (string.IsNullOrEmpty(f)) return AllClasses;
            var want = f.Split(',').Select(x => x.Trim().ToLowerInvariant()).Select(x => x.StartsWith("class.") ? x : "class." + x).ToList(); return want.Where(AllClasses.Contains).ToArray(); } }
        static string CsvName { get { var f = Environment.GetEnvironmentVariable("CRULANDA_BALANCE_CLASSES"); return string.IsNullOrEmpty(f) ? "balance.csv" : "balance-" + f.Replace(",", "-").Replace(" ", "") + ".csv"; } }
        static string Out { get { var d = Environment.GetEnvironmentVariable("CRULANDA_BALANCE_OUT"); return string.IsNullOrEmpty(d) ? @"D:\crulanda-work\outputs\balance" : d; } }
        readonly List<string> csv = new List<string> { "class,build,role,scenario,seed,seconds,dps,hps,resource_max,resource_used,time_to_oom,foes_hit" };
        // Never in the rotation: forms (the build's own is taken once at the start), escapes and crowd control that would end the fight;
        // once a fight: summons and standing auras.
        static readonly HashSet<string> Never = new HashSet<string> { "druid.barkhide", "druid.thornclaw", "druid.rootmend", "druid.thornsong", "mage.unbind", "mage.quench", "mage.bind",
            "rogue.vanish", "rogue.tumble", "rogue.sap", "rogue.blind", "ranger.disengage", "druid.sleep_of_the_wood",
            "archivist.cadence", "archivist.dirge" };   // songs of pace and slowing: nothing a dummy shows, and one would replace the Song of Mending
        /// <summary>Kept up: a damage over time with no cooldown, recast when its last use is this many seconds old (2026-10-13: the Cleric's
        /// Brand costs what Rebuke costs and has no cooldown, so the rotation always reached Rebuke first and never cast it).</summary>
        static readonly Dictionary<string, float> Every = new Dictionary<string, float> { { "cleric.brand", 7.5f } };
        static readonly HashSet<string> Once = new HashSet<string> { "mage.raise_thrall", "ranger.call_companion", "paladin.oath", "paladin.colours",
            "archivist.song_of_mending", "salt.mender_craft" };   // 2026-10-13: the healers' songs and chants are toggles: started once (they were never started, or toggled off again)
        int SlotOf(string id) { for (int i = 0; i < s.Kit.ActionCount; i++) { var a = s.Kit.ActionAt(i); if (a != null && a.id == id) return i; } return -1; }
        string root; EncounterSession s;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var x = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (x != null) x.SaveDirectoryOverride = root; }

        [UnityTest, Timeout(3600000)] public IEnumerator Every_class_and_build_on_the_dummies()
        {
            Directory.CreateDirectory(Out);
            foreach (var cls in Classes)
            {
                root = Path.Combine(Path.GetTempPath(), "Crulanda-balance-" + Guid.NewGuid().ToString("N"));
                EncounterSession.StartClassOverride = cls; SceneManager.sceneLoaded += OnLoaded;
                yield return SceneManager.LoadSceneAsync("PlayableEncounter", LoadSceneMode.Single);
                SceneManager.sceneLoaded -= OnLoaded; yield return null; yield return null;
                s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
                if (s == null || s.Kit == null) { csv.Add(cls + ",LOAD FAILED,,,,,,,,,,"); continue; }
                if (s.Companion != null) s.Companion.gameObject.SetActive(false);
                s.Progress.experience = EncounterProgress.XpForLevel(15); s.Player.SetLevel(15);   // the gear asks for the level
                string gear = Gear(); csv.Add("# " + cls + " gear: " + gear); if (cls == Classes[0]) csv.Add("# " + DummyNote);
                foreach (var branch in s.Talents.Branches.ToList())
                    for (int seed = 0; seed < Seeds; seed++)
                    {
                        string role = (branch.role ?? "").ToLowerInvariant();
                        yield return Fight(cls, branch, role, "single", seed, 1);
                        yield return Fight(cls, branch, role, "aoe3", seed, 3);
                        if (role.Contains("heal")) yield return Fight(cls, branch, role, "heal", seed, 0);
                    }
                File.WriteAllLines(Path.Combine(Out, CsvName), csv);
                EncounterSession.StartClassOverride = null;
                var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync("PlayableEncounter");
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
            Time.captureDeltaTime = 0;
            File.WriteAllLines(Path.Combine(Out, CsvName), csv);
            File.WriteAllText(Path.Combine(Out, CsvName.Replace(".csv", "") + "_TABLE.md"), Table(csv));
            Assert.Greater(csv.Count, 1, "rows");
            var over = CapViolations(csv); File.WriteAllLines(Path.Combine(Out, "role_caps.txt"), over.DefaultIfEmpty("all non-DPS builds under their caps"));
            // The caps are against the DPS-role median of the whole roster: a set of two classes has no such median (the cleric,shaman set
            // failed on a median of 21.9, 2026-10-13), so the check holds only when every class ran; tools/validation/balance_merge.py checks sets.
            if (Classes.Length == AllClasses.Length) Assert.IsEmpty(over, "non-DPS builds over their damage cap (note 177): " + string.Join("; ", over));
        }

        // ---------- The party heal test (note 178, Chris 19:18: "still need other healers to test. paladin, cleric (new class) , shaman (new class), salt mender") ----------
        static readonly (string cls, string build)[] Healers = { ("class.paladin", "sanctuary"), ("class.saltmender", "mender"), ("class.archivist", "the-cantor"), ("class.druid", "rootmend"), ("class.cleric", "lantern"), ("class.cleric", "choir"), ("class.shaman", "rainspeaker") };
        /// <summary>Each healer build with Mira and three sims (level-nearest, not healers) for 90 s: every member loses 5% a second
        /// (spread) and one, by the seed, 45% every 4 s (spike); no one is let die (held at 1). HPS, overheal, the share of the healing
        /// that went to others than the healer, mana, and how often a member was under 25%. To party_heal.csv / PARTY_HEAL.md.</summary>
        [UnityTest, Timeout(1800000)] public IEnumerator Party_heal_test()
        {
            Directory.CreateDirectory(Out);
            var rows = new List<string> { "class,build,seed,seconds,members,hps,overheal_pct,others_share_pct,mana_max,mana_used,mana_left,low_member_seconds" };
            foreach (var (cls, build) in Healers)
            {
                root = Path.Combine(Path.GetTempPath(), "Crulanda-balance-" + Guid.NewGuid().ToString("N"));
                EncounterSession.StartClassOverride = cls; SceneManager.sceneLoaded += OnLoaded;
                yield return SceneManager.LoadSceneAsync("PlayableEncounter", LoadSceneMode.Single);
                SceneManager.sceneLoaded -= OnLoaded; yield return null; yield return null;
                s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
                var branch = s?.Talents?.Branches.FirstOrDefault(b => b.id == build);
                if (s == null || branch == null) { rows.Add(cls + "," + build + ",LOAD FAILED"); continue; }
                s.Progress.experience = EncounterProgress.XpForLevel(15); s.Player.SetLevel(15); Gear();
                foreach (var e in s.Enemies.ToList()) if (e != null) Dummy(e);   // no fight: the hurt is the harness's
                s.Progress.recruited = true; if (s.Companion != null) { s.Companion.gameObject.SetActive(true); s.Companion.MatchLevel(15); }
                var pop = SimPopulation.Active; int added = 0;
                // PlayableEncounter has no zone, so no SimPopulation (party1: "[no SimPopulation]"): the harness makes one on its own save root.
                if (pop == null) { pop = new GameObject("Sims").AddComponent<SimPopulation>(); pop.Init(s, root); yield return null; } string diag = pop == null ? "no SimPopulation" : pop.World == null ? "no sim world" : "";
                // Three sims put in this zone at your side and online all day (the test scene has none standing about), then invited as
                // returning members, the way a saved party comes back (SimPopulation.RestoreParty). Not healers: the healing is the build's.
                if (pop != null && pop.World != null)
                {
                    var pick = pop.World.sims.Where(x => x.classId != "class.saltmender" && x.classId != "class.paladin" && x.classId != "class.archivist" && x.classId != "class.druid" && x.classId != "class.cleric" && x.classId != "class.shaman")
                        .OrderBy(x => Mathf.Abs(x.level - 15)).ThenBy(x => x.id).Take(3).ToList();
                    int k = 0;
                    foreach (var sim in pick)
                    {
                        var at = s.Player.transform.position - s.Player.transform.forward * (2 + k) + s.Player.transform.right * (k % 2 == 0 ? 1.5f : -1.5f); k++;
                        sim.zone = pop.ZoneId; sim.x = at.x; sim.z = at.z; sim.onlineFrom = 0; sim.onlineHours = 24;
                    }
                    pop.Refresh();
                    foreach (var sim in pick) if (pop.Find(sim.id) == null) pop.Figures.Add(SimFigure.Spawn(pop, sim));   // no zone here, so Refresh places nobody: stood up by hand
                    for (float w0 = Time.time; Time.time - w0 < 6 && pick.Any(x => pop.Find(x.id) == null); ) { yield return new WaitForSeconds(.25f); pop.Refresh(); }   // the figures come in over frames
                    diag = "pop ok, picked " + pick.Count + ", figures " + pick.Count(x => pop.Find(x.id) != null) + " (zone " + pop.ZoneId + ")";
                    foreach (var sim in pick) if (s.Invite(sim.id, true)) added++;
                }
                rows.Add("# party " + cls + " " + build + ": " + added + " sims joined [" + diag + "]: " + string.Join(" ", s.PartySims.Where(c => c != null).Select(c => c.sim.name + "(" + c.sim.classId.Replace("class.", "") + " " + c.sim.level + ")")));
                yield return null;
                for (int seed = 0; seed < Seeds; seed++) yield return PartyFight(cls, branch, seed, rows);
                EncounterSession.StartClassOverride = null;
                var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync("PlayableEncounter");
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
            Time.captureDeltaTime = 0;
            File.WriteAllLines(Path.Combine(Out, "party_heal.csv"), rows);
            Assert.Greater(rows.Count, 1, "rows");
        }
        IEnumerator PartyFight(string cls, TalentBranchData branch, int seed, List<string> rows)
        {
            var rng = new System.Random(1000 + seed);
            Buy(branch); s.Kit.ClearTransient(); s.Select(null);
            var members = new List<Actor> { s.Player }; if (s.Companion != null && s.Companion.gameObject.activeInHierarchy) members.Add(s.Companion.actor);
            foreach (var c in s.PartySims) if (c != null) members.Add(c.actor);
            var at = s.Player.transform.position;
            for (int i = 1; i < members.Count; i++)
            {
                var p = at + Quaternion.Euler(0, i * 72, 0) * Vector3.forward * 2.5f; var ag = members[i].GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (ag != null && ag.isOnNavMesh) ag.Warp(p); else members[i].transform.position = p;
            }
            foreach (var m in members) { if (m.Health.Pool.Current <= 0) m.Health.Revive(m.Health.Pool.Max); m.Health.Pool.Fill(); }
            s.Player.Resource.Pool.Fill();
            { int form = SlotOf((cls == "class.druid" ? "druid." : "") + branch.id);   // the build's form, retried until it holds (a shift on the GCD failed and left the last build's form on)
              for (float w0 = Time.time; form >= 0 && Time.time - w0 < 6 && s.Kit.ActionLockLabel(form) != "Active"; ) { if (!s.PlayerCasting) s.UseAbility(form); yield return new WaitForSeconds(.25f); }
              if (form >= 0) { yield return new WaitForSeconds(.5f); csv.Add("# form " + cls + " " + branch.id + ": " + (s.Kit.ActionLockLabel(form) ?? "not held")); } }
            if (seed > 0) yield return new WaitForSeconds(.4f * seed);
            long tried = 0, healed = 0, others = 0;
            Action<Actor, Actor, int, int> onTry = (by, who, amount, got) => { if (by != s.Player) return; tried += amount; healed += got; if (who != s.Player) others += got; };
            Combatant.HealAttempted += onTry;
            var pool = s.Player.Resource.Pool; int last = pool.Current, used = 0; float t = 0, tick = 0, spike = 0, low = 0;
            var lastUse = new Dictionary<int, float>(); var slots = Enumerable.Range(0, s.Kit.ActionCount).Where(i => s.Kit.ActionAt(i) != null).ToList();
            var sung = new HashSet<string>();   // songs and chants started (Once): never toggled off again
            Time.captureDeltaTime = Step;
            while (t < FightSeconds)
            {
                yield return null; float dt = Time.deltaTime; t += dt; tick += dt; spike += dt;
                if (tick >= 1) { tick -= 1; foreach (var m in members) m.Health.ApplyDamage(Mathf.Min(m.Health.Pool.Current - 1, Mathf.RoundToInt(m.Health.Pool.Max * .05f))); }
                if (spike >= 4) { spike -= 4; var m = members[rng.Next(members.Count)]; m.Health.ApplyDamage(Mathf.Min(m.Health.Pool.Current - 1, Mathf.RoundToInt(m.Health.Pool.Max * .45f))); }
                foreach (var m in members) if (m.Health.Pool.Ratio < .25f) low += dt;
                if (!s.PlayerCasting)
                    foreach (int slot in slots.OrderByDescending(i => s.Kit.ActionAt(i).effect == AbilityEffect.Heal ? 1 : 0).ThenByDescending(i => s.Kit.ActionAt(i).cooldown).ThenBy(i => lastUse.TryGetValue(i, out var w) ? w : -1))
                    {
                        var ab = s.Kit.ActionAt(slot); if (ab == null || s.Kit.ActionLockLabel(slot) != null || Never.Contains(ab.id) || sung.Contains(ab.id)) continue;
                        if (ab.effect == AbilityEffect.Damage) continue;   // a healer at work, no foe in reach
                        if (ab.cost > 0 && pool.Max > 0 && pool.Current - ab.cost < pool.Max * .1f) continue;
                        if (s.UseAbility(slot)) { lastUse[slot] = t; if (Once.Contains(ab.id)) sung.Add(ab.id); break; }
                    }
                int now = pool.Current; if (now < last) used += last - now; last = now;
            }
            Time.captureDeltaTime = 0; Combatant.HealAttempted -= onTry;
            rows.Add(string.Join(",", cls, branch.id, seed, t.ToString("0.0"), members.Count, (healed / t).ToString("0.0"), tried > 0 ? (100f * (tried - healed) / tried).ToString("0") : "",
                healed > 0 ? (100f * others / healed).ToString("0") : "", pool.Max, used, pool.Current, low.ToString("0.0")));
        }

        /// <summary>Standard gear: an uncommon piece at level 15 in every slot (ItemDatabase.GearId, seed 7), bagged and worn.</summary>
        string Gear()
        {
            if (s.Items == null) return "no item database";
            int worn = 0;
            foreach (var slot in ItemDatabase.SlotIds)
            {
                string id = ItemDatabase.GearId(slot, 15, 2, 7);
                try
                {
                    Inventory.Add(s.Progress, s.Items, id, 1);
                    for (int i = 0; i < s.Progress.bag.Count; i++) if (s.Progress.bag[i] != null && s.Progress.bag[i].item == id) { if (s.EquipFromBag(i)) worn++; break; }
                }
                catch (Exception e) { Debug.LogWarning("balance gear " + id + ": " + e.Message); }
            }
            return worn + " of " + ItemDatabase.SlotIds.Length + " slots worn";
        }
        void Buy(TalentBranchData branch)
        {
            s.Progress.talents = new List<TalentRank>();
            var ids = branch.nodes.Select(n => n.id).ToList();
            for (int pass = 0; pass < 40 && s.Talents.Available(s.Progress) > 0; pass++)
                foreach (var id in ids) if (s.Talents.Propose(s.Progress, id, 1, out var next, out _)) s.Progress.talents = next;
            s.Kit.ApplyStats();
        }
        /// <summary>A test dummy (Chris, 2026-10-13: "create test dummies to test the dps ... so there are no variables in the mobs"): whatever
        /// mob stood there, now level 15, no armour, not an elite, five million health, no blows and no moving.</summary>
        public const int DummyLevel = 15, DummyArmour = 0; public const string DummyNote = "dummies: level 15, armour 0, 5,000,000 health, no blows; the wounded dummy: Mira at 3,000 health losing 4% a second";
        static void Dummy(EncounterEnemy e)
        {
            e.enabled = false; var a = e.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (a != null && a.isOnNavMesh) a.isStopped = true;
            e.Elite = false; e.actor.SetLevel(DummyLevel); e.actor.Stats.SetBase(Crulanda.Core.StatType.Armor, DummyArmour);
            e.actor.Health.Pool.SetMax(5000000, false); e.actor.Health.Pool.SetCurrent(5000000);
        }
        /// <summary>The wounded dummy for the heal test ("wounded test dummies for the heal test"): Mira, a party member the kits mend, standing
        /// still, doing nothing, at 3,000 health; the harness wounds her 4% a second.</summary>
        public const int WoundedHealth = 3000; public const float WoundShare = .04f;
        HealerCompanion WoundedDummy()
        {
            var m = s.Companion; if (m == null) return null;
            s.Progress.recruited = true; m.gameObject.SetActive(true); m.MatchLevel(15); m.enabled = false;
            var ag = m.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (ag != null && ag.isOnNavMesh) ag.isStopped = true;
            m.actor.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, WoundedHealth); if (!m.actor.IsAlive) m.actor.Health.Revive(WoundedHealth); m.actor.Health.Pool.Fill();
            var at = s.Player.transform.position + s.Player.transform.forward * 2.5f; if (ag != null && ag.isOnNavMesh) ag.Warp(at); else m.transform.position = at;
            return m;
        }
        IEnumerator Fight(string cls, TalentBranchData branch, string role, string scenario, int seed, int targets)
        {
            UnityEngine.Random.InitState(1000 + seed);
            if (s.Player.Health.Pool.Current <= 0) s.Player.Health.Revive(s.Player.Health.Pool.Max);
            s.Progress.experience = EncounterProgress.XpForLevel(15); s.Player.SetLevel(15); Buy(branch);
            s.Player.Health.Pool.Fill(); s.Player.Resource.Pool.Fill(); s.Kit.ClearTransient();
            var foes = s.Enemies.Where(x => x != null && x.actor.IsAlive).OrderBy(x => x.transform.position.sqrMagnitude).Take(Math.Max(1, targets)).ToList();
            if (foes.Count == 0) { csv.Add(string.Join(",", cls, branch.id, role, scenario, seed, "NO FOE", "", "", "", "", "", "")); yield break; }
            var motor = s.Player.GetComponent<AdventurerMotor>(); var at = foes[0].transform.position;
            for (int i = 0; i < foes.Count; i++)
            {
                Dummy(foes[i]); var ag = foes[i].GetComponent<UnityEngine.AI.NavMeshAgent>();
                var p = at + Quaternion.Euler(0, i * 120 + seed * 23, 0) * Vector3.right * (i == 0 ? 0 : 1.4f + .15f * seed);
                if (targets == 0) p = at + Vector3.forward * 80 + Vector3.right * 3 * i;   // the heal test: no foe in reach, nothing to strike
                if (ag != null && ag.isOnNavMesh) ag.Warp(p); else foes[i].transform.position = p;
            }
            motor.Teleport(at + Vector3.back * 2.4f + Vector3.up * .5f); s.Player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            if (targets > 0) s.Select(foes[0]); else s.Select(null);
            var wounded = targets == 0 ? WoundedDummy() : null;
            yield return null;
            var used1 = new HashSet<string>();
            { int form = SlotOf((cls == "class.druid" ? "druid." : "") + branch.id);   // the build's form, retried until it holds (a shift on the GCD failed and left the last build's form on)
              for (float w0 = Time.time; form >= 0 && Time.time - w0 < 6 && s.Kit.ActionLockLabel(form) != "Active"; ) { if (!s.PlayerCasting) s.UseAbility(form); yield return new WaitForSeconds(.25f); }
              if (form >= 0) { yield return new WaitForSeconds(.5f); csv.Add("# form " + cls + " " + branch.id + ": " + (s.Kit.ActionLockLabel(form) ?? "not held")); } }
            // The fight is deterministic (no crit or damage rolls in combat), so the seed changes the opening instead: a late start of
            // .4 s a seed, and which of the equally-ranked abilities leads.
            if (seed > 0) yield return new WaitForSeconds(.4f * seed);
            var order = Enumerable.Range(0, s.Kit.ActionCount).Where(i => s.Kit.ActionAt(i) != null)
                .OrderByDescending(i => targets == 0 && s.Kit.ActionAt(i).effect == AbilityEffect.Heal ? 1 : 0)   // the heal test: mends first
                .ThenByDescending(i => s.Kit.ActionAt(i).cooldown)   // cooldowns (the splashes, the big hits) when ready, the filler last
                .ThenBy(i => (i + seed) % Math.Max(1, s.Kit.ActionCount)).ToList();
            int cheapest = order.Where(i => !Never.Contains(s.Kit.ActionAt(i).id) && !Once.Contains(s.Kit.ActionAt(i).id) && (targets == 0 ? s.Kit.ActionAt(i).effect == AbilityEffect.Heal : s.Kit.ActionAt(i).effect == AbilityEffect.Damage))
                .OrderBy(i => s.Kit.ActionAt(i).cost).DefaultIfEmpty(-1).First();
            var uses = new Dictionary<string, int>(); var lastUse = new Dictionary<int, float>();
            if (seed == 0) csv.Add("# locks " + cls + " " + branch.id + " " + scenario + ": " + string.Join(" ", Enumerable.Range(0, s.Kit.ActionCount).Where(i => s.Kit.ActionAt(i) != null && s.Kit.ActionLockLabel(i) != null).Select(i => s.Kit.ActionAt(i).id + "=" + s.Kit.ActionLockLabel(i).Replace(" ", "_"))));
            s.Meter.Reset(); s.Meter.RunName = cls.Replace("class.", "") + " / " + branch.id + " · " + scenario + " · seed " + seed;   // read on the meter when the fights run on screen (Chris 2026-10-13)
            var pool = s.Player.Resource.Pool; int last = pool.Current, used = 0; float oom = -1, t = 0, hurt = 0;
            Time.captureDeltaTime = Step;
            while (t < FightSeconds)
            {
                yield return null; float dt = Time.deltaTime; t += dt;
                if (targets > 0 && (s.Target == null || !s.Target.actor.IsAlive)) { var f = foes.FirstOrDefault(x => x != null && x.actor.IsAlive); if (f != null) s.Select(f); }
                if (targets == 0 && wounded != null) { hurt += dt; if (hurt >= 1) { hurt -= 1; wounded.actor.Health.ApplyDamage(Mathf.Min(wounded.actor.Health.Pool.Current - 1, Mathf.RoundToInt(wounded.actor.Health.Pool.Max * WoundShare))); } }
                else if (targets == 0) { hurt += dt; if (hurt >= 1) { hurt -= 1; s.Player.Health.ApplyDamage(Mathf.Min(s.Player.Health.Pool.Current - 1, Mathf.RoundToInt(s.Player.Health.Pool.Max * WoundShare))); } }   // no Mira in the scene: you
                if (s.Player.Health.Pool.Current < s.Player.Health.Pool.Max * .3f && targets > 0) s.Player.Health.Pool.Fill();
                if (!s.PlayerCasting)   // a cast in hand is let finish
                foreach (int slot in order.OrderByDescending(i => s.Kit.ActionAt(i) != null && Every.TryGetValue(s.Kit.ActionAt(i).id, out var keepEvery) && (!lastUse.TryGetValue(i, out var keptAt) || t - keptAt >= keepEvery) ? 1 : 0)
                    .ThenByDescending(i => targets == 0 && s.Kit.ActionAt(i) != null && s.Kit.ActionAt(i).effect == AbilityEffect.Heal ? 1 : 0)
                    .ThenBy(i => targets == 0 && lastUse.TryGetValue(i, out var healUsedAt) ? healUsedAt : -1)   // the heal test: heals take turns, so the first in slot order does not starve the rest (2026-10-13).ThenByDescending(i => s.Kit.ActionAt(i) != null ? s.Kit.ActionAt(i).cooldown : 0).ThenBy(i => s.Kit.ActionAt(i) != null && cls == "class.druid" && s.Kit.ActionAt(i).cooldown <= 0 && s.Kit.ActionAt(i).effect == AbilityEffect.Damage && lastUse.TryGetValue(i, out var w) ? w : -1).ThenBy(i => order.IndexOf(i)))   // a Druid's equal fillers taken in turn (Thornsong's loop); every other kit keeps bar order (v6/v7: the turn-taking cost the casters, Mage and Archivist, most of their damage) taken in turn (a Thornsong alternates Seedshot and Thornbolt)
                {
                    var ab = s.Kit.ActionAt(slot); if (ab == null || s.Kit.ActionLockLabel(slot) != null || Never.Contains(ab.id) || used1.Contains(ab.id)) continue;
                    // A damage fight casts no heals (2026-10-13: the Cleric and Shaman, whose bars lock nothing by build, spent most casts mending nobody).
                    if (targets > 0 && ab.effect == AbilityEffect.Heal) continue;
                    // Mana-aware: a costly ability waits while the pool is under a fifth, the cheapest one (the filler) never waits.
                    if (ab.cost > 0 && slot != cheapest && pool.Max > 0 && pool.Current - ab.cost < pool.Max * .2f) continue;
                    if (s.UseAbility(slot)) { if (Once.Contains(ab.id)) used1.Add(ab.id); lastUse[slot] = t; uses[ab.id] = (uses.TryGetValue(ab.id, out var n) ? n : 0) + 1; break; }
                }
                int now = pool.Current; if (now < last) used += last - now; last = now;
                if (oom < 0 && pool.Max > 0 && pool.Ratio < .05f) oom = t;
            }
            Time.captureDeltaTime = 0;
            int hit = foes.Count(f => f != null && f.actor.Health.Pool.Current < 5000000);
            int dmg = 0, heal = 0; foreach (var r in s.Meter.Rows) { dmg += r.damage; heal += r.healing; }
            if (s.Player.Health.Pool.Current <= 0) s.Player.Health.Revive(s.Player.Health.Pool.Max);
            csv.Add(string.Join(",", cls, branch.id, role, scenario, seed, t.ToString("0.0"), (dmg / t).ToString("0.0"), (heal / t).ToString("0.0"), pool.Max, used, oom < 0 ? "" : oom.ToString("0.0"), hit));
            if (seed == 0) csv.Add("# uses " + cls + " " + branch.id + " " + scenario + ": " + string.Join(" ", uses.OrderByDescending(x => x.Value).Select(x => x.Key + "=" + x.Value)));
            foreach (var f in foes) if (f != null) { f.actor.Health.Pool.SetMax(5000000, false); f.actor.Health.Pool.SetCurrent(5000000); }
        }

        /// <summary>Note 177 (Chris 18:22, "control classes should not be top tier dps for sure" / "or healers"): single-target DPS of each
        /// healer, control and support build against the DPS-role median; healer at most 50%, control and support at most 75% (3 points'
        /// slack for "about").</summary>
        public static List<string> CapViolations(List<string> rows)
        {
            var data = rows.Skip(1).Select(l => l.Split(',')).Where(c => c.Length >= 11 && c[3] == "single" && float.TryParse(c[6], out _)).ToList();
            var builds = data.GroupBy(c => (c[0], c[1], c[2])).Select(g => (key: g.Key, dps: g.Average(c => float.Parse(c[6])))).ToList();
            var dps = builds.Where(b => b.key.Item3 == "dps").Select(b => b.dps).OrderBy(x => x).ToList(); if (dps.Count == 0) return new List<string>();
            float med = dps.Count % 2 == 1 ? dps[dps.Count / 2] : (dps[dps.Count / 2 - 1] + dps[dps.Count / 2]) / 2;
            var bad = new List<string>();
            foreach (var b in builds)
            {
                float cap = b.key.Item3.Contains("heal") ? EncounterSession.HealerDpsCap : b.key.Item3 == "control" ? EncounterSession.ControlDpsCap : b.key.Item3 == "support" ? EncounterSession.SupportDpsCap : -1;
                if (cap > 0 && b.dps > med * (cap + .03f)) bad.Add(b.key.Item1.Replace("class.", "") + " " + b.key.Item2 + " (" + b.key.Item3 + ") " + b.dps.ToString("0.0") + " = " + (100 * b.dps / med).ToString("0") + "% of " + med.ToString("0.0") + ", cap " + (cap * 100).ToString("0") + "%");
            }
            return bad;
        }
        /// <summary>Mean of the seeds a build and scenario; each build against its role's median in that scenario, flagged past 15%.</summary>
        public static string Table(List<string> rows)
        {
            var data = rows.Skip(1).Select(l => l.Split(',')).Where(c => c.Length >= 11 && float.TryParse(c[6], out _)).ToList();
            var groups = data.GroupBy(c => (c[0], c[1], c[2], c[3])).Select(g => (key: g.Key, dps: g.Average(c => float.Parse(c[6])), hps: g.Average(c => float.Parse(c[7])),
                used: g.Average(c => float.Parse(c[9])), oom: g.Select(c => c[10]).FirstOrDefault(x => x != ""))).ToList();
            float Median(IEnumerable<float> v) { var a = v.OrderBy(x => x).ToList(); return a.Count == 0 ? 0 : a.Count % 2 == 1 ? a[a.Count / 2] : (a[a.Count / 2 - 1] + a[a.Count / 2]) / 2; }
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("| class | build | role | scenario | DPS | HPS | resource used | time to OOM (s) | vs role median | flag |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (var g in groups.OrderBy(x => x.key.Item4).ThenBy(x => x.key.Item3).ThenBy(x => x.key.Item1))
            {
                bool healing = g.key.Item4 == "heal"; float v = healing ? g.hps : g.dps;
                float med = Median(groups.Where(x => x.key.Item4 == g.key.Item4 && x.key.Item3 == g.key.Item3).Select(x => healing ? x.hps : x.dps));
                float off = med > 0 ? (v - med) / med : 0;
                sb.AppendLine("| " + string.Join(" | ", g.key.Item1.Replace("class.", ""), g.key.Item2, g.key.Item3, g.key.Item4, g.dps.ToString("0.0"), g.hps.ToString("0.0"), g.used.ToString("0"), g.oom ?? "-",
                    (off * 100).ToString("+0;-0") + "%", Math.Abs(off) > .15f ? "**OUTLIER**" : "") + " |");
            }
            return sb.ToString();
        }
    }
}
#endif
