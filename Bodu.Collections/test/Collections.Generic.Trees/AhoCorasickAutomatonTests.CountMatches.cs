// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonTests.CountMatches.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class AhoCorasickAutomatonTests
{
    /// <summary>
    /// Verifies that the span-based <see cref="AhoCorasickAutomaton.CountMatches(ReadOnlySpan{char})" /> agrees with
    /// the number of matches yielded by the lazy enumeration for a variety of texts.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    [TestMethod]
    [DataRow("ushers")]
    [DataRow("shishershers")]
    [DataRow("hhhh")]
    [DataRow("xyzzy")]
    [DataRow("")]
    public void CountMatches_WhenComparedToEnumeration_ShouldAgree(string text)
    {
        var sut = CreateClassic();

        Assert.AreEqual(sut.EnumerateMatches(text).Count(), sut.CountMatches(text));
    }

    /// <summary>
    /// Verifies that overlapping and nested occurrences are all counted.
    /// </summary>
    [TestMethod]
    public void CountMatches_WhenMatchesOverlapAndNest_ShouldCountEveryOccurrence()
    {
        var sut = AhoCorasickAutomaton.Build(["a", "aa"]);

        Assert.AreEqual(5, sut.CountMatches("aaa"));
    }

    /// <summary>
    /// Verifies that an empty span counts zero matches.
    /// </summary>
    [TestMethod]
    public void CountMatches_WhenTextIsEmpty_ShouldReturnZero()
    {
        var sut = CreateClassic();

        Assert.AreEqual(0, sut.CountMatches(ReadOnlySpan<char>.Empty));
    }
}
