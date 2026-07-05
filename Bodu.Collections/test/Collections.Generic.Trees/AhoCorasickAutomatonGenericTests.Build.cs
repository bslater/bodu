// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonGenericTests.Build.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class AhoCorasickAutomatonGenericTests
{
    /// <summary>
    /// Verifies that a duplicate pattern key throws <see cref="ArgumentException" /> naming <c>patterns</c>, unlike
    /// the deduplicating unkeyed automaton.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternKeyIsDuplicate_ShouldThrowForPatterns()
    {
        Assert.AreEqual(
            "patterns",
            Assert.ThrowsExactly<ArgumentException>(() => AhoCorasickAutomaton<int>.Build(
            [
                new KeyValuePair<string, int>("ab", 1),
                new KeyValuePair<string, int>("ab", 2),
            ])).ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> pattern collection throws <see cref="ArgumentNullException" /> naming
    /// <c>patterns</c>.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternsIsNull_ShouldThrowForPatterns()
    {
        Assert.AreEqual(
            "patterns",
            Assert.ThrowsExactly<ArgumentNullException>(() => AhoCorasickAutomaton<int>.Build(null!)).ParamName);
    }

    /// <summary>
    /// Verifies that an empty pattern collection throws <see cref="ArgumentException" /> naming <c>patterns</c>.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternsIsEmpty_ShouldThrowForPatterns()
    {
        Assert.AreEqual(
            "patterns",
            Assert.ThrowsExactly<ArgumentException>(() => AhoCorasickAutomaton<int>.Build([])).ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> or empty pattern key throws <see cref="ArgumentException" /> naming
    /// <c>patterns</c>.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternKeyIsNullOrEmpty_ShouldThrowForPatterns()
    {
        Assert.AreEqual(
            "patterns",
            Assert.ThrowsExactly<ArgumentException>(() => AhoCorasickAutomaton<int>.Build(
            [
                new KeyValuePair<string, int>(null!, 1),
            ])).ParamName);

        Assert.AreEqual(
            "patterns",
            Assert.ThrowsExactly<ArgumentException>(() => AhoCorasickAutomaton<int>.Build(
            [
                new KeyValuePair<string, int>(string.Empty, 1),
            ])).ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="AhoCorasickAutomaton{TValue}.Patterns" /> preserves supplied order and
    /// <see cref="AhoCorasickAutomaton{TValue}.ContainsPattern(string)" /> reports membership.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternsValid_ShouldExposePatternsAndMembership()
    {
        var sut = AhoCorasickAutomaton<string>.Build(
        [
            new KeyValuePair<string, string>("zz", "last"),
            new KeyValuePair<string, string>("aa", "first"),
        ]);

        CollectionAssert.AreEqual(new[] { "zz", "aa" }, sut.Patterns.ToArray());
        Assert.IsTrue(sut.ContainsPattern("zz"));
        Assert.IsFalse(sut.ContainsPattern("z"));
    }
}
