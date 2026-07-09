// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderTests.GetRatesAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the range-read behavior of <see cref="BoeRateProvider.GetRatesAsync" />.
/// </summary>
public partial class BoeRateProviderTests
{
    /// <summary>
    /// Verifies that a direct GBP-based range read returns every observation in date order.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenDirectPair_ShouldReturnOrderedRates()
    {
        (BoeRateProvider provider, _) = Create(allowSync: false);

        IReadOnlyList<ExchangeRate> rates =
            [.. await provider.GetRatesAsync("GBP", "JPY", new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31))];

        Assert.HasCount(2, rates);
        Assert.AreEqual(new DateOnly(2023, 1, 3), rates[0].Date);
        Assert.AreEqual(159.10m, rates[0].Rate);
        Assert.AreEqual(new DateOnly(2023, 1, 4), rates[1].Date);
        Assert.AreEqual(158.50m, rates[1].Rate);
    }

    /// <summary>
    /// Verifies that the reverse direction inverts each observation in the range.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenInversePair_ShouldReturnInvertedRates()
    {
        (BoeRateProvider provider, _) = Create(allowSync: false);

        IReadOnlyList<ExchangeRate> rates =
            [.. await provider.GetRatesAsync("JPY", "GBP", new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31))];

        Assert.HasCount(2, rates);
        Assert.IsTrue(rates[0].IsInverted);
        Assert.AreEqual(1m / 159.10m, rates[0].Rate, 1e-12m);
    }

    /// <summary>
    /// Verifies that a cross-currency pair with neither side equal to GBP throws
    /// <see cref="RateSeriesNotFoundException" />.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenCrossPair_ShouldThrowSeriesNotFound()
    {
        (BoeRateProvider provider, _) = Create(allowSync: false);

        _ = await Assert.ThrowsExactlyAsync<RateSeriesNotFoundException>(async () =>
        {
            _ = await provider.GetRatesAsync("USD", "JPY", new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));
        });
    }

    /// <summary>
    /// Verifies that an inverted date range throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenRangeInverted_ShouldThrowArgumentException()
    {
        (BoeRateProvider provider, _) = Create(allowSync: false);

        _ = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await provider.GetRatesAsync("GBP", "USD", new DateOnly(2023, 12, 31), new DateOnly(2023, 1, 1));
        });
    }
}
