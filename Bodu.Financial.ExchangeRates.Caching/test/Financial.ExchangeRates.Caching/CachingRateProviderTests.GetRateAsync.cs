// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.GetRateAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingRateProviderTests
{
    /// <summary>
    /// Verifies that the undated <see cref="CachingRateProvider.GetRateAsync(string, string, RateLookupOptions?, CancellationToken)" />
    /// resolves the most recent rate on or before the current UTC date, as the wrapped provider's undated lookup does,
    /// rather than requiring a rate on the current date itself.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task GetRateAsync_WhenUndatedAndLatestRateIsBeforeToday_ShouldReturnMostRecentRate()
    {
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 5), 0.50m), ("AUD", "USD", new DateOnly(2023, 1, 6), 0.51m));
        CachingRateProvider sut = CreateDecorator(inner);

        RateLookupResult result = await sut.GetRateAsync("AUD", "USD");

        Assert.AreEqual(new DateOnly(2023, 1, 6), result.Rate.Date);
        Assert.AreEqual(0.51m, result.Rate.Rate);
    }
}
