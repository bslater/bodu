// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalPairTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

/// <summary>
/// Verifies the <see cref="IntervalPair{T}" /> result type returned by the binary interval set operations.
/// </summary>
[TestClass]
public sealed class IntervalPairTests
{
    /// <summary>
    /// Verifies that the empty result reports zero pieces and the empty-set glyph.
    /// </summary>
    [TestMethod]
    public void Empty_WhenInspected_ShouldReportNoPieces()
    {
        IntervalPair<int> empty = IntervalPair<int>.Empty;

        Assert.AreEqual(0, empty.Count);
        Assert.IsTrue(empty.IsEmpty);
        Assert.AreEqual("∅", empty.ToString());
    }

    /// <summary>
    /// Verifies that a two-piece result exposes its pieces through <see cref="IntervalPair{T}.First" />,
    /// <see cref="IntervalPair{T}.Second" />, the indexer, and enumeration, ordered lowest-first.
    /// </summary>
    [TestMethod]
    public void TwoPieceResult_WhenInspected_ShouldExposeOrderedPieces()
    {
        IntervalPair<int> pair = Interval<int>.Closed(0, 10).Difference(Interval<int>.Closed(3, 5));

        Assert.AreEqual(2, pair.Count);
        Assert.IsFalse(pair.IsEmpty);
        Assert.AreEqual(Interval<int>.ClosedOpen(0, 3), pair.First);
        Assert.AreEqual(Interval<int>.OpenClosed(5, 10), pair.Second);
        Assert.AreEqual(pair.First, pair[0]);
        Assert.AreEqual(pair.Second, pair[1]);

        var enumerated = new List<Interval<int>>();
        foreach (Interval<int> piece in pair)
            enumerated.Add(piece);

        CollectionAssert.AreEqual(new[] { pair.First, pair.Second }, enumerated);
        Assert.AreEqual("[0, 3) ∪ (5, 10]", pair.ToString());
    }

    /// <summary>
    /// Verifies that indexing beyond the available pieces throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        IntervalPair<int> single = Interval<int>.Closed(0, 10).Difference(Interval<int>.Closed(8, 20));

        Assert.AreEqual(1, single.Count);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = single[1];
        });
    }
}
