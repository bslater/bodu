// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MovingSumTests.Overflow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class MovingSumTests
{
    /// <summary>
    /// Verifies that a fixed-width integer sum that would overflow during warm-up throws
    /// <see cref="OverflowException" /> and leaves the window state unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenWarmUpSumOverflowsInt_ShouldThrowOverflowExceptionAndPreserveState()
    {
        var window = new MovingSum<int>(2);
        window.Add(int.MaxValue);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            window.Add(1);
        });

        Assert.AreEqual(1, window.Count);
        Assert.AreEqual(int.MaxValue, window.Sum);
    }

    /// <summary>
    /// Verifies that a fixed-width integer sum that would overflow during a steady-state eviction throws
    /// <see cref="OverflowException" />, leaves the window state unchanged, and the window keeps working afterwards.
    /// </summary>
    [TestMethod]
    public void Add_WhenSteadyStateSumOverflowsInt_ShouldThrowOverflowExceptionAndPreserveState()
    {
        var window = new MovingSum<int>(2);
        window.Add(-5);
        window.Add(int.MaxValue - 1);

        Assert.AreEqual(int.MaxValue - 6, window.Sum);

        // Evicting −5 raises the sum to int.MaxValue − 1; adding 10 then overflows in the checked update.
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            window.Add(10);
        });

        Assert.AreEqual(2, window.Count);
        Assert.AreEqual(int.MaxValue - 6, window.Sum);

        // The rejected sample was never admitted, so a subsequent in-range add slides the original window:
        // {−5, int.MaxValue − 1} → {int.MaxValue − 1, −int.MaxValue}, whose sum is −1.
        window.Add(-int.MaxValue);
        Assert.AreEqual(-1, window.Sum);
    }

    /// <summary>
    /// Verifies that negative-direction integer overflow also throws <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenSumOverflowsIntNegatively_ShouldThrowOverflowException()
    {
        var window = new MovingSum<int>(2);
        window.Add(int.MinValue);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            window.Add(-1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="long" /> sums stay correct next to the type's edge and throw
    /// <see cref="OverflowException" /> past it.
    /// </summary>
    [TestMethod]
    public void Add_WhenLongSumApproachesEdge_ShouldStayExactThenThrowPastIt()
    {
        var window = new MovingSum<long>(2);
        window.Add(5L);
        window.Add(long.MaxValue - 10L);

        Assert.AreEqual(long.MaxValue - 5L, window.Sum);

        // Evicting the oldest sample (5) leaves long.MaxValue − 10 in the window; adding 20 overflows.
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            window.Add(20L);
        });

        Assert.AreEqual(long.MaxValue - 5L, window.Sum);
    }

    /// <summary>
    /// Verifies that a <see cref="decimal" /> sum overflow surfaces as <see cref="OverflowException" /> through
    /// decimal's own checked arithmetic.
    /// </summary>
    [TestMethod]
    public void Add_WhenDecimalSumOverflows_ShouldThrowOverflowException()
    {
        var window = new MovingSum<decimal>(2);
        window.Add(decimal.MaxValue);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            window.Add(1m);
        });
    }

    /// <summary>
    /// Verifies that a <see cref="BigInteger" /> window keeps its sum exact at any magnitude, while reading
    /// <see cref="MovingSum{T}.Mean" /> for a sum beyond the range of <see cref="double" /> throws
    /// <see cref="OverflowException" /> instead of returning infinity.
    /// </summary>
    [TestMethod]
    public void Mean_WhenBigIntegerSumExceedsDoubleRange_ShouldThrowOverflowException()
    {
        var huge = BigInteger.Pow(10, 400);
        var window = new MovingSum<BigInteger>(2);
        window.Add(huge);
        window.Add(huge);

        Assert.AreEqual(huge * 2, window.Sum);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = window.Mean;
        });
    }
}
