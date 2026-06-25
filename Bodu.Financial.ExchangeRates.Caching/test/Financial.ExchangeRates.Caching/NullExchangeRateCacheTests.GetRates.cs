// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullExchangeRateCacheTests.GetRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class NullExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that a read always returns an empty result, even after a store.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenAnythingStored_ShouldReturnEmpty()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");
        ExchangeRatePair pair = new(CurrencyCode.AUD, CurrencyCode.USD);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        cache.Store(pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, TimeSpan.FromHours(24), now);

        Assert.IsEmpty(cache.GetRates(pair, TimeSpan.FromHours(24), now));
    }
}
