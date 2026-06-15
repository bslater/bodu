// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullExchangeRateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the no-op behaviour of <see cref="NullExchangeRateCache" />.
/// </summary>
[TestClass]
public sealed class NullExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that the factory binds the supplied provider.
    /// </summary>
    [TestMethod]
    public void Create_WhenProviderSupplied_ShouldBindProvider()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");

        Assert.AreEqual("Yahoo", cache.Provider);
    }

    /// <summary>
    /// Verifies that the factory rejects a blank provider.
    /// </summary>
    [TestMethod]
    public void Create_WhenProviderIsBlank_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NullExchangeRateCache.Create("  ");
        });

        Assert.AreEqual("provider", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a read always returns an empty result, even after a store.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenAnythingStored_ShouldReturnEmpty()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");
        ExchangeRatePair pair = new("AUD", "USD");
        var now = DateTimeOffset.UtcNow;

        cache.Store(pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, TimeSpan.FromHours(24), now);

        Assert.AreEqual(0, cache.GetRates(pair, TimeSpan.FromHours(24), now).Count);
    }

    /// <summary>
    /// Verifies that coverage is never reported, even after a window is recorded.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenCoverageRecorded_ShouldReturnEmpty()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");
        ExchangeRatePair pair = new("AUD", "USD");
        var now = DateTimeOffset.UtcNow;

        cache.RecordCoverage(pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), TimeSpan.FromHours(24), now);

        Assert.IsTrue(cache.GetCoverage(pair, TimeSpan.FromHours(24), now).IsEmpty);
    }
}
