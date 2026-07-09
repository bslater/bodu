// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullRateCacheTests.Store.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class NullRateCacheTests
{
    /// <summary>
    /// Verifies that <see cref="NullRateCache.Store" /> rejects a <see langword="null" /> rates collection,
    /// enforcing the same argument contract as every other backend rather than silently ignoring it.
    /// </summary>
    [TestMethod]
    public void Store_WhenRatesNull_ShouldThrowArgumentNullException()
    {
        IRateCache cache = NullRateCache.Create("Yahoo");
        CurrencyPair pair = new(CurrencyCode.AUD, CurrencyCode.USD);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            cache.Store(pair, null!, TimeSpan.FromHours(24), now);
        });

        Assert.AreEqual("rates", ex.ParamName);
    }
}
