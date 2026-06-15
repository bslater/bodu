// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateProviderTests.GetRatesAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Financial.ExchangeRates.Rba;

public partial class RbaExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that the range API returns the dated rates within the requested window, in ascending date order.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenRangeRequested_ShouldReturnDatedRatesInRange()
    {
        RbaExchangeRateProvider provider = await CreatePreloadedAsync();

        IReadOnlyList<ExchangeRate> rates =
            [.. await provider.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 31))];

        Assert.IsTrue(rates.Count > 0);
        Assert.IsTrue(rates.All(r => r.Date >= new DateOnly(2023, 1, 3) && r.Date <= new DateOnly(2023, 1, 31)));
        Assert.IsTrue(rates.SequenceEqual(rates.OrderBy(r => r.Date)));
        Assert.AreEqual(new DateOnly(2023, 1, 3), rates[0].Date);
        Assert.AreEqual(0.6828m, rates[0].Rate);
    }

    /// <summary>
    /// Verifies that the range API inverts the AUD-based series for the reverse direction.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenInversePair_ShouldReturnInvertedRates()
    {
        RbaExchangeRateProvider provider = await CreatePreloadedAsync();

        IReadOnlyList<ExchangeRate> rates =
            [.. await provider.GetRatesAsync("USD", "AUD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3))];

        Assert.AreEqual(1, rates.Count);
        Assert.IsTrue(rates[0].IsInverted);
        Assert.AreEqual(1m / 0.6828m, rates[0].Rate, 1e-12m);
    }

    /// <summary>
    /// Verifies that requesting a cross-currency pair throws <see cref="RbaExchangeRateSeriesNotFoundException" />.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenCrossPair_ShouldThrowSeriesNotFound()
    {
        (RbaExchangeRateProvider provider, _) = Create(allowSync: false);

        await Assert.ThrowsExactlyAsync<RbaExchangeRateSeriesNotFoundException>(async () =>
            await provider.GetRatesAsync("USD", "JPY", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 31)));
    }

    /// <summary>
    /// Verifies that an inverted date range throws <see cref="RbaExchangeRateDateRangeException" />.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenEndBeforeStart_ShouldThrowDateRange()
    {
        (RbaExchangeRateProvider provider, _) = Create(allowSync: false);

        await Assert.ThrowsExactlyAsync<RbaExchangeRateDateRangeException>(async () =>
            await provider.GetRatesAsync("AUD", "USD", new DateOnly(2023, 2, 1), new DateOnly(2023, 1, 1)));
    }
}
