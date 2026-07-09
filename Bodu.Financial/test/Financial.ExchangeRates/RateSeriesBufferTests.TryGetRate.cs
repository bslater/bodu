// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesBufferTests.TryGetRate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateSeriesBufferTests
{
    /// <summary>
    /// Verifies that <see cref="RateSeriesBuffer.TryGetRate" /> returns the recorded rate on exact match.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDayExists_ShouldReturnTrueAndRate()
    {
        RateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m));

        Assert.IsTrue(buffer.TryGetRate(1000, out decimal rate0));
        Assert.AreEqual(1.4m, rate0);
        Assert.IsTrue(buffer.TryGetRate(1010, out decimal rate1));
        Assert.AreEqual(1.5m, rate1);
    }

    /// <summary>
    /// Verifies that <see cref="RateSeriesBuffer.TryGetRate" /> returns <see langword="false" /> and a
    /// default rate for a missing day number — the buffer's lookup is intentionally exact-only.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDayMissing_ShouldReturnFalseAndDefault()
    {
        RateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m));

        Assert.IsFalse(buffer.TryGetRate(1005, out decimal rate));
        Assert.AreEqual(0m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="RateSeriesBuffer.TryGetRate" /> on an empty buffer returns
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenBufferEmpty_ShouldReturnFalse()
    {
        RateSeriesBuffer buffer = NewBuffer();

        Assert.IsFalse(buffer.TryGetRate(1000, out _));
    }

    /// <summary>
    /// Verifies that lookups around the boundary day numbers (one less and one more than every entry) all return
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenBoundaryNeighboursQueried_ShouldReturnFalse()
    {
        RateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m), (1020, 1.6m));

        Assert.IsFalse(buffer.TryGetRate(999, out _));
        Assert.IsFalse(buffer.TryGetRate(1001, out _));
        Assert.IsFalse(buffer.TryGetRate(1009, out _));
        Assert.IsFalse(buffer.TryGetRate(1011, out _));
        Assert.IsFalse(buffer.TryGetRate(1019, out _));
        Assert.IsFalse(buffer.TryGetRate(1021, out _));
    }
}
