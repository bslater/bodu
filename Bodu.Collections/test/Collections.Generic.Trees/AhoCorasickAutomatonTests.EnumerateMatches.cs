// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonTests.EnumerateMatches.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class AhoCorasickAutomatonTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> text throws <see cref="ArgumentNullException" /> naming <c>text</c>
    /// eagerly, before the sequence is iterated.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenTextIsNull_ShouldThrowForText()
    {
        var sut = CreateClassic();

        Assert.AreEqual(
            "text",
            Assert.ThrowsExactly<ArgumentNullException>(() => sut.EnumerateMatches(null!)).ParamName);
    }

    /// <summary>
    /// Verifies that an empty text yields no matches.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenTextIsEmpty_ShouldYieldNothing()
    {
        var sut = CreateClassic();

        Assert.AreEqual(0, sut.EnumerateMatches(string.Empty).Count());
    }

    /// <summary>
    /// Verifies that a text containing no pattern yields no matches.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenNoPatternOccurs_ShouldYieldNothing()
    {
        var sut = CreateClassic();

        Assert.AreEqual(0, sut.EnumerateMatches("xyzzy").Count());
    }

    /// <summary>
    /// Verifies that overlapping occurrences of the same pattern are all reported.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenOccurrencesOverlap_ShouldReportEveryOccurrence()
    {
        var sut = AhoCorasickAutomaton.Build(["aba"]);

        var matches = sut.EnumerateMatches("ababa").ToArray();

        CollectionAssert.AreEqual(
            new[] { new AhoCorasickMatch("aba", 0), new AhoCorasickMatch("aba", 2) },
            matches);
    }

    /// <summary>
    /// Verifies that nested patterns — one pattern a suffix of another — are all reported, in ascending end index
    /// then ascending pattern length order.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenPatternsNest_ShouldReportAllInDocumentedOrder()
    {
        var sut = AhoCorasickAutomaton.Build(["a", "aa"]);

        var matches = sut.EnumerateMatches("aaa").ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new AhoCorasickMatch("a", 0),
                new AhoCorasickMatch("a", 1),
                new AhoCorasickMatch("aa", 0),
                new AhoCorasickMatch("a", 2),
                new AhoCorasickMatch("aa", 1),
            },
            matches);
    }

    /// <summary>
    /// Verifies that occurrences flush against the start and end of the text are reported.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenPatternAtTextBoundaries_ShouldReportBoth()
    {
        var sut = AhoCorasickAutomaton.Build(["ab"]);

        var matches = sut.EnumerateMatches("abxab").ToArray();

        CollectionAssert.AreEqual(
            new[] { new AhoCorasickMatch("ab", 0), new AhoCorasickMatch("ab", 3) },
            matches);
    }

    /// <summary>
    /// Verifies that single-character patterns match at every occurrence of the character.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenPatternIsSingleCharacter_ShouldMatchEveryOccurrence()
    {
        var sut = AhoCorasickAutomaton.Build(["s"]);

        var matches = sut.EnumerateMatches("ushers").ToArray();

        CollectionAssert.AreEqual(
            new[] { new AhoCorasickMatch("s", 1), new AhoCorasickMatch("s", 5) },
            matches);
    }

    /// <summary>
    /// Verifies that repeated occurrences of multiple patterns keep the documented ordering across the whole
    /// enumeration: end indices never decrease, and matches sharing an end index appear in ascending pattern length.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenManyMatches_ShouldHonorEndThenLengthOrder()
    {
        var sut = AhoCorasickAutomaton.Build(["s", "he", "she", "hers", "ushers"]);

        var matches = sut.EnumerateMatches("ushersushers").ToArray();

        Assert.IsGreaterThan(0, matches.Length);
        for (int i = 1; i < matches.Length; i++)
        {
            var previous = matches[i - 1];
            var current = matches[i];
            bool ordered = previous.End < current.End
                || (previous.End == current.End && previous.Pattern.Length < current.Pattern.Length);

            Assert.IsTrue(ordered, $"Match {i} ({current.Pattern}@{current.Start}) breaks the (end, length) order.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="AhoCorasickMatch.End" /> is the exclusive end index — start plus pattern length.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenMatchReported_ShouldExposeExclusiveEnd()
    {
        var sut = CreateClassic();

        var match = sut.EnumerateMatches("ushers").First();

        Assert.AreEqual("he", match.Pattern);
        Assert.AreEqual(2, match.Start);
        Assert.AreEqual(4, match.End);
    }

    /// <summary>
    /// Verifies that matching is case-sensitive (ordinal): patterns do not match differently cased text.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenTextDiffersByCase_ShouldNotMatch()
    {
        var sut = AhoCorasickAutomaton.Build(["she"]);

        Assert.AreEqual(0, sut.EnumerateMatches("SHE").Count());
        Assert.AreEqual(1, sut.EnumerateMatches("she").Count());
    }
}
