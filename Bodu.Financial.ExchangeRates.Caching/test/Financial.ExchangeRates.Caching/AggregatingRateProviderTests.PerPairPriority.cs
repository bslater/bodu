// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingRateProviderTests.PerPairPriority.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class AggregatingRateProviderTests
{
    /// <summary>
    /// Verifies that a per-pair route consults its providers in route order, overriding the child order.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenPairRouted_ShouldUseRouteOrderNotChildOrder()
    {
        NamedDatedRateProvider rba = Named("RBA", ("AUD", "USD", D1, 0.50m));
        NamedDatedRateProvider ecb = Named("ECB", ("AUD", "USD", D1, 0.51m));
        RateAggregationOptions options = new();
        options.Routes[new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD)] = new CurrencyPairRoute(new[] { "ECB", "RBA" });
        AggregatingRateProvider agg = new(new[] { rba, ecb }, options);

        agg.TryGetRate("AUD", "USD", D1, RateLookupOptions.Exact, out RateLookupResult result);

        Assert.AreEqual("ECB", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that different pairs honour their own route order — AUD/USD via [RBA, ECB] and USD/GBP via [ECB, RBA].
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDifferentPairsRoutedDifferently_ShouldHonourEachRoute()
    {
        NamedDatedRateProvider rba = Named("RBA", ("AUD", "USD", D1, 0.50m), ("USD", "GBP", D1, 0.80m));
        NamedDatedRateProvider ecb = Named("ECB", ("AUD", "USD", D1, 0.51m), ("USD", "GBP", D1, 0.81m));
        RateAggregationOptions options = new();
        options.Routes[new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD)] = new CurrencyPairRoute(new[] { "RBA", "ECB" });
        options.Routes[new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP)] = new CurrencyPairRoute(new[] { "ECB", "RBA" });
        AggregatingRateProvider agg = new(new[] { rba, ecb }, options);

        agg.TryGetRate("AUD", "USD", D1, RateLookupOptions.Exact, out RateLookupResult audUsd);
        agg.TryGetRate("USD", "GBP", D1, RateLookupOptions.Exact, out RateLookupResult usdGbp);

        Assert.AreEqual("RBA", audUsd.Rate.Provider);
        Assert.AreEqual("ECB", usdGbp.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a pair without a route uses the order the children were supplied in.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenNoRoute_ShouldUseSuppliedChildOrder()
    {
        AggregatingRateProvider agg = new(new[]
        {
            Named("RBA", ("AUD", "USD", D1, 0.50m)),
            Named("ECB", ("AUD", "USD", D1, 0.51m)),
        });

        agg.TryGetRate("AUD", "USD", D1, RateLookupOptions.Exact, out RateLookupResult result);

        Assert.AreEqual("RBA", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a configured default provider order is used for pairs without a route.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDefaultProviderOrderConfigured_ShouldUseThatOrder()
    {
        RateAggregationOptions options = new() { DefaultProviderOrder = new[] { "ECB", "RBA" } };
        AggregatingRateProvider agg = new(
            new[]
            {
                Named("RBA", ("AUD", "USD", D1, 0.50m)),
                Named("ECB", ("AUD", "USD", D1, 0.51m)),
            },
            options);

        agg.TryGetRate("AUD", "USD", D1, RateLookupOptions.Exact, out RateLookupResult result);

        Assert.AreEqual("ECB", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that an inverse-pair request is served through the direct pair's route when inversion is allowed.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenInversePairRequested_ShouldUseDirectRouteAndInvert()
    {
        RateAggregationOptions options = new();
        options.Routes[new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD)] = new CurrencyPairRoute(new[] { "RBA" });
        AggregatingRateProvider agg = new(new[] { Named("RBA", ("AUD", "USD", D1, 0.50m)) }, options);

        bool found = agg.TryGetRate("USD", "AUD", D1, RateLookupOptions.Exact, out RateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual("RBA", result.Rate.Provider);
        Assert.IsTrue(result.Rate.IsInverted);
    }

    /// <summary>
    /// Verifies that a route can override the strategy for a single pair, averaging that pair while others fall back.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenRouteOverridesStrategy_ShouldAverageThatPair()
    {
        NamedDatedRateProvider rba = Named("RBA", ("AUD", "USD", D1, 0.50m));
        NamedDatedRateProvider ecb = Named("ECB", ("AUD", "USD", D1, 0.52m));
        RateAggregationOptions options = new();
        options.Routes[new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD)] = new CurrencyPairRoute(new[] { "RBA", "ECB" }, new AverageStrategy());
        AggregatingRateProvider agg = new(new[] { rba, ecb }, options);

        agg.TryGetRate("AUD", "USD", D1, RateLookupOptions.Exact, out RateLookupResult result);

        Assert.AreEqual(0.51m, result.Rate.Rate);
        Assert.AreEqual("Average", result.Rate.Provider);
    }
}
