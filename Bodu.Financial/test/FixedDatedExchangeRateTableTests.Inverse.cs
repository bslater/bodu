// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedExchangeRateTableTests.Inverse.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class FixedDatedExchangeRateTableTests
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
            new ExchangeRate("USD", "AUD", s_d1, 1.50m, "RBA"),
        ];

        FixedDatedExchangeRateTable table = new(direct);

        bool found = table.TryGetRate(
            "AUD",
            "USD",
            s_d1,
            ExchangeRateLookupOptions.Exact,
            out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual("AUD", result.Rate.FromIsoCode);
        Assert.AreEqual("USD", result.Rate.ToIsoCode);
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
            new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 1), 1.50m, "RBA"),
            new ExchangeRate("AUD", "USD", new DateOnly(2024, 1, 10), 0.67m, "RBA"),
        ];

        FixedDatedExchangeRateTable table = new(rates);

        ExchangeRateLookupOptions previousTwoDays = ExchangeRateLookupOptions.PreviousWithin(2);

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
