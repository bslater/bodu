// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonGenericTests.EnumerateMatches.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class AhoCorasickAutomatonGenericTests
{
    /// <summary>
    /// Verifies that every reported match carries the value associated with its pattern, including for nested and
    /// overlapping occurrences.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenPatternsNest_ShouldSurfaceEachPatternsValue()
    {
        var sut = AhoCorasickAutomaton<string>.Build(
        [
            new KeyValuePair<string, string>("a", "short"),
            new KeyValuePair<string, string>("aa", "long"),
        ]);

        var matches = sut.EnumerateMatches("aa").ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new AhoCorasickMatch<string>("a", 0, "short"),
                new AhoCorasickMatch<string>("a", 1, "short"),
                new AhoCorasickMatch<string>("aa", 0, "long"),
            },
            matches);
    }

    /// <summary>
    /// Verifies that reference-typed <see langword="null" /> values are surfaced on matches unchanged.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenValueIsNull_ShouldSurfaceNull()
    {
        var sut = AhoCorasickAutomaton<string?>.Build(
        [
            new KeyValuePair<string, string?>("ab", null),
        ]);

        var match = sut.EnumerateMatches("xabx").Single();

        Assert.AreEqual("ab", match.Pattern);
        Assert.AreEqual(1, match.Start);
        Assert.AreEqual(3, match.End);
        Assert.IsNull(match.Value);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> text throws <see cref="ArgumentNullException" /> naming <c>text</c>
    /// and that the span conveniences agree with the lazy enumeration.
    /// </summary>
    [TestMethod]
    public void EnumerateMatches_WhenTextIsNullOrScanned_ShouldGuardAndAgreeWithSpanForms()
    {
        var sut = AhoCorasickAutomaton<int>.Build(
        [
            new KeyValuePair<string, int>("he", 1),
            new KeyValuePair<string, int>("she", 2),
        ]);

        Assert.AreEqual(
            "text",
            Assert.ThrowsExactly<ArgumentNullException>(() => sut.EnumerateMatches(null!)).ParamName);

        Assert.AreEqual(sut.EnumerateMatches("ushers").Count(), sut.CountMatches("ushers"));
        Assert.AreEqual(sut.EnumerateMatches("ushers").Any(), sut.HasMatch("ushers"));
        Assert.IsFalse(sut.HasMatch("xyz"));
    }
}
