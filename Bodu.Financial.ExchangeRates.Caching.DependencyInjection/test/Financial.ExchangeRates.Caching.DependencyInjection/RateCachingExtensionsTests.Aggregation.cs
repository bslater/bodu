// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCachingExtensionsTests.Aggregation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Caching.DependencyInjection;

public sealed partial class RateCachingExtensionsTests
{
    /// <summary>
    /// Verifies that the registered aggregator resolves on both the dated and timeless surfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddAggregatedExchangeRateProvider_WhenRegistered_ShouldResolveBothSurfaces()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddAggregatedExchangeRateProvider(
                agg => agg
                    .AddCachedChild("RBA", _ => Fixed("RBA", 0.50m))
                    .AddCachedChild("ECB", _ => Fixed("ECB", 0.51m)),
                configureCache: o => o.CacheDirectory = _directory));

        IDatedExchangeRateProvider dated = provider.GetRequiredService<IDatedExchangeRateProvider>();
        IExchangeRateProvider timeless = provider.GetRequiredService<IExchangeRateProvider>();

        Assert.IsInstanceOfType<AggregatingExchangeRateProvider>(dated);
        Assert.AreSame<object>(dated, timeless);
    }

    /// <summary>
    /// Verifies that the aggregator serves via the default priority-fallback strategy, returning the first child.
    /// </summary>
    [TestMethod]
    public void AddAggregatedExchangeRateProvider_WhenResolved_ShouldServeViaPriorityFallback()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddAggregatedExchangeRateProvider(
                agg => agg
                    .AddCachedChild("RBA", _ => Fixed("RBA", 0.50m))
                    .AddCachedChild("ECB", _ => Fixed("ECB", 0.51m)),
                configureCache: o => o.CacheDirectory = _directory));

        IDatedExchangeRateProvider resolved = provider.GetRequiredService<IDatedExchangeRateProvider>();
        ExchangeRateLookupResult result = resolved.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        Assert.AreEqual("RBA", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that each child is registered as a keyed cached provider resolvable by name.
    /// </summary>
    [TestMethod]
    public void AddAggregatedExchangeRateProvider_WhenChildKeyed_ShouldResolveSpecificChildByName()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddAggregatedExchangeRateProvider(
                agg => agg
                    .AddCachedChild("RBA", _ => Fixed("RBA", 0.50m))
                    .AddCachedChild("ECB", _ => Fixed("ECB", 0.51m)),
                configureCache: o => o.CacheDirectory = _directory));

        IDatedExchangeRateProvider rba = provider.GetRequiredKeyedService<IDatedExchangeRateProvider>("RBA");

        Assert.IsInstanceOfType<CachingExchangeRateProvider>(rba);
        Assert.AreEqual(0.50m, rba.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact).Rate.Rate);
    }

    /// <summary>
    /// Verifies that a per-pair route configured through the builder is honoured, overriding the child order.
    /// </summary>
    [TestMethod]
    public void AddAggregatedExchangeRateProvider_WhenRouteConfigured_ShouldHonourRoute()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddAggregatedExchangeRateProvider(
                agg => agg
                    .AddCachedChild("RBA", _ => Fixed("RBA", 0.50m))
                    .AddCachedChild("ECB", _ => Fixed("ECB", 0.51m))
                    .MapPair(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), "ECB", "RBA"),
                configureCache: o => o.CacheDirectory = _directory));

        IDatedExchangeRateProvider resolved = provider.GetRequiredService<IDatedExchangeRateProvider>();
        ExchangeRateLookupResult result = resolved.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        Assert.AreEqual("ECB", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> configure callback is rejected.
    /// </summary>
    [TestMethod]
    public void AddAggregatedExchangeRateProvider_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();
        IFinancialServiceBuilder builder = services.AddBoduFinancial();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddAggregatedExchangeRateProvider(null!);
        });

        Assert.AreEqual("configure", ex.ParamName);
    }

    /// <summary>
    /// Builds a fixed provider resolving a single AUD/USD observation tagged with a provider name.
    /// </summary>
    /// <param name="provider">The provider tag.</param>
    /// <param name="rate">The rate.</param>
    /// <returns>A new fixed provider.</returns>
    private static IDatedExchangeRateProvider Fixed(string provider, decimal rate) =>
        new FixedDatedExchangeRateProvider(new[] { new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2023, 1, 3), rate, provider) });
}
