// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="RangeSet{T}" />.
/// </summary>
[TestClass]
public partial class RangeSetTests
{

    /// <summary>
    /// Verifies the happy-path smoke check: adding a single range exposes it via Count, the indexer, and
    /// the enumerator.
    /// </summary>
    [TestMethod]
    public void Add_WhenSingleRangeAdded_ShouldExposeRange()
    {
        var sut = new RangeSet<int>();

        sut.Add(0, 10);

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(new Range<int>(0, 10), sut[0]);
    }

    /// <summary>
    /// Asserts that the contents of <paramref name="set" /> match the specified expected ranges.
    /// </summary>
    /// <param name="set">The set under test.</param>
    /// <param name="expected">The expected ordered list of half-open ranges.</param>
    private static void AssertContents(RangeSet<int> set, params (int Start, int End)[] expected) => CollectionAssert.AreEqual(expected, Snapshot(set));

    /// <summary>
    /// Creates a <see cref="RangeSet{T}" /> seeded with the specified <c>(start, end)</c> tuples.
    /// </summary>
    /// <param name="ranges">The half-open ranges to seed, in order.</param>
    /// <returns>The populated range set.</returns>
    private static RangeSet<int> CreateSet(params (int Start, int End)[] ranges)
    {
        var set = new RangeSet<int>();
        foreach ((int Start, int End) range in ranges)
            set.Add(range.Start, range.End);

        return set;
    }

    /// <summary>
    /// Materialises the contents of <paramref name="set" /> as a sequence of <c>(start, end)</c> tuples in
    /// ascending insertion order.
    /// </summary>
    /// <param name="set">The set to snapshot.</param>
    /// <returns>An array of endpoint pairs.</returns>
    private static (int Start, int End)[] Snapshot(RangeSet<int> set)
    {
        (int Start, int End)[] result = new (int, int)[set.Count];
        for (var i = 0; i < set.Count; i++)
            result[i] = (set[i].StartInclusive, set[i].EndExclusive);

        return result;
    }

    /// <summary>
    /// A reverse-order comparer over <see cref="int" /> used to verify that a non-default comparer is honoured
    /// during validation. This comparer treats values that are numerically larger as logically smaller.
    /// </summary>
    private sealed class ReverseIntComparer
        : IComparer<int>
    {

        /// <inheritdoc />
        public int Compare(int x, int y) => y.CompareTo(x);

    }

}
