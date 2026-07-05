// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeTests.Comparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeTests
{
    /// <summary>
    /// Verifies that a case-insensitive comparer governs endpoint ordering, containment, and stabbing — intervals
    /// added and queried with differing case behave identically.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCaseInsensitive_ShouldGovernContainmentAndQueries()
    {
        var sut = new IntervalTree<string>(StringComparer.OrdinalIgnoreCase);

        sut.Add("banana", "grape");

        Assert.IsTrue(sut.Contains("BANANA", "GRAPE"));
        CollectionAssert.AreEqual(new[] { ("banana", "grape") }, sut.QueryPoint("CHERRY").ToList());
        Assert.IsTrue(sut.IntersectsPoint("date"));
        Assert.IsFalse(sut.IntersectsPoint("apple"));
    }

    /// <summary>
    /// Verifies that comparer-equal intervals added with differing case are treated as duplicates of the stored
    /// interval, preserving the first stored endpoints.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCaseInsensitiveDuplicateAdded_ShouldRecordOccurrenceAndKeepStoredEndpoints()
    {
        var sut = new IntervalTree<string>(StringComparer.OrdinalIgnoreCase);

        sut.Add("banana", "grape");
        sut.Add("BANANA", "GRAPE");

        Assert.AreEqual(2, sut.Count);
        CollectionAssert.AreEqual(
            new[] { ("banana", "grape"), ("banana", "grape") }, sut.ToList());
    }

    /// <summary>
    /// Verifies that the endpoint-order guard applies the injected comparer — a pair that is reversed only under
    /// the custom comparer throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenGuardEvaluatesCustomOrdering_ShouldThrowForReversedPair()
    {
        var sut = new IntervalTree<string>(StringComparer.OrdinalIgnoreCase);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add("GRAPE", "banana");
        });

        Assert.AreEqual("low", ex.ParamName);
    }
}
