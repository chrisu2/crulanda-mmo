using System;
using System.Collections.Generic;
using NUnit.Framework;
using Crulanda.Encounter;
namespace Crulanda.Tests
{
    public class BuildTreeTests
    {
        static BuildTree Tree() { return new BuildTree(new[] {
            new BuildNode { id="guard", branch="tank", maxRank=3 },
            new BuildNode { id="bulwark", branch="tank", prerequisite="guard", prerequisiteRank=2, branchPointsRequired=2, requiredLevel=4, exclusiveGroup="stance" },
            new BuildNode { id="pressure", branch="dps", maxRank=3 },
            new BuildNode { id="assault", branch="dps", prerequisite="pressure", exclusiveGroup="stance" }
        }); }
        [Test] public void Hybrid_investment_is_allowed_within_budget()
        {
            var ranks = new Dictionary<string,int> { {"guard",2}, {"pressure",1}, {"bulwark",1} };
            Assert.IsTrue(Tree().Validate(ranks,4,4,out _));
            Assert.IsFalse(Tree().Validate(ranks,4,3,out _));
            Assert.IsFalse(Tree().Validate(ranks,3,4,out _));
        }
        [Test] public void Respec_cannot_leave_orphaned_or_exclusive_talents()
        {
            Assert.IsFalse(Tree().Validate(new Dictionary<string,int>{{"bulwark",1}},10,10,out _));
            Assert.IsFalse(Tree().Validate(new Dictionary<string,int>{{"guard",2},{"pressure",1},{"bulwark",1},{"assault",1}},10,10,out _));
        }
        [Test] public void Unknown_or_excess_ranks_are_rejected()
        {
            Assert.IsFalse(Tree().Validate(new Dictionary<string,int>{{"guard",4}},10,10,out _));
            Assert.IsFalse(Tree().Validate(new Dictionary<string,int>{{"foreign",1}},10,10,out _));
        }
        [Test] public void Cycles_and_unresolved_prerequisites_are_rejected()
        {
            Assert.Throws<ArgumentException>(()=>new BuildTree(new[]{new BuildNode{id="a",branch="tank",prerequisite="a"}}));
            Assert.Throws<ArgumentException>(()=>new BuildTree(new[]{new BuildNode{id="a",branch="tank",prerequisite="missing"}}));
        }
        [Test] public void Tier_nodes_cannot_fund_each_other()
        {
            var tree = new BuildTree(new[]{new BuildNode{id="a",branch="tank",branchPointsRequired=1},new BuildNode{id="b",branch="tank",branchPointsRequired=1}});
            Assert.IsFalse(tree.Validate(new Dictionary<string,int>{{"a",1},{"b",1}},10,10,out _));
        }
    }
}
