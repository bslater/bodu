// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedExchangeRateProviderTests.Inverse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class FixedDatedExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that an inverse-fallback result reports the rate as <c>1 / storedRate</c>, the original requested
    /// direction (not the stored direction), and an <see cref="ExchangeRate.IsInverted" /> flag of <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenInverseFallbackTaken_ShouldReportInvertedDirectionAndReciprocalRate()
    {
        ExchangeRate[] direct =
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, s_d1, 1.50m, "RBA"),
        ];

        FixedDatedExchangeRateProvider table = new(direct);

        bool found = table.TryGetRate(
            "AUD",
            "USD",
            s_d1,
            ExchangeRateLookupOptions.Exact,
            out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(CurrencyCode.AUD, result.Rate.From);
        Assert.AreEqual(CurrencyCode.USD, result.Rate.To);
        Assert.AreEqual(1m / 1.50m, result.Rate.Rate);
        Assert.IsTrue(result.Rate.IsInverted);
        Assert.AreEqual("RBA", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that when the direct pair fails the tolerance check but the inverse pair satisfies it, the inverse
    /// fallback is used.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDirectFailsToleranceButInversePasses_ShouldUseInverse()
    {
        ExchangeRate[] rates =
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 1), 1.50m, "RBA"),
            new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2024, 1, 10), 0.67m, "RBA"),
        ];

        FixedDatedExchangeRateProvider table = new(rates);

        var previousTwoDays = ExchangeRateLookupOptions.PreviousWithin(2);

        bool found = table.TryGetRate(
            "AUD",
            "USD",
            new DateOnly(2024, 1, 11),
            previousTwoDays,
            out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(0.67m, result.Rate.Rate);
        Assert.IsFalse(result.Rate.IsInverted);
        Assert.AreEqual(new DateOnly(2024, 1, 10), result.Rate.Date);
    }
}
