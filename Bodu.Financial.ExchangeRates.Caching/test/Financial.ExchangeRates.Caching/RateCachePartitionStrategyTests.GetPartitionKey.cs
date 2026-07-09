// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCachePartitionStrategyTests.GetPartitionKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class RateCachePartitionStrategyTests
{
    /// <summary>
    /// Verifies that the single strategy keys every date to the empty string, so the whole pair lands in one file.
    /// </summary>
    [TestMethod]
    public void GetPartitionKey_ForSingle_ShouldReturnEmptyString()
    {
        Assert.AreEqual(string.Empty, RateCachePartitionStrategy.Single.GetPartitionKey(new DateOnly(2023, 6, 15)));
        Assert.AreEqual(string.Empty, RateCachePartitionStrategy.Single.GetPartitionKey(DateOnly.MaxValue));
    }

    /// <summary>
    /// Verifies that the yearly strategy keys a date by its four-digit year.
    /// </summary>
    [TestMethod]
    public void GetPartitionKey_ForYearly_ShouldKeyByYear() =>
        Assert.AreEqual("2023", RateCachePartitionStrategy.Yearly.GetPartitionKey(new DateOnly(2023, 6, 15)));

    /// <summary>
    /// Verifies that the daily strategy keys a date by its ISO calendar day.
    /// </summary>
    [TestMethod]
    public void GetPartitionKey_ForDaily_ShouldKeyByDay() =>
        Assert.AreEqual("2023-06-15", RateCachePartitionStrategy.Daily.GetPartitionKey(new DateOnly(2023, 6, 15)));
}
