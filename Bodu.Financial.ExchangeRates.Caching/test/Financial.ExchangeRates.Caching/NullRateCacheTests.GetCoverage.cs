// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullRateCacheTests.GetCoverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class NullRateCacheTests
{
    /// <summary>
    /// Verifies that coverage is never reported, even after a window is recorded.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenCoverageRecorded_ShouldReturnEmpty()
    {
        IRateCache cache = NullRateCache.Create("Yahoo");
        CurrencyPair pair = new(CurrencyCode.AUD, CurrencyCode.USD);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        cache.RecordCoverage(pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), TimeSpan.FromHours(24), now);

        Assert.IsTrue(cache.GetCoverage(pair, TimeSpan.FromHours(24), now).IsEmpty);
    }
}
