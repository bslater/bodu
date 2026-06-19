// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateLookupResultTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

[TestClass]
public partial class ExchangeRateLookupResultTests
{
    /// <summary>
    /// Verifies that <c>IsExactDate</c> is <see langword="true" /> when the offset is
    /// zero days.
    /// </summary>
    [TestMethod]
    public void IsExactDate_WhenOffsetIsZero_ShouldReturnTrue()
    {
        DateOnly date = new(2024, 1, 3);
        ExchangeRate rate = new(CurrencyCode.USD, CurrencyCode.AUD, date, 1.5m, "RBA");
        ExchangeRateLookupResult result = new(rate, date, ExchangeRateDateResolution.Exact, 0, ExchangeRateProvenance.Live(rate.Provider));

        Assert.IsTrue(result.IsExactDate);
    }

    /// <summary>
    /// Verifies that <c>IsExactDate</c> is <see langword="false" /> when the offset is
    /// non-zero.
    /// </summary>
    [TestMethod]
    public void IsExactDate_WhenOffsetIsNonZero_ShouldReturnFalse()
    {
        DateOnly requested = new(2024, 1, 5);
        DateOnly resolved = new(2024, 1, 3);
        ExchangeRate rate = new(CurrencyCode.USD, CurrencyCode.AUD, resolved, 1.5m, "RBA");
        ExchangeRateLookupResult result = new(rate, requested, ExchangeRateDateResolution.PreviousOnOrBefore, 2, ExchangeRateProvenance.Live(rate.Provider));

        Assert.IsFalse(result.IsExactDate);
    }

    /// <summary>
    /// Verifies that records with identical components compare equal via the generated record-struct equality.
    /// </summary>
    [TestMethod]
    public void Equality_WhenComponentsMatch_ShouldReportEqual()
    {
        DateOnly date = new(2024, 1, 3);
        ExchangeRate rate = new(CurrencyCode.USD, CurrencyCode.AUD, date, 1.5m, "RBA");
        ExchangeRateLookupResult a = new(rate, date, ExchangeRateDateResolution.Exact, 0, ExchangeRateProvenance.Live(rate.Provider));
        ExchangeRateLookupResult b = new(rate, date, ExchangeRateDateResolution.Exact, 0, ExchangeRateProvenance.Live(rate.Provider));

        Assert.AreEqual(a, b);
    }
}
