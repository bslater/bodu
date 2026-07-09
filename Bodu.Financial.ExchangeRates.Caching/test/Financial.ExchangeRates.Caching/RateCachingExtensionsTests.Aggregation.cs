// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCachingExtensionsTests.Aggregation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class RateCachingExtensionsTests
{
    /// <summary>
    /// Verifies that the registered aggregator resolves on both the dated and timeless surfaces as the same instance.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddAggregatedRateProvider_WhenRegistered_ShouldResolveBothSurfaces()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddAggregatedRateProvider(
                agg => agg
                    .AddCachedChild("RBA", _ => Fixed("RBA", 0.50m))
                    .AddCachedChild("ECB", _ => Fixed("ECB", 0.51m)),
                configureCache: o => o.CacheDirectory = _directory));

        IDatedRateProvider dated = provider.GetRequiredService<IDatedRateProvider>();
        IRateProvider timeless = provider.GetRequiredService<IRateProvider>();

        Assert.IsInstanceOfType<AggregatingRateProvider>(dated);
        Assert.AreSame<object>(dated, timeless);
    }

    /// <summary>
    /// Verifies that the aggregator serves via the default priority-fallback strategy, returning the first child.
    /// </summary>
    [TestMethod]
    public void AddAggregatedRateProvider_WhenResolved_ShouldServeViaPriorityFallback()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddAggregatedRateProvider(
                agg => agg
                    .AddCachedChild("RBA", _ => Fixed("RBA", 0.50m))
                    .AddCachedChild("ECB", _ => Fixed("ECB", 0.51m)),
                configureCache: o => o.CacheDirectory = _directory));

        IDatedRateProvider resolved = provider.GetRequiredService<IDatedRateProvider>();
        RateLookupResult result = resolved.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual("RBA", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that each child is registered as a keyed cached provider resolvable by name.
    /// </summary>
    [TestMethod]
    public void AddAggregatedRateProvider_WhenChildKeyed_ShouldResolveSpecificChildByName()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddAggregatedRateProvider(
                agg => agg
                    .AddCachedChild("RBA", _ => Fixed("RBA", 0.50m))
                    .AddCachedChild("ECB", _ => Fixed("ECB", 0.51m)),
                configureCache: o => o.CacheDirectory = _directory));

        IDatedRateProvider rba = provider.GetRequiredKeyedService<IDatedRateProvider>("RBA");

        Assert.IsInstanceOfType<CachingRateProvider>(rba);
        Assert.AreEqual(0.50m, rba.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact).Rate.Rate);
    }

    /// <summary>
    /// Verifies that a per-child <c>cacheFactory</c> chooses that child's storage, writing a JSON file for the child
    /// that supplied the factory.
    /// </summary>
    [TestMethod]
    public void AddAggregatedRateProvider_WhenChildCacheFactoryProvided_ShouldUseChosenCache()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddAggregatedRateProvider(
                agg => agg
                    .AddCachedChild("RBA", _ => Fixed("RBA", 0.50m), (_, name) => new JsonFileRateCache(
                        new FileRateCacheOptions { Provider = name, CacheDirectory = _directory }))
                    .AddCachedChild("ECB", _ => Fixed("ECB", 0.51m)),
                configureCache: o => o.CacheDirectory = _directory));

        IDatedRateProvider rba = provider.GetRequiredKeyedService<IDatedRateProvider>("RBA");
        _ = rba.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.IsTrue(File.Exists(Path.Combine(_directory, "RBA", "AUDUSD.json")), "the RBA child uses its supplied JSON cache");
    }

    /// <summary>
    /// Verifies that a per-pair route configured through the builder is honoured, overriding the child order.
    /// </summary>
    [TestMethod]
    public void AddAggregatedRateProvider_WhenRouteConfigured_ShouldHonourRoute()
    {
        ServiceProvider provider = BuildProvider(builder =>
            builder.AddAggregatedRateProvider(
                agg => agg
                    .AddCachedChild("RBA", _ => Fixed("RBA", 0.50m))
                    .AddCachedChild("ECB", _ => Fixed("ECB", 0.51m))
                    .MapPair(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), "ECB", "RBA"),
                configureCache: o => o.CacheDirectory = _directory));

        IDatedRateProvider resolved = provider.GetRequiredService<IDatedRateProvider>();
        RateLookupResult result = resolved.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual("ECB", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> configure callback is rejected.
    /// </summary>
    [TestMethod]
    public void AddAggregatedRateProvider_WhenConfigureIsNull_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();
        IFinancialServiceBuilder builder = services.AddFinancialService();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddAggregatedRateProvider(null!);
        });

        Assert.AreEqual("configure", ex.ParamName);
    }

    /// <summary>
    /// Builds a fixed provider resolving a single AUD/USD observation tagged with a provider name.
    /// </summary>
    /// <param name="provider">The provider tag.</param>
    /// <param name="rate">The rate.</param>
    /// <returns>A new fixed provider.</returns>
    private static IDatedRateProvider Fixed(string provider, decimal rate) =>
        new FixedDatedRateProvider(new[] { new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2023, 1, 3), rate, provider) });
}
