using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// Every class's talent file at the cap (Chris, 2026-10-16: "raise top level to 20"): each tree loads with its kit's implemented
    /// talents, and at level 20 (21 points) every branch of every class can spend all 21 in itself, so a single-branch build is never
    /// left with a point it cannot place (Docs/TALENT_DEPTH.md, "The tiers at 20"). Tier t opens at 5t points in the branch, so tier 4
    /// takes the 21st point and no more: the extra ranks are tier 3's.
    /// </summary>
    public class TalentDataTests
    {
        static readonly Dictionary<string, string[]> Kits = new Dictionary<string, string[]> {
            { "archivist", ArchivistKit.ImplementedIds }, { "cleric", ClericKit.ImplementedIds }, { "druid", DruidKit.ImplementedIds },
            { "mage", MageKit.ImplementedIds }, { "paladin", PaladinKit.ImplementedIds }, { "ranger", RangerKit.ImplementedIds },
            { "rogue", RogueKit.ImplementedIds }, { "saltmender", SaltMenderKit.ImplementedIds }, { "scoundrel", ScoundrelKit.ImplementedIds },
            { "shaman", ShamanKit.ImplementedIds }, { "warrior", WarriorKit.ImplementedIds } };
        static string Json(string id) { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/" + id + ".json")); }
        static TalentTree Tree(string id) { return TalentTree.Parse("class." + id, Json(id), EncounterProgress.TalentCap, Kits[id]); }

        /// <summary>The hybrid classes (EncounterSession.HybridClasses; Chris, 2026-10-16, "A bit lower": their damage paths sit at 90% of
        /// the pure classes') are those with a tank's or a healer's path besides a damage path.</summary>
        [Test] public void The_hybrids_are_the_classes_with_a_tank_or_healer_path()
        {
            var hybrids = Kits.Keys.Where(id => { var roles = Tree(id).Branches.Select(b => b.role).ToList(); return roles.Contains("dps") && (roles.Contains("tank") || roles.Contains("healer")); })
                .Select(id => "class." + id).ToList();
            CollectionAssert.AreEquivalent(hybrids, EncounterSession.HybridClasses);
            foreach (var id in hybrids)
                foreach (var b in Tree(id.Substring(6)).Branches.Where(b => b.role == "dps"))
                    Assert.IsTrue(EncounterSession.BuildDamageScale.ContainsKey(id + "/" + b.id), id + "/" + b.id + ": a hybrid's damage path has its scale (x0.9 of the pure band)");
        }
        [Test] public void Every_class_file_has_a_kit_and_loads_at_the_cap()
        {
            var files = Directory.GetFiles(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents"), "*.json").Select(Path.GetFileNameWithoutExtension).ToList();
            CollectionAssert.AreEquivalent(Kits.Keys, files, "A kit for every talent file.");
            foreach (var id in Kits.Keys)
            {
                var t = Tree(id); int count = 0; foreach (var b in t.Branches) count += b.nodes.Length;
                Assert.AreEqual(Kits[id].Length, count, id + ": every implemented id is a built node and every built node has code.");
                Assert.AreEqual(Kits[id].Length, Kits[id].Distinct().Count(), id + ": no id twice.");
                Assert.AreEqual(EncounterProgress.LevelCap + 1, t.Budget(EncounterProgress.LevelCap), id + ": 21 points at 20.");
            }
        }

        /// <summary>At 20 each branch takes all 21 points in itself: bought a rank at a time, lowest tier first, through the tree's own rules
        /// (TalentTree.Propose: tiers, prerequisites, exclusive groups, the budget).</summary>
        [Test] public void At_twenty_every_branch_can_spend_21_in_itself()
        {
            var problems = new List<string>();
            foreach (var id in Kits.Keys)
            {
                var t = Tree(id);
                foreach (var b in t.Branches)
                {
                    var p = EncounterSession.FreshProgress(); p.classId = "class." + id; p.experience = EncounterProgress.XpForLevel(EncounterProgress.LevelCap);
                    Assert.AreEqual(EncounterProgress.LevelCap, p.Level);
                    var order = b.nodes.OrderBy(n => n.tier).ThenBy(n => n.col).ToList(); bool bought = true;
                    while (bought && t.Available(p) > 0)
                    {
                        bought = false;
                        foreach (var n in order)
                            if (t.Propose(p, n.id, 1, out var next, out _)) { p.talents = next; bought = true; break; }
                    }
                    if (t.Available(p) != 0) problems.Add(id + "/" + b.id + ": " + TalentTree.Spent(p) + " of " + t.Budget(p.Level) + " points placed in the branch alone.");
                    else Assert.IsTrue(t.Validate(p, out var why), id + "/" + b.id + ": " + why);
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>The level-20 nodes are modest (each about 3% a rank at most) and named in the game's own words: a node's desc never
        /// promises more than 3% a rank (5% on a one-rank node) more or less of anything (a health threshold or a slow is not a measure), and no name is used twice in a class.</summary>
        [Test] public void Names_are_unique_in_a_class_and_percentages_stay_modest()
        {
            foreach (var id in Kits.Keys)
            {
                var data = JsonUtility.FromJson<TalentTreeData>(Json(id)); var names = new HashSet<string>();
                foreach (var b in data.branches) foreach (var n in b.nodes)
                {
                    Assert.IsTrue(names.Add(n.name), id + ": '" + n.name + "' twice.");
                    Assert.IsFalse(string.IsNullOrEmpty(n.desc), n.id + " says what it does.");
                }
            }
            // The cap-20 nodes (tools/wip/talents19.py, built 2026-10-16): 3% a rank at most, 5% on a one-rank node.
            foreach (var id in Kits.Keys)
                foreach (var b in JsonUtility.FromJson<TalentTreeData>(Json(id)).branches)
                    foreach (var n in b.nodes.Where(n => Level20.Contains(n.id)))
                    {
                        Assert.IsTrue(n.impl, n.id + " is built.");
                        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(n.desc, @"(\d+)% (more|less|harder|softer|faster|stronger)"))
                            Assert.LessOrEqual(int.Parse(m.Groups[1].Value), n.max == 1 ? 5 : 3, n.id + ": " + n.desc);
                    }
        }
        /// <summary>The nodes built for the cap of 20 (ZONE_LOWTOWNS.md 7.2; TALENT_DEPTH.md).</summary>
        public static readonly HashSet<string> Level20 = new HashSet<string> {
            "vw-long-stillness", "lu-long-echo", "lu-half-waking",
            "ln-trimmed-wick", "ln-lamp-held-low", "ch-even-breath", "ch-answering-hills", "cn-burning-word", "cn-ashes-returned", "wd-patient-ward", "wd-shrine-holds", "wd-renew",
            "cb-even-flame", "cb-kept-coals", "hw-thick-smoke", "sb-open-hands", "bc-ossuary-coat",
            "og-iron-oath", "ju-sentence-passed", "sa-shared-mercy",
            "mk-practised-draw", "bb-scarred-hide", "bb-shared-hunt", "pf-lasting-mark", "sk-second-wind",
            "tg-gone-clean", "tg-unremarked", "sw-spare-motion", "sw-close-work", "lk-practised-hands", "lk-pocket-dust", "bk-crowd-pleaser",
            "ss-ready-salt",
            "ct-old-grudge", "ct-collected", "st-honeyed-words", "st-rude-awakening", "fx-second-chorus", "fx-greased-palms",
            "rs-soft-rain", "rs-spring-tide", "sc-low-clouds", "sc-heavy-sky", "sb-bedrock", "sb-stone-call", "gw-far-wind", "gw-clinging-mist" };
    }
}
