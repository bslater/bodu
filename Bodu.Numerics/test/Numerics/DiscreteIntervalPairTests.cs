// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalPairTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

/// <summary>
/// Verifies the <see cref="DiscreteIntervalPair{T}" /> result type returned by the binary discrete-interval set
/// operations.
/// </summary>
[TestClass]
public sealed class DiscreteIntervalPairTests
{
    /// <summary>
    /// Verifies that the empty result reports zero runs and the empty-set glyph.
    /// </summary>
    [TestMethod]
    public void Empty_WhenInspected_ShouldReportNoRuns()
    {
        DiscreteIntervalPair<int> empty = DiscreteIntervalPair<int>.Empty;

        Assert.AreEqual(0, empty.Count);
        Assert.IsTrue(empty.IsEmpty);
        Assert.AreEqual("∅", empty.ToString());
    }

    /// <summary>
    /// Verifies that a two-run result exposes its runs through <see cref="DiscreteIntervalPair{T}.First" />,
    /// <see cref="DiscreteIntervalPair{T}.Second" />, the indexer, and enumeration, ordered lowest-first.
    /// </summary>
    [TestMethod]
    public void TwoRunResult_WhenInspected_ShouldExposeOrderedRuns()
    {
        DiscreteIntervalPair<int> pair = DiscreteInterval<int>.Closed(0, 10).Difference(DiscreteInterval<int>.Closed(3, 5));

        Assert.AreEqual(2, pair.Count);
        Assert.IsFalse(pair.IsEmpty);
        Assert.AreEqual(DiscreteInterval<int>.Closed(0, 2), pair.First);
        Assert.AreEqual(DiscreteInterval<int>.Closed(6, 10), pair.Second);
        Assert.AreEqual(pair.First, pair[0]);
        Assert.AreEqual(pair.Second, pair[1]);

        var enumerated = new List<DiscreteInterval<int>>();
        foreach (DiscreteInterval<int> run in pair)
            enumerated.Add(run);

        CollectionAssert.AreEqual(new[] { pair.First, pair.Second }, enumerated);
        Assert.AreEqual("[0, 2] ∪ [6, 10]", pair.ToString());
    }

    /// <summary>
    /// Verifies that indexing beyond the available runs throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        DiscreteIntervalPair<int> single = DiscreteInterval<int>.Closed(0, 10).Difference(DiscreteInterval<int>.Closed(8, 20));

        Assert.AreEqual(1, single.Count);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = single[1];
        });
    }

    /// <summary>
    /// Verifies that <see cref="DiscreteIntervalPair{T}.ToIntervalSet" /> lifts a two-run result into the equivalent
    /// continuous <see cref="IntervalSet{T}" /> covering the same integer runs.
    /// </summary>
    [TestMethod]
    public void ToIntervalSet_WhenTwoRuns_ShouldYieldEquivalentSet()
    {
        DiscreteIntervalPair<int> pair = DiscreteInterval<int>.Closed(0, 10).Difference(DiscreteInterval<int>.Closed(3, 5));

        IntervalSet<int> set = pair.ToIntervalSet();

        Assert.AreEqual(IntervalSet<int>.Of(Interval<int>.Closed(0, 2), Interval<int>.Closed(6, 10)), set);
    }

    /// <summary>
    /// Verifies that <see cref="DiscreteIntervalPair{T}.ToIntervalSet" /> on the empty result yields the empty set.
    /// </summary>
    [TestMethod]
    public void ToIntervalSet_WhenEmpty_ShouldYieldEmptySet()
    {
        Assert.IsTrue(DiscreteIntervalPair<int>.Empty.ToIntervalSet().IsEmpty);
    }
}
