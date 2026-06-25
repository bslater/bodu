// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateProviderTests.GetRatesAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class YahooExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that the range API returns the dated rates within the requested window, in ascending date order,
    /// skipping days that carry no published close.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenRangeRequested_ShouldReturnDatedRatesInRange()
    {
        (YahooExchangeRateProvider provider, _) = Create(allowSync: false);

        IReadOnlyList<ExchangeRate> rates =
            [.. await provider.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31))];

        Assert.HasCount(3, rates);
        Assert.IsTrue(rates.SequenceEqual(rates.OrderBy(r => r.Date)));
        Assert.AreEqual(new DateOnly(2023, 1, 3), rates[0].Date);
        Assert.AreEqual(0.6828m, rates[0].Rate);
        Assert.AreEqual(new DateOnly(2023, 1, 6), rates[2].Date);
        Assert.AreEqual(0.6855m, rates[2].Rate);
        Assert.DoesNotContain(r => r.Date == new DateOnly(2023, 1, 5), rates);
    }

    /// <summary>
    /// Verifies that the range API inverts a loaded series for the reverse direction when only the forward series is
    /// available.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenInversePair_ShouldReturnInvertedRates()
    {
        YahooExchangeRateProvider provider = await CreatePreloadedAsync();

        IReadOnlyList<ExchangeRate> rates =
            [.. await provider.GetRatesAsync("USD", "AUD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3))];

        Assert.HasCount(1, rates);
        Assert.IsTrue(rates[0].IsInverted);
        Assert.AreEqual(1m / 0.6828m, rates[0].Rate, 1e-12m);
    }

    /// <summary>
    /// Verifies that an inverted date range throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenEndBeforeStart_ShouldThrowArgumentException()
    {
        (YahooExchangeRateProvider provider, _) = Create(allowSync: false);

        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await provider.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 31), new DateOnly(2023, 1, 1));
        });
    }
}
