// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingExchangeRateProviderTests.PerPairPriority.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class AggregatingExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that a per-pair route consults its providers in route order, overriding the child order.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenPairRouted_ShouldUseRouteOrderNotChildOrder()
    {
        NamedDatedExchangeRateProvider rba = Named("RBA", ("AUD", "USD", D1, 0.50m));
        NamedDatedExchangeRateProvider ecb = Named("ECB", ("AUD", "USD", D1, 0.51m));
        ExchangeRateAggregationOptions options = new();
        options.Routes[new ExchangeRatePair("AUD", "USD")] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" });
        AggregatingExchangeRateProvider agg = new(new[] { rba, ecb }, options);

        agg.TryGetRate("AUD", "USD", D1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.AreEqual("ECB", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that different pairs honour their own route order — AUD/USD via [RBA, ECB] and USD/GBP via [ECB, RBA].
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDifferentPairsRoutedDifferently_ShouldHonourEachRoute()
    {
        NamedDatedExchangeRateProvider rba = Named("RBA", ("AUD", "USD", D1, 0.50m), ("USD", "GBP", D1, 0.80m));
        NamedDatedExchangeRateProvider ecb = Named("ECB", ("AUD", "USD", D1, 0.51m), ("USD", "GBP", D1, 0.81m));
        ExchangeRateAggregationOptions options = new();
        options.Routes[new ExchangeRatePair("AUD", "USD")] = new ExchangeRatePairRoute(new[] { "RBA", "ECB" });
        options.Routes[new ExchangeRatePair("USD", "GBP")] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" });
        AggregatingExchangeRateProvider agg = new(new[] { rba, ecb }, options);

        agg.TryGetRate("AUD", "USD", D1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult audUsd);
        agg.TryGetRate("USD", "GBP", D1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult usdGbp);

        Assert.AreEqual("RBA", audUsd.Rate.Provider);
        Assert.AreEqual("ECB", usdGbp.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a pair without a route uses the order the children were supplied in.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenNoRoute_ShouldUseSuppliedChildOrder()
    {
        AggregatingExchangeRateProvider agg = new(new[]
        {
            Named("RBA", ("AUD", "USD", D1, 0.50m)),
            Named("ECB", ("AUD", "USD", D1, 0.51m)),
        });

        agg.TryGetRate("AUD", "USD", D1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.AreEqual("RBA", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a configured default provider order is used for pairs without a route.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDefaultProviderOrderConfigured_ShouldUseThatOrder()
    {
        ExchangeRateAggregationOptions options = new() { DefaultProviderOrder = new[] { "ECB", "RBA" } };
        AggregatingExchangeRateProvider agg = new(
            new[]
            {
                Named("RBA", ("AUD", "USD", D1, 0.50m)),
                Named("ECB", ("AUD", "USD", D1, 0.51m)),
            },
            options);

        agg.TryGetRate("AUD", "USD", D1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.AreEqual("ECB", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that an inverse-pair request is served through the direct pair's route when inversion is allowed.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenInversePairRequested_ShouldUseDirectRouteAndInvert()
    {
        ExchangeRateAggregationOptions options = new();
        options.Routes[new ExchangeRatePair("AUD", "USD")] = new ExchangeRatePairRoute(new[] { "RBA" });
        AggregatingExchangeRateProvider agg = new(new[] { Named("RBA", ("AUD", "USD", D1, 0.50m)) }, options);

        var found = agg.TryGetRate("USD", "AUD", D1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

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
        NamedDatedExchangeRateProvider rba = Named("RBA", ("AUD", "USD", D1, 0.50m));
        NamedDatedExchangeRateProvider ecb = Named("ECB", ("AUD", "USD", D1, 0.52m));
        ExchangeRateAggregationOptions options = new();
        options.Routes[new ExchangeRatePair("AUD", "USD")] = new ExchangeRatePairRoute(new[] { "RBA", "ECB" }, new AverageStrategy());
        AggregatingExchangeRateProvider agg = new(new[] { rba, ecb }, options);

        agg.TryGetRate("AUD", "USD", D1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.AreEqual(0.51m, result.Rate.Rate);
        Assert.AreEqual("Average", result.Rate.Provider);
    }
}
