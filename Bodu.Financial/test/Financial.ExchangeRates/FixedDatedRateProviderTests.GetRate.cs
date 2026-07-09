// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedRateProviderTests.GetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class FixedDatedRateProviderTests
{
    /// <summary>
    /// Verifies that the undated lookup resolves the most recent observation for the pair.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenUndated_ShouldReturnMostRecentObservation()
    {
        FixedDatedRateProvider table = new(
        [
            new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2024, 1, 3), 0.67m, "RBA"),
            new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2024, 1, 10), 0.69m, "RBA"),
            new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2024, 1, 6), 0.68m, "RBA"),
        ]);

        RateLookupResult result = table.GetRate("AUD", "USD");

        Assert.AreEqual(new DateOnly(2024, 1, 10), result.Rate.Date);
        Assert.AreEqual(0.69m, result.Rate.Rate);
        Assert.AreEqual(0, result.OffsetDays);
    }

    /// <summary>
    /// Verifies that the undated lookup inverts the reverse pair when only it is available.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenUndatedAndOnlyInverseAvailable_ShouldReturnInvertedMostRecent()
    {
        FixedDatedRateProvider table = new(
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 3), 1.50m, "RBA"),
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 10), 1.25m, "RBA"),
        ]);

        RateLookupResult result = table.GetRate("AUD", "USD");

        Assert.AreEqual(new DateOnly(2024, 1, 10), result.Rate.Date);
        Assert.IsTrue(result.Rate.IsInverted);
        Assert.AreEqual(1m / 1.25m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that the undated lookup throws when no observation exists for the pair.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenUndatedAndPairAbsent_ShouldThrowKeyNotFoundException()
    {
        FixedDatedRateProvider table = new(
        [
            new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2024, 1, 3), 0.67m, "RBA"),
        ]);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = table.GetRate("EUR", "JPY");
        });
    }
}
