// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonTests.HasMatch.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class AhoCorasickAutomatonTests
{
    /// <summary>
    /// Verifies that the span-based <see cref="AhoCorasickAutomaton.HasMatch(ReadOnlySpan{char})" /> agrees with the
    /// lazy enumeration's emptiness for a variety of texts.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    [TestMethod]
    [DataRow("ushers")]
    [DataRow("his")]
    [DataRow("hhhh")]
    [DataRow("xyzzy")]
    [DataRow("")]
    public void HasMatch_WhenComparedToEnumeration_ShouldAgree(string text)
    {
        var sut = CreateClassic();

        Assert.AreEqual(sut.EnumerateMatches(text).Any(), sut.HasMatch(text));
    }

    /// <summary>
    /// Verifies that a text containing a pattern reports a match and one that does not reports none.
    /// </summary>
    [TestMethod]
    public void HasMatch_WhenPatternPresentOrAbsent_ShouldReportAccordingly()
    {
        var sut = CreateClassic();

        Assert.IsTrue(sut.HasMatch("ushers"));
        Assert.IsFalse(sut.HasMatch("uvwxyz"));
    }

    /// <summary>
    /// Verifies that an empty span reports no match.
    /// </summary>
    [TestMethod]
    public void HasMatch_WhenTextIsEmpty_ShouldReturnFalse()
    {
        var sut = CreateClassic();

        Assert.IsFalse(sut.HasMatch(ReadOnlySpan<char>.Empty));
    }
}
