using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    [Serializable] public sealed class TalentRank { public string id; public int rank; }

    // JSON shape shared with the design calculator (workspace work\calculator\trees\<class>.json).
    // Extra calculator-only fields such as "emphasis" are ignored by JsonUtility.
    [Serializable] public sealed class TalentTreeData { public string id, name, resource; public TalentBranchData[] branches; }
    [Serializable] public sealed class TalentBranchData { public string id, name, role, loop, tradeoff; public TalentNodeData[] nodes; }
    [Serializable] public sealed class TalentNodeData
    {
        public string id, name, kind, icon, desc, req, exclusive;
        public int tier, col, max;
        public bool impl;
    }

    /// <summary>
    /// Class talent tree loaded from data. Only nodes flagged "impl" are playable; every such node must have code
    /// behind it (checked against the host's implemented-effect list). Rules match the calculator exactly:
    /// tier t needs 5*t points already spent in that branch, a prerequisite must be fully ranked, and the
    /// budget is level + 1 (a starting point plus one per level).
    /// </summary>
    public sealed class TalentTree
    {
        public const int PointsPerTier = 5;
        public readonly string ClassId;
        public readonly int LevelCap;
        public readonly List<TalentBranchData> Branches = new List<TalentBranchData>();
        readonly Dictionary<string, TalentNodeData> nodes = new Dictionary<string, TalentNodeData>(StringComparer.Ordinal);
        readonly Dictionary<string, string> branchOf = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly BuildTree rules;

        TalentTree(string classId, int levelCap, List<BuildNode> built)
        {
            ClassId = classId; LevelCap = levelCap; rules = new BuildTree(built);
        }

        public static TalentTree Parse(string classId, string json, int levelCap, ICollection<string> implementedEffects)
        {
            if (string.IsNullOrWhiteSpace(classId) || string.IsNullOrWhiteSpace(json) || levelCap < 1)
                throw new ArgumentException("Talent tree source missing.");
            var data = JsonUtility.FromJson<TalentTreeData>(json);
            if (data == null || data.branches == null || data.branches.Length == 0) throw new ArgumentException("Talent tree has no branches.");
            var playable = new List<TalentBranchData>(); var built = new List<BuildNode>();
            var all = new Dictionary<string, TalentNodeData>(StringComparer.Ordinal);
            foreach (var b in data.branches)
            {
                if (b == null || string.IsNullOrWhiteSpace(b.id) || b.nodes == null) throw new ArgumentException("Invalid talent branch.");
                foreach (var n in b.nodes)
                    if (n == null || string.IsNullOrWhiteSpace(n.id) || all.ContainsKey(n.id)) throw new ArgumentException("Invalid or duplicate talent id.");
                    else all.Add(n.id, n);
            }
            foreach (var b in data.branches)
            {
                var kept = new List<TalentNodeData>();
                foreach (var n in b.nodes)
                {
                    if (!n.impl) continue;
                    if (implementedEffects != null && !implementedEffects.Contains(n.id))
                        throw new ArgumentException("Talent '" + n.id + "' is flagged implemented but has no effect code.");
                    TalentNodeData parent = null;
                    if (!string.IsNullOrEmpty(n.req) && (!all.TryGetValue(n.req, out parent) || !parent.impl))
                        throw new ArgumentException("Talent '" + n.id + "' requires an unimplemented talent.");
                    kept.Add(n);
                    built.Add(new BuildNode { id = n.id, branch = b.id, maxRank = n.max, requiredLevel = 1,
                        prerequisite = string.IsNullOrEmpty(n.req) ? null : n.req, prerequisiteRank = parent == null ? 1 : parent.max,
                        branchPointsRequired = PointsPerTier * n.tier, exclusiveGroup = string.IsNullOrEmpty(n.exclusive) ? null : n.exclusive });
                }
                if (kept.Count > 0)
                    playable.Add(new TalentBranchData { id = b.id, name = b.name, role = b.role, loop = b.loop, tradeoff = b.tradeoff, nodes = kept.ToArray() });
            }
            if (built.Count == 0) throw new ArgumentException("Talent tree has no implemented talents.");
            var tree = new TalentTree(classId, levelCap, built);
            tree.Branches.AddRange(playable);
            foreach (var b in playable) foreach (var n in b.nodes) { tree.nodes.Add(n.id, n); tree.branchOf.Add(n.id, b.id); }
            return tree;
        }

        public TalentNodeData Find(string id) { return id != null && nodes.TryGetValue(id, out var n) ? n : null; }
        public TalentBranchData BranchOf(string id) { return id != null && branchOf.TryGetValue(id, out var b) ? Branches.Find(x => x.id == b) : null; }
        public int Budget(int level) { return Math.Max(1, Math.Min(LevelCap, level)) + 1; }
        public static int Rank(EncounterProgress progress, string id)
        { return progress?.talents?.Find(t => t != null && t.id == id)?.rank ?? 0; }
        public static int Spent(EncounterProgress progress)
        { int count = 0; if (progress?.talents != null) foreach (var t in progress.talents) if (t != null) count += t.rank; return count; }
        public int SpentIn(EncounterProgress progress, TalentBranchData branch)
        { int count = 0; foreach (var n in branch.nodes) count += Rank(progress, n.id); return count; }
        public int Available(EncounterProgress progress) { return Budget(progress.Level) - Spent(progress); }

        public bool Validate(EncounterProgress progress, out string reason)
        {
            reason = null;
            if (progress == null || progress.classId != ClassId || progress.talents == null) { reason = "Unsupported class or missing talent allocation."; return false; }
            var ranks = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var t in progress.talents)
            {
                if (t == null || string.IsNullOrWhiteSpace(t.id) || ranks.ContainsKey(t.id)) { reason = "Invalid or duplicate talent."; return false; }
                ranks.Add(t.id, t.rank);
            }
            return rules.Validate(ranks, progress.Level, Budget(progress.Level), out reason);
        }

        public bool Propose(EncounterProgress progress, string id, int delta, out List<TalentRank> proposed, out string reason)
        {
            proposed = new List<TalentRank>();
            foreach (var t in progress.talents) proposed.Add(new TalentRank { id = t.id, rank = t.rank });
            var entry = proposed.Find(t => t.id == id);
            if (entry == null) { entry = new TalentRank { id = id }; proposed.Add(entry); }
            entry.rank += delta;
            var check = new EncounterProgress { classId = progress.classId, experience = progress.experience, talents = proposed };
            if (!Validate(check, out reason)) { reason = Friendly(reason, id, delta, progress); return false; }
            proposed.RemoveAll(t => t.rank == 0); return true;
        }

        /// <summary>Plain-language reason a node cannot take its next rank (for tooltips).</summary>
        public string Requirement(EncounterProgress progress, string id)
        {
            var n = Find(id); if (n == null) return "Unknown talent.";
            if (Rank(progress, id) >= n.max) return "Fully ranked.";
            var parts = new List<string>();
            var branch = BranchOf(id);
            int need = PointsPerTier * n.tier, have = SpentIn(progress, branch);
            if (have < need) parts.Add("Requires " + need + " points in " + branch.name + " (" + have + ")");
            var parent = Find(n.req);
            if (parent != null && Rank(progress, parent.id) < parent.max) parts.Add("Requires " + parent.max + "/" + parent.max + " " + parent.name);
            if (Available(progress) <= 0) parts.Add("No points left (level " + progress.Level + ")");
            return parts.Count == 0 ? "Available." : string.Join(" · ", parts);
        }

        string Friendly(string reason, string id, int delta, EncounterProgress progress)
        {
            if (delta > 0) { var r = Requirement(progress, id); return r == "Available." ? reason : r; }
            return "Other talents depend on this rank.";
        }
    }
}
