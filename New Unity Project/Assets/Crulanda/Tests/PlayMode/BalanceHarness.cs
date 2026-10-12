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
        public static float FightSeconds = 90; public static int Seeds = 3; public static float Step = .05f;   // game seconds a frame (Time.captureDeltaTime): as fast as the editor runs, the same steps every run
        static readonly string[] AllClasses = { "class.warrior", "class.druid", "class.paladin", "class.ranger", "class.mage", "class.rogue", "class.archivist", "class.saltmender" };
        /// <summary>CRULANDA_BALANCE_CLASSES=mage,archivist runs those only (about two a chunk); each set writes its own balance-<set>.csv.</summary>
        static string[] Classes { get { var f = Environment.GetEnvironmentVariable("CRULANDA_BALANCE_CLASSES"); if (string.IsNullOrEmpty(f)) return AllClasses;
            var want = f.Split(',').Select(x => x.Trim().ToLowerInvariant()).Select(x => x.StartsWith("class.") ? x : "class." + x).ToList(); return want.Where(AllClasses.Contains).ToArray(); } }
        static string CsvName { get { var f = Environment.GetEnvironmentVariable("CRULANDA_BALANCE_CLASSES"); return string.IsNullOrEmpty(f) ? "balance.csv" : "balance-" + f.Replace(",", "-").Replace(" ", "") + ".csv"; } }
        static string Out { get { var d = Environment.GetEnvironmentVariable("CRULANDA_BALANCE_OUT"); return string.IsNullOrEmpty(d) ? @"D:\crulanda-work\outputs\balance" : d; } }
        readonly List<string> csv = new List<string> { "class,build,role,scenario,seed,seconds,dps,hps,resource_max,resource_used,time_to_oom,foes_hit" };
        // Never in the rotation: forms (the build's own is taken once at the start), escapes and crowd control that would end the fight;
        // once a fight: summons and standing auras.
        static readonly HashSet<string> Never = new HashSet<string> { "druid.barkhide", "druid.thornclaw", "druid.rootmend", "druid.thornsong", "mage.unbind", "mage.quench", "mage.bind",
            "rogue.vanish", "rogue.tumble", "rogue.sap", "rogue.blind", "ranger.disengage", "druid.sleep_of_the_wood" };
        static readonly HashSet<string> Once = new HashSet<string> { "mage.raise_thrall", "ranger.call_companion", "paladin.oath", "paladin.colours" };
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
                string gear = Gear(); csv.Add("# " + cls + " gear: " + gear);
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
            Assert.IsEmpty(over, "non-DPS builds over their damage cap (note 177): " + string.Join("; ", over));
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
        static void Dummy(EncounterEnemy e)
        {
            e.enabled = false; var a = e.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (a != null && a.isOnNavMesh) a.isStopped = true;
            e.actor.Health.Pool.SetMax(5000000, false); e.actor.Health.Pool.SetCurrent(5000000);
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
            yield return null;
            var used1 = new HashSet<string>();
            { int form = SlotOf((cls == "class.druid" ? "druid." : "") + branch.id); if (form >= 0 && s.Kit.ActionLockLabel(form) == null) { s.UseAbility(form); yield return new WaitForSeconds(1.6f); } }
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
            s.Meter.Reset(); var pool = s.Player.Resource.Pool; int last = pool.Current, used = 0; float oom = -1, t = 0, hurt = 0;
            Time.captureDeltaTime = Step;
            while (t < FightSeconds)
            {
                yield return null; float dt = Time.deltaTime; t += dt;
                if (targets > 0 && (s.Target == null || !s.Target.actor.IsAlive)) { var f = foes.FirstOrDefault(x => x != null && x.actor.IsAlive); if (f != null) s.Select(f); }
                if (targets == 0) { hurt += dt; if (hurt >= 1) { hurt -= 1; s.Player.Health.ApplyDamage(Mathf.Min(s.Player.Health.Pool.Current - 1, Mathf.RoundToInt(s.Player.Health.Pool.Max * .04f))); } }
                if (s.Player.Health.Pool.Current < s.Player.Health.Pool.Max * .3f && targets > 0) s.Player.Health.Pool.Fill();
                if (!s.PlayerCasting)   // a cast in hand is let finish
                foreach (int slot in order.OrderByDescending(i => targets == 0 && s.Kit.ActionAt(i) != null && s.Kit.ActionAt(i).effect == AbilityEffect.Heal ? 1 : 0).ThenByDescending(i => s.Kit.ActionAt(i) != null ? s.Kit.ActionAt(i).cooldown : 0).ThenBy(i => lastUse.TryGetValue(i, out var w) ? w : -1))   // equal ones taken in turn (a Thornsong alternates Seedshot and Thornbolt)
                {
                    var ab = s.Kit.ActionAt(slot); if (ab == null || s.Kit.ActionLockLabel(slot) != null || Never.Contains(ab.id) || used1.Contains(ab.id)) continue;
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
