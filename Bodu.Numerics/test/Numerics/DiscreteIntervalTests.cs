// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

/// <summary>
/// Verifies the <see cref="DiscreteInterval{T}" /> type — the discrete integer-domain interval — covering the
/// successor/predecessor-aware emptiness and adjacency that distinguish it from the continuous <see cref="Interval{T}" />.
/// </summary>
[TestClass]
public partial class DiscreteIntervalTests
{
    /// <summary>
    /// Verifies that an open interval over consecutive integers is empty, because no integer lies strictly between the
    /// bounds — the P0.1 discrete-domain contradiction the continuous interval cannot express.
    /// </summary>
    [TestMethod]
    public void Open_WhenBoundsAreConsecutiveIntegers_ShouldBeEmpty()
    {
        Assert.IsTrue(DiscreteInterval<int>.Open(1, 2).IsEmpty);
        Assert.IsFalse(DiscreteInterval<int>.Open(1, 3).IsEmpty);
        Assert.AreEqual(DiscreteInterval<int>.Singleton(2), DiscreteInterval<int>.Open(1, 3));
    }

    /// <summary>
    /// Verifies that two successor-adjacent intervals union into a single contiguous run, because no integer separates
    /// them.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenSuccessorAdjacent_ShouldReturnSingleRun()
    {
        bool ok = DiscreteInterval<int>.Closed(1, 2).TryUnion(DiscreteInterval<int>.Closed(3, 4), out DiscreteInterval<int> run);

        Assert.IsTrue(ok);
        Assert.AreEqual(DiscreteInterval<int>.Closed(1, 4), run);
    }

    /// <summary>
    /// Verifies that intervals separated by at least one missing integer do not union into a single run.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenAnIntegerGapSeparatesThem_ShouldReturnFalse()
    {
        bool ok = DiscreteInterval<int>.Closed(1, 2).TryUnion(DiscreteInterval<int>.Closed(4, 5), out _);

        Assert.IsFalse(ok);
    }

    /// <summary>
    /// Verifies that the default value is the empty interval.
    /// </summary>
    [TestMethod]
    public void Default_WhenUninitialized_ShouldBeEmpty()
    {
        DiscreteInterval<int> value = default;

        Assert.IsTrue(value.IsEmpty);
        Assert.AreEqual(DiscreteInterval<int>.Empty, value);
    }
}
