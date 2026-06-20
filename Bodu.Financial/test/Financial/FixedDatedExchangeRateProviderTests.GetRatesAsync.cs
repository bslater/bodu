// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedExchangeRateProviderTests.GetRatesAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class FixedDatedExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that the range lookup returns only the observations within the inclusive window, ordered by date.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenObservationsInRange_ShouldReturnOrderedSubset()
    {
        FixedDatedExchangeRateProvider table = new(
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 6), 1.52m, "RBA"),
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 3), 1.50m, "RBA"),
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 10), 1.55m, "RBA"),
        ]);

        IReadOnlyList<ExchangeRate> rates =
            [.. await table.GetRatesAsync("USD", "AUD", new DateOnly(2024, 1, 3), new DateOnly(2024, 1, 6))];

        Assert.AreEqual(2, rates.Count);
        Assert.AreEqual(new DateOnly(2024, 1, 3), rates[0].Date);
        Assert.AreEqual(new DateOnly(2024, 1, 6), rates[1].Date);
    }

    /// <summary>
    /// Verifies that the range lookup returns an empty list when the pair is not present.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenPairAbsent_ShouldReturnEmpty()
    {
        FixedDatedExchangeRateProvider table = new(SingleRate());

        IReadOnlyList<ExchangeRate> rates =
            [.. await table.GetRatesAsync("EUR", "JPY", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31))];

        Assert.AreEqual(0, rates.Count);
    }

    /// <summary>
    /// Verifies that an inverted range is rejected.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenRangeInverted_ShouldThrowArgumentException()
    {
        FixedDatedExchangeRateProvider table = new(SingleRate());

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await table.GetRatesAsync("USD", "AUD", new DateOnly(2024, 1, 31), new DateOnly(2024, 1, 1));
        });

        Assert.AreEqual("endDate", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the range result reports the requested window and the observed span alongside the rates.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenObservationsInRange_ShouldReportRequestedWindowAndObservedSpan()
    {
        FixedDatedExchangeRateProvider table = new(
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 6), 1.52m, "RBA"),
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 3), 1.50m, "RBA"),
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 10), 1.55m, "RBA"),
        ]);

        ExchangeRateRangeResult result = table.GetRates("USD", "AUD", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 8));

        Assert.AreEqual("USD", result.FromIsoCode);
        Assert.AreEqual("AUD", result.ToIsoCode);
        Assert.AreEqual(new DateOnly(2024, 1, 1), result.RequestedStartDate);
        Assert.AreEqual(new DateOnly(2024, 1, 8), result.RequestedEndDate);
        Assert.AreEqual(2, result.Count);
        Assert.IsFalse(result.IsEmpty);
        Assert.AreEqual(new DateOnly(2024, 1, 3), result.FirstObservedDate);
        Assert.AreEqual(new DateOnly(2024, 1, 6), result.LastObservedDate);

        // The result is itself the rate sequence (indexable, enumerable, LINQ-friendly).
        Assert.AreEqual(new DateOnly(2024, 1, 3), result[0].Date);
    }

    /// <summary>
    /// Verifies that an empty range result reports no observed span.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenPairAbsent_ShouldReportEmptyWithNullObservedSpan()
    {
        FixedDatedExchangeRateProvider table = new(SingleRate());

        ExchangeRateRangeResult result = table.GetRates("EUR", "JPY", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        Assert.IsTrue(result.IsEmpty);
        Assert.AreEqual(0, result.Count);
        Assert.IsNull(result.FirstObservedDate);
        Assert.IsNull(result.LastObservedDate);
    }
}
