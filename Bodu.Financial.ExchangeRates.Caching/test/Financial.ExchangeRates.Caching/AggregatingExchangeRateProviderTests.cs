// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingExchangeRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the grouping, routing, and both-surface behaviour of <see cref="AggregatingExchangeRateProvider" />.
/// </summary>
[TestClass]
public sealed partial class AggregatingExchangeRateProviderTests
{
    /// <summary>
    /// A fixed date used by the tests.
    /// </summary>
    private static readonly DateOnly D1 = new(2024, 1, 3);

    /// <summary>
    /// Builds a named child source from the supplied observation rows, tagged with the child name.
    /// </summary>
    /// <param name="name">The child name.</param>
    /// <param name="rows">The observation rows.</param>
    /// <returns>The named child.</returns>
    private static NamedDatedExchangeRateProvider Named(string name, params (string From, string To, DateOnly Date, decimal Rate)[] rows) =>
        new(name, new FixedDatedExchangeRateProvider(rows.Select(r => new ExchangeRate(CurrencyInfo.ParseCurrencyCode(r.From), CurrencyInfo.ParseCurrencyCode(r.To), r.Date, r.Rate, name))));

    /// <summary>
    /// Verifies that a <see langword="null" /> children collection is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenChildrenIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new AggregatingExchangeRateProvider(null!);
        });

        Assert.AreEqual("children", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty children collection is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenChildrenIsEmpty_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new AggregatingExchangeRateProvider(Array.Empty<NamedDatedExchangeRateProvider>());
        });

        Assert.AreEqual("children", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a blank child name is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenChildNameIsBlank_ShouldThrowArgumentException()
    {
        NamedDatedExchangeRateProvider blank = new("  ", new FixedDatedExchangeRateProvider(Array.Empty<ExchangeRate>()));

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new AggregatingExchangeRateProvider(new[] { blank });
        });

        Assert.AreEqual("children", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> child provider is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenChildProviderIsNull_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new AggregatingExchangeRateProvider(new[] { new NamedDatedExchangeRateProvider("X", null!) });
        });

        Assert.AreEqual("children", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a duplicate child name is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenChildNameIsDuplicated_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new AggregatingExchangeRateProvider(new[]
            {
                Named("Dup", ("USD", "AUD", D1, 1.5m)),
                Named("Dup", ("USD", "AUD", D1, 1.6m)),
            });
        });

        Assert.AreEqual("children", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a route referencing a child name that was not supplied is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRouteReferencesUnknownChild_ShouldThrowArgumentException()
    {
        ExchangeRateAggregationOptions options = new();
        options.Routes[new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD)] = new ExchangeRatePairRoute(new[] { "Unknown" });

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new AggregatingExchangeRateProvider(new[] { Named("RBA", ("USD", "AUD", D1, 1.5m)) }, options);
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a single-child aggregator resolves that child's rate.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetRate_WhenSingleChild_ShouldReturnChildRate()
    {
        AggregatingExchangeRateProvider agg = new(new[] { Named("RBA", ("USD", "AUD", D1, 1.5m)) });

        ExchangeRateLookupResult result = agg.GetRate("USD", "AUD", D1, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(1.5m, result.Rate.Rate);
        Assert.AreEqual("RBA", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that <see cref="AggregatingExchangeRateProvider.GetRate(string, string, DateOnly, ExchangeRateLookupOptions?)" />
    /// throws when no child can satisfy the request.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenAllChildrenMiss_ShouldThrowKeyNotFoundException()
    {
        AggregatingExchangeRateProvider agg = new(new[] { Named("RBA") });

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = agg.GetRate("USD", "AUD", D1, ExchangeRateLookupOptions.Exact);
        });
    }

    /// <summary>
    /// Verifies that a grouped child is resolvable by name through <see cref="AggregatingExchangeRateProvider.TryGetProvider" />.
    /// </summary>
    [TestMethod]
    public void TryGetProvider_WhenNameKnown_ShouldReturnChild()
    {
        NamedDatedExchangeRateProvider rba = Named("RBA", ("USD", "AUD", D1, 1.5m));
        AggregatingExchangeRateProvider agg = new(new[] { rba });

        bool found = agg.TryGetProvider("RBA", out IDatedExchangeRateProvider? provider);

        Assert.IsTrue(found);
        Assert.AreSame(rba.Provider, provider);
    }

    /// <summary>
    /// Verifies that an unknown name yields <see langword="false" /> from <see cref="AggregatingExchangeRateProvider.TryGetProvider" />.
    /// </summary>
    [TestMethod]
    public void TryGetProvider_WhenNameUnknown_ShouldReturnFalse()
    {
        AggregatingExchangeRateProvider agg = new(new[] { Named("RBA", ("USD", "AUD", D1, 1.5m)) });

        Assert.IsFalse(agg.TryGetProvider("ECB", out _));
    }

    /// <summary>
    /// Verifies that the timeless surface resolves the current UTC date from the injected time provider.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenTimelessSurface_ShouldResolveCurrentUtcDate()
    {
        MutableTimeProvider clock = new(new DateTimeOffset(D1.Year, D1.Month, D1.Day, 12, 0, 0, TimeSpan.Zero));
        AggregatingExchangeRateProvider agg = new(new[] { Named("RBA", ("USD", "AUD", D1, 1.5m)) }, options: null, timeProvider: clock);

        decimal rate = ((IExchangeRateProvider)agg).GetRate("USD", "AUD");

        Assert.AreEqual(1.5m, rate);
    }
}
