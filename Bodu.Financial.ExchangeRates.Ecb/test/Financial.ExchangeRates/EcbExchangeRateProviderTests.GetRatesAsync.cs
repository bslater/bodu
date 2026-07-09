// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateProviderTests.GetRatesAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the range-read behavior of <see cref="EcbExchangeRateProvider.GetRatesAsync" />.
/// </summary>
public partial class EcbExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that a direct EUR-based range read returns every observation in date order.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenDirectPair_ShouldReturnOrderedRates()
    {
        (EcbExchangeRateProvider provider, _) = Create(allowSync: false);

        IReadOnlyList<ExchangeRate> rates =
            [.. await provider.GetRatesAsync("EUR", "JPY", new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31))];

        Assert.HasCount(2, rates);
        Assert.AreEqual(new DateOnly(2023, 1, 3), rates[0].Date);
        Assert.AreEqual(140.06m, rates[0].Rate);
        Assert.AreEqual(new DateOnly(2023, 1, 4), rates[1].Date);
        Assert.AreEqual(139.92m, rates[1].Rate);
    }

    /// <summary>
    /// Verifies that the reverse direction inverts each observation in the range.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenInversePair_ShouldReturnInvertedRates()
    {
        (EcbExchangeRateProvider provider, _) = Create(allowSync: false);

        IReadOnlyList<ExchangeRate> rates =
            [.. await provider.GetRatesAsync("JPY", "EUR", new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31))];

        Assert.HasCount(2, rates);
        Assert.IsTrue(rates[0].IsInverted);
        Assert.AreEqual(1m / 140.06m, rates[0].Rate, 1e-12m);
    }

    /// <summary>
    /// Verifies that a cross-currency pair with neither side equal to EUR throws
    /// <see cref="RateSeriesNotFoundException" />.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenCrossPair_ShouldThrowSeriesNotFound()
    {
        (EcbExchangeRateProvider provider, _) = Create(allowSync: false);

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
        (EcbExchangeRateProvider provider, _) = Create(allowSync: false);

        _ = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await provider.GetRatesAsync("EUR", "USD", new DateOnly(2023, 12, 31), new DateOnly(2023, 1, 1));
        });
    }
}
