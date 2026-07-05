// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonTests.Build.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class AhoCorasickAutomatonTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> pattern collection throws <see cref="ArgumentNullException" /> naming
    /// <c>patterns</c>.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternsIsNull_ShouldThrowForPatterns()
    {
        Assert.AreEqual(
            "patterns",
            Assert.ThrowsExactly<ArgumentNullException>(() => AhoCorasickAutomaton.Build(null!)).ParamName);
    }

    /// <summary>
    /// Verifies that an empty pattern collection throws <see cref="ArgumentException" /> naming <c>patterns</c>.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternsIsEmpty_ShouldThrowForPatterns()
    {
        Assert.AreEqual(
            "patterns",
            Assert.ThrowsExactly<ArgumentException>(() => AhoCorasickAutomaton.Build([])).ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> pattern inside the collection throws <see cref="ArgumentException" />
    /// naming <c>patterns</c>.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternIsNull_ShouldThrowForPatterns()
    {
        Assert.AreEqual(
            "patterns",
            Assert.ThrowsExactly<ArgumentException>(() => AhoCorasickAutomaton.Build(["ok", null!])).ParamName);
    }

    /// <summary>
    /// Verifies that an empty pattern inside the collection throws <see cref="ArgumentException" /> naming
    /// <c>patterns</c>, because an empty pattern is meaningless for matching.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternIsEmpty_ShouldThrowForPatterns()
    {
        Assert.AreEqual(
            "patterns",
            Assert.ThrowsExactly<ArgumentException>(() => AhoCorasickAutomaton.Build(["ok", string.Empty])).ParamName);
    }

    /// <summary>
    /// Verifies that repeated patterns are deduplicated: <see cref="AhoCorasickAutomaton.Patterns" /> reports each
    /// distinct pattern once and matches are not double-reported.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternsContainDuplicates_ShouldDeduplicate()
    {
        var sut = AhoCorasickAutomaton.Build(["ab", "cd", "ab", "ab"]);

        Assert.AreEqual(2, sut.Patterns.Count);
        Assert.AreEqual(1, sut.EnumerateMatches("ab").Count());
    }

    /// <summary>
    /// Verifies that <see cref="AhoCorasickAutomaton.Patterns" /> preserves first-seen order.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternsValid_ShouldExposePatternsInFirstSeenOrder()
    {
        var sut = AhoCorasickAutomaton.Build(["zz", "aa", "mm", "aa"]);

        CollectionAssert.AreEqual(new[] { "zz", "aa", "mm" }, sut.Patterns.ToArray());
    }

    /// <summary>
    /// Verifies that a collection consisting only of duplicates of one pattern still builds a usable automaton.
    /// </summary>
    [TestMethod]
    public void Build_WhenAllPatternsAreDuplicates_ShouldBuildSinglePatternAutomaton()
    {
        var sut = AhoCorasickAutomaton.Build(["x", "x", "x"]);

        Assert.AreEqual(1, sut.Patterns.Count);
        Assert.IsTrue(sut.HasMatch("axb"));
    }
}
