// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingRateProviderTests.RouteSnapshot.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that routing is frozen at construction: the per-pair routes and default order are resolved once, and
/// mutating the options afterwards has no effect on an existing provider.
/// </summary>
public sealed partial class AggregatingRateProviderTests
{
    /// <summary>
    /// Verifies that replacing a route on the options after construction does not change how an existing provider
    /// routes the pair.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenRouteReplacedAfterConstruction_ShouldKeepConstructionRouting()
    {
        var date = new DateOnly(2024, 1, 3);
        var pair = new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD);
        var options = new RateAggregationOptions();
        options.Routes[pair] = new CurrencyPairRoute(new[] { "B" });
        var provider = new AggregatingRateProvider(
            new[]
            {
                Named("A", ("AUD", "USD", date, 0.11m)),
                Named("B", ("AUD", "USD", date, 0.22m)),
            },
            options);

        options.Routes[pair] = new CurrencyPairRoute(new[] { "A" });

        Assert.IsTrue(provider.TryGetRate("AUD", "USD", date, RateLookupOptions.Exact, out RateLookupResult result));
        Assert.AreEqual(0.22m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that a route added to the options after construction is ignored, with the pair still resolved through
    /// the construction-time default candidates.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenRouteAddedAfterConstruction_ShouldUseConstructionDefaults()
    {
        var date = new DateOnly(2024, 1, 3);
        var pair = new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD);
        var options = new RateAggregationOptions();
        var provider = new AggregatingRateProvider(
            new[]
            {
                Named("A", ("AUD", "USD", date, 0.11m)),
                Named("B", ("AUD", "USD", date, 0.22m)),
            },
            options);

        options.Routes[pair] = new CurrencyPairRoute(new[] { "B" });

        // The children order supplied at construction is the default candidate set, so "A" answers first.
        Assert.IsTrue(provider.TryGetRate("AUD", "USD", date, RateLookupOptions.Exact, out RateLookupResult result));
        Assert.AreEqual(0.11m, result.Rate.Rate);
    }
}
