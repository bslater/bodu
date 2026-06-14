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
    /// Verifies that <see cref="NullExchangeRateCache.Instance" /> returns the same shared singleton.
    /// </summary>
    [TestMethod]
    public void Instance_WhenAccessedTwice_ShouldReturnSameSingleton() =>
        Assert.AreSame(NullExchangeRateCache.Instance, NullExchangeRateCache.Instance);

    /// <summary>
    /// Verifies that a read always returns an empty result, even after a store.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenAnythingStored_ShouldReturnEmpty()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Instance;
        ExchangeRatePair pair = new("AUD", "USD");
        var now = DateTimeOffset.UtcNow;

        cache.Store("Yahoo", pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, TimeSpan.FromHours(24), now);

        Assert.AreEqual(0, cache.GetRates("Yahoo", pair, TimeSpan.FromHours(24), now).Count);
    }
}
