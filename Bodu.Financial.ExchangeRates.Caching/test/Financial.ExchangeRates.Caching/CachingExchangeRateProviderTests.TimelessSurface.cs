// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProviderTests.TimelessSurface.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the timeless <see cref="IExchangeRateProvider" /> surface of <see cref="CachingExchangeRateProvider" />,
/// which resolves the current UTC date from the injected time provider under the configured default lookup options.
/// </summary>
public sealed partial class CachingExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that the timeless surface serves a fresh cached rate for the current UTC date without consulting the
    /// inner provider.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetRate_WhenTimelessAndCacheFreshForToday_ShouldReturnTodaysRate()
    {
        var today = DateOnly.FromDateTime(Now.UtcDateTime);
        CountingDatedExchangeRateProvider inner = InnerWith();
        SeedCache(new ExchangeRatePair("AUD", "USD"), (today, 0.5m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        decimal rate = ((IExchangeRateProvider)sut).GetRate("AUD", "USD");

        Assert.AreEqual(0.5m, rate);
        Assert.AreEqual(0, inner.TotalCallCount);
    }

    /// <summary>
    /// Verifies that the timeless surface delegates to the inner provider on a cache miss for the current UTC date.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenTimelessAndCacheMiss_ShouldDelegateToInner()
    {
        var today = DateOnly.FromDateTime(Now.UtcDateTime);
        CountingDatedExchangeRateProvider inner = InnerWith(("AUD", "USD", today, 0.5m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        decimal rate = ((IExchangeRateProvider)sut).GetRate("AUD", "USD");

        Assert.AreEqual(0.5m, rate);
        Assert.AreEqual(1, inner.TotalCallCount);
    }

    /// <summary>
    /// Verifies that the timeless surface throws <see cref="KeyNotFoundException" /> when no rate is available for the
    /// current UTC date.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenTimelessAndNoRate_ShouldThrowKeyNotFoundException()
    {
        CachingExchangeRateProvider sut = CreateDecorator(InnerWith());

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = ((IExchangeRateProvider)sut).GetRate("AUD", "USD");
        });
    }
}
