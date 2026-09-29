using System;
using System.Collections.Generic;

namespace Crulanda.Encounter
{
    [Serializable]
    public sealed class BuildNode
    {
        public string id, branch, prerequisite, exclusiveGroup;
        public int maxRank = 1, requiredLevel = 1, prerequisiteRank = 1, branchPointsRequired;
    }
    /// <summary>Class-scoped graph rules. Validate a proposed allocation before committing it or applying effects.</summary>
    public sealed class BuildTree
    {
        readonly Dictionary<string, BuildNode> nodes = new Dictionary<string, BuildNode>(StringComparer.Ordinal);
        public BuildTree(IEnumerable<BuildNode> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            foreach (var n in definitions)
            {
                if (n == null || string.IsNullOrWhiteSpace(n.id) || string.IsNullOrWhiteSpace(n.branch) ||
                    n.maxRank < 1 || n.requiredLevel < 1 || n.prerequisiteRank < 1 || n.branchPointsRequired < 0 || nodes.ContainsKey(n.id))
                    throw new ArgumentException("Invalid or duplicate build node.");
                // Snapshot authored data so later edits cannot invalidate the checked graph.
                nodes.Add(n.id, new BuildNode { id=n.id, branch=n.branch, prerequisite=n.prerequisite,
                    exclusiveGroup=n.exclusiveGroup, maxRank=n.maxRank, requiredLevel=n.requiredLevel,
                    prerequisiteRank=n.prerequisiteRank, branchPointsRequired=n.branchPointsRequired });
            }
            foreach (var n in nodes.Values)
            {
                var seen = new HashSet<string>(); var current = n;
                while (!string.IsNullOrEmpty(current.prerequisite))
                {
                    if (!seen.Add(current.id) || !nodes.TryGetValue(current.prerequisite, out var parent) || current.prerequisiteRank > parent.maxRank)
                        throw new ArgumentException("Missing, cyclic or impossible build prerequisite.");
                    current = parent;
                }
            }
        }
        public bool Validate(IReadOnlyDictionary<string, int> ranks, int level, int budget, out string reason)
        {
            reason = null;
            if (ranks == null || level < 1 || budget < 0) { reason = "Invalid allocation context."; return false; }
            long spent = 0;
            var groups = new HashSet<string>();
            foreach (var pair in ranks)
            {
                if (!nodes.TryGetValue(pair.Key, out var node) || pair.Value < 0 || pair.Value > node.maxRank)
                { reason = "Unknown talent or invalid rank."; return false; }
                spent += pair.Value;
                if (pair.Value == 0) continue;
                if (level < node.requiredLevel) { reason = "Required level not reached."; return false; }
                if (!string.IsNullOrEmpty(node.prerequisite) && (!ranks.TryGetValue(node.prerequisite, out var rank) || rank < node.prerequisiteRank))
                { reason = "Prerequisite ranks missing."; return false; }
                if (!string.IsNullOrEmpty(node.exclusiveGroup) && !groups.Add(node.exclusiveGroup))
                { reason = "Mutually exclusive talents selected."; return false; }
            }
            if (spent > budget) { reason = "Not enough talent points."; return false; }
            // Simulate reachable purchases. This rejects circular tier self-funding, including on respec/load.
            var reached = new HashSet<string>();
            var branchSpent = new Dictionary<string, long>();
            bool changed;
            do
            {
                changed = false;
                foreach (var pair in ranks)
                {
                    if (pair.Value == 0 || reached.Contains(pair.Key)) continue;
                    var node = nodes[pair.Key]; branchSpent.TryGetValue(node.branch, out var points);
                    if (points < node.branchPointsRequired || (!string.IsNullOrEmpty(node.prerequisite) && !reached.Contains(node.prerequisite))) continue;
                    reached.Add(pair.Key); branchSpent[node.branch] = points + pair.Value; changed = true;
                }
            } while (changed);
            foreach (var pair in ranks) if (pair.Value > 0 && !reached.Contains(pair.Key))
            { reason = "Branch investment requirement not met."; return false; }
            return true;
        }
    }
}
