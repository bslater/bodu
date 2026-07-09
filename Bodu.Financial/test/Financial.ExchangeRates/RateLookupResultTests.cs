// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateLookupResultTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

[TestClass]
public partial class RateLookupResultTests
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
        RateLookupResult result = new(rate, date, RateDateResolution.Exact, 0, RateProvenance.Live(rate.Provider));

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
        RateLookupResult result = new(rate, requested, RateDateResolution.PreviousOnOrBefore, 2, RateProvenance.Live(rate.Provider));

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
        RateLookupResult a = new(rate, date, RateDateResolution.Exact, 0, RateProvenance.Live(rate.Provider));
        RateLookupResult b = new(rate, date, RateDateResolution.Exact, 0, RateProvenance.Live(rate.Provider));

        Assert.AreEqual(a, b);
    }
}
