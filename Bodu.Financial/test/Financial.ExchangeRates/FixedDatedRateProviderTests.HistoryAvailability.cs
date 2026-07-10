// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedRateProviderTests.HistoryAvailability.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class FixedDatedRateProviderTests
{
    /// <summary>
    /// Verifies that an empty book advertises <see cref="RateHistoryAvailability.Unbounded" />, since there is
    /// no observation to anchor a floor to.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenBookIsEmpty_ShouldReturnUnbounded()
    {
        FixedDatedRateProvider table = new([]);

        Assert.AreEqual(RateHistoryAvailability.Unbounded, table.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that a single-series book advertises <c>Since</c> anchored at that series' earliest observation date.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenSingleSeries_ShouldReturnSinceEarliestObservation()
    {
        ExchangeRate[] rates =
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 5), 1.51m, "RBA"),
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 3), 1.50m, "RBA"),
        ];
        FixedDatedRateProvider table = new(rates);

        Assert.AreEqual(RateHistoryAvailability.Since(new DateOnly(2024, 1, 3)), table.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that a multi-series book advertises <c>Since</c> anchored at the earliest observation date across every
    /// series, not just the first series enumerated.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenMultipleSeries_ShouldReturnSinceEarliestAcrossSeries()
    {
        ExchangeRate[] rates =
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 3), 1.50m, "RBA"),
            new ExchangeRate(CurrencyCode.EUR, CurrencyCode.USD, new DateOnly(2023, 6, 1), 1.08m, "ECB"),
        ];
        FixedDatedRateProvider table = new(rates);

        Assert.AreEqual(RateHistoryAvailability.Since(new DateOnly(2023, 6, 1)), table.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that the provider is discoverable through the <see cref="IHistoricalRateProvider" />
    /// capability interface, so composing layers can probe it at runtime.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenAccessedThroughInterface_ShouldMatchDirectAccess()
    {
        FixedDatedRateProvider table = new(SingleRate());

        IHistoricalRateProvider aware = table;

        Assert.AreEqual(table.HistoryAvailability, aware.HistoryAvailability);
        Assert.AreEqual(RateHistoryAvailability.Since(s_d1), aware.HistoryAvailability);
    }
}
