// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCachePartitionStrategyTests.GetPartitionRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class ExchangeRateCachePartitionStrategyTests
{
    /// <summary>
    /// Verifies that the single strategy spans the whole representable date range, so one file holds every date.
    /// </summary>
    [TestMethod]
    public void GetPartitionRange_ForSingle_ShouldSpanFullRange()
    {
        (DateOnly start, DateOnly end) = ExchangeRateCachePartitionStrategy.Single.GetPartitionRange(new DateOnly(2023, 6, 15));

        Assert.AreEqual(DateOnly.MinValue, start);
        Assert.AreEqual(DateOnly.MaxValue, end);
    }

    /// <summary>
    /// Verifies that the yearly strategy spans the containing calendar year.
    /// </summary>
    [TestMethod]
    public void GetPartitionRange_ForYearly_ShouldSpanCalendarYear()
    {
        (DateOnly start, DateOnly end) = ExchangeRateCachePartitionStrategy.Yearly.GetPartitionRange(new DateOnly(2023, 6, 15));

        Assert.AreEqual(new DateOnly(2023, 1, 1), start);
        Assert.AreEqual(new DateOnly(2023, 12, 31), end);
    }

    /// <summary>
    /// Verifies that the monthly strategy spans the containing calendar month, with the last day reflecting month length.
    /// </summary>
    [TestMethod]
    public void GetPartitionRange_ForMonthly_ShouldSpanCalendarMonth()
    {
        (DateOnly start, DateOnly end) = ExchangeRateCachePartitionStrategy.Monthly.GetPartitionRange(new DateOnly(2023, 2, 15));

        Assert.AreEqual(new DateOnly(2023, 2, 1), start);
        Assert.AreEqual(new DateOnly(2023, 2, 28), end);
    }

    /// <summary>
    /// Verifies that the monthly strategy spans a leap February through its 29th day.
    /// </summary>
    [TestMethod]
    public void GetPartitionRange_ForMonthly_WhenLeapFebruary_ShouldEndOnTwentyNinth()
    {
        (DateOnly start, DateOnly end) = ExchangeRateCachePartitionStrategy.Monthly.GetPartitionRange(new DateOnly(2024, 2, 10));

        Assert.AreEqual(new DateOnly(2024, 2, 1), start);
        Assert.AreEqual(new DateOnly(2024, 2, 29), end);
    }

    /// <summary>
    /// Verifies that the daily strategy spans only the single day it is asked about.
    /// </summary>
    [TestMethod]
    public void GetPartitionRange_ForDaily_ShouldSpanSingleDay()
    {
        var date = new DateOnly(2023, 6, 15);
        (DateOnly start, DateOnly end) = ExchangeRateCachePartitionStrategy.Daily.GetPartitionRange(date);

        Assert.AreEqual(date, start);
        Assert.AreEqual(date, end);
    }
}
