// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateProviderTests.Matrix.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the symmetric synchronous and asynchronous lookup matrix exposed by
/// <see cref="YahooExchangeRateProvider" /> through <see cref="WebExchangeRateProvider" />.
/// </summary>
public partial class YahooExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that the dated asynchronous lookup resolves the published rate for an exact date.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenDated_ShouldReturnPublishedRate()
    {
        YahooExchangeRateProvider provider = await CreatePreloadedAsync();

        ExchangeRateLookupResult result = await provider.GetRateAsync("AUD", "USD", new DateOnly(2023, 1, 3));

        Assert.AreEqual(0.6828m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that the undated synchronous and asynchronous lookups agree and report the provider name.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenUndated_ShouldAgreeAcrossSyncAndAsync()
    {
        YahooExchangeRateProvider provider = await CreatePreloadedAsync();

        ExchangeRateLookupResult sync = provider.GetRate("AUD", "USD");
        ExchangeRateLookupResult async = await provider.GetRateAsync("AUD", "USD");

        Assert.AreEqual(sync.Rate.Rate, async.Rate.Rate);
        Assert.IsGreaterThan(0m, sync.Rate.Rate);
        Assert.AreEqual(YahooExchangeRateProvider.ProviderName, sync.Rate.Provider);
    }

    /// <summary>
    /// Verifies that the explicit timeless <see cref="IExchangeRateProvider" /> surface returns the same multiplier as
    /// the rich undated lookup.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenAccessedThroughIExchangeRateProvider_ShouldMatchRichResult()
    {
        YahooExchangeRateProvider provider = await CreatePreloadedAsync();

        ExchangeRateLookupResult rich = provider.GetRate("AUD", "USD");
        decimal plain = ((IExchangeRateProvider)provider).GetRate("AUD", "USD");

        Assert.AreEqual(rich.Rate.Rate, plain);
    }

    /// <summary>
    /// Verifies that the synchronous range lookup returns the loaded window ordered by date.
    /// </summary>
    [TestMethod]
    public async Task GetRates_WhenLoaded_ShouldReturnWindowOrderedByDate()
    {
        YahooExchangeRateProvider provider = await CreatePreloadedAsync();

        List<ExchangeRate> rates = [.. provider.GetRates("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31))];

        Assert.IsNotEmpty(rates);
        for (int i = 1; i < rates.Count; i++)
            Assert.IsLessThanOrEqualTo(rates[i].Date, rates[i - 1].Date);
    }
}
