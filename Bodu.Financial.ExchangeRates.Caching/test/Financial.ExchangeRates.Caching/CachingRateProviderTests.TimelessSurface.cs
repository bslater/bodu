// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.TimelessSurface.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the timeless <see cref="IRateProvider" /> surface of <see cref="CachingRateProvider" />,
/// which resolves the current UTC date from the injected time provider under the configured default lookup options.
/// </summary>
public sealed partial class CachingRateProviderTests
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
        CountingDatedRateProvider inner = InnerWith();
        SeedCache(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), (today, 0.5m));
        CachingRateProvider sut = CreateDecorator(inner);

        decimal rate = ((IRateProvider)sut).GetRate("AUD", "USD");

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
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", today, 0.5m));
        CachingRateProvider sut = CreateDecorator(inner);

        decimal rate = ((IRateProvider)sut).GetRate("AUD", "USD");

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
        CachingRateProvider sut = CreateDecorator(InnerWith());

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = ((IRateProvider)sut).GetRate("AUD", "USD");
        });
    }
}
