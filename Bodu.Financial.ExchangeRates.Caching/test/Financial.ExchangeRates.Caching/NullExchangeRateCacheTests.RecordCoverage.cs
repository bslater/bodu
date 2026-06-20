// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullExchangeRateCacheTests.RecordCoverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class NullExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that <see cref="NullExchangeRateCache.RecordCoverage" /> rejects an inverted window, enforcing the same
    /// argument contract as every other backend.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenStartAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");
        ExchangeRatePair pair = new(CurrencyCode.AUD, CurrencyCode.USD);

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            cache.RecordCoverage(pair, new DateOnly(2023, 1, 10), new DateOnly(2023, 1, 3), TimeSpan.FromHours(24), DateTimeOffset.UtcNow);
        });

        Assert.AreEqual("start", ex.ParamName);
    }
}
